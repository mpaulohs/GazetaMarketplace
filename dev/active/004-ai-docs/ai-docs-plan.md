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

✅ **GitHub Repository**: NotMyself/net10-project-example (already on GitHub)

✅ **Claude Code Infrastructure**: WSL2 (Ubuntu) environment with hooks, skills, and agents

✅ **.NET 10 Project Structure**: Clean solution with centralized package management

✅ **Existing Documentation**: CLAUDE.md, README.md, docs/BEST-PRACTICES.md, etc.

### What We Don't Have
❌ **XML Documentation**: Not enabled in .csproj files
❌ **DocFX**: Not installed
❌ **Documentation Content**: No API docs, no architecture diagrams
❌ **CI/CD for Docs**: No pipelines for documentation
❌ **Diagram Tools**: dll2mmd, PlantUML generators not installed
❌ **MCP Servers**: Not configured for Claude Code
❌ **Platform Infrastructure**: No `.docgen/` directory, no platform plugins

### Key Decision: GitHub First, Then Azure DevOps
**Rationale**: Repository already on GitHub, GitHub Pages simpler, can add Azure DevOps plugin later for client work.

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
- API reference + conceptual articles
- Mermaid diagrams (architecture, classes)
- Published to GitHub Pages (/) or Azure Static Web Apps

**2. System User Docs** (`docs/docfx-user/`)
- Getting started guides, tutorials, screenshots
- Published to GitHub Pages (/user/) or separate Azure SWA

**3. Company System Docs** (`docs/wiki/`)
- System purpose, access, feature summaries, active development
- Synced to GitHub Wiki or Azure DevOps Wiki

### Hosting Strategy

**GitHub:**
- Developer Docs: `https://notmyself.github.io/net10-project-example/`
- User Docs: `https://notmyself.github.io/net10-project-example/user/`
- Company Docs: `https://github.com/NotMyself/net10-project-example/wiki`

**Azure DevOps (future):**
- Developer Docs: `https://net10-docs.azurestaticapps.net/`
- User Docs: `https://net10-docs.azurestaticapps.net/user/`
- Company Docs: `https://dev.azure.com/org/project/_wiki`

---

## Implementation Phases Overview

**Detailed implementation instructions for each phase are in `phases/` directory.**

| Phase | Description | Time | Details |
|-------|-------------|------|---------|
| 1 | Documentation Planning & Architecture | 3-4 hours | [phases/phase-1-planning.md](phases/phase-1-planning.md) |
| 2 | Core Foundation Setup | 6-8 hours | [phases/phase-2-foundation.md](phases/phase-2-foundation.md) |
| 3 | System Developer Docs Setup | 4-6 hours | [phases/phase-3-developer-docs.md](phases/phase-3-developer-docs.md) |
| 4 | System User Docs Setup | 3-4 hours | [phases/phase-4-user-docs.md](phases/phase-4-user-docs.md) |
| 5 | Company System Docs Setup | 2-3 hours | [phases/phase-5-company-docs.md](phases/phase-5-company-docs.md) |
| 6 | GitHub Plugin Implementation | 4-6 hours | [phases/phase-6-github-plugin.md](phases/phase-6-github-plugin.md) |
| 7 | Azure DevOps Plugin (OPTIONAL) | 6-8 hours | [phases/phase-7-azure-devops-plugin.md](phases/phase-7-azure-devops-plugin.md) |
| 8 | AI Integration & Workflows | 2-3 hours | [phases/phase-8-ai-integration.md](phases/phase-8-ai-integration.md) |
| 9 | Platform Switching & Testing | 2-3 hours | [phases/phase-9-platform-switching.md](phases/phase-9-platform-switching.md) |

### Phase Summaries

**Phase 1: Documentation Planning & Architecture** (3-4 hours)
- Create 5 architecture documents in `docs/architecture/`
- Extract and organize content from this plan
- Foundation for all implementation work

**Phase 2: Core Foundation Setup** (6-8 hours)
- Install tools (DocFX, dll2mmd, PlantUmlClassDiagramGenerator)
- Create `.docgen/` directory with platform-agnostic scripts
- Enable XML documentation generation
- Create cross-platform Makefile
- Configure MCP servers

