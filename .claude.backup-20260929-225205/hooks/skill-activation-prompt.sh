#!/bin/bash
# skill-activation-prompt hook wrapper
#
# Copyright (c) 2025 Claude Code Infrastructure Contributors
# Licensed under the MIT License
# Source: https://github.com/yourusername/claude-code-infrastructure-showcase

set -e

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# Change to the hooks directory
cd "$SCRIPT_DIR"

# Check if node/npx is available
if ! command -v npx >/dev/null 2>&1; then
    echo "ERROR: npx not found. Install Node.js to use skill-activation hooks." >&2
    exit 1
fi

# Check if dependencies are installed
if [ ! -d "node_modules" ]; then
    echo "ERROR: Hook dependencies not installed. Run: cd .claude/hooks && npm install" >&2
    exit 1
fi

# Run the TypeScript hook
cat | npx tsx skill-activation-prompt.ts
