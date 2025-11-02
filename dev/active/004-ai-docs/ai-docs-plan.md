# AI-Assisted Documentation System - Implementation Plan

**Last Updated:** 2025-11-02

---

## Executive Summary

### The Goal
Implement a platform-agnostic AI-assisted documentation system that maintains three distinct types of documentation (**System Developer Docs**, **System User Docs**, and **Company System Docs**) with support for both GitHub and Azure DevOps through a plugin architecture.

### Why This Matters
- **Professional Quality**: Publication-ready documentation suitable for client deliverables
- **Platform Flexibility**: Consultant can work in any client environment (GitHub or Azure DevOps)
- **AI Integration**: Leverage Claude Code + MCP servers for intelligent documentation assistance
- **Zero Cost**: Free tiers of both platforms sufficient for most projects
- **Low Maintenance**: Automated CI/CD keeps documentation current (30-60 min/month)

### Core Architecture: Option A (Approved)
1. **System Developer Docs** → DocFX Static Site (Primary) + Optional Wiki Mirror
   - Covers: Architecture, deployment, domain models, type models, database models, interactions, system boundaries
   - Published to: GitHub Pages OR Azure Static Web Apps
   - Format: Professional HTML with API reference + Mermaid diagrams

2. **System User Docs** → DocFX Static Site (User-Focused Section)
   - Covers: Introductions, domain, feature sets, usage, usage requirements
   - Published to: Same site as developer docs (different section) OR separate site
   - Format: User-friendly HTML with screenshots, simple diagrams, tables

3. **System System Docs** → Wiki Only (Living Documentation)
   - Covers: System purpose, system access, feature summaries, active development in progress
   - Published to: GitHub Wiki OR Azure DevOps Wiki
   - Format: Markdown wiki pages, frequently updated

**Note:** `/dev-docs` system (this directory) is for implementation planning, NOT published documentation.

### Success Criteria
- ✅ All three documentation types automated and deployed
- ✅ Platform switching works (GitHub ↔ Azure DevOps)
- ✅ AI assistance via MCP servers functional
- ✅ Diagrams auto-generate from code
- ✅ PR validation enforces documentation quality
- ✅ Total cost: $0/month

### Timeline
- **GitHub Implementation**: 20-25 hours over 2-3 weeks
- **Add Azure DevOps**: +10-12 hours (1-2 weeks)
- **Total**: 30-36 hours over 3-4 weeks

---

## Current State Analysis

### What We Have
✅ **Comprehensive Research**: [docs/research/ai-assisted-documentation.md](../../../docs/research/ai-assisted-documentation.md) (582 lines)
- DocFX recommendations
- MCP server integrations
- Diagram generation tools
- Azure DevOps patterns
- Implementation phases

✅ **GitHub Repository**: NotMyself/net10-project-example
- Already on GitHub (not Azure DevOps)
- GitHub Actions workflow exists
- GitHub CLI installed

✅ **Claude Code Infrastructure**:
- WSL2 (Ubuntu) environment
- 2 hooks active (skill-activation-prompt, post-tool-use-tracker)
- 8 skills installed (including documentation-architect agent)
- MCP server support (not yet configured)
- Dev docs system operational

✅ **.NET 10 Project Structure**:
- SDK: 10.0.100-rc.2.25502.107
- Centralized Package Management (Directory.Packages.props)
- Clean solution: src/ (Example.Web, Example.API), tests/ (4 projects)
- MSTest + Playwright

✅ **Existing Documentation**:
- CLAUDE.md (comprehensive)
- README.md
- docs/BEST-PRACTICES.md
- docs/TROUBLESHOOTING.md
- docs/research/ai-assisted-documentation.md

### What We Don't Have
❌ **XML Documentation**: Not enabled in .csproj files
❌ **DocFX**: Not installed
❌ **Documentation Content**: No API docs, no architecture diagrams, minimal conceptual docs
❌ **CI/CD for Docs**: No pipelines for documentation
❌ **Diagram Tools**: dll2mmd, PlantUML generators not installed
❌ **MCP Servers**: Not configured for Claude Code
❌ **Platform Infrastructure**: No `.docgen/` directory, no platform plugins

### Technical Environment
- **OS**: Windows 11 with WSL2 (Ubuntu)
- **Git**: Repository on GitHub
- **CI/CD**: GitHub Actions available, Azure DevOps not yet set up
- **Tools**: PowerShell Core, Node.js 22.18.0, .NET 10 RC 2
- **Claude Code**: Ready with agent/skill infrastructure

### Key Decision: GitHub First, Then Azure DevOps
**Rationale**:
- Repository already on GitHub
- GitHub Pages simpler than Azure Static Web Apps
- Lower learning curve
- Can add Azure DevOps plugin later for client work

---

## Proposed Future State

### Architecture Overview

```
Platform-Agnostic Core (.docgen/)
├── DocFX Configuration (docs/docfx-{developer,user}/)
├── MCP Servers (mcp-config.json)
├── Diagram Generation (diagram-gen.ps1)
└── Cross-Platform Build (Makefile)
         ↓
    ┌────┴────┐
    ↓         ↓
GitHub Plugin  Azure DevOps Plugin
├── Workflows  ├── Pipelines
├── Scripts    ├── Scripts
└── Pages      └── Static Web Apps/Wiki
```

### Three Documentation Targets

**1. System Developer Docs** (`docs/docfx-developer/`)
```
docfx-developer/
├── docfx.json (API + conceptual)
├── index.md (Developer audience)
├── articles/
│   ├── architecture.md (Mermaid diagrams)
│   ├── deployment.md
│   ├── domain-models.md
│   ├── database-models.md
│   └── system-boundaries.md
├── diagrams/
│   ├── architecture.mmd
│   ├── classes.md (dll2mmd output)
│   └── uml/ (PlantUML)
└── _site/ (generated HTML)
```

**2. System User Docs** (`docs/docfx-user/`)
```
docfx-user/
├── docfx.json (user-focused template)
├── index.md (User audience)
├── articles/
│   ├── getting-started.md
│   ├── features.md
│   ├── tutorials/
│   └── user-guide.md
├── images/
│   └── screenshots/
└── diagrams/
    └── simple-flows.mmd
```

