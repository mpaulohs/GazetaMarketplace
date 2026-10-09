# Threat Model: GazetaMarketplace v1

> **Em resumo:** este é o modelo de ameaças do GazetaMarketplace antes de qualquer código. Ele lista o que vale proteger, onde ficam as fronteiras de confiança, **29 ameaças** classificadas pelo método STRIDE (cada uma com a mitigação, o teste que a prova e a tarefa do plano responsável) e a superfície de maior risco: **o envio e o processamento de fotos**. Quem lê: o Product Owner, para aprovar o risco aceito, e a equipe de construção, para saber o que implementar em cada tarefa.

## Document Info
- **Author**: Claude (Security Auditor)
- **Date**: 2026-09-30
- **Status**: Approved (Product Owner, 2026-09-30)
- **Reviewers**: Product Owner (aprovou em 2026-09-30)
- **Modo**: greenfield, execução por mudança sobre o plano `plans/plan.md` (39 tarefas)

## System Overview
Site de classificados com site público (sem login), painel da equipe (Redator e Administrador), SQL Server do provedor, pasta persistente de fotos, ViaCEP, SendGrid e publicação por WebDeploy no IIS compartilhado do SmarterASP.NET. Arquitetura: `architecture/ARCHITECTURE.md`, ADRs 001 a 012 e diagramas em `architecture/diagrams/`. Plano: `plans/plan.md`.

## Assets (feature: GazetaMarketplace v1)

| Asset | Classification | Owner | Notes |
|-------|---------------|-------|-------|
| Credenciais da equipe (hash de senha) e e-mails | Critical | Módulo Equipe e acesso | Hash do Identity; o e-mail é o único dado pessoal do sistema (NFR-19) |
| Cookie de sessão e token de redefinição de senha | Critical | Módulo Equipe e acesso | Quem os tem age como a pessoa até vencerem (30 min deslizantes; 1 h) |
| Chaves do Data Protection | Critical | Infraestrutura | Protegem cookies e tokens antiforgery; ficam em pasta fora da raiz do site |
| Segredos de produção (conexão do banco, chave do SendGrid, senha inicial do Administrador) | Critical | Infraestrutura | Só em variáveis de ambiente do `web.config` publicado |
| Credenciais de publicação e do painel do provedor | Critical | Product Owner | Quem as tem troca o site inteiro; fora do código |
| Telefone/WhatsApp do site (`SiteSettings`) | High | Módulo Configurações | Uma troca indevida desvia todo contato de todos os anúncios |
| Registro de auditoria (`AuditEntries`) e logs | High | Transversal | Prova de quem fez o quê; os logs não podem ter dado sensível |
| Fotos originais com GPS (`_originals/`) | High | Módulo Fotos | Revelam o endereço do vendedor; guardadas 30 dias e nunca servidas |
| Anúncios não publicados (rascunho, em revisão, rejeitado) | Medium | Módulo Anúncios | Informação de negócio; não pode aparecer ao público |
| Anúncios publicados e fotos publicadas | Medium | Módulo Anúncios | Integridade do conteúdo e da reputação do site |
| Catálogo de veículos | Medium | Módulo Catálogo | Origem registrada em `Source` |
| Árvore de categorias e grupos de campos | Medium | Módulo Categorias | Alteração indevida quebra o cadastro e a busca |
| Texto digitado pela equipe (título, descrição, nomes, motivos) | Medium | Módulo Anúncios | Vetor de XSS se for exibido sem codificação |
| Favoritos do visitante (`localStorage`) | Low | Visitante | Só lista de ids; sem dado pessoal |

## Trust Boundaries

```text
  ZONA NÃO CONFIÁVEL                         ZONA CONFIÁVEL (servidor)               DADOS
  ┌────────────────────┐                    ┌──────────────────────────────┐
  │ Visitante          │ ──── HTTPS ──────▶ │ Site público (sem login)     │ ──▶ SQL Server (só leitura lógica)
  │ (qualquer pessoa)  │      FB1           │ limite de requisições, CSP   │
  └────────────────────┘                    └──────────────────────────────┘
  ┌────────────────────┐                    ┌──────────────────────────────┐
  │ Equipe (Redator,   │ ──── HTTPS ──────▶ │ Painel (login, papéis,       │ ──▶ SQL Server (escrita)
  │ Administrador)     │      FB2           │ antiforgery, auditoria)      │ ──▶ Pasta de fotos (fora da raiz)
  └────────────────────┘                    └──────────────┬───────────────┘
                                                           │ FB3
            ┌──────────────────────┬───────────────────────┴──────────────┐
            ▼                      ▼                                      ▼
   ViaCEP (HTTPS, 5 s)     SendGrid (HTTPS, chave)           Biblioteca de imagem (nativa)
   SISTEMA EXTERNO         SISTEMA EXTERNO                   recebe arquivos NÃO confiáveis

  ┌────────────────────┐   WebDeploy    ┌────────────────────────────┐
  │ Máquina de quem    │ ─────────────▶ │ Raiz do site (IIS)         │   FB4
  │ publica + segredos │      FB4       │ pasta persistente separada │
  └────────────────────┘                └────────────────────────────┘
```

| Fronteira | Entre | O que cruza | Ameaças principais |
|---|---|---|---|
| FB1 | Internet ↔ site público | Buscas, páginas, favoritos, fotos publicadas | T2, D2, D3, I3, T3, I6 |
| FB2 | Equipe ↔ painel | Login, formulários, envio de fotos, ações de revisão | S1, S2, S4, S5, T1, T5, T7, E1, R1 |
| FB3 | Servidor ↔ componentes externos e nativos | CEP, e-mail de redefinição, arquivos de imagem | E2, T4, D4, D5, I7 |
| FB4 | Máquina de publicação ↔ servidor | Pacote, configuração de produção, segredos | I4, S3 |

## Matriz de risco (fixa, do modelo)

| L \ I    | Low | Medium | High | Critical |
|----------|-----|--------|------|----------|
| High     | M   | H      | H    | Critical |
| Medium   | L   | M      | H    | H        |
| Low      | L   | L      | M    | H        |

**Regras aplicadas a toda ameaça:** Critical e High → mitigação `[Required for v1]`; Medium → `[Required for v1]` ou `[Deferred to v2 with trigger]`; Low → `[Accepted]` com motivo.

## Threats (STRIDE)

### Spoofing (S)

## Threat: Força bruta e preenchimento de credenciais no login — [ID: S1]

**Category**: S
**Component**: `AccountController.Entrar` (US-006)
**Description**: Um atacante testa muitas senhas, ou senhas vazadas de outros sites, contra os e-mails da equipe.
**Likelihood**: High
**Impact**: High
**Risk**: High

### Attack Scenario
1. Descobre ou adivinha o e-mail de uma pessoa da equipe
2. Tenta milhares de senhas por minuto
3. Entra no painel e publica, rejeita ou altera anúncios

