# GitHub Integration Implementation Plan

**Last Updated:** 2025-11-02

**Reference Repository:** claude-win11-speckit-safe-update-skill
**Target Repository:** net10-project-example (.NET 10 RC 2 project)

---

## Executive Summary

This plan outlines the implementation of comprehensive GitHub integration features for the .NET 10 project example repository, modeled after the production-grade setup in the claude-win11-speckit-safe-update-skill repository. The integration will provide automated PR validation, access-controlled Claude Code integration, security scanning, issue/PR templates, and comprehensive CI/CD workflows.

**Goals:**
- Establish professional OSS project standards
- Automate quality checks and security scanning
- Enable controlled Claude Code integration via @mentions
- Provide structured contribution guidelines
- Implement multi-stage PR validation pipeline

**Expected Outcomes:**
- 6-step PR validation workflow with detailed feedback
- Access-controlled Claude Code integration
- Comprehensive issue and PR templates
- Security and quality automation
- Professional contribution workflow

---

## Current State Analysis

### Repository State
**Environment:** WSL2 (Ubuntu) with .NET 10.0 RC 2

**What Exists:**
- ✅ `.gitignore` with Claude Code exclusions
- ✅ `README.md` with project overview
- ✅ `CLAUDE.md` with development guidance and Claude Code infrastructure documentation
- ✅ Claude Code infrastructure (.claude/ directory with skills, hooks, agents)
- ✅ dev/active/ and dev/archive/ for development documentation
- ✅ .NET 10 solution with MVC and API projects
- ✅ MSTest with Microsoft.Testing.Platform
- ✅ Playwright E2E testing setup

**What's Missing:**
- ❌ `.github/` directory (not present)
- ❌ GitHub Actions workflows
- ❌ Issue and PR templates
- ❌ Access control configuration
- ❌ PR validation scripts
- ❌ CI/CD automation
- ❌ `CONTRIBUTING.md`
- ❌ `SECURITY.md`
- ❌ `CODE_OF_CONDUCT.md`

**Gap Analysis:**
The reference repository (claude-win11-speckit-safe-update-skill) has a sophisticated GitHub integration with:
1. **2 workflow files** - PR validation (869 lines), Claude Code integration (176 lines)
2. **Authorization config** - claude-authorized-users.yml (54 lines)
3. **5 issue templates** - Bug report, feature request, community contribution, config
4. **1 PR template** - Structured checklist format
5. **4 validation scripts** - PowerShell scripts for dependencies, path security, spec compliance, PR comment formatting
6. **Community health files** - CONTRIBUTING.md, SECURITY.md

---

## Proposed Future State

### Directory Structure
```
.github/
├── workflows/
│   ├── pr-validation.yml          # 6-step PR validation pipeline
│   ├── claude.yml                 # Access-controlled Claude Code integration
│   ├── dotnet-ci.yml              # .NET build and test workflow
│   └── README.md                  # Workflow documentation
├── scripts/
│   ├── check-dotnet-formatting.ps1
│   ├── check-test-coverage.ps1
│   ├── check-csproj-structure.ps1
│   ├── format-pr-comment.ps1
│   └── README.md
├── ISSUE_TEMPLATE/
│   ├── bug_report.yml
│   ├── feature_request.yml
│   ├── documentation.yml
│   └── config.yml
├── pull_request_template.md
└── claude-authorized-users.yml
```

### Additional Repository Files
```
CONTRIBUTING.md                     # Contribution guidelines
SECURITY.md                         # Security policy
CODE_OF_CONDUCT.md                 # Code of conduct
```

---

## Implementation Phases

### **Phase 1: Foundation & Templates**
**Goal:** Establish basic GitHub infrastructure and templates

**Deliverables:**
- `.github/` directory structure
- Issue templates (bug, feature, documentation)
- PR template with .NET-specific checklist
- Basic community health files

**Dependencies:** None

**Estimated Effort:** Small (2-3 hours)

---

### **Phase 2: Authorization & Access Control**
**Goal:** Implement access-controlled Claude Code integration

**Deliverables:**
- `claude-authorized-users.yml` configuration
- Basic Claude Code workflow with authorization
- Emergency circuit breaker controls
- Audit logging

**Dependencies:** Phase 1 complete

**Estimated Effort:** Medium (4-6 hours)

---

### **Phase 3: PR Validation Pipeline - Core**
**Goal:** Implement Steps 1-3 of PR validation (Authorization, Guardrails, Quality Checks)

**Deliverables:**
- PR validation workflow (Steps 1-3)
- Authorization check job
- PR size/description guardrails
- .NET linting and unit test execution
- PR comment formatting system

**Dependencies:** Phase 2 complete

**Estimated Effort:** Large (8-10 hours)

---

### **Phase 4: Code Review Integration**
**Goal:** Add Claude Code review step to PR pipeline

**Deliverables:**
- Claude Code review job (Step 4)
- OIDC authentication setup
- .NET-specific review prompts
- Integration with authorization system

**Dependencies:** Phase 3 complete

**Estimated Effort:** Medium (3-4 hours)

---

### **Phase 5: Security Scanning**
**Goal:** Implement comprehensive security checks (Step 5)

**Deliverables:**
- GitLeaks secret scanning
- .NET security analyzer rules
- NuGet package vulnerability scan
- Path traversal detection (adapted for .NET)
- Security result aggregation

