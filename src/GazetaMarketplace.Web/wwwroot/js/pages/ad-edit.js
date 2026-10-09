// Formulário do anúncio: melhoria progressiva. Sem JavaScript tudo funciona com envio comum e o botão "Atualizar campos".
import { consultarCep, listarCidades, somenteDigitos } from "../modules/cep.js";
import { ativarCadeiaDoCatalogo } from "../modules/catalog-chain.js";
import { atualizarContador, ativarContadores } from "../modules/counter.js";
import { ativarFotos } from "../modules/photos.js";
import { ativarMascaraDeInteiro } from "../modules/integer.js";
import { ativarMascaraDePreco } from "../modules/price.js";

const formulario = document.querySelector("[data-ad-form]");

if (formulario) {
  ativarMascaraDePreco();
  ativarMascaraDeInteiro();
  ativarContadores();
  ativarCadeiaDoCatalogo();
  ativarFotos();
  trocaDeCategoria(formulario);
  localizacao(formulario);
  envio(formulario);

  pendencias();

  // Depois de uma recusa do servidor, o foco vai para a lista de pendências ou para o primeiro campo com erro
  (document.querySelector("[data-pendings]") ?? formulario.querySelector("[aria-invalid='true']") ?? document.querySelector("[data-error-summary]"))?.focus();
}

/** Cada item da lista de pendências leva o foco ao campo indicado (o link sozinho só rola a página). */
function pendencias() {
  for (const link of document.querySelectorAll("[data-pending-link]")) {
    link.addEventListener("click", (evento) => {
      const alvo = document.getElementById(link.getAttribute("href").slice(1));
      if (!alvo) return;
      evento.preventDefault();
      alvo.scrollIntoView({ block: "center" });
      alvo.focus();
    });
  }
}

/** Guarda o que foi digitado, por nome de campo, para devolver depois que a região for refeita. */
function coletar(regiao) {
  const valores = new Map();
  for (const controle of regiao.querySelectorAll("input[name], select[name], textarea[name]")) {
    if (controle.type === "checkbox") {
      if (controle.checked) valores.set(controle.name, [...(valores.get(controle.name) ?? []), controle.value]);
    } else {
      valores.set(controle.name, [controle.value]);
    }
  }

  return valores;
}

function restaurar(regiao, valores) {
  for (const controle of regiao.querySelectorAll("input[name], select[name], textarea[name]")) {
    const guardado = valores.get(controle.name);
    if (!guardado) continue;
    if (controle.type === "checkbox") {
      controle.checked = guardado.includes(controle.value);
    } else if (controle.tagName === "SELECT") {
      // Só volta se a opção também existe na categoria nova
      if ([...controle.options].some((opcao) => opcao.value === guardado[0])) controle.value = guardado[0];
    } else {
      controle.value = guardado[0];
    }

    if (controle.matches("[data-counter-input]")) atualizarContador(controle);
  }
}

/** A resposta de um fetch de HTML foi a tela de entrada: a sessão da pessoa venceu. */
class SessaoVencida extends Error {}