### Mitigations
- [ ] Limitador de 5 tentativas em 15 min por IP na rota de login e de "Esqueci minha senha" (`AddRateLimiter`) (Task 0.4)
- [ ] Bloqueio de conta por 5 falhas em 15 min (`MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 15 min`) e mensagem sempre genérica (Task 1.1)
- [ ] **RC-10** o IP do cliente vem do cabeçalho encaminhado de proxy confiável, para o limite valer por visitante e não por proxy (Task 0.4)
- [ ] **RC-16** falhas e bloqueios de login registrados no log (Task 1.1)

### Acceptance Criteria
- [ ] `RateLimiterTests.SextaTentativaDeLogin_Devolve429` (0.4)
- [ ] teste do cenário `@US-006-S06` (1.1)
- [ ] `RateLimiterTests.IpDoCliente_VemDoCabecalhoEncaminhado_DeProxyConfiavel` (0.4)
- [ ] `AccountTests.FalhaEBloqueioDeLogin_SaoRegistradosNoLog` (1.1)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 0.4, 1.1
**Slot into existing task?**: Yes — critérios novos nas tarefas 0.4 e 1.1

## Threat: Roubo e reuso do cookie de sessão — [ID: S2]

**Category**: S
**Component**: Cookie `.AspNetCore.Identity.Application`
**Description**: O cookie de sessão é copiado (rede insegura, XSS, computador compartilhado) e usado por outra pessoa.
**Likelihood**: Low
**Impact**: Critical
**Risk**: High

### Attack Scenario
1. A pessoa usa o painel num computador compartilhado ou numa rede aberta
2. O atacante obtém o cookie
3. Age como a pessoa até o cookie vencer

### Mitigations
- [ ] Cookie `HttpOnly`, `Secure`, `SameSite=Lax`, expiração deslizante de 30 min, revalidação do `SecurityStamp` a cada 5 min (Task 1.1)
- [ ] HTTPS obrigatório, HSTS em produção e `Content-Security-Policy` sem script inline (reduz o roubo por XSS) (Task 0.4)
- [ ] "Sair" encerra a sessão e o botão Voltar não mostra o painel (Task 1.1)

### Acceptance Criteria
- [ ] `SessionTests.Cookie_Tem_HttpOnly_Secure_SameSite_E_30Min` (1.1)
- [ ] `HeadersTests.Hsts_SoEmProducao` (0.4)
- [ ] `CspTests.Csp_NaoPermiteScriptInline` (0.4)
- [ ] teste do cenário `@US-006-S03` (1.1)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 0.4, 1.1
**Slot into existing task?**: Yes — sem tarefa nova

## Threat: Forja de cookie ou de token antiforgery por vazamento das chaves do Data Protection — [ID: S3]

**Category**: S
**Component**: Pasta `DataProtection__KeysDirectory`
**Description**: Quem lê as chaves do Data Protection consegue fabricar cookies de sessão e tokens válidos.
**Likelihood**: Low
**Impact**: Critical
**Risk**: High

### Attack Scenario
1. Alguém com acesso à conta de hospedagem (FTP, painel) ou a um backup lê a pasta de chaves
2. Fabrica um cookie de Administrador
3. Entra no painel sem senha

### Mitigations
- [ ] A pasta de chaves fica fora da raiz do site e é obrigatória em produção (o site não sobe sem ela); permissão de escrita só para a identidade do site (Task 0.2)
- [ ] A pasta nunca entra no git nem no pacote de publicação (`.gitignore` e `SkipExtraFilesOnServer`) (Task 0.2)
- [ ] Chaves sem criptografia em repouso ficam como risco residual (RR-2); trocar as chaves invalida todas as sessões (Task 0.2)

### Acceptance Criteria
- [ ] `OptionsTests.Producao_SemPastaDeFotos_FalhaNaPartida` (0.2, mesma validação da pasta de chaves)
- [ ] `SecretsTests.Repositorio_NaoContem_ConnectionStringComSenha` (0.2)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 0.2
**Slot into existing task?**: Yes — sem tarefa nova

## Threat: Sequestro de conta pelo fluxo de redefinição de senha — [ID: S4]

**Category**: S
**Component**: `/equipe/esqueci`, `/equipe/redefinir`
**Description**: O token de redefinição é adivinhado, reaproveitado, vaza num e-mail encaminhado ou é usado depois de vencer.
**Likelihood**: Medium
**Impact**: High
**Risk**: High

### Attack Scenario
1. O atacante pede a redefinição para o e-mail de um Administrador
2. Obtém o link (e-mail comprometido ou encaminhado)
3. Define uma senha nova e entra

### Mitigations
- [ ] Token de 1 hora e uso único (o `SecurityStamp` muda ao redefinir) (Task 1.4)
- [ ] A política de senha vale para a senha nova (8+, maiúscula, minúscula, número, símbolo) (Task 1.1, 1.4)
- [ ] Link e token nunca aparecem no log; destinatário mascarado (Task 1.4, 0.3)
- [ ] **RC-11** limite de 3 pedidos por hora por e-mail (Task 1.4)
- [ ] Sessões abertas são encerradas ao redefinir (mudança do `SecurityStamp`) (Task 1.4)

### Acceptance Criteria
- [ ] `TokenTests.Token_ExpiraEmUmaHora` (1.4)
- [ ] testes dos cenários `@US-007-S04` e `@US-007-S05` (1.4)
- [ ] `MaskingTests.Email_ApareceMascarado` (0.3)
- [ ] `RecuperarSenhaTests.QuartoPedidoNaMesmaHora_NaoEnviaEmail_MasRespondeIgual` (1.4)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 0.3, 1.4
**Slot into existing task?**: Yes — critério novo na tarefa 1.4

## Threat: Redirecionamento aberto depois do login (phishing) — [ID: S5]

**Category**: S
**Component**: `AccountController.Entrar` (`returnUrl`, US-006-S07)
**Description**: O link de login carrega um endereço de retorno; se for aceito sem conferir, a vítima cai em site falso depois de entrar.
**Likelihood**: Medium
**Impact**: Medium
**Risk**: Medium

### Attack Scenario
1. O atacante envia um link `/equipe/entrar?returnUrl=https://site-falso`
2. A vítima entra de verdade
3. É levada ao site falso, que pede a senha de novo

### Mitigations
- [ ] **RC-18** `returnUrl` só é aceito se for local (`Url.IsLocalUrl`) (Task 1.1)

### Acceptance Criteria
- [ ] `AccountTests.ReturnUrlExterno_E_Ignorado` (1.1)
- [ ] teste do cenário `@US-006-S07` (1.1)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 1.1
**Slot into existing task?**: Yes — critério novo na tarefa 1.1

### Tampering (T)

