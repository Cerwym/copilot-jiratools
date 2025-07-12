using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using JiraTools;
using JiraTools.Commands;

namespace JiraTools.Tests.Commands
{
    public class HelpCommandTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly CommandLineOptions _options;
        private readonly CommandFactory _commandFactory;

        public HelpCommandTests()
        {
            _mockLogger = new Mock<ILogger>();
            _options = new CommandLineOptions();
            _commandFactory = new CommandFactory();
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnTrue()
        {
            // Arrange
            var command = new HelpCommand(_options, _commandFactory, _mockLogger.Object);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CommandName_ShouldReturnCorrectValue()
        {
            // Arrange
            var command = new HelpCommand(_options, _commandFactory, _mockLogger.Object);

            // Act & Assert
            Assert.Equal("help", command.CommandName);
        }

        [Fact]
        public void Description_ShouldReturnCorrectValue()
        {
            // Arrange
            var command = new HelpCommand(_options, _commandFactory, _mockLogger.Object);

            // Act & Assert
            Assert.Equal("Show help information", command.Description);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => new HelpCommand(_options, _commandFactory, null));
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_WithNullOptions_ShouldThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => new HelpCommand(null, _commandFactory, _mockLogger.Object));
            Assert.NotNull(exception);
            Assert.IsType<ArgumentNullException>(exception);
        }
    }
}
