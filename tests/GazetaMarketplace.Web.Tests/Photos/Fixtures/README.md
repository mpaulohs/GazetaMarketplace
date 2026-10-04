# Fixtures de foto

`sample.heic` (885 bytes, 64 × 48) é um HEIC mínimo e válido, montado à mão: um quadro HEVC gerado com `ffmpeg -c:v libx265` dentro de uma caixa HEIF (`make-heic.py`).
Existe porque o Magick.NET só **lê** HEIC (não escreve) e o ambiente de teste não tem codificador de HEIC. Ele não vem de um iPhone: um arquivo real de iPhone
(com orientação, perfil de cor e GPS) continua sendo a verificação manual da tarefa 3.4.

Para gerar de novo:

```bash
ffmpeg -y -f lavfi -i testsrc=size=64x48:rate=1 -frames:v 1 -pix_fmt yuv420p -c:v libx265 -x265-params "keyint=1:log-level=none:repeat-headers=1" -f hevc in.hevc
python3 make-heic.py        # lê in.hevc e escreve sample.heic
```
