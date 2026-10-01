# Rótulos e textos das telas de cadastro do GazetaOnline

> **Em resumo:** para cada uma das 29 categorias que tinham formulário no GazetaOnline, a lista dos campos **na ordem em que aparecem na tela**, com o rótulo exato, o nome técnico do campo, o texto de exemplo dentro do campo (placeholder) e o texto de ajuda. O asterisco (\*) faz parte do rótulo original e indica campo obrigatório na tela. Documento de descoberta, gerado só por leitura das telas `src/GazetaOnline.Web/Views/Admin*/Create*.cshtml` e das partes compartilhadas em `Views/Shared/`.

## Resumo

- **29 telas** lidas, **307 campos** no total (incluindo os campos comuns de cada tela); **220** rótulos têm asterisco de obrigatório.
- **Textos em inglês na tela** (a interface é em português): "Your changes will be saved as draft/pending and require publisher approval." — aparecem em 29 telas.
- **Redação útil para reaproveitar:** rótulos que falam com quem anuncia ("Defina o preço do seu veículo", "CEP da localização do veículo", "Quer vender ou alugar?") e exemplos de preenchimento ("Ex: 50.800 km").
- **Pontos a padronizar no GazetaMarketplace:** "Titulo" sem acento; mistura de rótulos como pergunta e como substantivo; o asterisco colado ao texto (sem legenda explicando o que significa — o protótipo já resolveu com a legenda do Ajuste 1).
- **Rótulo de veículo em tela que não é de veículo** (copiado de outra tela): "CEP da localização do veículo*" aparece em 11 telas fora de Veículos (por exemplo, 26, 27, 28, 29, 30, 31…).
- **O campo Descrição tem rótulos diferentes entre as telas:** "Descrição*"; "Informações adicionais*".
- Limitação: textos montados em JavaScript ou vindos do servidor em tempo de execução não aparecem aqui; campos que só aparecem sob condição (`@if`, por exemplo a Capacidade só em Ar-condicionados) estão listados como os demais, sem marca de condição.

## 26 · Apartamentos

Tela: `Views/AdminApartments/CreateApartments.cshtml` · título da página: "Criar Anúncio - Apartamentos"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Título\* | `Title` | Exemplo: Mesa de jantar redonda | Sugerimos incluir o tipo de imóvel, bairro e número de quartos. Ex.: Apartamento em Ipanema, 2 quartos 0/90 |
| 2 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 3 | Tipo\* | `PropertyTypeId` | — | — |
| 4 | Quer vender ou alugar?\* | `TransactionTypeId` | — | — |
| 5 | Número de quartos\* | `Bedrooms` | — | — |
| 6 | Número de banheiros | `Bathrooms` | — | — |
| 7 | Área (m²) | `AreaM2` | 0,00 | — |
| 8 | Vagas na garagem | `ParkingSpaces` | — | — |
| 9 | Condomínio (R$) | `CondoFee` | — | — |
| 10 | IPTU (R$) | `PropertyTax` | — | — |
| 11 | Detalhes do imóvel | — | — | — |
| 12 | Detalhes do condomínio | — | — | — |
| 13 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | Utilize esse campo para descrever características que não foram informadas nos campos acima, como: unidades por andar, número do andar, itens de lazer disponíveis no condomínio, estado do imóvel, área do terreno e etc 0/6000 |
| 14 | CEP da localização do veículo\* | `ZipCode` | 00000-000 | — |
| 15 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 27 · Casas

