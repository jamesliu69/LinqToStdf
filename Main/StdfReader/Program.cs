using System;
using System.IO;
using System.Windows.Forms;

namespace StdfReader
{
    internal static class Program
    {
        /// <summary>應用程式進入點；啟動 MainForm，並將命令列參數一併傳入（供開檔使用）。</summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(args));
        }
    }
}
