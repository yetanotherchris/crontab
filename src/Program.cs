using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Crontab.Commands;
using Crontab.Services;

namespace Crontab;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Enable UTF-8 encoding for emoji support
        Console.OutputEncoding = Encoding.UTF8;

        var stopwatch = Stopwatch.StartNew();

        // Setup dependency injection
        var serviceProvider = SetupIoC();

        // Create crontab command (the main command with execute option integrated)
        var crontabCommand = serviceProvider.GetRequiredService<CrontabCommand>();
        var rootCommand = crontabCommand.CreateCommand();

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

        return result;
    }

    private static ServiceProvider SetupIoC()
    {
        var services = new ServiceCollection();

        // Register services
        services.AddSingleton<ITaskSchedulerService, TaskSchedulerService>();
        services.AddSingleton<ICrontabService, CrontabService>();
        services.AddSingleton<ICredentialService, CredentialService>();

        // Register command handlers
        services.AddSingleton<CrontabCommand>();
        services.AddSingleton<ExecuteCommand>();

        return services.BuildServiceProvider();
    }
}
