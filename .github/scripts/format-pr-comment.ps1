#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Formats PR validation results as a GitHub-flavored Markdown comment.

.DESCRIPTION
    Takes validation results in JSON format and generates a formatted Markdown comment
    with HTML markers for update-in-place functionality. Supports grouping findings
    by category, truncation of large result sets, and links to workflow runs.

.PARAMETER InputJson
    JSON string containing validation results. Expected structure:
    {
      "step": "step-name",
      "status": "pass|warning|failed",
      "timestamp": "ISO 8601 timestamp",
      "findings": [
        {
          "severity": "error|warning|info",
          "category": "category-name",
          "file": "path/to/file",
          "line": 123,
          "rule": "rule-id",
          "message": "Description of issue",
          "remediation": "How to fix it"
        }
      ],
      "summary": {
        "total": 10,
        "errors": 2,
        "warnings": 5,
        "info": 3
      }
    }

.PARAMETER StepNumber
    Step number (2-6) for the validation pipeline.

.PARAMETER StepName
    Human-readable name for the step (e.g., "PR Guardrails", "Quality Checks").

.PARAMETER Emoji
    Emoji code for the step (e.g., "🛡️", "✨", "🔒").

.PARAMETER MaxFindings
    Maximum number of findings to display before truncating (default: 100).

.PARAMETER RunUrl
    URL to the GitHub Actions workflow run for detailed logs.

.EXAMPLE
    .\format-pr-comment.ps1 -InputJson $json -StepNumber 2 -StepName "PR Guardrails" -Emoji "🛡️" -RunUrl $url

.NOTES
    Author: GitHub Integration Implementation
    Version: 1.0
    Requires: PowerShell 7+
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$InputJson,

    [Parameter(Mandatory = $true)]
    [ValidateRange(2, 6)]
    [int]$StepNumber,

    [Parameter(Mandatory = $true)]
    [string]$StepName,

    [Parameter(Mandatory = $true)]
    [string]$Emoji,

    [Parameter(Mandatory = $false)]
    [int]$MaxFindings = 100,

    [Parameter(Mandatory = $false)]
    [string]$RunUrl = ""
)

# Parse JSON input
try {
    $result = $InputJson | ConvertFrom-Json
}
catch {
    Write-Error "Failed to parse JSON input: $_"
    exit 1
}

# Determine status emoji
$statusEmoji = switch ($result.status) {
    "pass" { "✅" }
    "warning" { "⚠️" }
    "failed" { "❌" }
    default { "❓" }
}

# Build Markdown comment
$markdown = @"
<!-- pr-validation:step-$StepNumber -->

## $Emoji Step $StepNumber`: $StepName $statusEmoji

**Status:** $($result.status.ToUpper())
**Timestamp:** $($result.timestamp)
**Total Findings:** $($result.summary.total) (❌ $($result.summary.errors) errors, ⚠️ $($result.summary.warnings) warnings, ℹ️ $($result.summary.info) info)

"@

# Add findings if present
if ($result.findings -and $result.findings.Count -gt 0) {
    # Group findings by category
    $groupedFindings = $result.findings | Group-Object -Property category

    $truncated = $false
    $displayedCount = 0

    foreach ($group in $groupedFindings) {
        $categoryName = if ($group.Name) { $group.Name } else { "General" }
        $markdown += "`n### 📋 $categoryName`n`n"

        foreach ($finding in $group.Group) {
            # Check if we've reached the max findings limit
            if ($displayedCount -ge $MaxFindings) {
                $truncated = $true
                break
            }

            # Severity icon
            $severityIcon = switch ($finding.severity) {
                "error" { "❌" }
                "warning" { "⚠️" }
                "info" { "ℹ️" }
                default { "•" }
            }

            # Build finding entry
            $markdown += "**$severityIcon $($finding.severity.ToUpper())**"

            if ($finding.file) {
                $fileDisplay = $finding.file
                if ($finding.line) {
                    $fileDisplay += ":$($finding.line)"
                }
                $markdown += " | ``$fileDisplay``"
            }

            if ($finding.rule) {
                $markdown += " | Rule: ``$($finding.rule)``"
            }

            $markdown += "`n"

            if ($finding.message) {
                $markdown += "- **Message:** $($finding.message)`n"
            }

            if ($finding.remediation) {
                $markdown += "- **Remediation:** $($finding.remediation)`n"
            }

            $markdown += "`n"
            $displayedCount++
        }

        if ($truncated) {
            break
        }
    }

    # Add truncation notice if needed
    if ($truncated) {
        $remaining = $result.summary.total - $displayedCount
        $markdown += @"

---

**⚠️ Results Truncated**
Showing $displayedCount of $($result.summary.total) findings.
$remaining additional finding(s) not shown.

"@
    }
}
else {
    # No findings - success message
    $markdown += @"

### ✨ All Checks Passed

No issues found in this step.

"@
}

# Add workflow run link if provided
if ($RunUrl) {
    $markdown += @"

---

📊 [View detailed logs in GitHub Actions]($RunUrl)

"@
}

# Add footer with timestamp
$markdown += @"

---

*Last updated: $($result.timestamp)*

<!-- /pr-validation:step-$StepNumber -->
"@

# Output the formatted markdown
Write-Output $markdown
