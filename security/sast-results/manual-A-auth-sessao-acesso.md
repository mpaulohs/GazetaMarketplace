# Escopo A — SAST manual: autenticação, sessão, autorização e redefinição de senha

Repositório: `/home/user/GazetaMarketplace` (HEAD `ebaec49`). Leitura somente; nenhum arquivo do repositório foi alterado, nenhum build ou teste foi rodado.
Idioma: prosa em português; código e identificadores em inglês.

## Resumo executivo

**Nenhum achado Crítico nem Alto novo.** Autenticação, autorização por papel, autoria dos anúncios, antiforgery, `ReturnUrl`, token de redefinição e troca da senha provisória estão bem fechados: não achei bypass de autenticação, IDOR, escalada Redator → Administrador, mass assignment nem open redirect explorável.

Os achados novos são **1 Médio, 4 Baixos e 3 Informativos**. O mais relevante é o bloqueio de conta (5 falhas = 15 minutos) usado como negação de serviço dirigida contra o Administrador (A-01). Os riscos mais pesados que já aparecem no BACKLOG (R-11/V-01 proxy e balde único de limite, R-38 IPv6, R-31 tempo do login, R-34 `/api` com senha provisória) continuam abertos no código e foram confirmados; abaixo acrescento o que o BACKLOG não diz.

| Id | Severidade | OWASP | Título | Confiança |
|----|-----------|-------|--------|-----------|
| A-01 | Médio | A07 / A04 | Bloqueio de conta como negação de serviço dirigida (Administrador) | Confirmado pela leitura |
| A-02 | Baixo | A09 | Senha digitada no campo e-mail pode ir para o log em texto puro | Confirmado pela leitura |
| A-03 | Baixo | A07 | Tempo do login: conta existente com senha errada faz mais acessos ao banco que e-mail inexistente (acréscimo a R-31) | A confirmar (medição) |
| A-04 | Baixo | A07 | Sessão deslizante sem teto absoluto e saída que não revoga o cookie | Confirmado pela leitura |
| A-05 | Baixo | A05 | Proteções ligadas só quando o ambiente se chama exatamente "Production" (acréscimo a R-39/R-46/V-05) | Confirmado pela leitura |
| A-06 | Baixo | A04 / A05 | Acréscimo a R-38: varredura O(n) sob trava, tabela de tentativas e PBKDF2 sem teto com endereços IPv6 trocados | Confirmado pela leitura (efeito a medir) |
| A-07 | Info | A07 | Política de senha só por composição; custo PBKDF2 padrão; sem lista de senhas comuns | Confirmado pela leitura |
| A-08 | Info | A09 | Entrada, saída e bloqueio só no log de arquivo (14 dias), sem IP na entrada bem-sucedida e fora de `AuditEntries` | Confirmado pela leitura |

Itens do BACKLOG reconfirmados no código (sem exploração nova): **R-11/V-01, R-19, R-25, R-26, R-30, R-31, R-34, R-35, R-38, R-39, R-42, R-46** e as linhas 318, 320 e 321 do BACKLOG. Detalhe na seção "Já conhecidos".

---

## Achados novos

### A-01 — Médio — A07 Identification and Authentication Failures / A04 Insecure Design
**Bloqueio de conta como negação de serviço dirigida ao Administrador (ou a qualquer conta conhecida).**

- **Onde:** `src/GazetaMarketplace.Web/Security/IdentityExtensions.cs:44-47` (`Lockout.AllowedForNewUsers = true`, `MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 15 min`); `Areas/Panel/Controllers/AccountController.cs:91` (`lockoutOnFailure: true`); `src/GazetaMarketplace.Infrastructure/Recovery/PasswordRecoveryService.cs:117-118` (a redefinição por link zera o bloqueio).
- **Descrição:** o contador de falhas do Identity é por **conta**, não por origem. Quem conhece o e-mail de um membro da equipe (o do Administrador costuma ser público ou fácil de achar) trava a conta com 5 senhas erradas, e isso vale 15 minutos para qualquer IP. O contador por IP e o limite `auth` (5 pedidos em 15 min) só limitam **quanto cada IP tenta**, não protegem a conta. Não há CAPTCHA (SECURITY_REQUIREMENTS §N/A) nem alerta ao dono da conta.
- **Exploração passo a passo:**
  1. O atacante, de um IP qualquer, envia 5 `POST /painel/entrar` com o e-mail do Administrador e senhas erradas (cada resposta é a mesma mensagem, não revela nada).
  2. O Identity grava `LockoutEnd = agora + 15 min`; a partir daí até a senha certa do Administrador devolve a mesma mensagem genérica (`AccountController.cs:109`).
  3. O atacante espera cerca de 15 minutos (ou usa um segundo IP) e repete. O Administrador só consegue entrar numa janela de segundos a cada ciclo.
  4. Pedir "Esqueci minha senha" não resolve por muito tempo: o link zera o bloqueio (`PasswordRecoveryService.cs:117-118`), mas o atacante trava de novo em 5 pedidos.
  Custo para o atacante: 5 requisições a cada 15 minutos. Efeito: o painel fica sem Administrador utilizável (a fila de revisão para; nenhum anúncio é publicado).
