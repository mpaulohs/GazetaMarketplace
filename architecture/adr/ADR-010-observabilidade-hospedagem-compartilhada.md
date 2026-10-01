# ADR-010: Observabilidade em hospedagem compartilhada (desvio de `rules/monitoring.md`)

**Date**: 2026-09-30
**Status**: Accepted

> **Em resumo:** na hospedagem compartilhada não há onde rodar Prometheus, Grafana nem Jaeger. A v1 registra logs estruturados do Serilog em arquivos diários fora da raiz do site, com identificador de correlação e dados sensíveis mascarados, e expõe `/health/live` e `/health/ready`. Métricas e rastreamento ficam para a v2.

## Context
- `rules/monitoring.md` define Serilog + Prometheus + Grafana + Jaeger como padrão; desvio exige ADR.
- NFR-18: correlação por requisição, nada de senha, token ou e-mail em texto claro, e o código de referência das telas de erro precisa levar ao registro no log.
- Hospedagem SmarterASP.NET (IIS compartilhado): sem processos auxiliares, sem acesso ao servidor; os arquivos da conta são acessados pelo painel ou por FTP.
- A saída padrão (`stdout`) do processo não é coletada pelo IIS em produção.

## Options Considered
| Option | Pros | Cons |
|--------|------|------|
| **A. Serilog em arquivo JSON rotativo + health checks; métricas na v2** | Funciona no plano atual sem custo; atende à NFR-18; arquivos acessíveis pelo painel ou FTP | Sem painéis nem alertas automáticos; análise manual dos arquivos |
| B. Prometheus + Grafana + Jaeger (padrão do kit) | Painéis RED e alertas | Impossível rodar na hospedagem compartilhada |
| C. Serviço externo de logs e métricas (Application Insights, Grafana Cloud, Seq na nuvem) | Painéis e alertas sem servidor próprio | Conta e custo novos; dados de uso fora da empresa; mais segredos a gerenciar |
| D. Log do `stdout` do ASP.NET Core Module | Nenhum pacote | Não indicado para produção; sem rotação; arquivos crescem sem limite |

## Decision
Adopt **Option A** because é a única opção sem custo que funciona no plano atual e atende integralmente à NFR-18; a ausência de métricas é aceitável na escala da v1 (NFR-23).

## Consequences
**Positive**: registro estruturado e pesquisável por `traceId`; nada sensível em log; nenhum serviço a mais.
**Negative**: sem painéis RED (taxa, erros, duração) nem alertas automáticos; indisponibilidade descoberta por quem usa ou por verificação externa.
**Risks**: falhas passarem despercebidas. Mitigação: verificação externa gratuita batendo em `/health/ready` a cada 5 minutos (ferramenta a escolher no `/infra`); revisão semanal dos logs de erro.

## v2 Upgrade Trigger
Revisit this decision when **any** of the following becomes true:
- Visitas acima de 5.000 por dia ou anúncios ativos acima de 1.000 (NFR-23).
- Mais de uma indisponibilidade por mês descoberta por usuários antes da equipe.
- A hospedagem sair do IIS compartilhado → adotar o padrão de `rules/monitoring.md` (OpenTelemetry + Prometheus + Grafana).

## Implementation Notes
- Pacotes: `Serilog.AspNetCore`, `Serilog.Sinks.File`, `Serilog.Formatting.Compact` (aprovação: AR-06).
- Arquivo: `<Logging__FileDirectory>/gazeta-.json`, rotação diária, `retainedFileCountLimit: 14`, formato JSON compacto; `Information` como mínimo, `Microsoft.AspNetCore` e `Microsoft.EntityFrameworkCore` em `Warning`.
- Correlação: middleware que lê ou cria `X-Correlation-ID`, devolve no cabeçalho da resposta e empurra `CorrelationId` para o `LogContext`; o `traceId` do ProblemDetails e o "código de referência" das telas de erro usam o mesmo valor.
- Mascaramento: nunca registrar senha, token, link de redefinição nem corpo de formulário; e-mails mascarados por um *enricher*/*destructuring policy*; objetos de domínio registrados só por id.
- Health checks: `/health/live` (processo, sem dependências) e `/health/ready` (banco acessível e última migration aplicada); respostas sem detalhes internos.
- Erros: exceções não tratadas em `Error`, com pilha só no log; erros de cliente (4xx) em `Warning`.
- Serviços em segundo plano (limpeza de originais, ADR-005) registram início, fim e totais.
