# .docgen - Platform-Agnostic Documentation Automation

This directory contains scripts and configuration for cross-platform documentation automation.

## Purpose

The `.docgen/` directory provides platform-agnostic automation for the AI-assisted documentation system. Scripts and configurations here work across both GitHub Actions and Azure DevOps Pipelines, enabling seamless platform switching.

## Files

### Configuration Files

- **platform-config.json**: Platform selection and configuration for GitHub and Azure DevOps
- **mcp-config.json**: MCP server configuration for AI-assisted documentation with Claude Code

### Automation Scripts

- **diagram-gen.ps1**: Automated diagram generation from .NET assemblies (Mermaid + PlantUML)
- **validate-docs.ps1**: Documentation validation (XML comments, markdown linting, broken links)
- **detect-platform.ps1**: Detect current CI/CD platform (GitHub Actions, Azure DevOps, local)
- **switch-platform.ps1**: Switch between GitHub and Azure DevOps platforms
- **setup-mcp.ps1**: Configure MCP servers for Claude Code
- **wiki-sync.ps1**: Sync wiki content to GitHub Wiki or Azure DevOps Wiki

## Usage

### Generate Diagrams

```powershell
pwsh .docgen/diagram-gen.ps1 -All
```

### Validate Documentation

```powershell
pwsh .docgen/validate-docs.ps1
```

### Setup MCP Servers

```powershell
pwsh .docgen/setup-mcp.ps1
```

### Switch Platform

```powershell
# Switch to GitHub
pwsh .docgen/switch-platform.ps1 -Platform GitHub

# Switch to Azure DevOps
pwsh .docgen/switch-platform.ps1 -Platform AzureDevOps
```

## Requirements

- PowerShell Core (cross-platform)
- .NET 10 SDK
- DocFX (installed globally)
- dll2mmd (installed globally)
- PlantUmlClassDiagramGenerator (installed globally)

## See Also

- [Platform-Agnostic Architecture](../docs/architecture/ai-docs-platform-agnostic-architecture.md)
- [Implementation Plan](../docs/architecture/ai-docs-implementation-plan.md)
- [GitHub Plugin Guide](../docs/architecture/github-plugin-guide.md)
- [Azure DevOps Plugin Guide](../docs/architecture/azure-devops-plugin-guide.md)
