using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using JiraTools;
using JiraTools.Commands;

namespace JiraTools.Tests
{
    public class AddCommentCommandTests
    {
        private readonly Mock<IJiraClient> _mockJiraClient;
        private readonly Mock<ILogger> _mockLogger;
        private readonly CommandLineOptions _options;

        public AddCommentCommandTests()
        {
            _mockJiraClient = new Mock<IJiraClient>();
            _mockLogger = new Mock<ILogger>();
            _options = new CommandLineOptions
            {
                IssueKey = "TEST-123",
                Comment = "Test comment"
            };
        }

        [Fact]
        public async Task ExecuteAsync_WithValidParameters_ShouldSucceed()
        {
            // Arrange
            _mockJiraClient.Setup(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()))
                         .Returns(Task.CompletedTask);

            var command = new AddCommentCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result);
            _mockJiraClient.Verify(x => x.AddCommentAsync("TEST-123", "Test comment"), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithMissingIssueKey_ShouldFail()
        {
            // Arrange
            _options.IssueKey = ""; // Missing issue key
            var command = new AddCommentCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
            _mockJiraClient.Verify(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WithMissingComment_ShouldFail()
        {
            // Arrange
            _options.Comment = ""; // Missing comment
            var command = new AddCommentCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
            _mockJiraClient.Verify(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_WhenJiraClientThrows_ShouldReturnFalse()
        {
            // Arrange
            _mockJiraClient.Setup(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .ThrowsAsync(new System.Exception("Jira API error"));

            var command = new AddCommentCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitialize()
        {
            // Act & Assert
            var exception = Record.Exception(() => new AddCommentCommand(_mockJiraClient.Object, _options, _mockLogger.Object));
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => new AddCommentCommand(_mockJiraClient.Object, _options, null));
            Assert.Null(exception);
        }

        [Fact]
        public void CommandName_ShouldReturnCorrectValue()
        {
            // Arrange
            var command = new AddCommentCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act & Assert
            Assert.Equal("add-comment", command.CommandName);
        }

        [Fact]
        public void Description_ShouldReturnCorrectValue()
        {
            // Arrange
            var command = new AddCommentCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act & Assert
            Assert.Equal("Add a comment to a Jira task", command.Description);
        }
    }
}
