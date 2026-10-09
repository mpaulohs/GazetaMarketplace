# Carga do catálogo de veículos

> **Em resumo:** esta pasta guarda os scripts de carga do catálogo de marcas, modelos, anos e versões de carros e motos (ADR-008). Os scripts são **gerados** pela ferramenta `tools/VehicleCatalogExport`; não se edita à mão.

| Arquivo | O que é | Pode ir para produção? |
|---|---|---|
| `sample/vehicle-catalog-sample.sql` | Catálogo **reduzido de teste** (10 marcas, 45 modelos, 214 anos, 307 versões; `Source = 'sample'`), gerado a partir da origem simulada `tests/VehicleCatalogExport.Tests/Data/sample-catalog.sql`. Os nomes são genéricos; nada foi copiado do GazetaOnline. | **Não.** Serve para desenvolvimento, testes e para provar a ferramenta. |
| `vehicle-catalog.sql` | O catálogo real exportado do GazetaOnline. **Não fica no repositório**: foi gerado e aplicado em produção em 2026-10-09 (`--source gazetaonline-2026-10`: 277 marcas, 2154 modelos, 29031 anos, 71280 versões). Para atualizar, quem tem acesso ao banco do GazetaOnline gera de novo na própria máquina (próxima seção), confere o relatório e aplica. A origem (`--source`) registra de onde veio cada linha. | Sim, depois de revisado. |

## Passo a passo: do GazetaOnline para o GazetaMarketplace

> **Em resumo:** a ferramenta lê `CarBrands`, `CarModels`, `CarYearModels`, `CarVersions` e as quatro de `Motorcycle…` do banco do GazetaOnline e escreve um arquivo `.sql` com o catálogo inteiro. Você confere o relatório, faz backup do banco do GazetaMarketplace e aplica o arquivo nas tabelas `VehicleBrands`, `VehicleModels`, `VehicleModelYears` e `VehicleVersions`. A ferramenta **nunca grava** no GazetaOnline nem no GazetaMarketplace; quem aplica é você.

**1. Gerar o arquivo (no Windows, na sua máquina).** Use uma conta **somente leitura** do GazetaOnline, e coloque a cadeia de conexão **só na variável de ambiente** (nunca em arquivo nem no histórico compartilhado):

```powershell
$env:VEHICLE_CATALOG_ORIGIN_CONNECTION = "<cadeia do GazetaOnline, conta somente leitura>"
dotnet run --project tools/VehicleCatalogExport -- export `
  --source gazetaonline-2026-10 --out db/seed/vehicle-catalog.sql --report reports/vehicle-catalog-orphans.txt
