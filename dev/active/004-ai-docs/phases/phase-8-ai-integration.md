# Phase 8: AI Integration & Workflows

**Estimated Time**: 2-3 hours
**Status**: NOT STARTED
**Dependencies**: Phase 2 (MCP config created), Phase 6 complete

---

## Overview

**Goal**: Enable AI-assisted documentation generation using MCP servers and Claude Code agents.

**Why This Phase Matters**: AI assistance dramatically accelerates documentation creation and ensures correct Microsoft terminology. This phase makes documentation maintenance sustainable long-term.

---

## Tasks

### Task 8.1: Run MCP Server Setup Script (Small - 30 minutes)

**Script**: `.docgen/setup-mcp.ps1`

**Purpose**: Configure MCP servers for Claude Code to provide AI assistance with documentation.

**Acceptance Criteria**:
- [ ] Script runs without errors on WSL2
- [ ] Claude Code config updated with MCP servers
- [ ] Microsoft Learn MCP server configured and enabled
- [ ] Docs MCP server configured (optional)
- [ ] Context7 MCP server configured (optional)
- [ ] Verify MCP servers connect in Claude Code (restart Claude Code, check MCP status)

**Implementation**:
```bash
# Run MCP setup script
pwsh .docgen/setup-mcp.ps1

# Expected output:
# Claude Code config path: /home/user/.config/claude/config.json
# Backed up existing config to: /home/user/.config/claude/config.json.backup.20251102123456
# MCP servers configured successfully!
# Restart Claude Code to apply changes.

# Restart Claude Code
# In Claude Code UI: Check that MCP servers are connected
```

**Verify MCP Connection**:
1. Restart Claude Code
2. In Claude Code: Check MCP server status indicator
3. Expected: Microsoft Learn MCP server shows "Connected"

**Troubleshooting**:
- If connection fails: Check Node.js version (need 18+)
- If config not found: Verify Claude Code installation path
- If merge fails: Check JSON syntax in `.docgen/mcp-config.json`

---

### Task 8.2: Test AI-Assisted API Documentation (Small - 30 minutes)

**Workflow**: Generate XML comments for C# classes using AI assistance.

**Purpose**: Validate that AI can generate accurate API documentation with Microsoft Learn context.

**Acceptance Criteria**:
- [ ] Select a C# class (e.g., `src/ClaudeStack.Web/Controllers/HomeController.cs`)
- [ ] Prompt Claude: "Generate XML documentation comments for this class using Microsoft Learn conventions"
- [ ] AI generates comments using MCP server context
- [ ] Comments use correct .NET terminology
- [ ] Include `<summary>`, `<param>`, `<returns>`, `<example>` sections
- [ ] Commit generated comments
- [ ] Rebuild and verify API reference includes new comments

**Test Prompt**:
```
Generate comprehensive XML documentation comments for the HomeController class following Microsoft Learn conventions.

Include:
- <summary> for class and all public methods
- <param> for action method parameters
- <returns> for action methods
- <remarks> with usage notes
- <example> showing typical usage

Use correct ASP.NET Core MVC terminology from Microsoft Learn.
```

**Expected Outcome**:
```csharp
/// <summary>
/// Handles HTTP requests for the home page and related views.
/// </summary>
/// <remarks>
/// This controller serves as the entry point for the application's main user interface.
/// It inherits from <see cref="Controller"/> to provide MVC functionality.
/// </remarks>
public class HomeController : Controller
{
    /// <summary>
    /// Displays the application's home page.
    /// </summary>
    /// <returns>
    /// An <see cref="IActionResult"/> that renders the Index view.
    /// </returns>
    /// <example>
    /// This action is invoked when navigating to the root URL:
    /// <code>
    /// GET /
    /// </code>
    /// </example>
    public IActionResult Index()
    {
        return View();
    }
}
```

**Validation**:
```bash
# Rebuild to regenerate XML
dotnet build src/ClaudeStack.Web/ClaudeStack.Web.csproj

# Rebuild developer docs
make docs-developer

# Serve and check API reference
make docs-serve
# Navigate to API reference, verify HomeController has documentation
```

---

### Task 8.3: Test documentation-architect Agent (Medium - 1 hour)

**Workflow**: Generate architecture documentation using the documentation-architect agent.

**Purpose**: Validate that the agent can create comprehensive, accurate architecture documentation.

**Acceptance Criteria**:
- [ ] Invoke documentation-architect agent
- [ ] Prompt: "Create architecture documentation for this .NET 10 project explaining the MVC app, Minimal API, test structure, and centralized package management"
- [ ] Agent examines codebase (reads project files, CLAUDE.md, etc.)
- [ ] Generates comprehensive architecture.md (or updates existing)
- [ ] Includes Mermaid diagrams (component, sequence, C4)
- [ ] Uses correct Microsoft terminology (via MCP)
- [ ] Review and refine output
- [ ] Commit to `docs/docfx-developer/articles/architecture.md`

