# Sequência: envio de foto com conversão (HEIC incluído)

> **Em resumo:** cada foto é conferida pelo tamanho e pelo conteúdo, gravada como original temporário, convertida em duas versões WebP sem GPS e registrada no anúncio. Falhas devolvem mensagem por arquivo e não deixam arquivos pela metade. A limpeza dos originais com mais de 30 dias aparece no fim. Base: US-008-S02 a S06, NFR-12, ADR-005.

```text
 ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
 │  Tela (JS)  │    │  Site (API) │    │    Fotos    │    │    Pasta    │    │    Banco    │
 └──────┬──────┘    └──────┬──────┘    └──────┬──────┘    └──────┬──────┘    └──────┬──────┘
        │ 1. POST /ads/{id}/photos            │                  │                  │
        │─────────────────▶│                  │                  │                  │
        │                  │─┐ 2. até 10 MB? antiforgery? autoria e situação?       │
        │                  │◀┘                │                  │                  │
        │                  │                  │                  │                  │
  ╔═ > 10 MB → 400 "A foto excede o limite de 10 MB" (US-008-S05)
  ╠═ sem permissão ou anúncio Em revisão → 403 / 409
  ╚═
        │                  │ 3. Adicionar(arquivo)               │                  │
        │                  │─────────────────▶│                  │                  │
        │                  │                  │─┐ 4. limite do grupo (20/6/0) e assinatura do arquivo
        │                  │                  │◀┘                │                  │
        │                  │                  │                  │                  │
  ╔═ limite atingido → 409 "Cada anúncio pode ter no máximo N fotos" (US-008-S04)
  ╠═ formato não aceito → 400 "Formato não aceito. Use JPG, PNG, WebP, GIF ou HEIC"
  ╚═
        │                  │                  │ 5. grava _originals/yyyy-MM/guid    │
        │                  │                  │─────────────────▶│                  │
        │                  │                  │─┐ 6. decodifica (HEIC via Magick.NET), orienta, tira metadados
        │                  │                  │◀┘                │                  │
        │                  │                  │                  │                  │
  ╔═ falha ao decodificar → 400 "Não foi possível processar a foto"; original apagado; log Error
  ╚═
        │                  │                  │ 7. grava _1600.webp e _480.webp     │
        │                  │                  │─────────────────▶│                  │
        │                  │                  │ 8. INSERT AdPhotos (OriginalKey)    │
        │                  │                  │────────────────────────────────────▶│
        │                  │                  │                  │                  │
  ╔═ erro de disco/banco → apaga os arquivos já gravados; 500 com código de referência
  ╚═
        │ 9. 201 miniatura; 1ª = capa         │                  │                  │
        │◀─────────────────│                  │                  │                  │
        │                  │                  │                  │                  │
  ╔═ conexão cai no envio → "Falha ao enviar" + "Tentar de novo" só nessa foto (US-008-S06)
  ╚═
        │                  │                  │                  │                  │
        │                  │                  │─┐ 10. Limpeza: ao iniciar e a cada 24 h
        │                  │                  │◀┘                │                  │
        │                  │                  │ 11. apaga originais > 30 dias       │
        │                  │                  │─────────────────▶│                  │
        │                  │                  │ 12. OriginalKey = NULL; log         │
        │                  │                  │────────────────────────────────────▶│
        │                  │                  │                  │                  │
```

Legenda: `╔═` abre um caminho alternativo (falha ou exceção), `╠═` outro caminho alternativo, `╚═` volta ao caminho principal. Números seguem a ordem das mensagens.

## Ramos de falha

| Situação | O que acontece | Referência |
|---|---|---|
| Arquivo acima de 10 MB | Recusado antes de ler o corpo inteiro; mensagem por arquivo | NFR-12, US-008-S05 |
| PDF ou outro formato (mesmo com extensão .jpg) | Assinatura não confere → recusado | NFR-12 |
| Limite de fotos do grupo | 409; nenhuma foto além do limite; Vagas de emprego não tem envio | US-008-S04, ADR-002 |
| HEIC que a biblioteca não consegue ler (componente nativo ausente) | 400 com mensagem clara; log Error; ver AR-05 | ADR-005 |
| Falha de disco ou de banco no meio | Arquivos gravados são apagados; nada fica sem registro | ADR-005 |
| Conexão cai durante o envio | A tela marca a foto com "Falha ao enviar" e permite tentar de novo; as outras fotos e os textos seguem | US-008-S06 |
| Limpeza não roda porque o site ficou parado | Roda na próxima partida e apaga tudo o que passou de 30 dias | ADR-005 |
| Reprocessar foto sem original | Falha com "original indisponível" | ADR-005 |
