#!/bin/bash

# Simple script to create a new release
# This will trigger the GitHub Actions workflow

set -e

VERSION="$1"
if [ -z "$VERSION" ]; then
    echo "Usage: $0 <version>"
    echo "Example: $0 1.0.0"
    exit 1
fi

# Validate version format (basic check)
if ! [[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "Error: Version must be in format X.Y.Z (e.g., 1.0.0)"
    exit 1
fi

echo "Creating release for version $VERSION..."

# Update the version in the project file
sed -i.bak "s/<Version>.*<\/Version>/<Version>$VERSION<\/Version>/" JiraTools.csproj
rm JiraTools.csproj.bak 2>/dev/null || true

# Commit the version change
git add JiraTools.csproj
git commit -m "chore: bump version to $VERSION" || echo "No changes to commit"

# Create and push the tag
git tag "v$VERSION"
git push origin "v$VERSION"
git push origin main || git push origin master

echo "Release v$VERSION created successfully!"
echo "Check GitHub Actions for build progress: https://github.com/peterlockett/copilot-jiratools/actions"