- **Atenuantes existentes:** a mensagem é uniforme (não vira enumeração); o bloqueio não vaza que a conta existe; contas desativadas não acumulam falhas (`PreSignInCheck` sai antes da senha).
- **Correção sugerida (qualquer combinação):**
  1. Bloquear por par **(conta + origem)** em vez de só por conta, ou manter o bloqueio por conta mas **exigir passo extra** (CAPTCHA/atraso progressivo) em vez de recusar tudo.
  2. Atraso exponencial (1 s, 2 s, 4 s…) em lugar de bloqueio total de 15 minutos.
  3. Avisar por e-mail o dono da conta quando houver bloqueio, e registrar `Warning` com a contagem de contas bloqueadas por hora (alerta operacional).
  4. Dar ao Administrador um caminho de saída que o atacante não consiga travar (por exemplo, o link de redefinição não deve ficar sujeito ao mesmo bloqueio, o que já ocorre; falta limitar o re-bloqueio logo depois de uma redefinição).
- **Confiança:** confirmado pela leitura do fluxo; a repetição do ciclo depende do desencontro de 1 a 2 segundos entre a janela do contador por IP e o fim do bloqueio, que um segundo IP elimina.

### A-02 — Baixo — A09 Security Logging and Monitoring Failures
**Senha digitada no campo e-mail pode ir para o log em texto puro.**

- **Onde:** `AccountController.cs:231-233`:
  ```csharp
  string forLog = email.Contains('@', StringComparison.Ordinal) && email.Length <= 254 ? email : "(formato inválido)";
  log.LogWarning("Falha de entrada de {Email} a partir de {Source}: {Reason}", forLog, source, reason);
  ```
  `src/GazetaMarketplace.Infrastructure/Logging/MaskingEnricher.cs` (regex `Email()`: `[…]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}`; o nome `Email` não está em `SensitiveNames`).
- **Descrição:** a heurística "tem `@` e até 254 caracteres" deixa passar qualquer senha que contenha `@`. O mascarador só troca texto que parece e-mail **com domínio e ponto** (`algo@dominio.tld`). Uma senha como `Joao@Gazeta1` ou `Senha@123` não casa com a regex, então entra no arquivo de log inteira. O formulário usa `novalidate`, então o navegador não barra um texto que não é e-mail (`Views/Account/SignIn.cshtml`, tag `<form … novalidate>`).
- **Exploração:** não é remota; é vazamento por erro de uso. Quem trocar os campos (ou o gerenciador de senhas preencher o campo errado) deixa a senha em `gazeta-AAAAMMDD.json`, retido por 14 dias (`SerilogConfiguration.RetentionDays`). Quem lê o log (operador, backup, suporte do provedor) obtém a senha com o e-mail da própria pessoa em outra linha do mesmo instante (`Entrada do usuário`/`Falha`).
- **Teste que existe e o que falta:** `tests/GazetaMarketplace.Web.Tests/Account/AccountTests.cs:98-108` prova só `Minha$enhaSecreta9` (sem `@`). Falta o caso com `@`.
- **Correção sugerida:** registrar o e-mail **somente** se passar numa validação estrita (`MailAddress`/`EmailAddressAttribute` e domínio com ponto) e, mesmo assim, mascarado; na dúvida, registrar `(formato inválido)`. Acrescentar ao teste os casos `Senha@123`, `Joao@Gazeta1`, `a@b`.
- **Confiança:** confirmado pela leitura (regex e heurística).

### A-03 — Baixo — A07 (enumeração de usuário por tempo) — acréscimo a R-31
**A conta existente com senha errada gasta mais acessos ao banco que um e-mail inexistente, além da diferença já registrada para conta bloqueada/desativada.**

