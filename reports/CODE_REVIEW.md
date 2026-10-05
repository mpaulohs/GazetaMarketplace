# Code Review — Fase 4: revisão e retirada de anúncios (tarefas 4.1 a 4.4)

**Data**: 2026-10-05
**Revisor**: agente Code Reviewer (Five-Axis Framework)
**Escopo**: `git diff 86c1ed8..HEAD` na branch `claude/admiring-cray-wrjfmg` (commits 9ae2205, 1d80a1f, 62e312f e 164a4d3, mais 7c19033 só de documentação), 70 arquivos, +5.642 / -177 linhas.
**Inputs**: specs/SPEC.md (US-010, US-011, US-012 e Apêndice A) · architecture/adr (ADR-004, ADR-005 e ARCHITECTURE.md) · plans/plan.md (blocos 4.1 a 4.4 e Checkpoint 4) · plans/todo.md · plans/BACKLOG.md · security/PRE_DEV_REVIEW.md e THREAT_MODEL.md · reports/TEST_REPORT.md (seções 4.1 a 4.4)
**Método**: leitura do código e dos testes. Conforme a orientação recebida, **não** foram executados `dotnet build` nem `dotnet test`; o veredito do Gate 6 vem de `reports/TEST_REPORT.md` (aprovado nas quatro tarefas).

## 1. Executive Summary

**Veredito: APPROVE com condições.** A Fase 4 entrega o que a SPEC pede, com a autorização feita no servidor em duas camadas, a decisão simultânea tratada com `RowVersion` e auditoria única, a leitura Dapper sem concatenação de SQL e uma bateria de testes forte (mutações derrubadas e prova no SQL Server real). Não há defeito que cause dano (nenhum 🔴). Há **6 avisos 🟡**, todos baratos de corrigir; antes do `/scan` o orquestrador deve corrigi-los ou aceitá-los explicitamente (Gate 7). Os mais relevantes: a página `pagina=` muito grande derruba a lista em 503 (estouro de inteiro), os ramos de repetição por conflito de `RowVersion` não têm teste determinístico, o contrato "409" ainda consta no plano e nas ADRs enquanto o código devolve 302 com mensagem, e a cobertura numérica nunca foi medida.

🔴 0 · 🟡 6 · 🟢 20 · ✅ 10

## 2. Five-Axis Scores

| # | Axis | Score (1–5) | One-line justification |
|---|------|-------------|------------------------|
| 1 | Correctness | 4 | Todos os cenários da fase têm caminho ligado e teste com efeito observável; sobram os avisos 🟡 1 (estouro do deslocamento em `pagina=`), 2 (repetição por conflito sem teste determinístico) e 3 (contrato 409 desatualizado nos documentos). |
| 2 | Readability | 4 | Nomes, comentários de motivo e mensagens centralizadas em `AdMessages` são claros; o aviso 🟡 5 (rótulos de texto como chave) e o 🟡 6 (`catch` vazio), mais duplicações 🟢 (10, 12, 13, 16), impedem o 5. |
| 3 | Architecture | 4 | Camadas respeitadas (interfaces no Core, Dapper e EF na Infrastructure) e uma única tabela de passagens; o aviso 🟡 5 acopla a tela ao texto dos rótulos e os 🟢 8, 9 e 11 apontam controllers grandes e regra repetida em três pontos. |
| 4 | Security | 4 | NFR-13 verificado nas duas camadas (política por ação e `EnsureAdministrator`), antiforgery, XSS e foto só para Publicado com teste; sem 🟡 de segurança, mas ficam os 🟢 7 (exceções sem tratamento na tela) e 20 (cache público de 1 ano da foto despublicada). |
| 5 | Performance | 4 | Dapper com parâmetros, tempo limite de 10 s (RC-15), índice do autor provado por plano de execução e sem N+1; ficam os 🟢 18, 19 e 25 (volume: fila sem paginação, busca sem índice, ordenação por COALESCE já no BACKLOG). |

## 3. Findings (by severity: 🔴 → 🟡 → 🟢 → ✅)

### 🔴 Critical

Nenhum.

### 🟡 Warning

1. **`src/GazetaMarketplace.Infrastructure/Data/SqlBuilder.cs:100` (com `Core/Ads/PanelAdListService.cs:26` e `Infrastructure/Ads/PanelAdListReadRepository.cs:38`) — `pagina=` muito grande derruba a lista em 503.** `Math.Max(page, 1)` só barra valores menores que 1; `(page - 1) * size` é calculado em `int`, sem verificação de estouro. Com `?pagina=214748365` o produto vale -16 (e com `pagina=2147483647` vale -40), o SQL Server recusa `OFFSET` negativo, a exceção cai no `catch` genérico de `AdsController.Index` (linhas 51-63) e a pessoa vê "Não foi possível carregar os anúncios" com um `LogError` por requisição. Qualquer membro da equipe provoca isso digitando a URL. O teste `Paginacao_PaginaForaDoIntervaloOuInvalida…` (`tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListTests.cs:242-252`) cobre 0, -5, abc e 99, mas não um inteiro grande. — **Recomendação:** limitar a página no serviço (por exemplo `Math.Clamp(page, 1, 100000)`) ou calcular o deslocamento em `long` e tratar o excesso como "além do fim"; acrescentar o caso `pagina=2147483647` ao teste. — *Relates-to: US-012-S07, US-012-S08, RC-15, Task 4.4*

