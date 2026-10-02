// Módulo carregado por todas as páginas (site público e painel).

const regiaoDeAvisos = document.getElementById("avisos-globais");

/** @param {string} texto */
function mostrarAviso(texto) {
  if (!regiaoDeAvisos) return;
  regiaoDeAvisos.textContent = texto;
  regiaoDeAvisos.hidden = false;
}

// Nenhuma falha assíncrona passa em silêncio (rules/frontend.md §Error Handling)
window.addEventListener("unhandledrejection", (evento) => {
  if (evento.reason?.name === "AbortError") return;
  const referencia = evento.reason?.traceId ? ` Código de referência: ${evento.reason.traceId}.` : "";
  mostrarAviso(`Algo deu errado. Tente de novo em alguns instantes.${referencia}`);
});

// Página de estado (vazio, sem resultado, erro): o título recebe o foco
document.querySelector("[data-foco-inicial]")?.focus();
