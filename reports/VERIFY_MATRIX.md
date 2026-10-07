# Verify Matrix — GazetaMarketplace 6953fa3

> **Em resumo:** cada cenário `@US-XXX-Snn` de `specs/SPEC.md` (v1.9, 130 cenários) ligado ao teste que o prova e à **camada** em que ele roda. Como o produto tem interface, um cenário que o usuário observa só conta como provado **no artefato** quando existe um teste de navegador (E2E-UI) marcado com o id do cenário. As colunas abaixo separam o que está provado no artefato publicado do que só tem prova **em processo** (host de teste dentro do `dotnet test`, sem o site publicado, sem HTTPS real).

**Como o mapa foi feito:** busca automática, nos três projetos de teste, de qualquer menção ao id do cenário (`US008S15`, `US-008-S15`) em nome de teste ou comentário; `E2E-UI` = projeto `GazetaMarketplace.Web.Tests.Playwright` (roda contra o artefato publicado, 258 testes, 0 falhas nesta rodada); `Integration` = SQL Server real em contêiner, host dentro do processo; `In-process` = SQLite em memória, host dentro do processo. "PROVÁVEL" = um arquivo de E2E cita a história e o cenário, mas nenhum teste leva o id; **não conta como prova** até ser marcado.

| Scenario ID | Acceptance scenario | Verify test (arquivos) | Layer | Phase | Result |
|---|---|---|---|---|---|
| US-001-S01 | Página inicial mostra categorias e anúncios recentes (@happy) | In-process: `ShowcaseTests.cs` | In-process | — | GAP-UI: só em processo |
| US-001-S02 | Entrar em uma categoria principal (@happy) | In-process: `ShowcaseTests.cs` | In-process | — | GAP-UI: só em processo |
| US-001-S03 | Entrar em uma subcategoria e voltar pelo caminho de navegação (@happy) | E2E-UI: `ShowcaseE2ETests.cs`; In-process: `ShowcaseTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-001-S04 | Categoria sem anúncios publicados (@edge) | In-process: `ShowcaseTests.cs` | In-process | — | GAP-UI: só em processo |
| US-001-S05 | Site ainda sem nenhum anúncio publicado (@edge) | In-process: `ShowcaseTests.cs` | In-process | — | GAP-UI: só em processo |
| US-001-S06 | Falha ao carregar a página inicial (@negative) | In-process: `ShowcaseTests.cs`, `StubShowcaseRepository.cs` | In-process | — | GAP-UI: só em processo |
| US-001-S07 | Endereço de categoria que não existe (@negative) | E2E-UI: `ShowcaseE2ETests.cs`; In-process: `ShowcaseTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-001-S08 | Página inicial em tela de celular estreita (@edge) | E2E-UI: `ShowcaseE2ETests.cs`; In-process: `ShowcaseTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-002-S01 | Buscar por texto (@happy) | E2E-UI: `SearchE2ETests.cs`; Integration: `SearchQueryTests.cs`; In-process: `SearchTests.cs` | E2E-UI + Integration + In-process | 3 | PASS (E2E no artefato) |
| US-002-S02 | Combinar categoria, localização e preço (@happy) | E2E-UI (por comentário): `SearchE2ETests.cs`; Integration: `SearchQueryTests.cs`; In-process: `SearchTests.cs` | E2E-UI (por comentário) + Integration + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-002-S03 | Filtrar por características de veículo (@happy) | E2E-UI: `SearchE2ETests.cs`; Integration: `SearchQueryTests.cs`; In-process: `SearchTests.cs` | E2E-UI + Integration + In-process | 3 | PASS (E2E no artefato) |
| US-002-S04 | Filtrar terrenos, sítios e fazendas por área (@happy) | E2E-UI (por comentário): `SearchE2ETests.cs`; Integration: `SearchQueryTests.cs`; In-process: `SearchTests.cs` | E2E-UI (por comentário) + Integration + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-002-S05 | Ordenar os resultados (@happy) | E2E-UI (por comentário): `SearchE2ETests.cs`; Integration: `SearchQueryTests.cs`; In-process: `SearchTests.cs` | E2E-UI (por comentário) + Integration + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-002-S06 | Paginar os resultados (@happy) | Integration: `SearchQueryTests.cs`; In-process: `SearchTests.cs` | Integration + In-process | — | GAP-UI: só em processo |
| US-002-S07 | Busca sem resultados (@edge) | E2E-UI: `SearchE2ETests.cs`; In-process: `SearchTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-002-S08 | Faixa de preço invertida (@negative) | E2E-UI: `SearchE2ETests.cs`; In-process: `SearchTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-002-S09 | Compartilhar uma busca pelo endereço da página (@edge) | E2E-UI: `SearchE2ETests.cs`; In-process: `SearchTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-002-S10 | Trocar a UF limpa a cidade escolhida (@edge) | E2E-UI: `SearchE2ETests.cs`; In-process: `SearchTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-002-S11 | Falha ao buscar (@negative) | In-process: `SearchTests.cs`, `StubSearchReadRepository.cs` | In-process | — | GAP-UI: só em processo |
| US-002-S12 | Busca em tela de celular estreita (@edge) | E2E-UI: `SearchE2ETests.cs`; In-process: `SearchTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-003-S01 | Abrir um anúncio completo (@happy) | In-process: `AdDetailTests.cs` | In-process | — | GAP-UI: só em processo |
| US-003-S02 | Percorrer a galeria de um anúncio com 20 fotos (@happy) | E2E-UI: `AdDetailE2ETests.cs`; In-process: `AdDetailTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-003-S03 | Ampliar uma foto (@happy) | E2E-UI: `AdDetailE2ETests.cs`; In-process: `AdDetailTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-003-S04 | Anúncio de categoria sem ficha de veículo nem de terreno (@edge) | In-process: `AdDetailTests.cs` | In-process | — | GAP-UI: só em processo |
| US-003-S05 | Anúncio com uma única foto (@edge) | E2E-UI: `AdDetailE2ETests.cs`; In-process: `AdDetailTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-003-S06 | Abrir um anúncio que não está mais disponível (@negative) | In-process: `AdDetailTests.cs` | In-process | — | GAP-UI: só em processo |
| US-003-S07 | Uma foto não carrega (@negative) | E2E-UI: `AdDetailE2ETests.cs`; In-process: `AdDetailTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-003-S08 | Anúncio em tela de celular estreita (@edge) | E2E-UI: `AdDetailE2ETests.cs`; In-process: `AdDetailTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-004-S01 | Chamar no WhatsApp a partir de um anúncio (@happy) | E2E-UI: `ContactE2ETests.cs`; In-process: `ContactTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-004-S02 | Ligar a partir de um anúncio (@happy) | E2E-UI (por comentário): `ContactE2ETests.cs`; In-process: `ContactTests.cs` | E2E-UI (por comentário) + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-004-S03 | O contato é visível sem login (@happy) | E2E-UI (por comentário): `ContactE2ETests.cs`; In-process: `ContactTests.cs` | E2E-UI (por comentário) + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-004-S04 | Título com acentos e símbolos na mensagem do WhatsApp (@edge) | E2E-UI: `ContactE2ETests.cs`; In-process: `ContactTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-004-S05 | WhatsApp em computador sem o aplicativo instalado (@edge) | E2E-UI: `ContactE2ETests.cs`; In-process: `ContactTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-005-S01 | Favoritar um anúncio pela lista (@happy) | E2E-UI: `FavoritesE2ETests.cs` | E2E-UI | 3 | PASS (E2E no artefato) |
| US-005-S02 | Favoritar e desfavoritar pela página do anúncio (@happy) | E2E-UI (por comentário): `FavoritesE2ETests.cs`; In-process: `FavoritesTests.cs` | E2E-UI (por comentário) + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-005-S03 | Favoritos continuam depois de fechar o navegador (@happy) | E2E-UI: `FavoritesE2ETests.cs` | E2E-UI | 3 | PASS (E2E no artefato) |
| US-005-S04 | Remover um anúncio da página Meus favoritos (@happy) | E2E-UI (por comentário): `FavoritesE2ETests.cs` | E2E-UI (por comentário) | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-005-S05 | Lista de favoritos vazia (@edge) | E2E-UI (por comentário): `FavoritesE2ETests.cs`; In-process: `FavoritesTests.cs` | E2E-UI (por comentário) + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-005-S06 | Um favorito deixa de estar disponível (@edge) | E2E-UI: `FavoritesE2ETests.cs` | E2E-UI | 3 | PASS (E2E no artefato) |
| US-005-S07 | O navegador não permite salvar favoritos (@negative) | E2E-UI: `FavoritesE2ETests.cs` | E2E-UI | 3 | PASS (E2E no artefato) |
| US-005-S08 | Favoritos não acompanham o visitante em outro aparelho (@edge) | E2E-UI (por comentário): `FavoritesE2ETests.cs` | E2E-UI (por comentário) | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-006-S01 | Redator entra no painel (@happy) | In-process: `AccountTests.cs` | In-process | — | GAP-UI: só em processo |
| US-006-S02 | Administrador entra no painel (@happy) | In-process: `AccountTests.cs` | In-process | — | GAP-UI: só em processo |
| US-006-S03 | Sair do painel (@happy) | E2E-UI: `AccountE2ETests.cs`; In-process: `AccountTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-006-S04 | E-mail ou senha incorretos (@negative) | In-process: `AccountTests.cs` | In-process | — | GAP-UI: só em processo |
| US-006-S05 | Conta desativada (@negative) | In-process: `AccountTests.cs` | In-process | — | GAP-UI: só em processo |
| US-006-S06 | Muitas tentativas de entrada (@negative) | In-process: `AccountTests.cs`, `LoginFailureCounterTests.cs` | In-process | — | GAP-UI: só em processo |
| US-006-S07 | Abrir uma página do painel sem estar logado (@edge) | In-process: `AccountTests.cs` | In-process | — | GAP-UI: só em processo |
| US-006-S08 | Sessão expirada por inatividade (@edge) | E2E-UI: `AccountE2ETests.cs`; In-process: `AccountTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-006-S09 | Primeiro acesso exige trocar a senha provisória (@edge) | In-process: `FirstAccessTests.cs` | In-process | — | GAP-UI: só em processo |
| US-006-S10 | Redator tenta abrir uma página exclusiva do administrador (@negative) | In-process: `UsersTests.cs` | In-process | — | GAP-UI: só em processo |
| US-007-S01 | Pedir a redefinição de senha (@happy) | E2E-UI (por comentário): `PasswordRecoveryE2ETests.cs`; In-process: `PasswordRecoveryTests.cs` | E2E-UI (por comentário) + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-007-S02 | Definir uma nova senha pelo link (@happy) | E2E-UI (por comentário): `PasswordRecoveryE2ETests.cs`; In-process: `PasswordRecoveryTests.cs` | E2E-UI (por comentário) + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-007-S03 | E-mail não cadastrado (@negative) | E2E-UI: `PasswordRecoveryE2ETests.cs`; In-process: `PasswordRecoveryTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-007-S04 | Link de redefinição expirado (@negative) | In-process: `PasswordRecoveryTests.cs` | In-process | — | GAP-UI: só em processo |
| US-007-S05 | Link de redefinição já utilizado (@negative) | E2E-UI (por comentário): `PasswordRecoveryE2ETests.cs`; In-process: `PasswordRecoveryTests.cs` | E2E-UI (por comentário) + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-007-S06 | Nova senha que não cumpre a política (@negative) | E2E-UI (por comentário): `PasswordRecoveryE2ETests.cs`; In-process: `PasswordRecoveryTests.cs` | E2E-UI (por comentário) + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-007-S07 | Confirmação de senha diferente (@negative) | E2E-UI (por comentário): `PasswordRecoveryE2ETests.cs`; In-process: `PasswordRecoveryTests.cs` | E2E-UI (por comentário) + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-008-S01 | Salvar um rascunho completo (@happy) | E2E-UI: `DraftE2ETests.cs`; In-process: `AdDraftServiceTests.cs`, `DraftTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-008-S02 | Adicionar fotos ao anúncio (@happy) | E2E-UI: `PhotosE2ETests.cs`; In-process: `PhotosEndpointsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-008-S03 | Trocar a capa e remover uma foto (@happy) | E2E-UI: `PhotosE2ETests.cs`; In-process: `PhotosEndpointsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-008-S04 | Passar do limite de 20 fotos (@negative) | E2E-UI: `PhotosE2ETests.cs`; In-process: `PhotosEndpointsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-008-S05 | Enviar um arquivo que não é foto aceita (@negative) | E2E-UI: `PhotosE2ETests.cs`; In-process: `PhotosEndpointsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-008-S06 | Falha ao enviar uma foto (@negative) | E2E-UI: `PhotosE2ETests.cs`; In-process: `PhotosEndpointsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-008-S07 | Salvar um rascunho só com o título (@edge) | E2E-UI: `PhotosE2ETests.cs`; In-process: `AdDraftServiceTests.cs`, `AdEntityTests.cs`, `DraftTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-008-S08 | Salvar sem título (@negative) | E2E-UI: `DraftE2ETests.cs`; In-process: `AdDraftServiceTests.cs`, `AdEntityTests.cs`, `DraftTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-008-S09 | Campos mudam conforme a categoria (@edge) | E2E-UI: `DraftE2ETests.cs`; In-process: `AdDraftServiceTests.cs`, `DraftTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-008-S10 | Redator tenta editar anúncio de outro redator (@negative) | In-process: `AdDraftServiceTests.cs`, `AdServiceTests.cs`, `DraftTests.cs` | In-process | — | GAP-UI: só em processo |
| US-008-S11 | Corrigir um anúncio rejeitado (@edge) | In-process: `AdDraftServiceTests.cs`, `DraftTests.cs` | In-process | — | GAP-UI: só em processo |
| US-008-S12 | Anúncio em revisão não pode ser editado pelo Redator (@edge) | In-process: `AdDraftServiceTests.cs`, `DraftTests.cs` | In-process | — | GAP-UI: só em processo |
| US-008-S13 | Administrador corrige o preço de um anúncio publicado (@edge) | In-process: `AdDraftServiceTests.cs`, `DraftTests.cs`, `PublishedEditTests.cs` | In-process | — | GAP-UI: só em processo |
| US-008-S16 | Administrador não remove a última foto de um anúncio publicado (@negative) | In-process: `PublishedPhotoTests.cs` | In-process | — | GAP-UI: só em processo |
| US-008-S15 | Administrador não salva um anúncio publicado que ficaria incompleto (@negative) | In-process: `PublishedEditTests.cs` | In-process | — | GAP-UI: só em processo |
| US-008-S14 | Serviço de CEP fora do ar (@negative) | E2E-UI: `DraftE2ETests.cs`; In-process: `DraftTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-009-S01 | Enviar um rascunho completo para revisão (@happy) | E2E-UI: `SubmitForReviewE2ETests.cs`; In-process: `SubmitForReviewTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-009-S02 | Enviar um rascunho incompleto (@negative) | E2E-UI: `SubmitForReviewE2ETests.cs`; In-process: `SubmitForReviewTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-009-S03 | Faltam características obrigatórias da categoria (@negative) | E2E-UI (por comentário): `SubmitForReviewE2ETests.cs`; In-process: `SubmitForReviewTests.cs` | E2E-UI (por comentário) + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-009-S04 | Reenviar um anúncio rejeitado depois de corrigi-lo (@edge) | E2E-UI: `Checkpoint4E2ETests.cs`; In-process: `AdServiceTests.cs`, `SubmitForReviewTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-009-S05 | Clicar duas vezes em enviar (@edge) | E2E-UI: `SubmitForReviewE2ETests.cs`; Integration: `AdServiceConcurrencyTests.cs`, `SubmitForReviewConcurrencyTests.cs`; In-process: `AdServiceTests.cs`, `SubmitForReviewTests.cs` | E2E-UI + Integration + In-process | 3 | PASS (E2E no artefato) |
| US-010-S01 | Ver a fila de revisão (@happy) | In-process: `ReviewQueueTests.cs` | In-process | — | GAP-UI: só em processo |
| US-010-S02 | Pré-visualizar um anúncio antes de decidir (@happy) | In-process: `ReviewQueueTests.cs`, `ReviewTests.cs` | In-process | — | GAP-UI: só em processo |
| US-010-S03 | Publicar um anúncio (@happy) | E2E-UI: `ReviewDecisionE2ETests.cs`; In-process: `ReviewTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-010-S04 | Rejeitar um anúncio com motivo (@happy) | E2E-UI: `ReviewDecisionE2ETests.cs`; In-process: `ReviewTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-010-S05 | Rejeitar sem informar o motivo (@negative) | E2E-UI: `ReviewDecisionE2ETests.cs`; In-process: `ReviewTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-010-S06 | Fila de revisão vazia (@edge) | In-process: `ReviewQueueTests.cs` | In-process | — | GAP-UI: só em processo |
| US-010-S07 | Dois administradores decidem o mesmo anúncio (@edge) | E2E-UI: `ReviewDecisionE2ETests.cs`; Integration: `ReviewDecisionConcurrencyTests.cs`; In-process: `DecisionRetryTests.cs`, `ReviewTests.cs` | E2E-UI + Integration + In-process | 3 | PASS (E2E no artefato) |
| US-010-S08 | Publicar sem o telefone do site configurado (@negative) | E2E-UI: `ReviewDecisionE2ETests.cs`; In-process: `PermissionMatrixTests.cs`, `ReviewTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-010-S09 | Redator não pode revisar anúncios (@negative) | In-process: `ReviewQueueTests.cs` | In-process | — | GAP-UI: só em processo |
| US-011-S01 | Despublicar um anúncio (@happy) | E2E-UI: `TakedownE2ETests.cs`; In-process: `TakedownTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-011-S02 | Arquivar um anúncio publicado (@happy) | E2E-UI (por comentário): `FavoritesE2ETests.cs`, `TakedownE2ETests.cs`; In-process: `TakedownTests.cs` | E2E-UI (por comentário) + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-011-S03 | Cancelar a confirmação (@edge) | E2E-UI: `TakedownE2ETests.cs`; In-process: `TakedownTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-011-S04 | Anúncio arquivado some dos favoritos do visitante (@edge) | E2E-UI: `FavoritesE2ETests.cs` | E2E-UI | 3 | PASS (E2E no artefato) |
| US-011-S05 | Arquivar um anúncio que ainda não foi publicado (@edge) | E2E-UI: `TakedownE2ETests.cs`; In-process: `ReviewQueueTests.cs`, `TakedownTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-011-S06 | Anúncio arquivado não tem ações de retirada (@negative) | E2E-UI (por comentário): `FavoritesE2ETests.cs`; In-process: `TakedownTests.cs` | E2E-UI (por comentário) + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-011-S07 | Redator não vê as ações de retirada (@negative) | E2E-UI (por comentário): `FavoritesE2ETests.cs`; In-process: `TakedownTests.cs` | E2E-UI (por comentário) + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-012-S01 | Redator vê apenas os próprios anúncios (@happy) | Integration: `PanelAdListQueryTests.cs`; In-process: `PanelAdListTests.cs` | Integration + In-process | — | GAP-UI: só em processo |
| US-012-S02 | Administrador vê todos os anúncios com o autor (@happy) | In-process: `PanelAdListTests.cs` | In-process | — | GAP-UI: só em processo |
| US-012-S03 | Filtrar por situação (@happy) | E2E-UI: `PanelAdListE2ETests.cs`; Integration: `PanelAdListQueryTests.cs`; In-process: `PanelAdListTests.cs` | E2E-UI + Integration + In-process | 3 | PASS (E2E no artefato) |
| US-012-S04 | Buscar um anúncio pelo título no painel (@happy) | E2E-UI (por comentário): `PanelAdListE2ETests.cs`; Integration: `PanelAdListQueryTests.cs`; In-process: `PanelAdListTests.cs` | E2E-UI (por comentário) + Integration + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-012-S05 | Abrir um anúncio da lista (@happy) | E2E-UI (por comentário): `PanelAdListE2ETests.cs`; In-process: `PanelAdListTests.cs` | E2E-UI (por comentário) + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-012-S06 | Redator ainda sem anúncios (@edge) | In-process: `PanelAdListTests.cs` | In-process | — | GAP-UI: só em processo |
| US-012-S07 | Lista com mais de 20 anúncios (@edge) | E2E-UI: `PanelAdListE2ETests.cs`; Integration: `PanelAdListQueryTests.cs`; In-process: `PanelAdListTests.cs` | E2E-UI + Integration + In-process | 3 | PASS (E2E no artefato) |
| US-012-S08 | Falha ao carregar a lista (@negative) | In-process: `PanelAdListTests.cs`, `StubPanelAdListRepository.cs` | In-process | — | GAP-UI: só em processo |
| US-013-S01 | Criar uma subcategoria (@happy) | E2E-UI: `CategoriesE2ETests.cs`; In-process: `CategoriesTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-013-S02 | Criar uma categoria principal (@happy) | E2E-UI: `CategoriesE2ETests.cs`; In-process: `CategoriesTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-013-S03 | Renomear uma categoria (@happy) | E2E-UI (por comentário): `CategoriesE2ETests.cs`; In-process: `CategoriesTests.cs` | E2E-UI (por comentário) + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-013-S04 | Mudar a ordem das categorias (@happy) | E2E-UI: `CategoriesE2ETests.cs`; In-process: `OrderingTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-013-S05 | Excluir uma categoria vazia (@happy) | E2E-UI (por comentário): `CategoriesE2ETests.cs`; In-process: `DeletionTests.cs` | E2E-UI (por comentário) + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-013-S06 | Nome de categoria repetido (@negative) | E2E-UI: `CategoriesE2ETests.cs`; In-process: `CategoriesTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-013-S07 | Nome de categoria vazio (@negative) | E2E-UI: `CategoriesE2ETests.cs`; In-process: `CategoriesTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-013-S08 | Excluir categoria que tem anúncios (@negative) | E2E-UI (por comentário): `CategoriesE2ETests.cs`; Integration: `AdsCategoryUsageTests.cs`; In-process: `DeletionTests.cs` | E2E-UI (por comentário) + Integration + In-process | 3 | PROVÁVEL: E2E cita a história e o cenário no arquivo, sem marca no teste |
| US-013-S09 | Excluir categoria que tem subcategorias (@negative) | E2E-UI: `CategoriesE2ETests.cs`; In-process: `DeletionTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-013-S10 | Categorias com características específicas não podem ser excluídas (@negative) | E2E-UI: `CategoriesE2ETests.cs`; In-process: `DeletionTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-013-S11 | Categorias têm até três níveis (@edge) | E2E-UI: `CategoriesE2ETests.cs`; In-process: `CategoriesTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-014-S01 | Criar uma conta de Redator (@happy) | In-process: `UsersTests.cs` | In-process | — | GAP-UI: só em processo |
| US-014-S02 | Mudar o papel de um usuário (@happy) | In-process: `UsersTests.cs` | In-process | — | GAP-UI: só em processo |
| US-014-S03 | Desativar uma conta (@happy) | E2E-UI: `UsersE2ETests.cs`; In-process: `UsersTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-014-S04 | Reativar uma conta (@happy) | In-process: `UsersTests.cs` | In-process | — | GAP-UI: só em processo |
| US-014-S05 | E-mail já cadastrado (@negative) | In-process: `UsersTests.cs` | In-process | — | GAP-UI: só em processo |
| US-014-S06 | Senha provisória fraca (@negative) | In-process: `ErrorDescriberTests.cs`, `UsersTests.cs` | In-process | — | GAP-UI: só em processo |
| US-014-S07 | E-mail em formato inválido (@negative) | In-process: `UsersTests.cs` | In-process | — | GAP-UI: só em processo |
| US-014-S08 | Desativar a própria conta (@negative) | In-process: `UsersTests.cs` | In-process | — | GAP-UI: só em processo |
| US-014-S09 | Remover o último administrador (@negative) | Integration: `ConcurrencyTests.cs`; In-process: `UsersTests.cs` | Integration + In-process | — | GAP-UI: só em processo |
| US-014-S10 | Redefinir a senha de alguém da equipe (@happy) | E2E-UI: `UsersE2ETests.cs`; In-process: `UsersTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-015-S01 | Definir o telefone/WhatsApp do site (@happy) | E2E-UI: `SettingsE2ETests.cs`; In-process: `SettingsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-015-S02 | Trocar o número em uso (@happy) | E2E-UI: `SettingsE2ETests.cs`; In-process: `SettingsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-015-S03 | Número digitado sem formatação (@edge) | E2E-UI: `SettingsE2ETests.cs`; In-process: `SettingsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-015-S04 | Número inválido (@negative) | E2E-UI: `SettingsE2ETests.cs`; In-process: `SettingsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-015-S05 | Número vazio (@negative) | E2E-UI: `SettingsE2ETests.cs`; In-process: `SettingsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |
| US-015-S06 | Redator não acessa as configurações (@negative) | E2E-UI: `SettingsE2ETests.cs`; In-process: `SettingsTests.cs` | E2E-UI + In-process | 3 | PASS (E2E no artefato) |

