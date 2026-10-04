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

## Tarefa 3.2 — consulta de CEP, cache e lista de municípios

> **Em resumo:** 824 testes unitários (110 novos), 43 da ferramenta de catálogo, 27 da nova `CitiesImport.Tests`, 88 de integração (7 novos, SQL Server real) e 39 de navegador (3 novos) passam. As seis mutações planejadas derrubam testes. **Nenhuma consulta ao ViaCEP real nem ao IBGE foi feita:** o ambiente bloqueia os dois hosts; tudo usa manipuladores de teste e um ViaCEP de mentira (BACKLOG).

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 824 | 824 | 0 |
| Ferramenta de catálogo (`VehicleCatalogExport.Tests`) | 43 | 43 | 0 |
| Ferramenta de municípios (`CitiesImport.Tests`) | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 88 | 88 | 0 |
| E2E (Playwright, site publicado em Production, ViaCEP de mentira) | 39 | 39 | 0 |

| Mutação | Testes que caíram |
|---|---|
| Cache valendo 30 dias e mais um | `CepEncontrado_ValeTrintaDias` (a fronteira de 30 dias exatos) |
| `{"erro": true}` tratado como falha do serviço | 3 (`ErroDoViaCep_EhCepInexistente…` ×3) |
| CEP incompleto consultando mesmo assim | 9 (endpoint ×6, serviço ×3) |
| CEP inexistente tratado como encontrado (entra no cache) | 2 (`CepInexistente_NaoEntraNoCache`, `CepInexistente_Devolve404…`) |
| Tempo limite padrão de 25 s em vez de 5 s | 2 (`ClienteReal_ConsultaARotaFixa…`, `Padroes_SaoOEnderecoEOTempoDoAdr007`) |
| Cidade ignorando a lista do IBGE | `Cidade_ConferidaComAListaDoIbge_UsaONomeOficial…` |

Todas foram desfeitas depois.

**O que cada camada prova**

- Unitários: `GET /api/v1/cep/{cep}` com 401 em JSON sem login (nada é consultado), 400 para 6 formas de CEP malformado sem consultar, 503 para 5xx e para falha de rede, 404 para `erro: true` sem entrar no cache, e **resposta com só `cep`, `city`, `uf` e `source`** mesmo quando o ViaCEP manda rua e bairro (D3); cliente real com a rota fixa `https://viacep.com.br/ws/{cep}/json/` e o tempo limite de 5 s; `ViaCepLookup` isolado (lento, cancelado pelo cliente, formatos ilegíveis, 6 códigos HTTP de erro, aviso com CEP e traceId); cache (29 dias e 23 horas vale, 30 vence, entrada vencida atualizada no lugar, vencida com o ViaCEP fora devolve 503 sem servir o dado velho, corrida de dois pedidos); padronização (nome oficial pelo código IBGE, regra da SPEC fora da lista, artigos minúsculos); `GET /api/v1/cities` (ordem sem acento, só a UF pedida, UF sem carga devolve `[]`, 400, cache privado de 10 min); 30 consultas por minuto por usuário (a 31ª recebe 429 `RATE_LIMITED` com `Retry-After` e outro usuário continua); 403 em JSON em `/api` e as páginas continuam redirecionando; texto da migration.
- `CitiesImport.Tests`: leitura dos dois formatos de UF do IBGE, validação que recusa a carga inteira (código curto, UF inexistente, código que não é da UF, nome vazio ou longo, código ou nome repetido), script determinístico em lotes de 500 com aspas escapadas, `.sql` da amostra igual ao que a ferramenta gera do `.json`, recusa de produção antes de ler o arquivo.
- Integração: CHECK do CEP e índice único de município em SQL Server; duas consultas do mesmo CEP ao mesmo tempo (6 rodadas) deixam uma linha só e ambas recebem resposta; o script da amostra aplica 40 municípios e aplicar de novo não muda nada; nova carga corrige nome, acrescenta e **não apaga**; o carregador em lote grava o mesmo que o script; `NameSearch` gravado = `Normalizer` do site para os 40; código de cada município pertence à UF dele.
- E2E: `cep.js` real contra o endpoint real e um ViaCEP de mentira: encontrado (rua e bairro não chegam ao navegador), segundo pedido do cache (uma consulta externa só), incompleto sem chamar o servidor, 404 sem repetir, 503 repetindo uma vez com "tentativa 2 de 2" e depois `indisponivel` (exatamente duas chamadas), falha na primeira e sucesso na segunda, 401 em JSON sem login, 400 para UF inválida.

