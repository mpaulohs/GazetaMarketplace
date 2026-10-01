# Pre-Development Security Review: GazetaMarketplace v1

> **Em resumo:** a revisão de segurança antes de programar está **APROVADA pelo Product Owner (2026-09-30), com pendências registradas**. O desenho é seguro para começar a construir: são **15 controles novos**, que entram como critérios de aceite nas tarefas que já existem (nenhuma tarefa nova), e **10 riscos residuais** aceitos, cada um com condição de revisão. O Product Owner decidiu as três perguntas novas (SEC-01 a SEC-03), reconheceu as 10 pendências herdadas do `/arch` e os 4 itens de operação. **Bloqueiam o lançamento (não o `/build`):** AR-09, AR-12, A5, RR-10 e SEC-01.

## Scope
- **Modo:** greenfield, execução por mudança: sistema inteiro da v1, com `plans/plan.md` (39 tarefas) existente, então cada mitigação aponta uma **tarefa real do plano**. Nenhum id foi inventado.
- **Entradas lidas:** `specs/SPEC.md` (Approved v1.2, matriz de permissões e NFR-06 a NFR-19), `architecture/ARCHITECTURE.md` (§7 e §13), ADR-001 a ADR-012, `plans/plan.md`, `.claude/rules/security.md`.
- **Fora do escopo (é do `/scan`):** varredura de código, de dependências e de segredos; qualquer achado que exija ler código-fonte.
- **Status:** **APPROVED** pelo Product Owner em 2026-09-30, com as pendências registradas nas seções abaixo

## OWASP Top 10 (2021) Compliance

| # | Category | Status | Evidence |
|---|----------|--------|----------|
| A01 | Broken Access Control | Addressed | Políticas `Administrador` e `Redator` + checagem de autoria em `AnuncioService` + `WHERE AuthorId` na consulta Dapper do painel + `MatrizDeAcessoTests` (Task 6.1); "somente publicados" em fragmento único (`SqlFragments`); ameaças E1, T5, I3 |
| A02 | Cryptographic Failures | Partial | HTTPS obrigatório (`UseHttpsRedirection`, HSTS), hash do Identity, segredos só em variáveis de ambiente; **chaves do Data Protection sem criptografia em repouso (RR-2)** e TLS definido pelo provedor (AR-09) |
| A03 | Injection | Addressed | EF Core LINQ; Dapper só por `SqlBuilder` (parâmetros nomeados, ordenação por lista permitida); Razor codifica a saída e CSP sem script inline; limite de termo e tempo de consulta (RC-15); ameaças T2, T3 |
| A04 | Insecure Design | Addressed | Este modelo de ameaças (29 ameaças STRIDE), os ADR-003, 004, 005, 009, 011, o aprofundamento da superfície de fotos (RC-1 a RC-9) e o plano com critérios de aceite por controle |
| A05 | Security Misconfiguration | Addressed | **Security headers:** `SecurityHeadersMiddleware` com `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`, CSP e HSTS só em produção (Task 0.4, `CabecalhosTests.TodaResposta_TemOsCabecalhosObrigatorios`). **CORS:** nenhuma política registrada, mesma origem (`CorsTests.Nenhuma_PoliticaCors_Registrada`). Também: `ExceptionHandlingMiddleware` primeiro no pipeline, sem página de exceção fora de Development, configuração validada na partida (`ValidateOnStart`), IP do cliente atrás de proxy (RC-10, depende da SEC-01) |
| A06 | Vulnerable & Outdated Components | Deferred to `/scan` | SCA enforced post-build (`dotnet list package --vulnerable`); atenção especial a Magick.NET (componente nativo) e à versão candidata do .NET 10 (RR-10) |
| A07 | Identification & Authentication Failures | Partial | Política de senha de 8+ com 4 classes, bloqueio de 5 falhas em 15 min, limitador de 5 por 15 min por IP, mensagens genéricas, token de 1 h de uso único, `returnUrl` só local (RC-18); **sem MFA (RR-1, SEC-02)** |
| A08 | Software & Data Integrity Failures | Partial | Antiforgery global, `rowversion` contra decisão simultânea, migrations por script revisado, `SqlBuilder` sem montagem de SQL por texto; **publicação manual sem verificação de integridade do pacote (RR-7)** |
| A09 | Security Logging & Monitoring Failures | Partial | Serilog com mascaramento, `X-Correlation-ID`, `AuditEntries` só de acréscimo e eventos de login (RC-16), `/health/live` e `/health/ready`; **sem alertas automáticos (RR-3)** |
| A10 | Server-Side Request Forgery (SSRF) | Addressed | Únicas chamadas de saída: ViaCEP e SendGrid, ambas com `BaseAddress` fixa; CEP validado como 8 dígitos; nenhuma URL vem do usuário; decodificadores de rede do ImageMagick (URL, HTTP) desligados (RC-2) |

