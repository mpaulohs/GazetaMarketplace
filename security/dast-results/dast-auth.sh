#!/bin/bash
# DAST autenticado contra o artefato publicado: sessão de Redator (usuário 3) e Administrador (usuário 1)
B=${B:-https://localhost:5443}
T=$(mktemp -d); C="curl -sk --max-time 30"
tok() { grep -o 'name="__RequestVerificationToken"[^>]*value="[^"]*"' | head -1 | sed 's/.*value="//;s/"$//'; }
login() { # $1 jar $2 email $3 returnUrl(optional)
  local t; t=$($C -c $1 -b $1 "$B/painel/entrar" | tok)
  $C -c $1 -b $1 -o /dev/null -D $T/login.hdr -X POST "$B/painel/entrar" --data-urlencode "__RequestVerificationToken=$t" --data-urlencode "Email=$2" --data-urlencode "Password=E2e!Admin#Pass1" ${3:+--data-urlencode "ReturnUrl=$3"}
  grep -i "^HTTP\|^location" $T/login.hdr | tr -d '\r' | tr '\n' ' '; echo; }
csrf() { $C -b $1 "$B/painel/anuncios" | grep -o 'name="request-verification-token"[^>]*content="[^"]*"\|content="[^"]*"[^>]*name="request-verification-token"' | head -1 | sed 's/.*content="\([^"]*\)".*/\1/'; }
WR=$T/w.jar; AD=$T/a.jar
echo "=== A1 login do Redator (usuário 3) ==="; login $WR e2e-a300fade@exemplo.com.br
echo "=== A2 returnUrl externo no login (esperado: Location local, nunca evil.example) ==="
for r in "https://evil.example/" "//evil.example/" "/\\evil.example"; do rm -f $T/r.jar; printf "returnUrl=%s -> " "$r"; login $T/r.jar e2e-a300fade@exemplo.com.br "$r"; done
echo "=== A3 flags do cookie de sessão ==="
rm -f $T/c.jar; t=$($C -c $T/c.jar -b $T/c.jar "$B/painel/entrar" | tok); $C -c $T/c.jar -b $T/c.jar -o /dev/null -D - -X POST "$B/painel/entrar" --data-urlencode "__RequestVerificationToken=$t" --data-urlencode "Email=e2e-a300fade@exemplo.com.br" --data-urlencode "Password=E2e!Admin#Pass1" | grep -i "set-cookie" | sed 's/=[^;]*;/=<valor>;/' 
echo "=== A4 rotas de administrador com sessão de Redator (esperado 302 acesso-negado, nunca 200) ==="
for p in /painel/usuarios /painel/usuarios/novo /painel/usuarios/1/editar /painel/categorias /painel/configuracoes /painel/anuncios/fila /painel/fila /painel/revisao; do printf "%s -> " "$p"; $C -b $WR -o /dev/null -w "%{http_code} %{redirect_url}\n" "$B$p"; done
echo "=== A5 IDOR: Redator tenta anúncios do Administrador (16210 rascunho, 16214 outro estado) ==="
for p in /painel/anuncios/16210/editar /painel/anuncios/16214/editar /painel/anuncios/16210/pre-visualizacao /painel/anuncios/16210/enviar/confirmar /painel/anuncios/16214/arquivar /painel/anuncios/16214/despublicar /painel/anuncios/16210/excluir; do printf "GET %s -> " "$p"; $C -b $WR -o /dev/null -w "%{http_code} %{redirect_url}\n" "$B$p"; done
echo "=== A6 API fotos: IDOR e validações (Redator, anúncio do Administrador 16210) ==="
H=$(csrf $WR); echo "token antiforgery obtido: ${H:+sim}"
printf '\x89PNG\r\n' > $T/x.png; head -c 100 /dev/urandom > $T/rand.bin
printf "upload sem antiforgery -> "; $C -b $WR -o /dev/null -w "%{http_code}\n" -X POST -F "file=@$T/x.png" "$B/api/v1/ads/16210/photos"
printf "upload em anúncio de outro autor -> "; $C -b $WR -o /dev/null -w "%{http_code}\n" -X POST -H "RequestVerificationToken: $H" -F "file=@$T/x.png;type=image/png" "$B/api/v1/ads/16210/photos"
printf "upload em anúncio inexistente -> "; $C -b $WR -o /dev/null -w "%{http_code}\n" -X POST -H "RequestVerificationToken: $H" -F "file=@$T/x.png;type=image/png" "$B/api/v1/ads/99999999/photos"
printf "cover/delete em anúncio de outro autor -> "; $C -b $WR -o /dev/null -w "%{http_code} / " -X POST -H "RequestVerificationToken: $H" "$B/api/v1/ads/16210/photos/1/cover"; $C -b $WR -o /dev/null -w "%{http_code}\n" -X DELETE -H "RequestVerificationToken: $H" "$B/api/v1/ads/16210/photos/1"
echo "=== A7 upload no próprio anúncio (Redator cria um rascunho) ==="
NID=$($C -b $WR -c $WR -o /dev/null -D - "$B/painel/anuncios/novo" | grep -i "^location" | tr -d '\r'); echo "GET novo -> $NID"
