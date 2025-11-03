# PRD: Phase 7 - Azure DevOps Plugin Implementation

**Document Version:** 1.0
**Status:** Not Started
**Priority:** Optional (Post-GitHub Implementation)
**Author:** Documentation Team
**Last Updated:** 2025-11-03

---

## Executive Summary

Phase 7 implements Azure DevOps integration for the AI-assisted documentation system, providing an alternative platform to GitHub for enterprise clients who use Azure DevOps as their primary DevOps platform. This phase leverages the platform-agnostic core infrastructure already established in Phase 2 to enable seamless multi-platform support.

**Key Deliverables:**
- 4 Azure Pipelines for documentation deployment
- Azure Static Web Apps integration for hosting
- Azure DevOps Wiki automation
- Platform switching capabilities (GitHub ” Azure DevOps)
- Enterprise-grade documentation infrastructure

**Business Value:**
- Expands addressable market to Azure DevOps enterprise clients
- Demonstrates platform portability and consultant flexibility
- Provides migration path for organizations moving between platforms
- Enhances credibility with Fortune 500 companies using Azure DevOps

**Estimated Effort:** 6-8 hours (vs 28-32 hours for equivalent GitHub implementation)
**Time Savings:** 70-75% due to platform-agnostic architecture and reusable patterns

---

## Problem Statement

### Current State

The AI-assisted documentation system is fully functional on GitHub with:
-  Three documentation types (Developer, User, Company/Wiki)
-  Platform-agnostic core infrastructure (`.docgen/` scripts)
-  GitHub Actions workflows for CI/CD
-  GitHub Pages hosting for static sites
-  GitHub Wiki integration for company docs

However, many enterprise clients use **Azure DevOps** as their primary platform:
- 68% of Fortune 500 companies use Microsoft Azure (Flexera 2023)
- Azure DevOps provides enterprise features (compliance, security, audit logs)
- Organizations may prefer Azure for strategic Microsoft alignment
- Some clients are migrating from GitHub to Azure DevOps or vice versa

### Gaps & Constraints

**Technical Gaps:**
- No Azure DevOps CI/CD pipelines for documentation
- No Azure Static Web Apps hosting configured
- No Azure DevOps Wiki automation
- No testing for Azure DevOps-specific workflows

**Business Constraints:**
- Requires Azure subscription ($0-$200/month depending on usage)
- Requires Azure DevOps organization (free tier available)
- Requires learning curve for Azure-specific tooling
- Optional phase - not blocking GitHub users

**User Pain Points:**
- Consultants working with Azure DevOps clients lack turnkey solution
- Organizations migrating platforms need dual support during transition
- Enterprise clients may require Azure DevOps for compliance reasons
- Current GitHub-only implementation limits reusability

---

## Goals & Objectives

### Primary Goals

1. **Enable Azure DevOps Platform Support**
   - Implement complete Azure DevOps plugin matching GitHub feature parity
   - Deploy documentation to Azure Static Web Apps
   - Automate Azure DevOps Wiki synchronization
   - Validate with end-to-end testing

2. **Demonstrate Platform Portability**
   - Prove platform-agnostic architecture works across platforms
   - Enable switching between GitHub and Azure DevOps
   - Maintain single codebase with platform plugins
   - Document migration procedures

3. **Provide Enterprise-Grade Solution**
   - Meet enterprise security and compliance requirements
   - Support Azure AD authentication integration
   - Enable audit logging and governance
   - Provide SLA-backed hosting (Azure SWA 99.95% uptime)

### Success Criteria

| Metric | Target | Measurement |
|--------|--------|-------------|
| Implementation time | < 8 hours | Actual time tracked |
| Code reuse from GitHub | > 80% | Lines of code analysis |
| Feature parity with GitHub | 100% | Feature comparison matrix |
| Pipeline success rate | > 95% | Pipeline run history |
| Documentation build time | < 5 minutes | Pipeline execution time |
| Platform switch time | < 5 minutes | Switching script execution |

### Non-Goals

- L Deprecating GitHub plugin (both platforms supported)
- L Implementing Azure-specific features not in GitHub (maintain parity)
- L Supporting other platforms (GitLab, Bitbucket, etc.)
- L Creating unified multi-platform pipeline (separate plugins maintained)

---

## User Stories

### Epic 1: Azure DevOps CI/CD Automation

**US-1.1: As a DevOps engineer, I want documentation to deploy automatically when I push to main, so I don't have to manually build and publish.**

**Acceptance Criteria:**
- Azure Pipeline triggers on push to main branch
- Pipeline builds developer docs, user docs, and wiki content
- Pipeline deploys to Azure Static Web Apps
- Pipeline completes in < 5 minutes
- Failed builds provide clear error messages

**US-1.2: As a developer, I want PR validation to check documentation quality, so issues are caught before merge.**

**Acceptance Criteria:**
- Pipeline triggers on pull request creation
- Markdown linting runs and reports issues
- DocFX builds validate syntax and cross-references
- XML documentation coverage checked
- PR blocked if validation fails

### Epic 2: Azure Static Web Apps Hosting

