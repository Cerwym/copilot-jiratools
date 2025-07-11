using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using JiraTools;
using JiraTools.Commands;

namespace JiraTools.Tests
{
    [TestFixture]
    public class AddCommentCommandTests
    {
        [Test]
        public async Task ExecuteAsync_WithValidParameters_ShouldSucceed()
        {
            // Arrange
            var mockJiraClient = new Mock<IJiraClient>();
            var mockLogger = new Mock<ILogger>();
            
            var options = new CommandLineOptions
            {
                IssueKey = "TEST-123",
                Comment = "Test comment"
            };

            // Setup the mock to return successfully
            mockJiraClient.Setup(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()))
                         .Returns(Task.CompletedTask);

            var command = new AddCommentCommand(mockJiraClient.Object, options, mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.That(result, Is.True);
            mockJiraClient.Verify(x => x.AddCommentAsync("TEST-123", "Test comment"), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_WithMissingIssueKey_ShouldFail()
        {
            // Arrange
            var mockJiraClient = new Mock<IJiraClient>();
            var mockLogger = new Mock<ILogger>();
            
            var options = new CommandLineOptions
            {
                IssueKey = "", // Missing issue key
                Comment = "Test comment"
            };

            var command = new AddCommentCommand(mockJiraClient.Object, options, mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.That(result, Is.False);
            mockJiraClient.Verify(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_WithMissingComment_ShouldFail()
        {
            // Arrange
            var mockJiraClient = new Mock<IJiraClient>();
            var mockLogger = new Mock<ILogger>();
            
            var options = new CommandLineOptions
            {
                IssueKey = "TEST-123",
                Comment = "" // Missing comment
            };

            var command = new AddCommentCommand(mockJiraClient.Object, options, mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.That(result, Is.False);
            mockJiraClient.Verify(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_WhenJiraClientThrows_ShouldReturnFalse()
        {
            // Arrange
            var mockJiraClient = new Mock<IJiraClient>();
            var mockLogger = new Mock<ILogger>();
            
            var options = new CommandLineOptions
            {
                IssueKey = "TEST-123",
                Comment = "Test comment"
            };

            // Setup the mock to throw an exception
            mockJiraClient.Setup(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()))
                         .ThrowsAsync(new System.Exception("Jira API error"));

            var command = new AddCommentCommand(mockJiraClient.Object, options, mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.That(result, Is.False);
            mockJiraClient.Verify(x => x.AddCommentAsync("TEST-123", "Test comment"), Times.Once);
        }
    }
}
