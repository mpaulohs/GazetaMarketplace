// Fotos do anúncio (US-008-S02 a S06): melhoria progressiva. Sem JavaScript, cada ação é um formulário comum (uma foto por vez).
// Com JavaScript, o envio vai pela API, uma foto por pedido, e a lista muda na hora, sem recarregar a página nem apagar o que foi digitado.
import { ApiError, apiFetch } from "./api.js";

const TEXTO_FORMATO_HEIC = /\.(heic|heif)$/i;
const MENSAGEM_GRANDE = "A foto excede o limite de 10 MB";
const MENSAGEM_FALHA = "Falha ao enviar";

/** Liga a galeria da página do anúncio, se houver uma que aceite envio. */
export function ativarFotos() {
  const secao = document.querySelector("[data-photos][data-upload-url]");
  if (!secao) return;

  const lista = secao.querySelector("[data-photo-list]");
  const formularioEnvio = secao.querySelector("[data-photo-upload]");
  const campo = formularioEnvio?.querySelector("input[type='file']");
  const modeloFoto = secao.querySelector("template[data-photo-template]");
  const modeloPendente = secao.querySelector("template[data-photo-pending-template]");
  if (!lista || !campo || !modeloFoto || !modeloPendente) return;

  const dialogo = secao.querySelector("[data-photo-dialog]");
  const titulo = secao.querySelector("[data-photo-heading]");
  const aviso = secao.querySelector("[data-photo-status]");
  const mensagem = secao.querySelector("[data-photo-message]");
  const erro = secao.querySelector("[data-photo-error]");
  const vazio = secao.querySelector("[data-photo-empty]");
  const enderecoBase = secao.dataset.uploadUrl;
  const maximo = Number(secao.dataset.max);
  const maximoDeBytes = Number(secao.dataset.maxBytes);

  // Com JavaScript várias fotos de uma vez; sem ele o campo aceita uma (o servidor recebe um arquivo por pedido)
  campo.multiple = true;

  const fila = [];
  const arquivosDosQuadros = new WeakMap(); // para "Tentar de novo" reenviar o mesmo arquivo
  let enviando = false;

  const fotos = () => [...lista.querySelectorAll(":scope > li[data-photo-id]")];

  /** Mostra o aviso de um problema geral (capa, remoção); o texto é anunciado como alerta. */
  const mostrarErro = (texto) => {
    erro.textContent = texto;
    erro.hidden = !texto;
    if (texto) erro.focus();
  };

  const anunciar = (texto) => {
    mensagem.hidden = true;
    aviso.textContent = "";
    // Troca o texto num segundo momento para o leitor de tela anunciar também uma frase repetida
    requestAnimationFrame(() => (aviso.textContent = texto));
  };

  /** Posição, marca de "Capa", botões e textos alternativos de acordo com a ordem atual da lista. */
  const atualizarPosicoes = () => {
    const itens = fotos();
    itens.forEach((item, indice) => {
      const capa = indice === 0;
      const numero = indice + 1;
      const imagem = item.querySelector("img");
      if (imagem) imagem.alt = `Foto ${numero}`;
      item.querySelectorAll("[data-show-on-cover]").forEach((elemento) => (elemento.hidden = !capa));
      item.querySelectorAll("[data-hide-on-cover]").forEach((elemento) => (elemento.hidden = capa));
      item.querySelector("[data-action='set-cover']")?.setAttribute("aria-label", `Tornar capa da foto ${numero}`);
      item.querySelector("[data-action='remove-photo']")?.setAttribute("aria-label", `Remover foto ${numero}`);
    });

    if (titulo) titulo.textContent = `Fotos (${itens.length} de ${maximo})`;
    if (vazio) vazio.hidden = itens.length > 0 || lista.querySelector("[data-photo-pending]") !== null;
  };

  const criarFoto = (foto) => {
    const item = modeloFoto.content.firstElementChild.cloneNode(true);
    item.dataset.photoId = String(foto.id);
    const imagem = item.querySelector("img");
    imagem.src = foto.url480;
    imagem.loading = "lazy";
    return item;
  };

  // --- Envio: um arquivo por vez, na ordem escolhida. A foto entra na galeria na ordem em que o servidor a grava ---

  const criarPendente = (arquivo) => {
    const quadro = modeloPendente.content.firstElementChild.cloneNode(true);
    quadro.querySelector("[data-pending-name]").textContent = arquivo.name;
    lista.append(quadro);
    arquivosDosQuadros.set(quadro, arquivo);
    return { arquivo, quadro };
  };

  const estado = (pendente, texto) => {
    pendente.quadro.querySelector("[data-pending-state]").textContent = texto;
    const textoDoErro = pendente.quadro.querySelector("[data-pending-error]");
    textoDoErro.hidden = true;
    textoDoErro.textContent = "";
    pendente.quadro.querySelector("[data-action='retry']").hidden = true;
    pendente.quadro.querySelector("[data-action='discard']").hidden = true;
  };

  const recusar = (pendente, texto, { podeTentarDeNovo }) => {
    pendente.quadro.querySelector("[data-pending-state]").textContent = "";
    const textoDoErro = pendente.quadro.querySelector("[data-pending-error]");
    textoDoErro.textContent = texto;
    textoDoErro.hidden = false;
    pendente.quadro.querySelector("[data-action='retry']").hidden = !podeTentarDeNovo;
    pendente.quadro.querySelector("[data-action='discard']").hidden = false;
  };

  const mensagemDoServidor = (falha) => Object.values(falha.errors ?? {}).flat()[0] ?? falha.message;

  const falhou = (pendente, falha) => {
    if (!(falha instanceof ApiError)) throw falha;
    switch (falha.status) {
      case 400:
      case 409:
        recusar(pendente, mensagemDoServidor(falha), { podeTentarDeNovo: false });
        break;
      case 413:
        recusar(pendente, MENSAGEM_GRANDE, { podeTentarDeNovo: false });
        break;
      case 401:
        recusar(pendente, "Sua sessão expirou. Entre de novo para enviar fotos.", { podeTentarDeNovo: false });
        break;
      case 403:
      case 404:
        recusar(pendente, mensagemDoServidor(falha), { podeTentarDeNovo: false });
        break;
      case 429:
        recusar(pendente, "Muitos envios em pouco tempo. Aguarde um instante e tente de novo.", { podeTentarDeNovo: true });
        break;
      default:
        // Conexão caída ou erro do servidor: só esta foto falha; o resto da tela fica como está
        recusar(pendente, MENSAGEM_FALHA, { podeTentarDeNovo: true });
    }
  };

  const enviarUm = async (pendente) => {
    estado(pendente, TEXTO_FORMATO_HEIC.test(pendente.arquivo.name) ? "Convertendo foto HEIC…" : "Enviando…");
    const dados = new FormData();
    dados.append("file", pendente.arquivo);
    try {
      const foto = await apiFetch(enderecoBase, { method: "POST", body: dados });
      const item = criarFoto(foto);
      pendente.quadro.replaceWith(item);
      // Fotos antes dos quadros de envio: a galeria nunca fica com um quadro pendente no meio das fotos gravadas
      const primeiroPendente = lista.querySelector(":scope > li[data-photo-pending]");
      if (primeiroPendente && primeiroPendente.compareDocumentPosition(item) & Node.DOCUMENT_POSITION_FOLLOWING) {
        lista.insertBefore(item, primeiroPendente);
      }
      atualizarPosicoes();
      anunciar(`Foto ${fotos().indexOf(item) + 1} adicionada.`);
    } catch (falha) {
      falhou(pendente, falha);
    }
  };

  const processarFila = async () => {
    if (enviando) return;
    enviando = true;
    try {
      while (fila.length > 0) await enviarUm(fila.shift());
    } finally {
      enviando = false;
    }
  };

  const enfileirar = (arquivos) => {
    mostrarErro("");
    for (const arquivo of arquivos) {
      const pendente = criarPendente(arquivo);
      if (arquivo.size > maximoDeBytes) {
        // O servidor recusaria igual; aqui poupa subir 15 MB à toa
        recusar(pendente, MENSAGEM_GRANDE, { podeTentarDeNovo: false });
      } else {
        estado(pendente, "Na fila…");
        fila.push(pendente);
      }
    }

    atualizarPosicoes();
    processarFila();
  };

  formularioEnvio.addEventListener("submit", (evento) => {
    evento.preventDefault();
    const arquivos = [...campo.files];
    if (arquivos.length === 0) {
      mostrarErro("Escolha uma foto para enviar");
      return;
    }

    campo.value = "";
    enfileirar(arquivos);
  });

  // --- Capa, remoção e os botões dos quadros de envio ---

  let removendo = null;

  const mudarCapa = async (item, botao) => {
    mostrarErro("");
    botao.disabled = true;
    try {
      await apiFetch(`${enderecoBase}/${item.dataset.photoId}/cover`, { method: "POST" });
      lista.prepend(item);
      atualizarPosicoes();
      anunciar("A foto passou a ser a capa.");
      item.tabIndex = -1;
      item.focus();
    } catch (falha) {
      mostrarErro(falha instanceof ApiError ? mensagemDoServidor(falha) : MENSAGEM_FALHA);
    } finally {
      botao.disabled = false;
    }
  };

  const remover = async (item) => {
    mostrarErro("");
    const proximo = item.nextElementSibling ?? item.previousElementSibling;
    try {
      await apiFetch(`${enderecoBase}/${item.dataset.photoId}`, { method: "DELETE" });
      item.remove();
      atualizarPosicoes();
      anunciar("Foto removida.");
      const destino = proximo?.matches("li[data-photo-id]") ? proximo : null;
      if (destino) {
        destino.tabIndex = -1;
        destino.focus();
      } else {
        campo.focus();
      }
    } catch (falha) {
      mostrarErro(falha instanceof ApiError ? mensagemDoServidor(falha) : MENSAGEM_FALHA);
    }
  };

  secao.addEventListener("click", (evento) => {
    const botao = evento.target.closest?.("[data-action]");
    if (!botao || !secao.contains(botao)) return;

    const item = botao.closest("li");
    switch (botao.dataset.action) {
      case "set-cover":
        evento.preventDefault();
        mudarCapa(item, botao);
        break;
      case "remove-photo":
        // Sem <dialog> o botão segue o formulário e abre a página de confirmação
        if (!dialogo || typeof dialogo.showModal !== "function") return;
        evento.preventDefault();
        removendo = item;
        dialogo.returnValue = "";
        dialogo.showModal();
        break;
      case "retry": {
        const arquivo = arquivosDosQuadros.get(item);
        if (!arquivo) return;
        const pendente = { arquivo, quadro: item };
        estado(pendente, "Na fila…");
        fila.push(pendente);
        processarFila();
        break;
      }

      case "discard":
        item.remove();
        atualizarPosicoes();
        campo.focus();
        break;
      default:
    }
  });

  dialogo?.addEventListener("close", () => {
    const item = removendo;
    removendo = null;
    if (item && dialogo.returnValue === "confirm") remover(item);
    else item?.querySelector("[data-action='remove-photo']")?.focus();
  });

  atualizarPosicoes();
}
