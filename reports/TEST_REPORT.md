# Relatório de testes — Fase 1 (tarefas 1.1 a 1.4) e Fase 2 (em andamento)

> **Em resumo:** os 322 testes passam (283 unitários, 22 de integração em SQL Server real, 17 de navegador). Os dois testes de corrida agora usam uma barreira e falham de verdade quando a transação deixa de ser serializável. Veredito: **aprovado**.

## Resultado das execuções (saídas reais dos executores)

| Camada | Comando | Total | Passaram | Falharam |
|---|---|---|---|---|
| Unitários (SQLite em memória) | `dotnet run --project tests/GazetaMarketplace.Web.Tests` | 283 | 283 | 0 |
| Integração (SQL Server 2022 em contêiner) | `dotnet test --project tests/GazetaMarketplace.IntegrationTests --filter "TestCategory=Integration"` | 22 | 22 | 0 |
| E2E (Playwright, site publicado em Production) | `dotnet run --project tests/GazetaMarketplace.Web.Tests.Playwright` | 17 | 17 | 0 |

## Correções feitas nesta rodada

| # | Achado | Correção |
|---|---|---|
| 1 | `UsersE2ETests` usava `Context.NewPageAsync()`, que divide os cookies com a página do Administrador | Contexto de navegador separado (`Browser.NewContextAsync`) |
| 2 | O script não ligava `QUOTED_IDENTIFIER`; o `sqlcmd` sem `-I` falhava no erro 1934 | `SET QUOTED_IDENTIFIER ON;` no topo, gerado por `db/scripts/gerar-script.sh`; testes: unitário (SET no topo) e de integração (sessão iniciada em `OFF`) |
| 3 | Parâmetro SQL com nome em português | `PublishedStatusParameter = "PublishedStatus"` |
| 4 | O teste de corrida de troca de papel passava mesmo com `ReadCommitted` | Barreira (`Barrier(2)`) que solta as duas operações juntas |

## Verificação por mutação

| Mutação | Esperado | Obtido |
|---|---|---|
| Remover o `SET QUOTED_IDENTIFIER ON;` do script | teste de integração falha | falhou (`CREATE INDEX failed ... 'QUOTED_IDENTIFIER'`) |
| `IsolationLevel.Serializable` → `ReadCommitted` em `UserManagement` | os dois testes de corrida falham | **os dois falharam** (rebaixamento: rodada 7; desativação: rodada 1). Antes da barreira só o de desativação falhava |

As duas mutações foram desfeitas (`git diff` limpo nesses arquivos).

## Pendências registradas

- Telas e CSS: ver `plans/BACKLOG.md`.
- Cobertura numérica (80%/75%) não foi medida nesta rodada; fica para o `/review`.
- Os testes de CSS (`BaseCssTests`) só valem contra a saída publicada (documentado em `docs/RODAR-TESTES-DE-INTEGRACAO-E-E2E.md`).

## Tarefa 1.4 — recuperar senha por e-mail (US-007)

> **Em resumo:** 337 testes unitários, 25 de integração (SQL Server real) e 20 de navegador passam. As quatro mutações planejadas derrubam testes.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários | 337 | 337 | 0 |
| Integração (SQL Server 2022 em contêiner) | 25 | 25 | 0 |
| E2E (Playwright, site publicado em Production, SendGrid de mentira local) | 20 | 20 | 0 |

| Mutação | Testes que caíram |
|---|---|
| Tirar o limite de 3 pedidos por hora por e-mail | `QuartoPedidoNaMesmaHora_NaoEnviaEmail_MasRespondeIgual`, `DepoisDeUmaHora_OLimitePorEmailLibera` |
| Tirar a checagem de conta ativa no envio | `ContaDesativada_NaoRecebeEmailDeRedefinicao` |
| Deixar o token valer duas vezes (ignorar o carimbo de segurança) | `US007S05_LinkDeRedefinicaoJaUtilizado`, `Token_QueVirouUsado_NaoValeDeNovo`, `TokenDeRedefinicao_PodeSerGerado_ESoValeUmaVez` |
| Deixar o corpo do e-mail (com o link) no log de Production | `EmProduction_OLogTemSoODestinatarioMascarado_SemOLink`, `EnviaPost_ParaApiV3_ComBearer_SemVazarChaveNoLog` |

Todas foram desfeitas depois. No log do site em Production (arquivo JSON do E2E) não aparece `code=` nem o link; só o caminho da requisição, sem consulta.

