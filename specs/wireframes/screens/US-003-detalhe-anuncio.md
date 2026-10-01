# Wireframe: Detalhe do anúncio — @US-003

> **Em resumo:** a página do anúncio mostra a galeria (até 20 fotos), o título, o preço, o local, a descrição e as características do bem, além do bloco de contato com a Gazeta e do botão de favoritar. Só anúncios publicados aparecem; qualquer outro caso mostra a mesma mensagem de indisponibilidade. O bloco de contato está detalhado em `US-004-contato-intermediario.md`.

**Evidência da SPEC:** `specs/SPEC.md` → US-003 (cenários S01 a S08); regra "Salário" de Vagas de emprego e suposições A4 e A6.

## Layout — desktop (anúncio de Carros, vans e utilitários com 20 fotos)

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ GazetaMarketplace                                                ♡ Favoritos (2) │
│ [ Buscar anúncios…                                        ]  [Buscar]            │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Início > Automóveis, Peças e Acessórios > Carros, vans e utilitários >           │
│ Honda Civic 2018                                                                 │
│ ┌──────────────────────────────────────────┐ Honda Civic 2018                    │
│ │                   [ ◀ ]                  │ R$ 62.000                           │
│ │        [ foto em destaque ]              │ Carros, vans e utilitários ·        │
│ │                   [ ▶ ]                  │ Campinas/SP                         │
│ │                   5 de 20                │ Publicado em 12/09/2026             │
│ │ [m1][m2][m3][m4][*m5*][m6][m7] …         │ Fale com a Gazeta (intermediária)   │
│ │ (clique na foto para ampliar)            │ (11) 91234-5678                     │
│ └──────────────────────────────────────────┘ [ Ligar ]                           │
│                                              [ Chamar no WhatsApp ]              │
│                                              [ ♡ Favoritar ]                     │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Descrição                                                                        │
│ Único dono, revisões feitas na concessionária, pneus novos.                      │
│ Aceita troca por veículo de menor valor.                                         │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Características do veículo                                                       │
│ Marca: Honda   Modelo: Civic   Ano: 2018   Quilometragem: 45.000 km              │
├──────────────────────────────────────────────────────────────────────────────────┤
│ © GazetaMarketplace                                               Área da equipe │
└──────────────────────────────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Variantes do topo da página: Serviços e Vagas de emprego                         │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Serviços (sem preço, suposição A6)                                               │
│ ┌──────────────────────────┐ Diarista com experiência em limpeza residencial     │
│ │   [ foto em destaque ]   │ Serviços domésticos          (Tipo, em destaque)    │
│ │        1 de 3            │                                                     │
│ └──────────────────────────┘ Serviços · São Paulo/SP                             │
│ Seção de texto: "Informações adicionais"                                         │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Vagas de emprego (sem fotos, suposição A4)                                       │
│ ┌──────────────────────────┐ Pizzaiolo com experiência, período integral         │
│ │ Vaga de emprego          │ Salário R$ 2.800                                    │
│ │ Área: Turismo / Hotelaria│ Vagas de emprego · Campinas/SP                      │
│ │                          │                                                     │
│ └──────────────────────────┘ (sem galeria, sem miniaturas, sem ampliar)          │
│ Seção de texto: "Informações adicionais"                                         │
└──────────────────────────────────────────────────────────────────────────────────┘
```

Em Vagas de emprego o valor aparece com o rótulo **"Salário"** (regra da US-003), no formato da S29. Serviços não tem preço: o Tipo ocupa o lugar do preço. As duas variantes seguem o `architecture/design-system.md` (§5.3 e §5.4), que resolveu as suposições A4 e A6.

O bloco "Características do veículo" existe só nas categorias de veículo de "Automóveis, Peças e Acessórios"; em Terrenos, sítios e fazendas aparece "Área: 450 m²"; nas demais categorias aparecem as características do grupo de campos da categoria (Apêndice B do SPEC). Em "Decorações Para Casa", por exemplo, o bloco "Características" mostra "Condição" e "Tipo de produto" (@US-003-S04).

## Layout — mobile (320 px)

```text
┌──────────────────────────────────────┐
│ GazetaMarketplace              ♡ (2) │
│ [ Buscar anúncios…  ] [Buscar]       │
├──────────────────────────────────────┤
│ Início > … > Honda Civic 2018        │
│ [ ◀ ]  [ foto em destaque ]  [ ▶ ]   │
│               5 de 20                │
│ [m1][m2][m3][*m5*][m6]  ← desliza    │
├──────────────────────────────────────┤
│ Honda Civic 2018                     │
│ R$ 62.000                            │
│ Carros, vans e utilitários ·         │
│ Campinas/SP                          │
│ Publicado em 12/09/2026              │
├──────────────────────────────────────┤
│ Fale com a Gazeta                    │
│ (11) 91234-5678                      │
│ [      Ligar      ]                  │
│ [ Chamar no WhatsApp ]               │
│ [    ♡ Favoritar    ]                │
├──────────────────────────────────────┤
│ Descrição                            │
│ Único dono, revisões feitas…         │
│ Características do veículo           │
│ Marca: Honda · Modelo: Civic         │
│ Ano: 2018 · Km: 45.000               │
└──────────────────────────────────────┘
```

## Foto ampliada (janela sobre a página)

```text
┌──────────────────────────────────────────────────────────┐
│ Foto 5 de 20                                  [ Fechar ] │
├──────────────────────────────────────────────────────────┤
│            [ foto ampliada: Honda Civic 2018 ]           │
│               (Esc também fecha e devolve o              │
│                foco à miniatura de origem)               │
└──────────────────────────────────────────────────────────┘
```

## Estados

| Estado | Cenário | O que o visitante vê |
|---|---|---|
| Padrão | @US-003-S01 | Foto de capa em destaque, miniaturas, título, preço, categoria, cidade/UF, data, descrição, características, contato e favoritar. |
| Galeria com 20 fotos | @US-003-S02 | A 5ª miniatura vira a foto em destaque com "5 de 20"; a seta para a direita do teclado passa para "6 de 20". |
| Foto ampliada | @US-003-S03 | Janela com a foto e o botão "Fechar"; Esc fecha e volta ao mesmo ponto da página. |
| Sem ficha de veículo nem de terreno | @US-003-S04 | Categoria "Decorações Para Casa": título, preço, cidade/UF, descrição e o bloco "Características" com "Condição" e "Tipo de produto"; nenhum bloco de veículo ou de terreno. |
| Uma única foto | @US-003-S05 | Só a foto em destaque; sem miniaturas nem setas. |
| Vaga de emprego | @US-003-S01 | Bloco "Vaga de emprego" com a Área no lugar da galeria (A4); valor com o rótulo "Salário"; texto em "Informações adicionais". |
| Serviço | @US-003-S01 | Tipo do serviço em destaque no lugar do preço (A6); texto em "Informações adicionais". |
| Anúncio indisponível | @US-003-S06 | "Este anúncio não está mais disponível" e links para a página inicial e para a categoria dele; nada do conteúdo do anúncio aparece. |
| Foto quebrada | @US-003-S07 | No lugar da 2ª foto, "Foto indisponível"; as demais continuam navegáveis. |
| Erro | — | Página de erro com código de referência, como na página inicial (a SPEC não traz cenário para o detalhe; ver Lacunas no `README.md`). |
| Carregando | — | Esqueleto da foto e das linhas de texto, com altura reservada. |
| Vazio / sem resultado | — | Não se aplica: a página mostra um único anúncio. |

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Anúncio indisponível                                                             │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Este anúncio não está mais disponível                                            │
│ Voltar para a  [Página inicial]  ou ver mais anúncios de                         │
│ [Carros, vans e utilitários]                                                     │
└──────────────────────────────────────────────────────────────────────────────────┘
```

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Foto que não carrega (2ª miniatura escolhida)                                    │
├──────────────────────────────────────────────────────────────────────────────────┤
│         ┌───────────────────────────────┐                                        │
│         │        Foto indisponível      │     [◀] [▶] continuam ativos           │
│         └───────────────────────────────┘                                        │
│ [m1] [ ! ] [m3]                                                                  │
└──────────────────────────────────────────────────────────────────────────────────┘
```

## Responsivo

- **320 px:** coluna única na ordem galeria, título e preço, contato, descrição, características; troca de foto por toque nas miniaturas ou deslizando; os botões de contato ocupam a largura toda (altura mínima 44 px) e aparecem sem ampliar a tela; sem rolagem horizontal (@US-003-S08). A faixa de miniaturas rola na horizontal dentro dela mesma, sem mover a página.
- **768 px ou mais:** galeria à esquerda, informações e contato à direita.

## Acessibilidade

- A foto em destaque é um `button` de nome "Ampliar foto 5 de 20: Honda Civic 2018"; cada foto tem texto alternativo com o título e o número dela.
- As miniaturas formam uma lista de botões "Foto 5 de 20"; a atual usa `aria-current="true"`. As setas do teclado (esquerda e direita) trocam a foto; o indicador "5 de 20" é uma região `role="status"`.
- A janela da foto ampliada é `role="dialog"` com `aria-modal="true"`, prende o foco, fecha com Esc e devolve o foco ao elemento de origem.
- O texto do anúncio é exibido como texto puro.
- "Foto indisponível" é texto real, não só ícone.

## Mapeamento: controle → cenário

| Região da tela | Controle (papel / nome acessível) | Comportamento | Cenário |
|---|---|---|---|
| Galeria | `button "Ampliar foto 1 de 6: Honda Civic 2018"` (foto em destaque) | Mostra a capa em destaque; ao clicar, abre a foto ampliada | @US-003-S01, @US-003-S03 |
| Galeria | Lista de `button "Foto 5 de 20"` (miniaturas) | Clicar troca a foto em destaque | @US-003-S01, @US-003-S02 |
| Galeria | `status "5 de 20"` (indicador) | Acompanha a foto atual | @US-003-S02 |
| Galeria | `button "Foto anterior"` e `button "Próxima foto"` (e setas do teclado) | Trocam a foto; ausentes quando há uma só foto | @US-003-S02, @US-003-S05 |
| Foto ampliada | `dialog "Foto ampliada"` + `button "Fechar"` + tecla Esc | Fecha e volta à página no mesmo ponto | @US-003-S03 |
| Cabeçalho do anúncio | `heading 1` título, preço, categoria, cidade/UF, data de publicação | Informações principais | @US-003-S01 |
| Descrição | `region "Descrição"` (texto puro) | Descrição completa | @US-003-S01 |
| Características | `region "Características do veículo"` (marca, modelo, ano, quilometragem) | Só aparece em Automóveis, Peças e Acessórios | @US-003-S01 |
| Características | `region "Características"` com "Condição" e "Tipo de produto" (ex.: "Decorações Para Casa") | Mostra as características do grupo "Produtos em geral"; não mostra bloco de veículo nem de terreno | @US-003-S04 |
| Contato | `link "Ligar"` e `link "Chamar no WhatsApp"` | Detalhados na US-004; visíveis sem ampliar a tela em 320 px | @US-003-S01, @US-003-S08 |
| Favoritar | `button "Favoritar"` / `"Favoritado"` (`aria-pressed`) | Marca ou desmarca favorito (ref. cruzada, US-005) | @US-003-S01, @US-005-S02 |
| Galeria | Ausência de miniaturas e setas com uma foto | Só a foto em destaque | @US-003-S05 |
| Página de indisponibilidade | Mensagem "Este anúncio não está mais disponível" + `link "Página inicial"` + `link` da categoria | Sem título, fotos nem descrição | @US-003-S06 |
| Galeria | Marcador "Foto indisponível" no lugar da foto | As demais fotos seguem navegáveis | @US-003-S07 |
| Topo (Vagas) | Texto "Salário R$ 2.800" no lugar do preço | Mostra o salário oferecido com o rótulo "Salário" | @US-003-S01 |
| Galeria (Vagas) | Bloco "Vaga de emprego" + Área | Substitui a galeria, que não existe em Vagas | @US-003-S01 |
| Topo (Serviços) | Tipo do serviço no lugar do preço | Substitui o preço, que não existe em Serviços | @US-003-S01 |
| Página inteira (mobile) | Comportamento em 320 px (sem controle próprio) | Sem rolagem horizontal; troca de foto por toque | @US-003-S08 |
| Caminho | `nav "Você está em"` → links das categorias | Volta à categoria ou à página inicial | ⚠️ nenhum cenário cobre este controle |
