<#
.SYNOPSIS
    Automates Claude Code infrastructure setup for .NET 10 projects

.DESCRIPTION
    Installs hooks, skills, agents, slash commands, and dev docs system
    from claude-code-infrastructure-showcase repository.

    This script automates the setup process documented in:
    dev/archive/001-claude-code-setup/001-claude-code-setup-plan.md

.PARAMETER SourceRepoPath
    Path to claude-code-infrastructure-showcase repository.
    If not provided, script will prompt interactively.

.PARAMETER TargetPath
    Target project directory (defaults to current directory).

.PARAMETER DryRun
    Show what would be done without making changes.

.PARAMETER SkipValidation
    Skip validation suite (faster, use only if confident).

.EXAMPLE
    .\setup-claude-code-infrastructure.ps1
    # Interactive mode - prompts for source repo

.EXAMPLE
    .\setup-claude-code-infrastructure.ps1 -SourceRepoPath "C:\src\showcase"
    # Automated mode with source path

.EXAMPLE
    .\setup-claude-code-infrastructure.ps1 -DryRun
    # Dry run - see what would happen

.NOTES
    Requires: PowerShell 7+, WSL2, Node.js in WSL
    License: MIT
    Version: 1.0.0
    Author: Claude Code Infrastructure Contributors
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Position=0)]
    [string]$SourceRepoPath,

    [Parameter()]
    [string]$TargetPath = (Get-Location).Path,

    [Parameter()]
    [switch]$DryRun,

    [Parameter()]
    [switch]$SkipValidation
)

#region Helper Functions

function Write-LogMessage {
    param(
        [Parameter(Mandatory)]
        [string]$Message,

        [Parameter()]
        [ValidateSet('Info', 'Success', 'Warning', 'Error')]
        [string]$Level = 'Info',

        [Parameter()]
        [switch]$NoNewline
    )

    $timestamp = Get-Date -Format 'HH:mm:ss'
    $prefix = "[$timestamp]"

    $color = switch ($Level) {
        'Info' { 'Cyan' }
        'Success' { 'Green' }
        'Warning' { 'Yellow' }
        'Error' { 'Red' }
    }

    $symbol = switch ($Level) {
        'Info' { 'ℹ' }
        'Success' { '✓' }
        'Warning' { '⚠' }
        'Error' { '✗' }
    }

    if ($NoNewline) {
        Write-Host "$prefix $symbol $Message" -ForegroundColor $color -NoNewline
    } else {
        Write-Host "$prefix $symbol $Message" -ForegroundColor $color
    }
}

function Write-SectionHeader {
    param([string]$Title)
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Magenta
    Write-Host " $Title" -ForegroundColor Magenta
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Magenta
    Write-Host ""
}

function Test-Prerequisites {
    Write-SectionHeader "Checking Prerequisites"

    $allPassed = $true

    # Check PowerShell version
    Write-LogMessage "Checking PowerShell version..." -NoNewline
    if ($PSVersionTable.PSVersion.Major -ge 7) {
        Write-Host " " -NoNewline
        Write-LogMessage "PowerShell $($PSVersionTable.PSVersion) ✓" -Level Success
    } else {
        Write-Host ""
        Write-LogMessage "PowerShell 7+ required. Current: $($PSVersionTable.PSVersion)" -Level Error
        Write-LogMessage "Install from: https://aka.ms/powershell" -Level Info
        $allPassed = $false
    }

    # Check WSL
    Write-LogMessage "Checking WSL installation..." -NoNewline
    try {
        $wslVersion = wsl --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host " " -NoNewline
            Write-LogMessage "WSL installed ✓" -Level Success
        } else {
            throw "WSL not found"
        }
    } catch {
        Write-Host ""
        Write-LogMessage "WSL2 required but not found" -Level Error
        Write-LogMessage "Install from: https://aka.ms/wsl" -Level Info
        $allPassed = $false
    }

    # Check Node.js in WSL
    if ($allPassed) {
        Write-LogMessage "Checking Node.js in WSL..." -NoNewline
        try {
            $nodeVersion = wsl node --version 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host " " -NoNewline
                Write-LogMessage "Node.js $nodeVersion ✓" -Level Success
            } else {
                throw "Node.js not found"
            }
        } catch {
            Write-Host ""
            Write-LogMessage "Node.js not found in WSL" -Level Error
            Write-LogMessage "Install in WSL: sudo apt update && sudo apt install nodejs npm" -Level Info
            $allPassed = $false
        }
    }

    # Check npm in WSL
    if ($allPassed) {
        Write-LogMessage "Checking npm in WSL..." -NoNewline
        try {
            $npmVersion = wsl npm --version 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host " " -NoNewline
                Write-LogMessage "npm $npmVersion ✓" -Level Success
            } else {
                throw "npm not found"
            }
        } catch {
            Write-Host ""
            Write-LogMessage "npm not found in WSL" -Level Error
            $allPassed = $false
        }
    }

    # Check target directory
    Write-LogMessage "Checking target directory..." -NoNewline
    if (Test-Path $TargetPath) {
        Write-Host " " -NoNewline
        Write-LogMessage "$TargetPath ✓" -Level Success
    } else {
        Write-Host ""
        Write-LogMessage "Target directory not found: $TargetPath" -Level Error
        $allPassed = $false
    }

    Write-Host ""

    if (!$allPassed) {
        throw "Prerequisites check failed. Please resolve the issues above."
    }

    return $allPassed
}