**Problema de ambiente resolvido:** com mais de 300 hosts de teste, o limite de 128 instâncias de inotify do Linux estourava (testes aleatórios falhavam com `IOException`). `TestHostSettings` desliga a recarga automática de configuração nos hosts de teste.

## Tarefa 2.1 — árvore de categorias e carga inicial

> **Em resumo:** 411 testes unitários, 31 de integração (SQL Server real) e 20 de navegador passam. As seis mutações derrubam testes. Não há tela nesta tarefa, então não há E2E novo; os 20 existentes rodaram de novo sem regressão.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários | 411 | 411 | 0 |
| Integração (SQL Server 2022 em contêiner) | 31 | 31 | 0 |
| E2E (Playwright, site publicado em Production, banco com a carga das 147 categorias) | 20 | 20 | 0 |

| Mutação | Testes que caíram |
|---|---|
| Cache da árvore não invalida ao gravar | `EditarCategoria_InvalidaOCache`, `CriarEExcluirCategoria_TambemInvalidam` |
| Profundidade máxima fora por um | `QuartoNivel_E_Recusado` |
| Slug sem sufixo de colisão | 9 testes (`SlugGeneratorTests` e `Slugs_SaoOsExplicitosDoArquivo_ENosDemais_OsGeradosPelaRegra`) |
| `Descendants` só até o 2º nível | `Descendentes_IncluemTodosOsNiveis`, `Descendentes_VemEmOrdemDeExibicao_PaiAntesDasFilhas`, `Autopecas_EstaNoTerceiroNivel` |
| Autopeças marcada como postável na carga | 6 testes (`ParityTests`, `InitialLoadTests`, snapshot do modelo) |
| Regra do slug invertida (não postável ganha o limpo) | `AssignInitial_OsQuatroCasosDoProductOwner_PostavelGanhaOLimpo`, `Slugs_SaoOsExplicitosDoArquivo_ENosDemais_OsGeradosPelaRegra` |

Todas foram desfeitas depois. Um bug real apareceu na primeira rodada de testes: `Dictionary` não aceita chave nula, e o primeiro nível tem `ParentId` nulo; corrigido com uma lista separada de raízes antes de qualquer commit.

**Honestidade sobre o método:** nesta tarefa os testes foram escritos junto com o código, e não antes dele; o que prova que eles discriminam são as seis mutações acima.

## Tarefa 2.2 — framework de grupos de campos e 4 grupos (Serviços, Vagas, Produtos em geral, Imóveis)

> **Em resumo:** 436 testes unitários, 32 de integração (SQL Server real) e 20 de navegador passam. As cinco mutações derrubam testes. Não há tela nesta tarefa, então não há E2E novo; os 20 existentes rodaram de novo sem regressão.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários | 436 | 436 | 0 |
| Integração (SQL Server 2022 em contêiner) | 32 | 32 | 0 |
| E2E (Playwright, site publicado em Production) | 20 | 20 | 0 |

| Mutação | Testes que caíram |
|---|---|
| Serviços com preço | `Servicos_SemPreco_6Fotos_Tipo11Opcoes`, `TodoGrupo_TemChavesUnicasDeCampo_ELimitesCoerentes` |
| Vagas com fotos | `Vagas_SemFotos_Titulo90_14Areas_PrecoViraSalario` |
| Herança pega o primeiro ancestral, não o mais próximo | `GrupoProprioVenceOHerdado`, `GrupoDeCampos_ProprioVenceOHerdado_ESemNenhumDevolveNulo` |
| Id de lista deslocado | `IdsDasListas_SaoOsDoGazetaOnline`, `FieldList_BuscaPorId` |
| Quartos aplicado às categorias erradas | `Imoveis_ObrigatoriosPorCategoria`, `Imoveis_CamposQueExistemEmCadaCategoria`, `CamposDaCategoria_VemDoGrupoResolvido` |

Todas foram desfeitas depois. As listas (`FieldLists`) e o mapa categoria → grupo existem em duas representações (documentos e código); `ListsTests` relê `gazetaonline-lookups.md` e o Apêndice B da SPEC, e `ParityTests` confere o mapa contra a tabela da SPEC escrita à mão no teste.

## Tarefa 2.3 — Carros, Motos, Caminhões e ônibus, Barcos e aeronaves, Peças

