# Wireframe: Favoritos — @US-005

> **Em resumo:** o visitante marca anúncios com um coração (na lista ou na página do anúncio) e os revê em "Meus favoritos". Os favoritos ficam salvos **só no navegador e no aparelho** dele, sem conta; a página avisa isso de forma permanente. Anúncios que saíram do ar são retirados da lista com um aviso. Esta história depende da suposição S1 da SPEC, ainda a confirmar com o Product Owner.

**Evidência da SPEC:** `specs/SPEC.md` → US-005 (cenários S01 a S08).

## Layout — "Meus favoritos" (desktop)

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ GazetaMarketplace                                                ♡ Favoritos (2) │
│ [ Buscar anúncios…                                        ]  [Buscar]            │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Meus favoritos                                                                   │
│ i Seus favoritos ficam salvos apenas neste navegador.                            │
├──────────────────────────────────────────────────────────────────────────────────┤
│ 2 anúncios                                                                       │
│ ┌─────────────────┐ ┌─────────────────┐                                          │
│ │ [ foto de capa ]│ │ [ foto de capa ]│                                          │
│ │ Honda Civic 2018│ │ Terreno 450 m²  │                                          │
│ │ R$ 62.000       │ │ R$ 120.000      │                                          │
│ │ Campinas/SP     │ │ Curitiba/PR     │                                          │
│ │ [ Remover ]     │ │ [ Remover ]     │                                          │
│ └─────────────────┘ └─────────────────┘                                          │
├──────────────────────────────────────────────────────────────────────────────────┤
│ © GazetaMarketplace                                               Área da equipe │
└──────────────────────────────────────────────────────────────────────────────────┘
```

## Layout — "Meus favoritos" (mobile, 320 px)

```text
┌──────────────────────────────────────┐
│ GazetaMarketplace              ♥ (2) │
│ [ Buscar anúncios…  ] [Buscar]       │
├──────────────────────────────────────┤
│ Meus favoritos                       │
│ i Seus favoritos ficam salvos        │
│   apenas neste navegador.            │
│ 2 anúncios                           │
│ ┌───────────────┐ ┌───────────────┐  │
│ │ [ foto ]      │ │ [ foto ]      │  │
│ │ Honda Civic   │ │ Terreno       │  │
│ │ 2018          │ │ 450 m²        │  │
│ │ R$ 62.000     │ │ R$ 120.000    │  │
│ │ Campinas/SP   │ │ Curitiba/PR   │  │
│ │ [ Remover ]   │ │ [ Remover ]   │  │
│ └───────────────┘ └───────────────┘  │
└──────────────────────────────────────┘
```

## Componentes de favoritar

```text
┌──────────────────────────────────────────────────────────┐
│ Cartão na lista        Página do anúncio                 │
│ Campinas/SP        ♡  [ ♡ Favoritar ]                    │
│ Campinas/SP        ♥  [ ♥ Favoritado ]                   │
└──────────────────────────────────────────────────────────┘
```

O contador "Favoritos (n)" fica no cabeçalho de todas as páginas públicas e muda na hora em que o visitante favorita ou desfavorita.

## Estados

| Estado | Cenário | O que o visitante vê |
|---|---|---|
| Favoritar na lista | @US-005-S01 | O coração fica preenchido e o contador do topo passa de 0 para 1. |
| Favoritar e desfavoritar no detalhe | @US-005-S02 | "Favoritar" vira "Favoritado" e volta; o contador acompanha. |
| Favoritos guardados | @US-005-S03 | Depois de fechar e reabrir o navegador, o coração continua preenchido e o anúncio está em "Meus favoritos". |
| Remover na página de favoritos | @US-005-S04 | O anúncio some da lista e o contador diminui. |
| Vazio | @US-005-S05 | "Você ainda não favoritou nenhum anúncio" e link para a página inicial. |
| Favorito indisponível | @US-005-S06 | O anúncio não aparece e surge o aviso "1 anúncio favoritado deixou de estar disponível e foi removido da sua lista". |
| Armazenamento bloqueado | @US-005-S07 | "Não foi possível salvar seus favoritos neste navegador"; o coração continua vazio. |
| Outro aparelho | @US-005-S08 | Lista vazia e o aviso permanente "Seus favoritos ficam salvos apenas neste navegador". |
| Carregando | — | Esqueleto dos cartões enquanto a lista é montada. Sem cenário na SPEC. |
| Erro | — | Não previsto na SPEC (ver Lacunas no `README.md`). |
| Sem resultado | — | Não se aplica: a página não tem filtros. |

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Estado vazio                                                                     │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Meus favoritos                                                                   │
│ i Seus favoritos ficam salvos apenas neste navegador.                            │
│ Você ainda não favoritou nenhum anúncio.                                         │
│ [Ir para a página inicial]                                                       │
└──────────────────────────────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Aviso de favorito que saiu do ar                                                 │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Meus favoritos                                                                   │
│ i Seus favoritos ficam salvos apenas neste navegador.                            │
│ ! 1 anúncio favoritado deixou de estar disponível e foi removido da sua lista.   │
│ 1 anúncio      (a lista mostra só os anúncios ainda publicados)                  │
└──────────────────────────────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Armazenamento bloqueado (ao clicar no coração)                                   │
├──────────────────────────────────────────────────────────────────────────────────┤
│ ! Não foi possível salvar seus favoritos neste navegador.                        │
│ [ ♡ ]  o coração continua vazio                                                  │
└──────────────────────────────────────────────────────────────────────────────────┘
```

