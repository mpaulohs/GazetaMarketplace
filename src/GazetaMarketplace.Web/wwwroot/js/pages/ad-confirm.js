// Confirmações do painel (enviar à revisão, publicar, rejeitar): o primeiro clique trava o botão, para um clique duplo não mandar dois pedidos.
// (Se mandasse, o servidor responderia "já foi decidido" a segunda vez; travar evita até esse pedido.)
const formulario = document.querySelector("[data-confirm-submit]");
const botao = formulario?.querySelector("[data-confirm-button]");

if (formulario && botao) {
  const rotulo = botao.textContent;
  formulario.addEventListener("submit", (evento) => {
    if (botao.disabled) {
      evento.preventDefault();
      return;
    }

    botao.disabled = true;
    botao.textContent = botao.dataset.busyLabel ?? "Enviando…";
  });

  // Voltar pelo histórico traz a página do cache com o botão ainda travado
  window.addEventListener("pageshow", () => {
    botao.disabled = false;
    botao.textContent = rotulo;
  });
}

// Página que volta com erro de campo (motivo da rejeição em branco): o foco vai para o campo com problema
document.querySelector("textarea[autofocus], input[autofocus]")?.focus();
