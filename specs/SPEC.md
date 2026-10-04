# SPEC — GazetaMarketplace

> **Em resumo:** O GazetaMarketplace é um site de classificados nacional (carros, motos, terrenos, eletrônicos e outras categorias) em que a equipe da Gazeta atua como **intermediária**: ela combina a venda com quem quer vender, cadastra o anúncio no painel e, depois que um administrador aprova, o anúncio aparece para todos. Quem visita o site não precisa de conta: navega, busca, favorita e entra em contato **direto com o telefone/WhatsApp da Gazeta**, que negocia com o vendedor. A receita é uma comissão paga pelo vendedor **fora do sistema**; o sistema não faz pagamento, carrinho, mensagens internas nem controla vendas, comissões ou interessados.
>
> **Para quem é este documento:** para o Product Owner e demais partes interessadas aprovarem **o que** será construído e **por quê**. Como será construído fica para a etapa de arquitetura. Os termos em itálico ou com sigla estão explicados no Glossário (fim do documento).

| Field | Value |
|-------|-------|
| Version | v1.3 |
| Coverage | full |
| Mode | greenfield |
| Status | **Approved** |
| Date | 2026-09-30 |

## Revision History

> Somente acréscimo: nenhuma linha é editada ou apagada. Uma linha representa um conjunto de mudanças aprovado.

| Version | Date | Type | Flow | Change description | Affected stories/scenarios | Reference | Approved by |
|---------|------|------|------|--------------------|----------------------------|-----------|-------------|
| v1.0 | 2026-09-30 | Baseline | greenfield | Especificação inicial do MVP do GazetaMarketplace. Aprovado após revisão do protótipo clicável. Pendências A2 a A6 abertas para o /arch | US-001 a US-015 | — | Product Owner, 2026-09-30 |
| v1.1 | 2026-09-30 | Changed | greenfield | Alinhamento dos dados de exemplo dos cenários com `specs/categories.md`; decisão do US-013-S11 (3 níveis) | US-001, US-002, US-003, US-008, US-009, US-013 | `specs/categories.md` | Product Owner, 2026-09-30 |
| v1.1 | 2026-09-30 | Changed | greenfield | Errata: removida a menção "lista do painel" da regra do Salário, por não ter cenário correspondente na US-012 | US-003 (regra do Salário) | — | Product Owner, 2026-09-30 |
| v1.2 | 2026-09-30 | Added | greenfield | O Administrador pode redefinir a senha de alguém da equipe com uma senha provisória, como alternativa quando o e-mail de recuperação não chega (AR-11 de `/arch`) | US-014-S10 | `architecture/adr/ADR-009-email-sendgrid.md` | Product Owner, 2026-09-30 |
| v1.3 | 2026-10-04 | Changed | greenfield | Em "Terrenos, sítios e fazendas" a Área (m²) passa a ser obrigatória para enviar o anúncio à revisão; nas demais categorias de Imóveis continua opcional (Apêndice B) | US-009 (pendências), US-008-S09 | Decisão do Product Owner, tarefa 3.8 | Product Owner, 2026-10-04 |

## Executive Summary

O GazetaMarketplace publica anúncios de bens (veículos, imóveis, eletrônicos, entre outros) captados pela equipe da Gazeta junto a vendedores. Todo anúncio passa por **pré-moderação**: um redator cadastra, um administrador aprova, e só então o público vê. O visitante encontra o anúncio por categoria ou busca com filtros e contata o intermediário pelo telefone/WhatsApp exibido no anúncio. Só a equipe tem cadastro e login; o público não tem conta.

## Objective

Disponibilizar, na versão 1.0, um site público nacional em que qualquer pessoa chegue ao telefone/WhatsApp do intermediário em **no máximo 3 cliques a partir da página inicial** (categoria, anúncio, botão de contato), vendo apenas anúncios que um administrador aprovou, com um painel interno em que redatores cadastram e administradores aprovam e gerenciam o catálogo.

**Volume-alvo da v1:** até cerca de 200 anúncios ativos e 1.000 visitas por dia (escala pequena, definida pelo Product Owner).

## Goals & Success Metrics

> Objetivo de negócio (Business) é o que a organização ganha; objetivo de produto (Product) é o que o site precisa entregar para isso. São medidas de acompanhamento após o lançamento, não critérios de aceite. Como o sistema **não** registra contatos, interessados nem vendas (decisão do Product Owner), os valores abaixo dependem de definição do Product Owner e de como a medição será feita fora do sistema. Nenhum número foi inventado.

| ID | Type | Goal | Metric / KPI | Baseline | Timeframe |
|----|------|------|--------------|----------|-----------|
| G-01 | Business | Gerar receita por comissão sobre vendas intermediadas (cobrada do vendedor, fora do sistema) | [NEEDS PO] | [NEEDS PO] | [NEEDS PO] |
| G-02 | Product | Ser a vitrine que leva compradores qualificados a contatar o intermediário | [NEEDS PO] | [NEEDS PO] | [NEEDS PO] |

## Target Users

| Persona | Description | Primary Goal |
|---------|-------------|--------------|
| Visitante | Pessoa qualquer, sem conta, que acessa o site pelo celular ou computador; conhecimento técnico de básico a intermediário. Pode ser comprador em potencial em qualquer lugar do Brasil. | Encontrar um bem que lhe interesse e falar rapidamente com o intermediário |
| Redator | Membro da equipe da Gazeta com conta e login. Recebe as informações do vendedor (fora do sistema) e cadastra o anúncio. Usa o painel no dia a dia, pelo computador ou celular. | Cadastrar anúncios completos, com boas fotos, e vê-los aprovados sem retrabalho |
| Administrador | Membro da equipe da Gazeta com conta e login, responsável por aprovar o que vai ao ar e por manter o catálogo, a equipe e o contato do site. | Garantir que só anúncios corretos sejam publicados e manter o site organizado |

## Permission Matrix (role × action)

> Regras de negócio de quem pode fazer o quê. O Visitante não tem conta nem login. Como as regras são aplicadas tecnicamente será definido nas etapas seguintes (segurança e arquitetura).

| Action / Feature area | Visitante | Redator | Administrador |
|-----------------------|-----------|---------|---------------|
| Ver página inicial, categorias, busca e detalhe de anúncios publicados | ✅ | ✅ | ✅ |
| Ver o telefone/WhatsApp do intermediário e usar os botões de contato | ✅ | ✅ | ✅ |
| Favoritar anúncios (salvos no navegador) | ✅ | ✅ | ✅ |
| Entrar e sair do painel | — | ✅ | ✅ |
| Trocar a própria senha e recuperar senha esquecida | — | ✅ | ✅ |
| Criar anúncio (rascunho) | — | ✅ | ✅ |
| Editar anúncio | — | 🔒¹ | ✅² |
| Enviar anúncio para revisão | — | 🔒¹ | ✅ |
| Ver a lista de anúncios no painel | — | 🔒³ | ✅ |
| Pré-visualizar anúncio ainda não publicado | — | 🔒³ | ✅ |
| Publicar ou rejeitar anúncio em revisão | — | — | ✅ |
| Despublicar ou arquivar anúncio | — | — | ✅ |
| Gerenciar categorias | — | — | ✅ |
| Gerenciar usuários da equipe | — | — | ✅ |
| Configurar o telefone/WhatsApp do site | — | — | ✅ |

Legenda: ✅ acesso total · 🔒 condicional (limitado por posse ou situação) · — sem acesso.

1. Redator só edita e envia para revisão anúncios **criados por ele** que estejam em *Rascunho* ou *Rejeitado*. Anúncio *Em revisão*, *Publicado* ou *Arquivado* fica somente leitura para o Redator.
2. O Administrador edita qualquer anúncio que não esteja *Arquivado*. Editar um anúncio publicado altera o site imediatamente.
3. Redator vê e pré-visualiza **somente os próprios anúncios**, em qualquer situação. O Administrador vê todos.

## User Stories

### Epic: Descoberta e contato (site público)

#### US-001: Página inicial e navegação por categorias — Must

**As a** visitante,
**I want to** ver as categorias e os anúncios mais recentes logo ao abrir o site,
**So that** eu chegue ao tipo de bem que procuro em poucos cliques, sem precisar de conta.

##### Acceptance Criteria

```gherkin
@US-001-S01 @happy
Scenario: Página inicial mostra categorias e anúncios recentes
  Given existem anúncios publicados em Carros, vans e utilitários e em Terrenos, sítios e fazendas
  When eu abro a página inicial do site
  Then vejo as categorias principais em destaque, cada uma com seu nome
  And vejo, abaixo, os 12 anúncios publicados mais recentes, cada um com foto de capa, título, preço e cidade/UF
  And vejo uma caixa de busca no topo da página

@US-001-S02 @happy
Scenario: Entrar em uma categoria principal
  Given existem anúncios publicados em Carros, vans e utilitários e em Motos
  When eu clico na categoria "Automóveis, Peças e Acessórios" na página inicial
  Then vejo a página "Automóveis, Peças e Acessórios" com a lista de suas subcategorias (Carros, vans e utilitários; Motos; Ônibus; Caminhões; Barcos e aeronaves; Autopeças)
  And vejo os anúncios publicados em Automóveis, Peças e Acessórios e em todas as suas subcategorias

@US-001-S03 @happy
Scenario: Entrar em uma subcategoria e voltar pelo caminho de navegação
  Given estou na página "Automóveis, Peças e Acessórios"
  When eu clico na subcategoria "Motos"
  Then vejo apenas anúncios publicados em Motos
  And vejo o caminho "Início > Automóveis, Peças e Acessórios > Motos" no topo
  When eu clico em "Automóveis, Peças e Acessórios" nesse caminho
  Then volto à página "Automóveis, Peças e Acessórios" com os anúncios de todas as suas subcategorias

@US-001-S04 @edge
Scenario: Categoria sem anúncios publicados
  Given a categoria "Serviços" não tem nenhum anúncio publicado
  When eu clico em "Serviços"
  Then vejo a mensagem "Ainda não há anúncios nesta categoria"
  And vejo links para as demais categorias principais

@US-001-S05 @edge
Scenario: Site ainda sem nenhum anúncio publicado
  Given nenhum anúncio foi publicado ainda
  When eu abro a página inicial do site
  Then vejo as categorias principais
  And vejo a mensagem "Em breve teremos novos anúncios" no lugar da lista de anúncios recentes

@US-001-S06 @negative
Scenario: Falha ao carregar a página inicial
  Given o site está com uma falha temporária ao montar a página inicial
  When eu abro a página inicial do site
  Then vejo a mensagem "Não foi possível carregar a página. Tente novamente."
  And vejo o botão "Tentar novamente" e um código de referência do erro
  And não vejo mensagens técnicas nem detalhes do sistema

@US-001-S07 @negative
Scenario: Endereço de categoria que não existe
  Given a categoria "Barcos e aeronaves" foi excluída pela equipe
  When eu abro o endereço antigo da categoria "Barcos e aeronaves"
  Then vejo a mensagem "Categoria não encontrada"
  And vejo links para a página inicial e para as categorias principais

@US-001-S08 @edge
Scenario: Página inicial em tela de celular estreita
  Given estou em uma tela de 320 px de largura
  When eu abro a página inicial do site
  Then a página não tem rolagem horizontal
  And consigo alcançar a busca, as categorias e a lista de anúncios rolando apenas para baixo
```

##### Business Rules
- Só aparecem anúncios com situação *Publicado*.
- Os "mais recentes" são ordenados pela data de publicação, do mais novo para o mais antigo. **Mostram-se 12** porque esse número preenche linhas completas em telas de 2, 3 e 4 colunas.
- Uma categoria principal lista os anúncios dela e de todas as suas subcategorias.
- A árvore de categorias é editável pelo administrador (US-013); a árvore inicial proposta está no Apêndice (pendente de confirmação, ver S3).

##### UI/UX Notes
- Wireframe: `specs/wireframes/screens/US-001-pagina-inicial.md`
- Estados a desenhar: padrão, vazio (sem anúncios), erro e categoria sem resultado.
- Layout mobile primeiro; deve haver um link discreto "Área da equipe" no rodapé para o login (US-006).

##### Dependencies
- Requires: US-013 (categorias existentes), US-010 (anúncios publicados)
- Blocks: US-002, US-003

#### US-002: Buscar e filtrar anúncios — Must

**As a** visitante,
**I want to** buscar por texto e filtrar por categoria, localização, preço e características do bem,
**So that** eu veja só os anúncios que me interessam, na ordem que eu preferir.

##### Acceptance Criteria

