using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JiraTools.Commands
{
    /// <summary>
    /// Command to discover and cache workflow paths for an issue
    /// </summary>
    public class DiscoverWorkflowCommand : BaseCommand
    {
        public DiscoverWorkflowCommand(IJiraClient jiraClient, CommandLineOptions options, ILogger logger = null) 
            : base(jiraClient, options, logger)
        {
        }

        public override string CommandName => "discover-workflow";

        public override string Description => "Discover and cache workflow paths for an issue";

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
                    _options.IssueKey = PromptForInput("Enter issue key to discover workflow from");
                }

                if (string.IsNullOrEmpty(_options.IssueKey))
                {
                    _logger?.LogError("Issue key is required for workflow discovery.");
                    return false;
                }

                var discovery = new WorkflowDiscovery(_jiraClient, _options.ProjectKey, _logger);
                
                // Get target status from user if not provided
                var targetStatus = _options.TransitionName ?? "Done";
                if (string.IsNullOrEmpty(_options.TransitionName))
                {
                    if (_options.NonInteractive)
                    {
                        _logger?.LogInformation("Using default target status 'Done' in non-interactive mode.");
                        targetStatus = "Done";
                    }
                    else
                    {
                        targetStatus = PromptForInput("Enter target status", "Done");
                    }
                }

                _logger?.LogInformation("Discovering workflow path to '{TargetStatus}'...", targetStatus);
                var workflowPath = await discovery.DiscoverWorkflowAsync(_options.IssueKey, targetStatus);

                if (workflowPath != null && workflowPath.Steps.Any())
                {
                    _logger?.LogInformation("Discovered workflow path ({StepCount} steps):", workflowPath.Steps.Count);
                    for (int i = 0; i < workflowPath.Steps.Count; i++)
                    {
                        var step = workflowPath.Steps[i];
                        _logger?.LogInformation("  {StepNumber}. {FromStatus} → {ToStatus} (via '{TransitionName}')", i + 1, step.FromStatus, step.ToStatus, step.TransitionName);
                    }
                    
                    _logger?.LogInformation("Workflow cached for future use. You can now use:");
                    _logger?.LogInformation("  jiratools complete --issue-key {IssueKey} --target {TargetStatus}", _options.IssueKey, targetStatus);

                    return true;
                }
                else
                {
                    _logger?.LogWarning("Could not discover a workflow path to the target status.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error discovering workflow: {Message}", ex.Message);
                return false;
            }
        }

        public override bool ValidateParameters()
        {
            if (string.IsNullOrEmpty(_options.IssueKey))
            {
                _logger?.LogError("Error: Issue key is required for workflow discovery.");
                return false;
            }

            return true;
        }
    }
}
