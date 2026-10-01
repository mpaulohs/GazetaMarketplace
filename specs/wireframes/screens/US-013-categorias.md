# Wireframe: Gerenciar categorias — @US-013

> **Em resumo:** página do Administrador para criar, renomear, ordenar e excluir categorias e subcategorias (até três níveis: categoria principal, subcategoria e subcategoria de terceiro nível). A ordem da lista é a ordem em que as categorias aparecem no site. Só se exclui categoria vazia, isto é, sem subcategorias e sem anúncios; as categorias com campos específicos (por exemplo, "Automóveis, Peças e Acessórios" e "Terrenos, sítios e fazendas") só podem ser renomeadas. Ordenar funciona por botões, sem arrastar.

**Evidência da SPEC:** `specs/SPEC.md` → US-013 (cenários S01 a S11).

## Layout — árvore de categorias (desktop)

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Gazeta · Painel                                 Marcos (Administrador)  [ Sair ] │
│ [Anúncios]  [Categorias]  [Usuários]  [Configurações]                            │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Categorias                                                    [ Nova categoria ] │
│ Nome                                         Anúncios Ações                      │
│ ▾ Imóveis                                    0        [▲] [▼] [Editar] [Excluir] │
│     Apartamentos                             3        [▲] [▼] [Editar] [Excluir] │
│     Terrenos, sítios e fazendas              1        [▲] [▼] [Editar] [Excluir] │
│ ▾ Automóveis, Peças e Acessórios             0        [▲] [▼] [Editar] [Excluir] │
│     Carros, vans e utilitários               4        [▲] [▼] [Editar] [Excluir] │
│     Motos                                    3        [▲] [▼] [Editar] [Excluir] │
│     Caminhões                                0        [▲] [▼] [Editar] [Excluir] │
│   ▾ Autopeças                                0        [▲] [▼] [Editar] [Excluir] │
│       Peças para carros, vans e utilitários  0        [▲] [▼] [Editar] [Excluir] │
│       Peças para motos                       0        [▲] [▼] [Editar] [Excluir] │
│ … (demais categorias; o terceiro nível fica recuado sob a subcategoria)          │
└──────────────────────────────────────────────────────────────────────────────────┘
```

Os botões de ordem têm o nome completo para o leitor de tela ("Mover Imóveis para cima", "Mover Imóveis para baixo"). A ordem só muda entre irmãs, isto é, dentro do mesmo grupo.

## Layout — formulário de categoria

```text
┌──────────────────────────────────────────────────────────┐
│ Nova categoria                                           │
├──────────────────────────────────────────────────────────┤
│ Nome *                                                   │
│ [ Quadriciclos                                        ]  │
│ Categoria pai                                            │
│ [ Automóveis, Peças e Acessórios                      ▾] │
│    (lista: "— Nenhuma (categoria principal) —", as       │
│     principais e as subcategorias; nunca uma categoria   │
│     de terceiro nível)                                   │
│ [ Cancelar ]   [ Salvar ]                                │
└──────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────┐
│ Editar categoria (renomear)                              │
├──────────────────────────────────────────────────────────┤
│ Nome *                                                   │
│ [ Veículos de passeio                                 ]  │
│ [ Cancelar ]   [ Salvar ]                                │
└──────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────┐
│ Confirmar exclusão                                       │
├──────────────────────────────────────────────────────────┤
│ Excluir a categoria "Colecionáveis"?                     │
│ Essa ação não pode ser desfeita.                         │
│ [ Cancelar ]   [ Excluir ]                               │
└──────────────────────────────────────────────────────────┘
```

## Layout — mensagens de erro e de bloqueio

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Mensagens (junto do campo Nome ou em destaque acima da árvore)                   │
├──────────────────────────────────────────────────────────────────────────────────┤
│ ! Já existe uma categoria com esse nome neste grupo                              │
│ ! Informe o nome da categoria                                                    │
│ ! Não é possível excluir: 3 anúncios usam esta categoria                         │
│ ! Exclua ou mova antes as subcategorias desta categoria                          │
│ ! Esta categoria é usada pelos campos específicos e não pode ser excluída.       │
│   Você pode renomeá-la.                                                          │
└──────────────────────────────────────────────────────────────────────────────────┘
```

## Estados

