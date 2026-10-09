# Verificação pós-publicação (produção)

> **Em resumo:** este é o roteiro, na ordem, para conferir em **`https://www.gzto.com.br`** que o site publicado funciona: primeiro três conferências rápidas de cabeçalhos e de scripts do Cloudflare (Parte 0), depois 20 testes manuais (Parte 1). Cada teste diz **o que fazer, o que deve acontecer, o que fazer se falhar e onde olhar o log**. Quem executa é o Product Owner; quando algo falhar, anote o número do item e **mande o trecho do log para o Claude** em vez de tentar corrigir. Complementa o [`GO-LIVE-CHECKLIST.md`](GO-LIVE-CHECKLIST.md) (que é o roteiro do dia da publicação) e o [`VERIFY-CHECKLIST.md`](VERIFY-CHECKLIST.md) (verificação manual por funcionalidade).

**Termos usados:** *log do dia* é o arquivo mais recente da pasta `h:\root\home\mpaulohs-001\www\gazeta-logs` (um arquivo por dia, acessível pelo FTP ou pelo gerenciador de arquivos do painel do SmarterASP); *CSP* é a política de segurança de conteúdo, que só deixa a página carregar scripts e estilos do próprio site; *DNS only* é o registro do Cloudflare com a **nuvem cinza** (sem proxy); *Redator* e *Administrador* são os dois papéis da equipe.

## Como usar

- Faça na ordem. Marque `[x]` ao passar.
- **Como ler o log:** abra o arquivo do dia em `gazeta-logs` e procure o texto indicado em cada item (`Ctrl+F`). Linhas importantes: `Modo Cloudflare ligado`, `Falha de entrada`, `E-mail enviado para`, `Falha ao enviar o e-mail`. Os e-mails aparecem mascarados (`a***@exemplo.com.br`): é proposital.
- **Nunca** cole o conteúdo do `web.Production.config`, senhas, chaves de API nem o conteúdo dos arquivos `key-*.xml` numa conversa. Cole só as linhas de log (que já saem mascaradas).
- **Fotos de teste:** use 3 fotos suas: uma JPEG, uma PNG e uma HEIC de iPhone (com a localização ligada na câmera, para o item 6 provar algo).

---

## Parte 0 — Antes do item 1: cabeçalhos, scripts do Cloudflare e endereço sem `www`

Rode no seu computador (terminal bash, PowerShell ou Prompt de Comando com `curl`). São só leituras da página.

### 0.1 Cabeçalhos de segurança e redirecionamento de `http`

```bash
curl -sI https://www.gzto.com.br/
curl -sI http://www.gzto.com.br/
```

| Conferir | Esperado | Se falhar | Onde olhar |
|---|---|---|---|
| `curl -sI https://www.gzto.com.br/` mostra `HTTP/... 200` | Página inicial responde | `525`/`526` = certificado de origem (Origin CA) ou modo Full (strict) do Cloudflare; `502`/`503` = o site não subiu | Painel do Cloudflare (SSL/TLS); log do dia |
| `strict-transport-security: max-age=31536000; includeSubDomains` | Presente | **Ausente = o site acha que a conexão não é `https`** (cabeçalho só sai quando `Request.IsHttps`). Isso é o risco do `OutOfProcess` (Parte 1, item 15). Mande o resultado ao Claude, não corrija sozinho | Log do dia; esta saída do `curl` |
| `content-security-policy: default-src 'self'; img-src 'self' data:; script-src 'self'; style-src 'self'; frame-ancestors 'none'; form-action 'self'` | Presente, igual a este texto | Texto diferente = o Cloudflare ou o IIS alterou o cabeçalho | Esta saída |
| `x-frame-options: DENY`, `x-content-type-options: nosniff`, `referrer-policy: strict-origin-when-cross-origin`, `permissions-policy: geolocation=(), microphone=(), camera=()` | Presentes | Qualquer um ausente: anote qual | Esta saída |
| `curl -sI http://www.gzto.com.br/` | `301` ou `308` com `location: https://www.gzto.com.br/` | Sem redirecionamento: ligue **Always Use HTTPS** no Cloudflare (SSL/TLS → Edge Certificates). **Laço de redirecionamentos** (o navegador diz "redirecionou muitas vezes"): o modo do Cloudflare está **Flexible**; troque para **Full (strict)** | Painel do Cloudflare |
| Um só `content-encoding` (`br` ou `gzip`) | Um único valor | Duplicado: compressão dupla no IIS (item M7 do `VERIFY-CHECKLIST.md`) | Esta saída |