/** Troca a categoria sem recarregar: o servidor devolve o trecho do formulário do novo grupo; o que é comum continua preenchido. */
function trocaDeCategoria(form) {
  const selecao = form.querySelector("[data-category-select]");
  const regiao = form.querySelector("[data-group-region]");
  const aviso = document.querySelector("[data-ad-status]");
  if (!selecao || !regiao) return;

  let controlador;
  selecao.addEventListener("change", async () => {
    controlador?.abort();
    controlador = new AbortController();
    const endereco = `${form.dataset.fieldsUrl}?categoryId=${encodeURIComponent(selecao.value)}`;
    // Campo em que a pessoa digitou depois de escolher a categoria, enquanto o servidor respondia
    let digitando = null;
    const aoDigitar = (evento) => {
      digitando = evento.target.name || null;
    };
    regiao.addEventListener("input", aoDigitar);

    try {
      const resposta = await fetch(endereco, { credentials: "same-origin", headers: { Accept: "text/html" }, signal: controlador.signal });
      if (!resposta.ok) throw new Error(`HTTP ${resposta.status}`);
      // Sessão vencida: o servidor redireciona para a tela de entrada e o fetch a segue com 200. Aquela página nunca entra no formulário (R-09)
      if (resposta.redirected && new URL(resposta.url).pathname.startsWith("/painel/entrar")) throw new SessaoVencida();
      // O HTML é a parcial do Razor, nunca texto montado no navegador. O DOMParser só lê (não executa script) e os nós passam para a página (RC-17: sem innerHTML)
      const documento = new DOMParser().parseFromString(await resposta.text(), "text/html");
      // Os valores são lidos só agora, com a resposta já na mão: o que a pessoa digitou enquanto o servidor respondia não se perde
      const valores = coletar(regiao);
      regiao.replaceChildren(...documento.body.childNodes);
      restaurar(regiao, valores);
      if (aviso) aviso.textContent = `Campos atualizados para a categoria ${selecao.selectedOptions[0]?.textContent ?? ""}.`;
      // Quem digitou num campo que continua existindo segue nele; nos demais casos o foco volta à categoria (S09)
      const continuaDigitando = digitando ? [...regiao.querySelectorAll("input[name], select[name], textarea[name]")].find((controle) => controle.name === digitando) : null;
      (continuaDigitando ?? selecao).focus();
    } catch (erro) {
      if (erro?.name === "AbortError") return;
      if (erro instanceof SessaoVencida) {
        // O botão "Atualizar campos" cairia no mesmo redirecionamento: a saída é entrar de novo (em outra aba), e o que foi digitado fica na tela até lá
        const alerta = document.querySelector("[data-session-expired]");
        if (alerta) {
          alerta.hidden = false;
          alerta.focus();
        } else if (aviso) {
          aviso.textContent = "Sua sessão expirou. Entre de novo para continuar; o que você digitou neste formulário ainda não foi salvo.";
        }
        return;
      }
      if (aviso) aviso.textContent = "Não foi possível atualizar os campos. Use o botão Atualizar campos.";
      document.getElementById("atualizar-campos")?.classList.remove("somente-sem-js");
    } finally {
      regiao.removeEventListener("input", aoDigitar);
    }
  });
}

