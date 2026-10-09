/**
 * Formata um número inteiro enquanto a pessoa digita: só dígitos, sem zeros à esquerda, no máximo `maximoDeDigitos` e com ponto de milhar
 * ("1234567" → "1.234.567"). Letra, espaço e sinal são descartados; um número colado grande demais é cortado no limite.
 * O servidor continua sendo quem valida (recusa o que passar do teto); aqui só se ajuda a digitar.
 * @param {string} texto
 * @param {number} maximoDeDigitos
 * @returns {string}
 */
export function formatarInteiro(texto, maximoDeDigitos) {
  const digitos = String(texto ?? "").replace(/\D/g, "").replace(/^0+(?=\d)/, "").slice(0, maximoDeDigitos);
  return digitos.replace(/\B(?=(\d{3})+(?!\d))/g, ".");
}

/** Liga a máscara a todo campo `[data-integer-input]`, inclusive aos que a troca de categoria colocar na página depois. */
export function ativarMascaraDeInteiro() {
  document.addEventListener("input", (evento) => {
    const campo = evento.target.closest?.("[data-integer-input]");
    if (!campo) return;
    const maximo = Number.parseInt(campo.dataset.maxDigits ?? "", 10);
    campo.value = formatarInteiro(campo.value, Number.isFinite(maximo) ? maximo : 10);
  });
}