### 0.2 Scripts que o site não escreveu (Cloudflare)

O site só carrega **dois** scripts, ambos dele: `/lib/bootstrap/dist/js/bootstrap.bundle.min.js` e `/js/pages/layout.js` (com `?v=...`). Qualquer outro `<script>` no HTML foi **injetado pelo Cloudflare**.

```bash
curl -s https://www.gzto.com.br/ | grep -i -E "cdn-cgi|cloudflareinsights|<script"
```

| Conferir | Esperado | Se falhar | Onde olhar |
|---|---|---|---|
| A saída tem **só duas** linhas com `<script`, as dos dois arquivos acima | Nenhuma linha com `cdn-cgi` nem `cloudflareinsights` | Linha com `cloudflareinsights` = **Web Analytics** automático ainda ligado (Cloudflare → Analytics & Logs → Web Analytics → desligar o site). Linha com `/cdn-cgi/scripts/.../email-decode` = **ofuscação de e-mail** (Cloudflare → Scrape Shield → Email Address Obfuscation → Off). Script com `rocket-loader` ou atributo `data-cf-settings` = **Rocket Loader** (Speed → Optimization → Content Optimization → Rocket Loader → Off). `/cdn-cgi/challenge-platform/...` = detecção de bots (Security → Bots); fica na mesma origem e **não** é bloqueada pela CSP, mas confira o console | Console do navegador (F12 → Console): bloqueios mostram `Refused to load the script ... Content Security Policy` |
| Ligue **depois de cada ajuste** no Cloudflare | O Cloudflare guarda cópia; use *Caching → Purge Everything* e repita o `curl` | Mesma saída depois do purge: o recurso continua ligado | Painel do Cloudflare |

### 0.3 Endereço sem `www` e `Site__BaseUrl`

O endereço público é **`https://www.gzto.com.br`**. Os links de e-mail, o mapa do site e a pré-visualização de link usam o `Site__BaseUrl`.

| Conferir | Esperado | Se falhar | Onde olhar |
|---|---|---|---|
| `Site__BaseUrl` no `web.Production.config` é exatamente `https://www.gzto.com.br` (com `https://`, **com `www`**, sem barra no fim) | Igual | Valor com `http://`, sem `www` ou com barra: os links do e-mail de redefinição e o `og:image` saem errados | `web.Production.config` (cofre de senhas) |
| `curl -sI https://gzto.com.br/` (sem `www`) | `301` com `location: https://www.gzto.com.br/` | Sem redirecionamento: crie a **regra do sem-www** no Cloudflare (Rules → Redirect Rules → *Redirect from root to WWW*: se o nome do host for `gzto.com.br`, redirecionar para `https://www.gzto.com.br` + o mesmo caminho, status 301, preservando a consulta). O registro DNS do `gzto.com.br` precisa estar **proxiado** (nuvem laranja) para a regra valer | Painel do Cloudflare (DNS e Rules) |
| `curl -s https://www.gzto.com.br/robots.txt` cita `https://www.gzto.com.br/sitemap.xml` | Endereço completo com `www` | Citar outro endereço: `Site__BaseUrl` errado | `robots.txt`; `web.Production.config` |

---

## Parte 1 — Os 20 testes manuais

### 1. Login do Administrador (bootstrap)

- **Testar:** abra `https://www.gzto.com.br/painel/entrar` e entre com `Bootstrap__AdminEmail` e `Bootstrap__AdminPassword`.
- **Esperado:** o site obriga a **trocar a senha provisória**; depois do primeiro acesso, remova `Bootstrap__AdminEmail` e `Bootstrap__AdminPassword` do `web.Production.config` e reinicie o site (ADR-003). O menu do painel mostra Anúncios, Categorias, Usuários e Configurações.
- **Se falhar:** "E-mail ou senha inválidos, ou conta desativada" na primeira tentativa: o Administrador inicial não foi criado, ou as duas variáveis estavam erradas na partida. Veja a partida do log. Página branca ou erro 500: log do dia.
- **Log:** `Falha de entrada de ... a partir de <IP>` (senha errada); linha de partida do dia (Administrador inicial criado, ou aviso de variáveis).

