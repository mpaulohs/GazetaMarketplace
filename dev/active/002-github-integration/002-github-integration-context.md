# GitHub Integration Implementation - Context

**Last Updated:** 2025-11-02

---

## Overview

Implementation of comprehensive GitHub integration for the .NET 10 project example repository, based on the production-grade setup from the claude-win11-speckit-safe-update-skill repository. This includes PR validation pipelines, access-controlled Claude Code integration, security scanning, and professional OSS project structure.

---

## Key Files and Directories

### Reference Repository Files (Source)
**Location:** `/mnt/c/Users/bobby/src/claude/claude-win11-speckit-safe-update-skill/`

**Workflows:**
- `.github/workflows/pr-validation.yml` (869 lines) - 6-step PR validation pipeline
- `.github/workflows/claude.yml` (176 lines) - Access-controlled Claude Code integration

**Configuration:**
- `.github/claude-authorized-users.yml` (54 lines) - Authorization config with emergency circuit breaker

**Scripts:**
- `.github/scripts/format-pr-comment.ps1` - Markdown PR comment formatter
- `.github/scripts/check-dependencies.ps1` - Dependency vulnerability scanner
- `.github/scripts/check-path-security.ps1` - Path traversal detection
- `.github/scripts/check-spec-compliance.ps1` - SpecKit compliance validator

**Templates:**
- `.github/ISSUE_TEMPLATE/bug_report.yml` - Structured bug report form
- `.github/ISSUE_TEMPLATE/feature_request.yml` - Feature request form
- `.github/ISSUE_TEMPLATE/community_contribution.yml` - Community template
- `.github/ISSUE_TEMPLATE/config.yml` - Template configuration
- `.github/pull_request_template.md` - PR checklist template

**Community Files:**
- `CONTRIBUTING.md` (>100 lines) - Comprehensive contribution guide
- `SECURITY.md` - Security policy and vulnerability reporting

### Target Repository Files (Destination)
**Location:** `/mnt/c/Users/bobby/src/claude/net10-project-example/`

**Existing:**
- `.gitignore` - Already has Claude Code exclusions
- `README.md` - Project overview
- `CLAUDE.md` - Development guidance with Claude Code infrastructure docs
- `.claude/` - Claude Code infrastructure (skills, hooks, agents)
- `dev/active/` and `dev/archive/` - Development documentation
- `src/` - Example.Web (MVC) and Example.API (Minimal APIs)
- `tests/` - MSTest + Playwright tests
- `Directory.Build.props` - Shared MSBuild properties
- `Directory.Packages.props` - Centralized Package Management
- `global.json` - SDK version and test runner config

**To Create:**
- `.github/` directory (entire structure)
- `CONTRIBUTING.md`
- `SECURITY.md`
- `CODE_OF_CONDUCT.md`

---

## Key Decisions

### 1. Workflow Strategy
**Decision:** Adapt 6-step PR validation pipeline to .NET ecosystem
**Rationale:**
- Proven pattern from reference repository
- Comprehensive coverage of quality dimensions
- Clear separation of concerns
**Impact:** Need to replace PowerShell-specific tooling with .NET equivalents

### 2. Authorization Approach
**Decision:** Use same authorization system with claude-authorized-users.yml
**Rationale:**
- Language-agnostic
- Emergency circuit breaker is critical
- Prevents unauthorized Claude Code usage
**Impact:** Configuration file and logic can be reused nearly verbatim

### 3. .NET Validation Focus
**Decision:** Replace SpecKit compliance validation with .NET-specific checks
**Rationale:**
- Validate .csproj structure
- Enforce Centralized Package Management
- Check ImplicitUsings and Nullable compliance
- Validate test project configuration
**Impact:** Need custom PowerShell scripts for .NET validation

### 4. Multi-Platform CI
**Decision:** Add multi-platform build matrix (Linux, Windows, macOS)
**Rationale:**
- Reference repo is Windows-only
- .NET is cross-platform
- Better coverage and compatibility testing
**Impact:** Longer CI times, need platform-specific handling for Playwright

### 5. Code Review Tool
**Decision:** Replace PSScriptAnalyzer with `dotnet format`
**Rationale:**
- .NET's official formatting tool
- Enforces consistent code style
- Integrates with IDE tooling
**Impact:** Different output format, need to parse `dotnet format` results

