# Security Scan Report — GazetaMarketplace

> **Em resumo:** a varredura de segurança antes da publicação não achou nenhuma falha **Crítica** nem **explorável de imediato**. Rodamos análise de código (automática e manual por três auditores), busca de segredos no código e no histórico do git, verificação de dependências e ataques reais contra o site publicado (cabeçalhos, acesso sem login, acesso a anúncio de outra pessoa, envio de arquivos hostis). O que precisa de ação antes de publicar são **dois itens de configuração e de limite de tentativas** (SC-01 e SC-02, já previstos para o `/infra`) e **uma decisão sua** sobre o bloqueio de conta (SC-03). Há também **uma exceção a assinar** (SC-28: uma chave de API já revogada que ficou no histórico do git). Este relatório **não está aprovado**: a tabela de aprovação no fim é sua.

## Summary

- **Date**: 2026-10-07 (scan automático às 01:48; DAST e análise manual no mesmo dia)
- **Commit**: `ebaec49` (código de `src/` idêntico ao do artefato do `/verify`, trava `6953fa3 6bb592c8…83ec`; nenhum arquivo de `src/` mudou desde então)
- **Tools run**: dotnet (`list package --vulnerable --include-transitive` + Roslyn `RunAnalyzers`), npm (só no ferramental `.claude*/hooks`), jq, python3, docker; **gitleaks** (instalado à mão a partir do release do GitHub: histórico completo de 216 commits + working tree); **semgrep** (instalado por `pip`, 23 regras locais offline sobre `src/` e `tools/`); `curl` para o DAST; três auditores de segurança em leitura de código.
- **Tools missing + compensating controls** (de `SCAN_SUMMARY.json §compensating_controls`):

  | Ferramenta ausente | Controle compensatório usado | Precisa entrar no pipeline? |
  |---|---|---|
  | gitleaks (no script) | instalado manualmente nesta sessão; histórico + working tree rodados; regex em `secrets-grep.txt` | **sim** (SC-28) |
  | semgrep (regras do registro, bloqueado com 403) | 23 regras locais offline (`security/sast-results/semgrep-local-rules.yml`) + revisão manual completa por 3 auditores | **sim** |
  | trivy (imagem/fs) | adiado para o `/infra` (ainda não há imagem; o destino é IIS no SmarterASP, sem contêiner) | **sim**, se o `/infra` produzir imagem |
  | hadolint | não se aplica (sem Dockerfile no destino IIS) | não |
  | trufflehog, snyk, pip-audit, safety, bandit, govulncheck, staticcheck, gosec, bundler-audit, brakeman | não se aplicam (stacks Python/Go/Ruby inexistentes) ou cobertos pela pilha nativa | não |

- **Stacks escaneadas**: `dotnet`, `nodejs`. **Stacks não escaneadas** (`stacks_unscanned`): nenhuma. Nota: o `nodejs` detectado é só o ferramental do kit (`.claude*/hooks`, sem lockfile: `npm audit` falhou com `ENOLOCK`); o produto não usa npm (JavaScript é puro, em módulos ES).
- **SAST scope**: `whole-repo` (igual ao `SCAN_SUMMARY.json`, `sast_scope`).
- **Números do scanner (brutos) e triagem:** `SCAN_SUMMARY.json` traz `critical 0 / high 30 / medium 241 / low 0`. Depois da triagem manual: os 30 *high* são **um único segredo** (chave de API do Google Maps, **já revogada**) repetido em 2 commits antigos de templates (SC-28; 0 no código atual); os 241 *medium* são **falsos positivos** (240 = `axe-core` minificado dentro do `bin` dos testes, que não é enviado; 1 = texto de teste em `PhotosEndpointsTests.cs:354`). A triagem está anotada no próprio `SCAN_SUMMARY.json` (campo `triage`).
- **Resultado após a triagem:** Crítico **0** · Alto **1** (condicional, SC-01) · Médio **4** · Baixo **10** · Info **12** · mais o item de histórico SC-28.
- **Overall Status**: **PASS WITH CONDITIONS — aguardando a sua decisão** (ver §Recommendations). Nada aqui foi corrigido: a regra desta rodada era só registrar.

