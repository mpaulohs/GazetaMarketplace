# Checklist de verificação manual e comandos de teste

> **Em resumo:** este arquivo tem tudo que **só você consegue conferir** (celular de verdade, WhatsApp, leitor de tela, Firefox, Safari, fotos reais, HTTPS com certificado válido, CEP real, pré-visualização de link, página 404) e os **comandos exatos** para rodar as suítes de teste e subir o site no seu computador. Cada item diz **como fazer, o que deve acontecer e o que fazer se falhar**. O que o computador consegue provar sozinho já foi rodado e está em `reports/VERIFY_REPORT.md`.

**Para quem é:** o Product Owner, que também publica o site. Não precisa saber programar; precisa copiar e colar comandos e olhar o resultado.

**Termos usados:** *Production* é o modo de produção do site (o que vai ao ar); *HTTPS* é o endereço seguro (`https://`); *artefato* é a pasta que o `dotnet publish` gera e que vai para a hospedagem; *Redator* e *Administrador* são os dois papéis da equipe.

---

## Parte 1 — Comandos

Todos os comandos supõem a raiz do repositório e um terminal bash (WSL2 no Windows). Quando aparecer `<senha-forte>`, invente uma senha só para o seu computador (não use a de produção).

### 1.1 Rodar as suítes de teste sozinho

| Suíte | Comando | O que esperar | Se falhar |
|---|---|---|---|
| Unidade (SQLite em memória, sem Docker) | `dotnet run --project tests/GazetaMarketplace.Web.Tests` | Linha final `total: 1765 … com falha: 0` (o total muda conforme o código cresce; **`com falha: 0` é o que importa**) | Guarde o texto do teste que falhou e me mande; não mexa no código |
| Ferramentas | `dotnet run --project tests/CitiesImport.Tests` e `dotnet run --project tests/VehicleCatalogExport.Tests` | 27 e 43 testes, `com falha: 0` | Idem |
| Integração (SQL Server em contêiner) | `dotnet run --project tests/GazetaMarketplace.IntegrationTests` | `total: 162 … com falha: 0`, cerca de 2 minutos; precisa do Docker ligado (`docker ps` responde) | Se aparecer erro de Docker, ligue o Docker e rode de novo. Se for erro de teste, guarde o texto |
| E2E (navegador, site publicado) | Veja 1.3 (precisa do site no ar) | `total: 258 … failed: 0 … skipped: 4`, cerca de 13 minutos. Os 4 ignorados são as métricas de velocidade (rodam à parte, 1.4) | Rode só o teste que falhou (1.3, "um teste só"); se passar sozinho, anote como instabilidade e me avise |

**Conferir que nenhum teste ficou de fora** (a contagem executada tem de bater com a listada):

```bash
dotnet run --project tests/GazetaMarketplace.Web.Tests -- --list-tests | tail -3     # "found 1765 test(s)"
```

### 1.2 Subir o site no seu computador (portas 5443 e 5444)

O site precisa de um SQL Server. Os comandos abaixo criam tudo numa pasta `~/gazeta-verify`. **Faça uma vez; depois só reinicie o contêiner e o site.**

