#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using JiraTools.Configuration;

namespace JiraTools.Tests.Configuration
{
    public class ProjectConfigurationTests
    {
        [Fact]
        public void ProjectConfiguration_DefaultConstructor_ShouldCreateEmptyConfiguration()
        {
            // Arrange & Act
            var config = new ProjectConfiguration();

            // Assert
            Assert.NotNull(config.Projects);
            Assert.Empty(config.Projects);
            Assert.NotNull(config.Settings);
            Assert.Empty(config.Settings);
        }

        [Fact]
        public void ProjectConfiguration_WithProjects_ShouldInitializeCorrectly()
        {
            // Arrange
            var projects = new List<ProjectInfo>
            {
                new("proj1", "Project 1", "PROJ-123"),
                new("proj2", "Project 2", "PROJ-456")
            };

            // Act
            var config = new ProjectConfiguration(projects);

            // Assert
            Assert.Equal(2, config.Projects.Count);
            Assert.Equal("Project 1", config.Projects[0].Name);
            Assert.Equal("Project 2", config.Projects[1].Name);
        }

        [Fact]
        public void FindProjectById_ExistingProject_ShouldReturnProject()
        {
            // Arrange
            var project = new ProjectInfo("test-proj", "Test Project", "TEST-123");
            var config = new ProjectConfiguration(new[] { project });

            // Act
            var result = config.FindProjectById("test-proj");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Project", result.Name);
        }

        [Fact]
        public void FindProjectById_NonExistingProject_ShouldReturnNull()
        {
            // Arrange
            var config = new ProjectConfiguration();

            // Act
            var result = config.FindProjectById("non-existing");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindProjectByName_CaseInsensitive_ShouldReturnProject()
        {
            // Arrange
            var project = new ProjectInfo("test-proj", "Test Project", "TEST-123");
            var config = new ProjectConfiguration(new[] { project });

            // Act
            var result = config.FindProjectByName("test project");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TEST-123", result.JiraTaskId);
        }

        [Fact]
        public void FindProjectByJiraTaskId_ExistingTask_ShouldReturnProject()
        {
            // Arrange
            var project = new ProjectInfo("test-proj", "Test Project", "TEST-123");
            var config = new ProjectConfiguration(new[] { project });

            // Act
            var result = config.FindProjectByJiraTaskId("TEST-123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Project", result.Name);
        }

        [Fact]
        public void GetProjectsByStatus_FilteredProjects_ShouldReturnCorrectProjects()
        {
            // Arrange
            var projects = new[]
            {
                new ProjectInfo("proj1", "Project 1", "PROJ-123") { Status = ProjectStatus.InProgress },
                new ProjectInfo("proj2", "Project 2", "PROJ-456") { Status = ProjectStatus.Done },
                new ProjectInfo("proj3", "Project 3", "PROJ-789") { Status = ProjectStatus.InProgress }
            };
            var config = new ProjectConfiguration(projects);

            // Act
            var inProgressProjects = config.GetProjectsByStatus(ProjectStatus.InProgress);

            // Assert
            Assert.Equal(2, inProgressProjects.Count());
        }

        [Fact]
        public void AddProject_NewProject_ShouldAddToCollection()
        {
            // Arrange
            var config = new ProjectConfiguration();
            var project = new ProjectInfo("new-proj", "New Project", "NEW-123");

            // Act
            config.AddProject(project);

            // Assert
            Assert.Single(config.Projects);
            Assert.Equal("New Project", config.Projects[0].Name);
        }

        [Fact]
        public void RemoveProject_ExistingProject_ShouldRemoveAndReturnTrue()
        {
            // Arrange
            var project = new ProjectInfo("test-proj", "Test Project", "TEST-123");
            var config = new ProjectConfiguration(new[] { project });

            // Act
            var result = config.RemoveProject("test-proj");

            // Assert
            Assert.True(result);
            Assert.Empty(config.Projects);
        }

        [Fact]
        public void RemoveProject_NonExistingProject_ShouldReturnFalse()
        {
            // Arrange
            var config = new ProjectConfiguration();

            // Act
            var result = config.RemoveProject("non-existing");

            // Assert
            Assert.False(result);
        }
    }
}
