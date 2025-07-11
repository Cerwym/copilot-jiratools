using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using JiraTools;
using JiraTools.Commands;

namespace JiraTools.Tests
{
    public class TransitionCommandTests
    {
        private readonly Mock<IJiraClient> _mockJiraClient;
        private readonly Mock<ILogger> _mockLogger;
        private readonly CommandLineOptions _options;

        public TransitionCommandTests()
        {
            _mockJiraClient = new Mock<IJiraClient>();
            _mockLogger = new Mock<ILogger>();
            _options = new CommandLineOptions
            {
                IssueKey = "TEST-123",
                TransitionName = "Done",
                NonInteractive = true
            };
        }

        [Fact]
        public async Task ExecuteAsync_WithValidParameters_ShouldSucceed()
        {
            // Arrange - Dictionary mapping transition names to IDs (as per interface)
            var transitions = new Dictionary<string, string>
            {
                { "Done", "11" },
                { "In Progress", "21" }
            };

            _mockJiraClient.Setup(x => x.GetIssueStatusAsync(It.IsAny<string>()))
                          .ReturnsAsync("To Do");
            _mockJiraClient.Setup(x => x.GetAvailableTransitionsAsync(It.IsAny<string>()))
                          .ReturnsAsync(transitions);
            _mockJiraClient.Setup(x => x.TransitionIssueAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .Returns(Task.CompletedTask);

            _options.SkipConfirmation = true;
            var command = new TransitionCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result);
            _mockJiraClient.Verify(x => x.TransitionIssueAsync("TEST-123", "11"), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithMissingIssueKey_ShouldFail()
        {
            // Arrange
            _options.IssueKey = "";
            var command = new TransitionCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
            _mockJiraClient.Verify(x => x.TransitionIssueAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WithListOnlyOption_ShouldListTransitionsOnly()
        {
            // Arrange - Dictionary mapping transition names to IDs (as per interface)
            var transitions = new Dictionary<string, string>
            {
                { "Done", "11" },
                { "In Progress", "21" }
            };

            _mockJiraClient.Setup(x => x.GetIssueStatusAsync(It.IsAny<string>()))
                          .ReturnsAsync("To Do");
            _mockJiraClient.Setup(x => x.GetAvailableTransitionsAsync(It.IsAny<string>()))
                          .ReturnsAsync(transitions);

            _options.ListOnly = true;
            var command = new TransitionCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result);
            _mockJiraClient.Verify(x => x.GetAvailableTransitionsAsync("TEST-123"), Times.Once);
            _mockJiraClient.Verify(x => x.TransitionIssueAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WithInvalidTransition_ShouldFail()
        {
            // Arrange - Dictionary mapping transition names to IDs (as per interface)
            var transitions = new Dictionary<string, string>
            {
                { "In Progress", "11" },
                { "Ready for Review", "21" }
            };

            _mockJiraClient.Setup(x => x.GetIssueStatusAsync(It.IsAny<string>()))
                          .ReturnsAsync("To Do");
            _mockJiraClient.Setup(x => x.GetAvailableTransitionsAsync(It.IsAny<string>()))
                          .ReturnsAsync(transitions);

            _options.TransitionName = "NonExistentTransition";
            var command = new TransitionCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
            _mockJiraClient.Verify(x => x.TransitionIssueAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WhenGetStatusThrows_ShouldReturnFalse()
        {
            // Arrange
            _mockJiraClient.Setup(x => x.GetIssueStatusAsync(It.IsAny<string>()))
                          .ThrowsAsync(new System.Exception("API error"));

            var command = new TransitionCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ExecuteAsync_WhenGetTransitionsThrows_ShouldReturnFalse()
        {
            // Arrange
            _mockJiraClient.Setup(x => x.GetIssueStatusAsync(It.IsAny<string>()))
                          .ReturnsAsync("To Do");
            _mockJiraClient.Setup(x => x.GetAvailableTransitionsAsync(It.IsAny<string>()))
                          .ThrowsAsync(new System.Exception("API error"));

            var command = new TransitionCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ExecuteAsync_WithCaseInsensitiveTransition_ShouldSucceed()
        {
            // Arrange - Dictionary mapping transition names to IDs (as per interface)
            var transitions = new Dictionary<string, string>
            {
                { "Done", "11" },
                { "In Progress", "21" }
            };

            _mockJiraClient.Setup(x => x.GetIssueStatusAsync(It.IsAny<string>()))
                          .ReturnsAsync("To Do");
            _mockJiraClient.Setup(x => x.GetAvailableTransitionsAsync(It.IsAny<string>()))
                          .ReturnsAsync(transitions);
            _mockJiraClient.Setup(x => x.TransitionIssueAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .Returns(Task.CompletedTask);

            _options.TransitionName = "done"; // lowercase
            _options.SkipConfirmation = true;
            var command = new TransitionCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result);
            _mockJiraClient.Verify(x => x.TransitionIssueAsync("TEST-123", "11"), Times.Once);
        }

        [Fact]
        public void CommandName_ShouldReturnCorrectValue()
        {
            // Arrange
            var command = new TransitionCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act & Assert
            Assert.Equal("transition", command.CommandName);
        }

        [Fact]
        public void Description_ShouldReturnCorrectValue()
        {
            // Arrange
            var command = new TransitionCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act & Assert
            Assert.Equal("Transition a Jira task to a new status", command.Description);
        }
    }
}
