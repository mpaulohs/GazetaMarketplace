# GitHub Integration Implementation - Tasks

**Last Updated:** 2025-11-02

---

## Phase Checklist

### Phase 1: Foundation & Templates
- [ ] Task 1.1: Create directory structure (.github/, workflows/, scripts/, ISSUE_TEMPLATE/)
- [ ] Task 1.2: Create issue templates (bug_report.yml, feature_request.yml, documentation.yml, config.yml)
- [ ] Task 1.3: Create PR template (pull_request_template.md with .NET-specific checklist)
- [ ] Task 1.4: Create CODE_OF_CONDUCT.md

**Checkpoint:** Templates render correctly in test issues/PRs

---

### Phase 2: Authorization & Access Control
- [ ] Task 2.1: Create authorization configuration (claude-authorized-users.yml)
- [ ] Task 2.2: Create Claude Code workflow (claude.yml with authorization checks)

**Checkpoint:** @claude mentions work for authorized users, blocked for unauthorized

---

### Phase 3: PR Validation Pipeline - Core
- [ ] Task 3.1: Create PR validation workflow structure (pr-validation.yml skeleton)
- [ ] Task 3.2: Implement authorization check (Step 1)
- [ ] Task 3.3: Implement PR guardrails (Step 2 - size and description checks)
- [ ] Task 3.4: Implement quality checks (Step 3 - dotnet format, dotnet build, dotnet test)
- [ ] Task 3.5: Create format PR comment script (format-pr-comment.ps1)
- [ ] Task 3.6: Integrate PR comment system (post/update comments for each step)

**Checkpoint:** PR validation Steps 1-3 working, comments posted correctly

---

### Phase 4: Code Review Integration
- [ ] Task 4.1: Add Claude Code review job (Step 4 with .NET-specific prompts)

**Checkpoint:** Claude Code reviews PRs automatically (when secret configured)

---

