#!/bin/bash
# skill-activation-prompt hook wrapper
#
# Copyright (c) 2025 Claude Code Infrastructure Contributors
# Licensed under the MIT License
# Source: https://github.com/yourusername/claude-code-infrastructure-showcase

# Fail silently if anything goes wrong - don't block the user
set +e

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "$0")" 2>/dev/null && pwd)"
if [ -z "$SCRIPT_DIR" ]; then
    exit 0
fi

# Change to the hooks directory
cd "$SCRIPT_DIR" 2>/dev/null || exit 0

# Check if node/npx is available
command -v npx >/dev/null 2>&1 || exit 0

# Check if tsx is available
npx tsx --version >/dev/null 2>&1 || exit 0

# Run the TypeScript hook
cat | npx tsx skill-activation-prompt.ts 2>/dev/null || exit 0
