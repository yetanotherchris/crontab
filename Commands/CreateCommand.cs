using System.CommandLine;
using Spectre.Console;
using TaskSchedulerCron.Services;

namespace TaskSchedulerCron.Commands;

public class CreateCommand
{
    private readonly ITaskSchedulerService _taskScheduler;

    public CreateCommand(ITaskSchedulerService taskScheduler)
    {
        _taskScheduler = taskScheduler;
    }

    public Command CreateCommand()
    {
        var nameArgument = new Argument<string>(
            name: "name",
            description: "The name of the task to create");

        var commandArgument = new Argument<string>(
            name: "command",
            description: "The command or executable to run");

        var scheduleOption = new Option<string>(
            aliases: new[] { "--schedule", "-s" },
            description: "Schedule in cron format (e.g., '0 9 * * *') or simple format (daily, hourly, weekly, monthly, boot, logon)")
        {
            IsRequired = true
        };

        var argsOption = new Option<string>(
            aliases: new[] { "--args", "-a" },
            description: "Arguments to pass to the command",
            getDefaultValue: () => string.Empty);

        var descriptionOption = new Option<string>(
            aliases: new[] { "--description", "-d" },
            description: "Description of the task");

        var createCommand = new Command("create", "Create a new scheduled task (similar to adding a crontab entry)");
        createCommand.AddArgument(nameArgument);
        createCommand.AddArgument(commandArgument);
        createCommand.AddOption(scheduleOption);
        createCommand.AddOption(argsOption);
        createCommand.AddOption(descriptionOption);

        createCommand.SetHandler(Execute, nameArgument, commandArgument, scheduleOption, argsOption, descriptionOption);

        return createCommand;
    }

    private void Execute(string name, string command, string schedule, string args, string? description)
    {
        try
        {
            AnsiConsole.Status()
                .Start($"Creating task '{name}'...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    _taskScheduler.CreateTask(name, command, args, schedule, description);
                });

            var panel = new Panel(GenerateTaskSummary(name, command, args, schedule, description))
            {
                Header = new PanelHeader("[bold green]✓ Task Created Successfully[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(2, 1)
            };

            AnsiConsole.Write(panel);
            AnsiConsole.MarkupLine($"\n[dim]Use 'view {name}' to see full task details.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error creating task: {ex.Message}[/]");
        }
    }

    private string GenerateTaskSummary(string name, string command, string args, string schedule, string? description)
    {
        var summary = new List<string>
        {
            $"[bold]Name:[/] {name}",
            $"[bold]Command:[/] {command}",
        };

        if (!string.IsNullOrWhiteSpace(args))
        {
            summary.Add($"[bold]Arguments:[/] {args}");
        }

        summary.Add($"[bold]Schedule:[/] {schedule}");

        if (!string.IsNullOrWhiteSpace(description))
        {
            summary.Add($"[bold]Description:[/] {description}");
        }

        return string.Join("\n", summary);
    }
}
