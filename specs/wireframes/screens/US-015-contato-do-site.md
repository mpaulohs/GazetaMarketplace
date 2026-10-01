# Wireframe: Configurar o telefone/WhatsApp do site — @US-015

> **Em resumo:** página "Configurações", só do Administrador, com um único campo: o telefone/WhatsApp da Gazeta. O número vale para todos os anúncios ao mesmo tempo e alimenta os botões "Ligar" e "Chamar no WhatsApp" do site. Enquanto ele não estiver configurado, nenhum anúncio pode ser publicado.

**Evidência da SPEC:** `specs/SPEC.md` → US-015 (cenários S01 a S06).

## Layout — Configurações (desktop)

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Gazeta · Painel                                 Marcos (Administrador)  [ Sair ] │
│ [Anúncios]  [Categorias]  [Usuários]  [Configurações]                            │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Configurações                                                                    │
│ Telefone/WhatsApp do site *                                                      │
│ [ (11) 91234-5678                        ]                                       │
│ Esse número aparece em todos os anúncios publicados, nos botões                  │
│ "Ligar" e "Chamar no WhatsApp".                                                  │
│ Informe DDD e número, por exemplo (11) 91234-5678.                               │
│ [ Salvar ]                                                                       │
└──────────────────────────────────────────────────────────────────────────────────┘
```

## Layout — número ainda não configurado

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Primeiro uso (campo vazio, aviso no topo)                                        │
├──────────────────────────────────────────────────────────────────────────────────┤
│ ! O telefone/WhatsApp do site ainda não foi configurado. Sem ele, nenhum         │
│   anúncio pode ser publicado.                                                    │
│ Telefone/WhatsApp do site *                                                      │
│ [                                        ]                                       │
│ [ Salvar ]                                                                       │
└──────────────────────────────────────────────────────────────────────────────────┘
```

## Layout — mobile (320 px)

```text
┌──────────────────────────────────────┐
│ Gazeta · Painel             [ Menu ] │
├──────────────────────────────────────┤
│ Configurações                        │
│ Telefone/WhatsApp do site *          │
│ [ (11) 91234-5678        ]           │
│ Esse número aparece em todos os      │
│ anúncios publicados.                 │
│ [        Salvar         ]            │
└──────────────────────────────────────┘
```

## Estados

| Estado | Cenário | O que o Administrador vê |
|---|---|---|
| Salvar número | @US-015-S01 | "Configurações salvas"; o visitante vê o número em qualquer anúncio publicado e o botão de WhatsApp usa esse número. |
| Trocar número em uso | @US-015-S02 | Todos os anúncios publicados passam a mostrar o número novo, sem editá-los; "Ligar" e "Chamar no WhatsApp" usam o novo número. |
| Sem formatação | @US-015-S03 | "11912345678" é aceito; os anúncios mostram (11) 91234-5678. |
| Número inválido | @US-015-S04 | "Informe um número com DDD, por exemplo (11) 91234-5678"; o número em uso não muda. |
| Número vazio | @US-015-S05 | "O telefone é obrigatório"; o número em uso não muda. |
| Acesso negado (Redator) | @US-015-S06 | "Você não tem permissão para acessar esta página"; o campo do telefone não aparece (mesmo desenho do acesso negado em `US-006-login-equipe.md`). |
| Número não configurado | — | Aviso no topo e campo vazio. Nenhum cenário desta história descreve o aviso; o efeito sobre a publicação está em @US-010-S08. |
| Carregando | — | O botão "Salvar" mostra "Salvando…" e fica desativado. Sem cenário na SPEC. |
| Erro do servidor | — | Não previsto na SPEC (ver Lacunas no `README.md`). |
| Vazio e sem resultado | — | Não se aplicam (a página não tem lista). |

## Responsivo

- Coluna única desde 320 px; o campo e o botão ocupam a largura toda; sem rolagem horizontal.

## Acessibilidade

- O campo tem `label` visível "Telefone/WhatsApp do site", `type="tel"`, `inputmode="tel"` e a dica ("Ex.: (11) 91234-5678") ligada por `aria-describedby`.
- O resultado ("Configurações salvas") usa `role="status"`; os erros usam `role="alert"`, com o campo marcado `aria-invalid="true"`.
- O aviso de número não configurado é texto real, no topo, antes do campo.

## Mapeamento: controle → cenário

| Região da tela | Controle (papel / nome acessível) | Comportamento | Cenário |
|---|---|---|---|
| Formulário | `textbox "Telefone/WhatsApp do site"` = "(11) 91234-5678" + `button "Salvar"` | Salva; mostra `status "Configurações salvas"`; o site passa a exibir o número | @US-015-S01 |
| Formulário | O mesmo campo com um número novo, "(21) 98765-4321" | Todos os anúncios publicados e os dois botões passam a usar o número novo | @US-015-S02 |
| Formulário | O mesmo campo com "11912345678" | Aceita sem formatação; o site mostra o número formatado | @US-015-S03 |
| Formulário | `alert "Informe um número com DDD, por exemplo (11) 91234-5678"` | Bloqueia número inválido; o número em uso não muda | @US-015-S04 |
| Formulário | `alert "O telefone é obrigatório"` | Bloqueia número vazio; o número em uso não muda | @US-015-S05 |
| Página inteira | `alert "Você não tem permissão para acessar esta página"` | O Redator não vê o campo | @US-015-S06 |
| Topo da página | Aviso "O telefone/WhatsApp do site ainda não foi configurado…" | Explica por que a publicação está bloqueada (ref. cruzada) | @US-010-S08 |
