using System;
using System.Diagnostics;

namespace ECFramework;

public static class StringExtensions
{
    public static string Run(this string command)
    {
        ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",            // The command interpreter on Linux
            Arguments = $"-c \"{command}\"",   // Pass the command as an argument to the interpreter
            RedirectStandardOutput = true,     // Redirect standard output
            UseShellExecute = false,           // Don't use the default shell execution
            CreateNoWindow = true              // Don't create a new window for the process
        };

        Process process = new Process { StartInfo = processStartInfo };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        Console.WriteLine(output);
        return output;
    }
}
