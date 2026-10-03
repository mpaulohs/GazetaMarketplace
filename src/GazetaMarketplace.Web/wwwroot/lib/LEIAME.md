# Bibliotecas estáticas de terceiros

Arquivos copiados sem alteração. Não editar; para atualizar, baixe a nova versão, confira a integridade e registre aqui.

| Biblioteca | Versão | Pasta | Procedência |
|---|---|---|---|
| Bootstrap (CSS e JS bundle) | 5.3.8 | `bootstrap/dist` | pacote npm `bootstrap@5.3.8` |
| Font Awesome (CSS e fonte `woff2`) | 4.7.0 | `font-awesome` | pacote npm `font-awesome@4.7.0`, copiado do template Autolist; só o `woff2` (os demais formatos são para navegadores antigos) |
| Poppins (`woff2`, subconjunto latino, pesos 400, 600 e 700) | 5.3.0 do pacote | `poppins` | pacote npm `@fontsource/poppins@5.3.0` (licença OFL 1.1, texto em `poppins/LICENSE`) |
| jQuery | 3.7.1 | `jquery` | template ASP.NET Core; só para a validação MVC |
| jquery-validation | do template | `jquery-validation` | só para a validação MVC |
| jquery-validation-unobtrusive | do template | `jquery-validation-unobtrusive` | só para a validação MVC |

## Verificação do Bootstrap 5.3.8 (2026-10-02)

- Integridade do tarball (`npm view bootstrap@5.3.8 dist.integrity`): `sha512-HP1SZDqaLDPwsNiqRqi5NcP0SSXciX2s9E+RyqJIIqGo+vJeN5AJVM98CXmW/Wux0nQ5L7jeWUdplCEf0Ee+tg==`
- SHA-1 do tarball: `6401a10057a22752d21f4e19055508980656aeed`
- O hash do tarball baixado conferiu com o do registro.
- `bootstrap/dist` é idêntico (`diff -rq`) ao `dist/` do tarball.
- O `bootstrap/LICENSE` é o do template e tem só o cabeçalho de copyright mais antigo; o corpo da licença (MIT) é o mesmo.

## Verificação do Font Awesome 4.7.0 (2026-10-03)

- Integridade do tarball (`npm view font-awesome@4.7.0 dist.integrity`): `sha512-U6kGnykA/6bFmg1M/oT9EkFeIYv7JlX3bozwQJWiiLz6L0w3F5vBVPxHlwyX/vtNq1ckcpRKOB9f2Qal/VtFpg==`
- `font-awesome.min.css` e `fontawesome-webfont.woff2` são idênticos (`cmp`) aos do tarball do npm.
- Licenças: fonte SIL OFL 1.1, CSS MIT.
- Só o `woff2` foi mantido. O CSS ainda cita `eot`, `woff`, `ttf` e `svg`; o navegador só pede esses formatos se não aceitar `woff2`, e não editamos o arquivo.
- Regra de uso: todo ícone decorativo leva `aria-hidden="true"`; ícone que sozinho faz o papel de um botão ou link precisa de nome acessível (`aria-label` ou texto `visually-hidden`).

| Arquivo | SHA-256 |
|---|---|
| `font-awesome/css/font-awesome.min.css` | `799aeb25cc0373fdee0e1b1db7ad6c2f6a0e058dfadaa3379689f583213190bd` |
| `font-awesome/fonts/fontawesome-webfont.woff2` | `2adefcbc041e7d18fcf2d417879dc5a09997aa64d675b7a3c4b6ce33da13f3fe` |

## Verificação da Poppins (2026-10-03)

- Integridade do tarball (`npm view @fontsource/poppins@5.3.0 dist.integrity`): `sha512-cms1nM7U6SN8epIV1WMVOV8VsA645aSm3wHfzIQnkk188U341ThbJGSl2Sv54V29gnDbT2arMPfLzvVIIVCceQ==`
- O hash do tarball baixado conferiu com o do registro; os `woff2` foram copiados sem alteração de `package/files/`.
- O subconjunto latino cobre todas as letras acentuadas do português. Os `@font-face` ficam em `wwwroot/css/poppins.css`; nenhum arquivo é buscado fora do site.

| Arquivo | SHA-256 |
|---|---|
| `poppins/files/poppins-latin-400-normal.woff2` | `7d93459d86585bfcdbb7e0376056226adb25821ee54b96236fe2123e9560929f` |
| `poppins/files/poppins-latin-600-normal.woff2` | `f4e80d9dfd374d02989b87a27b5ed4cb78fbb177c27f1478e9a8b0afb7513149` |
| `poppins/files/poppins-latin-700-normal.woff2` | `9338e65fc077355c7a87ae0d64cc101e23b9bf8ad78ae65f0f319c857311b526` |
| `poppins/LICENSE` | `5c4a92c8f8ce0ab211f4c72889d82383c9644546115d9ba39a4f2d5b8f37014b` |
