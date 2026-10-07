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

## Tarefa 3.6 — limpeza diária dos originais e dos arquivos órfãos (ADR-005)

> **Em resumo:** 1.136 testes unitários (19 novos), 43 da ferramenta de catálogo, 27 da `CitiesImport.Tests`, 101 de integração (2 novos, SQL Server real) e 55 de navegador (sem teste novo: a tarefa não tem tela; a suíte inteira rodou de novo como regressão, já com o serviço registrado no site publicado) passam. As nove mutações (6 planejadas e 3 extras) foram todas derrubadas. A verificação manual no site publicado apagou o original antigo e os dois órfãos e registrou tudo no log. Veredito: **aprovado**.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.136 | 1.136 | 0 |
| Ferramenta de catálogo (`VehicleCatalogExport.Tests`) | 43 | 43 | 0 |
| Ferramenta de municípios (`CitiesImport.Tests`) | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 101 | 101 | 0 |
| E2E (Playwright, site publicado em Production) | 55 | 55 | 0 |

| Mutação | Testes que caíram |
|---|---|
| M1 Prazo de 30 dias menor por 1 tick (o limite exato deixa de ficar) | `ApagaSoOriginaisComMaisDeTrintaDias` |
| M2 Versão WebP apagada mesmo com registro | 2 (`VersoesWebp_NuncaSaoApagadas…`, `DepoisDaLimpezaDe30Dias…`) |
| M3 `OriginalKey` não anulada | 4 (`AnulaOriginalKey…`, `Log_…`, `Anulacao_VaiEmLotes…`, `DepoisDaLimpezaDe30Dias…`) |
| M4 Órfão sem a carência de 24 horas | `VersaoSemRegistro_SoEApagadaDepoisDe24Horas` |
| M5 Lote de 500 virando 1.000.000 | `Anulacao_VaiEmLotesDe500_UmaInstrucaoPorLote` |
| M6 Falha de E/S interrompendo a rodada | `FalhaDeEscritaEmUmArquivo_NaoInterrompeOsOutros…` |
| (extra) M7 Reprocessar sem conferir o original | 2 (`SemOriginal_FalhaComOriginalIndisponivel…`, `DepoisDaLimpezaDe30Dias…`) |
| (extra) M8 Pasta recém-esvaziada removida sem carência | `PastaDeMesVazia_SoSaiNoDiaSeguinte…` |
| (extra) M9 Primeira rodada em 2 minutos | `RodaUmMinutoDepoisDaPartida_EDepoisACada24Horas` |

Todas foram desfeitas depois (arquivos restaurados; compilação limpa e a suíte rodada de novo sem alteração).

**O que cada camada prova**

- Idade dos originais: 29 dias fica, **exatamente 30 dias fica** ("mais de 30"), 30 dias e 1 minuto sai, 90 dias sai (um atraso não perde nada). O original de uma foto removida, sem linha no banco, sai pela idade do arquivo.
- `OriginalKey`: só a do arquivo realmente apagado é anulada; a de um original de 5 dias e as linhas sem original não mudam; se o arquivo não pôde ser apagado, a chave fica.
- Versões WebP: com registro, nunca são apagadas (nem com dois anos). Sem registro, só depois de **mais de 24 horas** (23 h e exatamente 24 h ficam). Arquivos `*.tmp` seguem a mesma carência.
- O que nunca é tocado: `_magick/` (com dois arquivos de 800 dias), arquivo solto na raiz, texto numa pasta de anúncio, pasta que não é um id de anúncio, GUID em maiúsculas, tamanho `_800`, pasta que não é um mês, original sem GUID e extensão `.exe`.
- Falha de E/S em um arquivo (dublê que lança `IOException`): os outros são apagados, o resultado conta 1 falha, o motivo vai ao log como aviso e a chave daquele arquivo não é anulada.
- Lotes: 1.200 originais antigos geram **exatamente 3 instruções `UPDATE`** (500 + 500 + 200), cobrindo as 1.200 linhas. No SQL Server real, o `IN (…)` com 1.200 chaves em lotes funciona e deixa só a chave do original de 2 dias.
- Agendamento (relógio manual, `Task.Delay` e `PeriodicTimer` seguindo o mesmo tempo): nada roda em 59 s; roda em 1 minuto; a segunda rodada vem 24 horas depois da primeira (nada em 23 h 59 min 59 s).
- Pastas: a pasta de mês que acabou de ficar vazia espera a carência e sai no dia seguinte; uma pasta vazia antiga sai na hora; `_originals/` fica.
- Sem `PhotoStorage:BasePath` (vazio, em branco ou pasta inexistente) a rodada é pulada com aviso e sem erro.
- Reprocessar: chave nula e arquivo ausente falham com "Original indisponível"; foto inexistente dá 404; com original, as duas versões voltam a ser a mesma do envio (byte a byte), largura, altura e tamanho são atualizados e nenhum temporário sobra. Depois de uma rodada que apagou o original de 31 dias, as versões continuam e reprocessar falha.
- Integração: varredura completa com arquivos de datas antigas contra o banco real (2 originais e 2 órfãos apagados; versões com registro, envio em andamento de 20 minutos e `_magick/` ficam).
- **Verificação manual** (site publicado em Production, SQL Server do E2E): um original de 45 dias e dois arquivos de versão de 3 dias em uma pasta de anúncio sem registro foram criados; o site foi reiniciado e, 1 minuto depois, o log trouxe `Original apagado`, dois `Arquivo órfão apagado` e o total (1 original, 2 órfãos). As pastas vazias ficaram para a rodada seguinte, como previsto.

**Achados desta rodada**

1. **A tarefa de segundo plano agora arma o temporizador depois do `StartAsync`.** No .NET 10 o `BackgroundService` não chega ao primeiro `await` de forma síncrona; o teste de agendamento esperava um temporizador que ainda não existia e avançou o relógio cedo demais. O teste agora espera o temporizador estar armado.
2. **A limpeza só enxerga o que o site gera.** O armazenamento lista apenas arquivos com nome no formato do site, então `_magick/` e qualquer arquivo alheio nem chegam à rotina de apagar; o teste cobre dez formatos diferentes de "arquivo estranho".
3. **Pasta recém-esvaziada:** apagar o último arquivo muda a data da pasta para agora, então a regra de 24 horas a deixa para o dia seguinte (testado com dois dias do relógio manual).
4. **`dotnet format` foi aplicado só aos arquivos da tarefa** (`--include`), sem tocar nos 14 arquivos antigos do achado do BOM.

## Tarefa 3.7 — enviar anúncio para revisão (US-009)

> **Em resumo:** 1.163 testes unitários (27 novos), 43 da ferramenta de catálogo, 27 da `CitiesImport.Tests`, 102 de integração (1 novo, SQL Server real) e 60 de navegador (5 novos) passam. As seis mutações planejadas e duas extras foram todas derrubadas. Veredito: **aprovado**.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.163 | 1.163 | 0 |
| Ferramenta de catálogo (`VehicleCatalogExport.Tests`) | 43 | 43 | 0 |
| Ferramenta de municípios (`CitiesImport.Tests`) | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 102 | 102 | 0 |
| E2E (Playwright, site publicado em Production) | 60 | 60 | 0 |

| Mutação | Testes que caíram |
|---|---|
| M1 Não exigir foto fora de Vagas | 4 (`Vagas_NaoExigeFoto…`, `AOrdemDaListaEADaTela`, `US009S02`, `ConfirmacaoEPostDeConfirmacao…`) |
| M2 Exigir preço também em Serviços | 2 (`Vagas_NaoExigeFoto_Servicos_NaoExigePreco`, clique duplo em paralelo) |
| M3 Enviar sem olhar as pendências | `ConfirmacaoEPostDeConfirmacao_ConferemDeNovoNoServidor…` |
| M4 Sem a idempotência do "já enviado" | `US009S05` e `CliqueDuploEmParalelo…` |
| M5 Reenvio sem limpar o motivo da rejeição | `US009S04` |
| M6 Enviar sem conferir o acesso | `Acesso_OutroRedatorEmRevisaoEPublicado_Recusam…` |
| (extra) M7 Frase "Informe o quilometragem" | 4 (`Carros_ExigemMarca…`, `AOrdemDaListaEADaTela`, `TodoCampoObrigatorioTemFrasePropria…`, `US009S03`) |
| (extra) M8 Perder a corrida e não reconhecer o envio do outro | `QuatroPedidosDeConfirmacaoAoMesmoTempo…` (integração) |

Duas mutações (M3 e M4) não compilavam na primeira forma (código inalcançável é erro com `TreatWarningsAsErrors`) e foram refeitas; todas foram desfeitas e a compilação final ficou limpa (0 avisos, 0 erros).

**O que cada camada prova**

- S01: o rascunho completo vai à página de confirmação, o POST muda a situação para Em revisão e a mensagem "Anúncio enviado para revisão" aparece; o anúncio passa a somente leitura para o Redator.
- S02/S03: a lista mostra só o que falta, na ordem título, categoria, descrição, preço, CEP, foto e campos do grupo, cada item com link para o campo; a situação continua Rascunho. Em Carros a falta da quilometragem mostra só "Informe a quilometragem".
- S04: o rejeitado corrigido volta a Em revisão, o motivo é limpo e `SentAt` é gravado; a auditoria guarda o histórico.
- S05: clique duplo e pedidos em paralelo geram uma passagem e uma auditoria `ad.submit`; a segunda vez diz "Este anúncio já foi enviado para revisão". No SQL Server real, 4 pedidos simultâneos × 3 rodadas também terminam em uma passagem, sem erro.
- O botão "Enviar para revisão" salva o formulário antes de conferir (mesmo com pendências) e só aparece depois do primeiro "Salvar rascunho"; formulário inválido volta com o erro ao lado do campo.
- A página de confirmação e o POST reconferem as pendências no servidor; acesso: outro Redator, Em revisão e Publicado são recusados e o Administrador não reenvia (409).
- As 41 frases de `RequiredMessage` são conferidas uma a uma (artigo e gênero certos) e nenhum campo obrigatório fica sem frase.
- E2E: fluxo completo com navegador, foco levado ao campo pelo link da pendência, botão "Enviando…" travado, fluxo inteiro sem JavaScript por formulários comuns, axe sem violações e sem rolagem horizontal em 320 px.

**Achados desta rodada**

1. **Terrenos e área:** o wireframe cita "Terrenos exige área", mas `areaM2` não é obrigatória nos dados; os testes seguem os dados (BACKLOG).
2. **Administrador:** edita Em revisão e Publicado, mas não reenvia; o botão não aparece e o servidor recusa.
3. **S04 sem E2E:** rejeitar só existe pela fila da 4.1; até lá o cenário é provado por teste HTTP com o anúncio rejeitado semeado.
4. **Contraste do "Cancelar":** em `btn-outline-secondary` direto sobre o fundo da página dava 4,29; a confirmação ficou dentro de um `.card`.
5. **Flake isolado:** `US008S03` falhou uma vez (613 ms) numa rodada, passou isolado e nas duas rodadas completas seguintes (60/60); acompanhar.
6. **`dotnet format` aplicado só aos arquivos da tarefa** (`--include`), sem tocar nos 14 arquivos antigos do achado do BOM.

## Tarefa 3.8 — card, valor e corpo do anúncio (componentes de apresentação)

> **Em resumo:** 1.210 testes unitários (47 novos), 43 da ferramenta de catálogo, 27 da `CitiesImport.Tests`, 102 de integração e 66 de navegador (6 novos, com um segundo site em Development) passam. As seis mutações planejadas e cinco extras foram derrubadas (uma delas só depois de um teste novo). Veredito: **aprovado**.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.210 | 1.210 | 0 |
| Ferramenta de catálogo (`VehicleCatalogExport.Tests`) | 43 | 43 | 0 |
| Ferramenta de municípios (`CitiesImport.Tests`) | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 102 | 102 | 0 |
| E2E (Playwright: site em Production + site em Development) | 66 | 66 | 0 |

| Mutação | Testes que caíram |
|---|---|
| M1 Centavos sempre aparecem ("R$ 62.000,00") | 10 (`Valor_CentavosSoQuandoNaoSaoZero` × 4, nome acessível, Vagas, texto codificado, centavos, sem cidade/preço) |
| M2 Vagas passam a ter foto | 3 (`SoVagasFicamSemFoto`, `Vagas_SemArea…`, `Vagas_MostraBlocoNeutro…`) |
| M3 Serviços mostram o preço quando existe | 2 (`Servicos_NuncaMostramPreco…`, `Servicos_MesmoComPrecoNaEntrada…`) |
| M4 Nome acessível sem o valor | 7 |
| M5 `loading` sempre `lazy` | `PrimeiraLinha_CarregaAImagemJa` |
| M6 Vagas sem o rótulo "Salário" | 3 (`ValorPorGrupo…`, `Vagas_MostraBlocoNeutro…`, página de componentes) |
| (extra) M7 Página de componentes aberta fora de Development | `PaginaDeComponentes_EmProduction_E404…` |
| (extra) M8 Área obrigatória na categoria errada | 2 (`Imoveis_ObrigatoriosPorCategoria`, `Terrenos_Exigem…`) |
| (extra) M9 Miniatura que amplia fotos pequenas | `Miniatura_Endereco_E_Tamanho` |
| (extra) M10 `RequiredFieldsFor` ignorando a regra por categoria | 2 (mesmos de M8) |
| (extra) M11 Asterisco do formulário ignorando a categoria | sobreviveu na primeira rodada; o teste novo `Area_TemAsteriscoDeObrigatorioSoEmTerrenos` o derruba |

M7 não compilava na primeira forma (parâmetro sem uso é erro com `TreatWarningsAsErrors`) e foi refeita. Todas foram desfeitas; a compilação final ficou limpa (0 avisos, 0 erros).

**O que cada camada prova**

- Valor (S29): 6200000 → "R$ 62.000", 249990 → "R$ 2.499,90", 50 → "R$ 0,50", 18000 → "R$ 180", teto "R$ 99.999.999,99". Sem preço ou tipo não há linha de valor (nunca "R$ 0").
- Card padrão: `<img>` com `width=480`, `height=360`, `alt` vazio e `loading` `lazy` (`eager` na primeira linha); um só link por card, com o nome "título, valor, cidade/UF". Serviços: Tipo no lugar do preço, mesmo que a entrada traga preço. Vagas: sem `<img>` mesmo com capa na entrada, bloco "Vaga de emprego" com a área e "Salário R$ 2.800". Sem capa: "Foto indisponível".
- Segurança: título e cidade com HTML saem codificados no texto e no atributo; nenhum `style` nem `on…=` em linha (CSP).
- Página de componentes: o Administrador vê 4 cards e o corpo (um `h1`, o título do corpo em `h3`); em Production é 404 para todos, até o Administrador logado e quem não entrou; em Development o Redator vai para "acesso negado" e quem não entrou, para a entrada; a rota não aparece no menu nem em `Routes.cs`.
- Navegador: os quatro cards têm a mesma altura de mídia (capa 4:3, bloco da vaga e bloco "Foto indisponível" idênticos); com a foto bloqueada pela rede os 3 cards com capa viram "Foto indisponível" sem mudar de tamanho; o foco desenha o contorno no card inteiro (`::after` do link) e o hover sublinha o título; axe sem violações e sem rolagem horizontal em 320, 768, 1024 e 1280 px, com 2 colunas em 320 px e 4 em 1280 px; o mesmo teste em Production recebe 404 também logado.
- Terrenos: a lista de pendências traz "Informe a área"; Apartamentos, Casas e Comércio não; o formulário mostra o asterisco só em Terrenos.

**Achados desta rodada**

1. **Página de componentes e E2E:** como a rota não existe em Production, os E2E usam um segundo processo da mesma saída publicada em Development (porta 5444, `GAZETA_DEV_BASE_URL`); o procedimento está em `docs/RODAR-TESTES-DE-INTEGRACAO-E-E2E.md`.
2. **Wireframe × design-system:** o nome acessível de Serviços é "Tipo:" no wireframe e "Serviço:" no `design-system.md`; foi seguido o wireframe (BACKLOG).
3. **SPEC v1.3:** a obrigatoriedade da área em Terrenos entrou no Apêndice B e no histórico de revisões.
4. **Verificação visual:** capturas em 1280 e 360 px da página de componentes conferidas à mão (cards alinhados, bloco neutro do mesmo tamanho da capa, corpo legível).
5. **`dotnet format` aplicado só aos arquivos da tarefa** (`--include`).

## Checkpoint 3 — Anúncios completos (fechamento da Fase 3)

> **Em resumo:** os três itens do checkpoint foram provados no site publicado e no SQL Server real. A jornada rascunho → fotos → envio para revisão passa para **Carros, Serviços e Vagas**; o HEIC chega como WebP, o GPS não aparece em nenhuma versão entregue e os originais não têm rota; o teste diferencial das colunas calculadas passa. A verificação achou **uma falha real** no formulário (preço digitado durante a troca de categoria se perdia), corrigida. Suítes: 1.210 unitários, 43 e 27 das ferramentas, 102 de integração e 71 de navegador, todas verdes. Veredito: **aprovado**.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.210 | 1.210 | 0 |
| Ferramenta de catálogo (`VehicleCatalogExport.Tests`) | 43 | 43 | 0 |
| Ferramenta de municípios (`CitiesImport.Tests`) | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 102 | 102 | 0 |
| E2E (Playwright: Production + Development) | 71 | 71 | 0 |

| Item do checkpoint | Prova |
|---|---|
| Rascunho com fotos e envio para revisão: Carros | `Carros_RascunhoComFoto_SemFotoNaoEnvia_ComFotoVaiParaRevisao`: preenche marca, modelo, ano, versão e km; sem foto o envio mostra só "Adicione ao menos 1 foto" e a situação continua Rascunho; com foto vai a Em revisão (conferido ao reabrir) |
| Serviços | `Servicos_SemPreco_ComDuasFotos_VaiParaRevisao`: sem campo de preço, duas fotos, envio |
| Vagas | `Vagas_SemFotos_ComSalario_VaiParaRevisaoSemExigirFoto`: a seção de fotos não existe, "Salário" com máscara, envio sem foto |
| HEIC convertido, GPS removido | `Fotos_JpegComGpsEHeic_ChegamComoWebpSemGps…`: um JPEG com GPS montado à mão (latitude presente nos bytes enviados) e o `sample.heic` sobem pela tela; as duas versões (480 e 1600) de cada foto chegam como `image/webp` (`RIFF…WEBP`), sem bloco `EXIF` nem `XMP ` e sem a latitude nos bytes |
| `_originals/` sem rota | 8 caminhos (`/fotos/{ad}/_originals/…`, `/_originals/…`, `-original.webp`, `.jpg`, `..%2F`) respondem 404; quem não entrou também recebe 404 nas fotos de um rascunho |
| Teste diferencial das colunas calculadas | `ComputedColumnsDifferentialTests` isolado, 4 testes, SQL Server real: Carros (33), Motos (36), Caminhões e Ônibus (34, 35) e Imóveis (26, 27, 30, 31), todas as classes de entrada; o teste de completude garante que nenhum grupo com filtro fica de fora |

| Mutação | Teste que caiu |
|---|---|
| MA Vagas passam a exigir foto | `Vagas_SemFotos_ComSalario_…` |
| MB Serviços passam a exigir preço | `Servicos_SemPreco_ComDuasFotos_…` |
| MC Envio sem conferir a foto exigida | `Carros_RascunhoComFoto_SemFotoNaoEnvia_…` |
| (extra) MD O foco volta sempre à categoria depois da troca | `TrocaDeCategoria_PrecoDigitadoEnquantoOServidorResponde_…` |
| (extra) Desfazer a correção do `ad-edit.js` | o mesmo teste |

Cada mutação derrubou só o teste esperado; os arquivos foram restaurados e o site, publicado de novo no estado final.

**Falha real encontrada e corrigida (lacuna do checkpoint)**

- **O preço digitado enquanto o servidor devolve os campos da nova categoria se perdia.** O `ad-edit.js` lia os valores do formulário no instante da troca e os repunha depois da resposta; o que a pessoa digitasse nesse intervalo (alguns milissegundos normalmente; vários segundos no primeiro pedido depois de o site subir) era sobrescrito. O Carros do checkpoint falhava sempre logo após reiniciar o site.
- **Correção mínima:** os valores passam a ser lidos **com a resposta já na mão**, e o foco continua no campo em que a pessoa digitou (nos demais casos volta à categoria, como a S09 pede). Teste novo: `TrocaDeCategoria_PrecoDigitadoEnquantoOServidorResponde_NaoSePerde_ESeguePodendoDigitar` (atraso de 1,5 s na resposta; sem a correção ele cai).
- A primeira versão da correção mantinha o foco no campo ativo mesmo sem digitação e quebrou `US008S09`; foi ajustada para só preservar o foco de quem digitou depois da troca.

**Outros achados**

1. Os testes de navegador agem só depois de o script da página rodar (`multiple` do campo de fotos) e de a rede assentar; sem isso, logo depois de o site subir, `SetInputFiles` caía na página sem script.
2. `FakeViaCep` virou classe compartilhada nos E2E novos; as cópias antigas seguem (BACKLOG).
3. Repetir o mesmo conjunto de E2E dezenas de vezes seguidas esgota o limite de entradas do painel (5 por 15 minutos por IP) e a entrada passa a estourar o tempo; não ocorre numa rodada normal.
4. A triagem dos 112 itens abertos do BACKLOG está em `plans/BACKLOG-TRIAGEM-FASE-3.md` (antes do lançamento, Fases 4/5, decisão do Product Owner).

## Tarefa 4.1 — fila de revisão e pré-visualização (US-010)

> **Em resumo:** 1.234 testes unitários (24 novos), 43 e 27 das ferramentas, 103 de integração (1 novo, SQL Server real) e 74 de navegador (3 novos) passam. Das 10 mutações, 9 foram derrubadas e 1 é um mutante equivalente (o desempate por id da fila). A S02 fica **coberta em parte** de propósito: a pré-visualização mostra só "Editar"; "Publicar" e "Rejeitar" chegam na 4.2 e "Arquivar" na 4.3. Veredito: **aprovado**.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.234 | 1.234 | 0 |
| Ferramenta de catálogo (`VehicleCatalogExport.Tests`) | 43 | 43 | 0 |
| Ferramenta de municípios (`CitiesImport.Tests`) | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 103 | 103 | 0 |
| E2E (Playwright: Production + Development) | 74 | 74 | 0 |

