# NET10 Project Example - Feature Summary

**Last Updated**: November 3, 2025
**Target Audience**: Internal development and product teams
**Purpose**: Quick reference for feature status and capabilities

---

## Current Feature Status

| Feature | Status | Description | Documentation |
|---------|--------|-------------|-----------------|
| ASP.NET Core MVC Application | Stable | Full-featured web application with home, privacy, and error pages | [Web App Docs](https://notmyself.github.io/net10-project-example/user/) |
| ASP.NET Core Minimal API | Stable | RESTful weather forecast API with OpenAPI/Swagger integration | [API Docs](https://notmyself.github.io/net10-project-example/user/) |
| Weather Forecast Service | Stable | Generates 5-day weather forecasts with temperatures and conditions | [API Reference](https://notmyself.github.io/net10-project-example/) |
| OpenAPI/Swagger Support | Stable | Interactive API documentation and testing interface | Developer Docs |
| MSTest Unit Tests | Stable | Comprehensive unit test coverage for both applications | Testing Guide |
| Playwright E2E Tests | Stable | End-to-end browser automation tests for web application | Testing Guide |
| Hot Reload Development | Stable | Enable rapid development with file change detection | Developer Docs |
| Centralized Package Management | Stable | Unified NuGet version management via Directory.Packages.props | CPM Guide |
| Docker Containerization | Stable | Pre-configured Docker images for both applications | [Docker Docs](https://notmyself.github.io/net10-project-example/) |
| GitHub Actions CI/CD | Stable | Automated build, test, and deployment pipeline | CI/CD Docs |
| Automated Code Coverage | Stable | XPlat coverage reporting with PR comments | CI/CD Docs |
| Claude Code Integration | Stable | AI-assisted development and automated code review | Developer Docs |

---

## Legend

| Symbol | Status | Meaning |
|--------|--------|---------|
| ✅ Stable | Production-ready | Feature fully implemented and tested |
| 🧪 Beta | Limited release | Feature working but may have issues |
| 📋 Planned | Roadmap | Scheduled for future implementation |
| 🚧 In Progress | Active development | Currently being developed |

---

## Application Features

### MVC Web Application

The `ClaudeStack.Web` project demonstrates a complete ASP.NET Core MVC implementation with modern patterns and best practices.

**Pages & Features:**

| Feature | Description | Route | Status |
|---------|-------------|-------|--------|
| Home Page | Landing page with application overview | `/` or `/home` | Stable |
| Privacy Policy | Privacy policy and data handling information | `/home/privacy` | Stable |
| Error Handling | Comprehensive error handling with request details | Error routes | Stable |
| Responsive Design | Mobile-optimized Razor views | All pages | Stable |
| Logging Integration | Structured logging via ILogger | All controllers | Stable |
| Dependency Injection | Full DI container support | Program.cs | Stable |

**Technical Capabilities:**

- ASP.NET Core MVC with explicit C# using statements
- Razor template engine with runtime compilation support
- Structured logging and request tracing
- Error page with request ID and trace identification
- Full controller-based routing and action methods
- View models for type-safe view data binding

### Minimal API Application

The `ClaudeStack.API` project demonstrates modern ASP.NET Core Minimal API patterns with comprehensive OpenAPI documentation.

**Endpoints:**

| Endpoint | Method | Description | Status |
|----------|--------|-------------|--------|
| `/weatherforecast` | GET | Returns 5-day weather forecast | Stable |
| `/openapi/v1.json` | GET | OpenAPI specification (Development) | Stable |

**Features:**

- Minimal API configuration for lightweight services
- OpenAPI/Swagger auto-documentation
- Type-safe endpoint definitions with records
- Automatic OpenAPI metadata generation
- Request/response contract serialization
- Development-environment-specific API exposure

**Weather Forecast Data Structure:**

```csharp
record WeatherForecast(DateOnly Date, int TemperatureC, string Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```

### Testing Features

Both applications include comprehensive testing infrastructure for unit and end-to-end testing.

**MSTest Unit Testing:**

| Component | Location | Status | Description |
|-----------|----------|--------|-------------|
| Web App Tests | `tests/ClaudeStack.Web.Tests/` | Stable | MSTest unit tests for MVC controllers and services |
| API Tests | `tests/ClaudeStack.API.Tests/` | Stable | MSTest unit tests for API endpoints and business logic |
| Test Runner | Microsoft.Testing.Platform | Stable | Modern async-aware test runner with method-level parallelization |

**Playwright E2E Testing:**

| Component | Location | Status | Description |
|-----------|----------|--------|-------------|
| Web App E2E | `tests/ClaudeStack.Web.Tests.Playwright/` | Stable | Browser automation tests for web workflows |
| API E2E | `tests/ClaudeStack.API.Tests.Playwright/` | Stable | HTTP-based API contract and integration tests |
| Browser Support | Chromium, Firefox, WebKit | Stable | Multi-browser testing capability |

**Test Capabilities:**

- Method-level parallelization for faster test execution
- Async/await support in test methods
- Cross-browser testing with Playwright
- API contract testing and validation
- UI workflow automation and validation
- Screenshot and video capture on failure
- HTML and Cobertura coverage reports

### Development Features

**Hot Reload & Rapid Development:**

- File change detection during `dotnet run` or `dotnet watch`
- Browser refresh on static file changes
- Rapid feedback loop for development
- Supported for both MVC and Minimal API projects

**Code Style & Conventions:**

- Explicit using statements (ImplicitUsings disabled)
- Nullable reference types handling
- Treat warnings as errors for code quality
- .NET 10.0 RC 2 SDK pinning
- Method-level async/await patterns

**Package Management:**

- Centralized version management in `Directory.Packages.props`
- Single source of truth for all NuGet dependencies
- Simplified project file maintenance
- Semantic versioning enforcement
- Easier dependency updates across solution

**.NET 10 Patterns:**

- Latest C# language features
- Modern async patterns
- Top-level statements support
- Record types for immutable data
- Nullable annotations
- Source generators (where applicable)

---

## Infrastructure Features

### CI/CD Pipeline

Comprehensive automated pipeline for quality assurance and deployment.

**Build & Test Pipeline:**

| Stage | Trigger | Actions | Status |
|-------|---------|---------|--------|
| Authorization | PR opened | Verify contributor authorization | Stable |
| Guardrails | PR opened | Check PR description and size | Stable |
| Quality Checks | PR opened/updated | Format, build, test verification | Stable |
| Code Review | PR opened/updated | Optional Claude Code automated review | Stable |
| Security Review | PR opened/updated | Secrets scan, vulnerability check | Stable |
| .NET Validation | PR opened/updated | Project structure and CPM compliance | Stable |

**Test Execution:**

- Runs `dotnet test` on all test projects
- Collects XPlat code coverage data
- Generates HTML and Cobertura reports
- Posts coverage summary to PR comments
- Fails PR if tests do not pass

**Artifact Collection:**

- Build artifacts (7-day retention)
- Coverage reports (30-day retention)
- Release binaries (permanent)
- Docker images (GHCR registry)

### Docker Containerization

Production-ready Docker images for both applications.

**Image Registry:**

- `ghcr.io/{owner}/{repo}/example-web` - MVC application
- `ghcr.io/{owner}/{repo}/example-api` - Minimal API

**Image Features:**

- Multi-stage builds for optimized size
- .NET 10.0 runtime environment
- Health check endpoints configured
- Port 8080 exposed by default
- Ubuntu-based Linux container
- Automated builds on push to main

**Usage:**

```bash
# Pull latest image
docker pull ghcr.io/{owner}/{repo}/example-web:main

# Run container
docker run -p 8080:8080 ghcr.io/{owner}/{repo}/example-web:main
```

### Documentation System

Multi-channel documentation approach for different audiences.

**User Documentation:**

- **Location**: [https://notmyself.github.io/net10-project-example/user/](https://notmyself.github.io/net10-project-example/user/)
- **Content**: Getting started guides, tutorials, API usage examples
- **Target Audience**: End users and integration partners
- **Format**: Markdown with DocFX rendering

**Developer Documentation:**

- **Location**: [https://notmyself.github.io/net10-project-example/](https://notmyself.github.io/net10-project-example/)
- **Content**: Architecture guides, API reference, contribution guidelines
- **Target Audience**: Developers and architects
- **Format**: Markdown, code samples, diagrams

**Internal Wiki:**

- **Location**: `/docs/wiki/` (this directory)
- **Content**: Feature status, team processes, internal guidelines
- **Target Audience**: Internal development team
- **Format**: Markdown for GitHub wiki-like experience

### Code Review & Quality Assurance

**Automated Code Review:**

- Optional Claude Code integration via OIDC
- Reviews for .NET 10 best practices
- Checks for security vulnerabilities
- Validates coding conventions and patterns
- Identifies breaking changes

**Security Scanning:**

- GitLeaks for secret detection
- .NET Roslyn security analyzers
- NuGet vulnerability scanning
- Path security analysis for file operations
- Dependency vulnerability tracking

---

## Planned Features

The following enhancements are scheduled for future releases:

| Feature | Priority | Target | Description |
|---------|----------|--------|-------------|
| GraphQL API | Medium | Q4 2025 | GraphQL endpoint alongside REST API |
| Database Integration | High | Q1 2026 | Entity Framework Core with SQL Server |
| Authentication & Authorization | High | Q1 2026 | Identity management and role-based access |
| Caching Layer | Medium | Q1 2026 | Distributed caching with Redis support |
| Rate Limiting | Medium | Q2 2026 | API rate limiting and throttling |
| Microservices Architecture | Medium | Q2 2026 | Service decomposition and orchestration |
| Advanced Monitoring | Low | Q2 2026 | Application Insights and custom metrics |
| Multi-tenant Support | Low | Q3 2026 | Tenant isolation and multi-tenant data handling |

---

## Quick Start Links

**For Users:**
- [User Documentation](https://notmyself.github.io/net10-project-example/user/) - Getting started and guides
- [GitHub Repository](https://github.com/NotMyself/net10-project-example) - Source code and releases

**For Developers:**
- [Developer Documentation](https://notmyself.github.io/net10-project-example/) - API reference and architecture
- [Contributing Guide](https://github.com/NotMyself/net10-project-example/blob/main/CONTRIBUTING.md) - How to contribute
- [CLAUDE.md](https://github.com/NotMyself/net10-project-example/blob/main/CLAUDE.md) - Development setup and patterns

**For Team:**
- [System Purpose](system-purpose.md) - What this system does
- [System Access](system-access.md) - Environment access and credentials
- [Active Development](active-development.md) - Current work in progress

---

## Key Metrics

- **Build Time**: 2-5 minutes (parallel)
- **Test Execution**: 1-3 minutes (parallel with parallelization enabled)
- **Code Coverage Target**: 80%+ for new features
- **Docker Image Size**: ~300MB (runtime)

---

## Support & Communication

- **Issues & Bugs**: [GitHub Issues](https://github.com/NotMyself/net10-project-example/issues)
- **Discussions**: [GitHub Discussions](https://github.com/NotMyself/net10-project-example/discussions)
- **Pull Requests**: [GitHub PRs](https://github.com/NotMyself/net10-project-example/pulls)
- **Security**: [SECURITY.md](https://github.com/NotMyself/net10-project-example/blob/main/SECURITY.md)

---

**Last Updated**: November 3, 2025
**Maintained by**: Development Team
**Next Review**: Q4 2025
