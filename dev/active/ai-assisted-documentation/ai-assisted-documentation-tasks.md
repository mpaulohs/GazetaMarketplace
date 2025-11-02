# AI-Assisted Documentation System - Task Checklist

**Last Updated:** 2025-11-02

---

## Quick Status Overview

| Phase | Description | Status | Est. Hours | Actual Hours |
|-------|-------------|--------|------------|--------------|
| 1 | Documentation Planning | ⏳ NOT STARTED | 3-4 | - |
| 2 | Core Foundation | ⏳ NOT STARTED | 6-8 | - |
| 3 | Developer Docs Setup | ⏳ NOT STARTED | 4-6 | - |
| 4 | User Docs Setup | ⏳ NOT STARTED | 3-4 | - |
| 5 | Company Docs Setup | ⏳ NOT STARTED | 2-3 | - |
| 6 | GitHub Plugin | ⏳ NOT STARTED | 4-6 | - |
| 7 | Azure DevOps Plugin | ⏳ NOT STARTED (OPTIONAL) | 6-8 | - |
| 8 | AI Integration | ⏳ NOT STARTED | 2-3 | - |
| 9 | Platform Switching | ⏳ NOT STARTED | 2-3 | - |

**Legend**: ✅ COMPLETE | 🟡 IN PROGRESS | ⏳ NOT STARTED

---

## Phase 1: Documentation Planning & Architecture ⏳ NOT STARTED

**Goal**: Extract architecture content from plan.md into separate documentation files
**Estimated**: 3-4 hours
**Note**: All content already exists in ai-assisted-documentation-plan.md, this phase extracts and organizes it

### Tasks

- [ ] **Task 1.1**: Create Architecture Documentation (M - 2 hours)
  - File: `docs/architecture/ai-docs-platform-agnostic-architecture.md`
  - Extract architecture sections from plan.md
  - Add platform-agnostic core architecture details
  - Include plugin pattern explanation
  - Add platform comparison matrix
  - Include migration guide between platforms

- [ ] **Task 1.2**: Create Content Strategy Guide (S - 30 min)
  - File: `docs/architecture/documentation-content-strategy.md`
  - Extract content strategy from plan.md
  - Document how to structure 3 documentation types
  - Add directory structure for each type
  - Include content guidelines and examples

- [ ] **Task 1.3**: Create Implementation Plan (M - 1.5 hours)
  - File: `docs/architecture/ai-docs-implementation-plan.md`
  - Extract implementation phases from plan.md
  - Add detailed step-by-step instructions
  - Include commands and code snippets
  - Add acceptance criteria for each step

- [ ] **Task 1.4**: Create GitHub Plugin Guide (M - 1.5 hours)
  - File: `docs/architecture/github-plugin-guide.md`
  - Extract GitHub workflows from plan.md
  - Add complete GitHub Actions YAML
  - Include deployment scripts
  - Add setup instructions and examples

- [ ] **Task 1.5**: Create Azure DevOps Plugin Guide (M - 1.5 hours)
  - File: `docs/architecture/azure-devops-plugin-guide.md`
  - Extract Azure DevOps pipelines from plan.md
  - Add complete Azure Pipelines YAML
  - Include PowerShell deployment scripts
  - Add Azure resources setup guide

**Phase 1 Complete When:**
- [ ] All 5 documentation files created in `docs/architecture/`
- [ ] Content extracted and organized from plan.md
- [ ] Files reviewed and approved
- [ ] Clear understanding of implementation approach

---

## Phase 2: Core Foundation Setup ⏳ NOT STARTED

**Goal**: Establish platform-agnostic infrastructure
**Estimated**: 6-8 hours
**Dependencies**: Phase 1 complete

### Tasks

- [ ] **Task 2.1**: Enable XML Documentation Generation (S - 15 min)
  - File: `Directory.Build.props`
  - Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
  - Add `<NoWarn>$(NoWarn);CS1591</NoWarn>`
  - Verify build succeeds
  - Verify XML files generated

- [ ] **Task 2.2**: Create `.docgen/` Directory Structure (S - 30 min)
  - Create directory
  - Stub out all scripts (platform-config.json, mcp-config.json, *.ps1)