## Pre-Dev Security Review

| Item | Status |
|------|--------|
| `THREAT_MODEL.md` | Encontrado (29 ameaças STRIDE) |
| `PRE_DEV_REVIEW.md` | Encontrado (21 controles `RC-1` a `RC-21`) |
| Mitigações em aberto | **Ameaças: 4 Partial** (S1, I2, D3, D5), **0 Not implemented**, **1 Deferred** (I8). **Controles: 1 Partial** (RC-10), **0 Not implemented** |
| Verification | PASS WITH CONDITIONS (a lacuna de cada *Partial* virou um achado SC-nn abaixo) |

## STRIDE Re-evaluation — Threat-Model Verification

> Conferido contra o código atual e os testes por um auditor (somente leitura). **Detalhe completo, com `arquivo:linha` de código e de teste para cada linha, em [`sast-results/manual-C-stride-rc.md`](sast-results/manual-C-stride-rc.md).** Abreviações de teste: `WT/` = `tests/GazetaMarketplace.Web.Tests/`, `IT/` = `tests/GazetaMarketplace.IntegrationTests/`, `E2E/` = `tests/GazetaMarketplace.Web.Tests.Playwright/`. Os nomes de teste do `THREAT_MODEL.md` mudaram na construção; o detalhe traz o nome real.

### Spoofing
| ID | Threat | Status | Evidence (resumo) |
|---|---|---|---|
| S1 | Força bruta / preenchimento de credenciais no login | **Partial → SC-01, SC-02** | Limite `auth`, bloqueio de conta, mensagem única: `RateLimitingExtensions.cs:72-73`, `AccountController.cs:61,91,109`, `LoginFailureCounter.cs`; testes `WT/Security/AuthRateLimitTests.cs:38`, `WT/Team/AccountTests.cs:129`. Lacunas: IP do proxy sem valor (RC-10) e o limite conta também entradas com sucesso |
| S2 | Roubo e reuso do cookie de sessão | Verified | `IdentityExtensions.cs:27,67-71,86-93`, `SecurityHeadersMiddleware.cs`; `WT/Account/SessionTests.cs:34,82,99`. Observação: Sair não revoga o cookie no servidor (SC-15) |
| S3 | Forja de cookie por vazamento das chaves do Data Protection | Verified | `Program.cs:85-93`, `OptionsExtensions.cs`; `WT/Configuration/DataProtectionKeysTests.cs:40` (dois hosts). Risco residual RR-2 aceito (chave sem criptografia em repouso → `/infra`) |
| S4 | Sequestro de conta pela redefinição de senha | Verified | `RecoveryTokenProvider.cs:52-91` (1 h, uso único, ligado ao carimbo), `PasswordRecoveryService.cs`; `WT/Account/RecoveryTokenTests.cs`, `PasswordRecoveryTests.cs` |
| S5 | Redirecionamento aberto depois do login (RC-18) | Verified | `AccountController.cs:103,218-221`; `WT/Account/AccountTests.cs:117,129`; **DAST A2: 3/3 `returnUrl` externos caem em `/painel/anuncios`** |

### Tampering
| ID | Threat | Status | Evidence (resumo) |
|---|---|---|---|
| T1 | Troca indevida do telefone do site | Verified | `SettingsController.cs:15,25`, `SiteSettingsManagement.cs:19-46`; `WT/Settings/SettingsTests.cs:62,217` |
| T2 | Injeção de SQL (busca, filtros, ordenação) | Verified | `SqlBuilder.cs`, `SearchReadRepository.cs:49-99` (`CHARINDEX` com parâmetro), nenhum `FromSqlRaw`; `IT/SearchQueryTests.cs:404`; **DAST D8: 12 sondas hostis sem 5xx** |
| T3 | XSS armazenado | Verified | Nenhum `Html.Raw`/`innerHTML`/`eval` em `src/Web` e `wwwroot/js`; CSP sem inline; `WT/Layout/XssTests.cs`, `WT/Security/XssInAllScreensTests.cs`; **DAST D9: 0 reflexões** |
| T4 | Arquivo forjado aceito como foto | Verified | `PhotoSignature.cs:19-42`, `MagickImageProcessor.cs`; `WT/Photos/FormatoTests.cs`; **DAST upload (c),(f),(g)** |
| T5 | CSRF nas ações e nos endpoints JSON | Verified | `Program.cs:52-64` (antiforgery global), `AntiforgeryJsonFilter.cs`; `WT/Security/AntiforgeryTests.cs`; **DAST D3: POST sem token → 400** |
| T6 | Edição ou decisão simultânea | Verified | `AppDbContext.cs:87-113`, `AdReview.cs`; `IT/AdServiceConcurrencyTests.cs`, `IT/ReviewDecisionConcurrencyTests.cs` |
| T7 | Sobrepostagem (mass assignment) | Verified | `AdViewModels.cs:13-36`, `IAdDraftService.cs:22`; `WT/Architecture/EditViewModelsTests.cs` |

