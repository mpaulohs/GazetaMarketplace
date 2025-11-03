# Phase 2: Core Foundation Setup

**Estimated Time**: 6-8 hours
**Status**: NOT STARTED
**Dependencies**: Phase 1 complete

---

## Overview

**Goal**: Establish platform-agnostic infrastructure that will support all documentation types and both GitHub and Azure DevOps platforms.

**Why This Phase Matters**: This phase creates the foundation that enables platform portability, automated diagram generation, and cross-platform builds. By investing in a platform-agnostic core now, we avoid duplication and enable seamless platform switching later.

---

## Tasks

### Task 2.1: Enable XML Documentation Generation (Small - 15 minutes)

**Files**: `Directory.Build.props`

**Purpose**: Enable XML documentation file generation for all C# projects, which DocFX will use to generate API reference documentation.

**Acceptance Criteria**:
- [ ] Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` to Directory.Build.props
- [ ] Add `<NoWarn>$(NoWarn);CS1591</NoWarn>` to suppress missing comment warnings initially
- [ ] Build succeeds without errors: `dotnet build`
- [ ] XML files generated in `bin/` directories for all projects
- [ ] Verify XML files exist: `find src -name "*.xml"`

**Implementation Steps**:
```xml
<!-- Add to Directory.Build.props, inside existing <PropertyGroup> -->
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<NoWarn>$(NoWarn);CS1591</NoWarn>
```

**Why CS1591 Suppression**: Initially, the codebase has zero XML comments. CS1591 warns about missing documentation. We suppress it initially to prevent build noise, but will remove this suppression in Phase 6 when PR validation enforces documentation.

**Dependencies**: None

**Effort**: Small (15 minutes)

---

### Task 2.2: Create `.docgen/` Directory Structure (Small - 30 minutes)

**Directory**: `.docgen/`

**Purpose**: Create the platform-agnostic core directory with stubbed-out scripts that will be implemented throughout this phase.

**Acceptance Criteria**:
- [ ] Directory `.docgen/` created
- [ ] All required files created (can be empty or minimal stubs initially):
  - `platform-config.json`
  - `mcp-config.json`
  - `diagram-gen.ps1`
  - `validate-docs.ps1`
  - `detect-platform.ps1`
  - `switch-platform.ps1`
  - `setup-mcp.ps1`
  - `wiki-sync.ps1`
- [ ] README.md in `.docgen/` explaining the purpose of each script

**Implementation Steps**:
```bash
mkdir -p .docgen
cd .docgen

# Create stub files
touch platform-config.json
touch mcp-config.json
touch diagram-gen.ps1
touch validate-docs.ps1
touch detect-platform.ps1
touch switch-platform.ps1
touch setup-mcp.ps1
touch wiki-sync.ps1
touch README.md
```

**README.md Content** (minimal):
```markdown
# .docgen - Platform-Agnostic Documentation Automation

This directory contains scripts and configuration for cross-platform documentation automation.

## Files