- **Onde:** `AccountController.cs:82-92`; o caminho de e-mail inexistente calcula só `Hasher.VerifyHashedPassword(...)` com `DummyHash` (linha 86); o caminho de conta existente chama `access.PasswordSignInAsync(user, …, lockoutOnFailure: true)` (linha 91).
- **Descrição:** o hash falso iguala o custo do PBKDF2, mas não o resto. Para conta existente com senha errada o Identity faz, depois do hash, `AccessFailedAsync` → `UpdateUserAsync`: o `UserValidator` consulta o nome e o e-mail por unicidade (`RequireUniqueEmail = true`, 2 `SELECT`) e grava o `UPDATE` do contador (`AccessFailedCount` + `ConcurrencyStamp`). Isso são cerca de 3 comandos SQL a mais que nunca ocorrem para e-mail inexistente. Com o SQL Server do provedor em outra máquina, a diferença pode ser de alguns a dezenas de milissegundos sobre um hash de dezenas de milissegundos.
- **Exploração:** o atacante mede, para cada e-mail candidato, o tempo mediano de poucas respostas e separa "existe" de "não existe". Limitação real: o limite `auth` e o contador por IP dão ~5 amostras por IP a cada 15 minutos; por isso o ataque só rende com muitos IPs (ver R-38/A-06) ou proxy não configurado (R-11 faz o inverso: bucket compartilhado, o que atrapalha o atacante e a equipe).
- **Por que importa:** a lista de e-mails confirmados alimenta o ataque do A-01 e phishing da equipe; a SPEC (RC-13) pede resposta "igual" para existir ou não.
- **Correção sugerida:** igualar o trabalho: para e-mail inexistente, executar também a mesma sequência (por exemplo um `UPDATE` inofensivo numa linha fantasma, ou fazer o registro de falha **assíncrono e fora do caminho da resposta** para os dois casos); no mínimo, registrar a tentativa de falha depois de enviar a resposta. Combinar com a correção de R-31 (rodar o hash falso também em `IsNotAllowed` e `IsLockedOut`).
- **Confiança:** **a confirmar** — a leitura do Identity e do código mostra a diferença de trabalho; só uma medição contra o artefato (≥ 200 amostras por caso, rede real até o banco) diz se ela é separável do ruído.

### A-04 — Baixo — A07 Identification and Authentication Failures
**A sessão não tem teto absoluto e a saída não revoga o cookie.**

- **Onde:** `IdentityExtensions.cs:90-91` (`ExpireTimeSpan = SessionMinutes` com `SlidingExpiration = true`); `AccountController.cs:178-188` (`access.SignOutAsync()` só apaga o cookie do navegador; não troca o carimbo de segurança).
- **Descrição:** o cookie é autocontido. Se alguém o copiar (máquina compartilhada, extensão maliciosa, backup do navegador), ele continua válido (a) enquanto for usado a cada menos de 30 minutos, **sem limite de idade**, e (b) **depois de a vítima clicar em "Sair"**, porque nada no servidor muda. A revalidação a cada 5 minutos (`IdentityExtensions.cs:27`, `SecurityStampValidatorOptions`) só pega mudança de carimbo, desativação ou troca de papel.
- **Exploração:** `HttpOnly`, `Secure`, `SameSite=Lax` tornam o roubo remoto difícil; o risco é local ou de máquina da redação. Passos: vítima usa o painel num computador compartilhado → clica "Sair" → atacante com o valor do cookie (cópia prévia) reenvia o `Cookie: Gazeta.Team=…` e continua como a vítima até 30 minutos depois da **última** requisição dele, indefinidamente se o ator mantiver tráfego.
- **Correção sugerida:** (1) teto absoluto (por exemplo 8–12 h): gravar `IssuedUtc` do **primeiro** login numa claim e recusar em `OnValidatePrincipal`; (2) no `SignOutAsync` do site, chamar `UserManager.UpdateSecurityStampAsync(user)` (encerra também as outras sessões da pessoa em até 5 min; avaliar o efeito de "sair em todos os lugares"); (3) opcional: `__Host-` no nome do cookie.
- **Confiança:** confirmado pela leitura.

### A-05 — Baixo — A05 Security Misconfiguration — acréscimo a R-39 / R-46 / V-05
**Proteções que dependem de o ambiente se chamar exatamente "Production".**

