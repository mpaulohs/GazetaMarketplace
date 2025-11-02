# Phase 7: Azure DevOps Plugin Implementation (OPTIONAL)

**Estimated Time**: 6-8 hours
**Status**: NOT STARTED
**Dependencies**: Phase 6 complete, Azure subscription required

---

## Overview

**Goal**: Implement complete Azure DevOps automation for all 3 documentation types (OPTIONAL phase for client flexibility).

**Why This Phase Matters**: Enables consultants to work in Azure DevOps environments. Once implemented, switching between GitHub and Azure DevOps becomes simple, allowing work with any client platform.

**Note**: This phase is OPTIONAL. GitHub implementation (Phases 1-6) provides complete, working documentation system.

---

## Prerequisites

- Azure subscription (free tier available)
- Azure CLI installed (`az`)
- Azure DevOps organization and project
- Permissions to create pipelines and Azure resources

---

## Tasks

### Task 7.1: Create Azure Resources (Medium - 1 hour)

**Purpose**: Create Azure Static Web Apps for hosting developer and user documentation.

**Acceptance Criteria**:
- [ ] Azure Resource Group created
- [ ] Azure Static Web App created for developer docs
- [ ] Azure Static Web App created for user docs (or use subdomain of first SWA)
- [ ] Deployment tokens retrieved and stored as Azure DevOps secrets
- [ ] Resource names documented in `.docgen/platform-config.json`

**Implementation**:
```bash
# Login to Azure
az login

# Create resource group
az group create --name docs-rg --location eastus

# Create Static Web App for developer docs
az staticwebapp create \
  --name net10-docs-developer \
  --resource-group docs-rg \
  --location eastus \
  --sku Free

# Create Static Web App for user docs (optional, or use /user/ path)
az staticwebapp create \
  --name net10-docs-user \
  --resource-group docs-rg \
  --location eastus \
  --sku Free

# Get deployment tokens
az staticwebapp secrets list \
  --name net10-docs-developer \
  --resource-group docs-rg \
  --query "properties.apiKey" -o tsv

az staticwebapp secrets list \
  --name net10-docs-user \
  --resource-group docs-rg \
  --query "properties.apiKey" -o tsv

# Get hostnames
az staticwebapp show \
  --name net10-docs-developer \
  --resource-group docs-rg \
  --query "defaultHostname" -o tsv

# Update platform-config.json with URLs
```

**Store Secrets in Azure DevOps**:
1. Go to Project Settings → Pipelines → Service connections
2. Create new service connection: Azure Resource Manager
3. Store deployment tokens as pipeline variables

---

### Task 7.2: Create Azure Pipelines Directory (Small - 15 minutes)

**Directory**: `.azuredevops/pipelines/`

**Acceptance Criteria**:
- [ ] Directory created: `.azuredevops/pipelines/`
- [ ] Pipeline files stubbed out:
  - `docs-developer-deploy.yml`
  - `docs-user-deploy.yml`
  - `docs-wiki-deploy.yml`
  - `docs-pr-validation.yml`
- [ ] Scripts directory: `.azuredevops/scripts/`
- [ ] PowerShell scripts stubbed:
  - `ado-deploy-developer.ps1`
  - `ado-deploy-user.ps1`
  - `ado-wiki-publish.ps1`

---

### Task 7.3: Create Developer Docs Pipeline (Large - 2.5 hours)

**File**: `.azuredevops/pipelines/docs-developer-deploy.yml`

**Purpose**: Build and deploy developer docs to Azure Static Web Apps on push to main.

**Acceptance Criteria**:
- [ ] Triggers on push to main, paths: `docs/docfx-developer/**`, `src/**`
- [ ] Uses windows-latest pool (DocFX compatibility)
- [ ] Installs .NET SDK from global.json
- [ ] Installs DocFX via Chocolatey
- [ ] Installs diagram generators
- [ ] Runs `dotnet restore` and `dotnet build`
- [ ] Generates diagrams
- [ ] Builds DocFX documentation
- [ ] Publishes build artifact
- [ ] Deploy stage to Azure Static Web Apps using AzureStaticWebApp@0 task

