# Phase 6: GitHub Plugin Implementation

**Estimated Time**: 4-6 hours
**Status**: NOT STARTED
**Dependencies**: Phase 5 complete

---

## Overview

**Goal**: Automate all 3 documentation types on GitHub with workflows for deployment, wiki sync, and PR validation.

**Why This Phase Matters**: This phase brings everything together with CI/CD automation. Once complete, documentation automatically updates on every commit, PR validation ensures quality, and the entire system runs hands-free.

---

## Key Workflows

1. **docs-developer-deploy.yml**: Deploy developer docs to GitHub Pages (/)
2. **docs-user-deploy.yml**: Deploy user docs to GitHub Pages (/user/)
3. **docs-wiki-sync.yml**: Sync wiki content to GitHub Wiki
4. **docs-pr-validation.yml**: Validate documentation changes in PRs

---

## Tasks

### Task 6.1: Create GitHub Workflows Directory (Small - 15 minutes)

**Directory**: `.github/workflows/`

**Acceptance Criteria**:
- [ ] Directory exists (may already exist from previous work)
- [ ] Workflow files created:
  - `docs-developer-deploy.yml`
  - `docs-user-deploy.yml`
  - `docs-wiki-sync.yml`
  - `docs-pr-validation.yml`

---

### Task 6.2: Create Developer Docs Deployment Workflow (Medium - 2 hours)

**File**: `.github/workflows/docs-developer-deploy.yml`

**Purpose**: Build and deploy developer docs to GitHub Pages root (/) on every push to main.

**Acceptance Criteria**:
- [ ] Triggers on push to main, paths: `docs/docfx-developer/**`, `src/**`
- [ ] Uses ubuntu-latest runner
- [ ] Installs .NET SDK from global.json
- [ ] Installs DocFX
- [ ] Installs diagram generators
- [ ] Runs `dotnet restore` and `dotnet build`
- [ ] Generates diagrams
- [ ] Builds DocFX documentation
- [ ] Uploads pages artifact to GitHub Pages (/) root
- [ ] Deploy step with github-pages environment

**Implementation**:
```yaml
name: Deploy Developer Documentation

on:
  push:
    branches: [main]
    paths:
      - 'docs/docfx-developer/**'
      - 'src/**'
      - '.github/workflows/docs-developer-deploy.yml'
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: "pages-developer"
  cancel-in-progress: true

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET SDK
        uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json

      - name: Install DocFX
        run: dotnet tool install -g docfx

      - name: Install Diagram Tools
        run: |
          dotnet tool install -g dll2mmd
          dotnet tool install -g PlantUmlClassDiagramGenerator

      - name: Restore dependencies
        run: dotnet restore

      - name: Build projects
        run: dotnet build --no-restore

      - name: Generate diagrams
        run: pwsh .docgen/diagram-gen.ps1 -All

      - name: Build Developer Documentation
        run: docfx build docs/docfx-developer/docfx.json

      - name: Upload artifact
        uses: actions/upload-pages-artifact@v3
        with:
          path: docs/docfx-developer/_site

  deploy:
    needs: build
    runs-on: ubuntu-latest
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    steps:
      - name: Deploy to GitHub Pages
        id: deployment
        uses: actions/deploy-pages@v4
```

---

### Task 6.3: Create User Docs Deployment Workflow (Medium - 1.5 hours)

**File**: `.github/workflows/docs-user-deploy.yml`

**Purpose**: Build and deploy user docs to GitHub Pages /user/ subdirectory.

**Acceptance Criteria**:
- [ ] Triggers on push to main, paths: `docs/docfx-user/**`
- [ ] Similar steps to Task 6.2
- [ ] Builds DocFX user documentation
- [ ] Uploads pages artifact to GitHub Pages (/user/) subdirectory

**Implementation**:
```yaml
name: Deploy User Documentation

on:
  push:
    branches: [main]
    paths:
      - 'docs/docfx-user/**'
      - '.github/workflows/docs-user-deploy.yml'
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: "pages-user"
  cancel-in-progress: true

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET SDK
        uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json

      - name: Install DocFX
        run: dotnet tool install -g docfx

      - name: Build User Documentation
        run: docfx build docs/docfx-user/docfx.json

      - name: Prepare artifact with user/ prefix
        run: |
          mkdir -p _site/user
          cp -r docs/docfx-user/_site/* _site/user/

      - name: Upload artifact
        uses: actions/upload-pages-artifact@v3
        with:
          path: _site

  deploy:
    needs: build
    runs-on: ubuntu-latest
    environment:
      name: github-pages-user
      url: ${{ steps.deployment.outputs.page_url }}user/
    steps:
      - name: Deploy to GitHub Pages
        id: deployment
        uses: actions/deploy-pages@v4
```

