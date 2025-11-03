# System Access Guide

This guide explains how to get access to the .NET 10 Project Example system and set up your local development environment.

## Repository

**GitHub Repository:** https://github.com/NotMyself/net10-project-example

## Environments

The project currently supports local development. Deployed instances are configured through GitHub Actions.

| Environment | Type | URL | Purpose |
|-----------|------|-----|---------|
| Local Development | Local Machine | `http://localhost:5000` (Web) `https://localhost:5001` (API) | Development and testing on your machine |
| GitHub Actions CI/CD | Automated | N/A | Build, test, and validation on pull requests |
| Docker Images | Container | `ghcr.io/{owner}/{repo}/example-web:main` | Containerized application deployment |

## Getting Access

### For Developers

Repository access is managed through GitHub. To contribute to this project:

1. **Request GitHub Access**
   - Contact the repository owner (NotMyself) to be added as a collaborator
   - Alternatively, fork the repository on GitHub

2. **Clone the Repository**
   ```bash
   git clone https://github.com/NotMyself/net10-project-example.git
   cd net10-project-example
   ```

3. **Verify Git Configuration**
   ```bash
   # Set your name and email (if not already configured)
   git config user.name "Your Name"
   git config user.email "your.email@example.com"
   ```

### For End Users

The project is not currently deployed to production. Binaries and Docker images are available from:

- **NuGet.org** - Packages published on tagged releases (if `NUGET_API_KEY` configured)
- **GitHub Packages** - Available for authenticated users with package read access
- **Docker Images** - Published to GitHub Container Registry (GHCR) on main branch pushes

## For Developers: Local Setup

### Prerequisites

Install the following on your development machine:

- **.NET 10.0 SDK RC 2** (version 10.0.100-rc.2.25502.107 or later)
  - Download: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- **Git** (for cloning the repository)
- **PowerShell 7** (for Playwright browser installation)
- **IDE** (Visual Studio 2022, VS Code, or JetBrains Rider)

### Step 1: Clone Repository

```bash
git clone https://github.com/NotMyself/net10-project-example.git
cd net10-project-example
```

### Step 2: Verify .NET Installation

```bash
# Check .NET SDK version
dotnet --version

# Expected output: 10.0.100-rc.2.25502.107 or later
```

### Step 3: Build the Solution

```bash
# Restore packages and build all projects
dotnet build
```

### Step 4: Run Applications

**MVC Web Application:**
```bash
dotnet run --project src/Example.Web/Example.Web.csproj
```
Access at: `http://localhost:5000`

**Minimal API Application:**
```bash
dotnet run --project src/Example.API/Example.API.csproj
```
Access at: `https://localhost:5001`
Swagger UI: `https://localhost:5001/swagger`

### Step 5: Run Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet run --project tests/Example.Web.Tests/Example.Web.Tests.csproj
dotnet run --project tests/Example.API.Tests/Example.API.Tests.csproj
```

### Step 6: Setup Playwright (First Time Only)

After building the Playwright test projects, install browser engines:

```powershell
# For MVC tests
pwsh -Command "cd tests/Example.Web.Tests.Playwright/bin/Debug/net10.0; ./playwright.ps1 install"

# For API tests
pwsh -Command "cd tests/Example.API.Tests.Playwright/bin/Debug/net10.0; ./playwright.ps1 install"
```

Then run the Playwright end-to-end tests:

```bash
dotnet run --project tests/Example.Web.Tests.Playwright/Example.Web.Tests.Playwright.csproj
dotnet run --project tests/Example.API.Tests.Playwright/Example.API.Tests.Playwright.csproj
```

## For End Users

### Running Docker Containers

Pull and run containerized applications from GitHub Container Registry:

```bash
# Pull the MVC application image
docker pull ghcr.io/notmyself/net10-project-example/example-web:main

# Run the MVC application
docker run -p 8080:8080 ghcr.io/notmyself/net10-project-example/example-web:main

# Pull the API application image
docker pull ghcr.io/notmyself/net10-project-example/example-api:main