```bash
# 0) pasta de trabalho e variáveis (copie este bloco no começo de cada terminal novo)
export WORK=~/gazeta-verify; export SQLPW='<senha-forte>'; export ADMINPW='<senha-do-admin-forte>'
mkdir -p $WORK/{publish,fotos,logs,chaves,cert}

# 1) certificado autoassinado (o navegador vai avisar "não seguro": é esperado no computador local)
openssl req -x509 -newkey rsa:2048 -nodes -days 30 -subj "/CN=localhost" \
  -addext "subjectAltName=DNS:localhost" -keyout $WORK/cert/key.pem -out $WORK/cert/cert.pem

# 2) SQL Server em contêiner + banco criado pelo script do repositório (idempotente)
docker run -d --name gazeta-e2e-sql -p 14330:1433 -e ACCEPT_EULA=Y -e "MSSQL_SA_PASSWORD=$SQLPW" mcr.microsoft.com/mssql/server:2022-latest
#    espere uns 40 segundos; se o comando abaixo der "Login timeout", espere mais e repita
docker exec gazeta-e2e-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$SQLPW" -Q "CREATE DATABASE gazeta_e2e"
docker cp db/scripts/gazeta-idempotente.sql gazeta-e2e-sql:/tmp/s.sql
docker exec gazeta-e2e-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$SQLPW" -d gazeta_e2e -b -I -i /tmp/s.sql
#    dados de exemplo (catálogo de veículos e cidades): SÓ para teste local, nunca para produção
docker cp db/seed/sample/vehicle-catalog-sample.sql gazeta-e2e-sql:/tmp/v.sql
docker cp db/seed/sample/cities-sample.sql gazeta-e2e-sql:/tmp/c.sql
docker exec gazeta-e2e-sql /opt/mssql-tools18/bin/sqlcmd -C -b -I -S localhost -U sa -P "$SQLPW" -d gazeta_e2e -i /tmp/v.sql
docker exec gazeta-e2e-sql /opt/mssql-tools18/bin/sqlcmd -C -b -I -S localhost -U sa -P "$SQLPW" -d gazeta_e2e -i /tmp/c.sql

# 3) publicar (a saída publicada é o que vai à hospedagem; em Production o CSS só é servido por ela)
dotnet publish src/GazetaMarketplace.Web -c Release -o $WORK/publish
```

**Site de produção local — porta 5443** (limites de segurança **padrão**, como na hospedagem: 5 tentativas de login por 15 minutos):

```bash
cd $WORK/publish
ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=https://localhost:5443 \
ASPNETCORE_Kestrel__Certificates__Default__Path=$WORK/cert/cert.pem \
ASPNETCORE_Kestrel__Certificates__Default__KeyPath=$WORK/cert/key.pem \
ConnectionStrings__DefaultConnection="Server=localhost,14330;Database=gazeta_e2e;User Id=sa;Password=$SQLPW;TrustServerCertificate=True;Encrypt=True" \
PhotoStorage__BasePath=$WORK/fotos Logging__FileDirectory=$WORK/logs DataProtection__KeysDirectory=$WORK/chaves \
SendGrid__ApiKey=chave-de-teste SendGrid__FromEmail=noreply@exemplo.com.br \
Bootstrap__AdminEmail=admin@exemplo.com.br Bootstrap__AdminPassword="$ADMINPW" \
Site__BaseUrl=https://localhost:5443 \
dotnet GazetaMarketplace.Web.dll
```

Abra `https://localhost:5443/`. **O que esperar:** página inicial em português; a primeira entrada em `/painel/entrar` com `admin@exemplo.com.br` pede para trocar a senha. **Se não subir:** a mensagem no terminal diz qual variável falta (o site recusa subir em Production sem `Site__BaseUrl`, chaves, pasta de fotos e SendGrid).

**Site de desenvolvimento — porta 5444** (só para ver a página `/painel/componentes`, que não existe em produção; use o mesmo banco, **em outro terminal**):

```bash
cd $WORK/publish
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=https://localhost:5444 \
ASPNETCORE_Kestrel__Certificates__Default__Path=$WORK/cert/cert.pem \
ASPNETCORE_Kestrel__Certificates__Default__KeyPath=$WORK/cert/key.pem \
ConnectionStrings__DefaultConnection="Server=localhost,14330;Database=gazeta_e2e;User Id=sa;Password=$SQLPW;TrustServerCertificate=True;Encrypt=True" \
PhotoStorage__BasePath=$WORK/fotos Logging__FileDirectory=$WORK/logs DataProtection__KeysDirectory=$WORK/chaves \
SendGrid__ApiKey=chave-de-teste SendGrid__FromEmail=noreply@exemplo.com.br \
Site__BaseUrl=https://localhost:5444 \
dotnet GazetaMarketplace.Web.dll
```

**O que esperar:** `https://localhost:5444/painel/componentes` abre (depois de entrar no painel). Em 5443 a mesma página dá 404.

**Parar os sites:** `Ctrl+C` em cada terminal. **Zerar tudo:** `docker rm -f gazeta-e2e-sql && rm -rf ~/gazeta-verify`.

### 1.3 E2E sobre o site local

