# Sequência: login e recuperação de senha

> **Em resumo:** a pessoa da equipe entra com e-mail e senha; o servidor aplica limite por IP, bloqueio de conta e troca obrigatória de senha. Quem esqueceu a senha recebe um link de 1 hora pelo SendGrid; a resposta é sempre a mesma, exista ou não a conta. Base: `specs/wireframes/flows/equipe-login-recuperacao-senha.md`, US-006, US-007, ADR-003, ADR-009.

```text
 ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
 │    Pessoa   │    │ Site (Conta)│    │   Identity  │    │    Banco    │    │   SendGrid  │
 └──────┬──────┘    └──────┬──────┘    └──────┬──────┘    └──────┬──────┘    └──────┬──────┘
        │ 1. POST entrar   │                  │                  │                  │
        │─────────────────▶│                  │                  │                  │
        │                  │─┐ 2. limite: 5 por 15 min por IP (NFR-06)              │
        │                  │◀┘                │                  │                  │
        │                  │                  │                  │                  │
  ╔═ limite atingido → 429, "Muitas tentativas. Tente em alguns minutos."
  ╚═
        │                  │ 3. PasswordSignIn│                  │                  │
        │                  │─────────────────▶│                  │                  │
        │                  │                  │ 4. usuário, hash, bloqueio          │
        │                  │                  │─────────────────▶│                  │
        │                  │                  │                  │                  │
  ╔═ senha errada → mensagem genérica; 5 falhas → conta bloqueada 15 min
  ╠═ conta desativada → mesma mensagem genérica, sem revelar o motivo
  ╚═
        │                  │ 5. sucesso + cookie                 │                  │
        │                  │◀─────────────────│                  │                  │
        │                  │                  │                  │                  │
  ╔═ MustChangePassword → "Defina sua nova senha" antes do painel (US-006)
  ╚═
        │ 6. Painel (Fila ou Meus anúncios)   │                  │                  │
        │◀─────────────────│                  │                  │                  │
        │                  │                  │                  │                  │
        │ 7. POST esqueci a senha             │                  │                  │
        │─────────────────▶│                  │                  │                  │
        │                  │ 8. gera token (1 h)                 │                  │
        │                  │─────────────────▶│                  │                  │
        │                  │ 9. POST /v3/mail/send               │                  │
        │                  │───────────────────────────────────────────────────────▶│
        │                  │                  │                  │                  │
  ╔═ conta não existe → nada é enviado; mesma resposta
  ╠═ SendGrid falha → log Error com traceId; mesma resposta (redefinição pelo Administrador: US-014-S10)
  ╚═
        │ 10. "Se o e-mail existir, enviamos um link"            │                  │
        │◀─────────────────│                  │                  │                  │
        │ 11. GET/POST redefinir?token        │                  │                  │
        │─────────────────▶│                  │                  │                  │
        │                  │ 12. ResetPassword│                  │                  │
        │                  │─────────────────▶│                  │                  │
        │                  │                  │                  │                  │
  ╔═ vencido (> 1 h) ou já usado → "Link vencido" / "Link já usado" (US-007)
  ╚═
        │                  │                  │ 13. novo hash + SecurityStamp       │
        │                  │                  │─────────────────▶│                  │
        │ 14. "Senha alterada" → entrar       │                  │                  │
        │◀─────────────────│                  │                  │                  │
        │                  │                  │                  │                  │
```

Legenda: `╔═` abre um caminho alternativo (falha ou exceção), `╠═` outro caminho alternativo, `╚═` volta ao caminho principal. Números seguem a ordem das mensagens.

## Ramos de falha

| Situação | O que acontece | Referência |
|---|---|---|
| Excesso de tentativas por IP | 429; a tela pede para esperar | NFR-06 |
| 5 senhas erradas na mesma conta | Conta bloqueada por 15 min; mensagem genérica | NFR-06, ADR-003 |
| Conta desativada | Não entra; mensagem genérica; sessões abertas caem em até 5 min (validação do SecurityStamp) | US-014, NFR-08 |
| Sessão sem uso por 30 min | Próxima ação leva ao login | NFR-08, US-006 |
| E-mail inexistente em "Esqueci minha senha" | Mesma resposta de sucesso; nada é enviado | US-007, ADR-003 |
| SendGrid indisponível ou chave inválida | Log Error com traceId e destinatário mascarado; mesma resposta ao usuário; alternativa: Administrador redefine a senha (US-014-S10) | ADR-009 |
| Link vencido ou já usado | Telas "Link vencido" e "Link já usado", com "Pedir novo link" | US-007, NFR-09 |
| Nova senha fora da política | Mensagens por regra de senha; senha não muda | NFR-07 |
