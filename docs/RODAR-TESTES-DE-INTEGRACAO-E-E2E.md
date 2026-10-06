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

## Testes da ferramenta de exportação do catálogo

```bash
dotnet run --project tests/VehicleCatalogExport.Tests      # 43 testes, sem Docker
```

Os testes que precisam do SQL Server (origem simulada, script, carga em lote, endpoints) ficam em `tests/GazetaMarketplace.IntegrationTests/VehicleCatalogTests.cs` e rodam com os de integração, logo abaixo. Para gerar de novo o script de exemplo (`db/seed/sample/vehicle-catalog-sample.sql`), veja `db/seed/README.md`.

## Testes de integração (SQL Server em contêiner)

```bash
dotnet run --project tests/GazetaMarketplace.IntegrationTests
# ou, só os marcados:
dotnet test --project tests/GazetaMarketplace.IntegrationTests --filter "TestCategory=Integration"
```

Um contêiner para a suíte inteira; cada teste cria o próprio banco com nome único. Se o Ryuk (limpador do Testcontainers) não puder ser baixado, use `TESTCONTAINERS_RYUK_DISABLED=true`.

## Site para os E2E (Production, HTTPS, SQL Server em contêiner)

1. Subir o SQL Server e criar o banco com o script do repositório. O script já começa com `SET QUOTED_IDENTIFIER ON;` (gerado por `db/scripts/gerar-script.sh`), então roda sozinho; **a opção `-I` do `sqlcmd` continua sendo uma redundância defensiva** e deve ser mantida: sem o `SET` e sem o `-I`, o `sqlcmd` falha com o erro 1934 em `CREATE INDEX ... WHERE` (índices filtrados).

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
export Site__BaseUrl=https://localhost:5443    # obrigatório em Production: endereço que vai nos links dos e-mails
export RateLimiting__GlobalPerMinute=1000      # só no site do E2E: a suíte faz centenas de pedidos de um IP só (o padrão de produção é 100 por minuto)
export RateLimiting__PhotoUploadsPerMinute=1000 # só no site do E2E: a suíte sobe dezenas de fotos de uma conta só (o padrão de produção é 30 por minuto por usuário)
export RateLimiting__PhotosPerMinute=5000       # só no site do E2E: a página de um anúncio com 20 fotos já pede mais de 20 imagens e a suíte abre dezenas de páginas de um IP só (o padrão de produção é 300 por minuto por IP)
export SendGrid__BaseUrl=http://localhost:5990 # só nos E2E: o SendGrid "de mentira" que o teste de recuperação de senha abre
export ViaCep__BaseUrl=http://localhost:5991/ws/ # só nos E2E: o ViaCEP "de mentira" que o teste de CEP abre (em produção vale https://viacep.com.br/ws/)
cd /caminho/publish && dotnet GazetaMarketplace.Web.dll
```

3. Carregar o catálogo de veículos e as cidades de exemplo. O formulário do anúncio (`DraftE2ETests`) escolhe Marca → Modelo → Ano e Cidade/UF, e o banco do E2E nasce vazio dessas tabelas. Os dois scripts são idempotentes; use `-I`:

```bash
docker cp db/seed/sample/vehicle-catalog-sample.sql gazeta-e2e-sql:/tmp/v.sql
docker cp db/seed/sample/cities-sample.sql gazeta-e2e-sql:/tmp/c.sql
docker exec gazeta-e2e-sql /opt/mssql-tools18/bin/sqlcmd -C -b -I -S localhost -U sa -P '<senha-forte>' -d gazeta_e2e -i /tmp/v.sql
docker exec gazeta-e2e-sql /opt/mssql-tools18/bin/sqlcmd -C -b -I -S localhost -U sa -P '<senha-forte>' -d gazeta_e2e -i /tmp/c.sql
```

O `sample` do catálogo e das cidades é só para teste: nunca vai para produção (ver `db/seed/README.md`).

4. O Administrador criado pela partida nasce com troca de senha obrigatória; para os E2E, desligue a exigência (use `-I` aqui também):

```bash
docker exec gazeta-e2e-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P '<senha-forte>' -d gazeta_e2e -I -Q "UPDATE AspNetUsers SET MustChangePassword = 0"
```

## E2E (Playwright)

```bash
export PLAYWRIGHT_BROWSERS_PATH=/caminho/pw
export GAZETA_BASE_URL=https://localhost:5443
export GAZETA_E2E_EMAIL=e2e.admin@exemplo.com.br GAZETA_E2E_PASSWORD='<senha-do-admin>'
export GAZETA_E2E_SESSION_MINUTES=1            # igual a Authentication__SessionMinutes do site
export GAZETA_E2E_SENDGRID_PORT=5990           # igual à porta de SendGrid__BaseUrl; o teste de recuperação de senha escuta nela e lê o link do e-mail
export GAZETA_E2E_VIACEP_PORT=5991             # igual à porta de ViaCep__BaseUrl; o teste de CEP escuta nela, responde por CEP e conta as consultas
# export GAZETA_VITALS=1                       # só para os 4 testes de métricas (LCP, INP, CLS em celular lento); rodam SOZINHOS, nunca junto da suíte (ver "Orçamentos de desempenho"). Sem ela ficam ignorados: o /verify precisa ligar esta variável
dotnet run --project tests/GazetaMarketplace.Web.Tests.Playwright
```

**Segundo site, em Development (só para os componentes):** a página `/painel/componentes` não existe em Production (404). `ComponentsE2ETests` usa um segundo processo da **mesma saída publicada**, ligado ao mesmo banco, com `ASPNETCORE_ENVIRONMENT=Development`, outra porta (`ASPNETCORE_URLS=https://localhost:5444`, `Site__BaseUrl` igual) e `Authentication__SessionMinutes=30`; aponte `GAZETA_DEV_BASE_URL=https://localhost:5444`. Sem a variável o teste é ignorado. Para religar os dois sites depois de publicar, mate todos os processos `GazetaMarketplace.Web.dll` (por PID) e suba os dois `run-site.sh`.