### 2. Criar um Redator

- **Testar:** `/painel/usuarios` → novo usuário, papel **Redator**, e-mail seu alternativo. Guarde a senha provisória.
- **Esperado:** o Redator aparece na lista. Entrando com ele, o painel mostra só "Meus anúncios" (sem Usuários, Categorias nem Configurações).
- **Se falhar:** sem o botão de novo usuário: você não está como Administrador. Erro ao salvar: log do dia.
- **Atenção ao endereço:** o site monta links usando `Site__BaseUrl`; confirme o item 0.3 (`https://www.gzto.com.br`, com `www`).
- **Log:** `Falha de entrada` e linhas de auditoria do usuário criado.

### 3. Informar o telefone do site

- **Testar:** `/painel/configuracoes` → telefone do site (um celular seu, com DDD).
- **Esperado:** salvo com a mensagem de sucesso; é o número que o "Ligar" e o WhatsApp dos anúncios usam (item 11).
- **Se falhar:** mensagem de campo inválido: confira DDD e dígitos. Erro 500: log do dia.
- **Log:** auditoria de alteração de configuração.

### 4. Criar anúncio em cada grupo

Para cada categoria: `/painel/anuncios/novo` → escolher a categoria → preencher os campos obrigatórios → **Salvar rascunho**.

| Grupo | Categoria de teste | O que conferir |
|---|---|---|
| Carros | qualquer de Carros | Marca → Modelo → Ano → Versão se liberam em cadeia. **Se a lista de marcas vier vazia, é esperado enquanto a carga real do catálogo de veículos não foi feita** (falta exportar e aplicar; `db/seed/README.md`) |
| Serviços | qualquer de Serviços | Tipo de serviço; **sem preço** é aceito |
| Vagas | qualquer de Vagas | Salário; **sem foto** é aceito |
| Imóveis | qualquer de Imóveis | Área, quartos, condomínio/IPTU com máscara de dinheiro |
| Roupas | qualquer de Roupas | Tamanho e gênero (listas fechadas) |
| Eletrônicos | qualquer de Eletrônicos | Marca em texto livre, estado do produto |

- **Esperado:** campos mudam ao trocar a categoria (anúncio "Campos atualizados para a categoria ..."); máscara de preço (`R$ 62.000,00`) ao digitar; CEP (`13015-100`) preenche cidade e UF; o rascunho é salvo e reabre com os dados.
- **Se falhar:** campos que não mudam ou máscara que não funciona = o JavaScript do site está sendo bloqueado ou alterado: repita o item 0.2 e veja o console (F12). CEP sem resposta: o servidor da hospedagem não alcança o ViaCEP; o site oferece Cidade/UF manuais (não bloqueia). Lista de cidades vazia: a carga de municípios (IBGE) ainda não foi feita (BACKLOG).
- **Log:** `Warning`/`Error` em horário do teste; erros do CEP aparecem como falha do `HttpClient` do ViaCEP.

### 5. Upload de foto (JPEG, PNG, HEIC)

- **Testar:** num rascunho, envie as 3 fotos (arrastar ou botão).
- **Esperado:** as três aparecem como miniatura; a HEIC vira imagem normal; um arquivo que não é imagem (um PDF) é recusado com mensagem clara; a 21ª foto recebe "Cada anúncio pode ter no máximo 20 fotos". Troque a capa e remova uma foto.
- **Se falhar:** "Não foi possível converter esta foto HEIC": o componente nativo do ImageMagick não carregou no Windows do provedor (AR-05). Todas as fotos falham: a pasta `gazeta-fotos` pode estar sem permissão de escrita, ou falta o componente do Visual C++ no servidor (abra chamado no SmarterASP citando **Magick.NET Q8 x64**). Foto enviada mas sem miniatura: `/fotos/...webp` bloqueado pelo Cloudflare (Cache Rules ou WAF).
- **Log:** `Error` com `Magick` ou `PhotoIngestion`; arquivos novos em `gazeta-fotos`.

### 6. Verificar GPS removido da foto

- **Testar:** no anúncio, clique com o botão direito numa foto, salve a imagem (`.webp`) e rode `exiftool foto.webp | grep -i gps` (no Windows: `exiftool foto.webp | findstr /i gps`). Use a foto HEIC ou JPEG **com localização**.
- **Esperado:** **nenhuma linha** de GPS.
- **Se falhar:** **sério (privacidade, LGPD)**. Despublique o anúncio de teste, anote o formato da foto e avise o Claude antes de seguir.
- **Log:** nenhum; é uma conferência do arquivo.

