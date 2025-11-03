# GitHub Scripts Documentation

This directory contains PowerShell scripts used by GitHub Actions workflows for validation and security scanning.

## Scripts Overview

### `format-pr-comment.ps1` - PR Comment Formatter

**Purpose:** Formats validation results as GitHub-flavored Markdown comments

**Usage:**
```powershell
pwsh -File .github/scripts/format-pr-comment.ps1 `
  -InputJson '{"step":"validation","status":"pass","findings":[],"summary":{}}' `
  -StepNumber 3 `
  -StepName "Quality Checks" `
  -Emoji "✨" `
  -RunUrl "https://github.com/org/repo/actions/runs/123456"
```

**Parameters:**
- `InputJson` - JSON string with validation results
- `StepNumber` - Step number (1-6)
- `StepName` - Human-readable step name
- `Emoji` - Emoji for the step header
- `MaxFindings` - Maximum findings to display (default: 100)
- `RunUrl` - Link to GitHub Actions run

**Input JSON Schema:**
```json
{
  "step": "step-name",
  "status": "pass|warning|failed",
  "timestamp": "2025-11-02T10:00:00Z",
  "findings": [
    {
      "severity": "error|warning|info",
      "category": "Category Name",
      "file": "path/to/file.cs",
      "line": 42,
      "rule": "RULE-ID",
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
```

**Output:** GitHub-flavored Markdown with HTML markers for update-in-place

**Features:**
- Groups findings by category
- Truncates at configurable limit
- Color-coded severity badges
- Expandable details sections
- Links to workflow run

---

## Security Scripts

### `check-dotnet-vulnerabilities.ps1` - NuGet Vulnerability Scanner

**Purpose:** Scans .NET projects for vulnerable NuGet packages

**Usage:**
```powershell
pwsh -File .github/scripts/check-dotnet-vulnerabilities.ps1 `
  -SolutionPath "." `
  -IncludeTransitive `
  -MinimumSeverity "Low"
```

**Parameters:**
- `SolutionPath` - Path to solution/directory (default: `.`)
- `IncludeTransitive` - Include indirect dependencies (default: `$true`)
- `MinimumSeverity` - Minimum severity to report: `Low`, `Moderate`, `High`, `Critical`

**How It Works:**
1. Runs `dotnet list package --vulnerable --include-vulnerable`
2. Parses output for vulnerability information
3. Maps severity levels (Critical/High → error, Moderate/Low → warning)
4. Separates direct vs. transitive dependencies
5. Outputs JSON with findings

**Output:**
```json
{
  "step": "nuget-vulnerability-scan",
  "status": "failed|warning|pass",
  "timestamp": "2025-11-02T10:00:00Z",
  "findings": [
    {
      "severity": "error",
      "category": "NuGet Vulnerability (Direct)",
      "file": "src/ClaudeStack.Web/ClaudeStack.Web.csproj",
      "rule": "NUGET-VULN-High",
      "message": "Newtonsoft.Json 12.0.1 has a known High severity vulnerability",
      "remediation": "Update to a non-vulnerable version. See advisory: https://..."
    }
  ],
  "summary": {...}
}
```

**Severity Mapping:**
- **Critical** → Error (blocks PR)
- **High** → Error (blocks PR)
- **Moderate** → Warning
- **Low** → Warning

**Common Vulnerabilities:**
- Deserialization issues
- XML external entity (XXE) attacks
- Regular expression DoS (ReDoS)
- Authentication bypass

---

### `check-path-security.ps1` - Path Security Scanner

**Purpose:** Detects path traversal and file security issues in C# code

**Usage:**
```powershell
pwsh -File .github/scripts/check-path-security.ps1 `
  -SourcePath "." `
  -Exclude @("**/obj/**", "**/bin/**")
```

**Parameters:**
- `SourcePath` - Directory with C# source files (default: `.`)
- `Exclude` - Patterns to exclude (default: obj, bin, wwwroot/lib, Designer.cs)

**Detected Patterns:**

| Pattern | Severity | Rule | Description |
|---------|----------|------|-------------|
| String concatenation for paths | Warning | PATH-CONCAT-* | Should use Path.Combine |
| User input in file paths | Error | UNSAFE-PATH-FROM-REQUEST | Direct use without validation |
| Path traversal sequences (`../`) | Info | PATH-TRAVERSAL-SEQUENCE | Potential security risk |
| Unsafe deserialization | Error | UNSAFE-DESERIALIZATION | BinaryFormatter, etc. |
| Process.Start concatenation | Error | PROCESS-START-CONCAT | Command injection risk |

**Examples:**

```csharp
// ❌ Detected: PATH-CONCAT-FILESTREAM
new FileStream("uploads/" + fileName, FileMode.Create)

// ✅ Fix: Use Path.Combine
new FileStream(Path.Combine("uploads", fileName), FileMode.Create)

// ❌ Detected: UNSAFE-PATH-FROM-REQUEST
var path = Request.Query["file"];
File.ReadAllText(path);

// ✅ Fix: Validate and sanitize
var basePath = Path.GetFullPath("uploads");
var fullPath = Path.GetFullPath(Path.Combine(basePath, fileName));
if (!fullPath.StartsWith(basePath))
    return BadRequest();

// ❌ Detected: UNSAFE-DESERIALIZATION
var formatter = new BinaryFormatter();
var obj = formatter.Deserialize(stream);

// ✅ Fix: Use safe serializer
var obj = JsonSerializer.Deserialize<MyType>(stream);
```

**Output:** JSON with findings (same schema as other validators)

---

## .NET Validation Scripts

### `check-csproj-structure.ps1` - Project File Validator

**Purpose:** Validates .csproj file structure and configuration

**Usage:**
```powershell
pwsh -File .github/scripts/check-csproj-structure.ps1 `
  -SolutionPath "."
```

**Parameters:**
- `SolutionPath` - Path to solution/directory (default: `.`)

**Validations:**

#### Centralized Package Management (CPM)
```xml
<!-- ❌ ERROR: CSPROJ-CPM-VIOLATION -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />

<!-- ✅ CORRECT -->
<PackageReference Include="Newtonsoft.Json" />
<!-- Version defined in Directory.Packages.props -->
```

#### MSTest Configuration
```xml
<!-- ❌ WARNING: Missing for MSTest projects -->
<PropertyGroup>
  <OutputType>Library</OutputType>
</PropertyGroup>

<!-- ✅ CORRECT -->
<PropertyGroup>
  <EnableMSTestRunner>true</EnableMSTestRunner>
  <OutputType>Exe</OutputType>
</PropertyGroup>
```

#### Implicit Usings
```xml
<!-- ❌ ERROR: CSPROJ-IMPLICIT-USINGS -->
<ImplicitUsings>enable</ImplicitUsings>

<!-- ✅ CORRECT -->
<!-- Removed or set to disable (inherited from Directory.Build.props) -->
```

**Additional Checks:**
- Duplicate PackageReference entries
- Empty PropertyGroup/ItemGroup elements
- Target framework consistency
- Valid XML structure

---

### `check-cpm-compliance.ps1` - CPM Validator

**Purpose:** Validates Directory.Packages.props for Centralized Package Management

**Usage:**
```powershell
pwsh -File .github/scripts/check-cpm-compliance.ps1 `
  -SolutionPath "."
```

**Parameters:**
- `SolutionPath` - Path to repository root (default: `.`)

**Validations:**

#### File Existence
```powershell
# ❌ ERROR: CPM-MISSING-FILE
# Directory.Packages.props not found

# ✅ CORRECT: File exists at repository root
```

#### ManagePackageVersionsCentrally
```xml
<!-- ❌ ERROR: CPM-NOT-ENABLED -->
<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>

<!-- ✅ CORRECT -->
<PropertyGroup>
  <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
</PropertyGroup>
```

#### Package Version Format
```xml
<!-- ❌ ERROR: CPM-MISSING-VERSION -->
<PackageVersion Include="Newtonsoft.Json" />

<!-- ✅ CORRECT -->
<PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
```

#### Semantic Versioning
```xml
<!-- ⚠️ WARNING: CPM-INVALID-SEMVER -->
<PackageVersion Include="MyPackage" Version="1.0" />

<!-- ✅ CORRECT -->
<PackageVersion Include="MyPackage" Version="1.0.0" />
<PackageVersion Include="MyPackage" Version="1.0.0-preview.1" />
<PackageVersion Include="MyPackage" Version="1.0.0+build.123" />
```

**Additional Checks:**
- Duplicate package versions
- Wildcard versions (not recommended)
- Cross-reference with .csproj files
- Missing package definitions

---

### `check-global-json.ps1` - SDK Configuration Validator

**Purpose:** Validates global.json for .NET SDK and test runner configuration

**Usage:**
```powershell
pwsh -File .github/scripts/check-global-json.ps1 `
  -SolutionPath "." `
  -ExpectedSdkVersion "10.0.*"
```

**Parameters:**
- `SolutionPath` - Path to repository root (default: `.`)
- `ExpectedSdkVersion` - Expected SDK version pattern (default: `10.0.*`)

**Validations:**

#### SDK Version
```json
// ❌ ERROR: GLOBALJSON-MISSING-SDK-VERSION
{
  "sdk": {}
}

// ✅ CORRECT
{
  "sdk": {
    "version": "10.0.100-rc.2.25502.107",
    "rollForward": "latestFeature"
  }
}
```

#### Test Runner
```json
// ⚠️ WARNING: GLOBALJSON-MISSING-TEST-RUNNER
{
  "sdk": { "version": "10.0.100" }
}

// ✅ CORRECT
{
  "sdk": { "version": "10.0.100" },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

#### RollForward Policy
```json
// ℹ️ INFO: Using default (patch)
{
  "sdk": { "version": "10.0.100" }
}

// ✅ EXPLICIT
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"  // or: patch, minor, major, disable
  }
}
```

**Valid RollForward Values:**
- `patch` - Latest patch version (default)
- `feature` - Latest feature band
- `minor` - Latest minor version
- `major` - Latest major version
- `latestPatch`, `latestFeature`, `latestMinor`, `latestMajor`
- `disable` - Exact version required

---

## Running Scripts Locally

### Prerequisites

```powershell
# Check PowerShell version (7.0+ recommended)
$PSVersionTable.PSVersion

# Install PowerShell 7 (if needed)
# https://aka.ms/powershell
```

### Test Individual Scripts

```powershell
# Navigate to repository root
cd /path/to/net10-project-example

# Run NuGet vulnerability scan
pwsh -File .github/scripts/check-dotnet-vulnerabilities.ps1

# Run path security scan
pwsh -File .github/scripts/check-path-security.ps1 -SourcePath src/

# Run .csproj validation
pwsh -File .github/scripts/check-csproj-structure.ps1

# Run CPM validation
pwsh -File .github/scripts/check-cpm-compliance.ps1

# Run global.json validation
pwsh -File .github/scripts/check-global-json.ps1
```

### Parse JSON Output

```powershell
# Run and parse results
$result = pwsh -File .github/scripts/check-csproj-structure.ps1 | ConvertFrom-Json

# Check status
Write-Host "Status: $($result.status)"
Write-Host "Total findings: $($result.summary.total)"
Write-Host "Errors: $($result.summary.errors)"

# List findings
$result.findings | ForEach-Object {
    Write-Host "[$($_.severity)] $($_.file):$($_.line) - $($_.message)"
}
```

## Error Codes

Scripts exit with standard codes:
- `0` - Success (no errors found)
- `1` - Failure (errors found or script error)

Check exit code:
```powershell
pwsh -File script.ps1
if ($LASTEXITCODE -eq 0) {
    Write-Host "Success"
} else {
    Write-Host "Failed"
}
```

## Customization

### Adjust Severity Levels

Edit script to change severity mappings:

```powershell
# In check-path-security.ps1
# Change from error to warning
$findings += @{
    severity = "warning"  # was "error"
    category = "Path Security"
    # ...
}
```

### Add New Patterns

Add to patterns array:

```powershell
# In check-path-security.ps1
$patterns = @(
    # Existing patterns...
    @{
        Pattern = 'YourCustomPattern'
        Severity = "warning"
        Rule = "CUSTOM-RULE-ID"
        Message = "Your message"
        Remediation = "How to fix"
    }
)
```

### Exclude Files/Directories

```powershell
# Pass exclude patterns
pwsh -File .github/scripts/check-path-security.ps1 `
  -Exclude @("**/obj/**", "**/bin/**", "**/Generated/**")
```

## Troubleshooting

### Script Won't Execute

**Error:** "Execution policy prevents..."

**Fix:**
```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

### JSON Parsing Errors

**Error:** "ConvertFrom-Json: Invalid JSON"

**Debug:**
```powershell
# Capture both stdout and stderr
$output = pwsh -File script.ps1 2>&1
Write-Host $output
```

### PowerShell Version Issues

**Error:** "Parameter set cannot be resolved"

**Fix:** Upgrade to PowerShell 7+
```bash
# Install PowerShell 7
https://aka.ms/powershell
```

## Best Practices

1. **Test locally** before pushing
2. **Review false positives** - not all warnings are issues
3. **Update regularly** - keep patterns current with threats
4. **Custom rules** - add project-specific validations
5. **Document exclusions** - explain why files are excluded

## Further Reading

- [PowerShell Documentation](https://docs.microsoft.com/en-us/powershell/)
- [.NET CLI Reference](https://docs.microsoft.com/en-us/dotnet/core/tools/)
- [OWASP Secure Coding Practices](https://owasp.org/www-project-secure-coding-practices-quick-reference-guide/)

---

**Questions?** See [.github/workflows/README.md](../workflows/README.md) or [CONTRIBUTING.md](../../CONTRIBUTING.md)