> **Em resumo:** 462 testes unitários, 32 de integração (SQL Server real) e 20 de navegador passam. As seis mutações derrubam testes. Não há tela nesta tarefa, então não há E2E novo; os 20 existentes rodaram de novo sem regressão.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários | 462 | 462 | 0 |
| Integração (SQL Server 2022 em contêiner) | 32 | 32 | 0 |
| E2E (Playwright, site publicado em Production) | 20 | 20 | 0 |

| Mutação | Testes que caíram |
|---|---|
| Quilometragem deixa de ser obrigatória em Carros | `Carros_Obrigatorios_MarcaModeloAnoVersaoKm` |
| Horas de uso deixa de ser obrigatória em Barcos | `Barcos_HorasDeUso_NoLugarDeKm` |
| Grupo `Parts` não gravado em Autopeças | 5 testes (herança das peças, `ParityTests`, carga, snapshot do modelo) |
| Ônibus com a lista de Tipo de Caminhões | `CaminhoesOnibus_AnoEKm_Obrigatorios_ListasMudamEntreAsDuas` |
| Filtro específico em Barcos | `Barcos_Pecas_Servicos_Vagas_EProdutosEmGeral_NaoTemFiltroEspecifico` |
| Teto do ano do modelo sem o +1 | `Lista_Em2026_VaiDe2027Ate1951_Mais1950OuAnterior`, `Lista_NaViradaDeAno_GanhaUmAnoNoTopo`, `Validacao_AceitaDe1950AoAnoAtualMais1_UsandoORelogioDoSite` |

Todas foram desfeitas depois. As 25 listas novas relêem `gazetaonline-lookups.md` nos três formatos do documento (tabela, linha única com a contagem declarada e a regra dos anos gerados). Os testes de ano do modelo congelam o relógio e provam a virada de 31/12 para 1º/1 no fuso de São Paulo.

## Tarefa 2.7 — telefone/WhatsApp do site (US-015)

> **Em resumo:** 519 testes unitários, 35 de integração (SQL Server real) e 27 de navegador passam. As cinco mutações derrubam testes. A tela `/painel/configuracoes` é nova, então entram 7 testes de navegador.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários | 519 | 519 | 0 |
| Integração (SQL Server 2022 em contêiner) | 35 | 35 | 0 |
| E2E (Playwright, site publicado em Production) | 27 | 27 | 0 |

| Mutação | Testes que caíram |
|---|---|
| Salvar sem invalidar o cache | 14 (S01, S02, S03, S04, S05, S06 por POST direto e o aviso de "não configurado") |
| Normalização frouxa (aceita DDD fora da lista) | 6 (`PhoneNumberTests`, S04) |
| Salvar sem gravar a auditoria | `SalvarAuditaOAnteriorEONovo_ESoQuandoMuda` |
| Perder o valor anterior na auditoria | `SalvarAuditaOAnteriorEONovo_ESoQuandoMuda` |
| Redator passa na política da tela | `US015S06_RedatorNaoAcessaAsConfiguracoes`, `US015S06_RedatorNaoConsegueSalvarNemPorPostDireto` |

Todas foram desfeitas depois. Duas tentativas de mutação (remover `Invalidate()` e remover a auditoria) não compilavam por causa do aviso de parâmetro não lido; foram refeitas de modo que compilam, e os resultados acima são só os das versões que compilaram.

**O que cada camada prova**

- Unitários: o telefone (formatos aceitos e recusados, os 67 DDDs um a um, formatação de ida e volta), os seis cenários pelo site, a auditoria (anterior e novo, sem gravar quando não muda), o cache de 10 minutos (9min59 ainda vale, 10min relê) e a migration no script.
- Integração: chave única no SQL Server (erro 2601), tamanho das colunas, fluxo completo pela tela com auditoria, e duas pessoas criando a primeira configuração juntas (oito rodadas, sem duplicar a linha nem perder auditoria).
- E2E: salvar sem formatação e ver o número formatado depois de recarregar, trocar com `+55`, número inválido e vazio (o valor guardado não muda), Redator barrado, Axe sem violações, Enter envia o formulário e o fluxo sem JavaScript.

**Achados desta rodada**