### Repudiation
| ID | Threat | Status | Evidence (resumo) |
|---|---|---|---|
| R1 | Ação sensível sem registro ou com registro alterável | Verified (ressalva aceita RR-6) | `AuditLog.cs:12-33`, `AppDbContext.cs:172-176`; `WT/Persistence/AuditLogTests.cs`. A imutabilidade vale só na aplicação (conta única do banco, aceito pelo PO) |

### Information disclosure
| ID | Threat | Status | Evidence (resumo) |
|---|---|---|---|
| I1 | Vazamento de GPS pelos originais | Verified | versões servidas sem metadados (`Strip`, `MagickImageProcessor.cs:49`); `WT/Photos/MetadadosTests.cs`. O **original** guarda o GPS por 30 dias (SC-07) |
| I2 | Enumeração de contas pelo login e pela recuperação | **Partial → SC-14** | mensagem única e resposta igual (`AccountController.cs:34,109,117-129`); lacuna de tempo (conta desativada/bloqueada sem hash; ~3 comandos SQL a mais para conta existente) |
| I3 | Vazamento de anúncio não publicado | Verified | `SqlFragments.cs:16`, `PhotoDelivery.cs:31-35`; **DAST: foto de rascunho → 404 igual ao "não existe"** |
| I4 | Vazamento de segredos (repositório, logs, pacote) | Verified | `OptionsExtensions.cs`, `web.Production.config.example`; busca de segredos limpa no código atual (histórico: SC-28) |
| I5 | Erro com detalhe técnico no log ou na tela | Verified | `ExceptionHandlingMiddleware.cs`, `MaskingEnricher.cs`; **DAST D11: 0 pilhas na resposta**. Ver SC-06 (senha com `@` no log) e SC-12 (CEP no log do HttpClient) |
| I6 | Cabeçalhos ausentes e CORS aberto | Verified | `SecurityHeadersMiddleware.cs`; `WT/Security/HeadersTests.cs`, `CorsTests.cs`; **DAST D1/D7: CSP, HSTS, nosniff, XFO, sem CORS**. R-36 e R-37 seguem abertos (SC-21) |
| I7 | Travessia de caminho na entrega/gravação de fotos | Verified | `FileSystemPhotoStorage.cs:25-45,324-361`; `WT/Photos/PhotosSecurityTests.cs`; **DAST: 11 sondas de traversal/original → 404** |
| I8 | Fotos com placas e rostos de terceiros | **Deferred** | RR-5 (decisão do PO sobre a S18; gatilho: decisão do PO ou primeira reclamação). Nenhum código cobre, como previsto |

