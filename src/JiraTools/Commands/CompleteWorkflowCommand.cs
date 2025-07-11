using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JiraTools.Commands
{
    /// <summary>
    /// Command to execute a complete workflow to transition an issue to a target status
    /// </summary>
    public class CompleteWorkflowCommand : BaseCommand
    {
        public CompleteWorkflowCommand(IJiraClient jiraClient, CommandLineOptions options, ILogger logger = null) 
            : base(jiraClient, options, logger)
        {
        }

        public override string CommandName => "complete";

        public override string Description => "Auto-complete workflow to target status (default: Done)";

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
                
                // Get target status
                var targetStatus = _options.TransitionName ?? "Done";
                if (string.IsNullOrEmpty(_options.TransitionName) && !_options.NonInteractive)
                {
                    targetStatus = PromptForInput("Enter target status", "Done");
                }

                _logger?.LogInformation("Finding workflow path to '{TargetStatus}'...", targetStatus);
                var workflowPath = await discovery.GetWorkflowPathAsync(_options.IssueKey, targetStatus);

                if (workflowPath != null && workflowPath.Steps.Any())
                {
                    var success = await discovery.ExecuteWorkflowAsync(_options.IssueKey, workflowPath, 
                        !_options.NonInteractive && !_options.SkipConfirmation);
                    
                    if (success)
                    {
                        _logger?.LogInformation("Successfully completed workflow to '{TargetStatus}'!", targetStatus);
                        return true;
                    }
                    else
                    {
                        _logger?.LogError("Failed to complete workflow to '{TargetStatus}'.", targetStatus);
                        return false;
                    }
                }
                else
                {
                    _logger?.LogWarning("No workflow path found to '{TargetStatus}'. Try discovering the workflow first:", targetStatus);
                    _logger?.LogInformation("  jiratools discover-workflow --issue-key {IssueKey} --transition \"{TargetStatus}\"", _options.IssueKey, targetStatus);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error completing workflow: {Message}", ex.Message);
                return false;
            }
        }

        public override bool ValidateParameters()
        {
            if (string.IsNullOrEmpty(_options.IssueKey))
            {
                _logger?.LogError("Error: Issue key is required for workflow completion.");
                return false;
            }

            return true;
        }
    }
}
