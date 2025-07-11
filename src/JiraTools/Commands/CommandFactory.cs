using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace JiraTools.Commands
{
    /// <summary>
    /// Factory for creating and managing command instances
    /// </summary>
    public class CommandFactory
    {
        private readonly Dictionary<string, Func<IJiraClient, CommandLineOptions, ILogger, ICommand>> _commandCreators;
        private readonly Dictionary<string, Func<CommandLineOptions, ILogger, ICommand>> _standaloneCommandCreators;

        public CommandFactory()
        {
            _commandCreators = new Dictionary<string, Func<IJiraClient, CommandLineOptions, ILogger, ICommand>>(StringComparer.OrdinalIgnoreCase)
            {
                { "create-task", (client, options, logger) => new CreateTaskCommand(client, options, logger) },
                { "update-task", (client, options, logger) => new UpdateTaskCommand(client, options, logger) },
                { "add-comment", (client, options, logger) => new AddCommentCommand(client, options, logger) },
                { "comment-task", (client, options, logger) => new CommentTaskCommand(client, options, logger) },
                { "transition", (client, options, logger) => new TransitionCommand(client, options, logger) },
                { "discover-workflow", (client, options, logger) => new DiscoverWorkflowCommand(client, options, logger) },
                { "complete", (client, options, logger) => new CompleteWorkflowCommand(client, options, logger) },
                { "workflow-help", (client, options, logger) => new WorkflowHelpCommand(client, options, logger) }
            };

            _standaloneCommandCreators = new Dictionary<string, Func<CommandLineOptions, ILogger, ICommand>>(StringComparer.OrdinalIgnoreCase)
            {
                { "help", (options, logger) => new HelpCommand(options, this, logger) }
            };
        }

        /// <summary>
        /// Create a command instance for the specified command name
        /// </summary>
        /// <param name="commandName">The name of the command to create</param>
        /// <param name="jiraClient">The Jira client instance (can be null for standalone commands)</param>
        /// <param name="options">The command line options</param>
        /// <param name="logger">The logger instance</param>
        /// <returns>A command instance, or null if the command is not found</returns>
        public ICommand CreateCommand(string commandName, IJiraClient jiraClient, CommandLineOptions options, ILogger logger = null)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                return null;
            }

            // Check standalone commands first
            if (_standaloneCommandCreators.TryGetValue(commandName, out var standaloneCreator))
            {
                return standaloneCreator(options, logger);
            }

            // Check Jira client commands
            if (_commandCreators.TryGetValue(commandName, out var creator))
            {
                return creator(jiraClient, options, logger);
            }

            return null;
        }

        /// <summary>
        /// Get all available command names
        /// </summary>
        /// <returns>A collection of available command names</returns>
        public IEnumerable<string> GetAvailableCommands()
        {
            var allCommands = _commandCreators.Keys.Concat(_standaloneCommandCreators.Keys);
            return allCommands.OrderBy(k => k);
        }

        /// <summary>
        /// Check if a command exists
        /// </summary>
        /// <param name="commandName">The command name to check</param>
        /// <returns>True if the command exists, false otherwise</returns>
        public bool CommandExists(string commandName)
        {
            return !string.IsNullOrEmpty(commandName) && 
                   (_commandCreators.ContainsKey(commandName) || _standaloneCommandCreators.ContainsKey(commandName));
        }

        /// <summary>
        /// Get command metadata for help generation without instantiating commands that require JiraClient
        /// </summary>
        /// <returns>A collection of command metadata for display</returns>
        public IEnumerable<(string CommandName, string Description)> GetCommandMetadata()
        {
            var commands = new List<(string, string)>();

            // Add metadata for Jira commands
            commands.Add(("create-task", "Create a new Jira task"));
            commands.Add(("update-task", "Update an existing Jira task"));
            commands.Add(("add-comment", "Add a comment to a Jira task"));
            commands.Add(("comment-task", "Add a comment to a Jira task"));
            commands.Add(("transition", "Transition a Jira task to a new status"));
            commands.Add(("discover-workflow", "Discover workflow transitions for a task"));
            commands.Add(("complete", "Complete a workflow transition"));
            commands.Add(("workflow-help", "Show workflow help for a task"));

            // Add metadata for standalone commands
            commands.Add(("help", "Show help information"));

            return commands.OrderBy(c => c.Item1);
        }

        /// <summary>
        /// Get all command instances for help generation (deprecated - use GetCommandMetadata instead)
        /// </summary>
        /// <param name="jiraClient">The Jira client instance (can be null for standalone commands)</param>
        /// <param name="options">The command line options</param>
        /// <param name="logger">The logger instance</param>
        /// <returns>A collection of all available commands</returns>
        [Obsolete("Use GetCommandMetadata() instead to avoid requiring JiraClient for metadata")]
        public IEnumerable<ICommand> GetAllCommands(IJiraClient jiraClient, CommandLineOptions options, ILogger logger = null)
        {
            var jiraCommands = _commandCreators.Values.Select(creator => creator(jiraClient, options, logger));
            var standaloneCommands = _standaloneCommandCreators.Values.Select(creator => creator(options, logger));
            return jiraCommands.Concat(standaloneCommands);
        }
    }
}
