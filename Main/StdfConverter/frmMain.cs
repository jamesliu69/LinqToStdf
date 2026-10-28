using STDF;
using System;
using System.IO;
using System.Windows.Forms;

namespace StdfConverter
{
	public partial class frmMain : Form
	{
		/// <summary>LogEntryPointException 寫入 Console.Error 時的行首標記，用於辨識 STDF 轉換流程的錯誤追蹤輸出。</summary>
		private const    string LogTag              = "[STDF-TRACE-ERR]";
		/// <summary>預設的 P2020 測試記錄資料夾路徑，作為資料夾模式的初始路徑與瀏覽對話框的備選路徑。</summary>
		private readonly string _defaultInputFolder = @"D:\Pti_Doc\AutoScan Log\2026-04-01-16-39-45";
		/// <summary>以執行檔目錄向上兩層推算的範例 DETAIL CSV 路徑，作為 CSV 模式的初始路徑與開檔對話框的備選路徑。</summary>
		private readonly string _defaultCsvPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\SampleLog\DETAIL_LOG_TEST-502_001.csv"));
		/// <summary>記錄套用目前介面前的輸入格式是否為 CSV，決定切換時輸入框路徑回存到 _lastCsvInputPath 或 _lastFolderInputPath。</summary>
		private bool _lastCsvInput;
		/// <summary>輸入格式是否已套用過一次；首次套用不回存輸入路徑，之後切換格式才回存。</summary>
		private bool _inputFormatInitialized;
		/// <summary>資料夾模式上次使用的輸入路徑，切回資料夾模式時還原到輸入框。</summary>
		private string _lastFolderInputPath;
		/// <summary>CSV 模式上次使用的輸入路徑，切回 CSV 模式時還原到輸入框。</summary>
		private string _lastCsvInputPath;
		/// <summary>預設輸出檔路徑（桌面 testSTDF.stdf），輸出路徑空白時以此值補上。</summary>
		private readonly string _defaultOutputPath  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "testSTDF.stdf");

		/// <summary>初始化表單元件、以預設路徑初始化兩種輸入格式的上次路徑、預選第一個輸入格式並填入預設輸出路徑。</summary>
		public frmMain()
		{
			InitializeComponent();
			_lastFolderInputPath = _defaultInputFolder;
			_lastCsvInputPath = _defaultCsvPath;
			cboInputFormat.SelectedIndex = 0;
			txtOutputPath.Text  = _defaultOutputPath;
		}

