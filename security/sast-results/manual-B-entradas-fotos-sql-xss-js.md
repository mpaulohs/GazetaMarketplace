# Varredura SAST manual — Escopo B (entrada, arquivos, banco, saída HTML/JS, cabeçalhos, configuração)

Repositório: `/home/user/GazetaMarketplace` (ASP.NET Core 10 MVC/Razor, EF Core + Dapper, SQL Server, Magick.NET 14.17.2). Somente leitura (Read/Grep/Glob); nada foi compilado, executado nem alterado.
Idioma: português; código e identificadores em inglês.

## Resumo executivo

**Nenhum achado Crítico, Alto ou explorável de forma séria neste escopo.** As defesas que o THREAT_MODEL promete (RC-2, RC-4, RC-6, RC-15, RC-17, RC-21) estão no código e conferem com a leitura. Há **1 achado Médio** (saturação do log por tráfego anônimo), **4 Baixos** e **7 Informativos**. Nada aqui bloqueia o Gate 8 por severidade.

| Id | Severidade | Título | OWASP | Confiança |
|----|-----------|--------|-------|-----------|
| B-01 | Médio | Log saturável por tráfego anônimo (429 grava o caminho inteiro, teto de 1 GB/dia derruba o log de segurança, máscara de e-mail quadrática) | A09 / A04 | Código confirmado pela leitura; volume em disco a confirmar |
| B-02 | Baixo | Originais com EXIF/GPS ficam 30 dias no disco sem nenhum consumidor (`IPhotoReprocessing` não tem chamador) e sobrevivem à remoção da foto | A02 / A04 (LGPD) | Confirmado pela leitura |
| B-03 | Baixo | Sem cota de disco por usuário/anúncio/total para envio de fotos (conta de equipe comprometida enche o disco) | A04 / A05 | Confirmado pela leitura |
| B-04 | Baixo | Dois decodificadores de 50 MP em paralelo podem estourar a memória do pool compartilhado; faltam limites `Disk`/`Area` do ImageMagick | A04 / A05 | A confirmar na hospedagem |
| B-05 | Baixo | `PhotoStorage:BasePath` e demais pastas só têm `[Required]`: nada impede caminho relativo ou dentro da raiz publicada | A05 | Confirmado pela leitura |
| B-06 | Info | `X-Correlation-ID` do cliente vira `TraceIdentifier`, id de log e "código de referência" | A09 | Confirmado |
| B-07 | Info | `Normalize(FormD)` pode expandir o título além de `TitleSearch nvarchar(200)`; corte em 100 chars pode partir um par substituto e lançar `ArgumentException` (lista do painel) | A04 | A confirmar com teste |
| B-08 | Info | Regex de forma de chave com `$` aceita `\n` final (defesa em profundidade; `\z`) | A03 | Confirmado (sem caminho de exploração) |
| B-09 | Info | Cabeçalhos a mais: `Cross-Origin-Resource-Policy`/`COOP`, `Referrer-Policy: no-referrer` no link de redefinição (CSP `base-uri`/`object-src` já é R-37) | A05 | Confirmado |
| B-10 | Info | `HttpClient` do ViaCEP/SendGrid segue redirecionamentos e lê resposta sem teto | A10 | Confirmado |
| B-11 | Info | `wwwroot` publica bibliotecas sem uso (jQuery/validation), `.map` e `LEIAME.md` | A05 | Confirmado |
| B-12 | Info | `LoginFailureCounter` varre todas as origens sob um lock global quando passa de 1024 (observação para o escopo A) | A07 | Confirmado |

Coincidências com o BACKLOG estão marcadas como **já conhecido: R-nn** (não repetidas como achados novos) na seção "Já conhecido".

---

## Achados

