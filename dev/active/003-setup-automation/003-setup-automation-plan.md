# Claude Code Environment Setup Automation - Implementation Plan

**Last Updated:** 2025-11-02

## Executive Summary

Create a comprehensive PowerShell 7+ automation script that replicates the Claude Code infrastructure setup across team environments. The script will automate the installation of hooks, skills, agents, slash commands, and dev docs system from the showcase repository into any .NET 10 project.

**Target Users:** Development team members setting up Claude Code infrastructure on Windows with WSL2

**Estimated Script Runtime:** 3-5 minutes (with manual source repo location input)

**Approach:** Defensive automation with validation at each step, clear error messages, and rollback capability

---

## Current State Analysis

### What Was Done Manually

The initial setup followed a manual 7-phase process documented in [001-claude-code-setup-plan.md](../../archive/001-claude-code-setup/001-claude-code-setup-plan.md):

1. ✅ Created `.claude/` directory structure (hooks, skills, agents, commands)
2. ✅ Copied hook files from showcase repository
3. ✅ Installed npm dependencies via WSL
4. ✅ Configured `settings.json` with hook registrations
5. ✅ Copied 8 skills (skill-developer + azure-devops + 6 .NET 10 skills)
6. ✅ Copied 6 specialized agents
7. ✅ Copied 2 slash commands (/dev-docs, /dev-docs-update)
8. ✅ Created dev docs system structure
9. ✅ Updated documentation (CLAUDE.md, .claude/README.md)
10. ✅ Configured .gitignore

**Source Repository:** `C:\Users\bobby\src\claude\claude-code-infrastructure-showcase`

**Environment Details:**
- **OS:** Windows with WSL2 (Ubuntu)
- **WSL Distro:** Ubuntu (Linux kernel 6.6.87.2)
- **Node.js:** v18.19.1 (in WSL)
- **npm:** 9.2.0 (in WSL)
- **PowerShell:** 7+ required

### Current State - What Works

All components successfully installed and validated:

**Hooks (3 files):**
- `skill-activation-prompt.ts` + `.sh` wrapper
- `post-tool-use-tracker.sh`
- Dependencies: tsx, typescript, @types/node, node-jq

**Skills (8 total):**
- skill-developer (7 files)
- azure-devops
- 6 .NET 10 skills (mstest, playwright, centralized-packages, minimal-apis, cli-essentials, aspnet-configuration)

**Agents (6 total):**
- code-architecture-reviewer, code-refactor-master, documentation-architect, plan-reviewer, refactor-planner, web-research-specialist

**Configuration:**
- settings.json with hook registrations
- skill-rules.json with trigger definitions
- 2 slash commands functional

**Documentation:**
- .claude/README.md, ATTRIBUTION.md, TROUBLESHOOTING.md
- dev/README.md
- CLAUDE.md updated

### Challenges to Automate

1. **Source Repository Location:** May vary per developer (needs parameterization)
2. **WSL Path Translation:** Windows paths → WSL paths for npm operations
3. **npm in WSL:** Must execute npm commands within WSL context
4. **Error Handling:** Graceful failures with clear diagnostics
5. **Idempotency:** Safe to re-run if partially completed
6. **Validation:** Comprehensive checks to ensure successful installation

---

## Proposed Future State

### Script Capabilities

**Primary Script:** `setup-claude-code-infrastructure.ps1`

**Features:**
- ✅ Prerequisites validation (WSL, Node.js, PowerShell 7+)
- ✅ Interactive source repository location prompt (with validation)
- ✅ Automated directory structure creation
- ✅ Bulk file copying with progress indicators
- ✅ WSL-aware npm dependency installation
- ✅ Configuration file generation (settings.json, skill-rules.json)
- ✅ Documentation creation and updates
- ✅ .gitignore updates
- ✅ Comprehensive validation suite
- ✅ Rollback capability on errors
- ✅ Installation report generation

**Optional Features:**
- Dry-run mode (`-DryRun`)
- Custom target directory parameter
- Skip validation flag for CI/CD
- Verbose logging option

---

## Implementation Phases

### Phase 1: Script Foundation (High Priority)