- A migration `AddSiteSettings` e o script idempotente foram gerados juntos; `ScriptTests` agora espera sete migrations.
- Dois ou mais envios simultâneos para a primeira configuração podem acabar em duas gravações em sequência (a segunda troca a primeira) ou em um conflito avisado; as duas saídas são corretas, e o teste confere as invariantes (uma linha, uma auditoria por gravação, última auditoria igual ao valor guardado).
- O limite de 128 instâncias de inotify do Linux voltou a derrubar testes ao acaso quando a suíte cresceu; ver `plans/BACKLOG.md`.
- A suíte E2E tem limites de pedidos por IP (100/min global, 10/h na recuperação de senha); rodar duas vezes seguidas no mesmo banco falha em `US007` até limpar `PasswordRecoveryAttempts`.

## Tarefa 2.5 — catálogo de veículos: tabelas, consulta encadeada e ferramenta de exportação (ADR-008)

> **Em resumo:** 553 testes unitários, 43 da ferramenta, 50 de integração (SQL Server real) e 27 de navegador passam. As sete mutações derrubam testes. Não há tela nesta tarefa; os 27 E2E rodaram de novo contra o site publicado e o endpoint novo respondeu nele. **A exportação real do GazetaOnline não foi feita** (A5 e acesso somente leitura pendentes); tudo foi provado com um catálogo reduzido de teste.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site | 553 | 553 | 0 |
| Ferramenta de exportação (`tests/VehicleCatalogExport.Tests`) | 43 | 43 | 0 |
| Integração (SQL Server 2022 em contêiner) | 50 | 50 | 0 |
| E2E (Playwright, site publicado em Production) | 27 | 27 | 0 |

**Catálogo reduzido de teste** (`tests/VehicleCatalogExport.Tests/Data/sample-catalog.sql`, origem simulada): 10 marcas (Honda, Toyota, Volkswagen, Fiat, Chevrolet em carros; Honda, Yamaha, Kawasaki, Suzuki, BMW em motos), 45 modelos, 214 anos, 307 versões. Cobre marca com vários modelos, modelo com vários anos, ano com várias versões, marca sem modelos (Suzuki), modelo sem anos (Etios), modelo com anos e nenhuma versão (Mobi), ano sem versões (Civic 2016), nome com apóstrofo e acento, e **ids que colidem entre carros e motos** (Honda = 1 nos dois, modelos 1 a 17 nos dois, versões 1 a 124 nos dois).

| Mutação | Testes que caíram |
|---|---|
| Anos em ordem crescente | `Anos_EmOrdemDecrescente` |
| Marca inexistente devolve lista vazia em vez de 404 | 3 (404 de marca, 404 do outro tipo, cache não envenenado) |
| `Invalidate()` não esvazia o cache | `Invalidate_ForcaReleituraNaHora` |
| Cache sem o tipo na chave | 3 (cache por consulta, Honda de carro × moto, 404 do outro tipo) |
| Guarda de produção aceita qualquer ambiente | 6 (5 ambientes inválidos e a recusa antes de tocar na origem) |
| Guarda ignora a cadeia marcada como produção | 4 |
| `MERGE` que nunca casa (duplicaria na segunda carga) | 1 de ferramenta (texto da chave) e 5 de integração (script duas vezes, carga duas vezes, golden, nova origem, endpoints) |
| Leitor da origem passa a escrever (`DELETE`) | `LeitorDaOrigem_NaoTemNenhumComandoDeEscrita` |
| Órfão de modelo (marca inexistente) passa | 3 |

Todas foram desfeitas depois (o `MERGE` foi mutado duas vezes, uma em cada camada).

**O que cada camada prova**

- Unitários: ordem (marcas, modelos e versões alfabéticos; anos decrescentes), 200 com lista vazia nas bordas, 404 e 400, o tipo (`kind`) em toda consulta, ids de carro e moto iguais sem se misturar, cabeçalho de cache de 10 minutos, cache de 10 minutos (9min59 vale, 10 relê), `Invalidate`, trocar o `Source` não muda a consulta nem aparece na resposta, e as rotas do controlador conferem com o `openapi.yaml`.
- Ferramenta: órfãos descartados em cascata e relatados; repetições; script com ordem, lotes de 500, aspas duplicadas, determinismo e sem `DELETE`; recusa de produção antes de tocar na origem; variável ausente e conexão recusada não criam arquivo nem pasta; o leitor da origem não tem nenhum comando de escrita; nenhuma credencial na ferramenta, no seed e nos dados de teste.
- Integração: exportação do catálogo reduzido (contagens, zero descartes, **igual ao script versionado** em `db/seed/sample/`); órfãos relatados; script aplicado duas vezes sem mudar nenhuma linha; carga em lote igual ao script, duas vezes sem duplicar, desfeita por inteiro quando falha no meio, recusada em produção sem gravar nada; nova origem atualiza `Source` e corrige um nome corrompido; chaves compostas aceitam o mesmo id nos dois tipos e a chave estrangeira recusa pai de outro tipo; endpoints sobre o SQL Server real; script das migrations e migrations dão o mesmo esquema.

