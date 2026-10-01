# Wireframe: Página inicial e categorias — @US-001

> **Em resumo:** a página inicial mostra a busca, as categorias e os 12 anúncios mais recentes. Ao clicar em uma categoria, o visitante vê as subcategorias e os anúncios dela. Este arquivo cobre a página inicial e a página de categoria (ambas públicas, sem login). Textos entre aspas são os textos reais da tela.

**Evidência da SPEC:** `specs/SPEC.md` → US-001 (cenários S01 a S08).

## Layout — página inicial (desktop)

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ GazetaMarketplace                                                ♡ Favoritos (0) │
│ [ Buscar anúncios…                                        ]  [Buscar]            │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Categorias                                                                       │
│ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐  │
│ │ ▣               │ │ ▣               │ │ ▣               │ │ ▣               │  │
│ │ Automóveis,     │ │ Imóveis         │ │ Celulares e     │ │ Casa, Decoração │  │
│ │ Peças e         │ │                 │ │ Telefonia       │ │ e Utensílios    │  │
│ │ Acessórios      │ │                 │ │                 │ │                 │  │
│ └─────────────────┘ └─────────────────┘ └─────────────────┘ └─────────────────┘  │
│ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐  │
│ │ ▣               │ │ ▣               │ │ ▣               │ │ ▣               │  │
│ │ Moda e beleza   │ │ Eletro          │ │ Música e        │ │ Agro e          │  │
│ │                 │ │                 │ │ hobbies         │ │ indústria       │  │
│ └─────────────────┘ └─────────────────┘ └─────────────────┘ └─────────────────┘  │
│ ┌─────────────────┐ ┌─────────────────┐                                          │
│ │ ▣               │ │ ▣               │                                          │
│ │ Serviços        │ │ Vagas de        │                                          │
│ │                 │ │ emprego         │                                          │
│ └─────────────────┘ └─────────────────┘                                          │
│ … (todas as categorias principais, na ordem definida pelo Administrador)         │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Anúncios mais recentes                                                           │
│ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐  │
│ │ [ foto de capa ]│ │ [ foto de capa ]│ │ [ foto de capa ]│ │ [ foto de capa ]│  │
│ │ Honda Civic 2018│ │ Terreno 450 m²  │ │ Moto CG 160 2021│ │ Jaqueta jeans   │  │
│ │ R$ 62.000       │ │ R$ 120.000      │ │ R$ 14.500       │ │ R$ 180          │  │
│ │ Campinas/SP   ♡ │ │ Curitiba/PR   ♡ │ │ Recife/PE     ♡ │ │ Goiânia/GO    ♡ │  │
│ └─────────────────┘ └─────────────────┘ └─────────────────┘ └─────────────────┘  │
│ … (12 anúncios no total, em linhas completas de 4 colunas)                       │
├──────────────────────────────────────────────────────────────────────────────────┤
│ © GazetaMarketplace                                               Área da equipe │
└──────────────────────────────────────────────────────────────────────────────────┘
```

## Layout — página inicial (mobile, 320 px)

```text
┌──────────────────────────────────────┐
│ GazetaMarketplace              ♡ (0) │
│ [ Buscar anúncios…   ] [Buscar]      │
├──────────────────────────────────────┤
│ Categorias                           │
│ ┌───────────────┐ ┌───────────────┐  │
│ │ ▣             │ │ ▣             │  │
│ │ Automóveis,   │ │ Imóveis       │  │
│ │ Peças e       │ │               │  │
│ │ Acessórios    │ │               │  │
│ └───────────────┘ └───────────────┘  │
│ ┌───────────────┐ ┌───────────────┐  │
│ │ ▣             │ │ ▣             │  │
│ │ Celulares e   │ │ Casa,         │  │
│ │ Telefonia     │ │ Decoração e   │  │
│ │               │ │ Utensílios    │  │
│ └───────────────┘ └───────────────┘  │
│ … (demais categorias)                │
├──────────────────────────────────────┤
│ Anúncios mais recentes               │
│ ┌───────────────┐ ┌───────────────┐  │
│ │ [ foto ]      │ │ [ foto ]      │  │
│ │ Honda Civic   │ │ Terreno       │  │
│ │ 2018          │ │ 450 m²        │  │
│ │ R$ 62.000     │ │ R$ 120.000    │  │
│ │ Campinas/SP ♡ │ │ Curitiba/PR ♡ │  │
│ └───────────────┘ └───────────────┘  │
│ … (rolagem apenas para baixo)        │
├──────────────────────────────────────┤
│ Área da equipe                       │
└──────────────────────────────────────┘
```

## Layout — página de categoria (desktop)

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ GazetaMarketplace                                                ♡ Favoritos (0) │
│ [ Buscar anúncios…                                        ]  [Buscar]            │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Início > Automóveis, Peças e Acessórios                                          │
│ Automóveis, Peças e Acessórios                                                   │
│ Subcategorias:  [Carros, vans e utilitários]  [Motos]  [Ônibus]  [Caminhões]     │
│                 [Barcos e aeronaves]  [Autopeças]                                │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Anúncios em Automóveis, Peças e Acessórios                                       │
│ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐  │
│ │ [ foto de capa ]│ │ [ foto de capa ]│ │ [ foto de capa ]│ │ [ foto de capa ]│  │
│ │ Honda Civic 2018│ │ Moto CG 160 2021│ │ Fiat Strada 2019│ │ Yamaha Fazer 250│  │
│ │ R$ 62.000       │ │ R$ 14.500       │ │ R$ 58.000       │ │ R$ 17.900       │  │
│ │ Campinas/SP   ♡ │ │ Recife/PE     ♡ │ │ Manaus/AM     ♡ │ │ Salvador/BA   ♡ │  │
│ └─────────────────┘ └─────────────────┘ └─────────────────┘ └─────────────────┘  │
│ [Filtrar e ordenar os anúncios desta categoria]   ← leva à busca (US-002)        │
└──────────────────────────────────────────────────────────────────────────────────┘
```

