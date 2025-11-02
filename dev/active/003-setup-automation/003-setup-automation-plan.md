# Claude Code WSL Environment Setup - Implementation Plan

**Last Updated:** 2025-11-02

## Executive Summary

Create a comprehensive PowerShell 7+ automation script that sets up a fresh WSL2 environment with Claude Code CLI for team members. The script automates the complete WSL setup process from scratch, including cleaning up any existing installations.

**Target Users:** Development team members who need to set up Claude Code on their Windows machines with WSL2

**Estimated Script Runtime:** 10-15 minutes (including Ubuntu installation and user account creation)

**Approach:** Defensive automation with backup capability, validation at each step, clear error messages, and option to preserve existing WSL installations

**Reference Guide:** https://claude.ai/public/artifacts/03a4aa0c-67b2-427f-838e-63770900bf1d

---

## Current State Analysis

### What Was Done Manually

After resetting WSL, the manual setup process included:

1. ✅ Enable WSL features (Microsoft-Windows-Subsystem-Linux, VirtualMachinePlatform)
2. ✅ Set WSL default version to 2
3. ✅ Install Ubuntu (24.04) via `wsl --install`
4. ✅ Create user account in Ubuntu
5. ✅ Install Node.js LTS via nodesource repository
6. ✅ Configure npm global directory (`~/.npm-global`)
7. ✅ Set npm prefix to use global directory
8. ✅ Add npm global bin to PATH in `~/.bashrc`
9. ✅ Install Claude Code CLI: `npm install -g @anthropic-ai/claude-code`
10. ✅ Install additional tools (git, build-essential, curl)
11. ✅ Verify `claude` command available

**Current Environment:**
- **OS:** Windows with WSL2
- **WSL Distro:** Ubuntu 24.04 (Linux kernel 6.6.87.2)
- **Node.js:** v18.19.1 (in WSL)
- **npm:** 9.2.0 (in WSL)
- **npm global prefix:** /home/bobby/.npm-global
- **Claude Code:** v2.0.31 (installed globally)
- **Claude binary:** ~/.npm-global/bin/claude (symlink)
- **PATH:** Configured in ~/.bashrc

### Challenges to Automate

1. **Administrator Privileges:** Script must run as admin to enable WSL features
2. **Reboot Requirement:** May need system reboot after enabling WSL features
3. **User Account Creation:** Ubuntu setup prompts for username/password interactively
4. **Existing WSL Cleanup:** Need safe backup and removal of existing installations
5. **PATH Updates:** Must update .bashrc and ensure changes take effect
6. **Validation:** Comprehensive checks to ensure each step succeeded
7. **Error Recovery:** Handle failures at various stages gracefully

---

## Proposed Future State

### Script Capabilities

**Primary Script:** `setup-claude-code-wsl.ps1`

**Core Features:**
- ✅ Prerequisites validation (PowerShell 7+, Windows version, admin rights)
- ✅ Optional backup of existing WSL distributions
- ✅ Safe cleanup of existing WSL Ubuntu installations (with confirmation)
- ✅ WSL feature installation with reboot handling
- ✅ Fresh Ubuntu installation via `wsl --install`
- ✅ Node.js LTS installation via nodesource
- ✅ npm global directory configuration
- ✅ PATH configuration in .bashrc
- ✅ Claude Code CLI installation via npm
- ✅ Additional tools installation (git, build-essential, curl)
- ✅ Comprehensive validation suite
- ✅ Installation report with next steps

**Optional Parameters:**
- `-SkipBackup` - Skip backing up existing WSL distributions
- `-SkipCleanup` - Preserve existing WSL installations (install alongside)
- `-UbuntuVersion` - Specify Ubuntu version (default: Ubuntu-24.04)
- `-NodeVersion` - Specify Node.js major version (default: 20)

---

## Implementation Phases

### Phase 1: Script Foundation (High Priority)

**Objective:** Create core script structure with parameter handling and prerequisites validation

#### Tasks

**1.1: Script Header and Parameters**
- Effort: S
- Define script parameters: `-SkipBackup`, `-SkipCleanup`, `-UbuntuVersion`, `-NodeVersion`
- Add script metadata with WARNING about WSL cleanup
- Include usage examples in comment-based help
- **Acceptance Criteria:** Script accepts parameters and shows help with `Get-Help`

