// Fotos do anúncio novo: melhoria progressiva. O anúncio ainda não tem id, então as fotos ficam no navegador (escolher, ver, ordenar, remover)
// e sobem quando a pessoa salva o rascunho: o rascunho é gravado primeiro e cada foto vai pela API, na ordem da lista (a primeira é a capa).
// Sem JavaScript a seção continua oculta e a regra do formulário pede para salvar o rascunho antes.
import { ApiError, apiFetch } from "./api.js";

const EDICAO = /^\/painel\/anuncios\/(\d+)\/editar\/?$/;
const MENSAGEM_GRANDE = "A foto excede o limite de 10 MB";

/** Liga a seção de fotos do anúncio novo ao formulário. */
export function ativarFotosDoAnuncioNovo(form) {
  const secao = document.querySelector("[data-photos-new]");
  const campo = secao?.querySelector("[data-new-input]");
  const lista = secao?.querySelector("[data-new-list]");
  const modelo = secao?.querySelector("template[data-new-template]");
  if (!secao || !campo || !lista || !modelo) return;

  const titulo = secao.querySelector("[data-new-heading]");
  const aviso = secao.querySelector("[data-new-status]");
  const erro = secao.querySelector("[data-new-error]");
  const vazio = secao.querySelector("[data-new-empty]");
  const maximoDeBytes = Number(secao.dataset.maxBytes);
  const escolhidas = []; // { arquivo, url }, na ordem da lista

  // O limite vem da regra da categoria, que o servidor refaz a cada troca de categoria
  const maximo = () => Number(document.querySelector("[data-photo-rule]")?.dataset.photoMax ?? 0);

  const anunciar = (texto) => {
    aviso.textContent = "";
    requestAnimationFrame(() => (aviso.textContent = texto));
  };

  const mostrarErro = (texto, { focar = true } = {}) => {
    erro.textContent = texto;
    erro.hidden = !texto;
    if (texto && focar) erro.focus();
  };

  const desenhar = () => {
    const limite = maximo();
    secao.hidden = limite === 0;
    titulo.textContent = `Fotos (${escolhidas.length} de ${limite})`;
    vazio.hidden = escolhidas.length > 0;
    campo.disabled = escolhidas.length >= limite;

    lista.replaceChildren(
      ...escolhidas.map((foto, indice) => {
        const item = modelo.content.firstElementChild.cloneNode(true);
        const numero = indice + 1;
        const imagem = item.querySelector("img");
        imagem.src = foto.url;
        imagem.alt = `Foto ${numero}`;
        // HEIC e formatos que o navegador não desenha: fica o nome do arquivo
        imagem.addEventListener("error", () => (imagem.hidden = true), { once: true });
        item.querySelector("[data-new-name]").textContent = foto.arquivo.name;
        item.querySelector("[data-show-on-cover]").hidden = indice !== 0;
        item.querySelector("[data-action='move-up']").disabled = indice === 0;
        item.querySelector("[data-action='move-down']").disabled = indice === escolhidas.length - 1;
        item.querySelector("[data-action='move-up']").setAttribute("aria-label", `Subir a foto ${numero}`);
        item.querySelector("[data-action='move-down']").setAttribute("aria-label", `Descer a foto ${numero}`);
        item.querySelector("[data-action='remove-new']").setAttribute("aria-label", `Remover a foto ${numero}`);
        item.dataset.index = String(indice);
        return item;
      }),
    );
  };

  campo.addEventListener("change", () => {
    mostrarErro("", { focar: false });
    const limite = maximo();
    const recusadas = [];
    for (const arquivo of campo.files) {
      if (escolhidas.length >= limite) {
        recusadas.push(`${arquivo.name}: o anúncio aceita até ${limite} fotos`);
      } else if (arquivo.size > maximoDeBytes) {
        recusadas.push(`${arquivo.name}: ${MENSAGEM_GRANDE}`);
      } else {
        escolhidas.push({ arquivo, url: URL.createObjectURL(arquivo) });
      }
    }

    campo.value = "";
    desenhar();
    if (recusadas.length > 0) mostrarErro(recusadas.join(". "));
    else anunciar(`${escolhidas.length} de ${limite} fotos escolhidas.`);
  });

  lista.addEventListener("click", (evento) => {
    const botao = evento.target.closest("[data-action]");
    const item = botao?.closest("[data-new-item]");
    if (!item) return;

    const indice = Number(item.dataset.index);
    const acao = botao.dataset.action;
    if (acao === "remove-new") {
      URL.revokeObjectURL(escolhidas[indice].url);
      escolhidas.splice(indice, 1);
    } else {
      const destino = acao === "move-up" ? indice - 1 : indice + 1;
      if (destino < 0 || destino >= escolhidas.length) return;
      [escolhidas[indice], escolhidas[destino]] = [escolhidas[destino], escolhidas[indice]];
    }

    mostrarErro("", { focar: false });
    desenhar();
    // O foco acompanha o botão que a pessoa usou; depois de remover, vai para a foto vizinha ou para o campo de arquivo
    const novoIndice = acao === "remove-new" ? Math.min(indice, escolhidas.length - 1) : acao === "move-up" ? indice - 1 : indice + 1;
    const vizinho = lista.children[novoIndice];
    (vizinho?.querySelector(`[data-action='${acao}']:not(:disabled)`) ?? vizinho?.querySelector("button:not(:disabled)") ?? campo).focus();
    anunciar(acao === "remove-new" ? "Foto removida." : `Foto movida para a posição ${novoIndice + 1}.`);
  });

  // Quando a categoria muda, o limite muda junto: a seção se ajusta (e some em categoria sem fotos)
  new MutationObserver(desenhar).observe(form.querySelector("[data-group-region]") ?? form, { childList: true, subtree: true });

  form.addEventListener("submit", (evento) => {
    // "Atualizar campos" não salva; sem fotos escolhidas o envio comum do formulário segue como sempre
    if (escolhidas.length === 0 || (evento.submitter && !evento.submitter.matches("[data-submit-button]"))) return;
    evento.preventDefault();
    // Impede o travamento de botões de ad-edit.js: aqui os botões são travados e liberados pelo próprio fluxo
    evento.stopImmediatePropagation();
    salvarEEnviar(evento.submitter);
  });

  const botoes = () => [...document.querySelectorAll("[data-submit-button]")];

  async function salvarEEnviar(botaoDoEnvio) {
    mostrarErro("", { focar: false });
    const campoTitulo = form.querySelector("#titulo");
    if (!campoTitulo?.value.trim()) {
      // O rascunho precisa de título; sem ele nada seria gravado e as fotos não teriam onde ficar
      mostrarErro("Informe o título para salvar o rascunho e enviar as fotos.", { focar: false });
      campoTitulo?.focus();
      return;
    }

    const rotulos = new Map(botoes().map((botao) => [botao, botao.textContent]));
    const liberar = () => rotulos.forEach((rotulo, botao) => ((botao.disabled = false), (botao.textContent = rotulo)));
    for (const botao of rotulos.keys()) botao.disabled = true;
    if (botaoDoEnvio) botaoDoEnvio.textContent = "Salvando…";

    let resposta;
    try {
      resposta = await fetch(form.action, { method: "POST", body: new FormData(form), credentials: "same-origin", headers: { Accept: "text/html" } });
    } catch {
      liberar();
      mostrarErro("Sem conexão com o servidor. Tente de novo.");
      return;
    }

    const caminho = new URL(resposta.url).pathname;
    if (resposta.redirected && caminho.startsWith("/painel/entrar")) {
      liberar();
      const alerta = document.querySelector("[data-session-expired]");
      if (alerta) {
        alerta.hidden = false;
        alerta.focus();
      }
      return;
    }

    const edicao = EDICAO.exec(caminho);
    if (!resposta.ok || !edicao) {
      // O servidor recusou o rascunho (nada foi gravado): o envio comum mostra os campos com erro. As fotos escolhidas precisam ser escolhidas de novo
      form.submit();
      return;
    }

    const url = new URL(resposta.url);
    await enviarFotos(`/api/v1/ads/${edicao[1]}/photos`, url.pathname + url.search);
  }

  async function enviarFotos(endereco, destino) {
    const falhas = [];
    for (let i = 0; i < escolhidas.length; i++) {
      const { arquivo } = escolhidas[i];
      anunciar(`Enviando foto ${i + 1} de ${escolhidas.length}…`);
      titulo.textContent = `Enviando foto ${i + 1} de ${escolhidas.length}…`;
      const dados = new FormData();
      dados.append("file", arquivo);
      try {
        await apiFetch(endereco, { method: "POST", body: dados });
      } catch (falha) {
        if (!(falha instanceof ApiError)) throw falha;
        falhas.push(`${arquivo.name}: ${Object.values(falha.errors ?? {}).flat()[0] ?? falha.message}`);
      }
    }

    if (falhas.length === 0) {
      location.assign(destino);
      return;
    }

    // O rascunho já está gravado: a pessoa segue para ele e envia o que faltou pela galeria
    erro.replaceChildren();
    erro.append(document.createTextNode(`O rascunho foi salvo, mas ${falhas.length === 1 ? "uma foto não foi enviada" : `${falhas.length} fotos não foram enviadas`}: ${falhas.join("; ")}. `));
    const link = document.createElement("a");
    link.href = destino;
    link.textContent = "Abrir o rascunho para enviar as fotos de novo";
    erro.append(link);
    erro.hidden = false;
    erro.focus();
  }

  desenhar();
}
