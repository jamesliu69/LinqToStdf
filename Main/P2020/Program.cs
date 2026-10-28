using System;
using System.Windows.Forms;
using STDF;

namespace STDF
{
	internal static class Program
	{
		/// <summary>The main entry point for the application. 應用程式進入點；P2020 專案目前主要作為程式庫使用，此表單供手動轉檔測試。</summary>
		[STAThread]
		private static void Main()
		{
			Application.ThreadException += (s, e) => HandleFatal("UI.Unhandled", e.Exception);
			AppDomain.CurrentDomain.UnhandledException += (s, e) => { Environment.ExitCode = 1; SafeLog("FATAL", "AppDomain.Unhandled", "application=P2020 terminating=" + e.IsTerminating, e.ExceptionObject as Exception); };
			try { TraceLogger.Log("INFO", "Application.Start", "application=P2020"); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new frmMain()); }
			catch (Exception ex) { HandleFatal("Application.StartFailed", ex); }
			finally { SafeLog("INFO", "Application.Stop", "application=P2020", null); }
		}

		private static void HandleFatal(string operation, Exception ex)
		{
			Environment.ExitCode = 1;
			SafeLog("FATAL", operation, "application=P2020", ex);
			try { MessageBox.Show("P2020 發生錯誤，應用程式即將關閉。\n\n" + ex?.Message, "P2020", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch { }
			try { Application.Exit(); } catch { }
		}

		private static void SafeLog(string level, string operation, string details, Exception ex)
		{
			try { TraceLogger.Log(level, operation, details, ex); } catch { try { System.Diagnostics.Debug.WriteLine("P2020: failed to write application log."); } catch { } }
		}
	}
}
