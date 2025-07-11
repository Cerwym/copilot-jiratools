# JiraTools AI Reference

## Code Structure
- JiraTools.csproj - Project file
- src/Program.cs - Main application with command handling
- src/JiraClient.cs - API integration with Jira
- src/EnvFileReader.cs - Environment file reader
- Built executable: bin/Debug/net48/JiraTools.exe

## Command Syntax
Correct format for commands (note the `--` after `dotnet run`):
```bash
cd /Users/peterlockett/Documents/Projects/n-central-win-agent/dev-assist/JiraTools && dotnet run -- [command] [options]
```

## Available Commands

### 1. create-task
Creates a new Jira task.

**Syntax:**
```bash
dotnet run -- create-task --summary "Task title" --description "Task description" [options]
```

**Required Options:**
- `--summary` - The task title/summary

**Optional Options:**
- `--description`, `--desc` - Task description
- `--type` - Issue type (default: Task)
- `--components` - Components to assign
- `--parent` - Link to parent task (uses default parent from environment)

**Example:**
```bash
dotnet run -- create-task --summary "Fix critical bug" --description "This bug needs immediate attention" --type "Bug" --components "Windows Agent"
```

### 2. update-task
Updates an existing Jira task.

**Syntax:**
```bash
dotnet run -- update-task --issue-key NCCF-12345 [options]
```

**Required Options:**
- `--issue-key`, `--key`, `--issue` - The Jira issue key (e.g., NCCF-12345)

**Optional Options:**
- `--summary` - Update the task summary
- `--description`, `--desc` - Update the task description

**Example:**
```bash
dotnet run -- update-task --issue-key NCCF-12345 --summary "Updated task title"
```

### 3. add-comment
Adds a comment to an existing Jira task.

**Syntax:**
```bash
dotnet run -- add-comment --issue-key NCCF-12345 --comment "Your comment text"
```

**Required Options:**
- `--issue-key`, `--key`, `--issue` - The Jira issue key
- `--comment` - The comment text to add

**Example:**
```bash
dotnet run -- add-comment --issue-key NCCF-12345 --comment "Work completed successfully"
```

### 4. transition
Transitions a Jira task to a new status.

**Syntax:**
```bash
dotnet run -- transition --issue-key NCCF-12345 [--transition "Status Name"] [options]
```

**Required Options:**
- `--issue-key`, `--key`, `--issue` - The Jira issue key

**Optional Options:**
- `--transition`, `--status` - The target transition name (if not provided, interactive selection)
- `--list-only`, `--list` - Only show available transitions without executing
- `--yes`, `-y` - Skip confirmation prompts
- `--non-interactive`, `--auto` - Run in non-interactive mode

**Examples:**
```bash
# Interactive transition selection
dotnet run -- transition --issue-key NCCF-12345

# Direct transition with confirmation
dotnet run -- transition --issue-key NCCF-12345 --transition "Done"

# Non-interactive transition (auto-confirm)
dotnet run -- transition --issue-key NCCF-12345 --transition "Done" --yes

# List available transitions only
dotnet run -- transition --issue-key NCCF-12345 --list-only
```

**N-able Workflow Transitions:**
Common transition sequences in the N-able Jira workflow:
1. `Open` → `Start doing` → `Doing`
2. `Doing` → `Ready for verification` → `Ready for Verification`
3. `Ready for Verification` → `Start verification` → `Verifying`
4. `Verifying` → `Ready for acceptance` → `Ready for Acceptance`
5. `Ready for Acceptance` → `Accept for Release` → `Ready for Release`
6. `Ready for Release` → `Release to Closed` → `Closed`

### 5. comment-task
Comments on tasks mentioned in a document.

**Syntax:**
```bash
dotnet run -- comment-task [options]
```

**Optional Options:**
- `--comment` - The comment to add to tasks found in documents

## Global Options
These options work with all commands:

- `--help`, `-h` - Show help message
- `--url` - Jira URL (default: https://n-able.atlassian.net)
- `--user`, `--username` - Jira username (email)
- `--token`, `--api-token` - Jira API token
- `--project` - Jira project key (default: NCCF)

## Creating Retrospective Tasks
```bash
# QScheduler task
dotnet run -- create-task --summary "QScheduler Migration to Nable.Quartz103" --description "Migration of QScheduler project from using _tempdlls/Quartz.dll to proper NuGet package reference (Nable.Quartz103 v0.0.1). This task was completed as part of the Quartz.NET migration initiative on July 11, 2025." --type "Task" --components "Quartz Migration" --parent

# RebootManager task
dotnet run -- create-task --summary "RebootManager Migration to Nable.Quartz103" --description "Migration of RebootManager project from using _tempdlls/Quartz.dll to proper NuGet package reference (Nable.Quartz103 v0.0.1). This task was completed as part of the Quartz.NET migration initiative on July 11, 2025." --type "Task" --components "Quartz Migration" --parent

# PatchManager task
dotnet run -- create-task --summary "PatchManager Migration to Nable.Quartz103" --description "Migration of PatchManager project from using _tempdlls/Quartz.dll to proper NuGet package reference (Nable.Quartz103 v0.0.1). This task was completed as part of the Quartz.NET migration initiative on July 11, 2025." --type "Task" --components "Quartz Migration" --parent
```

## Important Files
- `.env` - Contains API credentials and project information (JIRA_API_TOKEN, JIRA_USERNAME, JIRA_URL, JIRA_PROJECT_KEY)

## Common Errors
1. **Command format**: Ensure `--` is used after `dotnet run` before the command
2. **Authentication issues**: Check .env file credentials
3. **Transition errors**: Some transitions require additional fields (like Fix Versions)
4. **Invalid transition names**: Use `--list-only` to see available transitions first