### 7. Enviar para revisão

- **Testar:** com o Redator, num anúncio completo, **Enviar para revisão**; num incompleto, deixe um campo obrigatório vazio e envie.
- **Esperado:** o completo vira "Em revisão"; o incompleto mostra "Faltam N itens..." com links para cada campo.
- **Se falhar:** o botão não faz nada: JavaScript bloqueado (item 0.2). Erro 500: log do dia.
- **Log:** auditoria de mudança de situação.

### 8. Fila de revisão (Administrador)

- **Testar:** com o Administrador, `/painel/anuncios/fila`.
- **Esperado:** o anúncio enviado aparece; a pré-visualização abre o anúncio como o público veria, com o aviso de pré-visualização.
- **Se falhar:** fila vazia com anúncio "Em revisão" no painel do Redator: log do dia. 403: papel errado.
- **Log:** `Acesso negado` (registrado em `Warning`) se o papel estiver errado.

### 9. Publicar, rejeitar, despublicar e arquivar

- **Testar:** publicar um anúncio; rejeitar outro (com motivo); despublicar um publicado; arquivar um.
- **Esperado:** publicado aparece na home e na categoria; rejeitado volta ao Redator com o motivo; despublicado e arquivado **somem** da home, da busca, dos favoritos e do mapa do site; o endereço do arquivado mostra "Este anúncio não está mais disponível" e `<meta name="robots" content="noindex">`.
- **Se falhar:** o anúncio some e volta: cache do Cloudflare no HTML (o HTML não deve ser guardado; confira que não há regra "Cache Everything"). Erro 500: log do dia.
- **Log:** auditoria de cada mudança de situação.

### 10. Página pública (início, categoria, busca, detalhe)

- **Testar:** `/`, `/categoria/<slug>`, `/busca?...`, `/anuncio/<id>/<slug>` (de um anúncio publicado).
- **Esperado:** páginas em português, sem rolagem lateral, fotos carregando; o detalhe mostra galeria, preço e campos da categoria; o título da aba muda por página.
- **Se falhar:** estilo ou fonte quebrados: CSS bloqueado (console, item 0.2). Fotos quebradas: item 5. Página branca: log do dia.
- **Log:** `Error`/`Warning` no horário; `404` em `/fotos/...` indica foto faltando na pasta.

### 11. Contato (Ligar, WhatsApp)

- **Testar (celular):** num anúncio publicado, **Chamar no WhatsApp** e **Ligar**.
- **Esperado:** o WhatsApp abre com o telefone do site (item 3) e a mensagem `Olá! Tenho interesse no anúncio “<título>”: <endereço>`; "Ligar" disca `+55` e o número.
- **Se falhar:** número errado: confira o item 3. O endereço da mensagem com outro domínio: confira `Site__BaseUrl` (item 0.3).
- **Log:** nenhum.

### 12. Favoritos

- **Testar:** toque no coração de dois anúncios, abra `/favoritos`, feche e abra o navegador, volte a `/favoritos`.
- **Esperado:** os dois continuam; "Favoritos (2)" no cabeçalho; despublicar um deles o tira da lista. Os favoritos ficam só no navegador.
- **Se falhar:** não guarda: navegador em aba anônima ou `localStorage` bloqueado. Lista vazia com favoritos marcados: erro na rota `/favoritos/lista` (log do dia).
- **Log:** `Warning`/`Error` em `favoritos`.

### 13. Busca com filtros

- **Testar:** `/busca` com texto, categoria, faixa de preço, estado/cidade e filtros de Carros (marca, ano).
- **Esperado:** resultados coerentes e paginados; "sem resultados" com mensagem amigável; o painel de filtros abre e fecha no celular; ordenação funciona.
- **Se falhar:** erro 503 ou página de erro em alguma combinação de filtros: anote os filtros usados; lista de modelos vazia depois de escolher a marca: catálogo de veículos não carregado (item 4).
- **Log:** `Error` com `Search`.

### 14. Recuperação de senha (SendGrid)