## Threat: Troca indevida do telefone/WhatsApp do site — [ID: T1]

**Category**: T
**Component**: `ConfiguracoesController` (US-015)
**Description**: O número é trocado por um número do atacante (conta comprometida ou formulário forjado): todo contato dos visitantes vai para outra pessoa.
**Likelihood**: Low
**Impact**: Critical
**Risk**: High

### Attack Scenario
1. O atacante usa uma sessão de Administrador roubada ou um formulário forjado
2. Troca o número único do site
3. Os visitantes ligam e escrevem ao atacante em todos os anúncios

### Mitigations
- [ ] Só o Administrador acessa a tela (política `Administrador`); antiforgery em todo POST (Task 2.7, 0.4)
- [ ] **RC-16** a troca grava em `AuditEntries` o valor anterior e o novo, com ator e data (Task 2.7)
- [ ] Número validado e normalizado (só dígitos); mudança vale na hora, então o registro precisa existir antes (auditoria acima) (Task 2.7)

### Acceptance Criteria
- [ ] testes dos cenários `@US-015-S01`, `@US-015-S06` (2.7)
- [ ] `ConfiguracoesTests.TrocaDeTelefone_GravaValorAntigoENovo` (2.7)
- [ ] `AntiforgeryTests.PostSemToken_E_Recusado` (0.4)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 0.4, 2.7
**Slot into existing task?**: Yes — critério novo na tarefa 2.7

## Threat: Injeção de SQL em busca, filtros e ordenação (Dapper) — [ID: T2]

**Category**: T
**Component**: `BuscaReadRepository`, `PainelListaReadRepository` (ADR-004)
**Description**: Texto da busca, UF, cidade, preço ou o nome da coluna de ordenação chegam ao SQL sem controle e alteram a consulta.
**Likelihood**: Medium
**Impact**: Critical
**Risk**: High

### Attack Scenario
1. O atacante coloca aspas e `UNION` no termo ou troca `sort=` por um nome de coluna
2. Uma consulta montada por concatenação executa o texto
3. Lê tabelas de contas ou altera dados

### Mitigations
- [ ] Valores sempre como parâmetros nomeados; `SqlBuilder` só aceita fragmentos fixos (Task 0.6)
- [ ] Colunas e direção de ordenação só de uma lista permitida (Task 0.6, 5.4, 4.4)
- [ ] Conta do banco do site com o mínimo de permissões (sem `db_owner` em produção) (Task 0.2)
- [ ] **RC-15** termo limitado a 100 caracteres e consulta com tempo máximo de 10 s (Task 5.4, 4.4)

### Acceptance Criteria
- [ ] `SqlBuilderTests.Valores_SempreViramParametros` (0.6)
- [ ] `SqlBuilderTests.OrdenacaoForaDaLista_EIgnorada` (0.6)
- [ ] `BuscaQueryTests.TermoComAspasEPonto_NaoQuebraNemInjeta` (5.4)
- [ ] `SegurancaTests.Termo_E_Parametro_NaoConcatenado` (5.4)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 0.6, 4.4, 5.4
**Slot into existing task?**: Yes — critério novo nas tarefas 4.4 e 5.4

## Threat: XSS armazenado por textos digitados — [ID: T3]

**Category**: T
**Component**: Título, descrição, nome, motivo de rejeição, nome de categoria
**Description**: Um texto com código é gravado por alguém da equipe (ou por conta comprometida) e executa no navegador de quem vê.
**Likelihood**: Medium
**Impact**: High
**Risk**: High

### Attack Scenario
1. O atacante grava `<script>` ou `<img onerror>` no título ou na descrição
2. O anúncio é publicado e aberto por visitantes
3. O código rouba sessões da equipe ou redireciona visitantes

### Mitigations
- [ ] Codificação automática do Razor; nenhum `Html.Raw` com texto de usuário (Task 0.7)
- [ ] `Content-Security-Policy` sem `unsafe-inline` em scripts (Task 0.4)
- [ ] **RC-17** nenhum módulo JavaScript usa `innerHTML` com texto do servidor (cards de favoritos e mensagens) (Task 0.7)
- [ ] Título e descrição aparecem como texto também em `<title>`, `meta description` e no corpo do WhatsApp (codificados) (Task 5.6, 5.3)

### Acceptance Criteria
- [ ] `XssTests.TextoDeUsuario_EhCodificado_NaoExecuta` (0.7)
- [ ] `XssEmTodasAsTelasTests.Script_EmCadaCampo_ApareceComoTexto` (6.1)
- [ ] `CspTests.Csp_NaoPermiteScriptInline` (0.4)
- [ ] `JsModulesTests.NenhumModuloUsaInnerHtmlComTextoDoServidor` (0.7)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 0.4, 0.7, 6.1
**Slot into existing task?**: Yes — critério novo na tarefa 0.7

## Threat: Arquivo forjado aceito como foto (extensão falsa, poliglota) — [ID: T4]

**Category**: T
**Component**: `AnuncioFotosController.uploadAdPhoto`
**Description**: Um arquivo que não é imagem (ou é imagem e outra coisa ao mesmo tempo) passa como `.jpg`.
**Likelihood**: Medium
**Impact**: Medium
**Risk**: Medium

### Attack Scenario
1. O atacante envia um HTML, SVG ou executável renomeado para `.jpg`
2. O servidor confia na extensão
3. O arquivo é gravado e depois servido ou processado

### Mitigations
- [ ] **RC-1** formato decidido pela assinatura do arquivo, nunca pela extensão (Task 3.4)
- [ ] **RC-3** toda foto é decodificada e regravada como WebP; o arquivo enviado nunca é servido (Task 3.4)
- [ ] Resposta de foto com `Content-Type: image/webp` fixo e `X-Content-Type-Options: nosniff` (Task 3.4, 0.4)

### Acceptance Criteria
- [ ] `FormatoTests.Assinatura_DecideOFormato_NaoAExtensao` (3.4)
- [ ] `FotosSegurancaTests.ArquivoSvgOuMvgComExtensaoJpg_E_Recusado` (3.4)
- [ ] teste do cenário `@US-008-S05` (3.5)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 0.4, 3.4, 3.5
**Slot into existing task?**: Yes — sem tarefa nova

## Threat: CSRF nas ações do painel e nos endpoints JSON — [ID: T5]

**Category**: T
**Component**: Todos os POST, PUT, PATCH e DELETE
**Description**: Uma página de outro site faz o navegador de um Administrador logado enviar uma ação (publicar, desativar, trocar telefone).
**Likelihood**: Medium
**Impact**: High
**Risk**: High

### Attack Scenario
1. O Administrador, logado, abre uma página do atacante
2. A página envia um POST ao painel com o cookie da vítima
3. A ação é executada em nome do Administrador

