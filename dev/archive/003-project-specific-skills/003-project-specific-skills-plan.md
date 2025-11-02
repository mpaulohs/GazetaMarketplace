# Project-Specific Skills Implementation Plan

**Last Updated:** 2025-11-01

---

## Executive Summary

This plan outlines the creation of six Claude Code skills tailored to this .NET 10 project's specific technology stack and patterns. These skills will auto-activate based on user prompts to provide contextual guidance for:

**High Priority (Unique Patterns):**
1. **MSTest Testing Platform** - New Microsoft.Testing.Platform patterns and best practices
2. **Centralized Package Management** - Directory.Packages.props workflows
3. **Playwright .NET Testing** - End-to-end testing with Microsoft.Playwright.MSTest.v4

**Medium Priority (Project-Specific):**
4. **Minimal APIs** - ASP.NET Core Minimal API patterns used in Example.API
5. **.NET CLI Essentials** - Common CLI operations specific to this project structure
6. **ASP.NET Configuration** - Configuration patterns for appsettings and environments

**Estimated Total Time:** 7-8 hours
**Priority:** HIGH for first 3, MEDIUM for last 3
**Dependencies:** Claude Code infrastructure (already installed), skill-developer skill

---

## Current State Analysis

### Existing Infrastructure
✅ **Claude Code Setup Complete:**
- UserPromptSubmit hook (skill-activation-prompt.ts)
- PostToolUse hook (post-tool-use-tracker.sh)
- skill-developer meta-skill
- azure-devops skill (recently added)
- skill-rules.json configured

### Project Technology Stack
**Unique/Less Common Patterns:**
- ✅ Microsoft.Testing.Platform (NEW test runner, not legacy VSTest)
- ✅ Centralized Package Management (Directory.Packages.props)
- ✅ ImplicitUsings disabled (requires explicit using statements)
- ✅ Playwright with MSTest.v4 integration
- ✅ .NET 10 RC 2 (preview features)
- ✅ .slnx solution format

**Standard Patterns:**
- ASP.NET Core MVC with Razor
- Minimal APIs with OpenAPI
- MSTest for unit testing

### Gap Analysis

**Skills Needed (High Priority):**
1. **mstest-testing-platform** - Critical, 4 test projects use this
2. **dotnet-centralized-packages** - Essential for package management
3. **playwright-dotnet** - 2 Playwright projects need guidance

**Skills Potentially Useful (Lower Priority):**
- dotnet-minimal-apis (Example.API patterns)
- aspnet-mvc-razor (Example.Web patterns)
- dotnet-explicit-usings (ImplicitUsings=false)

---

## Proposed Future State

### Target Outcome
Six production-ready skills that:
- Auto-activate on relevant prompts and file patterns
- Provide comprehensive, accurate guidance
- Include real examples from this project
- Stay under 500-line limit (with reference files if needed)
- Pass all trigger tests (keywords, intent patterns)

### Skill Specifications

#### Skill 1: mstest-testing-platform
**Type:** Domain
**Enforcement:** Suggest
**Priority:** High
**Estimated Lines:** ~450

**Coverage:**
- Microsoft.Testing.Platform vs VSTest differences
- Running tests with `dotnet run --project`
- MSTestSettings.cs configuration
- EnableMSTestRunner property requirements
- Test parallelization (ExecutionScope.MethodLevel)
- Avoiding `--test-runner` flag gotcha
- Debugging tests
- Test filters and selection
- Integration with CI/CD

**Triggers:**
- Keywords: "MSTest", "Testing.Platform", "run tests", "test runner", "EnableMSTestRunner"
- Intent: "(run|execute|debug) tests", "mstest.*?(configuration|setup|runner)"
- Files: "tests/**/*.csproj", "**/MSTestSettings.cs"

#### Skill 2: dotnet-centralized-packages
**Type:** Domain
**Enforcement:** Suggest
**Priority:** High
**Estimated Lines:** ~400

**Coverage:**
- Directory.Packages.props structure
- Adding packages correctly (no Version in .csproj)
- Managing package versions centrally
- CentralPackageTransitivePinningEnabled
- Updating packages across solution
- Troubleshooting version conflicts
- Common patterns and gotchas
- Integration with NuGet tooling

