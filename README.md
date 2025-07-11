# JiraTools

A cross-platform command-line tool for interacting with Jira from development environments.

## Overview

JiraTools is a lightweight CLI utility that helps developers interact with Jira instances. The tool supports basic Jira operations like creating tasks, adding comments, transitioning tasks between states, and more.

## Installation

### Homebrew (macOS/Linux)

First, add the tap:
```bash
brew tap peterlockett/tap
brew install jiratools
```

### .NET Global Tool

If you have .NET 8.0+ installed:
```bash
dotnet tool install -g JiraTools
```

### Manual Installation

Download the appropriate release for your platform from the [releases page](https://github.com/peterlockett/copilot-jiratools/releases):

- **Windows (x64)**: Download and extract the `-win-x64.zip` file
- **Linux (x64)**: Download and extract the `-linux-x64.tar.gz` file  
- **macOS (Intel)**: Download and extract the `-osx-x64.tar.gz` file
- **macOS (Apple Silicon)**: Download and extract the `-osx-arm64.tar.gz` file

Add the extracted `jiratools` executable to your PATH.

### Build from Source

Requirements: .NET 8.0+ SDK

```bash
git clone https://github.com/peterlockett/copilot-jiratools.git
cd copilot-jiratools
dotnet build -c Release
dotnet publish -c Release -r <your-runtime> --self-contained
```

Replace `<your-runtime>` with: `win-x64`, `linux-x64`, `osx-x64`, or `osx-arm64`

## Features

- Create, update, and transition Jira tasks
- Add comments to existing tasks
- **Smart workflow discovery and automation** - Learn and automate complex transition sequences
- **Intelligent workflow caching** - Remembers successful paths for faster execution
- **One-command completion** - Automatically transition through multiple steps to reach target status
- Read credentials securely from a `.env` file
- Support for linking tasks to parent tasks
- Command-line and interactive modes
- Cross-platform support (Windows, macOS, Linux)

## Getting Started

### 1. Set up credentials

Create a `.env` file in your working directory with the following content:

```
JIRA_API_TOKEN=your-api-token-here
JIRA_USERNAME=your.email@company.com
JIRA_URL=https://your-instance.atlassian.net
JIRA_PROJECT_KEY=PROJ
```

To generate an API token, visit: https://id.atlassian.com/manage-profile/security/api-tokens

### 2. Run the tool

```bash
# Using installed version
jiratools --help

# Using .NET global tool
jiratools --help

# Using manual installation
./jiratools --help
```

## Usage

### Basic Commands

```bash
# Create a new task
jiratools create-task --summary "Fix critical bug" --description "This bug needs immediate attention"

# Update an existing task
jiratools update-task --issue-key PROJ-12345 --summary "Updated summary"

# Add a comment to a task
jiratools add-comment --issue-key PROJ-12345 --comment "Work completed successfully"

# Transition a task to a new state (interactive)
jiratools transition --issue-key PROJ-12345

# Transition a task directly with confirmation
jiratools transition --issue-key PROJ-12345 --transition "Done" --yes

# List available transitions without executing
jiratools transition --issue-key PROJ-12345 --list-only

# Create a task linked to a parent task
jiratools create-task --summary "Subtask work" --parent
```

### 🚀 Smart Workflow Commands (NEW)

```bash
# Get workflow help and current status
jiratools workflow-help --issue-key PROJ-12345

# Discover and cache workflow path to "Done" status
jiratools discover-workflow --issue-key PROJ-12345 --target "Done"

# Auto-complete entire workflow to "Done" in one command
jiratools complete --issue-key PROJ-12345 --target "Done"

# Non-interactive completion (for scripts)
jiratools complete --issue-key PROJ-12345 --target "Done" --non-interactive

# Discover workflow to custom status
jiratools discover-workflow --issue-key PROJ-12345 --target "Ready for Release"
```

**How Smart Workflows Work:**
1. **Discovery**: The tool learns your Jira workflow by exploring available transitions
2. **Caching**: Successful paths are saved locally for reuse
3. **Automation**: Execute multi-step workflows with a single command
4. **Intelligence**: Suggests common completion paths based on usage

### Command Reference

#### create-task
Creates a new Jira task.
- `--summary` (required) - Task title
- `--description`, `--desc` - Task description
- `--type` - Issue type (default: Task)
- `--components` - Components to assign
- `--parent` - Link to default parent task

#### update-task
Updates an existing Jira task.
- `--issue-key`, `--key`, `--issue` (required) - Issue key (e.g., PROJ-12345)
- `--summary` - Update task summary
- `--description`, `--desc` - Update task description

#### add-comment
Adds a comment to a task.
- `--issue-key`, `--key`, `--issue` (required) - Issue key
- `--comment` (required) - Comment text

#### transition
Transitions a task to a new status.
- `--issue-key`, `--key`, `--issue` (required) - Issue key
- `--transition`, `--status` - Target transition name (optional, interactive if not provided)
- `--list-only`, `--list` - Show available transitions only
- `--yes`, `-y` - Skip confirmation prompts
- `--non-interactive`, `--auto` - Non-interactive mode

#### Smart Workflow Commands

##### discover-workflow
Discovers and caches workflow paths for future automation.
- `--issue-key`, `--key`, `--issue` (required) - Issue key to analyze
- `--target`, `--transition` - Target status to reach (default: "Done")

##### complete
Auto-executes complete workflow to reach target status.
- `--issue-key`, `--key`, `--issue` (required) - Issue key to complete
- `--target`, `--transition` - Target status to reach (default: "Done")
- `--yes`, `-y` - Skip confirmation prompts
- `--non-interactive`, `--auto` - Non-interactive mode for scripts

##### workflow-help
Shows workflow information and suggestions.
- `--issue-key`, `--key`, `--issue` (required) - Issue key to analyze

#### Global Options
- `--help`, `-h` - Show help
- `--url` - Jira URL (default: configured in .env)
- `--user`, `--username` - Jira username
- `--token`, `--api-token` - Jira API token
- `--project` - Project key (default: configured in .env)

### Smart Workflow Examples

#### Before: Manual Multi-Step Transitions
```bash
# Old way - multiple manual commands
jiratools transition --issue-key PROJ-12345 --transition "Start Progress"
jiratools transition --issue-key PROJ-12345 --transition "Ready for Review"
jiratools transition --issue-key PROJ-12345 --transition "Start Review" 
jiratools transition --issue-key PROJ-12345 --transition "Ready for Testing"
jiratools transition --issue-key PROJ-12345 --transition "Start Testing"
jiratools transition --issue-key PROJ-12345 --transition "Done"
```

#### After: Smart Workflow Automation
```bash
# New way - one command does it all!
jiratools complete --issue-key PROJ-12345 --target "Done"

# First time? Discover the workflow path:
jiratools discover-workflow --issue-key PROJ-12345 --target "Done"
# Then use complete command above
```

#### Real-World Example
```bash
# 1. Check current status and get suggestions
jiratools workflow-help --issue-key PROJ-12345
# Output: Current Status: "Open", suggests cached workflows

# 2. Auto-complete to "Ready for Release" 
jiratools complete --issue-key PROJ-12345 --target "Ready for Release"
# Executes: Open → Doing → Ready for Verification → Verifying → Ready for Acceptance → Ready for Release

# 3. For scripts - non-interactive mode
jiratools complete --issue-key PROJ-12345 --target "Done" --non-interactive
```

**Note**: The tool learns your organization's specific workflow patterns and caches them for future use.

### Usage Examples

```bash
# Create a task with specific components
jiratools create-task --summary "Component Migration to New Framework" --description "Migration details here" --components "Migration"

# Transition multiple tasks quickly
jiratools transition --issue-key PROJ-12345 --transition "Done" --yes
```

### Environment Variables

All settings can be provided via command-line arguments or set in the `.env` file:

| Environment Variable | Command Line      | Description                       |
|---------------------|-------------------|-----------------------------------|
| JIRA_API_TOKEN      | --token           | Your Jira API token               |
| JIRA_USERNAME       | --user            | Your Jira username (email)        |
| JIRA_URL            | --url             | Jira instance URL                 |
| JIRA_PROJECT_KEY    | --project         | Default Jira project key          |

## Security

- API tokens are stored in the `.env` file which is excluded from git via `.gitignore`
- No credentials are logged to the console
- Tokens are only stored in memory during execution

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test your changes
5. Submit a pull request

## Releasing

To create a new release:

1. Run the release script: `./scripts/release.sh 1.0.1`
2. This will create a git tag and trigger the GitHub Actions workflow
3. The workflow will build binaries for all platforms and create a GitHub release
4. Update the Homebrew formula: `./scripts/update-homebrew-formula.sh 1.0.1`

## Development

To extend this tool:
1. Add new command methods to `Program.cs`
2. Update the command-line parser and help text
3. Add any specialized client methods to `JiraClient.cs`
4. Test your changes across platforms
