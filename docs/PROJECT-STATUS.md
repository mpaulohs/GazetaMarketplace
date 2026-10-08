# Estado do projeto — GazetaMarketplace v1

> **Em resumo:** o site está **construído, testado e verificado no pacote que vai ao ar**, e a documentação de publicação está pronta. O que falta **não é código**: são ações suas no painel do SmarterASP (certificado, pastas, e-mail do SendGrid, banco) e três conferências com o site já publicado (fotos reais, HTTPS e página de erro). **Nada foi publicado**; a publicação é sua, pelo [`GO-LIVE-CHECKLIST.md`](GO-LIVE-CHECKLIST.md). Data deste retrato: 2026-10-08.

**Para quem é:** o Product Owner. Termos técnicos aparecem só onde são o nome de um arquivo ou comando.

**Onde está cada coisa:** publicar → [`DEPLOY-RUNBOOK.md`](DEPLOY-RUNBOOK.md) e [`GO-LIVE-CHECKLIST.md`](GO-LIVE-CHECKLIST.md) · servidor e variáveis → [`INFRA.md`](INFRA.md) · conferências manuais → [`VERIFY-CHECKLIST.md`](VERIFY-CHECKLIST.md) · testes → `reports/TEST_REPORT.md` · revisão de código → `reports/CODE_REVIEW.md` · segurança → `security/SCAN_REPORT.md` · lista viva de pendências → `plans/BACKLOG.md`.

---

## 1. O que foi construído (Fases 0 a 6)

São **39 tarefas em 7 fases**, todas concluídas, cobrindo **15 histórias de usuário e 130 cenários** da especificação (`specs/SPEC.md`, versão 1.10).

| Fase | O que entrega | Histórias |
|---|---|---|
| **0 — Fundação** | Solução em três projetos (Web, Core, Infrastructure), configuração e segredos fora do git, logs em arquivo com máscara de dados pessoais, erros padronizados, cabeçalhos de segurança e HTTPS, saúde do site, banco com auditoria e controle de edição simultânea, layout e componentes de estado | Base |
| **1 — Equipe e acesso** | Entrar e sair do painel (com bloqueio e sessão), primeiro acesso e Administrador inicial, gerenciar usuários da equipe, recuperar senha por e-mail | US-006, US-007, US-014 |
| **2 — Categorias e catálogo** | Árvore de categorias, 18 grupos de campos por categoria, catálogo de veículos (marca, modelo, ano, versão) com ferramenta de exportação, gerenciar categorias, telefone e WhatsApp do site | US-013, US-015 |
| **3 — Anúncios (equipe)** | Rascunho do anúncio, consulta de CEP com cache, fotos (HEIC e JPEG viram WebP, GPS removido), enviar para revisão, limpeza diária de arquivos | US-008, US-009 |
| **4 — Revisão e ciclo de vida** | Fila de revisão, publicar e rejeitar, despublicar e arquivar, lista de anúncios do painel | US-010, US-011, US-012 |
| **5 — Site público** | Página inicial e categorias, detalhe com galeria, contato por telefone e WhatsApp, busca e filtros, favoritos no navegador, SEO básico | US-001 a US-005 |
| **6 — Verificações transversais** | Acesso de visitante e do painel em toda rota, acessibilidade (nível A e AA) e larguras em 45 telas, orçamentos de desempenho (LCP, INP, CLS e peso) | Qualidade |

**Depois das fases:** `/review` formal do código inteiro, `/verify` no pacote publicado, `/scan` de segurança e `/infra` para a hospedagem SmarterASP (Windows, IIS, SQL Server 2022). O `/deploy` desta etapa é **só documentação**.

## 2. Testes (números finais)

| Suíte | O que prova | Resultado |
|---|---|---|
| Unitários (`Web.Tests`, SQLite em memória) | Regras de negócio, autorização, segurança, telas | **1.803 de 1.803** passam |
| Integração (SQL Server real em contêiner) | Consultas, índices, migrations, transações | **162 de 162** passam |
| Navegador (E2E, contra o site publicado em Production) | Jornadas completas, acessibilidade, larguras | **258 listados: 254 passam, 4 ignorados, 0 falhas** (os 4 ignorados são as métricas de velocidade, que rodam à parte com `GAZETA_VITALS=1` e passaram: 4 de 4) |
| Ferramentas | Exportação do catálogo de veículos; importação de cidades | **43 de 43** e **27 de 27** |
| Mutações | Estraga o código de propósito para ver se algum teste percebe | **22 de 22** mortas na última rodada (os testes pegaram todas) |

