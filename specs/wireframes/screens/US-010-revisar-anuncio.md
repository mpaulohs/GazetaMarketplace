# Wireframe: Revisar e publicar ou rejeitar anúncios — @US-010

> **Em resumo:** o Administrador abre a "Fila de revisão", que lista os anúncios enviados do mais antigo ao mais novo. Ao clicar em um deles vê uma pré-visualização idêntica à página pública, marcada como "ainda não publicado", com os botões "Publicar", "Rejeitar" e "Editar". Rejeitar exige um motivo, que o Redator autor vê depois. Sem o telefone do site configurado, nada pode ser publicado. Só o Administrador acessa estas telas.

**Evidência da SPEC:** `specs/SPEC.md` → US-010 (cenários S01 a S09).

## Layout — Fila de revisão (desktop)

A "Fila de revisão" é a lista de anúncios da `US-012-painel-anuncios.md` já filtrada por "Em revisão" e ordenada do mais antigo ao mais novo. Ela é a primeira tela do Administrador depois de entrar e fica como uma aba dentro do menu "Anúncios".

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Gazeta · Painel                                 Marcos (Administrador)  [ Sair ] │
│ [Anúncios]  [Categorias]  [Usuários]  [Configurações]                            │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Anúncios:  [ Fila de revisão (3) ]   [ Todos os anúncios ]                       │
│ 3 anúncios aguardando revisão, do mais antigo ao mais recente                    │
│ Título                      Autor         Categoria       Enviado em             │
│ Casa com quintal            Bruno Lima    Casas           28/09/2026             │
│ Honda Civic 2018            Ana Souza     Carros          29/09/2026             │
│ Moto para retirar peças     Ana Souza     Motos           30/09/2026             │
│ (cada linha é um link para a pré-visualização do anúncio)                        │
└──────────────────────────────────────────────────────────────────────────────────┘
```

## Layout — pré-visualização com decisão

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Gazeta · Painel                                 Marcos (Administrador)  [ Sair ] │
│ [Anúncios]  [Categorias]  [Usuários]  [Configurações]                            │
├──────────────────────────────────────────────────────────────────────────────────┤
│ ◀ Fila de revisão                                                                │
│ i Pré-visualização — ainda não publicado                                         │
│ [ Publicar ]   [ Rejeitar ]   [ Editar ]                                         │
│ (só se Cidade/UF foram manuais) [Cidade/UF informadas manualmente (CEP não       │
│ conferido)] CEP 13015-100 · Campinas/SP                                          │
├──────────────────────────────────────────────────────────────────────────────────┤
│ (a página do anúncio como o visitante a veria, igual à US-003)                   │
│ Honda Civic 2018                          Fale com a Gazeta                      │
│ R$ 62.000 · Carros · Campinas/SP          (11) 91234-5678                        │
│ [ foto em destaque ]  5 de 20             [ Ligar ] [ Chamar no WhatsApp ]       │
│ Descrição · Características do veículo                                           │
└──────────────────────────────────────────────────────────────────────────────────┘
```

## Layout — diálogos e avisos

```text
┌──────────────────────────────────────────────────────────┐
│ Publicar este anúncio?                                   │
├──────────────────────────────────────────────────────────┤
│ Ele passa a aparecer no site para todos os visitantes.   │
│ [ Cancelar ]   [ Publicar ]                              │
└──────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────┐
│ Rejeitar anúncio                                         │
├──────────────────────────────────────────────────────────┤
│ Motivo da rejeição *                                     │
│ [ Fotos escuras; envie fotos com boa iluminação    ]     │
│ O motivo fica visível para quem cadastrou o anúncio.     │
│ [ Cancelar ]   [ Rejeitar anúncio ]                      │
└──────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────┐
│ Rejeitar sem motivo                                      │
├──────────────────────────────────────────────────────────┤
│ Motivo da rejeição *                                     │
│ [                                                    ]   │
│ ! Informe o motivo da rejeição                           │
│ [ Cancelar ]   [ Rejeitar anúncio ]                      │
└──────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Conflito: outro Administrador decidiu antes                                      │
├──────────────────────────────────────────────────────────────────────────────────┤
│ ! Este anúncio já foi publicado por outro administrador                          │
│ Situação: Publicado                                                              │
└──────────────────────────────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Publicar sem o telefone do site configurado                                      │
├──────────────────────────────────────────────────────────────────────────────────┤
│ ! Configure o telefone/WhatsApp do site antes de publicar     [ Configurações ]  │
│ Situação do anúncio: Em revisão                                                  │
└──────────────────────────────────────────────────────────────────────────────────┘
```

Quem não é Administrador vê a tela de "acesso negado" desenhada em `US-006-login-equipe.md`: "Você não tem permissão para acessar esta página", sem os botões "Publicar" nem "Rejeitar".

## Estados