- **Testar:** saia, abra `/painel/esqueci-minha-senha`, informe o e-mail do Redator criado no item 2 (precisa ser um e-mail seu), abra o e-mail, siga o link e defina a senha nova. Repita o link: deve ser recusado (vale 1 vez).
- **Esperado:** mensagem neutra na tela ("se existir conta, enviaremos..."); e-mail chega em minutos, **fora do spam**, com o link `https://www.gzto.com.br/painel/redefinir-senha?...` (com `www`).
- **Se falhar:** o e-mail não chega: procure no log `Falha ao enviar o e-mail de redefinição de senha` (o envio falhou) ou `E-mail enviado para` (o SendGrid aceitou; veja o spam e o painel **Activity** do SendGrid). Causas comuns, nesta ordem: (a) domínio remetente não autenticado no SendGrid (SPF e DKIM); (b) **os registros CNAME de autenticação do SendGrid no Cloudflare estão com a nuvem laranja: eles precisam ficar em *DNS only* (nuvem cinza)**, senão o SendGrid não valida o domínio; (c) `SendGrid__FromEmail` diferente do remetente/domínio validado; (d) chave de API sem permissão **Mail Send**. O link com domínio errado: `Site__BaseUrl` (item 0.3).
- **Log:** `Falha ao enviar o e-mail ...` (`Error`), `E-mail enviado para ...`, `Pedido de redefinição acima do limite` (limite de 4 por hora por e-mail).

### 15. Teste do IP (limite de tentativas)

- **Testar:** primeiro, no log do dia procure `Modo Cloudflare ligado: N faixas de IP do Cloudflare confiáveis`. Depois, em `https://www.gzto.com.br/painel/entrar` (pelo endereço público), **erre a senha de propósito uma vez** e procure `Falha de entrada de ... a partir de <IP>`. Pesquise "meu IP" no navegador, na mesma rede.
- **Esperado:** o `<IP>` do log é o **seu**.

| O IP no log é | Significa | O que fazer |
|---|---|---|
| **O seu** | O site leu o `CF-Connecting-IP`: está funcionando | Nada. Os limites normais (5 por 15 min) valem |
| **`127.0.0.1` (ou `::1`)** | Com a hospedagem `OutOfProcess` o pedido chega ao site por loopback e o IP do Cloudflare/visitante se perdeu no caminho | **Não mexa e não grave `127.0.0.1` em `ForwardedHeaders__KnownProxies__0`:** isso faria o site aceitar um `CF-Connecting-IP` forjado por quem acessar o IP do SmarterASP direto, sem passar pelo Cloudflare. **Mande ao Claude** a linha do log, a linha `Modo Cloudflare ligado` e o resultado do item 0.1; o ajuste é de código e depende da sua autorização (BACKLOG, alta prioridade) |
| **De um servidor do Cloudflare** (confira em `cloudflare.com/ips`) | O IP não foi trocado: o modo Cloudflare não está valendo | `ForwardedHeaders__Cloudflare` precisa ser `true` no `web.Production.config` (sem ele há um `Warning` de `KnownProxies` na partida); a linha `Modo Cloudflare ligado: N faixas` precisa existir (N zero gera um `Error`); o registro DNS do `www` precisa estar com a nuvem laranja |
| **Sempre o mesmo, nem o seu nem do Cloudflare** | Há um proxy do SmarterASP entre o Cloudflare e o site | Grave o IP dele em `ForwardedHeaders__KnownProxies__0` e repita |
| Não achou a linha | Log em outra pasta ou o erro de senha não chegou ao site | Confira `Logging__FileDirectory` |

- **Se não resolver:** `ForwardedHeaders__Cloudflare=false` e reinicie: o site volta ao modo seguro (limite temporário de 20 tentativas por 15 minutos para o site inteiro). **Não divulgue o endereço** até resolver.
- **Como conferir o limite (opcional):** erre a senha 6 vezes do mesmo IP: a 6ª recebe "Muitas tentativas". Avise a equipe antes, porque o limite vale para o seu IP por 15 minutos.
- **Log:** `Modo Cloudflare ligado`, `Falha de entrada de ... a partir de <IP>`, `ForwardedHeaders:KnownProxies não está configurado` (modo desligado).

### 16. Verificar headers HTTPS (HSTS, CSP, XFO)

