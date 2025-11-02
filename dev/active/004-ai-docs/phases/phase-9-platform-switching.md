# Phase 9: Platform Switching & Testing

**Estimated Time**: 2-3 hours
**Status**: NOT STARTED
**Dependencies**: Phases 6, 8 complete (Phase 7 optional)

---

## Overview

**Goal**: Enable seamless platform portability and validate the entire system works end-to-end.

**Why This Phase Matters**: This phase validates the platform-agnostic architecture and ensures consultants can switch between GitHub and Azure DevOps environments without code changes.

---

## Tasks

### Task 9.1: Implement Platform Detection Script (Medium - 1.5 hours)

**File**: `.docgen/detect-platform.ps1`

**Purpose**: Detect current CI/CD platform (GitHub Actions, Azure DevOps, or Local) and load appropriate configuration.

**Acceptance Criteria**:
- [ ] Function: `Get-CIPlatform` (detects GitHub Actions, Azure DevOps, Local)
- [ ] Function: `Get-PlatformConfig` (loads from `.docgen/platform-config.json`)
- [ ] Auto-detection from git remote URL when running locally
- [ ] Returns appropriate config for detected platform
- [ ] Works on WSL2 and Windows

**Implementation**:
```powershell
#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Detect current CI/CD platform and load configuration
#>

function Get-CIPlatform {
    <#
    .SYNOPSIS
        Detect if running in GitHub Actions, Azure DevOps, or locally
    .OUTPUTS
        String: "GitHub", "AzureDevOps", or "Local"
    #>
    if ($env:GITHUB_ACTIONS -eq "true") {
        return "GitHub"
    }
    elseif ($env:TF_BUILD -eq "True") {
        return "AzureDevOps"
    }
    else {
        return "Local"
    }
}

function Get-PlatformConfig {
    <#
    .SYNOPSIS
        Load platform configuration from platform-config.json
    .PARAMETER Platform
        Platform name (GitHub, AzureDevOps, or Auto)
    #>
    param(
        [ValidateSet("GitHub", "AzureDevOps", "Auto")]
        [string]$Platform = "Auto"
    )

    $configPath = ".docgen/platform-config.json"
    if (-not (Test-Path $configPath)) {
        Write-Error "Platform config not found: $configPath"
        return $null
    }

    $config = Get-Content $configPath | ConvertFrom-Json

    # Auto-detect if requested
    if ($Platform -eq "Auto") {
        $detectedPlatform = Get-CIPlatform

        if ($detectedPlatform -eq "Local") {
            # Detect from git remote
            $remoteUrl = git config --get remote.origin.url
            if ($remoteUrl -match "github\.com") {
                $Platform = "GitHub"
            }
            elseif ($remoteUrl -match "dev\.azure\.com") {
                $Platform = "AzureDevOps"
            }
            else {
                Write-Warning "Cannot detect platform from git remote. Using default from config."
                $Platform = $config.defaultPlatform
            }
        }
        else {
            $Platform = $detectedPlatform
        }
    }

    Write-Host "Detected platform: $Platform"

    # Return platform-specific config
    $platformKey = $Platform.ToLower()
    return $config.platforms.$platformKey
}

# Export functions
Export-ModuleMember -Function Get-CIPlatform, Get-PlatformConfig

# If run directly, show current platform
if ($MyInvocation.InvocationName -ne ".") {
    $platform = Get-CIPlatform
    Write-Host "Current Platform: $platform"

    if ($platform -eq "Local") {
        $config = Get-PlatformConfig -Platform "Auto"
        Write-Host "Platform Config:"
        $config | ConvertTo-Json -Depth 5
    }
}
```

**Testing**:
```bash
# Test platform detection
pwsh .docgen/detect-platform.ps1

# Expected output (on local machine with GitHub remote):
# Current Platform: Local
# Detected platform: GitHub
# Platform Config: [JSON output]
```

---

### Task 9.2: Implement Platform Switching Script (Medium - 1.5 hours)

**File**: `.docgen/switch-platform.ps1`

**Purpose**: Switch between GitHub and Azure DevOps platforms by updating configuration and enabling/disabling workflows.

**Acceptance Criteria**:
- [ ] Parameter: `-Platform` (GitHub | AzureDevOps)
- [ ] Updates `docfx.json` git contribute URLs (GitHub ↔ Azure DevOps)
- [ ] Enables/disables workflows vs pipelines (rename directories or update config)
- [ ] Updates `.docgen/platform-config.json` (set default platform)
- [ ] Prints next steps for user
- [ ] Reversible (can switch back)
- [ ] Does NOT commit changes (user responsibility)

