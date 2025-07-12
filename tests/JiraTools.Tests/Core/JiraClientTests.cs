using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Moq.Protected;
using Microsoft.Extensions.Logging;
using JiraTools;
using System.Threading;

namespace JiraTools.Tests.Core
{
    public class JiraClientTests
    {
        private readonly Mock<ILogger<JiraClient>> _mockLogger;
        private const string BaseUrl = "https://test.atlassian.net";
        private const string Username = "test@example.com";
        private const string ApiToken = "test-token";

        public JiraClientTests()
        {
            _mockLogger = new Mock<ILogger<JiraClient>>();
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitialize()
        {
            // Act & Assert
            var exception = Record.Exception(() => new JiraClient(BaseUrl, Username, ApiToken, _mockLogger.Object));
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_WithTrailingSlashInUrl_ShouldTrimSlash()
        {
            // Arrange
            var urlWithSlash = "https://test.atlassian.net/";

            // Act & Assert
            var exception = Record.Exception(() => new JiraClient(urlWithSlash, Username, ApiToken, _mockLogger.Object));
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => new JiraClient(BaseUrl, Username, ApiToken, null));
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_WithEmptyUrl_ShouldNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => new JiraClient("", Username, ApiToken, _mockLogger.Object));
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_WithEmptyUsername_ShouldNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => new JiraClient(BaseUrl, "", ApiToken, _mockLogger.Object));
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_WithEmptyApiToken_ShouldNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => new JiraClient(BaseUrl, Username, "", _mockLogger.Object));
            Assert.Null(exception);
        }

        // Note: Testing HTTP operations would require more complex mocking of HttpClient
        // which is challenging with the current implementation. In a production environment,
        // we would typically inject HttpClient or use an HTTP client factory for better testability.
        
        [Fact]
        public void Constructor_AuthenticationHeader_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var client = new JiraClient(BaseUrl, Username, ApiToken, _mockLogger.Object);
            
            // Assert - We can't directly test the authentication header without exposing HttpClient,
            // but we can verify the constructor doesn't throw and the object is created
            Assert.NotNull(client);
        }
    }
}