| Estado | Cenário | O que o Administrador vê |
|---|---|---|
| Criar subcategoria | @US-013-S01 | "Quadriciclos" aparece abaixo de "Automóveis, Peças e Acessórios"; no site aparece entre as subcategorias de Automóveis, Peças e Acessórios. |
| Criar categoria principal | @US-013-S02 | "Colecionáveis" aparece como principal; a página inicial do site a mostra. |
| Renomear | @US-013-S03 | A lista mostra "Veículos de passeio" no lugar de "Carros, vans e utilitários"; os 4 anúncios seguem publicados. |
| Reordenar | @US-013-S04 | "Mover para cima" em "Automóveis, Peças e Acessórios" a põe antes de "Imóveis"; o site segue a nova ordem. |
| Excluir categoria vazia | @US-013-S05 | Depois da confirmação, "Colecionáveis" some da lista e do site. |
| Nome repetido | @US-013-S06 | "Já existe uma categoria com esse nome neste grupo"; nada é criado. |
| Nome vazio | @US-013-S07 | "Informe o nome da categoria"; nada é criado. |
| Excluir com anúncios | @US-013-S08 | "Não é possível excluir: 3 anúncios usam esta categoria"; a categoria continua. |
| Excluir com subcategorias | @US-013-S09 | "Exclua ou mova antes as subcategorias desta categoria"; a categoria continua. |
| Categoria com campos específicos | @US-013-S10 | Mensagem de que só pode ser renomeada; a categoria continua. |
| Até três níveis | @US-013-S11 | "Peças para quadriciclos" é criada dentro de "Autopeças", no terceiro nível; a lista "Categoria pai" mostra principais e subcategorias, mas não as de terceiro nível, então não dá para criar dentro de "Peças para carros, vans e utilitários". |
| Carregando | — | Esqueleto da árvore. Sem cenário na SPEC. |
| Erro | — | Não previsto na SPEC (ver Lacunas no `README.md`). |
| Vazio e sem resultado | — | A árvore inicial nunca fica vazia na v1 (Apêndice B da SPEC); sem cenário. |

## Responsivo

- Em 320 px cada categoria vira um cartão empilhado (nome, número de anúncios, botões em duas linhas); as subcategorias ficam recuadas sob a principal; o formulário ocupa a largura toda em janela sobre a página; sem rolagem horizontal.
- Os botões de ordem e de ação têm área de toque de pelo menos 44 × 44 px.

## Acessibilidade

- A árvore é uma lista aninhada (`ul` dentro de `li`), com o nível anunciado ("Subcategoria de Automóveis, Peças e Acessórios").
- Todos os botões trazem o nome da categoria no nome acessível ("Excluir Imóveis"); o primeiro item de um grupo não tem "Mover para cima" ativo (`aria-disabled`), e o último não tem "Mover para baixo".
- Depois de mover, o foco fica no botão usado e um `role="status"` anuncia a nova posição ("Automóveis, Peças e Acessórios agora está antes de Imóveis").
- Mensagens de bloqueio em `role="alert"`; erros de campo ligados ao campo por `aria-describedby`.
- O diálogo de exclusão é `role="alertdialog"`, com foco inicial em "Cancelar".

## Mapeamento: controle → cenário

| Região da tela | Controle (papel / nome acessível) | Comportamento | Cenário |
|---|---|---|---|
| Topo da página | `button "Nova categoria"` + formulário (`textbox "Nome"`, `combobox "Categoria pai"` = "Automóveis, Peças e Acessórios", `button "Salvar"`) | Cria a subcategoria abaixo do pai | @US-013-S01 |
| Formulário | `combobox "Categoria pai"` deixado em "— Nenhuma —" + `button "Salvar"` | Cria categoria principal | @US-013-S02 |
| Árvore | `button "Editar"` (por categoria) + `textbox "Nome"` + `button "Salvar"` | Renomeia; os anúncios continuam na categoria | @US-013-S03 |
| Árvore | `button "Mover Automóveis, Peças e Acessórios para cima"` | Troca a ordem no grupo e no site | @US-013-S04 |
| Árvore | `button "Mover Imóveis para baixo"` | Contrapartida de "Mover para cima" | ⚠️ nenhum cenário cobre este controle |
| Árvore | `button "Excluir"` + diálogo `button "Excluir"` (categoria vazia) | Remove a categoria da lista e do site | @US-013-S05 |
| Formulário | `alert "Já existe uma categoria com esse nome neste grupo"` | Bloqueia o nome repetido | @US-013-S06 |
| Formulário | `alert "Informe o nome da categoria"` | Bloqueia o nome vazio | @US-013-S07 |
| Árvore | `alert "Não é possível excluir: 3 anúncios usam esta categoria"` | Mantém a categoria | @US-013-S08 |
| Árvore | `alert "Exclua ou mova antes as subcategorias desta categoria"` | Mantém a categoria | @US-013-S09 |
| Árvore | `alert "Esta categoria é usada pelos campos específicos e não pode ser excluída. Você pode renomeá-la."` | Mantém a categoria; a renomeação continua permitida | @US-013-S10 |
| Formulário | Lista do `combobox "Categoria pai"` com principais e subcategorias, sem as de terceiro nível | Permite criar até o terceiro nível e impede o quarto | @US-013-S11 |
| Formulário e diálogo | `button "Cancelar"` | Fecha sem salvar ou sem excluir | ⚠️ nenhum cenário cobre este controle |
| Árvore | Coluna "Anúncios" (número de anúncios por categoria) | Explica por que uma exclusão é bloqueada | @US-013-S08 |
