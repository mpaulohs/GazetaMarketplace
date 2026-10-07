# Verify Report — GazetaMarketplace 6953fa3

- **Artifact digest(s):** `6bb592c8aa261c08c2572b2bbe1e87a0a5bb1c69e625e548c59f2630eb9b83ec` (pasta publicada com `dotnet publish -c Release`, soma das somas SHA-256 dos arquivos; ver `reports/verify-artifact.lock`). O `/deploy` só pode usar este digest; qualquer nova publicação exige refazer este `/verify`.
- **Environment:** `ASPNETCORE_ENVIRONMENT=Production`, HTTPS (certificado autoassinado, `https://localhost:5443`), SQL Server 2022 em contêiner. **Não existe perfil Staging** no projeto: a verificação roda com a configuração de Production, e isso fica registrado como dispensa (dono: `/infra`). Não houve rede de produção real, domínio nem certificado válido: esses itens estão no `docs/VERIFY-CHECKLIST.md`.
- **Date:** 2026-10-07

> **Em resumo:** o artefato publicado **responde, serve o que promete e passou em todos os testes automáticos de navegador (254 de 254 executados; 0 falhas)**, com cabeçalhos de segurança, erros em formato padrão, CORS fechado, limite de login funcionando e as métricas de velocidade muito abaixo dos limites. O **veredito é PASS WITH CONDITIONS**, por três razões: (1) 64 dos 130 cenários da SPEC não têm teste de navegador **marcado** com o id do cenário no artefato (a regra da matriz exige 100%; 41 só têm prova em processo e 23 têm um E2E provável, sem marca); (2) o que só uma pessoa, um aparelho ou a rede de produção prova (celular, WhatsApp, leitor de tela, Firefox, Safari, fotos reais, HTTPS válido, CEP real, pré-visualização de link) está no checklist e **ainda não foi feito**; (3) não há perfil Staging. Nenhuma falha de produto foi encontrada.

## 1. Summary

| Phase | Suite | Pass / Total | Verdict |
|-------|-------|--------------|---------|
| 0 | Trava do artefato (digest, versão, ambiente) | 1 / 1 | PASS (com dispensa: sem perfil Staging) |
| 1 | Liveness (saúde, 9 páginas, leitura do banco) | 12 / 12 | PASS |
| 2 | Contrato HTTP no artefato (cabeçalhos, erros, CORS, rotas de desenvolvimento, limites, fotos, compressão, SEO) | 52 / 52 verificações · 3 observações para o BACKLOG | PASS |
| 3 | E2E de navegador no artefato | 254 / 254 executados; 4 ignorados (métricas) rodados à parte: 4 / 4 | PASS |
| 4 | NFR (25 linhas) | 20 medidas (9 com complemento manual) · 4 verificadas em outro portão · 1 gatilho pós-lançamento · 0 fora do limite | PASS WITH CONDITIONS (ver §4) |
| 5 | Rastreabilidade cenário → teste | 66 / 130 provados no artefato; 23 prováveis; 41 só em processo | **Abaixo de 100%** |

**Gate verdict: PASS WITH CONDITIONS.** Não é `SUCCEEDED` porque a Fase 5 não chegou a 100% e porque os itens manuais ainda não foram feitos. Não é `FAILED`: nada falhou. Promover para produção fica **bloqueado até as condições da §5 serem fechadas ou dispensadas por você**.

Evidências de texto: `reports/verify-artifacts/evidence/` (`phase1-liveness.txt`, `phase2-contract.txt`, `phase2-rate-limits.txt`, `phase3-e2e-summary.txt`, `phase4-vitals-and-weight.txt`, `phase4-logs-and-formats.txt`). O log completo do E2E (`reports/verify-artifacts/report/e2e-full.log`) fica fora do git. O executor de testes não gera `results.json` (OPEN-007, já diferido a `/infra`); a contagem foi conferida contra `--list-tests` (258 listados = 254 + 4).

## 2. Traceability matrix

Veja `reports/VERIFY_MATRIX.md`. **Cobertura: 66 de 130 cenários (50%) com teste de navegador marcado no artefato**; 23 com E2E provável (um arquivo de E2E cita a história e o cenário, mas nenhum teste leva o id); 41 só com prova em processo (SQLite ou SQL Server real, host dentro do `dotnet test`, que passou). Nenhuma dispensa foi dada: **o `/verify` não pode se dispensar sozinho**; as 64 lacunas precisam de marca/teste (`/test`) ou da sua assinatura de dispensa. Observação sobre a leitura do número: a busca é por menção explícita do id, então **subestima** a cobertura real (por exemplo, `US-001-S01` aparece coberto pela jornada de ponta a ponta sem login, mas o teste não leva o id).