| Mutação | Testes que caíram |
|---|---|
| M1 Fila sem filtrar por situação | `US010S01`, `US010S06` |
| M2 Ordem invertida (mais novo primeiro) | `US010S01`, desempate e reenvio |
| M3 Ordenar pela criação em vez da data de envio | desempate e reenvio, `US010S01` |
| M4 Fila aberta ao Redator (sem a política de Administrador) | `US010S09` |
| M5 Selo de Cidade/UF manual sempre visível | `US010S02`, `PreVisualizar_CidadeEUfManuais…` |
| M6 Pré-visualização de anúncio fora de revisão com a faixa e as ações | `PreVisualizar_AnuncioForaDeRevisao…` |
| (extra) M7 Sem o desempate por id | **sobreviveu** (equivalente, ver abaixo) |
| (extra) M8 Características sem separador de milhar | 3 (`Carros_Usa…`, `HorasDeUso…`, `US010S02`) |
| (extra) M9 Características não omitem campo vazio | 6 |
| (extra) M10 Marca mostrada como id em vez do nome do catálogo | `US010S02` |

Os arquivos foram restaurados depois de cada mutação; a compilação final ficou limpa (0 avisos, 0 erros).

**O que cada camada prova**

- **S01:** três anúncios Em revisão gravados fora de ordem saem do mais antigo ao mais novo; cada linha mostra título (link para a pré-visualização), autor, categoria e data (dd/mm/aaaa, hora no `title`); Rascunho, Publicado, Rejeitado e Arquivado ficam de fora; o total aparece no texto e na aba "Fila de revisão (3)". Um anúncio rejeitado e reenviado vale pela data do último envio.
- **S06:** fila vazia mostra "Nenhum anúncio aguardando revisão", sem tabela, com a aba "(0)".
- **S09:** o Redator recebe "acesso negado" ("Você não tem permissão para acessar esta página", sem botões de decisão) na fila e na pré-visualização; sem login vai para a entrada. No navegador, uma conta de Redator criada pela tela de usuários (com troca da senha provisória) recebe o mesmo.
- **S02 (em parte):** faixa "Pré-visualização — ainda não publicado" logo abaixo do `h1`, título do anúncio em `h2`, valor, local, descrição com quebras de linha, características reais (Marca "Honda", Modelo "Civic", Versão "LX", "45.000 km"), destaque e 3 miniaturas (links para a versão grande), contato "Fale com a Gazeta" com "(11) 91234-5678", `tel:` e WhatsApp, e o botão "Editar". Nenhum "Publicar", "Rejeitar" ou "Arquivar" existe.
- Vagas mostra o bloco "Vaga de emprego" com as áreas (sem `<img>`, "Salário R$ 2.800", área fora das características); Serviços mostra o Tipo no lugar do preço; sem telefone do site, aviso com link para "Configurações"; o selo "Cidade/UF informadas manualmente (CEP não conferido)" só aparece quando a localização foi manual; anúncio fora de revisão só informa "Este anúncio não está em revisão. Situação: …"; anúncio inexistente é 404; título e descrição com HTML saem codificados.
- **Falha ao ler a fila:** 503, "Não foi possível carregar os anúncios", "Tentar novamente" e código de referência, sem a mensagem da exceção.
- **SQL Server real:** a consulta EF (junção com o autor, filtro e ordem) devolve a ordem esperada e a página abre.
- **Navegador:** o anúncio enviado pela tela aparece na fila, a pré-visualização carrega a foto de verdade (entregue ao Administrador mesmo sem estar publicado), "Editar" leva ao formulário e o link "Fila de revisão" volta; axe sem violações e sem rolagem horizontal em 1280, 1024, 768 e 320 px, com as linhas empilhadas em 320 px e em tabela em 1280 px.

**Achados desta rodada**

1. **Mutante equivalente (M7):** `ORDER BY data, id` e `ORDER BY data` dão o mesmo resultado no SQLite e no SQL Server de teste, porque o desempate natural já é pelo id. O desempate fica no código como garantia de ordem estável (BACKLOG).
2. **Fila grande:** o banco do E2E acumulou centenas de anúncios Em revisão; a página renderiza todos (sem paginação, decisão aprovada) e o axe passa.
3. **Unidades das características:** só km e horas levam a unidade no valor; área e medidas já a trazem no rótulo.
4. **Menu:** o item "Anúncios" já ficava ativo na fila, então `PanelMenu` não precisou mudar.
5. **`AdSpec` foi para o Core** (a 5.2 usa a mesma regra); a classe antiga da camada web foi removida.
6. O E2E do Redator deixa uma conta nova por rodada no banco de teste (BACKLOG).

## Tarefa 4.2 — publicar e rejeitar anúncios (US-010)

> **Em resumo:** 1.253 testes unitários (19 novos), 43 e 27 das ferramentas, 105 de integração (2 novos, SQL Server real) e 79 de navegador (5 novos) passam. As 9 mutações (7 planejadas e 2 extras) foram derrubadas. Duas partes da S04 ficam **para a Fase 5** de propósito ("vê o anúncio entre os mais recentes" e "o encontra na busca" dependem da home e da busca). Veredito: **aprovado**.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.253 | 1.253 | 0 |
| Ferramenta de catálogo (`VehicleCatalogExport.Tests`) | 43 | 43 | 0 |
| Ferramenta de municípios (`CitiesImport.Tests`) | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 105 | 105 | 0 |
| E2E (Playwright: Production + Development) | 79 | 79 | 0 |

| Mutação | Testes que caíram |
|---|---|
| M1 Publicar sem conferir o telefone do site | `US010S08`, clique duplo em paralelo |
| M2 Rejeitar sem exigir o motivo | `US010S05` (3 motivos vazios) e mais 4 |
| M3 Publicar anúncio que não está em revisão | `Decidir_AnuncioQueNaoEstaEmRevisao…` |
| M4 Frase "já foi publicado" trocada com "por outro administrador" | `MesmoAdministradorDuasVezes…`, `US010S07`, `Decidir_AnuncioQueNaoEstaEmRevisao…` |
| M5 Motivo gravado sem aparar os espaços | `US010S04`, `Rejeitar_GravaQuemQuandoEOMotivoSemEspacos` |
| M6 Decisão sem auditoria | 23 (entre eles `US010S03`, `Publicar_RegistraUmaAuditoria…`) |
| M7 Redator autorizado a decidir (política da fila trocada) | `US010S09`, `Redator_NaoAbreAFila…`, `Redator_NaoDecide…` |
| (extra) E1 Publicar sem exigir o token antiforgery | `SemLogin_SemTokenEAnuncioInexistente_SaoRecusados` |
| (extra) E2 `PublishedById` não gravado | `US010S03`, `MesmoAdministradorDuasVezes…`, `Decidir_AnuncioQueNaoEstaEmRevisao…` |

Os arquivos foram restaurados depois de cada mutação; a compilação final ficou limpa (0 avisos, 0 erros).

**O que cada camada prova**

- **S03:** a página de confirmação ("Publicar este anúncio?", "Ele passa a aparecer no site para todos os visitantes.") leva ao POST; a situação vira Publicado, `PublishedAt` e `PublishedById` ficam gravados, o anúncio sai da fila e a fila mostra "Anúncio publicado". Antes de publicar o visitante recebe 404 na foto; depois, 200 com `image/*` (no HTTP e no navegador).
- **S04 (em parte):** a situação vira Rejeitado, quem, quando e o motivo (aparado) ficam gravados, o anúncio não aparece ao público e o autor lê "Este anúncio foi rejeitado. Motivo: …" na tela de edição, inclusive depois de recarregar. A auditoria `ad.reject` guarda o motivo; ao reenviar, o motivo sai do anúncio e o histórico fica na auditoria. "Vê entre os mais recentes" e "encontra na busca" ficam para a Fase 5.
- **S05:** motivo vazio, só espaços ou só quebras de linha mostram "Informe o motivo da rejeição" na própria página, devolvem o texto digitado, deixam o anúncio Em revisão e levam o foco ao campo. Exatamente 500 caracteres passa; 501 recusa com a frase do limite. HTML no motivo sai codificado.
- **S07:** dois administradores, nas duas ordens: quem chega depois vê "Este anúncio já foi publicado por outro administrador" (ou "rejeitado") e a situação continua a da primeira decisão; o mesmo administrador duas vezes vê "Este anúncio já foi publicado". No SQL Server real, 4 pedidos simultâneos × 3 rodadas, para publicar e para rejeitar: exatamente uma passagem, uma auditoria e nenhum 500. No navegador, duas sessões da mesma conta reproduzem o aviso.
- **S08:** sem telefone do site, publicar volta para a pré-visualização com o aviso e o link "Configurações", e a situação não muda. Provado só nos testes HTTP (o banco do E2E já tem telefone).
- **Pendências reconferidas:** anúncio em revisão editado sem foto ou sem preço não vai ao ar ("Faltam N itens…").
- **Acesso:** Redator recebe "acesso negado" no GET e no POST sem alterar nada; sem login vai para a entrada; sem token antiforgery é 400; anúncio inexistente é 404; Rascunho, Publicado, Rejeitado e Arquivado dão a mensagem da situação e nada muda.
- **Acessibilidade:** axe sem violações na confirmação, na rejeição (também com o erro) e na pré-visualização em 1280 e 320 px, sem rolagem horizontal.

**Achados desta rodada**

1. **Contraste:** o botão "Rejeitar" (`btn-outline-danger`) dava 4,14:1 sobre o fundo cinza; o tema ganhou um vermelho mais escuro para todo `btn-outline-danger` (BACKLOG).
2. **Foco no erro:** o atributo `autofocus` não levou o foco ao campo no navegador de teste; `ad-confirm.js` agora foca o campo com erro.
3. **Cache de CEP:** `CepE2ETests` falha numa segunda rodada no mesmo banco (as entradas `13015100` e `60000000` já estão no cache). Limpar `CepCache` antes da rodada completa; documentado em `docs/RODAR-TESTES-DE-INTEGRACAO-E-E2E.md` e no BACKLOG.
4. **Ferramenta de mutação:** a restauração do arquivo deixava a data de modificação mais antiga que a compilação e o MSBuild não recompilava; o script passou a atualizar a data. As mutações M1 e M6 foram refeitas em separado depois da correção.
5. **Cobertura parcial declarada:** S02 agora completa (Publicar, Rejeitar e Editar; "Arquivar" na 4.3); S04 sem as duas frases da Fase 5.

## Tarefa 4.3 — despublicar e arquivar anúncios (US-011)

> **Em resumo:** 1.273 testes unitários (20 novos), 43 e 27 das ferramentas, 108 de integração (3 novos, SQL Server real) e 83 de navegador (4 novos) passam. As 11 mutações (8 planejadas e 3 extras) foram derrubadas. As partes de S01, S02 e S05 que dependem da busca, do endereço antigo e da lista do painel ficam para a 4.4 e a Fase 5 de propósito. Veredito: **aprovado**, com um teste instável antigo registrado (ver achado 2).

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.273 | 1.273 | 0 |
| Ferramenta de catálogo (`VehicleCatalogExport.Tests`) | 43 | 43 | 0 |
| Ferramenta de municípios (`CitiesImport.Tests`) | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 108 | 108 | 0 |
| E2E (Playwright: Production + Development) | 83 | 83 | 0 |

| Mutação | Testes que caíram |
|---|---|
| T1 Despublicar leva a Arquivado em vez de Rascunho | 9 (`US011S01`, clique duplo, `Despublicar_AnuncioQueNaoEstaPublicado…`, autor reenvia…) |
| T2 O GET da confirmação já arquiva | `US011S02`, `US011S03` |
| T3 Despublicar e arquivar abertos ao Redator (política trocada) | `US011S07` (3 situações) |
| T4 A barra aparece em anúncio Arquivado | `US011S06` |
| T5 A barra aparece para o Redator autor | `US011S07` (3 situações) |
| T6 Retirada sem auditoria | 31 (entre eles `US011S01`, `US011S02`, `US011S05`) |
| T7 Despublicar vale para anúncio que não está Publicado | 8 (`Despublicar_AnuncioQueNaoEstaPublicado…` e outros) |
| T8 A foto continua chegando ao visitante depois de arquivar | `US011S02`, `AnuncioNaoPublicado_Devolve404…`, `AnuncioNaoPublicado_OAutorEOAdministradorVeem…` |
| (extra) E1 Arquivar sem exigir o token antiforgery | `SemLogin_SemToken_EAnuncioInexistente_SaoRecusados` |
| (extra) E2 "Arquivar" some do anúncio não publicado | `BarraDeAcoes_PorSituacao…`, `US010S02` |
| (extra) E3 "Cancelar" sem o foco inicial | `US011S03` |

Os arquivos foram restaurados depois de cada mutação (com a data de modificação atualizada, como corrigido na 4.2); a compilação final ficou limpa (0 avisos, 0 erros).

**O que cada camada prova**

- **S01:** a página "Despublicar este anúncio?" explica que o anúncio sai do site e volta a Rascunho; depois do POST a situação é Rascunho, `PublishedAt` e `PublishedById` ficam vazios, a tela de edição mostra "Anúncio despublicado" uma vez só e a foto deixa de chegar ao visitante (200 antes, 404 depois, no HTTP e no navegador). O autor (Redator) volta a editar e consegue enviar o anúncio de novo para a revisão. A auditoria `ad.unpublish` guarda quem, "Publicado" e "Rascunho".
- **S02 (em parte):** o aviso "O anúncio sairá do site e não poderá ser reativado." aparece; depois da confirmação a situação é Arquivado, `ArchivedAt` fica gravado, a lista mostra "Anúncio arquivado", a foto sai do ar e a auditoria `ad.archive` guarda quem e as duas situações. O filtro "Arquivado" da lista é da 4.4; "visitante abre o endereço antigo" é da Fase 5.
- **S03:** abrir a confirmação (GET) não grava nada; "Cancelar" é um link de volta, vem antes do botão de confirmar e leva o foco inicial. A situação continua Publicado e a foto continua no site; nenhuma auditoria nova.
- **S05 (em parte):** Rascunho, Em revisão e Rejeitado vão a Arquivado, cada um com a situação de partida na auditoria; o Em revisão sai da fila. "Some da lista padrão" é da 4.4. No navegador, a pré-visualização de um anúncio em revisão ganhou o botão "Arquivar".
- **S06:** o anúncio arquivado abre em somente leitura ("Este anúncio não pode ser editado"), com "Situação: Arquivado" e sem a barra de retirada; GET e POST de despublicar e arquivar levam de volta à tela do anúncio com a frase da situação, sem gravar nada.
- **S07:** o Redator não vê a barra em Rascunho, Rejeitado e Publicado; GET e POST pela URL dão "acesso negado" e nada muda.
- **Barra por situação:** Rascunho, Rejeitado e Em revisão mostram só "Arquivar"; Publicado mostra "Despublicar" e depois "Arquivar"; a pré-visualização fora de revisão só informa; anúncio novo não tem barra.
- **Frases novas:** "Este anúncio não está mais publicado. Situação: …" (despublicar fora de Publicado, com 4 situações) e "Este anúncio já foi arquivado".
- **Clique duplo e dois administradores:** a segunda resposta diz o que já aconteceu e só há uma auditoria; quem chega depois de um despublicar ainda consegue arquivar (Rascunho → Arquivado), e quem chega depois de arquivar vê "Situação: Arquivado".
- **SQL Server real:** 4 despublicações, 4 arquivamentos e 2+2 misturados ao mesmo tempo, em 3 rodadas: nenhum 500; despublicar termina Rascunho com uma auditoria; arquivar termina Arquivado com uma auditoria de arquivar e no máximo uma de despublicar.
- **Navegador:** despublicar, cancelar e arquivar, arquivar em revisão pela pré-visualização, foco inicial em "Cancelar", axe sem violações e sem rolagem horizontal em 1280 e 320 px (barra, as duas confirmações e o anúncio arquivado), com "Arquivar" abaixo de "Despublicar" em 320 px.

**Achados desta rodada**

1. **TempData de uma leitura:** o aviso de sucesso e o de "já foi arquivado" são do `TempData` da sessão; dois pedidos seguidos mostram só o último aviso. Os testes leem a página logo depois de cada pedido (BACKLOG).
2. **Teste instável antigo:** `SubmitForReviewTests.CliqueDuploEmParalelo…` (3.7, unitários) falha às vezes com duas auditorias (1 vez em 6 rodadas isoladas; caiu em 3 das 11 mutações sem relação). O SQLite compartilhado não garante a corrida; a prova real é a integração no SQL Server. Isso também explica por que a M1 da 4.2 listou esse teste entre os que "caíram": a queda real daquela mutação é `US010S08`. Registrado no BACKLOG para decisão.
3. **Docker:** o `dockerd` e o contêiner do E2E pararam no meio da sessão e foram religados (já descrito na documentação).
4. **Cobertura parcial declarada:** S01 e S02 sem "busca" e "endereço antigo" (Fase 5); S02 e S05 sem a lista com filtro (4.4); S04 (favoritos) é da Fase 5.

## Tarefa 4.4 — lista de anúncios do painel (US-012)

> **Em resumo:** 1.287 testes unitários (15 novos e 1 removido), 43 e 27 das ferramentas, 117 de integração (9 novos, SQL Server real) e 88 de navegador (5 novos) passam. As 15 mutações (as 9 planejadas, em variantes de serviço e de SQL, mais o tempo limite e 2 extras) foram derrubadas. O plano de execução usa o índice de autor, então não houve migration. Veredito: **aprovado**.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.287 | 1.287 | 0 |
| Ferramenta de catálogo (`VehicleCatalogExport.Tests`) | 43 | 43 | 0 |
| Ferramenta de municípios (`CitiesImport.Tests`) | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 117 | 117 | 0 |
| E2E (Playwright: Production + Development) | 88 | 88 | 0 |

| Mutação | Testes que caíram |
|---|---|
| Q1 Redator sem o filtro de autoria (serviço) | `US012S01`, `ParametrosForaDaLista…` |
| Q1b Redator sem o filtro de autoria (SQL) | `US012S01_S02_Autoria…`, página inteira do Redator, plano do índice |
| Q2 Arquivados aparecem por padrão (serviço) | `US012S03` |
| Q2b Arquivados aparecem por padrão (SQL) | `US012S03_FiltroPorSituacao…`, página inteira |
| Q3 Busca sem normalizar | `US012S04_Buscar_NormalizaOTermo…` |
| Q4 Busca com `LIKE` e curingas sem escape | `US012S04_Busca_SemAcento…` (`%`, `_`, `[`) |
| Q5 Página com deslocamento errado (SQL) | 7 (paginação, ordem, página inteira) |
| Q5b Página além do fim não volta para a última (serviço) | `Paginacao_PaginaForaDoIntervalo…` |
| Q6 Total ignorando todos os filtros | `US012S01_S02_Autoria…`, `US012S03_FiltroPorSituacao…` |
| Q7 Filtro de situação ignorado | `US012S03_FiltroPorSituacao…`, página inteira |
| Q8 Motivo da rejeição some da linha | `Rejeitado_MostraOMotivoInteiro…` |
| Q9 Ordem trocada (id crescente) | `Ordem_ComAMesmaData…`, `US012S07`, página inteira |
| Q10 Tempo limite de 30 s em vez de 10 s | `ConsultaQueEstouraOTempo…` |
| (extra) E1 "Em revisão" do Administrador abre a edição | `US012S05` |
| (extra) E2 Redator vê a coluna "Autor" | `US012S01` |

O Q1 foi refeito uma vez porque a primeira forma da mutação não compilava (o parâmetro `currentUser` ficava sem uso); os arquivos foram restaurados depois de cada mutação, com a data de modificação atualizada, e a compilação final ficou limpa (0 avisos, 0 erros).

**O que cada camada prova**

- **S01 e S02:** no SQL Server real, com 5 anúncios da Ana e 3 do Bruno, a consulta do Redator devolve exatamente os 5 dele e a do Administrador devolve os 8 com o nome do autor; na tela, o Redator não vê a coluna "Autor" e o Administrador vê as abas "Fila de revisão (n)" e "Todos os anúncios". Cada linha mostra título (link), categoria, situação em texto e "Alterado em" (dd/mm/aaaa no fuso de São Paulo, hora no `title`). A autoria vem da sessão: `?autor=` e `?authorId=` na URL não mudam a consulta.
- **S03:** filtrar por "Rejeitado" mostra só as rejeitadas e o total; a lista padrão esconde os arquivados e "Arquivado" os mostra (fecha o que a S02 e a S05 da US-011 deixaram para a 4.4); situação desconhecida não filtra.
- **S04:** "civic" mostra só o Honda Civic; o termo é aparado, sem acento, sem diferença de maiúsculas e sem espaços repetidos; passa de 100 caracteres, é cortado; `%`, `_` e `[` valem como texto (a busca usa `CHARINDEX`, sem curinga); texto de ataque (`'; DROP TABLE Ads; --`) é só texto e a tabela continua inteira.
- **S05:** Rascunho e Rejeitado abrem a edição; Em revisão abre a pré-visualização (Administrador) ou a leitura (Redator); Publicado e Arquivado abrem a tela do anúncio. No navegador, o Rascunho abre a edição e o Em revisão a pré-visualização.
- **S06:** o Redator sem anúncios vê "Você ainda não criou anúncios" e um único "Novo anúncio", sem filtros; o Administrador sem anúncios vê "Nenhum anúncio cadastrado ainda". Busca ou filtro sem resultado mostra "Nenhum anúncio encontrado" e "Limpar filtros", mantendo o formulário.
- **S07:** 45 anúncios saem em páginas de 20, 20 e 5, sem repetir nem perder nenhum, com o total em todas, do alterado mais recentemente ao mais antigo e com desempate pelo id; os links "Anterior", "Próxima" e das páginas preservam busca e situação, a atual leva `aria-current="page"`; página 0, negativa ou texto mostra a primeira e a 99 mostra a última. No navegador, o banco do E2E (mais de 20 anúncios) mostra 20 por página, a página 2 começa em outro anúncio e "Anterior" volta.
- **S08:** falha na leitura devolve 503 com "Não foi possível carregar os anúncios. Tente novamente.", o botão "Tentar novamente" leva ao mesmo endereço com os filtros e a tela não mostra mensagem da exceção, servidor nem pilha. No SQL Server real, uma conexão que tranca a tabela faz a leitura estourar o tempo limite de 10 s (RC-15) e o site responde 503 em cerca de 10 s; solto o bloqueio, a lista volta.
- **Motivo da rejeição:** a linha "Rejeitado" mostra o motivo inteiro (500 caracteres, sem corte); outras situações não mostram motivo; título e motivo com HTML saem codificados.
- **Plano de execução (D7):** com 4.000 anúncios, a consulta do Redator usa `IX_Ads_AuthorId_Status_UpdatedAt` e não faz varredura inteira da tabela; nenhuma migration foi necessária.
- **Navegador:** busca sem acento, filtro por situação, "Limpar filtros", arquivados escondidos até filtrar, o mesmo fluxo com JavaScript desligado (formulário GET), axe sem violações e sem rolagem horizontal em 1280, 768 e 320 px (lista, página 2, filtro e sem resultado), com as linhas virando cartões em 320 px.

