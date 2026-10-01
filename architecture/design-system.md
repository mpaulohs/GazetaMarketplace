# Design system: GazetaMarketplace v1

> **Em resumo:** este documento define o vocabulário visual comum às telas já aprovadas em `specs/wireframes/`: os **tokens** (cores, tipografia, raio, espaçamento), a **matriz de estados** de cada componente compartilhado e o **contrato** de cada componente em termos de classes do Bootstrap 5.3.8. Ele não redesenha telas. Sem identidade visual definida, a v1 usa **as cores e a fonte padrão do Bootstrap**, com três ajustes de acessibilidade medidos (foco, bordas de campos e links). Também fecha a **A4** (anúncio sem foto) e a **A6** (Serviços sem preço). Leitor: quem constrói o frontend no `/build`.

| Campo | Valor |
|---|---|
| Base | Bootstrap 5.3.8, arquivos estáticos em `wwwroot/lib/bootstrap/` (`rules/frontend.md`) |
| Onde os tokens moram | `wwwroot/css/base.css`, carregado depois do `bootstrap.min.css` |
| Sem | Sass, bundler, CDN, outro framework CSS, fontes externas, biblioteca de ícones |
| SPEC | `specs/SPEC.md` Approved v1.2 · NFR-03, NFR-16, NFR-17, NFR-20 |

---

## 1. Decisões principais

1. **Cores e fonte padrão do Bootstrap** (decisão do Product Owner, 2026-09-30). A paleta, a fonte e o logotipo da Gazeta ficam como perguntas em aberto (§9). Trocar depois é mexer só em `base.css`.
2. **Três ajustes de acessibilidade sobre o padrão**, porque o padrão não passa nos critérios da NFR-16 (medidos em §2.4):
   - **foco:** contorno sólido `#0a58ca` de 2 px em `:focus-visible`, no lugar do anel azul translúcido (contraste 1,41:1 → 6,44:1);
   - **borda dos campos de formulário:** `#6c757d` no lugar de `#dee2e6` (1,3:1 → 4,69:1);
   - **links sempre sublinhados** (o azul padrão tem 4,5:1, no limite; o sublinhado garante que o link não depende só da cor).
3. **A4 — anúncio sem foto (Vagas de emprego):** o card e a página de detalhe usam um **bloco neutro "Vaga de emprego"** no lugar da foto, do mesmo tamanho, com a área da vaga quando houver (§5.2, §5.4).
4. **A6 — Serviços sem preço:** o card e a página de detalhe mostram o **tipo do serviço no lugar do preço**; na busca, Serviços ficam fora da faixa de preço e no fim das ordenações (ADR-006).
5. **Estado na tela:** tudo vem renderizado do servidor; o único estado no navegador é a lista de favoritos no `localStorage` (S1). Não há biblioteca de estado nem framework JavaScript.

---

## 2. Tokens

### 2.1 Bloco de tokens (`wwwroot/css/base.css`)