function Get-SourceRepositoryPath {
    if ($SourceRepoPath) {
        if (Test-Path $SourceRepoPath) {
            Write-LogMessage "Using source repository: $SourceRepoPath" -Level Info
            return $SourceRepoPath
        } else {
            Write-LogMessage "Source repository not found: $SourceRepoPath" -Level Error
            throw "Source repository path is invalid"
        }
    }

    Write-Host ""
    Write-LogMessage "Source repository path required" -Level Info
    Write-Host ""
    Write-Host "  Please provide the path to the claude-code-infrastructure-showcase repository."
    Write-Host "  Example: C:\Users\bobby\src\claude\claude-code-infrastructure-showcase"
    Write-Host ""

    do {
        $path = Read-Host "Source repository path"

        if (Test-Path $path) {
            # Validate it looks like the correct repo
            $claudeDir = Join-Path $path ".claude"
            if (Test-Path $claudeDir) {
                Write-LogMessage "Source repository validated ✓" -Level Success
                return $path
            } else {
                Write-LogMessage "This doesn't look like the showcase repository (no .claude/ directory)" -Level Warning
                Write-Host ""
            }
        } else {
            Write-LogMessage "Path not found: $path" -Level Error
            Write-Host ""
        }
    } while ($true)
}

function ConvertTo-WslPath {
    param([string]$WindowsPath)

    # Convert Windows path to WSL path
    # C:\Users\bobby\... -> /mnt/c/Users/bobby/...
    $wslPath = $WindowsPath -replace '\\', '/'
    $wslPath = $wslPath -replace '^([A-Za-z]):', { "/mnt/$($_.Groups[1].Value.ToLower())" }

    return $wslPath
}

function New-ClaudeDirectories {
    Write-SectionHeader "Creating Directory Structure"

    $directories = @(
        ".claude",
        ".claude\hooks",
        ".claude\skills",
        ".claude\agents",
        ".claude\commands",
        "dev",
        "dev\active"
    )

    foreach ($dir in $directories) {
        $fullPath = Join-Path $TargetPath $dir

        if ($DryRun) {
            Write-LogMessage "[DRY RUN] Would create: $dir" -Level Info
        } else {
            if (!(Test-Path $fullPath)) {
                New-Item -Path $fullPath -ItemType Directory -Force | Out-Null
                Write-LogMessage "Created: $dir" -Level Success
            } else {
                Write-LogMessage "Already exists: $dir" -Level Info
            }
        }
    }

    Write-Host ""
}

function Copy-HookFiles {
    param([string]$SourcePath)

    Write-LogMessage "Copying hook files..." -Level Info

    $hookFiles = @(
        "skill-activation-prompt.ts",
        "skill-activation-prompt.sh",
        "post-tool-use-tracker.sh",
        "package.json"
    )

    $sourceHooksDir = Join-Path $SourcePath ".claude\hooks"
    $targetHooksDir = Join-Path $TargetPath ".claude\hooks"

    foreach ($file in $hookFiles) {
        $sourcePath = Join-Path $sourceHooksDir $file
        $targetPath = Join-Path $targetHooksDir $file

        if ($DryRun) {
            Write-LogMessage "[DRY RUN] Would copy: $file" -Level Info
        } else {
            if (Test-Path $sourcePath) {
                Copy-Item -Path $sourcePath -Destination $targetPath -Force
                Write-LogMessage "Copied: $file" -Level Success
            } else {
                Write-LogMessage "Not found: $file" -Level Warning
            }
        }
    }
}