**1.2: Prerequisites Validation Functions**
```powershell
function Test-Prerequisites {
    # Check PowerShell version (7+)
    # Check Windows version (build 19041+)
    # Check administrator privileges
    # Provide clear error messages
}
```
- Effort: M
- Validate all dependencies before proceeding
- Must run as Administrator
- **Acceptance Criteria:** Returns $true/$false with detailed diagnostics

**1.3: Logging and Output Functions**
- Effort: S
- Consistent output formatting with colors
- Timestamps for all messages
- **Acceptance Criteria:** All script output uses logging functions

**1.4: Error Handling Framework**
- Effort: S
- Set `$ErrorActionPreference = 'Stop'`
- Try-catch blocks for all operations
- Graceful failures with remediation guidance
- **Acceptance Criteria:** Script fails gracefully with meaningful errors

---

### Phase 2: Backup and Cleanup (High Priority)

**Objective:** Safely backup and remove existing WSL installations

#### Tasks

**2.1: Backup Function**
```powershell
function Backup-ExistingWSL {
    # List all Ubuntu distributions
    # Export each to timestamped .tar files
    # Save to %USERPROFILE%\WSL-Backups\
    # Validate backups created
}
```
- Effort: M
- Uses `wsl --export` to backup distributions
- Creates timestamped backup directory
- **Acceptance Criteria:** All Ubuntu distributions backed up successfully

**2.2: Cleanup Function**
```powershell
function Remove-ExistingWSL {
    # List Ubuntu distributions
    # Show user what will be deleted
    # Require 'DELETE' confirmation
    # Unregister each distribution
}
```
- Effort: M
- ⚠️ **DESTRUCTIVE OPERATION**
- Requires explicit confirmation
- Uses `wsl --unregister` to remove
- **Acceptance Criteria:** Safe removal with confirmation, clear warnings

---

### Phase 3: WSL Feature Installation (High Priority)

**Objective:** Enable WSL features and configure version 2

#### Tasks

**3.1: Feature Installation Function**
```powershell
function Install-WSLFeatures {
    # Check if WSL feature enabled
    # Enable Microsoft-Windows-Subsystem-Linux
    # Enable VirtualMachinePlatform
    # Handle reboot requirement
    # Set WSL default version to 2
}
```
- Effort: M
- Uses `Enable-WindowsOptionalFeature`
- Detects if reboot needed
- Prompts user for reboot (or manual restart)
- **Acceptance Criteria:** WSL features enabled, version 2 set as default

---

### Phase 4: Ubuntu Installation (High Priority)

**Objective:** Install fresh Ubuntu distribution

#### Tasks

**4.1: Ubuntu Install Function**
```powershell
function Install-Ubuntu {
    # Install specified Ubuntu version
    # Wait for installation completion
    # Handle user account creation prompt
}
```
- Effort: S
- Uses `wsl --install -d Ubuntu-24.04`
- User creates account interactively
- **Acceptance Criteria:** Ubuntu installed, user account created

---

### Phase 5: Node.js Installation (High Priority)

**Objective:** Install Node.js LTS in WSL via nodesource

#### Tasks

**5.1: Node.js Installation Function**
```powershell
function Install-NodeJS {
    # Update apt packages
    # Install prerequisites (ca-certificates, curl, gnupg)
    # Add nodesource GPG key
    # Add nodesource repository
    # Install Node.js
    # Verify installation
}
```
- Effort: M
- Executes bash script via `wsl bash`
- Installs specified Node.js version (default 20)
- Includes npm automatically
- **Acceptance Criteria:** Node.js and npm installed and working

---

### Phase 6: npm Configuration (High Priority)

**Objective:** Configure npm to use user-local global directory

#### Tasks

**6.1: npm Configuration Function**
```powershell
function Configure-NPM {
    # Create ~/.npm-global directory
    # Set npm prefix to ~/.npm-global
    # Verify configuration
}
```
- Effort: S
- Avoids need for sudo on global installs
- Uses `npm config set prefix`
- **Acceptance Criteria:** npm prefix set to ~/.npm-global

