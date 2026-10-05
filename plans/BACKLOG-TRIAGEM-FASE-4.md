# Triagem do BACKLOG ao fechar a Fase 4 (Checkpoint 4)

> **Em resumo:** esta lista só **classifica** os itens abertos que nasceram na Fase 4 (tarefas 4.1 a 4.4, o Checkpoint 4 e o `/review`); nenhum código mudou. Os itens da Fase 3 continuam em `plans/BACKLOG-TRIAGEM-FASE-3.md`. Os 6 avisos 🟡 do `/review` estão no grupo "antes do lançamento" porque o Gate 7 pede que sejam corrigidos ou aceitos antes do `/scan`; quem decide é o Product Owner.

## 1. Antes do lançamento (bloqueadores)

| Item | Por que bloqueia |
|---|---|
| `/review` 🟡 1: `pagina=` muito grande derruba a lista em 503 | Qualquer membro da equipe provoca o erro digitando o endereço; correção de uma linha e um teste |
| `/review` 🟡 4: cobertura numérica (80% linha / 75% ramo) e métodos de regra com 0% nunca medidos | O Gate 6 exige os dois números; depende de adotar o pacote de cobertura do Microsoft Testing Platform |
| `/review` 🟡 6: `catch` vazio em `AdSpecsReader` | Contraria `error-handling.md`; uma carga trocada do catálogo ficaria invisível |
| `/review` 🟡 2: sem teste determinístico dos ramos de repetição por conflito de `RowVersion` | A única prova hoje é a integração sem barreira; remover a repetição poderia passar |
| `/review` 🟡 5: pré-visualização usa o texto do rótulo como chave (`Área`, `Tipo`) | Renomear o rótulo some com o bloco "Vaga de emprego"; a 5.2 copiaria a lógica |
| Resolver os mesmos itens da Fase 3 que ainda bloqueiam o lançamento (carga do IBGE, catálogo de veículos, AR-05, `RuntimeIdentifier`, runbook, banco) | Ver `BACKLOG-TRIAGEM-FASE-3.md` |

## 2. Vai para a Fase 5 / 6 (ou para uma tarefa técnica já prevista)

| Item | Para onde |
|---|---|
| S03/S04 da US-010 e S01/S02 da US-011: "mais recentes", "na busca" e "endereço antigo" | Testes das tarefas 5.1, 5.2 e 5.4 |
| US-011-S04: anúncio arquivado some dos favoritos | Tarefa 5.5 |
| Galeria de fotos, bloco "Vaga de emprego" e selo manual no detalhe público | Tarefa 5.2 (reaproveita `_AdBody`, `AdSpecs`) |
| `/review` 🟢 12, 13, 16: CSS de lista repetido, `SubmitAdAsync` copiado em 5 arquivos E2E, cartão de confirmação repetido | Antes da 5.2 (a página pública reaproveita o CSS) e do próximo conjunto de E2E |
| `/review` 🟢 8, 9, 10, 11: `AdsController` grande, `Preview` longo, pendências duplicadas, regra de retirada em três formas | Refatoração antes da 5.2, com os testes atuais como rede |
| `/review` 🟢 7: `ConflictException` e `ForbiddenException` sem tratamento nas ações de decisão | Junto com o 🟡 2 |
| `/review` 🟢 14, 15, 17, 18, 19: `!`, limite "500" à mão, comentários, `AuthorId` sem uso, `Kind` da data do Dapper | Tarefa 6.x de acabamento |
| `/review` 🟢 21, 22, 23: NVDA nas tabelas empilhadas, regra de nomes em CSS/JS, axe nas telas antigas | Tarefa 6.1 e `/verify` |
| `/review` 🟢 25 e BACKLOG 4.1 e 4.4: fila sem paginação, `COALESCE` no `ORDER BY`, busca sem índice | `/verify`, com volume real e gatilhos numéricos |
| Fila de espera do E2E: conta de Redator e de Administrador por rodada, `CepCache`, instabilidade de montagem | Documentar em `docs/RODAR-TESTES-DE-INTEGRACAO-E-E2E.md` na tarefa 6.x |
| Estado "Carregando" (esqueleto) e `alertdialog` de confirmação | Melhoria progressiva, se a lista passar a carregar por JavaScript |

## 3. Decisão do Product Owner (não técnica)

> **Respondido em 2026-10-05 (depois do Checkpoint 4):** reconferência de pendências **mantida** · `ArchivedById` **adicionado** (migration `AddArchivedBy`) · aviso ao autor na despublicação **fica no BACKLOG** · "Alterado em" = `UpdatedAt` **confirmado** · cache de 1 ano da foto retirada **risco aceito** · cor `#b02a37` **mantida**. As seis perguntas abaixo ficam como registro.

| Item | Pergunta |
|---|---|
| Reconferência de pendências ao publicar ("Faltam N itens…") | O texto e a proteção (a SPEC só pede o telefone do site) valem? |
| "Arquivar" não grava quem arquivou no anúncio (só `ArchivedAt`; o ator fica na auditoria) | Quer `ArchivedById` na tabela? |
| Despublicar não avisa o autor | O autor deve receber aviso de que o anúncio saiu do ar? |
| "Alterado em" muda em toda passagem de situação | Serve como "data da última alteração" da SPEC? |
| Cache público de 1 ano da foto despublicada ou arquivada em quem já a baixou | Compensa um prazo menor para fotos publicadas, ao custo de mais requisições? |
| Contraste global de `btn-outline-danger` e `btn-outline-secondary` mais escuro | Aprova a cor nas telas antigas? |
