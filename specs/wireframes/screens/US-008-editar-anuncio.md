# Wireframe: Criar e editar anúncio com fotos — @US-008

> **Em resumo:** o formulário em que o Redator (ou o Administrador) monta o anúncio: campos comuns, campos que mudam conforme a categoria e até 20 fotos (6 em Serviços; Vagas de emprego não tem fotos). Dá para salvar como rascunho só com o título e continuar depois. A ordem das fotos muda por botões ("Tornar capa"), sem arrastar. Anúncio em revisão aparece só para leitura; anúncio rejeitado mostra o motivo no topo. O botão "Enviar para revisão" é detalhado em `US-009-enviar-revisao.md`.

**Evidência da SPEC:** `specs/SPEC.md` → US-008 (cenários S01 a S14) e Apêndice B (campos por categoria).

## Layout — formulário (desktop, anúncio "Novo", categoria Carros)

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Gazeta · Painel                                    Ana Souza (Redator)  [ Sair ] │
│ [Meus anúncios]                                                                  │
├──────────────────────────────────────────────────────────────────────────────────┤
│ ◀ Meus anúncios                                                                  │
│ Novo anúncio                                                  Situação: Rascunho │
├──────────────────────────────────────────────────────────────────────────────────┤
│ * obrigatório para enviar à revisão; o rascunho pode ser salvo só com o título   │
│ Título *                                                                         │
│ [ Honda Civic 2018                                                           ]   │
│ Descrição *                                                                      │
│ [ Único dono, revisões feitas na concessionária…                             ]   │
│ Preço *                           Categoria *                                    │
│ [ R$ 62.000,00        ]           [ Carros, vans e utilitários          ▾]       │
│ CEP *                                                                            │
│ [ 13015-100 ]   Buscando…   (Cidade e UF são preenchidas a partir do CEP)        │
│ Cidade (automático)               UF (automático)                                │
│ [ Campinas              ] somente leitura   [ SP ] somente leitura               │
│ Ficha do veículo (aparece porque a categoria usa os campos de veículo)           │
│ Marca               Modelo              Ano       Quilometragem (km)             │
│ [ Honda        ]    [ Civic       ]     [ 2018 ]  [ 45000     ]                  │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Fotos (3 de 20)      Formatos JPG, PNG, WebP, GIF ou HEIC · até 10 MB por foto   │
│ [ Selecionar fotos ]  [ Enviar fotos ]                                           │
│ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐                      │
│ │ [ miniatura 1 ] │ │ [ miniatura 2 ] │ │ [ miniatura 3 ] │                      │
│ │ CAPA            │ │ [ Tornar capa ] │ │ [ Tornar capa ] │                      │
│ │ [ Remover ]     │ │ [ Remover ]     │ │ [ Remover ]     │                      │
│ └─────────────────┘ └─────────────────┘ └─────────────────┘                      │
├──────────────────────────────────────────────────────────────────────────────────┤
│ [ Salvar rascunho ]   [ Enviar para revisão ]                                    │
└──────────────────────────────────────────────────────────────────────────────────┘
```

O botão principal de salvar se chama "Salvar rascunho" enquanto o anúncio é um rascunho e "Salvar" quando o anúncio já foi rejeitado ou está publicado (edição feita pelo Administrador).

### Campos por categoria

A lista completa de campos de cada categoria está no Apêndice B do `specs/SPEC.md`. O quadro abaixo mostra o que o protótipo desenha.

| Categoria escolhida | O que muda no formulário |
|---|---|
| Automóveis: Carros, vans e utilitários; Motos; Caminhões | Ficha do veículo: Marca, Modelo, Ano, Quilometragem (km) |
| Terrenos, sítios e fazendas | Ficha do terreno: Área (m²) |
| Livros e revistas; Decorações Para Casa (grupo "Produtos em geral") | Ficha do produto: Condição * (Novo, Usado - Excelente, Usado - Bom, Recondicionado, Com defeito ou avarias) e Tipo de produto |
| **Serviços** | **Sem Preço.** Ficha do serviço: Tipo * (11 opções). A descrição se chama "Informações adicionais" (até 6000 caracteres, contador "X/6000"). Até **6** fotos |
| **Vagas de emprego** | **Sem Fotos.** Título com até **90** caracteres, texto de ajuda e contador "X/90". Área (14 caixas de seleção, opcional). "Informações adicionais" com texto de exemplo, texto de ajuda e contador "X/6000". Preço é o salário oferecido |
| Demais categorias do protótipo (Apartamentos, Casas, Celulares, Roupas, Ar-condicionados, Tratores) | Só os campos comuns. As fichas próprias dessas categorias (Apêndice B) não foram simuladas no protótipo |

Ao trocar a categoria, o formulário se refaz na hora: os campos específicos aparecem ou somem, o limite do título e da descrição e o limite de fotos mudam, e os campos comuns continuam preenchidos.

## Layout — mobile (320 px)

```text
┌──────────────────────────────────────┐
│ Gazeta · Painel             [ Menu ] │
├──────────────────────────────────────┤
│ ◀ Meus anúncios                      │
│ Novo anúncio                         │
│ Situação: Rascunho                   │
│ * obrigatório para enviar à revisão; │
│ o rascunho pode ser salvo só com o   │
│ título                               │
│ Título *                             │
│ [ Honda Civic 2018           ]       │
│ Descrição *                          │
│ [ Único dono, revisões…      ]       │
│ Preço *                              │
│ [ R$ 62.000,00               ]       │
│ Categoria *                          │
│ [ Carros, vans e utilitários ▾ ]     │
│ CEP *                                │
│ [ 13015-100 ]  Buscando…             │
│ Cidade (automático)  UF (automático) │
│ [ Campinas     ]      [ SP ]         │
│ Marca                                │
│ [ Honda                      ]       │
│ Modelo                               │
│ [ Civic                      ]       │
│ Ano       Quilometragem (km)         │
│ [ 2018 ]  [ 45000          ]         │
├──────────────────────────────────────┤
│ Fotos (3 de 20)                      │
│ [ Selecionar fotos ]                 │
│ [ Enviar fotos ]                     │
│ ┌───────────────┐ ┌───────────────┐  │
│ │ [ foto 1 ]    │ │ [ foto 2 ]    │  │
│ │ CAPA          │ │ [Tornar capa] │  │
│ │ [ Remover ]   │ │ [ Remover ]   │  │
│ └───────────────┘ └───────────────┘  │
├──────────────────────────────────────┤
│ [   Salvar rascunho     ]            │
│ [  Enviar para revisão  ]            │
└──────────────────────────────────────┘
```

## Layout — Serviços e Vagas de emprego (@US-008-S09)

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Serviços (desktop): sem Preço, Tipo obrigatório, até 6 fotos                     │
├──────────────────────────────────────────────────────────────────────────────────┤
│ * obrigatório para enviar à revisão; o rascunho pode ser salvo só com o título   │
│ Título *                                                                         │
│ [ Diarista com experiência em limpeza residencial                         ]      │
│ Informações adicionais *                                                         │
│ [ Faxina completa, passadoria e organização…                              ]      │
│                                                                     68/6000      │
│ Serviços não têm preço.           Categoria *                                    │
│                                   [ Serviços                            ▾]       │
│ CEP *  [ 01310-100 ]    Cidade (automático) [ São Paulo ]  UF [ SP ]             │
│ Ficha do serviço                                                                 │
│ Tipo *                                                                           │
│ [ Serviços domésticos                                                   ▾]       │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Fotos (3 de 6)   Formatos JPG, PNG, WebP, GIF ou HEIC · até 10 MB por foto       │
│ i foto-iphone.heic: convertida para JPG no envio                                 │
│ [ miniatura 1 · CAPA ]  [ miniatura 2 ]  [ miniatura 3 ]                         │
└──────────────────────────────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Vagas de emprego (desktop): sem Fotos, título de 90, Área, Preço = salário       │
├──────────────────────────────────────────────────────────────────────────────────┤
│ * obrigatório para enviar à revisão; o rascunho pode ser salvo só com o título   │
│ Título *                                                                         │
│ [ Pizzaiolo com experiência, período integral                             ]      │
│ Sugerimos especificar a vaga com clareza. Ex.: Pizzaiolo com experiência,        │
│ período integral                                                     43/90       │
│ Informações adicionais *                                                         │
│ [ Escreva aqui por que você está anunciando esta vaga e informações que  ]       │
│ [ podem ajudar no processo seletivo.  (texto de exemplo, some ao digitar) ]      │
│ Inclua detalhes sobre o cargo, responsabilidades, remuneração, localização,      │
│ ambiente de trabalho, etc                                           0/6000       │
│ Preço *                           Categoria *                                    │
│ [ R$ 2.800,00          ]          [ Vagas de emprego                    ▾]       │
│ É o salário oferecido; no site aparece como "Salário".                           │
│ CEP *  [ 13015-100 ]    Cidade (automático) [ Campinas ]   UF [ SP ]             │
│ Área (opcional; marque uma ou mais)                                              │
│ [ ] Administrativo / Secretariado / Finanças   [ ] Comercial / Vendas            │
│ [ ] Engenharia / Arquitetura / Design          [ ] Telecomunicações / …          │
│ [x] Turismo / Hotelaria / Restaurante          … (14 opções no total)            │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Fotos                                                                            │
│ Vagas de emprego não têm fotos.                                                  │
└──────────────────────────────────────────────────────────────────────────────────┘
```

