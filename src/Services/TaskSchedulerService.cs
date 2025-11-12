using Microsoft.Win32.TaskScheduler;

namespace Crontab.Services;

public interface ITaskSchedulerService
{
    IEnumerable<TaskInfo> ListTasks();
    IEnumerable<TaskInfo> GetCronTasks();
    TaskInfo? GetTask(string name);
    void CreateTask(string name, string command, string arguments, string schedule, string? description = null, bool enableLogging = false, bool enableHidden = false);
    void DeleteTask(string name);
    void SyncCrontab(IEnumerable<CrontabEntry> entries);
    void RemoveAllCronTasks();
    void CreateFolder(string folderPath);
    void DeleteFolder(string folderPath);
}

public class TaskSchedulerService : ITaskSchedulerService, IDisposable
{
    private readonly TaskService _taskService;
    private const string CrontabFolderPath = "\\Crontab";
    private readonly string _logsDirectory;

    public TaskSchedulerService()
    {
        _taskService = new TaskService();

        // Ensure the Crontab folder exists
        try
        {
            CreateFolder(CrontabFolderPath);
        }
        catch
        {
            // Folder might already exist
        }

        // Set up logs directory at %USERPROFILE%\.crontab\logs
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var crontabDir = Path.Combine(homeDir, ".crontab");
        _logsDirectory = Path.Combine(crontabDir, "logs");

        // Ensure logs directory exists
        Directory.CreateDirectory(_logsDirectory);
    }

    public IEnumerable<TaskInfo> ListTasks()
    {
        var tasks = new List<TaskInfo>();

        foreach (var task in _taskService.AllTasks)
        {
            tasks.Add(new TaskInfo
            {
                Name = task.Name,
                Path = task.Path,
                Enabled = task.Enabled,
                State = task.State.ToString(),
                LastRunTime = task.LastRunTime,
                NextRunTime = task.NextRunTime,
                Description = task.Definition.RegistrationInfo.Description
            });
        }

        return tasks.OrderBy(t => t.Name);
    }

    public TaskInfo? GetTask(string name)
    {
        var task = _taskService.FindTask(name, true);

        if (task == null)
            return null;

        var triggers = task.Definition.Triggers
            .Select(t => new TriggerInfo
            {
                Type = t.TriggerType.ToString(),
                StartBoundary = t.StartBoundary,
                Enabled = t.Enabled,
                Description = t.ToString()
            })
            .ToList();

        var actions = task.Definition.Actions
            .Select(a => new ActionInfo
            {
                Type = a.ActionType.ToString(),
                Description = a.ToString()
            })
            .ToList();

        return new TaskInfo
        {
            Name = task.Name,
            Path = task.Path,
            Enabled = task.Enabled,
            State = task.State.ToString(),
            LastRunTime = task.LastRunTime,
            NextRunTime = task.NextRunTime,
            Description = task.Definition.RegistrationInfo.Description,
            Triggers = triggers,
            Actions = actions
        };
    }

    public void CreateTask(string name, string command, string arguments, string schedule, string? description = null, bool enableLogging = false, bool enableHidden = false)
    {
        var taskDefinition = _taskService.NewTask();
        taskDefinition.RegistrationInfo.Description = description ?? $"Task created by taskscheduler-cron: {name}";

        // Remove the default 3-day execution time limit
        taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.Zero;

        // Parse schedule and create appropriate trigger
        var trigger = ParseSchedule(schedule);
        taskDefinition.Triggers.Add(trigger);

        // Create action - wrap with logging and/or hidden window style if enabled
        if (enableLogging || enableHidden)
        {
            string wrappedCommand, wrappedArguments;
            if (enableLogging)
            {
                var logFile = Path.Combine(_logsDirectory, $"{name}.log");
                (wrappedCommand, wrappedArguments) = WrapCommandWithLogging(command, arguments, logFile, enableHidden);
            }
            else
            {
                // Only hidden, no logging
                (wrappedCommand, wrappedArguments) = WrapCommandWithHidden(command, arguments);
            }
            taskDefinition.Actions.Add(new ExecAction(wrappedCommand, wrappedArguments, null));
        }
        else
        {
            taskDefinition.Actions.Add(new ExecAction(command, arguments, null));
        }

        // Get the Crontab folder
        var folder = _taskService.GetFolder(CrontabFolderPath);

        // Register the task in the Crontab folder
        folder.RegisterTaskDefinition(
            name,
            taskDefinition,
            TaskCreation.CreateOrUpdate,
            null,
            null,
            TaskLogonType.InteractiveToken);
    }

    private (string command, string arguments) WrapCommandWithLogging(string originalCommand, string originalArguments, string logFile, bool enableHidden = false)
    {
        // Simplified logging approach: Write script to temp file and execute it
        // This avoids complex Base64 encoding and escaping issues

        var escapedLogFile = logFile.Replace("'", "''");
        var escapedCommand = originalCommand.Replace("'", "''");
        var escapedArguments = string.IsNullOrWhiteSpace(originalArguments) ? "" : originalArguments.Replace("'", "''");
        var displayCommand = string.IsNullOrWhiteSpace(escapedArguments)
            ? escapedCommand
            : $"{escapedCommand} {escapedArguments}";

        var commandLower = originalCommand.ToLowerInvariant();
        var isPowerShellScript = originalCommand.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase);
        var isPowerShellExe = commandLower.EndsWith("powershell.exe") || commandLower.EndsWith("pwsh.exe");