Ao entrar em "Motos" o caminho passa a ser `Início > Automóveis, Peças e Acessórios > Motos`, sem a linha de subcategorias.

## Card do anúncio (vale para a página inicial, a categoria e a busca)

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Card do anúncio: três variantes (lista da página inicial, categoria e busca)     │
├──────────────────────────────────────────────────────────────────────────────────┤
│ ┌──────────────────────┐ ┌──────────────────────┐ ┌──────────────────────┐       │
│ │ [ foto de capa ]     │ │ [ foto de capa ]     │ │   Vaga de emprego    │       │
│ │                      │ │                      │ │                      │       │
│ │ Honda Civic 2018     │ │ Diarista com experi… │ │ Pizzaiolo com exper… │       │
│ │ R$ 62.000            │ │ Serviços domésticos  │ │ Salário R$ 2.800     │       │
│ │                      │ │                      │ │                      │       │
│ │ Campinas/SP       ♡  │ │ São Paulo/SP      ♡  │ │ Campinas/SP       ♡  │       │
│ └──────────────────────┘ └──────────────────────┘ └──────────────────────┘       │
│   padrão                   Serviços (sem preço)     Vagas (sem foto)             │
└──────────────────────────────────────────────────────────────────────────────────┘
```

- **Vagas de emprego:** sem foto, um bloco neutro com "Vaga de emprego" ocupa o lugar da capa, com a área da vaga quando houver (A4, `architecture/design-system.md` §5.2); o valor aparece como "Salário".
- **Serviços:** sem preço, o Tipo do serviço ocupa o lugar do preço (A6, `architecture/design-system.md` §5.3).
- O nome acessível do link do card segue a mesma regra: "Pizzaiolo…, Salário R$ 2.800, Campinas/SP" e "Diarista…, Tipo: Serviços domésticos, São Paulo/SP".

## Estados

| Estado | Cenário | O que o visitante vê |
|---|---|---|
| Padrão | @US-001-S01 | Busca, categorias principais e 12 anúncios recentes (capa, título, preço, cidade/UF). |
| Categoria principal | @US-001-S02 | Página "Automóveis, Peças e Acessórios" com as subcategorias e os anúncios de todas elas. |
| Subcategoria e caminho | @US-001-S03 | Só anúncios de "Motos"; caminho `Início > Automóveis, Peças e Acessórios > Motos` clicável. |
| Categoria vazia | @US-001-S04 | "Ainda não há anúncios nesta categoria" e links para as demais categorias principais. |
| Site sem anúncios | @US-001-S05 | Categorias visíveis; no lugar da lista, "Em breve teremos novos anúncios". |
| Erro | @US-001-S06 | "Não foi possível carregar a página. Tente novamente.", botão "Tentar novamente" e código de referência; nenhum detalhe técnico. |
| Categoria inexistente | @US-001-S07 | "Categoria não encontrada" e links para a página inicial e para as categorias principais. |
| Carregando | — | Blocos cinza no lugar das capas (esqueleto); a altura reservada evita que a página "pule". Sem cenário na SPEC (ver Lacunas no `README.md`). |
| Sem resultado | — | Não se aplica: a página não tem filtros; o caso "categoria sem anúncios" é o estado vazio acima. |

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Estado vazio da categoria "Serviços"                                             │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Início > Serviços                                                                │
│ Serviços                                                                         │
│ Ainda não há anúncios nesta categoria.                                           │
│ Veja outras categorias:  [Automóveis, Peças e Acessórios]  [Imóveis]             │
│                          [Celulares e Telefonia]  …                              │
└──────────────────────────────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Estado de erro da página inicial                                                 │
├──────────────────────────────────────────────────────────────────────────────────┤
│ ! Não foi possível carregar a página. Tente novamente.                           │
│   Código de referência: 7F3A-92C1                                                │
│   [Tentar novamente]                                                             │
└──────────────────────────────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Categoria inexistente                                                            │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Categoria não encontrada                                                         │
│ Voltar para a  [Página inicial]  ou escolher:                                    │
│ [Automóveis, Peças e Acessórios]  [Imóveis]  [Serviços]  …                       │
└──────────────────────────────────────────────────────────────────────────────────┘
```

