# JiraTools Release and Distribution Setup Guide

This guide walks you through setting up JiraTools for distribution via package managers and automated releases.

## Prerequisites

- .NET 8.0+ SDK
- Git repository on GitHub
- GitHub account with admin access to the repository

## Initial Setup

### 1. Repository Configuration

Ensure your GitHub repository is properly configured:

1. **Repository name**: `copilot-jiratools` (or your preferred name)
2. **Visibility**: Public (required for Homebrew)
3. **License**: MIT or compatible license
4. **Branch protection**: Optional but recommended for `main` branch

### 2. GitHub Secrets Configuration

Set up the following secrets in your GitHub repository (`Settings > Secrets and variables > Actions`):

- `NUGET_API_KEY` (optional): For publishing to NuGet.org
- `HOMEBREW_TAP_TOKEN` (optional): Personal access token for updating Homebrew tap

To create a personal access token:
1. Go to GitHub Settings > Developer settings > Personal access tokens
2. Generate a new token with `repo` scope
3. Add it as `HOMEBREW_TAP_TOKEN` secret

### 3. Homebrew Tap Setup

Create a separate repository for your Homebrew tap:

1. Create repository: `homebrew-tap` under your GitHub account
2. Follow the instructions in `docs/HOMEBREW_SETUP.md`

## Release Process

### Automated Release (Recommended)

1. **Create a release using the script**:
   ```bash
   ./scripts/release.sh 1.0.0
   ```

2. **Monitor the GitHub Actions workflow**:
   - Go to your repository's Actions tab
   - Watch the "Release" workflow complete
   - Verify artifacts are created

3. **Update Homebrew tap** (if automated workflow fails):
   ```bash
   ./scripts/update-homebrew-formula.sh 1.0.0
   ```

### Manual Release

1. **Update version in project file**:
   ```xml
   <Version>1.0.0</Version>
   ```

2. **Commit and tag**:
   ```bash
   git add JiraTools.csproj
   git commit -m "chore: bump version to 1.0.0"
   git tag v1.0.0
   git push origin v1.0.0
   git push origin main
   ```

3. **GitHub Actions will automatically**:
   - Build for all platforms
   - Create release with binaries
   - Upload to NuGet (if configured)
   - Update Homebrew tap (if configured)

## Distribution Channels

### 1. Homebrew (macOS/Linux)

Users can install via:
```bash
brew tap peterlockett/tap
brew install jiratools
```

### 2. .NET Global Tool

Users can install via:
```bash
dotnet tool install -g JiraTools
```

### 3. Direct Download

Users can download platform-specific binaries from GitHub releases.

### 4. Package Managers (Future)

- **Chocolatey** (Windows): Requires separate setup
- **Snap** (Linux): Requires separate setup
- **Winget** (Windows): Requires Microsoft Store submission

## Testing Releases

### Local Testing

```bash
# Build and test locally
dotnet build -c Release
dotnet publish -c Release -r osx-arm64 --self-contained

# Test the published binary
./bin/Release/net8.0/osx-arm64/publish/jiratools --help
```

### Testing Homebrew Formula

```bash
# Install from your tap
brew install --build-from-source ./Formula/jiratools.rb

# Test the installation
jiratools --help

# Uninstall for clean testing
brew uninstall jiratools
```

## Troubleshooting

### Common Issues

1. **GitHub Actions failing**:
   - Check secrets are properly configured
   - Verify repository permissions
   - Check workflow logs for specific errors

2. **Homebrew formula issues**:
   - Verify SHA256 checksums match
   - Check formula syntax with `brew audit`
   - Test formula locally before pushing

3. **NuGet publishing fails**:
   - Verify API key is valid
   - Check package metadata in `.csproj`
   - Ensure version hasn't been published before

### Getting Help

1. Check GitHub Actions logs for detailed error messages
2. Test locally before creating releases
3. Verify all prerequisites are met
4. Check file permissions on scripts

## Version Management

### Semantic Versioning

Follow semantic versioning (semver.org):
- `MAJOR.MINOR.PATCH`
- `1.0.0` - Initial release
- `1.0.1` - Bug fixes
- `1.1.0` - New features (backward compatible)
- `2.0.0` - Breaking changes

### Release Cadence

- **Patch releases**: As needed for bug fixes
- **Minor releases**: Monthly or quarterly for new features
- **Major releases**: Annually or when breaking changes are necessary

## Maintenance

### Regular Tasks

1. **Security updates**: Keep dependencies updated
2. **Platform testing**: Test on Windows, macOS, and Linux
3. **Documentation**: Keep README and docs updated
4. **Community**: Respond to issues and pull requests

### Automation Improvements

Consider adding:
1. Automated dependency updates (Dependabot)
2. Automated testing on multiple platforms
3. Integration with more package managers
4. Automated documentation generation
