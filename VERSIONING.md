# Versioning Guide for ITW.FluentMasker

This project uses **MinVer** for automatic semantic versioning based on Git tags and commit history.

## How It Works

MinVer automatically calculates the package version by:
1. Finding the latest Git tag (e.g., `v2.0.1`)
2. Counting commits since that tag (git height)
3. Generating a version number based on the tag and height

## Quick Reference

| Scenario | Resulting Version |
|----------|-------------------|
| On tag `v2.0.1` (clean) | `2.0.1` |
| On tag `v2.0.1` (dirty working tree) | `2.0.1+uncommitted-changes` |
| 5 commits after `v2.0.1` | `2.0.2-preview.0.5` |
| 10 commits after `v2.0.1` | `2.0.2-preview.0.10` |
| No tags in repository | `2.0.0-preview.0.{height}` |

## Configuration

The following MinVer properties are configured in `ITW.FluentMasker.csproj`:

```xml
<MinVerTagPrefix>v</MinVerTagPrefix>                           <!-- Tags must start with 'v' (e.g., v2.0.1) -->
<MinVerMinimumMajorMinor>2.0</MinVerMinimumMajorMinor>         <!-- Fallback version if no tags exist -->
<MinVerDefaultPreReleaseIdentifiers>preview</MinVerDefaultPreReleaseIdentifiers>  <!-- Pre-release label -->
<MinVerVerbosity>normal</MinVerVerbosity>                      <!-- Show version calculation during build -->
```

## Release Workflow

### 1. Development Phase

During development, each commit automatically increments the version:

```bash
# Working on features after v2.0.1 release
git commit -m "Add new feature"
# Build produces: 2.0.2-preview.0.1

git commit -m "Fix bug"
# Build produces: 2.0.2-preview.0.2
```

**Preview versions** are automatically generated and won't conflict with stable releases.

### 2. Creating a Release

When you're ready to release a new version:

```bash
# Ensure working directory is clean
git status

# Create and push a version tag
git tag v2.0.2
git push origin v2.0.2

# Build the package
dotnet pack -c Release

# The package will be versioned as 2.0.2 (no preview suffix)
```

### 3. Publishing to NuGet

```bash
# Build the release package
dotnet pack -c Release -o ./nupkg

# Publish to NuGet.org
dotnet nuget push ./nupkg/ITW.FluentMasker.2.0.2.nupkg \
    --api-key YOUR_API_KEY \
    --source https://api.nuget.org/v3/index.json
```

## Semantic Versioning Rules