**Triggers:**
- Keywords: "Directory.Packages", "CPM", "centralized package", "add package", "package version"
- Intent: "(add|install|update|manage) (package|nuget)", "package.*?version"
- Files: "Directory.Packages.props", "**/*.csproj" (when adding PackageReference)

#### Skill 3: playwright-dotnet
**Type:** Domain
**Enforcement:** Suggest
**Priority:** High
**Estimated Lines:** ~450

**Coverage:**
- Installing Playwright browsers (./playwright.ps1 install)
- Microsoft.Playwright.MSTest.v4 patterns
- Page object model patterns
- Test isolation and cleanup
- Debugging Playwright tests
- Headless vs headed mode
- Screenshots and traces
- CI/CD integration
- Common selectors and assertions

**Triggers:**
- Keywords: "Playwright", "E2E", "end-to-end", "browser test", "playwright install"
- Intent: "(create|write|debug) (playwright|e2e|browser) test"
- Files: "**/*.Playwright.csproj", "**/PageTests.cs"

#### Skill 4: dotnet-minimal-apis
**Type:** Domain
**Enforcement:** Suggest
**Priority:** Medium
**Estimated Lines:** ~400

**Coverage:**
- Minimal API fundamentals and patterns
- MapGet, MapPost, MapPut, MapDelete
- Route groups and organization
- Parameter binding and validation
- OpenAPI/Swagger integration
- Dependency injection in minimal APIs
- Request/response patterns
- Error handling and Problem Details
- Filters and middleware
- Testing minimal APIs

**Triggers:**
- Keywords: "minimal API", "minimal apis", "MapGet", "MapPost", "WebApplication", "endpoint", "route group"
- Intent: "(create|build|configure) minimal api", "map(get|post|put|delete)"
- Files: "**/Program.cs" (in API projects)

#### Skill 5: dotnet-cli-essentials
**Type:** Domain
**Enforcement:** Suggest
**Priority:** Medium
**Estimated Lines:** ~350

**Coverage:**
- dotnet build with Directory.Build.props
- dotnet run --project patterns
- Solution management (.slnx format)
- Project references and structure
- Clean, restore, publish workflows
- Watch mode for development
- Configuration profiles
- Common flags and options
- Troubleshooting build issues
- Project-specific conventions

**Triggers:**
- Keywords: "dotnet build", "dotnet run", "dotnet test", "dotnet cli", "solution", "slnx", "dotnet watch"
- Intent: "(build|run|restore|clean) (project|solution)", "dotnet.*?(command|cli)"
- Files: "*.sln", "*.slnx", "Directory.Build.props"

#### Skill 6: aspnet-configuration
**Type:** Domain
**Enforcement:** Suggest
**Priority:** Medium
**Estimated Lines:** ~380

**Coverage:**
- appsettings.json structure and patterns
- Environment-specific configuration (Development, Production)
- appsettings.Development.json overrides
- Configuration binding and IConfiguration
- Options pattern and strongly-typed config
- User secrets for development
- Environment variables
- Configuration providers hierarchy
- Connection strings management
- Accessing configuration in controllers/services

**Triggers:**
- Keywords: "appsettings", "configuration", "IConfiguration", "user secrets", "environment variables", "options pattern"
- Intent: "(configure|setup|manage) (appsettings|configuration)", "user.*?secrets"
- Files: "**/appsettings*.json", "**/Program.cs", "**/Startup.cs"

---

## Implementation Phases

### Phase 1: mstest-testing-platform Skill (90 minutes)

#### Task 1.1: Research and Document Current Project Usage (20 min)
**Effort:** S

**Actions:**
1. Examine all 4 test project .csproj files
2. Review MSTestSettings.cs configurations
3. Document current test running patterns
4. Identify common issues/questions

**Acceptance Criteria:**
- [ ] Documented all MSTestSettings.cs configurations
- [ ] Listed all test execution methods used
- [ ] Identified project-specific patterns

**Output:** Notes document with current usage patterns

---

