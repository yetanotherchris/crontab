using System.CommandLine;
using Spectre.Console;
using Crontab.Services;

namespace Crontab.Commands;

public class CrontabCommand
{
    private readonly ITaskSchedulerService _taskScheduler;
    private readonly ICrontabService _crontabService;
    private readonly ICredentialService _credentialService;
    private readonly ExecuteCommand _executeCommand;

    public CrontabCommand(ITaskSchedulerService taskScheduler, ICrontabService crontabService, ICredentialService credentialService, ExecuteCommand executeCommand)
    {
        _taskScheduler = taskScheduler;
        _crontabService = crontabService;
        _credentialService = credentialService;
        _executeCommand = executeCommand;
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

        var executeOption = new Option<string?>(
            aliases: new[] { "--command", "-c" },
            description: "Execute a command with hidden window (internal use)");

        var logFileOption = new Option<string?>(
            aliases: new[] { "--log-file" },
            description: "Log file path for command execution");

        crontabCommand.AddOption(listOption);
        crontabCommand.AddOption(editOption);
        crontabCommand.AddOption(removeOption);
        crontabCommand.AddOption(executeOption);
        crontabCommand.AddOption(logFileOption);

        crontabCommand.SetHandler((list, edit, remove, execute, logFile) =>
        {
            // Handle execute option first (internal use by Task Scheduler)
            if (!string.IsNullOrWhiteSpace(execute))
            {
                _executeCommand.ExecuteTask(execute, logFile);
                return;
            }

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
        }, listOption, editOption, removeOption, executeOption, logFileOption);

        return crontabCommand;
    }

    private void ShowHelp()
    {
        AnsiConsole.MarkupLine($"[bold yellow]{Markup.Escape("Usage: crontab [-l | -e | -r]")}[/]");
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
        Console.WriteLine("  │ │ │ │ └─ day of week (0-7, 0 and 7 = Sunday)");
        Console.WriteLine("  │ │ │ └─── month (1-12)");
        Console.WriteLine("  │ │ └───── day of month (1-31)");
        Console.WriteLine("  │ └─────── hour (0-23)");
        Console.WriteLine("  └───────── minute (0-59)");
        AnsiConsole.MarkupLine("");

        AnsiConsole.MarkupLine("[bold]Keywords:[/]");
        AnsiConsole.MarkupLine("  [cyan]@log[/]     Capture command output to log files");
        AnsiConsole.MarkupLine($"            Logs stored in: [dim]{Markup.Escape("%USERPROFILE%\\.crontab\\logs")}[/]");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("  [cyan]@s4u[/]     Use Service-For-User authentication (no password needed)");
        AnsiConsole.MarkupLine("            [yellow]⚠ Limitations:[/] No network access (mapped drives, UNC paths, etc.)");
        AnsiConsole.MarkupLine("            Use for local tasks only (file operations, scripts without network)");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("  [dim]Keywords can be combined: @log @s4u command[/]");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[bold]Behavior:[/]");
        AnsiConsole.MarkupLine("  Tasks run whether user is logged in or not (non-interactive)");
        AnsiConsole.MarkupLine("  Windows are automatically hidden to prevent flashing");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[bold]Credentials:[/]");
        AnsiConsole.MarkupLine("  On first use, you'll be prompted for your password:");
        AnsiConsole.MarkupLine("  - Username: Automatically detected (run [cyan]whoami[/] to see it)");
        AnsiConsole.MarkupLine("  - Password: Your Microsoft account password (or local account password)");
        AnsiConsole.MarkupLine("  - Storage: Securely stored in Windows Credential Manager");
        AnsiConsole.MarkupLine("  This allows tasks to run whether you're logged in or not, with network access");
        AnsiConsole.MarkupLine("  [dim](Tasks with @s4u don't need stored credentials)[/]");
        AnsiConsole.MarkupLine("");

        AnsiConsole.MarkupLine("[bold]Examples:[/]");
        AnsiConsole.MarkupLine("  [dim]# Run backup every day at 3 AM[/]");
        AnsiConsole.MarkupLine($"  [green]0 3 * * *[/] {Markup.Escape("C:\\scripts\\backup.bat")}");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("  [dim]# Sync to cloud storage daily at 3 AM with logging[/]");
        AnsiConsole.MarkupLine($"  [green]0 3 * * *[/] [cyan]@log[/] {Markup.Escape("rclone sync C:\\data remote:s3-backup")}");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("  [dim]# Local file cleanup every 15 minutes (no network, no password)[/]");
        AnsiConsole.MarkupLine($"  [green]*/15 * * * *[/] [cyan]@s4u[/] {Markup.Escape("powershell.exe -File C:\\scripts\\cleanup.ps1")}");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("  [dim]# Weekly report on Monday at 9 AM with logging[/]");
        AnsiConsole.MarkupLine($"  [green]0 9 * * 1[/] [cyan]@log[/] {Markup.Escape("C:\\scripts\\weekly-report.bat")}");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("  [dim]# Local maintenance task with logging and no password[/]");
        AnsiConsole.MarkupLine($"  [green]0 2 * * *[/] [cyan]@log @s4u[/] {Markup.Escape("C:\\scripts\\local-maintenance.ps1")}");
        AnsiConsole.MarkupLine("");

        AnsiConsole.MarkupLine($"[dim]{Markup.Escape("Run 'crontab -e' to edit your scheduled jobs")}[/]");
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
                var trimmed = Markup.Escape(line.TrimEnd());
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                {
                    AnsiConsole.MarkupLine($"[dim]{trimmed}[/]");
                }
                else
                {
                    Console.WriteLine(trimmed);
                }
            }

