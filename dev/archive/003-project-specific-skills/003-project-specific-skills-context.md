# Project-Specific Skills - Context

**Last Updated:** 2025-11-01

---

## SESSION PROGRESS

### ✅ COMPLETED
- [x] Analyzed project structure and technology stack
- [x] Identified six skills needed (3 high-priority, 3 medium-priority)
- [x] Created comprehensive implementation plan
- [x] Phase 1: Create mstest-testing-platform skill (488 lines)
- [x] Phase 2: Create dotnet-centralized-packages skill (495 lines)
- [x] Phase 3: Create playwright-dotnet skill (432 lines)
- [x] Phase 5: Create dotnet-minimal-apis skill (452 lines)
- [x] Phase 6: Create dotnet-cli-essentials skill (358 lines)
- [x] Phase 7: Create aspnet-configuration skill (437 lines)
- [x] Phase 8: Integration and documentation

### 🟡 IN PROGRESS
- [ ] None currently

### ⏳ NOT STARTED
- [ ] None - all tasks completed!

---

## Key Decisions

### Decision 1: Expanded to Six Skills
**Rationale:** User requested adding three medium-priority skills to the original plan:
1. dotnet-minimal-apis
2. dotnet-cli-essentials
3. aspnet-configuration

**Original Plan:** 3 high-priority skills (MSTest, CPM, Playwright)
**Expanded Plan:** 6 skills total (3 high + 3 medium priority)

**Alternatives Considered:**
- Only create the 3 high-priority skills (can still be done first, then decide)
- Single comprehensive ".NET 10" skill (rejected: would exceed 500-line limit)

**Impact:** More comprehensive coverage of .NET 10 project patterns, ~8 hours total vs ~4 hours

---

### Decision 2: Phased Implementation Approach Recommended
**Rationale:**
- One skill per session allows validation
- Can incorporate learnings between skills
- Less risk of rework

**Alternatives:**
- Single 4-5 hour session (feasible but higher risk)

**Impact:** Slower overall but higher quality outcome

---

### Decision 3: Skill Types and Enforcement
**Skill Configuration:**
- Type: domain (all three)
- Enforcement: suggest (not block)
- Priority: high (MSTest), high (CPM), high (Playwright)

**Rationale:** Advisory guidance fits better than blocking for these topics

---

## Key Files and Locations

### Project Configuration Files
**Directory.Build.props** (`/Directory.Build.props`)
- TargetFramework: net10.0
- ImplicitUsings: disabled
- Nullable: disabled
- TreatWarningsAsErrors: true

**Directory.Packages.props** (`/Directory.Packages.props`)
- ManagePackageVersionsCentrally: true
- CentralPackageTransitivePinningEnabled: true
- Current packages: Razor RuntimeCompilation, OpenAPI, MSTest, Playwright

**global.json** (`/global.json`)
- SDK: 10.0.100-rc.2.25502.107
- Test runner: Microsoft.Testing.Platform

### Test Projects
**MSTest Projects:**
- tests/ClaudeStack.API.Tests/ClaudeStack.API.Tests.csproj
- tests/ClaudeStack.Web.Tests/ClaudeStack.Web.Tests.csproj

**Playwright Projects:**
- tests/ClaudeStack.API.Tests.Playwright/ClaudeStack.API.Tests.Playwright.csproj
- tests/ClaudeStack.Web.Tests.Playwright/ClaudeStack.Web.Tests.Playwright.csproj

All test projects use:
- EnableMSTestRunner: true
- OutputType: Exe
- Microsoft.Testing.Platform runner

### Claude Code Infrastructure
**.claude/skills/** - Skill directory
- skill-developer/ (meta-skill)
- azure-devops/ (recently created)
- mstest-testing-platform/ (to be created)
- dotnet-centralized-packages/ (to be created)
- playwright-dotnet/ (to be created)

**.claude/skills/skill-rules.json** - Trigger configuration

**.claude/hooks/** - Auto-activation hooks
- skill-activation-prompt.ts (UserPromptSubmit)
- post-tool-use-tracker.sh (PostToolUse)

---

## Technical Constraints

### Line Count Limit
- **Constraint:** Each SKILL.md must stay under 500 lines
- **Mitigation:** Use reference files if needed (EXAMPLES.md, PATTERNS.md)
- **Current Status:** All planned skills estimated at 400-450 lines

### Testing Platform Specifics
- **Challenge:** Microsoft.Testing.Platform is new (less documentation)
- **Project Uses:** v4.0.0-preview.25465.3
- **Key Gotcha:** Using `--test-runner` flag overwrites global.json

### Package Management Specifics
- **Challenge:** CPM is less common than traditional package management
- **Project Pattern:** All versions in Directory.Packages.props, no Version in .csproj
- **Validation:** ManagePackageVersionsCentrally + CentralPackageTransitivePinningEnabled

### Playwright Specifics
- **Challenge:** Browser installation requires PowerShell script
- **Package:** Microsoft.Playwright.MSTest.v4 1.55.0-beta-4
- **Installation Path:** tests/*/bin/Debug/net10.0/playwright.ps1 install

