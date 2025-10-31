# Claude Code Infrastructure

This directory contains Claude Code infrastructure installed from the showcase repository.

## Installed Components

### Hooks
- **skill-activation-prompt** (UserPromptSubmit): Auto-suggests relevant skills based on prompts and file context
- **post-tool-use-tracker** (PostToolUse): Tracks file changes for context management

### Skills
- **skill-developer**: Meta-skill for creating and managing Claude Code skills (tech-agnostic)

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

## Next Steps

1. Test skill activation: "Explain how skill triggers work"
2. Create your first .NET-specific skill using skill-developer
3. Use dev docs pattern for next complex task
