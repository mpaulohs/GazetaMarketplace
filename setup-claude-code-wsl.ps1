<#
.SYNOPSIS
    Automates WSL setup with Claude Code installation for team members

.DESCRIPTION
    This script sets up a fresh WSL2 environment with Ubuntu and installs Claude Code CLI.
    It will WIPE any existing WSL installation and start fresh.

    What it does:
    1. Checks prerequisites (PowerShell 7+, Windows version, administrator)
    2. Installs WSL features if needed
    3. Backs up existing WSL distributions (optional)
    4. Unregisters and removes existing WSL Ubuntu (optional)
    5. Installs fresh WSL2 with Ubuntu
    6. Installs complete development environment (Node.js, git, build tools, utilities)
    7. Configures npm global directory
    8. Adds npm to PATH in .bashrc
    9. Installs Claude Code CLI via npm
    10. Installs GitHub CLI and configures authentication
    11. Validates all installations
    12. Displays setup instructions

.PARAMETER SkipBackup
    Skip backing up existing WSL distribution

.PARAMETER SkipCleanup
    Skip cleaning up existing WSL (install alongside existing)

.PARAMETER UbuntuVersion
    Ubuntu version to install (default: Ubuntu-24.04)

.PARAMETER NodeVersion
    Node.js major version to install (default: 20)

.EXAMPLE
    .\setup-claude-code-wsl.ps1
    # Full setup with backup and cleanup

.EXAMPLE
    .\setup-claude-code-wsl.ps1 -SkipBackup
    # Skip backup step (faster)

.EXAMPLE
    .\setup-claude-code-wsl.ps1 -SkipCleanup
    # Install alongside existing WSL

.NOTES
    Requires: PowerShell 7+, Windows 10 build 19041+ or Windows 11
    License: MIT
    Version: 2.0.0
    Author: Bobby Johnson

    Optimizations in v2.0.0:
    - WSL features now installed before backup/cleanup (critical fix)
    - Combined development environment installation (single apt transaction)
    - Shared package cache (66% reduction in apt updates)
    - Progressive validation after each critical phase
    - Fresh shell testing for PATH and Claude availability

    WARNING: This script will WIPE your existing WSL Ubuntu installation by default.
    Use -SkipCleanup to preserve existing installations.
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter()]
    [switch]$SkipBackup,

    [Parameter()]
    [switch]$SkipCleanup,

    [Parameter()]
    [string]$UbuntuVersion = "Ubuntu-24.04",

    [Parameter()]
    [ValidateRange(18, 22)]
    [int]$NodeVersion = 20
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

    # Check Windows version
    Write-LogMessage "Checking Windows version..." -NoNewline
    $winVersion = [System.Environment]::OSVersion.Version
    if ($winVersion.Build -ge 19041) {
        Write-Host " " -NoNewline
        Write-LogMessage "Windows build $($winVersion.Build) ✓" -Level Success
    } else {
        Write-Host ""
        Write-LogMessage "Windows 10 build 19041+ or Windows 11 required" -Level Error
        Write-LogMessage "Current build: $($winVersion.Build)" -Level Info
        $allPassed = $false
    }

    # Check if running as Administrator
    Write-LogMessage "Checking administrator privileges..." -NoNewline
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if ($currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Write-Host " " -NoNewline
        Write-LogMessage "Running as Administrator ✓" -Level Success
    } else {
        Write-Host ""
        Write-LogMessage "This script requires Administrator privileges" -Level Error
        Write-LogMessage "Right-click and 'Run as Administrator'" -Level Info
        $allPassed = $false
    }

    Write-Host ""

    if (!$allPassed) {
        throw "Prerequisites check failed. Please resolve the issues above."
    }

    return $allPassed
}

function Update-PackageCache {
    Write-LogMessage "Updating package cache..." -Level Info

    # Use script-scoped variable to track if we've already updated
    if ($script:PackageCacheUpdated) {
        Write-LogMessage "Package cache already updated (skipping)" -Level Info
        return
    }

    wsl bash -c "sudo apt update"

    if ($LASTEXITCODE -ne 0) {
        Write-LogMessage "Failed to update package cache" -Level Error
        throw "Failed to update package cache"
    }

    Write-LogMessage "Package cache updated successfully" -Level Success
    $script:PackageCacheUpdated = $true
}

