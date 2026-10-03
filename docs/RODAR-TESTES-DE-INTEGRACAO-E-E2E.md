# Como rodar os testes de integração e os E2E

> **Em resumo:** os testes de integração sobem um SQL Server de verdade em contêiner; os E2E (navegador) rodam contra o site publicado, em Production, ligado a outro SQL Server em contêiner. Tudo isolado: nenhum teste toca em banco ou serviço que já exista.

## Pré-requisitos

| Item | Como conferir |
|---|---|
| Docker com o daemon ligado | `docker ps` responde (no ambiente em nuvem: `dockerd` pode estar parado; suba com `setsid nohup dockerd > dockerd.log 2>&1 &`) |
| Imagem do SQL Server | `docker pull mcr.microsoft.com/mssql/server:2022-latest` (cerca de 2,3 GB em disco) |
| Navegador do Playwright | O pacote 1.63.0 espera o Chromium revisão 1243. Se só houver outra revisão em `/opt/pw-browsers`, aponte `PLAYWRIGHT_BROWSERS_PATH` para uma pasta com `chromium_headless_shell-1243/chrome-headless-shell-linux64` ligada ao navegador instalado. **Não** rode `playwright install` |

## Testes unitários (SQLite em memória)

```bash
dotnet run --project tests/GazetaMarketplace.Web.Tests
```

## Testes de integração (SQL Server em contêiner)

```bash
dotnet run --project tests/GazetaMarketplace.IntegrationTests
# ou, só os marcados:
dotnet test --project tests/GazetaMarketplace.IntegrationTests --filter "TestCategory=Integration"
```

Um contêiner para a suíte inteira; cada teste cria o próprio banco com nome único. Se o Ryuk (limpador do Testcontainers) não puder ser baixado, use `TESTCONTAINERS_RYUK_DISABLED=true`.

## Site para os E2E (Production, HTTPS, SQL Server em contêiner)

1. Subir o SQL Server e criar o banco com o script do repositório. **O `sqlcmd` precisa da opção `-I`** (identificadores entre aspas ligados): sem ela, o script falha em `CREATE INDEX ... WHERE` (índices filtrados).

```bash
docker run -d --name gazeta-e2e-sql -p 14330:1433 -e ACCEPT_EULA=Y -e 'MSSQL_SA_PASSWORD=<senha-forte>' mcr.microsoft.com/mssql/server:2022-latest
docker exec gazeta-e2e-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P '<senha-forte>' -Q "CREATE DATABASE gazeta_e2e"
docker cp db/scripts/gazeta-idempotente.sql gazeta-e2e-sql:/tmp/s.sql
docker exec gazeta-e2e-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P '<senha-forte>' -d gazeta_e2e -b -I -i /tmp/s.sql
```

2. Publicar e subir o site. **Rodar a saída publicada**, não `dotnet run` na pasta do código: em Production os arquivos estáticos só são servidos pela saída publicada (senão o CSS não carrega e os testes de layout falham).

```bash
dotnet publish src/GazetaMarketplace.Web -c Release -o /caminho/publish
export ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=https://localhost:5443
export ASPNETCORE_Kestrel__Certificates__Default__Path=/caminho/cert.pem      # certificado autoassinado
export ASPNETCORE_Kestrel__Certificates__Default__KeyPath=/caminho/key.pem
export ConnectionStrings__DefaultConnection='Server=localhost,14330;Database=gazeta_e2e;User Id=sa;Password=<senha-forte>;TrustServerCertificate=True;Encrypt=True'
export PhotoStorage__BasePath=/caminho/fotos  Logging__FileDirectory=/caminho/logs  DataProtection__KeysDirectory=/caminho/chaves
export SendGrid__ApiKey=chave-de-teste  SendGrid__FromEmail=noreply@exemplo.com.br   # exigidos em Production (ADR-011)
export Bootstrap__AdminEmail=e2e.admin@exemplo.com.br  Bootstrap__AdminPassword='<senha-do-admin>'
export Authentication__SessionMinutes=1        # só para o teste de sessão expirada
cd /caminho/publish && dotnet GazetaMarketplace.Web.dll
```

3. O Administrador criado pela partida nasce com troca de senha obrigatória; para os E2E, desligue a exigência (o `-I` vale aqui também):

```bash
docker exec gazeta-e2e-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P '<senha-forte>' -d gazeta_e2e -I -Q "UPDATE AspNetUsers SET MustChangePassword = 0"
```

## E2E (Playwright)

```bash
export PLAYWRIGHT_BROWSERS_PATH=/caminho/pw
export GAZETA_BASE_URL=https://localhost:5443
export GAZETA_E2E_EMAIL=e2e.admin@exemplo.com.br GAZETA_E2E_PASSWORD='<senha-do-admin>'
export GAZETA_E2E_SESSION_MINUTES=1            # igual a Authentication__SessionMinutes do site
dotnet run --project tests/GazetaMarketplace.Web.Tests.Playwright
```

Sem as variáveis, os testes que dependem delas ficam ignorados. `AccountE2ETests` e `UsersE2ETests` exigem uma conta de **Administrador**.
