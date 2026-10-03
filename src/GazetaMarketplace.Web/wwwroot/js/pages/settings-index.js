// Tela de configurações do site: só reforça o formulário. Sem JavaScript, o envio comum continua funcionando.

const form = document.querySelector("[data-settings-form]");
const button = form?.querySelector("[data-submit-button]");

if (form && button) {
  const originalLabel = button.textContent;

  form.addEventListener("submit", () => {
    button.disabled = true;
    button.textContent = "Salvando…";
  });

  // Voltar pelo histórico traz a página do cache com o botão ainda travado
  window.addEventListener("pageshow", () => {
    button.disabled = false;
    button.textContent = originalLabel;
  });
}
