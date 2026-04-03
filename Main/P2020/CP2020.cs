#region

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

#endregion

namespace STDF
{
	/// <summary>
	/// P2020 測試記錄解析器，用於讀取和處理半導體測試的 P2020 格式日誌檔案
	/// 實現 IAnalyze 介面以支援 STDF 檔案的標準化解析流程
	/// </summary>
	public class CP2020 : IAnalyze
	{
		/// <summary>
		/// 錯誤追蹤標籤，用於記錄日誌時標識異常來源
		/// </summary>
		private const    string       LogTag         = "[STDF-TRACE-ERR]";
		
		/// <summary>
		/// 用於解析 P2020 測試資料行的正則表達式
		/// 匹配格式：Pass/Fail Site PinName ForceValue LowLimit HighLimit MeasureValue MinMeasureValue MaxMeasureValue
		/// </summary>
		private readonly Regex        _dataLineRegex = new Regex(@"^\s*(?<passOrFail>\S+)\s+(?<site>\S+)\s+(?<pinName>.+?)\s+(?<forceValue>\S+)\s+(?<lowLimit>\S*)\s+(?<highLimit>\S*)\s+(?<measureValue>\S+)\s+(?<minMeasureValue>\S+)\s+(?<maxMeasureValue>\S+)\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
		
		/// <summary>
		/// 待處理的日誌檔案清單
		/// </summary>
		private readonly List<string> _fileNames     = new List<string>();

		public CP2020(string filename)
		{
			_fileNames.Add(filename);
		}

		public CP2020(string[] filename)
		{
			_fileNames.AddRange(filename);
		}

		/// <summary>
		/// 晶片資料清單，儲存解析後的所有測試資料
		/// </summary>
		public List<CChipData> ChipDataList { get; } = new List<CChipData>();

		/// <summary>
		/// 資料字典計數，記錄處理過的資料筆數
		/// </summary>
		public int DictCount { get; set; }

		/// <summary>
		/// 取得資料表（目前未實作，返回 null）
		/// </summary>
		public DataTable GetTable() => null;

		/// <summary>
		/// 自動顯示項目（目前未實作）
		/// </summary>
		public void AutoShowItem()
		{
		}

		/// <summary>
		/// 選取項目變更事件
		/// </summary>
		public event EventHandler? evtSelectItem;

		/// <summary>
		/// 錯誤發生事件
		/// </summary>
		public event EventHandler? evtErrorArise;

		/// <summary>
		/// 儲存路徑，用於指定輸出檔案的儲存位置
		/// </summary>
		public string SavePath { get; set; }

		/// <summary>
		/// 總Pin數量
		/// </summary>
		public int AllPin { get; set; }

		/// <summary>
		/// 通過的Pin數量
		/// </summary>
		public int PassPin { get; set; }

		/// <summary>
		/// 失敗的Pin數量
		/// </summary>
		public int FailPin { get; set; }

		/// <summary>
		/// 分析並解析 P2020 測試日誌檔案
		/// 讀取所有指定的日誌檔案，解析測試資料並填入 ChipDataList 集合
		/// </summary>
		public void AnalyzeFile()
		{
			try
			{
				// 清空現有的晶片資料清單，準備重新解析
				ChipDataList.Clear();
				int parsedLogCount = 0;

				// 遍歷所有待處理的日誌檔案
				foreach(string logFilePath in _fileNames)
				{
					string[] logLines;

					try
					{
						// 讀取日誌檔案的所有行
						logLines = File.ReadAllLines(logFilePath);
					}
					catch(Exception ex)
					{
						// 記錄檔案讀取異常並重新拋出
						LogException("ReadAllLines", ex, logFilePath, "SourceLog", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, -1);
						throw;
					}
					
					// 當前處理的零件編號（DUT/Part 索引）
					int    currentPartIndex = 0;
					// 標記是否正在處理測試區塊內容
					bool   inTestBlock      = false;
					// 當前測試項目的標題
					string testItemTitle    = string.Empty;

					// 逐行處理日誌內容
					for(int lineIndex = 0; lineIndex < logLines.Length; lineIndex++)
					{
						string rawLine = logLines[lineIndex];
						string line    = rawLine?.Trim() ?? string.Empty;

						// 檢測測試開始標記，每個 Test Start 對應一個 DUT/Part
						if(line.StartsWith("==> Test Start", StringComparison.OrdinalIgnoreCase))
						{
							// 每個 Test Start 對應一個 DUT/Part，後續的 PIR/PRR 會以此分組。
							currentPartIndex++;
							inTestBlock   = true;
							testItemTitle = string.Empty;
							continue;
						}

						// 如果不在測試區塊內，則跳過此行
						if(!inTestBlock)
						{
							continue;
						}

						// 檢測測試結束標記
						if(line.StartsWith("==> Test End", StringComparison.OrdinalIgnoreCase))
						{
							inTestBlock = false;
							continue;
						}

						// 跳過空白行
						if(string.IsNullOrWhiteSpace(rawLine))
						{
							continue;
						}

						// 檢測測試項目標題行，此標題會影響 STDF 的 TestName 與後續 TSR 彙總
						if(rawLine.Contains("<<<<<<---------------     Test Item :"))
						{
							// Test Item 標題會影響 STDF 的 TestName 與後續 TSR 彙總。
							testItemTitle = ExtractTestItemTitle(rawLine);
							continue;
						}

						// 跳過判定電壓和站點資訊行（不需要解析的內容）
						if(rawLine.Contains("JUDGE_V:") || rawLine.Contains("P/F   Site"))
						{
							continue;
						}

						// 使用正則表達式匹配資料行格式
						if(!_dataLineRegex.IsMatch(rawLine))
						{
							continue;
						}

						// 只保留真正的量測列，避免把標頭或空白列誤當成測試資料。
						CChipData chipData = EnumerableConvert(logFilePath, testItemTitle, rawLine, lineIndex + 1);
						chipData.Id = currentPartIndex;
						ChipDataList.Add(chipData);
					}

					// 如果檔案中沒有找到任何 Test Start/Test End 區段，則略過此檔案
					if(currentPartIndex == 0)
					{
						InvalidDataException ex = new InvalidDataException("缺少 Test Start/Test End 區段，略過此檔案。");
						LogException("ExtractTestRange.Skip", ex, logFilePath, "DataLines", string.Empty, string.Empty, string.Empty, "marker=Test Start/Test End", string.Empty, -1);
						continue;
					}
					parsedLogCount++;
				}

				// 如果沒有成功解析任何日誌檔案，則拋出異常
				if(parsedLogCount == 0)
				{
					InvalidDataException ex = new InvalidDataException("找不到可解析的測試 Log（需包含 Test Start/Test End 區段）。");
					LogException("AnalyzeFile.NoValidTestLog", ex, string.Join(";", _fileNames), "ChipDataList", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, -1);
					throw ex;
				}
				
				// 更新資料字典計數為實際解析的晶片資料數量
				DictCount = ChipDataList.Count;
			}
			catch(Exception e)
			{
				// 記錄分析過程中的異常並顯示錯誤訊息
				LogException("AnalyzeFile", e, string.Join(";", _fileNames), "ChipDataList", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, -1);
				Console.WriteLine(e);
				MessageBox.Show(e.Message + "\r\n" + e.StackTrace);
				throw;
			}
		}

		public void GroupBySite()
		{
		}

		public void CalMeasure()
		{
			// 目前 P2020 資料來源不需要額外計算量測值。
		}

		public void OutputFile()
		{
		}

		/// <summary>
		/// 從日誌行中提取測試項目標題
		/// 用於解析包含 "Test Item :" 的行，並清理標題中的前綴符號
		/// </summary>
		/// <param name="rawLine">原始日誌行內容</param>
		/// <returns>清理後的測試項目標題</returns>
		private static string ExtractTestItemTitle(string rawLine)
		{
			if(string.IsNullOrWhiteSpace(rawLine))
			{
				return string.Empty;
			}
			Match match = Regex.Match(rawLine, @"Test\s*Item\s*:\s*(?<name>.+?)\s*-{5,}>+");

			if(match.Success)
			{
				return match.Groups["name"].Value.Replace("OSitem_", string.Empty).Trim();
			}
			int markerIndex = rawLine.IndexOf("Test Item :", StringComparison.OrdinalIgnoreCase);

			if(markerIndex < 0)
			{
				return rawLine.Trim();
			}
			string suffix   = rawLine.Substring(markerIndex + "Test Item :".Length);
			int    endIndex = suffix.IndexOf("--------------->>>>>>", StringComparison.Ordinal);

			if(endIndex >= 0)
			{
				suffix = suffix.Substring(0, endIndex);
			}
			return suffix.Replace("OSitem_", string.Empty).Trim();
		}

		/// <summary>
		/// 異步讀取指定路徑的檔案內容並分割為行陣列
		/// </summary>
		/// <param name="path">要讀取的檔案完整路徑</param>
		/// <returns>檔案內容按行分割的字串陣列</returns>
		public async Task<string[]> ReadAllLinesAsync(string path)
		{
			using(StreamReader reader = new StreamReader(path))
			{
				string text = await reader.ReadToEndAsync();

				return text.Split(new[]
				{
					"\r\n", "\r", "\n",
				}, StringSplitOptions.None);
			}
		}

		/// <summary>
		/// 將單行測試資料轉換為 CChipData 物件
		/// 解析符合 P2020 格式的資料行並填入晶片資料物件的各個欄位
		/// </summary>
		/// <param name="filePath">來源日誌檔案路徑</param>
		/// <param name="testItemTitle">當前測試項目標題</param>
		/// <param name="dataLine">要解析的原始資料行</param>
		/// <param name="lineNumber">資料行在檔案中的行號（從 1 開始）</param>
		/// <returns>包含解析結果的 CChipData 物件</returns>
		private CChipData EnumerableConvert(string filePath, string testItemTitle, string dataLine, int lineNumber)
		{
			try
			{
				Match match = _dataLineRegex.Match(dataLine);

				if(!match.Success)
				{
					InvalidOperationException ex = new InvalidOperationException($"資料列格式不符合預期: {dataLine}");
					LogException("EnumerableConvert.Match", ex, filePath, "DataLineRegex", testItemTitle, string.Empty, string.Empty, dataLine, string.Empty, lineNumber);
					throw ex;
				}
				CChipData chipData = new CChipData();
				chipData.FileName = Path.GetFileNameWithoutExtension(filePath);
				chipData.Comment  = testItemTitle.Trim();

				// 依欄位直接取值，保留 L-Limit / H-Limit 的空白狀態。
				chipData.PassOrFail    = match.Groups["passOrFail"].Value.Trim();
				chipData.Site          = match.Groups["site"].Value.Trim();
				chipData.PinName       = match.Groups["pinName"].Value.Trim();
				chipData.strForceValue = match.Groups["forceValue"].Value.Trim();
				string lowLimitText  = match.Groups["lowLimit"].Value.Trim();
				string highLimitText = match.Groups["highLimit"].Value.Trim();
				chipData.LowLimit           = string.IsNullOrWhiteSpace(lowLimitText) ? null : lowLimitText;
				chipData.HighLimit          = string.IsNullOrWhiteSpace(highLimitText) ? null : highLimitText;
				chipData.strMeasureValue    = match.Groups["measureValue"].Value.Trim();
				chipData.strMinMeasureValue = match.Groups["minMeasureValue"].Value.Trim();
				chipData.strMaxMeasureValue = match.Groups["maxMeasureValue"].Value.Trim();
				return chipData;
			}
			catch(Exception e)
			{
				LogException("EnumerableConvert", e, filePath, "CChipData", testItemTitle, null, null, dataLine, string.Empty, lineNumber);
				MessageBox.Show($"EnumerableConvert 錯誤:\n檔案: {filePath}\n標題: {testItemTitle}\n資料列: {dataLine}\n\n{e.Message}\n{e.StackTrace}");
				throw;
			}
		}

		/// <summary>
		/// 記錄異常資訊到追蹤日誌
		/// 標準化的異常記錄方法，包含操作名稱、檔案路徑、區段資訊等詳細內容
		/// </summary>
		/// <param name="operation">發生異常的操作名稱</param>
		/// <param name="exception">要記錄的異常物件</param>
		/// <param name="filePath">相關檔案路徑</param>
		/// <param name="section">程式區段或模組名稱</param>
		/// <param name="testItem">當前測試項目</param>
		/// <param name="site">站點編號</param>
		/// <param name="pin">腳位編號</param>
		/// <param name="rawInputValue">原始輸入值</param>
		/// <param name="keyRawValue">關鍵原始值</param>
		/// <param name="lineNumber">發生異常的行號</param>
		private static void LogException(string operation, Exception ex, string filePath, string section, string testItem, string site, string pin, string rawInputValue, string keyRawValue, int lineNumber)
		{
			string safeMessage = ex?.Message?.Replace(Environment.NewLine, " ");
			TraceLogger.WriteLine($"{LogTag} op={operation} filePath=\"{filePath ?? "N/A"}\" section=\"{section ?? "N/A"}\" testItem=\"{testItem ?? "N/A"}\" line=\"{lineNumber}\" site=\"{site ?? "N/A"}\" pin=\"{pin ?? "N/A"}\" rawInput=\"{rawInputValue ?? "N/A"}\" keyRaw=\"{keyRawValue ?? "N/A"}\" message=\"{safeMessage}\" stack=\"{ex?.StackTrace}\"");
		}

		/// <summary>
		/// 釋放資源（目前未實作，保留介面相容性）
		/// </summary>
		public void Dispose()
		{
		}

		public static CP2020 CreateInstance(string   filename, int calOffset) => new CP2020(filename);
		public static CP2020 CreateInstance(string[] filename, int calOffset) => new CP2020(filename);
	}
}