using STDF;
using System;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
	public partial class frmMain : Form
	{
		private const string LogTag = "[STDF-TRACE-ERR]";

		public frmMain()
		{
			InitializeComponent();
		}

		private void GenerateStdfButton_Click(object sender, EventArgs e)
		{
			// 以目前指定的 P2020 測試資料夾產生 STDF 檔案。
			string inputFolder = @"D:\P2020 Log\2023-08-26-10-53-30";
			string outputPath  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "testSTDF.stdf");
			try
			{
				CStdf stdfConverter = new CStdf(inputFolder, outputPath);
				stdfConverter.DoWork();
			}
			catch(Exception ex)
			{
				LogEntryPointException("GenerateStdfButton_Click", ex, inputFolder, outputPath);
				throw;
			}
		}

		private static void LogEntryPointException(string stage, Exception ex, string inputFolder, string outputPath)
		{
			string safeMessage = ex?.Message?.Replace(Environment.NewLine, " ");
			Console.Error.WriteLine(
				$"{LogTag} stage=\"{stage}\" op=UIEntryPoint inputFolder=\"{inputFolder ?? "N/A"}\" outputPath=\"{outputPath ?? "N/A"}\" target=\"DoWork\" message=\"{safeMessage}\" stack=\"{ex?.StackTrace}\"");
		}
	}
}
