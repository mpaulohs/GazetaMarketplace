# Wireframe: Gerenciar usuários da equipe — @US-014

> **Em resumo:** página do Administrador para criar contas da equipe (com uma senha provisória, que a pessoa troca no primeiro acesso), mudar o papel de cada um (Redator ou Administrador), desativar ou reativar contas e redefinir a senha de alguém que não conseguiu recuperá-la por e-mail. Não existe excluir conta, para os anúncios manterem o autor. Sempre precisa restar ao menos um Administrador ativo, e ninguém desativa a própria conta.

**Evidência da SPEC:** `specs/SPEC.md` → US-014 (cenários S01 a S10).

## Layout — lista de usuários (desktop)

```text
┌──────────────────────────────────────────────────────────────────────────────────────────┐
│ Gazeta · Painel                                   Marcos Paulo (Administrador)  [ Sair ] │
│ [Anúncios]  [Categorias]  [Usuários]  [Configurações]                                    │
├──────────────────────────────────────────────────────────────────────────────────────────┤
│ Usuários da equipe                                                      [ Novo usuário ] │
│ Nome          E-mail                     Papel          Situação                         │
│ Marcos Paulo  marcos@exemplo.com.br      Administrador  Ativa                            │
│   Ações: [Editar] [Desativar]      (a própria senha: recuperação normal)                 │
│ Ana Souza     ana.souza@exemplo.com.br   Redator        Ativa                            │
│   Ações: [Editar] [Redefinir senha] [Desativar]                                          │
│ Bruno Lima    bruno.lima@exemplo.com.br  Redator        Desativada                       │
│   Ações: [Editar] [Reativar]                                                             │
└──────────────────────────────────────────────────────────────────────────────────────────┘
```

## Layout — formulário de novo usuário e de edição

```text
┌──────────────────────────────────────────────────────────┐
│ Novo usuário                                             │
├──────────────────────────────────────────────────────────┤
│ Nome *                                                   │
│ [ Ana Souza                                            ] │
│ E-mail *                                                 │
│ [ ana.souza@exemplo.com.br                             ] │
│ Papel *                                                  │
│ ( ) Redator     ( ) Administrador                        │
│ Senha provisória *                                       │
│ [ ••••••••••                                           ] │
│ Informe a senha à pessoa fora do sistema. Ela precisará  │
│ trocá-la no primeiro acesso. Requisitos: 8 caracteres ou │
│ mais, maiúscula, minúscula, número e símbolo.            │
│ [ Cancelar ]   [ Salvar ]                                │
└──────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────┐
│ Editar usuário (mudar o papel)                           │
├──────────────────────────────────────────────────────────┤
│ Nome: Ana Souza                                          │
│ E-mail: ana.souza@exemplo.com.br                         │
│ Papel *                                                  │
│ ( ) Redator     ( ) Administrador                        │
│ [ Cancelar ]   [ Salvar ]                                │
└──────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────┐
│ Confirmar desativação                                    │
├──────────────────────────────────────────────────────────┤
│ Desativar Ana Souza?                                     │
│ Ela deixa de entrar no painel. Os anúncios dela          │
│ continuam existindo com o nome dela como autora.         │
│ [ Cancelar ]   [ Desativar ]                             │
└──────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────┐
│ Redefinir senha de Ana Souza                             │
├──────────────────────────────────────────────────────────┤
│ Senha provisória *                                       │
│ [ ••••••••••                                           ] │
│ Informe a senha à pessoa fora do sistema. Ela precisará  │
│ trocá-la no próximo acesso. A senha atual deixa de       │
│ valer e as sessões abertas dela são encerradas.          │
│ Requisitos: 8 caracteres ou mais, maiúscula, minúscula,  │
│ número e símbolo.                                        │
│ [ Cancelar ]   [ Redefinir senha ]                       │
└──────────────────────────────────────────────────────────┘
```

Depois de confirmar, o topo da lista mostra "Senha de Ana Souza redefinida. Informe a senha provisória a ela fora do sistema." O botão não aparece na linha da própria pessoa nem em contas desativadas.

## Layout — mobile (320 px)

```text
┌──────────────────────────────────────┐
│ Gazeta · Painel             [ Menu ] │
├──────────────────────────────────────┤
│ Usuários da equipe                   │
│ [ Novo usuário ]                     │
│ ┌──────────────────────────────────┐ │
│ │ Ana Souza                        │ │
│ │ ana.souza@exemplo.com.br         │ │
│ │ Redator · Ativa                  │ │
│ │ [ Editar ] [ Desativar ]         │ │
│ │ [ Redefinir senha ]              │ │
│ └──────────────────────────────────┘ │
│ ┌──────────────────────────────────┐ │
│ │ Bruno Lima                       │ │
│ │ bruno.lima@exemplo.com.br        │ │
│ │ Redator · Desativada             │ │
│ │ [ Editar ] [ Reativar ]          │ │
│ └──────────────────────────────────┘ │
└──────────────────────────────────────┘
```

## Estados

