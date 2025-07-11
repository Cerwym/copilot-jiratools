using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JiraTools
{
    /// <summary>
    /// Interface for interacting with Jira API
    /// </summary>
    public interface IJiraClient
    {
        /// <summary>
        /// Creates a new issue in Jira
        /// </summary>
        /// <param name="projectKey">Project key (e.g., "NC" for N-Central)</param>
        /// <param name="issueType">Type of issue (e.g., "Task", "Bug")</param>
        /// <param name="summary">Issue summary/title</param>
        /// <param name="description">Issue description</param>
        /// <param name="components">Optional array of component names</param>
        /// <returns>The key of the created issue (e.g., "NC-1234")</returns>
        Task<string> CreateIssueAsync(string projectKey, string issueType, string summary, string description, string[] components = null);

        /// <summary>
        /// Updates an existing Jira issue
        /// </summary>
        /// <param name="issueKey">Key of the issue to update (e.g., "NC-1234")</param>
        /// <param name="fields">Dictionary of fields to update</param>
        Task UpdateIssueAsync(string issueKey, Dictionary<string, object> fields);

        /// <summary>
        /// Adds a comment to a Jira issue
        /// </summary>
        /// <param name="issueKey">Key of the issue (e.g., "NC-1234")</param>
        /// <param name="comment">Comment text</param>
        Task AddCommentAsync(string issueKey, string comment);

        /// <summary>
        /// Transitions an issue to a different status
        /// </summary>
        /// <param name="issueKey">Key of the issue (e.g., "NC-1234")</param>
        /// <param name="transitionId">ID of the transition</param>
        Task TransitionIssueAsync(string issueKey, string transitionId);

        /// <summary>
        /// Gets available transitions for an issue
        /// </summary>
        /// <param name="issueKey">Key of the issue (e.g., "NC-1234")</param>
        /// <returns>Dictionary mapping transition names to IDs</returns>
        Task<Dictionary<string, string>> GetAvailableTransitionsAsync(string issueKey);

        /// <summary>
        /// Gets the current status of an issue
        /// </summary>
        /// <param name="issueKey">Key of the issue (e.g., "NC-1234")</param>
        /// <returns>Current status name</returns>
        Task<string> GetIssueStatusAsync(string issueKey);

        /// <summary>
        /// Gets information about a specific issue
        /// </summary>
        /// <param name="issueKey">Key of the issue (e.g., "NC-1234")</param>
        /// <returns>Dictionary containing the issue fields</returns>
        Task<Dictionary<string, object>> GetIssueAsync(string issueKey);

        /// <summary>
        /// Gets metadata about issue creation, including required fields
        /// </summary>
        /// <param name="projectKey">Project key (e.g., "NCCF")</param>
        /// <param name="issueType">Issue type (e.g., "Task")</param>
        /// <returns>Dictionary of field metadata</returns>
        Task<Dictionary<string, object>> GetCreateMetaAsync(string projectKey, string issueType = "Task");

        /// <summary>
        /// Gets the list of required fields for issue creation
        /// </summary>
        /// <param name="projectKey">Project key (e.g., "NCCF")</param>
        /// <param name="issueType">Issue type (e.g., "Task")</param>
        /// <returns>Dictionary of required field IDs and their names</returns>
        Task<Dictionary<string, string>> GetRequiredFieldsAsync(string projectKey, string issueType = "Task");

        /// <summary>
        /// Gets allowed values for a custom field
        /// </summary>
        /// <param name="projectKey">Project key (e.g., "NCCF")</param>
        /// <param name="issueType">Issue type (e.g., "Task")</param>
        /// <param name="fieldId">Field ID (e.g., "customfield_18333")</param>
        /// <returns>List of allowed values</returns>
        Task<List<string>> GetAllowedValuesForFieldAsync(string projectKey, string issueType, string fieldId);

        /// <summary>
        /// Creates a link between two issues
        /// </summary>
        /// <param name="outwardIssueKey">The issue that has the relationship (e.g., "NCCF-12345")</param>
        /// <param name="inwardIssueKey">The issue that is the target of the relationship (e.g., "NCCF-67890")</param>
        /// <param name="linkType">The type of link (e.g., "Relates", "Blocks", "is subtask of")</param>
        /// <returns>True if the link was created successfully</returns>
        Task<bool> CreateIssueLinkAsync(string outwardIssueKey, string inwardIssueKey, string linkType = "Relates");

        /// <summary>
        /// Makes an issue a subtask of another issue
        /// </summary>
        /// <param name="subtaskKey">The key of the subtask (e.g., "NCCF-12345")</param>
        /// <param name="parentKey">The key of the parent task (e.g., "NCCF-67890")</param>
        /// <returns>True if the subtask relationship was created successfully</returns>
        Task<bool> SetParentTaskAsync(string subtaskKey, string parentKey);

        /// <summary>
        /// Gets all available components for a project
        /// </summary>
        /// <param name="projectKey">The project key (e.g., "NCCF")</param>
        /// <returns>Dictionary of component names and IDs</returns>
        Task<Dictionary<string, string>> GetAvailableComponentsAsync(string projectKey);

        /// <summary>
        /// Gets the issue type for a specific issue
        /// </summary>
        /// <param name="issueKey">Key of the issue (e.g., "NC-1234")</param>
        /// <returns>Issue type name</returns>
        Task<string> GetIssueTypeAsync(string issueKey);

        /// <summary>
        /// Gets detailed information about available transitions including target statuses
        /// </summary>
        /// <param name="issueKey">Key of the issue (e.g., "NC-1234")</param>
        /// <returns>Dictionary mapping transition names to transition details</returns>
        Task<Dictionary<string, TransitionDetails>> GetDetailedTransitionsAsync(string issueKey);
    }
}