### 6. Security Scanning Adaptations
**Decision:**
- Keep GitLeaks (language-agnostic)
- Replace PSScriptAnalyzer security rules with .NET analyzers
- Add NuGet vulnerability scanning
**Rationale:**
- GitLeaks works for any codebase
- .NET has robust security analyzers (SecurityCodeScan, built-in analyzers)
- NuGet has vulnerability database
**Impact:** Different tooling but similar output structure

### 7. Test Framework
**Decision:** MSTest with Microsoft.Testing.Platform (not VSTest)
**Rationale:**
- Already used in the project (per global.json)
- New test runner architecture
- Faster execution, better IDE integration
**Impact:** Use `dotnet test` or `dotnet run --project` (test project)

### 8. Comment Formatting
**Decision:** Reuse format-pr-comment.ps1 pattern
**Rationale:**
- Well-designed, flexible script
- Handles update-in-place via HTML markers
- Produces professional-looking PR comments
**Impact:** Can adapt with minimal changes

---

## Dependencies and Prerequisites

### GitHub Secrets Required
1. **CLAUDE_CODE_OAUTH_TOKEN**
   - Required for: Claude Code integration (claude.yml, pr-validation.yml Step 4)
   - Obtain from: Claude Code CLI or web interface
   - Priority: High (workflows fail without it, but marked as continue-on-error)

### GitHub Repository Settings
- Actions enabled
- Workflow permissions: Read and write
- Allow workflows to create/update PR comments
- Allow workflows to post status checks

### External Actions/Tools
- `actions/checkout@v4`
- `actions/setup-dotnet@v4`
- `actions/cache@v4`
- `actions/upload-artifact@v4`
- `actions/github-script@v7`
- `anthropics/claude-code-action@v1`
- `gitleaks/gitleaks-action@v2`

### Local Development Requirements
- PowerShell 7+ (for script development)
- .NET 10.0 SDK RC 2
- Git
- IDE or text editor

---

## Integration Points

### 1. Claude Code Infrastructure
**Location:** `.claude/` directory
**Integration:** Claude Code workflow references repository's Claude Code setup
**Note:** Workflows trigger Claude Code which already has skills and hooks configured

### 2. Dev Docs System
**Location:** `dev/active/` and `dev/archive/`
**Integration:** This task's documentation follows the same pattern
**Note:** Future tasks can reference this implementation plan

### 3. Existing Test Infrastructure
**Location:** `tests/` directory
**Integration:** CI workflows will execute existing tests
**Note:** No changes to test projects needed

### 4. Centralized Package Management
**Location:** `Directory.Packages.props`
**Integration:** Validation scripts will check compliance
**Note:** .csproj files must not have Version attributes

### 5. Build Configuration
**Location:** `Directory.Build.props`, `global.json`
**Integration:** CI workflows respect these settings
**Note:** SDK version pinned in global.json

---

## Risks and Mitigations

### High Priority Risks

**1. CLAUDE_CODE_OAUTH_TOKEN Secret Not Set**
- **Risk:** Workflows fail without secret
- **Impact:** Claude Code integration doesn't work
- **Mitigation:** Mark jobs as `continue-on-error: true`, provide setup docs
- **Status:** Addressed in plan

**2. Authorization Blocks External Contributors**
- **Risk:** Discourages community involvement
- **Impact:** Fewer contributions
- **Mitigation:** Clear docs on becoming authorized, easy allowlist addition
- **Status:** Addressed in plan

**3. PowerShell Scripts Fail on Linux CI**
- **Risk:** Scripts not cross-platform compatible
- **Impact:** CI fails on Ubuntu runners
- **Mitigation:** Use PowerShell Core (pwsh), test locally on Linux
- **Status:** Needs validation during implementation

### Medium Priority Risks

**4. Playwright Browser Installation Fails**
- **Risk:** Browser downloads timeout or fail
- **Impact:** E2E tests can't run
- **Mitigation:** Cache browsers, add retry logic, make non-blocking
- **Status:** Addressed in plan

**5. Security Scans Produce False Positives**
- **Risk:** Legitimate code flagged as insecure
- **Impact:** PR validation fails unnecessarily
- **Mitigation:** Non-blocking scans, suppression mechanism, clear docs
- **Status:** Addressed in plan

