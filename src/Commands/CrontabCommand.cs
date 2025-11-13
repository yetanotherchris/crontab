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
        AnsiConsole.MarkupLine("  [cyan]@user[/]    Use password-based authentication for full network access");
        AnsiConsole.MarkupLine("            Enables access to: mapped drives, UNC paths, domain shares");
        AnsiConsole.MarkupLine("            Requires password stored in Windows Credential Manager");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("  [dim]Keywords can be combined: @log @user command[/]");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[bold]Behavior:[/]");
        AnsiConsole.MarkupLine("  Tasks run whether user is logged in or not (non-interactive)");
        AnsiConsole.MarkupLine("  Windows are automatically hidden to prevent flashing");
        AnsiConsole.MarkupLine("  [green]Default:[/] Tasks run with S4U authentication (no password needed)");
        AnsiConsole.MarkupLine("            ✓ Has internet access for downloads, APIs, cloud services");
        AnsiConsole.MarkupLine("            ✗ Cannot access authenticated network resources (mapped drives, UNC paths)");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[bold]Credentials:[/]");
        AnsiConsole.MarkupLine("  Only required for tasks with [cyan]@user[/] keyword:");
        AnsiConsole.MarkupLine("  - Username: Automatically detected (run [cyan]whoami[/] to see it)");
        AnsiConsole.MarkupLine("  - Password: Your Microsoft account password (or local account password)");
        AnsiConsole.MarkupLine("  - Storage: Securely stored in Windows Credential Manager");
        AnsiConsole.MarkupLine("  [dim](Most tasks don't need @user unless accessing network shares)[/]");
        AnsiConsole.MarkupLine("");

        AnsiConsole.MarkupLine("[bold]Examples:[/]");
        AnsiConsole.MarkupLine("  [dim]# Download file daily at 3 AM (default S4U, no password needed)[/]");
        AnsiConsole.MarkupLine($"  [green]0 3 * * *[/] {Markup.Escape("powershell.exe -Command \"Invoke-WebRequest https://example.com/data.json -OutFile C:\\data\\latest.json\"")}");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("  [dim]# Sync to cloud storage with logging (internet access, no password)[/]");
        AnsiConsole.MarkupLine($"  [green]0 3 * * *[/] [cyan]@log[/] {Markup.Escape("rclone sync C:\\data remote:s3-backup")}");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("  [dim]# Local file cleanup every 15 minutes (no password)[/]");
        AnsiConsole.MarkupLine($"  [green]*/15 * * * *[/] {Markup.Escape("powershell.exe -File C:\\scripts\\cleanup.ps1")}");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("  [dim]# Backup to network share (requires @user for UNC path access)[/]");
        AnsiConsole.MarkupLine($"  [green]0 2 * * *[/] [cyan]@user[/] {Markup.Escape("robocopy C:\\important \\\\server\\backups /MIR")}");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("  [dim]# Copy to mapped drive with logging (requires @user)[/]");
        AnsiConsole.MarkupLine($"  [green]0 1 * * *[/] [cyan]@log @user[/] {Markup.Escape("xcopy C:\\data Z:\\backup\\ /E /Y")}");
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

            AnsiConsole.MarkupLine($"[dim]Opening editor for crontab file...[/]");

            _crontabService.OpenEditor();

            // Read crontab entries to see if we need to create tasks
            var entries = _crontabService.ReadCrontab().ToList();

            // Check if any tasks require password (UseS4U = false means @user was specified)
            var requiresPassword = entries.Any(e => !e.UseS4U);
            string? password = null;

            if (requiresPassword)
            {
                // Check if we have stored credentials
                password = _credentialService.GetPassword(username);
                if (password == null)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[yellow]One or more tasks use [cyan]@user[/] and require password authentication.[/]");
                    AnsiConsole.MarkupLine("[yellow]Please enter your password for authenticated network access:[/]");
                    AnsiConsole.MarkupLine("[dim](This will be securely stored in Windows Credential Manager)[/]");
                    AnsiConsole.MarkupLine("[dim](This is usually your Microsoft account password, unless you're on a domain or using a local user)[/]");
                    AnsiConsole.MarkupLine("[dim](Tasks without @user don't need a password)[/]");
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
                    AnsiConsole.MarkupLine("[dim]Using stored credentials from Windows Credential Manager for @user tasks[/]");
                }
            }

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
