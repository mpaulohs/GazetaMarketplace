# Fluxo: Visitante favorita anúncios neste navegador — @US-005, @US-011

> **Em resumo:** o visitante marca anúncios com o coração e os revê em "Meus favoritos", sem conta. Os favoritos ficam só no navegador dele; quando a equipe tira um anúncio do ar, o anúncio some da lista com um aviso. Depende da suposição S1 da SPEC, ainda a confirmar.

## Jornada (Mermaid)

```mermaid
flowchart TD
    Start(["Visitante em uma lista ou na página de um anúncio"]) --> Marca{"Onde marca?"}
    Marca -->|"coração no cartão @US-005-S01"| Salva{"O navegador permite salvar?"}
    Marca -->|"botão Favoritar na página do anúncio @US-005-S02"| Salva
    Salva -->|"sim"| Ok["Coração preenchido e contador do topo aumenta"]
    Salva -->|"não @US-005-S07"| Bloq["Mensagem: Não foi possível salvar seus favoritos neste navegador"]
    Ok -->|"fecha e reabre o navegador @US-005-S03"| Persiste["Favorito continua marcado"]
    Persiste --> Lista["Meus favoritos"]
    Ok --> Lista
    Lista -->|"clica em Remover @US-005-S04"| Menos["Anúncio sai da lista e o contador diminui"]
    Lista -->|"nenhum favorito @US-005-S05"| Vazio["Você ainda não favoritou nenhum anúncio, link para a página inicial"]
    Lista -->|"favorito arquivado pela equipe @US-011-S04"| Aviso["Anúncio não aparece; aviso de que 1 anúncio deixou de estar disponível"]
    Lista -->|"abre em outro aparelho @US-005-S08"| Outro["Lista vazia e aviso: Seus favoritos ficam salvos apenas neste navegador"]
```

## Notas

- Os favoritos não passam pelo servidor. Por isso o teste automatizado (`/verify`) precisa reabrir o navegador no mesmo perfil para provar @US-005-S03.
- O aviso "Seus favoritos ficam salvos apenas neste navegador" é permanente na página "Meus favoritos" (@US-005-S08), não aparece só na primeira vez.
- Um favorito que deixou de estar publicado é removido da lista do visitante quando ele abre "Meus favoritos"; a remoção depende de o anúncio ter sido consultado de novo, não de um aviso enviado pelo servidor.