**6. High CI Resource Usage**
- **Risk:** Matrix builds consume too many minutes
- **Impact:** Exceeds free tier limits
- **Mitigation:** Run full matrix only on main, cache aggressively
- **Status:** Addressed in plan

### Low Priority Risks

**7. .NET SDK Version Mismatch**
- **Risk:** CI uses different SDK than developers
- **Impact:** Build failures, inconsistent behavior
- **Mitigation:** Pin SDK in global.json, use setup-dotnet action
- **Status:** Already mitigated by existing global.json

**8. Breaking Changes to GitHub Actions**
- **Risk:** Actions updated with breaking changes
- **Impact:** Workflows break without notice
- **Mitigation:** Pin action versions, monitor changelog
- **Status:** Standard practice

---

## Adaptation Strategy

### From PowerShell to .NET

| PowerShell Ecosystem | .NET Ecosystem | Adaptation Notes |
|---------------------|----------------|------------------|
| PSScriptAnalyzer | `dotnet format --verify-no-changes` | Check code formatting |
| Pester tests | `dotnet test` | Execute unit tests |
| PowerShell modules | NuGet packages | Check for vulnerabilities |
| .ps1/.psm1 files | .cs files | Source files to scan |
| PSScriptAnalyzer security rules | .NET security analyzers | SecurityCodeScan, Roslyn analyzers |
| SpecKit compliance | CPM/global.json compliance | Custom validation scripts |

### Workflow Adaptations

1. **Keep Identical:**
   - Authorization logic
   - PR comment formatting pattern
   - Workflow job structure (6 steps)
   - HTML markers for update-in-place
   - JSON result format

2. **Adapt Significantly:**
   - Quality checks (Step 3): Replace linter/test tools
   - .NET validation (Step 6): Replace SpecKit with CPM checks
   - CI/CD: Add multi-platform matrix

3. **New Additions:**
   - NuGet vulnerability scanning
   - .csproj structure validation
   - Playwright browser installation
   - Cross-platform build matrix

---

## Implementation Checkpoints

### Checkpoint 1: Templates Ready
- [ ] `.github/` directory structure created
- [ ] Issue templates created and tested (create test issues)
- [ ] PR template created and tested (create test PR)
- [ ] CODE_OF_CONDUCT.md present

**Validation:** Create a test issue and test PR to verify templates render correctly

---

### Checkpoint 2: Authorization Working
- [ ] `claude-authorized-users.yml` configured
- [ ] Claude Code workflow created
- [ ] Authorization tested with @claude mention
- [ ] Emergency circuit breaker tested

**Validation:**
1. Mention @claude in an issue/PR as authorized user (should succeed)
2. Enable circuit breaker, test again (should fail)
3. Disable circuit breaker, test again (should succeed)

---

### Checkpoint 3: PR Validation Core (Steps 1-3)
- [ ] PR validation workflow created
- [ ] Authorization check works
- [ ] Guardrails check works (test with large PR)
- [ ] Quality checks work (test with formatting violations)
- [ ] PR comments are posted correctly

**Validation:**
1. Create PR with formatting violations (should fail Step 3)
2. Create PR >2000 lines (should fail/warn Step 2)
3. Create PR with short description (should warn Step 2)
4. Fix issues, push, verify comments update

---

### Checkpoint 4: Code Review Integration
- [ ] Claude Code review step added
- [ ] OIDC authentication configured
- [ ] Review comments posted to PR
- [ ] Works correctly when secret is present
- [ ] Gracefully skips when secret is absent

**Validation:**
1. With secret: Create PR, verify Claude Code reviews it
2. Without secret: Create PR, verify step skips gracefully

---

### Checkpoint 5: Security Scanning
- [ ] GitLeaks running
- [ ] .NET security analyzers running
- [ ] NuGet vulnerability scan running
- [ ] Path security check running
- [ ] Results aggregated and posted

**Validation:**
1. Commit a test secret (should be detected by GitLeaks)
2. Add vulnerable package version (should be detected)
3. Verify PR comment shows security findings

---

### Checkpoint 6: .NET Validation
- [ ] .csproj structure check working
- [ ] CPM compliance check working
- [ ] global.json check working
- [ ] Results posted to PR

**Validation:**
1. Add PackageReference with Version attribute (should fail)
2. Add duplicate PackageVersion (should warn)
3. Fix issues, verify PR comment updates

---