**Achados desta rodada**

- **Esquema da origem presumido.** O banco do GazetaOnline nunca foi lido e não há esquema no repositório; não foi possível confirmar se os ids de carros e motos são globalmente únicos. A chave composta (Id, Kind) fica, e `OriginReader.cs` é o único arquivo a ajustar quando houver acesso (BACKLOG).
- **O contrato mudou:** como o id não diz o tipo, os três endpoints filhos passaram a exigir `kind`. `openapi.yaml` e ADR-008 foram atualizados.
- **Colisão de nome `Program`:** a ferramenta tinha instruções de nível superior, o que criava um segundo tipo `Program` e quebrava o `WebApplicationFactory` do projeto de integração; o ponto de entrada virou a classe `ExportEntryPoint`.
- A geração do script de exemplo com a ferramenta de verdade (contra um SQL Server) e o teste que compara o resultado com o arquivo versionado fecham o ciclo: se o gerador mudar, o teste diz para gerar de novo (`db/seed/README.md`).
- Fumaça no site publicado: o script de exemplo aplicado duas vezes ao banco do E2E (segunda vez: 0 linhas afetadas) e os endpoints respondendo; as linhas foram removidas depois.

## Tarefa 2.6 — gerenciar categorias (US-013)

> **Em resumo:** 596 testes unitários, 43 da ferramenta de catálogo, 57 de integração (SQL Server real) e 36 de navegador passam. Das seis mutações planejadas, cinco derrubam testes e uma (a segunda chamada que esvazia o cache) **sobrevive por ser redundante de propósito**; a mutação que remove as duas chamadas derruba 15 testes. A tela `/painel/categorias` é nova: entram 9 testes de navegador.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site | 596 | 596 | 0 |
| Ferramenta (`VehicleCatalogExport.Tests`) | 43 | 43 | 0 |
| Integração (SQL Server 2022 em contêiner) | 57 | 57 | 0 |
| E2E (Playwright, site publicado em Production) | 36 | 36 | 0 |

| Mutação | Testes que caíram |
|---|---|
| Criar sem a checagem de nome repetido | `US013S06_NomeRepetido_EmQualquerCaixaOuAcento_ENoMesmoGrupo` (o índice único do banco só pega o nome idêntico; as variações de caixa e acento exigem a regra do serviço) |
| Aceitar o quarto nível (`MaxDepth = 4`) | 2 (`US013S11…`, `QuartoNivel_E_Recusado`) |
| Excluir ignorando as subcategorias | 2 (`US013S09…`, ordem dos bloqueios) |
| Mover sem restringir às irmãs | 5 |
| Renomear sem gravar a auditoria | `CriarRenomearReordenarExcluir_GravamAuditoria…` |
| Excluir sem gravar o valor anterior | `CriarRenomearReordenarExcluir_GravamAuditoria…` |
| Tirar a chamada `tree.Invalidate()` do serviço | **nenhum** — sobrevive: o `AppDbContext` já esvazia o cache dentro do `SaveChanges`, então a chamada do serviço (feita depois do commit) é uma segunda defesa |
| Tirar as duas chamadas (serviço e `AppDbContext`) | 15 (a árvore em cache deixa de refletir criar, renomear, mover e excluir) |

Todas foram desfeitas depois. Três mutações não compilavam de primeira (código inalcançável, parâmetro não lido) e foram refeitas; os números acima são só os das versões que compilaram.

**O que cada camada prova**

