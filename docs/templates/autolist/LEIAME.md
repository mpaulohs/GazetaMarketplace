# Template Autolist (referência de visual)

> **Em resumo:** esta pasta guarda o que foi aproveitado do template **Autolist** (Spruko, "Car Listing, Dealer, Rental Auto Classifieds") como **referência de visual**. O site **não carrega** nenhum arquivo daqui: o visual foi extraído à mão para `wwwroot/css/` (tokens em `base.css`, peças em `components/*.css`). Quem for construir uma tela nova consulta os HTMLs e o `style.css` abaixo para copiar a aparência, nunca o markup ou o JavaScript.

## Origem

| Campo | Valor |
|---|---|
| Template | Autolist (Spruko Market), HTML estático com Bootstrap 5.1.3 e jQuery 3.6.0 |
| Versão do pacote | Não consta nos arquivos (sem `package.json`, changelog nem cabeçalho de versão) |
| Branch de origem | `adicionar-template-autolist`, commit `3121e6c` (arquivos completos, ~126 MB, **não** mesclados nesta branch) |
| Extraído em | 2026-10-03 |

## O que existe aqui

| Pasta | Conteúdo | Para quê |
|---|---|---|
| `Html/` | 86 páginas do site público | Ver a aparência de cabeçalho, busca, cards, detalhe, formulários e rodapés |
| `Html-Admin/` | 118 páginas do admin | Ver cards, tabelas e widgets do painel (o menu lateral **não** será usado: vale o menu superior dos wireframes) |
| `assets/css/` | `style.css`, `plugins.css`, `icons.css`, `admin-custom.css` | Fonte dos valores (cores, raios, sombras, espaçamentos). **Só leitura: nunca carregado, nunca editado** |

As páginas HTML apontam para `../assets/images`, `../assets/js` e outros arquivos que não foram trazidos; para vê-las completas, abra a branch `adicionar-template-autolist`.

## O que foi extraído para o site

| Do template | Foi para | Como |
|---|---|---|
| Cores (vermelho `#e72a1a`, roxo `#8d0bb7`, cinzas, fundo `#f1f5fd`) | `base.css` (`--bs-*` e `--app-*`) | Primária escurecida para `#d72213` (contraste 5,09:1 com branco; o original dava 4,43:1) |
| Fonte Poppins | `wwwroot/lib/poppins/` e `css/poppins.css` | Autohospedada (o template a busca no Google Fonts, bloqueado pela CSP) |
| Ícones Font Awesome 4.7 | `wwwroot/lib/font-awesome/` | Só esta família; as outras 14 saíram |
| Raios, sombras, bordas dos cards e campos | `base.css` | Variáveis `--bs-border-radius*` e `--bs-box-shadow` |
| Degradê vermelho-roxo do topo, faixa de busca, rodapé escuro | `components/cabecalho.css` e `components/rodape.css` | Reescritos em CSS puro; o degradê é CSS, sem a imagem `banner1.jpg` |

## O que foi removido (e por quê)

| Removido | Motivo |
|---|---|
| `style.css` como folha carregada | 927 KB, 3.140 `!important`, `@import` do Google Fonts, 21 variáveis SCSS inválidas, foco suprimido, assume Bootstrap 5.1.3 (o projeto usa 5.3.8) |
| Todos os plugins jQuery (`plugins/`, `js/`) | Regra do projeto: nenhum plugin jQuery por padrão. Cada um tem substituto nativo: `owl-carousel` → carrossel do Bootstrap; `select2` → `<select>`; slider de preço → dois campos numéricos; `horizontal-menu` → `navbar` do Bootstrap; `sticky` → `position: sticky`; máscara de preço → módulo próprio; upload → `<input type="file">`; `ihavecookies` → fora (não está no SPEC) |
| `switcher/`, modo escuro e RTL | Fora do escopo da v1 |
| 14 famílias de ícones (~31 MB) | Decisão do Product Owner: só Font Awesome 4.7 |
| `video/` (59 MB), imagens de demonstração, `.psd` e `.eps` | Conteúdo de exemplo; as telas usam as fotos dos anúncios |
| Chave da API do Google Maps nos HTMLs | Segredo do autor do template em 15 páginas (`key=AIza…`); trocada por `key=CHAVE_REMOVIDA` para o GitLeaks não barrar. Esta é a única alteração nos HTMLs |

## Integridade

SHA-256 dos arquivos copiados sem alteração:

| Arquivo | SHA-256 |
|---|---|
| `assets/css/style.css` | `87d7d2af633adcd55d66422b8ab8b93e003e4a2f7586fd3b2dd00e7c3fd777aa` |
| `assets/css/plugins.css` | `99743e5832cb3443010bc536e547bc956b5090d886d1611efd4f07c281105b43` |
| `assets/css/icons.css` | `b9ae0f04c78d98eb81c32ff0999328aa849aa1708745fca1e6643718adf657d9` |
| `assets/css/admin-custom.css` | `7b51e79980502dad64c607872f0efb9f06592be6117f49e983091cbf28b6e97d` |

Os 204 HTMLs vieram da branch de origem com a única troca da chave descrita acima. O Font Awesome mantido é idêntico ao `font-awesome@4.7.0` do npm, e a Poppins vem do `@fontsource/poppins@5.3.0` (hashes e integridade em `src/GazetaMarketplace.Web/wwwroot/lib/LEIAME.md`).
