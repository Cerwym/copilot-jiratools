#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace JiraTools.Configuration
{
    /// <summary>
    /// Represents a project with its associated metadata and tasks
    /// </summary>
    public class ProjectInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string JiraTaskId { get; set; } = string.Empty;
        public ProjectStatus Status { get; set; } = ProjectStatus.ToDo;
        public string Priority { get; set; } = "medium";
        public string Assignee { get; set; } = string.Empty;
        public List<ProjectComment> Comments { get; set; } = new();
        public Dictionary<string, object> CustomFields { get; set; } = new();

        public ProjectInfo()
        {
        }

        public ProjectInfo(string id, string name, string jiraTaskId, ProjectStatus status = ProjectStatus.ToDo)
        {
            Id = id;
            Name = name;
            JiraTaskId = jiraTaskId;
            Status = status;
        }

        /// <summary>
        /// Add a new comment to the project
        /// </summary>
        public void AddComment(string text, string author)
        {
            Comments.Add(new ProjectComment(text, author));
        }

        /// <summary>
        /// Get the most recent comment
        /// </summary>
        public ProjectComment? GetLatestComment()
        {
            return Comments.OrderByDescending(c => c.Date).FirstOrDefault();
        }

        /// <summary>
        /// Get comments formatted as a string
        /// </summary>
        public string GetCommentsAsString()
        {
            if (!Comments.Any())
                return string.Empty;

            return string.Join("; ", Comments.Select(c => c.ToString()));
        }

        public override string ToString()
        {
            return $"{Name} ({JiraTaskId}) - {Status}";
        }
    }
}
