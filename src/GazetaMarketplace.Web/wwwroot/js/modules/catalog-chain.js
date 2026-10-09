import { apiFetch } from "./api.js";

const NIVEIS = ["brand", "model", "year", "version"];
const BASE = "/api/v1/vehicle-catalog";

const campo = (nivel) => document.querySelector(`select[data-catalog-level='${nivel}']`);

function esvaziar(select, textoVazio) {
  select.replaceChildren(new Option(textoVazio, ""));
  select.disabled = true;
}

function preencher(select, itens) {
  select.replaceChildren(new Option("Selecione", ""), ...itens.map(({ valor, rotulo }) => new Option(rotulo, valor)));
  select.disabled = false;
}

function endereco(nivel, tipo, select) {
  const consulta = `kind=${encodeURIComponent(tipo)}`;
  switch (nivel) {
    case "brand":
      return `${BASE}/brands/${select.value}/models?${consulta}`;
    case "model":
      return `${BASE}/models/${select.value}/years?${consulta}`;
    default: {
      const modelo = campo("model")?.value;
      return `${BASE}/models/${modelo}/years/${select.value}/versions?${consulta}`;
    }
  }
}

/** Marca → Modelo → Ano → Versão: ao escolher um nível, o seguinte recebe as opções do servidor e os de baixo são esvaziados. */
export function ativarCadeiaDoCatalogo() {
  document.addEventListener("change", async (evento) => {
    const select = evento.target.closest?.("select[data-catalog-level]");
    if (!select) return;

    // Selects comuns (Câmbio, Cor…) não têm nível, mas trazem o atributo vazio: não fazem parte da cadeia
    const nivel = select.dataset.catalogLevel;
    if (!NIVEIS.includes(nivel)) return;
    const seguintes = NIVEIS.slice(NIVEIS.indexOf(nivel) + 1);
    for (const nome of seguintes) {
      const filho = campo(nome);
      if (filho) esvaziar(filho, "Escolha o campo anterior primeiro");
    }

    const proximo = campo(seguintes[0]);
    if (!select.value || !proximo) return;

    try {
      const dados = await apiFetch(endereco(nivel, select.dataset.catalogKind, select));
      // Ano vem como número puro; marca, modelo e versão vêm como { id, name }
      preencher(proximo, dados.map((item) => (typeof item === "number" ? { valor: String(item), rotulo: String(item) } : { valor: String(item.id), rotulo: item.name })));
    } catch (erro) {
      if (erro?.name === "AbortError") return;
      esvaziar(proximo, "Não foi possível carregar a lista. Escolha o campo anterior de novo.");
    }
  });
}
