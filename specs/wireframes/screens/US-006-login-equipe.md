# Wireframe: Entrar e sair do painel da equipe — @US-006

> **Em resumo:** a equipe (redatores e administradores) entra no painel com e-mail e senha; o visitante do site não tem conta. Este arquivo desenha a página de entrada, a tela "Defina sua nova senha" (primeiro acesso), o menu do painel para cada papel e a mensagem de acesso negado. Todas as mensagens de erro de entrada são as mesmas, para não revelar quais e-mails existem.

**Evidência da SPEC:** `specs/SPEC.md` → US-006 (cenários S01 a S10).

## Layout — página de entrada

```text
┌──────────────────────────────────────────────────────────┐
│ GazetaMarketplace · Área da equipe                       │
├──────────────────────────────────────────────────────────┤
│ Entrar                                                   │
│ E-mail *                                                 │
│ [ ana.souza@exemplo.com.br                 ]             │
│ Senha *                                                  │
│ [ ••••••••                                 ]             │
│ [ Entrar ]                                               │
│ Esqueci minha senha                                      │
└──────────────────────────────────────────────────────────┘
```

Em 320 px o cartão ocupa a largura toda, com margem de 16 px; nada muda no conteúdo.

## Layout — "Defina sua nova senha" (primeiro acesso)

```text
┌──────────────────────────────────────────────────────────┐
│ GazetaMarketplace · Área da equipe                       │
├──────────────────────────────────────────────────────────┤
│ Defina sua nova senha                                    │
│ Você entrou com uma senha provisória.                    │
│ Escolha uma senha nova para continuar.                   │
│ Nova senha *                                             │
│ [ ••••••••••                               ]             │
│ Confirmar nova senha *                                   │
│ [ ••••••••••                               ]             │
│ A senha precisa ter 8 caracteres ou mais,                │
│ maiúscula, minúscula, número e símbolo.                  │
│ [ Salvar senha ]                                         │
└──────────────────────────────────────────────────────────┘
```

