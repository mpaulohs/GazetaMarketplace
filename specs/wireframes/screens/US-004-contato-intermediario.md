# Wireframe: Contato com o intermediário (bloco da página de detalhe) — @US-004

> **Em resumo:** o bloco de contato aparece em toda página de anúncio publicado e mostra o telefone da Gazeta escrito na tela, com dois botões grandes: "Ligar" e "Chamar no WhatsApp". O visitante não precisa de conta, e o site não registra quem clicou. Este arquivo desenha só o bloco; a página que o contém é `US-003-detalhe-anuncio.md`.

**Evidência da SPEC:** `specs/SPEC.md` → US-004 (cenários S01 a S05).

## Layout — bloco de contato (desktop e mobile)

```text
┌──────────────────────────────────────────────────────────┐
│ Fale com a Gazeta (intermediária desta venda)            │
│ (11) 91234-5678                                          │
│ [ Ligar ]   [ Chamar no WhatsApp ]                       │
└──────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────┐
│ Fale com a Gazeta                    │
│ (11) 91234-5678                      │
│ [         Ligar         ]            │
│ [  Chamar no WhatsApp   ]            │
└──────────────────────────────────────┘
```

Fica logo abaixo do preço, à direita da galeria no desktop e em coluna única no celular. Os botões têm rótulo de texto (não só ícone) e altura mínima de 44 px.

## Comportamento dos botões (intenção)

| Botão | O que acontece | Observação |
|---|---|---|
| "Ligar" | O aplicativo de telefone abre com (11) 91234-5678 pronto para discar | Em computador sem aplicativo de telefone, o navegador decide o que fazer. |
| "Chamar no WhatsApp" | O WhatsApp abre uma conversa com o número; a mensagem já vem preenchida com um cumprimento, o título do anúncio e o endereço da página | Sem o aplicativo no computador, abre o WhatsApp Web em **nova aba**; a página do anúncio continua aberta. |
| Título com símbolos | A mensagem mostra o título exatamente como está, por exemplo Sítio "Boa Vista" & Cia | Acentos, aspas e "&" seguem íntegros. |

## Estados

| Estado | Cenário | O que o visitante vê |
|---|---|---|
| Padrão (com número configurado) | @US-004-S01, @US-004-S02, @US-004-S03 | Número escrito e os dois botões, sem login. |
| Título com acentos e símbolos | @US-004-S04 | Mensagem preenchida idêntica ao título. |
| Computador sem WhatsApp instalado | @US-004-S05 | WhatsApp Web em nova aba, com a conversa e a mensagem prontas. |
| Vazio (número não configurado) | — | Não ocorre no site público: sem número configurado o administrador não consegue publicar (@US-010-S08), e o número não pode ser apagado (@US-015-S05). |
| Carregando e erro | — | Não se aplicam: o bloco faz parte da página já montada pelo servidor; os botões são links comuns. |

## Responsivo

- Em 320 px os dois botões ocupam a largura toda, um sobre o outro, com espaço suficiente para o toque sem ampliar a tela (@US-003-S08).
- O bloco nunca é escondido por rolagem interna nem por menu recolhido.

## Acessibilidade

- "Ligar" é um `link` para `tel:` e "Chamar no WhatsApp" é um `link` que abre em nova aba; o nome acessível inclui o número e avisa a nova aba ("Chamar no WhatsApp, abre em nova aba").
- O número também está escrito como texto, para quem quiser copiá-lo ou discá-lo em outro aparelho.
- O contraste do rótulo dos botões é de pelo menos 4,5:1; o foco tem contorno visível.

## Mapeamento: controle → cenário

| Região da tela | Controle (papel / nome acessível) | Comportamento | Cenário |
|---|---|---|---|
| Bloco de contato | `link "Chamar no WhatsApp"` | Abre a conversa com o número da Gazeta e a mensagem pré-preenchida com título e endereço do anúncio | @US-004-S01 |
| Bloco de contato | `link "Ligar"` | Abre o aplicativo de telefone com o número pronto para discar | @US-004-S02 |
| Bloco de contato | Texto do número "(11) 91234-5678" + os dois botões, visíveis sem login | O visitante anônimo vê o número e os botões | @US-004-S03 |
| Bloco de contato | `link "Chamar no WhatsApp"` (anúncio com título especial) | Mensagem mantém acentos, aspas e "&" | @US-004-S04 |
| Bloco de contato | `link "Chamar no WhatsApp"` (computador sem aplicativo) | Abre o WhatsApp Web em nova aba; o anúncio continua na aba anterior | @US-004-S05 |
