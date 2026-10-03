# Relatório de testes — Fase 1 (tarefas 1.1 a 1.4)

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
