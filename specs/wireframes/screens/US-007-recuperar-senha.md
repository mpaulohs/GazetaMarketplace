# Wireframe: Recuperar senha esquecida — @US-007

> **Em resumo:** quem esqueceu a senha informa o e-mail e recebe um link válido por 1 hora e de uso único; pelo link, define uma senha nova. A resposta ao pedido é sempre a mesma, exista ou não a conta. Este arquivo cobre duas telas: "Esqueci minha senha" e "Definir nova senha", mais as telas de link vencido ou já usado. Depende da suposição S6 da SPEC (recuperação por e-mail); se o Product Owner preferir que o administrador redefina senhas, esta história é substituída.

**Evidência da SPEC:** `specs/SPEC.md` → US-007 (cenários S01 a S07).

## Tela 1 — "Esqueci minha senha"

```text
┌──────────────────────────────────────────────────────────┐
│ GazetaMarketplace · Área da equipe                       │
├──────────────────────────────────────────────────────────┤
│ Esqueci minha senha                                      │
│ Informe o e-mail da sua conta. Enviaremos as instruções. │
│ E-mail *                                                 │
│ [ ana.souza@exemplo.com.br                 ]             │
│ [ Enviar ]                                               │
│ Voltar para entrar                                       │
└──────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────┐
│ Confirmação neutra (mesma resposta para qualquer e-mail) │
├──────────────────────────────────────────────────────────┤
│ i Se o e-mail estiver cadastrado,                        │
│   enviaremos as instruções                               │
│ Voltar para entrar                                       │
└──────────────────────────────────────────────────────────┘
```

## Tela 2 — "Definir nova senha" (aberta pelo link do e-mail)

```text
┌──────────────────────────────────────────────────────────┐
│ GazetaMarketplace · Área da equipe                       │
├──────────────────────────────────────────────────────────┤
│ Definir nova senha                                       │
│ Nova senha *                                             │
│ [ ••••••••••                               ]             │
│ Confirmar nova senha *                                   │
│ [ ••••••••••                               ]             │
│ A senha precisa ter 8 caracteres ou mais,                │
│ maiúscula, minúscula, número e símbolo.                  │
│ [ Salvar senha ]                                         │
└──────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────┐
│ Erro: requisitos que faltam (senha "abc123")             │
├──────────────────────────────────────────────────────────┤
│ ! A senha não cumpre os requisitos. Falta:               │
│   - mínimo de 8 caracteres                               │
│   - letra maiúscula                                      │
│   - símbolo                                              │
│ Nova senha *                                             │
│ [                                          ]             │
│ [ Salvar senha ]                                         │
└──────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────┐
│ Erro: confirmação diferente                              │
├──────────────────────────────────────────────────────────┤
│ Confirmar nova senha *                                   │
│ [ ••••••••                                 ]             │
│ ! As senhas não coincidem                                │
│ [ Salvar senha ]                                         │
└──────────────────────────────────────────────────────────┘
```

## Tela 3 — link vencido ou já usado

```text
┌──────────────────────────────────────────────────────────┐
│ Link vencido                                             │
├──────────────────────────────────────────────────────────┤
│ Este link expirou                                        │
│ [ Pedir novo link ]                                      │
└──────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────┐
│ Link já usado                                            │
├──────────────────────────────────────────────────────────┤
│ Este link já foi usado                                   │
│ [ Pedir novo link ]                                      │
└──────────────────────────────────────────────────────────┘
```

## Tela 4 — depois de salvar a nova senha

```text
┌──────────────────────────────────────────────────────────┐
│ Página de entrada com aviso de sucesso                   │
├──────────────────────────────────────────────────────────┤
│ i Senha alterada. Entre com a nova senha.                │
│ E-mail *                                                 │
│ [                                          ]             │
│ Senha *                                                  │
│ [                                          ]             │
│ [ Entrar ]                                               │
└──────────────────────────────────────────────────────────┘
```

## Estados

| Estado | Cenário | O que a pessoa vê |
|---|---|---|
| Pedido enviado | @US-007-S01 | "Se o e-mail estiver cadastrado, enviaremos as instruções"; um e-mail com o link chega à caixa de entrada. |
| Senha alterada | @US-007-S02 | Página de entrada com "Senha alterada. Entre com a nova senha."; a senha antiga deixa de funcionar. |
| E-mail não cadastrado | @US-007-S03 | A mesma mensagem do pedido; nenhum e-mail é enviado. |
| Link vencido | @US-007-S04 | "Este link expirou" e o botão "Pedir novo link". |
| Link já usado | @US-007-S05 | "Este link já foi usado" e o botão "Pedir novo link". |
| Senha fraca | @US-007-S06 | Lista dos requisitos que faltam; a senha não muda. |
| Confirmação diferente | @US-007-S07 | "As senhas não coincidem"; a senha não muda. |
| Carregando | — | "Enviar" e "Salvar senha" mostram "Enviando…" e ficam desativados. Sem cenário na SPEC. |
| Erro do servidor | — | Não previsto na SPEC (ver Lacunas no `README.md`). |
| Vazio e sem resultado | — | Não se aplicam. |

## Responsivo

- Coluna única desde 320 px; campos e botões com largura total e altura de toque de pelo menos 44 px.

## Acessibilidade

- Cada campo tem `label` visível; `autocomplete="email"` no pedido e `new-password` na nova senha.
- A confirmação neutra e os erros usam `role="status"` e `role="alert"`, respectivamente, e recebem o foco depois do envio.
- Os requisitos que faltam formam uma lista (`ul`) ligada ao campo por `aria-describedby`.
- A mensagem do pedido é idêntica para e-mail cadastrado ou não, para não revelar quais contas existem.

## Mapeamento: controle → cenário

| Região da tela | Controle (papel / nome acessível) | Comportamento | Cenário |
|---|---|---|---|
| Esqueci minha senha | `link "Esqueci minha senha"` (na entrada, ref. cruzada) + `textbox "E-mail"` + `button "Enviar"` | Envia o pedido; mostra a confirmação neutra e dispara o e-mail com o link | @US-007-S01 |
| Esqueci minha senha | `status "Se o e-mail estiver cadastrado, enviaremos as instruções"` | Mesma resposta para e-mail que não existe; nenhum e-mail sai | @US-007-S03 |
| Definir nova senha | `textbox "Nova senha"` + `textbox "Confirmar nova senha"` + `button "Salvar senha"` | Troca a senha e leva à entrada com aviso de sucesso | @US-007-S02 |
| Entrada (após salvar) | `status "Senha alterada. Entre com a nova senha."` | Confirma a troca; a senha antiga deixa de valer | @US-007-S02 |
| Link vencido | Mensagem "Este link expirou" + `button "Pedir novo link"` | Volta à tela "Esqueci minha senha" | @US-007-S04 |
| Link usado | Mensagem "Este link já foi usado" + `button "Pedir novo link"` | Volta à tela "Esqueci minha senha" | @US-007-S05 |
| Definir nova senha | `alert` com a lista de requisitos que faltam | Não altera a senha | @US-007-S06 |
| Definir nova senha | `alert "As senhas não coincidem"` junto do campo de confirmação | Não altera a senha | @US-007-S07 |
| Esqueci minha senha e Definir nova senha | `link "Voltar para entrar"` | Retorna à página de entrada | ⚠️ nenhum cenário cobre este controle |
