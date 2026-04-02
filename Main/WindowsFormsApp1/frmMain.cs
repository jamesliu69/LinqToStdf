using STDF;
using System;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
	public partial class frmMain : Form
	{
		private const string LogTag = "[STDF-TRACE-ERR]";
		private readonly string _defaultInputFolder = @"D:\Pti_Doc\AutoScan Log\2026-04-01-16-39-45";
		private readonly string _defaultOutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "testSTDF.stdf");

		public frmMain()
		{
			InitializeComponent();
			txtInputFolder.Text = _defaultInputFolder;
			txtOutputPath.Text = _defaultOutputPath;
		}

		private void btnGenerateStdf_Click(object sender, EventArgs e)
		{
			string inputFolder = txtInputFolder.Text?.Trim();
			string outputPath = txtOutputPath.Text?.Trim();

			if(string.IsNullOrWhiteSpace(inputFolder))
			{
				MessageBox.Show(this, "請先選取資料夾。", "STDF 產生器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				txtInputFolder.Focus();
				return;
			}

			if(string.IsNullOrWhiteSpace(outputPath))
			{
				outputPath = _defaultOutputPath;
				txtOutputPath.Text = outputPath;
			}

			if(!Directory.Exists(inputFolder))
			{
				MessageBox.Show(this, $"找不到資料夾：{inputFolder}", "STDF 產生器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				txtInputFolder.Focus();
				return;
			}

			string outputDirectory = Path.GetDirectoryName(outputPath);

			if(!string.IsNullOrWhiteSpace(outputDirectory))
			{
				Directory.CreateDirectory(outputDirectory);
			}

			if(string.IsNullOrWhiteSpace(Path.GetExtension(outputPath)))
			{
				outputPath += ".stdf";
				txtOutputPath.Text = outputPath;
			}

			try
			{
				CStdf stdfConverter = new CStdf(inputFolder, outputPath);
				stdfConverter.DoWork();
				MessageBox.Show(this, $"STDF 產生完成。\n\n輸出檔案：{outputPath}", "STDF 產生器", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch(Exception ex)
			{
				LogEntryPointException("btnGenerateStdf_Click", ex, inputFolder, outputPath);
				MessageBox.Show(this, $"產生 STDF 失敗：{ex.Message}", "STDF 產生器", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private static void LogEntryPointException(string stage, Exception ex, string inputFolder, string outputPath)
		{
			string safeMessage = ex?.Message?.Replace(Environment.NewLine, " ");
			Console.Error.WriteLine($"{LogTag} stage=\"{stage}\" op=UIEntryPoint inputFolder=\"{inputFolder ?? "N/A"}\" outputPath=\"{outputPath ?? "N/A"}\" target=\"DoWork\" message=\"{safeMessage}\" stack=\"{ex?.StackTrace}\"");
		}

		//選取資料夾
		private void btnBrowseInputFolder_Click(object sender, EventArgs e)
		{
			using(FolderBrowserDialog dialog = new FolderBrowserDialog())
			{
				dialog.Description = "請選取 P2020 測試資料夾";
				dialog.ShowNewFolderButton = false;
				dialog.SelectedPath = Directory.Exists(txtInputFolder.Text) ? txtInputFolder.Text : _defaultInputFolder;

				if(dialog.ShowDialog(this) == DialogResult.OK)
				{
					txtInputFolder.Text = dialog.SelectedPath;
				}
			}

		}

		//設定輸出路徑+檔案名稱
		private void btnBrowseOutputPath_Click(object sender, EventArgs e)
		{
			using(SaveFileDialog dialog = new SaveFileDialog())
			{
				dialog.Title = "設定輸出路徑與檔案名稱";
				dialog.Filter = "STDF 檔案 (*.stdf)|*.stdf|所有檔案 (*.*)|*.*";
				dialog.DefaultExt = "stdf";
				dialog.AddExtension = true;

				string currentOutputPath = txtOutputPath.Text?.Trim();

				if(!string.IsNullOrWhiteSpace(currentOutputPath))
				{
					string currentDirectory = Path.GetDirectoryName(currentOutputPath);

					if(!string.IsNullOrWhiteSpace(currentDirectory) && Directory.Exists(currentDirectory))
					{
						dialog.InitialDirectory = currentDirectory;
					}

					dialog.FileName = Path.GetFileName(currentOutputPath);
				}
				else
				{
					dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
					dialog.FileName = Path.GetFileName(_defaultOutputPath);
				}

				if(dialog.ShowDialog(this) == DialogResult.OK)
				{
					txtOutputPath.Text = dialog.FileName;
				}
			}

		}
	}
}