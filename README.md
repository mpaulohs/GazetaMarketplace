# GazetaMarketplace

Site de classificados (carros, imóveis, serviços, vagas e produtos) em **ASP.NET Core MVC (.NET 10)** com SQL Server, painel para a equipe (Redator e Administrador) e site público em português. O estado atual do projeto, o que falta e como publicar estão nos documentos abaixo.

## Documentação de publicação e estado do projeto

| Documento | Para que serve |
|---|---|
| [`docs/GO-LIVE-CHECKLIST.md`](docs/GO-LIVE-CHECKLIST.md) | **Checklist do dia da publicação**, uma página, na ordem |
| [`docs/DEPLOY-RUNBOOK.md`](docs/DEPLOY-RUNBOOK.md) | Passo a passo para publicar no SmarterASP (pacote, pastas, banco, HTTPS, variáveis, teste do IP, DPAPI) |
| [`docs/INFRA.md`](docs/INFRA.md) | Servidor e configuração: pré-requisitos, variáveis de ambiente, proxy, chaves, fuso, conferência depois de subir |
| [`docs/VERIFY-CHECKLIST.md`](docs/VERIFY-CHECKLIST.md) | Comandos de teste e verificação manual (celular, fotos reais, HTTPS, página de erro...) |
| [`docs/PROJECT-STATUS.md`](docs/PROJECT-STATUS.md) | O que foi construído, resultados dos testes, achados corrigidos e pendências |

Relatórios: `reports/TEST_REPORT.md` (testes e cobertura), `reports/CODE_REVIEW.md` (revisão de código), `security/SCAN_REPORT.md` (segurança), `plans/BACKLOG.md` (pendências). Especificação: `specs/SPEC.md`; arquitetura: `architecture/ARCHITECTURE.md`.

**Projetos do site:** `src/GazetaMarketplace.Web` (telas e rotas), `src/GazetaMarketplace.Core` (regras de negócio) e `src/GazetaMarketplace.Infrastructure` (banco, fotos, e-mail, logs). Testes em `tests/`.

---

> **O restante deste arquivo é o texto do modelo de projeto (template ClaudeStack.net) em que o site foi criado.** Os nomes `ClaudeStack.*` abaixo são do modelo e **não existem neste repositório**; os projetos reais são os `GazetaMarketplace.*` acima. Para compilar use `dotnet build`; para rodar as suítes de teste, os comandos corretos estão em [`docs/VERIFY-CHECKLIST.md`](docs/VERIFY-CHECKLIST.md) §1.1.

# ClaudeStack.net

