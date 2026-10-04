import { apiFetch, ApiError } from "./api.js";

/** Quantas vezes o endpoint é chamado no total: a primeira e uma nova tentativa só se o serviço falhar (ADR-007, NFR-24). */
export const MAX_TENTATIVAS = 2;

/** Deixa só os dígitos de um CEP digitado ("13015-100" → "13015100"). */
export const somenteDigitos = (texto) => String(texto ?? "").replace(/\D/g, "");

/**
 * Consulta o CEP no servidor e padroniza o resultado para a tela (US-008-S14).
 *
 * - Menos de 8 dígitos: não chama o servidor (`incompleto`).
 * - 200: `encontrado`, com cidade e UF.
 * - 404: `naoEncontrado`. NÃO é falha do serviço e NÃO abre o preenchimento manual (S24).
 * - 503, falha de rede ou qualquer outra falha: tenta mais uma vez, avisando a tela pela `aoTentar(2, 2)` ("Buscando… (tentativa 2 de 2)");
 *   falhando de novo, `indisponivel` e a tela abre UF e cidade em listas.
 *
 * @param {string} cepDigitado
 * @param {{ aoTentar?: (tentativa: number, total: number) => void, signal?: AbortSignal }} [opcoes]
 * @returns {Promise<{ status: "incompleto" | "encontrado" | "naoEncontrado" | "indisponivel", cep?: string, cidade?: string, uf?: string, origem?: string, mensagem?: string }>}
 */
export async function consultarCep(cepDigitado, { aoTentar, signal } = {}) {
  const cep = somenteDigitos(cepDigitado);
  if (cep.length !== 8) return { status: "incompleto", cep };

  for (let tentativa = 1; tentativa <= MAX_TENTATIVAS; tentativa++) {
    if (tentativa > 1) aoTentar?.(tentativa, MAX_TENTATIVAS);
    try {
      const resposta = await apiFetch(`/api/v1/cep/${cep}`, { signal });
      return { status: "encontrado", cep, cidade: resposta.city, uf: resposta.uf, origem: resposta.source };
    } catch (erro) {
      if (erro?.name === "AbortError") throw erro;
      if (erro instanceof ApiError && erro.status === 404) {
        return { status: "naoEncontrado", cep, mensagem: erro.message };
      }
      // 503 (serviço fora), falha de rede (status 0), 429 e qualquer outro erro: o preenchimento manual sempre funciona
      if (tentativa === MAX_TENTATIVAS) return { status: "indisponivel", cep, mensagem: erro?.message };
    }
  }

  return { status: "indisponivel", cep };
}

/**
 * Cidades de uma UF para o preenchimento manual. Lista vazia: a UF ainda não tem carga do IBGE e a tela mostra um campo de texto.
 * @param {string} uf
 * @param {{ signal?: AbortSignal }} [opcoes]
 * @returns {Promise<Array<{ ibgeCode: number, name: string }>>}
 */
export function listarCidades(uf, { signal } = {}) {
  return apiFetch(`/api/v1/cities?uf=${encodeURIComponent(uf)}`, { signal });
}
