/** Atualiza o contador "X/N" de um campo (título, descrição). A quebra de linha conta 1, como o servidor passa a contar. */
export function atualizarContador(campo) {
  const alvo = document.querySelector(`[data-counter-for="${campo.id}"]`);
  if (!alvo) return;
  alvo.textContent = `${campo.value.length}/${alvo.dataset.counterMax}`;
}

/** Liga os contadores de todo campo `[data-counter-input]`, inclusive aos que a troca de categoria colocar na página depois. */
export function ativarContadores() {
  document.addEventListener("input", (evento) => {
    const campo = evento.target.closest?.("[data-counter-input]");
    if (campo) atualizarContador(campo);
  });
}