As opções de "Tipo" e de "Área" seguem a ordem do Apêndice B do `specs/SPEC.md`. Os contadores mostram caracteres digitados / máximo, sem mínimo.

## Layout — CEP fora do ar (preenchimento manual, @US-008-S14)

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ CEP *                                                                            │
│ [ 13015-100 ]   Buscando… (tentativa 2 de 2)                                     │
│ ! Não foi possível buscar o CEP. Preencha Cidade e UF manualmente.               │
│ UF *                              Cidade *                                       │
│ [ Escolha a UF ▾]                 [ Escolha a UF primeiro             ▾]         │
└──────────────────────────────────────────────────────────────────────────────────┘
```

Depois de duas tentativas sem resposta, UF e Cidade deixam de ser automáticas e viram listas (UF entre as 27 unidades federativas; cidade na lista da UF escolhida). Trocar o CEP faz nova busca.

## Estados

| Estado | Cenário | O que a pessoa vê |
|---|---|---|
| Novo / edição, rascunho salvo | @US-008-S01, @US-008-S07 | Formulário preenchido; "Rascunho salvo"; o anúncio aparece em "Meus anúncios" como "Rascunho". Um rascunho pode ter só o título. |
| Fotos enviadas | @US-008-S02 | Miniaturas na ordem de envio, a primeira marcada "Capa"; continuam após salvar e reabrir. |
| Trocar capa e remover foto | @US-008-S03 | "Tornar capa" põe a foto em 1º lugar; "Remover" pede confirmação. |
| Limite de fotos | @US-008-S04 | "Cada anúncio pode ter no máximo 20 fotos"; o anúncio segue com 20. Em Serviços: "Cada anúncio de Serviços pode ter no máximo 6 fotos". |
| Arquivo não aceito | @US-008-S05 | Por arquivo: "Formato não aceito. Use JPG, PNG, WebP, GIF ou HEIC" e "A foto excede o limite de 10 MB"; nenhum dos dois é adicionado. |
| Foto HEIC | @US-008-S02 | A foto entra normalmente e aparece o aviso "foto-iphone.heic: convertida para JPG no envio", porque HEIC não aparece em todos os navegadores. |
| Falha no envio de uma foto | @US-008-S06 | Sobre a foto: "Falha ao enviar" e "Tentar de novo"; as outras fotos e os textos continuam na tela. |
| Sem título | @US-008-S08 | Junto do campo: "Informe um título"; o rascunho não é criado. |
| Campos por categoria | @US-008-S09 | Carros, vans e utilitários mostra marca, modelo, ano e quilometragem; Terrenos, sítios e fazendas mostra "Área (m²)"; Livros e revistas mostra "Condição" e "Tipo de produto"; Vagas de emprego esconde as Fotos, mostra "Área" e limita o título a 90 caracteres; Serviços esconde o Preço e mostra "Tipo". |
| Anúncio de outro Redator | @US-008-S10 | "Você não tem permissão para acessar este anúncio", sem o conteúdo (mesmo desenho do acesso negado em `US-006-login-equipe.md`). |
| Rejeitado com motivo | @US-008-S11 | Motivo em destaque no topo; campos e fotos editáveis; a situação continua "Rejeitado" até novo envio. |
| Em revisão (somente leitura) | @US-008-S12 | Dados sem campos editáveis e a mensagem "Este anúncio está em revisão e não pode ser editado". |
| Administrador edita anúncio publicado | @US-008-S13 | Botão "Salvar"; a situação continua "Publicado" e o site mostra o valor novo na hora. |
| CEP fora do ar | @US-008-S14 | "Buscando…", depois "Buscando… (tentativa 2 de 2)" e o aviso "Não foi possível buscar o CEP. Preencha Cidade e UF manualmente."; UF e Cidade viram listas; ao enviar sem cidade, pendência "Informe a cidade". |
| Enviando foto | — | Miniatura com barra de progresso e texto "Enviando… 60%"; sem cenário na SPEC (ver Lacunas no `README.md`). |
| Vazio e sem resultado | — | Não se aplicam; um formulário novo é o "vazio". |

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Rejeitado com motivo                                                             │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Editar anúncio                                               Situação: Rejeitado │
│ ! Motivo da rejeição: Fotos escuras                                              │
│   Corrija o que for preciso, salve e envie de novo para revisão.                 │
│ * obrigatório para enviar à revisão; o rascunho pode ser salvo só com o título   │
│ Título *                                                                         │
│ [ Moto para retirar peças                                                    ]   │
│ … (demais campos editáveis)                                                      │
│ [ Salvar ]   [ Enviar para revisão ]                                             │
└──────────────────────────────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Em revisão (somente leitura)                                                     │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Anúncio · Honda Civic 2018                                  Situação: Em revisão │
│ i Este anúncio está em revisão e não pode ser editado.                           │
│ Título: Honda Civic 2018      Preço: R$ 62.000                                   │
│ Categoria: Carros, vans e utilitários                                            │
│ CEP: 13015-100   Cidade/UF: Campinas/SP   Fotos: 3                               │
│ (nenhum botão de salvar ou enviar)                                               │
└──────────────────────────────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Foto com problema e limite de fotos                                              │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Fotos (20 de 20)                                                                 │
│ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐                      │
│ │ [ miniatura 4 ] │ │ [ miniatura 5 ] │ │ [ miniatura 6 ] │                      │
│ │ Enviando… 60%   │ │ Falha ao enviar │ │ [ Tornar capa ] │                      │
│ │                 │ │ [Tentar de novo]│ │ [ Remover ]     │                      │
│ └─────────────────┘ └─────────────────┘ └─────────────────┘                      │
│ ! Cada anúncio pode ter no máximo 20 fotos                                       │
│ ! contrato.pdf: Formato não aceito. Use JPG, PNG, WebP, GIF ou HEIC              │
│ ! foto-grande.jpg: A foto excede o limite de 10 MB                               │
└──────────────────────────────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────┐
│ Confirmar remoção de foto                                │
├──────────────────────────────────────────────────────────┤
│ Remover esta foto?                                       │
│ Ela deixa de fazer parte do anúncio.                     │
│ [ Cancelar ]   [ Remover ]                               │
└──────────────────────────────────────────────────────────┘
```