| Estado | Cenário | O que o Administrador vê |
|---|---|---|
| Criar conta | @US-014-S01 | "Ana Souza" aparece com papel "Redator" e situação "Ativa"; no primeiro acesso ela é levada a definir nova senha (tela em `US-006-login-equipe.md`). |
| Mudar papel | @US-014-S02 | A lista mostra "Administrador"; no próximo acesso ela vê Categorias, Usuários e Configurações. |
| Desativar | @US-014-S03 | Situação "Desativada"; os 3 anúncios dela continuam com o nome dela; ela recebe "E-mail ou senha inválidos, ou conta desativada" ao tentar entrar. |
| Reativar | @US-014-S04 | Situação "Ativa"; ela consegue entrar. |
| E-mail já cadastrado | @US-014-S05 | "Já existe um usuário com este e-mail"; nada é criado. |
| Senha provisória fraca | @US-014-S06 | Lista dos requisitos que faltam (8 caracteres, maiúscula, minúscula, símbolo); nada é criado. |
| E-mail inválido | @US-014-S07 | "Informe um e-mail válido"; nada é criado. |
| Desativar a própria conta | @US-014-S08 | "Você não pode desativar a sua própria conta"; a conta continua "Ativa". |
| Último administrador | @US-014-S09 | "Deve existir ao menos um administrador ativo"; o papel continua "Administrador". |
| Redefinir senha | @US-014-S10 | Diálogo com a senha provisória; depois, "Senha de Ana Souza redefinida…"; no próximo acesso ela é levada a "Defina sua nova senha" (tela em `US-006-login-equipe.md`). Senha fraca mostra a mesma lista de requisitos da criação de conta. |
| Carregando | — | Esqueleto das linhas. Sem cenário na SPEC. |
| Erro | — | Não previsto na SPEC (ver Lacunas no `README.md`). |
| Vazio e sem resultado | — | Não se aplicam: sempre existe ao menos um Administrador. |

## Responsivo

- Em 320 px cada pessoa vira um cartão (nome, e-mail, papel e situação, botões abaixo); o formulário abre em janela de largura total com margem de 16 px; sem rolagem horizontal.
- Botões com área de toque de pelo menos 44 × 44 px.

## Acessibilidade

- Tabela com `th scope="col"`; a situação ("Ativa" ou "Desativada") é texto, não só cor.
- Botões trazem o nome da pessoa: "Desativar Ana Souza", "Reativar Bruno Lima".
- Papel em grupo de rádio (`role="radiogroup"` com legenda "Papel"); campos com `label` visível; erros por campo em `role="alert"` ligados por `aria-describedby`; requisitos da senha em lista.
- O diálogo de desativação é `role="alertdialog"`, foco inicial em "Cancelar".
- O botão traz o nome da pessoa ("Redefinir senha de Ana Souza"); o diálogo de redefinição é `role="dialog"`, com foco inicial no campo da senha e retorno do foco ao botão ao fechar.
- Depois de salvar, um `role="status"` anuncia o resultado ("Usuário criado").

## Mapeamento: controle → cenário

| Região da tela | Controle (papel / nome acessível) | Comportamento | Cenário |
|---|---|---|---|
| Topo da lista | `button "Novo usuário"` + formulário (`textbox "Nome"`, `textbox "E-mail"`, `radiogroup "Papel"`, `textbox "Senha provisória"`, `button "Salvar"`) | Cria a conta ativa; a pessoa troca a senha no primeiro acesso | @US-014-S01 |
| Lista | `button "Editar"` + `radiogroup "Papel"` + `button "Salvar"` | Muda o papel; vale no próximo acesso | @US-014-S02 |
| Lista | `button "Desativar"` + diálogo `button "Desativar"` | Situação "Desativada"; anúncios seguem com o autor | @US-014-S03 |
| Lista | `button "Reativar"` (sem confirmação) | Situação "Ativa" | @US-014-S04 |
| Formulário | `alert "Já existe um usuário com este e-mail"` | Bloqueia o e-mail repetido | @US-014-S05 |
| Formulário | `alert` com a lista de requisitos que faltam | Bloqueia a senha provisória fraca | @US-014-S06 |
| Formulário | `alert "Informe um e-mail válido"` | Bloqueia e-mail sem formato válido | @US-014-S07 |
| Lista (linha da própria pessoa) | `button "Desativar"` → `alert "Você não pode desativar a sua própria conta"` | Mantém a conta ativa | @US-014-S08 |
| Formulário (último administrador) | `radiogroup "Papel"` → `alert "Deve existir ao menos um administrador ativo"` | Mantém o papel de Administrador | @US-014-S09 |
| Lista | `button "Redefinir senha de Ana Souza"` + diálogo (`textbox "Senha provisória"`, `button "Redefinir senha"`) + `status "Senha de Ana Souza redefinida…"` | Troca a senha, encerra as sessões dela e exige nova senha no próximo acesso | @US-014-S10 |
| Formulário e diálogo | `button "Cancelar"` | Fecha sem salvar ou sem desativar | ⚠️ nenhum cenário cobre este controle |
