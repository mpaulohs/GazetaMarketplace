#!/usr/bin/env bash
# Regenera db/scripts/gazeta-idempotente.sql a partir das migrations e coloca no topo o SET que o EF não emite.
# Os índices filtrados (ex.: IX_AspNetUsers_NormalizedEmail) exigem QUOTED_IDENTIFIER ON; sem ele o sqlcmd falha com o erro 1934.
# Índice sobre coluna calculada (colunas de busca, JSON_VALUE) exige ainda ARITHABORT, ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS e CONCAT_NULL_YIELDS_NULL ON e NUMERIC_ROUNDABORT OFF:
# o sqlcmd nem sempre os liga (depende do driver), então o script os declara.
set -euo pipefail
cd "$(dirname "$0")/../.."
tmp="$(mktemp)"
dotnet tool run dotnet-ef migrations script --idempotent \
  --project src/GazetaMarketplace.Infrastructure --startup-project src/GazetaMarketplace.Infrastructure -o "$tmp"
{
  printf '%s\n' '-- Script idempotente das migrations do GazetaMarketplace (gerado por db/scripts/gerar-script.sh; não edite à mão).' \
    'SET QUOTED_IDENTIFIER ON;' 'SET ARITHABORT ON;' 'SET ANSI_NULLS ON;' 'SET ANSI_PADDING ON;' 'SET ANSI_WARNINGS ON;' \
    'SET CONCAT_NULL_YIELDS_NULL ON;' 'SET NUMERIC_ROUNDABORT OFF;' 'GO' ''
  # remove o BOM que o EF coloca no início
  sed '1s/^\xEF\xBB\xBF//' "$tmp"
} > db/scripts/gazeta-idempotente.sql
rm -f "$tmp"
