# Platform-Agnostic AI-Assisted Documentation System Architecture

**Version:** 1.0
**Last Updated:** 2025-11-02
**Status:** Approved for Implementation

---

## Executive Summary

This document defines the architecture for a **platform-agnostic AI-assisted documentation system** that supports three distinct types of documentation across both **GitHub** and **Azure DevOps** platforms through a plugin architecture.

### Key Characteristics

- **Platform Independence**: Single documentation codebase deploys to GitHub or Azure DevOps
- **Three Documentation Types**: System Developer Docs, System User Docs, Company System Docs
- **AI Integration**: Claude Code + MCP servers for intelligent documentation assistance
- **Zero Cost**: Free tiers of both platforms sufficient for most projects
- **Professional Quality**: Publication-ready documentation suitable for client deliverables
- **Low Maintenance**: 30-60 minutes per month after initial setup

### Architecture Pattern: Core + Platform Plugins

```
Platform-Agnostic Core (.docgen/)
├── DocFX Configuration
├── MCP Servers
├── Diagram Generation
└── Cross-Platform Build (Makefile)
         ↓
    ┌────┴────┐
    ↓         ↓
GitHub Plugin  Azure DevOps Plugin
├── Actions    ├── Pipelines
├── Scripts    ├── Scripts
└── Pages      └── Static Web Apps/Wiki
```

### Business Value

**For Solo Consultant (Primary Audience)**:
- Work in any client environment (GitHub or Azure DevOps)
- Professional documentation demonstrates expertise
- Automated system minimizes maintenance burden
- AI assistance accelerates documentation authoring
- Platform portability reduces lock-in risk

**For Development Teams**:
- Single source of truth for all documentation
- Automated updates keep documentation current
- PR validation enforces documentation quality
- Clear separation between developer, user, and company docs

---

## Table of Contents

