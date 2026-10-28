using System;
using System.IO;

namespace STDF
{
	/// <summary>將含時間戳的追蹤訊息同時附加寫入 {應用程式目錄}\logs\stdf-trace-{日期}.log 與 Console.Error；寫檔失敗時僅輸出 Console.Error 並附失敗原因。</summary>
	public static class TraceLogger
	{
		private static readonly object SyncRoot = new object();
		internal static Func<DateTime> NowProvider = () => DateTime.Now;
		internal static Func<string> LogDirectoryProvider = () => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

		public static void WriteLine(string message)
		{
		DateTime now = NowProvider();
		string line = $"{now:yyyy-MM-dd HH:mm:ss.fff} {message}";

			try
			{
				string logDirectory = LogDirectoryProvider();
				string logFilePath  = Path.Combine(logDirectory,                          $"stdf-trace-{now:yyyy-MM-dd}.log");

				lock(SyncRoot)
				{
					Directory.CreateDirectory(logDirectory);
					File.AppendAllText(logFilePath, line + Environment.NewLine);
				}
			}
			catch(Exception ex)
			{
				try { Console.Error.WriteLine(line); Console.Error.WriteLine($"[STDF-TRACE-ERR] op=TraceLogger.WriteLine target=\"daily-log-file\" exception=\"{ex}\""); }
				catch { try { System.Diagnostics.Debug.WriteLine("[STDF-TRACE-ERR] daily log write failed; stderr unavailable."); } catch { } }
				return;
			}
			try { Console.Error.WriteLine(line); } catch { try { System.Diagnostics.Debug.WriteLine(line); } catch { } }
		}

		public static void Log(string level, string operation, string details, Exception exception = null)
		{
			WriteLine($"level={level} op=\"{operation}\" {details ?? string.Empty}{(exception == null ? string.Empty : " exception=\"" + exception + "\"")}");
		}
	}
}
