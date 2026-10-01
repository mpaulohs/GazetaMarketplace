# Inventário de campos de anúncio do GazetaOnline, por categoria

> **Em resumo:** lista de todos os campos que o formulário de anúncio do projeto anterior (GazetaOnline) pede, categoria por categoria, com o tipo de cada campo e se ele é obrigatório. É a matéria-prima para mapear os campos para as categorias ativas do GazetaMarketplace nas próximas etapas. Documento de descoberta, gerado só por leitura do código; nada no GazetaOnline foi alterado.
>
> **Fonte:** `C:\Users\mpaul\source\repos\GazetaOnlineProject` (commit `d5a50bc`, mais alterações locais ainda não commitadas que já existiam na pasta; nenhuma delas nos perfis, registros ou validadores lidos), lido em 2026-09-30. Arquivos principais: `src/GazetaOnline.Application/Ads/Categories/**` (perfis de categoria), `src/GazetaOnline.Infrastructure/Hierarchies/*Registration.cs` e `HierarchyServiceCollectionExtensions.cs` (registro das categorias), `src/GazetaOnline.Application/Ads/Validation/**` (obrigatoriedade), `src/GazetaOnline.Core/Entities/Ad.cs` (campos comuns).

## Como o GazetaOnline organiza os campos

- **Campos comuns** ficam na tabela do anúncio (`Ad`) e valem para todas as categorias.
- **Campos específicos** são declarados num **perfil de categoria** (`ICategoryProfile`) e gravados na tabela `AdAttributes` como pares nome/valor (modelo atributo-valor). Cada campo tem um tipo:
  - **Texto** — digitado livremente.
  - **Número** — inteiro digitado.
  - **Lista** — uma opção de uma lista fechada (grava o código e o nome da opção).
  - **Lista múltipla** — várias opções de uma lista fechada.
- **Hierarquia** — alguns veículos escolhem **marca → modelo → ano → versão** em listas encadeadas, vindas de tabelas do banco (catálogo de veículos). As demais categorias têm hierarquia "plana" (sem esse encadeamento).
- **Obrigatoriedade** vem dos validadores: um comum a todos os anúncios e um por categoria.

## Achados principais

1. **Só 29 categorias aceitam anúncio no GazetaOnline.** Há perfil para exatamente as 29 categorias que têm `Slug` em `specs/categories.md`. Para as outras 100 categorias ativas, o serviço de anúncios lança erro ("Nenhum profile registrado para a categoria …"), ou seja, **nenhuma dessas 100 tem formulário no projeto anterior**.
2. **São 17 perfis para 29 categorias:** 11 perfis cobrem uma categoria cada; os de Peças (5 categorias), Telefonia (4) e Eletrodomésticos (7) são reaproveitados, mudando só as listas de opções.
3. **O preço é gravado duas vezes:** como número decimal no anúncio (`Ad.Price`) e como texto no atributo `Price` de todos os perfis.
4. **Diferenças com o SPEC do GazetaMarketplace** (para decidir nas próximas etapas):
   - Fotos: GazetaOnline aceita **JPG, PNG e GIF até 5 MB**, de 50×50 a 3840×2160 px; o SPEC atual supõe **JPG, PNG e WebP até 10 MB** (S16).
   - O GazetaOnline já usa **CEP obrigatório** (`ZipCode`, 8 dígitos), além de Cidade e UF — alinhado com a decisão do Ajuste 2.
   - Imóveis têm **"Vender ou alugar"** (`TransactionType`) e categorias de **aluguel** (Quartos, Temporada), fora do modelo "venda intermediada" do SPEC.
   - Os 5 perfis de veículos têm **vídeo do YouTube** (`VideoYouTubeId`) — campo que o SPEC não prevê.

## Campos comuns (todas as categorias)

| Campo | Tipo | Obrigatório | Regra |
|---|---|---|---|
| Título (`Title`) | Texto | Sim | Não pode ser vazio |
| Descrição (`Description`) | Texto | Sim | Não pode ser vazia |
| Preço (`Price`) | Número decimal | Sim | Formato numérico válido |
| Categoria (`CategoryId`) | Lista | Sim | — |
| CEP (`ZipCode`) | Texto | Sim | Mínimo 8 caracteres |
| Cidade (`City`), UF (`State`) | Texto | — | Gravados no anúncio |
| Fotos | Arquivos | Sim | De 1 a 20; JPG, PNG ou GIF; até 5 MB cada; de 50×50 a 3840×2160 px |

