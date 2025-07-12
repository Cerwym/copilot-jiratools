#nullable enable
using System.Threading.Tasks;

namespace JiraTools.Configuration
{
    /// <summary>
    /// Interface for loading and saving project configuration from various sources
    /// </summary>
    public interface IProjectConfigurationProvider
    {
        /// <summary>
        /// Load configuration from the specified path or default location
        /// </summary>
        /// <param name="configPath">Optional path to configuration file</param>
        /// <returns>The loaded project configuration</returns>
        Task<ProjectConfiguration> LoadAsync(string? configPath = null);

        /// <summary>
        /// Save configuration to the specified path or default location
        /// </summary>
        /// <param name="configuration">The configuration to save</param>
        /// <param name="configPath">Optional path to save configuration file</param>
        Task SaveAsync(ProjectConfiguration configuration, string? configPath = null);

        /// <summary>
        /// Check if a configuration file exists at the specified path or default location
        /// </summary>
        /// <param name="configPath">Optional path to check</param>
        /// <returns>True if configuration exists</returns>
        Task<bool> ExistsAsync(string? configPath = null);

        /// <summary>
        /// Get the default configuration file path for this provider
        /// </summary>
        string GetDefaultConfigPath();
    }
}