```gherkin
@US-002-S01 @happy
Scenario: Buscar por texto
  Given existem anúncios publicados, entre eles "Honda Civic 2018" e "Casa com quintal"
  When eu digito "civic" na caixa de busca e clico em "Buscar"
  Then vejo apenas anúncios publicados cujo título ou descrição contém "civic"
  And vejo o total "1 anúncio encontrado"

@US-002-S02 @happy
Scenario: Combinar categoria, localização e preço
  Given existem anúncios publicados de Carros, vans e utilitários em Campinas/SP e em Curitiba/PR
  When eu escolho a categoria "Carros, vans e utilitários", a UF "SP", a cidade "Campinas" e o preço máximo "50000" e clico em "Aplicar filtros"
  Then vejo apenas anúncios de Carros, vans e utilitários em Campinas/SP com preço igual ou menor que R$ 50.000
  And cada resultado mostra "Campinas/SP" e um preço de até R$ 50.000

@US-002-S03 @happy
Scenario: Filtrar por características de veículo
  Given existem anúncios publicados de Carros, vans e utilitários de várias marcas, anos e quilometragens
  When eu escolho a categoria "Carros, vans e utilitários", a marca "Honda", o ano de "2015" até "2020" e a quilometragem máxima "100000" e clico em "Aplicar filtros"
  Then vejo apenas carros da marca Honda, com ano entre 2015 e 2020 e até 100.000 km
  And os filtros de marca, ano e quilometragem só aparecem porque escolhi uma categoria de veículos

@US-002-S04 @happy
Scenario: Filtrar terrenos, sítios e fazendas por área
  Given existem anúncios publicados em Terrenos, sítios e fazendas com áreas diferentes
  When eu escolho a categoria "Terrenos, sítios e fazendas", a área mínima "300" e a máxima "600" m² e clico em "Aplicar filtros"
  Then vejo apenas terrenos com área entre 300 m² e 600 m²

@US-002-S05 @happy
Scenario: Ordenar os resultados
  Given uma busca com vários resultados de preços diferentes
  When eu escolho a ordenação "Menor preço"
  Then os resultados aparecem do menor para o maior preço
  And a ordenação escolhida continua selecionada quando eu passo para a página seguinte

@US-002-S06 @happy
Scenario: Paginar os resultados
  Given uma busca com 30 resultados
  When eu abro a lista de resultados
  Then vejo 24 anúncios na primeira página e o controle "Próxima"
  When eu clico em "Próxima"
  Then vejo os 6 anúncios restantes e a página 2 aparece destacada no controle de páginas

@US-002-S07 @edge
Scenario: Busca sem resultados
  Given nenhum anúncio publicado contém "xyzabc"
  When eu busco por "xyzabc"
  Then vejo a mensagem "Nenhum anúncio encontrado para esses filtros"
  And vejo o botão "Limpar filtros"
  When eu clico em "Limpar filtros"
  Then vejo novamente todos os anúncios publicados, do mais recente ao mais antigo

@US-002-S08 @negative
Scenario: Faixa de preço invertida
  Given estou na página de busca
  When eu informo preço mínimo "5000" e preço máximo "1000" e clico em "Aplicar filtros"
  Then vejo, junto ao campo de preço, a mensagem "O preço mínimo não pode ser maior que o máximo"
  And a lista de resultados continua igual à anterior

@US-002-S09 @edge
Scenario: Compartilhar uma busca pelo endereço da página
  Given eu apliquei os filtros categoria "Carros, vans e utilitários", UF "SP" e ordenação "Menor preço"
  When eu copio o endereço da página e o abro em outra aba do navegador
  Then os mesmos filtros aparecem preenchidos
  And vejo os mesmos resultados na mesma ordem

@US-002-S10 @edge
Scenario: Trocar a UF limpa a cidade escolhida
  Given escolhi a UF "SP" e a cidade "Campinas"
  When eu troco a UF para "RJ"
  Then o campo cidade volta para "Todas as cidades"
  And a lista de cidades passa a mostrar apenas cidades do Rio de Janeiro

@US-002-S11 @negative
Scenario: Falha ao buscar
  Given o site está com uma falha temporária na busca
  When eu clico em "Buscar"
  Then vejo a mensagem "Não foi possível buscar agora. Tente novamente."
  And os filtros que eu escolhi continuam preenchidos
  And vejo o botão "Tentar novamente"

@US-002-S12 @edge
Scenario: Busca em tela de celular estreita
  Given estou em uma tela de 320 px de largura
  When eu abro a página de busca
  Then a página não tem rolagem horizontal
  And consigo abrir e fechar o painel de filtros e ver os resultados rolando apenas para baixo
```

##### Business Rules
- A busca por texto considera título e descrição e ignora diferença entre maiúsculas/minúsculas e acentos.
- Os filtros se combinam (todos precisam ser atendidos). Escolher uma categoria principal inclui suas subcategorias.
- A cidade só pode ser escolhida depois da UF, e sempre pertence à UF escolhida (ver S15).
- Filtros de atributos específicos aparecem só para categorias de Automóveis, Peças e Acessórios (marca, modelo, ano, quilometragem) e de Terrenos, sítios e fazendas (área em m²) (ver S4).
- Ordenações: **Mais recentes** (padrão), Menor preço, Maior preço.
- Anúncios de Serviços não têm preço. O comportamento dos filtros de preço para Serviços está em aberto (A6).
- **24 resultados por página**, porque 24 divide em linhas completas de 2, 3 e 4 colunas (ver S11).
- Só aparecem anúncios *Publicados*.
- Os filtros ficam no endereço da página, para poder compartilhar ou recarregar sem perdê-los.

##### UI/UX Notes
- Wireframe: `specs/wireframes/screens/US-002-busca-filtros.md`
- Estados a desenhar: carregando, sem resultado, erro, e filtros recolhíveis em telas estreitas.

##### Dependencies
- Requires: US-001, US-013
- Blocks: US-003

#### US-003: Ver o detalhe do anúncio — Must

**As a** visitante,
**I want to** ver todas as fotos, a descrição e as características de um anúncio,
**So that** eu decida se vale a pena entrar em contato.

##### Acceptance Criteria

```gherkin
@US-003-S01 @happy
Scenario: Abrir um anúncio completo
  Given existe o anúncio publicado "Honda Civic 2018" com 6 fotos
  When eu clico nesse anúncio na lista de resultados
  Then vejo a foto de capa em destaque e as miniaturas das 6 fotos
  And vejo título, preço, categoria, cidade/UF, data de publicação e descrição completa
  And vejo marca, modelo, ano e quilometragem
  And vejo os botões de contato e o botão de favoritar

@US-003-S02 @happy
Scenario: Percorrer a galeria de um anúncio com 20 fotos
  Given existe um anúncio publicado com 20 fotos
  When eu clico na 5ª miniatura
  Then a foto em destaque passa a ser a 5ª e vejo o indicador "5 de 20"
  When eu pressiono a seta para a direita do teclado
  Then a foto em destaque passa a ser a 6ª e o indicador mostra "6 de 20"

@US-003-S03 @happy
Scenario: Ampliar uma foto
  Given estou na página de um anúncio com fotos
  When eu clico na foto em destaque
  Then a foto abre ampliada sobre a página, com o botão "Fechar"
  When eu pressiono a tecla Esc
  Then a foto ampliada se fecha e volto à página do anúncio no mesmo ponto

@US-003-S04 @edge
Scenario: Anúncio de categoria sem ficha de veículo nem de terreno
  Given existe um anúncio publicado na categoria "Decorações Para Casa"
  When eu abro esse anúncio
  Then vejo título, preço, cidade/UF, descrição e as características "Condição" e "Tipo de produto"
  And não vejo o bloco de características de veículo nem de terreno

@US-003-S05 @edge
Scenario: Anúncio com uma única foto
  Given existe um anúncio publicado com 1 foto
  When eu abro esse anúncio
  Then vejo a foto em destaque
  And não vejo miniaturas nem setas de navegação

@US-003-S06 @negative
Scenario: Abrir um anúncio que não está mais disponível
  Given o anúncio "Honda Civic 2018" foi arquivado pela equipe
  When eu abro o endereço que eu tinha salvo desse anúncio
  Then vejo a mensagem "Este anúncio não está mais disponível"
  And vejo links para a página inicial e para a categoria em que ele estava
  And não vejo título, fotos nem descrição do anúncio

@US-003-S07 @negative
Scenario: Uma foto não carrega
  Given um anúncio publicado com 3 fotos, das quais a 2ª não pode ser carregada
  When eu abro esse anúncio e escolho a 2ª miniatura
  Then vejo no lugar dela a mensagem "Foto indisponível"
  And consigo continuar navegando para a 1ª e a 3ª fotos

@US-003-S08 @edge
Scenario: Anúncio em tela de celular estreita
  Given estou em uma tela de 320 px de largura
  When eu abro um anúncio com 20 fotos
  Then a página não tem rolagem horizontal
  And consigo trocar de foto deslizando ou tocando nas miniaturas
  And os botões de contato ficam visíveis sem eu precisar ampliar a tela
```

##### Business Rules
- Um anúncio mostra **até 20 fotos** (Serviços: até 6; Vagas de emprego não tem fotos); a primeira é a capa.
- **Vagas de emprego:** o valor do campo Preço é exibido com o rótulo **"Salário"** no card do anúncio e na página de detalhe; nas demais categorias o rótulo é "Preço". O formato do valor segue a S29 (centavos só quando não são zero).
- Só anúncios *Publicados* podem ser vistos pelo público. Qualquer outra situação (rascunho, em revisão, rejeitado, arquivado) ou endereço inexistente mostra a mesma mensagem de indisponibilidade, sem revelar o conteúdo.
- O anúncio **não mostra nome nem telefone do vendedor**; o contato exibido é sempre o do intermediário (US-004).
- O texto do anúncio é exibido como texto puro; não interpreta código nem formatação especial.

##### UI/UX Notes
- Wireframe: `specs/wireframes/screens/US-003-detalhe-anuncio.md`
- Estados a desenhar: padrão, uma foto, sem ficha de veículo nem de terreno, indisponível, erro e foto quebrada.
- A galeria precisa ser operável por teclado e leitor de tela (texto alternativo em cada foto).

##### Dependencies
- Requires: US-001, US-010
- Blocks: US-004, US-005

#### US-004: Contatar o intermediário por telefone ou WhatsApp — Must

**As a** visitante,
**I want to** ligar ou chamar no WhatsApp direto pelo anúncio,
**So that** eu fale com o intermediário sobre o bem sem precisar de conta nem de mensagens dentro do site.

##### Acceptance Criteria

```gherkin
@US-004-S01 @happy
Scenario: Chamar no WhatsApp a partir de um anúncio
  Given o telefone do site está configurado como (11) 91234-5678
  And estou na página do anúncio "Honda Civic 2018"
  When eu clico em "Chamar no WhatsApp"
  Then o WhatsApp abre uma conversa com o número (11) 91234-5678
  And a mensagem já vem preenchida citando o título "Honda Civic 2018" e o endereço da página do anúncio

@US-004-S02 @happy
Scenario: Ligar a partir de um anúncio
  Given o telefone do site está configurado como (11) 91234-5678
  And estou na página de um anúncio em um celular
  When eu clico em "Ligar"
  Then o aplicativo de telefone abre com o número (11) 91234-5678 pronto para discar

@US-004-S03 @happy
Scenario: O contato é visível sem login
  Given não estou logado e não tenho conta
  When eu abro a página de um anúncio publicado
  Then vejo o número (11) 91234-5678 escrito na página
  And vejo os botões "Ligar" e "Chamar no WhatsApp"

@US-004-S04 @edge
Scenario: Título com acentos e símbolos na mensagem do WhatsApp
  Given existe o anúncio publicado com o título Sítio "Boa Vista" & Cia
  When eu clico em "Chamar no WhatsApp" nesse anúncio
  Then a mensagem preenchida mostra o título exatamente como Sítio "Boa Vista" & Cia, com acentos, aspas e o símbolo &

@US-004-S05 @edge
Scenario: WhatsApp em computador sem o aplicativo instalado
  Given estou em um computador sem o aplicativo do WhatsApp
  When eu clico em "Chamar no WhatsApp"
  Then o WhatsApp Web abre em uma nova aba com a conversa para o número do intermediário e a mensagem preenchida
  And a página do anúncio continua aberta na aba anterior
```

##### Business Rules
- O número exibido é **sempre o do intermediário**, configurado pelo administrador (US-015). Vale o mesmo número para ligação e WhatsApp (ver S2).
- O número aparece formatado com DDD, por exemplo (11) 91234-5678.
- O sistema **não registra** quem clicou nem quem entrou em contato.
- Mensagem pré-preenchida (texto fixo): cumprimento, título do anúncio e endereço da página do anúncio.

##### UI/UX Notes
- Wireframe: `specs/wireframes/screens/US-004-contato-intermediario.md` (bloco de contato da página de detalhe).
- Botões grandes e fáceis de tocar no celular, com rótulo de texto (não só ícone).

##### Dependencies
- Requires: US-003, US-015
- Blocks: —

#### US-005: Favoritar anúncios neste navegador — Must

**As a** visitante,
**I want to** marcar anúncios como favoritos e vê-los depois em uma lista,
**So that** eu volte facilmente aos anúncios que me interessaram, sem criar conta.

> Depende da suposição S1: os favoritos ficam salvos **apenas no navegador** do visitante. O Product Owner precisa confirmar (alternativa: retirar Favoritos do MVP).

##### Acceptance Criteria

```gherkin
@US-005-S01 @happy
Scenario: Favoritar um anúncio pela lista
  Given estou na lista de resultados e ainda não tenho favoritos
  When eu clico no coração do anúncio "Honda Civic 2018"
  Then o coração fica preenchido
  And o contador de favoritos no topo da página muda de 0 para 1

@US-005-S02 @happy
Scenario: Favoritar e desfavoritar pela página do anúncio
  Given estou na página do anúncio "Honda Civic 2018", que não está nos meus favoritos
  When eu clico em "Favoritar"
  Then o botão passa a mostrar "Favoritado"
  When eu clico em "Favoritado"
  Then o botão volta a mostrar "Favoritar" e o contador de favoritos diminui em 1

@US-005-S03 @happy
Scenario: Favoritos continuam depois de fechar o navegador
  Given eu favoritei o anúncio "Honda Civic 2018"
  When eu fecho o navegador e abro o site de novo no mesmo aparelho
  Then o coração desse anúncio continua preenchido
  And a página "Meus favoritos" lista o anúncio "Honda Civic 2018"

@US-005-S04 @happy
Scenario: Remover um anúncio da página Meus favoritos
  Given tenho 2 anúncios favoritados
  When eu abro "Meus favoritos" e clico em "Remover" no anúncio "Honda Civic 2018"
  Then esse anúncio desaparece da lista e sobra 1 anúncio
  And o contador de favoritos no topo mostra 1

@US-005-S05 @edge
Scenario: Lista de favoritos vazia
  Given não tenho nenhum favorito neste navegador
  When eu abro "Meus favoritos"
  Then vejo a mensagem "Você ainda não favoritou nenhum anúncio"
  And vejo um link para a página inicial

@US-005-S06 @edge
Scenario: Um favorito deixa de estar disponível
  Given eu favoritei o anúncio "Honda Civic 2018" e a equipe o arquivou depois
  When eu abro "Meus favoritos"
  Then o anúncio "Honda Civic 2018" não aparece na lista
  And vejo o aviso "1 anúncio favoritado deixou de estar disponível e foi removido da sua lista"

@US-005-S07 @negative
Scenario: O navegador não permite salvar favoritos
  Given meu navegador bloqueia o armazenamento de dados do site
  When eu clico no coração de um anúncio
  Then vejo a mensagem "Não foi possível salvar seus favoritos neste navegador"
  And o coração continua vazio

@US-005-S08 @edge
Scenario: Favoritos não acompanham o visitante em outro aparelho
  Given eu favoritei um anúncio no meu celular
  When eu abro "Meus favoritos" no computador
  Then a lista está vazia
  And vejo o aviso permanente "Seus favoritos ficam salvos apenas neste navegador"
```