**Achados desta rodada**

1. **Contraste:** "Limpar filtros" (`btn-outline-secondary`) dava 4,29:1 sobre o fundo cinza; o tema ganhou `#4a5568` para todo `btn-outline-secondary` (BACKLOG).
2. **Guarda do filtro único:** o teste `FragmentoSomentePublicados_E_UnicoEReutilizado` recusa filtros de situação reescritos fora de `SqlFragments`; os filtros "esta situação" e "sem arquivados" da lista foram para `SqlFragments`, mantendo o filtro de situação num lugar só.
3. **Hospedeiro de teste:** o repositório Dapper (T-SQL) não roda no SQLite; o hospedeiro dos testes registra por padrão `StubPanelAdListRepository` (devolve linhas fatiadas por página, guarda a consulta, não filtra). Sem isso 20 testes antigos, que entram em "Meus anúncios" ao fazer login, caíram em 503.
4. **E2E instável de montagem:** uma vez em ~8 rodadas completas o auxiliar que monta o anúncio perdeu a descrição digitada logo depois de trocar a categoria; ele agora espera a rede ficar parada (BACKLOG).
5. **Teste removido (D8):** `SubmitForReviewTests.CliqueDuploEmParalelo…` (instável); a corrida de verdade continua provada em `SubmitForReviewConcurrencyTests`.
6. **Dois pontos de volume registrados:** a busca não usa índice (o termo pode estar no meio do título) e a lista do Administrador ordena por `COALESCE(UpdatedAt, CreatedAt)`; revisar com volume real (BACKLOG).

## Checkpoint 4 — Revisão completa (fechamento da Fase 4)

> **Em resumo:** os três itens do checkpoint foram provados com contas e sessões de verdade, no SQL Server real e no site publicado: o ciclo de vida inteiro do anúncio funciona com os dois papéis, a decisão simultânea entre dois Administradores deixa uma só decisão e uma só auditoria (quem perde lê "por outro administrador"; o código é 302 com mensagem, não 409, por decisão de 2026-10-05), e cada ação fica em `AuditEntries` com o ator e as situações. Nenhum código de produto mudou. O `/review` da Fase 4 deu **APPROVE com condições**: 0 críticos, 6 avisos e 20 sugestões, todos registrados no BACKLOG e classificados em `plans/BACKLOG-TRIAGEM-FASE-4.md`. Veredito: **aprovado**, com 6 avisos 🟡 para o Product Owner corrigir ou aceitar antes do `/scan`.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.289 | 1.289 | 0 |
| Ferramenta de catálogo (`VehicleCatalogExport.Tests`) | 43 | 43 | 0 |
| Ferramenta de municípios (`CitiesImport.Tests`) | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 120 | 120 | 0 |
| E2E (Playwright: Production + Development) | 90 | 90 | 0 |

| Item do checkpoint | Prova |
|---|---|
| Fila, publicar, rejeitar, despublicar e arquivar com os dois papéis | `Checkpoint4E2ETests.CicloDeVida_…` no site publicado: o Administrador cria uma conta de Redator pela tela de usuários; o Redator cria e envia o anúncio e o vê "Em revisão" em "Meus anúncios" (sem a coluna "Autor"); o Administrador o rejeita com motivo; o Redator vê "Rejeitado" com o motivo na lista e na edição, corrige e reenvia (`@US-009-S04` de ponta a ponta, pendente desde a 3.7); o Administrador publica; o Redator vê "Publicado", só leitura e sem ações de retirada; o Administrador despublica, o Redator reenvia, o Administrador publica de novo e arquiva; o arquivado some da lista padrão do Redator, aparece ao filtrar por "Arquivado" e abre só para leitura; a foto chega ao visitante (200) só enquanto o anúncio está Publicado e dá 404 em Em revisão, despublicado e arquivado; o Redator na fila recebe "acesso negado" |
| Decisão simultânea | `Checkpoint4LifecycleTests.DoisAdministradores_PublicamOMesmoAnuncio…` e `…_RejeitamOMesmoAnuncio…`, SQL Server real: 2 contas de Administrador em sessões separadas × 4 pedidos de cada, em 3 rodadas, para publicar e para rejeitar. Resultado: todos respondem 302, o anúncio termina na situação da decisão, **uma só** auditoria, o ator dela é quem venceu (`PublishedById`/`RejectedById`) e quem perdeu lê "Este anúncio já foi publicado/rejeitado por outro administrador". No navegador, `Checkpoint4E2ETests.DoisAdministradores_…`: uma conta de Administrador criada pela tela, duas sessões abrem a confirmação, a primeira publica, a segunda confirma depois e lê a mensagem na pré-visualização; a foto continua pública |
| Ações em `AuditEntries` | `Checkpoint4LifecycleTests.CicloDeVidaCompleto_…`, SQL Server real, os dois papéis e dois Administradores: 8 linhas, na ordem, cada uma com ator e situações (`ad.submit` Redator Rascunho → Em revisão; `ad.reject` Administrador A com "Rejeitado — motivo: …"; `ad.submit` Rejeitado → Em revisão; `ad.publish` A; `ad.unpublish` Administrador B Publicado → Rascunho; `ad.submit`; `ad.publish` B; `ad.archive` A Publicado → Arquivado), todas com sucesso; reenviar um rejeitado limpa o motivo no anúncio (o histórico fica na auditoria); pedidos negados (Redator em publicar, rejeitar, despublicar e arquivar; pedidos sem token) não gravam nada |
| Matriz de permissões (NFR-13) | `PermissionMatrixTests`: GET de lista, fila, pré-visualização, confirmar publicar, motivo da rejeição, confirmar despublicar e confirmar arquivar × {sem login → entrada, Redator → "acesso negado" (a lista abre), Administrador → 200}, sem alterar nada; POST de publicar, rejeitar, despublicar e arquivar: sem login → entrada, Redator com token → "acesso negado", Administrador sem token → 400, Administrador com token grava uma auditoria |

| Mutação | Teste que caiu |
|---|---|
| C1 `ad.unpublish` sem o ator na auditoria | `CicloDeVidaCompleto_…` |
| C2 O reenvio não limpa o motivo no anúncio | `CicloDeVidaCompleto_…` |
| C3 A foto de um anúncio despublicado continua entregue ao visitante | `Checkpoint4E2ETests.CicloDeVida_…` ("despublicado: a foto sai do ar", 200 em vez de 404) |
| (extra) C4 O GET de arquivar aberto ao Redator | `PermissionMatrixTests.Get_TodosOsEnderecosDaFase4_PorPapel` |
| (extra) C5 A auditoria de `ad.reject` sem o ator | `CicloDeVidaCompleto_…`, `DoisAdministradores_Rejeitam…` |

Os arquivos foram restaurados depois de cada mutação (`git diff` limpo); a compilação final ficou limpa (0 avisos, 0 erros) e o site do E2E foi publicado de novo no estado final.

**Revisão de código (`/review` do kit, `reports/CODE_REVIEW.md`)**

- **Veredito:** APPROVE com condições. Notas dos 5 eixos: Corretude 4, Legibilidade 4, Arquitetura 4, Segurança 4, Desempenho 4 (nenhum chega a 5 porque todos têm achado em aberto). Cobertura dos cenários: todos os de US-010 (S01 a S09), US-011 (S01 a S07) e US-012 (S01 a S08) têm caminho ligado ao app e teste que afirma o efeito observável; as partes de S03/S04 (US-010) e S01/S02 (US-011) que dependem da home, da busca e do endereço antigo, e a S04 da US-011 (favoritos), são da Fase 5.
- **🟡 (6):** `pagina=` muito grande estoura o `OFFSET` e dá 503; ramos de repetição por conflito de `RowVersion` sem teste determinístico; contrato "409" no plano, na ADR-004 e na ARCHITECTURE (ajustado nesta rodada, ver abaixo); cobertura numérica nunca medida; rótulo de texto usado como chave na pré-visualização; `catch` vazio em `AdSpecsReader`.
- **🟢 (20):** extrações de controller, CSS e auxiliar E2E; `!` e comentários; `Kind` da data do Dapper; cache de 1 ano da foto retirada; NVDA nas tabelas empilhadas; nomes de CSS/JS; volume. Cada um tem item no BACKLOG.
- **Ajustes de documento feitos aqui (decisão D1):** a ADR-004, a ARCHITECTURE e o checklist do Checkpoint 4 agora dizem "302 com a mensagem; 409 só para edição concorrente e API". O BACKLOG teve a linha colada separada e o item do teste instável removido marcado como resolvido.
- **Nada do código de produto foi corrigido nesta rodada**, como combinado ("só relatar"); os 6 avisos aguardam sua decisão.

**Achados desta rodada**

1. **Cobertura numérica:** não há pacote de cobertura instalado; medir pede `Microsoft.Testing.Extensions.CodeCoverage` (dependência nova, pelo processo de decisão de tecnologia). Fica como 🟡 4.
2. **Docker:** o `dockerd` e o contêiner do E2E pararam de novo no meio da rodada e foram religados (já na documentação).
3. **Auxiliar E2E copiado:** o Checkpoint 4 acrescentou uma versão com a página como parâmetro (5 cópias no total); a extração está no BACKLOG.

---

## Correções pós-Checkpoint 4 e cobertura de código (2026-10-05)

> **Em resumo:** as decisões do Product Owner depois do Checkpoint 4 foram executadas: três avisos do `/review` corrigidos com teste, o teste determinístico das repetições por conflito de versão escrito, o pacote de cobertura instalado e `ArchivedById` gravado (migration `AddArchivedBy`). **A cobertura passa das duas metas do Gate 6: 97,7% de linhas (meta 80%) e 90,9% de ramos (meta 75%).**

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.312 | 1.312 | 0 |
| Ferramenta de catálogo (`VehicleCatalogExport.Tests`) | 43 | 43 | 0 |
| Ferramenta de municípios (`CitiesImport.Tests`) | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 121 | 121 | 0 |
| E2E (Playwright: Production + Development, banco com a migration nova) | 90 | 90 | 0 |

**O que mudou**

| Item | Correção | Teste que prova |
|---|---|---|
| 🟡 1 `?pagina=2147483647` dava 503 | `PanelAdListFilters.MaxPage` (100.000) com `Math.Clamp` no serviço; `SqlBuilder.Page` conta em 64 bits e recusa o estouro | `PanelAdListTests.Paginacao_PaginaGigante_…` (a lista mostra a última página, sem 503), `SqlBuilderTests.Paginar_ContaQueEstouraOInt_…`, `PanelAdListQueryTests.Paginacao_PaginaNoLimiteDoPainel_NoSqlServer_…` |
| 🟡 5 rótulo usado como chave na pré-visualização | `AdSpec` ganhou `Key` e `Items`; `FieldKeys.JobAreas` e `FieldKeys.ServiceType` valem para os grupos e para a tela | `ReviewQueueTests.PreVisualizar_RotuloDoCampoMudou_…` (um leitor que renomeia todos os rótulos; o bloco "Vaga de emprego" e o tipo do serviço continuam no lugar), `AdSpecsTests.CadaLinha_GuardaAChaveDoCampo_…` |
| 🟡 6 `catch (NotFoundException)` vazio | `AdSpecsReader` registra `LogWarning` com o id do anúncio e o tipo do catálogo | `AdSpecsReaderTests` (a linha de registro, e nenhum aviso com o catálogo completo) |
| 🟡 2 repetições por conflito de `RowVersion` sem teste | só teste: `IAdService` roteirizado lança `ConflictException` na ordem combinada | `DecisionRetryTests` (11 casos: 1 conflito repete; 2 conflitos repassam ou dizem a situação; "por outro administrador"; edição no meio vira pendências; o contexto é limpo entre as tentativas) |
| `ArchivedById` | migration `AddArchivedBy` (coluna nula, FK sem cascata, índice `IX_Ads_ArchivedById`); `Ad.ApplyTransition` grava quem arquivou | `AdEntityTests` (4 situações de partida), `AdServiceTests`, `TakedownTests`, `Checkpoint4LifecycleTests` (mesmo ator da auditoria), `AdsSchemaTests`, `MigrationsTests.MigrationAddArchivedBy_…`, `ScriptTests` (12 migrations) |

Anúncios arquivados antes da migration ficam com `ArchivedById` nulo; o ator deles está na auditoria `ad.archive`.

**Mutações (13, todas mortas; arquivos restaurados depois de cada uma)**

| Mutação | Testes que caíram |
|---|---|
| M1 sem o limite de página | 1 (`Paginacao_PaginaGigante_…`) |
| M2 `SqlBuilder` sem a guarda de estouro | 1 |
| M3 áreas da vaga procuradas pelo rótulo | 1 (`RotuloDoCampoMudou_…`) |
| M4 tipo do serviço procurado pelo rótulo | 1 (mesmo teste) |
| M5 log removido de `AdSpecsReader` | 1 |
| M5b log em nível errado (Debug) | 1 |
| M6 `AdReview` sem repetição | 5 |
| M7 `AdTakedown` sem repetição | 3 |
| M8 `AdReview` sem `ChangeTracker.Clear` | 1 |
| M9 `AdTakedown` sem `ChangeTracker.Clear` | 1 |
| M10 `ArchivedById` não gravado | 5 |
| M11 `ArchivedById` com o ator errado (quem publicou) | 5 |
| M12 `AdTakedown`: perde a corrida e repassa o erro em vez de dizer a situação | 1 |

### Cobertura (Gate 6)

**Como foi medido:** pacote `Microsoft.Testing.Extensions.CodeCoverage` 18.11.2 (aprovado em 2026-10-05), escopo em `coverage.settings.xml` (só os projetos do site; ficam de fora migrações do EF Core, `Program.cs`, Razor compilado, código gerado e o que tem `[ExcludeFromCodeCoverage]`, `[GeneratedCode]`, `[CompilerGenerated]`, `[Obsolete]`). Cada projeto de teste gera um relatório Cobertura; a cobertura do site é a **união** dos dois (linha coberta por qualquer um). O ramo de uma linha vale o maior número de ramos cobertos entre os dois, então o número de ramos é um piso. Comando em `docs/RODAR-TESTES-DE-INTEGRACAO-E-E2E.md`. Os E2E não entram na conta (rodam contra o site publicado, fora do processo medido). Modo greenfield: o gate é o número do repositório inteiro.

| Medida | Unitários | Integração | **União (o gate)** | Meta | Resultado |
|---|---|---|---|---|---|
| Linhas | 97,1% (4.853 de 4.998) | 79,4% (3.970 de 4.998) | **97,7% (4.882 de 4.998)** | ≥ 80% | atendida |
| Ramos | 89,9% (1.282 de 1.426) | 51,9% (740 de 1.426) | **90,9% (1.296 de 1.426)** | ≥ 75% | atendida |

| Projeto | Linhas | Ramos |
|---|---|---|
| `GazetaMarketplace.Core` | 99,0% (2.376 de 2.400) | 95,3% (683 de 717) |
| `GazetaMarketplace.Infrastructure` | 96,7% (1.780 de 1.840) | 87,2% (333 de 382) |
| `GazetaMarketplace.Web` | 95,8% (726 de 758) | 85,6% (280 de 327) |

Só os unitários já passam das duas metas, então o gate não depende do Docker. Os 15 arquivos de menor cobertura de linhas estão no BACKLOG (nenhum foi corrigido, como combinado).

**Métodos a 0% (12 de 661):** nenhum é regra de negócio sem teste *que ainda não é chamada*; os que o código de produção já chama estão marcados para conferência.

| Método | Motivo |
|---|---|
| `AppDbContextFactory.CreateDbContext`, `SystemUser` (3 propriedades) | fábrica de tempo de projeto, só `dotnet ef` chama |
| `AppRole..ctor(string)` | exigido pela API do Identity, nunca chamado pelo site |
| `RecoveryTokenProvider.CanGenerateTwoFactorTokenAsync` | exigido pelo contrato do Identity (devolve `false`; o site não usa duplo fator) |
| `FieldLimits..cctor`, `AdFormFactory..cctor` | construtores estáticos (membro estrutural) |
| `AdFieldViewModel.HelpId` | propriedade de uma linha |
| `PasswordResetResult.BadLink`, `FieldValueParser.Invalid`, `UserManagement.NotFound` | **chamados por código de produção** (`PasswordRecoveryService`, `FieldValueParser`, `UserManagement`) e testados por esses caminhos; o 0% pode ser artefato da medição de método de uma linha. Conferir: se a conferência confirmar 0%, vira teste (regra "já está em uso") |

**Achado da rodada:** com a cobertura ligada, o teste de integração `PanelAdListQueryTests.ConsultaDoRedator_UsaOIndiceDeAutorSituacaoEData` falhou uma vez (o plano de execução não citou o índice); sem cobertura, a suíte inteira passou (121 de 121) e o teste também passou em duas rodadas isoladas. Registrado no BACKLOG como possível sensibilidade do plano à carga (tabela pequena, estatísticas); não é regressão do código.

**Ambiente:** o `dockerd` parou de novo e o contêiner do E2E foi religado; a primeira rodada do E2E falhou 90 de 90 por o Playwright procurar o navegador em `/opt/pw-browsers` (versão 1243 está em outra pasta); com `PLAYWRIGHT_BROWSERS_PATH` correto, 90 de 90 passaram.

---

## Tarefa 5.1 — página inicial e páginas de categoria (US-001, 2026-10-05)

> **Em resumo:** o visitante abre `/` e vê a busca, as categorias principais e os 12 anúncios publicados mais recentes; entra em `/categoria/{slug}` e vê subcategorias, caminho de navegação e os anúncios da categoria e de todas as descendentes, 24 por página. Os 8 cenários da US-001 estão provados (S01 a S08), os estados vazio, 404 e 503 funcionam e **o plano de execução dos recentes foi medido: sem índice novo ele varria a tabela inteira e ordenava; com o índice `IX_Ads_Status_PublishedAt_Id` lê só as 12 primeiras entradas**.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.335 | 1.335 | 0 |
| Ferramenta de catálogo | 43 | 43 | 0 |
| Ferramenta de municípios | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 127 | 127 | 0 |
| E2E (Playwright, site publicado com a migration nova) | 95 | 95 | 0 |

**Testes novos:** 22 unitários (`ShowcaseTests`), 6 de integração (`ShowcaseQueryTests`), 5 E2E (`ShowcaseE2ETests`), mais o teste da migration `AddPublishedAtIndex`.

| Cenário | Prova |
|---|---|
| S01 página inicial | `ShowcaseTests.US001S01_…` (categorias na ordem da árvore, 12 cards mesmo com 15 publicados, capa, título, preço, cidade/UF, busca no topo; variantes de Serviços e Vagas; só a primeira linha de imagens sem `lazy`) e `PaginasInteiras_Visitante_…` no SQL Server |
| S02 categoria principal | `US001S02_…`: subcategorias na ordem da árvore e o pedido de anúncios leva a categoria e **todas** as descendentes (inclusive as netas) |
| S03 subcategoria e caminho | `US001S03_…` (só os anúncios dela; caminho Início > categoria > subcategoria, o passo atual não é link; voltar traz os anúncios de todas as subcategorias; caminho de 3 níveis) e `ShowcaseE2ETests.US001S03_…` no navegador, com e sem JavaScript |
| S04 categoria sem anúncios | `US001S04_…`: mensagem e links das demais categorias principais, resposta 200 |
| S05 site sem anúncios | `US001S05_…`: categorias à mostra e "Em breve teremos novos anúncios" |
| S06 falha | `US001S06_…` (503, mensagem, "Tentar novamente", código de referência, nada técnico na tela, causa no registro; página inicial e categoria) |
| S07 categoria inexistente | `US001S07_…` (404 com mensagem e caminhos de volta; categoria excluída; slugs estranhos, com HTML ou gigantes, sem consulta e sem eco do texto) e E2E |
| S08 320 px | `US001S08_…` (grade de 2 colunas, sem largura fixa, busca → categorias → lista) e `ShowcaseE2ETests.US001S08_…` (sem rolagem horizontal; busca, categorias e card dentro dos 320 px) |
| Acessibilidade | `ShowcaseE2ETests.Acessibilidade_…`: axe sem violações na página inicial, categoria, subcategoria e 404, em 1280 e 320 px |

**Consulta (SQL Server real):** só publicados (rascunho, em revisão, rejeitado, arquivado e despublicado fora); ordem por data de publicação e, no empate, o de maior id; limite de 12; categorias descendentes; total sem duplicar por foto; capa = foto de menor posição (empate pelo menor id) e sem foto vem vazia; página no limite devolve vazio com o total certo; a linha da vitrine não tem autor nem e-mail.

**Decisão D4 (índice), medida:** com 6.000 anúncios (2.000 publicados) o plano dos mais recentes era `Clustered Index Scan` de `PK_Ads` + `Sort` (varredura completa a cada visita à página inicial). A migration `AddPublishedAtIndex` cria `IX_Ads_Status_PublishedAt_Id` (situação, data de publicação e id, os dois últimos em ordem decrescente) e o plano passa a ser `Index Seek` + `Top`, sem varredura e sem ordenar à parte; a categoria usa `IX_Ads_Status_CategoryId_PublishedAt`. Teste: `PlanoDeExecucao_ComMuitosAnuncios_…`.

**Achado e correção (teste de integração instável):** o teste do plano da lista do painel falhava de vez em quando porque a consulta que lê o plano pelo texto do SQL pegava a execução mais recente **de qualquer banco do servidor** (outro teste, tabela pequena, outro plano). Os dois testes de plano agora filtram pelo banco do teste (`DB_ID()`). Era o item do BACKLOG "sensibilidade do plano à carga".

**Mutações (12; 11 mortas na primeira rodada, o mutante equivalente foi tratado)**

