<#
.SYNOPSIS
    Automates WSL setup with Claude Code installation for team members

.DESCRIPTION
    This script sets up a fresh WSL2 environment with Ubuntu and installs Claude Code CLI.
    It will WIPE any existing WSL installation and start fresh.

    What it does:
    1. Checks prerequisites (PowerShell 7+, Windows version)
    2. Backs up existing WSL distributions (optional)
    3. Unregisters and removes existing WSL Ubuntu
    4. Installs fresh WSL2 with Ubuntu
    5. Installs Node.js LTS in WSL
    6. Configures npm global directory
    7. Installs Claude Code CLI via npm
    8. Configures PATH in .bashrc
    9. Validates installation

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
    Version: 1.0.0
    Author: Bobby Johnson

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

function Backup-ExistingWSL {
    Write-SectionHeader "Backing Up Existing WSL"

    $distributions = wsl --list --quiet
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

    $distributions = wsl --list --quiet
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

    Write-LogMessage "Installing $Version..." -Level Info
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

function Install-NodeJS {
    param([int]$Version)

    Write-SectionHeader "Installing Node.js $Version LTS in WSL"

    Write-LogMessage "Installing Node.js via nodesource repository..." -Level Info
    Write-Host ""

    # Install Node.js using nodesource script
    $setupScript = @"
# Update package list
sudo apt update

# Install prerequisites
sudo apt install -y ca-certificates curl gnupg

# Add NodeSource repository
sudo mkdir -p /etc/apt/keyrings
curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | sudo gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg

echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_$Version.x nodistro main" | sudo tee /etc/apt/sources.list.d/nodesource.list

# Install Node.js
sudo apt update
sudo apt install -y nodejs

# Verify installation
node --version
npm --version
"@

    $setupScript | wsl bash

    if ($LASTEXITCODE -ne 0) {
        Write-LogMessage "Node.js installation failed" -Level Error
        throw "Failed to install Node.js"
    }

    Write-Host ""
    Write-LogMessage "Node.js installed successfully" -Level Success
    Write-Host ""
}

function Configure-NPM {
    Write-SectionHeader "Configuring npm"

    Write-LogMessage "Setting up npm global directory..." -Level Info

    $npmConfig = @"
# Create npm global directory
mkdir -p ~/.npm-global

# Configure npm to use new directory
npm config set prefix '~/.npm-global'

# Verify configuration
npm config get prefix
"@

    $npmConfig | wsl bash

    if ($LASTEXITCODE -ne 0) {
        Write-LogMessage "npm configuration failed" -Level Error
        throw "Failed to configure npm"
    }

    Write-LogMessage "npm configured successfully" -Level Success
    Write-Host ""
}

function Add-NPMToPath {
    Write-SectionHeader "Configuring PATH"

    Write-LogMessage "Adding npm global bin to PATH..." -Level Info

    $pathConfig = @"
# Add npm global bin to PATH if not already present
if ! grep -q '\.npm-global/bin' ~/.bashrc; then
    echo 'export PATH=~/.npm-global/bin:\$PATH' >> ~/.bashrc
    echo 'PATH configuration added to .bashrc'
else
    echo 'PATH already configured in .bashrc'
fi

# Source .bashrc to apply changes
source ~/.bashrc

# Verify PATH
echo \$PATH | grep -o '\.npm-global/bin'
"@

    $pathConfig | wsl bash

    if ($LASTEXITCODE -ne 0) {
        Write-LogMessage "PATH configuration failed" -Level Error
        throw "Failed to configure PATH"
    }

    Write-LogMessage "PATH configured successfully" -Level Success
    Write-Host ""
}

function Install-ClaudeCode {
    Write-SectionHeader "Installing Claude Code CLI"

    Write-LogMessage "Installing @anthropic-ai/claude-code globally..." -Level Info
    Write-Host ""

    $installCmd = @"
# Install Claude Code globally
npm install -g @anthropic-ai/claude-code

# Verify installation
which claude
claude --version 2>&1 || echo 'Claude Code installed (authentication required)'
"@

    $installCmd | wsl bash

    if ($LASTEXITCODE -ne 0) {
        Write-LogMessage "Claude Code installation failed" -Level Error
        Write-LogMessage "You may need to run: wsl npm install -g @anthropic-ai/claude-code" -Level Info
        throw "Failed to install Claude Code"
    }

    Write-Host ""
    Write-LogMessage "Claude Code installed successfully" -Level Success
    Write-Host ""
}

function Install-AdditionalTools {
    Write-SectionHeader "Installing Additional Tools"

    Write-LogMessage "Installing git, build-essential, curl..." -Level Info
    Write-Host ""

    $toolsCmd = @"
# Update package list
sudo apt update

# Install essential tools
sudo apt install -y git build-essential curl

# Verify installations
git --version
gcc --version | head -1
curl --version | head -1
"@

    $toolsCmd | wsl bash

    if ($LASTEXITCODE -ne 0) {
        Write-LogMessage "Additional tools installation completed with warnings" -Level Warning
    } else {
        Write-LogMessage "Additional tools installed successfully" -Level Success
    }

    Write-Host ""
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
        $nodeVersion = wsl node --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host " " -NoNewline
            Write-LogMessage "Node.js $nodeVersion ✓" -Level Success
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
        $npmVersion = wsl npm --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host " " -NoNewline
            Write-LogMessage "npm $npmVersion ✓" -Level Success
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

    Write-Host "  4. Navigate to your project:" -ForegroundColor White
    Write-Host "     cd /mnt/c/your/project/path" -ForegroundColor Gray
    Write-Host ""

    Write-Host "  5. Start using Claude Code:" -ForegroundColor White
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
}

#endregion

#region Main Execution

try {
    $script:ErrorActionPreference = 'Stop'

    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║                                                               ║" -ForegroundColor Cyan
    Write-Host "║        Claude Code WSL Setup Automation Script               ║" -ForegroundColor Cyan
    Write-Host "║                     Version 1.0.0                             ║" -ForegroundColor Cyan
    Write-Host "║                                                               ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""

    if (!$SkipCleanup) {
        Write-Host "  ⚠ WARNING: This will WIPE existing WSL Ubuntu installations!" -ForegroundColor Yellow
        Write-Host "  Use -SkipCleanup to preserve existing installations" -ForegroundColor Yellow
        Write-Host ""
    }

    # Phase 1: Prerequisites check
    $prereqsPassed = Test-Prerequisites

    if (!$prereqsPassed) {
        throw "Prerequisites check failed"
    }

    # Phase 2: Backup existing WSL (optional)
    if (!$SkipBackup -and !$SkipCleanup) {
        Backup-ExistingWSL
    }

    # Phase 3: Clean up existing WSL (optional)
    if (!$SkipCleanup) {
        Remove-ExistingWSL
    }

    # Phase 4: Install WSL features
    Install-WSLFeatures

    # Phase 5: Install Ubuntu
    Install-Ubuntu -Version $UbuntuVersion

    # Phase 6: Install Node.js
    Install-NodeJS -Version $NodeVersion

    # Phase 7: Configure npm
    Configure-NPM

    # Phase 8: Add npm to PATH
    Add-NPMToPath

    # Phase 9: Install Claude Code
    Install-ClaudeCode

    # Phase 10: Install additional tools
    Install-AdditionalTools

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
