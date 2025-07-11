using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using JiraTools;
using JiraTools.Commands;

namespace JiraTools.Tests
{
    public class WorkflowHelpCommandTests : IDisposable
    {
        private readonly Mock<IJiraClient> _mockJiraClient;
        private readonly Mock<ILogger> _mockLogger;
        private readonly CommandLineOptions _options;

        public WorkflowHelpCommandTests()
        {
            _mockJiraClient = new Mock<IJiraClient>();
            _mockLogger = new Mock<ILogger>();
            _options = new CommandLineOptions
            {
                IssueKey = "TEST-123",
                ProjectKey = "TEST",
                NonInteractive = true
            };
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public async Task ExecuteAsync_WithValidParameters_ShouldSucceed()
        {
            // Arrange
            _mockJiraClient.Setup(x => x.GetIssueStatusAsync(It.IsAny<string>()))
                          .ReturnsAsync("In Progress");
            _mockJiraClient.Setup(x => x.GetIssueTypeAsync(It.IsAny<string>()))
                          .ReturnsAsync("Task");
            _mockJiraClient.Setup(x => x.GetAvailableTransitionsAsync(It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, string> { { "Done", "31" }, { "In Review", "21" } });
            
            var command = new WorkflowHelpCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

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
            var command = new WorkflowHelpCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CommandName_ShouldReturnCorrectValue()
        {
            // Arrange
            var command = new WorkflowHelpCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act & Assert
            Assert.Equal("workflow-help", command.CommandName);
        }

        [Fact]
        public void Description_ShouldReturnCorrectValue()
        {
            // Arrange
            var command = new WorkflowHelpCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act & Assert
            Assert.Equal("Show workflow information and suggestions", command.Description);
        }
    }
}
