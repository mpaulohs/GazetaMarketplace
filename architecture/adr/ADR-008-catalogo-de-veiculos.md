# ADR-008: Catálogo de veículos importado uma vez do GazetaOnline, com origem registrada

**Date**: 2026-09-30
**Status**: Accepted

> **Em resumo:** as listas encadeadas de marca → modelo → ano → versão (carros e motos) vêm do banco do GazetaOnline por uma exportação única, feita fora do site, que gera um script de carga. Cada registro guarda a origem dos dados, para que a fonte possa ser trocada (por exemplo, pela tabela FIPE) se o parecer jurídico da A5 pedir. O site nunca se conecta ao banco do GazetaOnline.

## Context
- Decisão do Product Owner (Q4 da descoberta): usar o catálogo do GazetaOnline para Carros e Motos (SPEC, Apêndice B).
- A3: como trazer o catálogo e mantê-lo. A5: o catálogo foi coletado da API interna da OLX; parecer jurídico pendente, **bloqueia o lançamento**.
- O catálogo mora em tabelas do banco do GazetaOnline, preenchidas pelo projeto `GazetaOnline.Import` (`specs/discovery/gazetaonline-lookups.md`).
- Achado crítico da descoberta: o `GazetaOnline.Import/Program.cs` contém a credencial do banco de produção em texto puro. Ela não pode ser reutilizada.

## Options Considered
| Option | Pros | Cons |
|--------|------|------|
| **A. Exportação única → script de carga versionado, coluna `Source`** | Nenhuma dependência em tempo de execução; carga repetível em qualquer ambiente (dev, teste, produção); fonte trocável | O catálogo não se atualiza sozinho; nova carga para novos modelos |
| B. Site lendo o banco do GazetaOnline em tempo real | Sempre atualizado | Acoplamento a outro sistema e a uma credencial de produção; indisponibilidade de um derruba o outro |
| C. Marca, modelo e versão em texto livre | Sem risco jurídico; nada a importar | Contraria a decisão do Product Owner; filtros de marca e modelo imprecisos |
| D. Tabela FIPE (fonte pública) desde já | Menor risco jurídico | Não é a decisão do Product Owner; API oficial exige acordo e as não oficiais podem mudar |

## Decision
Adopt **Option A** because realiza a decisão do Product Owner sem criar dependência de outro sistema e deixa a fonte substituível — a resposta técnica à A5, cuja decisão continua jurídica.

## Consequences
**Positive**: carga idêntica em todos os ambientes; testes com um catálogo reduzido; troca de fonte = nova carga com outra `Source`, sem mudar código de tela nem regra.
**Negative**: modelos lançados depois da carga não aparecem até uma nova exportação.
**Risks**:
- Parecer jurídico negativo (A5). Mitigação: fonte substituível; em último caso, Opção C como alternativa, com emenda do SPEC.
- Vazamento da credencial do GazetaOnline. Mitigação: a exportação usa uma conta **somente leitura**, informada por variável de ambiente na máquina de quem exporta; a senha antiga do banco deve ser trocada (ação do responsável pelo GazetaOnline, já registrada na descoberta).

## v2 Upgrade Trigger
Revisit this decision when **any** of the following becomes true:
- O parecer da A5 exigir outra fonte.
- A equipe precisar de modelos novos mais de uma vez por trimestre → rotina de atualização periódica.
- O catálogo passar a ser usado por outras categorias (caminhões, ônibus, barcos).

## Implementation Notes
- **Tabelas** (`ARCHITECTURE.md` §6.2): `VehicleBrands`, `VehicleModels`, `VehicleModelYears`, `VehicleVersions`, cada uma com `Id`, nome, chave do pai, `Kind` (`car` | `moto`) e `Source` (texto curto, por exemplo `gazetaonline-2026-09`).
- **Ferramenta de exportação:** pequeno console .NET em `tools/VehicleCatalogExport/`, **fora da solução do site**; lê a conexão de uma variável de ambiente; gera `db/seed/vehicle-catalog.sql` com `INSERT` idempotentes (`MERGE` ou `IF NOT EXISTS`). O script gerado é revisado e versionado no git. A ferramenta lê as tabelas de origem com Dapper (junções de 4 níveis) e, **só em bancos de desenvolvimento e de teste**, pode carregar o catálogo direto em lote (Dapper, com o comentário de justificativa do ADR-004); para o banco do provedor o caminho continua sendo o script.
- **Carga:** aplicada como as migrations — pelo script, na ferramenta de SQL do provedor.
- **Uso no site:** `IVehicleCatalog` (Core) com as consultas encadeadas; lidas do banco e mantidas em cache de memória por 10 minutos; endpoint `GET /api/v1/vehicle-catalog/brands?kind=car`, `…/brands/{id}/models`, `…/models/{id}/years`, `…/years/{id}/versions` para o formulário.
- **Anúncio:** o JSON de atributos guarda os ids (`brandId`, `modelId`, `modelYear`, `versionId`); as colunas calculadas de filtro (ADR-002) usam `brandId`, `modelId` e `modelYear`.
- **Reimplementation flag:** não se aplica — o catálogo tem uma só representação (as tabelas carregadas).

## Amendment 2026-10-03 (tarefa 2.5)
- **Chave composta com o tipo.** Carros e motos vêm de tabelas separadas do GazetaOnline, cada uma numerando a partir de 1. Toda tabela do catálogo tem `Kind` na chave primária e nas chaves estrangeiras: `VehicleBrands (Id, Kind)`, `VehicleModels (Id, Kind)`, `VehicleModelYears (ModelId, Year, Kind)`, `VehicleVersions (Id, Kind)`. O esquema da origem nunca foi lido, então não se sabe se os ids são globalmente únicos; a chave composta vale nos dois casos.
- **Contrato:** `GET …/brands/{id}/models`, `…/models/{id}/years` e `…/models/{id}/years/{year}/versions` exigem `kind=car|moto`. Sem ele o id não identifica o item.
- **Ano em tabela própria**, para existirem anos sem versões.
- **Carga em lote:** executa os mesmos `MERGE` do script numa transação (uma só implementação da regra); recusa ambiente que não seja Development ou Testing e cadeia de conexão com "prod".
- **Esquema da origem presumido:** ver `plans/BACKLOG.md`; só `OriginReader.cs` conhece a origem.
