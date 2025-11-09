using System.Security.Cryptography;
using System.Text;

namespace Crontab.Services;

public interface ICrontabService
{
    string GetCrontabFilePath();
    IEnumerable<CrontabEntry> ReadCrontab();
    void WriteCrontab(IEnumerable<CrontabEntry> entries);
    void ClearCrontab();
    string GetCrontabContent();
    void OpenEditor();
}

public class CrontabService : ICrontabService
{
    private const string CrontabFileName = ".crontab";
    private readonly string _crontabPath;

    public CrontabService()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _crontabPath = Path.Combine(homeDir, CrontabFileName);
    }

    public string GetCrontabFilePath() => _crontabPath;

    public IEnumerable<CrontabEntry> ReadCrontab()
    {
        if (!File.Exists(_crontabPath))
        {
            return Enumerable.Empty<CrontabEntry>();
        }

        var entries = new List<CrontabEntry>();
        var lines = File.ReadAllLines(_crontabPath);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            var entry = ParseCrontabLine(trimmed);
            if (entry != null)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    public void WriteCrontab(IEnumerable<CrontabEntry> entries)
    {
        var lines = new List<string>
        {
            "# Crontab file for Windows Task Scheduler",
            "# Format: minute hour day month day-of-week command [arguments...]",
            "# Examples:",
            "#   0 9 * * * C:\\scripts\\backup.bat",
            "#   */15 * * * * powershell.exe -File C:\\scripts\\check.ps1",
            ""
        };

        foreach (var entry in entries)
        {
            lines.Add($"{entry.Schedule} {entry.Command} {entry.Arguments}".Trim());
        }

        File.WriteAllLines(_crontabPath, lines);
    }

    public void ClearCrontab()
    {
        if (File.Exists(_crontabPath))
        {
            File.Delete(_crontabPath);
        }
    }

    public string GetCrontabContent()
    {
        if (!File.Exists(_crontabPath))
        {
            return string.Empty;
        }

        return File.ReadAllText(_crontabPath);
    }

    public void OpenEditor()
    {
        // Ensure file exists
        if (!File.Exists(_crontabPath))
        {
            WriteCrontab(Enumerable.Empty<CrontabEntry>());
        }

        // Try to find a suitable editor
        var editor = GetEditor();

        var processInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = editor,
            Arguments = $"\"{_crontabPath}\"",
            UseShellExecute = true
        };

        var process = System.Diagnostics.Process.Start(processInfo);
        process?.WaitForExit();
    }

    private string GetEditor()
    {
        // Check for VISUAL environment variable (preferred)
        var editor = Environment.GetEnvironmentVariable("VISUAL");
        if (!string.IsNullOrWhiteSpace(editor))
        {
            return editor;
        }

        // Check for EDITOR environment variable
        editor = Environment.GetEnvironmentVariable("EDITOR");
        if (!string.IsNullOrWhiteSpace(editor))
        {
            return editor;
        }

        // Default to notepad
        return "notepad.exe";
    }

    private CrontabEntry? ParseCrontabLine(string line)
    {
        // Split by whitespace, but preserve quoted strings
        var parts = SplitCrontabLine(line);

        if (parts.Length < 2)
        {
            return null; // Invalid format
        }

        // Check if this is a special schedule (e.g., @hourly, @daily, etc.)
        var isSpecialSchedule = parts[0].StartsWith('@');

        string schedule;
        string command;
        string arguments;

        if (isSpecialSchedule)
        {
            // Special schedule: @hourly command [arguments...]
            schedule = parts[0];
            command = parts[1];
            arguments = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : string.Empty;
        }
        else
        {
            // Standard cron format: minute hour day month day-of-week command [arguments...]
            if (parts.Length < 6)
            {
                return null; // Invalid format for standard cron
            }

            schedule = string.Join(" ", parts.Take(5));
            command = parts[5];
            arguments = parts.Length > 6 ? string.Join(" ", parts.Skip(6)) : string.Empty;
        }

        // Check if command starts with @log prefix
        var enableLogging = false;
        if (command.StartsWith("@log", StringComparison.OrdinalIgnoreCase))
        {
            enableLogging = true;
            command = command.Substring(4).TrimStart();

            // If command is now empty, it means @log was followed by a space and the actual command is in arguments
            if (string.IsNullOrWhiteSpace(command) && !string.IsNullOrWhiteSpace(arguments))
            {
                var argParts = arguments.Split(new[] { ' ' }, 2);
                command = argParts[0];
                arguments = argParts.Length > 1 ? argParts[1] : string.Empty;
            }
        }

        // Generate a unique task name based on the entry
        var taskName = GenerateTaskName(schedule, command, arguments);

        return new CrontabEntry
        {
            TaskName = taskName,
            Schedule = schedule,
            Command = command,
            Arguments = arguments,
            OriginalLine = line,
            EnableLogging = enableLogging
        };
    }

    private string[] SplitCrontabLine(string line)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
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

    private string GenerateTaskName(string schedule, string command, string arguments)
    {
        // Create a unique identifier based on the command content
        var content = $"{schedule}|{command}|{arguments}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        var hashString = Convert.ToHexString(hash).Substring(0, 8);

        // Use a readable prefix with the hash
        var commandName = Path.GetFileNameWithoutExtension(command);
        return $"cron-{commandName}-{hashString}".Replace(" ", "-");
    }
}

public class CrontabEntry
{
    public string TaskName { get; set; } = string.Empty;
    public string Schedule { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string OriginalLine { get; set; } = string.Empty;
    public bool EnableLogging { get; set; } = false;
}