Tela: `Views/AdminHouses/CreateHouses.cshtml` · título da página: "Criar Anúncio - Casas"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Título\* | `Title` | Exemplo: Mesa de jantar redonda | Sugerimos incluir o tipo de imóvel, bairro e número de quartos. Ex.: Casa em Ipanema, 2 quartos 0/90 |
| 2 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 3 | Tipo\* | `PropertyTypeId` | — | — |
| 4 | Quer vender ou alugar?\* | `TransactionTypeId` | — | — |
| 5 | Número de quartos\* | `Bedrooms` | — | — |
| 6 | Número de banheiros | `Bathrooms` | — | — |
| 7 | Área (m²) | `AreaM2` | 0,00 | — |
| 8 | Vagas na garagem | `ParkingSpaces` | — | — |
| 9 | Condomínio (R$) | `CondoFee` | — | — |
| 10 | IPTU (R$) | `PropertyTax` | — | — |
| 11 | Detalhes do imóvel | — | — | — |
| 12 | Detalhes do condomínio | — | — | — |
| 13 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | Utilize esse campo para descrever características que não foram informadas nos campos acima, como: unidades por andar, número do andar, itens de lazer disponíveis no condomínio, estado do imóvel, área do terreno e etc 0/6000 |
| 14 | CEP da localização do veículo\* | `ZipCode` | 00000-000 | — |
| 15 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 28 · Aluguel de quartos

Tela: `Views/AdminRooms/CreateRooms.cshtml` · título da página: "Criar Anúncio - Aluguel de quartos"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Título\* | `Title` | Exemplo: Mesa de jantar redonda | O título deve resumir o conteúdo do anúncio. Ex: Quarto para moças em Copacabana 0/90 |
| 2 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 3 | Detalhes do imóvel | — | — | — |
| 4 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | Inclua características do quarto, como ar condicionado, frente ou fundos, silencioso, armários, etc. 0/6000 |
| 5 | CEP da localização do veículo\* | `ZipCode` | 00000-000 | — |
| 6 | Defina o preço do seu anúncio\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 29 · Temporada

Tela: `Views/AdminSeasonal/CreateSeasonal.cshtml` · título da página: "Criar Anúncio - Temporada"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Título\* | `Title` | Exemplo: Mesa de jantar redonda | Sugerimos incluir o tipo de imóvel, bairro e número de quartos. Ex.: Apartamento temporada em Ipanema, 2 quartos 0/90 |
| 2 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 3 | Tipo\* | `PropertyTypeId` | — | — |
| 4 | Detalhes | — | — | — |
| 5 | Número de quartos\* | `Bedrooms` | — | — |
| 6 | Acomoda\* | `Accommodates` | — | — |
| 7 | Número de banheiros | `Bathrooms` | — | — |
| 8 | Vagas na garagem | `ParkingSpaces` | — | — |
| 9 | Tipo de pagamento | `PaymentTypeId` | — | — |
| 10 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | Inclua características do imóvel, como ar condicionado, armários, número de banheiros, vagas, piscina, quadra, varanda, churrasqueira, localização (pontos de referência) 0/6000 |
| 11 | CEP da localização do veículo\* | `ZipCode` | 00000-000 | — |
| 12 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 30 · Terrenos, sítios e fazendas

Tela: `Views/AdminLand/CreateLand.cshtml` · título da página: "Criar Anúncio - Terrenos, sítios e fazendas"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Título\* | `Title` | Exemplo: Mesa de jantar redonda | Sugerimos incluir o tipo de imóvel e metragem. Ex.: Terreno 2.000m2 0/90 |
| 2 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 3 | Quer vender ou alugar?\* | `TransactionTypeId` | — | — |
| 4 | Área (m²) | `AreaM2` | 0,00 | — |
| 5 | Tipo\* | `PropertyTypeId` | — | — |
| 6 | Condomínio (R$) | `CondoFee` | — | — |
| 7 | IPTU (R$) | `PropertyTax` | — | — |
| 8 | Detalhes do imóvel | — | — | — |
| 9 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | 0/6000 |
| 10 | CEP da localização do veículo\* | `ZipCode` | 00000-000 | — |
| 11 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 31 · Comércio e indústria