- Unitários: os onze cenários S01 a S11; auditoria com anterior e novo e sem rastro nas recusas e nas operações sem efeito; nome repetido por caixa e acento; nome vazio e longo; quarto nível forjado por POST; pai inexistente; subcategoria dentro de folha com e sem anúncios; slug único e inalterado ao renomear; ordem dos bloqueios; regra de proteção (exatamente as 12 categorias da carga com grupo próprio); mover só entre irmãs, empates, primeiro e último; Redator e anônimo barrados, inclusive por POST direto; antiforgery; 404 em todas as ações; listas aninhadas, nomes acessíveis, botões inativos nos extremos, janela de confirmação só para quem pode ser excluída; limite global configurável (e valores inválidos voltam a 100).
- Integração: fluxo completo com a identidade continuando em 156, auditoria e cache; duas pessoas criando o mesmo nome (6 rodadas); a mesma categoria principal (maiúscula e minúscula); dois movimentos juntos sem empate nem irmã perdida (6 rodadas); excluir o pai enquanto outra pessoa cria uma filha (6 rodadas, nunca filha sem pai); o banco recusa apagar pai com filhas por SQL direto (erro 547).
- E2E: criar principal, renomear e excluir pela janela; subcategoria e terceiro nível (a lista de pai não mostra o terceiro nível); mover com status e foco no botão usado, com a ordem original restaurada; nome repetido e vazio com `aria-invalid`; excluir com subcategorias (bloqueio depois de confirmar); campos específicos (bloqueio direto, sem janela, e renomear continua); janela com foco inicial em Cancelar, Esc e Cancelar sem excluir, foco devolvido ao botão e Axe com a janela aberta; Axe na lista e no formulário e sem rolagem horizontal em 320 px; fluxo inteiro sem JavaScript.

**Achados desta rodada**

1. **Bug pego pelo teste: `data-confirm=""`.** O atributo vazio ainda casa com o seletor `[data-confirm]`, então a categoria protegida abria a janela de confirmação em vez de ir direto à mensagem. O valor agora é `"true"`/`"false"` e o JavaScript seleciona `[data-confirm='true']`.
2. **Foco perdido depois de mover.** O endereço leva a âncora `#categoria-N` e o navegador move o foco para ela ao terminar de carregar, depois de o módulo rodar. O foco agora é devolvido depois do evento `load`.
3. **Contraste.** Os botões "Editar" e "Excluir" sobre o fundo da página (`#f1f5fd`) ficavam abaixo de 4,5:1 (Axe). A árvore passou a ficar dentro de um cartão branco.
4. **Limite global de pedidos.** A suíte completa passou de 100 pedidos por minuto de um IP só e começou a receber 429 em páginas de login. O limite ficou configurável (`RateLimiting:GlobalPerMinute`, padrão 100); só o site do E2E usa 1000. O limite de login e de recuperação de senha não mudou.
5. **Rolagem animada.** O Bootstrap rola até a âncora com animação e a lista tem ~150 itens; o Playwright via os botões "instáveis". Os E2E de categorias rodam com movimento reduzido (BACKLOG avalia `scroll-behavior: auto` nesta tela).
6. **SPEC S08 × A7 b.** O exemplo do S08 ("Motos tem 3 anúncios") bate com a proteção por campos específicos; ver BACKLOG.

## Tarefa 2.4 — nove grupos de campos que fecham o Apêndice B

> **Em resumo:** 619 testes unitários (23 novos), 43 da ferramenta de catálogo, 57 de integração (SQL Server real) e 36 de navegador passam. As seis mutações planejadas derrubam testes. A tarefa não tem tela nova, então não há teste de navegador novo: o E2E existente confirma que nada das telas de categorias quebrou com as 65 categorias protegidas.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site | 619 | 619 | 0 |
| Ferramenta (`VehicleCatalogExport.Tests`) | 43 | 43 | 0 |
| Integração (SQL Server 2022 em contêiner) | 57 | 57 | 0 |
| E2E (Playwright, site publicado em Production, script idempotente com 9 migrations) | 36 | 36 | 0 |

| Mutação | Testes que caíram |
|---|---|
| Celulares sem Marca obrigatória | 2 (`AppendixBTests.NoveGruposDa2_4…`, `Celulares_Marca…`) |
| Calçados infantis (75) com a lista de calçados adultos | `RoupasECalcados_TamanhoMudaPorCategoria…` |
| Categoria 110 (Eletrônicos) sem o grupo gravado | 4 (`AppendixBTests` ×2, `ParityTests`, `ModeloDoCodigo_EstaEmDiaComASnapshotDasMigrations`) |
| Capacidade fora do Ar-condicionado (128 e 129) | 2 (`AppendixBTests.CampoComRestricaoDeCategoria…`, `Eletro_Tipo…`) |
| Voltagem sem obrigatoriedade | 2 (`AppendixBTests.NoveGruposDa2_4…`, `Eletro_Tipo…`) |
| Ano de fabricação aceitando o ano seguinte | 4 (`ManufactureYearTests` ×4) |

