# Infraestrutura e pacote de publicação

> **Em resumo:** este documento diz **o que o site precisa do servidor** (SmarterASP, IIS no Windows), **como gerar a pasta que vai para lá**, **como criar o banco com o script do repositório** e **quais variáveis de ambiente configurar**. Quem publica é o Product Owner. O passo a passo da publicação em si, com a lista de conferência final, é o `docs/DEPLOY-RUNBOOK.md` (etapa `/deploy`); aqui fica o que é preciso estar pronto antes.

| O que | Valor |
|---|---|
| Destino | SmarterASP, IIS, Windows, ASP.NET Core 10 em processo (`hostingModel="inprocess"`). Conta `mpaulohs-001`; raiz de arquivos `h:\root\home\mpaulohs-001\www\` |
| Pasta publicada | `src/GazetaMarketplace.Web/bin/publish/win-x64/`, cerca de **62 MB** (eram 233 MB com os componentes de todas as plataformas) |
| Banco | **SQL Server 2022** (o provedor oferece 2022 e 2025; escolhido o 2022, o mesmo dos testes), criado só pelo script `db/scripts/gazeta-idempotente.sql`. Conta única com permissão total (SEC-03, risco aceito RR-9) |
| Contêiner | **Não há.** O destino é o IIS compartilhado; o `trivy` e o `hadolint` do `/scan` não se aplicam (nenhum Dockerfile) |

## 1. Gerar a pasta publicada

Na linha de comando (Windows, Linux ou Mac, com o SDK 10):

```bash
dotnet publish src/GazetaMarketplace.Web -c Release -p:PublishProfile=IIS-win-x64
```

No Visual Studio 2026: botão direito em `GazetaMarketplace.Web` → **Publicar** → perfil **IIS-win-x64**. O perfil está em `src/GazetaMarketplace.Web/Properties/PublishProfiles/IIS-win-x64.pubxml` e não leva servidor, usuário nem senha.

O que o pacote **tem**: os `.dll` do site, o `web.config` gerado (IIS), `wwwroot` (CSS, JS, imagens, fontes) e os componentes nativos do **Windows x64** (ImageMagick e SQL Client).
O que o pacote **não tem** (V-06): `.pdb` (símbolos de depuração), `.xml` de documentação, `appsettings.Development.json` e `web.Production.config.example`. Nada disso é servido ao público, mas também não precisa estar no servidor.

### Web Deploy (Visual Studio 2026)

1. Copie `src/GazetaMarketplace.Web/Properties/PublishProfiles/IIS-WebDeploy.pubxml.example` para `IIS-WebDeploy.pubxml` na mesma pasta.
2. Troque os valores entre parênteses pelos do painel do SmarterASP (**Web Deploy / Publish settings**): servidor de Web Deploy, nome do site, usuário.
3. Publicar → **IIS-WebDeploy**. O Visual Studio pede a senha e a guarda em `IIS-WebDeploy.pubxml.user`, que o git ignora.
4. O Web Deploy envia **só o que mudou** e **não apaga** o que está no servidor e não veio no pacote (`SkipExtraFilesOnServer`): as pastas de fotos, logs e chaves ficam fora da raiz do site e não são afetadas.

## 2. Pré-requisitos no servidor (respostas do SmarterASP, 2026-10-07)

| Item | Situação | O que fazer |
|---|---|---|
| **.NET 10** (runtime e módulo do IIS) | **Instalado** (AR-02). Publicação *framework-dependent*, `win-x64` | Nada. Se o site der erro 500.19 ou 502.5 na primeira publicação, abra chamado citando o runtime 10 |
| **HTTPS** | **Cloudflare** na frente do site (domínio `gzto.com.br`), modo **Full (strict)**, com **Origin CA** no SmarterASP (§5-A). **Não** solicitar o certificado grátis do provedor (decisão de 2026-10-08) | Criar o Origin Certificate no Cloudflare, instalá-lo no painel do SmarterASP e ligar Full (strict). O site redireciona `http` para `https` (308) e envia HSTS; sem origem em HTTPS válido o Cloudflare mostra erro 525/526 |
| **Pastas fora da raiz do site** | Caminho base: `h:\root\home\mpaulohs-001\www\`. O pool do IIS tem **leitura e escrita por padrão** (AR-01) | Criar **três subpastas irmãs do site** (não dentro dele): `gazeta-fotos`, `gazeta-chaves`, `gazeta-logs`. Passo a passo no `docs/DEPLOY-RUNBOOK.md`. Dentro da pasta do site o Web Deploy poderia apagá-las numa publicação |
| **SQL Server** | 2022 e 2025 disponíveis; **escolher a 2022** (AR-03) | Criar o banco na 2022 pelo painel; rodar o script (§3) |
| **Disco** | **30 GB**, limite flexível (AR-04) | Cabe com folga (estimativa de ~2 GB de fotos). O envio de fotos não tem cota por usuário (SC-05): olhe o uso da pasta `gazeta-fotos` toda semana |
| **Usuário do banco** | O provedor não respondeu (SEC-03) | Vale a conta única com permissão total, **risco aceito RR-9** (`security/SECURITY_REQUIREMENTS.md` §5). Reavaliar se o provedor oferecer conta separada |
| Conta SendGrid com **remetente validado**, SPF e DKIM do domínio (AR-12) | Pendente, com o Product Owner | Sem isso o e-mail de redefinição cai no spam |

## 3. Banco de dados: o script idempotente

O site **não** aplica migrations na partida (`Database.Migrate()` não existe no código): um site que sobe tentando alterar o banco de produção é arriscado e pode rodar duas vezes ao mesmo tempo. O esquema vem do script, que você roda **antes** de publicar uma versão nova.

```bash
sqlcmd -S (servidor) -U (usuario) -P (senha) -d (banco) -b -I -i db/scripts/gazeta-idempotente.sql
```

| Opção | Por que |
|---|---|
| `-b` | Para no primeiro erro e devolve código de saída diferente de zero; sem ela o `sqlcmd` segue adiante e você acha que deu certo |
| `-I` | Liga `QUOTED_IDENTIFIER`. O script já começa com `SET QUOTED_IDENTIFIER ON` e os demais `SET` exigidos por índice sobre coluna calculada (`ARITHABORT`, `ANSI_NULLS`, `ANSI_PADDING`, `ANSI_WARNINGS`, `CONCAT_NULL_YIELDS_NULL`, `NUMERIC_ROUNDABORT OFF`), mas o `-I` continua obrigatório como segunda barreira: sem os dois, o `sqlcmd` falha com o **erro 1934** em `CREATE INDEX ... WHERE` (índices filtrados) |
| `-i` | Arquivo de entrada |

- **Idempotente:** pode rodar de novo sem estragar nada; cada migration só se aplica se ainda não estiver em `__EFMigrationsHistory`. Verificado em 2026-10-07 num SQL Server 2022 limpo: 1.ª execução sem `-I`, 2.ª com `-I`, as duas com código de saída 0 e **13 migrations / 20 tabelas**.
- **Gerar de novo** depois de uma migration nova: `bash db/scripts/gerar-script.sh` (usa `dotnet-ef`; reescreve o arquivo; não edite à mão).
- **Antes de produção:** faça um backup do banco, rode o script num banco de **cópia** (ou de homologação) e só depois no de produção.
- O **primeiro Administrador** nasce quando o site sobe com `Bootstrap__AdminEmail` e `Bootstrap__AdminPassword` (e só se ainda não houver Administrador). Remova as duas variáveis logo depois do primeiro acesso (ADR-003, RC-19).

## 4. Variáveis de ambiente (`web.Production.config`)

O arquivo real **não vai para o git**: ele fica no cofre de senhas e na máquina de quem publica. O exemplo é `src/GazetaMarketplace.Web/web.Production.config.example` (só marcadores entre parênteses). Em produção as variáveis ficam em `<aspNetCore><environmentVariables>`.

| Variável | Obrigatória | Valor |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | sim | `Production`. Para um site de homologação, `Staging`: sobe com as **mesmas** proteções (ver §7) |
| `ConnectionStrings__DefaultConnection` | sim | Cadeia do SQL Server, com `Encrypt=True`. Nunca no git |
| `PhotoStorage__BasePath` | sim | `h:\root\home\mpaulohs-001\www\gazeta-fotos` |
| `Logging__FileDirectory` | sim | `h:\root\home\mpaulohs-001\www\gazeta-logs` (arquivo diário, 14 arquivos, cada um de até 100 MB) |
| `DataProtection__KeysDirectory` | sim | `h:\root\home\mpaulohs-001\www\gazeta-chaves`: chaves de sessão e antiforgery. **Sem ela a reciclagem do pool derruba a sessão da equipe e invalida links de redefinição** (R-01) |
| `SendGrid__ApiKey`, `SendGrid__FromEmail` | sim | Chave de API do SendGrid (permissão só de envio) e remetente validado |
| `Site__BaseUrl` | sim | Endereço público **com `https`**, sem barra no fim. Vai nos links dos e-mails; sem ele o link nasceria do cabeçalho `Host`, que um atacante forja |
| `DataProtection__ProtectWithDpapi` | opcional | **Começa `false`.** `true` cifra as chaves com o DPAPI do Windows, mas exige que o pool do provedor tenha o perfil do usuário carregado, o que só se descobre testando (§6). Só no servidor Windows; em outro sistema o site **recusa subir** com a opção ligada |
| `ForwardedHeaders__Cloudflare` | **sim (atrás do Cloudflare)** | `true`: o site confia nas faixas de IP do Cloudflare e lê o IP do visitante de `CF-Connecting-IP`. Padrão `false`. Veja §5 |
| `ForwardedHeaders__KnownProxies__0` | só se o teste do IP mostrar outro proxy | IP exato de um proxy do SmarterASP entre o Cloudflare e o site. Veja §5-B |
| `Bootstrap__AdminEmail`, `Bootstrap__AdminPassword` | só na 1.ª vez | Primeiro Administrador, com troca de senha obrigatória no primeiro acesso. **Remover depois** |
| `HttpsRedirection__HttpsPort` | não | Padrão 443 em produção |

**Nunca defina em produção** `RateLimiting__*`, `SendGrid__BaseUrl`, `ViaCep__BaseUrl` nem `Authentication__SessionMinutes` com valor de teste: existem para a suíte de testes e afrouxam os limites de proteção ou desviam a chave de API (R-39). Se faltar qualquer variável obrigatória, o site **não sobe** e diz qual faltou (validação na partida).

## 5. Cloudflare: HTTPS de origem e IP do visitante (SEC-01, R-11)

> **Em resumo:** o domínio `gzto.com.br` passa pelo Cloudflare (CDN e proxy). Isso muda duas coisas no site: **(A)** o certificado de HTTPS entre o Cloudflare e o SmarterASP é um *Origin CA* do Cloudflare, e **(B)** todo pedido chega ao site com o IP de um servidor do Cloudflare, então o IP do visitante precisa ser lido do cabeçalho `CF-Connecting-IP`. O site faz o item (B) quando `ForwardedHeaders__Cloudflare=true`. Decisão do Product Owner de 2026-10-08; substitui o plano de esperar o IP do proxy do SmarterASP.

### 5-A. HTTPS: Origin CA + Full (strict)

| Etapa | Onde | O que fazer |
|---|---|---|
| 1 | Cloudflare → **SSL/TLS → Origin Server → Create Certificate** | Gerar o certificado (RSA, hosts `gzto.com.br` e `*.gzto.com.br`, validade à escolha). Copie o **certificado** e a **chave privada** na hora: a chave **não** pode ser vista de novo. Guarde as duas no cofre de senhas, **nunca no git** |
| 2 | Sua máquina | Se o painel do SmarterASP pedir um arquivo `.pfx`, converta: `openssl pkcs12 -export -inkey origin-key.pem -in origin-cert.pem -out origin.pfx` (defina uma senha) |
| 3 | SmarterASP → painel → **SSL** | Instalar o certificado de origem no site (importar o certificado próprio). **Não** solicitar o certificado grátis do provedor. *Se o painel não oferecer importação de certificado próprio, abra um chamado antes de seguir; sem isso o modo Full (strict) não fecha.* |
| 4 | Cloudflare → **SSL/TLS → Overview** | Modo **Full (strict)**. **Nunca "Flexible"**: com Flexible o Cloudflare fala `http` com o site, que responde com redirecionamento 308 para `https`, e o navegador entra em laço infinito de redirecionamentos |
| 5 | Cloudflare → DNS | Registros do domínio apontando para o SmarterASP com o proxy ligado (nuvem laranja) |

**Efeitos que você vai ver:** o cadeado do navegador é o certificado do **Cloudflare** (borda); o Origin CA só vale entre o Cloudflare e o site, e **navegador nenhum confia nele**. Acessar o IP do SmarterASP direto, sem passar pelo Cloudflare, mostra aviso de certificado: é esperado. **Erros do Cloudflare:** 525 = falha no aperto de mão com o site (certificado não instalado ou site sem HTTPS); 526 = certificado de origem inválido ou fora do prazo.

**Ajustes recomendados no Cloudflare** (o site usa uma política de segurança de conteúdo estrita, `script-src 'self'`, e nenhum script inserido pelo Cloudflare passa por ela): deixar **desligados** o *Rocket Loader*, a *ofuscação de e-mail* (Email Address Obfuscation) e a injeção automática do *Web Analytics*. Se algum estiver ligado, o console do navegador mostrará bloqueios da política e o item M7 do `docs/VERIFY-CHECKLIST.md` pode falhar. Confira o M7 (cabeçalhos e **um só** `content-encoding`) já pelo domínio final.

### 5-B. IP do visitante: pacote `Cloudflare.ForwardedHeaders` e `CF-Connecting-IP`

**Como funciona (`ForwardedHeaders__Cloudflare=true`):**

1. Na partida o site baixa as faixas oficiais de IP do Cloudflare (`cloudflare.com/ips-v4` e `ips-v6`, até 5 segundos). Se a hospedagem não alcançar o endereço, o pacote usa a **cópia embutida nele** e registra um `Warning` ("Failed to fetch Cloudflare IP ranges. Using embedded fallback").
2. Quando o pedido chega de um endereço dessas faixas, o site troca o IP da conexão pelo valor de **`CF-Connecting-IP`**, que o Cloudflare preenche com o IP do visitante (e sobrescreve se o visitante mandar o seu).
3. Pedido que **não** vem de um endereço do Cloudflare (por exemplo, quem acessa o IP do SmarterASP direto) **não** pode forjar o cabeçalho: ele é ignorado e o IP da conexão vale.
4. O pacote só preenche as faixas confiáveis; **a escolha do cabeçalho `CF-Connecting-IP` é do site** (por padrão o pacote usaria `X-Forwarded-For`).
5. Na partida o log traz `Modo Cloudflare ligado: N faixas de IP do Cloudflare confiáveis`. Se `N` for zero aparece um `Error`: o site passa a ignorar `CF-Connecting-IP`.

**Não é preciso `ForwardedHeaders__KnownProxies__0` para o Cloudflare.** Ele só entra se o teste do IP (abaixo) mostrar **outro proxy do SmarterASP** entre o Cloudflare e o site.

**Limites de tentativas com o IP real** (SC-01, SC-02): com `ForwardedHeaders__Cloudflare=true` o site passa a usar os limites normais.

| Ação | Atrás do Cloudflare (IP real lido) | Modo desligado atrás de um proxy (pior caso) |
|---|---|---|
| Entrar | só **falhas** contam: 5 por 15 min por IP | só falhas: **20** por 15 min por IP |
| "Esqueci minha senha" | balde próprio: 5 por 15 min por IP | balde próprio: **20** |
| "Redefinir senha" | balde próprio: 5 por 15 min por IP | balde próprio: **20** |
| Bloqueio de conta | 5 falhas **do mesmo IP contra a mesma conta**; outro IP segue entrando (SC-03) | sem o IP real todos parecem o mesmo IP: o bloqueio de uma conta vale para todos |
| Aviso na partida | `Information` com o número de faixas | `Warning`: "ForwardedHeaders:KnownProxies não está configurado..." |

**Teste do IP (primeira publicação):** é o §7.1 do `docs/DEPLOY-RUNBOOK.md`. Erre a senha de propósito uma vez e leia no log do dia a linha `Falha de entrada de ... a partir de <IP>`.

| O IP no log é | Significa | O que fazer |
|---|---|---|
| **O seu** | O pacote e o `CF-Connecting-IP` estão funcionando | Nada |
| **De um servidor do Cloudflare** (confira na lista oficial, `cloudflare.com/ips`) | O IP **não** foi trocado: o modo Cloudflare não está valendo | Confira `ForwardedHeaders__Cloudflare=true` (sem isso há o `Warning` de `KnownProxies` na partida), a linha `Modo Cloudflare ligado: N faixas` (com `N` zero há um `Error`), e se o domínio está com a nuvem laranja ligada |
| **Sempre o mesmo e nem o seu nem o do Cloudflare** | Há um proxy do SmarterASP **entre** o Cloudflare e o site | Grave esse IP em `ForwardedHeaders__KnownProxies__0` (um por variável, só o IP) e repita o teste |

**Se nada resolver:** ponha `ForwardedHeaders__Cloudflare=false` e reinicie. O site volta ao modo fail-closed (ignora cabeçalhos de IP, limite temporário de 20 tentativas) e **não divulgue o endereço** antes de resolver.

**Atenção:** a proteção do SC-03 (5 senhas erradas de qualquer rede não travam o Administrador) **só vale de verdade com o IP real do visitante**. Por isso o teste acima se faz antes de divulgar o site.

**Sobre o pacote (decisão de tecnologia, ADR-013):** `Cloudflare.ForwardedHeaders` 1.0.0 (licença MIT, versão única, publicado em 2026-04, autor individual, sem histórico de uso conhecido). O código foi lido antes de entrar: só baixa as listas oficiais, usa a cópia embutida se falhar e preenche as redes confiáveis. Como ele decide **quem pode informar o IP** (e portanto os limites de login), a versão fica **fixa** em `Directory.Packages.props` e qualquer atualização passa por revisão. A alternativa, se ele for abandonado ou der problema, é uma lista de faixas escrita no próprio site (poucas linhas; BACKLOG CF-01).

## 6. Chaves do Data Protection

- As chaves ficam em `DataProtection__KeysDirectory` (XML). Sem criptografia em repouso quem lê a pasta pelo FTP as lê (RR-2).
- **Começa desligado** (`false`). Com `DataProtection__ProtectWithDpapi=true` elas são cifradas com o DPAPI **no escopo do usuário do pool**: copiadas para outra máquina, não abrem. Custo: se a identidade do pool mudar, as chaves antigas deixam de abrir e a equipe precisa entrar de novo (links de redefinição já enviados também deixam de valer). Por isso é **opcional**: ligue só depois que a primeira publicação estiver funcionando (login, sessão, link de redefinição) e confira o resultado: reinicie o site, entre de novo e abra o arquivo novo em `gazeta-chaves` (deve citar `dpapi`). Se der erro por falta de perfil de usuário no pool, volte a `false`: nada se perde além de a equipe entrar de novo.
- Permissão da pasta: escrita **só** para a identidade do pool.

## 7. Staging (V-05)

O site trata como produção **qualquer ambiente que não seja `Development` nem `Testing`**: `Production`, `Staging` ou o nome que a hospedagem usar. O que isso liga: HSTS, remetente real do SendGrid, porta HTTPS 443, log só em arquivo e **validação de toda a configuração na partida** (inclusive `Site__BaseUrl`). Antes valia só o nome exato `Production`, e um site de homologação subiria sem validar o endereço público.

Se o SmarterASP oferecer um segundo site: use `ASPNETCORE_ENVIRONMENT=Staging`, **outro banco**, outras pastas de fotos/logs/chaves e outro `Site__BaseUrl`. Nunca aponte homologação para o banco ou as pastas de produção. Se não houver segundo site, a dispensa registrada no `/verify` continua valendo.

## 8. Fuso horário (R-05)

O site exibe datas em São Paulo. Procura o fuso `America/Sao_Paulo` e, no Windows sem ICU, `E. South America Standard Time`. Se nenhum dos dois existir no servidor, usa **UTC−03:00 fixo** (o Brasil não tem horário de verão desde 2019) em vez de derrubar toda tela com data. Não precisa configurar nada; a conferência com a hospedagem real continua no `docs/VERIFY-CHECKLIST.md`.

## 9. Segredos no histórico do git (SC-28)

O `gitleaks` acusa 30 ocorrências de uma única chave de API do Google Maps em dois commits antigos (`a3521ae0`, `2ee43b89`). A chave **já foi revogada no Google Cloud Console**, o histórico **não é reescrito** e a exceção está registrada em `security/SECURITY_REQUIREMENTS.md` §5. O `.gitleaksignore` na raiz faz o `/scan` ignorar só esses 30 achados; qualquer segredo novo continua sendo acusado. **Antes de publicar**, confirme no console do Google que a chave aparece como revogada.

## 10. Conferência rápida depois de subir

| Verificação | Esperado |
|---|---|
| `https://(site)/health/ready` | `Healthy` (banco acessível e migration em dia) |
| `https://(site)/health/live` | `Healthy` |
| Cabeçalhos da página inicial | `Strict-Transport-Security`, `Content-Security-Policy`, `X-Content-Type-Options` |
| Logs na pasta `Logging__FileDirectory` | arquivo do dia, com a linha de início e `Modo Cloudflare ligado: N faixas de IP do Cloudflare confiáveis` (§5-B). Um `Warning` de `KnownProxies` significa que o modo Cloudflare não está ligado |
| Entrar com o Administrador do primeiro acesso | pede a troca de senha; depois **remover** `Bootstrap__*` |