- **Testar:** repita o item 0.1 numa página interna (`/painel/entrar`, um anúncio) e numa foto (`/fotos/...webp`); opcional: securityheaders.com com `https://www.gzto.com.br`.
- **Esperado:** `strict-transport-security`, `content-security-policy`, `x-frame-options: DENY`, `x-content-type-options: nosniff`, `referrer-policy` em todas as respostas de página; a foto traz `cache-control: public, max-age=31536000, immutable`. O Cloudflare pode acrescentar `cf-cache-status`: nas fotos `HIT` é bom; no HTML deve ser `DYNAMIC`.
- **Se falhar:** cabeçalho ausente só em páginas de erro: anote a rota. `HSTS` ausente em tudo: caso do item 15 (esquema `http` no site). HTML com `cf-cache-status: HIT`: há regra de cache que guarda HTML; remova.
- **Log:** nenhum; é cabeçalho de resposta.

### 17. Testar em celular

- **Testar:** Android e iPhone: início, categoria, filtros, detalhe com galeria, favoritos, WhatsApp, envio de foto direto da câmera (HEIC) e login no painel.
- **Esperado:** sem rolagem lateral, texto legível sem zoom, botões grandes o bastante para o dedo, teclado numérico em preço e CEP, foto da câmera aceita e na orientação certa.
- **Se falhar:** anote o modelo, o navegador e uma captura de tela; a lista "não pula" quando as fotos chegam (se pular, é o CLS, correção).
- **Log:** `Error` no horário do envio de foto.

### 18. Testar NVDA (se possível)

- **Testar (Windows com o NVDA):** só teclado (`Tab`, `Enter`, setas): "Ir para o conteúdo" é o primeiro foco; login com senha errada anuncia o erro; formulário de anúncio lê "Faltam N itens"; janelas de confirmação devolvem o foco ao botão.
- **Esperado:** foco sempre visível, ordem lógica, erros lidos sem procurar.
- **Se falhar:** anote a tela e o elemento (itens de contorno de foco já estão no BACKLOG, R-14).
- **Log:** nenhum.

### 19. Verificar `gazeta-chaves` tem `key-*.xml`

- **Testar:** depois de abrir `/painel/entrar` (o login gera o token antiforgery), olhe a pasta `h:\root\home\mpaulohs-001\www\gazeta-chaves` pelo FTP ou pelo gerenciador de arquivos.
- **Esperado:** pelo menos um arquivo `key-<guid>.xml`. Reinicie o site (ou recicle o pool) e confirme que a **sessão do painel continua válida** e o link de redefinição enviado antes ainda funciona (prova que as chaves persistem). O DPAPI começa **desligado** (`DataProtection__ProtectWithDpapi=false`); só depois de tudo funcionar, ligue (veja o runbook, §7.2).
- **Se falhar:** pasta vazia depois do login: a pasta não tem escrita para o pool ou o caminho de `DataProtection__KeysDirectory` está errado; no log da partida procure `Error`/`Warning` de `DataProtection`. Sessão cai a cada reinício: as chaves não estão sendo gravadas ali.
- **Log:** `Warning` ou `Error` de categoria `Microsoft.AspNetCore.DataProtection` (falha ao gravar a chave).

### 20. Verificar `gazeta-logs` tem logs do dia

- **Testar:** abra a pasta `h:\root\home\mpaulohs-001\www\gazeta-logs`.
- **Esperado:** um arquivo do dia com a linha de partida e, no modo Cloudflare, `Modo Cloudflare ligado: N faixas de IP do Cloudflare confiáveis`. Até 14 arquivos, cada um com no máximo 100 MB. Um `Warning` "Failed to fetch Cloudflare IP ranges. Using embedded fallback" **é aceitável** (a hospedagem não alcançou o `cloudflare.com`; o pacote usou a cópia embutida).
- **Se falhar:** pasta vazia: sem escrita para o pool ou `Logging__FileDirectory` errado; sem o log, nenhum item acima pode ser diagnosticado. Arquivo do dia com `Error`: mande ao Claude.
- **Log:** o próprio arquivo.

---

## Resumo de decisão

- **Bloqueiam a divulgação:** Parte 0 (HSTS e redirecionamento; nenhum script estranho), itens **1, 5, 6, 14, 15, 16, 19 e 20**.
- **Corrigir logo depois:** itens 4, 7 a 13 se houver falha de tela; 17 e 18 viram correções se falharem.
- **Se o item 15 mostrar `127.0.0.1`:** o ajuste é de código e precisa da sua autorização.