```css
/* base.css — carregado depois do bootstrap.min.css. Valores do Bootstrap 5.3.8 mantidos,
   exceto os três ajustes de acessibilidade (§1, item 2). */
:root {
  /* Cores: padrão do Bootstrap 5.3.8 (paleta da Gazeta pendente, §9) */
  --bs-primary: #0d6efd;       --bs-primary-rgb: 13, 110, 253;
  --bs-secondary: #6c757d;     --bs-secondary-rgb: 108, 117, 125;
  --bs-success: #198754;       --bs-success-rgb: 25, 135, 84;
  --bs-warning: #ffc107;       --bs-warning-rgb: 255, 193, 7;
  --bs-danger: #dc3545;        --bs-danger-rgb: 220, 53, 69;
  --bs-body-color: #212529;    --bs-body-bg: #fff;
  --bs-link-color: #0d6efd;    --bs-link-hover-color: #0a58ca;

  /* Tipografia: pilha do sistema, sem fonte externa (fonte da Gazeta pendente, §9) */
  --bs-body-font-family: system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue",
    "Noto Sans", "Liberation Sans", Arial, sans-serif;
  --bs-body-font-size: 1rem;
  --bs-body-line-height: 1.5;

  /* Raio: padrão do Bootstrap */
  --bs-border-radius-sm: .25rem;
  --bs-border-radius: .375rem;
  --bs-border-radius-lg: .5rem;

  /* Tokens do projeto (o Bootstrap não tem variável para estes) */
  --app-focus-outline: 2px solid #0a58ca;      /* 6,44:1 sobre branco */
  --app-focus-offset: 2px;
  --app-input-border-color: #6c757d;           /* 4,69:1 sobre branco */
  --app-media-ratio: 4 / 3;                    /* capa do card, galeria e bloco "Vaga de emprego" */
  --app-media-placeholder-bg: var(--bs-secondary-bg-subtle);  /* #e2e3e5 */
  --app-media-placeholder-color: var(--bs-secondary-text-emphasis); /* #2b2f32, 10,51:1 */
  --app-skeleton-bg: var(--bs-tertiary-bg);    /* #f8f9fa */
  --app-touch-target: 44px;                    /* alvo mínimo de toque (wireframes) */
}

/* Foco visível e com contraste em todo elemento interativo (NFR-16) */
:focus-visible {
  outline: var(--app-focus-outline);
  outline-offset: var(--app-focus-offset);
  box-shadow: none;
}

/* Bordas de campos com 3:1 ou mais (WCAG 1.4.11) */
.form-control, .form-select, .form-check-input { border-color: var(--app-input-border-color); }

/* Link nunca depende só da cor */
a:not(.btn):not(.nav-link):not(.page-link) { text-decoration: underline; }

/* .btn-primary tem variáveis próprias e NÃO lê --bs-primary; listadas para quando a paleta mudar */
.btn-primary {
  --bs-btn-bg: var(--bs-primary);
  --bs-btn-border-color: var(--bs-primary);
  --bs-btn-hover-bg: #0b5ed7;
  --bs-btn-hover-border-color: #0a58ca;
  --bs-btn-active-bg: #0a58ca;
  --bs-btn-active-border-color: #0a53be;
  --bs-btn-disabled-bg: var(--bs-primary);
  --bs-btn-disabled-border-color: var(--bs-primary);
}
```

### 2.2 Onde cada token é usado

| Token | Valor | Onde |
|---|---|---|
| `--bs-primary` | `#0d6efd` | Botão principal ("Buscar", "Salvar", "Enviar para revisão", "Publicar", "Ligar"), paginação ativa |
| `--bs-success` | `#198754` | Situação "Publicado"; mensagens de sucesso (`alert-success`) |
| `--bs-warning` | `#ffc107` (texto preto) | Situação "Em revisão"; selo "Cidade/UF informadas manualmente" (versão *subtle*) |
| `--bs-danger` | `#dc3545` | Situação "Rejeitado"; erros; botões "Arquivar", "Desativar", "Excluir" (`btn-outline-danger`) |
| `--bs-secondary` | `#6c757d` | Situações "Rascunho" e "Arquivado"; botões secundários (`btn-outline-secondary`) |
| `--bs-body-font-family` | pilha do sistema | Todo o texto |
| `--bs-border-radius` | `.375rem` | Cards, campos, botões |
| `--app-focus-outline` | `2px solid #0a58ca` | Todo elemento com foco pelo teclado |
| `--app-input-border-color` | `#6c757d` | Campos de texto, listas e caixas de seleção |
| `--app-media-ratio` | `4 / 3` | Capa do card, foto em destaque, bloco "Vaga de emprego" (reserva o espaço: NFR-03) |
| `--app-media-placeholder-*` | `#e2e3e5` / `#2b2f32` | Bloco "Vaga de emprego" e "Foto indisponível" |
| `--app-skeleton-bg` | `#f8f9fa` | Esqueletos de carregamento |
| `--app-touch-target` | `44px` | Botões de foto, coração, paginação, ações das listas |

### 2.3 Tipografia e espaçamento

- **Escala:** a do Bootstrap. Título da página `h1` (`.fs-3` no celular); preço no detalhe `.fs-3 .fw-bold`; preço no card `.fw-bold`; textos de ajuda `.small .text-body-secondary`.
- **Espaçamento:** escala do Bootstrap (utilitários 0–5: 0,25 / 0,5 / 1 / 1,5 / 3 rem). Nenhum `--app-space-*` foi necessário.
- **Largura de leitura:** descrições e textos longos com no máximo ~70 caracteres por linha (`max-width: 70ch`) no detalhe.

