# Project-Specific Skills - Task Checklist

**Last Updated:** 2025-11-01

---

## Phase 1: MSTest Testing Platform Skill (90 min)

### Research & Planning
- [ ] 1.1.1 Examine all 4 test project .csproj files
- [ ] 1.1.2 Review MSTestSettings.cs configurations
- [ ] 1.1.3 Document test running patterns
- [ ] 1.1.4 Identify common issues/questions
- [ ] 1.1.5 Create notes document

### Skill Creation
- [ ] 1.2.1 Create `.claude/skills/mstest-testing-platform/` directory
- [ ] 1.2.2 Create `SKILL.md` with YAML frontmatter
- [ ] 1.2.3 Add placeholder sections

### Content Writing
- [ ] 1.3.1 Write Prerequisites section
- [ ] 1.3.2 Write Microsoft.Testing.Platform vs VSTest comparison
- [ ] 1.3.3 Write Project setup section
- [ ] 1.3.4 Write Running tests section (dotnet run vs dotnet test)
- [ ] 1.3.5 Write MSTestSettings.cs configuration
- [ ] 1.3.6 Write Test parallelization section
- [ ] 1.3.7 Write Debugging techniques
- [ ] 1.3.8 Write CI/CD integration
- [ ] 1.3.9 Write Troubleshooting section
- [ ] 1.3.10 Write Quick reference
- [ ] 1.3.11 Verify line count under 500

### Trigger Configuration
- [ ] 1.4.1 Add entry to skill-rules.json
- [ ] 1.4.2 Define keywords array
- [ ] 1.4.3 Define intent patterns
- [ ] 1.4.4 (Optional) Define file patterns
- [ ] 1.4.5 Validate JSON syntax

### Testing
- [ ] 1.5.1 Test: "How do I run MSTest tests?"
- [ ] 1.5.2 Test: "Configure test parallelization"
- [ ] 1.5.3 Test: "Debug unit tests"
- [ ] 1.5.4 Test: "EnableMSTestRunner setup"
- [ ] 1.5.5 Verify no false positives
- [ ] 1.5.6 Fix any issues found

### Completion
- [ ] 1.6.1 Update .claude/README.md
- [ ] 1.6.2 Git add and commit

---

## Phase 2: Centralized Package Management Skill (80 min)

### Research & Planning
- [ ] 2.1.1 Analyze Directory.Packages.props structure
- [ ] 2.1.2 Analyze Directory.Build.props settings
- [ ] 2.1.3 Document package addition process
- [ ] 2.1.4 List all current packages
- [ ] 2.1.5 Identify project conventions

### Skill Creation
- [ ] 2.2.1 Create `.claude/skills/dotnet-centralized-packages/` directory
- [ ] 2.2.2 Create `SKILL.md` with frontmatter
- [ ] 2.2.3 Set up section structure

### Content Writing
- [ ] 2.3.1 Write "What is CPM" section
- [ ] 2.3.2 Write Directory.Packages.props structure
- [ ] 2.3.3 Write "Adding packages correctly" (with examples)
- [ ] 2.3.4 Write "Updating package versions"
- [ ] 2.3.5 Write CentralPackageTransitivePinningEnabled section
- [ ] 2.3.6 Write Troubleshooting version conflicts
- [ ] 2.3.7 Write Visual Studio vs CLI workflows
- [ ] 2.3.8 Write Common errors and solutions
- [ ] 2.3.9 Write Best practices
- [ ] 2.3.10 Write Quick reference
- [ ] 2.3.11 Add real project examples (Razor, MSTest, Playwright packages)
- [ ] 2.3.12 Verify line count under 500

### Trigger Configuration
- [ ] 2.4.1 Add entry to skill-rules.json
- [ ] 2.4.2 Define keywords array
- [ ] 2.4.3 Define intent patterns
- [ ] 2.4.4 Validate JSON syntax

### Testing
- [ ] 2.5.1 Test: "How do I add a new NuGet package?"
- [ ] 2.5.2 Test: "Update package version"
- [ ] 2.5.3 Test: "Centralized package management"
- [ ] 2.5.4 Test: "Directory.Packages.props"
- [ ] 2.5.5 Verify no false positives
- [ ] 2.5.6 Fix any issues found

### Completion
- [ ] 2.6.1 Update .claude/README.md
- [ ] 2.6.2 Git add and commit

---

## Phase 3: Playwright .NET Testing Skill (90 min)