**Achados desta rodada**

1. **O limitador rodava antes da autenticação.** A política `cep` (por usuário) via todo mundo como anônimo e agrupava por IP: o teste "outro usuário não é afetado" pegou. O `UseRateLimiter` passou para depois do `UseAuthentication` (BACKLOG).
2. **O cookie redirecionava chamadas `/api` para a página de entrada.** O contrato pede 401 e 403 em JSON; em `/api` agora são ProblemDetails e as páginas seguem redirecionando (testado nos dois sentidos).
3. **Código IBGE × UF.** A validação de que os dois primeiros dígitos do código são os da UF confere todos os 40 códigos da amostra (escritos de memória) contra a UF informada; nomes e dígitos finais seguem a conferir na carga real.

## Tarefa 3.3 — criar e editar o rascunho do anúncio (US-008)

> **Em resumo:** 992 testes unitários (168 novos), 43 da ferramenta de catálogo, 27 da `CitiesImport.Tests`, 94 de integração (6 novos, SQL Server real) e 48 de navegador (9 novos) passam. As seis mutações planejadas derrubam testes. O ViaCEP real não foi consultado (o ambiente bloqueia o host): o E2E usa um ViaCEP de mentira. **Não houve migration nova**, então o script e a contagem de 11 migrations não mudam.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 992 | 992 | 0 |
| Ferramenta de catálogo (`VehicleCatalogExport.Tests`) | 43 | 43 | 0 |
| Ferramenta de municípios (`CitiesImport.Tests`) | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 94 | 94 | 0 |
| E2E (Playwright, site publicado em Production, ViaCEP de mentira) | 48 | 48 | 0 |

| Mutação | Testes que caíram |
|---|---|
| Preço lido em centavos em vez de reais (`62000` valendo R$ 620,00) | 26 (`PriceTextTests`, `AdDraftServiceTests`, `DraftTests`) |
| `AuthorId` aceito no formulário que recebe o POST | 2 (`FormularioDeEdicao_NaoTemCamposDeDecisao`, `OFormularioEnviaSoTexto…`) |
| Limite de título do grupo ignorado (sempre 120) | `LimiteDoTitulo_VemDoGrupoDaCategoria` (Vagas com 91) |
| Texto aceito e gravado em campo numérico | 11 (quase todos em `FieldValueParserTests`) |
| Trocar de categoria mantendo os campos do grupo antigo | `TrocarDeCategoria_MantemOsCamposComunsEDescartaOsDoGrupoAnterior` |
| Cidade do CEP não aplicada pelo servidor quando o JavaScript não rodou | 5 (`Cep_SemCidadeDoNavegador…`, `Cep_QueMudouDepoisDaCidade…`, `Cep_ResolvidoNoServidor…`, `US008S01…` ×2) |

Todas foram desfeitas depois (os 165 testes da tarefa passam de novo depois de desfeitas). Uma primeira tentativa da sexta mutação não compilava (parâmetro sem uso, aviso como erro) e mostrava o resultado da mutação anterior; foi refeita com uma variante que compila.

**O que cada camada prova**

