# Third-Party Attribution

## Claude Code Infrastructure

The Claude Code infrastructure in this directory (`.claude/`) is based on code from the [claude-code-infrastructure-showcase](https://github.com/yourusername/claude-code-infrastructure-showcase) repository.

### Components Used

The following components were copied and/or adapted from the showcase repository:

- **Hooks** (`.claude/hooks/`)
  - `skill-activation-prompt.ts` - Auto-activation hook for skills
  - `skill-activation-prompt.sh` - Shell wrapper for TypeScript hook
  - `post-tool-use-tracker.sh` - Post-tool-use tracking hook
  - `package.json` - Node.js dependencies for hooks

- **Skills** (`.claude/skills/`)
  - `skill-developer/` - Meta-skill for creating and managing skills (complete directory with all resource files)
  - `skill-rules.json` - Skill trigger configuration structure

- **Slash Commands** (`.claude/commands/`)
  - `dev-docs.md` - Command for creating dev docs
  - `dev-docs-update.md` - Command for updating dev docs

- **Dev Docs System** (`dev/`)
  - Directory structure pattern
  - README.md documentation

### License

These components are released under the MIT License:

```
MIT License

Copyright (c) 2025 Claude Code Infrastructure Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

### Modifications

The components have been integrated into this .NET 10 project with the following adaptations:
- skill-rules.json customized for .NET 10 project context
- Documentation updated to reference .NET-specific examples
- No modifications to core hook logic or skill-developer functionality

### Source Repository

Original source: `C:\Users\bobby\src\claude\claude-code-infrastructure-showcase`

For more information about the Claude Code Infrastructure Showcase, see the original repository's documentation.