Tela: `Views/AdminCommercial/CreateCommercial.cshtml` · título da página: "Criar Anúncio - Comércio e indústria"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Título\* | `Title` | Exemplo: Mesa de jantar redonda | Sugerimos incluir o tipo de imóvel, bairro e metragem. Ex.: Loja em Ipanema, 200m2 0/90 |
| 2 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 3 | Quer vender ou alugar?\* | `TransactionTypeId` | — | — |
| 4 | Tipo\* | `PropertyTypeId` | — | — |
| 5 | Área (m²) | `AreaM2` | 0,00 | — |
| 6 | Vagas na garagem | `ParkingSpaces` | — | — |
| 7 | Condomínio (R$) | `CondoFee` | — | — |
| 8 | IPTU (R$) | `PropertyTax` | — | — |
| 9 | Detalhes do imóvel | — | — | — |
| 10 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | Inclua características do imóvel, como ar condicionado, estado de conservação, etc 0/6000 |
| 11 | CEP da localização do veículo\* | `ZipCode` | 00000-000 | — |
| 12 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 33 · Carros, vans e utilitários

Tela: `Views/AdminCarsVansUtilityVehicles/CreateCarsVansUtilityVehicles.cshtml` · título da página: "Criar Anúncio - Carros / Vans / Utilitários"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Titulo\* | `Title` | Entre com Titulo | — |
| 2 | Descrição\* | `Description` | Entre com Descrição | — |
| 3 | Marca\* | `CarBrandId` | — | — |
| 4 | Modelo\* | `CarModelId` | — | — |
| 5 | Ano\* | `CarYearModelId` | — | — |
| 6 | Versão\* | `CarVersionId` | — | — |
| 7 | Itens de série e opcionais do seu veículo | `OptionalItemIds` | — | — |
| 8 | Informações adicionais | `AdditionalInfoIds` | — | — |
| 9 | Fotos do Anúncio | — | — | — |
| 10 | CEP da localização do veículo\* | `ZipCode` | 00000-000 | — |
| 11 | Defina o preço do seu veículo\* | `Price` | — | — |
| 12 | Quilometragem\* | `Mileage` | 0 | Ex: 50.800 km |
| 13 | Câmbio | `CarTransmissionId` | — | — |
| 14 | Portas | `CarDoorId` | — | — |
| 15 | Combustível | `CarFuelId` | — | — |
| 16 | Direção | `CarSteeringGearId` | — | — |
| 17 | Tipo do veículo | `VehicleTypeId` | — | — |
| 18 | Potência do motor | `CarEnginePowerId` | — | — |
| 19 | Cor | `CarColorId` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 36 · Motos

Tela: `Views/AdminMotorcycles/CreateMotorcycles.cshtml` · título da página: "Criar Anúncio - Motos"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Titulo\* | `Title` | Entre com Titulo | — |
| 2 | Descrição\* | `Description` | Entre com Descrição | — |
| 3 | Marca\* | `CarBrandId` | — | — |
| 4 | Modelo\* | `CarModelId` | — | — |
| 5 | Ano\* | `CarYearModelId` | — | — |
| 6 | Versão\* | `CarVersionId` | — | — |
| 7 | Quilometragem\* | `Mileage` | 0 | Ex: 50.800 km |
| 8 | Cilindradas\* | `DisplacementId` | — | — |
| 9 | Cor | `CarColorId` | — | — |
| 10 | Itens de série e opcionais do seu veículo | `OptionalItemIds` | — | — |
| 11 | Informações adicionais | `AdditionalInfoIds` | — | Selecione os itens que representam detalhes do seu veículo para atrair a atenção dos compradores |
| 12 | Link do vídeo no YouTube | `VideoYouTubeId` | https://www.youtube.com/watch?v=... | — |
| 13 | Fotos do Anúncio | — | — | — |
| 14 | CEP da localização do veículo\* | `ZipCode` | 00000-000 | — |
| 15 | Defina o preço do seu veículo\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 35 · Ônibus

