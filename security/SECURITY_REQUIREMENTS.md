# Security Requirements: GazetaMarketplace v1

> **Em resumo:** a lista de controles de segurança que a construção precisa implementar, em 6 áreas. Cada item aponta o arquivo, o atributo, o cabeçalho ou a opção exata, para o `/build` copiar para o código. Itens `[N/A]` trazem o motivo. A pilha usa **cookie do ASP.NET Core Identity** (ADR-003); por isso os itens de JWT do modelo padrão não se aplicam.

## Security Requirements

### 1. Authentication
- [x] Cookie de sessão de 30 min deslizantes — `Program.cs`: `ConfigureApplicationCookie(o => { o.Cookie.HttpOnly = true; o.Cookie.SecurePolicy = CookieSecurePolicy.Always; o.Cookie.SameSite = SameSiteMode.Lax; o.ExpireTimeSpan = TimeSpan.FromMinutes(30); o.SlidingExpiration = true; })` (Task 1.1)
- [x] Revalidação do usuário a cada 5 min — `SecurityStampValidatorOptions.ValidationInterval = TimeSpan.FromMinutes(5)`; usuário desativado perde o acesso em até 5 min (Task 1.1, 1.3)
- [x] Hash de senha do Identity — serviço `IPasswordHasher<AppUser>` padrão (PBKDF2-HMAC-SHA512); **`PasswordHasherOptions.IterationCount` não pode ser reduzido abaixo do padrão**. Escolha permitida por `rules/security.md` ("BCrypt ou `IPasswordHasher` do Identity") (Task 1.1)
- [x] Política de senha — `IdentityOptions.Password`: `RequiredLength = 8`, `RequireUppercase`, `RequireLowercase`, `RequireDigit`, `RequireNonAlphanumeric = true` (Task 1.1, 1.3, 1.4)
- [x] Bloqueio de conta — `IdentityOptions.Lockout`: `MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15)`, `AllowedForNewUsers = true`; **redefinir a senha com sucesso zera o bloqueio** (RC-12) (Task 1.1, 1.4)
- [x] Token de redefinição de 1 hora e uso único — `DataProtectionTokenProviderOptions.TokenLifespan = TimeSpan.FromHours(1)`; ao redefinir, `UserManager.UpdateSecurityStampAsync` (Task 1.4)
- [x] Endereço de retorno do login só local — `Url.IsLocalUrl(returnUrl)` em `AccountController.Entrar` (RC-18) (Task 1.1)
- [x] Mensagens de login e de recuperação genéricas; recuperação com resposta igual e sem esperar o envio (RC-13) (Task 1.1, 1.4)
- [x] Primeiro Administrador por variáveis `Bootstrap__AdminEmail` e `Bootstrap__AdminPassword`, só se não houver Administrador, com `MustChangePassword = true`; Warning no log se as variáveis continuarem (RC-19) (Task 1.2)
- [N/A] JWT, refresh token e rotação — o site usa cookie de sessão do Identity (ADR-003); não há API para outro cliente
- [N/A] MFA para operações sensíveis — fora da v1; risco residual RR-1 aceito; decisão SEC-02 do Product Owner (2026-09-30): não exigir a senha atual do Administrador

