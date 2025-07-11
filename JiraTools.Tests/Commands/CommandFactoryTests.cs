using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using JiraTools;
using JiraTools.Commands;
using System.Linq;

namespace JiraTools.Tests
{
    [TestFixture]
    public class CommandFactoryTests
    {
        [Test]
        public void CreateCommand_WithValidJiraCommand_ShouldReturnCorrectCommandType()
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
            Assert.That(command, Is.TypeOf<AddCommentCommand>());
        }

        [Test]
        public void CreateCommand_WithValidStandaloneCommand_ShouldReturnCorrectCommandType()
        {
            // Arrange
            var factory = new CommandFactory();
            var mockLogger = new Mock<ILogger>();
            var options = new CommandLineOptions();

            // Act - standalone commands don't need a JiraClient
            var command = factory.CreateCommand("help", null, options, mockLogger.Object);

            // Assert
            Assert.That(command, Is.Not.Null);
            Assert.That(command, Is.TypeOf<HelpCommand>());
        }

        [Test]
        public void CreateCommand_WithInvalidCommand_ShouldReturnNull()
        {
            // Arrange
            var factory = new CommandFactory();
            var mockJiraClient = new Mock<IJiraClient>();
            var mockLogger = new Mock<ILogger>();
            var options = new CommandLineOptions();

            // Act
            var command = factory.CreateCommand("invalid-command", mockJiraClient.Object, options, mockLogger.Object);

            // Assert
            Assert.That(command, Is.Null);
        }

        [Test]
        public void GetAvailableCommands_ShouldReturnAllRegisteredCommands()
        {
            // Arrange
            var factory = new CommandFactory();

            // Act
            var commands = factory.GetAvailableCommands().ToList();

            // Assert
            Assert.That(commands, Contains.Item("create-task"));
            Assert.That(commands, Contains.Item("update-task"));
            Assert.That(commands, Contains.Item("add-comment"));
            Assert.That(commands, Contains.Item("comment-task"));
            Assert.That(commands, Contains.Item("transition"));
            Assert.That(commands, Contains.Item("discover-workflow"));
            Assert.That(commands, Contains.Item("complete"));
            Assert.That(commands, Contains.Item("workflow-help"));
            Assert.That(commands, Contains.Item("help"));
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

        [Test]
        public void GetCommandMetadata_ShouldReturnCorrectMetadata()
        {
            // Arrange
            var factory = new CommandFactory();

            // Act
            var metadata = factory.GetCommandMetadata().ToList();

            // Assert
            Assert.That(metadata, Is.Not.Empty);
            
            var addCommentMetadata = metadata.FirstOrDefault(m => m.CommandName == "add-comment");
            Assert.That(addCommentMetadata, Is.Not.Null);
            Assert.That(addCommentMetadata.Description, Is.EqualTo("Add a comment to a Jira task"));
            
            var helpMetadata = metadata.FirstOrDefault(m => m.CommandName == "help");
            Assert.That(helpMetadata, Is.Not.Null);
            Assert.That(helpMetadata.Description, Is.EqualTo("Show help information"));
        }
    }
}