##### Business Rules
- Favoritos ficam **somente no navegador e no aparelho** onde foram marcados; não há sincronização entre aparelhos e o sistema não os guarda no servidor (ver S1).
- Só anúncios *Publicados* aparecem em "Meus favoritos"; os que deixaram de estar disponíveis são retirados da lista com aviso.
- Não há limite de quantidade definido para a v1.
- Se o visitante limpar os dados do navegador, perde os favoritos; o aviso da página deixa isso claro.

##### UI/UX Notes
- Wireframe: `specs/wireframes/screens/US-005-favoritos.md`
- Estados a desenhar: lista com itens, vazia, aviso de indisponíveis e armazenamento bloqueado.
- O coração deve ter nome acessível ("Favoritar anúncio Honda Civic 2018") e ser operável por teclado.

##### Dependencies
- Requires: US-002, US-003
- Blocks: —

### Epic: Acesso da equipe

#### US-006: Entrar e sair do painel da equipe — Must

**As a** membro da equipe (redator ou administrador),
**I want to** entrar no painel com meu e-mail e senha e sair quando terminar,
**So that** só a equipe consiga cadastrar e gerenciar anúncios.

##### Acceptance Criteria

```gherkin
@US-006-S01 @happy
Scenario: Redator entra no painel
  Given tenho uma conta ativa de Redator
  When eu abro a página de entrada da equipe, informo meu e-mail e minha senha corretos e clico em "Entrar"
  Then vejo a página "Meus anúncios" com meu nome no topo
  And vejo o botão "Sair"
  And não vejo os menus Categorias, Usuários nem Configurações

@US-006-S02 @happy
Scenario: Administrador entra no painel
  Given tenho uma conta ativa de Administrador
  When eu informo meu e-mail e minha senha corretos e clico em "Entrar"
  Then vejo a "Fila de revisão"
  And vejo os menus Anúncios, Categorias, Usuários e Configurações

@US-006-S03 @happy
Scenario: Sair do painel
  Given estou logado no painel
  When eu clico em "Sair"
  Then volto à página de entrada da equipe
  When eu clico no botão Voltar do navegador
  Then não vejo o conteúdo do painel e sou levado à página de entrada

@US-006-S04 @negative
Scenario: E-mail ou senha incorretos
  Given estou na página de entrada da equipe
  When eu informo um e-mail cadastrado com uma senha errada e clico em "Entrar"
  Then vejo a mensagem "E-mail ou senha inválidos, ou conta desativada"
  And o campo de senha aparece vazio
  And continuo na página de entrada

@US-006-S05 @negative
Scenario: Conta desativada
  Given minha conta foi desativada por um administrador
  When eu informo meu e-mail e minha senha corretos e clico em "Entrar"
  Then vejo a mesma mensagem "E-mail ou senha inválidos, ou conta desativada"
  And não entro no painel

@US-006-S06 @negative
Scenario: Muitas tentativas de entrada
  Given errei a senha 5 vezes em 15 minutos a partir do mesmo aparelho ou rede
  When eu tento entrar uma sexta vez, mesmo com a senha correta
  Then vejo a mensagem "Muitas tentativas. Tente novamente em alguns minutos."
  And não entro no painel até passar o período de espera

@US-006-S07 @edge
Scenario: Abrir uma página do painel sem estar logado
  Given não estou logado
  When eu abro diretamente o endereço da página "Meus anúncios"
  Then sou levado à página de entrada da equipe
  When eu entro com meus dados corretos
  Then vejo a página "Meus anúncios", que eu tinha tentado abrir

@US-006-S08 @edge
Scenario: Sessão expirada por inatividade
  Given fiquei 30 minutos sem usar o painel
  When eu clico em qualquer opção do painel
  Then sou levado à página de entrada com o aviso "Sua sessão expirou. Entre novamente."

@US-006-S09 @edge
Scenario: Primeiro acesso exige trocar a senha provisória
  Given um administrador criou minha conta com uma senha provisória
  When eu entro pela primeira vez com essa senha
  Then sou levado a uma tela "Defina sua nova senha" antes de ver o painel
  When eu informo e confirmo uma nova senha que cumpre a política de senha
  Then vejo o painel
  And, se eu tentar entrar de novo com a senha provisória, vejo a mensagem "E-mail ou senha inválidos, ou conta desativada"

@US-006-S10 @negative
Scenario: Redator tenta abrir uma página exclusiva do administrador
  Given estou logado como Redator
  When eu abro diretamente o endereço da página "Categorias"
  Then vejo a mensagem "Você não tem permissão para acessar esta página"
  And não vejo o conteúdo da página de categorias
```

##### Business Rules
- Só a equipe tem conta. O visitante do site público **não cria conta nem entra**.
- **Política de senha:** mínimo de 8 caracteres, com maiúscula, minúscula, número e símbolo.
- **Limite de tentativas:** 5 falhas em 15 minutos por origem bloqueiam novas tentativas até o fim do período. Valor adotado de `rules/security.md`.
- **Sessão:** expira após 30 minutos sem uso (ver S16). A mensagem de erro do login **não revela** se o e-mail existe, para não ajudar quem tenta adivinhar contas.
- Após entrar, o Redator vai para "Meus anúncios" e o Administrador vai para a "Fila de revisão".
- O primeiro Administrador é criado na implantação, fora da interface (ver S17).

##### UI/UX Notes
- Wireframe: `specs/wireframes/screens/US-006-login-equipe.md`
- Estados a desenhar: padrão, erro de credencial, bloqueio por tentativas, sessão expirada, troca de senha obrigatória e acesso negado.

##### Dependencies
- Requires: US-014 (contas existem)
- Blocks: US-007, US-008, US-009, US-010, US-011, US-012, US-013, US-014, US-015

#### US-007: Recuperar senha esquecida — Should

**As a** membro da equipe,
**I want to** redefinir minha senha quando a esquecer,
**So that** eu volte a trabalhar sem depender de ajuda técnica.

> Depende da suposição S6 (recuperação por e-mail). Se o Product Owner preferir que o administrador redefina senhas, esta história é substituída.

##### Acceptance Criteria

```gherkin
@US-007-S01 @happy
Scenario: Pedir a redefinição de senha
  Given tenho uma conta ativa e esqueci minha senha
  When eu clico em "Esqueci minha senha", informo meu e-mail e clico em "Enviar"
  Then vejo a mensagem "Se o e-mail estiver cadastrado, enviaremos as instruções"
  And recebo um e-mail com um link para definir uma nova senha

@US-007-S02 @happy
Scenario: Definir uma nova senha pelo link
  Given recebi o e-mail com o link de redefinição há menos de 1 hora
  When eu abro o link, informo uma nova senha válida duas vezes e clico em "Salvar senha"
  Then vejo a página de entrada com a mensagem "Senha alterada. Entre com a nova senha."
  And consigo entrar com a nova senha
  And a senha antiga deixa de funcionar

@US-007-S03 @negative
Scenario: E-mail não cadastrado
  Given o e-mail "naoexiste@exemplo.com.br" não pertence a nenhuma conta
  When eu peço a redefinição de senha para esse e-mail
  Then vejo a mesma mensagem "Se o e-mail estiver cadastrado, enviaremos as instruções"
  And nenhum e-mail é enviado

@US-007-S04 @negative
Scenario: Link de redefinição expirado
  Given o link de redefinição foi enviado há mais de 1 hora
  When eu abro o link
  Then vejo a mensagem "Este link expirou"
  And vejo o botão "Pedir novo link"

@US-007-S05 @negative
Scenario: Link de redefinição já utilizado
  Given eu já usei o link de redefinição para trocar minha senha
  When eu abro o mesmo link de novo
  Then vejo a mensagem "Este link já foi usado"
  And vejo o botão "Pedir novo link"

@US-007-S06 @negative
Scenario: Nova senha que não cumpre a política
  Given abri o link de redefinição dentro do prazo
  When eu informo a nova senha "abc123" duas vezes e clico em "Salvar senha"
  Then vejo a lista de requisitos que faltam (mínimo de 8 caracteres, maiúscula e símbolo)
  And a senha não é alterada

@US-007-S07 @negative
Scenario: Confirmação de senha diferente
  Given abri o link de redefinição dentro do prazo
  When eu informo duas senhas diferentes e clico em "Salvar senha"
  Then vejo a mensagem "As senhas não coincidem"
  And a senha não é alterada
```

##### Business Rules
- O link de redefinição vale **1 hora** e só pode ser usado **uma vez** (valor adotado de `rules/security.md`).
- A resposta ao pedido é sempre a mesma, exista ou não a conta, para não revelar quais e-mails são cadastrados.
- Conta desativada não recebe e-mail de redefinição.
- A nova senha segue a política de senha da US-006.

##### UI/UX Notes
- Wireframe: `specs/wireframes/screens/US-007-recuperar-senha.md` (telas "Esqueci minha senha" e "Definir nova senha").
- Estados a desenhar: padrão, confirmação neutra, link expirado ou usado, e erro de validação.

##### Dependencies
- Requires: US-006
- Blocks: —

### Epic: Anúncios

#### US-008: Criar e editar anúncio com fotos — Must

**As a** redator,
**I want to** cadastrar um anúncio com suas informações e fotos, salvando como rascunho,
**So that** eu possa montar o anúncio com calma antes de enviá-lo para revisão.

##### Acceptance Criteria

```gherkin
@US-008-S01 @happy
Scenario: Salvar um rascunho completo
  Given estou logado como Redator
  When eu clico em "Novo anúncio", preencho título "Honda Civic 2018", descrição, preço "R$ 62.000,00", categoria "Carros, vans e utilitários", CEP "13015-100" (Cidade e UF são preenchidas como Campinas/SP), marca "Honda", modelo "Civic", ano "2018" e quilometragem "45000" e clico em "Salvar rascunho"
  Then vejo a mensagem "Rascunho salvo"
  And o anúncio aparece em "Meus anúncios" com a situação "Rascunho"
  When eu reabro o anúncio
  Then todos os campos mostram os valores que eu preenchi

@US-008-S02 @happy
Scenario: Adicionar fotos ao anúncio
  Given estou editando um rascunho sem fotos
  When eu seleciono 3 fotos válidas e clico em "Enviar fotos"
  Then vejo as 3 miniaturas na ordem em que enviei, com a primeira marcada "Capa"
  When eu salvo o rascunho e o reabro
  Then as 3 fotos continuam no anúncio, na mesma ordem

@US-008-S03 @happy
Scenario: Trocar a capa e remover uma foto
  Given estou editando um rascunho com 3 fotos
  When eu clico em "Tornar capa" na 3ª foto
  Then ela passa para a primeira posição e recebe a marca "Capa"
  When eu clico em "Remover" na 2ª foto e confirmo
  Then o rascunho passa a ter 2 fotos

@US-008-S04 @negative
Scenario: Passar do limite de 20 fotos
  Given estou editando um rascunho que já tem 20 fotos
  When eu tento enviar mais 1 foto
  Then vejo a mensagem "Cada anúncio pode ter no máximo 20 fotos"
  And o anúncio continua com 20 fotos

@US-008-S05 @negative
Scenario: Enviar um arquivo que não é foto aceita
  Given estou editando um rascunho
  When eu envio um arquivo PDF e uma foto de 15 MB
  Then vejo a mensagem "Formato não aceito. Use JPG, PNG, WebP, GIF ou HEIC" para o PDF
  And vejo a mensagem "A foto excede o limite de 10 MB" para a foto grande
  And nenhum dos dois arquivos é adicionado ao anúncio

@US-008-S06 @negative
Scenario: Falha ao enviar uma foto
  Given estou editando um rascunho e já preenchi título e descrição
  When a conexão cai durante o envio de uma das fotos
  Then vejo, sobre essa foto, a mensagem "Falha ao enviar" e o botão "Tentar de novo"
  And as outras fotos e os textos que eu preenchi continuam na tela

@US-008-S07 @edge
Scenario: Salvar um rascunho só com o título
  Given estou criando um novo anúncio
  When eu preencho apenas o título "Moto para retirar peças" e clico em "Salvar rascunho"
  Then o rascunho é salvo e aparece em "Meus anúncios" como "Rascunho"
  And os demais campos aparecem vazios quando eu o reabro

@US-008-S08 @negative
Scenario: Salvar sem título
  Given estou criando um novo anúncio
  When eu deixo o título vazio e clico em "Salvar rascunho"
  Then vejo, junto ao campo, a mensagem "Informe um título"
  And o rascunho não é criado

@US-008-S09 @edge
Scenario: Campos mudam conforme a categoria
  Given estou editando um rascunho
  When eu escolho a categoria "Carros, vans e utilitários"
  Then aparecem os campos marca, modelo, ano, versão e quilometragem
  When eu escolho a categoria "Terrenos, sítios e fazendas"
  Then os campos de veículo somem e aparecem os campos "Tipo" e "Área (m²)"
  When eu escolho a categoria "Livros e revistas"
  Then aparecem só os campos específicos "Condição" e "Tipo de produto"
  When eu escolho a categoria "Vagas de emprego"
  Then o campo "Fotos" some, aparece o campo "Área" e o título passa a aceitar no máximo 90 caracteres

@US-008-S10 @negative
Scenario: Redator tenta editar anúncio de outro redator
  Given existe um rascunho criado por outro Redator
  When eu abro diretamente o endereço de edição desse rascunho
  Then vejo a mensagem "Você não tem permissão para acessar este anúncio"
  And não vejo o conteúdo do rascunho

@US-008-S11 @edge
Scenario: Corrigir um anúncio rejeitado
  Given tenho um anúncio "Rejeitado" com o motivo "Fotos escuras"
  When eu abro esse anúncio
  Then vejo o motivo "Fotos escuras" em destaque no topo do formulário
  And posso alterar os campos e as fotos e salvar
  And a situação continua "Rejeitado" até eu enviá-lo de novo para revisão

@US-008-S12 @edge
Scenario: Anúncio em revisão não pode ser editado pelo Redator
  Given tenho um anúncio com a situação "Em revisão"
  When eu abro esse anúncio
  Then vejo os dados apenas para leitura
  And vejo a mensagem "Este anúncio está em revisão e não pode ser editado"

@US-008-S13 @edge
Scenario: Administrador corrige o preço de um anúncio publicado
  Given estou logado como Administrador e o anúncio "Honda Civic 2018" está publicado por R$ 62.000
  When eu altero o preço para "R$ 59.000,00" e clico em "Salvar"
  Then a situação continua "Publicado"
  When um visitante abre a página desse anúncio
  Then ele vê o preço R$ 59.000

@US-008-S14 @negative
Scenario: Serviço de CEP fora do ar
  Given estou criando um anúncio e o serviço de consulta de CEP não responde
  When eu digito o CEP "13015-100"
  Then vejo "Buscando…", depois "Buscando… (tentativa 2 de 2)" e, em seguida, a mensagem "Não foi possível buscar o CEP. Preencha Cidade e UF manualmente."
  And a UF e a Cidade passam a ser listas que eu posso escolher
  When eu clico em "Enviar para revisão" sem escolher a cidade
  Then vejo a pendência "Informe a cidade", com link para o campo
  When eu escolho a UF "SP" e a cidade "Campinas" e envio o anúncio para revisão
  Then o Administrador vê, na pré-visualização desse anúncio, o selo "Cidade/UF informadas manualmente (CEP não conferido)" ao lado do CEP e da cidade
  And o selo não impede que ele publique o anúncio
```