- Unitários: `PriceText` (notação brasileira; `62000` e `62.000` são reais; `12.5`, `62.00`, `1,2,3` recusados; número gigante não estoura), `FieldValueParser` (inteiro com milhar em ponto, decimal com vírgula, dinheiro em centavos como `long`, opção fora da lista, várias opções sem repetir, ano do modelo até o ano que vem, ano de fabricação até o atual, tipo desconhecido falha alto); `AdDraftServiceTests` (limites por grupo — Vagas 90/6000, Serviços 120/6000, demais 120/5000 —, CRLF contando 1, Serviços sem preço, preço zero/teto/texto, cadeia do catálogo em carros e motos, CEP resolvido no servidor, cidade forjada, UF sem lista padronizada, modo manual com selo, Rejeitado continua Rejeitado, Administrador não muda a situação nem o autor, em revisão e arquivado não salvam, troca de categoria descarta o grupo antigo, limite de fotos, auditoria só com nomes de campos, salvar sem mudar nada não grava nem audita); `DraftTests` pelo site de teste (as nove cenas S01 a S14 da tela, XSS no formulário e na leitura, `Status`/`AuthorId`/`PublishedAt` no corpo sem efeito, tipo errado no corpo vira recusa e nunca erro 500); `FormRenderingTests` renderiza **as 124 categorias** (todos os 18 grupos e os 9 tipos de campo) e confere rótulo, nome, ids sem repetição, mensagem de erro ligada por `aria-describedby` e que toda referência de `aria-describedby` aponta para um id que existe.
- Integração: o que o formulário grava (`45.000`, `1.450,75`) alimenta as colunas calculadas do catálogo, do km e da área com o valor certo; duas telas salvando o mesmo anúncio ao mesmo tempo (6 rodadas) — uma grava, a outra recebe Conflito, sem rastro na auditoria; a versão velha do formulário é recusada; categoria apagada entre a conferência e a gravação vira recusa de categoria; anúncio e auditoria saem juntos.
- E2E: jornada completa com recarga (só o que o servidor guardou aparece) e nenhuma resposta ≥ 400 do próprio site; troca de categoria sem recarregar a página, com o foco de volta em "Categoria"; máscara de preço, contadores "X/N" (a quebra de linha conta 1), CEP fora do ar com "tentativa 2 de 2" e UF/cidade em listas, CEP inexistente sem abrir o manual, **todo o caminho sem JavaScript** (botão "Atualizar campos", salvar, cidade resolvida no servidor), Axe WCAG 2.1 AA no formulário de carros, no de vagas e com erros, e sem rolagem horizontal em 320 px.

**Achados desta rodada**

1. **`role="alert"` no `<ul>` deixava os `<li>` órfãos** (Axe: `listitem`). A mensagem de erro agora fica numa `div` com o `role` e a lista dentro; vale também para as telas antigas que usam `_FieldErrors`.
2. **Contraste do erro fora de um cartão.** O texto de erro (`text-danger` sobre o fundo `#f1f5fd` da página) ficava abaixo de 4,5:1. O formulário passou a ficar dentro de um `card` branco, como as outras telas do painel.
3. **O rótulo "UF (automático)" estourava 1 px em 320 px.** As colunas de cidade e UF empilham abaixo de 576 px.
4. **`innerHTML` é proibido por teste (RC-17).** A troca de categoria usa `DOMParser` para ler a parcial do servidor e passa os nós para a página.
5. **Máscara de caixa eletrônico:** digitar `1500` deixa `15,00` (dígitos entram pela direita). O comportamento é o aprovado (D1); registrado no BACKLOG para o Product Owner confirmar.
6. **O cache de 30 dias do CEP vale entre rodadas do E2E.** A asserção "uma consulta ao ViaCEP" passou a ser "no máximo uma"; para contar de novo, limpar `CepCache` (documentado).

## Tarefa 3.4 — processamento, armazenamento e entrega de fotos (ADR-005)