## Layout — menu do painel por papel

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Redator                                                                          │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Gazeta · Painel                                    Ana Souza (Redator)  [ Sair ] │
│ [Meus anúncios]                                                                  │
└──────────────────────────────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Administrador                                                                    │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Gazeta · Painel                                 Marcos (Administrador)  [ Sair ] │
│ [Anúncios]  [Categorias]  [Usuários]  [Configurações]                            │
└──────────────────────────────────────────────────────────────────────────────────┘
```

Depois de entrar, o Redator vai para "Meus anúncios" (`US-012-painel-anuncios.md`) e o Administrador vai para a "Fila de revisão" (`US-010-revisar-anuncio.md`). Em 320 px o menu vira um botão "Menu" que abre e fecha a lista de itens; o nome da pessoa e o botão "Sair" ficam dentro dele.

## Estados

| Estado | Cenário | O que a pessoa vê |
|---|---|---|
| Padrão | @US-006-S01, @US-006-S02 | Formulário de entrada; depois de entrar, o menu do painel do papel dela. |
| Sair | @US-006-S03 | Volta à página de entrada; o botão Voltar do navegador não mostra o painel. |
| Credencial inválida | @US-006-S04 | "E-mail ou senha inválidos, ou conta desativada"; o campo de senha volta vazio; continua na entrada. |
| Conta desativada | @US-006-S05 | A mesma mensagem, sem entrar. |
| Muitas tentativas | @US-006-S06 | "Muitas tentativas. Tente novamente em alguns minutos." mesmo com a senha certa. |
| Voltar para onde estava | @US-006-S07 | Ao abrir uma página do painel sem estar logado, vai para a entrada e, após entrar, cai na página pedida. |
| Sessão expirada | @US-006-S08 | Após 30 minutos parado, vai para a entrada com "Sua sessão expirou. Entre novamente." |
| Primeiro acesso | @US-006-S09 | Tela "Defina sua nova senha" antes de ver o painel; a senha provisória deixa de valer. |
| Acesso negado | @US-006-S10 | "Você não tem permissão para acessar esta página"; o conteúdo da página não aparece. |
| Carregando | — | O botão "Entrar" mostra "Entrando…" e fica desativado enquanto a resposta não chega. Sem cenário na SPEC. |
| Vazio e sem resultado | — | Não se aplicam. |

```text
┌──────────────────────────────────────────────────────────┐
│ Erro de credencial (campo de senha volta vazio)          │
├──────────────────────────────────────────────────────────┤
│ ! E-mail ou senha inválidos, ou conta desativada         │
│ E-mail *                                                 │
│ [ ana.souza@exemplo.com.br                 ]             │
│ Senha *                                                  │
│ [                                          ]             │
│ [ Entrar ]                                               │
└──────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────┐
│ Bloqueio por tentativas                                  │
├──────────────────────────────────────────────────────────┤
│ ! Muitas tentativas. Tente novamente em alguns minutos.  │
│ E-mail *   Senha *   [ Entrar ]                          │
└──────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────┐
│ Sessão expirada                                          │
├──────────────────────────────────────────────────────────┤
│ i Sua sessão expirou. Entre novamente.                   │
│ E-mail *   Senha *   [ Entrar ]                          │
└──────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Acesso negado (Redator abrindo "Categorias" pelo endereço)                       │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Gazeta · Painel                                    Ana Souza (Redator)  [ Sair ] │
│ [Meus anúncios]                                                                  │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Você não tem permissão para acessar esta página.                                 │
│ [ Voltar para Meus anúncios ]                                                    │
└──────────────────────────────────────────────────────────────────────────────────┘
```

Esse mesmo painel de "acesso negado" é usado em todas as páginas exclusivas do Administrador (Categorias, Usuários, Configurações, Fila de revisão) e ao abrir anúncio de outro Redator. As telas dessas páginas remetem a este desenho.

## Responsivo

- Formulários em coluna única a partir de 320 px, campos com largura total e altura de toque de pelo menos 44 px.
- O menu do painel usa o componente de navegação recolhível do Bootstrap abaixo de 768 px.

## Acessibilidade

- Cada campo tem `label` visível e `autocomplete` adequado (`username` e `current-password`; `new-password` na troca de senha).
- Mensagens de erro em `role="alert"` acima do formulário; o foco vai para a mensagem depois do envio com erro.
- O erro **não** indica qual dos campos está errado nem se o e-mail existe.
- Requisitos da senha em lista (`ul`) ligada ao campo por `aria-describedby`.
- Ordem de foco: e-mail → senha → Entrar → "Esqueci minha senha".
- Depois de sair, a página do painel não fica guardada no histórico do navegador (@US-006-S03).

## Mapeamento: controle → cenário

| Região da tela | Controle (papel / nome acessível) | Comportamento | Cenário |
|---|---|---|---|
| Entrada | `textbox "E-mail"` + `textbox "Senha"` (senha) + `button "Entrar"` (Redator) | Leva a "Meus anúncios" | @US-006-S01 |
| Entrada | Os mesmos campos (Administrador) | Leva à "Fila de revisão" | @US-006-S02 |
| Menu do painel (Redator) | Nome da pessoa no topo + `button "Sair"`; menus Categorias, Usuários e Configurações ausentes | Mostra "Meus anúncios" e o nome dela | @US-006-S01 |
| Menu do painel (Administrador) | `link "Anúncios"`, `link "Categorias"`, `link "Usuários"`, `link "Configurações"` | Mostra os quatro menus | @US-006-S02 |
| Menu do painel | `button "Sair"` | Encerra a sessão e volta à entrada; o Voltar do navegador não mostra o painel | @US-006-S03 |
| Entrada | `alert "E-mail ou senha inválidos, ou conta desativada"` + campo Senha vazio | Não diz se o e-mail existe; permanece na entrada | @US-006-S04 |
| Entrada | A mesma mensagem (conta desativada) | Mesma resposta da senha errada | @US-006-S05 |
| Entrada | `alert "Muitas tentativas. Tente novamente em alguns minutos."` | Bloqueia novas tentativas até o fim do período | @US-006-S06 |
| Entrada (via endereço direto) | Redirecionamento para a entrada e retorno à página pedida (sem controle próprio) | Depois de entrar, mostra "Meus anúncios" | @US-006-S07 |
| Entrada | `status "Sua sessão expirou. Entre novamente."` | Aparece ao clicar no painel após 30 minutos parado | @US-006-S08 |
| Defina sua nova senha | `textbox "Nova senha"` + `textbox "Confirmar nova senha"` + `button "Salvar senha"` + lista de requisitos | Exige senha nova antes do painel; a provisória não entra mais | @US-006-S09 |
| Acesso negado | `alert "Você não tem permissão para acessar esta página"` + `link "Voltar para Meus anúncios"` | Esconde o conteúdo da página restrita | @US-006-S10 |
| Entrada | `link "Esqueci minha senha"` | Abre a tela de recuperação (ref. cruzada, US-007) | @US-007-S01 |
| Entrada | `link "GazetaMarketplace"` (volta ao site público) | Leva à página inicial | ⚠️ nenhum cenário cobre este controle |
| Menu do painel (mobile) | `button "Menu"` (abre e fecha a lista de itens) | Recolhe a navegação em 320 px | ⚠️ nenhum cenário cobre este controle |