### Research & Planning
- [ ] 3.1.1 Examine ClaudeStack.Web.Tests.Playwright project
- [ ] 3.1.2 Examine ClaudeStack.API.Tests.Playwright project
- [ ] 3.1.3 Review .csproj configurations
- [ ] 3.1.4 Document browser installation process
- [ ] 3.1.5 Review Test1.cs patterns in both projects
- [ ] 3.1.6 Identify test patterns used

### Skill Creation
- [ ] 3.2.1 Create `.claude/skills/playwright-dotnet/` directory
- [ ] 3.2.2 Create `SKILL.md` with frontmatter
- [ ] 3.2.3 Set up section structure

### Content Writing
- [ ] 3.3.1 Write Installing Playwright for .NET
- [ ] 3.3.2 Write Browser installation (playwright.ps1 install)
- [ ] 3.3.3 Write Microsoft.Playwright.MSTest.v4 integration
- [ ] 3.3.4 Write Project setup and configuration
- [ ] 3.3.5 Write Writing first test (page object pattern)
- [ ] 3.3.6 Write Selectors and assertions
- [ ] 3.3.7 Write Test isolation and cleanup
- [ ] 3.3.8 Write Debugging (headed mode, screenshots, traces)
- [ ] 3.3.9 Write CI/CD setup
- [ ] 3.3.10 Write Common patterns
- [ ] 3.3.11 Write Troubleshooting
- [ ] 3.3.12 Write Quick reference
- [ ] 3.3.13 Include real project examples
- [ ] 3.3.14 Verify line count under 500

### Trigger Configuration
- [ ] 3.4.1 Add entry to skill-rules.json
- [ ] 3.4.2 Define keywords array
- [ ] 3.4.3 Define intent patterns
- [ ] 3.4.4 Define file patterns (*.Playwright.csproj)
- [ ] 3.4.5 Validate JSON syntax

### Testing
- [ ] 3.5.1 Test: "How do I install Playwright browsers?"
- [ ] 3.5.2 Test: "Create a Playwright test"
- [ ] 3.5.3 Test: "Debug E2E tests"
- [ ] 3.5.4 Test: "Playwright best practices"
- [ ] 3.5.5 Test file pattern trigger on .Playwright.csproj
- [ ] 3.5.6 Verify no false positives
- [ ] 3.5.7 Fix any issues found

### Completion
- [ ] 3.6.1 Update .claude/README.md
- [ ] 3.6.2 Git add and commit

---

## Phase 5: Minimal APIs Skill (75 min)

### Research & Planning
- [ ] 5.1.1 Examine ClaudeStack.API Program.cs
- [ ] 5.1.2 Document MapGet, MapPost, MapPut, MapDelete patterns
- [ ] 5.1.3 Review route group usage
- [ ] 5.1.4 Identify parameter binding patterns
- [ ] 5.1.5 Document response types and validation

### Skill Creation
- [ ] 5.2.1 Create `.claude/skills/dotnet-minimal-apis/` directory
- [ ] 5.2.2 Create `SKILL.md` with YAML frontmatter
- [ ] 5.2.3 Add placeholder sections

### Content Writing
- [ ] 5.3.1 Write Introduction to Minimal APIs
- [ ] 5.3.2 Write Program.cs structure section
- [ ] 5.3.3 Write Basic endpoints (MapGet, MapPost, MapPut, MapDelete)
- [ ] 5.3.4 Write Route groups (MapGroup)
- [ ] 5.3.5 Write Parameter binding (route, query, body, services)
- [ ] 5.3.6 Write Validation and filters
- [ ] 5.3.7 Write Response types (TypedResults, Results)
- [ ] 5.3.8 Write Middleware and CORS
- [ ] 5.3.9 Write OpenAPI/Swagger integration
- [ ] 5.3.10 Write Best practices
- [ ] 5.3.11 Write Troubleshooting section
- [ ] 5.3.12 Write Quick reference
- [ ] 5.3.13 Include real project examples
- [ ] 5.3.14 Verify line count under 500

### Trigger Configuration
- [ ] 5.4.1 Add entry to skill-rules.json
- [ ] 5.4.2 Define keywords array
- [ ] 5.4.3 Define intent patterns
- [ ] 5.4.4 Define file patterns (**/Program.cs in API projects)
- [ ] 5.4.5 Validate JSON syntax

### Testing
- [ ] 5.5.1 Test: "Create a minimal API endpoint"
- [ ] 5.5.2 Test: "How do I use MapGet?"
- [ ] 5.5.3 Test: "Configure route groups"
- [ ] 5.5.4 Test: "Add parameter binding"
- [ ] 5.5.5 Test file pattern trigger on Program.cs
- [ ] 5.5.6 Verify no false positives
- [ ] 5.5.7 Fix any issues found

