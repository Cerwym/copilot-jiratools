using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JiraTools.Commands
{
    /// <summary>
    /// Command to show workflow help and suggestions for an issue
    /// </summary>
    public class WorkflowHelpCommand : BaseCommand
    {
        public WorkflowHelpCommand(IJiraClient jiraClient, CommandLineOptions options, ILogger logger = null) 
            : base(jiraClient, options, logger)
        {
        }

        public override string CommandName => "workflow-help";

        public override string Description => "Show workflow information and suggestions";

        public override async Task<bool> ExecuteAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_options.IssueKey))
                {
                    if (_options.NonInteractive)
                    {
                        _logger?.LogError("Issue key is required but not provided in non-interactive mode.");
                        return false;
                    }
                    _options.IssueKey = PromptForInput("Enter issue key");
                }

                if (string.IsNullOrEmpty(_options.IssueKey))
                {
                    _logger?.LogError("Issue key is required.");
                    return false;
                }

                var discovery = new WorkflowDiscovery(_jiraClient, _options.ProjectKey, _logger);
                
                // Get current status and issue type
                var currentStatus = await _jiraClient.GetIssueStatusAsync(_options.IssueKey);
                var issueType = await _jiraClient.GetIssueTypeAsync(_options.IssueKey);
                
                _logger?.LogInformation("Issue: {IssueKey}", _options.IssueKey);
                _logger?.LogInformation("Type: {IssueType}", issueType);
                _logger?.LogInformation("Current Status: {CurrentStatus}", currentStatus);
                _logger?.LogInformation("");
                
                // Show available transitions
                var transitions = await _jiraClient.GetAvailableTransitionsAsync(_options.IssueKey);
                _logger?.LogInformation("Available next transitions:");
                foreach (var transition in transitions)
                {
                    _logger?.LogInformation("  • {TransitionName}", transition.Key);
                }
                _logger?.LogInformation("");
                
                // Show cached workflow suggestions
                var suggestions = discovery.GetCommonWorkflowSuggestions(issueType, currentStatus);
                if (suggestions.Any())
                {
                    _logger?.LogInformation("Common completion workflows:");
                    foreach (var suggestion in suggestions)
                    {
                        _logger?.LogInformation("  • {Suggestion}", suggestion);
                    }
                    _logger?.LogInformation("");
                }
                
                _logger?.LogInformation("Commands you can use:");
                _logger?.LogInformation("  jiratools transition --issue-key {IssueKey} --transition \"<transition-name>\"", _options.IssueKey);
                _logger?.LogInformation("  jiratools complete --issue-key {IssueKey} --transition \"<target-status>\"", _options.IssueKey);
                _logger?.LogInformation("  jiratools discover-workflow --issue-key {IssueKey} --transition \"<target-status>\"", _options.IssueKey);

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting workflow help: {Message}", ex.Message);
                return false;
            }
        }

        public override bool ValidateParameters()
        {
            if (string.IsNullOrEmpty(_options.IssueKey))
            {
                _logger?.LogError("Error: Issue key is required for workflow help.");
                return false;
            }

            return true;
        }
    }
}