#### Task 1.2: Create Skill File Structure (15 min)
**Effort:** S

**Actions:**
1. Create `.claude/skills/mstest-testing-platform/`
2. Create `SKILL.md` with YAML frontmatter
3. Add placeholder sections

**Acceptance Criteria:**
- [ ] Directory created
- [ ] SKILL.md exists with valid frontmatter
- [ ] Under 500 lines initially

**Dependencies:** None

---

#### Task 1.3: Write Skill Content (40 min)
**Effort:** M

**Sections to Include:**
- Prerequisites (SDK, packages)
- Microsoft.Testing.Platform vs VSTest comparison
- Project setup (EnableMSTestRunner, OutputType)
- Running tests (dotnet run vs dotnet test)
- MSTestSettings.cs configuration
- Test parallelization
- Debugging techniques
- CI/CD integration
- Troubleshooting (common errors)
- Quick reference

**Acceptance Criteria:**
- [ ] All sections completed
- [ ] Real examples from project included
- [ ] Under 500 lines
- [ ] Code examples tested

**Output:** Complete SKILL.md file

---

#### Task 1.4: Configure Skill Triggers (10 min)
**Effort:** S

**Actions:**
1. Add entry to skill-rules.json
2. Define keywords
3. Define intent patterns
4. Define file patterns (optional)

**Keywords:**
```json
["MSTest", "Microsoft.Testing.Platform", "test runner",
 "EnableMSTestRunner", "MSTestSettings", "run tests",
 "test execution", "dotnet test", "test parallelization"]
```

**Intent Patterns:**
```json
["(run|execute|debug).*?test",
 "mstest.*?(setup|config|runner|platform)",
 "(test|testing).*?(platform|runner|execution)",
 "how.*?(run|execute) (unit|integration) test"]
```

**Acceptance Criteria:**
- [ ] Entry added to skill-rules.json
- [ ] JSON validates
- [ ] Patterns cover expected use cases

---

#### Task 1.5: Test and Validate (5 min)
**Effort:** S

**Test Commands:**
```bash
# Validate JSON
python3 -c "import json; json.load(open('.claude/skills/skill-rules.json'))"

# Test triggers
cd .claude/hooks
echo '{"prompt":"How do I run MSTest tests?"}' | npx tsx skill-activation-prompt.ts
echo '{"prompt":"Configure test parallelization"}' | npx tsx skill-activation-prompt.ts
echo '{"prompt":"Debug unit tests"}' | npx tsx skill-activation-prompt.ts
```

**Acceptance Criteria:**
- [ ] JSON validates successfully
- [ ] All test prompts trigger skill
- [ ] No false positives on unrelated prompts
- [ ] Line count under 500

---

### Phase 2: dotnet-centralized-packages Skill (80 minutes)

#### Task 2.1: Research Project Package Management (15 min)
**Effort:** S

**Actions:**
1. Analyze Directory.Packages.props structure
2. Analyze Directory.Build.props settings
3. Document package addition process
4. Identify common patterns

**Acceptance Criteria:**
- [ ] Documented all package management properties
- [ ] Listed all current packages
- [ ] Identified project conventions

---

#### Task 2.2: Create Skill File Structure (10 min)
**Effort:** S

**Actions:**
1. Create `.claude/skills/dotnet-centralized-packages/`
2. Create `SKILL.md` with frontmatter
3. Set up sections

**Acceptance Criteria:**
- [ ] Directory created
- [ ] SKILL.md template ready

---

#### Task 2.3: Write Skill Content (40 min)
**Effort:** M

**Sections:**
- What is CPM and why use it
- Directory.Packages.props structure
- Adding packages (correct vs incorrect)
- Updating package versions
- CentralPackageTransitivePinningEnabled
- Troubleshooting version conflicts
- Visual Studio vs CLI workflows
- Common errors and solutions
- Best practices
- Quick reference commands

**Real Examples from Project:**
- Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
- Microsoft.Playwright.MSTest.v4
- MSTest packages

**Acceptance Criteria:**
- [ ] All sections complete
- [ ] Project-specific examples included
- [ ] Under 500 lines
- [ ] Commands tested

---