### Mitigations
- [ ] `AutoValidateAntiforgeryToken` global no MVC; endpoints JSON exigem `RequestVerificationToken` (Task 0.4)
- [ ] Cookie `SameSite=Lax` e nenhuma ação que altera dados por GET (Task 1.1, 6.1)

### Acceptance Criteria
- [ ] `AntiforgeryTests.PostSemToken_E_Recusado` (0.4)
- [ ] `AntiforgeryTests.EndpointJson_ExigeCabecalhoRequestVerificationToken` (0.4)
- [ ] `MatrizDeAcessoTests.TodaRotaDoPainel_ExigeLogin` (6.1)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 0.4, 1.1, 6.1
**Slot into existing task?**: Yes — sem tarefa nova

## Threat: Decisão ou edição simultânea sobrescreve o trabalho de outra pessoa — [ID: T6]

**Category**: T
**Component**: `RevisaoService`, `AnuncioService` (`rowversion`)
**Description**: Dois Administradores (ou o Redator e o Administrador) alteram o mesmo anúncio ao mesmo tempo.
**Likelihood**: Medium
**Impact**: Medium
**Risk**: Medium

### Attack Scenario
1. Dois Administradores abrem o mesmo anúncio em revisão
2. Um publica e o outro rejeita
3. A última gravação vence sem aviso

### Mitigations
- [ ] `rowversion` em `Ads`; conflito vira 409 com mensagem do SPEC (Task 0.6, 4.2)

### Acceptance Criteria
- [ ] `ConcurrencyTests.DoisAdministradores_SoUmDecide` (4.2)
- [ ] teste do cenário `@US-010-S07` (4.2)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 0.6, 4.2
**Slot into existing task?**: Yes — sem tarefa nova

## Threat: Sobrepostagem (mass assignment) de situação, autor ou datas de decisão — [ID: T7]

**Category**: T
**Component**: Formulários de anúncio
**Description**: O corpo do formulário traz campos extras (`Status=Publicado`, `AuthorId`) e o servidor os grava.
**Likelihood**: Medium
**Impact**: High
**Risk**: High

### Attack Scenario
1. Um Redator edita o formulário no navegador e acrescenta `Status=3`
2. O servidor liga o corpo direto à entidade
3. O anúncio é publicado sem revisão

### Mitigations
- [ ] **RC-14** ViewModels próprios sem campos de decisão; o servidor copia só os campos permitidos (Task 3.3)
- [ ] A situação só muda por serviços que conferem papel e transição (`AnuncioService`) (Task 3.1)

### Acceptance Criteria
- [ ] `RascunhoTests.PostComStatusEAutorNoCorpo_NaoAlteraSituacaoNemAutor` (3.3)
- [ ] `ArquiteturaTests.ViewModelsDeEdicao_NaoTemCamposDeDecisao` (3.3)
- [ ] `SituacaoTests.TransicoesValidas_E_Invalidas` (3.1)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 3.1, 3.3
**Slot into existing task?**: Yes — critério novo na tarefa 3.3

### Repudiation (R)

## Threat: Ação sensível sem registro, ou com registro que alguém consegue alterar — [ID: R1]

**Category**: R
**Component**: `IAuditLog`, `AuditEntries`
**Description**: Sem registro confiável, quem publicou, rejeitou, desativou ou trocou o telefone pode negar, e um incidente não é investigável.
**Likelihood**: Medium
**Impact**: High
**Risk**: High

### Attack Scenario
1. Uma ação sensível é executada (por engano ou má-fé)
2. Não há registro, ou o registro pode ser editado
3. Ninguém sabe quem fez nem quando

### Mitigations
- [ ] `AuditEntries` com ator, ação, alvo, data e resultado, por um serviço único (Task 0.6)
- [ ] **RC-16** todas as ações sensíveis registradas: usuários, categorias, telefone, publicar, rejeitar, despublicar e arquivar; login no log (Task 1.1, 1.3, 2.6, 2.7, 4.2, 4.3)
- [ ] `IAuditLog` só acrescenta; nenhuma tela ou endpoint edita ou apaga entradas (Task 0.6)
- [ ] Correlação (`X-Correlation-ID`) em cada linha de log (Task 0.3)

### Acceptance Criteria
- [ ] `AuditLogTests.Registra_AtorAcaoAlvoEData` (0.6)
- [ ] `AuditLogTests.NaoExisteOperacaoParaEditarOuApagarEntradas` (0.6)
- [ ] `AuditingTests.Publicar_E_Rejeitar_RegistramAutorDataEAcao` (4.2)
- [ ] `AuditingTests.RedefinirSenha_RegistraQuemEQuando` (1.3)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 0.6, 1.1, 1.3, 2.6, 2.7, 4.2, 4.3
**Slot into existing task?**: Yes — critérios novos nas tarefas 1.1, 1.3, 2.6, 2.7 e 0.6

### Information Disclosure (I)

## Threat: Vazamento da localização (GPS) pelos originais das fotos — [ID: I1]

**Category**: I
**Component**: `_originals/`
**Description**: O arquivo original, com GPS, fica no disco por 30 dias; se alguma rota ou cópia o expuser, revela onde o vendedor mora.
**Likelihood**: Low
**Impact**: High
**Risk**: Medium

### Attack Scenario
1. Uma rota ou um erro de configuração expõe `_originals/`
2. O atacante baixa um original com GPS
3. Descobre o endereço do vendedor

### Mitigations
- [ ] **RC-5** `_originals/` sem rota nenhuma e apagado em 30 dias (Task 3.4, 3.6)
- [ ] Versões publicadas sem nenhum metadado (Task 3.4)

### Acceptance Criteria
- [ ] `EntregaTests.OriginaisNaoTemRota` (3.4)
- [ ] `MetadadosTests.Gps_NaoSobrevive_NasVersoes` (3.4)
- [ ] `LimpezaTests.ApagaSoOriginaisComMaisDeTrintaDias` (3.6)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 3.4, 3.6
**Slot into existing task?**: Yes — sem tarefa nova

## Threat: Enumeração de contas pelo login e pela recuperação — [ID: I2]

**Category**: I
**Component**: Login, "Esqueci minha senha"
**Description**: Mensagens ou tempos de resposta diferentes revelam quais e-mails têm conta.
**Likelihood**: Medium
**Impact**: Medium
**Risk**: Medium

### Attack Scenario
1. O atacante tenta e-mails em massa
2. Compara mensagens e tempos de resposta
3. Monta a lista de contas para atacar

### Mitigations
- [ ] Mensagem de falha sempre genérica (conta inexistente, senha errada, conta desativada) (Task 1.1)
- [ ] **RC-13** resposta de recuperação igual exista ou não a conta, sem esperar o envio do e-mail (Task 1.4)

