using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JiraTools.Commands
{
    /// <summary>
    /// Command to add a comment to a Jira task
    /// </summary>
    public class AddCommentCommand : BaseCommand
    {
        public AddCommentCommand(IJiraClient jiraClient, CommandLineOptions options, ILogger logger = null) 
            : base(jiraClient, options, logger)
        {
        }

        public override string CommandName => "add-comment";

        public override string Description => "Add a comment to a Jira task";

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

                if (string.IsNullOrEmpty(_options.Comment))
                {
                    _options.Comment = PromptForMultiLineInput("Enter comment");
                }

                if (string.IsNullOrEmpty(_options.Comment))
                {
                    _logger?.LogError("Error: Comment text is required.");
                    return false;
                }

                await _jiraClient.AddCommentAsync(_options.IssueKey, _options.Comment);

                _logger?.LogInformation("Added comment to issue: {JiraUrl}/browse/{IssueKey}", _options.JiraUrl, _options.IssueKey);

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding comment: {Message}", ex.Message);
                return false;
            }
        }

        public override bool ValidateParameters()
        {
            if (string.IsNullOrEmpty(_options.IssueKey) && string.IsNullOrEmpty(_options.Comment))
            {
                _logger?.LogError("Error: Issue key and comment text are required.");
                return false;
            }

            return true;
        }
    }
}