## Controls added beyond ADRs

| RC id | Controle novo | Ameaças | Onde entra no plano |
|-------|---------------|---------|---------------------|
| RC-2 | Decodificação de imagem com limites de recurso e só os decodificadores necessários | D5, E2 | Task 3.4 (critério de aceite acrescentado) |
| RC-4 | Caminho de arquivo montado só com ids numéricos e nomes gerados, e conferido contra a pasta base | I7 | Task 3.4 (critério de aceite acrescentado) |
| RC-6 | Conversões de foto simultâneas limitadas e limite de envio por usuário | D5 | Task 3.5 (critério de aceite acrescentado) |
| RC-10 | IP do cliente correto atrás do proxy da hospedagem (limitadores) | S1 | Task 0.4 (critério de aceite acrescentado) |
| RC-11 | Limite de pedidos de redefinição por e-mail e aviso da cota do SendGrid | D4, S4 | Task 1.4 (critério de aceite acrescentado) |
| RC-12 | Redefinir a senha com sucesso limpa o bloqueio da conta | D1 | Task 1.4 (critério de aceite acrescentado) |
| RC-13 | Resposta de "Esqueci minha senha" que não revela se a conta existe, nem pelo tempo | I2 | Task 1.4 (critério de aceite acrescentado) |
| RC-14 | ViewModels de edição sem campos de decisão (sobrepostagem) | T7 | Task 3.3 (critério de aceite acrescentado) |
| RC-15 | Limites da busca: tamanho do termo e tempo de consulta | D2, T2 | Task 4.4, 5.4 (critério de aceite acrescentado) |
| RC-16 | Registro de toda ação sensível, com valor anterior e novo | E3, R1, S1, T1 | Task 0.6, 1.1, 1.3, 2.6, 2.7 (critério de aceite acrescentado) |
| RC-17 | Nenhum módulo JavaScript insere texto do servidor com innerHTML | T3 | Task 0.7 (critério de aceite acrescentado) |
| RC-18 | Endereço de retorno do login só se for local | S5 | Task 1.1 (critério de aceite acrescentado) |
| RC-19 | Aviso quando as variáveis do primeiro Administrador continuam no servidor | I4 | Task 1.2 (critério de aceite acrescentado) |
| RC-20 | Arquivo de exemplo da configuração de produção com aviso de guarda | I4 | Task 0.2 (critério de aceite acrescentado) |
| RC-21 | Limite global do corpo de requisição, com exceção do envio de foto | D5 | Task 0.4 (critério de aceite acrescentado) |

Os critérios e os testes de cada controle estão em `plans/plan.md`, nas tarefas citadas. **Nenhuma tarefa foi criada.**

## Residual Risks Accepted