**Coverage:** 66/130 cenários com teste de navegador marcado, no artefato (50%); 23 com E2E provável (arquivo cita o cenário, sem marca); 41 só com prova em processo (lista de lacunas abaixo). **Gate de rastreabilidade (Fase 5): NÃO atingiu 100%** — os 64 restantes precisam de marca/teste E2E-UI ou de uma dispensa assinada por você (o `/verify` não pode se dispensar sozinho).

## Lacunas (cenário sem teste de navegador marcado)

Cada uma tem prova em processo (coluna Layer), que roda no `dotnet test` e passou. O que falta é o mesmo cenário **no navegador, no artefato**. Dono do próximo passo: `/test` (marcar os E2E que já existem e escrever os que faltam); registrado no BACKLOG como item V-04.

- `US-001-S01` — Página inicial mostra categorias e anúncios recentes — In-process — só em processo
- `US-001-S02` — Entrar em uma categoria principal — In-process — só em processo
- `US-001-S04` — Categoria sem anúncios publicados — In-process — só em processo
- `US-001-S05` — Site ainda sem nenhum anúncio publicado — In-process — só em processo
- `US-001-S06` — Falha ao carregar a página inicial — In-process — só em processo
- `US-002-S02` — Combinar categoria, localização e preço — E2E-UI (por comentário) + Integration + In-process — PROVÁVEL (E2E cita)
- `US-002-S04` — Filtrar terrenos, sítios e fazendas por área — E2E-UI (por comentário) + Integration + In-process — PROVÁVEL (E2E cita)
- `US-002-S05` — Ordenar os resultados — E2E-UI (por comentário) + Integration + In-process — PROVÁVEL (E2E cita)
- `US-002-S06` — Paginar os resultados — Integration + In-process — só em processo
- `US-002-S11` — Falha ao buscar — In-process — só em processo
- `US-003-S01` — Abrir um anúncio completo — In-process — só em processo
- `US-003-S04` — Anúncio de categoria sem ficha de veículo nem de terreno — In-process — só em processo
- `US-003-S06` — Abrir um anúncio que não está mais disponível — In-process — só em processo
- `US-004-S02` — Ligar a partir de um anúncio — E2E-UI (por comentário) + In-process — PROVÁVEL (E2E cita)
- `US-004-S03` — O contato é visível sem login — E2E-UI (por comentário) + In-process — PROVÁVEL (E2E cita)
- `US-005-S02` — Favoritar e desfavoritar pela página do anúncio — E2E-UI (por comentário) + In-process — PROVÁVEL (E2E cita)
- `US-005-S04` — Remover um anúncio da página Meus favoritos — E2E-UI (por comentário) — PROVÁVEL (E2E cita)
- `US-005-S05` — Lista de favoritos vazia — E2E-UI (por comentário) + In-process — PROVÁVEL (E2E cita)
- `US-005-S08` — Favoritos não acompanham o visitante em outro aparelho — E2E-UI (por comentário) — PROVÁVEL (E2E cita)
- `US-006-S01` — Redator entra no painel — In-process — só em processo
- `US-006-S02` — Administrador entra no painel — In-process — só em processo
- `US-006-S04` — E-mail ou senha incorretos — In-process — só em processo
- `US-006-S05` — Conta desativada — In-process — só em processo
- `US-006-S06` — Muitas tentativas de entrada — In-process — só em processo
- `US-006-S07` — Abrir uma página do painel sem estar logado — In-process — só em processo
- `US-006-S09` — Primeiro acesso exige trocar a senha provisória — In-process — só em processo
- `US-006-S10` — Redator tenta abrir uma página exclusiva do administrador — In-process — só em processo
- `US-007-S01` — Pedir a redefinição de senha — E2E-UI (por comentário) + In-process — PROVÁVEL (E2E cita)
- `US-007-S02` — Definir uma nova senha pelo link — E2E-UI (por comentário) + In-process — PROVÁVEL (E2E cita)
- `US-007-S04` — Link de redefinição expirado — In-process — só em processo
- `US-007-S05` — Link de redefinição já utilizado — E2E-UI (por comentário) + In-process — PROVÁVEL (E2E cita)
- `US-007-S06` — Nova senha que não cumpre a política — E2E-UI (por comentário) + In-process — PROVÁVEL (E2E cita)
- `US-007-S07` — Confirmação de senha diferente — E2E-UI (por comentário) + In-process — PROVÁVEL (E2E cita)
- `US-008-S10` — Redator tenta editar anúncio de outro redator — In-process — só em processo
- `US-008-S11` — Corrigir um anúncio rejeitado — In-process — só em processo
- `US-008-S12` — Anúncio em revisão não pode ser editado pelo Redator — In-process — só em processo
- `US-008-S13` — Administrador corrige o preço de um anúncio publicado — In-process — só em processo
- `US-008-S16` — Administrador não remove a última foto de um anúncio publicado — In-process — só em processo
- `US-008-S15` — Administrador não salva um anúncio publicado que ficaria incompleto — In-process — só em processo
- `US-009-S03` — Faltam características obrigatórias da categoria — E2E-UI (por comentário) + In-process — PROVÁVEL (E2E cita)
- `US-010-S01` — Ver a fila de revisão — In-process — só em processo
- `US-010-S02` — Pré-visualizar um anúncio antes de decidir — In-process — só em processo
- `US-010-S06` — Fila de revisão vazia — In-process — só em processo
- `US-010-S09` — Redator não pode revisar anúncios — In-process — só em processo
- `US-011-S02` — Arquivar um anúncio publicado — E2E-UI (por comentário) + In-process — PROVÁVEL (E2E cita)
- `US-011-S06` — Anúncio arquivado não tem ações de retirada — E2E-UI (por comentário) + In-process — PROVÁVEL (E2E cita)
- `US-011-S07` — Redator não vê as ações de retirada — E2E-UI (por comentário) + In-process — PROVÁVEL (E2E cita)
- `US-012-S01` — Redator vê apenas os próprios anúncios — Integration + In-process — só em processo
- `US-012-S02` — Administrador vê todos os anúncios com o autor — In-process — só em processo
- `US-012-S04` — Buscar um anúncio pelo título no painel — E2E-UI (por comentário) + Integration + In-process — PROVÁVEL (E2E cita)
- `US-012-S05` — Abrir um anúncio da lista — E2E-UI (por comentário) + In-process — PROVÁVEL (E2E cita)
- `US-012-S06` — Redator ainda sem anúncios — In-process — só em processo
- `US-012-S08` — Falha ao carregar a lista — In-process — só em processo
- `US-013-S03` — Renomear uma categoria — E2E-UI (por comentário) + In-process — PROVÁVEL (E2E cita)
- `US-013-S05` — Excluir uma categoria vazia — E2E-UI (por comentário) + In-process — PROVÁVEL (E2E cita)
- `US-013-S08` — Excluir categoria que tem anúncios — E2E-UI (por comentário) + Integration + In-process — PROVÁVEL (E2E cita)
- `US-014-S01` — Criar uma conta de Redator — In-process — só em processo
- `US-014-S02` — Mudar o papel de um usuário — In-process — só em processo
- `US-014-S04` — Reativar uma conta — In-process — só em processo
- `US-014-S05` — E-mail já cadastrado — In-process — só em processo
- `US-014-S06` — Senha provisória fraca — In-process — só em processo
- `US-014-S07` — E-mail em formato inválido — In-process — só em processo
- `US-014-S08` — Desativar a própria conta — In-process — só em processo
- `US-014-S09` — Remover o último administrador — Integration + In-process — só em processo
