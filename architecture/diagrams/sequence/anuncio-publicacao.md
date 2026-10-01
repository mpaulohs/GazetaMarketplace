# Sequência: anúncio do rascunho até a publicação

> **Em resumo:** o Redator salva um rascunho, envia para revisão e o Administrador publica ou rejeita. O servidor confere autoria, situação e pendências em cada passo; a edição simultânea por dois administradores é barrada pela versão da linha (`rowversion`). Fotos: `envio-foto.md`. Base: `specs/wireframes/flows/equipe-anuncio-publicacao.md`, US-008 a US-011.

```text
 ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
 │   Redator   │    │Site (Painel)│    │   Anúncios  │    │    Banco    │    │Administrador│
 └──────┬──────┘    └──────┬──────┘    └──────┬──────┘    └──────┬──────┘    └──────┬──────┘
        │ 1. POST salvar (título…)            │                  │                  │
        │─────────────────▶│                  │                  │                  │
        │                  │ 2. Salvar rascunho                  │                  │
        │                  │─────────────────▶│                  │                  │
        │                  │                  │─┐ 3. confere autoria e situação; título obrigatório
        │                  │                  │◀┘                │                  │
        │                  │                  │ 4. INSERT/UPDATE Ads                │
        │                  │                  │─────────────────▶│                  │
        │ 5. "Rascunho salvo"                 │                  │                  │
        │◀─────────────────│                  │                  │                  │
        │                  │                  │                  │                  │
  ╔═ sem título → "Informe um título"; nada é criado (US-008-S08)
  ╚═
        │ 6. POST enviar p/ revisão           │                  │                  │
        │─────────────────▶│                  │                  │                  │
        │                  │ 7. Enviar para revisão              │                  │
        │                  │─────────────────▶│                  │                  │
        │                  │                  │─┐ 8. pendências do grupo de campos (US-009)
        │                  │                  │◀┘                │                  │
        │                  │                  │                  │                  │
  ╔═ há pendências → lista de pendências com links; continua Rascunho (US-009-S02/S03)
  ╠═ já está Em revisão (clique duplo) → nada muda; aparece uma vez na fila (US-009)
  ╚═
        │                  │                  │ 9. UPDATE Status=Em revisão         │
        │                  │                  │─────────────────▶│                  │
        │ 10. "Enviado para revisão"          │                  │                  │
        │◀─────────────────│                  │                  │                  │
        │                  │                  │                  │                  │
        │                  │ 11. Fila → Publicar                 │                  │
        │                  │◀───────────────────────────────────────────────────────│
        │                  │ 12. Publicar (RowVersion)           │                  │
        │                  │─────────────────▶│                  │                  │
        │                  │                  │─┐ 13. telefone do site configurado? (US-010-S08)
        │                  │                  │◀┘                │                  │
        │                  │                  │                  │                  │
  ╔═ sem telefone → "Configure o telefone/WhatsApp do site antes de publicar"
  ╚═
        │                  │                  │ 14. UPDATE … WHERE RowVersion=@v    │
        │                  │                  │─────────────────▶│                  │
        │                  │                  │                  │                  │
  ╔═ 0 linhas: outro Administrador decidiu antes → 409, "já foi publicado por outro administrador"
  ╚═
        │                  │                  │ 15. INSERT AuditEntries             │
        │                  │                  │─────────────────▶│                  │
        │                  │ 16. "Anúncio publicado"             │                  │
        │                  │───────────────────────────────────────────────────────▶│
        │                  │                  │                  │                  │
  ╔═ Rejeitar: motivo obrigatório → Rejeitado com motivo visível ao autor (US-010)
  ╠═ Despublicar → volta a Rascunho; Arquivar → definitivo, sai do site (US-011)
  ╚═
        │                  │                  │                  │                  │
```

Legenda: `╔═` abre um caminho alternativo (falha ou exceção), `╠═` outro caminho alternativo, `╚═` volta ao caminho principal. Números seguem a ordem das mensagens.

## Ramos de falha

| Situação | O que acontece | Referência |
|---|---|---|
| Título vazio ao salvar | Nada é gravado; mensagem junto ao campo | US-008-S08 |
| Redator tenta abrir anúncio de outro Redator | 403 → "Você não tem permissão para acessar este anúncio" | US-008-S10, NFR-13 |
| Redator tenta editar anúncio Em revisão | Página somente leitura; o servidor recusa gravações nessa situação | US-008-S12 |
| Pendências ao enviar | Lista de pendências com links para os campos; situação não muda | US-009-S02, S03 |
| Clique duplo em "Enviar para revisão" | A segunda chamada encontra a situação já alterada e não faz nada | US-009 |
| Publicar sem telefone configurado | Bloqueado com mensagem e link para Configurações | US-010-S08 |
| Dois administradores decidem o mesmo anúncio | `rowversion` diferente → `ConflictException` → 409 e mensagem | US-010, ADR-004 |
| Sessão expirada no meio do formulário | Volta ao login; o que foi digitado não é preservado (lacuna 8 dos wireframes) | NFR-08 |
| Erro de banco | 500 com código de referência; transação desfeita; nada parcial | ADR-004, ADR-010 |
