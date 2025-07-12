using System.Threading.Tasks;
using Xunit;
using Moq;
using JiraTools;
using JiraTools.Commands;

namespace JiraTools.Tests.Utils
{
    public class SimpleCommandTest
    {
        [Fact]
        public void CreateCommand_ShouldReturnCorrectInstance()
        {
            // Arrange
            var mockJiraClient = new Mock<IJiraClient>();
            var options = new CommandLineOptions
            {
                IssueKey = "TEST-123",
                Comment = "Test comment"
            };

            // Act
            var command = new AddCommentCommand(mockJiraClient.Object, options);

            // Assert
            Assert.NotNull(command);
            Assert.Equal("add-comment", command.CommandName);
        }
    }
}