**3. Company System Docs** (`docs/wiki/`)
```
wiki/
├── README.md
├── system-purpose.md
├── system-access.md
├── feature-summary.md
└── active-development.md
```

### Platform Plugin Structure

**GitHub Plugin** (`.github/`)
```
.github/
├── workflows/
│   ├── docs-developer-deploy.yml → GitHub Pages /
│   ├── docs-user-deploy.yml → GitHub Pages /user/
│   ├── docs-wiki-sync.yml → GitHub Wiki
│   └── docs-pr-validation.yml
└── scripts/
    ├── gh-pages-deploy-developer.sh
    ├── gh-pages-deploy-user.sh
    └── gh-wiki-sync.sh
```

**Azure DevOps Plugin** (`.azuredevops/`)
```
.azuredevops/
├── pipelines/
│   ├── docs-developer-deploy.yml → Azure Static Web Apps
│   ├── docs-user-deploy.yml → Azure Static Web Apps (subdomain)
│   ├── docs-wiki-deploy.yml → Azure DevOps Wiki (REST API)
│   └── docs-pr-validation.yml
└── scripts/
    ├── ado-deploy-developer.ps1
    ├── ado-deploy-user.ps1
    └── ado-wiki-publish.ps1
```

### Hosting Strategy

**GitHub:**
- Developer Docs: `https://notmyself.github.io/net10-project-example/`
- User Docs: `https://notmyself.github.io/net10-project-example/user/`
- Company Docs: `https://github.com/NotMyself/net10-project-example/wiki`

**Azure DevOps (future):**
- Developer Docs: `https://net10-docs.azurestaticapps.net/`
- User Docs: `https://net10-docs.azurestaticapps.net/user/` OR separate SWA
- Company Docs: `https://dev.azure.com/org/project/_wiki`

---

## Implementation Phases

### Phase 1: Documentation Planning & Architecture (3-4 hours)

**Goal**: Create comprehensive documentation for the implementation

#### Task 1.1: Create Architecture Documentation (M)
**File**: `docs/architecture/ai-docs-platform-agnostic-architecture.md`

**Acceptance Criteria**:
- [ ] Synthesizes existing research (ai-assisted-documentation.md)
- [ ] Documents platform-agnostic core architecture
- [ ] Explains plugin pattern for GitHub and Azure DevOps
- [ ] Includes Option A strategy (3 documentation types)
- [ ] Platform comparison matrix included
- [ ] Migration guide between platforms
- [ ] Reasoning for all tool choices
- [ ] Links to referenced documentation

**Dependencies**: None

**Effort**: Medium (2 hours)

#### Task 1.2: Create Content Strategy Guide (S)
**File**: `docs/architecture/documentation-content-strategy.md`

**Acceptance Criteria**:
- [ ] Documents how to structure 3 documentation types
- [ ] Directory structure for each type
- [ ] Content guidelines for each type
- [ ] Explains when to use each documentation type
- [ ] Examples for each type

**Dependencies**: Task 1.1

**Effort**: Small (30 minutes)

#### Task 1.3: Create Implementation Plan (M)
**File**: `docs/architecture/ai-docs-implementation-plan.md`

**Acceptance Criteria**:
- [ ] Detailed step-by-step implementation guide
- [ ] All 9 phases documented
- [ ] Commands and code snippets included
- [ ] Time estimates for each phase
- [ ] Dependencies clearly marked
- [ ] Acceptance criteria for each step

**Dependencies**: Tasks 1.1, 1.2

**Effort**: Medium (1.5 hours)

#### Task 1.4: Create GitHub Plugin Guide (M)
**File**: `docs/architecture/github-plugin-guide.md`

**Acceptance Criteria**:
- [ ] Complete GitHub Actions workflows for all 3 doc types
- [ ] Deployment scripts documented
- [ ] GitHub Pages setup instructions
- [ ] GitHub Wiki sync explained
- [ ] PR validation configuration
- [ ] Branch protection setup
- [ ] Complete file examples with full YAML

**Dependencies**: Task 1.1

**Effort**: Medium (1.5 hours)

#### Task 1.5: Create Azure DevOps Plugin Guide (M)
**File**: `docs/architecture/azure-devops-plugin-guide.md`

**Acceptance Criteria**:
- [ ] Complete Azure Pipelines YAML for all 3 doc types
- [ ] Azure Static Web Apps deployment documented
- [ ] Azure DevOps Wiki REST API explained
- [ ] PR validation policies documented
- [ ] Branch policies setup
- [ ] Complete file examples with full YAML
- [ ] PowerShell scripts documented

**Dependencies**: Task 1.1

**Effort**: Medium (1.5 hours)

---

### Phase 2: Core Foundation Setup (6-8 hours)

**Goal**: Establish platform-agnostic infrastructure

#### Task 2.1: Enable XML Documentation Generation (S)
**Files**: `Directory.Build.props`

