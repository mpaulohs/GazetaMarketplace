# Verify Matrix — GazetaMarketplace 6953fa3

> **Em resumo:** cada um dos 130 cenários `@US-XXX-Snn` de `specs/SPEC.md` (v1.9) ligado ao teste que o prova e à **camada** em que ele roda. **87 cenários (67%) estão provados por teste de navegador no artefato publicado.** Os outros **43 têm prova em processo e estão dispensados** da exigência de navegador por decisão do Product Owner em 2026-10-07 (41 aprovados na lista; 2 acrescentados por este relatório, `US-011-S07` e `US-013-S08`, da mesma natureza e **aguardando o seu "de acordo"**). Com as dispensas, nenhum cenário fica sem verificação; sem elas o gate (100%) não fecha.

**Como o mapa foi feito:** busca automática, nos três projetos de teste, de qualquer menção ao id do cenário no nome do teste ou em comentário (`US008S15`, `US-008-S15`, e nomes com vários cenários como `US002S01_S02_S05` e `US009S02eS03`). `E2E-UI` = projeto `GazetaMarketplace.Web.Tests.Playwright`, que roda contra o artefato publicado (258 testes, 0 falhas); `Integration` = SQL Server real em contêiner com o host dentro do processo; `In-process` = SQLite em memória, host dentro do processo. A busca é por menção explícita e **subestima** a cobertura real.

**O que mudou desde o primeiro relatório (mesmo dia):** 14 cenários só estavam "sem marca" porque o leitor não entendia `US002S01_S02_S05`; o leitor foi corrigido (sem mudar teste). 3 testes de navegador foram **renomeados** para levar os ids que já provam (`US007S01_S02_S05_S06_S07_…`, `US011S03_S02_S06_…`, `US004S01_S02_S03_…`); rodaram de novo e passaram (3/3), e o total continua 258.

