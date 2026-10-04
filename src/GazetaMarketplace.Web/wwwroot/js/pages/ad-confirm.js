// Confirmação do envio à revisão: o primeiro clique trava o botão, para um clique duplo não mandar dois pedidos.
// (Se mandasse, o servidor responderia "já foi enviado" a segunda vez; travar evita até esse pedido.)
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
    botao.textContent = "Enviando…";
  });

  // Voltar pelo histórico traz a página do cache com o botão ainda travado
  window.addEventListener("pageshow", () => {
    botao.disabled = false;
    botao.textContent = rotulo;
  });
}