##### Business Rules
- **Campos comuns:** título, descrição, preço, categoria, CEP (Cidade e UF preenchidas automaticamente a partir do CEP) e fotos. **Exceções:** Serviços não tem Preço; Vagas de emprego não tem Fotos. **Campos específicos de cada categoria:** Apêndice B (S4).
- **Título:** em Vagas de emprego, no máximo **90** caracteres, com contador visível "X/90" (caracteres digitados / máximo; sem mínimo); em Serviços, no máximo **120**; nas demais categorias o limite será definido em `/arch` (S13).
- **Rótulo da descrição:** o texto livre é o campo Descrição em todas as categorias. O rótulo é "Descrição" na maioria e **"Informações adicionais"** em Serviços e Vagas de emprego, onde o limite é **6000** caracteres, com contador visível "X/6000". Nas demais categorias o limite será definido em `/arch` (S13).
- O anúncio pertence à categoria mais específica disponível (a subcategoria, quando existir).
- **Fotos:** no máximo **20** por anúncio (definido pelo Product Owner). **Exceções:** Serviços, no máximo **6**; Vagas de emprego **não tem fotos**. Formatos **JPG, PNG, WebP, GIF e HEIC**, em todas as categorias; até **10 MB** por foto; a primeira foto é a capa e pode ser trocada.
- **Fotos HEIC** são convertidas para WebP ou JPG no envio, para aparecerem em todos os navegadores (Firefox e Chrome não exibem HEIC).
- **CEP fora do ar:** se a consulta do CEP falhar duas vezes (sem resposta, erro do serviço ou sem rede), Cidade e UF deixam de ser automáticas e passam a ser escolhidas em listas (UF entre as 27 unidades federativas; cidade na lista da UF). O CEP continua obrigatório e fica como foi digitado. Trocar o CEP dispara nova busca; se ela der certo, Cidade e UF voltam a ser automáticas. "CEP não encontrado" não é falha do serviço e não abre o preenchimento manual (ver S24).
- **Selo de conferência:** um anúncio com Cidade/UF preenchidas manualmente mostra ao Administrador, na pré-visualização, o selo "Cidade/UF informadas manualmente (CEP não conferido)". O selo é só informativo e não impede a publicação; o Administrador pode conferir o CEP antes de publicar.
- **Padronização do nome da cidade:** a cidade é sempre escolhida em lista. Se não houver lista para a UF, o nome digitado é comparado com a lista oficial ignorando acentos e maiúsculas; sem correspondência, é salvo sem espaços extras e com iniciais maiúsculas, **mantendo os acentos** ("sao jose" → "Sao Jose"; "são josé" → "São José").
- Um **rascunho pode ser salvo com o título apenas**; o que é obrigatório para enviar à revisão está na US-009 (ver S13).
- O anúncio **não guarda nome nem telefone do vendedor** e não tem campo para eles.
- O Redator só vê e altera os próprios anúncios em *Rascunho* ou *Rejeitado*; o Administrador altera qualquer anúncio que não esteja *Arquivado* (ver S10).
- Não há função de excluir rascunho na v1; um rascunho sem uso pode ser arquivado pelo Administrador (ver S10).

##### UI/UX Notes
- Wireframe: `specs/wireframes/screens/US-008-editar-anuncio.md`
- Estados a desenhar: novo, edição, enviando foto, erro de foto, limite de 20 fotos, somente leitura (em revisão) e rejeitado com motivo.
- Reordenar fotos precisa funcionar sem arrastar (botão "Tornar capa"), para atender a acessibilidade e o celular.

##### Dependencies
- Requires: US-006, US-013
- Blocks: US-009

#### US-009: Enviar anúncio para revisão — Must

**As a** redator,
**I want to** enviar um anúncio pronto para o administrador revisar,
**So that** ele possa ser aprovado e publicado.

##### Acceptance Criteria

```gherkin
@US-009-S01 @happy
Scenario: Enviar um rascunho completo para revisão
  Given tenho um rascunho com todos os campos obrigatórios preenchidos e ao menos 1 foto
  When eu clico em "Enviar para revisão" e confirmo
  Then vejo a mensagem "Anúncio enviado para revisão"
  And a situação do anúncio em "Meus anúncios" passa a ser "Em revisão"
  And o anúncio aparece na "Fila de revisão" do Administrador

@US-009-S02 @negative
Scenario: Enviar um rascunho incompleto
  Given tenho um rascunho sem foto e sem CEP
  When eu clico em "Enviar para revisão"
  Then vejo a lista de pendências "Adicione ao menos 1 foto" e "Informe o CEP", cada uma com link para o campo
  And a situação continua "Rascunho"

@US-009-S03 @negative
Scenario: Faltam características obrigatórias da categoria
  Given tenho um rascunho na categoria "Carros, vans e utilitários" sem quilometragem
  When eu clico em "Enviar para revisão"
  Then vejo a pendência "Informe a quilometragem"
  And a situação continua "Rascunho"

@US-009-S04 @edge
Scenario: Reenviar um anúncio rejeitado depois de corrigi-lo
  Given tenho um anúncio "Rejeitado" que já corrigi
  When eu clico em "Enviar para revisão" e confirmo
  Then a situação passa a ser "Em revisão"
  And o anúncio volta a aparecer na "Fila de revisão" do Administrador

@US-009-S05 @edge
Scenario: Clicar duas vezes em enviar
  Given tenho um rascunho completo
  When eu clico duas vezes seguidas em "Enviar para revisão" e confirmo
  Then o anúncio aparece uma única vez na "Fila de revisão"
```

##### Business Rules
- **Obrigatório para enviar:** título, descrição, preço maior que zero, categoria, CEP válido (que preenche Cidade e UF), ao menos 1 foto e os campos obrigatórios da categoria (Apêndice B) (ver S12 e S13). **Exceções:** Serviços não tem Preço; Vagas de emprego não tem campo Fotos, por isso não exige foto.
- Depois de enviado, o anúncio fica **somente leitura para o Redator** até o Administrador decidir.
- O Administrador que cria um anúncio segue o mesmo caminho (enviar para revisão e depois publicar), sem atalho (ver S10).

##### UI/UX Notes
- Wireframe: `specs/wireframes/screens/US-009-enviar-revisao.md`
- Estados a desenhar: diálogo de confirmação, lista de pendências e mensagem de sucesso.

##### Dependencies
- Requires: US-008
- Blocks: US-010

#### US-010: Revisar e publicar ou rejeitar anúncios — Must

**As a** administrador,
**I want to** conferir cada anúncio enviado e decidir se ele vai ao ar,
**So that** nada apareça ao público sem a minha aprovação.

##### Acceptance Criteria

```gherkin
@US-010-S01 @happy
Scenario: Ver a fila de revisão
  Given existem 3 anúncios com a situação "Em revisão"
  When eu abro a "Fila de revisão"
  Then vejo os 3 anúncios, do mais antigo para o mais recente
  And cada linha mostra título, autor, categoria e data de envio

@US-010-S02 @happy
Scenario: Pré-visualizar um anúncio antes de decidir
  Given estou na "Fila de revisão"
  When eu clico em um anúncio
  Then vejo uma pré-visualização igual à página pública, marcada "Pré-visualização — ainda não publicado"
  And vejo os botões "Publicar", "Rejeitar" e "Editar"

@US-010-S03 @happy
Scenario: Publicar um anúncio
  Given estou na pré-visualização de um anúncio "Em revisão"
  When eu clico em "Publicar" e confirmo
  Then a situação passa a "Publicado" e o anúncio sai da "Fila de revisão"
  When um visitante abre a página inicial
  Then ele vê esse anúncio entre os mais recentes e o encontra na busca

@US-010-S04 @happy
Scenario: Rejeitar um anúncio com motivo
  Given estou na pré-visualização de um anúncio "Em revisão" criado pela redatora Ana
  When eu clico em "Rejeitar", escrevo o motivo "Fotos escuras; envie fotos com boa iluminação" e confirmo
  Then a situação passa a "Rejeitado" e o anúncio sai da "Fila de revisão"
  And o anúncio não aparece no site público
  When Ana abre "Meus anúncios"
  Then ela vê o anúncio "Rejeitado" com o motivo "Fotos escuras; envie fotos com boa iluminação"

@US-010-S05 @negative
Scenario: Rejeitar sem informar o motivo
  Given estou na pré-visualização de um anúncio "Em revisão"
  When eu clico em "Rejeitar", deixo o motivo vazio e confirmo
  Then vejo a mensagem "Informe o motivo da rejeição"
  And a situação continua "Em revisão"

@US-010-S06 @edge
Scenario: Fila de revisão vazia
  Given nenhum anúncio está "Em revisão"
  When eu abro a "Fila de revisão"
  Then vejo a mensagem "Nenhum anúncio aguardando revisão"

@US-010-S07 @edge
Scenario: Dois administradores decidem o mesmo anúncio
  Given eu e outro Administrador abrimos o mesmo anúncio "Em revisão"
  And o outro Administrador clicou em "Publicar" antes de mim
  When eu clico em "Rejeitar" e confirmo
  Then vejo a mensagem "Este anúncio já foi publicado por outro administrador"
  And a situação continua "Publicado"

@US-010-S08 @negative
Scenario: Publicar sem o telefone do site configurado
  Given o telefone/WhatsApp do site ainda não foi configurado
  When eu clico em "Publicar" em um anúncio "Em revisão"
  Then vejo a mensagem "Configure o telefone/WhatsApp do site antes de publicar" com um link para "Configurações"
  And a situação do anúncio continua "Em revisão"

@US-010-S09 @negative
Scenario: Redator não pode revisar anúncios
  Given estou logado como Redator
  When eu abro diretamente o endereço da "Fila de revisão"
  Then vejo a mensagem "Você não tem permissão para acessar esta página"
  And não vejo botões "Publicar" nem "Rejeitar"
```

##### Business Rules
- **Pré-moderação:** nenhum anúncio aparece ao público sem que um Administrador o publique.
- Rejeitar exige **motivo** (texto obrigatório, visível ao Redator autor) (ver S9).
- O sistema guarda quem publicou ou rejeitou e quando, para o Administrador saber a quem perguntar (ver S10).
- Sem telefone/WhatsApp configurado (US-015) nenhum anúncio pode ser publicado, para que todo anúncio no ar tenha contato visível.
- O Administrador pode corrigir o anúncio (botão "Editar") antes de decidir; a decisão vale sobre a versão corrigida.
- Ordem da fila: do mais antigo para o mais novo, para ninguém esperar indefinidamente.

##### UI/UX Notes
- Wireframe: `specs/wireframes/screens/US-010-revisar-anuncio.md`
- Estados a desenhar: fila com itens, fila vazia, pré-visualização, diálogo de rejeição, conflito de decisão e aviso de contato não configurado.

##### Dependencies
- Requires: US-009, US-015
- Blocks: US-001, US-002, US-003, US-011

#### US-011: Despublicar e arquivar anúncios — Must

**As a** administrador,
**I want to** tirar um anúncio do ar, temporariamente ou em definitivo,
**So that** o site não mostre bens já vendidos ou anúncios com problemas.

##### Acceptance Criteria

