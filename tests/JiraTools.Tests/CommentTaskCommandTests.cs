using System.IO;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using JiraTools;
using JiraTools.Commands;

namespace JiraTools.Tests
{
    public class CommentTaskCommandTests : IDisposable
    {
        private readonly Mock<IJiraClient> _mockJiraClient;
        private readonly Mock<ILogger> _mockLogger;
        private readonly CommandLineOptions _options;
        private readonly string _tempDirectory;
        private readonly string _originalCurrentDirectory;

        public CommentTaskCommandTests()
        {
            _mockJiraClient = new Mock<IJiraClient>();
            _mockLogger = new Mock<ILogger>();
            _options = new CommandLineOptions();
            
            // Create a temporary directory for testing in a safe location
            _tempDirectory = Path.Combine(Directory.GetCurrentDirectory(), "test-temp", System.Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
            
            // Store original current directory
            _originalCurrentDirectory = Directory.GetCurrentDirectory();
        }

        public void Dispose()
        {
            try
            {
                // Clean up temporary directory first
                if (Directory.Exists(_tempDirectory))
                {
                    Directory.Delete(_tempDirectory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
            
            try
            {
                // Restore original current directory
                if (Directory.Exists(_originalCurrentDirectory))
                {
                    Directory.SetCurrentDirectory(_originalCurrentDirectory);
                }
            }
            catch
            {
                // Ignore directory restoration errors
            }
        }

        [Fact]
        public async Task ExecuteAsync_WithValidStatusDocument_ShouldSucceed()
        {
            // Arrange
            var statusDocPath = Path.Combine(_tempDirectory, "status.md");
            var statusContent = @"# Status Document

| Project | Status | Jira Task | Comments |
|---------|--------|-----------|----------|
| Test Project | In Progress | TEST-123 | Test task description |
";
            File.WriteAllText(statusDocPath, statusContent);
            
            // Verify file was created
            Assert.True(File.Exists(statusDocPath), $"File should exist at: {statusDocPath}");
            
            _options.StatusDocPath = statusDocPath;
            _options.Comment = "Test comment";
            _options.ProjectKey = "Test Project"; // Auto-select the project
            _options.NonInteractive = true; // Prevent prompting
            
            _mockJiraClient.Setup(x => x.AddCommentAsync(It.IsAny<string>(), It.IsAny<string>()))
                          .Returns(Task.CompletedTask);

            var command = new CommentTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExecuteAsync_WithNonExistentDocument_ShouldFail()
        {
            // Arrange
            _options.StatusDocPath = Path.Combine(_tempDirectory, "non-existent.md");
            var command = new CommentTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CommandName_ShouldReturnCorrectValue()
        {
            // Arrange
            var command = new CommentTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act & Assert
            Assert.Equal("comment-task", command.CommandName);
        }

        [Fact]
        public void Description_ShouldReturnCorrectValue()
        {
            // Arrange
            var command = new CommentTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act & Assert
            Assert.Equal("Comment on a task referenced in a status document", command.Description);
        }

        [Fact]
        public async Task ExecuteAsync_WithEmptyDocument_ShouldFail()
        {
            // Arrange
            var statusDocPath = Path.Combine(_tempDirectory, "empty.md");
            File.WriteAllText(statusDocPath, "");
            
            _options.StatusDocPath = statusDocPath;
            var command = new CommentTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ExecuteAsync_WithInvalidTableFormat_ShouldFail()
        {
            // Arrange
            var statusDocPath = Path.Combine(_tempDirectory, "invalid.md");
            var statusContent = @"# Status Document

This is not a valid table format.
";
            File.WriteAllText(statusDocPath, statusContent);
            
            _options.StatusDocPath = statusDocPath;
            var command = new CommentTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TestFileCreation_ShouldCreateFileCorrectly()
        {
            // Arrange
            var statusDocPath = Path.Combine(_tempDirectory, "test.md");
            var content = "test content";
            
            // Act
            File.WriteAllText(statusDocPath, content);
            
            // Assert
            Assert.True(File.Exists(statusDocPath));
            Assert.Equal("test content", File.ReadAllText(statusDocPath));
        }

        [Fact]
        public void TestOptionsStatusDocPath_ShouldBeSetCorrectly()
        {
            // Arrange
            var statusDocPath = Path.Combine(_tempDirectory, "status.md");
            File.WriteAllText(statusDocPath, "test");
            
            // Act
            _options.StatusDocPath = statusDocPath;
            
            // Assert
            Assert.Equal(statusDocPath, _options.StatusDocPath);
            Assert.True(File.Exists(_options.StatusDocPath));
        }

        [Fact]
        public void TestCommandDocumentPathResolution()
        {
            // Arrange
            var statusDocPath = Path.Combine(_tempDirectory, "status.md");
            File.WriteAllText(statusDocPath, "test content");
            
            _options.StatusDocPath = statusDocPath;
            
            // Create command
            var command = new CommentTaskCommand(_mockJiraClient.Object, _options, _mockLogger.Object);
            
            // Verify the file exists where we think it should
            Assert.True(File.Exists(statusDocPath), $"File should exist at: {statusDocPath}");
            Assert.True(File.Exists(_options.StatusDocPath), $"File should exist at options path: {_options.StatusDocPath}");
            
            // Test the specific null-coalescing logic from the command
            string documentPath = _options.StatusDocPath ?? Path.Combine(
                Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName,
                "docs",
                "status.md");
                
            Assert.Equal(statusDocPath, documentPath);
            Assert.True(File.Exists(documentPath), $"Document path should exist: {documentPath}");
        }
    }
}