### B-01 — Log saturável por tráfego anônimo (Médio) — A09 / A04
- **Onde:** `Web/Security/RateLimitingExtensions.cs:145` (`LogWarning("Limite de requisições excedido em {Method} {Path}")`), `Infrastructure/Logging/SerilogConfiguration.cs:31-36` (`WriteTo.File` sem `fileSizeLimitBytes`/`rollOnFileSizeLimit`), `Infrastructure/Logging/MaskingEnricher.cs:58,62` (regex `Email()` sem timeout).
- **Descrição:** toda requisição recusada pelo limite global (100/min por IP) grava uma linha `Warning` com o **caminho completo** (até ~4 KB no IIS, 8 KB no Kestrel). O sink de arquivo do Serilog tem teto padrão de **1 GB por arquivo diário e, ao atingi-lo, descarta os eventos seguintes sem aviso** (`rollOnFileSizeLimit=false`). Além disso, o `MaskingEnricher` roda a regex `([A-Za-z0-9._%+\-])[A-Za-z0-9._%+\-]*@(...)` em cada string do evento: numa sequência longa de caracteres permitidos sem `@` (um caminho `/aaaa…`), o custo é O(n²) (um início por posição, cada um varrendo até o fim da sequência), sem `matchTimeout`.
- **Exploração (passo a passo):**
  1. De um único IP (ou de um /64 IPv6, ver R-38), enviar pedidos a `GET /aaaa…aaaa` (4 KB) em ritmo acima de 100/min.
  2. Do 101º pedido em diante cada resposta é 429 e gera um evento com o caminho de 4 KB, mascarado pela regex quadrática (dezenas de milissegundos de CPU no mesmo thread da requisição) e gravado com descarga a cada evento.
  3. Em poucas dezenas de minutos o arquivo do dia chega a 1 GB: **o log para de registrar o resto do dia**, inclusive `Falha de entrada…`, `acesso negado` e auditoria de segurança (RC-16), o que apaga o rastro de um ataque simultâneo ao login. Com 14 arquivos retidos, o pior caso é ~14 GB de disco numa conta de hospedagem compartilhada.
- **Correção sugerida:** (1) `fileSizeLimitBytes` explícito + `rollOnFileSizeLimit: true` + `retainedFileCountLimit`, e alerta quando o arquivo passa de X MB; (2) não registrar o caminho inteiro em 429 (cortar em 200 caracteres) e registrar no máximo 1 aviso por IP por minuto (amostragem) ou em nível `Debug`; (3) `matchTimeoutMilliseconds: 50` nas duas regex do `MaskingEnricher` e pular o mascaramento de string acima de 1 KB (ou reescrever o padrão com `\b`/lookbehind para ficar linear); (4) teste com caminho de 4 KB que mede o tempo de `Sanitize`.
- **Confiança:** código confirmado pela leitura; o efeito em disco depende de a hospedagem aceitar o fluxo (a confirmar com um teste de carga controlado).
- **Já conhecido?** Parcialmente relacionado a R-38 e SEC-01/R-11 (limite por IP), mas o vetor de log não está no BACKLOG.

### B-02 — Originais com EXIF/GPS retidos 30 dias sem consumidor (Baixo) — A02 / A04 (LGPD)
- **Onde:** `Infrastructure/Photos/PhotoIngestion.cs:38` (grava `data` bruto em `_originals/`), `Core/Photos/PhotoLimits.cs:24` (retenção de 30 dias), `Infrastructure/Photos/AdPhotoService.cs:110` (a remoção da foto deixa o original), BACKLOG L112 (`IPhotoReprocessing` sem chamador).
- **Descrição:** as versões servidas são WebP sem metadados (`MagickImageProcessor.cs:49`, `Strip()`), mas o **arquivo original enviado é gravado intacto** (GPS, modelo da câmera, data) e só sai na limpeza diária depois de 30 dias. A remoção de uma foto pelo autor (ação que a pessoa entende como "apagar") não remove o original. A única funcionalidade que usaria o original (`IPhotoReprocessing`) não tem chamador na v1, então a retenção não serve a nada hoje. A pasta está fora da raiz do site e não tem rota (ok), mas aparece em backups e em qualquer acesso ao disco.
- **Exploração:** não há caminho HTTP; o risco é de vazamento por acesso ao disco/backup (ou conta de hospedagem comprometida) e de descumprimento do princípio da necessidade (LGPD art. 6º, III): localização exata de quem fotografou em casa.
- **Correção sugerida:** na v1 não guardar o original (YAGNI) **ou** guardá-lo já sem EXIF/GPS (recodificação sem perda) **ou** apagá-lo junto com a foto e junto com o arquivamento do anúncio, e reduzir a retenção. Documentar na ADR-005.
- **Confiança:** confirmado pela leitura. A decisão de 30 dias é do Product Owner (ADR-005); o que é novo é a leitura como risco de privacidade.