function Backup-ExistingWSL {
    Write-SectionHeader "Backing Up Existing WSL"

    # Note: wsl --list output may contain special Unicode characters, so we normalize it
    $distributions = wsl --list --quiet | ForEach-Object {
        # Remove null characters, trim whitespace, and normalize
        $_.Replace("`0", "").Trim()
    } | Where-Object { $_ -ne "" }

    $ubuntuDistros = $distributions | Where-Object { $_ -match "Ubuntu" }

    if ($ubuntuDistros.Count -eq 0) {
        Write-LogMessage "No Ubuntu distributions found to backup" -Level Info
        return
    }

    $backupDir = "$env:USERPROFILE\WSL-Backups\$(Get-Date -Format 'yyyy-MM-dd-HHmmss')"
    New-Item -Path $backupDir -ItemType Directory -Force | Out-Null

    foreach ($distro in $ubuntuDistros) {
        Write-LogMessage "Backing up: $distro" -Level Info
        $exportPath = Join-Path $backupDir "$distro.tar"

        wsl --export $distro $exportPath

        if ($LASTEXITCODE -eq 0) {
            Write-LogMessage "Backed up to: $exportPath" -Level Success
        } else {
            Write-LogMessage "Failed to backup $distro" -Level Warning
        }
    }

    Write-Host ""
    Write-LogMessage "Backups saved to: $backupDir" -Level Success
    Write-Host ""
}

function Remove-ExistingWSL {
    Write-SectionHeader "Cleaning Up Existing WSL"

    # Note: wsl --list output may contain special Unicode characters, so we normalize it
    $distributions = wsl --list --quiet | ForEach-Object {
        # Remove null characters, trim whitespace, and normalize
        $_.Replace("`0", "").Trim()
    } | Where-Object { $_ -ne "" }

    $ubuntuDistros = $distributions | Where-Object { $_ -match "Ubuntu" }

    if ($ubuntuDistros.Count -eq 0) {
        Write-LogMessage "No Ubuntu distributions found" -Level Info
        Write-Host ""
        return
    }

    Write-Host ""
    Write-LogMessage "Found Ubuntu distributions:" -Level Warning
    foreach ($distro in $ubuntuDistros) {
        Write-Host "  - $distro" -ForegroundColor Yellow
    }
    Write-Host ""

    $confirmation = Read-Host "Type 'DELETE' to confirm removal of these distributions"

    if ($confirmation -ne 'DELETE') {
        Write-LogMessage "Cleanup cancelled by user" -Level Warning
        throw "User cancelled WSL cleanup"
    }

    foreach ($distro in $ubuntuDistros) {
        Write-LogMessage "Unregistering: $distro" -Level Info
        wsl --unregister $distro

        if ($LASTEXITCODE -eq 0) {
            Write-LogMessage "Removed: $distro" -Level Success
        } else {
            Write-LogMessage "Failed to remove $distro" -Level Error
            throw "Failed to unregister WSL distribution"
        }
    }

    Write-Host ""
}

function Install-WSLFeatures {
    Write-SectionHeader "Installing WSL Features"

    # Check if WSL is already enabled
    $wslFeature = Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux
    $vmFeature = Get-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform

    $needsReboot = $false

    if ($wslFeature.State -ne 'Enabled') {
        Write-LogMessage "Enabling WSL feature..." -Level Info
        Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux -NoRestart
        $needsReboot = $true
    } else {
        Write-LogMessage "WSL feature already enabled" -Level Success
    }

    if ($vmFeature.State -ne 'Enabled') {
        Write-LogMessage "Enabling Virtual Machine Platform..." -Level Info
        Enable-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform -NoRestart
        $needsReboot = $true
    } else {
        Write-LogMessage "Virtual Machine Platform already enabled" -Level Success
    }

    if ($needsReboot) {
        Write-Host ""
        Write-LogMessage "System reboot required to enable WSL features" -Level Warning
        Write-Host ""
        $reboot = Read-Host "Reboot now? (y/N)"
        if ($reboot -eq 'y' -or $reboot -eq 'Y') {
            Restart-Computer
        } else {
            Write-LogMessage "Please reboot manually and re-run this script" -Level Info
            throw "Reboot required"
        }
    }

    # Set WSL default version to 2
    Write-LogMessage "Setting WSL default version to 2..." -Level Info
    wsl --set-default-version 2

    Write-Host ""
}

