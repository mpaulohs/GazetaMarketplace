# Contributing to .NET 10 Project Example

Thank you for your interest in contributing! This document provides guidelines and information for contributors.

## Table of Contents

- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Coding Standards](#coding-standards)
- [Pull Request Process](#pull-request-process)
- [Testing](#testing)
- [Project Standards](#project-standards)

## Getting Started

### Prerequisites

- **.NET SDK 10.0 RC 2 or later** (version pinned in `global.json`)
- **PowerShell** (for running scripts and Playwright setup)
- **Git** for version control
- **GitHub CLI** (optional, for enhanced workflow)

### Fork and Clone

1. Fork the repository on GitHub
2. Clone your fork:
   ```bash
   git clone https://github.com/YOUR-USERNAME/net10-project-example.git
   cd net10-project-example
   ```

3. Add upstream remote:
   ```bash
   git remote add upstream https://github.com/NotMyself/net10-project-example.git
   ```

## Development Setup

### Initial Setup

1. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

2. **Build the solution:**
   ```bash
   dotnet build
   ```

3. **Run tests:**
   ```bash
   dotnet test
   ```

4. **Install Playwright browsers** (first time only):
   ```powershell
   pwsh -Command "cd tests/ClaudeStack.Web.Tests.Playwright/bin/Debug/net10.0; ./playwright.ps1 install"
   pwsh -Command "cd tests/ClaudeStack.API.Tests.Playwright/bin/Debug/net10.0; ./playwright.ps1 install"
   ```

### Running Applications

**MVC Application:**
```bash
dotnet run --project src/ClaudeStack.Web/ClaudeStack.Web.csproj
# Navigate to https://localhost:5001
```

**API Application:**
```bash
dotnet run --project src/ClaudeStack.API/ClaudeStack.API.csproj
# Navigate to https://localhost:7001/swagger
```

## Coding Standards

### C# Conventions

This project follows strict .NET coding standards:

#### Explicit Using Statements

**ImplicitUsings is DISABLED** - all files must include explicit using statements.

```csharp
// CORRECT
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// WRONG - will not compile
// (relying on implicit usings)
```

#### Common Namespaces

Add these as needed:

```csharp
// Basic
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

// ASP.NET Core MVC
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ASP.NET Core API
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

// Testing (MSTest)
using Microsoft.VisualStudio.TestTools.UnitTesting;

// Testing (Playwright)
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
```

### Package Management

This project uses **Centralized Package Management (CPM)**.

**Adding a New Package:**

1. Add to `Directory.Packages.props`:
   ```xml
   <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
   ```

2. Reference in `.csproj` **WITHOUT version**:
   ```xml
   <PackageReference Include="Newtonsoft.Json" />
   ```

**NEVER include Version in .csproj:**
```xml
<!-- WRONG - will fail PR validation -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

### Code Quality

- **TreatWarningsAsErrors is enabled** - all warnings must be fixed
- **Async/await:** Use properly, avoid `async void` except for event handlers
- **Null safety:** Handle null references appropriately
- **Error handling:** Use try-catch for expected exceptions
- **LINQ:** Prefer LINQ for collections, but consider performance
- **XML comments:** Required for all public APIs

### Formatting

Run before committing:

```bash
dotnet format
```

The PR validation pipeline will check formatting automatically.

## Pull Request Process

### Before Creating a PR

1. **Update your fork:**
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

2. **Create a feature branch:**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make your changes** following coding standards

4. **Run quality checks:**
   ```bash
   # Format code
   dotnet format

   # Build
   dotnet build

   # Run tests
   dotnet test
   ```

5. **Commit with descriptive messages:**
   ```bash
   git add .
   git commit -m "Add feature X that does Y"
   ```

### Creating the PR

1. **Push to your fork:**
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create Pull Request** on GitHub

3. **Fill out the PR template** completely:
   - Description of changes
   - Motivation and context
   - Type of change
   - Testing performed
   - .NET-specific checklist

### PR Validation Pipeline

Your PR will automatically go through **6 validation steps**:

#### 1️⃣ Authorization
- Verifies you're authorized to contribute
- Checks access control settings

#### 2️⃣ PR Guardrails
- Ensures PR has adequate description
- Checks PR size (warns if >2000 lines)
- Validates required sections completed

#### 3️⃣ Quality Checks
- **Code formatting** (`dotnet format`)
- **Build** (`dotnet build`)
- **Tests** (`dotnet test`)

#### 4️⃣ Code Review (Optional)
- **Claude Code automated review** (if configured)
- Reviews for .NET 10 best practices
- Checks project-specific requirements

#### 5️⃣ Security Review
- **GitLeaks** - scans for secrets
- **.NET security analyzers** - Roslyn analyzers
- **NuGet vulnerabilities** - checks package CVEs
- **Path security** - detects unsafe file operations

#### 6️⃣ .NET Validation
- **.csproj structure** - validates project files
- **CPM compliance** - checks Directory.Packages.props
- **global.json** - validates SDK configuration

### Validation Results

- **✅ All checks pass:** PR can be merged
- **⚠️ Warnings:** Review and address if possible
- **❌ Failures:** Must be fixed before merge

Each step posts detailed results as PR comments.

### Addressing Feedback

1. **Make requested changes**
2. **Commit and push:**
   ```bash
   git add .
   git commit -m "Address review feedback"
   git push origin feature/your-feature-name
   ```

3. **Validation runs automatically** on each push
4. **PR comments update in place** (not duplicated)

## Testing

### Unit Tests (MSTest)

**Create test files:**

```bash
# DO NOT use --test-runner flag (it overwrites global.json)
dotnet new mstest -o tests/YourProject.Tests
```

**Manually add to .csproj:**

```xml
<PropertyGroup>
  <EnableMSTestRunner>true</EnableMSTestRunner>
  <OutputType>Exe</OutputType>
</PropertyGroup>
```

**Run tests:**

```bash
# All tests
dotnet test

# Specific project (using Microsoft.Testing.Platform)
dotnet run --project tests/ClaudeStack.Web.Tests/ClaudeStack.Web.Tests.csproj

# Single test
dotnet test --filter FullyQualifiedName~TestMethod1
```

### End-to-End Tests (Playwright)

**First-time setup:**

```powershell
pwsh -Command "cd tests/ClaudeStack.Web.Tests.Playwright/bin/Debug/net10.0; ./playwright.ps1 install"
```

**Run Playwright tests:**

```bash
dotnet run --project tests/ClaudeStack.Web.Tests.Playwright/ClaudeStack.Web.Tests.Playwright.csproj
```

### Test Parallelization

Tests run in parallel at the method level (configured in `MSTestSettings.cs`):

```csharp
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
```

## Project Standards

### Directory Structure

```
.
├── .github/              # GitHub workflows, scripts, templates
├── dev/                  # Development documentation
├── src/
│   ├── ClaudeStack.Web/      # ASP.NET Core MVC application
│   └── ClaudeStack.API/      # ASP.NET Core Web API (Minimal APIs)
└── tests/
    ├── ClaudeStack.Web.Tests/              # Unit tests for MVC
    ├── ClaudeStack.Web.Tests.Playwright/   # E2E tests for MVC
    ├── ClaudeStack.API.Tests/              # Unit tests for API
    └── ClaudeStack.API.Tests.Playwright/   # E2E tests for API
```

### Configuration Files

- **`Directory.Build.props`** - Shared MSBuild properties
- **`Directory.Packages.props`** - Centralized package versions
- **`global.json`** - SDK version and test runner configuration

### .NET 10 Specific

This project uses **.NET 10.0 RC 2** with:
- **Microsoft.Testing.Platform** (new test runner, not VSTest)
- **Minimal APIs** for the API project
- **Razor runtime compilation** enabled for MVC project
- **OpenAPI/Swagger** for API documentation

## Getting Help

- **Issues:** Open a GitHub issue for bugs or feature requests
- **Discussions:** Use GitHub Discussions for questions
- **Documentation:** Check `dev/` directory for additional docs
- **Claude Code:** See `CLAUDE.md` for AI assistant usage

## Code of Conduct

This project adheres to the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## License

By contributing, you agree that your contributions will be licensed under the same license as the project (see LICENSE file).

---

**Thank you for contributing!** 🎉
