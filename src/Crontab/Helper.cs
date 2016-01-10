using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crontab
{
	public class Helper
	{
		public static void WriteConsoleColor(string text, ConsoleColor color = ConsoleColor.White)
		{
			ConsoleColor oldColor = Console.ForegroundColor;

			Console.ForegroundColor = color;
			Console.Write(text);
			Console.ForegroundColor = oldColor;
		}
	}
}