Sem as variáveis, os testes que dependem delas ficam ignorados (`PasswordRecoveryE2ETests` exige também `GAZETA_E2E_SENDGRID_PORT`; `CepE2ETests` e `DraftE2ETests`, `GAZETA_E2E_VIACEP_PORT`). `AccountE2ETests` e `UsersE2ETests` exigem uma conta de **Administrador**; `SettingsE2ETests` também (e muda o telefone do site no banco de teste). `DraftE2ETests` cria rascunhos com título único no banco de teste e não os apaga (não há exclusão de rascunho na v1).

**Rodando a suíte mais de uma vez em menos de uma hora no mesmo banco:** o site limita a recuperação de senha a 10 pedidos por hora por IP e `US007` passa a falhar por tempo esgotado. Antes de rodar de novo, limpe a tabela:

```bash
docker exec gazeta-e2e-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '<senha-sa>' -C -d gazeta_e2e -I -Q "DELETE FROM PasswordRecoveryAttempts"
```

**Cache de CEP:** `CepE2ETests` espera que a primeira consulta de `13015100` e de `60000000` venha do ViaCEP de mentira; os outros E2E (que criam anúncios com o CEP `13015-100`) deixam essas entradas na tabela `CepCache` do banco por 30 dias. Antes de rodar a suíte inteira de novo no mesmo banco, limpe a tabela:

```bash
docker exec gazeta-e2e-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '<senha-sa>' -C -d gazeta_e2e -I -Q "DELETE FROM CepCache"
```

O limite global de 100 pedidos por minuto por IP também conta tudo que a suíte faz, e a suíte já passa disso: o site do E2E sobe com `RateLimiting__GlobalPerMinute=1000` (ver a lista de variáveis acima). O limite de login e de recuperação de senha (5 por 15 minutos) não é configurável.

### Acessibilidade e larguras de todas as telas

