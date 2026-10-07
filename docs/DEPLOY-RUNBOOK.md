# Runbook de publicação — GazetaMarketplace no SmarterASP

> **Em resumo:** este é o passo a passo para **você** (Product Owner) publicar o site na hospedagem, com os dados reais da conta. Ele cobre o que o `/infra` já definiu: o tipo de pacote, o banco, o certificado, as três pastas e as variáveis. A **conferência final de lançamento e a promoção para o público** são completadas na etapa `/deploy`; os pontos que ainda dependem de você estão no fim, em "Pendências antes de divulgar o site".
> Detalhes e motivos de cada decisão: [`INFRA.md`](INFRA.md).

## 1. Dados da conta (respostas do SmarterASP, 2026-10-07)

| Item | Valor | Origem |
|---|---|---|
| Raiz de arquivos da conta | `h:\root\home\mpaulohs-001\www\` | AR-01 |
| Permissão das pastas | O pool do IIS tem leitura e escrita por padrão | AR-01 |
| .NET 10 | Instalado no servidor | AR-02 |
| SQL Server | 2022 e 2025 disponíveis; **usar a 2022** | AR-03 |
| Disco | 30 GB, limite flexível | AR-04 |
| HTTPS | Certificado grátis, solicitado na aba **SSL** do painel | AR-09 |
| Usuário do banco | Sem resposta: **conta única com permissão total, risco aceito (RR-9)** | SEC-03 |
| IP do proxy | Resposta sobre IISNode; **decisão pendente do Product Owner** (ver §8) | SEC-01 |

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

## 5. HTTPS

No painel do SmarterASP, aba **SSL**: **solicite o certificado grátis** para o domínio e espere a emissão. Só depois divulgue o endereço. O site redireciona `http` para `https` (308) e envia HSTS; os cookies de sessão só viajam por HTTPS. Variável `Site__BaseUrl` com `https://` e sem barra no fim.

## 6. Variáveis de ambiente

Copie `src/GazetaMarketplace.Web/web.Production.config.example` para `web.Production.config` (fora do git, no cofre de senhas) e troque cada valor entre parênteses. As três pastas já vêm com os caminhos reais. Tabela completa e o que **nunca** definir em produção: [`INFRA.md`](INFRA.md) §4.

Em resumo: `ASPNETCORE_ENVIRONMENT=Production`, `ConnectionStrings__DefaultConnection`, as três pastas, `SendGrid__ApiKey`, `SendGrid__FromEmail`, `Site__BaseUrl`, `DataProtection__ProtectWithDpapi=false` (veja o §7 do `INFRA.md` antes de mudar) e, **só na primeira vez**, `Bootstrap__AdminEmail` e `Bootstrap__AdminPassword` (remover depois do primeiro acesso).

## 7. Ordem de uma publicação

1. Backup do banco.
2. Rodar o script do banco (§4).
3. Publicar o pacote (§2).
4. Abrir `https://(site)/health/ready` → `Healthy`.
5. Primeira vez: entrar com o Administrador, trocar a senha provisória, **remover `Bootstrap__*`** do `web.Production.config` e reiniciar.
6. Conferir os logs em `gazeta-logs` (linha de início do dia; veja o aviso do §8).

## 8. Pontos que ainda são seus

| Item | O que é | Situação |
|---|---|---|
| **SEC-01 — IP do proxy** | O artigo indicado pelo SmarterASP é para **IISNode**, não para ASP.NET Core. Para ASP.NET Core o padrão é `X-Forwarded-For` com a lista `KnownProxies`. Depois de ler o artigo, você configura a lista (`ForwardedHeaders__KnownProxies__0`...) | **Pendente.** O site segue fail-closed (lista vazia): ignora `X-Forwarded-For`, avisa na partida e aceita 20 tentativas de entrada por 15 min enquanto isso. Teste rápido no [`INFRA.md`](INFRA.md) §5: errar uma senha e ver que IP aparece no log |
| **SEC-03 — conta do banco** | Sem resposta do provedor: o site usa a conta única com permissão total | **Risco aceito (RR-9)** em `security/SECURITY_REQUIREMENTS.md` §5 |
| **SC-28 — chave do Google Maps no histórico do git** | Uma chave de API antiga aparece em dois commits de templates | **Exceção assinada.** A chave já foi revogada no Google Cloud Console; o histórico não é reescrito. Antes de publicar, confirme no console que ela aparece como revogada |
| **AR-12 — e-mail** | Domínio remetente validado no SendGrid, com SPF e DKIM, para o e-mail de redefinição não cair no spam | Pendente (Product Owner) |
| **AR-05 — imagens no Windows do provedor** | O ImageMagick (inclusive HEIC) precisa carregar no servidor | Conferir no primeiro envio de foto real (`docs/VERIFY-CHECKLIST.md`, M6) |
| **SC-05 — disco** | Não há cota por usuário para fotos | Olhar o tamanho de `gazeta-fotos` toda semana |

## 9. Pendências antes de divulgar o site

Os itens bloqueantes do `docs/VERIFY-CHECKLIST.md` são **M6 (fotos reais e GPS), M7 (HTTPS) e M10 (nunca página branca)**; os demais ficam para depois. Faça M6, M7 e M10 logo depois de publicar e **antes** de divulgar o endereço. A lista final de lançamento e a promoção são fechadas na etapa `/deploy`.