### Acceptance Criteria
- [ ] testes dos cenários `@US-006-S04`, `@US-006-S05` (1.1)
- [ ] teste do cenário `@US-007-S03` (1.4)
- [ ] `RecuperarSenhaTests.ContaExistenteEInexistente_TemMesmaRespostaESemEsperarOEnvio` (1.4)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 1.1, 1.4
**Slot into existing task?**: Yes — critério novo na tarefa 1.4

## Threat: Vazamento de anúncio não publicado (consulta sem filtro, foto ou leitura direta) — [ID: I3]

**Category**: I
**Component**: Leituras públicas, `FotosController`
**Description**: Rascunho, anúncio em revisão ou rejeitado aparece no site ou tem a foto acessível ao público.
**Likelihood**: Medium
**Impact**: Medium
**Risk**: Medium

### Attack Scenario
1. Uma consulta Dapper nova esquece o filtro de situação
2. Um anúncio não publicado é listado ou aberto
3. O conteúdo ou as fotos vazam

### Mitigations
- [ ] Fragmento único "somente publicados" nas leituras públicas e teste com um anúncio em cada situação (Task 0.6, 5.4)
- [ ] Detalhe e foto de anúncio não publicado respondem igual a "não existe" (Task 5.2, 3.4)

### Acceptance Criteria
- [ ] `SqlBuilderTests.FragmentoSomentePublicados_E_UnicoEReutilizado` (0.6)
- [ ] `BuscaQueryTests.SoAnunciosPublicados_EmCadaSituacao` (5.4)
- [ ] `IndisponivelTests.NaoPublicado_E_Inexistente_TemAMesmaResposta` (5.2)
- [ ] `EntregaTests.AnuncioNaoPublicado_Devolve404IgualAoInexistente` (3.4)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 0.6, 3.4, 5.2, 5.4
**Slot into existing task?**: Yes — sem tarefa nova

## Threat: Vazamento de segredos (repositório, logs, pacote, arquivo de produção, credenciais de publicação) — [ID: I4]

**Category**: I
**Component**: `web.Production.config`, `appsettings*.json`, logs
**Description**: Conexão com o banco, chave do SendGrid ou a senha do primeiro Administrador vão para o git, para o log ou ficam num computador sem cuidado.
**Likelihood**: Medium
**Impact**: Critical
**Risk**: High

### Attack Scenario
1. Um segredo é commitado, registrado num log ou deixado no pacote
2. Alguém com acesso ao repositório, aos logs ou ao computador de quem publica o lê
3. Usa o banco, o e-mail ou a conta da hospedagem

### Mitigations
- [ ] Configuração só por opções tipadas e variáveis de ambiente; `appsettings.Development.json` fora do git; varredura do repositório por teste (Task 0.2)
- [ ] **RC-20** arquivo de exemplo com aviso de guarda e sem valor real; o arquivo real fica em cofre de senhas, fora do git (Task 0.2)
- [ ] **RC-19** aviso no log se as variáveis do primeiro Administrador continuam no servidor (Task 1.2)
- [ ] Senha inicial, token e chave nunca no log (mascaramento) (Task 0.3, 1.2)
- [ ] Operacional (fora do código): autenticação em duas etapas no painel do provedor, conta de publicação própria e troca dos segredos quando quem publicava sair — **escalado ao `/infra`** (ver §Escalações) (Task 0.2)

### Acceptance Criteria
- [ ] `SecretsTests.Repositorio_NaoContem_ConnectionStringComSenha` (0.2)
- [ ] `SecretsTests.ArquivoDeExemplo_TemAvisoDeGuardaESemValorReal` (0.2)
- [ ] `MaskingTests.Senha_NuncaApareceNoLog` (0.3)
- [ ] `BootstrapAdminTests.SenhaInicial_NaoApareceNoLog` (1.2)
- [ ] `BootstrapAdminTests.VariaveisRemanescentes_RegistramWarning` (1.2)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 0.2, 0.3, 1.2
**Slot into existing task?**: Yes — critérios novos nas tarefas 0.2 e 1.2; a parte operacional vai ao `/infra`

## Threat: Erro com detalhe técnico para o usuário ou dado sensível no log — [ID: I5]

**Category**: I
**Component**: `ExceptionHandlingMiddleware`, Serilog
**Description**: Pilha, texto de SQL ou e-mail completo aparecem na tela ou no log.
**Likelihood**: Medium
**Impact**: Medium
**Risk**: Medium

### Attack Scenario
1. Uma falha inesperada acontece
2. A resposta ou o log traz pilha, SQL ou dados pessoais
3. O atacante aprende a estrutura do sistema

### Mitigations
- [ ] ProblemDetails sem pilha fora de Development; "código de referência" é o `traceId` (Task 0.3)
- [ ] Mascaramento de e-mail, senha e token nos logs (Task 0.3)

### Acceptance Criteria
- [ ] `ErrorsTests.ErroInesperado_NaoExpoePilha_EtrazTraceId` (0.3)
- [ ] `MaskingTests.Email_ApareceMascarado` (0.3)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 0.3
**Slot into existing task?**: Yes — sem tarefa nova

## Threat: Cabeçalhos de segurança ausentes e CORS aberto — [ID: I6]

**Category**: I
**Component**: Todas as respostas HTTP
**Description**: Sem cabeçalhos, o site pode ser embutido em outro (clickjacking), ter conteúdo adivinhado pelo navegador ou aceitar tráfego sem HTTPS; um CORS aberto deixaria outros sites lerem respostas autenticadas.
**Likelihood**: Medium
**Impact**: Medium
**Risk**: Medium

### Attack Scenario
1. Um site do atacante embute o painel num quadro invisível
2. A vítima clica sem perceber
3. Uma ação sensível é executada

### Mitigations
- [ ] Middleware com `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`, CSP e HSTS (só em produção) (Task 0.4)
- [ ] Nenhuma política CORS (mesma origem) (Task 0.4)

### Acceptance Criteria
- [ ] `HeadersTests.TodaResposta_TemOsCabecalhosObrigatorios` (0.4)
- [ ] `CorsTests.Nenhuma_PoliticaCors_Registrada` (0.4)
- [ ] `HeadersTests.Hsts_SoEmProducao` (0.4)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 0.4
**Slot into existing task?**: Yes — sem tarefa nova

## Threat: Travessia de caminho na entrega ou na gravação de fotos — [ID: I7]

**Category**: I
**Component**: `FotosController`, `FileSystemPhotoStorage`
**Description**: Um id ou nome manipulado faz o servidor ler ou apagar arquivos fora da pasta de fotos.
**Likelihood**: Low
**Impact**: High
**Risk**: Medium

### Attack Scenario
1. O atacante pede `/fotos/../../web.config` ou altera um id
2. O caminho é montado a partir do texto recebido
3. O servidor devolve ou apaga um arquivo que não é foto

### Mitigations
- [ ] **RC-4** caminho montado só com ids numéricos e nomes gerados, e conferido contra a pasta base (Task 3.4)
- [ ] Rota com restrição de tipo (`int`) e tamanho (`1600` ou `480`) como enumeração (Task 3.4)

