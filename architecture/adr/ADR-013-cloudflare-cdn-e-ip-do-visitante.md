# ADR-013: Cloudflare como CDN e proxy; IP do visitante por CF-Connecting-IP

**Date**: 2026-10-08
**Status**: Accepted (decisão do Product Owner)

> **Em resumo:** o domínio `gzto.com.br` já está no Cloudflare, então o site passa a ficar atrás dele. Isso exige duas coisas: HTTPS entre o Cloudflare e o SmarterASP com um certificado de origem (Origin CA, modo Full strict) e a leitura do IP real do visitante pelo cabeçalho `CF-Connecting-IP`, aceito só de endereços do Cloudflare. Supera, quanto ao CDN, a linha correspondente do ADR-012.

## Context
- Sem tratamento, todo pedido chega ao site com o IP de um servidor do Cloudflare. Os limites de tentativas por IP (SC-01, SC-02) e o bloqueio de conta por conta+IP (SC-03) passariam a valer para o site inteiro.
- O ADR-012 deixou o CDN de fora da v1 (gatilho: LCP p75 acima de 2,5 s). O Product Owner decidiu usar o Cloudflare desde o lançamento; o ganho de desempenho vem junto, não é o motivo.
- O provedor respondeu o SEC-01 com um artigo sobre IISNode, que não vale para ASP.NET Core.

## Decision
1. **HTTPS:** Cloudflare em **Full (strict)** com **Origin CA** instalado no SmarterASP. Nunca Flexible (laço de redirecionamentos 308). Não se solicita o certificado grátis do provedor.
2. **IP do visitante:** a configuração `ForwardedHeaders:Cloudflare=true` liga o modo Cloudflare. O pacote `Cloudflare.ForwardedHeaders` baixa as faixas oficiais (cópia embutida como reserva) e as grava nas redes confiáveis do `ForwardedHeadersOptions`; o site escolhe `CF-Connecting-IP` como cabeçalho do IP (o pacote usaria `X-Forwarded-For`). Pedido de fora das faixas não consegue forjar o IP.
3. O modo é **opcional e desligado por padrão**: desligado, nada é baixado e o site segue fail-closed (ADR-011, SC-01).
4. `KnownProxies` continua existindo, só para o caso de o teste do IP achar outro proxy do SmarterASP entre o Cloudflare e o site.

## Technology Decision: Cloudflare.ForwardedHeaders 1.0.0
- **Problema:** manter a lista de faixas do Cloudflare e aplicá-la ao middleware de cabeçalhos encaminhados.
- **Alternativa avaliada:** lista de faixas escrita no próprio site (poucas linhas, sem terceiros), com atualização manual.
- **Por que o pacote:** pedido do Product Owner; MIT; o código foi lido (decompilado) e só faz o que anuncia: baixa `cloudflare.com/ips-v4` e `ips-v6`, usa a cópia embutida se falhar e preenche `KnownNetworks`. Funciona no .NET 10 (provado nos testes `CloudflareForwardingTests`).
- **Risco (critérios de `rules/tech-stack.md` não atendidos):** versão única (1.0.0, abril de 2026), autor individual, sem histórico de uso conhecido, sem histórico de manutenção; o pacote decide quem pode informar o IP, e portanto os limites de login. Se a cópia embutida ficar velha, faixas novas do Cloudflare deixam de ser confiadas (falha para o lado seguro: o IP vira o do Cloudflare).
- **Mitigações:** versão **fixa** em `Directory.Packages.props`; atualização só com revisão do código; a lista é baixada de `cloudflare.com` na partida; log da quantidade de faixas na partida (zero vira `Error`); testes com rede simulada. Saída: BACKLOG CF-01 (trocar pelo código próprio).
- **Decisão:** Adopt.

## Consequences
- O `Site__BaseUrl` e todo o resto continuam com o domínio público; o IP do SmarterASP visto direto mostra aviso de certificado (esperado).
- Os scripts inseridos pelo Cloudflare (Rocket Loader, ofuscação de e-mail, Web Analytics automático) violam a CSP `script-src 'self'`: ficam desligados.
- **v2 Upgrade Trigger:** o pacote ficar sem manutenção por 12 meses, ou o Cloudflare mudar o formato das listas, ou uma vulnerabilidade no pacote: trocar pelo código próprio.
