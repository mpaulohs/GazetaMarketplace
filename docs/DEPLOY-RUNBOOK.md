# Runbook de publicação — GazetaMarketplace no SmarterASP

> **Em resumo:** este é o passo a passo para **você** (Product Owner) publicar o site na hospedagem, com os dados reais da conta: o tipo de pacote, o banco, o certificado, as três pastas e as variáveis. **A publicação é sua**; nada aqui foi publicado por quem escreveu o site. No dia, siga o [`GO-LIVE-CHECKLIST.md`](GO-LIVE-CHECKLIST.md) (uma página, na ordem). Os pontos que ainda dependem de você estão no fim, em "Pendências antes de divulgar o site".
> Detalhes e motivos de cada decisão: [`INFRA.md`](INFRA.md). Estado geral do projeto: [`PROJECT-STATUS.md`](PROJECT-STATUS.md).

## 1. Dados da conta (respostas do SmarterASP, 2026-10-07)

| Item | Valor | Origem |
|---|---|---|
| Raiz de arquivos da conta | `h:\root\home\mpaulohs-001\www\` | AR-01 |
| Permissão das pastas | O pool do IIS tem leitura e escrita por padrão | AR-01 |
| .NET 10 | Instalado no servidor | AR-02 |
| SQL Server | 2022 e 2025 disponíveis; **usar a 2022** | AR-03 |
| Disco | 30 GB, limite flexível | AR-04 |
| HTTPS | **Cloudflare** (domínio `gzto.com.br`): modo **Full (strict)** com **Origin CA** instalado na aba **SSL** do painel. **Não** solicitar o certificado grátis do provedor | AR-09, decisão de 2026-10-08 |
| Usuário do banco | Sem resposta: **conta única com permissão total, risco aceito (RR-9)** | SEC-03 |
| IP do visitante | O site lê `CF-Connecting-IP` (pacote `Cloudflare.ForwardedHeaders`, `ForwardedHeaders__Cloudflare=true`); confirmar com o teste do §7.1 | SEC-01 |

## 2. Pacote: framework-dependent, win-x64

O pacote leva só o código do site e os componentes nativos do Windows x64 (cerca de **62 MB**); o .NET 10 já está no servidor, então **não é autocontido**.

```bash
dotnet publish src/GazetaMarketplace.Web -c Release -p:PublishProfile=IIS-win-x64
```

A pasta sai em `src/GazetaMarketplace.Web/bin/publish/win-x64/`. No Visual Studio 2026: **Publicar → IIS-WebDeploy** (copie antes o `IIS-WebDeploy.pubxml.example`; passo a passo no [`INFRA.md`](INFRA.md) §1). O pacote **não** leva `.pdb`, `.xml`, `appsettings.Development.json` nem o arquivo de exemplo de configuração.

## 3. Pastas (uma vez, antes da primeira publicação)

Crie **três subpastas dentro de `h:\root\home\mpaulohs-001\www\`, irmãs da pasta do site** (no gerenciador de arquivos do painel do SmarterASP ou pelo FTP):

| Subpasta | Variável | Guarda |
|---|---|---|
| `gazeta-fotos` | `PhotoStorage__BasePath` | Fotos dos anúncios (as versões servidas e, por até 30 dias, os originais em `_originals`) |
| `gazeta-chaves` | `DataProtection__KeysDirectory` | Chaves de sessão e antiforgery. **Sem elas a reciclagem do site derruba a sessão da equipe** |
| `gazeta-logs` | `Logging__FileDirectory` | Logs do site (14 arquivos de até 100 MB) |

**Irmãs do site, não dentro dele:** o Web Deploy pode apagar o que não veio no pacote. O perfil `IIS-WebDeploy` já mantém `SkipExtraFilesOnServer`, mas fora da pasta do site não depende disso.
**Como conferir depois da primeira subida:** as três pastas passam a ter arquivos (um log do dia em `gazeta-logs`, um `key-*.xml` em `gazeta-chaves`) sem você ter dado nenhuma permissão a mais.

## 4. Banco: SQL Server 2022

1. No painel do SmarterASP, crie o banco na **versão 2022** e anote servidor, nome do banco, usuário e senha.
2. Faça um **backup** (ou confirme o backup automático do plano).
3. Rode o script do repositório, **antes** de publicar uma versão nova:

```bash
sqlcmd -S (servidor) -U (usuario) -P (senha) -d (banco) -b -I -i db/scripts/gazeta-idempotente.sql
```

- `-b` para no primeiro erro; `-I` liga `QUOTED_IDENTIFIER`. O script também declara `ARITHABORT ON` e os outros `SET` que índice sobre coluna calculada exige. Sem `-I` e sem esses `SET`, o `sqlcmd` falha com o erro 1934. O script é **idempotente**: pode rodar de novo.
- Se o `sqlcmd` não estiver na sua máquina, use a ferramenta de SQL do painel (cole o conteúdo do arquivo; ela precisa aceitar o `GO`).
- O site **não** altera o banco ao subir (`Database.Migrate()` não existe).
- A cadeia de conexão vai na variável `ConnectionStrings__DefaultConnection` (nunca no git), com `Encrypt=True`.

## 5. HTTPS com o Cloudflare (Origin CA + Full strict)

O endereço público passa pelo Cloudflare. O navegador vê o certificado do **Cloudflare**; entre o Cloudflare e o SmarterASP vale um **Origin Certificate** (Origin CA) que você cria no Cloudflare. Explicação completa e o que fazer se algo falhar: [`INFRA.md`](INFRA.md) §5-A.

1. **Cloudflare → SSL/TLS → Origin Server → Create Certificate.** Hosts `gzto.com.br` e `*.gzto.com.br`. Copie o **certificado** e a **chave privada** agora (a chave não aparece de novo) e guarde-os no cofre de senhas, **nunca no git**.
2. Se o painel do SmarterASP pedir `.pfx`, converta: `openssl pkcs12 -export -inkey origin-key.pem -in origin-cert.pem -out origin.pfx`.
3. **Painel do SmarterASP → aba SSL:** instale o certificado de origem no site. **Não solicite o certificado grátis do provedor.** Se o painel não tiver onde importar certificado próprio, abra chamado antes de seguir: sem isso o Full (strict) não fecha.
4. **Cloudflare → SSL/TLS → Overview:** modo **Full (strict)**. **Nunca "Flexible"**: causa laço infinito de redirecionamentos, porque o site redireciona `http` para `https`.
5. **Cloudflare → DNS:** registros apontando para o SmarterASP com a nuvem laranja ligada.
6. Recomendado no Cloudflare: *Rocket Loader*, *Email Address Obfuscation* e a injeção automática do *Web Analytics* **desligados** (o site bloqueia scripts que não são dele; [`INFRA.md`](INFRA.md) §5-A).

**Como conferir:** `https://gzto.com.br` abre sem erro 525 ou 526 (525 = o aperto de mão com o site falhou; 526 = certificado de origem inválido) e o painel do Cloudflare mostra **Full (strict)**. Acessar o IP do SmarterASP direto mostrar aviso de certificado é esperado (o Origin CA só vale para o Cloudflare). `Site__BaseUrl` = `https://gzto.com.br` (ou com `www`, se esse for o endereço principal), com `https://` e sem barra no fim. O site redireciona `http` para `https` (308) e envia HSTS; os cookies de sessão só viajam por HTTPS.

