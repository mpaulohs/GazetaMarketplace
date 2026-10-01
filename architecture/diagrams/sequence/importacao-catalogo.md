# Sequência: importação única do catálogo de veículos

> **Em resumo:** uma pessoa da equipe técnica roda, na própria máquina, a ferramenta de exportação contra o banco do GazetaOnline com uma conta somente leitura; ela gera um script SQL idempotente, que é revisado, versionado e aplicado no banco do GazetaMarketplace. O site só lê as tabelas carregadas. Base: A3, A5, ADR-008.

```text
 ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
 │   Técnico   │    │  Exportador │    │ GazetaOnline│    │     Git     │    │   Banco GM  │
 └──────┬──────┘    └──────┬──────┘    └──────┬──────┘    └──────┬──────┘    └──────┬──────┘
        │ 1. roda com conexão (variável)      │                  │                  │
        │─────────────────▶│                  │                  │                  │
        │                  │                  │                  │                  │
  ╔═ variável ausente → a ferramenta para sem gerar nada
  ╚═
        │                  │ 2. SELECT marcas…versões            │                  │
        │                  │─────────────────▶│                  │                  │
        │                  │                  │                  │                  │
  ╔═ conexão recusada → erro claro; nenhum arquivo parcial
  ╚═
        │                  │─┐ 3. valida hierarquia; descarta órfãos (relatório)    │
        │                  │◀┘                │                  │                  │
        │                  │─┐ 4. gera vehicle-catalog.sql (MERGE, Source)          │
        │                  │◀┘                │                  │                  │
        │ 5. revisão + commit do script       │                  │                  │
        │───────────────────────────────────────────────────────▶│                  │
        │ 6. aplica o script (ferramenta SQL) │                  │                  │
        │──────────────────────────────────────────────────────────────────────────▶│
        │                  │                  │                  │                  │
  ╔═ falha no meio → corrigir e rodar de novo (idempotente)
  ╚═
        │                  │                  │                  │                  │─┐ 7. site lê via IVehicleCatalog (cache 10 min)
        │                  │                  │                  │                  │◀┘
        │                  │                  │                  │                  │
  ╔═ parecer da A5 negativo → nova exportação de outra fonte com outro Source
  ╚═
        │                  │                  │                  │                  │
```

Legenda: `╔═` abre um caminho alternativo (falha ou exceção), `╠═` outro caminho alternativo, `╚═` volta ao caminho principal. Números seguem a ordem das mensagens.

## Ramos de falha

| Situação | O que acontece | Referência |
|---|---|---|
| Variável de conexão ausente | A ferramenta para antes de conectar | ADR-008 |
| Banco do GazetaOnline inacessível | Erro claro; nenhum arquivo gerado | ADR-008 |
| Modelos sem marca ou versões sem ano (dados órfãos) | Descartados e listados num relatório para revisão | ADR-008 |
| Script aplicado pela metade | Rodar de novo: o script é idempotente (MERGE) | ADR-008 |
| Parecer jurídico negativo (A5) | Nova carga de outra fonte com outro `Source`; o site não muda | A5, ADR-008 |
| Credencial antiga do GazetaOnline | Não é usada; só a conta somente leitura por variável de ambiente | Descoberta, ADR-008 |