        // Build the command execution
        string commandExecution;
        if (enableHidden)
        {
            // For hidden execution with logging - use simplified Start-Process
            if (isPowerShellScript || isPowerShellExe)
            {
                var psArgs = isPowerShellScript
                    ? (string.IsNullOrWhiteSpace(escapedArguments)
                        ? $"-WindowStyle Hidden -ExecutionPolicy Bypass -File '{escapedCommand}'"
                        : $"-WindowStyle Hidden -ExecutionPolicy Bypass -File '{escapedCommand}' {escapedArguments}")
                    : $"-WindowStyle Hidden {escapedArguments}";
                commandExecution = $@"
    $process = Start-Process -FilePath 'powershell.exe' -ArgumentList '{psArgs}' -WindowStyle Hidden -PassThru -Wait
    $exitCode = $process.ExitCode
    if ($null -eq $exitCode) {{ $exitCode = 0 }}";
            }
            else
            {
                var startProcessArgs = string.IsNullOrWhiteSpace(escapedArguments)
                    ? $"-WindowStyle Hidden -FilePath '{escapedCommand}' -Wait"
                    : $"-WindowStyle Hidden -FilePath '{escapedCommand}' -ArgumentList '{escapedArguments}' -Wait";
                commandExecution = $@"
    $process = Start-Process {startProcessArgs} -PassThru
    $exitCode = $process.ExitCode
    if ($null -eq $exitCode) {{ $exitCode = 0 }}";
            }
        }
        else
        {
            // For normal execution, capture output
            var fullCommand = string.IsNullOrWhiteSpace(originalArguments)
                ? $"& '{originalCommand}'"
                : $"& '{originalCommand}' {originalArguments}";
            commandExecution = $@"
    $output = {fullCommand} 2>&1
    $output | ForEach-Object {{ Add-Content -Path '{escapedLogFile}' -Value $_.ToString() }}
    $exitCode = $LASTEXITCODE
    if ($null -eq $exitCode) {{ $exitCode = 0 }}";
        }

        var script = $@"
$timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
Add-Content -Path '{escapedLogFile}' -Value ""[$timestamp] Starting: {displayCommand}""
try {{{commandExecution}
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    Add-Content -Path '{escapedLogFile}' -Value ""[$timestamp] Completed with exit code: $exitCode""
    exit $exitCode
}} catch {{
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    Add-Content -Path '{escapedLogFile}' -Value ""[$timestamp] Error: $($_.Exception.Message)""
    exit 1
}}
".Trim();

        // Write script to a temp file in the logs directory
        var scriptFileName = $"{Path.GetFileNameWithoutExtension(logFile)}_wrapper.ps1";
        var scriptPath = Path.Combine(_logsDirectory, scriptFileName);
        File.WriteAllText(scriptPath, script);

