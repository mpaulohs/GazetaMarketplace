# Inventário de categorias ativas (IsPostable = 1)

> **Em resumo:** lista das categorias em que se pode publicar anúncio, agrupadas sob as categorias-pai que só servem de agrupamento. É a base para mapear, nas próximas etapas, os campos de formulário de cada categoria. Documento de descoberta, gerado só por leitura.
>
> **Fonte:** `specs/categories.md` (adicionado pelo Product Owner em 2026-09-30; conteúdo idêntico ao export anexado antes). **Regra:** categoria ativa (postável) = `IsPostable = 1`; `IsPostable = 0` = só agrupamento.

## Resumo

| Indicador | Valor |
|---|---|
| Linhas no arquivo de origem | 152 |
| Categorias-pai de nível 1 (`ParentCategoryId = NULL`) | 22 |
| Agrupamentos intermediários (`IsPostable = 0` com pai) | 1 (3 Autopeças) |
| Categorias ativas (`IsPostable = 1`) | 129 |
| Categorias inativas / agrupamentos (`IsPostable = 0`) | 23 |
| Categorias-pai sem nenhuma subcategoria ativa | 0 |
| Categorias ativas com `Slug` preenchido | 29 de 129 |

## Achados para revisão

- **Contagem:** o arquivo tem **152** linhas, não 155. O maior `CategoryId` é 155, mas faltam os IDs **24, 25, 32**.
- **Hierarquia com 3 níveis:** "Autopeças" (3) é agrupamento dentro de outra categoria-pai. As subcategorias dele são postáveis e ficam três níveis abaixo da raiz. O SPEC atual (S3) supõe só 2 níveis.
- **Slug ausente:** 100 das 129 categorias ativas têm `Slug = NULL`. Sem slug não há endereço legível (NFR-21, SEO), então será preciso gerar os que faltam.
- **Slug fora do padrão:** "Carros, vans e utilitários" (33) tem o slug **`cars`**, em inglês. Os demais estão em português e sem acento (`caminhoes`, `motos`).
- **`DisplayOrder`:** sem repetição dentro do mesmo pai. Em "Automóveis, Peças e Acessórios" a ordem não segue o `CategoryId` (Motos 36 = 2, Ônibus 35 = 3, Caminhões 34 = 4).
- **Nome repetido entre pai e filha:** "Serviços" (7 → filha 66); "Vagas de emprego" (13 → filha 96). O pai tem uma única filha postável com o mesmo nome.
- **Integridade:** 0 categorias apontam para um pai inexistente; 0 categorias ativas estão na raiz ou sob outra categoria ativa.

## Categorias ativas por hierarquia

Ordenadas pelo `DisplayOrder` de cada nível. O cabeçalho é a categoria-pai (`IsPostable = 0`); a tabela lista só as postáveis.

### 1. Imóveis (CategoryId 1)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 26 | Apartamentos | 1 | apartamentos | 1 |
| 27 | Casas | 1 | casas | 2 |
| 28 | Aluguel de quartos | 1 | aluguel-de-quartos | 3 |
| 29 | Temporada | 1 | temporada | 4 |
| 30 | Terrenos, sítios e fazendas | 1 | terrenos-sitios-e-fazendas | 5 |
| 31 | Comércio e indústria | 1 | comercio-e-industria | 6 |

### 2. Automóveis, Peças e Acessórios (CategoryId 2)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 33 | Carros, vans e utilitários | 2 | cars | 1 |
| 36 | Motos | 2 | motos | 2 |
| 35 | Ônibus | 2 | onibus | 3 |
| 34 | Caminhões | 2 | caminhoes | 4 |
| 37 | Barcos e aeronaves | 2 | barcos-e-aeronaves | 5 |

#### Automóveis, Peças e Acessórios › Autopeças (CategoryId 3, agrupamento, DisplayOrder 6)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 38 | Peças para carros, vans e utilitários | 3 | pecas-carros-vans-e-utilitarios | 1 |
| 40 | Peças para motos | 3 | pecas-motos | 2 |
| 42 | Peças para ônibus | 3 | pecas-onibus | 3 |
| 39 | Peças para caminhões | 3 | pecas-caminhoes | 4 |
| 41 | Peças para barcos e aeronaves | 3 | pecas-barcos-e-aeronaves | 5 |