## 3. Failures & evidence

Nenhuma falha de teste. Observações do contrato HTTP, todas **sem risco de segurança explorável**, registradas no BACKLOG (V-01 a V-03):

| # | Observação | Evidência | Prioridade |
|---|---|---|---|
| V-01 | Entrar, "esqueci minha senha" e "redefinir senha" **dividem o mesmo balde de 5 pedidos por 15 minutos por IP**: depois de 5 tentativas de login, os outros dois também respondem 429 | `phase2-rate-limits.txt` | P2 |
| V-02 | O 429 de uma página responde **texto simples** ("Muitas tentativas. Tente novamente em alguns instantes."), sem a moldura do site | `phase2-rate-limits.txt` | P2 |
| V-03 | `/Home/Error` abre direto com 200 (a página de erro genérica é alcançável por URL); mesmo caso do `/Home/Status` já em R-47 | `phase2-contract.txt` | P2 |

Aviso importante que **este `/verify` confirmou**: sem a lista de proxies (R-11), a equipe inteira divide o balde de IP; com V-01, quem errou o login 5 vezes também não consegue pedir a redefinição de senha por 15 minutos. Vai no runbook do `/deploy`.

## 4. NFR results

| NFR | Target (spec) | Measured | Status |
|-----|---------------|----------|--------|
| NFR-01 · LCP | < 2,5 s, celular médio, 4G | início 916 ms · categoria 1.068 ms · busca 1.100 ms · detalhe 440 ms (celular emulado, CPU 4× mais lenta, artefato em HTTPS) | **measured** — PASS. Na hospedagem real: item M11.5 |
| NFR-02 · INP | < 200 ms | 56 · 56 · 80 · 64 ms | **measured** — PASS |
| NFR-03 · CLS | < 0,1 | 0,0000 nas quatro páginas; busca com script atrasado 2 s: 0,0043 | **measured** — PASS |
| NFR-04 · servidor p95 | < 500 ms com ~200 anúncios | `VolumeOfV1Tests` passou nesta rodada (limite 500 ms); números mais recentes registrados: p95 34,6 ms (200 anúncios), 52,4 ms (2.000 publicados), `TEST_REPORT` §6.3 | **measured** — PASS (o número desta rodada não aparece no console; o limite foi afirmado pelo teste) |
| NFR-05 · peso | lista ≤ 2 MB, detalhe ≤ 3 MB | lista 171 KB, detalhe 168 KB — **com fotos sintéticas minúsculas** | **measured** com ressalva — o peso com **fotos reais** é o item M6 do checklist (condição) |
| NFR-06 · limite de login | 5 falhas / 15 min / origem | no artefato com limites padrão: 5 pedidos passam, o 6.º recebe 429 com `Retry-After: 900` | **measured** — PASS (ver V-01) |
| NFR-07 · senha | 8+, maiúscula, minúscula, número, símbolo; só hash | — | **not runtime-measurable** — verificado em `/test` (`PasswordTests`, hash do Identity) |
| NFR-08 · sessão | 30 min sem uso | mecanismo provado no artefato (E2E `US006S08`, sessão de 1 min); o valor de 30 min é o padrão do código | **measured** (mecanismo) · valor real: item M11.2 |
| NFR-09 · link de redefinição | 1 h, 1 uso | `RecoveryTokenTests` em processo; E2E de recuperação passou com SendGrid de mentira | **verified at /test**; e-mail real: item M11.1 |
| NFR-10 · cabeçalhos | 5 cabeçalhos em todas as respostas | presentes em 200, 302, 401, 404 e no 429: `x-content-type-options`, `x-frame-options`, `referrer-policy`, `strict-transport-security`, `content-security-policy` | **measured** — PASS. Redirecionamento HTTP→HTTPS é do IIS: item M7 |
| NFR-11 · antiforgery | 100% das escritas | POST sem token em página → 400 com a página de erro (artefato); testes de varredura de todas as rotas em processo | **measured** — PASS |
| NFR-12 · fotos | formatos pelo conteúdo, 10 MB, 20 por anúncio, HEIC convertido | E2E de fotos (JPEG com GPS e HEIC → WebP sem GPS, originais sem rota) passou | **measured** com fotos sintéticas — fotos reais: item M6 |
| NFR-13 · acesso | login e papel em toda ação do painel | `/painel/anuncios` sem sessão → 302 para a entrada; `/api` → 401 ProblemDetails; E2E de acesso passou | **measured** — PASS |
| NFR-14 · segredos | nenhum no código | pasta publicada: 0 `appsettings*.json` com senha, chave ou connection string | **measured** na pasta publicada; varredura do repositório inteiro: `/scan` (próxima etapa) |
| NFR-15 · texto digitado | sempre texto puro | E2E com anúncio de título `<img src=x onerror=…>` passou; nenhuma execução | **measured** — PASS |
| NFR-16 · WCAG 2.1 AA | 0 falhas A/AA, teclado e leitor de tela | axe nível A e AA em **45 telas**: 0 falhas (E2E) | **measured** (automático) · teclado e NVDA: item M3 (condição) |
| NFR-17 · responsividade | sem rolagem lateral em 320, 768, 1024, 1280 | 45 telas × 4 larguras: 0 falhas (E2E) | **measured** — PASS |
| NFR-18 · logs | correlação em toda requisição; sem senha, token nem e-mail em claro | log do artefato: 0 e-mails completos, 0 senhas/códigos, 0 linhas de requisição sem `CorrelationId`; `X-Correlation-ID` em todas as respostas | **measured** — PASS |
| NFR-19 · privacidade | só nome, e-mail e senha (hash) da equipe | — | **not runtime-measurable** — verificado em `/review` (CODE_REVIEW §6) |
| NFR-20 · idioma e formatos | pt-BR, R$, dd/mm/aaaa | `<html lang="pt-BR">`, preço `R$`, data `06/10/2026` | **measured** — PASS |
| NFR-21 · SEO | título/descrição próprios, endereço legível, mapa, arquivado fora do índice | `og:*`, `description` e `canonical` por anúncio, `sitemap.xml` e `robots.txt` 200; E2E de SEO passou (arquivado sai do mapa e vira `noindex`) | **measured** — PASS. Prévia no WhatsApp: item M9 |
| NFR-22 · pilha aprovada | tech-stack e ProblemDetails | — | **not runtime-measurable** — verificado em `/review` (CODE_REVIEW §6) |
| NFR-23 · escala (decisão "ainda não") | reavaliar se LCP p75 > 2,5 s por 7 dias ou > 5.000 visitas/dia | — | **not runtime-measurable** — gatilho de monitoramento depois do lançamento (registrado no BACKLOG) |
| NFR-24 · CEP | 5 s por tentativa, 1 nova tentativa, manual depois, cache 30 dias | E2E de CEP com ViaCEP de mentira passou (tempo, nova tentativa, cache, 404 × 503); **o ambiente não alcança o ViaCEP real** | **measured** (simulado) · CEP real: item M8 (condição) |
| NFR-25 · página 404 e erros | página em português com título e link; status mantido | `/nao-existe` → 404 com título "Página não encontrada"; POST recusado → 400 com "Algo deu errado", nunca em branco | **measured** — PASS |