**Objective:** Create core script structure with parameter handling and prerequisites validation

#### Tasks

**1.1: Script Header and Parameters**
- Effort: S
- Define script parameters: `-SourceRepoPath`, `-TargetPath`, `-DryRun`, `-SkipValidation`, `-Verbose`
- Add script metadata (description, version, author, license)
- Include usage examples in comment-based help
- **Acceptance Criteria:** Script accepts parameters and shows help with `Get-Help`

**1.2: Prerequisites Validation Functions**
```powershell
function Test-Prerequisites {
    # Check PowerShell version (7+)
    # Check WSL installed and running
    # Check source repository exists
    # Check target directory writeable
    # Check WSL Node.js/npm available
}
```
- Effort: M
- Validate all dependencies before proceeding
- Provide clear error messages with remediation steps
- **Acceptance Criteria:** Returns $true/$false with detailed diagnostics

**1.3: Logging and Output Functions**
```powershell
function Write-LogMessage {
    # Structured logging with timestamps
    # Color-coded output (success/warning/error)
    # Optional verbose mode
}
```
- Effort: S
- Consistent output formatting
- **Acceptance Criteria:** All script output uses logging functions

**1.4: Error Handling Framework**
- Effort: S
- Set `$ErrorActionPreference = 'Stop'`
- Try-catch blocks for all operations
- Trap unexpected errors
- **Acceptance Criteria:** Script fails gracefully with meaningful errors

---

### Phase 2: Directory Structure Creation (High Priority)

**Objective:** Create all required directories in target project

#### Tasks

**2.1: Directory Creation Function**
```powershell
function New-ClaudeDirectories {
    # Create .claude/ structure
    # Create .claude/hooks, skills, agents, commands
    # Create dev/active
    # Validate creation
}
```
- Effort: S
- Create all directories atomically
- Handle existing directories gracefully
- **Acceptance Criteria:** All directories exist, no errors if already present

**2.2: Dry-Run Mode Support**
- Effort: S
- Mock directory creation in dry-run mode
- Show what would be created
- **Acceptance Criteria:** `-DryRun` shows planned operations without changes

---

### Phase 3: File Copying Operations (High Priority)

**Objective:** Copy all files from showcase repository to target project

#### Tasks

**3.1: Hook Files Copying**
```powershell
function Copy-HookFiles {
    # Copy skill-activation-prompt.ts, .sh
    # Copy post-tool-use-tracker.sh
    # Copy package.json
    # Validate file integrity
}
```
- Effort: S
- Copy 3 hook files + package.json
- Verify file sizes match
- **Acceptance Criteria:** All hook files copied successfully

**3.2: Skills Copying**
```powershell
function Copy-Skills {
    # Copy all 8 skill directories
    # Copy skill-rules.json
    # Validate all SKILL.md files present
}
```
- Effort: M
- Copy 8 skill directories with all resource files
- Handle skill-developer's 7 resource files
- **Acceptance Criteria:** All skills present with correct structure

**3.3: Agents Copying**
```powershell
function Copy-Agents {
    # Copy all 6 agent markdown files
    # Copy agents/README.md
    # Validate
}
```
- Effort: S
- Copy 6 agent definitions
- **Acceptance Criteria:** All agent files present

**3.4: Slash Commands and Dev Docs**
```powershell
function Copy-CommandsAndDevDocs {
    # Copy /dev-docs and /dev-docs-update commands
    # Copy dev/README.md
    # Create dev/active structure
}
```
- Effort: S
- Copy 2 command files and dev docs documentation
- **Acceptance Criteria:** Commands and dev docs structure ready

**3.5: Documentation Files**
```powershell
function Copy-Documentation {
    # Copy .claude/README.md, ATTRIBUTION.md, TROUBLESHOOTING.md
    # Validate content
}
```
- Effort: S
- Copy 3 documentation files
- **Acceptance Criteria:** All documentation present

**3.6: Progress Indicators**
- Effort: S
- Show progress bars during file operations
- Display current operation
- **Acceptance Criteria:** User sees clear progress during execution

---