- [ ] **Task 2.3**: Create Platform Configuration (M - 1 hour)
  - File: `.docgen/platform-config.json`
  - JSON structure for GitHub and Azure DevOps
  - Deployment targets configured
  - Feature flags

- [ ] **Task 2.4**: Create MCP Server Configuration (M - 1.5 hours)
  - Files: `.docgen/mcp-config.json`, `.docgen/setup-mcp.ps1`
  - Define Microsoft Learn, Docs MCP, Context7
  - Platform-specific paths (Windows/Linux)
  - Test on WSL2

- [ ] **Task 2.5**: Create Cross-Platform Makefile (M - 2 hours)
  - File: `Makefile`
  - Platform detection
  - Targets: docs-build, docs-serve, docs-clean, diagrams, validate, deploy
  - Test on WSL2

- [ ] **Task 2.6**: Install DocFX Globally (S - 5 min)
  - Command: `dotnet tool install -g docfx`
  - Verify: `docfx --version`

- [ ] **Task 2.7**: Install Diagram Generation Tools (S - 15 min)
  - Install dll2mmd: `dotnet tool install -g dll2mmd`
  - Install PlantUmlClassDiagramGenerator
  - Test on Example.Web.dll

- [ ] **Task 2.8**: Create Diagram Generation Script (M - 2 hours)
  - File: `.docgen/diagram-gen.ps1`
  - Accepts parameters: ProjectPath, OutputPath, -Mermaid, -PlantUML, -All
  - Finds assemblies, generates diagrams
  - Works on WSL2 and Windows

**Phase 2 Complete When:**
- [ ] All tools installed and verified
- [ ] `.docgen/` directory with all scripts created
- [ ] Makefile works: `make docs-build`
- [ ] Diagram generation works: `make diagrams`
- [ ] XML documentation enabled in Directory.Build.props

---

## Phase 3: System Developer Docs Setup ⏳ NOT STARTED

**Goal**: Create professional developer documentation site
**Estimated**: 4-6 hours
**Dependencies**: Phase 2 complete

### Tasks

- [ ] **Task 3.1**: Create DocFX Developer Directory Structure (S - 30 min)
  - Directory: `docs/docfx-developer/`
  - Subdirectories: articles/, diagrams/, images/
  - Create index.md, toc.yml
  - .gitignore for _site/, api/

- [ ] **Task 3.2**: Configure DocFX for Developer Docs (M - 1.5 hours)
  - File: `docs/docfx-developer/docfx.json`
  - metadata section for API reference
  - build section for api/ and articles/
  - Mermaid support enabled
  - filterConfig.yml for public API

- [ ] **Task 3.3**: Create Architecture Documentation (M - 2 hours, AI-assisted)
  - File: `docs/docfx-developer/articles/architecture.md`
  - System architecture overview
  - Mermaid diagrams (components, sequence)
  - Describes MVC app, Minimal API, tests
  - Use documentation-architect agent + MCP servers

- [ ] **Task 3.4**: Generate Class Diagrams (S - 30 min)
  - Command: `make diagrams`
  - Mermaid class diagrams for Example.Web, Example.API
  - Output to docs/docfx-developer/diagrams/

- [ ] **Task 3.5**: Create Domain Models Documentation (S - 1 hour, AI-assisted)
  - File: `docs/docfx-developer/articles/domain-models.md`
  - Documents key domain concepts
  - References generated class diagrams

- [ ] **Task 3.6**: Test Local Developer Docs Build (S - 30 min)
  - Command: `make docs-developer-build`
  - Verify DocFX builds without errors
  - `make docs-developer-serve` → localhost:8080
  - Check navigation, search, diagrams

**Phase 3 Complete When:**
- [ ] Developer docs build locally without errors
- [ ] API reference generated from XML comments
- [ ] Articles render correctly with Mermaid diagrams
- [ ] Navigation and search work

---

## Phase 4: System User Docs Setup ⏳ NOT STARTED

**Goal**: Create user-friendly documentation site
**Estimated**: 3-4 hours
**Dependencies**: Phase 3 complete

### Tasks