## 6. Variáveis de ambiente

Copie `src/GazetaMarketplace.Web/web.Production.config.example` para `web.Production.config` (fora do git, no cofre de senhas) e troque cada valor entre parênteses. As três pastas já vêm com os caminhos reais. Tabela completa e o que **nunca** definir em produção: [`INFRA.md`](INFRA.md) §4.

Em resumo: `ASPNETCORE_ENVIRONMENT=Production`, `ConnectionStrings__DefaultConnection`, as três pastas, `SendGrid__ApiKey`, `SendGrid__FromEmail`, `Site__BaseUrl`, `ForwardedHeaders__Cloudflare=true`, `DataProtection__ProtectWithDpapi=false` (veja o §7 do `INFRA.md` antes de mudar) e, **só na primeira vez**, `Bootstrap__AdminEmail` e `Bootstrap__AdminPassword` (remover depois do primeiro acesso).

## 7. Ordem de uma publicação

1. Backup do banco.
2. Rodar o script do banco (§4).
3. Publicar o pacote (§2).
4. Abrir `https://(site)/health/ready` → `Healthy`.
5. Primeira vez: entrar com o Administrador, trocar a senha provisória, **remover `Bootstrap__*`** do `web.Production.config` e reiniciar.
6. Conferir os logs em `gazeta-logs` (linha de início do dia; veja o aviso do §8).
7. **Teste prático do IP** (primeira publicação, §7.1).
8. **Só depois que o site funcionar**, decidir sobre o DPAPI (§7.2).

### 7.1 Teste prático do IP (primeira publicação, atrás do Cloudflare) — atualizado em 2026-10-08

**Para que serve:** conferir que o site enxerga o IP **real** de quem visita. Atrás do Cloudflare, sem a leitura do cabeçalho `CF-Connecting-IP`, todo visitante chegaria com o IP de um servidor do Cloudflare, e os limites de tentativas e o bloqueio por conta+IP valeriam para o site inteiro. O pacote `Cloudflare.ForwardedHeaders` (ligado por `ForwardedHeaders__Cloudflare=true`) cuida disso; este teste prova que cuidou.

