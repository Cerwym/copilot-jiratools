using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JiraTools.Commands
{
    /// <summary>
    /// Command to transition a Jira task to a new status
    /// </summary>
    public class TransitionCommand : BaseCommand
    {
        public TransitionCommand(IJiraClient jiraClient, CommandLineOptions options, ILogger logger = null) 
            : base(jiraClient, options, logger)
        {
        }

        public override string CommandName => "transition";

        public override string Description => "Transition a Jira task to a new status";

        public override async Task<bool> ExecuteAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_options.IssueKey))
                {
                    _logger?.LogError("Issue key is required. Use --issue-key parameter.");
                    return false;
                }

                // Get the current status of the issue
                string currentStatus;
                try
                {
                    currentStatus = await _jiraClient.GetIssueStatusAsync(_options.IssueKey);
                    _logger?.LogInformation("Current status of {IssueKey}: {CurrentStatus}", _options.IssueKey, currentStatus);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "ERROR: Failed to get current status of {IssueKey}: {Message}", _options.IssueKey, ex.Message);
                    return false;
                }

                // Get available transitions
                Dictionary<string, string> transitions;
                try
                {
                    transitions = await _jiraClient.GetAvailableTransitionsAsync(_options.IssueKey);

                    if (transitions.Count == 0)
                    {
                        _logger?.LogInformation("No transitions available for issue {IssueKey} in its current status ({CurrentStatus}).", _options.IssueKey, currentStatus);
                        return true;
                    }

                    _logger?.LogInformation("Available transitions for {IssueKey}:", _options.IssueKey);
                    int index = 1;
                    var transitionList = transitions.Keys.ToList();
                    foreach (var transition in transitionList)
                    {
                        _logger?.LogInformation("{Index}. {Transition}", index, transition);
                        index++;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "ERROR: Failed to get available transitions for {IssueKey}: {Message}", _options.IssueKey, ex.Message);
                    return false;
                }

                // If user specified --list-only, exit here after showing available transitions
                if (_options.ListOnly)
                {
                    return true;
                }

                return await ExecuteTransition(transitions, currentStatus);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ERROR in TransitionTask: {Message}", ex.Message);
                return false;
            }
        }

        private async Task<bool> ExecuteTransition(Dictionary<string, string> transitions, string currentStatus)
        {
            bool transitionSuccessful = false;
            int retryAttempts = 0;
            const int maxRetries = 3;
            var availableTransitions = transitions.Keys.ToList();

            while (!transitionSuccessful && retryAttempts < maxRetries)
            {
                string transitionName = _options.TransitionName;

                // If transition name wasn't provided via command line or we're retrying
                if (string.IsNullOrEmpty(transitionName) || retryAttempts > 0)
                {
                    if (_options.NonInteractive)
                    {
                        // In non-interactive mode, just use the first available transition
                        if (transitions.Count > 0)
                        {
                            transitionName = transitions.First().Key;
                            _logger?.LogInformation("Non-interactive mode: Selected transition '{TransitionName}'", transitionName);
                        }
                        else
                        {
                            _logger?.LogError("ERROR: No transition name specified and no transitions available.");
                            return false;
                        }
                    }
                    else
                    {
                        // Interactive mode - allow selection by number or name
                        int selectedIndex = SelectFromList(availableTransitions, "Enter transition number or name (or press Enter to exit)", null, _logger);
                        
                        if (selectedIndex >= 0)
                        {
                            transitionName = availableTransitions[selectedIndex];
                            _logger?.LogInformation("Selected transition: {TransitionName}", transitionName);
                        }
                        else
                        {
                            _logger?.LogInformation("No transition specified. Exiting.");
                            return false;
                        }
                    }
                }

                // Check if the transition name is valid (case-insensitive)
                var matchingTransition = transitions.FirstOrDefault(t => 
                    string.Equals(t.Key, transitionName, StringComparison.OrdinalIgnoreCase));
                
                if (!string.IsNullOrEmpty(matchingTransition.Key))
                {
                    var transitionId = matchingTransition.Value;
                    // Ask for confirmation before executing the transition
                    bool proceedWithTransition = true;

                    if (retryAttempts == 0 && !_options.SkipConfirmation && !_options.NonInteractive)
                    {
                        proceedWithTransition = ConfirmAction($"Are you sure you want to transition {_options.IssueKey} from '{currentStatus}' to '{transitionName}'?");
                        
                        if (!proceedWithTransition)
                        {
                            _logger?.LogInformation("Transition cancelled by user.");
                        }
                    }

                    if (proceedWithTransition)
                    {
                        try
                        {
                            await _jiraClient.TransitionIssueAsync(_options.IssueKey, transitionId);
                            _logger?.LogInformation("Successfully transitioned issue {IssueKey} from '{CurrentStatus}' to '{TransitionName}'", _options.IssueKey, currentStatus, transitionName);
                            _logger?.LogInformation("View issue at: {JiraUrl}/browse/{IssueKey}", _options.JiraUrl, _options.IssueKey);
                            transitionSuccessful = true;

                            // Get the new status to confirm the transition worked
                            string newStatus = await _jiraClient.GetIssueStatusAsync(_options.IssueKey);
                            _logger?.LogInformation("New status of {IssueKey}: {NewStatus}", _options.IssueKey, newStatus);

                            // Only show follow-up options in interactive mode
                            if (!_options.NonInteractive)
                            {
                                await HandleFollowUpTransitions(newStatus);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "ERROR performing transition: {Message}", ex.Message);
                            _logger?.LogInformation("Here are the available transitions:");
                            for (int i = 0; i < availableTransitions.Count; i++)
                            {
                                _logger?.LogInformation("{Index}. {Transition}", i + 1, availableTransitions[i]);
                            }

                            // Always prompt for another attempt without increasing retry counter
                            if (ConfirmAction("Would you like to try a different transition?"))
                            {
                                _options.TransitionName = null; // Clear previous transition name to prompt for a new one
                                // Don't increment retryAttempts here to allow the user to keep trying without limits
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        // User cancelled the transition, but we should allow another attempt
                        if (ConfirmAction("Would you like to try a different transition?"))
                        {
                            _options.TransitionName = null; // Clear previous transition name
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    _logger?.LogError("Could not find transition '{TransitionName}'. Here are the available transitions:", transitionName);
                    for (int i = 0; i < availableTransitions.Count; i++)
                    {
                        _logger?.LogInformation("{Index}. {Transition}", i + 1, availableTransitions[i]);
                    }

                    if (_options.NonInteractive)
                    {
                        // In non-interactive mode, just fail
                        return false;
                    }

                    int selectedIndex = SelectFromList(availableTransitions, "Please select a valid transition", null, _logger);
                    
                    if (selectedIndex >= 0)
                    {
                        _options.TransitionName = availableTransitions[selectedIndex];
                        _logger?.LogInformation("Selected transition: {TransitionName}", _options.TransitionName);
                    }
                    else
                    {
                        _logger?.LogInformation("No selection made. Exiting.");
                        return false;
                    }

                    // Don't increment retryAttempts here to allow unlimited attempts to get the transition right
                }
            }

            if (!transitionSuccessful && retryAttempts >= maxRetries)
            {
                _logger?.LogError("Failed to transition issue {IssueKey} after {MaxRetries} attempts.", _options.IssueKey, maxRetries);
                return false;
            }

            return transitionSuccessful;
        }

        private async Task HandleFollowUpTransitions(string newStatus)
        {
            // Show new available transitions
            var newTransitions = await _jiraClient.GetAvailableTransitionsAsync(_options.IssueKey);

            if (newTransitions.Count > 0)
            {
                _logger?.LogInformation("Available transitions now:");
                int idx = 1;
                var newTransitionList = newTransitions.Keys.ToList();
                foreach (var transition in newTransitionList)
                {
                    _logger?.LogInformation("{Index}. {Transition}", idx, transition);
                    idx++;
                }

                if (ConfirmAction("Do you want to apply another transition?"))
                {
                    var newCommand = new TransitionCommand(_jiraClient, _options);
                    _options.TransitionName = null; // Clear previous transition name
                    await newCommand.ExecuteTransition(newTransitions, newStatus);
                }
            }
            else
            {
                _logger?.LogInformation("No further transitions available.");
            }
        }

        public override bool ValidateParameters()
        {
            if (string.IsNullOrEmpty(_options.IssueKey))
            {
                _logger?.LogError("Error: Issue key is required for transitions.");
                return false;
            }

            return true;
        }
    }
}
