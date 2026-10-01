# Wireframe: Despublicar e arquivar anúncios — @US-011

> **Em resumo:** o Administrador tira um anúncio do ar em dois graus. **Despublicar** tira o anúncio do site na hora e o devolve a rascunho, para poder ser corrigido e enviado de novo. **Arquivar** encerra o anúncio de vez (por exemplo, bem vendido) e não pode ser desfeito na versão 1, por isso pede confirmação. As duas ações ficam na barra de ações da página do anúncio no painel; o Redator não as vê.

**Evidência da SPEC:** `specs/SPEC.md` → US-011 (cenários S01 a S07).

## Layout — página do anúncio no painel (Administrador, anúncio publicado)

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Gazeta · Painel                                 Marcos (Administrador)  [ Sair ] │
│ [Anúncios]  [Categorias]  [Usuários]  [Configurações]                            │
├──────────────────────────────────────────────────────────────────────────────────┤
│ ◀ Todos os anúncios                                                              │
│ Honda Civic 2018                                             Situação: Publicado │
│ [ Editar ]   [ Despublicar ]   [ Arquivar ]                                      │
├──────────────────────────────────────────────────────────────────────────────────┤
│ (visualização do anúncio, igual à página pública, em modo de leitura)            │
│ R$ 62.000 · Carros · Campinas/SP · Autor: Ana Souza                              │
└──────────────────────────────────────────────────────────────────────────────────┘
```

### Ações disponíveis por situação

| Situação do anúncio | Administrador vê | Redator (autor) vê |
|---|---|---|
| Rascunho | Editar, Enviar para revisão, **Arquivar** | Editar, Enviar para revisão |
| Em revisão | Publicar, Rejeitar, Editar, **Arquivar** | Somente leitura |
| Rejeitado | Editar, Enviar para revisão, **Arquivar** | Editar, Enviar para revisão |
| Publicado | Editar, **Despublicar**, **Arquivar** | Somente leitura, sem ações de retirada |
| Arquivado | Nenhuma ação (somente leitura) | Somente leitura |

Nos anúncios em Rascunho, Em revisão e Rejeitado, o botão "Arquivar" fica no rodapé do formulário (`US-008`) ou na barra de decisão (`US-010`), ao lado dos demais botões.

## Layout — diálogos de confirmação

```text
┌──────────────────────────────────────────────────────────┐
│ Despublicar este anúncio?                                │
├──────────────────────────────────────────────────────────┤
│ Ele sai do site agora e volta a Rascunho. Você poderá    │
│ corrigi-lo e enviá-lo de novo para revisão.              │
│ [ Cancelar ]   [ Despublicar ]                           │
└──────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────┐
│ Arquivar este anúncio?                                   │
├──────────────────────────────────────────────────────────┤
│ O anúncio sairá do site e não poderá ser reativado.      │
│ [ Cancelar ]   [ Arquivar ]                              │
└──────────────────────────────────────────────────────────┘
```

## Layout — anúncio arquivado e visão do Redator

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Honda Civic 2018                                             Situação: Arquivado │
│ (nenhum botão "Despublicar" nem "Arquivar"; conteúdo somente para leitura)       │
└──────────────────────────────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Redator com anúncio publicado                                                    │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Honda Civic 2018                                             Situação: Publicado │
│ (somente leitura; sem "Despublicar" nem "Arquivar")                              │
└──────────────────────────────────────────────────────────────────────────────────┘
```

## Estados

| Estado | Cenário | O que a pessoa vê |
|---|---|---|
| Despublicar | @US-011-S01 | Depois da confirmação, situação "Rascunho"; o anúncio deixa de aparecer na busca. |
| Arquivar publicado | @US-011-S02 | Aviso "O anúncio sairá do site e não poderá ser reativado"; situação "Arquivado"; aparece na lista ao filtrar por "Arquivado"; o endereço antigo mostra "Este anúncio não está mais disponível". |
| Cancelar | @US-011-S03 | O diálogo fecha; a situação continua "Publicado" e o anúncio segue no site. |
| Favorito do visitante | @US-011-S04 | No navegador do visitante, o anúncio some de "Meus favoritos" com o aviso de anúncio indisponível (tela em `US-005-favoritos.md`). |
| Arquivar não publicado | @US-011-S05 | Situação "Arquivado" (por exemplo, a partir de "Rejeitado"); deixa de aparecer na lista padrão do painel. |
| Anúncio arquivado | @US-011-S06 | Situação "Arquivado" e nenhum botão de retirada. |
| Redator | @US-011-S07 | Anúncio publicado sem botões "Despublicar" nem "Arquivar". |
| Carregando e erro | — | Os botões dos diálogos mostram "Aguarde…" e ficam desativados; falha de rede não é coberta pela SPEC (ver Lacunas no `README.md`). |
| Vazio e sem resultado | — | Não se aplicam. |

## Responsivo

- Em 320 px os botões da barra de ações ocupam a largura toda, empilhados, com "Arquivar" por último; os diálogos ocupam a largura da tela com margem de 16 px.

## Acessibilidade

- Os dois diálogos são `role="alertdialog"` com `aria-modal="true"`; o foco inicial fica em "Cancelar" (a ação segura), e volta ao botão de origem ao fechar.
- O texto "não poderá ser reativado" é lido junto do título do diálogo (`aria-describedby`).
- A situação é sempre texto ("Publicado", "Arquivado"), não só cor ou ícone.

## Mapeamento: controle → cenário

| Região da tela | Controle (papel / nome acessível) | Comportamento | Cenário |
|---|---|---|---|
| Barra de ações (publicado) | `button "Despublicar"` + diálogo `button "Despublicar"` | Tira o anúncio do site na hora e volta a "Rascunho" | @US-011-S01 |
| Barra de ações (publicado) | `button "Arquivar"` + diálogo com o aviso "O anúncio sairá do site e não poderá ser reativado" e `button "Arquivar"` | Situação "Arquivado"; endereço antigo mostra "Este anúncio não está mais disponível" | @US-011-S02 |
| Diálogo de arquivar | `button "Cancelar"` | Fecha; o anúncio continua "Publicado" no site | @US-011-S03 |
| Site público, Meus favoritos (ref. cruzada, US-005) | Aviso de anúncio indisponível | Efeito do arquivamento no navegador do visitante | @US-011-S04 |
| Barra de ações ou rodapé do formulário (anúncio "Rejeitado") | `button "Arquivar"` | Situação "Arquivado"; some da lista padrão | @US-011-S05 |
| Página do anúncio arquivado | Etiqueta de situação "Arquivado" + ausência de "Despublicar" e "Arquivar" | Somente leitura | @US-011-S06 |
| Página do anúncio publicado (Redator) | Ausência de "Despublicar" e "Arquivar" | Somente leitura | @US-011-S07 |
| Diálogo de despublicar | `button "Cancelar"` | Fecha sem despublicar | ⚠️ nenhum cenário cobre este controle |
| Barra de ações | `button "Editar"` | Abre o formulário de edição (`US-008`) | ⚠️ nenhum cenário cobre este controle |
