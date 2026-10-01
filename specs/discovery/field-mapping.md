# Mapeamento de campos: GazetaOnline → categorias ativas do GazetaMarketplace

> ⚠️ **Superado pelo SPEC (2026-09-30):** as propostas `P-SERVICO` e `P-VAGA` deste documento foram substituídas pelas definições do Product Owner em `specs/SPEC.md` (Apêndice B, A1). Os demais grupos foram aprovados como estão (Q8). Mantido como registro da descoberta.

> **Em resumo:** para cada uma das 129 categorias em que se pode anunciar, este documento diz quais campos o formulário de anúncio deve ter. Para 29 categorias os campos **vêm do GazetaOnline** (o sistema anterior já os tinha). Para as outras 100 o GazetaOnline não tinha formulário, então os campos aqui são uma **proposta** para o Product Owner aprovar, ajustar ou recusar. Documento de descoberta, gerado só por leitura; nada no SPEC foi alterado.
>
> **Fontes:** `specs/discovery/categories-active.md` (Etapa 1), `specs/discovery/gazetaonline-fields.md` (Etapa 2) e as decisões já tomadas no `specs/SPEC.md` (CEP, preço, descrição).

## Resumo

| Origem dos campos | Categorias | O que fazer |
|---|---|---|
| **Direto do GazetaOnline** | 29 | Confirmar os ajustes da seção "Campos herdados" |
| 📝 Proposta · Eletrônicos e informática (`P-ELETRONICO`) | 26 | Aprovar, ajustar ou recusar |
| 📝 Proposta · Roupas e calçados (`P-VESTUARIO`) | 8 | Aprovar, ajustar ou recusar |
| 📝 Proposta · Produtos em geral (`P-CONDICAO`) | 55 | Aprovar, ajustar ou recusar |
| 📝 Proposta · Máquinas e equipamentos pesados (`P-MAQUINA`) | 4 | Aprovar, ajustar ou recusar |
| 📝 Proposta · Animais vivos (`P-ANIMAL`) | 5 | Aprovar, ajustar ou recusar |
| 📝 Proposta · Serviços (`P-SERVICO`) | 1 | Aprovar, ajustar ou recusar |
| 📝 Proposta · Vagas de emprego (`P-VAGA`) | 1 | Aprovar, ajustar ou recusar |
| **Total** | **129** | |

## Campos comuns (todas as 129 categorias)

Já definidos no SPEC; o GazetaOnline tinha os mesmos, com as diferenças da última coluna.

| Campo | Regra no GazetaMarketplace (SPEC) | GazetaOnline |
|---|---|---|
| Título | Obrigatório para salvar o rascunho | Obrigatório |
| Descrição | Obrigatória para enviar à revisão (S22) | Obrigatória |
| Preço | Máscara R$, em centavos, maior que zero para enviar (S27, S28) | Decimal obrigatório, **e** repetido como texto no atributo `Price` → **não repetir** |
| CEP → Cidade/UF | CEP obrigatório para enviar; Cidade/UF automáticas (Ajuste 2, S26) | CEP obrigatório; Cidade e UF gravados |
| Fotos | 1 a 20; formato e tamanho em aberto (S16: JPG/PNG/WebP até 10 MB) | 1 a 20; JPG/PNG/GIF até 5 MB; 50×50 a 3840×2160 px |

## Campos herdados do GazetaOnline (29 categorias)

Os campos, tipos e obrigatoriedade estão detalhados em `gazetaonline-fields.md`. Aqui ficam só **as decisões de adaptação** — o que muda ao trazer para o GazetaMarketplace.

