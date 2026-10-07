# Code Review — Fase 5: site público (tarefas 5.1 a 5.6)

**Data**: 2026-10-06
**Revisor**: agente Code Reviewer (Five-Axis Framework)
**Escopo**: `git diff 731522d..HEAD` na branch `claude/admiring-cray-wrjfmg` (27 commits, de c3e7923 a d53e89b), 135 arquivos: 83 em `src/` (+6.855 / -77, dos quais 2.689 linhas são o `Designer` da migration) e 43 em `tests/` (+7.952 / -19); mais `plans/`, `reports/`, `docs/`, `db/` e `architecture/`.
**Inputs**: specs/SPEC.md (US-001 a US-005, US-011-S04, NFR-04, NFR-13, NFR-21) · architecture/adr (ADR-004, ADR-005, ADR-006) e ARCHITECTURE.md · plans/plan.md (blocos 5.1 a 5.6 e Checkpoint 5) · plans/todo.md · plans/BACKLOG.md (linhas 196 a 278) · security/PRE_DEV_REVIEW.md e THREAT_MODEL.md (T2, T3, I6, D2, D3, RC-15, RC-17) · reports/TEST_REPORT.md (seções 5.1 a 5.6; não existe ainda a seção do Checkpoint 5)
**Método**: leitura do código, dos testes e dos relatórios. Conforme a orientação recebida, **não** foram executados `dotnet build`, `dotnet test` nem `dotnet run` (há um ciclo de E2E usando os binários) e nenhum arquivo de `src/` ou `tests/` foi alterado. O veredito do Gate 6 vem de `reports/TEST_REPORT.md`. O achado 🟡 1 vem da leitura da semântica de `System.Decimal` e **não foi reproduzido em execução**.

## 1. Executive Summary

**Veredito: APPROVE com condições.** A Fase 5 entrega o site público que a SPEC pede e a superfície sem login está bem fechada: só anúncio **Publicado** sai em toda leitura (vitrine, busca, detalhe, favoritos, mapa do site e fotos), nenhum dado do autor chega à tela, todo SQL usa parâmetros e as ordens vêm de um `switch` fixo, os limites de DoS existem (100 ids, termo de 100 caracteres e 5 palavras, página máxima, tempo limite de 10 s), o texto digitado sai codificado em páginas, metas e fragmento de favoritos, e uma bateria forte de testes (73 mutações, prova no SQL Server real, E2E com e sem JavaScript, axe) sustenta o que o código afirma. Não há defeito que cause dano ou vazamento (nenhum 🔴). Há **5 avisos 🟡**: um número de 27 a 29 dígitos num filtro de preço derruba a busca em 503 (estouro de `decimal`), a cobertura numérica da fase nunca foi medida e o Checkpoint 5 não tem seção no relatório de testes, quatro decisões do Product Owner que mudam comportamento ainda não voltaram à SPEC, o caminho de mais de 100 favoritos (lotes) não tem teste, e a regra de ids dos favoritos existe em JavaScript e em C# sem tabela de paridade (e já difere no id 0).

🔴 0 · 🟡 5 · 🟢 23 · ✅ 13

## 2. Five-Axis Scores

| # | Axis | Score (1–5) | One-line justification |
|---|------|-------------|------------------------|
| 1 | Correctness | 3 | Todos os cenários da fase têm caminho ligado e teste com efeito observável, mas cinco avisos 🟡 se concentram aqui: o estouro de `decimal` na busca (🟡 1), a cobertura e o relatório do Checkpoint 5 (🟡 2), a SPEC sem as decisões aprovadas (🟡 3), os lotes de favoritos sem teste (🟡 4) e a paridade `limpar` × `FavoriteIds` (🟡 5). |
| 2 | Readability | 4 | Comentários de motivo, constantes nomeadas e mensagens centralizadas são claros; ficam os 🟢 9 (`PrepareAsync` com 137 linhas), 10, 13 (comentários e ADR desatualizados), 15 e 16 (duplicações) e 24 (nomes de CSS e JS em português), que impedem o 5. |
| 3 | Architecture | 4 | Camadas respeitadas (interfaces no Core, Dapper e EF na Infrastructure, controllers finos), um só `AdCardSql` e uma só `AdDetailFactory` para a vitrine, a busca e a pré-visualização; os 🟢 11, 12, 22 e 23 apontam duplicação de constantes, arquivos com vários tipos, API sem cliente com contrato e diagrama defasados, e a leitura de fotos sem checagem própria. |
| 4 | Security | 4 | NFR-13 e T2/T3/D2 verificados no código: filtro "só publicado" em toda leitura, parâmetros e listas permitidas, limites de entrada, 404 igual, XSS e `noopener`; sem 🟡 de segurança. Ficam os 🟢 6 (arquivado revela a categoria), 7 (cabeçalho Host fora de Production), 8 (log de erro por entrada inválida) e 23. |
| 5 | Performance | 4 | Dapper com `OUTER APPLY`, índice novo provado por plano de execução, p95 de 6 ms (200 anúncios) e 52 ms (6.000 cadastrados), sem N+1; ficam os 🟢 19 (parâmetro `nvarchar` contra `char(2)`), 20 (cidades e opções de categoria a cada busca) e 21 (mapa do site sem cache nem gatilho). |

## 3. Findings (by severity: 🔴 → 🟡 → 🟢 → ✅)

### 🔴 Critical

Nenhum.

### 🟡 Warning

1. **`src/GazetaMarketplace.Core/Search/DecimalInput.cs:63` (com `Core/Search/SearchService.cs:247` e `Web/Controllers/SearchController.cs:30-37`) — um número de 27 a 29 dígitos num filtro de preço derruba a busca em 503.** `TryParseCents` lê o texto com `TryParse` (aceita até 29 dígitos, o teto de `decimal`) e depois calcula `reais * 100m`. A multiplicação de `decimal` é sempre verificada e lança `OverflowException` quando passa de 7,9 × 10^28, ou seja, a partir de um valor na casa de 7,9 × 10^26 (27 dígitos). Exemplo: `/busca?precoMax=1000000000000000000000000000`. A documentação da classe diz "Nunca lança" e a exceção não é tratada em `PrepareAsync`: o `catch` genérico de `SearchController.Index` responde 503 "Não foi possível buscar agora" com um `LogError` e pilha por pedido. Qualquer visitante provoca isso pela barra de endereço (o limite de 100 pedidos por minuto por IP contém o volume, mas não o ruído de erro nem a falha de uma página que deveria mostrar "valor ilegível"). Os testes cobrem `100000000` e `99999999,995` (`DecimalInputTests.cs:68-86`, `SearchRulesTests.cs:216-228`), nenhum com 27 dígitos ou mais; o JavaScript (`lerNumero`, `search.js:16-22`) não tem o problema porque só compara números. — **Recomendação:** recusar antes de multiplicar (`if (reais > maxCents / 100m) return false;`), acrescentar `1000000000000000000000000000` e `79228162514264337593543950335` a `Centavos_AcimaDoTetoOuIlegivel_Recusa` e a `Preco_Ilegivel_MostraErroNoCampoEFicaSemEleNaLista`, e uma linha equivalente à tabela de paridade. — *Relates-to: US-002-S11, RC-15, D2, Task 5.4*

2. **`reports/TEST_REPORT.md:949-981` e `:983-1327` (com `plans/todo.md:68`) — a cobertura numérica da Fase 5 nunca foi medida e o Checkpoint 5 não tem seção no relatório de testes.** A única medição (97,7% de linhas, 90,9% de ramos, 4.998 linhas) é de antes da fase; as seções 5.1 a 5.6 trazem totais de testes e mutações, mas nenhum número de cobertura nem a lista de métodos com 0%, que o Gate 6 de `rules/testing.md` exige (em greenfield, repositório inteiro: linha ≥ 80%, ramo ≥ 75%). O commit d53e89b (`Checkpoint5PublicTests`, `Checkpoint5ServicesJobsTests`, `Checkpoint5E2ETests`) criou os testes do Checkpoint 5, mas o relatório termina na 5.6 e `todo.md` deixa o Checkpoint 5 sem itens. A fase acrescentou cerca de 4.100 linhas de produção fora o `Designer`; é provável que as metas continuem atendidas, mas o gate pede o número. — **Recomendação:** rodar a cobertura (`--coverage` do Microsoft.Testing.Platform) nos dois projetos de teste, registrar os números e os métodos de regra de negócio a 0% e escrever a seção "Checkpoint 5" (matriz de endereços, Serviços e Vagas, jornada do visitante, totais) antes do `/scan`. — *Relates-to: Task 5.1, Task 5.2, Task 5.3, Task 5.4, Task 5.5, Task 5.6, Checkpoint 5*

3. **`specs/SPEC.md:19-26` (Revision History) e `plans/BACKLOG.md:232-234`, `:238`, `:251`, `:217` — quatro decisões aprovadas do Product Owner que mudam comportamento ainda não voltaram à SPEC.** O histórico termina em v1.4 (S7). Continuam só no BACKLOG: (a) US-002-S08 pede "a lista continua igual à anterior", mas o servidor devolve a lista **sem** a faixa com erro (`SearchService.cs:219-224`, `ParsePriceRange` devolve `(null, null)`); (b) US-002-S04 e a regra de negócio citam o filtro de área só em Terrenos, mas ele vale em Apartamentos, Casas, Terrenos e Comércio e indústria (`SearchFilters.cs`, grupo Imóveis); (c) US-004-S01 e S03 pedem os dois botões, mas telefone fixo (10 dígitos) mostra só "Ligar" (`WhatsAppLink.cs:20`, `ContactViewModel.cs:18`); (d) a regra de US-003 diz "a mesma mensagem" para qualquer anúncio indisponível, mas o arquivado acrescenta o link da categoria (`ShowcaseService.cs:30`). `rules/principles-and-practices.md` §2.5 item 15 exige que toda decisão aprovada que muda comportamento volte à SPEC como emenda de um critério mais uma linha `Amended` no histórico. O `/verify` herda os critérios da SPEC e reprovaria (a), (b) e (c) se os lesse ao pé da letra. — **Recomendação:** uma linha v1.5 no Revision History e a emenda de cada critério (com a decisão do Product Owner), e marcar os quatro itens do BACKLOG. — *Relates-to: US-002-S04, US-002-S08, US-003-S06, US-004-S03*

4. **`src/GazetaMarketplace.Web/wwwroot/js/pages/favorites.js:7` e `:69-70` — o caminho de mais de 100 favoritos (lotes) não tem teste.** A decisão D3 da 5.5 (sem limite de total, lotes de 100 ids por chamada) vive só nesse laço em JavaScript, e o servidor recusa mais de 100 ids (`FavoriteIds.cs:27`, `ShowcaseService.cs:37`). A integração prova o servidor com 100 ids (`FavoritesQueryTests.cs:100`), mas os E2E de favoritos usam no máximo três anúncios (`FavoritesE2ETests.cs:114-330`) e a lista de mutações F1 a F13 não inclui o laço. Trocar `LOTE` para 1000, quebrar o acúmulo de `ausentes` entre lotes ou inverter a ordem entre lotes **passaria em toda a suíte**; com 101 favoritos reais a página inteira mostraria "Não foi possível carregar seus favoritos". — **Recomendação:** um E2E que grava no `localStorage` 150 a 250 ids (poucos reais, o resto inexistente), intercepta `/favoritos/lista` e confere: duas ou mais chamadas, nenhuma com mais de 100 ids, ordem preservada na fronteira do lote, ausentes de todos os lotes removidos e o aviso no plural; acrescentar a mutação `LOTE = 1000` ao roteiro. — *Relates-to: US-005-S06, US-011-S04, Task 5.5*

