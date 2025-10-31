# Claude Code Infrastructure Setup - Implementation Plan

## Executive Summary

This plan implements Claude Code infrastructure from the showcase repository into the .NET 10 project, including:
- Auto-activating skills via hooks (UserPromptSubmit + PostToolUse)
- skill-developer meta-skill (tech-agnostic)
- Dev docs system for context persistence
- Slash commands for automated dev docs creation (/dev-docs, /dev-docs-update)
- Modular skill pattern (500-line rule with progressive disclosure)

**Total Estimated Time:** 50-70 minutes (Claude Code implementation)

**Approach:** Incremental installation with validation at each step to ensure system works before proceeding.

---

## Context Optimization Strategy

### Why This Matters
Each phase loads minimal context needed for that step, proving functionality before moving forward. This prevents context buildup and allows for mid-implementation resets without losing progress.

### Implementation Strategy
1. **Incremental validation**: Test after each phase before proceeding
2. **Minimal file reading**: Only read files needed for current step
3. **Progressive disclosure**: Install foundation → test → add complexity
4. **State tracking**: Use dev docs to survive context resets
5. **Rollback points**: Clear checkpoints if issues arise

---

## Prerequisites Check

**Before starting, verify:**
- [ ] Source repository exists at `C:\Users\bobby\src\claude\claude-code-infrastructure-showcase`
- [ ] Current project is at `C:\Users\bobby\src\claude\net10-project-example`
- [ ] Node.js is installed (for TypeScript hooks)
- [ ] Write permissions in project directory

**Assumption Validation:**
- Source repo structure matches documentation (hooks/, skills/, dev/ directories)
- Project has no existing `.claude` directory (confirmed clean slate)

---

## Phase 1: Foundation Setup (10 minutes)

### Objective
Create basic `.claude` directory structure and validate accessibility.

### Tasks

#### 1.1: Create Directory Structure
```bash
mkdir .claude
mkdir .claude\hooks
mkdir .claude\skills
mkdir dev
mkdir dev\active
```

**Acceptance Criteria:**
- All directories exist
- No permission errors
- Can write to all directories

**Validation Command:**
```bash
ls .claude
ls dev
```

**Expected Output:**
```
.claude/hooks/
.claude/skills/
dev/active/
```

#### 1.2: Verify Source Repository Access
**Read and confirm these files exist:**
- Source: `.claude/hooks/skill-activation-prompt.ts`
- Source: `.claude/hooks/skill-activation-prompt.sh`
- Source: `.claude/hooks/post-tool-use-tracker.sh`
- Source: `.claude/skills/skill-developer/SKILL.md`
- Source: `.claude/skills/skill-rules.json`

**Acceptance Criteria:**
- All source files readable
- No access errors

**Context Optimization:** Only verify file existence, don't read contents yet.

#### 1.3: Create Initial settings.json
Create `.claude/settings.json` with minimal configuration (hooks will be added later):

```json
{
  "version": "1.0",
  "hooks": {}
}
```

**Acceptance Criteria:**
- File created
- Valid JSON syntax

**Validation:**
```bash
type .claude\settings.json | jq .
```

---

## Phase 2: Install Hooks (15 minutes)

### Objective
Install and configure the two essential hooks that enable skill auto-activation.

### Context Window Strategy
This phase requires reading 3 TypeScript/shell files. If context is tight, read one at a time, copy, then move to next.

### Tasks

#### 2.1: Copy Hook Files
**Copy from source to project:**

```bash
# skill-activation-prompt (TypeScript + shell wrapper)
cp C:\Users\bobby\src\claude\claude-code-infrastructure-showcase\.claude\hooks\skill-activation-prompt.ts .claude\hooks\
cp C:\Users\bobby\src\claude\claude-code-infrastructure-showcase\.claude\hooks\skill-activation-prompt.sh .claude\hooks\

# post-tool-use-tracker (shell only)
cp C:\Users\bobby\src\claude\claude-code-infrastructure-showcase\.claude\hooks\post-tool-use-tracker.sh .claude\hooks\
```

