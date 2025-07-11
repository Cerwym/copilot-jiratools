using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiraTools.Tests
{
    /// <summary>
    /// Test bootstrapper to provide isolated test environments and prevent test interference
    /// </summary>
    public class TestBootstrapper : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _originalJiraToolsPath;
        
        public string TestDirectory => _testDirectory;
        public Mock<IJiraClient> MockJiraClient { get; }
        public Mock<ILogger> MockLogger { get; }
        public CommandLineOptions Options { get; }

        public TestBootstrapper(string? testName = null)
        {
            // Create unique test directory for this test instance
            var testId = testName ?? Guid.NewGuid().ToString("N")[..8];
            _testDirectory = Path.Combine(Path.GetTempPath(), "jiratools-tests", testId);
            Directory.CreateDirectory(_testDirectory);

            // Backup and override the .jiratools directory to use our test directory
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _originalJiraToolsPath = Path.Combine(userProfile, ".jiratools");
            var testJiraToolsPath = Path.Combine(_testDirectory, ".jiratools");
            
            // Set environment variable to redirect cache to test directory
            Environment.SetEnvironmentVariable("JIRATOOLS_CACHE_DIR", testJiraToolsPath);
            
            // Initialize mocks
            MockJiraClient = new Mock<IJiraClient>();
            MockLogger = new Mock<ILogger>();
            
            // Create default options
            Options = new CommandLineOptions
            {
                IssueKey = "TEST-123",
                ProjectKey = "TEST",
                NonInteractive = true,
                SkipConfirmation = true
            };
        }

        /// <summary>
        /// Setup common workflow mocks for tests that use WorkflowDiscovery
        /// </summary>
        public void SetupWorkflowMocks(string currentStatus = "To Do", string targetStatus = "Done")
        {
            MockJiraClient.Setup(x => x.GetIssueStatusAsync(It.IsAny<string>()))
                          .ReturnsAsync(currentStatus);
            MockJiraClient.Setup(x => x.GetIssueTypeAsync(It.IsAny<string>()))
                          .ReturnsAsync("Task");
            MockJiraClient.Setup(x => x.GetAvailableTransitionsAsync(It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, string> 
                          { 
                              { "In Progress", "11" }, 
                              { "Done", "31" },
                              { "In Review", "21" }
                          });
            MockJiraClient.Setup(x => x.GetDetailedTransitionsAsync(It.IsAny<string>()))
                          .ReturnsAsync(new Dictionary<string, TransitionDetails> 
                          { 
                              { "In Progress", new TransitionDetails { Id = "11", Name = "In Progress", ToStatusName = "In Progress" } },
                              { "Done", new TransitionDetails { Id = "31", Name = "Done", ToStatusName = "Done" } },
                              { "In Review", new TransitionDetails { Id = "21", Name = "In Review", ToStatusName = "In Review" } }
                          });
            MockJiraClient.Setup(x => x.TransitionIssueAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .Returns(Task.CompletedTask);
        }

        /// <summary>
        /// Setup mocks for comment operations
        /// </summary>
        public void SetupCommentMocks()
        {
            MockJiraClient.Setup(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .Returns(Task.CompletedTask);
        }

        /// <summary>
        /// Create a WorkflowDiscovery instance that uses the isolated test cache
        /// </summary>
        public WorkflowDiscovery CreateWorkflowDiscovery()
        {
            return new TestableWorkflowDiscovery(MockJiraClient.Object, Options.ProjectKey, MockLogger.Object, _testDirectory);
        }

        public void Dispose()
        {
            try
            {
                // Clean up environment variable
                Environment.SetEnvironmentVariable("JIRATOOLS_CACHE_DIR", null);
                
                // Clean up test directory
                if (Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, true);
                }
            }
            catch (Exception)
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    /// <summary>
    /// Testable version of WorkflowDiscovery that allows custom cache directory
    /// </summary>
    public class TestableWorkflowDiscovery : WorkflowDiscovery
    {
        private readonly string _testCacheDirectory;

        public TestableWorkflowDiscovery(IJiraClient jiraClient, string projectKey, ILogger logger, string testCacheDirectory)
            : base(jiraClient, projectKey, logger)
        {
            _testCacheDirectory = testCacheDirectory;
            // Force reload cache from test directory
            ReloadCache();
        }

        protected virtual void ReloadCache()
        {
            // This will be implemented by modifying WorkflowDiscovery to support custom cache paths
            // For now, we'll use the environment variable approach
        }
    }
}
