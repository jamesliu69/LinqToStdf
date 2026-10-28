using System;
using System.IO;
using System.Windows.Forms;
using STDF;

namespace StdfReader
{
    internal static class Program
    {
        /// <summary>應用程式進入點；啟動 MainForm，並將命令列參數一併傳入（供開檔使用）。</summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Application.ThreadException += (s, e) => HandleFatal("UI.Unhandled", e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => { Environment.ExitCode = 1; SafeLog("FATAL", "AppDomain.Unhandled", "application=StdfReader terminating=" + e.IsTerminating, e.ExceptionObject as Exception); };
            try { TraceLogger.Log("INFO", "Application.Start", "application=StdfReader args=" + (args == null ? 0 : args.Length)); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new MainForm(args)); }
            catch (Exception ex) { HandleFatal("Application.StartFailed", ex); }
            finally { SafeLog("INFO", "Application.Stop", "application=StdfReader", null); }
        }

        private static void HandleFatal(string operation, Exception ex)
        {
            Environment.ExitCode = 1;
            SafeLog("FATAL", operation, "application=StdfReader", ex);
            try { MessageBox.Show("STDF Reader 發生錯誤，應用程式即將關閉。\n\n" + ex?.Message, "STDF Reader", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch { }
            try { Application.Exit(); } catch { }
        }

        private static void SafeLog(string level, string operation, string details, Exception ex)
        {
            try { TraceLogger.Log(level, operation, details, ex); } catch { try { System.Diagnostics.Debug.WriteLine("STDF Reader: failed to write application log."); } catch { } }
        }
    }
}