		/// <summary>驗證輸入路徑與輸出路徑後，建立 CStdf 執行轉換；過程以對話框回報成功或失敗，失敗時先寫入錯誤追蹤日誌。</summary>
		private void btnGenerateStdf_Click(object sender, EventArgs e)
		{
			string inputPath = txtInputFolder.Text?.Trim();
			string outputPath  = txtOutputPath.Text?.Trim();

			if(string.IsNullOrWhiteSpace(inputPath))
			{
				MessageBox.Show(this, cboInputFormat.SelectedIndex == 1 ? "請先選取 CSV 檔案。" : "請先選取資料夾。", "STDF 產生器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				txtInputFolder.Focus();
				return;
			}

			if(string.IsNullOrWhiteSpace(outputPath))
			{
				outputPath         = _defaultOutputPath;
				txtOutputPath.Text = outputPath;
			}

			bool csvInput = cboInputFormat.SelectedIndex == 1;
			if(csvInput ? !File.Exists(inputPath) : !Directory.Exists(inputPath))
			{
				MessageBox.Show(this, csvInput ? $"找不到 CSV 檔案：{inputPath}" : $"找不到資料夾：{inputPath}", "STDF 產生器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
				outputPath         += ".stdf";
				txtOutputPath.Text =  outputPath;
			}

			try
			{
				CStdf stdfConverter = new CStdf(inputPath, outputPath, csvInput);
				stdfConverter.DoWork();
				MessageBox.Show(this, $"STDF 產生完成。\n\n輸出檔案：{outputPath}", "STDF 產生器", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch(Exception ex)
			{
				LogEntryPointException("btnGenerateStdf_Click", ex, inputPath, outputPath);
				MessageBox.Show(this, $"產生 STDF 失敗：{ex.Message}", "STDF 產生器", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>輸入格式切換時回存前一格式的輸入路徑，再切換介面文字並還原新格式上次使用的路徑。</summary>
		private void cboInputFormat_SelectedIndexChanged(object sender, EventArgs e)
		{
			bool csvInput = cboInputFormat.SelectedIndex == 1;
			if(_inputFormatInitialized)
			{
				if(_lastCsvInput) _lastCsvInputPath = txtInputFolder.Text;
				else _lastFolderInputPath = txtInputFolder.Text;
			}
			_lastCsvInput = csvInput;
			_inputFormatInitialized = true;
			lblInputFolder.Text = csvInput ? "CSV 測試資料" : "P2020 測試記錄資料夾";
			lblInputHint.Text = csvInput
				? "選取單一 DETAIL 或 RESULT CSV；DETAIL 含完整測項，RESULT 可能只有部分結果。"
				: "選取包含 P2020 測試記錄與摘要檔的資料夾。";
			btnBrowseInputFolder.Text = csvInput ? "選擇 CSV…" : "選擇資料夾…";
			txtInputFolder.Text = csvInput ? _lastCsvInputPath : _lastFolderInputPath;
		}

		/// <summary>把 UI 進入點的例外訊息與堆疊以單行結構化格式寫入 Console.Error；不寫入檔案日誌。</summary>
		private static void LogEntryPointException(string stage, Exception ex, string inputFolder, string outputPath)
		{
			string safeMessage = ex?.Message?.Replace(Environment.NewLine, " ");
			Console.Error.WriteLine($"{LogTag} stage=\"{stage}\" op=UIEntryPoint inputFolder=\"{inputFolder ?? "N/A"}\" outputPath=\"{outputPath ?? "N/A"}\" target=\"DoWork\" message=\"{safeMessage}\" stack=\"{ex?.StackTrace}\"");
		}

		//選取 P2020 資料夾或單一 CSV 檔案
		private void btnBrowseInputFolder_Click(object sender, EventArgs e)
		{
			if(cboInputFormat.SelectedIndex == 1)
			{
				using(OpenFileDialog dialog = new OpenFileDialog())
				{
					dialog.Title = "選取 CSV 檔案";
					dialog.Filter = "CSV 檔案 (*.csv)|*.csv|所有檔案 (*.*)|*.*";
					dialog.CheckFileExists = true;
					dialog.FileName = File.Exists(txtInputFolder.Text) ? txtInputFolder.Text : _defaultCsvPath;
					if(dialog.ShowDialog(this) == DialogResult.OK)
					{
						 txtInputFolder.Text = dialog.FileName;
						 _lastCsvInputPath = dialog.FileName;
					}
				}
				return;
			}
			using(FolderBrowserDialog dialog = new FolderBrowserDialog())
			{
				dialog.Description         = "請選取 P2020 測試資料夾";
				dialog.ShowNewFolderButton = false;
				dialog.SelectedPath        = Directory.Exists(txtInputFolder.Text) ? txtInputFolder.Text : _defaultInputFolder;

				if(dialog.ShowDialog(this) == DialogResult.OK)
				{
					txtInputFolder.Text = dialog.SelectedPath;
					_lastFolderInputPath = dialog.SelectedPath;
				}
			}
		}

		//設定輸出路徑+檔案名稱
		private void btnBrowseOutputPath_Click(object sender, EventArgs e)
		{
			using(SaveFileDialog dialog = new SaveFileDialog())
			{
				dialog.Title        = "設定輸出路徑與檔案名稱";
				dialog.Filter       = "STDF 檔案 (*.stdf)|*.stdf|所有檔案 (*.*)|*.*";
				dialog.DefaultExt   = "stdf";
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
					dialog.FileName         = Path.GetFileName(_defaultOutputPath);
				}

				if(dialog.ShowDialog(this) == DialogResult.OK)
				{
					txtOutputPath.Text = dialog.FileName;
				}
			}
		}
	}
}
