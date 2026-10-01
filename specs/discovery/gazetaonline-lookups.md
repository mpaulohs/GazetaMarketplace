# Listas de opções do GazetaOnline (29 categorias herdadas)

> **Em resumo:** todas as opções que os campos do tipo "lista" do GazetaOnline oferecem (por exemplo, os combustíveis ou os tipos de apartamento), com o código (id) e o nome de cada opção e as categorias que usam a lista. Serve para reaproveitar as listas no GazetaMarketplace sem digitá-las de novo. Documento de descoberta, gerado só por leitura do código em `src/GazetaOnline.Core/**/Lookups`.

## Resumo

- **67 listas** lidas, com **666 opções** no total.
- **Listas geradas por código:** `VehicleModelYear` — sem itens fixos (ver a própria seção).
- **Listas que existem no código mas nenhum perfil usa:** `HouseFeature`.
- **Catálogo de veículos (marca → modelo → ano → versão de carros e motos) não é uma lista de código:** fica em tabelas do banco do GazetaOnline, preenchidas pelo projeto `GazetaOnline.Import` (ver "Achados críticos"). Não foi extraído.

## ⚠️ Achados críticos (fora do escopo de campos, mas urgentes)

1. **Credencial do banco de produção dentro do código.** O arquivo `src/GazetaOnline.Import/Program.cs` do GazetaOnline contém, em texto puro, o endereço do servidor SQL, o usuário e a senha do banco (usados quando a variável `CARCATALOG_CONNSTRING` não existe). **A credencial não foi copiada para este documento.** Recomendação: trocar a senha do banco imediatamente, retirar o valor do código e do histórico do git, e usar só variável de ambiente ou cofre de segredos.
2. **O catálogo de veículos foi coletado da API interna da OLX** (`valet.olx.com.br`), pelo coletor `OlxValetApiClient`. Usar dados coletados de um concorrente sem autorização tem risco jurídico (termos de uso e direito sobre a base de dados). Recomendação: **não reaproveitar esse catálogo no GazetaMarketplace** sem validação jurídica; preferir uma fonte licenciada ou pública (por exemplo, a tabela FIPE). Isso responde parcialmente à pergunta Q4 do `field-mapping.md`.

## Comuns

### `ProductCondition` — Condição · 43–48, 128–134

Arquivo: `src/GazetaOnline.Core/Common/Lookups/ProductCondition.cs`

| Id | Opção |
|---|---|
| 1 | Novo |
| 2 | Usado - Excelente |
| 3 | Usado - Bom |
| 4 | Recondicionado |
| 5 | Com defeito ou avarias |
## Veículos

### `VehicleModelYear` — Ano do modelo · 34, 35, 37

Arquivo: `src/GazetaOnline.Core/Ads/Vehicles/Lookups/VehicleModelYear.cs`

Gerada por código: do ano seguinte ao atual até 1951, mais "1950 ou anterior" (id 1950). O id é o próprio ano.

### `MotorcycleDisplacement` — Cilindrada · 36

Arquivo: `src/GazetaOnline.Core/Ads/Motorcycles/Lookups/MotorcycleDisplacement.cs`

39 opções: 50 (1) · 85 (2) · 100 (3) · 110 (4) · 115 (5) · 120 (6) · 125 (7) · 150 (8) · 155 (9) · 160 (10) · 180 (11) · 190 (12) · 200 (13) · 230 (14) · 250 (15) · 300 (16) · 321 (17) · 350 (18) · 390 (19) · 400 (20) · 411 (21) · 420 (22) · 450 (23) · 500 (24) · 550 (25) · 600 (26) · 636 (27) · 650 (28) · 660 (29) · 689 (30) · 700 (31) · 750 (32) · 765 (33) · 800 (34) · 850 (35) · 900 (36) · 950 (37) · 1000 (38) · Acima de 1.000 (39)

### `CarFuel` — Combustível · 33, 34, 35, 37

Arquivo: `src/GazetaOnline.Core/Ads/CarsVansUtilityVehicles/Lookups/CarFuel.cs`

| Id | Opção |
|---|---|
| 1 | Gasolina |
| 2 | Álcool |
| 3 | Flex |
| 4 | Diesel |
| 5 | Híbrido |
| 6 | Elétrico |

### `CarColor` — Cor · 33, 36

Arquivo: `src/GazetaOnline.Core/Ads/CarsVansUtilityVehicles/Lookups/CarColor.cs`