### 3. Celulares e Telefonia (CategoryId 4)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 43 | Celulares e Smartphones | 4 | celulares-e-smartphones | 1 |
| 44 | Acessórios de Celular | 4 | acessorios-de-celular | 2 |
| 45 | Peças de Celular | 4 | pecas-de-celular | 3 |
| 46 | Smartwatches | 4 | smartwatches | 4 |
| 47 | Acessórios Para Smartwatch | 4 | acessorios-para-smartwatch | 5 |
| 48 | Telefonia Fixa e Sem Fio | 4 | telefonia-fixa-e-sem-fio | 6 |

### 4. Casa, Decoração e Utensílios (CategoryId 5)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 49 | Tecidos de Cama, Mesa e Banho | 5 | — | 1 |
| 50 | Decorações Para Casa | 5 | — | 2 |
| 51 | Casa Inteligente | 5 | — | 3 |
| 52 | Utensílios Para Cozinha | 5 | — | 4 |
| 53 | Utensílios Para Banheiro e Limpeza | 5 | — | 5 |
| 54 | Iluminação | 5 | — | 6 |
| 55 | Segurança Residencial | 5 | — | 7 |
| 56 | Jardinagem e Plantas | 5 | — | 8 |
| 57 | Área Externa | 5 | — | 9 |

### 5. Esportes e Fitness (CategoryId 6)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 58 | Ciclismo | 6 | — | 1 |
| 59 | Academia e Exercícios | 6 | — | 2 |
| 60 | Acampamento | 6 | — | 3 |
| 61 | Esportes Sobre Rodas | 6 | — | 4 |
| 62 | Esportes de Quadra e Ao Ar Livre | 6 | — | 5 |
| 63 | Esportes Aquáticos | 6 | — | 6 |
| 64 | Roupas Esportivas | 6 | — | 7 |
| 65 | Calçados Esportivos | 6 | — | 8 |

### 6. Serviços (CategoryId 7)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 66 | Serviços | 7 | — | 1 |

### 7. Moda e beleza (CategoryId 8)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 67 | Beleza e Cuidados Pessoais | 8 | — | 1 |
| 68 | Roupas | 8 | — | 2 |
| 69 | Calçados | 8 | — | 3 |
| 70 | Bolsas, malas e mochilas | 8 | — | 4 |
| 71 | Acessórios | 8 | — | 5 |

### 8. Artigos infantis (CategoryId 9)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 72 | Roupas Infantis | 9 | — | 1 |
| 73 | Brinquedos e Jogos | 9 | — | 2 |
| 74 | Maternidade e Cuidados com o Bebê | 9 | — | 3 |
| 75 | Calçados Infantis | 9 | — | 4 |
| 76 | Roupas para Bebês | 9 | — | 5 |
| 77 | Calçados Para Bebês | 9 | — | 6 |
| 78 | Móveis Infantis | 9 | — | 7 |

### 9. Animais de estimação (CategoryId 10)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 79 | Cachorros | 10 | — | 1 |
| 80 | Gatos | 10 | — | 2 |
| 81 | Acessórios para pets | 10 | — | 3 |
| 82 | Roedores | 10 | — | 4 |
| 83 | Outros animais | 10 | — | 5 |

### 10. Música e hobbies (CategoryId 11)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 84 | Instrumentos musicais | 11 | — | 1 |
| 85 | CDs, DVDs etc | 11 | — | 2 |
| 86 | Livros e revistas | 11 | — | 3 |
| 87 | Antiguidades | 11 | — | 4 |
| 88 | Hobbies e coleções | 11 | — | 5 |

### 11. Agro e indústria (CategoryId 12)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 89 | Tratores e máquinas agrícolas | 12 | — | 1 |
| 90 | Peças para tratores e máquinas | 12 | — | 2 |
| 91 | Animais para agropecuária | 12 | — | 3 |
| 92 | Máquinas pesadas para construção | 12 | — | 4 |
| 93 | Máquinas para produção industrial | 12 | — | 5 |
| 94 | Outros itens para agro e indústria | 12 | — | 6 |
| 95 | Produção Rural | 12 | — | 7 |

### 12. Vagas de emprego (CategoryId 13)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 96 | Vagas de emprego | 13 | — | 1 |

### 13. Comércio (CategoryId 14)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 97 | Trailers e carrinhos comerciais | 14 | — | 1 |
| 98 | Equipamentos Para Comércio | 14 | — | 2 |
| 99 | Gastronomia e Hotelaria | 14 | — | 3 |
| 100 | Equipamentos Médicos e Hospitalares | 14 | — | 4 |
| 101 | Uniformes de Trabalho e EPIs | 14 | — | 5 |

