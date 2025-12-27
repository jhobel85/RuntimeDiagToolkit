# Release & Packaging Guide

This document describes how to build, package, and release DiagnosticsToolkit to NuGet.

## Versioning

We follow [Semantic Versioning](https://semver.org/):
- **MAJOR.MINOR.PATCH** (e.g., `1.0.0`, `1.1.2`)
- Increment MAJOR for breaking changes
- Increment MINOR for new features (backward compatible)
- Increment PATCH for bug fixes

## Building NuGet Packages Locally

### Prerequisites
- .NET 8 SDK
- NuGet.org account (for publishing)

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

## Publishing to NuGet.org

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

### Step 5: Publish to NuGet.org

```bash
# Publish core library
dotnet nuget push ./nupkg/DiagnosticsToolkit.1.1.0.nupkg --api-key YOUR_NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# Publish extensions
dotnet nuget push ./nupkg/DiagnosticsToolkit.AspNetCore.1.1.0.nupkg --api-key YOUR_NUGET_API_KEY --source https://api.nuget.org/v3/index.json
dotnet nuget push ./nupkg/DiagnosticsToolkit.Maui.1.1.0.nupkg --api-key YOUR_NUGET_API_KEY --source https://api.nuget.org/v3/index.json
dotnet nuget push ./nupkg/DiagnosticsToolkit.Generators.1.1.0.nupkg --api-key YOUR_NUGET_API_KEY --source https://api.nuget.org/v3/index.json
dotnet nuget push ./nupkg/DiagnosticsToolkit.Cli.1.1.0.nupkg --api-key YOUR_NUGET_API_KEY --source https://api.nuget.org/v3/index.json
```

## GitHub Release Workflow

Git tag push will automatically build, test, and publish all packages to NuGet.org with zero manual intervention.

The workflow also includes continue-on-error: true on the NuGet push step, so if there's an issue (like a duplicate version), the workflow won't fail—it will just skip that package and continue.

Automated via GitHub Actions:

1. **Create a Git tag**:
   ```bash
   git tag -a v1.1.0 -m "Release 1.1.0"
   git push origin v1.1.0
   ```

2. **GitHub Actions triggers**:
   - Builds all packages
   - Runs tests & benchmarks
   - Publishes to NuGet.org (requires `NUGET_API_KEY` secret)
   - Creates GitHub Release with CHANGELOG excerpt

## Package Consumption

### NuGet Package Manager
```bash
Install-Package DiagnosticsToolkit
Install-Package DiagnosticsToolkit.AspNetCore
Install-Package DiagnosticsToolkit.Maui
Install-Package DiagnosticsToolkit.Generators
Install-Package DiagnosticsToolkit.Cli
```

### .NET CLI
```bash
dotnet add package DiagnosticsToolkit
dotnet add package DiagnosticsToolkit.AspNetCore
dotnet add package DiagnosticsToolkit.Maui
dotnet add package DiagnosticsToolkit.Generators
dotnet add package DiagnosticsToolkit.Cli
```

## Troubleshooting

### NuGet Push Fails
- Verify API key is valid and not expired - set NUGET_API_KEY secret in GitHub repo settings
    - Workflow automatically builds, tests, packs, and publishes to NuGet.org
- Check version doesn't already exist on NuGet.org
- Ensure all tests pass locally

### Package Missing Dependencies
- Run `dotnet pack --include-symbols` for debugging symbols
- Verify `.csproj` ProjectReferences are correct

## Support

For questions or issues, see [CONTRIBUTING.md](CONTRIBUTING.md).
