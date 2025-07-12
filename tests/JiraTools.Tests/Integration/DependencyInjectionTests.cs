using Xunit;
using Moq;
using JiraTools;
using JiraTools.Commands;

namespace JiraTools.Tests.Integration
{
    public class DependencyInjectionTests
    {
        [Fact]
        public void AddCommentCommand_WithMockedJiraClient_ShouldCreate()
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
            Assert.Equal("Add a comment to a Jira task", command.Description);
        }

        [Fact]
        public void CreateTaskCommand_WithMockedJiraClient_ShouldCreate()
        {
            // Arrange
            var mockJiraClient = new Mock<IJiraClient>();
            var options = new CommandLineOptions
            {
                ProjectKey = "TEST",
                Summary = "Test task",
                Description = "Test description"
            };

            // Act
            var command = new CreateTaskCommand(mockJiraClient.Object, options);

            // Assert
            Assert.NotNull(command);
            Assert.Equal("create-task", command.CommandName);
        }

        [Fact]
        public void CommandFactory_CreateCommand_WithMockedJiraClient_ShouldReturnCommand()
        {
            // Arrange
            var factory = new CommandFactory();
            var mockJiraClient = new Mock<IJiraClient>();
            var options = new CommandLineOptions();

            // Act
            var command = factory.CreateCommand("add-comment", mockJiraClient.Object, options);

            // Assert
            Assert.NotNull(command);
            Assert.IsType<AddCommentCommand>(command);
        }
    }
}