        // Execute the script file
        var windowStyleArg = enableHidden ? "-WindowStyle Hidden " : "";
        return ("powershell.exe", $"-NoProfile {windowStyleArg}-ExecutionPolicy Bypass -File \"{scriptPath}\"");
    }

    private (string command, string arguments) WrapCommandWithHidden(string originalCommand, string originalArguments)
    {
        // Simplified approach: Use Start-Process -WindowStyle Hidden -FilePath directly
        var commandLower = originalCommand.ToLowerInvariant();
        var isPowerShellScript = originalCommand.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase);
        var isPowerShellExe = commandLower.EndsWith("powershell.exe") || commandLower.EndsWith("pwsh.exe");

        if (isPowerShellScript || isPowerShellExe)
        {
            // For .ps1 files or PowerShell executables, use powershell.exe -WindowStyle Hidden
            var args = isPowerShellScript
                ? $"-WindowStyle Hidden -ExecutionPolicy Bypass -File \"{originalCommand}\" {originalArguments}".Trim()
                : $"-WindowStyle Hidden {originalArguments}".Trim();
            return ("powershell.exe", args);
        }
        else
        {
            // For other executables, use Start-Process -WindowStyle Hidden -FilePath
            var startProcessArgs = string.IsNullOrWhiteSpace(originalArguments)
                ? $"-WindowStyle Hidden -FilePath \"{originalCommand}\" -Wait"
                : $"-WindowStyle Hidden -FilePath \"{originalCommand}\" -ArgumentList '{originalArguments}' -Wait";
            return ("powershell.exe", $"-Command \"Start-Process {startProcessArgs}\"");
        }
    }

    public void DeleteTask(string name)
    {
        try
        {
            var folder = _taskService.GetFolder(CrontabFolderPath);
            folder.DeleteTask(name, false);
        }
        catch
        {
            // If not found in Crontab folder, try root folder for backwards compatibility
            _taskService.RootFolder.DeleteTask(name, false);
        }
    }

    public IEnumerable<TaskInfo> GetCronTasks()
    {
        try
        {
            var folder = _taskService.GetFolder(CrontabFolderPath);
            var tasks = new List<TaskInfo>();

            foreach (var task in folder.Tasks)
            {
                tasks.Add(new TaskInfo
                {
                    Name = task.Name,
                    Path = task.Path,
                    Enabled = task.Enabled,
                    State = task.State.ToString(),
                    LastRunTime = task.LastRunTime,
                    NextRunTime = task.NextRunTime,
                    Description = task.Definition.RegistrationInfo.Description
                });
            }

            return tasks.OrderBy(t => t.Name);
        }
        catch
        {
            // Folder doesn't exist, return empty list or fallback to old behavior
            return ListTasks().Where(t => t.Name.StartsWith("cron-"));
        }
    }

    public void SyncCrontab(IEnumerable<CrontabEntry> entries)
    {
        var errors = new List<string>();

        // Get existing cron tasks
        var existingTasks = GetCronTasks().ToDictionary(t => t.Name);

        // Get task names from crontab entries
        var newTaskNames = new HashSet<string>(entries.Select(e => e.TaskName));

        // Delete tasks that are no longer in crontab
        foreach (var existingTask in existingTasks.Values)
        {
            if (!newTaskNames.Contains(existingTask.Name))
            {
                try
                {
                    DeleteTask(existingTask.Name);
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to delete task '{existingTask.Name}': {ex.Message}");
                }
            }
        }

        // Create or update tasks from crontab
        foreach (var entry in entries)
        {
            try
            {
                CreateTask(
                    entry.TaskName,
                    entry.Command,
                    entry.Arguments,
                    entry.Schedule,
                    $"Cron: {entry.Schedule} {entry.Command} {entry.Arguments}".Trim(),
                    entry.EnableLogging,
                    entry.EnableHidden);
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to create task '{entry.TaskName}': {ex.Message}");
            }
        }

        // If there were errors, throw an exception with all the error details
        if (errors.Any())
        {
            throw new Exception($"Sync completed with errors:\n{string.Join("\n", errors)}");
        }
    }

    public void RemoveAllCronTasks()
    {
        var cronTasks = GetCronTasks().ToList();
        foreach (var task in cronTasks)
        {
            try
            {
                DeleteTask(task.Name);
            }
            catch
            {
                // Ignore deletion errors
            }
        }
    }

    private Trigger ParseSchedule(string schedule)
    {
        // Try to parse as cron expression first
        if (schedule.Contains(' ') && schedule.Split(' ').Length >= 5)
        {
            try
            {
                var triggers = Trigger.FromCronFormat(schedule);
                if (triggers.Length > 0)
                {
                    return triggers[0];
                }
            }
            catch
            {
                // If cron parsing fails, fall through to simple parsing
            }
        }

        // Simple schedule parsing (daily, hourly, etc.)
        var parts = schedule.ToLowerInvariant().Split(' ');

        return parts[0] switch
        {
            "daily" or "@daily" or "@midnight" => new DailyTrigger { DaysInterval = 1 },
            "hourly" or "@hourly" => new DailyTrigger { Repetition = new RepetitionPattern(TimeSpan.FromHours(1), TimeSpan.FromDays(1)) },
            "weekly" or "@weekly" => new WeeklyTrigger { WeeksInterval = 1 },
            "monthly" or "@monthly" => new MonthlyTrigger { MonthsOfYear = MonthsOfTheYear.AllMonths, DaysOfMonth = new[] { 1 } },
            "@yearly" or "@annually" => new MonthlyTrigger { MonthsOfYear = MonthsOfTheYear.January, DaysOfMonth = new[] { 1 } },
            "boot" or "@reboot" => new BootTrigger(),
            "logon" => new LogonTrigger(),
            _ => throw new ArgumentException($"Invalid schedule format: {schedule}. Use cron format (e.g., '0 9 * * *') or shorthand (@reboot, @hourly, @daily, @midnight, @weekly, @monthly, @yearly, @annually)")
        };
    }

    public void CreateFolder(string folderPath)
    {
        try
        {
            _taskService.RootFolder.CreateFolder(folderPath);
        }
        catch (Exception ex)
        {
            // Folder might already exist - check if it's an "already exists" error
            if (!ex.Message.Contains("already exists"))
            {
                throw;
            }
        }
    }

    public void DeleteFolder(string folderPath)
    {
        _taskService.RootFolder.DeleteFolder(folderPath, false);
    }

    public void Dispose()
    {
        _taskService?.Dispose();
    }
}

public class TaskInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTime LastRunTime { get; set; }
    public DateTime NextRunTime { get; set; }
    public string? Description { get; set; }
    public List<TriggerInfo>? Triggers { get; set; }
    public List<ActionInfo>? Actions { get; set; }
}

public class TriggerInfo
{
    public string Type { get; set; } = string.Empty;
    public DateTime StartBoundary { get; set; }
    public bool Enabled { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ActionInfo
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
