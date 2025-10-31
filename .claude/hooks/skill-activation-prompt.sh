#!/bin/bash
# skill-activation-prompt hook wrapper
#
# Copyright (c) 2025 Claude Code Infrastructure Contributors
# Licensed under the MIT License
# Source: https://github.com/yourusername/claude-code-infrastructure-showcase

set -e

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Change to the hooks directory and run the TypeScript hook
cd "$SCRIPT_DIR"
cat | npx tsx skill-activation-prompt.ts
