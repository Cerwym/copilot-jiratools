using JiraTools.Commands;
using JiraTools.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JiraTools.Tests.Commands;

/// <summary>
/// Tests for CommentTaskCommand using the new configuration provider pattern.
/// These tests demonstrate the elimination of brittle parsing and improved testability.
/// </summary>
public class CommentTaskCommandConfigurationTests
{
    private readonly Mock<IJiraClient> _mockJiraClient;
    private readonly Mock<ILogger> _mockLogger;

    public CommentTaskCommandConfigurationTests()
    {
        _mockJiraClient = new Mock<IJiraClient>();
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public async Task ExecuteAsync_WithInMemoryConfiguration_SelectsCorrectProject()
    {
        // Arrange - Create in-memory configuration with test data
        var configuration = new ProjectConfiguration();
        configuration.AddProject(new ProjectInfo("test-project", "Test Project", "TEST-123")
        {
            Status = ProjectStatus.InProgress
        });
        configuration.AddProject(new ProjectInfo("another-project", "Another Project", "TEST-456")
        {
            Status = ProjectStatus.ToDo
        });

        var provider = new InMemoryProjectConfigurationProvider(configuration);
        var options = new CommandLineOptions
        {
            Command = "comment-task",
            Comment = "Test comment using new configuration system",
            ProjectKey = "test-project", // Select specific project
            NonInteractive = true
        };

        var command = new CommentTaskCommand(_mockJiraClient.Object, provider, options, _mockLogger.Object);

        // Setup mock to complete successfully
        _mockJiraClient.Setup(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()))
                      .Returns(Task.CompletedTask);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.True(result);

        // Verify the correct Jira task was used
        _mockJiraClient.Verify(x => x.AddCommentAsync("TEST-123", It.IsAny<string>()), Times.Once);

        // Verify the comment was properly formatted with date prefix
        _mockJiraClient.Verify(x => x.AddCommentAsync(
            It.IsAny<string>(),
            It.Is<string>(comment => comment.Contains("Test comment using new configuration system"))
        ), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleProjects_HandlesNonInteractiveMode()
    {
        // Arrange - Multiple projects but no specific selection
        var configuration = new ProjectConfiguration();
        configuration.AddProject(new ProjectInfo("proj1", "Project One", "PROJ-001"));
        configuration.AddProject(new ProjectInfo("proj2", "Project Two", "PROJ-002"));

        var provider = new InMemoryProjectConfigurationProvider(configuration);
        var options = new CommandLineOptions
        {
            Command = "comment-task",
            Comment = "Test comment",
            NonInteractive = true // No project key specified
        };

        var command = new CommentTaskCommand(_mockJiraClient.Object, provider, options, _mockLogger.Object);

        // Act
        var result = await command.ExecuteAsync();

        // Assert - Should fail in non-interactive mode without project selection
        Assert.False(result);

        // Verify no Jira API calls were made
        _mockJiraClient.Verify(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyConfiguration_ReturnsFailure()
    {
        // Arrange - Empty configuration
        var configuration = new ProjectConfiguration();
        var provider = new InMemoryProjectConfigurationProvider(configuration);
        var options = new CommandLineOptions
        {
            Command = "comment-task",
            Comment = "Test comment",
            NonInteractive = true
        };

        var command = new CommentTaskCommand(_mockJiraClient.Object, provider, options, _mockLogger.Object);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.False(result);
        _mockJiraClient.Verify(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidProjectKey_ReturnsFailure()
    {
        // Arrange
        var configuration = new ProjectConfiguration();
        configuration.AddProject(new ProjectInfo("valid-project", "Valid Project", "VALID-123"));

        var provider = new InMemoryProjectConfigurationProvider(configuration);
        var options = new CommandLineOptions
        {
            Command = "comment-task",
            Comment = "Test comment",
            ProjectKey = "nonexistent-project",
            NonInteractive = true
        };

        var command = new CommentTaskCommand(_mockJiraClient.Object, provider, options, _mockLogger.Object);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.False(result);
        _mockJiraClient.Verify(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithConfigurationUpdate_SavesChanges()
    {
        // Arrange
        var configuration = new ProjectConfiguration();
        var project = new ProjectInfo("test-project", "Test Project", "TEST-123")
        {
            Status = ProjectStatus.InProgress
        };
        configuration.AddProject(project);

        var provider = new InMemoryProjectConfigurationProvider(configuration);
        var options = new CommandLineOptions
        {
            Command = "comment-task",
            Comment = "Test comment that should be saved",
            ProjectKey = "test-project",
            NonInteractive = true
        };

        var command = new CommentTaskCommand(_mockJiraClient.Object, provider, options, _mockLogger.Object);

        // Setup mock to complete successfully
        _mockJiraClient.Setup(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()))
                      .Returns(Task.CompletedTask);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.True(result);

        // Verify configuration was saved
        var updatedConfig = await provider.LoadAsync();
        var updatedProject = updatedConfig.Projects.First(p => p.Id == "test-project");

        // The comment should be added to the project
        Assert.Single(updatedProject.Comments);
        Assert.Contains("Test comment that should be saved", updatedProject.Comments.First().Text);
    }

    [Theory]
    [InlineData(ProjectStatus.ToDo)]
    [InlineData(ProjectStatus.InProgress)]
    [InlineData(ProjectStatus.Review)]
    [InlineData(ProjectStatus.Done)]
    [InlineData(ProjectStatus.Blocked)]
    public async Task ExecuteAsync_WithDifferentProjectStatuses_HandlesAllTypes(ProjectStatus status)
    {
        // Arrange
        var configuration = new ProjectConfiguration();
        configuration.AddProject(new ProjectInfo("test-project", "Test Project", "TEST-123")
        {
            Status = status
        });

        var provider = new InMemoryProjectConfigurationProvider(configuration);
        var options = new CommandLineOptions
        {
            Command = "comment-task",
            Comment = $"Test comment for {status} project",
            ProjectKey = "test-project",
            NonInteractive = true
        };

        var command = new CommentTaskCommand(_mockJiraClient.Object, provider, options, _mockLogger.Object);

        // Setup mock to complete successfully
        _mockJiraClient.Setup(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()))
                      .Returns(Task.CompletedTask);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.True(result);
        _mockJiraClient.Verify(x => x.AddCommentAsync("TEST-123", It.IsAny<string>()), Times.Once);
    }
}
