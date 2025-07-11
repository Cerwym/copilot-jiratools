using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using JiraTools;
using JiraTools.Commands;

namespace JiraTools.Tests
{
    [TestFixture]
    public class CommandFactoryTests
    {
        [Test]
        public void CreateCommand_WithValidJiraCommand_ShouldReturnCommand()
        {
            // Arrange
            var factory = new CommandFactory();
            var mockJiraClient = new Mock<IJiraClient>();
            var mockLogger = new Mock<ILogger>();
            var options = new CommandLineOptions();

            // Act
            var command = factory.CreateCommand("add-comment", mockJiraClient.Object, options, mockLogger.Object);

            // Assert
            Assert.That(command, Is.Not.Null);
            Assert.That(command, Is.InstanceOf<AddCommentCommand>());
        }

        [Test]
        public void CreateCommand_WithStandaloneCommand_ShouldReturnCommand()
        {
            // Arrange
            var factory = new CommandFactory();
            var options = new CommandLineOptions();
            var mockLogger = new Mock<ILogger>();

            // Act
            var command = factory.CreateCommand("help", null, options, mockLogger.Object);

            // Assert
            Assert.That(command, Is.Not.Null);
            Assert.That(command, Is.InstanceOf<HelpCommand>());
        }

        [Test]
        public void CommandExists_WithValidCommand_ShouldReturnTrue()
        {
            // Arrange
            var factory = new CommandFactory();

            // Act & Assert
            Assert.That(factory.CommandExists("add-comment"), Is.True);
            Assert.That(factory.CommandExists("help"), Is.True);
        }

        [Test]
        public void CommandExists_WithInvalidCommand_ShouldReturnFalse()
        {
            // Arrange
            var factory = new CommandFactory();

            // Act & Assert
            Assert.That(factory.CommandExists("invalid-command"), Is.False);
            Assert.That(factory.CommandExists(""), Is.False);
            Assert.That(factory.CommandExists(null), Is.False);
        }
    }
}
