/** Tamanho máximo do preço em dígitos (R$ 99.999.999,99 = 10 dígitos de centavos). */
const MAXIMO_DE_DIGITOS = 10;

/**
 * Formata enquanto a pessoa digita, no estilo de caixa eletrônico: os dígitos entram pela direita e o valor sempre aparece com duas casas
 * ("6200000" → "62.000,00"). O servidor é quem lê o preço (um número sem vírgula são reais); aqui só se ajuda a digitar.
 * @param {string} texto
 * @returns {string}
 */
export function formatarPreco(texto) {
  const digitos = String(texto ?? "").replace(/\D/g, "").replace(/^0+/, "").slice(0, MAXIMO_DE_DIGITOS);
  if (!digitos) return "";
  const centavos = digitos.padStart(3, "0");
  const inteiro = centavos.slice(0, -2).replace(/\B(?=(\d{3})+(?!\d))/g, ".");
  return `${inteiro},${centavos.slice(-2)}`;
}

/** Liga a máscara a todo campo `[data-price-input]`, inclusive aos que a troca de categoria colocar na página depois. */
export function ativarMascaraDePreco() {
  document.addEventListener("input", (evento) => {
    const campo = evento.target.closest?.("[data-price-input]");
    if (!campo) return;
    campo.value = formatarPreco(campo.value);
  });
}
