# ADR-007: Consulta de CEP no servidor, com cache no banco e nova tentativa comandada pela tela

**Date**: 2026-09-30
**Status**: Accepted

> **Em resumo:** quando alguém da equipe digita o CEP, a tela pede ao nosso servidor, que consulta o ViaCEP (até 5 s) e guarda o resultado por 30 dias numa tabela do banco. Se o ViaCEP não responder, a tela tenta mais uma vez; se falhar de novo, abre o preenchimento manual de UF e cidade, com o selo de conferência. "CEP não encontrado" não é falha e não abre o preenchimento manual.

## Context
- US-008-S01 e US-008-S14 (CEP preenche cidade e UF; serviço fora do ar leva ao preenchimento manual com selo); S23 e S24 (CEP incompleto e CEP inexistente); regra de padronização do nome da cidade (US-008).
- NFR-24: tempo limite de 5 s por tentativa, 1 nova tentativa só em falha do serviço, cache no servidor (validade sugerida de 30 dias), consulta feita pelo servidor.
- O wireframe mostra "Buscando… (tentativa 2 de 2)" (`specs/wireframes/screens/US-008-editar-anuncio.md`): a tela precisa saber em que tentativa está.
- Hospedagem compartilhada recicla o processo com frequência: cache em memória não dura 30 dias (decisão aprovada em 2026-09-30).

## Options Considered
| Option | Pros | Cons |
|--------|------|------|
| **A. Endpoint próprio → ViaCEP (1 tentativa de 5 s por chamada), cache em tabela, nova tentativa comandada pela tela** | Atende a NFR-24; a tela mostra a tentativa atual; cache sobrevive à reciclagem; nenhuma dependência nova | Duas idas ao servidor no pior caso |
| B. Endpoint próprio com as 2 tentativas dentro do servidor | Uma chamada só da tela | A tela não sabe em que tentativa está (não mostra "tentativa 2 de 2"); chamada pode durar 10 s |
| C. Navegador consulta o ViaCEP direto | Sem carga no servidor | Contraria a NFR-24; exige abrir a CSP para outro domínio; sem cache compartilhado; padronização da cidade fica no cliente |
| D. Base de CEPs local (DNE dos Correios) | Sem dependência externa | Licença paga, atualização mensal, centenas de MB — desproporcional |

## Decision
Adopt **Option A** because cumpre a NFR-24 e a US-008-S14 exatamente como desenhadas (inclusive o texto da tentativa), mantém a consulta no servidor e o cache persiste mesmo com o IIS reciclando o processo.

## Consequences
**Positive**: CEPs repetidos respondem do banco, sem depender do ViaCEP; padronização da cidade pela lista oficial num só lugar; comportamento testável com um `ICepLookup` falso.
**Negative**: a tela orquestra a nova tentativa (mais lógica no módulo JavaScript da página).
**Risks**: o ViaCEP mudar o formato da resposta ou ficar fora por muito tempo. Mitigação: o preenchimento manual sempre funciona; o formato fica isolado em `ViaCepLookup`.

## v2 Upgrade Trigger
Revisit this decision when **any** of the following becomes true:
- O ViaCEP falhar em mais de 5% das consultas num mês → avaliar um segundo provedor como alternativa (por exemplo, BrasilAPI) atrás do mesmo `ICepLookup`.
- O visitante passar a informar CEP (por exemplo, busca por proximidade), aumentando muito o volume de consultas.

## Implementation Notes
- **Endpoint:** `GET /api/v1/cep/{cep}` — só para a equipe logada (evita que o site vire proxy público de CEP) e com o limite de requisições do painel. `cep` validado como 8 dígitos antes de qualquer consulta (S23: incompleto não consulta).
- **Respostas:** `200` com `{ cep, city, uf, source: "cache" | "viacep" }`; `404 NOT_FOUND` quando o ViaCEP responde `"erro": true` (S24 — não é falha, não abre o manual); `503 CEP_SERVICE_UNAVAILABLE` quando não há resposta em 5 s, há erro 5xx ou falha de rede.
- **Tela:** chama o endpoint; em `503`, mostra "Buscando… (tentativa 2 de 2)" e chama de novo uma vez; no segundo `503`, abre UF e cidade em listas (US-008-S14) e marca `LocationManual`.
- **Cliente HTTP:** `HttpClient` tipado com `BaseAddress = https://viacep.com.br/ws/`, rota fixa `{cep}/json/`, `Timeout` de 5 s por chamada; sem Polly (ADR-012).
- **Cache:** tabela `CepCache (Cep PK, City, Uf, IbgeCode, FetchedAt)`; entrada válida por 30 dias; só respostas encontradas entram no cache (CEP inexistente não é guardado, para não esconder um CEP criado depois).
- **Padronização da cidade:** o nome devolvido é conferido com a tabela `Cities` (lista de municípios do IBGE, carregada por script) pelo código IBGE; no preenchimento manual, a cidade é escolhida da lista da UF; sem lista, vale a regra de padronização da US-008 (comparação sem acentos, gravação mantendo os acentos).
- **Log:** falhas do ViaCEP em `Warning`, com o CEP e o `traceId`; o CEP não é dado pessoal sensível na v1 (não identifica endereço completo).