## Responsivo

- Em 320 px os campos ficam em coluna única (Cidade e UF automáticas, ou ano e quilometragem, podem dividir a linha); as miniaturas em 2 colunas; os botões de salvar e enviar ocupam a largura toda; sem rolagem horizontal.
- Reordenar fotos funciona só por botões ("Tornar capa"), sem arrastar, para funcionar no celular e por teclado.
- Alvos de toque de pelo menos 44 × 44 px em "Tornar capa", "Remover" e "Tentar de novo".

## Acessibilidade

- Cada campo tem `label` visível; obrigatórios com `*` e `aria-required`; erros por campo em `role="alert"`, ligados por `aria-describedby`.
- Cada miniatura tem texto alternativo "Foto 2 de 3" e, quando é a capa, "Capa"; os botões repetem o número: "Tornar foto 2 capa", "Remover foto 2".
- Os avisos de envio (limite, formato, tamanho, falha) ficam em região `aria-live="assertive"`.
- O diálogo de remoção é `role="dialog"` com `aria-modal="true"`, prende o foco e devolve o foco ao botão de origem.
- O motivo da rejeição fica em `role="alert"` no topo, antes do primeiro campo.
- Ao trocar a categoria, o formulário se refaz, o foco continua na lista "Categoria" e a mudança é anunciada ("Campos atualizados para a categoria …").
- As caixas de seleção de "Área" ficam num `fieldset` com `legend "Área"`; cada opção tem área de toque de pelo menos 24 px de altura.
- Os contadores ("X/90", "X/6000") são texto visível; o limite também vale como `maxlength` no campo.

