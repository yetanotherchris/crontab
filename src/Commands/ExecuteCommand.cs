using System.CommandLine;
using System.Diagnostics;
using Spectre.Console;

namespace Crontab.Commands;

public class ExecuteCommand
{
    public Command CreateCommand()
    {
        var execCommand = new Command("exec", "Execute a command with hidden window (used internally by Task Scheduler)");

        var commandOption = new Option<string>(
            aliases: new[] { "--command", "-c" },
            description: "Command to execute")
        { IsRequired = true };

        var argumentsOption = new Option<string?>(
            aliases: new[] { "--arguments", "-a" },
            description: "Arguments for the command");

        var logFileOption = new Option<string?>(
            aliases: new[] { "--log-file", "-l" },
            description: "Log file path (enables logging)");

        var usePwshOption = new Option<bool>(
            aliases: new[] { "--pwsh" },
            description: "Use PowerShell Core (pwsh.exe) instead of Windows PowerShell");

        execCommand.AddOption(commandOption);
        execCommand.AddOption(argumentsOption);
        execCommand.AddOption(logFileOption);
        execCommand.AddOption(usePwshOption);

        execCommand.SetHandler((command, arguments, logFile, usePwsh) =>
        {
            ExecuteTask(command, arguments, logFile, usePwsh);
        }, commandOption, argumentsOption, logFileOption, usePwshOption);

        return execCommand;
    }

    private void ExecuteTask(string command, string? arguments, string? logFile, bool usePwsh)
    {
        try
        {
            var exitCode = 0;

            if (!string.IsNullOrWhiteSpace(logFile))
            {
                // Execute with logging
                exitCode = ExecuteWithLogging(command, arguments, logFile, usePwsh);
            }
            else
            {
                // Execute without logging
                exitCode = ExecuteHidden(command, arguments, usePwsh);
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
