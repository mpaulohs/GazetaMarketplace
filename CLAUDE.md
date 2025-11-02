# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 10.0 RC 2 example project demonstrating full-stack architecture with centralized package management. The project includes ASP.NET Core MVC and Minimal API applications, each with corresponding unit tests (MSTest) and end-to-end tests (Playwright).

## Build & Test Commands

### Building
```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/Example.Web/Example.Web.csproj
dotnet build src/Example.API/Example.API.csproj
```

### Running Applications
```bash
# Run MVC application
dotnet run --project src/Example.Web/Example.Web.csproj

# Run API application
dotnet run --project src/Example.API/Example.API.csproj
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test project (using Microsoft.Testing.Platform)
dotnet run --project tests/Example.Web.Tests/Example.Web.Tests.csproj
dotnet run --project tests/Example.API.Tests/Example.API.Tests.csproj

# Run Playwright tests
dotnet run --project tests/Example.Web.Tests.Playwright/Example.Web.Tests.Playwright.csproj
dotnet run --project tests/Example.API.Tests.Playwright/Example.API.Tests.Playwright.csproj

# Run a single test (using test filter)
dotnet test --filter FullyQualifiedName~TestMethod1
```

### Playwright Setup
After creating a new Playwright test project, install browsers:
```powershell
pwsh -Command "cd tests/Example.Web.Tests.Playwright/bin/Debug/net10.0; ./playwright.ps1 install"
```

## Architecture

### Project Structure
- **src/Example.Web**: ASP.NET Core MVC application with Razor runtime compilation enabled
- **src/Example.API**: ASP.NET Core Web API using Minimal APIs with OpenAPI/Swagger
- **tests/Example.Web.Tests**: MSTest unit tests for MVC application
- **tests/Example.Web.Tests.Playwright**: Playwright end-to-end tests for MVC application
- **tests/Example.API.Tests**: MSTest unit tests for API application
- **tests/Example.API.Tests.Playwright**: Playwright end-to-end tests for API application

### Configuration Files

#### Directory.Build.props
Defines shared MSBuild properties for all projects:
- **TargetFramework**: net10.0
- **Nullable**: disabled (explicit using statements required)
- **ImplicitUsings**: disabled (explicit using statements required)
- **TreatWarningsAsErrors**: true

#### Directory.Packages.props
Centralized NuGet package version management (CPM). All package versions are defined here, and project files reference packages without versions.

To add a new package:
1. Add `<PackageVersion Include="PackageName" Version="x.y.z" />` to Directory.Packages.props
2. Add `<PackageReference Include="PackageName" />` (without Version) to the project file

#### global.json
Pins the .NET SDK version and configures the test runner:
```json
{
  "sdk": {
    "version": "10.0.100-rc.2.25502.107",
    "rollForward": "latestFeature"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

### Testing Framework

This project uses **MSTest with Microsoft.Testing.Platform** (the new test runner, not the legacy VSTest). Test projects require:
```xml
<PropertyGroup>
  <EnableMSTestRunner>true</EnableMSTestRunner>
  <OutputType>Exe</OutputType>
</PropertyGroup>
```

Tests are configured for method-level parallelization in MSTestSettings.cs:
```csharp
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
```

## Important Notes

### Creating New Test Projects

**CRITICAL**: When creating new MSTest projects, do NOT use the `--test-runner` flag. The test runner is already configured in global.json, and using the flag will overwrite the entire global.json file, removing the SDK version configuration.

```bash
# WRONG - will overwrite global.json
dotnet new mstest -o tests/NewProject --test-runner Microsoft.Testing.Platform

# CORRECT - test runner inherited from global.json
dotnet new mstest -o tests/NewProject
```

After creating the test project, manually add to the .csproj:
```xml
<PropertyGroup>
  <EnableMSTestRunner>true</EnableMSTestRunner>
  <OutputType>Exe</OutputType>
</PropertyGroup>
```

### Implicit Usings Disabled

Since ImplicitUsings is disabled, all C# files must include explicit using statements. Common namespaces needed:
- `using System;`
- `using System.Linq;`
- `using System.Threading.Tasks;`
- `using Microsoft.VisualStudio.TestTools.UnitTesting;` (for tests)
- `using Microsoft.Playwright.MSTest;` (for Playwright tests)
- ASP.NET Core namespaces (Microsoft.AspNetCore.Builder, Microsoft.Extensions.DependencyInjection, etc.)

### Package References

Always reference packages without Version attributes in .csproj files. Versions are centrally managed in Directory.Packages.props.

```xml
<!-- CORRECT -->
<PackageReference Include="Microsoft.AspNetCore.OpenApi" />

<!-- WRONG -->
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
```

## Claude Code Infrastructure

This project uses Claude Code infrastructure for enhanced development workflow.

**Environment:** Running on WSL2 (Ubuntu) - all hooks and scripts use Linux/bash conventions.

### Installed Components
- **Auto-activating skills** via hooks (UserPromptSubmit, PostToolUse)
- **skill-developer** meta-skill for creating project-specific skills
- **Specialized agents** for complex tasks (refactoring, documentation, code review, etc.)
- **Dev docs system** for context persistence across sessions
- **Slash commands** for automated dev docs creation (/dev-docs, /dev-docs-update)

### Configuration
- `.claude/` directory contains skills, hooks, agents, and configuration
- `.claude/hooks/` - TypeScript/bash hooks with npm dependencies
- `dev/active/` contains development documentation for complex tasks

### Usage
Skills activate automatically based on your prompts and file context. See `.claude/README.md` for details.

### Creating .NET-Specific Skills
Use skill-developer to create skills tailored to this .NET 10 project:
- ASP.NET Core MVC patterns
- Minimal API best practices
- MSTest with Microsoft.Testing.Platform
- .NET 10 specific guidance

Start with: "I want to create a skill for [ASP.NET Core/testing/etc]"