### 14. Câmeras e Drones (CategoryId 15)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 102 | Câmeras e Filmadoras | 15 | — | 1 |
| 103 | Acessórios para Câmeras e Filmadoras | 15 | — | 2 |
| 104 | Drones | 15 | — | 3 |

### 15. Games (CategoryId 16)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 105 | Consoles de Vídeo Game | 16 | — | 1 |
| 106 | Jogos de Vídeo Game | 16 | — | 2 |
| 107 | Peças e Acessórios de Vídeo Game | 16 | — | 3 |

### 16. TVs e video (CategoryId 17)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 108 | TVs | 17 | — | 1 |
| 109 | Peças e Acessórios para TV | 17 | — | 2 |
| 110 | Projetores e Telas de Projeção | 17 | — | 3 |
| 111 | DVD, Blu-Ray e Vídeo Cassete | 17 | — | 4 |
| 112 | Dispositivos de Streaming | 17 | — | 5 |

### 17. Áudio (CategoryId 18)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 113 | Fones de Ouvido | 18 | — | 1 |
| 114 | Aparelhos de Som | 18 | — | 2 |
| 115 | Microfones e Gravadores | 18 | — | 3 |
| 116 | Equipamentos e Acessórios de Som | 18 | — | 4 |

### 18. Informática (CategoryId 19)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 117 | Computadores e Desktops | 19 | — | 1 |
| 118 | Notebooks | 19 | — | 2 |
| 119 | Monitores | 19 | — | 3 |
| 120 | Periféricos e Acessórios de Computador | 19 | — | 4 |
| 121 | Peças de Hardware | 19 | — | 5 |
| 122 | Armazenamento | 19 | — | 6 |
| 123 | Memória RAM | 19 | — | 7 |
| 124 | Processadores | 19 | — | 8 |
| 125 | Placas de Vídeo | 19 | — | 9 |
| 126 | Conectividade e Dispositivos de Rede | 19 | — | 10 |
| 127 | Tablets e E-Readers | 19 | — | 11 |

### 19. Eletro (CategoryId 20)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 128 | Ar-condicionados | 20 | ar-condicionados | 1 |
| 129 | Ventiladores e Climatizadores | 20 | ventiladores-e-climatizadores | 2 |
| 130 | Geladeiras e Freezers | 20 | geladeiras-e-freezers | 3 |
| 131 | Fogões e Fornos | 20 | fogoes-e-fornos | 4 |
| 132 | Máquinas de Lavar e Secadoras | 20 | maquinas-de-lavar-e-secadoras | 5 |
| 133 | Eletroportáteis Para Cozinha e Limpeza | 20 | eletroportateis-para-cozinha-e-limpeza | 6 |
| 134 | Eletroportáteis Para Cuidados Pessoais | 20 | eletroportateis-para-cuidados-pessoais | 7 |

### 20. Móveis (CategoryId 21)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 135 | Camas e Colchões | 21 | — | 1 |
| 136 | Sofás e Poltronas | 21 | — | 2 |
| 137 | Bancos e Cadeiras | 21 | — | 3 |
| 138 | Mesas | 21 | — | 4 |
| 139 | Escrivaninhas e Penteadeiras | 21 | — | 5 |
| 140 | Racks e Painéis | 21 | — | 6 |
| 141 | Armários e Guarda-Roupas | 21 | — | 7 |
| 142 | Móveis Para Organização | 21 | — | 8 |

### 21. Materiais de Construção (CategoryId 22)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 143 | Fundação e Estrutura | 22 | — | 1 |
| 144 | Alvenaria | 22 | — | 2 |
| 145 | Pisos e Revestimentos | 22 | — | 3 |
| 146 | Portas e Janelas | 22 | — | 4 |
| 147 | Cubas e Pias | 22 | — | 5 |
| 148 | Torneiras, Duchas e Vasos | 22 | — | 6 |
| 149 | Instalações Elétricas e Hidráulicas | 22 | — | 7 |
| 150 | Ferramentas de Construção | 22 | — | 8 |
| 151 | Ferramentas de Pintura | 22 | — | 9 |

### 22. Escritório e Home Office (CategoryId 23)

| CategoryId | Name | ParentCategoryId | Slug | DisplayOrder |
|---|---|---|---|---|
| 152 | Itens Para Escritório | 23 | — | 1 |
| 153 | Cadeiras de Escritório e Gamer | 23 | — | 2 |
| 154 | Móveis de Escritório | 23 | — | 3 |
| 155 | Papelaria | 23 | — | 4 |