function Install-Ubuntu {
    param([string]$Version)

    Write-SectionHeader "Installing Ubuntu"

    # Check if Ubuntu is already installed
    $distributions = wsl --list --quiet | ForEach-Object {
        # Remove null characters, trim whitespace, and normalize
        $_.Replace("`0", "").Trim()
    } | Where-Object { $_ -ne "" }

    $existingDistro = $distributions | Where-Object { $_ -eq $Version }

    if ($existingDistro) {
        Write-LogMessage "Ubuntu $Version is already installed" -Level Success
        Write-LogMessage "Skipping installation step" -Level Info
        Write-Host ""

        # Verify it's running WSL2
        $distroInfo = wsl --list --verbose | Select-String -Pattern $Version
        if ($distroInfo -match "2\s*$") {
            Write-LogMessage "Distribution is already using WSL2" -Level Success
        } else {
            Write-LogMessage "Converting distribution to WSL2..." -Level Info
            wsl --set-version $Version 2
            if ($LASTEXITCODE -eq 0) {
                Write-LogMessage "Converted to WSL2 successfully" -Level Success
            } else {
                Write-LogMessage "Failed to convert to WSL2" -Level Warning
            }
        }
        Write-Host ""
        return
    }

    Write-LogMessage "Installing $Version..." -Level Info
    Write-Host ""

    # Display user guidance before installation
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Yellow
    Write-Host " Ubuntu Account Setup" -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "You will now set up your Ubuntu user account." -ForegroundColor White
    Write-Host ""
    Write-Host "Steps:" -ForegroundColor Cyan
    Write-Host "  1. Ubuntu will use your Windows username by default" -ForegroundColor Gray
    Write-Host "  2. Enter a password (you'll need this for sudo commands)" -ForegroundColor Gray
    Write-Host "  3. You'll be dropped into a bash prompt" -ForegroundColor Gray
    Write-Host "  4. Type 'exit' and press Enter to continue this script" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Press Enter when ready to continue..." -ForegroundColor Yellow -NoNewline
    $null = Read-Host
    Write-Host ""

    # Install Ubuntu from Microsoft Store
    wsl --install -d $Version

    if ($LASTEXITCODE -ne 0) {
        Write-LogMessage "Failed to install $Version" -Level Error
        Write-LogMessage "You may need to install manually from Microsoft Store" -Level Info
        throw "Ubuntu installation failed"
    }

    Write-Host ""
    Write-LogMessage "Ubuntu installed successfully" -Level Success
    Write-LogMessage "You will be prompted to create a user account..." -Level Info
    Write-Host ""

    # Wait for Ubuntu to be ready
    Start-Sleep -Seconds 3
}