| Grupo | Categorias | Manter | Adaptar ou decidir |
|---|---|---|---|
| Carros | 33 | Marca → Modelo → Ano → Versão (catálogo), Quilometragem, Câmbio, Portas, Combustível, Direção, Tipo, Potência, Cor, Opcionais, Informações adicionais | **Catálogo de veículos** vem de tabelas do banco do GazetaOnline: definir se será migrado ou substituído (ex.: FIPE). **Vídeo do YouTube**: não previsto no SPEC. Slug `cars` em inglês |
| Motos | 36 | Marca → Modelo → Ano → Versão, Quilometragem, Cilindrada, Cor, Opcionais, Informações adicionais | Mesmo catálogo e vídeo do YouTube |
| Caminhões, Ônibus | 34, 35 | Ano do modelo, Quilometragem, Câmbio, Combustível, Direção, Tipo, Opcionais, Informações adicionais | Sem marca/modelo: decidir se vale incluir marca (texto ou lista) |
| Barcos e aeronaves | 37 | Ano do modelo, Tipo, Combustível, Dimensões, Informações adicionais | **Trocar "Quilometragem" obrigatória por "Horas de uso"** (embarcação não mede km). Barco e aeronave na mesma categoria |
| Peças | 38–42 | Condição (obrigatória), Tipo de peça, Cor | — |
| Imóveis | 26–31 | Tipo, Quartos, Banheiros, Área (m²) (26, 27, 30, 31), Vagas, Condomínio, IPTU, Características | **"Vender ou alugar"**, **Aluguel de quartos** e **Temporada** são aluguel: não cabem na "venda com comissão" do SPEC. Decidir se ficam |
| Celulares | 43 | Marca, Modelo, Condição (obrigatórios), Armazenamento, Cor, Saúde da bateria | — |
| Smartwatches | 46 | Marca, Condição (obrigatórios) | — |
| Produtos de telefonia | 44, 45, 47, 48 | Tipo de produto, Condição (obrigatórios); Marcas compatíveis (44, 45) | — |
| Eletro | 128–134 | Tipo, Marca, Voltagem, Condição (obrigatórios); Capacidade (128) | — |

## 📝 Proposta de campos para as 100 categorias sem formulário no GazetaOnline

> Tudo nesta seção é **proposta**, não decisão. Cada grupo reúne categorias que se descrevem com os mesmos campos; os campos seguem os padrões que o GazetaOnline já usava (Condição, Marca, Tipo).

| Grupo | Campos propostos (✅ = obrigatório para enviar à revisão) | Por quê |
|---|---|---|
| `P-ELETRONICO` · Eletrônicos e informática | Condição ✅ (lista `ProductCondition`) · Marca ✅ (lista por categoria, como Eletro) · Modelo (texto, opcional) | Reaproveita o padrão de Eletro/Telefonia do GazetaOnline |
| `P-VESTUARIO` · Roupas e calçados | Condição ✅ · Tamanho ✅ (lista: PP a GG / numeração de calçado) · Gênero (lista, opcional) | Tamanho é o filtro que mais importa para quem compra roupa e calçado |
| `P-CONDICAO` · Produtos em geral | Condição ✅ · Tipo de produto (lista por categoria, opcional) | Mínimo útil: saber se é novo ou usado; o tipo ajuda a filtrar |
| `P-MAQUINA` · Máquinas e equipamentos pesados | Condição ✅ · Marca (lista) · Ano de fabricação (número) · Horas de uso (número) | Máquinas se avaliam por marca, ano e horas de uso, não por quilometragem |
| `P-ANIMAL` · Animais vivos | Espécie/Raça (lista) · Sexo (lista) · Idade (número + unidade) · Vacinado (sim/não) | ⚠️ Antes de definir campos: decidir se animais vivos podem ser anunciados (regras legais e de bem-estar animal) |
| `P-SERVICO` · Serviços | Tipo de serviço ✅ (lista) · Área de atendimento (texto) · Preço "a combinar" permitido? | ⚠️ Serviço não é um bem vendido com comissão; checar se cabe no modelo de intermediação |
| `P-VAGA` · Vagas de emprego | Tipo de contratação ✅ (CLT, PJ, estágio…) · Jornada (lista) · Salário (no lugar de Preço) · Requisitos (texto) | ⚠️ Vaga não tem "preço" nem vendedor; checar se cabe no modelo de intermediação |

