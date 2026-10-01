# Wireframe: Lista de anúncios no painel — @US-012

> **Em resumo:** a lista de trabalho da equipe. O Redator vê só os próprios anúncios ("Meus anúncios"); o Administrador vê todos, com o nome do autor ("Anúncios"). Dá para buscar pelo título e filtrar pela situação (Rascunho, Em revisão, Publicado, Rejeitado, Arquivado). São 20 anúncios por página, e os arquivados ficam escondidos até alguém filtrar por "Arquivado".

**Evidência da SPEC:** `specs/SPEC.md` → US-012 (cenários S01 a S08).

## Layout — "Meus anúncios" (Redator, desktop)

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Gazeta · Painel                                    Ana Souza (Redator)  [ Sair ] │
│ [Meus anúncios]                                                                  │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Meus anúncios                                                   [ Novo anúncio ] │
│ [ Buscar por título…          ]  [ Situação: Todas (exceto arquivados) ▾ ]       │
│ 5 anúncios                                                                       │
│ Título                    Categoria                     Situação     Alterado em │
│ Moto para retirar peças   Motos                         Rejeitado    30/09/2026  │
│     Motivo da rejeição: Fotos escuras; envie fotos com boa iluminação            │
│ Honda Civic 2018          Carros, vans e utilitários    Em revisão   29/09/2026  │
│ Terreno 450 m²            Terrenos, sítios e fazendas   Rascunho     28/09/2026  │
│ Casa com quintal          Casas                         Rascunho     27/09/2026  │
│ Jaqueta jeans masculina   Roupas                        Publicado    20/09/2026  │
├──────────────────────────────────────────────────────────────────────────────────┤
│ ◀ Anterior   [1]   Próxima ▶                                                     │
└──────────────────────────────────────────────────────────────────────────────────┘
```

Cada título é um link. Rascunho e Rejeitado abrem o formulário de edição (`US-008`); Em revisão, Publicado e Arquivado abrem a visualização somente leitura.

## Layout — "Anúncios" (Administrador, desktop)

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Gazeta · Painel                                 Marcos (Administrador)  [ Sair ] │
│ [Anúncios]  [Categorias]  [Usuários]  [Configurações]                            │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Anúncios                                                        [ Novo anúncio ] │
│ Abas:  [ Fila de revisão (3) ]   [ Todos os anúncios ]                           │
│ [ Buscar por título…          ]  [ Situação: Todas (exceto arquivados) ▾ ]       │
│ 48 anúncios                                                                      │
│ Título                   Autor        Categoria         Situação     Alterado em │
│ Honda Civic 2018         Ana Souza    Carros, vans e    Publicado    29/09/2026  │
│                                       utilitários                                │
│ Casa com quintal         Bruno Lima   Casas             Em revisão   28/09/2026  │
│ Moto para retirar peças  Ana Souza    Motos             Rejeitado    30/09/2026  │
├──────────────────────────────────────────────────────────────────────────────────┤
│ ◀ Anterior   [1]  2  3   Próxima ▶                                               │
└──────────────────────────────────────────────────────────────────────────────────┘
```

## Layout — mobile (320 px)

```text
┌──────────────────────────────────────┐
│ Gazeta · Painel             [ Menu ] │
├──────────────────────────────────────┤
│ Meus anúncios                        │
│ [ Novo anúncio ]                     │
│ [ Buscar por título…    ]            │
│ [ Situação: Todas (exc. arquiv.) ▾ ] │
│ 5 anúncios                           │
│ ┌──────────────────────────────────┐ │
│ │ Moto para retirar peças          │ │
│ │ Motos · Rejeitado                │ │
│ │ Alterado em 30/09/2026           │ │
│ │ Motivo: Fotos escuras; envie…    │ │
│ └──────────────────────────────────┘ │
│ ┌──────────────────────────────────┐ │
│ │ Honda Civic 2018                 │ │
│ │ Carros, vans e utilitários ·     │ │
│ │ Em revisão                       │ │
│ │ Alterado em 29/09/2026           │ │
│ └──────────────────────────────────┘ │
│ … (cartões empilhados)               │
│ ◀  1 2 3  ▶                          │
└──────────────────────────────────────┘
```

## Estados