function Install-DevelopmentEnvironment {
    param([int]$NodeVersion)

    Write-SectionHeader "Installing Development Environment"

    # Display what will be installed
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Yellow
    Write-Host " Development Environment Components" -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Runtime & Languages:" -ForegroundColor Cyan
    Write-Host "  • Node.js $NodeVersion LTS    - JavaScript runtime" -ForegroundColor Gray
    Write-Host "  • npm                - Node package manager" -ForegroundColor Gray
    Write-Host "  • python3 + pip      - Python runtime and package manager" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Build Tools:" -ForegroundColor Cyan
    Write-Host "  • build-essential    - C/C++ compilers (for native npm modules)" -ForegroundColor Gray
    Write-Host "  • git                - Version control system" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Utilities:" -ForegroundColor Cyan
    Write-Host "  • curl/wget          - HTTP clients" -ForegroundColor Gray
    Write-Host "  • openssh-client     - SSH client for git & remote servers" -ForegroundColor Gray
    Write-Host "  • jq                 - JSON processor" -ForegroundColor Gray
    Write-Host "  • zip/unzip          - Archive utilities" -ForegroundColor Gray
    Write-Host "  • tree               - Directory structure viewer" -ForegroundColor Gray
    Write-Host "  • ripgrep            - Super fast code search" -ForegroundColor Gray
    Write-Host "  • htop               - Better process viewer" -ForegroundColor Gray
    Write-Host "  • bat                - Better cat with syntax highlighting" -ForegroundColor Gray
    Write-Host "  • fd-find            - User-friendly find alternative" -ForegroundColor Gray
    Write-Host ""
    Write-Host "You will be prompted for your Ubuntu password to run sudo commands." -ForegroundColor Yellow
    Write-Host ""

    # Update package cache once
    Update-PackageCache
    Write-Host ""

    Write-LogMessage "Installing all components..." -Level Info
    Write-Host ""

    # Create comprehensive installation script
    $setupScript = @"
#!/bin/bash
set -e

# Clean up any old nodesource files with invalid names
sudo rm -f /etc/apt/sources.list.d/nodesource.list* 2>/dev/null || true

# Install prerequisites for Node.js
sudo apt install -y ca-certificates curl gnupg

# Add NodeSource repository
sudo mkdir -p /etc/apt/keyrings
curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | sudo gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg

echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_$NodeVersion.x nodistro main" | sudo tee /etc/apt/sources.list.d/nodesource.list

# Update package list with new repository
sudo apt update

# Install ALL components in single transaction
sudo apt install -y \
    nodejs \
    git \
    build-essential \
    curl \
    wget \
    python3 \
    python3-pip \
    python3-venv \
    jq \
    zip \
    unzip \
    openssh-client \
    tree \
    ripgrep \
    htop \
    bat \
    fd-find

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo " Verifying Installations"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "Node.js: `$(node --version 2>&1 || echo 'FAILED')`"
echo "npm: `$(npm --version 2>&1 || echo 'FAILED')`"
echo "git: `$(git --version 2>&1 || echo 'FAILED')`"
echo "Python: `$(python3 --version 2>&1 || echo 'FAILED')`"
echo "gcc: `$(gcc --version 2>&1 | head -n1 || echo 'FAILED')`"
echo ""
"@

    # Write script to temp file and execute (convert to Unix line endings)
    $tempScript = "/tmp/setup-dev-env-$([guid]::NewGuid().ToString('N').Substring(0,8)).sh"
    $unixScript = $setupScript -replace "`r`n", "`n" -replace "`r", "`n"
    $unixScript | wsl bash -c "cat > $tempScript && chmod +x $tempScript"
    wsl bash $tempScript
    $exitCode = $LASTEXITCODE
    wsl bash -c "rm -f $tempScript"

    if ($exitCode -ne 0) {
        Write-LogMessage "Development environment installation failed" -Level Error
        throw "Failed to install development environment"
    }

    Write-Host ""
    Write-LogMessage "Development environment installed successfully" -Level Success
    Write-Host ""

    # Immediate validation
    Write-LogMessage "Validating critical components..." -Level Info

    # Test Node.js
    $nodeVersionOutput = (wsl node --version 2>&1) | Out-String
    $nodeVersionOutput = $nodeVersionOutput.Trim()
    if ($LASTEXITCODE -eq 0) {
        Write-LogMessage "Node.js $nodeVersionOutput ✓" -Level Success
    } else {
        Write-LogMessage "Node.js validation failed" -Level Error
        throw "Node.js not working after installation"
    }

    # Test npm
    $npmVersionOutput = (wsl npm --version 2>&1) | Out-String
    $npmVersionOutput = $npmVersionOutput.Trim()
    if ($LASTEXITCODE -eq 0) {
        Write-LogMessage "npm $npmVersionOutput ✓" -Level Success
    } else {
        Write-LogMessage "npm validation failed" -Level Error
        throw "npm not working after installation"
    }

    # Test git
    $result = wsl bash -c "git --version >/dev/null 2>&1 && echo 'ok' || echo 'fail'"
    if ($result -match 'ok') {
        Write-LogMessage "git ✓" -Level Success
    } else {
        Write-LogMessage "git validation failed" -Level Warning
    }

    Write-Host ""
}

function Set-NPMConfiguration {
    Write-SectionHeader "Configuring npm"

    Write-LogMessage "Setting up npm global directory..." -Level Info

    $npmConfig = @"
#!/bin/bash
set -e

# Create npm global directory
mkdir -p ~/.npm-global

# Configure npm to use new directory
npm config set prefix '~/.npm-global'

# Verify configuration
npm config get prefix
"@

    # Write script to temp file and execute (convert to Unix line endings)
    $tempScript = "/tmp/setup-npm-$([guid]::NewGuid().ToString('N').Substring(0,8)).sh"
    $unixScript = $npmConfig -replace "`r`n", "`n" -replace "`r", "`n"
    $unixScript | wsl bash -c "cat > $tempScript && chmod +x $tempScript"
    wsl bash $tempScript
    $exitCode = $LASTEXITCODE
    wsl bash -c "rm -f $tempScript"

    if ($exitCode -ne 0) {
        Write-LogMessage "npm configuration failed" -Level Error
        throw "Failed to configure npm"
    }

    Write-LogMessage "npm configured successfully" -Level Success

    # Immediate validation
    Write-LogMessage "Validating npm configuration..." -Level Info
    $npmPrefix = wsl bash -c "npm config get prefix"
    if ($npmPrefix -match '\.npm-global') {
        Write-LogMessage "npm prefix: $npmPrefix ✓" -Level Success
    } else {
        Write-LogMessage "npm prefix not set correctly (got: $npmPrefix)" -Level Warning
    }

    Write-Host ""
}

