#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using JiraTools.Configuration;

namespace JiraTools.Commands
{
    /// <summary>
    /// Command to comment on a task referenced in a project configuration
    /// </summary>
    public class CommentTaskCommand : BaseCommand
    {
        private readonly IProjectConfigurationProvider _configurationProvider;

        public CommentTaskCommand(IJiraClient jiraClient,
                                 IProjectConfigurationProvider configurationProvider,
                                 CommandLineOptions options,
                                 ILogger? logger = null)
            : base(jiraClient, options, logger)
        {
            _configurationProvider = configurationProvider ?? throw new ArgumentNullException(nameof(configurationProvider));
        }

        // Backward compatibility constructor for existing code
        public CommentTaskCommand(IJiraClient jiraClient, CommandLineOptions options, ILogger? logger = null)
            : this(jiraClient, ProjectConfigurationProviderFactory.CreateProvider(options?.StatusDocPath, logger), options ?? throw new ArgumentNullException(nameof(options)), logger)
        {
        }

        public override string CommandName => "comment-task";

        public override string Description => "Comment on a task referenced in a status document";

        public override async Task<bool> ExecuteAsync()
        {
            try
            {
                _logger?.LogInformation("Loading project configuration...");

                // Load configuration using our new provider system
                var configuration = await _configurationProvider.LoadAsync(_options.StatusDocPath);

                if (configuration.Projects.Count == 0)
                {
                    _logger?.LogWarning("No projects found in configuration.");

                    // If no projects found, check if configuration exists
                    if (!await _configurationProvider.ExistsAsync(_options.StatusDocPath))
                    {
                        _logger?.LogError("Configuration file not found at {ConfigPath}",
                            _options.StatusDocPath ?? _configurationProvider.GetDefaultConfigPath());

                        if (_options.NonInteractive)
                        {
                            _logger?.LogError("Running in non-interactive mode, cannot prompt for configuration path.");
                            return false;
                        }

                        // Could add interactive config creation here in the future
                        _logger?.LogError("Please create a configuration file or use markdown table format.");
                        return false;
                    }
                    return false;
                }

                _logger?.LogInformation("Found {ProjectCount} projects in configuration", configuration.Projects.Count);

                // Select a project using our type-safe configuration
                var selectedProject = SelectProject(configuration.Projects);
                if (selectedProject == null)
                {
                    return false;
                }

                // Get the comment text
                if (string.IsNullOrEmpty(_options.Comment))
                {
                    if (_options.NonInteractive)
                    {
                        _logger?.LogError("No comment provided and running in non-interactive mode.");
                        return false;
                    }
                    _options.Comment = PromptForMultiLineInput("Enter comment");
                }

                if (string.IsNullOrEmpty(_options.Comment))
                {
                    _logger?.LogWarning("Comment is empty. Operation cancelled.");
                    return false;
                }

                // Add comment to project and update configuration
                await UpdateProjectAndConfiguration(selectedProject, configuration);

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error commenting on task: {Message}", ex.Message);
                return false;
            }
        }

        private ProjectInfo? SelectProject(IList<ProjectInfo> projects)
        {
            // List available projects
            _logger?.LogInformation("");
            _logger?.LogInformation("Available projects:");
            for (int i = 0; i < projects.Count; i++)
            {
                var project = projects[i];
                _logger?.LogInformation("{Index}. {ProjectName} (Jira: {JiraTask}) - {Status}",
                    i + 1, project.Name, project.JiraTaskId, project.Status);
            }

            ProjectInfo? selectedProject = null;
            if (!string.IsNullOrEmpty(_options.ProjectKey))
            {
                selectedProject = projects.FirstOrDefault(p =>
                    p.Name.Equals(_options.ProjectKey, StringComparison.OrdinalIgnoreCase) ||
                    p.Id.Equals(_options.ProjectKey, StringComparison.OrdinalIgnoreCase));

                if (selectedProject == null)
                {
                    _logger?.LogWarning("Project '{ProjectKey}' not found in configuration.", _options.ProjectKey);
                }
            }

            if (selectedProject == null)
            {
                if (_options.NonInteractive)
                {
                    _logger?.LogError("No matching project found and running in non-interactive mode.");
                    return null;
                }

                var projectIndex = SelectFromList(projects.ToList(), "Select project",
                    p => $"{p.Name} (Jira: {p.JiraTaskId}) - {p.Status}", _logger);

                if (projectIndex >= 0 && projectIndex < projects.Count)
                {
                    selectedProject = projects[projectIndex];
                }
            }

            return selectedProject;
        }

        private async Task UpdateProjectAndConfiguration(ProjectInfo selectedProject, ProjectConfiguration configuration)
        {
            // Format the comment
            string datePrefix = $"[{DateTime.Now:yyyy-MM-dd}] ";
            string formattedComment = datePrefix + _options.Comment.Replace("\n", " ");

            // Add comment to the project
            selectedProject.AddComment(_options.Comment, Environment.UserName);
            _logger?.LogInformation("Added comment to project '{ProjectName}': {Comment}",
                selectedProject.Name, formattedComment);

            // Save the updated configuration
            try
            {
                await _configurationProvider.SaveAsync(configuration, _options.StatusDocPath);
                _logger?.LogInformation("Updated project configuration with comment for project '{ProjectName}'", selectedProject.Name);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Warning: Failed to save configuration: {Message}", ex.Message);
            }

            // If there's an associated Jira task, also add the comment there
            if (!string.IsNullOrEmpty(selectedProject.JiraTaskId) &&
                selectedProject.JiraTaskId != "N/A" &&
                selectedProject.JiraTaskId.Contains("-"))
            {
                _logger?.LogInformation("Adding comment to Jira task {JiraTask}...", selectedProject.JiraTaskId);
                try
                {
                    // Add a reference to the configuration in the Jira comment
                    string jiraComment = $"{_options.Comment}\n\n_This comment was added via JiraTools from the project configuration._";
                    await _jiraClient.AddCommentAsync(selectedProject.JiraTaskId, jiraComment);
                    _logger?.LogInformation("Comment added to Jira task: {JiraUrl}/browse/{JiraTask}",
                        _options.JiraUrl, selectedProject.JiraTaskId);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Warning: Failed to add comment to Jira task: {Message}", ex.Message);
                }
            }
        }

        // ...existing code...

        public override bool ValidateParameters()
        {
            return true; // This command has flexible parameters
        }
    }
}