**Note**: This approach deploys to `/user/` subdirectory. Alternative: Use separate `gh-pages-user` branch and configure as separate GitHub Pages site.

---

### Task 6.4: Create Wiki Sync Workflow (Small - 1 hour)

**File**: `.github/workflows/docs-wiki-sync.yml`

**Purpose**: Sync wiki markdown files to GitHub Wiki on changes.

**Acceptance Criteria**:
- [ ] Triggers on push to main, paths: `docs/wiki/**`
- [ ] Checks out repository with persistCredentials
- [ ] Checks out GitHub Wiki repository
- [ ] Copies `docs/wiki/*.md` to wiki/
- [ ] Creates `_Sidebar.md` for navigation
- [ ] Commits and pushes to wiki
- [ ] Uses GITHUB_TOKEN for authentication

**Implementation**:
```yaml
name: Sync Wiki

on:
  push:
    branches: [main]
    paths:
      - 'docs/wiki/**'
      - '.github/workflows/docs-wiki-sync.yml'
  workflow_dispatch:

permissions:
  contents: write

jobs:
  sync-wiki:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout main repository
        uses: actions/checkout@v4

      - name: Checkout wiki repository
        uses: actions/checkout@v4
        with:
          repository: ${{ github.repository }}.wiki
          path: wiki
          token: ${{ secrets.GITHUB_TOKEN }}

      - name: Copy wiki files
        run: |
          cp docs/wiki/*.md wiki/

      - name: Create sidebar
        run: |
          cat > wiki/_Sidebar.md <<'EOF'
          **[Home](Home)**

          **Documentation**
          * [System Purpose](system-purpose)
          * [System Access](system-access)
          * [Feature Summary](feature-summary)
          * [Active Development](active-development)
          EOF

      - name: Commit and push to wiki
        working-directory: wiki
        run: |
          git config user.name "github-actions[bot]"
          git config user.email "github-actions[bot]@users.noreply.github.com"
          git add .
          git diff-index --quiet HEAD || git commit -m "Update wiki from main repository"
          git push
```

---

### Task 6.5: Create PR Validation Workflow (Medium - 1.5 hours)

**File**: `.github/workflows/docs-pr-validation.yml`

**Purpose**: Validate documentation changes in pull requests before merge.

**Acceptance Criteria**:
- [ ] Triggers on pull_request, paths: `docs/**`, `src/**/*.cs`
- [ ] Checks XML documentation comments exist (enforce later, warn initially)
- [ ] Runs markdownlint on all markdown files
- [ ] Builds all DocFX documentation (validation only, no deployment)
- [ ] Posts comment on PR if validation fails
- [ ] Blocks merge if validation fails

**Implementation**:
```yaml
name: Validate Documentation

on:
  pull_request:
    paths:
      - 'docs/**'
      - 'src/**/*.cs'
      - '.github/workflows/docs-pr-validation.yml'

permissions:
  contents: read
  pull-requests: write
  checks: write

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET SDK
        uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'

      - name: Install markdownlint
        run: npm install -g markdownlint-cli

      - name: Lint markdown files
        run: markdownlint 'docs/**/*.md' --config .markdownlint.json
        continue-on-error: true

      - name: Install DocFX
        run: dotnet tool install -g docfx

      - name: Install Diagram Tools
        run: |
          dotnet tool install -g dll2mmd
          dotnet tool install -g PlantUmlClassDiagramGenerator

      - name: Build projects
        run: dotnet build

      - name: Generate diagrams
        run: pwsh .docgen/diagram-gen.ps1 -All

      - name: Build Developer Documentation
        run: docfx build docs/docfx-developer/docfx.json

      - name: Build User Documentation
        run: docfx build docs/docfx-user/docfx.json

      - name: Check for XML documentation
        run: |
          # Find C# files with public types but no XML comments (warning only for now)
          # TODO: Enforce in future by failing build
          echo "Checking for missing XML documentation..."
          # This is a placeholder - implement proper check script

      - name: Validation Summary
        if: always()
        run: |
          echo "✅ Documentation validation complete"
          echo "📝 Markdown linting: Check logs above"
          echo "📚 DocFX builds: Check logs above"
```