- [ ] **Task 4.1**: Create DocFX User Directory Structure (S - 30 min)
  - Directory: `docs/docfx-user/`
  - Subdirectories: articles/, images/screenshots/, diagrams/
  - Create index.md (user audience), toc.yml

- [ ] **Task 4.2**: Configure DocFX for User Docs (M - 1 hour)
  - File: `docs/docfx-user/docfx.json`
  - build section (no API reference)
  - User-friendly template
  - Mermaid support for flow diagrams

- [ ] **Task 4.3**: Create Getting Started Guide (M - 1.5 hours, AI-assisted)
  - File: `docs/docfx-user/articles/getting-started.md`
  - Installation instructions
  - Screenshots for key steps
  - Simple Mermaid flow diagram

- [ ] **Task 4.4**: Create Features Overview (S - 1 hour, AI-assisted)
  - File: `docs/docfx-user/articles/features.md`
  - Lists main features
  - Screenshots of features
  - Links to tutorials

- [ ] **Task 4.5**: Test Local User Docs Build (S - 30 min)
  - Command: `make docs-user-build`
  - Verify builds without errors
  - `make docs-user-serve` → localhost:8081
  - Check screenshots display, diagrams render

**Phase 4 Complete When:**
- [ ] User docs build locally without errors
- [ ] Articles render with user-friendly tone
- [ ] Screenshots display properly
- [ ] Navigation is simple and clear

---

## Phase 5: Company System Docs Setup ⏳ NOT STARTED

**Goal**: Create wiki-based company documentation
**Estimated**: 2-3 hours
**Dependencies**: Phase 4 complete

### Tasks

- [ ] **Task 5.1**: Create Wiki Directory Structure (S - 30 min)
  - Directory: `docs/wiki/`
  - Create README.md (wiki home)
  - Template files: system-purpose.md, system-access.md, feature-summary.md, active-development.md

- [ ] **Task 5.2**: Write System Purpose Documentation (S - 30 min, AI-assisted)
  - File: `docs/wiki/system-purpose.md`
  - Explains what system does
  - Target audience
  - Key value propositions

- [ ] **Task 5.3**: Write System Access Documentation (S - 30 min, AI-assisted)
  - File: `docs/wiki/system-access.md`
  - How to get access
  - URLs for environments
  - Authentication methods

- [ ] **Task 5.4**: Write Feature Summary (S - 30 min, AI-assisted)
  - File: `docs/wiki/feature-summary.md`
  - Current feature set
  - Feature status table
  - Links to detailed docs

- [ ] **Task 5.5**: Write Active Development Documentation (S - 30 min, AI-assisted)
  - File: `docs/wiki/active-development.md`
  - What's currently being worked on
  - Timeline for upcoming features
  - Optional Mermaid Gantt chart