function Add-NPMToPath {
    Write-SectionHeader "Configuring PATH"

    Write-LogMessage "Adding npm global bin to PATH..." -Level Info

    # Check if already configured
    $alreadyConfigured = wsl bash -c "grep -q '.npm-global/bin' ~/.bashrc 2>/dev/null && echo 'yes' || echo 'no'"

    if ($alreadyConfigured -match 'yes') {
        Write-LogMessage "PATH configuration already exists in .bashrc" -Level Info
    } else {
        # Add PATH configuration using printf to avoid all quote escaping issues
        # printf is more reliable than echo for this use case
        wsl bash -c "printf '%s\n' 'export PATH=`"`$HOME/.npm-global/bin:`$PATH`"' >> ~/.bashrc"

        if ($LASTEXITCODE -eq 0) {
            Write-LogMessage "PATH configuration added to .bashrc" -Level Success
        } else {
            Write-LogMessage "Failed to add PATH configuration" -Level Error
            throw "Failed to configure PATH"
        }
    }

    # Verify in .bashrc
    wsl bash -c "grep '.npm-global/bin' ~/.bashrc"
    if ($LASTEXITCODE -eq 0) {
        Write-LogMessage "PATH configuration verified in .bashrc" -Level Success
    } else {
        Write-LogMessage "PATH not found in .bashrc" -Level Warning
        throw "Failed to configure PATH"
    }

    # Test in fresh login shell
    Write-LogMessage "Testing PATH in fresh shell session..." -Level Info
    $pathTest = wsl bash -l -c "echo `$PATH | grep -o '.npm-global/bin' || echo 'not-found'"
    if ($pathTest -match 'npm-global') {
        Write-LogMessage "PATH active in new shell sessions ✓" -Level Success
    } else {
        Write-LogMessage "PATH not active in new sessions (may need manual shell restart)" -Level Warning
    }

    Write-Host ""
}

function Install-ClaudeCode {
    Write-SectionHeader "Installing Claude Code CLI"

    Write-LogMessage "Installing @anthropic-ai/claude-code globally..." -Level Info
    Write-Host ""

    # Install Claude Code
    wsl bash -c "npm install -g @anthropic-ai/claude-code"

    if ($LASTEXITCODE -ne 0) {
        Write-LogMessage "Claude Code installation failed" -Level Error
        Write-LogMessage "You may need to run: wsl npm install -g @anthropic-ai/claude-code" -Level Info
        throw "Failed to install Claude Code"
    }

    Write-Host ""
    Write-LogMessage "Claude Code installed successfully" -Level Success

    # Immediate validation - test binary file exists
    Write-LogMessage "Validating installation..." -Level Info
    $binaryCheck = wsl bash -c "test -f ~/.npm-global/bin/claude && echo 'found' || echo 'not-found'"
    if ($binaryCheck -match 'found') {
        Write-LogMessage "Claude binary file exists ✓" -Level Success
    } else {
        Write-LogMessage "Claude binary not found at expected location" -Level Warning
    }

    # Test in fresh login shell
    $commandCheck = wsl bash -l -c "command -v claude >/dev/null 2>&1 && echo 'available' || echo 'not-available'"
    if ($commandCheck -match 'available') {
        Write-LogMessage "Claude command available in PATH ✓" -Level Success
    } else {
        Write-LogMessage "Claude command not in PATH (may need shell restart)" -Level Warning
    }

    # Test version command in fresh shell
    $versionCheckOutput = (wsl bash -l -c "claude --version 2>&1 || echo 'failed'") | Out-String
    $versionCheckOutput = $versionCheckOutput.Trim()
    if ($versionCheckOutput -notmatch 'failed' -and $versionCheckOutput -match '\d+\.\d+') {
        Write-LogMessage "Claude version: $versionCheckOutput ✓" -Level Success
    } else {
        Write-LogMessage "Could not get Claude version (installation may need verification)" -Level Warning
    }

    Write-Host ""
}

function Install-GitHubCLI {
    Write-SectionHeader "Installing GitHub CLI"

    Write-LogMessage "Adding GitHub CLI repository..." -Level Info

    # Add GPG key
    wsl bash -c "curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg && sudo chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg"
    if ($LASTEXITCODE -ne 0) {
        Write-LogMessage "Failed to add GitHub CLI GPG key" -Level Error
        throw "Failed to add GitHub CLI repository"
    }

    # Add repository
    wsl bash -c "echo 'deb [arch=amd64 signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main' | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null"
    if ($LASTEXITCODE -ne 0) {
        Write-LogMessage "Failed to add GitHub CLI repository" -Level Error
        throw "Failed to add GitHub CLI repository"
    }

    Write-LogMessage "Repository added successfully" -Level Success
    Write-Host ""

    # Update package cache using shared function
    # Note: Force update since we just added a new repository
    $script:PackageCacheUpdated = $false
    Update-PackageCache
    Write-Host ""

    Write-LogMessage "Installing GitHub CLI..." -Level Info
    Write-Host ""
    wsl bash -c "sudo apt install -y gh"
    if ($LASTEXITCODE -ne 0) {
        Write-LogMessage "Failed to install GitHub CLI" -Level Error
        throw "Failed to install GitHub CLI"
    }

    Write-Host ""
    Write-LogMessage "GitHub CLI installed successfully" -Level Success
    Write-Host ""

    # Interactive auth setup
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host " GitHub CLI Setup" -ForegroundColor Green
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host ""
    Write-Host "Now let's configure git with your GitHub account." -ForegroundColor White
    Write-Host ""
    Write-Host "You will be prompted to:" -ForegroundColor Cyan
    Write-Host "  1. Authenticate with GitHub (browser or token)" -ForegroundColor Gray
    Write-Host "  2. Choose your preferred protocol (HTTPS/SSH)" -ForegroundColor Gray
    Write-Host "  3. Configure git credentials" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Press Enter to launch 'gh auth login'..." -ForegroundColor Yellow -NoNewline
    $null = Read-Host
    Write-Host ""

    # Run gh auth login interactively
    wsl gh auth login

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-LogMessage "GitHub authentication successful!" -Level Success

        # Verify auth status
        Write-LogMessage "Verifying authentication..." -Level Info
        wsl gh auth status
        Write-Host ""
    } else {
        Write-Host ""
        Write-LogMessage "GitHub authentication failed or was skipped" -Level Warning
        Write-LogMessage "You can run 'wsl gh auth login' later to authenticate" -Level Info
        Write-Host ""
    }
}

