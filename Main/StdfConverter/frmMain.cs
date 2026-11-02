using STDF;
using System;
using System.IO;
using System.Windows.Forms;

namespace StdfConverter
{
	public partial class frmMain : Form
	{
		/// <summary>LogEntryPointException 傳給共用每日追蹤日誌的操作標記。</summary>
		private const    string LogTag              = "[STDF-TRACE-ERR]";
		/// <summary>預設的 P2020 測試記錄資料夾路徑，作為資料夾模式的初始路徑與瀏覽對話框的備選路徑。</summary>
		private readonly string _defaultInputFolder = @"D:\Pti_Doc\AutoScan Log\2026-04-01-16-39-45";
		/// <summary>以執行檔目錄向上兩層推算的範例 DETAIL CSV 路徑，作為 CSV 模式的初始路徑與開檔對話框的備選路徑。</summary>
		private readonly string _defaultCsvPath = ResolveDefaultCsvPath();
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

		/// <summary>優先使用儲存庫根目錄的 DETAIL_LOG，找不到時回退至程式內建範例。</summary>
		private static string ResolveDefaultCsvPath()
		{
			string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			string fileName = "DETAIL_LOG_TEST-502_001.csv";
			string sourcePath = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\..\SampleLog", fileName));
			return File.Exists(sourcePath)
				? sourcePath
				: Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\SampleLog", fileName));
		}

		/// <summary>驗證輸入路徑與輸出路徑後，建立 CStdf 執行轉換；過程以對話框回報成功或失敗，失敗時先寫入錯誤追蹤日誌。</summary>
		private void btnGenerateStdf_Click(object sender, EventArgs e)
		{
			string inputPath = txtInputFolder.Text?.Trim();
			string outputPath  = txtOutputPath.Text?.Trim();

			if(string.IsNullOrWhiteSpace(inputPath))
			{
				MessageBox.Show(this, cboInputFormat.SelectedIndex == 1 ? "請先選取 DETAIL_LOG CSV 檔案。" : "請先選取資料夾。", "STDF 產生器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
				MessageBox.Show(this, csvInput ? $"找不到 DETAIL_LOG CSV 檔案：{inputPath}" : $"找不到資料夾：{inputPath}", "STDF 產生器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				txtInputFolder.Focus();
				return;
			}
			if(csvInput &&
			   (!Path.GetFileName(inputPath).StartsWith("DETAIL_LOG", StringComparison.OrdinalIgnoreCase) ||
			    !string.Equals(Path.GetExtension(inputPath), ".csv", StringComparison.OrdinalIgnoreCase)))
			{
				MessageBox.Show(this, "CSV 分析轉檔僅接受 DETAIL_LOG*.csv，請選取 DETAIL_LOG 檔案。", "STDF 產生器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
				TraceLogger.Log("INFO", "Convert.Start", $"input=\"{inputPath}\" output=\"{outputPath}\"");
				CStdf stdfConverter = new CStdf(inputPath, outputPath, csvInput);
				stdfConverter.DoWork();
				TraceLogger.Log("INFO", "Convert.Success", $"input=\"{inputPath}\" output=\"{outputPath}\"");
				MessageBox.Show(this, $"STDF 產生完成。\n\n輸出檔案：{outputPath}", "STDF 產生器", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch(Exception ex)
			{
				TraceLogger.Log("ERROR", "Convert.Failed", $"input=\"{inputPath}\" output=\"{outputPath}\"", ex);
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
			lblInputFolder.Text = csvInput ? "DETAIL_LOG CSV" : "P2020 測試記錄資料夾";
			lblInputHint.Text = csvInput
				? "選取單一 DETAIL_LOG CSV 進行分析轉檔。"
				: "選取包含 P2020 測試記錄與摘要檔的資料夾。";
			btnBrowseInputFolder.Text = csvInput ? "選擇 DETAIL_LOG…" : "選擇資料夾…";
			txtInputFolder.Text = csvInput ? _lastCsvInputPath : _lastFolderInputPath;
		}

		/// <summary>把 UI 進入點的操作、路徑與完整例外鏈寫入共用每日追蹤日誌。</summary>
		private static void LogEntryPointException(string stage, Exception ex, string inputFolder, string outputPath)
		{
			TraceLogger.Log("ERROR", stage, $"input=\"{inputFolder ?? "N/A"}\" output=\"{outputPath ?? "N/A"}\"", ex);
		}

		/// <summary>依目前輸入格式開啟 P2020 資料夾或 DETAIL_LOG CSV 選擇器。</summary>
		private void btnBrowseInputFolder_Click(object sender, EventArgs e)
		{
			if(cboInputFormat.SelectedIndex == 1)
			{
				using(OpenFileDialog dialog = new OpenFileDialog())
				{
				dialog.Title = "選取 DETAIL_LOG CSV 檔案";
				dialog.Filter = "DETAIL_LOG CSV 檔案 (DETAIL_LOG*.csv)|DETAIL_LOG*.csv";
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
