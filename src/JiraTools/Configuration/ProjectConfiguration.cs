#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace JiraTools.Configuration
{
    /// <summary>
    /// Represents the complete project configuration
    /// </summary>
    public class ProjectConfiguration
    {
        public List<ProjectInfo> Projects { get; set; } = new();
        public Dictionary<string, object> Settings { get; set; } = new();

        public ProjectConfiguration()
        {
        }

        public ProjectConfiguration(IEnumerable<ProjectInfo> projects)
        {
            Projects = projects.ToList();
        }

        /// <summary>
        /// Find a project by ID
        /// </summary>
        public ProjectInfo? FindProjectById(string id)
        {
            return Projects.FirstOrDefault(p => p.Id.Equals(id, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Find a project by name
        /// </summary>
        public ProjectInfo? FindProjectByName(string name)
        {
            return Projects.FirstOrDefault(p => p.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Find a project by Jira task ID
        /// </summary>
        public ProjectInfo? FindProjectByJiraTaskId(string jiraTaskId)
        {
            return Projects.FirstOrDefault(p => p.JiraTaskId.Equals(jiraTaskId, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get projects filtered by status
        /// </summary>
        public IEnumerable<ProjectInfo> GetProjectsByStatus(ProjectStatus status)
        {
            return Projects.Where(p => p.Status == status);
        }

        /// <summary>
        /// Add a new project
        /// </summary>
        public void AddProject(ProjectInfo project)
        {
            Projects.Add(project);
        }

        /// <summary>
        /// Remove a project by ID
        /// </summary>
        public bool RemoveProject(string id)
        {
            var project = FindProjectById(id);
            if (project != null)
            {
                Projects.Remove(project);
                return true;
            }
            return false;
        }
    }
}
