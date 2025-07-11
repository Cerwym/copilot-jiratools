# JiraTools

A command-line tool for interacting with Jira from the N-Central development environment.

## Overview

JiraTools is a lightweight CLI utility that helps N-Central developers interact with the N-able Jira instance. The tool supports basic Jira operations like creating tasks, adding comments, transitioning tasks between states, and more.

## Features

- Create, update, and transition Jira tasks
- Add comments to existing tasks
- Read credentials securely from a `.env` file
- Specialized utilities for migration tasks
- Support for linking tasks to parent tasks
- Command-line and interactive modes

## Setup

### 1. Create a `.env` file

Create a `.env` file in the same directory as the JiraTools executable with the following content:

```
JIRA_API_TOKEN=your-api-token-here
JIRA_USERNAME=your.email@n-able.com
JIRA_URL=https://n-able.atlassian.net
JIRA_PROJECT_KEY=NCCF
```

To generate an API token, visit: https://id.atlassian.com/manage-profile/security/api-tokens

### 2. Build the project

```bash
# On macOS/Linux
dotnet build

# On Windows
dotnet build
```

## Usage

### Basic Commands

```bash
# Create a new task
dotnet run -- create-task --summary "Fix critical bug" --description "This bug needs immediate attention"

# Update an existing task
dotnet run -- update-task --issue-key NCCF-12345 --summary "Updated summary"

# Add a comment to a task
dotnet run -- add-comment --issue-key NCCF-12345 --comment "Work completed successfully"

# Transition a task to a new state (interactive)
dotnet run -- transition --issue-key NCCF-12345

# Transition a task directly with confirmation
dotnet run -- transition --issue-key NCCF-12345 --transition "Done" --yes

# List available transitions without executing
dotnet run -- transition --issue-key NCCF-12345 --list-only

# Create a task linked to parent NCCF-741626
dotnet run -- create-task --summary "Subtask work" --parent
```

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
- `--issue-key`, `--key`, `--issue` (required) - Issue key (e.g., NCCF-12345)
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

#### Global Options
- `--help`, `-h` - Show help
- `--url` - Jira URL (default: https://n-able.atlassian.net)
- `--user`, `--username` - Jira username
- `--token`, `--api-token` - Jira API token
- `--project` - Project key (default: NCCF)

### N-able Workflow Transitions

The N-able Jira workflow requires specific transition sequences:

```bash
# Complete workflow from Open to Closed
dotnet run -- transition --issue-key NCCF-12345 --transition "Start doing"
dotnet run -- transition --issue-key NCCF-12345 --transition "Ready for verification"
dotnet run -- transition --issue-key NCCF-12345 --transition "Start verification"
dotnet run -- transition --issue-key NCCF-12345 --transition "Ready for acceptance"
dotnet run -- transition --issue-key NCCF-12345 --transition "Accept for Release"
dotnet run -- transition --issue-key NCCF-12345 --transition "Release to Closed"
```

**Common Transition Flow:**
1. `Open` → `Start doing` → `Doing`
2. `Doing` → `Ready for verification` → `Ready for Verification`
3. `Ready for Verification` → `Start verification` → `Verifying`
4. `Verifying` → `Ready for acceptance` → `Ready for Acceptance`
5. `Ready for Acceptance` → `Accept for Release` → `Ready for Release`
6. `Ready for Release` → `Release to Closed` → `Closed`

### Migration Task Examples

```bash
# Create a migration task
dotnet run -- create-task --summary "Component Migration to New Framework" --description "Migration details here" --components "Migration" --parent

# Transition multiple tasks quickly
dotnet run -- transition --issue-key NCCF-12345 --transition "Done" --yes
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

## Development

To extend this tool:
1. Add new command methods to `Program.cs`
2. Update the command-line parser and help text
3. Add any specialized client methods to `JiraClient.cs`