`AllScreensTests` (em `Accessibility/` e em `Responsiveness/`) mede cada tela de `tests/GazetaMarketplace.Web.Tests/Screens/screens.json` com o axe-core e nas quatro larguras. Usa as mesmas variáveis do E2E (`GAZETA_DEV_BASE_URL` inclusive, para o catálogo de componentes) e prepara os dados pelas telas do painel na primeira tela medida (um anúncio publicado com duas fotos, um rascunho com foto, um em revisão, a categoria "Telas sem anúncios" e um Redator). Para medir só algumas telas, ou repetir uma mutação: `GAZETA_SCREENS=home,panel-settings`. Uma tela nova do site precisa entrar na lista (o teste unitário `ScreenCoverageTests` falha enquanto não entrar). Para mutar o site, edite o código-fonte (a página, o CSS) e rode `dotnet publish` de novo: o estilo escrito direto no HTML (`style=`) é bloqueado pela política de segurança do site e não muda nada.

### Orçamentos de desempenho

Os limites (peso, p95 do servidor, LCP, INP, CLS) estão em `tests/GazetaMarketplace.Web.Tests/Performance/budgets.json`, um arquivo só lido pelos três projetos. `PageWeightTests` roda com o E2E normal (`GAZETA_BASE_URL`) e usa a categoria "Livros e revistas" do banco de teste (24 cards; fica inconclusivo se faltar anúncio). `VolumeOfV1Tests` roda com a integração (Docker; 200 anúncios criados na hora). `VitalsTests` só roda com `GAZETA_VITALS=1` e roda **sozinho**: simula celular (rede 4G, processador 4 vezes mais lento) e é sensível à máquina ocupada (uma compilação ao lado já fez o CLS da busca passar do limite). Os números medidos ficam em `performance-numbers.txt`, na pasta de saída do projeto de navegador (`bin/Debug/net10.0/`).

### Mutar um arquivo de `wwwroot` no site publicado

O site do E2E serve cada script e folha de estilo pela **lista de arquivos publicados** (`GazetaMarketplace.Web.staticwebassets.endpoints.json`): para cada arquivo há três entradas (o original, a cópia `.gz` e a cópia `.br`), cada uma com o tamanho (`Content-Length`) já escrito. O navegador pede o arquivo comprimido; por isso mexer só no arquivo original **não** muta nada, e **apagar** as cópias comprimidas faz o site responder `200` com o corpo vazio (o script não carrega e todo teste que usa JavaScript falha, o que parece "mutação morta" mas não é).

O jeito certo, para cada mutação:

1. Troque o texto no arquivo original da pasta publicada (`wwwroot/js/…`).
2. Na lista de arquivos publicados, tire as entradas `.gz` e `.br` desse arquivo e acerte o `Content-Length` da entrada do original para o tamanho novo, em bytes.
3. **Reinicie o site** (a lista é lida só na partida). Reiniciar também zera o limite de login (5 por 15 minutos, em memória), que as rodadas repetidas esgotam.
4. Confirme com `curl -k -H 'Accept-Encoding: gzip' https://localhost:5443/js/modules/favorites.js` que o corpo vem com o texto mutado e não vazio.
5. Rode o E2E; ao terminar, restaure o arquivo e a lista (as cópias de segurança) e reinicie o site, ou publique de novo.

Uma mutação **só conta como morta** se os testes que falharam são os que deveriam falhar por aquela mudança; se quase todos os testes de JavaScript falham, desconfie do passo 2 ou 3.

### Conferir que todos os testes de navegador rodaram

Antes de `dotnet run --no-build` no projeto de navegador, rode `dotnet build tests/GazetaMarketplace.Web.Tests.Playwright`: o binário velho pode ter uma lista de telas (`Screens/screens.json`) desatualizada, e a rodada executa dois testes a menos por tela que falta sem avisar. No fim, compare o total executado com `dotnet run --no-build --project tests/GazetaMarketplace.Web.Tests.Playwright -- --list-tests` (257 no momento): um teste que falta variável aparece como **pulado**, nunca some. Para ver quais testes foram executados, rode com `--diagnostic --diagnostic-verbosity Trace --diagnostic-output-directory <pasta>`: o arquivo `.diag` lista cada teste com o estado final.

