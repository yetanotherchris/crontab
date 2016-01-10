using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CronExpressionDescriptor;
using Microsoft.Win32.TaskScheduler;
using Action = Microsoft.Win32.TaskScheduler.Action;
using Task = Microsoft.Win32.TaskScheduler.Task;

namespace Crontab
{
	//# ┌───────────── min (0 - 59)
	//# │ ┌────────────── hour (0 - 23)
	//# │ │ ┌─────────────── day of month (1 - 31)
	//# │ │ │ ┌──────────────── month (1 - 12)
	//# │ │ │ │ ┌───────────────── day of week (0 - 6) (0 to 6 are Sunday to Saturday, or use names; 7 is Sunday, the same as 0)
	//# │ │ │ │ │
	//# │ │ │ │ │
	//# * * * * *  command to execute

	// Range:
	// e.g. hours: 21-22
	// Use trigger.EndBoundary

	// Lists:
	// 15,20,35 minutes
	// MON,FRI,SUN
	// These are multiple triggers.

	public class TaskToCronExpressionConverter
	{
		private readonly TextWriter _writer;
		private readonly ListOptions _listOptions;

		public TaskToCronExpressionConverter(TextWriter writer, ListOptions listOptions)
		{
			_writer = writer;
			_listOptions = listOptions;
		}

		public string ToCronExpression(Task task)
		{
			TaskDefinition definition = task.Definition;

			if (definition.Triggers.Any())
			{
				var lines = new List<string>();

				foreach (Trigger trigger in definition.Triggers)
				{
					// Windows has many-to-many for actions and triggers, cron doesn't.
					// So map them all into rows - 2 triggers and 2 actions are 4 rows.
					foreach (Action action in definition.Actions)
					{
						var cronRow = new CronRow()
						{
							Name = task.Name
						};
						cronRow.Command = GetCommandText(action);

						DateTime startDate = trigger.StartBoundary;

						cronRow.SetStartFromDate(startDate);

						if (trigger.Repetition.IsSet())
						{
							TimeSpan interval = trigger.Repetition.Interval;
							SetRepeatItems(cronRow, interval);
						}

						Helper.WriteConsoleColor($"[{cronRow.Name}] ", ConsoleColor.Green);

						if (_listOptions.Explain)
						{
							_writer.WriteLine();
							_writer.WriteLine("{0}", cronRow.Explain());
						}
						else
						{
							_writer.WriteLine("{0}", cronRow.CronString());
						}

						Helper.WriteConsoleColor(cronRow.ShortnedCommand, ConsoleColor.DarkGray);
						_writer.WriteLine();
					}
				}
			}

			return "";
		}

		private string GetCommandText(Action action)
		{
			ExecAction execAction = action as ExecAction;
			ShowMessageAction showMessageAction = action as ShowMessageAction;
			ComHandlerAction comHandlerAction = action as ComHandlerAction;
			EmailAction emailAction = action as EmailAction;

			if (execAction != null)
			{
				return $"{execAction.Path} {execAction.Arguments}";
			}
			else if (showMessageAction != null)
			{
				return $"Show message: '{showMessageAction.Title}'";
			}
			else if (comHandlerAction != null)
			{
				return $"COM handler: '{comHandlerAction.ClassName}'";
			}
			else if (emailAction != null)
			{
				return $"Send email: '{emailAction.Subject}'";
			}
			else
			{
				return "unknown action.";
			}
		}

		private void SetRepeatItems(CronRow row, TimeSpan timeSpan)
		{
			if (timeSpan.Minutes > 0 && timeSpan.Hours == 0 && timeSpan.Days == 0)
			{
				// e.g. every 15 minutes
				row.Minute.Start = timeSpan.Minutes.ToString();
				row.Minute.IsRepeating = true;
			}
			else if (timeSpan.Minutes > 0 && timeSpan.Hours > 0 && timeSpan.Days == 0)
			{
				// e.g. every 15 minutes, 3 hours
				row.Minute.Start = timeSpan.Minutes.ToString();
				row.Minute.IsRepeating = true;

				row.Hours.Start = timeSpan.Hours.ToString();
				row.Hours.IsRepeating = true;
			}
			else if (timeSpan.Minutes > 0 && timeSpan.Hours > 0 && timeSpan.Days > 0)
			{
				// e.g. every 15 minutes, 3 hours, 2nd day
				row.Minute.Start = timeSpan.Minutes.ToString();
				row.Minute.IsRepeating = true;

				row.Hours.Start = timeSpan.Hours.ToString();
				row.Hours.IsRepeating = true;

				row.DayOfMonth.Start = timeSpan.Days.ToString();
				row.DayOfMonth.IsRepeating = true;
			}
			else if (timeSpan.Minutes == 0 && timeSpan.Hours > 0 && timeSpan.Days == 0)
			{
				// e.g. every 5 hours
				row.Hours.Start = timeSpan.Hours.ToString();
				row.Hours.IsRepeating = true;
			}
			else if (timeSpan.Minutes == 0 && timeSpan.Hours > 0 && timeSpan.Days > 0)
			{
				// e.g. every 5 hours, 2 Days
				row.Hours.Start = timeSpan.Hours.ToString();
				row.Hours.IsRepeating = true;

				row.DayOfMonth.Start = timeSpan.Days.ToString();
				row.DayOfMonth.IsRepeating = true;
			}
			else if (timeSpan.Minutes == 0 && timeSpan.Hours == 0 && timeSpan.Days > 0)
			{
				// e.g. every 2nd Day
				row.DayOfMonth.Start = timeSpan.Days.ToString();
				row.DayOfMonth.IsRepeating = true;
			}
		}
	}
}