**Implementation**:
```yaml
trigger:
  branches:
    include:
      - main
  paths:
    include:
      - docs/docfx-developer/**
      - src/**
      - .azuredevops/pipelines/docs-developer-deploy.yml

pool:
  vmImage: 'windows-latest'

variables:
  - group: docs-deployment-secrets

stages:
  - stage: Build
    displayName: 'Build Developer Documentation'
    jobs:
      - job: BuildDocs
        displayName: 'Build DocFX Site'
        steps:
          - checkout: self
            fetchDepth: 1

          - task: UseDotNet@2
            displayName: 'Install .NET SDK'
            inputs:
              packageType: 'sdk'
              useGlobalJson: true
              workingDirectory: '$(Build.SourcesDirectory)'

          - script: choco install docfx -y
            displayName: 'Install DocFX'

          - script: |
              dotnet tool install -g dll2mmd
              dotnet tool install -g PlantUmlClassDiagramGenerator
            displayName: 'Install Diagram Tools'

          - script: dotnet restore
            displayName: 'Restore NuGet Packages'

          - script: dotnet build --no-restore
            displayName: 'Build Projects'

          - pwsh: .docgen/diagram-gen.ps1 -All
            displayName: 'Generate Diagrams'

          - script: docfx build docs/docfx-developer/docfx.json
            displayName: 'Build Developer Documentation'

          - task: PublishBuildArtifacts@1
            displayName: 'Publish Artifact'
            inputs:
              PathtoPublish: 'docs/docfx-developer/_site'
              ArtifactName: 'developer-docs'

  - stage: Deploy
    displayName: 'Deploy to Azure Static Web Apps'
    dependsOn: Build
    condition: succeeded()
    jobs:
      - deployment: DeployDocs
        displayName: 'Deploy Developer Docs'
        environment: 'production'
        strategy:
          runOnce:
            deploy:
              steps:
                - task: AzureStaticWebApp@0
                  inputs:
                    app_location: '$(Pipeline.Workspace)/developer-docs'
                    api_location: ''
                    output_location: ''
                    azure_static_web_apps_api_token: '$(AZURE_STATIC_WEB_APPS_API_TOKEN_DEVELOPER)'
```

---

### Task 7.4: Create User Docs Pipeline (Medium - 1.5 hours)

**File**: `.azuredevops/pipelines/docs-user-deploy.yml`

**Purpose**: Build and deploy user docs to Azure Static Web Apps.

**Acceptance Criteria**:
- [ ] Similar to Task 7.3 structure
- [ ] Builds user docs (`docs/docfx-user/`)
- [ ] Deploys to separate Azure Static Web App OR subdomain of developer SWA

**Implementation**: Similar to Task 7.3, but:
- Change paths to `docs/docfx-user/**`
- Build `docs/docfx-user/docfx.json`
- Use different deployment token (`$(AZURE_STATIC_WEB_APPS_API_TOKEN_USER)`)

---

### Task 7.5: Create Wiki Deployment Pipeline (Large - 3 hours)

**File**: `.azuredevops/pipelines/docs-wiki-deploy.yml`

**Purpose**: Publish wiki content to Azure DevOps Wiki using REST API.

**Acceptance Criteria**:
- [ ] Triggers on push to main, paths: `docs/wiki/**`
- [ ] Uses ubuntu-latest pool
- [ ] Authenticates with System.AccessToken
- [ ] Calls Azure DevOps Wiki REST API
- [ ] Creates/updates wiki pages
- [ ] PowerShell script: `.azuredevops/scripts/ado-wiki-publish.ps1`
- [ ] Handles wiki creation if doesn't exist

**Implementation - Pipeline**:
```yaml
trigger:
  branches:
    include:
      - main
  paths:
    include:
      - docs/wiki/**
      - .azuredevops/pipelines/docs-wiki-deploy.yml

pool:
  vmImage: 'ubuntu-latest'

variables:
  - name: System.Debug
    value: true

steps:
  - checkout: self
    fetchDepth: 1

  - pwsh: |
      .azuredevops/scripts/ado-wiki-publish.ps1 `
        -Organization "$(System.TeamFoundationCollectionUri)" `
        -Project "$(System.TeamProject)" `
        -WikiName "$(System.TeamProject).wiki" `
        -SourcePath "docs/wiki" `
        -AccessToken "$(System.AccessToken)"
    displayName: 'Publish Wiki Content'
    env:
      AZURE_DEVOPS_EXT_PAT: $(System.AccessToken)
```

**Implementation - PowerShell Script** (`.azuredevops/scripts/ado-wiki-publish.ps1`):
```powershell
#!/usr/bin/env pwsh
param(
    [Parameter(Mandatory)]
    [string]$Organization,

    [Parameter(Mandatory)]
    [string]$Project,

    [Parameter(Mandatory)]
    [string]$WikiName,

    [Parameter(Mandatory)]
    [string]$SourcePath,

    [Parameter(Mandatory)]
    [string]$AccessToken
)

$ErrorActionPreference = "Stop"

# Base API URL
$baseUrl = "$Organization$Project/_apis/wiki/wikis"
$apiVersion = "7.1-preview.2"