| Scenario ID | Acceptance scenario | Verify test (arquivos) | Layer | Phase | Result |
|---|---|---|---|---|---|
| US-001-S01 | Página inicial mostra categorias e anúncios recentes (@happy) | In-process: `ShowcaseTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-001-S02 | Entrar em uma categoria principal (@happy) | In-process: `ShowcaseTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-001-S03 | Entrar em uma subcategoria e voltar pelo caminho de navegação (@happy) | E2E-UI: `ShowcaseE2ETests.cs`; In-process: `ShowcaseTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-001-S04 | Categoria sem anúncios publicados (@edge) | In-process: `ShowcaseTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-001-S05 | Site ainda sem nenhum anúncio publicado (@edge) | In-process: `ShowcaseTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-001-S06 | Falha ao carregar a página inicial (@negative) | In-process: `ShowcaseTests.cs`, `StubShowcaseRepository.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-001-S07 | Endereço de categoria que não existe (@negative) | E2E-UI: `ShowcaseE2ETests.cs`; In-process: `ShowcaseTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-001-S08 | Página inicial em tela de celular estreita (@edge) | E2E-UI: `ShowcaseE2ETests.cs`; In-process: `ShowcaseTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-002-S01 | Buscar por texto (@happy) | E2E-UI: `SearchE2ETests.cs`; Integration: `SearchQueryTests.cs`; In-process: `SearchTests.cs` | E2E-UI + Integration + In-process | 3 | PASS (E2E no artefato) |
| US-002-S02 | Combinar categoria, localização e preço (@happy) | E2E-UI: `SearchE2ETests.cs`; Integration: `SearchQueryTests.cs`; In-process: `SearchTests.cs` | E2E-UI + Integration + In-process | 3 | PASS (E2E no artefato) |
| US-002-S03 | Filtrar por características de veículo (@happy) | E2E-UI: `SearchE2ETests.cs`; Integration: `SearchQueryTests.cs`; In-process: `SearchTests.cs` | E2E-UI + Integration + In-process | 3 | PASS (E2E no artefato) |
| US-002-S04 | Filtrar terrenos, sítios e fazendas por área (@happy) | E2E-UI: `SearchE2ETests.cs`; Integration: `SearchQueryTests.cs`; In-process: `SearchTests.cs` | E2E-UI + Integration + In-process | 3 | PASS (E2E no artefato) |
| US-002-S05 | Ordenar os resultados (@happy) | E2E-UI: `SearchE2ETests.cs`; Integration: `SearchQueryTests.cs`; In-process: `SearchTests.cs` | E2E-UI + Integration + In-process | 3 | PASS (E2E no artefato) |
| US-002-S06 | Paginar os resultados (@happy) | Integration: `SearchQueryTests.cs`; In-process: `SearchTests.cs` | Integration + In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-002-S07 | Busca sem resultados (@edge) | E2E-UI: `SearchE2ETests.cs`; In-process: `SearchTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-002-S08 | Faixa de preço invertida (@negative) | E2E-UI: `SearchE2ETests.cs`; In-process: `SearchTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-002-S09 | Compartilhar uma busca pelo endereço da página (@edge) | E2E-UI: `SearchE2ETests.cs`; In-process: `SearchTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-002-S10 | Trocar a UF limpa a cidade escolhida (@edge) | E2E-UI: `SearchE2ETests.cs`; In-process: `SearchTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-002-S11 | Falha ao buscar (@negative) | In-process: `SearchTests.cs`, `StubSearchReadRepository.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-002-S12 | Busca em tela de celular estreita (@edge) | E2E-UI: `SearchE2ETests.cs`; In-process: `SearchTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-003-S01 | Abrir um anúncio completo (@happy) | In-process: `AdDetailTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-003-S02 | Percorrer a galeria de um anúncio com 20 fotos (@happy) | E2E-UI: `AdDetailE2ETests.cs`; In-process: `AdDetailTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-003-S03 | Ampliar uma foto (@happy) | E2E-UI: `AdDetailE2ETests.cs`; In-process: `AdDetailTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-003-S04 | Anúncio de categoria sem ficha de veículo nem de terreno (@edge) | In-process: `AdDetailTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-003-S05 | Anúncio com uma única foto (@edge) | E2E-UI: `AdDetailE2ETests.cs`; In-process: `AdDetailTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-003-S06 | Abrir um anúncio que não está mais disponível (@negative) | In-process: `AdDetailTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-003-S07 | Uma foto não carrega (@negative) | E2E-UI: `AdDetailE2ETests.cs`; In-process: `AdDetailTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-003-S08 | Anúncio em tela de celular estreita (@edge) | E2E-UI: `AdDetailE2ETests.cs`; In-process: `AdDetailTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-004-S01 | Chamar no WhatsApp a partir de um anúncio (@happy) | E2E-UI: `ContactE2ETests.cs`; In-process: `ContactTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-004-S02 | Ligar a partir de um anúncio (@happy) | E2E-UI: `ContactE2ETests.cs`; In-process: `ContactTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-004-S03 | O contato é visível sem login (@happy) | E2E-UI: `ContactE2ETests.cs`; In-process: `ContactTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-004-S04 | Título com acentos e símbolos na mensagem do WhatsApp (@edge) | E2E-UI: `ContactE2ETests.cs`; In-process: `ContactTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-004-S05 | WhatsApp em computador sem o aplicativo instalado (@edge) | E2E-UI: `ContactE2ETests.cs`; In-process: `ContactTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-005-S01 | Favoritar um anúncio pela lista (@happy) | E2E-UI: `FavoritesE2ETests.cs` | E2E-UI | 3 | PASS (E2E no artefato) |
| US-005-S02 | Favoritar e desfavoritar pela página do anúncio (@happy) | E2E-UI: `FavoritesE2ETests.cs`; In-process: `FavoritesTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-005-S03 | Favoritos continuam depois de fechar o navegador (@happy) | E2E-UI: `FavoritesE2ETests.cs` | E2E-UI | 3 | PASS (E2E no artefato) |
| US-005-S04 | Remover um anúncio da página Meus favoritos (@happy) | E2E-UI: `FavoritesE2ETests.cs` | E2E-UI | 3 | PASS (E2E no artefato) |
| US-005-S05 | Lista de favoritos vazia (@edge) | E2E-UI: `FavoritesE2ETests.cs`; In-process: `FavoritesTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-005-S06 | Um favorito deixa de estar disponível (@edge) | E2E-UI: `FavoritesE2ETests.cs` | E2E-UI | 3 | PASS (E2E no artefato) |
| US-005-S07 | O navegador não permite salvar favoritos (@negative) | E2E-UI: `FavoritesE2ETests.cs` | E2E-UI | 3 | PASS (E2E no artefato) |
| US-005-S08 | Favoritos não acompanham o visitante em outro aparelho (@edge) | E2E-UI: `FavoritesE2ETests.cs`; In-process: `FavoritesTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-006-S01 | Redator entra no painel (@happy) | In-process: `AccountTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-006-S02 | Administrador entra no painel (@happy) | In-process: `AccountTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-006-S03 | Sair do painel (@happy) | E2E-UI: `AccountE2ETests.cs`; In-process: `AccountTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-006-S04 | E-mail ou senha incorretos (@negative) | In-process: `AccountTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-006-S05 | Conta desativada (@negative) | In-process: `AccountTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-006-S06 | Muitas tentativas de entrada (@negative) | In-process: `AccountTests.cs`, `LoginFailureCounterTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-006-S07 | Abrir uma página do painel sem estar logado (@edge) | In-process: `AccountTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-006-S08 | Sessão expirada por inatividade (@edge) | E2E-UI: `AccountE2ETests.cs`; In-process: `AccountTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-006-S09 | Primeiro acesso exige trocar a senha provisória (@edge) | In-process: `FirstAccessTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-006-S10 | Redator tenta abrir uma página exclusiva do administrador (@negative) | In-process: `UsersTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-007-S01 | Pedir a redefinição de senha (@happy) | E2E-UI: `PasswordRecoveryE2ETests.cs`; In-process: `PasswordRecoveryTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-007-S02 | Definir uma nova senha pelo link (@happy) | E2E-UI: `PasswordRecoveryE2ETests.cs`; In-process: `PasswordRecoveryTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-007-S03 | E-mail não cadastrado (@negative) | E2E-UI: `PasswordRecoveryE2ETests.cs`; In-process: `PasswordRecoveryTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-007-S04 | Link de redefinição expirado (@negative) | In-process: `PasswordRecoveryTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-007-S05 | Link de redefinição já utilizado (@negative) | E2E-UI: `PasswordRecoveryE2ETests.cs`; In-process: `PasswordRecoveryTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-007-S06 | Nova senha que não cumpre a política (@negative) | E2E-UI: `PasswordRecoveryE2ETests.cs`; In-process: `PasswordRecoveryTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-007-S07 | Confirmação de senha diferente (@negative) | E2E-UI: `PasswordRecoveryE2ETests.cs`; In-process: `PasswordRecoveryTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-008-S01 | Salvar um rascunho completo (@happy) | E2E-UI: `DraftE2ETests.cs`; In-process: `AdDraftServiceTests.cs`, `DraftTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-008-S02 | Adicionar fotos ao anúncio (@happy) | E2E-UI: `PhotosE2ETests.cs`; In-process: `PhotosEndpointsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-008-S03 | Trocar a capa e remover uma foto (@happy) | E2E-UI: `PhotosE2ETests.cs`; In-process: `PhotosEndpointsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-008-S04 | Passar do limite de 20 fotos (@negative) | E2E-UI: `PhotosE2ETests.cs`; In-process: `PhotosEndpointsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-008-S05 | Enviar um arquivo que não é foto aceita (@negative) | E2E-UI: `PhotosE2ETests.cs`; In-process: `PhotosEndpointsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-008-S06 | Falha ao enviar uma foto (@negative) | E2E-UI: `PhotosE2ETests.cs`; In-process: `PhotosEndpointsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-008-S07 | Salvar um rascunho só com o título (@edge) | E2E-UI: `PhotosE2ETests.cs`; In-process: `AdDraftServiceTests.cs`, `AdEntityTests.cs`, `DraftTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-008-S08 | Salvar sem título (@negative) | E2E-UI: `DraftE2ETests.cs`; In-process: `AdDraftServiceTests.cs`, `AdEntityTests.cs`, `DraftTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-008-S09 | Campos mudam conforme a categoria (@edge) | E2E-UI: `DraftE2ETests.cs`; In-process: `AdDraftServiceTests.cs`, `DraftTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-008-S10 | Redator tenta editar anúncio de outro redator (@negative) | In-process: `AdDraftServiceTests.cs`, `AdServiceTests.cs`, `DraftTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-008-S11 | Corrigir um anúncio rejeitado (@edge) | In-process: `AdDraftServiceTests.cs`, `DraftTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-008-S12 | Anúncio em revisão não pode ser editado pelo Redator (@edge) | In-process: `AdDraftServiceTests.cs`, `DraftTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-008-S13 | Administrador corrige o preço de um anúncio publicado (@edge) | In-process: `AdDraftServiceTests.cs`, `DraftTests.cs`, `PublishedEditTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-008-S16 | Administrador não remove a última foto de um anúncio publicado (@negative) | In-process: `PublishedPhotoTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-008-S15 | Administrador não salva um anúncio publicado que ficaria incompleto (@negative) | In-process: `PublishedEditTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-008-S14 | Serviço de CEP fora do ar (@negative) | E2E-UI: `DraftE2ETests.cs`; In-process: `DraftTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-009-S01 | Enviar um rascunho completo para revisão (@happy) | E2E-UI: `SubmitForReviewE2ETests.cs`; In-process: `SubmitForReviewTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-009-S02 | Enviar um rascunho incompleto (@negative) | E2E-UI: `SubmitForReviewE2ETests.cs`; In-process: `SubmitForReviewTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-009-S03 | Faltam características obrigatórias da categoria (@negative) | E2E-UI: `SubmitForReviewE2ETests.cs`; In-process: `SubmitForReviewTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-009-S04 | Reenviar um anúncio rejeitado depois de corrigi-lo (@edge) | E2E-UI: `Checkpoint4E2ETests.cs`; In-process: `AdServiceTests.cs`, `SubmitForReviewTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-009-S05 | Clicar duas vezes em enviar (@edge) | E2E-UI: `SubmitForReviewE2ETests.cs`; Integration: `AdServiceConcurrencyTests.cs`, `SubmitForReviewConcurrencyTests.cs`; In-process: `AdServiceTests.cs`, `SubmitForReviewTests.cs` | E2E-UI + Integration + In-process | 3 | PASS (E2E no artefato) |
| US-010-S01 | Ver a fila de revisão (@happy) | In-process: `ReviewQueueTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-010-S02 | Pré-visualizar um anúncio antes de decidir (@happy) | In-process: `ReviewQueueTests.cs`, `ReviewTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-010-S03 | Publicar um anúncio (@happy) | E2E-UI: `ReviewDecisionE2ETests.cs`; In-process: `ReviewTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-010-S04 | Rejeitar um anúncio com motivo (@happy) | E2E-UI: `ReviewDecisionE2ETests.cs`; In-process: `ReviewTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-010-S05 | Rejeitar sem informar o motivo (@negative) | E2E-UI: `ReviewDecisionE2ETests.cs`; In-process: `ReviewTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-010-S06 | Fila de revisão vazia (@edge) | In-process: `ReviewQueueTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-010-S07 | Dois administradores decidem o mesmo anúncio (@edge) | E2E-UI: `ReviewDecisionE2ETests.cs`; Integration: `ReviewDecisionConcurrencyTests.cs`; In-process: `DecisionRetryTests.cs`, `ReviewTests.cs` | E2E-UI + Integration + In-process | 3 | PASS (E2E no artefato) |
| US-010-S08 | Publicar sem o telefone do site configurado (@negative) | E2E-UI: `ReviewDecisionE2ETests.cs`; In-process: `PermissionMatrixTests.cs`, `ReviewTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-010-S09 | Redator não pode revisar anúncios (@negative) | In-process: `ReviewQueueTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-011-S01 | Despublicar um anúncio (@happy) | E2E-UI: `TakedownE2ETests.cs`; In-process: `TakedownTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-011-S02 | Arquivar um anúncio publicado (@happy) | E2E-UI: `TakedownE2ETests.cs`; In-process: `TakedownTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-011-S03 | Cancelar a confirmação (@edge) | E2E-UI: `TakedownE2ETests.cs`; In-process: `TakedownTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-011-S04 | Anúncio arquivado some dos favoritos do visitante (@edge) | E2E-UI: `FavoritesE2ETests.cs` | E2E-UI | 3 | PASS (E2E no artefato) |
| US-011-S05 | Arquivar um anúncio que ainda não foi publicado (@edge) | E2E-UI: `TakedownE2ETests.cs`; In-process: `ReviewQueueTests.cs`, `TakedownTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-011-S06 | Anúncio arquivado não tem ações de retirada (@negative) | E2E-UI: `TakedownE2ETests.cs`; In-process: `TakedownTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-011-S07 | Redator não vê as ações de retirada (@negative) | citado em E2E (sem prova do cenário): `FavoritesE2ETests.cs`; In-process: `TakedownTests.cs` | In-process | — | DISPENSADO: prova em processo (acrescentado, aguarda "de acordo") |
| US-012-S01 | Redator vê apenas os próprios anúncios (@happy) | Integration: `PanelAdListQueryTests.cs`; In-process: `PanelAdListTests.cs` | Integration + In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-012-S02 | Administrador vê todos os anúncios com o autor (@happy) | Integration: `PanelAdListQueryTests.cs`; In-process: `PanelAdListTests.cs` | Integration + In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-012-S03 | Filtrar por situação (@happy) | E2E-UI: `PanelAdListE2ETests.cs`; Integration: `PanelAdListQueryTests.cs`; In-process: `PanelAdListTests.cs` | E2E-UI + Integration + In-process | 3 | PASS (E2E no artefato) |
| US-012-S04 | Buscar um anúncio pelo título no painel (@happy) | E2E-UI: `PanelAdListE2ETests.cs`; Integration: `PanelAdListQueryTests.cs`; In-process: `PanelAdListTests.cs` | E2E-UI + Integration + In-process | 3 | PASS (E2E no artefato) |
| US-012-S05 | Abrir um anúncio da lista (@happy) | E2E-UI: `PanelAdListE2ETests.cs`; In-process: `PanelAdListTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-012-S06 | Redator ainda sem anúncios (@edge) | In-process: `PanelAdListTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-012-S07 | Lista com mais de 20 anúncios (@edge) | E2E-UI: `PanelAdListE2ETests.cs`; Integration: `PanelAdListQueryTests.cs`; In-process: `PanelAdListTests.cs` | E2E-UI + Integration + In-process | 3 | PASS (E2E no artefato) |
| US-012-S08 | Falha ao carregar a lista (@negative) | In-process: `PanelAdListTests.cs`, `StubPanelAdListRepository.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-013-S01 | Criar uma subcategoria (@happy) | E2E-UI: `CategoriesE2ETests.cs`; In-process: `CategoriesTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-013-S02 | Criar uma categoria principal (@happy) | E2E-UI: `CategoriesE2ETests.cs`; In-process: `CategoriesTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-013-S03 | Renomear uma categoria (@happy) | E2E-UI: `CategoriesE2ETests.cs`; In-process: `CategoriesTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-013-S04 | Mudar a ordem das categorias (@happy) | E2E-UI: `CategoriesE2ETests.cs`; In-process: `OrderingTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-013-S05 | Excluir uma categoria vazia (@happy) | E2E-UI: `CategoriesE2ETests.cs`; In-process: `DeletionTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-013-S06 | Nome de categoria repetido (@negative) | E2E-UI: `CategoriesE2ETests.cs`; In-process: `CategoriesTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-013-S07 | Nome de categoria vazio (@negative) | E2E-UI: `CategoriesE2ETests.cs`; In-process: `CategoriesTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-013-S08 | Excluir categoria que tem anúncios (@negative) | citado em E2E (sem prova do cenário): `CategoriesE2ETests.cs`; Integration: `AdsCategoryUsageTests.cs`; In-process: `DeletionTests.cs` | Integration + In-process | — | DISPENSADO: prova em processo (acrescentado, aguarda "de acordo") |
| US-013-S09 | Excluir categoria que tem subcategorias (@negative) | E2E-UI: `CategoriesE2ETests.cs`; In-process: `DeletionTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-013-S10 | Categorias com características específicas não podem ser excluídas (@negative) | E2E-UI: `CategoriesE2ETests.cs`; In-process: `DeletionTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-013-S11 | Categorias têm até três níveis (@edge) | E2E-UI: `CategoriesE2ETests.cs`; In-process: `CategoriesTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-014-S01 | Criar uma conta de Redator (@happy) | In-process: `UsersTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-014-S02 | Mudar o papel de um usuário (@happy) | In-process: `UsersTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-014-S03 | Desativar uma conta (@happy) | E2E-UI: `UsersE2ETests.cs`; In-process: `UsersTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-014-S04 | Reativar uma conta (@happy) | In-process: `UsersTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-014-S05 | E-mail já cadastrado (@negative) | In-process: `UsersTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-014-S06 | Senha provisória fraca (@negative) | In-process: `ErrorDescriberTests.cs`, `UsersTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-014-S07 | E-mail em formato inválido (@negative) | In-process: `UsersTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-014-S08 | Desativar a própria conta (@negative) | In-process: `UsersTests.cs` | In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-014-S09 | Remover o último administrador (@negative) | Integration: `ConcurrencyTests.cs`; In-process: `UsersTests.cs` | Integration + In-process | — | DISPENSADO: prova em processo (aprovado pelo PO, 2026-10-07) |
| US-014-S10 | Redefinir a senha de alguém da equipe (@happy) | E2E-UI: `UsersE2ETests.cs`; In-process: `UsersTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-015-S01 | Definir o telefone/WhatsApp do site (@happy) | E2E-UI: `SettingsE2ETests.cs`; In-process: `SettingsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-015-S02 | Trocar o número em uso (@happy) | E2E-UI: `SettingsE2ETests.cs`; In-process: `SettingsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-015-S03 | Número digitado sem formatação (@edge) | E2E-UI: `SettingsE2ETests.cs`; In-process: `SettingsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-015-S04 | Número inválido (@negative) | E2E-UI: `SettingsE2ETests.cs`; In-process: `SettingsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-015-S05 | Número vazio (@negative) | E2E-UI: `SettingsE2ETests.cs`; In-process: `SettingsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-015-S06 | Redator não acessa as configurações (@negative) | E2E-UI: `SettingsE2ETests.cs`; In-process: `SettingsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |

**Coverage:** 87/130 (66%) provados por teste de navegador no artefato; 43/130 dispensados com prova em processo (waiver do Product Owner, 2026-10-07: "a cobertura real é alta, o número subestima por buscar menção explícita; não vale escrever testes novos"). **0 cenários sem nenhum teste.** Quando o `/test` marcar ou escrever os E2E que faltam, as dispensas caem uma a uma (BACKLOG V-04, agora P2).

## Dispensas (cenário sem teste de navegador; prova em processo)

Justificativa comum: **prova em processo** — o cenário é afirmado por teste que roda no `dotnet test` (SQLite ou SQL Server real) com a mesma pilha de HTTP, regras e banco, mas **sem o site publicado, sem HTTPS real e sem navegador**. Aprovador: Product Owner, 2026-10-07.

- `US-001-S01` — Página inicial mostra categorias e anúncios recentes — In-process
- `US-001-S02` — Entrar em uma categoria principal — In-process
- `US-001-S04` — Categoria sem anúncios publicados — In-process
- `US-001-S05` — Site ainda sem nenhum anúncio publicado — In-process
- `US-001-S06` — Falha ao carregar a página inicial — In-process
- `US-002-S06` — Paginar os resultados — Integration + In-process
- `US-002-S11` — Falha ao buscar — In-process
- `US-003-S01` — Abrir um anúncio completo — In-process
- `US-003-S04` — Anúncio de categoria sem ficha de veículo nem de terreno — In-process
- `US-003-S06` — Abrir um anúncio que não está mais disponível — In-process
- `US-006-S01` — Redator entra no painel — In-process
- `US-006-S02` — Administrador entra no painel — In-process
- `US-006-S04` — E-mail ou senha incorretos — In-process
- `US-006-S05` — Conta desativada — In-process
- `US-006-S06` — Muitas tentativas de entrada — In-process
- `US-006-S07` — Abrir uma página do painel sem estar logado — In-process
- `US-006-S09` — Primeiro acesso exige trocar a senha provisória — In-process
- `US-006-S10` — Redator tenta abrir uma página exclusiva do administrador — In-process
- `US-007-S04` — Link de redefinição expirado — In-process
- `US-008-S10` — Redator tenta editar anúncio de outro redator — In-process
- `US-008-S11` — Corrigir um anúncio rejeitado — In-process
- `US-008-S12` — Anúncio em revisão não pode ser editado pelo Redator — In-process
- `US-008-S13` — Administrador corrige o preço de um anúncio publicado — In-process
- `US-008-S16` — Administrador não remove a última foto de um anúncio publicado — In-process
- `US-008-S15` — Administrador não salva um anúncio publicado que ficaria incompleto — In-process
- `US-010-S01` — Ver a fila de revisão — In-process
- `US-010-S02` — Pré-visualizar um anúncio antes de decidir — In-process
- `US-010-S06` — Fila de revisão vazia — In-process
- `US-010-S09` — Redator não pode revisar anúncios — In-process
- `US-011-S07` — Redator não vê as ações de retirada — In-process  **(acrescentado: aguarda de acordo)**
- `US-012-S01` — Redator vê apenas os próprios anúncios — Integration + In-process
- `US-012-S02` — Administrador vê todos os anúncios com o autor — Integration + In-process
- `US-012-S06` — Redator ainda sem anúncios — In-process
- `US-012-S08` — Falha ao carregar a lista — In-process
- `US-013-S08` — Excluir categoria que tem anúncios — Integration + In-process  **(acrescentado: aguarda de acordo)**
- `US-014-S01` — Criar uma conta de Redator — In-process
- `US-014-S02` — Mudar o papel de um usuário — In-process
- `US-014-S04` — Reativar uma conta — In-process
- `US-014-S05` — E-mail já cadastrado — In-process
- `US-014-S06` — Senha provisória fraca — In-process
- `US-014-S07` — E-mail em formato inválido — In-process
- `US-014-S08` — Desativar a própria conta — In-process
- `US-014-S09` — Remover o último administrador — Integration + In-process