## Campos específicos por categoria

Legenda da coluna **Obrig.**: ✅ obrigatório · — opcional. "Lista (X)" indica a lista de opções usada (nome da classe no GazetaOnline).

### Veículos

#### 33 · Carros, vans e utilitários (`cars`) — perfil `CarsProfile`, hierarquia de catálogo

| Campo | Tipo | Obrig. |
|---|---|---|
| Marca → Modelo → Ano → Versão | Listas encadeadas do catálogo (tabelas `CarBrands`, `CarModels`, `CarYearModels`, `CarVersions`) | ✅ (os 4) |
| Quilometragem (`Mileage`) | Número | ✅ |
| Câmbio (`TransmissionId`) | Lista (`CarTransmission`) | — |
| Portas (`DoorId`) | Lista (`CarDoor`) | — |
| Combustível (`FuelId`) | Lista (`CarFuel`) | — |
| Direção (`SteeringGearId`) | Lista (`CarSteeringGear`) | — |
| Tipo de veículo (`VehicleTypeId`) | Lista (`VehicleType`) | — |
| Potência do motor (`EnginePowerId`) | Lista (`CarEnginePower`) | — |
| Cor (`ColorId`) | Lista (`CarColor`) | — |
| Opcionais (`OptionalItemId`) | Lista múltipla (`OptionalItem`) | — |
| Informações adicionais (`AdditionalInfoId`) | Lista múltipla (`AdditionalInfo`) | — |
| Vídeo do YouTube (`VideoYouTubeId`) | Texto | — |

#### 36 · Motos (`motos`) — perfil `MotorcyclesProfile`, hierarquia de catálogo

| Campo | Tipo | Obrig. |
|---|---|---|
| Marca → Modelo → Ano → Versão | Listas encadeadas do catálogo (tabelas `MotorcycleBrands`, `MotorcycleModels`, `MotorcycleYearModels`, `MotorcycleVersions`) | ✅ (os 4) |
| Quilometragem (`Mileage`) | Número | ✅ |
| Cilindrada (`DisplacementId`) | Lista (`MotorcycleDisplacement`) | ✅ |
| Cor (`ColorId`) | Lista (`CarColor`) | — |
| Opcionais (`OptionalItemId`) | Lista múltipla (`MotorcycleOptionalItem`) | — |
| Informações adicionais (`AdditionalInfoId`) | Lista múltipla (`MotorcycleAdditionalInfo`) | — |
| Vídeo do YouTube (`VideoYouTubeId`) | Texto | — |

#### 34 · Caminhões (`caminhoes`) — perfil `TrucksProfile`, hierarquia plana

| Campo | Tipo | Obrig. |
|---|---|---|
| Ano do modelo (`YearModelId`) | Lista (`VehicleModelYear`) | ✅ |
| Quilometragem (`Mileage`) | Número | ✅ |
| Câmbio (`TransmissionId`) | Lista (`CarTransmission`) | — |
| Combustível (`FuelId`) | Lista (`CarFuel`) | — |
| Direção (`SteeringGearId`) | Lista (`VehicleSteeringGear`) | — |
| Tipo (`VehicleTypeId`) | Lista (`TruckType`) | — |
| Opcionais (`OptionalItemId`) | Lista múltipla (`TruckOptionalItem`) | — |
| Informações adicionais (`AdditionalInfoId`) | Lista múltipla (`VehicleAdditionalInfo`) | — |
| Vídeo do YouTube (`VideoYouTubeId`) | Texto | — |

#### 35 · Ônibus (`onibus`) — perfil `BusesProfile`, hierarquia plana

Mesmos campos e obrigatoriedade de Caminhões (34), trocando as listas: Tipo = `BusType`; Opcionais = `BusOptionalItem`.

#### 37 · Barcos e aeronaves (`barcos-e-aeronaves`) — perfil `BoatsProfile`, hierarquia plana

