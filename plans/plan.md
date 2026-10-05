# Plano: GazetaMarketplace v1

> **Em resumo:** este plano quebra o SPEC (Approved v1.2, 128 cenários) e a arquitetura (Gate 2 aprovado) em **39 tarefas** verticais, em 7 fases. Cada tarefa entrega algo verificável e diz quais cenários, NFRs e ADRs ela cumpre. **Nada foi inventado:** o que o SPEC não define virou pendência bloqueante (seção 9), e não tarefa. Quem lê: a equipe que vai construir (`/build`), testar (`/test`) e revisar (`/review`).

## 1. Inputs consumed

- `specs/SPEC.md` — Approved v1.2: US-001 a US-015, 128 cenários `@US-XXX-Snn`, NFR-01 a NFR-24, Apêndice B (campos por categoria), suposições S e A
- `specs/categories.md` (124 categorias ativas, ids reais), `specs/discovery/gazetaonline-lookups.md` (listas de opções)
- `specs/wireframes/screens/*.md` (15 telas) e `specs/wireframes/flows/*.md` (4 fluxos)
- `architecture/ARCHITECTURE.md` (§3 módulos, §4 NFRs com mecanismo, §6 modelo de dados, §7 segurança, §8 erros, §9 deploy, §13 perguntas em aberto)
- `architecture/adr/ADR-001` a `ADR-012`; `architecture/api/openapi.yaml` (12 operações); `architecture/design-system.md`
- `architecture/diagrams/` (contexto, contêineres, componentes e 6 sequências)
- Regras: `.claude/rules/` (incl. `overrides/lang-dotnet.md` e `overrides/database-sqlserver.md`) e `.claude/local/CLAUDE.local.md`

## 2. Impact Analysis

**Blast radius:** 3 projetos de produção + 2 de teste + 1 ferramenta · 8 módulos · 12 endpoints JSON (todos novos) + páginas Razor do site e do painel · migration de banco: **sim** (esquema inicial + cargas) · breaking change: **não** (greenfield) · arquitetura: conforme o Gate 2 (sem redesenho).

