// Tela de categorias: melhoria progressiva. Sem JavaScript, "Excluir" abre uma página de confirmação e "Mover" é um formulário.

const dialog = document.getElementById("dialogo-exclusao");
const form = dialog?.querySelector("[data-dialog-form]");

if (dialog && form && typeof dialog.showModal === "function") {
  const nameTarget = dialog.querySelector("[data-dialog-name]");

  // Só os links com data-confirm="true" abrem a janela; o de categoria protegida segue direto para a mensagem de bloqueio
  document.addEventListener("click", (event) => {
    const link = event.target.closest("[data-action='delete'][data-confirm='true']");
    if (!link) return;
    event.preventDefault();
    form.action = link.href;
    nameTarget.textContent = link.dataset.categoryName;
    dialog.showModal();
  });

  dialog.querySelector("[data-dialog-cancel]")?.addEventListener("click", () => dialog.close());
}

// Depois de mover, o foco volta para o botão usado, para quem navega por teclado ou leitor de tela não perder o lugar.
// O endereço leva a âncora do item (#categoria-N), e o navegador move o foco para ela ao terminar de carregar: por isso o foco é
// devolvido só depois do evento "load", e não na hora em que o módulo roda.
const focusId = document.querySelector("[data-focus-id]")?.dataset.focusId;
if (focusId) {
  const restoreFocus = () => setTimeout(() => document.getElementById(focusId)?.focus(), 0);
  if (document.readyState === "complete") {
    restoreFocus();
  } else {
    window.addEventListener("load", restoreFocus, { once: true });
  }
}