```gherkin
@US-011-S01 @happy
Scenario: Despublicar um anúncio
  Given o anúncio "Honda Civic 2018" está "Publicado"
  When eu clico em "Despublicar" e confirmo
  Then a situação passa a "Rascunho"
  When um visitante procura por "Honda Civic 2018" na busca
  Then o anúncio não aparece nos resultados

@US-011-S02 @happy
Scenario: Arquivar um anúncio publicado
  Given o anúncio "Honda Civic 2018" está "Publicado"
  When eu clico em "Arquivar" e confirmo o aviso "O anúncio sairá do site e não poderá ser reativado"
  Then a situação passa a "Arquivado"
  And o anúncio aparece na lista do painel quando eu filtro pela situação "Arquivado"
  When um visitante abre o endereço antigo do anúncio
  Then ele vê a mensagem "Este anúncio não está mais disponível"

@US-011-S03 @edge
Scenario: Cancelar a confirmação
  Given estou vendo o aviso de confirmação de "Arquivar" em um anúncio publicado
  When eu clico em "Cancelar"
  Then a situação continua "Publicado"
  And o anúncio continua aparecendo no site

@US-011-S04 @edge
Scenario: Anúncio arquivado some dos favoritos do visitante
  Given um visitante favoritou o anúncio "Honda Civic 2018" e eu o arquivei
  When esse visitante abre "Meus favoritos"
  Then o anúncio não aparece na lista
  And ele vê o aviso de que 1 anúncio deixou de estar disponível

@US-011-S05 @edge
Scenario: Arquivar um anúncio que ainda não foi publicado
  Given o anúncio "Moto para retirar peças" está "Rejeitado"
  When eu clico em "Arquivar" e confirmo
  Then a situação passa a "Arquivado"
  And o anúncio deixa de aparecer na lista padrão do painel

@US-011-S06 @negative
Scenario: Anúncio arquivado não tem ações de retirada
  Given o anúncio "Honda Civic 2018" está "Arquivado"
  When eu abro esse anúncio no painel
  Then vejo a situação "Arquivado"
  And não vejo os botões "Despublicar" nem "Arquivar"

@US-011-S07 @negative
Scenario: Redator não vê as ações de retirada
  Given estou logado como Redator e tenho um anúncio "Publicado"
  When eu abro esse anúncio no painel
  Then não vejo os botões "Despublicar" nem "Arquivar"
```

##### Business Rules
- **Despublicar** vale só para anúncio *Publicado*: ele sai do site na hora e volta a *Rascunho*, podendo ser editado e enviado de novo para revisão.
- **Arquivar** encerra o anúncio (por exemplo, quando o bem foi vendido) e vale para qualquer anúncio que não esteja *Arquivado*. **Na v1 não há reativação** (ver S5).
- O sistema **não registra** se o anúncio foi vendido nem por quanto; arquivar é apenas retirar do ar.
- Arquivar pede confirmação porque não pode ser desfeito.

##### UI/UX Notes
- Wireframe: `specs/wireframes/screens/US-011-arquivar-despublicar.md`
- Estados a desenhar: diálogo de confirmação de cada ação e situação arquivada somente leitura.

##### Dependencies
- Requires: US-010
- Blocks: —

#### US-012: Acompanhar os anúncios no painel — Must

**As a** membro da equipe,
**I want to** ver a lista dos anúncios com sua situação e poder filtrá-los,
**So that** eu saiba o que está em rascunho, em revisão, publicado, rejeitado ou arquivado e retome meu trabalho.

##### Acceptance Criteria

```gherkin
@US-012-S01 @happy
Scenario: Redator vê apenas os próprios anúncios
  Given sou o Redator Ana com 2 rascunhos, 1 "Em revisão", 1 "Rejeitado" e 1 "Publicado", e outro Redator tem 3 anúncios
  When eu abro "Meus anúncios"
  Then vejo exatamente os meus 5 anúncios
  And cada linha mostra título, categoria, situação e data da última alteração
  And não vejo os anúncios do outro Redator

@US-012-S02 @happy
Scenario: Administrador vê todos os anúncios com o autor
  Given existem anúncios de vários redatores em situações diferentes
  When eu abro "Anúncios" como Administrador
  Then vejo os anúncios de todos os redatores
  And cada linha mostra o nome do autor

@US-012-S03 @happy
Scenario: Filtrar por situação
  Given a lista tem anúncios em várias situações
  When eu escolho o filtro de situação "Rejeitado"
  Then vejo somente anúncios "Rejeitado"
  And vejo o total de anúncios dessa situação

@US-012-S04 @happy
Scenario: Buscar um anúncio pelo título no painel
  Given a lista tem os anúncios "Honda Civic 2018" e "Casa com quintal"
  When eu digito "civic" no campo de busca do painel
  Then vejo apenas "Honda Civic 2018"

@US-012-S05 @happy
Scenario: Abrir um anúncio da lista
  Given vejo na lista um anúncio "Rascunho" e um "Em revisão"
  When eu clico no "Rascunho"
  Then abre o formulário de edição desse anúncio
  When eu volto e clico no "Em revisão"
  Then abre a visualização somente leitura desse anúncio

@US-012-S06 @edge
Scenario: Redator ainda sem anúncios
  Given sou um Redator que ainda não criou nenhum anúncio
  When eu abro "Meus anúncios"
  Then vejo a mensagem "Você ainda não criou anúncios"
  And vejo o botão "Novo anúncio"

@US-012-S07 @edge
Scenario: Lista com mais de 20 anúncios
  Given a minha lista tem 45 anúncios
  When eu abro a lista
  Then vejo 20 anúncios e o controle de páginas com 3 páginas
  When eu clico na página 3
  Then vejo os 5 anúncios restantes

@US-012-S08 @negative
Scenario: Falha ao carregar a lista
  Given o painel está com uma falha temporária
  When eu abro a lista de anúncios
  Then vejo a mensagem "Não foi possível carregar os anúncios. Tente novamente."
  And vejo o botão "Tentar novamente"
```

##### Business Rules
- O Redator só enxerga os **próprios** anúncios, em qualquer situação; o Administrador enxerga **todos**.
- Por padrão a lista **não mostra os anúncios arquivados**; eles aparecem ao filtrar pela situação "Arquivado".
- 20 anúncios por página no painel, por ser uma lista de trabalho mais densa do que a do site público.
- A "Fila de revisão" (US-010) é a mesma lista já filtrada por "Em revisão", com ordem do mais antigo ao mais novo.

##### UI/UX Notes
- Wireframe: `specs/wireframes/screens/US-012-painel-anuncios.md`
- Estados a desenhar: com itens, vazio, sem resultado do filtro, carregando e erro.

##### Dependencies
- Requires: US-006, US-008
- Blocks: —

### Epic: Administração do site

#### US-013: Gerenciar categorias — Must

**As a** administrador,
**I want to** criar, renomear, ordenar e excluir categorias e subcategorias,
**So that** o site tenha a organização que o negócio precisa.

##### Acceptance Criteria

```gherkin
@US-013-S01 @happy
Scenario: Criar uma subcategoria
  Given estou logado como Administrador na página "Categorias"
  When eu clico em "Nova categoria", escrevo o nome "Quadriciclos", escolho como categoria pai "Automóveis, Peças e Acessórios" e clico em "Salvar"
  Then "Quadriciclos" aparece na lista abaixo de "Automóveis, Peças e Acessórios"
  When um visitante abre a página "Automóveis, Peças e Acessórios"
  Then ele vê "Quadriciclos" entre as subcategorias

@US-013-S02 @happy
Scenario: Criar uma categoria principal
  Given estou na página "Categorias"
  When eu clico em "Nova categoria", escrevo o nome "Colecionáveis", deixo a categoria pai vazia e clico em "Salvar"
  Then "Colecionáveis" aparece na lista como categoria principal
  And a página inicial do site mostra "Colecionáveis" entre as categorias

@US-013-S03 @happy
Scenario: Renomear uma categoria
  Given a subcategoria "Carros, vans e utilitários" tem 4 anúncios publicados
  When eu a renomeio para "Veículos de passeio" e clico em "Salvar"
  Then a lista mostra "Veículos de passeio" no lugar de "Carros, vans e utilitários"
  And os 4 anúncios continuam publicados e aparecem em "Veículos de passeio"

@US-013-S04 @happy
Scenario: Mudar a ordem das categorias
  Given a lista mostra "Imóveis" antes de "Automóveis, Peças e Acessórios"
  When eu clico em "Mover para cima" em "Automóveis, Peças e Acessórios"
  Then a lista mostra "Automóveis, Peças e Acessórios" antes de "Imóveis"
  And a página inicial do site mostra as categorias nessa nova ordem

@US-013-S05 @happy
Scenario: Excluir uma categoria vazia
  Given a categoria "Colecionáveis" não tem subcategorias nem anúncios
  When eu clico em "Excluir" e confirmo
  Then "Colecionáveis" some da lista de categorias
  And a página inicial do site deixa de mostrar "Colecionáveis"

@US-013-S06 @negative
Scenario: Nome de categoria repetido
  Given já existe a subcategoria "Motos" em "Automóveis, Peças e Acessórios"
  When eu crio outra subcategoria "Motos" em "Automóveis, Peças e Acessórios"
  Then vejo a mensagem "Já existe uma categoria com esse nome neste grupo"
  And a nova categoria não é criada

@US-013-S07 @negative
Scenario: Nome de categoria vazio
  Given estou criando uma categoria
  When eu deixo o nome vazio e clico em "Salvar"
  Then vejo a mensagem "Informe o nome da categoria"
  And a categoria não é criada

@US-013-S08 @negative
Scenario: Excluir categoria que tem anúncios
  Given a categoria "Motos" tem 3 anúncios em situações variadas
  When eu clico em "Excluir" e confirmo
  Then vejo a mensagem "Não é possível excluir: 3 anúncios usam esta categoria"
  And a categoria continua na lista

@US-013-S09 @negative
Scenario: Excluir categoria que tem subcategorias
  Given a categoria "Imóveis" tem subcategorias
  When eu clico em "Excluir" e confirmo
  Then vejo a mensagem "Exclua ou mova antes as subcategorias desta categoria"
  And a categoria continua na lista

@US-013-S10 @negative
Scenario: Categorias com características específicas não podem ser excluídas
  Given "Automóveis, Peças e Acessórios" e "Terrenos, sítios e fazendas" têm campos específicos de anúncio
  When eu clico em "Excluir" em "Terrenos, sítios e fazendas"
  Then vejo a mensagem "Esta categoria é usada pelos campos específicos e não pode ser excluída. Você pode renomeá-la."
  And a categoria continua na lista

@US-013-S11 @edge
Scenario: Categorias têm até três níveis
  Given estou criando uma nova categoria
  When eu escrevo o nome "Peças para quadriciclos", escolho como categoria pai "Autopeças" (subcategoria de "Automóveis, Peças e Acessórios") e clico em "Salvar"
  Then "Peças para quadriciclos" aparece na lista abaixo de "Autopeças", no terceiro nível
  When eu abro de novo a lista de "Categoria pai"
  Then a lista mostra categorias principais e subcategorias, mas não as do terceiro nível
  And não posso criar uma categoria dentro de "Peças para carros, vans e utilitários"
```

##### Business Rules
- **Até três níveis:** categoria principal > subcategoria > subcategoria de terceiro nível (decisão do Product Owner em 2026-09-30, conforme `specs/categories.md`; por exemplo, "Automóveis, Peças e Acessórios" > "Autopeças" > "Peças para carros, vans e utilitários"). Uma categoria de terceiro nível não pode ter subcategorias.
- Nome único dentro do mesmo grupo (mesmo pai).
- **Excluir** só é permitido para categoria **sem subcategorias e sem anúncios** (em qualquer situação); para retirar uma categoria em uso o Administrador antes muda os anúncios de categoria (ver S14).
- As categorias com campos específicos (Automóveis, Peças e Acessórios e suas subcategorias; Terrenos, sítios e fazendas) podem ser renomeadas, mas **não excluídas** na v1. Toda subcategoria nova de Automóveis, Peças e Acessórios herda os campos de veículo (ver S4).
- A árvore inicial proposta está no Apêndice e é uma **suposição** (S3).

##### UI/UX Notes
- Wireframe: `specs/wireframes/screens/US-013-categorias.md`
- Estados a desenhar: árvore com itens, formulário de criação e edição, confirmação de exclusão e mensagens de bloqueio.
- Mover categorias deve funcionar por botões (sem exigir arrastar), para acessibilidade.

##### Dependencies
- Requires: US-006
- Blocks: US-001, US-002, US-008

#### US-014: Gerenciar usuários da equipe — Must

**As a** administrador,
**I want to** criar contas para a equipe, mudar o papel de cada pessoa, desativar contas e redefinir senhas,
**So that** só as pessoas certas acessem o painel e cada uma tenha o papel adequado.

##### Acceptance Criteria

```gherkin
@US-014-S01 @happy
Scenario: Criar uma conta de Redator
  Given estou logado como Administrador na página "Usuários"
  When eu clico em "Novo usuário", informo nome "Ana Souza", e-mail "ana.souza@exemplo.com.br", papel "Redator" e uma senha provisória válida e clico em "Salvar"
  Then "Ana Souza" aparece na lista com o papel "Redator" e a situação "Ativa"
  When Ana entra com o e-mail e a senha provisória
  Then ela é levada a definir uma nova senha antes de ver o painel

@US-014-S02 @happy
Scenario: Mudar o papel de um usuário
  Given "Ana Souza" é Redatora
  When eu altero o papel dela para "Administrador" e clico em "Salvar"
  Then a lista mostra "Ana Souza" como "Administrador"
  When Ana entra no painel na próxima vez
  Then ela vê os menus Categorias, Usuários e Configurações

@US-014-S03 @happy
Scenario: Desativar uma conta
  Given "Ana Souza" está com a situação "Ativa" e é autora de 3 anúncios
  When eu clico em "Desativar" e confirmo
  Then a lista mostra "Ana Souza" como "Desativada"
  And os 3 anúncios continuam existindo com o nome "Ana Souza" como autora
  When Ana tenta entrar com seus dados corretos
  Then ela vê a mensagem "E-mail ou senha inválidos, ou conta desativada"

@US-014-S04 @happy
Scenario: Reativar uma conta
  Given "Ana Souza" está com a situação "Desativada"
  When eu clico em "Reativar"
  Then a lista mostra "Ana Souza" como "Ativa"
  And Ana consegue entrar com seus dados

@US-014-S05 @negative
Scenario: E-mail já cadastrado
  Given já existe um usuário com o e-mail "ana.souza@exemplo.com.br"
  When eu tento criar outro usuário com o mesmo e-mail
  Then vejo a mensagem "Já existe um usuário com este e-mail"
  And o usuário não é criado

@US-014-S06 @negative
Scenario: Senha provisória fraca
  Given estou criando um usuário
  When eu informo a senha provisória "12345" e clico em "Salvar"
  Then vejo a lista de requisitos que faltam (mínimo de 8 caracteres, maiúscula, minúscula e símbolo)
  And o usuário não é criado

@US-014-S07 @negative
Scenario: E-mail em formato inválido
  Given estou criando um usuário
  When eu informo o e-mail "ana.souza" e clico em "Salvar"
  Then vejo a mensagem "Informe um e-mail válido"
  And o usuário não é criado

@US-014-S08 @negative
Scenario: Desativar a própria conta
  Given estou logado como Administrador
  When eu tento desativar a minha própria conta
  Then vejo a mensagem "Você não pode desativar a sua própria conta"
  And minha conta continua "Ativa"

@US-014-S09 @negative
Scenario: Remover o último administrador
  Given existe apenas 1 Administrador ativo, que sou eu
  When eu tento mudar meu papel para "Redator"
  Then vejo a mensagem "Deve existir ao menos um administrador ativo"
  And meu papel continua "Administrador"

@US-014-S10 @happy
Scenario: Redefinir a senha de alguém da equipe
  Given "Ana Souza" é Redatora ativa e não conseguiu recuperar a senha por e-mail
  When eu clico em "Redefinir senha" na linha de "Ana Souza", informo a senha provisória "Provis0ria!" e confirmo
  Then vejo a mensagem "Senha de Ana Souza redefinida. Informe a senha provisória a ela fora do sistema."
  And a senha anterior de "Ana Souza" deixa de funcionar e as sessões abertas dela são encerradas
  When "Ana Souza" entra com a senha provisória
  Then ela é levada a "Defina sua nova senha" antes de acessar o painel
```