## Matriz completa (129 categorias ativas)

Ordem da árvore de categorias (`DisplayOrder`).

### Imóveis

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 26 | Apartamentos | GazetaOnline | Imóveis (6 perfis) |
| 27 | Casas | GazetaOnline | Imóveis (6 perfis) |
| 28 | Aluguel de quartos | GazetaOnline | Imóveis (6 perfis) |
| 29 | Temporada | GazetaOnline | Imóveis (6 perfis) |
| 30 | Terrenos, sítios e fazendas | GazetaOnline | Imóveis (6 perfis) |
| 31 | Comércio e indústria | GazetaOnline | Imóveis (6 perfis) |

### Automóveis, Peças e Acessórios

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 33 | Carros, vans e utilitários | GazetaOnline | Carros (`CarsProfile`) |
| 36 | Motos | GazetaOnline | Motos (`MotorcyclesProfile`) |
| 35 | Ônibus | GazetaOnline | Caminhões/Ônibus (`TrucksProfile`/`BusesProfile`) |
| 34 | Caminhões | GazetaOnline | Caminhões/Ônibus (`TrucksProfile`/`BusesProfile`) |
| 37 | Barcos e aeronaves | GazetaOnline | Barcos (`BoatsProfile`) |
| 38 | Peças para carros, vans e utilitários | GazetaOnline | Peças (`PartsProfile`) |
| 40 | Peças para motos | GazetaOnline | Peças (`PartsProfile`) |
| 42 | Peças para ônibus | GazetaOnline | Peças (`PartsProfile`) |
| 39 | Peças para caminhões | GazetaOnline | Peças (`PartsProfile`) |
| 41 | Peças para barcos e aeronaves | GazetaOnline | Peças (`PartsProfile`) |

### Celulares e Telefonia

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 43 | Celulares e Smartphones | GazetaOnline | Celulares (`PhonesProfile`) |
| 44 | Acessórios de Celular | GazetaOnline | Produtos de telefonia (`TelephonyProductsProfile`) |
| 45 | Peças de Celular | GazetaOnline | Produtos de telefonia (`TelephonyProductsProfile`) |
| 46 | Smartwatches | GazetaOnline | Smartwatches (`SmartwatchesProfile`) |
| 47 | Acessórios Para Smartwatch | GazetaOnline | Produtos de telefonia (`TelephonyProductsProfile`) |
| 48 | Telefonia Fixa e Sem Fio | GazetaOnline | Produtos de telefonia (`TelephonyProductsProfile`) |

### Casa, Decoração e Utensílios

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 49 | Tecidos de Cama, Mesa e Banho | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 50 | Decorações Para Casa | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 51 | Casa Inteligente | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 52 | Utensílios Para Cozinha | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 53 | Utensílios Para Banheiro e Limpeza | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 54 | Iluminação | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 55 | Segurança Residencial | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 56 | Jardinagem e Plantas | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 57 | Área Externa | 📝 Proposta | `P-CONDICAO` · Produtos em geral |

### Esportes e Fitness

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 58 | Ciclismo | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 59 | Academia e Exercícios | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 60 | Acampamento | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 61 | Esportes Sobre Rodas | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 62 | Esportes de Quadra e Ao Ar Livre | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 63 | Esportes Aquáticos | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 64 | Roupas Esportivas | 📝 Proposta | `P-VESTUARIO` · Roupas e calçados |
| 65 | Calçados Esportivos | 📝 Proposta | `P-VESTUARIO` · Roupas e calçados |

### Serviços

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 66 | Serviços | 📝 Proposta | `P-SERVICO` · Serviços |