### Checkpoint 7: CI/CD Complete
- [ ] Multi-platform builds working
- [ ] Tests passing on all platforms
- [ ] Playwright tests running
- [ ] Artifacts uploaded
- [ ] Coverage reporting working (if implemented)

**Validation:**
1. Push to main branch
2. Verify builds on Linux, Windows, macOS all pass
3. Check artifacts are uploaded
4. Review test results

---

### Checkpoint 8: Documentation Complete
- [ ] CONTRIBUTING.md published
- [ ] SECURITY.md published
- [ ] Workflow README created
- [ ] Scripts documented
- [ ] Main README updated
- [ ] CLAUDE.md updated

**Validation:**
1. Read through all documentation as a new contributor
2. Verify all links work
3. Test workflows as documented

---

## Testing Strategy

### Unit Testing (Scripts)
- Test PowerShell scripts locally before committing
- Use mock JSON inputs for format-pr-comment.ps1
- Validate regex patterns in security/validation scripts
- Test edge cases (empty results, large result sets, errors)

### Integration Testing (Workflows)
- Create feature branch for workflow development
- Test each phase incrementally
- Create "test PRs" to validate PR validation workflow
- Use draft PRs to avoid noise in main branch
- Test with different user roles (owner, collaborator, external)

### End-to-End Testing
- Full PR workflow:
  1. Fork repository (or use secondary account)
  2. Create feature branch
  3. Make changes (intentionally introduce issues)
  4. Create PR
  5. Verify all 6 validation steps run
  6. Fix issues based on PR comments
  7. Verify comments update
  8. Merge PR
  9. Verify CI/CD runs on main branch

### Platform Testing
- Test workflows on all matrix platforms
- Verify Playwright works on each platform
- Check for platform-specific failures
- Validate artifact uploads from each platform

---

## Rollout Plan

### Phase 1: Silent Testing (Week 1)
- Implement templates and workflows on feature branch
- Test extensively without affecting main branch
- Invite trusted collaborators to test workflows
- Iterate on feedback

### Phase 2: Soft Launch (Week 2)
- Merge to main branch
- Announce in README that PR validation is active
- Monitor first few PRs closely
- Be ready to disable circuit breaker if issues arise

### Phase 3: Refinement (Week 3-4)
- Adjust thresholds based on real usage
- Fix false positives
- Optimize CI times
- Improve error messages

### Phase 4: Documentation Push (Week 5-6)
- Complete all documentation
- Announce to community
- Update links and badges
- Mark as production-ready

---

## Success Criteria

### Must Have (MVP)
- ✅ Issue and PR templates functional
- ✅ Authorization system prevents unauthorized Claude Code usage
- ✅ PR validation catches formatting violations
- ✅ PR validation runs unit tests
- ✅ PR comments are clear and actionable
- ✅ Security scans detect secrets

### Should Have (Full Feature)
- ✅ All 6 PR validation steps implemented
- ✅ Claude Code review working
- ✅ NuGet vulnerability scanning
- ✅ .NET-specific validation (.csproj, CPM)
- ✅ Multi-platform CI builds
- ✅ CONTRIBUTING.md and SECURITY.md published

### Nice to Have (Polish)
- ✅ Test coverage reporting
- ✅ Build artifact publishing
- ✅ Comprehensive script documentation
- ✅ GitHub Actions status badges
- ✅ Docker image builds (optional)

---

## Quick Resume Instructions

**If this task is interrupted and needs to be resumed:**

1. **Read this file** to understand current state and decisions
2. **Check tasks file** (`002-github-integration-tasks.md`) for progress
3. **Review plan file** (`002-github-integration-plan.md`) for detailed tasks
4. **Identify last checkpoint** completed in this file
5. **Continue from next incomplete checkpoint**
6. **Update progress** in tasks file as work is completed
7. **Update this context file** with any new decisions or risks discovered

---

## Notes and Observations

- Reference repository is PowerShell-focused; significant adaptation required for .NET
- Authorization system is language-agnostic and can be reused verbatim
- PR comment formatting pattern is well-designed and flexible
- 6-step validation pipeline provides comprehensive quality coverage
- Multi-platform testing is critical for .NET but adds complexity
- Claude Code integration requires secret setup (document this clearly)
- Emergency circuit breaker is a critical safety feature (keep it)
- Test locally on Linux before committing (many devs use WSL/Ubuntu)

---

**END OF CONTEXT**
