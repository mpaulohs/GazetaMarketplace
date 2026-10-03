# Relatório de testes — Fase 1 (tarefas 1.1 a 1.3)

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
