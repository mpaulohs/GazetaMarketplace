#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Detects potential path traversal and file security issues in C# code.

.DESCRIPTION
    Scans C# source files for patterns that may indicate path traversal vulnerabilities,
    unsafe file operations, or improper path handling. Identifies risky patterns like:
    - Direct string concatenation for paths (instead of Path.Combine)
    - User input used in file paths without validation
    - Unsafe deserialization patterns
    - Potential path traversal sequences (../, ..\)

.PARAMETER SourcePath
    Path to the directory containing C# source files (default: current directory).

.PARAMETER Exclude
    Patterns to exclude from scanning (e.g., "**/obj/**", "**/bin/**").

.OUTPUTS
    JSON object with findings array containing security issue details.
#>

param(
    [string]$SourcePath = ".",
    [string[]]$Exclude = @("**/obj/**", "**/bin/**", "**/wwwroot/lib/**", "**/*.Designer.cs")
)

$ErrorActionPreference = "Continue"

# Result structure
$findings = @()
$errorCount = 0
$warningCount = 0
$infoCount = 0

Write-Host "🔍 Scanning for path security issues in C# code..."
Write-Host "Source: $SourcePath"
Write-Host ""

# Security patterns to detect
$patterns = @(
    @{
        Pattern = 'new\s+FileStream\s*\([^)]*\+[^)]*\)'
        Severity = "warning"
        Rule = "PATH-CONCAT-FILESTREAM"
        Message = "FileStream path created with string concatenation - use Path.Combine"
        Remediation = "Use Path.Combine() or Path.Join() to safely construct file paths"
    },
    @{
        Pattern = 'new\s+StreamWriter\s*\([^)]*\+[^)]*\)'
        Severity = "warning"
        Rule = "PATH-CONCAT-STREAMWRITER"
        Message = "StreamWriter path created with string concatenation - use Path.Combine"
        Remediation = "Use Path.Combine() or Path.Join() to safely construct file paths"
    },
    @{
        Pattern = 'File\.(ReadAllText|ReadAllLines|ReadAllBytes|WriteAllText|WriteAllLines|WriteAllBytes|Delete|Move|Copy)\s*\([^)]*\+[^)]*\)'
        Severity = "warning"
        Rule = "PATH-CONCAT-FILE-IO"
        Message = "File I/O path created with string concatenation - use Path.Combine"
        Remediation = "Use Path.Combine() or Path.Join() to safely construct file paths"
    },
    @{
        Pattern = 'Directory\.(CreateDirectory|Delete|Move|GetFiles|GetDirectories)\s*\([^)]*\+[^)]*\)'
        Severity = "warning"
        Rule = "PATH-CONCAT-DIR-IO"
        Message = "Directory path created with string concatenation - use Path.Combine"
        Remediation = "Use Path.Combine() or Path.Join() to safely construct file paths"
    },
    @{
        Pattern = '\.\.[\\/]|\.\.\\\\|\.\./'
        Severity = "info"
        Rule = "PATH-TRAVERSAL-SEQUENCE"
        Message = "Path traversal sequence detected (../or ..\) in code"
        Remediation = "Ensure path traversal sequences are properly validated if from user input"
    },
    @{
        Pattern = 'Path\s*=\s*Request\.(Query|Form|Headers)\['
        Severity = "error"
        Rule = "UNSAFE-PATH-FROM-REQUEST"
        Message = "File path constructed directly from user input without validation"
        Remediation = "Validate and sanitize user input. Use Path.GetFullPath() and check against allowed directory"
    },
    @{
        Pattern = 'BinaryFormatter|ObjectStateFormatter|LosFormatter|NetDataContractSerializer'
        Severity = "error"
        Rule = "UNSAFE-DESERIALIZATION"
        Message = "Unsafe deserialization formatter detected - known security vulnerability"
        Remediation = "Use JSON.NET, System.Text.Json, or other safe serializers instead"
    },
    @{
        Pattern = '(fileName|filePath|path)\s*=\s*.*Request\.'
        Severity = "warning"
        Rule = "PATH-FROM-USER-INPUT"
        Message = "File path may be constructed from user input"
        Remediation = "Validate user input, use allowlists, and verify paths stay within allowed directories"
    },
    @{
        Pattern = 'Process\.Start\s*\([^)]*\+[^)]*\)'
        Severity = "error"
        Rule = "PROCESS-START-CONCAT"
        Message = "Process.Start with concatenated arguments - potential command injection"
        Remediation = "Use ProcessStartInfo with separate FileName and Arguments properties"
    }
)

# Find all C# files
try {
    $csFiles = Get-ChildItem -Path $SourcePath -Filter "*.cs" -Recurse -File | Where-Object {
        $filePath = $_.FullName
        $shouldExclude = $false
        foreach ($pattern in $Exclude) {
            if ($filePath -like $pattern) {
                $shouldExclude = $true
                break
            }
        }
        -not $shouldExclude
    }

    Write-Host "Found $($csFiles.Count) C# files to scan"
    Write-Host ""

    foreach ($file in $csFiles) {
        $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
        if (-not $content) { continue }

        $lines = $content -split "`n"
        $lineNumber = 0

        foreach ($line in $lines) {
            $lineNumber++

            foreach ($patternDef in $patterns) {
                if ($line -match $patternDef.Pattern) {
                    # Map severity
                    switch ($patternDef.Severity) {
                        "error"   { $errorCount++ }
                        "warning" { $warningCount++ }
                        "info"    { $infoCount++ }
                    }

                    # Get relative path
                    $relativePath = $file.FullName.Replace($PWD.Path, "").TrimStart('\', '/')

                    $findings += @{
                        severity = $patternDef.Severity
                        category = "Path Security"
                        file = $relativePath
                        line = $lineNumber
                        rule = $patternDef.Rule
                        message = $patternDef.Message
                        remediation = $patternDef.Remediation
                    }

                    Write-Host "  $($patternDef.Severity.ToUpper()): $relativePath:$lineNumber - $($patternDef.Rule)"
                }
            }
        }
    }

    if ($findings.Count -eq 0) {
        Write-Host "✅ No path security issues found"
    } else {
        Write-Host ""
        Write-Host "Found $($findings.Count) potential security issues"
    }

} catch {
    Write-Host "❌ Error scanning files: $_"
    $findings += @{
        severity = "error"
        category = "Path Security Scan"
        file = ""
        line = 0
        rule = "PATH-SCAN-ERROR"
        message = "Failed to scan for path security issues: $_"
        remediation = "Check that source files are accessible"
    }
    $errorCount++
}

# Determine overall status
$status = if ($errorCount -gt 0) { "failed" } elseif ($warningCount -gt 0) { "warning" } else { "pass" }

# Build result object
$result = @{
    step = "path-security-scan"
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
