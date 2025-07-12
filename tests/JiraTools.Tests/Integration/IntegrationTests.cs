using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using JiraTools;
using JiraTools.Commands;

namespace JiraTools.Tests.Integration
{
    public class IntegrationTests : IDisposable
    {
        private readonly Mock<IJiraClient> _mockJiraClient;
        private readonly Mock<ILogger> _mockLogger;
        private readonly CommandFactory _commandFactory;

        public IntegrationTests()
        {
            _mockJiraClient = new Mock<IJiraClient>();
            _mockLogger = new Mock<ILogger>();
            _commandFactory = new CommandFactory();
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public async Task FullWorkflow_CreateTaskThenAddComment_ShouldSucceed()
        {
            // Arrange
            var createOptions = new CommandLineOptions
            {
                ProjectKey = "TEST",
                Summary = "Integration test task",
                Description = "Created by integration test",
                IssueType = "Task"
            };

            var commentOptions = new CommandLineOptions
            {
                IssueKey = "TEST-123",
                Comment = "Integration test comment"
            };

            // Setup mocks for create task
            _mockJiraClient.Setup(x => x.GetRequiredFieldsAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, string>());
            _mockJiraClient.Setup(x => x.GetAvailableComponentsAsync(It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, string>());
            _mockJiraClient.Setup(x => x.CreateIssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
                          .ReturnsAsync("TEST-123");

            // Setup mocks for add comment
            _mockJiraClient.Setup(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .Returns(Task.CompletedTask);

            // Act
            var createCommand = _commandFactory.CreateCommand("create-task", _mockJiraClient.Object, createOptions, _mockLogger.Object);
            var createResult = await createCommand.ExecuteAsync();

            var commentCommand = _commandFactory.CreateCommand("add-comment", _mockJiraClient.Object, commentOptions, _mockLogger.Object);
            var commentResult = await commentCommand.ExecuteAsync();

            // Assert
            Assert.True(createResult);
            Assert.True(commentResult);
            _mockJiraClient.Verify(x => x.CreateIssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
            _mockJiraClient.Verify(x => x.AddCommentAsync("TEST-123", "Integration test comment"), Times.Once);
        }

        [Fact]
        public async Task FullWorkflow_CreateTaskThenTransition_ShouldSucceed()
        {
            // Arrange
            var createOptions = new CommandLineOptions
            {
                ProjectKey = "TEST",
                Summary = "Integration test task",
                Description = "Created by integration test",
                IssueType = "Task"
            };

            var transitionOptions = new CommandLineOptions
            {
                IssueKey = "TEST-123",
                TransitionName = "In Progress",
                SkipConfirmation = true
            };

            // Setup mocks for create task
            _mockJiraClient.Setup(x => x.GetRequiredFieldsAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, string>());
            _mockJiraClient.Setup(x => x.GetAvailableComponentsAsync(It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, string>());
            _mockJiraClient.Setup(x => x.CreateIssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
                          .ReturnsAsync("TEST-123");

            // Setup mocks for transition
            _mockJiraClient.Setup(x => x.GetIssueStatusAsync(It.IsAny<string>()))
                          .ReturnsAsync("To Do");
            _mockJiraClient.Setup(x => x.GetAvailableTransitionsAsync(It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, string> { { "In Progress", "11" } });
            _mockJiraClient.Setup(x => x.TransitionIssueAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .Returns(Task.CompletedTask);

            // Act
            var createCommand = _commandFactory.CreateCommand("create-task", _mockJiraClient.Object, createOptions, _mockLogger.Object);
            var createResult = await createCommand.ExecuteAsync();

            var transitionCommand = _commandFactory.CreateCommand("transition", _mockJiraClient.Object, transitionOptions, _mockLogger.Object);
            var transitionResult = await transitionCommand.ExecuteAsync();

            // Assert
            Assert.True(createResult);
            Assert.True(transitionResult);
            _mockJiraClient.Verify(x => x.CreateIssueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
            _mockJiraClient.Verify(x => x.TransitionIssueAsync("TEST-123", "11"), Times.Once);
        }

        [Fact]
        public async Task CommandChaining_UpdateThenComment_ShouldSucceed()
        {
            // Arrange
            var updateOptions = new CommandLineOptions
            {
                IssueKey = "TEST-123",
                Summary = "Updated summary",
                Description = "Updated description"
            };

            var commentOptions = new CommandLineOptions
            {
                IssueKey = "TEST-123",
                Comment = "Task updated successfully"
            };

            // Setup mocks
            _mockJiraClient.Setup(x => x.UpdateIssueAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                          .Returns(Task.CompletedTask);
            _mockJiraClient.Setup(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .Returns(Task.CompletedTask);

            // Act
            var updateCommand = _commandFactory.CreateCommand("update-task", _mockJiraClient.Object, updateOptions, _mockLogger.Object);
            var updateResult = await updateCommand.ExecuteAsync();

            var commentCommand = _commandFactory.CreateCommand("add-comment", _mockJiraClient.Object, commentOptions, _mockLogger.Object);
            var commentResult = await commentCommand.ExecuteAsync();

            // Assert
            Assert.True(updateResult);
            Assert.True(commentResult);
            _mockJiraClient.Verify(x => x.UpdateIssueAsync("TEST-123", It.IsAny<Dictionary<string, object>>()), Times.Once);
            _mockJiraClient.Verify(x => x.AddCommentAsync("TEST-123", "Task updated successfully"), Times.Once);
        }

        [Fact]
        public void CommandFactory_ShouldCreateAllSupportedCommands()
        {
            // Arrange
            var supportedCommands = new[]
            {
                "add-comment",
                "create-task",
                "update-task",
                "transition",
                "discover-workflow",
                "complete",
                "workflow-help",
                "help"
            };

            var options = new CommandLineOptions();

            // Act & Assert
            foreach (var commandName in supportedCommands)
            {
                var requiresJiraClient = commandName != "help";
                var jiraClient = requiresJiraClient ? _mockJiraClient.Object : null;
                
                var command = _commandFactory.CreateCommand(commandName, jiraClient, options, _mockLogger.Object);
                
                Assert.NotNull(command);
                Assert.Equal(commandName, command.CommandName);
                Assert.True(_commandFactory.CommandExists(commandName));
            }
        }

        [Fact]
        public async Task ErrorHandling_WhenJiraClientFails_ShouldReturnFalse()
        {
            // Arrange
            var options = new CommandLineOptions
            {
                IssueKey = "TEST-123",
                Comment = "Test comment"
            };

            _mockJiraClient.Setup(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .ThrowsAsync(new System.Exception("Jira API error"));

            // Act
            var command = _commandFactory.CreateCommand("add-comment", _mockJiraClient.Object, options, _mockLogger.Object);
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task StandaloneCommands_ShouldNotRequireJiraClient()
        {
            // Arrange
            var options = new CommandLineOptions();

            // Act
            var helpCommand = _commandFactory.CreateCommand("help", null, options, _mockLogger.Object);
            var result = await helpCommand.ExecuteAsync();

            // Assert
            Assert.NotNull(helpCommand);
            Assert.True(result);
        }
    }
}
