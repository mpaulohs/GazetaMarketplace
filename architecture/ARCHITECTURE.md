# Architecture: GazetaMarketplace v1

> **Em resumo:** o GazetaMarketplace é um **único site ASP.NET Core 10** (monólito modular), com o site público e o painel da equipe no mesmo processo, gravando num **SQL Server** e guardando as fotos numa **pasta persistente fora da raiz do site**. Ele roda num **IIS de hospedagem compartilhada (SmarterASP.NET)**, publicado pelo Visual Studio com WebDeploy; por isso segredos vêm de variáveis de ambiente do `web.config` gerado na publicação, e não há Docker, Redis, fila nem Prometheus em produção. Este documento é para os engenheiros que vão planejar (`/plan`) e construir (`/build`): diz **como** o sistema atende ao `specs/SPEC.md` (Approved v1.2) e **por quê**. As decisões que restringem mudanças futuras estão nos ADRs de `architecture/adr/`.

| Campo | Valor |
|---|---|
| SPEC de origem | `specs/SPEC.md` — Approved v1.2 (Coverage full) |
| Modo | greenfield |
| Data | 2026-09-30 |
| Diagramas | `diagrams/system-context.md` · `diagrams/container.md` · `diagrams/component.md` · `diagrams/sequence/` |

---

## 1. Objetivos da arquitetura

- **Simplicidade operacional:** um único site para publicar e manter, compatível com hospedagem compartilhada, sem serviços auxiliares para operar (ADR-001, ADR-012).
- **Atender às NFRs do SPEC com a escala da v1** (cerca de 200 anúncios ativos, 1.000 visitas por dia), deixando **pontos de troca** preparados para crescer sem reescrever: armazenamento de fotos, fonte do catálogo de veículos, provedor de e-mail e cache.
- **Segurança por padrão:** controle de acesso no servidor, antiforgery em toda escrita, cabeçalhos HTTP de segurança, segredos fora do repositório.
- **Campos por categoria sem mudar o esquema do banco** a cada categoria nova: 124 categorias ativas, 18 grupos de campos (ADR-002).

## 2. Restrições que moldam o desenho

| Restrição | Origem | Efeito no desenho |
|---|---|---|
| Hospedagem compartilhada IIS (SmarterASP.NET), deploy por WebDeploy do Visual Studio | Decisão do Product Owner | Sem Docker e sem Prometheus/Grafana; só um serviço em segundo plano leve (limpeza diária dos originais), sujeito ao ciclo de vida do IIS (§10); configuração por variáveis de ambiente no `web.config` (ADR-011) |
| .NET 10 (RC no momento), `Nullable` e `ImplicitUsings` desligados, `TreatWarningsAsErrors`, Central Package Management | `Directory.Build.props`, `CLAUDE.md` | Pode exigir publicação *self-contained* (OQ AR-02); todo pacote novo passa por aprovação |
| Frontend: Razor + Bootstrap 5.3.8 estático + JavaScript em módulos, sem framework nem bundler | `rules/frontend.md` | Páginas renderizadas no servidor; poucos endpoints JSON (CEP, fotos, catálogo de veículos, favoritos) |
| Testes: MSTest + Microsoft.Testing.Platform + Playwright | `CLAUDE.md` | Arquitetura testável sem servidor externo: todas as integrações atrás de interfaces |

---

## 3. Visão geral

**Decisão: monólito modular em Clean Architecture, com 3 projetos** (ADR-001).

| Projeto | Papel | Depende de |
|---|---|---|
| `GazetaMarketplace.Web` | Apresentação: controllers MVC e Razor (site público e painel, este na área `Panel`), endpoints JSON mínimos em `/api/v1/...`, middlewares (erros, correlação, cabeçalhos de segurança), composição (DI) | Core, Infrastructure |
| `GazetaMarketplace.Core` | Domínio e aplicação: entidades, regras (situação do anúncio, campos por categoria, limites de fotos), serviços de aplicação, interfaces (`IPhotoStorage`, `ICepLookup`, `IEmailSender`, `IVehicleCatalog`…), exceções `AppException` | nada além da BCL |
| `GazetaMarketplace.Infrastructure` | EF Core 10 (escrita e migrations) + Dapper (leitura complexa e escrita justificada) sobre SQL Server, ASP.NET Core Identity, `FileSystemPhotoStorage`, processamento de imagem, `ViaCepLookup`, `SendGridEmailSender`, importador do catálogo | Core |

Testes: `tests/GazetaMarketplace.Web.Tests` (MSTest: unidade e integração com `WebApplicationFactory`) e `tests/GazetaMarketplace.Web.Tests.Playwright` (ponta a ponta). Os projetos de exemplo do template (`ClaudeStack.*`, `Example.*`) não fazem parte do produto e ficam fora da solução do GazetaMarketplace (tarefa de limpeza para o `/plan`).

### 3.1 Módulos dentro do monólito

| Módulo | Responsabilidade | Histórias |
|---|---|---|
| Vitrine | Página inicial, categoria, busca e filtros, detalhe, favoritos (resolução de ids) | US-001 a US-005 |
| Anúncios | Criar, editar, enviar para revisão, revisar, publicar, rejeitar, despublicar, arquivar; campos por categoria | US-008 a US-012 |
| Fotos | Envio, validação, conversão, miniaturas, ordem e capa, entrega das imagens | US-003, US-008 |
| Categorias | Árvore de até 3 níveis, grupos de campos, ordem | US-013 |
| Equipe e acesso | Login, sessão, troca obrigatória de senha, recuperação, usuários e papéis | US-006, US-007, US-014 |
| Configurações | Telefone/WhatsApp do site | US-015, US-004 |
| Localização | Consulta de CEP e lista oficial de cidades por UF | US-008 |
| Catálogo de veículos | Marca → modelo → ano → versão de carros e motos | US-008, US-002 |

Módulos conversam por interfaces do Core; nenhum módulo lê as tabelas de outro diretamente (regra de `principles-and-practices.md` §3.1).

---

## 4. Requisitos não funcionais: mecanismo de cada um