#### Task 2.4: Configure Skill Triggers (10 min)
**Effort:** S

**Keywords:**
```json
["Directory.Packages.props", "centralized package management",
 "CPM", "add package", "nuget package", "package version",
 "CentralPackageTransitivePinningEnabled", "package reference",
 "manage packages"]
```

**Intent Patterns:**
```json
["(add|install|update|remove).*?package",
 "package.*?(version|management|reference)",
 "Directory\\.Packages",
 "centralized.*?package"]
```

**Acceptance Criteria:**
- [ ] Triggers configured
- [ ] JSON validates

---

#### Task 2.5: Test and Validate (5 min)
**Effort:** S

**Test Prompts:**
- "How do I add a new NuGet package?"
- "Update package version in Directory.Packages.props"
- "Centralized package management setup"
- "Package version conflict resolution"

**Acceptance Criteria:**
- [ ] All triggers work
- [ ] No false positives

---

### Phase 3: playwright-dotnet Skill (90 minutes)

#### Task 3.1: Research Playwright Project Setup (20 min)
**Effort:** S

**Actions:**
1. Examine both Playwright test projects
2. Review .csproj configurations
3. Document browser installation process
4. Identify test patterns used

**Files to Review:**
- tests/Example.Web.Tests.Playwright/Example.Web.Tests.Playwright.csproj
- tests/Example.API.Tests.Playwright/Example.API.Tests.Playwright.csproj
- Test1.cs in both projects

**Acceptance Criteria:**
- [ ] Documented project setup
- [ ] Listed browser installation steps
- [ ] Identified test patterns

---

#### Task 3.2: Create Skill File Structure (10 min)
**Effort:** S

**Actions:**
1. Create `.claude/skills/playwright-dotnet/`
2. Create `SKILL.md` with frontmatter

**Acceptance Criteria:**
- [ ] Directory and file created

---

#### Task 3.3: Write Skill Content (45 min)
**Effort:** M

**Sections:**
- Installing Playwright for .NET
- Browser installation (playwright.ps1 install)
- Microsoft.Playwright.MSTest.v4 integration
- Project setup and configuration
- Writing first test (page object pattern)
- Selectors and assertions
- Test isolation and cleanup
- Debugging (headed mode, screenshots, traces)
- CI/CD setup
- Common patterns
- Troubleshooting
- Quick reference

**Acceptance Criteria:**
- [ ] All sections complete
- [ ] Real project examples
- [ ] Under 500 lines

---

#### Task 3.4: Configure Skill Triggers (10 min)
**Effort:** S

**Keywords:**
```json
["Playwright", "browser automation", "E2E", "end-to-end test",
 "playwright install", "Microsoft.Playwright.MSTest",
 "page object", "browser test", "headless", "playwright.ps1"]
```

**Intent Patterns:**
```json
["(create|write|debug).*?(playwright|e2e|browser) test",
 "playwright.*?(install|setup|config)",
 "(browser|web).*?(automation|testing)",
 "end.?to.?end test"]
```

**File Patterns:**
```json
["**/*.Playwright.csproj",
 "**/Playwright/**/*.cs"]
```

**Acceptance Criteria:**
- [ ] Triggers configured
- [ ] JSON validates

---

#### Task 3.5: Test and Validate (5 min)
**Effort:** S

**Test Prompts:**
- "How do I install Playwright browsers?"
- "Create a Playwright test for login page"
- "Debug E2E tests in headed mode"
- "Playwright test best practices"

**Acceptance Criteria:**
- [ ] All triggers work
- [ ] File pattern triggers on .Playwright.csproj

---

### Phase 5: dotnet-minimal-apis Skill (75 minutes)

#### Task 5.1: Research Example.API Project (15 min)
**Effort:** S

**Actions:**
1. Analyze Example.API/Program.cs minimal API patterns
2. Document current endpoint patterns (MapGet, etc.)
3. Review OpenAPI integration
4. Identify common patterns to document

**Acceptance Criteria:**
- [ ] Documented all minimal API endpoints
- [ ] Identified pattern examples
- [ ] Listed OpenAPI configuration

---