- **Onde:** `src/GazetaMarketplace.Infrastructure/Configuration/OptionsExtensions.cs` (método `Validate(... production)`: `ValidateDataAnnotations().ValidateOnStart()` só se `production`); `src/GazetaMarketplace.Core/Configuration/SiteOptions.cs` (`BaseUrl` `[Required]` só validado em Production); `Program.cs:109,67-68` e `Middleware/SecurityHeadersMiddleware.cs:35` (HSTS só `production && IsHttps`); `AllowedHosts: "*"` em `appsettings.json`.
- **Descrição:** qualquer nome diferente (o V-05 discute criar um perfil "Staging") desliga a validação de partida (`Site:BaseUrl`, `DataProtection:KeysDirectory`, `SendGrid`, `Photo`…), o HSTS e a obrigação de `BaseUrl`. Sem `BaseUrl`, `PasswordRecoveryService.cs:79` monta o link de redefinição a partir de `Request.Scheme://Request.Host` (`AccountController.cs:127`), e com `AllowedHosts=*` o cabeçalho `Host` forjado vira o domínio do link enviado por e-mail (envenenamento do link de redefinição, exatamente o que a documentação de `SiteOptions` quer evitar).
- **Exploração (só em ambiente que não seja "Production" e exposto à internet):** atacante abre `/painel/esqueci-minha-senha` (pega o token antiforgery de uma sessão própria), envia o POST com `Host: atacante.exemplo` e o e-mail da vítima; o e-mail legítimo chega com `https://atacante.exemplo/painel/redefinir-senha?id=…&code=…`; se a vítima clicar, o atacante captura o `code` válido por 1 h. Em Production o `BaseUrl` obrigatório impede isso; a fragilidade é a condição `IsProduction()`.
- **Correção sugerida:** exigir `BaseUrl` (https), `KeysDirectory` e HSTS **sempre que o ambiente não for Development**; `AllowedHosts` com os domínios reais; recusar na partida (fail closed) se `Site:BaseUrl` estiver vazio e o ambiente não for Development/Testing.
- **Confiança:** confirmado pela leitura; hoje não explorável em Production.

### A-06 — Baixo — A04 / A05 — acréscimo a R-38
**Com endereços IPv6 trocados a cada pedido, os limites por IP deixam de existir e três componentes pagam o custo: o contador de falhas, a tabela de tentativas e o PBKDF2.**

- **Onde:** `Security/LoginFailureCounter.cs:51-57` (a cada falha, se há mais de 1024 origens, **varre todas** sob `lock`); `Security/RateLimitingExtensions.cs:139` (`ClientIp` usa o endereço completo; R-38); `Recovery/PasswordRecoveryService.cs:50-57` (cada "esqueci" grava 1 linha e roda 3 `COUNT`); `AccountController.cs:86` (cada tentativa com e-mail inexistente paga 1 PBKDF2).
- **Descrição:** além do que o R-38 descreve (escapar dos contadores), o efeito colateral é de disponibilidade: (1) a lista `_failures` cresce com cada endereço distinto e, passando de 1024 chaves, **cada nova falha custa O(n) segurando o `lock`** que todas as tentativas de login usam; (2) `PasswordRecoveryAttempts` ganha uma linha por pedido e as três contagens (`COUNT` por e-mail, por IP e por 24 h) ficam mais lentas; (3) cada login com e-mail inexistente gasta um PBKDF2 de cerca de 50-100 ms de CPU, que o contador por conta não limita (não há conta).
- **Exploração:** quem tem um bloco IPv6 /64 (comum em planos residenciais e VPS) envia login falho com e-mails aleatórios, cada um de um endereço diferente do /64: nenhum limite por IP dispara; o processo único da hospedagem compartilhada satura a CPU e a trava global, e o painel deixa de responder.
- **Correção sugerida:** a de R-38 (chave = prefixo /64 para IPv6) **e** (a) trocar a varredura completa por limpeza amortizada (por exemplo, descartar só algumas chaves por chamada, ou um `Timer`); (b) limite global de tentativas de login por minuto (todos os IPs somados) com resposta 429 barata antes do hash; (c) índice em `PasswordRecoveryAttempts (Email, RequestedAt)` e `(Ip, RequestedAt)` (conferir `Data/Configurations`).
- **Confiança:** confirmado pela leitura; o impacto real em CPU precisa de teste de carga.

### A-07 — Info — A07
**Política de senha só por composição, sem lista de senhas comuns; custo do hash é o padrão do framework.**

