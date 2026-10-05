// Página de busca (US-002). O formulário já funciona sem JavaScript (GET comum); aqui só enriquecemos:
// recolher o painel em tela estreita (S12), trocar a UF limpa a cidade (S10), marca → modelo, mostrar os filtros da categoria,
// conferir a faixa invertida antes de enviar (S08) e dar retorno ao enviar.

import { apiFetch } from "../modules/api.js";

const TELA_ESTREITA = window.matchMedia("(max-width: 991.98px)");

const MENSAGEM_DA_FAIXA = {
  preco: "O preço mínimo não pode ser maior que o máximo",
  ano: "O ano inicial não pode ser maior que o final",
  area: "A área mínima não pode ser maior que a máxima",
};

/** Mesma gramática do servidor (DecimalInput): 50000, 50.000, 50.000,00, 50000,5 ou 50000.50. NaN quando ilegível. */
export function lerNumero(texto) {
  const t = String(texto ?? "").trim().replace(/^R\$\s*/i, "");
  if (/^\d+$/.test(t)) return Number(t);
  if (/^\d{1,3}(\.\d{3})+(,\d{1,2})?$/.test(t)) return Number(t.replaceAll(".", "").replace(",", "."));
  if (/^\d+,\d{1,2}$/.test(t)) return Number(t.replace(",", "."));
  if (/^\d+\.\d{1,2}$/.test(t)) return Number(t);
  return Number.NaN;
}

/** Deixa só a primeira opção ("Todas as…") do select. */
function esvaziar(select) {
  while (select.options.length > 1) select.remove(1);
  select.value = "";
}

function preencher(select, itens) {
  esvaziar(select);
  for (const { valor, rotulo } of itens) select.add(new Option(rotulo, valor));
}

function configurarPainel() {
  const painel = document.getElementById("filtros");
  const alternar = document.querySelector("[data-filters-toggle]");
  const fechar = document.querySelector("[data-filters-close]");
  if (!painel || !alternar) return;

  alternar.hidden = false;
  if (fechar) fechar.hidden = false;

  // Painel aberto no servidor; em tela estreita recolhe, a não ser que haja erro a mostrar
  if (TELA_ESTREITA.matches && painel.dataset.keepOpen !== "true") painel.classList.remove("show");

  const sincronizar = () => {
    const aberto = painel.classList.contains("show");
    alternar.setAttribute("aria-expanded", String(aberto));
    alternar.textContent = aberto ? "Filtros ▴" : "Filtros ▾";
  };
  sincronizar();
  painel.addEventListener("shown.bs.collapse", sincronizar);
  painel.addEventListener("hidden.bs.collapse", () => {
    sincronizar();
    // O foco não pode ficar num botão que sumiu
    if (painel.contains(document.activeElement) || document.activeElement === document.body) alternar.focus();
  });
}

function configurarLocal(form) {
  const uf = form.querySelector("[data-filter-uf]");
  const cidade = form.querySelector("[data-filter-city]");
  if (!uf || !cidade) return;

  uf.addEventListener("change", async () => {
    esvaziar(cidade); // trocar a UF limpa a cidade (S10)
    if (!uf.value) {
      cidade.disabled = true;
      return;
    }

    const pedida = uf.value;
    cidade.disabled = true;
    try {
      const lista = await apiFetch(`/api/v1/public/cities?uf=${encodeURIComponent(pedida)}`);
      if (uf.value !== pedida) return; // a pessoa já escolheu outra UF
      preencher(cidade, lista.map((c) => ({ valor: c.name, rotulo: c.name })));
    } finally {
      if (uf.value === pedida) cidade.disabled = false;
    }
  });
}

function configurarCatalogo(form) {
  const marca = form.querySelector("[data-filter-brand]");
  const modelo = form.querySelector("[data-filter-model]");
  if (!marca || !modelo) return;

  marca.addEventListener("change", async () => {
    esvaziar(modelo);
    modelo.options[0].text = marca.value ? "Todos os modelos" : "Escolha a marca";
    modelo.disabled = true;
    if (!marca.value) return;

    const escolhida = marca.value;
    try {
      const lista = await apiFetch(`/api/v1/vehicle-catalog/brands/${encodeURIComponent(escolhida)}/models?kind=${encodeURIComponent(form.dataset.kind)}`);
      if (marca.value !== escolhida) return;
      preencher(modelo, lista.map((m) => ({ valor: String(m.id), rotulo: m.name })));
    } finally {
      modelo.disabled = marca.value !== escolhida ? modelo.disabled : false;
    }
  });

}