##### Business Rules
- Só o Administrador cria, altera papel, desativa e reativa contas. **Não há exclusão de conta**, para que os anúncios mantenham o autor.
- E-mail único por conta.
- A conta nasce com **senha provisória** definida pelo Administrador e informada à pessoa fora do sistema; a pessoa é obrigada a trocá-la no primeiro acesso (US-006). Alternativa a confirmar: convite por e-mail (S6).
- Papéis existentes: Redator e Administrador.
- Sempre deve haver ao menos 1 Administrador ativo.
- **Redefinir senha:** o Administrador define uma senha provisória para outra pessoa da equipe (mesma política de senha da criação de conta) e a informa fora do sistema; a pessoa é obrigada a trocá-la no próximo acesso (US-006), a senha anterior deixa de valer e as sessões abertas dela são encerradas. O sistema registra quem redefiniu e quando. Serve de alternativa quando o e-mail de recuperação (US-007) não chega. Para a própria senha, o Administrador usa a recuperação normal.
- Dados pessoais da conta: nome, e-mail e senha guardada de forma irreversível (hash); nada além disso.

##### UI/UX Notes
- Wireframe: `specs/wireframes/screens/US-014-usuarios-equipe.md`
- Estados a desenhar: lista, formulário de criação e edição, confirmação de desativação, redefinição de senha e mensagens de erro.

##### Dependencies
- Requires: US-006
- Blocks: US-008, US-010

#### US-015: Configurar o telefone/WhatsApp do site — Must

**As a** administrador,
**I want to** definir o número de telefone/WhatsApp do intermediário exibido nos anúncios,
**So that** os visitantes falem sempre com o número correto, mesmo quando ele mudar.

##### Acceptance Criteria

```gherkin
@US-015-S01 @happy
Scenario: Definir o telefone/WhatsApp do site
  Given estou logado como Administrador na página "Configurações"
  When eu digito "(11) 91234-5678" no campo "Telefone/WhatsApp do site" e clico em "Salvar"
  Then vejo a mensagem "Configurações salvas"
  When um visitante abre a página de qualquer anúncio publicado
  Then ele vê o número (11) 91234-5678
  And o botão "Chamar no WhatsApp" abre uma conversa com esse número

@US-015-S02 @happy
Scenario: Trocar o número em uso
  Given o número (11) 91234-5678 aparece nos anúncios publicados
  When eu altero o número para "(21) 98765-4321" e clico em "Salvar"
  Then todos os anúncios publicados passam a mostrar (21) 98765-4321 sem precisar ser editados
  And os botões "Ligar" e "Chamar no WhatsApp" passam a usar o novo número

@US-015-S03 @edge
Scenario: Número digitado sem formatação
  Given estou na página "Configurações"
  When eu digito "11912345678" e clico em "Salvar"
  Then o número é salvo
  And os anúncios o exibem formatado como (11) 91234-5678

@US-015-S04 @negative
Scenario: Número inválido
  Given o número atual do site é (11) 91234-5678
  When eu digito "abc123" e clico em "Salvar"
  Then vejo a mensagem "Informe um número com DDD, por exemplo (11) 91234-5678"
  And o número em uso continua (11) 91234-5678

@US-015-S05 @negative
Scenario: Número vazio
  Given estou na página "Configurações"
  When eu apago o número e clico em "Salvar"
  Then vejo a mensagem "O telefone é obrigatório"
  And o número em uso não muda

@US-015-S06 @negative
Scenario: Redator não acessa as configurações
  Given estou logado como Redator
  When eu abro diretamente o endereço da página "Configurações"
  Then vejo a mensagem "Você não tem permissão para acessar esta página"
  And não vejo o campo do telefone
```

##### Business Rules
- Existe **um único número** para todo o site, usado para ligação e WhatsApp (ver S2). Aceita números brasileiros com DDD, de 10 ou 11 dígitos.
- O número é sempre exibido formatado, e vale para todos os anúncios ao mesmo tempo.
- Enquanto não houver número configurado, o Administrador não consegue publicar anúncios (US-010).
- Somente o Administrador altera.

##### UI/UX Notes
- Wireframe: `specs/wireframes/screens/US-015-contato-do-site.md`
- Estados a desenhar: padrão, número não configurado, sucesso e erro de validação.

##### Dependencies
- Requires: US-006
- Blocks: US-004, US-010

## Non-Functional Requirements

> Metas com número são **propostas de partida**; a origem de cada uma está na coluna "Target / Threshold". Metas propostas por esta especificação (e não confirmadas pelo Product Owner) estão listadas em S16. As regras de segurança seguem `rules/security.md`.

| ID | Category | Requirement (measurable) | Target / Threshold |
|----|----------|--------------------------|--------------------|
| NFR-01 | Desempenho | Tempo até o maior elemento visível da página (LCP) nas páginas públicas (início, categoria, busca, detalhe) | Menor que 2,5 s, medido em teste com perfil de celular médio e rede 4G. Base: faixa "boa" das métricas Core Web Vitals |
| NFR-02 | Desempenho | Resposta a cliques e toques (INP) nas páginas públicas | Menor que 200 ms. Base: faixa "boa" das Core Web Vitals |
| NFR-03 | Desempenho | Estabilidade visual (CLS): elementos não pulam enquanto as fotos carregam | Menor que 0,1. Base: faixa "boa" das Core Web Vitals |
| NFR-04 | Escalabilidade | Volume da v1 e tempo de resposta do servidor nas páginas de busca e detalhe | Suportar ~200 anúncios ativos e ~1.000 visitas por dia com tempo de resposta do servidor abaixo de 500 ms em 95% das requisições. Base: deixa folga para o LCP de 2,5 s |
| NFR-05 | Desempenho | Peso das imagens: a lista mostra versões reduzidas; no detalhe, as fotos além da primeira só são carregadas quando o visitante as pede | Primeira carga da lista de 24 anúncios até 2 MB e do detalhe até 3 MB, antes de qualquer interação. Meta inicial, a calibrar após medição (ver NFR-23) |
| NFR-06 | Segurança | Limite de tentativas no login da equipe | Máximo de 5 falhas em 15 minutos por origem; a partir daí, bloqueio até o fim do período |
| NFR-07 | Segurança | Política de senha da equipe e guarda da senha | Mínimo 8 caracteres com maiúscula, minúscula, número e símbolo; senha guardada só de forma irreversível (hash), nunca em texto |
| NFR-08 | Segurança | Duração da sessão da equipe | Encerra após 30 minutos sem uso. Base: protege computadores compartilhados sem atrapalhar o cadastro; valor proposto (S16) |
| NFR-09 | Segurança | Link de redefinição de senha | Vale 1 hora e pode ser usado 1 vez |
| NFR-10 | Segurança | Cabeçalhos de segurança HTTP em todas as respostas do site | Presentes: X-Content-Type-Options, X-Frame-Options, Referrer-Policy, HSTS (em produção, com HTTPS obrigatório) e CSP (Content-Security-Policy) |
| NFR-11 | Segurança | Proteção contra envio forjado (antiforgery) | 100% dos formulários e ações do painel que alteram dados exigem o token antiforgery |
| NFR-12 | Segurança | Envio de fotos | Aceitar só JPG, PNG, WebP, GIF e HEIC verificados pelo conteúdo do arquivo (não só pela extensão), até 10 MB por foto e no máximo 20 fotos por anúncio (Serviços: 6); converter HEIC para WebP ou JPG no envio; rejeitar o resto com mensagem clara. A regra de "ao menos 1 foto" não se aplica a Vagas de emprego, que não tem campo Fotos |
| NFR-13 | Segurança | Controle de acesso aplicado no servidor | Toda página e ação do painel exige login e o papel adequado; acesso indevido mostra "Você não tem permissão"; o Redator nunca acessa anúncio de outro Redator |
| NFR-14 | Segurança | Segredos (senhas de banco, chaves, credenciais de e-mail) | Nenhum segredo no código-fonte nem no repositório |
| NFR-15 | Segurança | Textos digitados (título, descrição, nomes, motivos) | Sempre exibidos como texto puro; nenhum código digitado é executado pelo navegador de quem vê |
| NFR-16 | Acessibilidade | Conformidade com WCAG 2.1 nível AA (padrão internacional de acessibilidade web) em toda tela do site e do painel | 0 falhas de nível A ou AA nas verificações automáticas e nos testes manuais por teclado e leitor de tela, por tela |
| NFR-17 | Responsividade | Uso confortável em celular e computador, projetado primeiro para o celular | Sem rolagem horizontal em larguras de 320, 768, 1024 e 1280 px, verificado tela a tela |
| NFR-18 | Observabilidade | Registros (logs) estruturados, com identificador de correlação em cada requisição e senhas, tokens e e-mails ocultados | Toda requisição tem identificador de correlação; nenhuma senha, token nem e-mail aparece em texto claro nos logs; o código de referência mostrado nas telas de erro permite achar o registro |
| NFR-19 | Privacidade (LGPD) | Dados pessoais tratados pelo sistema | Apenas nome, e-mail e senha (hash) das contas da equipe; nenhum dado pessoal de vendedor, comprador ou interessado |
| NFR-20 | Localização | Idioma e formatos | Interface em português do Brasil; preços em reais (R$); datas no formato dd/mm/aaaa; abrangência nacional |
| NFR-21 | SEO | Páginas públicas encontráveis em buscadores (condicional à confirmação de S7) | Início, categorias e cada anúncio publicado com título e descrição próprios, endereço legível e mapa do site; anúncio arquivado deixa de ser indexado |
| NFR-22 | Arquitetura | Alinhamento com a pilha tecnológica aprovada e contratos de API | Segue `rules/tech-stack.md` (ASP.NET Core 10, SQL Server e EF Core, telas Razor com Bootstrap 5.3.8); endpoints JSON, se houver, usam erro no formato ProblemDetails e listas paginadas no envelope PagedResult |
| NFR-23 | Infraestrutura | Componentes de escala (cache Redis, fila Kafka, réplica de leitura do banco, CDN) | **Ainda não**: escala pequena não justifica. Reavaliar se o LCP p75 passar de 2,5 s por 7 dias seguidos mesmo com imagens reduzidas (avaliar CDN de imagens), ou se as visitas superarem 5.000 por dia ou os anúncios ativos passarem de 1.000 (avaliar cache/Redis). Base: 5 vezes o volume-alvo; fotos de até 20 por anúncio pesam a página e devem ser medidas |
| NFR-24 | Resiliência | Consulta de CEP (feita pelo servidor, por exemplo no ViaCEP) | Tempo limite de 5 s por tentativa; 1 nova tentativa só em falha do serviço (sem resposta, erro 5xx, sem rede); depois disso, preenchimento manual (US-008-S14). "CEP não encontrado" não conta como falha. CEPs já encontrados ficam em cache no servidor, com validade sugerida de 30 dias (valor final em `/arch`). Espera máxima percebida: cerca de 10 s |

## Boundaries

### Always Do
- Aplicar a **pré-moderação**: nenhum anúncio aparece ao público sem aprovação de um Administrador.
- Mostrar nos anúncios **somente** o contato do intermediário configurado; nunca dados pessoais do vendedor.
- Exigir login e papel adequado para toda a área da equipe, verificado no servidor.
- Validar tipo, tamanho e quantidade de fotos no envio (NFR-12).
- Usar token antiforgery em toda operação que altera dados (NFR-11).
- Enviar os cabeçalhos de segurança HTTP em todas as respostas (NFR-10).
- Remover dos arquivos de foto publicados os dados de localização (GPS) embutidos pela câmera, para não expor onde o bem ou o vendedor está (proposta, ver S18).
- Mostrar ao Redator o motivo de qualquer rejeição.
- Cumprir WCAG 2.1 AA e o layout sem rolagem horizontal a partir de 320 px em toda tela nova.

### Ask First
- Fotos que mostrem placas de veículos ou pessoas: decidir se a equipe deve cobrir/borrar antes de publicar (LGPD, ver S18).
- Incluir ferramentas de medição de acesso (analytics) ou qualquer cookie não essencial, porque isso exige aviso de cookies aos visitantes (S8).
- Guardar no sistema qualquer dado do vendedor, do valor combinado, da comissão ou de interessados: mudaria o escopo e criaria obrigações de LGPD.
- Permitir que o Administrador reative um anúncio arquivado (S5).
- Trocar a árvore de categorias inicial ou incluir novos campos específicos por categoria (S3 e S4).
- Adotar componentes de escala (Redis, Kafka, réplica de leitura, CDN) antes de o gatilho do NFR-23 ser atingido.
- Enviar e-mails do sistema (recuperação de senha), pois exige contratar ou configurar um serviço de envio (S6).

