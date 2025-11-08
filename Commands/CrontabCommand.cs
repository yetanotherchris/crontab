using System.CommandLine;
using Spectre.Console;
using TaskSchedulerCron.Services;

namespace TaskSchedulerCron.Commands;

public class CrontabCommand
{
    private readonly ITaskSchedulerService _taskScheduler;
    private readonly ICrontabService _crontabService;

    public CrontabCommand(ITaskSchedulerService taskScheduler, ICrontabService crontabService)
    {
        _taskScheduler = taskScheduler;
        _crontabService = crontabService;
    }

    public Command CreateCommand()
    {
        var crontabCommand = new Command("crontab", "Manage scheduled tasks using cron-style interface");

        var listOption = new Option<bool>(
            aliases: new[] { "--list", "-l" },
            description: "List all cron jobs");

        var editOption = new Option<bool>(
            aliases: new[] { "--edit", "-e" },
            description: "Edit crontab file");

        var removeOption = new Option<bool>(
            aliases: new[] { "--remove", "-r" },
            description: "Remove all cron jobs");

        crontabCommand.AddOption(listOption);
        crontabCommand.AddOption(editOption);
        crontabCommand.AddOption(removeOption);

        crontabCommand.SetHandler((list, edit, remove) =>
        {
            // Count how many options are set
            var optionsSet = new[] { list, edit, remove }.Count(x => x);

            if (optionsSet == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Usage: crontab [-l | -e | -r][/]");
                AnsiConsole.MarkupLine("");
                AnsiConsole.MarkupLine("Options:");
                AnsiConsole.MarkupLine("  [cyan]-l, --list[/]     List all cron jobs");
                AnsiConsole.MarkupLine("  [cyan]-e, --edit[/]     Edit crontab file");
                AnsiConsole.MarkupLine("  [cyan]-r, --remove[/]   Remove all cron jobs");
                return;
            }

            if (optionsSet > 1)
            {
                AnsiConsole.MarkupLine("[red]Error: Only one option can be specified at a time[/]");
                return;
            }

            if (list)
            {
                ExecuteList();
            }
            else if (edit)
            {
                ExecuteEdit();
            }
            else if (remove)
            {
                ExecuteRemove();
            }
        }, listOption, editOption, removeOption);

        return crontabCommand;
    }

    private void ExecuteList()
    {
        try
        {
            var content = _crontabService.GetCrontabContent();

            if (string.IsNullOrWhiteSpace(content))
            {
                AnsiConsole.MarkupLine("[yellow]No crontab for current user[/]");
                return;
            }

            // Display crontab content (like cron does - just raw output)
            var lines = content.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.TrimEnd();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                {
                    AnsiConsole.MarkupLine($"[dim]{trimmed}[/]");
                }
                else
                {
                    Console.WriteLine(trimmed);
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error reading crontab: {ex.Message}[/]");
        }
    }

    private void ExecuteEdit()
    {
        try
        {
            AnsiConsole.MarkupLine($"[dim]Opening editor for crontab file...[/]");

            _crontabService.OpenEditor();

            // After editing, sync with Task Scheduler
            AnsiConsole.Status()
                .Start("Syncing crontab with Task Scheduler...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    var entries = _crontabService.ReadCrontab().ToList();
                    _taskScheduler.SyncCrontab(entries);
                });

            var entries = _crontabService.ReadCrontab().ToList();
            var cronTasks = _taskScheduler.GetCronTasks().ToList();

            AnsiConsole.MarkupLine($"[green]✓ Crontab updated - {entries.Count} entries, {cronTasks.Count} tasks synchronized[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error editing crontab: {ex.Message}[/]");
        }
    }

    private void ExecuteRemove()
    {
        try
        {
            var entries = _crontabService.ReadCrontab().ToList();

            if (!entries.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No crontab to remove[/]");
                return;
            }

            // Confirm removal
            var confirm = AnsiConsole.Confirm($"Remove crontab with {entries.Count} entries?");
            if (!confirm)
            {
                AnsiConsole.MarkupLine("[yellow]Removal cancelled[/]");
                return;
            }

            AnsiConsole.Status()
                .Start("Removing crontab and associated tasks...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    _taskScheduler.RemoveAllCronTasks();
                    _crontabService.ClearCrontab();
                });

            AnsiConsole.MarkupLine("[green]✓ Crontab removed[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error removing crontab: {ex.Message}[/]");
        }
    }
}
