#!/bin/bash
# DAST de upload de fotos contra o artefato publicado (Redator autor do anúncio 16210/16206)
B=${B:-https://localhost:5443}; P=${P:?pasta das imagens de sondagem}; AD=${AD:-16206}
T=$(mktemp -d); C="curl -sk --max-time 60"
tok() { grep -o 'name="__RequestVerificationToken"[^>]*value="[^"]*"' | head -1 | sed 's/.*value="//;s/"$//'; }
t=$($C -c $T/j -b $T/j "$B/painel/entrar" | tok)
$C -c $T/j -b $T/j -o /dev/null -X POST "$B/painel/entrar" --data-urlencode "__RequestVerificationToken=$t" --data-urlencode "Email=e2e-a300fade@exemplo.com.br" --data-urlencode "Password=E2e!Admin#Pass1"
H=$($C -b $T/j "$B/painel/anuncios" | grep -o '<meta name="request-verification-token" content="[^"]*"' | sed 's/.*content="//;s/"$//')
echo "antiforgery: ${H:+ok}"
up() { printf "%-34s -> " "$1"; $C -b $T/j -X POST -H "RequestVerificationToken: $H" -F "file=@$P/$2;type=$3;filename=${4:-$2}" -w "%{http_code} " "$B/api/v1/ads/$AD/photos" -o $T/b; head -c 160 $T/b | tr '\n' ' '; echo; }
up "(a) vazio" empty.png image/png
up "(b) acima de 10 MB (10,6 MB)" big.png image/png
up "(c) MZ disfarçado de .jpg" fake.jpg image/jpeg
up "(d) nome ../../etc/passwd.png" ok.png image/png "../../etc/passwd.png"
up "(e) nome evil.jpg.exe" ok.png image/png "evil.jpg.exe"
up "(f) SVG com script" script.svg image/svg+xml
up "(g) PNG + PHP/JS anexado" polyglot.png image/png
up "(h) PNG 6000x6000 (36 MP)" bomb.png image/png
up "(i) PNG válido 800x600" ok.png image/png
echo "--- o que ficou no disco para o anúncio (nomes)"
ls -R ${PHOTO_DIR:-/dev/null} 2>/dev/null | head -0
echo "--- leitura das versões geradas"
LU=$(python3 - <<PY
import json
try: print(json.load(open("$T/b")).get("url1600",""))
except Exception: print("")
PY
)
echo "url1600=$LU"; [ -n "$LU" ] && { $C -o $T/img -D - "$B$LU" | grep -iE "^HTTP|content-type|cache-control|x-content-type|content-disposition"; file $T/img; }