- **Onde:** `IdentityExtensions.cs:38-42` (8 caracteres, maiúscula, minúscula, dígito, símbolo; `RequiredUniqueChars` padrão = 1); nenhum `PasswordHasherOptions` configurado (`grep` em `src`: sem ocorrência).
- **Descrição:** `Senha123!`, `Password1!` e `Gazeta2026!` passam. O PBKDF2 do `PasswordHasher` usa o padrão do framework (a versão 3 usa HMAC-SHA512 com 100 mil iterações; a recomendação atual do OWASP para esse algoritmo é 210 mil — **a confirmar na versão do .NET em uso**). Como o limite por conta é 5 tentativas em 15 minutos (A-01), o risco prático é baixo.
- **Correção sugerida:** validador adicional que recuse as 1.000 senhas mais comuns e senhas contendo o e-mail ou o nome da pessoa; `Configure<PasswordHasherOptions>(o => o.IterationCount = 210_000)` (rehash transparente no próximo login; medir o custo de CPU junto com A-06).
- **Confiança:** confirmado pela leitura da configuração.

### A-08 — Info — A09
**Entrada, saída e bloqueio ficam só no log em arquivo (14 dias), e a entrada bem-sucedida não registra o IP.**

- **Onde:** `AccountController.cs:98` (`Entrada do usuário {UserId} ({Role})`, sem origem); `:183` (saída); `:223-246` (falhas); `src/GazetaMarketplace.Infrastructure/Data/AuditLog.cs` (nenhuma ação `auth.*`).
- **Descrição:** não há trilha de auditoria no banco para login, logout, bloqueio e desbloqueio; após 14 dias não se prova quem entrou nem de onde (o IP das falhas é registrado, o do sucesso não). Para a v1 é aceitável (RC-16 fala em log), mas dificulta resposta a incidente depois de um comprometimento.
- **Correção sugerida:** acrescentar `{Source}` à linha de sucesso e gravar `auth.login`, `auth.lockout` e `auth.logout` em `AuditEntries` (sem senha, sem e-mail completo).
- **Confiança:** confirmado pela leitura.

---

## Já conhecidos (confirmados no código; marcados como no BACKLOG)

| BACKLOG | Estado no código | Acréscimo |
|---------|------------------|-----------|
| **R-11 / V-01** | `ForwardingExtensions.cs:42` só ativa `UseForwardedHeaders` com `KnownProxies`; `RateLimitingExtensions.cs:72-73` mantém **um** balde `auth` por IP para entrar, esqueci e redefinir | Qualquer visitante, sem e-mail válido, esgota o balde com 5 POST vazios (o limitador conta todo POST, inclusive os que falham na validação do formulário), então enquanto o proxy do SmarterASP não entra em `KnownProxies` **todo o painel fica sem login por 15 min por 5 requisições** (e o `LoginFailureCounter.Reset` por uma entrada de qualquer pessoa zera o contador de todos). Já é P0 no BACKLOG. |
| **R-31** | `TeamSignInManager.cs:26-27`, `AccountController.cs:82-92` | Ver A-03 (diferença maior que a registrada). |
| **R-34** | `Controllers/Api/AdPhotosController.cs:24`, `CepController.cs:19`, `CitiesController.cs:19` usam só `[Authorize(Writer)]`; o `MustChangePasswordFilter` está só em `PanelControllerBase` (`PanelControllerBase.cs:15`) | Confirmado: sessão com senha provisória envia, remove e troca capa de foto pela API. Impacto baixo (quem tem a senha provisória já é a pessoa autorizada; a falha é não forçar a troca). |
| **R-38** | `RateLimitingExtensions.cs:139`, `AccountController.cs:64` | Ver A-06. |
| **R-19** | `UserManagement`, `CategoryManagement`, `SiteSettingsManagement`, `ReviewQueue` sem `EnsureAdministrator()`; `AdReview`/`AdTakedown` têm (`AdReview.cs:122-128`) | Reconfirmado; a única barreira é `[Authorize(Policy = Administrator)]` nos controllers e o `AccessMatrixTests`. |
| **R-25** | `BootstrapAdminInitializer.cs:113` ignora o resultado de `AddToRoleAsync` | Reconfirmado. |
| **R-26** | `PasswordRecoveryService.cs:107-126` (senha, desbloqueio e auditoria em gravações separadas) | Reconfirmado. |
| **R-30** | `PasswordRecoveryService.cs:67-71` | Acréscimo: com 2 contas conhecidas e 3 e-mails por hora cada, o atacante consome os 100 e-mails por dia do plano gratuito do SendGrid (item 18 do BACKLOG) e a recuperação legítima falha. |
| **R-35** | `CategoriesController.cs:60-63` ignora `ModelState` (`parentId=abc` vira `null`) | Reconfirmado; só Administrador. |
| **R-39** | `Configured(...)` em `RateLimitingExtensions.cs:99-102` aceita qualquer inteiro positivo de `RateLimiting:AuthPermits` | Reconfirmado: quem controla o ambiente do servidor controla o limite. |
| **R-42 / linha 318** | `Program.cs` sem `FallbackPolicy` | Hoje cada endpoint tem decisão explícita; conferido que **todos** os controllers públicos são públicos de propósito (Home, Search, Category, Ad, Favorites, Photos, Seo, PublicAds, PublicCities, VehicleCatalog) e que o `AccessMatrixTests` cobre as rotas. |
| **R-46** | `AllowedHosts: "*"` | Ver A-05. |
| **Linha 320** | 403 em vez de 404 para anúncio alheio (`AdService.GetAsync` → `ForbiddenException`) | Reconfirmado: um Redator distingue "id inexistente" (404) de "id de outro autor" (403) e enumera ids existentes. |
| **Linha 321** | `AdPhotosController.Upload` valida arquivo antes da autoria | Reconfirmado (sem efeito). |