### Phase 5: Security Scanning
- [ ] Task 5.1: Implement secret scanning (GitLeaks)
- [ ] Task 5.2: Implement .NET security analyzer (SecurityCodeScan, Roslyn analyzers)
- [ ] Task 5.3: Implement NuGet vulnerability scan (check-dotnet-vulnerabilities.ps1)
- [ ] Task 5.4: Implement path security check (check-path-security.ps1 adapted for C#)
- [ ] Task 5.5: Aggregate security results (combine all findings, post PR comment)

**Checkpoint:** Security scans detect secrets, vulnerabilities, and security issues

---

### Phase 6: .NET-Specific Validation
- [ ] Task 6.1: Create .csproj structure validator (check-csproj-structure.ps1)
- [ ] Task 6.2: Create CPM validator (check Directory.Packages.props compliance)
- [ ] Task 6.3: Create global.json consistency check
- [ ] Task 6.4: Implement .NET validation job (Step 6 - aggregate all .NET checks)

**Checkpoint:** .NET validation detects CPM violations, .csproj issues, config problems

---

### Phase 7: CI/CD & Build Pipeline
- [ ] Task 7.1: Create multi-platform build workflow (dotnet-ci.yml with Linux/Windows/macOS matrix)
- [ ] Task 7.2: Add test coverage reporting (Codecov or similar)
- [ ] Task 7.3: Add build artifact publishing (binaries, NuGet packages)
- [ ] Task 7.4: Add Docker image build (optional - Example.Web and Example.API containers)

**Checkpoint:** CI builds pass on all platforms, artifacts published

---

### Phase 8: Documentation & Polish
- [ ] Task 8.1: Create CONTRIBUTING.md (development setup, workflow, standards, PR guidelines)
- [ ] Task 8.2: Create SECURITY.md (supported versions, vulnerability reporting, best practices)
- [ ] Task 8.3: Create workflow documentation (.github/workflows/README.md)
- [ ] Task 8.4: Create scripts documentation (.github/scripts/README.md)
- [ ] Task 8.5: Update main README.md (add badges, contributing section, links)
- [ ] Task 8.6: Update CLAUDE.md (add GitHub integration section)

**Checkpoint:** Documentation complete, all links working, badges displayed

---

## Task Progress Tracking

### Phase 1: Foundation & Templates (Estimated: 2-3 hours)
| Task | Status | Notes |
|------|--------|-------|
| 1.1 - Directory structure | ⏳ Not Started | |
| 1.2 - Issue templates | ⏳ Not Started | Reference: bug_report.yml, feature_request.yml |
| 1.3 - PR template | ⏳ Not Started | Add .NET-specific checklist items |
| 1.4 - CODE_OF_CONDUCT.md | ⏳ Not Started | Use Contributor Covenant v2.1 |

---

### Phase 2: Authorization & Access Control (Estimated: 4-6 hours)
| Task | Status | Notes |
|------|--------|-------|
| 2.1 - Authorization config | ⏳ Not Started | claude-authorized-users.yml |
| 2.2 - Claude Code workflow | ⏳ Not Started | Test with @claude mentions |

---

### Phase 3: PR Validation Pipeline - Core (Estimated: 8-10 hours)
| Task | Status | Notes |
|------|--------|-------|
| 3.1 - Workflow structure | ⏳ Not Started | Job dependency chain |
| 3.2 - Authorization check | ⏳ Not Started | Step 1 |
| 3.3 - PR guardrails | ⏳ Not Started | Step 2 - size/description |
| 3.4 - Quality checks | ⏳ Not Started | Step 3 - format/build/test |
| 3.5 - Comment formatter | ⏳ Not Started | format-pr-comment.ps1 |
| 3.6 - Comment integration | ⏳ Not Started | Post/update logic |

---

### Phase 4: Code Review Integration (Estimated: 3-4 hours)
| Task | Status | Notes |
|------|--------|-------|
| 4.1 - Claude Code review | ⏳ Not Started | Step 4 - OIDC auth required |

---

### Phase 5: Security Scanning (Estimated: 6-8 hours)
| Task | Status | Notes |
|------|--------|-------|
| 5.1 - Secret scanning | ⏳ Not Started | GitLeaks action |
| 5.2 - .NET security analyzer | ⏳ Not Started | SecurityCodeScan, Roslyn |
| 5.3 - NuGet vulnerability scan | ⏳ Not Started | check-dotnet-vulnerabilities.ps1 |
| 5.4 - Path security check | ⏳ Not Started | Adapt for C# |
| 5.5 - Aggregate security results | ⏳ Not Started | Step 5 |

---

### Phase 6: .NET-Specific Validation (Estimated: 4-5 hours)
| Task | Status | Notes |
|------|--------|-------|
| 6.1 - .csproj validator | ⏳ Not Started | check-csproj-structure.ps1 |
| 6.2 - CPM validator | ⏳ Not Started | Directory.Packages.props |
| 6.3 - global.json check | ⏳ Not Started | SDK version, test runner |
| 6.4 - .NET validation job | ⏳ Not Started | Step 6 |

---

### Phase 7: CI/CD & Build Pipeline (Estimated: 6-8 hours)
| Task | Status | Notes |
|------|--------|-------|
| 7.1 - Multi-platform build | ⏳ Not Started | Linux/Windows/macOS matrix |
| 7.2 - Test coverage | ⏳ Not Started | Optional - Codecov |
| 7.3 - Artifact publishing | ⏳ Not Started | Binaries, NuGet packages |
| 7.4 - Docker images | ⏳ Not Started | Optional |

---

### Phase 8: Documentation & Polish (Estimated: 3-4 hours)
| Task | Status | Notes |
|------|--------|-------|
| 8.1 - CONTRIBUTING.md | ⏳ Not Started | |
| 8.2 - SECURITY.md | ⏳ Not Started | |
| 8.3 - Workflow docs | ⏳ Not Started | .github/workflows/README.md |
| 8.4 - Scripts docs | ⏳ Not Started | .github/scripts/README.md |
| 8.5 - Update README.md | ⏳ Not Started | Badges, links |
| 8.6 - Update CLAUDE.md | ⏳ Not Started | GitHub integration section |

---

## Overall Progress

**Current Phase:** Phase 1 (Foundation & Templates)

**Overall Completion:** 0% (0/40 tasks completed)

**Phases Complete:** 0/8

**Estimated Time Remaining:** 36-47 hours (realistic, part-time schedule)

---

## Blockers and Issues

**None currently**

---

## Notes

- Start with Phase 1 - low risk, foundational work
- Test templates by creating test issues/PRs before proceeding
- Phase 2 requires testing with @claude mentions (may need secondary GitHub account)
- Phase 3 is the most complex - break into smaller chunks if needed
- Phases 4-6 can be done incrementally (each step is independent)
- Phase 7 (CI/CD) is separate from PR validation and can be done last
- Phase 8 (documentation) should be done continuously, not all at the end

---

**Status Legend:**
- ⏳ Not Started
- 🟡 In Progress
- ✅ Completed
- ⏸️ Blocked
- ⏭️ Skipped

---

**END OF TASKS**
