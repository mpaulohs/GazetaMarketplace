# GitHub Pages & Branch Protection Setup Guide

**Purpose**: Manual configuration steps to complete Phase 6 (GitHub Plugin Implementation)
**Audience**: Project maintainers with repository admin access
**Prerequisites**: All GitHub Actions workflows created (Phase 6, Tasks 6.1-6.5)

---

## Table of Contents

1. [GitHub Pages Configuration](#github-pages-configuration)
2. [Branch Protection Rules](#branch-protection-rules)
3. [Testing & Verification](#testing--verification)
4. [Troubleshooting](#troubleshooting)

---

## GitHub Pages Configuration

GitHub Pages hosts the developer and user documentation as static HTML sites.

### Step 1: Navigate to Repository Settings

1. Go to your repository: `https://github.com/NotMyself/net10-project-example`
2. Click **Settings** (top navigation)
3. Click **Pages** in the left sidebar (under "Code and automation")

### Step 2: Configure Build and Deployment Source

**Recommended: GitHub Actions Deployment**

1. Under "Build and deployment":
   - **Source**: Select **GitHub Actions** (recommended for multiple docs sites)

2. This allows the workflows to deploy automatically without branch-based deployment

**Alternative: Branch-Based Deployment (Not Recommended)**

If you prefer traditional branch deployment:
1. Source: **Deploy from a branch**
2. Branch: **gh-pages** (create if doesn't exist)
3. Folder: **/ (root)**

> **Note**: GitHub Actions deployment is preferred because it supports multiple documentation sites (developer + user) with better control.

### Step 3: Configure Custom Domain (Optional)

If you have a custom domain:

1. Under "Custom domain", enter: `docs.yourdomain.com`
2. Click **Save**
3. Wait for DNS check to complete (may take a few minutes)
4. Add DNS CNAME record pointing to: `notmyself.github.io`

**Skip this step if using the default GitHub Pages URL**

### Step 4: Enforce HTTPS

1. Check the box: **Enforce HTTPS**
2. This ensures all traffic to your documentation is encrypted

### Step 5: Verify Configuration

After configuration, you should see:

```
Your site is ready to be published at:
https://notmyself.github.io/net10-project-example/
```

**URLs**:
- Developer Docs: `https://notmyself.github.io/net10-project-example/`
- User Docs: `https://notmyself.github.io/net10-project-example/user/`

---

## Branch Protection Rules

Branch protection ensures that documentation changes are validated before merging to `main`.

### Step 1: Navigate to Branch Settings

1. Go to repository **Settings**
2. Click **Branches** in the left sidebar
3. Under "Branch protection rules", click **Add branch protection rule**

### Step 2: Configure Protection for Main Branch

**Branch name pattern**: `main`

**Protect matching branches**:

1. ✅ **Require a pull request before merging**
   - Require approvals: **1** (optional but recommended)
   - Dismiss stale pull request approvals when new commits are pushed (optional)

2. ✅ **Require status checks to pass before merging**
   - **Require branches to be up to date before merging** (recommended)
   - Search for and select these status checks:
     - `validate` (from docs-pr-validation.yml workflow)
     - You may also want to add existing checks:
       - `authorization` (if present from PR validation pipeline)
       - `quality-checks` (if present)

3. ✅ **Require conversation resolution before merging** (optional)

4. ✅ **Do not allow bypassing the above settings** (recommended for team projects)

**Rules applied to administrators**:
- ⚠️ **Include administrators** - Check this if you want admins to also follow the rules (recommended for consistency)

### Step 3: Save Protection Rule

1. Click **Create** at the bottom
2. Verify the rule appears in the "Branch protection rules" list

### Step 4: Verify Protection is Active

1. Try to push directly to `main` - you should get an error
2. Create a test PR - validation should run automatically
3. Protection is working when you see:
   - ❌ Merge blocked until status checks pass
   - ✅ After checks pass, merge button becomes enabled

---

## Testing & Verification

After completing configuration, test the full end-to-end workflow.

### Test 1: Create Test PR

```bash
# Create test branch
git checkout -b test-docs-deployment

# Make a small change to documentation
echo "<!-- Test deployment: $(date) -->" >> docs/docfx-developer/index.md

# Commit and push
git add docs/docfx-developer/index.md
git commit -m "Test: Documentation deployment"
git push origin test-docs-deployment

# Create PR using GitHub CLI
gh pr create \
  --title "Test: Documentation Deployment" \
  --body "Testing GitHub Actions workflows for documentation deployment"
```

### Test 2: Verify PR Validation

1. Go to the PR page on GitHub
2. Wait for "Validate Documentation" check to run (~3-5 minutes)
3. Verify the check passes with ✅
4. Check the "Files changed" tab shows your change

Expected result: PR validation runs and passes

### Test 3: Merge and Deploy

```bash
# Merge PR (if checks pass)
gh pr merge --squash

# Switch back to main and pull
git checkout main
git pull
```

### Test 4: Verify Deployment

Wait 3-5 minutes for deployment workflows to complete, then verify:

```bash
# Check workflow status
gh run list --workflow=docs-developer-deploy.yml --limit=1

# Check user docs workflow (if user docs were changed)
gh run list --workflow=docs-user-deploy.yml --limit=1

# Check wiki sync workflow (if wiki was changed)
gh run list --workflow=docs-wiki-sync.yml --limit=1
```

### Test 5: Verify Live Documentation

Open these URLs in your browser:

1. **Developer Docs**: https://notmyself.github.io/net10-project-example/
2. **User Docs**: https://notmyself.github.io/net10-project-example/user/
3. **Wiki**: https://github.com/NotMyself/net10-project-example/wiki

Verify:
- ✅ Sites load without errors
- ✅ Navigation works
- ✅ Search functionality works
- ✅ Diagrams render (if present)
- ✅ Content matches what you expect

---

## Troubleshooting

### Issue: GitHub Pages Not Showing Up

**Symptoms**: 404 error when visiting documentation URL

**Solutions**:
1. Check GitHub Pages settings are correctly configured
2. Verify workflow ran successfully: Go to Actions → Latest "Deploy Developer Documentation" run
3. Check workflow logs for errors
4. Wait 5-10 minutes - initial deployment can be slow
5. Try hard refresh in browser (Ctrl+Shift+R or Cmd+Shift+R)

### Issue: Wiki Sync Failing

**Symptoms**: "Failed to checkout wiki repository" error in workflow

**Solutions**:
1. **Create wiki first**: GitHub wikis don't exist until you create the first page manually
   - Go to repository → Wiki tab
   - Click "Create the first page"
   - Add any content (it will be overwritten by sync)
   - Save
2. Re-run the wiki sync workflow
3. Verify GITHUB_TOKEN has write permissions

### Issue: PR Validation Check Not Appearing

**Symptoms**: PR doesn't show "Validate Documentation" status check

**Solutions**:
1. Verify the PR touches files matching the workflow paths (`docs/**` or `src/**/*.cs`)
2. Check `.github/workflows/docs-pr-validation.yml` exists
3. Look at Actions tab → Check if workflow is disabled
4. Verify workflow YAML is valid (no syntax errors)

### Issue: 403 Permission Error During Deployment

**Symptoms**: "Resource not accessible by integration" error

**Solutions**:
1. Go to Settings → Actions → General
2. Under "Workflow permissions":
   - Select "Read and write permissions"
   - Check "Allow GitHub Actions to create and approve pull requests"
3. Save changes
4. Re-run failed workflow

### Issue: Diagrams Not Rendering

**Symptoms**: Mermaid diagrams show as code blocks instead of diagrams

**Solutions**:
1. Verify `markdigExtensions: ["diagrams"]` is in docfx.json
2. Check DocFX version supports Mermaid (requires 2.70+)
3. Ensure diagrams use proper Mermaid syntax
4. Hard refresh browser cache

### Issue: User Docs Showing at Wrong Path

**Symptoms**: User docs appear at root instead of `/user/` subdirectory

**Solutions**:
1. Check `docs-user-deploy.yml` workflow
2. Verify "Prepare artifact with user/ prefix" step copies to correct path
3. Check for typos in path manipulation commands
4. Verify DocFX output directory is `_site/_site` (nested directory)

---

## Additional Configuration

### Enable Dependabot for Workflow Dependencies

To keep GitHub Actions up to date:

1. Go to Settings → Code security and analysis
2. Enable **Dependabot version updates**
3. Create `.github/dependabot.yml`:

```yaml
version: 2
updates:
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
```

### Configure Environment Protection Rules

For additional deployment safety:

1. Go to Settings → Environments
2. Select `github-pages` environment
3. Configure:
   - Required reviewers (for manual approval before deployment)
   - Wait timer (delay deployments by X minutes)
   - Deployment branches (restrict which branches can deploy)

---

## Success Criteria

Phase 6 is complete when:

- ✅ GitHub Pages configured and accessible
- ✅ Branch protection rules enforced on `main`
- ✅ Test PR successfully validated and merged
- ✅ Documentation automatically deployed after merge
- ✅ Developer docs visible at root URL
- ✅ User docs visible at `/user/` URL
- ✅ Wiki synced automatically
- ✅ Team can create PRs and see validation results

---

## Next Steps

After completing this setup:

1. **Phase 7** (Optional): Implement Azure DevOps plugin for multi-platform support
2. **Phase 8**: Configure AI integration with MCP servers
3. **Phase 9**: Test platform switching and validate end-to-end

---

**Setup Guide Status**: ✅ COMPLETE
**Created**: 2025-11-03
**Last Updated**: 2025-11-03
**Maintainer**: Development Team

For issues or questions, see [Troubleshooting](#troubleshooting) or open a GitHub issue.
