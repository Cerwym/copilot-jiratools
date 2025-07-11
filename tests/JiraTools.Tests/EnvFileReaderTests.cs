using System.Collections.Generic;
using Xunit;
using JiraTools;

namespace JiraTools.Tests
{
    public class EnvFileReaderTests
    {
        private const string TestEnvContent = @"# Test .env file
JIRA_URL=https://test.atlassian.net
JIRA_USERNAME=test@example.com
JIRA_API_TOKEN=test-token-123
JIRA_PROJECT_KEY=TEST

# Comment line
QUOTED_VALUE=""quoted value with spaces""
SINGLE_QUOTED='single quoted value'
EMPTY_VALUE=
NO_EQUALS_SIGN_LINE
=EMPTY_KEY
";

        [Fact]
        public void ReadEnvFile_WithValidContent_ShouldParseCorrectly()
        {
            // Arrange
            var tempFilePath = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(tempFilePath, TestEnvContent);

            try
            {
                // Act
                var result = EnvFileReader.ReadEnvFile(tempFilePath);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(7, result.Count);
                Assert.Equal("https://test.atlassian.net", result["JIRA_URL"]);
                Assert.Equal("test@example.com", result["JIRA_USERNAME"]);
                Assert.Equal("test-token-123", result["JIRA_API_TOKEN"]);
                Assert.Equal("TEST", result["JIRA_PROJECT_KEY"]);
                Assert.Equal("quoted value with spaces", result["QUOTED_VALUE"]);
                Assert.Equal("single quoted value", result["SINGLE_QUOTED"]);
            }
            finally
            {
                // Cleanup
                System.IO.File.Delete(tempFilePath);
            }
        }

        [Fact]
        public void ReadEnvFile_WithNonExistentFile_ShouldReturnEmptyDictionary()
        {
            // Act
            var result = EnvFileReader.ReadEnvFile("non-existent-file.env");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void ReadEnvFile_WithEmptyFile_ShouldReturnEmptyDictionary()
        {
            // Arrange
            var tempFilePath = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(tempFilePath, "");

            try
            {
                // Act
                var result = EnvFileReader.ReadEnvFile(tempFilePath);

                // Assert
                Assert.NotNull(result);
                Assert.Empty(result);
            }
            finally
            {
                // Cleanup
                System.IO.File.Delete(tempFilePath);
            }
        }

        [Fact]
        public void ReadEnvFile_WithOnlyComments_ShouldReturnEmptyDictionary()
        {
            // Arrange
            var tempFilePath = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(tempFilePath, @"# This is a comment
# Another comment
   # Indented comment
");

            try
            {
                // Act
                var result = EnvFileReader.ReadEnvFile(tempFilePath);

                // Assert
                Assert.NotNull(result);
                Assert.Empty(result);
            }
            finally
            {
                // Cleanup
                System.IO.File.Delete(tempFilePath);
            }
        }

        [Fact]
        public void ReadEnvFile_WithSpecialCharacters_ShouldParseCorrectly()
        {
            // Arrange
            var tempFilePath = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(tempFilePath, @"SPECIAL_CHARS=value!@#$%^&*()
URL_WITH_EQUALS=https://example.com?param=value&other=123
MULTILINE_NOT_SUPPORTED=first line
");

            try
            {
                // Act
                var result = EnvFileReader.ReadEnvFile(tempFilePath);

                // Assert
                Assert.NotNull(result);
                Assert.Equal("value!@#$%^&*()", result["SPECIAL_CHARS"]);
                Assert.Equal("https://example.com?param=value&other=123", result["URL_WITH_EQUALS"]);
                Assert.Equal("first line", result["MULTILINE_NOT_SUPPORTED"]);
            }
            finally
            {
                // Cleanup
                System.IO.File.Delete(tempFilePath);
            }
        }

        [Fact]
        public void ReadEnvFile_WithWhitespace_ShouldTrimCorrectly()
        {
            // Arrange
            var tempFilePath = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(tempFilePath, @"  KEY_WITH_SPACES  =  value with spaces  
TABS_AND_SPACES=	value	with	tabs	
");

            try
            {
                // Act
                var result = EnvFileReader.ReadEnvFile(tempFilePath);

                // Assert
                Assert.NotNull(result);
                Assert.Equal("value with spaces", result["KEY_WITH_SPACES"]);
                Assert.Equal("value	with	tabs", result["TABS_AND_SPACES"]);
            }
            finally
            {
                // Cleanup
                System.IO.File.Delete(tempFilePath);
            }
        }

        [Fact]
        public void ReadEnvFile_WithDefaultPath_ShouldUseCurrentDirectory()
        {
            // This test verifies the default parameter behavior
            // Since we can't easily create a .env file in the test directory without side effects,
            // we just verify that it doesn't throw when calling with default parameter
            
            // Act & Assert (should not throw)
            var exception = Record.Exception(() => EnvFileReader.ReadEnvFile());
            Assert.Null(exception);
        }
    }
}