2. **`src/GazetaMarketplace.Infrastructure/Ads/AdReview.cs:66-81` e `Infrastructure/Ads/AdTakedown.cs:53-67` — os ramos de repetição por conflito de `RowVersion` não têm teste determinístico.** Os testes de "dois administradores" (`tests/GazetaMarketplace.Web.Tests/Review/ReviewTests.cs:243-287`, `TakedownTests.cs:316`) executam os pedidos em sequência, então a segunda decisão para na conferência prévia (`PublishChecksAsync` ou `Check`) e nunca chega ao `catch (ConflictException)`. A única prova do `catch` e do `ChangeTracker.Clear()` está nas integrações (`ReviewDecisionConcurrencyTests.cs:79`, `TakedownConcurrencyTests.cs:69`), que disparam os pedidos com `Task.Run` sem barreira que force a corrida entre a leitura e a gravação; o relatório de mutação (TEST_REPORT 4.2 e 4.3) não inclui "remover a repetição" nem "remover o token". Remover o laço de repetição pode continuar passando em quase todas as rodadas. Mesmo cenário de "decisão sobre a versão corrigida" (anúncio editado no intervalo, SPEC US-010) não é provado. — **Recomendação:** um teste unitário com um `IAdService` falso que lança `ConflictException` na primeira chamada (e outro que lança nas duas) para provar: segunda tentativa decide; segunda falha com anúncio ainda Em revisão relança; segunda falha com anúncio já decidido devolve `AlreadyDecided`. Acrescentar a mutação "sem repetição" ao roteiro. — *Relates-to: US-010-S07, US-011-S01, US-011-S02, ADR-004, Task 4.2, Task 4.3*

3. **`plans/plan.md:1616`, `architecture/adr/ADR-004-persistencia-efcore-sqlserver.md:70`, `architecture/ARCHITECTURE.md:170` e `:257` — o contrato de decisão simultânea ainda diz "409", mas o código devolve 302 com a mensagem da SPEC.** O desvio é correto para o produto (páginas HTML; a SPEC US-010-S07 pede a mensagem "já foi publicado por outro administrador") e está descrito apenas de passagem em `reports/TEST_REPORT.md:745`; nenhum item do BACKLOG nem a ADR o registram. O critério do Checkpoint 4 ("Decisão simultânea devolve 409") falharia se alguém o verificasse literalmente, e o `/verify` herda esse critério. — **Recomendação:** registrar o desvio (302 + mensagem, sem 409, nas páginas; 409 só para `/api`) no BACKLOG com a decisão do Product Owner e ajustar o texto do Checkpoint 4, da ADR-004 e da ARCHITECTURE. — *Relates-to: US-010-S07, ADR-004, Task 4.2, Task 4.3*

4. **`reports/TEST_REPORT.md:34` — a cobertura numérica (linha ≥ 80% e ramo ≥ 75%) nunca foi medida e foi adiada "para o /review".** O `/review` não mede cobertura (e esta revisão foi instruída a não executar testes). O Gate 6 de `rules/testing.md` exige os dois números, a lista de métodos com 0% justificados e, em greenfield, a medida do repositório inteiro. As quatro seções 4.x não trazem nenhum número, só mutações. As mutações dão boa confiança nos caminhos de decisão, mas não substituem a medida. — **Recomendação:** rodar uma vez `dotnet run --project tests/GazetaMarketplace.Web.Tests -- --coverage` (e a integração), registrar em `TEST_REPORT.md` os números e os métodos de regra de negócio com 0%, antes do `/scan`. — *Relates-to: Task 4.1, Task 4.2, Task 4.3, Task 4.4*

5. **`src/GazetaMarketplace.Web/Areas/Panel/Controllers/ReviewQueueController.cs:118`, `:124` e `:128` — a tela usa o texto do rótulo como chave e reanalisa texto formatado.** `characteristics.Where(s => s.Label == "Área")`, `FirstOrDefault(s => s.Label == "Tipo")` e `s.Value.Split(", ")` dependem do rótulo escrito em `Fields/Groups/JobsGroup.cs:22` e do separador usado por `AdSpecs.ValueOf` (`Core/Ads/AdSpecs.cs:53-54`). Renomear o rótulo ("Área" para "Áreas") some silenciosamente com o bloco "Vaga de emprego"; uma opção com vírgula no nome (hoje nenhuma, mas as listas de `FieldLists` aceitam) seria partida em duas. A 5.2 vai copiar esta lógica para a página pública. — **Recomendação:** dar uma `Key` ao `AdSpec` (o campo já a tem em `FieldDefinition`) e filtrar pela chave `jobAreaIds`/tipo de serviço; expor as áreas como lista vinda de `AdSpecs`, sem reanalisar texto. — *Relates-to: US-010-S02, ADR-002, Task 4.1*

6. **`src/GazetaMarketplace.Infrastructure/Ads/AdSpecsReader.cs:57-59` — `catch (NotFoundException)` vazio.** `rules/error-handling.md` ("Never swallow exceptions silently") e o checklist do `/review` ("No swallowed exceptions") proíbem engolir a exceção sem log. O comentário da linha 33 explica a intenção (nome desconhecido não aparece), mas uma carga trocada do catálogo ficaria invisível: nenhuma linha de log, e a primeira falha também cancela a busca de modelo e versão que viriam depois. — **Recomendação:** registrar `LogWarning` com o id do anúncio e o nível do catálogo (sem dado pessoal) ou tratar o `NotFoundException` por nível (marca, modelo, versão) em vez de um `try` único. — *Relates-to: US-010-S02, Task 4.1*

### 🟢 Suggestion

7. **`src/GazetaMarketplace.Web/Areas/Panel/Controllers/ReviewQueueController.cs:154-172` e `:194-219`, `AdsController.cs:256-276` — `ConflictException` e `ForbiddenException` não são tratadas nas ações de decisão.** O segundo fracasso consecutivo de `RowVersion` com o anúncio ainda Em revisão relança `ConflictException` (`AdReview.cs:80`, `AdTakedown.cs:66`), que chega a `UseExceptionHandler("/Home/Error")` (`Program.cs:87`) como página 500 genérica; a confirmação de envio da 3.7 já trata o mesmo caso com 409 amigável (`AdsController.cs:201-205`). A chance é mínima (três escritores na mesma janela) e a política já barra o Redator. — **Recomendação:** copiar o `catch (ConflictException)` com a view de 409 já usada em `Submit`. — *Relates-to: US-010-S07, NFR-13, Task 4.2, Task 4.3*

8. **`src/GazetaMarketplace.Web/Areas/Panel/Controllers/AdsController.cs:27` (452 linhas, 8 dependências) — o controller agrega lista, formulário de rascunho, envio à revisão e retirada.** Cresceu 121 linhas na fase. — **Recomendação:** mover despublicar e arquivar (e depois a lista) para controllers próprios (`AdTakedownController`, `AdListController`) mantendo as rotas; os testes por URL continuam valendo. — *Relates-to: ADR-001, Task 4.3, Task 4.4*

