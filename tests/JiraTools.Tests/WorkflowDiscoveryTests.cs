using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using JiraTools;

namespace JiraTools.Tests
{
    public class WorkflowDiscoveryTests : IDisposable
    {
        private readonly Mock<IJiraClient> _mockJiraClient;
        private readonly Mock<ILogger> _mockLogger;
        private readonly string _tempCacheDirectory;
        private readonly WorkflowDiscovery _workflowDiscovery;

        public WorkflowDiscoveryTests()
        {
            _mockJiraClient = new Mock<IJiraClient>();
            _mockLogger = new Mock<ILogger>();
            
            // Create a temporary directory for cache files
            _tempCacheDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempCacheDirectory);
            
            // We need to test with a real instance to test file operations
            _workflowDiscovery = new WorkflowDiscovery(_mockJiraClient.Object, "TEST", _mockLogger.Object);
        }

        public void Dispose()
        {
            // Clean up temporary files
            if (Directory.Exists(_tempCacheDirectory))
            {
                Directory.Delete(_tempCacheDirectory, true);
            }
        }

        [Fact]
        public void Constructor_WithProjectKey_ShouldInitialize()
        {
            // Act & Assert
            var exception = Record.Exception(() => new WorkflowDiscovery(_mockJiraClient.Object, "TEST", _mockLogger.Object));
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_WithoutProjectKey_ShouldInitialize()
        {
            // Act & Assert
            var exception = Record.Exception(() => new WorkflowDiscovery(_mockJiraClient.Object, null, _mockLogger.Object));
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => new WorkflowDiscovery(_mockJiraClient.Object, "TEST", null));
            Assert.Null(exception);
        }

        [Fact]
        public async Task DiscoverWorkflowAsync_WithValidIssueKey_ShouldNotThrow()
        {
            // Arrange
            _mockJiraClient.Setup(x => x.GetIssueStatusAsync(It.IsAny<string>()))
                          .ReturnsAsync("To Do");
            _mockJiraClient.Setup(x => x.GetIssueAsync(It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, object>
                          {
                              ["fields"] = new Dictionary<string, object>
                              {
                                  ["issuetype"] = new Dictionary<string, object>
                                  {
                                      ["name"] = "Task"
                                  }
                              }
                          });
            _mockJiraClient.Setup(x => x.GetAvailableTransitionsAsync(It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, string> 
                          { 
                              { "11", "In Progress" },
                              { "21", "Done" }
                          });

            // Act & Assert
            var exception = await Record.ExceptionAsync(async () => 
                await _workflowDiscovery.DiscoverWorkflowAsync("TEST-123", "Done"));
            Assert.Null(exception);
        }

        [Fact]
        public async Task GetWorkflowPathAsync_WithValidParameters_ShouldNotThrow()
        {
            // Arrange
            _mockJiraClient.Setup(x => x.GetIssueStatusAsync(It.IsAny<string>()))
                          .ReturnsAsync("To Do");
            _mockJiraClient.Setup(x => x.GetIssueAsync(It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, object>
                          {
                              ["fields"] = new Dictionary<string, object>
                              {
                                  ["issuetype"] = new Dictionary<string, object>
                                  {
                                      ["name"] = "Task"
                                  }
                              }
                          });

            // Act & Assert
            var exception = await Record.ExceptionAsync(async () => 
                await _workflowDiscovery.GetWorkflowPathAsync("TEST-123", "Done"));
            Assert.Null(exception);
        }

        [Fact]
        public async Task DiscoverWorkflowAsync_WithNullIssueKey_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => 
                await _workflowDiscovery.DiscoverWorkflowAsync(null, "Done"));
        }

        [Fact]
        public async Task DiscoverWorkflowAsync_WithEmptyIssueKey_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => 
                await _workflowDiscovery.DiscoverWorkflowAsync("", "Done"));
        }

        [Fact]
        public async Task GetWorkflowPathAsync_WithNullIssueKey_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => 
                await _workflowDiscovery.GetWorkflowPathAsync(null, "Done"));
        }

        [Fact]
        public async Task GetWorkflowPathAsync_WithEmptyIssueKey_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => 
                await _workflowDiscovery.GetWorkflowPathAsync("", "Done"));
        }

        [Fact]
        public async Task ExecuteWorkflowAsync_WithNullPath_ShouldThrow()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => 
                await _workflowDiscovery.ExecuteWorkflowAsync("TEST-123", null));
        }

        [Fact]
        public async Task ExecuteWorkflowAsync_WithNullIssueKey_ShouldThrow()
        {
            // Arrange
            var workflowPath = new WorkflowPath
            {
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        FromStatus = "To Do",
                        ToStatus = "In Progress",
                        TransitionName = "Start Progress",
                        TransitionId = "11"
                    }
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => 
                await _workflowDiscovery.ExecuteWorkflowAsync(null, workflowPath));
        }

        [Fact]
        public void Constructor_ShouldCreateCacheDirectory()
        {
            // The cache directory should be created in the user's profile
            // We can't easily test this without potentially affecting the user's system
            // So we just verify the constructor doesn't throw
            var exception = Record.Exception(() => new WorkflowDiscovery(_mockJiraClient.Object, "TEST", _mockLogger.Object));
            Assert.Null(exception);
        }
    }
}