#### Task 5.2: Create Skill File Structure (10 min)
**Effort:** S

**Actions:**
1. Create `.claude/skills/dotnet-minimal-apis/`
2. Create `SKILL.md` with frontmatter

**Acceptance Criteria:**
- [ ] Directory and file created
- [ ] Valid YAML frontmatter

---

#### Task 5.3: Write Skill Content (35 min)
**Effort:** M

**Sections:**
- Minimal API fundamentals
- MapGet, MapPost, MapPut, MapDelete patterns
- Route groups and organization
- Parameter binding (query, route, body)
- Validation and filters
- OpenAPI/Swagger integration
- Dependency injection patterns
- Error handling and Problem Details
- Testing minimal APIs
- Best practices
- Quick reference

**Acceptance Criteria:**
- [ ] All sections complete
- [ ] Real Example.API examples included
- [ ] Under 500 lines
- [ ] Code tested

---

#### Task 5.4: Configure Skill Triggers (10 min)
**Effort:** S

**Keywords:**
```json
["minimal API", "minimal apis", "MapGet", "MapPost", "MapPut", "MapDelete",
 "WebApplication", "endpoint", "route group", "minimal endpoint",
 "map endpoint", "endpoint routing"]
```

**Intent Patterns:**
```json
["(create|build|add|configure).*?minimal api",
 "map(get|post|put|delete|patch)",
 "minimal.*?(api|endpoint)",
 "(route|endpoint).*?(group|organization)"]
```

**Acceptance Criteria:**
- [ ] Triggers configured
- [ ] JSON validates

---

#### Task 5.5: Test and Validate (5 min)
**Effort:** S

**Test Prompts:**
- "Create a minimal API endpoint"
- "How do I use MapGet in ASP.NET?"
- "Configure route groups for minimal APIs"
- "Minimal API dependency injection"

**Acceptance Criteria:**
- [ ] All triggers work
- [ ] No false positives

---

### Phase 6: dotnet-cli-essentials Skill (70 minutes)

#### Task 6.1: Research Project CLI Patterns (12 min)
**Effort:** S

**Actions:**
1. Review CLAUDE.md for documented CLI commands
2. Analyze .slnx solution format usage
3. Document build/run patterns
4. Identify project-specific conventions

**Acceptance Criteria:**
- [ ] Documented all CLI patterns
- [ ] Listed .slnx specifics
- [ ] Identified conventions

---

#### Task 6.2: Create Skill File Structure (8 min)
**Effort:** S

**Actions:**
1. Create `.claude/skills/dotnet-cli-essentials/`
2. Create `SKILL.md` with frontmatter

**Acceptance Criteria:**
- [ ] Directory and file created

---

#### Task 6.3: Write Skill Content (35 min)
**Effort:** M

**Sections:**
- dotnet CLI overview
- Building (dotnet build with Directory.Build.props)
- Running projects (dotnet run --project)
- Solution management (.slnx format)
- Restore and clean operations
- Watch mode for development
- Project references
- Common flags and options
- Troubleshooting build issues
- Project-specific patterns from CLAUDE.md
- Quick reference

**Acceptance Criteria:**
- [ ] All sections complete
- [ ] CLAUDE.md examples referenced
- [ ] Under 500 lines

---

#### Task 6.4: Configure Skill Triggers (10 min)
**Effort:** S

**Keywords:**
```json
["dotnet build", "dotnet run", "dotnet test", "dotnet cli",
 "dotnet restore", "dotnet clean", "dotnet watch",
 "solution", "slnx", "dotnet command", "dotnet publish"]
```

**Intent Patterns:**
```json
["(build|run|restore|clean|publish).*?(project|solution)",
 "dotnet.*?(build|run|restore|clean|watch|publish)",
 "solution.*?(management|structure)",
 "\\.slnx"]
```

**File Patterns:**
```json
["*.sln", "*.slnx", "Directory.Build.props"]
```

**Acceptance Criteria:**
- [ ] Triggers configured
- [ ] JSON validates

---

#### Task 6.5: Test and Validate (5 min)
**Effort:** S

