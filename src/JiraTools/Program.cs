using System;
using System.Threading.Tasks;
using JiraTools.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JiraTools
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Initialize Copilot context file if it doesn't exist
                ProgramUtilities.InitializeCopilotContextFile();

                // Parse command line args
                var options = ProgramUtilities.ParseCommandLineArgs(args);

                // Set up dependency injection and logging (initial setup without JiraClient)
                var services = new ServiceCollection();
                ConfigureServices(services);
                
                using var serviceProvider = services.BuildServiceProvider();
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

                // Create command factory
                var commandFactory = serviceProvider.GetRequiredService<CommandFactory>();

                // Check for help request
                if (options.ShowHelp)
                {
                    var helpCommand = new HelpCommand(options, commandFactory, logger);
                    await helpCommand.ExecuteAsync();
                    return;
                }

                // Validate that a command was provided
                if (string.IsNullOrEmpty(options.Command))
                {
                    logger.LogError("No command specified. Use --help for usage information.");
                    return;
                }

                // Check if the command exists
                if (!commandFactory.CommandExists(options.Command))
                {
                    logger.LogError("Unknown command '{Command}'. Use --help for usage information.", options.Command);
                    logger.LogInformation("Available commands: {Commands}", string.Join(", ", commandFactory.GetAvailableCommands()));
                    return;
                }

                // Check if this is a standalone command that doesn't need Jira client
                if (options.Command == "help")
                {
                    var helpCommand = new HelpCommand(options, commandFactory, logger);
                    await helpCommand.ExecuteAsync();
                    return;
                }

                // Load credentials from .env file
                ProgramUtilities.LoadCredentialsFromEnvFile(options);

                // Prompt for credentials if missing
                ProgramUtilities.PromptForCredentials(options);

                // Now set up services with JiraClient
                var servicesWithJira = new ServiceCollection();
                ConfigureServices(servicesWithJira, options);
                
                using var serviceProviderWithJira = servicesWithJira.BuildServiceProvider();
                var jiraClient = serviceProviderWithJira.GetRequiredService<IJiraClient>();
                var commandFactoryWithJira = serviceProviderWithJira.GetRequiredService<CommandFactory>();
                var loggerWithJira = serviceProviderWithJira.GetRequiredService<ILogger<Program>>();

                // Create and execute the command
                var command = commandFactoryWithJira.CreateCommand(options.Command, jiraClient, options, loggerWithJira);
                
                if (command == null)
                {
                    logger.LogError("Failed to create command '{Command}'.", options.Command);
                    return;
                }

                // Validate command parameters
                if (!command.ValidateParameters())
                {
                    logger.LogError("Command validation failed. Use --help for usage information.");
                    return;
                }

                // Execute the command
                bool success = await command.ExecuteAsync();
                
                if (!success)
                {
                    logger.LogError("Command '{Command}' failed to execute successfully.", options.Command);
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                var services = new ServiceCollection();
                ConfigureServices(services);
                using var serviceProvider = services.BuildServiceProvider();
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                
                logger.LogError(ex, "Error: {Message}", ex.Message);
                Environment.Exit(1);
            }
        }

        private static void ConfigureServices(IServiceCollection services, CommandLineOptions options = null)
        {
            services.AddLogging(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = false;
                    options.TimestampFormat = "[HH:mm:ss] ";
                    options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
                });
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Register CommandFactory
            services.AddSingleton<CommandFactory>();

            // Register JiraClient as a factory if credentials are available
            if (options != null && !string.IsNullOrEmpty(options.JiraUrl) && 
                !string.IsNullOrEmpty(options.Username) && !string.IsNullOrEmpty(options.ApiToken))
            {
                services.AddSingleton<IJiraClient>(serviceProvider =>
                    new JiraClient(
                        options.JiraUrl,
                        options.Username,
                        options.ApiToken,
                        serviceProvider.GetRequiredService<ILogger<JiraClient>>()
                    ));
            }
        }
    }

    public class CommandLineOptions
    {
        public string JiraUrl { get; set; }
        public string Username { get; set; }
        public string ApiToken { get; set; }
        public string ProjectKey { get; set; }
        public string EpicKey { get; set; }
        public string Command { get; set; }
        public string IssueKey { get; set; }
        public string IssueType { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public string Comment { get; set; }
        public string Components { get; set; }
        public string DefaultComponent { get; set; }  // Added for environment variable support
        public string TransitionName { get; set; }
        public string MappingFile { get; set; }
        public string StatusDocPath { get; set; }
        public string WorkClassification { get; set; }
        public bool ShowHelp { get; set; }
        public bool LinkToParent { get; set; }
        public string ParentTask { get; set; }  // Parent task to link to
        public bool ListOnly { get; set; }  // Added to support listing transitions without executing them
        public bool SkipConfirmation { get; set; } // Added to support skipping confirmation prompts
        public bool NonInteractive { get; set; } // Added to support non-interactive mode
    }
}