| # | Residual risk | Why accepted for v1 | v2 upgrade trigger |
|---|---------------|---------------------|---------------------|
| RR-1 | Sem autenticação em duas etapas e sem pedir a senha atual em ações de alto impacto (S1, S2, E3) | **Aceito pelo PO em 2026-09-30 (SEC-02).** Equipe pequena; login limitado e bloqueado, sessão curta de 30 min, auditoria de toda ação sensível e HTTPS; pedir a senha atual exigiria mudar cenários do SPEC (v1.3) e o ganho é marginal num MVP | Primeiro acesso indevido confirmado nos logs ou em `AuditEntries`, OU o site passar a ter contas de visitantes, OU o PO reabrir a SEC-02 |
| RR-2 | Chaves do Data Protection sem criptografia em repouso (S3) | O plano compartilhado não oferece certificado nem cofre de chaves; a proteção é a pasta fora da raiz e a permissão da identidade do site | Hospedagem que ofereça cofre de chaves ou certificado instalável (ou mudança de hospedagem) |
| RR-3 | Sem alertas automáticos de segurança e de disponibilidade (A09) | Sem Prometheus nem Grafana na hospedagem compartilhada (ADR-010); verificação externa gratuita de `/health/ready` e revisão semanal dos logs de erro | Mais de uma indisponibilidade por mês descoberta por usuários (gatilho do ADR-010) |
| RR-4 | Inundação distribuída (muitos IPs) sem proteção de borda (D3) | O limite por IP e as consultas leves cobrem a escala da v1; CDN e firewall não existem no plano compartilhado; o provedor mitiga volumes extremos | Visitas acima de 5.000 por dia (NFR-23) OU uma indisponibilidade causada por tráfego |
| RR-5 | Fotos com placas e rostos de terceiros (I8) | Depende de decisão do Product Owner (S18); o GPS já é removido de toda foto publicada | Decisão do Product Owner sobre a S18 OU a primeira reclamação de uma pessoa fotografada |
| RR-6 | Administrador desonesto ou comprometido tem o poder total do papel (E3) | É o desenho do SPEC (D-03); a auditoria permite investigar e responsabilizar | Criação da primeira conta de Administrador de alguém que não seja do quadro da Gazeta |
| RR-7 | Publicação manual sem verificação de integridade do pacote (A08) | Uma pessoa publica pelo Visual Studio; o pacote vem do próprio computador dela | Mais de uma pessoa publicando OU adoção de pipeline de CI/CD |
| RR-8 | Quem tem acesso FTP ou ao painel da conta de hospedagem lê logs, originais de foto (30 dias) e chaves (I1, I4, S3) | Limitação do plano compartilhado; mitigada por pasta fora da raiz, retenção curta e autenticação em duas etapas no painel do provedor (escalada ao `/infra`) | Mudança para hospedagem com isolamento por permissão ou com cofre de segredos |
| RR-9 | Conta única do banco com permissão total (T2, I4) | **Aceito pelo PO em 2026-09-30 (SEC-03):** assumido o pior caso até o SmarterASP responder; mitigado por consultas parametrizadas e por o site nunca montar SQL por texto | O provedor oferecer conta separada (reavaliar no `/infra`) OU mudança de hospedagem |
| RR-10 | SDK e runtime do .NET 10 fixados em versão candidata (`10.0.0-rc.2`) | Aceito durante o desenvolvimento; candidata não tem garantia de correção de segurança do produto final. **Bloqueia o lançamento** (PO, 2026-09-30) | **Antes da primeira publicação em produção:** trocar para a versão estável do .NET 10 |

## Open Questions

Reconciliação de `architecture/ARCHITECTURE.md` §13 (cada linha aparece aqui). **"Deferred — ack recebido"** significa que a pergunta continua aberta, mas o Product Owner reconheceu o adiamento em 2026-09-30. O que bloqueia o lançamento e o que não bloqueia o `/build` está na seção "Decisões do Product Owner".