**Test Prompts:**
- "How do I build the solution?"
- "Run a specific project with dotnet"
- "What is .slnx format?"
- "dotnet watch mode"

**Acceptance Criteria:**
- [ ] All triggers work
- [ ] File patterns work on .slnx

---

### Phase 7: aspnet-configuration Skill (75 minutes)

#### Task 7.1: Research Configuration Patterns (15 min)
**Effort:** S

**Actions:**
1. Review appsettings.json files in both projects
2. Analyze appsettings.Development.json overrides
3. Review Program.cs/Startup.cs configuration usage
4. Document current patterns

**Acceptance Criteria:**
- [ ] All appsettings files analyzed
- [ ] Configuration patterns documented
- [ ] Usage examples identified

---

#### Task 7.2: Create Skill File Structure (10 min)
**Effort:** S

**Actions:**
1. Create `.claude/skills/aspnet-configuration/`
2. Create `SKILL.md` with frontmatter

**Acceptance Criteria:**
- [ ] Directory and file created

---

#### Task 7.3: Write Skill Content (35 min)
**Effort:** M

**Sections:**
- Configuration fundamentals
- appsettings.json structure
- Environment-specific configuration
- Configuration hierarchy and overrides
- IConfiguration interface usage
- Options pattern (strongly-typed config)
- User secrets for development
- Environment variables
- Connection strings
- Accessing configuration in controllers/services
- Best practices
- Troubleshooting
- Quick reference

**Acceptance Criteria:**
- [ ] All sections complete
- [ ] Real project examples
- [ ] Under 500 lines

---

#### Task 7.4: Configure Skill Triggers (10 min)
**Effort:** S

**Keywords:**
```json
["appsettings", "configuration", "IConfiguration",
 "user secrets", "environment variables", "options pattern",
 "appsettings.json", "appsettings.Development.json",
 "configure services", "config binding"]
```

**Intent Patterns:**
```json
["(configure|setup|manage|access).*?(appsettings|configuration)",
 "user.*?secrets",
 "environment.*?(variables|configuration)",
 "options.*?pattern",
 "(read|get|access).*?configuration"]
```

**File Patterns:**
```json
["**/appsettings*.json"]
```

**Acceptance Criteria:**
- [ ] Triggers configured
- [ ] JSON validates

---

#### Task 7.5: Test and Validate (5 min)
**Effort:** S

**Test Prompts:**
- "How do I configure appsettings?"
- "Use user secrets for development"
- "Access configuration in controller"
- "Options pattern for configuration"

**Acceptance Criteria:**
- [ ] All triggers work
- [ ] File patterns work on appsettings.json

---

### Phase 8: Integration and Documentation (45 minutes)

#### Task 8.1: Update Project Documentation (20 min)
**Effort:** S

**Actions:**
1. Update `.claude/README.md` to list all 6 new skills
2. Update `CLAUDE.md` if needed
3. Document skill usage examples
4. Organize skills by priority (high/medium)

**Acceptance Criteria:**
- [ ] README updated with 6 new skills
- [ ] Skills categorized by priority
- [ ] Usage examples provided for each

---

#### Task 8.2: Create Validation Test Suite (15 min)
**Effort:** S

**Create:** `tests/skill-validation.sh`

```bash
#!/bin/bash
# Test all skill triggers

echo "=== HIGH PRIORITY SKILLS ==="

echo "Testing mstest-testing-platform..."
echo '{"prompt":"How do I run MSTest tests?"}' | npx tsx .claude/hooks/skill-activation-prompt.ts

echo "Testing dotnet-centralized-packages..."
echo '{"prompt":"Add a new NuGet package"}' | npx tsx .claude/hooks/skill-activation-prompt.ts

echo "Testing playwright-dotnet..."
echo '{"prompt":"Install Playwright browsers"}' | npx tsx .claude/hooks/skill-activation-prompt.ts

echo ""
echo "=== MEDIUM PRIORITY SKILLS ==="

echo "Testing dotnet-minimal-apis..."
echo '{"prompt":"Create a minimal API endpoint"}' | npx tsx .claude/hooks/skill-activation-prompt.ts

echo "Testing dotnet-cli-essentials..."
echo '{"prompt":"How do I build the solution?"}' | npx tsx .claude/hooks/skill-activation-prompt.ts

echo "Testing aspnet-configuration..."
echo '{"prompt":"Configure appsettings.json"}' | npx tsx .claude/hooks/skill-activation-prompt.ts
```