**US-2.1: As a user, I want to access documentation at a stable URL, so I can bookmark and share documentation.**

**Acceptance Criteria:**
- Developer docs deployed to Azure SWA root path
- User docs deployed to Azure SWA `/user/` path
- Custom domain configuration supported (optional)
- HTTPS enabled by default
- 99.95% uptime SLA

**US-2.2: As a documentation author, I want near-instant deployment, so changes are visible within minutes.**

**Acceptance Criteria:**
- Deployment completes within 3-5 minutes of push
- No manual approval required for main branch
- Deployment status visible in Azure Portal
- Rollback capability available

### Epic 3: Azure DevOps Wiki Integration

**US-3.1: As a team member, I want company documentation in Azure DevOps Wiki, so it's easy to find and edit.**

**Acceptance Criteria:**
- Wiki content syncs from `docs/wiki/` directory
- README.md converts to wiki homepage
- Wiki sidebar shows navigation
- Updates within 5 minutes of push to main
- Wiki editing through Azure DevOps UI preserved

**US-3.2: As a project manager, I want wiki content versioned with code, so documentation changes are tracked.**

**Acceptance Criteria:**
- Wiki content stored in git repository
- Changes appear in git history
- Pull requests can include wiki changes
- Wiki syncs bidirectionally (code ’ wiki)

### Epic 4: Platform Switching

**US-4.1: As a consultant, I want to switch documentation platforms easily, so I can adapt to client requirements.**

**Acceptance Criteria:**
- Single command switches from GitHub to Azure DevOps
- DocFX git URLs update automatically
- GitHub workflows disable, Azure pipelines enable
- Process reversible with no data loss
- Switch completes in < 2 minutes

**US-4.2: As an architect, I want platform migration documentation, so I can plan migrations confidently.**

**Acceptance Criteria:**
- Step-by-step migration guide provided
- Rollback procedures documented
- Testing checklist included
- Troubleshooting guide comprehensive
- Estimated migration time provided (1-3 hours)

---

## Technical Requirements

### Functional Requirements

#### FR-1: Azure Pipeline Definitions

**FR-1.1: Developer Documentation Pipeline**
- Trigger: Push to main, changes in `docs/docfx-developer/**` or `src/**`
- Steps: Install tools, build solution, generate diagrams, build DocFX, deploy to Azure SWA
- Artifacts: Developer documentation site
- Deployment: Azure Static Web Apps root path
- Status: Required

**FR-1.2: User Documentation Pipeline**
- Trigger: Push to main, changes in `docs/docfx-user/**`
- Steps: Install tools, build DocFX, deploy to Azure SWA
- Artifacts: User documentation site
- Deployment: Azure Static Web Apps `/user/` path
- Status: Required

**FR-1.3: Wiki Synchronization Pipeline**
- Trigger: Push to main, changes in `docs/wiki/**`
- Steps: Checkout wiki repo, copy files, commit, push
- Artifacts: None (syncs to Azure DevOps Wiki)
- Deployment: Azure DevOps Wiki
- Status: Required

**FR-1.4: PR Validation Pipeline**
- Trigger: Pull request to main
- Steps: Markdown lint, DocFX build validation, XML doc coverage check
- Artifacts: Validation report
- Deployment: PR comment with results
- Status: Required

#### FR-2: Azure Resources

**FR-2.1: Resource Group**
- Name: `rg-docs-{environment}`
- Location: East US (or nearest region)
- Purpose: Container for documentation resources
- Cost: Free

**FR-2.2: Azure Static Web App (Developer Docs)**
- Name: `swa-docs-developer-{environment}`
- SKU: Free tier (100 GB bandwidth/month)
- Build preset: Custom
- Deployment: Developer docs to root path
- Custom domain: Optional

**FR-2.3: Azure Static Web App (User Docs)**
- Name: `swa-docs-user-{environment}`
- SKU: Free tier
- Build preset: Custom
- Deployment: User docs to root path (or `/user/` on primary SWA)
- Custom domain: Optional

**FR-2.4: Service Connection**
- Type: Azure Resource Manager
- Scope: Resource Group
- Purpose: Pipeline deployment authorization
- Authentication: Service Principal (automated) or Managed Identity

#### FR-3: Azure DevOps Wiki Configuration

**FR-3.1: Wiki Provisioning**
- Type: Publish code as wiki
- Repository: Main repository
- Branch: main
- Folder: `/docs/wiki`
- Auto-sync: Via pipeline

**FR-3.2: Wiki Content Structure**
- Homepage: README.md ’ Wiki home
- Navigation: Auto-generated from folder structure
- Ordering: Via `.order` file
- Mermaid support: Native rendering

#### FR-4: Platform Detection & Switching

**FR-4.1: Enhanced Platform Detection**
- Detect Azure DevOps via `TF_BUILD` environment variable
- Load Azure-specific configuration from `platform-config.json`
- Auto-detect from git remote: `dev.azure.com/{org}/{project}/_git/{repo}`
- Return Azure DevOps URLs for DocFX configuration