### 2.4 Contrastes medidos (WCAG 2.1 AA)

| Par | Contraste | Mínimo | Resultado |
|---|---|---|---|
| Texto `#212529` sobre branco | 15,43:1 | 4,5:1 | Passa |
| Texto secundário (`--bs-secondary-color`) sobre branco | 6,76:1 | 4,5:1 | Passa |
| Branco sobre `--bs-primary` (botões) | 4,50:1 | 4,5:1 | Passa, **sem folga** — qualquer paleta nova deve ter mais |
| Link `#0d6efd` sobre branco | 4,50:1 | 4,5:1 | Passa, sem folga; links sublinhados |
| Branco sobre `--bs-success` / `--bs-danger` / `--bs-secondary` | 4,53 / 4,53 / 4,69:1 | 4,5:1 | Passa |
| Preto sobre `--bs-warning` | 12,88:1 | 4,5:1 | Passa |
| Textos *emphasis* sobre fundos *subtle* (alertas, selo, situações) | 7,21 a 10,51:1 | 4,5:1 | Passa |
| Anel de foco padrão (azul 25%) sobre branco | 1,41:1 | 3:1 | **Falha** → trocado por `--app-focus-outline` (6,44:1) |
| Borda padrão de campo `#dee2e6` sobre branco | 1,30:1 | 3:1 | **Falha** → trocada por `#6c757d` (4,69:1) |

---

## 3. Navegação e arquitetura de informação

| Área | Navegação | Decisão |
|---|---|---|
| Site público | Cabeçalho: logotipo (texto "GazetaMarketplace" até haver logo, §9), caixa de busca, "Favoritos (n)"; rodapé: "Área da equipe" | Categorias principais só na página inicial e na página de categoria (wireframes US-001) |
| Caminho de navegação | `breadcrumb` em categoria, busca e detalhe, até 3 níveis de categoria + o anúncio | Nas telas estreitas, o caminho quebra em linhas; nunca rolagem horizontal (NFR-17) |
| Painel do Redator | "Meus anúncios", nome da pessoa, "Sair" | 1 item |
| Painel do Administrador | "Anúncios" (abas "Fila de revisão" e "Todos os anúncios"), "Categorias", "Usuários", "Configurações", nome, "Sair" | 4 itens; item ativo com `aria-current="page"` |
| Menu no celular | `navbar` recolhível do Bootstrap; sem JavaScript, os itens ficam visíveis abaixo do cabeçalho | Progressive enhancement (`rules/frontend.md`) |

---

## 4. Matriz de estados por componente

Legenda: ✅ desenhado e obrigatório · — não se aplica. Detalhes de cada estado nos contratos (§5).

