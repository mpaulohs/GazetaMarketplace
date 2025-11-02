#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates global.json for .NET SDK and test runner configuration.

.DESCRIPTION
    Checks global.json file for:
    - File exists at repository root
    - Valid JSON structure
    - SDK version is specified
    - SDK version matches expected .NET 10 RC/release
    - Test runner is configured (Microsoft.Testing.Platform)
    - RollForward policy is appropriate
    - No deprecated properties

.PARAMETER SolutionPath
    Path to the solution or repository root (default: current directory).

.PARAMETER ExpectedSdkVersion
    Expected SDK version (default: 10.0.*). Use wildcard for flexibility.

.OUTPUTS
    JSON object with findings array containing validation issue details.
#>

param(
    [string]$SolutionPath = ".",
    [string]$ExpectedSdkVersion = "10.0.*"
)

$ErrorActionPreference = "Continue"

# Result structure
$findings = @()
$errorCount = 0
$warningCount = 0
$infoCount = 0

Write-Host "🔍 Validating global.json configuration..."
Write-Host "Solution: $SolutionPath"
Write-Host "Expected SDK: $ExpectedSdkVersion"
Write-Host ""

$globalJsonPath = Join-Path $SolutionPath "global.json"

# Check if global.json exists
if (-not (Test-Path $globalJsonPath)) {
    $findings += @{
        severity = "warning"
        category = ".NET Configuration"
        file = "global.json"
        line = 0
        rule = "GLOBALJSON-MISSING"
        message = "global.json not found at repository root"
        remediation = "Create global.json to pin SDK version and configure test runner"
    }
    $warningCount++

    # Early exit - can't validate further without the file
    $status = "warning"
    $result = @{
        step = "global-json-validation"
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
    exit 0
}

Write-Host "Found global.json"

# Load and validate JSON
try {
    $globalJson = Get-Content $globalJsonPath -Raw | ConvertFrom-Json -ErrorAction Stop
} catch {
    $findings += @{
        severity = "error"
        category = ".NET Configuration"
        file = "global.json"
        line = 0
        rule = "GLOBALJSON-INVALID-JSON"
        message = "Failed to parse global.json as valid JSON: $_"
        remediation = "Fix JSON syntax errors in global.json"
    }
    $errorCount++

    $status = "failed"
    $result = @{
        step = "global-json-validation"
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

# Check for sdk section
if (-not $globalJson.sdk) {
    $findings += @{
        severity = "error"
        category = ".NET Configuration"
        file = "global.json"
        line = 0
        rule = "GLOBALJSON-MISSING-SDK"
        message = "global.json missing 'sdk' section"
        remediation = "Add 'sdk' section with 'version' property"
    }
    $errorCount++
} else {
    # Check SDK version
    if (-not $globalJson.sdk.version) {
        $findings += @{
            severity = "error"
            category = ".NET Configuration"
            file = "global.json"
            line = 0
            rule = "GLOBALJSON-MISSING-SDK-VERSION"
            message = "global.json sdk section missing 'version' property"
            remediation = "Add 'version' property to sdk section (e.g., '10.0.100-rc.2.25502.107')"
        }
        $errorCount++
    } else {
        $sdkVersion = $globalJson.sdk.version
        Write-Host "SDK Version: $sdkVersion"

        # Check if version matches expected pattern
        if ($ExpectedSdkVersion -match '\*') {
            $pattern = $ExpectedSdkVersion -replace '\*', '.*'
            if ($sdkVersion -notmatch "^$pattern") {
                $findings += @{
                    severity = "warning"
                    category = ".NET Configuration"
                    file = "global.json"
                    line = 0
                    rule = "GLOBALJSON-SDK-VERSION-MISMATCH"
                    message = "SDK version '$sdkVersion' does not match expected pattern '$ExpectedSdkVersion'"
                    remediation = "Verify SDK version is correct for .NET 10"
                }
                $warningCount++
            }
        } else {
            if ($sdkVersion -ne $ExpectedSdkVersion) {
                $findings += @{
                    severity = "warning"
                    category = ".NET Configuration"
                    file = "global.json"
                    line = 0
                    rule = "GLOBALJSON-SDK-VERSION-MISMATCH"
                    message = "SDK version '$sdkVersion' does not match expected '$ExpectedSdkVersion'"
                    remediation = "Update SDK version or adjust expected version"
                }
                $warningCount++
            }
        }
    }

    # Check rollForward policy
    if ($globalJson.sdk.rollForward) {
        $rollForward = $globalJson.sdk.rollForward
        Write-Host "RollForward Policy: $rollForward"

        $validPolicies = @("patch", "feature", "minor", "major", "latestPatch", "latestFeature", "latestMinor", "latestMajor", "disable")
        if ($rollForward -notin $validPolicies) {
            $findings += @{
                severity = "warning"
                category = ".NET Configuration"
                file = "global.json"
                line = 0
                rule = "GLOBALJSON-INVALID-ROLLFORWARD"
                message = "Invalid rollForward value: '$rollForward'"
                remediation = "Use one of: $($validPolicies -join ', ')"
            }
            $warningCount++
        }

        # Info about rollForward implications
        if ($rollForward -eq "disable") {
            $findings += @{
                severity = "info"
                category = ".NET Configuration"
                file = "global.json"
                line = 0
                rule = "GLOBALJSON-ROLLFORWARD-DISABLED"
                message = "rollForward is disabled - exact SDK version required"
                remediation = "Consider 'latestPatch' or 'latestFeature' for CI/CD flexibility"
            }
            $infoCount++
        }
    } else {
        # Default rollForward is 'patch'
        $findings += @{
            severity = "info"
            category = ".NET Configuration"
            file = "global.json"
            line = 0
            rule = "GLOBALJSON-NO-ROLLFORWARD"
            message = "rollForward not specified - defaults to 'patch'"
            remediation = "Consider explicitly setting rollForward policy for clarity"
        }
        $infoCount++
    }

    # Check for allowPrerelease (deprecated in .NET 7+)
    if ($globalJson.sdk.allowPrerelease) {
        $findings += @{
            severity = "warning"
            category = ".NET Configuration"
            file = "global.json"
            line = 0
            rule = "GLOBALJSON-DEPRECATED-PROPERTY"
            message = "Property 'allowPrerelease' is deprecated (use rollForward instead)"
            remediation = "Remove 'allowPrerelease' and use 'rollForward' policy"
        }
        $warningCount++
    }
}

# Check for test runner configuration (Microsoft.Testing.Platform)
if (-not $globalJson.test) {
    $findings += @{
        severity = "warning"
        category = ".NET Configuration"
        file = "global.json"
        line = 0
        rule = "GLOBALJSON-MISSING-TEST"
        message = "global.json missing 'test' section - test runner not configured"
        remediation = "Add 'test' section with 'runner': 'Microsoft.Testing.Platform' for MSTest projects"
    }
    $warningCount++
} else {
    if (-not $globalJson.test.runner) {
        $findings += @{
            severity = "warning"
            category = ".NET Configuration"
            file = "global.json"
            line = 0
            rule = "GLOBALJSON-MISSING-TEST-RUNNER"
            message = "global.json test section missing 'runner' property"
            remediation = "Add 'runner': 'Microsoft.Testing.Platform'"
        }
        $warningCount++
    } else {
        $testRunner = $globalJson.test.runner
        Write-Host "Test Runner: $testRunner"

        if ($testRunner -ne "Microsoft.Testing.Platform") {
            $findings += @{
                severity = "info"
                category = ".NET Configuration"
                file = "global.json"
                line = 0
                rule = "GLOBALJSON-UNEXPECTED-TEST-RUNNER"
                message = "Test runner is '$testRunner' - expected 'Microsoft.Testing.Platform'"
                remediation = "Verify this is the intended test runner for the project"
            }
            $infoCount++
        }
    }
}

# Check for msbuild-sdks (optional but good to know about)
if ($globalJson.'msbuild-sdks') {
    $sdkCount = ($globalJson.'msbuild-sdks'.PSObject.Properties).Count
    Write-Host "MSBuild SDKs: $sdkCount configured"

    $findings += @{
        severity = "info"
        category = ".NET Configuration"
        file = "global.json"
        line = 0
        rule = "GLOBALJSON-MSBUILD-SDKS"
        message = "global.json defines $sdkCount MSBuild SDK(s)"
        remediation = "Ensure all referenced MSBuild SDKs are necessary and versions are current"
    }
    $infoCount++
}

# Summary
Write-Host ""
if ($findings.Count -eq 0) {
    Write-Host "✅ global.json is valid and properly configured"
} else {
    Write-Host "Found $($findings.Count) items in global.json"
}

# Determine overall status
$status = if ($errorCount -gt 0) { "failed" } elseif ($warningCount -gt 0) { "warning" } else { "pass" }

# Build result object
$result = @{
    step = "global-json-validation"
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