**Phase 3: System Developer Docs Setup** (4-6 hours)
- Create `docs/docfx-developer/` structure
- Configure DocFX for API reference
- Generate class diagrams automatically
- Create architecture and domain model documentation
- Test local build

**Phase 4: System User Docs Setup** (3-4 hours)
- Create `docs/docfx-user/` structure
- Configure DocFX for user-focused content
- Create getting started guide and features overview
- Test local build

**Phase 5: Company System Docs Setup** (2-3 hours)
- Create `docs/wiki/` structure
- Write system purpose, access, feature summary, active development docs
- Create wiki sync script

**Phase 6: GitHub Plugin Implementation** (4-6 hours)
- Create GitHub Actions workflows for all 3 doc types
- Deploy to GitHub Pages
- Sync wiki to GitHub Wiki
- PR validation
- Branch protection

**Phase 7: Azure DevOps Plugin (OPTIONAL)** (6-8 hours)
- Create Azure resources (Static Web Apps)
- Create Azure Pipelines for all 3 doc types
- Azure DevOps Wiki REST API integration
- Branch policies

**Phase 8: AI Integration & Workflows** (2-3 hours)
- Run MCP server setup script
- Test AI-assisted API documentation
- Test documentation-architect agent
- Document AI workflows

**Phase 9: Platform Switching & Testing** (2-3 hours)
- Implement platform detection and switching scripts
- Test platform switching both directions
- Comprehensive end-to-end testing
- Create migration guide

---

## Timeline Estimates

### Aggressive Timeline (Focus, No Interruptions)
- **Week 1**: Phases 1-3 (Documentation planning + Core foundation + Developer docs)
- **Week 2**: Phases 3-5 (Complete developer docs, User docs, Company docs)
- **Week 3**: Phase 6 (GitHub plugin) + Phase 8 (AI integration) + Phase 9 partial
- **Week 4** (Optional): Phase 7 (Azure DevOps plugin) + Phase 9 complete

**Total: 3-4 weeks, 30-36 hours**

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
| 7 | Azure DevOps Plugin (OPTIONAL) | 6 | 8 | 7 |
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

## Next Steps

### Immediate Actions
1. ✅ Create dev docs structure (this file)
2. ✅ Create context.md file
3. ✅ Create tasks.md file
4. ✅ Extract phases into separate files
5. ⏳ Begin Phase 1 (Documentation Planning)

### This Week
- Complete Phase 1 (create 5 architecture documents)
- Begin Phase 2 (core foundation setup)
- Install tools (DocFX, diagram generators)

---

## Key File Locations

### Dev Docs (This Implementation)
- `dev/active/004-ai-docs/ai-docs-plan.md` (this file - executive summary)
- `dev/active/004-ai-docs/ai-docs-context.md` (session context)
- `dev/active/004-ai-docs/ai-docs-tasks.md` (task checklist)
- `dev/active/004-ai-docs/phases/phase-*.md` (detailed phase instructions)

### Architecture Documentation (To Be Created in Phase 1)
- `docs/architecture/ai-docs-platform-agnostic-architecture.md`
- `docs/architecture/documentation-content-strategy.md`
- `docs/architecture/ai-docs-implementation-plan.md`
- `docs/architecture/github-plugin-guide.md`
- `docs/architecture/azure-devops-plugin-guide.md`

### Research & Reference
- `docs/research/ai-assisted-documentation.md` (existing, 582 lines)
- `CLAUDE.md` (project overview)
- `.claude/README.md` (Claude Code infrastructure)

### Core Documentation Directories (To Be Created in Phases 3-5)
- `docs/docfx-developer/` (System Developer Docs)
- `docs/docfx-user/` (System User Docs)
- `docs/wiki/` (Company System Docs)

### Platform-Agnostic Core (To Be Created in Phase 2)
- `.docgen/` (automation scripts)
- `Makefile` (cross-platform build)

### Platform Plugins (To Be Created in Phases 6-7)
- `.github/workflows/` (GitHub plugin)
- `.azuredevops/pipelines/` (Azure DevOps plugin - OPTIONAL)

---

**Plan Status**: ✅ COMPLETE (Refactored for progressive disclosure)
**Structure**: Executive summary (~200 lines) + 9 detailed phase files
**Ready to Begin**: YES
**First Task**: Begin Phase 1 implementation - see [phases/phase-1-planning.md](phases/phase-1-planning.md)
