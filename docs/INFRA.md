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
| **HTTPS** | Certificado **grátis** do provedor (AR-09) | **Solicitar no painel, aba SSL**, antes de divulgar o site. O site redireciona `http` para `https` (308) e envia HSTS; sem o certificado válido o HSTS trava o navegador |
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
| `ForwardedHeaders__KnownProxies__0` | **pendente (SEC-01)** | IP do proxy do SmarterASP. Veja §5 |
| `Bootstrap__AdminEmail`, `Bootstrap__AdminPassword` | só na 1.ª vez | Primeiro Administrador, com troca de senha obrigatória no primeiro acesso. **Remover depois** |
| `HttpsRedirection__HttpsPort` | não | Padrão 443 em produção |

**Nunca defina em produção** `RateLimiting__*`, `SendGrid__BaseUrl`, `ViaCep__BaseUrl` nem `Authentication__SessionMinutes` com valor de teste: existem para a suíte de testes e afrouxam os limites de proteção ou desviam a chave de API (R-39). Se faltar qualquer variável obrigatória, o site **não sobe** e diz qual faltou (validação na partida).

## 5. IP do proxy (SEC-01, R-11): decisão pendente do Product Owner

**Resposta do SmarterASP (2026-10-07):** o provedor indicou o artigo <https://www.smarterasp.net/support/kb/a2314/how-to-get-remote-ip-address-with-iisnode.aspx>.
**O KB do SmarterASP é para IISNode (Node.js atrás do IIS). Para ASP.NET Core, o padrão é `X-Forwarded-For` com `KnownProxies`.** O Product Owner vai ler o artigo e, com o que ele disser sobre como as requisições chegam ao site, configura a lista (`ForwardedHeaders__KnownProxies__0`, `__1`...). **Decisão mantida: o site continua fail-closed (lista vazia) até lá.**

**O que acontece sem o valor:** o site ignora o cabeçalho `X-Forwarded-For` (seguro por padrão, "fail-closed"). Se existir um proxy na frente do IIS, todo visitante chega com o mesmo IP e os limites por IP valem para o **site inteiro**. Se **não** existir proxy (o ASP.NET Core roda **em processo** dentro do IIS, e nesse modo o IIS costuma entregar o IP do cliente direto), o site já enxerga o IP certo e a lista nem é necessária.

**Como descobrir sem esperar ninguém (depois da primeira publicação):** erre a senha de propósito uma vez no painel e abra o log do dia em `gazeta-logs`. A linha `Falha de entrada de ... a partir de <IP>` mostra o IP que o site vê. Se for o **seu IP**, não há proxy a configurar: o aviso de partida pode ser ignorado e o limite temporário de 20 passa a ser folga. Se for **sempre o mesmo IP do provedor** (outro que não o seu), é o proxy: grave esse IP em `ForwardedHeaders__KnownProxies__0`. Esta é uma forma prática de decidir; a confirmação do provedor continua valendo.

**O que foi feito para isso não travar a redação (SC-01, SC-02, decisão do Product Owner de 2026-10-07):**

| Ação | Com `KnownProxies` (ou com IP real visto direto) | Sem `KnownProxies` atrás de um proxy (pior caso) |
|---|---|---|
| Entrar | só **falhas** contam: 5 por 15 min por IP | só falhas: **20** por 15 min por IP |
| "Esqueci minha senha" | balde próprio: 5 por 15 min por IP | balde próprio: **20** |
| "Redefinir senha" | balde próprio: 5 por 15 min por IP | balde próprio: **20** |
| Bloqueio de conta | 5 falhas **do mesmo IP contra a mesma conta**; outro IP segue entrando (SC-03) | igual no código, **mas sem o IP real todos os visitantes parecem o mesmo IP**: o bloqueio de uma conta vale para todo mundo, como antes do SC-03. O atraso progressivo e o aviso por e-mail continuam valendo |
| Aviso na partida | nenhum quando a lista tem valor | `Warning` nos logs: "ForwardedHeaders:KnownProxies não está configurado..." (também aparece se não houver proxy nenhum; nesse caso é só um lembrete) |

**Atenção:** a proteção do SC-03 (5 senhas erradas de qualquer rede não travam o Administrador) **só funciona de verdade com o IP real do visitante**. Por isso o SEC-01 se fecha antes de divulgar o site.

**Quando houver o IP do proxy:** grave uma variável por endereço (`ForwardedHeaders__KnownProxies__0`, `__1`...) com o IP exato, sem faixa e sem texto (um valor que não é IP derruba a partida) e reinicie o site. O aviso some e o limite volta a 5.
**Como conferir:** faça 6 senhas erradas de um IP e confira que o 6.º recebe 429 e que outro IP segue entrando.

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
| Logs na pasta `Logging__FileDirectory` | arquivo do dia, com a linha de início e, sem proxy configurado, o `Warning` do §5 |
| Entrar com o Administrador do primeiro acesso | pede a troca de senha; depois **remover** `Bootstrap__*` |
