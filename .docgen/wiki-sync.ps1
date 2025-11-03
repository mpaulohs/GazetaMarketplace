#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Sync wiki content to GitHub or Azure DevOps Wiki

.DESCRIPTION
    This script synchronizes the wiki content from docs/wiki/ to either GitHub Wiki
    or Azure DevOps Wiki based on the repository's platform.

.PARAMETER Platform
    Target platform (GitHub or AzureDevOps). Auto-detects if not specified.

.PARAMETER Force
    Force sync even if there are no changes detected

.EXAMPLE
    ./wiki-sync.ps1
    # Auto-detects platform and syncs wiki

.EXAMPLE
    ./wiki-sync.ps1 -Platform GitHub
    # Explicitly sync to GitHub Wiki

.EXAMPLE
    ./wiki-sync.ps1 -Platform AzureDevOps
    # Explicitly sync to Azure DevOps Wiki (requires Phase 7)

.NOTES
    Author: NET10 Project Example Team
    Created: 2025-11-03
    Requires: Git, PowerShell Core (pwsh)
#>

param(
    [ValidateSet("GitHub", "AzureDevOps", "Auto")]
    [string]$Platform = "Auto",

    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Colors for output
function Write-Info { param($Message) Write-Host "9  $Message" -ForegroundColor Cyan }
function Write-Success { param($Message) Write-Host " $Message" -ForegroundColor Green }
function Write-Warning { param($Message) Write-Host "   $Message" -ForegroundColor Yellow }
function Write-Failure { param($Message) Write-Host "L $Message" -ForegroundColor Red }

Write-Info "Wiki Sync Script - Starting..."

# Detect platform if Auto
if ($Platform -eq "Auto") {
    Write-Info "Auto-detecting platform from git remote..."

    try {
        $remoteUrl = git config --get remote.origin.url

        if ($remoteUrl -match "github\.com") {
            $Platform = "GitHub"
            Write-Success "Detected GitHub platform"
        } elseif ($remoteUrl -match "dev\.azure\.com") {
            $Platform = "AzureDevOps"
            Write-Success "Detected Azure DevOps platform"
        } else {
            Write-Failure "Cannot detect platform from remote URL: $remoteUrl"
            Write-Info "Please specify -Platform GitHub or -Platform AzureDevOps"
            exit 1
        }
    } catch {
        Write-Failure "Failed to detect platform: $_"
        exit 1
    }
}

Write-Info "Target platform: $Platform"

# Validate wiki directory exists
$wikiDir = "docs/wiki"
if (-not (Test-Path $wikiDir)) {
    Write-Failure "Wiki directory not found: $wikiDir"
    Write-Info "Please run Phase 5 first to create wiki content"
    exit 1
}

# Count wiki files
$wikiFiles = Get-ChildItem -Path $wikiDir -Filter "*.md" -File
Write-Info "Found $($wikiFiles.Count) wiki files to sync"

if ($wikiFiles.Count -eq 0) {
    Write-Warning "No wiki files found in $wikiDir"
    exit 0
}

# Platform-specific sync logic
if ($Platform -eq "GitHub") {
    Write-Info "Syncing to GitHub Wiki..."

    # Get wiki repository URL (append .wiki.git to main repo URL)
    try {
        $mainRepoUrl = git config --get remote.origin.url
        $wikiRepoUrl = $mainRepoUrl -replace "\.git$", ".wiki.git"

        # Handle both HTTPS and SSH URLs
        if ($mainRepoUrl -notmatch "\.git$") {
            $wikiRepoUrl = "$mainRepoUrl.wiki.git"
        }

        Write-Info "Wiki repository URL: $wikiRepoUrl"
    } catch {
        Write-Failure "Failed to get remote URL: $_"
        exit 1
    }

    # Create temporary directory for wiki clone
    $tempWikiDir = ".wiki-temp"

    if (Test-Path $tempWikiDir) {
        Write-Info "Removing existing temp wiki directory..."
        Remove-Item $tempWikiDir -Recurse -Force
    }

    # Clone wiki repository
    Write-Info "Cloning GitHub Wiki repository..."
    try {
        git clone $wikiRepoUrl $tempWikiDir 2>&1 | Out-Null

        if (-not $?) {
            Write-Warning "Wiki repository does not exist yet. It will be created on first GitHub Wiki page creation."
            Write-Info "Please create at least one page via GitHub UI first, then re-run this script."
            exit 0
        }

        Write-Success "Wiki repository cloned"
    } catch {
        Write-Failure "Failed to clone wiki: $_"
        Write-Info "The wiki may not exist yet. Create a page via GitHub UI first."
        exit 1
    }

    # Copy wiki files
    Write-Info "Copying wiki files..."
    try {
        # Copy all .md files
        Copy-Item "$wikiDir/*.md" $tempWikiDir -Force

        # Rename README.md to Home.md (GitHub Wiki convention)
        if (Test-Path "$tempWikiDir/README.md") {
            Move-Item "$tempWikiDir/README.md" "$tempWikiDir/Home.md" -Force
            Write-Info "Renamed README.md to Home.md (GitHub Wiki homepage)"
        }

        Write-Success "Files copied to wiki repository"
    } catch {
        Write-Failure "Failed to copy files: $_"
        Remove-Item $tempWikiDir -Recurse -Force
        exit 1
    }

    # Create _Sidebar.md for navigation
    Write-Info "Creating wiki sidebar navigation..."
    $sidebarContent = @"
**[Home](Home)**

**Documentation**
* [System Purpose](system-purpose)
* [System Access](system-access)
* [Feature Summary](feature-summary)
* [Active Development](active-development)

---

**External Links**
* [Developer Docs](https://notmyself.github.io/net10-project-example/)
* [User Docs](https://notmyself.github.io/net10-project-example/user/)
* [GitHub Repository](https://github.com/NotMyself/net10-project-example)
"@

    Set-Content -Path "$tempWikiDir/_Sidebar.md" -Value $sidebarContent
    Write-Success "Sidebar navigation created"

    # Check for changes
    Push-Location $tempWikiDir
    try {
        $gitStatus = git status --porcelain

        if (-not $gitStatus -and -not $Force) {
            Write-Success "No changes detected in wiki content"
            Pop-Location
            Remove-Item $tempWikiDir -Recurse -Force
            exit 0
        }

        # Commit and push changes
        Write-Info "Committing wiki changes..."
        git add .

        $commitMessage = "Update wiki content from main repository ($(Get-Date -Format 'yyyy-MM-dd HH:mm'))"
        git commit -m $commitMessage

        Write-Info "Pushing to GitHub Wiki..."
        git push

        Write-Success "GitHub Wiki sync complete!"
        Write-Info "View at: https://github.com/NotMyself/net10-project-example/wiki"

    } catch {
        Write-Failure "Failed to commit/push changes: $_"
        Pop-Location
        Remove-Item $tempWikiDir -Recurse -Force
        exit 1
    } finally {
        Pop-Location
    }

    # Cleanup
    Write-Info "Cleaning up temporary files..."
    Remove-Item $tempWikiDir -Recurse -Force
    Write-Success "Cleanup complete"

} elseif ($Platform -eq "AzureDevOps") {
    # Azure DevOps Wiki sync (via REST API)
    Write-Warning "Azure DevOps Wiki sync requires REST API calls"
    Write-Info "This feature will be implemented in Phase 7 (Azure DevOps Plugin)"
    Write-Info ""
    Write-Info "Planned implementation will use:"
    Write-Info "  - Azure DevOps REST API for wiki operations"
    Write-Info "  - Service principal authentication"
    Write-Info "  - Wiki page creation/update via HTTP POST"
    Write-Info ""
    Write-Info "For now, manually copy wiki files to Azure DevOps Wiki via the web UI."
    exit 0
}

Write-Success "Wiki sync operation completed successfully!"