**Acceptance Criteria:**
- All 3 files copied successfully
- Files are readable

**Validation:**
```bash
ls .claude\hooks\
```

**Expected Output:**
```
skill-activation-prompt.ts
skill-activation-prompt.sh
post-tool-use-tracker.sh
```

#### 2.2: Install Hook Dependencies

Check if package.json exists in source hooks directory, copy if needed:

```bash
# Check for package.json
ls C:\Users\bobby\src\claude\claude-code-infrastructure-showcase\.claude\hooks\package.json

# If exists, copy and install
cp C:\Users\bobby\src\claude\claude-code-infrastructure-showcase\.claude\hooks\package.json .claude\hooks\
cp C:\Users\bobby\src\claude\claude-code-infrastructure-showcase\.claude\hooks\package-lock.json .claude\hooks\ 2>nul || echo "No lock file"
cd .claude\hooks
npm install
cd ..\..
```

**Acceptance Criteria:**
- package.json copied (if exists)
- npm install completes without errors
- node_modules directory created

**Validation:**
```bash
ls .claude\hooks\node_modules
```

#### 2.3: Make Hooks Executable (Windows)

On Windows, scripts need proper permissions:

```powershell
# Verify scripts can be read
type .claude\hooks\skill-activation-prompt.sh
type .claude\hooks\post-tool-use-tracker.sh
```

**Note:** Windows doesn't use chmod. Verify scripts are accessible via PowerShell.

**Acceptance Criteria:**
- Scripts can be read
- No access denied errors

#### 2.4: Configure Hooks in settings.json

Update `.claude/settings.json` to register hooks:

```json
{
  "version": "1.0",
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
    ],
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

**Acceptance Criteria:**
- Valid JSON
- Both hooks registered
- Paths use $CLAUDE_PROJECT_DIR variable

**Validation:**
```bash
type .claude\settings.json | jq .
```

**Context Optimization:** At this point, if context is getting full, create a dev doc checkpoint before proceeding.

---

## Phase 3: Install skill-developer Skill (10 minutes)

### Objective
Install the skill-developer meta-skill with all its resource files.

### Tasks

#### 3.1: Create skill-developer Directory Structure

```bash
mkdir .claude\skills\skill-developer
mkdir .claude\skills\skill-developer\resources
```

**Acceptance Criteria:**
- Directories created
- No errors

#### 3.2: Copy skill-developer Files

The skill-developer has these files (verify first):
- SKILL.md (main skill file)
- Multiple resource files in resources/ subdirectory

**Strategy:** Copy main SKILL.md first, then resources:

```bash
# Copy main skill file
cp C:\Users\bobby\src\claude\claude-code-infrastructure-showcase\.claude\skills\skill-developer\SKILL.md .claude\skills\skill-developer\

# List resource files first to see what needs copying
ls C:\Users\bobby\src\claude\claude-code-infrastructure-showcase\.claude\skills\skill-developer\

