#nullable enable
using System.IO;
using Microsoft.Extensions.Logging;

namespace JiraTools.Configuration
{
    /// <summary>
    /// Factory for creating configuration providers based on file type or preference
    /// </summary>
    public static class ProjectConfigurationProviderFactory
    {
        /// <summary>
        /// Create a configuration provider based on the file path or auto-detection
        /// </summary>
        public static IProjectConfigurationProvider CreateProvider(string? configPath = null, ILogger? logger = null)
        {
            // If no specific path provided, try to auto-detect
            if (string.IsNullOrEmpty(configPath))
            {
                return CreateAutoDetectedProvider(logger);
            }

            // Determine provider type based on file extension
            var extension = Path.GetExtension(configPath).ToLowerInvariant();

            return extension switch
            {
                ".md" => new MarkdownProjectConfigurationProvider(logger, configPath),
                ".json" => throw new System.NotImplementedException("JSON provider will be implemented in Phase 2"),
                ".yaml" or ".yml" => throw new System.NotImplementedException("YAML provider will be implemented in Phase 2"),
                _ => new MarkdownProjectConfigurationProvider(logger, configPath)
            };
        }

        /// <summary>
        /// Create a provider for testing with in-memory configuration
        /// </summary>
        public static IProjectConfigurationProvider CreateInMemoryProvider(ProjectConfiguration? configuration = null)
        {
            return new InMemoryProjectConfigurationProvider(configuration);
        }

        /// <summary>
        /// Auto-detect the best available configuration provider
        /// </summary>
        private static IProjectConfigurationProvider CreateAutoDetectedProvider(ILogger? logger)
        {
            var currentDir = Directory.GetCurrentDirectory();

            // Look for configuration files in order of preference
            var configPaths = new[]
            {
                Path.Combine(currentDir, ".jiratools", "config.json"),
                Path.Combine(currentDir, ".jiratools", "config.yaml"),
                Path.Combine(currentDir, ".jiratools", "config.yml"),
                Path.Combine(currentDir, "docs", "status.md"),
                Path.Combine(Directory.GetParent(currentDir)?.Parent?.Parent?.FullName ?? currentDir, "docs", "status.md")
            };

            foreach (var path in configPaths)
            {
                if (File.Exists(path))
                {
                    logger?.LogInformation("Auto-detected configuration file: {ConfigPath}", path);
                    return CreateProvider(path, logger);
                }
            }

            // Fallback to markdown provider with default path
            logger?.LogInformation("No configuration file found, using default markdown provider");
            return new MarkdownProjectConfigurationProvider(logger);
        }
    }
}