| Id | Opção |
|---|---|
| 1 | Amarelo |
| 2 | Azul |
| 3 | Branco |
| 4 | Cinza |
| 5 | Laranja |
| 6 | Prata |
| 7 | Preto |
| 8 | Verde |
| 9 | Vermelho |
| 10 | Outro |

### `CarTransmission` — Câmbio · 33, 34, 35

Arquivo: `src/GazetaOnline.Core/Ads/CarsVansUtilityVehicles/Lookups/CarTransmission.cs`

| Id | Opção |
|---|---|
| 1 | Manual |
| 2 | Automático |
| 3 | Semi-Automático |
| 4 | Automatizado |

### `CarSteeringGear` — Direção · 33

Arquivo: `src/GazetaOnline.Core/Ads/CarsVansUtilityVehicles/Lookups/CarSteeringGear.cs`

| Id | Opção |
|---|---|
| 1 | Hidráulica |
| 2 | Elétrica |
| 3 | Mecânica |
| 4 | Assistida |
| 5 | Eletro-hidráulica |

### `VehicleSteeringGear` — Direção · 34, 35

Arquivo: `src/GazetaOnline.Core/Ads/Vehicles/Lookups/VehicleSteeringGear.cs`

| Id | Opção |
|---|---|
| 1 | Elétrica |
| 2 | Eletro-hidráulica |
| 3 | Hidráulica |
| 4 | Mecânica |

### `AdditionalInfo` — Informações adicionais · 33

Arquivo: `src/GazetaOnline.Core/Ads/CarsVansUtilityVehicles/Lookups/AdditionalInfo.cs`

| Id | Opção |
|---|---|
| 1 | Carro de leilão |
| 2 | Com chave reserva |
| 3 | Com garantia de fábrica |
| 4 | Com manual |
| 5 | Com multas |
| 6 | IPVA pago |
| 7 | Revisões feitas em concessionária |
| 8 | Único dono |
| 9 | Veículo em financiamento |
| 10 | Veículo quitado |

### `VehicleAdditionalInfo` — Informações adicionais · 34, 35

Arquivo: `src/GazetaOnline.Core/Ads/Vehicles/Lookups/VehicleAdditionalInfo.cs`

| Id | Opção |
|---|---|
| 1 | Com chave reserva |
| 2 | Com garantia de fábrica |
| 3 | Com manual |
| 4 | Com multas |
| 5 | De leilão |
| 6 | IPVA pago |
| 7 | Revisões feitas em concessionária |
| 8 | Único dono |
| 9 | Veículo em financiamento |
| 10 | Veículo quitado |

### `MotorcycleAdditionalInfo` — Informações adicionais · 36

Arquivo: `src/GazetaOnline.Core/Ads/Motorcycles/Lookups/MotorcycleAdditionalInfo.cs`

| Id | Opção |
|---|---|
| 1 | Com multas |
| 2 | De leilão |
| 3 | IPVA pago |
| 4 | Único dono |
| 5 | Veículo em financiamento |
| 6 | Veículo quitado |

### `BoatAdditionalInfo` — Informações adicionais · 37

Arquivo: `src/GazetaOnline.Core/Ads/Boats/Lookups/BoatAdditionalInfo.cs`

| Id | Opção |
|---|---|
| 1 | De leilão |
| 2 | Único dono |
| 3 | Veículo em financiamento |
| 4 | Veículo quitado |

### `OptionalItem` — Opcionais · 33

Arquivo: `src/GazetaOnline.Core/Ads/CarsVansUtilityVehicles/Lookups/OptionalItem.cs`

20 opções: Airbag (1) · Ar Condicionado (2) · Alarme (3) · Bancos de Couro (4) · Blindado (5) · Camera de ré (6) · Com Kit GNV (7) · Computador de bordo (8) · Conexão USB (9) · Controle automático de velocidade (10) · Interface Bluetooth (11) · Navegador GPS (12) · Rodas de liga leve (13) · Sensor de ré (14) · Som (15) · Teto Solar (16) · Tração 4x4 (17) · Trava elétrica (18) · Vidro elétrico (19) · Volante multifuncional (20)

### `TruckOptionalItem` — Opcionais · 34

Arquivo: `src/GazetaOnline.Core/Ads/Trucks/Lookups/TruckOptionalItem.cs`

| Id | Opção |
|---|---|
| 1 | ABS |
| 2 | Ar Condicionado |
| 3 | Blindado |
| 4 | Trava elétrica |
| 5 | Vidro elétrico |

