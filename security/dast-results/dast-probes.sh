#!/bin/bash
# DAST contra o artefato publicado (Production, https://localhost:5443). Somente leitura + sondas de ataque.
B=${B:-https://localhost:5443}
C="curl -sk --max-time 20"
hdr() { $C -D - -o /dev/null "$@"; }
echo "=== D1 cabeçalhos de segurança (/ , /painel/entrar, /404, /anuncio/999999) ==="
for p in / /painel/entrar /rota-que-nao-existe /anuncio/999999; do
  echo "--- $p"; hdr "$B$p" | grep -iE "^HTTP|strict-transport|x-content-type|x-frame|referrer-policy|permissions-policy|content-security|^server|x-powered|cache-control|set-cookie" ; done
echo "=== D2 acesso sem login às rotas do painel (esperado: 302 para /painel/entrar, nunca 200) ==="
for p in /painel /painel/anuncios /painel/anuncios/novo /painel/anuncios/1/editar /painel/fila /painel/usuarios /painel/categorias /painel/componentes /painel/configuracoes /painel/anuncios/1/fotos /painel/usuarios/1/editar /painel/categorias/1/editar; do
  printf "%s -> " "$p"; $C -o /dev/null -w "%{http_code} %{redirect_url}\n" "$B$p"; done
echo "=== D3 POST sem login e sem antiforgery (esperado 400/302/401, nunca 200/204) ==="
for p in /painel/entrar /painel/anuncios /painel/usuarios /painel/anuncios/1/excluir /painel/fila/1/rejeitar /painel/sair; do
  printf "POST %s -> " "$p"; $C -X POST -o /dev/null -w "%{http_code}\n" "$B$p"; done
echo "=== D4 API sem sessão (esperado 401 ProblemDetails) ==="
for p in /api/v1/ads /api/v1/ads/1 /api/v1/ads/1/photos; do
  printf "%s -> " "$p"; $C -w "%{http_code} %{content_type}\n" -o /tmp/dast_body "$B$p"; head -c 200 /tmp/dast_body; echo; done
echo "=== D5 fotos: traversal e acesso direto a originais ==="
for p in "/fotos/1/1-large.webp" "/fotos/1/1-orig.webp" "/fotos/../appsettings.json" "/fotos/1/..%2f..%2fappsettings.json" "/fotos/%2e%2e/%2e%2e/etc/passwd" "/_originals/" "/photos/" "/uploads/" "/appsettings.json" "/appsettings.Production.json" "/web.config" "/GazetaMarketplace.Web.dll" "/.env" "/.git/config" "/logs/" "/keys/" "/swagger" "/swagger/v1/swagger.json" "/openapi/v1.json" "/metrics" "/health" "/health/ready" "/error" "/Home/Error" "/Home/Status" "/diagnostics" "/debug"; do
  printf "%s -> " "$p"; $C -o /dev/null -w "%{http_code}\n" --path-as-is "$B$p"; done
echo "=== D6 open redirect em returnUrl ==="
for r in "https://evil.example/" "//evil.example/" "/\\evil.example" "javascript:alert(1)"; do
  printf "returnUrl=%s -> " "$r"; $C -o /dev/null -w "%{http_code} %{redirect_url}\n" --get --data-urlencode "returnUrl=$r" "$B/painel/entrar"; done
echo "=== D7 métodos HTTP e CORS ==="
printf "OPTIONS / -> "; $C -X OPTIONS -o /dev/null -w "%{http_code}\n" "$B/"
printf "TRACE / -> "; $C -X TRACE -o /dev/null -w "%{http_code}\n" "$B/"
echo "-- preflight com Origin externo"; hdr -X OPTIONS -H "Origin: https://evil.example" -H "Access-Control-Request-Method: POST" "$B/api/v1/ads" | grep -iE "^HTTP|access-control"
echo "-- GET com Origin externo"; hdr -H "Origin: https://evil.example" "$B/api/v1/cities" | grep -iE "^HTTP|access-control"
echo "=== D8 injeção/entradas hostis em rotas públicas ==="
for p in "/busca?q=%27%20OR%201%3D1--" "/busca?q=%3Cscript%3Ealert(1)%3C/script%3E" "/busca?q=$(python3 -c 'print("A"*5000)')" "/busca?page=-1" "/busca?page=99999999999999999999" "/anuncio/abc" "/anuncio/-1" "/anuncio/2147483648/x" "/categoria/..%2f..%2f" "/api/v1/cep/abcdefgh" "/api/v1/cep/00000000" "/api/v1/cities?q=%27%3B--"; do
  printf "%.70s -> " "$p"; $C -o /dev/null -w "%{http_code}\n" --path-as-is "$B$p"; done
echo "=== D9 reflexão de XSS ==="
$C "$B/busca?q=%22%3E%3Cscript%3Ealert(1)%3C/script%3E" | grep -c "<script>alert(1)" 
echo "=== D10 corpo gigante / content-type inesperado ==="
printf "POST 20MB em /painel/entrar -> "; head -c 20000000 /dev/zero | $C -X POST -H "Content-Type: application/x-www-form-urlencoded" --data-binary @- -o /dev/null -w "%{http_code}\n" "$B/painel/entrar"
printf "POST JSON malformado em /api/v1/ads -> "; $C -X POST -H "Content-Type: application/json" -d '{"x":' -o /dev/null -w "%{http_code}\n" "$B/api/v1/ads"
echo "=== D11 página de erro/stack trace ==="
$C "$B/anuncio/abc" | grep -ciE "stack trace|System\.|at GazetaMarketplace|Exception"
