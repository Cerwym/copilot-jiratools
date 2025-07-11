using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JiraTools.Commands
{
    /// <summary>
    /// Interface for all command implementations
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Execute the command
        /// </summary>
        /// <returns>True if the command executed successfully, false otherwise</returns>
        Task<bool> ExecuteAsync();

        /// <summary>
        /// Get the command name (used for routing)
        /// </summary>
        string CommandName { get; }

        /// <summary>
        /// Get a description of what this command does
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Validate that the command has all required parameters
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        bool ValidateParameters();
    }
}