Remove-Item Env:VEHICLE_CATALOG_ORIGIN_CONNECTION
```

A saída traz as contagens (`Marcas: N | Modelos: N | Anos: N | Versões: N | Descartados: N`). Cada tipo (`car` e `moto`) tem os seus ids, que se repetem entre os dois; no GazetaMarketplace a chave é o par (id, tipo).

**2. Conferir o relatório** (`reports/vehicle-catalog-orphans.txt`). O catálogo **inteiro** é lido, inclusive o que nenhum anúncio usa: o `IsPublished` do GazetaOnline só diz que algum anúncio publicado usa o item e **não** é usado como filtro. Ficam de fora, com o motivo no relatório:

| Motivo no relatório | O que significa |
|---|---|
| `sem nome` | nome vazio ou só espaços |
| `nome longo demais` | acima de 150 letras (marca, modelo) ou 250 (versão), o limite da coluna; nada é cortado em silêncio |
| `id inválido` | id zero ou negativo |
| `sem marca` / `sem modelo` / `sem ano` | o pai não existe (ou foi descartado, e os filhos vão junto) |
| `ano inválido` | o ano da origem é texto que não vira número (por exemplo "Zero km") |
| `ano fora de 1950 a 2100` | a API do site só aceita esse intervalo |
| `ano repetido no modelo` / `id repetido` | a segunda ocorrência é descartada |
| `texto corrompido` | o nome tem acento já corrompido na origem (por exemplo `AutomÃ¡tico`); corrija na origem e exporte de novo |

**3. Fazer o backup** do banco do GazetaMarketplace e aplicar, **depois** das migrations:

```powershell
sqlcmd -S <servidor> -d <banco> -U <usuario> -b -I -f 65001 -i db/seed/vehicle-catalog.sql
```

> **O `-f 65001` é obrigatório.** Sem ele, o `sqlcmd` do Windows lê o arquivo na página de código do sistema e grava os acentos corrompidos (`ImÃ³veis` em vez de `Imóveis`). Já aconteceu em produção em 2026-10-09; o reparo é o `db/scripts/reparar-acentos-categorias.sql`. Como rede de segurança, o fim do script procura acentos corrompidos nas linhas desta carga e, se achar, **desfaz a carga inteira** com a mensagem "Acentos corrompidos".

**4. Conferir** no banco: `SELECT Kind, COUNT(*) FROM VehicleBrands GROUP BY Kind;` (e o mesmo nas outras três tabelas) tem de bater com as contagens da saída do passo 1. O site guarda as listas em cache por 10 minutos: espere esse tempo ou reinicie o site para os campos Marca, Modelo, Ano e Versão aparecerem.

O script é idempotente (`MERGE`): pode ser aplicado de novo sem duplicar nada, e uma nova carga com outro `--source` atualiza a origem das linhas.

- Sem a variável, ou com a origem inacessível, a ferramenta **para sem criar arquivo** (códigos de saída 2 e 3).
- A cadeia de conexão nunca vai para o repositório nem para o chat. Se ela já apareceu em algum lugar, troque a senha.

## Carga em lote (só desenvolvimento e teste)

```bash
export VEHICLE_CATALOG_TARGET_CONNECTION='<banco de desenvolvimento ou de teste>'
dotnet run --project tools/VehicleCatalogExport -- load --source sample --environment Development
```

A ferramenta **recusa** ambiente que não seja `Development` ou `Testing` e cadeia de conexão com "prod" no servidor, no banco ou no nome da aplicação. Executa os mesmos `MERGE` do script, numa transação.

## Esquema da origem

Lido do código do GazetaOnline (entidades `CarBrand`, `CarModel`, `CarYearModel`, `CarVersion` e as de moto): `CarBrands(CarBrandId, Name)`, `CarModels(CarModelId, CarBrandId, Name)`, `CarYearModels(CarYearModelId, CarModelId, Year)` com `Year` em **texto**, `CarVersions(CarVersionId, CarYearModelId, Name)` e as quatro equivalentes `Motorcycle…`. Se o banco de lá mudar, ajuste as consultas em `tools/VehicleCatalogExport/OriginReader.cs`, o único lugar que conhece a origem.

---

# Carga dos municípios do IBGE

> **Em resumo:** a tabela `Cities` (lista oficial de municípios) é carregada por um script **gerado** pela ferramenta `tools/CitiesImport` a partir do JSON oficial do IBGE. Enquanto a carga real não é feita, uma UF sem municípios mostra um campo de texto e vale a padronização do nome da SPEC.

| Arquivo | O que é | Pode ir para produção? |
|---|---|---|
| `sample/cities-sample.json` | **Amostra** de 40 municípios no formato do IBGE (os códigos foram escritos de memória e precisam ser conferidos). | **Não.** |
| `sample/cities-sample.sql` | O script gerado da amostra (`-- AMOSTRA DE TESTE` no cabeçalho). Um teste confere que ele é igual ao que a ferramenta gera do JSON. | **Não.** Uma UF com poucas cidades daria uma lista de preenchimento manual incompleta. |
| `cities.sql` | O script da carga real. **Não fica no repositório**: foi gerado e aplicado em produção em 2026-10-09 (`--source ibge-2026-10`, 5571 municípios). Para atualizar, gere de novo com o passo abaixo. | Sim, depois de revisado. |

## Gerar o script da carga real

```bash
curl -o /tmp/municipios.json https://servicodados.ibge.gov.br/api/v1/localidades/municipios
dotnet run --project tools/CitiesImport -- export --input /tmp/municipios.json --source ibge-2026-10 --out db/seed/cities.sql
```

- A ferramenta **recusa o arquivo inteiro** (código de saída 4, nenhum arquivo criado) se houver município com código fora de 7 dígitos, UF desconhecida, código que não é da UF informada, nome vazio ou longo demais, código repetido ou o mesmo nome duas vezes na mesma UF.
- `NameSearch` é calculado pelo mesmo `Normalizer` do site (a ferramenta referencia o Core): não existe uma segunda implementação em SQL.

## Aplicar o script

> **O `-f 65001` é obrigatório.** Sem ele, o `sqlcmd` do Windows lê o arquivo na página de código do sistema e grava os acentos corrompidos (`ImÃ³veis` em vez de `Imóveis`). Já aconteceu em produção em 2026-10-09; o reparo é o `db/scripts/reparar-acentos-categorias.sql`.

Com `sqlcmd -I -f 65001` ou na ferramenta de SQL do provedor, **depois** das migrations. É idempotente (`MERGE`): aplicar de novo não duplica nada e um nome corrigido pelo IBGE é atualizado. O `MERGE` **nunca apaga**: município extinto continua na tabela até ser removido à mão.

## Carga em lote (só desenvolvimento e teste)

```bash
export CITIES_IMPORT_TARGET_CONNECTION='<banco de desenvolvimento ou de teste>'
dotnet run --project tools/CitiesImport -- load --input db/seed/sample/cities-sample.json --source sample --environment Development
```

Recusa ambiente que não seja `Development` ou `Testing` e cadeia com "prod" no servidor, no banco ou no nome da aplicação (antes de ler o arquivo).