| Componente | Padrão | Hover | Foco | Ativo | Desabilitado | Carregando | Erro | Vazio |
|---|---|---|---|---|---|---|---|---|
| Botão (`btn`) | ✅ | ✅ | ✅ contorno | ✅ | ✅ `disabled` + texto | ✅ "Salvando…" / "Enviando…" | — | — |
| Link | ✅ sublinhado | ✅ `#0a58ca` | ✅ contorno | — | — | — | — | — |
| Campo de texto / lista | ✅ | — | ✅ contorno | — | ✅ somente leitura (`readonly`, fundo `tertiary`) | — | ✅ `is-invalid` + mensagem | ✅ placeholder só como exemplo |
| Campo com contador (título, informações adicionais) | ✅ "X/90", "X/6000" | — | ✅ | — | — | — | ✅ | — |
| Campo de preço (máscara) | ✅ "R$ 0,00" | — | ✅ | — | — | — | ✅ "Informe um preço maior que zero" | ✅ |
| Campo de CEP | ✅ | — | ✅ | — | — | ✅ "Buscando…" / "tentativa 2 de 2" | ✅ não encontrado; serviço fora → listas | — |
| Card de anúncio | ✅ 3 variantes | ✅ título sublinhado | ✅ contorno no card | — | — | ✅ esqueleto | ✅ foto indisponível | — |
| Coração (favoritar) | ✅ ♡ | ✅ | ✅ | ✅ ♥ `aria-pressed="true"` | — | — | ✅ armazenamento bloqueado | — |
| Galeria | ✅ | ✅ setas | ✅ | ✅ miniatura atual | — | ✅ esqueleto | ✅ "Foto indisponível" | ✅ uma foto: sem setas |
| Bloco "Vaga de emprego" (A4) | ✅ | — | — | — | — | — | — | — |
| Preço / Salário / Tipo (A6) | ✅ 3 formas | — | — | — | — | — | — | — |
| Envio de fotos (miniatura) | ✅ capa / "Tornar capa" | ✅ | ✅ | — | ✅ limite atingido | ✅ "Enviando… 60%" | ✅ "Falha ao enviar" + "Tentar de novo" | ✅ "Nenhuma foto enviada ainda" |
| Filtros da busca | ✅ | — | ✅ | — | ✅ cidade sem UF | — | ✅ faixa de preço invertida | — |
| Lista / tabela do painel | ✅ | ✅ linha | ✅ | — | — | ✅ esqueleto | ✅ + "Tentar novamente" | ✅ "Você ainda não criou anúncios" |
| Paginação | ✅ | ✅ | ✅ | ✅ página atual | ✅ "Anterior" na página 1 | — | — | — |
| Etiqueta de situação | ✅ 5 situações | — | — | — | — | — | — | — |
| Selo "Cidade/UF manual" | ✅ | — | — | — | — | — | — | — |
| Alerta / lista de pendências | ✅ sucesso, erro, aviso, info | — | ✅ recebe foco quando aparece | — | — | — | ✅ | — |
| Diálogo (confirmação) | ✅ | — | ✅ foco inicial definido | — | ✅ confirmar desabilitado durante envio | ✅ "Enviando…" | ✅ mensagem no diálogo | — |
| Estado de página (vazio, erro, sem resultado) | ✅ | — | ✅ título recebe foco | — | — | ✅ esqueleto | ✅ código de referência + "Tentar novamente" | ✅ |

---

## 5. Contratos de componentes

### 5.1 Botões

| Uso | Classes | Observação |
|---|---|---|
| Ação principal da tela (uma por tela) | `btn btn-primary` | "Salvar rascunho", "Enviar para revisão", "Publicar", "Buscar" |
| Ação secundária | `btn btn-outline-secondary` | "Cancelar", "Limpar filtros", "Tornar capa" |
| Ação destrutiva | `btn btn-outline-danger`; no diálogo, `btn btn-danger` | "Arquivar", "Desativar", "Excluir", "Remover" |
| Contato | `btn btn-primary` ("Ligar") e `btn btn-success` ("Chamar no WhatsApp") | Links reais (`tel:`, `https://wa.me/`) estilizados como botão |

Estados: carregando troca o texto ("Salvando…") e aplica `disabled` para evitar clique duplo (US-009). O botão de envio **não** fica desabilitado enquanto o formulário tem pendências: ao clicar, a lista de pendências aparece (US-009-S02), porque um botão desabilitado não explica o que falta.

### 5.2 Card de anúncio (três variantes)

Estrutura: `article.card` com um único link cobrindo o card (`a.stretched-link` no título) e o coração fora do link. Grade `row-cols-2 row-cols-md-3 row-cols-lg-4` (12 na página inicial, 24 na busca).

| Variante | Área da foto (`--app-media-ratio`) | Linha de valor | Nome acessível do link |
|---|---|---|---|
| Padrão | Capa 480 px (`<img width height loading="lazy">`); a primeira linha de cards com `loading="eager"` | `R$ 62.000` (`.fw-bold`, S29) | "Honda Civic 2018, R$ 62.000, Campinas/SP" |
| **Serviços (A6)** | Capa normal | Tipo do serviço (`.fw-semibold`), por exemplo "Serviços domésticos" | "Diarista…, Serviço: Serviços domésticos, São Paulo/SP" |
| **Vagas de emprego (A4)** | Bloco `div.card-media-placeholder` com "Vaga de emprego" (`.fw-bold`) e a primeira área marcada (`.small`), fundo `--app-media-placeholder-bg`; **sem `<img>`** | `Salário R$ 2.800` (rótulo `.small .text-body-secondary` + valor `.fw-bold`) | "Pizzaiolo…, Salário R$ 2.800, Campinas/SP" |