- **platform-config.json**: Platform selection and configuration
- **mcp-config.json**: MCP server configuration for AI assistance
- **diagram-gen.ps1**: Automated diagram generation from assemblies
- **validate-docs.ps1**: Documentation validation (XML comments, markdown)
- **detect-platform.ps1**: Detect current CI/CD platform
- **switch-platform.ps1**: Switch between GitHub and Azure DevOps
- **setup-mcp.ps1**: Configure MCP servers for Claude Code
- **wiki-sync.ps1**: Sync wiki content to GitHub/Azure DevOps Wiki
```

**Dependencies**: None

**Effort**: Small (30 minutes)

---

### Task 2.3: Create Platform Configuration (Medium - 1 hour)

**File**: `.docgen/platform-config.json`

**Purpose**: Define configuration for both GitHub and Azure DevOps platforms, including deployment targets, workflow paths, and feature flags.

**Acceptance Criteria**:
- [ ] JSON structure for GitHub and Azure DevOps platforms
- [ ] Deployment targets configured (URLs, paths)
- [ ] Workflow/pipeline paths specified
- [ ] Feature flags for each platform (wiki support, static site hosting)
- [ ] Default platform set to "auto" (detect from git remote)
- [ ] Valid JSON (test with `cat platform-config.json | jq .`)

**Implementation**:
```json
{
  "defaultPlatform": "auto",
  "platforms": {
    "github": {
      "enabled": true,
      "workflowsPath": ".github/workflows",
      "scriptsPath": ".github/scripts",
      "deploymentTargets": {
        "developerDocs": {
          "type": "github-pages",
          "branch": "gh-pages",
          "path": "/",
          "url": "https://notmyself.github.io/net10-project-example/"
        },
        "userDocs": {
          "type": "github-pages",
          "branch": "gh-pages",
          "path": "/user/",
          "url": "https://notmyself.github.io/net10-project-example/user/"
        },
        "companyDocs": {
          "type": "github-wiki",
          "repository": "NotMyself/net10-project-example.wiki",
          "url": "https://github.com/NotMyself/net10-project-example/wiki"
        }
      },
      "features": {
        "prValidation": true,
        "automaticDeployment": true,
        "wikiSync": true,
        "diagramGeneration": true
      }
    },
    "azuredevops": {
      "enabled": false,
      "pipelinesPath": ".azuredevops/pipelines",
      "scriptsPath": ".azuredevops/scripts",
      "deploymentTargets": {
        "developerDocs": {
          "type": "azure-static-web-app",
          "resourceGroup": "docs-rg",
          "appName": "net10-docs-developer",
          "url": "https://net10-docs.azurestaticapps.net/"
        },
        "userDocs": {
          "type": "azure-static-web-app",
          "resourceGroup": "docs-rg",
          "appName": "net10-docs-user",
          "url": "https://net10-docs-user.azurestaticapps.net/"
        },
        "companyDocs": {
          "type": "azure-devops-wiki",
          "project": "net10-project-example",
          "wikiName": "net10-project-example.wiki",
          "url": "https://dev.azure.com/org/net10-project-example/_wiki"
        }
      },
      "features": {
        "prValidation": true,
        "automaticDeployment": true,
        "wikiSync": true,
        "diagramGeneration": true
      }
    }
  },
  "sharedConfig": {
    "docfxVersion": "latest",
    "diagramTools": ["dll2mmd", "PlantUmlClassDiagramGenerator"],
    "markdownlintConfig": ".markdownlint.json",
    "validateOnPR": true
  }
}
```

**Dependencies**: Task 2.2 (directory structure exists)

**Effort**: Medium (1 hour)

---

### Task 2.4: Create MCP Server Configuration (Medium - 1.5 hours)

**Files**: `.docgen/mcp-config.json`, `.docgen/setup-mcp.ps1`

**Purpose**: Configure MCP servers for AI-assisted documentation and create a setup script that merges this configuration into Claude Code's config.

**Acceptance Criteria**:
- [ ] `mcp-config.json` defines Microsoft Learn, Docs MCP, Context7 servers
- [ ] `setup-mcp.ps1` detects Windows/Linux environment
- [ ] `setup-mcp.ps1` sets platform-specific paths for Claude Code config
- [ ] `setup-mcp.ps1` merges MCP config into Claude Code config (~/.config/claude/config.json or AppData)
- [ ] Script tested on WSL2
- [ ] Script includes rollback capability (backup original config)

**Implementation - mcp-config.json**:
```json
{
  "mcpServers": {
    "microsoft-learn": {
      "command": "npx",
      "args": ["-y", "@microsoft/mcp-server-learn"],
      "description": "Microsoft Learn documentation server for .NET and Azure",
      "enabled": true
    },
    "docs-mcp": {
      "command": "npx",
      "args": ["-y", "docs-mcp-server"],
      "description": "General documentation context server",
      "enabled": false
    },
    "context7": {
      "command": "npx",
      "args": ["-y", "context7-mcp"],
      "description": "Context7 MCP server for code understanding",
      "enabled": false
    }
  }
}
```

**Implementation - setup-mcp.ps1**:
```powershell
#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Configure MCP servers for Claude Code
.DESCRIPTION
    Merges .docgen/mcp-config.json into Claude Code's configuration file.
    Detects platform (Windows/Linux) and locates appropriate config path.
