using System;
using System.Collections.Generic;
using System.Reflection;
using CommandLine;
using CommandLine.Text;

namespace Crontab
{
	[Verb("delete", HelpText = "Deletes a scheduled task")]
	public class DeleteOptions
	{
		[Option('n', "name", Required = true, HelpText = "The name of the task to delete.")]
		public string Name { get; set; }

		[Usage(ApplicationAlias = "crontab.exe")]
		public static IEnumerable<Example> GetUsage
		{
			get
			{
				yield return new Example("Delete a task named 'Run notepad.exe'", new DeleteOptions() { Name = "Run notepad.exe" });
			}
		}
	}
}