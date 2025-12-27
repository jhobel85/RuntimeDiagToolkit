# Release & Packaging Guide - Manual steps todo locally

This document describes how to build, package, and release DiagnosticsToolkit to GitHub Packages.

## Versioning

We follow [Semantic Versioning](https://semver.org/):
- **MAJOR.MINOR.PATCH** (e.g., `1.0.0`, `1.1.2`)
- Increment MAJOR for breaking changes
- Increment MINOR for new features (backward compatible)
- Increment PATCH for bug fixes

## Building NuGet Packages Locally

### Prerequisites
- .NET 8 SDK
- GitHub.com account (for publishing)

### Build Release Packages

```bash
# Build all projects in Release mode
dotnet build RuntimeDiagToolkit.sln -c Release

# Pack individual packages
dotnet pack DiagnosticsToolkit/DiagnosticsToolkit.csproj -c Release -o ./nupkg
dotnet pack DiagnosticsToolkit.AspNetCore/DiagnosticsToolkit.AspNetCore.csproj -c Release -o ./nupkg
dotnet pack DiagnosticsToolkit.Maui/DiagnosticsToolkit.Maui.csproj -c Release -o ./nupkg
dotnet pack DiagnosticsToolkit.Generators/DiagnosticsToolkit.Generators.csproj -c Release -o ./nupkg
dotnet pack DiagnosticsToolkit.Cli/DiagnosticsToolkit.Cli.csproj -c Release -o ./nupkg
```

Packages will be in `./nupkg/`

## Publishing to GitHub Packages

### Step 1: Update Version

Edit each `.csproj` file:
```xml
<Version>1.1.0</Version>
```

### Step 2: Run Tests & Benchmarks

```bash
dotnet test DiagnosticsToolkit.Tests/DiagnosticsToolkit.Tests.csproj -c Release
dotnet run --project DiagnosticsToolkit.Benchmarks/DiagnosticsToolkit.Benchmarks.csproj -c Release
```

### Step 3: Update CHANGELOG.md

Add a new section for the version:
```markdown
## [1.1.0] - 2025-12-28

### Added
- New feature X
- New feature Y

### Fixed
- Bug fix A
```

### Step 4: Build Packages

```bash
dotnet pack DiagnosticsToolkit/DiagnosticsToolkit.csproj -c Release -o ./nupkg
# ... pack other projects
```

### Step 5: Publish to GitHub Packages

```bash
# Publish core library
dotnet nuget push ./nupkg/DiagnosticsToolkit.1.1.0.nupkg --api-key YOUR_GITHUB_TOKEN --source https://nuget.pkg.github.com/jhobe85/index.json

# Publish extensions
dotnet nuget push ./nupkg/DiagnosticsToolkit.AspNetCore.1.1.0.nupkg --api-key YOUR_GITHUB_TOKEN --source https://nuget.pkg.github.com/jhobe85/index.json
dotnet nuget push ./nupkg/DiagnosticsToolkit.Maui.1.1.0.nupkg --api-key YOUR_GITHUB_TOKEN --source https://nuget.pkg.github.com/jhobe85/index.json
dotnet nuget push ./nupkg/DiagnosticsToolkit.Generators.1.1.0.nupkg --api-key YOUR_GITHUB_TOKEN --source https://nuget.pkg.github.com/jhobe85/index.json
dotnet nuget push ./nupkg/DiagnosticsToolkit.Cli.1.1.0.nupkg --api-key YOUR_GITHUB_TOKEN --source https://nuget.pkg.github.com/jhobe85/index.json
```

**Note**: Generate a GitHub Personal Access Token with `write:packages` permission from https://github.com/settings/tokens

## GitHub Release Workflow - Automated steps via CI Runner

Git tag push will automatically build, test, and publish all packages to GitHub Packages with zero manual intervention.

The workflow also includes continue-on-error: true on the push step, so if there's an issue (like a duplicate version), the workflow won't fail—it will just skip that package and continue.

Automated via GitHub Actions:

1. **Create a Git tag**:
   ```bash
   git tag -a v1.1.0 -m "Release 1.1.0"
   git push origin v1.1.0
   ```

2. **GitHub Actions triggers**:
   - Builds all packages
   - Runs tests
   - Publishes to GitHub Packages (uses built-in `GITHUB_TOKEN`)
   - Creates GitHub Release with CHANGELOG excerpt

3. **View your packages**:
   - Visit https://github.com/jhobe85?tab=packages

## Package Consumption

### Configure GitHub Packages Source
- Package Sources: Add multiple sources files to the provided package.
- Authentication: Store credentials for private feeds (GitHub PAT, Azure Artifacts token). Use ClearTextPassword or encrypted storage.
- Source Priority: Control which source is checked first for packages.
- Fallback Behavior: Configure whether to fail or continue if a source is unavailable.

First, add GitHub Packages as a NuGet source and authenticate:

```bash
# Add GitHub Packages source
dotnet nuget add source https://nuget.pkg.github.com/jhobe85/index.json --name github --username YOUR_GITHUB_USERNAME --password YOUR_GITHUB_TOKEN --store-password-in-clear-text
```

Or create/update `nuget.config` in your project:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="github" value="https://nuget.pkg.github.com/jhobe85/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="YOUR_GITHUB_USERNAME" />
      <add key="ClearTextPassword" value="YOUR_GITHUB_TOKEN" />
    </github>
  </packageSourceCredentials>
</configuration>
```

### NuGet Package Manager
```bash
Install-Package DiagnosticsToolkit -Source github
Install-Package DiagnosticsToolkit.AspNetCore -Source github
Install-Package DiagnosticsToolkit.Maui -Source github
Install-Package DiagnosticsToolkit.Generators -Source github
Install-Package DiagnosticsToolkit.Cli -Source github
```

### .NET CLI
```bash
dotnet add package DiagnosticsToolkit --source github
dotnet add package DiagnosticsToolkit.AspNetCore --source github
dotnet add package DiagnosticsToolkit.Maui --source github
dotnet add package DiagnosticsToolkit.Generators --source github
dotnet add package DiagnosticsToolkit.Cli --source github
```

## Troubleshooting

### GitHub Packages Push Fails
- Verify GitHub token has `write:packages` and `read:packages` permissions
- Check version doesn't already exist on GitHub Packages
- Ensure all tests pass locally
- Verify repository URL in `.csproj` matches your GitHub repository

### Package Installation Fails
- Ensure you've configured authentication (see Package Consumption section)
- Verify your GitHub token has `read:packages` permission
- Check that the package exists: https://github.com/jhobe85?tab=packages

### Package Missing Dependencies
- Run `dotnet pack --include-symbols` for debugging symbols
- Verify `.csproj` ProjectReferences are correct

## Support

For questions or issues, see [CONTRIBUTING.md](CONTRIBUTING.md).
