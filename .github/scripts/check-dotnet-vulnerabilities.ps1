#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Scans .NET projects for vulnerable NuGet package dependencies.

.DESCRIPTION
    Uses `dotnet list package --vulnerable` to detect known security vulnerabilities
    in NuGet package dependencies. Checks all projects in the solution and aggregates
    results into a JSON format compatible with the PR validation pipeline.

.PARAMETER SolutionPath
    Path to the solution or directory containing projects to scan (default: current directory).

.PARAMETER IncludeTransitive
    Include transitive (indirect) dependencies in the scan (default: true).

.PARAMETER MinimumSeverity
    Minimum severity level to report: Low, Moderate, High, Critical (default: Low).

.OUTPUTS
    JSON object with findings array containing vulnerability details.
#>

param(
    [string]$SolutionPath = ".",
    [switch]$IncludeTransitive = $true,
    [string]$MinimumSeverity = "Low"
)

$ErrorActionPreference = "Continue"

# Result structure
$findings = @()
$errorCount = 0
$warningCount = 0
$infoCount = 0

Write-Host "🔍 Scanning for NuGet package vulnerabilities..."
Write-Host "Solution: $SolutionPath"
Write-Host "Include Transitive: $IncludeTransitive"
Write-Host "Minimum Severity: $MinimumSeverity"
Write-Host ""

# Build the dotnet command
$dotnetArgs = @("list", "package", "--vulnerable", "--include-vulnerable")
if ($IncludeTransitive) {
    $dotnetArgs += "--include-transitive"
}

# Run the command
try {
    Push-Location $SolutionPath
    $output = & dotnet @dotnetArgs 2>&1 | Out-String
    $exitCode = $LASTEXITCODE
    Pop-Location

    Write-Host "dotnet list package output:"
    Write-Host $output
    Write-Host ""

    # Parse output for vulnerabilities
    # Format:
    # Project 'ProjectName' has vulnerable packages:
    #    [net10.0]:
    #    Top-level Package      Requested   Resolved   Severity   Advisory URL
    #    > PackageName          1.0.0       1.0.0      High       https://...

    $lines = $output -split "`n"
    $currentProject = $null
    $inVulnerableSection = $false
    $isTopLevel = $true

    foreach ($line in $lines) {
        $line = $line.Trim()

        # Detect project line
        if ($line -match "^Project '(.+?)' has the following vulnerable packages") {
            $currentProject = $Matches[1]
            $inVulnerableSection = $true
            $isTopLevel = $true
            Write-Host "Found vulnerabilities in project: $currentProject"
            continue
        }

        # Detect transitive dependencies section
        if ($line -match "^Transitive Package") {
            $isTopLevel = $false
            continue
        }

        # Detect top-level package section
        if ($line -match "^Top-level Package") {
            $isTopLevel = $true
            continue
        }

        # Parse vulnerability line (starts with >)
        if ($line -match "^>\s+(\S+)\s+(\S+)\s+(\S+)\s+(Critical|High|Moderate|Low)\s+(.+)$") {
            $packageName = $Matches[1]
            $requested = $Matches[2]
            $resolved = $Matches[3]
            $severity = $Matches[4]
            $advisoryUrl = $Matches[5]

            # Map severity to our categories
            $findingSeverity = switch ($severity) {
                "Critical" { "error"; $errorCount++ }
                "High"     { "error"; $errorCount++ }
                "Moderate" { "warning"; $warningCount++ }
                "Low"      { "warning"; $warningCount++ }
                default    { "info"; $infoCount++ }
            }

            # Determine category based on dependency type
            $category = if ($isTopLevel) { "NuGet Vulnerability (Direct)" } else { "NuGet Vulnerability (Transitive)" }

            # Extract project file path
            $projectFile = if ($currentProject) {
                # Try to find the .csproj file
                $projectName = $currentProject -replace "^.*[\\/]", "" -replace "\.csproj$", ""
                "src/$projectName/$projectName.csproj"
            } else {
                "Unknown"
            }

            $findings += @{
                severity = $findingSeverity
                category = $category
                file = $projectFile
                line = 0
                rule = "NUGET-VULN-$severity"
                message = "$packageName $resolved has a known $severity severity vulnerability"
                remediation = "Update to a non-vulnerable version. See advisory: $advisoryUrl"
            }

            Write-Host "  - $packageName $resolved ($severity): $advisoryUrl"
        }
    }

    # Check if no vulnerabilities were found
    if ($output -match "no vulnerable packages") {
        Write-Host "✅ No vulnerable packages found"
    }

} catch {
    Write-Host "❌ Error running dotnet list package: $_"
    $findings += @{
        severity = "error"
        category = "NuGet Vulnerability Scan"
        file = ""
        line = 0
        rule = "NUGET-SCAN-ERROR"
        message = "Failed to scan for vulnerabilities: $_"
        remediation = "Check that .NET SDK is properly installed and projects are valid"
    }
    $errorCount++
}

# Determine overall status
$status = if ($errorCount -gt 0) { "failed" } elseif ($warningCount -gt 0) { "warning" } else { "pass" }

# Build result object
$result = @{
    step = "nuget-vulnerability-scan"
    status = $status
    timestamp = (Get-Date).ToUniversalTime().ToString("o")
    findings = $findings
    summary = @{
        total = $findings.Count
        errors = $errorCount
        warnings = $warningCount
        info = $infoCount
    }
}

# Output JSON
$json = $result | ConvertTo-Json -Depth 10 -Compress
Write-Output $json

# Exit with appropriate code
if ($errorCount -gt 0) {
    exit 1
} else {
    exit 0
}