### Acceptance Criteria
- [ ] `EntregaTests.TentativaDeSairDaPastaBase_E_Recusada` (3.4)
- [ ] `EntregaTests.RotaComIdNaoNumerico_Devolve404` (3.4)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 3.4
**Slot into existing task?**: Yes — critério novo na tarefa 3.4

## Threat: Fotos com placas e rostos de terceiros — [ID: I8]

**Category**: I
**Component**: Fotos publicadas (S18)
**Description**: Fotos de veículos e de ruas podem mostrar placas e pessoas que não consentiram.
**Likelihood**: Medium
**Impact**: Medium
**Risk**: Medium

### Attack Scenario
1. A equipe publica uma foto com a placa ou o rosto de alguém
2. A foto fica pública e indexável
3. A pessoa reclama ou o dado é reaproveitado

### Mitigations
- [ ] O GPS é removido de toda foto publicada (ADR-005); cobrir placas e rostos **depende da decisão do Product Owner (S18)** (Task 3.4)

### Acceptance Criteria
- [ ] teste `MetadadosTests.Gps_NaoSobrevive_NasVersoes` (3.4) cobre só o GPS

**Required for v1?**: Deferred to v2 with trigger: decisão do Product Owner sobre a S18 (ou a primeira reclamação de uma pessoa fotografada)
**Owner task(s)**: plan Task 3.4
**Slot into existing task?**: Não se aplica — decisão do Product Owner

### Denial of Service (D)

## Threat: Bloqueio proposital da conta do Administrador — [ID: D1]

**Category**: D
**Component**: Bloqueio de conta do Identity
**Description**: O atacante erra a senha de propósito e mantém o Administrador bloqueado.
**Likelihood**: Medium
**Impact**: High
**Risk**: High

### Attack Scenario
1. O atacante tenta senhas erradas contra o e-mail do Administrador
2. A conta é bloqueada por 15 min
3. Repete a cada desbloqueio, e o Administrador não consegue entrar

### Mitigations
- [ ] Bloqueio de duração fixa (15 min), sem crescimento (Task 1.1)
- [ ] **RC-12** redefinir a senha com sucesso limpa o bloqueio, e a redefinição não depende de entrar (Task 1.4)
- [ ] Existir mais de um Administrador ativo (a regra "sempre ao menos 1" do SPEC não basta): recomendação operacional ao Product Owner (Task 1.3)

### Acceptance Criteria
- [ ] `RecuperarSenhaTests.RedefinirComSucesso_LimpaOBloqueio` (1.4)
- [ ] teste do cenário `@US-006-S06` (1.1)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 1.1, 1.3, 1.4
**Slot into existing task?**: Yes — critério novo na tarefa 1.4

## Threat: Busca pesada no site público (termo longo, varredura) — [ID: D2]

**Category**: D
**Component**: `BuscaReadRepository`
**Description**: Buscas com termos enormes ou muitas combinações mantêm o banco ocupado e derrubam o site compartilhado.
**Likelihood**: High
**Impact**: Medium
**Risk**: High

### Attack Scenario
1. O atacante envia buscas em sequência com termos longos
2. Cada uma varre as colunas normalizadas (`LIKE '%…%'`)
3. O banco fica lento para todos

### Mitigations
- [ ] Limite global de 100 pedidos por minuto por IP no site público (Task 0.4)
- [ ] **RC-15** termo de até 100 caracteres e consulta com tempo máximo de 10 s (Task 5.4)
- [ ] Paginação fixa de 24 e ordem estável (sem tamanho de página escolhido pelo cliente) (Task 5.4)

### Acceptance Criteria
- [ ] `RateLimiterTests.LimiteGlobal_Devolve429AposCemPedidos` (0.4)
- [ ] `BuscaTests.TermoComMaisDe100Caracteres_MostraErroJuntoDoCampo` (5.4)
- [ ] `BuscaQueryTests.ConsultaQueEstouraOTempo_Devolve503SemPilha` (5.4)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 0.4, 5.4
**Slot into existing task?**: Yes — critério novo na tarefa 5.4

## Threat: Inundação distribuída de pedidos ao site público — [ID: D3]

**Category**: D
**Component**: Todo o site público
**Description**: Muitos IPs juntos sobrecarregam o site; o limite por IP não resolve.
**Likelihood**: High
**Impact**: Medium
**Risk**: High

### Attack Scenario
1. O atacante usa muitos IPs
2. Cada IP fica abaixo do limite
3. O site compartilhado fica indisponível

### Mitigations
- [ ] Limite global de 100 pedidos por minuto por IP no site público (Task 0.4)
- [ ] Consultas leves, paginadas e com tempo máximo (busca e listas) (Task 5.4)
- [ ] Proteção de borda (CDN, firewall) não existe no plano compartilhado: risco residual RR-4 (Task 0.4)

### Acceptance Criteria
- [ ] `RateLimiterTests.LimiteGlobal_Devolve429AposCemPedidos` (0.4) cobre só o limite por IP

**Required for v1?**: Yes (parcial): limite por IP e consultas leves são obrigatórios; o que sobra (ataque de muitos IPs, sem proteção de borda no plano compartilhado) é risco residual aceito em RR-4
**Owner task(s)**: plan Task 0.4, 5.4
**Slot into existing task?**: Yes — sem tarefa nova; o risco residual está em RR-4

## Threat: Esgotamento da cota de e-mail por pedidos em massa de redefinição — [ID: D4]

**Category**: D
**Component**: `/equipe/esqueci`, SendGrid (100 e-mails por dia)
**Description**: Pedidos em massa gastam a cota diária gratuita e impedem redefinições legítimas, ou enviam e-mails indesejados a terceiros.
**Likelihood**: High
**Impact**: High
**Risk**: High

### Attack Scenario
1. O atacante dispara pedidos para muitos e-mails
2. O servidor envia um e-mail por pedido existente
3. A cota acaba e a equipe não consegue recuperar a senha

### Mitigations
- [ ] Limitador de 5 pedidos em 15 min por IP (Task 0.4)
- [ ] **RC-11** no máximo 3 pedidos por hora por e-mail e aviso no log quando o total diário chegar a 80 (Task 1.4)
- [ ] Alternativa de redefinição pelo Administrador (US-014-S10) quando o e-mail não chega (Task 1.3)

### Acceptance Criteria
- [ ] `RateLimiterTests.SextaTentativaDeLogin_Devolve429` (0.4, mesma regra para "Esqueci minha senha")
- [ ] `RecuperarSenhaTests.QuartoPedidoNaMesmaHora_NaoEnviaEmail_MasRespondeIgual` (1.4)
- [ ] `RecuperarSenhaTests.TotalDiarioChegaA80_RegistraWarning` (1.4)
- [ ] teste do cenário `@US-014-S10` (1.3)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 0.4, 1.3, 1.4
**Slot into existing task?**: Yes — critério novo na tarefa 1.4