### Denial of service
| ID | Threat | Status | Evidence (resumo) |
|---|---|---|---|
| D1 | Bloqueio proposital da conta do Administrador | Verified (mas ver SC-03) | `IdentityExtensions.cs:45-47`, `PasswordRecoveryService.cs:116-118`. O auditor A mostrou que o bloqueio pode ser repetido a cada 15 min (SC-03) |
| D2 | Busca pesada no site público | Verified | limite de termo e 10 s (`SearchService.cs`, `AdCardSql.cs`); `IT/SearchQueryTests.cs:512`; ver SC-11 (brecha de prefixo) e SC-25 (sitemap sem cache) |
| D3 | Inundação distribuída | **Partial → SC-01, SC-11** | limite por IP existe, mas só vale com o IP certo (proxy) e o limite global é dispensado por prefixo de caminho |
| D4 | Esgotamento da cota de e-mail (RC-11) | Verified | `PasswordRecoveryService.cs:31-77`; `WT/Account/PasswordRecoveryLimitsTests.cs` |
| D5 | Envio em massa de fotos | **Partial → SC-05** | limites de 10 MB, 20/6/0 fotos, 2 conversões, 30/min, 50 MP; **DAST: 400 MP recusado em 26 ms**. Lacuna: sem cota de disco nem alerta |

### Elevation of privilege
| ID | Threat | Status | Evidence (resumo) |
|---|---|---|---|
| E1 | Redator acessa ou altera o que é de outro | Verified | `IdentityExtensions.cs:76-78`, `AdAccess.cs`, `AdService.cs`; `WT/Security/AccessMatrixTests.cs`, `OwnershipMatrixTests.cs`; **DAST A4/A5/A6: 18 de 18 acessos indevidos negados** |
| E2 | Execução de código pela biblioteca de imagens | Verified | `policy.xml`, `MagickRuntime.cs`; `WT/Photos/PhotosSecurityTests.cs`. Falta provar o ImageMagick/HEIC no Windows do provedor (AR-05, `/verify` pós-deploy) |
| E3 | Administrador comprometido ou desonesto | Verified (residual aceito RR-1/RR-6) | auditoria e papéis; sem 2FA, aceito pelo PO |

**Totals**: 24 Verified, 4 Partial, 0 Not implemented, 1 Deferred — de 29 ameaças. (`R1` e `E3` contam como Verified porque o próprio Product Owner aceitou o risco residual.)

### Controles `RC-1` a `RC-21`
`RC-1` a `RC-9` e `RC-11` a `RC-21`: **Verified** (20 de 21), cada um com implementação e teste citados em [`manual-C-stride-rc.md`](sast-results/manual-C-stride-rc.md) §2. **`RC-10` (IP do cliente atrás do proxy): Partial** → SC-01. **Totals RC**: 20 Verified, 1 Partial, 0 Not implemented, 0 Deferred — de 21.

## Findings Summary (auto-aggregated + triagem)

| Severidade | Bruto (`SCAN_SUMMARY.json`) | Depois da triagem |
|----------|-------|-------|
| Critical | 0 | **0** |
| High     | 30 | **1** (SC-01, condicional ao proxy) + SC-28 (exceção) |
| Medium   | 241 | **4** (SC-02, SC-03, SC-04, SC-05) |
| Low      | 0 | **10** |
| Info     | — | **12** |

> Escala de severidade do kit (`/scan` §Severity): Crítico = explorável, impacto alto, público; Alto = explorável com esforço, impacto relevante; Médio = alcance ou impacto limitado; Baixo = risco mínimo.

### Todos os achados consolidados (28)

> Origem: A = auditor de autenticação/sessão/acesso · B = auditor de entradas/fotos/SQL/XSS/JS · C = auditor STRIDE/RC · DAST = ataque ao artefato · "R-nn"/"V-nn" = linha já existente no BACKLOG. Detalhe de cada um nos três relatórios em `security/sast-results/manual-*.md`.

