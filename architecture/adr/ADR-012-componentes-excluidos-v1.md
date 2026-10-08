# ADR-012: Componentes da pilha aprovada que não entram na v1

**Date**: 2026-09-30
**Status**: Accepted (a linha **CDN** foi superada em 2026-10-08 pelo ADR-013: o site passa a ficar atrás do Cloudflare)

> **Em resumo:** vários componentes aprovados em `rules/tech-stack.md` ficam de fora da v1, porque a escala é pequena (NFR-04, NFR-23) e a hospedagem compartilhada não comporta serviços auxiliares. Cada linha diz por que ficou de fora e o que faria a decisão ser revista.

## Context
Estes componentes de `rules/tech-stack.md` **não** são adotados na v1 — uma linha por componente; os gatilhos vêm de `rules/principles-and-practices.md` §5 e da NFR-23 do SPEC.

| Componente | Por que fica de fora na v1 | v2 Upgrade Trigger |
|---|---|---|
| Redis (cache distribuído) | Um único processo; cache de memória nos serviços de leitura e cache de CEP no banco bastam; a hospedagem não oferece Redis | Visitas acima de 5.000/dia ou anúncios ativos acima de 1.000 (NFR-23), ou p95 acima de 500 ms com CPU do banco acima de 70% |
| Kafka (fila de mensagens) | Nenhuma integração assíncrona nem segundo consumidor | Surgir um segundo serviço que precise reagir a eventos do anúncio, ou envio de e-mails em volume |
| Hangfire (agendador de tarefas) | Só há uma tarefa agendada leve (limpeza de originais, ADR-005), resolvida com `BackgroundService` | Uma tarefa precisar de horário garantido, reexecução com histórico ou durar mais de 30 s; ou a hospedagem permitir o site sempre ativo |
| YARP (API gateway) | Um único site; nenhuma API para rotear | Surgir um segundo serviço de backend |
| Keycloak (identidade) | Só a equipe tem login; Identity atende (ADR-003); inviável na hospedagem | Contas para o público ou login único com outros sistemas da Gazeta |
| Elasticsearch / OpenSearch | Busca por texto normalizado atende à escala (ADR-006) | Busca acima de 500 ms no p95 ou pedido de relevância e tolerância a erros |
| Prometheus + Grafana | Não rodam na hospedagem compartilhada (ADR-010) | Mudança de hospedagem ou os gatilhos do ADR-010 |
| OpenTelemetry + Jaeger (rastreamento) | Um único processo; o log com correlação basta (ADR-010) | Mais de um serviço participando de uma mesma requisição |
| Réplica de leitura do banco | Leitura e escrita pequenas | Leitura/escrita acima de 4:1 **e** CPU do banco primário acima de 70% |
| CDN (imagens e arquivos estáticos) | Fotos em 480 px já cumprem a NFR-05 | LCP p75 acima de 2,5 s por 7 dias seguidos mesmo com imagens reduzidas (NFR-23) |
| Polly (resiliência) | Só uma integração com nova tentativa (CEP), comandada pela tela (ADR-007); o SendGrid falha com mensagem neutra | Duas ou mais integrações externas precisarem de nova tentativa ou disjuntor (*circuit breaker*) |
| Docker em produção | Hospedagem IIS compartilhada; Docker só no desenvolvimento e nos testes (TestContainers) | Mudança para hospedagem com contêineres |
| Azure Blob / S3 | Pasta persistente fora da raiz do site atende (ADR-005) | Os gatilhos do ADR-005 (espaço, LCP, mudança de hospedagem) |
