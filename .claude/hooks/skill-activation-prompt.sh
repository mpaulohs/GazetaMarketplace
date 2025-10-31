#!/bin/bash
# skill-activation-prompt hook wrapper
#
# Copyright (c) 2025 Claude Code Infrastructure Contributors
# Licensed under the MIT License
# Source: https://github.com/yourusername/claude-code-infrastructure-showcase

set -e

cd "$CLAUDE_PROJECT_DIR/.claude/hooks"
cat | npx tsx skill-activation-prompt.ts