| Id | Disposição | Detalhe |
|----|------------|---------|
| AR-01 | Deferred — ack recebido (PO, 2026-09-30) | Requisitos de segurança da pasta definidos (fora da raiz, só a identidade do site escreve, fora do git e do pacote: S3, I1, I7); o **caminho exato e a permissão** continuam com o Product Owner e o SmarterASP **Não bloqueia o `/build`** (PO, 2026-09-30). |
| AR-02 | Deferred — ack recebido (PO, 2026-09-30) | Não é de segurança em si, mas a versão candidata do runtime tem risco (RR-10): trocar pela estável antes do lançamento **Não bloqueia o `/build`**; a versão candidata é condição de lançamento no RR-10. |
| AR-03 | Deferred — ack recebido (PO, 2026-09-30) | Versão do SQL Server (correções de segurança), conta do banco com permissão mínima (SEC-03, RR-9) e backup (S19) Reconhecida; o Product Owner não classificou o bloqueio (o plano assume SQL Server 2016 ou mais novo). |
| AR-04 | Resolvida pelo /secure | Espaço em disco é disponibilidade (D5): controles RC-6, RC-2 e RC-21 limitam o consumo; monitorar o espaço fica no `/infra` |
| AR-05 | Resolvida pelo /secure | Controles RC-1 e RC-2 e plano B (recusar HEIC) definidos; a prova no ambiente fica na tarefa 3.4 |
| AR-06 | Deferred — ack recebido (PO, 2026-09-30) | Pacotes de segurança listados; cada pacote é auditado no `/scan` (A06); aprovação segue com o Product Owner (Dapper já aprovado) Reconhecida; o Product Owner não classificou o bloqueio (cada pacote é pedido antes de entrar na tarefa). |
| AR-07 | Resolvida antes (/arch) | Aprovada pelo Product Owner em 2026-09-30 |
| AR-08 | Resolvida antes (/arch) | Aprovada pelo Product Owner em 2026-09-30 |
| AR-09 | Deferred — ack recebido (PO, 2026-09-30) | HTTPS é **requisito de segurança e bloqueia o lançamento**: sem certificado não há cookie `Secure` nem HSTS (S2, A02) **Bloqueia o lançamento**; não bloqueia o `/build` (resolução parcial durante a construção). |
| AR-11 | Resolvida antes (/arch) | SPEC v1.2, cenário US-014-S10; relevante para D1 e D4 |
| AR-12 | Deferred — ack recebido (PO, 2026-09-30) | SPF, DKIM e validação do domínio no SendGrid são necessários para o e-mail de redefinição não ser falsificado nem cair em spam (S4); bloqueiam o lançamento **Bloqueia o lançamento** (PO, 2026-09-30). |
| DS-01 a DS-03 | Não é de segurança | Paleta, fonte e logotipo; os contrastes já foram medidos no design system |
| AR-10 | Resolvida pelo /secure | CORS (nenhuma política), health checks (sem detalhes) e registro de ações sensíveis (RC-16) estão definidos aqui; o registro no SPEC fica para a próxima versão |
| A2 | Não é de segurança | A comissão é cobrada fora do sistema |
| A3 | Deferred — ack recebido (PO, 2026-09-30) | Exportação do catálogo: usar só conta somente leitura por variável de ambiente; **a senha antiga do banco do GazetaOnline, que estava em texto no código, precisa ser trocada pelo responsável** (achado crítico da descoberta) Reconhecida; a troca da senha é ação imediata do responsável pelo GazetaOnline (item de operação 3). |
| A4 | Não é de segurança | Resolvida no design system |
| A5 | Deferred — ack recebido (PO, 2026-09-30) | Parecer jurídico do catálogo coletado da OLX; não é risco técnico e **bloqueia o lançamento** **Bloqueia o lançamento** (PO, 2026-09-30). |
| A6 | Não é de segurança | Resolvida; o filtro "somente publicados" vale também para Serviços (I3) |
| A7 | Não é de segurança | Resolvida; a herança de campos não altera permissões |
| S6 | Resolvida pelo /secure | SendGrid com RC-11, RC-12 e RC-13 e redefinição pelo Administrador (US-014-S10) |
| S16, S19 | Deferred — ack recebido (PO, 2026-09-30) | Sessão de 30 min adotada (S2); disponibilidade e backup dependem do provedor (AR-03) Reconhecida; o Product Owner não classificou o bloqueio. |
| S17 | Resolvida pelo /secure | Primeiro Administrador por variáveis de ambiente, com RC-19 (Warning se as variáveis continuarem) |
| S18 | Deferred — ack recebido (PO, 2026-09-30) | Placas e rostos em fotos (I8, RR-5); o GPS já é removido Reconhecida; risco residual RR-5. |
| S28 | Não é de segurança | Preço em centavos inteiros (`bigint`) evita erro de arredondamento |
| S1, S2, S5, S7–S15, S20–S25, S27, S29 | Não é de segurança | Assumidas pelo SPEC; o cookie de sessão é essencial, então a S8 (aviso de cookies) não muda |
| SEC-01 | Assumido; pendência de lançamento | **Decidido pelo PO (2026-09-30):** o IP do cliente vem em `X-Forwarded-For`; `UseForwardedHeaders` com `ForwardedHeaders.XForwardedFor` e `XForwardedProto`; `KnownProxies` a definir depois da resposta do SmarterASP (ticket aberto). **Cuidado para o `/build`:** sem `KnownProxies` ou `KnownNetworks`, o ASP.NET confia só em loopback e o limitador veria o IP do proxy; limpar essas listas para confiar em qualquer origem deixaria qualquer cliente forjar o cabeçalho e escapar do limite. Até a resposta, usar `ForwardLimit = 1` e manter o teste `LimitadorTests.CabecalhoEncaminhado_DeOrigemNaoConfiavel_E_Ignorado`. **Bloqueia o lançamento** |
| SEC-02 | **Decidida pelo PO (2026-09-30): NÃO exigir** | A senha atual do Administrador **não** será pedida ao redefinir a senha de outra pessoa, mudar papel ou trocar o telefone. Motivo do PO: exigiria mudar cenários do SPEC (v1.3) e o ganho é marginal num MVP com poucos usuários. O risco residual **RR-1 fica aceito** |
| SEC-03 | Assumido o pior caso | **Decidido pelo PO (2026-09-30):** assumir uma conta única do banco com permissão total; ticket aberto ao SmarterASP; **RR-9 aceito**; se o provedor oferecer conta separada, reavaliar no `/infra` |