### Phase 4: Configuration Generation (High Priority)

**Objective:** Generate configuration files with correct settings

#### Tasks

**4.1: settings.json Generation**
```powershell
function New-SettingsJson {
    # Generate settings.json with hook configurations
    # Use $CLAUDE_PROJECT_DIR variable for portability
    # Validate JSON syntax
}
```
- Effort: M
- Generate settings.json with both hooks registered
- Handle existing settings.json (backup + merge)
- **Acceptance Criteria:** Valid settings.json with correct hook paths

**4.2: skill-rules.json Validation**
- Effort: S
- Validate copied skill-rules.json is valid JSON
- Check all 8 skills have entries
- **Acceptance Criteria:** skill-rules.json valid and complete

---

### Phase 5: WSL npm Installation (High Priority)

**Objective:** Install npm dependencies via WSL

#### Tasks

**5.1: WSL Path Translation Function**
```powershell
function ConvertTo-WslPath {
    # Convert Windows path to WSL path
    # Handle drive letters (C:\ → /mnt/c/)
    # Handle spaces in paths
}
```
- Effort: M
- Reliable Windows → WSL path conversion
- Handle edge cases (spaces, special chars)
- **Acceptance Criteria:** Paths work correctly in WSL commands

**5.2: npm Install via WSL**
```powershell
function Install-HookDependencies {
    # Execute npm install in WSL context
    # Show npm output
    # Validate node_modules created
}
```
- Effort: M
- Run `wsl bash -c "cd <path> && npm install"`
- Capture and display npm output
- **Acceptance Criteria:** node_modules populated, no vulnerabilities

**5.3: Validation of npm Dependencies**
- Effort: S
- Check node_modules directory exists
- Verify key packages (tsx, typescript, node-jq)
- Test hook execution
- **Acceptance Criteria:** All dependencies installed correctly

---

### Phase 6: Documentation Updates (Medium Priority)

**Objective:** Update project documentation to reflect installed infrastructure

#### Tasks