### `BusOptionalItem` — Opcionais · 35

Arquivo: `src/GazetaOnline.Core/Ads/Buses/Lookups/BusOptionalItem.cs`

| Id | Opção |
|---|---|
| 1 | Ar Condicionado |

### `MotorcycleOptionalItem` — Opcionais · 36

Arquivo: `src/GazetaOnline.Core/Ads/Motorcycles/Lookups/MotorcycleOptionalItem.cs`

| Id | Opção |
|---|---|
| 1 | ABS |
| 2 | Alarme |
| 3 | Amortecedor de direção |
| 4 | Bolsa / Baú / Bauleto |
| 5 | Computador de bordo |
| 6 | Contra peso no guidon |
| 7 | Escapamento esportivo |
| 8 | Faróis de neblina |
| 9 | GPS |
| 10 | Som |

### `CarDoor` — Portas · 33

Arquivo: `src/GazetaOnline.Core/Ads/CarsVansUtilityVehicles/Lookups/CarDoor.cs`

| Id | Opção |
|---|---|
| 1 | 2 portas |
| 2 | 4 portas |

### `CarEnginePower` — Potência · 33

Arquivo: `src/GazetaOnline.Core/Ads/CarsVansUtilityVehicles/Lookups/CarEnginePower.cs`

13 opções: 1.0 (1) · 1.2 (2) · 1.3 (3) · 1.4 (4) · 1.5 (5) · 1.6 (6) · 1.7 (7) · 1.8 (8) · 1.9 (9) · 2.0 (10) · 2.0 - 2.9 (11) · 3.0 - 3.9 (12) · 4.0 ou mais (13)

### `VehicleType` — Tipo de veículo · 33

Arquivo: `src/GazetaOnline.Core/Ads/CarsVansUtilityVehicles/Lookups/VehicleType.cs`

| Id | Opção |
|---|---|
| 1 | Buggy |
| 2 | Caminhão Leve |
| 3 | Conversível |
| 4 | Coupé |
| 5 | Hatch |
| 6 | Perua |
| 7 | Pick-up |
| 8 | Sedã |
| 9 | SUV |
| 10 | Van/Utilitário |

### `TruckType` — Tipo · 34

Arquivo: `src/GazetaOnline.Core/Ads/Trucks/Lookups/TruckType.cs`

| Id | Opção |
|---|---|
| 1 | Baú |
| 2 | Boladeiro |
| 3 | Caçamba |
| 4 | Carga seca |
| 5 | Carroceria |
| 6 | Graneleiro |
| 7 | Guindaste |
| 8 | Munck |
| 9 | No Chassi |
| 10 | Outro |
| 11 | Pipa |
| 12 | Plataforma |

### `BusType` — Tipo · 35

Arquivo: `src/GazetaOnline.Core/Ads/Buses/Lookups/BusType.cs`

| Id | Opção |
|---|---|
| 1 | Micro ônibus |
| 2 | Ônibus |
| 3 | Outros |

### `BoatType` — Tipo · 37

Arquivo: `src/GazetaOnline.Core/Ads/Boats/Lookups/BoatType.cs`

| Id | Opção |
|---|---|
| 1 | Barco |
| 2 | Embarcação Inflável |
| 3 | Hovercraft |
| 4 | Jet Ski |
| 5 | Lancha |
| 6 | Motor |
| 7 | Outros |
| 8 | Pedalinho |
| 9 | Veleiro Monocasco |
| 10 | Veleiro Multicasco |
## Peças

### `PartCondition` — Condição · 38–42

Arquivo: `src/GazetaOnline.Core/Ads/Parts/Lookups/PartCondition.cs`

| Id | Opção |
|---|---|
| 1 | Novo |
| 2 | Usado - Excelente |
| 3 | Usado - Bom |
| 4 | Recondicionado |
| 5 | Com defeito ou avarias |

### `PartColor` — Cor · 38–42

Arquivo: `src/GazetaOnline.Core/Ads/Parts/Lookups/PartColor.cs`

| Id | Opção |
|---|---|
| 1 | Amarelo |
| 2 | Azul |
| 3 | Branco |
| 4 | Cinza |
| 5 | Laranja |
| 6 | Prata |
| 7 | Preto |
| 8 | Verde |
| 9 | Vermelho |
| 10 | Outra |

