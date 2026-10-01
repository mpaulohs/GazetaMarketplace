# Wireframe: Busca e filtros — @US-002

> **Em resumo:** a página de busca lista os anúncios publicados, 24 por página, e deixa o visitante afinar o resultado por texto, categoria, localização, preço e características do bem (marca, ano, quilometragem ou área). Em telas estreitas os filtros ficam recolhidos atrás de um botão. Os filtros ficam no endereço da página, então uma busca pode ser compartilhada.

**Evidência da SPEC:** `specs/SPEC.md` → US-002 (cenários S01 a S12).

## Layout — desktop (exemplo: categoria "Carros, vans e utilitários" escolhida)

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ GazetaMarketplace                                                ♡ Favoritos (2) │
│ [ civic                                                   ]  [Buscar]            │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Início > Automóveis, Peças e Acessórios > Carros, vans e utilitários             │
│ ┌────────────────────────────┐ 30 anúncios encontrados      Ordenar [Recentes ▾] │
│ │ Filtros                    │ ┌──────────────────────┐ ┌──────────────────────┐ │
│ │ Categoria                  │ │ [ foto de capa ]     │ │ [ foto de capa ]     │ │
│ │ [ Carros, vans e utili… ▾] │ │ Honda Civic 2018     │ │ Honda Fit 2016       │ │
│ │ UF                         │ │ R$ 62.000            │ │ R$ 48.500            │ │
│ │ [ SP                    ▾] │ │ Campinas/SP        ♡ │ │ Campinas/SP        ♡ │ │
│ │ Cidade                     │ └──────────────────────┘ └──────────────────────┘ │
│ │ [ Campinas              ▾] │ ┌──────────────────────┐ ┌──────────────────────┐ │
│ │ Preço (R$)                 │ │ [ foto de capa ]     │ │ [ foto de capa ]     │ │
│ │ [ mín      ] a [ 50000  ]  │ │ Honda City 2017      │ │ Honda HR-V 2019      │ │
│ │ Marca *só veículos*        │ │ R$ 55.000            │ │ R$ 49.900            │ │
│ │ [ Honda                 ▾] │ │ Campinas/SP        ♡ │ │ Campinas/SP        ♡ │ │
│ │ Modelo                     │ └──────────────────────┘ └──────────────────────┘ │
│ │ [ Civic                  ] │ … (24 anúncios por página)                        │
│ │ Ano                        │                                                   │
│ │ [ 2015 ] até [ 2020 ]      │ ◀ Anterior   [1]  2   Próxima ▶                   │
│ │ Quilometragem máx. (km)    │                                                   │
│ │ [ 100000                 ] │                                                   │
│ │ [Aplicar filtros]          │                                                   │
│ │ [Limpar filtros]           │                                                   │
│ └────────────────────────────┘                                                   │
└──────────────────────────────────────────────────────────────────────────────────┘
```

A ordem dos campos de filtro segue a ordem de leitura: categoria, local, preço, características do bem. Marca, modelo, ano e quilometragem só aparecem quando a categoria escolhida é de Automóveis, Peças e Acessórios; "Área (m²)" mínima e máxima só aparece em "Terrenos, sítios e fazendas".

## Layout — mobile (320 px), filtros recolhidos e abertos

```text
┌──────────────────────────────────────┐
│ GazetaMarketplace              ♡ (2) │
│ [ civic             ] [Buscar]       │
│ [ Filtros ▾ ]  Ordenar [Recentes ▾]  │
├──────────────────────────────────────┤
│ 30 anúncios encontrados              │
│ ┌───────────────┐ ┌───────────────┐  │
│ │ [ foto ]      │ │ [ foto ]      │  │
│ │ Honda Civic   │ │ Honda Fit     │  │
│ │ 2018          │ │ 2016          │  │
│ │ R$ 62.000     │ │ R$ 48.500     │  │
│ │ Campinas/SP ♡ │ │ Campinas/SP ♡ │  │
│ └───────────────┘ └───────────────┘  │
│ … (rolagem apenas para baixo)        │
│ ◀  1 2 3…  Próxima ▶                 │
└──────────────────────────────────────┘
```

```text
┌──────────────────────────────────────┐
│ Filtros                 [ Fechar ▴ ] │
├──────────────────────────────────────┤
│ Categoria                            │
│ [ Carros, vans e utilitários ▾]      │
│ UF                                   │
│ [ SP                         ▾]      │
│ Cidade                               │
│ [ Campinas                   ▾]      │
│ Preço mín. (R$)   Preço máx. (R$)    │
│ [ 0         ]     [ 50000     ]      │
│ Marca                                │
│ [ Honda                      ▾]      │
│ Ano de      até                      │
│ [ 2015 ]    [ 2020 ]                 │
│ Quilometragem máx. (km)              │
│ [ 100000                      ]      │
│ [Aplicar filtros] [Limpar filtros]   │
└──────────────────────────────────────┘
```

## Cards e preço em Serviços e Vagas

- Os cards dos resultados seguem as três variantes desenhadas em `US-001-pagina-inicial.md` (padrão, Serviços sem preço, Vagas sem foto e com "Salário").
- **A6 (decidido no `/arch`, ADR-006):** anúncios de Serviços, que não têm preço, ficam fora da faixa de preço quando ela é usada e vão para o fim das ordenações "Menor preço" e "Maior preço". O filtro mostra a nota "Serviços não têm preço: ficam fora da faixa de preço e no fim da ordenação por preço".

## Estados

| Estado | Cenário | O que o visitante vê |
|---|---|---|
| Padrão (resultados) | @US-002-S01 a @US-002-S06 | Total ("1 anúncio encontrado", "30 anúncios encontrados"), 24 cartões por página, ordenação e paginação. |
| Sem resultado | @US-002-S07 | "Nenhum anúncio encontrado para esses filtros" e botão "Limpar filtros", que volta à lista completa, do mais recente ao mais antigo. |
| Filtro inválido | @US-002-S08 | Mensagem junto ao campo de preço: "O preço mínimo não pode ser maior que o máximo"; a lista anterior continua igual. |
| Erro | @US-002-S11 | "Não foi possível buscar agora. Tente novamente." e botão "Tentar novamente"; os filtros escolhidos continuam preenchidos. |
| Carregando | — | Esqueleto dos 24 cartões com a altura reservada; a região de resultados recebe `aria-busy="true"`. Sem cenário na SPEC (ver Lacunas no `README.md`). |
| Vazio (sem anúncios no site) | — | Cai no estado "sem resultado". Não há cenário separado. |

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Sem resultado                                                                    │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Nenhum anúncio encontrado para esses filtros                                     │
│ [Limpar filtros]                                                                 │
└──────────────────────────────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Erro de validação do preço                                                       │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Preço (R$)                                                                       │
│ [ 5000      ] a [ 1000      ]                                                    │
│ ! O preço mínimo não pode ser maior que o máximo                                 │
└──────────────────────────────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Erro de busca                                                                    │
├──────────────────────────────────────────────────────────────────────────────────┤
│ ! Não foi possível buscar agora. Tente novamente.                                │
│ [Tentar novamente]     (filtros mantidos ao lado)                                │
└──────────────────────────────────────────────────────────────────────────────────┘
```