**Implementation**:
```powershell
#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Switch between GitHub and Azure DevOps platforms
.PARAMETER Platform
    Target platform (GitHub or AzureDevOps)
#>
param(
    [Parameter(Mandatory)]
    [ValidateSet("GitHub", "AzureDevOps")]
    [string]$Platform
)

$ErrorActionPreference = "Stop"

Write-Host "Switching to $Platform platform..."

# Load current config
$configPath = ".docgen/platform-config.json"
$config = Get-Content $configPath | ConvertFrom-Json

# Update default platform
$config.defaultPlatform = $Platform
$config | ConvertTo-Json -Depth 10 | Set-Content $configPath
Write-Host "✅ Updated platform-config.json"

# Update DocFX git contribute URLs
$docfxFiles = Get-ChildItem -Path "docs" -Filter "docfx.json" -Recurse

foreach ($file in $docfxFiles) {
    $docfxConfig = Get-Content $file.FullName | ConvertFrom-Json

    if ($Platform -eq "GitHub") {
        $docfxConfig.build.globalMetadata._gitContribute.repo = "https://github.com/NotMyself/net10-project-example"
        $docfxConfig.build.globalMetadata._gitContribute.branch = "main"
    }
    elseif ($Platform -eq "AzureDevOps") {
        $docfxConfig.build.globalMetadata._gitContribute.repo = "https://dev.azure.com/org/net10-project-example/_git/net10-project-example"
        $docfxConfig.build.globalMetadata._gitContribute.branch = "main"
    }

    $docfxConfig | ConvertTo-Json -Depth 10 | Set-Content $file.FullName
    Write-Host "✅ Updated $($file.FullName)"
}

# Enable/disable workflows and pipelines
if ($Platform -eq "GitHub") {
    # Ensure .github/workflows/ enabled
    if (Test-Path ".github/workflows.disabled") {
        Rename-Item ".github/workflows.disabled" ".github/workflows"
        Write-Host "✅ Enabled GitHub workflows"
    }

    # Disable Azure DevOps pipelines
    if (Test-Path ".azuredevops/pipelines") {
        Rename-Item ".azuredevops/pipelines" ".azuredevops/pipelines.disabled"
        Write-Host "✅ Disabled Azure DevOps pipelines"
    }
}
elseif ($Platform -eq "AzureDevOps") {
    # Ensure .azuredevops/pipelines/ enabled
    if (Test-Path ".azuredevops/pipelines.disabled") {
        Rename-Item ".azuredevops/pipelines.disabled" ".azuredevops/pipelines"
        Write-Host "✅ Enabled Azure DevOps pipelines"
    }

    # Disable GitHub workflows
    if (Test-Path ".github/workflows") {
        Rename-Item ".github/workflows" ".github/workflows.disabled"
        Write-Host "✅ Disabled GitHub workflows"
    }
}

Write-Host ""
Write-Host "Platform switch complete! 🎉"
Write-Host ""
Write-Host "Next Steps:"
Write-Host "1. Review changes: git status"
Write-Host "2. Test locally: make docs-build"
Write-Host "3. Commit changes: git add . && git commit -m 'Switch to $Platform platform'"
Write-Host "4. Push to remote: git push"

if ($Platform -eq "AzureDevOps") {
    Write-Host "5. Configure Azure DevOps pipelines in Azure DevOps UI"
    Write-Host "6. Set up branch policies"
}
else {
    Write-Host "5. Verify GitHub Actions in repository settings"
    Write-Host "6. Check GitHub Pages deployment"
}
```

**Testing**:
```bash
# Switch to Azure DevOps
pwsh .docgen/switch-platform.ps1 -Platform AzureDevOps

# Review changes
git status

# Switch back to GitHub
pwsh .docgen/switch-platform.ps1 -Platform GitHub

# Review changes
git status
```

---

### Task 9.3: Test Platform Switching (Small - 30 minutes)

**Action**: Verify platform switching works in both directions.

**Acceptance Criteria**:
- [ ] Run `.docgen/switch-platform.ps1 -Platform GitHub`
- [ ] Verify `.github/workflows/` enabled, `.azuredevops/pipelines/` disabled (or renamed)
- [ ] Verify docfx.json has GitHub URLs
- [ ] Run `.docgen/switch-platform.ps1 -Platform AzureDevOps`
- [ ] Verify `.azuredevops/pipelines/` enabled, `.github/workflows/` disabled
- [ ] Verify docfx.json has Azure DevOps URLs
- [ ] Switch back to GitHub
- [ ] Verify configuration correct after round-trip

**Test Procedure**:
```bash
# 1. Switch to GitHub (if not already)
pwsh .docgen/switch-platform.ps1 -Platform GitHub

# 2. Verify GitHub workflows exist
ls .github/workflows/

# 3. Verify DocFX config
grep "github.com" docs/docfx-developer/docfx.json

# 4. Switch to Azure DevOps
pwsh .docgen/switch-platform.ps1 -Platform AzureDevOps

# 5. Verify pipelines exist
ls .azuredevops/pipelines/

# 6. Verify DocFX config
grep "dev.azure.com" docs/docfx-developer/docfx.json

# 7. Switch back to GitHub
pwsh .docgen/switch-platform.ps1 -Platform GitHub

# 8. Verify everything back to original state
git status
```

---

### Task 9.4: Comprehensive End-to-End Test (Medium - 1.5 hours)

