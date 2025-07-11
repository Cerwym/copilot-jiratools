using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JiraTools.Commands
{
    /// <summary>
    /// Command to update an existing Jira task
    /// </summary>
    public class UpdateTaskCommand : BaseCommand
    {
        public UpdateTaskCommand(IJiraClient jiraClient, CommandLineOptions options, ILogger logger = null) 
            : base(jiraClient, options, logger)
        {
        }

        public override string CommandName => "update-task";

        public override string Description => "Update an existing Jira task";

        public override async Task<bool> ExecuteAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_options.IssueKey))
                {
                    _options.IssueKey = PromptForInput("Enter issue key (e.g., PROJ-12345)");
                }

                if (string.IsNullOrEmpty(_options.IssueKey))
                {
                    _logger?.LogError("Error: Issue key is required.");
                    return false;
                }

                var fields = new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(_options.Summary))
                {
                    fields["summary"] = _options.Summary;
                }

                if (!string.IsNullOrEmpty(_options.Description))
                {
                    fields["description"] = _options.Description;
                }

                if (fields.Count == 0)
                {
                    _logger?.LogWarning("No fields to update. Specify --summary or --description.");
                    return false;
                }

                await _jiraClient.UpdateIssueAsync(_options.IssueKey, fields);

                _logger?.LogInformation("Updated issue: {JiraUrl}/browse/{IssueKey}", _options.JiraUrl, _options.IssueKey);

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating task: {Message}", ex.Message);
                return false;
            }
        }

        public override bool ValidateParameters()
        {
            if (string.IsNullOrEmpty(_options.IssueKey) && 
                string.IsNullOrEmpty(_options.Summary) && 
                string.IsNullOrEmpty(_options.Description))
            {
                _logger?.LogError("Error: Issue key and at least one field to update (summary or description) are required.");
                return false;
            }

            return true;
        }
    }
}
