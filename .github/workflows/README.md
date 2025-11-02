# GitHub Workflows Documentation

This directory contains GitHub Actions workflows that automate quality checks, security scanning, and code review for pull requests.

## Workflows Overview

### `pr-validation.yml` - Pull Request Validation Pipeline

**Purpose:** Comprehensive automated validation for all pull requests

**Triggers:**
- Pull request opened/edited/synchronized/reopened
- Target branches: `main`, `develop`
- Manual workflow dispatch

**Pipeline Steps:**

#### 1️⃣ Authorization
- **Job:** `authorization`
- **Purpose:** Verify contributor is authorized
- **Checks:**
  - User on authorized list
  - Organization membership
  - Collaborator status
  - Emergency circuit breaker status

**Authorization Config:** `.github/claude-authorized-users.yml`

**Emergency Disable:**
```yaml
emergency:
  enabled: true  # Blocks ALL automated actions
  reason: "Investigating security issue"
```

#### 2️⃣ PR Guardrails
- **Job:** `guardrails`
- **Purpose:** Enforce PR quality standards
- **Checks:**
  - PR has adequate description (min 50 characters)
  - PR size reasonable (<2000 lines changed)
  - Required template sections completed

**Thresholds:**
- **Small:** < 200 lines (✅ Ideal)
- **Medium:** 200-1000 lines (⚠️ Consider splitting)
- **Large:** 1000-2000 lines (⚠️ May be hard to review)
- **Huge:** > 2000 lines (❌ Should be split)

#### 3️⃣ Quality Checks
- **Job:** `quality-checks`
- **Purpose:** Verify code quality
- **Checks:**
  - Code formatting (`dotnet format --verify-no-changes`)
  - Build success (`dotnet build`)
  - All tests pass (`dotnet test`)

**Auto-fixes:**
- Formatting: Run `dotnet format` locally
- Build: Fix compilation errors
- Tests: Fix failing tests

#### 4️⃣ Code Review (Optional)
- **Job:** `code-review`
- **Purpose:** Automated .NET-specific code review
- **Provider:** Claude Code (Anthropic)
- **Requires:** `CLAUDE_CODE_OAUTH_TOKEN` secret

**Review Criteria:**
- C# coding conventions and naming
- Async/await patterns
- Error handling and null safety
- ImplicitUsings compliance (disabled)
- Centralized Package Management compliance
- MSTest patterns
- Security best practices
- Breaking changes detection
- Documentation completeness

**Setup:**
1. Get OAuth token from Claude Code
2. Add as repository secret: `CLAUDE_CODE_OAUTH_TOKEN`
3. Re-run workflow

**Graceful Degradation:** Step skips if secret not configured

#### 5️⃣ Security Review
- **Job:** `security-review`
- **Purpose:** Detect security vulnerabilities
- **Scanners:**
  1. **GitLeaks** - Secret detection
  2. **.NET Security Analyzers** - Roslyn analyzers
  3. **NuGet Vulnerability Scanner** - CVE detection
  4. **Path Security Scanner** - Unsafe file operations

**Security Categories:**
- **Secrets:** API keys, tokens, passwords
- **Dependencies:** Vulnerable NuGet packages
- **Code Patterns:** SQL injection, XSS, etc.
- **File Security:** Path traversal, unsafe deserialization

#### 6️⃣ .NET Validation
- **Job:** `dotnet-validation`
- **Purpose:** Enforce .NET project standards
- **Validators:**
  1. **.csproj Structure** - Project file compliance
  2. **CPM Compliance** - Directory.Packages.props
  3. **global.json** - SDK configuration

**.csproj Checks:**
- No Version attributes in PackageReference (CPM)
- MSTest projects have EnableMSTestRunner=true
- MSTest projects have OutputType=Exe
- No duplicate PackageReference entries
- ImplicitUsings not enabled
- Valid XML structure

**CPM Checks:**
- ManagePackageVersionsCentrally=true
- All PackageVersion entries well-formed
- No duplicate package versions
- Semantic versioning compliance
- Cross-reference with .csproj files

**global.json Checks:**
- SDK version matches .NET 10
- Test runner configured (Microsoft.Testing.Platform)
- RollForward policy set
- No deprecated properties

#### ✅ Validation Complete
- **Job:** `validation-complete`
- **Purpose:** Summary of all validation results
- **Output:** Markdown summary with step statuses

**Summary Table:**
| Step | Status |
|------|--------|
| 1️⃣ Authorization | ✅ Passed |
| 2️⃣ PR Guardrails | ✅ Passed |
| 3️⃣ Quality Checks | ✅ Passed |
| 4️⃣ Code Review | ⏭️ Skipped |
| 5️⃣ Security Review | ✅ Passed |
| 6️⃣ .NET Validation | ✅ Passed |

## PR Comments