> **Em resumo:** 1.084 testes unitários (92 novos), 43 da ferramenta de catálogo, 27 da `CitiesImport.Tests`, 95 de integração (1 novo, SQL Server real) e 48 de navegador (sem teste novo: a tarefa não tem tela; a suíte inteira foi rodada de novo como regressão) passam. As sete mutações planejadas, mais uma extra, derrubam testes. **O HEIC dos testes é sintético** (um quadro x265 numa caixa HEIF montada à mão, 885 bytes); a verificação com um HEIC de iPhone de verdade e a prova na hospedagem Windows (AR-05) seguem pendentes (BACKLOG).

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.084 | 1.084 | 0 |
| Ferramenta de catálogo (`VehicleCatalogExport.Tests`) | 43 | 43 | 0 |
| Ferramenta de municípios (`CitiesImport.Tests`) | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 95 | 95 | 0 |
| E2E (Playwright, site publicado em Production) | 48 | 48 | 0 |

| Mutação | Testes que caíram |
|---|---|
| Conteúdo sem assinatura tratado como JPEG (a extensão, na prática, decidindo) | 2 (`QuemNaoEUmDosCincoFormatos…`, `ArquivoFalsoComExtensaoJpg…`) |
| Metadados mantidos (sem `Strip`) | 2 (`Gps_NaoSobrevive_NasVersoes`, `NoArquivoGravado_OGps…`) |
| Formato da chave de foto frouxo (aceita `../`, `_originals/`, maiúsculas) | 7 (`ChaveForaDoFormato…`, `ChaveDeOriginalForaDoFormato…`) |
| Limite de 50 milhões de pixels removido | 2 (`ImagemAcimaDoLimiteDePixels…`, `CincoMilhoesDePixelsNoLimite…`) |
| Limpeza depois de falha no meio removida | `FalhaNoMeio_ApagaArquivosGravados` |
| Toda foto tratada como de anúncio publicado | 8 (`AnuncioNaoPublicado_…` em 4 situações × 2 testes) |
| Política do ImageMagick sem o "nega tudo" | 8 (`DecodificadoresNaoUsados_…`, `DecodificadoresDeTexto…`, `Politica_Embutida…`) |
| (extra) Limite de 2 decodificações ao mesmo tempo virando 100 | `DecodificacoesAoMesmoTempo_NuncaPassamDoLimite` |

Todas foram desfeitas depois.

**O que cada camada prova**

- Assinaturas: JPEG, PNG, GIF87a/89a, WebP e HEIC (marcas `heic`, `heix`, `hevc`, e `mif1`/`msf1` só com marca de HEIC compatível); PDF, SVG, MVG, RIFF que não é WebP, AVIF, MP4 e arquivos cortados no cabeçalho não têm formato. Arquivo falso nunca chega ao disco.
- Versões: 3000 × 2000 → 1600 × 1067 e 480 × 320; foto menor não é ampliada; o arquivo gerado é **byte a byte** o WebP de qualidade 80 feito direto (e menor que o de 95); orientação 6 do EXIF gira 40 × 20 para 20 × 40; GIF animado de 3 quadros vira 1 (o vermelho); PNG transparente mantém o alfa; 8 envios ao mesmo tempo gravam 24 arquivos com chaves diferentes e nenhum `.tmp`.
- Metadados: o JPEG de teste tem mesmo latitude, longitude, fabricante e texto no EXIF (teste de controle); nas duas versões não há EXIF, XMP nem o texto em lugar nenhum dos bytes; no original gravado o texto continua (por isso nenhuma rota o serve).
- Segurança: PNG de 8000 × 8000 (64 milhões de pixels, 279 KB) recusado em menos de 2 s sem decodificar; 10.000 × 5.000 passa e 10.001 × 5.000 não; lado de 20.001 px recusado; 12 decodificadores (SVG, MVG, MSL, URL, HTTP, TEXT, EPHEMERAL, MSVG, PS, PDF…) recusados pela política, por prefixo, em arquivo que existe; SVG/MVG com assinatura de JPEG na frente recusados como "não foi possível ler" (o formato é imposto); 11 chaves de armazenamento e 6 de original fora do formato recusadas sem tocar no disco; todo arquivo gravado casa com `^12/[0-9a-f]{32}_(1600|480).webp$` ou `_originals/AAAA-MM/<guid>.<ext>`.
- Falhas: falha ao gravar as versões apaga o original já gravado; falha na miniatura apaga a versão grande e o erro original sobe (não o da limpeza); JPEG, PNG, GIF e WebP cortados em 60% recusados; 10 MB exatos passam e 10 MB + 1 byte não chegam ao processador; fluxo sem tamanho conhecido é lido só até o limite; arquivo vazio tem mensagem própria; HEIC quebrado ou sem a biblioteca devolve a mensagem de HEIC.
- Entrega: publicado → `public, max-age=31536000, immutable`, `image/webp`, bytes iguais ao arquivo; Rascunho, Em revisão, Rejeitado e Arquivado → 404 **idêntico** (status, cabeçalho de cache, tipo e tamanho) ao de foto inexistente para anônimo e para outro Redator, e 200 com `no-store, private` para o autor e o Administrador; foto de outro anúncio 404; 9 tentativas de chegar a `_originals/` 404; 7 tentativas de sair da pasta e 12 rotas com id ou tamanho inválido 404; arquivo sumido do disco 404 (não 500); 300 pedidos passam acima do limite global de 100, o 301º recebe 429 com `Retry-After`, `/painel/entrar` continua 200 e outro IP não é afetado.
- Integração: a consulta foto × anúncio com as chaves estrangeiras reais, publicado público, rascunho 404 para anônimo e 200 privado para o autor logado, foto trocada de anúncio 404.

