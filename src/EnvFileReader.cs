using System;
using System.Collections.Generic;
using System.IO;

namespace JiraTools
{
    /// <summary>
    /// Simple utility to read environment variables from a .env file
    /// </summary>
    public static class EnvFileReader
    {
        /// <summary>
        /// Reads variables from a .env file and returns them as a dictionary
        /// </summary>
        /// <param name="filePath">Path to the .env file (defaults to ".env" in current directory)</param>
        /// <returns>Dictionary of environment variables</returns>
        public static Dictionary<string, string> ReadEnvFile(string filePath = ".env")
        {
            var result = new Dictionary<string, string>();

            // Return empty dictionary if file doesn't exist
            if (!File.Exists(filePath))
                return result;

            try
            {
                string[] lines = File.ReadAllLines(filePath);

                foreach (string line in lines)
                {
                    // Skip empty lines and comments
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    // Split by first equals sign
                    int equalsPos = line.IndexOf('=');
                    if (equalsPos <= 0) continue;

                    string key = line.Substring(0, equalsPos).Trim();
                    string value = line.Substring(equalsPos + 1).Trim();

                    // Remove quotes if present
                    if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                        (value.StartsWith("'") && value.EndsWith("'")))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    result[key] = value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading .env file: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Gets a value from the .env file or returns null if not found
        /// </summary>
        public static string GetEnvValue(string key, string defaultValue = null, string filePath = ".env")
        {
            var values = ReadEnvFile(filePath);
            return values.TryGetValue(key, out string value) ? value : defaultValue;
        }
    }
}
