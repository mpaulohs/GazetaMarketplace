# Infraestrutura e pacote de publicação

> **Em resumo:** este documento diz **o que o site precisa do servidor** (SmarterASP, IIS no Windows), **como gerar a pasta que vai para lá**, **como criar o banco com o script do repositório** e **quais variáveis de ambiente configurar**. Quem publica é o Product Owner. O passo a passo da publicação em si, com a lista de conferência final, é o `docs/DEPLOY-RUNBOOK.md` (etapa `/deploy`); aqui fica o que é preciso estar pronto antes.

| O que | Valor |
|---|---|
| Destino | SmarterASP, IIS, Windows, ASP.NET Core 10 em processo (`hostingModel="inprocess"`) |
| Pasta publicada | `src/GazetaMarketplace.Web/bin/publish/win-x64/`, cerca de **62 MB** (eram 233 MB com os componentes de todas as plataformas) |
| Banco | SQL Server 2022 (ou Azure SQL), criado só pelo script `db/scripts/gazeta-idempotente.sql` |
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

## 2. Pré-requisitos no servidor

| Item | Como confirmar | Quem |
|---|---|---|
| **ASP.NET Core Hosting Bundle 10** (runtime e módulo do IIS) | Pergunte ao SmarterASP; sem ele o site não sobe (erro 500.19 ou 502.5). Se só houver a 9, o provedor precisa instalar a 10 ou o pacote vira autocontido (mais ~70 MB) | Product Owner (ticket) |
| HTTPS com certificado válido no domínio | Abrir `https://seudominio` sem aviso; o site redireciona `http` para `https` (308) e envia HSTS | Product Owner |
| Pastas **fora da raiz do site**, com escrita só para a identidade do pool: `fotos`, `logs`, `chaves` | Caminhos absolutos (ex.: `D:\dados\fotos`); a `fotos` terá `_originals` por baixo | Product Owner |
| Caminho **absoluto** nas três pastas | Se for relativo, `_originals/` (que tem GPS) pode cair dentro da pasta publicada (SC-10, P1) | Product Owner |
| SQL Server com usuário próprio do site (sem `sa`, sem `db_owner` em runtime) | O script cria o esquema; o usuário do site precisa de leitura, escrita e `EXECUTE` | Product Owner |
| Conta SendGrid com **remetente validado**, SPF e DKIM do domínio | Sem isso o e-mail de redefinição cai no spam | Product Owner |
| **Monitoramento de espaço em disco** (SC-05) | O envio de fotos não tem cota por usuário: configure aviso de uso de disco no painel do provedor ou olhe a pasta `fotos` toda semana | Product Owner |

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
| `PhotoStorage__BasePath` | sim | Pasta das fotos, **absoluta e fora da raiz do site** |
| `Logging__FileDirectory` | sim | Pasta dos logs (arquivo diário, 14 arquivos, cada um de até 100 MB) |
| `DataProtection__KeysDirectory` | sim | Pasta das chaves de sessão e antiforgery. **Sem ela a reciclagem do pool derruba a sessão da equipe e invalida links de redefinição** (R-01) |
| `SendGrid__ApiKey`, `SendGrid__FromEmail` | sim | Chave de API do SendGrid (permissão só de envio) e remetente validado |
| `Site__BaseUrl` | sim | Endereço público **com `https`**, sem barra no fim. Vai nos links dos e-mails; sem ele o link nasceria do cabeçalho `Host`, que um atacante forja |
| `DataProtection__ProtectWithDpapi` | recomendada | `true`: cifra as chaves com o DPAPI do Windows. Só no servidor Windows; em outro sistema o site **recusa subir** com a opção ligada |
| `ForwardedHeaders__KnownProxies__0` | **pendente (SEC-01)** | IP do proxy do SmarterASP. Veja §5 |
| `Bootstrap__AdminEmail`, `Bootstrap__AdminPassword` | só na 1.ª vez | Primeiro Administrador, com troca de senha obrigatória no primeiro acesso. **Remover depois** |
| `HttpsRedirection__HttpsPort` | não | Padrão 443 em produção |

**Nunca defina em produção** `RateLimiting__*`, `SendGrid__BaseUrl`, `ViaCep__BaseUrl` nem `Authentication__SessionMinutes` com valor de teste: existem para a suíte de testes e afrouxam os limites de proteção ou desviam a chave de API (R-39). Se faltar qualquer variável obrigatória, o site **não sobe** e diz qual faltou (validação na partida).

## 5. IP do proxy (SEC-01, R-11): o que fazer enquanto o provedor não responde

**O que acontece sem o valor:** o site ignora o cabeçalho `X-Forwarded-For` (seguro por padrão, "fail-closed"). Atrás do proxy do provedor, todo visitante chega com o mesmo IP, então os limites por IP valem para o **site inteiro** e não para cada visitante.

**O que foi feito para isso não travar a redação (SC-01, SC-02, decisão do Product Owner de 2026-10-07):**

| Ação | Com `KnownProxies` | Sem `KnownProxies` (hoje) |
|---|---|---|
| Entrar | só **falhas** contam: 5 por 15 min por IP | só falhas: **20** por 15 min por IP |
| "Esqueci minha senha" | balde próprio: 5 por 15 min por IP | balde próprio: **20** |
| "Redefinir senha" | balde próprio: 5 por 15 min por IP | balde próprio: **20** |
| Bloqueio de conta | 5 falhas **do mesmo IP contra a mesma conta**; outro IP segue entrando (SC-03) | igual no código, **mas sem o IP real todos os visitantes parecem o mesmo IP**: o bloqueio de uma conta vale para todo mundo, como antes do SC-03. O atraso progressivo e o aviso por e-mail continuam valendo |
| Aviso na partida | nenhum | `Warning` nos logs: "ForwardedHeaders:KnownProxies não está configurado..." |

**Atenção:** a proteção do SC-03 (5 senhas erradas de qualquer rede não travam o Administrador) **só funciona de verdade depois que o IP real do visitante é conhecido**. Esta é mais uma razão para fechar o SEC-01 antes de divulgar o site.

**O que fazer:** abra um chamado no SmarterASP perguntando **quais endereços de proxy ou balanceador entregam as requisições ao IIS e se enviam `X-Forwarded-For`**. Com a resposta, grave uma variável por endereço (`ForwardedHeaders__KnownProxies__0`, `__1`...) com o IP exato, sem faixa e sem texto (um valor que não é IP derruba a partida). Reinicie o site. O aviso some e o limite volta a 5 por IP.
**Como conferir:** depois de configurado, `X-Forwarded-For` de um IP qualquer só vale se a conexão vier de um dos proxies listados; faça 6 senhas erradas de um IP e confira que o 6.º recebe 429 e que outro IP segue entrando.

## 6. Chaves do Data Protection

- As chaves ficam em `DataProtection__KeysDirectory` (XML). Sem criptografia em repouso quem lê a pasta pelo FTP as lê (RR-2).
- Com `DataProtection__ProtectWithDpapi=true` elas são cifradas com o DPAPI **no escopo do usuário do pool**: copiadas para outra máquina, não abrem. Custo: se a identidade do pool mudar, as chaves antigas deixam de abrir e a equipe precisa entrar de novo (links de redefinição já enviados também deixam de valer). Por isso é **recomendada, mas opcional**.
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