| Mutação | Resultado |
|---|---|
| V1 categoria mostra anúncios não publicados | morta (2 testes de integração) |
| V2 ordem invertida | morta (3) |
| V3 13 recentes em vez de 12 | morta (S01) |
| V4 categoria sem as descendentes | morta (2) |
| V5 slug inexistente responde 200 | morta (7); a primeira versão da mutação não compilava (código inalcançável) e foi reformulada |
| V6 sem a mensagem do site vazio | morta (S05) |
| V7 página sem limite | morta |
| V8 a tela de erro mostra a mensagem técnica | morta (S06) |
| V9 capa é a última foto | morta (2) |
| V10 caminho sem a categoria principal | morta |
| V12 estado vazio sem links das categorias | morta (S04) |
| V13 primeira linha de imagens também `lazy` | morta |
| V11 slug sem limite de tamanho | **sobreviveu: mutante equivalente.** A guarda de tamanho era redundante (a busca no dicionário de slugs devolve nulo para qualquer texto inexistente e o servidor já limita o endereço); a guarda e `SlugMaxLength` foram removidas (YAGNI) e o teste do slug gigante continua provando o 404 sem consulta |

**Outros achados da rodada**

1. **A página inicial agora lê categorias e anúncios**, então os testes de layout que pedem `/` rodavam sem banco; passaram a usar o site de teste com banco (a página inicial agora lê categorias e anúncios).
2. **E2E do cache de CEP dependia da ordem de execução** (os outros E2E deixam `13015-100` no cache de CEP; ao acrescentar uma classe a ordem mudou e a primeira consulta vinha do cache). `CepE2ETests` passou a usar o CEP `13015-300`, só dele, e deixou de depender da ordem.
3. **Os cards já apontam para `/anuncio/{id}/{slug}`**, endereço que a página de detalhe (5.2) vai atender; até lá o clique leva a 404. Decisão de projeto: o endereço é o da ARCHITECTURE (NFR-21), não precisa mudar na 5.2.
4. **Docker:** o `dockerd` caiu de novo com o reinício do worker e foi religado; a primeira tentativa de integração falhou 127 de 127 por isso (ambiente).


## Tarefa 5.2 — detalhe do anúncio e galeria (US-003, 2026-10-05)

> **Em resumo:** `/anuncio/{id}/{slug}` mostra o anúncio publicado com galeria (destaque, miniaturas, contador, setas, teclado, toque e ampliação em janela nativa), características por grupo, local, data de publicação e descrição, e responde 404 com a mesma mensagem para tudo que não está publicado. Os 8 cenários da US-003 estão provados (S01 a S08); **a S01 fecha por completo só depois da 5.3 e da 5.5**, porque os botões de contato e "Favoritar" ficaram de fora desta tarefa (decisão do Product Owner, D1).

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.378 | 1.378 | 0 |
| Ferramenta de catálogo | 43 | 43 | 0 |
| Ferramenta de municípios | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 130 | 130 | 0 |
| E2E (Playwright, site publicado Production e Development) | 105 | 105 | 0 |

**Testes novos:** 39 unitários (`AdDetailTests` 33 contando as linhas de `DataRow`, `AdDetailRulesTests` 6) mais 4 do limite de fotos, 3 de integração (`AdDetailQueryTests`), 10 E2E (`AdDetailE2ETests`).

| Cenário | Prova |
|---|---|
| S01 anúncio completo | `AdDetailTests.US003S01_…` (capa, miniaturas, título, preço, categoria, local, "Publicado em 12/09/2026", descrição com quebras de linha como texto, características; o espaço do contato existe e **não traz telefone nem WhatsApp**); variantes de Vaga (sem fotos, bloco "Vaga de emprego") e Serviço (Tipo no lugar do preço); privacidade (nem nome, nem e-mail, nem telefone do autor) |
| S02 galeria de 20 fotos | `US003S02_…` (o servidor entrega as 20 miniaturas e o contador "1 de 20"; só a primeira foto grande tem carga imediata) e E2E (quinta miniatura, seta, teclado, voltar ao começo; na carga só uma foto grande é pedida) |
| S03 ampliar | `US003S03_…` (sem JavaScript a foto é um link para a versão grande; com JavaScript há `<dialog>` com "Fechar") e E2E (abre, setas, Esc devolve o foco, a página fica no mesmo ponto de rolagem) |
| S04 sem ficha de veículo ou terreno | `US003S04_…` + por grupo: Carros, Motos, Caminhões e ônibus, Barcos e aviões → "Características do veículo"; Terrenos → "Características do terreno"; demais → "Características" |
| S05 uma foto só | `US003S05_…` e E2E: sem miniaturas, setas nem contador; ampliar continua |
| S06 indisponível | `US003S06_…` (404 com mensagem, links para o início e, **só no arquivado**, para a categoria; sem título, fotos nem descrição) e `NaoPublicado_E_Inexistente_…` (rascunho, em revisão, rejeitado e inexistente têm corpo idêntico) + integração no SQL Server |
| S07 foto quebrada | `US003S07_…` (o lugar da mensagem vem no HTML) e E2E (fotos bloqueadas → "Foto indisponível"; as outras continuam navegáveis) |
| S08 celular de 320 px | `US003S08_…` e E2E (sem rolagem horizontal; deslizar troca de foto; miniaturas rolam na própria faixa; alvos de toque de 44 px) |
| Endereço | slug errado ou faltando → 301 para o endereço atual, com a query string; título editado depois não quebra o link; id inválido → 404 |
| Outros | data em São Paulo (23:59 UTC = 20:59 do mesmo dia; 02:00 UTC = 23:00 do dia anterior), SEO (título, descrição até 160 caracteres, `canonical`; `noindex` só na indisponível), 503 com código de referência e sem detalhe, axe sem violações em desktop e celular, inclusive com a janela aberta |

**Mutações (12; 12 mortas)**

| Mutação | Resultado |
|---|---|
| W1 mostra anúncio não publicado | morta (2) |
| W2 arquivado responde 410 | morta (2) |
| W3 contato/Favoritar antes da hora | morta (S01) |
| W4 miniaturas sem `lazy` | morta (S02) |
| W5 uma foto mostra setas | morta (S05) |
| W6 slug errado não redireciona | morta |
| W7 descrição sem escape (`Html.Raw`) | morta |
| W8 data em UTC | morta (caso das 02:00 UTC) |
| W9 título das características sem Motos | morta (2) |
| W10 fotos em ordem invertida (SQL Server) | morta |
| W11 descrição curta sem limite de 160 | morta; a primeira versão (`if (true)`) não compilava (código inalcançável) e foi reformulada para `MaxLength + 100` |
| W12 valor sem o Tipo do serviço | morta |

**Achados da rodada**

1. **Limite de fotos pegou o E2E (não era defeito dos testes de fotos):** a galeria de 20 fotos e dezenas de páginas de um IP só esgotaram os 300 pedidos por minuto da entrega de fotos e `PhotosE2ETests` (US008S02 e S03) recebeu 429. O limite passou a ser configurável por `RateLimiting:PhotosPerMinute` (padrão 300; zero, negativo ou ilegível voltam a 300; 4 testes), e o site do E2E sobe com 5000. **Achado de projeto no BACKLOG:** 300 por minuto por IP pode ser apertado para usuários reais atrás de um mesmo IP; decidir o valor no `/verify`.
2. **Rolagem ao fechar a janela de ampliação:** devolver o foco ao link fazia a página pular; o código guarda e restaura a posição de rolagem, e o E2E mede o ponto depois de abrir.
3. **Deslizar com o mouse:** o navegador arrastava a imagem e cancelava o gesto; `draggable="false"`, `user-drag: none` e tratamento de `pointercancel`. O E2E começa o gesto na parte de cima do palco (longe das setas).
4. **Teste unitário desatualizado:** a expressão do link de ampliar não conhecia o `draggable="false"` adicionado no item 3; corrigida e a suíte rodou de novo inteira.
5. **Tensão entre S06 e "mesma mensagem para tudo":** a S06 pede o link da categoria no anúncio arquivado, e a regra de negócio pede que os casos indisponíveis sejam indistinguíveis. Implementado: corpo idêntico para rascunho, em revisão, rejeitado e inexistente; o arquivado só acrescenta o link da categoria (404, nunca 410). Registrado no BACKLOG para o Product Owner decidir se prefere tirar o link.
6. **Docker:** o `dockerd` caiu de novo com reinício do worker durante a rodada e foi religado; sem efeito nos resultados finais.

## Tarefa 5.3 — contato por telefone e WhatsApp (US-004, 2026-10-05)

> **Em resumo:** a página do anúncio mostra o bloco "Fale com a Gazeta" a qualquer visitante, sem login: o telefone do site escrito e dois links comuns, "Ligar" (`tel:+55…`) e "Chamar no WhatsApp" (`https://wa.me/55…?text=…`, em nova aba com `noopener`). A mensagem traz o título e o endereço da página, codificados como URL, e o título chega exatamente como foi escrito. Os 5 cenários da US-004 estão provados (S01 a S05). Sem telefone configurado o bloco não aparece. O site não registra cliques: são links diretos, sem JavaScript e sem chamada ao servidor.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.416 | 1.416 | 0 |
| Ferramenta de catálogo | 43 | 43 | 0 |
| Ferramenta de municípios | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 131 | 131 | 0 |
| E2E (Playwright, site publicado Production e Development) | 113 | 113 | 0 |

**Testes novos:** 38 unitários (13 de `ContactTests`, 25 casos de `WhatsAppLinkTests`), 1 de integração, 8 E2E (`ContactE2ETests`).

| Cenário | Prova |
|---|---|
| S01 WhatsApp | `US004S01_…`: `https://wa.me/5511912345678?text=` e a mensagem lida como o WhatsApp lê (`Olá! Tenho interesse no anúncio “Honda Civic 2018”: https://localhost/anuncio/{id}/honda-civic-2018`); E2E: o número do `wa.me` é o escrito na tela e a mensagem é lida pelo próprio navegador (`URL.searchParams`) |
| S02 Ligar | `US004S02_…` e E2E: `tel:+55{número}`; sem `target` |
| S03 sem login | `US004S03_…` (visitante anônimo vê "Fale com a Gazeta", o número `(11) 91234-5678` e os dois botões; fixo de 10 dígitos sai `(11) 3456-7890`) e E2E em contexto sem cookie |
| S04 título com símbolos | `US004S04_…` e E2E: `Sítio "Boa Vista" & Cia` chega idêntico; o `&` vai como `%26` e a query tem um único parâmetro |
| S05 nova aba | `US004S05_…` (`target="_blank"`, `rel` com `noopener`, nome acessível avisa a nova aba) e E2E: o clique abre uma aba nova no `wa.me` (página do WhatsApp simulada), `window.opener` é nulo e a aba do anúncio continua com o mesmo endereço |
| Sem rastreio | `Contato_NaoRegistraCliques_…` (só 2 links, ambos direto a `tel:` e `wa.me`; sem `onclick`, `ping` nem marcador de métrica) e E2E: **zero** pedidos ao servidor do site durante o clique |
| Layout e acessibilidade | E2E: em 320 px o contato vem depois da descrição, botões de altura de pelo menos 44 px, um sobre o outro, sem rolagem horizontal; em 1280 px fica à direita; sem JavaScript funciona; foco por teclado com contorno; axe sem violações nos dois tamanhos |
| Integração | `Contato_TelefoneVemDoBanco_…`: sem telefone no SQL Server o bloco não aparece; com `site.phone` gravado aparece com a mensagem certa |

**Link do WhatsApp (`WhatsAppLinkTests`):** acentos, aspas, `&`, `#`, `%`, `?`, `+`, `=`, `<`, `>`, aspas simples, tabulação, quebra de linha e emoji (UTF-8) chegam iguais; título vazio vira "neste anúncio" (sem aspas vazias); título de 500 caracteres vira 120, sem reticências e com o endereço total abaixo de 700 caracteres; 119 e 120 passam sem corte; o corte no meio de um emoji deixa o par inteiro de fora (sem caractere partido); o corte não deixa espaço antes das aspas.

**Mutações (8; 8 mortas)**

| Mutação | Resultado |
|---|---|
| C1 mensagem sem o título | morta (20) |
| C2 mensagem sem o endereço | morta (18) |
| C3 título sem codificação | morta (13) |
| C4 `tel:` sem o `+55` | morta (5) |
| C5 nova aba sem `noopener` | morta (S05) |
| C6 telefone sem formatação | morta (4) |
| C7 bloco visível sem telefone configurado | morta |
| C8 botão do WhatsApp só com ícone | morta (3) |

**Achados da rodada**

1. **O Razor escreve o `+` do `tel:` como `&#x2B;`** (HTML válido; o navegador lê `tel:+55…`). Os testes de página decodificam o atributo; o E2E lê o `href` já decodificado pelo navegador e confirmou `tel:+55{número}`.
2. **A pré-visualização da fila e a página pública usam o mesmo bloco** (`_Contact`), com o endereço público do anúncio na mensagem; os testes antigos da pré-visualização passaram a conferir o `wa.me/…?text=`.
3. **Aspas na mensagem:** a decisão escreveu `"{título}"`; implementei aspas tipográficas (“título”) para distinguir das aspas retas de um título como `Sítio "Boa Vista" & Cia` (registrado no BACKLOG, é uma linha para trocar).
4. **Telefone fixo:** a US-015 aceita fixo de 10 dígitos, mas o `wa.me` só abre conversa de WhatsApp (registrado no BACKLOG para decisão).
5. **Docker:** o `dockerd` caiu de novo no meio da rodada (todos os testes de integração falharam em 0 ms) e foi religado; sem efeito nos resultados finais.
6. **O E2E de contato não depende de um número fixo:** lê o telefone escrito na tela e confere os links contra ele (o `SettingsE2ETests` troca o número do banco de teste).

## Tarefa 5.4 — busca e filtros (US-002, 2026-10-05)

> **Em resumo:** `/busca` lista os anúncios publicados que atendem a todos os filtros do endereço: texto (cada palavra, sem acento e sem diferença de maiúscula, no título ou na descrição), categoria com as subcategorias, UF e cidade, preço em reais e as características do bem que a categoria escolhida oferece. Ordena (mais recentes, menor e maior preço), pagina de 24 em 24 e reabre idêntica em outra aba. Os 12 cenários da US-002 estão provados (S01 a S12); no SQL Server real a busca leva **6 ms (p95) com 200 anúncios e 52 ms com 6.000**, bem abaixo dos 500 ms da NFR-04. Junto, saiu o ajuste da 5.3: com telefone fixo só o botão "Ligar" aparece.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.564 | 1.564 | 0 |
| Ferramenta de catálogo | 43 | 43 | 0 |
| Ferramenta de municípios | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 151 | 151 | 0 |
| E2E (Playwright, site publicado Production e Development) | 131 | 131 | 0 |

**Testes novos:** 148 unitários (`SearchRulesTests` 60 casos, `SearchTests` 28, `DecimalInputTests` 47, `PublicCitiesTests` 7, mais 6 do telefone fixo), 20 de integração (`SearchQueryTests`), 18 E2E (`SearchE2ETests`).

| Cenário | Prova |
|---|---|
| S01 buscar por texto | `SearchTests.US002S01_…` (pede as palavras normalizadas; "1 anúncio encontrado"; a caixa do topo mostra o texto) e `SearchQueryTests.US002S01_…` no SQL Server: só publicados (rascunho, em revisão, rejeitado e arquivado com "civic" ficam de fora), texto sem acento e sem maiúscula no título **e** na descrição (o Corolla só tem "parece um civic" na descrição), todas as palavras em qualquer ordem; E2E pela caixa do topo |
| S02 categoria, local e preço | `US002S02_…` e SQL Server: Carros em Campinas/SP até R$ 50.000 devolve só o Fit; faixa com extremos inclusive; categoria principal inclui filhas e netas; E2E combinando texto, UF e preço máximo |
| S03 características de veículo | `US002S03_…`: marca, ano de/até e km máximo só aparecem (e só valem) em categoria de veículos; SQL Server: marca, modelo, ano e km; E2E: a categoria liga os filtros, marca carrega os modelos, trocar a marca esvazia o modelo, categoria nova tira o que não serve do endereço |
| S04 terrenos por área | `US002S04_…`, SQL Server (extremos inclusive; anúncio sem área nunca passa numa faixa de área) e E2E |
| S05 ordenar | `US002S05_…` (a ordem fica selecionada e em todo link de página; o "Ordenar" leva os filtros) e SQL Server (data, menor e maior preço, desempate estável pelo id nas três ordens); E2E escolhendo a ordem (envia sozinho) |
| S06 paginar | `US002S06_…` (30 resultados: 24 + 6, "Próxima", página 2 destacada; página além do fim mostra a última) e SQL Server (total igual em todas as páginas, nenhum anúncio repetido ou perdido, capa com uma só linha por anúncio) |
| S07 sem resultados | `US002S07_…` ("Nenhum anúncio encontrado para esses filtros" e "Limpar filtros" para `/busca`; site sem anúncios diz outra coisa) e E2E (Limpar filtros volta a todos) |
| S08 faixa invertida | `US002S08_…` (erro junto do campo com `aria-invalid`, valores digitados mantidos, lista sem a faixa) e E2E: **com JavaScript** o envio é bloqueado, o endereço não muda e a lista continua igual; **sem JavaScript** o servidor mostra o mesmo erro |
| S09 compartilhar | `US002S09_…` (o mesmo endereço pede a mesma consulta, campos e opções iguais, links de página com todos os filtros) e E2E (outra aba mostra os mesmos filtros, os mesmos resultados na mesma ordem); teste de produtor e consumidor: os nomes dos campos do formulário são exatamente os parâmetros que o servidor lê |
| S10 trocar a UF | `US002S10_…` (a cidade só vale com a UF e da UF; fica desabilitada sem UF) e E2E: trocar SP por RJ volta a cidade para "Todas as cidades" e a lista passa a ter só cidades do RJ |
| S11 falha | `US002S11_…` (503, "Não foi possível buscar agora. Tente novamente.", "Tentar novamente" para o mesmo endereço, código de referência, filtros preservados, nada técnico na tela) e SQL Server: a tabela trancada estoura os 10 s e dá 503 sem pilha, e a busca volta quando o bloqueio sai |
| S12 320 px | `US002S12_…` (painel aberto no servidor; botões de abrir e fechar só com JavaScript; ordem busca, filtros, total e cards) e E2E em 320 px: painel recolhido ao carregar, abre e fecha, o foco volta ao botão, nada passa de 320 px, sem rolagem horizontal; com erro no filtro o painel não recolhe |
| Sem JavaScript e acessibilidade | E2E sem JavaScript (formulário e "Ordenar" funcionam; a cidade libera depois de "Aplicar filtros"); axe sem violações em 5 páginas (com filtros de veículo, de terreno, com erro e sem resultado), em 1280 e 320 px |

**Quais categorias oferecem cada filtro específico (lista exata, conferida por teste contra a árvore real de 147 categorias):**

| Filtros oferecidos | Categorias |
|---|---|
| Marca, modelo, ano e quilometragem máxima | **Carros, vans e utilitários** · **Motos** |
| Ano e quilometragem máxima | **Caminhões** · **Ônibus** |
| Área mínima e máxima | **Apartamentos** · **Casas** · **Terrenos, sítios e fazendas** · **Comércio e indústria** |
| Nenhum | todas as outras: as 22 categorias principais (as que misturam grupos ficam sem filtro específico), Autopeças e as 5 peças, Barcos e aeronaves, Vagas, Serviços e Produtos em geral |

**Ponto para decisão:** o filtro de **área** também aparece em Apartamentos, Casas e Comércio e indústria, não só em Terrenos. A busca segue o grupo de campos (ADR-006): o grupo Imóveis marca "Área (m²)" como filtrável para as quatro. Restringir a Terrenos pede uma regra por categoria no grupo; está no BACKLOG.

**Desempenho (SQL Server 2022 real, contêiner):** p95 de **6,0 ms** com 200 anúncios cadastrados (NFR-04: abaixo de 500 ms) e **52,4 ms** com 6.000 cadastrados (2.000 publicados), em quatro formas de busca (texto, duas palavras com ordem por preço, categoria + UF + cidade + preço, texto + três categorias + página 2). O plano de categoria + UF não usa varredura de tabela. Nenhum índice novo foi necessário.

**Mutações (16; 16 mortas)**

| Mutação | Resultado |
|---|---|
| S1 mostra anúncio não publicado | morta (8, integração) |
| S2 categoria sem as descendentes | morta |
| S3 texto sem normalização | morta |
| S4 palavras não restringem (o "e" vira "ou") | morta (6) |
| S5 termo concatenado no SQL | morta (9): o `SqlBuilder` recusa o fragmento com aspas |
| S6 ordem por data invertida | morta |
| S7 Serviços dentro da faixa de preço | morta |
| S8 Serviços no início do "Menor preço" | morta |
| S9 página de 25 | morta (2) |
| S10 faixa invertida aceita | morta (2) |
| S11 cidade mantida ao trocar a UF | morta |
| S12 filtro específico fora do grupo | morta |
| S13 filtros fora do endereço | morta (3) |
| J1 `lerNumero` do JavaScript aceita 3 casas decimais | morta (paridade com o `DecimalInput`) |
| J2 painel não recolhe em tela estreita | morta (S12 no navegador) |
| J3 trocar a UF não limpa a cidade | morta (S10 no navegador) |
| (5.3) WhatsApp aparece para telefone fixo | morta (3) |

As três mutações de JavaScript rodaram no site publicado (apagando as cópias `.gz` e `.br` do script; ver o runbook). A primeira tentativa **não** mutou nada (o navegador recebia a cópia comprimida original): os três "sobreviventes" daquela rodada eram artefato da rodada, não do teste, e foram refeitos.

**Achados da rodada**

