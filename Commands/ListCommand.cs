using System.CommandLine;
using Spectre.Console;
using TaskSchedulerCron.Services;

namespace TaskSchedulerCron.Commands;

public class ListCommand
{
    private readonly ITaskSchedulerService _taskScheduler;

    public ListCommand(ITaskSchedulerService taskScheduler)
    {
        _taskScheduler = taskScheduler;
    }

    public Command CreateCommand()
    {
        var listCommand = new Command("list", "List all scheduled tasks (similar to 'crontab -l')");
        listCommand.AddAlias("ls");

        listCommand.SetHandler(Execute);

        return listCommand;
    }

    private void Execute()
    {
        try
        {
            var tasks = _taskScheduler.ListTasks().ToList();

            if (!tasks.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No scheduled tasks found.[/]");
                return;
            }

            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn(new TableColumn("[bold]Name[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold]State[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold]Enabled[/]").Centered());
            table.AddColumn(new TableColumn("[bold]Last Run[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold]Next Run[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold]Description[/]").LeftAligned());

            foreach (var task in tasks)
            {
                var enabledIcon = task.Enabled ? "[green]✓[/]" : "[red]✗[/]";
                var stateColor = task.State switch
                {
                    "Running" => "green",
                    "Ready" => "blue",
                    "Disabled" => "red",
                    _ => "yellow"
                };

                var lastRun = task.LastRunTime == DateTime.MinValue ? "-" : task.LastRunTime.ToString("yyyy-MM-dd HH:mm:ss");
                var nextRun = task.NextRunTime == DateTime.MinValue ? "-" : task.NextRunTime.ToString("yyyy-MM-dd HH:mm:ss");

                table.AddRow(
                    task.Name,
                    $"[{stateColor}]{task.State}[/]",
                    enabledIcon,
                    lastRun,
                    nextRun,
                    task.Description ?? "-"
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[dim]Total tasks: {tasks.Count}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error listing tasks: {ex.Message}[/]");
        }
    }
}