### `AutoPartType` — Tipo de peça · 38, 39, 42

Arquivo: `src/GazetaOnline.Core/Ads/Parts/Lookups/AutoPartType.cs`

| Id | Opção |
|---|---|
| 1 | Acessórios para exterior |
| 2 | Acessórios para interior |
| 3 | Calotas |
| 4 | GPS |
| 5 | Peças automotivas |
| 6 | Pneus |
| 7 | Rodas |
| 8 | Som e multimídia |
| 9 | Tuning e Performance |
| 10 | Outros |

### `MotorcyclePartType` — Tipo de peça · 40

Arquivo: `src/GazetaOnline.Core/Ads/Parts/Lookups/MotorcyclePartType.cs`

| Id | Opção |
|---|---|
| 1 | Acabamento |
| 2 | Alarmes |
| 3 | Bagageiros, baús e mochilas |
| 4 | Calotas |
| 5 | Capacetes |
| 6 | Peças de motos |
| 7 | Pneus |
| 8 | Rodas |
| 9 | Roupas de moto |
| 10 | Suportes |
| 11 | Outros |

### `BoatPartType` — Tipo de peça · 41

Arquivo: `src/GazetaOnline.Core/Ads/Parts/Lookups/BoatPartType.cs`

| Id | Opção |
|---|---|
| 1 | Âncoras |
| 2 | Bombas |
| 3 | Cabos |
| 4 | Hélices |
| 5 | Iluminação |
| 6 | Inversores |
| 7 | Motores |
| 8 | Rotores |
| 9 | Sonares e GPS |
| 10 | Velas |
| 11 | Outros |
## Imóveis

### `ApartmentFeature` — Características · 26, 27

Arquivo: `src/GazetaOnline.Core/Ads/RealEstate/Lookups/ResidentialFeature.cs` (a classe `ApartmentFeature` fica neste arquivo, de nome diferente)

| Id | Opção |
|---|---|
| 1 | Área de serviço |
| 2 | Armários no quarto |
| 3 | Armários na cozinha |
| 4 | Mobiliado |
| 5 | Ar condicionado |
| 6 | Churrasqueira |
| 7 | Varanda |
| 8 | Academia |
| 9 | Piscina |
| 10 | Quarto de serviço |

### `RoomFeature` — Características · 28

Arquivo: `src/GazetaOnline.Core/Ads/RealEstate/Lookups/RoomFeature.cs`

| Id | Opção |
|---|---|
| 1 | Armário no quarto |
| 2 | Banheiro no quarto |
| 3 | Mobiliado |
| 4 | Ar condicionado |
| 5 | Varanda |
| 6 | Aquecimento |
| 7 | Internet |
| 8 | TV a cabo |

### `SeasonalFeature` — Características · 29

Arquivo: `src/GazetaOnline.Core/Ads/RealEstate/Lookups/SeasonalFeature.cs`

17 opções: Geladeira (1) · Fogão (2) · Estacionamento (3) · Ventilador (4) · Varanda/Terraço (5) · TV a cabo (6) · Churrasqueira (7) · Piscina (8) · Ar condicionado (9) · Roupa de cama (10) · Internet (11) · Permitido animais (12) · Máquina de lavar (13) · Toalhas (14) · Café da manhã (15) · Aquecimento (16) · Lareira (17)

### `LandFeature` — Características · 30

Arquivo: `src/GazetaOnline.Core/Ads/RealEstate/Lookups/LandFeature.cs`

| Id | Opção |
|---|---|
| 1 | Área verde |
| 2 | Casa sede |
| 3 | Pomar |
| 4 | Piscina |
| 5 | Churrasqueira |
| 6 | Poço artesiano |
| 7 | Água encanada |
| 8 | Energia elétrica |
| 9 | Campo de futebol |
| 10 | Acesso asfaltado |

### `CommercialFeature` — Características · 31

Arquivo: `src/GazetaOnline.Core/Ads/RealEstate/Lookups/CommercialFeature.cs`

| Id | Opção |
|---|---|
| 1 | Garagem |
| 2 | Segurança 24h |
| 3 | Câmeras de segurança |
| 4 | Elevador |
| 5 | Portaria |
| 6 | Acesso para deficientes |

### `ApartmentCondoFeature` — Condomínio · 26

Arquivo: `src/GazetaOnline.Core/Ads/RealEstate/Lookups/ApartmentCondoFeature.cs`