Os E2E usam um **segundo conjunto de variáveis do site** (limites altos, porque a suíte faz centenas de pedidos de um IP só). **Não use os mesmos limites do 5443 acima.** O roteiro completo e o motivo de cada variável estão em `docs/RODAR-TESTES-DE-INTEGRACAO-E-E2E.md` (seção "Site para os E2E"); resumo do que muda em relação ao 5443:

```bash
# acrescente estas variáveis ao comando do site 5443 (e reinicie-o)
Authentication__SessionMinutes=1 RateLimiting__GlobalPerMinute=1000 RateLimiting__AuthPermits=100000 \
RateLimiting__PhotoUploadsPerMinute=1000 RateLimiting__PhotosPerMinute=5000 \
SendGrid__BaseUrl=http://localhost:5990 ViaCep__BaseUrl=http://localhost:5991/ws/

# desligar a troca obrigatória de senha do Administrador de teste e limpar os contadores
docker exec gazeta-e2e-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$SQLPW" -d gazeta_e2e -I -Q "UPDATE AspNetUsers SET MustChangePassword = 0; DELETE FROM PasswordRecoveryAttempts; DELETE FROM CepCache"

# rodar (outro terminal; PLAYWRIGHT_BROWSERS_PATH aponta para a pasta do navegador do Playwright)
export GAZETA_BASE_URL=https://localhost:5443 GAZETA_DEV_BASE_URL=https://localhost:5444
export GAZETA_E2E_EMAIL=admin@exemplo.com.br GAZETA_E2E_PASSWORD="$ADMINPW"
export GAZETA_E2E_SESSION_MINUTES=1 GAZETA_E2E_SENDGRID_PORT=5990 GAZETA_E2E_VIACEP_PORT=5991
dotnet build tests/GazetaMarketplace.Web.Tests.Playwright
dotnet run --no-build --project tests/GazetaMarketplace.Web.Tests.Playwright
```

**Um teste só** (troque o trecho do nome): `dotnet run --no-build --project tests/GazetaMarketplace.Web.Tests.Playwright -- --filter "FullyQualifiedName~US006S08"`.
**Regra importante:** depois de **qualquer** mudança de código, rode `dotnet build tests/GazetaMarketplace.Web.Tests.Playwright` **antes** de `--no-build`, senão o teste roda a versão velha. Para rodar a suíte duas vezes seguidas, repita o `DELETE` acima.

### 1.4 Métricas de velocidade (LCP, INP, CLS) — rodar sozinho

```bash
export GAZETA_VITALS=1
dotnet run --no-build --project tests/GazetaMarketplace.Web.Tests.Playwright -- --filter "FullyQualifiedName~VitalsTests"
cat tests/GazetaMarketplace.Web.Tests.Playwright/bin/Debug/net10.0/performance-numbers.txt
```

**O que esperar:** 4 testes passam; LCP abaixo de 2,5 s, INP abaixo de 200 ms, CLS abaixo de 0,1 em início, categoria, busca e detalhe. **Feche tudo o mais no computador antes** (uma compilação ao lado já estourou o CLS). **Se falhar:** rode de novo com o computador parado; se persistir, guarde `performance-numbers.txt` e me avise (vira item de correção).

---

## Parte 2 — Verificação manual

> **Decisão do Product Owner (2026-10-07):** só **M6 (fotos reais e GPS), M7 (HTTPS) e M10 (nunca página branca)** bloqueiam o go-live (divulgar o site ao público). Os outros 8 itens (**M1 a M5, M8, M9 e M11**) ficam **para depois do deploy** e não seguram a publicação; eles viram correções se falharem. Como M7 só dá para conferir com o site já publicado em HTTPS válido, faça M6, M7 e M10 **logo depois de publicar e antes de divulgar o endereço**. A preparação P1 a P3 é pré-requisito dos três.
>
> | Item | Quando | Bloqueia? |
> |---|---|---|
> | P1–P3 (preparação) | logo depois da publicação | pré-requisito |
> | **M6** fotos reais, orientação, cor e **GPS retirado** | antes de divulgar | **sim** |
> | **M7** HTTPS, cabeçalhos, compressão, cache das fotos | antes de divulgar | **sim** |
> | **M10** página 404 e erro sem página branca | antes de divulgar | **sim** |
> | M1 celular · M2 WhatsApp · M3 NVDA · M4 Firefox · M5 Safari · M8 CEP real · M9 og:image · M11 outros | depois do deploy | não |
>
> **Como usar:** faça na ordem. Marque `[x]` ao passar. Onde o item diz "**na hospedagem**", precisa do site publicado em HTTPS com certificado válido; os demais podem ser feitos em `https://localhost:5443` (aceite o aviso do certificado). Se um item falhar, **anote o número do item, o que apareceu e uma captura de tela**, e siga para o próximo; não tente corrigir.

