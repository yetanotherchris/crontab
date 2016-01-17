using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using CronExpressionDescriptor;
using Microsoft.Win32.TaskScheduler;
using Quartz;

namespace Crontab
{
	public class TaskManager
	{
		private readonly TextWriter _writer;

		public TaskManager(TextWriter writer)
		{
			_writer = writer;
		}

		public void AddOrUpdate(AddOrUpdateOptions addOrUpdateOptions)
		{
			try
			{
				string name = addOrUpdateOptions.Name;
				string description = addOrUpdateOptions.Description;

				if (string.IsNullOrEmpty(name))
					name = "Run " + addOrUpdateOptions.Command;

				if (string.IsNullOrEmpty(description))
					description = "Run " + addOrUpdateOptions.Command;

				using (TaskService service = new TaskService())
				{
					bool exists = service.AllTasks.Any(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

					Trigger[] triggers = Trigger.FromCronFormat(addOrUpdateOptions.Expression); // doesn't support "*/5" for minutes/hours yet.

					string workingDirectory = addOrUpdateOptions.WorkingDirectory ?? Path.GetDirectoryName(addOrUpdateOptions.Command);
					var action = new ExecAction(addOrUpdateOptions.Command, addOrUpdateOptions.Arguments, workingDirectory);
					
					TaskDefinition taskDefinition = service.NewTask();
					taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;
					taskDefinition.Principal.LogonType = TaskLogonType.InteractiveTokenOrPassword;
					taskDefinition.RegistrationInfo.Description = description;
					taskDefinition.Triggers.AddRange(triggers);
					taskDefinition.Actions.Add(action);

					service.RootFolder.RegisterTaskDefinition(name, taskDefinition);

					string outputStatusText = (exists) ? "updated" : "created";

					Helper.WriteConsoleColor($"'{name}' {outputStatusText}:", ConsoleColor.Green);
					Console.WriteLine();
					Console.WriteLine(ExpressionDescriptor.GetDescription(addOrUpdateOptions.Expression));
				}
			}
			catch (Exception ex)
			{
				Helper.WriteConsoleColor("An error occurred: " + ex, ConsoleColor.Red);
				Console.WriteLine();
			}
		}

		public void List(ListOptions listOptions)
		{
			var converter = new TaskToCronExpressionConverter(_writer, listOptions);
			using (TaskService service = new TaskService())
			{
				var allTasks = service.AllTasks;

				if (!string.IsNullOrEmpty(listOptions.Filter))
				{
					allTasks = allTasks.Where(x => !string.IsNullOrEmpty(x.Name) && x.Name.ToLowerInvariant().Contains(listOptions.Filter.ToLowerInvariant()));
				}

				if (listOptions.UserOnly)
				{
					// InteractiveTokenOrPassword doesn't work for this.
					allTasks = allTasks.Where(x => x.Definition.Principal.LogonType == TaskLogonType.InteractiveToken || x.Definition.Principal.LogonType == TaskLogonType.Password);
				}

				foreach (Task task in allTasks.OrderBy(x => x.Name))
				{
					converter.ToCronExpression(task);
				}
			}
		}

		public void Delete(DeleteOptions deleteOptions)
		{
			try
			{
				using (TaskService service = new TaskService())
				{
					Task task = service.AllTasks.FirstOrDefault(x => x.Name.Equals(deleteOptions.Name, StringComparison.CurrentCultureIgnoreCase));

					if (task != null)
					{
						task.Folder.DeleteTask(task.Name);

						Helper.WriteConsoleColor($"The task '{deleteOptions.Name}' was deleted.", ConsoleColor.Green);
						Console.WriteLine();
					}
					else
					{
						Helper.WriteConsoleColor($"A task named '{deleteOptions.Name}' could not be found", ConsoleColor.Red);
						Console.WriteLine();
					}
				}
			}
			catch (Exception ex)
			{
				Helper.WriteConsoleColor("An error occurred: " + ex, ConsoleColor.Red);
				Console.WriteLine();
			}
		}

		public void ShowUI()
		{
			using (TaskService service = new TaskService())
			{
				service.StartSystemTaskSchedulerManager();

				Helper.WriteConsoleColor("UI opened.", ConsoleColor.Green);
				Console.WriteLine();
			}
		}
	}
}