**Create .markdownlint.json**:
```json
{
  "default": true,
  "MD013": false,
  "MD033": false,
  "MD041": false
}
```

---

### Task 6.6: Configure GitHub Pages (Small - 15 minutes)

**Manual Step**: GitHub UI

**Acceptance Criteria**:
- [ ] Navigate to Settings → Pages
- [ ] Source: Deploy from a branch OR GitHub Actions (recommended)
- [ ] Branch: gh-pages, /(root) (if using branch deployment)
- [ ] Custom domain configured (optional)
- [ ] HTTPS enforced

**Steps**:
1. Go to repository Settings
2. Click "Pages" in left sidebar
3. Under "Build and deployment":
   - Source: **GitHub Actions** (recommended for multiple sites)
   - OR: Deploy from branch `gh-pages` (simpler, single site)
4. If custom domain: Enter domain, verify
5. Check "Enforce HTTPS"

---

### Task 6.7: Configure Branch Protection (Small - 15 minutes)

**Manual Step**: GitHub UI

**Acceptance Criteria**:
- [ ] Navigate to Settings → Branches → Branch protection rules
- [ ] Add rule for main branch
- [ ] Require status checks: "Validate Documentation"
- [ ] Require approvals: 1 (optional)
- [ ] Dismiss stale reviews on push (optional)

**Steps**:
1. Go to Settings → Branches
2. Click "Add branch protection rule"
3. Branch name pattern: `main`
4. Check "Require status checks to pass before merging"
5. Search for "validate" and select "Validate Documentation"
6. Check "Require approvals" (optional)
7. Click "Create"

---

### Task 6.8: Test Full GitHub Workflow (Medium - 1 hour)

**Action**: Create test PR and merge to verify end-to-end flow

**Acceptance Criteria**:
- [ ] Make documentation change (e.g., edit docs/docfx-developer/index.md)
- [ ] Create PR from feature branch
- [ ] PR validation runs and passes
- [ ] Merge PR
- [ ] Developer docs deploy workflow runs successfully
- [ ] User docs deploy workflow runs (if user docs changed)
- [ ] Wiki sync workflow runs (if wiki changed)
- [ ] Docs visible at `https://notmyself.github.io/net10-project-example/`
- [ ] User docs at `/user/` subdirectory
- [ ] Wiki updated at `https://github.com/NotMyself/net10-project-example/wiki`

**Test Steps**:
```bash
# Create test branch
git checkout -b test-docs-deployment

# Make change
echo "Test change" >> docs/docfx-developer/index.md
git add docs/docfx-developer/index.md
git commit -m "Test: Documentation deployment"

# Push and create PR
git push origin test-docs-deployment
gh pr create --title "Test: Documentation Deployment" --body "Testing GitHub Actions workflows"

# Wait for PR validation to complete
gh pr checks

# Merge PR
gh pr merge --squash

# Wait for deployment workflows
gh run list --workflow=docs-developer-deploy.yml --limit=1

# Verify documentation
open https://notmyself.github.io/net10-project-example/
```

---

## Phase Completion Criteria

Phase 6 is complete when:

- [ ] All GitHub Actions workflows created and working
- [ ] Documentation deploys to GitHub Pages automatically
- [ ] Wiki syncs to GitHub Wiki automatically
- [ ] PR validation blocks bad PRs
- [ ] Branch protection configured and enforced
- [ ] Full end-to-end test passes (PR creation, validation, merge, deployment)

---

## Output Artifacts

1. **GitHub Workflows**:
   - `.github/workflows/docs-developer-deploy.yml`
   - `.github/workflows/docs-user-deploy.yml`
   - `.github/workflows/docs-wiki-sync.yml`
   - `.github/workflows/docs-pr-validation.yml`

2. **Configuration Files**:
   - `.markdownlint.json`

3. **Deployed Sites**:
   - Developer Docs: https://notmyself.github.io/net10-project-example/
   - User Docs: https://notmyself.github.io/net10-project-example/user/
   - Wiki: https://github.com/NotMyself/net10-project-example/wiki

---

## Success Indicators

You'll know this phase is successful when:

- Push to main triggers automatic documentation deployment
- Documentation appears at GitHub Pages URLs within 5 minutes
- Wiki changes sync automatically
- PRs with documentation errors are blocked
- Team can rely on docs being always up-to-date
- Zero manual deployment steps required

---

**Phase Status**: NOT STARTED
**Next Task**: Task 6.1 - Create GitHub Workflows Directory
**Estimated Completion**: After 4-6 hours of focused work