**Acceptance Criteria**:
- [ ] Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` to Directory.Build.props
- [ ] Add `<NoWarn>$(NoWarn);CS1591</NoWarn>` to suppress missing comment warnings initially
- [ ] Build succeeds without errors
- [ ] XML files generated in bin directories

**Dependencies**: None

**Effort**: Small (15 minutes)

#### Task 2.2: Create `.docgen/` Directory Structure (S)
**Directory**: `.docgen/`

**Acceptance Criteria**:
- [ ] Directory created
- [ ] All required scripts stubbed out:
  - platform-config.json
  - mcp-config.json
  - diagram-gen.ps1
  - validate-docs.ps1
  - detect-platform.ps1
  - switch-platform.ps1
  - setup-mcp.ps1
  - wiki-sync.ps1

**Dependencies**: None

**Effort**: Small (30 minutes)

#### Task 2.3: Create Platform Configuration (M)
**File**: `.docgen/platform-config.json`

**Acceptance Criteria**:
- [ ] JSON structure for GitHub and Azure DevOps platforms
- [ ] Deployment targets configured
- [ ] Workflow/pipeline paths specified
- [ ] Feature flags for each platform
- [ ] Default platform set to "auto" (detect from git remote)

**Dependencies**: Task 2.2

**Effort**: Medium (1 hour)

#### Task 2.4: Create MCP Server Configuration (M)
**Files**: `.docgen/mcp-config.json`, `.docgen/setup-mcp.ps1`

**Acceptance Criteria**:
- [ ] mcp-config.json defines Microsoft Learn, Docs MCP, Context7 servers
- [ ] setup-mcp.ps1 detects Windows/Linux
- [ ] setup-mcp.ps1 sets platform-specific paths
- [ ] setup-mcp.ps1 merges MCP config into Claude Code config
- [ ] Script tested on WSL2

**Dependencies**: Task 2.2

**Effort**: Medium (1.5 hours)

#### Task 2.5: Create Cross-Platform Makefile (M)
**File**: `Makefile`

**Acceptance Criteria**:
- [ ] Platform detection (Linux/macOS/Windows)
- [ ] Common targets: docs-build, docs-serve, docs-clean, diagrams, validate
- [ ] deploy target with CI/CD platform detection (GitHub Actions vs Azure DevOps)
- [ ] Works on WSL2 and Windows
- [ ] `make docs-build` successfully builds all documentation

**Dependencies**: Tasks 2.1-2.4

**Effort**: Medium (2 hours)

#### Task 2.6: Install DocFX Globally (S)
**Command**: `dotnet tool install -g docfx`

**Acceptance Criteria**:
- [ ] DocFX installed
- [ ] `docfx --version` works
- [ ] Accessible from PATH

**Dependencies**: None

**Effort**: Small (5 minutes)

#### Task 2.7: Install Diagram Generation Tools (S)
**Commands**:
```bash
dotnet tool install -g dll2mmd
dotnet tool install -g PlantUmlClassDiagramGenerator
```

**Acceptance Criteria**:
- [ ] dll2mmd installed
- [ ] PlantUmlClassDiagramGenerator installed
- [ ] Both tools accessible from PATH
- [ ] Test generation on Example.Web.dll works

**Dependencies**: None

**Effort**: Small (15 minutes)

#### Task 2.8: Create Diagram Generation Script (M)
**File**: `.docgen/diagram-gen.ps1`

**Acceptance Criteria**:
- [ ] Accepts parameters: ProjectPath, OutputPath, -Mermaid, -PlantUML, -All
- [ ] Finds all compiled assemblies (excluding obj/, ref/)
- [ ] Generates Mermaid class diagrams with dll2mmd
- [ ] Generates PlantUML diagrams with puml-gen
- [ ] Works on both WSL2 and Windows
- [ ] Called from Makefile `diagrams` target

**Dependencies**: Task 2.7

**Effort**: Medium (2 hours)

---

### Phase 3: System Developer Docs Setup (4-6 hours)

**Goal**: Create professional developer documentation site

#### Task 3.1: Create DocFX Developer Directory Structure (S)
**Directory**: `docs/docfx-developer/`

**Acceptance Criteria**:
- [ ] Directory created
- [ ] Subdirectories: articles/, diagrams/, images/
- [ ] index.md created (developer audience)
- [ ] toc.yml created
- [ ] .gitignore for _site/ and api/

**Dependencies**: Phase 2 complete

**Effort**: Small (30 minutes)

#### Task 3.2: Configure DocFX for Developer Docs (M)
**File**: `docs/docfx-developer/docfx.json`

**Acceptance Criteria**:
- [ ] metadata section configured for API reference (src/**/*.csproj)
- [ ] build section includes api/ and articles/
- [ ] Mermaid diagram support enabled (markdigExtensions)
- [ ] Custom template path configured (will create later)
- [ ] Git contribute links configured (platform-agnostic)
- [ ] filterConfig.yml created for public API filtering

**Dependencies**: Task 3.1

**Effort**: Medium (1.5 hours)

#### Task 3.3: Create Architecture Documentation (M)
**File**: `docs/docfx-developer/articles/architecture.md`

**Acceptance Criteria**:
- [ ] System architecture overview
- [ ] Mermaid diagram showing components
- [ ] Mermaid sequence diagram for key interactions
- [ ] Describes: ASP.NET Core MVC app, Minimal API, test projects
- [ ] References Directory.Build.props and centralized packages
- [ ] Uses AI assistance (documentation-architect agent + MCP servers)

**Dependencies**: Task 3.2, Phase 2 Task 2.4 (MCP servers)

**Effort**: Medium (2 hours, AI-assisted)

#### Task 3.4: Generate Class Diagrams (S)
**Script**: `make diagrams`

**Acceptance Criteria**:
- [ ] Mermaid class diagrams generated for Example.Web
- [ ] Mermaid class diagrams generated for Example.API
- [ ] Output in docs/docfx-developer/diagrams/
- [ ] Diagrams referenced from articles/api-guide.md

**Dependencies**: Task 3.1, Phase 2 Task 2.8

**Effort**: Small (30 minutes)

#### Task 3.5: Create Domain Models Documentation (S)
**File**: `docs/docfx-developer/articles/domain-models.md`

**Acceptance Criteria**:
- [ ] Documents key domain concepts
- [ ] References generated class diagrams
- [ ] Explains relationships
- [ ] Uses AI assistance for content

**Dependencies**: Tasks 3.3, 3.4

**Effort**: Small (1 hour, AI-assisted)

#### Task 3.6: Test Local Developer Docs Build (S)
**Command**: `make docs-developer-build` (need to add to Makefile)

**Acceptance Criteria**:
- [ ] DocFX builds without errors
- [ ] API reference generated from XML comments
- [ ] Articles render correctly
- [ ] Mermaid diagrams render
- [ ] `make docs-developer-serve` shows site at localhost:8080
- [ ] Navigation works, search works

**Dependencies**: Tasks 3.1-3.5

**Effort**: Small (30 minutes)

---

### Phase 4: System User Docs Setup (3-4 hours)

**Goal**: Create user-friendly documentation site

#### Task 4.1: Create DocFX User Directory Structure (S)
**Directory**: `docs/docfx-user/`

**Acceptance Criteria**:
- [ ] Directory created
- [ ] Subdirectories: articles/, images/screenshots/, diagrams/
- [ ] index.md created (user audience, welcoming tone)
- [ ] toc.yml created
- [ ] .gitignore for _site/

**Dependencies**: Phase 3 complete

**Effort**: Small (30 minutes)

#### Task 4.2: Configure DocFX for User Docs (M)
**File**: `docs/docfx-user/docfx.json`

**Acceptance Criteria**:
- [ ] build section (no metadata/API reference)
- [ ] User-friendly template (default modern template)
- [ ] Simpler navigation structure
- [ ] Mermaid support for simple flow diagrams
- [ ] Different branding/color scheme than developer docs (optional)

**Dependencies**: Task 4.1

**Effort**: Medium (1 hour)

#### Task 4.3: Create Getting Started Guide (M)
**File**: `docs/docfx-user/articles/getting-started.md`

**Acceptance Criteria**:
- [ ] Installation instructions
- [ ] First-time setup
- [ ] Hello World example
- [ ] Screenshots for key steps
- [ ] Simple Mermaid flow diagram
- [ ] Uses AI assistance for content

**Dependencies**: Task 4.2

**Effort**: Medium (1.5 hours, AI-assisted)

#### Task 4.4: Create Features Overview (S)
**File**: `docs/docfx-user/articles/features.md`

**Acceptance Criteria**:
- [ ] Lists main features
- [ ] Brief description of each
- [ ] Links to detailed tutorials
- [ ] Screenshots of features in action

**Dependencies**: Task 4.2

**Effort**: Small (1 hour, AI-assisted)

#### Task 4.5: Test Local User Docs Build (S)
**Command**: `make docs-user-build`

**Acceptance Criteria**:
- [ ] DocFX builds without errors
- [ ] Articles render correctly
- [ ] Screenshots display properly
- [ ] Mermaid diagrams render
- [ ] `make docs-user-serve` shows site at localhost:8081
- [ ] User-friendly navigation

**Dependencies**: Tasks 4.1-4.4

**Effort**: Small (30 minutes)

---

### Phase 5: Company System Docs Setup (2-3 hours)

**Goal**: Create wiki-based company documentation

#### Task 5.1: Create Wiki Directory Structure (S)
**Directory**: `docs/wiki/`

**Acceptance Criteria**:
- [ ] Directory created
- [ ] README.md created (wiki home page)
- [ ] Template files created:
  - system-purpose.md
  - system-access.md
  - feature-summary.md
  - active-development.md

**Dependencies**: Phase 4 complete

**Effort**: Small (30 minutes)

#### Task 5.2: Write System Purpose Documentation (S)
**File**: `docs/wiki/system-purpose.md`

**Acceptance Criteria**:
- [ ] Explains what the system does
- [ ] Target audience identified
- [ ] Key value propositions listed
- [ ] Links to developer/user docs

**Dependencies**: Task 5.1

**Effort**: Small (30 minutes, AI-assisted)

#### Task 5.3: Write System Access Documentation (S)
**File**: `docs/wiki/system-access.md`

**Acceptance Criteria**:
- [ ] How to get access (for team members)
- [ ] URLs for deployed environments
- [ ] Authentication methods
- [ ] Permissions model

**Dependencies**: Task 5.1

**Effort**: Small (30 minutes, AI-assisted)

#### Task 5.4: Write Feature Summary (S)
**File**: `docs/wiki/feature-summary.md`

**Acceptance Criteria**:
- [ ] Current feature set
- [ ] Feature status (stable, beta, planned)
- [ ] Simple table format
- [ ] Links to detailed user docs

**Dependencies**: Task 5.1

**Effort**: Small (30 minutes, AI-assisted)

#### Task 5.5: Write Active Development Documentation (S)
**File**: `docs/wiki/active-development.md`

**Acceptance Criteria**:
- [ ] What's currently being worked on
- [ ] Links to relevant dev docs (NOT /dev-docs implementation docs)
- [ ] Timeline for upcoming features
- [ ] Simple Mermaid Gantt chart (optional)

**Dependencies**: Task 5.1

**Effort**: Small (30 minutes, AI-assisted)

#### Task 5.6: Create Wiki Sync Script (M)
**File**: `.docgen/wiki-sync.ps1`

**Acceptance Criteria**:
- [ ] Copies docs/wiki/*.md to GitHub Wiki OR Azure DevOps Wiki
- [ ] Creates .order file for Azure DevOps Wiki (if applicable)
- [ ] Commits and pushes changes
- [ ] Detects platform (GitHub vs Azure DevOps)
- [ ] Works on WSL2

**Dependencies**: Tasks 5.1-5.5

**Effort**: Medium (1.5 hours)

---

### Phase 6: GitHub Plugin Implementation (4-6 hours)

**Goal**: Automate all 3 documentation types on GitHub

#### Task 6.1: Create GitHub Workflows Directory (S)
**Directory**: `.github/workflows/`

**Acceptance Criteria**:
- [ ] Directory exists (may already exist)
- [ ] Workflow files stubbed out:
  - docs-developer-deploy.yml
  - docs-user-deploy.yml
  - docs-wiki-sync.yml
  - docs-pr-validation.yml

**Dependencies**: Phase 5 complete

**Effort**: Small (15 minutes)

#### Task 6.2: Create Developer Docs Deployment Workflow (M)
**File**: `.github/workflows/docs-developer-deploy.yml`

**Acceptance Criteria**:
- [ ] Triggers on push to main, paths: docs/docfx-developer/**, src/**
- [ ] Uses ubuntu-latest runner
- [ ] Installs .NET SDK from global.json
- [ ] Installs DocFX
- [ ] Installs diagram generators
- [ ] Runs `dotnet restore` and `dotnet build`
- [ ] Generates diagrams
- [ ] Builds DocFX documentation
- [ ] Uploads pages artifact to GitHub Pages (/) root
- [ ] Deploy step with github-pages environment

**Dependencies**: Task 6.1

**Effort**: Medium (2 hours)

#### Task 6.3: Create User Docs Deployment Workflow (M)
**File**: `.github/workflows/docs-user-deploy.yml`

**Acceptance Criteria**:
- [ ] Triggers on push to main, paths: docs/docfx-user/**
- [ ] Similar steps to Task 6.2
- [ ] Builds DocFX user documentation
- [ ] Uploads pages artifact to GitHub Pages (/user/) subdirectory
- [ ] OR deploys to separate GitHub Pages site

**Dependencies**: Task 6.1

**Effort**: Medium (1.5 hours)

#### Task 6.4: Create Wiki Sync Workflow (S)
**File**: `.github/workflows/docs-wiki-sync.yml`

**Acceptance Criteria**:
- [ ] Triggers on push to main, paths: docs/wiki/**
- [ ] Checks out repository with persistCredentials
- [ ] Checks out GitHub Wiki repository
- [ ] Copies docs/wiki/*.md to wiki/
- [ ] Creates _Sidebar.md for navigation
- [ ] Commits and pushes to wiki
- [ ] Uses GITHUB_TOKEN for authentication

**Dependencies**: Task 6.1, Phase 5 Task 5.6

**Effort**: Small (1 hour)

#### Task 6.5: Create PR Validation Workflow (M)
**File**: `.github/workflows/docs-pr-validation.yml`

**Acceptance Criteria**:
- [ ] Triggers on pull_request, paths: docs/**, src/**/*.cs
- [ ] Checks XML documentation comments exist
- [ ] Runs markdownlint on all markdown files
- [ ] Builds all DocFX documentation (validation only)
- [ ] Posts comment on PR if validation fails
- [ ] Blocks merge if validation fails

**Dependencies**: Task 6.1

**Effort**: Medium (1.5 hours)

#### Task 6.6: Configure GitHub Pages (S)
**Manual Step**: GitHub UI

**Acceptance Criteria**:
- [ ] Navigate to Settings → Pages
- [ ] Source: Deploy from a branch
- [ ] Branch: gh-pages, /(root)
- [ ] Custom domain configured (optional)
- [ ] HTTPS enforced

**Dependencies**: Tasks 6.2, 6.3

**Effort**: Small (15 minutes)

#### Task 6.7: Configure Branch Protection (S)
**Manual Step**: GitHub UI

**Acceptance Criteria**:
- [ ] Navigate to Settings → Branches → Branch protection rules
- [ ] Add rule for main branch
- [ ] Require status checks: "validate / Validate Documentation"
- [ ] Require approvals: 1
- [ ] Dismiss stale reviews on push

**Dependencies**: Task 6.5

**Effort**: Small (15 minutes)

#### Task 6.8: Test Full GitHub Workflow (M)
**Action**: Create test PR and merge

**Acceptance Criteria**:
- [ ] Make documentation change
- [ ] Create PR
- [ ] PR validation runs and passes
- [ ] Merge PR
- [ ] Developer docs deploy workflow runs
- [ ] User docs deploy workflow runs (if changes)
- [ ] Wiki sync workflow runs (if changes)
- [ ] Docs visible at notmyself.github.io/net10-project-example/
- [ ] User docs at /user/ subdirectory
- [ ] Wiki updated

**Dependencies**: Tasks 6.1-6.7

**Effort**: Medium (1 hour)

---

### Phase 7: Azure DevOps Plugin Implementation (6-8 hours)

**Goal**: Implement complete Azure DevOps automation (optional, for client flexibility)

#### Task 7.1: Create Azure Resources (M)
**Azure CLI**: Create Static Web Apps

**Acceptance Criteria**:
- [ ] Azure Resource Group created
- [ ] Azure Static Web App created for developer docs
- [ ] Azure Static Web App created for user docs (or subdomain)
- [ ] Deployment tokens retrieved
- [ ] Resource names documented in platform-config.json

**Dependencies**: Phase 6 complete, Azure subscription required

**Effort**: Medium (1 hour)

#### Task 7.2: Create Azure Pipelines Directory (S)
**Directory**: `.azuredevops/pipelines/`

**Acceptance Criteria**:
- [ ] Directory created
- [ ] Pipeline files stubbed out:
  - docs-developer-deploy.yml
  - docs-user-deploy.yml
  - docs-wiki-deploy.yml
  - docs-pr-validation.yml
- [ ] scripts/ directory with PowerShell scripts

**Dependencies**: Phase 6 complete

**Effort**: Small (15 minutes)

#### Task 7.3: Create Developer Docs Pipeline (L)
**File**: `.azuredevops/pipelines/docs-developer-deploy.yml`

**Acceptance Criteria**:
- [ ] Triggers on push to main, paths: docs/docfx-developer/**, src/**
- [ ] Uses windows-latest pool (DocFX compatibility)
- [ ] Installs .NET SDK from global.json
- [ ] Installs DocFX via Chocolatey
- [ ] Installs diagram generators
- [ ] Runs `dotnet restore` and `dotnet build`
- [ ] Generates diagrams
- [ ] Builds DocFX documentation
- [ ] Publishes build artifact
- [ ] Deploy stage to Azure Static Web Apps
- [ ] Uses AzureStaticWebApp@0 task

**Dependencies**: Task 7.2

**Effort**: Large (2.5 hours)

#### Task 7.4: Create User Docs Pipeline (M)
**File**: `.azuredevops/pipelines/docs-user-deploy.yml`

**Acceptance Criteria**:
- [ ] Similar to Task 7.3
- [ ] Builds user docs
- [ ] Deploys to separate Azure Static Web App or subdomain

**Dependencies**: Task 7.2

**Effort**: Medium (1.5 hours)

#### Task 7.5: Create Wiki Deployment Pipeline (L)
**File**: `.azuredevops/pipelines/docs-wiki-deploy.yml`

**Acceptance Criteria**:
- [ ] Triggers on push to main, paths: docs/wiki/**
- [ ] Uses ubuntu-latest pool
- [ ] Authenticates with System.AccessToken
- [ ] Calls Azure DevOps Wiki REST API
- [ ] Creates/updates wiki pages
- [ ] PowerShell script: .azuredevops/scripts/ado-wiki-publish.ps1
- [ ] Handles wiki creation if doesn't exist

**Dependencies**: Task 7.2

**Effort**: Large (3 hours, complex REST API interaction)

#### Task 7.6: Create PR Validation Pipeline (M)
**File**: `.azuredevops/pipelines/docs-pr-validation.yml`

**Acceptance Criteria**:
- [ ] Triggers on PR, paths: docs/**, src/**/*.cs
- [ ] Checks XML documentation
- [ ] Runs markdownlint
- [ ] Builds all DocFX documentation (validation)
- [ ] Configured as build validation policy

**Dependencies**: Task 7.2

**Effort**: Medium (1.5 hours)

#### Task 7.7: Configure Branch Policies (S)
**Manual Step**: Azure DevOps UI

**Acceptance Criteria**:
- [ ] Navigate to Repos → Branches → main → Branch policies
- [ ] Add build validation policy
- [ ] Select docs-pr-validation.yml pipeline
- [ ] Require approvals: 1
- [ ] Check for linked work items (optional)

**Dependencies**: Task 7.6

**Effort**: Small (15 minutes)

#### Task 7.8: Test Azure DevOps Workflow (M)
**Action**: Create test PR and merge

**Acceptance Criteria**:
- [ ] Make documentation change
- [ ] Create PR in Azure DevOps
- [ ] PR validation runs and passes
- [ ] Merge PR
- [ ] Developer docs pipeline runs and deploys
- [ ] User docs pipeline runs and deploys
- [ ] Wiki pipeline runs and publishes
- [ ] Docs visible at Azure Static Web Apps URL
- [ ] Wiki visible in Azure DevOps

**Dependencies**: Tasks 7.1-7.7

**Effort**: Medium (1.5 hours)

---

### Phase 8: AI Integration & Workflows (2-3 hours)

**Goal**: Enable AI-assisted documentation generation

#### Task 8.1: Run MCP Server Setup Script (S)
**Script**: `.docgen/setup-mcp.ps1`

**Acceptance Criteria**:
- [ ] Script runs without errors on WSL2
- [ ] Claude Code config updated with MCP servers
- [ ] Microsoft Learn MCP server configured
- [ ] Docs MCP server configured (optional)
- [ ] Context7 MCP server configured (optional)
- [ ] Verify MCP servers connect in Claude Code

**Dependencies**: Phase 2 Task 2.4

**Effort**: Small (30 minutes)

#### Task 8.2: Test AI-Assisted API Documentation (S)
**Workflow**: Generate XML comments with AI

**Acceptance Criteria**:
- [ ] Select a C# class (e.g., HomeController)
- [ ] Prompt Claude: "Generate XML documentation comments for this class using Microsoft Learn conventions"
- [ ] AI generates comments using MCP server context
- [ ] Comments use correct .NET terminology
- [ ] Include <summary>, <param>, <returns>, <example> sections
- [ ] Commit generated comments

**Dependencies**: Task 8.1

**Effort**: Small (30 minutes)

#### Task 8.3: Test documentation-architect Agent (M)
**Workflow**: Generate architecture documentation

**Acceptance Criteria**:
- [ ] Invoke documentation-architect agent
- [ ] Prompt: "Create architecture documentation for this .NET 10 project explaining the MVC app, Minimal API, test structure, and centralized package management"
- [ ] Agent examines codebase
- [ ] Generates comprehensive architecture.md
- [ ] Includes Mermaid diagrams
- [ ] Uses correct Microsoft terminology (via MCP)
- [ ] Review and refine output

**Dependencies**: Task 8.1

**Effort**: Medium (1 hour)

#### Task 8.4: Document AI Workflows (S)
**File**: `docs/architecture/ai-documentation-workflows.md` (new)

**Acceptance Criteria**:
- [ ] Documents how to use AI for each doc type
- [ ] Workflow 1: API documentation (XML comments)
- [ ] Workflow 2: Architecture documentation
- [ ] Workflow 3: User guide generation
- [ ] Workflow 4: Feature summaries for company docs
- [ ] Example prompts for each workflow
- [ ] Best practices for AI-assisted documentation

**Dependencies**: Tasks 8.1-8.3

**Effort**: Small (1 hour)

---

### Phase 9: Platform Switching & Testing (2-3 hours)

**Goal**: Enable platform portability and validate everything works

#### Task 9.1: Implement Platform Detection Script (M)
**File**: `.docgen/detect-platform.ps1`

**Acceptance Criteria**:
- [ ] Function: Get-CIPlatform (detects GitHub Actions, Azure DevOps, Local)
- [ ] Function: Get-PlatformConfig (loads from platform-config.json)
- [ ] Auto-detection from git remote URL when local
- [ ] Returns appropriate config for detected platform
- [ ] Works on WSL2

**Dependencies**: Phase 2 Task 2.3

**Effort**: Medium (1.5 hours)

#### Task 9.2: Implement Platform Switching Script (M)
**File**: `.docgen/switch-platform.ps1`

**Acceptance Criteria**:
- [ ] Parameter: -Platform (GitHub | AzureDevOps)
- [ ] Updates docfx.json git contribute URLs
- [ ] Enables/disables workflows vs pipelines (rename directories)
- [ ] Updates platform-config.json
- [ ] Prints next steps for user
- [ ] Reversible (can switch back)

**Dependencies**: Task 9.1

**Effort**: Medium (1.5 hours)

#### Task 9.3: Test Platform Switching (S)
**Action**: Switch between platforms

**Acceptance Criteria**:
- [ ] Run `.docgen/switch-platform.ps1 -Platform GitHub`
- [ ] Verify .github/workflows/ enabled, .azuredevops/pipelines/ disabled
- [ ] Run `.docgen/switch-platform.ps1 -Platform AzureDevOps`
- [ ] Verify .azuredevops/pipelines/ enabled, .github/workflows/ disabled
- [ ] Switch back to GitHub
- [ ] Verify configuration correct

**Dependencies**: Task 9.2

**Effort**: Small (30 minutes)

#### Task 9.4: Comprehensive End-to-End Test (M)
**Action**: Validate entire system

**Acceptance Criteria**:
- [ ] All three doc types build locally (make docs-build)
- [ ] GitHub workflows all succeed
- [ ] Azure DevOps pipelines all succeed (if implemented)
- [ ] Platform switching works both directions
- [ ] AI assistance works for all doc types
- [ ] Diagrams generate correctly
- [ ] PR validation catches issues
- [ ] Documentation quality is professional

**Dependencies**: All previous phases

**Effort**: Medium (1.5 hours)

#### Task 9.5: Create Migration Guide (S)
**File**: `docs/architecture/platform-migration-guide.md` (new)

**Acceptance Criteria**:
- [ ] Documents how to migrate GitHub → Azure DevOps
- [ ] Documents how to migrate Azure DevOps → GitHub
- [ ] Step-by-step commands
- [ ] Time estimates
- [ ] Troubleshooting common issues

**Dependencies**: Tasks 9.1-9.4

**Effort**: Small (1 hour)

---

## Risk Assessment and Mitigation Strategies

### Risk 1: DocFX .NET 10 RC Compatibility
**Probability**: Low
**Impact**: Medium
**Mitigation**:
- DocFX processes compiled assemblies (IL), not source code
- .NET RC 2 assemblies are standard .NET assemblies
- Fallback: Build documentation against .NET 8 assemblies if issues arise
- Test immediately in Phase 3

### Risk 2: Empty Documentation Initially
**Probability**: High (current state)
**Impact**: Low (expected)
**Mitigation**:
- Use AI assistance to generate initial content quickly
- Incremental approach: document one module at a time
- Prioritize System Developer Docs first (most important)

### Risk 3: GitHub Actions Minutes Exhaustion
**Probability**: Low (small project)
**Impact**: Medium (docs won't deploy)
**Mitigation**:
- Cache NuGet packages aggressively
- Trigger only on docs/** and src/** changes (path filters)
- Use public repo for unlimited minutes
- Monitor usage in GitHub UI

### Risk 4: MCP Server Configuration Complexity
**Probability**: Medium (new to user)
**Impact**: Low (docs work without MCP)
**Mitigation**:
- Start with simplest MCP server (Microsoft Learn)
- MCP is enhancement, not requirement
- Detailed setup instructions in Phase 8
- Test incrementally

### Risk 5: Azure DevOps Wiki REST API Complexity
**Probability**: Medium
**Impact**: Medium
**Mitigation**:
- Wiki deployment is optional
- Can use Azure Static Web Apps only
- Phase 7 is optional (GitHub works standalone)
- Extensive PowerShell scripting experience available

### Risk 6: Maintenance Burden (Documentation Drift)
**Probability**: Medium (if not automated)
**Impact**: Medium (outdated docs worse than no docs)
**Mitigation**:
- Automate everything possible in CI/CD
- Use PR validation to enforce documentation
- StyleCop Analyzers can enforce XML comments (Phase 4, optional)
- Pre-commit hooks for validation (future enhancement)

### Risk 7: Time Overrun
**Probability**: Medium
**Impact**: Low (flexible timeline)
**Mitigation**:
- Phases are independent, can pause between phases
- GitHub implementation (Phases 1-6) sufficient for initial value
- Azure DevOps (Phase 7) is optional
- Adjust scope if needed (e.g., skip custom templates initially)

### Risk 8: Diagram Quality Issues
**Probability**: Low
**Impact**: Low
**Mitigation**:
- Multiple diagram tools available (Mermaid, PlantUML, dll2mmd)
- Can manually create diagrams if automation insufficient
- Mermaid has broad support and good defaults

---

## Success Metrics

### Immediate Success (After Phase 6)
- [ ] Developer docs deployed to GitHub Pages
- [ ] User docs deployed to GitHub Pages (/user/)
- [ ] Company docs synced to GitHub Wiki
- [ ] All three doc types auto-update on commit to main
- [ ] PR validation working
- [ ] AI assistance via MCP servers functional

### 1 Month Success
- [ ] All public APIs have XML documentation comments
- [ ] Architecture documentation complete with diagrams
- [ ] User guide with screenshots available
- [ ] Company wiki actively maintained
- [ ] Zero documentation-related PR failures

### 3 Month Success
- [ ] Documentation cited as project strength
- [ ] Contributors actively use AI-assisted workflows
- [ ] Platform switching tested and documented
- [ ] Azure DevOps plugin implemented (if needed for clients)
- [ ] Maintenance burden < 1 hour/month

### Qualitative Success
- [ ] Documentation is professional quality (suitable for client deliverables)
- [ ] Diagrams are accurate and helpful
- [ ] Users can find answers in documentation
- [ ] Developers can understand system from documentation alone
- [ ] Confidence in using with clients

---

## Required Resources and Dependencies

### Technical Resources
- **Development Environment**: Windows 11 + WSL2 (Ubuntu) ✅ Available
- **.NET SDK**: 10.0.100-rc.2.25502.107 ✅ Available
- **PowerShell Core**: For cross-platform scripts ✅ Available
- **Node.js**: For MCP servers ✅ Available (22.18.0)
- **Git**: Version control ✅ Available
- **Docker**: For PlantUML rendering (optional) ⚠️ Check availability
- **Azure Subscription**: For Phase 7 only ⚠️ Required for Azure DevOps plugin

### Tools to Install
- **DocFX**: .NET global tool ❌ To install (Phase 2)
- **dll2mmd**: .NET global tool ❌ To install (Phase 2)
- **PlantUmlClassDiagramGenerator**: .NET global tool ❌ To install (Phase 2)
- **markdownlint-cli**: npm package ❌ To install (Phase 6)

### Cloud Resources (Free Tiers)
- **GitHub Pages**: ✅ Available (repository already on GitHub)
- **GitHub Actions**: ✅ Available (unlimited for public repos)
- **GitHub Wiki**: ✅ Available
- **Azure Static Web Apps**: ⚠️ Optional (Phase 7), Free tier available
- **Azure DevOps**: ⚠️ Optional (Phase 7), Free for small teams

### Knowledge Resources
- **Existing Research**: docs/research/ai-assisted-documentation.md ✅ Available
- **DocFX Documentation**: https://dotnet.github.io/docfx/ ✅ Public
- **MCP Protocol**: https://modelcontextprotocol.io/ ✅ Public
- **GitHub Actions Docs**: https://docs.github.com/actions ✅ Public
- **Azure Pipelines Docs**: https://learn.microsoft.com/azure/devops/pipelines/ ✅ Public

### External Dependencies
- **MCP Servers**:
  - Microsoft Learn MCP: https://mcp.docs.microsoft.com/mcp ✅ Available (Microsoft-hosted)
  - Docs MCP Server: npm package ✅ Available
  - Context7 MCP: npm package ✅ Available
- **GitHub API**: For Wiki operations ✅ Available
- **Azure DevOps REST API**: For Wiki operations ⚠️ Phase 7 only

### Skills Required
- **.NET Development**: ✅ Available
- **PowerShell Scripting**: ✅ Available
- **YAML Configuration**: ✅ Available (existing GitHub Actions)
- **DocFX**: ⚠️ Will learn (well-documented)
- **MCP Server Configuration**: ⚠️ Will learn (new, but straightforward)
- **Azure DevOps Pipelines**: ⚠️ Phase 7 only, will learn

---

## Timeline Estimates

### Aggressive Timeline (Focus, No Interruptions)
- **Week 1**: Phases 1-3 (Documentation planning + Core foundation + Developer docs)
  - Mon-Tue: Phase 1 (4 hours)
  - Wed-Thu: Phase 2 (8 hours)
  - Fri: Phase 3 start (4 hours)
- **Week 2**: Phases 3-5 (Complete developer docs, User docs, Company docs)
  - Mon: Phase 3 complete (2 hours)
  - Tue-Wed: Phase 4 (4 hours)
  - Thu: Phase 5 (3 hours)
  - Fri: Buffer
- **Week 3**: Phase 6 (GitHub plugin)
  - Mon-Wed: Phase 6 (6 hours)
  - Thu: Phase 8 (AI integration, 3 hours)
  - Fri: Phase 9 partial (platform switching, 2 hours)
- **Week 4** (Optional): Phase 7 (Azure DevOps plugin)
  - Mon-Wed: Phase 7 (8 hours)
  - Thu: Phase 9 complete (1 hour)
  - Fri: Buffer and refinement

**Total: 3-4 weeks, 30-36 hours**

### Relaxed Timeline (Part-Time, With Interruptions)
- **Week 1-2**: Phases 1-2
- **Week 3-4**: Phases 3-4
- **Week 5**: Phase 5
- **Week 6-7**: Phase 6
- **Week 8**: Phase 8-9
- **Week 9-10** (Optional): Phase 7

**Total: 8-10 weeks, 30-36 hours**

### Minimum Viable Product (MVP)
Focus on GitHub only, defer Azure DevOps:
- **Week 1**: Phases 1-2 (12 hours)
- **Week 2**: Phases 3-4 (8 hours)
- **Week 3**: Phase 6 (GitHub plugin, 6 hours)

**Total: 3 weeks, 26 hours for GitHub-only implementation**

### Phase-by-Phase Estimates (Detailed)
| Phase | Description | Min Hours | Max Hours | Avg Hours |
|-------|-------------|-----------|-----------|-----------|
| 1 | Documentation Planning | 3 | 4 | 3.5 |
| 2 | Core Foundation | 6 | 8 | 7 |
| 3 | Developer Docs | 4 | 6 | 5 |
| 4 | User Docs | 3 | 4 | 3.5 |
| 5 | Company Docs | 2 | 3 | 2.5 |
| 6 | GitHub Plugin | 4 | 6 | 5 |
| 7 | Azure DevOps Plugin | 6 | 8 | 7 |
| 8 | AI Integration | 2 | 3 | 2.5 |
| 9 | Platform Switching | 2 | 3 | 2.5 |
| **Total** | | **32** | **45** | **38.5** |

**Recommendation**: Plan for 36-40 hours over 3-4 weeks for complete implementation (both platforms).

---

## Dependencies Graph

```
Phase 1 (Documentation)
└─> Phase 2 (Core Foundation)
    ├─> Phase 3 (Developer Docs)
    │   └─> Phase 4 (User Docs)
    │       └─> Phase 5 (Company Docs)
    │           └─> Phase 6 (GitHub Plugin)
    │               ├─> Phase 8 (AI Integration)
    │               │   └─> Phase 9 (Platform Switching)
    │               └─> Phase 7 (Azure DevOps Plugin - Optional)
    │                   └─> Phase 9 (Platform Switching)
    └─> Phase 8 (AI Integration, partial - MCP setup can happen early)