Each validation step posts results as PR comments with **update-in-place** behavior:

- **First run:** Creates new comment
- **Subsequent runs:** Updates existing comment (no spam)
- **Marker:** `<!-- pr-validation:step-N -->`

**Comment Format:**
```markdown
## 3️⃣ Quality Checks

✅ **Passed** - All quality checks succeeded

### Results
- **Code Formatting:** ✅ Passed
- **Build:** ✅ Passed (0 errors, 0 warnings)
- **Tests:** ✅ Passed (42 tests, 0 failures)

**Status:** ✅ Pass
**Timestamp:** 2025-11-02T10:30:00Z

[View workflow run →](https://github.com/...)
```

### `claude.yml` - Claude Code Mentions

**Purpose:** Respond to `@claude` mentions in issues and PRs

**Triggers:**
- Issue comment created with `@claude`
- PR comment created with `@claude`

**Authorization:** Same as PR validation (`.github/claude-authorized-users.yml`)

**Actions:**
- Analyzes issue/PR context
- Provides .NET-specific assistance
- Suggests fixes and improvements
- Can reference project documentation

## Workflow Configuration

### Secrets Required

| Secret | Purpose | Required | Setup |
|--------|---------|----------|-------|
| `GITHUB_TOKEN` | Standard GitHub token | ✅ Auto | Automatic |
| `CLAUDE_CODE_OAUTH_TOKEN` | Claude Code auth | ❌ Optional | Manual |

### Setting Up Claude Code Integration

1. **Get OAuth Token:**
   ```bash
   # From Claude Code CLI
   claude auth token
   ```

2. **Add to Repository:**
   - Settings → Secrets and variables → Actions
   - New repository secret
   - Name: `CLAUDE_CODE_OAUTH_TOKEN`
   - Value: Paste token

3. **Verify:**
   - Create test PR
   - Check Step 4 (Code Review)
   - Should execute instead of skip

### Customizing Validation

**Adjust PR Size Thresholds:**

Edit `pr-validation.yml`:
```yaml
- name: Check PR size
  run: |
    if [ "$CHANGES_COUNT" -gt 5000 ]; then  # Increase from 2000
      echo "::warning::PR is very large"
    fi
```

**Disable Specific Steps:**

Comment out job in `pr-validation.yml`:
```yaml
# security-review:
#   name: "5️⃣ Security Review"
#   ...
```

**Adjust Severity:**

Edit validation scripts (`.github/scripts/*.ps1`):
```powershell
# Change from error to warning
severity = "warning"  # was "error"
```

## Troubleshooting

### Workflow Not Running

**Check:**
1. Workflow files exist in `main` branch
2. PR targets correct branch (`main` or `develop`)
3. Workflow syntax is valid (YAML)

### Authorization Failures

**Common causes:**
- User not in authorized list
- Expired permissions
- Emergency circuit breaker enabled

**Fix:**
1. Add user to `.github/claude-authorized-users.yml`
2. Check `authorized_users` list
3. Verify `emergency.enabled` is false

### GitLeaks False Positives

**Ignore patterns:**

Create `.gitleaksignore`:
```
# Ignore test fixtures
tests/fixtures/sample-api-key.txt
```

### Build Failures

**Common issues:**
- SDK version mismatch (check `global.json`)
- Missing NuGet restore
- Platform-specific dependencies

**Debug locally:**
```bash
dotnet restore
dotnet build --verbosity detailed
```

### Test Failures

**Check:**
1. Tests pass locally: `dotnet test`
2. Playwright browsers installed
3. Test environment configured

## Performance

**Typical Run Times:**
- Authorization: ~5 seconds
- PR Guardrails: ~10 seconds
- Quality Checks: ~2-5 minutes
- Code Review: ~1-3 minutes (if enabled)
- Security Review: ~2-4 minutes
- .NET Validation: ~30 seconds
- **Total:** ~5-13 minutes

**Optimization:**
- Jobs run in parallel where possible
- Caching enabled for .NET dependencies
- Only affected tests run (future enhancement)

## Monitoring

**View Workflow Runs:**
- Actions tab in GitHub
- Filter by workflow name
- Check run logs for details

**Metrics to Watch:**
- Success rate
- Average duration
- Failure patterns
- Step-specific failures

## Best Practices

1. **Keep workflows fast:** Optimize slow steps
2. **Fail fast:** Run quick checks first
3. **Clear feedback:** Detailed error messages
4. **Update-in-place:** Avoid comment spam
5. **Security first:** Never commit secrets to workflows

## Further Reading

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Workflow Syntax](https://docs.github.com/en/actions/reference/workflow-syntax-for-github-actions)
- [Security Best Practices](https://docs.github.com/en/actions/security-guides/security-hardening-for-github-actions)

---

**Questions?** See [CONTRIBUTING.md](../../CONTRIBUTING.md) or open an issue.