5. **`src/GazetaMarketplace.Web/wwwroot/js/modules/favorites.js:23-28` × `src/GazetaMarketplace.Core/Showcase/FavoriteIds.cs:21-55` — a regra de ids dos favoritos existe em duas implementações sem tabela de paridade, e já difere no id 0.** `limpar` (JavaScript) guarda inteiros de 1 a 2147483647 sem repetição; `FavoriteIds.TryParse` (C#) aceita de 0 a 2147483647 (`FavoritesTests.cs:52` prova `"0"` aceito, e o contrato do OpenAPI usa `[0-9]+`). Hoje a diferença é inofensiva (o JavaScript é mais estrito), mas `rules/testing.md` §Dual-Implementation Parity é obrigatória quando a mesma regra tem duas representações, e cita exatamente o espelho de validação do cliente. O `DecimalInput` × `lerNumero` ganhou tabela nas duas pontas; este par só tem o teste de "lixo no armazenamento" (`FavoritesE2ETests.cs:300-333`), sem as fronteiras (0, 2147483647, 2147483648, "007", repetidos). O limite de 100 também está escrito duas vezes (`LOTE` no JavaScript e `MaxPerRequest` no C#), sem nada que os amarre. — **Recomendação:** primeiro, eliminar a segunda representação do 100 (o servidor escreve `data-batch-size="@FavoriteIds.MaxPerRequest"` no `Favorites/Index.cshtml` e o JavaScript lê o atributo); depois, uma tabela única de entradas armazenadas conferida em C# e no navegador, e decidir o 0 (o servidor passar a recusar, ou o JavaScript passar a aceitar). — *Relates-to: US-005-S03, NFR-13, Task 5.5*

### 🟢 Suggestion

6. **`src/GazetaMarketplace.Core/Showcase/ShowcaseService.cs:28-31` (com `Views/Ad/Unavailable.cshtml:17-21`) — o anúncio arquivado revela que o id existiu e qual era a categoria.** Rascunho, em revisão, rejeitado e inexistente respondem igual (provado por `AdDetailTests.cs:269-299` até no tamanho da página), mas o arquivado acrescenta o link da categoria, como a US-003-S06 pede. Quem varre ids sequenciais distingue "arquivado" de "nunca existiu" e aprende a categoria. O BACKLOG já registra a escolha (linha 217) e deixa em aberto "se o Product Owner preferir esconder". — **Recomendação:** o Product Owner confirma por escrito que aceita essa exposição (o dado já foi público) ou tira o link; fechar o item do BACKLOG. — *Relates-to: US-003-S06, NFR-13*

7. **`src/GazetaMarketplace.Web/Navigation/PublicUrl.cs:10` e `src/GazetaMarketplace.Web/appsettings.json:11` — fora de Production, o endereço público vem do cabeçalho Host, e `AllowedHosts` é `*`.** O `canonical`, o `og:image`, a mensagem do WhatsApp, o `sitemap.xml` e o `robots.txt` usam `Site:BaseUrl`; sem ele (Development e Testing) usam `Request.Host`. Em Production o valor é obrigatório e validado na partida (`SiteOptions.cs:15-16`), então não há risco ali; o risco é um ambiente de homologação exposto, onde um cabeçalho Host forjado entra no mapa e nos links. O comentário de `SiteOptions.cs:6-8` ainda diz que o valor serve só a e-mails. — **Recomendação:** `AllowedHosts` restrito no exemplo de configuração de produção e na homologação, e atualizar o comentário de `SiteOptions`. — *Relates-to: NFR-21, US-004-S01, I6*

8. **`src/GazetaMarketplace.Web/Controllers/FavoritesController.cs:24-29` — `ids` inválido em `/favoritos/lista` lança `ValidationException` até o `UseExceptionHandler` das páginas.** O contrato é 400 (provado em `FavoritesTests.cs:297`), mas o middleware padrão registra toda exceção não tratada em nível Error com pilha, e a rota é pública: cada `?ids=x` gera um erro no registro (hoje sem alerta, RR-3, mas é ruído que esconde erros de verdade). A API JSON passa pelo `ExceptionHandlingMiddleware`, que registra 4xx em Warning. — **Recomendação:** devolver `BadRequest()` (ou a view de erro com 400) direto, sem lançar, no endpoint de fragmento. — *Relates-to: US-005-S06, D3*

9. **`src/GazetaMarketplace.Core/Search/SearchService.cs:40-176` — `PrepareAsync` tem 137 linhas.** Texto, categoria, local, preço, catálogo, ano, quilometragem, área, ordem, página e a montagem de `SearchForm` num método só (`rules/code-style.md`: preferir menos de 30 linhas; a revisão da Fase 4 apontou 61 linhas no `Preview`). A classe tem 351 linhas e os ajudantes de leitura já estão separados. — **Recomendação:** extrair `ParseText`, `ParseLocationAsync` e `ParseVehicleAsync` devolvendo registros pequenos, e deixar `PrepareAsync` só com a montagem. — *Relates-to: US-002, Task 5.4*

10. **`Web/Controllers/AdController.cs:58`, `CategoryController.cs:33`, `HomeController.cs:31` e `SearchController.cs:34` e `:46` — o mesmo bloco de "falha ao carregar" repetido cinco vezes** (`LogError` com `traceId`, `StatusCode = 503`, `PageStateViewModel` com `ActionUrl = Request.Path + Request.QueryString` e `ReferenceCode`). Uma mudança no texto ou no código de referência exige cinco edições. — **Recomendação:** um método `LoadError(string title)` numa base `PublicControllerBase` (como o painel já tem `PanelControllerBase`). — *Relates-to: US-001-S06, US-002-S11, Task 5.1*

11. **`Infrastructure/Seo/SitemapReadRepository.cs:16` e `:18`, `Infrastructure/Ads/ShowcaseReadRepository.cs:21-29` e `:70-72`, `Infrastructure/Ads/PanelAdListReadRepository.cs:22` — constantes e atalhos repetidos.** O tempo limite de 10 s (RC-15) está escrito em três lugares (`AdCardSql.cs:14`, `SitemapReadRepository.cs:16`, `PanelAdListReadRepository.cs:22`), o dicionário vazio `NoUserSort` em dois, e `ShowcaseReadRepository` repete como atalhos privados `Columns`, `CoverJoin`, `ToRow` e `Command` que só reencaminham para `AdCardSql`. A API `OrderBy(null, null, NoUserSort, "…")` do `SqlBuilder` obriga a passar dois nulos e um dicionário vazio para uma ordem fixa. — **Recomendação:** uma constante única para o tempo limite, remover os atalhos, e um `SqlBuilder.OrderByFixed(string)` para a ordem sem entrada do usuário. — *Relates-to: RC-15, ADR-004, Task 5.4*

12. **`Core/Showcase/IShowcase.cs` (interfaces, registros e constantes), `Core/Search/SearchModels.cs`, `Core/Seo/Sitemap.cs` e `Core/Seo/ISitemap.cs` — vários tipos públicos por arquivo.** `rules/overrides/lang-dotnet.md` pede um tipo público por arquivo (a revisão da Fase 4 apontou `IAdSpecsReader` dentro de `IReviewQueue.cs`); `IShowcase.cs` guarda mais de dez tipos, e `Sitemap.cs` junta `SitemapEntry`, `SitemapDocument` e `RobotsText`. — **Recomendação:** separar ao tocar de novo nos arquivos (`ShowcaseFilters`, `ShowcaseRows`, `ShowcaseAdPage`, `RobotsText`, `SearchLimits`, `SearchOrders`). — *Relates-to: Task 5.1, Task 5.4, Task 5.6*

13. **Comentários e documentos desatualizados.** `Web/Controllers/FavoritesController.cs:14` e `SearchController.cs:15` dizem "o SEO é a tarefa 5.6" (já entregue); `Views/Ad/Index.cshtml:60` diz que o botão "Favoritar" "entra nesta coluna" (ele está duas linhas abaixo); `Core/Ads/AdRoutes.cs:5` diz que a página de detalhe virá na 5.2; `Views/Shared/_AdPhotos.cshtml:5` diz que a galeria "é da página pública (5.2)"; `architecture/adr/ADR-006-busca-e-filtros.md` escreve `@Zero` onde o código usa `@NoMatch` (`SearchReadRepository.cs:58`). — **Recomendação:** corrigir os seis textos junto da próxima edição de cada arquivo. — *Relates-to: Task 5.2, Task 5.4, Task 5.5*

14. **`src/GazetaMarketplace.Web/Controllers/HomeController.cs:16-18` — o arquivo foi reescrito (construtor primário, `try`/`catch`, SEO) e manteve o namespace em bloco e a classe sem `sealed`.** `rules/code-style.md` e `lang-dotnet.md` pedem namespace de arquivo e `sealed` nas classes sem herança; todos os controllers novos da fase (`AdController`, `CategoryController`, `SeoController`…) seguem a regra. — **Recomendação:** converter para `namespace …;` e `public sealed class`, já que o arquivo mudou quase por inteiro. — *Relates-to: Task 5.1*

15. **`src/GazetaMarketplace.Web/Views/Search/Index.cshtml:125-195` (254 linhas) — três blocos de faixa (preço, ano, área) quase idênticos, e `hidden` combinado com `d-block` nas mensagens de erro.** Cada bloco repete `fieldset`, dois `input` com as mesmas classes e atributos e a mensagem; a view cresce a cada filtro novo. Nas linhas 137, 173, 178 e 192 as mensagens usam `class="invalid-feedback d-block"` com `hidden`: o `d-block` do Bootstrap (`display: block !important`) vence o `[hidden]` do reboot (mesma especificidade, vem depois), então o `hidden` não esconde nada; como o texto está vazio o efeito é nulo, mas o atributo engana o próximo leitor e o NVDA pode ler uma região vazia. — **Recomendação:** um parcial `_RangeFilter` (nome, rótulos, erro, valores) e esconder a mensagem pelo texto vazio (sem `d-block` e sem `hidden`) ou trocar por uma classe que respeite `hidden`; conferir no NVDA já previsto para o `/verify`. — *Relates-to: US-002-S08, NFR-16, Task 5.4*

16. **JavaScript e views dos favoritos — pequenas duplicações.** `mostrarAviso` existe igual em `pages/layout.js:9` e em `modules/favorites-ui.js:9`; `modules/favorites-ui.js:66` reexporta `atualizarContador`, que ninguém importa e que tem o mesmo nome de outra função de `modules/counter.js:2`; `Views/Favorites/List.cshtml:11` passa `favorite = "remove"` como texto em vez de `AdCardFavorite.Remove`. — **Recomendação:** `mostrarAviso` num módulo só, remover o reexport morto e usar a constante. — *Relates-to: US-005-S07, Task 5.5*

17. **`src/GazetaMarketplace.Web/wwwroot/js/pages/favorites.js:46-114` — duas chamadas de `carregar()` ao mesmo tempo duplicam a lista.** `carregar` limpa o host no início e só anexa a lista no fim (depois de `await`); se outra aba mudar os favoritos (`aoMudar`, linha 111) ou o visitante tocar duas vezes em "Tentar novamente" (linha 107) enquanto a primeira chamada ainda espera a rede, as duas terminam e as duas anexam a sua `ul`. A janela é curta, mas a falha é visível (cards em dobro e contagem errada). — **Recomendação:** um contador de geração: cada chamada guarda o seu número e só anexa se ainda é a última. — *Relates-to: US-005-S03, Task 5.5*

18. **`tests/GazetaMarketplace.Web.Tests/Search/DecimalInputTests.cs:19-58` e `tests/GazetaMarketplace.Web.Tests.Playwright/Showcase/SearchE2ETests.cs:333-374` — a "mesma tabela" de paridade foi copiada à mão em dois arquivos.** Uma linha nova num arquivo e não no outro faz as duas pontas deixarem de ser comparadas sem nenhum teste falhar. Além disso o `search.js:195` usa `lerNumero` também para ano, e `lerNumero("2.020")` vale 2020, enquanto o servidor recusa `2.020` como ano (`SearchService.cs`, `int.TryParse(NumberStyles.None)`): o navegador pode acusar "ano inicial maior que o final" onde o servidor acusaria "ano inválido". — **Recomendação:** um arquivo de dados único (JSON) lido pelos dois testes e uma coluna para o ano. — *Relates-to: US-002-S08, Task 5.4*

19. **`src/GazetaMarketplace.Infrastructure/Search/SearchReadRepository.cs:74` — o parâmetro `@Uf` chega como `nvarchar` contra a coluna `char(2)` (`AdConfiguration.cs:48`).** O Dapper envia `string` como `nvarchar(4000)`; comparar com `char(2)` converte a **coluna** (`CONVERT_IMPLICIT`), o que, conforme a collation do provedor (ainda desconhecida, AR-03), impede a busca por índice em `IX_Ads_Status_Uf_City`. O teste de plano (`SearchQueryTests.cs:568-581`) só cobre categoria mais UF, em que o índice da categoria resolve; UF sozinha ou UF e cidade sem categoria não têm plano provado. Com 200 anúncios não pesa. — **Recomendação:** enviar o parâmetro como `DbType.AnsiStringFixedLength` com tamanho 2 e acrescentar o plano de "só UF" ao teste. — *Relates-to: NFR-04, ADR-006, Task 5.4*

20. **`src/GazetaMarketplace.Core/Search/SearchService.cs:78` e `:149` — a cada busca com UF o servidor lê a lista inteira de cidades do banco e normaliza cada nome; a cada busca monta 147 opções de categoria.** `CityDirectory` não tem cache (645 cidades em SP, cerca de 25 KB, também desenhadas no HTML), e `OptionOf` resolve o grupo de campos de todas as categorias a cada pedido. Dentro do orçamento da NFR-04 hoje (52 ms com 6.000 anúncios), mas é trabalho repetido igual para todos os visitantes. — **Recomendação:** guardar as opções de categoria no snapshot da árvore e a lista de cidades por UF em memória com validade curta; medir no `/verify`. — *Relates-to: NFR-04, Task 5.4*

21. **`src/GazetaMarketplace.Web/Controllers/SeoController.cs:25-37` e `Infrastructure/Seo/SitemapReadRepository.cs:30-37` — `/sitemap.xml` faz duas consultas e lê até 50.000 linhas por pedido anônimo, sem cache e sem gatilho numérico para mudar.** A decisão (sem cache, "arquivar tira do mapa na hora") está justificada e o volume da v1 (200 anúncios) é pequeno; o teto de 10 s e os 100 pedidos por minuto por IP limitam o dano. O BACKLOG (linha 276) não diz quando revisar. — **Recomendação:** registrar o gatilho (por exemplo mais de 5.000 publicados ou mais de 2 pedidos por segundo ao mapa) e a opção de cache de 60 s, e medir no `/verify`. — *Relates-to: NFR-21, NFR-04, D3*

22. **`src/GazetaMarketplace.Web/Controllers/Api/PublicAdsController.cs:20-41` — a API `GET /api/v1/ads?ids=` não tem cliente no produto, e o contrato e o diagrama não refletem o que foi entregue.** A página de favoritos usa `/favoritos/lista` (decisão D1 da 5.5, BACKLOG linha 255); a API só roda nos testes e é uma superfície pública a mais para manter e proteger. `architecture/diagrams/sequence/favoritos.md:21` ainda desenha `GET /api/v1/ads?ids=…`, e `architecture/api/openapi.yaml` e `ARCHITECTURE.md` não listam `/api/v1/public/cities`, `/favoritos/lista`, `/sitemap.xml` nem `/robots.txt`. — **Recomendação:** o Product Owner decide manter ou retirar a API; em qualquer caso acrescentar os quatro endereços ao contrato e corrigir o diagrama. — *Relates-to: US-005-S06, ADR-006, NFR-21*

23. **`src/GazetaMarketplace.Infrastructure/Photos/AdPhotoService.cs:40-47` (com `Web/Models/AdDetailFactory.cs:47`) — `ListAsync` lê as fotos por id de anúncio sem checar a situação nem o acesso.** Hoje é seguro: a página pública só chama a fábrica depois de `FindPublishedAsync` (`ShowcaseService.cs:23`) e a pré-visualização do painel exige Administrador. É uma defesa em profundidade que depende de quem chama (o comentário da fábrica diz "quem chama decidiu"); um terceiro chamador futuro que passe um anúncio não publicado entregaria os endereços das fotos (a entrega do arquivo ainda recusa, `PhotoDelivery.cs:31-38`). — **Recomendação:** um teste que falhe se a página pública montar a fábrica sem passar pelo leitor de publicados, ou um `IPublishedPhotoReader` próprio. — *Relates-to: NFR-13, US-003-S06*

24. **Nomes de CSS e JavaScript em português, contra `rules/overrides/lang-dotnet.md` (código em inglês).** Classes novas (`ad-galeria__*`, `categoria-tile`, `categoria-caminho`, `ad-card__favorito` convive com `search-filters`) e identificadores de `pages/favorites.js` (`raiz`, `campo`, `lote`), `modules/favorites.js` (`CHAVE`, `armazenamento`) e `pages/search.js` (`configurarPainel`, `esvaziar`). É o mesmo ponto da revisão da Fase 4 (🟢 22), que segue sem regra escrita em `local/CLAUDE.local.md`; a fase acrescentou nomes novos nos dois idiomas. — **Recomendação:** decidir a regra para CSS e JavaScript (o glossário só cobre C#, banco e rotas), registrar em `local/CLAUDE.local.md` e aplicar aos nomes novos. — *Relates-to: Task 5.2, Task 5.4, Task 5.5*

25. **`src/GazetaMarketplace.Core/Ads/AdRoutes.cs:23-30` (com `Core/Categories/SlugGenerator.cs:26`) — título sem letras latinas ou números gera o endereço `/anuncio/{id}/categoria`.** `Slugify` devolve o texto de reserva `"categoria"` quando não sobra nada (título só com emoji, ideogramas ou símbolos). O endereço funciona (o id manda e o 301 corrige), mas é um endereço enganoso para o SEO e para quem lê o link. — **Recomendação:** para anúncio, usar o slug vazio (`/anuncio/{id}`) quando o título não gera slug, e cobrir no teste de endereços. — *Relates-to: NFR-21, Task 5.2*

26. **`src/GazetaMarketplace.Core/Search/SearchService.cs:205-216` — `ModelsOrEmptyAsync` engole `NotFoundException` sem log.** A marca já foi validada no catálogo (`ParseId`), então um `NotFoundException` aqui significa catálogo e anúncios fora de sintonia, e o visitante só vê "Escolha a marca" sem que ninguém saiba; é o mesmo padrão que o 🟡 6 da Fase 4 corrigiu em `AdSpecsReader.cs:57-60` com `LogWarning`. — **Recomendação:** o mesmo `LogWarning` (tipo e marca, sem dado pessoal). — *Relates-to: US-002-S03, Task 5.4*

27. **Estado do repositório.** `reports/CODE_REVIEW.md → reports/CODE_REVIEW-FASE-4.md` está renomeado e preparado no índice, mas sem commit; este relatório cria um novo `reports/CODE_REVIEW.md`. Quatro commits de feature têm "work in progress" no título (c3e7923, adae03f, 7aba6bd, 99ba64e); estão corretos como estado intermediário na branch, mas viram ruído no histórico se a branch não for comprimida na integração. `rules/git-workflow.md` pede que cada comando declare o que deixou sem commit. — **Recomendação:** commitar o renomear e o novo relatório juntos, com título próprio (`docs(review): code review of phase 5`), antes do próximo comando. — *Relates-to: Task 5.6, Checkpoint 5*

28. **`plans/BACKLOG.md:201`, `:206`, `:208`, `:215`, `:216`, `:242`, `:243` e `:259` — itens entregues que continuam abertos (`[ ]`).** O link "Filtrar e ordenar" (201, resolvido na linha 247), os cards que levam ao detalhe (206, provado por `AdDetailTests.cs:519`), a descrição nas páginas (208), os botões de contato e "Favoritar" (215, entregues na 5.3 e na 5.5), o mapa do site (216, entregue na 5.6), o coração nos cards da busca (242, provado por `FavoritesTests.cs:372`) e o SEO da busca e dos favoritos (243 e 259, `noindex, follow` da 5.6) estão fechados no código. Mesmo problema do 🟢 24 da Fase 4. — **Recomendação:** marcar os oito itens como `[x]` com a referência e registrar os achados deste relatório (item 28 da §4). — *Relates-to: Task 5.1, Task 5.2, Task 5.5, Task 5.6*

### ✅ Good

- **`Infrastructure/Ads/ShowcaseReadRepository.cs:33`, `:56`, `:65`; `Infrastructure/Search/SearchReadRepository.cs:58`; `Infrastructure/Seo/SitemapReadRepository.cs:22`, `:35`; `Infrastructure/Ads/PublishedAdReader.cs:15-18`** — "só Publicado" em toda leitura pública por um fragmento único (`SqlFragments.OnlyPublished`, via `OnlyPublished()` do `SqlBuilder`) e, no caminho de EF, na própria consulta (`Status == AdStatus.Published`); nenhuma consulta pública lê nome, e-mail ou outro dado do autor (`AdCardSql.cs:12-17`). Provado no SQL Server real com um anúncio em cada situação mais o despublicado: `FavoritesQueryTests.cs:75`, `SitemapQueryTests.cs:78`, `SearchQueryTests.cs:170`, `AdDetailQueryTests.cs:69`, `ShowcaseQueryTests.cs:97`, e a linha sem autor em `ShowcaseQueryTests.cs:200` e `SearchQueryTests.cs:428`.
- **`Core/Search/SearchService.cs:40-176` e `Infrastructure/Search/SearchReadRepository.cs:30-60`** — nada do pedido vira texto de SQL: UF vem de `BrazilianStates`, cidade da lista do IBGE, marca e modelo do catálogo, categoria da árvore, ordem de um `enum` com `switch` de texto fixo, números lidos e limitados, e o termo vai como parâmetro de `CHARINDEX` (sem `LIKE`, sem escape). O `TOP (@Take)` do mapa também é parâmetro. Provas: `SearchQueryTests.cs:377` e `:390` (aspas, `%`, `_`, `[` como texto) e as mutações da 5.4.
- **`Core/Search/SearchModels.cs:20-23`, `Core/Showcase/IShowcase.cs:23`, `FavoriteIds.cs:14`, `SqlBuilder.cs:100-104`, `AdCardSql.cs:14`** — limites de DoS no lugar certo: termo de 100 caracteres e 5 palavras, página máxima 100.000 (com a conta do deslocamento em 64 bits, o que também resolve o 🟡 1 da Fase 4), no máximo 100 ids por chamada (e a guarda extra em `ShowcaseService.cs:37`), tempo limite de 10 s e limite global de 100 pedidos por minuto por IP. Provas: `ShowcaseTests.cs:341-352` (`pagina=2147483647`), `SearchRulesTests.cs:182`, `SearchQueryTests.cs:498`.
- **`Web/Controllers/AdController.cs:30-38` e `Views/Ad/Unavailable.cshtml`** — rascunho, em revisão, rejeitado e inexistente dão 404 com **o mesmo corpo**, `noindex` e sem título, foto ou descrição; nunca 410. O slug errado leva a 301 para o endereço atual (`AdController.cs:40`) sem redirecionamento aberto. Provas: `AdDetailTests.cs:269-299`, `:334`, `:369`.
- **`Views/Shared/_Seo.cshtml:4-5`, `Web/Models/SeoModel.cs` e `Core/Seo/Sitemap.cs`** — padrão seguro por omissão: quem não pede para ser indexado sai com `noindex, follow`; só início, categoria e anúncio publicado pedem. O `robots.txt` deixa `/busca` e `/favoritos` abertos de propósito (o robô precisa abrir a página para ler o `noindex`) e fecha `/painel`, `/api/` e `/favoritos/lista`; o mapa usa `Site:BaseUrl` e escapa o XML. Provas: `TitleTests.cs:235`, `RobotsTests.cs:47`, `SitemapTests.cs:109` e `:177`.
- **`Views/Shared/_Contact.cshtml:11`, `Core/Contact/WhatsAppLink.cs:34-38`** — o link do WhatsApp abre em nova aba com `rel="noopener noreferrer"`, a mensagem vai codificada com `Uri.EscapeDataString` (acentos, aspas, `&`, `#`, emoji) e o título é cortado sem partir par substituto; os links são comuns, sem chamada ao servidor e sem registro de cliques. Provas: `WhatsAppLinkTests.cs`, `ContactTests.cs:116` e `:178`, `ContactE2ETests.cs:154`.
- **Escape de texto e RC-17** — título, descrição, cidade, termo de busca e metas saem codificados (`AdDetailTests.cs:477-488`, `FavoritesTests.cs:307-315`, `SearchTests.cs:466-474`, `TitleTests.cs:202`, `ShowcaseTests.cs:367`); `favorites.js:37` usa `DOMParser` e `append`, sem `innerHTML`, com a varredura `NenhumModuloUsaInnerHtmlComTextoDoServidor` que já pegou uma regressão (TEST_REPORT, achado 1 da 5.5). Nenhum `style=`, `onclick=` ou `<script>` inline nas views novas.
- **`Web/Models/AdDetailFactory.cs`, `Infrastructure/Ads/AdCardSql.cs`, `Core/Showcase/ShowcaseCards.cs`** — uma só montagem do corpo do anúncio (a pré-visualização da fila e a página pública), uma só lista de colunas e capa para vitrine e busca, e um só tradutor de linha para card. O que o Administrador aprova é o que o visitante vê, e o 🟡 5 da Fase 4 (rótulo como chave) foi resolvido com chave de campo.
- **`Infrastructure/Data/Migrations/20261005161939_AddPublishedAtIndex.cs`, `AdConfiguration.cs:71`, `db/scripts/gazeta-idempotente.sql`** — índice novo só aditivo (nada renomeado nem apagado), com `Down` e script idempotente atualizado; o plano de execução foi medido antes e depois (varredura mais ordenação virou `Index Seek`), com teste (`ShowcaseQueryTests.cs:224`).
- **Camadas e controllers finos** — interfaces no Core (`IShowcase`, `ISearch`, `ISitemap`), Dapper e EF na Infrastructure (`ServiceCollectionExtensions.cs:68-74`), Core sem referência a EF ou Dapper, e as páginas só montam o `ViewModel`, tratam a falha e devolvem a view.
- **Progressive enhancement** — o formulário de busca, a paginação, a galeria (miniaturas como links e `<dialog>` nativo), o contato e a navegação funcionam sem JavaScript; os controles que só fazem sentido com ele (coração, "Favoritar", botão de recolher filtros) nascem com `hidden`. Provas: `SearchE2ETests.cs:171` e `:448`, `FavoritesE2ETests.cs:354`, `AdDetailE2ETests.cs:255`, `ContactE2ETests.cs:207`, `ShowcaseE2ETests.cs:85`.
- **Rastreabilidade e disciplina de testes** — 73 mutações aplicadas na fase (5.1: 12, 5.2: 12, 5.3: 8, 5.4: 16, 5.5: 13, 5.6: 12) e todas mortas ao final, incluindo as que sobreviveram na primeira rodada e foram refeitas (V11 equivalente justificado, J3 e F5 corrigidos); nomes de teste com o prefixo do cenário; a prova das consultas é no SQL Server real, e os repositórios falsos dos testes do site **não** filtram de propósito (`StubShowcaseRepository.cs`, `StubSearchReadRepository.cs`), só provam o que o serviço pede.
- **Matriz do Checkpoint 5** — `Checkpoint5PublicTests.cs:31-70` abre sem login os 13 endereços públicos e confirma 200 sem redirecionar para a entrada do painel, 404 sem login para o que não existe, que o painel e a API da equipe seguem pedindo login e que nenhuma página pública cria cookie de entrada; a jornada do visitante com Serviços e Vagas está em `Checkpoint5E2ETests.cs:91` e `:135` e `Checkpoint5ServicesJobsTests.cs`.

## 4. Action Items

- [ ] **P0**: nenhum.
- [ ] **P1**: recusar número acima do teto antes de multiplicar em `DecimalInput.TryParseCents` e cobrir 27 a 29 dígitos (aviso 1).
- [ ] **P1**: medir a cobertura da Fase 5, listar os métodos a 0% e escrever a seção do Checkpoint 5 no `TEST_REPORT.md` antes do `/scan` (aviso 2).
- [ ] **P1**: registrar a emenda v1.5 da SPEC com as quatro decisões do Product Owner (S08, área, fixo sem WhatsApp, arquivado) (aviso 3; decisão do Product Owner).
- [ ] **P1**: teste de favoritos com mais de 100 ids e mutação `LOTE` (aviso 4).
- [ ] **P1**: tabela de paridade `limpar` × `FavoriteIds`, tamanho do lote vindo do servidor e decisão sobre o id 0 (aviso 5).
- [ ] **P2**: itens 6 a 28 (arquivado e Host: decisões e `AllowedHosts`; 400 sem exceção em `/favoritos/lista`; extrações de `PrepareAsync`, do bloco de erro dos controllers, do parcial de faixa e das constantes; separar tipos por arquivo; comentários e ADR; `HomeController`; duplicações de JavaScript e a corrida do `carregar`; tabela de paridade única; `AnsiStringFixedLength` no `@Uf`; cache de cidades, opções e mapa com gatilho; API de favoritos, contrato e diagrama; defesa em profundidade de `ListAsync`; regra de nomes de CSS e JavaScript; slug de reserva; log do catálogo; commit do renomear; limpeza do BACKLOG).
- [ ] **P2**: registrar no `plans/BACKLOG.md` os 23 itens 🟢 (e os 5 🟡 se o Product Owner decidir aceitar algum) com `found by /review Fase 5, 2026-10-06`.

## 5. Test Coverage

**Gate 6** (de `reports/TEST_REPORT.md`): aprovado em cada tarefa. Números da última rodada (tarefa 5.6): 1.650 testes unitários do site, 43 da ferramenta de catálogo, 27 da ferramenta de municípios, 160 de integração (SQL Server 2022 em contêiner) e 144 de navegador (Playwright), todos passando. Evolução dos unitários, da integração e do E2E: 5.1 (1.335 / 127 / 95), 5.2 (1.378 / 130 / 105), 5.3 (1.416 / 131 / 113), 5.4 (1.564 / 151 / 131), 5.5 (1.620 / 155 / 141), 5.6 (1.650 / 160 / 144). Os testes do Checkpoint 5 (d53e89b) ainda não entraram nesses totais.

**Mutação:** 73 mutações na fase, todas mortas ao final (as sobreviventes da primeira rodada foram corrigidas ou justificadas como equivalentes). Não há mutação para o laço de lotes de favoritos (aviso 4) nem para o estouro de `decimal` (aviso 1).

**Cobertura numérica:** não medida para a fase (aviso 2); a última medição, de antes da Fase 5, é de 97,7% de linhas e 90,9% de ramos.

**Cenários da SPEC (cada um com caminho ligado ao app e teste que afirma o efeito observável):**

| Cenário | Caminho ligado | Teste (efeito observável) |
|---|---|---|
| US-001-S01 | `HomeController.Index` → `ShowcaseService.HomeAsync` → `ShowcaseReadRepository.RecentAsync` | `ShowcaseTests.cs:59`, `:85`, `:109` (categorias, 12 cards, capa, título, preço, cidade/UF, busca no topo); `ShowcaseQueryTests.cs:97` (SQL real) |
| US-001-S02 | `CategoryController.Index` → `ShowcaseService.CategoryAsync` | `ShowcaseTests.cs:121` (subcategorias e pedido com todas as descendentes); `ShowcaseQueryTests.cs:137` |
| US-001-S03 | idem, caminho de navegação | `ShowcaseTests.cs:144`, `:174`; E2E `ShowcaseE2ETests.cs:50`, `:85` |
| US-001-S04, S05 | `Category/Index.cshtml`, `Home/Index.cshtml` | `ShowcaseTests.cs:188`, `:203` (mensagens e links das demais categorias) |
| US-001-S06 | `catch` de `HomeController` e `CategoryController` → 503 | `ShowcaseTests.cs:220` (mensagem, "Tentar novamente", código, nada técnico) |
| US-001-S07 | `FindBySlug` nulo → `Category/NotFound.cshtml` | `ShowcaseTests.cs:243`, `:258`, `:283`, `:294`; E2E `ShowcaseE2ETests.cs:99` |
| US-001-S08 | `row-cols-2` e Bootstrap | `ShowcaseTests.cs:305`; E2E `ShowcaseE2ETests.cs:113` (sem rolagem horizontal em 320 px) |
| US-002-S01 | `SearchController` → `SearchService.PrepareAsync` → `SearchReadRepository` | `SearchTests.cs:71`; `SearchQueryTests.cs:185`, `:210` (sem acento, título e descrição, todas as palavras) |
| US-002-S02 | filtros combinados, cidade da UF | `SearchTests.cs:100`; `SearchQueryTests.cs:222`, `:239`; E2E `SearchE2ETests.cs:85` |
| US-002-S03, S04 | filtros pelo grupo de campos da categoria | `SearchTests.cs:122`, `:150`; `SearchQueryTests.cs:259`, `:278`; E2E `SearchE2ETests.cs:255`, `:301` |
| US-002-S05, S06 | `SearchOrders`, `Page` de 24 | `SearchTests.cs:166`, `:181`, `:199`, `:217`; `SearchQueryTests.cs:295`, `:330`, `:355` |
| US-002-S07 | `data-no-results` e "Limpar filtros" | `SearchTests.cs:230`, `:245`; E2E `SearchE2ETests.cs:127` |
| US-002-S08 | `ParsePriceRange` e `search.js` | `SearchTests.cs:271`; `SearchRulesTests.cs:238`; E2E `SearchE2ETests.cs:144`, `:171` (a lista "sem a faixa" difere da SPEC, ver 🟡 3) |
| US-002-S09, S10 | endereço com filtros; UF limpa a cidade | `SearchTests.cs:303`, `:332`, `:347`; `PublicCitiesTests.cs:18`; E2E `SearchE2ETests.cs:187`, `:211`, `:239` |
| US-002-S11, S12 | 503 com filtros preservados; painel recolhível | `SearchTests.cs:366`, `:387`, `:403`; `SearchQueryTests.cs:498` (tempo limite real); E2E `SearchE2ETests.cs:389`, `:425` |
| US-003-S01 a S05 | `AdController.Index` → `AdDetailFactory` → `_AdGallery` | `AdDetailTests.cs:97`, `:131`, `:156`, `:172`, `:205`, `:230`; E2E `AdDetailE2ETests.cs:84`, `:112`, `:137`, `:180` |
| US-003-S06 | `ShowcaseService.AdAsync` → 404 | `AdDetailTests.cs:248`, `:269`; `AdDetailQueryTests.cs:69`; E2E `AdDetailE2ETests.cs:285` |
| US-003-S07, S08 | `ad-gallery.js`, CSS da galeria | `AdDetailTests.cs:302`, `:317`; E2E `AdDetailE2ETests.cs:196`, `:223` |
| US-004-S01 a S05 | `ContactViewModel` → `_Contact` | `ContactTests.cs:42`, `:58`, `:71`, `:89`, `:103`, `:116`, `:132`; E2E `ContactE2ETests.cs:121`, `:141`, `:154` (nova aba, `noopener`, sem chamada ao site) |
| US-005-S01, S02 | `favorites-ui.js`, `AdCard` e botão da página | `FavoritesTests.cs:354`, `:387` (markup); E2E `FavoritesE2ETests.cs:114` (coração, contador, "Favoritado") |
| US-005-S03, S04, S05, S08 | `pages/favorites.js` e `/favoritos/lista` | E2E `FavoritesE2ETests.cs:153`; `FavoritesTests.cs:252`, `:321` |
| US-005-S06 e US-011-S04 | ids ausentes saem do `localStorage` com aviso | E2E `FavoritesE2ETests.cs:201` (singular e plural); `FavoritesQueryTests.cs:75` |
| US-005-S07 | armazenamento bloqueado | E2E `FavoritesE2ETests.cs:244` |
| NFR-21 | `_Seo`, `SeoController`, `SitemapService` | `TitleTests.cs:64-298`, `SitemapTests.cs:66-190`, `SitemapQueryTests.cs:78`, `:140`; E2E `SeoE2ETests.cs:63`, `:99` |

Auditoria anti-vacuous: os testes abertos (`AdDetailTests.cs:269`, `FavoritesQueryTests.cs:75`, `SitemapQueryTests.cs:78`) leem o corpo, a lista ou o mapa depois do pedido; remover a funcionalidade os derrubaria (e as mutações confirmam). Três pontos fracos: o laço de lotes de favoritos passaria em toda a suíte se fosse removido ou alterado (🟡 4); `Checkpoint5PublicTests.cs:31-55` só afirma 200 e ausência de redirecionamento, o que uma página vazia também satisfaria (o conteúdo é provado por outros testes e pela jornada E2E); e o estouro de `decimal` não tem entrada na tabela (🟡 1). Os testes de coração e de botão no nível HTTP provam só o markup (`hidden`, `aria-pressed`, nome acessível); o comportamento (clique, contador, `aria-pressed` mudando) é provado só no E2E, o que é adequado para JavaScript, mas depende do site publicado. Paridade de duas implementações: `DecimalInput` × `lerNumero` tem tabela nas duas pontas (copiada à mão, 🟢 18); `limpar` × `FavoriteIds` não tem (🟡 5); as colunas normalizadas da busca têm uma só implementação (`Normalizer`, ADR-006), sem backfill em SQL.

**Reconciliação de OPEN-NNN:** o `TEST_REPORT.md` atual **não possui** nenhum conjunto `OPEN-NNN`; as pendências das tarefas 5.x foram registradas em `plans/BACKLOG.md` (linhas 196 a 278). Esta é a disposição de cada item:

| Item do BACKLOG (linha) | Disposição |
|---|---|
| 3 métodos a 0% a conferir (198) | DEFERRED-to-/scan (conferir na medição do aviso 2) |
| teste de plano instável (199) | CLOSED |
| runbook `PLAYWRIGHT_BROWSERS_PATH` (200) e mutação de JavaScript (245) | DEFERRED-to-P2 (documentação) |
| link "Filtrar e ordenar" (201, 247) | CLOSED (`SearchTests.cs:478`, E2E `SearchE2ETests.cs:518`) |
| sem cache da inicial e da categoria (202) | DEFERRED-to-/verify (definir gatilho numérico) |
| cards levam ao detalhe (206) | CLOSED (`AdDetailTests.cs:519`; marcar a caixa, 🟢 28) |
| ícone único das categorias (207) | CLOSED (decisão) |
| `meta description` e dados estruturados (208) | CLOSED a descrição (5.6); JSON-LD em 267 |
| tipo e área do card pelo JSON (209) | DEFERRED-to-/verify (custo medido: 52 ms com 6.000 anúncios) |
| CEP dos E2E (210) | DEFERRED-to-P2 |
| D1: sem contato nem "Favoritar" (215) | CLOSED (5.3 e 5.5; marcar a caixa) |
| mapa do site (216) | CLOSED (5.6); JSON-LD em 267 |
| arquivado mostra a categoria (217) | ESCALATED (Product Owner; 🟢 6) |
| limite de fotos 300/min por IP (218) | DEFERRED-to-/verify (valor de produção com medida real) |
| cópias do roteiro "cria e publica" nos E2E (219) | DEFERRED-to-P2 |
| fábrica compartilhada (220) | CLOSED |
| aspas tipográficas no WhatsApp (224) | CLOSED (decisão) |
| ligar e WhatsApp de verdade num celular (225, 233) | DEFERRED-to-/verify |
| telefone fixo só com "Ligar" (226, 232) | ESCALATED (emenda da SPEC, 🟡 3) |
| pré-visualização com o mesmo bloco de contato (227) e S01 da US-003 completa (228) | CLOSED |
| S08 "igual à anterior" (234) | ESCALATED (emenda da SPEC, 🟡 3) |
| filtro de área em quatro categorias (238, 251) | ESCALATED (emenda da SPEC, 🟡 3) |
| texto casa em qualquer pedaço, `CHARINDEX` (239) | DEFERRED-to-pós-lançamento (gatilho da ADR-006) |
| desempenho medido: 6 ms e 52 ms (240) | DEFERRED-to-/verify (volume real) |
| cidades públicas, cache de 10 min (241) | CLOSED (limite global vale; 🟢 20 sugere cache no servidor) |
| coração nos cards da busca (242) | CLOSED (`FavoritesTests.cs:372`; marcar a caixa) |
| SEO da busca (243, 259, 271) | CLOSED (5.6, `TitleTests.cs:235`) |
| só a quilometragem máxima (244) | CLOSED (a SPEC pede só o máximo) |
| preço digitado vira centavos (246) | DEFERRED-to-P2 |
| API de favoritos sem cliente (255) | ESCALATED (Product Owner; 🟢 22) |
| favoritos sem limite, em lotes (256) | DEFERRED-to-P1 (falta o teste, 🟡 4) |
| favoritos só no navegador (257), sem JavaScript (258) | CLOSED (decisão e aviso permanente) |
| nome acessível do coração (260) | DEFERRED-to-/verify (NVDA) |
| E2E `US003S03` instável (261) | DEFERRED-to-/verify (acompanhar) |
| limite de login nos E2E (262) | CLOSED (runbook) |
| S7 confirmada (266) | CLOSED |
| JSON-LD nos anúncios (267) | DEFERRED-to-pós-lançamento |
| canônico da página 2, título da subcategoria, `robots.txt` (272, 273, 274) | CLOSED (decisões registradas e testadas) |
| `og:image` em WebP (275) | DEFERRED-to-/verify (conferir a prévia de verdade) |
| mapa do site sem cache (276) | DEFERRED-to-pós-lançamento (definir gatilho, 🟢 21) |
| `Site:BaseUrl` define canônico, mapa e `robots.txt` (277) | DEFERRED-to-/deploy (conferir o valor de produção) |
| `SqlBuilder.Page` até 100 (278) | CLOSED (`TOP (@Take)` com parâmetro) |

## 6. Compliance Check

> Cada PASS cita arquivo e linha, o ponto em que o controle está ligado ao pipeline, ou o nome do teste. Controles transversais conferidos no `Program.cs`: antiforgery global (`:46-49`), limitador de requisições (`:64` e `:105`), correlation id e cabeçalhos de segurança (`:81-82`), exception handler das páginas (`:88`), HTTPS (`:90`), limite de corpo (`:101`).

| Rule | Status | Evidence (file:line / test) | Notes |
|------|--------|-----------------------------|-------|
| api-conventions.md | WARNING | Rotas em kebab-case e versionadas: `PublicAdsController.cs` (`api/v1/ads`), `PublicCitiesController.cs` (`api/v1/public/cities`); envelope `PagedResult.cs`; 400 por `ValidationException` (`PublicAdsController.cs:30`); testes `FavoritesTests.cs:177`, `:203`, `PublicCitiesTests.cs:48`. Falha: `/api/v1/public/cities` não consta de `architecture/api/openapi.yaml` | 🟢 22 |
| brownfield.md | N/A | `.claude/PROJECT_PROFILE.md`: Mode greenfield | Não ativo |
| clean-code.md | WARNING | Bom: `ShowcaseCards.Create` e `AdCardSql` únicos e pequenos; `CancellationToken` por último. Falha: `SearchService.PrepareAsync` com 137 linhas (`:40-176`); bloco de erro repetido em cinco pontos | 🟢 9, 10 |
| code-style.md | WARNING | Sem `!` nem `?` de referência nas linhas adicionadas (busca no diff); namespaces de arquivo e `using` explícitos nos arquivos novos; chaves em todos os blocos. Falha: `HomeController.cs:16-18` com namespace em bloco; arquivos com vários tipos | 🟢 12, 14 |
| error-handling.md | WARNING | Bom: falha de página vira 503 sem detalhe técnico, com código de referência (`ShowcaseTests.cs:220`, `AdDetailTests.cs:492`, `SearchTests.cs:366`); `ValidationException` com campo (`PublicAdsController.cs:30`). Falha: `OverflowException` sem tratamento em `DecimalInput.cs:63`; exceção lançada para entrada inválida em `FavoritesController.cs:27`; `catch` sem log em `SearchService.cs:211` | 🟡 1, 🟢 8, 26 |
| database.md | PASS | Parâmetros nomeados em todas as consultas (`SearchReadRepository.cs:56-101`, `ShowcaseReadRepository.cs:56-58`); `AsNoTracking` (`PublishedAdReader.cs:17`); sem N+1 (uma consulta por lista, capa por `OUTER APPLY TOP (@CoverCount)`, `AdCardSql.cs:16-17`); página limitada (`SqlBuilder.cs:100-104`); índice provado (`ShowcaseQueryTests.cs:224`, `SearchQueryTests.cs:568`); desempenho medido (6 ms e 52 ms) | 🟢 19, 20 |
| frontend.md | PASS | Um módulo por visão (`Ad/Index.cshtml:24-27`, `Favorites/Index.cshtml:9-11`, `Search/Index.cshtml:44-46`); sem `style=`, `onclick=` nem `<script>` inline nas views novas; ganchos `data-*`; Bootstrap com BEM só no que ele não cobre; sem `innerHTML` (`favorites.js:37`, varredura `NenhumModuloUsaInnerHtmlComTextoDoServidor`); `rel="noopener noreferrer"` (`_Contact.cshtml:11`); funciona sem JavaScript (`SearchE2ETests.cs:448`, `FavoritesE2ETests.cs:354`, `AdDetailE2ETests.cs:255`); axe (`SearchE2ETests.cs:529`, `AdDetailE2ETests.cs:299`, `ContactE2ETests.cs:235`, `FavoritesE2ETests.cs:375`) | Avisos 🟢 15, 16, 17, 24 são de manutenção |
| git-workflow.md | WARNING | Commits convencionais com atribuição (por exemplo d53e89b, 6bdeaa5, 232db89); um assunto por commit. Falha: renomear de relatório preparado e sem commit; quatro commits com "work in progress" | 🟢 27 |
| monitoring.md | PASS | `LogError` estruturado com `traceId` e sem dado sensível (`AdController.cs:58`, `CategoryController.cs:33`, `HomeController.cs:31`, `SearchController.cs:34`, `:46`, `SeoController.cs:35`); correlation id em `Program.cs:81`; testes provam que a tela não mostra a exceção | 🟢 8 (volume de log) |
| naming-conventions.md | PASS | Rotas em português e kebab-case (`/categoria/{slug}`, `/busca`, `/favoritos`, `/anuncio/{id}/{slug}`, permitido por `lang-dotnet.md`); índice `IX_Ads_Status_PublishedAt_Id` (`AdConfiguration.cs:71`); chave `gazeta:favoritos:v1` (`favorites.js:6`); métodos de teste `Metodo_Cenario_Resultado` com prefixo `USxxxSnn` | Nomes de CSS e JS: 🟢 24 |
| output-style.md | PASS | Seções 5.1 a 5.6 do `TEST_REPORT.md` abrem com "Em resumo" (linhas 985, 1043, 1096, 1145, 1224, 1281); mensagens da interface em português claro; textos fixos de SEO em `SeoTexts.cs` | A falta da seção do Checkpoint 5 é o 🟡 2 |
| principles-and-practices.md | WARNING | §2.5 cumprido na forma: achados fora de escopo em `plans/BACKLOG.md:196-278` com origem e arquivo; YAGNI e §4.5 respeitados na migration aditiva. Falha: decisões aprovadas que mudam comportamento não voltaram à SPEC (item 15 de §2.5); API sem cliente | 🟡 3, 🟢 22 |
| project-structure.md | PASS | Interfaces no Core (`IShowcase`, `ISearch`, `ISitemap`), implementações na Infrastructure e registro em `ServiceCollectionExtensions.cs:68-74`; Core sem referência a EF Core ou Dapper; controllers só montam `ViewModel` | 🟢 12 (tipos por arquivo) |
| security.md | PASS | Só publicado em toda leitura (`SearchReadRepository.cs:58`, `ShowcaseReadRepository.cs:33`, `:56`, `:65`, `SitemapReadRepository.cs:22`, `:35`, `PublishedAdReader.cs:15-18`, `PhotoDelivery.cs:31-38`) e testes `FavoritesQueryTests.cs:75`, `SitemapQueryTests.cs:78`, `SearchQueryTests.cs:170`, `AdDetailQueryTests.cs:69`; SQL com parâmetros e ordem de lista fixa (`SearchReadRepository.cs:40` e `:49-54`; `SearchQueryTests.cs:377`, `:390`); limites de entrada e tempo limite; XSS (`AdDetailTests.cs:477`, `FavoritesTests.cs:307`, `SearchTests.cs:466`); cabeçalhos e CSP em toda resposta (`SecurityHeadersMiddleware.cs`); nenhuma política CORS; 404 igual (`AdDetailTests.cs:269`); `Checkpoint5PublicTests.cs:31-70` | Sem 🟡 de segurança; ver 🟢 6, 7, 8, 23 |
| system-design.md | PASS | Tempo limite de 10 s e cancelamento (`AdCardSql.cs:14`, `SitemapReadRepository.cs:23`, `SearchQueryTests.cs:498`); 503 com "Tentar novamente" e filtros preservados (`SearchController.cs:30-50`); limite global de 100 por minuto (`RateLimitingExtensions.cs:61`) e das fotos (`:80`); paginação fixa de 24 com ordem estável | Volume: 🟢 21 |
| tech-stack.md | PASS | Nenhum `csproj`, `Directory.Packages.props` nem `global.json` no diff (`git diff --name-only 731522d..HEAD`); Dapper, EF Core, MSTest, Playwright e Bootstrap são os aprovados; sem framework de JavaScript nem biblioteca nova | Nenhuma dependência nova |
| testing.md | WARNING | Bom: pirâmide (HTTP com repositório falso que não filtra, integração no SQL Server real, E2E com e sem JavaScript), nomes por cenário, 73 mutações, host isolado (`WebFactory.cs` troca os três repositórios). Falha: cobertura numérica da fase não medida; regra de paridade sem tabela em `limpar` × `FavoriteIds`; laço de lotes sem teste | 🟡 2, 4, 5, 🟢 18 |
| overrides/lang-dotnet.md | WARNING | Bom: `CancellationToken` por último (`SearchService.cs:40`, `ShowcaseService.cs`), construtores primários, `record` para DTOs (`AdCardDto.cs`), `sealed`, identificadores em inglês e rotas em português (`CategoryController.cs`), `Nullable` desativado sem `?`. Falha: `HomeController` sem `sealed` e com namespace em bloco; nomes de CSS e JavaScript em português | 🟢 12, 14, 24 |
| overrides/database-sqlserver.md | PASS | Migration só aditiva com `Down` (`20261005161939_AddPublishedAtIndex.cs`), `IX_{Table}_{Columns}` (`AdConfiguration.cs:71`), consultas parametrizadas, `AsNoTracking`, script idempotente atualizado (`db/scripts/gazeta-idempotente.sql`) | 🟢 19 (tipo do parâmetro `@Uf`) |
| overrides (Node.js, PHP, Oracle, MySQL, PostgreSQL, MongoDB, ELK) | N/A | `PROJECT_PROFILE.md` não os declara | Fora do Profile |

## 7. Approval Status

| Decision | APPROVE |
|----------|---------|
| Conditions (if any) | 0 🔴. Antes do `/scan` o orquestrador corrige ou aceita explicitamente os 5 🟡 (Gate 7): recomendo corrigir já o 1 (barato, uma linha de guarda mais três linhas de teste), o 2 (rodar a cobertura e escrever a seção do Checkpoint 5), o 4 e o 5 (um E2E e uma tabela); o 3 depende do Product Owner (emenda v1.5 da SPEC com as quatro decisões já aprovadas). Registrar no BACKLOG os 23 itens 🟢 (P2). Nenhuma decisão de produto nova foi tomada pelo revisor. |

## Atualização do orquestrador (2026-10-06)

- **Aviso 🟡 1 (estouro de `decimal`):** **reproduzido em execução** com um teste descartável (`DecimalInput.TryParseCents("7000000000000000000000000000", …)` lança `OverflowException`). Sem correção de código, como combinado (só relatar); aguarda a decisão do Product Owner.
- **Aviso 🟡 2 (cobertura e seção do Checkpoint 5):** **resolvido.** A cobertura foi medida (união dos unitários e da integração: **97,9% de linhas e 91,5% de ramos**, 13 métodos a 0%, nenhum de regra de negócio sem justificativa) e a seção "Checkpoint 5" está em `reports/TEST_REPORT.md`.
- **Achado 🟢 27 (estado do repositório):** resolvido. O relatório da Fase 4 foi preservado em `reports/CODE_REVIEW-FASE-4.md` e entrou no commit.
- **Avisos 🟡 3, 4 e 5 e os 🟢 restantes:** abertos, sem correção de código, no BACKLOG (seção "Revisão do Checkpoint 5") aguardando a decisão do Product Owner.
- **Itens de teste achados fora da revisão e já corrigidos:** o E2E `US003S03` falhava em 11 de 20 rodadas (leitura antes do evento `close`) e agora passa em 20 de 20; a espera da página de confirmação entre os dois cliques de "Enviar para revisão" (falha recorrente na etapa "Anúncio enviado para revisão") foi corrigida nos 6 roteiros que a repetiam.

## Atualização do orquestrador — decisões do Product Owner (2026-10-06)

- **Aviso 🟡 1 (`OverflowException`):** corrigido (comparação antes da multiplicação), com teste de 27 a 29 dígitos no preço mínimo e no máximo.
- **Aviso 🟡 3 (decisões fora da SPEC):** resolvido, SPEC v1.5 com as quatro emendas.
- **Aviso 🟡 4 (mais de 100 favoritos):** resolvido, testes do servidor e E2E de 150 e de 101 ids.
- **Aviso 🟡 5 (paridade dos ids dos favoritos):** resolvido, tabela de 48 entradas; o C# passou a recusar id 0 e zero à esquerda.
- **Avisos 🟡 2 e os 23 🟢:** 🟡 2 resolvido antes; os 🟢 seguem no BACKLOG. **Todos os 🟡 estão fechados.**

---

# Code Review — Checkpoint 6: verificações transversais (tarefas 6.1 a 6.3, duas correções de produto e `dotnet format`)

**Data**: 2026-10-06
**Revisor**: agente Code Reviewer (Five-Axis Framework)
**Escopo**: `git log 17fb505..bbf64ea` (25 commits desde o fim do Checkpoint 5; 72 arquivos: 23 em `src/` com +131 / -33 e 41 em `tests/` com +2.940 / -29; o resto é `plans/`, `reports/`, `specs/`, `architecture/` e `docs/`). O HEAD no momento da revisão é `0949819`: os dois commits por cima de `bbf64ea` (`0e2e38e` e `0949819`) mexem só em documentação (`plans/plan.md`, `plans/BACKLOG.md`, `plans/BACKLOG-TRIAGEM-FASE-6.md`) e foram lidos como insumo.
**Inputs**: specs/SPEC.md (v1.6; US-002-S12, NFR-01 a NFR-05, NFR-10, NFR-11, NFR-13, NFR-15, NFR-16, NFR-17) · architecture/ARCHITECTURE.md §7 e ADR-005, ADR-006 · plans/plan.md (blocos 6.1 a 6.3 e Checkpoint 6) · plans/todo.md · plans/BACKLOG.md (linhas 300 a 337) e `plans/BACKLOG-TRIAGEM-FASE-6.md` · security/PRE_DEV_REVIEW.md (RC-15, RC-17, A01, A03, A05) · reports/TEST_REPORT.md (seções 6.1, 6.2 e 6.3, linhas 1426 a 1565; ainda **não existe** a seção "Checkpoint 6")
**Método**: leitura do código de produto e dos testes do escopo, dos relatórios e do BACKLOG; `dotnet build -c Release` (compilou, 0 avisos, 0 erros) e `dotnet format GazetaMarketplace.slnx --verify-no-changes` (saída 0, nada a formatar). Conforme a orientação recebida, **a suíte de testes não foi executada** (vale o `TEST_REPORT.md`; a cobertura nova é medida por outro responsável) e **nenhum arquivo de `src/` ou `tests/` foi alterado**. Os achados 1 a 3 vêm da leitura do código e dos testes e **não foram reproduzidos em execução**.

## 1. Executive Summary

**Veredito: APPROVE com condições.** O Checkpoint 6 entrega o que o plano pede e com prova forte: a matriz de acesso descobre as rotas por reflexão (78 hoje, a tabela escrita à mão manda) e falha com rota nova; a matriz de posse nega ao Redator B as 13 rotas do anúncio da Redatora A sem revelar nem mudar nada; texto hostil nunca vira marcação em 40 telas e a varredura de saída crua tem lista de exceções vazia; as 45 telas ficam numa lista única, medidas com axe (WCAG 2.1 A e AA) e em quatro larguras; os orçamentos de desempenho moram em um `budgets.json` só. Os controles transversais **estão ligados no pipeline** (`Program.cs:46-125`, tabela na §6) e cada um tem teste. As duas correções de produto (painel de filtros recolhido no HTML e página amigável para respostas de erro em branco) funcionam, têm teste e estão no `SPEC` e no `BACKLOG`. O `dotnet format` é só forma: li os 25 arquivos e conferi com `--verify-no-changes`. **Não há defeito que cause vazamento ou perda de dados (nenhum 🔴).** Os **7 🟡**: (1) o cache imutável de um ano vale para o módulo de página (`?v=`), mas não para os módulos compartilhados que ele importa, e isso gera versões misturadas depois de um deploy; (2) nenhum teste falha se as capas dos cards perderem `width`/`height`, e a mutação que provava isso sobreviveu e foi trocada por outra; (3) a "foto de câmera granulada" do teste de peso gera uma miniatura de 1 KB, o que não prova nada sobre o limite de 74 KB, e o comentário afirma o contrário; (4) a linha v1.6 do histórico da SPEC não tem `Reference` nem `Approved by`; (5) a página de status (404 e outros códigos) não tem critério na SPEC nem entrada no ARCHITECTURE; (6) o `TEST_REPORT.md` não registra as duas correções de produto, e três números (253 "passaram", 44 telas, 77 rotas) já não batem com o disco; (7) 12 usos do operador `!` nos testes novos, contra `code-style.md` e `testing.md`.

🔴 0 · 🟡 7 · 🟢 14 · ✅ 12

## 2. Five-Axis Scores

| # | Axis | Score (1–5) | One-line justification |
|---|------|-------------|------------------------|
| 1 | Correctness | 3 | Os cenários e as NFRs do escopo têm caminho ligado e teste com efeito (matrizes, XSS, telas, volume), mas cinco 🟡 caem aqui: capas sem prova de dimensão (2), prova vazia da miniatura (3), SPEC v1.6 sem aprovação (4), página de status sem critério (5) e relatório de testes sem as correções (6); somam-se os 🟢 8, 11, 15, 16 e 21. |
| 2 | Readability | 4 | Comentários de motivo, nomes claros e testes que dizem por que existem; o 🟡 7 (`!` proibido pelas regras) e os 🟢 12, 13 e 18 (`Status` repete a mesma condição quatro vezes, `!important` desnecessário, documentos com "44 telas") impedem o 5. |
| 3 | Architecture | 4 | Camadas respeitadas (middleware novo na Web, orçamentos compartilhados por link de arquivo, nada no Core); o 🟡 5 (comportamento e decisão de compressão sem SPEC e sem ARCHITECTURE) e o 🟡 1 (estratégia de cache sem invalidação do grafo de módulos) mantêm a nota em 4. |
| 4 | Security | 4 | NFR-13, NFR-15, NFR-10 e NFR-11 provados no código e por teste, com cabeçalhos, CSP, antiforgery, limitador e BREACH ligados no pipeline; sem 🟡 de segurança. Ficam os 🟢 9 (compressão decidida pelo caminho), 10 (`/Home/Status/{código}` fabrica 4xx e 5xx), 14 (nada varre `style=`) e 19 (itens aceitos da 6.1). |
| 5 | Performance | 3 | Compressão, cache e p95 de 34,6 ms medidos, mas o 🟡 1 (versões misturadas de JavaScript por um ano), o 🟡 2 (CLS das capas sem prova) e o 🟡 3 (peso da miniatura sem prova) são desempenho; os 🟢 17 e 20 completam. |

## 3. Findings (by severity: 🔴 → 🟡 → 🟢 → ✅)

### 🔴 Critical

Nenhum.

### 🟡 Warning

1. **`src/GazetaMarketplace.Web/Middleware/PerformanceExtensions.cs:40-58` (com `Views/Shared/_Layout.cshtml:62`, `Views/Search/Index.cshtml:45`, `wwwroot/js/pages/*.js:3-6` e `architecture/ARCHITECTURE.md:239`) — o cache imutável de um ano vale para o módulo de página, mas não para os módulos compartilhados que ele importa.** `UseVersionedStaticAssetCache` dá `public, max-age=31536000, immutable` a todo arquivo estático pedido com `?v=`. Só os módulos de **entrada** são pedidos assim (`<script type="module" src="~/js/pages/search.js" asp-append-version="true">`, e o `v` é o hash **desse arquivo**). Os módulos que eles importam (`import { apiFetch } from "../modules/api.js"`, `favorites-ui.js`, `ad-gallery.js`, `cep.js`…) são pedidos pelo navegador **sem** `?v=` e continuam em `no-cache`. Resultado: se um deploy mudar só um módulo compartilhado (por exemplo `api.js`), o `v` de `search.js` e de `layout.js` **não muda**, o navegador de quem já visitou mantém por um ano o módulo de página antigo e baixa o módulo compartilhado novo; um `export` renomeado ou um contrato alterado quebra a página inteira (`SyntaxError` na importação) sem nenhum jeito de o visitante se recuperar além de limpar o cache. Antes desta tarefa o módulo de página também revalidava, então o defeito é novo. O ARCHITECTURE §7 afirma "o endereço muda quando o conteúdo muda, então o cache longo é seguro": isso vale para CSS e para arquivos sem importação, não para o grafo de módulos. Nenhum teste cobre: `PageWeightTests.VersionedFiles_HaveImmutableCache_OnEveryPublicPage` (`:116`) só confere o cabeçalho de quem já tem `?v=`. Ainda não há visitante com cache (primeira publicação pendente), por isso não é 🔴; mas a correção precisa vir antes do `/deploy`. — **Recomendação:** o mais simples é limitar o cache imutável a `/css/`, `/lib/` e fontes e deixar `/js/` em `no-cache` (revalidação com ETag, 304); a alternativa é derivar um único `v` da pasta `wwwroot/js` inteira e usá-lo em todo `<script type="module">` (qualquer mudança em qualquer módulo muda o endereço de todas as entradas). Acrescentar um teste que falha se um módulo importado de uma entrada não tiver a mesma política da entrada, e corrigir a frase do ARCHITECTURE. — *Relates-to: NFR-01, NFR-05, Task 6.3*

2. **`src/GazetaMarketplace.Web/Views/Shared/Components/AdCard/Default.cshtml:12`, `wwwroot/css/components/card.css:7` e `tests/GazetaMarketplace.Web.Tests.Playwright/Performance/VitalsTests.cs:51` — nenhum teste falha se as capas dos cards perderem `width`, `height` e `aspect-ratio`.** O TEST_REPORT conta que a mutação "tirar `width`, `height` e `aspect-ratio` das capas" **não foi pega** (as miniaturas do E2E são minúsculas, chegam antes da primeira pintura e não deslocam nada) e que ela foi **trocada** por outra (M5, bloco que entra 800 ms depois), e não classificada como equivalente. Não é equivalente: a NFR-03 diz "elementos não pulam enquanto as fotos carregam", e é exatamente o que essa mutação quebraria com fotos reais. Procurei um teste mais barato e não existe: `ShowcaseTests.cs:105` e `:115` só contam os `<img data-ad-card-image>` e leem `loading`; só a página de detalhe tem a afirmação de `width="1600" height="1200"` (`AdDetailTests.cs:152`). — **Recomendação:** um teste unitário que lê o HTML do card e exige `width` e `height` numéricos no `<img data-ad-card-image>` (e, para a galeria e a lista de miniaturas, o mesmo), ou, no E2E, o mesmo truque do `SearchLayoutShiftTests`: atrasar `**/fotos/**` em 2 s e conferir CLS abaixo de 0,1; registrar a mutação original como morta. — *Relates-to: NFR-03, Task 6.3*

3. **`tests/GazetaMarketplace.Web.Tests/Performance/PhotoWeightBudgetTests.cs:12`, `:18-27` e `:30-49` — a "foto de câmera granulada" gera uma miniatura de 1 KB, e o teste não consegue falhar pelo peso.** O comentário da classe diz "uma foto de câmera de 4000 × 3000 px com muito grão (a pior que o limite de 10 MB deixa passar)". Mas `GrainyCameraPhoto` cria ruído em 400 × 300, **amplia** para 4000 × 3000 (o ruído vira mancha suave) e só então soma ruído gaussiano de 1,2; ao reduzir para 480 px o ruído se anula. O TEST_REPORT mede a miniatura em **1 KB** contra um limite de **74 KB** por capa (`TEST_REPORT.md:1535`): uma folga de 74 vezes, e uma foto de verdade pesa de 20 a 40 KB nessa largura. Se alguém subisse a qualidade do WebP para 100, a miniatura sintética continuaria abaixo de 74 KB e o teste passaria. O TEST_REPORT é honesto sobre isso (`:1551`, "mais leve que uma foto de verdade"), e o BACKLOG manda medir com fotos reais no `/verify` (F6-14); falta corrigir o teste e o comentário, que hoje afirmam um pior caso que não existe. A versão grande (279 KB) é mais plausível, mas pelo mesmo motivo não é o pior caso. — **Recomendação:** gerar o ruído **na resolução da miniatura** (por exemplo 960 × 720, reduzindo a 480 px sem suavizar) ou usar uma foto real pequena como fixture versionada; mudar o comentário para dizer o que o teste prova (que a miniatura e a versão grande têm a largura certa e o peso é da ordem esperada); manter F6-14 para o `/verify`. — *Relates-to: NFR-05, Task 6.3*

4. **`specs/SPEC.md:31` (histórico de revisões) — a linha v1.6 tem 7 colunas em vez de 8: faltam `Reference` e `Approved by`.** O cabeçalho (`:19`) tem 8 colunas e as linhas v1.0 a v1.5 trazem `Approved by` (por exemplo "Product Owner, 2026-10-05"); a v1.6 termina em `| US-002, NFR-03 |` e a tabela fica torta. O próprio BACKLOG dizia que a correção "precisa de decisão do Product Owner" (`BACKLOG.md:337`, antes do `[x]`) porque ela mexe numa decisão testada da US-002-S12, e o histórico diz que "uma linha representa um conjunto de mudanças **aprovado**" (`SPEC.md:17`). Não encontrei no repositório o registro da aprovação, nem o commit `79798a0` a cita. `rules/principles-and-practices.md` §2.5 item 15 exige a emenda **e** a aprovação. O texto da emenda e o `Business Rules` da S12 (`:276`) estão certos e batem com o código e com `SearchTests`. — **Recomendação:** completar a linha com `Reference` (`Tarefa 6.3`, `SearchLayoutShiftTests`) e `Approved by` (o Product Owner e a data da decisão); se a aprovação não existe, parar e levar ao Product Owner antes do `/scan`. — *Relates-to: US-002-S12, NFR-03, Task 6.3*

5. **`src/GazetaMarketplace.Web/Controllers/HomeController.cs:45-65`, `Program.cs:91-94` e `architecture/ARCHITECTURE.md` — a página de status ("Página não encontrada" e "Algo deu errado") é comportamento novo sem critério na SPEC e sem entrada no ARCHITECTURE.** A SPEC só descreve "Categoria não encontrada" (US-001-S07) e a falha de carga com "Tentar novamente" (US-001-S06, US-002-S11); o texto novo ("O endereço não existe ou foi alterado…", "Não foi possível concluir o que você pediu…") e a regra de quando ele aparece (qualquer resposta 4xx/5xx em branco fora de `/api`, `/health` e endereços com extensão) não estão em nenhum critério nem em suposição aprovada. O motivo é legítimo: `rules/frontend.md` §Error Handling pede `UseStatusCodePagesWithReExecute` e o item foi achado na 6.2 (`BACKLOG.md:325`); por isso não é 🔴. Mas `code-review-checklist.md` pede que comportamento novo tenha critério ou suposição aprovada, e o ARCHITECTURE §7 (linhas 238-239) descreve a compressão e o cache desta tarefa e **não** descreve nem a nova etapa do pipeline (`Program.cs:92-94`) nem a rota `/Home/Status/{código}`. — **Recomendação:** acrescentar um critério curto à SPEC (por exemplo na US-001, "endereço que não existe mostra a página de erro em português com o caminho para a página inicial", e a mesma ideia para o painel) com a aprovação do Product Owner, e uma linha no ARCHITECTURE §7 com o predicado do `UseWhen`. — *Relates-to: US-001-S07, Task 6.2*

6. **`reports/TEST_REPORT.md:1426-1565`, `:1469`, `:1442` e `:1522` — o relatório não registra as duas correções de produto do checkpoint e três números já não batem com o disco.** (a) Não há seção para `79798a0` (painel recolhido no HTML, `SearchLayoutShiftTests`, mutação contra o código antigo) nem para `c7c32b2` (`StatusPagesTests`, a tela nova na lista), e o texto da 6.3 (`:1553`) ainda diz "Não corrigi"; (b) a tabela da 6.3 diz "253 | 253 | 0 (4 puladas)": 253 é o **total**, e com 4 puladas os que passaram são 249; (c) a 6.2 fala em "44 telas" e a 6.1 em "77 linhas" de matriz, e hoje o disco tem 45 telas (`screens.json`) e 78 linhas (`AccessMatrixTests.cs:38-127`, com `Home.Status`); (d) pela soma do disco o total esperado é 1.707 unitários (1.702 + 5 de `StatusPagesTests`) e 256 de navegador (253 + `SearchLayoutShiftTests` + axe e largura da tela nova): são contas minhas, não medidas. A seção "Checkpoint 6" está a cargo de outro responsável, e este achado pede que ela cubra esses pontos. — **Recomendação:** escrever a seção com cobertura de linhas e ramos, a lista de métodos a 0%, os totais da última rodada completa **depois** das duas correções, a distinção entre "passaram" e "puladas", a tabela de mutações das correções e os números corrigidos (45 telas, 78 rotas); só então marcar o Checkpoint 6 no `plans/plan.md:2036-2043`. — *Relates-to: Task 6.2, Task 6.3, Checkpoint 6*

7. **`tests/GazetaMarketplace.Web.Tests.Playwright/Support/PageMeter.cs:73`, `:79`, `:92`; `Support/ScreenData.cs:119`, `:122`, `:129`, `:133`, `:149`, `:167`, `:181`; `tests/GazetaMarketplace.Web.Tests/Security/AccessMatrixTests.cs:178`, `:232` — 12 usos do operador `!` (null-forgiving) em código novo.** `rules/code-style.md` §Nullable diz "Do **not** add … the null-forgiving operator (`!`)" e `rules/testing.md` diz "no `?` annotations or `!` in tests"; `lang-dotnet.md` repete a regra. Com `Nullable` desativado o `!` não faz nada e não gera aviso, então o compilador não acusa; a revisão do Checkpoint 5 conferiu a ausência de `!` nas linhas novas e esta fase abriu uma exceção sem decisão. — **Recomendação:** remover os 12 `!` (comportamento idêntico) e, se quiser travar, uma varredura simples no estilo `RawOutputTests` para `)!` em `.cs`. — *Relates-to: Task 6.1, Task 6.2, Task 6.3*

### 🟢 Suggestion

8. **`tests/GazetaMarketplace.Web.Tests/Performance/CompressionAndCacheTests.cs:112` — o teste "nenhuma página pública leva o token" engole as respostas com falha.** `GetStringAsync(url).ContinueWith(t => t.IsFaulted ? string.Empty : t.Result)` transforma o 404 de `/anuncio/999999/x` e de `/categoria/nao-existe` (ambos na lista `PublicPages`, `:17`) em texto vazio, e uma página vazia não tem token: o teste passa sem olhar essas duas. O risco real é baixo (`StatusPagesTests.cs:20-36` confere que a página de 404 não leva o token), mas o teste afirma mais do que faz. — **Recomendação:** ler com `GetAsync` e comparar o corpo qualquer que seja o status, como o primeiro teste da classe. — *Relates-to: NFR-01, Task 6.3*

9. **`src/GazetaMarketplace.Web/Middleware/PerformanceExtensions.cs:37-38` (com `Program.cs:92-97`) — a exclusão do BREACH é decidida pelo caminho do pedido, e as páginas reexecutadas têm o caminho trocado.** `UsePublicResponseCompression` pula só o que começa com `/painel`. Quando o painel responde 404 ou erro, o `UseStatusCodePagesWithReExecute` e o `UseExceptionHandler` reexecutam o pedido em `/Home/Status/404` ou `/Home/Error`, e nesse segundo passo a compressão **liga**. Hoje é seguro: essas duas views usam o `_Layout` público, que não tem token (`_PanelLayout.cshtml:14` é o único que o gera). Mas a segurança depende de ninguém pôr o token nessas views, e nenhum teste prova isso para a resposta de um erro do painel: `StatusPagesTests.cs:38-47` não confere o cabeçalho `Content-Encoding` nem a ausência do token. — **Recomendação:** acrescentar essas duas conferências ao teste do painel com 404 e um comentário em `UsePublicResponseCompression` dizendo por que a regra por caminho basta. — *Relates-to: NFR-01, Task 6.3*

10. **`src/GazetaMarketplace.Web/Controllers/HomeController.cs:49-65` — `/Home/Status/{código}` pode ser chamada direto com qualquer código de 400 a 599.** Um `GET /Home/Status/503` devolve 503 com a mensagem "Algo deu errado" e um "Código de referência" que não existe em nenhum registro (só as falhas lançadas pelo `ExceptionHandler` têm o `traceId` no log); um robô que varre essa rota infla a taxa de 5xx que `monitoring.md` usa para alertar. Para os 4xx que não são 404 (por exemplo 400 de antiforgery vencido num formulário aberto por muito tempo) a mensagem "Tente de novo em alguns instantes" não ajuda; e dentro de `/painel` a página sai com o layout público e o botão leva ao início do site, não ao painel (`IStatusCodeReExecuteFeature.OriginalPath` permitiria escolher). — **Recomendação:** só atender quando `IStatusCodeReExecuteFeature` existir (senão 404), mostrar o código de referência só para 5xx e escolher a mensagem e o destino pela classe do código e pela origem. — *Relates-to: NFR-18, Task 6.2*

11. **`tests/GazetaMarketplace.Web.Tests/Middleware/StatusPagesTests.cs:65-78` e `Program.cs:93` — o teste "ApiHealthAndFiles" não tem nenhum endereço de saúde, e o predicado tem arestas sem teste.** A lista é `/api/v1/nao-existe`, `/css/nao-existe.css`, `/js/nao-existe.js`, `/wp-login.php`; a cláusula `!StartsWithSegments("/health")` (apagá-la não derruba nada, porque `/health/live` e `/health/ready` já devolvem corpo) e o comportamento de `Path.HasExtension` com rota do app (por exemplo `/categoria/a.b` continua em branco) não estão provados. Cada 404 tratado também passa duas vezes pelo limitador global (a reexecução volta ao pipeline em `Program.cs:113`), gastando 2 das 100 permissões por minuto. — **Recomendação:** acrescentar `/health/nao-existe` (que hoje seria reexecutado sem a cláusula) e `/categoria/a.b` ao teste, e registrar a contagem dupla no comentário do limitador. — *Relates-to: Task 6.2*

12. **`src/GazetaMarketplace.Web/Controllers/HomeController.cs:16`, `:52-63` — `Status` repete `code == StatusCodes.Status404NotFound` quatro vezes e o título duas vezes; o arquivo ainda tem namespace em bloco e a classe sem `sealed`.** `rules/clean-code.md` pede uma variável explicativa e `code-style.md` pede `namespace …;` (o 🟢 14 da revisão do Checkpoint 5 segue aberto, e o arquivo foi tocado de novo). — **Recomendação:** `bool notFound = code == 404;`, um só título, e converter o namespace e o `sealed` já que o arquivo mudou. — *Relates-to: Task 6.2*

13. **`src/GazetaMarketplace.Web/wwwroot/css/sem-js.css:21-24` — `display: none !important` sem necessidade.** `rules/frontend.md` pede evitar `!important` (exceto os utilitários do Bootstrap). Os botões `[data-filters-toggle]` e `[data-filters-close]` só têm `.btn` (especificidade igual, vem antes) e `d-lg-none` (que já é `!important` e só vale em telas largas), então um `display: none` simples, carregado depois do Bootstrap, já vence. — **Recomendação:** tirar o `!important` e conferir no `SearchE2ETests` sem JavaScript. — *Relates-to: US-002-S12, Task 6.3*

14. **`tests/GazetaMarketplace.Web.Tests/Security/RawOutputTests.cs:67-90` — a varredura das views não procura `style="…"`.** A CSP (`SecurityHeadersMiddleware.cs:13`, `style-src 'self'`) bloqueia estilo escrito direto no HTML, e o TEST_REPORT diz que uma mutação por `style=` "não muda nada" (`:1508`). Hoje não há nenhum `style=` em `Views/**/*.cshtml` nem em `wwwroot/js` (conferido por busca), mas um futuro `style="width: 50px"` seria ignorado em silêncio pelo navegador, e só uma tela torta (ou o teste de larguras) o acusaria. — **Recomendação:** um quarto teste da classe com `<[^>]+\sstyle\s*=` (com a mesma lista de exceções vazia). — *Relates-to: NFR-10, Task 6.1*

15. **`reports/TEST_REPORT.md:1502` — a primeira forma da mutação M3 (tirar o rótulo do campo de busca) foi classificada como "equivalente" por o campo ter `placeholder`.** É um limite do **oráculo** (o axe aceita `placeholder` como nome acessível), não uma mutação equivalente: um campo só com `placeholder` falha o WCAG 3.3.2 para quem usa leitor de tela e some do `GetByLabel`. O resultado final está certo (a segunda forma foi morta); falta só a classificação. — **Recomendação:** registrar como "sobrevivente por limite do axe" e manter o campo de busca na lista do NVDA do `/verify` (F6-17). — *Relates-to: NFR-16, Task 6.2*

16. **`tests/GazetaMarketplace.Web.Tests.Playwright/Performance/PageWeightTests.cs:24`, `:49`, `:75`, `VitalsTests.cs:25`, `:66` e `Accessibility/AllScreensTests.cs:28` — testes que viram `Inconclusive` ou nem rodam em vez de falhar.** A categoria `/categoria/livros-e-revistas` está escrita nos dois arquivos e depende de o banco do E2E ter 24 anúncios nela; sem isso o orçamento da lista fica "inconclusivo" (e o verde não avisa); sem `GAZETA_DEV_BASE_URL` a tela do catálogo de componentes some do axe e das larguras (2 casos); e `[RequiresVariables]` **ignora** o teste inteiro quando faltam as variáveis (`RequiresVariablesAttribute.cs:12-25`). `VitalsTests` só roda com `GAZETA_VITALS=1` e o `plan.md:2021` já marca como `[x]` "LCP, INP e CLS medidos no `/verify`", que ainda não existe. A divulgação no TEST_REPORT é boa; o risco é um verde que esconde teste não executado. — **Recomendação:** o `/test` e o `/verify` listam os testes pulados e inconclusivos como pendência explícita na seção do Checkpoint 6, e a caixa do `plan.md:2021` só é marcada depois do `/verify`. — *Relates-to: NFR-01, NFR-05, NFR-16, Task 6.2, Task 6.3*

17. **`src/GazetaMarketplace.Web/wwwroot/css/poppins.css:8`, `Views/Shared/_Layout.cshtml:8-9` — fontes e arquivos de `/lib` não têm `?v=` e ficam em `no-cache`.** As quatro fontes (99 KB, mais da metade do que a página baixa sem foto) são pedidas pelo `url()` do CSS e pelo `preload`, sem versão; o navegador pergunta ao servidor por cada uma a cada visita. `PageWeightTests.VersionedFiles…` (`:116-133`) só olha o que já tem `?v=`, então passa. Já está no BACKLOG como F6-55 (peso) mas não o ponto do cache. — **Recomendação:** servir `/lib/**` (versões fixas, nome com a versão do pacote) com cache longo por uma regra de caminho, ou pôr `?v=` no `preload` e no `url()` (junto com o 🟡 1). — *Relates-to: NFR-01, NFR-05, Task 6.3*

18. **`plans/plan.md:1978`, `:1988-1989`, `tests/GazetaMarketplace.Web.Tests.Playwright/Support/ScreenCatalog.cs:34` e o ARCHITECTURE — textos desatualizados e decisões sem ADR.** O plano e o comentário do `ScreenCatalog` ainda dizem "44 telas" (são 45); `ScreenListTests`/`ScreenCoverageTests` já usam a lista; as decisões do BREACH e do cache longo (ARCHITECTURE §7, "decisão do Product Owner, 2026-10-06") não têm ADR, e `rules/principles-and-practices.md` §2.4 item 13 pede um registro de cada decisão de arquitetura. — **Recomendação:** corrigir os dois textos e registrar um ADR curto ("compressão e cache do navegador") quando o 🟡 1 for decidido. — *Relates-to: Task 6.2, Task 6.3*

19. **`plans/BACKLOG.md:318-321` — três pontos de segurança da 6.1 seguem abertos e foram triados como aceitação (F6-20, F6-52).** (a) O site não tem `FallbackPolicy`: uma rota nova sem `[Authorize]` nasce pública e só o `AccessMatrixTests` (`:190`, depois de rodar) impede; (b) a negação por posse devolve 403 e revela que o id existe, para quem já está logado; (c) o envio de foto valida o arquivo antes do autor. Nenhum causa dano hoje (a matriz de posse prova que nada é gravado nem revelado além do id), e o conjunto está bem documentado. Registro aqui só para o `/scan` receber a lista. — **Recomendação:** o Product Owner decide o item (a) (F6-20); o `/scan` registra a aceitação de (b) e (c) no `SCAN_REPORT.md`. — *Relates-to: NFR-13, Task 6.1*

20. **`tests/GazetaMarketplace.IntegrationTests/Performance/VolumeOfV1Tests.cs:114-137` — o p95 de 34,6 ms mede o servidor em processo, sem compressão e sem rede.** O cliente de teste não envia `Accept-Encoding` (a compressão Brotli não entra na conta), as 200 fotos são linhas sem arquivo e o limitador global foi desligado de propósito (`:114`). O teste diz isso no cabeçalho ("mede o tempo do servidor") e a folga de 14 vezes cobre a diferença, mas a NFR-04 fala em "tempo de resposta do servidor" no site publicado. — **Recomendação:** repetir a medição no `/verify` contra o artefato (já em F6-13) e, no teste, enviar `Accept-Encoding: br` em metade dos pedidos. — *Relates-to: NFR-04, Task 6.3*

21. **`src/GazetaMarketplace.Web/Views/Search/Index.cshtml:79-80` — se o JavaScript falhar em carregar (rede ruim no celular), o painel de filtros nasce recolhido e o botão não faz nada.** O `<noscript>` cobre o JavaScript **desligado**, mas o Bootstrap só abre o painel depois que `bootstrap.bundle` carrega; antes da correção o painel nascia aberto no HTML e o filtro ficava utilizável mesmo sem o script. O `SearchLayoutShiftTests` prova o atraso de 2 s, não a falha. `rules/frontend.md` prefere elemento nativo onde ele resolve (`<details>`/`<summary>`). — **Recomendação:** avaliar `<details>` com o painel dentro (abre sem JavaScript e sem pulo de layout) ou, no mínimo, um teste que bloqueia `**/*.js` e confere que o formulário continua alcançável. — *Relates-to: US-002-S12, NFR-17*

### ✅ Good

- **`tests/GazetaMarketplace.Web.Tests/Security/AccessMatrixTests.cs:38-127`, `:158-183`, `:190`, `:218`, `:296`, `:343`, `:406`, `:440`** — a matriz de acesso é a **fonte da verdade escrita à mão** e o teste a compara com as rotas **descobertas** (`EndpointDataSource`) nos dois sentidos; uma segunda leitura confere que o `[Authorize]` do código bate com a tabela (`:218`); depois cada rota é chamada de verdade sem login, como Redator e como Administrador, e as negações são conferidas pelo contrato (redirecionamento para `/painel/entrar` ou `/painel/acesso-negado`, 401 e 403 com a mensagem, sem corpo). Toda escrita sem token antiforgery dá 400 (`:440`). Mutações M1 a M3, M7 e M8 mortas.
- **`tests/GazetaMarketplace.Web.Tests/Security/OwnershipMatrixTests.cs:29-44`, `:90-128`** — o Redator B chama as **13** rotas do anúncio da Redatora A (todas as de escrita que têm `{id}`), com foto de verdade para não parar na validação do arquivo, e o teste exige 403 ou 404, nenhuma palavra do anúncio na resposta **e o banco igual** (situação, título, descrição e foto); o par `:131` prova que a autora e o Administrador alcançam o mesmo anúncio, o que evita o falso positivo "negou tudo".
- **`tests/GazetaMarketplace.Web.Tests/Security/XssInAllScreensTests.cs:54-102`, `:143-178`, `:245-305`; `tests/GazetaMarketplace.Web.Tests.Playwright/Showcase/XssE2ETests.cs:41-52`** — o leitor de marcação procura tags e atributos de verdade (e não confunde texto codificado com atributo), `mustShow` exige que o texto **apareça** (a verificação não passa em página vazia), são 21 endereços públicos e 19 do painel mais as devoluções de formulário, e no navegador o teste olha janelas `alert`, `window.__xss` e a imagem do ataque, com o texto literal na tela. A CSP bloquearia o código sozinha, e por isso o teste do texto literal (não só o do código) é o que prova a codificação.
- **`tests/GazetaMarketplace.Web.Tests/Security/RawOutputTests.cs:18`, `:45-90`, `:93-108`; `src/GazetaMarketplace.Web/Areas/Panel/Views/Categories/Index.cshtml:35-90`** — varredura de `Html.Raw`, `HtmlString`, `AppendHtml`… em todas as views e em todo o código do site, com lista de exceções **vazia**, mais um teste que prova que as expressões enxergam cada forma de risco. Os cinco `Html.Raw` de texto fixo foram trocados por `@:` com a mesma marcação (conferi o diff linha a linha) para a exceção não existir; busca manual em `src/` não acha mais nenhuma saída crua nem `innerHTML`.
- **`tests/GazetaMarketplace.Web.Tests/Screens/screens.json`, `Security/ScreenCoverageTests.cs:43-107`, `tests/GazetaMarketplace.Web.Tests.Playwright/Support/ScreenCatalog.cs:128`** — a lista única de 45 telas alimenta o axe, as larguras e o teste de rotas; rota de página nova sem tela falha (`:43`), tela sem rota falha, quem abre cada tela tem de bater com a matriz de acesso (`:83`), e o filtro `GAZETA_SCREENS` permite repetir uma mutação sem rodar tudo. M1, M2, M4 a M7 mortas, com a mensagem apontando o elemento.
- **`src/GazetaMarketplace.Web/Middleware/PerformanceExtensions.cs:20-38`, `tests/GazetaMarketplace.Web.Tests/Performance/CompressionAndCacheTests.cs:70-101`** — a exclusão do BREACH tem a razão escrita no código e **no teste**: o teste confere que as páginas do painel não saem comprimidas **e** que ainda levam o token (se o token sumir, o teste avisa que "a razão da regra mudou"); o par `:104` confere que nenhuma página pública leva token. M2 e M7 mortas.
- **`tests/GazetaMarketplace.Web.Tests/Performance/Budgets.cs`, `budgets.json`, `tests/GazetaMarketplace.IntegrationTests/Performance/VolumeOfV1Tests.cs:105-138`** — os limites ficam num arquivo só, lido pelos três projetos (ligação de arquivo no `csproj`, sem pacote novo), e a mutação M6 prova que os testes **leem** o arquivo; o teste de volume usa SQL Server de verdade, aquece 20 pedidos, mede 200 de busca e 200 de detalhe e a M4 (atraso de 600 ms) o derruba com 629,6 ms.
- **`tests/GazetaMarketplace.Web.Tests.Playwright/Performance/SearchLayoutShiftTests.cs:27-55`, `src/GazetaMarketplace.Web/Views/Search/Index.cshtml:78-84`, `wwwroot/css/sem-js.css:15-24`, `wwwroot/js/pages/search.js:34-53`** — a correção do CLS é **determinística**: o teste atrasa todo `*.js` em 2 s (reproduz o pior caso sem depender da máquina), confere que o painel já está oculto e o botão visível **antes** do script e que o CLS fica abaixo do limite; o servidor desenha o estado (`aria-expanded`, rótulo "Filtros ▾" ou "▴" e `show` quando há erro), o JavaScript só sincroniza, e o `<noscript>` reabre o painel. O `SearchTests` (`US002S12_…`) trava o HTML novo.
- **`tests/GazetaMarketplace.Web.Tests/Middleware/StatusPagesTests.cs:20-91`, `Program.cs:92-94`** — a página amigável mantém o status (404 continua 404), nunca responde 200 (`:80`), não vaza o motivo interno do 500 (`:50`, "segredo-interno-123") e deixa de fora a API, a saúde e os arquivos (`:65`); `HeadersTests.TodaResposta_TemOsCabecalhosObrigatorios` já inclui `/nao-existe`, o que prova cabeçalhos de segurança também na página reexecutada.
- **`bbf64ea` e os 25 arquivos que ele toca** — o `dotnet format` ficou num commit só, com título e corpo honestos ("só forma"): 13 migrations e 5 arquivos de teste perdem o BOM, e o resto é ordem de `using`, um inicializador por linha e a ordem em `ServiceCollectionExtensions.cs:8-10`. Li cada hunk (`git show bbf64ea -w`): nenhuma linha de lógica muda. `dotnet format --verify-no-changes` sai com 0 e `dotnet build -c Release` compila com 0 avisos.
- **`reports/TEST_REPORT.md:1461`, `:1502`, `:1508`, `:1551-1553`, `:1563`** — o relatório declara os limites do que foi provado (fotos de E2E minúsculas, cache do navegador não provado com certificado de desenvolvimento, LCP/INP/CLS como amostra) e conta as mutações cuja **primeira forma** foi trocada (M4 da 6.1, M3 da 6.2, M5 da 6.3). É o tipo de honestidade que deixa esta revisão apontar só o que falta.
- **Controles transversais ligados no pipeline** — `Program.cs:46-58` (antiforgery global), `:65` e `:113` (limitador), `:66` e `:97` (compressão), `:84` (cabeçalhos e CSP), `:85-94` (erros e páginas de status), `:96` (HTTPS), `:108` (cache de estáticos versionados), `:109` (limite de corpo), `:111` e `:114` (autenticação e autorização): todos registrados **e** usados, na ordem certa (a compressão fica depois do redirecionamento HTTPS; o limitador, depois da autenticação por causa da política por usuário).
- **`tests/GazetaMarketplace.Web.Tests/Performance/CompressionAndCacheTests.cs:123-137`** — o cache imutável é provado nos quatro lados: arquivo com `?v=` (um ano e `immutable`), sem `v`, inexistente e página com `?v=` (nenhum dos três recebe o cabeçalho).

## 4. Action Items

- [ ] **P0**: nenhum.
- [ ] **P1** (antes do `/deploy`; o 1 também antes do `/scan` se possível): tirar o cache imutável de `/js/` ou versionar o grafo de módulos e acrescentar o teste (achado 1).
- [ ] **P1**: teste unitário de `width` e `height` nas capas dos cards (ou atraso das fotos no E2E) e registrar a mutação original como morta (achado 2).
- [ ] **P1**: refazer a foto sintética do `PhotoWeightBudgetTests` com ruído na resolução da miniatura e corrigir o comentário da classe (achado 3).
- [ ] **P1**: completar a linha v1.6 da SPEC com `Reference` e `Approved by`, ou levar a aprovação ao Product Owner (achado 4).
- [ ] **P1**: critério na SPEC (com aprovação) e entrada no ARCHITECTURE §7 para a página de status (achado 5).
- [ ] **P1**: escrever a seção "Checkpoint 6" do `TEST_REPORT.md` com cobertura, métodos a 0%, totais depois das correções (esperados 1.707 e 256, a confirmar), "passaram" separado de "puladas", mutações das correções e os números corrigidos (achado 6); depois marcar o Checkpoint 6 no `plans/plan.md:2036-2043` e no `plans/todo.md`.
- [ ] **P1**: remover os 12 `!` dos testes (achado 7).
- [ ] **P2**: itens 8 a 21: teste do token sem engolir erro; token e `Content-Encoding` na página de erro do painel; `/Home/Status` só por reexecução e mensagem por classe de código; arestas do predicado `UseWhen`; `HomeController` (variável, `sealed`, namespace); `!important`; varredura de `style=`; classificação do M3; testes inconclusivos e caixa do `plan.md:2021`; cache de `/lib` e fontes; textos "44 telas" e ADR de compressão e cache; itens aceitos da 6.1 para o `/scan`; compressão e rede no teste de volume; `<details>` ou teste de JavaScript bloqueado nos filtros.
- [ ] **P2**: registrar no `plans/BACKLOG.md` os 14 🟢 (e os 🟡 que o Product Owner decidir aceitar) com `found by /review Checkpoint 6, 2026-10-06`.

## 5. Test Coverage

**Gate 6** (de `reports/TEST_REPORT.md:1518-1522`, última rodada registrada, tarefa 6.3): 1.702 unitários do site, 43 da ferramenta de catálogo, 27 da de municípios, 162 de integração (SQL Server 2022 em contêiner) e 253 de navegador (**249 passaram e 4 foram puladas**: as de `VitalsTests`, que exigem `GAZETA_VITALS=1`; o relatório escreve "253 passaram", ver achado 6). Esses totais **não** incluem as duas correções de produto (`79798a0`, `c7c32b2`) nem a lista de 45 telas. Pela soma do disco, a próxima rodada completa deve dar 1.707 unitários (+5 de `StatusPagesTests`) e 256 de navegador (+1 `SearchLayoutShiftTests`, +2 da tela nova no axe e nas larguras): conta minha, a confirmar.

**Cobertura de linha e de ramo:** **ver TEST_REPORT Checkpoint 6** (medida por outro responsável; não citei número). Último valor medido, do Checkpoint 5: 97,9% de linhas e 91,5% de ramos (`TEST_REPORT.md:1372-1373`). O código de produto novo do escopo é pequeno e tem teste direto: `PerformanceExtensions` (`CompressionAndCacheTests`), `HomeController.Status` (`StatusPagesTests`), a troca de `Html.Raw` por `@:` (`RawOutputTests` e os 112 testes de categorias) e a view e o CSS da busca (`SearchTests`, `SearchLayoutShiftTests`). O gate pede também a lista de métodos a 0%, que esta seção deve herdar da medição.

**Mutação:** 6.1 com 8 mutações (8 mortas; a primeira forma de M4 era equivalente, defesa em duas camadas), 6.2 com 7 (7 mortas; a primeira forma de M3 sobreviveu por limite do axe, achado 15) e 6.3 com 7 (7 mortas; a primeira forma de M5, tirar as dimensões das capas, **sobreviveu e não é equivalente**, achado 2). As duas correções de produto não têm tabela de mutação no relatório; o corpo do commit `79798a0` diz que o teste novo falha contra o código antigo e passa com CLS 0,0043.

**Cenários e NFRs do escopo (caminho ligado e efeito observável):**

| Cenário ou NFR | Caminho ligado | Teste (efeito observável) |
|---|---|---|
| NFR-13 (acesso no servidor) | `[Authorize]` por política + `UseAuthentication`/`UseAuthorization` (`Program.cs:111`, `:114`) | `AccessMatrixTests.cs:190` (rota nova falha), `:218` (código × tabela), `:296`, `:343`, `:406`; `OwnershipMatrixTests.cs:90` (13 rotas, banco igual) |
| NFR-11 (antiforgery) | filtros globais (`Program.cs:46-51`) | `AccessMatrixTests.cs:440` (toda escrita sem token dá 400) |
| NFR-15 (texto como texto) | Razor codifica; sem `Html.Raw` | `XssInAllScreensTests.cs:143`, `:181`, `:202`, `:227`, `:245`, `:325`; `RawOutputTests.cs:45`, `:56`, `:67`; `XssE2ETests` (4) |
| NFR-16 (WCAG 2.1 A e AA) | lista de telas + axe | `Accessibility/AllScreensTests.cs:23` (45 casos); a parte manual de teclado e leitor de tela fica para o `/verify` (F6-17) |
| NFR-17 (larguras) | lista de telas + `WidthHelper` | `Responsiveness/AllScreensTests.cs:20` (45 casos em 320, 768, 1024 e 1280 px) |
| NFR-01 e NFR-05 (peso e compressão) | `Program.cs:66`, `:97`, `:108` | `CompressionAndCacheTests.cs:29-137`; `PageWeightTests.cs:42`, `:68`, `:116`; `PhotoWeightBudgetTests.cs:30` (vazio no peso da miniatura, achado 3) |
| NFR-04 (p95 do servidor) | busca e detalhe com 200 anúncios | `VolumeOfV1Tests.cs:105` (34,6 ms e 11,1 ms; limite 500 ms) |
| NFR-02 e NFR-03 (INP e CLS) | páginas públicas | `VitalsTests.cs:51` (só com `GAZETA_VITALS=1`); `SearchLayoutShiftTests.cs:27` (sempre; só a busca); capas sem prova (achado 2) |
| US-002-S12 (busca no celular, v1.6) | `Views/Search/Index.cshtml:78-84`, `sem-js.css`, `search.js:34-53` | `SearchTests.US002S12_Celular320px_OPainelDeFiltrosJaVemRecolhidoNoServidor_…`; `SearchLayoutShiftTests.cs:27`; E2E `SearchE2ETests` (não reexecutado no relatório depois da correção, achado 6) |
| Página de status | `Program.cs:92-94` → `HomeController.Status` | `StatusPagesTests.cs:20`, `:38`, `:50`, `:65`, `:80`; tela `not-found-page` no axe e nas larguras (sem critério na SPEC, achado 5) |

**Auditoria anti-vacuous:** abri os testes de cada linha da tabela e perguntei se passariam sem a funcionalidade. Os de matriz, posse, XSS, telas, compressão, cache imutável, volume e CLS da busca **não** passariam (as mutações confirmam). Passariam sem a funcionalidade: a miniatura de `PhotoWeightBudgetTests` (peso de 1 KB contra 74 KB; achado 3), a presença de `width`/`height` nas capas (nenhum teste; achado 2), a conferência de token nas páginas 404 de `CompressionAndCacheTests.cs:112` (achado 8) e a cláusula `/health` do predicado de status (achado 11). **Paridade de duas implementações:** o escopo não reimplementa regra nova; os pares que existem têm teste que os compara (lista de telas × rotas, matriz × atributos `[Authorize]`, `budgets.json` lido pelos três projetos, `aria-expanded` e rótulo do botão do painel desenhados no servidor e só sincronizados pelo `search.js`).

**Reconciliação de OPEN-NNN:** o `TEST_REPORT.md` **não possui** seção 12 nem nenhum conjunto `OPEN-NNN` (busca por `OPEN-`: 0 ocorrências); as pendências continuam no `plans/BACKLOG.md`, e a triagem do dono do projeto está em `plans/BACKLOG-TRIAGEM-FASE-6.md` (ids F6-xx). Esta é a disposição dos itens das tarefas 6.1 a 6.3 e do pós-Checkpoint 5 (BACKLOG, linhas 306 a 337) e das duas linhas de cobertura do Checkpoint 5 (302 e 303):

| Item do BACKLOG (linha) | Disposição |
|---|---|
| métodos a 0% e arquivos de menor cobertura (302, 303) | DEFERRED-to-/test (F6-26, F6-27; a medição nova está a cargo do Checkpoint 6) |
| roteiros de publicação do E2E copiados (306) | DEFERRED-to-P2 (F6-23) |
| conferência manual de celular, WhatsApp, `og:image` e NVDA (307) | DEFERRED-to-/verify (F6-15, F6-16, F6-17) |
| salto de 1 quadro ao fechar a galeria (311) | DEFERRED-to-/verify (F6-53) |
| `PublicRoutes.Ad` sem chamador (312) | DEFERRED-to-P2 (F6-39) |
| ids dos favoritos por desenho (313) | CLOSED (tabela de paridade) |
| `Html.Raw` do `Categories/Index.cshtml` (317) | CLOSED (`RawOutputTests.cs:18`, lista vazia) |
| site sem `FallbackPolicy` (318) | ESCALATED (Product Owner, F6-20; achado 19) |
| escape relaxado do JSON da API (319) | DEFERRED-to-/scan (F6-52; provado inofensivo por `nosniff`, `XssInAllScreensTests.cs:202`) |
| 403 por posse revela o id (320) e foto valida antes do autor (321) | DEFERRED-to-/scan (F6-52; achado 19) |
| 404 em branco (325) | CLOSED (`c7c32b2`), mas com o achado 5 aberto (critério na SPEC) |
| telas só como Administrador (326) | DEFERRED-to-P1 (F6-05, /test) |
| estados de falha provocada fora do axe (327) | DEFERRED-to-P2 (F6-29, /test) |
| Firefox, Safari, teclado e NVDA por tela (328) | DEFERRED-to-/verify (F6-17, F6-18) |
| axe sem violação em 44 telas (329) | CLOSED (45 telas após `c7c32b2`; texto do relatório a atualizar, achado 6) |
| LCP, INP e CLS reais, cache de um ano, IIS (333, 336) | DEFERRED-to-/verify (F6-13) |
| peso com fotos reais (334) | DEFERRED-to-/verify (F6-14; o achado 3 pede também corrigir o teste sintético) |
| fontes de 99 KB (335) | DEFERRED-to-/verify (F6-55; achado 17 acrescenta o cache) |
| CLS da busca em celular lento (337) | CLOSED (`79798a0`, `SearchLayoutShiftTests`), com o achado 4 aberto (aprovação na SPEC) |

## 6. Compliance Check

> Cada PASS cita arquivo e linha, o ponto em que o controle está ligado ao pipeline ou o nome do teste. Ordem real do pipeline em `Program.cs`: `:82` encaminhamento de cabeçalhos → `:83` correlation id → `:84` cabeçalhos de segurança e CSP → `:85-87` tratador de erros da API → `:88-90` tratador de erros das páginas → `:92-94` páginas de status → `:96` HTTPS → `:97` compressão (fora de `/painel`) → `:99-106` cultura → `:107` roteamento → `:108` cache de estáticos versionados → `:109` limite de corpo → `:111` autenticação → `:113` limitador → `:114` autorização → `:116` estáticos → `:119-125` saúde e controllers. O que foi **rodado** nesta revisão: `dotnet build -c Release` (0 avisos) e `dotnet format --verify-no-changes` (saída 0).

| Rule | Status | Evidence (file:line / test) | Notes |
|------|--------|-----------------------------|-------|
| api-conventions.md | PASS | Nenhum endpoint novo no escopo (`AccessMatrixTests.cs:38-127` lista 78 rotas); a API mantém o 404 simples (`StatusPagesTests.cs:65-78`) e sai comprimida (`CompressionAndCacheTests.cs:17`, `/api/v1/public/cities`) | Pendência do Checkpoint 5 (contrato `openapi.yaml`) segue no BACKLOG, fora deste escopo |
| brownfield.md | N/A | `.claude/PROJECT_PROFILE.md:3`: Mode greenfield | Não ativo |
| clean-code.md | WARNING | Bom: `PerformanceExtensions.cs:24-58` pequeno, constante nomeada (`:18`), `Budgets.cs` único. Falha: `HomeController.cs:52-63` repete a condição 4 vezes | 🟢 12 |
| code-style.md | WARNING | `dotnet format --verify-no-changes` com saída 0; arquivos novos com namespace de arquivo e `using` explícito (`PerformanceExtensions.cs:10`). Falha: 12 usos de `!` (`PageMeter.cs:73`, `ScreenData.cs:119`, `AccessMatrixTests.cs:178`…) e namespace em bloco em `HomeController.cs:16` | 🟡 7, 🟢 12 |
| error-handling.md | PASS | Resposta de erro em branco vira página amigável sem o motivo (`HomeController.cs:49-65`; `StatusPagesTests.cs:50-63`, "segredo-interno-123" nunca aparece); tratadores ligados em `Program.cs:85-94`; API mantém ProblemDetails (`StatusPagesTests.cs:65`) | 🟢 10 (código de referência sem registro nos 4xx) |
| database.md | PASS | Nenhuma consulta alterada; as existentes são medidas no SQL Server real (`VolumeOfV1Tests.cs:107-137`, p95 34,6 ms e 11,1 ms, `CreateMigratedDatabaseAsync` em `:110`) | — |
| frontend.md | WARNING | Bom: sem script nem manipulador inline (`RawOutputTests.cs:67`), sem `Html.Raw`/`innerHTML` (varredura `JsModulesTests.cs:19`), melhoria progressiva por `<noscript>` (`_Layout.cshtml:18`, `sem-js.css:15-24`), página de erro em português (`Views/Home/Status.cshtml`), `data-bs-*` no painel (`Search/Index.cshtml:79-84`). Falha: cache imutável de entradas com módulos importados sem versão; `!important` em `sem-js.css:23`; sem varredura de `style=` | 🟡 1, 🟢 13, 14, 21 |
| git-workflow.md | PASS | Commits convencionais com atribuição, um assunto por commit (`bbf64ea` só forma, `79798a0` só a correção do CLS com a SPEC, `c7c32b2` só a página de status); árvore limpa (`git status --short` vazio) | — |
| monitoring.md | PASS | Correlation id `Program.cs:83`; Serilog `Program.cs:37-43`; a página de erro não mostra pilha (`StatusPagesTests.cs:50`) | 🟢 10 (5xx sintético por `/Home/Status/{código}`) |
| naming-conventions.md | PASS | Testes `Metodo_Cenario_Resultado` (`AccessMatrixTests.cs:190`, `StatusPagesTests.cs:20`), ids de tela em kebab-case (`screens.json`), arquivos novos em inglês (`PerformanceExtensions.cs`, `Budgets.cs`, `PageMeter.cs`) | Nomes de CSS em português seguem no BACKLOG (F6-34) |
| output-style.md | PASS | As seções 6.1, 6.2 e 6.3 abrem com "Em resumo" (`TEST_REPORT.md:1428`, `:1469`, `:1514`), com tabela de limites e base de cada número (`:1528-1539`) | Números imprecisos: 🟡 6 |
| principles-and-practices.md | WARNING | §2.5 item 17 cumprido: achados fora de escopo registrados em `BACKLOG.md:315-337` com origem e arquivo, e o `dotnet format` isolado em `bbf64ea`. Falha: §2.5 item 15 (emenda v1.6 sem `Approved by`, `SPEC.md:31`) e §2.4 item 13 (decisões de compressão e cache sem ADR) | 🟡 4, 🟢 18 |
| project-structure.md | PASS | Middleware novo na camada Web (`Middleware/PerformanceExtensions.cs`), composição em `Program.cs:66`, `:97`, `:108`; Core intocado; `Budgets.cs` e os JSON compartilhados por ligação de arquivo nos `csproj` | — |
| security.md | PASS | Cabeçalhos e CSP em toda resposta: `Program.cs:84` + `SecurityHeadersMiddleware.cs:12-34` + `HeadersTests.TodaResposta_TemOsCabecalhosObrigatorios` (inclui `/nao-existe`); CSP sem `unsafe-inline`: `CspTests.cs`; CORS nenhuma: `CorsTests.Nenhuma_PoliticaCors_Registrada`; limitador: `Program.cs:65`, `:113` + `RateLimiterTests.cs:41`, `:66`, `:134`; antiforgery: `Program.cs:46-58` + `AccessMatrixTests.cs:440`; autenticação e papéis: `Program.cs:111`, `:114` + `AccessMatrixTests.cs:296`, `:343`, `:406`; HTTPS: `Program.cs:96` + `HeadersTests`; corpo: `Program.cs:109` + `BodyLimitTests.cs:19`; BREACH: `Program.cs:66`, `:97` + `CompressionAndCacheTests.cs:70-101`; texto como texto: `XssInAllScreensTests.cs` e `RawOutputTests.cs`; nenhum `style=` em `Views/**/*.cshtml` (busca) | Sem 🟡 de segurança; 🟢 9, 10, 14, 19 |
| system-design.md | WARNING | Bom: tempo limite e p95 (`VolumeOfV1Tests.cs:136-137`), limitador (`Program.cs:113`), cache de estáticos (`PerformanceExtensions.cs:40-58`). Falha: estratégia de cache sem invalidação do grafo de módulos | 🟡 1, 🟢 17 |
| tech-stack.md | PASS | Nenhum pacote novo: `git diff 17fb505..bbf64ea -- Directory.Packages.props` vazio e os três `csproj` só ganham ligação de arquivo; Deque.AxeCore.Playwright já aprovado (`Directory.Packages.props:32`); Bootstrap e JavaScript puro mantidos | — |
| testing.md | WARNING | Bom: pirâmide (HTTP, SQL Server real, navegador), mutações por tarefa, host isolado (`VolumeOfV1Tests.cs:110-114`, `IntegrationWebFactory`), nomes por cenário. Falha: peso da miniatura sem prova (`PhotoWeightBudgetTests.cs:18-49`), capas sem prova, `!` nos testes, testes que viram inconclusivos; a cobertura numérica nova ainda não está no relatório | 🟡 2, 3, 6, 7, 🟢 8, 15, 16, 20; cobertura: ver TEST_REPORT Checkpoint 6 |
| overrides/lang-dotnet.md | WARNING | Bom: identificadores em inglês, `ConfigureAwait(false)` e `CancellationToken` nos testes de navegador, `record` para `Screen` e `Transfer`, `sealed` nas classes novas, `Nullable` desativado sem `?`. Falha: `!` (proibido: "Do not add … `!`"), `HomeController` sem `sealed` | 🟡 7, 🟢 12 |
| overrides/database-sqlserver.md | PASS | Nenhuma migration nova; as BOMs removidas não mudam o conteúdo (`git show bbf64ea -w`); o teste de volume usa banco migrado de verdade (`VolumeOfV1Tests.cs:110`) | — |
| overrides/database-oracle.md | N/A | `.claude/PROJECT_PROFILE.md:6`: Database SQL Server | Fora do Profile |
| overrides/database-mysql.md | N/A | `.claude/PROJECT_PROFILE.md:6` | Fora do Profile |
| overrides/database-postgres.md | N/A | `.claude/PROJECT_PROFILE.md:6` | Fora do Profile |
| overrides/database-mongodb.md | N/A | `.claude/PROJECT_PROFILE.md:6` | Fora do Profile |
| overrides/framework-nodejs-web.md | N/A | `.claude/PROJECT_PROFILE.md:5`: Core C# | Fora do Profile |
| overrides/framework-php-laravel.md | N/A | `.claude/PROJECT_PROFILE.md:5` | Fora do Profile |
| overrides/lang-nodejs.md | N/A | `.claude/PROJECT_PROFILE.md:5` | Fora do Profile |
| overrides/lang-php.md | N/A | `.claude/PROJECT_PROFILE.md:5` | Fora do Profile |
| overrides/test-nodejs.md | N/A | `.claude/PROJECT_PROFILE.md:5` | Fora do Profile |
| overrides/test-php.md | N/A | `.claude/PROJECT_PROFILE.md:5` | Fora do Profile |
| overrides/monitoring-elk.md | N/A | `.claude/PROJECT_PROFILE.md:7`: Observability base | Fora do Profile |

## 7. Approval Status

| Decision | APPROVE |
|----------|---------|
| Conditions (if any) | 0 🔴. Antes do `/scan` o orquestrador corrige ou aceita explicitamente os 7 🟡 (Gate 7). Recomendo corrigir já: o **1** (cache imutável de `/js/`: uma linha de política e um teste; **precisa estar resolvido antes do `/deploy`**, quando passa a haver visitantes com cache), o **2** e o **3** (dois testes baratos, e o comentário errado), o **6** (a seção do Checkpoint 6, que já está a cargo de outro responsável) e o **7** (12 `!`, comportamento idêntico). O **4** e o **5** dependem do Product Owner: a aprovação da linha v1.6 da SPEC e o critério da página de status. Registrar no BACKLOG os 14 🟢 (P2). Nenhuma decisão de produto nova foi tomada pelo revisor, e nenhum arquivo de produto ou de teste foi alterado. |



---

# Code Review — Revisão formal do código completo (Fases 0 a 6)

**Date**: 2026-10-06
**Reviewer**: Code Reviewer agent (Five-Axis Framework), consolidando quatro revisores de leitura (A: Core, Infrastructure, banco e ferramentas · B1: Web em C# · B2: views, JavaScript e CSS · C: transversal, com regras, RC-N, ADRs, anti-vácuo, OPENs e plano)
**Inputs**: specs/SPEC.md · architecture/ARCHITECTURE.md e adr/* · plans/plan.md · plans/todo.md · plans/BACKLOG.md · plans/BACKLOG-TRIAGEM-FASE-6.md · security/PRE_DEV_REVIEW.md · security/SECURITY_REQUIREMENTS.md · security/THREAT_MODEL.md · reports/TEST_REPORT.md (seção "/test — Gate 6 da Fase 6") · `.claude/rules/**`
**Escopo**: todo o produto das Fases 0 a 6 (`src/`, `tests/`, `tools/`, `db/`), commit `f08e777`. Esta seção **não substitui** as seções anteriores deste arquivo (Fase 5 e Checkpoint 6); ela as consolida e as supera onde houver conflito.

> **Como foi feito.** Nenhum dos quatro revisores rodou `dotnet build`, `dotnet test` nem abriu navegador; todos leram o código. Os achados cuja conclusão depende de comportamento do framework, do navegador ou do provedor, e que **nenhum teste confirma**, levam a marca **(a confirmar)**. O consolidador conferiu no repositório os números que sustentam os achados mais pesados (listados na seção 1).

## 1. Executive Summary

> **Em resumo:** o produto está em bom estado: as regras de negócio, a autorização por papel e por autoria, o SQL parametrizado, o tratamento de fotos e a maior parte dos controles de segurança (19 dos 21 RC-N) estão implementados **e provados por teste**, com 98,0% de linhas e 91,8% de ramos cobertos. Há, porém, **um defeito crítico de segurança (R-01)**: as chaves do Data Protection (o mecanismo que assina o cookie de login, o token de antiforgery e o link de redefinição de senha) **nunca são gravadas na pasta persistente** que a arquitetura, o ADR-011 e os requisitos de segurança descrevem. A pasta é exigida na partida e depois ignorada. O problema **não é explorável por um atacante**; o efeito é de disponibilidade e de falsa garantia: dependendo do que a hospedagem faz por padrão, cada reciclagem do IIS pode deslogar a equipe e invalidar links de redefinição já enviados. A correção é pequena (poucas linhas e dois testes). Além do crítico, há **17 avisos** e **51 sugestões**.

**Verdict: APPROVE com condições** (no vocabulário do template, **equivale a REQUEST CHANGES até a correção do R-01**; o Gate 7 **não passa** enquanto o R-01 estiver aberto).

| Severidade | Qtde (consolidada, após deduplicação) | Bruto dos revisores |
|---|---|---|
| 🔴 Crítico | **1** (R-01) | 2 (B1-01 e C-01 são o mesmo problema) |
| 🟡 Aviso | **17** (R-02 a R-18) | 18 (B1-02 e C-02 são o mesmo) |
| 🟢 Sugestão | **51** (R-19 a R-69) | 53 (C-10 = B1-06; C-11 absorvida por B1-14) |
| ✅ Positivos | **18** (seção 3) | 49 |
| **Total de achados 🔴+🟡+🟢** | **69** | 73 |

Por revisor (bruto, antes de deduplicar): A 0 🔴 · 5 🟡 · 12 🟢; B1 1 🔴 · 4 🟡 · 17 🟢; B2 0 🔴 · 5 🟡 · 14 🟢; C 1 🔴 · 4 🟡 · 10 🟢. Dos 69 consolidados, **9 já estavam no `plans/BACKLOG.md`** (R-42 a R-48, R-61 e R-63) e **60 são novos**.

**Decisões de severidade.** Mantive a severidade dada por cada revisor. Nenhuma foi alterada. Sobre o 🔴: é uma **lacuna de implementação** contra o que ARCHITECTURE §7, ADR-011 (linha 84) e SECURITY_REQUIREMENTS §4 afirmam (o item está marcado `[x]` lá). Mantive 🔴 por três razões somadas: (1) o código valida a configuração e a descarta, o que dá **garantia falsa** a quem opera; (2) a ameaça S3 do modelo de ameaças não tem nenhuma implementação; (3) o efeito possível atinge os dois fluxos principais da equipe (sessão e redefinição de senha) justamente na hospedagem alvo. **Não** mantive 🔴 por exploração: não há caminho de ataque. O efeito real depende do provedor: no IIS o ASP.NET Core só grava as chaves no registro se o pool foi provisionado com o script próprio, e só no perfil do usuário se o perfil estiver carregado; sem nenhum dos dois, o anel fica em memória. Qual desses casos vale no SmarterASP compartilhado **é incerto e não foi verificado por ninguém**.

**Números conferidos pelo consolidador no repositório** (não repetidos de relatório):

| Verificação | Resultado |
|---|---|
| `AddDataProtection` / `PersistKeysToFileSystem` em `src/` (só `.cs`) | 0 ocorrências; só há `KeyStorageOptions` validada em `OptionsExtensions.cs:25` |
| `[EnableRateLimiting("auth")]` / `AuthPolicy` | `AuthPolicy` é definida em `RateLimitingExtensions.cs:21` e `:66`; **nenhuma ação a usa** (as políticas aplicadas por atributo são só `cep`, `fotos` e `fotos-envio`; o limite global vale para todo pedido) |
| Operador `!` (null-forgiving) | 5 linhas em `src/` e 184 em `tests/` (o `TEST_REPORT` e o OPEN-005 diziam "cerca de 60") |
| Arquivos `.claude.backup-*` versionados | 177 (`git ls-files`) |
| Arquivo solto `web` na raiz citado pelo revisor C | **já não existe**; `git status --short` está vazio |

## 2. Five-Axis Scores

Regra de honestidade aplicada: eixo com 🔴 aberto fica em no máximo 2; eixo com 🟡 aberto, em no máximo 4; 5 só sem nenhum achado pendente. O 🔴 deste ciclo é de **Segurança**.

| # | Axis | Score (1–5) | Notas por escopo (A · B1 · B2 · C) | One-line justification |
|---|------|-------------|---|---|
| 1 | Correctness | **3** | 3 · 2 · 3 · 4 | Sem 🔴 próprio do eixo, mas **12 🟡 abertos**: R-02 e R-03 podem gravar ou perder dado em silêncio, R-04 devolve 500 com entrada de forma errada, R-05 pode derrubar páginas no Windows, R-06 trava o envio de categorias novas, R-09 e R-10 estragam telas na sessão vencida e no token inválido, R-14 a R-18 são defeitos de acessibilidade e de tela medidos só por leitura. Os fluxos principais estão provados (35 de 36 cenários amostrados afirmam o efeito observável). O R-01 também pesa aqui (sessão e redefinição), mas o classifiquei em Segurança |
| 2 | Readability | **4** | 4 · 4 · 4 · 4 | Código claro, com comentários do porquê. O único 🟡 do eixo é R-13 (operador `!` em 184 linhas de teste e 5 de `src/`, proibido pelas regras). Os 🟢 (R-22, R-28, R-40, R-41, R-52, R-54, R-63) são pequenos |
| 3 | Architecture | **3** | 4 · 3 · 4 · 4 | Camadas e referências entre projetos respeitadas; 11 dos 12 ADRs cumpridos. O padrão que pesa: **documento afirma um mecanismo que o código não tem** (R-01 chaves, R-07 política `auth`, R-12 FluentValidation), mais R-65 (contrato e dados pessoais desatualizados). Dois 🟡 do eixo (R-07, R-12) e ADR-011 só parcial |
| 4 | Security | **2** | 4 · 2 · 5 · 2 | **🔴 R-01** (anel de chaves sem persistência) limita a nota a 2. Somam-se 🟡 R-07 (limite documentado e nunca aplicado, com teste que prova um controlador de teste), R-08 (permissão negada sem registro no log) e R-11 (SEC-01 pendente: sem o proxy configurado, as proteções por IP valem para todos juntos). Pontos fortes reais: matriz de acesso por descoberta de rotas, SQL parametrizado, fotos confinadas e decodificadas com política, zero `innerHTML`/`Html.Raw`, CSP sem exceção |
| 5 | Performance | **4** | 4 · 4 · 4 · 4 | Nenhum 🟡. Orçamentos medidos (p95 de 34,6 ms no volume da v1; tempo limite de consulta provado no SQL Server real). Pendências 🟢: R-32 (leitura de corpo antes do limite de 11 MB), R-56 (vendors sem versão e fonte 700 sem `preload`) e os itens já no BACKLOG. Não dou 5 porque há achados pendentes e números em rede real ainda por medir (`/verify`) |

**Re-pontuação:** se o R-01 for corrigido e provado, Segurança sobe para no máximo 4 (restam os 🟡 R-07, R-08 e R-11); os demais eixos só sobem com o fechamento dos 🟡 de cada um.

## 3. Findings (by severity: 🔴 → 🟡 → 🟢 → ✅)

Cada achado traz o id consolidado `R-NN` e, entre parênteses, a origem (`A-`, `B1-`, `B2-` e `C-` são os revisores; "=" indica o mesmo problema achado por mais de um). **(a confirmar)** marca a conclusão só de leitura, sem teste que a confirme. Tabela de rastreio de ids: ver a seção 4 (Action Items) e `plans/BACKLOG-TRIAGEM-FASE-6.md` §4.

### 🔴 Critical

#### R-01 (B1-01 = C-01) — Data Protection não persiste as chaves; a pasta `DataProtection__KeysDirectory` é exigida em produção e nunca é usada
- **Eixo:** Segurança (efeito também em Correção). **Base:** lido no código e confirmado por busca no repositório; o efeito em produção depende do provedor **(a confirmar no `/verify`)**.
- **Where:** `src/GazetaMarketplace.Web/Program.cs:47-85` (nenhuma chamada a `AddDataProtection`); `src/GazetaMarketplace.Infrastructure/Configuration/OptionsExtensions.cs:25` (valida e liga `KeyStorageOptions`); `src/GazetaMarketplace.Core/Configuration/KeyStorageOptions.cs:6-11` (`KeysDirectory` com `[Required]`); `web.Production.config.example:17`; `tests/GazetaMarketplace.Web.Tests/Configuration/OptionsTests.cs:139-150` (só prova que a variável é exigida).
- **Description:** ARCHITECTURE §7, ADR-011 (linha 84: "As chaves do Data Protection usam `PersistKeysToFileSystem`"), THREAT_MODEL S3 e SECURITY_REQUIREMENTS §4 (item `[x]`, RR-2 e RR-8) dizem que as chaves ficam numa pasta fora da raiz do site. A busca por `AddDataProtection`, `PersistKeysToFileSystem`, `SetApplicationName` e por qualquer leitor de `KeyStorageOptions` não acha nada. O site se recusa a subir sem a pasta e depois a ignora. **Não é explorável**; é lacuna de implementação com garantia falsa. O que acontece em produção é o padrão do ASP.NET Core para o host: no IIS, grava no registro se o pool foi provisionado com o script do IIS, grava no perfil do usuário se houver perfil carregado e, sem nenhum dos dois, mantém o anel só em memória. Na hospedagem compartilhada **não se sabe** qual desses casos vale. O token de redefinição (`RecoveryTokenProvider.cs:24`), o cookie `Gazeta.Team`, o antiforgery e o `TempData` dependem do anel.
- **Cenário:** produção no SmarterASP; o IIS recicla o pool por inatividade (o ADR-010 conta com isso). Se o anel estiver em memória: (1) a equipe perde a sessão; (2) uma redatora que pediu a redefinição de senha 10 minutos antes abre o link e recebe "O link não é mais válido", e o fluxo US-007 passa a falhar de forma intermitente; (3) um formulário aberto antes da reciclagem volta com 400 (ver R-10). Nenhum teste enxerga isso: a suíte roda num só processo, com o anel padrão do host de teste.
- **Recommendation:** em `Program.cs`, depois de `AddAppOptions`, `builder.Services.AddDataProtection().SetApplicationName("GazetaMarketplace").PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))` lendo `KeyStorageOptions` (em Development e nos testes, uma pasta temporária). Prova: um teste que sobe dois hosts com a mesma pasta e mostra que o cookie, o token de redefinição e o token de antiforgery do primeiro valem no segundo, e outro que confere que a pasta recebe `key-*.xml`. Se a hospedagem não oferecer criptografia das chaves em repouso (DPAPI), registrar a decisão na AR-01, como o ADR-011 já prevê. Acrescentar o critério à Task 0.2.
- Relates-to: ADR-003, ADR-011, AR-01, RR-2, RR-8, THREAT_MODEL S3, NFR-08, NFR-09, US-007, Task 0.2

### 🟡 Warning

#### R-02 (A-01) — O Administrador consegue deixar um anúncio **Publicado** incompleto (sem categoria, preço, descrição ou foto)
- **Eixo:** Correção. **Base:** lido no código; o teste `DraftTests.cs:318` já posta só `Title`, `CategoryId` e `Price` e o servidor aceita.
- **Where:** `src/GazetaMarketplace.Infrastructure/Ads/AdDraftService.cs:85-123` (`UpdateAsync`) e `:184-240` (`PrepareAsync`); `Photos/AdPhotoService.cs:89-105` (`DeleteAsync` aceita apagar a última foto); `AdSubmissionRules.Pending` só é chamada em `Ads/AdSubmission.cs:90` e `Ads/AdReview.cs:106`.
- **Description:** a SPEC diz que editar um Publicado altera o site na hora (US-008, regra 2) e que só o envio exige os campos obrigatórios (US-009). O serviço usa a validação frouxa de rascunho em qualquer situação; quem edita um anúncio no ar não passa por conferência de completude.
- **Cenário:** o Administrador abre um Publicado de Carros e salva com a categoria vazia, apaga a única foto ou esvazia a descrição. O anúncio continua `Published`: sem categoria some da categoria e da busca, mas segue na home e no mapa do site; sem foto aparece "Foto indisponível". `Attributes` é substituído inteiro em `Apply`, então os campos do grupo se perdem.
- **Recommendation:** com `ad.Status == Published` (ou `InReview`), `UpdateAsync` recusa o que `AdSubmissionRules.Pending` recusaria (como `AdReview.PublishChecksAsync` já faz) e `AdPhotoService.DeleteAsync` recusa apagar abaixo de `MinPhotosToSubmit` nesses estados. Teste: salvar um Publicado sem categoria e sem descrição devolve 400/409 e não grava. **Decisão de produto** envolvida (ver triagem §4, decisão 2).
- Relates-to: US-008 (regra 2, S13), US-009, US-010, US-003

#### R-03 (A-02) — `CepService` chama `ChangeTracker.Clear()` no contexto compartilhado e solta o anúncio que `AdDraftService.UpdateAsync` está editando
- **Eixo:** Correção (concorrência). **Base:** lido no código; o revisor não conseguiu executar para ver se o resultado é perda silenciosa ou erro 500 **(a confirmar com teste)**.
- **Where:** `src/GazetaMarketplace.Infrastructure/Location/CepService.cs:72-76` (`catch (DbUpdateException) { context.ChangeTracker.Clear(); }`); `Ads/AdDraftService.cs:88` (`GetForEditAsync` rastreia o `Ad`), `:90` e `:388` (`PrepareAsync` chama `cep.GetAsync` no mesmo `AppDbContext`), `:100` e `:107` (usam `context.Entry(ad)` depois).
- **Description:** se a gravação do cache do CEP falha (o caso previsto é duas pessoas gravando o mesmo CEP ao mesmo tempo), o `Clear()` desanexa tudo o que o contexto rastreava, inclusive o `Ad` carregado antes. `UpdateAsync` segue com um `ad` desanexado; `Apply(ad, …)` altera um objeto que o EF não vê e a auditoria `ad.update` é gravada sozinha.
- **Cenário:** dois membros da equipe salvam anúncios com o mesmo CEP novo no mesmo instante. Um recebe "Rascunho salvo", mas o anúncio não mudou, e a auditoria descreve uma mudança que não aconteceu (ou, se o EF recusar a entrada desanexada, a pessoa recebe 500). O teste `CepAndCitiesTests.cs:97` prova só o `CepService` isolado, com contextos separados.
- **Recommendation:** o `CepService` não deve limpar o rastreador do chamador: gravar o cache num contexto próprio (`IServiceScopeFactory`/`IDbContextFactory`, como `CategoryTree` faz) ou desanexar só a entrada que falhou. Teste: `UpdateAsync` com `cep.GetAsync` forçado a falhar a gravação do cache, conferindo que a edição foi gravada.
- Relates-to: US-008 (CEP, S23 a S25), NFR-24, ADR-007, RC-16

#### R-04 (A-03) — `PriceText` aceita dígitos de outros alfabetos no regex e depois lança `FormatException` (erro 500)
- **Eixo:** Correção / entrada de borda. **Base:** lido no código.
- **Where:** `src/GazetaMarketplace.Core/Formatting/PriceText.cs:28-29` (`\d` no regex `Shape`) e `:55` (`long.Parse(fraction, NumberStyles.None, InvariantCulture)`); chamadores `Ads/AdFormRules.cs:46-66` (`PriceError`) e `Fields/FieldValueParser.cs:132` (`ApplyMoney`, usado em Condomínio e IPTU).
- **Description:** em .NET `\d` casa qualquer dígito Unicode (categoria Nd); `long.Parse` com `NumberStyles.None` só aceita 0-9. `DecimalInput`, `FavoriteIds` e `CepRules` usam 0-9 de propósito; `PriceText` é a exceção. A regra "wrong-type input" de `testing.md` pede 4xx documentado, não falha.
- **Cenário:** o campo Preço (ou Condomínio) recebe `1,٥` por POST direto ou colado: `Shape()` casa, `PadRight` gera `"٥0"` e o `Parse` lança dentro de `PrepareAsync`: 500 em vez de "Informe o preço em reais". Com dígitos arábicos na parte inteira (`٦٢٠٠٠`) o código cai no ramo de estouro e devolve a mensagem errada ("entre R$ 0,01 e R$ 99.999.999,99").
- **Recommendation:** trocar `\d` por `[0-9]` (ou `RegexOptions.ECMAScript`) e usar `TryParse` nas casas decimais; acrescentar à tabela de testes de `PriceText` um dígito arábico-índico e um de largura total (`１２３`).
- Relates-to: US-008-S08, US-008 (preço, S28), testing.md (wrong-type input)

#### R-05 (A-04) — O fuso de exibição usa um id IANA (`America/Sao_Paulo`) num inicializador estático; a hospedagem é Windows/IIS
- **Eixo:** Correção / Arquitetura (risco de ambiente). **Base:** lido no código; o comportamento no servidor do provedor **(a confirmar no `/verify`)**; todos os testes rodaram em Linux.
- **Where:** `src/GazetaMarketplace.Core/Formatting/CurrencyAndDateFormatter.cs:15` (`TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")` em propriedade estática); usado também em `Fields/ModelYearRules.cs:17` (`CurrentYear`), chamado por `AdDraftService.cs:223` e `SearchService.cs:258`.
- **Description:** a ARCHITECTURE (§Hospedagem) fixa o IIS compartilhado do SmarterASP (Windows). No Windows o id IANA só resolve em .NET 6+ com o ICU disponível; sem ele, `FindSystemTimeZoneById` lança `TimeZoneNotFoundException` e, por ser inicializador estático, o tipo fica quebrado (`TypeInitializationException` em toda chamada seguinte).
- **Cenário:** o site sobe num Windows Server sem ICU; a primeira data exibida ou o primeiro salvamento com campo de ano lança e a página responde 500 até o processo reciclar.
- **Recommendation:** tentar o id IANA e, se falhar, o id do Windows (`"E. South America Standard Time"`) num método com `try/catch`; pôr no checklist do `/deploy` uma chamada de teste (`/` com datas e o formulário de anúncio) na hospedagem real. Se o `/verify` confirmar a falha, vira bloqueio de lançamento.
- Relates-to: NFR-20, ADR-004, AR-05

#### R-06 (A-05) — Categoria criada pelo Administrador sob Imóveis, Roupas, Eletro ou Telefonia herda o grupo, mas não a lista do campo obrigatório: o anúncio nunca é enviado
- **Eixo:** Correção. **Base:** lido no código.
- **Where:** `src/GazetaMarketplace.Core/Fields/FieldDefinition.cs:156-157` (`OptionsFor` só procura o id exato da categoria); `Fields/FieldValueParser.cs:146-152` (`ApplySelect` devolve `InvalidOption` com lista nula); `Ads/AdSubmissionRules.cs:76-78` (obrigatório vazio vira pendência); campos afetados: `Groups/RealEstateGroup.cs:26-38` (`propertyTypeId`), `ClothingAndShoesGroup.cs:20-35` (`sizeId`), `AppliancesGroup.cs:22-36` e `TelephonyProductsGroup.cs:19-30` (`typeId`). O texto de `FieldGroupRegistry.cs:11` ("Categoria nova herda do pai sem nenhum trabalho") promete o contrário.
- **Description:** a categoria nova usa o grupo do ancestral (A7(a)), mas a lista de opções desses quatro campos obrigatórios é indexada pelo **id** das categorias da carga inicial.
- **Cenário:** o Administrador cria "Kitnets" dentro de "Apartamentos" (26). `propertyTypeId` é obrigatório; `OptionsFor(novoId)` devolve `null`; a tela mostra uma caixa sem opções, o servidor recusa qualquer valor e o envio fica sempre com a pendência "Informe o tipo do imóvel". Em Peças o campo não é obrigatório: só degrada.
- **Recommendation:** `OptionsFor` cai para a lista da categoria ancestral mais próxima que tenha entrada (passando o caminho da árvore, como `ResolveFieldGroup`), ou para uma lista padrão; teste de paridade que crie uma filha de cada categoria com `OptionsByCategory` e confira que todo campo obrigatório de lista tem opções.
- Relates-to: US-013, A7(a), ADR-002, Task 2.6

#### R-07 (B1-02 = C-02) — A política de limite `auth` nunca é aplicada em produção, e o teste que a "prova" usa uma rota de mentira
- **Eixo:** Arquitetura e Segurança. **Base:** lido no código; `AuthPolicy` só aparece nas linhas 21 e 66 de `RateLimitingExtensions.cs` (conferido).
- **Where:** `src/GazetaMarketplace.Web/Security/RateLimitingExtensions.cs:21` e `:66-67`; `Areas/Panel/Controllers/AccountController.cs:59`, `:115`, `:137` (sem `[EnableRateLimiting]`); `tests/GazetaMarketplace.Web.Tests/Security/RateLimiterTests.cs:41-64` e `:119-131` com `Support/TestControllers.cs:38-39` (o atributo só existe na rota `/api/v1/teste/auth`).
- **Description:** ARCHITECTURE §7 ("Login e 'Esqueci minha senha': 5 por 15 min por IP"), SECURITY_REQUIREMENTS §5 (`[x]`), o THREAT_MODEL (S1/D4) e `plan.md:269` descrevem um controle que nenhuma rota usa. O que existe é outro, e funciona: o login conta **falhas** em `LoginFailureCounter` (decisão registrada em `plan.md:485`) e a recuperação tem 3 pedidos por hora por e-mail e 10 por IP dentro de `PasswordRecoveryService.RequestAsync`. A política virou código morto e `SextaTentativaDeLogin_Devolve429` continuaria verde se o login ficasse sem limite (só o `US006S06` falharia).
- **Cenário:** um cliente anônimo envia 100 POSTs por minuto para `/painel/esqueci-minha-senha` com e-mails aleatórios. Cada pedido grava uma linha em `PasswordRecoveryAttempts` e faz três `COUNT` (`PasswordRecoveryService.cs:46-52`) antes de o limite de 10 por hora calar o envio; só o limite global de 100 por minuto segura: cerca de 144 mil linhas por dia por IP e 400 consultas por minuto no banco do provedor. Também não há limite próprio em `POST /painel/redefinir-senha`.
- **Recommendation:** decidir e alinhar: (a) aplicar `[EnableRateLimiting(AuthPolicy)]` só nos POSTs de `Forgot` e `Reset` (não no login, para não contar acertos), com números condizentes (por exemplo 10 por hora); (b) corrigir ARCHITECTURE §7, SECURITY_REQUIREMENTS §5, THREAT_MODEL S1/D4 e o critério da Task 0.4; (c) reescrever o teste para chamar a rota real e provar o 429; ou (d) apagar a política e o controlador de teste.
- Relates-to: NFR-06, RC-11, RC-13, THREAT_MODEL S1/D4, Task 0.4, Task 1.4

#### R-08 (B1-03) — "Permissão negada" nas páginas do painel não deixa nenhum registro no log
- **Eixo:** Segurança. **Base:** lido no código.
- **Where:** `Areas/Panel/Controllers/AdsController.cs:438-442` (`NoPermission`) e os `catch (ForbiddenException)` em `:123`, `:167`, `:197`; `AdPhotoPagesController.cs:69`, `:102`, `:114-118`; `AccountController.cs:187-195` (`AccessDenied`); `Security/IdentityExtensions.cs:111-120`.
- **Description:** SECURITY_REQUIREMENTS §6 pede Serilog `Warning` para falha, bloqueio e recusa de login, **permissão negada** e limite excedido. Nas páginas, a recusa por papel e a recusa por autoria devolvem a tela 403 e nada mais: o `AdService` lança sem logar e o "Authorization failed" do framework é `Information`, abaixo do nível `Warning` configurado. Só a `/api` registra (`ExceptionHandlingMiddleware.cs:50`).
- **Cenário:** o Redator B abre `/painel/anuncios/17/editar`, da Redatora A, 200 vezes variando o id. Cada resposta é um 403 correto e invisível; a investigação de um IDOR sondado não tem por onde começar. A mesma sondagem pela `/api` ficaria registrada.
- **Recommendation:** `LogWarning("Acesso negado em {Method} {Path} ao usuário {UserId}", …)` no helper `NoPermission` dos dois controllers (ou num filtro de exceção para `ForbiddenException`) e na ação `AccessDenied`; teste de log nos moldes de `ErrorsTests`.
- Relates-to: RC-16, NFR-13, US-006-S10, US-008-S10

#### R-09 (B1-04) — Com a sessão vencida, o `fetch` de "trocar categoria" recebe a tela de login com status 200 e a injeta no formulário do anúncio
- **Eixo:** Correção. **Base:** lido no código.
- **Where:** `Security/IdentityExtensions.cs:96-109` (só `/api` ganha 401; o resto é 302); `Areas/Panel/Controllers/AdsController.cs:342-343` (`GET painel/anuncios/campos`); `wwwroot/js/pages/ad-edit.js:90-92` (`if (!resposta.ok)` e `DOMParser`, sem olhar `resposta.redirected`).
- **Description:** a rota `campos` fica sob `/painel`; cookie vencido gera 302 para `/painel/entrar?ReturnUrl=…`; o `fetch` segue o redirecionamento, a tela de login volta com 200 e `resposta.ok` é verdadeiro.
- **Cenário:** a pessoa abre "Novo anúncio", escreve a descrição por mais de 30 minutos sem nenhum pedido ao servidor e troca a categoria. A região `[data-group-region]` é substituída pelo corpo da página de login (campos de e-mail e senha e o token antiforgery da entrada) dentro do formulário do anúncio, e os campos do grupo somem. Salvar nessa tela cai de novo no login e o texto digitado se perde. A mesma mecânica faz um POST em sessão vencida voltar com `ReturnUrl` para uma rota só de POST, e depois do login a pessoa vê "Página não encontrada".
- **Recommendation:** no servidor, 401 para pedido que não é navegação (`Sec-Fetch-Mode` diferente de `navigate`) em `OnRedirectToLogin` e `OnRedirectToAccessDenied`; no cliente, `if (resposta.redirected || !resposta.ok)` em `ad-edit.js` com o aviso "sua sessão expirou". Teste: pedido a `/painel/anuncios/campos` sem cookie e com `Sec-Fetch-Mode: cors` deve dar 401, não 302.
- Relates-to: US-006-S08, US-008-S09, NFR-08

#### R-10 (B1-05) — Em POST com token antiforgery inválido, a página amigável de 400 sai em branco
- **Eixo:** Correção. **Base:** **(a confirmar)**: leitura do filtro do MVC (`AutoValidate` só dispensa GET, HEAD, OPTIONS e TRACE) e do fato de a reexecução manter o método; nenhum teste confirma.
- **Where:** `Program.cs:48-53` (`AutoValidateAntiforgeryToken` global) e `:100-102` (`UseStatusCodePagesWithReExecute`); `Controllers/HomeController.cs:50` (`Status`, sem `[IgnoreAntiforgeryToken]`); `tests/.../Security/AntiforgeryTests.cs:24-33` (só confere o status 400, não o corpo) e `Middleware/StatusPagesTests.cs` (todos os pedidos são GET).
- **Description:** a reexecução do pipeline mantém o método original; um POST cujo token falhou cai em `/Home/Status/400` também como POST, o filtro global valida o token de novo, falha de novo e devolve 400 vazio.
- **Cenário:** a equipe abre `/painel/entrar` de manhã, o pool recicla (ver R-01) e a pessoa envia o formulário: o token da página aberta já não vale e o navegador mostra uma página totalmente em branco, não a tela "Algo deu errado" que o NFR-25 promete. O mesmo vale para duas abas: entra numa e envia o formulário anônimo da outra.
- **Recommendation:** `[IgnoreAntiforgeryToken]` em `HomeController.Status` e `Error` (só leem estado); teste "POST sem token em página do painel mostra a página de status com 400".
- Relates-to: NFR-25, NFR-11, Task 6.2

#### R-11 (C-03) — SEC-01/RC-10 segue pendente e, sem o valor do provedor, o bloqueio de login e os limites viram um só para todos
- **Eixo:** Segurança (disponibilidade). **Base:** lido no código; o desenho fail-closed está provado em teste, o que falta é o valor do provedor.
- **Where:** `Web/Security/ForwardingExtensions.cs:40-43` (`UseSecureForwarding` só liga com `ForwardedHeaders:KnownProxies`); `web.Production.config.example:23-25` (a variável só aparece em comentário); `LoginFailureCounter.cs`; `PasswordRecoveryService.cs:56`; `RateLimitingExtensions.cs:133`.
- **Description:** o desenho é correto (não confia em cabeçalho forjado). Mas, **se** o provedor encaminha por um proxy e o valor não for configurado, o IP visto é o do proxy para todos: cinco falhas de qualquer pessoa bloqueiam o login de **toda a equipe por 15 minutos**, o limite global de 100 por minuto é do site inteiro e o de 10 pedidos de redefinição por hora vale para todos juntos. O efeito de negação de serviço do contador de login não está descrito no PRE_DEV_REVIEW; o item não está no BACKLOG (só F6-03 cita o checklist, sem SEC-01) e o critério da 0.4 segue `[ ]`.
- **Cenário:** no go-live a variável é esquecida; um bot erra a senha 5 vezes; ninguém da redação entra até as 15 minutos passarem.
- **Recommendation:** pôr SEC-01 no checklist de implantação (F6-03) com teste de aceite (depois de configurar, duas origens distintas não compartilham o contador); `Warning` na partida quando `ForwardedHeaders:KnownProxies` está vazio em Production (hoje é silencioso); registrar no PRE_DEV que o efeito é de disponibilidade.
- Relates-to: SEC-01, RC-10, RR-4, THREAT_MODEL S1/D1/D3, Task 0.4

#### R-12 (C-04) — FluentValidation exigido pelas regras, aprovado na AR-06, marcado como feito e nunca adotado
- **Eixo:** Arquitetura. **Base:** lido no código e confirmado por busca (sem `Core/Validators`, sem o pacote em `Directory.Packages.props`).
- **Where:** `security/SECURITY_REQUIREMENTS.md` §3 ("FluentValidation em `GazetaMarketplace.Core.Validators`"); `rules/security.md` e `rules/api-conventions.md`; `Directory.Packages.props`; a validação real está em `FieldValueParser`, `AdFormRules`, `SearchService`, `UserManagement.CreateAsync` e anotações nos ViewModels.
- **Description:** a validação existe e é bem testada, mas **não é a pilha aprovada**, não há ADR nem nota de desvio, e o documento de requisitos diz que foi feito de outro jeito. Mesmo padrão do R-07: o documento afirma um mecanismo que o código não tem.
- **Cenário:** um auditor procura os validadores citados e não os encontra.
- **Recommendation:** registrar a decisão ("validação nas regras de domínio e na entrada do serviço; FluentValidation não adotado, razões") numa linha de `ARCHITECTURE.md` §7 ou num ADR curto e corrigir SECURITY_REQUIREMENTS §3 e a AR-06; ou adotar o pacote pelo Technology Decision Process (custo alto, benefício baixo hoje). **Decisão do Product Owner** (triagem §4, decisão 3).
- Relates-to: AR-06, NFR-22, Task 1.3, Task 3.3, Task 5.4

#### R-13 (C-05 + OPEN-005) — O operador `!` está em 184 linhas de teste e 5 de `src/`, não "cerca de 60"
- **Eixo:** Legibilidade e testes. **Base:** contagem conferida pelo consolidador (`[\w)\]]!` seguido de `. ; , ) [` ou fim de linha, sem `!=`): 5 em `src/`, 184 em `tests/`.
- **Where:** `src/GazetaMarketplace.Infrastructure/Photos/FileSystemPhotoStorage.cs:53,75,104`; `src/.../Data/Configurations/AdConfiguration.cs:87,100`; 50 arquivos em `tests/` (por exemplo `Fields/VehicleGroupsTests.cs`, `Playwright/Showcase/ContactE2ETests.cs:135,146,160,161,216`).
- **Description:** `lang-dotnet.md`, `code-style.md` e `testing.md` proíbem `?` e `!` porque `Nullable` está desligado. O `TEST_REPORT` (correção D1.3), o OPEN-005 e o BACKLOG (F6-32, que cita só dois) subestimam: são 184 linhas de teste mais 5 de produto.
- **Cenário:** o item é dado como "pequeno, fora do escopo" três vezes seguidas e cresce a cada tarefa porque o hábito não é barrado.
- **Recommendation:** uma passada `/simplify` que remova todos (substituição mecânica, sem mudar comportamento) e uma trava que falhe o build se voltar (regra de análise no `.editorconfig` ou teste de varredura como o `RawOutputTests`); corrigir o número no BACKLOG.
- Relates-to: lang-dotnet.md, code-style.md, testing.md, OPEN-005, F6-32

#### R-14 (B2-01) — O contorno de foco do projeto não vale para botões, paginação, abas, campos e caixas de seleção
- **Eixo:** Correção (acessibilidade: WCAG 2.4.7 e 1.4.11). **Base:** **(a confirmar no navegador)**: leitura de `base.css` e do CSS do Bootstrap (verificado por `grep`).
- **Where:** `src/GazetaMarketplace.Web/wwwroot/css/base.css:60-64` (regra `:focus-visible`, especificidade 0,1,0) contra `wwwroot/lib/bootstrap/dist/css/bootstrap.min.css`: `.btn:focus-visible`, `.page-link:focus`, `.nav-link:focus-visible`, `.form-select:focus`, `.form-check-input:focus` e `.navbar-toggler:focus` (todos com `outline:0; box-shadow:…`, especificidade 0,2,0).
- **Description:** os seletores do Bootstrap vencem a regra global do projeto, que o `design-system.md:20` e `:191` descrevem como o contorno sólido `#0a58ca` (6,44:1). Na prática ele só chega a links comuns e ao "Ir para o conteúdo". No cabeçalho e no rodapé `.cabecalho :focus-visible` restaura o contorno, por isso a falha só aparece no corpo. O único teste de foco (`BaseCssTests.cs:38-50`) mede o link "Ir para o conteúdo"; o axe não avalia indicador de foco.
- **Cenário:** na fila de revisão, `Tab` até "Publicar" (`btn btn-primary`): o foco vira anel `rgba(215,34,19,.5)` (cerca de 2,3:1 sobre `#f1f5fd`). Pior caso, o coração de favoritar dos cards: anel `rgba(211,212,213,.5)` sobre foto e fundo `#f8f9fa` que vai a `#d3d4d5`; quem navega por teclado quase não vê onde está.
- **Recommendation:** subir a especificidade: `:is(.btn, .page-link, .nav-link, .form-select, .form-control, .form-check-input, .navbar-toggler):focus-visible { outline: var(--app-focus-outline); outline-offset: var(--app-focus-offset); box-shadow: none; }`; estender `BaseCssTests` para medir o contorno computado de um `.btn`, de um `.page-link` e do coração; confirmar com `Tab` em "Publicar", na paginação e no coração.
- Relates-to: NFR-16, Task 0.7, Task 6.2

#### R-15 (B2-02) — Painel de filtros `sticky` mais alto que a janela esconde "Aplicar filtros" no notebook
- **Eixo:** Correção (usabilidade). **Base:** **(a confirmar no navegador)**: medida por cálculo a partir da folha do Bootstrap.
- **Where:** `wwwroot/css/pages/search.css:339-344`; `Views/Search/Index.cshtml:82` e `:196-199`.
- **Description:** a partir de 992 px o formulário é `position: sticky; top: 1rem` sem `max-height` nem rolagem própria. Com Carros ou Motos escolhidos o painel passa de uns 850 px; um elemento `sticky` mais alto que a janela fica preso no topo e o fim só reaparece quando a coluna termina.
- **Cenário:** notebook 1366×768 (janela útil perto de 650 px), `/busca?categoria=carros`: "Aplicar filtros" e "Limpar filtros" ficam abaixo da dobra e não sobem com a rolagem; só aparecem com Enter num campo ou rolando os 24 resultados.
- **Recommendation:** no mesmo `@media (min-width: 992px)`, `max-height: calc(100vh - 2rem); overflow-y: auto;` em `.search-filters`; conferir no Chromium a 1366×768 com Carros escolhido.
- Relates-to: US-002 (S01, S02), NFR-17

#### R-16 (B2-03) — O recuo da árvore de categorias na busca usa espaços comuns, que o navegador colapsa
- **Eixo:** Correção. **Base:** **(a confirmar no navegador)**: certeza alta pelo comportamento padrão de `<option>`.
- **Where:** `Views/Search/Index.cshtml:99` (`@(new string(' ', (option.Depth - 1) * 3))@option.Name`); o teste `SearchRulesTests.cs:465` confere o `Depth` do modelo, não o que o navegador mostra.
- **Description:** o texto de um `<option>` tem o espaço em branco removido e colapsado; o recuo some. O painel (`CategoriesController.cs:160`) usa `—` e o formulário do anúncio (`AdFormFactory.cs:160`) usa `Pai › Nome` e `optgroup`; só a busca depende de espaço.
- **Cenário:** "Serviços" (id 7, principal) e "Serviços" (id 66, filha) ficam idênticas numa lista plana de 147 opções; o visitante não sabe qual escolhe, e perde a noção de grupo e subgrupo de "Casas", "Apartamentos" etc.
- **Recommendation:** usar espaço não separável (U+00A0, escrito como `&nbsp;` ou ` `), prefixo visível (`— `) ou `optgroup` por categoria principal como no formulário do anúncio; teste de integração que procure o recuo no HTML e E2E que leia o texto da opção.
- Relates-to: US-002-S02, US-001, ADR-006

#### R-17 (B2-04) — Voltar pelo histórico mostra contador e corações de favoritos desatualizados
- **Eixo:** Correção. **Base:** **(a confirmar no navegador)**: leitura do código; o cache de página inteira (bfcache) não reexecuta o JavaScript.
- **Where:** `wwwroot/js/modules/favorites-ui.js:39-64` (`ativarFavoritos` só pinta ao carregar e escuta o evento próprio e `storage`); `wwwroot/js/pages/favorites.js:110-114`; as páginas públicas não têm `no-store` (só a rota do fragmento, `FavoritesController.cs:22`).
- **Description:** `storage` não chega a documentos no bfcache e o evento próprio só dispara na aba que gravou; ao restaurar a página nada chama `atualizarContador()` nem `pintarTodos()`. Os outros módulos já usam `pageshow` (`ad-confirm.js:19`, `ad-edit.js:250`, `search.js:224`).
- **Cenário:** busca (contador 0, coração vazio) → abre um anúncio → "Favoritar" → Voltar: a busca volta com "Favoritos (0)" e o coração vazio. Em `/favoritos`: desfavorita na página do anúncio, volta, e o anúncio continua na lista.
- **Recommendation:** `window.addEventListener("pageshow", (e) => { if (e.persisted) { atualizarContador(); pintarTodos(); } })` em `ativarFavoritos`; o mesmo gancho chamando `carregar()` em `pages/favorites.js`; E2E com `page.GoBackAsync()` depois de favoritar noutra página.
- Relates-to: US-005 (S01, S03, S04), NFR-12

#### R-18 (B2-05) — O selo "Cidade/UF informadas manualmente (CEP não conferido)" estoura 320 px
- **Eixo:** Correção (responsividade, NFR-17). **Base:** **(a confirmar no navegador)**: medida por cálculo (52 caracteres em negrito de 12 px, `nowrap` do `.badge`).
- **Where:** `Areas/Panel/Views/ReviewQueue/Preview.cshtml:61` (`badge text-bg-warning me-1`); `.badge{… white-space:nowrap}` no CSS do Bootstrap.
- **Description:** o selo mede cerca de 330 a 400 px contra os 296 px úteis do `container` em 320 px e vira transbordo do documento (nenhuma folha define `overflow-x` no `body`).
- **Cenário:** o Administrador abre a pré-visualização de um anúncio com localização digitada à mão (US-008-S14) num celular de 320 px; a tela ganha rolagem horizontal. A medição da 6.2 (44 telas, dados padrão com CEP automático) não incluiu este estado (`ScreenData.cs` não monta localização manual).
- **Recommendation:** `text-wrap` no selo (`white-space: normal !important` do Bootstrap) ou `<span class="badge …">Manual</span>` seguido do texto em parágrafo normal; incluir a tela com localização manual no `ScreenCoverageTests`.
- Relates-to: NFR-17, US-010, US-008-S14, Task 4.1

### 🟢 Suggestion

Formato compacto: cada sugestão traz os mesmos cinco campos. "Já no BACKLOG" indica onde o item já estava registrado (não é novo).

#### R-19 (A-06) — Autorização de Administrador só no controller em quatro serviços de alto impacto
- **Where:** `Infrastructure/Identity/UserManagement.cs:76,139,244`; `Categories/CategoryManagement.cs:27`; `Settings/SiteSettingsManagement.cs:17`; `Ads/ReviewQueue.cs:18`. Contraste: `AdReview.cs:28,35` e `AdTakedown.cs` chamam `EnsureAdministrator()`.
- **Description:** NFR-13 e o comentário de `IAdService` dizem que o serviço decide a autoria, não o controller. Para as contas da equipe isso não vale: `ResetPasswordAsync` e `ChangeRoleAsync` (equivalentes a tomar uma conta) confiam em `[Authorize(Policy = Administrator)]` no `UsersController`.
- **Cenário:** hoje sem caminho de exploração (a matriz do Checkpoint 6 descobre as 78 rotas por reflexão). Um controller, uma ação de API ou um serviço em segundo plano futuro que injete `IUserManagement` sem a política poderia promover a si mesmo sem que nenhum teste de serviço falhe.
- **Recommendation:** `EnsureAdministrator()` (via `ICurrentUser`) no início de `UserManagement`, `CategoryManagement`, `SiteSettingsManagement` e `ReviewQueue`; `Forbidden` para os demais (o `BootstrapAdminInitializer` não passa por `IUserManagement`).
- Relates-to: NFR-13, RC-16

#### R-20 (A-07) — Fotos: acesso e situação do anúncio só são conferidos antes da transação, não dentro do bloqueio
- **Where:** `Photos/AdPhotoService.cs:72,91` (`GetForEditAsync` antes de `InTransactionAsync`), `:49-59` (envio), `:107-131` (`InsertAsync` só reconfere a contagem).
- **Description:** o `UPDLOCK` serializa os envios do mesmo anúncio, mas a transação não relê `Status`; o envio leva segundos.
- **Cenário:** o autor envia uma foto e, na outra aba, "Enviar para revisão"; a foto termina depois e entra num anúncio Em revisão que ele já não pode editar. Defesa: a publicação reconfere as pendências, então um anúncio sem a foto mínima não seria publicado.
- **Recommendation:** dentro de `InTransactionAsync`, depois do `UPDLOCK`, ler `Status`/`AuthorId` e repetir `AdAccess.CanEdit`.
- Relates-to: US-008-S02, US-008-S06, NFR-13

#### R-21 (A-08) — Duas implementações da mesma regra: dinheiro em reais e limite de ano
- **Where:** `Formatting/CurrencyAndDateFormatter.cs:18` (`FormatCurrency`, chamada só numa mensagem de erro) contra `Ads/AdPresentation.cs:33` (`FormatMoney`); `Fields/ModelYearRules.cs` e `ManufactureYearRules.cs` (`IsValid`, sem chamador em `src`) contra `FieldValueParser.ApplyYear` e `SearchService.ParseYear`.
- **Description:** hoje produzem o mesmo texto, mas `testing.md` pede uma só fonte ou tabela de paridade quando uma regra tem duas representações. Parte do tema (acentos) já está no BACKLOG 70.
- **Cenário:** alguém muda o limite do ano ou o formato do dinheiro num lado e o outro continua com o valor antigo, sem teste que falhe.
- **Recommendation:** `FormatMoney` chama `FormatCurrency` (ou o inverso) e os `IsValid` passam a ser o único lugar da comparação, ou saem. **Já no BACKLOG parcialmente** (item 70, só a normalização de acentos).
- Relates-to: NFR-20, Task 3.8, testing.md (paridade)

#### R-22 (A-09) — Código de teste e código sem chamador em produção
- **Where:** `Recovery/PasswordRecoveryQueue.cs:41-54` (`WaitUntilIdleAsync` e o contador `_pending`, "serve aos testes", e usa `DateTime.UtcNow` em vez do `TimeProvider`); sem chamador em `src`: `ModelYearRules.IsValid`, `ManufactureYearRules.IsValid`, `AdStatus.IsValid`, `FieldGroupRegistry.All`, `CategoryTreeSnapshot.Empty`; `Core/ServiceCollectionExtensions.AddCore()` devolve a coleção sem registrar nada.
- **Description:** o `_pending` faz `Interlocked` em toda fila só para um helper de teste.
- **Cenário:** leitor novo gasta tempo achando quem usa cada um; o contador faz trabalho em produção que só os testes aproveitam.
- **Recommendation:** mover o helper para o projeto de teste, remover o que só os testes usam ou comentar a intenção, e remover `AddCore` vazio ou registrar nele o que for do Core. **Já no BACKLOG parcialmente** (item 112, só `IPhotoReprocessing`).
- Relates-to: Task 1.4, simplificação

#### R-23 (A-10) — `ProductionGuard` das ferramentas confia na palavra "prod"
- **Where:** `tools/CitiesImport/ProductionGuard.cs:19-24` e `tools/VehicleCatalogExport/ProductionGuard.cs:22-27`.
- **Description:** a trava recusa `load` quando servidor, banco ou nome da aplicação contêm "prod". Bancos de hospedagem compartilhada costumam ter nomes como `DB_184233_gazeta`; `--environment Development` contra um deles passa (e a trava confia no `--environment` digitado).
- **Cenário:** `CITIES_IMPORT_TARGET_CONNECTION` aponta, por engano, para o banco real; `load --environment Development` executa os `MERGE` em produção, sem a revisão do script que a trava existe para garantir. Importa agora: a carga real dos municípios (F6-01) e do catálogo (F6-04) usará estas ferramentas.
- **Recommendation:** lista de permissão (`(local)`, `localhost`, `.`, `127.0.0.1`, `host.docker.internal`, nomes terminados em `-dev`/`_test`) em vez de lista de proibição, ou uma segunda variável explícita (`…_ALLOW_LOAD=1`).
- Relates-to: ADR-008, Task 2.5, Task 3.2

#### R-24 (A-11) — O validador do catálogo de veículos não confere os limites das colunas
- **Where:** `tools/VehicleCatalogExport/CatalogValidator.cs:19-113`; colunas em `Infrastructure/Data/Configurations/VehicleCatalogConfiguration.cs:28,39,71` (marca e modelo `nvarchar(150)`, versão `nvarchar(250)`).
- **Description:** nome acima de 150/250, `Id <= 0` e ano fora de uma faixa razoável passam pelo validador e só falham ao aplicar o script (`XACT_ABORT` desfaz tudo, sem dano). A ferramenta de municípios (`CityValidator`) já confere o limite de 80.
- **Cenário:** o script é "aprovado", e a falha só aparece na hora da carga em produção.
- **Recommendation:** validar tamanho (150/150/250), `Id > 0` e `Year` entre 1900 e ano atual + 2, enviando ao relatório de descartados.
- Relates-to: ADR-008, Task 2.5 (A5)

#### R-25 (A-12) — `BootstrapAdminInitializer` ignora o resultado de `AddToRoleAsync` e não é atômico
- **Where:** `Identity/BootstrapAdminInitializer.cs:105-113`.
- **Description:** se a atribuição do papel falhar (ou o processo cair entre as duas chamadas) existe um usuário sem papel; na próxima partida `GetUsersInRoleAsync` ainda devolve zero, o código tenta criar de novo e recebe "e-mail repetido", só registrado no log.
- **Cenário:** o site sobe sem nenhum Administrador, justamente o caso que a rotina existe para evitar (S17).
- **Recommendation:** checar o `IdentityResult` e, em falha, remover o usuário recém-criado, ou fazer os dois passos numa transação como `UserManagement.RunAsync`.
- Relates-to: US-014-S17, ADR-003, ADR-011

#### R-26 (A-13) — Redefinição pelo link: senha, desbloqueio e auditoria são gravações separadas
- **Where:** `Recovery/PasswordRecoveryService.cs:107`, `:117-123`, `:126`.
- **Description:** cada chamada do `UserManager` grava por si. O comentário "Atômico" (linha 106) vale só para o token e a política de senha.
- **Cenário:** se a auditoria `user.recover_password` falhar, a senha já mudou e não há registro (RC-16).
- **Recommendation:** reaproveitar o padrão de transação de `UserManagement.RunAsync`, ou registrar a auditoria primeiro como tentativa e confirmar depois.
- Relates-to: US-007, RC-16, RC-12

#### R-27 (A-14) — Cidade vinda do ViaCEP não tem o limite de 80 caracteres conferido
- **Where:** `Location/ViaCepLookup.cs:79` (devolve `city.Trim()` sem limite), `Location/CepService.cs:43-50`, `Ads/AdDraftService.cs:388-390`; o ramo `Informed` já confere `Ad.CityMaxLength`.
- **Description:** `CepCache.City` e `Ads.City` são `nvarchar(80)`; o primeiro erro é engolido por `StoreAsync` (e dispara o `Clear()` do R-03), o segundo vira "Não foi possível salvar o anúncio agora" sem causa visível. Sem a carga do IBGE (F6-01) o nome vem direto do ViaCEP.
- **Cenário:** o ViaCEP devolve um nome fora do normal e o salvamento falha sem explicação.
- **Recommendation:** recusar `localidade` maior que 80 como "resposta ilegível" em `ViaCepLookup.Parse`.
- Relates-to: US-008 (CEP), ADR-007, NFR-24

#### R-28 (A-15) — Espaços invisíveis e cultura criada em seis lugares
- **Where:** `Formatting/PriceText.cs:79-81` (três `Replace` com **U+00A0** e **U+202F** literais no arquivo); `new CultureInfo("pt-BR")` em `PriceText`, `FieldValueParser`, `AdPresentation`, `AdSpecs`, `CurrencyAndDateFormatter` e `CityNames` (este com `GetCultureInfo`).
- **Description:** caracteres invisíveis no editor, fáceis de "normalizar" por engano; a cultura é repetida.
- **Cenário:** alguém troca os espaços especiais por espaços comuns e o formatador de preço quebra sem mensagem.
- **Recommendation:** escrever os dois como `" "` e `" "` com comentário do motivo, e uma constante `Cultures.PtBr` única.
- Relates-to: NFR-20, code-style.md

#### R-29 (A-16) — A limpeza de fotos apaga todo arquivo sem registro, sem trava de proporção
- **Where:** `Photos/OriginalsCleanupService.cs:130-131` e `:167-186` (`DeleteOrphanVersionsAsync`).
- **Description:** é correto e protegido contra envio em andamento (carência de 24 horas, só nomes gerados pelo site, sem seguir atalhos). Falta uma trava para o caso de o banco consultado **não ser o dono da pasta**: o serviço roda em qualquer ambiente com `PhotoStorage:BasePath` configurado.
- **Cenário:** um banco restaurado de backup antigo, ou um ambiente de teste apontando para a pasta de produção, faz toda versão WebP com mais de 24 horas parecer órfã e ser apagada na primeira rodada, 1 minuto depois da partida; sem backup de disco os arquivos não voltam.
- **Recommendation:** abortar e registrar `Error` quando a rodada achar órfãos acima de, por exemplo, 20% das versões listadas (ou de um teto absoluto), exigindo confirmação por configuração; documentar no runbook que a pasta de fotos é exclusiva de um banco.
- Relates-to: ADR-005, Task 3.6, AR-04

#### R-30 (A-17) — O limite de pedidos de redefinição por e-mail permite que qualquer visitante silencie a recuperação de outra pessoa
- **Where:** `Recovery/PasswordRecoveryService.cs:67-71` (`byEmail > 3` descarta sem enviar).
- **Description:** consequência direta do RC-11 com resposta neutra (RC-13). O limite por IP (10/hora) barra um atacante parado, não um com vários IPs. O Administrador ainda é recuperável por outro Administrador (US-014-S10) e a conta não é bloqueada.
- **Cenário:** 4 pedidos por hora com o e-mail do Administrador impedem o envio do e-mail verdadeiro naquela hora; repetido a cada hora, a recuperação por e-mail da conta fica indisponível.
- **Recommendation:** decisão de produto: aceitar e registrar, ou contar só os pedidos para contas existentes (a contagem é interna e a resposta continua neutra). Confirmar o aceite no `/scan`.
- Relates-to: RC-11, RC-13, US-007

#### R-31 (B1-06 = C-10) — Conta desativada ou bloqueada responde sem gastar o hash: o tempo revela que a conta existe
- **Where:** `Security/TeamSignInManager.cs:26-27`; `Areas/Panel/Controllers/AccountController.cs:42-43` (`DummyHash`), `:80-90`.
- **Description:** o comentário (linhas 42-43) quer o mesmo tempo para e-mail inexistente e senha errada, mas `PasswordSignInAsync` faz `PreSignInCheck` antes de verificar a senha: conta desativada (`NotAllowed`) e bloqueada (`LockedOut`) saem sem rodar o PBKDF2.
- **Cenário:** um atacante mede `POST /painel/entrar`: respostas de poucos milissegundos indicam conta existente e desativada ou bloqueada. Alcance pequeno (só a equipe tem conta; o contador por IP corta em 5 falhas), mas contradiz a intenção declarada e a mensagem única de US-006-S04/S05.
- **Recommendation:** rodar `Hasher.VerifyHashedPassword(user, DummyHash, submittedPassword)` também quando o resultado for `IsNotAllowed` ou `IsLockedOut`; ou aceitar e registrar no `SCAN_REPORT` (F6-52).
- Relates-to: THREAT_MODEL I2, US-006-S04, US-006-S05, NFR-06

#### R-32 (B1-07) — Na `/api`, sem o cabeçalho `RequestVerificationToken` o servidor lê o corpo antes do limite de 11 MB
- **Where:** `Filters/AntiforgeryJsonFilter.cs:22` (`Order = int.MinValue`) e `:36`; `Controllers/Api/AdPhotosController.cs:34` (`[RequestSizeLimit]`, ordem 900); `Middleware/BodyLimitMiddleware.cs:20-29`.
- **Description:** sem o cabeçalho, `ValidateRequestAsync` lê o formulário em busca do campo do token, antes do `RequestSizeLimitFilter`; o teto vigente é o padrão do servidor (30 MB).
- **Cenário:** uma conta de Redator envia POSTs em partes de 29 MB para `/api/v1/ads/1/photos` sem o cabeçalho; o servidor guarda o corpo em memória e disco temporário antes de responder 400 (só equipe logada, 30 por minuto).
- **Recommendation:** em `AntiforgeryJsonFilter`, recusar de imediato quando o cabeçalho não existe, sem chamar `ValidateRequestAsync`: a `/api` só aceita o token pelo cabeçalho.
- Relates-to: RC-21, NFR-11

#### R-33 (B1-08) — Erros gerados pelo framework na `/api` saem fora do contrato do ARCHITECTURE §8
- **Base:** lido no código; o ramo (3) é **(a confirmar com teste de corpo em partes)**.
- **Where:** `Program.cs:48-53` (nenhum `ConfigureApiBehaviorOptions`); `Middleware/BodyLimitMiddleware.cs:24` e `:40` (413 sem corpo); `Controllers/Api/*.cs` (`[ApiController]`); `BodyLimitTests.cs:19-30` só cobre `Content-Length`.
- **Description:** o §8 diz que a lista de códigos é exaustiva, com `traceId` sempre presente. Três saídas fogem: (1) corpo multipart malformado em `POST /api/v1/ads/{id}/photos` vira o 400 automático do `[ApiController]` (`ValidationProblemDetails` em inglês, sem `code`); (2) o 413 do `BodyLimitMiddleware` não tem corpo, `code` nem `traceId`; (3) corpo em partes acima do limite lança `BadHttpRequestException` dentro da leitura do formulário, que o `ExceptionHandlingMiddleware` tende a registrar como 500 `INTERNAL_ERROR` com `LogError`.
- **Cenário:** o cliente `apiFetch` mostra `problem.detail ?? problem.title`, que no caso (1) é "One or more validation errors occurred.", em inglês, na tela de fotos.
- **Recommendation:** `InvalidModelStateResponseFactory` que devolva `VALIDATION_ERROR` no formato do contrato; `ApiProblem.WriteAsync` no 413 da `/api`; tratar `BadHttpRequestException` pelo seu `StatusCode` no `ExceptionHandlingMiddleware`.
- Relates-to: ARCHITECTURE §8, RC-21, US-008-S02, error-handling.md

#### R-34 (B1-09) — A senha provisória não impede o uso da API de fotos, CEP e cidades
- **Where:** `Areas/Panel/Controllers/PanelControllerBase.cs:15` (o `MustChangePasswordFilter` só cobre quem herda dela); `Controllers/Api/AdPhotosController.cs:24`, `CepController.cs:19`, `CitiesController.cs:19` (`[Authorize(Policy = Writer)]` apenas).
- **Description:** US-006-S09 diz que a troca de senha vem antes de qualquer página; a `/api` não consulta o claim `must_change_password`.
- **Cenário:** o Administrador redefine a senha de um Redator que já tem anúncios; com a senha provisória ele entra e chama `DELETE /api/v1/ads/{id}/photos/{photoId}` ou `POST …/cover` sem trocar a senha.
- **Recommendation:** pôr a regra nas políticas (`RequireAssertion(ctx => !ctx.User.HasClaim(TeamClaims.MustChangePassword, "1"))` em `Writer` e `Administrator`) e dar a `PasswordController` uma política sem essa exigência.
- Relates-to: US-006-S09, NFR-07

#### R-35 (B1-10) — `CategoriesController` ignora o `ModelState`: `parentId` de tipo errado cria categoria principal
- **Where:** `Areas/Panel/Controllers/CategoriesController.cs:60-63`; `Areas/Panel/Models/CategoryViewModels.cs` (`public int? ParentId`).
- **Description:** `ParentId=abc` falha no binder, o valor fica `null` e `CreateAsync(null, name)` cria uma categoria **principal**; não há teste de tipo errado (busca por `ParentId` com texto em `tests/`).
- **Cenário:** um formulário antigo ou adulterado de um Administrador envia `parentId=abc` e o item aparece na raiz da árvore, não sob "Imóveis".
- **Recommendation:** `if (!ModelState.IsValid)` antes do serviço, voltando ao formulário com a mensagem; teste de tipo errado na rota real.
- Relates-to: US-013, NFR-13, testing.md (wrong-type input)

#### R-36 (B1-11) — Cabeçalhos `Server` e `X-Powered-By` não são removidos
- **Where:** `Program.cs` e `web.Production.config.example` (nenhum `removeServerHeader`, `AddServerHeader=false` ou `<remove name="X-Powered-By"/>`).
- **Description:** `rules/security.md` mostra `RemoveServerHeader()`; no IIS a resposta anuncia `Server: Microsoft-IIS/10.0` e `X-Powered-By: ASP.NET`.
- **Cenário:** um scanner de versões identifica o servidor sem esforço (informação de reconhecimento, não exploração).
- **Recommendation:** `<security><requestFiltering removeServerHeader="true"/></security>` e `<httpProtocol><customHeaders><remove name="X-Powered-By"/>` no `web.config` publicado, mais uma linha no checklist do `/deploy`.
- Relates-to: NFR-10, A05

#### R-37 (B1-12) — A CSP não tem `base-uri`
- **Where:** `Middleware/SecurityHeadersMiddleware.cs:12-14` (e a linha idêntica no ARCHITECTURE §7); `CspTests.cs:16`.
- **Description:** `default-src 'self'` não cobre `base-uri`; só `frame-ancestors` e `form-action` foram acrescentados.
- **Cenário:** uma injeção de marcação que não execute script ainda poderia redefinir a base de URLs relativas; sem custo para fechar, pois o site não usa `<base>`.
- **Recommendation:** acrescentar `base-uri 'self'; object-src 'none'` e atualizar o ARCHITECTURE §7 e `CspTests.cs:16`.
- Relates-to: NFR-10

#### R-38 (B1-13) — Os limites por IP valem para o endereço IPv6 completo
- **Where:** `Security/RateLimitingExtensions.cs:133` (`ClientIp`); `Areas/Panel/Controllers/AccountController.cs:62` (origem do `LoginFailureCounter`) e `:123`.
- **Description:** quem tem um bloco IPv6 /64 (qualquer conexão residencial) troca de endereço a cada pedido e escapa do contador de login (5 falhas), do limite global e do de recuperação. O bloqueio da conta (5 falhas, 15 minutos) segue valendo.
- **Cenário:** um atacante com /64 testa senhas contra um e-mail sem nunca atingir o limite por IP; só o bloqueio da conta o detém.
- **Recommendation:** usar o prefixo /64 como chave para IPv6 em `ClientIp` e em `source`; em conjunto com SEC-01 (R-11), que continua bloqueando o lançamento.
- Relates-to: NFR-06, RC-10, SEC-01

#### R-39 (B1-14 + C-11) — Chaves de configuração "só para testes" valem em Production e `Site__BaseUrl` aceita `http://` e `ftp://`
- **Where:** `Security/RateLimitingExtensions.cs:34,42,52` e `Configured(...)` `:93-96` (`RateLimiting:GlobalPerMinute`, `PhotosPerMinute`, `PhotoUploadsPerMinute`); `SendGridOptions.BaseUrl`, `ViaCepOptions.BaseUrl`; `Core/Configuration/SiteOptions.cs` (`[Url]`), usada em `PublicUrl.cs:11`, no mapa do site, nos canônicos e no link de redefinição; `AuthenticationOptions.SessionMinutes` (1 a 120).
- **Description:** um valor copiado do ambiente de teste para o `web.Production.config` afrouxaria RC-6 e o limite global em silêncio; apontar `SendGrid:BaseUrl` para outro host enviaria a chave de API para lá; `Site:BaseUrl` em `http` gera link de redefinição sem TLS e canônicos errados, e a validação em Production passa.
- **Cenário:** `RateLimiting__PhotoUploadsPerMinute=1000` esquecido no arquivo de produção; ou `Site__BaseUrl=http://…`.
- **Recommendation:** em Production, ignorar ou limitar (teto) as chaves de limite, recusar `BaseUrl` que não seja `https` e host conhecido (`api.sendgrid.com`, `viacep.com.br`), exigir `https` em `Site:BaseUrl` (`Uri.TryCreate` com esquema `https`), com teste em `OptionsTests`. **Já no BACKLOG parcialmente** (linha 277, "conferir o valor de produção no `/deploy`"; F6-12 fala do valor dos limites, não do risco da chave).
- Relates-to: RC-6, RC-20, RC-21, ADR-009, ADR-011, US-007, F6-12

#### R-40 (B1-15) — Identificadores em português contra `lang-dotnet.md`
- **Where:** `Models/SearchViewModels.cs:16-55` (`Categoria`, `Cidade`, `PrecoMin`, `PrecoMax`, `Marca`, `Modelo`, `AnoDe`, `AnoAte`, `KmMax`, `AreaMin`, `AreaMax`, `Ordem`, `Pagina`); `Filters/AntiforgeryJsonFilter.cs:42` (chave de erro `"requisicao"` no JSON da API); `Security/RateLimitingExtensions.cs:29,37,63,138` (políticas `"fotos"`, `"fotos-envio"`, partição `"estaticos"`, categoria de log `"LimiteDeRequisicoes"`); `Program.cs:66` (verificação de saúde `"banco"`).
- **Description:** a regra manda inglês para propriedades de ViewModel, códigos e identificadores de log. Os nomes do endereço (`?precoMin=`) ficam em português pelo `[FromQuery(Name = …)]`; só a propriedade C# muda.
- **Cenário:** leitor ou ferramenta de busca por convenção não acha a propriedade; inconsistência com o resto do código.
- **Recommendation:** renomear as propriedades (os nomes do endereço ficam no atributo), `"request"` na chave de erro e nomes em inglês nas políticas e na categoria de log. **Já no BACKLOG parcialmente** (linha 297, item 24, só CSS e JavaScript; F6-34).
- Relates-to: lang-dotnet.md, F6-34

#### R-41 (B1-16) — Constantes de rota sem uso e o caminho do link de redefinição em três lugares
- **Where:** `Navigation/Routes.cs:44` (`PanelRoutes.ResetPassword`, sem chamador) e `:12` (`PublicRoutes.Ad`); `AccountController.cs:137` (`[HttpPost("redefinir-senha")]`); `Infrastructure/Recovery/RecoveryCode.cs:9` (`LinkPath`).
- **Description:** a rota do link enviado por e-mail existe como atributo no controller, como constante de Infrastructure (a que o e-mail usa) e como constante do Web sem uso.
- **Cenário:** mudar uma e esquecer a outra quebra o link sem aviso do compilador.
- **Recommendation:** uma só fonte (a constante do Core/Infrastructure), teste que abre o link montado pelo `PasswordRecoveryMailer` na rota real, e remover `PanelRoutes.ResetPassword`. **Já no BACKLOG parcialmente** (linhas 302 e 312 citam `PublicRoutes.Ad`; F6-39).
- Relates-to: US-007, F6-39

#### R-42 (B1-17) — O site não tem `FallbackPolicy`: rota nova sem `[Authorize]` nasce pública
- **Where:** `Security/IdentityExtensions.cs:75-77` (só `AddPolicy`). **Já no BACKLOG** (linha 318; F6-20, decisão do Product Owner).
- **Description:** confirmado no código atual; `AccessMatrixTests` cobre o risco, mas só depois de rodar.
- **Cenário:** um controller novo esquecido sem atributo fica público até o teste rodar.
- **Recommendation:** adotar antes do lançamento (`[AllowAnonymous]` explícito nos controllers públicos: 7 de página, `PublicAds`, `PublicCities`, `VehicleCatalog` e o SEO); ver F6-20.
- Relates-to: NFR-13, F6-20

#### R-43 (B1-18) — `ConflictException` e `ForbiddenException` sem tratamento na publicação, rejeição e retirada
- **Where:** `ReviewQueueController.cs:141-159`, `:181-206`; `AdsController.cs:256-276`. **Já no BACKLOG** (linha 168; F6-06).
- **Description:** os `catch` cobrem só `NotFoundException` (e `ValidationException` na rejeição); o `Error` do `HomeController` mantém o status do `AppException` (409/403), então não há 500, mas a pessoa perde a mensagem da SPEC. O F6-06 diz "vira página 500", mas é página de erro genérica com 409.
- **Cenário:** dois Administradores decidem o mesmo anúncio e o segundo vê a tela genérica de erro.
- **Recommendation:** tratar as duas exceções nas três ações, devolvendo a mensagem da SPEC; corrigir o texto do F6-06.
- Relates-to: US-010, US-011, T6, F6-06

#### R-44 (B1-19) — `AdsController` agrega lista, formulário, envio e retirada
- **Where:** `Areas/Panel/Controllers/AdsController.cs` (452 linhas, 8 dependências). **Já no BACKLOG** (linha 169; F6-30).
- **Description:** confirmado.
- **Cenário:** cada mudança numa das responsabilidades arrisca as outras.
- **Recommendation:** dividir por responsabilidade num `/simplify`, sem mudar comportamento.
- Relates-to: clean-code.md, F6-30

#### R-45 (B1-20) — `ids` inválido em `/favoritos/lista` passa pelo tratador de exceções
- **Where:** `FavoritesController.cs:27`, `PublicAdsController.cs`. **Já no BACKLOG** (linha 294, item 8; F6-38).
- **Description:** lança `ValidationException` em rota de página: 400 correto, mas registrado com `LogError` e pilha.
- **Cenário:** qualquer robô enche o log de erros.
- **Recommendation:** devolver `BadRequest()` direto, sem lançar.
- Relates-to: US-005, F6-38

#### R-46 (B1-21) — `AllowedHosts: "*"`
- **Where:** `appsettings.json:11`. **Já no BACKLOG** (linha 294, item 7; F6-51).
- **Description:** com `Site__BaseUrl` obrigatório o risco é baixo (não há mais link montado do Host em produção).
- **Cenário:** um cabeçalho `Host` forjado é aceito fora de Production.
- **Recommendation:** restringir ao domínio real no `web.config` de produção.
- Relates-to: NFR-10, F6-51

#### R-47 (B1-22) — `/Home/Status/{código}` e `/Home/Error` acessíveis direto
- **Where:** `Controllers/HomeController.cs:50,68`. **Já no BACKLOG** (linha 350, item 10, as 14 sugestões do Checkpoint 6; OPEN-006).
- **Description:** a rota convencional responde direto a `/Home/Status/503` e `/Home/Error`.
- **Cenário:** um 5xx sintético para testar monitoramento ou enganar um usuário.
- **Recommendation:** restringir por `[NonAction]`/rota só de reexecução, ou aceitar com registro.
- Relates-to: NFR-25, OPEN-006

#### R-48 (B2-06) — `carregar()` em `favorites.js` pode rodar em duplicidade
- **Where:** `wwwroot/js/pages/favorites.js:46-114` (`host.replaceChildren()` na linha 48; `host.append(lista)` na linha 80 depois de `await`). **Já no BACKLOG** (linha 296, item 17; F6-36).
- **Description:** confirmado ainda aberto.
- **Cenário:** 150 favoritos (dois lotes) e outra aba favorita mais um durante a espera: `aoMudar(true)` chama `carregar()` de novo; as duas anexam a sua `ul` (cards em dobro e contagem errada).
- **Recommendation:** contador de geração; só a última chamada anexa.
- Relates-to: US-005-S03, Task 5.5, F6-36

#### R-49 (B2-07) — Listas encadeadas sem cancelamento e com falha só dentro do `<select>`
- **Where:** `wwwroot/js/modules/catalog-chain.js:34-56`; `wwwroot/js/pages/ad-edit.js:198-212`.
- **Description:** o `change` de Marca/Modelo/Ano e o de UF não guardam um `AbortController`, ao contrário da troca de categoria (`ad-edit.js:76-107`) e do CEP; a busca resolveu comparando o valor atual com o pedido (`search.js:67-74`). A mensagem de falha é o texto de uma `<option>` num `<select>` desabilitado (`catalog-chain.js:54`), sem `aria-live`.
- **Cenário:** escolhe a Marca A e logo a Marca B; a resposta de A chega depois e preenche os modelos de A com a Marca B selecionada. O servidor valida a cadeia, então vira erro de campo no envio, não dado errado gravado.
- **Recommendation:** comparar o valor pedido com o atual (como em `search.js`) ou usar `AbortController`; escrever a falha em `[data-ad-status]`.
- Relates-to: US-008-S11, ADR-008

#### R-50 (B2-08) — Contador de caracteres conta quebra de linha como 2 quando o formulário volta com erro
- **Where:** `Areas/Panel/Views/Ads/_AdGroupRegion.cshtml:8-9` (`Model.Description?.Length`) com `AdFormFactory.cs:96`; `AdFormRules.NormalizeLineBreaks` só é chamado em `AdDraftService.cs:207`.
- **Description:** a reexibição após erro (`Save`, `Create`, `Refresh`) usa o texto cru do navegador, com CRLF; o contador do servidor conta 2 por quebra, o do JS conta 1 (`counter.js:5`), e `AdFormRules.cs:11-14` diz que o número que a pessoa vê é o que o servidor confere.
- **Cenário:** descrição com 40 quebras de linha e 1.980 caracteres, um erro em outro campo: o formulário volta com "2020/2000" até a próxima digitação (o servidor aceitaria 1.980).
- **Recommendation:** normalizar `values.Description` em `AdFormFactory.BuildAsync` ou contar com `NormalizeLineBreaks` na view.
- Relates-to: US-008, NFR-14

#### R-51 (B2-09) — Duas implementações do limite do preço e um campo de dinheiro sem máscara
- **Where:** `wwwroot/js/modules/price.js:2` (`MAXIMO_DE_DIGITOS = 10`) contra `FieldLimits.MaxMoneyCents`; `_AdField.cshtml:10-13` (Condomínio e IPTU, `FieldType.Money`, sem `data-price-input`).
- **Description:** o teto está escrito dos dois lados sem ligação (o BACKLOG já registrou o conflito R$ 99.999.999,99 contra 9.999.999.999); na mesma tela "62000" no Preço vira "620,00" e no Condomínio vale R$ 62.000,00. **Já no BACKLOG parcialmente** (linha 86, só a máscara).
- **Cenário:** alguém muda o valor em C# e a máscara continua cortando em 10 dígitos, sem teste que falhe.
- **Recommendation:** passar o teto à view por `data-price-max-digits` calculado de `FieldLimits.MaxMoneyCents`, ou um teste que compare as duas constantes; aplicar `data-price-input` também aos campos `Money`, ou documentar a diferença.
- Relates-to: US-008-S15, ADR-002, testing.md (paridade), F6-21

#### R-52 (B2-10) — `<time>` sem `datetime` e totais do painel sem separador de milhar
- **Where:** `Views/Shared/_AdBody.cshtml:27` (`<time>@Model.PublishedOn</time>` com "05/10/2026"); `Areas/Panel/Views/Ads/Index.cshtml:75`, `ReviewQueue/Index.cshtml:24`, `Categories/Index.cshtml:52` (`list.Total + " anúncios"`) contra `Search/Index.cshtml:208` e `Favorites` (`N0` em pt-BR).
- **Description:** "05/10/2026" não é data válida para o HTML sem `datetime` (as listas do painel já fazem ISO, `Ads/Index.cshtml:108`); "1500 anúncios" contra "1.500".
- **Cenário:** leitor de tela e rastreadores leem a data sem formato; o painel mostra números sem milhar.
- **Recommendation:** `datetime` em ISO e `ToString("N0", pt-BR)` nos totais do painel.
- Relates-to: US-003, US-012

#### R-53 (B2-11) — Hierarquia de títulos pula níveis
- **Where:** `Views/Shared/Components/AdCard/Default.cshtml:19` (`<h3>` fixo); `Views/Favorites/Index.cshtml:13` (`h1` → cards `h3`); `Views/Search/Index.cshtml:238` (`h1` → `h2` "Filtros" → cards `h3`).
- **Description:** falta um `h2` de resultados; WCAG 1.3.1, boa prática.
- **Cenário:** quem navega por títulos com leitor de tela encontra "h1, h3" sem o nível intermediário.
- **Recommendation:** `h2` visível ou `visually-hidden` ("Resultados", "Seus favoritos") antes da grade.
- Relates-to: NFR-16

#### R-54 (B2-12) — Alvo de toque e padrão de erro de campo inconsistentes
- **Where:** `_ErrorState.cshtml:12`, `_EmptyState.cshtml:12`, `_NoResultsState.cshtml:12` (botão sem `alvo-toque`, cerca de 39 px contra 44 px de `base.css:48`); `_FieldErrors.cshtml:9-15` e `_AdField.cshtml:40-50` (`ul.small.text-danger`, id `error-X`, `div role="alert"`) contra `Account/SignIn.cshtml:34,39` e `Forgot.cshtml:24` (`span.invalid-feedback`, id `erro-X`; só o `Forgot` tem `role="alert"`).
- **Description:** duas aparências (vermelho `#dc3545` pequeno contra o vermelho escuro do Bootstrap) e dois padrões de id e anúncio; `#dc3545` sobre branco dá 4,52:1 (limite) e sobre `#f1f5fd` daria 4,14:1.
- **Cenário:** um erro de campo some para leitor de tela numa tela e é anunciado em outra.
- **Recommendation:** um parcial único de erro de campo (`.invalid-feedback` com a cor do tema) e `alvo-toque` nos botões dos estados.
- Relates-to: NFR-16, frontend.md (Forms)

#### R-55 (B2-13) — Código morto no produto
- **Where:** `Views/Shared/_NoResultsState.cshtml`, `Views/Shared/_Skeleton.cshtml`, `Models/PageStateViewModel.cs:21-29` (`SkeletonViewModel`), `wwwroot/css/components/estados.css:9-33,47-55` (`.esqueleto*`).
- **Description:** só as páginas de teste os usam; a busca escreveu a própria mensagem de "sem resultado" (`Search/Index.cshtml:222-235`). **Já no BACKLOG parcialmente** (linha 153 explica por que o esqueleto não se aplica ao painel; F6-44 cita que `_Skeleton.cshtml` não é incluído; a remoção não está registrada).
- **Cenário:** manutenção de código que ninguém usa.
- **Recommendation:** remover (e os testes de estado) ou ligar; `_NoResultsState` pode servir à busca e à lista do painel.
- Relates-to: NFR-03, Task 0.7, F6-44

#### R-56 (B2-14) — Arquivos de terceiros sem versão e peso de fonte sem `preload`
- **Where:** `_Layout.cshtml:10-11,61` e `_PanelLayout.cshtml:19-20,66` (`bootstrap.min.css`, `font-awesome.min.css`, `bootstrap.bundle.min.js` sem `asp-append-version`); `poppins.css:19-25` (peso 700, sem `preload` nem `size-adjust`).
- **Description:** sem `?v=` o arquivo sai com `no-cache` (`PerformanceExtensions.cs:55` só dá `immutable` com `v`): 6 pedidos de revalidação por página; o `font-awesome` não tem `font-display` e o peso 700 troca de fonte depois da primeira pintura. **Já no BACKLOG parcialmente** (L350, item 17, "fontes sem `?v=`"; F6-55 trata o peso).
- **Cenário:** a cada navegação o navegador revalida arquivos que quase nunca mudam; o logo e preços em negrito trocam de fonte depois de pintados.
- **Recommendation:** `asp-append-version="true"` nos três vendors e `preload` do 700 (ou usar 600 no logo).
- Relates-to: NFR-03, NFR-05, OPEN-006, F6-55

#### R-57 (B2-15) — "Favoritos (0)" aparece quando o número não é conhecido
- **Where:** `Views/Shared/_Layout.cshtml:28` (`(<span data-favoritos-contagem>0</span>)`).
- **Description:** sem JavaScript ou com `localStorage` bloqueado (S07) o cabeçalho diz "Favoritos (0)" em toda página, e o clique leva a uma página que diz "ative o JavaScript" (`Favorites/Index.cshtml:18`); com JavaScript há ainda um piscar de "0".
- **Cenário:** uma pessoa com 5 favoritos e armazenamento bloqueado vê "0".
- **Recommendation:** nascer sem o número (`hidden`) e mostrá-lo em `ativarFavoritos`.
- Relates-to: US-005-S07

#### R-58 (B2-16) — O formulário do anúncio não informa a obrigatoriedade a leitores de tela
- **Where:** `_AdGroupRegion.cshtml:12,29,49`, `_AdLocation.cshtml:14,31,43`, `_AdField.cshtml` (asterisco em `<span aria-hidden="true">`, sem `aria-required`); as telas de conta e de usuários usam `aria-required="true"` (`Account/SignIn.cshtml:33`, `Users/New.cshtml:16`).
- **Description:** a obrigatoriedade é "para enviar à revisão" (o rascunho só pede o título), então `required` nativo não serve; mas `aria-required` nos campos que as pendências exigem informaria o leitor de tela (WCAG 1.3.1, 3.3.2).
- **Cenário:** o leitor de tela só descobre o que falta depois de clicar em "Enviar para revisão" e ler a lista de pendências.
- **Recommendation:** `aria-required="true"` nos campos com `*` e ligar o parágrafo `:50` por `aria-describedby`.
- Relates-to: US-008, US-009-S02, NFR-16

#### R-59 (B2-17) — Falhas de rede sem retorno no campo (busca) e fila de fotos que pára com erro inesperado
- **Where:** `wwwroot/js/pages/search.js:60-100,102-107` (`try/finally` sem `catch`: a falha vira `unhandledrejection`); `wwwroot/js/modules/photos.js:111-112,157-165` (`falhou` relança o que não é `ApiError`; os itens restantes ficam "Na fila…").
- **Description:** só o aviso global "Algo deu errado" aparece, no alto da página.
- **Cenário:** sem rede ao escolher a UF, a lista de cidades fica só com "Todas as cidades", habilitada, e o aviso aparece fora da vista se a página estiver rolada; na galeria, um `TypeError` ao montar a foto deixa as demais paradas.
- **Recommendation:** `catch` com mensagem junto do campo (`aria-live`) e `try/catch` por foto dentro do laço de `processarFila`.
- Relates-to: US-002-S03, US-008-S05

#### R-60 (B2-18) — Duas técnicas para a tabela responsiva do painel
- **Where:** `Areas/Panel/Views/Users/Index.cshtml:17-56` (tabela `d-none d-md-block` e lista de cartões `d-md-none`, com `_UserActions` renderizado duas vezes por pessoa) contra `Ads/Index.cshtml:76-113` e `ReviewQueue/Index.cshtml:25-50` (tabela empilhada por CSS, `ads-index.css:234-269`, `review-queue.css`).
- **Description:** as duas folhas de CSS são cópias uma da outra; as tabelas com `display: block` em `table/tr/td` podem perder a semântica de tabela em leitores de tela (a conferência com NVDA está prevista no `/verify`, BACKLOG 307).
- **Cenário:** um ajuste de layout tem de ser feito em três lugares.
- **Recommendation:** uma classe comum (`.table-stack`) em `components/`, uma técnica só e `role="table|row|cell|columnheader"` nas variantes empilhadas.
- Relates-to: NFR-17, US-012, US-010, F6-17

#### R-61 (B2-19) — Achados da Fase 5 que continuam abertos no código (confirmação, não novos)
- **Where:** `Search/Index.cshtml:137,173,178,192` (`invalid-feedback d-block` com `hidden`: o `d-block` vence o `[hidden]`, comprovado em `bootstrap.min.css`, e a região `role="alert"` vazia fica no DOM; BACKLOG linha 295, item 15); `pages/layout.js:9` contra `modules/favorites-ui.js:9` (`mostrarAviso` duplicado) e `favorites-ui.js:66` (`export { atualizarContador }` sem importador; linha 295, item 16); `pages/search.js:188` usa `lerNumero` também para o ano ("2.020" vale 2020 no navegador e é recusado pelo servidor; linha 296, item 18 cita só a tabela copiada à mão); nomes de CSS e JS em português (`categoria-tile`, `ad-galeria__*`, `configurarPainel`, `alvo-toque`, `cabecalho`; linhas 10 e 297, item 24). **Já no BACKLOG** (F6-34, F6-35).
- **Description:** quatro itens agrupados, cada um já registrado.
- **Cenário:** o grupo só some do BACKLOG quando os quatro fecharem.
- **Recommendation:** tratar conforme F6-34 e F6-35; acrescentar a diferença do ano em `search.js:188` ao item 18.
- Relates-to: Task 5.4, F6-34, F6-35

#### R-62 (C-06) — Duas mensagens da SPEC são afirmadas pela constante do próprio código, e um cenário tem prova parcial
- **Where:** `Team/AccountTests.cs:129-140` (`AccountController.TooManyAttemptsMessage`; o texto "Muitas tentativas. Tente novamente em alguns minutos." não aparece literal em nenhum teste); `Team/FirstAccessTests.cs:138` (`PasswordController.SameAsProvisionalMessage`); `Ads/SubmitForReviewTests.cs: US009S04…` (sem `RejectedById` nem `RejectedAt`; `ClearRejection` em `Ad.cs:214-218` limpa três campos e o teste afirma um).
- **Description:** assertir a constante em vez do texto da SPEC deixa passar um erro de digitação na constante (o teste compara o código com ele mesmo).
- **Cenário:** alguém altera a mensagem da constante para um texto errado e os testes continuam verdes.
- **Recommendation:** literais da SPEC nos testes US-006-S06 e US-006-S09; acrescentar `RejectedAt`/`RejectedById` nulos ao `US009S04`.
- Relates-to: US-006-S06, US-006-S09, US-009-S04

#### R-63 (C-07) — Um teste vazio do template conta entre os 1.722
- **Where:** `tests/GazetaMarketplace.Web.Tests/Test1.cs` (`TestMethod1` sem corpo). **Já no BACKLOG** (L12; F6-22).
- **Description:** passa sempre; infla a contagem e contraria "testes são prova".
- **Cenário:** o total de testes passa a valer 1.721 quando o arquivo sair.
- **Recommendation:** remover o arquivo e o item do BACKLOG.
- Relates-to: testing.md, F6-22

#### R-64 (C-08) — Textos desatualizados: RR-10, "RC" do .NET e exemplo de `global.json`
- **Where:** `security/PRE_DEV_REVIEW.md` (RR-10 "fixado em `10.0.0-rc.2`" e lista de bloqueios do lançamento); `architecture/ARCHITECTURE.md` §2 ("RC no momento") e AR-02; `CLAUDE.md` raiz (exemplo de `global.json` em `rc.2`); contra `global.json` (`10.0.100`) e `Directory.Packages.props` (`10.0.12`, Task 0.3b).
- **Description:** o código já usa o .NET 10 final; o RR-10 aparece como bloqueio do lançamento que já foi resolvido (resta confirmar que o servidor do provedor tem o runtime ou publicar *self-contained*, AR-02).
- **Cenário:** um leitor da lista de bloqueios acredita que ainda há um impedimento resolvido.
- **Recommendation:** fechar o RR-10 no PRE_DEV (com a data da Task 0.3b) e manter só a AR-02.
- Relates-to: RR-10, AR-02, Task 0.3b

#### R-65 (C-09) — Contrato e modelo de dados não acompanham o código
- **Where:** `architecture/api/openapi.yaml` (sem `/api/v1/public/cities`; os caminhos de `brands/suggest` e `product-types/suggest` constam sem implementação, por decisão); `ARCHITECTURE.md` §6.2 e NFR-19 (sem a tabela `PasswordRecoveryAttempts`, que guarda e-mail e **IP** por 24 h; o IP também vai ao log de 14 dias); ADR-012 e §2/§10 ("uma só tarefa em segundo plano"; hoje há `OriginalsCleanupService`, `PasswordRecoveryWorker`, `PasswordRecoveryCleanupService` e `BootstrapAdminInitializer`); ADR-009 (tempo limite 10 s, código 15 s em `Infrastructure/ServiceCollectionExtensions.cs:121`); ADR-004 (`RowVersion` no usuário; o código usa o `ConcurrencyStamp` do Identity e transação serializável).
- **Description:** nenhum é defeito de código; são lacunas de documentação. A de dados pessoais merece atenção na LGPD: o NFR-19 diz "só nome, e-mail e hash".
- **Cenário:** um auditor de privacidade lê o NFR-19 e não sabe que IP e e-mail de tentativas de recuperação ficam guardados.
- **Recommendation:** uma passada de alinhamento em `/docs` ou `/arch` em modo conformidade: incluir o endpoint e a tabela, mencionar IP e e-mail em tentativas de recuperação e em logs, e atualizar o ADR-012 (F6-40 trata só do endpoint de favoritos).
- Relates-to: NFR-19, ADR-004, ADR-009, ADR-012, F6-40

#### R-66 (C-12) — 41 critérios de aceite desmarcados e três textos obsoletos em `plans/plan.md`
- **Where:** `plans/plan.md:122-125, 159-164, 199-203, 236-237, 266-271, 310-312, 358-368, 457, 1065-1068`; `:1978` ("44 telas", e são 45).
- **Description:** `todo.md` marca as tarefas como feitas e o código as cumpre (exceto as pendências R-07 e R-11), mas as caixas dos critérios nunca foram marcadas na Fase 0 e nos Checkpoints 0 e 2; o Checkpoint 0 ainda diz que a cobertura "não foi medida".
- **Cenário:** o plano parece incompleto a quem confere os critérios; os dois critérios realmente abertos (limitadores da 0.4 e RC-10) ficam escondidos entre os 41.
- **Recommendation:** marcar o que está atendido, deixar `[ ]` os dois critérios abertos com a razão, corrigir "44" para "45".
- Relates-to: Task 0.1 a 0.6, Checkpoint 0, Checkpoint 2

#### R-67 (C-13) — 177 arquivos de backup do kit versionados
- **Where:** `.claude.backup-20260929-222958/` e `.claude.backup-20260929-225205/` (177 arquivos rastreados, incluindo dois `settings.json`; conferido com `git ls-files`). O arquivo solto `web` citado pelo revisor C **já não existe**.
- **Description:** os backups do kit não são código do produto e carregam configuração antiga de hooks.
- **Cenário:** um colaborador ou uma ferramenta lê o `settings.json` antigo como configuração válida; o repositório carrega ruído.
- **Recommendation:** `git rm -r --cached` dos backups e entrada no `.gitignore`, num commit próprio (regra de propriedade do commit, `git-workflow.md`).
- Relates-to: git-workflow.md (Commit ownership)

#### R-68 (C-14) — O projeto de navegador não guarda evidência de falha
- **Where:** `tests/GazetaMarketplace.Web.Tests.Playwright` (nenhum `Tracing`, `Screenshot` nem `.runsettings`; grep vazio); `BACKLOG.md:352` (39 testes falharam uma vez e o log da primeira rodada se perdeu); OPEN-001.
- **Description:** duas instabilidades já ficaram sem causa porque a rodada que falhou não deixou nada para ler; é a razão pela qual o OPEN-001 não pode ser fechado. **Já no BACKLOG parcialmente** (BACKLOG:352 pede salvar o log).
- **Cenário:** o teste `US005S06` falha de novo no `/verify` e, sem trace nem captura, continua sem causa.
- **Recommendation:** ligar trace e captura de tela só em falha (`Context.Tracing` no ajudante da classe base, gravando em `reports/test-artifacts/runner/`) e salvar o log inteiro de cada rodada (o runbook já pede).
- Relates-to: OPEN-001, OPEN-007, commands/test.md (artefatos de falha)

#### R-69 (C-15) — Decisões de pilha sem registro e restos de template
- **Where:** `Directory.Packages.props:23` (SQLite de teste, commit `eff1a0f`, sem decisão registrada); `wwwroot/lib/font-awesome` (4.7, de 2016, fora da lista de `frontend.md` e `tech-stack.md`; só `architecture/design-system.md` §1 e §5 a justificam); entradas de CPM sem referência (`Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation`, `Microsoft.AspNetCore.OpenApi`); versões de `Magick.NET-Q8-x64 14.17.2`, `Microsoft.Data.SqlClient 6.1.6`, `Dapper 2.1.89` e `Serilog.AspNetCore 10.0.0` não verificáveis aqui.
- **Description:** desvios pequenos da pilha aprovada sem passar pelo Technology Decision Process.
- **Cenário:** o `/scan` encontra um pacote vulnerável (o Magick.NET atualiza a cada correção do ImageMagick) sem registro prévio de por que está ali.
- **Recommendation:** uma linha de decisão para SQLite (testes) e Font Awesome (licença e versão fixa) em `tech-stack.md` ou no design system; remover as duas entradas sem uso; rodar `dotnet list package --vulnerable --include-transitive` no `/scan`.
- Relates-to: tech-stack.md (Technology Decision Process), AR-06, Gate 8

### ✅ Good

Os melhores pontos de cada revisor, consolidados (18):

1. **Matriz de acesso por descoberta de rotas.** `AccessMatrixTests.cs` (tabela `Matrix`, linhas 38-127) lê a mesma verdade três vezes (tabela escrita à mão, atributos do código e chamadas reais sem login, como Redator e como Administrador); rota nova sem decisão quebra o teste; `EveryUnsafeEndpoint_RejectsARequestWithoutTheAntiforgeryToken` cobre toda escrita; `OwnershipMatrixTests` prova a posse por rota (13 rotas).
2. **SQL sempre parametrizado, com barreira dupla.** `SqlBuilder.cs:50-125` (lista de caracteres permitida, recusa aspas, `--` e número solto; ordenação só por mapa fixo; `Page` com teto de 100), todos os repositórios Dapper com parâmetro nomeado e `CHARINDEX` no lugar de `LIKE`; `SqlBuilderTests.FragmentoComLiteralOuTextoPerigoso_E_Recusado`; nenhum `FromSqlRaw` nem `Database.Migrate()` em `src/`.
3. **Filtro "só publicados" em um lugar.** `SqlFragments.OnlyPublished` (`Data/SqlFragments.cs`) em toda leitura pública; `PublishedAdReader.cs:15-19`; `PhotoDelivery.cs:20-38` devolve o mesmo `null` para "não existe" e "não pode ver" (`EntregaTests.AnuncioNaoPublicado_Devolve404IgualAoInexistente…`).
4. **Fotos: caminho confinado e imagem decodificada com política.** Chaves por regex ancorada (`FileSystemPhotoStorage.cs:25-45`), `Confine` do caminho final (`:345-361`), atalhos ignorados; formato pela assinatura (`PhotoSignature.cs`), decodificador forçado e pixels limitados antes de decodificar (`MagickImageProcessor.cs:24-34`), `policy.xml` que nega tudo por padrão, no máximo 2 decodificações simultâneas (`PhotoIngestion.cs:19-21`), `Strip()` (GPS e EXIF não saem; `MetadadosTests.Gps_NaoSobrevive_NasVersoes`).
5. **Concorrência de decisões bem desenhada.** `AdService.TransitionAsync` grava situação e auditoria no mesmo `SaveChanges` (`AdService.cs:70-75`); `AdReview`/`AdTakedown` refazem a decisão uma vez e devolvem `AlreadyDecided` (`AdReview.cs:46-86`); `AdSubmission` transforma o clique duplo em `AlreadySent` (`AdSubmission.cs:45-61`); `RowVersion` traduzido para `ConflictException` (`AppDbContext.cs:100-113`); transação `Serializable` em `UserManagement.cs:338-359` e `CategoryManagement.cs:238-263`; `ReviewDecisionConcurrencyTests` no SQL Server real.
6. **Tabela de passagens única e conferida.** `AdStatusRules.cs:16-26` bate linha a linha com o Apêndice A da SPEC (9 passagens; só as duas de envio têm `AuthorAllowed`); `AdAccess.cs:14-35` aplica leitura, edição e passagem sem duplicar a regra.
7. **Defesa em profundidade no banco.** `CK_Ads_*` e colunas calculadas com `TRY_CAST` (`AdConfiguration.cs:31-57`), sem cascata de exclusão; script idempotente com as 13 migrations (`db/scripts/gazeta-idempotente.sql`); `ComputedColumnsDifferentialTests` (teste diferencial exigido por `testing.md`).
8. **Logs mascarados.** `MaskingEnricher.cs` mascara nomes sensíveis, e-mails e `token=`/`code=`, inclusive em propriedades aninhadas; nenhum log lê senha, token ou link (`PasswordRecoveryMailer.cs`, `BootstrapAdminInitializer.cs:53-60`); `MaskingTests`, `US001S06`, `US012S08`.
9. **Recuperação de senha sem enumeração.** O pedido faz o mesmo trabalho exista a conta ou não (`PasswordRecoveryService.cs:44-84`), o envio sai da fila depois da resposta (`PasswordRecoveryWorker.cs`), o token é protegido, ligado ao propósito e ao carimbo de segurança e distingue expirado de usado (`RecoveryTokenProvider.cs:52-90`); `PasswordRecoveryLimitsTests`, `US007S04`, `US007S05`.
10. **Entradas numéricas defendidas.** `DecimalInput.TryParseCents` compara antes de multiplicar (`:62-74`; o 🟡 do Checkpoint 5 está corrigido), `FavoriteIds` só aceita 0-9 sem zero à esquerda (`FavoriteIds.cs:18-62`), paridade JavaScript × C# com 48 entradas (`FavoriteIdsParityTests`).
11. **Login, cookie e sessão.** Hash falso para e-mail inexistente e mensagem única (`AccountController.cs:43,84`); endereço de retorno só local (`:214-217`, `ReturnUrlExterno_E_Ignorado`); cookie `HttpOnly`, `Secure`, `SameSite=Lax`, 30 minutos deslizantes, revalidação a cada 5 minutos (`IdentityExtensions.cs:82-93`; `SessionTests.cs:34,82,99`).
12. **Antiforgery, autoria e rascunho cego a concorrência.** `AntiforgeryJsonFilter.cs` responde no contrato da API; `PanelControllerBase.cs:13-15` junta `Authorize(Writer)`, `no-store` e o filtro da senha provisória; `AdFormSubmission` sem campos de decisão (RC-14, `EditViewModelsTests`); `AdDraftService.UpdateAsync:93-100` exige `RowVersion`.
13. **Erro e saúde sem vazamento; encaminhamento fail-closed.** `ExceptionHandlingMiddleware.cs:41` (mensagem genérica, pilha só no log), `DatabaseAndMigrationHealthCheck.cs:29` (só o tipo da exceção), `ForwardingExtensions.cs:19-43` (sem `KnownProxies` o middleware nem entra; `RateLimiterTests.cs:160,171`), `CorrelationIdMiddleware.cs:23` (só `[A-Za-z0-9_-]{1,64}`).
14. **Sem XSS e com CSP respeitada.** Zero `Html.Raw` com texto de usuário e zero `innerHTML`/`insertAdjacentHTML`/`eval` (`RawOutputTests`, `XssInAllScreensTests`, `JsModulesTests.cs:27`); nenhum `<script>` com corpo, `style=` nem manipulador inline nas 77 views; dados do servidor para o JavaScript só por `data-*`; `ActionUrl` passa por `Url.IsLocalUrl` (`_ErrorState.cshtml:10`).
15. **Melhoria progressiva e armazenamento tolerante.** Filtros abrem por `:target` sem JavaScript (`search.css:362-364`), exclusão de categoria e remoção de foto têm página de confirmação e janela por cima, favoritos toleram armazenamento bloqueado, JSON quebrado e cota cheia (`favorites.js:10-16,23-30,47-54,61-73`).
16. **Front-end acessível e estável.** Galeria com `<dialog>`, foco preso e devolvido, setas por teclado e `aria-live` (`ad-gallery.js:164-172`, `_AdGallery.cshtml:25`); CLS controlado por `width`/`height`/`aspect-ratio` e painel de filtros recolhido no HTML (`AdCard/Default.cshtml:12`, `Search/Index.cshtml:78-81`; `SearchLayoutShiftTests` mede 0,0043); contraste calculado e documentado (`base.css`, `bootstrap-tema.css:4-22`); mensagens iguais às da SPEC (conferidas por `grep`).
17. **Provas fortes e honestas.** O limite de 10 s das consultas é medido trancando a tabela no SQL Server real (`PanelAdListQueryTests.cs:353`, `SearchQueryTests.cs:535`); 128 de 128 cenários da SPEC citados em teste e 35 de 36 amostrados afirmam o efeito; 11 mutações mortas; host de teste isolado (`WebFactory`, `IntegrationWebFactory`); o `TEST_REPORT` registra mutações, a investigação da contagem do E2E e cada lacuna com dono.
18. **Ferramentas e utilitários.** `CityValidator` reaproveita o `Normalizer` do site (sem segunda implementação em SQL) e confere a mesma restrição única do banco; as duas ferramentas escrevem arquivo por temporário e trocam, e rodam o `load` numa transação; `ExpiringCache.cs:19-50` não guarda valor lido enquanto outra requisição invalidava.

### Cobertura da leitura

| Revisor | Leu por inteiro | Ficou de fora ou só em estrutura |
|---|---|---|
| A (Core, Infrastructure, db, tools) | `Core/**` (142 `.cs`), `Infrastructure/**` (97 `.cs` + `policy.xml`), `tools/**` (16 arquivos), cabeçalhos do script de banco | `Core/Fields/FieldLists.cs` (888 linhas de listas de opções; só a estrutura e as checagens de id duplicado do construtor); `Data/Seeds/InitialCategories.cs` (lidas as ~75 primeiras linhas); as **13 migrations e os Designers gerados** (só conferi que a lista bate com `gazeta-idempotente.sql`); `db/seed/**` (README e cabeçalho/rodapé das amostras) |
| B1 (Web em C#) | 70 de 70 arquivos `.cs` do projeto Web, `Program.cs` linha a linha, `appsettings*`, `web.Production.config.example`, `launchSettings.json` | `.cshtml`, CSS e JavaScript (com o B2); Core e Infrastructure (com o A) |
| B2 (views, JS, CSS) | 77 `.cshtml`, 18 módulos e páginas JavaScript, 12 arquivos de CSS próprio | `bootstrap.min.css` só conferido em seletores; **nenhum navegador foi aberto** (por isso os 🟡 R-14 a R-18 são "a confirmar") |
| C (transversal) | os 17 arquivos `rules/*.md` e os 13 `rules/overrides/*.md`; PRE_DEV_REVIEW, SECURITY_REQUIREMENTS, THREAT_MODEL, ARCHITECTURE e ADR-001 a 012; `Program.cs`, a segurança e os middlewares do Web, serviços de Infrastructure; 36 cenários da SPEC contra seus testes; `AccessMatrixTests`, `OwnershipMatrixTests`, `RateLimiterTests`; TEST_REPORT, CODE_REVIEW, plan, todo, BACKLOG e triagem | `openapi.yaml` (só os caminhos); `dotnet list package --vulnerable` não foi rodado (sem rede e sem execução); o catálogo de componentes (só Development) |

**Limite comum:** ninguém rodou build, testes nem navegador. Os números de cobertura e de testes vêm do `TEST_REPORT.md`; os do consolidador (seção 1) vêm de `grep` e `git ls-files`.

## 4. Action Items

Prioridade: **P0** bloqueia o lançamento ou é falha de segurança ou de dados; **P1** é tratar antes do lançamento; **P2** é melhoria ou aceitar com registro. O dono de cada item e o texto completo estão em `plans/BACKLOG-TRIAGEM-FASE-6.md` §4 (ids `F7-NN`); os novos achados abertos estão em `plans/BACKLOG.md`.

- [ ] **P0 · R-01** (/fix-issue): persistir as chaves do Data Protection (`AddDataProtection` + `PersistKeysToFileSystem` + `SetApplicationName`) e provar com dois hosts na mesma pasta e com o conteúdo `key-*.xml`. **Bloqueia o Gate 7 e o `/scan`.**
- [ ] **P0 · R-11** (/infra): obter do provedor o IP do proxy, configurar `ForwardedHeaders:KnownProxies` na implantação e provar que duas origens não compartilham o contador; `Warning` na partida quando vazio em Production.
- [ ] **P1 · R-02, R-03, R-04, R-05, R-06** (/fix-issue): completude do Publicado editado pelo Administrador; `CepService` sem `ChangeTracker.Clear()`; `PriceText` só com 0-9; fuso com plano B do Windows; herança da lista de opções em categoria nova.
- [ ] **P1 · R-07, R-08, R-09, R-10** (/fix-issue): decidir e alinhar a política `auth` (e o teste que prova uma rota de mentira); log `Warning` de permissão negada; sessão vencida no `fetch` de "trocar categoria"; `[IgnoreAntiforgeryToken]` nas páginas de status.
- [ ] **P1 · R-14, R-15, R-16** (/fix-issue): foco visível em botões, paginação, abas e campos; "Aplicar filtros" sempre alcançável; recuo das categorias na busca.
- [ ] **P1 · R-23, R-24, R-29, R-39** (/fix-issue): lista de permissão no `ProductionGuard`; limites das colunas no validador do catálogo; trava de proporção na limpeza de fotos; chaves de teste e esquema `https` em Production.
- [ ] **P1 · R-36** (/infra): remover `Server` e `X-Powered-By` no `web.config` e pôr no checklist do `/deploy`.
- [ ] **P1 · R-13** (/simplify): remover o operador `!` (184 linhas de teste e 5 de `src/`) e travar a volta.
- [ ] **P1 · R-65** (/review): alinhar contrato, tabela `PasswordRecoveryAttempts`, retenção de IP e e-mail (LGPD) e ADRs 004, 009 e 012.
- [ ] **P1 · R-68** (/test): trace e captura de tela em falha no projeto de navegador (pré-requisito para fechar OPEN-001).
- [ ] **P1 · R-12** (decisão do Product Owner): aceitar e registrar que FluentValidation não foi adotado, ou adotar.
- [ ] **P1 já na triagem:** R-42 (F6-20, `FallbackPolicy`, decisão do Product Owner) e R-43 (F6-06, conflito e proibição sem tratamento).
- [ ] **P2:** os demais 38 achados novos (R-17, R-18, R-19 a R-22, R-25 a R-28, R-30 a R-35, R-37, R-38, R-40, R-41, R-47, R-49 a R-60, R-62, R-64, R-66, R-67, R-69), mais os 6 já na triagem (R-44, R-45, R-46, R-48, R-61, R-63), conforme a triagem §4.

## 5. Test Coverage

Números do `reports/TEST_REPORT.md`, seção **"/test — Gate 6 da Fase 6"** (commit `7f3d3d1`, Release, SQL Server 2022 real em contêiner): **2.211 testes distintos, 2.211 passaram, 0 falhas, 0 pulados** (1.722 unitários do site + 27 da ferramenta de municípios + 43 do catálogo de veículos + 162 de integração + 257 de navegador, dos quais 253 no E2E e 4 de métricas rodados à parte com `GAZETA_VITALS=1`). **Cobertura (gate, modo greenfield, união de unitários e integração): 98,0% de linhas (5.581 de 5.697) e 91,8% de ramos (1.642 de 1.788)**, contra as metas de 80% e 75%; `Core` 98,9% e 94,8%, `Infrastructure` 96,9% e 88,6%, `Web` 97,3% e 88,2%. **13 de 783 métodos a 0%**, todos estruturais ou de uma linha chamados por produção, nenhum com regra de negócio sem teste que o produto já chame. Veredito do Gate 6: **PASS**.

**O que este review acrescenta aos números (anti-vácuo e plano):**
- Dos 36 cenários da SPEC auditados pelo revisor C (abrir o teste e perguntar se falharia caso o recurso fosse removido), **35 afirmam o efeito observável e 1 é parcial** (`US009S04`: não afirma `RejectedAt` nem `RejectedById`, R-62). Duas mensagens da SPEC são afirmadas pela constante do código (R-62). O teste `RateLimiterTests.SextaTentativaDeLogin_Devolve429` **não** prova o limite do login real (R-07). Nenhum cenário só prova "sem erro".
- A cobertura alta **não vê** o R-01 (a suíte roda num só processo), o R-03 (contextos separados), o R-04 (a tabela de `PriceText` não tem dígito Unicode), o R-10 (todos os testes de status são GET) nem os defeitos de CSS e de bfcache (R-14 a R-18): são lacunas de **tipo** de teste, não de linhas.
- `plans/todo.md`: as 40 tarefas das Fases 0 a 6 e os 7 checkpoints estão `[x]`, com entrega no código para cada uma. `plans/plan.md` mantém 41 critérios `[ ]` (R-66), dos quais dois **realmente** não estão atendidos como escritos: os limitadores da 0.4 (R-07) e o RC-10 (R-11).
- Matriz de permissões da SPEC (15 linhas) × `AccessMatrixTests`: todas cobertas.

### Disposição dos OPEN-001 a OPEN-007

> **Numeração.** Os sete ids e os títulos abaixo são os da seção 12 do `TEST_REPORT.md` ("Itens abertos para o `/review`"), que é a fonte. O pedido do Product Owner descrevia o **OPEN-007** como "métricas só com `GAZETA_VITALS=1`"; no relatório esse item é o **OPEN-004**, e o **OPEN-007** é "sem `results.json`/`.trx` do executor". Segui o relatório.

| OPEN | Título (TEST_REPORT §12) | Disposição | Razão, evidência e dono |
|---|---|---|---|
| OPEN-001 | Instabilidade de `FavoritesE2ETests.US005S06_…` ("Enviar para revisão?" não aparece em 15 s), 1 vez em 5 rodadas completas | **DEFERRED-to-/verify** | `PublishingFlow.PublishAsync` já contém a correção da 5.6 (`SaveAndSubmitAsync` espera o título "Enviar para revisão?" antes do segundo clique, `PublishingFlow.cs:126`) e a espera já foi esticada para 15 s (`AssemblySetup.cs`); logo a falha restante não é o clique duplo. **Causa não determinada**: o projeto de navegador não tem `Tracing`, captura de tela nem `.runsettings` (R-68), então a falha não deixa evidência. Hipóteses a descartar com captura: limite global do site de teste, pendência de envio, resposta 409. **Dono: /verify (com trace ligado), depois de R-68 pelo /test** |
| OPEN-002 | Dois testes de tela (axe e larguras de `not-found-page`) e a regra "compilar antes de `--no-build`" | **CLOSED** | `screens.json:16` tem a tela (45 telas no arquivo); `docs/RODAR-TESTES-DE-INTEGRACAO-E-E2E.md:140` traz a regra de compilar o projeto de navegador antes de `--no-build`. Resta só a correção textual "44 telas" para 45 em `plan.md:1978` (R-66). **Dono: /test** |
| OPEN-003 | Tripwire: confirmar o falso positivo do `viacep.com.br` (manipulador falso) | **CLOSED** | O cliente HTTP é registrado com `ConfigurePrimaryHttpMessageHandler(() => new StubHandler(respond))` (`Cep/CepEndpointTests.cs:198-199`), com o endereço pedido conferido no próprio manipulador (`:161`); `ViaCepLookupTests.cs:40` usa `DelegatingStub`; `CepHarness` troca o `ICepLookup` por um falso (`Support/CepHarness.cs:79-80`). Nenhum teste unitário alcança a rede. **Dono: /test** |
| OPEN-004 | Os 4 testes de métricas só rodam com `GAZETA_VITALS=1`; incluir no roteiro do `/verify` | **DEFERRED-to-/verify** | Confirmado: `Performance/VitalsTests.cs:20` `[RequiresVariables("GAZETA_BASE_URL", "GAZETA_VITALS")]`. A variável está no comentário da classe (`:15`), em `plan.md:2026` e no `TEST_REPORT`, **mas não no runbook** `docs/RODAR-TESTES-DE-INTEGRACAO-E-E2E.md` (busca sem resultado). Ação: uma linha no runbook e no roteiro do `/verify`. **Dono: /verify** |
| OPEN-005 | Cerca de 60 usos antigos do operador `!` nos testes, sem efeito (nullable desligado) | **DEFERRED-to-/simplify** (P1, número corrigido) | O número real, contado pelo consolidador: **184 linhas em 50 arquivos de teste e 5 linhas em `src/`** (`FileSystemPhotoStorage.cs:53,75,104`; `AdConfiguration.cs:87,100`), mais de três vezes o dito. Sem efeito em execução (nullable desligado), mas proibido por `lang-dotnet.md`, `code-style.md` e `testing.md` (R-13). **Dono: /simplify** |
| OPEN-006 | As 14 sugestões 🟢 do `/review` do Checkpoint 6 que continuam abertas | **PARCIALMENTE CLOSED** (3 fechados; 11 DEFERRED por item) | **Fechados:** 9 (BREACH em página reexecutada, corrigido em `79798a0`/`28557fc`, `CompressionAndCacheTests`), 21 (botão "Filtros" sem JavaScript: link `#filtros` com `:target`) e 15 (nota de relatório sobre o limite do oráculo do axe, sem ação). **Ainda valem 11:** 8 (`CompressionAndCacheTests.cs:177` engole resposta com falha) → /test; 10 (`/Home/Status/{código}` direto, R-47) → /fix-issue; 11 (`StatusPagesTests.cs:65-77` sem URL de saúde) → /test; 12 (`HomeController` repete a condição, namespace em bloco, sem `sealed`) → /simplify; 13 (`sem-js.css:23` `!important`) → /simplify; 14 (`RawOutputTests` não procura `style="…"`) → /test; 16 (6 `Assert.Inconclusive`) → /test; 17 (fontes sem `?v=`, R-56) → /fix-issue; 18 (`plan.md:1978` "44 telas", R-66) → /review; 19 (BACKLOG:318-321, F6-20/F6-52) → decisão do Product Owner (F6-20); 20 (p95 medido em processo) → /verify. O BACKLOG:350 ainda lista os 14 |
| OPEN-007 | Sem `results.json`/`.trx` do executor: decidir se vale o pacote de relatório | **DEFERRED-to-/infra** | O executor da Microsoft.Testing.Platform só gera `.trx` com a extensão `Microsoft.Testing.Extensions.TrxReport` (pacote novo, passa pelo Technology Decision Process). Sem risco para o produto; só vale se houver CI. Ação: registrar no runbook que o resultado é o log e o código de saída. **Dono: /infra** |

## 6. Compliance Check

> `PASS` exige `Evidence` (arquivo:linha, ligação no pipeline ou nome de teste). Controles transversais citam **onde estão ligados** no pipeline de `Program.cs`, não só "definidos". Ordem real do pipeline: `:90` encaminhamento de cabeçalhos (só com `KnownProxies`) → `:91` correlation id → `:92` cabeçalhos de segurança e CSP → `:93-95` tratador de erros da API → `:96-98` tratador de erros das páginas → `:100-102` páginas de status → `:104` HTTPS → `:105` compressão (fora de `/painel`) → `:107-114` cultura → `:115` roteamento → `:116` cache dos estáticos versionados → `:117` limite de corpo → `:119` autenticação → `:121` limitador → `:122` autorização → `:127-128` saúde; filtros globais de antiforgery em `:50-52`.

### 6.1 Uma linha por arquivo de regra (17 de `rules/` + 13 de `rules/overrides/` = 30)

**Resultado: 6 PASS · 11 WARNING · 1 FAIL · 12 N/A.** A base é a tabela do revisor C (7 PASS · 10 WARNING · 1 FAIL · 12 N/A); **rebaixei `error-handling.md` de PASS para WARNING** porque os revisores B1 e C, lidos juntos, mostram duas saídas fora do contrato `ProblemDetails` com `traceId` que a regra exige (R-33: 400 automático em inglês e 413 sem corpo na `/api`; R-10: página de 400 em branco para POST sem token). A mudança é minha, não do revisor C.

| Rule | Status | Evidence (file:line / test) | Notes |
|------|--------|-----------------------------|-------|
| api-conventions.md | WARNING | Rotas `api/v1/...` em kebab-case (`AccessMatrixTests.cs:38-127`); ProblemDetails com `code` e `traceId` (`ExceptionHandlingMiddleware.cs:36-71`, `ApiProblem.cs`); `PagedResult` (`PublicAdsController.cs:34`); contrato `RATE_LIMITED`/`FORBIDDEN` provado (`RateLimiterTests.cs:41-63`) | FluentValidation exigido pela regra e não adotado (R-12). Nenhum `[ProducesResponseType]` e nenhum pacote de versionamento (prefixo `v1` fixo). `openapi.yaml` sem `/api/v1/public/cities` (R-65) |
| brownfield.md | N/A | `.claude/PROJECT_PROFILE.md:3` Mode greenfield | Não ativo |
| clean-code.md | WARNING | Sem `.Result`/`.Wait()`/`async void` (busca em `src/`); `CancellationToken` propagado (`PanelAdListReadRepository.cs:28`); métodos pequenos nos serviços de domínio. Falhas já no BACKLOG: `AdsController` com 452 linhas e 8 dependências (R-44, F6-30), `SearchService.PrepareAsync` com 137 linhas (F6-35) | Código sem chamador (R-22, R-55); regra duplicada (R-21, R-51) |
| code-style.md | WARNING | Namespace de arquivo e `using` explícitos na maioria; `dotnet format` limpo segundo o `TEST_REPORT`. Falhas: operador `!` em 184 linhas de teste e 5 de `src/` (R-13); `HomeController.cs:16-18` com namespace em bloco e sem `sealed` (OPEN-006 item 12) | Caracteres invisíveis no código (R-28) |
| database.md | PASS | EF com `AsNoTracking`/projeção (`AdService.cs:87`); Dapper só por `SqlBuilder` com parâmetros nomeados (`SqlBuilder.cs:50-125`, `SqlBuilderTests.FragmentoComLiteralOuTextoPerigoso_E_Recusado`); nenhum `FromSqlRaw`/`Database.Migrate` (busca vazia); retry (`Infrastructure/ServiceCollectionExtensions.cs:45-47`); transações em `CreateExecutionStrategy` (`UserManagement.cs:341`, `AdDraftService.cs:62`, `CategoryManagement.cs:243`); script idempotente (`MigrationsTests`, `IntegrationTests/ScriptTests`) | Escritas Dapper só nas ferramentas, com `ProductionGuard` (a trava é frágil, R-23). O R-03 e o R-20 são de concorrência na camada de serviço, não de regra de banco |
| error-handling.md | WARNING | Hierarquia `AppException` (7 arquivos em `Core/Exceptions`); middleware da API (`Program.cs:93-95`); páginas com `UseExceptionHandler("/Home/Error")` mantendo o status esperado (`HomeController.cs:72-74`); pilha só no log; `StatusPagesTests`, `ErrorsTests` | **Rebaixado de PASS (ver 6.1):** erros do framework na `/api` fora do contrato (R-33), página de 400 em branco no POST sem token (R-10), `ConflictException`/`ForbiddenException` sem tratamento em três ações (R-43). `catch` vazios só em limpeza de E/S filtrada por tipo (`FileSystemPhotoStorage.cs:384`, `PhotoIngestion.cs:48`) |
| frontend.md | WARNING | Sem `Html.Raw` com texto de usuário nem `innerHTML` (`RawOutputTests.NoView_HasInlineScriptInlineHandlerOrJavascriptUrl`, `JsModulesTests.cs:27`); sem script nem manipulador inline; Bootstrap 5.3.8 estático (`wwwroot/lib/LEIAME.md`); jQuery só na validação (`_ValidationScriptsPartial.cshtml:1-2`); `lang="pt-BR"` (`_Layout.cshtml:3`) | Foco do Bootstrap vence o do projeto (R-14); painel `sticky` (R-15); recuo colapsado (R-16); bfcache (R-17); Font Awesome 4.7 fora da lista aprovada (R-69); nomes de CSS e JS em português (R-61, F6-34); `!important` em `sem-js.css:23` (OPEN-006 item 13) |
| git-workflow.md | WARNING | Commits convencionais com atribuição (`git log` de `f08e777` para trás só tem `feat/fix/docs/test/...`); árvore limpa (`git status --short` vazio) | 177 arquivos `.claude.backup-*` versionados (R-67) |
| monitoring.md | WARNING | Serilog JSON com máscara (`SerilogConfiguration.cs:19-27`, `MaskingEnricher.cs`); correlação (`CorrelationIdMiddleware.cs:20-39`); `/health/live` e `/health/ready` (`Program.cs:127-128`) | Sem OpenTelemetry, Prometheus e Jaeger: **desvio registrado** no ADR-010 (com gatilhos de reavaliação). "Permissão negada" sem registro nas páginas (R-08). Sem `/metrics` |
| naming-conventions.md | PASS | Tabelas e colunas em PascalCase e `IX_`/`UQ_` (26 `HasDatabaseName` em `Data/Configurations`); variáveis de ambiente `Bootstrap__AdminEmail`, `Site__BaseUrl` (`web.Production.config.example`); rotas em kebab-case (`painel/esqueci-minha-senha`); testes `Metodo_Cenario_Resultado` | Identificadores em português são tema de `lang-dotnet.md` (R-40), não desta regra |
| output-style.md | PASS | Todos os documentos lidos abrem com "Em resumo" (ARCHITECTURE, ADRs, PRE_DEV_REVIEW, TEST_REPORT, CODE_REVIEW, BACKLOG-TRIAGEM) e explicam o porquê das decisões | |
| principles-and-practices.md | WARNING | Colunas de auditoria e `rowversion` (`AppDbContext.cs:87-90`, `:150-165`); registro de ações sensíveis (`AuditLog.cs`); achados fora de escopo em `plans/BACKLOG.md` (§2.5 item 17); idempotência do envio (`US009S05`) | Documentos que descrevem um estado antigo (R-64, R-65, R-66); chaves sem persistência (R-01); autorização só no controller em quatro serviços (R-19) |
| project-structure.md | PASS | Core → só `Microsoft.Extensions.DependencyInjection.Abstractions` (`Core.csproj`); Infrastructure → Core; Web → Core e Infrastructure; nenhum `using` de ASP.NET, EF ou Dapper no Core (busca vazia); `AddInfrastructure`/`AddCore` em `Program.cs:81`, `:84` | `AddCore()` vazio (R-22) |
| security.md | **FAIL** | Segredos fora do repositório (`SecretsTests.Repositorio_NaoContem_ConnectionStringComSenha`); consultas parametrizadas; cabeçalhos e CSP (`Program.cs:92` + `SecurityHeadersMiddleware.cs:11-33`, `HeadersTests`); CORS nenhuma (`CorsTests`); antiforgery (`Program.cs:50-60`); cookie `HttpOnly/Secure/Lax` (`IdentityExtensions.cs:87-90`); autenticação e autorização (`Program.cs:119,122`, `AccessMatrixTests`) | **R-01:** o controle "chaves do Data Protection em pasta persistente" (§Data Protection, ARCHITECTURE §7, ADR-011, ameaça S3) está definido e validado na partida, **mas não ligado**. Também R-07 (política `auth` sem uso), R-08 (permissão negada sem log), R-11 (SEC-01), R-12 (validação), R-36 (cabeçalhos `Server`/`X-Powered-By`), R-37 (CSP sem `base-uri`) |
| system-design.md | PASS | Verificações de saúde (`Program.cs:127-128`); limitadores (`Program.cs:121`); tempo limite de consulta de 10 s provado no SQL Server real (`PanelAdListQueryTests.cs:353`, `SearchQueryTests.cs:535`); cache de memória de 10 minutos com invalidação (`AppDbContext.cs:134-141`, `ExpiringCache.cs:19-50`); paginação por deslocamento aceita no ADR-006 | Disjuntor e Polly fora da v1 (ADR-012) |
| tech-stack.md | WARNING | Pilha respeitada: ASP.NET Core 10, EF Core 10 + Dapper, SQL Server, Serilog, MSTest, Playwright, Bootstrap; nenhum Moq, Redis, Kafka, Hangfire, Polly (`Directory.Packages.props`) | FluentValidation aprovado na AR-06 e não adotado (R-12); SQLite de teste e Font Awesome sem decisão registrada, entradas de CPM sem uso (R-69) |
| testing.md | WARNING | Cobertura 98,0% e 91,8% (`TEST_REPORT` §6); teste diferencial das colunas calculadas (`ComputedColumnsDifferentialTests`); paridade de favoritos e de leitor de número; isolamento do host (`WebFactory`, `IntegrationWebFactory`, `git status` limpo no `/test`); 128 de 128 cenários citados | `!` nos testes (R-13); teste vazio `Test1.TestMethod1` (R-63); o teste de limite de login prova um controlador de teste (R-07); duas mensagens pela constante (R-62); sem trace nem captura de falha no E2E (R-68); lacuna de tipo de teste em `PriceText` (R-04) e `CategoriesController` (R-35) |
| overrides/database-sqlserver.md | PASS | `int IDENTITY`, `rowversion` (`AppDbContext.cs:87-90`), `EnableRetryOnFailure`, PascalCase, índices `IX_/UQ_`, script idempotente, nunca `Migrate()`; segredos por variável de ambiente e `ValidateOnStart` (`OptionsExtensions.cs:16-36`) | O usuário usa o `ConcurrencyStamp` do Identity, não `rowversion` (R-65); sem `CommandTimeout(30)` explícito (o padrão do driver já é 30 s) |
| overrides/lang-dotnet.md | WARNING | Identificadores em inglês, rotas e textos em português; injeção por construtor; `record` para DTOs; `CancellationToken` por último; `Nullable`/`ImplicitUsings` desligados, sem `?` de referência (busca vazia); CPM sem `Version=` em nenhum `.csproj` | Operador `!` (R-13); identificadores em português em ViewModels e políticas (R-40); `HomeController` sem `sealed` |
| overrides/database-oracle.md | N/A | `.claude/PROJECT_PROFILE.md:6` SQL Server | Fora do Profile |
| overrides/database-mysql.md | N/A | `.claude/PROJECT_PROFILE.md:6` | Fora do Profile |
| overrides/database-postgres.md | N/A | `.claude/PROJECT_PROFILE.md:6` | Fora do Profile |
| overrides/database-mongodb.md | N/A | `.claude/PROJECT_PROFILE.md:6` | Fora do Profile |
| overrides/framework-nodejs-web.md | N/A | `.claude/PROJECT_PROFILE.md:5` Core C# | Fora do Profile |
| overrides/framework-php-laravel.md | N/A | `.claude/PROJECT_PROFILE.md:5` | Fora do Profile |
| overrides/lang-nodejs.md | N/A | `.claude/PROJECT_PROFILE.md:5` | Fora do Profile |
| overrides/lang-php.md | N/A | `.claude/PROJECT_PROFILE.md:5` | Fora do Profile |
| overrides/test-nodejs.md | N/A | `.claude/PROJECT_PROFILE.md:5` | Fora do Profile |
| overrides/test-php.md | N/A | `.claude/PROJECT_PROFILE.md:5` | Fora do Profile |
| overrides/monitoring-elk.md | N/A | `.claude/PROJECT_PROFILE.md:7` observabilidade base | Fora do Profile |

**Contagem final: 30 linhas = 6 PASS (database, naming-conventions, output-style, project-structure, system-design, database-sqlserver) · 11 WARNING · 1 FAIL (security) · 12 N/A** (brownfield e os 11 overrides de pilhas que o Profile não declara: Oracle, MySQL, PostgreSQL, MongoDB, Node.js em três arquivos, PHP em três arquivos e ELK).

### 6.2 RC-N × implementação × teste (RC-1 a RC-21 do PRE_DEV_REVIEW)

**Resumo:** 19 implementados e provados; **RC-10 parcial** (código pronto, falta o valor do provedor: R-11); **RC-16 com ressalva** (só acréscimo vale no EF, não impede `ExecuteDelete` nem o banco, aceito em RR-6; a permissão negada nas páginas não é registrada, R-08). Nenhum RC ausente.

| RC | Implementação (arquivo:linha) | Teste que prova | Situação |
|---|---|---|---|
| RC-1 formato pela assinatura | `Core/Photos/PhotoSignature.cs`; `PhotoIngestion.cs` decide o formato antes de qualquer outra coisa | `FormatoTests.Assinatura_DecideOFormato_NaoAExtensao`, `ArquivoFalsoComExtensaoJpg_ERecusado_ENadaVaiParaODisco` | Implementado |
| RC-2 limites e decodificadores | `Photos/policy.xml` (`coder none *`, só 5 formatos), `MagickRuntime.cs:46-77` (memória 512 MB, 30 s, lado máximo), pixels antes de decodificar | `PhotosSecurityTests.DecodificadoresNaoUsados_EstaoDesligados`, `ImagemAcimaDoLimiteDePixels_E_Recusada_AntesDeDecodificar`, `LimitesDeRecursoDoMagick_FicamLigados` | Implementado; a prova no Windows do provedor segue pendente (AR-05, F6-02) |
| RC-3 foto reprocessada, original nunca servido | `PhotoIngestion.cs` grava só as versões WebP; `PhotoDelivery.cs` só abre versões | `MetadadosTests.Gps_NaoSobrevive_NasVersoes`, `VersoesTests` | Implementado |
| RC-4 caminho só com ids e conferido | `FileSystemPhotoStorage.cs:25-45` (regex de chave) e conferência do caminho final | `PhotosSecurityTests.ChaveForaDoFormato_ERecusada_SemTocarNoDisco`, `EntregaTests.TentativaDeSairDaPastaBase_E_Recusada` | Implementado |
| RC-5 `_originals/` sem rota e apagado em 30 dias | Nenhuma rota para originais (`PhotosController` só entrega 480/1600); `OriginalsCleanupService` | `EntregaTests.OriginaisNaoTemRota`, `CleanupTests.ApagaSoOriginaisComMaisDeTrintaDias` | Implementado (a limpeza sem trava de proporção é R-29) |
| RC-6 conversões limitadas e envios por usuário | `PhotoIngestion.cs:21` (2 vagas), `RateLimitingExtensions.cs:75` (`fotos-envio`, 30/min por usuário) | `FailureTests.DecodificacoesAoMesmoTempo_NuncaPassamDoLimite`, `PhotosEndpointsTests` (429 com `Retry-After`) | Implementado (chave de configuração afrouxável em Production: R-39) |
| RC-7 antiforgery, autoria e limite de fotos no envio | `AntiforgeryJsonFilter.cs`; `AdPhotosController` com `[Authorize(Writer)]` e autoria no serviço | `AccessMatrixTests.EveryUnsafeEndpoint_RejectsARequestWithoutTheAntiforgeryToken`, `OwnershipMatrixTests.AnotherWriter_IsDeniedEveryRouteOfAnAdThatIsNotTheirs…`, `US008S04` | Implementado |
| RC-8 foto só de anúncio publicado, 404 igual, `nosniff` | `PhotoDelivery.cs` (`isPublic`/`AdAccess.CanView`, `null` em toda recusa); `SecurityHeadersMiddleware.cs:22` | `EntregaTests.AnuncioNaoPublicado_Devolve404IgualAoInexistente_ParaQuemNaoPodeVer` | Implementado |
| RC-9 falha no meio apaga o que foi gravado | `PhotoIngestion.cs:35-52` (catch com `DeleteAsync`), `FileSystemPhotoStorage.cs:55-66` | `FailureTests.FalhaNoMeio_ApagaArquivosGravados`, `US008S06_FalhaDeGravacao_NaoDeixaArquivosNemRegistro` | Implementado |
| RC-10 IP do cliente atrás do proxy | `ForwardingExtensions.cs:22-43` (fail-closed), `Program.cs:75`, `:90` | `RateLimiterTests.IpDoCliente_VemDoCabecalhoEncaminhado_DeProxyConfiavel`, `CabecalhoEncaminhado_DeOrigemNaoConfiavel_E_Ignorado`, `SemProxiesConfigurados_…` | **Parcial:** falta o valor do provedor (SEC-01, bloqueia o lançamento; R-11; IPv6 em R-38) |
| RC-11 3 pedidos por hora por e-mail e aviso aos 80 por dia | `PasswordRecoveryService.cs:31-37`, `:59-77` (também 10 por hora por IP) | `PasswordRecoveryLimitsTests.QuartoPedidoNaMesmaHora_NaoEnviaEmail_MasRespondeIgual`, `TotalDiarioChegaA80_RegistraWarning` | Implementado (efeito colateral aceitável a decidir: R-30) |
| RC-12 redefinir limpa o bloqueio | `PasswordRecoveryService.cs:117-118` | `PasswordRecoveryLimitsTests.RedefinirComSucesso_LimpaOBloqueio` | Implementado (gravações separadas: R-26) |
| RC-13 resposta igual e sem esperar o envio | `AccountController.cs:115-126`; fila em memória (`PasswordRecoveryQueue`, `PasswordRecoveryWorker`) | `PasswordRecoveryLimitsTests.ContaExistenteEInexistente_TemMesmaRespostaESemEsperarOEnvio`, `FalhaNoEnvio_NaoMudaARespostaNemDerrubaOSite_ELogaComTraceId` | Implementado (fila em memória aceita, BACKLOG:19) |
| RC-14 ViewModels sem campos de decisão | `Areas/Panel/Models/AdViewModels.cs` | `Architecture/EditViewModelsTests.FormularioDeEdicao_NaoTemCamposDeDecisao`, `EntradaDoServicoDoRascunho_NaoTemCamposDeDecisao` | Implementado |
| RC-15 termo de 100 caracteres e consulta de 10 s | `SearchLimits.MaxTermLength = 100` (`Core/Search/SearchModels.cs:20`); `CommandTimeoutSeconds = 10` (`PanelAdListReadRepository.cs:22`, `AdCardSql.cs:14`) | `SearchRulesTests.cs:96-100`; `PanelAdListQueryTests.cs:353` e `SearchQueryTests.cs:535` (trancam a tabela no SQL Server real e medem o tempo) | Implementado, com a melhor prova do conjunto |
| RC-16 toda ação sensível registrada | `AuditLog.cs`; chamadas em `UserManagement`, `CategoryManagement`, `SiteSettingsManagement.cs:46`, `AdService.cs:73`, `AdDraftService.cs:72`, `:115`, `PasswordRecoveryService.cs:126`; só acréscimo em `AppDbContext.cs:172-176`; login no log (`AccountController.cs:96`, `:229-241`) | `UserAuditTests`, `AccountTests.FalhaEBloqueioDeLogin_SaoRegistradosNoLog`, `…EntradaESaida_SaoRegistradasNoLog_SemSenha`, `SettingsTests`, `CategoriesTests` | **Implementado com ressalva** (RR-6; permissão negada nas páginas sem log: R-08; redefinição não atômica: R-26) |
| RC-17 sem `innerHTML` com texto do servidor | Nenhum `innerHTML`/`insertAdjacentHTML`/`eval` em `wwwroot/js` | `Layout/JsModulesTests.cs:27`, `RawOutputTests`, `XssInAllScreensTests` | Implementado |
| RC-18 `returnUrl` só local | `AccountController.cs:214-217` (`Url.IsLocalUrl` e não a própria entrada) | `AccountTests.ReturnUrlExterno_E_Ignorado` (`https://evil.example` e `//evil.example`) | Implementado |
| RC-19 aviso se as variáveis do Administrador continuam | `BootstrapAdminInitializer.cs:57-64` | `BootstrapAdminTests.VariaveisRemanescentes_RegistramWarning`, `SoUmaVariavelRemanescente_ComAdministrador_ApenasAvisa` | Implementado (papel sem checagem: R-25) |
| RC-20 exemplo com aviso de guarda | `web.Production.config.example:2-5`; `.gitignore:340-342` | `SecretsTests.ArquivoDeExemplo_TemAvisoDeGuardaESemValorReal`, `GitIgnore_ProtegeArquivosLocaisDeConfiguracao` | Implementado |
| RC-21 corpo de 1 MB (foto 11 MB) | `BodyLimitMiddleware.cs`; `Program.cs:117`; `[RequestSizeLimit]` no envio | `BodyLimitTests.CorpoAcimaDe1Mb_Devolve413_ExcetoNoEnvioDeFoto` | Implementado (só `Content-Length` testado; leitura antes do limite: R-32; 413 fora do contrato: R-33) |

**Mitigações STRIDE (29 ameaças).** 25 implementadas e provadas; **S3 (forja por vazamento das chaves) não implementada: R-01**; D3 (inundação distribuída) parcial por desenho (RR-4) e dependente de SEC-01 (R-11); S1 implementada pelo contador de falhas, mas a política `auth` documentada não é usada (R-07); I8 (placas e rostos) e E3 (Administrador desonesto) aceitas como residuais (RR-5, RR-6). A tabela completa está no relatório do revisor C.

### 6.3 ADR × respeitada? × evidência

| ADR | Respeitada? | Evidência |
|---|---|---|
| ADR-001 monólito modular, 3 projetos | **Sim** | `GazetaMarketplace.slnx` (Core, Infrastructure, Web); `Core.csproj` só com `Microsoft.Extensions.DependencyInjection.Abstractions`; `AddCore()` e `AddInfrastructure()` em `Program.cs:81`, `:84`; projetos do template (`ClaudeStack.*`, `Example.*`) fora de `src/` e `tests/` |
| ADR-002 campos por categoria em JSON | **Sim** | Grupos em código (`Core/Fields/Groups`, 19 arquivos), colunas calculadas com `TRY_CAST`, `ComputedColumnsDifferentialTests`. Ressalva: lista de opções de campo obrigatório não herdada por categoria nova (R-06) |
| ADR-003 Identity com cookie | **Sim** | `IdentityExtensions.cs:34-121`; `MustChangePasswordFilter` em `PanelControllerBase.cs:15`; `BootstrapAdminInitializer`. Dependência declarada em "Risks": chaves do Data Protection fora da raiz (**não ligada**, R-01) |
| ADR-004 EF para escrita, Dapper para leitura complexa | **Sim**, com desvio menor | Dapper só em 4 repositórios de leitura e 2 ferramentas; `SqlBuilder` único; `Database.Migrate()` ausente; `UPDLOCK` do envio de fotos; `rowversion`. O usuário usa `ConcurrencyStamp` (R-65) |
| ADR-005 fotos fora da raiz, WebP, original 30 dias | **Sim** | `FileSystemPhotoStorage.cs`, `MagickImageProcessor`, `OriginalsCleanupService`, `PhotosController` com limite próprio (`RateLimitingExtensions.cs:80`) |
| ADR-006 busca por texto normalizado | **Sim** | `CHARINDEX` sobre `TitleSearch`/`DescriptionSearch`, normalização só na aplicação (`Normalizer`) |
| ADR-007 CEP no servidor com cache em tabela | **Sim** | `CepController` com `[Authorize(Writer)]` e `[EnableRateLimiting("cep")]`; `ViaCepLookup` (5 s, uma tentativa); `CepCache`; só cidade, UF e IBGE (cuidados em R-03 e R-27) |
| ADR-008 catálogo importado uma vez | **Sim** | `tools/VehicleCatalogExport` fora da solução, chave composta com `Kind`, `Source`; o site nunca lê o banco do GazetaOnline |
| ADR-009 SendGrid por HttpClient | **Sim**, com desvio menor | `SendGridEmailSender.cs` sem SDK; chave só no cabeçalho; tempo limite de **15 s** (`Infrastructure/ServiceCollectionExtensions.cs:121`) contra os 10 s do ADR (R-65) |
| ADR-010 observabilidade em hospedagem compartilhada | **Sim** | Serilog em arquivo JSON, retenção de 14 dias (`SerilogConfiguration.cs`), `/health/live` e `/health/ready` sem detalhes (`Program.cs:127-128`) |
| ADR-011 configuração e segredos por WebDeploy | **Parcial** | Opções tipadas com `ValidateOnStart` em produção (`OptionsExtensions.cs:16-44`), `web.Production.config.example`, `appsettings.Development.json` fora do git. **Falta** o `PersistKeysToFileSystem` das Implementation Notes (R-01) |
| ADR-012 componentes fora da v1 | **Sim**, com premissa desatualizada | Nenhum Redis, Kafka, Hangfire, YARP, Keycloak, Polly, OpenTelemetry nem Docker em produção; a premissa "só há uma tarefa em segundo plano" já não vale (4 serviços hospedados, R-65) |

Referências e pacotes: `Directory.Build.props` com `net10.0`, `Nullable=disable`, `ImplicitUsings=disable`, `TreatWarningsAsErrors=true`, sem sobrescrita em nenhum `.csproj`; `ManagePackageVersionsCentrally=true` e **nenhum `Version=`** em `src/`, `tests/` ou `tools/`; `global.json` com SDK `10.0.100` final e runner `Microsoft.Testing.Platform` (o RR-10 já não vale: R-64); `.gitignore` protege `appsettings.Development.json`, `web.Production.config`, `*.pubxml` (`:155`, `:340-342`). Vulnerabilidades de pacote **não foram verificadas** aqui (R-69; fica para o `/scan`).

### 6.4 Controle × onde está ligado × teste (escopo Web, `Program.cs`)

| Controle | Ligado em (`Program.cs`) | Teste que o prova | Observação |
|---|---|---|---|
| Cabeçalhos de segurança e CSP | `:92` `SecurityHeadersMiddleware` | `HeadersTests.TodaResposta_TemOsCabecalhosObrigatorios` (`HeadersTests.cs:20`), `CspTests.cs:16` | CSP sem `base-uri` (R-37) |
| HSTS e HTTPS | `:61-64` opções, `:104` `UseHttpsRedirection`, HSTS em `SecurityHeadersMiddleware.cs:28` | `HeadersTests.cs:36,52,64,76` | HSTS só com `Request.IsHttps` (depende do encaminhamento: R-11) |
| Antiforgery (páginas e JSON) | `:48-60` (filtros globais, cabeçalho e cookie) | `AntiforgeryTests.cs:24,36,49` (rotas de teste), `AccessMatrixTests` nas rotas reais | Corpo da resposta de falha não testado (R-10); leitura de corpo antes do limite (R-32) |
| Rate limiting | `:67` registro, `:121` uso depois da autenticação | `RateLimiterTests.cs:66` (global), `:41` (política `auth`, rota de teste), `LoginFailureCounterTests` | **Política `auth` sem uso real** (R-07) |
| Autenticação | `:79` `AddTeamIdentity`, `:119` `UseAuthentication` | `SessionTests.cs:34,58,82,99` | Falta teste de chaves persistentes (R-01) |
| Autorização | `:122` `UseAuthorization`; políticas em `IdentityExtensions.cs:75-77` | `AccessMatrixTests`, `OwnershipMatrixTests` | Sem `FallbackPolicy` (R-42, F6-20) |
| Tratador de exceções | `:93-95` `/api` (middleware próprio), `:96-98` páginas (`/Home/Error`) | `ErrorsTests.cs:25,47,69,81` | Erros do framework fora do contrato (R-33) |
| Páginas de status | `:100-102` | `StatusPagesTests.cs` | Só GET testado (R-10) |
| Compressão | `:68` serviço, `:105` uso (painel excluído) | `CompressionAndCacheTests.cs` | — |
| Limite de corpo | `:117` `BodyLimitMiddleware`; `[RequestSizeLimit]` nos envios de foto | `BodyLimitTests.cs:19` | Só `Content-Length`; corpo em partes sem teste (R-33) |
| Cabeçalhos encaminhados | `:75` registro, `:90` `UseSecureForwarding` (primeiro) | `RateLimiterTests.cs:149,160,171` | `KnownProxies` pendente (R-11) |
| Correlação | `:91` | `CorrelationIdTests.cs`, `ErrorsTests.cs:81` | — |
| Saúde | `:65-66`, `:127-128` | `HealthTests.cs:21,34,53,66,78` | — |
| CORS (nenhum) | `:76` (comentário; nada registrado) | `CorsTests.cs:17` | — |
| Cultura fixa | `:33-36`, `:107-114` | `CultureTests.cs:15` | Fuso IANA no Windows (R-05) |
| **Data Protection (chaves)** | **Não ligado** | **Nenhum** | **R-01** |

## 7. Approval Status

| Decision | **APPROVE com condições** (rótulos do Product Owner: APPROVE · APPROVE com condições · REJECT). No vocabulário do template, **equivale a REQUEST CHANGES até a correção do R-01**: o Gate 7 **não passa** enquanto o 🔴 estiver aberto |
|----------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Condição bloqueante | **Resolver o R-01** (persistir as chaves do Data Protection, provado por teste com dois hosts e pela presença de `key-*.xml` na pasta). Correção pequena: poucas linhas em `Program.cs` e dois testes. Sem isso, nem `/scan` nem `/deploy` |
| Condições antes do `/scan` ou `/deploy` | **Registrar a decisão sobre os 17 🟡** (corrigir ou aceitar cada um, com dono). Recomendo corrigir antes do `/scan`: R-02 e R-03 (os dois que podem alterar dado em silêncio), R-07 (documentação, política e teste que dizem o que o código não faz), R-08 e R-10. Antes do `/deploy`: R-04, R-05 (confirmar no `/verify`), R-06, R-09, R-11 (SEC-01, já bloqueio de lançamento), R-14 a R-16 e R-36. R-12, R-13 e R-17/R-18 podem ir para `/simplify` e para depois do lançamento, com registro |
| Dependem do Product Owner | R-02 (regra do Publicado editado), R-12 (FluentValidation), R-30 (limite de e-mail de recuperação), R-65 (retenção de IP e e-mail), R-69 (SQLite de teste e Font Awesome) e a obtenção do IP do proxy do provedor (R-11); ver triagem §4 |
| O que NÃO bloqueia | Os 51 🟢 (nenhum é defeito de dados nem de segurança explorável; 8 já estavam na triagem) e os OPEN-001 a OPEN-007 (disposições na seção 5) |
| Fronteira deste review | Nenhum arquivo de `src/`, `tests/`, `db/` ou `tools/` foi alterado; nenhum build, teste nem navegador foi executado; nada foi commitado por este review |

**Re-pontuação esperada após a correção do R-01:** Segurança de 2 para no máximo 4; Arquitetura continua em 3 até R-07 e R-12 serem fechados; Correção continua em 3 até os 🟡 de comportamento (R-02 a R-06, R-09, R-10, R-14 a R-18) serem fechados. Quando o R-01 for corrigido, acrescentar aqui a subseção "Resolution" com (a) o que mudou, (b) os números da nova rodada de teste e (c) as notas recalculadas.

### Resolution — após o `/fix-issue` de 2026-10-06 (R-01 e os 6 avisos de P1)

> **Em resumo:** o 🔴 **R-01 foi corrigido e provado**, então o Gate 7 deixa de estar bloqueado por ele. Seis dos avisos aprovados também foram corrigidos, cada um com teste que falha sem a correção. Dois avisos (R-03 e R-06) foram investigados e **provados como defeito real**; a correção espera aprovação. O veredito continua **APPROVE com condições**, agora sem condição bloqueante de segurança.

**(a) O que mudou** (um commit por correção; ids certos do review na primeira coluna):

| Achado | O que mudou | Commit |
|---|---|---|
| R-01 🔴 | `AddDataProtection` com `XmlRepository` na pasta `DataProtection:KeysDirectory` (leitura tardia da configuração); ARCHITECTURE §7 | 3494a9e |
| R-07 | `[EnableRateLimiting("auth")]` em entrar, esqueci-minha-senha e redefinir-senha; `RateLimiting:AuthPermits` (padrão 5 por 15 min por IP) | 2a8e1a5 |
| R-02 (salvar) | `AdDraftService.UpdateAsync` aplica `AdSubmissionRules.Pending` a um Publicado; SPEC v1.8, cenário `@US-008-S15` | f12300b |
| R-04 | `PriceText` só lê `0-9` | 7b15f3d |
| R-09 | `ad-edit.js` reconhece o redirecionamento para a entrada, mostra aviso visível e não toca no formulário | b76e7f0 |
| R-08 | `Warning` "Permissão negada…" no evento do cookie e em `AccessDeniedLoggingMiddleware` | 677bd7e |
| R-10 | `[IgnoreAntiforgeryToken]` em `HomeController.Status` e `Error`; **a resposta saía mesmo em branco** | 190539f |

Quatro mensagens de commit citam o achado com número trocado (R-03, R-05, R-04, R-06 em vez de R-04, R-08, R-09, R-10); o histórico já enviado não foi reescrito e os comentários do código foram corrigidos (6410425). O host de teste da integração ganhou `RateLimiting:AuthPermits=1000` (a primeira rodada completa falhou em `UserFlowTests` por causa do limite novo).

**Investigações (sem correção, aguardando aprovação):**

- **R-03 — PROVADO.** Com um interceptor que grava o mesmo CEP entre o `SELECT` e o `INSERT` do cache, a edição de um rascunho com CEP novo responde 302 de sucesso, **não grava o título** e **grava a auditoria `ad.update`**; sem a corrida o título é gravado (controle).
- **R-06 — PROVADO** para Imóveis, Roupas, Eletro e Telefonia: uma categoria **filha** de uma categoria com grupo próprio herda o grupo, mas o campo obrigatório de lista (`propertyTypeId`, `sizeId`, `typeId`) fica com 0 opções e o envio pede "Informe o tipo…" sempre; somem também campos do pai (quartos, banheiros, vagas, capacidade, marcas compatíveis). Uma irmã (mesmo pai, sem grupo próprio) cai em Produtos em geral e não tem o problema.
- **Novo, fora do pedido:** apagar a **última foto** de um Publicado continua permitido (metade do R-02); registrado como R-02b.

**(b) Números da nova rodada** (commit `a487e20` + o ajuste do host de integração):

| Suíte | Total | Falhas | Ignorados | Observação |
|---|---|---|---|---|
| Unidade (`Web.Tests`) | 1.751 | 0 | 0 | 2 min 39 s |
| `CitiesImport.Tests` + `VehicleCatalogExport.Tests` | 27 + 43 | 0 | 0 | |
| Integração (SQL Server em Docker) | 162 | 0 | 0 | 1 min 42 s (a primeira rodada deu 1 falha, ver acima; corrigida e rodada de novo inteira) |
| E2E (site publicado) | 258 | 0 | 4 | 12 min 39 s; 258 = `--list-tests`; os 4 ignorados são o `VitalsTests` (OPEN-004, só com `GAZETA_VITALS=1`) |

**Mutações** (cada ponto de correção tirado e o teste correspondente falhou; fonte restaurada): R-01 (sem `XmlRepository`: 1 falha) · R-07 (sem `[EnableRateLimiting]`: 3) · R-02 (sem a checagem: 4) · R-04 (volta `\d`: 2) · R-09 (sem a detecção no JS: E2E falha) · R-08 (sem log no cookie: 1; sem o middleware: 2) · R-10 (sem `[IgnoreAntiforgeryToken]`: 3).

**(c) Notas recalculadas** (regra de honestidade: 🟡 aberto limita a 4):

| Eixo | Antes | Depois | Por quê |
|---|---|---|---|
| Correctness | 3 | **3** | R-03 e R-06 provados e abertos, mais R-05, R-14 a R-18 e R-02b |
| Readability | 4 | **4** | R-13 (operador `!`) segue aberto |
| Architecture | 3 | **4** | R-01 e R-07 fechados (documento e código voltaram a dizer o mesmo); resta R-12 (desvio documentado) |
| Security | 2 | **4** | sem 🔴; resta R-11 (proxy, `/infra`) |
| Performance | 4 | **4** | sem mudança |

**Veredito:** **APPROVE com condições** (sem condição bloqueante). Condições abertas: decidir a correção do R-03 e do R-06 (P1), o R-02b, e o R-11/SEC-01 antes do `/deploy`. Os 7 OPEN seguem as disposições da seção 5; a única mudança é o OPEN-004, cuja variável `GAZETA_VITALS` agora está no roteiro (`docs/RODAR-TESTES-DE-INTEGRACAO-E-E2E.md`), e a correção de "44 telas" para 45 em `plans/plan.md`.

### Resolution (2.ª rodada) — R-03, R-06 e R-02b, aprovados pelo Product Owner em 2026-10-07

> **Em resumo:** os três itens que estavam provados e aguardando decisão foram corrigidos, cada um com teste que falha sem a correção. Não sobra nenhum 🟡 de comportamento aberto além do R-05 (fuso no Windows, a confirmar na hospedagem) e dos de tela que vão para o `/simplify`. O veredito é **APPROVE, sem condição bloqueante**.

**(a) O que mudou**

| Achado | O que mudou | Commit |
|---|---|---|
| R-03 | `CepService` desanexa só a entrada do cache (`Entry(entry).State = Detached`) em vez de `ChangeTracker.Clear()`; um interceptor de teste grava o mesmo CEP entre o `SELECT` e o `INSERT` e a edição do rascunho grava o título, uma linha de auditoria e uma de cache | 523978b |
| R-06 | `FieldGroupRegistry.Resolve` devolve, para a categoria sem dados próprios, o grupo com as **listas, os campos e os obrigatórios** do ancestral mais próximo que os tem (`FieldGroup.ForCategory`, cópia em cache; as chamadas existentes não mudam). SPEC v1.9, A7 (a). Vale para Imóveis, Roupas, Eletro, Telefonia e para qualquer grupo com dados por categoria | 3867cd7 |
| R-02b | `AdPhotoService.DeleteAsync` responde 409 ("Não é possível remover a última foto de um anúncio publicado. Despublique antes.") quando a remoção deixaria um Publicado abaixo do mínimo do grupo, conferido dentro da transação. SPEC v1.9, cenário `@US-008-S16`. Vale na API e na página sem JavaScript | f4bc8bd |

Observação sobre o R-06: o pedido falava em buscar só a lista no ancestral. Implementei os três dados por categoria (lista, campos que existem, obrigatórios) porque, com só a lista, a filha de Imóveis continuaria sem quartos, banheiros e vagas e a de Terrenos sem a área obrigatória; os dois testes novos travam os três.

**(b) Números da nova rodada** (commit `c92cb7b`):

| Suíte | Total | Falhas | Ignorados | Observação |
|---|---|---|---|---|
| Unidade (`Web.Tests`) | 1.765 | 0 | 0 | 2 min 29 s; = `--list-tests` |
| `CitiesImport.Tests` + `VehicleCatalogExport.Tests` | 27 + 43 | 0 | 0 | |
| Integração (SQL Server em Docker) | 162 | 0 | 0 | 1 min 52 s |
| E2E (site publicado) | 258 | 0 | 4 | 13 min 00 s; = `--list-tests`; os 4 ignorados são o `VitalsTests` (OPEN-004) |

**Mutações desta rodada** (fonte restaurada depois de cada uma): R-03 (volta `ChangeTracker.Clear()`: 1 falha) · R-06 (`Resolve` sem herança: 6 falhas) · R-02b (sem a recusa: 3 falhas). Somadas às 8 da rodada anterior, são **11 mutações detectadas**.

**(c) Notas recalculadas** (🟡 aberto limita a 4):

| Eixo | Antes | Depois | Por quê |
|---|---|---|---|
| Correctness | 3 | **4** | R-02, R-03, R-04, R-06, R-09, R-10 e R-02b fechados; restam R-05 (fuso, confirmar na hospedagem) e R-14 a R-18 (tela, `/simplify`) |
| Readability | 4 | **4** | R-13 (operador `!`) segue aberto |
| Architecture | 4 | **4** | R-12 (FluentValidation) é desvio documentado |
| Security | 4 | **4** | R-11/SEC-01 (proxy) segue para `/infra`; a rodada anterior já tinha fechado o 🔴 |
| Performance | 4 | **4** | sem mudança |

**Veredito: APPROVE, sem condição bloqueante.** Itens que seguem abertos, todos com dono: R-11/SEC-01 e as chaves sem criptografia (`/infra`, com o R-05), R-12 (desvio documentado), R-13 e R-14/R-15/R-18 (`/simplify`) e os 🟢. Os 7 OPEN seguem as disposições da seção 5.

### Addendum — `/verify` (2026-10-07)

O `/verify` do artefato `6bb592c8…83ec` (commit `6953fa3`) **não achou defeito novo de segurança nem de dados** e confirmou na prática duas correções do review: o limite `auth` (R-07: 5 pedidos passam, o 6.º recebe 429) e o aviso de erro no lugar da página branca (R-10). Trouxe três observações de P2 (V-01 a V-03 no BACKLOG: balde de limite compartilhado entre entrar, esqueci e redefinir; 429 em texto simples; `/Home/Error` abrindo direto) e a dispensa de rastreabilidade (V-04, agora P2: 43 de 130 cenários dispensados pelo Product Owner com prova em processo; 87 provados no navegador). O V-01 passou a **P0 do `/infra`** por decisão do Product Owner. As notas dos cinco eixos **não mudam**; o veredito do review continua **APPROVE sem condição bloqueante**, e as condições de promoção passam a ser as do `reports/VERIFY_REPORT.md` §5.
