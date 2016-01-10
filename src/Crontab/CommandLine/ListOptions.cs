using System;
using System.Collections.Generic;
using System.Reflection;
using CommandLine;
using CommandLine.Text;

namespace Crontab
{
	[Verb("list", HelpText = "List all scheduled tasks in crontab format. Only supports time/dates are supported (*/ format is experimental)")]
	public class ListOptions
	{
		[Option('f', "filter", Required = false, HelpText = "Filter the list.")]
		public string Filter { get; set; }

		[Option('e', "explain", Required = false, HelpText = "Display the cron format in a human readable way.")]
		public bool Explain { get; set; }

		[Option('u', "useronly", Required = false, HelpText = "Only display tasks that require a logged in user or password.")]
		public bool UserOnly { get; set; }

		[Usage(ApplicationAlias = "crontab.exe")]
		public static IEnumerable<Example> GetUsage
		{
			get
			{
				yield return new Example("List all tasks", new ListOptions());
				yield return new Example("List all tasks with the name adobe", new ListOptions() { Filter = "adobe" });
			}
		}
	}
}