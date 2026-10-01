# ADR-003: ASP.NET Core Identity com cookie

**Date**: 2026-09-30
**Status**: Accepted

> **Em resumo:** só a equipe tem conta. O login usa o ASP.NET Core Identity com cookie de sessão, dois papéis (Administrador e Redator), bloqueio após tentativas erradas, troca obrigatória de senha no primeiro acesso e link de redefinição de 1 hora e uso único. O primeiro Administrador nasce de variáveis de ambiente na primeira publicação.

## Context
- US-006 (entrar e sair), US-007 (recuperar senha) e US-014 (gerenciar usuários); papéis Administrador e Redator (D-03, matriz de permissões).
- NFR-06 (5 falhas em 15 min), NFR-07 (política de senha e hash), NFR-08 (sessão de 30 min sem uso), NFR-09 (link de 1 h, uso único), NFR-11 (antiforgery), NFR-13 (controle de acesso no servidor).
- S6 (contas criadas pelo Administrador com senha provisória) e S17 (primeiro Administrador fora da interface).
- O site é renderizado no servidor (Razor); não há aplicativo nem API pública que precise de token.
- `rules/tech-stack.md` indica JWT + Keycloak como padrão e ASP.NET Identity como alternativa.

## Options Considered
| Option | Pros | Cons |
|--------|------|------|
| **A. ASP.NET Core Identity + cookie** | Pronto para senha com hash, bloqueio, *security stamp*, tokens de redefinição e papéis; integra com antiforgery e Razor; roda no mesmo processo | Tabelas do Identity no banco; menos flexível para login social (não pedido) |
| B. JWT + Keycloak (padrão do kit) | Padrão para APIs e múltiplos clientes | Um servidor Keycloak a mais, impossível na hospedagem compartilhada; JWT não serve bem a páginas Razor com antiforgery |
| C. Autenticação própria (tabela de usuários + cookie) | Controle total | Reescrever hash, bloqueio, tokens e invalidação de sessão: risco de segurança sem ganho |

## Decision
Adopt **Option A** because cobre NFR-06 a NFR-09 com recursos prontos e testados, não exige servidor extra (inviável no SmarterASP) e combina com páginas Razor e antiforgery (NFR-11).

## Consequences
**Positive**: política de senha, bloqueio, invalidação de sessão ao desativar usuário e redefinição de senha prontos; menos código de segurança próprio.
**Negative**: esquema do Identity no banco; personalizações (nome completo, `IsActive`, `MustChangePassword`) exigem estender o usuário.
**Risks**: chaves do Data Protection perdidas a cada reciclagem do IIS derrubariam todas as sessões e invalidariam tokens. Mitigação: chaves persistidas fora da raiz do site (ADR-011, AR-01).

## v2 Upgrade Trigger
Revisit this decision when **any** of the following becomes true:
- O público passar a ter contas (hoje fora do escopo, D-06).
- Surgir um segundo cliente (aplicativo móvel, integração) que precise de API autenticada por token.
- A Gazeta exigir login único com outro sistema da empresa (SSO).

## Implementation Notes
- Pacote `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (aprovação: AR-06); usuário estendido com `FullName`, `IsActive`, `MustChangePassword`.
- Senha (NFR-07): `RequiredLength = 8`, `RequireUppercase`, `RequireLowercase`, `RequireDigit`, `RequireNonAlphanumeric`; hash padrão do Identity.
- Bloqueio (NFR-06): `MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 15 min`, mais o limitador por IP na rota de login (5 por 15 min, `ARCHITECTURE.md` §7).
- Sessão (NFR-08): cookie `HttpOnly`, `Secure`, `SameSite=Lax`, `ExpireTimeSpan = 30 min`, `SlidingExpiration = true`; `SecurityStampValidatorOptions.ValidationInterval = 5 min` (usuário desativado perde o acesso em até 5 min).
- Redefinição (NFR-09): `DataProtectionTokenProviderOptions.TokenLifespan = 1 h`; ao redefinir, o *security stamp* muda e o mesmo link deixa de valer. A resposta de "Esqueci minha senha" é sempre a mesma, exista ou não o e-mail (não revela contas).
- Primeiro acesso (S6): filtro global na área `Painel` que redireciona para "Defina sua nova senha" enquanto `MustChangePassword = true`.
- Primeiro Administrador (S17): na inicialização, se não existir nenhum Administrador **e** as variáveis `Bootstrap__AdminEmail` e `Bootstrap__AdminPassword` existirem, criar o usuário com `MustChangePassword = true`; registrar no log (sem a senha); remover as variáveis depois da primeira publicação (ADR-011).
- Autorização (NFR-13): políticas `Administrador` e `Redator` nas áreas e ações; a checagem de autoria fica no serviço de aplicação, não só no controller.