Follow [Semantic Versioning 2.0.0](https://semver.org/):

### MAJOR.MINOR.PATCH (e.g., 2.0.1)

- **MAJOR** (`2`) - Breaking changes, incompatible API changes
- **MINOR** (`0`) - New features, backward-compatible
- **PATCH** (`1`) - Bug fixes, backward-compatible

### Examples

```bash
# Bug fix release
git tag v2.0.2

# New feature release (backward-compatible)
git tag v2.1.0

# Breaking change release
git tag v3.0.0
```

## Pre-Release Versions

MinVer automatically creates pre-release versions between tags:

### Preview Versions (automatic)
- `2.0.2-preview.0.1` - 1 commit after v2.0.1
- `2.0.2-preview.0.5` - 5 commits after v2.0.1

### Manual Pre-Release Tags

You can create manual pre-release tags:

```bash
# Alpha release
git tag v2.1.0-alpha.1
git push origin v2.1.0-alpha.1
# Produces: 2.1.0-alpha.1

# Beta release
git tag v2.1.0-beta.1
git push origin v2.1.0-beta.1
# Produces: 2.1.0-beta.1

# Release candidate
git tag v2.1.0-rc.1
git push origin v2.1.0-rc.1
# Produces: 2.1.0-rc.1

# Final release
git tag v2.1.0
git push origin v2.1.0
# Produces: 2.1.0
```

## Checking the Current Version

### During Build

MinVer displays the calculated version during build:

```bash
dotnet build
# Output:
# MinVer: Using version 2.0.2-preview.0.3
```

### Using MSBuild

```bash
dotnet msbuild -t:MinVer
# Output shows detailed version calculation
```

### In PowerShell

```powershell
# Get the version that will be built
dotnet msbuild -getProperty:Version

# Get all version properties
dotnet msbuild -getProperty:MinVerVersion
```

## Troubleshooting

### Issue: Version is always 2.0.0-preview.0.X

**Cause**: No Git tags exist in the repository.

**Solution**: Create your first tag:
```bash
git tag v2.0.1
git push origin v2.0.1
```

### Issue: Version shows as 0.0.0-alpha.0.X

**Cause**: Not in a Git repository or Git is not installed.

**Solution**: 
```bash
# Initialize Git if needed
git init

# Or ensure Git is in PATH
git --version
```

### Issue: Dirty working tree adds +uncommitted-changes suffix

**Cause**: Uncommitted changes in the working directory.

**Solution**: Commit or stash your changes before building release packages:
```bash
git status
git add .
git commit -m "Prepare for release"
```

### Issue: Tag format not recognized

**Cause**: Tag doesn't start with 'v' prefix.

**Solution**: Use 'v' prefix in tags:
```bash
# ? Wrong
git tag 2.0.1

# ? Correct
git tag v2.0.1
```

## Advanced Configuration

### Change Tag Prefix

To use a different tag prefix (e.g., `release-`):

```xml
<MinVerTagPrefix>release-</MinVerTagPrefix>
```

Then use tags like: `release-2.0.1`

### Change Pre-Release Identifier

To use a different pre-release label (e.g., `alpha`):

```xml
<MinVerDefaultPreReleaseIdentifiers>alpha</MinVerDefaultPreReleaseIdentifiers>
```

Commits between tags will be: `2.0.2-alpha.0.5`

### Skip Pre-Release Versioning

To version as patch increments without pre-release suffix:

```xml
<MinVerDefaultPreReleaseIdentifiers></MinVerDefaultPreReleaseIdentifiers>
```

Commits between tags will be: `2.0.2+5` (build metadata only)

### Build Metadata

Add custom build metadata:

```xml
<MinVerBuildMetadata>$(BUILD_NUMBER)</MinVerBuildMetadata>
```

Result: `2.0.1+build.123`

## CI/CD Integration

### GitHub Actions

```yaml
name: Build and Pack

on:
  push:
    branches: [ main ]
    tags: [ 'v*' ]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0  # Required for MinVer to read Git history
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      
      - name: Restore
        run: dotnet restore
      
      - name: Build
        run: dotnet build -c Release --no-restore
      
      - name: Pack
        run: dotnet pack -c Release --no-build -o ./artifacts
      
      - name: Push to NuGet (on tag)
        if: startsWith(github.ref, 'refs/tags/v')
        run: dotnet nuget push ./artifacts/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
```

### Azure DevOps

```yaml
trigger:
  branches:
    include:
      - main
  tags:
    include:
      - v*

pool:
  vmImage: 'ubuntu-latest'

steps:
- checkout: self
  fetchDepth: 0  # Required for MinVer

- task: UseDotNet@2
  inputs:
    version: '8.0.x'

- task: DotNetCoreCLI@2
  displayName: 'dotnet build'
  inputs:
    command: 'build'
    configuration: 'Release'

- task: DotNetCoreCLI@2
  displayName: 'dotnet pack'
  inputs:
    command: 'pack'
    configuration: 'Release'
    outputDir: '$(Build.ArtifactStagingDirectory)'

- task: NuGetCommand@2
  displayName: 'Push to NuGet'
  condition: startsWith(variables['Build.SourceBranch'], 'refs/tags/v')
  inputs:
    command: 'push'
    packagesToPush: '$(Build.ArtifactStagingDirectory)/**/*.nupkg'
    nuGetFeedType: 'external'
    publishFeedCredentials: 'NuGet.org'
```

## Best Practices

1. **Always use annotated tags for releases**:
   ```bash
   git tag -a v2.0.1 -m "Release version 2.0.1"
   ```

2. **Tag after merging to main**:
   ```bash
   git checkout main
   git pull
   git tag v2.0.1
   git push origin v2.0.1
   ```

3. **Don't delete tags** - They're part of your version history

4. **Use consistent versioning**:
   - Start with `v1.0.0` for first release
   - Increment MAJOR for breaking changes
   - Increment MINOR for new features
   - Increment PATCH for bug fixes

5. **Document breaking changes** in release notes when incrementing MAJOR version

## References

- [MinVer Documentation](https://github.com/adamralph/minver)
- [Semantic Versioning 2.0.0](https://semver.org/)
- [Git Tagging](https://git-scm.com/book/en/v2/Git-Basics-Tagging)

---

**Current Configuration Summary**:
- **Tag Prefix**: `v` (e.g., v2.0.1)
- **Minimum Version**: `2.0.0`
- **Pre-Release Label**: `preview`
- **Verbosity**: `normal` (shows version during build)