function Test-Installation {
    Write-SectionHeader "Validating Installation"

    $allValid = $true

    # Test WSL
    Write-LogMessage "Testing WSL..." -NoNewline
    try {
        $wslVersion = wsl --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host " " -NoNewline
            Write-LogMessage "WSL working ✓" -Level Success
        } else {
            throw "WSL not working"
        }
    } catch {
        Write-Host ""
        Write-LogMessage "WSL test failed" -Level Error
        $allValid = $false
    }

    # Test Node.js
    Write-LogMessage "Testing Node.js..." -NoNewline
    try {
        $nodeVersionOutput = (wsl node --version 2>&1) | Out-String
        $nodeVersionOutput = $nodeVersionOutput.Trim()
        if ($LASTEXITCODE -eq 0) {
            Write-Host " " -NoNewline
            Write-LogMessage "Node.js $nodeVersionOutput ✓" -Level Success
        } else {
            throw "Node.js not found"
        }
    } catch {
        Write-Host ""
        Write-LogMessage "Node.js test failed" -Level Error
        $allValid = $false
    }

    # Test npm
    Write-LogMessage "Testing npm..." -NoNewline
    try {
        $npmVersionOutput = (wsl npm --version 2>&1) | Out-String
        $npmVersionOutput = $npmVersionOutput.Trim()
        if ($LASTEXITCODE -eq 0) {
            Write-Host " " -NoNewline
            Write-LogMessage "npm $npmVersionOutput ✓" -Level Success
        } else {
            throw "npm not found"
        }
    } catch {
        Write-Host ""
        Write-LogMessage "npm test failed" -Level Error
        $allValid = $false
    }

    # Test Claude Code
    Write-LogMessage "Testing Claude Code..." -NoNewline
    $claudeCheck = @"
