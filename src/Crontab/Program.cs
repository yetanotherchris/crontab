using System;
using CommandLine;
using CommandLine.Text;

namespace Crontab
{
	// http://taskscheduler.codeplex.com/wikipage?title=TriggerSamples&referringTitle=Classes%20Overview
	// https://github.com/yetanotherchris/kelpie/tree/master/src/Kelpie.Core/Console
	// https://github.com/gsscoder/commandline
	// http://stackoverflow.com/questions/132971/what-is-the-windows-version-of-cron
	// https://github.com/bradyholt/cron-expression-descriptor
	// 	https://en.wikipedia.org/wiki/Cron#Format

	class Program
	{
		private static TaskManager _manager;

		static int Main(string[] args)
		{
			_manager = new TaskManager(Console.Out);

			var result = Parser.Default.ParseArguments<ListOptions, AddOrUpdateOptions, DeleteOptions, ShowUIOptions>(args);

			return result.MapResult(
				RunOptions(),
				RunAddOptions(),
				RunDeleteOptions(),
				RunShowUIOptions(),
				errs => 0);
		}

		private static Func<ListOptions, int> RunOptions()
		{
			return (ListOptions options) =>
			{
				_manager.List(options);
				return 0;
			};
		}

		private static Func<AddOrUpdateOptions, int> RunAddOptions()
		{
			return (AddOrUpdateOptions options) =>
			{
				_manager.AddOrUpdate(options);
				return 0;
			};
		}

		private static Func<DeleteOptions, int> RunDeleteOptions()
		{
			return (DeleteOptions options) =>
			{
				_manager.Delete(options);
				return 0;
			};
		}

		private static Func<ShowUIOptions, int> RunShowUIOptions()
		{
			return (ShowUIOptions options) =>
			{
				_manager.ShowUI();
				return 0;
			};
		}

		static string M()
		{
			return "";
		}
	}
}
