#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate diagrams from compiled .NET assemblies
.DESCRIPTION
    Automatically generates Mermaid and/or PlantUML class diagrams from compiled .NET assemblies.
    Finds all assemblies in bin/ directories and generates diagrams to specified output path.
.PARAMETER ProjectPath
    Path to project or solution (default: current directory)
.PARAMETER OutputPath
    Output directory for diagrams (default: docs/docfx-developer/diagrams)
.PARAMETER Mermaid
    Generate Mermaid diagrams only
.PARAMETER PlantUML
    Generate PlantUML diagrams only
.PARAMETER All
    Generate all diagram types (default if no type specified)
.EXAMPLE
    pwsh .docgen/diagram-gen.ps1 -All
.EXAMPLE
    pwsh .docgen/diagram-gen.ps1 -Mermaid
.EXAMPLE
    pwsh .docgen/diagram-gen.ps1 -ProjectPath src/Example.Web -OutputPath docs/diagrams
#>

param(
    [string]$ProjectPath = ".",
    [string]$OutputPath = "docs/docfx-developer/diagrams",
    [switch]$Mermaid,
    [switch]$PlantUML,
    [switch]$All
)

# Default to all if no specific type selected
if (-not $Mermaid -and -not $PlantUML) {
    $All = $true
}

Write-Host ""
Write-Host "Diagram Generation Script"
Write-Host "========================="
Write-Host "Project Path: $ProjectPath"
Write-Host "Output Path: $OutputPath"
Write-Host ""

# Create output directory
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    Write-Host "Created output directory: $OutputPath"
}

# Find all assemblies (exclude obj, ref directories)
Write-Host "Searching for assemblies in: $ProjectPath"
$assemblies = Get-ChildItem -Path $ProjectPath -Recurse -Filter "*.dll" -ErrorAction SilentlyContinue |
    Where-Object {
        $_.FullName -notmatch [regex]::Escape([IO.Path]::DirectorySeparatorChar) + "obj" + [regex]::Escape([IO.Path]::DirectorySeparatorChar) -and
        $_.FullName -notmatch [regex]::Escape([IO.Path]::DirectorySeparatorChar) + "ref" + [regex]::Escape([IO.Path]::DirectorySeparatorChar) -and
        $_.FullName -match [regex]::Escape([IO.Path]::DirectorySeparatorChar) + "bin" + [regex]::Escape([IO.Path]::DirectorySeparatorChar)
    }

if ($assemblies.Count -eq 0) {
    Write-Error "No assemblies found in $ProjectPath"
    Write-Host ""
    Write-Host "Build the project first: dotnet build"
    Write-Host ""
    exit 1
}

Write-Host "Found $($assemblies.Count) assemblies:"
foreach ($asm in $assemblies) {
    Write-Host "  - $($asm.Name)"
}
Write-Host ""

$successCount = 0
$failureCount = 0

foreach ($assembly in $assemblies) {
    $assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($assembly.Name)
    Write-Host "Processing: $assemblyName"

    # Generate Mermaid diagram
    if ($Mermaid -or $All) {
        $mermaidOutput = Join-Path $OutputPath "$assemblyName-mermaid.md"
        try {
            dll2mmd $assembly.FullName -o $mermaidOutput 2>&1 | Out-Null
            if (Test-Path $mermaidOutput) {
                Write-Host "   Generated Mermaid: $mermaidOutput" -ForegroundColor Green
                $successCount++
            } else {
                Write-Warning "    Mermaid diagram not created (empty assembly?)"
                $failureCount++
            }
        } catch {
            Write-Warning "   Failed to generate Mermaid diagram: $_"
            $failureCount++
        }
    }

    # Generate PlantUML diagram
    if ($PlantUML -or $All) {
        $pumlOutput = Join-Path $OutputPath "$assemblyName.puml"
        try {
            puml-gen $assembly.FullName -o $pumlOutput 2>&1 | Out-Null
            if (Test-Path $pumlOutput) {
                Write-Host "   Generated PlantUML: $pumlOutput" -ForegroundColor Green
                $successCount++
            } else {
                Write-Warning "    PlantUML diagram not created (empty assembly?)"
                $failureCount++
            }
        } catch {
            Write-Warning "   Failed to generate PlantUML diagram: $_"
            $failureCount++
        }
    }
}

Write-Host ""
Write-Host "Diagram generation complete!"
Write-Host "   Success: $successCount"
if ($failureCount -gt 0) {
    Write-Host "   Failures: $failureCount" -ForegroundColor Yellow
}
Write-Host ""

# List generated files
$generatedFiles = Get-ChildItem -Path $OutputPath -File | Sort-Object LastWriteTime -Descending | Select-Object -First 10
if ($generatedFiles) {
    Write-Host "Recent diagrams in $OutputPath`:"
    foreach ($file in $generatedFiles) {
        $size = [math]::Round($file.Length / 1KB, 2)
        Write-Host "  - $($file.Name) ($size KB)"
    }
}

exit 0