.EXAMPLE
    pwsh .docgen/setup-mcp.ps1
#>

param(
    [switch]$Rollback
)

# Detect platform and config path
$configPath = if ($IsWindows -or $env:OS -eq "Windows_NT") {
    "$env:APPDATA\Claude\config.json"
} else {
    "$HOME/.config/claude/config.json"
}

Write-Host "Claude Code config path: $configPath"

# Backup existing config
if (Test-Path $configPath) {
    $backupPath = "$configPath.backup.$(Get-Date -Format 'yyyyMMddHHmmss')"
    Copy-Item $configPath $backupPath
    Write-Host "Backed up existing config to: $backupPath"
}

if ($Rollback) {
    $latestBackup = Get-ChildItem "$configPath.backup.*" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($latestBackup) {
        Copy-Item $latestBackup.FullName $configPath -Force
        Write-Host "Restored config from: $($latestBackup.FullName)"
    } else {
        Write-Error "No backup found to restore"
    }
    exit
}

# Load configs
$claudeConfig = if (Test-Path $configPath) {
    Get-Content $configPath | ConvertFrom-Json
} else {
    @{}
}

$mcpConfig = Get-Content ".docgen/mcp-config.json" | ConvertFrom-Json

# Merge MCP servers
if (-not $claudeConfig.mcpServers) {
    $claudeConfig | Add-Member -MemberType NoteProperty -Name "mcpServers" -Value @{}
}

foreach ($server in $mcpConfig.mcpServers.PSObject.Properties) {
    $claudeConfig.mcpServers | Add-Member -MemberType NoteProperty -Name $server.Name -Value $server.Value -Force
}

# Save merged config
$claudeConfig | ConvertTo-Json -Depth 10 | Set-Content $configPath

Write-Host "MCP servers configured successfully!"
Write-Host "Restart Claude Code to apply changes."
```

**Dependencies**: Task 2.2 (directory structure exists)

**Effort**: Medium (1.5 hours)

---

### Task 2.5: Create Cross-Platform Makefile (Medium - 2 hours)

**File**: `Makefile`

**Purpose**: Provide cross-platform build automation for documentation, diagrams, validation, and deployment.

**Acceptance Criteria**:
- [ ] Platform detection (Linux/macOS/Windows)
- [ ] Common targets: `docs-build`, `docs-serve`, `docs-clean`, `diagrams`, `validate`
- [ ] `deploy` target with CI/CD platform detection (GitHub Actions vs Azure DevOps)
- [ ] Works on WSL2 and Windows
- [ ] `make docs-build` successfully builds all documentation
- [ ] `make help` shows all available targets

**Implementation**:
```makefile
# Platform-Agnostic Documentation Makefile

.PHONY: help docs-build docs-serve docs-clean diagrams validate deploy

# Detect platform
UNAME_S := $(shell uname -s)
ifeq ($(UNAME_S),Linux)
    PLATFORM := linux
endif
ifeq ($(UNAME_S),Darwin)
    PLATFORM := macos
endif
ifeq ($(OS),Windows_NT)
    PLATFORM := windows
endif

# Tool commands
DOCFX := docfx
PWSH := pwsh
DOTNET := dotnet

help:
	@echo "Available targets:"
	@echo "  docs-build          Build all documentation"
	@echo "  docs-developer      Build developer documentation only"
	@echo "  docs-user           Build user documentation only"
	@echo "  docs-serve          Serve documentation locally"
	@echo "  docs-clean          Clean generated documentation"
	@echo "  diagrams            Generate diagrams from assemblies"
	@echo "  validate            Validate documentation quality"
	@echo "  deploy              Deploy documentation (CI/CD only)"

