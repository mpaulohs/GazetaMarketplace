<#
.SYNOPSIS
    Detects the current platform (GitHub Actions, Azure DevOps, or Local).

.DESCRIPTION
    This script identifies the platform where documentation is being built.
    It checks for CI/CD environment variables and git remote configuration to
    determine whether the build is running on GitHub Actions, Azure DevOps, or locally.

.EXAMPLE
    PS> .\detect-platform.ps1
    Detected Platform: GitHub
    Repository: NotMyself/net10-project-example
    Branch: main

.NOTES
    Used by documentation build scripts to configure platform-specific settings.
#>

function Get-CIPlatform {
    <#
    .SYNOPSIS
        Determines the current CI/CD platform.

    .OUTPUTS
        String - One of: "GitHub", "AzureDevOps", "Local"
    #>

    # Check for GitHub Actions
    if ($env:GITHUB_ACTIONS -eq "true") {
        return "GitHub"
    }

    # Check for Azure DevOps
    if ($env:TF_BUILD -eq "True") {
        return "AzureDevOps"
    }

    # Default to Local
    return "Local"
}

function Get-PlatformConfig {
    <#
    .SYNOPSIS
        Retrieves platform-specific configuration.

    .PARAMETER Platform
        The platform name (GitHub, AzureDevOps, or Local).

    .OUTPUTS
        PSCustomObject - Platform configuration with git URLs and other settings.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("GitHub", "AzureDevOps", "Local")]
        [string]$Platform
    )

    # Load platform configuration
    $configPath = Join-Path $PSScriptRoot "platform-config.json"
    if (-not (Test-Path $configPath)) {
        throw "Platform configuration not found at: $configPath"
    }

    $config = Get-Content $configPath -Raw | ConvertFrom-Json

    # For local environment, auto-detect from git remote
    if ($Platform -eq "Local") {
        try {
            # Get git remote URL
            $gitRemote = git config --get remote.origin.url

            # Detect platform from remote URL
            if ($gitRemote -match "github\.com[:/]([^/]+)/([^/.]+)") {
                Write-Host "Detected GitHub repository from git remote: $($matches[1])/$($matches[2])"
                return [PSCustomObject]@{
                    Platform = "GitHub"
                    Repository = "$($matches[1])/$($matches[2])"
                    GitBaseUrl = "https://github.com/$($matches[1])/$($matches[2])"
                    GitEditUrl = "https://github.com/$($matches[1])/$($matches[2])/blob"
                    AutoDetected = $true
                }
            }
            elseif ($gitRemote -match "dev\.azure\.com/([^/]+)/([^/]+)/_git/([^/]+)") {
                Write-Host "Detected Azure DevOps repository from git remote: $($matches[1])/$($matches[2])/$($matches[3])"
                return [PSCustomObject]@{
                    Platform = "AzureDevOps"
                    Organization = $matches[1]
                    Project = $matches[2]
                    Repository = $matches[3]
                    GitBaseUrl = "https://dev.azure.com/$($matches[1])/$($matches[2])/_git/$($matches[3])"
                    GitEditUrl = "https://dev.azure.com/$($matches[1])/$($matches[2])/_git/$($matches[3])?path="
                    AutoDetected = $true
                }
            }
            else {
                Write-Warning "Could not detect platform from git remote: $gitRemote"
            }
        }
        catch {
            Write-Warning "Could not read git remote configuration: $_"
        }
    }

    # Return platform-specific configuration from config file
    switch ($Platform) {
        "GitHub" {
            return [PSCustomObject]@{
                Platform = "GitHub"
                Repository = $config.github.repository
                GitBaseUrl = $config.github.gitBaseUrl
                GitEditUrl = $config.github.gitEditUrl
                AutoDetected = $false
            }
        }
        "AzureDevOps" {
            return [PSCustomObject]@{
                Platform = "AzureDevOps"
                Organization = $config.azureDevOps.organization
                Project = $config.azureDevOps.project
                Repository = $config.azureDevOps.repository
                GitBaseUrl = $config.azureDevOps.gitBaseUrl
                GitEditUrl = $config.azureDevOps.gitEditUrl
                AutoDetected = $false
            }
        }
    }
}

# Main script execution
$platform = Get-CIPlatform
$platformConfig = Get-PlatformConfig -Platform $platform

Write-Host "Detected Platform: $($platformConfig.Platform)" -ForegroundColor Green
Write-Host "Git Base URL: $($platformConfig.GitBaseUrl)" -ForegroundColor Cyan
Write-Host "Git Edit URL: $($platformConfig.GitEditUrl)" -ForegroundColor Cyan

if ($platformConfig.AutoDetected) {
    Write-Host "Platform was auto-detected from git remote configuration" -ForegroundColor Yellow
}

# Output configuration for use by calling scripts
return $platformConfig