### B-03 — Sem cota de disco para fotos (Baixo) — A04 / A05
- **Onde:** `Web/Security/RateLimitingExtensions.cs` (política `fotos-envio`: 30/min por usuário), `Infrastructure/Photos/PhotoIngestion.cs:23-36`, `FileSystemPhotoStorage.cs`.
- **Descrição:** RC-6 limita CPU (2 conversões simultâneas) e a taxa (30/min), não o **volume em disco**. Cada envio grava até 10 MB de original (30 dias) mais duas versões WebP; não há teto de rascunhos por usuário, nem de bytes por anúncio/usuário, nem checagem de espaço livre.
- **Exploração:** conta de Redator comprometida (phishing) ou mal-intencionada cria rascunhos em série e envia 30 fotos de 10 MB por minuto (até a vazão de 2 conversões): ~300 MB/min, dezenas de GB por dia, até o disco/quota da hospedagem acabar (o site para de gravar fotos, chaves do Data Protection e logs).
- **Correção sugerida:** teto de rascunhos abertos por usuário, teto de bytes por usuário/dia, recusa quando o espaço livre da pasta de fotos estiver abaixo de um limite, e (junto com B-02) não reter originais.
- **Confiança:** confirmado pela leitura; exige conta de equipe, por isso Baixo.

### B-04 — Memória nativa do ImageMagick em hospedagem compartilhada (Baixo, a confirmar) — A04 / A05
- **Onde:** `Infrastructure/Photos/MagickRuntime.cs:72-75` (`Memory` 512 MB, `Time` 30 s, `Width/Height` 20.000), `Core/Photos/PhotoLimits.cs` (`MaxPixels` 50 milhões), `PhotoIngestion.cs:16` (2 conversões simultâneas).
- **Descrição:** 50 Mpx em Q8 RGBA são ~200 MB por imagem; `AutoOrient`/`Resize` criam cópias. Duas conversões em paralelo podem passar de 600–800 MB de memória nativa, acima do teto típico do pool do SmarterASP. O ImageMagick então usa disco para o excedente (`Disk` não limitado) em vez de recusar. A pedido do DAST, um PNG de 36 Mpx foi aceito (`dast-upload.txt`, caso h), sem medida de pico para ele.
- **Exploração:** conta de equipe envia duas fotos de ~49 Mpx ao mesmo tempo, repetidamente; o pool recicla (indisponibilidade) ou enche o `MAGICK_TEMPORARY_PATH`.
- **Correção sugerida:** medir o pico de RSS com 2 fotos de 50 Mpx na hospedagem real; baixar `MaxPixels` (um limite de 25–30 Mpx cobre celulares comuns, já que a saída tem no máximo 1600 × 2560) ou reduzir para 1 conversão; definir `ResourceLimits.Disk`, `Area` e `ListLength` na inicialização.
- **Confiança:** a confirmar (depende dos limites do pool).

### B-05 — Pastas de configuração só com `[Required]` (Baixo) — A05
- **Onde:** `Core/Configuration/PhotoStorageOptions.cs:11`, `KeyStorageOptions.cs`, `LogStorageOptions.cs`; validação em `Infrastructure/Configuration/OptionsExtensions.cs`.
- **Descrição:** `PhotoStorage:BasePath`, `DataProtection:KeysDirectory` e `Logging:FileDirectory` aceitam qualquer texto. Nada confere que o caminho é **absoluto**, **fora de `ContentRoot`/`WebRoot`** e gravável. Um valor relativo (ex.: `wwwroot/fotos`) colocaria `_originals/` (com GPS, B-02) dentro da pasta publicada; `FileSystemPhotoStorage.Root()` usa `Path.GetFullPath` relativo ao diretório de trabalho.
- **Exploração:** erro de configuração do operador, não ataque externo.
- **Correção sugerida:** validador de opções que exige `Path.IsPathRooted`, recusa caminho dentro de `WebRootPath`/`ContentRootPath` e testa criação/escrita na partida (em Production). Registrar no checklist do `/deploy`.
- **Confiança:** confirmado pela leitura.

### B-06 — `X-Correlation-ID` do cliente aceito (Info) — A09
- **Onde:** `Web/Middleware/CorrelationIdMiddleware.cs:19-28`.
- **Descrição:** o formato é restrito (`[A-Za-z0-9_-]{1,64}`), o que impede injeção de linha de log, mas o valor do cliente vira `TraceIdentifier`, `CorrelationId` das linhas de log e o "Código de referência" mostrado ao usuário. Um cliente pode repetir um id de outro pedido para poluir a investigação.
- **Correção sugerida:** sempre gerar o id no servidor e registrar o do cliente como propriedade separada (`ClientCorrelationId`).