# Copy all resource files (if resources directory exists)
xcopy C:\Users\bobby\src\claude\claude-code-infrastructure-showcase\.claude\skills\skill-developer\*.md .claude\skills\skill-developer\ /Y
```

**Acceptance Criteria:**
- SKILL.md copied
- All resource .md files copied
- No copy errors

**Validation:**
```bash
ls .claude\skills\skill-developer\
```

**Expected:** SKILL.md plus any additional .md files

#### 3.3: Create skill-rules.json

Create `.claude/skills/skill-rules.json` with ONLY skill-developer entry:

```json
{
  "version": "1.0",
  "description": "Skill activation triggers for Claude Code in .NET 10 project",
  "skills": {
    "skill-developer": {
      "type": "domain",
      "enforcement": "suggest",
      "priority": "high",
      "description": "Meta-skill for creating and managing Claude Code skills",
      "promptTriggers": {
        "keywords": [
          "skill system",
          "create skill",
          "add skill",
          "skill triggers",
          "skill rules",
          "hook system",
          "skill development",
          "skill-rules.json"
        ],
        "intentPatterns": [
          "(how do|how does|explain).*?skill",
          "(create|add|modify|build).*?skill",
          "skill.*?(work|trigger|activate|system)"
        ]
      }
    }
  }
}
```

**Acceptance Criteria:**
- Valid JSON
- Only skill-developer entry present
- Trigger patterns match source repository

**Validation:**
```bash
type .claude\skills\skill-rules.json | jq .
```

**Context Optimization:** This is a small file, no context issues.

---

## Phase 4: Test Skill Auto-Activation (5 minutes)

### Objective
Prove the skill auto-activation system works before proceeding.

### Tasks

#### 4.1: Manual Hook Test

Test the skill-activation-prompt hook directly:

```bash
cd .claude\hooks
echo {"session_id":"test","prompt":"how do I create a new skill?"} | npx tsx skill-activation-prompt.ts
cd ..\..
```

**Expected Output:**
Should output a formatted reminder mentioning skill-developer skill.

**Acceptance Criteria:**
- Hook executes without errors
- Detects "create a new skill" trigger
- Suggests skill-developer skill

**If this fails:**
- Check node_modules installed correctly
- Verify skill-rules.json path is correct
- Check TypeScript files for syntax errors

#### 4.2: Test with Claude Code Session

**Restart Claude Code** to pick up new settings.json configuration.

**Test prompt:** "Explain how the skill system works"

**Expected Behavior:**
- Hook should trigger before Claude sees prompt
- Should inject reminder about skill-developer skill
- Claude should mention or use the skill

**Acceptance Criteria:**
- skill-developer activates on relevant prompts
- No errors in hook execution

#### 4.3: Verify PostToolUse Hook

Create a test file to trigger PostToolUse hook:

```bash
# Create test file
echo "// Test file" > test-hook.txt
```

**Expected Behavior:**
- post-tool-use-tracker.sh should execute after write
- Should track the file change

**Acceptance Criteria:**
- No errors during write operation
- Hook executes silently (may create state files)

**Cleanup:**
```bash
del test-hook.txt
```

---

## Phase 5: Dev Docs System Setup (15 minutes)

### Objective
Set up the dev docs pattern for maintaining context across resets, including slash commands for automated dev docs creation.

### Tasks

#### 5.1: Create Directory Structures

```bash
mkdir dev
mkdir dev\active
mkdir .claude\commands
```

**Acceptance Criteria:**
- All directories exist
- Can write to them

#### 5.2: Copy Dev Docs Slash Commands

Copy the two dev docs slash commands from source repository:

```bash
cp C:\Users\bobby\src\claude\claude-code-infrastructure-showcase\.claude\commands\dev-docs.md .claude\commands\
cp C:\Users\bobby\src\claude\claude-code-infrastructure-showcase\.claude\commands\dev-docs-update.md .claude\commands\
```

**Acceptance Criteria:**
- Both command files copied successfully
- Files are readable

**Validation:**
```bash
ls .claude\commands
```

**Expected Output:**
```
dev-docs.md
dev-docs-update.md
```

**Verify command content:**
```bash
type .claude\commands\dev-docs.md | head -30
```

**Expected:** Should see command description and instructions for creating dev docs.

**What these commands do:**
- **/dev-docs**: Creates new dev docs (plan, context, tasks) for a task
- **/dev-docs-update**: Updates existing dev docs before context reset

#### 5.3: Copy Dev Docs README

```bash
cp C:\Users\bobby\src\claude\claude-code-infrastructure-showcase\dev\README.md dev\
```

**Acceptance Criteria:**
- README.md copied
- Explains dev docs pattern

**Validation:**
```bash
type dev\README.md | head -20
```

**Expected:** Should see dev docs pattern explanation

#### 5.4: Create This Setup as Dev Doc (Meta!)

Create dev docs for THIS setup task as an example:

```bash
mkdir dev\active\001-claude-code-setup
```

Create three files:
1. `001-claude-code-setup-plan.md` (copy this implementation plan)
2. `001-claude-code-setup-context.md` (track progress)
3. `001-claude-code-setup-tasks.md` (checklist)

**Content for context.md:**
```markdown
# Claude Code Infrastructure Setup - Context