## Mapeamento: controle → cenário

| Região da tela | Controle (papel / nome acessível) | Comportamento | Cenário |
|---|---|---|---|
| Topo do formulário | texto "* obrigatório para enviar à revisão; o rascunho pode ser salvo só com o título" | Explica o asterisco: obrigatório só para enviar; rascunho só com título | @US-008-S07, @US-009-S02 |
| Campos comuns | `textbox "Título"` | Único campo obrigatório para salvar rascunho | @US-008-S01, @US-008-S07 |
| Campos comuns | `textbox "Título"` (vazio) + mensagem "Informe um título" | Bloqueia a criação do rascunho | @US-008-S08 |
| Campos comuns | `textbox "Descrição *"` | Preenche o rascunho | @US-008-S01 |
| Campos comuns (Serviços, Vagas) | `textbox "Informações adicionais *"` + contador "X/6000" | Mesmo dado da Descrição, com outro rótulo; até 6000 caracteres | @US-008-S09 |
| Campos comuns (Vagas) | `textbox "Título *"` + ajuda "Sugerimos especificar a vaga com clareza…" + contador "X/90" | Limita o título a 90 caracteres, sem mínimo | @US-008-S09 |
| Campos comuns | `textbox "Preço *"` (máscara R$; os dígitos entram pelos centavos) | Mostra "R$ 62.000,00"; só aceita dígitos, sem negativo, até R$ 99.999.999,99; vazio ou R$ 0,00 bloqueia o envio com "Informe um preço maior que zero" (suposições S27 e S28) | @US-008-S01, @US-008-S13, @US-009-S02 |
| Campos comuns | `textbox "CEP *"` (máscara 00000-000) + `status "Buscando…"` | Com 8 dígitos, busca e preenche Cidade e UF; incompleto: "Informe um CEP com 8 dígitos"; não encontrado: "CEP não encontrado. Confira os números." (suposições S23 e S24) | @US-008-S01, @US-009-S02 |
| Campos comuns (CEP fora do ar) | `alert "Não foi possível buscar o CEP. Preencha Cidade e UF manualmente."` | Aparece depois da 2ª tentativa sem resposta | @US-008-S14 |
| Campos comuns (CEP fora do ar) | `combobox "UF *"` (27 UFs), `combobox "Cidade *"` (lista da UF) | Substituem Cidade/UF automáticas; sem cidade, o envio mostra "Informe a cidade" | @US-008-S14 |
| Campos comuns | `textbox "Cidade (automático)"`, `textbox "UF (automático)"` (somente leitura) | Mostram o resultado do CEP; não são editáveis | @US-008-S01 |
| Campos comuns | `combobox "Categoria"` | Escolhe a categoria e liga ou desliga os campos específicos | @US-008-S01, @US-008-S09 |
| Ficha da categoria | `textbox "Marca"`, `textbox "Modelo"`, `spinbutton "Ano"`, `spinbutton "Quilometragem (km)"` | Aparecem nas categorias com campos de veículo (Carros, vans e utilitários; Motos) | @US-008-S01, @US-008-S09 |
| Ficha da categoria | `spinbutton "Área (m²)"` | Aparece em Terrenos, sítios e fazendas | @US-008-S09 |
| Ficha da categoria | `combobox "Condição *"`, `textbox "Tipo de produto"` | Aparecem em Livros e revistas e em Decorações Para Casa; sem condição, o envio mostra "Escolha a condição" | @US-008-S09 |
| Ficha da categoria (Serviços) | `combobox "Tipo *"` (11 opções) + texto "Serviços não têm preço." no lugar do Preço | Sem tipo, o envio mostra "Escolha o tipo de serviço"; o preço não é pedido | @US-008-S09 |
| Ficha da categoria (Vagas) | `group "Área"` com 14 `checkbox` | Opcional; aceita várias áreas | @US-008-S09 |
| Fotos (Vagas) | Texto "Vagas de emprego não têm fotos." no lugar da seção de fotos | O envio não pede foto | @US-008-S09 |
| Rodapé do formulário | `button "Salvar rascunho"` | Salva; mostra `status "Rascunho salvo"`; reabrir mostra os mesmos valores | @US-008-S01, @US-008-S07, @US-008-S08 |
| Rodapé do formulário | `button "Salvar"` (anúncio rejeitado) | Salva sem mudar a situação; continua "Rejeitado" | @US-008-S11 |
| Rodapé do formulário | `button "Salvar"` (Administrador, anúncio publicado) | Salva; o anúncio continua "Publicado" e o site mostra o novo preço | @US-008-S13 |
| Rodapé do formulário | `button "Enviar para revisão"` | Detalhado na US-009 | @US-009-S01 |
| Fotos | `button "Selecionar fotos"` (entrada de arquivos) + `button "Enviar fotos"` | Envia as fotos escolhidas; miniaturas na ordem de envio, primeira marcada "Capa" | @US-008-S02 |
| Fotos | `status "Fotos (3 de 20)"` + aviso "Cada anúncio pode ter no máximo 20 fotos" | Impede a 21ª foto (em Serviços, "de 6" e a 7ª) | @US-008-S04 |
| Fotos | Mensagens por arquivo: "Formato não aceito. Use JPG, PNG, WebP, GIF ou HEIC" e "A foto excede o limite de 10 MB" | Nenhum dos dois arquivos é adicionado | @US-008-S05 |
| Fotos | Aviso "<arquivo>.heic: convertida para JPG no envio" | A foto HEIC entra e é convertida | @US-008-S02 |
| Fotos | Sobre a miniatura: `alert "Falha ao enviar"` + `button "Tentar de novo"` | Reenvia só essa foto; o resto da tela não muda | @US-008-S06 |
| Fotos | `button "Tornar capa"` (por foto) | A foto vai para a 1ª posição e recebe a marca "Capa" | @US-008-S03 |
| Fotos | `button "Remover"` (por foto) + diálogo `button "Remover"` / `button "Cancelar"` | Remove a foto depois de confirmar | @US-008-S03 |
| Topo | Etiqueta de situação "Rascunho" | Mostra a situação atual do anúncio | @US-008-S01 |
| Topo | `alert "Motivo da rejeição: Fotos escuras"` | Mostra o motivo antes dos campos; situação continua "Rejeitado" | @US-008-S11 |
| Página inteira | Modo somente leitura (sem controles) + `status "Este anúncio está em revisão e não pode ser editado"` | Dados só para leitura para o Redator | @US-008-S12 |
| Página inteira | `alert "Você não tem permissão para acessar este anúncio"` | Não mostra o conteúdo do rascunho de outro Redator | @US-008-S10 |
| Topo | `link "Meus anúncios"` (voltar) | Retorna à lista do painel | ⚠️ nenhum cenário cobre este controle |