| Estado | Cenário | O que a pessoa vê |
|---|---|---|
| Redator com itens | @US-012-S01 | Exatamente os 5 anúncios dela, com título, categoria, situação e data da última alteração; nenhum anúncio de outro Redator. |
| Administrador com itens | @US-012-S02 | Anúncios de todos os redatores, cada linha com o nome do autor. |
| Filtro por situação | @US-012-S03 | Só a situação escolhida (por exemplo, "Rejeitado") e o total dela. |
| Busca por título | @US-012-S04 | "civic" mostra só "Honda Civic 2018". |
| Abrir anúncio | @US-012-S05 | Rascunho abre o formulário; Em revisão abre a visualização somente leitura. |
| Vazio | @US-012-S06 | "Você ainda não criou anúncios" e o botão "Novo anúncio". |
| Mais de 20 anúncios | @US-012-S07 | 20 por página e controle de páginas (45 anúncios: 3 páginas; a página 3 tem 5). |
| Erro | @US-012-S08 | "Não foi possível carregar os anúncios. Tente novamente." e botão "Tentar novamente". |
| Arquivados escondidos | @US-011-S05 | A lista padrão não mostra "Arquivado"; ele aparece ao filtrar por essa situação. |
| Carregando | — | Esqueleto de linhas com altura reservada. Sem cenário na SPEC. |
| Sem resultado do filtro ou da busca | — | Mensagem "Nenhum anúncio encontrado" e botão "Limpar filtros". A SPEC pede este estado nas notas de UI mas não traz cenário (ver Lacunas no `README.md`). |

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Vazio (Redator sem anúncios)                                                     │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Meus anúncios                                                                    │
│ Você ainda não criou anúncios                                                    │
│ [ Novo anúncio ]                                                                 │
└──────────────────────────────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Erro ao carregar                                                                 │
├──────────────────────────────────────────────────────────────────────────────────┤
│ ! Não foi possível carregar os anúncios. Tente novamente.                        │
│ [ Tentar novamente ]                                                             │
└──────────────────────────────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Sem resultado (filtro ou busca)                                                  │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Nenhum anúncio encontrado                                                        │
│ [ Limpar filtros ]                                                               │
└──────────────────────────────────────────────────────────────────────────────────┘
```

## Responsivo

- Em 320 px a tabela vira uma lista de cartões empilhados (título, categoria, situação, data; autor para o Administrador); busca e filtro ocupam a largura toda; sem rolagem horizontal.
- 20 anúncios por página em qualquer largura.

## Acessibilidade

- A tabela usa `th scope="col"`; em telas estreitas os cartões mantêm a ordem de leitura título, situação, categoria, data.
- A situação é sempre texto (não só cor). O motivo da rejeição é texto visível na linha.
- O total ("5 anúncios") fica em região `role="status"`, anunciada a cada filtro ou busca.
- A paginação é um `nav` de nome "Paginação"; a página atual usa `aria-current="page"`.
- O erro usa `role="alert"`.

## Mapeamento: controle → cenário

| Região da tela | Controle (papel / nome acessível) | Comportamento | Cenário |
|---|---|---|---|
| Lista do Redator | Tabela com colunas Título, Categoria, Situação, Alterado em (só os anúncios dele) | Mostra os anúncios do próprio Redator | @US-012-S01 |
| Lista do Administrador | Coluna "Autor" | Mostra o autor de cada anúncio | @US-012-S02 |
| Barra de filtros | `combobox "Situação"` | Filtra por situação e mostra o total dela | @US-012-S03 |
| Barra de filtros | Opção "Arquivado" e padrão "Todas (exceto arquivados)" | Arquivados só aparecem quando o filtro os pede | @US-011-S05 |
| Barra de filtros | `searchbox "Buscar por título"` | Mostra só os anúncios cujo título contém o texto | @US-012-S04 |
| Lista | `link "Honda Civic 2018"` (título) | Rascunho e Rejeitado abrem o formulário; Em revisão abre a leitura | @US-012-S05 |
| Lista | Linha "Rejeitado" com "Motivo da rejeição: …" | O autor vê o motivo escrito pelo Administrador (ref. cruzada) | @US-010-S04 |
| Lista vazia | Mensagem "Você ainda não criou anúncios" + `link "Novo anúncio"` | Estado vazio com saída | @US-012-S06 |
| Cabeçalho da lista | `link "Novo anúncio"` | Abre o formulário em branco (`US-008`) | @US-008-S01 |
| Rodapé da lista | `nav "Paginação"` → `link "3"` | 20 por página; a página 3 mostra os 5 restantes | @US-012-S07 |
| Lista | `alert "Não foi possível carregar os anúncios. Tente novamente."` + `button "Tentar novamente"` | Recarrega a lista | @US-012-S08 |
| Lista (sem resultado) | Mensagem "Nenhum anúncio encontrado" + `button "Limpar filtros"` | Volta à lista completa | ⚠️ nenhum cenário cobre este controle |
| Menu do Administrador | Abas `link "Fila de revisão (3)"` e `link "Todos os anúncios"` | Alternam entre fila e lista completa | ⚠️ nenhum cenário cobre este controle |
