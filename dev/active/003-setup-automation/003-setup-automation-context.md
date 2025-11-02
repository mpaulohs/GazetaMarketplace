# Claude Code WSL Environment Setup - Context

**Last Updated:** 2025-11-02

## SESSION PROGRESS

### ✅ COMPLETED
- Clarified requirement - WSL system setup, NOT project infrastructure setup
- Examined current WSL environment and Claude Code CLI installation
- Analyzed manual setup process (Ubuntu, Node.js, npm, Claude Code)
- Created comprehensive implementation plan (003-setup-automation-plan.md)
- Generated PowerShell 7+ WSL automation script (setup-claude-code-wsl.ps1)
- Deleted incorrect project infrastructure script (setup-claude-code-infrastructure.ps1)
- Updated dev docs with correct scope (WSL setup, not project setup)
- Created complete dev docs structure (plan, context, tasks)

### 🟡 IN PROGRESS
- None

### ⏳ NOT STARTED
- Script testing on clean Windows installation
- Script testing on machine with existing WSL
- Documentation for team distribution
- Team rollout and training

## Key Files

### Created Files

**setup-claude-code-wsl.ps1** (root directory)
- Complete WSL setup automation script
- PowerShell 7+ compatible with Administrator requirement
- Features: backup, cleanup, WSL install, Node.js, npm config, Claude Code CLI
- ~650 lines with comprehensive error handling and validation
- **PURPOSE: Sets up Claude Code CLI in WSL environment (system-level)**