| Id | Opção |
|---|---|
| 1 | Condomínio fechado |
| 2 | Elevador |
| 3 | Segurança 24h |
| 4 | Portaria |
| 5 | Permitido animais |
| 6 | Academia |
| 7 | Piscina |
| 8 | Salão de festas |

### `HouseCondoFeature` — Condomínio · 27

Arquivo: `src/GazetaOnline.Core/Ads/RealEstate/Lookups/HouseCondoFeature.cs`

| Id | Opção |
|---|---|
| 1 | Condomínio fechado |
| 2 | Segurança 24h |
| 3 | Área murada |
| 4 | Permitido animais |
| 5 | Portão eletrônico |
| 6 | Academia |
| 7 | Piscina |

### `SeasonalPaymentType` — Forma de pagamento · 29

Arquivo: `src/GazetaOnline.Core/Ads/RealEstate/Lookups/SeasonalPaymentType.cs`

| Id | Opção |
|---|---|
| 1 | Por dia |
| 2 | Por semana |
| 3 | Por mês |
| 4 | Pacote |

### `ApartmentType` — Tipo · 26

Arquivo: `src/GazetaOnline.Core/Ads/RealEstate/Lookups/ApartmentType.cs`

| Id | Opção |
|---|---|
| 1 | Padrão |
| 2 | Cobertura |
| 3 | Duplex/triplex |
| 4 | Kitnet |
| 5 | Loft |

### `HouseType` — Tipo · 27

Arquivo: `src/GazetaOnline.Core/Ads/RealEstate/Lookups/HouseType.cs`

| Id | Opção |
|---|---|
| 1 | Padrão |
| 2 | Casa de vila |
| 3 | Casa de condomínio |

### `SeasonalType` — Tipo · 29

Arquivo: `src/GazetaOnline.Core/Ads/RealEstate/Lookups/SeasonalType.cs`

| Id | Opção |
|---|---|
| 1 | Apartamento |
| 2 | Casa |
| 3 | Quarto individual |
| 4 | Quarto compartilhado |
| 5 | Hotel, hostel e pousada |
| 6 | Sítio, fazenda e chácara |

### `LandType` — Tipo · 30

Arquivo: `src/GazetaOnline.Core/Ads/RealEstate/Lookups/LandType.cs`

| Id | Opção |
|---|---|
| 1 | Terrenos e lotes |
| 2 | Sítios e chácaras |
| 3 | Fazendas |
| 4 | Outros |

### `CommercialType` — Tipo · 31

Arquivo: `src/GazetaOnline.Core/Ads/RealEstate/Lookups/CommercialType.cs`

| Id | Opção |
|---|---|
| 1 | Escritório |
| 2 | Galpão/Depósito |
| 3 | Hotel |
| 4 | Fábrica |
| 5 | Garagem/Vaga |
| 6 | Loja |
| 7 | Outros |

### `PropertyTransactionType` — Vender ou alugar · 26, 27, 30, 31

Arquivo: `src/GazetaOnline.Core/Ads/RealEstate/Lookups/PropertyTransactionType.cs`

| Id | Opção |
|---|---|
| 1 | Oferta - Vendo |
| 2 | Oferta - Alugo |

### `HouseFeature` — não usada por nenhum perfil

Arquivo: `src/GazetaOnline.Core/Ads/RealEstate/Lookups/HouseFeature.cs`

| Id | Opção |
|---|---|
| 1 | Área de serviço |
| 2 | Armários no quarto |
| 3 | Ar condicionado |
| 4 | Varanda |
| 5 | Armários na cozinha |
| 6 | Mobiliado |
| 7 | Churrasqueira |
| 8 | Quarto de serviço |
## Celulares e Telefonia

### `PhoneStorage` — Armazenamento · 43

Arquivo: `src/GazetaOnline.Core/Ads/Phones/Lookups/PhoneStorage.cs`

| Id | Opção |
|---|---|
| 1 | 512MB |
| 2 | 1GB |
| 3 | 2GB |
| 4 | 4GB |
| 5 | 8GB |
| 6 | 16GB |
| 7 | 32GB |
| 8 | 64GB |
| 9 | 128GB |
| 10 | 256GB |
| 11 | 512GB |
| 12 | 1TB |

### `PhoneColor` — Cor · 43

Arquivo: `src/GazetaOnline.Core/Ads/Phones/Lookups/PhoneColor.cs`

