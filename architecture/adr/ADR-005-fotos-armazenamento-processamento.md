# ADR-005: Fotos em pasta fora da raiz do site, reprocessadas para WebP, com original retido 30 dias

**Date**: 2026-09-30
**Status**: Accepted

> **Em resumo:** toda foto enviada é conferida pelo conteúdo, convertida em duas versões WebP (1.600 px e 480 px) sem GPS e gravada numa pasta persistente fora da raiz do site, que o WebDeploy não toca. O arquivo original fica 30 dias numa pasta de descarte, para permitir reprocessar, e depois é apagado por uma limpeza diária. As fotos são entregues por um controller, que só mostra ao público as de anúncios publicados.

## Context
- US-008 (envio, capa, remoção), US-003 (galeria e ampliação), NFR-05 (peso das páginas), NFR-12 (formatos pelo conteúdo, 10 MB, 20/6/0 fotos, HEIC convertido), S18 (remover GPS).
- Publicação por WebDeploy: arquivos dentro da raiz do site podem ser apagados ou sobrescritos a cada publicação (`ARCHITECTURE.md` §9).
- Decisão do Product Owner (2026-09-30): guardar as versões WebP para sempre e o original por 30 dias, para reprocessar (novo tamanho, novo formato, correção de erro) sem ocupar disco indefinidamente.
- `rules/tech-stack.md` prefere armazenamento em nuvem (Blob/S3) a disco local; a hospedagem escolhida não inclui um.

## Options Considered
| Option | Pros | Cons |
|--------|------|------|
| **A. `IPhotoStorage` em pasta persistente fora da raiz + WebP + original 30 dias** | Sem custo extra; não é apagada pelo WebDeploy; troca futura por Blob só na implementação; versões leves para a vitrine | Depende do espaço do plano (AR-04) e de backup do provedor; entrega pelo próprio site |
| B. Azure Blob / S3 desde já | Padrão do kit; durável; pode usar CDN | Conta e custo novos; segredos a mais; latência de envio; sem necessidade na escala da v1 |
| C. `wwwroot/uploads` dentro do site | Mais simples de servir | O WebDeploy pode apagar as fotos; mistura dados com arquivos publicados |
| D. Fotos no banco (`varbinary`) | Backup junto com o banco | Banco do provedor cresce rápido; leitura mais lenta; limite de espaço do SQL |

## Decision
Adopt **Option A** because é a única que não perde fotos numa publicação sem acrescentar serviço pago, atende a NFR-05 e NFR-12 e mantém a troca por Blob isolada atrás de `IPhotoStorage`.

## Consequences
**Positive**: fotos sobrevivem às publicações; páginas leves (480 px na lista); GPS removido de tudo o que é publicado; reprocessamento possível por 30 dias.
**Negative**: o próprio site entrega as imagens (sem CDN); backup das fotos depende do provedor (S19, AR-03); originais ocupam espaço por 30 dias (AR-04).
**Risks**:
- A biblioteca de imagem com HEIC usa componentes nativos que podem não carregar na hospedagem compartilhada (AR-05). Mitigação: prova no ambiente antes do `/build` terminar; se falhar, HEIC passa a ser recusado com mensagem clara até haver alternativa, e o SPEC é ajustado.
- A limpeza dos originais não rodar porque o IIS para o processo ocioso. Mitigação: a limpeza roda ao iniciar e a cada 24 horas e apaga tudo o que já passou de 30 dias; atraso não perde nada.
- Os originais ainda têm GPS. Mitigação: `_originals/` nunca é servida por nenhuma rota.

## v2 Upgrade Trigger
Revisit this decision when **any** of the following becomes true:
- O espaço do plano passar de 70% de uso, ou as fotos ultrapassarem 10 GB.
- O LCP p75 passar de 2,5 s por 7 dias seguidos mesmo com as versões reduzidas (NFR-23: avaliar CDN de imagens).
- A hospedagem mudar para nuvem com armazenamento de objetos → `BlobPhotoStorage`.
- O volume de originais a limpar por dia deixar a limpeza lenta → primeiro avaliar Dapper em lote (ADR-004) e, se não bastar, Hangfire (ADR-012).