| NFR | Meta (SPEC) | Resposta da arquitetura |
|---|---|---|
| NFR-01 · LCP | < 2,5 s (celular, 4G) | HTML renderizado no servidor; capa servida na versão reduzida (480 px, WebP) com `width`/`height`; `fetchpriority="high"` só na capa do detalhe; CSS e JS com cache longo (`asp-append-version` nos CSS; `asp-module-version` nos scripts de página, que cobre os módulos importados); compressão de resposta |
| NFR-02 · INP | < 200 ms | JavaScript mínimo por página (módulos ES, um por tela); nenhuma biblioteca de interface além do Bootstrap |
| NFR-03 · CLS | < 0,1 | Toda imagem com dimensões explícitas; área da galeria e dos cards com `aspect-ratio` reservado; esqueletos com altura fixa |
| NFR-04 · Escala e resposta do servidor | 200 anúncios, 1.000 visitas/dia, p95 < 500 ms | Leituras simples com `AsNoTracking` e projeção; busca e lista do painel em Dapper com SQL controlado (§6.5); índices nas colunas de filtro (§6.4); árvore de categorias e catálogo em cache de memória; paginação no banco (24 e 20 por página) |
| NFR-05 · Peso das imagens | Lista ≤ 2 MB; detalhe ≤ 3 MB | Cada foto é gravada em duas versões: 1.600 px e 480 px, WebP qualidade 80 (ADR-005); a lista usa só a de 480 px; no detalhe, fotos além da primeira com `loading="lazy"` |
| NFR-06 · Tentativas de login | 5 falhas em 15 min por origem | *Rate limiter* do ASP.NET Core por IP na rota de login (5 por 15 min) **e** bloqueio de conta do Identity (5 falhas → 15 min) |
| NFR-07 · Senha | ≥ 8, maiúscula, minúscula, número, símbolo; só hash | Opções de senha do Identity com essa política; hash PBKDF2 do Identity (ADR-003) |
| NFR-08 · Sessão | 30 min sem uso | Cookie de autenticação com expiração deslizante de 30 min; `SecurityStamp` revalidado a cada 5 min (usuário desativado perde o acesso) |
| NFR-09 · Link de redefinição | 1 h, uso único | Token de redefinição do Identity com vida de 1 h; uso único garantido pela troca do `SecurityStamp` ao redefinir |
| NFR-10 · Cabeçalhos HTTP | XCTO, XFO, Referrer-Policy, HSTS, CSP | Middleware próprio no início do pipeline (§7); HSTS só em produção |
| NFR-11 · Antiforgery | 100% das escritas | Filtro global `AutoValidateAntiforgeryToken` no MVC; endpoints JSON de escrita exigem o cabeçalho `RequestVerificationToken` |
| NFR-12 · Envio de fotos | Formatos por conteúdo, 10 MB, 20/6/0, HEIC convertido | Validação pelo conteúdo (assinatura do arquivo) antes de gravar; limite de 10 MB no servidor (`RequestSizeLimit`); limite por categoria vindo do grupo de campos; reprocessamento de toda foto para WebP; o original fica 30 dias numa pasta de descarte e depois é apagado (ADR-005) |
| NFR-13 · Controle de acesso | Login + papel; Redator só os próprios | Políticas `Administrator` e `Writer` (os valores dos papéis no banco continuam `Administrador` e `Redator`); checagem de autoria no serviço de aplicação (não só na tela), com a mesma resposta "Você não tem permissão" |
| NFR-14 · Segredos | Nenhum no repositório | Variáveis de ambiente no `web.config` gerado na publicação, a partir de arquivo de transformação fora do git (ADR-011) |
| NFR-15 · Textos digitados | Sempre texto puro | Codificação automática do Razor; proibido `Html.Raw` com texto de usuário; CSP sem `unsafe-inline` para scripts |
| NFR-16 · Acessibilidade | WCAG 2.1 AA | Componentes Bootstrap com ARIA documentada; contratos de acessibilidade no `design-system.md`; axe-core nos testes Playwright |
| NFR-17 · Responsividade | Sem rolagem horizontal em 320/768/1024/1280 px | Grade do Bootstrap, *mobile first*; verificação nas quatro larguras nos testes Playwright |
| NFR-18 · Logs | Correlação; sem senha, token ou e-mail | Serilog em JSON, arquivo rotativo diário (ADR-010); middleware de correlação (`X-Correlation-ID`) enriquece cada linha; filtros que mascaram senha, token e e-mail; o código de referência das telas de erro é o `traceId` |
| NFR-19 · LGPD | Só nome, e-mail e hash da equipe | Modelo de dados sem campos de vendedor ou comprador (§6); fotos sem GPS (ADR-005) |
| NFR-20 · Localização | pt-BR, R$, dd/mm/aaaa | Cultura `pt-BR` fixa; datas gravadas em UTC e exibidas no fuso `America/Sao_Paulo` |
| NFR-21 · SEO (condicional a S7) | Títulos, endereço legível, mapa do site | Endereço `/anuncio/{id}/{slug}`; `<title>` e `meta description` por página; `/sitemap.xml` gerado dos anúncios publicados; arquivado responde 404 e sai do mapa |
| NFR-22 · Pilha e contratos | `tech-stack.md`; ProblemDetails; PagedResult | ASP.NET Core 10, EF Core 10 + Dapper, SQL Server, Razor + Bootstrap; endpoints JSON com ProblemDetails (§8) e `PagedResult` |
| NFR-23 · Componentes de escala | Ainda não | Redis, Kafka, réplica de leitura e CDN rejeitados na v1, com os gatilhos do SPEC (ADR-012). O cache em memória fica dentro dos serviços de leitura, que são o ponto de troca para um cache distribuído na v2 |
| NFR-24 · CEP | 5 s, 1 nova tentativa, manual depois, cache | `ViaCepLookup` no servidor: cada chamada faz 1 tentativa de até 5 s; em falha do serviço responde `503 CEP_SERVICE_UNAVAILABLE` e a tela chama de novo uma vez ("tentativa 2 de 2"), depois abre o preenchimento manual; "CEP não encontrado" responde 404 e não conta como falha; cache **em tabela do banco** por 30 dias (sobrevive à reciclagem do IIS) (ADR-007) |