1. [Background & Research Summary](#background--research-summary)
2. [Architecture Overview](#architecture-overview)
3. [Three Documentation Types](#three-documentation-types)
4. [Platform-Agnostic Core](#platform-agnostic-core)
5. [Platform Plugins](#platform-plugins)
6. [Technology Stack](#technology-stack)
7. [Platform Comparison](#platform-comparison)
8. [AI Integration](#ai-integration)
9. [Deployment Architecture](#deployment-architecture)
10. [Migration Strategy](#migration-strategy)
11. [Implementation Roadmap](#implementation-roadmap)
12. [References](#references)

---

## Background & Research Summary

### Research Foundation

Comprehensive research ([docs/research/ai-assisted-documentation.md](../research/ai-assisted-documentation.md)) identified the optimal technology stack for .NET/Azure DevOps environments:

**Key Findings**:
1. **DocFX** emerged as the clear winner for .NET documentation automation
   - Used by Microsoft Engineering teams
   - MIT license, actively maintained by .NET Foundation
   - Generates professional HTML from XML comments + Markdown
   - Native Mermaid diagram support
   - 2-3 hour initial setup, zero ongoing maintenance

2. **Claude Code + MCP Servers** provide production-ready AI assistance
   - Microsoft Learn MCP Server for official .NET docs context
   - Docs MCP Server for indexing project documentation
   - Context7 MCP for current library documentation
   - Prevents API hallucinations, ensures correct terminology

3. **Automated Diagram Generation** from code
   - dll2mmd: Mermaid class diagrams from .NET assemblies
   - PlantUmlClassDiagramGenerator: Comprehensive UML from source
   - C4Sharp: Architecture diagrams using C4 Model
   - EF Core Power Tools: Database/entity diagrams

4. **Azure DevOps Recommendation**: Original research focused on Azure DevOps + Azure Pipelines + Azure Static Web Apps

### Adaptation for This Project

**Critical Change**: Repository is on **GitHub**, not Azure DevOps.

**Decision**: Implement platform-agnostic core with **plugin architecture** supporting both platforms:
- GitHub plugin (primary, Phases 1-6)
- Azure DevOps plugin (optional, Phase 7)
- Platform switching mechanism

This provides maximum flexibility for a consultant working with different clients.

---

## Architecture Overview

### High-Level Architecture

The system consists of three layers:

1. **Documentation Content Layer** (Platform-Agnostic)
   - DocFX configurations for developer and user docs
   - Markdown content for all documentation types
   - Wiki source files

2. **Automation Layer** (Platform-Agnostic Core)
   - `.docgen/` directory with shared scripts
   - Makefile for cross-platform builds
   - Diagram generation automation
   - MCP server configurations

3. **Deployment Layer** (Platform-Specific Plugins)
   - GitHub Actions workflows + GitHub Pages
   - Azure Pipelines YAML + Azure Static Web Apps
   - Platform detection and switching

### Directory Structure

```
project-root/
├── docs/                          # Documentation Content (Platform-Agnostic)
│   ├── docfx-developer/          # System Developer Docs
│   │   ├── docfx.json
│   │   ├── articles/
│   │   ├── diagrams/
│   │   └── _site/
│   ├── docfx-user/               # System User Docs
│   │   ├── docfx.json
│   │   ├── articles/
│   │   ├── images/screenshots/
│   │   └── _site/
│   └── wiki/                     # Company System Docs (source)
│       ├── README.md
│       ├── system-purpose.md
│       └── active-development.md
│
├── .docgen/                       # Automation (Platform-Agnostic Core)
│   ├── platform-config.json
│   ├── mcp-config.json
│   ├── diagram-gen.ps1
│   ├── validate-docs.ps1
│   ├── detect-platform.ps1
│   ├── switch-platform.ps1
│   ├── setup-mcp.ps1
│   └── wiki-sync.ps1
│
├── .github/                       # GitHub Plugin
│   ├── workflows/
│   │   ├── docs-developer-deploy.yml
│   │   ├── docs-user-deploy.yml
│   │   ├── docs-wiki-sync.yml
│   │   └── docs-pr-validation.yml
│   └── scripts/
│
├── .azuredevops/                  # Azure DevOps Plugin (Optional)
│   ├── pipelines/
│   │   ├── docs-developer-deploy.yml
│   │   ├── docs-user-deploy.yml
│   │   ├── docs-wiki-deploy.yml
│   │   └── docs-pr-validation.yml
│   └── scripts/
│
├── Makefile                       # Cross-Platform Build
└── Directory.Build.props          # XML Documentation Enabled
```

### Design Principles

1. **Separation of Concerns**
   - Content (docs/) independent of deployment
   - Automation (.docgen/) works on any platform
   - Deployment (plugins) encapsulated

2. **Platform Detection**
   - Auto-detect GitHub Actions vs Azure DevOps in CI
   - Local detection from git remote URL
   - Manual override via platform-config.json

3. **Single Source of Truth**
   - One set of documentation content
   - One set of automation scripts
   - Platform-specific only for deployment

4. **Graceful Degradation**
   - Works without AI (MCP servers optional)
   - Works without diagrams (manual alternatives)
   - Works on single platform (other plugin optional)

---

## Three Documentation Types

### Type 1: System Developer Docs

**Purpose**: Technical documentation for developers working on the system

**Target**: `docs/docfx-developer/` → DocFX Static Site

**Content**:
- **Architecture**: System architecture, component interactions, design patterns
- **Deployment**: Deployment procedures, infrastructure, CI/CD
- **Domain Models**: Business domain concepts and relationships
- **Type Models**: Class hierarchies, interfaces, generic types
- **Database Models**: Entity relationships, schema, migrations
- **System Boundaries**: Integration points, external dependencies
- **Roles & Abilities**: Authentication, authorization, permissions

**Technologies**:
- DocFX for API reference (from XML comments) + conceptual documentation
- Mermaid diagrams for architecture (component, sequence, class)
- PlantUML for comprehensive UML
- dll2mmd for automated class diagrams from assemblies
- EF Core Power Tools for database ERD

**Publishing**:
- **GitHub**: GitHub Pages at `https://yourorg.github.io/yourproject/`
- **Azure DevOps**: Azure Static Web Apps at `https://yourproject-docs.azurestaticapps.net/`
- **Optional Mirror**: Can also sync key articles to Wiki

**Example Structure**:
```
docfx-developer/
├── docfx.json                    # Includes API metadata extraction
├── index.md                      # Developer-focused homepage
├── toc.yml                       # Technical navigation
├── articles/
│   ├── architecture.md           # System architecture (Mermaid diagrams)
│   ├── deployment.md             # Deployment guide
│   ├── domain-models.md          # Domain model documentation
│   ├── database-models.md        # Database schema, ERD
│   ├── api-guide.md              # API usage guide
│   └── contributing.md           # Contribution guidelines
├── diagrams/
│   ├── architecture.mmd          # Mermaid source
│   ├── classes.md                # dll2mmd output
│   └── uml/                      # PlantUML diagrams
└── api/                          # Generated API reference (gitignored)
```

### Type 2: System User Docs

**Purpose**: User-facing documentation for end users of the system

**Target**: `docs/docfx-user/` → DocFX Static Site (User-Focused Section)

**Content**:
- **Introductions**: What the system does, who it's for
- **Domain**: Business concepts explained for users
- **Feature Sets**: Overview of features
- **Usage**: How to use features (step-by-step guides)
- **Usage Requirements**: Prerequisites, permissions, setup

**Technologies**:
- DocFX with user-friendly template (no API reference)
- Screenshots for visual guidance
- Simple Mermaid diagrams for workflows
- Tables for feature matrices
- Markdown for conceptual content

**Publishing**:
- **GitHub**: GitHub Pages at `https://yourorg.github.io/yourproject/user/` (subdirectory)
- **Azure DevOps**: Azure Static Web Apps subdomain OR separate SWA
- **Alternative**: Can be separate repository/site if needed

**Example Structure**:
```
docfx-user/
├── docfx.json                    # User-focused template, no API ref
├── index.md                      # User-friendly homepage
├── toc.yml                       # User-friendly navigation
├── articles/
│   ├── getting-started.md        # Quick start guide (screenshots)
│   ├── features.md               # Feature overview
│   ├── tutorials/
│   │   ├── task-1.md
│   │   └── task-2.md
│   └── faq.md                    # Frequently asked questions
├── images/
│   └── screenshots/              # Application screenshots
└── diagrams/
    └── user-flows.mmd            # Simple workflow diagrams
```

### Type 3: Company System Docs

**Purpose**: Living documentation about the system's status, purpose, and ongoing work

**Target**: `docs/wiki/` → GitHub Wiki OR Azure DevOps Wiki (Wiki Only)

**Content**:
- **System Purpose**: What the system does, why it exists
- **System Access**: How to get access, URLs, credentials
- **Feature Summary**: Current features, status, roadmap
- **Active Development**: What's currently being worked on

**Technologies**:
- Markdown files (GitHub Flavored Markdown)
- Simple Mermaid diagrams (e.g., Gantt charts for timelines)
- Tables for status tracking
- Screenshots

**Publishing**:
- **GitHub**: GitHub Wiki (via git push to .wiki repository)
- **Azure DevOps**: Azure DevOps Wiki (via REST API)
- **Update Frequency**: High (wiki easy to update frequently)

**Example Structure**:
```
wiki/
├── README.md                     # Wiki home page
├── system-purpose.md             # What & why
├── system-access.md              # How to access
├── feature-summary.md            # Feature status table
├── active-development.md         # Current work (NOT /dev-docs!)
└── team-contacts.md              # Who to ask for help
```

**Important Distinction**:
- `/dev-docs` system (e.g., `dev/active/`) is for **implementation planning**, NOT published documentation
- `docs/wiki/` is for **company system documentation** (published to wiki)

### Comparison Matrix

| Aspect | Developer Docs | User Docs | Company Docs |
|--------|---------------|-----------|--------------|
| **Audience** | Developers | End Users | Team/Stakeholders |
| **Update Frequency** | Medium (with code changes) | Low (with features) | High (ongoing status) |
| **Technology** | DocFX static site | DocFX static site | Wiki (Markdown) |
| **Diagrams** | Technical (UML, class, architecture) | Simple (workflows, flows) | Simple (tables, timelines) |
| **Publishing** | GitHub Pages / Azure SWA | GitHub Pages /user/ OR separate | GitHub Wiki / ADO Wiki |
| **Formality** | High (professional) | Medium (user-friendly) | Low (conversational) |
| **Automation** | CI/CD builds HTML | CI/CD builds HTML | CI/CD syncs markdown |

---

## Platform-Agnostic Core

### .docgen/ Directory

The `.docgen/` directory contains all platform-independent automation:

#### platform-config.json

Configuration for platform selection and settings:

```json
{
  "platform": "auto",
  "platforms": {
    "github": {
      "enabled": true,
      "deployment": {
        "target": "github-pages",
        "branch": "gh-pages",
        "domain": "https://yourorg.github.io/yourproject"
      },
      "workflows": {
        "build": ".github/workflows/docs-deploy.yml",
        "validation": ".github/workflows/docs-pr-validation.yml"
      }
    },
    "azuredevops": {
      "enabled": true,
      "deployment": {
        "target": "static-web-app",
        "static_web_app_name": "your-docs-swa",
        "resource_group": "your-resource-group"
      },
      "pipelines": {
        "build": ".azuredevops/pipelines/docs-deploy-swa.yml",
        "validation": ".azuredevops/pipelines/docs-pr-validation.yml"
      }
    }
  }
}
```

#### mcp-config.json

MCP server configuration for Claude Code:

```json
{
  "mcpServers": {
    "microsoft-docs": {
      "type": "http",
      "url": "https://mcp.docs.microsoft.com/mcp",
      "description": "Official Microsoft .NET documentation"
    },
    "docs-mcp-local": {
      "command": "npx",
      "args": ["-y", "@arabold/docs-mcp-server"],
      "env": {
        "OPENAI_API_KEY": "${OPENAI_API_KEY}",
        "DATA_DIR": "${DOCS_MCP_DATA_DIR}"
      }
    }
  }
}
```

#### diagram-gen.ps1

Cross-platform diagram generation:

```powershell
param(
    [string]$ProjectPath = ".",
    [string]$OutputPath = "docs/diagrams",
    [switch]$Mermaid,
    [switch]$PlantUML,
    [switch]$All
)

# Platform-agnostic script that:
# 1. Finds all compiled assemblies
# 2. Generates Mermaid class diagrams (dll2mmd)
# 3. Generates PlantUML diagrams (puml-gen)
# 4. Works on WSL2, Windows, Linux
```

#### Makefile

Cross-platform build automation:

```makefile
# Platform detection
UNAME_S := $(shell uname -s 2>/dev/null || echo Windows)
PWSH := pwsh

# Common targets
docs-build: diagrams
	dotnet restore
	dotnet build --configuration Release
	$(PWSH) .docgen/diagram-gen.ps1 -All
	docfx docs/docfx-developer/docfx.json
	docfx docs/docfx-user/docfx.json

docs-serve: docs-build
	docfx docs/docfx-developer/docfx.json --serve

diagrams:
	$(PWSH) .docgen/diagram-gen.ps1 -All

# Platform-specific deployment
deploy:
ifeq ($(GITHUB_ACTIONS),true)
	@$(MAKE) deploy-github
else ifeq ($(TF_BUILD),True)
	@$(MAKE) deploy-azure
endif
```

### DocFX Configurations

Platform-agnostic DocFX configurations:

**docs/docfx-developer/docfx.json**:
- Processes src/**/*.csproj for API metadata
- Includes api/ and articles/ content
- Mermaid diagram support enabled
- Git contribute links (set dynamically based on platform)

**docs/docfx-user/docfx.json**:
- No API metadata extraction
- Only articles/ content
- User-friendly template
- Simpler navigation structure

Both configurations are platform-independent. The git repository URLs are updated by `.docgen/switch-platform.ps1` when switching platforms.

### Directory.Build.props

XML documentation generation enabled for entire solution:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);CS1591</NoWarn>
</PropertyGroup>
```

This ensures all projects generate XML comments that DocFX consumes for API reference.

---

## Platform Plugins

### GitHub Plugin

**Location**: `.github/`

**Components**:

1. **Workflows** (`.github/workflows/`):
   - `docs-developer-deploy.yml`: Build and deploy developer docs to GitHub Pages (/)
   - `docs-user-deploy.yml`: Build and deploy user docs to GitHub Pages (/user/)
   - `docs-wiki-sync.yml`: Sync docs/wiki/ to GitHub Wiki
   - `docs-pr-validation.yml`: Validate documentation on pull requests

2. **Scripts** (`.github/scripts/`):
   - `gh-pages-deploy-developer.sh`: Deploy developer docs to gh-pages branch
   - `gh-pages-deploy-user.sh`: Deploy user docs to gh-pages branch /user/
   - `gh-wiki-sync.sh`: Sync wiki files to GitHub Wiki repository

**Hosting**:
- **GitHub Pages**: Free static site hosting
- **URL Pattern**: `https://yourorg.github.io/yourproject/` (developer), `/user/` (user)
- **GitHub Wiki**: `https://github.com/yourorg/yourproject/wiki`

**Authentication**: `GITHUB_TOKEN` (automatically provided by GitHub Actions)

**Advantages**:
- Simpler setup (one-click GitHub Pages enable)
- Free and unlimited for public repositories
- Integrated with GitHub ecosystem
- GitHub Wiki is git-based (easy to sync)

**See**: [GitHub Plugin Guide](github-plugin-guide.md) for complete implementation details.

### Azure DevOps Plugin

**Location**: `.azuredevops/`

**Components**:

1. **Pipelines** (`.azuredevops/pipelines/`):
   - `docs-developer-deploy.yml`: Build and deploy developer docs to Azure Static Web Apps
   - `docs-user-deploy.yml`: Build and deploy user docs to Azure SWA (subdomain or separate)
   - `docs-wiki-deploy.yml`: Publish docs/wiki/ to Azure DevOps Wiki via REST API
   - `docs-pr-validation.yml`: Validate documentation on pull requests

2. **Scripts** (`.azuredevops/scripts/`):
   - `ado-deploy-developer.ps1`: Deploy developer docs to Azure SWA
   - `ado-deploy-user.ps1`: Deploy user docs to Azure SWA
   - `ado-wiki-publish.ps1`: Publish to Azure DevOps Wiki via REST API

**Hosting**:
- **Azure Static Web Apps**: Professional hosting with CDN
- **URL Pattern**: `https://yourproject-docs.azurestaticapps.net/` (developer), `/user/` or separate subdomain (user)
- **Azure DevOps Wiki**: `https://dev.azure.com/org/project/_wiki`

**Authentication**: `System.AccessToken` (automatically provided by Azure Pipelines)

**Advantages**:
- Advanced features (custom domains, auth, CDN)
- Tighter integration with Azure ecosystem
- Azure DevOps Wiki has advanced features (work item linking)
- Enterprise-grade hosting

**See**: [Azure DevOps Plugin Guide](azure-devops-plugin-guide.md) for complete implementation details.

### Platform Switching

**Script**: `.docgen/switch-platform.ps1`

```powershell
param(
    [ValidateSet('GitHub', 'AzureDevOps')]
    [string]$Platform
)

# Updates:
# 1. docfx.json git URLs
# 2. Enables/disables workflows vs pipelines (rename directories)
# 3. Updates platform-config.json
# 4. Prints next steps

# Reversible: Can switch back and forth
```

**Usage**:
```bash
# Switch to GitHub
pwsh .docgen/switch-platform.ps1 -Platform GitHub

# Switch to Azure DevOps
pwsh .docgen/switch-platform.ps1 -Platform AzureDevOps
```

---

## Technology Stack

### Core Documentation Generation

| Component | Technology | Why | License |
|-----------|-----------|-----|---------|
| **API Documentation** | DocFX | .NET Foundation standard, used by Microsoft teams | MIT |
| **Conceptual Docs** | Markdown | Universal, version-controllable | N/A |
| **Diagram Language** | Mermaid | Native GitHub/DocFX support, simple syntax | MIT |
| **Class Diagrams** | dll2mmd | Automated from assemblies, Mermaid output | MIT |
| **UML Diagrams** | PlantUmlClassDiagramGenerator | Comprehensive C# support | MIT |
| **Build Automation** | Makefile + PowerShell | Cross-platform | N/A |

### AI Integration

| Component | Technology | Why | Cost |
|-----------|-----------|-----|------|
| **AI Platform** | Claude Code | Already in use, agent/skill infrastructure | Existing subscription |
| **Official .NET Docs** | Microsoft Learn MCP Server | Microsoft-hosted, official documentation | Free |
| **Project Docs Index** | Docs MCP Server | Indexes local docs + external sources | Free |
| **Library Docs** | Context7 MCP | Current version-specific library docs | Free |
| **Documentation Agent** | documentation-architect | Already installed in this project | Free |

### Diagram Generation

| Tool | Input | Output | Use Case |
|------|-------|--------|----------|
| **dll2mmd** | .NET DLLs | Mermaid class diagrams | Quick automated class diagrams |
| **PlantUmlClassDiagramGenerator** | C# source | PlantUML | Comprehensive UML with relationships |
| **Mermaid** | Markdown code blocks | SVG/PNG | Architecture, sequence, flow diagrams |
| **EF Core Power Tools** | DbContext | DGML | Database entity relationships |

### CI/CD & Hosting

#### GitHub
| Component | Technology | Cost |
|-----------|-----------|------|
| **CI/CD** | GitHub Actions | Free (unlimited for public repos) |
| **Static Hosting** | GitHub Pages | Free |
| **Wiki** | GitHub Wiki | Free |
| **Authentication** | GITHUB_TOKEN | Free (built-in) |

#### Azure DevOps
| Component | Technology | Cost |
|-----------|-----------|------|
| **CI/CD** | Azure Pipelines | Free (1,800 min/month) |
| **Static Hosting** | Azure Static Web Apps | Free tier (100 GB/month) |
| **Wiki** | Azure DevOps Wiki | Free |
| **Authentication** | System.AccessToken | Free (built-in) |

### Development Tools

| Tool | Purpose | Installation |
|------|---------|--------------|
| **.NET SDK 10** | Build, restore projects | Already installed (10.0.100-rc.2.25502.107) |
| **PowerShell Core** | Cross-platform scripting | Already installed (pwsh) |
| **Node.js** | MCP servers | Already installed (22.18.0) |
| **DocFX** | Documentation generation | `dotnet tool install -g docfx` |
| **dll2mmd** | Diagram generation | `dotnet tool install -g dll2mmd` |
| **PlantUmlClassDiagramGenerator** | UML generation | `dotnet tool install -g PlantUmlClassDiagramGenerator` |

**Total Monthly Cost**: $0 (all free tiers sufficient)

---

## Platform Comparison

### Feature Parity Matrix

| Feature | GitHub | Azure DevOps | Winner |
|---------|--------|--------------|--------|
| **Static Site Hosting** | ✅ GitHub Pages | ✅ Azure Static Web Apps | Tie |
| **Custom Domain** | ✅ Free with HTTPS | ✅ Included | Tie |
| **Wiki** | ✅ Git-based | ✅ REST API-based | GitHub (simpler) |
| **CI/CD** | ✅ GitHub Actions | ✅ Azure Pipelines | Tie |
| **Build Minutes (Free)** | Unlimited (public) | 1,800/month | GitHub |
| **PR Validation** | ✅ Status checks | ✅ Build policies | Azure DevOps (more features) |
| **Branch Protection** | ✅ Basic | ✅ Advanced policies | Azure DevOps |
| **Setup Complexity** | ✅ Low (15-30 min) | ⚠️ Medium (30-60 min) | GitHub |
| **Enterprise Features** | ⚠️ Good | ✅ Excellent | Azure DevOps |
| **Azure Integration** | ⚠️ External | ✅ Native | Azure DevOps |
| **Cost (Private Repos)** | $0-4/month | $0-40/month | GitHub |

### When to Use Each Platform

**GitHub** (Recommended for This Project):
- ✅ Repository already on GitHub
- ✅ Simplest setup (15-30 minutes)
- ✅ GitHub Pages one-click enable
- ✅ Unlimited Actions minutes (public repo)
- ✅ Open source project
- ✅ Public documentation

**Azure DevOps** (Optional, Client Work):
- ✅ Client uses Azure DevOps
- ✅ Need advanced work item tracking
- ✅ Deploying to Azure infrastructure
- ✅ Enterprise compliance requirements
- ✅ Internal documentation only

**Both** (Maximum Flexibility):
- ✅ Consultant working with multiple clients
- ✅ Want to demonstrate platform portability
- ✅ Public docs (GitHub) + internal docs (Azure DevOps)

---

## AI Integration

### MCP Server Architecture

```
Claude Code
     ↓
MCP Protocol
     ↓
  ┌──┴──┐
  ↓     ↓
Microsoft Learn MCP    Docs MCP Server    Context7 MCP
(Official .NET docs)   (Project + external) (Library docs)
     ↓                      ↓                    ↓
Generated Documentation Uses Correct Microsoft Terminology
```

### Microsoft Learn MCP Server

**Provider**: Microsoft (official)
**Endpoint**: `https://mcp.docs.microsoft.com/mcp`
**Purpose**: Official .NET, Azure, C# documentation

**Benefits**:
- No local installation (remote HTTP endpoint)
- Always up-to-date with Microsoft Learn
- Covers .NET 10, ASP.NET Core, Entity Framework, Azure
- Prevents outdated API usage

**Integration**:
```json
{
  "microsoft-docs": {
    "type": "http",
    "url": "https://mcp.docs.microsoft.com/mcp"
  }
}
```

### Docs MCP Server

**Provider**: @arabold/docs-mcp-server (open source)
**Deployment**: Local (npx) or Docker
**Purpose**: Index project documentation + external sources

**Features**:
- Semantic search with embeddings
- Index GitHub repositories
- Index Microsoft Learn (local copy)
- Index local files
- Version-specific targeting

**Use Case**: "Document this class using patterns from our existing architecture documentation"

### documentation-architect Agent

**Already Installed**: In `.claude/agents/`
**Purpose**: Comprehensive documentation generation

**Capabilities**:
- Examines codebase structure
- Creates architecture documentation
- Generates API documentation
- Writes user guides
- Creates diagrams

**Integration with MCP**:
- Agent queries MCP servers for context
- Ensures terminology consistency
- Uses official Microsoft patterns
- References current library documentation

### AI-Assisted Workflows

**Workflow 1: Generate XML Comments**
```
1. Select C# class/method
2. Prompt: "Generate XML documentation comments using Microsoft Learn .NET conventions"
3. MCP server provides official patterns
4. Claude generates <summary>, <param>, <returns>, <example> sections
5. Review and commit
```

**Workflow 2: Architecture Documentation**
```
1. Invoke documentation-architect agent
2. Prompt: "Create architecture documentation for this .NET 10 project"
3. Agent examines src/, tests/, Directory.Build.props
4. Queries MCP for .NET 10 patterns
5. Generates architecture.md with Mermaid diagrams
6. Review and refine
```

**Workflow 3: User Guide Generation**
```
1. Prompt: "Create user guide for [feature] targeting non-technical users"
2. Claude generates step-by-step guide
3. Suggests screenshots to capture
4. Creates simple Mermaid workflow diagrams
5. Review and add screenshots
```

**See**: [AI Documentation Workflows](ai-documentation-workflows.md) (to be created in Phase 8)

---

## Deployment Architecture

### GitHub Deployment Flow

```
Developer Commits to main
         ↓
GitHub Actions Workflow Triggers
         ↓
    ┌────┴────┐
    ↓         ↓         ↓
Developer   User     Wiki
Workflow    Workflow  Workflow
    ↓         ↓         ↓
Build       Build     Sync
DocFX       DocFX     Markdown
    ↓         ↓         ↓
Upload      Upload    Push to
Artifact    Artifact  Wiki Repo
    ↓         ↓         ↓
Deploy      Deploy    GitHub
Pages (/)   Pages     Wiki
           (/user/)
         ↓
https://yourorg.github.io/yourproject/
```

### Azure DevOps Deployment Flow

```
Developer Commits to main
         ↓
Azure Pipeline Triggers
         ↓
    ┌────┴────┐
    ↓         ↓         ↓
Developer   User     Wiki
Pipeline    Pipeline  Pipeline
    ↓         ↓         ↓
Build       Build     Publish
DocFX       DocFX     via REST API
    ↓         ↓         ↓
Deploy      Deploy    Azure DevOps
to SWA      to SWA    Wiki
    ↓         ↓
https://yourproject-docs.azurestaticapps.net/
```

### Hosting Architecture

**GitHub Pages**:
- Static files served from `gh-pages` branch
- Root (/) = developer docs
- /user/ = user docs
- Automatic SSL via Let's Encrypt
- Global CDN via Fastly

**Azure Static Web Apps**:
- Static files + serverless functions support
- Root (/) = developer docs
- /user/ OR separate subdomain = user docs
- Custom domain with automatic SSL
- Global CDN via Azure CDN
- Built-in authentication (optional)

---

## Migration Strategy

### Platform Detection

**Automatic Detection**:
1. In CI/CD: Check environment variables (`GITHUB_ACTIONS`, `TF_BUILD`)
2. Locally: Parse git remote URL

**Script**: `.docgen/detect-platform.ps1`

```powershell
function Get-CIPlatform {
    if ($env:GITHUB_ACTIONS -eq 'true') { return 'GitHub' }
    if ($env:TF_BUILD -eq 'True') { return 'AzureDevOps' }

    # Local: detect from git remote
    $remote = git config --get remote.origin.url
    if ($remote -match 'github.com') { return 'GitHub' }
    if ($remote -match 'dev.azure.com|visualstudio.com') { return 'AzureDevOps' }
}
```

### GitHub → Azure DevOps Migration

**Time**: 2-3 hours

**Steps**:
1. Run `.docgen/switch-platform.ps1 -Platform AzureDevOps`
2. Create Azure resources (Resource Group, Static Web Apps)
3. Import repository to Azure Repos (or keep on GitHub + Azure Pipelines)
4. Create pipelines from `.azuredevops/pipelines/*.yml`
5. Set pipeline variables (STATIC_WEB_APP_NAME, RESOURCE_GROUP)
6. Configure branch policies
7. Run pipeline to deploy

**Outcome**: Documentation now deploys to Azure Static Web Apps + Azure DevOps Wiki

### Azure DevOps → GitHub Migration

**Time**: 1-2 hours

**Steps**:
1. Run `.docgen/switch-platform.ps1 -Platform GitHub`
2. Push repository to GitHub (if not already mirrored)
3. Enable GitHub Pages (Settings → Pages → Source: gh-pages branch)
4. Enable GitHub Actions (automatically enabled)
5. Push to main to trigger workflows
6. Configure branch protection rules

**Outcome**: Documentation now deploys to GitHub Pages + GitHub Wiki

### Dual-Platform Operation

**Possible**: Yes, but not recommended (complexity without clear benefit)

**Scenario**: Public documentation on GitHub Pages, internal documentation on Azure DevOps Wiki

**Approach**:
- Keep both plugin directories active
- Don't rename/disable workflows
- Configure different triggers (e.g., main branch → GitHub, internal-docs branch → Azure DevOps)

---

## Implementation Roadmap

### Phase 1: Documentation Planning (3-4 hours)
Create comprehensive architecture documentation (5 files in docs/architecture/).

### Phase 2: Core Foundation (6-8 hours)
Establish platform-agnostic infrastructure (.docgen/, Makefile, tools).

### Phase 3: System Developer Docs (4-6 hours)
DocFX setup, architecture documentation, API reference, diagrams.

### Phase 4: System User Docs (3-4 hours)
User-friendly DocFX setup, getting started guide, screenshots.

### Phase 5: Company System Docs (2-3 hours)
Wiki markdown files, sync automation.

### Phase 6: GitHub Plugin (4-6 hours)
GitHub Actions workflows, GitHub Pages deployment, PR validation.

### Phase 7: Azure DevOps Plugin (6-8 hours, OPTIONAL)
Azure Pipelines, Static Web Apps deployment, Wiki REST API integration.

### Phase 8: AI Integration (2-3 hours)
MCP server configuration, AI workflow documentation.

### Phase 9: Platform Switching (2-3 hours)
Platform detection, switching scripts, migration guide.

**Total Time**:
- GitHub Only: 28-32 hours (Phases 1-6, 8-9)
- Both Platforms: 36-40 hours (All phases)

**See**: [Implementation Plan](ai-docs-implementation-plan.md) for step-by-step guide.

---

## References

### Internal Documentation
- [Research: AI-Assisted Documentation](../research/ai-assisted-documentation.md) - Comprehensive research (582 lines)
- [Documentation Content Strategy](documentation-content-strategy.md) - How to structure 3 doc types
- [Implementation Plan](ai-docs-implementation-plan.md) - Step-by-step implementation guide
- [GitHub Plugin Guide](github-plugin-guide.md) - Complete GitHub setup
- [Azure DevOps Plugin Guide](azure-devops-plugin-guide.md) - Complete Azure DevOps setup
- [CLAUDE.md](../../CLAUDE.md) - Project overview
- [.claude/README.md](../../.claude/README.md) - Claude Code infrastructure

### External Resources
- **DocFX**: https://dotnet.github.io/docfx/
- **MCP Protocol**: https://modelcontextprotocol.io/
- **Microsoft Learn MCP**: https://mcp.docs.microsoft.com/
- **dll2mmd**: https://github.com/cezarypiatek/dll2mmd
- **PlantUmlClassDiagramGenerator**: https://github.com/pierre3/PlantUmlClassDiagramGenerator
- **Docs MCP Server**: https://github.com/arabold/docs-mcp-server
- **GitHub Actions**: https://docs.github.com/actions
- **Azure Pipelines**: https://learn.microsoft.com/azure/devops/pipelines/
- **Azure Static Web Apps**: https://learn.microsoft.com/azure/static-web-apps/

### Dev Docs (Implementation Planning)
- [Plan](../../dev/active/ai-assisted-documentation/ai-assisted-documentation-plan.md) - Comprehensive strategic plan
- [Context](../../dev/active/ai-assisted-documentation/ai-assisted-documentation-context.md) - Key decisions, current state
- [Tasks](../../dev/active/ai-assisted-documentation/ai-assisted-documentation-tasks.md) - Checklist for progress tracking

---

**Document Status**: ✅ COMPLETE
**Version**: 1.0
**Last Updated**: 2025-11-02
**Next**: Create [Documentation Content Strategy](documentation-content-strategy.md)