---

## O que foi conferido e está correto (evidência)

### Autenticação e sessão
- **Cookie** `Gazeta.Team`: `HttpOnly`, `SecurePolicy = Always`, `SameSite = Lax`, sem `Domain`, expiração deslizante de `SessionMinutes` (30; `[Range(1,120)]`), `IsPersistent = false` — `IdentityExtensions.cs:85-91`, `AuthenticationOptions.cs`. Antiforgery: cookie `HttpOnly`, `Secure` sempre, `SameSite = Strict` — `Program.cs:58-64`.
- **Fixação de sessão:** não há ID de sessão no servidor; cada entrada emite cookie novo (`PasswordSignInAsync`), e a troca de senha chama `RefreshSignInAsync` (`PasswordController.cs:79`).
- **Revalidação e desativação:** `ValidationInterval = 5 min` (`IdentityExtensions.cs:27,67-71`); `TeamSignInManager.ValidateSecurityStampAsync` devolve nulo para conta inativa (`TeamSignInManager.cs:29-33`); `DeactivateAsync` troca o carimbo (`UserManagement.cs:203-205`) e `ChangeRoleAsync` também (`:172-173`). Conta desativada perde a sessão em até 5 min (NFR-08). Mudança de papel e desativação só aparecem no cookie depois da próxima revalidação, que reconstrói a identidade a partir do banco.
- **Entrada:** mensagem única para senha errada, e-mail inexistente, conta desativada e conta bloqueada (`AccountController.cs:34,109`); hash falso para e-mail inexistente (`:43-44,86`); a senha nunca volta para a tela (`:66`; campo `type=password` nunca renderiza valor).
- **Troca obrigatória da senha provisória:** claim `must_change_password` no cookie (`TeamClaimsPrincipalFactory.cs`), filtro de ação no `PanelControllerBase` (`PanelControllerBase.cs:15`) cobre Ads, AdPhotoPages, ReviewQueue, Users, Categories, Settings; `PasswordController` fica fora de propósito. `AccountController.SignIn` redireciona para `SetPassword` antes de qualquer `returnUrl` (`:101-103`). Conferido que não há rota do painel fora do filtro, exceto as de `/api` (R-34). A nova senha não pode ser igual à provisória (`PasswordController.cs:55-59`).
- **Antiforgery em toda escrita:** filtro global `AutoValidateAntiforgeryToken` + `AntiforgeryJsonFilter` (ordem `int.MinValue`) para `/api` (`Program.cs:52-57`, `Filters/AntiforgeryJsonFilter.cs`); `[IgnoreAntiforgeryToken]` só em `HomeController.Status` e `Error` (alvos de reexecução, sem efeito colateral). `POST /painel/sair` exige token (`_PanelLayout.cshtml:48-49`). Não há `MapPost`/minimal API no `Program.cs`. Nenhum GET altera estado (conferidos todos os `[HttpGet]` dos controllers do escopo).

