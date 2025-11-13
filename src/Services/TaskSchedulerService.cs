using Microsoft.Win32.TaskScheduler;
using Spectre.Console;
using System.Security;

namespace Crontab.Services;

public interface ITaskSchedulerService
{
    IEnumerable<TaskInfo> ListTasks();
    IEnumerable<TaskInfo> GetCronTasks();
    TaskInfo? GetTask(string name);
    void CreateTask(string name, string command, string arguments, string schedule, string? description = null, bool enableLogging = false, string? password = null, bool useS4U = false);
    void DeleteTask(string name);
    void SyncCrontab(IEnumerable<CrontabEntry> entries, string? password = null);
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

        // Ensure directory exists
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

    public void CreateTask(string name, string command, string arguments, string schedule, string? description = null, bool enableLogging = false, string? password = null, bool useS4U = false)
    {
        var taskDefinition = _taskService.NewTask();
        taskDefinition.RegistrationInfo.Description = description ?? $"Task created by taskscheduler-cron: {name}";

        // Configure task settings
        taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.Zero;  // Remove the default 3-day execution time limit
        taskDefinition.Settings.StartWhenAvailable = false;  // Don't run missed tasks
        taskDefinition.Settings.DisallowStartIfOnBatteries = false;  // Run on battery power
        taskDefinition.Settings.StopIfGoingOnBatteries = false;  // Don't stop if switching to battery
        taskDefinition.Settings.Hidden = true;  // Hide task from Task Scheduler UI

        // Parse schedule and create appropriate trigger
        var trigger = ParseSchedule(schedule);
        taskDefinition.Triggers.Add(trigger);

        // Create action - always wrap with hidden window wrapper, optionally with logging
        string wrappedCommand, wrappedArguments;

        if (enableLogging)
        {
            var logFile = Path.Combine(_logsDirectory, $"{name}.log");
            (wrappedCommand, wrappedArguments) = WrapCommandWithLogging(command, arguments, logFile);
        }
        else
        {
            (wrappedCommand, wrappedArguments) = WrapCommandWithHidden(command, arguments);
        }

        taskDefinition.Actions.Add(new ExecAction(wrappedCommand, wrappedArguments, null));

        // Get the Crontab folder
        var folder = _taskService.GetFolder(CrontabFolderPath);

        // Delete existing task if it exists to ensure clean registration
        try
        {
            DeleteTask(name);
        }
        catch
        {
            // Task doesn't exist, that's fine
        }

        // Get fully qualified username (e.g., "COMPUTERNAME\username" or "DOMAIN\username")
        var fullyQualifiedUsername = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

        // Choose logon type based on useS4U flag
        TaskLogonType logonType;
        if (useS4U)
        {
            // S4U (Service For User): Runs whether user is logged on or not, but no network access
            logonType = TaskLogonType.S4U;
        }
        else
        {
            // Password: Runs whether user is logged on or not, with network access
            logonType = TaskLogonType.Password;
        }

        // Register the task
        folder.RegisterTaskDefinition(
            name,
            taskDefinition,
            TaskCreation.Create,
            fullyQualifiedUsername,  // Fully qualified username (COMPUTERNAME\user or DOMAIN\user)
            password,  // Use provided password (required for Password logon type, not needed for S4U)
            logonType);
    }

    private (string command, string arguments) WrapCommandWithLogging(string originalCommand, string originalArguments, string logFile)
    {
        // Use crontab.exe --command to run commands with logging and hidden window
        var crontabExe = Environment.ProcessPath ?? "crontab.exe";

        // Combine command and arguments into a single string
        var fullCommand = string.IsNullOrWhiteSpace(originalArguments)
            ? originalCommand
            : $"{originalCommand} {originalArguments}";

        // Base64 encode the command to avoid any quoting/escaping issues
        var bytes = System.Text.Encoding.UTF8.GetBytes(fullCommand);
        var base64Command = Convert.ToBase64String(bytes);

        var args = $"--command base64:{base64Command} --log-file \"{logFile}\"";

        return (crontabExe, args);
    }

    private (string command, string arguments) WrapCommandWithHidden(string originalCommand, string originalArguments)
    {
        // Use crontab.exe --command to run commands with hidden window (no logging)
        var crontabExe = Environment.ProcessPath ?? "crontab.exe";

        // Combine command and arguments into a single string
        var fullCommand = string.IsNullOrWhiteSpace(originalArguments)
            ? originalCommand
            : $"{originalCommand} {originalArguments}";

        // Base64 encode the command to avoid any quoting/escaping issues
        var bytes = System.Text.Encoding.UTF8.GetBytes(fullCommand);
        var base64Command = Convert.ToBase64String(bytes);

        var args = $"--command base64:{base64Command}";

        return (crontabExe, args);
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

    public void SyncCrontab(IEnumerable<CrontabEntry> entries, string? password = null)
    {
        var errors = new List<string>();
        var entriesList = entries.ToList();

        // Get existing cron tasks
        var existingTasks = GetCronTasks().ToDictionary(t => t.Name);

        // Get task names from crontab entries
        var newTaskNames = new HashSet<string>(entriesList.Select(e => e.TaskName));

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

        // Create or update tasks from crontab using the provided password
        foreach (var entry in entriesList)
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
                    password,
                    entry.UseS4U);
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
