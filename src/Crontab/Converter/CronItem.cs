namespace Crontab
{
	public class CronItem
	{
		public bool IsRepeating { get; set; }
		public string Start { get; set; }

		public CronItem()
		{
			Start = "*";
		}

		public override string ToString()
		{
			if (IsRepeating)
			{
				return $"*/{Start}";
			}

			return Start;
		}
	}
}