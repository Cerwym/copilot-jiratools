#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JiraTools.Configuration
{
    /// <summary>
    /// Configuration provider that reads from markdown tables (backwards compatibility)
    /// </summary>
    public class MarkdownProjectConfigurationProvider : IProjectConfigurationProvider
    {
        private readonly ILogger? _logger;
        private readonly string _defaultPath;

        public MarkdownProjectConfigurationProvider(ILogger? logger = null, string? defaultPath = null)
        {
            _logger = logger;
            _defaultPath = defaultPath ?? Path.Combine(
                Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.FullName ?? Directory.GetCurrentDirectory(),
                "docs",
                "status.md");
        }

        public async Task<ProjectConfiguration> LoadAsync(string? configPath = null)
        {
            var filePath = configPath ?? _defaultPath;
            
            if (!File.Exists(filePath))
            {
                _logger?.LogWarning("Markdown configuration file not found at {FilePath}", filePath);
                return new ProjectConfiguration();
            }

            try
            {
                var lines = await File.ReadAllLinesAsync(filePath);
                return ParseMarkdownTable(lines);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error reading markdown configuration from {FilePath}", filePath);
                throw new InvalidOperationException($"Failed to load markdown configuration from {filePath}", ex);
            }
        }

        public async Task SaveAsync(ProjectConfiguration configuration, string? configPath = null)
        {
            var filePath = configPath ?? _defaultPath;
            
            try
            {
                var markdownContent = GenerateMarkdownTable(configuration);
                await File.WriteAllTextAsync(filePath, markdownContent);
                _logger?.LogInformation("Saved configuration to markdown file at {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving markdown configuration to {FilePath}", filePath);
                throw new InvalidOperationException($"Failed to save markdown configuration to {filePath}", ex);
            }
        }

        public Task<bool> ExistsAsync(string? configPath = null)
        {
            var filePath = configPath ?? _defaultPath;
            return Task.FromResult(File.Exists(filePath));
        }

        public string GetDefaultConfigPath()
        {
            return _defaultPath;
        }

        private ProjectConfiguration ParseMarkdownTable(string[] lines)
        {
            var configuration = new ProjectConfiguration();

            // Find the table header line
            int tableHeaderIndex = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("| Project | Status |"))
                {
                    tableHeaderIndex = i;
                    break;
                }
            }

            if (tableHeaderIndex == -1)
            {
                _logger?.LogWarning("No project table found in markdown file");
                return configuration;
            }

            // Parse table structure to find column indices
            var headerCells = lines[tableHeaderIndex].Split('|').Select(c => c.Trim()).ToArray();
            int projectColIndex = Array.FindIndex(headerCells, c => c.Equals("Project", StringComparison.OrdinalIgnoreCase));
            int statusColIndex = Array.FindIndex(headerCells, c => c.Equals("Status", StringComparison.OrdinalIgnoreCase));
            int jiraTaskColIndex = Array.FindIndex(headerCells, c => c.Equals("Jira Task", StringComparison.OrdinalIgnoreCase));
            int commentsColIndex = Array.FindIndex(headerCells, c => c.Equals("Comments", StringComparison.OrdinalIgnoreCase));

            if (projectColIndex == -1)
            {
                _logger?.LogError("Project column not found in markdown table");
                return configuration;
            }

            // Parse projects from the table (skip header and separator lines)
            for (int i = tableHeaderIndex + 2; i < lines.Length; i++)
            {
                string line = lines[i];
                if (!line.StartsWith("|") || string.IsNullOrWhiteSpace(line))
                {
                    break; // End of table
                }

                var cells = line.Split('|').Select(c => c.Trim()).ToArray();
                if (cells.Length <= projectColIndex)
                    continue;

                string projectName = cells[projectColIndex];
                if (string.IsNullOrWhiteSpace(projectName))
                    continue;

                var project = new ProjectInfo
                {
                    Id = GenerateProjectId(projectName),
                    Name = projectName,
                    JiraTaskId = jiraTaskColIndex != -1 && cells.Length > jiraTaskColIndex ? cells[jiraTaskColIndex] : "N/A",
                    Status = ParseStatus(statusColIndex != -1 && cells.Length > statusColIndex ? cells[statusColIndex] : "ToDo")
                };

                // Parse comments if available
                if (commentsColIndex != -1 && cells.Length > commentsColIndex)
                {
                    var commentsText = cells[commentsColIndex];
                    if (!string.IsNullOrWhiteSpace(commentsText))
                    {
                        project.AddComment(commentsText, "markdown-import");
                    }
                }

                configuration.AddProject(project);
            }

            return configuration;
        }

        private string GenerateMarkdownTable(ProjectConfiguration configuration)
        {
            var lines = new List<string>
            {
                "# Project Status",
                "",
                "| Project | Status | Jira Task | Comments |",
                "|---------|--------|-----------|----------|"
            };

            foreach (var project in configuration.Projects)
            {
                var status = project.Status.ToString().Replace("InProgress", "In Progress");
                var comments = project.GetCommentsAsString();
                
                lines.Add($"| {project.Name} | {status} | {project.JiraTaskId} | {comments} |");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private ProjectStatus ParseStatus(string statusText)
        {
            return statusText.ToLowerInvariant() switch
            {
                "to do" or "todo" => ProjectStatus.ToDo,
                "in progress" or "inprogress" or "in-progress" => ProjectStatus.InProgress,
                "review" => ProjectStatus.Review,
                "done" or "completed" => ProjectStatus.Done,
                "blocked" => ProjectStatus.Blocked,
                _ => ProjectStatus.ToDo
            };
        }

        private string GenerateProjectId(string projectName)
        {
            return projectName.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("_", "-")
                .Trim('-');
        }
    }
}
