# ADR-004: Persistência híbrida: EF Core 10 para escrita e Dapper para leitura complexa, no SQL Server

**Date**: 2026-09-30
**Status**: Accepted (revisado em 2026-09-30: Dapper entra como padrão de leitura complexa e como escrita justificada)

> **Em resumo:** todos os dados ficam no SQL Server do provedor. O **EF Core 10** é o padrão para escrever (cadastros, regras do domínio, controle de edição simultânea) e para o esquema (migrations). O **Dapper** é o padrão para **leituras complexas** (junções, filtros dinâmicos, listas paginadas com ordenação variável) e pode ser usado para **escrever em lote ou com SQL específico**, sempre com um comentário que justifique. Chaves são inteiros sequenciais, tabelas editáveis têm `rowversion`, datas ficam em UTC e o preço é guardado em centavos, nulo quando a categoria não tem preço (Serviços).

## Context
- Profile: SQL Server 2022+ com EF Core 10 (`rules/overrides/database-sqlserver.md`: `int IDENTITY`, `rowversion`, migrations por script idempotente, nunca `Database.Migrate()` em produção). `rules/database.md`: EF Core para escrita e leituras simples; Dapper para leituras complexas.
- S28 (preço em centavos, teto R$ 99.999.999,99) e A6 (Serviços sem preço).
- US-010: dois administradores podem decidir o mesmo anúncio ao mesmo tempo.
- A busca (US-002) combina texto, categoria com descendentes, UF, cidade, preço, filtros por grupo de campos e ordenação variável; a lista do painel (US-012) junta anúncios, categorias e autores. Ambas são leituras de filtro dinâmico.
- A hospedagem compartilhada oferece um banco SQL Server gerenciado (versão a confirmar, AR-03); a aplicação não consegue rodar migrations na inicialização com segurança.
- Decisão do Product Owner (2026-09-30): modelo híbrido, com critério, substituindo a decisão anterior de usar só EF Core.

## Options Considered
| Option | Pros | Cons |
|--------|------|------|
| **A. Híbrido: EF Core para escrita e migrations; Dapper para leitura complexa e para escrita em casos justificados** | Cada ferramenta no que faz melhor; SQL explícito e otimizável nas consultas de filtro dinâmico; migrations e concorrência otimista continuam no EF | Duas formas de acesso a dados; consultas Dapper não herdam o que o EF faz sozinho (filtros globais, conversões); exige teste com banco real |
| B. Só EF Core (decisão anterior) | Um modelo só; LINQ testável em memória | Filtros dinâmicos geram SQL verboso e difícil de ajustar; consultas de relatório e de busca ficam caras de otimizar |
| C. Só Dapper | SQL explícito | Sem migrations nem rastreamento de mudanças; mais código repetitivo; perde a concorrência otimista pronta |

## Decision
Adopt **Option A** because as leituras de filtro dinâmico (busca e lista do painel) são onde o SQL precisa ser controlado, enquanto a escrita e o esquema se beneficiam das regras de domínio, da auditoria e da concorrência do EF Core.

**Critério de decisão (vale para toda consulta nova):**

| Operação | Ferramenta | Quando |
|---|---|---|
| Escrita comum (1 ou 2 tabelas, com regras, validação ou auditoria) | **EF Core** | Padrão |
| Esquema e evolução | **EF Core** (migrations) | Sempre |
| Leitura complexa: junções, filtros dinâmicos, agregações, relatórios, listas paginadas com ordenação variável | **Dapper** | Padrão |
| Leitura simples (por id, lista curta, projeção de uma tabela) | **EF Core** (`AsNoTracking` + projeção) | Padrão; Dapper só se o SQL gerado for ineficiente |
| Escrita em lote (centenas ou milhares de linhas) | **Dapper** | Justificada |
| Escrita com SQL que o EF não gera bem (`MERGE`, `OUTPUT`, dicas de índice) | **Dapper** | Justificada |
| Operação em lote de desempenho extremo (manutenção, segundo plano) | **Dapper** | Só se o volume medido justificar |

**Regra de desempate:** se o EF Core gerar SQL ineficiente ou verboso, usar Dapper; se o EF Core resolver bem, ficar com ele. **Todo uso de Dapper para escrever tem um comentário no código com o motivo**, por exemplo: `// Dapper: carga de 10 mil linhas; o EF Core geraria 10 mil INSERTs individuais`.

## Consequences
**Positive**: busca e lista do painel com SQL controlado e parametrizado; esquema versionado junto com o código; concorrência otimista pronta nas escritas; escrita em lote possível sem contornar o EF de forma improvisada.
**Negative**: duas formas de acesso a dados no mesmo projeto; migrations precisam ser aplicadas manualmente no banco do provedor (script idempotente), um passo a mais em cada publicação; o Dapper **não roda em banco em memória**, então suas consultas só são provadas com SQL Server real (TestContainers, no `/test`).
**Risks**:
- Esquecer de aplicar o script antes de publicar o site. Mitigação: checklist de publicação no `ARCHITECTURE.md` §9 e *health check* `/health/ready` que confere a última migration aplicada.
- **Uma consulta Dapper esquecer o "só publicados".** O Dapper não herda filtros do EF, e um anúncio não publicado apareceria no site público (US-003-S06). Mitigação: o filtro `Status = Publicado` vem de um único fragmento de SQL compartilhado pelas leituras públicas, e cada consulta pública tem um teste com anúncio em cada situação que só aceita o publicado.
- Injeção de SQL em filtros e ordenação dinâmicos. Mitigação: valores sempre como parâmetros; colunas e direção de ordenação só de uma lista permitida, nunca do texto recebido.
- Escrita Dapper fora da transação do EF quando precisa ser atômica com ela. Mitigação: nesses casos o Dapper usa a conexão e a transação do `DbContext` (ver Implementation Notes).

