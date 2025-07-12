using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using JiraTools;
using JiraTools.Commands;

namespace JiraTools.Tests.Commands
{
    public class CreateTaskCommandTests
    {
        private readonly Mock<IJiraClient> _mockJiraClient;
        private readonly Mock<ILogger> _mockLogger;
        private readonly CommandLineOptions _options;

        public CreateTaskCommandTests()
        {
            _mockJiraClient = new Mock<IJiraClient>();
            _mockLogger = new Mock<ILogger>();
            _options = new CommandLineOptions
            {
                ProjectKey = "TEST",
                Summary = "Test task summary",
                Description = "Test task description",
                IssueType = "Task",
                NonInteractive = true
            };
        }

        [Fact]
        public async Task ExecuteAsync_WithValidParameters_ShouldSucceed()
        {
            // Arrange
            _mockJiraClient.Setup(x => x.GetRequiredFieldsAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, string>());
            _mockJiraClient.Setup(x => x.GetAvailableComponentsAsync(It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, string> { { "Component1", "1" }, { "Component2", "2" } });
            _mockJiraClient.Setup(x => x.CreateIssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
                          .ReturnsAsync("TEST-123");

            var command = new CreateTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result);
            _mockJiraClient.Verify(x => x.CreateIssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithMissingProjectKey_ShouldFail()
        {
            // Arrange
            _options.ProjectKey = "";
            var command = new CreateTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
            _mockJiraClient.Verify(x => x.CreateIssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WithMissingSummary_ShouldFail()
        {
            // Arrange
            _options.Summary = "";
            var command = new CreateTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
            _mockJiraClient.Verify(x => x.CreateIssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WithRequiredFields_ShouldHandleRequiredFields()
        {
            // Arrange
            var requiredFields = new Dictionary<string, string>
            {
                { "customfield_12345", "Work Classification" }
            };
            
            _mockJiraClient.Setup(x => x.GetRequiredFieldsAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .ReturnsAsync(requiredFields);
            _mockJiraClient.Setup(x => x.GetAvailableComponentsAsync(It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, string>());
            _mockJiraClient.Setup(x => x.CreateIssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
                          .ReturnsAsync("TEST-123");

            _options.WorkClassification = "Development";
            var command = new CreateTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result);
            _mockJiraClient.Verify(x => x.GetRequiredFieldsAsync("TEST", "Task"), Times.Once);
            _mockJiraClient.Verify(x => x.CreateIssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithComponents_ShouldHandleComponents()
        {
            // Arrange
            _mockJiraClient.Setup(x => x.GetRequiredFieldsAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, string>());
            _mockJiraClient.Setup(x => x.GetAvailableComponentsAsync(It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, string> { { "Frontend", "1" }, { "Backend", "2" }, { "Database", "3" } });
            _mockJiraClient.Setup(x => x.CreateIssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
                          .ReturnsAsync("TEST-123");

            _options.Components = "Frontend,Backend";
            var command = new CreateTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result);
            _mockJiraClient.Verify(x => x.GetAvailableComponentsAsync("TEST"), Times.Once);
            _mockJiraClient.Verify(x => x.CreateIssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public void CommandName_ShouldReturnCorrectValue()
        {
            // Arrange
            var command = new CreateTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act & Assert
            Assert.Equal("create-task", command.CommandName);
        }

        [Fact]
        public void Description_ShouldReturnCorrectValue()
        {
            // Arrange
            var command = new CreateTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act & Assert
            Assert.Equal("Create a new Jira task", command.Description);
        }
    }
}
