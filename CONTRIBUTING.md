# Contributing to WinUI 3 PDF Viewer

First off, thank you for considering contributing to WinUI 3 PDF Viewer! It's people like you that make this project better for everyone.

## Code of Conduct

This project and everyone participating in it is governed by our commitment to creating a welcoming and inclusive environment. Please be respectful and constructive in all interactions.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check existing issues to avoid duplicates. When creating a bug report, include:

- **Clear title and description**
- **Steps to reproduce** the behavior
- **Expected behavior**
- **Actual behavior**
- **Screenshots** if applicable
- **Environment details**:
  - OS version
  - .NET version
  - Visual Studio version
  - Windows App SDK version

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, include:

- **Clear title and description**
- **Use case** - explain why this would be useful
- **Possible implementation** if you have ideas
- **Alternatives considered**

### Pull Requests

1. **Fork** the repository
2. **Create a branch** from `master`:
   ```bash
   git checkout -b feature/amazing-feature
   ```
3. **Make your changes** following our coding standards
4. **Test thoroughly**
5. **Commit** with clear messages:
   ```bash
   git commit -m "Add amazing feature"
   ```
6. **Push** to your fork:
   ```bash
   git push origin feature/amazing-feature
   ```
7. **Open a Pull Request**

## Coding Standards

### C# Style Guide

- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use **4 spaces** for indentation (not tabs)
- Use **PascalCase** for public members
- Use **camelCase** for private fields (with `_` prefix for instance fields)
- Use **meaningful names** for variables and methods

### Documentation

- **All public APIs** must have XML documentation
- Include `<summary>`, `<param>`, `<returns>`, and `<exception>` tags
- Document **why**, not just **what**
- Include usage examples for complex features

Example:
```csharp
/// <summary>
/// Asynchronously loads a document file into the viewer.
/// </summary>
/// <param name="file">The storage file to load.</param>
/// <param name="cancellationToken">A token to observe for cancellation requests.</param>
/// <returns>A task representing the asynchronous operation.</returns>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
/// <exception cref="InvalidOperationException">Thrown when no bitmap provider is configured.</exception>
public async Task LoadFileAsync(StorageFile file, CancellationToken cancellationToken = default)
{
    // Implementation
}
```

### Error Handling

- **Always validate inputs** at public API boundaries
- **Throw appropriate exceptions** with helpful messages
- **Use specific exception types** (ArgumentNullException, ArgumentOutOfRangeException, etc.)
- **Include context** in exception messages
- **Clean up resources** on error (use try/finally or using statements)

Example:
```csharp
if (file == null)
    throw new ArgumentNullException(nameof(file));

if (dpi < MinDpi || dpi > MaxDpi)
    throw new ArgumentOutOfRangeException(nameof(dpi), dpi, 
        $"DPI must be between {MinDpi} and {MaxDpi}.");
```

### Async/Await

- **Always use async/await** for I/O operations
- **Support CancellationToken** in async methods
- **Use ConfigureAwait(false)** in library code
- **Don't block on async code** (no .Result or .Wait())

### Resource Management

- **Implement IDisposable** for classes with unmanaged resources
- **Use using statements** or declarations
- **Dispose in finally blocks** if not using 'using'
- **Unsubscribe from events** in Dispose

### Testing

- Write **unit tests** for new features
- Ensure **existing tests pass**
- Test **error conditions** and edge cases
- Test on **multiple Windows versions** if possible

## Project Structure

```
Winui3PdfViewer/
├── Interfaces/          # Public interfaces
├── Providers/           # Bitmap conversion implementations
├── Helpers/             # Utility classes
└── PdfViewerControl.cs  # Main control
```

## Building the Project

1. Open `Winui3PdfViewer.sln` in Visual Studio 2022
2. Restore NuGet packages: `Tools > NuGet Package Manager > Restore`
3. Build: `Ctrl+Shift+B`

## Running Tests

Currently, the project uses manual testing via the demo app. We welcome contributions to add automated tests!

## Git Commit Messages

- Use the **present tense** ("Add feature" not "Added feature")
- Use the **imperative mood** ("Move cursor to..." not "Moves cursor to...")
- **Limit the first line** to 72 characters or less
- **Reference issues and pull requests** liberally after the first line

Examples:
```
Add TIFF codec error detection

- Explicitly specify TiffDecoderId when creating decoder
- Add detailed error messages for missing codec
- Include error code 0x88982F41 handling

Fixes #123
```

## Release Process

1. Update CHANGELOG.md
2. Update version numbers
3. Create a Git tag
4. Create a GitHub release
5. Publish NuGet package (if applicable)

## Questions?

Feel free to open an issue with the `question` label, or reach out to the maintainers.

---

Thank you for contributing! 🎉