## v2 Upgrade Trigger
Revisit this decision when **any** of the following becomes true:
- A maior parte das consultas de leitura acabar em Dapper por desempate (sinal de que o EF Core não atende ao caso de uso) → avaliar um modelo de leitura dedicado. Sem número fixo: a avaliação é feita a cada revisão do `ARCHITECTURE.md`.
- Uma escrita por EF Core passar de 500 ms no p95 (NFR-04) mesmo com índices → avaliar Dapper para ela, com o comentário de justificativa.
- A proporção leitura/escrita passar de 4:1 com CPU do banco acima de 70% (réplica de leitura, `principles-and-practices.md` §5).
- A hospedagem permitir aplicar migrations automaticamente num pipeline.

## Implementation Notes
- **Pacotes:** `Microsoft.EntityFrameworkCore.SqlServer` e `.Design` já estão no `Directory.Packages.props` (`.Design` com `PrivateAssets="all"`); **`Dapper` 2.1.89** é acrescentado (Apache-2.0; aprovação do Product Owner em 2026-09-30). `Microsoft.Data.SqlClient` vem pelo provider do EF Core.
- **Estrutura de código (Infrastructure):**
  - escritas com EF Core: repositórios/serviços sobre o `AppDbContext`;
  - leituras com Dapper: *query objects* ou *read repositories* (`IBuscaReadRepository`, `IPainelListaReadRepository`…) com as interfaces no Core e as implementações na Infrastructure, sobre `IDbConnection`;
  - escritas Dapper justificadas: métodos específicos nos repositórios, cada um com o comentário de justificativa.
- **Conexão:** `IDbConnection` registrado como *scoped* com `SqlConnection` e a mesma cadeia de conexão do EF Core. Uma escrita Dapper que precise ser atômica com uma escrita do EF usa `context.Database.GetDbConnection()` e `context.Database.CurrentTransaction?.GetDbTransaction()`.
- **Onde o Dapper é usado na v1** (o plano `plans/plan.md` segue esta lista):
  - **leitura:** busca com filtros dinâmicos (tarefa 5.4) e lista do painel com junções (tarefa 4.4);
  - **leitura e escrita em lote:** ferramenta de exportação do catálogo de veículos, que lê as tabelas do GazetaOnline e, nos bancos de desenvolvimento e de teste, carrega o catálogo em lote (tarefa 2.5; o script SQL continua sendo o caminho para o banco do provedor, ADR-008);
  - **fica no EF Core:** página inicial, página de categoria, detalhe, favoritos por ids, cadastros e fotos. A limpeza dos originais de foto (tarefa 3.6) usa `ExecuteUpdateAsync` do EF, uma única instrução por lote; só migra para Dapper se o volume medido justificar (gatilho no ADR-005).
- **SQL dinâmico:** montado por um construtor único (`SqlBuilder`) que só aceita fragmentos fixos, parâmetros nomeados e colunas de ordenação de uma lista permitida.
- Chaves `int IDENTITY`; exceção: `Categories.Id` recebe os ids reais da carga inicial (`IDENTITY_INSERT` na migration de carga) e a sequência continua depois do maior id.
- `UseSqlServer(..., o => o.EnableRetryOnFailure(3))`; transações explícitas dentro de `CreateExecutionStrategy().ExecuteAsync(...)`.
- `RowVersion` (`rowversion`) em `Ads`, `Categories`, `SiteSettings` e no usuário; conflito vira `ConflictException` → 409 / mensagem "Este anúncio já foi publicado por outro administrador" (US-010). **Escritas Dapper em tabelas com `RowVersion` precisam conferir a versão na cláusula `WHERE`.**
- Preço: `Ads.PriceCents bigint NULL`; o formulário converte a máscara (dígitos) em centavos; nulo quando o grupo não tem preço; nunca zero para representar "sem preço".
- Datas em UTC (`datetime2`); conversão para `America/Sao_Paulo` só na exibição. Colunas de auditoria preenchidas no `SaveChangesAsync`; escritas Dapper preenchem as mesmas colunas explicitamente.
- Migrations: `dotnet ef migrations script --idempotent` gera o script de cada publicação; o arquivo acompanha o pacote de publicação e é executado na ferramenta de SQL do provedor antes do site novo.
- **Testes:** consultas Dapper precisam de testes de integração com **SQL Server real (TestContainers, no `/test`)**; no `/build`, as telas são testadas com os *read repositories* substituídos por implementações falsas. Escritas Dapper ganham teste com banco real que confere o efeito (linhas gravadas, versão, auditoria).
- Leituras EF com `AsNoTracking` e projeção para ViewModels; paginação no banco (`Skip`/`Take`).
