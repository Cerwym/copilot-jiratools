using System.IO;
using System.Threading.Tasks;
using Xunit;
using JiraTools.Commands;
using JiraTools.Tests.Utils;

namespace JiraTools.Tests.Commands
{
    public class CommentTaskCommandTests : IDisposable
    {
        private readonly TestBootstrapper _bootstrapper;
        private readonly string _tempDirectory;
        private readonly string _originalCurrentDirectory;

        public CommentTaskCommandTests()
        {
            _bootstrapper = new TestBootstrapper(nameof(CommentTaskCommandTests));
            
            // Create a temporary directory for testing in a safe location
            _tempDirectory = Path.Combine(_bootstrapper.TestDirectory, "status-docs");
            Directory.CreateDirectory(_tempDirectory);
            
            // Store original current directory
            _originalCurrentDirectory = Directory.GetCurrentDirectory();
            
            // Setup comment-specific mocks
            _bootstrapper.SetupCommentMocks();
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
            // Ensure directory exists before writing file
            var directoryPath = Path.GetDirectoryName(statusDocPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            File.WriteAllText(statusDocPath, statusContent);
            
            // Verify file was created
            Assert.True(File.Exists(statusDocPath), $"File should exist at: {statusDocPath}");
            
            _bootstrapper.Options.StatusDocPath = statusDocPath;
            _bootstrapper.Options.Comment = "Test comment";
            _bootstrapper.Options.ProjectKey = "Test Project"; // Auto-select the project
            _bootstrapper.Options.NonInteractive = true; // Prevent prompting
            
            var command = new CommentTaskCommand(_bootstrapper.MockJiraClient.Object, _bootstrapper.Options, _bootstrapper.MockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExecuteAsync_WithMissingStatusDocument_ShouldFail()
        {
            // Arrange
            _bootstrapper.Options.StatusDocPath = Path.Combine(_tempDirectory, "non-existent.md");
            var command = new CommentTaskCommand(_bootstrapper.MockJiraClient.Object, _bootstrapper.Options, _bootstrapper.MockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CommandName_ShouldReturnCorrectValue()
        {
            // Arrange
            var command = new CommentTaskCommand(_bootstrapper.MockJiraClient.Object, _bootstrapper.Options, _bootstrapper.MockLogger.Object);

            // Act & Assert
            Assert.Equal("comment-task", command.CommandName);
        }

        [Fact]
        public void Description_ShouldReturnCorrectValue()
        {
            // Arrange
            var command = new CommentTaskCommand(_bootstrapper.MockJiraClient.Object, _bootstrapper.Options, _bootstrapper.MockLogger.Object);

            // Act & Assert
            Assert.Equal("Comment on a task referenced in a status document", command.Description);
        }

        [Fact]
        public async Task ExecuteAsync_WithEmptyDocument_ShouldFail()
        {
            // Arrange
            var statusDocPath = Path.Combine(_tempDirectory, "empty.md");
            File.WriteAllText(statusDocPath, "");
            
            _bootstrapper.Options.StatusDocPath = statusDocPath;
            var command = new CommentTaskCommand(_bootstrapper.MockJiraClient.Object, _bootstrapper.Options, _bootstrapper.MockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ExecuteAsync_WithInvalidTableFormat_ShouldFail()
        {
            // Arrange
            var statusDocPath = Path.Combine(_tempDirectory, "invalid-table.md");
            var statusContent = @"# Status Document

| Wrong Header Format |
|---------------------|
| Some data here |
";
            File.WriteAllText(statusDocPath, statusContent);
            
            _bootstrapper.Options.StatusDocPath = statusDocPath;
            var command = new CommentTaskCommand(_bootstrapper.MockJiraClient.Object, _bootstrapper.Options, _bootstrapper.MockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.False(result);
        }

        public void Dispose()
        {
            try
            {
                // Restore original current directory
                Directory.SetCurrentDirectory(_originalCurrentDirectory);
                
                // Clean up through bootstrapper
                _bootstrapper?.Dispose();
            }
            catch (System.Exception)
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}