### Completion
- [ ] 5.6.1 Update .claude/README.md
- [ ] 5.6.2 Git add and commit

---

## Phase 6: .NET CLI Essentials Skill (70 min)

### Research & Planning
- [ ] 6.1.1 Review sln.slnx structure
- [ ] 6.1.2 Document dotnet build/run/test patterns
- [ ] 6.1.3 Review Directory.Build.props integration
- [ ] 6.1.4 Document watch mode usage
- [ ] 6.1.5 Identify common CLI patterns

### Skill Creation
- [ ] 6.2.1 Create `.claude/skills/dotnet-cli-essentials/` directory
- [ ] 6.2.2 Create `SKILL.md` with frontmatter
- [ ] 6.2.3 Set up section structure

### Content Writing
- [ ] 6.3.1 Write Common dotnet commands overview
- [ ] 6.3.2 Write Solution file management (.slnx format)
- [ ] 6.3.3 Write Project management (new, add, remove)
- [ ] 6.3.4 Write Package management (add, remove, update)
- [ ] 6.3.5 Write Build operations (build, clean, restore)
- [ ] 6.3.6 Write Run operations (run, watch)
- [ ] 6.3.7 Write Test operations (test, dotnet run for MSTest)
- [ ] 6.3.8 Write Publish and deployment
- [ ] 6.3.9 Write Watch mode and hot reload
- [ ] 6.3.10 Write Best practices
- [ ] 6.3.11 Write Troubleshooting section
- [ ] 6.3.12 Write Quick reference
- [ ] 6.3.13 Include project-specific examples
- [ ] 6.3.14 Verify line count under 500

### Trigger Configuration
- [ ] 6.4.1 Add entry to skill-rules.json
- [ ] 6.4.2 Define keywords array
- [ ] 6.4.3 Define intent patterns
- [ ] 6.4.4 Define file patterns (*.slnx, Directory.Build.props)
- [ ] 6.4.5 Validate JSON syntax

### Testing
- [ ] 6.5.1 Test: "How do I build the project?"
- [ ] 6.5.2 Test: "dotnet watch"
- [ ] 6.5.3 Test: "Add project to solution"
- [ ] 6.5.4 Test: "dotnet run commands"
- [ ] 6.5.5 Test file pattern trigger on .slnx
- [ ] 6.5.6 Verify no false positives
- [ ] 6.5.7 Fix any issues found

### Completion
- [ ] 6.6.1 Update .claude/README.md
- [ ] 6.6.2 Git add and commit

---

## Phase 7: ASP.NET Configuration Skill (75 min)

### Research & Planning
- [ ] 7.1.1 Review appsettings.json and appsettings.Development.json
- [ ] 7.1.2 Document IConfiguration usage patterns
- [ ] 7.1.3 Identify options pattern implementations
- [ ] 7.1.4 Review user secrets setup
- [ ] 7.1.5 Document environment-specific configurations

### Skill Creation
- [ ] 7.2.1 Create `.claude/skills/aspnet-configuration/` directory
- [ ] 7.2.2 Create `SKILL.md` with frontmatter
- [ ] 7.2.3 Set up section structure

### Content Writing
- [ ] 7.3.1 Write appsettings.json structure
- [ ] 7.3.2 Write Environment-specific configuration
- [ ] 7.3.3 Write User secrets setup and management
- [ ] 7.3.4 Write Options pattern implementation
- [ ] 7.3.5 Write IConfiguration usage
- [ ] 7.3.6 Write Configuration binding
- [ ] 7.3.7 Write Environment variables
- [ ] 7.3.8 Write Configuration validation
- [ ] 7.3.9 Write Configuration in Minimal APIs vs MVC
- [ ] 7.3.10 Write Best practices
- [ ] 7.3.11 Write Troubleshooting section
- [ ] 7.3.12 Write Quick reference
- [ ] 7.3.13 Include project examples
- [ ] 7.3.14 Verify line count under 500

### Trigger Configuration
- [ ] 7.4.1 Add entry to skill-rules.json
- [ ] 7.4.2 Define keywords array
- [ ] 7.4.3 Define intent patterns
- [ ] 7.4.4 Define file patterns (**/appsettings*.json)
- [ ] 7.4.5 Validate JSON syntax

### Testing
- [ ] 7.5.1 Test: "How do I configure appsettings?"
- [ ] 7.5.2 Test: "Set up user secrets"
- [ ] 7.5.3 Test: "Options pattern"
- [ ] 7.5.4 Test: "Access IConfiguration"
- [ ] 7.5.5 Test file pattern trigger on appsettings.json
- [ ] 7.5.6 Verify no false positives
- [ ] 7.5.7 Fix any issues found