**Test Prompt**:
```
Using the documentation-architect agent:

Create comprehensive architecture documentation for this .NET 10 project located in src/.

Analyze:
- ClaudeStack.Web (ASP.NET Core MVC application)
- ClaudeStack.API (ASP.NET Core Minimal API)
- Test projects (MSTest + Playwright)
- Directory.Build.props (centralized configuration)
- Directory.Packages.props (centralized package management)

Generate:
1. System architecture overview
2. Mermaid C4 component diagram
3. Sequence diagram for HTTP request flow
4. Description of centralized package management approach
5. Testing strategy section
6. Links to related documentation

Output to: docs/docfx-developer/articles/architecture.md

Use Microsoft Learn terminology for ASP.NET Core, Minimal APIs, and MSTest.
```

**Expected Agent Workflow**:
1. Agent reads src/ directory structure
2. Agent reads CLAUDE.md for project context
3. Agent reads Directory.Build.props and Directory.Packages.props
4. Agent queries Microsoft Learn MCP for correct terminology
5. Agent generates comprehensive architecture.md
6. Agent creates Mermaid diagrams
7. Agent saves to specified location

**Validation**:
```bash
# Check output file exists
ls -lh docs/docfx-developer/articles/architecture.md

# Rebuild docs
make docs-developer

# Serve and review
make docs-serve
# Navigate to Articles → Architecture, verify content and diagrams
```

---

### Task 8.4: Document AI Workflows (Small - 1 hour)

**File**: `docs/architecture/ai-documentation-workflows.md` (new)

**Purpose**: Create reference guide for using AI assistance for each documentation type.

**Acceptance Criteria**:
- [ ] Documents how to use AI for each doc type
- [ ] Workflow 1: API documentation (XML comments)
- [ ] Workflow 2: Architecture documentation (conceptual articles)
- [ ] Workflow 3: User guide generation (tutorials, getting started)
- [ ] Workflow 4: Feature summaries for company docs (wiki content)
- [ ] Example prompts for each workflow (copy-paste ready)
- [ ] Best practices for AI-assisted documentation
- [ ] MCP server configuration reference

**Template**:
```markdown
# AI Documentation Workflows

## Overview

This guide explains how to leverage AI assistance for documentation creation using Claude Code + MCP servers.

## Prerequisites

- Claude Code with MCP servers configured (see Phase 8, Task 8.1)
- Microsoft Learn MCP server enabled

## Workflow 1: API Documentation (XML Comments)

### Use Case
Generate XML documentation comments for C# code.

### Prompt Template
```
Generate comprehensive XML documentation comments for [ClassName] following Microsoft Learn conventions.

Include:
- <summary> for class and all public members
- <param> for method parameters
- <returns> for methods with return values
- <remarks> with usage notes
- <example> showing typical usage

Use correct [Framework] terminology from Microsoft Learn.
```

### Example
[Include full example from Task 8.2]

### Best Practices
- Review AI-generated comments for accuracy
- Ensure examples are realistic
- Update comments when code changes
- Run `dotnet build` to verify XML generation

---

## Workflow 2: Architecture Documentation

### Use Case
Create comprehensive architecture articles with diagrams.

### Agent
Use **documentation-architect** agent for best results.

### Prompt Template
[Include template from Task 8.3]

### Output
- Comprehensive articles (500-2000 words)
- Mermaid diagrams (C4, sequence, component)
- Microsoft terminology
- Links to related documentation

---

## Workflow 3: User Guide Generation

[Similar structure for user docs]

---

## Workflow 4: Feature Summaries (Company Docs)

[Similar structure for wiki content]

---

## Best Practices

1. **Always verify AI output**: AI is very good but not perfect
2. **Use Microsoft Learn MCP**: Ensures correct terminology
3. **Iterative refinement**: Start broad, then ask for specifics
4. **Save prompts**: Reuse successful prompts for similar tasks
5. **Update regularly**: As code evolves, update docs with AI assistance

## MCP Server Reference

### Microsoft Learn MCP
- **Purpose**: Official Microsoft documentation context
- **Best For**: .NET, ASP.NET Core, Azure terminology
- **Command**: `npx -y @microsoft/mcp-server-learn`

### Docs MCP (Optional)
- **Purpose**: General documentation context
- **Best For**: Markdown formatting, documentation patterns
- **Command**: `npx -y docs-mcp-server`
```

---

## Phase Completion Criteria

Phase 8 is complete when:

- [ ] MCP servers configured and working in Claude Code
- [ ] AI assistance tested for all doc types (API, architecture, user, company)
- [ ] AI workflows documented in `docs/architecture/ai-documentation-workflows.md`
- [ ] Example documentation generated with AI (at least one for each type)
- [ ] Team understands how to use AI for documentation

---

## Success Indicators

You'll know this phase is successful when:

- AI generates accurate XML comments using Microsoft terminology
- documentation-architect agent creates comprehensive architecture docs
- Mermaid diagrams generated by AI render correctly
- AI-generated content requires minimal manual editing
- Team can confidently use AI for documentation tasks

---

**Phase Status**: NOT STARTED
**Next Task**: Task 8.1 - Run MCP Server Setup Script
**Estimated Completion**: After 2-3 hours of focused work