**Achados desta rodada**

1. **O ImageMagick não sobrescreve arquivos de configuração que já existem.** A mutação "sem nega-tudo" só era derrubada pelo teste que lê o texto da política; os de comportamento passavam porque sobrava uma pasta `_magick` antiga com a política certa. Em produção, uma política nova numa versão nova do site seria ignorada. A pasta agora leva o hash da política (`_magick/<hash>`), com teste.
2. **Arquivo cortado vira foto pela metade.** O ImageMagick devolve a parte lida e só avisa; o teste de JPEG cortado passou sem erro na primeira versão. Os avisos de fim prematuro agora recusam a foto (JPEG, PNG, GIF e WebP conferidos).
3. **O limite de recurso de largura já recusa no decodificador de PNG** ("Invalid IHDR data" acima de 20.000 px), então a mensagem pode ser a de "dimensões" ou a de "não foi possível ler"; a recusa é o que vale.
4. **Defesa em profundidade escondia a mutação do formato da chave.** O confinamento do caminho (`Confine`) também recusa `../`, então o teste só derruba a mutação com chaves que passam por ele (maiúsculas, `0/`, sufixo `.webp`); incluídas.
5. **Memória:** 512 MB para o ImageMagick e 200 MB por foto de 50 milhões de pixels pedem um teto de decodificações ao mesmo tempo; o limite é 2 por processo (não estava no plano; sem ele, 3 envios grandes ao mesmo tempo estouram o limite e viram erro).
6. **A API do Magick.NET 14 mudou** (`ColorProfile.SRGB` obsoleto, tamanhos `uint`, `IExifProfile`); o código usa `ColorProfiles.SRGB`.

## Tarefa 3.5 — enviar, trocar a capa e remover fotos do anúncio (US-008-S02 a S06)

> **Em resumo:** 1.117 testes unitários (33 novos), 43 da ferramenta de catálogo, 27 da `CitiesImport.Tests`, 99 de integração (4 novos, SQL Server real) e 55 de navegador (7 novos) passam. As seis mutações planejadas e mais cinco extras foram todas derrubadas pelos testes. Veredito: **aprovado**. Pontos de atenção: o envio com JavaScript é de uma foto por vez (a ordem da galeria depende disso) e a prova do `UPDLOCK` só existe no SQL Server real.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.117 | 1.117 | 0 |
| Ferramenta de catálogo (`VehicleCatalogExport.Tests`) | 43 | 43 | 0 |
| Ferramenta de municípios (`CitiesImport.Tests`) | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 99 | 99 | 0 |
| E2E (Playwright, site publicado em Production) | 55 | 55 | 0 |