9. **`src/GazetaMarketplace.Web/Areas/Panel/Controllers/ReviewQueueController.cs:72-132` — `Preview` tem 61 linhas e monta o ViewModel inteiro no controller** (categoria, grupo de campos, características, galeria, TempData, telefone). O projeto já tem o padrão `AdFormFactory`. — **Recomendação:** extrair um `ReviewPreviewFactory` (a 5.2 reaproveitará a montagem do corpo e das características); o controller fica com o TempData e o retorno. — *Relates-to: US-010-S02, ADR-001, Task 4.1*

10. **`src/GazetaMarketplace.Infrastructure/Ads/AdReview.cs:97-107` repete `AdSubmission.cs:80-90`** (resolver o grupo de campos pela árvore, contar fotos e chamar `AdSubmissionRules.Pending`). Se uma das cópias mudar (por exemplo, nova regra de foto), a publicação e o envio passam a exigir coisas diferentes. A regra em si é única (`AdSubmissionRules`); só o carregamento está duplicado. — **Recomendação:** extrair `IAdPendingReader` (ou um método público único em `AdSubmission`) usado pelos dois. — *Relates-to: US-009, US-010-S03, Task 4.2*

11. **`src/GazetaMarketplace.Core/Ads/TakedownActions.cs:10-16` e `Infrastructure/Ads/AdTakedown.cs:72-77` — a regra "o que pode ser retirado" existe em três formas** (tabela `AdStatusRules`, `TakedownActions.Allows` e `AdTakedown.Check`). O parâmetro `requiredFrom` de `Allows` é redundante, já que a única passagem que chega a Rascunho parte de Publicado (`AdStatusRules.cs:22`); o comentário da linha 12 também se contradiz. Não há teste que varra `AdStatusRules.All` contra as duas derivações (a tela e o servidor); hoje só o HTTP prova a coerência. — **Recomendação:** derivar `Check` e `Allows` apenas de `AdStatusRules.Find`, e acrescentar um teste que, para cada situação e cada papel, compara "a barra mostra a ação" com "o servidor aceita a ação". — *Relates-to: US-011-S01, US-011-S06, US-011-S07, ADR-001, Task 4.3*

12. **`src/GazetaMarketplace.Web/wwwroot/css/pages/ads-index.css:4-39` é cópia literal de `review-queue.css:4-39`** (tabela vira cartões em 320 px); `review-queue.css:42-66` ainda guarda `.ad-fotos__*` e `.ad-vaga`, que a página pública (5.2) vai usar. O BACKLOG (linha 157) adia a extração para "uma terceira lista", mas já há duas cópias idênticas e um estilo de componente preso a uma página. — **Recomendação:** criar `css/components/tabela-cartoes.css` e `css/components/ad-fotos.css` e carregá-los pelas duas telas. — *Relates-to: US-010-S01, US-012-S07, Task 4.1, Task 4.4*

13. **`tests/GazetaMarketplace.Web.Tests.Playwright/Ads/ReviewQueueE2ETests.cs:43`, `PanelAdListE2ETests.cs:44`, `TakedownE2ETests.cs:44` e `ReviewDecisionE2ETests.cs:44` — `SubmitAdAsync` copiado igual em 4 arquivos** (mais o auxiliar `Describe`). O BACKLOG (linha 159) diz "extrair quando houver mais uma cópia", mas as quatro já existem, e a correção de instabilidade da 4.4 (esperar a rede após trocar a categoria) teve de ser aplicada em todas. — **Recomendação:** mover para uma classe base ou auxiliar comum em `tests/.../Ads/`. — *Relates-to: Task 4.1, Task 4.2, Task 4.3, Task 4.4*

14. **`src/GazetaMarketplace.Web/Areas/Panel/Views/Ads/Index.cshtml:53` e `tests/GazetaMarketplace.Web.Tests/Review/ReviewTests.cs:119` — operador de supressão de nulo (`!`).** `CLAUDE.md` e `rules/code-style.md` mandam não usar `!` (Nullable desativado). Em `Index.cshtml` ele é supérfluo (`ParseStatus(slug).Value`; a lista de slugs vem de `AdStatus.All`) e chama `ParseStatus` uma vez por opção. — **Recomendação:** remover os dois; no teste, `Assert.IsNotNull` antes de ler `CacheControl`. — *Relates-to: Task 4.2, Task 4.4*

15. **`src/GazetaMarketplace.Web/Areas/Panel/Views/ReviewQueue/Reject.cshtml:17` e `:23` — o limite "500" está escrito à mão** (atributo `maxlength` e o texto "Até 500 caracteres"), enquanto a regra vive em `Core/Ads/Ad.cs:25` (`RejectionReasonMaxLength`). Mudar a constante deixa a tela errada. — **Recomendação:** passar o limite pelo `RejectViewModel`. — *Relates-to: US-010-S05, Task 4.2*

16. **`Areas/Panel/Views/ReviewQueue/ConfirmPublish.cshtml:12-20`, `Views/Ads/ConfirmUnpublish.cshtml:10-19`, `ConfirmArchive.cshtml:10-18` e `ConfirmSubmit.cshtml` repetem o mesmo cartão de confirmação;** `Views/Shared/_TakedownActions.cshtml:8` e `:12` recebem o id do anúncio pelo `ViewData["AdId"]` em vez de um modelo tipado. — **Recomendação:** um parcial `_ConfirmCard` e um modelo que leve `Id` e `Takedown` juntos. — *Relates-to: US-010-S03, US-011-S01, US-011-S02, Task 4.3*

17. **Comentários e organização de arquivos.** `AdsController.cs:23` mistura crases de Markdown e começa em minúscula; `AdsController.cs:29` ainda diz que "Meus anúncios" é provisória (a lista real já existe); `ReviewQueueController.cs:110` termina em vírgula solta ("…só do Administrador,"); `Core/Ads/IReviewQueue.cs:26` abriga `IAdSpecsReader`, interface sem relação com a fila (`lang-dotnet.md` pede um tipo público por arquivo). — **Recomendação:** corrigir os textos e mover `IAdSpecsReader` para `IAdSpecsReader.cs`. — *Relates-to: Task 4.1, Task 4.4*