if command -v claude &> /dev/null; then
    echo 'installed'
else
    echo 'not found'
fi
"@

    $result = $claudeCheck | wsl bash
    if ($result -match 'installed') {
        Write-Host " " -NoNewline
        Write-LogMessage "Claude Code CLI ✓" -Level Success
    } else {
        Write-Host ""
        Write-LogMessage "Claude Code CLI not found in PATH" -Level Error
        $allValid = $false
    }

    # Test PATH configuration
    Write-LogMessage "Testing PATH configuration..." -NoNewline
    $pathCheck = @"
if grep -q '\.npm-global/bin' ~/.bashrc; then
    echo 'configured'
else
    echo 'not configured'
fi
"@

    $result = $pathCheck | wsl bash
    if ($result -match 'configured') {
        Write-Host " " -NoNewline
        Write-LogMessage "PATH in .bashrc ✓" -Level Success
    } else {
        Write-Host ""
        Write-LogMessage "PATH not configured in .bashrc" -Level Warning
        $allValid = $false
    }

    Write-Host ""

    if ($allValid) {
        Write-LogMessage "All validation checks passed ✓" -Level Success
    } else {
        Write-LogMessage "Some validation checks failed" -Level Warning
        Write-LogMessage "Review the messages above" -Level Info
    }

    Write-Host ""

    return $allValid
}

function New-InstallationReport {
    Write-SectionHeader "Installation Complete"

    Write-Host ""
    Write-LogMessage "Claude Code WSL environment is ready!" -Level Success
    Write-Host ""

    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " Next Steps" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""

    Write-Host "  1. Open WSL terminal:" -ForegroundColor White
    Write-Host "     wsl" -ForegroundColor Gray
    Write-Host ""

    Write-Host "  2. Verify Claude Code:" -ForegroundColor White
    Write-Host "     claude --version" -ForegroundColor Gray
    Write-Host ""

    Write-Host "  3. Login to Claude Code:" -ForegroundColor White
    Write-Host "     claude" -ForegroundColor Gray
    Write-Host "     (Follow authentication prompts)" -ForegroundColor Gray
    Write-Host ""

    Write-Host "  4. (Optional) Add PowerShell convenience function:" -ForegroundColor White
    Write-Host "     Add this to your PowerShell profile to use 'claude' from PowerShell:" -ForegroundColor Gray
    Write-Host ""
    Write-Host "     function claude {" -ForegroundColor DarkGray
    Write-Host "          wsl bash -c `"claude `$(`$args -join ' ')`"" -ForegroundColor DarkGray
    Write-Host "     }" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "     Then reload profile: . `$PROFILE" -ForegroundColor Gray
    Write-Host ""

    Write-Host "  5. Navigate to your project:" -ForegroundColor White
    Write-Host "     cd /mnt/c/your/project/path" -ForegroundColor Gray
    Write-Host ""

    Write-Host "  6. Start using Claude Code:" -ForegroundColor White
    Write-Host "     claude" -ForegroundColor Gray
    Write-Host ""

    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " Installed Components" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""

    Write-Host "  ✓ WSL2 with Ubuntu $UbuntuVersion" -ForegroundColor Green
    Write-Host "  ✓ Node.js $NodeVersion LTS" -ForegroundColor Green
    Write-Host "  ✓ npm (configured with global directory)" -ForegroundColor Green
    Write-Host "  ✓ Claude Code CLI (latest version)" -ForegroundColor Green
    Write-Host "  ✓ Git, build-essential, curl" -ForegroundColor Green
    Write-Host ""

    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " Configuration" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""

    Write-Host "  npm global directory: ~/.npm-global" -ForegroundColor Gray
    Write-Host "  Claude binary: ~/.npm-global/bin/claude" -ForegroundColor Gray
    Write-Host "  PATH: Updated in ~/.bashrc" -ForegroundColor Gray
    Write-Host ""

    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " Troubleshooting" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""

    Write-Host "  If 'claude' command not found:" -ForegroundColor Yellow
    Write-Host "    1. Restart WSL: wsl --shutdown && wsl" -ForegroundColor Gray
    Write-Host "    2. Or reload .bashrc: source ~/.bashrc" -ForegroundColor Gray
    Write-Host ""

    Write-Host "  If authentication fails:" -ForegroundColor Yellow
    Write-Host "    - Ensure you have an active Claude subscription" -ForegroundColor Gray
    Write-Host "    - Follow browser OAuth flow" -ForegroundColor Gray
    Write-Host ""

    Write-Host "  To use claude from PowerShell (not just WSL):" -ForegroundColor Yellow
    Write-Host "    - Add the function shown in step 4 to your PowerShell profile" -ForegroundColor Gray
    Write-Host "    - Profile location: `$PROFILE (usually Documents\PowerShell\Microsoft.PowerShell_profile.ps1)" -ForegroundColor Gray
    Write-Host "    - Then you can use 'claude' command from any PowerShell window" -ForegroundColor Gray
    Write-Host ""
}