**FR-4.2: Platform Switching Script**
- Update `platform-config.json` defaultPlatform to AzureDevOps
- Update DocFX git URLs to Azure DevOps format
- Disable GitHub workflows (move to `.github/workflows.disabled/`)
- Enable Azure Pipelines (move from `.azuredevops/pipelines.disabled/`)
- Provide next steps guidance

### Non-Functional Requirements

#### NFR-1: Performance
- Documentation build time: < 5 minutes per pipeline
- Full deployment (all 3 doc types): < 10 minutes
- Wiki sync delay: < 3 minutes after push
- Platform switch execution: < 2 minutes

#### NFR-2: Reliability
- Pipeline success rate: > 95% (excluding known issues)
- Azure SWA uptime: 99.95% (Microsoft SLA)
- Automatic retry on transient failures
- Graceful degradation if wiki sync fails

#### NFR-3: Security
- Azure AD authentication for pipeline access
- Service Principal with least-privilege permissions
- Secrets stored in Azure Key Vault or Azure DevOps Library
- No credentials committed to repository
- Audit logging enabled for all pipeline runs

#### NFR-4: Maintainability
- YAML pipelines follow Azure DevOps best practices
- PowerShell scripts compatible with PowerShell 7.0+
- Clear error messages with troubleshooting guidance
- Comprehensive inline comments
- Versioned pipeline templates for reusability

#### NFR-5: Portability
- Scripts run on windows-latest and ubuntu-latest agents
- No hard-coded organization/project names
- Configuration externalized to `platform-config.json`
- Azure CLI commands compatible with v2.50+

---

## Architecture & Design

### High-Level Architecture

```
                                                             
                     Azure DevOps                             
                                                               
                                                       
   Azure Repos       ¶   Pipelines      ¶ Deployments  
                                                       
                                                            
         <                     <                     <        
                                                    
          ¼                     ¼                     ¼
                                                         
    docs/               .azuredevops/     Azure Static   
    - docfx-dev/        pipelines/        Web Apps       
    - docfx-user/       - developer       - Developer    
    - wiki/             - user            - User         
                        - wiki                           
                          - pr-val                 
                                                   ¼
                                                              
                                               Azure DevOps   
                                             ¶ Wiki           
                                                               
```

### Directory Structure

```
.azuredevops/
   pipelines/
      docs-developer-deploy.yml       # Developer docs pipeline
      docs-user-deploy.yml            # User docs pipeline
      docs-wiki-sync.yml              # Wiki sync pipeline
      docs-pr-validation.yml          # PR validation pipeline
      templates/                       # Reusable templates
          setup-dotnet.yml            # .NET SDK setup
          setup-docfx.yml             # DocFX installation
          deploy-swa.yml              # Azure SWA deployment
   scripts/
       deploy-wiki.ps1                 # Wiki deployment script
       validate-docs.ps1               # Documentation validation
       setup-azure-resources.ps1       # Resource provisioning

.docgen/
   detect-platform.ps1                 # Enhanced with Azure detection
   switch-platform.ps1                 # Enhanced with Azure switching
   platform-config.json                # Updated with Azure settings

docs/
   architecture/
      azure-devops-setup-guide.md     # Azure DevOps configuration
      azure-swa-deployment.md         # Azure SWA deployment guide
   PRDs/
       phase-7-azure-devops-plugin.md  # This document
```

### Azure Pipeline Design

#### Developer Docs Pipeline Architecture

```yaml
# .azuredevops/pipelines/docs-developer-deploy.yml
trigger:
  branches:
    include: [main]
  paths:
    include: [docs/docfx-developer/**, src/**]

pool:
  vmImage: 'windows-latest'

stages:
  - stage: Build
    jobs:
      - job: BuildDocs
        steps:
          - template: templates/setup-dotnet.yml
          - template: templates/setup-docfx.yml
          - script: dotnet build
          - script: docfx build docs/docfx-developer/docfx.json
          - publish: docs/docfx-developer/_site

  - stage: Deploy
    dependsOn: Build
    jobs:
      - deployment: DeployToAzure
        environment: production
        strategy:
          runOnce:
            deploy:
              steps:
                - template: templates/deploy-swa.yml
                  parameters:
                    appLocation: '$(Pipeline.Workspace)/_site'
                    resourceName: 'swa-docs-developer-prod'
```

### Azure Static Web Apps Configuration

**Option 1: Separate SWAs (Recommended)**
- Developer docs: `https://happy-stone-123abc.azurestaticapps.net/`
- User docs: `https://proud-wave-456def.azurestaticapps.net/`
- Advantages: Independent scaling, separate custom domains, clearer analytics

**Option 2: Single SWA with Routes**
- Developer docs: `https://docs.azurestaticapps.net/`
- User docs: `https://docs.azurestaticapps.net/user/`
- Advantages: Single deployment, unified domain, lower cost

**Recommendation:** Option 1 for production, Option 2 for cost-sensitive scenarios

### Wiki Sync Mechanism