### B-07 — Normalização Unicode: expansão e par substituto (Info) — A04
- **Onde:** `Core/Search/Normalizer.cs:23` (`Normalize(FormD)`), `Core/Ads/Ad.cs:123` + `Infrastructure/Data/Configurations/AdConfiguration.cs:50` (`TitleSearch` = 200), `Core/Ads/PanelAdListService.cs:20` (`typed[..100]`).
- **Descrição:** (a) `FormD` expande sílabas Hangul e outros caracteres compostos (2–3 vezes): um título de 120 caracteres pode gerar `TitleSearch` acima de 200 e o `SaveChanges` falha com "Não foi possível salvar" (genérico). (b) O corte de 100 caracteres da busca do painel pode separar um par substituto (emoji); `string.Normalize` lança `ArgumentException` para surrogate solitário. A tela cai em 503 "Não foi possível carregar" (a `Index` captura a exceção); não é falha de segurança.
- **Correção sugerida:** truncar `TitleSearch` na normalização (ou aumentar a coluna), cortar sem partir par substituto (como já faz `AdMetaDescription.Truncate`) e adicionar os dois casos à tabela de testes.
- **Confiança:** a confirmar com teste.

### B-08 — `$` em regex de forma de chave (Info) — A03
- **Onde:** `Infrastructure/Photos/FileSystemPhotoStorage.cs:25-44` (`StorageKeyShape`, `OriginalKeyShape`, etc.).
- **Descrição:** em .NET, `$` casa antes de um `\n` final: `"1/<guid>\n"` passa. Hoje as chaves vêm só do banco e do código (`$"{adId}/{Guid:N}"`), então não há caminho de exploração; o `Confine` continua conferindo o caminho final.
- **Correção sugerida:** trocar `$` por `\z` nas sete expressões (defesa em profundidade).

### B-09 — Cabeçalhos adicionais (Info) — A05
- **Onde:** `Web/Middleware/SecurityHeadersMiddleware.cs:12-30`.
- **Descrição:** presentes: `nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy`, CSP (`script-src 'self'`, `style-src 'self'`, `frame-ancestors 'none'`, `form-action 'self'`), HSTS em Production sobre HTTPS. Faltam `Cross-Origin-Resource-Policy: same-site` (evita incorporar `/fotos/*` em outro site) e `Cross-Origin-Opener-Policy: same-origin`; o link de redefinição de senha (`?code=` na URL) poderia sair com `Referrer-Policy: no-referrer` na própria página. `base-uri`/`object-src` na CSP: **já conhecido: R-37**.

### B-10 — `HttpClient` sem endurecimento (Info) — A10
- **Onde:** `Infrastructure/ServiceCollectionExtensions.cs:82-86,121`; `Location/ViaCepLookup.cs:37`.
- **Descrição:** o host é fixo (CEP só com 8 dígitos ASCII, `CepRules.IsValid`; `BaseUrl` só por configuração), logo não há SSRF por entrada de usuário. Mesmo assim o `HttpClient` segue redirecionamentos (por padrão) e `ReadAsStringAsync` não tem teto; uma resposta enorme ou um 302 do terceiro (ou de um MITM) consumiria memória ou desviaria a chamada. Correção: `AllowAutoRedirect = false`, `MaxResponseContentBufferSize` (ex.: 64 KB) e `localidade` ≤ 80 (**R-27 já conhecido**).

### B-11 — Arquivos estáticos desnecessários publicados (Info) — A05
- **Onde:** `Web/wwwroot/lib/` (jQuery, jquery-validation, jquery-validation-unobtrusive sem uso: `_ValidationScriptsPartial.cshtml` não é incluída por nenhuma view), `*.map`, `lib/LEIAME.md`.
- **Descrição:** serve versões e procedência (fingerprinting) e aumenta a superfície se uma biblioteca tiver CVE futuro. Junto com **V-06** (publicação com `.pdb`/`.xml`). Correção: remover o que não é usado do `wwwroot` e excluir `*.map`/`LEIAME.md` da publicação.

### B-12 — `LoginFailureCounter` (observação para o escopo A) (Info) — A07
- **Onde:** `Web/Security/LoginFailureCounter.cs:44-52`.
- **Descrição:** acima de 1024 origens, cada nova falha copia e varre todas as chaves sob um `lock` único; com rotação de IPv6 (**R-38**) o custo cresce com o número de origens. Sem impacto sem esse bypass.

---

## Já conhecido (não repetido como achado)