### 2. Authorization
- [x] RBAC — políticas `Administrador` e `Redator` em `Program.cs` (`AddAuthorizationBuilder().AddPolicy(...)`); `[Authorize(Policy = "Administrador")]` em toda ação de Fila, Categorias, Usuários e Configurações (Task 1.1, 2.6, 2.7, 4.1)
- [x] Validação de autoria no serviço de aplicação — `AnuncioService` recusa leitura e escrita de anúncio alheio pelo Redator, independentemente do controller (Task 3.1)
- [x] Autoria dentro da consulta Dapper da lista do painel — `WHERE AuthorId = @authorId` para o Redator (Task 4.4)
- [x] Matriz de acesso por reflexão — `MatrizDeAcessoTests` lista toda rota do painel e todo endpoint JSON de escrita; rota nova fora da matriz faz o teste falhar (Task 6.1)
- [x] IDOR — os ids de anúncio são inteiros e aparecem em endereços públicos de anúncios publicados; **a proteção é a checagem de autoria e de situação no servidor, não o segredo do id**. Decisão: GUID não é necessário (Task 3.1, 3.4, 5.2)
- [x] Menor privilégio no banco — conta do site sem `db_owner` quando o provedor oferecer conta separada; migrations aplicadas por script por outra conta (Task 0.2). **SEC-03 (PO, 2026-09-30): assumido o pior caso, com uma conta de permissão total; risco residual RR-9; reavaliar no `/infra`**
- [x] Processo do site com a identidade de menor privilégio do IIS, sem acesso fora das pastas do site (Task 0.2)

### 3. Input Validation
- [x] Validação de toda entrada — FluentValidation em `GazetaMarketplace.Core.Validators` para cada request DTO e ViewModel; aprovação do pacote na AR-06 (Task 3.3, 5.4, 1.3)
- [x] Só consultas parametrizadas — EF Core LINQ; Dapper por `SqlBuilder` com parâmetros nomeados; **proibido** `FromSqlRaw` e concatenação de texto do usuário no SQL (Task 0.6)
- [x] Ordenação dinâmica só por lista permitida — `SqlBuilder.OrderBy(column, direction)` ignora o que não está na lista (Task 0.6, 4.4, 5.4)
- [x] Limites de entrada — termo de busca ≤ 100 caracteres; consulta Dapper com `commandTimeout: 10` (RC-15) (Task 5.4, 4.4)
- [x] Sobrepostagem — ViewModels de edição sem `Status`, `AuthorId`, `PublishedAt` e campos de decisão (RC-14) (Task 3.3)
- [x] Upload de foto — assinatura do arquivo (JPEG `FF D8 FF`, PNG `89 50 4E 47`, GIF `47 49 46 38`, WebP `RIFF….WEBP`, HEIC/HEIF caixa `ftyp`), 10 MB na aplicação e 11 MB no servidor, nome gerado, reprocessamento para WebP sem metadados (RC-1, RC-3) (Task 3.4, 3.5)
- [x] Decodificação com limites — Magick.NET com no máximo 50 milhões de pixels, memória e tempo limitados e política que liga só JPEG, PNG, GIF, WebP e HEIC/HEIF (RC-2) (Task 3.4)
- [x] Caminho de arquivo — só ids numéricos e nomes gerados; caminho final conferido dentro de `PhotoStorage__BasePath` (RC-4) (Task 3.4)
- [x] Saída de texto — Razor codifica por padrão; **proibido `Html.Raw` com texto de usuário**; nenhum `innerHTML` com texto do servidor (RC-17) (Task 0.7)
- [x] Chamadas de saída — só `HttpClient` tipado com `BaseAddress` fixa (`https://viacep.com.br/ws/`, `https://api.sendgrid.com/`); CEP validado como 8 dígitos antes de montar a rota; nenhuma URL vinda do usuário (Task 3.2, 1.4)
- [N/A] Sanitização de HTML de usuário — o sistema não aceita HTML: todo texto é exibido como texto puro (NFR-15)