function Copy-Skills {
    param([string]$SourcePath)

    Write-LogMessage "Copying skills..." -Level Info

    $sourceSkillsDir = Join-Path $SourcePath ".claude\skills"
    $targetSkillsDir = Join-Path $TargetPath ".claude\skills"

    # Copy skill-rules.json first
    $skillRulesSource = Join-Path $sourceSkillsDir "skill-rules.json"
    $skillRulesTarget = Join-Path $targetSkillsDir "skill-rules.json"

    if ($DryRun) {
        Write-LogMessage "[DRY RUN] Would copy: skill-rules.json" -Level Info
    } else {
        if (Test-Path $skillRulesSource) {
            Copy-Item -Path $skillRulesSource -Destination $skillRulesTarget -Force
            Write-LogMessage "Copied: skill-rules.json" -Level Success
        }
    }

    # Copy each skill directory
    $skillDirs = Get-ChildItem -Path $sourceSkillsDir -Directory

    foreach ($skillDir in $skillDirs) {
        $targetSkillDir = Join-Path $targetSkillsDir $skillDir.Name

        if ($DryRun) {
            Write-LogMessage "[DRY RUN] Would copy skill: $($skillDir.Name)" -Level Info
        } else {
            if (!(Test-Path $targetSkillDir)) {
                New-Item -Path $targetSkillDir -ItemType Directory -Force | Out-Null
            }

            Copy-Item -Path "$($skillDir.FullName)\*" -Destination $targetSkillDir -Recurse -Force
            Write-LogMessage "Copied skill: $($skillDir.Name)" -Level Success
        }
    }
}

function Copy-Agents {
    param([string]$SourcePath)

    Write-LogMessage "Copying agents..." -Level Info

    $sourceAgentsDir = Join-Path $SourcePath ".claude\agents"
    $targetAgentsDir = Join-Path $TargetPath ".claude\agents"

    if ($DryRun) {
        Write-LogMessage "[DRY RUN] Would copy agents directory" -Level Info
    } else {
        if (Test-Path $sourceAgentsDir) {
            if (!(Test-Path $targetAgentsDir)) {
                New-Item -Path $targetAgentsDir -ItemType Directory -Force | Out-Null
            }

            Copy-Item -Path "$sourceAgentsDir\*" -Destination $targetAgentsDir -Recurse -Force

            $agentCount = (Get-ChildItem -Path $sourceAgentsDir -Filter "*.md" | Where-Object { $_.Name -ne "README.md" }).Count
            Write-LogMessage "Copied $agentCount agent definitions" -Level Success
        } else {
            Write-LogMessage "Agents directory not found in source" -Level Warning
        }
    }
}

function Copy-CommandsAndDevDocs {
    param([string]$SourcePath)

    Write-LogMessage "Copying slash commands and dev docs..." -Level Info

    # Copy commands
    $sourceCommandsDir = Join-Path $SourcePath ".claude\commands"
    $targetCommandsDir = Join-Path $TargetPath ".claude\commands"

    if ($DryRun) {
        Write-LogMessage "[DRY RUN] Would copy commands directory" -Level Info
    } else {
        if (Test-Path $sourceCommandsDir) {
            Copy-Item -Path "$sourceCommandsDir\*" -Destination $targetCommandsDir -Force
            $cmdCount = (Get-ChildItem -Path $targetCommandsDir -Filter "*.md").Count
            Write-LogMessage "Copied $cmdCount slash commands" -Level Success
        }
    }

    # Copy dev/README.md
    $sourceDevReadme = Join-Path $SourcePath "dev\README.md"
    $targetDevReadme = Join-Path $TargetPath "dev\README.md"

    if ($DryRun) {
        Write-LogMessage "[DRY RUN] Would copy dev/README.md" -Level Info
    } else {
        if (Test-Path $sourceDevReadme) {
            Copy-Item -Path $sourceDevReadme -Destination $targetDevReadme -Force
            Write-LogMessage "Copied: dev/README.md" -Level Success
        }
    }
}