**Requisitos obrigatórios das regras do projeto** (sem id no SPEC): CORS, limite global de requisições, *health checks* e registro de ações sensíveis estão em §7 e §10; o que o SPEC não pediu e as regras pedem foi registrado como pergunta AR-10 para virar requisito na próxima versão do SPEC.

---

## 5. Contexto e componentes

Os diagramas estão em `architecture/diagrams/`:

- `system-context.md` — atores (Visitante, Redator, Administrador) e sistemas externos (ViaCEP, SendGrid, WhatsApp/telefone do visitante, banco do GazetaOnline só na importação única).
- `container.md` — o site no IIS, o SQL Server, a pasta persistente de fotos, a pasta de logs e os serviços externos.
- `component.md` — os módulos da §3.1 dentro dos três projetos.

**Integrações externas:**

| Sistema | Uso | Como | Falha |
|---|---|---|---|
| ViaCEP | CEP → cidade e UF | HTTP do servidor, `HttpClient` tipado, 5 s por chamada; a tela comanda 1 nova tentativa | Preenchimento manual com selo (US-008-S14) |
| SendGrid | E-mail de redefinição de senha | API HTTP, chave em variável de ambiente (ADR-009) | Mensagem neutra ao usuário; Administrador redefine a senha manualmente (alternativa da S6) |
| WhatsApp / telefone | Contato do visitante | Só links (`https://wa.me/...`, `tel:`) abertos pelo navegador | Nenhuma chamada do servidor |
| Banco do GazetaOnline | Catálogo de veículos | Exportação única, fora do site, gerando script de carga (ADR-008) | Sem dependência em tempo de execução |

---

## 6. Modelo de dados

> Desenho de decisões (chaves, índices, precisão, relacionamentos). As configurações do EF Core (`Configure()`) e as consultas Dapper ficam para o `/build`. Convenções: tabelas no plural em PascalCase, chave `int IDENTITY`, datas em UTC (`datetime2`), colunas de auditoria e `rowversion` nas tabelas editáveis (`rules/overrides/database-sqlserver.md`).

### 6.1 Diagrama de entidades

```text
┌────────────────┐ 1   * ┌────────────────────┐ 1   * ┌──────────────┐
│ Categories     │───────│ Ads                │───────│ AdPhotos     │
│ (até 3 níveis) │       │ (campos comuns +   │       │ (ordem, capa)│
│ ParentId ──┐   │       │  Attributes JSON)  │       └──────────────┘
└────────────┼───┘       └──┬──────────┬──────┘
             └─ auto-rel.   │ *        │ *
                            │ 1        │ 0..1
                  ┌─────────┴──┐   ┌───┴─────────────────────────────────┐
                  │ AspNetUsers│   │ VehicleBrands → VehicleModels →     │
                  │ (equipe)   │   │ VehicleModelYears → VehicleVersions │
                  └────────────┘   └─────────────────────────────────────┘
┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ SiteSettings │  │ CepCache     │  │ Cities       │  │ AuditEntries │
└──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘
```

### 6.2 Tabelas

**Categories** — árvore de até 3 níveis (US-013-S11), carregada inicialmente de `specs/categories.md` com os ids reais.

| Coluna | Tipo | Regra |
|---|---|---|
| `Id` | `int` PK | Ids reais da árvore; categorias novas continuam a sequência |
| `ParentId` | `int` NULL, FK → Categories | Profundidade máxima 3, validada no serviço |
| `Name` | `nvarchar(100)` | Único entre irmãs (`UQ_Categories_ParentId_Name`, filtro para `ParentId` nulo) |
| `Slug` | `varchar(120)` | Gerado do nome; único |
| `DisplayOrder` | `int` | Ordem entre irmãs |
| `IsPostable` | `bit` | Só categorias folha aceitam anúncio |
| `FieldGroup` | `varchar(40)` NULL | Grupo de campos da própria categoria (ADR-002); nulo = herda do ancestral mais próximo |
| `IsSystem` | `bit` | Veio da carga inicial (ver A7, §6.3) |
| auditoria + `RowVersion` | | |

**Ads** — o anúncio.

| Coluna | Tipo | Regra |
|---|---|---|
| `Id` | `int` PK | Aparece no endereço público |
| `CategoryId` | `int` NULL, FK → Categories | Nulo só no rascunho que ainda não escolheu categoria (US-008-S07: rascunho só com o título); o envio para revisão exige uma categoria `IsPostable` (US-009). Emenda de 2026-10-03 |
| `Status` | `tinyint` | 1 Rascunho · 2 Em revisão · 3 Publicado · 4 Rejeitado · 5 Arquivado (S5) |
| `Title` | `nvarchar(120)` | 90 em Vagas; limite por grupo (ADR-002) |
| `Description` | `nvarchar(max)` | 6000 em Serviços e Vagas; nas demais, 5000 (valor do `/arch`, resolve a parte de S13) |
| `PriceCents` | `bigint` NULL | Centavos; **nulo = sem preço** (Serviços), nunca zero; teto 9.999.999.999 (S28) |
| `Cep` | `char(8)` | Só dígitos |
| `City`, `Uf` | `nvarchar(80)`, `char(2)` | Padronizadas (regra da US-008) |
| `LocationManual` | `bit` | Cidade/UF manual por falha do CEP (selo) |
| `Attributes` | `nvarchar(max)` + `CHECK (ISJSON(Attributes)=1)` | Campos do grupo, em JSON (ADR-002) |
| `VehicleBrandId`, `VehicleModelId`, `ModelYear`, `Km`, `AreaM2` | colunas calculadas persistidas a partir do JSON com `TRY_CAST(JSON_VALUE(...))` (`AreaM2` é `decimal(12,2)`; valor malformado vira `NULL`, ADR-002) | Só as usadas em filtro; indexadas |
| `TitleSearch`, `DescriptionSearch` | `nvarchar(200)`, `nvarchar(max)` | Título e descrição sem acento e em minúsculas, para a busca; preenchidos só pela aplicação (ADR-006) |
| `AuthorId` | FK → AspNetUsers | Dono do anúncio (S10) |
| `SentAt`, `PublishedAt`, `PublishedById`, `RejectedAt`, `RejectedById`, `RejectionReason`, `ArchivedAt`, `ArchivedById` | | Rastro de decisão (S10); motivo visível ao autor (S9); quem arquivou (migration `AddArchivedBy`, US-011) |
| auditoria (`CreatedAt/By`, `UpdatedAt/By`) + `RowVersion` | | Concorrência: dois administradores decidindo o mesmo anúncio → o segundo recebe a mensagem "por outro administrador" (302 de volta à pré-visualização) e nada é gravado duas vezes (US-010); edição concorrente → `409` |