## Cobertura de código (unitários e integração)

Os dois projetos de teste trazem a extensão `Microsoft.Testing.Extensions.CodeCoverage`; o escopo (o que entra na conta e o que fica de fora) está em `coverage.settings.xml`, na raiz. Rode cada projeto uma vez com cobertura (a de integração precisa do Docker) e junte os dois relatórios:

```bash
for project in GazetaMarketplace.Web.Tests GazetaMarketplace.IntegrationTests; do
  dotnet run --project tests/$project -- --coverage --coverage-settings coverage.settings.xml \
    --coverage-output-format cobertura --coverage-output $project.cobertura.xml --results-directory reports/test-artifacts/coverage
done
```

Cada projeto gera um arquivo Cobertura (XML). A cobertura do site é a **união** dos dois: uma linha conta como coberta se qualquer um dos dois a executou. O ramo (branch) de uma linha vale o maior número de ramos cobertos entre os dois, o que é um piso (a soma exata exigiria os dados por condição). Os números e a lista de métodos de regra de negócio com 0% ficam em `reports/TEST_REPORT.md` (seção "Cobertura pós-Checkpoint 4").

## Regenerar o script das migrations

Depois de criar uma migration, rode `db/scripts/gerar-script.sh`. Ele chama `dotnet ef migrations script --idempotent` e põe no topo o `SET QUOTED_IDENTIFIER ON;` que o EF não emite. O teste `Script_LigaQuotedIdentifierNoTopo_AntesDeQualquerComando` e o teste de integração com a sessão em `QUOTED_IDENTIFIER OFF` falham se o `SET` faltar.

## Publicação (para o runbook de implantação)

- **Script de banco:** aplicar `db/scripts/gazeta-idempotente.sql` com `sqlcmd -I` (redundância defensiva; o script já liga o `QUOTED_IDENTIFIER`).
- **Limite de pedidos do E2E:** `RateLimiting__GlobalPerMinute=1000` só no site de teste. Sem isso, a suíte completa recebe 429 em páginas de login e os testes caem por tempo esgotado. Em produção a chave não existe e vale 100.
- **Limite de login e recuperação do E2E:** `RateLimiting__AuthPermits=100000` só no site de teste (o limite de produção é 5 pedidos por 15 minutos por IP, nas ações de entrar, "esqueci minha senha" e "redefinir senha"; a suíte entra dezenas de vezes do mesmo IP). Sem isso, a suíte recebe 429 nas telas de entrada. Em produção a chave não existe e vale 5.
- **Limite de entrega de fotos do E2E:** `RateLimiting__PhotosPerMinute=5000` só no site de teste. Sem isso, a galeria do detalhe do anúncio esgota os 300 pedidos por minuto e `PhotosE2ETests` recebe 429. Em produção a chave não existe e vale 300.
- **Endereço do site:** definir `Site__BaseUrl` (https). Sem ele o site não sobe em Production, por segurança: o link do e-mail de redefinição de senha não pode nascer do cabeçalho Host.
- **Arquivos estáticos:** em Production, os arquivos estáticos só saem da saída publicada. Rodar os testes de CSS contra a pasta publicada, nunca contra o código-fonte.

## Desligar e religar o ambiente de E2E

```bash
# Desligar (libera memória e a porta 5443)
kill "$(pgrep -f 'GazetaMarketplace.Web.dll' | head -1)"      # por PID; evite `pkill -f`, que pode matar o próprio shell
docker stop gazeta-e2e-sql

# Religar
docker start gazeta-e2e-sql                                    # o banco e os dados continuam no contêiner
setsid nohup bash /caminho/run-site.sh > /caminho/site.log 2>&1 &   # as variáveis do passo 2 ficam dentro do run-site.sh
```
