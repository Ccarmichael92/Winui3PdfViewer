# Security Policy

## Supported Versions

We release patches for security vulnerabilities for the following versions:

| Version | Supported          |
| ------- | ------------------ |
| Latest  | :white_check_mark: |
| < Latest| :x:                |

## Reporting a Vulnerability

We take the security of WinUI 3 PDF Viewer seriously. If you believe you have found a security vulnerability, please report it to us as described below.

### Please Do Not

- Open a public GitHub issue for security vulnerabilities
- Disclose the vulnerability publicly before we have had a chance to address it

### Please Do

**Report security vulnerabilities by emailing:** (Add your security contact email here)

Or create a private security advisory on GitHub:
https://github.com/Ccarmichael92/Winui3PdfViewer/security/advisories/new

### What to Include

Please include the following information in your report:

1. **Type of issue** (e.g., buffer overflow, SQL injection, cross-site scripting, etc.)
2. **Full paths of source file(s)** related to the issue
3. **Location of the affected source code** (tag/branch/commit or direct URL)
4. **Step-by-step instructions** to reproduce the issue
5. **Proof-of-concept or exploit code** (if possible)
6. **Impact of the issue**, including how an attacker might exploit it

### What to Expect

- **Acknowledgment** within 48 hours of your report
- **Regular updates** on our progress
- **Credit** in the release notes when the vulnerability is fixed (if desired)
- We'll work with you to understand and validate the issue

## Security Best Practices for Users

### File Validation

Always validate files from untrusted sources before loading them:

```csharp
// Check file size
var properties = await file.GetBasicPropertiesAsync();
if (properties.Size > MAX_FILE_SIZE)
{
    throw new InvalidOperationException("File too large");
}

// Verify file extension
var extension = Path.GetExtension(file.Name).ToLowerInvariant();
if (extension != ".pdf" && extension != ".tif" && extension != ".tiff")
{
    throw new InvalidOperationException("Invalid file type");
}
```

### Temporary File Cleanup

The control automatically cleans up temporary files, but ensure proper disposal:

```csharp
try
{
    await pdfViewerControl.LoadFileAsync(file);
}
finally
{
    pdfViewerControl.Dispose(); // Ensures temp files are cleaned up
}
```

### Memory Management

For large documents or memory-constrained environments:

```csharp
pdfViewerControl.UseTempFiles = true;  // Use disk instead of memory
pdfViewerControl.PreloadedPages = 2;   // Reduce preloaded pages
```

### DPI Validation

The library validates DPI values, but you can add additional checks:

```csharp
const int MAX_SAFE_DPI = 600;
if (userRequestedDpi > MAX_SAFE_DPI)
{
    userRequestedDpi = MAX_SAFE_DPI;
}
pdfViewerControl.Dpi = userRequestedDpi;
```

## Known Security Considerations

### 1. File System Access

The control requires read access to files and may create temporary files. Ensure:
- Files are from trusted sources
- Temporary directory has appropriate permissions
- Temporary files are cleaned up (handled automatically)

### 2. Memory Usage

Large files or high DPI settings can consume significant memory:
- Use `UseTempFiles = true` for large documents
- Validate file size before loading
- Monitor memory usage in production

### 3. COM Interop

TIFF processing uses Windows Imaging Component (COM):
- Ensure Windows is up to date
- COM exceptions are caught and handled
- Error messages don't leak sensitive information

### 4. Dependency Security

Keep dependencies up to date:
- Monitor NuGet packages for security updates
- Review dependency licenses and security advisories
- Use Dependabot or similar tools for automated updates

## Vulnerability Disclosure Timeline

We aim to disclose vulnerabilities responsibly:

1. **Day 0**: Vulnerability reported
2. **Day 2**: Acknowledgment sent
3. **Day 7**: Initial assessment and priority assigned
4. **Day 30**: Fix developed and tested (target)
5. **Day 45**: Patch released (target)
6. **Day 60**: Public disclosure (after users have time to update)

Timelines may vary based on severity and complexity.

## Security Update Process

When a security update is released:

1. GitHub security advisory published
2. Release notes include security information
3. NuGet package updated (if applicable)
4. Users notified through GitHub
5. Detailed information published after reasonable adoption period

## Additional Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Microsoft Security Development Lifecycle](https://www.microsoft.com/en-us/securityengineering/sdl/)
- [.NET Security Best Practices](https://docs.microsoft.com/en-us/dotnet/standard/security/)

## Questions?

If you have questions about this security policy, please open a public issue (non-security related) or contact the maintainers.

---

Thank you for helping keep WinUI 3 PDF Viewer and its users safe!
