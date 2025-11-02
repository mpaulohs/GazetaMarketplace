# Security Policy

## Supported Versions

This project is a demonstration/example .NET 10 project. Security updates are provided for the following versions:

| Version | Supported          | Notes |
| ------- | ------------------ | ----- |
| .NET 10.0 RC 2+  | :white_check_mark: | Current |
| .NET 10.0 RC 1   | :x:                | Upgrade to RC 2+ |
| < .NET 10.0      | :x:                | Not supported |

## Reporting a Vulnerability

### How to Report

If you discover a security vulnerability in this project, please report it responsibly:

1. **DO NOT** open a public GitHub issue
2. **DO NOT** disclose the vulnerability publicly until it has been addressed

**Preferred Method:**

Use GitHub's private vulnerability reporting:
- Navigate to the Security tab
- Click "Report a vulnerability"
- Fill out the advisory form with details

**Alternative Method:**

Email the maintainer directly at: bobby@theynotme.com

### What to Include

Please provide the following information:

- **Description** of the vulnerability
- **Steps to reproduce** the issue
- **Potential impact** (what an attacker could do)
- **Affected versions** (if known)
- **Suggested fix** (if you have one)
- **Your contact information** (for follow-up)

### Response Timeline

- **Initial Response:** Within 48 hours
- **Status Update:** Within 7 days
- **Fix Target:** Within 30 days for critical issues

We will:
1. Confirm receipt of your report
2. Investigate and verify the vulnerability
3. Develop and test a fix
4. Release a security patch
5. Credit you for the discovery (if desired)

## Security Best Practices

### For Contributors

When contributing to this project:

#### 1. Never Commit Secrets

**NEVER commit:**
- API keys, tokens, passwords
- Private keys, certificates
- Database connection strings with credentials
- OAuth secrets
- Environment-specific configuration with sensitive data

**Use instead:**
- User secrets (`dotnet user-secrets`)
- Environment variables
- Azure Key Vault / AWS Secrets Manager
- .gitignore for local config files

#### 2. Validate User Input

Always validate and sanitize user input:

```csharp
// GOOD - validate and sanitize
public IActionResult UpdateUser(string username)
{
    if (string.IsNullOrWhiteSpace(username) || username.Length > 50)
        return BadRequest("Invalid username");

    // Sanitize before use
    username = username.Trim();

    // ...
}

// BAD - direct use of user input
public IActionResult UpdateUser(string username)
{
    _db.Execute($"UPDATE Users SET Name = '{username}'"); // SQL injection!
}
```

#### 3. Use Parameterized Queries

Prevent SQL injection:

```csharp
// GOOD - parameterized query
var user = _db.Query<User>(
    "SELECT * FROM Users WHERE Username = @username",
    new { username }
).FirstOrDefault();

// BAD - string concatenation
var user = _db.Query<User>(
    $"SELECT * FROM Users WHERE Username = '{username}'" // SQL injection!
).FirstOrDefault();
```

#### 4. Secure File Operations

Use safe path operations:

```csharp
// GOOD - use Path.Combine and validate
var basePath = Path.GetFullPath("uploads");
var userPath = Path.GetFullPath(Path.Combine(basePath, userFileName));

if (!userPath.StartsWith(basePath))
    return BadRequest("Invalid file path"); // Path traversal attempt

// BAD - string concatenation
var filePath = "uploads/" + userFileName; // Path traversal vulnerability!
```

#### 5. Avoid Unsafe Deserialization

```csharp
// GOOD - use safe serializers
var user = JsonSerializer.Deserialize<User>(json);

// BAD - unsafe deserializer
var formatter = new BinaryFormatter(); // Known security vulnerability!
var user = (User)formatter.Deserialize(stream);
```

### Automated Security Scanning

This project includes automated security scanning in the PR validation pipeline:

#### GitLeaks
- Scans for exposed secrets in code
- Checks commit history
- Blocks PRs with potential leaks