Todas foram desfeitas depois.

**O que cada camada prova**

- Unitários: as 22 listas herdadas conferidas linha a linha contra `gazetaonline-lookups.md` (ids e rótulos); Tamanho (roupas, calçados adultos e infantis) e Gênero; o conjunto de categorias de cada um dos 18 grupos e os rótulos, a ordem e a obrigatoriedade dos campos dos nove grupos novos, lidos da tabela do Apêndice B da SPEC; as 124 categorias postáveis resolvem para o grupo do Apêndice B (as 55 demais, para Produtos em geral); marca e modelo em texto livre com limite 60; limites numéricos de Temporada e de Máquinas; Ano de fabricação (sem ano futuro, virada de ano no fuso de São Paulo); a migration de dados faz exatamente 53 `UPDATE`; a proteção contra exclusão cobre exatamente as 65 categorias da carga com grupo próprio.
- Integração: o script idempotente e as migrations dão o mesmo esquema e as mesmas 147 linhas de categorias (com o `FieldGroup` de cada uma); 9 migrations; árvore com 65 categorias com grupo gravado e a herança das novas.
- E2E: as telas de categorias continuam funcionando sobre o banco com 65 categorias protegidas.

**Achados desta rodada**

1. Dois testes da 2.6 usavam Aluguel de quartos (28) como categoria excluível; agora que ela é protegida, passaram a usar Ciclismo (58), que continua sem grupo próprio.
2. O banco do E2E (reaproveitado entre rodadas) acumulou 3 categorias criadas por testes; o banco de testes de integração, limpo, mostra 147 linhas e 124 postáveis. Sem efeito sobre o resultado.

## Checkpoint 2 — Categorias e catálogo completos (2026-10-03)

> **Em resumo:** os quatro itens do Checkpoint 2 da `plans/plan.md` estão atendidos e verificados por teste. A Fase 2 está pronta para a sua confirmação.

| Item do checkpoint | Situação | Prova |
|---|---|---|
| Árvore com 124 postáveis e ids reais | **OK** | `CategoriesTests` (integração, SQL Server limpo): 147 linhas, 124 postáveis, 22 de primeiro nível, ids reais (24, 25 e 32 ausentes como no arquivo; animais vivos fora); `ParityTests` relê `specs/categories.md` linha a linha |
| 18 grupos de campos implementados | **OK** | `AppendixBTests`: os 18 grupos da tabela do Apêndice B estão no registro; o conjunto de categorias de cada grupo bate com a SPEC; as 124 postáveis resolvem para o grupo certo; 65 categorias da carga com grupo gravado |
| Catálogo consultável e ferramenta de exportação testada | **OK** | `VehicleCatalogTests` (integração) e `Web.Tests/Catalog/*`: 4 consultas encadeadas, tipo `car`/`moto`, cache; `VehicleCatalogExport.Tests`: 43 testes (validação, `MERGE` idempotente, trava de produção, lotes). **Ressalva registrada:** a exportação real depende do parecer jurídico (A5) e do esquema do GazetaOnline, ainda não lido; hoje só a amostra reduzida existe |
| Telefone do site configurável | **OK** | `SettingsTests` (integração) e `SettingsE2ETests` (navegador): salvar em `/painel/configuracoes`, regras de telefone, auditoria, cache |

**Também conferido na verificação:** o script `db/scripts/gazeta-idempotente.sql` aplicado em SQL Server real termina com 9 migrations e 65 categorias com grupo; `git status` não mostra mudança em `appsettings*.json`, `Program.cs` nem `docker-compose*.yml`; as quatro suítes passam ao mesmo tempo (619 + 43 + 57 + 36).

**Pendências que seguem abertas (todas no `plans/BACKLOG.md`):** exportação real do catálogo (A5); esquema presumido da origem; `ICategoryUsage` real e o autocomplete de marcas na Fase 3; mensagem "Mova antes os anúncios desta categoria" fora da SPEC; caches por processo; suposições de limites numéricos.

## Tarefa 3.1 — modelo do anúncio, situações e autoria

