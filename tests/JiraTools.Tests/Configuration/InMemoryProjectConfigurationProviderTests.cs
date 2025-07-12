#nullable enable
using System.Threading.Tasks;
using Xunit;
using JiraTools.Configuration;

namespace JiraTools.Tests.Configuration
{
    public class InMemoryProjectConfigurationProviderTests
    {
        [Fact]
        public async Task LoadAsync_DefaultConfiguration_ShouldReturnEmptyConfiguration()
        {
            // Arrange
            var provider = new InMemoryProjectConfigurationProvider();

            // Act
            var config = await provider.LoadAsync();

            // Assert
            Assert.NotNull(config);
            Assert.Empty(config.Projects);
        }

        [Fact]
        public async Task LoadAsync_WithInitialConfiguration_ShouldReturnConfiguration()
        {
            // Arrange
            var initialConfig = new ProjectConfiguration();
            initialConfig.AddProject(new ProjectInfo("test", "Test Project", "TEST-123"));
            
            var provider = new InMemoryProjectConfigurationProvider(initialConfig);

            // Act
            var config = await provider.LoadAsync();

            // Assert
            Assert.NotNull(config);
            Assert.Single(config.Projects);
            Assert.Equal("Test Project", config.Projects[0].Name);
        }

        [Fact]
        public async Task SaveAsync_UpdateConfiguration_ShouldPersistChanges()
        {
            // Arrange
            var provider = new InMemoryProjectConfigurationProvider();
            var newConfig = new ProjectConfiguration();
            newConfig.AddProject(new ProjectInfo("saved", "Saved Project", "SAVE-123"));

            // Act
            await provider.SaveAsync(newConfig);
            var loadedConfig = await provider.LoadAsync();

            // Assert
            Assert.Single(loadedConfig.Projects);
            Assert.Equal("Saved Project", loadedConfig.Projects[0].Name);
        }

        [Fact]
        public async Task ExistsAsync_EmptyConfiguration_ShouldReturnFalse()
        {
            // Arrange
            var provider = new InMemoryProjectConfigurationProvider();

            // Act
            var exists = await provider.ExistsAsync();

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public async Task ExistsAsync_WithProjects_ShouldReturnTrue()
        {
            // Arrange
            var config = new ProjectConfiguration();
            config.AddProject(new ProjectInfo("test", "Test", "TEST-123"));
            var provider = new InMemoryProjectConfigurationProvider(config);

            // Act
            var exists = await provider.ExistsAsync();

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public void GetDefaultConfigPath_ShouldReturnTestPath()
        {
            // Arrange
            var provider = new InMemoryProjectConfigurationProvider();

            // Act
            var path = provider.GetDefaultConfigPath();

            // Assert
            Assert.Equal("test-config.json", path);
        }

        [Fact]
        public void UpdateConfiguration_NewConfiguration_ShouldUpdateInMemoryData()
        {
            // Arrange
            var provider = new InMemoryProjectConfigurationProvider();
            var newConfig = new ProjectConfiguration();
            newConfig.AddProject(new ProjectInfo("updated", "Updated Project", "UPD-123"));

            // Act
            provider.UpdateConfiguration(newConfig);
            var result = provider.GetConfiguration();

            // Assert
            Assert.Single(result.Projects);
            Assert.Equal("Updated Project", result.Projects[0].Name);
        }

        [Fact]
        public async Task CompleteWorkflow_LoadSaveReload_ShouldMaintainData()
        {
            // Arrange
            var provider = new InMemoryProjectConfigurationProvider();
            
            // Create initial configuration
            var config1 = new ProjectConfiguration();
            config1.AddProject(new ProjectInfo("proj1", "Project 1", "PROJ-123"));
            
            // Act & Assert - Save and reload
            await provider.SaveAsync(config1);
            var loaded1 = await provider.LoadAsync();
            Assert.Single(loaded1.Projects);
            Assert.Equal("Project 1", loaded1.Projects[0].Name);
            
            // Modify configuration
            loaded1.AddProject(new ProjectInfo("proj2", "Project 2", "PROJ-456"));
            await provider.SaveAsync(loaded1);
            
            // Reload and verify
            var loaded2 = await provider.LoadAsync();
            Assert.Equal(2, loaded2.Projects.Count);
            Assert.Contains(loaded2.Projects, p => p.Name == "Project 1");
            Assert.Contains(loaded2.Projects, p => p.Name == "Project 2");
        }
    }
}
