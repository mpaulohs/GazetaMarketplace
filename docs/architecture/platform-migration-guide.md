# Platform Migration Guide

This guide walks through migrating the AI-assisted documentation system between GitHub and Azure DevOps platforms.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [GitHub to Azure DevOps Migration](#github-to-azure-devops-migration)
- [Azure DevOps to GitHub Migration](#azure-devops-to-github-migration)
- [Testing After Migration](#testing-after-migration)
- [Troubleshooting](#troubleshooting)
- [Rollback Procedures](#rollback-procedures)

## Overview

The documentation system is designed to be platform-agnostic, with a core implementation in `.docgen/` and platform-specific plugins. Migrating between platforms involves:

1. **Updating configuration files** - Switch git URLs and platform settings
2. **Managing CI/CD workflows** - Enable/disable platform-specific automation
3. **Migrating repository content** - Move code and documentation to new platform
4. **Configuring platform services** - Set up Pages/Wiki/Pipelines on target platform

**Migration Time Estimate:**
- Configuration switch: 5 minutes (automated)
- Repository migration: 15-30 minutes (manual)
- Platform setup: 30-60 minutes (manual)
- Testing and validation: 30-60 minutes
- **Total: 1.5-3 hours**

## Prerequisites

### Required Tools

```bash
# Git 2.30+
git --version

# PowerShell 7.0+ (pwsh)
pwsh --version

# DocFX 2.76+
docfx --version

# .NET SDK 10.0 RC 2+
dotnet --version
```

### Required Access

**For GitHub:**
- Repository admin access
- GitHub Pages enabled
- GitHub Actions enabled (if using automation)

**For Azure DevOps:**
- Project admin access
- Azure Repos access
- Azure Pipelines enabled (if using automation)
- Azure Wiki enabled (if using company docs)

### Backup Current State

Before migrating, create a backup:

```bash
# Create backup branch
git checkout -b backup-before-migration
git push origin backup-before-migration

# Export current documentation builds
make docs-all
zip -r docs-backup-$(date +%Y%m%d).zip docs/docfx-developer/_site docs/docfx-user/_site docs/wiki
```

## GitHub to Azure DevOps Migration

### Step 1: Prepare Azure DevOps

**1.1 Create Azure DevOps Project**

```bash
# If using Azure DevOps CLI
az devops configure --defaults organization=https://dev.azure.com/YourOrg
az devops project create --name "NET10-Project-Example" --description "AI-assisted .NET 10 example project"
```

Or create via web UI:
1. Navigate to https://dev.azure.com/YourOrg
2. Click **+ New project**
3. Enter project name: "NET10-Project-Example"
4. Set visibility: Private or Public
5. Click **Create**

**1.2 Initialize Git Repository**

```bash
# In Azure DevOps project
1. Go to Repos ’ Files
2. Click "Initialize" or import from GitHub
3. Note the repository URL: https://dev.azure.com/YourOrg/YourProject/_git/YourRepo
```

### Step 2: Update Platform Configuration

**2.1 Edit Platform Config**

Edit `.docgen/platform-config.json`:

```json
{
  "defaultPlatform": "AzureDevOps",
  "azureDevOps": {
    "organization": "YourOrg",
    "project": "NET10-Project-Example",
    "repository": "net10-project-example",
    "gitBaseUrl": "https://dev.azure.com/YourOrg/NET10-Project-Example/_git/net10-project-example",
    "gitEditUrl": "https://dev.azure.com/YourOrg/NET10-Project-Example/_git/net10-project-example?path="
  }
}
```

**2.2 Run Platform Switch Script**

```bash
# Switch to Azure DevOps platform
pwsh .docgen/switch-platform.ps1 -Platform AzureDevOps

# Output:
# [1/4] Updated platform-config.json
# [2/4] Updated DocFX configuration files
# [3/4] Disabled GitHub Actions workflows
# [4/4] Platform switch complete!
```

**2.3 Verify Changes**

```bash
# Check modified files
git status

# Should show:
# modified:   .docgen/platform-config.json
# modified:   docs/docfx-developer/docfx.json
# modified:   docs/docfx-user/docfx.json
# renamed:    .github/workflows/* -> .github/workflows.disabled/*
```

### Step 3: Migrate Repository

**3.1 Add Azure DevOps Remote**

```bash
# Add new remote
git remote add azure https://dev.azure.com/YourOrg/YourProject/_git/YourRepo

# Or replace origin
git remote set-url origin https://dev.azure.com/YourOrg/YourProject/_git/YourRepo

# Verify remotes
git remote -v
```

**3.2 Push to Azure DevOps**

```bash
# Commit platform switch changes
git add .
git commit -m "Migrate to Azure DevOps platform

- Updated platform configuration to Azure DevOps
- Updated DocFX git URLs
- Disabled GitHub Actions workflows
- Ready for Azure Pipelines setup"

# Push all branches
git push azure --all

# Push all tags
git push azure --tags
```

### Step 4: Configure Azure DevOps Services

**4.1 Create Azure Pipelines**

Create `.azure-pipelines/docs-developer-deploy.yml`:

```yaml
trigger:
  branches:
    include:
      - main
  paths:
    include:
      - docs/docfx-developer/**
      - src/**

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: UseDotNet@2
    inputs:
      version: '10.0.x'
      includePreviewVersions: true

  - script: |
      dotnet tool install -g docfx
      docfx build docs/docfx-developer/docfx.json
    displayName: 'Build Developer Documentation'

  - task: PublishBuildArtifacts@1
    inputs:
      pathToPublish: 'docs/docfx-developer/_site'
      artifactName: 'developer-docs'
```

**4.2 Enable Azure Wiki**

1. Navigate to **Overview ’ Wiki**
2. Click **Create Wiki**
3. Select **Publish code as wiki**
4. Choose branch: `main`
5. Choose folder: `/docs/wiki`
6. Click **Publish**

**4.3 Configure Branch Policies**

```bash
# Using Azure DevOps CLI
az repos policy create --repository-id <repo-id> \
  --branch main \
  --policy-type required-reviewers \
  --policy-configuration '{
    "minimumApproverCount": 1,
    "creatorVoteCounts": false,
    "allowDownvotes": false,
    "resetOnSourcePush": true
  }'
```

Or via web UI:
1. Go to **Repos ’ Branches**
2. Click **...** next to `main` ’ **Branch policies**
3. Enable **Require a minimum number of reviewers**
4. Set minimum reviewers: 1
5. Enable **Build validation** (add your pipelines)

### Step 5: Test Migration

```bash
# 1. Build documentation locally
make docs-all

# 2. Verify git URLs in DocFX output
grep -r "dev.azure.com" docs/docfx-developer/_site

# 3. Create test PR in Azure DevOps
git checkout -b test-migration
echo "Test migration" >> README.md
git add README.md
git commit -m "Test: Verify Azure DevOps integration"
git push azure test-migration

# Create PR via web UI and verify:
# - Branch policies work
# - Build pipelines trigger
# - Wiki is accessible
```

### Step 6: Decommission GitHub (Optional)

If fully migrating away from GitHub:

```bash
# 1. Archive GitHub repository
# Via web UI: Settings ’ Archive this repository

# 2. Add migration notice to GitHub README
# Create a redirect notice pointing to Azure DevOps

# 3. Keep GitHub backup for 90 days before deleting
```

## Azure DevOps to GitHub Migration

### Step 1: Prepare GitHub

**1.1 Create GitHub Repository**

```bash
# Using GitHub CLI
gh repo create NotMyself/net10-project-example \
  --public \
  --description "AI-assisted .NET 10 example project" \
  --clone

# Or create via web UI
# https://github.com/new
```

**1.2 Initialize Repository**

```bash
# If created via CLI, repository is already cloned
cd net10-project-example

# If created via web UI
git clone https://github.com/NotMyself/net10-project-example.git
cd net10-project-example
```

### Step 2: Update Platform Configuration

**2.1 Edit Platform Config**

Edit `.docgen/platform-config.json`:

```json
{
  "defaultPlatform": "GitHub",
  "github": {
    "owner": "NotMyself",
    "repository": "net10-project-example",
    "gitBaseUrl": "https://github.com/NotMyself/net10-project-example",
    "gitEditUrl": "https://github.com/NotMyself/net10-project-example/blob"
  }
}
```

**2.2 Run Platform Switch Script**

```bash
# Switch to GitHub platform
pwsh .docgen/switch-platform.ps1 -Platform GitHub

# Output:
# [1/4] Updated platform-config.json
# [2/4] Updated DocFX configuration files
# [3/4] Enabled GitHub Actions workflows
# [4/4] Platform switch complete!
```

**2.3 Verify Changes**

```bash
# Check modified files
git status

# Should show:
# modified:   .docgen/platform-config.json
# modified:   docs/docfx-developer/docfx.json
# modified:   docs/docfx-user/docfx.json
# renamed:    .github/workflows.disabled/* -> .github/workflows/*
```

### Step 3: Migrate Repository

**3.1 Add GitHub Remote**

```bash
# Add new remote
git remote add github https://github.com/NotMyself/net10-project-example.git

# Or replace origin
git remote set-url origin https://github.com/NotMyself/net10-project-example.git

# Verify remotes
git remote -v
```

**3.2 Push to GitHub**

```bash
# Commit platform switch changes
git add .
git commit -m "Migrate to GitHub platform

- Updated platform configuration to GitHub
- Updated DocFX git URLs
- Enabled GitHub Actions workflows
- Ready for GitHub Pages setup"

# Push all branches
git push github --all

# Push all tags
git push github --tags
```

### Step 4: Configure GitHub Services

**4.1 Enable GitHub Pages**

Via web UI:
1. Navigate to **Settings ’ Pages**
2. Source: **Deploy from a branch**
3. Branch: **gh-pages**
4. Folder: **/ (root)**
5. Click **Save**

Via CLI:
```bash
gh api repos/NotMyself/net10-project-example/pages \
  -X POST \
  -F source[branch]=gh-pages \
  -F source[path]=/
```

**4.2 Configure Branch Protection**

```bash
# Using GitHub CLI
gh api repos/NotMyself/net10-project-example/branches/main/protection \
  -X PUT \
  -F required_status_checks[strict]=true \
  -F required_status_checks[contexts][]=build \
  -F required_status_checks[contexts][]=test \
  -F required_pull_request_reviews[required_approving_review_count]=1 \
  -F enforce_admins=true
```

Or via web UI:
1. Go to **Settings ’ Branches**
2. Click **Add rule**
3. Branch name pattern: `main`
4. Enable **Require pull request reviews before merging**
5. Enable **Require status checks to pass before merging**
6. Add status checks: build, test
7. Click **Create**

**4.3 Enable GitHub Wiki**

1. Navigate to **Settings ’ Features**
2. Enable **Wikis**
3. Go to **Wiki** tab
4. Click **Create the first page**
5. Content will sync from `.github/workflows/docs-wiki-sync.yml`

**4.4 Configure GitHub Actions Secrets (Optional)**

If using Claude Code review:

```bash
# Get Claude Code OAuth token
claude auth token

# Add secret via CLI
gh secret set CLAUDE_CODE_OAUTH_TOKEN

# Or via web UI
# Settings ’ Secrets and variables ’ Actions ’ New repository secret
```

### Step 5: Test Migration

```bash
# 1. Build documentation locally
make docs-all

# 2. Verify git URLs in DocFX output
grep -r "github.com" docs/docfx-developer/_site

# 3. Create test PR
git checkout -b test-migration
echo "Test migration" >> README.md
git add README.md
git commit -m "Test: Verify GitHub integration"
git push github test-migration

# Create PR
gh pr create --title "Test: Verify GitHub integration" --body "Testing migration"

# Verify:
# - Branch protection works
# - GitHub Actions workflows trigger
# - PR validation steps pass
# - GitHub Pages deploys
# - GitHub Wiki syncs
```

### Step 6: Decommission Azure DevOps (Optional)

If fully migrating away from Azure DevOps:

```bash
# 1. Archive project via web UI
# Project Settings ’ Overview ’ Archive project

# 2. Export work items if needed
az boards query --wiql "SELECT [System.Id] FROM WorkItems" --output json > work-items-backup.json

# 3. Keep Azure DevOps project for 90 days before deleting
```

## Testing After Migration

### Comprehensive Test Checklist

**Documentation Builds:**
- [ ] Developer docs build locally (`make docs-developer`)
- [ ] User docs build locally (`make docs-user`)
- [ ] Wiki files are valid markdown
- [ ] All Mermaid diagrams render correctly
- [ ] API reference generates from XML comments
- [ ] Cross-references and links work

**Git Integration:**
- [ ] "Edit this page" links point to correct platform
- [ ] Git URLs match platform configuration
- [ ] Branch links work in documentation
- [ ] Contribution links work

**CI/CD Workflows:**
- [ ] Workflows trigger on documentation changes
- [ ] Workflows trigger on source code changes
- [ ] Build steps complete successfully
- [ ] Deployment steps succeed
- [ ] Artifacts are generated correctly

**Platform Services:**
- [ ] Pages/Artifacts are accessible
- [ ] Wiki syncs automatically (if applicable)
- [ ] Branch protection rules enforce
- [ ] PR validation works

**AI Assistance:**
- [ ] MCP server provides context (if configured)
- [ ] documentation-architect agent works
- [ ] AI-generated content maintains quality

### Automated Test Script

```bash
#!/bin/bash
# test-migration.sh - Automated migration testing

echo "Testing documentation migration..."

# Test 1: Build all documentation
echo "[1/5] Building documentation..."
make docs-all || exit 1

# Test 2: Verify platform URLs
echo "[2/5] Verifying platform URLs..."
PLATFORM=$(grep "defaultPlatform" .docgen/platform-config.json | cut -d'"' -f4)
if [ "$PLATFORM" = "GitHub" ]; then
    grep -r "github.com" docs/docfx-developer/_site >/dev/null || exit 1
elif [ "$PLATFORM" = "AzureDevOps" ]; then
    grep -r "dev.azure.com" docs/docfx-developer/_site >/dev/null || exit 1
fi

# Test 3: Validate workflow files
echo "[3/5] Validating workflows..."
if [ "$PLATFORM" = "GitHub" ]; then
    test -d .github/workflows || exit 1
    test ! -d .github/workflows.disabled || exit 1
elif [ "$PLATFORM" = "AzureDevOps" ]; then
    test -d .github/workflows.disabled || exit 1
fi

# Test 4: Check git remote
echo "[4/5] Checking git remote..."
git remote -v | grep origin || exit 1

# Test 5: Verify documentation content
echo "[5/5] Verifying documentation content..."
test -f docs/docfx-developer/_site/index.html || exit 1
test -f docs/docfx-user/_site/index.html || exit 1

echo " All tests passed!"
```

## Troubleshooting

### Common Issues

**Issue: DocFX URLs still point to old platform**

```bash
# Solution: Re-run platform switch script
pwsh .docgen/switch-platform.ps1 -Platform GitHub -Force

# Verify changes
grep -r "_gitContribute" docs/docfx-*/docfx.json
```

**Issue: Workflows don't trigger after migration**

```bash
# GitHub: Check workflow permissions
# Settings ’ Actions ’ General ’ Workflow permissions ’ Read and write

# Azure DevOps: Check pipeline authorization
# Project Settings ’ Pipelines ’ Settings ’ Disable "Limit job authorization scope"
```

**Issue: Branch protection blocks pushes**

```bash
# Temporarily disable branch protection
# GitHub: Settings ’ Branches ’ Edit rule ’ Disable temporarily
# Azure DevOps: Repos ’ Branches ’ Branch policies ’ Temporarily exempt

# Complete migration, then re-enable
```

**Issue: Wiki doesn't sync**

```bash
# GitHub: Check workflow permissions include wiki access
# Settings ’ Actions ’ General ’ Workflow permissions ’ Read and write

# Azure DevOps: Re-publish wiki
# Overview ’ Wiki ’ More ’ Unpublish ’ Re-publish from /docs/wiki
```

**Issue: Documentation builds fail locally**

```bash
# Check DocFX installation
docfx --version

# Reinstall if needed
dotnet tool uninstall -g docfx
dotnet tool install -g docfx

# Check .NET SDK version
dotnet --version  # Should be 10.0.100-rc.2 or later
```

**Issue: MCP server not providing context**

```bash
# Verify MCP configuration
code ~/.config/claude/claude_desktop_config.json

# Should include microsoft-learn MCP server
# Restart Claude desktop app after configuration changes
```

### Platform-Specific Issues

**GitHub Issues:**

| Issue | Solution |
|-------|----------|
| 404 on GitHub Pages | Wait 5-10 minutes for initial deployment, check gh-pages branch exists |
| Actions workflow disabled | Settings ’ Actions ’ Enable Actions |
| CNAME conflicts | Remove custom domain or update CNAME record |
| Permissions errors | Ensure GITHUB_TOKEN has pages: write permissions |

**Azure DevOps Issues:**

| Issue | Solution |
|-------|----------|
| Wiki not visible | Enable Wiki in Project Settings ’ Overview ’ Features |
| Pipeline authorization fails | Project Settings ’ Pipelines ’ Settings ’ Enable "Limit job authorization scope" |
| Artifact publish fails | Check Build Service account permissions on repository |
| Branch policy too strict | Adjust policy to exempt administrators or specific users |

## Rollback Procedures

### Emergency Rollback

If migration fails, quickly restore previous state:

```bash
# 1. Switch back to backup branch
git checkout backup-before-migration

# 2. Force push to main (CAUTION)
git push origin +backup-before-migration:main

# 3. Re-run platform switch if needed
pwsh .docgen/switch-platform.ps1 -Platform <OriginalPlatform>

# 4. Rebuild documentation
make docs-all
```

### Planned Rollback

For a controlled rollback:

```bash
# 1. Create rollback branch
git checkout -b rollback-migration

# 2. Revert platform switch commit
git revert <migration-commit-sha>

# 3. Create PR and merge
gh pr create --title "Rollback: Revert platform migration" --body "Reverting to original platform"
gh pr merge --auto --squash

# 4. Reconfigure platform services
# - Re-enable original platform services
# - Disable new platform services
# - Update DNS/URLs if needed
```

### Partial Rollback

To run on both platforms simultaneously:

```bash
# 1. Don't disable workflows on either platform
# Keep both .github/workflows and .azure-pipelines active

# 2. Configure both platforms in platform-config.json
# Use detect-platform.ps1 to auto-detect environment

# 3. Deploy to both platforms
# Documentation will be available on both GitHub Pages and Azure Artifacts
```

## Additional Resources

- [GitHub Pages Setup Guide](github-pages-setup-guide.md)
- [Azure DevOps Setup Guide](azure-devops-setup-guide.md)
- [AI Documentation Workflows](ai-documentation-workflows.md)
- [DocFX Configuration Guide](https://dotnet.github.io/docfx/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure Pipelines Documentation](https://docs.microsoft.com/en-us/azure/devops/pipelines/)

## Summary

This guide covered:

-  Platform prerequisites and preparation
-  GitHub ’ Azure DevOps migration (6 steps)
-  Azure DevOps ’ GitHub migration (6 steps)
-  Testing procedures and validation
-  Troubleshooting common issues
-  Rollback procedures for emergencies

**Key Takeaways:**

1. **Always backup** before migrating (`backup-before-migration` branch)
2. **Use automation** (switch-platform.ps1) to reduce errors
3. **Test thoroughly** after migration using provided checklist
4. **Keep both platforms** active during transition if possible
5. **Document lessons learned** for future migrations

For questions or issues, refer to the AI Documentation Workflows guide or create an issue in the repository.

---

Last Updated: [Current Date]
Platform: Platform-Agnostic
Maintained By: Documentation Team
