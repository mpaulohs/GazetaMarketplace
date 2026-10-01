# Fluxo: Visitante, da página inicial ao contato pelo WhatsApp — @US-001, @US-002, @US-003, @US-004

> **Em resumo:** este é o caminho principal do site. O visitante chega à página inicial, encontra o anúncio por categoria ou por busca e fala com a Gazeta pelo telefone ou pelo WhatsApp. O objetivo da SPEC é que isso leve **no máximo 3 cliques a partir da página inicial**: categoria, anúncio, botão de contato.

## Jornada (Mermaid)

```mermaid
flowchart TD
    Start(["Visitante abre o site"]) --> Home["Página inicial: busca, categorias e 12 anúncios recentes"]
    Home -->|"happy @US-001-S01"| Escolha{"Como o visitante procura?"}
    Escolha -->|"clica em uma categoria @US-001-S02"| Cat["Página da categoria: subcategorias e anúncios"]
    Escolha -->|"digita na busca e clica em Buscar @US-002-S01"| Busca["Resultados da busca: 24 por página"]
    Escolha -->|"clica em um anúncio recente"| Detalhe["Página do anúncio: fotos, descrição e bloco de contato @US-003-S01"]

    Cat -->|"escolhe subcategoria @US-001-S03"| Cat
    Cat -->|"categoria sem anúncios @US-001-S04"| VazioCat["Mensagem: Ainda não há anúncios nesta categoria"]
    VazioCat --> Home
    Cat -->|"clica em um anúncio"| Detalhe
    Cat -->|"quer filtrar e ordenar"| Busca

    Busca -->|"aplica filtros @US-002-S02"| Busca
    Busca -->|"nenhum resultado @US-002-S07"| SemRes["Mensagem: Nenhum anúncio encontrado, botão Limpar filtros"]
    SemRes -->|"Limpar filtros"| Busca
    Busca -->|"falha ao buscar @US-002-S11"| ErroBusca["Mensagem de erro e botão Tentar novamente"]
    ErroBusca -->|"Tentar novamente"| Busca
    Busca -->|"clica em um anúncio"| Detalhe

    Detalhe -->|"endereço antigo ou anúncio arquivado @US-003-S06"| Indisp["Este anúncio não está mais disponível"]
    Indisp --> Home
    Detalhe -->|"clica em Chamar no WhatsApp @US-004-S01"| Zap["WhatsApp abre com a mensagem pronta"]
    Detalhe -->|"computador sem o aplicativo @US-004-S05"| ZapWeb["WhatsApp Web em nova aba; o anúncio continua aberto"]
    Detalhe -->|"clica em Ligar @US-004-S02"| Tel["Aplicativo de telefone com o número pronto"]
    Zap --> Fim(["Visitante fala com a Gazeta"])
    ZapWeb --> Fim
    Tel --> Fim
```

## Notas

- **Contagem de cliques (meta da SPEC):** Página inicial → categoria → anúncio → "Chamar no WhatsApp" são 3 cliques; pelos anúncios recentes da página inicial são 2 cliques (anúncio, botão).
- O site não registra quem clicou nos botões de contato (@US-004, regra de negócio). O `/verify` só consegue conferir o endereço aberto (`tel:` e o link do WhatsApp), não a conversa em si.
- O número exibido vem da configuração do Administrador (`US-015`); todo anúncio publicado tem contato visível porque, sem número, a publicação é bloqueada (@US-010-S08).
- O endereço de uma busca guarda todos os filtros: abrir o mesmo endereço em outra aba repete o resultado (@US-002-S09).
- Favoritar em qualquer ponto desta jornada está em `visitante-favoritos.md`.
