using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace JiraTools.Commands
{
    /// <summary>
    /// Command to create a new Jira task
    /// </summary>
    public class CreateTaskCommand : BaseCommand
    {
        public CreateTaskCommand(IJiraClient jiraClient, CommandLineOptions options, ILogger logger = null)
            : base(jiraClient, options, logger)
        {
        }

        public override string CommandName => "create-task";

        public override string Description => "Create a new Jira task";

        public override async Task<bool> ExecuteAsync()
        {
            try
            {
                // Get required fields for this project and issue type
                _logger?.LogInformation("Checking for required fields...");
                var requiredFields = await _jiraClient.GetRequiredFieldsAsync(_options.ProjectKey, _options.IssueType ?? "Task");

                if (requiredFields.Count > 0)
                {
                    _logger?.LogInformation("Required fields for this issue type:");
                    foreach (var field in requiredFields)
                    {
                        _logger?.LogInformation("- {FieldName} ({FieldKey})", field.Value, field.Key);
                    }
                }

                // Handle Work Classification field if required
                if (!await HandleWorkClassificationField(requiredFields))
                {
                    return false;
                }

                // Handle components
                var validComponents = await HandleComponentSelection();

                // Get summary and description if not provided
                if (!GetSummaryAndDescription())
                {
                    return false;
                }

                // Create the issue
                await CreateIssue(validComponents, requiredFields);

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogCommandFailure(CommandName, ex.Message);
                return false;
            }
        }

        private async Task<bool> HandleWorkClassificationField(Dictionary<string, string> requiredFields)
        {
            string workClassificationFieldId = "customfield_18333"; // From the error message
            if (requiredFields.ContainsKey(workClassificationFieldId) && string.IsNullOrEmpty(_options.WorkClassification))
            {
                // Get allowed values for Work Classification
                var allowedValues = await _jiraClient.GetAllowedValuesForFieldAsync(
                    _options.ProjectKey,
                    _options.IssueType ?? "Task",
                    workClassificationFieldId
                );

                _logger?.LogInformation("Work Classification is required. Allowed values:");
                if (_options.NonInteractive)
                {
                    _logger?.LogError("Work Classification is required but running in non-interactive mode.");
                    return false;
                }
                int selectedIndex = SelectFromList(allowedValues, "Select Work Classification", null, _logger);

                if (selectedIndex >= 0)
                {
                    _options.WorkClassification = allowedValues[selectedIndex];
                    _logger?.LogInformation("Selected: {WorkClassification}", _options.WorkClassification);
                }
                else
                {
                    _logger?.LogWarning("Invalid selection. Using default 'Maintenance'");
                    _options.WorkClassification = "Maintenance";
                }
            }

            return true;
        }

        private async Task<List<string>> HandleComponentSelection()
        {
            // Get available components for the project
            _logger?.LogInformation("Fetching available components for project {ProjectKey}...", _options.ProjectKey);
            var availableComponents = await _jiraClient.GetAvailableComponentsAsync(_options.ProjectKey);

            // Display available components
            if (availableComponents.Count > 0)
            {
                _logger?.LogInformation("Available components:");
                int index = 1;
                var componentsList = new List<string>(availableComponents.Keys);
                foreach (var component in componentsList)
                {
                    _logger?.LogInformation("{Index}. {Component}", index, component);
                    index++;
                }
            }
            else
            {
                _logger?.LogWarning("No components available for this project or you don't have permission to view them.");
            }

            // Process requested components
            List<string> validComponents = new List<string>();
            if (!string.IsNullOrEmpty(_options.Components))
            {
                string[] requestedComponents = _options.Components.Split(',');
                foreach (var component in requestedComponents)
                {
                    string trimmedComponent = component.Trim();
                    if (availableComponents.ContainsKey(trimmedComponent))
                    {
                        validComponents.Add(trimmedComponent);
                    }
                    else
                    {
                        _logger?.LogWarning("Component '{TrimmedComponent}' is not available in project {ProjectKey}. Skipping.", trimmedComponent, _options.ProjectKey);
                    }
                }
            }

            if (validComponents.Count == 0 && !string.IsNullOrEmpty(_options.Components))
            {
                if (_options.NonInteractive)
                {
                    _logger?.LogWarning("No valid components found and running in non-interactive mode. Skipping component selection.");
                    return validComponents;
                }
                _logger?.LogInformation("No valid components were found. You can specify components by number:");
                _logger?.LogInformation("Enter component numbers (comma-separated, e.g., '1,3'): ");
                string input = Console.ReadLine();
                if (!string.IsNullOrEmpty(input))
                {
                    string[] selections = input.Split(',');
                    var componentsList = new List<string>(availableComponents.Keys);
                    foreach (var selection in selections)
                    {
                        if (int.TryParse(selection.Trim(), out int selectedIndex) &&
                            selectedIndex > 0 && selectedIndex <= componentsList.Count)
                        {
                            validComponents.Add(componentsList[selectedIndex - 1]);
                        }
                    }
                }
            }

            return validComponents;
        }

        private bool GetSummaryAndDescription()
        {
            if (string.IsNullOrEmpty(_options.Summary))
            {
                if (_options.NonInteractive)
                {
                    _logger?.LogError("Summary is required but not provided in non-interactive mode.");
                    return false;
                }
                _options.Summary = PromptForInput("Enter task summary");
            }

            if (string.IsNullOrEmpty(_options.Description))
            {
                if (_options.NonInteractive)
                {
                    _logger?.LogError("Description is required but not provided in non-interactive mode.");
                    return false;
                }
                _options.Description = PromptForMultiLineInput("Enter task description");
            }

            return true;
        }

        private async Task CreateIssue(List<string> validComponents, Dictionary<string, string> requiredFields)
        {
            // Prepare components for the call
            string[] components = validComponents.Count > 0 ? validComponents.ToArray() : null;

            // Use the IJiraClient interface instead of direct HTTP calls
            string issueKey = await _jiraClient.CreateIssueAsync(
                _options.ProjectKey,
                _options.Summary,
                _options.Description,
                _options.IssueType ?? "Task",
                components
            );

            _logger?.LogCommandSuccess(CommandName, $"Issue created: {_options.JiraUrl}/browse/{issueKey}");

            // Link to parent task if needed
            await HandleParentLinking(issueKey);
        }

        private async Task HandleParentLinking(string issueKey)
        {
            if (_options.LinkToParent)
            {
                var parentTask = _options.ParentTask ?? GetParentTaskFromEnvironment();
                if (!string.IsNullOrEmpty(parentTask))
                {
                    _logger?.LogInformation("Linking to parent task {ParentTask}...", parentTask);
                    bool linked = false;

                    try
                    {
                        // First try to create a "Relates to" link
                        linked = await _jiraClient.CreateIssueLinkAsync(issueKey, parentTask, "Relates");

                        if (linked)
                        {
                            _logger?.LogInformation("Successfully linked issue to parent {ParentTask}", parentTask);
                        }
                        else
                        {
                            // If that fails, try to set as a subtask
                            _logger?.LogInformation("Failed to create standard link, trying subtask relationship...");
                            linked = await _jiraClient.SetParentTaskAsync(issueKey, parentTask);

                            if (linked)
                            {
                                _logger?.LogInformation("Successfully set {IssueKey} as subtask of {ParentTask}", issueKey, parentTask);
                            }
                            else
                            {
                                _logger?.LogWarning("Could not link task to parent {ParentTask}", parentTask);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error linking tasks: {Message}", ex.Message);
                    }

                    _logger?.LogInformation("Parent URL: {JiraUrl}/browse/{ParentTask}", _options.JiraUrl, parentTask);
                }
                else
                {
                    _logger?.LogWarning("--parent option specified but no parent task configured.");
                    _logger?.LogWarning("Set PARENT_TASK=PROJ-123 in your .env file or use --parent-task option.");
                }
            }
        }

        private string GetParentTaskFromEnvironment()
        {
            // This would be set from the environment constants
            return "";
        }

        public override bool ValidateParameters()
        {
            if (string.IsNullOrEmpty(_options.ProjectKey))
            {
                _logger?.LogError("Project key is required for creating tasks.");
                return false;
            }

            return true;
        }
    }
}