Resumo: 25 linhas, nenhuma omitida; 20 medidas neste `/verify` (as NFR-01, 05, 08, 09, 10, 12, 16, 21 e 24 têm complemento manual indicado no checklist), 4 verificadas em outro portão (NFR-07 e 09 em `/test`; NFR-19 e 22 em `/review`) e 1 gatilho pós-lançamento (NFR-23). Nenhuma NFR medida ficou fora do limite.

## 5. Gate decision

**PASS WITH CONDITIONS** — promoção bloqueada até que:

1. **Fase 5.** Os 64 cenários sem teste de navegador marcado sejam (a) marcados/escritos no `/test` (item V-04 do BACKLOG) **ou** (b) dispensados por você, por escrito, com o motivo (sugestão: dispensar os 41 que já têm prova em processo na mesma regra de negócio, e exigir marca para os 23 prováveis, que é só trocar o nome do teste).
2. **Itens manuais.** Você faça o `docs/VERIFY-CHECKLIST.md`; bloqueiam a publicação se falharem: **M6 (GPS retirado das fotos)**, **M7 (HTTPS e cabeçalhos na hospedagem)** e **M10 (nunca página branca)**. Os demais viram correções.
3. **Staging.** Dispensa de ambiente registrada: sem perfil Staging, a verificação usou Production (dono: `/infra`).

Recomendação: seguir para o `/scan` (a próxima etapa do seu plano) enquanto você faz o checklist; o `/deploy` só depois do passo 1 e do passo 2.

**Fronteira:** `/verify` não alterou nenhum arquivo de `src/` nem de `tests/`.