18. **Pequenos excessos na consulta da lista.** `Core/Ads/IPanelAdList.cs:21` leva `AuthorId` em `PanelAdListRow`, e o serviço nunca o usa (`PanelAdListService.cs:40-43`); com um filtro de situação diferente de Arquivado, `PanelAdListReadRepository.cs:59-67` soma `a.Status = @Status AND a.Status <> @ArchivedStatus`, predicado redundante. Inofensivo, mas é ruído numa consulta que o plano de execução já cobre. — **Recomendação:** retirar `AuthorId` do registro e aplicar `NotArchived` só quando não há filtro de situação. — *Relates-to: US-012-S03, ADR-004, Task 4.4*

19. **`src/GazetaMarketplace.Infrastructure/Ads/PanelAdListReadRepository.cs:95` — a data lida pelo Dapper chega com `Kind` não especificado.** O EF converte para UTC (`AppDbContext.cs:95-96`), o Dapper não; hoje tudo funciona porque `CurrencyAndDateFormatter.ToSaoPaulo` trata "sem Kind" como UTC (`Core/Formatting/CurrencyAndDateFormatter.cs:36-37`) e `Index.cshtml:108` escreve o literal `Z` no `datetime`. Um consumidor futuro das leituras Dapper pode interpretar como hora local. — **Recomendação:** registrar um `TypeHandler` de `DateTime` que aplique `DateTimeKind.Utc` em `DapperConfiguration`, ou usar `SpecifyKind` ao montar `PanelAdListRow`. — *Relates-to: NFR-20, ADR-004, Task 4.4*

20. **`src/GazetaMarketplace.Web/Controllers/PhotosController.cs:22` e `:41` (com `Infrastructure/Photos/PhotoDelivery.cs:31-38`) — a foto de um anúncio despublicado ou arquivado continua em cache público por até 1 ano em quem já a baixou.** O servidor recusa corretamente (404 para o visitante, provado em `US011S01`/`US011S02`), mas as fotos publicadas saem com `public, max-age=31536000, immutable` (ADR-005, linha 49). A SPEC US-011 diz que o anúncio "sai do site na hora"; o ADR-005 não menciona a retirada. Não vaza nada a quem nunca viu a foto. — **Recomendação:** registrar a limitação no ADR-005 e no BACKLOG (o parâmetro de versão `?v=` já previsto no BACKLOG não resolve a retirada; um prazo menor para publicados resolveria, ao custo de mais requisições). — *Relates-to: US-011-S01, US-011-S02, ADR-005, Task 4.3*

21. **`src/GazetaMarketplace.Web/wwwroot/css/pages/ads-index.css:13-20` e `:35-38` (e `review-queue.css:52-59`, `:74-77`) — `display: block` em `table`, `tr`, `td` e `th` remove a semântica de tabela de alguns leitores de tela,** e o rótulo vem de `content: attr(data-label)`. O axe não acusa isso (a prova é de violações automáticas), então o risco real só se vê com NVDA. — **Recomendação:** conferir com NVDA (já exigido por `rules/frontend.md`) e, se necessário, acrescentar `role="table"`, `role="row"`, `role="cell"`/`columnheader` nas linhas empilhadas ou trocar por lista de cartões abaixo de 768 px. — *Relates-to: NFR-14, US-012-S07, Task 4.4*

