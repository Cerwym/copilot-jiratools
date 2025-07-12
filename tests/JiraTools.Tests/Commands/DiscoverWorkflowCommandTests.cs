using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using JiraTools;
using JiraTools.Commands;

namespace JiraTools.Tests.Commands
{
    public class DiscoverWorkflowCommandTests
    {
        private readonly Mock<IJiraClient> _mockJiraClient;
        private readonly Mock<ILogger> _mockLogger;
        private readonly CommandLineOptions _options;

        public DiscoverWorkflowCommandTests()
        {
            _mockJiraClient = new Mock<IJiraClient>();
            _mockLogger = new Mock<ILogger>();
            _options = new CommandLineOptions
            {
                IssueKey = "TEST-123",
                ProjectKey = "TEST",
                TransitionName = "Done",
                NonInteractive = true
            };
        }

        [Fact]
        public async Task ExecuteAsync_WithValidParameters_ShouldSucceed()
        {
            // Arrange
            _mockJiraClient.Setup(x => x.GetIssueStatusAsync(It.IsAny<string>()))
                          .ReturnsAsync("To Do");
            _mockJiraClient.Setup(x => x.GetIssueTypeAsync(It.IsAny<string>()))
                          .ReturnsAsync("Task");
            _mockJiraClient.Setup(x => x.GetAvailableTransitionsAsync(It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, string> { { "In Progress", "11" }, { "Done", "31" } });
            _mockJiraClient.Setup(x => x.GetDetailedTransitionsAsync(It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, TransitionDetails> 
                          { 
                              { "In Progress", new TransitionDetails { Id = "11", Name = "In Progress", ToStatusName = "In Progress" } },
                              { "Done", new TransitionDetails { Id = "31", Name = "Done", ToStatusName = "Done" } }
                          });
            _mockJiraClient.Setup(x => x.TransitionIssueAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .Returns(Task.CompletedTask);
            
            var command = new DiscoverWorkflowCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExecuteAsync_WithMissingIssueKey_ShouldFail()
        {
            // Arrange
            _options.IssueKey = "";
            var command = new DiscoverWorkflowCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ExecuteAsync_WithDefaultTarget_ShouldUseDoneAsDefault()
        {
            // Arrange - Add all necessary mocks for WorkflowDiscovery
            _options.TransitionName = null;
            
            _mockJiraClient.Setup(x => x.GetIssueStatusAsync(It.IsAny<string>()))
                          .ReturnsAsync("To Do");
            _mockJiraClient.Setup(x => x.GetIssueTypeAsync(It.IsAny<string>()))
                          .ReturnsAsync("Task");
            _mockJiraClient.Setup(x => x.GetAvailableTransitionsAsync(It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, string> { { "In Progress", "11" }, { "Done", "31" } });
            _mockJiraClient.Setup(x => x.GetDetailedTransitionsAsync(It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, TransitionDetails> 
                          { 
                              { "In Progress", new TransitionDetails { Id = "11", Name = "In Progress", ToStatusName = "In Progress" } },
                              { "Done", new TransitionDetails { Id = "31", Name = "Done", ToStatusName = "Done" } }
                          });
            _mockJiraClient.Setup(x => x.TransitionIssueAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .Returns(Task.CompletedTask);
            
            var command = new DiscoverWorkflowCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CommandName_ShouldReturnCorrectValue()
        {
            // Arrange
            var command = new DiscoverWorkflowCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act & Assert
            Assert.Equal("discover-workflow", command.CommandName);
        }

        [Fact]
        public void Description_ShouldReturnCorrectValue()
        {
            // Arrange
            var command = new DiscoverWorkflowCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act & Assert
            Assert.Equal("Discover and cache workflow paths for an issue", command.Description);
        }
    }
}