Tela: `Views/AdminBuses/CreateBuses.cshtml` · título da página: "Criar Anúncio - Ônibus"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Titulo\* | `Title` | Entre com Titulo | — |
| 2 | Descrição\* | `Description` | Entre com Descrição | — |
| 3 | Quilometragem\* | `Mileage` | 0 | Ex: 50.800 km |
| 4 | Ano do modelo\* | `CarYearModelId` | — | — |
| 5 | Câmbio | `CarTransmissionId` | — | — |
| 6 | Combustível | `CarFuelId` | — | — |
| 7 | Direção | `CarSteeringGearId` | — | — |
| 8 | @Model.TypeLabel | `VehicleTypeId` | — | — |
| 9 | Itens de série e opcionais do seu veículo | `OptionalItemIds` | — | — |
| 10 | Informações adicionais | `AdditionalInfoIds` | — | Selecione os itens que representam detalhes do seu veículo para atrair a atenção dos compradores |
| 11 | Link do vídeo no YouTube | `VideoYouTubeId` | https://www.youtube.com/watch?v=... | — |
| 12 | Fotos do Anúncio\* | — | — | — |
| 13 | CEP da localização do veículo\* | `ZipCode` | 00000-000 | — |
| 14 | Defina o preço do seu veículo\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 34 · Caminhões

Tela: `Views/AdminTrucks/CreateTrucks.cshtml` · título da página: "Criar Anúncio - Caminhões"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Titulo\* | `Title` | Entre com Titulo | — |
| 2 | Descrição\* | `Description` | Entre com Descrição | — |
| 3 | Quilometragem\* | `Mileage` | 0 | Ex: 50.800 km |
| 4 | Ano do modelo\* | `CarYearModelId` | — | — |
| 5 | Câmbio | `CarTransmissionId` | — | — |
| 6 | Combustível | `CarFuelId` | — | — |
| 7 | Direção | `CarSteeringGearId` | — | — |
| 8 | @Model.TypeLabel | `VehicleTypeId` | — | — |
| 9 | Itens de série e opcionais do seu veículo | `OptionalItemIds` | — | — |
| 10 | Informações adicionais | `AdditionalInfoIds` | — | Selecione os itens que representam detalhes do seu veículo para atrair a atenção dos compradores |
| 11 | Link do vídeo no YouTube | `VideoYouTubeId` | https://www.youtube.com/watch?v=... | — |
| 12 | Fotos do Anúncio\* | — | — | — |
| 13 | CEP da localização do veículo\* | `ZipCode` | 00000-000 | — |
| 14 | Defina o preço do seu veículo\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 37 · Barcos e aeronaves

Tela: `Views/AdminBoats/CreateBoats.cshtml` · título da página: "Criar Anúncio - Barcos e aeronaves"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Fotos do Anúncio\* | — | — | — |
| 2 | Quilometragem\* | `Mileage` | 0 | Ex: 50.800 km |
| 3 | Ano do modelo\* | `CarYearModelId` | — | — |
| 4 | Combustível | `CarFuelId` | — | — |
| 5 | Tipo\* | `VehicleTypeId` | — | — |
| 6 | Comprimento (em metros) | `LengthMeters` | 0,00 | — |
| 7 | Largura (em metros) | `WidthMeters` | 0,00 | — |
| 8 | Altura (em metros) | `HeightMeters` | 0,00 | — |
| 9 | Informações adicionais | `AdditionalInfoIds` | — | — |
| 10 | Link do vídeo no YouTube | `VideoYouTubeId` | https://www.youtube.com/watch?v=... | — |
| 11 | Titulo\* | `Title` | Entre com Titulo | — |
| 12 | Descrição\* | `Description` | Entre com Descrição | — |
| 13 | CEP da localização do veículo\* | `ZipCode` | 00000-000 | — |
| 14 | Defina o preço do seu veículo\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 38 · Peças para carros, vans e utilitários