```

**Critical Path**: Phases 1 → 2 → 3 → 4 → 5 → 6 → 9 (GitHub only, 28-32 hours)
**Full Path**: Add Phase 7 → 9 (both platforms, 36-40 hours)

---

## Next Steps

### Immediate Actions (Today)
1. ✅ Create dev docs structure (this file)
2. ⏳ Create context.md file
3. ⏳ Create tasks.md file
4. Review this plan with user
5. Begin Phase 1 (Documentation Planning)

### This Week
- Complete Phase 1 (create 5 architecture documents)
- Begin Phase 2 (core foundation setup)
- Install tools (DocFX, diagram generators)

### Questions to Resolve
- [ ] Is Docker available for PlantUML rendering? (Optional, can skip if not)
- [ ] Do we have an Azure subscription for Phase 7? (Optional phase)
- [ ] What's the priority: Speed (GitHub only) or Completeness (both platforms)?
- [ ] Should we use a custom DocFX template from the start, or use defaults initially?

---

## Appendix: Key File Locations

### Dev Docs (This Implementation)
- `dev/active/004-ai-docs/ai-docs-plan.md` (this file)
- `dev/active/004-ai-docs/ai-docs-context.md`
- `dev/active/004-ai-docs/ai-docs-tasks.md`

### Architecture Documentation (To Be Created)
- `docs/architecture/ai-docs-platform-agnostic-architecture.md`
- `docs/architecture/documentation-content-strategy.md`
- `docs/architecture/ai-docs-implementation-plan.md`
- `docs/architecture/github-plugin-guide.md`
- `docs/architecture/azure-devops-plugin-guide.md`

### Research & Reference
- `docs/research/ai-assisted-documentation.md` (existing, 582 lines)
- `CLAUDE.md` (project overview)
- `.claude/README.md` (Claude Code infrastructure)

### Core Documentation Directories (To Be Created)
- `docs/docfx-developer/` (System Developer Docs)
- `docs/docfx-user/` (System User Docs)
- `docs/wiki/` (Company System Docs)

### Platform-Agnostic Core (To Be Created)
- `.docgen/` (automation scripts)
- `Makefile` (cross-platform build)

### Platform Plugins (To Be Created)
- `.github/workflows/` (GitHub plugin)
- `.azuredevops/pipelines/` (Azure DevOps plugin)

---

**Plan Status**: ✅ COMPLETE
**Ready to Begin**: YES
**First Task**: Begin Phase 1 implementation