## SESSION PROGRESS

### ✅ COMPLETED
- [Updated during implementation]

### 🟡 IN PROGRESS
- [Current phase]

### ⏳ NOT STARTED
- [Remaining phases]

## Key Files

**.claude/settings.json**
- Hook configurations for UserPromptSubmit and PostToolUse

**.claude/skills/skill-rules.json**
- Skill trigger definitions (currently only skill-developer)

**.claude/hooks/**
- skill-activation-prompt.ts/sh - Auto-suggests skills based on prompts
- post-tool-use-tracker.sh - Tracks file changes

**.claude/skills/skill-developer/**
- Meta-skill for creating new skills
- Tech-agnostic, works with any language

## Quick Resume
1. Read this file for current state
2. Check tasks.md for next steps
3. Continue from IN PROGRESS phase
```

**Acceptance Criteria:**
- Three files created
- Context file has SESSION PROGRESS section
- Plan file contains this implementation plan
- Tasks file has checklist format

#### 5.5: Test Dev Docs Pattern and Slash Commands

**Test 1: Context Reset Recovery**

After creating these files, test context reset recovery:

**Simulate:** "I need to resume the Claude Code setup task"

**Expected:** Claude should be instructed to read files in dev/active/001-claude-code-setup/

**Test 2: Slash Commands Available**

Verify slash commands are accessible:

**Check:** Type `/dev` in Claude Code and see if autocomplete shows:
- /dev-docs
- /dev-docs-update

**Or test directly:** "/dev-docs test command availability"

**Expected:** Command should be recognized (may create test dev docs or prompt for task name)

**Acceptance Criteria:**
- Dev docs are readable
- Contain enough info to resume work
- Follow three-file pattern
- Slash commands are recognized by Claude Code
- Commands are accessible via `/` prefix

---

## Phase 6: Documentation & Validation (10 minutes)

### Objective
Document the installation, create usage guide, and perform final validation.

### Tasks

#### 6.1: Create Project-Specific Documentation

Create `.claude/README.md`:

```markdown
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
```

**Acceptance Criteria:**
- README.md created in .claude directory
- Explains what's installed
- Provides usage guidance
- Mentions .NET 10 compatibility

#### 6.2: Update Project CLAUDE.md

Add section to existing `CLAUDE.md` about the Claude Code infrastructure:

**Add at end:**
```markdown
## Claude Code Infrastructure

This project uses Claude Code infrastructure for enhanced development workflow:

### Installed Components
- **Auto-activating skills** via hooks
- **skill-developer** meta-skill for creating project-specific skills
- **Dev docs system** for context persistence across sessions

### Configuration
- `.claude/` directory contains skills, hooks, and configuration
- `dev/active/` contains development documentation for complex tasks

### Usage
Skills activate automatically based on your prompts and file context. See `.claude/README.md` for details.

### Creating .NET-Specific Skills
Use skill-developer to create skills tailored to this .NET 10 project:
- ASP.NET Core MVC patterns
- Minimal API best practices
- MSTest with Microsoft.Testing.Platform
- .NET 10 specific guidance

Start with: "I want to create a skill for [ASP.NET Core/testing/etc]"
```

**Acceptance Criteria:**
- CLAUDE.md updated
- New section at end
- Mentions .NET-specific skill creation potential

#### 6.3: Comprehensive Validation Checklist

Run through complete validation:

**File Structure Check:**
```bash
# Verify all files exist
ls .claude
ls .claude\hooks
ls .claude\skills
ls .claude\skills\skill-developer
ls dev
ls dev\active
```

**Expected:**
```
.claude/
  commands/
    dev-docs.md
    dev-docs-update.md
  hooks/
    skill-activation-prompt.ts
    skill-activation-prompt.sh
    post-tool-use-tracker.sh
    package.json
    node_modules/
  skills/
    skill-developer/
      SKILL.md
      [resource files]
    skill-rules.json
  settings.json
  README.md

dev/
  active/
    001-claude-code-setup/
  README.md
```

**Configuration Validation:**
```bash
# Verify JSON files are valid
type .claude\settings.json | jq .
type .claude\skills\skill-rules.json | jq .
```

**Hook Execution Test:**
```bash
cd .claude\hooks
echo {"prompt":"how do skills work"} | npx tsx skill-activation-prompt.ts
cd ..\..
```

**skill-developer Content Check:**
```bash
type .claude\skills\skill-developer\SKILL.md | head -50
```

Should see YAML frontmatter and skill content.

**Acceptance Criteria:**
- [ ] All files exist in correct locations
- [ ] All JSON files are valid
- [ ] skill-activation-prompt executes without errors
- [ ] skill-developer SKILL.md is readable
- [ ] Node modules installed correctly
- [ ] Dev docs structure in place
- [ ] Slash commands (dev-docs, dev-docs-update) are present

#### 6.4: Create Success Report

Create `docs/features/001-claude-code-setup/installation-report.md`:

```markdown
# Claude Code Infrastructure Installation Report

**Date:** [YYYY-MM-DD]
**Duration:** [Actual time taken]

## Installed Components

### Hooks
✅ skill-activation-prompt (UserPromptSubmit)
✅ post-tool-use-tracker (PostToolUse)

### Skills
✅ skill-developer (meta-skill)

### Slash Commands
✅ /dev-docs (creates dev docs)
✅ /dev-docs-update (updates dev docs)

### Dev Docs System
✅ Directory structure created
✅ README.md documented
✅ Example dev docs created (this setup)

## Validation Results

✅ All files copied successfully
✅ JSON configurations valid
✅ Hooks execute without errors
✅ skill-developer skill readable
✅ Node dependencies installed

## Testing Performed

### Hook Execution Test
- Command: `echo {"prompt":"..."} | npx tsx skill-activation-prompt.ts`
- Result: SUCCESS - skill-developer detected

### File Structure Test
- Verified all directories and files present
- Result: SUCCESS - complete structure

### Configuration Test
- Validated settings.json and skill-rules.json
- Result: SUCCESS - valid JSON

## Next Steps

1. Restart Claude Code to load new configuration
2. Test skill activation with prompt: "Explain how skill triggers work"
3. Create first .NET-specific skill using skill-developer
4. Use dev docs pattern for next complex task

## Notes

- Installation on Windows (paths use backslashes)
- .NET 10 project (skill-developer is tech-agnostic)
- No .NET-specific skills yet (can be created using skill-developer)
- hooks/node_modules directory gitignored (should add to .gitignore)

## Issues Encountered

[Document any issues and resolutions]
```

**Acceptance Criteria:**
- Report created
- Documents what was installed
- Lists validation results
- Provides next steps

---

## Phase 7: Cleanup & .gitignore (5 minutes)

### Objective
Clean up temporary files and configure version control.

### Tasks

#### 7.1: Update .gitignore

Add to project's `.gitignore`:

```gitignore
# Claude Code Infrastructure
.claude/hooks/node_modules/
.claude/hooks/package-lock.json
.claude/hooks/state/
```

**Rationale:**
- node_modules should not be committed
- package-lock.json can be regenerated
- state/ directory contains session-specific data

**Acceptance Criteria:**
- .gitignore updated
- Infrastructure files still tracked (settings.json, skills, etc.)

#### 7.2: Verify Git Status

```bash
git status
```

**Expected:**
New files to be committed:
- .claude/ (most files)
- dev/ (most files)
- docs/features/001-claude-code-setup/

Ignored:
- .claude/hooks/node_modules/
- .claude/hooks/state/ (if created)

**Acceptance Criteria:**
- Appropriate files tracked
- Large/generated files ignored

---

## Rollback Plan

If issues occur at any phase:

### Phase 1 Rollback
```bash
rmdir /s /q .claude
rmdir /s /q dev
```

### Phase 2 Rollback
```bash
rmdir /s /q .claude\hooks
# Restore settings.json to minimal version
```

### Phase 3 Rollback
```bash
rmdir /s /q .claude\skills
```

### Complete Rollback
```bash
rmdir /s /q .claude
rmdir /s /q dev
git checkout .gitignore
```

---

## Context Reset Recovery

If context resets during implementation:

1. **Read:** `dev/active/001-claude-code-setup/001-claude-code-setup-context.md`
2. **Check:** What phase was IN PROGRESS
3. **Resume:** Start validation of that phase, then continue

**Key information preserved:**
- Which phases completed successfully
- Current phase progress
- Any issues encountered
- File paths and commands

---

## Success Criteria

Installation is successful when:

✅ **Structure Complete:**
- .claude/ directory with hooks/, skills/, commands/, settings.json
- dev/active/ directory with README.md
- docs/features/001-claude-code-setup/ with plan and reports

✅ **Functionality Verified:**
- skill-activation-prompt executes without errors
- skill-developer skill readable and properly formatted
- JSON configurations valid
- Slash commands (/dev-docs, /dev-docs-update) accessible

✅ **Documentation Present:**
- .claude/README.md explains infrastructure
- CLAUDE.md updated with Claude Code section
- Installation report documents process

✅ **Testing Passed:**
- Hook test executes successfully
- Skill detection works for trigger keywords
- File structure matches expected layout

✅ **Version Control Configured:**
- .gitignore excludes node_modules and state
- Infrastructure files tracked appropriately

---

## Estimated Timeline

| Phase | Duration | Context Risk |
|-------|----------|--------------|
| 1. Foundation Setup | 10 min | Low |
| 2. Install Hooks | 15 min | Medium |
| 3. Install skill-developer | 10 min | Low |
| 4. Test Activation | 5 min | Low |
| 5. Dev Docs System + Commands | 15 min | Low |
| 6. Documentation | 10 min | Medium |
| 7. Cleanup | 5 min | Low |

**Total:** 50-70 minutes (Claude Code implementation)

**Context Checkpoints:**
- After Phase 2 (before Phase 3)
- After Phase 4 (if needed)
- After Phase 6 (before final cleanup)

---

## Next Steps After Installation

1. **Test the System:**
   - Prompt: "Explain how skill triggers work"
   - Expected: skill-developer activates

2. **Create First .NET Skill:**
   - Prompt: "I want to create a skill for ASP.NET Core MVC best practices"
   - Use skill-developer to guide creation
   - Follow 500-line rule and modular pattern

3. **Use Dev Docs with Slash Commands:**
   - For next complex task, use: `/dev-docs [task description]`
   - Command automatically creates three-file structure
   - Update with `/dev-docs-update` before context resets
   - Or create manually in dev/active/ following three-file pattern

4. **Extend Infrastructure:**
   - Create more .NET-specific skills as needed
   - All skills auto-activate via existing hooks
   - No additional hook configuration needed

---

## Appendix: Commands Quick Reference

### Validation Commands
```bash
# Check structure
ls .claude
ls .claude\hooks
ls .claude\skills\skill-developer
ls .claude\commands

# Validate JSON
type .claude\settings.json | jq .
type .claude\skills\skill-rules.json | jq .

# Verify slash commands exist
type .claude\commands\dev-docs.md | head -20
type .claude\commands\dev-docs-update.md | head -20

# Test hook
cd .claude\hooks
echo {"prompt":"test"} | npx tsx skill-activation-prompt.ts
cd ..\..

# Check git status
git status
```

### Troubleshooting Commands
```bash
# If hook fails
cd .claude\hooks
npm install
npx tsc --version
cd ..\..

# If files missing
ls C:\Users\bobby\src\claude\claude-code-infrastructure-showcase\.claude

# If permissions issue
icacls .claude /grant %USERNAME%:F /T
```

---

**Plan Status:** READY FOR IMPLEMENTATION
**Next Action:** Begin Phase 1 - Foundation Setup