**dev/active/003-setup-automation/**
- 003-setup-automation-plan.md - Comprehensive WSL setup implementation plan
- 003-setup-automation-context.md - This file (session progress and context)
- 003-setup-automation-tasks.md - Checklist format for tracking

### Deleted Files

**setup-claude-code-infrastructure.ps1** ❌ DELETED
- Was incorrectly scoped for project infrastructure (hooks/skills/agents)
- Not what was requested - user wanted WSL setup, not project setup

### Reference Files

**dev/archive/001-claude-code-setup/**
- Contains documentation for project infrastructure setup (different scope)
- Not directly relevant to WSL setup task

**.claude/README.md** - Project infrastructure documentation (different scope)
**CLAUDE.md** - Project instructions (different scope)

## Environment Details

### Current WSL Setup (What Script Replicates)
- **OS:** Windows with WSL2
- **WSL Distro:** Ubuntu 24.04 (Linux kernel 6.6.87.2)
- **Node.js:** v18.19.1 (in WSL via nodesource)
- **npm:** 9.2.0 (in WSL)
- **npm global prefix:** ~/.npm-global (custom config)
- **Claude Code:** v2.0.31 (installed globally via npm)
- **Claude binary location:** ~/.npm-global/bin/claude (symlink)
- **PATH configuration:** export PATH=~/.npm-global/bin:$PATH (in ~/.bashrc)
- **Additional tools:** git, build-essential (gcc, g++, make), curl

### Manual Setup Steps (What Was Automated)
1. Enable WSL features via Windows Optional Features
2. Install Ubuntu 24.04 via `wsl --install`
3. Create user account in Ubuntu
4. Install Node.js 20 LTS via nodesource repository
5. Configure npm global directory to ~/.npm-global
6. Add ~/.npm-global/bin to PATH in .bashrc
7. Install Claude Code CLI: `npm install -g @anthropic-ai/claude-code`
8. Install additional development tools
9. Verify installation and login to Claude Code

## Script Features Implemented

### Core Functionality
✅ **Prerequisites validation:** PowerShell 7+, Windows 10/11, Administrator privileges
✅ **Backup capability:** Exports existing WSL Ubuntu distributions to timestamped backups
✅ **Safe cleanup:** Removes existing WSL Ubuntu (requires 'DELETE' confirmation)
✅ **WSL feature installation:** Enables required Windows features with reboot handling
✅ **Ubuntu installation:** Installs Ubuntu 24.04 via `wsl --install`
✅ **Node.js installation:** Installs Node.js LTS via nodesource repository
✅ **npm configuration:** Sets up ~/.npm-global directory and npm prefix
✅ **PATH configuration:** Adds npm global bin to PATH in .bashrc
✅ **Claude Code installation:** npm install -g @anthropic-ai/claude-code
✅ **Additional tools:** git, build-essential, curl
✅ **Comprehensive validation:** Tests WSL, Node.js, npm, Claude CLI, PATH
✅ **Installation report:** Next steps, troubleshooting, configuration details

### Script Parameters
- `-SkipBackup` - Skip backing up existing WSL (faster)
- `-SkipCleanup` - Preserve existing WSL installations
- `-UbuntuVersion` - Specify Ubuntu version (default: Ubuntu-24.04)
- `-NodeVersion` - Specify Node.js major version (default: 20)

### Helper Functions
- `Write-LogMessage` - Color-coded logging with timestamps
- `Write-SectionHeader` - Visual section separators
- `Test-Prerequisites` - Validates PowerShell 7+, Windows version, admin rights
- `Backup-ExistingWSL` - Exports WSL distributions to backups
- `Remove-ExistingWSL` - Safely unregisters WSL with confirmation
- `Install-WSLFeatures` - Enables WSL features with reboot detection
- `Install-Ubuntu` - Installs fresh Ubuntu distribution
- `Install-NodeJS` - Installs Node.js via nodesource
- `Configure-NPM` - Sets up npm global directory
- `Add-NPMToPath` - Updates .bashrc with PATH
- `Install-ClaudeCode` - npm install -g @anthropic-ai/claude-code
- `Install-AdditionalTools` - Installs git, build-essential, curl
- `Test-Installation` - Comprehensive validation suite
- `New-InstallationReport` - Displays completion report

### Error Handling
- `$ErrorActionPreference = 'Stop'` for fail-fast behavior
- Try-catch blocks around all major operations
- Administrator privilege check at start
- Reboot detection and user prompting
- Clear error messages with remediation steps
- Backup before destructive operations

### User Experience
- Color-coded output (Cyan/Green/Yellow/Red)
- Unicode symbols (ℹ ✓ ⚠ ✗) for visual clarity
- Progress indicators for each phase
- Clear section headers
- Explicit warnings for destructive operations
- Confirmation prompts for cleanup
- Comprehensive installation report with next steps

## Technical Decisions

### Why PowerShell 7+
- Cross-platform compatibility (though script Windows-specific)
- Modern syntax and features
- Native JSON support (not needed here, but good for extensibility)
- Better error handling than Windows PowerShell 5.1
- Required for advanced CmdletBinding features

### Why Require Administrator
- Enabling Windows Optional Features requires elevation
- WSL feature installation needs admin privileges
- Cannot proceed without these privileges

### Why Backup Before Cleanup
- Users may have work in existing WSL
- Provides safety net if they change their mind
- Timestamped backups allow restoration
- Optional with `-SkipBackup` for speed

### Why Require 'DELETE' Confirmation
- Destructive operation (deletes WSL data)
- Prevents accidental data loss
- Explicit confirmation required
- Cannot be bypassed (by design)

### Why nodesource Repository
- Official Node.js recommended method for Ubuntu
- Provides LTS versions
- Easier to specify exact version
- Includes npm automatically

### Why Custom npm Global Directory
- Avoids need for sudo on global installs
- User-specific installation location
- Cleaner than default /usr/local
- Follows npm best practices

### Why PATH in .bashrc
- Ensures `claude` command available in all shells
- Automatic activation on WSL start
- Standard Linux practice
- Idempotent (can re-run safely)

## Known Limitations

1. **Windows Only:** Script designed specifically for Windows with WSL2
2. **Administrator Required:** Cannot run without elevation
3. **Interactive Prompts:** Ubuntu user account creation requires manual input
4. **Reboot May Be Needed:** First-time WSL install may require restart
5. **Internet Required:** Must download Ubuntu, Node.js packages, npm packages
6. **Destructive by Default:** Wipes existing WSL unless `-SkipCleanup` used

## Testing Recommendations

### Test Scenarios
1. **Fresh Windows install:** No WSL, no Node.js, clean slate
2. **Existing WSL:** Test backup and cleanup functions
3. **Preserve existing WSL:** Test `-SkipCleanup` parameter
4. **Skip backup:** Test `-SkipBackup` for faster execution
5. **Reboot handling:** Test on machine requiring reboot for WSL
6. **Failed installation:** Test error recovery and reporting
7. **Re-run after failure:** Test idempotency

### Test Environments
- Windows 10 21H2 or later
- Windows 11 (any build)
- Various PowerShell 7.x versions (7.3, 7.4)
- Machines with/without existing WSL
- Different network conditions

## Quick Resume

**If context resets, read this section first:**

1. **What was done:** Created PowerShell script for WSL setup with Claude Code CLI
2. **Current state:** Script complete and ready for testing
3. **Key file:** `setup-claude-code-wsl.ps1` in project root
4. **Scope:** System-level WSL setup, NOT project infrastructure
5. **Next steps:** Test script, document usage, distribute to team

## Usage Instructions (for team)

### Prerequisites
- PowerShell 7+ installed ([Install](https://aka.ms/powershell))
- Windows 10 build 19041+ or Windows 11
- Internet connection for downloads
- Administrator account

### Running the Script

**⚠️ IMPORTANT: Run as Administrator!**

**Full setup (with backup and cleanup):**
```powershell
# Right-click PowerShell 7 → Run as Administrator
cd path\to\project
.\setup-claude-code-wsl.ps1
```

**Skip backup (faster):**
```powershell
.\setup-claude-code-wsl.ps1 -SkipBackup
```

**Preserve existing WSL:**
```powershell
.\setup-claude-code-wsl.ps1 -SkipCleanup
```

**Custom Ubuntu version:**
```powershell
.\setup-claude-code-wsl.ps1 -UbuntuVersion "Ubuntu-22.04"
```

**Custom Node.js version:**
```powershell
.\setup-claude-code-wsl.ps1 -NodeVersion 18
```

### What to Expect

1. **Prerequisites check** - Validates environment
2. **Backup existing WSL** - Exports to %USERPROFILE%\WSL-Backups
3. **Cleanup confirmation** - Type 'DELETE' to confirm removal
4. **WSL feature install** - May prompt for reboot
5. **Ubuntu install** - You'll create username/password
6. **Automated setup** - Node.js, npm, Claude Code install
7. **Validation** - Tests all components
8. **Installation report** - Next steps and troubleshooting

**Total time:** ~10-15 minutes

### After Installation

1. Open WSL: `wsl`
2. Verify Claude: `claude --version`
3. Login: `claude` (follow OAuth flow)
4. Navigate to project: `cd /mnt/c/your/project`
5. Start using: `claude`

## Important Distinctions

### This Script vs. Project Infrastructure Script

**This Script (setup-claude-code-wsl.ps1):**
- ✅ Sets up **Claude Code CLI** in **WSL environment**
- ✅ System-level setup (Windows, WSL, Node.js, npm)
- ✅ One-time per developer machine
- ✅ Creates WSL environment with `claude` command

**Deleted Script (setup-claude-code-infrastructure.ps1):**
- ❌ Was for setting up **project infrastructure** (hooks, skills, agents)
- ❌ Project-level setup (`.claude/` directory)
- ❌ One-time per project repository
- ❌ Not what was requested

**Key Difference:**
- **WSL setup = System-level** (this script)
- **Project infrastructure = Repository-level** (different script, not created here)

## Reference Guide

The manual process was documented in this guide:
https://claude.ai/public/artifacts/03a4aa0c-67b2-427f-838e-63770900bf1d

(Note: WebFetch returned 403, but the script was created based on examination of the actual WSL environment)

## Notes

- Script runtime: ~10-15 minutes (varies based on network speed)
- Requires Administrator privileges throughout
- Ubuntu user account creation is interactive (can't be automated)
- Reboot may be required for first-time WSL installation
- Backups saved to: %USERPROFILE%\WSL-Backups\[timestamp]
- All operations logged with color-coded output
- Validation suite confirms successful installation
- Safe to re-run if interrupted (mostly idempotent)

---

**Session Status:** COMPLETE ✅
**Deliverables:** ✅ Plan, ✅ Script, ✅ Dev Docs (all corrected for WSL setup scope)
**Next Action:** Test script on clean Windows installation