22. **Nomes de CSS e JavaScript em português, contra `rules/overrides/lang-dotnet.md`** (identificadores de código em inglês): `ads-lista`, `ad-fotos`, `ad-vaga`, `alvo-toque`, `data-foco-inicial` e, em `wwwroot/js/pages/ad-confirm.js:3-8`, as variáveis `formulario`, `botao`, `rotulo`, `evento`; na mesma fase `review-queue` já está em inglês. A maior parte vem de fases anteriores; o diff acrescentou `ads-lista` e `ad-fotos`. — **Recomendação:** decidir a regra para CSS/JS (o glossário só cobre C#, banco e rotas) e registrar em `local/CLAUDE.local.md`; aplicar nos nomes novos. — *Relates-to: Task 4.1, Task 4.4*

23. **`src/GazetaMarketplace.Web/wwwroot/css/components/bootstrap-tema.css:4-23` — o contraste de `btn-outline-danger` e `btn-outline-secondary` foi trocado para o tema inteiro** (valores fixos `#b02a37` e `#4a5568`). A correção é legítima e está no BACKLOG (linhas 143 e 156), mas muda telas antigas (Desativar, Remover foto, Excluir categoria, Cancelar fora de cartão). O relatório não diz que o axe foi rodado de novo nelas. — **Recomendação:** rodar o axe nas telas antigas afetadas na próxima rodada de `/test` e mover as duas cores para variáveis `--app-*` em `base.css`. — *Relates-to: NFR-14, Task 4.2, Task 4.4*

24. **`plans/BACKLOG.md:132` e `:150` — registro desorganizado.** A linha 132 contém dois itens colados (o da 3.3b e o "4.1: fila sem paginação" começam na mesma linha, sem quebra); a linha 150 (teste instável `CliqueDuploEmParalelo`) continua aberta, mas a 4.4 já removeu o teste (TEST_REPORT, achado 5 da 4.4). — **Recomendação:** separar a linha 132 e marcar a 150 como resolvida. — *Relates-to: Task 3.7, Task 4.1, Task 4.4*

25. **Riscos de volume já conhecidos, sem gatilho definido.** `Infrastructure/Ads/ReviewQueue.cs:20-27` carrega e renderiza toda a fila sem `Take` (BACKLOG:132, decisão D4 aprovada); `PanelAdListReadRepository.cs:24` e `:37` ordenam a lista do Administrador por `COALESCE(UpdatedAt, CreatedAt)` (não usa índice) e `:72` busca por `CHARINDEX` (varredura); BACKLOG:153-154. Hoje o volume é pequeno. — **Recomendação:** definir um gatilho numérico (por exemplo, fila acima de 300 anúncios ou lista acima de 5.000) e uma medição no `/verify`. — *Relates-to: NFR-04, ADR-004, Task 4.1, Task 4.4*

26. **Estado do repositório: `tests/GazetaMarketplace.IntegrationTests/Checkpoint4LifecycleTests.cs` está não rastreado** (fora do diff 86c1ed8..HEAD, não revisado aqui). `rules/git-workflow.md` pede que cada comando declare o que deixou sem commit; um arquivo solto entre fluxos pode ser engolido pelo commit seguinte. — **Recomendação:** commitar o arquivo sozinho, com título próprio (`test(checkpoint): …`), antes do próximo comando. — *Relates-to: Task 4.4, Checkpoint 4*

### ✅ Good

- **`src/GazetaMarketplace.Web/Areas/Panel/Controllers/ReviewQueueController.cs:28` e `AdsController.cs:212-227`; `Infrastructure/Ads/AdReview.cs:122-128`, `AdTakedown.cs:79-85`** — autorização em duas camadas: política Administrator no controller e `EnsureAdministrator` no serviço, mais `AdAccess` no `AdService.TransitionAsync` (`AdService.cs:57-68`). Uma tela esquecida não abre brecha (NFR-13); os testes `US010S09`, `US011S07` e as mutações M4/M7/T3/T5 provam.
- **`Infrastructure/Ads/AdService.cs:53-77`** — passagem, trilha e auditoria no mesmo `SaveChanges`, com a tabela única `AdStatusRules`; o motivo da rejeição vai ao valor novo da auditoria (`:80-81`), cobrindo RC-16 sem tabela nova.
- **`Infrastructure/Ads/AdReview.cs:46-83`** — cada tentativa relê o anúncio, limpa o `ChangeTracker` e reconfere, de modo que a decisão vale sobre a versão corrigida (SPEC US-010) e só uma auditoria é gravada.
- **`Infrastructure/Ads/PanelAdListReadRepository.cs:24,37,72,78-79` e `Data/SqlBuilder.cs:157-174`** — SQL só por fragmentos fixos, todo valor como parâmetro nomeado, ordem sem entrada do usuário, `CHARINDEX` sem curinga e `commandTimeout` de 10 s com `CancellationToken` (RC-15); `SqlFragments.cs:18-26` mantém o filtro de situação num lugar só, guardado por teste.
- **`Core/Ads/PanelAdListService.cs:25`** — a autoria do Redator vem da sessão (`ICurrentUser`), nunca da URL; o teste `ParametrosForaDaLista_AutorEOrdenacao_SaoIgnorados` (`PanelAdListTests.cs:308`) prova.
- **`Infrastructure/Photos/PhotoDelivery.cs:31-38`** — a foto de anúncio não publicado só é entregue à equipe com acesso, com resposta 404 idêntica; as mutações T8 e a prova no navegador (200 antes, 404 depois de arquivar) cobrem o vazamento pela pré-visualização.
- **`Views/Shared/_ErrorState.cshtml:11`** — "Tentar novamente" só renderiza se `Url.IsLocalUrl`, o que neutraliza o `ActionUrl` montado com `Request.Path + Request.QueryString` (`AdsController.cs:60`); sem script inline nem `style` nas telas novas (CSP).
- **`wwwroot/js/pages/ad-confirm.js:3-23`** — trava o botão no primeiro clique (clique duplo não duplica o pedido), restaura no `pageshow` e foca o campo com erro; as páginas de confirmação funcionam sem JavaScript.
- **Testes** — nomes com prefixo do cenário (`US010S03_…`), asserções sobre estado persistido, fila e auditoria (por exemplo `ReviewTests.cs:59-80`), integração no SQL Server real com plano de execução do índice do autor (`PanelAdListQueryTests.cs:217`) e 45 mutações aplicadas (10 na 4.1, 9 na 4.2, 11 na 4.3, 15 na 4.4): 44 derrubadas e uma sobrevivente, o desempate por id, que é equivalente e está justificada.
- **Rastreabilidade das decisões** — os desvios aprovados (CHARINDEX no lugar de LIKE, páginas de confirmação, ordem fixa, cobertura parcial de S01/S02/S04/S05 da US-011 e S04 da US-010) estão registrados em `plans/BACKLOG.md` (linhas 138, 139, 144, 145, 153) e nas seções 4.x do `TEST_REPORT.md`.

## 4. Action Items

- [ ] **P0**: nenhum.
- [ ] **P1**: corrigir o estouro de `pagina=` e cobrir o caso grande (aviso 1).
- [ ] **P1**: acrescentar teste determinístico dos ramos de repetição de `AdReview` e `AdTakedown` e a mutação "sem repetição" (aviso 2).
- [ ] **P1**: registrar o desvio "302 + mensagem, não 409" no BACKLOG e ajustar Checkpoint 4, ADR-004 e ARCHITECTURE (aviso 3; decisão do Product Owner).
- [ ] **P1**: medir cobertura numérica e a lista de métodos com 0% e gravar no `TEST_REPORT.md` antes do `/scan` (aviso 4).
- [ ] **P1**: dar `Key` ao `AdSpec` e remover os filtros por texto de rótulo e o `Split(", ")` (aviso 5).
- [ ] **P1**: registrar log no `catch` de `AdSpecsReader` (aviso 6).
- [ ] **P2**: itens 7 a 26 (extrações de controller, CSS e auxiliar E2E; remoção dos `!`; comentários; cache da foto despublicada; NVDA nas tabelas empilhadas; arquivo não rastreado; BACKLOG).

## 5. Test Coverage

**Gate 6** (de `reports/TEST_REPORT.md`): aprovado nas quatro tarefas. Números da última rodada (tarefa 4.4, linhas 808 a 860): 1.287 testes unitários do site, 43 da ferramenta de catálogo, 27 da ferramenta de municípios, 117 de integração (SQL Server 2022 em contêiner) e 88 de navegador (Playwright), todos passando. Evolução por tarefa: 4.1 (1.234 / 103 / 74), 4.2 (1.253 / 105 / 79), 4.3 (1.273 / 108 / 83), 4.4 (1.287 / 117 / 88).

**Mutação:** 45 mutações aplicadas na fase, 44 derrubadas e 1 equivalente justificada (desempate por id da fila). Não consta mutação para os ramos de repetição por conflito (ver aviso 2).

**Cobertura numérica:** não medida (ver aviso 4); o relatório (linha 34) a adiou para o `/review`, que não a mede.

**Cenários da SPEC (cada um com caminho ligado ao app e teste que afirma o efeito observável):**

| Cenário | Caminho ligado | Teste (efeito observável) |
|---|---|---|
| US-010-S01 | `ReviewQueueController.Index` → `ReviewQueue` | `ReviewQueueTests.cs:35` (ordem, colunas, total, fora Rascunho/Publicado/Rejeitado/Arquivado); `ReviewQueueSqlTests.cs:53` (SQL real) |
| US-010-S02 | `Preview` + `_AdBody`/`_AdPhotos` | `ReviewQueueTests.cs:173` (faixa, corpo, fotos, contato, três botões); E2E `ReviewQueueE2ETests.cs:71` |
| US-010-S03 | `ConfirmPublish`/`Publish` → `AdReview.PublishAsync` | `ReviewTests.cs:59` (situação, `PublishedBy/At`, sai da fila); `:104` (foto passa a chegar); E2E `ReviewDecisionE2ETests.cs:81`. A frase "home e busca" é da Fase 5 (BACKLOG:138) |
| US-010-S04 | `Reject` POST → `AdReview.RejectAsync` | `ReviewTests.cs:123` (motivo na edição do autor); E2E `:119`. "Mais recentes" e "busca" são da Fase 5 |
| US-010-S05 | idem, `ValidationException` → página com erro | `ReviewTests.cs:189` (3 motivos vazios); `:205` (500 e 501); E2E `:146` |
| US-010-S06 | `Index` com fila vazia | `ReviewQueueTests.cs:91` |
| US-010-S07 | `BackToPreview` com `AlreadyDecided` | `ReviewTests.cs:243`, `:269`, `:289`; integração `ReviewDecisionConcurrencyTests.cs:106-110`; E2E `:169`. Ver aviso 2 sobre o ramo de repetição |
| US-010-S08 | `PublishChecksAsync` → `PhoneNotConfigured` | `ReviewTests.cs:311` (somente HTTP; o banco do E2E já tem telefone, BACKLOG:139) |
| US-010-S09 | política Administrator na classe | `ReviewQueueTests.cs:105`; `ReviewTests.cs:398`; E2E `ReviewQueueE2ETests.cs:141` |
| US-011-S01 | `Unpublish` → `AdTakedown.UnpublishAsync` | `TakedownTests.cs:54`, `:86`; E2E `TakedownE2ETests.cs:100`. "Não aparece na busca" é da Fase 5 (BACKLOG:144) |
| US-011-S02 | `ConfirmArchive`/`Archive` | `TakedownTests.cs:105`; E2E `:127`; filtro "Arquivado" fechado por `US012S03` (integração `PanelAdListQueryTests.cs:125`). "Endereço antigo" é da Fase 5 |
| US-011-S03 | GET de confirmação não grava | `TakedownTests.cs:132`; E2E `:127` |
| US-011-S04 | favoritos | Fase 5 (declarado) |
| US-011-S05 | `Archive` de Rascunho, Em revisão e Rejeitado | `TakedownTests.cs:159` (3 situações); E2E `:161`; lista padrão sem arquivados em `PanelAdListQueryTests.cs:125` e E2E `PanelAdListE2ETests.cs:136` |
| US-011-S06 | `TakedownActions` + `Read.cshtml` | `TakedownTests.cs:179` |
| US-011-S07 | política por ação + `TakedownActions` | `TakedownTests.cs:217` (3 situações, GET e POST) |
| US-012-S01 e S02 | `AdsController.Index` → `PanelAdListService` → Dapper | `PanelAdListTests.cs:44`, `:73` (o que o serviço pede e o que a tela mostra); `PanelAdListQueryTests.cs:93` e `:246` (o filtro no SQL real) |
| US-012-S03 | filtro `situacao` | `PanelAdListTests.cs:102`; `PanelAdListQueryTests.cs:125` |
| US-012-S04 | busca `q` | `PanelAdListTests.cs:132`; `PanelAdListQueryTests.cs:150` (acento, caixa, `%`, `_`, `[`) |
| US-012-S05 | `RowHref` | `PanelAdListTests.cs:156`; E2E `PanelAdListE2ETests.cs:85` |
| US-012-S06 | estado vazio | `PanelAdListTests.cs:181`, `:197` |
| US-012-S07 | `_Pagination` | `PanelAdListTests.cs:211`; `PanelAdListQueryTests.cs:175` e `:285`; E2E `:169` |
| US-012-S08 | `LoadError` com 503 | `PanelAdListTests.cs:265`; `PanelAdListQueryTests.cs:313` (tempo limite de 10 s real) |

Auditoria anti-vacuous: os testes abertos (`ReviewTests.cs:59`, `TakedownTests.cs:54`, `PanelAdListQueryTests.cs:93`) leem o estado do banco, a fila ou a auditoria depois do pedido; remover a funcionalidade os derrubaria (e as mutações M1 a M7, T1 a T8 e Q1 a Q10 confirmam). A única lacuna de "passaria sem a feature" é o ramo de repetição (aviso 2). O `StubPanelAdListRepository` não filtra de propósito (`tests/GazetaMarketplace.Web.Tests/Support/StubPanelAdListRepository.cs:28-39`): nos testes do site ele só prova o que o serviço **pede**; o filtro real é provado na integração, o que as mutações Q1b, Q2b, Q5 e Q6 sustentam. Paridade de duas implementações: não se aplica à busca (o mesmo `Normalizer` grava `TitleSearch` e normaliza o termo, `Core/Search/Normalizer.cs:13-46`); a regra "o que pode ser retirado" tem três formas sem teste de varredura (aviso 11).

**Reconciliação de OPEN-NNN:** o `TEST_REPORT.md` atual **não possui** nenhum conjunto `OPEN-NNN` (não há seção 12 nem identificadores desse tipo); nas tarefas 4.x as pendências foram registradas em `plans/BACKLOG.md`. Para não deixar nada solto, esta é a disposição dos itens 4.x do BACKLOG:

| Item do BACKLOG (linha) | Disposição |
|---|---|
| 4.1 fila sem paginação (132) | DEFERRED-to-pós-lançamento (aviso 25: definir gatilho) |
| 4.1 mutante equivalente do desempate (133) | CLOSED (aceito, justificado) |
| 4.1 galeria simples (134) | DEFERRED-to-5.2 |
| 4.1 unidades das características (135) | CLOSED (decisão) |
| 4.1 conta de Redator por rodada no E2E (136) | DEFERRED-to-P2 |
| 4.2 reconferência de pendências (137) | ESCALATED (Product Owner confirmar o texto "Faltam N itens…") |
| 4.2 frases da S04 (138) | DEFERRED-to-Fase 5 |
| 4.2 S08 só em HTTP; diálogos (139) | DEFERRED-to-P2 (melhoria progressiva) |
| 4.2 motivo na auditoria (140) | CLOSED (decisão) |
| 4.2 mesma conta nos E2E (141) | DEFERRED-to-P2 (a integração com dois administradores cobre) |
| 4.2 `CepE2ETests` e cache (142) | DEFERRED-to-P2 (item anterior à fase) |
| 4.2 contraste do `btn-outline-danger` (143) | CLOSED (aviso 23 sugere revalidar telas antigas) |
| 4.3 S01/S02/S05 parciais (144) | DEFERRED-to-Fase 5 |
| 4.3 diálogo `alertdialog` (145) | DEFERRED-to-P2 |
| 4.3 `TempData` de uma leitura (146) | DEFERRED-to-P2 |
| 4.3 `ArchivedById` (147) | ESCALATED (Product Owner) |
| 4.3 sem aviso ao autor na despublicação (148) | ESCALATED (Product Owner) |
| 4.3 Docker parou (149) | CLOSED (documentado) |
| Teste instável da 3.7 (150) | CLOSED na prática (teste removido na 4.4), mas a caixa continua aberta (aviso 24) |
| 4.4 estado "Carregando" (152) | DEFERRED-to-P2 |
| 4.4 CHARINDEX e busca de texto completo (153) | DEFERRED-to-pós-lançamento (aviso 25) |
| 4.4 ordenação por COALESCE (154) | DEFERRED-to-pós-lançamento (aviso 25) |
| 4.4 "Alterado em" muda em toda passagem (155) | ESCALATED (Product Owner) |
| 4.4 contraste do `btn-outline-secondary` (156) | CLOSED |
| 4.4 CSS repetido (157) | DEFERRED, recomendo antecipar (aviso 12) |
| 4.4 stub do repositório (158) | CLOSED (convenção) |
| 4.4 `SubmitAdAsync` ×4 (159) | DEFERRED, recomendo antecipar (aviso 13) |
| 4.4 `pagina=` inválida (160) | CLOSED em parte (aviso 1: falta o inteiro grande) |

## 6. Compliance Check

> Cada PASS cita arquivo e linha, o ponto em que o controle está ligado ao pipeline, ou o nome do teste. Controles transversais conferidos no `Program.cs`: antiforgery global (`:47-49`), correlation id e cabeçalhos de segurança (`:80-81`), exception handler das páginas (`:87`), HTTPS (`:89`), limite de corpo (`:100`), autenticação, limitador e autorização (`:102-105`).

| Rule | Status | Evidence (file:line / test) | Notes |
|------|--------|-----------------------------|-------|
| api-conventions.md | PASS | Rotas em kebab-case e plural: `ReviewQueueController.cs:29,47,72,134,154,174,194`, `AdsController.cs:26`; escrita só por POST (`US011S03` prova que o GET não grava) | A fase não cria API JSON nova; versionamento e `ProblemDetails` não se aplicam |
| brownfield.md | N/A | `.claude/PROJECT_PROFILE.md`: Mode greenfield | Não ativo |
| clean-code.md | WARNING | Bom: `AdReview.cs:46-83` (uma responsabilidade por método). Falha: `Preview` com 61 linhas (`ReviewQueueController.cs:72-132`), `AdsController` com 452 linhas e 8 dependências | Avisos 🟢 8, 9, 10, 11 |
| code-style.md | WARNING | `!` em `Index.cshtml:53` e `ReviewTests.cs:119`; comentário truncado `ReviewQueueController.cs:110`; resto em conformidade (namespaces de arquivo, `using` explícitos, sem `?` de referência) | Avisos 🟢 14 e 17 |
| error-handling.md | WARNING | `catch` vazio em `AdSpecsReader.cs:57-59`; `ConflictException` sem tratamento nas ações de decisão (`ReviewQueueController.cs:154-219`). Bom: exceções do domínio (`ForbiddenException`, `ConflictException`, `ValidationException`) e 503 sem detalhe técnico (`Fila_FalhaAoCarregar…`, `US012S08`) | Aviso 🟡 6 e 🟢 7 |
| database.md | WARNING | Bom: parâmetros nomeados (`PanelAdListReadRepository.cs:30-43,72`), `AsNoTracking` (`ReviewQueue.cs:21-22`, `AdService.cs:87`), sem N+1 (`ReviewQueue.cs:29-31`), índice provado (`ConsultaDoRedator_UsaOIndiceDeAutorSituacaoEData`), `RowVersion` com `ConflictException` (`AppDbContext.cs:109-111`). Falha: deslocamento com estouro (`SqlBuilder.cs:100`) | Aviso 🟡 1; sem migration no diff |
| frontend.md | PASS | Só módulo `type="module"` por visão (`ConfirmArchive.cshtml:6`), nenhum `style=`/`onclick`, ganchos `data-*`, Bootstrap, `Url.IsLocalUrl` (`_ErrorState.cshtml:11`); sem JavaScript: `SemJavaScript_BuscaEFiltroFuncionamPorFormularioComum` (`PanelAdListE2ETests.cs:194`); axe e rolagem em `Acessibilidade_*` (`:215`, `ReviewQueueE2ETests.cs:107`) | Avisos 🟢 12, 16, 21, 22, 23 são de manutenção, não de conformidade |
| git-workflow.md | WARNING | Commits convencionais: `9ae2205`, `1d80a1f`, `62e312f`, `164a4d3`. Arquivo não rastreado `Checkpoint4LifecycleTests.cs` | Aviso 🟢 26 |
| monitoring.md | PASS | `LogError` estruturado com `traceId` e sem dado sensível: `ReviewQueueController.cs:58`, `AdsController.cs:54`; correlation id ligado em `Program.cs:80`; testes `Fila_FalhaAoCarregar…` e `US012S08` provam que a tela não mostra a exceção | Métricas e rastreamento não se aplicam à fase |
| naming-conventions.md | PASS | Índice `IX_Ads_AuthorId_Status_UpdatedAt` (`AdConfiguration.cs:74`); parâmetros SQL `Status` e `ArchivedStatus` (`SqlFragments.cs:19,24`); rotas em kebab-case; testes `Metodo_Cenario_Resultado` com prefixo `USxxxSnn` | Os nomes de CSS e JS em português são tratados no aviso 🟢 22 |
| output-style.md | PASS | Seções 4.x do `TEST_REPORT.md` abrem com "Em resumo" (linhas 669, 716, 761, 810); mensagens da interface em `AdMessages.cs` | Este relatório segue o mesmo padrão |
| principles-and-practices.md | WARNING | §2.5 cumprido: achados fora de escopo registrados em `plans/BACKLOG.md:132-160`; §4.5 auditoria e `RowVersion` (`AdService.cs:73`, `AppDbContext.cs:109`). Falha: duplicações (`AdReview.cs:97-107` vs `AdSubmission.cs:80-90`; `ads-index.css` vs `review-queue.css`); mudança de tema global (`bootstrap-tema.css:4-23`) | Avisos 🟢 10, 12, 13, 23 |
| project-structure.md | PASS | Interfaces no Core (`IPanelAdList.cs`, `IAdReview.cs`), implementações na Infrastructure (`PanelAdListReadRepository.cs`, `AdReview.cs`), registro em `ServiceCollectionExtensions.cs` (linhas 60 a 65), Core sem referência a EF ou Dapper | Controllers grandes: avisos 🟢 8 e 9 |
| security.md | PASS | NFR-13: `ReviewQueueController.cs:28`, `AdsController.cs:212-227`, `AdReview.cs:122-128`, `AdTakedown.cs:79-85`; antiforgery `Program.cs:47-49` com a mutação extra E1 da 4.2 derrubada (`SemLogin_SemTokenEAnuncioInexistente_SaoRecusados`, `ReviewTests.cs:424`); SQL parametrizado; XSS (`PreVisualizar_TextoDoUsuarioSaiCodificado`, `Rejeitar_MotivoComHtml…`, `Rejeitado_MostraOMotivoInteiro…`); foto só Publicado (`PhotoDelivery.cs:31-35`); auditoria (RC-16) | Sem 🟡 de segurança; ver 🟢 7 e 20 |
| system-design.md | PASS | Tempo limite de 10 s e cancelamento (`PanelAdListReadRepository.cs:22,78-79`, teste `ConsultaQueEstouraOTempo…`); repetição limitada (`AdReview.cs:24,49-82`); falha com 503 e "Tentar novamente" (`AdsController.cs:51-63`) | Volume: aviso 🟢 25 |
| tech-stack.md | PASS | Nenhum arquivo `csproj`, `Directory.Packages.props` ou `global.json` no diff (`git diff --stat`); Dapper, EF Core, MSTest e Playwright são os aprovados | Nenhuma dependência nova |
| testing.md | WARNING | Bom: pirâmide (HTTP com stub, integração no SQL Server real, E2E), nomes por cenário, mutações. Falha: cobertura numérica ausente (`TEST_REPORT.md:34`); concorrência sem barreira (`ReviewDecisionConcurrencyTests.cs:79`) | Avisos 🟡 2 e 4 |
| overrides/lang-dotnet.md | WARNING | Bom: `CancellationToken` por último, records para DTOs, construtor primário, identificadores em inglês e rotas em português (`ReviewQueueController.cs:29`). Falha: `!` e nomes de CSS e JS em português (`ad-confirm.js:3-8`) | Avisos 🟢 14 e 22 |
| overrides/database-sqlserver.md | PASS | Índice por autor, situação e data (`AdConfiguration.cs:74`), consultas parametrizadas, `AsNoTracking`, `RowVersion` tratado (`AppDbContext.cs:109-111`), nenhuma migration no diff | |
| overrides (Node.js, PHP, Oracle, MySQL, PostgreSQL, MongoDB, ELK) | N/A | `PROJECT_PROFILE.md` não os declara | Fora do Profile |

## 7. Approval Status

| Decision | APPROVE |
|----------|---------|
| Conditions (if any) | 0 🔴. Antes do `/scan` o orquestrador corrige ou aceita explicitamente os 6 🟡 (Gate 7): recomendo corrigir já os avisos 1, 4 e 6 (baratos) e decidir o 3 com o Product Owner (ajuste de documentos). Os avisos 2 e 5 podem ser aceitos com item no BACKLOG se o prazo apertar. Registrar no BACKLOG os 20 itens 🟢 (P2). Nenhuma decisão de produto nova foi tomada pelo revisor. |

## Atualização do orquestrador (2026-10-05)

- **Aviso 🟡 3 (contrato "409"):** resolvido por decisão do Product Owner. O Checkpoint 4 do plano, a ADR-004 e a ARCHITECTURE agora dizem que a decisão simultânea nas páginas HTML responde 302 com a mensagem "por outro administrador" e que o 409 vale para edição concorrente e API.
- **Aviso 🟢 24 (BACKLOG):** a linha colada foi separada e o item do teste instável removido foi marcado como resolvido.
- **Aviso 🟢 26 (arquivo não rastreado):** `Checkpoint4LifecycleTests.cs` entra no commit do Checkpoint 4, junto dos demais testes e documentos do fechamento da fase.
- **Avisos 🟡 1, 2, 4, 5 e 6:** abertos, sem correção de código, aguardando a decisão do Product Owner (corrigir antes do `/scan` ou aceitar). Todos estão no BACKLOG e em `plans/BACKLOG-TRIAGEM-FASE-4.md`.