docs-build: docs-developer docs-user

docs-developer:
	@echo "Building developer documentation..."
	cd docs/docfx-developer && $(DOCFX) build

docs-user:
	@echo "Building user documentation..."
	cd docs/docfx-user && $(DOCFX) build

docs-serve:
	@echo "Serving documentation at http://localhost:8080"
	cd docs/docfx-developer && $(DOCFX) serve _site

docs-user-serve:
	@echo "Serving user documentation at http://localhost:8081"
	cd docs/docfx-user && $(DOCFX) serve _site --port 8081

docs-clean:
	@echo "Cleaning documentation..."
	rm -rf docs/docfx-developer/_site
	rm -rf docs/docfx-developer/api
	rm -rf docs/docfx-user/_site

diagrams:
	@echo "Generating diagrams..."
	$(PWSH) .docgen/diagram-gen.ps1 -All

validate:
	@echo "Validating documentation..."
	$(PWSH) .docgen/validate-docs.ps1

deploy:
	@echo "Deploying documentation..."
	@if [ "$$GITHUB_ACTIONS" = "true" ]; then \
		echo "Detected GitHub Actions"; \
		$(PWSH) .github/scripts/deploy.ps1; \
	elif [ "$$TF_BUILD" = "True" ]; then \
		echo "Detected Azure DevOps"; \
		$(PWSH) .azuredevops/scripts/deploy.ps1; \
	else \
		echo "Not running in CI/CD, skipping deployment"; \
	fi
```

**Dependencies**: Tasks 2.1-2.4 (tools will be installed in subsequent tasks)

**Effort**: Medium (2 hours)

---

### Task 2.6: Install DocFX Globally (Small - 5 minutes)

**Command**: `dotnet tool install -g docfx`

**Purpose**: Install DocFX as a global .NET tool so it's accessible from any directory.

**Acceptance Criteria**:
- [ ] DocFX installed globally
- [ ] `docfx --version` works and shows version
- [ ] Accessible from PATH in all terminal sessions

**Implementation**:
```bash
# Install DocFX
dotnet tool install -g docfx

# Verify installation
docfx --version

# Expected output: "docfx X.Y.Z"
```

**Troubleshooting**:
- If `docfx` command not found, ensure `~/.dotnet/tools` is in PATH
- On WSL2: Add to `~/.bashrc` or `~/.zshrc`: `export PATH="$PATH:$HOME/.dotnet/tools"`
- On Windows: Tools path usually added automatically

**Dependencies**: None (.NET SDK already installed)

**Effort**: Small (5 minutes)

---

### Task 2.7: Install Diagram Generation Tools (Small - 15 minutes)

**Commands**:
```bash
dotnet tool install -g dll2mmd
dotnet tool install -g PlantUmlClassDiagramGenerator
```

**Purpose**: Install tools for automated diagram generation from compiled assemblies.

**Acceptance Criteria**:
- [ ] `dll2mmd` installed globally
- [ ] `PlantUmlClassDiagramGenerator` installed globally (command: `puml-gen`)
- [ ] Both tools accessible from PATH
- [ ] Test generation on Example.Web.dll works

**Implementation**:
```bash
# Install diagram generators
dotnet tool install -g dll2mmd
dotnet tool install -g PlantUmlClassDiagramGenerator

# Verify installations
dll2mmd --version
puml-gen --version

# Test on compiled assembly
dotnet build src/Example.Web/Example.Web.csproj
dll2mmd src/Example.Web/bin/Debug/net10.0/Example.Web.dll -o test-diagram.md