#### .NET Security Analyzers
- Roslyn security analyzers
- Detects common vulnerabilities
- Enforced on build

#### NuGet Vulnerability Scanning
- Checks for vulnerable package versions
- Scans both direct and transitive dependencies
- Reports CVEs with severity levels

#### Path Security Scanner
- Detects unsafe file operations
- Finds path traversal vulnerabilities
- Identifies command injection risks

### Dependency Management

**Keep dependencies updated:**

```bash
# Check for vulnerable packages
dotnet list package --vulnerable

# Update packages in Directory.Packages.props
# Review release notes for breaking changes
```

**Subscribe to security advisories:**
- [.NET Security Advisories](https://github.com/dotnet/announcements/labels/security)
- [NuGet Security Advisories](https://github.com/NuGet/Announcements)

## Security Features

### Authentication & Authorization

This is a demonstration project. If implementing authentication:

- Use **ASP.NET Core Identity** for user management
- Implement **multi-factor authentication** (MFA)
- Use **OAuth 2.0/OpenID Connect** for third-party auth
- Store passwords with **strong hashing** (PBKDF2, bcrypt, Argon2)
- Implement **rate limiting** on auth endpoints

### HTTPS

Always use HTTPS in production:

```csharp
// Program.cs - enforce HTTPS
app.UseHttpsRedirection();
app.UseHsts(); // HTTP Strict Transport Security
```

### Content Security Policy (CSP)

Implement CSP headers:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';");
    await next();
});
```

### CORS

Configure CORS properly:

```csharp
// DO NOT use in production:
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", // BAD for production!
        builder => builder.AllowAnyOrigin());
});

// GOOD - specific origins:
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowTrusted",
        builder => builder.WithOrigins("https://trusted-domain.com"));
});
```

## Common Vulnerabilities

### OWASP Top 10 (.NET Context)

1. **Broken Access Control**
   - Use `[Authorize]` attributes
   - Implement role/claims-based authorization
   - Verify resource ownership

2. **Cryptographic Failures**
   - Never roll your own crypto
   - Use .NET cryptography APIs
   - Use TLS 1.2+ for transport

3. **Injection**
   - Use parameterized queries (EF Core, Dapper)
   - Validate and sanitize input
   - Use allowlists, not denylists

4. **Insecure Design**
   - Implement defense in depth
   - Follow principle of least privilege
   - Use secure defaults

5. **Security Misconfiguration**
   - Disable debug mode in production
   - Remove default credentials
   - Keep frameworks updated

6. **Vulnerable Components**
   - Regularly update NuGet packages
   - Monitor security advisories
   - Remove unused dependencies

7. **Authentication Failures**
   - Implement MFA
   - Use secure session management
   - Implement account lockout

8. **Software and Data Integrity**
   - Verify package integrity
   - Use code signing
   - Implement CI/CD security checks

9. **Logging Failures**
   - Log security events
   - Don't log sensitive data
   - Monitor logs for anomalies

10. **Server-Side Request Forgery (SSRF)**
    - Validate URLs
    - Use allowlists for external requests
    - Implement network segmentation

## Security Checklist

Before deploying to production:

- [ ] All dependencies are up to date
- [ ] No vulnerable packages (`dotnet list package --vulnerable`)
- [ ] Secrets are not in source control
- [ ] HTTPS is enforced
- [ ] Authentication is implemented
- [ ] Authorization is implemented
- [ ] Input validation is comprehensive
- [ ] SQL queries are parameterized
- [ ] File operations are secure
- [ ] Logging is configured (no sensitive data)
- [ ] Error handling doesn't expose internals
- [ ] Security headers are configured (CSP, HSTS, etc.)
- [ ] CORS is properly configured
- [ ] Rate limiting is implemented
- [ ] Security scanning is passing

## Resources

- [ASP.NET Core Security Best Practices](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [.NET Security Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/security/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [OWASP .NET Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/DotNet_Security_Cheat_Sheet.html)

---

**Security is everyone's responsibility. Report issues responsibly.**
