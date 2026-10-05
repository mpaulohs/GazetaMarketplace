// Favoritos do visitante (US-005): moram só no navegador (localStorage, suposição S1); o servidor não guarda nada.
// A lista é um array JSON de ids numéricos, sem repetição, na ordem em que foram favoritados. Tudo aqui tolera um armazenamento
// bloqueado, cheio ou com lixo dentro: nada lança, e quem chama recebe `ok: false` para mostrar a mensagem.

export const CHAVE = "gazeta:favoritos:v1";
export const EVENTO = "gazeta:favoritos";
const ID_MAXIMO = 2147483647;

/** O armazenamento do navegador, ou nulo se ele bloqueia até o acesso (modo privado restrito, política do navegador). */
function armazenamento() {
  try {
    return window.localStorage ?? null;
  } catch {
    return null;
  }
}

/**
 * Deixa só ids inteiros de 1 a 2147483647, sem repetição e na ordem. Qualquer outra coisa (JSON quebrado, objeto, texto, número com vírgula) vira lista vazia ou perde o item inválido.
 * @param {unknown} valor
 * @returns {number[]}
 */
export function limpar(valor) {
  if (!Array.isArray(valor)) return [];
  const vistos = new Set();
  for (const item of valor) {
    if (Number.isInteger(item) && item >= 1 && item <= ID_MAXIMO) vistos.add(item);
  }
  return [...vistos];
}

/** @returns {boolean} verdadeiro se o navegador deixa guardar e apagar um valor */
export function armazenamentoDisponivel() {
  const store = armazenamento();
  if (!store) return false;
  try {
    const teste = `${CHAVE}:teste`;
    store.setItem(teste, "1");
    store.removeItem(teste);
    return true;
  } catch {
    return false;
  }
}

/** @returns {number[]} os ids favoritados (vazio se não há nada, se o valor guardado está quebrado ou se o armazenamento está bloqueado) */
export function lerFavoritos() {
  try {
    const bruto = armazenamento()?.getItem(CHAVE);
    return bruto ? limpar(JSON.parse(bruto)) : [];
  } catch {
    return [];
  }
}

/** @param {number} id */
export function ehFavorito(id) {
  return lerFavoritos().includes(id);
}

function gravar(ids) {
  const store = armazenamento();
  if (!store) return false;
  try {
    store.setItem(CHAVE, JSON.stringify(ids));
  } catch {
    return false;
  }

  // A própria aba não recebe o evento "storage" (ele só vai para as outras abas): avisa por um evento nosso
  window.dispatchEvent(new CustomEvent(EVENTO));
  return true;
}

/**
 * Favorita se não é favorito, desfavorita se é.
 * @param {number} id
 * @returns {{ ok: boolean, favorito: boolean }} `ok` falso = não deu para salvar (o estado não mudou)
 */
export function alternarFavorito(id) {
  const atuais = lerFavoritos();
  const jaEh = atuais.includes(id);
  const novos = jaEh ? atuais.filter((x) => x !== id) : [...atuais, id];
  const ok = gravar(novos);
  return { ok, favorito: ok ? !jaEh : jaEh };
}

/**
 * Tira ids da lista (o anúncio saiu do ar ou o visitante clicou em "Remover").
 * @param {number[]} ids
 * @returns {boolean} falso se não deu para salvar
 */
export function removerFavoritos(ids) {
  if (ids.length === 0) return true;
  const remover = new Set(ids);
  return gravar(lerFavoritos().filter((x) => !remover.has(x)));
}

/**
 * Chama `ouvinte` quando a lista muda: nesta aba (nosso evento) ou em outra (evento "storage").
 * @param {(aOutraAba: boolean) => void} ouvinte `aOutraAba` é verdadeiro quando a mudança veio de outra aba
 */
export function aoMudar(ouvinte) {
  window.addEventListener(EVENTO, () => ouvinte(false));
  window.addEventListener("storage", (evento) => {
    if (evento.key === CHAVE || evento.key === null) ouvinte(true);
  });
}