/** CEP: máscara, busca com "tentativa 2 de 2" e preenchimento manual quando o serviço falha (US-008-S14). */
function localizacao(form) {
  const campoCep = form.querySelector("[data-cep-input]");
  const status = form.querySelector("[data-cep-status]");
  const cepDaCidade = form.querySelector("[data-location-cep]");
  const sinalManual = form.querySelector("[data-location-manual]");
  const automaticos = [...form.querySelectorAll("[data-auto-location]")];
  const manuais = [...form.querySelectorAll("[data-manual-location]")];
  if (!campoCep || !status) return;

  let controlador;

  const definirModo = (manual) => {
    sinalManual.value = manual ? "true" : "false";
    for (const bloco of automaticos) {
      bloco.hidden = manual;
      bloco.querySelectorAll("input").forEach((c) => (c.disabled = manual));
    }

    for (const bloco of manuais) {
      bloco.hidden = !manual;
      bloco.querySelectorAll("input, select").forEach((c) => (c.disabled = !manual));
    }
  };

  const cidadeAutomatica = form.querySelector("[data-city-auto]");
  const ufAutomatica = form.querySelector("[data-uf-auto]");
  const limparAutomatico = () => {
    cidadeAutomatica.value = "";
    ufAutomatica.value = "";
    cepDaCidade.value = "";
  };

  const buscar = async (digitos) => {
    controlador?.abort();
    controlador = new AbortController();
    status.className = "form-text";
    status.textContent = "Buscando…";
    try {
      const resultado = await consultarCep(digitos, {
        signal: controlador.signal,
        aoTentar: (tentativa, total) => (status.textContent = `Buscando… (tentativa ${tentativa} de ${total})`),
      });

      if (resultado.status === "encontrado") {
        definirModo(false);
        cidadeAutomatica.value = resultado.cidade;
        ufAutomatica.value = resultado.uf;
        cepDaCidade.value = digitos;
        status.textContent = "Cidade e UF preenchidas a partir do CEP.";
      } else if (resultado.status === "naoEncontrado") {
        limparAutomatico();
        status.className = "form-text text-danger";
        status.textContent = "CEP não encontrado. Confira os números.";
      } else if (resultado.status === "indisponivel") {
        limparAutomatico();
        definirModo(true);
        status.className = "form-text text-danger";
        status.textContent = "Não foi possível buscar o CEP. Preencha Cidade e UF manualmente.";
      }
    } catch (erro) {
      if (erro?.name !== "AbortError") throw erro;
    }
  };

  campoCep.addEventListener("input", () => {
    const digitos = somenteDigitos(campoCep.value).slice(0, 8);
    campoCep.value = digitos.length > 5 ? `${digitos.slice(0, 5)}-${digitos.slice(5)}` : digitos;
    if (digitos.length === 8) {
      buscar(digitos);
    } else {
      controlador?.abort();
      limparAutomatico();
      status.className = "form-text";
      status.textContent = "A cidade e a UF são preenchidas a partir do CEP.";
    }
  });

  campoCep.addEventListener("blur", () => {
    const digitos = somenteDigitos(campoCep.value);
    if (digitos.length > 0 && digitos.length < 8) {
      status.className = "form-text text-danger";
      status.textContent = "Informe um CEP com 8 dígitos";
    }
  });

  // Modo manual: a lista de cidades acompanha a UF escolhida
  form.querySelector("[data-uf-manual]")?.addEventListener("change", async (evento) => {
    const uf = evento.target.value;
    const atual = form.querySelector("[data-city-manual]");
    if (!atual) return;
    if (!uf) {
      trocarCidade(atual, [], "Escolha a UF primeiro");
      return;
    }

    try {
      trocarCidade(atual, await listarCidades(uf), "Escolha a cidade");
    } catch (erro) {
      if (erro?.name !== "AbortError") status.textContent = "Não foi possível carregar as cidades. Digite o nome da cidade.";
    }
  });
}

/** Lista de cidades da UF; sem lista carregada (UF fora da carga do IBGE), a cidade vira um campo de texto. */
function trocarCidade(atual, cidades, textoVazio) {
  const comum = { id: atual.id, name: atual.name };
  let novo;
  if (cidades.length > 0 || textoVazio === "Escolha a UF primeiro") {
    novo = document.createElement("select");
    novo.className = "form-select";
    novo.append(new Option(textoVazio, ""), ...cidades.map((cidade) => new Option(cidade.name, cidade.name)));
  } else {
    novo = document.createElement("input");
    novo.className = "form-control";
    novo.maxLength = 80;
    novo.autocomplete = "off";
  }

  Object.assign(novo, comum);
  novo.setAttribute("data-city-manual", "");
  atual.replaceWith(novo);
}

/**
 * Trava os botões de salvar e enviar enquanto envia, para um clique duplo não criar dois rascunhos nem dois envios.
 * Os botões são lidos na hora do envio: o "Salvar rascunho" da seção de fotos nasce e some junto com os campos da categoria.
 */
function envio(form) {
  const botoes = () => [...document.querySelectorAll("[data-submit-button]")];
  if (botoes().length === 0) return;
  const rotulos = new Map();
  form.addEventListener("submit", (evento) => {
    // "Atualizar campos" também envia o formulário, mas não deve travar os botões
    if (evento.submitter && !evento.submitter.matches("[data-submit-button]")) return;
    for (const botao of botoes()) {
      rotulos.set(botao, botao.textContent);
      botao.disabled = true;
      if (botao === evento.submitter) botao.textContent = botao.dataset.submitReview === undefined ? "Salvando…" : "Enviando…";
    }
  });

  // Voltar pelo histórico traz a página do cache com os botões ainda travados
  window.addEventListener("pageshow", () => {
    for (const [botao, rotulo] of rotulos) {
      botao.disabled = false;
      botao.textContent = rotulo;
    }
  });
}