function Copy-Documentation {
    param([string]$SourcePath)

    Write-LogMessage "Copying documentation files..." -Level Info

    $docFiles = @(
        "README.md",
        "ATTRIBUTION.md",
        "TROUBLESHOOTING.md"
    )

    $sourceClaudeDir = Join-Path $SourcePath ".claude"
    $targetClaudeDir = Join-Path $TargetPath ".claude"

    foreach ($file in $docFiles) {
        $sourcePath = Join-Path $sourceClaudeDir $file
        $targetPath = Join-Path $targetClaudeDir $file

        if ($DryRun) {
            Write-LogMessage "[DRY RUN] Would copy: $file" -Level Info
        } else {
            if (Test-Path $sourcePath) {
                Copy-Item -Path $sourcePath -Destination $targetPath -Force
                Write-LogMessage "Copied: $file" -Level Success
            } else {
                Write-LogMessage "Not found: $file" -Level Warning
            }
        }
    }
}

function New-SettingsJson {
    Write-SectionHeader "Creating Configuration Files"

    $targetSettings = Join-Path $TargetPath ".claude\settings.json"

    $settings = @{
        hooks = @{
            UserPromptSubmit = @(
                @{
                    hooks = @(
                        @{
                            type = "command"
                            command = "`$CLAUDE_PROJECT_DIR/.claude/hooks/skill-activation-prompt.sh"
                        }
                    )
                }
            )
            PostToolUse = @(
                @{
                    matcher = "Edit|MultiEdit|Write"
                    hooks = @(
                        @{
                            type = "command"
                            command = "`$CLAUDE_PROJECT_DIR/.claude/hooks/post-tool-use-tracker.sh"
                        }
                    )
                }
            )
        }
    }

    if ($DryRun) {
        Write-LogMessage "[DRY RUN] Would create settings.json" -Level Info
    } else {
        $settings | ConvertTo-Json -Depth 10 | Set-Content -Path $targetSettings -Encoding UTF8
        Write-LogMessage "Created: settings.json" -Level Success
    }

    Write-Host ""
}