Tela: `Views/AdminCarParts/CreateCarParts.cshtml` · título da página: "Criar Anúncio - Peças para carros, vans e utilitários"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 2 | Condição\* | `ConditionId` | — | — |
| 3 | Tipo | `PartTypeId` | — | — |
| 4 | Cor | `PartColorId` | — | — |
| 5 | Título\* | `Title` | Ex.: Motor de Fiat Uno 1.6 | Descreva a peça para que fique clara sua utilidade. 0/90 |
| 6 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | 0/6000 |
| 7 | CEP da localização do veículo\* | `ZipCode` | 00000-000 | — |
| 8 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 40 · Peças para motos

Tela: `Views/AdminMotorcycleParts/CreateMotorcycleParts.cshtml` · título da página: "Criar Anúncio - Peças para motos"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 2 | Condição\* | `ConditionId` | — | — |
| 3 | Tipo | `PartTypeId` | — | — |
| 4 | Cor | `PartColorId` | — | — |
| 5 | Título\* | `Title` | Ex.: Motor de Fiat Uno 1.6 | Descreva a peça para que fique clara sua utilidade. 0/90 |
| 6 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | 0/6000 |
| 7 | CEP da localização do veículo\* | `ZipCode` | 00000-000 | — |
| 8 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 42 · Peças para ônibus

Tela: `Views/AdminBusParts/CreateBusParts.cshtml` · título da página: "Criar Anúncio - Peças para ônibus"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 2 | Condição\* | `ConditionId` | — | — |
| 3 | Tipo | `PartTypeId` | — | — |
| 4 | Cor | `PartColorId` | — | — |
| 5 | Título\* | `Title` | Ex.: Motor de Fiat Uno 1.6 | Descreva a peça para que fique clara sua utilidade. 0/90 |
| 6 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | 0/6000 |
| 7 | CEP da localização do veículo\* | `ZipCode` | 00000-000 | — |
| 8 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 39 · Peças para caminhões

Tela: `Views/AdminTruckParts/CreateTruckParts.cshtml` · título da página: "Criar Anúncio - Peças para caminhões"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 2 | Condição\* | `ConditionId` | — | — |
| 3 | Tipo | `PartTypeId` | — | — |
| 4 | Cor | `PartColorId` | — | — |
| 5 | Título\* | `Title` | Ex.: Motor de Fiat Uno 1.6 | Descreva a peça para que fique clara sua utilidade. 0/90 |
| 6 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | 0/6000 |
| 7 | CEP da localização do veículo\* | `ZipCode` | 00000-000 | — |
| 8 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 41 · Peças para barcos e aeronaves

Tela: `Views/AdminBoatParts/CreateBoatParts.cshtml` · título da página: "Criar Anúncio - Peças para barcos e aeronaves"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 2 | Condição\* | `ConditionId` | — | — |
| 3 | Tipo | `PartTypeId` | — | — |
| 4 | Cor | `PartColorId` | — | — |
| 5 | Título\* | `Title` | Ex.: Motor de Fiat Uno 1.6 | Descreva a peça para que fique clara sua utilidade. 0/90 |
| 6 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | 0/6000 |
| 7 | CEP da localização do veículo\* | `ZipCode` | 00000-000 | — |
| 8 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 43 · Celulares e Smartphones

Tela: `Views/AdminPhones/CreatePhones.cshtml` · título da página: "Criar Anúncio - Celulares e Smartphones"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Título\* | `Title` | Exemplo: Mesa de jantar redonda | Sugerimos incluir o modelo, versão, estado e operadora. Ex.: iPhone 4 VIVO sem nenhum arranhão 0/90 |
| 2 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 3 | Marca\* | `BrandId` | — | — |
| 4 | Modelo\* | `ModelName` | Ex.: iPhone 13 Pro 128GB | Escreva o modelo como ele é conhecido, sem abreviar. 0/60 |
| 5 | Condição\* | `ConditionId` | — | — |
| 6 | Memória interna | `StorageId` | — | — |
| 7 | Cor | `ColorId` | — | — |
| 8 | Saúde da bateria | `BatteryHealthId` | — | — |
| 9 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | Inclua características como estado de conservação, ano de fabricação/versão, voltagem, defeitos, acessórios, etc 0/6000 |
| 10 | CEP da localização do produto\* | `ZipCode` | 00000-000 | — |
| 11 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 44 · Acessórios de Celular