**6.1: CLAUDE.md Update Function**
```powershell
function Update-ClaudeMd {
    # Check if CLAUDE.md exists
    # Append or update Claude Code Infrastructure section
    # Handle missing CLAUDE.md (offer to create)
}
```
- Effort: M
- Append standard infrastructure section to CLAUDE.md
- Check for existing section (don't duplicate)
- **Acceptance Criteria:** CLAUDE.md contains infrastructure documentation

**6.2: .gitignore Update Function**
```powershell
function Update-GitIgnore {
    # Add Claude Code exclusions
    # Check for existing rules (don't duplicate)
    # Validate .gitignore syntax
}
```
- Effort: S
- Add `.claude/hooks/node_modules/` and related paths
- **Acceptance Criteria:** .gitignore contains Claude Code rules

---

### Phase 7: Validation Suite (High Priority)

**Objective:** Comprehensive validation to ensure successful installation

#### Tasks

**7.1: File Structure Validation**
```powershell
function Test-FileStructure {
    # Verify all directories exist
    # Verify all files present
    # Check file sizes reasonable
}
```
- Effort: M
- Check 100+ files and directories
- Report any missing items
- **Acceptance Criteria:** Complete structure validated

**7.2: Configuration Validation**
```powershell
function Test-Configuration {
    # Validate settings.json (JSON syntax + structure)
    # Validate skill-rules.json (JSON syntax + all skills)
    # Check hook paths correct
}
```
- Effort: S
- Use JSON parsing to validate
- **Acceptance Criteria:** All configs valid

**7.3: Hook Execution Test**
```powershell
function Test-HookExecution {
    # Test skill-activation-prompt hook
    # Verify npm dependencies work
    # Check hook produces expected output
}
```
- Effort: M
- Execute hook with test input via WSL
- Parse output for success indicators
- **Acceptance Criteria:** Hook executes without errors

**7.4: Validation Report Generation**
- Effort: S
- Generate detailed validation report
- List all validated items
- Highlight any warnings/issues
- **Acceptance Criteria:** Clear pass/fail report

---

### Phase 8: Rollback and Error Recovery (Medium Priority)

**Objective:** Provide rollback capability if installation fails

#### Tasks

**8.1: Backup Function**
```powershell
function Backup-ExistingFiles {
    # Backup .claude/ if exists
    # Backup dev/ if exists
    # Store in .claude.backup.TIMESTAMP
}
```
- Effort: S
- Create backup before modifications
- **Acceptance Criteria:** Existing files safely backed up

**8.2: Rollback Function**
```powershell
function Invoke-Rollback {
    # Remove newly created files
    # Restore from backup
    # Clean up partial installation
}
```
- Effort: M
- Complete cleanup on failure
- Restore previous state
- **Acceptance Criteria:** Clean rollback to pre-installation state

**8.3: Error Recovery Guidance**
- Effort: S
- Provide clear error messages with next steps
- Link to troubleshooting documentation
- **Acceptance Criteria:** Users know how to proceed after errors

---

### Phase 9: Installation Report (Medium Priority)

**Objective:** Generate detailed installation report

#### Tasks

**9.1: Report Generation Function**
```powershell
function New-InstallationReport {
    # Generate markdown report
    # Include timestamp, duration, components
    # List validation results
    # Provide next steps
}
```
- Effort: M
- Create `INSTALLATION_REPORT.md` in project root or dev/
- Include all relevant details
- **Acceptance Criteria:** Comprehensive report generated

---

## Risk Assessment and Mitigation

### High-Risk Areas

**Risk 1: WSL Path Translation Failures**
- **Impact:** High - npm install will fail
- **Probability:** Medium - Special characters, spaces can break paths
- **Mitigation:** Extensive testing of path conversion function; validation before npm operations
- **Contingency:** Clear error messages with manual npm install instructions

**Risk 2: npm Dependency Installation Failures**
- **Impact:** High - Hooks won't function
- **Probability:** Low - Standard npm packages, no exotic dependencies
- **Mitigation:** Check network connectivity; validate package.json before install; provide offline installation option
- **Contingency:** Manual npm install instructions in error message

**Risk 3: Source Repository Not Found**
- **Impact:** High - Cannot proceed with installation
- **Probability:** Medium - Path varies per developer
- **Mitigation:** Interactive prompt with validation; file browser option; provide clone instructions
- **Contingency:** Script exits with clone instructions

**Risk 4: Partial Installation on Failure**
- **Impact:** Medium - Inconsistent state
- **Probability:** Medium - Various failure points possible
- **Mitigation:** Atomic operations where possible; backup existing files; comprehensive rollback function
- **Contingency:** Manual cleanup instructions provided

---

## Success Metrics

### Installation Success

**Primary Metrics:**
- ✅ All files copied successfully (100+ files)
- ✅ npm dependencies installed (4 packages)
- ✅ Configuration files valid JSON
- ✅ Hook execution test passes
- ✅ Validation suite reports 0 errors

**Secondary Metrics:**
- ✅ Script completes in < 5 minutes
- ✅ Clear progress indicators throughout
- ✅ Installation report generated
- ✅ Zero manual intervention required (after source repo prompt)

---

## Timeline Estimates

### Development Phase

| Phase | Tasks | Estimated Time |
|-------|-------|----------------|
| 1. Script Foundation | Parameters, prerequisites, logging, error handling | 1-2 hours |
| 2. Directory Creation | Directory structure function | 30 mins |
| 3. File Copying | 5 copy functions + progress indicators | 1-2 hours |
| 4. Configuration | settings.json generation, validation | 1 hour |
| 5. WSL npm Install | Path conversion, npm execution | 1 hour |
| 6. Documentation Updates | CLAUDE.md, .gitignore updates | 30 mins |
| 7. Validation Suite | 4 validation functions | 1 hour |
| 8. Rollback | Backup and rollback functions | 1 hour |
| 9. Reporting | Installation report generation | 30 mins |
| Testing & Refinement | Multiple test runs, bug fixes | 2-3 hours |

**Total Development Time:** 8-12 hours (1-2 days)

---

**Plan Status:** READY FOR IMPLEMENTATION
**Next Action:** Generate PowerShell 7+ automation script
