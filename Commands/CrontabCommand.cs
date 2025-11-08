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
                ShowHelp();
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

    private void ShowHelp()
    {
        AnsiConsole.MarkupLine("[bold yellow]Usage: crontab [-l | -e | -r][/]");
        AnsiConsole.MarkupLine("");

        AnsiConsole.MarkupLine("[bold]Options:[/]");
        AnsiConsole.MarkupLine("  [cyan]-l, --list[/]     List all cron jobs");
        AnsiConsole.MarkupLine("  [cyan]-e, --edit[/]     Edit crontab file");
        AnsiConsole.MarkupLine("  [cyan]-r, --remove[/]   Remove all cron jobs");
        AnsiConsole.MarkupLine("");

        AnsiConsole.MarkupLine("[bold]Crontab Format:[/]");
        AnsiConsole.MarkupLine("");
        Console.WriteLine("  * * * * * command");
        Console.WriteLine("  │ │ │ │ │");
        Console.WriteLine("  │ │ │ │ └─ day of week (0–7, 0 and 7 = Sunday)");
        Console.WriteLine("  │ │ │ └─── month (1–12)");
        Console.WriteLine("  │ │ └───── day of month (1–31)");
        Console.WriteLine("  │ └─────── hour (0–23)");
        Console.WriteLine("  └───────── minute (0–59)");
        AnsiConsole.MarkupLine("");

        AnsiConsole.MarkupLine("[bold]Examples:[/]");
        AnsiConsole.MarkupLine("  [dim]# Run backup every day at 3 AM[/]");
        AnsiConsole.MarkupLine("  [green]0 3 * * *[/] C:\\scripts\\backup.bat");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("  [dim]# Sync to cloud storage daily at 3 AM[/]");
        AnsiConsole.MarkupLine("  [green]0 3 * * *[/] rclone sync C:\\data remote:s3-backup --log-file=C:\\logs\\rclone.log");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("  [dim]# Check status every 15 minutes[/]");
        AnsiConsole.MarkupLine("  [green]*/15 * * * *[/] powershell.exe -File C:\\scripts\\status.ps1");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("  [dim]# Weekly report on Monday at 9 AM[/]");
        AnsiConsole.MarkupLine("  [green]0 9 * * 1[/] C:\\scripts\\weekly-report.bat");
        AnsiConsole.MarkupLine("");

        AnsiConsole.MarkupLine("[dim]Run 'crontab -e' to edit your scheduled jobs[/]");
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