| Estado | Cenário | O que o Administrador vê |
|---|---|---|
| Fila com itens | @US-010-S01 | Os anúncios em revisão, do mais antigo ao mais novo, com título, autor, categoria e data de envio. |
| Pré-visualização | @US-010-S02 | Página igual à pública, marcada "Pré-visualização — ainda não publicado", com "Publicar", "Rejeitar" e "Editar". |
| Publicação | @US-010-S03 | Depois da confirmação, situação "Publicado", o anúncio sai da fila e passa a aparecer no site e na busca. |
| Rejeição | @US-010-S04 | Diálogo com motivo; depois de confirmar, situação "Rejeitado", fora da fila e fora do site; o Redator vê o motivo em "Meus anúncios". |
| Rejeição sem motivo | @US-010-S05 | "Informe o motivo da rejeição"; a situação continua "Em revisão". |
| Fila vazia | @US-010-S06 | "Nenhum anúncio aguardando revisão". |
| Conflito de decisão | @US-010-S07 | "Este anúncio já foi publicado por outro administrador"; a situação continua "Publicado". |
| Telefone do site não configurado | @US-010-S08 | "Configure o telefone/WhatsApp do site antes de publicar" com link para "Configurações"; situação continua "Em revisão". |
| Acesso negado (Redator) | @US-010-S09 | "Você não tem permissão para acessar esta página"; sem botões de decisão. |
| Carregando | — | Esqueleto das linhas da fila. Sem cenário na SPEC. |
| Erro | — | Igual ao da lista de anúncios: "Não foi possível carregar os anúncios. Tente novamente." com "Tentar novamente" (@US-012-S08). |
| Sem resultado | — | Não se aplica: a fila não tem filtros próprios. |

## Responsivo

- Em 320 px cada anúncio da fila vira um cartão empilhado (título, autor, categoria, data de envio); os botões "Publicar", "Rejeitar" e "Editar" ocupam a largura toda, um sobre o outro; os diálogos ocupam a largura da tela com margem de 16 px.

## Acessibilidade

- A fila é uma tabela com `th scope="col"`; o título de cada linha é o link que abre a pré-visualização.
- A faixa "Pré-visualização — ainda não publicado" é texto real (não só cor) e fica logo abaixo do `h1`.
- Os diálogos de publicar e rejeitar são `role="dialog"` com `aria-modal="true"`, prendem o foco e devolvem o foco ao botão de origem; o campo do motivo tem `label` e, com erro, `aria-describedby` para a mensagem em `role="alert"`.
- Conflito e telefone não configurado usam `role="alert"`; o link "Configurações" recebe o foco depois da mensagem.

## Mapeamento: controle → cenário

| Região da tela | Controle (papel / nome acessível) | Comportamento | Cenário |
|---|---|---|---|
| Pré-visualização (ref. cruzada, US-008) | Selo "Cidade/UF informadas manualmente (CEP não conferido)" + CEP e cidade | Só aparece se Cidade/UF foram preenchidas à mão; informativo, não bloqueia "Publicar" | @US-008-S14 |
| Fila | Tabela: colunas Título, Autor, Categoria, Enviado em; ordem do mais antigo ao mais novo | Lista os anúncios "Em revisão" | @US-010-S01 |
| Fila | `link "Honda Civic 2018"` (título em cada linha) | Abre a pré-visualização | @US-010-S02 |
| Fila | Mensagem "Nenhum anúncio aguardando revisão" | Estado vazio | @US-010-S06 |
| Pré-visualização | Faixa `status "Pré-visualização — ainda não publicado"` | Deixa claro que ainda não está no ar | @US-010-S02 |
| Pré-visualização | `button "Editar"` | Abre o formulário de edição (`US-008`) para corrigir antes de decidir | @US-010-S02 |
| Pré-visualização | `button "Publicar"` + diálogo `button "Publicar"` / `button "Cancelar"` | Publica; o anúncio sai da fila e aparece no site | @US-010-S03 |
| Pré-visualização | `button "Rejeitar"` + diálogo com `textbox "Motivo da rejeição"` e `button "Rejeitar anúncio"` | Rejeita com motivo; o Redator autor o vê | @US-010-S04 |
| Diálogo de rejeição | `alert "Informe o motivo da rejeição"` | Bloqueia a rejeição sem motivo | @US-010-S05 |
| Pré-visualização | `alert "Este anúncio já foi publicado por outro administrador"` | Mostra o resultado do conflito; situação mantida | @US-010-S07 |
| Pré-visualização | `alert "Configure o telefone/WhatsApp do site antes de publicar"` + `link "Configurações"` | Impede a publicação e leva à configuração | @US-010-S08 |
| Páginas da fila e da pré-visualização | `alert "Você não tem permissão para acessar esta página"` | Redator não vê os botões de decisão | @US-010-S09 |
| Menu "Anúncios" | Abas `link "Fila de revisão (3)"` e `link "Todos os anúncios"` | Alternam entre a fila e a lista completa (`US-012`) | ⚠️ nenhum cenário cobre este controle |
| Pré-visualização | `link "Fila de revisão"` (voltar) | Retorna à fila | ⚠️ nenhum cenário cobre este controle |
| Diálogo de publicar | `button "Cancelar"` | Fecha sem publicar | ⚠️ nenhum cenário cobre este controle |