```
              
 Push to main 
 (docs/wiki/)
      ,       
       
       ¼
                         
 Wiki Sync Pipeline      
 1. Checkout main repo   
 2. Clone wiki repo      
 3. Copy docs/wiki/ ’    
    wiki repo            
 4. Rename README ’ Home 
 5. Create .order file   
 6. Commit & push        
      ,                  
       
       ¼
                         
 Azure DevOps Wiki       
 (visible in UI)         
                         
```

---

## Implementation Tasks

### Task 7.1: Azure Resource Provisioning (1 hour)

**Description:** Create Azure resources for hosting documentation.

**Subtasks:**
1. Create Azure Resource Group
   ```bash
   az group create --name rg-docs-prod --location eastus
   ```

2. Create Azure Static Web App (Developer)
   ```bash
   az staticwebapp create \
     --name swa-docs-developer-prod \
     --resource-group rg-docs-prod \
     --location eastus2 \
     --sku Free
   ```

3. Create Azure Static Web App (User) - Optional
   ```bash
   az staticwebapp create \
     --name swa-docs-user-prod \
     --resource-group rg-docs-prod \
     --location eastus2 \
     --sku Free
   ```

4. Retrieve deployment tokens
   ```bash
   az staticwebapp secrets list \
     --name swa-docs-developer-prod \
     --resource-group rg-docs-prod \
     --query "properties.apiKey" -o tsv
   ```

5. Store tokens in Azure DevOps Library
   - Create variable group: `docs-deployment-secrets`
   - Add variables: `SWA_DEVELOPER_TOKEN`, `SWA_USER_TOKEN`
   - Mark as secret

**Acceptance Criteria:**
- [ ] Resource group created
- [ ] 2 Azure Static Web Apps created (or 1 if using single SWA)
- [ ] Deployment tokens stored securely
- [ ] Resources visible in Azure Portal
- [ ] Total cost: $0/month (Free tier)

**Deliverables:**
- Azure resources provisioned
- Deployment tokens stored
- Resource configuration documented

**Estimated Time:** 1 hour

---

### Task 7.2: Create Azure Pipelines Directory (15 min)

**Description:** Set up directory structure for Azure Pipeline definitions.

**Subtasks:**
1. Create `.azuredevops/pipelines/` directory
2. Create `.azuredevops/scripts/` directory
3. Create `.azuredevops/pipelines/templates/` directory
4. Create `.azuredevops/README.md` with overview
5. Create placeholder YAML files (4 pipelines)

**Acceptance Criteria:**
- [ ] Directory structure matches design
- [ ] README.md explains pipeline purpose
- [ ] Placeholder files created
- [ ] .gitignore updated if needed

**Deliverables:**
- `.azuredevops/` directory structure
- README.md documentation

**Estimated Time:** 15 minutes

---

### Task 7.3: Implement Developer Docs Pipeline (2.5 hours)

**Description:** Create Azure Pipeline for building and deploying developer documentation.

**Subtasks:**
1. Create `docs-developer-deploy.yml` (main pipeline)
   - Trigger configuration
   - Windows agent pool
   - Build and deploy stages
   - Environment approval (optional)

2. Create `templates/setup-dotnet.yml`
   - Install .NET SDK from global.json
   - Cache NuGet packages
   - Restore dependencies

3. Create `templates/setup-docfx.yml`
   - Install DocFX tool
   - Install diagram generation tools
   - Cache tool installations

4. Create `templates/deploy-swa.yml`
   - Use Azure/static-web-apps-deploy action
   - Configure app location and output location
   - Handle deployment failures

5. Test pipeline locally with `az pipelines run`
6. Debug and fix any issues
7. Document pipeline in comments

**Acceptance Criteria:**
- [ ] Pipeline triggers on push to main
- [ ] Pipeline builds solution successfully
- [ ] DocFX generates documentation
- [ ] Deployment to Azure SWA succeeds
- [ ] Pipeline completes in < 5 minutes
- [ ] Build artifacts available for download

**Deliverables:**
- `docs-developer-deploy.yml` (60-80 lines)
- 3 reusable templates (20-30 lines each)
- Pipeline tested and working

**Estimated Time:** 2.5 hours

---

### Task 7.4: Implement User Docs Pipeline (1.5 hours)

**Description:** Create Azure Pipeline for building and deploying user documentation.

**Subtasks:**
1. Create `docs-user-deploy.yml`
   - Similar structure to developer pipeline
   - No solution build required
   - Deploy to separate SWA or `/user/` path

2. Reuse templates from Task 7.3
   - setup-docfx.yml
   - deploy-swa.yml

3. Configure deployment parameters
   - Target SWA resource
   - Output location
   - Custom domain (optional)

4. Test and validate deployment

**Acceptance Criteria:**
- [ ] Pipeline triggers on `docs/docfx-user/**` changes
- [ ] Builds user documentation successfully
- [ ] Deploys to correct location
- [ ] No interference with developer docs
- [ ] Pipeline completes in < 3 minutes

**Deliverables:**
- `docs-user-deploy.yml` (40-50 lines)
- Tested and working pipeline

**Estimated Time:** 1.5 hours

---

### Task 7.5: Implement Wiki Sync Pipeline (3 hours)

