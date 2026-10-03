# Carga do catálogo de veículos

> **Em resumo:** esta pasta guarda os scripts de carga do catálogo de marcas, modelos, anos e versões de carros e motos (ADR-008). Os scripts são **gerados** pela ferramenta `tools/VehicleCatalogExport`; não se edita à mão.

| Arquivo | O que é | Pode ir para produção? |
|---|---|---|
| `sample/vehicle-catalog-sample.sql` | Catálogo **reduzido de teste** (10 marcas, 45 modelos, 214 anos, 307 versões; `Source = 'sample'`), gerado a partir da origem simulada `tests/VehicleCatalogExport.Tests/Data/sample-catalog.sql`. Os nomes são genéricos; nada foi copiado do GazetaOnline. | **Não.** Serve para desenvolvimento, testes e para provar a ferramenta. |
| `vehicle-catalog.sql` | O catálogo real exportado do GazetaOnline. **Ainda não existe**: depende do parecer jurídico (A5) e do acesso somente leitura ao banco do GazetaOnline. | Sim, depois de revisado e versionado. |

## Gerar o script (quem exporta, na própria máquina)

```bash
export VEHICLE_CATALOG_ORIGIN_CONNECTION='<cadeia de uma conta SOMENTE LEITURA do GazetaOnline>'
dotnet run --project tools/VehicleCatalogExport -- export \
  --source gazetaonline-2026-10 --out db/seed/vehicle-catalog.sql --report reports/vehicle-catalog-orphans.txt
```

- Sem a variável, ou com a origem inacessível, a ferramenta **para sem criar arquivo** (códigos de saída 2 e 3).
- Registros sem pai (modelo sem marca, ano sem modelo, versão sem ano) são **descartados e listados** no relatório; decida o que fazer com eles antes de aplicar.
- Nenhuma credencial entra no repositório: a cadeia vem só da variável de ambiente.

## Aplicar o script

Com `sqlcmd -I` ou na ferramenta de SQL do provedor, **depois** de aplicar as migrations. É idempotente (`MERGE`): pode ser aplicado de novo sem duplicar nada, e uma nova carga com outro `--source` atualiza a origem das linhas.

## Carga em lote (só desenvolvimento e teste)

```bash
export VEHICLE_CATALOG_TARGET_CONNECTION='<banco de desenvolvimento ou de teste>'
dotnet run --project tools/VehicleCatalogExport -- load --source sample --environment Development
```

A ferramenta **recusa** ambiente que não seja `Development` ou `Testing` e cadeia de conexão com "prod" no servidor, no banco ou no nome da aplicação. Executa os mesmos `MERGE` do script, numa transação.

## Esquema da origem (presumido)

O banco do GazetaOnline nunca foi lido. A ferramenta presume `CarBrands(Id, Name)`, `CarModels(Id, BrandId, Name)`, `CarYearModels(Id, ModelId, Year)`, `CarVersions(Id, YearModelId, Name)` e as quatro equivalentes de motos (`Motorcycle…`). Quando houver acesso, ajuste as consultas em `tools/VehicleCatalogExport/OriginReader.cs`, o único lugar que conhece a origem.