## Itens de operação (não de código)

**Reconhecidos pelo Product Owner em 2026-09-30.** São mitigações operacionais ou de decisão que não cabem como critério de teste de nenhuma tarefa; ficam sob responsabilidade das pessoas indicadas, fora do código:

| # | Item | Quem | Quando |
|---|------|------|--------|
| 1 | Autenticação em duas etapas no painel do provedor, conta de publicação própria e troca dos segredos (banco, SendGrid) quando quem publicava sair; guardar o `web.Production.config` real em cofre de senhas | Product Owner, depois `/infra` | Antes da primeira publicação |
| 2 | Ter **mais de um Administrador ativo** (a regra do SPEC exige só um): evita que o bloqueio da conta única deixe o site sem gestão (D1) | Product Owner | Antes do lançamento |
| 3 | Trocar a senha antiga do banco do GazetaOnline e retirá-la do código e do histórico do git de lá | Responsável pelo GazetaOnline | Imediato |
| 4 | Verificação externa gratuita de `/health/ready` a cada 5 minutos (ADR-010, RR-3) | `/infra` | Antes do lançamento |

## Decisões do Product Owner (2026-09-30)

| Tema | Decisão |
|------|---------|
| SEC-01 (IP do cliente) | Assumir `X-Forwarded-For`; `UseForwardedHeaders` com `XForwardedFor` e `XForwardedProto`; `KnownProxies` depois da resposta do SmarterASP; **pendência de lançamento** |
| SEC-02 (senha atual do Administrador) | **Não exigir**; RR-1 aceito; sem v1.3 do SPEC |
| SEC-03 (conta do banco) | Assumir o pior caso; RR-9 aceito; reavaliar no `/infra` se houver conta separada |
| Pendências herdadas | As 10 reconhecidas |
| Itens de operação | Os 4 reconhecidos |

**Bloqueiam o lançamento (resolver antes do go-live):** AR-09 (certificado HTTPS), AR-12 (SPF e DKIM do SendGrid), A5 (parecer jurídico do catálogo), RR-10 (.NET 10 candidata para estável) e SEC-01 (`KnownProxies` definido).

**Não bloqueiam o `/build`:** AR-01, AR-02, AR-04, AR-09 (resolução parcial durante a construção) e DS-01 a DS-03.

**Reconhecidas sem classificação de bloqueio:** AR-03, AR-06, A3, S16 e S19, S18 (ver a seção Open Questions).

## Approval

| Role | Name | Date | Status |
|------|------|------|--------|
| Security Auditor | Claude | 2026-09-30 | Recomenda aprovar |
| Product Owner | Product Owner | 2026-09-30 | **APPROVED** — com as pendências, os riscos residuais e os itens de operação registrados neste documento |

**Status do documento:** **APPROVED** (Gate 4 fechado em 2026-09-30, com as pendências registradas).
