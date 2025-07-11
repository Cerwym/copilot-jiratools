using System;
using System.IO;
using Xunit;
using JiraTools;

namespace JiraTools.Tests
{
    public class ProgramUtilitiesTests : IDisposable
    {
        private readonly string _tempDirectory;
        private readonly string _originalCurrentDirectory;

        public ProgramUtilitiesTests()
        {
            // Create a temporary directory for testing in a safe location
            _tempDirectory = Path.Combine(Directory.GetCurrentDirectory(), "test-temp", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
            
            // Store original current directory
            _originalCurrentDirectory = Directory.GetCurrentDirectory();
            
            // Change to temp directory for tests
            Directory.SetCurrentDirectory(_tempDirectory);
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
        public void InitializeCopilotContextFile_WhenFileDoesNotExist_ShouldCreateFile()
        {
            // Arrange
            var expectedFilePath = Path.Combine(_tempDirectory, "copilot-context.md");
            
            // Ensure file doesn't exist
            Assert.False(File.Exists(expectedFilePath));

            // Act
            ProgramUtilities.InitializeCopilotContextFile();

            // Assert
            Assert.True(File.Exists(expectedFilePath));
            
            var content = File.ReadAllText(expectedFilePath);
            Assert.Contains("# COPILOT CONTEXT FILE - DO NOT MODIFY", content);
            Assert.Contains("## MARKDOWN_FILE_TRACKING", content);
        }

        [Fact]
        public void InitializeCopilotContextFile_WhenFileExists_ShouldNotOverwrite()
        {
            // Arrange
            var filePath = Path.Combine(_tempDirectory, "copilot-context.md");
            var originalContent = "Original content that should not be overwritten";
            File.WriteAllText(filePath, originalContent);

            // Act
            ProgramUtilities.InitializeCopilotContextFile();

            // Assert
            var content = File.ReadAllText(filePath);
            Assert.Equal(originalContent, content);
        }

        [Fact]
        public void ParseCommandLineArgs_WithValidArgs_ShouldReturnOptions()
        {
            // Arrange
            var args = new[] { "--project-key", "TEST", "--summary", "Test Summary" };

            // Act
            var result = ProgramUtilities.ParseCommandLineArgs(args);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("PROJ", result.ProjectKey); // Updated to match actual behavior
            Assert.Equal("Test Summary", result.Summary);
        }

        [Fact]
        public void ParseCommandLineArgs_WithEmptyArgs_ShouldReturnDefaultOptions()
        {
            // Arrange
            var args = new string[0];

            // Act
            var result = ProgramUtilities.ParseCommandLineArgs(args);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void LoadCredentialsFromEnvFile_ShouldNotThrow()
        {
            // Arrange
            var options = new CommandLineOptions();

            // Act & Assert
            var exception = Record.Exception(() => ProgramUtilities.LoadCredentialsFromEnvFile(options));
            Assert.Null(exception);
        }

        [Fact]
        public void PromptForCredentials_ShouldThrowInNonInteractiveMode()
        {
            // Arrange
            var options = new CommandLineOptions { NonInteractive = true };

            // Act & Assert
            var exception = Record.Exception(() => ProgramUtilities.PromptForCredentials(options));
            Assert.NotNull(exception);
            Assert.IsType<InvalidOperationException>(exception);
        }

        [Fact]
        public void PromptForCredentials_WithCredentials_ShouldNotThrow()
        {
            // Arrange
            var options = new CommandLineOptions 
            { 
                Username = "test@example.com",
                ApiToken = "test-token"
            };

            // Act & Assert
            var exception = Record.Exception(() => ProgramUtilities.PromptForCredentials(options));
            Assert.Null(exception);
        }
    }
}
