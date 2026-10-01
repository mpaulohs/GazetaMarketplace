# ADR-009: E-mail de redefinição de senha pelo SendGrid

**Date**: 2026-09-30
**Status**: Accepted

> **Em resumo:** o único e-mail do sistema, o link de redefinição de senha da equipe (US-007), é enviado pela API do SendGrid (plano gratuito, 100 e-mails por dia). A chave fica em variável de ambiente. Se o envio falhar, a pessoa vê a mesma mensagem neutra e o Administrador pode redefinir a senha manualmente — alternativa que ainda precisa entrar no SPEC.

## Context
- US-007 (recuperar senha), NFR-09 (link de 1 h, uso único), S6 (recuperação por e-mail; alternativa: o Administrador redefine a senha).
- Decisão do Product Owner (2026-09-30): SendGrid, sem depender do SMTP do SmarterASP (limites do plano compartilhado) nem de um SMTP da Gazeta (pode não existir).
- NFR-14: nenhum segredo no repositório. NFR-18: e-mails não aparecem em texto claro nos logs.

## Options Considered
| Option | Pros | Cons |
|--------|------|------|
| **A. API HTTP do SendGrid, chamada com `HttpClient`** | Entrega confiável; plano gratuito suficiente (poucos e-mails por mês); sem pacote novo | Conta externa; domínio remetente precisa de autenticação (SPF/DKIM) |
| B. SDK oficial do SendGrid (NuGet) | Menos código de integração | Pacote a mais para aprovar e manter, para uma única chamada |
| C. SMTP do SmarterASP | Incluído no plano | Limites e reputação de IP compartilhado; entrega menos confiável |
| D. Sem e-mail: só redefinição pelo Administrador | Nenhuma integração | Pior experiência; a US-007 deixaria de existir |

## Decision
Adopt **Option A** because atende à US-007 com a entrega mais confiável entre as opções sem custo e dispensa um pacote novo para uma única chamada de API.

## Consequences
**Positive**: e-mail entregue sem depender da hospedagem; troca de provedor isolada em `IEmailSender`; testes com um remetente falso.
**Negative**: dependência de uma conta externa; o domínio remetente precisa ser configurado no DNS.
**Risks**: chave revogada ou limite diário atingido. Mitigação: falha registrada no log com o `traceId`; mensagem ao usuário sempre neutra; redefinição manual pelo Administrador (AR-11).

## v2 Upgrade Trigger
Revisit this decision when **any** of the following becomes true:
- O sistema passar a enviar outros e-mails (avisos de publicação, contato) ou mais de 100 por dia.
- A Gazeta passar a ter um serviço de e-mail corporativo com API.

## Implementation Notes
- `IEmailSender` (Core) com `SendPasswordResetAsync(to, link, cancellationToken)`; implementação `SendGridEmailSender` (Infrastructure) com `HttpClient` tipado: `POST https://api.sendgrid.com/v3/mail/send`, cabeçalho `Authorization: Bearer <SendGrid__ApiKey>`, tempo limite de 10 s.
- Configuração: `SendGrid__ApiKey` e `SendGrid__FromEmail` por variável de ambiente (ADR-011), validadas na inicialização em produção; em desenvolvimento, um `IEmailSender` que grava o e-mail no log (sem a chave).
- Domínio remetente autenticado no SendGrid (SPF e DKIM no DNS do domínio da Gazeta — AR-12).
- Texto do e-mail em português, com o link `/equipe/redefinir?token=…` válido por 1 hora (ADR-003).
- A resposta de "Esqueci minha senha" é a mesma, exista ou não a conta, e mesmo se o envio falhar (não revela contas).
- Logs: destinatário mascarado (`m***@exemplo.com.br`); o token e o link nunca vão para o log.
- Sem SDK do SendGrid: remover o pacote da lista da AR-06.

> **Atualização (2026-09-30):** a alternativa de redefinição pelo Administrador entrou no SPEC v1.2 como cenário US-014-S10; a AR-11 está resolvida.