**Description:** Create Azure Pipeline to synchronize wiki content to Azure DevOps Wiki.

**Subtasks:**
1. Create `docs-wiki-sync.yml`
   - Trigger on `docs/wiki/**` changes
   - Ubuntu agent (git operations)
   - Checkout wiki repository
   - Copy and commit changes

2. Create `.azuredevops/scripts/deploy-wiki.ps1`
   - Clone wiki repository via Azure DevOps API
   - Copy `docs/wiki/` content
   - Rename README.md to Wiki homepage
   - Create `.order` file for navigation
   - Commit and push changes
   - Handle conflicts gracefully

3. Configure Wiki in Azure DevOps
   - Enable Wiki feature
   - Publish code as wiki
   - Set source folder to `/docs/wiki`
   - Configure permissions

4. Set up authentication
   - Use `$(System.AccessToken)`
   - Grant Build Service permissions to wiki
   - Test authentication

5. Test wiki sync end-to-end

**Acceptance Criteria:**
- [ ] Pipeline triggers on wiki changes
- [ ] Wiki content syncs within 3 minutes
- [ ] README.md becomes wiki homepage
- [ ] Navigation structure preserved
- [ ] Mermaid diagrams render correctly
- [ ] No manual intervention required

**Deliverables:**
- `docs-wiki-sync.yml` (50-60 lines)
- `deploy-wiki.ps1` (100-150 lines)
- Wiki configured and working

**Estimated Time:** 3 hours

---

### Task 7.6: Implement PR Validation Pipeline (1.5 hours)

**Description:** Create Azure Pipeline for pull request validation.

**Subtasks:**
1. Create `docs-pr-validation.yml`
   - Trigger on pull requests to main
   - Paths: `docs/**`, `src/**/*.cs`
   - Ubuntu agent for speed

2. Add validation steps
   - Install markdownlint-cli
   - Lint markdown files
   - Build all documentation
   - Check XML documentation coverage
   - Generate validation report

3. Create `.azuredevops/scripts/validate-docs.ps1`
   - Run all validation checks
   - Collect results
   - Format report for PR comment

4. Configure PR comment posting
   - Use Azure DevOps REST API
   - Post validation summary
   - Include pass/fail status
   - Link to detailed logs

5. Test with sample PRs

**Acceptance Criteria:**
- [ ] Pipeline runs on every PR
- [ ] Markdown linting catches issues
- [ ] DocFX build validates syntax
- [ ] XML doc coverage checked
- [ ] Results posted as PR comment
- [ ] PR blocked if validation fails

**Deliverables:**
- `docs-pr-validation.yml` (60-70 lines)
- `validate-docs.ps1` (80-100 lines)
- PR validation working

**Estimated Time:** 1.5 hours

---

### Task 7.7: Configure Azure DevOps Settings (15 min)

**Description:** Configure Azure DevOps project settings for documentation pipelines.

**Subtasks:**
1. Grant pipeline permissions
   - Allow pipelines to access Azure resources
   - Grant Build Service access to wiki repository
   - Configure service connections

2. Configure branch policies
   - Require PR validation pipeline to pass
   - Require 1 reviewer (optional)
   - Enable build validation for main branch

3. Set up environments
   - Create `production` environment
   - Add approval gates (optional)
   - Configure deployment history retention

4. Configure notifications
   - Build failure notifications
   - Deployment success notifications
   - PR validation results

**Acceptance Criteria:**
- [ ] Pipelines can deploy to Azure
- [ ] Build Service can access wiki
- [ ] Branch policies enforce validation
- [ ] Notifications configured

**Deliverables:**
- Azure DevOps project configured
- Settings documented

**Estimated Time:** 15 minutes

---

### Task 7.8: End-to-End Testing (1.5 hours)

**Description:** Validate complete Azure DevOps documentation workflow.

**Test Scenarios:**

1. **Test Scenario 1: Developer Docs Deployment**
   - Make change to `src/Example.Web/Controllers/HomeController.cs`
   - Push to main branch
   - Verify pipeline triggers
   - Verify build succeeds
   - Verify deployment to Azure SWA
   - Access developer docs URL
   - Confirm changes visible

2. **Test Scenario 2: User Docs Deployment**
   - Make change to `docs/docfx-user/articles/getting-started.md`
   - Push to main branch
   - Verify pipeline triggers
   - Verify deployment to Azure SWA
   - Access user docs URL
   - Confirm changes visible

3. **Test Scenario 3: Wiki Sync**
   - Make change to `docs/wiki/system-purpose.md`
   - Push to main branch
   - Verify pipeline triggers
   - Wait for sync completion
   - Access Azure DevOps Wiki
   - Confirm changes visible

4. **Test Scenario 4: PR Validation**
   - Create branch with documentation change
   - Create pull request
   - Verify validation pipeline triggers
   - Check PR comment with results
   - Fix any issues
   - Verify PR can be merged

5. **Test Scenario 5: Platform Switching**
   - Switch from GitHub to Azure DevOps
   - Verify configuration changes
   - Build all documentation
   - Deploy to Azure DevOps
   - Switch back to GitHub
   - Verify reversibility

