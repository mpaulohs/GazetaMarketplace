// A galeria da página do anúncio (US-003-S02, S03, S05, S07). Melhoria progressiva: sem este módulo a foto em destaque e as miniaturas funcionam como links comuns.
// Miniatura troca a foto; setas na tela e no teclado percorrem; deslizar troca no celular; clicar na foto abre a janela ampliada (<dialog>, que prende o foco,
// fecha com Esc e devolve o foco a quem abriu). Foto que não carrega vira "Foto indisponível" e as outras continuam navegáveis. Sem innerHTML (RC-17).

const SWIPE_MIN_DISTANCE = 40;

/**
 * @typedef {{ large: string, width: string, height: string }} GalleryPhoto
 */

/** @param {HTMLElement} root */
function startGallery(root) {
  const total = Number(root.dataset.total) || 1;
  const title = root.dataset.title ?? "";
  const main = /** @type {HTMLImageElement} */ (root.querySelector("[data-gallery-main]"));
  const stage = /** @type {HTMLElement} */ (root.querySelector("[data-gallery-stage]"));
  const opener = /** @type {HTMLAnchorElement} */ (root.querySelector("[data-gallery-open]"));
  const broken = /** @type {HTMLElement} */ (root.querySelector("[data-gallery-broken]"));
  const counter = root.querySelector("[data-gallery-counter]");
  const strip = root.querySelector("[data-gallery-thumbs]");
  const thumbs = /** @type {HTMLAnchorElement[]} */ ([...root.querySelectorAll("[data-gallery-thumb]")]);
  const dialog = /** @type {HTMLDialogElement | null} */ (document.querySelector("[data-gallery-dialog]"));
  if (!main || !stage || !opener || !broken) return;

  /** @type {GalleryPhoto[]} */
  const photos = thumbs.length > 0
    ? thumbs.map((t) => ({ large: t.dataset.large ?? t.href, width: t.dataset.width ?? "", height: t.dataset.height ?? "" }))
    : [{ large: main.getAttribute("src") ?? "", width: main.getAttribute("width") ?? "", height: main.getAttribute("height") ?? "" }];
  let index = 0;
  let swiped = false;

  const prev = root.querySelector("[data-gallery-prev]");
  const next = root.querySelector("[data-gallery-next]");
  if (total > 1) {
    prev?.removeAttribute("hidden");
    next?.removeAttribute("hidden");
    counter?.removeAttribute("hidden");
  }

  function showBroken() {
    main.hidden = true;
    broken.hidden = false;
  }

  function render() {
    const photo = photos[index];
    broken.hidden = true;
    main.hidden = false;
    if (main.getAttribute("src") !== photo.large) {
      main.setAttribute("src", photo.large);
      if (photo.width) main.setAttribute("width", photo.width);
      if (photo.height) main.setAttribute("height", photo.height);
    }
    main.alt = `Foto ${index + 1} de ${total}: ${title}`;
    opener.href = photo.large;
    if (counter) counter.textContent = `${index + 1} de ${total}`;
    thumbs.forEach((thumb, i) => {
      if (i === index) thumb.setAttribute("aria-current", "true");
      else thumb.removeAttribute("aria-current");
    });
    // Mostra a miniatura escolhida dentro da faixa, sem rolar a página
    const current = thumbs[index];
    if (strip instanceof HTMLElement && current) {
      const left = current.offsetLeft - strip.offsetLeft;
      if (left < strip.scrollLeft || left + current.offsetWidth > strip.scrollLeft + strip.clientWidth) {
        strip.scrollLeft = left - (strip.clientWidth - current.offsetWidth) / 2;
      }
    }
    if (dialog?.open) renderDialog();
  }

  /** @param {number} to */
  function select(to) {
    index = (to + total) % total;
    render();
  }

  main.addEventListener("error", showBroken);
  // A foto pode ter falhado antes de este módulo rodar (módulos são adiados)
  if (main.complete && main.naturalWidth === 0) showBroken();

  for (const thumb of thumbs) {
    thumb.addEventListener("click", (evento) => {
      evento.preventDefault();
      select(Number(thumb.dataset.index));
    });
    const image = thumb.querySelector("img");
    image?.addEventListener("error", () => thumb.setAttribute("data-broken", ""));
    if (image && image.complete && image.naturalWidth === 0) thumb.setAttribute("data-broken", "");
  }

  prev?.addEventListener("click", () => select(index - 1));
  next?.addEventListener("click", () => select(index + 1));

  /** @param {KeyboardEvent} evento */
  function onArrow(evento) {
    if (total < 2 || evento.altKey || evento.ctrlKey || evento.metaKey) return;
    if (evento.key === "ArrowRight") select(index + 1);
    else if (evento.key === "ArrowLeft") select(index - 1);
    else return;
    evento.preventDefault();
  }
  root.addEventListener("keydown", onArrow);

  // Deslizar na foto em destaque (celular)
  let startX = 0;
  let startY = 0;
  stage.addEventListener("pointerdown", (evento) => {
    startX = evento.clientX;
    startY = evento.clientY;
    swiped = false;
  });
  stage.addEventListener("pointerup", (evento) => {
    const dx = evento.clientX - startX;
    const dy = evento.clientY - startY;
    if (total > 1 && Math.abs(dx) >= SWIPE_MIN_DISTANCE && Math.abs(dx) > Math.abs(dy) * 2) {
      swiped = true;
      select(dx < 0 ? index + 1 : index - 1);
    }
  });

  // Ampliar
  if (!dialog || typeof dialog.showModal !== "function") return;
  const dialogImage = /** @type {HTMLImageElement} */ (dialog.querySelector("[data-gallery-dialog-image]"));
  const dialogTitle = dialog.querySelector("[data-gallery-dialog-title]");
  const dialogBroken = /** @type {HTMLElement} */ (dialog.querySelector("[data-gallery-dialog-broken]"));
  let origin = /** @type {HTMLElement} */ (opener);

  function renderDialog() {
    const photo = photos[index];
    dialogBroken.hidden = true;
    dialogImage.hidden = false;
    dialogImage.src = photo.large;
    dialogImage.alt = `Foto ${index + 1} de ${total}: ${title}`;
    if (dialogTitle) dialogTitle.textContent = `Foto ${index + 1} de ${total}`;
  }

  dialogImage.addEventListener("error", () => {
    dialogImage.hidden = true;
    dialogBroken.hidden = false;
  });

  opener.addEventListener("click", (evento) => {
    evento.preventDefault();
    if (swiped) {
      swiped = false;
      return;
    }
    origin = opener;
    renderDialog();
    dialog.showModal();
  });

  dialog.querySelector("[data-gallery-close]")?.addEventListener("click", () => dialog.close());
  dialog.querySelector("[data-gallery-dialog-prev]")?.addEventListener("click", () => select(index - 1));
  dialog.querySelector("[data-gallery-dialog-next]")?.addEventListener("click", () => select(index + 1));
  dialog.addEventListener("keydown", onArrow);
  // Clicar fora da foto (no fundo escuro) também fecha
  dialog.addEventListener("click", (evento) => {
    if (evento.target === dialog) dialog.close();
  });
  // Esc ou "Fechar": o foco volta a quem abriu e a página fica no mesmo ponto
  dialog.addEventListener("close", () => origin.focus({ preventScroll: true }));
}

/** Liga todas as galerias da página. */
export function startGalleries() {
  for (const root of document.querySelectorAll("[data-gallery]")) {
    if (root instanceof HTMLElement) startGallery(root);
  }
}