Os números são do código do commit `c5a8f68`. Os commits seguintes mudaram só documentação e o arquivo de exemplo de configuração; depois dessa atualização, o subconjunto de testes de configuração (32) foi rodado de novo e passou. Esta etapa (`/deploy`) não alterou nenhum arquivo de `src/` nem de `tests/`.

**Cobertura de código** (medida no `/test`, commit `7f3d3d1`): **98,0% das linhas** (meta 80%) e **91,8% dos ramos** (meta 75%). Não foi medida de novo depois das correções de segurança; o código novo dessas correções tem testes diretos, e a próxima medição está no `reports/TEST_REPORT.md`, que é o lugar do número oficial.

**Rastreabilidade no pacote publicado (`/verify`):** dos 130 cenários, **87 provados no navegador** no pacote e **43 dispensados** por você, com prova em processo (2 deles ainda aguardam o seu "de acordo": US-011-S07 e US-013-S08). Nenhum cenário ficou sem teste.

## 3. Achados que foram corrigidos

| Origem | Achado | Situação |
|---|---|---|
| `/review` | **R-01 (crítico):** chaves de sessão e de links não eram gravadas na pasta persistente; cada reciclagem do IIS podia derrubar a equipe | **Corrigido** e provado com dois sites na mesma pasta |
| `/review` | R-02 e R-02b: Administrador conseguia deixar anúncio Publicado incompleto (e apagar a última foto) | Corrigido |
| `/review` | R-03: cadastro de CEP podia atrapalhar a edição em andamento · R-06: categoria nova não herdava a lista do campo obrigatório | Investigados, **provados como defeito** e corrigidos |
| `/review` | R-04 preço com dígito de outro alfabeto (dava erro 500) · R-05 fuso horário no Windows · R-07 limite de tentativas nunca aplicado · R-08 negação de acesso sem registro · R-09 sessão vencida injetava a tela de login no formulário · R-10 envio com token vencido saía em branco | Corrigidos, cada um com teste que falha sem a correção |
| `/scan` | SC-01 e SC-02: limites de entrada atrás de proxy (lista vazia deixa de travar a equipe; só falhas contam; "esqueci" e "redefinir" têm balde próprio) | Corrigidos |
| `/scan` | SC-03: bloqueio da conta como negação de serviço ao Administrador | Corrigido: bloqueio por conta **e** IP, atraso progressivo e e-mail ao dono a partir do 2º bloqueio |
| `/scan` | SC-04 log saturável · SC-06 senha digitada no campo de e-mail ia ao log · SC-07 foto original com GPS guardada por 30 dias (LGPD) · SC-12 CEP do vendedor no log do `HttpClient` | Corrigidos |
| `/scan` | SC-28: chave antiga do Google Maps no histórico do git | **Exceção assinada por você** (a chave já foi revogada; o histórico não é reescrito) |
| `/verify` e `/infra` | V-01 entrar/esqueci/redefinir dividiam o mesmo balde · V-05 sem perfil Staging · V-06 pacote levava arquivos desnecessários (233 MB → 62 MB) | Resolvidos |
| `/infra` | Banco por script idempotente (sem `Database.Migrate()` ao subir), HTTPS e proteções para qualquer ambiente que não seja de desenvolvimento, DPAPI opcional | Prontos |

**Segurança — retrato:** o `/scan` terminou com 0 crítico, 1 alto condicional (SC-01, já tratado), 4 médios, 10 baixos e 12 informativos. Das 29 ameaças do modelo de ameaças, 24 estão verificadas, 4 parciais e 0 ausentes (1 adiada); dos 21 controles exigidos, 20 estão verificados e 1 parcial (RC-10, que depende do SEC-01 abaixo).

## 4. Pendências

### P0 — antes de divulgar o site (são suas ou só se resolvem com o site no ar)

| Item | O que é | Como resolver |
|---|---|---|
| **SEC-01 (R-11)** | Saber se o site enxerga o IP real de quem visita | Teste prático na primeira publicação: errar a senha e ler o IP no log ([`DEPLOY-RUNBOOK.md`](DEPLOY-RUNBOOK.md) §7.1). A proteção do SC-03 só vale de verdade com o IP real |
| **AR-09** | Certificado HTTPS grátis | Solicitar na aba SSL do painel |
| **Pastas** | `gazeta-fotos`, `gazeta-chaves`, `gazeta-logs` | Criar ao lado da pasta do site |
| **AR-12** | Domínio remetente no SendGrid, com SPF e DKIM | Sem isso o e-mail de redefinição pode cair no spam |
| **SC-28** | Confirmar a chave do Google como revogada | 1 minuto no Google Cloud Console |
| **M6, M7, M10** | Fotos reais e GPS; HTTPS; página 404 e erro sem página branca | Com o site publicado, antes de divulgar ([`VERIFY-CHECKLIST.md`](VERIFY-CHECKLIST.md)) |
| **AR-05** | ImageMagick e HEIC no Windows do provedor | Provado no primeiro envio de foto real (M6) |