### Completion
- [ ] 7.6.1 Update .claude/README.md
- [ ] 7.6.2 Git add and commit

---

## Phase 8: Integration & Documentation (45 min)

### Documentation Updates
- [ ] 8.1.1 Update `.claude/README.md` with all six skills
- [ ] 8.1.2 Add usage examples for each skill
- [ ] 8.1.3 Update CLAUDE.md if needed
- [ ] 8.1.4 Document skill interaction patterns

### Validation Suite
- [ ] 8.2.1 Create `tests/skill-validation.sh` script
- [ ] 8.2.2 Add tests for mstest-testing-platform
- [ ] 8.2.3 Add tests for dotnet-centralized-packages
- [ ] 8.2.4 Add tests for playwright-dotnet
- [ ] 8.2.5 Add tests for dotnet-minimal-apis
- [ ] 8.2.6 Add tests for dotnet-cli-essentials
- [ ] 8.2.7 Add tests for aspnet-configuration
- [ ] 8.2.8 Make script executable
- [ ] 8.2.9 Run validation suite
- [ ] 8.2.10 Verify all skills trigger correctly

### Final Commit
- [ ] 8.3.1 Review all changes
- [ ] 8.3.2 Stage all skill files (6 skills)
- [ ] 8.3.3 Stage skill-rules.json
- [ ] 8.3.4 Stage documentation updates
- [ ] 8.3.5 Create comprehensive commit message
- [ ] 8.3.6 Commit changes
- [ ] 8.3.7 Push to remote (if desired)

---

## Post-Implementation

### Testing in Real Usage
- [ ] Use skills in actual development work
- [ ] Document any false positives
- [ ] Document any false negatives (missed triggers)
- [ ] Collect feedback on helpfulness

### Maintenance Planning
- [ ] Plan for .NET 10 RTM update
- [ ] Monitor for outdated information
- [ ] Track new patterns that emerge
- [ ] Consider additional skills if needed

---

## Success Criteria

### Per-Skill Completion
Each skill is complete when:
- [ ] SKILL.md created with valid frontmatter
- [ ] Content covers all planned sections
- [ ] Line count under 500
- [ ] Real project examples included
- [ ] Code examples tested
- [ ] Entry in skill-rules.json
- [ ] JSON validates successfully
- [ ] All trigger tests pass
- [ ] No false positives
- [ ] Committed to git

### Overall Project Completion
Project is complete when:
- [ ] All six skills created and tested
- [ ] Documentation updated
- [ ] Validation suite passes
- [ ] All changes committed
- [ ] Total time < 9 hours (or < 5 hours if high-priority only)

---

## Notes & Tracking

### Session 1 Notes
*Date: _____*
*Skills Completed: _____*
*Issues Encountered: _____*
*Time Spent: _____*

### Session 2 Notes
*Date: _____*
*Skills Completed: _____*
*Issues Encountered: _____*
*Time Spent: _____*

### Session 3 Notes
*Date: _____*
*Skills Completed: _____*
*Issues Encountered: _____*
*Time Spent: _____*

### Session 4 Notes
*Date: _____*
*Skills Completed: _____*
*Issues Encountered: _____*
*Time Spent: _____*

### Session 5 Notes
*Date: _____*
*Skills Completed: _____*
*Issues Encountered: _____*
*Time Spent: _____*

### Session 6 Notes
*Date: _____*
*Skills Completed: _____*
*Issues Encountered: _____*
*Time Spent: _____*

---

## Quick Progress Check

**Total Tasks:** 221
**Completed:** 0
**In Progress:** 0
**Remaining:** 221

**Phase 1 Progress (High Priority):** 0/29 tasks (mstest-testing-platform)
**Phase 2 Progress (High Priority):** 0/28 tasks (dotnet-centralized-packages)
**Phase 3 Progress (High Priority):** 0/33 tasks (playwright-dotnet)
**Phase 4 Progress (Medium Priority):** 0/31 tasks (dotnet-minimal-apis)
**Phase 5 Progress (Medium Priority):** 0/31 tasks (dotnet-cli-essentials)
**Phase 6 Progress (Medium Priority):** 0/31 tasks (aspnet-configuration)
**Phase 7 Progress (Integration):** 0/21 tasks

---

**Status:** READY TO START
**Next Task:** Phase 1, Task 1.1.1 - Examine test project .csproj files
**Option:** Start with high-priority skills (Phases 1-3) or all 6 skills
