using System;
using System.Net;
using System.Text;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace JiraTools
{
    /// <summary>
    /// Client for interacting with Jira API
    /// </summary>
    public class JiraClient
    {
        private readonly string _baseUrl;
        private readonly string _username;
        private readonly string _apiToken;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Creates a new instance of JiraClient
        /// </summary>
        /// <param name="baseUrl">Base URL of Jira instance (e.g., https://n-able.atlassian.net)</param>
        /// <param name="username">Jira username (typically email address)</param>
        /// <param name="apiToken">Jira API token</param>
        public JiraClient(string baseUrl, string username, string apiToken)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _username = username;
            _apiToken = apiToken;

            _httpClient = new HttpClient();

            // Set up basic authentication
            var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_username}:{_apiToken}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

            // Set content type to JSON
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Creates a new issue in Jira
        /// </summary>
        /// <param name="projectKey">Project key (e.g., "NC" for N-Central)</param>
        /// <param name="issueType">Type of issue (e.g., "Task", "Bug")</param>
        /// <param name="summary">Issue summary/title</param>
        /// <param name="description">Issue description</param>
        /// <param name="components">Optional array of component names</param>
        /// <returns>The key of the created issue (e.g., "NC-1234")</returns>
        public async Task<string> CreateIssueAsync(string projectKey, string issueType, string summary, string description, string[] components = null)
        {
            var url = $"{_baseUrl}/rest/api/2/issue";

            // Build the fields object
            var fields = new Dictionary<string, object>
            {
                { "project", new { key = projectKey } },
                { "summary", summary },
                { "description", description },
                { "issuetype", new { name = issueType } }
            };

            // Add components if specified
            if (components != null && components.Length > 0)
            {
                var componentsList = new List<object>();
                foreach (var component in components)
                {
                    componentsList.Add(new { name = component });
                }
                fields.Add("components", componentsList);
            }

            // Create the request body
            var requestBody = new { fields };
            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Send the request
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error creating Jira issue: {response.StatusCode} - {errorContent}");
            }

            // Parse the response to get the issue key
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObj = JsonConvert.DeserializeObject<JiraCreateIssueResponse>(responseContent);

            return responseObj.Key;
        }

        /// <summary>
        /// Updates an existing Jira issue
        /// </summary>
        /// <param name="issueKey">Key of the issue to update (e.g., "NC-1234")</param>
        /// <param name="fields">Dictionary of fields to update</param>
        public async Task UpdateIssueAsync(string issueKey, Dictionary<string, object> fields)
        {
            var url = $"{_baseUrl}/rest/api/2/issue/{issueKey}";

            // Create the request body
            var requestBody = new { fields };
            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Send the request
            var response = await _httpClient.PutAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error updating Jira issue {issueKey}: {response.StatusCode} - {errorContent}");
            }
        }

        /// <summary>
        /// Adds a comment to a Jira issue
        /// </summary>
        /// <param name="issueKey">Key of the issue (e.g., "NC-1234")</param>
        /// <param name="comment">Comment text</param>
        public async Task AddCommentAsync(string issueKey, string comment)
        {
            var url = $"{_baseUrl}/rest/api/2/issue/{issueKey}/comment";

            // Create the request body
            var requestBody = new { body = comment };
            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Send the request
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error adding comment to Jira issue {issueKey}: {response.StatusCode} - {errorContent}");
            }
        }

        /// <summary>
        /// Transitions an issue to a different status
        /// </summary>
        /// <param name="issueKey">Key of the issue (e.g., "NC-1234")</param>
        /// <param name="transitionId">ID of the transition</param>
        public async Task TransitionIssueAsync(string issueKey, string transitionId)
        {
            var url = $"{_baseUrl}/rest/api/2/issue/{issueKey}/transitions";

            // Create the request body
            var requestBody = new { transition = new { id = transitionId } };
            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Send the request
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error transitioning Jira issue {issueKey}: {response.StatusCode} - {errorContent}");
            }
        }

        /// <summary>
        /// Gets available transitions for an issue
        /// </summary>
        /// <param name="issueKey">Key of the issue (e.g., "NC-1234")</param>
        /// <returns>Dictionary mapping transition names to IDs</returns>
        public async Task<Dictionary<string, string>> GetAvailableTransitionsAsync(string issueKey)
        {
            var url = $"{_baseUrl}/rest/api/2/issue/{issueKey}/transitions";

            // Send the request
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error getting transitions for Jira issue {issueKey}: {response.StatusCode} - {errorContent}");
            }

            // Parse the response to get the transitions
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObj = JsonConvert.DeserializeObject<JiraTransitionsResponse>(responseContent);

            var result = new Dictionary<string, string>();
            foreach (var transition in responseObj.Transitions)
            {
                result[transition.Name] = transition.Id;
            }

            return result;
        }

        /// <summary>
        /// Gets the current status of an issue
        /// </summary>
        /// <param name="issueKey">Key of the issue (e.g., "NC-1234")</param>
        /// <returns>Current status name</returns>
        public async Task<string> GetIssueStatusAsync(string issueKey)
        {
            var url = $"{_baseUrl}/rest/api/2/issue/{issueKey}?fields=status";

            // Send the request
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error getting status for Jira issue {issueKey}: {response.StatusCode} - {errorContent}");
            }

            // Parse the response to get the status
            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic responseObj = JsonConvert.DeserializeObject(responseContent);

            return responseObj.fields.status.name.ToString();
        }

        /// <summary>
        /// Gets information about a specific issue
        /// </summary>
        /// <param name="issueKey">Key of the issue (e.g., "NC-1234")</param>
        /// <returns>Dictionary containing the issue fields</returns>
        public async Task<Dictionary<string, object>> GetIssueAsync(string issueKey)
        {
            var url = $"{_baseUrl}/rest/api/2/issue/{issueKey}";

            // Send the request
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error getting Jira issue {issueKey}: {response.StatusCode} - {errorContent}");
            }

            // Parse the response
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);

            return responseObj;
        }

        /// <summary>
        /// Gets metadata about issue creation, including required fields
        /// </summary>
        /// <param name="projectKey">Project key (e.g., "NCCF")</param>
        /// <param name="issueType">Issue type (e.g., "Task")</param>
        /// <returns>Dictionary of field metadata</returns>
        public async Task<Dictionary<string, object>> GetCreateMetaAsync(string projectKey, string issueType = "Task")
        {
            var url = $"{_baseUrl}/rest/api/2/issue/createmeta?projectKeys={projectKey}&issuetypeNames={issueType}&expand=projects.issuetypes.fields";

            // Send the request
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error getting create meta: {response.StatusCode} - {errorContent}");
            }

            // Parse the response
            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);

            var fieldMeta = new Dictionary<string, object>();

            // Navigate through the complex structure to get field metadata
            var projects = jsonResponse.projects;
            if (projects.Count > 0)
            {
                var project = projects[0];
                var issueTypes = project.issuetypes;
                if (issueTypes.Count > 0)
                {
                    var issueTypeObj = issueTypes[0];
                    var fields = issueTypeObj.fields;

                    // Convert fields to dictionary
                    foreach (var field in fields)
                    {
                        string fieldId = field.Name;
                        bool required = field.Value.required != null && (bool)field.Value.required;
                        string name = field.Value.name;

                        fieldMeta[fieldId] = new
                        {
                            id = fieldId,
                            name = name,
                            required = required,
                            allowedValues = field.Value.allowedValues
                        };
                    }
                }
            }

            return fieldMeta;
        }

        /// <summary>
        /// Gets the list of required fields for issue creation
        /// </summary>
        /// <param name="projectKey">Project key (e.g., "NCCF")</param>
        /// <param name="issueType">Issue type (e.g., "Task")</param>
        /// <returns>Dictionary of required field IDs and their names</returns>
        public async Task<Dictionary<string, string>> GetRequiredFieldsAsync(string projectKey, string issueType = "Task")
        {
            var meta = await GetCreateMetaAsync(projectKey, issueType);
            var requiredFields = new Dictionary<string, string>();

            foreach (var field in meta)
            {
                dynamic fieldData = field.Value;
                if (fieldData.required)
                {
                    requiredFields[field.Key] = fieldData.name;
                }
            }

            return requiredFields;
        }

        /// <summary>
        /// Gets allowed values for a custom field
        /// </summary>
        /// <param name="projectKey">Project key (e.g., "NCCF")</param>
        /// <param name="issueType">Issue type (e.g., "Task")</param>
        /// <param name="fieldId">Field ID (e.g., "customfield_18333")</param>
        /// <returns>List of allowed values</returns>
        public async Task<List<string>> GetAllowedValuesForFieldAsync(string projectKey, string issueType, string fieldId)
        {
            var meta = await GetCreateMetaAsync(projectKey, issueType);
            var allowedValues = new List<string>();

            if (meta.TryGetValue(fieldId, out object fieldMetaObj))
            {
                dynamic fieldMeta = fieldMetaObj;
                if (fieldMeta.allowedValues != null)
                {
                    foreach (var value in fieldMeta.allowedValues)
                    {
                        if (value.value != null)
                        {
                            allowedValues.Add(value.value.ToString());
                        }
                        else if (value.name != null)
                        {
                            allowedValues.Add(value.name.ToString());
                        }
                    }
                }
            }

            return allowedValues;
        }

        /// <summary>
        /// Creates a link between two issues
        /// </summary>
        /// <param name="outwardIssueKey">The issue that has the relationship (e.g., "NCCF-12345")</param>
        /// <param name="inwardIssueKey">The issue that is the target of the relationship (e.g., "NCCF-67890")</param>
        /// <param name="linkType">The type of link (e.g., "Relates", "Blocks", "is subtask of")</param>
        /// <returns>True if the link was created successfully</returns>
        public async Task<bool> CreateIssueLinkAsync(string outwardIssueKey, string inwardIssueKey, string linkType = "Relates")
        {
            var url = $"{_baseUrl}/rest/api/2/issueLink";

            // Create the request body
            var requestBody = new
            {
                type = new { name = linkType },
                inwardIssue = new { key = inwardIssueKey },
                outwardIssue = new { key = outwardIssueKey }
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Send the request
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error creating issue link: {response.StatusCode} - {errorContent}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Makes an issue a subtask of another issue
        /// </summary>
        /// <param name="subtaskKey">The key of the subtask (e.g., "NCCF-12345")</param>
        /// <param name="parentKey">The key of the parent task (e.g., "NCCF-67890")</param>
        /// <returns>True if the subtask relationship was created successfully</returns>
        public async Task<bool> SetParentTaskAsync(string subtaskKey, string parentKey)
        {
            var url = $"{_baseUrl}/rest/api/2/issue/{subtaskKey}";

            // Create the update body with the parent link
            var fields = new Dictionary<string, object>
            {
                { "parent", new { key = parentKey } }
            };

            var requestBody = new { fields };
            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Send the request
            var response = await _httpClient.PutAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error setting parent task: {response.StatusCode} - {errorContent}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets all available components for a project
        /// </summary>
        /// <param name="projectKey">The project key (e.g., "NCCF")</param>
        /// <returns>Dictionary of component names and IDs</returns>
        public async Task<Dictionary<string, string>> GetAvailableComponentsAsync(string projectKey)
        {
            var url = $"{_baseUrl}/rest/api/2/project/{projectKey}/components";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error getting components: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var components = JsonConvert.DeserializeObject<List<JiraComponent>>(responseContent);

            var result = new Dictionary<string, string>();
            foreach (var component in components)
            {
                result.Add(component.Name, component.Id);
            }

            return result;
        }

        /// <summary>
        /// Gets the issue type for a specific issue
        /// </summary>
        /// <param name="issueKey">Key of the issue (e.g., "NC-1234")</param>
        /// <returns>Issue type name</returns>
        public async Task<string> GetIssueTypeAsync(string issueKey)
        {
            var url = $"{_baseUrl}/rest/api/2/issue/{issueKey}?fields=issuetype";

            // Send the request
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error getting issue type for Jira issue {issueKey}: {response.StatusCode} - {errorContent}");
            }

            // Parse the response to get the issue type
            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic responseObj = JsonConvert.DeserializeObject(responseContent);

            return responseObj.fields.issuetype.name.ToString();
        }

        /// <summary>
        /// Gets detailed information about available transitions including target statuses
        /// </summary>
        /// <param name="issueKey">Key of the issue (e.g., "NC-1234")</param>
        /// <returns>Dictionary mapping transition names to transition details</returns>
        public async Task<Dictionary<string, TransitionDetails>> GetDetailedTransitionsAsync(string issueKey)
        {
            var url = $"{_baseUrl}/rest/api/2/issue/{issueKey}/transitions?expand=transitions.fields";

            // Send the request
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error getting detailed transitions for Jira issue {issueKey}: {response.StatusCode} - {errorContent}");
            }

            // Parse the response to get the transitions with details
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObj = JsonConvert.DeserializeObject<JiraTransitionsResponse>(responseContent);

            var result = new Dictionary<string, TransitionDetails>();
            foreach (var transition in responseObj.Transitions)
            {
                var details = new TransitionDetails
                {
                    Id = transition.Id,
                    Name = transition.Name,
                    ToStatusName = transition.To?.Name ?? "Unknown",
                    ToStatusId = transition.To?.Id ?? "unknown"
                };
                result[transition.Name] = details;
            }

            return result;
        }
    }

    #region Response Classes

    internal class JiraCreateIssueResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("self")]
        public string Self { get; set; }
    }

    internal class JiraTransition
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("to")]
        public JiraStatus To { get; set; }
    }

    internal class JiraStatus
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    internal class JiraTransitionsResponse
    {
        [JsonProperty("transitions")]
        public List<JiraTransition> Transitions { get; set; }
    }

    internal class JiraComponent
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class TransitionDetails
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ToStatusName { get; set; }
        public string ToStatusId { get; set; }
    }

    #endregion
}
