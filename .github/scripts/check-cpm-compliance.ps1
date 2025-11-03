#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates Directory.Packages.props for Centralized Package Management compliance.

.DESCRIPTION
    Checks Directory.Packages.props file for:
    - File exists at repository root
    - ManagePackageVersionsCentrally is enabled
    - All PackageVersion entries are properly formatted
    - No duplicate package versions
    - Package versions follow semantic versioning
    - Cross-references with .csproj files to find missing packages

.PARAMETER SolutionPath
    Path to the solution or repository root (default: current directory).

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

Write-Host "🔍 Validating Directory.Packages.props (Centralized Package Management)..."
Write-Host "Solution: $SolutionPath"
Write-Host ""

$directoryPackagesPath = Join-Path $SolutionPath "Directory.Packages.props"

# Check if Directory.Packages.props exists
if (-not (Test-Path $directoryPackagesPath)) {
    $findings += @{
        severity = "error"
        category = "Centralized Package Management"
        file = "Directory.Packages.props"
        line = 0
        rule = "CPM-MISSING-FILE"
        message = "Directory.Packages.props not found at repository root"
        remediation = "Create Directory.Packages.props to enable Centralized Package Management"
    }
    $errorCount++

    # Early exit - can't validate further without the file
    $status = "failed"
    $result = @{
        step = "cpm-validation"
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
    $json = $result | ConvertTo-Json -Depth 10 -Compress
    Write-Output $json
    exit 1
}

Write-Host "Found Directory.Packages.props"

# Load and validate XML
try {
    [xml]$packageProps = Get-Content $directoryPackagesPath -ErrorAction Stop
} catch {
    $findings += @{
        severity = "error"
        category = "Centralized Package Management"
        file = "Directory.Packages.props"
        line = 0
        rule = "CPM-INVALID-XML"
        message = "Failed to parse Directory.Packages.props as valid XML"
        remediation = "Fix XML syntax errors in Directory.Packages.props"
    }
    $errorCount++

    $status = "failed"
    $result = @{
        step = "cpm-validation"
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
    $json = $result | ConvertTo-Json -Depth 10 -Compress
    Write-Output $json
    exit 1
}

# Check for ManagePackageVersionsCentrally
$manageVersions = $packageProps.SelectSingleNode("//PropertyGroup/ManagePackageVersionsCentrally")
if (-not $manageVersions) {
    $findings += @{
        severity = "error"
        category = "Centralized Package Management"
        file = "Directory.Packages.props"
        line = 0
        rule = "CPM-NOT-ENABLED"
        message = "ManagePackageVersionsCentrally property not found"
        remediation = "Add <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally> to PropertyGroup"
    }
    $errorCount++
} elseif ($manageVersions.InnerText -ne "true") {
    $findings += @{
        severity = "error"
        category = "Centralized Package Management"
        file = "Directory.Packages.props"
        line = 0
        rule = "CPM-NOT-ENABLED"
        message = "ManagePackageVersionsCentrally is set to '$($manageVersions.InnerText)' - should be 'true'"
        remediation = "Set <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>"
    }
    $errorCount++
}

# Get all PackageVersion entries
$packageVersions = $packageProps.SelectNodes("//ItemGroup/PackageVersion")
Write-Host "Found $($packageVersions.Count) PackageVersion entries"

# Track package names and versions
$packages = @{}
$duplicatePackages = @()

foreach ($pkgVer in $packageVersions) {
    $packageName = $pkgVer.GetAttribute("Include")
    $version = $pkgVer.GetAttribute("Version")

    # Check for missing Include attribute
    if ([string]::IsNullOrWhiteSpace($packageName)) {
        $findings += @{
            severity = "error"
            category = "Centralized Package Management"
            file = "Directory.Packages.props"
            line = 0
            rule = "CPM-MISSING-PACKAGE-NAME"
            message = "PackageVersion entry missing Include attribute"
            remediation = "Add Include attribute with package name"
        }
        $errorCount++
        continue
    }

    # Check for missing Version attribute
    if ([string]::IsNullOrWhiteSpace($version)) {
        $findings += @{
            severity = "error"
            category = "Centralized Package Management"
            file = "Directory.Packages.props"
            line = 0
            rule = "CPM-MISSING-VERSION"
            message = "PackageVersion '$packageName' missing Version attribute"
            remediation = "Add Version attribute with semantic version"
        }
        $errorCount++
        continue
    }

    # Check for duplicate packages
    if ($packages.ContainsKey($packageName)) {
        $duplicatePackages += $packageName
        $findings += @{
            severity = "error"
            category = "Centralized Package Management"
            file = "Directory.Packages.props"
            line = 0
            rule = "CPM-DUPLICATE-PACKAGE"
            message = "Duplicate PackageVersion entry for '$packageName'"
            remediation = "Remove duplicate entry - keep only one version definition"
        }
        $errorCount++
    } else {
        $packages[$packageName] = $version
    }

    # Validate semantic versioning format (basic check)
    if ($version -notmatch '^\d+\.\d+\.\d+(\.\d+)?(-[a-zA-Z0-9\-\.]+)?(\+[a-zA-Z0-9\-\.]+)?$') {
        $findings += @{
            severity = "warning"
            category = "Centralized Package Management"
            file = "Directory.Packages.props"
            line = 0
            rule = "CPM-INVALID-SEMVER"
            message = "PackageVersion '$packageName' has non-standard version format: '$version'"
            remediation = "Use semantic versioning format (e.g., 1.0.0, 1.0.0-preview, 1.0.0+build)"
        }
        $warningCount++
    }

    # Check for wildcard versions (not recommended)
    if ($version -match '[\*\+]' -and $version -notmatch '\+[a-zA-Z0-9\-\.]+$') {
        $findings += @{
            severity = "warning"
            category = "Centralized Package Management"
            file = "Directory.Packages.props"
            line = 0
            rule = "CPM-WILDCARD-VERSION"
            message = "PackageVersion '$packageName' uses wildcard version: '$version'"
            remediation = "Pin to specific version for reproducible builds"
        }
        $warningCount++
    }
}

# Cross-reference with .csproj files to find packages not in CPM
Write-Host ""
Write-Host "Cross-referencing with .csproj files..."

$csprojFiles = Get-ChildItem -Path $SolutionPath -Filter "*.csproj" -Recurse -File | Where-Object {
    $_.FullName -notmatch '[\\/](obj|bin)[\\/]'
}

$allReferencedPackages = @{}
foreach ($csprojFile in $csprojFiles) {
    try {
        [xml]$csproj = Get-Content $csprojFile.FullName -ErrorAction Stop
        $packageRefs = $csproj.SelectNodes("//PackageReference")

        foreach ($pkgRef in $packageRefs) {
            $pkgName = $pkgRef.GetAttribute("Include")
            if (-not [string]::IsNullOrWhiteSpace($pkgName)) {
                if (-not $allReferencedPackages.ContainsKey($pkgName)) {
                    $allReferencedPackages[$pkgName] = @()
                }
                $relativePath = $csprojFile.FullName.Replace($PWD.Path, "").TrimStart('\', '/')
                $allReferencedPackages[$pkgName] += $relativePath
            }
        }
    } catch {
        # Skip invalid project files (already caught by csproj validator)
    }
}

# Check for packages used in projects but not defined in Directory.Packages.props
foreach ($pkgName in $allReferencedPackages.Keys) {
    if (-not $packages.ContainsKey($pkgName)) {
        $projectList = $allReferencedPackages[$pkgName] -join ", "
        $findings += @{
            severity = "warning"
            category = "Centralized Package Management"
            file = "Directory.Packages.props"
            line = 0
            rule = "CPM-MISSING-PACKAGE-VERSION"
            message = "Package '$pkgName' used in projects but not defined in Directory.Packages.props"
            remediation = "Add <PackageVersion Include=`"$pkgName`" Version=`"x.y.z`" /> to Directory.Packages.props. Used in: $projectList"
        }
        $warningCount++
    }
}

# Summary
Write-Host ""
if ($findings.Count -eq 0) {
    Write-Host "✅ Directory.Packages.props is valid and complete"
} else {
    Write-Host "Found $($findings.Count) issues in Directory.Packages.props"
}

# Determine overall status
$status = if ($errorCount -gt 0) { "failed" } elseif ($warningCount -gt 0) { "warning" } else { "pass" }

# Build result object
$result = @{
    step = "cpm-validation"
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
