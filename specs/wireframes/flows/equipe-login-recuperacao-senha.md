# Fluxo: Equipe, entrar no painel e recuperar a senha — @US-006, @US-007, @US-014

> **Em resumo:** a equipe entra com e-mail e senha. No primeiro acesso troca a senha provisória; quem esquece a senha pede um link por e-mail (válido por 1 hora e de uso único). Todas as respostas de erro são as mesmas, para não revelar quais e-mails existem. Este fluxo depende da suposição S6 da SPEC (recuperação por e-mail).

## Jornada — entrar e sair (Mermaid)

```mermaid
flowchart TD
    Start(["Pessoa da equipe abre a página de entrada"]) --> Form["Informa e-mail e senha e clica em Entrar"]
    Form --> Valida{"Credenciais válidas e conta ativa?"}
    Valida -->|"não @US-006-S04 @US-006-S05"| Erro["E-mail ou senha inválidos, ou conta desativada; senha volta vazia"]
    Erro --> Tent{"5 falhas em 15 minutos?"}
    Tent -->|"não"| Form
    Tent -->|"sim @US-006-S06"| Bloq["Muitas tentativas. Tente novamente em alguns minutos."]
    Bloq -->|"espera o período"| Form
    Valida -->|"sim"| Prov{"Entrou com senha provisória?"}
    Prov -->|"sim @US-006-S09"| Troca["Defina sua nova senha"]
    Troca -->|"senha nova válida"| Papel{"Papel da pessoa"}
    Prov -->|"não"| Papel
    Papel -->|"Redator @US-006-S01"| Meus["Meus anúncios"]
    Papel -->|"Administrador @US-006-S02"| Fila["Fila de revisão"]
    Meus -->|"clica em Sair @US-006-S03"| Sai["Volta à página de entrada; Voltar do navegador não mostra o painel"]
    Fila -->|"clica em Sair @US-006-S03"| Sai
    Meus -->|"30 minutos parado @US-006-S08"| Expira["Entrada com aviso: Sua sessão expirou. Entre novamente."]
    Fila -->|"30 minutos parado @US-006-S08"| Expira
    Expira --> Form
    Meus -->|"abre página do Administrador @US-006-S10"| Negado["Você não tem permissão para acessar esta página"]

    Fora(["Abre uma página do painel sem estar logado"]) -->|"@US-006-S07"| Form
    Papel -->|"tinha tentado abrir outra página do painel @US-006-S07"| Volta["Cai na página que tentou abrir"]
```

## Jornada — recuperar a senha (Mermaid)

```mermaid
flowchart TD
    Esq(["Clica em Esqueci minha senha na página de entrada"]) --> Pede["Informa o e-mail e clica em Enviar @US-007-S01"]
    Pede --> Neutra["Se o e-mail estiver cadastrado, enviaremos as instruções"]
    Neutra --> Existe{"E-mail de conta ativa?"}
    Existe -->|"sim @US-007-S01"| Email["Chega um e-mail com o link de nova senha"]
    Existe -->|"não ou conta desativada @US-007-S03"| Nada["Nenhum e-mail é enviado; a tela mostrou a mesma mensagem"]
    Email --> Abre["Abre o link"]
    Abre --> Link{"Estado do link"}
    Link -->|"vencido, mais de 1 hora @US-007-S04"| Venc["Este link expirou, botão Pedir novo link"]
    Link -->|"já usado @US-007-S05"| Usado["Este link já foi usado, botão Pedir novo link"]
    Venc --> Pede
    Usado --> Pede
    Link -->|"válido"| Nova["Definir nova senha: informa duas vezes e clica em Salvar senha"]
    Nova -->|"não cumpre a política @US-007-S06"| Fraca["Lista dos requisitos que faltam; senha não muda"]
    Nova -->|"senhas diferentes @US-007-S07"| Dif["As senhas não coincidem; senha não muda"]
    Fraca --> Nova
    Dif --> Nova
    Nova -->|"senha válida @US-007-S02"| Ok["Página de entrada: Senha alterada. Entre com a nova senha."]
    Ok --> Entra(["Entra com a nova senha; a antiga não funciona mais"])
```

## Notas

- **Conta nova:** o Administrador cria a conta com senha provisória (@US-014-S01); no primeiro acesso a pessoa é obrigada a trocá-la e a senha provisória deixa de valer (@US-006-S09).
- **Conta desativada:** não entra (@US-006-S05, @US-014-S03) e não recebe e-mail de redefinição (regra de negócio da @US-007); reativar (@US-014-S04) devolve o acesso.
- **Mensagens iguais de propósito:** a tela de entrada e a de recuperação nunca dizem se um e-mail está cadastrado. O teste automatizado deve conferir que o texto é idêntico nos dois casos.
- **Redirecionamento:** depois de entrar, quem tentou abrir uma página do painel cai nela (@US-006-S07); quem entrou pela página de entrada vai a "Meus anúncios" (Redator) ou à "Fila de revisão" (Administrador).