**Dependencies:** Phase 4 complete

**Estimated Effort:** Large (6-8 hours)

---

### **Phase 6: .NET-Specific Validation**
**Goal:** Add .NET project validation (Step 6)

**Deliverables:**
- .csproj structure validation
- Centralized Package Management compliance
- global.json consistency check
- Test project configuration validation
- ImplicitUsings compliance check

**Dependencies:** Phase 5 complete

**Estimated Effort:** Medium (4-5 hours)

---

### **Phase 7: CI/CD & Build Pipeline**
**Goal:** Implement .NET build/test/publish workflow

**Deliverables:**
- Multi-platform build workflow (Linux, Windows, macOS)
- Test execution with coverage
- Playwright browser installation
- Build artifact publishing
- Docker image build (optional)

**Dependencies:** Phases 1-6 complete

**Estimated Effort:** Large (6-8 hours)

---

### **Phase 8: Documentation & Polish**
**Goal:** Complete documentation and refinement

**Deliverables:**
- CONTRIBUTING.md with .NET workflow
- SECURITY.md with vulnerability reporting
- CODE_OF_CONDUCT.md
- Workflow README documentation
- Script documentation
- Update main README.md

**Dependencies:** All previous phases complete

**Estimated Effort:** Medium (3-4 hours)

---

## Detailed Task Breakdown

### Phase 1: Foundation & Templates

#### Task 1.1: Create Directory Structure
**Effort:** S
**Priority:** P0 (Critical)

**Acceptance Criteria:**
- [ ] `.github/` directory created at repository root
- [ ] `.github/workflows/` subdirectory exists
- [ ] `.github/scripts/` subdirectory exists
- [ ] `.github/ISSUE_TEMPLATE/` subdirectory exists
- [ ] Directory structure matches planned layout

**Dependencies:** None

---

#### Task 1.2: Create Issue Templates
**Effort:** M
**Priority:** P0 (Critical)

**Acceptance Criteria:**
- [ ] `bug_report.yml` created with .NET-specific fields
  - Problem description
  - Steps to reproduce
  - Expected vs actual behavior
  - Environment (OS, .NET SDK version, IDE)
  - Build output or error messages
- [ ] `feature_request.yml` created
  - Problem statement
  - Proposed solution
  - User experience description
  - Implementation details (optional)
  - Acceptance criteria
- [ ] `documentation.yml` template created
  - Documentation type (README, API docs, tutorial)
  - Current state
  - Proposed changes
- [ ] `config.yml` configured with contact links

**Dependencies:** Task 1.1

**Reference Files:**
- `claude-win11-speckit-safe-update-skill/.github/ISSUE_TEMPLATE/bug_report.yml`
- `claude-win11-speckit-safe-update-skill/.github/ISSUE_TEMPLATE/feature_request.yml`

---

#### Task 1.3: Create PR Template
**Effort:** S
**Priority:** P0 (Critical)

**Acceptance Criteria:**
- [ ] `pull_request_template.md` created
- [ ] Includes description section
- [ ] Type of change checkboxes (bug fix, feature, breaking change, docs)
- [ ] Testing description section
- [ ] .NET-specific checklist:
  - [ ] Tests added/updated
  - [ ] All tests passing (`dotnet test`)
  - [ ] Build succeeds (`dotnet build`)
  - [ ] Code formatting applied
  - [ ] No ImplicitUsings violations (if applicable)
  - [ ] Package versions in Directory.Packages.props
  - [ ] Documentation updated
  - [ ] CHANGELOG.md updated (if applicable)
- [ ] Related issues section

**Dependencies:** Task 1.1

**Reference Files:**
- `claude-win11-speckit-safe-update-skill/.github/pull_request_template.md`

---

#### Task 1.4: Create CODE_OF_CONDUCT.md
**Effort:** S
**Priority:** P1 (High)

**Acceptance Criteria:**
- [ ] `CODE_OF_CONDUCT.md` created at repository root
- [ ] Uses Contributor Covenant v2.1 or similar standard
- [ ] Contact information specified
- [ ] Enforcement guidelines included

**Dependencies:** None

---

### Phase 2: Authorization & Access Control

#### Task 2.1: Create Authorization Configuration
**Effort:** M
**Priority:** P0 (Critical)

**Acceptance Criteria:**
- [ ] `.github/claude-authorized-users.yml` created
- [ ] Configuration includes:
  - [ ] `authorized_users` list with repository owner
  - [ ] `settings` section with flags:
    - `allow_collaborators: true`
    - `allow_org_members: true`
    - `allow_owner: true`
    - `block_first_time_contributors: true`
    - `block_external_contributors: true`
  - [ ] `audit` section with logging enabled
  - [ ] `emergency` section with circuit breaker (disabled by default)
  - [ ] `rate_limiting` section (future feature placeholder)
- [ ] Comments explain each setting
- [ ] Owner GitHub username matches repository owner

**Dependencies:** Task 1.1

**Reference Files:**
- `claude-win11-speckit-safe-update-skill/.github/claude-authorized-users.yml`

---

#### Task 2.2: Create Claude Code Workflow
**Effort:** M
**Priority:** P0 (Critical)