**Acceptance Criteria:**
- [ ] All test scenarios pass
- [ ] No manual intervention required
- [ ] Documentation accessible at expected URLs
- [ ] Platform switching works bidirectionally
- [ ] Performance meets targets (< 5 min builds)

**Deliverables:**
- Test results documented
- Issues logged and fixed
- Testing checklist

**Estimated Time:** 1.5 hours

---

### Task 7.9: Create Azure DevOps Setup Guide (1 hour)

**Description:** Document Azure DevOps configuration and usage.

**Content Sections:**
1. Prerequisites (Azure subscription, Azure DevOps organization)
2. Resource provisioning step-by-step
3. Pipeline configuration
4. Service connection setup
5. Wiki configuration
6. Branch policy configuration
7. Troubleshooting guide
8. Cost estimation and optimization

**Acceptance Criteria:**
- [ ] Guide is comprehensive and clear
- [ ] Step-by-step instructions with commands
- [ ] Screenshots for Azure Portal steps
- [ ] Troubleshooting section covers common issues
- [ ] Cost analysis included

**Deliverables:**
- `docs/architecture/azure-devops-setup-guide.md` (500-700 lines)

**Estimated Time:** 1 hour

---

## Dependencies

### Internal Dependencies

| Dependency | Status | Blocker | Notes |
|------------|--------|---------|-------|
| Phase 2: Core Foundation |  Complete | No | Platform-agnostic scripts ready |
| Phase 3: Developer Docs |  Complete | No | DocFX configuration exists |
| Phase 4: User Docs |  Complete | No | User docs structure ready |
| Phase 5: Wiki Content |  Complete | No | Wiki content created |
| Phase 6: GitHub Plugin |  Complete | No | Provides pattern to follow |
| Phase 9: Platform Switching |  Complete | No | Scripts need Azure enhancement |

### External Dependencies

| Dependency | Required | Status | Mitigation |
|------------|----------|--------|------------|
| Azure Subscription | Yes | Unknown | Free tier available, $0/month |
| Azure DevOps Organization | Yes | Unknown | Free tier available |
| Azure CLI installed | Yes | Unknown | Install via curl/package manager |
| Azure DevOps Extension | Yes | Unknown | `az extension add --name azure-devops` |
| Service Principal | Optional | Unknown | Can use Managed Identity or manual auth |
| Custom Domain | Optional | Unknown | Not required, nice-to-have |

### Technical Dependencies

- **Azure Static Web Apps**: Free tier (100 GB bandwidth/month)
- **Azure DevOps Pipelines**: Free tier (1 free parallel job, 1800 min/month)
- **Azure DevOps Wiki**: Included with Azure DevOps (no additional cost)
- **PowerShell 7.0+**: Already installed in project environment
- **Azure CLI 2.50+**: Required for resource management
- **Git 2.30+**: Already available

---

## Risks & Mitigation

### Risk 1: Azure Subscription Costs

**Likelihood:** Medium
**Impact:** Medium
**Description:** Azure Static Web Apps and other resources may incur unexpected costs.

**Mitigation:**
- Use Free tier for Azure Static Web Apps (100 GB bandwidth/month)
- Use Free tier for Azure DevOps (up to 5 users)
- Set up cost alerts at $50/month threshold
- Monitor usage in Azure Cost Management
- Document cost optimization strategies

**Contingency:**
- Downgrade to single Azure SWA instead of two
- Use Azure DevOps Artifacts hosting instead of SWA (free alternative)
- Switch back to GitHub if costs prohibitive

---

### Risk 2: Azure DevOps Learning Curve

**Likelihood:** Medium
**Impact:** Low
**Description:** Team unfamiliar with Azure DevOps may struggle with pipeline configuration.

**Mitigation:**
- Leverage GitHub implementation as reference
- Use azure-devops skill for guidance
- Copy patterns from existing Azure Pipelines documentation
- Include comprehensive inline comments
- Create detailed setup guide

**Contingency:**
- Spend extra time on learning and testing
- Seek help from Azure DevOps community
- Use AI assistance (Claude Code) for pipeline generation

---

### Risk 3: Service Connection Authentication Issues

**Likelihood:** High
**Impact:** High
**Description:** Pipeline may fail to deploy to Azure due to authentication/permission issues.

**Mitigation:**
- Use Azure DevOps service connection wizard (simplifies setup)
- Grant Service Principal minimum required permissions
- Test service connection before pipeline implementation
- Document authentication troubleshooting steps
- Use Managed Identity if available (more secure)

**Contingency:**
- Fall back to manual deployment tokens
- Use Azure CLI authentication in pipeline
- Engage Azure support if needed

---

### Risk 4: Wiki Sync Conflicts

**Likelihood:** Low
**Impact:** Medium
**Description:** Wiki sync may conflict with manual edits made in Azure DevOps UI.

**Mitigation:**
- Document "code as source of truth" policy
- Add warning in wiki: "Edit in repository, not wiki"
- Implement conflict detection in sync script
- Provide manual resolution instructions
- Consider one-way sync only (repo ’ wiki)