### P1 — logo depois do lançamento ou na próxima versão

- **Código (`/fix-issue`):** SC-08 a SC-11 (rotas de fotos, CEP e cidades com senha provisória; `https` obrigatório em qualquer ambiente de produção; pastas só com caminho absoluto; limite global em rotas de arquivos estáticos); R-19 a R-35 (autorização de Administrador também nos serviços, corrida na troca de situação durante o envio de fotos, atomicidade da redefinição de senha, erros do framework fora do formato padrão, entre outros).
- **Operação:** SC-05 e SC-13 (olhar o tamanho de `gazeta-fotos` toda semana; medir memória das fotos na hospedagem); V-02 e V-03 (a mensagem de "muitas tentativas" sai como texto simples; `/Home/Error` abre direto); V-08 e M11 (métricas e e-mail real na hospedagem).
- **Decisão sua:** R-30 (4 pedidos de redefinição por hora com o e-mail do Administrador impedem o e-mail verdadeiro naquela hora) e SC-27 (anúncio de outra pessoa responde 403 e não 404).

### P2 — melhorias, sem pressa

- **`/simplify`:** R-13 (operador `!` em 5 pontos de `src/` e 184 de testes), R-14 a R-18 (contorno de foco, painel de filtros alto demais, recuo da árvore de categorias, favoritos após "voltar", selo largo em 320 px), R-21, R-22, R-55 a R-60, SC-18, SC-20, SC-25.
- **Segurança em profundidade:** SC-14 a SC-17, SC-19, SC-21 a SC-24, SC-26 (tempo do login, teto da sessão, senhas comuns, trilha de auditoria de entrada, cabeçalhos a mais, endurecer o `HttpClient`).
- **Registros e limpeza:** R-12 (FluentValidation foi aprovado e nunca adotado; desvio a documentar), R-64 a R-69, os **177 arquivos `.claude.backup-*`** versionados (remover do git numa tarefa própria), 2 cenários aguardando o seu "de acordo".

A lista completa e atualizada, com arquivo e linha de cada item, está em `plans/BACKLOG.md`.

## 5. O que fica para depois do lançamento

| Quando | O que | Por quê |
|---|---|---|
| **Logo depois de divulgar** | M1 a M5, M8, M9 e M11 do checklist (celular, WhatsApp, leitor de tela, Firefox, Safari, CEP real, pré-visualização do link, e-mail real, sessão) | Decisão sua de 2026-10-07: não seguram o lançamento; se falharem, viram correção |
| **Com o site funcionando** | Ligar o **DPAPI** (`DataProtection__ProtectWithDpapi=true`) | Começa desligado de propósito; ligar só depois do login, da sessão e do link de redefinição funcionando ([`DEPLOY-RUNBOOK.md`](DEPLOY-RUNBOOK.md) §7.2) |
| **Toda semana** | Tamanho da pasta de fotos | Não há cota por usuário nem alerta de disco (SC-05) |
| **Primeiros 7 dias** | LCP p75 e visitas por dia | Gatilhos de CDN, cache e fila (V-07; NFR-23 respondeu "ainda não") |
| **Quando houver tempo** | P1 e P2 acima; carga real do catálogo de veículos (depende do parecer jurídico A5) e de municípios do IBGE | Dependem de você ou de decisões de produto |

## 6. Riscos aceitos (com o seu "de acordo")

| Id | Risco | Registro |
|---|---|---|
| SEC-03 / RR-9 | O site usa a conta única do banco, com permissão total (o provedor não respondeu sobre conta restrita) | `security/SECURITY_REQUIREMENTS.md` §5 |
| SC-28 | Chave antiga do Google Maps no histórico do git (revogada) | `security/SECURITY_REQUIREMENTS.md` §5 |
| RR-2 | Chaves de sessão em arquivo sem criptografia até o DPAPI ser ligado | `docs/INFRA.md` §6 |

## 7. Situação de publicação

**Não publicado.** O pacote é gerado por `dotnet publish ... -p:PublishProfile=IIS-win-x64` (cerca de 62 MB) e a ordem do dia está no [`GO-LIVE-CHECKLIST.md`](GO-LIVE-CHECKLIST.md). A publicação e a divulgação do endereço são suas.