| BACKLOG | Onde apareceu na leitura |
|---------|--------------------------|
| R-11 / SEC-01 `KnownProxies` | `Security/ForwardingExtensions.cs:40-43` |
| R-20 situação do anúncio fora do `UPDLOCK` | `Photos/AdPhotoService.cs:72-131` |
| R-27 `localidade` sem limite de 80 | `Location/ViaCepLookup.cs:79` |
| R-28 espaços Unicode literais, cultura repetida | `Core/Formatting/PriceText.cs` |
| R-29 limpeza sem trava de proporção | `Photos/OriginalsCleanupService.cs` |
| R-32 antiforgery antes do `RequestSizeLimit` na `/api` | `Filters/AntiforgeryJsonFilter.cs:22,36` (só autenticado: `UseAuthorization` recusa antes) |
| R-33 400/413 fora do contrato | `Middleware/BodyLimitMiddleware.cs:24-27`, `Program.cs` |
| R-35 `CategoriesController` ignora `ModelState` | `Areas/Panel/Controllers/CategoriesController.cs:60-63` |
| R-36 `Server`/`X-Powered-By` | `Program.cs`, `web.Production.config.example` |
| R-37 CSP sem `base-uri`/`object-src` | `Middleware/SecurityHeadersMiddleware.cs:12-14` |
| R-38 limite por IPv6 completo | `Security/RateLimitingExtensions.cs:133` |
| R-39 chaves de teste valem em Production; `[Url]` aceita `http`/`ftp` | `Core/Configuration/SiteOptions.cs`, `SendGridOptions.cs`, `ViaCepOptions.cs` |
| R-46 `AllowedHosts: *` (Checkpoint 5, item 7) | `appsettings.json:11` |
| V-03 `/Home/Error` abre direto | `Controllers/HomeController.cs:73` |
| V-06 `.pdb`/`.xml` na pasta publicada | publicação |
| BACKLOG L174 cache público de foto retirada (risco aceito pelo PO) | `Controllers/PhotosController.cs:41` |
| BACKLOG L239/L240/L276/L296(21) busca por `CHARINDEX` e `/sitemap.xml` sem cache (custo por pedido anônimo) | `Search/SearchReadRepository.cs:64`, `Seo/SitemapReadRepository.cs` |
| BACKLOG L319 JSON com escape relaxado | MVC padrão |
| BACKLOG L430 chaves do Data Protection em XML sem criptografia em repouso | `Program.cs` (`FileSystemXmlRepository` sem `ProtectKeysWith…`) |
| BACKLOG L107/L113 arquivos órfãos / datas | `AdPhotoService.cs`, `FileSystemPhotoStorage.cs` |

---

## Áreas conferidas e limpas (com evidência)