**AdPhotos** — `Id`, `AdId`, `SortOrder` (0 = capa), `StorageKey` (nome do arquivo, gerado — nunca o nome enviado), `Width`, `Height`, `SizeBytes`, `OriginalKey` (caminho do original em `_originals/`; **nulo depois da limpeza de 30 dias**), `CreatedAt`.

**Catálogo de veículos** (ADR-008) — `VehicleBrands`, `VehicleModels`, `VehicleModelYears`, `VehicleVersions`. Chave primária e estrangeiras **compostas com `Kind`** (`car`/`moto`, porque os ids de carros e de motos podem coincidir); `VehicleModelYears` é tabela própria (há anos sem versões); `Source` registra a origem dos dados (A5). Ids vêm da origem, nunca são gerados.

**Demais tabelas**

| Tabela | Uso |
|---|---|
| `AspNetUsers` e tabelas do Identity | Equipe: + `FullName`, `IsActive`, `MustChangePassword` (S6, S17). **Chave `int IDENTITY`** (`AppUser : IdentityUser<int>`, `AppRole : IdentityRole<int>`), como todas as demais tabelas; `CreatedBy`, `UpdatedBy` e `ActorId` das auditorias são `int` nulo (nulo = ação do sistema) |
| `SiteSettings` | Chave-valor (`Key` única, `Value`): hoje só `site.phone`, o telefone/WhatsApp do site (US-015), guardado só com dígitos; pronta para novas chaves |
| `CepCache` | `Cep` `char(8)` PK, `City`, `Uf`, `IbgeCode` (nulo se o ViaCEP não informar), `FetchedAt`; validade 30 dias (NFR-24). Só CEP encontrado entra. **Só cidade, UF e código do IBGE:** rua e bairro nunca são lidos nem guardados (NFR-19, S25). Sem `RowVersion` nem auditoria |
| `Cities` | Lista oficial de municípios (IBGE): `IbgeCode` `int` PK (vem do IBGE), `Name`, `Uf` `char(2)`, `NameSearch` (normalizado pelo `Normalizer`); índice único `(Uf, NameSearch)`. Carregada por script (`tools/CitiesImport`), nunca pelo site; sem `RowVersion`. Serve ao preenchimento manual (`GET /api/v1/cities?uf=`) e à padronização do nome. Uma UF sem carga mostra campo de texto e vale a regra de padronização da SPEC |
| `AuditEntries` | Quem fez o quê e quando: publicar, rejeitar, despublicar, arquivar, mudar categoria, criar ou desativar usuário (`principles-and-practices.md` §4.6) |

### 6.3 Resolução da A7 (herança de campos, exclusão, filtros)

**Decisão aprovada pelo Product Owner em 2026-09-30** (A7 resolvida no SPEC):

- **(a) Herança:** cada categoria tem um grupo de campos **próprio ou herdado do ancestral mais próximo** que tenha um. "Autopeças" e suas filhas recebem o grupo **Peças**; carros e motos, os grupos **Carros** e **Motos**. Toda subcategoria nova herda o grupo do pai, a menos que o Administrador escolha outro — o que **não** está no SPEC e fica fora da v1 (o grupo não é editável pela tela).
- **(b) Exclusão:** o bloqueio "categoria com campos específicos não pode ser excluída" passa a valer para **categorias da carga inicial que definem o próprio grupo** (`IsSystem = 1` e `FieldGroup` não nulo). As regras "sem subcategorias" e "sem anúncios" continuam valendo para todas.
- **(c) Filtros:** os filtros específicos da busca vêm do **grupo da categoria escolhida**: marca/modelo/ano/km só para os grupos Carros e Motos (e ano/km para Caminhões e Ônibus); área para Imóveis e Terrenos; nenhum filtro específico para Peças e Produtos em geral na v1.

### 6.4 Índices

| Índice | Por quê |
|---|---|
| `IX_Ads_Status_CategoryId_PublishedAt` | Listas públicas por categoria, mais recentes primeiro |
| `IX_Ads_Status_PublishedAt_Id` (`PublishedAt` e `Id` em ordem decrescente) | Os 12 anúncios mais recentes da página inicial, já na ordem pedida (sem varrer a tabela nem ordenar à parte; medido na 5.1) |
| `IX_Ads_Status_Uf_City` | Filtro por UF e cidade |
| `IX_Ads_Status_PriceCents` (filtrado `PriceCents IS NOT NULL`) | Ordenação e faixa de preço, sem Serviços (A6) |
| `IX_Ads_VehicleBrandId_ModelYear`, `IX_Ads_Km`, `IX_Ads_AreaM2` | Filtros de veículo e de terreno |
| `IX_Ads_AuthorId_Status_UpdatedAt` | Lista do painel |
| `IX_AdPhotos_AdId_SortOrder` | Galeria na ordem |
| FKs | Todas indexadas |

### 6.5 Acesso a dados (modelo híbrido, ADR-004)

**Em resumo:** o EF Core escreve e cuida do esquema; o Dapper lê o que é complexo e escreve só com justificativa comentada no código.

| Operação | Ferramenta | Onde na v1 |
|---|---|---|
| Escrita comum, regras, auditoria, concorrência (`rowversion`) | EF Core | Anúncios, categorias, equipe, fotos, configurações |
| Esquema e evolução | EF Core (migrations por script idempotente) | Todas as tabelas |
| Leitura complexa (junções, filtros dinâmicos, listas paginadas com ordenação variável) | **Dapper** | **Busca (tarefa 5.4)** e **lista do painel (tarefa 4.4)** |
| Leitura simples (por id, lista curta, projeção de uma tabela) | EF Core (`AsNoTracking` + projeção) | Página inicial, categoria, detalhe, favoritos por ids |
| Escrita em lote ou com SQL específico | **Dapper**, com comentário do motivo | Carga do catálogo de veículos em bancos de desenvolvimento e de teste (tarefa 2.5) |
| Manutenção em lote | EF Core (`ExecuteUpdateAsync`); Dapper só se o volume medido justificar | Limpeza dos originais de foto (tarefa 3.6) |

