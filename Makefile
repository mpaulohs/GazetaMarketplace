# Platform-Agnostic Documentation Makefile
# Works on Linux, macOS, and Windows (via WSL2 or GNU Make)

.PHONY: help docs-build docs-serve docs-clean diagrams validate deploy

# Detect platform
UNAME_S := $(shell uname -s 2>/dev/null || echo "Windows")
ifeq ($(UNAME_S),Linux)
    PLATFORM := linux
endif
ifeq ($(UNAME_S),Darwin)
    PLATFORM := macos
endif
ifeq ($(findstring MINGW,$(UNAME_S)),MINGW)
    PLATFORM := windows
endif
ifeq ($(findstring MSYS,$(UNAME_S)),MSYS)
    PLATFORM := windows
endif

# Tool commands
DOCFX := docfx
PWSH := pwsh
DOTNET := dotnet
PYTHON := python3

# Default target
.DEFAULT_GOAL := help

help:
	@echo ""
	@echo "AI-Assisted Documentation System - Make Targets"
	@echo "================================================"
	@echo ""
	@echo "Building:"
	@echo "  docs-build           Build all documentation (developer + user)"
	@echo "  docs-developer       Build developer documentation only"
	@echo "  docs-user            Build user documentation only"
	@echo ""
	@echo "Serving (Local Development):"
	@echo "  docs-serve           Serve developer documentation at http://localhost:8080"
	@echo "  docs-user-serve      Serve user documentation at http://localhost:8081"
	@echo ""
	@echo "Diagrams:"
	@echo "  diagrams             Generate all diagrams from assemblies"
	@echo "  diagrams-mermaid     Generate Mermaid diagrams only"
	@echo "  diagrams-plantuml    Generate PlantUML diagrams only"
	@echo ""
	@echo "Quality:"
	@echo "  validate             Validate documentation quality"
	@echo "  lint                 Lint markdown files"
	@echo ""
	@echo "Cleanup:"
	@echo "  docs-clean           Clean generated documentation"
	@echo "  clean-all            Clean all generated files (docs + diagrams)"
	@echo ""
	@echo "Deployment:"
	@echo "  deploy               Deploy documentation (CI/CD only)"
	@echo ""
	@echo "Platform: $(PLATFORM)"
	@echo ""

# Build targets
docs-build: docs-developer docs-user

docs-developer:
	@echo "Building developer documentation..."
	@if [ ! -d "docs/docfx-developer" ]; then \
		echo "✗ docs/docfx-developer not found. Run Phase 3 first."; \
		exit 1; \
	fi
	@cd docs/docfx-developer && $(DOCFX) build

docs-user:
	@echo "Building user documentation..."
	@if [ ! -d "docs/docfx-user" ]; then \
		echo "✗ docs/docfx-user not found. Run Phase 4 first."; \
		exit 1; \
	fi
	@cd docs/docfx-user && $(DOCFX) build

# Serve targets
docs-serve:
	@echo "Serving developer documentation at http://localhost:8080"
	@echo "Press Ctrl+C to stop"
	@if [ ! -d "docs/docfx-developer/_site" ]; then \
		echo "✗ Documentation not built. Run 'make docs-developer' first."; \
		exit 1; \
	fi
	@cd docs/docfx-developer && $(DOCFX) serve _site

docs-user-serve:
	@echo "Serving user documentation at http://localhost:8081"
	@echo "Press Ctrl+C to stop"
	@if [ ! -d "docs/docfx-user/_site" ]; then \
		echo "✗ Documentation not built. Run 'make docs-user' first."; \
		exit 1; \
	fi
	@cd docs/docfx-user && $(DOCFX) serve _site --port 8081

# Diagram targets
diagrams:
	@echo "Generating diagrams from assemblies..."
	@$(PWSH) .docgen/diagram-gen.ps1 -All

diagrams-mermaid:
	@echo "Generating Mermaid diagrams only..."
	@$(PWSH) .docgen/diagram-gen.ps1 -Mermaid

diagrams-plantuml:
	@echo "Generating PlantUML diagrams only..."
	@$(PWSH) .docgen/diagram-gen.ps1 -PlantUML

# Validation targets
validate:
	@echo "Validating documentation..."
	@$(PWSH) .docgen/validate-docs.ps1

lint:
	@echo "Linting markdown files..."
	@if command -v markdownlint >/dev/null 2>&1; then \
		markdownlint docs/**/*.md --ignore docs/**/node_modules --ignore docs/**/_site; \
	else \
		echo "⚠ markdownlint not installed. Install with: npm install -g markdownlint-cli"; \
	fi

# Cleanup targets
docs-clean:
	@echo "Cleaning documentation..."
	@rm -rf docs/docfx-developer/_site
	@rm -rf docs/docfx-developer/api
	@rm -rf docs/docfx-developer/obj
	@rm -rf docs/docfx-user/_site
	@rm -rf docs/docfx-user/api
	@rm -rf docs/docfx-user/obj
	@echo "✓ Documentation cleaned"

clean-all: docs-clean
	@echo "Cleaning diagrams..."
	@rm -rf docs/docfx-developer/diagrams/*.md
	@rm -rf docs/docfx-developer/diagrams/*.puml
	@echo "✓ All generated files cleaned"

# Deployment target (CI/CD only)
deploy:
	@echo "Deploying documentation..."
	@if [ "$$GITHUB_ACTIONS" = "true" ]; then \
		echo "Detected GitHub Actions"; \
		if [ -f ".github/scripts/deploy.ps1" ]; then \
			$(PWSH) .github/scripts/deploy.ps1; \
		else \
			echo "ℹ No GitHub deploy script found"; \
		fi; \
	elif [ "$$TF_BUILD" = "True" ]; then \
		echo "Detected Azure DevOps"; \
		if [ -f ".azuredevops/scripts/deploy.ps1" ]; then \
			$(PWSH) .azuredevops/scripts/deploy.ps1; \
		else \
			echo "ℹ No Azure DevOps deploy script found"; \
		fi; \
	else \
		echo "⚠ Not running in CI/CD environment"; \
		echo "  Set GITHUB_ACTIONS=true or TF_BUILD=True to simulate"; \
	fi

# Installation helpers (not in help menu)
.PHONY: install-tools
install-tools:
	@echo "Installing documentation tools..."
	@$(DOTNET) tool install -g docfx
	@$(DOTNET) tool install -g dll2mmd
	@$(DOTNET) tool install -g PlantUmlClassDiagramGenerator
	@echo "✓ Tools installed"

.PHONY: check-tools
check-tools:
	@echo "Checking required tools..."
	@command -v $(DOTNET) >/dev/null 2>&1 || { echo "✗ dotnet not found"; exit 1; }
	@command -v $(PWSH) >/dev/null 2>&1 || { echo "✗ pwsh not found"; exit 1; }
	@command -v $(DOCFX) >/dev/null 2>&1 || { echo "✗ docfx not found. Run: make install-tools"; exit 1; }
	@echo "✓ All required tools found"
