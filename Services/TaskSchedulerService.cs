using Microsoft.Win32.TaskScheduler;

namespace TaskSchedulerCron.Services;

public interface ITaskSchedulerService
{
    IEnumerable<TaskInfo> ListTasks();
    TaskInfo? GetTask(string name);
    void CreateTask(string name, string command, string arguments, string schedule, string? description = null);
    void DeleteTask(string name);
}

public class TaskSchedulerService : ITaskSchedulerService, IDisposable
{
    private readonly TaskService _taskService;

    public TaskSchedulerService()
    {
        _taskService = new TaskService();
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

    public void CreateTask(string name, string command, string arguments, string schedule, string? description = null)
    {
        var taskDefinition = _taskService.NewTask();
        taskDefinition.RegistrationInfo.Description = description ?? $"Task created by taskscheduler-cron: {name}";

        // Parse schedule and create appropriate trigger
        var trigger = ParseSchedule(schedule);
        taskDefinition.Triggers.Add(trigger);

        // Create action
        taskDefinition.Actions.Add(new ExecAction(command, arguments, null));

        // Register the task
        _taskService.RootFolder.RegisterTaskDefinition(
            name,
            taskDefinition,
            TaskCreation.CreateOrUpdate,
            null,
            null,
            TaskLogonType.InteractiveToken);
    }

    public void DeleteTask(string name)
    {
        _taskService.RootFolder.DeleteTask(name, false);
    }

    private Trigger ParseSchedule(string schedule)
    {
        // Try to parse as cron expression first
        if (schedule.Contains(' ') && schedule.Split(' ').Length >= 5)
        {
            try
            {
                return Trigger.FromCronFormat(schedule);
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
            "daily" => new DailyTrigger { DaysInterval = 1 },
            "hourly" => new DailyTrigger { Repetition = new RepetitionPattern(TimeSpan.FromHours(1), TimeSpan.FromDays(1)) },
            "weekly" => new WeeklyTrigger { WeeksInterval = 1 },
            "monthly" => new MonthlyTrigger { MonthsOfYear = MonthsOfTheYear.AllMonths, DaysOfMonth = new[] { 1 } },
            "boot" => new BootTrigger(),
            "logon" => new LogonTrigger(),
            _ => throw new ArgumentException($"Invalid schedule format: {schedule}. Use cron format (e.g., '0 9 * * *') or simple format (daily, hourly, weekly, monthly, boot, logon)")
        };
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
