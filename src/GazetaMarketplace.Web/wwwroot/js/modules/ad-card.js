// Foto do card que não carrega: troca a imagem pelo mesmo bloco neutro "Foto indisponível" (design-system §5.2).
// Sem JavaScript a imagem quebrada fica com alt vazio e o título ao lado. Montado com createElement/textContent (RC-17).

const SELETOR = "img[data-ad-card-image]";

/** @param {HTMLImageElement} imagem */
function trocarPorBloco(imagem) {
  if (!imagem.isConnected) return;
  const bloco = document.createElement("div");
  bloco.className = "media-reservada ad-card__bloco px-2";
  bloco.setAttribute("aria-hidden", "true");
  bloco.dataset.adPlaceholder = "";
  const texto = document.createElement("span");
  texto.className = "fw-bold";
  texto.textContent = "Foto indisponível";
  bloco.append(texto);
  imagem.replaceWith(bloco);
}

/** Erros de imagem não sobem na árvore: escuta na captura, uma vez só para a página inteira. */
export function watchAdCardImages() {
  document.addEventListener(
    "error",
    (evento) => {
      if (evento.target instanceof HTMLImageElement && evento.target.matches(SELETOR)) trocarPorBloco(evento.target);
    },
    true,
  );

  // A imagem pode ter falhado antes de este módulo rodar (módulos são adiados)
  for (const imagem of document.querySelectorAll(SELETOR)) {
    if (imagem.complete && imagem.naturalWidth === 0) trocarPorBloco(imagem);
  }
}
