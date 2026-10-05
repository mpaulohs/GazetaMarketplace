// "Meus favoritos" (US-005). Lê os ids do localStorage, pede os cards a /favoritos/lista (fragmento de HTML desenhado pelo AdCard, em lotes de 100 ids), tira da
// lista os que o servidor não devolveu (despublicados, arquivados, inexistentes) com aviso, e deixa remover com "Remover". Os favoritos nunca vão para o servidor.

import { armazenamentoDisponivel, aoMudar, lerFavoritos, removerFavoritos } from "../modules/favorites.js";
import { MENSAGEM_NAO_SALVOU, mostrarAviso } from "../modules/favorites-ui.js";

const LOTE = 100;

const raiz = document.querySelector("[data-favorites-page]");
const campo = (nome) => raiz?.querySelector(`[data-favorites-${nome}]`);

/** "1 anúncio" ou "N anúncios". */
const rotuloDeAnuncios = (n) => (n === 1 ? "1 anúncio" : `${n.toLocaleString("pt-BR")} anúncios`);

/** O aviso dos que saíram do ar, no singular ou no plural. */
export function avisoDeIndisponiveis(n) {
  return n === 1
    ? "1 anúncio favoritado deixou de estar disponível e foi removido da sua lista."
    : `${n.toLocaleString("pt-BR")} anúncios favoritados deixaram de estar disponíveis e foram removidos da sua lista.`;
}

function mostrar(nome, visivel = true) {
  const alvo = campo(nome);
  if (alvo) alvo.hidden = !visivel;
}

function atualizarTotal() {
  const itens = raiz.querySelectorAll("[data-favorites-host] li[data-ad-id]").length;
  const contagem = campo("count");
  contagem.textContent = rotuloDeAnuncios(itens);
  contagem.hidden = itens === 0;
  mostrar("empty", itens === 0);
}

/** Pede um lote e devolve os itens (li) e os ids que voltaram. */
async function buscarLote(ids) {
  const resposta = await fetch(`/favoritos/lista?ids=${ids.join(",")}`, { headers: { Accept: "text/html" }, credentials: "same-origin" });
  if (!resposta.ok) throw new Error(`HTTP ${resposta.status}`);

  // O fragmento é o HTML do próprio servidor (o mesmo AdCard das demais listas)
  const documento = new DOMParser().parseFromString(await resposta.text(), "text/html");
  const itens = [...documento.querySelectorAll("li[data-ad-id]")];
  return { itens, voltaram: new Set(itens.map((li) => Number(li.dataset.adId))) };
}

async function carregar() {
  const host = campo("host");
  host.replaceChildren();
  for (const nome of ["error", "empty", "count", "unavailable", "blocked"]) mostrar(nome, false);

  if (!armazenamentoDisponivel()) {
    // S07: sem armazenamento não há o que listar
    mostrar("blocked");
    return;
  }

  const ids = lerFavoritos();
  if (ids.length === 0) {
    atualizarTotal();
    return;
  }

  const lista = document.createElement("ul");
  lista.className = "row row-cols-2 row-cols-md-3 row-cols-lg-4 g-3 list-unstyled mb-0";
  lista.dataset.adGrid = "";
  const ausentes = [];
  try {
    // Sem limite de favoritos: lotes de 100 ids por chamada, na ordem em que foram favoritados
    for (let inicio = 0; inicio < ids.length; inicio += LOTE) {
      const lote = ids.slice(inicio, inicio + LOTE);
      const { itens, voltaram } = await buscarLote(lote);
      lista.append(...itens);
      ausentes.push(...lote.filter((id) => !voltaram.has(id)));
    }
  } catch {
    mostrar("error");
    return;
  }

  host.append(lista);
  if (ausentes.length > 0) {
    // US-005-S06 e US-011-S04: o anúncio saiu do ar; some da lista e do armazenamento, com aviso
    if (!removerFavoritos(ausentes)) mostrarAviso(MENSAGEM_NAO_SALVOU);
    const aviso = campo("unavailable");
    aviso.textContent = avisoDeIndisponiveis(ausentes.length);
    aviso.hidden = false;
  }

  atualizarTotal();
}

if (raiz) {
  // Remover: tira da lista e do armazenamento; o contador do topo acompanha pelo evento de mudança
  raiz.addEventListener("click", (evento) => {
    const botao = evento.target.closest?.("[data-favorite-remove]");
    if (!botao) return;
    if (!removerFavoritos([Number(botao.dataset.adId)])) {
      mostrarAviso(MENSAGEM_NAO_SALVOU);
      return;
    }

    botao.closest("li[data-ad-id]")?.remove();
    atualizarTotal();
    document.querySelector("[data-favorites-title]")?.focus(); // o botão que tinha o foco sumiu
  });

  campo("retry")?.addEventListener("click", carregar);

  // Outra aba mudou a lista: esta mostra a lista nova
  aoMudar((aOutraAba) => {
    if (aOutraAba) carregar();
  });

  carregar();
}
