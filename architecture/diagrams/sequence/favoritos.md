# Sequência: favoritos do visitante

> **Em resumo:** os favoritos ficam só no navegador do visitante (lista de ids no `localStorage`, S1). Na página "Meus favoritos", o navegador pede ao servidor os anúncios desses ids; o servidor devolve só os publicados, e os que sumiram são tirados da lista com aviso. Base: `specs/wireframes/flows/visitante-favoritos.md`, US-005.

```text
 ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
 │  Visitante  │    │ Navegador JS│    │  Site (API) │    │    Banco    │
 └──────┬──────┘    └──────┬──────┘    └──────┬──────┘    └──────┬──────┘
        │ 1. clica no coração                 │                  │
        │─────────────────▶│                  │                  │
        │                  │─┐ 2. adiciona/remove id no localStorage
        │                  │◀┘                │                  │
        │                  │                  │                  │
  ╔═ armazenamento bloqueado → "Não foi possível salvar seus favoritos neste navegador"
  ╚═
        │ 3. coração marcado; contador        │                  │
        │◀─────────────────│                  │                  │
        │                  │                  │                  │
        │ 4. abre Meus favoritos              │                  │
        │─────────────────▶│                  │                  │
        │                  │ 5. GET /api/v1/ads?ids=…            │
        │                  │─────────────────▶│                  │
        │                  │                  │ 6. SELECT publicados
        │                  │                  │─────────────────▶│
        │                  │ 7. lista (só publicados)            │
        │                  │◀─────────────────│                  │
        │                  │─┐ 8. ids ausentes saem do localStorage
        │                  │◀┘                │                  │
        │                  │                  │                  │
  ╔═ algum id sumiu → aviso "1 anúncio favoritado deixou de estar disponível…" (US-005-S06)
  ╠═ lista vazia → "Você ainda não favoritou nenhum anúncio"
  ╠═ falha da API → mensagem de erro e "Tentar novamente"; a lista local não é apagada
  ╚═
        │ 9. cards dos favoritos              │                  │
        │◀─────────────────│                  │                  │
        │                  │                  │                  │
```

Legenda: `╔═` abre um caminho alternativo (falha ou exceção), `╠═` outro caminho alternativo, `╚═` volta ao caminho principal. Números seguem a ordem das mensagens.

## Ramos de falha

| Situação | O que acontece | Referência |
|---|---|---|
| `localStorage` indisponível (modo privado, bloqueio) | Mensagem; nada é salvo; o site continua funcionando | US-005 |
| Anúncio favoritado foi arquivado ou despublicado | Servidor não o devolve; o navegador tira o id e mostra o aviso | US-005-S06, US-011-S04 |
| Lista de ids adulterada no navegador | Servidor aceita só números, limita a 100 ids e devolve só publicados | NFR-13 |
| Falha da API | Erro com código de referência; ids locais preservados para nova tentativa | ADR-010 |