**Acceptance Criteria:**
- [ ] `.github/workflows/claude.yml` created
- [ ] Triggers on:
  - `issue_comment` (created)
  - `pull_request_review_comment` (created)
  - `issues` (opened, assigned)
  - `pull_request_review` (submitted)
- [ ] Only runs when `@claude` is mentioned
- [ ] Two jobs:
  - **authorize**: Checks user authorization against config
  - **claude**: Runs Claude Code (only if authorized)
- [ ] Authorization logic:
  - Loads `.github/claude-authorized-users.yml`
  - Checks emergency circuit breaker
  - Checks owner/collaborator/member status
  - Checks explicit allowlist
  - Outputs authorization result
- [ ] Claude Code action configured with:
  - `claude_code_oauth_token` secret
  - `additional_permissions: actions: read`
  - Proper OIDC permissions
- [ ] Logging for authorized and unauthorized attempts

**Dependencies:** Task 2.1

**Reference Files:**
- `claude-win11-speckit-safe-update-skill/.github/workflows/claude.yml`

---

### Phase 3: PR Validation Pipeline - Core

#### Task 3.1: Create PR Validation Workflow Structure
**Effort:** M
**Priority:** P0 (Critical)

**Acceptance Criteria:**
- [ ] `.github/workflows/pr-validation.yml` created
- [ ] Triggers on PR events:
  - `opened`, `synchronize`, `reopened`, `edited`
  - Branches: `main`, `develop`
- [ ] Concurrency control configured (cancel in-progress runs)
- [ ] Job dependency chain established:
  - authorization → guardrails → quality-checks → code-review → security-review → dotnet-validation → validation-complete
- [ ] Each job has clear naming (1️⃣, 2️⃣, 3️⃣, etc.)
- [ ] `validation-complete` job creates final summary table

**Dependencies:** Phase 2 complete

---

#### Task 3.2: Implement Authorization Check (Step 1)
**Effort:** S
**Priority:** P0 (Critical)

**Acceptance Criteria:**
- [ ] Job: `authorization` created
- [ ] Checks user via same logic as Claude workflow
- [ ] Loads `.github/claude-authorized-users.yml`
- [ ] Checks emergency circuit breaker
- [ ] Validates user role (OWNER, COLLABORATOR, MEMBER, ALLOWLIST)
- [ ] Outputs: `authorized`, `actor`, `association`, `reason`
- [ ] Creates step summary with authorization result
- [ ] Fails pipeline if not authorized

**Dependencies:** Task 3.1

**Reference Files:**
- `claude-win11-speckit-safe-update-skill/.github/workflows/pr-validation.yml` (lines 17-110)

---

#### Task 3.3: Implement PR Guardrails (Step 2)
**Effort:** M
**Priority:** P0 (Critical)

**Acceptance Criteria:**
- [ ] Job: `guardrails` created, depends on `authorization`
- [ ] **PR size check:**
  - Calculates lines added + removed
  - Max size: 2000 lines
  - Owner bypass allowed (warning instead of error)
  - Files changed count tracked
- [ ] **PR description check:**
  - Minimum 20 characters required
  - Non-blocking (warning only)
- [ ] Results formatted as JSON with:
  - `step`: "guardrails"
  - `status`: "pass" | "warning" | "failed"
  - `timestamp`: ISO 8601 UTC
  - `findings`: array of issues
  - `summary`: counts by severity
- [ ] PR comment created/updated with formatted results
- [ ] Step summary generated

**Dependencies:** Task 3.2, Task 3.6 (comment formatter)

**Reference Files:**
- `claude-win11-speckit-safe-update-skill/.github/workflows/pr-validation.yml` (lines 114-294)

---

#### Task 3.4: Implement Quality Checks (Step 3)
**Effort:** L
**Priority:** P0 (Critical)

**Acceptance Criteria:**
- [ ] Job: `quality-checks` created, depends on `guardrails`
- [ ] Runs on: `ubuntu-latest` (can use dotnet CLI cross-platform)
- [ ] **Code formatting check:**
  - Runs `dotnet format --verify-no-changes`
  - Captures formatting violations
  - Returns file paths and line numbers
- [ ] **Build check:**
  - Runs `dotnet build --no-restore`
  - Captures compilation errors/warnings
  - Treats warnings as errors per project config
- [ ] **Unit test execution:**
  - Runs `dotnet test --no-build --verbosity normal`
  - Captures test failures with details
  - Includes test count and pass/fail summary
- [ ] Results aggregated into JSON format matching guardrails structure
- [ ] PR comment created/updated
- [ ] Step summary generated
- [ ] Caches NuGet packages for performance

**Dependencies:** Task 3.3

**Adaptations from Reference:**
- Replace PSScriptAnalyzer with `dotnet format`
- Replace Pester tests with `dotnet test`
- Add NuGet restore and caching

---

#### Task 3.5: Create Format PR Comment Script
**Effort:** M
**Priority:** P0 (Critical)

**Acceptance Criteria:**
- [ ] `.github/scripts/format-pr-comment.ps1` created
- [ ] Parameters:
  - `InputJson`: Validation result JSON string
  - `StepNumber`: 2-6
  - `StepName`: Human-readable step name
  - `Emoji`: Status emoji code
  - `MaxFindings`: Truncation limit (default: 100)
  - `RunUrl`: GitHub Actions run URL
