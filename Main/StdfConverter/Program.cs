using System;
using System.Windows.Forms;
using STDF;

namespace StdfConverter
{
	internal static class Program
	{
		/// <summary>
		///     應用程式的主要進入點。
		/// </summary>
		[STAThread]
		private static void Main()
		{
			Application.ThreadException += (s, e) => HandleFatal("UI.Unhandled", e.Exception);
			AppDomain.CurrentDomain.UnhandledException += (s, e) => { Environment.ExitCode = 1; SafeLog("FATAL", "AppDomain.Unhandled", "application=StdfConverter terminating=" + e.IsTerminating, e.ExceptionObject as Exception); };
			try { TraceLogger.Log("INFO", "Application.Start", "application=StdfConverter"); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new frmMain()); }
			catch (Exception ex) { HandleFatal("Application.StartFailed", ex); }
			finally { SafeLog("INFO", "Application.Stop", "application=StdfConverter", null); }
		}

		private static void HandleFatal(string operation, Exception ex)
		{
			Environment.ExitCode = 1;
			SafeLog("FATAL", operation, "application=StdfConverter", ex);
			try { MessageBox.Show("STDF Converter 發生錯誤，應用程式即將關閉。\n\n" + ex?.Message, "STDF Converter", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch { }
			try { Application.Exit(); } catch { }
		}

		private static void SafeLog(string level, string operation, string details, Exception ex)
		{
			try { TraceLogger.Log(level, operation, details, ex); } catch { try { System.Diagnostics.Debug.WriteLine("STDF Converter: failed to write application log."); } catch { } }
		}
	}
}
