class Jiratools < Formula
  desc "A command-line tool for interacting with Jira from development environments"
  homepage "https://github.com/peterlockett/copilot-jiratools"
  version "1.0.0"
  license "MIT"

  if Hardware::CPU.arm?
    url "https://github.com/peterlockett/copilot-jiratools/releases/download/v#{version}/jiratools-#{version}-osx-arm64.tar.gz"
    sha256 "REPLACE_WITH_ACTUAL_SHA256_ARM64"
  else
    url "https://github.com/peterlockett/copilot-jiratools/releases/download/v#{version}/jiratools-#{version}-osx-x64.tar.gz"
    sha256 "REPLACE_WITH_ACTUAL_SHA256_X64"
  end

  def install
    bin.install "jiratools"
  end

  test do
    assert_match "JiraTools", shell_output("#{bin}/jiratools --help")
  end
end
