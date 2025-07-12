#nullable enable
using System.Threading.Tasks;

namespace JiraTools.Configuration
{
    /// <summary>
    /// In-memory configuration provider for testing and mocking scenarios
    /// </summary>
    public class InMemoryProjectConfigurationProvider : IProjectConfigurationProvider
    {
        private ProjectConfiguration _configuration;
        private readonly string _defaultPath;

        public InMemoryProjectConfigurationProvider(ProjectConfiguration? configuration = null, string defaultPath = "test-config.json")
        {
            _configuration = configuration ?? new ProjectConfiguration();
            _defaultPath = defaultPath;
        }

        public Task<ProjectConfiguration> LoadAsync(string? configPath = null)
        {
            return Task.FromResult(_configuration);
        }

        public Task SaveAsync(ProjectConfiguration configuration, string? configPath = null)
        {
            _configuration = configuration;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string? configPath = null)
        {
            return Task.FromResult(_configuration.Projects.Count > 0);
        }

        public string GetDefaultConfigPath()
        {
            return _defaultPath;
        }

        /// <summary>
        /// Update the in-memory configuration (useful for testing)
        /// </summary>
        public void UpdateConfiguration(ProjectConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Get the current in-memory configuration (useful for testing)
        /// </summary>
        public ProjectConfiguration GetConfiguration()
        {
            return _configuration;
        }
    }
}
