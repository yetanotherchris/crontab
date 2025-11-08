using System.CommandLine;
using Spectre.Console;
using TaskSchedulerCron.Services;

namespace TaskSchedulerCron.Commands;

public class ViewCommand
{
    private readonly ITaskSchedulerService _taskScheduler;

    public ViewCommand(ITaskSchedulerService taskScheduler)
    {
        _taskScheduler = taskScheduler;
    }

    public Command CreateCommand()
    {
        var nameArgument = new Argument<string>(
            name: "name",
            description: "The name of the task to view");

        var viewCommand = new Command("view", "View detailed information about a specific task");
        viewCommand.AddArgument(nameArgument);

        viewCommand.SetHandler(Execute, nameArgument);

        return viewCommand;
    }

    private void Execute(string name)
    {
        try
        {
            var task = _taskScheduler.GetTask(name);

            if (task == null)
            {
                AnsiConsole.MarkupLine($"[red]Task '{name}' not found.[/]");
                return;
            }

            var panel = new Panel(GenerateTaskDetails(task))
            {
                Header = new PanelHeader($"[bold blue]Task: {task.Name}[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(2, 1)
            };

            AnsiConsole.Write(panel);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error viewing task: {ex.Message}[/]");
        }
    }

    private string GenerateTaskDetails(TaskInfo task)
    {
        var details = new List<string>
        {
            $"[bold]Path:[/] {task.Path}",
            $"[bold]State:[/] {GetColoredState(task.State)}",
            $"[bold]Enabled:[/] {(task.Enabled ? "[green]Yes[/]" : "[red]No[/]")}",
            $"[bold]Description:[/] {task.Description ?? "[dim]No description[/]"}",
            ""
        };

        if (task.LastRunTime != DateTime.MinValue)
        {
            details.Add($"[bold]Last Run:[/] {task.LastRunTime:yyyy-MM-dd HH:mm:ss}");
        }

        if (task.NextRunTime != DateTime.MinValue)
        {
            details.Add($"[bold]Next Run:[/] {task.NextRunTime:yyyy-MM-dd HH:mm:ss}");
        }

        if (task.Triggers != null && task.Triggers.Any())
        {
            details.Add("");
            details.Add("[bold underline]Triggers:[/]");
            foreach (var trigger in task.Triggers)
            {
                details.Add($"  • [cyan]{trigger.Type}[/]");
                details.Add($"    Enabled: {(trigger.Enabled ? "[green]Yes[/]" : "[red]No[/]")}");
                if (trigger.StartBoundary != DateTime.MinValue)
                {
                    details.Add($"    Start: {trigger.StartBoundary:yyyy-MM-dd HH:mm:ss}");
                }
                details.Add($"    {trigger.Description}");
            }
        }

        if (task.Actions != null && task.Actions.Any())
        {
            details.Add("");
            details.Add("[bold underline]Actions:[/]");
            foreach (var action in task.Actions)
            {
                details.Add($"  • [yellow]{action.Type}[/]");
                details.Add($"    {action.Description}");
            }
        }

        return string.Join("\n", details);
    }

    private string GetColoredState(string state)
    {
        return state switch
        {
            "Running" => "[green]Running[/]",
            "Ready" => "[blue]Ready[/]",
            "Disabled" => "[red]Disabled[/]",
            _ => $"[yellow]{state}[/]"
        };
    }
}