### Fotos (RC-2, RC-4, RC-6, polyglots, SVG/HEIC)
- **Formato decidido pelo conteúdo, nunca pela extensão/nome:** `Core/Photos/PhotoSignature.cs:27-52` (JPEG `FF D8 FF`, PNG, GIF, WebP `RIFF…WEBP`, HEIC pela caixa `ftyp` com marcas explícitas; AVIF recusado). `file.FileName` nunca é usado no servidor (só aparece no cliente por `textContent`, `photos.js:105`). DAST `dast-upload.txt` (c,d,e,f,g) confirma: MZ como `.jpg` → 400; nome `../../etc/passwd.png` → 201 sem efeito no caminho; SVG com script → 400; PNG + PHP/JS anexado → reencodado.
- **Decodificador forçado e política restritiva:** `MagickImageProcessor.cs:21` (`settings.Format = ToMagick(format)`), `policy.xml:5-10` (coder `none` em `*`, só `JPEG,JPG,PNG,GIF,WEBP,HEIC,HEIF`; `delegate`, `filter` e `path @*`/`|*` em `none`), aplicada em `MagickRuntime.cs:69`. MVG, MSL, SVG, URL, HTTP, TEXT, EPHEMERAL e `label:`/`@arquivo` ficam desligados; entrada vem de `byte[]`, sem caminho de arquivo.
- **Limites de pixels/tempo/memória antes da decodificação (decompression bomb, RC-2):** `MagickImageProcessor.cs:29-34` (`MagickImageInfo` lê só o cabeçalho; `MaxSide` 20.000 e `MaxPixels` 50 milhões), `MagickRuntime.cs:72-75` (`Memory` 512 MB, `Time` 30 s, `Width`/`Height`), `MagickResourceLimitErrorException` tratada em `MagickImageProcessor.cs:63`. Animados viram o quadro 0 (`FrameCount = 1`). Corte detectado por avisos (`IsTruncation`). DAST caso (j): PNG 20000×20000 recusado em 26 ms.
- **Metadados:** `AutoOrient` antes de `Strip()` (`MagickImageProcessor.cs:47-49`); perfil de cor convertido para sRGB e descartado. As versões servidas não têm EXIF/GPS (o original sim, ver B-02).
- **Tamanho e quantidade (RC-6, RC-21):** `PhotoIngestion.cs:73-96` (lê no máximo 10 MB + 1 byte, recusa antes de carregar), `Slots` de 2 conversões (`PhotoIngestion.cs:16,61`), `[RequestSizeLimit(11 MB)]` (`AdPhotosController.cs:34`, `AdPhotoPagesController.cs:30`), `BodyLimitMiddleware.cs:22-37` (413 por `Content-Length`; 1 MB nas demais rotas, inclusive sem `Content-Length`), 30 envios/min por usuário (`RateLimitingExtensions.cs`), limite de fotos por categoria dentro da transação (`AdPhotoService.cs:107-131`, `UPDLOCK`).
- **Caminho de arquivo (RC-4):** chaves só de ids numéricos e GUIDs gerados (`PhotoIngestion.cs:33`), regex de forma (`FileSystemPhotoStorage.cs:25-44`), `Confine` com `GetFullPath` + prefixo da pasta base (`:345-358`), atalhos (`ReparsePoint`) nunca seguidos (`:245,268`), `Delete(path)` só para nomes gerados (`:209`). Nenhum nome vindo do usuário entra no caminho. DAST `dast-photos-serve.txt`: `..%2f`, `%00`, `/../appsettings.json`, `-orig.webp` → 404.
- **Entrega e enumeração (`PhotosController`/`PhotoDelivery`):** rota `{adId:int}/{photoId:int}-{size}.webp` com `size` só `1600`/`480` (`PhotosController.cs:27-33`); a foto precisa pertencer ao anúncio da rota; não publicado + não autor/admin = `null` = **404 idêntico ao de "não existe"** (`PhotoDelivery.cs:20-35`); `Cache-Control: private, no-store` para não público (`:41`), `nosniff` global, conteúdo sempre WebP reencodado. Limite próprio de 300/min por IP.
- **Limpeza:** só apaga nomes gerados, 24 h de carência para órfãos, nunca versão registrada (`OriginalsCleanupService.cs:106-185`).

### Banco (Dapper, EF, SQL)
- **Sem concatenação de SQL com dado de usuário:** busca por `grep` em `src/` de `FromSql*`, `ExecuteSql*`, `SqlQuery`, `$@"`, `string.Format` → só `AdPhotoService.cs:152` (`SqlQuery<int>($"… WHERE Id = {adId}")` é `FormattableString` parametrizada pelo EF, com `adId` inteiro) e `HasComputedColumnSql` com caminho constante (`AdConfiguration.cs:57`).
- **`SqlBuilder`:** fragmentos fixos, valores só por parâmetro nomeado (`SqlBuilder.cs:50-60`); `Validate` recusa aspas, `;`, `--`, `/*` e número solto (`:161-186`); ordenação só por lista permitida (`OrderBy`, `:68-82`); `Page` limita a 100 e calcula o deslocamento em 64 bits (`:84-96`). Nenhum fragmento recebe texto do pedido: palavras da busca vão como `@Word0…@Word4` (`SearchReadRepository.cs:64`), faixas por constantes (`:118-130`).
- **LIKE sem escape (RC-15):** não há `LIKE`; a busca pública e a do painel usam `CHARINDEX(@termo, coluna)` (`SearchReadRepository.cs:64`, `PanelAdListReadRepository.cs:70`), então `%`, `_` e `[` valem como texto. Sem `EF.Functions.Like` em `src/`.
- **`ORDER BY` dinâmico:** `OrderOf(SearchOrder)` é um `switch` sobre enum com texto fixo (`SearchReadRepository.cs:45-50`).
- **TRY_CAST/JSON:** `TRY_CAST(JSON_VALUE([Attributes], '{path}') AS tipo)` com `path` de constante (`AdConfiguration.cs:54-57`); texto em campo numérico recusado na entrada (`FieldValueParser`, R-69 da linha 69 do BACKLOG, resolvido).
- **Tempo limite:** `commandTimeout: 10` em todas as leituras Dapper (`AdCardSql.cs:12,24`, `PanelAdListReadRepository.cs:20`, `SitemapReadRepository.cs:14`); EF com o padrão de 30 s.
- **`DecimalInput`/`FavoriteIds`:** dígitos só `0-9`, sem `OverflowException` (`DecimalInput.cs:61-70`), no máximo 100 ids (`FavoriteIds.cs`).
- **`\d` Unicode:** `FieldValueParser.cs:20,23` usa `\d`, mas `decimal.TryParse` recusa o que não é `0-9` e devolve a mensagem de campo (sem 500). `PriceText` já usa `[0-9]` (R-04 resolvido).