# Headers
$headers = @{
    Authorization = "Bearer $AccessToken"
    "Content-Type" = "application/json"
}

# Get or create wiki
Write-Host "Checking for wiki: $WikiName"
$wikirUrl = "$baseUrl/$WikiName/?api-version=$apiVersion"
try {
    $wiki = Invoke-RestMethod -Uri $wikiUrl -Headers $headers -Method Get
    Write-Host "Wiki exists: $($wiki.name)"
} catch {
    Write-Host "Wiki does not exist, creating..."
    $createBody = @{
        name = $WikiName
        projectId = $env:SYSTEM_TEAMPROJECTID
        type = "projectWiki"
    } | ConvertTo-Json

    $wiki = Invoke-RestMethod -Uri "$baseUrl/?api-version=$apiVersion" -Headers $headers -Method Post -Body $createBody
    Write-Host "Wiki created: $($wiki.name)"
}

# Publish wiki pages
$wikiPages = Get-ChildItem -Path $SourcePath -Filter "*.md"
foreach ($page in $wikiPages) {
    $pageName = [System.IO.Path]::GetFileNameWithoutExtension($page.Name)
    $content = Get-Content $page.FullName -Raw

    Write-Host "Publishing page: $pageName"

    $pageUrl = "$baseUrl/$($wiki.id)/pages?path=$pageName&api-version=$apiVersion"
    $pageBody = @{
        content = $content
    } | ConvertTo-Json

    try {
        Invoke-RestMethod -Uri $pageUrl -Headers $headers -Method Put -Body $pageBody
        Write-Host "  ✅ Published: $pageName"
    } catch {
        Write-Warning "  ❌ Failed to publish: $pageName - $_"
    }
}

Write-Host "Wiki publishing complete!"
```

---

### Task 7.6: Create PR Validation Pipeline (Medium - 1.5 hours)

**File**: `.azuredevops/pipelines/docs-pr-validation.yml`

**Purpose**: Validate documentation in pull requests.

**Acceptance Criteria**:
- [ ] Triggers on PR, paths: `docs/**`, `src/**/*.cs`
- [ ] Checks XML documentation
- [ ] Runs markdownlint
- [ ] Builds all DocFX documentation (validation)
- [ ] Configured as build validation policy

**Implementation**: Similar to GitHub PR validation workflow, but using Azure Pipelines syntax.

---

### Task 7.7: Configure Branch Policies (Small - 15 minutes)

**Manual Step**: Azure DevOps UI

**Acceptance Criteria**:
- [ ] Navigate to Repos → Branches → main → Branch policies
- [ ] Add build validation policy pointing to `docs-pr-validation.yml`
- [ ] Require approvals: 1 (optional)
- [ ] Check for linked work items (optional)

**Steps**:
1. Go to Repos → Branches
2. Click "..." on `main` branch → Branch policies
3. Scroll to "Build validation"
4. Click "+ Add build policy"
5. Select `docs-pr-validation` pipeline
6. Set trigger: Automatic
7. Click "Save"

---

### Task 7.8: Test Azure DevOps Workflow (Medium - 1.5 hours)

**Action**: Create test PR and merge to verify end-to-end flow.

**Acceptance Criteria**:
- [ ] Make documentation change
- [ ] Create PR in Azure DevOps
- [ ] PR validation runs and passes
- [ ] Merge PR
- [ ] Developer docs pipeline runs and deploys to Azure SWA
- [ ] User docs pipeline runs and deploys
- [ ] Wiki pipeline runs and publishes to Azure DevOps Wiki
- [ ] Docs visible at Azure Static Web Apps URL
- [ ] Wiki visible in Azure DevOps (Project → Overview → Wiki)

---

## Phase Completion Criteria

Phase 7 is complete when (if implemented):

- [ ] Azure resources created (Resource Group, Static Web Apps)
- [ ] All Azure Pipelines working and triggered correctly
- [ ] Docs deployed to Azure Static Web Apps successfully
- [ ] Wiki published to Azure DevOps Wiki via REST API
- [ ] Branch policies configured and enforcing PR validation
- [ ] Full end-to-end test passes

---

## Success Indicators

You'll know this phase is successful when:

- Azure DevOps platform works as seamlessly as GitHub
- Can switch between platforms without code changes (only config)
- Client can choose GitHub or Azure DevOps based on their preference
- All documentation types deploy automatically in Azure DevOps environment

---

**Phase Status**: NOT STARTED (OPTIONAL)
**Next Task**: Task 7.1 - Create Azure Resources
**Estimated Completion**: After 6-8 hours of focused work