## Threat: Envio em massa de fotos (disco, CPU e memória) — [ID: D5]

**Category**: D
**Component**: `uploadAdPhoto`, conversão de imagem
**Description**: Uma conta comprometida (ou uma pessoa descuidada) envia muitas fotos grandes e esgota o disco, o processador ou a memória do plano.
**Likelihood**: Low
**Impact**: High
**Risk**: Medium

### Attack Scenario
1. Um atacante com sessão da equipe envia centenas de fotos de 10 MB
2. Cada foto exige decodificação e duas conversões
3. O site fica lento ou o disco do plano enche

### Mitigations
- [ ] Limite de fotos por anúncio (20, 6 ou 0) e de 10 MB por arquivo (Task 3.4, 3.5)
- [ ] **RC-6** no máximo 2 conversões simultâneas e 30 envios por minuto por usuário (Task 3.5)
- [ ] **RC-2** limite de 50 milhões de pixels por imagem (Task 3.4)
- [ ] Originais apagados em 30 dias; monitorar o espaço em disco do plano (AR-04) (Task 3.6)
- [ ] **RC-21** corpo de requisição limitado a 1 MB fora do envio de foto (Task 0.4)

### Acceptance Criteria
- [ ] `LimitesTests.TerceiraConversaoSimultanea_EsperaNaFila` (3.5)
- [ ] `LimitesTests.ExcessoDeEnviosPorMinuto_Devolve429` (3.5)
- [ ] `FotosSegurancaTests.ImagemAcimaDoLimiteDePixels_E_Recusada` (3.4)
- [ ] `LimitesTests.Servicos_AceitaSeis_Vagas_AceitaZero` (3.5)
- [ ] `BodyLimitTests.CorpoAcimaDe1Mb_Devolve413_ExcetoNoEnvioDeFoto` (0.4)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 3.4, 3.5, 3.6
**Slot into existing task?**: Yes — critérios novos nas tarefas 3.4 e 3.5

### Elevation of Privilege (E)

## Threat: Redator acessa ou altera o que é do Administrador ou de outro Redator — [ID: E1]

**Category**: E
**Component**: Painel (`AdsController`, `FilaController`, categorias, usuários, configurações)
**Description**: O Redator tenta abrir páginas ou ações exclusivas do Administrador, ou anúncios de outra pessoa, mudando o endereço ou o id.
**Likelihood**: Medium
**Impact**: High
**Risk**: High

### Attack Scenario
1. O Redator troca o id na URL ou digita o endereço de uma página de Administrador
2. O servidor confia só em esconder o botão
3. O Redator publica, edita o anúncio alheio ou vê usuários

### Mitigations
- [ ] Políticas `Administrador` e `Redator` nas áreas e ações; a autoria é conferida no serviço de aplicação, não só no controller (Task 1.1, 3.1)
- [ ] A consulta Dapper da lista do painel filtra pela autoria dentro do SQL (Task 4.4)
- [ ] Matriz automática de acesso por reflexão: toda rota nova do painel precisa entrar na matriz (Task 6.1)
- [ ] Resposta igual ("Você não tem permissão") sem revelar o conteúdo (Task 1.3, 3.3)

### Acceptance Criteria
- [ ] `MatrizDeAcessoTests.Redator_NaoAcessaRotasDeAdministrador` (6.1)
- [ ] `AutoriaTests.Redator_NaoLeAnuncioDeOutro_NoServico` (3.1)
- [ ] `PainelListaQueryTests.Redator_RecebeSoOsProprios_NaConsulta` (4.4)
- [ ] testes dos cenários `@US-006-S10` (1.3) e `@US-008-S10` (3.3)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 1.1, 1.3, 3.1, 3.3, 4.4, 6.1
**Slot into existing task?**: Yes — sem tarefa nova

## Threat: Execução de código ou esgotamento de memória pela biblioteca de imagens — [ID: E2]

**Category**: E
**Component**: Magick.NET / ImageMagick (HEIC) — componentes nativos
**Description**: Um arquivo malformado explora uma falha do decodificador (ou usa um formato perigoso como MVG/SVG/URL) e executa código ou consome toda a memória.
**Likelihood**: Medium
**Impact**: Critical
**Risk**: High

### Attack Scenario
1. O atacante com conta da equipe envia um arquivo especialmente montado
2. O decodificador nativo o processa
3. O atacante executa código no servidor ou derruba o processo

### Mitigations
- [ ] **RC-1** só formatos conferidos pela assinatura entram (Task 3.4)
- [ ] **RC-2** política que liga só JPEG, PNG, GIF, WebP e HEIC/HEIF e desliga MVG, MSL, SVG, URL, HTTP, TEXT e EPHEMERAL; limites de pixels, memória e tempo (Task 3.4)
- [ ] Pacote do Magick.NET atualizado e acompanhado no `/scan` (A06) (Task 3.4)
- [ ] Processo do site com a identidade de menor privilégio do IIS (sem acesso a nada além das pastas do site) (Task 0.2)
- [ ] Se a biblioteca não puder ser protegida na hospedagem, HEIC passa a ser recusado (plano B do ADR-005) (Task 3.4)

### Acceptance Criteria
- [ ] `FotosSegurancaTests.DecodificadoresNaoUsados_EstaoDesligados` (3.4)
- [ ] `FotosSegurancaTests.ImagemAcimaDoLimiteDePixels_E_Recusada` (3.4)
- [ ] `HeicTests.BibliotecaNativaAusente_DevolveMensagemClara` (3.4)
- [ ] `FormatoTests.Heic_E_ConvertidoParaWebP` (3.4)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 0.2, 3.4
**Slot into existing task?**: Yes — critério novo na tarefa 3.4

## Threat: Administrador comprometido ou desonesto — [ID: E3]

**Category**: E
**Component**: Papel `Administrador`
**Description**: Quem tem o papel de Administrador pode publicar, apagar categorias, criar contas e trocar o telefone; uma conta roubada ou uma pessoa de má-fé tem o mesmo poder.
**Likelihood**: Low
**Impact**: Critical
**Risk**: High

### Attack Scenario
1. A conta de um Administrador é roubada (S1, S2, S4) ou a pessoa age de má-fé
2. Usa o poder do papel
3. Altera o site e as contas

### Mitigations
- [ ] Controles que reduzem o roubo da conta: limite de tentativas, bloqueio, sessão curta, HTTPS, CSP (Task 0.4, 1.1)
- [ ] **RC-16** toda ação sensível registrada com ator e data, para investigação e responsabilização (Task 1.3, 2.6, 2.7, 4.2, 4.3)
- [ ] Sem autenticação em duas etapas na v1 (RR-1); **decisão do Product Owner (SEC-02, 2026-09-30): não exigir a senha atual** em ações de alto impacto, o que exigiria mudar cenários do SPEC; risco aceito (Task 1.1)