# Check output
cat test-diagram.md
rm test-diagram.md
```

**Troubleshooting**:
- Ensure assemblies are compiled before running diagram generation
- dll2mmd generates Mermaid syntax markdown files
- puml-gen generates PlantUML files (.puml)

**Dependencies**: None (.NET SDK already installed)

**Effort**: Small (15 minutes)

---

### Task 2.8: Create Diagram Generation Script (Medium - 2 hours)

**File**: `.docgen/diagram-gen.ps1`

**Purpose**: Automate diagram generation from compiled assemblies for both Mermaid and PlantUML formats.

**Acceptance Criteria**:
- [ ] Accepts parameters: `-ProjectPath`, `-OutputPath`, `-Mermaid`, `-PlantUML`, `-All`
- [ ] Finds all compiled assemblies (excluding `obj/`, `ref/` directories)
- [ ] Generates Mermaid class diagrams with dll2mmd
- [ ] Generates PlantUML diagrams with puml-gen
- [ ] Works on both WSL2 and Windows
- [ ] Called from Makefile `diagrams` target
- [ ] Handles errors gracefully (missing assemblies, tool failures)

**Implementation**:
```powershell
#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate diagrams from compiled .NET assemblies
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

# Create output directory
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

# Find all assemblies (exclude obj, ref directories)
$assemblies = Get-ChildItem -Path $ProjectPath -Recurse -Filter "*.dll" |
    Where-Object {
        $_.FullName -notmatch "\\obj\\" -and
        $_.FullName -notmatch "\\ref\\" -and
        $_.FullName -match "\\bin\\"
    }

if ($assemblies.Count -eq 0) {
    Write-Error "No assemblies found. Build the project first: dotnet build"
    exit 1
}

Write-Host "Found $($assemblies.Count) assemblies"

foreach ($assembly in $assemblies) {
    $assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($assembly.Name)
    Write-Host "Processing: $assemblyName"

    # Generate Mermaid diagram
    if ($Mermaid -or $All) {
        $mermaidOutput = Join-Path $OutputPath "$assemblyName-mermaid.md"
        try {
            dll2mmd $assembly.FullName -o $mermaidOutput
            Write-Host "  Generated Mermaid: $mermaidOutput"
        } catch {
            Write-Warning "  Failed to generate Mermaid diagram: $_"
        }
    }

    # Generate PlantUML diagram
    if ($PlantUML -or $All) {
        $pumlOutput = Join-Path $OutputPath "$assemblyName.puml"
        try {
            puml-gen $assembly.FullName -o $pumlOutput
            Write-Host "  Generated PlantUML: $pumlOutput"
        } catch {
            Write-Warning "  Failed to generate PlantUML diagram: $_"
        }
    }
}

Write-Host "Diagram generation complete!"
```

**Dependencies**: Task 2.7 (diagram tools installed)

**Effort**: Medium (2 hours)

---

## Phase Completion Criteria

Phase 2 is complete when:

- [ ] All tools installed and verified (DocFX, dll2mmd, puml-gen)
- [ ] `.docgen/` directory with all scripts created
- [ ] Makefile works: `make docs-build` executes without errors
- [ ] Diagram generation works: `make diagrams` generates diagrams
- [ ] XML documentation enabled in Directory.Build.props
- [ ] MCP server configuration created (script ready for Phase 8)
- [ ] Platform configuration JSON is valid and complete

---

## Success Indicators

You'll know this phase is successful when:

- `make help` shows all available targets
- `make diagrams` generates Mermaid and PlantUML diagrams from assemblies
- XML files appear in `bin/` directories after `dotnet build`
- `.docgen/` directory has all required scripts
- `docfx --version`, `dll2mmd --version`, `puml-gen --version` all work

---

## Common Issues and Solutions

1. **DocFX not in PATH**: Add `~/.dotnet/tools` to PATH
2. **Assemblies not found**: Run `dotnet build` before diagram generation
3. **PowerShell script execution policy**: On Windows, run `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`
4. **MCP config merge fails**: Check JSON syntax in mcp-config.json with `jq`

---

**Phase Status**: NOT STARTED
**Next Task**: Task 2.1 - Enable XML Documentation Generation
**Estimated Completion**: After 6-8 hours of focused work