### Saída HTML (Razor, e-mails, SEO)
- **Nenhum `Html.Raw`, `HtmlString` nem `IHtmlContent` em `src/`** (grep em `.cshtml` e `.cs`); o único `TagBuilder` (`_AdBody.cshtml:8`) usa `InnerHtml.Append(string)`, que codifica. Nenhum `style=`, `onclick`, `<script>` inline nem `javascript:`; `@:` só em `Categories/Index.cshtml` com texto fixo.
- **Atributos sempre entre aspas** (grep de `=@x` sem aspas: 0 resultados). Texto do usuário em `value=`, `data-title`, `alt`, `aria-label`, `og:*` e `<title>` sai codificado (`_Layout.cshtml:6,36`, `_Seo.cshtml`, `_AdGallery.cshtml:13,16`, `Default.cshtml (AdCard):6,19`).
- **URLs montadas no servidor:** `href` vêm de `PublicRoutes`/`AdRoutes` (slug ASCII, `SlugGenerator.cs:50-62`) e de `PhotoUrls` com inteiros; "Tentar novamente" só renderiza com `Url.IsLocalUrl` (`_ErrorState.cshtml:10`); `RedirectPermanent` do anúncio usa só o caminho canônico + querystring (`AdController.cs:36-39`); `ReturnUrl` do login usa `IsLocalUrl` (`AccountController.cs:218`). Link do WhatsApp: número validado + mensagem com `Uri.EscapeDataString` (`WhatsAppLink.cs:40`); `tel:` só com dígitos.
- **SEO:** `canonical`/`og:*` por `Site:BaseUrl` (obrigatório em Production); sem JSON-LD no código; `sitemap.xml` escrito por `XmlWriter` (escape automático) com slugs ASCII; `robots.txt` fixo (`SeoController.cs`, `Sitemap.cs`).
- **E-mail:** assunto constante, destinatário e corpo vão no JSON da API do SendGrid (sem montar cabeçalho SMTP, logo **sem injeção de CRLF**), HTML com `WebUtility.HtmlEncode` em nome e link (`PasswordRecoveryMailer.cs:57-58`), chave só no `Authorization` (`SendGridEmailSender.cs:46`), log só do destinatário mascarado.
- **Mensagens de erro:** produção usa `UseExceptionHandler("/Home/Error")` e `ExceptionHandlingMiddleware` com texto genérico; a pilha só vai ao log; `DatabaseAndMigrationHealthCheck` devolve só `Healthy/Unhealthy` (sem detalhe), com o tipo da exceção no log (`:31-36`).

### JavaScript (RC-17)
- `grep` em `wwwroot/js/**`: **nenhum** `innerHTML`, `outerHTML`, `insertAdjacentHTML`, `document.write`, `eval`, `new Function`, `postMessage`, `window.open`, `location =`. Texto vem por `textContent`/`createElement`/`new Option` (`photos.js:105,115`, `ad-card.js`, `favorites-ui.js:13`, `ad-edit.js:163-168`, `catalog-chain.js`, `search.js:30-37`). Os dois `DOMParser` (`favorites.js:41`, `ad-edit.js:97`) leem HTML do próprio servidor (parciais Razor com saída codificada) e movem os nós; scripts do `DOMParser` não executam.
- `localStorage` guarda só ids de favoritos (`favorites.js:6`, não sensível), validados na leitura (`limpar`); `fetch` de escrita só em `apiFetch`, com `RequestVerificationToken` (`api.js:30-39`) e `credentials: "same-origin"`; sem redirecionamento no cliente; sem mesclagem de objeto vindo de JSON (`Object.assign(novo, {id,name})` do DOM, `ad-edit.js:246`) → sem poluição de protótipo.