**Action**: Validate entire system works end-to-end.

**Acceptance Criteria**:
- [ ] All three doc types build locally: `make docs-build`
- [ ] GitHub workflows all succeed (if on GitHub)
- [ ] Azure DevOps pipelines all succeed (if implemented and on Azure DevOps)
- [ ] Platform switching works both directions
- [ ] AI assistance works for all doc types (MCP servers connected)
- [ ] Diagrams generate correctly: `make diagrams`
- [ ] PR validation catches issues (test with intentional error)
- [ ] Documentation quality is professional (manual review)

**Test Checklist**:
```markdown
## Local Build Test
- [ ] `make docs-build` succeeds
- [ ] `make docs-serve` works (localhost:8080)
- [ ] `make docs-user-serve` works (localhost:8081)
- [ ] `make diagrams` generates diagrams
- [ ] `make validate` passes

## GitHub Integration Test (if on GitHub)
- [ ] Push change to docs/docfx-developer/
- [ ] Developer docs deploy workflow runs
- [ ] Documentation visible at GitHub Pages
- [ ] Wiki sync workflow runs (if wiki changed)
- [ ] PR validation blocks bad PR (test)

## Azure DevOps Integration Test (if implemented)
- [ ] Switch to Azure DevOps platform
- [ ] Push change to main
- [ ] Pipelines trigger correctly
- [ ] Documentation deploys to Azure SWA
- [ ] Wiki publishes to Azure DevOps Wiki

## AI Integration Test
- [ ] MCP servers connected
- [ ] Generate API docs with AI
- [ ] Generate architecture docs with agent
- [ ] AI output uses correct terminology

## Platform Switching Test
- [ ] Switch GitHub → Azure DevOps works
- [ ] Switch Azure DevOps → GitHub works
- [ ] No manual code changes required
```

---

### Task 9.5: Create Migration Guide (Small - 1 hour)

**File**: `docs/architecture/platform-migration-guide.md` (new)

**Purpose**: Document step-by-step process for migrating between platforms.

**Acceptance Criteria**:
- [ ] Documents how to migrate GitHub → Azure DevOps
- [ ] Documents how to migrate Azure DevOps → GitHub
- [ ] Step-by-step commands for each direction
- [ ] Time estimates for migration
- [ ] Troubleshooting common issues
- [ ] Checklist format for validation

**Template**:
```markdown
# Platform Migration Guide

## Overview

This guide explains how to migrate the documentation system between GitHub and Azure DevOps.

## Migration Time Estimates

- **GitHub → Azure DevOps**: 2-3 hours (first time), 30 minutes (subsequent)
- **Azure DevOps → GitHub**: 1-2 hours (first time), 15 minutes (subsequent)

## Prerequisites

- [ ] Both platforms have repository/project created
- [ ] Azure resources created (if migrating to Azure DevOps)
- [ ] Git remote configured for both platforms
- [ ] Required permissions on both platforms

---

## Migration: GitHub → Azure DevOps

### Step 1: Prepare Azure DevOps
[Detailed steps...]

### Step 2: Switch Platform Configuration
```bash
pwsh .docgen/switch-platform.ps1 -Platform AzureDevOps
```

### Step 3: Create Azure Resources
[Commands for Azure Static Web Apps, etc.]

### Step 4: Configure Pipelines
[Steps to set up pipelines in Azure DevOps]

### Step 5: Update Git Remote
```bash
git remote set-url origin https://dev.azure.com/org/project/_git/repo
```

### Step 6: Push and Test
[Validation steps...]

---

## Migration: Azure DevOps → GitHub

### Step 1: Prepare GitHub Repository
[Detailed steps...]

### Step 2: Switch Platform Configuration
```bash
pwsh .docgen/switch-platform.ps1 -Platform GitHub
```

### Step 3: Configure GitHub Pages
[Steps...]

### Step 4: Update Git Remote
```bash
git remote set-url origin https://github.com/org/repo.git
```

### Step 5: Push and Test
[Validation steps...]

---

## Troubleshooting

### Issue: Workflows/Pipelines not triggering
**Solution**: [Steps...]

### Issue: Documentation not deploying
**Solution**: [Steps...]

[More issues...]
```

---

## Phase Completion Criteria

Phase 9 is complete when:

- [ ] Platform detection script works correctly
- [ ] Platform switching script works both directions (GitHub ↔ Azure DevOps)
- [ ] Full end-to-end test passes (all doc types, workflows, AI)
- [ ] Migration guide created and validated
- [ ] No code changes required when switching platforms (only config)

---

## Success Indicators

You'll know this phase is successful when:

- Can switch platforms in < 5 minutes (after initial setup)
- Documentation builds identically on both platforms
- Team confident in platform portability
- Client can choose platform without technical constraints
- End-to-end system works flawlessly

---

**Phase Status**: NOT STARTED
**Next Task**: Task 9.1 - Implement Platform Detection Script
**Estimated Completion**: After 2-3 hours of focused work
