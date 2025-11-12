using System.CommandLine;
using System.Diagnostics;

namespace Crontab.Commands;

public class ExecuteCommand
{
    public Option<string?> CreateExecuteOption()
    {
        return new Option<string?>(
            aliases: new[] { "--command", "-c" },
            description: "Execute a command with hidden window (internal use by Task Scheduler)");
    }

    public Option<string?> CreateLogFileOption()
    {
        return new Option<string?>(
            aliases: new[] { "--log-file" },
            description: "Log file path for command execution");
    }

    public Option<bool> CreateUsePwshOption()
    {
        return new Option<bool>(
            aliases: new[] { "--use-pwsh" },
            description: "Use PowerShell Core (pwsh.exe)");
    }

    public void ExecuteTask(string command, string? logFile, bool usePwsh)
    {
        try
        {
            // Check if command is base64 encoded (starts with "base64:")
            if (command.StartsWith("base64:", StringComparison.OrdinalIgnoreCase))
            {
                var base64String = command.Substring(7);
                var bytes = Convert.FromBase64String(base64String);
                command = System.Text.Encoding.UTF8.GetString(bytes);
            }

            // Split command into executable and arguments
            var parts = SplitCommandLine(command);
            if (parts.Length == 0)
            {
                Environment.Exit(1);
                return;
            }

            var executable = parts[0];
            var arguments = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : null;

            var exitCode = 0;

            if (!string.IsNullOrWhiteSpace(logFile))
            {
                // Execute with logging
                exitCode = ExecuteWithLogging(executable, arguments, logFile, usePwsh);
            }
            else
            {
                // Execute without logging
                exitCode = ExecuteHidden(executable, arguments, usePwsh);
            }

            Environment.Exit(exitCode);
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrWhiteSpace(logFile))
            {
                try
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    File.AppendAllText(logFile, $"[{timestamp}] Fatal error: {ex.Message}\n");
                }
                catch
                {
                    // Can't log, ignore
                }
            }

            // Exit with error code
            Environment.Exit(1);
        }
    }

    private string[] SplitCommandLine(string commandLine)
    {
        var parts = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < commandLine.Length; i++)
        {
            var c = commandLine[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ' ' && !inQuotes)
            {
                if (current.Length > 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            parts.Add(current.ToString());
        }

        return parts.ToArray();
    }

    private int ExecuteWithLogging(string command, string? arguments, string logFile, bool usePwsh)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var displayCommand = string.IsNullOrWhiteSpace(arguments)
            ? command
            : $"{command} {arguments}";

        File.AppendAllText(logFile, $"[{timestamp}] Starting: {displayCommand}\n");

        try
        {
            var psi = CreateProcessStartInfo(command, arguments, usePwsh);
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            using var process = Process.Start(psi);
            if (process == null)
            {
                File.AppendAllText(logFile, $"[{timestamp}] Error: Failed to start process\n");
                return 1;
            }

            // Read output
            var output = process.StandardOutput.ReadToEnd();
            var errorOutput = process.StandardError.ReadToEnd();

            process.WaitForExit();

            // Log output
            if (!string.IsNullOrWhiteSpace(output))
            {
                File.AppendAllText(logFile, output);
                if (!output.EndsWith("\n"))
                {
                    File.AppendAllText(logFile, "\n");
                }
            }

            if (!string.IsNullOrWhiteSpace(errorOutput))
            {
                File.AppendAllText(logFile, errorOutput);
                if (!errorOutput.EndsWith("\n"))
                {
                    File.AppendAllText(logFile, "\n");
                }
            }

            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(logFile, $"[{timestamp}] Completed with exit code: {process.ExitCode}\n");

            return process.ExitCode;
        }
        catch (Exception ex)
        {
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(logFile, $"[{timestamp}] Error: {ex.Message}\n");
            return 1;
        }
    }

    private int ExecuteHidden(string command, string? arguments, bool usePwsh)
    {
        var psi = CreateProcessStartInfo(command, arguments, usePwsh);

        using var process = Process.Start(psi);
        if (process == null)
        {
            return 1;
        }

        process.WaitForExit();
        return process.ExitCode;
    }

    private ProcessStartInfo CreateProcessStartInfo(string command, string? arguments, bool usePwsh)
    {
        var isPowerShellScript = command.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase);
        var psi = new ProcessStartInfo();

        if (isPowerShellScript)
        {
            // For PowerShell scripts, execute via powershell.exe or pwsh.exe
            var powershellExe = usePwsh ? "pwsh.exe" : "powershell.exe";
            psi.FileName = powershellExe;
            psi.Arguments = string.IsNullOrWhiteSpace(arguments)
                ? $"-NoProfile -ExecutionPolicy Bypass -File \"{command}\""
                : $"-NoProfile -ExecutionPolicy Bypass -File \"{command}\" {arguments}";
        }
        else
        {
            // For other executables
            psi.FileName = command;
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                psi.Arguments = arguments;
            }
        }

        // Critical settings to prevent any window from appearing
        psi.CreateNoWindow = true;
        psi.UseShellExecute = false;
        psi.WindowStyle = ProcessWindowStyle.Hidden;

        return psi;
    }
}