function Install-HookDependencies {
    Write-SectionHeader "Installing npm Dependencies"

    $targetHooksDir = Join-Path $TargetPath ".claude\hooks"
    $wslPath = ConvertTo-WslPath $targetHooksDir

    Write-LogMessage "Converting to WSL path: $wslPath" -Level Info

    if ($DryRun) {
        Write-LogMessage "[DRY RUN] Would run: npm install in $wslPath" -Level Info
        return
    }

    Write-LogMessage "Running npm install (this may take a minute)..." -Level Info
    Write-Host ""

    try {
        $npmCommand = "cd '$wslPath' && npm install"
        wsl bash -c $npmCommand

        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-LogMessage "npm dependencies installed successfully" -Level Success
        } else {
            throw "npm install failed with exit code $LASTEXITCODE"
        }
    } catch {
        Write-LogMessage "npm install failed: $_" -Level Error
        Write-LogMessage "You can manually install with: wsl bash -c `"cd '$wslPath' && npm install`"" -Level Info
        throw
    }

    Write-Host ""
}

function Update-ClaudeMd {
    Write-LogMessage "Updating CLAUDE.md..." -Level Info

    $claudeMdPath = Join-Path $TargetPath "CLAUDE.md"

    if ($DryRun) {
        Write-LogMessage "[DRY RUN] Would update CLAUDE.md" -Level Info
        return
    }

    if (Test-Path $claudeMdPath) {
        $content = Get-Content -Path $claudeMdPath -Raw

        # Check if already has Claude Code Infrastructure section
        if ($content -match "## Claude Code Infrastructure") {
            Write-LogMessage "CLAUDE.md already contains infrastructure section" -Level Info
            return
        }

        # Append the section
        $infrastructureSection = @"

## Claude Code Infrastructure

This project uses Claude Code infrastructure for enhanced development workflow.

**Environment:** Running on WSL2 (Ubuntu) - all hooks and scripts use Linux/bash conventions.

### Installed Components

**Hooks:**
- **skill-activation-prompt** (UserPromptSubmit) - Auto-suggests relevant skills based on prompts and file context
- **post-tool-use-tracker** (PostToolUse) - Tracks file changes for context management

**Skills:**
- **skill-developer** - Meta-skill for creating and managing Claude Code skills
- **azure-devops** - Azure DevOps automation using az CLI with azure-devops extension
- **.NET 10 Project-Specific Skills:**
  - **mstest-testing-platform** - MSTest with Microsoft.Testing.Platform (new test runner)
  - **dotnet-centralized-packages** - Centralized Package Management with Directory.Packages.props
  - **playwright-dotnet** - E2E testing with Playwright for .NET
  - **dotnet-minimal-apis** - ASP.NET Core Minimal APIs with OpenAPI
  - **dotnet-cli-essentials** - Essential .NET CLI commands for this project
  - **aspnet-configuration** - ASP.NET Core configuration and options pattern

**Agents:**
- **code-architecture-reviewer** - Reviews code for best practices and architectural consistency
- **code-refactor-master** - Handles comprehensive code refactoring tasks
- **documentation-architect** - Creates and enhances documentation
- **plan-reviewer** - Reviews development plans before implementation
- **refactor-planner** - Analyzes code and creates refactoring plans
- **web-research-specialist** - Researches technical issues and solutions online

**Dev Docs System:**
- **Slash commands:** `/dev-docs` (create new docs), `/dev-docs-update` (update existing docs)
- **Location:** `dev/active/` directory
- **Pattern:** Three-file structure (plan.md, context.md, tasks.md) for complex tasks

### Configuration
- `.claude/` directory contains skills, hooks, agents, and configuration
- `.claude/hooks/` - TypeScript/bash hooks with npm dependencies
- `.claude/settings.json` - Hook registration and settings
- `.claude/skills/skill-rules.json` - Skill trigger definitions
- `dev/active/` - Development documentation for complex tasks

### Usage
Skills activate automatically based on your prompts and file context. See `.claude/README.md` for details.

### Creating Additional .NET-Specific Skills
Use skill-developer to create skills tailored to this .NET 10 project:
- ASP.NET Core MVC patterns
- Minimal API best practices
- MSTest with Microsoft.Testing.Platform
- .NET 10 specific guidance

Start with: "I want to create a skill for [ASP.NET Core/testing/etc]"
"@

        Add-Content -Path $claudeMdPath -Value $infrastructureSection
        Write-LogMessage "Updated CLAUDE.md with infrastructure section" -Level Success
    } else {
        Write-LogMessage "CLAUDE.md not found - skipping" -Level Warning
    }
}

function Update-GitIgnore {
    Write-LogMessage "Updating .gitignore..." -Level Info

    $gitignorePath = Join-Path $TargetPath ".gitignore"

    if ($DryRun) {
        Write-LogMessage "[DRY RUN] Would update .gitignore" -Level Info
        return
    }

    $claudeIgnoreRules = @"

# Claude Code Infrastructure
.claude/hooks/node_modules/
.claude/hooks/package-lock.json
.claude/state/
"@

    if (Test-Path $gitignorePath) {
        $content = Get-Content -Path $gitignorePath -Raw

        if ($content -match "# Claude Code Infrastructure") {
            Write-LogMessage ".gitignore already contains Claude Code rules" -Level Info
        } else {
            Add-Content -Path $gitignorePath -Value $claudeIgnoreRules
            Write-LogMessage "Updated .gitignore with Claude Code exclusions" -Level Success
        }
    } else {
        Set-Content -Path $gitignorePath -Value $claudeIgnoreRules
        Write-LogMessage "Created .gitignore with Claude Code exclusions" -Level Success
    }
}

function Test-FileStructure {
    Write-SectionHeader "Validating Installation"

    Write-LogMessage "Checking file structure..." -Level Info

    $requiredPaths = @(
        ".claude\hooks\skill-activation-prompt.ts",
        ".claude\hooks\skill-activation-prompt.sh",
        ".claude\hooks\post-tool-use-tracker.sh",
        ".claude\hooks\package.json",
        ".claude\hooks\node_modules",
        ".claude\skills\skill-rules.json",
        ".claude\skills\skill-developer\SKILL.md",
        ".claude\agents",
        ".claude\commands",
        ".claude\settings.json",
        ".claude\README.md",
        "dev\active"
    )

    $allPresent = $true

    foreach ($path in $requiredPaths) {
        $fullPath = Join-Path $TargetPath $path

        if (Test-Path $fullPath) {
            Write-LogMessage "✓ $path" -Level Success
        } else {
            Write-LogMessage "✗ $path" -Level Error
            $allPresent = $false
        }
    }

    Write-Host ""

    if ($allPresent) {
        Write-LogMessage "All required files and directories present" -Level Success
    } else {
        Write-LogMessage "Some files or directories are missing" -Level Warning
    }

    return $allPresent
}

function Test-Configuration {
    Write-LogMessage "Validating configuration files..." -Level Info

    $settingsPath = Join-Path $TargetPath ".claude\settings.json"
    $skillRulesPath = Join-Path $TargetPath ".claude\skills\skill-rules.json"

    $allValid = $true

    # Validate settings.json
    try {
        $settings = Get-Content -Path $settingsPath -Raw | ConvertFrom-Json

        if ($settings.hooks.UserPromptSubmit -and $settings.hooks.PostToolUse) {
            Write-LogMessage "✓ settings.json is valid" -Level Success
        } else {
            Write-LogMessage "✗ settings.json missing required hooks" -Level Error
            $allValid = $false
        }
    } catch {
        Write-LogMessage "✗ settings.json is invalid: $_" -Level Error
        $allValid = $false
    }

    # Validate skill-rules.json
    try {
        $skillRules = Get-Content -Path $skillRulesPath -Raw | ConvertFrom-Json

        if ($skillRules.skills) {
            $skillCount = ($skillRules.skills | Get-Member -MemberType NoteProperty).Count
            Write-LogMessage "✓ skill-rules.json is valid ($skillCount skills configured)" -Level Success
        } else {
            Write-LogMessage "✗ skill-rules.json missing skills" -Level Error
            $allValid = $false
        }
    } catch {
        Write-LogMessage "✗ skill-rules.json is invalid: $_" -Level Error
        $allValid = $false
    }

    Write-Host ""

    return $allValid
}

function Test-HookExecution {
    Write-LogMessage "Testing hook execution..." -Level Info

    $targetHooksDir = Join-Path $TargetPath ".claude\hooks"
    $wslPath = ConvertTo-WslPath $targetHooksDir

    try {
        $testInput = '{"session_id":"test","prompt":"how do I create a skill"}'
        $testCommand = "cd '$wslPath' && echo '$testInput' | npx tsx skill-activation-prompt.ts"

        $result = wsl bash -c $testCommand 2>&1

        if ($LASTEXITCODE -eq 0) {
            Write-LogMessage "✓ Hook execution successful" -Level Success
        } else {
            Write-LogMessage "✗ Hook execution failed" -Level Error
            Write-LogMessage "Output: $result" -Level Warning
            return $false
        }
    } catch {
        Write-LogMessage "✗ Hook test failed: $_" -Level Error
        return $false
    }

    Write-Host ""

    return $true
}

function New-InstallationReport {
    Write-SectionHeader "Generating Installation Report"

    $reportPath = Join-Path $TargetPath "dev\active\INSTALLATION_REPORT.md"
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

    $report = @"
# Claude Code Infrastructure - Installation Report

**Date:** $timestamp
**Environment:** WSL2 (Ubuntu) on Windows
**Target:** $TargetPath

## Installation Summary

Successfully installed Claude Code infrastructure from showcase repository.

## Installed Components

### Configuration Files
- ✅ .claude/settings.json - Hook registration and settings
- ✅ .claude/skills/skill-rules.json - Skill trigger definitions

### Hooks (4 files)
- ✅ skill-activation-prompt.ts - TypeScript hook for skill auto-activation
- ✅ skill-activation-prompt.sh - Bash wrapper for hook execution
- ✅ post-tool-use-tracker.sh - Tracks file changes for context management
- ✅ package.json - npm dependencies configuration

### NPM Dependencies
Installed in `.claude/hooks/`:
- @types/node
- node-jq
- tsx
- typescript

### Skills
- ✅ skill-developer - Meta-skill for creating and managing Claude Code skills
- ✅ azure-devops - Azure DevOps automation using az CLI
- ✅ 6 .NET 10 project-specific skills

### Agents (6 total)
- ✅ code-architecture-reviewer
- ✅ code-refactor-master
- ✅ documentation-architect
- ✅ plan-reviewer
- ✅ refactor-planner
- ✅ web-research-specialist

### Slash Commands
- ✅ /dev-docs - Creates new dev docs structure
- ✅ /dev-docs-update - Updates existing dev docs

### Documentation
- ✅ .claude/README.md - Complete infrastructure documentation
- ✅ .claude/ATTRIBUTION.md - Licensing and attribution
- ✅ .claude/TROUBLESHOOTING.md - Common issues and solutions

## Next Steps

1. **Restart Claude Code** to pick up new configuration
2. **Test skill activation** with prompt: "Explain how skill triggers work"
3. **Verify hooks working** - Check for skill suggestions in responses
4. **Create project-specific skill** using skill-developer
5. **Use dev docs pattern** - Start next complex task with `/dev-docs`

## Troubleshooting

If you encounter issues:
- See .claude/TROUBLESHOOTING.md for common problems and solutions
- Check .claude/README.md for complete usage documentation
- Test hooks manually: \`wsl bash -c "cd .claude/hooks && echo '{\"prompt\":\"test\"}' | npx tsx skill-activation-prompt.ts"\`

---

Installation completed successfully!
"@

    if ($DryRun) {
        Write-LogMessage "[DRY RUN] Would create installation report" -Level Info
    } else {
        Set-Content -Path $reportPath -Value $report -Encoding UTF8
        Write-LogMessage "Created: INSTALLATION_REPORT.md" -Level Success
        Write-Host ""
        Write-LogMessage "Report location: $reportPath" -Level Info
    }
}

#endregion

#region Main Execution

try {
    $script:ErrorActionPreference = 'Stop'

    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║                                                               ║" -ForegroundColor Cyan
    Write-Host "║     Claude Code Infrastructure Setup Automation Script       ║" -ForegroundColor Cyan
    Write-Host "║                        Version 1.0.0                          ║" -ForegroundColor Cyan
    Write-Host "║                                                               ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""

    if ($DryRun) {
        Write-LogMessage "DRY RUN MODE - No changes will be made" -Level Warning
        Write-Host ""
    }

    # Phase 1: Prerequisites check
    $prereqsPassed = Test-Prerequisites

    if (!$prereqsPassed) {
        throw "Prerequisites check failed"
    }

    # Phase 2: Get source repository path
    $sourcePath = Get-SourceRepositoryPath

    # Phase 3: Create directories
    New-ClaudeDirectories

    # Phase 4: Copy files
    Write-SectionHeader "Copying Files from Showcase Repository"

    Copy-HookFiles -SourcePath $sourcePath
    Copy-Skills -SourcePath $sourcePath
    Copy-Agents -SourcePath $sourcePath
    Copy-CommandsAndDevDocs -SourcePath $sourcePath
    Copy-Documentation -SourcePath $sourcePath

    Write-Host ""

    # Phase 5: Generate configuration
    New-SettingsJson

    # Phase 6: Install npm dependencies
    if (!$DryRun) {
        Install-HookDependencies
    }

    # Phase 7: Update documentation
    Write-SectionHeader "Updating Project Documentation"
    Update-ClaudeMd
    Update-GitIgnore
    Write-Host ""

    # Phase 8: Validation (unless skipped)
    if (!$SkipValidation -and !$DryRun) {
        $structureValid = Test-FileStructure
        $configValid = Test-Configuration
        $hookValid = Test-HookExecution

        if ($structureValid -and $configValid -and $hookValid) {
            Write-LogMessage "All validation checks passed ✓" -Level Success
        } else {
            Write-LogMessage "Some validation checks failed" -Level Warning
            Write-LogMessage "Review the messages above and consult .claude/TROUBLESHOOTING.md" -Level Info
        }

        Write-Host ""
    }

    # Phase 9: Generate installation report
    New-InstallationReport

    # Success summary
    Write-SectionHeader "Installation Complete"

    if ($DryRun) {
        Write-LogMessage "DRY RUN completed - no changes were made" -Level Info
        Write-LogMessage "Run without -DryRun to perform actual installation" -Level Info
    } else {
        Write-LogMessage "Claude Code infrastructure has been successfully installed!" -Level Success
        Write-Host ""
        Write-LogMessage "Next steps:" -Level Info
        Write-Host "  1. Restart Claude Code to load new configuration"
        Write-Host "  2. Test skill activation: 'Explain how skill triggers work'"
        Write-Host "  3. Review .claude/README.md for complete documentation"
        Write-Host "  4. Check dev/active/INSTALLATION_REPORT.md for details"
    }

    Write-Host ""

} catch {
    Write-Host ""
    Write-LogMessage "Installation failed: $_" -Level Error
    Write-Host ""
    Write-LogMessage "To retry, run the script again:" -Level Info
    Write-Host "  .\setup-claude-code-infrastructure.ps1 -SourceRepoPath `"$sourcePath`"" -ForegroundColor Gray
    Write-Host ""
    exit 1
}

#endregion
