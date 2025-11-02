# Claude Code Infrastructure

This directory contains Claude Code infrastructure installed from the showcase repository.

## Attribution

This infrastructure is based on the [Claude Code Infrastructure Showcase](https://github.com/yourusername/claude-code-infrastructure-showcase) repository, released under the MIT License.

**Copyright (c) 2025 Claude Code Infrastructure Contributors**

See [ATTRIBUTION.md](ATTRIBUTION.md) for complete licensing information and details about which components were used.

## Environment

**Running on:** WSL2 (Ubuntu) on Windows

This installation is configured for native Linux execution:
- Hooks use `#!/bin/bash` shebang
- Paths use forward slashes (`/`)
- File permissions managed via `chmod`
- npm/npx via Ubuntu Node.js installation

## Installed Components

### Hooks
- **skill-activation-prompt** (UserPromptSubmit): Auto-suggests relevant skills based on prompts and file context
- **post-tool-use-tracker** (PostToolUse): Tracks file changes for context management

### Skills

**General:**
- **skill-developer**: Meta-skill for creating and managing Claude Code skills (tech-agnostic)
- **azure-devops**: Azure DevOps automation using az CLI with azure-devops extension

**.NET 10 Project-Specific Skills:**
- **mstest-testing-platform** (High Priority): MSTest with Microsoft.Testing.Platform (new test runner)
- **dotnet-centralized-packages** (High Priority): Centralized Package Management with Directory.Packages.props
- **playwright-dotnet** (High Priority): E2E testing with Playwright for .NET
- **dotnet-minimal-apis** (Medium Priority): ASP.NET Core Minimal APIs with OpenAPI
- **dotnet-cli-essentials** (Medium Priority): Essential .NET CLI commands for this project
- **aspnet-configuration** (Medium Priority): ASP.NET Core configuration and options pattern

### Agents
Specialized autonomous agents for complex, multi-step tasks (see `agents/README.md`):
- **code-architecture-reviewer**: Reviews code for best practices and architectural consistency
- **code-refactor-master**: Handles comprehensive code refactoring tasks
- **documentation-architect**: Creates and enhances documentation
- **plan-reviewer**: Reviews development plans before implementation
- **refactor-planner**: Analyzes code and creates refactoring plans
- **web-research-specialist**: Researches technical issues and solutions online

### Slash Commands
- **/dev-docs**: Creates new dev docs (plan, context, tasks) for a task
- **/dev-docs-update**: Updates existing dev docs before context reset

### Dev Docs
Located in `dev/active/`, following the three-file pattern:
- `[task]-plan.md` - Strategic implementation plan
- `[task]-context.md` - Current state and key decisions
- `[task]-tasks.md` - Checklist format

## Configuration

- **settings.json**: Hook registration and settings
- **skills/skill-rules.json**: Skill trigger definitions
- **commands/**: Slash command definitions

## Usage

### Activating Skills
Skills auto-activate when you:
- Mention trigger keywords in prompts
- Work with files matching path patterns
- Describe tasks matching intent patterns

### Creating New Skills
Use the skill-developer skill:
1. Prompt: "I want to create a new skill for [topic]"
2. skill-developer will activate and guide you
3. Follows 500-line rule and progressive disclosure pattern

### Dev Docs
For complex tasks, use slash commands to automate creation:

**Option 1: Using slash commands (recommended):**
```
/dev-docs implement feature name
```
This automatically creates the three-file structure with comprehensive content.

**Option 2: Manual creation:**
1. Create directory in `dev/active/[task-name]/`
2. Create three files: plan.md, context.md, tasks.md
3. Update context.md frequently during implementation
4. Use for resuming after context resets

**Updating before context reset:**
```
/dev-docs-update
```
This updates the context.md with current progress.

## Tech Stack Compatibility

This infrastructure is tech-agnostic and works with this .NET 10 project:
- skill-developer works with any language
- Hooks detect file changes regardless of tech stack
- Can create .NET-specific skills using skill-developer

## WSL/Ubuntu Requirements

**Prerequisites:**
- Node.js installed in Ubuntu/WSL (not Windows)
- npm/npx accessible from bash
- Bash shell (default in WSL)

**Hook Dependencies:**
```bash
cd .claude/hooks
npm install
```

This installs:
- `tsx` - TypeScript execution for hooks
- `typescript` - TypeScript compiler
- `node-jq` - JSON processing (jq via npm)
- `@types/node` - TypeScript definitions

**Validating Installation:**
```bash
# Test skill activation hook
cd .claude/hooks
echo '{"prompt":"test"}' | npx tsx skill-activation-prompt.ts

# Check hook permissions
ls -l .claude/hooks/*.sh
# Should show: -rwxrwxrwx (executable)
```

## Troubleshooting

See [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for common issues and solutions.

## Next Steps

1. Test skill activation: "Explain how skill triggers work"
2. Create your first .NET-specific skill using skill-developer
3. Use dev docs pattern for next complex task