[![.NET Version](https://img.shields.io/badge/.NET-10.0%20RC%202-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)
[![Code of Conduct](https://img.shields.io/badge/Code%20of%20Conduct-Contributor%20Covenant-purple.svg)](CODE_OF_CONDUCT.md)

A full-stack .NET 10 template project with Claude Code AI integration, demonstrating modern ASP.NET Core architecture with centralized package management, MSTest with the new Microsoft.Testing.Platform, end-to-end testing with Playwright, and comprehensive GitHub Actions CI/CD pipeline with automated AI code review.

## Prerequisites

- [.NET 10.0 SDK RC 2](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) (version 10.0.100-rc.2.25502.107 or later)
- [PowerShell](https://github.com/PowerShell/PowerShell) (for Playwright browser installation)
- IDE: Visual Studio 2022, VS Code, or Rider

## Technologies

- **.NET 10.0 RC 2** - Latest .NET release candidate
- **ASP.NET Core MVC** - Web application with Razor runtime compilation
- **ASP.NET Core Minimal APIs** - Lightweight API with OpenAPI/Swagger
- **MSTest v4** - Testing framework with Microsoft.Testing.Platform
- **Playwright** - End-to-end browser testing
- **Centralized Package Management** - NuGet package version management

## Project Structure

```
├── src/
│   ├── ClaudeStack.Web/          # ASP.NET Core MVC application
│   └── ClaudeStack.API/          # ASP.NET Core Web API (Minimal APIs)
├── tests/
│   ├── ClaudeStack.Web.Tests/    # Unit tests for MVC application
│   ├── ClaudeStack.Web.Tests.Playwright/  # E2E tests for MVC application
│   ├── ClaudeStack.API.Tests/    # Unit tests for API application
│   └── ClaudeStack.API.Tests.Playwright/  # E2E tests for API application
├── Directory.Build.props          # Shared MSBuild properties
├── Directory.Packages.props       # Centralized NuGet package versions
├── global.json                    # SDK version and test runner config
└── sln.slnx                      # Solution file
```

## Getting Started

### Build the Solution

```bash
dotnet build
```

### Run the Applications

```bash
# Run the MVC application
dotnet run --project src/ClaudeStack.Web/ClaudeStack.Web.csproj

# Run the API application
dotnet run --project src/ClaudeStack.API/ClaudeStack.API.csproj
```

The API will be available at `https://localhost:5001` with Swagger UI at `/swagger`.

### Run Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet run --project tests/ClaudeStack.Web.Tests/ClaudeStack.Web.Tests.csproj
dotnet run --project tests/ClaudeStack.API.Tests/ClaudeStack.API.Tests.csproj
```

### Setup Playwright (First Time)

After building the Playwright test projects, install the browsers:

```powershell
pwsh -Command "cd tests/ClaudeStack.Web.Tests.Playwright/bin/Debug/net10.0; ./playwright.ps1 install"
pwsh -Command "cd tests/ClaudeStack.API.Tests.Playwright/bin/Debug/net10.0; ./playwright.ps1 install"
```

Then run the Playwright tests:

```bash
dotnet run --project tests/ClaudeStack.Web.Tests.Playwright/ClaudeStack.Web.Tests.Playwright.csproj
dotnet run --project tests/ClaudeStack.API.Tests.Playwright/ClaudeStack.API.Tests.Playwright.csproj
```

## Key Features

### Centralized Package Management

This project uses [Central Package Management (CPM)](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management) to manage NuGet package versions in a single location (`Directory.Packages.props`). All project files reference packages without specifying versions.

### Shared Build Configuration

`Directory.Build.props` defines common build properties for all projects:

- Target Framework: `net10.0`
- Nullable Reference Types: Disabled
- Implicit Usings: Disabled
- Treat Warnings as Errors: Enabled

### Microsoft.Testing.Platform

This project uses the new [Microsoft.Testing.Platform](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-runner-intro) instead of the legacy VSTest runner. Test projects are executable and can be run directly with `dotnet run`.

### Automated PR Validation Pipeline

Comprehensive GitHub Actions workflow that automatically validates all pull requests:

- **6-step validation process**: Authorization → PR Guardrails → Quality Checks → Code Review → Security Review → .NET Validation
- **Security scanning**: GitLeaks, NuGet vulnerabilities, .NET security analyzers, path security
- **.NET-specific validation**: CPM compliance, .csproj structure, global.json configuration
- **Code quality**: Automated formatting, build verification, test execution
- **Optional AI review**: Claude Code integration for .NET best practices

See [.github/workflows/README.md](.github/workflows/README.md) for detailed documentation.

## Development Notes

- **Explicit Using Statements Required**: `ImplicitUsings` is disabled, so all C# files must include explicit `using` statements.
- **Centralized Versions**: All NuGet package versions are managed in `Directory.Packages.props`. Do not specify versions in project files.
- **Test Runner Configuration**: The test runner is configured in `global.json`. Do not use the `--test-runner` flag when creating new test projects, as it will overwrite `global.json`.

## Claude Code Infrastructure

This project includes Claude Code infrastructure for enhanced AI-assisted development:

- **Auto-activating skills** via hooks that detect context and suggest relevant skills
- **skill-developer** meta-skill for creating project-specific skills
- **Dev docs system** for maintaining context across development sessions
- **Specialized agents** for code review, refactoring, and documentation tasks

See [.claude/README.md](.claude/README.md) for usage details and [CLAUDE.md](CLAUDE.md) for comprehensive development guidance.

### Attribution

The Claude Code infrastructure (`.claude/` directory) is based on the [Claude Code Infrastructure Showcase](https://github.com/diet103/claude-code-infrastructure-showcase) repository, released under the MIT License. Copyright (c) 2025 Claude Code Infrastructure Contributors. See [.claude/ATTRIBUTION.md](.claude/ATTRIBUTION.md) for complete licensing information.

## Contributing

We welcome contributions! This project demonstrates .NET 10 features and modern development practices.

**Before contributing, please read:**
- [CONTRIBUTING.md](CONTRIBUTING.md) - Development setup, coding standards, and PR process
- [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) - Community guidelines
- [SECURITY.md](SECURITY.md) - Security policy and vulnerability reporting

**Key contribution areas:**
- Bug fixes and improvements
- Additional test coverage
- Documentation enhancements
- New example features demonstrating .NET 10 capabilities

All pull requests go through automated validation including code quality, security scanning, and .NET-specific checks.

## License

This project is provided as-is for educational and reference purposes. Note that the Claude Code infrastructure in the `.claude/` directory is subject to the MIT License (see [.claude/ATTRIBUTION.md](.claude/ATTRIBUTION.md)).