14 opções: Amarelo (1) · Azul (2) · Branco (3) · Bronze (4) · Cinza (5) · Dourado (6) · Laranja (7) · Prata (8) · Preto (9) · Rosa (10) · Roxo (11) · Verde (12) · Vermelho (13) · Outros (14)

### `PhoneBrand` — Marca · 43; Marcas compatíveis · 44, 45

Arquivo: `src/GazetaOnline.Core/Ads/Phones/Lookups/PhoneBrand.cs`

| Id | Opção |
|---|---|
| 1 | Apple |
| 2 | Asus |
| 3 | Huawei |
| 4 | Infinix |
| 5 | Lenovo |
| 6 | LG |
| 7 | Motorola |
| 8 | Samsung |
| 9 | Sony |
| 10 | Xiaomi |
| 11 | Outros |

### `SmartwatchBrand` — Marca · 46

Arquivo: `src/GazetaOnline.Core/Ads/Phones/Lookups/SmartwatchBrand.cs`

16 opções: Apple (1) · Samsung (2) · Garmin (3) · Xiaomi (4) · Amazfit (5) · Fitbit (6) · Huawei (7) · Fossil (8) · Mobvoi (9) · Suunto (10) · Polar (11) · Coros (12) · Tag Heuer (13) · Withings (14) · Michael Kors (15) · Outros (16)

### `PhoneBatteryHealth` — Saúde da bateria · 43

Arquivo: `src/GazetaOnline.Core/Ads/Phones/Lookups/PhoneBatteryHealth.cs`

| Id | Opção |
|---|---|
| 1 | Perfeita (95% até 100%) |
| 2 | Boa (80% até 94%) |
| 3 | OK (60% até 79%) |
| 4 | Ruim (40% até 59%) |
| 5 | Muito ruim (abaixo de 39%) |

### `PhoneAccessoryType` — Tipo · 44

Arquivo: `src/GazetaOnline.Core/Ads/Phones/Lookups/PhoneAccessoryType.cs`

| Id | Opção |
|---|---|
| 1 | Adaptadores para Celular |
| 2 | Cabos para Celular |
| 3 | Capas para Celular |
| 4 | Carregadores de Celular |
| 5 | Carregadores Portáteis |
| 6 | Controles para Celular |
| 7 | Estabilizadores e Gimbals |
| 8 | Microfones para Celular |
| 9 | Películas para Celular |
| 10 | Ring Lights para Celular |
| 11 | Suportes para Celular |
| 12 | Outros |

### `PhonePartType` — Tipo · 45

Arquivo: `src/GazetaOnline.Core/Ads/Phones/Lookups/PhonePartType.cs`

| Id | Opção |
|---|---|
| 1 | Baterias de Celular |
| 2 | Carcaças de Celular |
| 3 | Conectores de Celular |
| 4 | Câmeras de Celular |
| 5 | Displays e Telas de Celular |
| 6 | Placas-mãe de Celular |
| 7 | Outros |

### `SmartwatchAccessoryType` — Tipo · 47

Arquivo: `src/GazetaOnline.Core/Ads/Phones/Lookups/SmartwatchAccessoryType.cs`

| Id | Opção |
|---|---|
| 1 | Carregadores para Smartwatch |
| 2 | Películas para Smartwatch |
| 3 | Pulseiras para Smartwatch |
| 4 | Outros |

### `LandlinePhoneType` — Tipo · 48

Arquivo: `src/GazetaOnline.Core/Ads/Phones/Lookups/LandlinePhoneType.cs`

| Id | Opção |
|---|---|
| 1 | Aparelhos de Telefone Fixo |
| 2 | Centrais Telefônicas |
| 3 | Interfones |
| 4 | Telefones sem Fio |
| 5 | Walkie Talkie |
| 6 | Outros |
## Eletro

### `AirConditionerCapacity` — Capacidade · 128

Arquivo: `src/GazetaOnline.Core/Ads/Appliances/Lookups/AirConditionerCapacity.cs`

| Id | Opção |
|---|---|
| 1 | Até 9000 BTUs |
| 2 | 10000 até 15000 BTUs |
| 3 | 16000 até 20000 BTUs |
| 4 | Acima de 20000 BTUs |

### `AirConditionerBrand` — Marca · 128

Arquivo: `src/GazetaOnline.Core/Ads/Appliances/Lookups/AirConditionerBrand.cs`

