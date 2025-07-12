using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using JiraTools;
using JiraTools.Commands;

namespace JiraTools.Tests.Commands
{
    public class UpdateTaskCommandTests
    {
        private readonly Mock<IJiraClient> _mockJiraClient;
        private readonly Mock<ILogger> _mockLogger;
        private readonly CommandLineOptions _options;

        public UpdateTaskCommandTests()
        {
            _mockJiraClient = new Mock<IJiraClient>();
            _mockLogger = new Mock<ILogger>();
            _options = new CommandLineOptions
            {
                IssueKey = "TEST-123",
                Summary = "Updated summary",
                Description = "Updated description"
            };
        }

        [Fact]
        public async Task ExecuteAsync_WithValidParameters_ShouldSucceed()
        {
            // Arrange
            _mockJiraClient.Setup(x => x.UpdateIssueAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                          .Returns(Task.CompletedTask);

            var command = new UpdateTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result);
            _mockJiraClient.Verify(x => x.UpdateIssueAsync("TEST-123", It.IsAny<Dictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithMissingIssueKey_ShouldFail()
        {
            // Arrange
            _options.IssueKey = "";
            var command = new UpdateTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
            _mockJiraClient.Verify(x => x.UpdateIssueAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WithOnlySummary_ShouldUpdateSummaryOnly()
        {
            // Arrange
            _options.Description = "";
            _mockJiraClient.Setup(x => x.UpdateIssueAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                          .Returns(Task.CompletedTask);

            var command = new UpdateTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result);
            _mockJiraClient.Verify(x => x.UpdateIssueAsync("TEST-123", 
                It.Is<Dictionary<string, object>>(d => d.ContainsKey("summary") && !d.ContainsKey("description"))), 
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithOnlyDescription_ShouldUpdateDescriptionOnly()
        {
            // Arrange
            _options.Summary = "";
            _mockJiraClient.Setup(x => x.UpdateIssueAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                          .Returns(Task.CompletedTask);

            var command = new UpdateTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result);
            _mockJiraClient.Verify(x => x.UpdateIssueAsync("TEST-123", 
                It.Is<Dictionary<string, object>>(d => !d.ContainsKey("summary") && d.ContainsKey("description"))), 
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithNoFieldsToUpdate_ShouldFail()
        {
            // Arrange
            _options.Summary = "";
            _options.Description = "";
            var command = new UpdateTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
            _mockJiraClient.Verify(x => x.UpdateIssueAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WhenJiraClientThrows_ShouldReturnFalse()
        {
            // Arrange
            _mockJiraClient.Setup(x => x.UpdateIssueAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                          .ThrowsAsync(new System.Exception("Jira API error"));

            var command = new UpdateTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CommandName_ShouldReturnCorrectValue()
        {
            // Arrange
            var command = new UpdateTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act & Assert
            Assert.Equal("update-task", command.CommandName);
        }

        [Fact]
        public void Description_ShouldReturnCorrectValue()
        {
            // Arrange
            var command = new UpdateTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act & Assert
            Assert.Equal("Update an existing Jira task", command.Description);
        }
    }
}