| Mutação | Testes que caíram |
|---|---|
| M1 Limite de fotos com uma a mais (`>=` virando `>`) | 4 (`US008S04…`, `LimiteDe20…`, `Servicos_AceitamNoMaximo6Fotos`, `VagasDeEmprego_NaoTemFotos…`) |
| M2 Anúncio carregado sem conferir autoria nem situação | 2 (`Autorizacao_…`, `SemJavaScript_AnuncioEmRevisao…`) |
| M3 Remover sem apagar os WebP | `US008S03_TrocarACapaERemoverUmaFoto` |
| M4 Remover sem renumerar as posições | 2 (`US008S03…`, `RemoverACapa_…`) |
| M5 "Tornar capa" sem reordenar | 2 (`US008S03…`, `SemJavaScript_TornarCapa…`) |
| M6 Envio sem antiforgery (`[IgnoreAntiforgeryToken]`) | `SemToken_Devolve400_ESemLogin_Devolve401` |
| (extra) M7 Sem `UPDLOCK` na transação | 3 de integração (posições únicas, limite sob corrida, prova do bloqueio) |
| (extra) M8 Envio sem o limite de 30 por minuto | `Envio_31oNaMesmaJanela…` |
| (extra) M9 Teto de 2560 px do lado maior removido | 2 (`PanoramaVertical500x20000…`, `RetratoAlto1200x3000…`) |
| (extra) M10 413 por limite da rota ignorado no middleware | `US008S05_PdfEFotoDe15MB…` |
| (extra) M11 Limite não reconferido dentro da transação | `SeisEnviosAoMesmoTempo_EmAnuncioCom18Fotos…` |

Todas foram desfeitas depois (arquivos restaurados; compilação e suíte unitária rodadas de novo sem alteração).

**O que cada camada prova**