**6.2: PATH Configuration Function**
```powershell
function Add-NPMToPath {
    # Add ~/.npm-global/bin to PATH in .bashrc
    # Check if already present (idempotent)
    # Source .bashrc to apply changes
}
```
- Effort: S
- Appends to .bashrc if not present
- Sources .bashrc to activate immediately
- **Acceptance Criteria:** PATH includes ~/.npm-global/bin

---

### Phase 7: Claude Code Installation (High Priority)

**Objective:** Install Claude Code CLI globally via npm

#### Tasks

**7.1: Claude Code Installation Function**
```powershell
function Install-ClaudeCode {
    # Run npm install -g @anthropic-ai/claude-code
    # Verify installation
    # Check claude command available
}
```
- Effort: M
- Installs latest version from npm
- Creates symlink in ~/.npm-global/bin/
- **Acceptance Criteria:** `claude` command available in PATH

---

### Phase 8: Additional Tools (Medium Priority)

**Objective:** Install essential development tools

#### Tasks

**8.1: Tools Installation Function**
```powershell
function Install-AdditionalTools {
    # Update apt packages
    # Install git, build-essential, curl
    # Verify installations
}
```
- Effort: S
- Installs common development tools
- Non-blocking if some tools fail
- **Acceptance Criteria:** Core tools installed (git, gcc, curl)

---

### Phase 9: Validation Suite (High Priority)

**Objective:** Comprehensive validation to ensure successful setup

#### Tasks

**9.1: Validation Function**
```powershell
function Test-Installation {
    # Test WSL working
    # Test Node.js installed and accessible
    # Test npm installed and accessible
    # Test Claude Code CLI in PATH
    # Test .bashrc PATH configuration
    # Generate validation report
}
```
- Effort: M
- Tests each component individually
- Reports pass/fail for each check
- **Acceptance Criteria:** All components validated and working

---

### Phase 10: Installation Report (Medium Priority)

**Objective:** Provide clear next steps and configuration summary

#### Tasks

**10.1: Report Function**
```powershell
function New-InstallationReport {
    # Display success message
    # List installed components
    # Show next steps (wsl, claude, login)
    # Provide troubleshooting tips
    # Show configuration details
}
```
- Effort: M
- User-friendly formatted output
- Clear next steps for team members
- **Acceptance Criteria:** Comprehensive report with actionable guidance

---

## Risk Assessment and Mitigation

### High-Risk Areas

**Risk 1: WSL Feature Installation Requiring Reboot**
- **Impact:** High - Script cannot continue until reboot
- **Probability:** Medium - Only on first-time WSL install
- **Mitigation:** Detect need for reboot; prompt user; allow script re-run after reboot
- **Contingency:** Clear instructions to reboot and re-run script

**Risk 2: Deleting Existing WSL Data**
- **Impact:** Critical - User loses work in WSL
- **Probability:** High - Script designed to wipe existing WSL
- **Mitigation:** Backup function before cleanup; require 'DELETE' confirmation; provide `-SkipCleanup` option
- **Contingency:** Restore from backup in %USERPROFILE%\WSL-Backups

**Risk 3: Administrator Privileges Not Available**
- **Impact:** High - Cannot enable WSL features
- **Probability:** Medium - Users may forget to run as admin
- **Mitigation:** Check at prerequisites; fail early with clear message
- **Contingency:** Right-click and "Run as Administrator"

**Risk 4: Internet Connection Failure**
- **Impact:** High - Cannot download Node.js, npm packages
- **Probability:** Low - Most environments have internet
- **Mitigation:** Check connectivity early; clear error messages
- **Contingency:** Manual installation instructions

### Medium-Risk Areas

**Risk 5: Ubuntu Installation Interactive Prompt**
- **Impact:** Medium - User must create account manually
- **Probability:** Low - Expected behavior
- **Mitigation:** Clear messaging that user will be prompted
- **Contingency:** Instructions in installation report

**Risk 6: PATH Not Taking Effect**
- **Impact:** Medium - `claude` command not found
- **Probability:** Low - .bashrc sourcing usually works
- **Mitigation:** Source .bashrc in script; provide troubleshooting
- **Contingency:** Manual restart WSL or run `source ~/.bashrc`

