/** Erro devolvido pelo servidor no formato ProblemDetails (RFC 7807) ou falha de rede (status 0). */
export class ApiError extends Error {
  /** @param {{ status?: number, title?: string, detail?: string, code?: string, errors?: Record<string, string[]>, traceId?: string }} problema */
  constructor(problema) {
    super(problema.detail ?? problema.title ?? "Não foi possível concluir a operação.");
    this.name = "ApiError";
    this.status = problema.status ?? 0;
    this.code = problema.code;
    this.errors = problema.errors ?? {};
    this.traceId = problema.traceId;
  }
}

// Páginas com formulário POST trazem o token num campo oculto; as demais, numa meta
const tokenAntiforgery = () =>
  document.querySelector("meta[name='request-verification-token']")?.content ??
  document.querySelector("input[name='__RequestVerificationToken']")?.value;

/**
 * Chama um endpoint JSON do próprio site. Escritas levam o token antiforgery no cabeçalho.
 * @param {string} url
 * @param {{ method?: string, body?: object | FormData, signal?: AbortSignal }} [opcoes] um FormData segue como está (envio de arquivo); qualquer outro objeto vai como JSON
 * @returns {Promise<any>} o JSON da resposta, ou null quando não há corpo (204)
 * @throws {ApiError}
 */
export async function apiFetch(url, { method = "GET", body, signal } = {}) {
  const cabecalhos = { Accept: "application/json" };
  const ehFormulario = body instanceof FormData;
  if (body !== undefined && !ehFormulario) cabecalhos["Content-Type"] = "application/json";

  if (method !== "GET") {
    const token = tokenAntiforgery();
    if (!token) throw new ApiError({ status: 0, title: "Esta página não permite essa operação." });
    cabecalhos["RequestVerificationToken"] = token;
  }

  let resposta;
  try {
    resposta = await fetch(url, {
      method,
      headers: cabecalhos,
      body: body === undefined || ehFormulario ? body : JSON.stringify(body),
      credentials: "same-origin",
      signal,
    });
  } catch (erro) {
    if (erro?.name === "AbortError") throw erro;
    throw new ApiError({ status: 0, title: "Sem conexão com o servidor. Tente de novo." });
  }

  if (!resposta.ok) {
    const problema = await resposta.json().catch(() => ({ status: resposta.status }));
    throw new ApiError(problema);
  }

  return resposta.status === 204 ? null : resposta.json();
}