#endregion

#region Main Execution

try {
    $script:ErrorActionPreference = 'Stop'

    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║                                                               ║" -ForegroundColor Cyan
    Write-Host "║       Claude Code WSL Setup Automation Script                ║" -ForegroundColor Cyan
    Write-Host "║                    Version 2.0.0                              ║" -ForegroundColor Cyan
    Write-Host "║              (Optimized Flow & Validation)                    ║" -ForegroundColor Cyan
    Write-Host "║                                                               ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""

    if (!$SkipCleanup) {
        Write-Host "  ⚠ WARNING: This will WIPE existing WSL Ubuntu installations!" -ForegroundColor Yellow
        Write-Host "  Use -SkipCleanup to preserve existing installations" -ForegroundColor Yellow
        Write-Host ""
    }

    # Initialize script-level variables
    $script:PackageCacheUpdated = $false

    # Phase 1: Prerequisites check
    $prereqsPassed = Test-Prerequisites

    if (!$prereqsPassed) {
        throw "Prerequisites check failed"
    }

    # Phase 2: Install WSL features (MOVED UP - required for backup/cleanup to work)
    if ($PSCmdlet.ShouldProcess("WSL features", "Install")) {
        Install-WSLFeatures
    }

    # Phase 3: Backup existing WSL (optional)
    if (!$SkipBackup -and !$SkipCleanup) {
        if ($PSCmdlet.ShouldProcess("Existing WSL distributions", "Backup")) {
            Backup-ExistingWSL
        }
    }

    # Phase 4: Clean up existing WSL (optional)
    if (!$SkipCleanup) {
        if ($PSCmdlet.ShouldProcess("Existing WSL Ubuntu installations", "Remove")) {
            Remove-ExistingWSL
        }
    }

    # Phase 5: Install Ubuntu
    if ($PSCmdlet.ShouldProcess("Ubuntu $UbuntuVersion", "Install")) {
        Install-Ubuntu -Version $UbuntuVersion
    }

    # Phase 6: Install Development Environment (Node.js + system tools combined)
    if ($PSCmdlet.ShouldProcess("Development environment (Node.js $NodeVersion + system tools)", "Install")) {
        Install-DevelopmentEnvironment -NodeVersion $NodeVersion
    }

    # Phase 7: Configure npm
    if ($PSCmdlet.ShouldProcess("npm global directory", "Configure")) {
        Set-NPMConfiguration
    }

    # Phase 8: Add npm to PATH
    if ($PSCmdlet.ShouldProcess("~/.bashrc PATH configuration", "Add npm global bin")) {
        Add-NPMToPath
    }

    # Phase 9: Install Claude Code
    if ($PSCmdlet.ShouldProcess("Claude Code CLI via npm", "Install")) {
        Install-ClaudeCode
    }

    # Phase 10: Install GitHub CLI
    if ($PSCmdlet.ShouldProcess("GitHub CLI (gh) in WSL", "Install and configure")) {
        Install-GitHubCLI
    }

    # Phase 11: Validate installation
    $validationPassed = Test-Installation

    # Phase 12: Show completion message
    New-InstallationReport

    if (!$validationPassed) {
        Write-LogMessage "Installation completed with warnings - review validation results above" -Level Warning
    }

} catch {
    Write-Host ""
    Write-LogMessage "Setup failed: $_" -Level Error
    Write-Host ""
    Write-LogMessage "For troubleshooting, see:" -Level Info
    Write-Host "  - WSL docs: https://docs.microsoft.com/en-us/windows/wsl/" -ForegroundColor Gray
    Write-Host "  - Claude Code docs: https://docs.claude.com/en/docs/claude-code" -ForegroundColor Gray
    Write-Host ""
    exit 1
}

#endregion