23 opções: Agratto (1) · Britânia (2) · Carrier (3) · Comfee (4) · Consul (5) · Coolair (6) · Daikin (7) · Electrolux (8) · Elgin (9) · EOS (10) · Gree (11) · Gree Eco Garden (12) · Hitachi (13) · LG (14) · Midea (15) · Philco (16) · Samsung (17) · Springer (18) · Springer Carrier (19) · Springer Midea (20) · Springer Midea Comfee (21) · TCL (22) · Outros (23)

### `FanBrand` — Marca · 129

Arquivo: `src/GazetaOnline.Core/Ads/Appliances/Lookups/FanBrand.cs`

20 opções: Arno (1) · Britânia (2) · Cadence (3) · Consul (4) · Climatize (5) · Electrolux (6) · Elgin (7) · EOS (8) · Faet (9) · Loren Sid (10) · Mallory (11) · Midea (12) · Mondial (13) · Philco (14) · Spirit (15) · Springer (16) · Tron (17) · Venti-Delta (18) · Ventisol (19) · Outros (20)

### `RefrigeratorBrand` — Marca · 130

Arquivo: `src/GazetaOnline.Core/Ads/Appliances/Lookups/RefrigeratorBrand.cs`

19 opções: Ártico (1) · Black+Decker (2) · Brastemp (3) · Britânia (4) · Consul (5) · Continental (6) · Conservex (7) · Dako (8) · Electrolux (9) · Esmaltec (10) · EOS (11) · EVOL (12) · Hisense (13) · LG (14) · Midea (15) · Panasonic (16) · Philco (17) · Samsung (18) · Outros (19)

### `StoveBrand` — Marca · 131

Arquivo: `src/GazetaOnline.Core/Ads/Appliances/Lookups/StoveBrand.cs`

32 opções: Agratto (1) · Alfa Suportel (2) · Atlas (3) · Bosch (4) · Braslar (5) · Brastemp (6) · Britânia (7) · Cadence (8) · Chamalux (9) · Clarice (10) · Consul (11) · Continental (12) · Dako (13) · Electrolux (14) · Elgin (15) · Esmaltec (16) · Fischer (17) · Fogatti (18) · Franke (19) · Itatiaia (20) · LG (21) · Midea (22) · Mondial (23) · Multilaser (24) · Oster (25) · Panasonic (26) · Philco (27) · Samsung (28) · Sanyo (29) · Sharp (30) · Tramontina (31) · Outros (32)

### `WasherBrand` — Marca · 132

Arquivo: `src/GazetaOnline.Core/Ads/Appliances/Lookups/WasherBrand.cs`

21 opções: Arno (1) · Bosch (2) · Brastemp (3) · Britânia (4) · Colormaq (5) · Consul (6) · Continental (7) · Electrolux (8) · GE (9) · HiSense (10) · LG (11) · Midea (12) · Mueller (13) · Panasonic (14) · Philco (15) · Praxis (16) · Samsung (17) · Suggar (18) · Toshiba (19) · Wanke (20) · Outros (21)

### `KitchenApplianceBrand` — Marca · 133

Arquivo: `src/GazetaOnline.Core/Ads/Appliances/Lookups/KitchenApplianceBrand.cs`

33 opções: 3 Corações (1) · Arno (2) · Black+Decker (3) · Bosch (4) · Brastemp (5) · Britânia (6) · Cadence (7) · Consul (8) · Electrolux (9) · Elgin (10) · Fix (11) · Hitachi (12) · HomeGoods (13) · LG (14) · LIP (15) · Lynus (16) · Midea (17) · Mondial (18) · Multifrio (19) · Nescafé Dolce Gusto (20) · Nespresso (21) · Oster (22) · Panasonic (23) · Philco (24) · Philips Walita (25) · Samsung (26) · Singer (27) · Suggar (28) · Tramontina (29) · Ultra (30) · Vonder (31) · WAP (32) · Outros (33)

### `PersonalCareApplianceBrand` — Marca · 134

Arquivo: `src/GazetaOnline.Core/Ads/Appliances/Lookups/PersonalCareApplianceBrand.cs`

19 opções: Arno (1) · BaBylissPRO (2) · Britânia (3) · Cadence (4) · Conair (5) · Electrolux (6) · Elgin (7) · Gama Italy (8) · Hitachi (9) · HomeGoods (10) · LIP (11) · Mondial (12) · Panasonic (13) · Philco (14) · Philips (15) · Taiff (16) · Tany (17) · Wahl (18) · Outros (19)

