<#
.SYNOPSIS
    Switches the documentation platform between GitHub and Azure DevOps.

.DESCRIPTION
    This script reconfigures the documentation system for a different platform.
    It updates DocFX configuration files, enables/disables platform-specific workflows,
    and updates the default platform in platform-config.json.

.PARAMETER Platform
    The target platform: GitHub or AzureDevOps.

.PARAMETER Force
    Skip confirmation prompts.

.EXAMPLE
    PS> .\switch-platform.ps1 -Platform GitHub
    Switches the documentation system to GitHub platform.

.EXAMPLE
    PS> .\switch-platform.ps1 -Platform AzureDevOps -Force
    Switches to Azure DevOps without confirmation prompts.

.NOTES
    This script modifies configuration files and workflow directories.
    Commit changes to version control after switching platforms.
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("GitHub", "AzureDevOps")]
    [string]$Platform,

    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Determine repository root
$repoRoot = Split-Path -Parent $PSScriptRoot
$configPath = Join-Path $PSScriptRoot "platform-config.json"

# Load current configuration
if (-not (Test-Path $configPath)) {
    throw "Platform configuration not found at: $configPath"
}

$config = Get-Content $configPath -Raw | ConvertFrom-Json

# Check if already on target platform
if ($config.defaultPlatform -eq $Platform) {
    Write-Host "Already configured for $Platform platform." -ForegroundColor Yellow
    if (-not $Force) {
        $confirm = Read-Host "Continue anyway? (y/n)"
        if ($confirm -ne "y") {
            Write-Host "Cancelled." -ForegroundColor Red
            exit 0
        }
    }
}

Write-Host "Switching documentation platform to: $Platform" -ForegroundColor Cyan

# Update platform-config.json
$config.defaultPlatform = $Platform
$config | ConvertTo-Json -Depth 10 | Set-Content $configPath -Encoding UTF8
Write-Host "[1/4] Updated platform-config.json" -ForegroundColor Green

# Update DocFX configuration files
$docfxFiles = @(
    (Join-Path $repoRoot "docs/docfx-developer/docfx.json"),
    (Join-Path $repoRoot "docs/docfx-user/docfx.json")
)

$gitBaseUrl = ""
$gitEditUrl = ""

switch ($Platform) {
    "GitHub" {
        $gitBaseUrl = $config.github.gitBaseUrl
        $gitEditUrl = $config.github.gitEditUrl
    }
    "AzureDevOps" {
        $gitBaseUrl = $config.azureDevOps.gitBaseUrl
        $gitEditUrl = $config.azureDevOps.gitEditUrl
    }
}

foreach ($docfxFile in $docfxFiles) {
    if (Test-Path $docfxFile) {
        $docfxContent = Get-Content $docfxFile -Raw | ConvertFrom-Json

        # Update git URLs
        if ($docfxContent.build.globalMetadata) {
            $docfxContent.build.globalMetadata._gitContribute = [PSCustomObject]@{
                repo = $gitBaseUrl
                branch = "main"
            }
            $docfxContent.build.globalMetadata._gitUrlPattern = $gitEditUrl
        }

        $docfxContent | ConvertTo-Json -Depth 10 | Set-Content $docfxFile -Encoding UTF8
        Write-Host "  Updated: $docfxFile" -ForegroundColor Gray
    }
}
Write-Host "[2/4] Updated DocFX configuration files" -ForegroundColor Green

# Enable/disable platform-specific workflows
$workflowsDir = Join-Path $repoRoot ".github/workflows"
$workflowsDisabledDir = Join-Path $repoRoot ".github/workflows.disabled"

switch ($Platform) {
    "GitHub" {
        # Enable GitHub workflows
        if (Test-Path $workflowsDisabledDir) {
            if (-not (Test-Path $workflowsDir)) {
                New-Item -ItemType Directory -Path $workflowsDir -Force | Out-Null
            }

            # Move workflows back
            Get-ChildItem $workflowsDisabledDir -Filter "docs-*.yml" | ForEach-Object {
                Move-Item $_.FullName (Join-Path $workflowsDir $_.Name) -Force
                Write-Host "  Enabled: $($_.Name)" -ForegroundColor Gray
            }

            # Remove disabled directory if empty
            if ((Get-ChildItem $workflowsDisabledDir).Count -eq 0) {
                Remove-Item $workflowsDisabledDir -Force
            }
        }
        Write-Host "[3/4] Enabled GitHub Actions workflows" -ForegroundColor Green
    }
    "AzureDevOps" {
        # Disable GitHub workflows
        if (Test-Path $workflowsDir) {
            if (-not (Test-Path $workflowsDisabledDir)) {
                New-Item -ItemType Directory -Path $workflowsDisabledDir -Force | Out-Null
            }

            # Move documentation workflows
            Get-ChildItem $workflowsDir -Filter "docs-*.yml" | ForEach-Object {
                Move-Item $_.FullName (Join-Path $workflowsDisabledDir $_.Name) -Force
                Write-Host "  Disabled: $($_.Name)" -ForegroundColor Gray
            }
        }
        Write-Host "[3/4] Disabled GitHub Actions workflows" -ForegroundColor Green
        Write-Host "  Note: Enable Azure Pipelines in Azure DevOps portal" -ForegroundColor Yellow
    }
}

# Summary
Write-Host "[4/4] Platform switch complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan

switch ($Platform) {
    "GitHub" {
        Write-Host "  1. Review changes: git status" -ForegroundColor White
        Write-Host "  2. Commit changes: git add . && git commit -m 'Switch to GitHub platform'" -ForegroundColor White
        Write-Host "  3. Push to GitHub: git push" -ForegroundColor White
        Write-Host "  4. Configure GitHub Pages in repository settings" -ForegroundColor White
        Write-Host "  5. Workflows will run automatically on next push to main" -ForegroundColor White
    }
    "AzureDevOps" {
        Write-Host "  1. Review changes: git status" -ForegroundColor White
        Write-Host "  2. Commit changes: git add . && git commit -m 'Switch to Azure DevOps platform'" -ForegroundColor White
        Write-Host "  3. Push to Azure DevOps: git push" -ForegroundColor White
        Write-Host "  4. Create Azure Pipelines (see docs/architecture/azure-devops-setup-guide.md)" -ForegroundColor White
        Write-Host "  5. Configure Wiki synchronization in Azure DevOps" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "Platform switched to: $Platform" -ForegroundColor Green
