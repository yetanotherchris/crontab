using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using TaskSchedulerCron.Commands;
using TaskSchedulerCron.Services;

namespace TaskSchedulerCron;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Enable UTF-8 encoding for emoji support
        Console.OutputEncoding = Encoding.UTF8;

        var stopwatch = Stopwatch.StartNew();

        // Setup dependency injection
        var serviceProvider = SetupIoC();

        // Create root command
        var rootCommand = new RootCommand("A command line tool for managing Windows Task Scheduler with cron-like functionality");

        // Add commands
        var listCommand = serviceProvider.GetRequiredService<ListCommand>();
        rootCommand.AddCommand(listCommand.CreateCommand());

        var viewCommand = serviceProvider.GetRequiredService<ViewCommand>();
        rootCommand.AddCommand(viewCommand.CreateCommand());

        var createCommand = serviceProvider.GetRequiredService<CreateCommand>();
        rootCommand.AddCommand(createCommand.CreateCommand());

        var deleteCommand = serviceProvider.GetRequiredService<DeleteCommand>();
        rootCommand.AddCommand(deleteCommand.CreateCommand());

        // Build command line parser
        var parser = new CommandLineBuilder(rootCommand)
            .UseVersionOption()
            .UseHelp()
            .UseEnvironmentVariableDirective()
            .UseParseDirective()
            .UseSuggestDirective()
            .RegisterWithDotnetSuggest()
            .UseTypoCorrections()
            .UseParseErrorReporting()
            .UseExceptionHandler()
            .CancelOnProcessTermination()
            .Build();

        var result = await parser.InvokeAsync(args);

        stopwatch.Stop();

        if (args.Length > 0)
        {
            AnsiConsole.MarkupLine($"[dim]Completed in {stopwatch.ElapsedMilliseconds}ms[/]");
        }

        return result;
    }

    private static ServiceProvider SetupIoC()
    {
        var services = new ServiceCollection();

        // Register services
        services.AddSingleton<ITaskSchedulerService, TaskSchedulerService>();

        // Register command handlers
        services.AddSingleton<ListCommand>();
        services.AddSingleton<ViewCommand>();
        services.AddSingleton<CreateCommand>();
        services.AddSingleton<DeleteCommand>();

        return services.BuildServiceProvider();
    }
}