### Moda e beleza

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 67 | Beleza e Cuidados Pessoais | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 68 | Roupas | 📝 Proposta | `P-VESTUARIO` · Roupas e calçados |
| 69 | Calçados | 📝 Proposta | `P-VESTUARIO` · Roupas e calçados |
| 70 | Bolsas, malas e mochilas | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 71 | Acessórios | 📝 Proposta | `P-CONDICAO` · Produtos em geral |

### Artigos infantis

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 72 | Roupas Infantis | 📝 Proposta | `P-VESTUARIO` · Roupas e calçados |
| 73 | Brinquedos e Jogos | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 74 | Maternidade e Cuidados com o Bebê | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 75 | Calçados Infantis | 📝 Proposta | `P-VESTUARIO` · Roupas e calçados |
| 76 | Roupas para Bebês | 📝 Proposta | `P-VESTUARIO` · Roupas e calçados |
| 77 | Calçados Para Bebês | 📝 Proposta | `P-VESTUARIO` · Roupas e calçados |
| 78 | Móveis Infantis | 📝 Proposta | `P-CONDICAO` · Produtos em geral |

### Animais de estimação

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 79 | Cachorros | 📝 Proposta | `P-ANIMAL` · Animais vivos |
| 80 | Gatos | 📝 Proposta | `P-ANIMAL` · Animais vivos |
| 81 | Acessórios para pets | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 82 | Roedores | 📝 Proposta | `P-ANIMAL` · Animais vivos |
| 83 | Outros animais | 📝 Proposta | `P-ANIMAL` · Animais vivos |

### Música e hobbies

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 84 | Instrumentos musicais | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 85 | CDs, DVDs etc | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 86 | Livros e revistas | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 87 | Antiguidades | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 88 | Hobbies e coleções | 📝 Proposta | `P-CONDICAO` · Produtos em geral |

### Agro e indústria

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 89 | Tratores e máquinas agrícolas | 📝 Proposta | `P-MAQUINA` · Máquinas e equipamentos pesados |
| 90 | Peças para tratores e máquinas | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 91 | Animais para agropecuária | 📝 Proposta | `P-ANIMAL` · Animais vivos |
| 92 | Máquinas pesadas para construção | 📝 Proposta | `P-MAQUINA` · Máquinas e equipamentos pesados |
| 93 | Máquinas para produção industrial | 📝 Proposta | `P-MAQUINA` · Máquinas e equipamentos pesados |
| 94 | Outros itens para agro e indústria | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 95 | Produção Rural | 📝 Proposta | `P-CONDICAO` · Produtos em geral |

### Vagas de emprego

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 96 | Vagas de emprego | 📝 Proposta | `P-VAGA` · Vagas de emprego |

### Comércio

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 97 | Trailers e carrinhos comerciais | 📝 Proposta | `P-MAQUINA` · Máquinas e equipamentos pesados |
| 98 | Equipamentos Para Comércio | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 99 | Gastronomia e Hotelaria | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 100 | Equipamentos Médicos e Hospitalares | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 101 | Uniformes de Trabalho e EPIs | 📝 Proposta | `P-CONDICAO` · Produtos em geral |

### Câmeras e Drones

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 102 | Câmeras e Filmadoras | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 103 | Acessórios para Câmeras e Filmadoras | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 104 | Drones | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |

### Games

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 105 | Consoles de Vídeo Game | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 106 | Jogos de Vídeo Game | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 107 | Peças e Acessórios de Vídeo Game | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |

### TVs e video

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 108 | TVs | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 109 | Peças e Acessórios para TV | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 110 | Projetores e Telas de Projeção | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 111 | DVD, Blu-Ray e Vídeo Cassete | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 112 | Dispositivos de Streaming | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |

### Áudio

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 113 | Fones de Ouvido | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 114 | Aparelhos de Som | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 115 | Microfones e Gravadores | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 116 | Equipamentos e Acessórios de Som | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |

### Informática

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 117 | Computadores e Desktops | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 118 | Notebooks | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 119 | Monitores | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 120 | Periféricos e Acessórios de Computador | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 121 | Peças de Hardware | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 122 | Armazenamento | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 123 | Memória RAM | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 124 | Processadores | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 125 | Placas de Vídeo | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 126 | Conectividade e Dispositivos de Rede | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |
| 127 | Tablets e E-Readers | 📝 Proposta | `P-ELETRONICO` · Eletrônicos e informática |

### Eletro

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 128 | Ar-condicionados | GazetaOnline | Eletro (`AppliancesProfile`) |
| 129 | Ventiladores e Climatizadores | GazetaOnline | Eletro (`AppliancesProfile`) |
| 130 | Geladeiras e Freezers | GazetaOnline | Eletro (`AppliancesProfile`) |
| 131 | Fogões e Fornos | GazetaOnline | Eletro (`AppliancesProfile`) |
| 132 | Máquinas de Lavar e Secadoras | GazetaOnline | Eletro (`AppliancesProfile`) |
| 133 | Eletroportáteis Para Cozinha e Limpeza | GazetaOnline | Eletro (`AppliancesProfile`) |
| 134 | Eletroportáteis Para Cuidados Pessoais | GazetaOnline | Eletro (`AppliancesProfile`) |

### Móveis

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 135 | Camas e Colchões | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 136 | Sofás e Poltronas | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 137 | Bancos e Cadeiras | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 138 | Mesas | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 139 | Escrivaninhas e Penteadeiras | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 140 | Racks e Painéis | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 141 | Armários e Guarda-Roupas | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 142 | Móveis Para Organização | 📝 Proposta | `P-CONDICAO` · Produtos em geral |

### Materiais de Construção

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 143 | Fundação e Estrutura | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 144 | Alvenaria | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 145 | Pisos e Revestimentos | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 146 | Portas e Janelas | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 147 | Cubas e Pias | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 148 | Torneiras, Duchas e Vasos | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 149 | Instalações Elétricas e Hidráulicas | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 150 | Ferramentas de Construção | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 151 | Ferramentas de Pintura | 📝 Proposta | `P-CONDICAO` · Produtos em geral |

### Escritório e Home Office

| CategoryId | Categoria | Origem | Conjunto de campos |
|---|---|---|---|
| 152 | Itens Para Escritório | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 153 | Cadeiras de Escritório e Gamer | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 154 | Móveis de Escritório | 📝 Proposta | `P-CONDICAO` · Produtos em geral |
| 155 | Papelaria | 📝 Proposta | `P-CONDICAO` · Produtos em geral |

## Decisões pendentes para o Product Owner

| ID | Pergunta |
|---|---|
| Q1 | Animais vivos (79, 80, 82, 83, 91) podem ser anunciados? Se sim, com quais campos e regras. |
| Q2 | Serviços (66) e Vagas de emprego (96) cabem no modelo de venda intermediada com comissão? Se não, saem das categorias ativas. |
| Q3 | Imóveis para alugar ("Vender ou alugar", Aluguel de quartos 28, Temporada 29) ficam? O SPEC hoje só descreve venda. |
| Q4 | Catálogo de veículos (marca, modelo, ano e versão de carros e motos): migrar do banco do GazetaOnline, usar outra fonte (ex.: FIPE) ou começar com texto livre? |
| Q5 | Vídeo do YouTube nos veículos: manter? (não previsto no SPEC; embutir vídeo externo afeta a política de segurança do site). |
| Q6 | Barcos e aeronaves: trocar "Quilometragem" por "Horas de uso"? Separar barco de aeronave? |
| Q7 | Fotos: seguir o GazetaOnline (JPG/PNG/GIF, 5 MB) ou a suposição S16 do SPEC (JPG/PNG/WebP, 10 MB)? |
| Q8 | As 7 propostas de grupo (📝) acima: aprovar, ajustar ou recusar cada uma. |

> Depois das respostas, os campos aprovados entram no SPEC como atualização das suposições S3 (árvore de categorias) e S4 (campos por categoria), com uma linha na Revision History.