- API (`PhotosEndpointsTests`): 3 envios gravam 3 linhas na ordem enviada (posições 0, 1, 2), cada uma com as duas versões e o original em disco, e a resposta 201 segue o contrato `Photo` (`id`, `sortOrder`, `url480`, `url1600`, `width`, `height`). Tornar capa na 3ª foto vira [3ª, 1ª, 2ª]; remover a 2ª deixa 2 fotos com posições 0 e 1, apaga os dois WebP, **mantém o original** e a URL da foto removida devolve 404. Com 20 fotos, a 21ª recebe 409 com "Cada anúncio pode ter no máximo 20 fotos" e nada é processado nem gravado; a 20ª entra (19 + 1); Serviços recusa a 7ª; Vagas recusa qualquer envio.
- Recusas por arquivo: PDF devolve 400 com "Formato não aceito. Use JPG, PNG, WebP, GIF ou HEIC"; arquivo de 10 MB + 1 KB devolve 400 com "A foto excede o limite de 10 MB"; arquivo de 15 MB devolve 413 antes de ler o corpo; arquivo vazio e campo `file` ausente devolvem 400. Nenhum deixa linha nem arquivo.
- Acesso: sem login 401; sem token 400 (envio, capa e remoção); outro Redator, anúncio Em revisão e anúncio Publicado 403 `FORBIDDEN`; Administrador envia no rascunho de outro; anúncio inexistente 404; foto de outro anúncio ou inexistente 404 na capa e na remoção.
- Limites: 30 envios no mesmo minuto passam (recusados pelo serviço, mas contados) e o 31º devolve 429 `RATE_LIMITED` com `Retry-After`, sem afetar outro usuário.
- Falha de gravação: quando o registro falha depois de a foto estar em disco (o anúncio some no meio), a resposta é 500 e **nem as versões nem o original ficam no disco**, e não há linha em `AdPhotos`.
- Páginas (`PhotosPageTests`): anúncio novo mostra a regra "Salve o rascunho primeiro" e nenhuma galeria; rascunho gravado mostra "Fotos (0 de 20)" / "(0 de 6)" e o formulário de envio; Vagas não tem galeria; os botões de salvar ficam fora do formulário do anúncio e ligados a ele pelo atributo `form`; sem JavaScript, enviar uma foto de cada vez mostra "Foto adicionada." (uma vez só) e as miniaturas em ordem, o PDF volta com a recusa como alerta, tornar capa reordena, remover pede uma página de confirmação (GET não remove) e só o POST remove; outro Redator recebe 403 e não vê as fotos; anúncio Em revisão mostra as fotos sem formulário nem botões.
- Imagem (`VersoesTests`, D4): 500 × 20000 vira **64 × 2560** (miniatura igual); 1200 × 3000 vira 1024 × 2560 e a miniatura 480 × 1200; 1200 × 2400 e 1000 × 2560 não mudam.
- Integração (SQL Server real, `PhotoConcurrencyTests`): 8 envios ao mesmo tempo no mesmo anúncio, em 3 rodadas, gravam sempre as posições 0 a 7 sem repetir; com 18 fotos, 6 envios ao mesmo tempo resultam em exatamente 2 criadas e 4 com 409, total 20 e **só 6 arquivos** no disco; uma segunda sessão segurando `UPDLOCK` na linha do anúncio **bloqueia o envio por mais de 4 s** e o envio termina quando ela libera; capa e remoção contra o banco real mantêm as posições 0 a n-1 (a restrição `CK_AdPhotos_SortOrder` do banco vale).
- E2E (`PhotosE2ETests`): S02 (3 fotos, miniaturas carregam, "Capa" só na primeira, salvar o rascunho, recarregar, mesma ordem); S03 (tornar capa na 3ª, cancelar e depois confirmar a remoção na caixa de diálogo, recarregar: 2 fotos na ordem certa); S04 (20 fotos enviadas, a 21ª mostra a mensagem, 20 depois de recarregar); S05 (PDF e foto de 15 MB com as duas mensagens, nada adicionado, "Descartar", recarregar); S06 (a conexão cai só no envio da 2ª foto: "Falha ao enviar" e "Tentar de novo" naquela foto, a 1ª foto, o título e a descrição continuam, e o reenvio funciona); sem JavaScript (enviar, tornar capa, confirmar a remoção em página própria); acessibilidade (axe sem violações com 3 fotos, com a confirmação aberta e com uma recusa na lista) e sem rolagem horizontal em 320 px. Nenhuma jornada feliz teve resposta ≥ 400.

**Achados desta rodada**

1. **O limite de 2 conversões ligava o ImageMagick cedo demais.** Como toda página do anúncio agora monta o serviço de fotos, o construtor do `MagickImageProcessor` ligava a biblioteca na primeira página aberta, com a política de produção, antes de os testes poderem ligar a variante com `XC`. A suíte inteira caiu em 40 testes de foto (que passavam isolados). A inicialização foi para a primeira foto processada.
2. **A seção de fotos não pode ficar dentro do formulário do anúncio** (formulários não se aninham). A galeria saiu do trecho trocado pelo JavaScript; o trecho continua trazendo a regra de fotos da categoria escolhida ("Este anúncio aceita até N fotos" / "Vagas de emprego não têm fotos"), que dois E2E antigos já cobriam (`DraftE2ETests` mudou para o novo texto).
3. **Cor de botão no axe:** `btn-outline-primary` medido logo depois do clique, com o mouse em cima, pegou a transição de cor do Bootstrap. O teste move o mouse e espera o fundo transparente antes de medir.
4. **O limite de envios por minuto precisou de uma chave de configuração** (`RateLimiting:PhotoUploadsPerMinute`) para o E2E: só o S04 sobe 21 fotos de uma conta em um minuto.
5. **O 413 acima de 11 MB não existia no ambiente de teste:** o `BodyLimitMiddleware` pulava as rotas com limite próprio e o servidor de teste não aplica o `[RequestSizeLimit]`. O middleware agora confere o `Content-Length` contra o limite da própria rota (M10).
6. **`dotnet format` tirou o BOM de arquivos antigos sem relação com a tarefa** (migrations e `MSTestSettings.cs`); essas mudanças foram desfeitas.