### Acceptance Criteria
- [ ] `AuditingTests.Publicar_E_Rejeitar_RegistramAutorDataEAcao` (4.2)
- [ ] `AuditingTests.RedefinirSenha_RegistraQuemEQuando` (1.3)
- [ ] `UsuariosTests.CriarMudarPapelDesativarReativar_GravamAuditoria` (1.3)

**Required for v1?**: Yes
**Owner task(s)**: plan Task 0.4, 1.1, 1.3, 2.6, 2.7, 4.2, 4.3
**Slot into existing task?**: Yes — sem tarefa nova

**Resumo dos riscos:** 29 ameaças — Critical 0, High 18, Medium 11, Low 0. Todas as Critical e High têm mitigação `[Required for v1]`; a única ameaça sem mitigação obrigatória na v1 é a I8 (Medium, deferida até a decisão do Product Owner sobre a S18). A D3 (inundação distribuída) tem mitigação obrigatória parcial e o resto é risco residual (RR-4).

## Highest-Risk Active Surface

## Phase 3.5: Highest-Risk Active Surface — Deep Dive

**Surface chosen:** envio e processamento de fotos (upload de arquivo + decodificação de imagem com componentes nativos, incluindo HEIC)
**Reason for choosing:** é a única superfície em que **arquivos não confiáveis** entram numa biblioteca **nativa** (risco de execução de código, E2), consomem disco e processador de um plano compartilhado (D5), carregam GPS de endereços reais (I1) e depois são **servidos ao público** (T4, I7). Quem envia é a equipe (autenticada), o que reduz a probabilidade, mas um login roubado (S1, S2) leva ao mesmo alcance, com impacto até Critical.

| RC id | Control | Threat sub-vector closed | Test in plan task | Source |
|-------|---------|--------------------------|-------------------|--------|
| RC-1 | Formato decidido pela assinatura do arquivo e limite de tamanho (ADR-005) | Extensão falsa, poliglota, HTML/SVG renomeado para `.jpg` (T4) | Task 3.4 / `FormatoTests.Assinatura_DecideOFormato_NaoAExtensao` | ADR-005 |
| RC-2 | Decodificação de imagem com limites de recurso e só os decodificadores necessários | Imagem gigante (pixel flood), decodificadores perigosos (MVG, MSL, SVG, URL), exploração do decodificador (E2, D5) | Task 3.4 / `FotosSegurancaTests.DecodificadoresNaoUsados_EstaoDesligados`, `FotosSegurancaTests.ImagemAcimaDoLimiteDePixels_E_Recusada` | **[NEW]** |
| RC-3 | Toda foto é reprocessada e sem metadados; o arquivo enviado nunca é servido (ADR-005) | Conteúdo malicioso embutido servido ao público; metadados e GPS (T4, I1) | Task 3.4 / `MetadadosTests.Gps_NaoSobrevive_NasVersoes`, `VersoesTests.Gera1600e480_Webp_Qualidade80` | ADR-005 |
| RC-4 | Caminho de arquivo montado só com ids numéricos e nomes gerados, e conferido contra a pasta base | Travessia de caminho, leitura ou exclusão fora da pasta de fotos (I7) | Task 3.4 / `EntregaTests.TentativaDeSairDaPastaBase_E_Recusada` | **[NEW]** |
| RC-5 | `_originals/` sem rota e apagado em 30 dias (ADR-005) | Vazamento de GPS pelos originais (I1) | Task 3.4 / `EntregaTests.OriginaisNaoTemRota`; Task 3.6 / `LimpezaTests.ApagaSoOriginaisComMaisDeTrintaDias` | ADR-005 |
| RC-6 | Conversões de foto simultâneas limitadas e limite de envio por usuário | Esgotar CPU, memória e disco com envio em massa (D5) | Task 3.5 / `LimitesTests.TerceiraConversaoSimultanea_EsperaNaFila`, `LimitesTests.ExcessoDeEnviosPorMinuto_Devolve429` | **[NEW]** |
| RC-7 | Antiforgery, autoria e limite de fotos por grupo no envio (ADR-005, ADR-003) | Envio por quem não pode (E1) e forja de requisição (T5) | Task 3.5 / `AutorizacaoTests.FotoDeAnuncioAlheio_Devolve403`; Task 0.4 / `AntiforgeryTests.EndpointJson_ExigeCabecalhoRequestVerificationToken` | ADR-005, ADR-003 |
| RC-8 | Entrega de foto só para anúncio publicado, com 404 igual e `nosniff` (ADR-005, ADR-004) | Foto de anúncio não publicado visível ao público (I3) | Task 3.4 / `EntregaTests.AnuncioNaoPublicado_Devolve404IgualAoInexistente` | ADR-005, ADR-004 |
| RC-9 | Falha no meio do processamento apaga os arquivos já gravados (ADR-005) | Arquivos parciais e registros órfãos depois de uma falha | Task 3.4 / `FalhaTests.FalhaNoMeio_ApagaArquivosGravados` | ADR-005 |

> `RC-N` é o id estável do controle exigido (sequencial em `security/PRE_DEV_REVIEW.md`). O `/build` implementa por RC-N e o `/review` cita `Relates-to: RC-N`. Os controles `[NEW]` estão repetidos em `PRE_DEV_REVIEW.md` §"Controls added beyond ADRs" com o mesmo número.

## Security Requirements
Ver `security/SECURITY_REQUIREMENTS.md` (gerado do modelo `OWASP_TEMPLATE.md` §B, adaptado à pilha: cookie do Identity no lugar de JWT).

## Approval

| Role | Name | Date | Status |
|------|------|------|--------|
| Security Lead | Claude (Security Auditor) | 2026-09-30 | Recomenda aprovar |
| Tech Lead / Product Owner | Product Owner | 2026-09-30 | Approved |

## Open Issues
- [x] **SEC-01** — IP do cliente atrás do proxy: **assumido `X-Forwarded-For`**; `KnownProxies` a definir após a resposta do SmarterASP (ticket aberto); **pendência de lançamento** (RC-10)
- [x] **SEC-02** — Senha atual do Administrador em ações de alto impacto: **decidido não exigir** (2026-09-30); RR-1 aceito
- [x] **SEC-03** — Conta separada do banco: **assumido o pior caso** (RR-9); ticket aberto; reavaliar no `/infra`
- [ ] Pendências herdadas do `/arch` reconhecidas pelo Product Owner (AR-01, AR-02, AR-03, AR-06, AR-09, AR-12, A3, S16 e S19, S18): ver `PRE_DEV_REVIEW.md` §Open Questions e §Decisões do Product Owner
