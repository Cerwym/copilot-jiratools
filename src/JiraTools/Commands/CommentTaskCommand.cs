using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JiraTools.Commands
{
    /// <summary>
    /// Command to comment on a task referenced in a status document
    /// </summary>
    public class CommentTaskCommand : BaseCommand
    {
        public CommentTaskCommand(IJiraClient jiraClient, CommandLineOptions options, ILogger logger = null) 
            : base(jiraClient, options, logger)
        {
        }

        public override string CommandName => "comment-task";

        public override string Description => "Comment on a task referenced in a status document";

        public override async Task<bool> ExecuteAsync()
        {
            try
            {
                // Default path to the status document
                string documentPath = _options.StatusDocPath ?? Path.Combine(
                    Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName,
                    "docs",
                    "status.md");

                if (!File.Exists(documentPath))
                {
                    _logger?.LogError("Error: Status document not found at {DocumentPath}", documentPath);
                    if (_options.NonInteractive)
                    {
                        _logger?.LogError("Running in non-interactive mode, cannot prompt for status document path.");
                        return false;
                    }
                    documentPath = PromptForInput("Enter the path to the status document");
                    if (!File.Exists(documentPath))
                    {
                        _logger?.LogError("Error: Document not found at {DocumentPath}", documentPath);
                        return false;
                    }
                }

                // Read the status document
                _logger?.LogInformation("Reading status document from: {DocumentPath}", documentPath);
                var lines = File.ReadAllLines(documentPath);

                // Find the table header line (contains "| Project | Status |")
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
                    _logger?.LogError("Error: Could not find the status table in the document.");
                    return false;
                }

                // Check if the table has a Comments column
                if (!lines[tableHeaderIndex].Contains("Comments"))
                {
                    _logger?.LogError("Error: The status table doesn't have a Comments column. Please add one first.");
                    return false;
                }

                // Parse table structure to find column indices
                var headerCells = lines[tableHeaderIndex].Split('|');
                int projectColIndex = -1;
                int jiraTaskColIndex = -1;
                int commentsColIndex = -1;

                for (int i = 0; i < headerCells.Length; i++)
                {
                    string cell = headerCells[i].Trim();
                    if (cell.Equals("Project", StringComparison.OrdinalIgnoreCase))
                    {
                        projectColIndex = i;
                    }
                    else if (cell.Equals("Jira Task", StringComparison.OrdinalIgnoreCase))
                    {
                        jiraTaskColIndex = i;
                    }
                    else if (cell.Equals("Comments", StringComparison.OrdinalIgnoreCase))
                    {
                        commentsColIndex = i;
                    }
                }

                if (projectColIndex == -1 || commentsColIndex == -1)
                {
                    _logger?.LogError("Error: Could not find the Project or Comments column in the table.");
                    return false;
                }

                // Parse projects from the table
                var projectData = ParseProjectsFromTable(lines, tableHeaderIndex, projectColIndex, jiraTaskColIndex, commentsColIndex);

                if (projectData.Count == 0)
                {
                    _logger?.LogWarning("No projects found in the status table.");
                    return false;
                }

                // Select a project
                int selectedProjectIndex = SelectProject(projectData);
                if (selectedProjectIndex < 0)
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

                // Update the document and Jira
                await UpdateDocumentAndJira(documentPath, lines, projectData[selectedProjectIndex]);

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error commenting on task: {Message}", ex.Message);
                return false;
            }
        }

        private class ProjectData
        {
            public string Name { get; set; }
            public string JiraTask { get; set; }
            public int LineIndex { get; set; }
        }

        private List<ProjectData> ParseProjectsFromTable(string[] lines, int tableHeaderIndex, int projectColIndex, int jiraTaskColIndex, int commentsColIndex)
        {
            var projects = new List<ProjectData>();

            // Start from the line after the header and separator
            for (int i = tableHeaderIndex + 2; i < lines.Length; i++)
            {
                string line = lines[i];
                if (!line.StartsWith("|") || line.Trim() == "")
                {
                    break; // End of table
                }

                var cells = line.Split('|');
                if (cells.Length > Math.Max(projectColIndex, commentsColIndex))
                {
                    string project = cells[projectColIndex].Trim();
                    string jiraTask = jiraTaskColIndex != -1 && cells.Length > jiraTaskColIndex ?
                        cells[jiraTaskColIndex].Trim() : "N/A";

                    projects.Add(new ProjectData
                    {
                        Name = project,
                        JiraTask = jiraTask,
                        LineIndex = i
                    });
                }
            }

            return projects;
        }

        private int SelectProject(List<ProjectData> projects)
        {
            // List available projects
            _logger?.LogInformation("");
            _logger?.LogInformation("Available projects:");
            for (int i = 0; i < projects.Count; i++)
            {
                var project = projects[i];
                _logger?.LogInformation("{Index}. {ProjectName} (Jira: {JiraTask})", i + 1, project.Name, project.JiraTask);
            }

            int selectedProjectIndex = -1;
            if (!string.IsNullOrEmpty(_options.ProjectKey))
            {
                for (int i = 0; i < projects.Count; i++)
                {
                    if (projects[i].Name.Equals(_options.ProjectKey, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedProjectIndex = i;
                        break;
                    }
                }

                if (selectedProjectIndex == -1)
                {
                    _logger?.LogWarning("Project '{ProjectKey}' not found in the status table.", _options.ProjectKey);
                }
            }

            if (selectedProjectIndex == -1)
            {
                if (_options.NonInteractive)
                {
                    _logger?.LogError("No matching project found and running in non-interactive mode.");
                    return -1;
                }
                selectedProjectIndex = SelectFromList(projects, "Select project", p => $"{p.Name} (Jira: {p.JiraTask})", _logger);
            }

            return selectedProjectIndex;
        }

        private async Task UpdateDocumentAndJira(string documentPath, string[] lines, ProjectData selectedProject)
        {
            // Format the comment
            string datePrefix = $"[{DateTime.Now:yyyy-MM-dd}] ";
            string formattedComment = datePrefix + _options.Comment.Replace("\n", " ");

            // Update the document with the comment
            var rowCells = lines[selectedProject.LineIndex].Split('|');

            // Find the comments column index (we already validated it exists)
            var headerCells = lines[Array.FindIndex(lines, line => line.Contains("| Project | Status |"))].Split('|');
            int commentsColIndex = -1;
            for (int i = 0; i < headerCells.Length; i++)
            {
                if (headerCells[i].Trim().Equals("Comments", StringComparison.OrdinalIgnoreCase))
                {
                    commentsColIndex = i;
                    break;
                }
            }

            // Update the comments cell
            if (rowCells.Length > commentsColIndex)
            {
                string existingComment = rowCells[commentsColIndex].Trim();
                rowCells[commentsColIndex] = existingComment.Length > 0 ?
                    $" {existingComment}; {formattedComment} " :
                    $" {formattedComment} ";
            }
            else
            {
                // Need to extend the cells array
                var newCells = new string[commentsColIndex + 2]; // +2 because we need one after for the closing |
                Array.Copy(rowCells, newCells, rowCells.Length);
                for (int i = rowCells.Length; i < newCells.Length - 1; i++)
                {
                    newCells[i] = " ";
                }
                newCells[commentsColIndex] = $" {formattedComment} ";
                rowCells = newCells;
            }

            // Reconstruct the line
            lines[selectedProject.LineIndex] = string.Join("|", rowCells);

            // Write the updated document
            File.WriteAllLines(documentPath, lines);
            _logger?.LogInformation("Updated status document with comment for project '{ProjectName}'", selectedProject.Name);

            // If there's an associated Jira task, also add the comment there
            if (selectedProject.JiraTask != "N/A" && !string.IsNullOrEmpty(selectedProject.JiraTask) && 
                selectedProject.JiraTask.Contains("-"))
            {
                _logger?.LogInformation("Adding comment to Jira task {JiraTask}...", selectedProject.JiraTask);
                try
                {
                    // Add a reference to the document in the Jira comment
                    string jiraComment = $"{_options.Comment}\n\n_This comment was added via JiraTools from the status document._";
                    await _jiraClient.AddCommentAsync(selectedProject.JiraTask, jiraComment);
                    _logger?.LogInformation("Comment added to Jira task: {JiraUrl}/browse/{JiraTask}", _options.JiraUrl, selectedProject.JiraTask);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Warning: Failed to add comment to Jira task: {Message}", ex.Message);
                }
            }
        }

        public override bool ValidateParameters()
        {
            return true; // This command has flexible parameters
        }
    }
}