### `AirConditionerType` — Tipo · 128

Arquivo: `src/GazetaOnline.Core/Ads/Appliances/Lookups/AirConditionerType.cs`

| Id | Opção |
|---|---|
| 1 | Ar-condicionados de Janela |
| 2 | Ar-condicionados Piso Teto |
| 3 | Ar-condicionados Portáteis |
| 4 | Ar-condicionados Split |
| 5 | Ar-condicionados Split Cassete |
| 6 | Ar-condicionados Split Inverter |
| 7 | Peças Para Ar-condicionado |
| 8 | Outros |

### `FanType` — Tipo · 129

Arquivo: `src/GazetaOnline.Core/Ads/Appliances/Lookups/FanType.cs`

| Id | Opção |
|---|---|
| 1 | Aquecedores |
| 2 | Circuladores de Ar |
| 3 | Climatizadores de Ar |
| 4 | Exaustores |
| 5 | Ventiladores de Parede |
| 6 | Ventiladores de Torre e Coluna |
| 7 | Ventiladores de Mesa |
| 8 | Ventiladores de Teto |
| 9 | Peças Para Ventiladores e Climatizadores |
| 10 | Outros |

### `RefrigeratorType` — Tipo · 130

Arquivo: `src/GazetaOnline.Core/Ads/Appliances/Lookups/RefrigeratorType.cs`

| Id | Opção |
|---|---|
| 1 | Adegas e Cervejeiras Climatizadas |
| 2 | Freezers Horizontais |
| 3 | Freezers Verticais |
| 4 | Frigobares |
| 5 | Geladeiras |
| 6 | Geladeiras Duplex |
| 7 | Geladeiras Inverse |
| 8 | Peças Para Geladeiras e Freezers |
| 9 | Outros |

### `StoveType` — Tipo · 131

Arquivo: `src/GazetaOnline.Core/Ads/Appliances/Lookups/StoveType.cs`

| Id | Opção |
|---|---|
| 1 | Coifas e Depuradores |
| 2 | Cooktop a Gás |
| 3 | Cooktop Por Indução |
| 4 | Fogões a Gás |
| 5 | Fogões Elétricos |
| 6 | Fornos a Gás |
| 7 | Fornos Elétricos |
| 8 | Micro-Ondas |
| 9 | Peças Para Fogões e Fornos |
| 10 | Outros |

### `WasherType` — Tipo · 132

Arquivo: `src/GazetaOnline.Core/Ads/Appliances/Lookups/WasherType.cs`

| Id | Opção |
|---|---|
| 1 | Centrífugas de Roupa |
| 2 | Lava e Seca |
| 3 | Lava Louças |
| 4 | Máquinas de Lavar Roupa |
| 5 | Secadoras de Roupa |
| 6 | Tanquinhos |
| 7 | Peças Para Máquinas de Lavar e Secar |
| 8 | Outros |

### `KitchenApplianceType` — Tipo · 133

Arquivo: `src/GazetaOnline.Core/Ads/Appliances/Lookups/KitchenApplianceType.cs`

| Id | Opção |
|---|---|
| 1 | Aspiradores de Pó |
| 2 | Batedeiras Elétricas |
| 3 | Bebedouros e Purificadores de Água |
| 4 | Cafeteiras Elétricas |
| 5 | Churrasqueiras Elétricas |
| 6 | Ferros de Passar |
| 7 | Fritadeiras Elétricas |
| 8 | Grills, Sanduicheiras e Torradeiras |
| 9 | Liquidificadores |
| 10 | Máquinas de Costurar Elétricas |
| 11 | Panelas Elétricas |
| 12 | Outros |

### `PersonalCareApplianceType` — Tipo · 134

Arquivo: `src/GazetaOnline.Core/Ads/Appliances/Lookups/PersonalCareApplianceType.cs`

| Id | Opção |
|---|---|
| 1 | Aparadores e Barbeadores Elétricos |
| 2 | Chapinhas |
| 3 | Escovas Elétricas |
| 4 | Modeladores de Cacho |
| 5 | Pranchas de Cabelo |
| 6 | Secadores de Cabelo |
| 7 | Outros |

### `ApplianceVoltage` — Voltagem · 128–134

Arquivo: `src/GazetaOnline.Core/Ads/Appliances/Lookups/ApplianceVoltage.cs`

| Id | Opção |
|---|---|
| 1 | 127v |
| 2 | 220v |
| 3 | Bivolt |