Tela: `Views/AdminPhoneAccessories/CreatePhoneAccessories.cshtml` · título da página: "Criar Anúncio - Acessórios de Celular"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Título\* | `Title` | Exemplo: Mesa de jantar redonda | Sugerimos incluir o modelo, versão, estado e operadora. Ex.: iPhone 4 VIVO sem nenhum arranhão 0/90 |
| 2 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 3 | Tipo\* | `ProductTypeId` | — | — |
| 4 | Condição\* | `ConditionId` | — | — |
| 5 | Marca | — | — | — |
| 6 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | Inclua características como estado de conservação, ano de fabricação/versão, voltagem, defeitos, acessórios, etc 0/6000 |
| 7 | CEP da localização do produto\* | `ZipCode` | 00000-000 | — |
| 8 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 45 · Peças de Celular

Tela: `Views/AdminPhoneParts/CreatePhoneParts.cshtml` · título da página: "Criar Anúncio - Peças de Celular"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Título\* | `Title` | Exemplo: Mesa de jantar redonda | Sugerimos incluir o modelo, versão, estado e operadora. Ex.: iPhone 4 VIVO sem nenhum arranhão 0/90 |
| 2 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 3 | Tipo\* | `ProductTypeId` | — | — |
| 4 | Condição\* | `ConditionId` | — | — |
| 5 | Marca | — | — | — |
| 6 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | Inclua características como estado de conservação, ano de fabricação/versão, voltagem, defeitos, acessórios, etc 0/6000 |
| 7 | CEP da localização do produto\* | `ZipCode` | 00000-000 | — |
| 8 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 46 · Smartwatches

Tela: `Views/AdminSmartwatches/CreateSmartwatches.cshtml` · título da página: "Criar Anúncio - Smartwatches"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Título\* | `Title` | Exemplo: Mesa de jantar redonda | Sugerimos incluir o modelo, versão, estado e operadora. Ex.: iPhone 4 VIVO sem nenhum arranhão 0/90 |
| 2 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 3 | Marca\* | `BrandId` | — | — |
| 4 | Condição\* | `ConditionId` | — | — |
| 5 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | Inclua características como estado de conservação, ano de fabricação/versão, voltagem, defeitos, acessórios, etc 0/6000 |
| 6 | CEP da localização do produto\* | `ZipCode` | 00000-000 | — |
| 7 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 47 · Acessórios Para Smartwatch

Tela: `Views/AdminSmartwatchAccessories/CreateSmartwatchAccessories.cshtml` · título da página: "Criar Anúncio - Acessórios para Smartwatch"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Título\* | `Title` | Exemplo: Mesa de jantar redonda | Sugerimos incluir o modelo, versão, estado e operadora. Ex.: iPhone 4 VIVO sem nenhum arranhão 0/90 |
| 2 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 3 | Tipo\* | `ProductTypeId` | — | — |
| 4 | Condição\* | `ConditionId` | — | — |
| 5 | Marca | — | — | — |
| 6 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | Inclua características como estado de conservação, ano de fabricação/versão, voltagem, defeitos, acessórios, etc 0/6000 |
| 7 | CEP da localização do produto\* | `ZipCode` | 00000-000 | — |
| 8 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 48 · Telefonia Fixa e Sem Fio

