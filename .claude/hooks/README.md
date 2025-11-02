# Hooks

Claude Code hooks that enable skill auto-activation, file tracking, and validation.

## Dependencies & Installation

All dependencies are installed via npm (no system packages required):

```bash
cd .claude/hooks
npm install
```

This installs:
- **tsx** - TypeScript execution engine
- **node-jq** - JSON parsing (no system jq needed!)
- **typescript** - TypeScript compiler
- **@types/node** - Node.js type definitions

**Requirements**: Node.js v14+ and npm

---

## What Are Hooks?

Hooks are scripts that run at specific points in Claude's workflow:

- **UserPromptSubmit**: When user submits a prompt
- **PreToolUse**: Before a tool executes
- **PostToolUse**: After a tool completes
- **Stop**: When user requests to stop

**Key insight:** Hooks can modify prompts, block actions, and track state - enabling features Claude can't do alone.

---

## Essential Hooks (Start Here)

### skill-activation-prompt (UserPromptSubmit)

**Purpose:** Automatically suggests relevant skills based on user prompts and file context

**How it works:**

1. Reads `skill-rules.json`
2. Matches user prompt against trigger patterns
3. Checks which files user is working with
4. Injects skill suggestions into Claude's context

**Why it's essential:** This is THE hook that makes skills auto-activate.

**Integration:**

```bash
# Copy both files
cp skill-activation-prompt.sh your-project/.claude/hooks/
cp skill-activation-prompt.ts your-project/.claude/hooks/

# Make executable
chmod +x your-project/.claude/hooks/skill-activation-prompt.sh

# Install dependencies
cd your-project/.claude/hooks
npm install
```

**Add to settings.json:**

```json
{
  "hooks": {
    "UserPromptSubmit": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "$CLAUDE_PROJECT_DIR/.claude/hooks/skill-activation-prompt.sh"
          }
        ]
      }
    ]
  }
}
```

**Customization:** ✅ None needed - reads skill-rules.json automatically

---

### post-tool-use-tracker (PostToolUse)

**Purpose:** Tracks file changes to maintain context across sessions

**How it works:**

1. Monitors Edit/Write/MultiEdit tool calls
2. Records which files were modified
3. Creates cache for context management
4. Auto-detects project structure (frontend, backend, packages, etc.)

**Why it's essential:** Helps Claude understand what parts of your codebase are active.

**Integration:**

```bash
# Copy file
cp post-tool-use-tracker.sh your-project/.claude/hooks/

# Make executable
chmod +x your-project/.claude/hooks/post-tool-use-tracker.sh
```

**Add to settings.json:**

```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Edit|MultiEdit|Write",
        "hooks": [
          {
            "type": "command",
            "command": "$CLAUDE_PROJECT_DIR/.claude/hooks/post-tool-use-tracker.sh"
          }
        ]
      }
    ]
  }
}
```

**Customization:** ✅ None needed - auto-detects structure

---

**Purpose:** TypeScript compilation check when user stops

**⚠️ WARNING:** Configured for multi-service monorepo structure

**Integration:**

**First, determine if this is right for you:**

- ✅ Use if: Multi-service TypeScript monorepo
- ❌ Skip if: Single-service project or different build setup

**If using:**

1. Copy tsc-check.sh
2. **EDIT the service detection (line ~28):**
   ```bash
   # Replace example services with YOUR services:
   case "$repo" in
       api|web|auth|payments|...)  # ← Your actual services
   ```
3. Test manually before adding to settings.json

**Customization:** ⚠️⚠️⚠️ Heavy

---

---
