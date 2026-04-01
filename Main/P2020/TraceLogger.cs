using System;
using System.IO;

namespace STDF
{
	public static class TraceLogger
	{
		private static readonly object SyncRoot = new object();

		public static void WriteLine(string message)
		{
			string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}";

			try
			{
				string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
				string logFilePath  = Path.Combine(logDirectory,                          $"stdf-trace-{DateTime.Now:yyyy-MM-dd}.log");

				lock(SyncRoot)
				{
					Directory.CreateDirectory(logDirectory);
					File.AppendAllText(logFilePath, line + Environment.NewLine);
				}
			}
			catch(Exception ex)
			{
				Console.Error.WriteLine(line);
				Console.Error.WriteLine($"[STDF-TRACE-ERR] op=TraceLogger.WriteLine target=\"daily-log-file\" message=\"{ex.Message}\" stack=\"{ex.StackTrace}\"");
				return;
			}

			Console.Error.WriteLine(line);
		}
	}
}