using System;
using System.Text;
using CronExpressionDescriptor;

namespace Crontab
{
	public class CronRow
	{
		public string Name { get; set; }
		public string Command { get; set; }

		public string ShortnedCommand
		{
			get
			{
				if (!string.IsNullOrEmpty(Command) && Command.Length > 80)
					return Command.Substring(0, 80) + "...";

				return Command;
			}
		}

		public CronItem Minute { get; set; }
		public CronItem Hours { get; set; }
		public CronItem DayOfMonth { get; set; }
		public CronItem Month { get; set; }
		public CronItem DayofWeek { get; set; }
		public CronItem Year { get; set; }

		public CronRow()
		{
			Minute = new CronItem();
			Hours = new CronItem();
			DayOfMonth = new CronItem();
			Month = new CronItem();
			DayofWeek = new CronItem();
			Year = new CronItem();
		}

		public void SetStartFromDate(DateTime startDate)
		{
			Minute.Start     = startDate.Minute.ToString();
			Hours.Start      = startDate.Hour.ToString();
			DayOfMonth.Start = startDate.Day.ToString();
			Month.Start      = startDate.Month.ToString();
			DayofWeek.Start  = ((int) startDate.DayOfWeek).ToString();
			Year.Start       = startDate.Year.ToString();
		}

		public string Explain()
		{
			string cron = $"{Minute} {Hours} {DayOfMonth} {Month} {DayofWeek} {Year}";
			string description = ExpressionDescriptor.GetDescription(cron);

			return $"{description}";
		}

		public string CronString()
		{
			return $"{Minute} {Hours} {DayOfMonth} {Month} {DayofWeek} {Year}";
		}
	}
}