# Run the API application
docker run -p 8080:8080 ghcr.io/notmyself/net10-project-example/example-api:main
```

Access the applications at:
- Web: `http://localhost:8080`
- API: `http://localhost:8080` (Swagger at `/swagger`)

### Using Published Packages

NuGet packages are published on each tagged release:

```bash
# Search NuGet.org for packages (when available)
dotnet package search Example
```

## Authentication

### GitHub Authentication

Repository access uses standard GitHub authentication:

1. **HTTPS (Recommended)**
   - Requires GitHub username and personal access token
   - Create token: https://github.com/settings/tokens
   - Configure Git credentials via credential manager

2. **SSH (Alternative)**
   - Generate SSH key pair
   - Add public key to GitHub: https://github.com/settings/keys
   - Configure Git to use SSH

### Docker Registry Authentication

To pull private Docker images:

```bash
# Login to GitHub Container Registry
echo ${{ secrets.GITHUB_TOKEN }} | docker login ghcr.io -u username --password-stdin

# Or use personal access token with 'read:packages' scope
echo YOUR_PAT | docker login ghcr.io -u your-username --password-stdin
```

## Permissions

Role-based access controls are configured in the GitHub repository:

| Role | Repository | Tests | Deployments | PR Review | Publishing |
|------|-----------|-------|------------|-----------|-----------|
| **Owner** | Admin | Full | Full | Approve/Request | Full |
| **Developer** | Write | Full | Via PR | Request | None |
| **Contributor** | Read | Full | None | None | None |
| **Public** | Read | Full | None | None | None |

### Managing Access

Repository owners can manage roles through GitHub:

1. Go to repository Settings
2. Navigate to Collaborators and Teams
3. Add users and assign roles
4. Configure branch protection rules for main

## Troubleshooting

### Build Fails with SDK Version Error

**Error:** `.NET SDK version 10.0.100-rc.2.25502.107 not found`

**Solution:** Install the correct .NET 10.0 RC 2 SDK version
```bash
# Check your installed SDKs
dotnet --list-sdks

# Download and install from: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
```

### Playwright Browser Installation Fails

**Error:** `playwright.ps1 install` fails or browsers not found

**Solution:** Ensure PowerShell 7 is installed and properly configured
```bash
# Verify PowerShell version
pwsh --version

# If not installed: https://github.com/PowerShell/PowerShell

# Manually install browsers
pwsh -Command "Install-PlaywrightBrowsers"
```

### Port Already in Use

**Error:** `Address already in use` when running applications

**Solution:** Use different ports or find the process using the port
```bash
# Linux/macOS: Find process using port 5000
lsof -i :5000

# Kill the process
kill -9 <PID>

# Windows: Use netstat
netstat -ano | findstr :5000
```

### Clone Fails with HTTPS

**Error:** `fatal: could not read Username for 'github.com'`

**Solution:** Configure Git credentials or use SSH
```bash
# Configure credentials permanently (HTTPS)
git config --global credential.helper store

# Or switch to SSH
git remote set-url origin git@github.com:NotMyself/net10-project-example.git
```

### Docker Pull Fails with Authentication

**Error:** `Error response from daemon: unauthorized`

**Solution:** Authenticate with GitHub Container Registry
```bash
# Create personal access token with 'read:packages' scope
# Then login
docker login ghcr.io

# When prompted:
# Username: your-github-username
# Password: your-personal-access-token
```

## Additional Resources

- **Getting Started:** See [README.md](../../README.md)
- **Development Guide:** See [CLAUDE.md](../../CLAUDE.md)
- **Contributing:** See [CONTRIBUTING.md](../../CONTRIBUTING.md)
- **CI/CD Pipelines:** See [.github/workflows/README.md](../../.github/workflows/README.md)
- **Security Policy:** See [SECURITY.md](../../SECURITY.md)

## Support

For access issues or questions:

1. Check the [Troubleshooting](#troubleshooting) section
2. Review project documentation in `/docs`
3. Check GitHub Issues for similar problems
4. Contact the repository owner or team lead
