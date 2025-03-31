using System;
using System.Collections.Generic;
using System.IO;

namespace ECFramework;

public enum DebugKeys
{
    IgnorePermissions,
}

public static partial class Debug
{
    public static bool IgnorePermissions => GetValue(DebugKeys.IgnorePermissions.ToString());
}

public static partial class Debug
{
    private static Dictionary<string, string> GetVariables()
    {
        // Define the path to the 'de.bug' file
        string filePath = "de.bug";

        // Initialize a dictionary to store the key-value pairs
        Dictionary<string, string> variables = new Dictionary<string, string>();

        // Check if the file exists
        if (!File.Exists(filePath))
        {
            Console.WriteLine("The file 'de.bug' does not exist.");
            return variables; // Return an empty dictionary if file does not exist
        }

        // Read all lines from the file
        string[] lines = File.ReadAllLines(filePath);

        // Process each line
        foreach (string line in lines)
        {
            // Split the line on '='
            string[] parts = line.Split('=');

            // Check if the line has the correct format (contains '=')
            if (parts.Length == 2)
            {
                string key = parts[0].Trim();
                string value = parts[1].Trim();

                // Add the key-value pair to the dictionary
                if (!variables.ContainsKey(key))
                    variables.Add(key, value);
                else
                    Console.WriteLine($"Duplicate key found: {key}. Skipping.");
            }
            else
                Console.WriteLine($"Line does not contain '=' or has an incorrect format: {line}");
        }

        // Return the populated dictionary
        return variables;
    }

    public static bool GetValue(string key)
    {
        // Get the variables from the 'de.bug' file
        Dictionary<string, string> variables = GetVariables();

        // Check if the dictionary contains the key 'key1'
        if (variables.ContainsKey(key))
        {
            // Get the value associated with the key
            string value = variables[key];

            // Check if the value is 'true' or 'false'
            if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
                return true;
            else if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
                return false;
            else
            {
                Console.WriteLine($"Invalid value for key '{key}': {value}. Defaulting to 'false'.");
                return false;
            }
        }
        else
        {
            Console.WriteLine($"Key '{key}' not found in 'de.bug' file. Defaulting to 'false'.");
            return false;
        }
    }
}