async function carregarMarcas(form, tipo) {
  const marca = form.querySelector("[data-filter-brand]");
  const lista = await apiFetch(`/api/v1/vehicle-catalog/brands?kind=${encodeURIComponent(tipo)}`);
  if (form.dataset.kind !== tipo) return; // a categoria mudou de novo durante a espera
  preencher(marca, lista.map((m) => ({ valor: String(m.id), rotulo: m.name })));
}

function configurarCategoria(form) {
  const categoria = form.querySelector("[data-filter-category]");
  const marca = form.querySelector("[data-filter-brand]");
  const modelo = form.querySelector("[data-filter-model]");
  if (!categoria) return;

  categoria.addEventListener("change", async () => {
    const opcao = categoria.selectedOptions[0];
    const filtros = (opcao?.dataset.filters ?? "").split(" ").filter(Boolean);
    const tipo = opcao?.dataset.kind ?? "";

    // Cada bloco aparece (e vale) só se a categoria escolhida o oferece; o que some não vai no endereço
    for (const bloco of form.querySelectorAll("[data-filter]")) {
      const ligado = filtros.includes(bloco.dataset.filter);
      bloco.hidden = !ligado;
      for (const controle of bloco.querySelectorAll("input, select")) {
        controle.disabled = !ligado;
        if (!ligado) controle.value = "";
      }

      limparErro(form, bloco.querySelector("[data-error-for]")?.dataset.errorFor);
    }

    if (marca && filtros.includes("brand") && tipo) {
      if (form.dataset.kind !== tipo) {
        form.dataset.kind = tipo; // Carros e Motos têm marcas diferentes
        esvaziar(marca);
        if (modelo) {
          esvaziar(modelo);
          modelo.options[0].text = "Escolha a marca";
        }

        await carregarMarcas(form, tipo);
      }
    } else {
      form.dataset.kind = "";
    }

    if (modelo && filtros.includes("model")) modelo.disabled = !marca?.value;
  });
}

function limparErro(form, nome) {
  if (!nome) return;
  const area = form.querySelector(`[data-error-for="${nome}"]`);
  if (area) {
    area.textContent = "";
    area.hidden = true;
  }

  for (const campo of form.querySelectorAll(`[data-range="${nome}"]`)) campo.classList.remove("is-invalid");
}

function configurarEnvio(form) {
  const enviar = form.querySelector("[data-search-submit]");
  const rotulo = enviar?.textContent;

  for (const campo of form.querySelectorAll("[data-range]")) campo.addEventListener("input", () => limparErro(form, campo.dataset.range));

  let desligados = [];
  const restaurar = () => {
    for (const controle of desligados) controle.disabled = false;
    desligados = [];
    if (enviar) {
      enviar.disabled = false;
      enviar.textContent = rotulo;
    }
  };

  form.addEventListener("submit", (evento) => {
    // Faixa invertida: erro junto do campo e a lista não muda (S08)
    let primeiro = null;
    for (const [nome, mensagem] of Object.entries(MENSAGEM_DA_FAIXA)) {
      const campos = [...form.querySelectorAll(`[data-range="${nome}"]`)];
      if (campos.length !== 2 || campos[0].disabled) continue;
      const [minimo, maximo] = campos;
      const invertida = minimo.value.trim() !== "" && maximo.value.trim() !== "" && lerNumero(minimo.value) > lerNumero(maximo.value);
      const area = form.querySelector(`[data-error-for="${nome}"]`);
      for (const campo of campos) campo.classList.toggle("is-invalid", invertida);
      if (area) {
        area.textContent = invertida ? mensagem : "";
        area.hidden = !invertida;
      }

      if (invertida && !primeiro) primeiro = minimo;
    }

    if (primeiro) {
      evento.preventDefault();
      primeiro.focus();
      return;
    }

    // Endereço enxuto: campo vazio não vai; e o botão mostra que a busca está andando
    for (const controle of form.elements) {
      if (controle.name && !controle.disabled && controle.value === "") {
        controle.disabled = true;
        desligados.push(controle);
      }
    }

    if (enviar) {
      enviar.disabled = true;
      enviar.textContent = "Buscando…";
    }
  });

  // Voltar com o botão do navegador pode trazer a página de volta como estava: o botão não pode ficar travado
  window.addEventListener("pageshow", restaurar);
}

function configurarOrdem() {
  const formulario = document.querySelector("[data-order-form]");
  const select = formulario?.querySelector("[data-order-select]");
  if (!formulario || !select) return;

  formulario.querySelector("[data-order-apply]").hidden = true; // com JavaScript, escolher a ordem já envia
  select.addEventListener("change", () => formulario.requestSubmit());
}

const form = document.querySelector("[data-search-form]");
if (form) {
  configurarPainel();
  configurarLocal(form);
  configurarCatalogo(form);
  configurarCategoria(form);
  configurarEnvio(form);
  configurarOrdem();
}