1. **Paridade de implementação dupla (JavaScript × C#):** a conferência de faixa do navegador (`lerNumero`) repete a gramática do `DecimalInput` do servidor. As duas rodam sobre a mesma tabela de 40 entradas (`DecimalInputTests` e o E2E de paridade) e dão o mesmo valor em cada linha; o C# passou a usar `[0-9]` (o `\d` do .NET aceita dígitos de outros alfabetos, o do JavaScript não).
2. **O texto casa em qualquer pedaço da palavra** ("acao" também acha "documentação"). É a decisão da ADR-006; registrado no BACKLOG.
3. **Lista de cidades para o visitante:** a rota `/api/v1/cities` é só da equipe (com login e cache privado, testes antigos); a busca ganhou `/api/v1/public/cities`, pública e com cache compartilhável. 7 testes.
4. **`SqlBuilder` recusa números soltos no SQL:** `CASE WHEN … THEN 1 ELSE 0 END` e `> 0` viraram parâmetros (`@First`, `@Last`, `@NoMatch`), no mesmo espírito de "valor nunca no texto do SQL".
5. **"Buscando…" no navegador:** com a resposta atrasada por rota o Playwright perde o contexto da página no meio da espera, então o estado do botão é conferido disparando o envio e cancelando-o logo depois do tratamento da página, e o envio de verdade é conferido à parte (endereço enxuto, sem parâmetro vazio, botão de volta).
6. **O Bootstrap ignora um clique no meio da animação de abrir/fechar o painel:** o E2E espera a animação terminar (`collapse show`) antes de clicar em "Fechar".
7. **Na primeira rodada um E2E falhou ao publicar os anúncios de apoio** (etapa "Anúncio enviado para revisão" da tela, antes de tocar na busca) e não repetiu nas rodadas seguintes; o roteiro de publicação já era usado por outros E2E.
8. **O campo "Preço" da tela do anúncio guarda os dígitos como centavos** (`5000` vira R$ 50,00): os anúncios de apoio usam `300000`, `500000` e `800000` (R$ 3.000, 5.000 e 8.000). Não é defeito: é a máscara do campo.
9. **Docker:** nenhuma queda nesta rodada.

## Tarefa 5.5 — favoritos no navegador (US-005 e US-011-S04, 2026-10-05)

> **Em resumo:** o visitante favorita anúncios pelo coração do card e pelo botão da página do anúncio; a lista fica **só no navegador** (`localStorage`, chave `gazeta:favoritos:v1`), o contador do topo acompanha, e `/favoritos` mostra os cards pedindo-os ao servidor por id (só os publicados, na ordem em que foram favoritados). O anúncio que saiu do ar (despublicado, arquivado) some da lista e do armazenamento, com aviso. Os 9 cenários (S01 a S08 da US-005 e S04 da US-011) estão provados; a API `GET /api/v1/ads?ids=` (até 100 ids) também. Das 13 mutações, **13 mortas** (uma delas só depois de reforçar um teste).

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.620 | 1.620 | 0 |
| Ferramenta de catálogo | 43 | 43 | 0 |
| Ferramenta de municípios | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 155 | 155 | 0 |
| E2E (Playwright, site publicado Production e Development) | 141 | 141 | 0 |

**Testes novos:** 56 unitários (`FavoritesTests`), 4 de integração (`FavoritesQueryTests`), 10 E2E (`FavoritesE2ETests`). Dois testes antigos foram ajustados (ver achados 1 e 2); o E2E `US002S09` ganhou uma espera e o `US002S10` uma checagem a mais.

| Cenário | Prova |
|---|---|
| S01 favoritar pela lista | `FavoritesTests` (o coração do card: nome "Favoritar anúncio {título}", `aria-pressed="false"`, escondido sem JavaScript, na página inicial, na categoria e na busca) e E2E: o coração fica `aria-pressed="true"`, o contador do topo vai de 0 a 1 a 2, o armazenamento guarda os ids na ordem; clicar de novo desfavorita |
| S02 pela página do anúncio | `FavoritesTests` (botão com o texto "Favoritar" e `aria-pressed`) e E2E: o botão passa a "Favoritado", o contador sobe, e depois de recarregar o estado volta |
| S03 continua depois de fechar o navegador | E2E: o estado de armazenamento do contexto é gravado e aberto num contexto novo; `/favoritos` mostra os dois, na ordem em que foram favoritados |
| S04 remover | E2E: "Remover {título} dos favoritos" tira o card, o texto passa de "2 anúncios" a "1 anúncio" e o contador do topo acompanha; o armazenamento fica com o outro id |
| S05 lista vazia | E2E: "Você ainda não favoritou nenhum anúncio" com o link "Ir para a página inicial" (leva a `/`); o total some |
| S06 e US-011-S04 anúncio que saiu do ar | E2E com 4 favoritos: o Administrador arquiva um pelo painel; `/favoritos` mostra "1 anúncio favoritado deixou de estar disponível e foi removido da sua lista.", os 3 que ficaram e o contador 3; o id também sai do armazenamento; ao recarregar o aviso não repete; arquivando mais dois, o aviso vem no plural ("2 anúncios favoritados deixaram de estar disponíveis e foram removidos da sua lista.") |
| S07 armazenamento bloqueado | E2E com `setItem` lançando erro: o alerta "Não foi possível salvar seus favoritos neste navegador" aparece, o coração continua vazio, o contador fica em 0; em `/favoritos` aparece o aviso de armazenamento bloqueado (e não a lista vazia) |
| S08 outro aparelho | E2E: outro contexto (outro navegador) abre `/favoritos` vazio, contador 0, e o aviso permanente "Seus favoritos ficam salvos apenas neste navegador…" continua lá |
| API `GET /api/v1/ads?ids=` | `FavoritesTests`: sem login, envelope `PagedResult`, na ordem pedida, repetidos contam uma vez, 100 ids passam e 101 dão 400, `ids` vazio, ausente, com letra, negativo, decimal, vírgula dupla ou fora do limite dão 400 **sem consultar o banco**; SQL Server: só publicados (rascunho, em revisão, rejeitado, arquivado e despublicado ficam de fora), ordem pedida, 100 ids com capa |
| Outros | E2E: outra aba (favoritar numa atualiza a segunda e a lista sem recarregar), teclado (Enter e Espaço favoritam, o foco fica no coração), lixo no armazenamento (JSON quebrado, texto, decimal, negativo, nulo, objeto, número fora do limite, repetidos: só os ids válidos, sem repetir; o próximo favorito grava a lista limpa), 320 px (sem rolagem horizontal; coração e "Remover" com pelo menos 24 × 24 px), sem JavaScript (sem coração nem botão; "Meus favoritos" com o aviso e "Para ver seus favoritos, ative o JavaScript"), axe sem violações em 1280 e 320 px na busca com coração marcado, na página do anúncio, na lista e na lista vazia |

**Mutações (13; 13 mortas)**

| Mutação | Resultado |
|---|---|
| F1 a API devolve não publicados | morta (3, integração) |
| F2 a API devolve fora da ordem pedida | morta (3) |
| F3 a API aceita mais de 100 ids | morta (2) |
| F4 ids repetidos não são deduplicados no servidor | morta (2) |
| F5 o coração sai do servidor sem `hidden` (aparece sem JavaScript) | **sobreviveu na primeira rodada** (o teste procurava `\\bhidden\\b` e achava o `aria-hidden` do ícone); o teste passou a olhar só a abertura do `<button>`; **morta** depois |
| F6 `/favoritos` sem `noindex` | morta |
| F7 o coração não atualiza o `aria-pressed` | morta (3, E2E) |
| F8 o contador do topo não atualiza | morta (6) |
| F9 o armazenamento não é limpo nem deduplicado | morta (1: o teste do lixo) |
| F10 o anúncio indisponível não sai do armazenamento | morta (2) |
| F11 o aviso fica sempre no singular | morta (1: S06 no plural) |
| F12 o armazenamento bloqueado falha em silêncio | morta (2, incluindo S07) |
| F13 "Remover" não atualiza o contador (o evento de mudança some) | morta (5) |

Mutações da 5.4 refeitas (ver achado 3): J1 (`lerNumero` com 3 casas) e J2 (painel não recolhe) **mortas de verdade**; **J3 (trocar a UF não limpa a cidade) sobreviveu**, porque o preenchimento da lista já limpava a cidade; o `US002S10` ganhou a conferência "sem UF, a cidade escolhida antes some e o campo fica vazio e travado" e a mutação passou a morrer.

**Achados da rodada**

1. **Regressão pega pela varredura de `innerHTML` (RC-17):** a primeira versão de `pages/favorites.js` montava o fragmento do servidor com `template.innerHTML`; o teste `NenhumModuloUsaInnerHtmlComTextoDoServidor` falhou. Passou a usar `DOMParser` (não executa scripts nem lê no documento da página).
2. **Regressão pega no teste do card de Vagas:** o ícone do coração tem `aria-hidden="true"`, e o teste que contava um só `aria-hidden` (o bloco neutro de Vagas) passou a contar sem o botão do coração.
3. **As mutações de JavaScript da 5.4 (J1 a J3) tinham sido feitas pelo método do runbook antigo, que estava errado:** apagar as cópias `.gz` e `.br` deixa o site responder `200` com corpo vazio para quem pede compressão; nenhum script carrega e qualquer teste de JavaScript falha, o que simula "morta" sem provar nada. Nesta rodada as 7 primeiras mutações de JavaScript "morreram" por 10 testes cada, sinal do problema. O runbook foi reescrito (mutar o arquivo e a lista de arquivos publicados, reiniciar o site) e J1 a J3 foram refeitas: J1 e J2 mortas, **J3 sobrevivia** e agora morre.
4. **Limite de login:** 5 por 15 minutos, em memória; cada rodada de E2E que entra no painel gasta um. O site é reiniciado antes de cada mutação (isso também zera o limite).
5. **Dois E2E falharam numa rodada completa e passaram nas repetições:** `US002S09` tinha uma corrida do próprio teste (escolher a ordem enquanto a página nova ainda carregava; corrigido esperando o endereço novo) e `US003S03` (a página voltou 4 px abaixo do ponto da rolagem) não repetiu em quatro rodadas. Fica no BACKLOG com a pista a seguir se voltar.
6. **O Docker caiu uma vez** (o daemon parou, de novo, ao rodar a integração); reiniciado como no runbook.
7. **"Favoritar" muda o texto do botão da página do anúncio, o coração do card mantém o nome:** o nome acessível do coração é constante ("Favoritar anúncio {título}") e quem diz o estado é o `aria-pressed`; conferir com leitor de tela é item do `/verify` (BACKLOG).

## Tarefa 5.6 — SEO básico das páginas públicas (NFR-21, 2026-10-05)

> **Em resumo:** início, categorias e anúncios publicados têm título e descrição próprios, endereço canônico com o endereço do site e, no anúncio, o Open Graph mínimo (título, descrição e a capa) para a prévia no WhatsApp. Toda outra página (busca, favoritos, anúncio indisponível, categoria que não existe, páginas de erro) sai com `noindex`. `/sitemap.xml` traz só o início, as categorias com anúncio publicado e os anúncios publicados; arquivar tira o anúncio do mapa na hora. `/robots.txt` libera o site, fecha o painel e a API e aponta o mapa. A S7 da SPEC foi confirmada (v1.4). **11 mutações, 11 mortas** (as 6 do Checkpoint 6 repetidas mais 5 novas, uma por correção).

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.650 | 1.650 | 0 |
| Ferramenta de catálogo | 43 | 43 | 0 |
| Ferramenta de municípios | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 160 | 160 | 0 |
| E2E (Playwright, site publicado Production e Development) | 144 | 144 | 0 |

**Testes novos:** 30 unitários (`SitemapTests` 10, `RobotsTests` 4, `TitleTests` 16), 5 de integração (`SitemapQueryTests`), 3 E2E (`SeoE2ETests`).

| Critério | Prova |
|---|---|
| Início, categoria e anúncio com título e descrição próprios | `TitleTests`: título e descrição de cada um; **a varredura das 147 categorias** exige descrição com no máximo 160 caracteres e título e descrição todos diferentes; cinco páginas indexáveis lado a lado não repetem título, descrição nem canônico; E2E lê o `<head>` no navegador |
| Endereço canônico com `Site:BaseUrl` | `TitleTests` (início, categoria, anúncio; nunca o cabeçalho Host); categoria sem parâmetro de rastreio; página 2 aponta para si (`?pagina=2`); página além do fim aponta para a última |
| Open Graph mínimo no anúncio | `TitleTests`: `og:title`, `og:description` e `og:image` (capa em miniatura de 480 px com o endereço completo do site) e **só esses três**; sem foto e em Vagas não há `og:image`; título com `<b>` e aspas sai codificado; E2E: a imagem do `og:image` abre de verdade (200, `image/webp`) |
| Anúncio arquivado, despublicado ou inexistente | `TitleTests`: 404 com `noindex`, sem descrição, canônico nem Open Graph; E2E depois de arquivar pelo painel |
| Busca, favoritos e páginas de erro fora do índice | `TitleTests`: `noindex, follow` e sem canônico (busca com e sem filtros, favoritos, categoria inexistente, 503 da vitrine); padrão do parcial `_Seo` |
| `/sitemap.xml` só com publicados | `SitemapTests` (início, categorias com anúncio e os ancestrais delas, anúncios na ordem da consulta com `lastmod` do dia; sem categoria sem anúncio; endereço do site, não do Host; sem cache; 503 sem detalhe; XML com escape; no máximo 50.000 endereços); integração no SQL Server: rascunho, em revisão, rejeitado, arquivado e despublicado ficam de fora, mais novo primeiro, limite, categorias distintas, **arquivar tira do mapa na hora** e 1.500 anúncios em menos de 3 s; E2E: publica, confere início, categoria e anúncio no mapa, arquiva e confere que saiu |
| `/robots.txt` | `RobotsTests` e E2E: `User-agent: *`, `Disallow: /painel`, `/api/`, `/favoritos/lista` e `Sitemap: {Site:BaseUrl}/sitemap.xml`; o site público, a busca e os favoritos **não** são bloqueados (o robô precisa abrir a página para ler o `noindex`) |

**Mutações (12; 12 mortas)**

| Mutação | Resultado |
|---|---|
| M1 o mapa traz anúncio não publicado | morta (2, integração) |
| M2 o ancestral da categoria com anúncio fica fora do mapa | morta (2) |
| M3 o canônico do início sem a barra do endereço do site | morta (a primeira versão da mutação não compilava, refeita) |
| M4 página sem `SeoModel` sai indexável | morta (3) |
| M5 descrição de categoria sem o corte em 160 | morta |
| M6 `robots.txt` sem fechar o painel | morta |
| M7 endereço do mapa sem escape de XML | morta |
| M8 título de categoria sem o pai (títulos repetidos) | morta (4) |
| M9 `og:image` com a foto grande em vez da miniatura | morta |
| M10 limite de 50 mil ignora o que o início e as categorias ocupam | morta |
| M11 canônico da página 2 sem a página | morta (2) |
| M12 mapa guardado em cache | morta |

**Achados da rodada**

1. **A integração pegou um erro que os testes unitários não podiam pegar:** o `SqlBuilder.Page` aceita no máximo 100 por página e o mapa lê até 50.000; o repositório de mentira dos unitários não tem esse limite. Passou a usar `TOP (@Take)` com parâmetro.
2. **Nomes de categoria repetidos:** "Serviços", "Autopeças" e "Vagas de emprego" existem em pai e filho, então só o nome daria títulos repetidos. A subcategoria passou a levar o pai no título ("Anúncios de Casas · Imóveis"), e o teste que varre as 147 categorias garante títulos e descrições todos diferentes.
3. **Canônico da página 2:** o plano dizia "sem parâmetros"; fica sem parâmetros de rastreio e de ordem, mas a página 2 em diante aponta para `?pagina=N` (apontar a página 2 para a 1 esconderia os anúncios dela dos buscadores). Interpretação registrada no BACKLOG.
4. **Um teste existente caiu:** `PaginaNaoCarregaRecursosExternos` proibia qualquer endereço absoluto no HTML; o canônico é absoluto por definição e não carrega nada, então ficou fora da conta.
5. **Padrão seguro:** a página que não pede para ser indexada sai `noindex, follow`; só início, categoria e anúncio publicado pedem.
6. **Docker e E2E:** nenhuma queda e nenhum teste instável nesta rodada.

## Checkpoint 5 — Site público completo (fechamento da Fase 5, 2026-10-06)

> **Em resumo:** os três itens do checkpoint foram provados com um visitante sem login, no SQL Server real e no site publicado: todo endereço público abre sem pedir entrada e o painel continua fechado; Serviços (tipo, sem preço) e Vagas (salário, sem foto) aparecem certos em cards, detalhe, busca, favoritos, API e mapa; e arquivar um Serviço e despublicar uma Vaga os tira de todos esses lugares na hora. A revisão de código (5.1 a 5.6) deu **APPROVE com condições** (0 🔴, 5 🟡, 23 🟢). A cobertura passa das duas metas (**97,9% de linhas, 91,5% de ramos**). O E2E instável `US003S03` foi explicado e corrigido (11 falhas em 20 → 0 em 20), e uma segunda causa de instabilidade nos roteiros de publicação também. **6 mutações, 6 mortas.**

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.654 | 1.654 | 0 |
| Ferramenta de catálogo | 43 | 43 | 0 |
| Ferramenta de municípios | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 161 | 161 | 0 |
| E2E (Playwright, site publicado Production e Development) | 146 | 146 | 0 |

**Testes novos do checkpoint:** 4 unitários (`Checkpoint5PublicTests`), 1 de integração (`Checkpoint5ServicesJobsTests`), 2 E2E (`Checkpoint5E2ETests`). O roteiro de publicação do E2E ganhou `PublishServiceAsync` e `PublishJobAsync`.

| Item do checkpoint | Prova |
|---|---|
| Início, categoria, busca, detalhe, contato e favoritos funcionam sem login | `Checkpoint5PublicTests`: 13 endereços públicos (início, categoria e página 2, busca com e sem filtros, anúncio, favoritos e o fragmento, a API de ids, as cidades da UF, o mapa e o `robots.txt`) respondem 200 para um visitante e **nenhum redireciona**; o que não existe dá 404 sem pedir login; 6 endereços do painel e a lista de cidades da equipe **continuam pedindo login**; o visitante não recebe cookie de entrada. E2E: o visitante vai do início ao anúncio, vê "Ligar" (`tel:+5511912345678`) e "Chamar no WhatsApp", favorita, abre "Meus favoritos" pelo link do topo, abre a categoria pelo caminho de navegação e busca pela caixa do topo ("3 anúncios encontrados"), sem sair da vitrine e com **nenhuma chamada do site com erro** |
| Serviços e Vagas em cards, detalhe e busca | `Checkpoint5ServicesJobsTests` (SQL Server real): o Serviço mostra "Serviços domésticos" e **nenhum "R$"** (com capa), a Vaga mostra "Salário R$ 2.800" e **nenhum `<img>`** (bloco neutro); a página do Serviço não tem preço e a da Vaga não tem galeria nem `og:image`; na busca, o Serviço fica fora da faixa de preço e **no fim** de "Menor preço" e de "Maior preço"; nos favoritos e na API o Serviço tem `priceCents` nulo e a Vaga `coverUrl` nulo. E2E: os mesmos pontos no navegador, com os três anúncios publicados pelas telas |
| Mapa só com publicados; arquivar e despublicar tiram na hora | integração: o mapa lista os três publicados e nunca o rascunho; depois de **arquivar o Serviço e despublicar a Vaga** eles somem da página inicial, da busca, da categoria, do fragmento de favoritos, da API e do mapa (a categoria de Serviços sai do mapa), e as duas páginas viram 404. E2E: os dois somem da página inicial e da busca, os favoritos mostram "2 anúncios favoritados deixaram de estar disponíveis e foram removidos da sua lista.", e o mapa também os perde |

**Mutações (6; 6 mortas)**

| Mutação | Resultado |
|---|---|
| K1 "Meus favoritos" exige login | morta (matriz sem login) |
| K2 o Serviço deixa de ir para o fim de "Menor preço" | morta (integração) |
| K3 o card do Serviço perde o tipo | morta |
| K4 a Vaga mostra a foto no card | morta (a primeira versão apontava para o arquivo errado, refeita) |
| K5 a página inicial mostra anúncio não publicado | morta |
| K6 a página do anúncio exige login | morta (2) |

### Revisão de código (`reports/CODE_REVIEW.md`)

- **Veredito:** APPROVE com condições. Notas dos 5 eixos: Corretude 3, Legibilidade 4, Arquitetura 4, Segurança 4, Desempenho 4. **🔴 0 · 🟡 5 · 🟢 23 · ✅ 13.** A Corretude fica em 3 porque os cinco 🟡 caem nela.
- **🟡:** (1) um número de 27 a 29 dígitos num filtro de preço faz `DecimalInput.TryParseCents` lançar `OverflowException` e a busca responde 503 (**reproduzido em execução** nesta rodada); (2) cobertura numérica e seção do Checkpoint 5 (**resolvido aqui**); (3) quatro decisões aprovadas do Product Owner ainda não voltaram à SPEC (S08 "lista sem a faixa", filtro de área em quatro categorias, telefone fixo só com "Ligar", link da categoria no anúncio arquivado); (4) o caminho de mais de 100 favoritos (lotes) não tem teste; (5) a regra de ids dos favoritos existe em JS e em C# sem tabela de paridade (já diferem no id 0).
- **Nada do código de produto foi corrigido**, como combinado ("só relatar").

### Cobertura (Gate 6)

Medida como na seção "Correções pós-Checkpoint 4" (`Microsoft.Testing.Extensions.CodeCoverage`, escopo em `coverage.settings.xml`), unitários e integração, e unida por linha.

| Medida | Unitários | Integração | **União (o gate)** | Meta | Resultado |
|---|---|---|---|---|---|
| Linhas | 96,2% (5.366 de 5.576) | 81,5% (4.546 de 5.576) | **97,9% (5.457 de 5.576)** | ≥ 80% | atendida |
| Ramos | 88,8% (1.533 de 1.726) | 58,2% (1.004 de 1.726) | **91,5% (1.580 de 1.726)** | ≥ 75% | atendida |

| Projeto | Linhas | Ramos |
|---|---|---|
| `GazetaMarketplace.Core` | 99,0% (2.810 de 2.839) | 94,9% (899 de 947) |
| `GazetaMarketplace.Infrastructure` | 96,9% (1.854 de 1.914) | 88,2% (365 de 414) |
| `GazetaMarketplace.Web` | 96,4% (793 de 823) | 86,6% (316 de 365) |

Só os unitários já passam das duas metas. Antes da fase: 97,7% de linhas e 90,9% de ramos; o acréscimo de ~580 linhas da Fase 5 não baixou a cobertura.

**Métodos a 0% (13 de 769):** nenhum é regra de negócio sem teste *que o produto já chama*. Os 12 do relatório anterior continuam (fábrica de tempo de projeto, contrato do Identity, construtores estáticos, propriedade de uma linha e três métodos de uma linha chamados por produção e provados por esses caminhos). **Novo:** `PublicRoutes.Ad(int, string)` — **nenhum código de produção nem de teste o chama** (as telas usam `AdRoutes.Detail`); é um atalho sem uso, no BACKLOG para remover ou usar.

### Achados da rodada

1. **`US003S03` instável explicado e corrigido.** Em 20 rodadas o teste falhou 11 vezes (a página "voltava" a 64, 41 ou 28 px em vez de 60). Duas causas, as duas **do teste**: (a) o Playwright rola a página até o botão antes de clicar, então a posição lida depois de abrir o diálogo não era a do instante do clique; (b) ao fechar, o navegador rola até o botão e só depois dispara o evento `close`, em que o `ad-gallery.js` restaura o ponto de antes; o teste lia a rolagem no instante em que o diálogo sumia, antes do evento. Passou a medir a posição dentro do clique e a esperar a restauração (até 2 s). **20 de 20** depois. **Pista para o produto:** há um salto de 1 quadro entre o navegador rolar e o script restaurar; se incomodar, restaurar antes (por exemplo, `scrollTo` no próprio clique de fechar). Fica no BACKLOG.
2. **Segunda instabilidade corrigida:** a etapa "Anúncio enviado para revisão" falhava de vez em quando em vários E2E (3 ocorrências desde a 5.4). Causa: o roteiro clica em "Enviar para revisão" duas vezes e o segundo clique podia cair no botão da página que ainda estava saindo, reenviando o mesmo formulário. Agora espera o título "Enviar para revisão?" da página de confirmação entre os dois cliques, nos 6 roteiros que repetiam o trecho. Última rodada completa do E2E: 146 de 146.
3. **O método de mutação de arquivos estáticos** (runbook reescrito na 5.5) foi usado nas mutações de JavaScript anteriores a este checkpoint; as deste checkpoint são todas de C# e de Razor.
4. **O número de testes** subiu de 1.620 para 1.654 nos unitários e de 155 para 161 na integração desde o fim da 5.5 (5.6 e checkpoint).

## Correções pós-Checkpoint 5 (2026-10-06)

> **Em resumo:** as decisões do Product Owner sobre os avisos 🟡 da revisão do Checkpoint 5 foram executadas: o estouro de `decimal` no filtro de preço foi corrigido, a SPEC foi emendada (v1.5) com as quatro decisões que só estavam no BACKLOG, o caminho de mais de 100 favoritos ganhou testes e a regra de ids dos favoritos ganhou uma **tabela de paridade de 48 entradas** em que o JavaScript e o C# rodam sobre as mesmas linhas (a primeira rodada achou e corrigiu uma diferença real: o id 0). **6 mutações, 6 mortas.**

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.673 | 1.673 | 0 |
| Ferramenta de catálogo | 43 | 43 | 0 |
| Ferramenta de municípios | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 161 | 161 | 0 |
| E2E (Playwright, site publicado Production e Development) | 149 | 149 | 0 |

| Aviso | O que foi feito | Teste que prova |
|---|---|---|
| 🟡 1 `OverflowException` num preço de 27 a 29 dígitos | `DecimalInput.TryParseCents` compara `reais` com `maxCents / 100m` **antes** de multiplicar por 100; vale para o preço mínimo e o máximo (os dois passam por `ParseCents`) | `DecimalInputTests.Centavos_NumeroEnormeQueCabeEmDecimalMasEstouraNaMultiplicacao_RecusaSemLancar` (6 números, de 28 dígitos ao `decimal.MaxValue` e 38 dígitos), `Centavos_NoTeto_Passa_EUmCentavoAcima_Recusa`, `SearchTests.PrecoEnormeNoFiltro_…` (quatro casos: o preço mínimo e o máximo mostram a mensagem "Informe um valor em reais…" junto do campo, o valor digitado fica no campo, a lista sai sem a faixa e a resposta é 200, **não 503**) |
| 🟡 3 quatro decisões fora da SPEC | `specs/SPEC.md` v1.5: US-002-S08 "a lista de resultados **sai sem a faixa de preço**"; filtro de área nos quatro tipos de Imóveis (regra de filtros da US-002 e Apêndice B; a área continua obrigatória só em Terrenos); telefone fixo só com "Ligar" (US-004 e US-015); exceção do arquivado na regra de indisponibilidade (US-003). Quatro linhas no histórico; o campo Version, que ainda dizia v1.3, passou a v1.5 | testes que leem a SPEC (`AppendixBTests`, `ListsTests`, `ParityTests`) continuam verdes |
| 🟡 4 mais de 100 favoritos sem teste | testes do servidor e do navegador (a regra dos lotes mora no `favorites.js`) | `FavoritesTests.Api_150Ids_EmDuasChamadas100Mais50_…` e `Fragmento_150Ids_…` (cada lote é atendido na ordem; 150 numa chamada só dão 400; o repositório recebe um pedido de 100 e outro de 50); E2E `MaisDe100Favoritos_BuscamEmLotesDe100Mais50_…` (150 ids no `localStorage`, dois reais, um em cada lote: o navegador faz **duas chamadas, de 100 e de 50 ids**, o segundo lote começa onde o primeiro parou, os dois cards saem na ordem, o aviso diz "148 anúncios favoritados deixaram de estar disponíveis…" e o armazenamento fica só com os dois) e `Exatamente101Favoritos_UmLoteDe100EOutroDe1` |
| 🟡 5 paridade JS × C# dos favoritos | tabela `tests/GazetaMarketplace.Web.Tests/Favorites/favorite-ids-parity.json` (29 valores e 19 listas = **48 entradas**: id 0, menos zero, negativo, `int.MinValue`, `int.MaxValue` e +1, 2^32, acima de 2^53, 1,5, notação científica, texto, texto vazio, dígito de outro alfabeto, `null`, `true`, array e objeto dentro do item, repetidos, ordem, lista só de inválidos, texto, objeto, número e `null` no lugar do array e JSON quebrado), compartilhada pelos dois projetos de teste | `FavoriteIdsParityTests` (lado C#: cada entrada dá o veredito da tabela) e E2E `Paridade_OJavaScriptDaOMesmoResultadoQueATabela_EOServidorConcorda` (o `limpar` do navegador **e** a resposta real do servidor, 200 ou 400, batem com a tabela) |

**Diferenças achadas e corrigidas pela tabela:** o C# aceitava o id `0` e zeros à esquerda (`007`), e o JavaScript descartava os dois. `FavoriteIds` passou a seguir o JavaScript (id de anúncio é um inteiro de 1 em diante, sem zero à esquerda), e os testes antigos que esperavam `0` e `007` válidos foram invertidos. Duas diferenças que ficam **por desenho** e estão escritas na tabela: o JavaScript **descarta** o item inválido de uma lista e o servidor **recusa** o pedido inteiro; e o JavaScript lê `1e3` e `1.0` como 1000 e 1, mas o endereço que ele monta usa o texto canônico (`1000`, `1`).

**Mutações (6; 6 mortas)**

| Mutação | Resultado |
|---|---|
| P1 o preço enorme volta a estourar na multiplicação | morta (4) |
| P2 o servidor volta a aceitar id 0 e zero à esquerda | morta (5) |
| P3 o JavaScript aceita id 0 | morta (3, E2E) |
| P4 lote de 200 ids no `favorites.js` | morta (2) |
| P5 só os ausentes do último lote são contados | morta (2) |
| P6 o JavaScript aceita número escrito como texto ("5") | morta (a tabela de paridade) |

As quatro mutações de JavaScript usaram o método do runbook reescrito (a lista de arquivos publicados, com o site reiniciado a cada rodada).

## Tarefa 6.1 — verificação transversal de acesso e de texto digitado (NFR-13 e NFR-15, 2026-10-06)

> **Em resumo:** agora o site prova por teste, e não por confiança, duas coisas: (1) **toda rota** do site sabe quem pode chamá-la (sem login, Redator ou Administrador), e uma rota nova que ninguém classificou **falha o teste**; (2) texto digitado com código dentro (`<script>`, `<img onerror>`, aspas, `&`, `javascript:`) aparece **como texto** em todas as telas, nunca roda. Nenhuma brecha de acesso ou de texto foi encontrada no produto; a varredura achou só cinco `Html.Raw` de texto fixo, que foram trocados para a lista de exceções ficar vazia.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.692 | 1.692 | 0 |
| Ferramenta de catálogo | 43 | 43 | 0 |
| Ferramenta de municípios | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 161 | 161 | 0 |
| E2E (Playwright, site publicado Production e Development) | 153 | 153 | 0 |

**Testes novos (19 unitários + 4 E2E):**

| Arquivo | O que prova |
|---|---|
| `Security/AccessMatrixTests` (7) | A matriz escrita à mão (77 linhas na 6.1; 78 desde o Checkpoint 6, com `Home.Status`: método, endereço, ação → Público, Entrada do painel, Redator ou Administrador) bate com as rotas descobertas no site (`EndpointDataSource`) nos dois sentidos; o papel declarado no código (`[Authorize]`) bate com a matriz; sem login o painel vai para `/painel/entrar` e a API responde 401, sem corpo; o Redator é negado em toda rota de Administrador (página → `/painel/acesso-negado`; API → 403 "Você não tem permissão para esta operação.") e não é negado nas dele; o Administrador não é negado em lugar nenhum; toda escrita sem token antiforgery responde 400; só as rotas de controller existem, fora elas só arquivos estáticos, saúde e a página padrão |
| `Security/OwnershipMatrixTests` (2) | O Redator B chama as 13 rotas do anúncio da Redatora A (editar, salvar, atualizar, enviar, confirmar, fotos da página e da API): todas negadas (403 ou 404), sem o título nem a descrição na resposta, e o banco fica igual (situação, título, descrição, foto); a autora e o Administrador abrem o mesmo anúncio |
| `Security/XssInAllScreensTests` (6) | Texto hostil de 109 caracteres em título, descrição, cidade, nome de pessoa, motivo de rejeição, nome de categoria e busca (seis ataques isolados mais o combinado): 21 endereços públicos (início, categoria, busca, anúncio com endereço torto, favoritos, fragmento) e 19 do painel (lista, fila, pré-visualização, editar, as seis páginas de confirmação, usuários, categorias, configurações), mais as devoluções de formulário com erro (entrada, usuário, categoria repetida, anúncio novo); nada vira marcação (leitor de marcação procura `<script>` com código, atributo `on…=`, endereço `javascript:`, segundo `</title>`) e o texto **aparece** como texto; a API devolve o título como dado JSON (`application/json`, `nosniff`); o mapa do site continua XML válido |
| `Security/RawOutputTests` (4) | Varredura de todas as views e do código do site: nenhuma saída crua (`Html.Raw`, `HtmlString`, `AppendHtml`, `SetHtmlContent`…), nenhum `<script>` com código, nenhum `on…=`, nenhum `javascript:` fixo; lista de exceções **vazia**; um teste prova que as expressões reconhecem cada forma de risco |
| `Showcase/XssE2ETests` (4, navegador) | Anúncio com título e descrição de ataque cadastrado, enviado e publicado pelas telas, aberto no painel e por visitante (início, anúncio, favoritos, busca); pessoa e categoria com nome de ataque; motivo de rejeição de ataque lido pela autora; busca com os sete textos de ataque, pelo endereço e digitada. Em cada passo: nenhuma janela abre (`alert`), `window.__xss` não existe, não nasce a imagem do ataque, e o texto aparece literal |

**O que foi corrigido no produto:** `Categories/Index.cshtml` usava `@Html.Raw` cinco vezes para abrir e fechar `<ul>`/`<li>` com texto fixo (não era brecha); passou para linhas `@:` do Razor, com a mesma marcação (os 112 testes de categorias passam), para a lista de exceções ficar vazia. No E2E, `PublishingFlow` ganhou o parâmetro `description` e o método `SubmitAsync` (envia para revisão sem publicar), usados pelos testes novos.

**Descobertas (registradas no BACKLOG, nenhuma é brecha):** o site não tem política padrão de entrada (rota nova sem atributo nasce pública; a matriz pega); o JSON da API usa o escape relaxado do MVC (seguro com `application/json` + `nosniff`); a negação por posse é 403 e não 404; o envio de foto valida o arquivo antes da autoria.

**Rodadas do E2E completo:** a 1ª deu 151 de 153 e a 2ª, 152 de 153; as falhas eram de tempo com 4 navegadores em paralelo (`US005S01_S02`, uma vez; `US002S09`, duas vezes) e todas passam isoladas (3 de 3). `US002S09` era uma corrida do teste: escolhia a ordem antes de o `search.js` ligar o envio automático; agora espera o botão "Ordenar" sumir. Com a correção, a 3ª rodada completa deu **153 de 153**.

**Mutações (8 planejadas; 8 mortas)**

| Mutação | Resultado |
|---|---|
| M1 `[AllowAnonymous]` na ação de arquivar | morta (3: papel declarado × matriz, sem login, Redator) |
| M2 `Users` com a política de Redator no lugar da de Administrador | morta (2) |
| M3 `[AllowAnonymous]` no controller de configurações | morta (3) |
| M4 a autoria deixa de ser conferida (`AdAccess.CanView`) | morta (1: matriz de posse). A primeira forma, tirar a autoria só do `CanEdit`, **sobreviveu** por ser equivalente: o `CanView` roda antes e já nega (defesa em duas camadas) |
| M5 `@Html.Raw` no título do card | morta (2: varredura e tela pública) |
| M6 `@Html.Raw` no motivo da rejeição | morta (2: varredura e tela do painel) |
| M7 rota nova `/painel/zzz` fora da matriz | morta (4; a primeira diz qual rota falta classificar) |
| M8 `[IgnoreAntiforgeryToken]` numa escrita | morta (1) |

## Tarefa 6.2 — acessibilidade e larguras de todas as telas (NFR-16 e NFR-17, 2026-10-06)

> **Em resumo:** existe agora **uma lista única de 44 telas e estados** (45 desde o Checkpoint 6, com a página de erro de status) (`tests/GazetaMarketplace.Web.Tests/Screens/screens.json`) e dois testes de navegador que a percorrem: o axe-core (WCAG 2.1 níveis A e AA, em 1280 e em 320 px) e a conferência de rolagem horizontal (320, 768, 1024 e 1280 px). **Nenhuma das 44 telas tem violação do axe, e nenhuma rola na horizontal.** Uma rota de página nova sem entrada na lista falha um teste unitário. A prova de que os testes enxergam é a mutação: rótulo de campo removido, largura fixa e contraste baixo foram pegos, com a mensagem apontando o elemento.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.695 | 1.695 | 0 |
| Ferramenta de catálogo | 43 | 43 | 0 |
| Ferramenta de municípios | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 161 | 161 | 0 |
| E2E (Playwright, site publicado Production e Development) | 246 | 246 | 0 |

**Testes novos:** 3 unitários (`Security/ScreenCoverageTests`) e 93 de navegador (44 de axe, 44 de larguras e 5 de `ScreenListTests`).

| Arquivo | O que prova |
|---|---|
| `Security/ScreenCoverageTests` (3) | Toda rota GET de página (a descoberta das rotas é a da matriz de acesso) está na lista de telas ou na lista de "não é tela", com o motivo (pedaços de HTML, imagem, `robots.txt`, `sitemap.xml`); entrada da lista sem rota também falha; ids únicos, fichas conhecidas, status esperado; quem abre cada tela (visitante ou Administrador) bate com a matriz de acesso da 6.1 |
| `Accessibility/AllScreensTests` (44) | Cada tela, no site publicado, não tem nenhuma violação do axe (etiquetas `wcag2a`, `wcag2aa`, `wcag21a`, `wcag21aa`) em 1280 px e em 320 px; a tela abre com o status esperado (200, 403 ou 404); as telas com passo de preparo são medidas depois dele (painel de filtros aberto, galeria ampliada, favoritos preenchidos) |
| `Responsiveness/AllScreensTests` (44) | Cada tela é aberta de novo em 320, 768, 1024 e 1280 px e `scrollWidth` não passa de `clientWidth`; se passar, a mensagem diz os elementos que ultrapassam a borda |
| `Accessibility/ScreenListTests` (5) | Fichas da lista = fichas que o preparo sabe preencher; ids únicos; telas do site público e do painel; fonte dos casos = a lista inteira; as quatro etiquetas do axe e as quatro larguras travadas |

**As 44 telas:** 15 públicas (início, página de erro, categoria com anúncios, vazia e inexistente, busca em cinco estados, anúncio, galeria ampliada e anúncio inexistente, favoritos vazio e cheio), 4 da entrada do painel (entrar, esqueci a senha, link inválido, sem permissão) e 25 do painel como Administrador (lista de anúncios e vazia, anúncio novo e rascunho, confirmar envio e remoção de foto, fila, pré-visualização, publicar, rejeitar, despublicar, arquivar, categorias em seis estados, configurações, usuários em cinco estados, definir senha e o catálogo de componentes do site em Development).

**Dados:** os dados públicos são criados de verdade pelas telas do painel na primeira tela medida (um anúncio publicado com duas fotos, um rascunho com foto e um anúncio em revisão); a categoria "Telas sem anúncios" e o Redator são reaproveitados se já existirem. O axe mede, portanto, o contraste do que o visitante vê.

**Correções feitas durante a tarefa (todas no teste, nenhuma no produto):** o preparo lia o link de remover foto, que é um formulário e só existe na página recarregada; esperava uma URL exata depois de salvar a categoria; entrava de novo numa página que já estava dentro. Por causa disso, o preparo agora guarda a tarefa (uma falha aparece uma vez, não 88). Em paralelo com os testes de CEP as duas classes novas disputavam a porta do ViaCEP de mentira: ficaram `DoNotParallelize`. E o limite padrão de `Expect` passou de 5 para 15 s no conjunto (um caso por rodada completa, `Acessibilidade_ConfirmarEPaginaDeRejeicao…`, esgotava 5 s sob carga de 4 navegadores; passa isolado 5 de 5).

**Atenção ao rodar a suíte completa duas vezes seguidas no mesmo banco:** os dois casos `CepJs_…` falham se a tabela `CepCache` já tem o CEP `13015100` (os E2E que publicam anúncios, as telas inclusive, o deixam lá); o runbook já pedia limpar a tabela (o `republish.sh` faz isso). Rodando limpo: 246 de 246.

**Mutações (6 planejadas e 1 extra; 7 mortas)**

| Mutação | Resultado |
|---|---|
| M1 tirar uma tela da lista (`Users.Index`) | morta (`ScreenCoverageTests`: rota de página sem tela) |
| M2 rota GET de página nova sem entrada na lista | morta (3 testes: lista de telas, matriz de acesso, Redator) |
| M3 tirar o rótulo de um campo (nome da categoria) | morta no navegador (`label (critical): Form elements must have labels`, 1280 e 320 px). A primeira forma, tirar o rótulo da busca, **sobreviveu** por ser equivalente: o campo tem `placeholder`, que o axe aceita como nome |
| M4 largura fixa de 420 px no site público (CSS) | morta (`320 px: a página tem 440 px… main#conteudo`) |
| M5 largura fixa de 420 px no painel (CSS) | morta (`320 px: a página tem 436 px… main#conteudo, nav, ul.nav`) |
| M6 o axe sem a etiqueta `wcag2aa` | morta (`ScreenListTests.TheAxeRules_AreTheWcag21LevelsAAndAA`) |
| M7 (extra) texto cinza claro no painel | morta (`color-contrast (serious)` no título, 1280 e 320 px) |

Observação do método: o estilo escrito direto no HTML (`style="…"`) é bloqueado pela política de segurança do site e **não** muda nada; a primeira tentativa de M4 assim sobreviveu por esse motivo (e foi refeita por CSS). Está no runbook.

**O que não foi medido (BACKLOG):** teclado e leitor de tela (NVDA) por tela e Firefox/Safari ficam para o `/verify`; os estados que dependem de falha provocada (erro 503, "sem permissão" do painel, erro de entrada com senha errada, redefinição com link válido); o Redator como conta de teste (a tela somente leitura do anúncio em revisão); e o endereço desconhecido, que devolve 404 com corpo vazio e portanto não tem página para medir.

## Tarefa 6.3 — orçamentos de desempenho (NFR-01 a NFR-05, 2026-10-06)

> **Em resumo:** o site público agora sai comprimido (Brotli e gzip) e os arquivos com `?v=` ficam um ano no cache do navegador; o painel **não** é comprimido, de propósito (ataque BREACH). Com o volume da v1 (200 anúncios num SQL Server de verdade) o servidor responde a busca em **34,6 ms** e o detalhe em **11,1 ms** no p95, contra o limite de 500 ms. As páginas pesam **171 KB** (lista de 24) e **168 KB** (detalhe) contra 2 MB e 3 MB. LCP, INP e CLS medidos no perfil de celular ficam dentro dos limites, com **uma exceção real**: a busca em celular lento pode pular (CLS de 0,36), registrada no BACKLOG para decisão.

| Camada | Total | Passaram | Falharam |
|---|---|---|---|
| Unitários do site (SQLite) | 1.702 | 1.702 | 0 |
| Ferramenta de catálogo | 43 | 43 | 0 |
| Ferramenta de municípios | 27 | 27 | 0 |
| Integração (SQL Server 2022 em contêiner) | 162 | 162 | 0 |
| E2E (Playwright, site publicado Production e Development) | 253 | 249 | 0 (4 puladas: as de métricas, que exigem `GAZETA_VITALS=1`) |

**Mudança no produto (pequena e na lista de arquivos do plano):** `Middleware/PerformanceExtensions.cs`, ligada em `Program.cs`. (1) Compressão Brotli e gzip em tudo fora de `/painel`. Os estáticos já saíam pré-comprimidos. Motivo da exceção: o HTML do painel leva o token antiforgery, e comprimir segredo por HTTPS permite o BREACH (decisão do Product Owner, registrada no ARCHITECTURE §7). (2) `Cache-Control: public, max-age=31536000, immutable` para arquivo estático pedido com `?v=` (o `asp-append-version`); antes saía `no-cache` e o navegador revalidava cada CSS e JS a cada visita.

**Números medidos**

| O que | Medido | Limite |
|---|---|---|
| Servidor, busca, início e categoria (200 anúncios, 200 pedidos) | p50 16,4 ms · **p95 34,6 ms** · máx 65,8 ms | p95 < 500 ms |
| Servidor, detalhe (200 pedidos) | p50 7,3 ms · **p95 11,1 ms** · máx 17,5 ms | p95 < 500 ms |
| Lista de 24 cards (celular, cache vazio) | **171 KB**: HTML 5 KB, fontes 99 KB, CSS 35 KB, JS 25 KB, 24 capas de 480 px 7 KB | 2.048 KB |
| Detalhe (8 anúncios da lista; o mais pesado) | **168 KB** (só a 1ª foto grande, as outras sob demanda) | 3.072 KB |
| O que não é foto (HTML, CSS, JS, fontes) | ≈ 170 KB | folga de 256 KB |
| Miniatura de 480 px de uma foto de câmera granulada de 4000 × 3000 px | 1 KB (cabem 74 KB por capa na lista) | 74 KB por capa |
| Versão grande de 1600 px da mesma foto | 279 KB (cabem 2.800 KB no detalhe, já com 20 miniaturas) | 2.800 KB |
| LCP (início, categoria, busca, detalhe) | ≈ 1,0 a 1,3 s · ≈ 1,0 s · 1,5 a 1,9 s · 0,45 a 0,5 s | 2,5 s |
| INP (mesmas páginas; 4 a 9 interações reais cada) | 64 a 112 ms · 64 a 112 ms · 88 a 184 ms · 88 a 96 ms | 200 ms |
| CLS | 0,0000 em início, categoria e detalhe; **busca: 0,0000 na maioria, 0,3561 em 3 de cerca de 25 medições** | 0,1 |

**Testes novos (+7 unitários, +1 de integração, +7 de navegador)**

| Arquivo | O que prova |
|---|---|
| `Performance/CompressionAndCacheTests` (5, unitário) | Site público, API, mapa e `robots.txt` saem em Brotli com `Accept-Encoding: br, gzip` e em gzip com `gzip`, com `Vary: Accept-Encoding`; sem `Accept-Encoding` não comprime; **as páginas do painel nunca saem comprimidas** e continuam levando o token (a razão da regra); **nenhuma página pública leva o token**; arquivo com `?v=` sai com cache de um ano e `immutable`, sem `v`, inexistente ou página com `?v=` não |
| `Performance/PhotoWeightBudgetTests` (2, unitário) | A miniatura de 480 px e a versão grande de 1600 px de uma foto de câmera granulada (a pior que o limite de 10 MB deixa passar) cabem no que sobra do orçamento: 24 miniaturas na lista, e a versão grande mais a faixa de 20 miniaturas no detalhe |
| `IntegrationTests/Performance/VolumeOfV1Tests` (1) | 200 anúncios publicados de quatro tipos (carros, livros, serviços, vagas) em cinco cidades, com 0 a 5 fotos; aquece 20 pedidos e mede 200 de busca com filtros, início e categoria e 200 de detalhe, no site inteiro em processo e SQL Server em contêiner; p95 abaixo do limite de `budgets.json` |
| `Performance/PageWeightTests` (3, navegador) | Lista de 24 e detalhe de 8 anúncios, no perfil de celular e sem cache, pelo protocolo do Chrome: bytes que trafegaram (cabeçalhos e corpo comprimidos) abaixo do limite; o que não é foto abaixo da folga; a lista só baixa capas `-480.webp`; o detalhe baixa uma só foto grande antes de qualquer toque; o HTML sai comprimido; todo arquivo com `?v=` das páginas públicas tem cache imutável |
| `Performance/VitalsTests` (4, navegador, `GAZETA_VITALS=1`) | LCP, INP e CLS no perfil de celular (412 × 823, toque, processador 4 vezes mais lento, rede 4G de 1,6 Mbit/s e 150 ms), com `PerformanceObserver` e interações de verdade (favoritar, filtros, passar e ampliar foto); mostra a fonte de cada mudança de layout |

**Limites do que foi provado:** (1) as fotos do E2E são minúsculas (as 24 capas somam 7 KB), então o peso real com fotos de celular não foi medido; o que está provado é a soma "o que não é foto ≈ 170 KB + 24 miniaturas + a foto grande" com o teto de cada termo (`PhotoWeightBudgetTests` e a folga do E2E). A miniatura sintética pesou 1 KB, mais leve que uma foto de verdade (um caso em `BACKLOG`: medir com 24 fotos reais no `/verify`). (2) O cache do navegador (guardar por um ano e não perguntar de novo) não pôde ser provado: o site de teste usa certificado de desenvolvimento e o Chrome não guarda em cache resposta de HTTPS com erro de certificado; provou-se o cabeçalho em todo arquivo versionado. (3) LCP, INP e CLS são uma **amostra**: rede e processador simulados, site de teste na mesma máquina. Valem os números do `/verify` contra o artefato.

**Achado real (BACKLOG, decisão do Product Owner):** na busca, em celular lento, o painel de filtros nasce aberto no HTML e o `search.js` o recolhe depois da primeira pintura. Com o processador 4 vezes mais lento e a máquina ocupada, a pintura vem antes e a lista de resultados sobe de uma vez (CLS 0,3561; fonte: `div.col-12 › section › article.card`). Não corrigi na 6.3, porque a correção (nascer recolhido por CSS e aberto sem JavaScript por `<noscript>`) muda uma decisão testada da US-002-S12; **foi corrigida no Checkpoint 6** (seção abaixo) com o aval do Product Owner. Outras medições: INP de 224 ms no início numa primeira rodada com a máquina compilando ao lado (as 6 seguintes ficaram em 64 a 112 ms).

**Mutações (7; 7 mortas)**

| Mutação | Resultado |
|---|---|
| M1 a lista baixa a capa de 1600 px | morta (`PageWeightTests`: "a lista só baixa a capa de 480 px", lista as URLs `-1600.webp`) |
| M2 compressão desligada | morta (`PublicPagesAndApi_AreCompressed…`) |
| M3 cache imutável removido | morta (`VersionedStaticFile_GetsOneYearImmutableCache…`) |
| M4 atraso de 600 ms na busca | morta (`VolumeOfV1Tests`: p95 da busca 629,6 ms contra 500 ms) |
| M5 um bloco entra 800 ms depois da carga, empurrando o conteúdo | morta (`VitalsTests`: CLS 0,2903 na busca). A primeira forma da mutação (tirar `width`, `height` e `aspect-ratio` das capas) **não** foi pega: as miniaturas minúsculas chegam antes da primeira pintura e não deslocam nada; foi trocada |
| M6 o limite da lista no `budgets.json` mudado para 1000 bytes | morta (`ListOf24…`: "limite 0 KB"; prova que o teste lê o arquivo) |
| M7 o painel também comprimido (BREACH) | morta (`PanelPages_AreNeverCompressed_…`) |

## Checkpoint 6 — Verificações transversais completas (fechamento da Fase 6, 2026-10-06)

> **Em resumo:** a Fase 6 fecha com as três suítes verdes depois das duas correções de produto do checkpoint (o salto da lista na busca do celular e a página amigável para respostas de erro em branco): **1.707 unitários, 70 das duas ferramentas, 162 de integração e 250 de navegador passam, com 0 falhas** (4 de navegador ficam de fora: as de métricas, que só rodam com `GAZETA_VITALS=1`). A cobertura passa das duas metas (**97,9% de linhas, 91,7% de ramos**). Seis mutações, **seis mortas**. A revisão de código deu **APPROVE com condições** (0 🔴, 7 🟡, 14 🟢). Uma instabilidade não explicada: na primeira rodada completa do navegador, 39 testes de larguras falharam logo no início; as duas rodadas seguintes não repetiram (ver "Achados da rodada").

### Resultado das suítes (última rodada completa, site republicado e `CepCache` limpo)

| Camada | Total | Passaram | Falharam | Puladas |
|---|---|---|---|---|
| Unitários do site (SQLite) | 1.707 | 1.707 | 0 | 0 |
| Ferramenta de catálogo de veículos | 43 | 43 | 0 | 0 |
| Ferramenta de municípios | 27 | 27 | 0 | 0 |
| Integração (SQL Server real, Docker) | 162 | 162 | 0 | 0 |
| E2E (Playwright, site publicado Production e Development) | 254 | 250 | 0 | 4 (métricas, `GAZETA_VITALS=1`) |

**Números corrigidos em relação à revisão:** o total de navegador é **254** (a soma da revisão dava 256 e era só uma conta); a tabela da 6.3 dizia 253 "passaram" e o certo é 249 (253 era o total); o disco tem **45 telas** (`screens.json`) e **78 rotas** na matriz de acesso (`AccessMatrixTests`, com `Home.Status`).

### Cobertura (Gate 6)

Medida como na seção "Correções pós-Checkpoint 4" (`Microsoft.Testing.Extensions.CodeCoverage`, escopo em `coverage.settings.xml`), unitários e integração unidos por linha. Modo greenfield: o gate é o número do repositório inteiro.

| Medida | Unitários | Integração | **União (o gate)** | Meta | Resultado |
|---|---|---|---|---|---|
| Linhas | 96,3% (5.416 de 5.623) | 81,6% (4.588 de 5.623) | **97,9% (5.507 de 5.623)** | ≥ 80% | atendida |
| Ramos | 89,1% (1.562 de 1.754) | 59,5% (1.043 de 1.754) | **91,7% (1.609 de 1.754)** | ≥ 75% | atendida |

| Projeto | Linhas | Ramos |
|---|---|---|
| `GazetaMarketplace.Core` | 98,9% (2.811 de 2.842) | 94,8% (902 de 951) |
| `GazetaMarketplace.Infrastructure` | 96,9% (1.854 de 1.914) | 88,6% (367 de 414) |
| `GazetaMarketplace.Web` | 97,1% (842 de 867) | 87,4% (340 de 389) |

O ramo de uma linha vale o maior número de ramos cobertos entre os dois projetos, então o número de ramos é um piso. Os E2E não entram na conta. Só os unitários já passam das duas metas.

**Métodos a 0% (13 de 773):** nenhum é regra de negócio sem teste *que o produto já chama*. Os mesmos 13 do Checkpoint 5: fábrica de tempo de projeto (`AppDbContextFactory.CreateDbContext`, chamada só pelo `dotnet ef`), construtores estáticos (`FieldLimits`, `AdFormFactory`), `AppRole..ctor` e `RecoveryTokenProvider.CanGenerateTwoFactorTokenAsync` (contrato do Identity), três propriedades de uma linha do `SystemUser` e uma de `AdFieldViewModel.HelpId`, e três métodos de uma linha (`FieldValueParser.Invalid`, `PasswordResetResult.BadLink`, `UserManagement.NotFound`) que o produto chama e que os testes provam pelos caminhos que os usam. **Sem referência em nenhum código:** `PublicRoutes.Ad(int, string)` (atalho sem uso; as telas usam `AdRoutes.Detail`), já no BACKLOG para remover ou usar. Arquivos de menor cobertura: `AppDbContextFactory.cs` 0% (só `dotnet ef`), `AdRoutes.cs` 76,9%, `AdSubmission.cs` 80,0%, `PasswordRecoveryQueue.cs` 83,3%, `FileSystemPhotoStorage.cs` 84,7%; os demais passam de 85%.

### Duas correções de produto do checkpoint

| Correção | Commit | Prova |
|---|---|---|
| **Busca no celular: lista que pulava (CLS 0,36 em celular lento).** O painel de filtros nasce recolhido por CSS no HTML; sem JavaScript, um `<noscript>` com `sem-js.css` o mantém aberto e esconde os botões. SPEC v1.6 (US-002-S12). | `79798a0` | `SearchLayoutShiftTests` (celular lento, CLS medido: 0,0043, limite 0,1); `SearchTests` S12 atualizado; mutação E2 abaixo |
| **Página amigável para respostas de erro em branco (404 e outras).** `UseStatusCodePagesWithReExecute("/Home/Status/{0}")` só para páginas (não `/api`, não `/health`, não arquivos); o status continua o mesmo. | `c7c32b2` | `StatusPagesTests` (404 e 500 na página; API, saúde e arquivo ficam como estavam; status preservado); a tela entrou em `screens.json` (axe e larguras) e na matriz de acesso (`Home.Status`, Público); mutação U4 abaixo |

### Mutações (6; 6 mortas)

| Mutação | Resultado |
|---|---|
| U1 tirar `[Authorize(Policy = Administrator)]` do `UsersController` | morta (`AccessMatrixTests`: `ThePolicyDeclaredInTheCode_MatchesTheMatrix_ForEveryAction` e `Writer_ReachesWriterEndpoints_ButIsDeniedEveryAdministratorEndpoint`) |
| U2 `@Html.Raw(Model.Title)` em `_NoResultsState.cshtml` | morta (`RawOutputTests.NoView_WritesTextWithoutEncoding`) |
| U3 compressão desligada (`UsePublicResponseCompression` comentada) | morta (`CompressionAndCacheTests.PublicPagesAndApi_AreCompressed…`) |
| U4 sem a página de status (`UseStatusCodePagesWithReExecute` trocada) | morta (`StatusPagesTests`, 2 falhas). A primeira forma (comentar a linha) quebrou a compilação e não valeu; refeita trocando a chamada |
| E1 `main { min-width: 700px }` no `base.css` | morta (`AllScreensTests.Screen_DoesNotScrollHorizontally_AtFourWidths`, 3 de 3 telas medidas; o axe das mesmas telas continuou verde, como esperado) |
| E2 painel de filtros de novo aberto no HTML (`class="collapse show …"`) | morta (`SearchLayoutShiftTests`) |

Depois de cada mutação o código voltou ao original (`git checkout`) e o site foi republicado; o teste do salto da busca passou de novo no código restaurado.

### Achados da rodada

1. **Instabilidade não explicada, a acompanhar:** na primeira rodada completa do navegador depois da republicação, 39 testes `Screen_DoesNotScrollHorizontally_AtFourWidths` falharam em cerca de 200 ms cada (parece falha de preparação dos dados, e não de layout); o log dessa rodada foi cortado por um filtro meu e a mensagem se perdeu. A segunda rodada completa (254 testes) deu 2 falhas nos `CepJs_*` (a primeira rodada deixou o `CepCache` sujo, armadilha já documentada) e **0** nas larguras; a terceira, limpa (site republicado, `CepCache` zerado), deu **0 falhas**. O teste de larguras também passou nas mutações E1. Não consegui reproduzir. No `/test`, rodar o E2E sempre com o log inteiro salvo em arquivo.
2. **Pontos da revisão que ficam abertos para decisão** (🟡 do `reports/CODE_REVIEW.md`, seção Checkpoint 6): cache imutável de um ano para módulos de página sem versão nos módulos que eles importam; capas sem prova de `width`/`height`; peso da "foto granulada" sem prova do limite de 74 KB; página de status sem critério na SPEC nem linha no ARCHITECTURE §7; uso de `!` em 12 pontos dos testes. A linha v1.6 da SPEC e os números deste relatório já foram corrigidos (🟡 4 e 6).

### Entrega para o `/test` e o `/verify`

**O `/test` roda** (Docker ligado; se o daemon parar: `dockerd` e `docker start gazeta-e2e-sql`):

| Suíte | Comando | Observação |
|---|---|---|
| Unitários do site | `dotnet run --project tests/GazetaMarketplace.Web.Tests` | 1.707 esperados, sem Docker |
| Ferramentas | `dotnet run --project tests/CitiesImport.Tests` e `tests/VehicleCatalogExport.Tests` | 27 e 43 |
| Integração | `dotnet run --project tests/GazetaMarketplace.IntegrationTests` | 162, usa Docker; contém `VolumeOfV1Tests` (p95 abaixo de 500 ms) |
| Cobertura | comando do runbook (seção "Cobertura de código") | metas 80% e 75%; a lista de métodos a 0% acima |
| E2E | republicar o site de teste (limpa `PasswordRecoveryAttempts` e `CepCache`) e rodar `tests/GazetaMarketplace.Web.Tests.Playwright` com as variáveis do runbook | 254 esperados (250 passam, 4 puladas); salvar o log inteiro em arquivo |
| Métricas de celular | `GAZETA_VITALS=1`, **sozinho**, sem compilação ao lado | LCP, INP e CLS; os 4 testes pulados |
| Telas | `GAZETA_SCREENS=<ids>` para repetir só algumas | 45 telas, axe e quatro larguras |

**Armadilhas conhecidas:** o `CepCache` sujo derruba os `CepJs_*` (limpar ao republicar); a porta única do ViaCEP de mentira obriga `[DoNotParallelize]` nas classes que a usam; sem `RateLimiting__GlobalPerMinute=1000` e `RateLimiting__PhotosPerMinute=5000` o E2E recebe 429; o estilo escrito direto no HTML (`style=`) é bloqueado pela política de segurança, então mutação de visual é por CSS; mutar `wwwroot` exige republicar.

**Fica para o `/verify` (manual ou fora deste ambiente):** celular de verdade e abertura do WhatsApp; Firefox e Safari; teclado e leitor de tela (NVDA); peso com fotos reais de celular; cache do navegador com certificado real (o Chrome não guarda cache de HTTPS com erro de certificado); compressão no IIS; LCP, INP e CLS medidos no site publicado; carga real dos municípios do IBGE, componente nativo do Magick.NET (HEIC e WebP) e script do banco com `sqlcmd -I` na hospedagem; catálogo real de veículos.

**Testes que o verde não mostra:** os 4 de métricas (`GAZETA_VITALS`), o do peso da lista quando faltam 24 anúncios na categoria "Livros e revistas" (fica inconclusivo) e as 2 telas do catálogo de componentes sem `GAZETA_DEV_BASE_URL`: listar como pendência no `/test` se não rodarem.

## Correções pós-Checkpoint 6 (2026-10-06)

> **Em resumo:** as decisões do Product Owner sobre os avisos 🟡 e os dois 🟢 que escondiam defeito foram executadas. **Corrigido com teste:** o cache de um ano agora cobre a cadeia de módulos de cada página (antes um deploy podia juntar página antiga com módulo novo); a página de erro de quem está logado no painel **saía comprimida** (BREACH; reproduzido em 5 rotas, corrigido); o botão "Filtros" abre o painel mesmo se o JavaScript do Bootstrap não carregar; o teste de peso da foto agora prova o limite (a miniatura de pior caso pesa 45 KB contra os 74 KB, antes pesava 1 KB); as capas têm teste de `width`/`height`/proporção; os 12 `!` saíram. **1.722 unitários, 70 das ferramentas, 162 de integração e 253 de navegador passam, com 0 falhas** (4 de navegador puladas: as de métricas). **12 mutações, 12 mortas.**

### Resultado das suítes (rodada completa depois das correções, site republicado, `CepCache` limpo)

| Camada | Total | Passaram | Falharam | Puladas |
|---|---|---|---|---|
| Unitários do site (SQLite) | 1.722 | 1.722 | 0 | 0 |
| Ferramenta de catálogo de veículos | 43 | 43 | 0 | 0 |
| Ferramenta de municípios | 27 | 27 | 0 | 0 |
| Integração (SQL Server real, Docker) | 162 | 162 | 0 | 0 |
| E2E (Playwright, site publicado Production e Development) | 257 | 253 | 0 | 4 (métricas, `GAZETA_VITALS=1`) |

Testes novos: 15 unitários (12 de `ModuleVersionsTests`, 1 de capas em `CardTests`, 2 de BREACH em `CompressionAndCacheTests`) e 1 de navegador (`SearchE2ETests`: o painel com o JavaScript do Bootstrap bloqueado). Unitários: 1.707 → 1.722. Navegador: a listagem (`--list-tests`) dá **256 no commit do Checkpoint 6 e 257 agora** (+1, o novo); a rodada de fechamento do Checkpoint 6 tinha relatado total 254, dois a menos que a listagem, e a rodada de hoje executou todos os 257. **Não investiguei por que as rodadas anteriores contaram 2 a menos** (testes ignorados por variável faltando seria a hipótese); no `/test`, conferir que o total executado bate com a listagem. A cobertura (97,9% de linhas, 91,7% de ramos) é a do Checkpoint 6; o código novo tem teste direto.

### O que mudou e como foi provado

| Decisão | Correção | Prova |
|---|---|---|
| **D1.1 cache de módulos** | `ModuleVersions` + `ModuleScriptTagHelper`: o `?v=` de um script de página é o hash da página e de **todos** os módulos que ela importa (`import`, `export … from`, `import()` relativos). 13 views passaram de `asp-append-version` para `asp-module-version`. Os módulos importados saem sem `?v=` e continuam revalidando (um pedido condicional a mais por módulo, sem baixar o arquivo de novo) | `ModuleVersionsTests`: mudar qualquer arquivo da cadeia muda a versão; mudar um módulo alheio não muda; ciclo; `import()`; **contra os arquivos reais de `wwwroot/js`** (cada `import` relativo achado por outra expressão, mais simples, muda a versão de quem importa); toda página sai com `?v=` de 16 caracteres |
| **D1.2 peso da foto** | A foto de pior caso passa a ser 4000 × 3000 com grão de sensor (±32 de 255) que sobrevive à redução: miniatura de **45 KB** (limite por capa 74 KB), versão grande de 422 KB (cabem 1.919 KB). O teste exige que a miniatura pese ao menos um quarto do limite, para não voltar a passar vazio. Ruído total (0 a 255 por canal) deu 101 KB, acima do limite, mas isso não é uma foto de câmera. **Não há foto real no repositório**: a conferência com fotos de verdade (HEIC de iPhone, JPEG grande de Android, 24 fotos) continua no `/verify`, com amostras do Product Owner | `PhotoWeightBudgetTests`; mutação N4 |
| **D1.3 operador `!`** | Os 12 usos dos testes novos (`PageMeter`, `ScreenData`, `AccessMatrixTests`) saíram (nullable está desligado, o `!` não fazia nada). Há ~60 usos antigos em outros testes, fora do escopo, no BACKLOG | `dotnet format --verify-no-changes` e build Release sem aviso |
| **D1.5 capas** | Teste novo: a capa do card traz `width` e `height` positivos no HTML e o CSS fixa `aspect-ratio`, `width: 100%` e `height: auto` | `CardTests.ACapa_ReservaOEspaco…`; mutação N3 |
| **D2.1 BREACH em página reexecutada** | **Era real:** 404 de `/painel/...` saía em Brotli (a página de erro tem o caminho `/Home/Status/404`). A regra olha também o caminho do pedido original (`IStatusCodeReExecuteFeature.OriginalPath` e `IExceptionHandlerPathFeature.Path`). A página de erro vista por quem está logado não leva o token (conferido), então não houve vazamento do segredo, mas a regra "o painel nunca é comprimido" estava furada | `CompressionAndCacheTests`: 5 rotas do painel com 404 e uma falha lançada num servidor mínimo com a mesma ordem do `Program.cs`; mutação N2 |
| **D2.2 painel sem JavaScript do Bootstrap** | **Não estava coberto** (o `<noscript>` só vale com JavaScript desligado, e o botão era um `<button>` que depende do Bootstrap). O botão virou um link `href="#filtros"` (padrão do Bootstrap) e o `search.css` mostra o painel pelo `:target`; com o JavaScript funcionando nada muda | `SearchE2ETests.US002S12_Celular_SeOJavaScriptDoBootstrapNaoCarrega…`; mutação N5 |
| **D1.4 SPEC e ARCHITECTURE** | SPEC v1.7: NFR-25 (página de status), os dois critérios aprovados (D4) e o botão como link; ARCHITECTURE §7: BREACH reexecutado, páginas de status e versão dos scripts | — |

### Mutações (11; 11 mortas)

| Mutação | Resultado |
|---|---|
| U1 tirar `[Authorize(Policy = Administrator)]` do `UsersController` | morta (`AccessMatrixTests`, 2 testes) |
| U2 `@Html.Raw(Model.Title)` em `_NoResultsState.cshtml` | morta (`RawOutputTests.NoView_WritesTextWithoutEncoding`) |
| U3 compressão pública desligada | morta (`PublicPagesAndApi_AreCompressed…`) |
| U4 sem a página de status | morta (`StatusPagesTests`, 2 testes) |
| N1 a versão do script deixa de seguir os imports | morta (`ModuleVersionsTests`, 7 testes) |
| N2 a regra do painel olha só o caminho atual (volta o BREACH reexecutado) | morta (os 2 testes novos de `CompressionAndCacheTests`) |
| N3 capa sem `width` e `height` | morta (`CardTests`, 2 testes) |
| N4 qualidade do WebP a 100 | morta (`PhotoWeightBudgetTests`: miniatura de 365 KB contra 74 KB) |
| E1 `main { min-width: 700px }` | morta (`AllScreensTests.Screen_DoesNotScrollHorizontally_AtFourWidths`, 3 de 3 telas) |
| E2 painel de filtros de novo aberto no HTML | morta (`SearchLayoutShiftTests`). **A primeira tentativa não valeu:** o `sed` usou o número de linha antigo, não mudou o arquivo e o teste passou; refeita pelo texto |
| N5 sem a regra `:target` do `search.css` | morta (`US002S12_Celular_SeOJavaScriptDoBootstrapNaoCarrega…`) |

Depois de cada mutação o código voltou ao original (`git status` limpo), o site foi republicado e os testes do salto e do botão de filtros passaram de novo no código restaurado. Mutação com número de linha é frágil; mudar o arquivo e conferir `git diff --numstat` antes de rodar.

### Achado para o `/test`

O teste "Filtros" do E2E agora acha o botão pelo papel `button` (o link tem `role="button"`); qualquer teste novo que procure `<button>` ali não acha.

### Pronto para o `/test` (estado em 2026-10-06, depois destas correções)

| Item | Estado |
|---|---|
| Código | `claude/admiring-cray-wrjfmg`, árvore limpa; `dotnet build -c Release` sem aviso; `dotnet format --verify-no-changes` limpo |
| Docker | contêiner `gazeta-e2e-sql` ligado (SQL Server do E2E, porta 14330, banco `gazeta_e2e`); a integração sobe os próprios contêineres. Se o daemon parar: `dockerd` e `docker start gazeta-e2e-sql` |
| Sites de teste | publicados do código atual (Release): Production em `https://localhost:5443` e Development em `https://localhost:5444` (só para o catálogo de componentes); o `CepCache` e o `PasswordRecoveryAttempts` foram limpos na republicação |
| Variáveis do E2E | `PLAYWRIGHT_BROWSERS_PATH`, `GAZETA_BASE_URL`, `GAZETA_DEV_BASE_URL`, `GAZETA_E2E_EMAIL`, `GAZETA_E2E_PASSWORD`, `GAZETA_E2E_SESSION_MINUTES=1`, `GAZETA_E2E_SENDGRID_PORT=5990`, `GAZETA_E2E_VIACEP_PORT=5991`; as do site: `RateLimiting__GlobalPerMinute=1000`, `RateLimiting__PhotosPerMinute=5000`, `RateLimiting__PhotoUploadsPerMinute=1000`, `Authentication__SessionMinutes=1` (runbook: seção do E2E) |
| Totais esperados | unitários 1.722 · municípios 27 · catálogo 43 · integração 162 · E2E 257 (253 passam, 4 puladas: métricas) |
| Comandos | `dotnet run --project tests/GazetaMarketplace.Web.Tests`, `tests/CitiesImport.Tests`, `tests/VehicleCatalogExport.Tests`, `tests/GazetaMarketplace.IntegrationTests` e `tests/GazetaMarketplace.Web.Tests.Playwright` (E2E, com as variáveis acima); cobertura pelo comando do runbook. Todos rodaram hoje, sem falha |
| Ao republicar | limpar `CepCache`; salvar o log inteiro do E2E em arquivo; conferir que o total executado bate com `--list-tests` (257) |

## /test — Gate 6 da Fase 6 (2026-10-06)

> **Em resumo:** a suíte completa rodou contra o código do commit `7f3d3d1` e contra o artefato publicado (Release, Production em `:5443` e Development em `:5444`, SQL Server real em contêiner). **Nenhuma falha.** 1.722 unitários, 70 das ferramentas, 162 de integração, 253 de navegador e 4 de métricas (rodadas sozinhas com `GAZETA_VITALS=1`) passam; nada ficou pulado no conjunto. Cobertura **98,0% de linhas e 91,8% de ramos** (metas 80% e 75%). A investigação pedida sobre a contagem de testes de navegador terminou assim: **os 257 testes listados foram todos executados**; os 254 das rodadas anteriores não se repetiram (causa mais provável abaixo, não provada). **Veredito: PASS.**

### 1. Resumo

| Suíte | Total | Passaram | Falharam | Puladas | Tempo | Log |
|---|---|---|---|---|---|---|
| Unitários do site (SQLite) | 1.722 | 1.722 | 0 | 0 | 3 min 56 s | `reports/test-artifacts/report/test-GazetaMarketplace.Web.Tests.log` |
| Ferramenta de municípios | 27 | 27 | 0 | 0 | 2 s | `test-CitiesImport.Tests.log` |
| Ferramenta de catálogo de veículos | 43 | 43 | 0 | 0 | 2 s | `test-VehicleCatalogExport.Tests.log` |
| Integração (SQL Server real, TestContainers) | 162 | 162 | 0 | 0 | 2 min 37 s | `test-GazetaMarketplace.IntegrationTests.log` |
| E2E de navegador (site publicado) | 257 | 253 | 0 | 4 | 16 min 24 s | `test-e2e.log` |
| Métricas de celular (`GAZETA_VITALS=1`, sozinhas) | 4 | 4 | 0 | 0 | 26 s | `test-vitals.log` |
| **Conjunto** | **2.211 testes distintos** (os 4 das métricas são os 4 pulados do E2E) | **2.211** | **0** | **0** | **24 min 29 s** (1.469 s do primeiro ao último comando, com a compilação do E2E) | |

Todos os comandos saíram com código 0. Pontuação dos testes de métricas (LCP, INP, CLS; celular, 4G, processador 4 vezes mais lento): início LCP 1.120 ms · INP 88 ms · CLS 0,0000; categoria 1.236 · 48 · 0,0000; busca 1.164 · 112 · 0,0000; detalhe 436 · 80 · 0,0000 (limites 2.500 ms, 200 ms, 0,1). **O CLS da busca, que era 0,36 no Checkpoint 6, agora é 0,0000.**

### 2. Unitários e em memória (reexecução)
1.722 de 1.722. Sem dependência externa; os que usam o servidor de teste rodam com SQLite em memória ou com fakes escritos à mão.

### 3. TestContainers
162 de 162 contra o SQL Server 2022 real em contêiner (uma instância por conjunto de testes), incluindo `VolumeOfV1Tests` (p95 abaixo de 500 ms com cerca de 200 anúncios) e o teste diferencial das colunas calculadas.

### 4. Playwright (reexecução no código atual)
Faz parte do item 5: os testes de navegador rodam contra o site publicado; não há uma segunda rodada em processo.

### 5. E2E contra o artefato
253 passaram e 4 foram puladas por falta de `GAZETA_VITALS=1`; as 4 rodaram à parte e passaram (item 1). **Total executado = total listado** (`--list-tests`): 257 listados, 257 com estado final no log de diagnóstico (253 passou, 4 pulou; 0 falhou). Os dois sites (`:5443` Production, `:5444` Development, só para o catálogo de componentes) foram republicados do código atual antes da rodada, com `CepCache` e `PasswordRecoveryAttempts` zerados.

**Investigação da contagem (pedida pelo Product Owner).** Fatos: a listagem dava 256 testes no commit do Checkpoint 6 e 257 agora (+1: o teste do painel de filtros sem o JavaScript do Bootstrap); as duas rodadas completas do fechamento do Checkpoint 6 tinham relatado total 254.
- *(a) `[Ignore]` sem documentação:* não há nenhum `[Ignore]` no projeto de navegador.
- *(b) filtro de `TestCategory` excluindo em silêncio:* não há `[TestCategory]` no projeto, e nenhuma rodada usou `--filter` (exceto a das métricas, de propósito).
- *(c) `RequiresVariables` ignorando sem avisar:* um teste que falta variável aparece como **pulado** (e as 4 puladas são só as de métricas, com a mensagem "Defina GAZETA_BASE_URL, GAZETA_VITALS"); os `Assert.Inconclusive` (peso da lista, telas do catálogo de componentes) também contam como pulado. Nenhum apareceu.
- *(d) contagem dupla:* a listagem tem 257 nomes únicos; não há duplicata.
- *Prova por reexecução:* o código do Checkpoint 6 (`5f48725`), compilado do zero numa cópia limpa e rodado inteiro, executou **256 de 256** (251 passaram, 4 puladas, 1 falha por instabilidade, descrita abaixo); a rodada de hoje executou **257 de 257**. Ou seja, no mesmo código, uma compilação nova executa tudo. **As duas rodadas de 254 foram feitas com `dotnet run --no-build` sobre um binário de teste que eu não recompilei** (o script do ambiente de teste não compila); a hipótese mais provável, **não provada**, é que o binário tinha a lista de telas (`screens.json`) sem a tela `not-found-page` (dois testes por tela: axe e larguras, 44 telas em vez de 45); os contadores de progresso das rodadas batem com dois testes de tela a menos antes do fim. Não consegui recuperar o binário antigo para confirmar.
- *Conclusão e medida:* **é problema de ambiente, não de produto nem de teste.** O runbook ganhou a regra: compilar o projeto de navegador antes de qualquer `--no-build`, e conferir que o total executado bate com `--list-tests`. Nesta rodada o `dotnet build` do projeto de navegador rodou antes (`build-e2e.log`, 0 erros).
- *Instabilidade vista na reexecução do Checkpoint 6 (fora desta rodada):* `FavoritesE2ETests.US005S06_US011S04_…` falhou uma vez ("Enviar para revisão?" não apareceu em 15 s), a mesma família do título da página de confirmação que já tinha causado instabilidade na 5.6. Nas outras três rodadas completas do mesmo código (e na de hoje) passou. Registrada no BACKLOG.

### 6. Cobertura (Gate 6, modo greenfield: número do repositório inteiro)
Medida pelo comando do runbook (`Microsoft.Testing.Extensions.CodeCoverage`, escopo em `coverage.settings.xml`), unitários e integração unidos por linha.

| Medida | Unitários | Integração | **União (o gate)** | Meta | Resultado |
|---|---|---|---|---|---|
| Linhas | 96,4% (5.490 de 5.697) | 81,7% (4.654 de 5.697) | **98,0% (5.581 de 5.697)** | ≥ 80% | atendida |
| Ramos | 89,2% (1.595 de 1.788) | 59,8% (1.070 de 1.788) | **91,8% (1.642 de 1.788)** | ≥ 75% | atendida |

| Projeto | Linhas | Ramos |
|---|---|---|
| `GazetaMarketplace.Core` | 98,9% (2.811 de 2.842) | 94,8% (902 de 951) |
| `GazetaMarketplace.Infrastructure` | 96,9% (1.854 de 1.914) | 88,6% (367 de 414) |
| `GazetaMarketplace.Web` | 97,3% (916 de 941) | 88,2% (373 de 423) |

A parte nova da fase (`ModuleVersions`, `ModuleScriptTagHelper`, a regra de compressão do painel) entrou na conta e subiu o total em relação ao Checkpoint 6 (97,9% e 91,7%). O ramo é um piso (maior número de ramos cobertos entre os dois projetos por linha). **Métodos a 0% (13 de 783), os mesmos do Checkpoint 6:** `FieldLimits..cctor`, `AdFormFactory..cctor` (construtores estáticos), `AppDbContextFactory.CreateDbContext` (chamada só pelo `dotnet ef`), `AppRole..ctor` e `RecoveryTokenProvider.CanGenerateTwoFactorTokenAsync` (contrato do Identity), as três propriedades de uma linha do `SystemUser`, `AdFieldViewModel.get_HelpId`, três métodos de uma linha que o produto chama e que os testes provam pelos caminhos que os usam (`FieldValueParser.Invalid`, `PasswordResetResult.BadLink`, `UserManagement.NotFound`) e `PublicRoutes.Ad(int, string)`, **sem nenhuma referência em código** (atalho sem uso, no BACKLOG). Nenhum é regra de negócio sem teste que o produto já chame. Menores arquivos: `AppDbContextFactory.cs` 0% (só o `dotnet ef`), `AdRoutes.cs` 76,9%, `AdSubmission.cs` 80,0%, `PasswordRecoveryQueue.cs` 83,3%, `FileSystemPhotoStorage.cs` 84,7%.

### 7. Checklist do Gate 6

| Item | Resultado |
|---|---|
| Todos os testes passam, confirmado pelos comandos (código de saída 0) | **sim**, 6 comandos com saída 0 |
| Os projetos de navegador não quebram o `dotnet run` dos unitários | sim (projetos separados; o `dotnet run` dos unitários saiu com código 0) |
| Nenhuma configuração de produção alterada para isolar os testes | **sim**: `git status` limpo antes e depois da rodada; nada em `src/`, `appsettings*.json`, `Program.cs` nem `docker-compose*` mudou; a troca é em memória (fábrica do servidor de teste e variáveis de ambiente) |
| Schema do banco tocado nesta rodada | não; `db/schema-snapshot/` não se aplica |
| Tripwire de conexões (lista de hosts permitidos) | **passou, com 3 itens explicados** (abaixo) |
| Cada `@US-XXX-Snn` tem teste que cita o cenário | **128 de 128** (conferido por script: nome do teste ou texto). A auditoria de que o teste afirma o efeito (e não só a presença) foi feita no `/review` do Checkpoint 5 e do 6 |
| Contrato entre quem chama a API e a API | os pedidos do JavaScript (`apiFetch`) e do servidor são exercitados pelos E2E, e qualquer resposta de erro do próprio site durante uma jornada derruba o teste; não há verificação automática contra um arquivo OpenAPI (a API pública tem poucos endpoints; no BACKLOG) |
| Regra em duas representações (paridade) | sim: tabela de 48 ids dos favoritos (JavaScript × C#), tabela do leitor de número (`Paridade_*` no E2E), teste diferencial das colunas calculadas (integração) |
| Cobertura por modo | greenfield: 98,0% / 91,8% (item 6) |
| Sem testes pulados ou desabilitados | **sim no conjunto** (os 4 pulados do E2E rodaram à parte e passaram) |
| Correções têm teste de reprodução | sim (cache de módulos, BREACH em página de erro, botão de filtros sem JavaScript, peso da foto, capas) |
| E2E dos caminhos críticos | sim (publicação, revisão, favoritos, busca, contato, painel) |
| Artefatos no caminho canônico | **em parte:** `reports/test-artifacts/report/` guarda os logs de cada suíte e os tempos; o executor de testes (MSTest sobre a plataforma de testes da Microsoft) não gera `results.json` nem `.trx` (precisaria de um pacote novo, pelo processo de decisão de tecnologia). Nenhuma falha, então `runner/` ficou vazio. O diretório inteiro é ignorado pelo git; as saídas estão neste relatório |
| Nenhum código de `src/` alterado durante o `/test` | sim |

**Tripwire.** Hosts achados nos logs: `viacep.com.br` (4 linhas; um teste unitário cria o cliente HTTP do ViaCEP com um **manipulador falso** e a resposta chega em 0,85 ms; o teste `CepEndpointTests` confere o endereço pedido no próprio manipulador, então nenhuma conexão sai da máquina; o endereço é o padrão do `appsettings.json`, e por isso a regra pede para tratar como "achado", mas aqui é falso positivo provado), `db.interno` e `prod` (texto dentro de mensagens de exceção inventadas pelos testes que provam que a página de erro não vaza segredo) e endereços `10.x` (dados dos testes de cabeçalhos de proxy). Nenhum host real de banco, cache ou mensageria apareceu; o SQL Server do E2E é `localhost,14330` e os contêineres da integração são de localhost.

### 8. Bugs
**Nenhum BUG novo.** Nenhuma falha nesta rodada. Uma instabilidade, fora da rodada, está no BACKLOG (item 5 acima).

### 9. Lacunas adiadas (cada uma com o próximo responsável)

| Lacuna | Dono |
|---|---|
| Celular de verdade e abertura do WhatsApp; Firefox e Safari; teclado e leitor de tela (NVDA) | `/verify` |
| Peso com fotos reais (HEIC de iPhone, JPEG grande de Android, 24 fotos) e orientação, cor e GPS retirado; os testes de peso usam foto sintética | `/verify`, com amostras do Product Owner |
| LCP, INP e CLS no site publicado com certificado válido; cache do navegador com HTTPS real; compressão duplicada no IIS | `/verify` |
| Componente nativo do Magick.NET (HEIC e WebP) e checklist de implantação (`Site__BaseUrl`, `sqlcmd -I`, `ARITHABORT` e `QUOTED_IDENTIFIER`) | `/infra` |
| Carga real dos municípios (IBGE) e catálogo real de veículos | Product Owner |
| CEP e municípios contra a rede real (o ambiente bloqueia `viacep.com.br`) | `/verify` |
| Cenários só de interface com prova no navegador de teste (Chromium); outros navegadores | `/verify` |
| Rodar com `GAZETA_VITALS=1` na máquina de produção, sem outra carga ao lado | `/verify` |

### 10. Arquivos acrescentados
Nenhum arquivo de teste novo nesta rodada; só os logs em `reports/test-artifacts/report/` (ignorados pelo git) e este relatório. **Fronteira:** nenhum arquivo de `src/` foi alterado durante o `/test`.

### 11. Veredito do Gate 6
**PASS.** Tudo passa pelos comandos canônicos, a cobertura passa das duas metas, não há BUG aberto e o conjunto não tem teste pulado. Pontos para o `/review` e o `/verify` abaixo.

### 12. Itens abertos para o `/review`

| Id | Item | Origem |
|---|---|---|
| OPEN-001 | Instabilidade de `FavoritesE2ETests.US005S06_…` ("Enviar para revisão?" não aparece em 15 s), 1 vez em 5 rodadas completas do mesmo código | Investigação da contagem |
| OPEN-002 | Dois testes de tela (axe e larguras de `not-found-page`) e a regra "compilar antes de `--no-build`": confirmar que o runbook basta | Investigação da contagem |
| OPEN-003 | Tripwire: confirmar o falso positivo do `viacep.com.br` (manipulador falso) | Item 7 |
| OPEN-004 | Os 4 testes de métricas só rodam com `GAZETA_VITALS=1`; incluir no roteiro do `/verify` | Item 1 |
| OPEN-005 | Cerca de 60 usos antigos do operador `!` nos testes, sem efeito (nullable desligado) | BACKLOG |
| OPEN-006 | As 14 sugestões 🟢 do `/review` do Checkpoint 6 que continuam abertas | BACKLOG |
| OPEN-007 | Sem `results.json`/`.trx` do executor: decidir se vale o pacote de relatório | Item 7 |

## /verify — verificação do artefato (2026-10-07)

> **Em resumo:** o artefato publicado (`6bb592c8…83ec`, commit `6953fa3`) **passou em tudo que a máquina consegue provar**: 254 de 254 testes de navegador executados, 4 de 4 métricas de velocidade rodadas à parte, contrato HTTP com 52 verificações sem falha. O veredito é **PASS WITH CONDITIONS** (ver `reports/VERIFY_REPORT.md`): itens manuais bloqueadores M6, M7 e M10 do `docs/VERIFY-CHECKLIST.md`, o V-01 (P0, `/infra`) e a ausência de perfil Staging; a rastreabilidade fechou com 87 provados e 43 dispensados.

| Item | Resultado |
|---|---|
| Trava do artefato | `reports/verify-artifact.lock`: digest da pasta publicada `6bb592c8aa26…b83ec`; Production, HTTPS autoassinado, SQL Server em contêiner |
| Liveness | 12 / 12 |
| Contrato HTTP (curl no artefato) | 52 / 52; 3 observações (V-01 a V-03) |
| E2E no artefato | 258 listados = 254 passaram + 4 ignorados; 0 falhas; 13 min 07 s |
| Métricas de velocidade (`GAZETA_VITALS=1`, sozinhas) | 4 / 4: LCP 916 · 1.068 · 1.100 · 440 ms; INP 56 · 56 · 80 · 64 ms; CLS 0,0000 (busca com script atrasado: 0,0043) |
| Peso (fotos sintéticas) | lista 171 KB, detalhe 168 KB (limites 2 MB e 3 MB) |
| Limite de login no artefato (limites padrão) | 5 passam, o 6.º recebe 429 com `Retry-After: 900` |
| Rastreabilidade | **87 / 130** provados no navegador no artefato; **43 dispensados** com prova em processo (waiver do Product Owner, 2026-10-07; 2 deles aguardam o "de acordo"); 0 sem teste. 3 E2E renomeados (rodaram: 3/3; total segue 258). `reports/VERIFY_MATRIX.md`; V-04 agora P2 |
| Fronteira | Nenhum arquivo de `src/` nem de `tests/` foi alterado pelo `/verify` |

**OPEN do `/review` fechados ou movidos por este `/verify`:** OPEN-004 (`GAZETA_VITALS`) **fechado**: os 4 testes de métricas rodaram sozinhos e passaram; falta repeti-los na hospedagem (V-08). OPEN-001 (instabilidade de `US005S06`) **sem recorrência** em 3 rodadas completas hoje (258 testes cada). OPEN-005 e OPEN-007 seguem diferidos (`/simplify`, `/infra`).
