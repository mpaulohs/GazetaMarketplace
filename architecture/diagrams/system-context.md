# Diagrama de contexto do sistema

> **Em resumo:** quem usa o GazetaMarketplace e com quais sistemas externos ele conversa. Há três tipos de pessoa (Visitante, Redator e Administrador) e quatro sistemas de fora; só dois deles (ViaCEP e SendGrid) são chamados pelo servidor durante o uso normal. Detalhes: `ARCHITECTURE.md` §5.

```text
   ┌──────────────┐          ┌──────────────┐          ┌───────────────┐
   │  Visitante   │          │   Redator    │          │ Administrador │
   │  (sem conta) │          │   (equipe)   │          │   (equipe)    │
   └──────┬───────┘          └──────┬───────┘          └───────┬───────┘
          │ navega, busca,          │ cria e envia             │ revisa, publica,
          │ favorita, contata       │ anúncios                 │ gerencia tudo
          ▼                         ▼                          ▼
   ┌──────────────────────────────────────────────────────────────────────┐
   │                          GazetaMarketplace                           │
   │        site público + painel da equipe (ASP.NET Core 10, IIS)        │
   └────────┬──────────────────┬───────────────────┬──────────────────────┘
            │ CEP → cidade/UF  │ e-mail de         │ links wa.me e tel:
            │ (HTTPS, 5 s)     │ redefinição       │ (abertos pelo navegador)
            ▼                  ▼                   ▼
   ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────────┐
   │     ViaCEP      │ │    SendGrid     │ │ WhatsApp / telefone │
   │ (consulta CEP)  │ │ (API de e-mail) │ │   do visitante      │
   └─────────────────┘ └─────────────────┘ └─────────────────────┘

   ┌───────────────────────────────┐
   │ Banco do GazetaOnline         │  exportação única do catálogo de
   │ (fora do sistema, só leitura) │  veículos, feita fora do site
   └───────────────┬───────────────┘  (ADR-008)
                   │ script de carga
                   ▼
            banco do GazetaMarketplace
```

| Elemento | Tipo | Relação com o sistema |
|---|---|---|
| Visitante | Pessoa, sem conta | Navega, busca, favorita (no próprio navegador) e contata a Gazeta por telefone ou WhatsApp |
| Redator | Pessoa, equipe | Cria e edita os próprios anúncios e os envia para revisão |
| Administrador | Pessoa, equipe | Revisa, publica, rejeita, despublica e arquiva; gerencia categorias, usuários e o telefone do site |
| ViaCEP | Sistema externo | Chamado pelo servidor para converter CEP em cidade e UF; em falha, a tela abre o preenchimento manual (ADR-007) |
| SendGrid | Sistema externo | Chamado pelo servidor para enviar o link de redefinição de senha (ADR-009) |
| WhatsApp / telefone | Sistema externo | **Nenhuma chamada do servidor:** o site só oferece links que o navegador abre |
| Banco do GazetaOnline | Sistema externo | Usado **uma única vez**, fora do site, para gerar o script de carga do catálogo de veículos (ADR-008); sem dependência em tempo de execução |
