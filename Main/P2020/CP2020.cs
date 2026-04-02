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
	public class CP2020 : IAnalyze
	{
		private const    string       LogTag         = "[STDF-TRACE-ERR]";
		private readonly Regex        _dataLineRegex = new Regex(@"^\s*(?<passOrFail>\S+)\s+(?<site>\S+)\s+(?<pinName>.+?)\s+(?<forceValue>\S+)\s+(?<lowLimit>\S*)\s+(?<highLimit>\S*)\s+(?<measureValue>\S+)\s+(?<minMeasureValue>\S+)\s+(?<maxMeasureValue>\S+)\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
		private readonly List<string> _fileNames     = new List<string>();

		public CP2020(string filename)
		{
			_fileNames.Add(filename);
		}

		public CP2020(string[] filename)
		{
			_fileNames.AddRange(filename);
		}

		public List<CChipData> ChipDataList { get; } = new List<CChipData>();

		public int DictCount { get; set; }

		public DataTable GetTable() => null;

		public void AutoShowItem()
		{
		}

		public event EventHandler? evtSelectItem;

		public event EventHandler? evtErrorArise;

		public string SavePath { get; set; }

		public int AllPin { get; set; }

		public int PassPin { get; set; }

		public int FailPin { get; set; }

		public void AnalyzeFile()
		{
			try
			{
				ChipDataList.Clear();
				int parsedLogCount = 0;

				foreach(string logFilePath in _fileNames)
				{
					string[] logLines;

					try
					{
						logLines = File.ReadAllLines(logFilePath);
					}
					catch(Exception ex)
					{
						LogException("ReadAllLines", ex, logFilePath, "SourceLog", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, -1);
						throw;
					}
					int    currentPartIndex = 0;
					bool   inTestBlock      = false;
					string testItemTitle    = string.Empty;

					for(int lineIndex = 0; lineIndex < logLines.Length; lineIndex++)
					{
						string rawLine = logLines[lineIndex];
						string line    = rawLine?.Trim() ?? string.Empty;

						if(line.StartsWith("==> Test Start", StringComparison.OrdinalIgnoreCase))
						{
							// 每個 Test Start 對應一個 DUT/Part，後續的 PIR/PRR 會以此分組。
							currentPartIndex++;
							inTestBlock   = true;
							testItemTitle = string.Empty;
							continue;
						}

						if(!inTestBlock)
						{
							continue;
						}

						if(line.StartsWith("==> Test End", StringComparison.OrdinalIgnoreCase))
						{
							inTestBlock = false;
							continue;
						}

						if(string.IsNullOrWhiteSpace(rawLine))
						{
							continue;
						}

						if(rawLine.Contains("<<<<<<---------------     Test Item :"))
						{
							// Test Item 標題會影響 STDF 的 TestName 與後續 TSR 彙總。
							testItemTitle = ExtractTestItemTitle(rawLine);
							continue;
						}

						if(rawLine.Contains("JUDGE_V:") || rawLine.Contains("P/F   Site"))
						{
							continue;
						}

						if(!_dataLineRegex.IsMatch(rawLine))
						{
							continue;
						}

						// 只保留真正的量測列，避免把標頭或空白列誤當成測試資料。
						CChipData chipData = EnumerableConvert(logFilePath, testItemTitle, rawLine, lineIndex + 1);
						chipData.Id = currentPartIndex;
						ChipDataList.Add(chipData);
					}

					if(currentPartIndex == 0)
					{
						InvalidDataException ex = new InvalidDataException("缺少 Test Start/Test End 區段，略過此檔案。");
						LogException("ExtractTestRange.Skip", ex, logFilePath, "DataLines", string.Empty, string.Empty, string.Empty, "marker=Test Start/Test End", string.Empty, -1);
						continue;
					}
					parsedLogCount++;
				}

				if(parsedLogCount == 0)
				{
					InvalidDataException ex = new InvalidDataException("找不到可解析的測試 Log（需包含 Test Start/Test End 區段）。");
					LogException("AnalyzeFile.NoValidTestLog", ex, string.Join(";", _fileNames), "ChipDataList", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, -1);
					throw ex;
				}
				DictCount = ChipDataList.Count;
			}
			catch(Exception e)
			{
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

		private static void LogException(string operation, Exception ex, string filePath, string section, string testItem, string site, string pin, string rawInputValue, string keyRawValue, int lineNumber)
		{
			string safeMessage = ex?.Message?.Replace(Environment.NewLine, " ");
			TraceLogger.WriteLine($"{LogTag} op={operation} filePath=\"{filePath ?? "N/A"}\" section=\"{section ?? "N/A"}\" testItem=\"{testItem ?? "N/A"}\" line=\"{lineNumber}\" site=\"{site ?? "N/A"}\" pin=\"{pin ?? "N/A"}\" rawInput=\"{rawInputValue ?? "N/A"}\" keyRaw=\"{keyRawValue ?? "N/A"}\" message=\"{safeMessage}\" stack=\"{ex?.StackTrace}\"");
		}

		public void Dispose()
		{
		}

		public static CP2020 CreateInstance(string   filename, int calOffset) => new CP2020(filename);
		public static CP2020 CreateInstance(string[] filename, int calOffset) => new CP2020(filename);
	}
}