| ID | Sev. | Prio | Achado | Origem / BACKLOG | Dono |
|----|------|------|--------|------------------|------|
| SC-01 | Alto (cond.) | P0 | `KnownProxies` vazio: limites por IP valem para o site/equipe inteiros; sem aviso na partida; IPv6 | C F-01, RC-10 · R-11/SEC-01, R-38, A-06 | /infra |
| SC-02 | Médio | P0 | Entrar, esqueci e redefinir dividem o balde `auth` **e** o limite conta entradas com sucesso: a 6ª pessoa da redação recebe 429 com a senha certa | C F-02 · V-01 | /infra |
| SC-03 | Médio | P1 | Bloqueio de conta (5 falhas = 15 min por conta) permite negar acesso ao Administrador de qualquer IP, de novo a cada 15 min | A A-01 · D1 | decisão do PO, depois /fix-issue |
| SC-04 | Médio | P1 | Log saturável por tráfego anônimo (429 grava o caminho inteiro, arquivo sem teto; regex de máscara quadrática) | B B-01 | /fix-issue |
| SC-05 | Médio | P1 | Sem cota de disco nem alerta de espaço para fotos; upload lido para a memória antes de esperar a vaga de conversão | C F-05, B B-03 · D5 | /infra (alerta) + /fix-issue (cota) |
| SC-06 | Baixo | P1 | Senha com `@` digitada no campo de e-mail entra no log em texto puro | A A-02 | /fix-issue |
| SC-07 | Baixo | P1 | Original com EXIF/GPS retido 30 dias, sobrevive à remoção da foto e `IPhotoReprocessing` não tem chamador | B B-02 (LGPD) | decisão do PO |
| SC-08 | Baixo | P1 | Sessão com senha provisória ainda usa a `/api` (fotos, CEP, cidades) | C F-06 · R-34 | /fix-issue |
| SC-09 | Baixo | P1 | `Site:BaseUrl` aceita `http://`; validações, HSTS e `BaseUrl` só ligam se o ambiente se chama exatamente "Production" (link de redefinição pelo `Host` forjado em outro nome de ambiente) | C F-10, A A-05 · R-39, R-46, V-05 | /infra |
| SC-10 | Baixo | P1 | Pastas de fotos, chaves e logs só com `[Required]`: caminho relativo poria `_originals` dentro da pasta publicada | B B-05 | /infra |
| SC-11 | Baixo | P1 | Limite global dispensado por prefixo de caminho (`/css/x`, `/fotos/lixo` sem limite e renderizam a página 404) | C F-04 · D2, D3 | /fix-issue (junto de SC-02) |
| SC-12 | Baixo | P1 | Log padrão do `HttpClient` grava a URL do ViaCEP com o CEP do vendedor (arquivos de 14 dias) | C F-07 | /fix-issue (1 linha) |
| SC-13 | Baixo | P1 | Duas conversões de 50 MP podem estourar a memória do pool compartilhado; `Disk`/`Area` do ImageMagick sem limite (a confirmar) | B B-04 · V-08 | /verify pós-deploy |
| SC-14 | Baixo | P2 | Tempo do login distingue conta desativada/bloqueada e conta existente com senha errada (~3 comandos SQL a mais) | C F-03, A A-03 · R-31, I2 | /fix-issue |
| SC-15 | Baixo | P2 | Sessão deslizante sem teto absoluto; "Sair" não revoga o cookie no servidor | A A-04, C F-08 · S2 | /fix-issue |
| SC-16 | Info | P2 | Política de senha só por composição; custo do PBKDF2 no padrão | A A-07 | /fix-issue |
| SC-17 | Info | P2 | Entrada, saída e bloqueio só no log de arquivo (14 dias), sem IP na entrada com sucesso, fora de `AuditEntries` | A A-08 | /fix-issue |
| SC-18 | Info | P2 | `X-Correlation-ID` do cliente vira id de log e "código de referência" | B B-06 | /simplify |
| SC-19 | Info | P2 | `Normalize(FormD)` pode expandir o título além de `TitleSearch` (200); corte em 100 caracteres pode partir par substituto | B B-07 | /fix-issue |
| SC-20 | Info | P2 | Regex de chave de foto com `$` aceita `\n` final (usar `\z`) | B B-08 | /simplify |
| SC-21 | Info | P2 | Faltam CORP/COOP, `Referrer-Policy: no-referrer` na página de redefinição, `base-uri`/`object-src` na CSP, remoção de `Server`/`X-Powered-By` | B B-09, C F-11 · R-36, R-37 | /infra |
| SC-22 | Info | P2 | `HttpClient` do ViaCEP/SendGrid segue redirecionamento e lê resposta sem teto | B B-10 · R-27 | /fix-issue |
| SC-23 | Info | P2 | `wwwroot` publica jQuery/validation sem uso, `.map` e `LEIAME.md` | B B-11 · V-06 | /infra |
| SC-24 | Info | P2 | Sem teste de que nenhum `GET` do painel altera dados | C F-09 | /test |
| SC-25 | Info | P2 | `sitemap.xml` sem cache (até 50.000 linhas por pedido anônimo) | C F-12 | /simplify |
| SC-26 | Info | P2 | A rota convencional `{controller=Home}/{action=Index}` responde `TRACE` e `OPTIONS` na raiz com 200 e `/Home/Error` abre direto | DAST D5/D7 · V-03 | /fix-issue |
| SC-27 | Info | P2 | Anúncio de outro autor responde 403 (e 404 se não existe): revela que o id existe, só a quem tem login | DAST A5 · BACKLOG L320 | decisão do PO |
| SC-28 | Info (Alto bruto) | exceção | Chave de API do Google Maps **já revogada** no histórico (30 hits gitleaks, 2 commits de templates) | gitleaks | Security Lead |