1. **Conferir a partida:** no log do dia em `gazeta-logs` procure `Modo Cloudflare ligado: N faixas de IP do Cloudflare confiáveis`. Se aparecer um `Warning` de `KnownProxies`, a variável `ForwardedHeaders__Cloudflare` não está `true`. Se aparecer um `Error` com "nenhuma faixa", a lista não carregou. Um `Warning` "Failed to fetch Cloudflare IP ranges" **não é problema**: o pacote usou a cópia embutida.
2. Abra `https://gzto.com.br/painel/entrar` **pelo endereço público** (passando pelo Cloudflare) e **erre a senha de propósito uma vez**.
3. No mesmo log, procure a linha **`Falha de entrada de ... a partir de <IP>`**.
4. Compare o `<IP>` com o **seu IP** (pesquise "meu IP" no navegador, na mesma rede).

| O IP na linha é | O que significa | O que fazer |
|---|---|---|
| **O seu** | O site leu o `CF-Connecting-IP`: **o pacote resolve** | Nada. Os limites normais (5 por 15 minutos) já estão valendo |
| **De um servidor do Cloudflare** (confira na lista oficial em `cloudflare.com/ips`) | O IP **não** foi trocado: o modo Cloudflare não está valendo | Revise o passo 1; confirme que o domínio está com a nuvem laranja ligada; corrija e repita o teste |
| **Sempre o mesmo, e nem o seu nem o do Cloudflare** | Há um proxy do SmarterASP **entre** o Cloudflare e o site | Grave esse IP em `ForwardedHeaders__KnownProxies__0` (uma variável por IP, só o IP, sem faixa nem texto), reinicie e repita: o site passa a aceitar o `CF-Connecting-IP` também vindo desse proxy |
| Não achou a linha | Log em outra pasta, ou o erro de senha não chegou ao site | Confira `Logging__FileDirectory` e repita o erro |

**Se não resolver:** ponha `ForwardedHeaders__Cloudflare=false` e reinicie. O site volta ao modo seguro de antes (ignora cabeçalhos de IP; entrar aceita 20 tentativas por 15 minutos para o site inteiro) e **você não divulga o endereço** até resolver. Detalhes: [`INFRA.md`](INFRA.md) §5-B.

### 7.2 DPAPI (criptografia das chaves de sessão) — decisão de 2026-10-07

**Na primeira publicação o DPAPI fica desligado:** `DataProtection__ProtectWithDpapi=false` (já é o valor do `web.Production.config.example`). **Só ligue depois que o site funcionar** (login, sessão, link de redefinição).

Para ligar depois:

1. Mude a variável para `true` e reinicie o site.
2. Entre no painel de novo (a sessão antiga pode cair; é esperado).
3. Peça uma redefinição de senha para o seu e-mail e abra o link.
4. Confira em `gazeta-chaves` que existe um arquivo `key-*.xml` **novo** e que ele cita `dpapi`.

**Se der erro ou o site não subir:** volte a variável para `false` e reinicie. Nada se perde além de a equipe precisar entrar de novo e de links de redefinição já enviados deixarem de valer. O motivo (chaves ligadas à identidade do pool): [`INFRA.md`](INFRA.md) §6.

## 8. Pontos que ainda são seus

| Item | O que é | Situação |
|---|---|---|
| **SEC-01 — IP do visitante** | O SmarterASP indicou um artigo sobre **IISNode**, que não vale para ASP.NET Core. Com o Cloudflare na frente a questão muda: o IP real vem de `CF-Connecting-IP` | **Resolvido no código** (pacote `Cloudflare.ForwardedHeaders`, `ForwardedHeaders__Cloudflare=true`) e **confirmado pelo teste do §7.1** na primeira publicação. Se o teste mostrar outro proxy do SmarterASP, é só acrescentar o IP dele em `ForwardedHeaders__KnownProxies__0` |
| **SEC-03 — conta do banco** | Sem resposta do provedor: o site usa a conta única com permissão total | **Risco aceito (RR-9)** em `security/SECURITY_REQUIREMENTS.md` §5 |
| **SC-28 — chave do Google Maps no histórico do git** | Uma chave de API antiga aparece em dois commits de templates | **Exceção assinada.** A chave já foi revogada no Google Cloud Console; o histórico não é reescrito. Antes de publicar, confirme no console que ela aparece como revogada |
| **AR-12 — e-mail** | Domínio remetente validado no SendGrid, com SPF e DKIM, para o e-mail de redefinição não cair no spam | Pendente (Product Owner) |
| **AR-05 — imagens no Windows do provedor** | O ImageMagick (inclusive HEIC) precisa carregar no servidor | Conferir no primeiro envio de foto real (`docs/VERIFY-CHECKLIST.md`, M6) |
| **SC-05 — disco** | Não há cota por usuário para fotos | Olhar o tamanho de `gazeta-fotos` toda semana |

## 9. Pendências antes de divulgar o site

Os itens bloqueantes do `docs/VERIFY-CHECKLIST.md` são **M6 (fotos reais e GPS), M7 (HTTPS) e M10 (nunca página branca)**; os demais ficam para depois. Faça M6, M7 e M10 logo depois de publicar e **antes** de divulgar o endereço. A lista final de lançamento e a promoção são fechadas na etapa `/deploy`.