### Redefinição de senha
- **Token** (`RecoveryTokenProvider.cs`): protegido pelo Data Protection (cifrado e autenticado), amarrado a usuário, propósito, carimbo de segurança e hora; vale 1 h (`IdentityExtensions.cs:62`) e **uso único** porque `ResetPasswordAsync` troca o carimbo; `Inspect` separa expirado, usado e inválido sem expor dado.
- **Vínculo:** `FindAsync` exige conta **ativa** e id igual ao do token (`PasswordRecoveryService.cs:138-143`, `RecoveryTokenProvider.cs:77`); conta desativada recebe "inválido".
- **Resposta neutra e sem diferença de caminho** em "esqueci": grava a tentativa, conta e enfileira igual para e-mail existente ou não (`PasswordRecoveryService.cs:44-84`); quem procura a conta e envia é o worker, fora da resposta (`PasswordRecoveryWorker.cs`); só conta ativa recebe e-mail e o destinatário vem do banco, não do formulário (`PasswordRecoveryMailer.cs:27-32,68`). `ForgotPasswordViewModel` tem `[EmailAddress]` e `[StringLength(254)]`.
- **Link:** a base vem de `Site:BaseUrl` (obrigatória em Production) e não do cabeçalho `Host` (`PasswordRecoveryService.cs:79`; ver A-05). A página do link não carrega recurso de terceiros; `Referrer-Policy: strict-origin-when-cross-origin` e CSP `default-src 'self'` (`SecurityHeadersMiddleware.cs:12-14,29`). O GET só confere (o prefetch de antivírus não consome o link); o POST consome. O token não vai para o log (`MaskingEnricher` mascara `token=`/`code=`; mailer e worker não o registram).
- **Rate limit:** `[EnableRateLimiting("auth")]` em entrar, esqueci e redefinir (`AccountController.cs:61,118,141`); limites por e-mail (3/h) e por IP (10/h) no serviço.
- A redefinição zera contagem de falhas e bloqueio (`:117-118`), desliga `MustChangePassword` (`:119-123`) e muda o carimbo (encerra as demais sessões).

### Autorização e autoria
- **Papéis:** `AccessPolicies.Administrator` e `Writer` (`IdentityExtensions.cs:76-78`); `UsersController`, `CategoriesController`, `SettingsController`, `ReviewQueueController`, `ComponentsController` têm `[Authorize(Policy = Administrator)]` no controller; `Despublicar`/`Arquivar` têm o atributo por ação (`AdsController.cs:212-228`). `DevelopmentOnly` protege `/painel/componentes`.
- **Autoria no servidor:** `AdAccess.CanView/CanEdit/CanTransition` (`Core/Ads/AdAccess.cs:17-33`) aplicadas por `AdService.GetAsync/GetForEditAsync/TransitionAsync` (`AdService.cs:25-77`); **toda** operação por id passa por elas: editar (`AdDraftService.cs:88`), enviar (`AdSubmission.cs:25,32,68`), fotos (`AdPhotoService.cs:60,75,89` via `GetForEditAsync`), publicar/rejeitar/despublicar/arquivar (`AdReview`, `AdTakedown` com `EnsureAdministrator` + `GetAsync`), tela `Refresh` e `Edit` (`AdsController.cs:109,319`). `AdPhotoService.ListAsync` não confere acesso, mas só é chamado depois de `GetAsync`/`CanEdit` (`AdPhotoPagesController.cs:60-66`, `AdFormFactory.cs:106`, `AdDetailFactory.cs:47` para o anúncio já carregado).
- **Listagem do Redator:** o `authorId` vem da sessão, nunca do pedido (`PanelAdListService.cs:23`); consulta Dapper totalmente parametrizada, `CHARINDEX` sem curinga (`PanelAdListReadRepository.cs`).
- **Fotos:** `PhotoDelivery.cs:22-38` exige que a foto pertença ao anúncio da rota; anúncio não publicado só vai ao autor ou Administrador; "não existe" e "não pode ver" são o mesmo 404; resposta privada `private, no-store`.
- **Escalada de papel:** só `UsersController.Edit` → `ChangeRoleAsync` muda papel, protegido por política de Administrador, papel validado contra a lista fechada (`UserManagement.cs:298`), "último Administrador ativo" protegido dentro de transação `Serializable` (`:160,198,345`); desativar a si mesmo e redefinir a própria senha por esta via são recusados (`:188,254`). Não existe rota de auto-cadastro.
- **Mass assignment:** `NewUserViewModel`, `EditUserViewModel` (só `Role` é usado), `ResetPasswordViewModel`, `CategoryFormViewModel`, `SettingsViewModel`, `AdFormSubmission` não têm campo de situação, autor, papel, `IsActive` nem `MustChangePassword`; `AdDraftService` nunca lê esses dados do formulário (comentário RC-14 e `ToInput`, `AdsController.cs:346`). `RowVersion` só serve para concorrência.
- **IDOR por id de usuário/categoria/configuração:** todas as rotas são de Administrador; ids não numéricos são recusados pela restrição `:int`.