            // Display task execution history
            var tasks = _taskScheduler.GetCronTasks().ToList();
            if (tasks.Any())
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]Task Execution History:[/]");
                AnsiConsole.WriteLine();

                var table = new Table();
                table.Border = TableBorder.Rounded;
                table.AddColumn("Task Name");
                table.AddColumn("Status");
                table.AddColumn("Last Run");
                table.AddColumn("Next Run");

                foreach (var task in tasks)
                {
                    var lastRun = task.LastRunTime.Year > 1900
                        ? task.LastRunTime.ToString("yyyy-MM-dd HH:mm:ss")
                        : "Never";

                    var nextRun = task.NextRunTime.Year > 1900
                        ? task.NextRunTime.ToString("yyyy-MM-dd HH:mm:ss")
                        : "Not scheduled";

                    var statusColor = task.State switch
                    {
                        "Running" => "yellow",
                        "Ready" => "green",
                        "Disabled" => "dim",
                        _ => "white"
                    };

                    table.AddRow(
                        Markup.Escape(task.Name),
                        $"[{statusColor}]{Markup.Escape(task.State)}[/]",
                        lastRun,
                        nextRun
                    );
                }

                AnsiConsole.Write(table);
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
            var username = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

            // Check if we have stored credentials, if not prompt before opening editor
            string? password = _credentialService.GetPassword(username);
            if (password == null)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]No stored credentials found.[/]");
                AnsiConsole.MarkupLine("[yellow]Please enter your password to allow tasks to run whether you're logged in or not:[/]");
                AnsiConsole.MarkupLine("[dim](This will be securely stored in Windows Credential Manager)[/]");
                AnsiConsole.MarkupLine("[dim](This is usually your Microsoft account password, unless you're on a domain or using a local user)[/]");
                AnsiConsole.MarkupLine("[dim](Tasks marked with @s4u don't need a password but won't have network access)[/]");
                AnsiConsole.WriteLine();

                password = AnsiConsole.Prompt(
                    new TextPrompt<string>("Password:")
                        .Secret());

                _credentialService.StorePassword(username, password);
                AnsiConsole.MarkupLine("[green]✓ Credentials stored securely in Windows Credential Manager[/]");
                AnsiConsole.WriteLine();
            }
            else
            {
                AnsiConsole.MarkupLine("[dim]Using stored credentials from Windows Credential Manager[/]");
            }

            AnsiConsole.MarkupLine($"[dim]Opening editor for crontab file...[/]");

            _crontabService.OpenEditor();

            // Read crontab entries to see if we need to create tasks
            var entries = _crontabService.ReadCrontab().ToList();

            if (entries.Any())
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[yellow]Creating {entries.Count} scheduled task(s)...[/]");
                AnsiConsole.MarkupLine($"[dim]Username: {Markup.Escape(username)}[/]");
            }

            // Sync with Task Scheduler using stored credentials
            AnsiConsole.Status()
                .Start("Syncing crontab with Task Scheduler...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    _taskScheduler.SyncCrontab(entries, password);
                });

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
