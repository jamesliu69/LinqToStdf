using System;

namespace Stdf.RecordConverting
{
	public class ConverterLog
	{
		public static bool IsLogging { get => MessageLogged != null; }

		public static event Action<string> MessageLogged;

		public static void Log(string msg) => MessageLogged?.Invoke(msg);
	}
}