Tela: `Views/AdminLandlinePhones/CreateLandlinePhones.cshtml` · título da página: "Criar Anúncio - Telefonia Fixa e Sem Fio"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Título\* | `Title` | Exemplo: Mesa de jantar redonda | Sugerimos incluir o modelo, versão, estado e operadora. Ex.: iPhone 4 VIVO sem nenhum arranhão 0/90 |
| 2 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 3 | Tipo\* | `ProductTypeId` | — | — |
| 4 | Condição\* | `ConditionId` | — | — |
| 5 | Marca | — | — | — |
| 6 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | Inclua características como estado de conservação, ano de fabricação/versão, voltagem, defeitos, acessórios, etc 0/6000 |
| 7 | CEP da localização do produto\* | `ZipCode` | 00000-000 | — |
| 8 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 128 · Ar-condicionados

Tela: `Views/AdminAirConditioners/CreateAirConditioners.cshtml` · título da página: "Criar Anúncio - Ar-condicionados"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Título\* | `Title` | Exemplo: Mesa de jantar redonda | Sugerimos incluir o modelo, versão, estado e operadora. Ex.: iPhone 4 VIVO sem nenhum arranhão 0/90 |
| 2 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 3 | Tipo\* | `ProductTypeId` | — | — |
| 4 | Marca\* | `BrandId` | — | — |
| 5 | Voltagem\* | `VoltageId` | — | — |
| 6 | Potência | `CapacityId` | — | — |
| 7 | Condição\* | `ConditionId` | — | — |
| 8 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | Inclua características como estado de conservação, ano de fabricação/versão, voltagem, defeitos, acessórios, etc 0/6000 |
| 9 | CEP da localização do produto\* | `ZipCode` | 00000-000 | — |
| 10 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 129 · Ventiladores e Climatizadores

Tela: `Views/AdminFans/CreateFans.cshtml` · título da página: "Criar Anúncio - Ventiladores e Climatizadores"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Título\* | `Title` | Exemplo: Mesa de jantar redonda | Sugerimos incluir o modelo, versão, estado e operadora. Ex.: iPhone 4 VIVO sem nenhum arranhão 0/90 |
| 2 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 3 | Tipo\* | `ProductTypeId` | — | — |
| 4 | Marca\* | `BrandId` | — | — |
| 5 | Voltagem\* | `VoltageId` | — | — |
| 6 | Potência | `CapacityId` | — | — |
| 7 | Condição\* | `ConditionId` | — | — |
| 8 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | Inclua características como estado de conservação, ano de fabricação/versão, voltagem, defeitos, acessórios, etc 0/6000 |
| 9 | CEP da localização do produto\* | `ZipCode` | 00000-000 | — |
| 10 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 130 · Geladeiras e Freezers

Tela: `Views/AdminRefrigerators/CreateRefrigerators.cshtml` · título da página: "Criar Anúncio - Geladeiras e Freezers"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Título\* | `Title` | Exemplo: Mesa de jantar redonda | Sugerimos incluir o modelo, versão, estado e operadora. Ex.: iPhone 4 VIVO sem nenhum arranhão 0/90 |
| 2 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 3 | Tipo\* | `ProductTypeId` | — | — |
| 4 | Marca\* | `BrandId` | — | — |
| 5 | Voltagem\* | `VoltageId` | — | — |
| 6 | Potência | `CapacityId` | — | — |
| 7 | Condição\* | `ConditionId` | — | — |
| 8 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | Inclua características como estado de conservação, ano de fabricação/versão, voltagem, defeitos, acessórios, etc 0/6000 |
| 9 | CEP da localização do produto\* | `ZipCode` | 00000-000 | — |
| 10 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 131 · Fogões e Fornos

Tela: `Views/AdminStoves/CreateStoves.cshtml` · título da página: "Criar Anúncio - Fogões e Fornos"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Título\* | `Title` | Exemplo: Mesa de jantar redonda | Sugerimos incluir o modelo, versão, estado e operadora. Ex.: iPhone 4 VIVO sem nenhum arranhão 0/90 |
| 2 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 3 | Tipo\* | `ProductTypeId` | — | — |
| 4 | Marca\* | `BrandId` | — | — |
| 5 | Voltagem\* | `VoltageId` | — | — |
| 6 | Potência | `CapacityId` | — | — |
| 7 | Condição\* | `ConditionId` | — | — |
| 8 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | Inclua características como estado de conservação, ano de fabricação/versão, voltagem, defeitos, acessórios, etc 0/6000 |
| 9 | CEP da localização do produto\* | `ZipCode` | 00000-000 | — |
| 10 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 132 · Máquinas de Lavar e Secadoras