### Never Do
- Processar pagamento, carrinho, pedido ou qualquer transação financeira no sistema.
- Criar contas para o público nem exigir login do visitante.
- Mostrar o telefone pessoal ou o nome do vendedor no anúncio.
- Guardar senhas em texto legível ou mostrar senhas, tokens ou e-mails em logs.
- Publicar automaticamente um anúncio sem a aprovação de um Administrador.
- Revelar, na tela de login ou de recuperação de senha, se um e-mail está ou não cadastrado.
- Enviar dados de visitantes a serviços de terceiros sem aviso e decisão do Product Owner.

## Out of Scope (Won't)

Não serão feitos na v1 (decisões do Product Owner):

- Pagamento online.
- Carrinho e pedido de compra.
- Mensagens internas entre comprador e vendedor (o contato é por telefone/WhatsApp fora do sistema).
- Contas do público (cadastro e login de visitantes).
- Registro, no sistema, de vendedor, valor combinado, comissão, interessados ou situação de venda (o controle da intermediação e da comissão é feito fora do sistema).
- Avaliações ou comentários sobre anúncios ou vendedores.
- Anúncio de animais vivos (categorias Cachorros, Gatos, Roedores, Outros animais e Animais para agropecuária); "Acessórios para pets" continua.
- Vídeo do YouTube no anúncio.

## Open Questions & Decisions

> Cada linha abaixo está **Confirmada** (com data) ou **Aberta** (com dono e próximo passo). As suposições S1 a S9 foram propostas pelo analista e **ainda não foram confirmadas** pelo Product Owner; as demais (S10 em diante) surgiram durante a escrita desta especificação.

### Resolved (2026-09-30)

| ID | Question | Decision | Status |
|----|----------|----------|--------|
| D-01 | Como o negócio funciona? | Site de classificados curado e intermediado: a Gazeta capta o vendedor, a equipe cadastra usando o telefone/WhatsApp do intermediário, o visitante fala direto com o intermediário | Confirmed 2026-09-30 |
| D-02 | Como é a receita? | Comissão sobre a venda, paga pelo vendedor, fora do sistema | Confirmed 2026-09-30 |
| D-03 | Quais são os papéis? | Visitante (sem conta), Redator e Administrador; só a equipe tem cadastro e login | Confirmed 2026-09-30 |
| D-04 | Como é a moderação? | Pré-moderação: nada aparece ao público sem aprovação do Administrador | Confirmed 2026-09-30 |
| D-05 | Quais funcionalidades entram no MVP? | Cadastro e login (só da equipe), anúncio com até 20 fotos, busca com filtros e categorias, favoritos, contato por exibição do telefone/WhatsApp | Confirmed 2026-09-30 |
| D-06 | O que fica de fora do MVP? | Carrinho, pedido, pagamento online, mensagens internas, contas do público, situação de venda/comissão/interessados no sistema | Confirmed 2026-09-30 |
| D-07 | Estilo das categorias? | Amplas, no estilo OLX, conforme `specs/categories.md`: categoria > subcategoria, com um terceiro nível em "Automóveis, Peças e Acessórios › Autopeças" (ver S3) | Confirmed 2026-09-30 (atualizado em 2026-09-30: árvore definitiva com até 3 níveis) |
| D-08 | Abrangência e escala? | Nacional (filtro por UF e cidade); escala pequena: cerca de 200 anúncios ativos e 1.000 visitas por dia | Confirmed 2026-09-30 |
| D-09 | O sistema guarda dados do vendedor? | Não: nome e telefone do dono do bem não são armazenados | Confirmed 2026-09-30 |
| A1 | Quais campos têm Serviços e Vagas de emprego? | Decidido pelo Product Owner: Serviços (Título, Fotos, Tipo, Informações adicionais, CEP; sem Preço) e Vagas de emprego (Título, Área, Informações adicionais, CEP, Preço exibido como "Salário"; sem Fotos), detalhados no Apêndice B | Resolved 2026-09-30 |

### Open

| ID | Question | Decision / Assumption | Status (Confirmed date / Open + owner) |
|----|----------|-----------------------|----------------------------------------|
| S1 | Onde ficam os favoritos, se o visitante não tem conta? | Assumido: no navegador do visitante, sem sincronizar entre aparelhos (US-005). Alternativa: tirar Favoritos do MVP | Open — dono: Product Owner; próximo passo: confirmar antes de `/arch` |
| S2 | Qual telefone é exibido e quem o vê? | Assumido: um único número, configurável pelo Administrador, para ligação e WhatsApp, visível a qualquer visitante, com mensagem de WhatsApp pré-preenchida citando o anúncio (US-004, US-015). Assumido também: sem número configurado, não se publica | Open — dono: Product Owner; próximo passo: confirmar |
| S3 | Qual é a árvore de categorias inicial? | Decidido pelo Product Owner: a árvore de `specs/categories.md` (152 categorias, até 3 níveis). Na v1 ficam **124 categorias ativas**: as 129 com IsPostable = 1, menos as 5 de animais vivos (ver Out of Scope). Descoberta em `specs/discovery/categories-active.md` e `consolidation.md` | Resolved (2026-09-30) |
| S4 | Quais campos cada anúncio tem? | Decidido pelo Product Owner: campos comuns (com as exceções de Serviços e Vagas de emprego) mais os campos específicos de cada categoria no Apêndice B; origem em `specs/discovery/consolidation.md` | Resolved (2026-09-30) |
| S5 | Qual é o ciclo de vida do anúncio? | Assumido: Rascunho, Em revisão, depois Publicado ou Rejeitado (com motivo), e Arquivado. Acrescentado a partir do pedido "despublica/arquiva": Despublicar volta o anúncio a Rascunho. Vendido = arquivado, sem rastreio de venda. Arquivado é definitivo (sem reativação na v1) | Open — dono: Product Owner; próximo passo: confirmar, inclusive a ausência de reativação |
| S6 | Como a equipe recupera a senha e recebe a conta? | Assumido: contas criadas pelo Administrador com senha provisória trocada no primeiro acesso; recuperação por e-mail (US-007). Alternativa: o Administrador redefine a senha, sem envio de e-mail | Open — dono: Product Owner; próximo passo: decidir; depende do serviço de e-mail em `/arch` e `/infra` |
| S7 | O site deve ser encontrado em buscadores (SEO básico)? | Assumido: sim (NFR-21) | Open — dono: Product Owner; próximo passo: confirmar |
| S8 | Haverá ferramenta de medição de acesso (analytics)? Se sim, precisa de aviso de cookies | Assumido: aviso de cookies só se houver cookie não essencial; nenhum analytics previsto na v1 | Open — dono: Product Owner; próximo passo: decidir; depois `/secure` |
| S9 | A rejeição precisa de motivo visível ao Redator? | Assumido: sim, motivo obrigatório (US-010) | Open — dono: Product Owner; próximo passo: confirmar |
| S10 | Quais os limites do Redator e do Administrador sobre anúncios? | Assumido: o Redator vê e edita só os próprios anúncios em Rascunho ou Rejeitado; o Administrador edita qualquer um não arquivado e segue o mesmo caminho (enviar e publicar) nos anúncios que cria; o sistema guarda quem publicou ou rejeitou e quando; não há exclusão de rascunho (o Administrador pode arquivar) | Open — dono: Product Owner; próximo passo: confirmar |
| S11 | Quantos anúncios por página? Qual a ordenação padrão? | Assumido: 12 na página inicial e 24 na busca (dividem em linhas completas de 2, 3 e 4 colunas), 20 por página no painel; ordenação padrão "mais recentes", com opções de menor e maior preço | Open — dono: Product Owner; próximo passo: confirmar |
| S12 | Quais itens são obrigatórios para enviar à revisão? | Assumido: título, descrição, preço maior que zero, categoria, CEP válido, ao menos 1 foto e os campos obrigatórios da categoria (Apêndice B). Serviços não exige preço; **Vagas de emprego não exige fotos para enviar à revisão** | Open — dono: Product Owner; próximo passo: confirmar |
| S13 | O preço pode ser "a combinar"? Qual o tamanho máximo dos textos? | Assumido: preço obrigatório e maior que zero (sem "a combinar"); tamanhos máximos de título e descrição serão definidos em `/arch` | Open — dono: Product Owner; próximo passo: confirmar |
| S14 | O que acontece com anúncios ao mudar ou excluir uma categoria? | Assumido: só se exclui categoria vazia (sem subcategorias e sem anúncios); para esvaziá-la, o Administrador muda a categoria dos anúncios | Open — dono: Product Owner; próximo passo: confirmar |
| S15 | Cidade é digitada livremente ou escolhida de uma lista oficial? | Substituída em 2026-09-30: no cadastro do anúncio, Cidade e UF vêm do CEP e não são digitadas (ver S23 a S26); na busca pública, UF e cidade continuam escolhidas de listas | Open — dono: Product Owner; próximo passo: confirmar; fonte da lista em `/arch` |
| S16 | Valores propostos pelo analista, sem confirmação do Product Owner | Sessão de 30 min de inatividade; metas de peso de página (2 MB e 3 MB); gatilhos do NFR-23 (5.000 visitas por dia, 1.000 anúncios ativos, LCP acima de 2,5 s por 7 dias). **Fotos: confirmado pelo Product Owner em 2026-09-30** — até 10 MB; JPG, PNG, WebP, GIF e HEIC (HEIC convertido no envio) | Open — dono: Product Owner com o arquiteto; próximo passo: confirmar os demais valores em `/arch` |
| S17 | Como nasce o primeiro Administrador? | Assumido: criado na implantação, fora da interface do site | Open — dono: arquiteto; próximo passo: `/arch` e `/infra` |
| S18 | Como tratar dados de localização (GPS) das fotos e fotos com placas ou pessoas? | Proposta: remover o GPS das fotos publicadas; decisão sobre cobrir placas e rostos pendente | Open — dono: Product Owner; próximo passo: decidir; revisar em `/secure` |
| S19 | Qual a meta de disponibilidade do site e a política de backup e guarda das fotos? | Sem valor definido nesta versão | Open — dono: Product Owner; próximo passo: definir meta em `/arch` e `/infra` |
| S20 | O site terá página "Como funciona" e política de privacidade explicando a intermediação? | Não previsto nas histórias da v1 | Open — dono: Product Owner; próximo passo: decidir; se sim, nova história em nova versão |
| S21 | Qual a meta e como medir os objetivos G-01 e G-02, se o sistema não registra contatos nem vendas? | KPI, linha de base e prazo ficam `[NEEDS PO]` na tabela de metas | Open — dono: Product Owner; próximo passo: definir antes do lançamento |
| S22 | Descrição vazia impede o envio? | Assumido: descrição vazia ou só com espaços impede o envio à revisão, com a mensagem "Informe a descrição" junto ao campo e na lista de pendências; o rascunho continua podendo ser salvo sem descrição (US-008-S07) | Open — dono: Product Owner; próximo passo: confirmar e virar cenário `@US-009-S06 @negative` |
| S23 | O que acontece com um CEP incompleto? | Assumido: ao sair do campo com 1 a 7 dígitos, aparece "Informe um CEP com 8 dígitos" e Cidade/UF ficam vazias; o rascunho pode ser salvo assim, mas o envio à revisão é bloqueado | Open — dono: Product Owner; próximo passo: confirmar e virar cenário na US-008 |
| S24 | O que acontece com um CEP de 8 dígitos que não existe? | Assumido: aparece "CEP não encontrado. Confira os números.", Cidade/UF ficam vazias e o envio à revisão é bloqueado ("Informe um CEP válido"); durante a busca aparece "Buscando…" | Open — dono: Product Owner; próximo passo: confirmar e virar cenário na US-008 |
| S25 | O CEP aparece no site público? | Assumido: não. O CEP pode indicar a rua do dono do bem (NFR-19, LGPD); ele aparece só no painel da equipe, e o público vê apenas Cidade/UF | Open — dono: Product Owner; próximo passo: confirmar |
| S26 | O que fazer se o serviço de consulta de CEP estiver fora do ar? | Decidido pelo Product Owner: preenchimento manual de Cidade e UF em listas, com selo de conferência para o Administrador; tempo limite, nova tentativa e cache no NFR-24 | Resolved (2026-09-30) — ver US-008-S14 e NFR-24 |
| S27 | Preço vazio ou zero impede o envio? | Assumido: preço vazio ou "R$ 0,00" impede o envio à revisão, com a mensagem "Informe um preço maior que zero" junto ao campo e na lista de pendências; o rascunho continua podendo ser salvo sem preço (US-008-S07) | Open — dono: Product Owner; próximo passo: confirmar e virar cenário `@US-009-S07 @negative` |
| S28 | Como se digita o preço e qual o limite? | Confirmado pelo Product Owner em 2026-09-30: máscara estilo banco (só dígitos; os dois últimos são os centavos, por exemplo 6200000 → R$ 62.000,00), sem valor negativo, teto de R$ 99.999.999,99. Assumido: o preço é guardado em centavos, como número inteiro | Open — dono: arquiteto; próximo passo: confirmar o armazenamento em centavos em `/arch` |
| S29 | Como o preço aparece para o visitante? | Confirmado pelo Product Owner em 2026-09-30: centavos só quando não são zero ("R$ 62.000", "R$ 2.499,90") | Open — dono: Product Owner; próximo passo: virar regra de negócio da US-003 na aprovação do SPEC |
| A2 | Como cobrar comissão em Serviços, Vagas de emprego e aluguel de imóveis, se D-02 prevê comissão sobre **venda**? | Não decidido; o modelo de monetização dessas categorias fica para `/arch` | Open — dono: Product Owner; próximo passo: decidir em `/arch` e, se mudar D-02, atualizar Objective e Executive Summary |
| A3 | Como trazer o catálogo de veículos (marca, modelo, ano e versão de carros e motos) do banco do GazetaOnline, e como mantê-lo atualizado? | Decidido usar o catálogo do GazetaOnline; a forma de copiar e atualizar não está definida. A questão jurídica da origem do catálogo está na A5 | Open — dono: arquiteto; próximo passo: `/arch` |
| A4 | Como o card do anúncio e a página de detalhe mostram um anúncio sem foto (Vagas de emprego)? | Decidido no `/arch` (`architecture/design-system.md` §5.2 e §5.4): um bloco neutro "Vaga de emprego", do mesmo tamanho da foto, com a área da vaga quando houver, no card e no lugar da galeria | Resolved (2026-09-30) |
| A5 | O catálogo de veículos (marca, modelo, ano, versão) coletado da API interna da OLX pode ser usado? | Validação jurídica pendente. Antes do lançamento, avaliar a substituição por fonte licenciada (tabela FIPE oficial, API do DENATRAN ou catálogo próprio) | Open — dono: Product Owner + jurídico; próximo passo: parecer jurídico antes do lançamento |
| A6 | Como o card e a página de detalhe exibem Serviços (sem campo Preço)? O filtro "Menor preço" e a faixa de preço incluem ou ignoram Serviços? | Decidido no `/arch` (ADR-006 e `architecture/design-system.md` §5.3 e §6): o Tipo do serviço aparece no lugar do preço no card e no detalhe; com faixa de preço, Serviços não aparecem; nas ordenações por preço, vão para o fim. **Esta decisão prevalece sobre a nota "em aberto (A6)" das regras da US-002** | Resolved (2026-09-30) |
| A7 | Com a árvore de 3 níveis e os 18 grupos de campos do Apêndice B, três regras ficaram inconsistentes: (a) herança de campos em Automóveis; (b) bloqueio de exclusão de categorias com campos específicos; (c) filtros de marca, ano e km em Automóveis | Decidido pelo Product Owner em 2026-09-30 (`architecture/ARCHITECTURE.md` §6.3): (a) cada categoria usa o grupo de campos próprio ou o do ancestral mais próximo — "Autopeças" e suas filhas usam o grupo Peças; (b) o bloqueio de exclusão por campos específicos vale só para as categorias da carga inicial que definem o próprio grupo; sem subcategorias e sem anúncios continua valendo para todas; (c) os filtros específicos da busca vêm do grupo da categoria escolhida (marca, modelo, ano e km só nos grupos Carros e Motos; ano e km em Caminhões e Ônibus; área em Imóveis e Terrenos). **Esta decisão prevalece sobre o texto das regras de negócio da US-002 e da US-013** | Resolved (2026-09-30) |

