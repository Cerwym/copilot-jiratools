#!/bin/bash

# Script to update Homebrew formula after a release
# This should be run after GitHub Actions creates a new release

set -e

VERSION="$1"
if [ -z "$VERSION" ]; then
    echo "Usage: $0 <version>"
    echo "Example: $0 1.0.0"
    exit 1
fi

echo "Updating Homebrew formula for version $VERSION..."

# Download the release assets to calculate checksums
TEMP_DIR=$(mktemp -d)
cd "$TEMP_DIR"

echo "Downloading release assets..."
wget "https://github.com/peterlockett/copilot-jiratools/releases/download/v${VERSION}/jiratools-${VERSION}-osx-x64.tar.gz"
wget "https://github.com/peterlockett/copilot-jiratools/releases/download/v${VERSION}/jiratools-${VERSION}-osx-arm64.tar.gz"

# Calculate checksums
SHA256_X64=$(sha256sum "jiratools-${VERSION}-osx-x64.tar.gz" | awk '{print $1}')
SHA256_ARM64=$(sha256sum "jiratools-${VERSION}-osx-arm64.tar.gz" | awk '{print $1}')

echo "SHA256 for x64: $SHA256_X64"
echo "SHA256 for arm64: $SHA256_ARM64"

# Update the formula
cd - > /dev/null
FORMULA_FILE="Formula/jiratools.rb"

# Create updated formula
cat > "$FORMULA_FILE" << EOF
class Jiratools < Formula
  desc "A command-line tool for interacting with Jira from development environments"
  homepage "https://github.com/peterlockett/copilot-jiratools"
  version "$VERSION"
  license "MIT"

  if Hardware::CPU.arm?
    url "https://github.com/peterlockett/copilot-jiratools/releases/download/v#{version}/jiratools-#{version}-osx-arm64.tar.gz"
    sha256 "$SHA256_ARM64"
  else
    url "https://github.com/peterlockett/copilot-jiratools/releases/download/v#{version}/jiratools-#{version}-osx-x64.tar.gz"
    sha256 "$SHA256_X64"
  end

  def install
    bin.install "jiratools"
  end

  test do
    assert_match "JiraTools", shell_output("#{bin}/jiratools --help")
  end
end
EOF

# Clean up
rm -rf "$TEMP_DIR"

echo "Formula updated successfully!"
echo "Don't forget to:"
echo "1. Commit and push the updated formula"
echo "2. Create a pull request to your homebrew tap repository"
echo "3. Test the formula locally with: brew install --build-from-source ./Formula/jiratools.rb"
