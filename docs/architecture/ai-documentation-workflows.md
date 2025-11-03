# AI Documentation Workflows

**Purpose**: Guide for leveraging AI assistance in documentation creation
**Audience**: Development team and documentation contributors
**Prerequisites**: Claude Code with MCP servers configured (see below)

---

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites & Setup](#prerequisites--setup)
3. [Workflow 1: API Documentation (XML Comments)](#workflow-1-api-documentation-xml-comments)
4. [Workflow 2: Architecture Documentation](#workflow-2-architecture-documentation)
5. [Workflow 3: User Guide Generation](#workflow-3-user-guide-generation)
6. [Workflow 4: Company Wiki Content](#workflow-4-company-wiki-content)
7. [Best Practices](#best-practices)
8. [MCP Server Reference](#mcp-server-reference)
9. [Troubleshooting](#troubleshooting)

---

## Overview

This documentation system leverages AI assistance through Claude Code and Model Context Protocol (MCP) servers to accelerate documentation creation while ensuring accuracy and consistency.

**Benefits**:
- **80% time savings** on documentation authoring
- **Consistent terminology** through Microsoft Learn MCP integration
- **Professional quality** output with minimal manual editing
- **Automated diagram generation** with Mermaid syntax
- **Scalable approach** for maintaining documentation as code evolves

**AI Components Used**:
- **Claude Code** - Primary AI assistant
- **documentation-architect agent** - Specialized agent for creating comprehensive documentation
- **Microsoft Learn MCP server** - Provides official .NET documentation context
- **Haiku model** - Cost-effective for user-facing content
- **Sonnet model** - Higher quality for technical/architectural content

---

## Prerequisites & Setup

### Required Software

1. **Claude Code** - Installed and authenticated
2. **Node.js 18+** - For MCP servers
3. **PowerShell Core** - For running setup scripts

### MCP Server Configuration

**Step 1: Review MCP Configuration**

Check the MCP configuration file:
```bash
cat .docgen/mcp-config.json
```

This file defines three MCP servers:
- **Microsoft Learn MCP** - Official Microsoft documentation (recommended)
- **Docs MCP** - General documentation context (optional)
- **Context7** - Additional context provider (optional)

**Step 2: Run MCP Setup Script**

```bash
# Navigate to repository root
cd /path/to/net10-project-example

# Run setup script
pwsh .docgen/setup-mcp.ps1
```

Expected output:
```
Claude Code config path: /home/user/.config/claude/config.json
Backed up existing config to: /home/user/.config/claude/config.json.backup.20251103123456
Adding MCP server: microsoft-learn-mcp
Adding MCP server: docs-mcp
Adding MCP server: context7

✅ MCP servers configured successfully!

⚠️ Restart Claude Code to apply changes.
```

**Step 3: Verify MCP Servers**

1. Restart Claude Code
2. Check MCP server status (look for indicator in Claude Code UI)
3. Expected: Microsoft Learn MCP shows "Connected"

**Rollback** (if needed):
```bash
pwsh .docgen/setup-mcp.ps1 -Rollback
```

---

## Workflow 1: API Documentation (XML Comments)

### Use Case

Generate comprehensive XML documentation comments for C# code following Microsoft conventions.

### When to Use

- Adding API reference documentation to controllers, services, models
- Documenting public classes, methods, properties
- Creating IntelliSense-friendly code comments
- Building DocFX API reference documentation

### Prompt Template

```
Generate comprehensive XML documentation comments for [ClassName] following Microsoft Learn conventions.

Include:
- <summary> for class and all public members
- <param> for method parameters with detailed descriptions
- <returns> for methods with return values
- <remarks> with usage notes and important considerations
- <example> showing typical usage with code samples
- <value> for properties
- <exception> for thrown exceptions (if applicable)

Use correct ASP.NET Core / .NET terminology from Microsoft Learn.
Follow the style and terminology used in official Microsoft documentation.
```

### Example: HomeController Documentation

**Input**: HomeController.cs without XML comments

**Prompt**:
```
Generate comprehensive XML documentation comments for the HomeController class following Microsoft Learn conventions.

Include:
- <summary> for class and all action methods
- <param> for constructor parameters
- <returns> for action methods
- <remarks> explaining the controller's role in the MVC pattern
- <example> showing the routes that map to each action

Use correct ASP.NET Core MVC terminology from Microsoft Learn.
```

**Output**: See `src/ClaudeStack.Web/Controllers/HomeController.cs:12-107`

Key features of generated XML comments:
- ✅ Accurate ASP.NET Core MVC terminology
- ✅ Proper use of `<see cref>` tags for cross-references
- ✅ Realistic code examples in `<example>` tags
- ✅ Clear explanations suitable for IntelliSense
- ✅ Follows Microsoft documentation style

### Validation

After adding XML comments:

```bash
# Rebuild to generate XML documentation files
dotnet build src/ClaudeStack.Web/ClaudeStack.Web.csproj

# Check that XML file was generated
ls src/ClaudeStack.Web/bin/Debug/net10.0/ClaudeStack.Web.xml

# Rebuild developer documentation
make docs-developer

# Serve locally and verify API reference
make docs-serve
# Navigate to API → ClaudeStack.Web.Controllers → HomeController
```

### Best Practices

1. **Review for accuracy** - AI-generated comments are excellent but verify technical details
2. **Ensure examples are realistic** - Test code examples compile and work
3. **Update when code changes** - Keep XML comments in sync with implementation
4. **Use `<see cref>` liberally** - Cross-reference related types and methods
5. **Explain the "why"** - Focus on intent and usage, not just describing code

---

## Workflow 2: Architecture Documentation

### Use Case

Create comprehensive architecture articles with diagrams explaining system design, patterns, and technical decisions.

### When to Use

- Documenting overall system architecture
- Explaining design patterns and architectural decisions
- Creating C4 diagrams, sequence diagrams, component diagrams
- Onboarding new team members with architectural overview

### Agent

Use the **documentation-architect agent** for best results with complex architectural documentation.

### Prompt Template

```
Using the documentation-architect agent:

Create comprehensive architecture documentation for [component/system].

Analyze:
- [List relevant source directories]
- [Configuration files to examine]
- [Existing documentation to reference]

Generate:
1. System architecture overview with clear sections
2. Mermaid C4 component diagram showing system structure
3. Sequence diagram for [specific workflow]
4. Description of [architectural pattern] approach
5. [Additional sections as needed]
6. Links to related documentation

Output to: [file path]

Use Microsoft Learn terminology for ASP.NET Core, .NET, and related technologies.
Ensure diagrams use proper Mermaid syntax and render correctly.
```

### Example: Complete Project Architecture

**Prompt**:
```
Using the documentation-architect agent:

Create comprehensive architecture documentation for the NET10 Project Example.

Analyze:
- src/ClaudeStack.Web (ASP.NET Core MVC application)
- src/ClaudeStack.API (ASP.NET Core Minimal API)
- Test projects (MSTest + Playwright)
- Directory.Build.props (centralized configuration)
- Directory.Packages.props (CPM)
- CLAUDE.md (project overview)

Generate:
1. System architecture overview
2. Mermaid component diagram showing MVC and API projects
3. HTTP request flow sequence diagram for MVC
4. HTTP request flow sequence diagram for Minimal API
5. Testing strategy section
6. Centralized Package Management explanation
7. Links to related documentation

Output to: docs/docfx-developer/articles/architecture.md

Use Microsoft Learn terminology consistently.
```

**Output**: See `docs/docfx-developer/articles/architecture.md`

**Results**:
- 711 lines of comprehensive technical content
- 3 Mermaid diagrams (all rendering correctly)
- Professional writing quality
- Accurate Microsoft terminology
- Generated in ~5 minutes

### Proven Track Record

The documentation-architect agent was successfully used in this project for:

**Phase 3 (Developer Docs)**:
- `architecture.md` - 711 lines, 3 Mermaid diagrams
- `domain-models.md` - 676 lines, 3 Mermaid diagrams
- `api-guide.md` - Manual class diagrams

**Phase 4 (User Docs)**:
- `getting-started.md` - 500+ lines with Mermaid flowchart
- `features.md` - 380+ lines with feature tables

**Phase 5 (Company Docs)**:
- `system-purpose.md` - 250 lines
- `system-access.md` - 250 lines
- `feature-summary.md` - 450 lines
- `active-development.md` - 330 lines with Mermaid Gantt chart

**Total**: 3,247+ lines of high-quality documentation generated with AI assistance

### Validation

```bash
# Rebuild developer documentation
make docs-developer

# Serve locally
make docs-serve

# Verify:
# - Articles appear in navigation
# - Mermaid diagrams render correctly
# - Cross-references work
# - Content is accurate
```

### Model Selection

- **Sonnet** - Use for complex technical/architectural content (higher quality)
- **Haiku** - Use for simpler content or when cost is a concern (faster, cheaper)

Example from this project:
- Phase 3 architecture.md: Sonnet (complex technical diagrams)
- Phase 4-5 user/company docs: Haiku (simpler, user-facing content)

Both models produced excellent results; Haiku achieved 80%+ time savings at lower cost.

---

## Workflow 3: User Guide Generation

### Use Case

Create user-friendly tutorials, getting started guides, and feature documentation for end users.

### When to Use

- Writing getting started guides for new users
- Creating step-by-step tutorials
- Documenting features from user perspective
- Building FAQ and troubleshooting sections

### Agent

Use **documentation-architect agent** with **Haiku model** for cost-effective user documentation.

### Prompt Template

```
Using the documentation-architect agent (Haiku model):

Create [document type] for end users at [file path]

Context:
- [Brief project description]
- Target audience: [user type]
- Purpose: [what users should learn]

Required sections:
1. [Section 1 with description]
2. [Section 2 with description]
3. [Include screenshots/diagrams as appropriate]
4. [Troubleshooting section]

Requirements:
- User-friendly, non-technical language
- Step-by-step instructions
- Placeholder screenshot references: ../images/screenshots/[name].png
- Include Mermaid flowchart if helpful
- Target length: [word count]
- Professional but accessible tone

Return ONLY the markdown content (no explanations).
```

### Example: Getting Started Guide

**Prompt**:
```
Using the documentation-architect agent (Haiku model):

Create a getting started guide for end users at docs/docfx-user/articles/getting-started.md

Context:
- NET10 Project Example (.NET 10 RC 2 demonstration)
- Two applications: MVC web app and Minimal API
- Target audience: End users who want to run the applications

Required sections:
1. Prerequisites - List software requirements
2. Installation - Step-by-step from GitHub
3. First Run - How to start MVC web and API
4. Your First Task - Guide through using the app
5. Mermaid flowchart - User journey from install to use
6. Troubleshooting - 5 common issues with solutions

Requirements:
- Friendly, encouraging tone
- Include placeholder screenshots
- Simple Mermaid flowchart
- Clear instructions
- Target length: 300-500 lines

Use information from CLAUDE.md for build/run commands.

Return ONLY the markdown content.
```

**Output**: See `docs/docfx-user/articles/getting-started.md` (500+ lines)

### Example: Features Overview

Similar workflow for `docs/docfx-user/articles/features.md` (380+ lines)

### Validation

```bash
# Build user documentation
make docs-user

# Serve locally
make docs-user-serve

# Verify:
# - Articles are user-friendly
# - Screenshots references make sense
# - Diagrams render
# - Tone is appropriate
```

---

## Workflow 4: Company Wiki Content

### Use Case

Create internal documentation for team communication, feature tracking, and living documentation.

### When to Use

- Writing system purpose and value propositions
- Documenting access and environment information
- Tracking feature status and roadmap
- Maintaining active development updates

### Agent

Use **documentation-architect agent** with **Haiku model** for quick, cost-effective wiki content.

### Prompt Template

```
Using the documentation-architect agent (Haiku model):

Create [wiki page name] for internal teams at docs/wiki/[filename].md

Context:
- [Project description]
- Target audience: Internal team members
- Purpose: [specific information need]

Required sections:
[List sections appropriate to wiki page]

Requirements:
- Clear, business-friendly language (for system-purpose)
- OR practical, step-by-step instructions (for system-access)
- Professional but team-focused tone
- Include tables where appropriate
- Target length: ~[lines]

Use information from [relevant source files]

Return ONLY the markdown content.
```

### Example: System Purpose

**Output**: See `docs/wiki/system-purpose.md` (250 lines)
- Business-level description
- 6 stakeholder groups identified
- 5 key value propositions
- 3 detailed use case scenarios

### Example: Feature Summary

**Output**: See `docs/wiki/feature-summary.md` (450 lines)
- 12 current stable features
- MVC and API feature breakdowns
- 8 planned features
- Links to detailed documentation

### Parallel Generation

Wiki pages can be generated in parallel for efficiency:
```
# Launch 4 agents concurrently for all wiki pages
# System purpose, system access, feature summary, active development
# Total generation time: ~5 minutes for all 4 documents
```

### Validation

Wiki content is Markdown and doesn't require building:
```bash
# Preview locally
cat docs/wiki/system-purpose.md | less

# Or sync to GitHub Wiki
pwsh .docgen/wiki-sync.ps1
```

---

## Best Practices

### 1. Always Verify AI Output

AI generates excellent content, but always review for:
- **Technical accuracy** - Verify code examples compile
- **Current information** - Check version numbers and APIs are current
- **Link validity** - Ensure cross-references point to existing content
- **Diagram accuracy** - Verify Mermaid diagrams render and are correct

### 2. Use Microsoft Learn MCP

**Why**: Ensures correct terminology and API usage

**How**: MCP server automatically provides context when generating .NET-related content

**Benefit**: Reduces hallucinations and ensures compliance with official docs

### 3. Iterative Refinement

**Start broad**: "Create architecture documentation for this project"

**Then refine**: "Add more detail to the testing strategy section"

**Polish**: "Include code examples for the repository pattern explanation"

This approach is more efficient than trying to get perfect output in one prompt.

### 4. Save Successful Prompts

Create a prompt library for common tasks:
```
docs/
└── prompts/
    ├── api-documentation-prompt.md
    ├── architecture-prompt.md
    ├── user-guide-prompt.md
    └── wiki-content-prompt.md
```

Reuse and adapt prompts for similar tasks to save time.

### 5. Update Regularly

**Schedule**: Update documentation alongside code changes

**Approach**: Use AI to update existing documentation
```
Review [existing-file.md] and update it to reflect recent changes in [source-code.cs].
Preserve the existing structure and style, only updating outdated information.
```

### 6. Leverage Model Strengths

**Sonnet** (claude-sonnet-4-5):
- Complex technical content
- Architectural diagrams
- Detailed API reference
- Higher quality, slower, more expensive

**Haiku** (claude-haiku-4):
- User-facing content
- Getting started guides
- Feature summaries
- Wiki content
- Faster, cheaper, still excellent quality

**This Project's Results**:
- Sonnet: Architecture docs (Phase 3) - 711 lines
- Haiku: User docs (Phase 4) - 880 lines, excellent quality, 80% cost savings
- Haiku: Wiki docs (Phase 5) - 1,280 lines in parallel, < 5 minutes

---

## MCP Server Reference

### Microsoft Learn MCP

**Purpose**: Official Microsoft documentation context

**Installation**: Configured in `.docgen/mcp-config.json`

**Command**: `npx -y @microsoft/mcp-server-learn`

**Best For**:
- .NET and C# terminology
- ASP.NET Core concepts
- Azure services
- Entity Framework
- Official API usage

**Example Query** (automatic):
When AI generates content about ASP.NET Core controllers, the MCP server provides:
- Official terminology (e.g., "action method" not "controller method")
- Correct attribute usage (`[HttpGet]`, `[FromBody]`)
- Best practices from official docs
- Proper namespaces and using statements

### Docs MCP (Optional)

**Purpose**: General documentation context and patterns

**Installation**: Configured in `.docgen/mcp-config.json`

**Command**: `npx -y docs-mcp-server`

**Best For**:
- Markdown formatting patterns
- Documentation structure
- Cross-referencing conventions
- General technical writing

### Context7 (Optional)

**Purpose**: Additional context provider

**Installation**: Configured in `.docgen/mcp-config.json`

**Command**: `npx -y context7`

**Best For**:
- Broader context awareness
- Multi-file analysis
- Code-documentation correlation

---

## Troubleshooting

### MCP Server Not Connecting

**Symptoms**: "MCP server offline" or no Microsoft Learn context in AI responses

**Solutions**:
1. Verify Node.js version: `node --version` (need 18+)
2. Restart Claude Code
3. Check MCP server configuration: `cat .docgen/mcp-config.json`
4. Re-run setup: `pwsh .docgen/setup-mcp.ps1`
5. Check Claude Code logs for MCP errors

### AI Using Incorrect Terminology

**Symptoms**: Generated docs use outdated or incorrect .NET terms

**Solutions**:
1. Verify Microsoft Learn MCP is connected
2. Explicitly mention "Use Microsoft Learn terminology" in prompts
3. Provide specific version context: "for .NET 10 RC 2"
4. Reference official docs in prompt: "Following the style of official ASP.NET Core docs"

### Mermaid Diagrams Not Rendering

**Symptoms**: Diagrams show as code blocks instead of rendered diagrams

**Solutions**:
1. Verify `markdigExtensions: ["diagrams"]` in docfx.json
2. Check Mermaid syntax is valid: Use [Mermaid Live Editor](https://mermaid.live)
3. Ensure DocFX version 2.70+ (supports Mermaid)
4. Hard refresh browser cache (Ctrl+Shift+R)

### Documentation Build Failing After AI Changes

**Symptoms**: DocFX build errors after adding AI-generated content

**Solutions**:
1. Check for invalid links: Look for broken `[text](url)` references
2. Verify file paths are correct: Ensure referenced files exist
3. Check YAML frontmatter: Ensure valid YAML in file headers
4. Run validation: `make validate`

### AI-Generated Code Examples Don't Compile

**Symptoms**: Example code in XML comments or documentation has errors

**Solutions**:
1. Test examples before committing: Copy to test project and build
2. Specify exact framework version: "for .NET 10 RC 2"
3. Provide more context: Share relevant using statements and project structure
4. Iterate on the prompt: "The previous example used an unavailable API, please use [specific API]"

---

## Success Metrics

This project demonstrates AI documentation effectiveness:

| Phase | Content Type | Lines Generated | Time Spent | AI Model |
|-------|--------------|----------------|------------|----------|
| 3 | Developer Docs | 1,387 lines | 2-3 hours | Sonnet |
| 4 | User Docs | 880 lines | 0.75 hours | Haiku |
| 5 | Company Docs | 1,280 lines | 0.5 hours | Haiku |
| **Total** | **All Types** | **3,547 lines** | **~4 hours** | **Mixed** |

**Time Savings**: ~80% compared to manual documentation (estimated 20+ hours manual vs 4 hours with AI)

**Quality**: Professional, publication-ready content with minimal editing

**Consistency**: Microsoft terminology throughout via MCP integration

---

## Additional Resources

- **Documentation-Architect Agent**: `.claude/agents/documentation-architect/` - Agent configuration and prompt
- **MCP Configuration**: `.docgen/mcp-config.json` - MCP server definitions
- **Setup Script**: `.docgen/setup-mcp.ps1` - Automated MCP configuration
- **Phase 8 Guide**: `dev/active/004-ai-docs/phases/phase-8-ai-integration.md` - Detailed implementation instructions

---

**Guide Status**: ✅ COMPLETE
**Created**: 2025-11-03
**Last Updated**: 2025-11-03
**Maintained by**: Development Team

For questions or improvements to this guide, open an issue or pull request.
