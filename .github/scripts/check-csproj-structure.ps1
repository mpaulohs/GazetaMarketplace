#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates .csproj file structure and configuration for .NET 10 project standards.

.DESCRIPTION
    Scans all .csproj files in the solution to ensure they follow project conventions:
    - No Version attributes in PackageReference (CPM enforced)
    - Required properties set correctly (TargetFramework, Nullable, etc.)
    - Test projects have EnableMSTestRunner and OutputType configured
    - No duplicate PackageReference entries
    - Valid XML structure

.PARAMETER SolutionPath
    Path to the solution or directory containing projects to scan (default: current directory).

.OUTPUTS
    JSON object with findings array containing validation issue details.
#>

param(
    [string]$SolutionPath = "."
)

$ErrorActionPreference = "Continue"

# Result structure
$findings = @()
$errorCount = 0
$warningCount = 0
$infoCount = 0

Write-Host "🔍 Validating .csproj file structure..."
Write-Host "Solution: $SolutionPath"
Write-Host ""

# Find all .csproj files
try {
    $csprojFiles = Get-ChildItem -Path $SolutionPath -Filter "*.csproj" -Recurse -File | Where-Object {
        $_.FullName -notmatch '[\\/](obj|bin)[\\/]'
    }

    Write-Host "Found $($csprojFiles.Count) .csproj files to validate"
    Write-Host ""

    foreach ($csprojFile in $csprojFiles) {
        $relativePath = $csprojFile.FullName.Replace($PWD.Path, "").TrimStart('\', '/')
        Write-Host "Validating: $relativePath"

        # Load XML
        try {
            [xml]$csproj = Get-Content $csprojFile.FullName -ErrorAction Stop
        } catch {
            $findings += @{
                severity = "error"
                category = ".NET Project Structure"
                file = $relativePath
                line = 0
                rule = "CSPROJ-INVALID-XML"
                message = "Failed to parse .csproj file as valid XML"
                remediation = "Fix XML syntax errors in the project file"
            }
            $errorCount++
            continue
        }

        # Check for PackageReference with Version attribute (CPM violation)
        $packageReferences = $csproj.SelectNodes("//PackageReference[@Version]")
        if ($packageReferences.Count -gt 0) {
            foreach ($pkgRef in $packageReferences) {
                $packageName = $pkgRef.GetAttribute("Include")
                $version = $pkgRef.GetAttribute("Version")

                $findings += @{
                    severity = "error"
                    category = ".NET Project Structure"
                    file = $relativePath
                    line = 0
                    rule = "CSPROJ-CPM-VIOLATION"
                    message = "PackageReference '$packageName' has Version attribute - violates Centralized Package Management"
                    remediation = "Remove Version='$version' from PackageReference. Add to Directory.Packages.props instead."
                }
                $errorCount++
            }
        }

        # Check for duplicate PackageReference entries
        $allPackageRefs = $csproj.SelectNodes("//PackageReference")
        $packageNames = @{}
        foreach ($pkgRef in $allPackageRefs) {
            $packageName = $pkgRef.GetAttribute("Include")
            if ($packageNames.ContainsKey($packageName)) {
                $findings += @{
                    severity = "warning"
                    category = ".NET Project Structure"
                    file = $relativePath
                    line = 0
                    rule = "CSPROJ-DUPLICATE-PACKAGE"
                    message = "Duplicate PackageReference for '$packageName'"
                    remediation = "Remove duplicate PackageReference entries"
                }
                $warningCount++
            } else {
                $packageNames[$packageName] = $true
            }
        }

        # Determine if this is a test project
        $isTestProject = $relativePath -match '[\\/]tests[\\/]' -or
                        $csprojFile.Name -match '\.Tests\.' -or
                        $allPackageRefs | Where-Object { $_.GetAttribute("Include") -match '^(MSTest|xUnit|NUnit|Microsoft\.Testing)' }

        # Check test project specific requirements
        if ($isTestProject) {
            # Check for EnableMSTestRunner (if using MSTest)
            $usesMSTest = $allPackageRefs | Where-Object {
                $_.GetAttribute("Include") -match '^(MSTest\.TestFramework|MSTest\.TestAdapter|Microsoft\.Testing\.Platform)'
            }

            if ($usesMSTest) {
                $enableMSTestRunner = $csproj.SelectSingleNode("//PropertyGroup/EnableMSTestRunner")
                if (-not $enableMSTestRunner -or $enableMSTestRunner.InnerText -ne "true") {
                    $findings += @{
                        severity = "warning"
                        category = ".NET Project Structure"
                        file = $relativePath
                        line = 0
                        rule = "CSPROJ-MSTEST-RUNNER"
                        message = "MSTest project missing EnableMSTestRunner=true"
                        remediation = "Add <EnableMSTestRunner>true</EnableMSTestRunner> to PropertyGroup"
                    }
                    $warningCount++
                }

                # Check for OutputType
                $outputType = $csproj.SelectSingleNode("//PropertyGroup/OutputType")
                if (-not $outputType -or $outputType.InnerText -ne "Exe") {
                    $findings += @{
                        severity = "warning"
                        category = ".NET Project Structure"
                        file = $relativePath
                        line = 0
                        rule = "CSPROJ-MSTEST-OUTPUT-TYPE"
                        message = "MSTest project missing OutputType=Exe for Microsoft.Testing.Platform"
                        remediation = "Add <OutputType>Exe</OutputType> to PropertyGroup"
                    }
                    $warningCount++
                }
            }
        }

        # Check for TargetFramework (should be net10.0 or inherited from Directory.Build.props)
        $targetFramework = $csproj.SelectSingleNode("//PropertyGroup/TargetFramework")
        if ($targetFramework -and $targetFramework.InnerText -ne "net10.0") {
            $findings += @{
                severity = "info"
                category = ".NET Project Structure"
                file = $relativePath
                line = 0
                rule = "CSPROJ-TARGET-FRAMEWORK"
                message = "TargetFramework is '$($targetFramework.InnerText)' - expected 'net10.0'"
                remediation = "Verify this is intentional or remove to inherit from Directory.Build.props"
            }
            $infoCount++
        }

        # Check for ImplicitUsings (should be disabled or inherited)
        $implicitUsings = $csproj.SelectSingleNode("//PropertyGroup/ImplicitUsings")
        if ($implicitUsings -and $implicitUsings.InnerText -eq "enable") {
            $findings += @{
                severity = "error"
                category = ".NET Project Structure"
                file = $relativePath
                line = 0
                rule = "CSPROJ-IMPLICIT-USINGS"
                message = "ImplicitUsings is enabled - should be disabled per project standards"
                remediation = "Remove <ImplicitUsings>enable</ImplicitUsings> or set to 'disable'"
            }
            $errorCount++
        }

        # Check for Nullable (should be disable or inherited)
        $nullable = $csproj.SelectSingleNode("//PropertyGroup/Nullable")
        if ($nullable -and $nullable.InnerText -eq "enable") {
            $findings += @{
                severity = "info"
                category = ".NET Project Structure"
                file = $relativePath
                line = 0
                rule = "CSPROJ-NULLABLE"
                message = "Nullable is enabled - project standard is disabled"
                remediation = "Consider removing to inherit from Directory.Build.props (disable)"
            }
            $infoCount++
        }

        # Check for empty PropertyGroups or ItemGroups (cleanup opportunity)
        $emptyGroups = $csproj.SelectNodes("//PropertyGroup[not(*) and not(text()[normalize-space()])]") +
                      $csproj.SelectNodes("//ItemGroup[not(*) and not(text()[normalize-space()])]")
        if ($emptyGroups.Count -gt 0) {
            $findings += @{
                severity = "info"
                category = ".NET Project Structure"
                file = $relativePath
                line = 0
                rule = "CSPROJ-EMPTY-GROUPS"
                message = "Found $($emptyGroups.Count) empty PropertyGroup or ItemGroup elements"
                remediation = "Remove empty elements for cleaner project file"
            }
            $infoCount++
        }
    }

    if ($findings.Count -eq 0) {
        Write-Host ""
        Write-Host "✅ All .csproj files are valid"
    } else {
        Write-Host ""
        Write-Host "Found $($findings.Count) issues in .csproj files"
    }

} catch {
    Write-Host "❌ Error scanning .csproj files: $_"
    $findings += @{
        severity = "error"
        category = ".NET Project Structure"
        file = ""
        line = 0
        rule = "CSPROJ-SCAN-ERROR"
        message = "Failed to scan .csproj files: $_"
        remediation = "Check that project files are accessible"
    }
    $errorCount++
}

# Determine overall status
$status = if ($errorCount -gt 0) { "failed" } elseif ($warningCount -gt 0) { "warning" } else { "pass" }

# Build result object
$result = @{
    step = "csproj-validation"
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