### 4. Data Protection
- [x] HTTPS obrigatório — `UseHttpsRedirection()` e `UseHsts()` (1 ano) só em produção; certificado do provedor (AR-09). TLS 1.2 ou superior definido pelo IIS do provedor (Task 0.4)
- [x] Dados em repouso — o único dado pessoal é o e-mail da equipe (NFR-19); senha só como hash; chaves do Data Protection em pasta fora da raiz (risco residual RR-2, RR-8) (Task 0.2)
- [x] Nada sensível em log — Serilog com mascaramento de senha, token, link e e-mail (`m***@dominio`); `AuditEntries` guarda ids e valores de negócio, nunca senha (Task 0.3)
- [x] Segredos fora do repositório — variáveis de ambiente em `<aspNetCore><environmentVariables>` do `web.config` publicado; `web.Production.config` e `appsettings.Development.json` no `.gitignore`; `OptionsBuilder.ValidateOnStart()` em produção (RC-20) (Task 0.2)
- [x] Originais de foto com GPS — `_originals/` sem rota e apagado em 30 dias (RC-5) (Task 3.4, 3.6)
- [N/A] Criptografia de campos sensíveis (Always Encrypted) — não há dado pessoal sensível além do e-mail da equipe
- [N/A] Mascaramento de dados pessoais em ambientes de teste — os dados de desenvolvimento e teste são fictícios; o catálogo não tem dado pessoal

### 5. Rate Limiting & Abuse Prevention
- [x] Limite global — `AddRateLimiter` + `GlobalLimiter`: 100 pedidos por minuto por IP no site público (Task 0.4)
- [x] Limite de autenticação — política `auth`: 5 por 15 min por IP em login e em "Esqueci minha senha" (Task 0.4)
- [x] IP do cliente correto — `UseForwardedHeaders` com `ForwardedHeaders.XForwardedFor | XForwardedProto` e `KnownProxies` do provedor (RC-10). **SEC-01 (PO, 2026-09-30):** assumir `X-Forwarded-For`; `KnownProxies` só depois da resposta do SmarterASP; até lá `ForwardLimit = 1` e **não limpar `KnownProxies`/`KnownNetworks`** (senão qualquer cliente forja o IP e escapa do limite); pendência de lançamento (Task 0.4)
- [x] Limite de pedidos de redefinição por e-mail — 3 por hora; Warning aos 80 e-mails por dia (RC-11) (Task 1.4)
- [x] Limite do corpo de requisição — 1 MB global, 11 MB só no envio de foto: `RequestSizeLimit` e `MaxRequestBodySize` (RC-21) (Task 0.4, 3.5)
- [x] Envio de foto — 30 por minuto por usuário e no máximo 2 conversões simultâneas (RC-6) (Task 3.5)
- [x] Tempo de consulta — EF Core `CommandTimeout(30)`; Dapper `commandTimeout: 10` na busca e na lista do painel (RC-15) (Task 0.6, 5.4, 4.4)
- [N/A] CAPTCHA — o limite por IP, o bloqueio de conta e o limite por e-mail bastam na escala da v1; reavaliar se houver ataque comprovado

### 6. Monitoring & Audit
- [x] Registro de eventos de segurança — Serilog `Warning` para falha, bloqueio e recusa de login, permissão negada e limite excedido; `Information` para entrada e saída (RC-16) (Task 1.1, 0.4)
- [x] Correlação — `X-Correlation-ID` em toda requisição, resposta e linha de log; igual ao `traceId` do ProblemDetails (Task 0.3)
- [x] Auditoria só de acréscimo — `IAuditLog` sem operação de editar ou apagar; registra ator, ação, alvo, valor anterior e novo (quando houver) e resultado, para usuários, categorias, telefone, publicar, rejeitar, despublicar e arquivar (RC-16) (Task 0.6, 1.3, 2.6, 2.7, 4.2, 4.3)
- [x] Cabeçalhos de segurança — middleware `SecurityHeadersMiddleware`: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy`, `Content-Security-Policy: default-src 'self'; img-src 'self' data:; script-src 'self'; style-src 'self'; frame-ancestors 'none'; form-action 'self'`, HSTS em produção (Task 0.4)
- [x] CORS — **nenhuma política registrada** (mesma origem); `AllowAnyOrigin` não existe no código (Task 0.4)
- [x] Saúde sem detalhes — `/health/live` e `/health/ready` respondem só `Healthy`/`Unhealthy` (Task 0.5)
- [N/A] Alerta automático de anomalia — sem Prometheus nem Grafana na hospedagem compartilhada (ADR-010); risco residual RR-3