### Preparação (uma vez)

- [ ] **P1.** Entre no painel como Administrador e crie **uma conta de Redator** (`/painel/usuarios`). Guarde as duas senhas.
- [ ] **P2.** Em `/painel/configuracoes`, informe o **telefone do site** (um celular real seu, com DDD). O WhatsApp e o "Ligar" dos anúncios usam este número.
- [ ] **P3.** Como Redator, cadastre **3 anúncios reais** (carro, imóvel e um produto qualquer) com fotos de verdade (ver M6), envie para revisão; como Administrador, **publique** os três. Sem anúncios publicados, vários itens abaixo não têm o que mostrar.

### M1. Celular de verdade (Android e iPhone) — na hospedagem — **[PÓS-DEPLOY]**

| Passo | O que esperar | Se falhar |
|---|---|---|
| Abra o site no celular, na vertical | Página inicial sem rolagem para os lados; categorias e anúncios recentes legíveis; texto sem zoom | Capture a tela; informe modelo do celular e navegador |
| Toque numa categoria e depois em **Filtros** na busca | O painel de filtros abre e fecha; "Aplicar filtros" fica à vista; a lista não "pula" quando as fotos chegam | Idem (se pular, é o CLS; vira item de correção) |
| Abra um anúncio e deslize a galeria | A primeira foto aparece rápido; as demais carregam ao deslizar; botões grandes o bastante para o dedo | Idem |
| Toque no coração (favoritos), feche o navegador e abra de novo `/favoritos` | O anúncio continua favoritado | Informe se o navegador estava em aba anônima (favoritos ficam só no navegador) |
| Gire o celular para horizontal | Nada fica cortado | Captura de tela |

### M2. WhatsApp e ligação — celular, na hospedagem — **[PÓS-DEPLOY]**

| Passo | O que esperar | Se falhar |
|---|---|---|
| Num anúncio, toque **Chamar no WhatsApp** | O WhatsApp abre numa conversa com **o telefone do site (P2)** e a mensagem pronta: `Olá! Tenho interesse no anúncio “<título>”: <endereço da página>` | Se abrir o número errado, confira P2. Se não abrir, informe o aparelho |
| Toque **Ligar** | O celular mostra a discagem para `+55` e o número do site | Idem |
| No computador, abra o mesmo anúncio | Os dois botões existem; o do WhatsApp abre o WhatsApp Web | Captura de tela |

### M3. Leitor de tela NVDA e teclado (Windows) — local ou hospedagem — **[PÓS-DEPLOY]**

Instale o NVDA (gratuito, nvaccess.org). Use só o teclado: `Tab`, `Shift+Tab`, `Enter`, `Espaço`, setas.

| Passo | O que esperar | Se falhar |
|---|---|---|
| Página inicial: `Tab` desde o topo | Primeiro foco em **"Ir para o conteúdo"**; o foco é sempre visível (contorno); ordem de leitura lógica | Anote a tela e o elemento sem foco visível (já há item aberto de contorno de foco, R-14) |
| Entrar no painel (`/painel/entrar`) com senha errada | O NVDA anuncia o erro ("E-mail ou senha inválidos…") sem você procurar | Anote o texto que o NVDA leu |
| Formulário do anúncio: deixe campos obrigatórios vazios e **Enviar para revisão** | O NVDA lê a lista "Faltam N itens…"; cada item leva ao campo; os campos erram com mensagem lida | Anote qual campo ficou mudo |
| Troque a categoria | O NVDA diz "Campos atualizados para a categoria …" | Idem |
| Janelas de confirmação (remover foto, despublicar) | O foco vai para a janela e volta ao botão ao fechar; `Esc` fecha | Anote o passo |

### M4. Firefox (computador) — local ou hospedagem — **[PÓS-DEPLOY]**

