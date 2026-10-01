# ADR-002: Campos por categoria em JSON, com colunas calculadas e grupos de campos em código

**Date**: 2026-09-30
**Status**: Accepted

> **Em resumo:** os campos que mudam de categoria para categoria (marca, quartos, condição, área de vaga…) ficam numa coluna JSON do anúncio. Os poucos usados em filtro viram colunas calculadas e indexadas. Quais campos cada categoria tem é definido em código, em 18 grupos de campos, e cada categoria usa o grupo próprio ou o do ancestral mais próximo (A7).

## Context
- 124 categorias ativas em até 3 níveis (US-013-S11), com 18 grupos de campos (SPEC, Apêndice B): carros, motos, imóveis, serviços, vagas, produtos em geral etc.
- A busca filtra por marca, modelo, ano, quilometragem (veículos) e área (terrenos e imóveis) (US-002-S03, S04; A7 item c).
- O Administrador cria e renomeia categorias pela tela (US-013), mas não define campos: uma categoria nova herda o grupo do pai (A7 item a).
- O formulário, a validação para enviar à revisão e a página de detalhe mudam conforme o grupo (US-008-S09, US-009-S03, US-003).

## Options Considered
| Option | Pros | Cons |
|--------|------|------|
| **A. Coluna JSON + colunas calculadas persistidas para filtros + grupos em código** | Nenhuma mudança de esquema por categoria; filtros indexados; leitura de um anúncio em uma linha; grupos versionados junto com as regras | Os caminhos do JSON existem em dois lugares (código e coluna calculada); validação do JSON é da aplicação |
| B. EAV (tabela de atributos por anúncio, como no GazetaOnline) | Flexível; já conhecido pela equipe | Filtros viram *joins* e *pivots* caros; tipagem fraca; consultas difíceis de manter |
| C. Uma tabela por grupo de campos | Tipagem forte no banco | 18 tabelas e migrations a cada mudança de campo; consultas da vitrine precisam de uniões |
| D. Grupos e campos cadastrados no banco pelo Administrador | Máxima flexibilidade | Fora do escopo do SPEC; a tela de campos não existe; validação e formulário genéricos demais |

## Decision
Adopt **Option A** because atende aos filtros da US-002 com índices comuns, evita mudança de esquema por categoria e mantém as regras de cada grupo (campos, obrigatórios, limites) testáveis no Core.

## Consequences
**Positive**: incluir um campo novo num grupo não exige migration, a não ser que ele vire filtro; o detalhe e o formulário são gerados a partir da definição do grupo; os limites por categoria (título 90 em Vagas, 6 fotos em Serviços, sem fotos em Vagas, sem preço em Serviços) moram num só lugar.
**Negative**: campos fora das colunas calculadas não podem ser filtrados de forma eficiente; relatórios sobre atributos exigem `JSON_VALUE`.
**Risks**: o caminho de um campo mudar no código e a coluna calculada continuar lendo o caminho antigo. Mitigação: teste diferencial (ver Implementation Notes).

## v2 Upgrade Trigger
Revisit this decision when **any** of the following becomes true:
- O Product Owner pedir filtros por mais de 3 campos novos que não sejam colunas calculadas.
- O Administrador precisar criar ou editar campos pela tela (hoje fora do SPEC).
- Consultas de busca com filtro de atributo passarem de 500 ms no p95 (NFR-04) com os índices existentes.

## Implementation Notes
- **Grupos em código:** tipo `FieldGroup` no Core com a lista de campos (nome, tipo, obrigatório, lista de opções, limites) e as regras do grupo (`HasPrice`, `MaxPhotos`, `TitleMaxLength`, `DescriptionMaxLength`, `DescriptionLabel`, filtros de busca). Os 18 grupos seguem o Apêndice B; as listas de opções herdadas usam os mesmos ids de `specs/discovery/gazetaonline-lookups.md`.
- **Herança (A7 a):** `Categories.FieldGroup` nulo = herda do ancestral mais próximo; a resolução usa a árvore em cache.
- **Exclusão (A7 b):** bloqueada para `IsSystem = 1` com `FieldGroup` próprio; as regras "sem subcategorias" e "sem anúncios" continuam para todas.
- **Filtros (A7 c):** o formulário de busca pede ao grupo da categoria escolhida quais filtros mostrar.
- **JSON:** `Ads.Attributes nvarchar(max)` com `CHECK (ISJSON(Attributes) = 1)`; serialização pelo `System.Text.Json` com nomes de campo estáveis em inglês (`brandId`, `modelId`, `modelYear`, `km`, `areaM2`…); mapeamento pelo EF Core 10 como tipo complexo em JSON ou conversão explícita (decisão do `/build`).
- **Colunas calculadas persistidas:** `VehicleBrandId`, `VehicleModelId`, `ModelYear`, `Km`, `AreaM2` = `CAST(JSON_VALUE(Attributes, '$.<campo>') AS int)`, indexadas (`ARCHITECTURE.md` §6.4). Compatíveis com SQL Server 2016 ou mais novo (AR-03).
- **Reimplementation flag (dual-implementation parity):** os caminhos JSON dos campos filtráveis existem em **duas representações** — a definição do grupo no C# e a expressão da coluna calculada no SQL. Escolha: **(2) reimplementar com teste diferencial** (a coluna calculada é necessária para indexar). O teste grava, pela aplicação, um anúncio de cada grupo com filtro (Carros, Motos, Caminhões, Ônibus, Imóveis, Terrenos) com valores de todas as classes de entrada (campo presente, ausente, zero, limite máximo) e confere que cada coluna calculada devolve exatamente o valor que o C# gravou (`rules/testing.md` §Dual-Implementation Parity). Roda no `/test` com SQL Server em TestContainers.
