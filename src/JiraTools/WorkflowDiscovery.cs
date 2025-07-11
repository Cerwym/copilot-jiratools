using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace JiraTools
{
    /// <summary>
    /// Handles discovery and learning of Jira workflow transitions
    /// </summary>
    public class WorkflowDiscovery
    {
        private readonly IJiraClient _jiraClient;
        private readonly string _workflowCachePath;
        private readonly ILogger _logger;
        private WorkflowCache _cache;

        public WorkflowDiscovery(IJiraClient jiraClient, string projectKey = null, ILogger logger = null)
        {
            _jiraClient = jiraClient;
            _logger = logger;
            
            // Create a cache file specific to the project if provided
            var cacheFileName = string.IsNullOrEmpty(projectKey) 
                ? "jira-workflows.json" 
                : $"jira-workflows-{projectKey.ToLower()}.json";
            
            // Allow override of cache directory for testing
            var cacheDirectory = Environment.GetEnvironmentVariable("JIRATOOLS_CACHE_DIR") 
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".jiratools");
            
            _workflowCachePath = Path.Combine(cacheDirectory, cacheFileName);

            LoadCache();
        }

        /// <summary>
        /// Discovers and caches workflow transitions for a specific issue type
        /// </summary>
        public async Task<WorkflowPath> DiscoverWorkflowAsync(string sampleIssueKey, string targetStatus = "Done")
        {
            if (string.IsNullOrEmpty(sampleIssueKey))
                throw new ArgumentException("Issue key cannot be null or empty", nameof(sampleIssueKey));

            try
            {
                var currentStatus = await _jiraClient.GetIssueStatusAsync(sampleIssueKey);
                var issueType = await GetIssueTypeAsync(sampleIssueKey);
                
                _logger?.LogInformation("Discovering workflow for {IssueType} issues from '{CurrentStatus}' to '{TargetStatus}'...", 
                    issueType, currentStatus, targetStatus);

                var workflowPath = await TraceWorkflowPathAsync(sampleIssueKey, currentStatus, targetStatus);
                
                // Cache the discovered workflow
                CacheWorkflow(issueType, currentStatus, targetStatus, workflowPath);
                
                return workflowPath;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error discovering workflow: {Message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Gets a cached workflow path or discovers it if not cached
        /// </summary>
        public async Task<WorkflowPath> GetWorkflowPathAsync(string issueKey, string targetStatus = "Done")
        {
            if (string.IsNullOrEmpty(issueKey))
                throw new ArgumentException("Issue key cannot be null or empty", nameof(issueKey));
            
            try
            {
                var currentStatus = await _jiraClient.GetIssueStatusAsync(issueKey);
                var issueType = await GetIssueTypeAsync(issueKey);

                // Check cache first
                var cacheKey = $"{issueType}:{currentStatus}:{targetStatus}";
                if (_cache.Workflows.ContainsKey(cacheKey))
                {
                    var cachedPath = _cache.Workflows[cacheKey];
                    _logger?.LogInformation("Using cached workflow path for {IssueType} from '{CurrentStatus}' to '{TargetStatus}'", issueType, currentStatus, targetStatus);
                    return cachedPath;
                }

                // If not cached, discover it
                return await DiscoverWorkflowAsync(issueKey, targetStatus);
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error getting workflow path: {Message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Suggests common workflow completion paths based on cached data
        /// </summary>
        public List<string> GetCommonWorkflowSuggestions(string issueType, string currentStatus)
        {
            var suggestions = new List<string>();
            
            // Look for cached workflows for this issue type and status
            var relevantWorkflows = _cache.Workflows
                .Where(kv => kv.Key.StartsWith($"{issueType}:{currentStatus}:"))
                .OrderByDescending(kv => kv.Value.UsageCount)
                .Take(3);

            foreach (var workflow in relevantWorkflows)
            {
                var targetStatus = workflow.Key.Split(':')[2];
                suggestions.Add($"Complete to '{targetStatus}' ({workflow.Value.Steps.Count} steps)");
            }

            return suggestions;
        }

        /// <summary>
        /// Executes a workflow path automatically
        /// </summary>
        public async Task<bool> ExecuteWorkflowAsync(string issueKey, WorkflowPath workflowPath, bool interactive = true)
        {
            if (string.IsNullOrEmpty(issueKey))
                throw new ArgumentException("Issue key cannot be null or empty", nameof(issueKey));
                
            if (workflowPath == null)
                throw new ArgumentNullException(nameof(workflowPath));
            
            if (!workflowPath.Steps.Any())
            {
                _logger?.LogWarning("No workflow path available to execute.");
                return false;
            }

            _logger?.LogInformation("Executing workflow path with {StepCount} steps:", workflowPath.Steps.Count);
            for (int i = 0; i < workflowPath.Steps.Count; i++)
            {
                var step = workflowPath.Steps[i];
                _logger?.LogInformation("  {StepNumber}. {FromStatus} → {ToStatus} (via '{TransitionName}')", i + 1, step.FromStatus, step.ToStatus, step.TransitionName);
            }

            if (interactive)
            {
                Console.Write("Proceed with execution? (y/N): ");
                var response = Console.ReadLine();
                if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.LogInformation("Workflow execution cancelled.");
                    return false;
                }
            }

            try
            {
                foreach (var step in workflowPath.Steps)
                {
                    _logger?.LogInformation("Executing: {FromStatus} → {ToStatus} via '{TransitionName}'", step.FromStatus, step.ToStatus, step.TransitionName);
                    
                    await _jiraClient.TransitionIssueAsync(issueKey, step.TransitionName);
                    
                    // Small delay to ensure transition completes
                    await Task.Delay(1000);
                    
                    // Verify the transition worked
                    var currentStatus = await _jiraClient.GetIssueStatusAsync(issueKey);
                    if (currentStatus != step.ToStatus)
                    {
                        _logger?.LogWarning("Warning: Expected status '{ExpectedStatus}' but found '{ActualStatus}'", step.ToStatus, currentStatus);
                    }
                }

                // Update usage count
                workflowPath.UsageCount++;
                workflowPath.LastUsed = DateTime.UtcNow;
                SaveCache();

                _logger?.LogInformation("Workflow execution completed successfully!");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing workflow: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Traces a path from current status to target status using available transitions
        /// </summary>
        private async Task<WorkflowPath> TraceWorkflowPathAsync(string issueKey, string fromStatus, string targetStatus)
        {
            var visited = new HashSet<string>();
            var path = new List<WorkflowStep>();
            
            if (await FindPathRecursive(issueKey, fromStatus, targetStatus, visited, path))
            {
                return new WorkflowPath
                {
                    Steps = path,
                    DiscoveredDate = DateTime.UtcNow,
                    UsageCount = 0
                };
            }

            return null;
        }

        /// <summary>
        /// Recursive function to find workflow path using DFS
        /// </summary>
        private async Task<bool> FindPathRecursive(string issueKey, string currentStatus, string targetStatus, 
            HashSet<string> visited, List<WorkflowStep> path)
        {
            if (currentStatus == targetStatus)
                return true;

            if (visited.Contains(currentStatus))
                return false;

            visited.Add(currentStatus);

            try
            {
                var transitions = await _jiraClient.GetAvailableTransitionsAsync(issueKey);
                
                // Try each available transition
                foreach (var transition in transitions)
                {
                    // We need to get the target status for each transition
                    // This is a simplified approach - in reality we might need to get more details
                    var targetStatusForTransition = await GetTransitionTargetStatus(issueKey, transition.Value);
                    
                    if (!visited.Contains(targetStatusForTransition))
                    {
                        var step = new WorkflowStep
                        {
                            FromStatus = currentStatus,
                            ToStatus = targetStatusForTransition,
                            TransitionName = transition.Key,
                            TransitionId = transition.Value
                        };

                        path.Add(step);

                        // Simulate the transition to explore further
                        if (await FindPathRecursive(issueKey, targetStatusForTransition, targetStatus, visited, path))
                        {
                            return true;
                        }

                        // Backtrack
                        path.RemoveAt(path.Count - 1);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exploring transitions from {CurrentStatus}: {Message}", currentStatus, ex.Message);
            }

            visited.Remove(currentStatus);
            return false;
        }

        /// <summary>
        /// Gets the target status for a specific transition using detailed transition info
        /// </summary>
        private async Task<string> GetTransitionTargetStatus(string issueKey, string transitionId)
        {
            try
            {
                var detailedTransitions = await _jiraClient.GetDetailedTransitionsAsync(issueKey);
                var transition = detailedTransitions.Values.FirstOrDefault(t => t.Id == transitionId);
                
                if (transition != null)
                {
                    return transition.ToStatusName;
                }

                // Fallback to the old method
                var transitions = await _jiraClient.GetAvailableTransitionsAsync(issueKey);
                var transitionName = transitions.FirstOrDefault(t => t.Value == transitionId).Key;

                // Common patterns for inferring target status from transition names
                var statusMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Start doing", "Doing" },
                    { "Start Progress", "In Progress" },
                    { "Ready for verification", "Ready for Verification" },
                    { "Start verification", "Verifying" },
                    { "Ready for acceptance", "Ready for Acceptance" },
                    { "Accept for Release", "Ready for Release" },
                    { "Release to Closed", "Closed" },
                    { "Done", "Done" },
                    { "Close Issue", "Closed" },
                    { "Resolve Issue", "Resolved" }
                };

                if (statusMappings.ContainsKey(transitionName))
                {
                    return statusMappings[transitionName];
                }

                // Fallback: remove common prefixes and suffixes
                var cleanName = transitionName
                    .Replace("Start ", "")
                    .Replace("Mark as ", "")
                    .Replace("Move to ", "")
                    .Replace("Set to ", "");

                return cleanName;
            }
            catch
            {
                return "Unknown";
            }
        }

        private async Task<string> GetIssueTypeAsync(string issueKey)
        {
            try
            {
                return await _jiraClient.GetIssueTypeAsync(issueKey);
            }
            catch
            {
                return "Task"; // Default fallback
            }
        }

        private void LoadCache()
        {
            try
            {
                var directory = Path.GetDirectoryName(_workflowCachePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (File.Exists(_workflowCachePath))
                {
                    var json = File.ReadAllText(_workflowCachePath);
                    _cache = JsonConvert.DeserializeObject<WorkflowCache>(json) ?? new WorkflowCache();
                }
                else
                {
                    _cache = new WorkflowCache();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Warning: Could not load workflow cache: {Message}", ex.Message);
                _cache = new WorkflowCache();
            }
        }

        private void SaveCache()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_cache, Formatting.Indented);
                File.WriteAllText(_workflowCachePath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Warning: Could not save workflow cache: {Message}", ex.Message);
            }
        }

        private void CacheWorkflow(string issueType, string fromStatus, string toStatus, WorkflowPath workflow)
        {
            var key = $"{issueType}:{fromStatus}:{toStatus}";
            _cache.Workflows[key] = workflow;
            SaveCache();
        }
    }

    /// <summary>
    /// Represents a cached workflow
    /// </summary>
    public class WorkflowCache
    {
        public Dictionary<string, WorkflowPath> Workflows { get; set; } = new Dictionary<string, WorkflowPath>();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents a complete workflow path
    /// </summary>
    public class WorkflowPath
    {
        public List<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
        public DateTime DiscoveredDate { get; set; }
        public DateTime LastUsed { get; set; }
        public int UsageCount { get; set; }
    }

    /// <summary>
    /// Represents a single step in a workflow
    /// </summary>
    public class WorkflowStep
    {
        public string FromStatus { get; set; }
        public string ToStatus { get; set; }
        public string TransitionName { get; set; }
        public string TransitionId { get; set; }
    }
}