Estados: foto que não carrega → mesmo bloco neutro com "Foto indisponível"; carregando → esqueleto com a mesma altura (NFR-03).

### 5.3 Valor do anúncio (Preço, Salário ou Tipo)

Componente de apresentação único (partial view `_AdValue`), decidido pelo grupo de campos (ADR-002):

| Grupo | Detalhe (`.fs-3 .fw-bold`) | Card |
|---|---|---|
| Com preço | `R$ 62.000` (centavos só quando não são zero, S29) | Igual, `.fw-bold` |
| Vagas de emprego | Rótulo "Salário" (`.fs-6 .text-body-secondary`) + `R$ 2.800` | "Salário R$ 2.800" |
| Serviços | Tipo do serviço, sem valor | Tipo do serviço |

### 5.4 Galeria e bloco "Vaga de emprego" (detalhe)

- **Galeria** (US-003): foto em destaque 1.600 px com `aspect-ratio` reservado; setas "Foto anterior" e "Próxima foto"; contador "5 de 20" em `role="status"`; miniaturas em lista de botões (`aria-current` na atual); ampliar abre `modal` do Bootstrap com foco preso e Esc fechando; troca por teclado (setas) e por toque. Uma foto só: sem setas nem miniaturas. Foto quebrada: bloco "Foto indisponível", as demais seguem navegáveis.
- **Vagas de emprego (A4):** no lugar da galeria, um `section` com o mesmo `aspect-ratio`, fundo `--app-media-placeholder-bg`, título "Vaga de emprego" e a lista de áreas marcadas (`ul`); sem botão de ampliar. O texto da vaga fica em "Informações adicionais".

### 5.5 Formulário do anúncio

- Campos com `form-label` visível acima, `form-control` / `form-select`, `aria-required`, erro com `is-invalid` + `invalid-feedback` ligado por `aria-describedby` e `role="alert"`.
- A legenda "* obrigatório para enviar à revisão; o rascunho pode ser salvo só com o título" fica no topo.
- **Campos por grupo** vêm do grupo de campos (ADR-002): ficha em `fieldset` com `legend`; ao trocar a categoria, o formulário se refaz, o foco volta para "Categoria" e a mudança é anunciada.
- **Contadores** (`.form-text` alinhado à direita): "X/90" no título de Vagas; "X/6000" em "Informações adicionais".
- **Preço:** máscara estilo banco (S28), `inputmode="numeric"`; texto de ajuda "Digite só números; os dois últimos são os centavos."; em Vagas, a ajuda acrescenta "É o salário oferecido; no site aparece como Salário."; em Serviços, o campo não existe e aparece o texto "Serviços não têm preço.".
- **CEP:** `inputmode="numeric"`, máscara `00000-000`, status ao lado ("Buscando…", "tentativa 2 de 2"); Cidade e UF somente leitura; no preenchimento manual, duas `form-select` (UF e cidade) e o aviso em `alert-warning`.
- **Envio de fotos:** botão "Selecionar fotos" (entrada de arquivo real, sem arrastar) + "Enviar fotos"; miniaturas em grade com "CAPA" na primeira, "Tornar capa" e "Remover" nas demais; contador "Fotos (3 de 20)" em `role="status"`; avisos por arquivo em região `aria-live="assertive"`. Em Vagas de emprego, a seção é substituída pelo texto "Vagas de emprego não têm fotos.".
- **Pendências** (US-009): `alert-danger` no topo com lista de links para cada campo; recebe o foco ao aparecer.

### 5.6 Etiquetas de situação e selo

| Situação | Classes | Texto |
|---|---|---|
| Rascunho | `badge text-bg-secondary` | "Rascunho" |
| Em revisão | `badge text-bg-warning` | "Em revisão" |
| Publicado | `badge text-bg-success` | "Publicado" |
| Rejeitado | `badge text-bg-danger` | "Rejeitado" (motivo em texto na linha) |
| Arquivado | `badge bg-secondary-subtle text-secondary-emphasis` | "Arquivado" |
| Selo de conferência | `badge bg-warning-subtle text-warning-emphasis border border-warning-subtle` | "Cidade/UF informadas manualmente (CEP não conferido)" |

