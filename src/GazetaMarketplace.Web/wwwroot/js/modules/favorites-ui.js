// A parte dos favoritos que aparece em todas as páginas do site público: o contador do cabeçalho, o coração dos cards e o botão
// "Favoritar" da página do anúncio. Carregado pelo layout.js. Os controles nascem com `hidden` no HTML (sem JavaScript não há onde guardar).

import { alternarFavorito, aoMudar, ehFavorito, lerFavoritos } from "./favorites.js";

export const MENSAGEM_NAO_SALVOU = "Não foi possível salvar seus favoritos neste navegador";

/** Mostra um aviso na região de avisos do topo (role="alert"). */
export function mostrarAviso(texto) {
  const regiao = document.getElementById("avisos-globais");
  if (!regiao) return;
  regiao.textContent = texto;
  regiao.hidden = false;
}

function atualizarContador() {
  const total = String(lerFavoritos().length);
  for (const alvo of document.querySelectorAll("[data-favoritos-contagem]")) alvo.textContent = total;
}

/** O coração (ou o botão) mostra o estado atual: preenchido e pressionado quando é favorito. */
function pintar(botao) {
  const favorito = ehFavorito(Number(botao.dataset.adId));
  botao.setAttribute("aria-pressed", String(favorito));
  const icone = botao.querySelector(".fa");
  if (icone) {
    icone.classList.toggle("fa-heart", favorito);
    icone.classList.toggle("fa-heart-o", !favorito);
  }

  const rotulo = botao.querySelector("[data-favorite-label]");
  if (rotulo) rotulo.textContent = favorito ? "Favoritado" : "Favoritar";
}

function pintarTodos() {
  for (const botao of document.querySelectorAll("[data-favorite-toggle]")) pintar(botao);
}

export function ativarFavoritos() {
  atualizarContador();
  for (const botao of document.querySelectorAll("[data-favorite-toggle]")) {
    botao.hidden = false;
    pintar(botao);
  }

  document.addEventListener("click", (evento) => {
    const botao = evento.target.closest?.("[data-favorite-toggle]");
    if (!botao) return;

    const resultado = alternarFavorito(Number(botao.dataset.adId));
    if (!resultado.ok) {
      // S07: o navegador não deixa salvar; o coração continua como estava
      mostrarAviso(MENSAGEM_NAO_SALVOU);
      return;
    }

    pintarTodos();
  });

  aoMudar(() => {
    atualizarContador();
    pintarTodos();
  });
}

export { atualizarContador };
