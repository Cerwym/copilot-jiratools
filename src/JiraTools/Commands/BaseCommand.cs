using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JiraTools.Commands
{
    /// <summary>
    /// Base class for Jira command implementations that require a Jira client
    /// </summary>
    public abstract class BaseCommand : ICommand
    {
        protected readonly IJiraClient _jiraClient;
        protected readonly CommandLineOptions _options;
        protected readonly ILogger _logger;

        protected BaseCommand(IJiraClient jiraClient, CommandLineOptions options, ILogger logger = null)
        {
            _jiraClient = jiraClient ?? throw new ArgumentNullException(nameof(jiraClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        /// <summary>
        /// Execute the command
        /// </summary>
        /// <returns>True if the command executed successfully, false otherwise</returns>
        public abstract Task<bool> ExecuteAsync();

        /// <summary>
        /// Get the command name (used for routing)
        /// </summary>
        public abstract string CommandName { get; }

        /// <summary>
        /// Get a description of what this command does
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Validate that the command has all required parameters
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public virtual bool ValidateParameters()
        {
            return true;
        }

        /// <summary>
        /// Helper method to read masked input (e.g., for passwords)
        /// </summary>
        protected static string ReadMaskedInput()
        {
            var input = string.Empty;
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    input += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    input = input.Substring(0, input.Length - 1);
                    Console.Write("\b \b");
                }
            }
            while (key.Key != ConsoleKey.Enter);

            return input;
        }

        /// <summary>
        /// Helper method to prompt for user input with a default value
        /// </summary>
        protected static string PromptForInput(string prompt, string defaultValue = null)
        {
            if (!string.IsNullOrEmpty(defaultValue))
            {
                Console.Write($"{prompt} (default: {defaultValue}): ");
            }
            else
            {
                Console.Write($"{prompt}: ");
            }

            var input = Console.ReadLine();
            return string.IsNullOrEmpty(input) ? defaultValue : input;
        }

        /// <summary>
        /// Helper method to prompt for multi-line input
        /// </summary>
        protected static string PromptForMultiLineInput(string prompt)
        {
            Console.Write($"{prompt} (press Enter twice to finish): ");
            var lines = new List<string>();
            string line;
            while (!string.IsNullOrEmpty(line = Console.ReadLine()))
            {
                lines.Add(line);
            }
            return string.Join("\n", lines);
        }

        /// <summary>
        /// Helper method to confirm an action with the user
        /// </summary>
        protected static bool ConfirmAction(string message)
        {
            Console.Write($"{message} (y/n): ");
            var response = Console.ReadLine()?.ToLower();
            return response == "y" || response == "yes";
        }

        /// <summary>
        /// Helper method to select from a list of options
        /// </summary>
        protected static int SelectFromList<T>(IList<T> options, string prompt, Func<T, string> displayFunc = null, ILogger logger = null)
        {
            if (options == null || options.Count == 0)
            {
                logger?.LogInformation("No options available.");
                return -1;
            }

            logger?.LogInformation("");
            logger?.LogInformation("{Prompt}:", prompt);
            for (int i = 0; i < options.Count; i++)
            {
                var display = displayFunc != null ? displayFunc(options[i]) : options[i].ToString();
                logger?.LogInformation("{Index}. {Display}", i + 1, display);
            }

            Console.Write("Select option (enter number): ");
            if (int.TryParse(Console.ReadLine(), out int selection) && 
                selection > 0 && selection <= options.Count)
            {
                return selection - 1;
            }

            return -1;
        }
    }
}