| Campo | Tipo | Obrig. |
|---|---|---|
| Ano do modelo (`YearModelId`) | Lista (`VehicleModelYear`) | ✅ |
| Quilometragem (`Mileage`) | Número | ✅ |
| Tipo (`VehicleTypeId`) | Lista (`BoatType`) | ✅ |
| Combustível (`FuelId`) | Lista (`CarFuel`) | — |
| Comprimento, Largura, Altura em metros (`LengthMeters`, `WidthMeters`, `HeightMeters`) | Texto | — |
| Informações adicionais (`AdditionalInfoId`) | Lista múltipla (`BoatAdditionalInfo`) | — |
| Vídeo do YouTube (`VideoYouTubeId`) | Texto | — |

> ⚠️ "Quilometragem" obrigatória para barcos e aeronaves: em embarcações costuma-se usar horas de motor. Registrado como achado, não corrigido.

### Autopeças (perfil `PartsProfile`, hierarquia plana)

Categorias 38 (`pecas-carros-vans-e-utilitarios`), 39 (`pecas-caminhoes`), 40 (`pecas-motos`), 41 (`pecas-barcos-e-aeronaves`), 42 (`pecas-onibus`).

| Campo | Tipo | Obrig. |
|---|---|---|
| Condição (`ConditionId`) | Lista (`PartCondition`) | ✅ |
| Tipo de peça (`PartTypeId`) | Lista — 38, 39 e 42: `AutoPartType`; 40: `MotorcyclePartType`; 41: `BoatPartType` | — |
| Cor (`PartColorId`) | Lista (`PartColor`) | — |

### Imóveis (hierarquia plana; validador com obrigatoriedade configurada por categoria)

| Campo | 26 Apartamentos | 27 Casas | 28 Aluguel de quartos | 29 Temporada | 30 Terrenos, sítios e fazendas | 31 Comércio e indústria |
|---|---|---|---|---|---|---|
| Tipo de imóvel (`PropertyTypeId`) | ✅ `ApartmentType` | ✅ `HouseType` | — (não tem) | ✅ `SeasonalType` | ✅ `LandType` | ✅ `CommercialType` |
| Vender ou alugar (`TransactionTypeId`) | ✅ `PropertyTransactionType` | ✅ | — (não tem) | — (não tem) | ✅ | ✅ |
| Forma de pagamento (`PaymentTypeId`) | — (não tem) | — (não tem) | — (não tem) | opcional `SeasonalPaymentType` | — (não tem) | — (não tem) |
| Quartos (`Bedrooms`) | ✅ número | ✅ | — (não tem) | ✅ | — (não tem) | — (não tem) |
| Acomoda quantas pessoas (`Accommodates`) | — (não tem) | — (não tem) | — (não tem) | ✅ número | — (não tem) | — (não tem) |
| Banheiros (`Bathrooms`) | opcional, número | opcional | — (não tem) | opcional | — (não tem) | — (não tem) |
| Área (`AreaM2`), m² com 2 casas decimais | opcional, texto | opcional | — (não tem) | — (não tem) | opcional | opcional |
| Vagas de garagem (`ParkingSpaces`) | opcional, número | opcional | — (não tem) | opcional | — (não tem) | opcional |
| Condomínio (`CondoFee`) | opcional, texto | opcional | — (não tem) | — (não tem) | opcional | opcional |
| IPTU (`PropertyTax`) | opcional, texto | opcional | — (não tem) | — (não tem) | opcional | opcional |
| Características do imóvel (`PropertyFeatureId`), lista múltipla | `ApartmentFeature` | `ApartmentFeature` (reaproveitada) | `RoomFeature` | `SeasonalFeature` | `LandFeature` | `CommercialFeature` |
| Características do condomínio (`CondoFeatureId`), lista múltipla | `ApartmentCondoFeature` | `HouseCondoFeature` | — (não tem) | — (não tem) | — (não tem) | — (não tem) |

> Nota: "Aluguel de quartos" (28) só tem características e preço; nenhum campo específico é obrigatório. Casas reaproveita a lista de características de apartamentos (`ApartmentFeature`).

### Celulares e Telefonia (hierarquia plana; validador de telefonia)

#### 43 · Celulares e Smartphones — perfil `PhonesProfile`