**Contingency:**
- Manual conflict resolution documented
- Rollback capability in sync script
- Option to disable wiki sync and use manual updates

---

### Risk 5: Platform Switching Breaks Existing Setup

**Likelihood:** Low
**Impact:** High
**Description:** Switching platforms may disable working GitHub workflows or vice versa.

**Mitigation:**
- Thoroughly test switch script before using
- Implement dry-run mode for switch script
- Backup current state before switching
- Make switching reversible with single command
- Document rollback procedures

**Contingency:**
- Manual revert documented
- Git branch with pre-switch state
- Both platforms can run simultaneously during transition

---

## Success Metrics

### Quantitative Metrics

| Metric | Baseline (GitHub) | Target (Azure) | Measurement Method |
|--------|-------------------|----------------|-------------------|
| Implementation Time | 9.5-10 hours | 6-8 hours | Time tracking |
| Pipeline Success Rate | 95% | 95% | Azure DevOps analytics |
| Build Time (Developer) | < 5 min | < 5 min | Pipeline execution time |
| Build Time (User) | < 3 min | < 3 min | Pipeline execution time |
| Wiki Sync Delay | < 3 min | < 3 min | Time from push to visibility |
| Code Reuse | N/A | > 80% | Lines of code analysis |
| Cost per Month | $0 | < $10 | Azure Cost Management |

### Qualitative Metrics

| Metric | Success Criteria |
|--------|------------------|
| Feature Parity | 100% of GitHub features replicated |
| Documentation Quality | Setup guide rated 8/10+ by users |
| Ease of Switching | Single command, < 5 minutes |
| Reliability | No pipeline failures for 1 week |
| Maintainability | Code reviews pass with minor comments |

### Business Metrics

| Metric | Target | Rationale |
|--------|--------|-----------|
| Enterprise Adoption | 1+ client using Azure DevOps plugin | Validates market demand |
| Consultant Productivity | 50% faster client onboarding | Demonstrates ROI |
| Platform Flexibility | Successful migration demo | Proves architecture works |

---

## Timeline & Estimates

### Phase 7 Schedule

| Task | Duration | Dependencies | Owner |
|------|----------|--------------|-------|
| 7.1: Azure Resource Provisioning | 1 hour | Azure subscription | DevOps Engineer |
| 7.2: Create Pipeline Directory | 15 min | None | Developer |
| 7.3: Developer Docs Pipeline | 2.5 hours | 7.1, 7.2 | Developer |
| 7.4: User Docs Pipeline | 1.5 hours | 7.3 | Developer |
| 7.5: Wiki Sync Pipeline | 3 hours | 7.1, 7.2 | Developer |
| 7.6: PR Validation Pipeline | 1.5 hours | 7.2 | Developer |
| 7.7: Configure Azure DevOps | 15 min | 7.3, 7.4, 7.5 | DevOps Engineer |
| 7.8: End-to-End Testing | 1.5 hours | All above | QA/Developer |
| 7.9: Create Setup Guide | 1 hour | 7.8 | Technical Writer |

**Total Estimated Time:** 12 hours (raw task time)
**Adjusted for Context Switching:** 13-14 hours
**With AI Assistance:** 6-8 hours (50-60% time savings)

### Recommended Approach

**Option 1: Continuous Implementation (Recommended)**
- Week 1: Tasks 7.1-7.2 (setup, 1.25 hours)
- Week 2: Task 7.3 (developer pipeline, 2.5 hours)
- Week 3: Tasks 7.4-7.5 (user pipeline + wiki, 4.5 hours)
- Week 4: Tasks 7.6-7.9 (validation + testing, 4.5 hours)
- **Total: 4 weeks, 2-3 hours per week**

**Option 2: Sprint Implementation**
- Day 1: Tasks 7.1-7.3 (3.75 hours)
- Day 2: Tasks 7.4-7.6 (6 hours)
- Day 3: Tasks 7.7-7.9 (3 hours)
- **Total: 3 days, ~4 hours per day**

**Option 3: Just-In-Time (When Needed)**
- Implement only when client requests Azure DevOps support
- Fast-follow implementation (2-3 days intensive work)
- Leverage existing patterns and AI assistance

---

## Open Questions

### Technical Questions

1. **Q: Should we use one Azure Static Web App or two?**
   - A: Decision needed - Option 1 (two SWAs) for production, Option 2 (one SWA) for cost savings
   - **Action:** Decide based on client requirements and budget

2. **Q: Should we use Service Principal or Managed Identity for authentication?**
   - A: Managed Identity more secure but requires Azure-hosted agents
   - **Action:** Default to Service Principal, document Managed Identity as enhancement

3. **Q: How to handle Azure DevOps Wiki conflicts with manual edits?**
   - A: Policy decision - "code as source of truth" or bidirectional sync
   - **Action:** Implement one-way sync (repo ’ wiki), document policy

4. **Q: Should we support Azure Artifacts as alternative hosting?**
   - A: Azure Artifacts Universal Packages can host static sites for free
   - **Action:** Document as alternative in setup guide, don't implement by default