## Implementation Notes
- **Interfaces no Core:** `IPhotoStorage` (gravar, ler, apagar por chave) e `IImageProcessor` (validar, decodificar, gerar versões). Implementações `FileSystemPhotoStorage` e `MagickImageProcessor` na Infrastructure.
- **Biblioteca:** Magick.NET (licença Apache 2.0), pacote da variante Q8 para Windows x64 — aprovação em AR-06 e prova em AR-05.
- **Validação (NFR-12):** tamanho até 10 MB conferido antes de ler o corpo (`RequestSizeLimit`); formato pela assinatura do arquivo (JPEG `FF D8 FF`, PNG `89 50 4E 47`, GIF `47 49 46 38`, WebP `RIFF….WEBP`, HEIC/HEIF caixa `ftyp` com `heic`, `heix`, `mif1` ou `msf1`), nunca pela extensão; limite de fotos pelo grupo da categoria (20, 6 ou 0 — ADR-002).
- **Processamento:** corrigir orientação pelo EXIF; remover todos os metadados; gerar `_1600.webp` (largura máxima 1.600 px) e `_480.webp` (480 px), qualidade 80; GIF animado vira imagem estática do primeiro quadro.
- **Estrutura de pastas** (`PhotoStorage__BasePath`): `<adId>/<photoId>_1600.webp`, `<adId>/<photoId>_480.webp` (permanentes) e `_originals/<yyyy-MM>/<guid>.<ext>` (30 dias). Nome sempre gerado; o nome enviado pelo usuário nunca vai para o disco.
- **Registro:** `AdPhotos.OriginalKey` guarda o caminho do original; fica nulo quando a limpeza apaga o arquivo.
- **Reprocessamento:** operação de manutenção (sem tela na v1) que regera as versões a partir do original; se `OriginalKey` for nulo ou o arquivo não existir, falha com a mensagem "original indisponível".
- **Limpeza:** `OriginalsCleanupService : BackgroundService` — roda ao iniciar o site e depois a cada 24 horas enquanto o processo estiver vivo; varre `_originals/<yyyy-MM>/`, apaga arquivos com mais de 30 dias pela data de criação, anula o `OriginalKey` correspondente e registra no log (Serilog) cada arquivo apagado e o total. A anulação de `OriginalKey` é feita por lote, com uma instrução (`ExecuteUpdateAsync` do EF Core); Dapper só se o volume medido justificar (ADR-004).
- **Entrega:** rota `/fotos/{adId}/{photoId}-{tamanho}.webp` num controller; anúncio publicado → público; qualquer outra situação → só a equipe com acesso ao anúncio; `Cache-Control: public, max-age=31536000, immutable` nas fotos publicadas (nome muda quando a foto muda); `_originals/` não tem rota.
- **Envio:** endpoint `POST /api/v1/ads/{id}/photos` com antiforgery; responde a foto criada ou ProblemDetails (`VALIDATION_ERROR`, `CONFLICT` para limite de fotos).

## Revision note (2026-10-04): implementação da tarefa 3.4

Decisões tomadas ao construir o pipeline (aprovadas pelo Product Owner); o que não está aqui continua como acima.

- **Estrutura de arquivos:** `AdPhotos.StorageKey` guarda a **base** `<adId>/<guid>` (GUID em hexadecimal, 32 caracteres, gerado pelo site). O código acrescenta o sufixo da versão:
  - versão grande: `<StorageKey>_1600.webp`
  - miniatura: `<StorageKey>_480.webp`
  - original: `_originals/<yyyy-MM>/<guid>.<ext>`, com a extensão vinda do formato detectado pela assinatura, nunca do nome enviado (guardado em `AdPhotos.OriginalKey`).
  O arquivo usa um GUID, e não o `photoId`, porque o id do banco só existe depois do INSERT; assim os arquivos são gravados antes e desfeitos se algo falhar. A rota pública continua `/fotos/{adId}/{photoId}-{480|1600}.webp` (ids numéricos) e busca o `StorageKey` no banco. `AdPhotos.SizeBytes` é o tamanho da versão de 1600 px.
- **Biblioteca:** `Magick.NET-Q8-x64` 14.17.2. Carrega em Linux e traz os delegados `heic` e `webp`; lê HEIC mas **não escreve** (os testes usam um HEIC mínimo montado à mão, `tests/.../Photos/Fixtures`). A prova na hospedagem compartilhada Windows (AR-05) continua pendente.
- **Segurança (RC-2):** a política do ImageMagick (`Photos/policy.xml`, embutida) nega tudo (`coder none *`) e libera só JPEG, PNG, GIF, WebP e HEIC/HEIF; delegados externos, filtros e `@arquivo` ficam desligados. A leitura é **forçada** ao formato que a assinatura decidiu (`MagickReadSettings.Format`). O Magick.NET não tem API pública de política: o arquivo é gravado numa pasta `_magick/<hash da política>` dentro de `PhotoStorage__BasePath` (o ImageMagick **não sobrescreve** arquivos que já existem na pasta; sem o hash uma política nova seria ignorada). Antes de decodificar, o cabeçalho é lido e a foto é recusada acima de 50 milhões de pixels ou de 20.000 px de lado; limites de recurso: 512 MB de memória e 30 s por operação; no máximo 2 fotos decodificadas ao mesmo tempo no processo.
- **Assinatura de HEIC mais estrita que a nota acima:** as marcas genéricas `mif1`/`msf1` só valem se uma marca de HEIC (`heic`, `heix`, `hevc`…) aparecer entre as compatíveis; um AVIF puro (`avif` + `mif1`) é recusado.
- **Arquivo cortado:** o ImageMagick devolve a parte lida e só avisa. Avisos de fim de arquivo prematuro (`premature end`, `insufficient image data`…) viram recusa, para uma foto pela metade não ir ao ar.
- **Processamento:** orientação pelo EXIF, conversão para sRGB (perfil embutido ou CMYK) **antes** de descartar o perfil, remoção de todos os metadados, qualidade 80, só diminui (foto menor que 1600 ou 480 px mantém o tamanho), GIF/WebP animado vira o primeiro quadro.
- **Entrega:** `PhotoDelivery` usa a mesma regra de leitura de `AdAccess` (Administrador e autor); publicado responde `public, max-age=31536000, immutable`, o resto `private, no-store`; "não existe" e "não pode ver" dão a mesma 404. A rota tem limite próprio de **300 pedidos por minuto por IP** (`RateLimitingExtensions.PhotoPolicy`) e **sai do limite global de 100**: uma página de 24 cards com 2 fotos faz 48 pedidos e os estouraria.
- **Órfãos:** um processo que morra entre gravar os arquivos e registrar em `AdPhotos` deixa arquivos sem registro; a limpeza da 3.6 varre também `<adId>/` atrás deles (BACKLOG).