### Open redirect (RC-18)
- Único uso de destino vindo do usuário: `AccountController.LocalReturnUrlOr` com `Url.IsLocalUrl` e recusa de `/painel/entrar…` (`:218-221`), seguido de `LocalRedirect`; os demais `Redirect(...)` usam constantes (`PanelRoutes`) ou `Url.Action`. Testes `ReturnUrlExterno_E_Ignorado` cobrem `https://…`, `//…`, `/\…`, `javascript:` (`AccountTests.cs:111-125`). O campo oculto `ReturnUrl` é codificado pelo Razor.

### Logs com dado sensível
- Senha nunca é registrada (`AccountController` não passa `submittedPassword` a nenhum log; `PasswordController.cs:71` registra só códigos de erro). E-mail aparece mascarado (`MaskingEnricher`); token/código mascarados. Ver a exceção em A-02.
- `AccessDeniedLog` e `AccessDeniedLoggingMiddleware` (`Security/AccessDeniedLog.cs`): registram método, caminho, id e papel, sem e-mail nem conteúdo; o middleware roda depois da autenticação e antes da autorização (`Program.cs:136-141`) e ignora `/painel/acesso-negado` para não duplicar; `OnRedirectToAccessDenied` registra antes de redirecionar (`IdentityExtensions.cs:112-123`) e devolve 403 JSON em `/api`.

### Pipeline (`Program.cs`)
- Ordem: forwarded headers (condicional) → correlação → cabeçalhos de segurança → exceções → status → HTTPS → compressão → cultura → roteamento → limite de corpo → autenticação → registro de acesso negado → limitador → autorização (`:107-141`). Sem CORS (mesma origem), sem página de exceção de desenvolvimento.
- `ForwardingExtensions`: falha fechada; `ForwardLimit = 1`, `KnownProxies`/`KnownIPNetworks` limpos antes de preencher com a lista configurada (`:25-35`), então nenhum remetente fora da lista é confiável.
- O `X-Correlation-ID` do cliente só é aceito em formato seguro (`CorrelationIdMiddleware.cs:41`).

## Observações sem severidade

- `TempData` usa o provedor de cookie padrão (cifrado pelo Data Protection); com `KnownProxies` vazio atrás de um proxy que termine TLS, `Request.IsHttps` seria falso e o cookie do TempData sairia sem `Secure`. É mais um motivo para fechar R-11 antes do lançamento.
- Chaves do Data Protection sem criptografia em repouso (já no BACKLOG, dono `/infra`): quem lê a pasta de chaves forja cookies e links de redefinição. Confirmar a permissão da pasta no `/deploy`.
- Um Administrador pode redefinir a senha de outro Administrador sem aviso por e-mail ao dono (há auditoria `user.reset_password`). Decisão SEC-02 do Product Owner (sem reautenticação) já registrada em RR-1.
- Os parâmetros `?expirada=1` e `?alterada=1` do endereço de entrada podem ser forjados para mostrar os avisos "Sua sessão expirou" e "Sua senha foi alterada" (sem efeito além de texto).

## Prioridade sugerida

1. **A-01** (Médio): decidir a política de bloqueio antes do lançamento; é o único achado que derruba o acesso do Administrador com custo mínimo para o atacante.
2. Fechar **R-11/V-01** (proxy e balde separado) antes do `/deploy`; sem isso A-01 e a negação de serviço do painel valem para qualquer visitante.
3. **A-02** e **A-04**: correções pequenas (validação estrita do e-mail no log; teto absoluto e carimbo na saída).
4. **A-05**: tornar `BaseUrl`/HSTS/validação independentes do nome "Production" ao criar o perfil Staging (V-05).
5. **A-03/A-06/A-07/A-08**: no `/simplify` ou numa passada de endurecimento, com medição.