**Acceptance Criteria:**
- [ ] Test script created
- [ ] All 6 skills trigger correctly
- [ ] Script organized by priority

---

#### Task 8.3: Commit Changes (10 min)
**Effort:** S

**Git Workflow:**
```bash
# Add all 6 skills
git add .claude/skills/mstest-testing-platform/
git add .claude/skills/dotnet-centralized-packages/
git add .claude/skills/playwright-dotnet/
git add .claude/skills/dotnet-minimal-apis/
git add .claude/skills/dotnet-cli-essentials/
git add .claude/skills/aspnet-configuration/
git add .claude/skills/skill-rules.json
git add .claude/README.md
git add tests/skill-validation.sh
git commit -m "Add six project-specific .NET 10 skills

High priority:
- mstest-testing-platform
- dotnet-centralized-packages
- playwright-dotnet

Medium priority:
- dotnet-minimal-apis
- dotnet-cli-essentials
- aspnet-configuration

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

**Acceptance Criteria:**
- [ ] All 6 skill directories committed
- [ ] skill-rules.json updated with all triggers
- [ ] Documentation updated
- [ ] Commit message follows format
- [ ] No sensitive data committed

---

## Risk Assessment and Mitigation

### Risk 1: Skill Content Exceeds 500 Lines
**Likelihood:** Medium
**Impact:** Low
**Mitigation:**
- Monitor line count during writing
- Use reference files if needed (e.g., EXAMPLES.md)
- Prioritize most common use cases in main SKILL.md

### Risk 2: Triggers Too Broad (False Positives)
**Likelihood:** Medium
**Impact:** Medium
**Mitigation:**
- Test with variety of prompts
- Refine keywords to be more specific
- Use intent patterns carefully

### Risk 3: Skill Content Becomes Outdated
**Likelihood:** High (preview .NET version)
**Impact:** Medium
**Mitigation:**
- Add version numbers in skill descriptions
- Note "as of .NET 10 RC 2"
- Plan for updates when .NET 10 RTM releases

### Risk 4: Duplicate Information Across Skills
**Likelihood:** Low
**Impact:** Low
**Mitigation:**
- Cross-reference between skills
- Focus each skill on its specialty
- Link to other skills where appropriate

---

## Success Metrics

### Quantitative
- [ ] 6 skills created and committed (or 3 high-priority minimum)
- [ ] All skills under 500 lines
- [ ] 100% trigger test pass rate
- [ ] 0 JSON validation errors
- [ ] Total implementation time < 8 hours (or < 4.5 hours for high-priority only)

### Qualitative
- [ ] Skills provide accurate, project-specific guidance
- [ ] Triggers activate on expected prompts
- [ ] No false positives on unrelated topics
- [ ] Skills reference real project examples
- [ ] Documentation is clear and actionable

### Usage Metrics (Post-Implementation)
- Track how often skills activate in actual usage
- Collect feedback on skill helpfulness
- Monitor for false positives/negatives
- Update triggers based on real-world usage

---

## Required Resources and Dependencies

### Tools & Infrastructure
✅ Claude Code with hooks installed
✅ skill-developer skill available
✅ Node.js and npx (for hook testing)
✅ Python 3 (for JSON validation)
✅ Git (for version control)

### Reference Materials
- Microsoft.Testing.Platform documentation
- CPM documentation (learn.microsoft.com)
- Playwright .NET documentation
- Project files (*.csproj, *.props, *.cs)

### Time Allocation
- Phase 1 (MSTest): 90 minutes
- Phase 2 (CPM): 80 minutes
- Phase 3 (Playwright): 90 minutes
- Phase 4 (Integration): 30 minutes
- **Total:** ~4.5 hours

### Skills Required
- Understanding of Claude Code skill system
- .NET/C# knowledge
- YAML frontmatter syntax
- Regular expressions (for intent patterns)
- Markdown formatting

---

## Timeline Estimates

### Phase Time Breakdown
- Phase 1: MSTest Testing Platform - 90 minutes
- Phase 2: Centralized Packages - 80 minutes
- Phase 3: Playwright .NET - 90 minutes
- Phase 4: Minimal APIs - 75 minutes
- Phase 5: .NET CLI Essentials - 70 minutes
- Phase 6: ASP.NET Configuration - 75 minutes
- Phase 7: Integration & Documentation - 45 minutes
**Total:** ~8 hours

---

### Option 1: High Priority Skills Only (Recommended Start)
**Total Time:** ~4.5 hours
**Approach:** Complete the 3 unique/less-documented skills first

**Session 1 (90 min):**
- Complete Phase 1 (MSTest skill)
- Test and validate
- Commit

**Session 2 (80 min):**
- Complete Phase 2 (CPM skill)
- Test and validate
- Commit

**Session 3 (2 hours):**
- Complete Phase 3 (Playwright skill)
- Complete Phase 8 (Integration - partial)
- Final validation
- Commit

**Then decide:** Assess value and continue with medium-priority skills if beneficial

---

### Option 2: Phased Approach - All Six Skills
**Total Time:** 5-6 sessions over 2-3 days
**Approach:** One or two skills per session, validate before proceeding

**Session 1 (90 min):**
- Phase 1: MSTest skill
- Validate and commit

**Session 2 (80 min):**
- Phase 2: CPM skill
- Validate and commit

**Session 3 (90 min):**
- Phase 3: Playwright skill
- Validate and commit

**Session 4 (75 min):**
- Phase 5: Minimal APIs skill
- Validate and commit

**Session 5 (70 min):**
- Phase 6: .NET CLI skill
- Validate and commit

**Session 6 (2 hours):**
- Phase 7: ASP.NET Configuration skill
- Phase 8: Integration & Documentation
- Final validation
- Commit

**Benefits:**
- Can validate each skill individually
- Easier to adjust based on learnings
- Less mental fatigue
- Can incorporate feedback between sessions
- Can stop after high-priority skills if desired

---

### Option 3: Marathon Session (Not Recommended)
**Total Time:** 7-8 hours
**Approach:** Complete all six skills in one day

**Not recommended because:**
- Mental fatigue affects quality
- No validation checkpoints
- Hard to adjust if issues arise
- Difficult to maintain focus for 8 hours

---

## Next Steps

**Immediate Actions:**
1. Review and approve this plan
2. Choose implementation approach (single vs phased)
3. Block time on calendar
4. Prepare development environment

**Before Starting:**
- [ ] Ensure Claude Code hooks are working
- [ ] Test skill-developer skill activation
- [ ] Verify access to project files
- [ ] Clear any Git working directory issues

**After Completion:**
- [ ] Test all skills in real usage
- [ ] Document any issues found
- [ ] Plan for skill maintenance/updates
- [ ] Consider additional skills if successful

---

## Appendix: Skill Priority Justification

### Why These Three Skills?

**1. MSTest Testing Platform (Highest Priority)**
- 4 test projects depend on this
- New technology (less Stack Overflow content)
- Project-specific gotcha (--test-runner flag issue)
- Common developer task (running tests)

**2. Centralized Package Management (High Priority)**
- Affects every package addition
- Easy to make mistakes (adding Version= to .csproj)
- Project-wide impact
- Less common pattern (not default .NET)

**3. Playwright .NET (High Priority)**
- 2 dedicated Playwright projects
- Specific .NET integration patterns
- Browser installation is non-trivial
- E2E testing is complex

### Skills Deferred (Lower Priority)

**dotnet-minimal-apis**
- Only 1 project uses it
- Well-documented by Microsoft
- Patterns are straightforward

**aspnet-mvc-razor**
- Well-established technology
- Abundant documentation available
- Standard patterns in use

**dotnet-explicit-usings**
- Minor inconvenience
- IDE handles well
- Easy to look up

---

**Plan Status:** READY FOR IMPLEMENTATION
**Next Action:** Review plan and choose implementation approach
