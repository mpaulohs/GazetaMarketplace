# Troubleshooting Claude Code Infrastructure

## Hook Errors

If you're seeing "UserPromptSubmit hook error" or "PostToolUse hook error" messages, try these solutions:

### Quick Fix: Temporarily Disable Hooks

If hooks are causing issues, you can temporarily disable them:

```bash
# Backup current settings
cp .claude/settings.json .claude/settings.json.backup

# Use settings without hooks
cp .claude/settings.json.no-hooks .claude/settings.json

# Restart Claude Code
```

To re-enable hooks later:

```bash
cp .claude/settings.json.backup .claude/settings.json
```

### Solution 1: Install Missing Dependencies

The hooks require these dependencies:

1. **Node.js and npm** - Required for skill-activation-prompt hook
   ```bash
   node --version  # Should be v14 or higher
   npm --version
   ```

2. **jq** - Required for post-tool-use-tracker hook
   ```bash
   jq --version
   ```

   On Windows, install jq:
   ```bash
   # Using Chocolatey
   choco install jq

   # Or download from https://jqlang.github.io/jq/download/
   ```

3. **Install hook dependencies**
   ```bash
   cd .claude/hooks
   npm install
   ```

### Solution 2: Check Hook Scripts

Verify hook scripts are executable:

```bash
ls -la .claude/hooks/*.sh
```

### Solution 3: Test Hooks Manually

Test the skill-activation-prompt hook:

```bash
cd .claude/hooks
echo '{"session_id":"test","prompt":"test"}' | bash skill-activation-prompt.sh
```

Expected: Should either show skill suggestions or complete without errors.

Test the post-tool-use-tracker hook:

```bash
cd .claude/hooks
echo '{"tool_name":"Edit","tool_input":{"file_path":"test.cs"},"session_id":"test"}' | bash post-tool-use-tracker.sh
```

Expected: Should complete without errors.

### Solution 4: Check Error Details

If hooks are failing, Claude Code may provide more details in its error messages. Look for:
- "command not found" → Missing dependencies (node, npm, jq)
- "No such file or directory" → Path issues
- "Permission denied" → Need to mark scripts as executable

### Common Issues

**Issue**: "npx: command not found"
**Solution**: Install Node.js and npm

**Issue**: "jq: command not found"
**Solution**: Install jq (see Solution 1 above)

**Issue**: Hook errors on Windows
**Solution**: Ensure you're using Git Bash or WSL, not Command Prompt or PowerShell

**Issue**: Hooks work in terminal but not in Claude Code
**Solution**: Claude Code may use a different shell environment. Try disabling hooks temporarily.

## Skill Activation Not Working

If skills aren't auto-activating:

1. **Check skill-rules.json exists**
   ```bash
   cat .claude/skills/skill-rules.json
   ```

2. **Verify prompt triggers match**
   - Look at keywords and intentPatterns in skill-rules.json
   - Use those exact phrases in your prompts

3. **Test hook manually** (see Solution 3 above)

## Need Help?

If you're still having issues:

1. **Disable hooks** using the Quick Fix above - this allows you to use Claude Code without interruption
2. **Check logs** in `.claude/hooks/` for any generated log files
3. **Review commit history** - Recent commits document hook fixes and changes

## Removing Infrastructure

To completely remove Claude Code infrastructure:

```bash
# Remove hooks from settings
cp .claude/settings.json.no-hooks .claude/settings.json

# Or remove the entire .claude directory
rm -rf .claude

# Remove dev docs
rm -rf dev
```

**Note**: The infrastructure is optional. The project works fine without it - you'll just lose auto-skill-activation and dev docs features.