- [ ] Generates Markdown with:
  - HTML marker for update-in-place: `<!-- pr-validation:step-N -->`
  - Header with step number, name, emoji
  - Status indicator (✅/⚠️/❌)
  - Summary section (total, errors, warnings, info)
  - Findings grouped by category
  - Each finding includes: severity, file, line, rule, message, remediation
  - Truncation notice if >MaxFindings
  - Link to full workflow logs
  - Timestamp
- [ ] Returns formatted Markdown string

**Dependencies:** Task 1.1 (scripts directory exists)

**Reference Files:**
- `claude-win11-speckit-safe-update-skill/.github/scripts/format-pr-comment.ps1`

---

#### Task 3.6: Integrate PR Comment System
**Effort:** M
**Priority:** P0 (Critical)

**Acceptance Criteria:**
- [ ] Each validation step (2-6) calls `format-pr-comment.ps1`
- [ ] Uses `actions/github-script@v7` to post/update comments
- [ ] Comment update logic:
  - Searches for existing comment with matching HTML marker
  - Updates existing comment if found
  - Creates new comment if not found
- [ ] Comments are distinct per step (separate comments for each step)
- [ ] Comments include direct links to workflow run
- [ ] Error handling for API failures

**Dependencies:** Task 3.5

**Reference Files:**
- `claude-win11-speckit-safe-update-skill/.github/workflows/pr-validation.yml` (comment posting sections)

---

### Phase 4: Code Review Integration

#### Task 4.1: Add Claude Code Review Job (Step 4)
**Effort:** M
**Priority:** P1 (High)

**Acceptance Criteria:**
- [ ] Job: `code-review` created, depends on `quality-checks`
- [ ] Marked as `continue-on-error: true` (optional if secret not configured)
- [ ] Uses `anthropics/claude-code-action@v1`
- [ ] Configured with:
  - `claude_code_oauth_token` secret
  - `additional_permissions: actions: read`
  - OIDC permissions: `id-token: write`
- [ ] Custom prompt includes .NET-specific review points:
  - Code quality and best practices
  - .NET conventions and patterns
  - Proper use of async/await
  - Error handling and null safety
  - Test coverage and quality
  - Documentation completeness
  - Breaking changes in API surface
  - ImplicitUsings compliance
  - Centralized package management compliance
- [ ] Results posted to PR as review comment
- [ ] Integrates with PR comment system (if applicable)

**Dependencies:** Phase 3 complete

**Reference Files:**
- `claude-win11-speckit-safe-update-skill/.github/workflows/pr-validation.yml` (lines 482-510)

---

### Phase 5: Security Scanning

#### Task 5.1: Implement Secret Scanning
**Effort:** S
**Priority:** P1 (High)

**Acceptance Criteria:**
- [ ] Job: `security-review` created, depends on `code-review`
- [ ] Uses `gitleaks/gitleaks-action@v2`
- [ ] Scans entire PR diff for secrets
- [ ] Configured to detect:
  - API keys
  - Connection strings
  - Private keys
  - Authentication tokens
  - Passwords in plain text
- [ ] Results added to security findings array
- [ ] Marked as `continue-on-error: true` (non-blocking)

**Dependencies:** Phase 4 complete

**Reference Files:**
- `claude-win11-speckit-safe-update-skill/.github/workflows/pr-validation.yml` (lines 530-535)

---

#### Task 5.2: Implement .NET Security Analyzer
**Effort:** M
**Priority:** P1 (High)

**Acceptance Criteria:**
- [ ] Runs `dotnet format analyzers` with security rules
- [ ] Security-specific rules enabled:
  - SQL injection vulnerabilities
  - XSS vulnerabilities
  - Path traversal issues
  - Insecure deserialization
  - Hardcoded credentials
  - Weak cryptography usage
  - Insufficient logging
- [ ] Excludes test projects from security scanning (optional)
- [ ] Results added to security findings array
- [ ] Each finding includes: file, line, rule ID, message, severity

**Dependencies:** Task 5.1

**Adaptations from Reference:**
- Replace PSScriptAnalyzer security rules with .NET analyzers
- Use Roslyn security analyzers
- Consider SecurityCodeScan.NET6+ NuGet package

---

#### Task 5.3: Implement NuGet Vulnerability Scan
**Effort:** M
**Priority:** P1 (High)

**Acceptance Criteria:**
- [ ] `.github/scripts/check-dotnet-vulnerabilities.ps1` created
- [ ] Script checks all projects for vulnerable packages:
  - Parses Directory.Packages.props
  - Uses `dotnet list package --vulnerable` command
  - Aggregates vulnerability reports
- [ ] Queries NuGet vulnerability database
- [ ] Returns JSON with findings:
  - Package name
  - Current version
  - Vulnerable version range
  - Severity (low, moderate, high, critical)
  - Advisory URL
  - Recommended version
- [ ] Integrated into security-review job
- [ ] Results added to security findings array

**Dependencies:** Task 5.2

**Reference Files:**
- `claude-win11-speckit-safe-update-skill/.github/scripts/check-dependencies.ps1` (adapt for .NET)

---

#### Task 5.4: Implement Path Security Check
**Effort:** S
**Priority:** P2 (Medium)

