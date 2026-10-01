# Diagrama de componentes

> **Em resumo:** o que existe dentro do site, em três camadas (Clean Architecture, ADR-001). A camada Web recebe as requisições; o Core guarda as regras e declara interfaces; a Infrastructure implementa essas interfaces (banco, arquivos, ViaCEP, SendGrid). As dependências apontam sempre para o Core. Detalhes: `ARCHITECTURE.md` §3.

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│ GazetaMarketplace.Web  (apresentação)                                        │
│  ┌───────────────┐ ┌────────────────┐ ┌──────────────┐ ┌──────────────────┐  │
│  │ Controllers do│ │ Área Painel    │ │ API /api/v1  │ │ Middlewares      │  │
│  │ site público  │ │ (Redator,      │ │ CEP, fotos,  │ │ erros, correlação│  │
│  │ (Razor)       │ │ Administrador) │ │ catálogo,    │ │ segurança, rate  │  │
│  │               │ │                │ │ favoritos    │ │ limit            │  │
│  └───────┬───────┘ └───────┬────────┘ └──────┬───────┘ └──────────────────┘  │
└──────────┼─────────────────┼─────────────────┼───────────────────────────────┘
           │                 │                 │  chamam serviços de aplicação
           ▼                 ▼                 ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ GazetaMarketplace.Core  (domínio e aplicação; sem dependências externas)     │
│  ┌──────────┐ ┌───────────┐ ┌─────────┐ ┌────────────┐ ┌─────────────────┐   │
│  │ Vitrine  │ │ Anúncios  │ │ Fotos   │ │ Categorias │ │ Equipe e acesso │   │
│  └──────────┘ └───────────┘ └─────────┘ └────────────┘ └─────────────────┘   │
│  ┌───────────────┐ ┌─────────────┐ ┌───────────────────────┐                 │
│  │ Configurações │ │ Localização │ │ Catálogo de veículos  │                 │
│  └───────────────┘ └─────────────┘ └───────────────────────┘                 │
│  Grupos de campos por categoria (ADR-002) · exceções AppException            │
│  Interfaces: IPhotoStorage · IImageProcessor · ICepLookup · IEmailSender     │
│              IVehicleCatalog · repositórios dos módulos                      │
└──────────────────────────────────────┬───────────────────────────────────────┘
                                       │ implementadas por
                                       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ GazetaMarketplace.Infrastructure                                             │
│  ┌───────────────┐ ┌──────────────────┐ ┌──────────────┐ ┌────────────────┐  │
│  │ AppDbContext  │ │ FileSystemPhoto- │ │ ViaCepLookup │ │ SendGridEmail- │  │
│  │ (EF Core) +   │ │ Storage +        │ │ (HttpClient, │ │ Sender         │  │
│  │ Identity      │ │ ImageProcessor   │ │ cache no BD) │ │                │  │
│  └───────┬───────┘ └────────┬─────────┘ └──────┬───────┘ └───────┬────────┘  │
│  ┌──────────────────────────┴─────────┐        │                 │           │
│  │ OriginalsCleanupService            │        │                 │           │
│  │ (BackgroundService, 1 vez por dia) │        │                 │           │
│  └────────────────────────────────────┘        │                 │           │
└──────────┼─────────────────────────────────────┼─────────────────┼───────────┘
           ▼                                     ▼                 ▼
    ┌─────────────┐  ┌──────────────────┐   ┌─────────┐      ┌──────────┐
    │ SQL Server  │  │ Pasta persistente│   │ ViaCEP  │      │ SendGrid │
    └─────────────┘  └──────────────────┘   └─────────┘      └──────────┘
```

| Componente | Camada | Faz | Interface / implementação | ADR |
|---|---|---|---|---|
| Controllers do site público | Web | Início, categoria, busca, detalhe, favoritos, entrega de fotos | — | 001 |
| Área Painel | Web | Telas da equipe (anúncios, fila, categorias, usuários, configurações) | Políticas `Administrador` e `Redator` | 003 |
| API `/api/v1` | Web | CEP, envio de foto, catálogo encadeado, anúncios por ids | ProblemDetails (§8) | 001 |
| Middlewares | Web | Erros → ProblemDetails, correlação, cabeçalhos de segurança, limite de requisições | — | 010 |
| Módulos do Core | Core | Regras de cada área (§3.1 do ARCHITECTURE) | Serviços de aplicação | 001 |
| Grupos de campos | Core | Campos, limites e filtros por categoria | Registro em código | 002 |
| `AppDbContext` + Identity | Infrastructure | Escrita, esquema e contas | EF Core 10 | 003, 004 |
| Read repositories | Infrastructure | Leituras complexas: busca e lista do painel | Dapper sobre `IDbConnection` | 004 |
| `FileSystemPhotoStorage` + `ImageProcessor` | Infrastructure | Gravar original temporário e as versões WebP; servir arquivos | `IPhotoStorage`, `IImageProcessor` | 005 |
| `OriginalsCleanupService` | Infrastructure | Apagar originais com mais de 30 dias | `BackgroundService` | 005 |
| `ViaCepLookup` | Infrastructure | CEP → cidade/UF com tempo limite, nova tentativa e cache | `ICepLookup` | 007 |
| `SendGridEmailSender` | Infrastructure | Envio do e-mail de redefinição | `IEmailSender` | 009 |
| Importador do catálogo | Ferramenta fora do site | Gera o script de carga do catálogo de veículos | `IVehicleCatalog` lê as tabelas carregadas | 008 |
