using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JiraTools.Commands
{
    /// <summary>
    /// Command to display help information
    /// </summary>
    public class HelpCommand : BaseStandaloneCommand
    {
        private readonly CommandFactory _commandFactory;

        public HelpCommand(CommandLineOptions options, CommandFactory commandFactory, ILogger logger = null) 
            : base(options, logger)
        {
            _commandFactory = commandFactory;
        }

        public override string CommandName => "help";

        public override string Description => "Show help information";

        public override async Task<bool> ExecuteAsync()
        {
            ShowHelp();
            return await Task.FromResult(true);
        }

        private void ShowHelp()
        {
            _logger?.LogInformation("JiraTools - Generic tool for interacting with Jira");
            _logger?.LogInformation("");
            _logger?.LogInformation("Commands:");
            
            // Get all commands and show their descriptions
            var commands = _commandFactory.GetCommandMetadata();
            foreach (var command in commands)
            {
                _logger?.LogInformation("  {Command} {Description}", command.Item1.PadRight(20), command.Item2);
            }

            _logger?.LogInformation("");
            _logger?.LogInformation("Workflow Commands (NEW):");
            _logger?.LogInformation("  discover-workflow --issue-key PROJ-123 --transition \"Done\"");
            _logger?.LogInformation("    Learns the workflow path from current status to target status");
            _logger?.LogInformation("  complete --issue-key PROJ-123 --transition \"Done\"");
            _logger?.LogInformation("    Automatically executes all steps to reach target status");
            _logger?.LogInformation("  workflow-help --issue-key PROJ-123");
            _logger?.LogInformation("    Shows current status, available transitions, and cached workflows");
            _logger?.LogInformation("");
            _logger?.LogInformation("Options:");
            _logger?.LogInformation("  --help, -h            Show this help message");
            _logger?.LogInformation("  --url                 Jira URL (default: https://your-company.atlassian.net)");
            _logger?.LogInformation("  --user, --username    Jira username (email)");
            _logger?.LogInformation("  --token, --api-token  Jira API token");
            _logger?.LogInformation("  --project             Jira project key (default: {DefaultProjectKey})", GetDefaultProjectKey());
            _logger?.LogInformation("  --issue, --key        Issue key for operations on existing tasks");
            _logger?.LogInformation("  --type                Issue type (default: Task)");
            _logger?.LogInformation("  --summary             Issue summary/title");
            _logger?.LogInformation("  --description, --desc Issue description");
            _logger?.LogInformation("  --comment             Comment text");
            _logger?.LogInformation("  --components          Comma-separated list of components");
            _logger?.LogInformation("  --transition, --status Status to transition to");
            _logger?.LogInformation("  --parent              Link to parent task (configure PARENT_TASK in .env)");
            _logger?.LogInformation("  --parent-task         Specify parent task to link to (e.g., PROJ-123)");
            _logger?.LogInformation("  --list-only, --list    For transition: list available transitions without executing");
            _logger?.LogInformation("  --verbose             Show more detailed output");
            _logger?.LogInformation("  --yes, -y             Skip confirmation prompts");
            _logger?.LogInformation("  --non-interactive     Run in non-interactive mode");
            _logger?.LogInformation("");
        }

        private string GetDefaultProjectKey()
        {
            return _options?.ProjectKey ?? "PROJ";
        }

        public override bool ValidateParameters()
        {
            return true; // Help command doesn't need validation
        }
    }
}