## Responsivo

- **Mobile primeiro (320 px):** a busca fica sob o logotipo; categorias e anúncios em 2 colunas; a página rola só para baixo (@US-001-S08).
- **Tablet e desktop:** a grade de anúncios passa a 3 e a 4 colunas; o número de anúncios (12) sempre completa as linhas.
- **Alvos de toque:** categorias e cartões de anúncio com pelo menos 44 × 44 px.

## Acessibilidade

- Ordem de foco: link "Ir para o conteúdo" → logotipo → busca → Favoritos → categorias → anúncios → rodapé.
- Cada cartão de anúncio é um único link cujo nome acessível é "título, preço, cidade/UF"; a capa tem texto alternativo com o título.
- O caminho `Início > Automóveis, Peças e Acessórios > Motos` fica em `nav` com nome "Você está em"; o item atual usa `aria-current="page"`.
- Mensagens de erro usam `role="alert"`; "Categoria não encontrada" e o estado vazio recebem foco no título `h1`.
- Cada página tem um único `h1` ("Anúncios mais recentes" na página inicial, o nome da categoria nas demais).

## Mapeamento: controle → cenário

| Região da tela | Controle (papel / nome acessível) | Comportamento | Cenário |
|---|---|---|---|
| Cabeçalho | `searchbox "Buscar anúncios"` + `button "Buscar"` | Presentes no topo de toda página; a busca em si é descrita na US-002 | @US-001-S01 |
| Cabeçalho | `link "Favoritos (0)"` | Abre "Meus favoritos"; o número acompanha os favoritos (ref. cruzada, US-005) | @US-005-S01 |
| Categorias | `link "Automóveis, Peças e Acessórios"` (uma por categoria principal) | Abre a página da categoria com subcategorias e todos os anúncios dela | @US-001-S02 |
| Categorias | Nome de cada categoria principal em destaque | Todas as categorias principais aparecem, cada uma com seu nome | @US-001-S01 |
| Página de categoria | `link "Motos"` (subcategoria) | Mostra só anúncios da subcategoria e monta o caminho | @US-001-S03 |
| Caminho | `nav "Você está em"` → `link "Automóveis, Peças e Acessórios"` | Volta à categoria principal com anúncios de todas as subcategorias | @US-001-S03 |
| Lista recente | Grade de 12 cartões: `link "Honda Civic 2018, R$ 62.000, Campinas/SP"` | Mostra capa, título, preço e cidade/UF; o clique abre o detalhe (ref. cruzada, US-003) | @US-001-S01 |
| Lista recente | `button "Favoritar anúncio Honda Civic 2018"` (coração) | Marca ou desmarca favorito (ref. cruzada, US-005) | @US-005-S01 |
| Lista recente | Mensagem "Em breve teremos novos anúncios" | Substitui a lista quando não há anúncio publicado no site | @US-001-S05 |
| Categoria vazia | Mensagem "Ainda não há anúncios nesta categoria" + `link` de cada categoria principal | Oferece caminho para as demais categorias | @US-001-S04 |
| Erro | `alert "Não foi possível carregar a página. Tente novamente."` + `button "Tentar novamente"` + código de referência | Recarrega a página; não mostra mensagem técnica | @US-001-S06 |
| Categoria inexistente | Mensagem "Categoria não encontrada" + `link "Página inicial"` + links das categorias | Ajuda a sair do beco sem saída | @US-001-S07 |
| Página inteira | Comportamento em 320 px (sem controle próprio) | Sem rolagem horizontal; busca, categorias e anúncios se alcançam só rolando para baixo | @US-001-S08 |
| Rodapé | `link "Área da equipe"` | Leva à página de entrada da equipe (nenhum cenário da US-001 clica nele; ref. cruzada) | @US-006-S01 |
| Anúncios recentes | Card de Vaga: bloco "Vaga de emprego" + "Salário R$ 2.800" | Abre o anúncio; sem foto de capa (A4) | @US-001-S01 |
| Anúncios recentes | Card de Serviço: Tipo no lugar do preço | Abre o anúncio; sem preço | @US-001-S01 |
| Topo | `link "Ir para o conteúdo"` (oculto até receber foco) | Pula o cabeçalho | ⚠️ nenhum cenário cobre este controle |
| Cabeçalho | `link "GazetaMarketplace"` (logotipo) | Volta à página inicial | ⚠️ nenhum cenário cobre este controle |
