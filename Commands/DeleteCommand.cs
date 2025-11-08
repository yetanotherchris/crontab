using System.CommandLine;
using Spectre.Console;
using TaskSchedulerCron.Services;

namespace TaskSchedulerCron.Commands;

public class DeleteCommand
{
    private readonly ITaskSchedulerService _taskScheduler;

    public DeleteCommand(ITaskSchedulerService taskScheduler)
    {
        _taskScheduler = taskScheduler;
    }

    public Command CreateCommand()
    {
        var nameArgument = new Argument<string>(
            name: "name",
            description: "The name of the task to delete");

        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f" },
            description: "Force deletion without confirmation",
            getDefaultValue: () => false);

        var deleteCommand = new Command("delete", "Delete a scheduled task");
        deleteCommand.AddAlias("rm");
        deleteCommand.AddArgument(nameArgument);
        deleteCommand.AddOption(forceOption);

        deleteCommand.SetHandler(Execute, nameArgument, forceOption);

        return deleteCommand;
    }

    private void Execute(string name, bool force)
    {
        try
        {
            // Check if task exists
            var task = _taskScheduler.GetTask(name);
            if (task == null)
            {
                AnsiConsole.MarkupLine($"[red]Task '{name}' not found.[/]");
                return;
            }

            // Confirm deletion unless force flag is used
            if (!force)
            {
                var confirm = AnsiConsole.Confirm($"Are you sure you want to delete task '[yellow]{name}[/]'?");
                if (!confirm)
                {
                    AnsiConsole.MarkupLine("[yellow]Deletion cancelled.[/]");
                    return;
                }
            }

            AnsiConsole.Status()
                .Start($"Deleting task '{name}'...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    _taskScheduler.DeleteTask(name);
                });

            AnsiConsole.MarkupLine($"[green]✓ Task '{name}' deleted successfully.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error deleting task: {ex.Message}[/]");
        }
    }
}