## Glossary

| Term | Definition |
|------|------------|
| Sistema | O produto inteiro: o site público mais o painel da equipe |
| HEIC | Formato de foto usado por iPhones; nem todos os navegadores o exibem, por isso é convertido no envio |
| Site | A parte pública, aberta a qualquer visitante, sem login |
| Painel | A área interna, só para a equipe, acessada com e-mail e senha |
| Anúncio | Uma oferta de um bem à venda (por exemplo, um carro ou um lote), com título, descrição, preço, categoria, localização e fotos |
| Vendedor | Dono do bem que quer vendê-lo. Não aparece no sistema: seus dados não são guardados |
| Intermediário | A Gazeta, que negocia entre vendedor e comprador e cujo telefone/WhatsApp é exibido nos anúncios |
| Visitante | Qualquer pessoa que navega no site; não tem conta |
| Equipe | Conjunto de Redatores e Administradores, únicos com conta |
| Redator | Membro da equipe que cadastra anúncios e os envia para revisão |
| Administrador | Membro da equipe que aprova ou rejeita anúncios e gerencia categorias, usuários e configurações |
| Pré-moderação | Regra de que nenhum anúncio aparece ao público antes de aprovado por um Administrador |
| Rascunho | Situação de anúncio ainda em preparação, editável pelo autor |
| Em revisão | Situação de anúncio enviado e à espera da decisão de um Administrador |
| Publicado | Situação de anúncio aprovado e visível a todos |
| Rejeitado | Situação de anúncio recusado por um Administrador, com motivo, que o autor pode corrigir e reenviar |
| Arquivado | Situação final de anúncio retirado do site; não volta na v1 |
| Categoria / subcategoria | Grupo de anúncios em até três níveis (por exemplo, Automóveis, Peças e Acessórios > Motos, ou Automóveis, Peças e Acessórios > Autopeças > Peças para motos) |
| Ficha da categoria | Conjunto de campos extras de uma categoria (veículos: marca, modelo, ano, quilometragem; terrenos e lotes: área) |
| Favorito | Anúncio que o visitante marcou para ver depois; fica salvo no navegador dele |
| Comissão | Percentual da venda pago pelo vendedor à Gazeta, fora do sistema |
| UF | Sigla do estado brasileiro (por exemplo, SP) |
| CEP | Código de Endereçamento Postal, com 8 dígitos (00000-000); no cadastro do anúncio, dele vêm a Cidade e a UF |
| WhatsApp | Aplicativo de mensagens usado para o contato com o intermediário |
| WCAG 2.1 AA | Padrão internacional de acessibilidade web, nível AA |
| LGPD | Lei Geral de Proteção de Dados (lei brasileira de proteção de dados pessoais) |
| LCP, INP, CLS | Medidas de velocidade e estabilidade percebidas em uma página (Core Web Vitals): tempo até o maior elemento aparecer, tempo de resposta a cliques e quanto a página "pula" ao carregar |
| CSP, HSTS | Cabeçalhos de segurança do navegador: CSP limita o que a página pode carregar; HSTS obriga o uso de HTTPS |
| Antiforgery | Proteção que impede que outro site force uma ação em nome de quem está logado |
| Hash de senha | Forma irreversível de guardar a senha, sem que ninguém consiga lê-la depois |
| Identificador de correlação | Código único de cada requisição, que liga a tela de erro ao registro técnico |
| ProblemDetails, PagedResult | Formatos padronizados de resposta de erro e de listas paginadas em endpoints JSON |
| SEO | Conjunto de cuidados para o site ser encontrado em buscadores |
| Analytics | Ferramenta que mede acessos ao site |
| CDN | Rede que entrega arquivos (como fotos) a partir de servidores próximos ao visitante |
| Redis, Kafka, réplica de leitura | Componentes técnicos de escala (cache, fila de mensagens, cópia do banco só para leitura), adiados por ora |

## Appendix

### A. Situações do anúncio e quem as muda

| De | Para | Quem | Como |
|----|------|------|------|
| (novo) | Rascunho | Redator ou Administrador | Salvar rascunho (US-008) |
| Rascunho | Em revisão | Autor (Redator) ou Administrador | Enviar para revisão (US-009) |
| Rejeitado | Em revisão | Autor (Redator) ou Administrador | Enviar de novo, após corrigir (US-009) |
| Em revisão | Publicado | Administrador | Publicar (US-010) |
| Em revisão | Rejeitado | Administrador | Rejeitar com motivo (US-010) |
| Publicado | Rascunho | Administrador | Despublicar (US-011) |
| Rascunho, Em revisão, Rejeitado ou Publicado | Arquivado | Administrador | Arquivar (US-011) |

### B. Categorias e campos por categoria (S3 e S4)

**Árvore de categorias:** `specs/categories.md`. Na v1, **124 categorias ativas** (IsPostable = 1), sem as 5 de animais vivos. Levantamento completo em `specs/discovery/` (Etapas 1 a 6).

**Campos comuns:** Título, Descrição, Preço, Categoria, CEP (preenche Cidade e UF) e Fotos, com as exceções de Serviços e Vagas de emprego abaixo. ✅ = obrigatório para enviar à revisão.

| Grupo | Categorias | Campos específicos |
|---|---|---|
| Carros | 33 | Marca → Modelo → Ano → Versão ✅ (catálogo, A3) · Quilometragem ✅ · Câmbio · Portas · Combustível · Direção · Tipo · Potência · Cor · Opcionais · Informações adicionais do veículo |
| Motos | 36 | Marca → Modelo → Ano → Versão ✅ (catálogo) · Quilometragem ✅ · Cilindrada ✅ · Cor · Opcionais · Informações adicionais do veículo |
| Caminhões, Ônibus | 34, 35 | Ano do modelo ✅ · Quilometragem ✅ · Câmbio · Combustível · Direção · Tipo · Opcionais · Informações adicionais do veículo |
| Barcos e aeronaves | 37 | Ano do modelo ✅ · Horas de uso ✅ · Tipo ✅ · Combustível · Comprimento, Largura e Altura (m) · Informações adicionais |
| Peças | 38–42 | Condição ✅ · Tipo de peça · Cor |
| Imóveis | 26, 27, 30, 31 | Tipo ✅ · Vender ou alugar ✅ · Quartos ✅ (26, 27) · Banheiros (26, 27) · Área m² (✅ só em 30) · Vagas (26, 27, 31) · Condomínio · IPTU · Características · Características do condomínio (26, 27) |
| Aluguel de quartos | 28 | Características |
| Temporada | 29 | Tipo ✅ · Quartos ✅ · Acomoda quantas pessoas ✅ · Banheiros · Vagas · Forma de pagamento · Características |
| Celulares | 43 | Marca ✅ · Modelo ✅ · Condição ✅ · Armazenamento · Cor · Saúde da bateria |
| Smartwatches | 46 | Marca ✅ · Condição ✅ |
| Produtos de telefonia | 44, 45, 47, 48 | Tipo ✅ · Condição ✅ · Marcas compatíveis (44, 45) |
| Eletro | 128–134 | Tipo ✅ · Marca ✅ · Voltagem ✅ · Condição ✅ · Capacidade (128) |
| Eletrônicos e informática | 102–127 | Condição ✅ · Marca ✅ · Modelo |
| Roupas e calçados | 64, 65, 68, 69, 72, 75, 76, 77 | Condição ✅ · Tamanho ✅ · Gênero |
| Máquinas | 89, 92, 93, 97 | Condição ✅ · Marca · Ano de fabricação · Horas de uso |
| Serviços | 66 | Ver abaixo |
| Vagas de emprego | 96 | Ver abaixo |
| Produtos em geral | as demais categorias ativas | Condição ✅ · Tipo de produto |

As listas de opções das 29 categorias herdadas do GazetaOnline estão em `specs/discovery/gazetaonline-lookups.md`; as listas novas (Tamanho, Gênero, Marca por categoria de eletrônicos, Tipo de produto) serão definidas em `/arch`.

#### Serviços (CategoryId 66)

Exatamente estes campos, nesta ordem. **Não tem Preço** nem ficha específica.

| # | Campo | Tipo | Obrigatório | Regras |
|---|---|---|---|---|
| 1 | Título | Texto | ✅ | Até 120 caracteres |
| 2 | Fotos | Várias fotos | ✅ (ao menos 1) | Até **6** fotos; JPG, PNG, WebP, GIF ou HEIC; até 10 MB cada; a primeira é a capa |
| 3 | Tipo | Lista | ✅ | Serviços domésticos · Outros · Babá · Eventos / Festas · Reparação / Conserto / Reforma · Saúde / Beleza · Informática · Tradução · Transporte / Mudanças · Profissionais liberais · Turismo (nesta ordem) |
| 4 | Informações adicionais | Texto longo | ✅ | É o campo Descrição; até 6000 caracteres, contador "X/6000" |
| 5 | CEP | Texto com máscara 00000-000 | ✅ | Igual às demais categorias (Cidade e UF automáticas; preenchimento manual se o serviço falhar, US-008-S14) |

#### Vagas de emprego (CategoryId 96)

Exatamente estes campos, nesta ordem. **Não tem Fotos** nem ficha específica.

| # | Campo | Tipo | Obrigatório | Regras |
|---|---|---|---|---|
| 1 | Título | Texto | ✅ | Até **90** caracteres, contador "X/90" (digitados / máximo; sem mínimo). Texto de ajuda: "Sugerimos especificar a vaga com clareza. Ex.: Pizzaiolo com experiência, período integral" |
| 2 | Área | Várias opções (caixas de seleção) | — | Administrativo / Secretariado / Finanças · Comercial / Vendas · Engenharia / Arquitetura / Design · Telecomunicações / Informática / Multimídia · Atendimento ao Cliente / Call Center · Banco / Seguros / Consultoria / Jurídica · Logística / Distribuição · Turismo / Hotelaria / Restaurante · Educação / Formação · Marketing / Comunicação · Serviços Domésticos / Limpezas · Construção / Industrial · Saúde / Medicina / Enfermagem · Agricultura / Pecuária / Veterinária (14 opções, nesta ordem) |
| 3 | Informações adicionais | Texto longo | ✅ | É o campo Descrição; até 6000 caracteres, contador "X/6000". Texto de exemplo no campo: "Escreva aqui por que você está anunciando esta vaga e informações que podem ajudar no processo seletivo." Texto de ajuda: "Inclua detalhes sobre o cargo, responsabilidades, remuneração, localização, ambiente de trabalho, etc" |
| 4 | CEP | Texto com máscara 00000-000 | ✅ | Igual às demais categorias |
| 5 | Preço | Texto com máscara de preço | ✅ | Mesma máscara e regras das demais categorias (S27, S28); representa o salário oferecido e é **exibido como "Salário"** (US-003) |

### C. Convenções desta especificação

- Nos blocos de critérios de aceite, as palavras-chave `Scenario`, `Given`, `When`, `Then` e `And` ficam em inglês (padrão do kit e das ferramentas de teste); o texto de cada passo está em português.
- Cada cenário tem um identificador `@US-xxx-Snn` e uma classe: `@happy` (caminho principal), `@negative` (rejeição ou erro) ou `@edge` (limite ou variação).
- Os desenhos de tela (wireframes) ficam em `specs/wireframes/`, um arquivo por tela, ligando cada controle ao cenário correspondente.
