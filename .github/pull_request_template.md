# Pull Request

## Description

<!-- Provide a clear and concise description of your changes -->

## Type of Change

<!-- Mark the relevant option(s) with an 'x' -->

- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update
- [ ] Refactoring (no functional changes)
- [ ] Performance improvement
- [ ] Test update
- [ ] Build/CI configuration change

## Testing

<!-- Describe the tests you ran to verify your changes -->

**Test Configuration:**
- .NET SDK version: <!-- e.g., 10.0.100-rc.2.25502.107 -->
- OS: <!-- e.g., Windows 11, Ubuntu 22.04, macOS 14 -->
- IDE: <!-- e.g., Visual Studio 2022, VS Code, Rider -->

**Tests Performed:**
<!-- List the tests you performed -->
- [ ] Unit tests pass (`dotnet test`)
- [ ] Playwright E2E tests pass (if applicable)
- [ ] Manual testing completed
- [ ] Tested on multiple platforms (if applicable)

## .NET Project Checklist

<!-- Ensure all applicable items are checked -->

- [ ] All tests pass locally (`dotnet test`)
- [ ] Build succeeds without warnings (`dotnet build`)
- [ ] Code formatting applied (`dotnet format`)
- [ ] No `ImplicitUsings` violations (explicit using statements where required)
- [ ] Package references use Centralized Package Management (no Version attributes in .csproj)
- [ ] New packages added to `Directory.Packages.props` (if applicable)
- [ ] Test projects properly configured (EnableMSTestRunner, OutputType=Exe if using MSTest)
- [ ] No breaking changes to global.json (SDK version, test runner config)
- [ ] Code follows project conventions (see CLAUDE.md)

## Documentation

<!-- Check applicable documentation updates -->

- [ ] Code comments added/updated for complex logic
- [ ] README.md updated (if applicable)
- [ ] CLAUDE.md updated (if adding Claude Code guidance)
- [ ] API documentation updated (if applicable)
- [ ] CHANGELOG.md updated (if applicable)

## Related Issues

<!-- Link related issues using keywords: Fixes #123, Closes #456, Relates to #789 -->

Fixes #
Relates to #

## Additional Notes

<!-- Add any additional context, screenshots, or information here -->

## PR Size

<!-- PRs should generally be <2000 lines of changes. If larger, consider splitting into multiple PRs -->

Estimated lines changed: <!-- Approximate total of additions + deletions -->

---

**By submitting this PR, I confirm that:**
- [ ] I have reviewed my own code
- [ ] My code follows the project's code style and conventions
- [ ] I have tested my changes thoroughly
- [ ] This PR is ready for review
