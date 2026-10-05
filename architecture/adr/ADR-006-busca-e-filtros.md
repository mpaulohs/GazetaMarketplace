# ADR-006: Busca por texto normalizado e filtros definidos pelo grupo de campos

**Date**: 2026-09-30
**Status**: Accepted

> **Em resumo:** a busca por texto compara o que o visitante digitou, sem acentos e em minúsculas, com cópias normalizadas do título e da descrição guardadas no próprio anúncio. Os filtros de categoria, UF, cidade e preço valem para todos; os filtros específicos (marca, ano, km, área) aparecem conforme o grupo de campos da categoria escolhida. Anúncios de Serviços, que não têm preço, ficam fora da faixa de preço e no fim das ordenações por preço (A6).

## Context
- US-002: busca por texto que "considera título e descrição e ignora diferença entre maiúsculas/minúsculas e acentos"; filtros combináveis; categoria principal inclui as subcategorias (até 3 níveis); ordenação por mais recentes, menor e maior preço; 24 por página; filtros no endereço da página.
- A6: Serviços não têm preço. A7 item (c): filtros específicos pelo grupo da categoria.
- NFR-04: p95 abaixo de 500 ms com ~200 anúncios ativos.
- A collation do banco do provedor não é conhecida (AR-03); o recurso de Full-Text Search pode não estar disponível em hospedagem compartilhada.

## Options Considered
| Option | Pros | Cons |
|--------|------|------|
| **A. `LIKE` sobre colunas normalizadas pela aplicação** | Independe da collation e de recursos do provedor; resultado idêntico em teste e produção; simples | `LIKE '%termo%'` não usa índice; custo cresce linearmente com o número de anúncios |
| B. Full-Text Search do SQL Server | Relevância e desempenho em volume grande | Disponibilidade incerta na hospedagem; configuração e testes mais complexos; exagero para 200 anúncios |
| C. Collation `…_CI_AI` na consulta | Sem colunas extras | Depende de `COLLATE` em toda consulta; mais difícil de testar em banco de teste; normalização diferente da aplicação |
| D. Elasticsearch/OpenSearch | Busca avançada | Serviço externo inviável na hospedagem; sem necessidade na escala |

## Decision
Adopt **Option A** because, com ~200 anúncios, uma varredura das colunas normalizadas fica bem abaixo dos 500 ms da NFR-04, e o resultado não depende da configuração do banco do provedor.

## Consequences
**Positive**: comportamento previsível e testável; a mesma função de normalização serve para a cidade (regra de padronização da US-008).
**Negative**: sem ordenação por relevância; custo linear conforme o volume cresce.
**Risks**: a normalização existir em dois lugares (aplicação e algum script SQL). Mitigação: a normalização é **só da aplicação**; nenhum script SQL recalcula essas colunas.

## v2 Upgrade Trigger
Revisit this decision when **any** of the following becomes true:
- Anúncios ativos passarem de 1.000 (NFR-23) ou a busca por texto passar de 500 ms no p95.
- O Product Owner pedir ordenação por relevância ou busca com sinônimos e erros de digitação.
- A hospedagem oferecer Full-Text Search confirmado.

## Implementation Notes
- **Normalização:** função única no Core — minúsculas, remoção de acentos (decomposição Unicode e retirada das marcas), espaços repetidos reduzidos; aplicada ao gravar o anúncio em `TitleSearch` e `DescriptionSearch` e ao termo digitado na busca.
- **Reimplementation flag (dual-implementation parity):** escolha **(1) chamar o próprio código da aplicação** — as colunas normalizadas só são preenchidas pelo C#, inclusive na carga inicial; nenhum *backfill* em SQL. Por isso não há teste diferencial.
- **Execução:** leitura em Dapper (`IBuscaReadRepository`), com o SQL montado pelo `SqlBuilder` do ADR-004 (fragmentos fixos, parâmetros nomeados, ordenação só por colunas permitidas); as regras abaixo valem para esse SQL.
- **Consulta:** só anúncios `Publicado`; texto: o termo vira até 5 palavras normalizadas e **cada palavra** precisa aparecer no título ou na descrição, em qualquer ordem (`CHARINDEX(@Word0, a.TitleSearch) > @Zero OR CHARINDEX(@Word0, a.DescriptionSearch) > @Zero`, uma condição por palavra, ligadas por `AND`; parâmetros, nunca concatenação). `CHARINDEX` em vez de `LIKE`: `%`, `_` e `[` do termo valem como texto comum, sem escape (o `SqlBuilder` não aceita aspas nem `ESCAPE '\'`). Termo de no máximo 100 caracteres (RC-15); categoria: conjunto de ids da categoria e de todos os descendentes, lido da árvore em cache; UF e cidade por igualdade; preço por faixa em `PriceCents`.
- **A6 (Serviços sem preço):** faixa de preço usada → só anúncios com `PriceCents IS NOT NULL`; ordenação por preço → anúncios sem preço vão para o fim (`ORDER BY CASE WHEN PriceCents IS NULL THEN 1 ELSE 0 END, PriceCents`), em "Menor preço" e em "Maior preço".
- **Filtros específicos (A7 c):** o grupo da categoria escolhida informa os filtros disponíveis (marca, modelo, ano, km para Carros e Motos; ano e km para Caminhões e Ônibus; área para Imóveis e Terrenos); os filtros usam as colunas calculadas do ADR-002.
- **Paginação:** 24 por página, ordem estável (desempate por `Id` decrescente); `COUNT` na mesma consulta filtrada; endereço com os filtros em *query string*, com parâmetros em português e a categoria **pelo slug** (decisão do Product Owner, 2026-10-05; as rotas públicas já usam slug): `/busca?q=civic&categoria=carros&uf=SP&cidade=Campinas&precoMin=&precoMax=&marca=&modelo=&anoDe=&anoAte=&kmMax=&areaMin=&areaMax=&ordem=menor-preco&pagina=2`. Parâmetro inválido (`pagina=abc`, `ordem=xyz`, número negativo, categoria que não existe) volta ao padrão, sem erro; os erros com mensagem junto do campo são só a faixa invertida (US-002-S08), o valor ilegível e o termo com mais de 100 caracteres, e nesses casos a lista sai **sem** o filtro com erro.
- **Filtros específicos (decisão 2026-10-05):** os campos com `Filter` no grupo da categoria escolhida (`FieldGroup.FilterableFieldsFor`) são a única fonte: marca, modelo, ano e quilometragem (Carros e Motos), ano e quilometragem (Caminhões e Ônibus), área (Imóveis). Categoria principal que mistura grupos, Autopeças e as demais não oferecem filtro específico. Um teste garante que todo campo filtrável do registro é um dos cinco que a busca sabe filtrar.
- **Ordenação:** só as três do enum (`recentes`, `menor-preco`, `maior-preco`), cada uma com texto fixo no `SqlBuilder`; nada do pedido entra no SQL.