## Critical/High Findings

**Nenhum Crítico. Nenhum 🔴 explorável.** Itens de severidade Alta:

### [SC-01] IP do cliente atrás do proxy sem valor configurado (`KnownProxies` vazio)
- **Severity**: Alto — **condicional**: só vale se o SmarterASP entrega o tráfego por um proxy; se o IIS recebe a conexão direta, cai para Baixo
- **Source**: auditor C (S1, D3, RC-10); já no BACKLOG como R-11/SEC-01, com os acréscimos R-38 e A-06
- **Location**: `src/GazetaMarketplace.Web/Security/ForwardingExtensions.cs:40-43`
- **Details**: o código é *fail-closed*: sem proxies conhecidos o middleware de cabeçalhos encaminhados nem entra. Atrás de um proxy, o endereço visto é o do proxy, então o limite global de 100 requisições/min vale para **o site inteiro** e o limite de entrada (5 por 15 min), o contador de falhas e os contadores de recuperação valem para **a redação inteira junta**: quem manda 5 POST ruins trava o login de todos por 15 min. Não há aviso na partida quando a lista está vazia.
- **Remediation**: `/infra`: obter o IP/faixa do proxy do SmarterASP e gravar `ForwardedHeaders__KnownProxies__0`; no código: `Warning` (ou falha na partida) em Production com a lista vazia; tratar IPv6 e faixas (R-38); teste de aceite no artefato publicado com o cabeçalho real.
- **Status**: **OPEN — P0, bloqueia o lançamento** (dono `/infra`)

### [SC-28] Chave de API do Google Maps no histórico do git (30 ocorrências do gitleaks)
- **Severity**: Alto no scanner (30 × `gcp-api-key`); **Info depois da triagem** — é um único segredo, **já revogado**, em 2 commits antigos (`a3521ae0`, `2ee43b89`) de HTML de templates; 0 no working tree; o `gitleaks.json` do repositório está com o campo `Secret` redigido
- **Source**: gitleaks (histórico completo, 216 commits)
- **Location**: `docs/templates/…` (autolist) nesses dois commits
- **Details**: como o `Gate 8` bloqueia qualquer *High* sem exceção aprovada, **peço que o Security Lead assine a exceção** (ver §Approval). Reescrever o histórico não é necessário porque a chave não vale mais.
- **Remediation**: confirmar no console do Google que a chave está revogada (você me disse que está); colocar o gitleaks no pipeline com `.gitleaksignore` para esses 2 commits, para o scan futuro não repetir o alerta.
- **Status**: **OPEN — exceção a aprovar** (nenhuma ação de código)

## Dependency Vulnerabilities

| Verificação | Resultado |
|---|---|
| `dotnet list package --vulnerable --include-transitive` | **0 vulneráveis** nos 8 projetos (Core, Infrastructure, Web, 5 de teste) — `security/dependency-audit/nuget-vulnerable.json` e saída repetida na sessão |
| Pacotes desatualizados (informativo) | só `Microsoft.Testing.Extensions.CodeCoverage` 18.11.2 → 18.12.0 (ferramenta de teste) — `nuget-outdated.txt` |
| Bibliotecas de front vendorizadas | Bootstrap 5.3.8, jQuery 3.7.1, jquery-validation 1.21.0: sem CVE conhecido nas versões; jQuery/validation **não são carregados** (SC-23) |
| Bibliotecas nativas | `Magick.NET-Q8-x64` 14.17.2 (decodificadores nativos): manter em dia; revisar a cada versão |
| `npm audit` | não aplicável ao produto (sem `package.json`); no ferramental do kit falhou por falta de lockfile |