- [ ] **Task 5.6**: Create Wiki Sync Script (M - 1.5 hours)
  - File: `.docgen/wiki-sync.ps1`
  - Copies docs/wiki/*.md to GitHub/Azure DevOps Wiki
  - Platform detection
  - Works on WSL2

**Phase 5 Complete When:**
- [ ] All wiki markdown files created
- [ ] Content is clear and concise
- [ ] Wiki sync script works locally

---

## Phase 6: GitHub Plugin Implementation ⏳ NOT STARTED

**Goal**: Automate all 3 documentation types on GitHub
**Estimated**: 4-6 hours
**Dependencies**: Phase 5 complete

### Tasks

- [ ] **Task 6.1**: Create GitHub Workflows Directory (S - 15 min)
  - Directory: `.github/workflows/` (may exist)
  - Stub out workflow files

- [ ] **Task 6.2**: Create Developer Docs Deployment Workflow (M - 2 hours)
  - File: `.github/workflows/docs-developer-deploy.yml`
  - Trigger on push to main (paths: docs/docfx-developer/**, src/**)
  - Install .NET, DocFX, diagram tools
  - Build docs, upload to GitHub Pages (/)

- [ ] **Task 6.3**: Create User Docs Deployment Workflow (M - 1.5 hours)
  - File: `.github/workflows/docs-user-deploy.yml`
  - Similar to Task 6.2
  - Upload to GitHub Pages (/user/)

- [ ] **Task 6.4**: Create Wiki Sync Workflow (S - 1 hour)
  - File: `.github/workflows/docs-wiki-sync.yml`
  - Trigger on push to main (paths: docs/wiki/**)
  - Sync to GitHub Wiki

- [ ] **Task 6.5**: Create PR Validation Workflow (M - 1.5 hours)
  - File: `.github/workflows/docs-pr-validation.yml`
  - Check XML comments, run markdownlint
  - Build all docs (validation)
  - Post comment on PR if fails

- [ ] **Task 6.6**: Configure GitHub Pages (S - 15 min)
  - Manual: GitHub UI → Settings → Pages
  - Source: gh-pages branch
  - Verify deployment

- [ ] **Task 6.7**: Configure Branch Protection (S - 15 min)
  - Manual: GitHub UI → Settings → Branches
  - Require status checks
  - Require approvals

- [ ] **Task 6.8**: Test Full GitHub Workflow (M - 1 hour)
  - Create test PR
  - Verify validation runs
  - Merge PR
  - Verify deployments
  - Check docs at GitHub Pages URL

**Phase 6 Complete When:**
- [ ] All GitHub Actions workflows created and working
- [ ] Docs deployed to GitHub Pages
- [ ] Wiki synced to GitHub Wiki
- [ ] PR validation blocks bad PRs
- [ ] Branch protection configured

---

## Phase 7: Azure DevOps Plugin Implementation ⏳ NOT STARTED (OPTIONAL)

**Goal**: Implement complete Azure DevOps automation
**Estimated**: 6-8 hours
**Dependencies**: Phase 6 complete, Azure subscription required

### Tasks

- [ ] **Task 7.1**: Create Azure Resources (M - 1 hour)
  - Create Resource Group
  - Create Static Web App for developer docs
  - Create Static Web App for user docs
  - Retrieve deployment tokens

- [ ] **Task 7.2**: Create Azure Pipelines Directory (S - 15 min)
  - Directory: `.azuredevops/pipelines/`
  - Stub out pipeline files
  - Create scripts/ directory

- [ ] **Task 7.3**: Create Developer Docs Pipeline (L - 2.5 hours)
  - File: `.azuredevops/pipelines/docs-developer-deploy.yml`
  - Trigger, build, deploy to Azure Static Web Apps
  - Use windows-latest pool

- [ ] **Task 7.4**: Create User Docs Pipeline (M - 1.5 hours)
  - File: `.azuredevops/pipelines/docs-user-deploy.yml`
  - Similar to Task 7.3
  - Deploy to separate SWA or subdomain

- [ ] **Task 7.5**: Create Wiki Deployment Pipeline (L - 3 hours)
  - File: `.azuredevops/pipelines/docs-wiki-deploy.yml`
  - Use Azure DevOps Wiki REST API
  - PowerShell script: .azuredevops/scripts/ado-wiki-publish.ps1

- [ ] **Task 7.6**: Create PR Validation Pipeline (M - 1.5 hours)
  - File: `.azuredevops/pipelines/docs-pr-validation.yml`
  - Check XML comments, markdownlint
  - Build all docs

- [ ] **Task 7.7**: Configure Branch Policies (S - 15 min)
  - Manual: Azure DevOps UI → Repos → Branches
  - Add build validation policy

- [ ] **Task 7.8**: Test Azure DevOps Workflow (M - 1.5 hours)
  - Create test PR
  - Verify validation, merge
  - Verify deployments
  - Check docs at Azure SWA URL

**Phase 7 Complete When (if implemented):**
- [ ] Azure resources created
- [ ] All pipelines working
- [ ] Docs deployed to Azure Static Web Apps
- [ ] Wiki published to Azure DevOps Wiki
- [ ] Branch policies configured

---

## Phase 8: AI Integration & Workflows ⏳ NOT STARTED

**Goal**: Enable AI-assisted documentation generation
**Estimated**: 2-3 hours
**Dependencies**: Phase 2 (MCP config), Phase 6 complete

### Tasks

- [ ] **Task 8.1**: Run MCP Server Setup Script (S - 30 min)
  - Script: `.docgen/setup-mcp.ps1`
  - Run on WSL2
  - Verify MCP servers connect in Claude Code

- [ ] **Task 8.2**: Test AI-Assisted API Documentation (S - 30 min)
  - Select C# class
  - Prompt Claude to generate XML comments
  - Verify uses correct .NET terminology
  - Commit generated comments

- [ ] **Task 8.3**: Test documentation-architect Agent (M - 1 hour)
  - Invoke agent
  - Generate architecture documentation
  - Verify includes Mermaid diagrams
  - Review and refine

- [ ] **Task 8.4**: Document AI Workflows (S - 1 hour)
  - File: `docs/architecture/ai-documentation-workflows.md`
  - Workflow for each doc type
  - Example prompts
  - Best practices

**Phase 8 Complete When:**
- [ ] MCP servers configured and working
- [ ] AI assistance tested for all doc types
- [ ] AI workflows documented

---

## Phase 9: Platform Switching & Testing ⏳ NOT STARTED

**Goal**: Enable platform portability and validate everything
**Estimated**: 2-3 hours
**Dependencies**: Phases 6, 8 complete (Phase 7 optional)

### Tasks

- [ ] **Task 9.1**: Implement Platform Detection Script (M - 1.5 hours)
  - File: `.docgen/detect-platform.ps1`
  - Function: Get-CIPlatform (GitHub Actions, Azure DevOps, Local)
  - Function: Get-PlatformConfig
  - Works on WSL2

- [ ] **Task 9.2**: Implement Platform Switching Script (M - 1.5 hours)
  - File: `.docgen/switch-platform.ps1`
  - Parameter: -Platform (GitHub | AzureDevOps)
  - Updates docfx.json, enables/disables workflows
  - Reversible

- [ ] **Task 9.3**: Test Platform Switching (S - 30 min)
  - Switch to GitHub, verify
  - Switch to Azure DevOps, verify
  - Switch back to GitHub

- [ ] **Task 9.4**: Comprehensive End-to-End Test (M - 1.5 hours)
  - All docs build locally
  - GitHub workflows succeed
  - Azure pipelines succeed (if implemented)
  - Platform switching works
  - AI assistance works
  - Documentation quality verified

- [ ] **Task 9.5**: Create Migration Guide (S - 1 hour)
  - File: `docs/architecture/platform-migration-guide.md`
  - GitHub → Azure DevOps migration
  - Azure DevOps → GitHub migration
  - Step-by-step commands

**Phase 9 Complete When:**
- [ ] Platform detection works
- [ ] Platform switching works both directions
- [ ] Full end-to-end test passes
- [ ] Migration guide created

---

## Overall Project Completion Checklist

### GitHub Implementation Complete (Phases 1-6, 8-9)
- [ ] All dev docs created (plan, context, tasks)
- [ ] All 5 architecture docs created (Phase 1)
- [ ] Platform-agnostic core established (Phase 2)
- [ ] All three documentation types set up (Phases 3-5)
- [ ] GitHub plugin working (Phase 6)
- [ ] AI integration working (Phase 8)
- [ ] Platform detection implemented (Phase 9)
- [ ] **Estimated Total**: 28-32 hours

### Full Implementation Complete (All Phases)
- [ ] All GitHub implementation items above
- [ ] Azure DevOps plugin working (Phase 7)
- [ ] Platform switching fully tested (Phase 9 complete)
- [ ] **Estimated Total**: 36-40 hours

---

## Notes

### Update Instructions
- Mark tasks complete with `[x]` as you finish them
- Update phase status emojis (⏳ → 🟡 → ✅)
- Update "Actual Hours" column in Quick Status Overview
- Update "Last Updated" date at top

### Before Context Reset
Run `/dev-docs-update` to update all three files:
- This file (tasks.md)
- context.md
- plan.md (if scope changed)

### Task Size Legend
- **S** (Small): < 1 hour
- **M** (Medium): 1-2 hours
- **L** (Large): 2-4 hours
- **XL** (Extra Large): 4+ hours

---

**Tasks Status**: ✅ COMPLETE (checklist created)
**Next Action**: Begin Phase 1, Task 1.1 (Create Architecture Documentation)