---

## Dependencies

### Internal Dependencies
- Claude Code infrastructure (installed)
- skill-developer skill (available)
- Git repository (clean working directory)

### External Dependencies
- Node.js and npx (for testing hooks)
- Python 3 (for JSON validation)
- .NET 10 SDK (already installed)

### Documentation References
- [Microsoft.Testing.Platform docs](https://learn.microsoft.com/en-us/dotnet/core/testing/testing-platform)
- [CPM docs](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [Playwright .NET docs](https://playwright.dev/dotnet/docs/intro)

---

## Skill Trigger Strategy

### High Priority Skills

#### Keyword Triggers
**MSTest:** "MSTest", "Testing.Platform", "test runner", "EnableMSTestRunner"
**CPM:** "Directory.Packages", "centralized package", "CPM", "package version"
**Playwright:** "Playwright", "E2E", "browser test", "playwright install"

#### Intent Patterns
**MSTest:** "(run|execute|debug) tests", "mstest setup"
**CPM:** "(add|install|update) package", "package version"
**Playwright:** "(create|write) (playwright|e2e) test"

#### File Patterns
**MSTest:** "tests/**/*.csproj", "**/MSTestSettings.cs"
**CPM:** "Directory.Packages.props", when editing .csproj with PackageReference
**Playwright:** "**/*.Playwright.csproj"

### Medium Priority Skills

#### Keyword Triggers
**Minimal APIs:** "minimal API", "MapGet", "MapPost", "endpoint", "route group"
**CLI Essentials:** "dotnet build", "dotnet run", "dotnet cli", "slnx", "dotnet watch"
**Configuration:** "appsettings", "IConfiguration", "user secrets", "options pattern"

#### Intent Patterns
**Minimal APIs:** "(create|configure) minimal api", "map(get|post|put|delete)"
**CLI Essentials:** "(build|run|restore) (project|solution)", "dotnet.*?command"
**Configuration:** "(configure|access) (appsettings|configuration)", "user.*?secrets"

#### File Patterns
**Minimal APIs:** "**/Program.cs" (in API projects)
**CLI Essentials:** "*.sln", "*.slnx", "Directory.Build.props"
**Configuration:** "**/appsettings*.json"

---

## Quality Checklist

Before marking each skill complete, verify:

### Content Quality
- [ ] All sections complete and accurate
- [ ] Real examples from this project included
- [ ] Code examples tested and working
- [ ] Troubleshooting section addresses common issues
- [ ] Quick reference section included

### Technical Accuracy
- [ ] Commands tested in this project
- [ ] Version numbers match project (where applicable)
- [ ] Paths are correct for this project structure
- [ ] No outdated information

### Skill System Compliance
- [ ] YAML frontmatter valid
- [ ] Line count under 500
- [ ] Clear name and description
- [ ] Follows 500-line rule best practice

### Trigger Configuration
- [ ] Entry in skill-rules.json
- [ ] JSON validates successfully
- [ ] Keywords cover expected use cases
- [ ] Intent patterns tested
- [ ] No false positives on unrelated prompts

### Testing
- [ ] Manual trigger tests pass
- [ ] Skill activates on expected prompts
- [ ] Skill content is helpful and relevant
- [ ] No errors when skill loads

---

## Risk Tracking

### Current Risks

**Risk:** Skill content exceeds 500 lines
**Status:** OPEN
**Mitigation:** Monitor during writing, use reference files if needed
**Owner:** Implementation phase

**Risk:** Triggers too broad (false positives)
**Status:** OPEN
**Mitigation:** Extensive testing with variety of prompts
**Owner:** Testing phase

**Risk:** Content becomes outdated (.NET 10 RC→RTM)
**Status:** ACCEPTED
**Mitigation:** Plan for update when .NET 10 RTM releases
**Owner:** Future maintenance

---

## Session Notes

### 2025-11-01: Planning Session
- Created comprehensive implementation plan
- Identified six skills (3 high-priority, 3 medium-priority)
- Estimated 8 hours total implementation (or 4.5 hours for high-priority only)
- Recommended phased approach (6 sessions) or high-priority first
- User requested expansion from 3 to 6 skills
- Added: dotnet-minimal-apis, dotnet-cli-essentials, aspnet-configuration
- Next: Begin Phase 1 (MSTest skill)

---

## Quick Resume Guide

**If resuming this task:**
1. Read this context file for current state
2. Review `003-project-specific-skills-plan.md` for detailed plan
3. Check `003-project-specific-skills-tasks.md` for checklist
4. Continue from current IN PROGRESS phase
5. Use skill-developer skill for guidance when creating skills

**Current Phase:** Planning Complete (Expanded to 6 skills)
**Recommended Start:** High-priority skills first (Phases 1-3)
**Next Phase:** Phase 1 - Create mstest-testing-platform skill
**Estimated Time:** 90 minutes
**Option:** Can create all 6 skills (~8 hours) or start with 3 high-priority (~4.5 hours)
