# Wireframe: Enviar anúncio para revisão — @US-009

> **Em resumo:** quando o anúncio está pronto, o Redator clica em "Enviar para revisão" no rodapé do formulário (`US-008-editar-anuncio.md`) e confirma. Se faltar algo, a tela lista as pendências, cada uma com um link para o campo. Depois do envio o anúncio fica só para leitura e entra na fila do Administrador (`US-010-revisar-anuncio.md`). Vale também para reenviar um anúncio rejeitado que já foi corrigido.

**Evidência da SPEC:** `specs/SPEC.md` → US-009 (cenários S01 a S05).

## Layout — pendências (formulário com envio recusado)

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Gazeta · Painel                                    Ana Souza (Redator)  [ Sair ] │
│ [Meus anúncios]                                                                  │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Editar anúncio                                                Situação: Rascunho │
│ ! Faltam 2 itens para enviar este anúncio para revisão:                          │
│   - Adicione ao menos 1 foto              → vai para a seção "Fotos"             │
│   - Informe o CEP                         → vai para o campo "CEP"               │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Título *      [ Honda Civic 2018                                             ]   │
│ CEP *         [ 00000-000   ]   Cidade (automático) [      ]  UF [    ]          │
│ Fotos (0 de 20)   [ Selecionar fotos ]  [ Enviar fotos ]                         │
├──────────────────────────────────────────────────────────────────────────────────┤
│ [ Salvar rascunho ]   [ Enviar para revisão ]                                    │
└──────────────────────────────────────────────────────────────────────────────────┘
```

Para categoria com campos de veículo (por exemplo, "Carros, vans e utilitários") sem quilometragem, a lista traz "Informe a quilometragem"; o mesmo vale para marca, modelo, ano e, em "Terrenos, sítios e fazendas", para a área.

## Layout — confirmação

```text
┌──────────────────────────────────────────────────────────┐
│ Enviar para revisão?                                     │
├──────────────────────────────────────────────────────────┤
│ Depois de enviado, o anúncio fica somente para leitura   │
│ até um Administrador decidir.                            │
│ [ Cancelar ]   [ Enviar para revisão ]                   │
└──────────────────────────────────────────────────────────┘
```

## Layout — depois do envio (lista "Meus anúncios")

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Gazeta · Painel                                    Ana Souza (Redator)  [ Sair ] │
│ [Meus anúncios]                                                                  │
├──────────────────────────────────────────────────────────────────────────────────┤
│ i Anúncio enviado para revisão                                                   │
│ Meus anúncios                                                   [ Novo anúncio ] │
│ Título                          Categoria       Situação      Alterado em        │
│ Honda Civic 2018                Carros          Em revisão    30/09/2026         │
└──────────────────────────────────────────────────────────────────────────────────┘
```

## Estados

| Estado | Cenário | O que o Redator vê |
|---|---|---|
| Envio aceito | @US-009-S01 | Confirmação, depois "Anúncio enviado para revisão"; em "Meus anúncios" a situação passa a "Em revisão" e o anúncio surge na "Fila de revisão" do Administrador. |
| Pendências | @US-009-S02 | Lista "Adicione ao menos 1 foto" e "Informe o CEP", cada item com link para o campo; a situação continua "Rascunho". |
| Característica da categoria faltando | @US-009-S03 | Pendência "Informe a quilometragem"; a situação continua "Rascunho". |
| Reenvio de rejeitado | @US-009-S04 | Depois da confirmação, situação "Em revisão"; o anúncio volta à fila do Administrador. |
| Clique duplo | @US-009-S05 | O botão fica desativado ao primeiro clique; o anúncio aparece uma única vez na fila. |
| Carregando | — | O botão do diálogo mostra "Enviando…" e fica desativado. Sem cenário próprio na SPEC além do clique duplo. |
| Erro de rede ao enviar | — | Não previsto na SPEC (ver Lacunas no `README.md`). |
| Vazio e sem resultado | — | Não se aplicam. |

## Responsivo

- Em 320 px a lista de pendências ocupa a largura toda, acima dos campos; o botão "Enviar para revisão" ocupa a largura toda; o diálogo ocupa a largura da tela com margem de 16 px.

## Acessibilidade

- A lista de pendências recebe o foco ao aparecer (`role="alert"`, título "Faltam 2 itens…"); cada item é um `link` que move o foco para o campo indicado.
- O diálogo de confirmação é `role="alertdialog"` com `aria-modal="true"`, prende o foco, o foco inicial fica em "Cancelar" e o foco volta ao botão de origem ao fechar.
- A mensagem de sucesso usa `role="status"`.

## Mapeamento: controle → cenário

| Região da tela | Controle (papel / nome acessível) | Comportamento | Cenário |
|---|---|---|---|
| Rodapé do formulário | `button "Enviar para revisão"` (rascunho completo) | Abre a confirmação | @US-009-S01 |
| Diálogo de confirmação | `button "Enviar para revisão"` (confirmar) | Muda a situação para "Em revisão" e mostra `status "Anúncio enviado para revisão"` | @US-009-S01 |
| Diálogo de confirmação | `button "Cancelar"` | Fecha sem enviar; a situação não muda | ⚠️ nenhum cenário cobre este controle |
| Lista "Meus anúncios" | Célula de situação "Em revisão" (ref. cruzada, US-012) | Mostra o resultado do envio | @US-009-S01 |
| Fila de revisão do Administrador (ref. cruzada, US-010) | Linha do anúncio recém-enviado | Aparece na fila | @US-009-S01 |
| Topo do formulário | `alert` "Faltam 2 itens…" com `link "Adicione ao menos 1 foto"` e `link "Informe o CEP"` | Lista as pendências e leva ao campo; situação continua "Rascunho" | @US-009-S02 |
| Topo do formulário | `link "Informe a quilometragem"` | Pendência específica da categoria "Carros, vans e utilitários" | @US-009-S03 |
| Rodapé do formulário (anúncio rejeitado) | `button "Enviar para revisão"` + confirmação | Situação passa a "Em revisão"; o anúncio volta à fila | @US-009-S04 |
| Rodapé do formulário e diálogo | `button "Enviar para revisão"` (desativado após o 1º clique) | Evita envio duplicado; o anúncio aparece uma vez na fila | @US-009-S05 |