### Business Questions

5. **Q: Who pays for Azure resources - project or client?**
   - A: Likely client in production, project for demo/testing
   - **Action:** Clarify in setup guide, provide cost calculator

6. **Q: Is Phase 7 optional or required for project completion?**
   - A: Optional - GitHub implementation is complete and production-ready
   - **Action:** Mark as "optional enhancement" in documentation

7. **Q: Should we support both platforms simultaneously?**
   - A: Possible with current architecture (both plugins coexist)
   - **Action:** Document dual-platform configuration for large migrations

### Timeline Questions

8. **Q: When should Phase 7 be implemented?**
   - A: Just-in-time when client needs Azure DevOps, or proactively for portfolio completeness
   - **Action:** Defer to after GitHub implementation feedback

9. **Q: Can Phase 7 be split into smaller releases?**
   - A: Yes - could release pipelines incrementally (developer ’ user ’ wiki)
   - **Action:** Support phased rollout if needed

---

## Appendix

### A. Cost Analysis

#### Azure Free Tier Resources

| Resource | Free Tier | Overage Cost | Monthly Estimate |
|----------|-----------|--------------|------------------|
| Azure Static Web Apps (x2) | 100 GB bandwidth each | $0.20/GB | $0 (under limit) |
| Azure DevOps Pipelines | 1 parallel job, 1800 min/month | $40/job/month | $0 (under limit) |
| Azure DevOps Wiki | Unlimited | N/A | $0 |
| Azure Resource Group | Unlimited | N/A | $0 |
| Service Principal | Unlimited | N/A | $0 |
| **Total** | | | **$0/month** |

#### Cost Optimization Strategies

1. **Use Free Tier Aggressively**
   - 100 GB bandwidth sufficient for most documentation sites
   - 1800 minutes sufficient for ~30 builds/month at 5 min/build

2. **Monitor Usage**
   - Set up cost alerts at $10, $50, $100 thresholds
   - Review Azure Cost Management monthly
   - Track bandwidth usage in Azure Portal

3. **Optimize Build Frequency**
   - Use path filters to avoid unnecessary builds
   - Cache dependencies to speed up builds
   - Run PR validation only on docs changes

4. **Alternative Hosting**
   - Azure Artifacts Universal Packages (free, 2 GB limit)
   - Azure Blob Storage Static Website ($0.02/GB/month)
   - Keep GitHub Pages as fallback

### B. Feature Comparison Matrix

| Feature | GitHub | Azure DevOps | Notes |
|---------|--------|--------------|-------|
| Developer Docs Hosting | GitHub Pages | Azure Static Web Apps | Both free tier |
| User Docs Hosting | GitHub Pages | Azure Static Web Apps | Both free tier |
| Wiki Hosting | GitHub Wiki | Azure DevOps Wiki | Both included |
| CI/CD Platform | GitHub Actions | Azure Pipelines | Both have free tiers |
| Custom Domain | Yes (free) | Yes (free) | Both support |
| HTTPS | Yes (automatic) | Yes (automatic) | Both default |
| Authentication | GitHub SSO | Azure AD | Azure AD more enterprise |
| Audit Logging | Basic | Advanced | Azure more comprehensive |
| Compliance | Basic | SOC 2, ISO 27001 | Azure better for enterprise |
| Cost | $0/month | $0/month (free tier) | Both can scale up |

### C. Reference Implementation

See existing files for implementation patterns:
- `.github/workflows/docs-developer-deploy.yml` - Pattern for Azure equivalent
- `.github/workflows/docs-wiki-sync.yml` - Wiki sync pattern
- `.docgen/wiki-sync.ps1` - PowerShell wiki sync logic
- `docs/architecture/github-pages-setup-guide.md` - Setup guide template
- `docs/architecture/platform-migration-guide.md` - Migration procedures

### D. Related Documentation

**Internal Documentation:**
- `docs/architecture/ai-docs-platform-agnostic-architecture.md` - Platform-agnostic design
- `docs/architecture/azure-devops-plugin-guide.md` - Azure DevOps plugin design (Phase 1)
- `docs/architecture/platform-migration-guide.md` - Platform migration procedures
- `dev/active/004-ai-docs/ai-docs-tasks.md` - Phase 7 task checklist

**External Documentation:**
- [Azure Static Web Apps Docs](https://learn.microsoft.com/en-us/azure/static-web-apps/)
- [Azure DevOps Pipelines Docs](https://learn.microsoft.com/en-us/azure/devops/pipelines/)
- [Azure DevOps Wiki Docs](https://learn.microsoft.com/en-us/azure/devops/project/wiki/)
- [Azure CLI DevOps Extension](https://learn.microsoft.com/en-us/cli/azure/devops)

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-11-03 | Documentation Team | Initial PRD creation for Phase 7 |

---

**Status:** Ready for Implementation
**Next Steps:** Decide on implementation timeline (continuous, sprint, or just-in-time)
**Approvers:** Project Lead, Technical Architect

---

**End of PRD**