### Cabeçalhos, CSP, CORS, cache, compressão
- Cabeçalhos em toda resposta pelo `OnStarting` (`SecurityHeadersMiddleware.cs:20-35`); CSP sem `unsafe-inline`; **nenhuma política CORS** (mesma origem); antiforgery com cookie `HttpOnly`, `Secure=Always`, `SameSite=Strict` (`Program.cs:56-61`); cookie de equipe `HttpOnly/Secure/Lax` (`IdentityExtensions.cs`).
- **Cache autenticado:** `PanelControllerBase` com `NoStore` (`:14`), `AccountController`/`PasswordController` também; `/Home/Status` e `/Home/Error` com `NoStore`; fotos não públicas `private, no-store`. Estáticos com `?v=` recebem `immutable` só se forem `StaticAssetDescriptor` (`PerformanceExtensions.cs:56-64`).
- **BREACH:** compressão desligada para `/painel` e para páginas de erro reexecutadas que nasceram no painel (`PerformanceExtensions.cs:39-52`); o site público e a API não refletem segredo.
- **Swagger/diagnóstico:** nenhum `MapOpenApi`/Swagger; página de exceção do desenvolvedor só em Development; `ComponentsController` com `[DevelopmentOnly]`. Health checks sem detalhe.
- **ForwardedHeaders:** só `XForwardedFor|XForwardedProto`, `ForwardLimit = 1`, `KnownProxies` vazio por padrão e o middleware nem entra no pipeline sem proxies (`ForwardingExtensions.cs:22-43`); `XForwardedHost` não é aceito.

### Serviços externos e logs
- **ViaCEP:** CEP validado como 8 dígitos ASCII (`CepRules.cs:15-28`), URL relativa a um host fixo por configuração (HTTPS), timeout de 5 s, só `localidade/uf/ibge/erro` são lidos, `JsonException` tratada, resposta e erro nunca vão ao usuário. Sem SSRF por entrada (ver B-10 para endurecimento).
- **SendGrid:** JSON, sem cabeçalho montado com texto do usuário.
- **Logs:** `MaskingEnricher` mascara nomes sensíveis, e-mails e `token|code|key=` (limite conhecido: pilha de exceção, documentado). O `CorrelationIdMiddleware` restringe o formato (`:41`).

### Configuração e segredos
- `appsettings.json` só tem `Logging`, `Authentication:SessionMinutes` e `AllowedHosts`; `appsettings.Development.json` só `Logging`; `web.Production.config.example` tem apenas marcadores `(…)` e instrução de que o real não vai ao git. `grep` de `Password=|ApiKey|SG\.|AKIA|BEGIN PRIVATE|TrustServerCertificate` em `src/`: só `AppDbContextFactory.cs:17` (fábrica de tempo de projeto, `Server=(local)`, `Trusted_Connection`, `TrustServerCertificate=True`; **não** é usada em runtime, já listada nos 0% de cobertura) e documentos/testes com valores falsos. `UserSecretsId` presente no `.csproj` (sem segredo no repositório).
- `AddAppOptions` valida na partida em Production (`OptionsExtensions.cs:25-33`): conexão, fotos, logs, chaves, SendGrid e `Site:BaseUrl`; `Authentication:SessionMinutes` e `ViaCep` em qualquer ambiente. Data Protection com pasta persistente obrigatória em Production (`Program.cs:90-100`). Sem `Database.Migrate()` na partida (migração por script idempotente).
- Bibliotecas de front: Bootstrap 5.3.8, jQuery 3.7.1, jquery-validation 1.21.0 (sem CVE conhecido nas versões; jQuery/validation nem são carregados). `Magick.NET-Q8-x64` 14.17.2, `Microsoft.Data.SqlClient` 6.1.6, `Dapper` 2.1.89: recomenda-se manter o Magick.NET em dia (decodificadores nativos, inclusive libheif/lcms2 que leem arquivo e perfil ICC enviados por conta de equipe); a verificação de CVE é do `/scan` automatizado (`security/dependency-audit`).

## Próximos passos sugeridos (prioridade)
1. **B-01** (Médio): limite/rolagem do arquivo de log, amostragem do aviso 429 e `matchTimeout` no `MaskingEnricher` (antes do `/deploy`).
2. **B-02/B-03** (Baixo): decidir com o Product Owner sobre não guardar (ou limpar de GPS) os originais e sobre uma cota de disco; ajustar a ADR-005.
3. **B-04/B-05** (Baixo): medir memória com 2 fotos grandes na hospedagem e validar as pastas de configuração na partida; incluir no checklist do `/deploy`.
4. Itens Informativos na manutenção (B-06 a B-12).