| Campo | Tipo | Obrig. |
|---|---|---|
| Marca (`BrandId`) | Lista (`PhoneBrand`) | ✅ |
| Modelo (`ModelName`) | Texto | ✅ |
| Condição (`ConditionId`) | Lista (`ProductCondition`) | ✅ |
| Armazenamento (`StorageId`) | Lista (`PhoneStorage`) | — |
| Cor (`ColorId`) | Lista (`PhoneColor`) | — |
| Saúde da bateria (`BatteryHealthId`) | Lista (`PhoneBatteryHealth`) | — |

#### 46 · Smartwatches — perfil `SmartwatchesProfile`

| Campo | Tipo | Obrig. |
|---|---|---|
| Marca (`BrandId`) | Lista (`SmartwatchBrand`) | ✅ |
| Condição (`ConditionId`) | Lista (`ProductCondition`) | ✅ |

#### 44, 45, 47, 48 · Produtos de telefonia — perfil `TelephonyProductsProfile`

| Campo | Tipo | Obrig. | Categorias |
|---|---|---|---|
| Tipo de produto (`ProductTypeId`) | Lista — 44: `PhoneAccessoryType`; 45: `PhonePartType`; 47: `SmartwatchAccessoryType`; 48: `LandlinePhoneType` | ✅ | todas |
| Condição (`ConditionId`) | Lista (`ProductCondition`) | ✅ | todas |
| Marcas compatíveis (`CompatibleBrandId`) | Lista múltipla (`PhoneBrand`) | — | só 44 (Acessórios de celular) e 45 (Peças de celular) |

### Eletro (perfil `AppliancesProfile`, hierarquia plana)

| Campo | Tipo | Obrig. |
|---|---|---|
| Tipo de produto (`ProductTypeId`) | Lista, por categoria (abaixo) | ✅ |
| Marca (`BrandId`) | Lista, por categoria (abaixo) | ✅ |
| Voltagem (`VoltageId`) | Lista (`ApplianceVoltage`) | ✅ |
| Condição (`ConditionId`) | Lista (`ProductCondition`) | ✅ |
| Capacidade (`CapacityId`) | Lista — só 128 (`AirConditionerCapacity`) | — |

| Categoria | Lista de tipos | Lista de marcas |
|---|---|---|
| 128 Ar-condicionados | `AirConditionerType` | `AirConditionerBrand` |
| 129 Ventiladores e Climatizadores | `FanType` | `FanBrand` |
| 130 Geladeiras e Freezers | `RefrigeratorType` | `RefrigeratorBrand` |
| 131 Fogões e Fornos | `StoveType` | `StoveBrand` |
| 132 Máquinas de Lavar e Secadoras | `WasherType` | `WasherBrand` |
| 133 Eletroportáteis Para Cozinha e Limpeza | `KitchenApplianceType` | `KitchenApplianceBrand` |
| 134 Eletroportáteis Para Cuidados Pessoais | `PersonalCareApplianceType` | `PersonalCareApplianceBrand` |

## Cobertura: categorias ativas do GazetaMarketplace × GazetaOnline

| Situação | Quantidade | Categorias |
|---|---|---|
| Com campos específicos no GazetaOnline | **29** | Imóveis (26–31), Veículos (33–37), Autopeças (38–42), Celulares e Telefonia (43–48), Eletro (128–134) |
| Sem perfil no GazetaOnline (sem formulário) | **100** | Todas as demais categorias ativas de `specs/discovery/categories-active.md` |
| **Total de categorias ativas** | **129** | |

## Correção (2026-09-30, Etapa 5)

Na primeira versão deste documento faltou o campo **Área (`AreaM2`)** de Apartamentos (26), Casas (27), Terrenos (30) e Comércio (31): a busca usada aceitava só letras no nome do atributo e ignorou o dígito "2". A linha foi incluída na tabela de Imóveis. Nenhum outro atributo tem dígito no nome (verificado).

## O que não foi levantado nesta etapa

- **As opções de cada lista** (por exemplo, quais combustíveis ou quais tipos de apartamento). Estão em classes do tipo `*.All` em `src/GazetaOnline.Core/**/Lookups` e, para marca/modelo/ano/versão de carros e motos, em tabelas do banco (não consultado, conforme a regra "só leitura, sem banco").
- **Rótulos e ordem na tela**: foram lidos do código de domínio, não das telas `Views/Admin*/Create*.cshtml`; os nomes em português acima são descrições, não os textos exatos da interface.
