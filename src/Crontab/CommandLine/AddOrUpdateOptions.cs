using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace Crontab
{
	[Verb("add", HelpText = "Creates a new scheduled task based on crontab format.")]
	public class AddOrUpdateOptions
	{
		[Option('n', "name", Required = false, HelpText = "The name of the task. Defaults to 'Run <command>'")]
		public string Name { get; set; }

		[Option("description", Required = false, HelpText = "The description of the task. Defaults to 'Run <command>'")]
		public string Description { get; set; }

		[Option('e', "expression", Required = true, HelpText = "The crontab expression.")]
		public string Expression { get; set; }

		[Option('c', "command", Required = true, HelpText = "The command to run.")]
		public string Command { get; set; }

		[Option('a', "arguments", Required = false, HelpText = "Optional arguments for the command.")]
		public string Arguments { get; set; }

		[Option('d', "workingdirectory", Required = false, HelpText = "Optional working directory for the command.")]
		public string WorkingDirectory { get; set; }

		[Usage(ApplicationAlias = "crontab.exe")]
		public static IEnumerable<Example> GetUsage
		{
			get
			{
				yield return new Example("Run notepad every 15 minutes", new AddOrUpdateOptions() { Expression = "*/15 * * * *", Command = "notepad.exe" });
				yield return new Example("Kill all w3p processes at 16:15", new AddOrUpdateOptions() { Expression = "15 16 * * *", Command = "powershell.exe", Arguments = "kill -name w3p", Name = "Kill all processes", Description = "A long description"});
			}
		}
	}
}