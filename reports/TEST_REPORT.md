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
