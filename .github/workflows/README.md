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
- **Requires:** Claude Code GitHub App (recommended) or `CLAUDE_CODE_OAUTH_TOKEN` secret

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

**Setup (Option 1 - Recommended):**
1. Install Claude Code GitHub App:
   ```bash
   # From Claude Code CLI
   claude
   /install-github-app
   ```
2. Or visit: https://github.com/apps/claude
3. Install to your repository
4. App handles authentication automatically

**Setup (Option 2 - Manual OAuth):**
1. Get OAuth token: `claude auth token`
2. Add as repository secret: `CLAUDE_CODE_OAUTH_TOKEN`
3. Re-run workflow

**Graceful Degradation:** Step skips if not configured

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

**Option 1: GitHub App (Recommended)**

1. **Install via Claude Code CLI:**
   ```bash
   # Open Claude Code
   claude

   # Run installation command
   /install-github-app
   ```

2. **Or install manually:**
   - Visit https://github.com/apps/claude
   - Click **Install** or **Configure**
   - Select your repository
   - Grant permissions and complete installation

3. **Verify:**
   - Create test PR
   - Check Step 4 (Code Review)
   - Should execute instead of skip

**Benefits:**
- ✅ No manual token management
- ✅ Automatic authentication
- ✅ Fine-grained permissions
- ✅ Easier to set up and maintain

**Option 2: Manual OAuth Token**

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

---

### `dotnet-ci.yml` - CI/CD Pipeline

**Purpose:** Multi-platform continuous integration and deployment

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop`
- Manual workflow dispatch
- Tag pushes for releases (`v*`)

**Jobs:**

#### Build & Test (Multi-Platform)
- **Platforms:** Linux, Windows, macOS
- **Steps:**
  - Restore dependencies
  - Build solution (Release configuration)
  - Run all unit tests
  - Publish applications (Linux only)
  - Upload artifacts

**Artifacts Published:**
- `web-app-linux-x64` - Example.Web application
- `api-app-linux-x64` - Example.API application
- Retention: 7 days

#### Code Coverage
- **Platform:** Linux (for performance)
- **Steps:**
  - Run tests with code coverage (`XPlat Code Coverage`)
  - Generate HTML and Cobertura reports with ReportGenerator
  - Post coverage summary to PR comments
  - Upload to Codecov (if token configured)
  - Upload coverage report as artifact
  - Check coverage threshold

**Coverage Reports:**
- HTML report in artifacts (30-day retention)
- PR comment with summary
- Codecov dashboard (if configured)

**Setup Codecov:**
1. Sign up at [codecov.io](https://codecov.io)
2. Add repository
3. Get upload token
4. Add as repository secret: `CODECOV_TOKEN`

#### Docker Image Build
- **Trigger:** Push to `main` branch only
- **Registry:** GitHub Container Registry (ghcr.io)
- **Images:**
  - `ghcr.io/{owner}/{repo}/example-web`
  - `ghcr.io/{owner}/{repo}/example-api`
- **Tags:**
  - Branch name (`main`)
  - Git SHA (`main-{sha}`)
  - Semantic version (if tagged)

**Dockerfile Locations:**
- `src/Example.Web/Dockerfile`
- `src/Example.API/Dockerfile`

**Pull Images:**
```bash
docker pull ghcr.io/{owner}/{repo}/example-web:main
docker pull ghcr.io/{owner}/{repo}/example-api:main
```

#### NuGet Package Publishing
- **Trigger:** Tag push (`v*` tags)
- **Destinations:**
  - NuGet.org (if `NUGET_API_KEY` configured)
  - GitHub Packages (automatic)
- **Versioning:** Extracted from git tag

**Setup NuGet Publishing:**
1. Get API key from [nuget.org](https://www.nuget.org/account/apikeys)
2. Add as repository secret: `NUGET_API_KEY`
3. Create tag: `git tag v1.0.0 && git push --tags`

#### GitHub Release Creation
- **Trigger:** Tag push (`v*` tags)
- **Features:**
  - Automatic release notes from commits
  - Attached artifacts (binaries, NuGet packages)
  - Changelog link

**Create Release:**
```bash
git tag -a v1.0.0 -m "Release 1.0.0"
git push origin v1.0.0
```

### CI/CD Pipeline Diagram

```
Push/PR → Build & Test (3 platforms) → Coverage Report
   ↓
Tag Push → NuGet Publish → GitHub Release
   ↓
Main Push → Docker Build → Container Registry
```

### Environment Variables

| Variable | Purpose | Set By |
|----------|---------|--------|
| `DOTNET_SKIP_FIRST_TIME_EXPERIENCE` | Skip .NET welcome | Workflow |
| `DOTNET_CLI_TELEMETRY_OPTOUT` | Disable telemetry | Workflow |
| `DOTNET_NOLOGO` | Hide .NET logo | Workflow |
| `ASPNETCORE_URLS` | ASP.NET listen address | Dockerfile |
| `ASPNETCORE_ENVIRONMENT` | Environment name | Dockerfile |

### Secrets Required

| Secret | Required | Purpose | Setup |
|--------|----------|---------|-------|
| `GITHUB_TOKEN` | ✅ Auto | Artifacts, packages | Automatic |
| `CODECOV_TOKEN` | ❌ Optional | Coverage upload | codecov.io |
| `NUGET_API_KEY` | ❌ Optional | NuGet.org publish | nuget.org |

### Testing CI/CD Locally

**Build Docker images:**
```bash
# Build Web image
docker build -f src/Example.Web/Dockerfile -t example-web:local .

# Build API image
docker build -f src/Example.API/Dockerfile -t example-api:local .

# Run containers
docker run -p 8080:8080 example-web:local
docker run -p 8080:8080 example-api:local
```

**Test coverage locally:**
```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Install ReportGenerator
dotnet tool install --global dotnet-reportgenerator-globaltool

# Generate report
reportgenerator \
  -reports:./**/coverage.cobertura.xml \
  -targetdir:./coverage-report \
  -reporttypes:HtmlInline
```

### Monitoring & Metrics

**View Workflow Runs:**
- Actions tab → .NET CI/CD workflow
- Check logs for each job
- Download artifacts

**Key Metrics:**
- Build time per platform (typical: 2-5 minutes)
- Test duration (typical: 1-3 minutes)
- Coverage percentage (configure threshold)
- Docker build time (typical: 3-5 minutes)

### Troubleshooting

**Build fails on specific platform:**
- Check platform-specific code
- Review OS-specific file paths
- Verify dependencies available

**Coverage upload fails:**
- Check `CODECOV_TOKEN` secret
- Verify coverage file paths
- Check Codecov service status

**Docker build fails:**
- Verify Dockerfile syntax
- Check base image availability
- Review build context size

**NuGet publish fails:**
- Verify `NUGET_API_KEY` secret
- Check package version conflicts
- Ensure unique version numbers

## Further Reading

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Workflow Syntax](https://docs.github.com/en/actions/reference/workflow-syntax-for-github-actions)
- [Security Best Practices](https://docs.github.com/en/actions/security-guides/security-hardening-for-github-actions)
- [Docker Multi-Stage Builds](https://docs.docker.com/build/building/multi-stage/)
- [Codecov Documentation](https://docs.codecov.com/)

---

**Questions?** See [CONTRIBUTING.md](../../CONTRIBUTING.md) or open an issue.