Tela: `Views/AdminWashers/CreateWashers.cshtml` · título da página: "Criar Anúncio - Máquinas de Lavar e Secadoras"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Título\* | `Title` | Exemplo: Mesa de jantar redonda | Sugerimos incluir o modelo, versão, estado e operadora. Ex.: iPhone 4 VIVO sem nenhum arranhão 0/90 |
| 2 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 3 | Tipo\* | `ProductTypeId` | — | — |
| 4 | Marca\* | `BrandId` | — | — |
| 5 | Voltagem\* | `VoltageId` | — | — |
| 6 | Potência | `CapacityId` | — | — |
| 7 | Condição\* | `ConditionId` | — | — |
| 8 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | Inclua características como estado de conservação, ano de fabricação/versão, voltagem, defeitos, acessórios, etc 0/6000 |
| 9 | CEP da localização do produto\* | `ZipCode` | 00000-000 | — |
| 10 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 133 · Eletroportáteis Para Cozinha e Limpeza

Tela: `Views/AdminKitchenAppliances/CreateKitchenAppliances.cshtml` · título da página: "Criar Anúncio - Eletroportáteis Para Cozinha e Limpeza"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Título\* | `Title` | Exemplo: Mesa de jantar redonda | Sugerimos incluir o modelo, versão, estado e operadora. Ex.: iPhone 4 VIVO sem nenhum arranhão 0/90 |
| 2 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 3 | Tipo\* | `ProductTypeId` | — | — |
| 4 | Marca\* | `BrandId` | — | — |
| 5 | Voltagem\* | `VoltageId` | — | — |
| 6 | Potência | `CapacityId` | — | — |
| 7 | Condição\* | `ConditionId` | — | — |
| 8 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | Inclua características como estado de conservação, ano de fabricação/versão, voltagem, defeitos, acessórios, etc 0/6000 |
| 9 | CEP da localização do produto\* | `ZipCode` | 00000-000 | — |
| 10 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

## 134 · Eletroportáteis Para Cuidados Pessoais

Tela: `Views/AdminPersonalCareAppliances/CreatePersonalCareAppliances.cshtml` · título da página: "Criar Anúncio - Eletroportáteis Para Cuidados Pessoais"

| # | Rótulo na tela | Campo (`name`) | Placeholder | Texto de ajuda |
|---|---|---|---|---|
| 1 | Título\* | `Title` | Exemplo: Mesa de jantar redonda | Sugerimos incluir o modelo, versão, estado e operadora. Ex.: iPhone 4 VIVO sem nenhum arranhão 0/90 |
| 2 | Fotos do Anúncio\* | — | — | Até 20 fotos nos formatos JPG, GIF, PNG ou WEBP. |
| 3 | Tipo\* | `ProductTypeId` | — | — |
| 4 | Marca\* | `BrandId` | — | — |
| 5 | Voltagem\* | `VoltageId` | — | — |
| 6 | Potência | `CapacityId` | — | — |
| 7 | Condição\* | `ConditionId` | — | — |
| 8 | Informações adicionais\* | `Description` | Escreva aqui por quê você está anunciando esse item e informações adicionais que podem ajudar na venda. | Inclua características como estado de conservação, ano de fabricação/versão, voltagem, defeitos, acessórios, etc 0/6000 |
| 9 | CEP da localização do produto\* | `ZipCode` | 00000-000 | — |
| 10 | Defina o preço do seu produto\* | `Price` | — | Your changes will be saved as draft/pending and require publisher approval. |