**Acceptance Criteria:**
- [ ] `.github/scripts/check-path-security.ps1` created (or adapted)
- [ ] Scans for insecure path operations in C# code:
  - Unsafe Path.Combine usage
  - User input in file paths without validation
  - Directory traversal patterns (`../`, `..\\`)
  - Hardcoded absolute paths
- [ ] Uses regex patterns to detect issues
- [ ] Returns JSON with findings
- [ ] Integrated into security-review job

**Dependencies:** Task 5.3

**Reference Files:**
- `claude-win11-speckit-safe-update-skill/.github/scripts/check-path-security.ps1` (adapt for C#)

---

#### Task 5.5: Aggregate Security Results
**Effort:** M
**Priority:** P1 (High)

**Acceptance Criteria:**
- [ ] Security-review job aggregates results from:
  - GitLeaks
  - .NET Security Analyzer
  - NuGet vulnerability scan
  - Path security check
- [ ] Combined JSON output with:
  - All findings merged
  - Summary counts by severity
  - Overall status (pass/warning/failed)
  - Timestamp
- [ ] Formatted PR comment created via `format-pr-comment.ps1`
- [ ] Step summary generated
- [ ] Handles partial failures gracefully

**Dependencies:** Tasks 5.1-5.4

**Reference Files:**
- `claude-win11-speckit-safe-update-skill/.github/workflows/pr-validation.yml` (lines 605-641)

---

### Phase 6: .NET-Specific Validation

#### Task 6.1: Create .csproj Structure Validator
**Effort:** M
**Priority:** P1 (High)

**Acceptance Criteria:**
- [ ] `.github/scripts/check-csproj-structure.ps1` created
- [ ] Validates all `.csproj` files for:
  - **PackageReference without Version attributes** (CPM compliance)
  - **Proper PropertyGroup structure**
  - **EnableMSTestRunner and OutputType for test projects**
  - **No direct ImplicitUsings=true** (should inherit from Directory.Build.props)
  - **No direct Nullable settings** (should inherit from Directory.Build.props)
  - **TreatWarningsAsErrors consistency**
- [ ] Returns JSON findings array
- [ ] Each finding includes:
  - File path
  - Line number (if applicable)
  - Rule violated
  - Message
  - Remediation suggestion

**Dependencies:** Phase 5 complete

**Reference Files:**
- `claude-win11-speckit-safe-update-skill/.github/scripts/check-spec-compliance.ps1` (adapt for .NET)
- Project's `CLAUDE.md` for specific rules

---

#### Task 6.2: Create Centralized Package Management Validator
**Effort:** S
**Priority:** P1 (High)

**Acceptance Criteria:**
- [ ] Script validates `Directory.Packages.props`:
  - All `PackageVersion` elements have valid versions
  - No duplicate package entries
  - Versions use proper semantic versioning
- [ ] Cross-references with `.csproj` files:
  - All `PackageReference` elements exist in Directory.Packages.props
  - No `PackageReference` has Version attribute
- [ ] Returns JSON findings array
- [ ] Integrated into dotnet-validation job

**Dependencies:** Task 6.1

---

#### Task 6.3: Create global.json Consistency Check
**Effort:** S
**Priority:** P2 (Medium)

**Acceptance Criteria:**
- [ ] Script validates `global.json`:
  - SDK version pinned
  - `rollForward` policy set
  - Test runner configured (`"test": { "runner": "Microsoft.Testing.Platform" }`)
- [ ] Warns if SDK version is outdated (non-blocking)
- [ ] Returns JSON findings array

**Dependencies:** Task 6.1

---

#### Task 6.4: Implement .NET Validation Job (Step 6)
**Effort:** M
**Priority:** P1 (High)

**Acceptance Criteria:**
- [ ] Job: `dotnet-validation` created, depends on `security-review`
- [ ] Runs on: `ubuntu-latest`
- [ ] Executes all validation scripts:
  - `.csproj` structure check
  - Centralized Package Management check
  - `global.json` consistency check
- [ ] Aggregates results into JSON format
- [ ] Formatted PR comment created via `format-pr-comment.ps1`
- [ ] Step summary generated
- [ ] Marked as `continue-on-error: true` (non-blocking)

**Dependencies:** Tasks 6.1-6.3

---

### Phase 7: CI/CD & Build Pipeline

#### Task 7.1: Create Multi-Platform Build Workflow
**Effort:** L
**Priority:** P1 (High)

**Acceptance Criteria:**
- [ ] `.github/workflows/dotnet-ci.yml` created
- [ ] Triggers on:
  - `push` to `main` and `develop` branches
  - `pull_request` to `main` and `develop` branches
  - Manual `workflow_dispatch`
- [ ] Matrix strategy for multiple platforms:
  - `ubuntu-latest` (Linux)
  - `windows-latest` (Windows)
  - `macos-latest` (macOS)
- [ ] Steps for each platform:
  - Checkout code
  - Setup .NET SDK (version from global.json)
  - Restore dependencies with caching
  - Build solution (`dotnet build`)
  - Run unit tests (`dotnet test`)
  - Install Playwright browsers (for E2E tests)
  - Run Playwright tests
  - Upload test results as artifacts
- [ ] Fail fast disabled to see all platform results
- [ ] Job summaries with test results

**Dependencies:** Phases 1-6 complete

---

#### Task 7.2: Add Test Coverage Reporting
**Effort:** M
**Priority:** P2 (Medium)

**Acceptance Criteria:**
- [ ] `.github/scripts/check-test-coverage.ps1` created
- [ ] Uses `dotnet test --collect:"XPlat Code Coverage"`
- [ ] Generates coverage reports (Cobertura format)
- [ ] Uploads coverage to Codecov or similar (optional)
- [ ] PR comment with coverage summary
- [ ] Coverage badge in README.md (optional)
- [ ] Thresholds configurable:
  - Minimum line coverage: 80% (warning)
  - Minimum branch coverage: 70% (warning)

**Dependencies:** Task 7.1

---

#### Task 7.3: Add Build Artifact Publishing
**Effort:** M
**Priority:** P2 (Medium)

**Acceptance Criteria:**
- [ ] Publishes build artifacts on successful builds to `main`
- [ ] Artifacts include:
  - Compiled binaries (Release configuration)
  - NuGet packages (if applicable)
  - Docker images (optional)
- [ ] Uses `actions/upload-artifact@v4`
- [ ] Retention period: 90 days
- [ ] Includes version information in artifact names

**Dependencies:** Task 7.1

---

#### Task 7.4: Add Docker Image Build (Optional)
**Effort:** L
**Priority:** P3 (Low)

**Acceptance Criteria:**
- [ ] Dockerfile created for Example.Web
- [ ] Dockerfile created for Example.API
- [ ] Multi-stage build for optimized images
- [ ] Images built in CI workflow
- [ ] Images tagged with:
  - Git SHA
  - Branch name
  - `latest` (for main branch)
- [ ] Pushed to GitHub Container Registry (optional)

**Dependencies:** Task 7.1

---

### Phase 8: Documentation & Polish

#### Task 8.1: Create CONTRIBUTING.md
**Effort:** M
**Priority:** P1 (High)

**Acceptance Criteria:**
- [ ] `CONTRIBUTING.md` created at repository root
- [ ] Sections include:
  - **Development Setup**
    - Prerequisites (.NET 10 SDK, IDE)
    - Clone and restore
    - Running locally
  - **Project Structure**
    - Directory layout
    - Key files explained
  - **Development Workflow**
    - Creating issues
    - Forking and branching
    - Commit message conventions
    - Running tests
    - Code formatting (`dotnet format`)
    - Creating pull requests
  - **Code Standards**
    - No ImplicitUsings
    - Centralized Package Management
    - MSTest with Microsoft.Testing.Platform
    - Test naming conventions
  - **PR Guidelines**
    - PR size limits
    - Description requirements
    - Checklist items
  - **Testing Requirements**
    - Unit tests required for new features
    - Playwright tests for UI changes
    - All tests must pass
  - **Getting Help**
    - Links to issues, discussions, docs

**Dependencies:** Phases 1-7 complete

**Reference Files:**
- `claude-win11-speckit-safe-update-skill/CONTRIBUTING.md`

---

#### Task 8.2: Create SECURITY.md
**Effort:** S
**Priority:** P1 (High)

**Acceptance Criteria:**
- [ ] `SECURITY.md` created at repository root
- [ ] Sections include:
  - **Supported Versions**
    - Table of supported .NET versions
    - Security update policy
  - **Reporting a Vulnerability**
    - Contact method (email or GitHub Security Advisories)
    - Expected response time
    - Disclosure policy
  - **Security Best Practices**
    - Keep dependencies updated
    - Follow .NET security guidelines
    - Use secrets management (not hardcoded)
  - **Known Issues** (if any)
    - List of known security considerations
    - Mitigation steps

**Dependencies:** None

**Reference Files:**
- `claude-win11-speckit-safe-update-skill/SECURITY.md`

---

#### Task 8.3: Create Workflow Documentation
**Effort:** S
**Priority:** P2 (Medium)

**Acceptance Criteria:**
- [ ] `.github/workflows/README.md` created
- [ ] Documents each workflow:
  - **pr-validation.yml**
    - Purpose and trigger conditions
    - 6-step pipeline explanation
    - Required secrets
    - How to skip steps (if applicable)
  - **claude.yml**
    - Authorization system explained
    - How to add authorized users
    - Emergency circuit breaker usage
  - **dotnet-ci.yml**
    - Build and test pipeline
    - Platform matrix strategy
    - Artifact publishing
- [ ] Includes troubleshooting section
- [ ] Links to GitHub Actions documentation

**Dependencies:** Phases 1-7 complete

---

#### Task 8.4: Create Scripts Documentation
**Effort:** S
**Priority:** P2 (Medium)

**Acceptance Criteria:**
- [ ] `.github/scripts/README.md` created
- [ ] Documents each script:
  - Purpose
  - Parameters
  - Output format (JSON structure)
  - Usage examples
  - Dependencies
- [ ] Includes development notes for extending scripts

**Dependencies:** Phases 3-6 complete

---

#### Task 8.5: Update Main README.md
**Effort:** S
**Priority:** P1 (High)

**Acceptance Criteria:**
- [ ] Add "Contributing" section with link to CONTRIBUTING.md
- [ ] Add "Security" section with link to SECURITY.md
- [ ] Add GitHub Actions status badges:
  - PR validation workflow
  - CI/CD workflow
  - Test coverage (if implemented)
- [ ] Add "Code of Conduct" section with link
- [ ] Add "License" section
- [ ] Update "Getting Started" to mention PR workflow

**Dependencies:** Tasks 8.1-8.3 complete

---

#### Task 8.6: Update CLAUDE.md
**Effort:** S
**Priority:** P1 (High)

**Acceptance Criteria:**
- [ ] Add "GitHub Integration" section
- [ ] Document PR validation pipeline:
  - 6-step process
  - What each step checks
  - How to interpret PR comments
- [ ] Document Claude Code integration:
  - How to trigger via @mentions
  - Authorization system
  - Emergency controls
- [ ] Add link to `.github/workflows/README.md` for details
- [ ] Note any .NET-specific validation rules

**Dependencies:** Phases 1-7 complete

---

## Risk Assessment and Mitigation Strategies

### Risk 1: CLAUDE_CODE_OAUTH_TOKEN Secret Not Configured
**Impact:** Claude Code integration (workflows) will fail
**Probability:** Medium
**Mitigation:**
- Mark Claude Code jobs as `continue-on-error: true`
- Provide clear documentation in workflows README
- Fallback to manual code review if not configured
- Add setup instructions in CONTRIBUTING.md

### Risk 2: PR Validation Blocks All External Contributors
**Impact:** Discourages community contributions
**Probability:** Medium
**Mitigation:**
- Make authorization configurable in `claude-authorized-users.yml`
- Allow easy addition of trusted contributors
- Provide clear "how to contribute" instructions
- Consider auto-approval for trivial changes (typos, docs)

### Risk 3: .NET SDK Version Mismatch in CI
**Impact:** Builds fail due to SDK differences
**Probability:** Low
**Mitigation:**
- Pin SDK version in global.json
- Use `actions/setup-dotnet@v4` to install exact version
- Document SDK requirements in README.md

### Risk 4: Playwright Browser Installation Fails
**Impact:** E2E tests cannot run
**Probability:** Medium
**Mitigation:**
- Cache Playwright browsers in CI
- Add retry logic for browser downloads
- Make Playwright tests non-blocking (warning only)
- Provide manual installation instructions

### Risk 5: Security Scans Generate False Positives
**Impact:** PR validation fails unnecessarily
**Probability:** Medium
**Mitigation:**
- Mark security scans as non-blocking (`continue-on-error: true`)
- Provide suppression mechanism (.gitleaksignore, analyzer suppressions)
- Document how to handle false positives
- Manual review for security warnings

### Risk 6: PowerShell Scripts Don't Work on Linux CI Runners
**Impact:** Scripts fail in ubuntu-latest jobs
**Probability:** Low
**Mitigation:**
- Use PowerShell Core (pwsh) which is cross-platform
- Test scripts locally on Linux/macOS
- Avoid Windows-specific cmdlets
- Fallback to bash scripts if needed

### Risk 7: High Resource Usage from Matrix Builds
**Impact:** CI runs take too long or exceed free tier limits
**Probability:** Medium
**Mitigation:**
- Run full matrix only on main branch
- Run single platform (Ubuntu) on PRs
- Use caching aggressively (NuGet packages, Playwright browsers)
- Consider self-hosted runners if needed

### Risk 8: Breaking Changes to GitHub Actions or Claude Code Action
**Impact:** Workflows break without notice
**Probability:** Low
**Mitigation:**
- Pin action versions (e.g., `@v4` not `@latest`)
- Monitor GitHub Actions changelog
- Test workflow changes on feature branches
- Have rollback plan (revert commits)

---

## Success Metrics

### Quantitative Metrics
1. **PR Validation Coverage:** 100% of PRs go through 6-step validation
2. **Median PR Review Time:** Reduced by 50% (automated feedback)
3. **Security Findings:** 0 secrets committed (GitLeaks catches all)
4. **Code Quality:** 0 formatting violations in production code
5. **Test Pass Rate:** 95%+ on all platforms
6. **Build Success Rate:** 98%+ on main branch
7. **False Positive Rate:** <10% for security scans

### Qualitative Metrics
1. **Contributor Experience:** Clear, actionable PR feedback
2. **Maintainer Experience:** Reduced manual review burden
3. **Code Confidence:** Automated checks provide safety net
4. **Documentation Quality:** Contributors understand workflow
5. **Community Health:** Professional OSS project appearance

### Milestone Indicators
- ✅ First PR validated through complete 6-step pipeline
- ✅ First authorized @claude mention successfully processed
- ✅ First security finding detected and resolved
- ✅ First multi-platform CI build succeeds
- ✅ First external contribution follows new workflow
- ✅ Documentation complete and reviewed

---

## Required Resources and Dependencies

### GitHub Secrets Required
1. **CLAUDE_CODE_OAUTH_TOKEN** (required for Claude Code integration)
   - Obtain from Claude Code CLI or web interface
   - Add to repository secrets
   - Used by: `claude.yml`, `pr-validation.yml` (Step 4)

### GitHub Permissions Required
1. **Repository Settings:**
   - Actions enabled
   - Read/write permissions for GITHUB_TOKEN
   - Allow workflows to create/update PR comments
   - Allow workflows to post status checks

2. **OIDC Configuration:**
   - `id-token: write` permission for Claude Code authentication
   - Trusted by Anthropic (automatic for public repos)

### External Dependencies
1. **.NET SDK:** Version 10.0.100-rc.2.25502.107 (from global.json)
2. **PowerShell Core:** v7+ for cross-platform scripts
3. **GitHub Actions:**
   - `actions/checkout@v4`
   - `actions/setup-dotnet@v4`
   - `actions/cache@v4`
   - `actions/upload-artifact@v4`
   - `actions/github-script@v7`
   - `anthropics/claude-code-action@v1`
   - `gitleaks/gitleaks-action@v2`

### Local Development Tools (for script development)
1. PowerShell 7+
2. .NET 10 SDK
3. Git
4. Text editor or IDE

### Knowledge Requirements
1. GitHub Actions YAML syntax
2. PowerShell scripting
3. .NET CLI commands
4. JSON formatting and parsing
5. Markdown formatting
6. GitHub API basics (for script development)

---

## Timeline Estimates

### Aggressive Schedule (Focused Full-Time)
- **Phase 1:** 1 day
- **Phase 2:** 1 day
- **Phase 3:** 2 days
- **Phase 4:** 1 day
- **Phase 5:** 2 days
- **Phase 6:** 1-2 days
- **Phase 7:** 2 days
- **Phase 8:** 1 day

**Total:** 11-12 days (aggressive, full-time focus)

### Realistic Schedule (Part-Time, Iterative)
- **Phase 1:** 3-5 days (includes learning/setup)
- **Phase 2:** 2-3 days (testing authorization)
- **Phase 3:** 5-7 days (core PR validation)
- **Phase 4:** 2-3 days (Claude integration)
- **Phase 5:** 4-6 days (security tooling)
- **Phase 6:** 3-4 days (.NET validation)
- **Phase 7:** 4-6 days (CI/CD pipeline)
- **Phase 8:** 2-3 days (documentation)

**Total:** 25-37 days (realistic, part-time, with iterations)

### Phased Rollout
- **MVP (Phases 1-3):** 2 weeks (basic PR validation)
- **Full Feature (Phases 1-6):** 4 weeks (complete PR validation)
- **Production Ready (Phases 1-8):** 6 weeks (full CI/CD + docs)

---

## Implementation Notes

### Testing Strategy
1. **Test workflows on feature branches** before merging to main
2. **Use test PRs** to validate PR validation workflow
3. **Test authorization** with multiple GitHub accounts (if available)
4. **Verify cross-platform** builds by running on all OS matrix entries
5. **Validate scripts locally** before committing to workflows

### Rollout Strategy
1. Start with Phase 1-2 (templates and auth) - low risk
2. Add Phase 3 (PR validation) on a feature branch, test extensively
3. Merge to main only after thorough testing
4. Enable optional steps (security, .NET validation) incrementally
5. Monitor workflow runs for failures or false positives
6. Iterate on thresholds and rules based on real usage

### Adaptation Notes
**Key Differences from Reference Repository:**

1. **Language Ecosystem:**
   - PowerShell → C#/.NET
   - PSScriptAnalyzer → dotnet format
   - Pester → MSTest
   - PowerShell modules → NuGet packages

2. **Project Structure:**
   - .specify/ directory → .NET solution structure
   - specs/ → Projects in src/ and tests/
   - SpecKit compliance → Centralized Package Management compliance

3. **Validation Focus:**
   - Constitution updates → .csproj structure
   - PowerShell best practices → .NET conventions
   - Module imports → ImplicitUsings, Nullable, CPM

4. **CI/CD Differences:**
   - Windows-only → Multi-platform (Linux, Windows, macOS)
   - Pester tests → dotnet test with Microsoft.Testing.Platform
   - No browser deps → Playwright browser installation

**Reusable Components:**
- Authorization system (language-agnostic)
- PR comment formatting pattern
- Issue/PR templates (adapted content)
- Workflow structure and job dependencies
- Security scanning approach (GitLeaks, dependency scanning)

---

## Appendix

### Reference Repository Analysis Summary

**Repository:** claude-win11-speckit-safe-update-skill
**Lines of Code (GitHub Integration):** ~2,500 lines (workflows + scripts + templates)

**Key Components:**
1. **pr-validation.yml (869 lines):**
   - 6-step validation pipeline
   - Authorization, guardrails, quality, code review, security, spec compliance
   - PowerShell-based validation scripts
   - Comprehensive PR comment system

2. **claude.yml (176 lines):**
   - Access-controlled Claude Code integration
   - Authorization check before running
   - Supports multiple trigger types

3. **claude-authorized-users.yml (54 lines):**
   - Configuration for authorization
   - Emergency circuit breaker
   - Audit logging controls

4. **Scripts (4 files, ~500 lines total):**
   - format-pr-comment.ps1: Markdown formatting
   - check-dependencies.ps1: PowerShell module vulnerability scan
   - check-path-security.ps1: Path traversal detection
   - check-spec-compliance.ps1: SpecKit validation

5. **Templates (5 files):**
   - Bug report, feature request, community contribution, config
   - PR template with checklist

6. **Community Health Files:**
   - CONTRIBUTING.md: Comprehensive contribution guide
   - SECURITY.md: Security policy
   - (CODE_OF_CONDUCT.md implied)

---

**END OF PLAN**
