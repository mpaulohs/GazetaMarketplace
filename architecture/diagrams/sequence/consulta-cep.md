# Sequência: consulta de CEP no formulário do anúncio

> **Em resumo:** ao digitar o CEP, a tela pede ao servidor; o servidor responde do cache do banco ou consulta o ViaCEP (até 5 s). Em falha do serviço, a tela tenta mais uma vez e, se falhar de novo, abre UF e cidade em listas; o anúncio fica com o selo de conferência. "CEP não encontrado" não abre o preenchimento manual. Base: US-008-S01, US-008-S14, NFR-24, ADR-007.

```text
 ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
 │  Tela (JS)  │    │  Site (API) │    │    Banco    │    │    ViaCEP   │
 └──────┬──────┘    └──────┬──────┘    └──────┬──────┘    └──────┬──────┘
        │ 1. GET /api/v1/cep/{cep}            │                  │
        │─────────────────▶│                  │                  │
        │                  │─┐ 2. 8 dígitos? equipe logada?      │
        │                  │◀┘                │                  │
        │                  │                  │                  │
  ╔═ menos de 8 dígitos → nada é consultado; "Informe um CEP com 8 dígitos" (S23)
  ╚═
        │                  │ 3. CepCache (< 30 dias)             │
        │                  │─────────────────▶│                  │
        │                  │                  │                  │
  ╔═ encontrado no cache → 200 source=cache (sem ViaCEP)
  ╚═
        │                  │ 4. GET {cep}/json/ (5 s)            │
        │                  │────────────────────────────────────▶│
        │                  │ 5. localidade, uf, ibge             │
        │                  │◀────────────────────────────────────│
        │                  │ 6. grava CepCache│                  │
        │                  │─────────────────▶│                  │
        │ 7. 200 cidade e UF                  │                  │
        │◀─────────────────│                  │                  │
        │                  │                  │                  │
  ╔═ "erro": true → 404 "CEP não encontrado. Confira os números." (S24; não abre manual)
  ╠═ sem resposta/5xx/rede → 503 CEP_SERVICE_UNAVAILABLE
  ╚═
        │─┐ 8. 503: "Buscando… (tentativa 2 de 2)" e repete 1 vez│
        │◀┘                │                  │                  │
        │                  │                  │                  │
  ╔═ 2º 503 → aviso + listas UF (27) e cidade (Cities); LocationManual = true (US-008-S14)
  ╚═
        │                  │                  │                  │
```

Legenda: `╔═` abre um caminho alternativo (falha ou exceção), `╠═` outro caminho alternativo, `╚═` volta ao caminho principal. Números seguem a ordem das mensagens.

## Ramos de falha

| Situação | O que acontece | Referência |
|---|---|---|
| CEP incompleto | Nenhuma consulta; mensagem junto ao campo | S23 |
| CEP inexistente | 404; mensagem; o campo continua digitável; não abre o manual | S24, NFR-24 |
| ViaCEP lento (> 5 s), erro 5xx ou sem rede | 503; a tela repete uma vez; na 2ª falha, listas de UF e cidade | US-008-S14, NFR-24 |
| Cache vencido (> 30 dias) | Nova consulta ao ViaCEP; em falha, a tela segue o caminho acima | ADR-007 |
| Pessoa sem login chamando a API | 401 UNAUTHORIZED; a API não serve o público | ADR-007 |
| Anúncio salvo com cidade manual | Selo "Cidade/UF informadas manualmente (CEP não conferido)" na revisão; não impede publicar | US-008, US-010 |