| Item de mudança (história) | Tipo | Componentes afetados | Endpoints / contrato | Dados / banco | Telas | Risco e disciplina |
|---|---|---|---|---|---|---|
| Solução, configuração, erros, segurança HTTP, saúde (0.1–0.5) | new | Web, Core, Infrastructure; middlewares | `healthLive`, `healthReady` (`/health/live`, `/health/ready`); ProblemDetails global | — | Páginas de erro | Controle transversal: amplia o escopo do `/secure` (cabeçalhos, antiforgery, limitadores) |
| Persistência base (0.6) | new | `AppDbContext`, `AuditEntries`, `IDbConnection` (Dapper), `SqlBuilder` | — | Migration inicial; script idempotente | — | Migrations só por script (ADR-004); SQL Server do provedor (AR-03); Dapper não herda filtros do EF |
| Layout e design system (0.7) | new | `_Layout`, `base.css`, `api.js` | — | — | Todas | Contraste e foco medidos (design-system §2.4) |
| US-006, US-007, US-014 (1.1–1.4) | new | Identity, `AccountController`, `UsersController`, `SendGridEmailSender` | Páginas de conta e usuários | Tabelas do Identity | Entrar, primeiro acesso, recuperar, usuários | Segurança crítica: política de senha, bloqueio, sessão; chaves do Data Protection (AR-01) |
| Categorias e campos (2.1–2.4, US-013 em 2.6) | new | `Category`, `CategoryTree`, `FieldGroupRegistry`, 18 grupos | — | `Categories` com carga de 124 + mães | Categorias | Regras do Apêndice B e da A7; listas novas pendentes (PL-01) |
| Catálogo de veículos (2.5) | new | `IVehicleCatalog`, `VehicleCatalogController`, ferramenta de exportação (leitura e carga em lote com Dapper) | `listVehicleBrands`, `listVehicleModels`, `listVehicleModelYears`, `listVehicleVersions` | 4 tabelas + script de carga | Formulário de Carros e Motos | A5 (jurídico) bloqueia o lançamento; credencial antiga do GazetaOnline nunca reutilizada |
| US-015 (2.7) | new | `SiteSettingsService` | Página de configurações | `SiteSettings` | Configurações | Bloqueia a publicação (US-010-S08) |
| Modelo do anúncio (3.1) | new | `Ad`, `AdService`, `Normalizer` | — | `Ads` com JSON, colunas calculadas e índices | — | Duas representações (C# e SQL): teste diferencial obrigatório (ADR-002) |
| CEP (3.2) | new | `ViaCepLookup`, `CepController`, `cep.js` | `getCep` | `CepCache`, `Cities` | Campo de CEP | Dependência externa; contrato 503 × 404 |
| US-008, US-009 (3.3, 3.5, 3.7) | new | `AdsController` (Painel), `DraftService`, `SubmitForReviewService`, fotos | `uploadAdPhoto`, `setAdPhotoCover`, `deleteAdPhoto` | `Ads`, `AdPhotos` | Formulário do anúncio | Segurança: autoria, antiforgery, texto puro |
| Fotos (3.4, 3.5, 3.6) | new | `IPhotoStorage`, `IImageProcessor`, `IAdPhotoService`, `PhotosController`, `AdPhotosController`, `OriginalsCleanupService` | `getPhotoFile`, `uploadAdPhoto`, `setAdPhotoCover`, `deleteAdPhoto` | Pasta persistente fora da raiz | — | HEIC/Magick.NET (AR-05); originais com GPS em `_originals/`; limpeza depende do IIS (ADR-005) |
| Componentes do anúncio (3.8) | new | `AdCardViewComponent`, `_AdValue`, `_AdBody` | — | — | Card, detalhe, pré-visualização | A4 e A6 (design-system §5) |
| US-010, US-011, US-012 (4.1–4.4) | new | `ReviewQueueController`, `ReviewService`, `TakedownService`, `PanelAdListService`, `IPanelAdListReadRepository` (Dapper) | Páginas do painel | `Ads` (situação), `AuditEntries` | Fila, pré-visualização, lista | Decisão simultânea (`rowversion`); registro de ações |
| US-001 a US-005 (5.1–5.5) | new | `HomeController`, `CategoryController`, `AdController`, `SearchController`, `FavoritesController`, `ISearchReadRepository` (Dapper) | `listAdsByIds` | Consultas em `Ads` e `Categories` | Início, categoria, detalhe, busca, favoritos | Desempenho (NFR-04); A6 na busca; só publicados |
| SEO (5.6) | new | `SeoController` | `/sitemap.xml`, `/robots.txt` | — | — | Condicional à S7 |
| Verificações transversais (6.1–6.3) | new | Projetos de teste e Playwright | — | — | Todas | Sustenta NFR-13, 15, 16, 17 e as de desempenho |

## 3. Build-time testing scope

- **`/build`:** testes de unidade (MSTest + fakes escritas à mão) e de integração com o `WebApplicationFactory` em memória (Template A de `rules/testing.md`), sem Docker. Cada cenário tem **um teste próprio** que confere o *Then* daquele cenário, no nome `US001S01_…`; as páginas são conferidas no HTML renderizado.
- **Cenários que dependem de JavaScript, de layout ou de navegador** (marcados `(E2E, /test)` nas tarefas) **também** ganham um teste Playwright, escrito no `/build` com o projeto de teste e executado no `/test`.
- **Consultas Dapper (busca, lista do painel e carga do catálogo):** o Dapper não roda em banco em memória. No `/build`, as telas e as regras de apresentação usam os *read repositories* falsos; a consulta em si é provada no `/test` com SQL Server real (testes `…QueryTests` de cada tarefa).
- **Adiado ao `/test`:** TestContainers com SQL Server real (consultas Dapper, teste diferencial das colunas calculadas, script de migration aplicado duas vezes, volume da v1) e toda a execução Playwright (inclui axe-core e larguras).
- **Adiado ao `/verify`:** LCP, INP e CLS no artefato publicado.
- Cobertura: linha ≥ 80% e ramo ≥ 75% (greenfield, projeto inteiro); lista de métodos com 0% no relatório do `/test`.
- Regra de ouro: primeiro o teste que falha, depois o código (`/build` usa a skill `tdd`).

## 4. Summary table

| # | Task | User story | Estimate | Phase |
|---|------|------------|---------:|-------|
| 0.1 | Estruturar a solução em Web, Core e Infrastructure | Foundation | M | 0 |
| 0.2 | Configuração tipada, segredos fora do repositório e ambiente de desenvolvimento | Foundation | M | 0 |
| 0.3 | Logs estruturados, correlação e contrato de erros | Foundation | L | 0 |
| 0.3b | Migrar pacotes Microsoft de rc.2/preview para GA 10.0.12 | Foundation | S | 0 |
| 0.4 | Segurança HTTP: cabeçalhos, antiforgery, limites de requisição e HTTPS | Foundation | M | 0 |
| 0.5 | Verificações de saúde, cultura pt-BR e fuso | Foundation | S | 0 |
| 0.6 | Persistência base: DbContext, Dapper, auditoria, concorrência e script de migrations | Foundation | M | 0 |
| 0.7 | Layout base, tokens do design system e componentes de estado de página | Foundation | L | 0 |
| 1.1 | Entrar e sair do painel, com bloqueio e sessão | US-006 | L | 1 |
| 1.2 | Primeiro acesso e Administrador inicial | US-006 | M | 1 |
| 1.3 | Gerenciar usuários da equipe | US-014, US-006 | L | 1 |
| 1.4 | Recuperar senha esquecida por e-mail | US-007 | L | 1 |
| 2.1 | Árvore de categorias e carga inicial | Foundation | M | 2 |
| 2.2 | Grupos de campos: framework e grupos Serviços, Vagas, Produtos em geral e Imóveis | Foundation | L | 2 |
| 2.3 | Grupos de campos de veículos e peças | Foundation | M | 2 |
| 2.4 | Grupos de campos de aluguel, telefonia, eletro, eletrônicos, roupas e máquinas | Foundation | M | 2 |
| 2.5 | Catálogo de veículos: tabelas, consulta encadeada e ferramenta de exportação | Foundation | L | 2 |
| 2.6 | Gerenciar categorias | US-013 | L | 2 |
| 2.7 | Telefone/WhatsApp do site | US-015 | S | 2 |
| 3.1 | Modelo do anúncio, situações e autorização por autoria | Foundation | L | 3 |
| 3.2 | Consulta de CEP no servidor, com cache e lista de municípios | Foundation | L | 3 |
| 3.3 | Criar e editar rascunho do anúncio | US-008 | L | 3 |
| 3.4 | Processamento e armazenamento de fotos | Foundation | L | 3 |
| 3.5 | Enviar, reordenar e remover fotos do anúncio | US-008 | L | 3 |
| 3.6 | Limpeza diária dos originais de foto | Foundation | S | 3 |
| 3.7 | Enviar anúncio para revisão | US-009 | M | 3 |
| 3.8 | Componentes de apresentação do anúncio: card, valor e bloco sem foto | Foundation | M | 3 |
| 4.1 | Fila de revisão e pré-visualização | US-010 | M | 4 |
| 4.2 | Publicar e rejeitar anúncios | US-010 | L | 4 |
| 4.3 | Despublicar e arquivar anúncios | US-011 | M | 4 |
| 4.4 | Lista de anúncios do painel | US-012 | M | 4 |
| 5.1 | Página inicial e páginas de categoria | US-001 | M | 5 |
| 5.2 | Detalhe do anúncio e galeria de fotos | US-003 | L | 5 |
| 5.3 | Contato por telefone e WhatsApp | US-004 | S | 5 |
| 5.4 | Busca e filtros | US-002 | L | 5 |
| 5.5 | Favoritos no navegador | US-005, US-011 | M | 5 |
| 5.6 | SEO básico das páginas públicas | Foundation | S | 5 |
| 6.1 | Verificação transversal de acesso e de texto digitado | Foundation | M | 6 |
| 6.2 | Base de verificação de acessibilidade e responsividade | Foundation | M | 6 |
| 6.3 | Orçamentos de desempenho | Foundation | M | 6 |

**Total ideal points** (S=1, M=2, L=3): 89

## 5. Phases, Checkpoints, and Tasks

> **Nomenclatura (2026-10-03):** o código segue a regra "inglês no código, português na interface" de `.claude/rules/overrides/lang-dotnet.md`. Os caminhos e nomes das tarefas 0.x a 1.2 refletem o que existe hoje. Os nomes das tarefas futuras (2.x a 6.x) foram traduzidos pelo glossário da regra; cada tarefa confirma seus nomes quando for planejada. Nomes de métodos de teste continuam em português (exceção documentada).

## Fase 0 — Fundação

### Task 0.1: Estruturar a solução em Web, Core e Infrastructure

**User stories**: **Foundation** — Fundação: sem ela nenhuma história tem onde ser construída.

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**NFRs covered**: `NFR-22`

**References**: ADR-001

**Objective**: Criar os projetos Core e Infrastructure, corrigir o projeto Web e fixar as regras de build do repositório.

**Files to modify**:
- src/GazetaMarketplace.Core/GazetaMarketplace.Core.csproj (novo)
- src/GazetaMarketplace.Infrastructure/GazetaMarketplace.Infrastructure.csproj (novo)
- src/GazetaMarketplace.Web/GazetaMarketplace.Web.csproj (remover Nullable e ImplicitUsings = enable)
- `GazetaMarketplace.slnx`
- Directory.Packages.props (só pacotes já aprovados; os novos entram na 0.2 a 0.6 depois da AR-06)
- tests/GazetaMarketplace.Web.Tests/GazetaMarketplace.Web.Tests.csproj (referências ao Core e à Infrastructure)

**Acceptance Criteria**:
- [ ] `dotnet build` da solução passa com `Nullable` e `ImplicitUsings` desligados e `TreatWarningsAsErrors` ligado, sem override nos `.csproj`
- [ ] O Core não referencia pacotes de ASP.NET nem de EF Core; a Infrastructure referencia só o Core; o Web referencia os dois
- [ ] Os projetos de exemplo do template (`ClaudeStack.*`, `Example.*`) continuam fora da solução e, se ainda existirem em `src/` e `tests/`, são removidos do disco
- [ ] `AddCore()` e `AddInfrastructure(IConfiguration)` existem e são chamados no `Program.cs`

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Architecture/LayerDependenciesTests.Core_NaoReferencia_AspNetNemEfCore`
- `tests/GazetaMarketplace.Web.Tests/Architecture/LayerDependenciesTests.Infrastructure_ReferenciaSomenteCore`
- `tests/GazetaMarketplace.Web.Tests/Architecture/BuildRulesTests.Projetos_NaoSobrescrevem_NullableEImplicitUsings`

**Dependencies**: —

**Verification**: Done when every test under "Tests to add" passes, plus manual check: `dotnet build` e `dotnet test` verdes; conferir à mão que a solução abre no Visual Studio 2026.

**Estimate**: M

### Task 0.2: Configuração tipada, segredos fora do repositório e ambiente de desenvolvimento

**User stories**: **Foundation** — Fundação de segurança (NFR-14).

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**NFRs covered**: `NFR-14`

**References**: ADR-011

**Objective**: Fazer toda a configuração vir de opções tipadas validadas na partida e tirar `appsettings.Development.json` do git.

**Files to modify**:
- `src/GazetaMarketplace.Web/Program.cs`
- src/GazetaMarketplace.Core/Configuration/*.cs (opções: PhotoStorage, Logging, DataProtection, SendGrid, Bootstrap)
- src/GazetaMarketplace.Web/appsettings.json (só valores não sensíveis)
- .gitignore (appsettings.Development.json, web.Production.config, *.pubxml.user)
- src/GazetaMarketplace.Web/web.Production.config.example (novo, sem valores reais)
- git rm --cached src/GazetaMarketplace.Web/appsettings.Development.json (primeira ação desta tarefa)

**Acceptance Criteria**:
- [ ] Em `Production`, o site **não sobe** se faltar conexão com o banco, pasta de fotos, pasta de logs, pasta de chaves ou chave do SendGrid, e o erro aparece no log
- [ ] `appsettings.Development.json` deixa de ser rastreado pelo git e está no `.gitignore`; valores locais usam User Secrets
- [ ] Variáveis de ambiente valem mais que o `appsettings.json` (nomes com `__`)
- [ ] O arquivo de exemplo da transformação do `web.config` existe e não contém segredo
- [ ] Nenhum segredo aparece em arquivo versionado (varredura com `git grep`)
- [ ] **RC-20:** o `web.Production.config.example` traz no topo o aviso de que o arquivo real nunca vai para o git e fica em cofre de senhas, e nenhum valor real

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Configuration/OptionsTests.Producao_SemPastaDeFotos_FalhaNaPartida`
- `tests/GazetaMarketplace.Web.Tests/Configuration/OptionsTests.Producao_SemChaveSendGrid_FalhaNaPartida`
- `tests/GazetaMarketplace.Web.Tests/Configuration/OptionsTests.VariavelDeAmbiente_TemPrioridadeSobreAppsettings`
- `tests/GazetaMarketplace.Web.Tests/Configuration/SecretsTests.Repositorio_NaoContem_ConnectionStringComSenha`
- `tests/GazetaMarketplace.Web.Tests/Configuration/SecretsTests.ArquivoDeExemplo_TemAvisoDeGuardaESemValorReal`

**Dependencies**: 0.1

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Partir o site em `Production` sem as variáveis e ver a falha no log; `git ls-files | grep appsettings.Development` vazio.

**Estimate**: M

### Task 0.3: Logs estruturados, correlação e contrato de erros

**User stories**: **Foundation** — Fundação de observabilidade e de contrato (NFR-18, NFR-22).

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**NFRs covered**: `NFR-18`, `NFR-22`

**References**: ADR-010

**Objective**: Serilog em arquivo JSON, identificador de correlação em toda requisição e respostas de erro no formato ProblemDetails com os 8 códigos do contrato.

**Files to modify**:
- src/GazetaMarketplace.Core/Exceptions/*.cs (AppException e derivadas)
- `src/GazetaMarketplace.Web/Middleware/CorrelationIdMiddleware.cs`
- `src/GazetaMarketplace.Web/Middleware/ExceptionHandlingMiddleware.cs`
- src/GazetaMarketplace.Infrastructure/Logging/SerilogConfiguration.cs (mascaramento de senha, token e e-mail)
- src/GazetaMarketplace.Web/Views/Shared/Error.cshtml (código de referência)

**Acceptance Criteria**:
- [ ] Toda requisição tem `X-Correlation-ID` na resposta e em cada linha de log
- [ ] Nenhum log contém senha, token, link de redefinição ou e-mail completo (e-mail mascarado)
- [ ] Cada código do contrato (`VALIDATION_ERROR`, `UNAUTHORIZED`, `FORBIDDEN`, `NOT_FOUND`, `CONFLICT`, `RATE_LIMITED`, `CEP_SERVICE_UNAVAILABLE`, `INTERNAL_ERROR`) é devolvido com o status e o `traceId` corretos
- [ ] Erro não tratado em página Razor mostra a tela de erro com o código de referência, sem pilha
- [ ] Logs em arquivo rotativo diário com retenção de 14 dias

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Middleware/CorrelationIdTests.Requisicao_SemCabecalho_GeraEDevolveOId`
- `tests/GazetaMarketplace.Web.Tests/Middleware/CorrelationIdTests.Requisicao_ComCabecalho_PropagaOMesmoId`
- `tests/GazetaMarketplace.Web.Tests/Logging/MaskingTests.Email_ApareceMascarado`
- `tests/GazetaMarketplace.Web.Tests/Logging/MaskingTests.Senha_NuncaApareceNoLog`
- `tests/GazetaMarketplace.Web.Tests/Middleware/ErrorsTests.CadaExcecao_MapeiaParaSeuCodigoEStatus`
- `tests/GazetaMarketplace.Web.Tests/Middleware/ErrorsTests.ErroInesperado_NaoExpoePilha_EtrazTraceId`
- `tests/GazetaMarketplace.Web.Tests/Middleware/ErrorsTests.TraceId_DoProblemDetails_IgualAoCodigoDeReferenciaDaTela`

**Dependencies**: 0.1, 0.2

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Provocar um erro 500 e achar a linha no arquivo de log pelo código de referência mostrado na tela.

**Estimate**: L

### Task 0.3b: Migrar pacotes Microsoft de rc.2/preview para GA 10.0.12

**User stories**: **Foundation** — Fundação: o SDK e o runtime são GA; pacotes em rc.2 geram conflitos de versão (NU1109) a cada pacote novo.

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**NFRs covered**: `NFR-22`

**References**: —

**Objective**: Levar para a versão GA (10.0.12) os pacotes Microsoft do `Directory.Packages.props`, MSTest para 4.4.1 e Playwright MSTest para 1.63.0, sem pré-lançamento no grafo de dependências.

**Files to modify**:
- Directory.Packages.props

**Acceptance Criteria**:
- [ ] Nenhum pacote em `rc`, `preview`, `beta` ou `alpha` no grafo resolvido (`dotnet list package --include-transitive`)
- [ ] `dotnet build` (Debug e Release) sem avisos e `dotnet test` sem regressão em relação à 0.3

**Tests to add**:
- — (a suíte existente é a prova)

**Dependencies**: 0.3

**Verification**: Done when `dotnet build -c Release` e `dotnet test` passam como antes da migração.

**Estimate**: S

### Task 0.4: Segurança HTTP: cabeçalhos, antiforgery, limites de requisição e HTTPS

**User stories**: **Foundation** — Fundação de segurança (NFR-10, NFR-11).

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**NFRs covered**: `NFR-10`, `NFR-11`, `NFR-06`

**References**: ADR-003

**Objective**: Cabeçalhos de segurança e CSP em todas as respostas, antiforgery global, limitadores de requisição e HTTPS obrigatório.

**Files to modify**:
- `src/GazetaMarketplace.Web/Middleware/SecurityHeadersMiddleware.cs`
- src/GazetaMarketplace.Web/Program.cs (AutoValidateAntiforgeryToken, AddRateLimiter, HSTS, UseHttpsRedirection)
- `src/GazetaMarketplace.Web/Filters/AntiforgeryJsonFilter.cs`

**Acceptance Criteria**:
- [ ] Toda resposta tem `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy` e `Content-Security-Policy` (sem `unsafe-inline` em scripts); HSTS só em produção
- [ ] Nenhuma política CORS é registrada; pedido de outra origem não recebe cabeçalhos de permissão
- [ ] POST, PUT, PATCH e DELETE sem token antiforgery são recusados (formulários e endpoints JSON)
- [ ] Limitadores: login e "Esqueci minha senha" 5 por 15 min por IP; global 100 por minuto por IP no site público; excesso devolve 429 `RATE_LIMITED`
- [ ] **RC-10:** com a hospedagem atrás de proxy, `UseForwardedHeaders` (só `X-Forwarded-For` e `X-Forwarded-Proto`, só de proxies conhecidos do provedor) define o IP do cliente usado pelos limitadores; sem isso todos os visitantes dividiriam um único limite
- [ ] **RC-21:** limite global de 1 MB para o corpo de requisição (formulários e JSON), com exceção do envio de foto, que tem o limite próprio de 11 MB

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Security/HeadersTests.TodaResposta_TemOsCabecalhosObrigatorios`
- `tests/GazetaMarketplace.Web.Tests/Security/HeadersTests.Hsts_SoEmProducao`
- `tests/GazetaMarketplace.Web.Tests/Security/CspTests.Csp_NaoPermiteScriptInline`
- `tests/GazetaMarketplace.Web.Tests/Security/AntiforgeryTests.PostSemToken_E_Recusado`
- `tests/GazetaMarketplace.Web.Tests/Security/AntiforgeryTests.EndpointJson_ExigeCabecalhoRequestVerificationToken`
- `tests/GazetaMarketplace.Web.Tests/Security/CorsTests.Nenhuma_PoliticaCors_Registrada`
- `tests/GazetaMarketplace.Web.Tests/Security/RateLimiterTests.SextaTentativaDeLogin_Devolve429`
- `tests/GazetaMarketplace.Web.Tests/Security/RateLimiterTests.LimiteGlobal_Devolve429AposCemPedidos`
- `tests/GazetaMarketplace.Web.Tests/Security/RateLimiterTests.IpDoCliente_VemDoCabecalhoEncaminhado_DeProxyConfiavel`
- `tests/GazetaMarketplace.Web.Tests/Security/RateLimiterTests.CabecalhoEncaminhado_DeOrigemNaoConfiavel_E_Ignorado`
- `tests/GazetaMarketplace.Web.Tests/Security/BodyLimitTests.CorpoAcimaDe1Mb_Devolve413_ExcetoNoEnvioDeFoto`

**Dependencies**: 0.3

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Conferir os cabeçalhos com `curl -I` em Development e com `ASPNETCORE_ENVIRONMENT=Production`.

**Estimate**: M

### Task 0.5: Verificações de saúde, cultura pt-BR e fuso

**User stories**: **Foundation** — Fundação (NFR-20 e ADR-010).

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**NFRs covered**: `NFR-20`

**References**: ADR-010, architecture/api/openapi.yaml#healthLive, architecture/api/openapi.yaml#healthReady

**Objective**: Expor `/health/live` e `/health/ready`, fixar a cultura pt-BR e o fuso de exibição.

**Files to modify**:
- `src/GazetaMarketplace.Web/Program.cs`
- `src/GazetaMarketplace.Web/HealthChecks/DatabaseAndMigrationHealthCheck.cs`
- `src/GazetaMarketplace.Core/Formatting/CurrencyAndDateFormatter.cs`

**Acceptance Criteria**:
- [ ] `/health/live` responde 200 sem tocar no banco; `/health/ready` responde 200 só com o banco acessível e a última migration aplicada, 503 caso contrário, sem detalhes internos
- [ ] Cultura fixa `pt-BR`: valores em R$ (centavos só quando diferentes de zero, S29) e datas `dd/mm/aaaa`
- [ ] Datas gravadas em UTC e exibidas em `America/Sao_Paulo`

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Health/HealthTests.Live_Responde200_SemBanco`
- `tests/GazetaMarketplace.Web.Tests/Health/HealthTests.Ready_SemBanco_Responde503_SemDetalhes`
- `tests/GazetaMarketplace.Web.Tests/Health/HealthTests.Ready_ComMigrationPendente_Responde503`
- `tests/GazetaMarketplace.Web.Tests/Formatting/CurrencyTests.Centavos_SoQuandoNaoSaoZero`
- `tests/GazetaMarketplace.Web.Tests/Formatting/DateTests.Utc_ExibidaEmSaoPaulo_ComoDdMmAaaa`

**Dependencies**: 0.1

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Chamar os dois endereços no navegador.

**Estimate**: S

### Task 0.6: Persistência base: DbContext, Dapper, auditoria, concorrência e script de migrations

**User stories**: **Foundation** — Fundação de dados (ADR-004).

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**NFRs covered**: `NFR-04`

**References**: ADR-004

**Objective**: Criar o `AppDbContext` (escrita e migrations), o acesso Dapper para leitura complexa (`IDbConnection`, `SqlBuilder`), as convenções do projeto, as colunas de auditoria, `rowversion`, a tabela de auditoria de ações e o processo de geração do script idempotente. **A migration inicial cria só `AuditEntries`; cada tarefa seguinte adiciona a sua** (Identity na 1.1, categorias na 2.x, anúncios e catálogo na 3.x), uma migration por mudança lógica.

**Files to modify**:
- `src/GazetaMarketplace.Core/Entities/BaseEntity.cs`
- `src/GazetaMarketplace.Core/Entities/AuditEntry.cs`
- `src/GazetaMarketplace.Core/Interfaces/IAuditLog.cs`, `ICurrentUser.cs`
- `src/GazetaMarketplace.Infrastructure/Data/AppDbContext.cs`
- `src/GazetaMarketplace.Infrastructure/Data/Configurations/*.cs`
- `src/GazetaMarketplace.Infrastructure/Data/AuditLog.cs`
- `src/GazetaMarketplace.Infrastructure/Data/EfDatabaseReadiness.cs` (substitui a provisória da 0.5)
- `src/GazetaMarketplace.Infrastructure/Data/AppDbContextFactory.cs` (design-time, cadeia fictícia)
- `src/GazetaMarketplace.Infrastructure/Data/Migrations/` (migration inicial `CreateAuditEntries`)
- `src/GazetaMarketplace.Infrastructure/Data/DapperConfiguration.cs` (`IDbConnection` scoped com a cadeia do EF Core; conexão e transação do `DbContext` para escrita atômica)
- `src/GazetaMarketplace.Infrastructure/Data/SqlBuilder.cs` (fragmentos fixos, parâmetros nomeados, ordenação só por colunas permitidas)
- `src/GazetaMarketplace.Infrastructure/Data/SqlFragments.cs` (fragmento único do somente-publicados)
- `src/GazetaMarketplace.Web/Security/HttpCurrentUser.cs`
- `db/scripts/gazeta-idempotente.sql` (script único e cumulativo, regenerado a cada migration)
- `tests/GazetaMarketplace.Web.Tests/Support/TestDatabase.cs` (fixture SQLite em memória; `rowversion` por trigger)
- Directory.Packages.props (Dapper 2.1.89 já aprovado) e referências no projeto Infrastructure (EF Core SqlServer, Design com `PrivateAssets="all"`, Dapper)

**Acceptance Criteria**:
- [ ] `SaveChangesAsync` preenche `CreatedAt/By` e `UpdatedAt/By` em UTC (`By` é `int?`, nulo para ações do sistema)
- [ ] Entidades editáveis têm `RowVersion`; conflito vira `ConflictException`
- [ ] `AuditEntries` registra quem fez o quê e quando por um serviço único (`IAuditLog`)
- [ ] `dotnet ef migrations script --idempotent` gera o script sem erro e pode ser aplicado duas vezes
- [ ] Nenhum `Database.Migrate()` em produção
- [ ] `IDbConnection` fica registrado como *scoped* com a mesma cadeia de conexão; uma escrita Dapper que precisa ser atômica com o EF usa a conexão e a transação do `DbContext`
- [ ] `SqlBuilder` só aceita fragmentos fixos e parâmetros nomeados; coluna ou direção de ordenação fora da lista permitida é ignorada, nunca concatenada
- [ ] Toda escrita Dapper (`Execute*` com `INSERT`, `UPDATE`, `DELETE` ou `MERGE`) tem um comentário `// Dapper: <motivo>` no código (ADR-004)
- [ ] Escrita Dapper em tabela com `RowVersion` confere a versão no `WHERE` e preenche as colunas de auditoria
- [ ] **RC-16:** `IAuditLog` só tem operação de acrescentar; nenhuma tela, endpoint ou serviço edita ou apaga entradas (o `SaveChanges` recusa alterar ou apagar uma `AuditEntry`)
- [ ] `/health/ready` usa a implementação real: banco acessível e nenhuma migration pendente

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Persistence/AuditingTests.Salvar_PreencheCriacaoEAlteracao`
- `tests/GazetaMarketplace.Web.Tests/Persistence/ConcurrencyTests.DuasEdicoes_DaMesmaLinha_GeramConflito`
- `tests/GazetaMarketplace.Web.Tests/Persistence/AuditLogTests.Registra_AtorAcaoAlvoEData`
- `tests/GazetaMarketplace.Web.Tests/Persistence/AuditLogTests.NaoExisteOperacaoParaEditarOuApagarEntradas`
- `tests/GazetaMarketplace.Web.Tests/Persistence/MigrationsTests.Script_Idempotente_PodeSerAplicadoDuasVezes (TestContainers, roda no /test)`
- `tests/GazetaMarketplace.Web.Tests/Persistence/ConcorrenciaSqlServerTests.RowVersionReal_GeraConflito (TestContainers, roda no /test; no /build o rowversion é simulado por trigger no SQLite)`
- `tests/GazetaMarketplace.Web.Tests/Architecture/DapperJustificationTests.TodaEscritaDapper_TemComentarioComOMotivo`
- `tests/GazetaMarketplace.Web.Tests/Architecture/DapperTests.ReadRepositories_ImplementamInterfacesDoCore`
- `tests/GazetaMarketplace.Web.Tests/Persistence/SqlBuilderTests.OrdenacaoForaDaLista_EIgnorada`
- `tests/GazetaMarketplace.Web.Tests/Persistence/SqlBuilderTests.Valores_SempreViramParametros`
- `tests/GazetaMarketplace.Web.Tests/Persistence/SqlBuilderTests.FragmentoSomentePublicados_E_UnicoEReutilizado`

**Dependencies**: 0.1, 0.2

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Gerar o script e aplicá-lo duas vezes num SQL Server local.

**Estimate**: M

### Task 0.7: Layout base, tokens do design system e componentes de estado de página

**User stories**: **Foundation** — Fundação de interface: todas as telas usam estes layouts.

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**NFRs covered**: `NFR-16`, `NFR-17`, `NFR-03`, `NFR-15`

**References**: `architecture/design-system.md` (v. 2026-10-03, visual do template Autolist), `docs/templates/autolist/LEIAME.md`

**Objective**: Implementar o *shell* de interface com o visual do template Autolist: `base.css` e `components/*.css` escritos à mão (CSS puro, `--bs-*` e `--app-*`), cabeçalho com busca, rodapé, layout do painel com menu superior, Poppins e Font Awesome 4.7 locais, o módulo `api.js` e os estados de página (vazio, erro, sem resultado, carregando). Cards, detalhe e formulários ganham a skin nas tarefas das telas (3.x), usando `docs/templates/autolist/` como referência.

**Decisões de escopo (Product Owner, 2026-10-03)**:
- O `style.css` do template **não** é carregado nem editado (927 KB, 3.140 `!important`, `@import` externo, foco suprimido); o visual é extraído para o CSS do projeto.
- Primária `#d72213` (a do template, `#e72a1a`, dá 4,43:1 e falha o AA); Poppins 400/600/700 autohospedada; só Font Awesome 4.7 como biblioteca de ícones.
- Painel com **menu superior** dos wireframes (não o menu lateral do template); nenhum plugin jQuery do template.

**Files to modify**:
- `src/GazetaMarketplace.Web/wwwroot/lib/` (Bootstrap 5.3.8, `font-awesome/`, `poppins/`; procedência em `LEIAME.md`)
- `src/GazetaMarketplace.Web/wwwroot/css/poppins.css`, `base.css`, `sem-js.css`
- `src/GazetaMarketplace.Web/wwwroot/css/components/bootstrap-tema.css`, `layout.css`, `estados.css`
- `src/GazetaMarketplace.Web/wwwroot/js/modules/api.js`, `js/pages/layout.js`
- `src/GazetaMarketplace.Web/Views/Shared/_Layout.cshtml`, `_PanelLayout.cshtml`
- `src/GazetaMarketplace.Web/Views/Shared/_EstadoVazio.cshtml, _EstadoErro.cshtml, _EstadoSemResultado.cshtml, _Esqueleto.cshtml`
- `docs/templates/autolist/` (referência; sem a chave do Google Maps) e `architecture/design-system.md`

**Acceptance Criteria**:
- [x] `base.css` contém os tokens e os três ajustes de acessibilidade de `architecture/design-system.md` §2.1 e nenhum arquivo externo (CDN, fonte, ícones) é carregado
- [x] Layouts com `lang="pt-BR"`, link "Ir para o conteúdo", `nav` com `aria-current`, `main` com foco programático e menu recolhível que funciona sem JavaScript
- [x] Nenhum `Html.Raw` com texto de usuário; scripts só como módulos externos (nenhum script ou evento inline)
- [x] Estados de página: vazio, erro com código de referência e "Tentar novamente", sem resultado com "Limpar filtros" e esqueleto com altura reservada
- [x] `api.js` trata ProblemDetails e envia o token antiforgery em escritas
- [x] **RC-17:** nenhum módulo de `wwwroot/js` usa `innerHTML`, `outerHTML` ou `insertAdjacentHTML` com texto vindo do servidor; cards e mensagens são montados com `textContent` ou `<template>`
- [x] Visual do Autolist aplicado ao cabeçalho (degradê, painel de busca escuro, botão primário), ao rodapé escuro e ao fundo da página, sem carregar o `style.css` do template
- [x] Primária `#d72213` com branco a 5,09:1; todos os pares da §2.4 do design system a 4,5:1 (texto) ou 3:1 (foco e bordas)
- [x] Poppins (`woff2`) e Font Awesome 4.7 (`woff2`) servidos pelo próprio site; nenhum `@import` nem URL absoluta em CSS ou HTML
- [x] Ícones decorativos com `aria-hidden="true"`; nenhum plugin jQuery do template no site
- [x] Procedência, integridade, o que foi extraído e o que foi removido documentados em `docs/templates/autolist/LEIAME.md` e `wwwroot/lib/LEIAME.md`

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Layout/LayoutTests.Html_TemLangPtBr_SkipLink_E_MainComFoco`
- `tests/GazetaMarketplace.Web.Tests/Layout/LayoutTests.PaginaNaoCarregaRecursosExternos`
- `tests/GazetaMarketplace.Web.Tests/Layout/LayoutTests.NenhumScriptOuEventoInline`
- `tests/GazetaMarketplace.Web.Tests/Layout/XssTests.TextoDeUsuario_EhCodificado_NaoExecuta`
- `tests/GazetaMarketplace.Web.Tests/Layout/StatesTests.EstadoDeErro_MostraCodigoDeReferenciaETentarNovamente`
- `tests/GazetaMarketplace.Web.Tests/Layout/JsModulesTests.NenhumModuloUsaInnerHtmlComTextoDoServidor`
- `tests/GazetaMarketplace.Web.Tests/Layout/BaseCssTests` (tokens, três ajustes, contraste da primária `#d72213`, foco e borda)
- `tests/GazetaMarketplace.Web.Tests/Layout/FontsAndThemeTests` (`woff2` da Poppins e do Font Awesome servidos localmente; ícones decorativos com `aria-hidden`; nenhuma biblioteca do template carregada)
- `tests/GazetaMarketplace.Web.Tests/Layout/PanelMenuTests`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Layout/BaseCssTests.Foco_TemContornoComContraste (E2E, /test)` e, no mesmo arquivo, Poppins carregada, primária, borda, rolagem horizontal, CSP e axe-core

**Dependencies**: 0.1, 0.3

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Abrir as telas-base em 320 px e em 1280 px, navegar só pelo teclado e comparar o cabeçalho com `docs/templates/autolist/Html/index.html`.

**Estimate**: L

---
## Checkpoint 0 — Fundação completa

**Fechado em 2026-10-03 pelo Product Owner.** Evidência: `dotnet build` com 0 avisos; `dotnet test` com 178 testes (168 passam, 10 de navegador ignorados sem `GAZETA_BASE_URL`, e passam quando o site está no ar).

**Verify before proceeding**:
- [x] Solução em 3 projetos compila sem avisos
- [x] Partida em Production falha sem configuração (`OpcoesTests.Producao_Sem*_FalhaNaPartida`, tarefa 0.2)
- [x] Logs e erros com código de referência
- [x] Cabeçalhos e antiforgery ativos
- [x] Layout base navegável só pelo teclado (skip link, foco visível e axe-core por teste automático; a verificação manual com NVDA fica para o `/test`)
- [ ] Cobertura de linha ≥ 80% nos projetos novos — **ainda não medida**: a ferramenta de cobertura não está configurada (`testing.md` pede para confirmar o comando no primeiro `/test`); aceito pelo Product Owner ao fechar o checkpoint

---

## Fase 1 — Equipe e acesso

### Task 1.1: Entrar e sair do painel, com bloqueio e sessão

**User stories**: US-006

**Scenarios covered**: `@US-006-S01`, `@US-006-S02`, `@US-006-S03`, `@US-006-S04`, `@US-006-S05`, `@US-006-S06`, `@US-006-S07`, `@US-006-S08`

**NFRs covered**: `NFR-06`, `NFR-07`, `NFR-08`, `NFR-13`

**References**: ADR-003

**Objective**: Autenticar a equipe com Identity e cookie, com políticas de papel, bloqueio por tentativas e sessão de 30 minutos.

**Files to modify**:
- `src/GazetaMarketplace.Infrastructure/Identity/AppUser.cs` (`IdentityUser<int>`) e `AppRole.cs` (`IdentityRole<int>`), conforme ADR-003; configurações e `HasData` dos dois papéis em `Data/Configurations/`; migration `AddIdentity` e `db/scripts/gazeta-idempotente.sql` regenerado
- `src/GazetaMarketplace.Infrastructure/Data/AppDbContext.cs` (herda de `IdentityDbContext` e chama `base.OnModelCreating`)
- `src/GazetaMarketplace.Core/Configuration/AuthenticationOptions.cs` (`Authentication:SessionMinutes`, padrão 30, entre 1 e 120)
- `src/GazetaMarketplace.Web/Security/IdentityExtensions.cs` (Identity, cookie, políticas), `TeamSignInManager.cs`, `TeamClaimsPrincipalFactory.cs`, `LoginFailureCounter.cs`, `AccessPolicies.cs` — a ligação do Identity fica no Web (decisão do Product Owner)
- `src/GazetaMarketplace.Web/Areas/Panel/Controllers/PanelControllerBase.cs`, `AccountController.cs` (entrar, sair, acesso negado) e `AdsController.cs` (**provisório**)
- `src/GazetaMarketplace.Web/Areas/Panel/Views/Account/SignIn.cshtml`, `AccessDenied.cshtml` e `Ads/Index.cshtml`, `ReviewQueue.cshtml` (**provisórias**)
- `src/GazetaMarketplace.Web/Program.cs` (`AddTeamIdentity`, `UseAuthentication`)

**Decisões do Product Owner (2026-10-03)**:
- **Bloqueio por origem conta falhas, não requisições:** `LoginFailureCounter`, em memória, 5 falhas em 15 minutos por IP, zerado pela entrada com sucesso, testado com relógio falso. O limitador "auth" da 0.4 (que conta toda requisição) não é usado no login, para uma redação atrás do mesmo IP poder entrar de manhã.
- **Conta bloqueada pelo Identity mostra a mensagem genérica** ("E-mail ou senha inválidos, ou conta desativada"); o bloqueio é real e vai para o log, mas a tela não revela que a conta existe.
- **Páginas "Meus anúncios" e "Fila de revisão" são provisórias** (título e menu), só para US-006-S01, S02 e S07 terem aonde chegar. A 4.4 troca "Meus anúncios" e a 4.1 troca a "Fila de revisão".
- **Papéis por `HasData`** na migration, sem inicializador na partida. A política `Writer` aceita Redator e Administrador.
- **`/painel/acesso-negado`** nasce aqui (faz parte da configuração do cookie); o cenário US-006-S10 continua na 1.3. O link "Esqueci minha senha" aponta para `/painel/esqueci-minha-senha`, criada na 1.4 (404 até lá).
- **Implantação:** depois da 1.1 ainda não existe Administrador (nasce na 1.2). Publicar 1.1 e 1.2 juntas, nunca só a 1.1.

**Acceptance Criteria**:
- [x] `@US-006-S01` (@happy): Redator entra no painel — o *Then* do SPEC é atendido
- [x] `@US-006-S02` (@happy): Administrador entra no painel — o *Then* do SPEC é atendido
- [x] `@US-006-S03` (@happy): Sair do painel — o *Then* do SPEC é atendido
- [x] `@US-006-S04` (@negative): E-mail ou senha incorretos — o *Then* do SPEC é atendido
- [x] `@US-006-S05` (@negative): Conta desativada — o *Then* do SPEC é atendido
- [x] `@US-006-S06` (@negative): Muitas tentativas de entrada — o *Then* do SPEC é atendido
- [x] `@US-006-S07` (@edge): Abrir uma página do painel sem estar logado — o *Then* do SPEC é atendido
- [x] `@US-006-S08` (@edge): Sessão expirada por inatividade — o *Then* do SPEC é atendido
- [x] Política de senha (8+, maiúscula, minúscula, número, símbolo), hash do Identity, bloqueio de conta por 5 falhas em 15 min, cookie `HttpOnly`/`Secure`/`SameSite=Lax` com expiração deslizante de 30 min e revalidação do `SecurityStamp` a cada 5 min
- [x] Redator cai em "Meus anúncios" e Administrador na "Fila de revisão"; "Sair" encerra a sessão e o botão Voltar não mostra o painel
- [x] Mensagem de falha sempre genérica (não revela se a conta existe nem se está desativada)
- [x] **RC-16:** o log registra cada entrada, saída, falha e bloqueio de login (sem a senha e com o e-mail mascarado)
- [x] **RC-18:** o endereço de retorno depois do login só é aceito se for local (`Url.IsLocalUrl`); qualquer outro vai para a página inicial do painel

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Team/AccountTests.US006S01` a `US006S08` — um teste por cenário (`@US-006-S01` a `@US-006-S08`), mais campos em branco, Redator na fila (acesso negado) e Administrador na área do Redator
- `tests/GazetaMarketplace.Web.Tests/Team/AccountTests.US006S06_SeisPessoasDaMesmaRedacao_EntramDeManhaSemBloqueio` — o contador conta falhas
- `tests/GazetaMarketplace.Web.Tests/Security/LoginFailureCounterTests` (6 testes, relógio falso)
- `tests/GazetaMarketplace.Web.Tests/Account/SessionTests.Cookie_Tem_HttpOnly_Secure_SameSite_E_30Min`, `SessaoMinutos_ConfiguraAExpiracao`, `SessaoMinutosForaDoIntervalo_ImpedeAPartida`, `UsuarioDesativado_PerdeAcessoAposRevalidacao`
- `tests/GazetaMarketplace.Web.Tests/Account/PasswordTests.Politica_RejeitaSenhasFracas` (+ hash e e-mail repetido)
- `tests/GazetaMarketplace.Web.Tests/Account/AccountTests.FalhaEBloqueioDeLogin_SaoRegistradosNoLog` e `EntradaESaida_SaoRegistradasNoLog_SemSenha` (RC-16)
- `tests/GazetaMarketplace.Web.Tests/Account/AccountTests.ReturnUrlExterno_E_Ignorado` (RC-18) e `ContaInexistente_SenhaErrada_Desativada_E_Bloqueada_RespondemIgual`
- `tests/GazetaMarketplace.Web.Tests/Persistence/IdentityModelTests` e `MigrationsTests` (chave `int`, colunas, papéis, migration no script, modelo em dia)
- `tests/GazetaMarketplace.Web.Tests/Configuration/OptionsTests.Autenticacao_*` (padrão 30, intervalo 1 a 120)
- **E2E (rodam no `/test`, ignorados sem as variáveis):** `tests/GazetaMarketplace.Web.Tests.Playwright/Team/AccountE2ETests.US006S03_SairDoPainel` e `US006S08_SessaoExpiradaPorInatividade`. Variáveis: `GAZETA_BASE_URL`, `GAZETA_E2E_EMAIL`, `GAZETA_E2E_PASSWORD` (conta no banco de teste) e, para o S08, `GAZETA_E2E_SESSION_MINUTES` igual ao `Authentication__SessionMinutes` com que o site foi iniciado. Já passam localmente, sem conta: `SignInE2ETests` (axe-core, CSP, rótulos e 320 px)

**Dependencies**: 0.3, 0.4, 0.6, 0.7

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Entrar com os dois papéis no navegador, esperar 30 min (ou reduzir o tempo em teste) e ver o redirecionamento.

**Estimate**: L

### Task 1.2: Primeiro acesso e Administrador inicial

**User stories**: US-006

**Scenarios covered**: `@US-006-S09`

**NFRs covered**: `NFR-07`

**References**: ADR-003, ADR-011

**Objective**: Obrigar a troca da senha provisória no primeiro acesso e criar o primeiro Administrador a partir das variáveis de ambiente.

**Decisões aprovadas**:
- Qualquer conta no papel Administrador, ativa ou não, conta como "já existe Administrador". O inicializador não cria outro nem reativa a conta desativada.
- Variáveis inválidas (e-mail malformado, senha fora da política, só uma das duas) com intenção de criar derrubam a partida com erro claro, sem repetir a senha. Banco indisponível só registra Error e o site sobe (`/health/ready` avisa). Sem variáveis, o banco não é tocado.
- **Premissa (sem alterar o SPEC):** a nova senha não pode ser igual à provisória; a tela recusa com "A nova senha precisa ser diferente da provisória".
- `AddDefaultTokenProviders()` com `TokenLifespan` de 1 hora (a 1.4 reaproveita o mesmo token de recuperação).
- `IdentityErrorDescriber` próprio em pt-BR (`TeamIdentityErrorDescriber`), reaproveitado pela 1.3 (US-014-S06).
- A troca usa `GeneratePasswordResetTokenAsync` + `ResetPasswordAsync`, que valida a política antes de gravar: senha fraca deixa a provisória intacta.

**Files modified/created**:
- `src/GazetaMarketplace.Infrastructure/Identity/BootstrapAdminInitializer.cs` (`IHostedService`; `InvalidBootstrapException`)
- `src/GazetaMarketplace.Web/Areas/Panel/Filters/MustChangePasswordFilter.cs` (aplicado em `PanelControllerBase`)
- `src/GazetaMarketplace.Web/Areas/Panel/Controllers/PasswordController.cs`, `Models/SetPasswordViewModel.cs`, `Views/Password/SetPassword.cshtml` (controller separado porque `[AllowAnonymous]` e `[Authorize]` não convivem na base)
- `src/GazetaMarketplace.Web/Security/TeamIdentityErrorDescriber.cs`, `IdentityExtensions.cs`, `TeamClaimsPrincipalFactory.cs` (claim `must_change_password`), `AccessPolicies.cs`
- `AccountController` (entrada com troca pendente vai para `/painel/definir-senha`), `_PanelLayout.cshtml` (menu oculto durante a troca), `Program.cs` (falha de bootstrap derruba a partida)
- Correção encontrada pelos testes: `asp-validation-for` só funciona em `<span>`; as telas Entrar e DefinirSenha usavam `<div>` e a mensagem de campo não aparecia.

**Acceptance Criteria**:
- [x] `@US-006-S09` (@edge): Primeiro acesso exige trocar a senha provisória — o *Then* do SPEC é atendido
- [x] Com `MustChangePassword = true`, qualquer página do painel leva a "Defina sua nova senha" até a troca
- [x] Na partida, se não existir Administrador e as variáveis `Bootstrap__AdminEmail` e `Bootstrap__AdminPassword` existirem, o usuário é criado com troca obrigatória; nada é criado se já houver Administrador
- [x] A senha inicial nunca aparece no log
- [x] **RC-19:** se `Bootstrap__AdminEmail` ou `Bootstrap__AdminPassword` ainda existirem e já houver Administrador, o site registra Warning pedindo a remoção das variáveis

**Tests added** (253 no projeto Web.Tests, todos passando):
- `Team/FirstAccessTests` — `US006S09` fluxo completo, filtro em todas as páginas, menu oculto com Sair disponível, senhas fracas recusadas com a provisória ainda valendo (atomicidade), nova igual à provisória, divergência/vazio, Administrador cai na fila, sem necessidade/anônimo, senhas fora do log
- `Account/BootstrapAdminTests` — criação, Administrador existente (ativo, desativado), só Redator, RC-19, variável única, senha fora do log, duas partidas, sem variáveis não toca o banco, senha fraca/e-mail inválido/variável única derrubam a partida, banco indisponível registra Error
- `Account/ErrorDescriberTests` — mensagens em pt-BR, token de 1 hora, token de uso único, claim

**Verificação por mutação** (cada quebra derrubou exatamente 1 teste; código restaurado e suíte verde): (a) só contas ativas contam como Administrador → `ComAdministradorDesativado_TambemNaoCriaOutro`; (b) troca por Remove+Add em vez de token → `SenhaFraca_E_Recusada_ESenhaProvisoriaContinuaValendo`; (c) sem o filtro → `EnquantoASenhaForProvisoria_TodaPaginaDoPainelLevaATroca`.

**Não verificado aqui** (vai para `/test`): partida com variáveis contra SQL Server real e o script de migrations aplicado nele.

**Dependencies**: 1.1

**Verification**: Done when every test above passes, plus manual check: Publicar localmente com as variáveis, entrar e ser levado à troca.

**Estimate**: M

### Task 1.3: Gerenciar usuários da equipe

**User stories**: US-014, US-006

**Scenarios covered**: `@US-014-S01`, `@US-014-S02`, `@US-014-S03`, `@US-014-S04`, `@US-014-S05`, `@US-014-S06`, `@US-014-S07`, `@US-014-S08`, `@US-014-S09`, `@US-014-S10`, `@US-006-S10`

**NFRs covered**: `NFR-07`, `NFR-13`

**References**: ADR-003

**Objective**: Tela de usuários do Administrador: criar, mudar papel, desativar, reativar e redefinir a senha, sempre com ao menos um Administrador ativo.

**Decisões**:
- As regras ficam em `IUserManagement` (Core) e `UserManagement` (Infrastructure), não no controller: o Core não conhece o Identity.
- Cada operação que escreve roda numa transação **serializável**: contar os Administradores ativos e escrever dependem um do outro, e duas requisições ao mesmo tempo não podem se rebaixar juntas.
- As mensagens de e-mail repetido e e-mail inválido seguem o texto exato da SPEC ("Já existe um usuário com este e-mail", "Informe um e-mail válido"); o `TeamIdentityErrorDescriber` da 1.2 foi alinhado.
- Reativar não pede nova senha nem troca obrigatória (S04); só zera o bloqueio por tentativas. Redefinir senha também destrava a conta.
- A edição muda só o papel (wireframe): nome e e-mail aparecem, mas não se editam nesta versão.
- A auditoria grava `user.create`, `user.change_role`, `user.deactivate`, `user.reactivate` e `user.reset_password` com o id do ator e do alvo; os valores são só papel e situação. **Nunca** vão para a auditoria a senha nem o e-mail. Recusas de regra (desativar a si mesmo, último Administrador) gravam resultado Negado; erro de preenchimento do formulário não grava nada, porque nada mudou.
- Confirmar a desativação e redefinir a senha são **páginas**, não janelas: o fluxo completo funciona sem JavaScript (`frontend.md`). As janelas do wireframe (`alertdialog`/`dialog`) e o módulo `users-index.js` ficam como melhoria futura (BACKLOG); por isso a tarefa não cria JavaScript.

**Files created/modified**:
- `src/GazetaMarketplace.Core/Team/IUserManagement.cs`, `TeamMember.cs`, `UserManagementResult.cs`, `UserManagementMessages.cs`
- `src/GazetaMarketplace.Infrastructure/Identity/UserManagement.cs`; registro em `ServiceCollectionExtensions.cs`
- `src/GazetaMarketplace.Web/Areas/Panel/Controllers/UsersController.cs` (`[Route("painel/usuarios")]`, política Administrator)
- `src/GazetaMarketplace.Web/Areas/Panel/Models/UserViewModels.cs`
- `src/GazetaMarketplace.Web/Areas/Panel/Views/Users/Index.cshtml`, `New.cshtml`, `Edit.cshtml`, `ConfirmDeactivation.cshtml`, `ResetPassword.cshtml`, `_UserActions.cshtml`; `Views/Shared/_FieldErrors.cshtml`, `_GeneralError.cshtml`
- `src/GazetaMarketplace.Web/Security/TeamIdentityErrorDescriber.cs` (mensagens da SPEC)

**Acceptance Criteria**:
- [x] `@US-014-S01` (@happy): Criar uma conta de Redator — o *Then* do SPEC é atendido
- [x] `@US-014-S02` (@happy): Mudar o papel de um usuário — o *Then* do SPEC é atendido
- [x] `@US-014-S03` (@happy): Desativar uma conta — o *Then* do SPEC é atendido (a parte "os anúncios continuam existindo" só ganha prova com anúncios, na Fase 3; aqui o cadastro permanece, pois não há exclusão)
- [x] `@US-014-S04` (@happy): Reativar uma conta — o *Then* do SPEC é atendido
- [x] `@US-014-S05` (@negative): E-mail já cadastrado — o *Then* do SPEC é atendido
- [x] `@US-014-S06` (@negative): Senha provisória fraca — o *Then* do SPEC é atendido
- [x] `@US-014-S07` (@negative): E-mail em formato inválido — o *Then* do SPEC é atendido
- [x] `@US-014-S08` (@negative): Desativar a própria conta — o *Then* do SPEC é atendido
- [x] `@US-014-S09` (@negative): Remover o último administrador — o *Then* do SPEC é atendido
- [x] `@US-014-S10` (@happy): Redefinir a senha de alguém da equipe — o *Then* do SPEC é atendido
- [x] `@US-006-S10` (@negative): Redator tenta abrir uma página exclusiva do administrador — o *Then* do SPEC é atendido (provado com `/painel/usuarios`; a página Categorias só existe na 2.6)
- [x] Criar conta com senha provisória na mesma política de senha; e-mail único e válido
- [x] Desativar encerra as sessões em até 5 min; ninguém desativa a própria conta; sempre resta ao menos um Administrador ativo
- [x] Redefinir senha: muda a senha, encerra as sessões, exige troca no próximo acesso e registra em `AuditEntries`; o botão não aparece para a própria pessoa nem para contas desativadas
- [x] Redator que abre uma página exclusiva do Administrador recebe "Você não tem permissão" (US-006-S10)
- [x] **RC-16:** criar, mudar papel, desativar, reativar e redefinir senha gravam em `AuditEntries` o ator, a ação, o alvo e o resultado

**Tests added** (282 no projeto Web.Tests, todos passando):
- `tests/GazetaMarketplace.Web.Tests/Team/UsersTests` — `US014S01` a `US014S10`, `US006S10`, sessão aberta de conta desativada, dois Administradores, Administrador desativado não conta, Administrador desativado com sessão aberta não desativa o último, senha fraca na redefinição, botão ausente para a própria pessoa e conta desativada, nome em branco/longo, papel desconhecido, 404, ordem da lista, escrita sem token antiforgery
- `tests/GazetaMarketplace.Web.Tests/Team/UserAuditTests` — cinco ações gravam ator/ação/alvo/resultado, `RedefinirSenha_RegistraQuemEQuando`, sem senha nem e-mail na auditoria, recusas gravam Negado, erro de formulário não grava
- `tests/GazetaMarketplace.Web.Tests.Playwright/Team/UsersE2ETests` — `US014S03` e `US014S10` (rodam no `/test`; precisam de `GAZETA_BASE_URL` e de um Administrador em `GAZETA_E2E_EMAIL`/`GAZETA_E2E_PASSWORD`)

**Verificação por mutação** (cada quebra derrubou ao menos 1 teste; código restaurado): sem a regra do último Administrador na troca de papel (4 testes); sem a mesma regra ao desativar (1); sem a recusa de desativar a si mesmo (2); redefinir senha sem exigir troca (1); desativar sem auditoria (1); tela de usuários sem a política Administrator (1).

**Não verificado aqui** (vai para `/test`): a transação serializável contra o SQL Server real (o SQLite dos testes serializa tudo, então a corrida entre dois pedidos simultâneos só se prova lá) e os E2E.

**Dependencies**: 1.1, 1.2

**Verification**: Done when every test above passes, plus manual check: Criar, desativar e redefinir contas no navegador.

**Estimate**: L

### Task 1.4: Recuperar senha esquecida por e-mail

**User stories**: US-007

**Scenarios covered**: `@US-007-S01`, `@US-007-S02`, `@US-007-S03`, `@US-007-S04`, `@US-007-S05`, `@US-007-S06`, `@US-007-S07`

**NFRs covered**: `NFR-09`, `NFR-07`

**References**: ADR-009, ADR-003

**Objective**: Pedido de redefinição pelo SendGrid, link de 1 hora e uso único, resposta sempre neutra.

**Files to modify**:
- `src/GazetaMarketplace.Core/Interfaces/IEmailSender.cs`
- `src/GazetaMarketplace.Infrastructure/Email/SendGridEmailSender.cs`
- src/GazetaMarketplace.Infrastructure/Email/LogEmailSender.cs (desenvolvimento)
- `src/GazetaMarketplace.Web/Areas/Panel/Views/Account/Forgot.cshtml, Redefinir.cshtml`

**Acceptance Criteria**:
- [x] `@US-007-S01` (@happy): Pedir a redefinição de senha — o *Then* do SPEC é atendido
- [x] `@US-007-S02` (@happy): Definir uma nova senha pelo link — o *Then* do SPEC é atendido
- [x] `@US-007-S03` (@negative): E-mail não cadastrado — o *Then* do SPEC é atendido
- [x] `@US-007-S04` (@negative): Link de redefinição expirado — o *Then* do SPEC é atendido
- [x] `@US-007-S05` (@negative): Link de redefinição já utilizado — o *Then* do SPEC é atendido
- [x] `@US-007-S06` (@negative): Nova senha que não cumpre a política — o *Then* do SPEC é atendido
- [x] `@US-007-S07` (@negative): Confirmação de senha diferente — o *Then* do SPEC é atendido
- [x] A resposta de "Esqueci minha senha" é a mesma exista ou não a conta e mesmo se o envio falhar; a falha é registrada no log com o `traceId`
- [x] Token vale 1 hora; ao redefinir, o `SecurityStamp` muda e o mesmo link deixa de valer
- [x] E-mail enviado por `HttpClient` à API v3 do SendGrid, sem SDK, com chave em variável de ambiente
- [x] Destinatário mascarado no log; token e link nunca aparecem
- [x] **RC-11:** no máximo 3 pedidos de redefinição por hora por e-mail: acima disso nada é enviado e a resposta continua igual; o log registra Warning quando o total diário de e-mails chegar a 80 (o plano gratuito do SendGrid permite 100 por dia)
- [x] **RC-12:** redefinir a senha com sucesso zera o contador de falhas e o bloqueio da conta (quem foi bloqueado de propósito volta a entrar)
- [x] **RC-13:** a resposta de "Esqueci minha senha" não depende de a conta existir: o envio do e-mail fica fora do caminho da resposta (em segundo plano), e a resposta tem a mesma forma, o mesmo código e não espera o SendGrid

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Team/PasswordRecoveryTests.US007S01_PedirARedefinicaoDeSenha` — `@US-007-S01`
- `tests/GazetaMarketplace.Web.Tests/Team/PasswordRecoveryTests.US007S02_DefinirUmaNovaSenhaPeloLink` — `@US-007-S02`
- `tests/GazetaMarketplace.Web.Tests/Team/PasswordRecoveryTests.US007S03_EMailNaoCadastrado` — `@US-007-S03`
- `tests/GazetaMarketplace.Web.Tests/Team/PasswordRecoveryTests.US007S04_LinkDeRedefinicaoExpirado` — `@US-007-S04`
- `tests/GazetaMarketplace.Web.Tests/Team/PasswordRecoveryTests.US007S05_LinkDeRedefinicaoJaUtilizado` — `@US-007-S05`
- `tests/GazetaMarketplace.Web.Tests/Team/PasswordRecoveryTests.US007S06_NovaSenhaQueNaoCumpreAPolitica` — `@US-007-S06`
- `tests/GazetaMarketplace.Web.Tests/Team/PasswordRecoveryTests.US007S07_ConfirmacaoDeSenhaDiferente` — `@US-007-S07`
- `tests/GazetaMarketplace.Web.Tests/Account/SendGridTests.EnviaPost_ParaApiV3_ComBearer_SemVazarChaveNoLog`
- `tests/GazetaMarketplace.Web.Tests/Account/SendGridTests.FalhaDoSendGrid_RespostaContinuaNeutra`
- `tests/GazetaMarketplace.Web.Tests/Account/TokenTests.Token_ExpiraEmUmaHora`
- `tests/GazetaMarketplace.Web.Tests/Account/PasswordRecoveryTests.QuartoPedidoNaMesmaHora_NaoEnviaEmail_MasRespondeIgual`
- `tests/GazetaMarketplace.Web.Tests/Account/PasswordRecoveryTests.TotalDiarioChegaA80_RegistraWarning`
- `tests/GazetaMarketplace.Web.Tests/Account/PasswordRecoveryTests.RedefinirComSucesso_LimpaOBloqueio`
- `tests/GazetaMarketplace.Web.Tests/Account/PasswordRecoveryTests.ContaExistenteEInexistente_TemMesmaRespostaESemEsperarOEnvio`

**Decisões da implementação (aprovadas pelo Product Owner em 2026-10-03):**
- **Contador no banco** (tabela `PasswordRecoveryAttempts`: Id, Email, Ip, RequestedAt; índices em `(Email, RequestedAt)`, `(Ip, RequestedAt)` e `(RequestedAt)`, este último para o total diário e a limpeza), porque o IIS recicla o processo e zeraria um contador em memória. Migration `AddPasswordRecoveryAttempts`; limpeza diária de entradas com mais de 24 horas (`PasswordRecoveryCleanupService`).
- **Limites:** 3 pedidos por hora por e-mail e 10 por hora por IP; os pedidos de e-mail inexistente contam do mesmo jeito (a resposta é indistinguível). O aviso de 80 por dia conta **pedidos** das últimas 24 horas (limite superior dos e-mails enviados).
- **E-mail** em texto simples e HTML, em português, remetente `SendGrid:FromEmail`; assunto "Redefinição de senha — GazetaMarketplace".
- **Log:** em Production o remetente é só o SendGrid e o log mostra apenas "E-mail enviado para m***@dominio", sem link; em Development o `LogEmailSender` escreve o e-mail inteiro no console (o log mascara `code=`).
- **Token próprio** (`RecoveryTokenProvider`): mesmo desenho do token do Identity, mas com o relógio do site e com resultado "expirou" separado de "já foi usado".
- **Endereço do link vem de `Site:BaseUrl`** (obrigatório em Production), nunca do cabeçalho Host: sem isso, um atacante poderia apontar o e-mail da vítima para outro servidor.
- **Envio em segundo plano** (fila em memória + `PasswordRecoveryWorker`): a resposta não espera o SendGrid nem depende de a conta existir (RC-13). Pedidos na fila se perdem se o processo reiniciar; a pessoa pede de novo.

**Dependencies**: 1.1, 0.3

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Pedir o link em desenvolvimento e ler o e-mail no log; usar o link duas vezes.

**Estimate**: L

---
## Checkpoint 1 — Equipe completa

**Verify before proceeding**:
- [x] Entrar, sair, primeiro acesso, recuperar e gerenciar usuários funcionam
- [x] Nenhum log tem senha, token ou e-mail completo
- [x] Cobertura ≥ 80% e sem regressão da fase anterior

---

## Fase 2 — Categorias, campos e catálogo

### Task 2.1: Árvore de categorias e carga inicial

**User stories**: **Foundation** — Fundação: toda história de anúncio e de vitrine depende da árvore.

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**References**: ADR-002, ADR-004

**Objective**: Criar a entidade de categoria (até 3 níveis), carregar a árvore real de `specs/categories.md` com os ids reais e expor a árvore em cache.

**Files to modify**:
- `src/GazetaMarketplace.Core/Categories/Category.cs`
- `src/GazetaMarketplace.Core/Categories/ICategoryTree.cs`
- src/GazetaMarketplace.Infrastructure/Categories/CategoriaTree.cs (cache de memória, 10 min)
- src/GazetaMarketplace.Infrastructure/Data/Seeds/categorias.sql (gerado de specs/categories.md)
- `src/GazetaMarketplace.Infrastructure/Data/Configurations/CategoryConfiguration.cs`

**Acceptance Criteria**:
- [x] A carga cria 147 linhas (22 mães, a intermediária "Autopeças" e as 124 postáveis ativas), com os ids reais e `IsPostable` correto; os 5 animais vivos (79, 80, 82, 83, 91) ficam fora da v1; a sequência de ids continua depois do maior (próxima categoria = 156)
- [x] Profundidade máxima 3 validada no serviço; nome único entre irmãs; `Slug` único
- [x] A árvore em cache é invalidada ao editar; descendentes de uma categoria são resolvidos sem consulta por nível

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Categories/InitialLoadTests.Carga_Cria124CategoriasPostaveis_ComIdsReais`
- `tests/GazetaMarketplace.Web.Tests/Categories/InitialLoadTests.Autopecas_EstaNoTerceiroNivel`
- `tests/GazetaMarketplace.Web.Tests/Categories/ArvoreTests.Descendentes_IncluemTodosOsNiveis`
- `tests/GazetaMarketplace.Web.Tests/Categories/ArvoreTests.QuartoNivel_E_Recusado`
- `tests/GazetaMarketplace.Web.Tests/Categories/CacheTests.EditarCategoria_InvalidaOCache`

**Decisões da implementação (aprovadas pelo Product Owner em 2026-10-03):**
- **Carga por `HasData` do EF** (`Data/Seeds/InitialCategories.cs`, 147 linhas com o slug final), e não por `Seeds/categorias.sql`: entra no script idempotente como os papéis do Identity. O `ParityTests` relê `specs/categories.md` e confere linha a linha. Os ids 24, 25 e 32 continuam ausentes.
- **Slug:** o explícito do arquivo vale como está (29, inclusive `cars`); os demais são gerados do nome (sem acento, minúsculas, hífen, sem especiais) com unicidade global. Em colisão de nomes, **a postável fica com o slug limpo** e a não postável ganha `-grupo` (`-grupo-2`…); duas postáveis: menor id fica com o limpo, a outra ganha `-2`. Resultado: `servicos-grupo` (7), `servicos` (66), `vagas-de-emprego-grupo` (13), `vagas-de-emprego` (96).
- **Invalidação do cache dentro do `AppDbContext`** (grava → `ICategoryTree.Invalidate()`), e não em um interceptor do EF: os hosts de teste trocam as opções do contexto e perderiam o interceptor, e o teste passaria por um caminho diferente do site.
- `FieldGroup` fica nulo em toda a carga; os grupos entram nas tarefas 2.2 a 2.4. `IsSystem = 1` em toda a carga.
- Nomes (inglês): `Core/Categories/{Category, CategoryRules, SlugGenerator, CategoryTreeSnapshot, ICategoryTree}.cs` e `Infrastructure/Categories/CategoryTree.cs`; migration `AddCategories`.

**Dependencies**: 0.6

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Conferir a contagem e os ids contra `specs/categories.md`.

**Estimate**: M

### Task 2.2: Grupos de campos: framework e grupos Serviços, Vagas, Produtos em geral e Imóveis

**User stories**: **Foundation** — Fundação: formulário, validação, detalhe e filtros dependem dos grupos.

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**References**: ADR-002

**Objective**: Criar o framework de grupos de campos em código (campos, obrigatórios, limites, filtros, rótulos) e implementar os grupos mais visíveis do SPEC.

**Files to modify**:
- `src/GazetaMarketplace.Core/Campos/FieldGroup.cs, FieldDefinition.cs, FieldGroupRegistry.cs`
- `src/GazetaMarketplace.Core/Campos/Grupos/Servicos.cs, Vagas.cs, ProdutosEmGeral.cs, Imoveis.cs`
- src/GazetaMarketplace.Core/Campos/Listas/*.cs (listas com os ids de specs/discovery/gazetaonline-lookups.md)

**Acceptance Criteria**:
- [x] Cada categoria usa o grupo próprio ou o do ancestral mais próximo (A7 a); categoria nova herda o do pai
- [x] Serviços: sem preço, título até 120, "Informações adicionais" até 6000, até 6 fotos, tipo obrigatório com as 11 opções na ordem
- [x] Vagas de emprego: sem fotos, título até 90, "Informações adicionais" até 6000, 14 áreas (múltipla, opcional), preço exibido como Salário
- [x] Produtos em geral: condição obrigatória (5 opções) e tipo de produto; Imóveis: tipo, vender ou alugar, quartos, área e demais campos do Apêndice B
- [x] Os limites por grupo (`HasPrice`, `MaxPhotos`, `TitleMaxLength`, `DescriptionMaxLength`, `DescriptionLabel`) são a única fonte dessas regras

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Fields/HerancaTests.Autopecas_UsaGrupoPecas_E_CategoriaNovaHerdaDoPai`
- `tests/GazetaMarketplace.Web.Tests/Fields/ServicosTests.SemPreco_6Fotos_Tipo11Opcoes`
- `tests/GazetaMarketplace.Web.Tests/Fields/VagasTests.SemFotos_Titulo90_14Areas`
- `tests/GazetaMarketplace.Web.Tests/Fields/ProdutosEmGeralTests.CondicaoObrigatoria_5Opcoes`
- `tests/GazetaMarketplace.Web.Tests/Fields/ListasTests.IdsDasListas_SaoOsDoGazetaOnline`

**Decisões da implementação (aprovadas pelo Product Owner em 2026-10-03):**
- **Framework em código** (`Core/Fields/`): `FieldGroup`, `FieldDefinition` (com `AppliesToCategories` e `OptionsByCategory`, porque Imóveis muda por categoria), `FieldList`, `FieldGroupRegistry` e `FieldLists` (listas com os ids do GazetaOnline; as de Serviços e Vagas, ids 1 a N na ordem do SPEC). Quatro grupos: `Services`, `Jobs`, `GeneralProducts` (o padrão para quem não tem grupo na cadeia) e `RealEstate`.
- **Grupo gravado só nas categorias que o definem** (26, 27, 30, 31 → Imóveis; 66 → Serviços; 96 → Vagas), por migration de dados `AssignFieldGroupsToRealEstateServicesAndJobs`. Peças será gravado em Autopeças (id 3) na 2.3, e os ids 38 a 42 herdam (A7 a). Até a 2.3/2.4, as demais caem em Produtos em geral; nada consome isso antes da 3.1.
- **"Tipo de produto"** é texto livre opcional de até 60 caracteres com autocomplete dinâmico; **Marca** idem. Contratos `GET /api/v1/brands/suggest` e `GET /api/v1/product-types/suggest` no `openapi.yaml`; implementação na fase 3, quando a tabela de anúncios existir.
- **Área (m²)** é decimal com 2 casas (mín. 0,01; máx. 99.999.999,99); **Condomínio e IPTU** em centavos (`bigint`), teto de R$ 99.999.999,99; **Quartos, Banheiros e Vagas** de 0 a 20 (0 = kitnet; suposição, revisar se aparecer caso real de mais de 20). ADR-002 emendado.
- O teste "Autopeças usa Peças" mudou para a 2.3 (o grupo Peças só nasce lá).

**Dependencies**: 2.1

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Conferir as listas e os limites contra o Apêndice B do SPEC.

**Estimate**: L

### Task 2.3: Grupos de campos de veículos e peças

**User stories**: **Foundation** — Fundação: ficha de veículo do formulário e filtros da busca.

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**References**: ADR-002

**Objective**: Implementar os grupos Carros, Motos, Caminhões e Ônibus, Barcos e aeronaves e Peças.

**Files to modify**:
- `src/GazetaMarketplace.Core/Campos/Grupos/Carros.cs, Motos.cs, CaminhoesOnibus.cs, BarcosAeronaves.cs, Pecas.cs`

**Acceptance Criteria**:
- [x] Carros e Motos: marca → modelo → ano → versão do catálogo (obrigatórios), quilometragem obrigatória e demais campos do Apêndice B; Motos também com cilindrada
- [x] Caminhões e Ônibus: ano do modelo e quilometragem obrigatórios; Barcos e aeronaves: ano, horas de uso e tipo obrigatórios
- [x] Peças: condição obrigatória, tipo de peça e cor; filtros específicos só em Carros e Motos (marca, modelo, ano, km) e em Caminhões e Ônibus (ano, km)

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Fields/CarrosTests.Obrigatorios_MarcaModeloAnoVersaoKm`
- `tests/GazetaMarketplace.Web.Tests/Fields/MotosTests.Cilindrada_Obrigatoria`
- `tests/GazetaMarketplace.Web.Tests/Fields/CaminhoesOnibusTests.AnoEKm_Obrigatorios`
- `tests/GazetaMarketplace.Web.Tests/Fields/BarcosTests.HorasDeUso_NoLugarDeKm`
- `tests/GazetaMarketplace.Web.Tests/Fields/PecasTests.Condicao_Obrigatoria_SemFiltrosDeVeiculo`
- `tests/GazetaMarketplace.Web.Tests/Fields/FiltrosTests.FiltrosEspecificos_VemDoGrupo`

**Decisões da implementação (aprovadas pelo Product Owner em 2026-10-03):**
- **Cinco grupos** (`Cars`, `Motorcycles`, `TrucksAndBuses`, `BoatsAndAircraft`, `Parts`) em `Core/Fields/Groups/`, gravados por migration de dados `AssignFieldGroupsToVehiclesAndParts` nas categorias 33, 36, 34, 35, 37 e **3 (Autopeças)**; as filhas 38 a 42 herdam (A7 a). O registro tem 9 grupos.
- **Chaves JSON** do ADR-002: `brandId`, `modelId`, `modelYear` (a mesma em Carros, Motos, Caminhões e ônibus e Barcos, para a coluna calculada `ModelYear` servir a todos), `versionId`, `km`; o Tipo de qualquer veículo é `vehicleTypeId`.
- **Marca → Modelo → Ano → Versão** de Carros (catálogo de carros) e de Motos (catálogo de motos) são campos `CatalogItem` (nível e tipo de catálogo), sem consulta real até a 2.5.
- **Ano do modelo** (Caminhões, Ônibus, Barcos) é `FieldType.ModelYear`: de 1950 ("1950 ou anterior", id 1950) até o ano atual + 1, gerado por `FieldLists.ModelYears(currentYear)` e validado por `ModelYearRules` com o `TimeProvider` do site no fuso de São Paulo.
- **Filtros (A7 c):** Carros e Motos — marca, modelo (igualdade), ano e km (faixa); Caminhões e ônibus — ano e km; Barcos, Peças e demais — nenhum. Os filtráveis do sistema todo são exatamente `brandId`, `modelId`, `modelYear`, `km` e `areaM2` (as colunas calculadas do ADR-002).
- **Barcos e aeronaves:** Horas de uso obrigatória no lugar de quilometragem. **Limites (suposições):** Km de 0 a 9.999.999; Horas de uso de 0 a 999.999; Comprimento, Largura e Altura de 0,01 a 999,99 m (2 casas).
- **Vídeo do YouTube** continua fora (Out of Scope, Q5).

**Dependencies**: 2.2

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Conferir contra o Apêndice B.

**Estimate**: M

### Task 2.4: Grupos de campos de aluguel, telefonia, eletro, eletrônicos, roupas e máquinas

**User stories**: **Foundation** — Fundação: sem estes grupos, metade das 124 categorias não aceita anúncio.

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**References**: ADR-002

**Objective**: Implementar os grupos restantes do Apêndice B: Aluguel de quartos, Temporada, Celulares, Smartwatches, Produtos de telefonia, Eletro, Eletrônicos e informática, Roupas e calçados e Máquinas.

**Files to modify**:
- `src/GazetaMarketplace.Core/Campos/Grupos/AluguelQuartos.cs, Temporada.cs, Celulares.cs, Smartwatches.cs, ProdutosTelefonia.cs, Eletro.cs, EletronicosInformatica.cs, RoupasCalcados.cs, Maquinas.cs`

**Acceptance Criteria**:
- [x] Cada grupo tem os campos, obrigatórios e listas do Apêndice B; as listas herdadas usam os ids do GazetaOnline
- [x] ~~Bloqueado até a PL-01~~ PL-01 respondida em 2026-10-03: Tamanho (roupas PP a XGG; calçados adultos 34 a 45; infantis e de bebê 16 a 33), Gênero e Marca em texto livre com autocomplete entram nos grupos

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Fields/RemainingGroupsTests` (Celulares, Eletro, Temporada, Roupas, Máquinas e os demais: obrigatórios, limites, listas por categoria)
- `tests/GazetaMarketplace.Web.Tests/Fields/AppendixBTests` (conjunto de categorias de cada um dos 18 grupos, rótulos, ordem e obrigatórios lidos da tabela da SPEC; as 124 categorias postáveis resolvem para o grupo certo)
- `tests/GazetaMarketplace.Web.Tests/Fields/ManufactureYearTests` e `ListsTests` (22 listas herdadas conferidas contra `gazetaonline-lookups.md`; Tamanho e Gênero)

**Decisões da implementação (aprovadas pelo Product Owner em 2026-10-03):**
- **Nove grupos** em `Core/Fields/Groups/` (`RoomRental`, `Seasonal`, `Phones`, `Smartwatches`, `TelephonyProducts`, `Appliances`, `ElectronicsAndComputers`, `ClothingAndShoes`, `Machinery`), gravados nas 53 categorias que os definem pela migration de dados `AssignFieldGroupsToRemainingCategories` (53 `UPDATE`, nenhuma categoria nasce nem some). Com isso são **18 grupos** e **65 categorias da carga protegidas contra exclusão** (A7 b).
- **O grupo é gravado em cada uma das 53 categorias** (Eletrônicos e informática: 26 categorias; Eletro: 7; Roupas e calçados: 8; Produtos de telefonia: 4; Máquinas: 4), não só nas mães.
- **Marca em Eletro, Eletrônicos e Máquinas é texto livre de até 60 caracteres com autocomplete** (`SuggestionSource.Brands`, PL-01); as listas de marca por tipo herdadas do GazetaOnline continuam em `gazetaonline-lookups.md` sem uso. **Modelo** (Celulares e Eletrônicos): texto de até 60. Celulares e Smartwatches usam as listas de marca do GazetaOnline (`PhoneBrand`, `SmartwatchBrand`).
- **Limites (suposições):** Temporada — Quartos 0 a 20 (0 = estúdio), Pessoas 1 a 50, Banheiros e Vagas 0 a 20; Máquinas — Ano de fabricação de 1950 até o **ano atual** (nunca futuro, diferente do ano do modelo de veículos) e Horas de uso de 0 a 999.999.
- **Tamanho muda por categoria:** roupas (64, 68, 72, 76) PP a XGG; calçados adultos (65, 69) 34 a 45; calçados infantis e de bebê (75, 77) 16 a 33 (o id é a própria numeração). **Gênero** (Masculino, Feminino, Unissex, Infantil) é uma lista só para as oito categorias.
- **Tipo** de Produtos de telefonia e de Eletro muda por categoria (uma lista por categoria); **Marcas compatíveis** só em 44 e 45; **Capacidade** só em 128.
- Roupas e calçados **não tem Marca** (o Apêndice B não pede).

**Dependencies**: 2.2

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Conferir contra o Apêndice B e contra as respostas da PL-01. **Feito em 2026-10-03.**

**Estimate**: M

### Task 2.5: Catálogo de veículos: tabelas, consulta encadeada e ferramenta de exportação

**User stories**: **Foundation** — Fundação: o formulário de Carros e Motos não funciona sem o catálogo.

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**NFRs covered**: `NFR-04`

**References**: ADR-008, ADR-004

**Objective**: Criar as tabelas do catálogo, a consulta encadeada marca → modelo → ano → versão, os endpoints e a ferramenta de exportação, que lê a origem com Dapper, gera o script de carga e, só em bancos de desenvolvimento e de teste, pode carregar o catálogo em lote.

**Files to modify**:
- `src/GazetaMarketplace.Core/VehicleCatalog/IVehicleCatalog.cs`
- src/GazetaMarketplace.Infrastructure/CatalogoVeiculos/VehicleCatalog.cs (cache de memória, 10 min)
- `src/GazetaMarketplace.Web/Controllers/Api/VehicleCatalogController.cs`
- tools/VehicleCatalogExport/ (console .NET, fora da solução do site)
- db/seed/vehicle-catalog.sql (gerado, versionado)
- tools/VehicleCatalogExport/OrigemGazetaOnline.cs (leitura de origem em Dapper, junções de 4 níveis)
- tools/VehicleCatalogExport/CargaEmLote.cs (Dapper, só em desenvolvimento e teste)
- tools/VehicleCatalogExport/OrigemGazetaOnline.cs (leitura de origem em Dapper, junções de 4 níveis)
- tools/VehicleCatalogExport/CargaEmLote.cs (Dapper, só em desenvolvimento e teste)

**Acceptance Criteria**:
- [x] Endpoints `listVehicleBrands`, `listVehicleModels`, `listVehicleModelYears` e `listVehicleVersions` conforme `architecture/api/openapi.yaml`, públicos e com resposta em ordem definida
- [x] Cada registro tem `Source` (origem); a troca de fonte não muda código de tela nem de regra
- [x] A ferramenta para sem gerar nada se faltar a variável de conexão ou se a conexão falhar; descarta órfãos e lista-os num relatório; o script gerado é idempotente (`MERGE`)
- [x] Nenhuma credencial do GazetaOnline existe no repositório (a antiga não é usada)
- [x] A leitura da origem usa Dapper e nunca escreve na origem; a carga em lote **recusa** rodar contra o banco de produção (cadeia de conexão marcada como produção) e traz o comentário `// Dapper: carga em lote; o EF Core geraria um INSERT por linha`
- [x] A carga em lote é idempotente (`MERGE`) e deixa a mesma contagem que o script gerado
- [x] A leitura da origem usa Dapper e nunca escreve na origem; a carga em lote **recusa** rodar contra o banco de produção (cadeia de conexão marcada como produção) e traz o comentário `// Dapper: carga em lote; o EF Core geraria um INSERT por linha`
- [x] A carga em lote é idempotente (`MERGE`) e deixa a mesma contagem que o script gerado

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Catalog/EndpointsTests.Marcas_PorTipo_EmOrdemAlfabetica`
- `tests/GazetaMarketplace.Web.Tests/Catalog/EndpointsTests.Modelos_DeMarcaInexistente_Devolve404`
- `tests/GazetaMarketplace.Web.Tests/Catalog/EndpointsTests.Anos_EmOrdemDecrescente`
- `tests/GazetaMarketplace.Web.Tests/Catalog/SourceTests.TrocarFonte_NaoMudaAConsulta`
- `tests/VehicleCatalogExport.Tests/ExportTests.SemVariavelDeConexao_ParaSemGerarArquivo`
- `tests/VehicleCatalogExport.Tests/ExportTests.Orfaos_SaoDescartadosERelatados`
- `tests/VehicleCatalogExport.Tests/ScriptTests.Script_E_Idempotente`
- `tests/VehicleCatalogExport.Tests/CargaEmLoteTests.Lote_CarregaCatalogoEmBancoDeTeste_EConfereContagem (TestContainers, roda no /test)`
- `tests/VehicleCatalogExport.Tests/BatchLoadTests.RecusaBancoMarcadoComoProducao`
- `tests/VehicleCatalogExport.Tests/CargaEmLoteTests.RodarDuasVezes_NaoDuplica (TestContainers, roda no /test)`
- `tests/VehicleCatalogExport.Tests/CargaEmLoteTests.Lote_CarregaCatalogoEmBancoDeTeste_EConfereContagem (TestContainers, roda no /test)`
- `tests/VehicleCatalogExport.Tests/BatchLoadTests.RecusaBancoMarcadoComoProducao`
- `tests/VehicleCatalogExport.Tests/CargaEmLoteTests.RodarDuasVezes_NaoDuplica (TestContainers, roda no /test)`

**Implementado em 2026-10-03 (decisões do Product Owner)**:
- **Chave composta (Id, Kind) em todas as tabelas.** `CarBrands` e `MotorcycleBrands` são tabelas separadas no GazetaOnline e cada uma numera a partir de 1, então a Honda de carros e a Honda de motos podem ter o mesmo id. O esquema da origem **nunca foi lido** (não há esquema no repositório), então a unicidade global dos ids não pôde ser confirmada e a chave composta ficou. `VehicleBrands` e `VehicleModels` têm chave (Id, Kind); `VehicleModelYears`, (ModelId, Year, Kind); `VehicleVersions`, (Id, Kind); cada chave estrangeira também leva o tipo.
- **Ano em tabela própria** (`VehicleModelYears`), com as versões ligadas por chave estrangeira composta: existem anos sem versões.
- **Consequência no contrato:** os três endpoints filhos passaram a exigir `kind` (`car` ou `moto`) além do id, porque o id sozinho não diz de qual tipo é. `openapi.yaml` atualizado (parâmetro `VehicleKind` e resposta 400).
- Marca existente sem modelos, modelo sem anos e ano sem versões devolvem **200 com lista vazia**; item que não existe devolve **404**; `kind` ausente ou inválido e ano fora de 1950 a 2100 devolvem **400**.
- Cache de 10 minutos por consulta (só o que existe entra no cache); respostas HTTP com `Cache-Control: public, max-age=600`.
- Ferramenta `tools/VehicleCatalogExport` (fora da solução; testada por `tests/VehicleCatalogExport.Tests`, que está na solução): `export` gera o script `MERGE` idempotente e `load` carrega em lote, só em Development ou Testing, rodando os mesmos `MERGE` numa transação.
- **Catálogo reduzido de teste:** `tests/VehicleCatalogExport.Tests/Data/sample-catalog.sql` simula a origem (10 marcas, 45 modelos, 214 anos, 307 versões) com ids que colidem entre carros e motos e os casos de borda; `db/seed/sample/vehicle-catalog-sample.sql` é o script gerado dele (`Source = 'sample'`) e um teste confere que está em dia. **A exportação real fica para o lançamento**, quando a A5 e o acesso somente leitura estiverem resolvidos.

**Dependencies**: 2.1, 0.6

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Rodar a ferramenta contra um banco de teste reduzido e aplicar o script duas vezes. A exportação real depende da A5 e de acesso somente leitura (ver Risk register).

**Estimate**: L

### Task 2.6: Gerenciar categorias

**User stories**: US-013

**Scenarios covered**: `@US-013-S01`, `@US-013-S02`, `@US-013-S03`, `@US-013-S04`, `@US-013-S05`, `@US-013-S06`, `@US-013-S07`, `@US-013-S08`, `@US-013-S09`, `@US-013-S10`, `@US-013-S11`

**NFRs covered**: `NFR-13`

**References**: ADR-002

**Objective**: Tela de categorias do Administrador: criar até o terceiro nível, renomear, ordenar e excluir com as regras da A7.

**Files to modify**:
- `src/GazetaMarketplace.Web/Areas/Panel/Controllers/CategoriesController.cs`
- `src/GazetaMarketplace.Web/Areas/Panel/Views/Categories/*.cshtml`
- `src/GazetaMarketplace.Core/Categories/` (`ICategoryManagement`, `ICategoryUsage`, `CategoryRules.IsProtectedFromDeletion`) e `src/GazetaMarketplace.Infrastructure/Categories/` (`CategoryManagement`, `PendingAdsCategoryUsage`)
- `src/GazetaMarketplace.Web/wwwroot/js/pages/categories-index.js`

**Acceptance Criteria**:
- [x] `@US-013-S01` (@happy): Criar uma subcategoria — o *Then* do SPEC é atendido
- [x] `@US-013-S02` (@happy): Criar uma categoria principal — o *Then* do SPEC é atendido
- [x] `@US-013-S03` (@happy): Renomear uma categoria — o *Then* do SPEC é atendido
- [x] `@US-013-S04` (@happy): Mudar a ordem das categorias — o *Then* do SPEC é atendido
- [x] `@US-013-S05` (@happy): Excluir uma categoria vazia — o *Then* do SPEC é atendido
- [x] `@US-013-S06` (@negative): Nome de categoria repetido — o *Then* do SPEC é atendido
- [x] `@US-013-S07` (@negative): Nome de categoria vazio — o *Then* do SPEC é atendido
- [x] `@US-013-S08` (@negative): Excluir categoria que tem anúncios — o *Then* do SPEC é atendido
- [x] `@US-013-S09` (@negative): Excluir categoria que tem subcategorias — o *Then* do SPEC é atendido
- [x] `@US-013-S10` (@negative): Categorias com características específicas não podem ser excluídas — o *Then* do SPEC é atendido
- [x] `@US-013-S11` (@edge): Categorias têm até três níveis — o *Then* do SPEC é atendido
- [x] Criar categoria principal, subcategoria e terceiro nível; quarto nível é impedido pela lista "Categoria pai"
- [x] Só se exclui categoria sem subcategorias e sem anúncios; o bloqueio por campos específicos vale só para categorias da carga inicial que definem o próprio grupo (A7 b)
- [x] Renomear mantém os anúncios; reordenar só entre irmãs e vale no site
- [x] Nome único por grupo; nome vazio recusado; apenas Administrador acessa
- [x] **RC-16:** criar, renomear, reordenar e excluir categoria gravam em `AuditEntries`

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Categories/CategoriesTests.US013S01_CriarUmaSubcategoria` — `@US-013-S01`
- `tests/GazetaMarketplace.Web.Tests/Categories/CategoriesTests.US013S02_CriarUmaCategoriaPrincipal` — `@US-013-S02`
- `tests/GazetaMarketplace.Web.Tests/Categories/CategoriesTests.US013S03_RenomearUmaCategoria` — `@US-013-S03`
- `tests/GazetaMarketplace.Web.Tests/Categories/CategoriesTests.US013S04_MudarAOrdemDasCategorias` — `@US-013-S04`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Categories/CategoriesE2ETests.US013S04_MudarAOrdemDasCategorias` — `@US-013-S04` (E2E, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Categories/CategoriesTests.US013S05_ExcluirUmaCategoriaVazia` — `@US-013-S05`
- `tests/GazetaMarketplace.Web.Tests/Categories/CategoriesTests.US013S06_NomeDeCategoriaRepetido` — `@US-013-S06`
- `tests/GazetaMarketplace.Web.Tests/Categories/CategoriesTests.US013S07_NomeDeCategoriaVazio` — `@US-013-S07`
- `tests/GazetaMarketplace.Web.Tests/Categories/CategoriesTests.US013S08_ExcluirCategoriaQueTemAnuncios` — `@US-013-S08`
- `tests/GazetaMarketplace.Web.Tests/Categories/CategoriesTests.US013S09_ExcluirCategoriaQueTemSubcategorias` — `@US-013-S09`
- `tests/GazetaMarketplace.Web.Tests/Categories/CategoriesTests.US013S10_CategoriasComCaracteristicasEspecificasNaoPodemSerExclui` — `@US-013-S10`
- `tests/GazetaMarketplace.Web.Tests/Categories/CategoriesTests.US013S11_CategoriasTemAteTresNiveis` — `@US-013-S11`
- `tests/GazetaMarketplace.Web.Tests/Categories/ExclusaoTests.BloqueioPorCamposEspecificos_SoParaCategoriasDaCargaComGrupoProprio`
- `tests/GazetaMarketplace.Web.Tests/Categories/OrdemTests.Reordenar_SoEntreIrmas`
- `tests/GazetaMarketplace.Web.Tests/Categories/CategoriesTests.CriarRenomearReordenarExcluir_GravamAuditoria`

**Implementado em 2026-10-03 (decisões do Product Owner)**:
- **Anúncios por categoria (`ICategoryUsage`).** A tabela `Ads` só nasce na 3.1; até lá a implementação provisória devolve zero. A 3.1 troca por uma consulta em `Ads` e o teste S08 passa a rodar contra anúncios de verdade (hoje usa um duplo que devolve 3).
- **Subcategoria dentro de uma folha:** permitida só se a folha não tiver anúncios; ela deixa de aceitar anúncio (`IsPostable = 0`, auditado em `category.change_postable`). Com anúncios, recusa com "Mova antes os anúncios desta categoria" (mensagem que não está na SPEC).
- **Ordem dos bloqueios da exclusão:** campos específicos, depois subcategorias, depois anúncios. O bloqueio por campos específicos aparece **ao clicar em "Excluir"**, sem janela de confirmação (S10 não tem passo de confirmação); S08 e S09 mostram a mensagem depois de confirmar.
- **Renomear mantém o slug**, para as URLs públicas não quebrarem.
- **Mover por botões em formulário comum** (POST/redirecionar/GET), com `role="status"` ("X agora está antes de Y") e âncora no item; com JavaScript o foco volta ao botão usado. Empates na ordem da carga viram posições de 10 em 10 antes da troca.
- Toda operação lê o estado atual do **banco** (nunca o cache de 10 minutos), roda em transação `Serializable` e esvazia o cache da árvore ao terminar. Auditoria (RC-16) em `category.create`, `category.rename`, `category.move`, `category.delete` e `category.change_postable`, com anterior e novo, na mesma transação.
- A parte "o visitante vê" dos cenários S01 a S05 (página inicial, página de categoria) chega com as telas públicas (fase 5); aqui fica provado que a **árvore em cache** que elas vão ler já reflete cada mudança na hora.
- **Ponto da SPEC a corrigir:** o cenário S08 usa "Motos" como categoria com anúncios, mas Motos (id 36) é da carga e define o próprio grupo de campos; pela A7 b e pela ordem de bloqueios decidida, ela mostra a mensagem de campos específicos, não a de anúncios. Os testes de S08 usam uma categoria sem grupo próprio.

**Dependencies**: 2.1, 2.2, 1.1

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Criar, renomear e excluir no navegador.

**Estimate**: L

### Task 2.7: Telefone/WhatsApp do site

**User stories**: US-015

**Scenarios covered**: `@US-015-S01`, `@US-015-S02`, `@US-015-S03`, `@US-015-S04`, `@US-015-S05`, `@US-015-S06`

**NFRs covered**: `NFR-13`

**References**: —

**Objective**: Tela de configuração do número único usado em todos os anúncios.

**Files to modify**:
- `src/GazetaMarketplace.Web/Areas/Panel/Controllers/SettingsController.cs`
- `src/GazetaMarketplace.Web/Areas/Panel/Views/Settings/Index.cshtml`
- `src/GazetaMarketplace.Core/Settings/` (`SiteSetting`, `PhoneNumber`, `ISiteSettings`, `ISiteSettingsManagement`) e `src/GazetaMarketplace.Infrastructure/Settings/` (`SiteSettingsStore`, `SiteSettingsManagement`)

**Acceptance Criteria**:
- [x] `@US-015-S01` (@happy): Definir o telefone/WhatsApp do site — o *Then* do SPEC é atendido
- [x] `@US-015-S02` (@happy): Trocar o número em uso — o *Then* do SPEC é atendido
- [x] `@US-015-S03` (@edge): Número digitado sem formatação — o *Then* do SPEC é atendido
- [x] `@US-015-S04` (@negative): Número inválido — o *Then* do SPEC é atendido
- [x] `@US-015-S05` (@negative): Número vazio — o *Then* do SPEC é atendido
- [x] `@US-015-S06` (@negative): Redator não acessa as configurações — o *Then* do SPEC é atendido
- [x] Aceita número com ou sem formatação e guarda só dígitos; recusa número inválido e vazio
- [x] Trocar o número vale para todos os anúncios na hora; só o Administrador acessa
- [x] **RC-16:** trocar o telefone do site grava em `AuditEntries` o valor anterior e o novo

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Configurações/SettingsTests.US015S01_DefinirOTelefoneWhatsAppDoSite` — `@US-015-S01`
- `tests/GazetaMarketplace.Web.Tests/Configurações/SettingsTests.US015S02_TrocarONumeroEmUso` — `@US-015-S02`
- `tests/GazetaMarketplace.Web.Tests/Configurações/SettingsTests.US015S03_NumeroDigitadoSemFormatacao` — `@US-015-S03`
- `tests/GazetaMarketplace.Web.Tests/Configurações/SettingsTests.US015S04_NumeroInvalido` — `@US-015-S04`
- `tests/GazetaMarketplace.Web.Tests/Configurações/SettingsTests.US015S05_NumeroVazio` — `@US-015-S05`
- `tests/GazetaMarketplace.Web.Tests/Configurações/SettingsTests.US015S06_RedatorNaoAcessaAsConfiguracoes` — `@US-015-S06`
- `tests/GazetaMarketplace.Web.Tests/Settings/NormalizacaoTests.Numero_SemFormatacao_E_Normalizado`
- `tests/GazetaMarketplace.Web.Tests/Settings/SettingsTests.TrocaDeTelefone_GravaValorAntigoENovo`

**Implementado em 2026-10-03 (decisões do Product Owner)**:
- Tabela chave-valor `SiteSettings` (`Key varchar(100)` único, `Value nvarchar(500)`), migration `AddSiteSettings`; chave `site.phone`, valor só com dígitos e sem o código do país. A tabela nasce vazia: o Administrador precisa informar o número.
- Rigor total no telefone: DDD da lista oficial (67), celular com 11 dígitos começando em 9, fixo com 10 dígitos começando de 2 a 9. Aceita `(11) 91234-5678`, `11 91234-5678`, `11912345678`, `+55 (11) 91234-5678` e `5511912345678`.
- Cache de 10 minutos (relógio do site), invalidado ao salvar. O cache é por processo; ver `plans/BACKLOG.md`.
- A mudança grava o valor anterior e o novo em `AuditEntries` na mesma transação; reenviar o mesmo número não grava nem audita.
- Duas pessoas criando a primeira configuração ao mesmo tempo: o índice único recusa uma e ela recebe o aviso de conflito.
- Tela `/painel/configuracoes`, só do Administrador, pronta para receber outras chaves. Funciona sem JavaScript; o módulo `settings-index.js` só desabilita o botão ("Salvando…") durante o envio.
- A parte de S01/S02 que o visitante vê (número no anúncio, botões Ligar e WhatsApp) entra com o detalhe público do anúncio (5.x); aqui fica provado que o site lê o número novo na hora (`ISiteSettings`).

**Dependencies**: 1.1, 0.6

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Trocar o número e ver no detalhe de um anúncio.

**Estimate**: S

---
## Checkpoint 2 — Categorias e catálogo completos

**Verify before proceeding**:
- [ ] Árvore com 124 postáveis e ids reais
- [x] 18 grupos de campos implementados (2.4, com a PL-01 respondida)
- [ ] Catálogo consultável e ferramenta de exportação testada
- [ ] Telefone do site configurável

---

## Fase 3 — Anúncios (equipe)

### Task 3.1: Modelo do anúncio, situações e autorização por autoria

**User stories**: **Foundation** — Fundação: todas as histórias de anúncio e de vitrine leem e gravam esta entidade.

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**NFRs covered**: `NFR-04`, `NFR-13`, `NFR-19`

**References**: ADR-002, ADR-004, ADR-006

**Objective**: Criar a entidade do anúncio com atributos em JSON, colunas calculadas e indexadas, colunas de busca normalizadas e o serviço que controla situações e autoria.

**Files modified** (nomes em inglês, pelo glossário):
- `src/GazetaMarketplace.Core/Ads/` — `AdStatus`, `AdStatusRules`, `AdAccess`, `Ad`, `AdPhoto`, `AdAttributes`, `IAdService`, `AdMessages`
- `src/GazetaMarketplace.Core/Search/Normalizer.cs`
- `src/GazetaMarketplace.Infrastructure/Data/Configurations/AdConfiguration.cs, AdPhotoConfiguration.cs` (colunas calculadas, CHECKs e índices do §6.4)
- `src/GazetaMarketplace.Infrastructure/Ads/AdService.cs`, `Categories/AdsCategoryUsage.cs` (substitui `PendingAdsCategoryUsage`)
- `src/GazetaMarketplace.Infrastructure/Data/Migrations/` (`AddAds`) e `db/scripts/gazeta-idempotente.sql`
- `ICurrentUser.IsAdministrator`, `SqlFragments.PublishedStatus = AdStatus.Published`

**Acceptance Criteria**:
- [x] `Ads` com `PriceCents bigint NULL` (nulo = sem preço), `Attributes` JSON de objeto (`ISJSON` + chave `{`), colunas calculadas `VehicleBrandId`, `VehicleModelId`, `ModelYear`, `Km`, `AreaM2` persistidas, indexadas e com `TRY_CAST`; `TitleSearch`/`DescriptionSearch` preenchidos só pelo C#
- [x] Transições de situação: Rascunho → Em revisão → Publicado ou Rejeitado; Publicado → Rascunho (despublicar) ou Arquivado; Arquivado é definitivo; transição inválida devolve `CONFLICT` (as 9 passagens do Apêndice A)
- [x] O Redator **lê os próprios anúncios em qualquer situação** e **edita só em Rascunho ou Rejeitado**; o Administrador lê todos e edita os que não estão Arquivados (D2: o texto anterior, "só lê e altera Rascunho ou Rejeitado", contradizia a US-008-S12); a checagem fica no serviço, não só no controller
- [x] O modelo não tem campo de nome, telefone ou e-mail de vendedor ou comprador (NFR-19)
- [x] **Teste diferencial** (ADR-002): uma coluna calculada devolve exatamente o que o C# lê, em todos os grupos com filtro (`ComputedColumnsDifferentialTests`, SQL Server real)
- [x] `ICategoryUsage` passa a contar anúncios reais em qualquer situação; a US-013-S08 roda contra anúncios de verdade

**Tests added**: `Web.Tests/Ads/` (`StatusTests`, `AuthorshipTests`, `PrivacyTests`, `PriceTests`, `AdAttributesTests`, `AdEntityTests`, `AdServiceTests`, `AdsCategoryUsageTests`), `Web.Tests/Search/NormalizerTests`, `DeletionTests` e `CategoriesTests` com anúncios reais, `MigrationsTests.MigrationDosAnuncios…`; `IntegrationTests/` (`AdsSchemaTests`, `ComputedColumnsDifferentialTests` com a completude, `AdServiceConcurrencyTests`, `AdsCategoryUsageTests`, `ScriptTests` com 10 migrations).

**Decisões aprovadas pelo Product Owner (2026-10-03):**
- **D1** `CategoryId` aceita nulo no banco (US-008-S07); o envio para revisão exige categoria postável (ARCHITECTURE §6.2 atualizado).
- **D2** Redator lê o próprio em qualquer situação e edita só em Rascunho ou Rejeitado (a SPEC precisa de emenda; ver BACKLOG).
- **D3** `TRY_CAST` nas colunas calculadas (emenda do ADR-002).
- **D4** `TransitionAsync` genérico e protegido; reenviar um rejeitado limpa `Rejected*`; despublicar limpa `Published*`; motivo da rejeição de até 500 caracteres (suposição).
- **D5** `Attributes` como `string`; `AdPhotos.AdId` e todas as FKs `Restrict`; `CHECK LEN(Description) <= 6000`; `SizeBytes` é o da versão de 1600 px.

**Dependencies**: 2.1, 2.2, 2.3, 0.6

**Verification**: Done when every test under "Tests added" passes, plus manual check: Conferir o esquema gerado e os índices do §6.4 do ARCHITECTURE. **Feito em 2026-10-03.**

**Estimate**: L

### Task 3.2: Consulta de CEP no servidor, com cache e lista de municípios

**User stories**: **Foundation** — Sustenta a US-008-S14 e a NFR-24; os cenários ficam na 3.3.

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**NFRs covered**: `NFR-24`

**References**: ADR-007, architecture/api/openapi.yaml#getCep, #listCities

**Objective**: Endpoint de CEP para a equipe, cliente do ViaCEP com tempo limite, cache em tabela e lista de municípios do IBGE.

**Files modified** (nomes em inglês, pelo glossário):
- `src/GazetaMarketplace.Core/Location/` — `ICepLookup`, `ICepService`, `ICityDirectory`, `CepRules`, `BrazilianStates`, `CityNames`, `City`, `CepCacheEntry`; `Core/Configuration/ViaCepOptions.cs`
- `src/GazetaMarketplace.Infrastructure/Location/` — `ViaCepLookup`, `CepService`, `CityDirectory`; `CepCacheConfiguration`, `CityConfiguration`; migration `AddCepCacheAndCities`
- `src/GazetaMarketplace.Web/Controllers/Api/CepController.cs`, `CitiesController.cs`; `wwwroot/js/modules/cep.js`
- `tools/CitiesImport` (carregador idempotente) + `tests/CitiesImport.Tests`; `db/seed/sample/cities-sample.{json,sql}`
- `IdentityExtensions` (401/403 em JSON sob `/api`), `RateLimitingExtensions` (política `cep`), `Program.cs` (limitador depois da autenticação), `openapi.yaml` (`listCities`)

**Acceptance Criteria**:
- [x] `GET /api/v1/cep/{cep}` só para a equipe logada; CEP com menos de 8 dígitos não consulta nada (`VALIDATION_ERROR`)
- [x] Cada chamada faz 1 tentativa de até 5 s; sem resposta, 5xx ou sem rede devolve 503 `CEP_SERVICE_UNAVAILABLE`; o ViaCEP respondendo erro devolve 404 `NOT_FOUND` (não é falha)
- [x] CEP encontrado vale 30 dias em `CepCache` (sobrevive à reciclagem do IIS); CEP inexistente não entra no cache; entrada vencida com o ViaCEP fora do ar devolve 503 (ADR-007)
- [x] O nome da cidade é conferido com a lista do IBGE; `cep.js` repete a chamada uma vez em 503 ("tentativa 2 de 2")
- [x] `GET /api/v1/cities?uf=` (só equipe, cache privado de 10 minutos) alimenta o preenchimento manual; UF sem carga devolve lista vazia
- [x] Rua e bairro nunca são lidos, guardados nem devolvidos (NFR-19, S25)
- [x] 30 consultas de CEP por minuto por usuário; endpoints JSON respondem 401/403 em ProblemDetails, sem redirecionar

**Tests added**: `Web.Tests/Cep/` (`CepEndpointTests`, `ViaCepLookupTests`, `CacheAndStandardizationTests`, `CityNamesAndStatesTests`, `CitiesAndLimitsTests`), `MigrationsTests.MigrationDoCepEMunicipios…`; `CitiesImport.Tests` (27); `IntegrationTests/CepAndCitiesTests` (7); `Playwright/Location/CepE2ETests` (3, o `cep.js` real contra o endpoint real e um ViaCEP de mentira).

**Decisões aprovadas pelo Product Owner (2026-10-03):**
- **D1** Carga dos municípios: tabela e migration agora; carregador idempotente em `tools/CitiesImport`; amostra de 40 municípios; a carga real é feita depois com o JSON oficial do IBGE. Sem a lista, vale a regra da SPEC (campo de texto + padronização).
- **D2** `GET /api/v1/cities?uf=SP`, só equipe, cacheável 10 min (`listCities` no `openapi.yaml`); as 27 UFs em `BrazilianStates`.
- **D3** Só cidade, UF e código do IBGE.
- **D4** Política `cep`: 30 por minuto por usuário (a primeira por usuário do site; por isso o limitador passou a rodar depois da autenticação).
- **D5** ADR-007: entrada vencida não é usada; ViaCEP fora do ar → 503 e preenchimento manual.
- **D6** Artigos "de, da, do, das, dos, e" e o "d'" minúsculos fora da primeira palavra.

**Dependencies**: 0.3, 0.6, 1.1

**Verification**: Done when every test under "Tests added" passes, plus manual check: Consultar um CEP real com o cache vazio e de novo em seguida. **O teste manual com o ViaCEP real fica para o Product Owner ou o `/verify`** (o ambiente de desenvolvimento não alcança `viacep.com.br`).

**Estimate**: L

### Task 3.3: Criar e editar rascunho do anúncio

**User stories**: US-008

**Scenarios covered**: `@US-008-S01`, `@US-008-S07`, `@US-008-S08`, `@US-008-S09`, `@US-008-S10`, `@US-008-S11`, `@US-008-S12`, `@US-008-S13`, `@US-008-S14`

**NFRs covered**: `NFR-15`, `NFR-13`

**References**: ADR-002, ADR-004

**Objective**: Formulário do anúncio que muda conforme o grupo de campos, com preço em centavos, contadores, CEP e preenchimento manual.

**Decisões aprovadas (2026-10-04)**: D1 o campo de preço envia sempre notação brasileira e o servidor tem **um só leitor** (`PriceText`; número sem vírgula = reais), a máscara do navegador só formata ao digitar · D2 as sugestões de Marca e Tipo de produto ficam para a 3.3b (campos de texto simples aqui) · D3 o bloco de fotos só informa (envio é da 3.5) e o botão "Enviar para revisão" é da 3.7 · D4 cidade/UF do navegador nunca são aceitas sem conferência: UF existe, cidade está na lista da UF (ou é só padronizada se a UF não tem lista); sem cidade/UF o servidor consulta o CEP (CEP inexistente salva pendente; serviço fora do ar reabre no modo manual) · D5 sem JavaScript, "Atualizar campos" refaz o formulário sem salvar · D6 auditoria `ad.create` / `ad.update` só com os **nomes** dos campos alterados, categoria e preço em valor; título e descrição não entram · D7 a cadeia marca → modelo → ano → versão é conferida ao salvar (pode estar incompleta no rascunho) · D8 inteiros aceitam milhar com ponto ("45.000"), decimais aceitam vírgula ("450,75"); texto ou formato errado é recusado, nunca gravado · D9 trocar de categoria com mais fotos que o limite do grupo novo é recusado · D10 lista de categorias em `<optgroup>` por categoria principal, só as 124 postáveis.

**Files created or modified**:
- `src/GazetaMarketplace.Core/Ads/` — `IAdDraftService.cs` (`AdDraftInput`, `AdDraftResult`, `LocationOutcome`), `AdFormRules.cs`, `AdMessages.cs` (novas mensagens), `AdAttributes.cs` (`long` e `GetRaw`)
- `src/GazetaMarketplace.Core/Fields/FieldValueParser.cs` · `src/GazetaMarketplace.Core/Formatting/PriceText.cs`
- `src/GazetaMarketplace.Infrastructure/Ads/AdDraftService.cs` (+ registro em `ServiceCollectionExtensions`)
- `src/GazetaMarketplace.Web/Areas/Panel/Controllers/AdsController.cs` · `Models/AdViewModels.cs`, `Models/AdFormFactory.cs`
- `src/GazetaMarketplace.Web/Areas/Panel/Views/Ads/` — `Edit`, `Read`, `NoPermission`, `Index` e as parciais `_AdGroupRegion`, `_AdField`, `_AdLocation`
- `src/GazetaMarketplace.Web/wwwroot/js/pages/ad-edit.js` · `js/modules/price.js`, `counter.js`, `catalog-chain.js` · `css/pages/ads-edit.css` · `somente-sem-js` em `layout.css` e `sem-js.css`
- `_FieldErrors.cshtml` passou a pôr o `role="alert"` numa `div` (o `role` no `<ul>` deixava os `<li>` órfãos para o leitor de tela)

**Acceptance Criteria**:
- [x] `@US-008-S01` (@happy): Salvar um rascunho completo — o *Then* do SPEC é atendido
- [x] `@US-008-S07` (@edge): Salvar um rascunho só com o título — o *Then* do SPEC é atendido
- [x] `@US-008-S08` (@negative): Salvar sem título — o *Then* do SPEC é atendido
- [x] `@US-008-S09` (@edge): Campos mudam conforme a categoria — o *Then* do SPEC é atendido
- [x] `@US-008-S10` (@negative): Redator tenta editar anúncio de outro redator — o *Then* do SPEC é atendido
- [x] `@US-008-S11` (@edge): Corrigir um anúncio rejeitado — o *Then* do SPEC é atendido
- [x] `@US-008-S12` (@edge): Anúncio em revisão não pode ser editado pelo Redator — o *Then* do SPEC é atendido
- [x] `@US-008-S13` (@edge): Administrador corrige o preço de um anúncio publicado — o *Then* do SPEC é atendido
- [x] `@US-008-S14` (@negative): Serviço de CEP fora do ar — o *Then* do SPEC é atendido (a pendência "Informe a cidade" no envio é da 3.7)
- [x] O formulário se refaz ao trocar a categoria (campos, limites de título e descrição, limite de fotos), mantendo os campos comuns; o foco fica na lista "Categoria"
- [x] Preço com máscara estilo banco, até R$ 99.999.999,99, salvo em centavos; Serviços não têm o campo; Vagas mostram a ajuda de salário
- [x] Rascunho salva só com o título; sem título nada é criado
- [x] Com o CEP fora do ar (duas falhas), UF e cidade viram listas e o anúncio recebe o selo de conferência (`LocationManual`)
- [x] Redator não abre anúncio de outro; Administrador edita qualquer anúncio não arquivado; anúncio em revisão é somente leitura para o Redator
- [x] Textos digitados são gravados e exibidos como texto puro
- [x] **RC-14:** o formulário (`AdFormSubmission`) não tem `Status`, `AuthorId`, `PublishedAt` nem campo de decisão; o servidor copia só os campos permitidos
- [x] Texto em campo numérico é recusado em vez de gravado (BACKLOG da 3.1); o teto de preço validado é R$ 99.999.999,99

**Tests added** (nomes reais):
- `Web.Tests/Ads/DraftTests` — S01, S07, S08, S09 (fragmento e "Atualizar campos"), S10, S11, S12, S13, S14, XSS, `PostComSituacaoEAutorNoCorpo_NaoAlteraNada`, várias opções, tipo errado → recusa (nunca 500), CEP resolvido no servidor, CEP inexistente, cidade forjada
- `Web.Tests/Ads/AdDraftServiceTests` — limites por grupo, preço, texto em campo numérico, cadeia do catálogo, endereço (D4), troca de categoria e fotos, auditoria sem conteúdo, nada mudou = nada gravado
- `Web.Tests/Ads/FieldValueParserTests` · `Web.Tests/Formatting/PriceTextTests` (inclui a "passagem" da máscara) · `Web.Tests/Ads/FormRenderingTests` (todos os grupos × todos os tipos de campo: rótulo, nome, ids únicos, `aria-describedby` e erro) · `Web.Tests/Architecture/EditViewModelsTests`
- `IntegrationTests/DraftIntegrationTests` — colunas calculadas depois de salvar pelo formulário, duas telas ao mesmo tempo (6 rodadas), versão velha recusada, categoria apagada no meio, auditoria junto com o anúncio
- `Web.Tests.Playwright/Ads/DraftE2ETests` — S01 completo, S09 sem recarregar, máscara de preço, contadores, S08, S14 com ViaCEP de mentira, CEP inexistente, sem JavaScript, Axe (formulário, vagas, com erros, 320 px)

**Dependencies**: 3.1, 3.2, 2.5, 0.7

**Verification**: Done when every test under "Tests added" passes, plus manual check: Criar um anúncio de Carros, um de Serviços e um de Vagas no navegador.

**Estimate**: L

### Task 3.4: Processamento e armazenamento de fotos

**User stories**: **Foundation** — Fundação: a tela de fotos (3.5) e a galeria (3.9) usam este pipeline.

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**NFRs covered**: `NFR-12`, `NFR-05`

**References**: ADR-005 (nota de revisão de 2026-10-04), architecture/api/openapi.yaml#getPhotoFile

**Objective**: Validar pelo conteúdo, converter HEIC, gerar as duas versões WebP sem metadados, gravar fora da raiz e servir pelo controller.

**Decisões aprovadas (2026-10-04)**: pacote `Magick.NET-Q8-x64` 14.17.2 · `StorageKey` = base `<adId>/<guid>` e o código acrescenta `_1600.webp`/`_480.webp` (original em `_originals/<yyyy-MM>/<guid>.<ext>`) · política do ImageMagick só com JPEG, PNG, GIF, WebP, HEIC/HEIF e leitura forçada ao formato da assinatura · EXIF, sRGB, sem metadados, qualidade 80, GIF no primeiro quadro, nunca amplia · entrega com cache público só no publicado (`private, no-store` no resto), 404 igual, `_originals/` sem rota · limite próprio de 300/min por IP em vez de isentar `/fotos/` · HEIC de teste montado à mão (x265 + caixa HEIF) · arquivos órfãos para a 3.6.

**Files created or modified**:
- `src/GazetaMarketplace.Core/Photos/` — `PhotoFormat`, `PhotoSignature`, `PhotoLimits`, `PhotoMessages`, `IImageProcessor`, `IPhotoStorage`, `IPhotoIngestion`, `IPhotoDelivery`
- `src/GazetaMarketplace.Infrastructure/Photos/` — `MagickImageProcessor`, `MagickRuntime`, `policy.xml` (embutido), `FileSystemPhotoStorage`, `PhotoIngestion`, `PhotoDelivery`
- `src/GazetaMarketplace.Web/Controllers/PhotosController.cs` · `Security/RateLimitingExtensions.cs` (`PhotoPolicy`)
- `src/GazetaMarketplace.Core/Ads/AdAccess.cs` (`CanView` por autor) · `Directory.Packages.props`, `GazetaMarketplace.Infrastructure.csproj`

**Acceptance Criteria**:
- [x] A assinatura do arquivo (JPEG, PNG, GIF, WebP, HEIC/HEIF) decide o formato, nunca a extensão; arquivo falso com extensão `.jpg` é recusado
- [x] Cada foto vira `<StorageKey>_1600.webp` e `<StorageKey>_480.webp` (qualidade 80), orientação corrigida pelo EXIF e **todos os metadados removidos**; GIF animado vira o primeiro quadro
- [x] O original fica em `_originals/<yyyy-MM>/<guid>.<ext>`; **nenhuma rota serve `_originals/`**
- [x] Foto de anúncio publicado é pública com `Cache-Control: public, max-age=31536000, immutable`; de outra situação, só a equipe com acesso; resposta 404 igual para "não existe" e "não pode ver"
- [x] Falha no meio apaga os arquivos já gravados; HEIC que a biblioteca não lê devolve `VALIDATION_ERROR` com mensagem clara (AR-05)
- [x] **RC-2:** limites de recurso (50 milhões de pixels e 20.000 px de lado conferidos antes de decodificar, 512 MB, 30 s, 2 decodificações ao mesmo tempo) e política que deixa ligados só os cinco formatos
- [x] **RC-4:** o caminho de todo arquivo de foto usa só ids numéricos e GUIDs gerados, e o caminho final é conferido dentro de `PhotoStorage__BasePath`

**Tests added** (nomes reais, em `Web.Tests/Photos/` salvo indicação):
- `FormatoTests` (assinaturas, falsos `.jpg`, HEIC → WebP, original com a extensão detectada) · `MetadadosTests` (GPS, XMP e texto do EXIF fora das versões, dentro do original) · `VersoesTests` (1600/480, nunca amplia, qualidade 80 igual à codificação direta, orientação, GIF, alfa, envios simultâneos) · `FailureTests` (falha no meio, cortado em JPEG/PNG/GIF/WebP, 10 MB exatos, fluxo infinito, vazio, no máximo 2 decodificações) · `HeicTests` · `PhotosSecurityTests` (pixels, lado, 14 decodificadores desligados, política, hash da pasta de configuração, chaves e caminhos) · `EntregaTests` (cache, 404 igual, `_originals/`, saída da pasta, ids não numéricos, arquivo sumido, 300/min por IP e fora do global)
- `IntegrationTests/PhotoDeliveryTests` — entrega contra o SQL Server e o disco de verdade

**Dependencies**: 3.1, 0.2

**Verification**: Done when every test under "Tests added" passes. Verificação manual com um HEIC de iPhone de verdade (orientação, perfil de cor e GPS) fica pendente: o arquivo de teste é sintético (BACKLOG).

**Estimate**: L

### Task 3.5: Enviar, reordenar e remover fotos do anúncio

**User stories**: US-008

**Scenarios covered**: `@US-008-S02`, `@US-008-S03`, `@US-008-S04`, `@US-008-S05`, `@US-008-S06`

**NFRs covered**: `NFR-12`

**References**: ADR-005 (nota de revisão de 2026-10-04), architecture/api/openapi.yaml#uploadAdPhoto, #setAdPhotoCover, #deleteAdPhoto

**Objective**: Endpoints e tela de fotos: envio, limites por categoria, capa por botão, remoção e mensagens por arquivo.

**Decisões aprovadas (2026-10-04)**: D1 serializar por anúncio com `UPDLOCK` dentro de transação (sem migration) · D2 30 envios por minuto por usuário, contando cada arquivo · D3 remover apaga as duas versões WebP e deixa o original para a limpeza de 30 dias (3.6) · D4 lado maior limitado a 2560 px (`PhotoLimits.MaxLongSide`; 500 × 20000 vira 64 × 2560) · D5 fotos já gravadas continuam ao salvar o rascunho; "descartar rascunho com fotos" fica na 3.6.

**Files created or modified**:
- `src/GazetaMarketplace.Core/Photos/` — `IAdPhotoService` (e `AdPhotoItem`), `PhotoUrls`, `PhotoMessages` (limite e "sem fotos"), `PhotoLimits.MaxLongSide`
- `src/GazetaMarketplace.Infrastructure/Photos/AdPhotoService.cs` (envio, capa, remoção, listagem) · `MagickImageProcessor` (teto de 2560 px; liga o ImageMagick só na primeira foto)
- `src/GazetaMarketplace.Web/Controllers/Api/AdPhotosController.cs` (`uploadAdPhoto`, `setAdPhotoCover`, `deleteAdPhoto`) · `Areas/Panel/Controllers/AdPhotoPagesController.cs` (o mesmo, em formulários comuns, sem JavaScript)
- `src/GazetaMarketplace.Web/Security/RateLimitingExtensions.cs` (`PhotoUploadPolicy`, 30/min por usuário) · `Middleware/BodyLimitMiddleware.cs` (o limite da rota vale também para o 413)
- `src/GazetaMarketplace.Web/Areas/Panel/Views/Ads/_Photos.cshtml`, `Edit.cshtml`, `Read.cshtml`, `_AdGroupRegion.cshtml`, `AdPhotoPages/ConfirmRemoval.cshtml` · `Models/AdFormFactory.cs`, `AdViewModels.cs`
- `src/GazetaMarketplace.Web/wwwroot/js/modules/photos.js`, `modules/api.js` (aceita `FormData`), `pages/ad-edit.js`, `css/pages/ads-edit.css`

**Acceptance Criteria**:
- [x] `@US-008-S02` (@happy): Adicionar fotos ao anúncio — o *Then* do SPEC é atendido
- [x] `@US-008-S03` (@happy): Trocar a capa e remover uma foto — o *Then* do SPEC é atendido
- [x] `@US-008-S04` (@negative): Passar do limite de 20 fotos — o *Then* do SPEC é atendido
- [x] `@US-008-S05` (@negative): Enviar um arquivo que não é foto aceita — o *Then* do SPEC é atendido
- [x] `@US-008-S06` (@negative): Falha ao enviar uma foto — o *Then* do SPEC é atendido
- [x] Limite do servidor em 11 MB e da aplicação em 10 MB: arquivo de 10 a 11 MB recebe "A foto excede o limite de 10 MB"; acima de 11 MB, 413
- [x] Limite por grupo: 20 (padrão), 6 (Serviços), 0 (Vagas, sem envio); a foto além do limite recebe 409 "Cada anúncio pode ter no máximo N fotos"
- [x] Formato recusado devolve "Formato não aceito. Use JPG, PNG, WebP, GIF ou HEIC"; foto HEIC mostra o aviso de conversão
- [x] "Tornar capa" move a foto para a primeira posição; "Remover" pede confirmação; funciona sem arrastar
- [x] Falha de conexão marca só aquela foto com "Falha ao enviar" e "Tentar de novo"; as outras fotos e os textos seguem
- [x] Só o autor (ou Administrador) mexe nas fotos de um anúncio editável; antiforgery em todas as escritas
- [x] **RC-6:** no máximo 2 conversões de foto ao mesmo tempo no servidor (3.4) e no máximo 30 envios por minuto por usuário logado; o excesso devolve 429 `RATE_LIMITED`
- [x] **D4:** o lado maior de qualquer versão tem no máximo 2560 px, sem ampliar

**Tests added** (nomes reais):
- `Web.Tests/Photos/PhotosEndpointsTests` — S02 a S06 pela API, limite de 20/6/0, 10 a 11 MB e 413, arquivo vazio, autorização (outro Redator, em revisão, publicado, Administrador, inexistente), foto de outro anúncio, antiforgery e login, 429 no 31º, falha do registro sem arquivo sobrando, envios simultâneos
- `Web.Tests/Photos/PhotosPageTests` — a seção de fotos na página, o fluxo sem JavaScript (enviar, recusa, capa, confirmação de remoção), somente leitura, botões fora do formulário · `VersoesTests` (teto de 2560 px)
- `IntegrationTests/PhotoConcurrencyTests` — 8 envios simultâneos (posições únicas), limite de 20 sob corrida, prova do `UPDLOCK`, capa e remoção contra o SQL Server
- `Web.Tests.Playwright/Photos/PhotosE2ETests` — `US008S02` a `US008S06`, sem JavaScript, acessibilidade (axe) e 320 px

**Dependencies**: 3.4, 3.3

**Verification**: Done when every test under "Tests added" passes (unitários 1117, integração 99, E2E 55) e as 11 mutações são mortas.

**Estimate**: L

### Task 3.6: Limpeza diária dos originais de foto

**User stories**: **Foundation** — Decisão do Product Owner: original retido 30 dias (ADR-005).

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**References**: ADR-005 (nota de revisão de 2026-10-04), ADR-004

**Objective**: Serviço em segundo plano que apaga os originais com mais de 30 dias e os arquivos órfãos.

**Decisões aprovadas (2026-10-04)**: D5' sem "descartar rascunho" (o SPEC não tem exclusão de rascunho na v1) · D6 idade pela data de gravação do arquivo (`LastWriteTimeUtc`) · D7 carência de 24 horas para órfãos · D8 `IPhotoReprocessing` sem tela nem rota na v1 · D9 30 dias e 24 horas como constantes em `PhotoLimits`.

**Files created or modified**:
- `src/GazetaMarketplace.Infrastructure/Photos/OriginalsCleanupService.cs` (`BackgroundService`, `RunOnceAsync`, `PhotoCleanupResult`) · `PhotoReprocessing.cs`
- `src/GazetaMarketplace.Core/Photos/IPhotoStorageMaintenance.cs` (`StoredFile`, `StoredFileKind`), `IPhotoReprocessing.cs`, `PhotoLimits.cs` (`OriginalRetention`, `OrphanGrace`, `CleanupBatchSize`), `IPhotoStorage.cs` (`ReplaceVersionsAsync`, `ReadOriginalAsync`), `PhotoMessages.cs`
- `src/GazetaMarketplace.Infrastructure/Photos/FileSystemPhotoStorage.cs` (listar, apagar, pastas vazias) · `PhotoIngestion.cs` (limite de 2 conversões compartilhado) · `ServiceCollectionExtensions.cs`

**Acceptance Criteria**:
- [x] Roda 1 minuto depois da partida e a cada 24 horas enquanto o processo estiver vivo; apaga tudo o que passou de 30 dias (um atraso não perde nada)
- [x] Anula `AdPhotos.OriginalKey` do arquivo apagado e registra no log cada arquivo apagado e o total
- [x] Um reprocessamento sem original falha com "Original indisponível"; as versões WebP com registro nunca são apagadas
- [x] A anulação de `OriginalKey` é feita por lote de 500, com uma instrução (`ExecuteUpdateAsync` do EF Core); Dapper não foi necessário (ADR-004, ADR-005)
- [x] Arquivos órfãos em `<adId>/` (sem linha em `AdPhotos`) e `*.tmp` com mais de 24 horas são apagados; nada com registro, nada de `_magick/`, nada fora do formato do site
- [x] Sem `PhotoStorage:BasePath` (desenvolvimento) a rodada é pulada com aviso; falha de E/S em um arquivo não interrompe os outros

**Tests added** (nomes reais):
- `Web.Tests/Photos/CleanupTests` — 29 vs 30 dias, original de foto removida, `OriginalKey` anulada só do apagado, versões com registro nunca apagadas, órfão (24 h), `.tmp`, arquivos fora do formato e `_magick/`, falha de E/S, log, sem pasta, lotes de 500 (3 instruções para 1200 chaves), pastas vazias, partida e 24 h (relógio manual)
- `Web.Tests/Photos/ReprocessarTests` — sem original (chave nula e arquivo ausente), foto inexistente, regera versões e medidas, depois da limpeza de 30 dias
- `IntegrationTests/PhotoCleanupTests` — 1200 chaves em lotes no SQL Server real e a varredura completa

**Dependencies**: 3.4

**Verification**: Done when every test under "Tests added" passes (unitários 1136, integração 101, E2E 55) e as 9 mutações são mortas; verificação manual no site publicado: arquivos com data antiga apagados e registrados no log.

**Estimate**: S

### Task 3.7: Enviar anúncio para revisão

**User stories**: US-009

**Scenarios covered**: `@US-009-S01`, `@US-009-S02`, `@US-009-S03`, `@US-009-S04`, `@US-009-S05`

**NFRs covered**: `NFR-13`

**References**: ADR-004

**Objective**: Conferir as pendências do grupo de campos e mudar a situação para Em revisão sem duplicar o envio.

**Decisões aprovadas (2026-10-04)**: D1 página própria de confirmação (funciona com e sem JavaScript; diálogo pode vir depois) · D2 "Enviar para revisão" salva o formulário e depois confere as pendências · D3 cada campo obrigatório tem `RequiredMessage` (textos da SPEC como estão; os demais redigidos e revisados pelo Product Owner) · D4 o botão só aparece depois do primeiro "Salvar rascunho" · D5 clique duplo é idempotente ("Este anúncio já foi enviado para revisão"; a garantia vem do `RowVersion`).

**Files created or modified**:
- `src/GazetaMarketplace.Core/Ads/AdSubmission.cs` (`IAdSubmission`, `SubmitResult`, `SubmitOutcome`, `AdPending`) · `AdSubmissionRules.cs` (regra pura `Pending`) · `AdMessages.cs`
- `src/GazetaMarketplace.Infrastructure/Ads/AdSubmission.cs` (`CheckAsync`, `SubmitAsync`) · `ServiceCollectionExtensions.cs`
- `src/GazetaMarketplace.Core/Fields/FieldDefinition.cs` (`RequiredMessage`) e os 17 arquivos de `Fields/Groups/` (33 campos obrigatórios com frase própria)
- `src/GazetaMarketplace.Web/Areas/Panel/Controllers/AdsController.cs` (`SubmitForReview`, `ConfirmSubmit`, `Submit`), `Models/AdFormFactory.cs`, `Models/AdViewModels.cs`
- `Views/Ads/Edit.cshtml`, `_Pendings.cshtml`, `ConfirmSubmit.cshtml`, `Index.cshtml` (mensagem de sucesso provisória), `_Photos.cshtml` (âncora `#fotos`), `_ViewImports.cshtml`
- `wwwroot/js/pages/ad-edit.js` (foco na pendência, botão "Enviando…") · `ad-confirm.js`

**Acceptance Criteria**:
- [x] `@US-009-S01` (@happy): Enviar um rascunho completo para revisão — o *Then* do SPEC é atendido
- [x] `@US-009-S02` (@negative): Enviar um rascunho incompleto — o *Then* do SPEC é atendido
- [x] `@US-009-S03` (@negative): Faltam características obrigatórias da categoria — o *Then* do SPEC é atendido
- [x] `@US-009-S04` (@edge): Reenviar um anúncio rejeitado depois de corrigi-lo — o *Then* do SPEC é atendido (no banco e na página do anúncio; a tela de rejeição é da 4.1)
- [x] `@US-009-S05` (@edge): Clicar duas vezes em enviar — o *Then* do SPEC é atendido
- [x] Pendências na ordem título, categoria, descrição, preço (exceto Serviços), CEP, ao menos 1 foto (exceto Vagas) e campos obrigatórios do grupo, cada uma com link para o campo
- [x] O botão de envio nunca fica desabilitado por pendência: o clique mostra a lista; a situação só muda quando está tudo certo
- [x] Reenviar um anúncio rejeitado leva a Em revisão, limpa o motivo e grava `SentAt`; clique duplo gera uma única passagem e uma única auditoria `ad.submit`

**Tests added** (nomes reais):
- `Web.Tests/Ads/SubmitForReviewTests` — 12 testes: `US009S01` a `US009S05`, clique duplo em paralelo, formulário salvo antes da conferência, formulário inválido, botão (aparece após o primeiro salvar, não na leitura), reconferência no servidor, acesso, Administrador
- `Web.Tests/Ads/PendingTests` — regras puras: rascunho completo, Vagas sem foto e Serviços sem preço, Carros, Terrenos e Apartamentos, informações adicionais, preço vazio (zero nem chega ao banco), CEP, sem categoria, ordem da lista e da tela, as 41 frases de `RequiredMessage` (artigo certo, nenhum campo de fora), fallback pelo rótulo
- `IntegrationTests/SubmitForReviewConcurrencyTests` — 4 confirmações simultâneas × 3 rodadas no SQL Server real: uma passagem, uma auditoria, nenhum erro
- `Web.Tests.Playwright/Ads/SubmitForReviewE2ETests` — S01, S02/S03, S05, fluxo sem JavaScript, axe e 320 px (S04 não tem E2E: rejeitar só existe pela fila da 4.1)

**Dependencies**: 3.3, 3.5

**Verification**: Done when every test under "Tests added" passes (unitários 1163, integração 102, E2E 60) e as 8 mutações são mortas.

**Estimate**: M

### Task 3.8: Componentes de apresentação do anúncio: card, valor e bloco sem foto

**User stories**: **Foundation** — Fundação: o card e o corpo do anúncio aparecem no site e no painel.

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**NFRs covered**: `NFR-03`, `NFR-05`, `NFR-01`

**References**: `architecture/design-system.md` §5.2 a §5.4

**Objective**: Criar os componentes compartilhados de card (3 variantes), valor (Preço, Salário ou Tipo) e bloco "Vaga de emprego", usados no site e na pré-visualização do painel.

**Decisões aprovadas (2026-10-04)**: D1 página `/painel/componentes` só em Development e só para o Administrador (404 em Production, antes do login; sem menu nem rota pública) · D2 foto que falha vira o mesmo bloco neutro por módulo ES (`error` na captura, `createElement`/`textContent`, sem `innerHTML`); sem JavaScript fica o `alt` vazio e o título ao lado · D3 o nome acessível leva só cidade/UF (o selo "Cidade/UF manual" é só da pré-visualização da 4.1, nunca do card) · D4 `_AdBody` é só o corpo textual (a galeria é da US-003). **Mudança de dados junto**: a Área (m²) passa a ser obrigatória em Terrenos (categoria 30) e continua opcional em Apartamentos, Casas e Comércio (SPEC v1.3).

**Files created or modified**:
- `src/GazetaMarketplace.Core/Ads/AdCardModel.cs` (`AdCardModel`, `AdCardCover`) · `AdPresentation.cs` (`AdValue`, `AdValueKind`, `FormatMoney`, `ValueOf`, `AccessibleName`, `Location`, `CoverUrl`, `ThumbSize`, `HasPhotos`)
- `src/GazetaMarketplace.Web/ViewComponents/AdCardViewComponent.cs` · `Models/AdPresentationModels.cs` · `Views/Shared/Components/AdCard/Default.cshtml` · `Views/Shared/_AdValue.cshtml` · `_AdMediaPlaceholder.cshtml` · `_AdBody.cshtml`
- `wwwroot/css/components/card.css` (ligado nos dois layouts) · `wwwroot/js/modules/ad-card.js` (carregado por `layout.js`) · `wwwroot/images/componentes/capa-exemplo.svg`
- `Areas/Panel/Controllers/ComponentsController.cs` · `DevelopmentOnlyAttribute.cs` · `Models/ComponentsViewModel.cs` · `Views/Components/Index.cshtml`
- Terrenos: `Core/Fields/FieldDefinition.cs` (`RequiredForCategories`, `IsRequiredFor`) · `FieldGroup.cs` · `Groups/RealEstateGroup.cs` (`areaM2`, "Informe a área") · `Areas/Panel/Models/AdFormFactory.cs` (asterisco por categoria) · `specs/SPEC.md` (v1.3)

**Acceptance Criteria**:
- [x] Card padrão com capa de 480 px, `width`/`height`, `loading="lazy"` fora da primeira linha (`eager` na primeira) e proporção 4:3 reservada
- [x] Serviços mostram o Tipo no lugar do preço; Vagas mostram bloco neutro "Vaga de emprego" com a área da vaga e "Salário R$ …" (A4 e A6, `design-system.md` §5.2 e §5.3)
- [x] Nome acessível do link do card: "título, valor, cidade/UF" (com "Salário" ou "Tipo:" nas variantes); a parte que falta some sem vírgula sobrando
- [x] Valor em reais sem centavos quando são zero ("R$ 62.000", "R$ 2.499,90", S29); nunca "R$ 0"
- [x] Terrenos exigem a área no envio; Apartamentos, Casas e Comércio não

**Tests added** (nomes reais):
- `Web.Tests/Components/AdPresentationTests` — centavos (8 casos), valor por grupo, Serviços sem preço, sem preço/sem tipo, só Vagas sem foto, localização, nome acessível (com e sem partes), miniatura (endereço, tamanho, nunca amplia, teto do lado maior)
- `Web.Tests/Components/CardTests` — padrão (capa, dimensões, lazy/eager, um só link), Serviços, Vagas (sem `<img>`, bloco, salário), sem capa, sem cidade/sem preço, centavos, texto codificado, sem `style` nem `on…=` em linha, corpo (nível do título, codificação), CSS e layouts, módulo da foto que falha, página de componentes (Administrador vê, Production 404 para todos, Redator e anônimo não entram, sem menu nem rota)
- `Web.Tests/Ads/PendingTests` e `Fields/GroupsTests` — Terrenos exigem a área, as outras categorias não; `Ads/FormRenderingTests.Area_TemAsteriscoDeObrigatorioSoEmTerrenos`
- `Web.Tests.Playwright/Components/ComponentsE2ETests` — os 4 cards lado a lado (mesma altura de mídia, 4:3, nomes acessíveis), foto que falha, foco no card inteiro e hover, axe e rolagem em 320/768/1024/1280 px (2 e 4 colunas), corpo; `ComponentsProductionE2ETests` — 404 em Production

**Dependencies**: 0.7, 3.1, 2.2

**Verification**: Done when every test under "Tests added" passes (unitários 1210, integração 102, E2E 66) e as 11 mutações são mortas; verificação manual: ver os três cards lado a lado em `/painel/componentes` (Development).

**Estimate**: M

---
## Checkpoint 3 — Anúncios completos

**Verify before proceeding** (verificado em 2026-10-05; evidências em `reports/TEST_REPORT.md` §Checkpoint 3):
- [x] Rascunho com fotos e envio para revisão funcionam para Carros, Serviços e Vagas (`Checkpoint3E2ETests`, no site publicado)
- [x] Fotos: HEIC convertido, GPS removido, `_originals/` sem rota (`Checkpoint3E2ETests.Fotos_JpegComGpsEHeic…`, nos bytes entregues pelo site publicado)
- [x] Teste diferencial das colunas calculadas escrito e passando (`ComputedColumnsDifferentialTests`, SQL Server real, 4 testes)

---

## Fase 4 — Revisão e ciclo de vida

### Task 4.1: Fila de revisão e pré-visualização

**User stories**: US-010

**Scenarios covered**: `@US-010-S01`, `@US-010-S02` (parcial: só "Editar"; os outros botões chegam na 4.2 e na 4.3), `@US-010-S06`, `@US-010-S09`

**NFRs covered**: `NFR-13`

**References**: ADR-004

**Objective**: Fila de anúncios Em revisão (do mais antigo ao mais novo) e pré-visualização com a mesma aparência do site.

**Decisões aprovadas (2026-10-05)**: D1 a pré-visualização mostra só "Editar" (botão sem ação é pior que botão ausente; a S02 passa a conferir os três botões na 4.2) · D2 corpo completo agora (características reais, capa grande, miniaturas simples sem JavaScript, bloco "Vaga de emprego", contato "Fale com a Gazeta"); a 5.2 troca só as fotos pela galeria · D3 consulta em EF `AsNoTracking` (filtro e ordem fixos; o Dapper fica para a 4.4 e a busca) · D4 fila sem paginação · D5 aba "Todos os anúncios" aponta para a lista provisória até a 4.4 · D6 data de envio em dd/mm/aaaa no fuso de São Paulo, hora em `title` e `<time datetime>`.

**Files created or modified**:
- `src/GazetaMarketplace.Core/Ads/IReviewQueue.cs` (`IReviewQueue`, `ReviewQueueItem`, `IAdSpecsReader`) · `AdSpec.cs` · `AdSpecs.cs` (regra pura das características)
- `src/GazetaMarketplace.Infrastructure/Ads/ReviewQueue.cs` (EF: `Status == InReview`, `ORDER BY` data de envio e id) · `AdSpecsReader.cs` (nomes do catálogo de veículos) · `ServiceCollectionExtensions.cs`
- `src/GazetaMarketplace.Web/Areas/Panel/Controllers/ReviewQueueController.cs` (`/painel/anuncios/fila` e `/painel/anuncios/{id}/pre-visualizacao`, só Administrador) · `Models/ReviewViewModels.cs`
- `Views/ReviewQueue/Index.cshtml`, `Preview.cshtml`, `_Tabs.cshtml`, `LoadError.cshtml` · `Views/Shared/_AdPhotos.cshtml` · `wwwroot/css/pages/review-queue.css`
- `Views/Shared/_AdBody.cshtml` e `Models/AdPresentationModels.cs` (`AdSpec` foi para o Core) · `AdsController.cs` (sai a ação provisória) · removida `Views/Ads/ReviewQueue.cshtml`
- O item "Anúncios" do menu já ficava ativo em `/painel/anuncios/fila` (o menu compara o início do caminho), então `PanelMenu` não mudou.

**Acceptance Criteria**:
- [x] `@US-010-S01` (@happy): Ver a fila de revisão — o *Then* do SPEC é atendido
- [x] `@US-010-S02` (@happy): Pré-visualizar um anúncio antes de decidir — atendido **em parte**: faixa, corpo igual ao público e "Editar"; "Publicar" e "Rejeitar" chegam na 4.2
- [x] `@US-010-S06` (@edge): Fila de revisão vazia — o *Then* do SPEC é atendido
- [x] `@US-010-S09` (@negative): Redator não pode revisar anúncios — o *Then* do SPEC é atendido
- [x] A fila lista só Em revisão, do mais antigo ao mais novo (desempate pelo id; o reenvio vale pela data do último envio), com total; vazia mostra mensagem; falha de leitura mostra erro com "Tentar novamente" (503, sem detalhe técnico)
- [x] A pré-visualização mostra a faixa "Pré-visualização — ainda não publicado", o selo de Cidade/UF manual (informativo), o corpo com características reais, fotos, contato e "Editar"; anúncio fora de revisão só informa a situação, sem botões
- [x] Redator não acessa a fila nem a pré-visualização ("acesso negado"); sem login vai para a entrada

**Tests added** (nomes reais):
- `Web.Tests/Review/ReviewQueueTests` — 15 testes: `US010S01`, `US010S06`, `US010S09`, `US010S02`, desempate e reenvio, sem login, texto codificado e menu ativo, falha ao carregar, selo manual, sem telefone do site, Vagas, Serviços, fora de revisão, anúncio inexistente
- `Web.Tests/Review/AdSpecsTests` — 9 testes: nomes do catálogo, milhar e unidades, dinheiro, decimais, múltipla escolha, campo vazio ou de outra categoria, ano 1950, tipo errado no JSON
- `IntegrationTests/ReviewQueueSqlTests` — a consulta no SQL Server real (filtro, ordem, desempate, junção com autor e categoria) e a página
- `Web.Tests.Playwright/Ads/ReviewQueueE2ETests` — 3 testes: Administrador (fila → pré-visualização com foto carregada → Editar → voltar), axe e rolagem em 1280/1024/768/320 px com o empilhamento em 320, Redator com "acesso negado"

**Dependencies**: 3.7, 3.8, 1.1

**Verification**: Done when every test under "Tests added" passes (unitários 1234, integração 103, E2E 74) e as mutações são mortas; verificação manual: capturas da fila e da pré-visualização em 1280 e 360 px.

**Estimate**: M

### Task 4.2: Publicar e rejeitar anúncios

**User stories**: US-010

**Scenarios covered**: `@US-010-S02` (o restante: os botões "Publicar" e "Rejeitar"; "Arquivar" chega na 4.3), `@US-010-S03`, `@US-010-S04` (a frase "vê o anúncio entre os mais recentes" e "o encontra na busca" ficam para a Fase 5), `@US-010-S05`, `@US-010-S07`, `@US-010-S08`

**NFRs covered**: `NFR-13`

**References**: ADR-004

**Objective**: Decidir o anúncio com registro de quem decidiu, motivo obrigatório na rejeição e proteção contra decisão simultânea.

**Decisões aprovadas (2026-10-05)**: D1 páginas próprias de confirmação e de motivo (não diálogos; o diálogo vem depois como melhoria progressiva) · D2 a decisão de publicar roda `AdSubmissionRules` antes (se falta algo, a pré-visualização mostra "Faltam N itens…" e a situação não muda; proteção que a SPEC não pede, no BACKLOG) · D3 o motivo da rejeição é gravado também na auditoria `ad.reject` (autor, alvo, situação antes e depois, motivo até 500 caracteres) · D4 o mesmo administrador duas vezes recebe "Este anúncio já foi publicado"; corrida entre administradores, "…por outro administrador" · D5 a prova da 4.2 é o motivo na tela de edição de quem cadastrou, a situação no banco e a foto entregue ao visitante; "vê entre os mais recentes" e "encontra na busca" ficam para os testes da Fase 5 · D6 o cenário S08 é provado nos testes HTTP (o banco do E2E já tem telefone); o E2E cobre só o caminho feliz.

**Files created or modified**:
- `src/GazetaMarketplace.Core/Ads/IAdReview.cs` (`IAdReview`, `ReviewOutcome`, `ReviewResult`) · `AdMessages.cs` (frases da SPEC: publicado, rejeitado, telefone, "já foi decidido")
- `src/GazetaMarketplace.Infrastructure/Ads/AdReview.cs` (confere situação, telefone e pendências; decide pelo `IAdService.TransitionAsync`; refaz uma vez se o `RowVersion` mudou) · `AdService.cs` (a auditoria da rejeição leva o motivo) · `ServiceCollectionExtensions.cs`
- `src/GazetaMarketplace.Web/Areas/Panel/Controllers/ReviewQueueController.cs` (`GET`/`POST` `/painel/anuncios/{id}/publicar` e `/rejeitar`) · `Models/ReviewViewModels.cs` (`RejectViewModel`, `PublishConfirmationViewModel`, aviso da pré-visualização)
- `Views/ReviewQueue/ConfirmPublish.cshtml`, `Reject.cshtml` · `Preview.cshtml` (Publicar, Rejeitar, Editar; avisos de conflito, de telefone e de pendências) · `Index.cshtml` (mensagem de sucesso)
- `wwwroot/js/pages/ad-confirm.js` (rótulo do botão ocupado; foco no campo com erro) · `wwwroot/css/components/bootstrap-tema.css` (contraste do `btn-outline-danger`)

**Acceptance Criteria**:
- [x] `@US-010-S03` (@happy): Publicar um anúncio — o *Then* do SPEC é atendido (situação, quem e quando no banco, sai da fila, foto passa a chegar ao visitante)
- [x] `@US-010-S04` (@happy): Rejeitar um anúncio com motivo — atendido **em parte**: situação, quem/quando/motivo e o motivo na tela de edição de quem cadastrou; "vê entre os mais recentes" e "encontra na busca" ficam para a Fase 5
- [x] `@US-010-S05` (@negative): Rejeitar sem informar o motivo — o *Then* do SPEC é atendido
- [x] `@US-010-S07` (@edge): Dois administradores decidem o mesmo anúncio — o *Then* do SPEC é atendido
- [x] `@US-010-S08` (@negative): Publicar sem o telefone do site configurado — o *Then* do SPEC é atendido
- [x] Publicar exige o telefone do site configurado e confere de novo as pendências do anúncio
- [x] Rejeitar exige motivo (aparado, até 500 caracteres), visível ao autor; `PublishedBy/At`, `RejectedBy/At/Reason` e a ação entram em `AuditEntries`
- [x] Dois administradores decidindo o mesmo anúncio: só um passa (`RowVersion`), o outro vê "Este anúncio já foi publicado por outro administrador"

**Tests added** (nomes reais):
- `Web.Tests/Review/ReviewTests` — 17 métodos (19 casos): `US010S03`, `US010S04`, `US010S05` (3 motivos vazios), `US010S07` (dois administradores, nas duas ordens), `US010S08`, auditoria da publicação e da rejeição (com motivo), reenvio limpa o motivo e a auditoria guarda o histórico, limite de 500 caracteres, HTML no motivo, clique duplo do mesmo administrador, pendências reconferidas, matriz de situações, Redator (GET e POST), sem login, sem token, anúncio inexistente, foto entregue ao visitante
- `Web.Tests/Review/ReviewSupport` — anúncio publicável, segundo administrador, telefone
- `IntegrationTests/ReviewDecisionConcurrencyTests` — 4 pedidos ao mesmo tempo × 3 rodadas, publicar e rejeitar, no SQL Server real: uma passagem, uma auditoria, nenhum 500
- `Web.Tests.Playwright/Ads/ReviewDecisionE2ETests` — 5 testes: publicar (foto do visitante antes e depois), rejeitar com motivo (lido na edição), rejeitar sem motivo, duas janelas (`US010S07`), axe e rolagem em 1280 e 320 px (inclui o erro)

**Dependencies**: 4.1, 2.7, 0.6

**Verification**: Done when every test under "Tests added" passes (unitários 1253, integração 105, E2E 79) e as 9 mutações são mortas.

**Estimate**: L

### Task 4.3: Despublicar e arquivar anúncios

**User stories**: US-011

**Scenarios covered**: `@US-011-S01`, `@US-011-S02`, `@US-011-S03`, `@US-011-S05`, `@US-011-S06`, `@US-011-S07`

**NFRs covered**: `NFR-13`

**References**: —

**Objective**: Retirar anúncios do site: despublicar volta a Rascunho e arquivar é definitivo.

**Files to modify**:
- `src/GazetaMarketplace.Core/Ads/TakedownService.cs`
- `src/GazetaMarketplace.Web/Areas/Panel/Controllers/AdsController.cs`
- `src/GazetaMarketplace.Web/Areas/Panel/Views/Ads/_TakedownDialogs.cshtml`

**Acceptance Criteria**:
- [ ] `@US-011-S01` (@happy): Despublicar um anúncio — o *Then* do SPEC é atendido
- [ ] `@US-011-S02` (@happy): Arquivar um anúncio publicado — o *Then* do SPEC é atendido
- [ ] `@US-011-S03` (@edge): Cancelar a confirmação — o *Then* do SPEC é atendido
- [ ] `@US-011-S05` (@edge): Arquivar um anúncio que ainda não foi publicado — o *Then* do SPEC é atendido
- [ ] `@US-011-S06` (@negative): Anúncio arquivado não tem ações de retirada — o *Then* do SPEC é atendido
- [ ] `@US-011-S07` (@negative): Redator não vê as ações de retirada — o *Then* do SPEC é atendido
- [ ] Despublicar volta o anúncio a Rascunho e o tira do site; arquivar o torna definitivo e somente leitura, sem ações de retirada
- [ ] Arquivar é possível também num anúncio ainda não publicado; o diálogo tem "Cancelar" sem efeito
- [ ] Redator não vê as ações de retirada; ações registradas em `AuditEntries`

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Revisão/TakedownTests.US011S01_DespublicarUmAnuncio` — `@US-011-S01`
- `tests/GazetaMarketplace.Web.Tests/Revisão/TakedownTests.US011S02_ArquivarUmAnuncioPublicado` — `@US-011-S02`
- `tests/GazetaMarketplace.Web.Tests/Revisão/TakedownTests.US011S03_CancelarAConfirmacao` — `@US-011-S03`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Revisão/TakedownE2ETests.US011S03_CancelarAConfirmacao` — `@US-011-S03` (E2E, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Revisão/TakedownTests.US011S05_ArquivarUmAnuncioQueAindaNaoFoiPublicado` — `@US-011-S05`
- `tests/GazetaMarketplace.Web.Tests/Revisão/TakedownTests.US011S06_AnuncioArquivadoNaoTemAcoesDeRetirada` — `@US-011-S06`
- `tests/GazetaMarketplace.Web.Tests/Revisão/TakedownTests.US011S07_RedatorNaoVeAsAcoesDeRetirada` — `@US-011-S07`

**Dependencies**: 4.2

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Despublicar e arquivar pelo navegador.

**Estimate**: M

### Task 4.4: Lista de anúncios do painel

**User stories**: US-012

**Scenarios covered**: `@US-012-S01`, `@US-012-S02`, `@US-012-S03`, `@US-012-S04`, `@US-012-S05`, `@US-012-S06`, `@US-012-S07`, `@US-012-S08`

**NFRs covered**: `NFR-13`, `NFR-04`

**References**: ADR-004

**Objective**: Lista de trabalho com busca por título, filtro por situação e 20 por página, lida por um *read repository* em Dapper.

**Files to modify**:
- Trocar a página **provisória** `src/GazetaMarketplace.Web/Areas/Panel/Views/Ads/Index.cshtml` (criada na 1.1) pela lista real de "Meus anúncios"; o Redator cai nela depois de entrar (`PanelRoutes.Ads`)
- `src/GazetaMarketplace.Core/Ads/IPanelAdListReadRepository.cs`
- src/GazetaMarketplace.Infrastructure/Ads/PainelListaReadRepository.cs (Dapper: junção de anúncios, categorias e autor; filtros e ordenação dinâmicos)
- `src/GazetaMarketplace.Core/Ads/IPanelAdListReadRepository.cs`
- src/GazetaMarketplace.Infrastructure/Ads/PainelListaReadRepository.cs (Dapper: junção de anúncios, categorias e autor; filtros e ordenação dinâmicos)
- src/GazetaMarketplace.Web/Areas/Panel/Controllers/AdsController.cs (Index)
- `src/GazetaMarketplace.Web/Areas/Panel/Views/Ads/Index.cshtml`
- `src/GazetaMarketplace.Core/Ads/PanelAdListService.cs`

**Acceptance Criteria**:
- [ ] `@US-012-S01` (@happy): Redator vê apenas os próprios anúncios — o *Then* do SPEC é atendido
- [ ] `@US-012-S02` (@happy): Administrador vê todos os anúncios com o autor — o *Then* do SPEC é atendido
- [ ] `@US-012-S03` (@happy): Filtrar por situação — o *Then* do SPEC é atendido
- [ ] `@US-012-S04` (@happy): Buscar um anúncio pelo título no painel — o *Then* do SPEC é atendido
- [ ] `@US-012-S05` (@happy): Abrir um anúncio da lista — o *Then* do SPEC é atendido
- [ ] `@US-012-S06` (@edge): Redator ainda sem anúncios — o *Then* do SPEC é atendido
- [ ] `@US-012-S07` (@edge): Lista com mais de 20 anúncios — o *Then* do SPEC é atendido
- [ ] `@US-012-S08` (@negative): Falha ao carregar a lista — o *Then* do SPEC é atendido
- [ ] O Redator vê só os próprios anúncios; o Administrador vê todos com o autor; arquivados ficam escondidos até filtrar por "Arquivado"
- [ ] Busca por título sem acento e sem diferença de maiúsculas; 20 por página, com total em `role="status"`
- [ ] Lista vazia, sem resultado e erro com "Tentar novamente"; a lista não mostra preço (decisão do Product Owner)
- [ ] A leitura é um *read repository* em Dapper com parâmetros nomeados e ordenação só por colunas permitidas; o Redator só recebe linhas dele (filtro de autoria dentro da própria consulta)
- [ ] No `/build` as telas usam o repositório falso; a consulta é provada com SQL Server real no `/test`
- [ ] A leitura é um *read repository* em Dapper com parâmetros nomeados e ordenação só por colunas permitidas; o Redator só recebe linhas dele (filtro de autoria dentro da própria consulta)
- [ ] No `/build` as telas usam o repositório falso; a consulta é provada com SQL Server real no `/test`
- [ ] **RC-15:** a consulta Dapper da lista do painel usa `commandTimeout` de 10 s

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListTests.US012S01_RedatorVeApenasOsPropriosAnuncios` — `@US-012-S01`
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListTests.US012S02_AdministradorVeTodosOsAnunciosComOAutor` — `@US-012-S02`
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListTests.US012S03_FiltrarPorSituacao` — `@US-012-S03`
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListTests.US012S04_BuscarUmAnuncioPeloTituloNoPainel` — `@US-012-S04`
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListTests.US012S05_AbrirUmAnuncioDaLista` — `@US-012-S05`
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListTests.US012S06_RedatorAindaSemAnuncios` — `@US-012-S06`
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListTests.US012S07_ListaComMaisDe20Anuncios` — `@US-012-S07`
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListTests.US012S08_FalhaAoCarregarALista` — `@US-012-S08`
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListQueryTests.US012S01_RedatorVeApenasOsPropriosAnuncios` — `@US-012-S01` (consulta com SQL Server real, TestContainers, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListQueryTests.US012S02_AdministradorVeTodosOsAnunciosComOAutor` — `@US-012-S02` (consulta com SQL Server real, TestContainers, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListQueryTests.US012S03_FiltrarPorSituacao` — `@US-012-S03` (consulta com SQL Server real, TestContainers, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListQueryTests.US012S04_BuscarUmAnuncioPeloTituloNoPainel` — `@US-012-S04` (consulta com SQL Server real, TestContainers, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListQueryTests.US012S07_ListaComMaisDe20Anuncios` — `@US-012-S07` (consulta com SQL Server real, TestContainers, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Panel/ListTests.ArquivadosEscondidos_AteFiltrar`
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListQueryTests.Redator_RecebeSoOsProprios_NaConsulta (TestContainers, roda no /test)`
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListQueryTests.OrdenacaoForaDaLista_EIgnorada (TestContainers, roda no /test)`
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListQueryTests.ArquivadosEscondidos_AteFiltrarPorArquivado (TestContainers, roda no /test)`
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListQueryTests.Redator_RecebeSoOsProprios_NaConsulta (TestContainers, roda no /test)`
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListQueryTests.OrdenacaoForaDaLista_EIgnorada (TestContainers, roda no /test)`
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListQueryTests.ArquivadosEscondidos_AteFiltrarPorArquivado (TestContainers, roda no /test)`
- `tests/GazetaMarketplace.Web.Tests/Panel/PanelAdListQueryTests.ConsultaQueEstouraOTempo_Devolve503SemPilha (TestContainers, roda no /test)`

**Dependencies**: 3.3, 1.1

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Conferir as listas dos dois papéis com mais de 20 anúncios.

**Estimate**: M

---
## Checkpoint 4 — Revisão completa

**Verify before proceeding**:
- [ ] Fila, publicar, rejeitar, despublicar e arquivar funcionam com os dois papéis
- [ ] Decisão simultânea devolve 409
- [ ] Ações registradas em `AuditEntries`

---

## Fase 5 — Site público

### Task 5.1: Página inicial e páginas de categoria

**User stories**: US-001

**Scenarios covered**: `@US-001-S01`, `@US-001-S02`, `@US-001-S03`, `@US-001-S04`, `@US-001-S05`, `@US-001-S06`, `@US-001-S07`, `@US-001-S08`

**NFRs covered**: `NFR-04`, `NFR-17`

**References**: ADR-006

**Objective**: Página inicial com categorias e os 12 anúncios mais recentes, e página de categoria com subcategorias e caminho de navegação.

**Files to modify**:
- `src/GazetaMarketplace.Web/Controllers/HomeController.cs, CategoriaController.cs`
- `src/GazetaMarketplace.Web/Views/Home/Index.cshtml`
- `src/GazetaMarketplace.Web/Views/Category/Index.cshtml`
- `src/GazetaMarketplace.Core/Vitrine/VitrineService.cs`

**Acceptance Criteria**:
- [ ] `@US-001-S01` (@happy): Página inicial mostra categorias e anúncios recentes — o *Then* do SPEC é atendido
- [ ] `@US-001-S02` (@happy): Entrar em uma categoria principal — o *Then* do SPEC é atendido
- [ ] `@US-001-S03` (@happy): Entrar em uma subcategoria e voltar pelo caminho de navegação — o *Then* do SPEC é atendido
- [ ] `@US-001-S04` (@edge): Categoria sem anúncios publicados — o *Then* do SPEC é atendido
- [ ] `@US-001-S05` (@edge): Site ainda sem nenhum anúncio publicado — o *Then* do SPEC é atendido
- [ ] `@US-001-S06` (@negative): Falha ao carregar a página inicial — o *Then* do SPEC é atendido
- [ ] `@US-001-S07` (@negative): Endereço de categoria que não existe — o *Then* do SPEC é atendido
- [ ] `@US-001-S08` (@edge): Página inicial em tela de celular estreita — o *Then* do SPEC é atendido
- [ ] A página inicial mostra as categorias principais e os 12 anúncios publicados mais recentes; vazia mostra "Em breve teremos novos anúncios"
- [ ] A categoria principal mostra suas subcategorias e os anúncios de todas as descendentes; o caminho de navegação cobre até 3 níveis
- [ ] Categoria inexistente mostra "Categoria não encontrada"; falha mostra mensagem com código de referência; 320 px sem rolagem horizontal

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Vitrine/VitrineTests.US001S01_PaginaInicialMostraCategoriasEAnunciosRecentes` — `@US-001-S01`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/VitrineTests.US001S02_EntrarEmUmaCategoriaPrincipal` — `@US-001-S02`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/VitrineTests.US001S03_EntrarEmUmaSubcategoriaEVoltarPeloCaminhoDeNavegacao` — `@US-001-S03`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Vitrine/VitrineTestsE2E.US001S03_EntrarEmUmaSubcategoriaEVoltarPeloCaminhoDeNavegacao` — `@US-001-S03` (E2E, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Vitrine/VitrineTests.US001S04_CategoriaSemAnunciosPublicados` — `@US-001-S04`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/VitrineTests.US001S05_SiteAindaSemNenhumAnuncioPublicado` — `@US-001-S05`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/VitrineTests.US001S06_FalhaAoCarregarAPaginaInicial` — `@US-001-S06`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/VitrineTests.US001S07_EnderecoDeCategoriaQueNaoExiste` — `@US-001-S07`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/VitrineTests.US001S08_PaginaInicialEmTelaDeCelularEstreita` — `@US-001-S08`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Vitrine/VitrineTestsE2E.US001S08_PaginaInicialEmTelaDeCelularEstreita` — `@US-001-S08` (E2E, `/test`)

**Dependencies**: 3.8, 2.1, 0.7

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Navegar da página inicial até uma subcategoria.

**Estimate**: M

### Task 5.2: Detalhe do anúncio e galeria de fotos

**User stories**: US-003

**Scenarios covered**: `@US-003-S01`, `@US-003-S02`, `@US-003-S03`, `@US-003-S04`, `@US-003-S05`, `@US-003-S06`, `@US-003-S07`, `@US-003-S08`

**NFRs covered**: `NFR-01`, `NFR-03`, `NFR-05`, `NFR-16`

**References**: ADR-005

**Objective**: Página do anúncio com galeria (destaque, miniaturas, ampliar), características do grupo e página de indisponibilidade.

**Files to modify**:
- `src/GazetaMarketplace.Web/Controllers/AdController.cs`
- `src/GazetaMarketplace.Web/Views/Ad/Detalhe.cshtml`
- src/GazetaMarketplace.Web/wwwroot/js/pages/ad-detail.js (galeria)
- `src/GazetaMarketplace.Web/wwwroot/css/pages/anuncio-detalhe.css`

**Acceptance Criteria**:
- [ ] `@US-003-S01` (@happy): Abrir um anúncio completo — o *Then* do SPEC é atendido
- [ ] `@US-003-S02` (@happy): Percorrer a galeria de um anúncio com 20 fotos — o *Then* do SPEC é atendido
- [ ] `@US-003-S03` (@happy): Ampliar uma foto — o *Then* do SPEC é atendido
- [ ] `@US-003-S04` (@edge): Anúncio de categoria sem ficha de veículo nem de terreno — o *Then* do SPEC é atendido
- [ ] `@US-003-S05` (@edge): Anúncio com uma única foto — o *Then* do SPEC é atendido
- [ ] `@US-003-S06` (@negative): Abrir um anúncio que não está mais disponível — o *Then* do SPEC é atendido
- [ ] `@US-003-S07` (@negative): Uma foto não carrega — o *Then* do SPEC é atendido
- [ ] `@US-003-S08` (@edge): Anúncio em tela de celular estreita — o *Then* do SPEC é atendido
- [ ] Galeria com setas, miniaturas, contador "5 de 20", ampliar em janela com foco preso e Esc, navegação por teclado e toque; uma foto só não mostra setas nem miniaturas
- [ ] Foto que não carrega mostra "Foto indisponível" e as outras continuam navegáveis
- [ ] Anúncio que não está publicado (qualquer outra situação, ou endereço inexistente) mostra a **mesma** mensagem de indisponibilidade, sem revelar conteúdo
- [ ] Fotos além da primeira carregam sob demanda; o endereço é `/anuncio/{id}/{slug}` e o preço/Salário/Tipo segue o grupo

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Vitrine/DetalheTests.US003S01_AbrirUmAnuncioCompleto` — `@US-003-S01`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/DetalheTests.US003S02_PercorrerAGaleriaDeUmAnuncioCom20Fotos` — `@US-003-S02`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Vitrine/DetalheTestsE2E.US003S02_PercorrerAGaleriaDeUmAnuncioCom20Fotos` — `@US-003-S02` (E2E, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Vitrine/DetalheTests.US003S03_AmpliarUmaFoto` — `@US-003-S03`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Vitrine/DetalheTestsE2E.US003S03_AmpliarUmaFoto` — `@US-003-S03` (E2E, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Vitrine/DetalheTests.US003S04_AnuncioDeCategoriaSemFichaDeVeiculoNemDeTerreno` — `@US-003-S04`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/DetalheTests.US003S05_AnuncioComUmaUnicaFoto` — `@US-003-S05`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Vitrine/DetalheTestsE2E.US003S05_AnuncioComUmaUnicaFoto` — `@US-003-S05` (E2E, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Vitrine/DetalheTests.US003S06_AbrirUmAnuncioQueNaoEstaMaisDisponivel` — `@US-003-S06`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/DetalheTests.US003S07_UmaFotoNaoCarrega` — `@US-003-S07`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Vitrine/DetalheTestsE2E.US003S07_UmaFotoNaoCarrega` — `@US-003-S07` (E2E, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Vitrine/DetalheTests.US003S08_AnuncioEmTelaDeCelularEstreita` — `@US-003-S08`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Vitrine/DetalheTestsE2E.US003S08_AnuncioEmTelaDeCelularEstreita` — `@US-003-S08` (E2E, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Detalhe/IndisponivelTests.NaoPublicado_E_Inexistente_TemAMesmaResposta`
- `tests/GazetaMarketplace.Web.Tests/Detalhe/PesoTests.FotosAlemDaPrimeira_TemLoadingLazy`

**Dependencies**: 3.8, 3.4, 0.7

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Abrir anúncios dos três tipos e uma foto quebrada.

**Estimate**: L

### Task 5.3: Contato por telefone e WhatsApp

**User stories**: US-004

**Scenarios covered**: `@US-004-S01`, `@US-004-S02`, `@US-004-S03`, `@US-004-S04`, `@US-004-S05`

**References**: —

**Objective**: Bloco de contato do intermediário com "Ligar" e "Chamar no WhatsApp" e a mensagem pré-preenchida.

**Files to modify**:
- `src/GazetaMarketplace.Web/Views/Shared/_Contato.cshtml`
- `src/GazetaMarketplace.Core/Contato/WhatsAppLink.cs`

**Acceptance Criteria**:
- [ ] `@US-004-S01` (@happy): Chamar no WhatsApp a partir de um anúncio — o *Then* do SPEC é atendido
- [ ] `@US-004-S02` (@happy): Ligar a partir de um anúncio — o *Then* do SPEC é atendido
- [ ] `@US-004-S03` (@happy): O contato é visível sem login — o *Then* do SPEC é atendido
- [ ] `@US-004-S04` (@edge): Título com acentos e símbolos na mensagem do WhatsApp — o *Then* do SPEC é atendido
- [ ] `@US-004-S05` (@edge): WhatsApp em computador sem o aplicativo instalado — o *Then* do SPEC é atendido
- [ ] O contato aparece sem login com o telefone do site; título com acentos, aspas e símbolos vai codificado na mensagem
- [ ] "Ligar" usa `tel:`; "Chamar no WhatsApp" usa `https://wa.me/` e abre em nova aba com `rel="noopener"`; em computador sem o aplicativo, o WhatsApp Web; nenhuma chamada do servidor

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Vitrine/ContatoTests.US004S01_ChamarNoWhatsAppAPartirDeUmAnuncio` — `@US-004-S01`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/ContatoTests.US004S02_LigarAPartirDeUmAnuncio` — `@US-004-S02`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/ContatoTests.US004S03_OContatoEVisivelSemLogin` — `@US-004-S03`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/ContatoTests.US004S04_TituloComAcentosESimbolosNaMensagemDoWhatsApp` — `@US-004-S04`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/ContatoTests.US004S05_WhatsAppEmComputadorSemOAplicativoInstalado` — `@US-004-S05`
- `tests/GazetaMarketplace.Web.Tests/Contato/WhatsAppLinkTests.TituloComAcentosEAspas_VaiCodificado`

**Dependencies**: 5.2, 2.7

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Abrir os links num celular e num computador.

**Estimate**: S

### Task 5.4: Busca e filtros

**User stories**: US-002

**Scenarios covered**: `@US-002-S01`, `@US-002-S02`, `@US-002-S03`, `@US-002-S04`, `@US-002-S05`, `@US-002-S06`, `@US-002-S07`, `@US-002-S08`, `@US-002-S09`, `@US-002-S10`, `@US-002-S11`, `@US-002-S12`

**NFRs covered**: `NFR-04`

**References**: ADR-006, ADR-002, ADR-004

**Objective**: Busca por texto normalizado com filtros por categoria, UF, cidade, preço e características do grupo, ordenação e paginação de 24, lida por um *read repository* em Dapper.

**Files to modify**:
- `src/GazetaMarketplace.Core/Search/ISearchReadRepository.cs`
- src/GazetaMarketplace.Infrastructure/Search/BuscaReadRepository.cs (Dapper, SQL montado pelo `SqlBuilder`)
- `src/GazetaMarketplace.Core/Search/ISearchReadRepository.cs`
- src/GazetaMarketplace.Infrastructure/Search/BuscaReadRepository.cs (Dapper, SQL montado pelo `SqlBuilder`)
- `src/GazetaMarketplace.Web/Controllers/SearchController.cs`
- `src/GazetaMarketplace.Web/Views/Search/Index.cshtml`
- `src/GazetaMarketplace.Core/Search/SearchService.cs`
- `src/GazetaMarketplace.Web/wwwroot/js/pages/search.js`

**Acceptance Criteria**:
- [ ] `@US-002-S01` (@happy): Buscar por texto — o *Then* do SPEC é atendido
- [ ] `@US-002-S02` (@happy): Combinar categoria, localização e preço — o *Then* do SPEC é atendido
- [ ] `@US-002-S03` (@happy): Filtrar por características de veículo — o *Then* do SPEC é atendido
- [ ] `@US-002-S04` (@happy): Filtrar terrenos, sítios e fazendas por área — o *Then* do SPEC é atendido
- [ ] `@US-002-S05` (@happy): Ordenar os resultados — o *Then* do SPEC é atendido
- [ ] `@US-002-S06` (@happy): Paginar os resultados — o *Then* do SPEC é atendido
- [ ] `@US-002-S07` (@edge): Busca sem resultados — o *Then* do SPEC é atendido
- [ ] `@US-002-S08` (@negative): Faixa de preço invertida — o *Then* do SPEC é atendido
- [ ] `@US-002-S09` (@edge): Compartilhar uma busca pelo endereço da página — o *Then* do SPEC é atendido
- [ ] `@US-002-S10` (@edge): Trocar a UF limpa a cidade escolhida — o *Then* do SPEC é atendido
- [ ] `@US-002-S11` (@negative): Falha ao buscar — o *Then* do SPEC é atendido
- [ ] `@US-002-S12` (@edge): Busca em tela de celular estreita — o *Then* do SPEC é atendido
- [ ] Texto compara título e descrição normalizados; categoria principal inclui todas as descendentes; os filtros se combinam; só anúncios publicados
- [ ] Filtros específicos vêm do grupo da categoria escolhida (A7 c); trocar a UF limpa a cidade e a cidade só existe depois da UF
- [ ] Serviços ficam fora da faixa de preço e no fim de "Menor preço" e "Maior preço" (A6); faixa invertida mostra o erro junto do campo
- [ ] 24 por página com ordem estável; os filtros ficam no endereço e reabrem iguais; falha mostra mensagem com código de referência; consulta com parâmetros, nunca concatenação
- [ ] A leitura é um *read repository* em Dapper; o fragmento do somente-publicados é o único de `SqlFragments` e cada consulta pública tem teste com anúncio em cada situação
- [ ] Filtros e ordenação dinâmicos: valores sempre como parâmetros; coluna e direção de ordenação só de uma lista permitida
- [ ] No `/build` as telas usam o repositório falso; a consulta é provada com SQL Server real no `/test`
- [ ] A leitura é um *read repository* em Dapper; o fragmento do somente-publicados é o único de `SqlFragments` e cada consulta pública tem teste com anúncio em cada situação
- [ ] Filtros e ordenação dinâmicos: valores sempre como parâmetros; coluna e direção de ordenação só de uma lista permitida
- [ ] No `/build` as telas usam o repositório falso; a consulta é provada com SQL Server real no `/test`
- [ ] **RC-15:** termo de busca com no máximo 100 caracteres (mensagem junto do campo) e consulta Dapper com `commandTimeout` de 10 s; estouro devolve 503 com código de referência, sem pilha

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Vitrine/SearchTests.US002S01_BuscarPorTexto` — `@US-002-S01`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/SearchTests.US002S02_CombinarCategoriaLocalizacaoEPreco` — `@US-002-S02`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/SearchTests.US002S03_FiltrarPorCaracteristicasDeVeiculo` — `@US-002-S03`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/SearchTests.US002S04_FiltrarTerrenosSitiosEFazendasPorArea` — `@US-002-S04`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/SearchTests.US002S05_OrdenarOsResultados` — `@US-002-S05`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/SearchTests.US002S06_PaginarOsResultados` — `@US-002-S06`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/SearchTests.US002S07_BuscaSemResultados` — `@US-002-S07`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/SearchTests.US002S08_FaixaDePrecoInvertida` — `@US-002-S08`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/SearchTests.US002S09_CompartilharUmaBuscaPeloEnderecoDaPagina` — `@US-002-S09`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/SearchTests.US002S10_TrocarAUFLimpaACidadeEscolhida` — `@US-002-S10`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Vitrine/SearchE2ETests.US002S10_TrocarAUFLimpaACidadeEscolhida` — `@US-002-S10` (E2E, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Vitrine/SearchTests.US002S11_FalhaAoBuscar` — `@US-002-S11`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/SearchTests.US002S12_BuscaEmTelaDeCelularEstreita` — `@US-002-S12`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Vitrine/SearchE2ETests.US002S12_BuscaEmTelaDeCelularEstreita` — `@US-002-S12` (E2E, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Vitrine/SearchQueryTests.US002S01_BuscarPorTexto` — `@US-002-S01` (consulta com SQL Server real, TestContainers, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Vitrine/SearchQueryTests.US002S02_CombinarCategoriaLocalizacaoEPreco` — `@US-002-S02` (consulta com SQL Server real, TestContainers, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Vitrine/SearchQueryTests.US002S03_FiltrarPorCaracteristicasDeVeiculo` — `@US-002-S03` (consulta com SQL Server real, TestContainers, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Vitrine/SearchQueryTests.US002S04_FiltrarTerrenosSitiosEFazendasPorArea` — `@US-002-S04` (consulta com SQL Server real, TestContainers, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Vitrine/SearchQueryTests.US002S05_OrdenarOsResultados` — `@US-002-S05` (consulta com SQL Server real, TestContainers, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Vitrine/SearchQueryTests.US002S06_PaginarOsResultados` — `@US-002-S06` (consulta com SQL Server real, TestContainers, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Vitrine/SearchQueryTests.US002S07_BuscaSemResultados` — `@US-002-S07` (consulta com SQL Server real, TestContainers, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Search/ServicosTests.FaixaDePreco_ExcluiServicos_OrdenacaoMandaParaOFim`
- `tests/GazetaMarketplace.Web.Tests/Search/NormalizacaoTests.BuscaIgnoraAcentosEMaiusculas_NoTituloENaDescricao`
- `tests/GazetaMarketplace.Web.Tests/Search/SecurityTests.Termo_E_Parametro_NaoConcatenado`
- `tests/GazetaMarketplace.Web.Tests/Search/DesempenhoTests.Com200Anuncios_RespondeAbaixoDe500ms (TestContainers, roda no /test)`
- `tests/GazetaMarketplace.Web.Tests/Search/BuscaQueryTests.SoAnunciosPublicados_EmCadaSituacao (TestContainers, roda no /test)`
- `tests/GazetaMarketplace.Web.Tests/Search/BuscaQueryTests.OrdenacaoForaDaLista_EIgnorada (TestContainers, roda no /test)`
- `tests/GazetaMarketplace.Web.Tests/Search/BuscaQueryTests.TermoComAspasEPonto_NaoQuebraNemInjeta (TestContainers, roda no /test)`
- `tests/GazetaMarketplace.Web.Tests/Search/BuscaQueryTests.SoAnunciosPublicados_EmCadaSituacao (TestContainers, roda no /test)`
- `tests/GazetaMarketplace.Web.Tests/Search/BuscaQueryTests.OrdenacaoForaDaLista_EIgnorada (TestContainers, roda no /test)`
- `tests/GazetaMarketplace.Web.Tests/Search/BuscaQueryTests.TermoComAspasEPonto_NaoQuebraNemInjeta (TestContainers, roda no /test)`
- `tests/GazetaMarketplace.Web.Tests/Search/SearchTests.TermoComMaisDe100Caracteres_MostraErroJuntoDoCampo`
- `tests/GazetaMarketplace.Web.Tests/Search/BuscaQueryTests.ConsultaQueEstouraOTempo_Devolve503SemPilha (TestContainers, roda no /test)`
- `tests/GazetaMarketplace.Web.Tests/Search/EnderecoHandoffTests.FiltrosNoEndereco_ReabremIguaisNoServidor (produtor: `search.js`; consumidor: `BuscaService`)`

**Dependencies**: 3.8, 3.1, 2.3, 0.7

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Combinar filtros no navegador e abrir o mesmo endereço em outra aba.

**Estimate**: L

### Task 5.5: Favoritos no navegador

**User stories**: US-005, US-011

**Scenarios covered**: `@US-005-S01`, `@US-005-S02`, `@US-005-S03`, `@US-005-S04`, `@US-005-S05`, `@US-005-S06`, `@US-005-S07`, `@US-005-S08`, `@US-011-S04`

**NFRs covered**: `NFR-13`

**References**: —

**Objective**: Favoritos em `localStorage`, página "Meus favoritos" e endpoint que devolve só anúncios publicados por ids.

**Files to modify**:
- src/GazetaMarketplace.Web/Controllers/Api/AdsController.cs (listAdsByIds)
- `src/GazetaMarketplace.Web/Controllers/FavoritesController.cs`
- `src/GazetaMarketplace.Web/wwwroot/js/modules/favorites.js`
- `src/GazetaMarketplace.Web/Views/Favorites/Index.cshtml`

**Acceptance Criteria**:
- [ ] `@US-005-S01` (@happy): Favoritar um anúncio pela lista — o *Then* do SPEC é atendido
- [ ] `@US-005-S02` (@happy): Favoritar e desfavoritar pela página do anúncio — o *Then* do SPEC é atendido
- [ ] `@US-005-S03` (@happy): Favoritos continuam depois de fechar o navegador — o *Then* do SPEC é atendido
- [ ] `@US-005-S04` (@happy): Remover um anúncio da página Meus favoritos — o *Then* do SPEC é atendido
- [ ] `@US-005-S05` (@edge): Lista de favoritos vazia — o *Then* do SPEC é atendido
- [ ] `@US-005-S06` (@edge): Um favorito deixa de estar disponível — o *Then* do SPEC é atendido
- [ ] `@US-005-S07` (@negative): O navegador não permite salvar favoritos — o *Then* do SPEC é atendido
- [ ] `@US-005-S08` (@edge): Favoritos não acompanham o visitante em outro aparelho — o *Then* do SPEC é atendido
- [ ] `@US-011-S04` (@edge): Anúncio arquivado some dos favoritos do visitante — o *Then* do SPEC é atendido
- [ ] `GET /api/v1/ads?ids=` aceita até 100 ids numéricos e devolve só anúncios publicados, no envelope `PagedResult` com `priceCents` nulo em Serviços e `coverUrl` nulo em Vagas
- [ ] Ids de anúncios despublicados ou arquivados não voltam; a página tira esses ids do `localStorage` e mostra o aviso
- [ ] Armazenamento bloqueado mostra mensagem e o site continua funcionando; favoritos não acompanham o visitante em outro aparelho

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Vitrine/FavoritesTests.US005S01_FavoritarUmAnuncioPelaLista` — `@US-005-S01`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Vitrine/FavoritesE2ETests.US005S01_FavoritarUmAnuncioPelaLista` — `@US-005-S01` (E2E, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Vitrine/FavoritesTests.US005S02_FavoritarEDesfavoritarPelaPaginaDoAnuncio` — `@US-005-S02`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/FavoritesTests.US005S03_FavoritosContinuamDepoisDeFecharONavegador` — `@US-005-S03`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Vitrine/FavoritesE2ETests.US005S03_FavoritosContinuamDepoisDeFecharONavegador` — `@US-005-S03` (E2E, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Vitrine/FavoritesTests.US005S04_RemoverUmAnuncioDaPaginaMeusFavoritos` — `@US-005-S04`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Vitrine/FavoritesE2ETests.US005S04_RemoverUmAnuncioDaPaginaMeusFavoritos` — `@US-005-S04` (E2E, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Vitrine/FavoritesTests.US005S05_ListaDeFavoritosVazia` — `@US-005-S05`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/FavoritesTests.US005S06_UmFavoritoDeixaDeEstarDisponivel` — `@US-005-S06`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/FavoritesTests.US005S07_ONavegadorNaoPermiteSalvarFavoritos` — `@US-005-S07`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Vitrine/FavoritesE2ETests.US005S07_ONavegadorNaoPermiteSalvarFavoritos` — `@US-005-S07` (E2E, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Vitrine/FavoritesTests.US005S08_FavoritosNaoAcompanhamOVisitanteEmOutroAparelho` — `@US-005-S08`
- `tests/GazetaMarketplace.Web.Tests/Vitrine/FavoritesTests.US011S04_AnuncioArquivadoSomeDosFavoritosDoVisitante` — `@US-011-S04`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Vitrine/FavoritesE2ETests.US011S04_AnuncioArquivadoSomeDosFavoritosDoVisitante` — `@US-011-S04` (E2E, `/test`)
- `tests/GazetaMarketplace.Web.Tests/Favorites/ApiTests.MaisDe100Ids_Devolve400`
- `tests/GazetaMarketplace.Web.Tests/Favorites/ApiTests.IdsNaoNumericos_Devolve400`
- `tests/GazetaMarketplace.Web.Tests/Favorites/ApiTests.SoPublicados_NaOrdemPedida`
- `tests/GazetaMarketplace.Web.Tests/Favorites/FavoritosHandoffTests.IdsDoLocalStorage_ViramRespostaDaApi_ETiramOsAusentes (produtor: `favorites.js`; consumidor: `listAdsByIds`)`

**Dependencies**: 3.8, 4.3

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Favoritar, despublicar o anúncio e abrir "Meus favoritos".

**Estimate**: M

### Task 5.6: SEO básico das páginas públicas

**User stories**: **Foundation** — NFR-21 (condicional à S7).

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**NFRs covered**: `NFR-21`

**References**: —

**Objective**: Título e descrição próprios por página, mapa do site e retirada do anúncio arquivado do índice.

**Files to modify**:
- src/GazetaMarketplace.Web/Controllers/SeoController.cs (sitemap.xml, robots.txt)
- `src/GazetaMarketplace.Web/Views/Shared/_Seo.cshtml`

**Acceptance Criteria**:
- [ ] Início, categorias e cada anúncio publicado têm `<title>` e `meta description` próprios e endereço legível
- [ ] `/sitemap.xml` lista só anúncios publicados; anúncio arquivado ou despublicado responde conforme a página de indisponibilidade e sai do mapa
- [ ] Se o Product Owner não confirmar a S7, a tarefa é reduzida a `<title>` por página (condicional da NFR-21)

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Seo/SitemapTests.SoAnunciosPublicados`
- `tests/GazetaMarketplace.Web.Tests/Seo/SitemapTests.Arquivado_SaiDoMapa`
- `tests/GazetaMarketplace.Web.Tests/Seo/TitleTests.CadaPagina_TemTitleEDescriptionProprios`

**Dependencies**: 5.2, 5.1

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Abrir `/sitemap.xml` e o código-fonte de um anúncio.

**Estimate**: S

---
## Checkpoint 5 — Site público completo

**Verify before proceeding**:
- [ ] Início, categoria, busca, detalhe, contato e favoritos funcionam sem login
- [ ] Serviços e Vagas aparecem corretamente nos cards, no detalhe e na busca
- [ ] Sitemap só com anúncios publicados

---

## Fase 6 — Verificações transversais

### Task 6.1: Verificação transversal de acesso e de texto digitado

**User stories**: **Foundation** — Fecha a NFR-13 e a NFR-15 de ponta a ponta.

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**NFRs covered**: `NFR-13`, `NFR-15`

**References**: —

**Objective**: Matriz automática de acesso: toda rota do painel exige login e o papel certo; textos digitados nunca executam.

**Files to modify**:
- `tests/GazetaMarketplace.Web.Tests/Security/MatrizDeAcessoTests.cs`
- `tests/GazetaMarketplace.Web.Tests/Security/XssEmTodasAsTelasTests.cs`

**Acceptance Criteria**:
- [ ] Toda rota do painel e todo endpoint JSON de escrita é listado por reflexão e testado sem login, como Redator e como Administrador; acesso indevido responde "Você não tem permissão" sem revelar conteúdo
- [ ] Um texto com `<script>` digitado em título, descrição, nome, motivo e nome de categoria é exibido como texto em todas as telas
- [ ] Nenhuma rota nova do painel passa sem entrar na matriz (o teste falha)

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests/Security/MatrizDeAcessoTests.TodaRotaDoPainel_ExigeLogin`
- `tests/GazetaMarketplace.Web.Tests/Security/MatrizDeAcessoTests.Redator_NaoAcessaRotasDeAdministrador`
- `tests/GazetaMarketplace.Web.Tests/Security/MatrizDeAcessoTests.RotaNova_SemEntradaNaMatriz_FalhaOTeste`
- `tests/GazetaMarketplace.Web.Tests/Security/XssEmTodasAsTelasTests.Script_EmCadaCampo_ApareceComoTexto`

**Dependencies**: 1.3, 2.6, 4.4, 5.4

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Rodar a matriz e acrescentar uma rota falsa para ver o teste falhar.

**Estimate**: M

### Task 6.2: Base de verificação de acessibilidade e responsividade

**User stories**: **Foundation** — Sustenta NFR-16 e NFR-17 por tela; execução no `/test`.

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**NFRs covered**: `NFR-16`, `NFR-17`

**References**: —

**Objective**: Criar no projeto Playwright a base que roda axe-core e a matriz de larguras em todas as telas; a execução completa é do `/test`.

**Files to modify**:
- `tests/GazetaMarketplace.Web.Tests.Playwright/Support/AxeHelper.cs`
- tests/GazetaMarketplace.Web.Tests.Playwright/Support/LarguraHelper.cs (320, 768, 1024, 1280)
- `tests/GazetaMarketplace.Web.Tests.Playwright/Acessibilidade/TodasAsTelasTests.cs`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Responsividade/TodasAsTelasTests.cs`

**Acceptance Criteria**:
- [ ] Uma lista única de telas alimenta os testes de acessibilidade (0 falhas nível A e AA) e de largura (sem rolagem horizontal em 320, 768, 1024 e 1280 px)
- [ ] O teste falha se uma tela nova não estiver na lista

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests.Playwright/Acessibilidade/TodasAsTelasTests.Tela_NaoTemFalhasAxeAA (E2E, /test)`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Responsividade/TodasAsTelasTests.Tela_NaoRolaNaHorizontal (E2E, /test)`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Acessibilidade/ScreenListTests.TelaNova_SemEntradaNaLista_FalhaOTeste`

**Dependencies**: 5.1, 4.4

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Executar uma tela de amostra com o axe-core.

**Estimate**: M

### Task 6.3: Orçamentos de desempenho

**User stories**: **Foundation** — Fecha as NFRs de desempenho (medição no `/test` e no `/verify`).

**Scenarios covered**: — (nenhum; tarefa de fundação ou de sustentação)

**NFRs covered**: `NFR-01`, `NFR-02`, `NFR-03`, `NFR-04`, `NFR-05`

**References**: ADR-005, ADR-006

**Objective**: Compressão, cache de estáticos e medição de peso das páginas e de tempo de resposta com volume da v1.

**Files to modify**:
- src/GazetaMarketplace.Web/Program.cs (compressão, cache de estáticos com `asp-append-version`)
- `tests/GazetaMarketplace.Web.Tests.Playwright/Desempenho/PageWeightTests.cs`
- `tests/GazetaMarketplace.Web.Tests/Desempenho/VolumeDaV1Tests.cs`

**Acceptance Criteria**:
- [ ] Primeira carga da lista de 24 anúncios até 2 MB e do detalhe até 3 MB; capa servida na versão de 480 px
- [ ] Com ~200 anúncios ativos, busca e detalhe respondem abaixo de 500 ms no p95
- [ ] LCP abaixo de 2,5 s (perfil de celular, 4G), INP abaixo de 200 ms e CLS abaixo de 0,1 medidos no `/verify`; os limites ficam num arquivo de configuração de teste

**Tests to add**:
- `tests/GazetaMarketplace.Web.Tests.Playwright/Desempenho/PesoDasPaginasTests.ListaDe24_AteDoisMb (E2E, /test)`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Desempenho/PesoDasPaginasTests.Detalhe_AteTresMb (E2E, /test)`
- `tests/GazetaMarketplace.Web.Tests/Desempenho/VolumeDaV1Tests.Com200Anuncios_P95AbaixoDe500ms (TestContainers, roda no /test)`
- `tests/GazetaMarketplace.Web.Tests.Playwright/Desempenho/VitaisTests.LcpInpCls_NoPerfilDeCelular (E2E, /verify)`

**Dependencies**: 5.4, 5.2, 5.1

**Verification**: Done when every test under "Tests to add" passes, plus manual check: Medir as páginas com o volume de teste.

**Estimate**: M

---
## Checkpoint 6 — Verificações transversais completas

**Verify before proceeding**:
- [ ] Matriz de acesso cobre todas as rotas e falha com rota nova
- [ ] Base de acessibilidade e de larguras pronta para o `/test`
- [ ] Orçamentos de desempenho definidos em arquivo de configuração de teste

---

## 6. Risk register

| Task | Risk | Mitigation already in plan |
|------|------|----------------------------|
| 3.4 | HEIC não funciona na hospedagem (componente nativo do Magick.NET, AR-05) | `HeicTests.BibliotecaNativaAusente_DevolveMensagemClara` e `FormatoTests.Heic_E_ConvertidoParaWebP`; critério de aceite da 3.4 sobre a mensagem clara |
| 3.1 | Caminho JSON diferente entre o C# e a coluna calculada, e o filtro devolve resultado errado | `ColunasCalculadasDiferencialTests.CadaGrupoComFiltro_ClassesDeEntrada` (roda no `/test`) |
| 4.2 | Dois administradores decidem o mesmo anúncio | `ConcurrencyTests.DoisAdministradores_SoUmDecide` e o cenário `@US-010-S07` |
| 3.4 | Original com GPS vazar por alguma rota | `EntregaTests.OriginaisNaoTemRota` e `MetadadosTests.Gps_NaoSobrevive_NasVersoes` |
| 3.6 | IIS compartilhado para o processo e a limpeza não roda | `CleanupTests.RodaNaPartida_E_DepoisACada24h`: a limpeza roda na partida e apaga tudo o que passou de 30 dias |
| 1.4 | SendGrid fora do ar deixa a pessoa sem acesso | `SendGridTests.FalhaDoSendGrid_RespostaContinuaNeutra` e a redefinição pelo Administrador em `@US-014-S10` (tarefa 1.3) |
| 0.2 | Publicar sem a transformação do `web.config` e o site subir sem configuração | `OptionsTests.Producao_SemPastaDeFotos_FalhaNaPartida` e `OptionsTests.Producao_SemChaveSendGrid_FalhaNaPartida` |
| 1.1 | Chaves do Data Protection perdidas derrubam todas as sessões | Critério de aceite da 0.2 sobre a pasta de chaves obrigatória em produção e `OptionsTests.Producao_SemPastaDeFotos_FalhaNaPartida` (mesma validação para a pasta de chaves) |
| 3.2 | ViaCEP lento ou fora do ar trava o formulário | `CepEndpointTests.ViaCepLento_Devolve503Em5Segundos` e `@US-008-S14` |
| 5.4 | Consulta Dapper esquecer o somente-publicados e mostrar anúncio não publicado | `SearchQueryTests.SoAnunciosPublicados_EmCadaSituacao` (no `/test`) e `SqlBuilderTests.FragmentoSomentePublicados_E_UnicoEReutilizado` |
| 4.4 | Consulta Dapper da lista do painel devolver anúncio de outro Redator (IDOR) | `PanelAdListQueryTests.Redator_RecebeSoOsProprios_NaConsulta` (no `/test`) e `MatrizDeAcessoTests.Redator_NaoAcessaRotasDeAdministrador` (tarefa 6.1) |
| 5.4 | Injeção de SQL pela ordenação ou pelo termo de busca | `SqlBuilderTests.OrdenacaoForaDaLista_EIgnorada`, `SearchQueryTests.TermoComAspasEPonto_NaoQuebraNemInjeta` e `SecurityTests.Termo_E_Parametro_NaoConcatenado` |
| 0.6 | Escrita Dapper sem justificativa ou fora da transação do EF | `DapperJustificationTests.TodaEscritaDapper_TemComentarioComOMotivo` e o critério de aceite da 0.6 sobre a conexão e a transação do `DbContext` |
| 5.4 | Busca lenta com o volume da v1 | `DesempenhoTests.Com200Anuncios_RespondeAbaixoDe500ms` (no `/test`) e `VolumeDaV1Tests.Com200Anuncios_P95AbaixoDe500ms` |
| 2.5 | Catálogo copiado da API da OLX (A5) e credencial antiga do GazetaOnline | Critério de aceite da 2.5: nenhuma credencial no repositório; `ExportTests.SemVariavelDeConexao_ParaSemGerarArquivo`; bloqueio de lançamento registrado na seção 9 |
| 2.4 | Listas novas não definidas (PL-01) travam 9 grupos | Critério de aceite da 2.4 (bloqueado até a PL-01); a tarefa não fecha sem a resposta |
| 0.4 | Acesso indevido por rota nova sem política de papel | `MatrizDeAcessoTests.RotaNova_SemEntradaNaMatriz_FalhaOTeste` (tarefa 6.1) |
| 3.5 | Arquivo de 10 a 11 MB recebe erro do servidor em vez da mensagem do SPEC | `LimitesTests.ArquivoDe10a11Mb_RecebeMensagemDaAplicacao` |

## 7. Deferred/Waived scenarios

Nenhum cenário foi adiado nem dispensado: os **128 cenários** do SPEC estão cobertos pelas tarefas (conferido por script, sem faltas e sem ids desconhecidos). Itens que **não são cenários** e ficam fora do plano:

| Item | Destino | Motivo |
|---|---|---|
| NFR-23 (Redis, Kafka, réplica, CDN) | Deferred — gatilhos do SPEC | O SPEC responde "Ainda não"; componentes rejeitados no ADR-012 |
| ADR-012 (componentes excluídos) | Sem trabalho de implementação | ADR de rejeição: nenhuma tarefa, por definição |

## 8. Out of scope

Fica para as fases seguintes do fluxo e **não** é feito aqui:

- `/secure` — modelo de ameaças (STRIDE) com este plano como superfície
- `/test` — TestContainers e execução do Playwright (inclui axe-core e larguras)
- `/scan` — SCA e varredura de segredos
- `/infra` — perfil de publicação (`.pubxml`), `web.Production.config` real, script de migrations do pacote de publicação, Docker só de desenvolvimento, verificação externa de saúde
- `/docs` — referência da API e manuais de publicação
- `/deploy` — publicação em homologação e produção, notas de versão
- Execução real da exportação do catálogo contra o GazetaOnline (depende da A5 e de acesso somente leitura)

## 9. Pendências bloqueantes e perguntas em aberto

O `/plan` não preenche lacunas do SPEC (*no invented scope*). Estas ficam registradas e travam o que dizem:

| Id | Pendência | Bloqueia | Dono |
|---|---|---|---|
| **PL-01** | ~~As listas novas dos grupos de campos nunca foram definidas.~~ **Resolvida pelo Product Owner em 2026-10-03:** Tamanho de roupas (PP, P, M, G, GG, XG, XGG); Tamanho de calçados (16 a 33 infantil, 34 a 45 adulto); Gênero (Masculino, Feminino, Unissex, Infantil); Marca em texto livre com autocomplete (`/api/v1/brands/suggest?q=`, até 10 marcas que começam com o texto, a partir das marcas dos anúncios existentes). Listas ampliáveis; a de marcas é dinâmica | Tarefa **2.4** (liberada; a lista de marcas depende da tabela de anúncios, ver `plans/BACKLOG.md`) | Product Owner |
| AR-03 | Versão do SQL Server do SmarterASP e acesso para rodar scripts. O plano assume SQL Server 2016 ou mais novo (colunas calculadas sobre JSON); se a resposta for menor, as tarefas 0.6, 3.1 e 5.4 voltam a ser planejadas | Tarefas 0.6, 3.1, 5.4 (risco) | Product Owner com o SmarterASP |
| AR-06 | Aprovação dos pacotes novos: Identity EF Core, `Serilog.AspNetCore`, `Serilog.Sinks.File`, `Serilog.Formatting.Compact`, Magick.NET Q8 Windows x64, FluentValidation | Tarefas 0.3, 1.1 e 3.4 (cada pacote é pedido antes de entrar) | Product Owner |
| AR-05 | Magick.NET e HEIC na hospedagem compartilhada, provados no ambiente | Tarefa 3.4 (risco; há plano B no ADR-005) | Arquiteto |
| AR-01, AR-02, AR-04, AR-09 | Pasta fora da raiz, runtime do .NET 10, espaço em disco, certificado HTTPS | `/infra` e a primeira publicação | Product Owner com o SmarterASP |
| A5, AR-12, DS-01 a DS-03 | Parecer jurídico do catálogo, domínio e DNS do SendGrid, paleta, fonte e logotipo | **Lançamento** | Product Owner |
| SEC-01, RR-10, AR-09 | `KnownProxies` do SmarterASP (assumido `X-Forwarded-For` até lá), troca do .NET 10 candidata pela estável e certificado HTTPS (`security/PRE_DEV_REVIEW.md`) | **Lançamento** (não bloqueiam o `/build`) | Product Owner |
| AR-10 | Limpeza do texto das regras da US-002 e da US-013 e requisitos de CORS, limite de requisições e health checks no SPEC | Próxima versão do SPEC | Product Owner |
| S7 | Confirmação de SEO | Tarefa 5.6 (se não confirmada, reduz-se a `<title>`) | Product Owner |