## Responsivo

- Em 320 px os cartões seguem em 2 colunas; o aviso do navegador e o contador ficam sempre visíveis, sem rolagem horizontal.
- O coração e o botão "Remover" têm área de toque de pelo menos 44 × 44 px.

## Acessibilidade

- O coração é um `button` com `aria-pressed`; o nome acessível é "Favoritar anúncio Honda Civic 2018" (quando marcado, o estado é anunciado como pressionado).
- O botão da página do anúncio troca o rótulo entre "Favoritar" e "Favoritado" e também usa `aria-pressed`.
- O contador do cabeçalho fica em região `role="status"`, para o leitor de tela anunciar a mudança.
- Avisos e falhas usam `role="alert"` (falha de armazenamento) ou `role="status"` (aviso de indisponível).
- O aviso "Seus favoritos ficam salvos apenas neste navegador" é texto fixo na página, não um aviso que some.

## Mapeamento: controle → cenário

| Região da tela | Controle (papel / nome acessível) | Comportamento | Cenário |
|---|---|---|---|
| Cartão de anúncio (lista) | `button "Favoritar anúncio Honda Civic 2018"` (coração, `aria-pressed`) | Preenche o coração e soma 1 ao contador | @US-005-S01 |
| Cartão de anúncio (lista) | O mesmo botão, quando o navegador bloqueia o armazenamento | Mostra a falha e o coração continua vazio | @US-005-S07 |
| Cabeçalho | `link "Favoritos (1)"` (contador) | Mostra o total e abre "Meus favoritos" | @US-005-S01, @US-005-S02, @US-005-S04 |
| Página do anúncio | `button "Favoritar"` / `button "Favoritado"` | Alterna o favorito; o contador muda | @US-005-S02 |
| Cartões e página do anúncio | Estado guardado no navegador (sem controle próprio) | O coração continua preenchido depois de fechar o navegador | @US-005-S03 |
| Meus favoritos | Lista de cartões dos anúncios favoritados | Mostra os anúncios ainda publicados | @US-005-S03 |
| Meus favoritos | `button "Remover"` (um por cartão) | Tira o anúncio da lista e diminui o contador | @US-005-S04 |
| Meus favoritos | Mensagem "Você ainda não favoritou nenhum anúncio" + `link "Ir para a página inicial"` | Estado vazio com saída | @US-005-S05 |
| Meus favoritos | `status "1 anúncio favoritado deixou de estar disponível e foi removido da sua lista"` | Avisa sobre anúncio arquivado ou fora do ar; o anúncio não aparece (vale também para o arquivamento feito na @US-011-S04) | @US-005-S06 |
| Meus favoritos | `alert "Não foi possível salvar seus favoritos neste navegador"` | Explica a falha sem apagar nada | @US-005-S07 |
| Meus favoritos | Aviso fixo "Seus favoritos ficam salvos apenas neste navegador" | Mostra que a lista não acompanha o visitante em outro aparelho | @US-005-S08 |
