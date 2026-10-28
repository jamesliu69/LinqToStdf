using System;
using System.Windows.Forms;

namespace STDF
{
	internal static class Program
	{
		/// <summary>The main entry point for the application. 應用程式進入點；P2020 專案目前主要作為程式庫使用，此表單供手動轉檔測試。</summary>
		[STAThread]
		private static void Main()
		{
			// 目前 P2020 專案作為元件使用，若要直接啟動介面可取消下列註解。
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new frmMain());
		}
	}
}