A situação é sempre texto; a cor só reforça (NFR-16).

### 5.7 Alertas, diálogos e estados de página

- **Alertas:** `alert alert-success | alert-danger | alert-warning | alert-info`; sucesso em `role="status"`, erro em `role="alert"`.
- **Diálogos de confirmação:** `modal` do Bootstrap com `role="alertdialog"` nas ações destrutivas, foco inicial em "Cancelar", foco devolvido ao botão de origem ao fechar; sem JavaScript, a ação leva a uma página de confirmação equivalente (progressive enhancement).
- **Estados de página:** vazio (mensagem + ação), erro (mensagem + "Tentar novamente" + "Código de referência: …" com o `traceId`), sem resultado (mensagem + "Limpar filtros"), carregando (esqueletos com a altura final).

### 5.8 Tabelas do painel e árvore de categorias

- Tabela `table` com `th scope="col"` a partir de 768 px; abaixo disso, a mesma lista vira cartões (`list-group`), na ordem título, situação, categoria, data (wireframe US-012).
- Árvore de categorias: listas aninhadas (`ul` em `li`) com recuo por nível (até 3), botões "Mover … para cima/para baixo" com o nome da categoria e `aria-disabled` no primeiro e no último do grupo.

---

## 6. Filtros e ordenação com Serviços (A6)

- Faixa de preço usada → anúncios de Serviços **não aparecem** (não têm preço para comparar).
- Ordenação "Menor preço" e "Maior preço" → anúncios de Serviços vão **para o fim**, em ordem de mais recentes.
- O bloco de preço dos filtros mostra o texto de ajuda "Serviços não têm preço: ficam fora da faixa de preço e no fim da ordenação por preço."
- Mecanismo: ADR-006.

---

## 7. Responsivo

- Pontos de quebra do Bootstrap: `sm` 576, `md` 768, `lg` 992, `xl` 1200. Verificação em 320, 768, 1024 e 1280 px sem rolagem horizontal (NFR-17).
- Cards: 2 colunas até `md`, 3 até `lg`, 4 a partir de `lg`.
- Filtros da busca: recolhidos atrás do botão "Filtros" (`collapse`) abaixo de `lg`.
- Alvos de toque de pelo menos `--app-touch-target` (44 px) em coração, setas, miniaturas, "Tornar capa", "Remover", paginação e ações das listas.

---

## 8. Acessibilidade (resumo do contrato)

- Foco visível com `--app-focus-outline` em tudo o que é interativo; ordem de foco igual à ordem visual; link "Ir para o conteúdo" no topo.
- Contraste conforme §2.4; nada comunicado só por cor.
- Imagens: capa com texto alternativo igual ao título do anúncio; miniaturas "Foto 2 de 20"; bloco "Vaga de emprego" é texto, não imagem.
- Mudanças dinâmicas anunciadas (`aria-live`): contadores de fotos, resultado de busca, favoritos, campos que mudam com a categoria.
- Verificação automática com axe-core nos testes Playwright; verificação manual com teclado e leitor de tela (NVDA) por tela (NFR-16).

---

## 9. Perguntas em aberto

| Id | Pergunta | Até quando | Dono |
|---|---|---|---|
| DS-01 | **Paleta da marca Gazeta.** Ao trocar, conferir o contraste de cada par da §2.4 e atualizar as variáveis próprias dos componentes (`--bs-btn-*`), que não leem `--bs-primary` | Antes do lançamento | Product Owner com a Gazeta |
| DS-02 | **Fonte da marca.** Se for uma fonte própria, ela entra como arquivo estático em `wwwroot` (sem Google Fonts nem CDN, por causa da CSP) | Antes do lançamento | Product Owner com a Gazeta |
| DS-03 | **Logotipo** (e ícone do navegador). Até lá, o cabeçalho usa o texto "GazetaMarketplace" | Antes do lançamento | Product Owner com a Gazeta |