- **Estrutura:** escritas EF nos repositórios e serviços sobre o `AppDbContext`; leituras Dapper em *read repositories* (interfaces no Core, implementações na Infrastructure) sobre `IDbConnection`; escritas Dapper em métodos específicos, cada um com o comentário `// Dapper: <motivo>`.
- **Cuidados do Dapper:** não herda filtros do EF, então o "só publicados" vem de um fragmento de SQL compartilhado e cada consulta pública tem teste com anúncio em cada situação; ordenação só por colunas de uma lista permitida; valores sempre como parâmetros; escrita em tabela com `RowVersion` confere a versão no `WHERE`.
- **Testes:** consultas Dapper só são provadas com SQL Server real (TestContainers, no `/test`); no `/build`, as telas usam *read repositories* falsos.

---

## 7. Considerações de segurança

| Controle | Mecanismo |
|---|---|
| Autenticação | ASP.NET Core Identity com cookie (`HttpOnly`, `Secure`, `SameSite=Lax`), expiração deslizante de 30 min (ADR-003) |
| Primeiro Administrador (S17) | Criado na primeira inicialização a partir das variáveis `Bootstrap__AdminEmail` e `Bootstrap__AdminPassword`, **só se não existir nenhum Administrador**, com troca de senha obrigatória no primeiro acesso; as variáveis são removidas depois (ADR-011) |
| Autorização e IDOR | Políticas por papel na área `Panel`; os serviços de aplicação conferem a autoria (Redator só os próprios anúncios em Rascunho/Rejeitado); anúncio não publicado no site público responde a mesma página "não está mais disponível" (US-003-S06) |
| Cabeçalhos HTTP | Middleware: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy` restritiva, `Content-Security-Policy: default-src 'self'; img-src 'self' data:; script-src 'self'; style-src 'self'; frame-ancestors 'none'; form-action 'self'`; HSTS (1 ano) só em produção |
| CORS | **Nenhuma política CORS**: site e endpoints JSON são da mesma origem; pedidos de outras origens são recusados pelo navegador |
| Antiforgery | Global no MVC; cabeçalho `RequestVerificationToken` nos endpoints JSON de escrita |
| Limite de requisições | Login e "Esqueci minha senha": 5 por 15 min por IP; global: 100 por minuto por IP no site público (`rules/security.md`) |
| Segredos | Variáveis de ambiente no `web.config` publicado, geradas por transformação fora do git; nada no `appsettings.json` (ADR-011) |
| Compressão das respostas (BREACH) | Brotli e gzip em tudo que **não** é o painel (`/painel`): site público, API e arquivos estáticos (estes já saem pré-comprimidos). **O painel nunca é comprimido**: o HTML dele carrega o token antiforgery (`<meta name="request-verification-token">` e `__RequestVerificationToken`), e comprimir uma resposta que mistura um segredo com texto que o atacante consegue refletir, por HTTPS, deixa adivinhá-lo byte a byte (ataque BREACH). Nenhuma página pública leva o token. Provado por `CompressionAndCacheTests` (decisão do Product Owner, 2026-10-06, tarefa 6.3) |
| Cache dos estáticos | Arquivo estático pedido com `?v=hash` (o `asp-append-version` das views) sai com `Cache-Control: public, max-age=31536000, immutable`; sem `v` continua `no-cache` (revalida). O endereço muda quando o conteúdo muda, então o cache longo é seguro. As fotos já saem `public, max-age=31536000, immutable` (nome gerado, ADR-005) |
| BREACH em página de erro reexecutada | Uma resposta de erro do painel (404 sem corpo, falha) é reexecutada numa página pública (`/Home/Status/{código}`, `/Home/Error`) e o caminho do pedido muda. A regra "o painel nunca é comprimido" olha, além do caminho atual, o **caminho do pedido original** (`IStatusCodeReExecuteFeature.OriginalPath` e `IExceptionHandlerPathFeature.Path`): sem isso a página de erro de quem está logado saía comprimida. Provado por `CompressionAndCacheTests` (404 reexecutado no site e falha lançada num servidor mínimo com a mesma ordem do `Program.cs`) |
| Páginas de status (404 e outros) | `UseStatusCodePagesWithReExecute("/Home/Status/{0}")` dentro de um `UseWhen` que **exclui** `/api`, `/health` e endereços com extensão de arquivo; a página (`Views/Home/Status.cshtml`, `HomeController.Status`) mostra o texto em português e mantém o status da resposta (404 segue 404). Falhas lançadas continuam no `UseExceptionHandler("/Home/Error")`. Critério na SPEC: NFR-25; provado por `StatusPagesTests` |
| Versão dos scripts de página | O `?v=` de um script de página (`<script type="module" asp-module-version="~/js/pages/x.js">`, `ModuleScriptTagHelper`) é o hash da página **e de todos os módulos que ela importa** (`ModuleVersions`: segue `import`, `export … from` e `import()` relativos). O `asp-append-version` olhava só o arquivo da página, e mudar apenas um módulo compartilhado (`api.js`, `favorites-ui.js`) deixava o navegador, com cache de um ano, juntar a página antiga com o módulo novo. Os módulos importados saem sem `?v=` e continuam revalidando. Provado por `ModuleVersionsTests` (inclusive contra os arquivos reais de `wwwroot/js`) |
| Transporte | HTTPS obrigatório (`UseHttpsRedirection` + HSTS); certificado do SmarterASP (OQ AR-09) |
| Fotos | Validação pelo conteúdo; reprocessamento remove metadados (GPS, S18) das versões publicadas; nome de arquivo gerado; pasta fora da raiz do site, servida por controller que confere a situação do anúncio. **`_originals/` nunca é servida** (os originais ainda têm GPS até serem apagados em 30 dias) |
| SSRF | Única chamada de saída com dado do usuário é o CEP: validado como 8 dígitos e montado sobre a URL fixa do ViaCEP |
| Chaves de proteção de dados | Chaves do Data Protection (cookies, antiforgery, links de redefinição de senha) persistidas em pasta fora da raiz do site, senão o IIS as perde a cada reciclagem e todos são deslogados (OQ AR-01). **Ligado no `Program.cs`** (`AddDataProtection` com `SetApplicationName("GazetaMarketplace")` e o anel de chaves em `DataProtection__KeysDirectory`, lido na hora de montar o anel); a pasta é obrigatória em Production. Esteve só validada e ignorada até o `/review` formal (R-01); provado por `DataProtectionKeysTests` (as chaves são gravadas na pasta e um host novo lê o que o anterior protegeu). **Nota:** as chaves ficam em arquivos XML sem criptografia em repouso; a pasta precisa ter permissão só para a identidade do site, e protegê-las com DPAPI (`ProtectKeysWithDpapi`, só Windows) é uma melhoria registrada no BACKLOG |
| Criptografia em repouso | Não há dado pessoal além do e-mail da equipe; depende do SQL Server do provedor (OQ AR-03) |

Modelo de ameaças detalhado (STRIDE): `/secure`.

---

## 8. Contrato de códigos de erro (endpoints JSON)

As páginas Razor mostram as mensagens do SPEC na própria tela; os endpoints JSON respondem ProblemDetails (RFC 7807) com estes códigos. A lista é **exaustiva** para a v1: código novo exige ADR.

| `code` | HTTP | Exceção | Quando | História |
|---|---|---|---|---|
| `VALIDATION_ERROR` | 400 | `ValidationException` | Dados inválidos; foto com formato ou tamanho não aceito; `errors` traz a mensagem por campo | US-008, US-002 |
| `UNAUTHORIZED` | 401 | `UnauthorizedException` | Sessão ausente ou vencida em endpoint do painel | US-006 |
| `FORBIDDEN` | 403 | `ForbiddenException` | Papel ou autoria insuficiente | US-008-S10, NFR-13 |
| `NOT_FOUND` | 404 | `NotFoundException` | Recurso inexistente; CEP não encontrado; anúncio não publicado pedido pelo público | US-003-S06, US-008 |
| `CONFLICT` | 409 | `ConflictException` | Edição concorrente (`rowversion`) do formulário e do envio; nome de categoria repetido; limite de fotos atingido (a decisão simultânea da revisão e da retirada nas páginas HTML responde 302 com a mensagem) | US-010, US-013-S06, US-008-S04 |
| `RATE_LIMITED` | 429 | (limitador) | Excesso de tentativas | NFR-06 |
| `CEP_SERVICE_UNAVAILABLE` | 503 | `ServiceUnavailableException` | ViaCEP sem resposta depois da nova tentativa | US-008-S14 |
| `INTERNAL_ERROR` | 500 | qualquer outra | Erro não tratado; pilha só no log | todas |

`traceId` sempre presente (vem do `X-Correlation-ID`) e é o "código de referência" mostrado nas telas de erro.

---

## 9. Deploy e configuração

**Em resumo:** publicação pelo Visual Studio com WebDeploy para o IIS do SmarterASP.NET. O repositório não guarda segredos; os valores de produção entram no `web.config` publicado por um arquivo de transformação que fica **só na máquina de quem publica**. Fotos, logs e chaves ficam em pastas **fora da raiz do site**, que o WebDeploy não toca. Detalhes e alternativas: ADR-011.

| Tema | Decisão |
|---|---|
| `appsettings.json` (no git) | Só valores não sensíveis e padrões de desenvolvimento |
| `appsettings.Development.json` | Fora do git (`.gitignore`); valores locais. **Hoje ele está versionado:** a primeira execução do `/build` deve tirá-lo do git (`git rm --cached`) e incluí-lo no `.gitignore` (ADR-011) |
| Produção | Variáveis de ambiente na seção `<aspNetCore><environmentVariables>` do `web.config`, com `ASPNETCORE_ENVIRONMENT=Production`. Elas valem mais que o `appsettings.json` (ordem padrão do ASP.NET Core) |
| Transformação | `web.Production.config` **fora do git**, aplicado pelo perfil de publicação; se o painel do SmarterASP permitir variáveis por site, essa opção substitui a transformação |
| Perfil de publicação (`.pubxml`) | `<SkipExtraFilesOnServer>true</SkipExtraFilesOnServer>` (equivale a desmarcar "Remover arquivos adicionais no destino"); `MSDeploySkipRules` para qualquer pasta de dados que precise ficar dentro do site; exclusão de `appsettings.Development.json`; `EnvironmentName=Production`; `*.pubxml.user` fora do git |
| Pasta de fotos | Fora da raiz do site (`PhotoStorage__BasePath`, por exemplo `h:\root\home\<conta>\www\gazeta-fotos`); caminho e permissão de escrita a confirmar (AR-01). Estrutura: `<adId>/<photoId>_1600.webp` e `<adId>/<photoId>_480.webp` (permanentes) e `_originals/<yyyy-MM>/<guid>.<ext>` (30 dias) |
| Logs e chaves | `Logging__FileDirectory` e `DataProtection__KeysDirectory`, também fora da raiz |
| Sessão do banco | As colunas calculadas persistidas e indexadas de `Ads` exigem `ARITHABORT ON` e `QUOTED_IDENTIFIER ON` nas escritas (padrão do driver .NET). **Checklist de publicação:** confirmar que o servidor de produção usa esses dois ajustes; o script roda com `sqlcmd -I` |
| Banco | Migrations aplicadas por **script idempotente** gerado pelo EF (`dotnet ef migrations script --idempotent`), executado na ferramenta de SQL do provedor; nunca `Database.Migrate()` na inicialização em produção |
| Runtime | *Framework-dependent* se o .NET 10 estiver instalado no servidor; senão *self-contained* `win-x64` (AR-02) |
| Docker | Só no desenvolvimento (SQL Server local e testes com TestContainers); não usado em produção |

**Variáveis de ambiente de produção**

| Variável | Conteúdo |
|---|---|
| `ConnectionStrings__DefaultConnection` | Banco do SmarterASP |
| `PhotoStorage__BasePath` | Pasta persistente de fotos |
| `DataProtection__KeysDirectory` | Pasta das chaves |
| `Logging__FileDirectory` | Pasta dos logs |
| `SendGrid__ApiKey`, `SendGrid__FromEmail` | E-mail de redefinição |
| `Bootstrap__AdminEmail`, `Bootstrap__AdminPassword` | Só na primeira publicação |

---

## 10. Observabilidade

**Decisão (ADR-010, desvio registrado de `rules/monitoring.md`):** em hospedagem compartilhada não há onde rodar Prometheus, Grafana nem Jaeger.

- **Logs:** Serilog, JSON, arquivo rotativo diário com retenção de 14 dias, fora da raiz do site; nível `Information` (Microsoft em `Warning`); correlação por requisição; máscara de senha, token e e-mail.
- **Saúde:** `/health/live` (processo) e `/health/ready` (banco acessível e última migration aplicada); sem detalhes internos na resposta pública.
- **Métricas e rastreamento:** adiados para a v2 (gatilho no ADR-010).
- **Registro de ações sensíveis:** tabela `AuditEntries` (§6.2).
- **Limpeza dos originais:** `OriginalsCleanupService` (`BackgroundService`) apaga os arquivos de `_originals/` com mais de 30 dias e registra no log cada arquivo apagado. Como o IIS compartilhado **para o processo quando o site fica ocioso**, o serviço não depende de um horário fixo: roda **ao iniciar o site e depois a cada 24 horas** enquanto o processo estiver vivo, e apaga tudo o que já passou de 30 dias. Um atraso não perde nada; só adia a limpeza. Gatilho para trocar por um agendador dedicado (Hangfire): ADR-012.

---

## 11. Decisões de rotina (sem ADR)

| Tema | Valor | Por quê |
|---|---|---|
| Página inicial / busca / painel | 12 / 24 / 20 itens | SPEC (S11) |
| Tempo limite ViaCEP | 5 s, 1 nova tentativa | NFR-24 |
| Validade do cache de CEP | 30 dias | Proposta do SPEC; CEPs mudam raramente |
| Versões das fotos | 1.600 px e 480 px de largura, WebP qualidade 80 | Detalhe nítido e lista leve (NFR-05) |
| Retenção do original | 30 dias em `_originals/`, depois apagado | Permite reprocessar (novo tamanho, novo formato, correção de erro) sem ocupar disco para sempre; decisão do Product Owner |
| Descrição (exceto Serviços e Vagas) | até 5.000 caracteres | Resolve a parte aberta de S13; suficiente para anúncios detalhados |
| Título (exceto Vagas) | até 120 caracteres | Já usado no protótipo aprovado; Vagas 90 |
| Cache de memória | Árvore de categorias e catálogo de veículos: 10 min e invalidação ao editar | Dados pequenos e quase estáticos |
| Retenção de logs | 14 dias | Espaço limitado na hospedagem (AR-04) |
| Fuso para exibição | `America/Sao_Paulo` | NFR-20 |

---

## 12. Fluxos: diagrama ou dispensa

| Fluxo candidato | Origem | Destino |
|---|---|---|
| Equipe: anúncio até a publicação | `specs/wireframes/flows/equipe-anuncio-publicacao.md` | `diagrams/sequence/anuncio-publicacao.md` |
| Equipe: login e recuperação de senha | `specs/wireframes/flows/equipe-login-recuperacao-senha.md` | `diagrams/sequence/login-recuperacao-senha.md` (inclui SendGrid) |
| Visitante: favoritos | `specs/wireframes/flows/visitante-favoritos.md` | `diagrams/sequence/favoritos.md` |
| Visitante: início até o contato | `specs/wireframes/flows/visitante-home-contato.md` | Dispensado — caminho linear controller → serviço → banco renderizado no servidor; o contato é só link do navegador. O diagrama de componentes basta |
| ViaCEP (dependência externa) | container | `diagrams/sequence/consulta-cep.md` (com falhas e cache) |
| SendGrid (dependência externa) | container | Coberto em `login-recuperacao-senha.md` |
| Pasta de fotos + processamento | container | `diagrams/sequence/envio-foto.md` (validação, conversão, falhas) |
| Banco do GazetaOnline (importação única) | container | `diagrams/sequence/importacao-catalogo.md` |
| WhatsApp / telefone | container | Dispensado — só links abertos pelo navegador, sem chamada do servidor |


---

## 13. Perguntas em aberto

**Novas, levantadas pelo `/arch`**

| Id | Pergunta | Bloqueia | Dono |
|---|---|---|---|
| AR-01 | Caminho exato de uma pasta fora da raiz do site no SmarterASP (fotos, logs, chaves) e permissão de escrita da identidade do site | `/infra` e primeira publicação | Product Owner com o SmarterASP |
| AR-02 | O servidor tem o runtime do .NET 10 (RC)? Se não, publicar *self-contained* `win-x64` | `/infra` | Product Owner com o SmarterASP |
| AR-03 | Versão do SQL Server oferecida (o Profile pede 2022+), acesso para rodar scripts de migration e política de backup | `/plan` (compatibilidade de recursos) | Product Owner com o SmarterASP |
| AR-04 | Espaço em disco do plano. Estimativa: até ~2 GB de fotos WebP na escala da v1 (200 anúncios × 20 fotos × ~0,5 MB nas duas versões), mais os originais dos últimos 30 dias (até 10 MB cada) | `/infra` | Product Owner |
| AR-05 | A biblioteca de imagem com suporte a HEIC (Magick.NET, licença Apache 2.0) usa componentes nativos; confirmar que roda na hospedagem compartilhada | `/build` | Arquiteto (prova no ambiente) — **parcial (2026-10-04):** `Magick.NET-Q8-x64` 14.17.2 lê HEIC e escreve WebP em Linux (testes da 3.4); a prova na hospedagem Windows compartilhada segue pendente |
| AR-06 | Aprovar os pacotes novos: `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Serilog.AspNetCore`, `Serilog.Sinks.File`, `Serilog.Formatting.Compact`, Magick.NET (Q8, Windows x64), FluentValidation; **Dapper 2.1.89 já aprovado pelo Product Owner em 2026-09-30** (ADR-004). O SendGrid é chamado por `HttpClient`, sem SDK (ADR-009) | `/build` | Product Owner |
| AR-07 | ~~Aprovar a resolução da A7 (§6.3)~~ **Resolvida em 2026-09-30:** aprovada pelo Product Owner e registrada na A7 do SPEC | — | — |
| AR-08 | ~~Aprovar os valores de rotina (§11)~~ **Resolvida em 2026-09-30:** descrição 5.000, cache de CEP no banco por 30 dias, duas versões WebP e retenção do original por 30 dias aprovados | — | — |
| AR-09 | Certificado HTTPS no SmarterASP (gratuito ou pago) — necessário para HSTS e cookies `Secure` | primeira publicação | Product Owner |
| AR-11 | ~~A redefinição de senha pelo Administrador não tinha história no SPEC~~ **Resolvida em 2026-09-30:** SPEC v1.2, cenário US-014-S10 (senha provisória digitada pelo Administrador, troca obrigatória no próximo acesso, sessões encerradas, registro em `AuditEntries`) | — | — |
| AR-12 | E-mail do SendGrid pronto para produção. Antes do lançamento: (1) definir o domínio remetente (por exemplo, `noreply@gazetamarketplace.com.br`); (2) configurar SPF e DKIM no DNS desse domínio; (3) validar o domínio no SendGrid; (4) testar a entrega e confirmar que não cai em spam | Lançamento | Product Owner |
| DS-01 a DS-03 | Paleta, fonte e logotipo da Gazeta (`design-system.md` §9); até lá, padrão do Bootstrap com três ajustes de acessibilidade | Lançamento | Product Owner com a Gazeta |
| SEC-01 | Como o SmarterASP encaminha o IP do cliente, para o limitador de tentativas valer por visitante (RC-10). **Assumido (PO, 2026-09-30):** `X-Forwarded-For` com `UseForwardedHeaders` (`XForwardedFor`, `XForwardedProto`); `KnownProxies` a definir depois da resposta do SmarterASP (ticket aberto). Fonte: `/secure` | **Lançamento** | Product Owner com o SmarterASP |
| SEC-02 | ~~Pedir a senha atual do Administrador em ações de alto impacto~~ **Decidido em 2026-09-30:** não exigir (mudaria cenários do SPEC; ganho marginal num MVP); risco residual RR-1 aceito. Fonte: `/secure` | — | — |
| SEC-03 | O banco do provedor tem conta separada de leitura e escrita para o site? **Assumido o pior caso (PO, 2026-09-30):** conta única com permissão total (RR-9); ticket aberto; reavaliar no `/infra`. Fonte: `/secure` | `/infra` | Product Owner com o SmarterASP |
| AR-10 | Pendências para a **próxima versão do SPEC** (decisão do Product Owner em 2026-09-30: não abrir uma versão só para isso; entram quando o SPEC for revisto por outro motivo): (1) registrar os requisitos que as regras do projeto exigem e o SPEC não cita — CORS, limite global de requisições, *health checks*, registro de ações sensíveis; (2) reescrever as regras de negócio da US-002 (filtros e Serviços sem preço) e da US-013 (herança de campos e exclusão) conforme a A6 e a A7 resolvidas, que hoje prevalecem sobre esse texto | próxima versão do SPEC | Product Owner |

**Trazidas do SPEC**

| Id | Situação após o `/arch` | Bloqueia |
|---|---|---|
| A2 | Sem impacto técnico na v1: a comissão é cobrada fora do sistema (D-02, D-06) | Decisão de negócio (não bloqueia) |
| A3 | **Resolvida no ADR-008** (importação única com origem registrada) | — |
| A4 | **Resolvida:** bloco neutro "Vaga de emprego" no lugar da foto (`design-system.md` §5.2 e §5.4) | — |
| A5 | Continua aberta: parecer jurídico sobre o catálogo vindo da OLX; o ADR-008 permite trocar a fonte | **Lançamento** |
| A6 | **Resolvida:** preço nulo, fora da faixa de preço e no fim das ordenações (ADR-006); Tipo do serviço no lugar do preço (`design-system.md` §5.3) | — |
| A7 | **Resolvida:** §6.3, aprovada pelo Product Owner | — |
| S6 | **Resolvida:** SendGrid (ADR-009); alternativa de redefinição pelo Administrador na US-014-S10 (SPEC v1.2) | — |
| S16, S19 | Sessão 30 min adotada; disponibilidade e backup dependem do provedor (AR-03) | `/infra` |
| S17 | **Resolvida:** Administrador inicial por variáveis de ambiente (§7) | — |
| S18 | Parcial: GPS removido de toda foto; cobrir placas e rostos segue com o Product Owner | Não bloqueia |
| S28 | **Resolvida:** preço em centavos, `bigint`, nulo quando não há preço | — |
| S1, S2, S5, S7–S15, S20–S25, S27, S29 | Assumidas pelo SPEC e seguidas por esta arquitetura; confirmação do Product Owner não muda o desenho | Não bloqueiam |

---

## 14. ADRs

| ADR | Decisão |
|---|---|
| ADR-001 | Monólito modular com Clean Architecture em 3 projetos |
| ADR-002 | Campos por categoria em JSON com colunas calculadas e grupos de campos em código |
| ADR-003 | ASP.NET Core Identity com cookie |
| ADR-004 | Persistência híbrida: EF Core 10 (escrita e migrations) + Dapper (leitura complexa e escrita justificada), `int IDENTITY`, `rowversion`, preço em centavos |
| ADR-005 | Fotos: `IPhotoStorage` em pasta fora da raiz, reprocessadas para WebP, original retido 30 dias com limpeza diária |
| ADR-006 | Busca por texto normalizado e filtros por grupo de campos |
| ADR-007 | Consulta de CEP no servidor com cache no banco |
| ADR-008 | Catálogo de veículos importado com origem registrada |
| ADR-009 | E-mail pelo SendGrid |
| ADR-010 | Observabilidade em hospedagem compartilhada |
| ADR-011 | Configuração, segredos e publicação por WebDeploy |
| ADR-012 | Componentes rejeitados na v1 |

