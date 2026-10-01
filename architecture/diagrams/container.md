# Diagrama de contêineres

> **Em resumo:** as partes que rodam e onde guardam dados. Em produção há **um único processo** (o site ASP.NET Core dentro do IIS do SmarterASP.NET), **um banco SQL Server** do provedor e **uma pasta persistente fora da raiz do site** para fotos, originais temporários, logs e chaves. Não há Docker, cache distribuído nem fila em produção (ADR-012). Detalhes: `ARCHITECTURE.md` §3 e §9.

```text
 ┌─────────────────────────────────────┐
 │ Navegador                           │
 │ Páginas Razor + Bootstrap 5.3.8     │
 │ JavaScript em módulos ES            │
 │ Favoritos no localStorage           │
 └──────────────────┬──────────────────┘
                    │ HTTPS (HTML, /api/v1 JSON, /fotos)
                    ▼
 ┌──────────────────────────────────────────────────────────────────────────┐
 │ Hospedagem SmarterASP.NET (IIS, compartilhada)                           │
 │                                                                          │
 │  ┌────────────────────────────────────────┐                              │
 │  │ Site: GazetaMarketplace.Web            │                              │
 │  │ ASP.NET Core 10 no IIS (processo único)│                              │
 │  │ · site público e área Painel           │                              │
 │  │ · endpoints JSON /api/v1               │                              │
 │  │ · limpeza diária de originais          │                              │
 │  │ raiz do site = só arquivos publicados  │                              │
 │  └──────┬──────────────────────┬──────────┘                              │
 │         │ EF Core 10 + Dapper  │ leitura/escrita de arquivos             │
 │         ▼                      ▼                                         │
 │  ┌──────────────────┐  ┌──────────────────────────────────────────────┐  │
 │  │ SQL Server       │  │ Pasta persistente FORA da raiz do site       │  │
 │  │ (do provedor)    │  │ (PhotoStorage__BasePath e irmãs, AR-01)      │  │
 │  │ anúncios,        │  │ · <adId>/<photoId>_1600.webp e _480.webp     │  │
 │  │ categorias,      │  │ · _originals/<yyyy-MM>/ (apagados em 30 dias)│  │
 │  │ equipe, cache    │  │ · logs/ (Serilog, 14 dias)                   │  │
 │  │ de CEP, auditoria│  │ · keys/ (Data Protection)                    │  │
 │  └──────────────────┘  └──────────────────────────────────────────────┘  │
 └──────────┬──────────────────────────────┬────────────────────────────────┘
            │ HTTPS                        │ HTTPS
            ▼                              ▼
   ┌─────────────────┐            ┌─────────────────┐
   │     ViaCEP      │            │    SendGrid     │
   └─────────────────┘            └─────────────────┘

 Fora de produção:
 ┌──────────────────────────────┐      WebDeploy (MSDeploy)
 │ Máquina de quem publica      │ ───────────────────────────▶ raiz do site
 │ Visual Studio 2026 + .pubxml │      (não toca na pasta persistente)
 │ web.Production.config        │
 │ (fora do git)                │
 └──────────────────────────────┘
```

| Contêiner | Tecnologia | Responsabilidade | Dados que guarda |
|---|---|---|---|
| Navegador | HTML/CSS/JS, Bootstrap 5.3.8 | Mostrar as páginas; máscaras de preço e CEP; favoritos | Ids favoritos no `localStorage` (S1) |
| Site `GazetaMarketplace.Web` | ASP.NET Core 10, Razor, EF Core 10, Dapper, Identity | Todas as regras e telas; entrega das fotos; limpeza diária dos originais | Nenhum (sem estado próprio) |
| SQL Server | SQL Server do provedor (versão a confirmar, AR-03) | Dados do sistema | Anúncios, categorias, catálogo de veículos, equipe, configurações, cache de CEP, auditoria |
| Pasta persistente | Sistema de arquivos do IIS, fora da raiz do site | Arquivos que não podem sumir num deploy | Fotos WebP (permanentes), originais (30 dias), logs (14 dias), chaves do Data Protection |
| Máquina de quem publica | Visual Studio 2026, WebDeploy | Publicar a raiz do site com as variáveis de produção | `web.Production.config` com segredos (nunca no git) |

**Por que um processo só:** a escala da v1 cabe folgada num processo (NFR-04), e a hospedagem compartilhada não oferece serviços separados; os pontos de troca para crescer estão nos ADR-005, ADR-007 e ADR-012.