## OWASP Top 10 Compliance

| Category | Status | Notes |
|----------|--------|-------|
| A01: Broken Access Control | **PASS** | E1 Verified; DAST A4/A5/A6 (18/18 negados), D2 (rotas do painel sem login → 302); IDOR de anúncio alheio responde 403 em vez de 404 (SC-27, Info) |
| A02: Cryptographic Failures | **PASS** (ressalvas) | HTTPS + HSTS (`SecurityHeadersMiddleware.cs`), senhas com PBKDF2 do Identity, token de redefinição cifrado. Chaves do Data Protection sem criptografia em repouso → DPAPI no `/infra`; GPS em originais (SC-07) |
| A03: Injection | **PASS** | SQL só com parâmetros nomeados e listas permitidas (T2); saída codificada, sem `Html.Raw`/`innerHTML` (T3); DAST D8/D9 |
| A04: Insecure Design | **WARNING** | SC-02 (limite de entrada), SC-03 (bloqueio como negação de serviço), SC-05 (sem cota de disco) |
| A05: Security Misconfiguration | **WARNING** | SC-01 (proxy), SC-09 (https e nome do ambiente), SC-10 (pastas relativas), R-36/R-37 (SC-21); cabeçalhos, CORS e páginas de erro conferidos no DAST |
| A06: Vulnerable & Outdated Components | **PASS** | 0 pacotes vulneráveis; Magick.NET a vigiar |
| A07: Identification & Authentication Failures | **WARNING** | SC-02, SC-03, SC-08 (`/api` com senha provisória), SC-14, SC-15. Sem bypass encontrado |
| A08: Software & Data Integrity Failures | **PASS** | sem desserialização insegura; antiforgery em toda escrita; migração por script idempotente (sem `Migrate()` na partida) |
| A09: Security Logging & Monitoring Failures | **WARNING** | SC-04 (log saturável), SC-06 (senha com `@` no log), SC-17 (entrada só no arquivo) |
| A10: SSRF | **PASS** | só o ViaCEP, em host fixo, CEP validado como 8 dígitos (endurecimento: SC-22) |

## Live Verification

> Pelo `/scan`, a prova ao vivo é obrigatória para as superfícies de alto risco. Alvo: o **artefato publicado do `/verify`**, em Production (`https://localhost:5443`, SQL Server real no contêiner). Scripts e saídas em [`security/dast-results/`](dast-results/).

| Surface | Test file | Pass count | Status |
|---------|-----------|------------|--------|
| SSRF | — | N/A: o servidor só chama o ViaCEP em host fixo por configuração; o CEP é validado (8 dígitos ASCII, `CepRules.cs:15-28`); não há URL fornecida pelo usuário. Teste em processo: `WT/Cep/*` | N/A |
| Upload de arquivo | `dast-results/dast-upload.sh` → `dast-upload.txt` | **10/10** como esperado: vazio, 10,6 MB, `MZ` com nome `.jpg`, SVG com script e PNG de 400 MP **recusados (400)**; nome `../../etc/passwd.png`, `evil.jpg.exe`, PNG com PHP/JS anexado e PNG de 36 MP **aceitos mas reprocessados para WebP** (nome descartado, anexo perdido); PNG válido 201 | **PASS** |
| Servir foto / traversal | `dast-results/dast-photos-serve.txt` | **11 sondas, todas como esperado**: versões 480/1600 de anúncio publicado 200 com `image/webp` e `nosniff`; original, `..%2f`, `/../appsettings.json`, tamanho inexistente, extensão trocada, foto de rascunho: 404; `%00` nem sai do cliente (`curl` recusa) | **PASS** |
| Auth bypass / IDOR | `dast-results/dast-auth.sh` → `dast-auth.txt`; `dast-probes.sh` → `dast-unauth.txt` | **sem sessão**: 9 de 9 rotas existentes do painel → 302 ao login, POST sem token → 400; **Redator** (usuário 3) contra rotas de Administrador 6/6 → acesso negado, contra anúncio do Administrador 7/7 negados, contra a API de fotos 5/5 negados (401/403/404) | **PASS** (403 em vez de 404: SC-27) |
| Redirecionamento aberto | `dast-auth.sh` (A2) | 3/3 `returnUrl` externos → `/painel/anuncios` | PASS |
| Limites de taxa | `reports/verify-artifacts/evidence/phase2-rate-limits.txt` (do `/verify`) | políticas medidas no artefato; o balde compartilhado do `auth` é o V-01/SC-02 | PASS (com SC-02) |
| Cookie de sessão | `dast-auth.sh` (A3) | `Gazeta.Team`: `secure; httponly; samesite=lax` | PASS |