| Passo | O que esperar | Se falhar |
|---|---|---|
| Abra início, categoria, busca com filtros, detalhe, favoritos | Mesmo visual do Chrome; sem rolagem para os lados; **o preço aparece formatado ao digitar** no formulário (`R$ 62.000,00`) | Capture; informe a versão do Firefox |
| Formulário do anúncio: escolha a categoria Carros e a cadeia Marca → Modelo → Ano → Versão | Cada lista libera a seguinte | Idem |
| Galeria e envio de fotos | Envio por arrastar ou botão; miniaturas aparecem | Idem |

### M5. Safari (Mac e iPhone) — local ou hospedagem — **[PÓS-DEPLOY]**

| Passo | O que esperar | Se falhar |
|---|---|---|
| Mesmo roteiro do M4 | Igual | Capture; informe versão do Safari/iOS |
| No iPhone, **envie uma foto direto da câmera** (HEIC) no formulário | Aparece a miniatura; o anúncio mostra a foto **na orientação certa** | Se a foto vier deitada, anote (é o item de orientação de M6) |
| Preço e CEP digitados no iPhone | Máscaras funcionam; teclado numérico abre | Capture |

### M6. Fotos reais — local ou hospedagem — **[BLOQUEIA O GO-LIVE]**

Use **de 3 a 5 fotos suas**: uma HEIC de iPhone, um JPEG grande de Android (4 MB ou mais), uma com a câmera na vertical. Depois uma carga de 24 anúncios com foto (ou 24 fotos num só, no limite de 20) para a lista.

| Passo | O que esperar | Se falhar |
|---|---|---|
| Envie as fotos num anúncio | Todas aceitas; a HEIC vira imagem normal; mensagem clara para arquivo inválido (um PDF) | Anote formato e mensagem |
| Abra o anúncio publicado | **Orientação** certa, **cores** iguais às da foto original | Anote qual foto |
| **GPS removido:** baixe uma imagem do anúncio (clique direito → salvar) e rode `exiftool foto.webp \| grep -i gps` | **Nenhuma linha** de GPS | **Sério (privacidade):** anote e me avise antes de publicar |
| Peso da lista (24 anúncios) e do detalhe | Lista até 2 MB, detalhe até 3 MB na primeira carga (DevTools → Rede → "Transferido"); no detalhe, as fotos além da primeira só carregam depois do deslize | Anote os números |
| Limite: tente o 21.º foto | Mensagem "Cada anúncio pode ter no máximo 20 fotos" | Anote |

### M7. HTTPS — na hospedagem — **[BLOQUEIA O GO-LIVE]**

| Passo | O que esperar | Se falhar |
|---|---|---|
| Digite `http://seu-dominio` (sem o `s`) | Redireciona para `https://` | Anote; é configuração do IIS/SmarterASP (ver runbook) |
| Veja o cadeado | Certificado válido, sem aviso | Informe o aviso exibido |
| Em DevTools → Rede, abra qualquer resposta | Cabeçalhos presentes: `strict-transport-security`, `content-security-policy`, `x-content-type-options`, `x-frame-options`, `referrer-policy` | Anote quais faltam |
| Compressão: resposta da página inicial | Um só `content-encoding` (`br` ou `gzip`), **não duplicado** | Anote (compressão duplicada no IIS) |
| Fotos | `cache-control: public, max-age=31536000, immutable` | Anote |
| Opcional: cole o endereço em securityheaders.com | Nota A ou melhor | Anote a nota |

### M8. CEP real — na hospedagem (o ambiente de teste não alcança o ViaCEP) — **[PÓS-DEPLOY]**

| Passo | O que esperar | Se falhar |
|---|---|---|
| Formulário do anúncio, CEP `13015-100` | "Buscando…" e depois **Campinas / SP** preenchidos | Anote o que apareceu |
| CEP `60000-000` | Cidade e UF de Fortaleza/CE | Idem |
| CEP inexistente `00000-000` | Mensagem "CEP não encontrado. Confira os números." (sem ir ao preenchimento manual) | Idem |
| Repita `13015-100` | Resposta instantânea (veio do cache de 30 dias) | Anote se demorou |
| Simule falha: desligue a internet do servidor (ou bloqueie `viacep.com.br`) e digite um CEP novo | "Buscando… (tentativa 2 de 2)", depois mensagem e **Cidade/UF manuais**; consegue enviar o anúncio | Anote |