## Responsivo

- **320 px:** um botão "Filtros" abre e fecha o painel (componente recolhível do Bootstrap); resultados em 2 colunas; sem rolagem horizontal (@US-002-S12).
- **768 px:** painel de filtros aberto acima da lista, resultados em 3 colunas.
- **1024 px ou mais:** painel de filtros fixo à esquerda, resultados em 3 ou 4 colunas.
- Endereço da página: todos os filtros, a ordenação e o número da página ficam nele; abrir o mesmo endereço em outra aba reproduz a mesma tela (@US-002-S09).

## Acessibilidade

- Cada campo tem `label` visível; as mensagens de erro ficam ligadas ao campo por `aria-describedby` e anunciadas com `role="alert"`.
- Ao trocar a UF, o campo Cidade volta para "Todas as cidades" e o leitor de tela anuncia a mudança (região `aria-live="polite"`).
- O total de resultados fica em região `role="status"`, para ser anunciado após cada busca.
- A paginação é um `nav` de nome "Paginação"; a página atual usa `aria-current="page"`.
- O botão "Filtros" no celular usa `aria-expanded` e `aria-controls`.
- O painel fechado não recebe foco pelo teclado.

## Mapeamento: controle → cenário

| Região da tela | Controle (papel / nome acessível) | Comportamento | Cenário |
|---|---|---|---|
| Cabeçalho | `searchbox "Buscar anúncios"` + `button "Buscar"` | Filtra por título ou descrição, sem diferenciar maiúsculas e acentos; mostra o total | @US-002-S01 |
| Cabeçalho | `button "Buscar"` (com falha do servidor) | Mostra o erro de busca e mantém os filtros | @US-002-S11 |
| Filtros | `combobox "Categoria"` | Escolher uma categoria principal inclui suas subcategorias | @US-002-S02 |
| Filtros | `combobox "UF"` | Habilita a cidade; ao trocar, a cidade volta para "Todas as cidades" e a lista passa a ter só cidades da nova UF | @US-002-S02, @US-002-S10 |
| Filtros | `combobox "Cidade"` | Só pode ser escolhida depois da UF | @US-002-S02, @US-002-S10 |
| Filtros | `spinbutton "Preço máximo (R$)"` | Limita os resultados ao preço informado | @US-002-S02 |
| Filtros | `spinbutton "Preço mínimo (R$)"` | Junto do máximo, forma a faixa; erro se maior que o máximo | @US-002-S08 |
| Filtros | `combobox "Marca"` | Aparece só em categoria de Automóveis, Peças e Acessórios | @US-002-S03 |
| Filtros | `spinbutton "Ano de"` e `spinbutton "Ano até"` | Faixa de ano; só em Automóveis, Peças e Acessórios | @US-002-S03 |
| Filtros | `spinbutton "Quilometragem máxima (km)"` | Limite de quilometragem; só em Automóveis, Peças e Acessórios | @US-002-S03 |
| Filtros | `textbox "Modelo"` | Aparece só em Automóveis, Peças e Acessórios | ⚠️ nenhum cenário cobre este controle (a regra de negócio cita "modelo", mas S03 não o usa) |
| Filtros | `spinbutton "Área mínima (m²)"` e `spinbutton "Área máxima (m²)"` | Só em "Terrenos, sítios e fazendas" | @US-002-S04 |
| Filtros | `button "Aplicar filtros"` | Combina todos os filtros (todos precisam ser atendidos) e atualiza a lista | @US-002-S02, @US-002-S03, @US-002-S04, @US-002-S08 |
| Filtros | Regra de exibição dos filtros por categoria (sem controle próprio) | Filtros de veículo e de terreno só existem após escolher a categoria correspondente | @US-002-S03 |
| Resultados | `combobox "Ordenar por"` (Mais recentes, Menor preço, Maior preço) | Reordena; a escolha continua ao mudar de página | @US-002-S05 |
| Resultados | `nav "Paginação"` → `link "Próxima"` e `link "2"` | 24 por página; a página atual fica destacada | @US-002-S06 |
| Resultados | Mensagem "Nenhum anúncio encontrado para esses filtros" + `button "Limpar filtros"` | Volta à lista completa, do mais recente ao mais antigo | @US-002-S07 |
| Resultados | Mensagem de erro junto ao preço (`alert`) | Impede a aplicação da faixa invertida | @US-002-S08 |
| Resultados | `alert "Não foi possível buscar agora. Tente novamente."` + `button "Tentar novamente"` | Repete a busca com os mesmos filtros | @US-002-S11 |
| Resultados | Cartão: `link "Honda Civic 2018, R$ 62.000, Campinas/SP"` | Abre o detalhe do anúncio (ref. cruzada) | @US-003-S01 |
| Resultados | Cartão: `button "Favoritar anúncio Honda Civic 2018"` (coração) | Marca ou desmarca favorito (ref. cruzada) | @US-005-S01 |
| Filtros | Nota "Serviços não têm preço…" | Explica por que Serviços somem da faixa de preço | @US-002-S02 |
| Resultados | Ordenação "Menor preço" com anúncios de Serviços | Serviços aparecem no fim da lista (A6) | @US-002-S05 |
| Endereço da página | Filtros e ordenação no endereço (sem controle visível) | Abrir o mesmo endereço reproduz filtros e resultados | @US-002-S09 |
| Página inteira (mobile) | `button "Filtros"` (`aria-expanded`) | Abre e fecha o painel; a página não rola na horizontal | @US-002-S12 |
| Cabeçalho | `link "Favoritos (2)"` | Abre "Meus favoritos" (ref. cruzada) | @US-005-S01 |