Os testes automatizados que cobrem as mesmas superfícies em processo (`AccessMatrixTests`, `OwnershipMatrixTests`, `PhotosSecurityTests`, `AuthRateLimitTests`, `XssInAllScreensTests`) passaram nas rodadas completas do `/test` e do `/verify` (ver `reports/TEST_REPORT.md`).

## Recommendations

> Prioridade: **[P0]** = implementar ANTES de promover · **[P1]** = próxima versão (ou decisão sua antes) · **[P2]** = backlog. Cada item tem uma linha no `plans/BACKLOG.md` (seção "/scan (2026-10-07)").

1. **[P0] SC-01** — preencher `KnownProxies` com o dado do SmarterASP; aviso/falha na partida em Production com a lista vazia; IPv6 por prefixo /64 (R-11/SEC-01, R-38). Dono: `/infra` (já P0).
2. **[P0] SC-02** — separar os baldes de entrar / esqueci / redefinir (V-01) **e** fazer o limite de entrar contar só falhas (ou subir para 20 a 30 por 15 min), com teste usando o padrão de produção. Sem isso, a sexta pessoa da redação que entra do mesmo IP recebe 429 com a senha certa. Dono: `/infra` (já P0; acrescento novo do scan).
3. **[P1] SC-03** — **decisão sua:** como evitar que 5 senhas erradas travem o Administrador por 15 min (bloqueio por conta+origem, atraso progressivo, ou aviso por e-mail). Antes do `/deploy`.
4. **[P1] SC-04** — limitar e rolar o arquivo de log, amostrar o aviso 429 e pôr tempo-limite na regex de mascaramento (`/fix-issue`).
5. **[P1] SC-05** — alerta de disco no `/infra` e cota por usuário/dia de fotos (código).
6. **[P1] SC-06** — não gravar no log o que for digitado no campo de e-mail sem passar numa validação estrita (senha com `@` hoje vaza) (`/fix-issue`, correção pequena).
7. **[P1] SC-07** — **decisão sua:** guardar ou não o original com GPS por 30 dias (LGPD).
8. **[P1] SC-08, SC-09, SC-10, SC-11, SC-12, SC-13** — `/api` com senha provisória (R-34); `https` e nome do ambiente (R-39/R-46/V-05); pastas absolutas; brecha de prefixo no limite global; CEP no log; medir memória das fotos na hospedagem.
9. **[P2] SC-14 a SC-27** — tempos do login, teto absoluto de sessão, política de senha, log de acesso, cabeçalhos extras, `HttpClient`, arquivos publicados, e demais itens informativos.
10. **[P0-exceção] SC-28** — assinar a exceção da chave revogada e pôr o gitleaks no pipeline.

## Approval

> **Não preenchido por mim.** O auditor não aprova o próprio relatório. A decisão sobre SC-28 (exceção) e sobre SC-03/SC-07 é do Product Owner/Security Lead.

| Role | Name | Date | Decision |
|------|------|------|----------|
| Security Lead | | | APPROVED / REJECTED |
| Exceção SC-28 (chave de API revogada no histórico) | | | CONCEDIDA / NEGADA |