### M9. Pré-visualização do link (og:image) — na hospedagem — **[PÓS-DEPLOY]**

| Passo | O que esperar | Se falhar |
|---|---|---|
| Cole o endereço de um anúncio publicado numa conversa de WhatsApp (sem enviar) | Aparece **título, descrição e a foto** do anúncio | Se não aparecer foto, confira `og:image` no passo seguinte |
| Veja o código-fonte da página (`Ctrl+U`) e procure `og:image` | `content="https://seu-dominio/fotos/…-480.webp"` (endereço **completo**, com `https`, do seu domínio) | Se vier `localhost` ou `http`, falta/está errada a variável `Site__BaseUrl` |
| Opcional: Facebook Sharing Debugger com o mesmo endereço | Mesma prévia | Anote o erro que o depurador mostrar |
| Anúncio **arquivado** (arquive um de teste) | O endereço mostra "Este anúncio não está mais disponível" e o código-fonte tem `<meta name="robots" content="noindex">` | Anote |

### M10. Página 404 e erros — local ou hospedagem — **[BLOQUEIA O GO-LIVE]**

| Passo | O que esperar | Se falhar |
|---|---|---|
| Abra `/qualquer-coisa-que-nao-existe` | Título **"Página não encontrada"**, uma frase e o link **"Ir para a página inicial"**; o navegador mostra status 404 (DevTools → Rede) | Capture |
| Abra um anúncio inexistente `/anuncio/999999/x` | "Este anúncio não está mais disponível" (a mesma de um anúncio arquivado) | Idem |
| Envie um formulário com a aba velha (deixe a página aberta 1 hora, volte e clique Salvar) | Página "Algo deu errado" com status 400, **nunca branca** | Anote (se branca, é defeito grave) |

### M11. Outros itens que só você confere (vindos do `/test`) — **[PÓS-DEPLOY]**

- [ ] **M11.1** E-mail real de recuperação de senha (SendGrid, na hospedagem): peça a redefinição para o seu e-mail; o e-mail chega em minutos, o link abre `https://seu-dominio/painel/redefinir-senha?...` e **vale 1 vez**. Se não chegar, veja a caixa de spam e o painel do SendGrid.
- [ ] **M11.2** Sessão da equipe: entre, deixe 30 minutos parado e clique em qualquer coisa → volta ao login com o aviso de sessão expirada.
- [ ] **M11.3** Login: erre a senha 5 vezes → a 6.ª tentativa mostra "Muitas tentativas. Tente novamente em alguns minutos." por 15 minutos (a página do pedido bloqueado pelo limite pode aparecer só como texto simples: ver item novo no BACKLOG). **Cuidado:** na hospedagem, sem a lista de proxies (item R-11 do BACKLOG), **a equipe inteira divide esse limite**; faça este teste por último e avise a equipe.
- [ ] **M11.4** Componente de imagens (HEIC e WebP) na hospedagem Windows: envie uma foto HEIC; se aparecer "Não foi possível converter esta foto HEIC", o componente nativo não carregou (item do `/infra`).
- [ ] **M11.5** Métricas de velocidade **na hospedagem**, sem outra carga: rode 1.4 apontando `GAZETA_BASE_URL` para o domínio.

---

## Parte 3 — O que já foi verificado por máquina (não precisa repetir)

Está em `reports/VERIFY_REPORT.md`: liveness, cabeçalhos de segurança, erros em formato padrão, CORS fechado, rotas de desenvolvimento ausentes em Production, limite de login com 429, 258 testes de navegador no artefato publicado, acessibilidade (axe, nível A e AA) e larguras (320, 768, 1024, 1280) em 45 telas, e a matriz cenário → teste. Os itens acima são o que **não dá** para provar sem um aparelho, uma pessoa ou a rede de produção.

**Resumo de como decidir:** P1–P3, M6, M7 e M10 passando = pode divulgar o site (go-live). Qualquer falha em M6 (GPS), M7 (HTTPS) ou M10 (página branca) **bloqueia** a divulgação; as demais (M1–M5, M8, M9, M11) viram itens de correção depois do deploy e você decide.
