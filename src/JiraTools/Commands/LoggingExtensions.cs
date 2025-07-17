using Microsoft.Extensions.Logging;

namespace JiraTools.Commands
{
    /// <summary>
    /// Extension methods and utilities for consistent logging across commands
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Log a success message for command execution
        /// </summary>
        public static void LogCommandSuccess(this ILogger logger, string commandName, string details = null)
        {
            if (details != null)
            {
                logger?.LogInformation("Command '{CommandName}' completed successfully. {Details}", commandName, details);
            }
            else
            {
                logger?.LogInformation("Command '{CommandName}' completed successfully.", commandName);
            }
        }

        /// <summary>
        /// Log a failure message for command execution
        /// </summary>
        public static void LogCommandFailure(this ILogger logger, string commandName, string reason = null)
        {
            if (reason != null)
            {
                logger?.LogError("Command '{CommandName}' failed: {Reason}", commandName, reason);
            }
            else
            {
                logger?.LogError("Command '{CommandName}' failed.", commandName);
            }
        }

        /// <summary>
        /// Log API operation details
        /// </summary>
        public static void LogApiOperation(this ILogger logger, string operation, string endpoint, object parameters = null)
        {
            if (parameters != null)
            {
                logger?.LogDebug("API Operation: {Operation} to {Endpoint} with parameters {Parameters}",
                    operation, endpoint, parameters);
            }
            else
            {
                logger?.LogDebug("API Operation: {Operation} to {Endpoint}", operation, endpoint);
            }
        }

        /// <summary>
        /// Log validation errors
        /// </summary>
        public static void LogValidationError(this ILogger logger, string field, string reason)
        {
            logger?.LogWarning("Validation error for field '{Field}': {Reason}", field, reason);
        }

        /// <summary>
        /// Log user interaction prompts
        /// </summary>
        public static void LogUserPrompt(this ILogger logger, string promptType, string message)
        {
            logger?.LogTrace("User prompt ({PromptType}): {Message}", promptType, message);
        }

        /// <summary>
        /// Log workflow operations
        /// </summary>
        public static void LogWorkflowOperation(this ILogger logger, string issueKey, string fromStatus, string toStatus, string transitionName)
        {
            logger?.LogInformation("Workflow transition for {IssueKey}: {FromStatus} → {ToStatus} via '{TransitionName}'",
                issueKey, fromStatus, toStatus, transitionName);
        }

        /// <summary>
        /// Log cache operations
        /// </summary>
        public static void LogCacheOperation(this ILogger logger, string operation, string cacheKey, bool success)
        {
            logger?.LogDebug("Cache {Operation} for key '{CacheKey}': {Success}", operation, cacheKey, success ? "Success" : "Failed");
        }
    }
}