> **Em resumo:** 714 testes unitários (95 novos), 43 da ferramenta de catálogo, 81 de integração (24 novos, SQL Server real) e 36 de navegador passam. As seis mutações derrubam testes. O esquema real de `Ads` (colunas calculadas, `CHECK`s, chaves estrangeiras, índices) só existe no SQL Server, então é provado na integração; o script idempotente também foi aplicado com `sqlcmd -I` num SQL Server real (10 migrations, 5 colunas calculadas).

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 714 | 714 | 0 |
| Ferramenta (`VehicleCatalogExport.Tests`) | 43 | 43 | 0 |
| Integração (SQL Server 2022 em contêiner) | 81 | 81 | 0 |
| E2E (Playwright, site publicado em Production) | 36 | 36 | 0 |

| Mutação | Testes que caíram |
|---|---|
| Passagem extra `Arquivado → Rascunho` | 5 (`StatusTests` ×4, `AuthorshipTests`) |
| Redator edita anúncio de outro / em qualquer situação | `AuthorshipTests.Redator_EditaOsProprios…` |
| `Normalizer` deixando de tirar o acento (descarta a categoria errada) | 8 (`NormalizerTests`, `AdEntityTests`) |
| Caminho `$.modelYear` trocado por `$.year` na migration | 4 de integração (`ComputedColumnsDifferentialTests` ×2, `AdsSchemaTests` ×2) |
| `AdsCategoryUsage` contando só os publicados | 4 (`AdsCategoryUsageTests` ×2, `DeletionTests` S08 ×2) |
| Preço zero aceito | `PriceTests.ZeroNegativoEAcimaDoTeto…` |

Todas foram desfeitas depois. A mutação do `AdsCategoryUsage` não compilava na primeira versão (nome sem `using`) e foi refeita; o resultado acima é o da versão que compilou.

**O que cada camada prova**

- Unitários: as 25 combinações de situação, com as 9 passagens do Apêndice A (e `Arquivado` definitivo); a matriz de leitura e edição por papel, autoria e situação; o serviço (Redator de outro recebe 403, Redator não publica, passagem inexistente é conflito, motivo obrigatório ao rejeitar, trilha e auditoria, recusa sem rastro, reenvio limpa a rejeição); o `Normalizer`; `AdAttributes` estrito no tipo; preço nulo, nunca zero; o modelo sem campo de pessoa do vendedor, também nos 18 grupos (NFR-19); `AdsCategoryUsage` em todas as situações; US-013-S08 e "subcategoria numa folha com anúncios" contra anúncios reais; o texto da migration `AddAds`.
- Integração: colunas calculadas persistidas com `TRY_CAST`; índices do §6.4 (filtrado e descendente); o banco recusa JSON inválido, array, `Status` fora de 1 a 5, preço zero, negativo ou acima do teto, CEP, UF, título e descrição malformados; as chaves estrangeiras sem cascata (categoria, autor, quem decidiu, anúncio com foto); `UQ_AdPhotos_StorageKey`; **teste diferencial**: para Carros, Motos, Caminhões e Ônibus e Imóveis, 11 classes de entrada (presente, zero, máximo, negativo, texto, decimal em campo inteiro, estouro, nulo, objeto, lista, booleano), cada coluna igual ao que o C# lê; completude: os caminhos das colunas são exatamente os campos filtráveis dos grupos; dois administradores decidindo o mesmo anúncio e clique duplo em enviar (6 rodadas cada): só um vence, o outro recebe `Conflict` e não deixa rastro; edição velha não sobrescreve a nova; excluir a categoria enquanto outra pessoa cria um anúncio nela (6 rodadas) nunca deixa anúncio sem categoria.

**Achados desta rodada**

1. **Número entre aspas.** O SQL converte `"12"` (e `" 7 "`) em número; o `AdAttributes` estrito lê "ausente". O teste documenta a diferença e o BACKLOG manda a 3.3 recusar texto em campo numérico.
2. **`ISJSON` e escalares.** Um escalar (`42`) falha antes do `CHECK` (erro 13609 da coluna calculada) e um array passa no `ISJSON`; por isso o `CHECK` exige também que o texto comece com `{`.
3. **Caminho do JSON diferencia maiúsculas.** `"Km"` e `"KM"` não preenchem `$.km`.
4. **Chaves estrangeiras em SQLite.** O SQLite dos testes unitários também as aplica; os anúncios de teste com quem publicou ou rejeitou precisam de contas de verdade.
5. **Os erros do SQL Server variam:** truncamento é 2628 no SQL Server 2022 (não 8152), e o `CHECK` falha com 547 só quando a coluna calculada não falha antes.