---

## Success Metrics

### Installation Success

**Primary Metrics:**
- ✅ WSL2 enabled and working
- ✅ Ubuntu installed with user account
- ✅ Node.js and npm installed
- ✅ npm configured with global directory
- ✅ Claude Code CLI installed globally
- ✅ `claude` command available in PATH
- ✅ Validation suite reports 0 errors

**Secondary Metrics:**
- ✅ Script completes in < 15 minutes
- ✅ Clear progress indicators throughout
- ✅ Installation report generated
- ✅ No manual intervention (except user account creation and confirmation prompts)

---

## Timeline Estimates

### Development Phase

| Phase | Tasks | Estimated Time |
|-------|-------|----------------|
| 1. Script Foundation | Parameters, prerequisites, logging, error handling | 1 hour |
| 2. Backup & Cleanup | Backup and removal functions | 1 hour |
| 3. WSL Features | Feature installation, reboot handling | 1 hour |
| 4. Ubuntu Install | Installation function | 30 mins |
| 5. Node.js Install | nodesource setup, installation | 1 hour |
| 6. npm Configuration | Global directory, PATH setup | 30 mins |
| 7. Claude Code Install | npm global install, verification | 30 mins |
| 8. Additional Tools | git, build-essential, curl | 30 mins |
| 9. Validation Suite | Comprehensive testing | 1 hour |
| 10. Reporting | Installation report generation | 30 mins |
| Testing & Refinement | Multiple test runs, bug fixes | 2-3 hours |

**Total Development Time:** 8-12 hours

### Deployment Phase

**Per Developer:**
- Run script as Administrator: 10-15 minutes
- Create Ubuntu user account: 1 minute
- Login to Claude Code: 2 minutes
- Test installation: 2 minutes

**Total Per Developer:** ~15-20 minutes

---

## Dependencies Between Tasks

### Critical Path

```
Prerequisites Validation (1.2)
    ↓
Backup Existing WSL (2.1) [Optional]
    ↓
Cleanup Existing WSL (2.2) [Optional]
    ↓
Install WSL Features (3.1) [May require reboot]
    ↓
Install Ubuntu (4.1) [User creates account]
    ↓
Install Node.js (5.1)
    ↓
Configure npm (6.1)
    ↓
Configure PATH (6.2)
    ↓
Install Claude Code (7.1)
    ↓
Install Additional Tools (8.1) [Non-blocking]
    ↓
Validation Suite (9.1)
    ↓
Installation Report (10.1)
```

### Optional Operations

- Backup (2.1) - Only if `-SkipBackup` not specified
- Cleanup (2.2) - Only if `-SkipCleanup` not specified
- Additional Tools (8.1) - Failures don't block installation

---

## Next Steps After Installation

### Immediate Validation

1. **Open WSL** - `wsl` from PowerShell
2. **Verify Claude Code** - `claude --version`
3. **Login to Claude** - `claude` (follow OAuth flow)
4. **Test in project** - Navigate to project and run `claude`

### First Tasks

1. **Login to Claude Code** - Complete authentication
2. **Navigate to project** - `cd /mnt/c/your/project/path`
3. **Start using Claude Code** - `claude` to begin interactive session
4. **Familiarize with commands** - Try `/help` to see available commands

---

## Conclusion

This plan provides a comprehensive roadmap for automating the WSL and Claude Code setup. The PowerShell script will enable team members to get up and running with Claude Code in their WSL environment in 15-20 minutes, with minimal manual intervention.

**Key Success Factors:**
- Safe backup and cleanup of existing WSL
- Clear warnings about destructive operations
- Reboot handling for WSL feature installation
- Comprehensive validation suite
- Clear next steps in installation report

**Next Actions:**
1. ✅ Script development complete
2. ⏳ Test on clean Windows installation
3. ⏳ Test on machine with existing WSL
4. ⏳ Document and distribute to team

---

**Plan Status:** IMPLEMENTATION COMPLETE
**Script:** setup-claude-code-wsl.ps1
**Next Action:** Testing and validation
