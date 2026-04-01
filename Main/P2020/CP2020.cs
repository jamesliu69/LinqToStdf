#region

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

#endregion

namespace STDF
{
	public class CP2020 : IAnalyze
	{
		private const string LogTag = "[STDF-TRACE-ERR]";
		private readonly Regex _dataLineRegex = new Regex(@"^\s*(?<passOrFail>\S+)\s+(?<site>\S+)\s+(?<pinName>\S+)\s+(?<forceValue>\S+)\s+(?<lowLimit>\S*)\s+(?<highLimit>\S*)\s+(?<measureValue>\S+)\s+(?<minMeasureValue>\S+)\s+(?<maxMeasureValue>\S+)\s*$",
														  RegexOptions.Compiled | RegexOptions.CultureInvariant);
		private readonly List<string> _fileNames = new List<string>();

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

				foreach(string logFilePath in _fileNames)
				{
					string[] logLines;
					try
					{
						// 讀取原始資料列，後續依 STDF 需要的欄位結構解析。
						logLines = File.ReadAllLines(logFilePath);
						logLines = logLines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
					}
					catch(Exception ex)
					{
						LogException("ReadAllLines", ex, logFilePath, "SourceLog", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, -1);
						throw;
					}

					int testStartIndex = Array.FindIndex(logLines, line => line.Trim().StartsWith("==> Test Start"));
					if(testStartIndex < 0)
					{
						InvalidDataException ex = new InvalidDataException("缺少 Test Start 標記，無法擷取測試資料區段。");
						LogException("ExtractTestRange", ex, logFilePath, "DataLines", string.Empty, string.Empty, string.Empty, "marker=Test Start", string.Empty, testStartIndex);
						throw ex;
					}

					int testEndIndex = Array.FindIndex(logLines, testStartIndex + 1, line => line.Trim().StartsWith("==> Test End"));
					if(testEndIndex < 0)
					{
						InvalidDataException ex = new InvalidDataException($"缺少 Test End 標記，Test Start index={testStartIndex}。");
						LogException("ExtractTestRange", ex, logFilePath, "DataLines", string.Empty, string.Empty, string.Empty, $"startIndex={testStartIndex}; marker=Test End", string.Empty, testEndIndex);
						throw ex;
					}

					if(testEndIndex <= testStartIndex + 1)
					{
						InvalidDataException ex = new InvalidDataException($"Test Start/Test End 之間無可用資料，start={testStartIndex}, end={testEndIndex}。");
						LogException("ExtractTestRange", ex, logFilePath, "DataLines", string.Empty, string.Empty, string.Empty, $"startIndex={testStartIndex}; endIndex={testEndIndex}", string.Empty, testEndIndex);
						throw ex;
					}

					// 只保留 Test Start / Test End 之間的資料。
					string[] dataLines = logLines
										 .Skip(testStartIndex + 1)
										 .Take(testEndIndex - testStartIndex - 1)
										 .Where(line => !line.Contains("P/F   Site              Pin_name        Force      L-Limit      H-Limit      Measure   Min Measure   Max Measure"))
										 .ToArray();
					if(dataLines.Length == 0)
					{
						InvalidDataException ex = new InvalidDataException($"Test Start/Test End 之間資料為空，start={testStartIndex}, end={testEndIndex}。");
						LogException("ExtractTestRange", ex, logFilePath, "DataLines", string.Empty, string.Empty, string.Empty, $"startIndex={testStartIndex}; endIndex={testEndIndex}; dataLineCount=0", string.Empty, testEndIndex);
						throw ex;
					}

					int    currentJudgeIndex = 0;
					string testItemTitle     = string.Empty;

					for(int dataLineIndex = 0; dataLineIndex < dataLines.Length; dataLineIndex++)
					{
						string dataLine = dataLines[dataLineIndex];
						if(dataLine.Contains("<<<<<<---------------     Test Item :"))
						{
							testItemTitle = dataLine.Replace("<<<<<<---------------     Test Item : OSitem_", string.Empty).Replace("--------------->>>>>>", string.Empty).Trim();
							continue;
						}

						if(dataLine.Contains("JUDGE_V:"))
						{
							currentJudgeIndex++;
							continue;
						}

						CChipData chipData = EnumerableConvert(logFilePath, testItemTitle, dataLine, dataLineIndex + testStartIndex + 2);

						if(string.Equals(chipData.PassOrFail, "PASS", StringComparison.OrdinalIgnoreCase))
						{
							chipData.Id = currentJudgeIndex;
						}

						ChipDataList.Add(chipData);
					}
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

		public async Task<string[]> ReadAllLinesAsync(string path)
		{
			using(StreamReader reader = new StreamReader(path))
			{
				string text = await reader.ReadToEndAsync();

				return text.Split(new[]
								  {
									  "\r\n", "\r", "\n"
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

				chipData.LowLimit  = string.IsNullOrWhiteSpace(lowLimitText) ? null : lowLimitText;
				chipData.HighLimit = string.IsNullOrWhiteSpace(highLimitText) ? null : highLimitText;

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
			TraceLogger.WriteLine(
				$"{LogTag} op={operation} filePath=\"{filePath ?? "N/A"}\" section=\"{section ?? "N/A"}\" testItem=\"{testItem ?? "N/A"}\" line=\"{lineNumber}\" site=\"{site ?? "N/A"}\" pin=\"{pin ?? "N/A"}\" rawInput=\"{rawInputValue ?? "N/A"}\" keyRaw=\"{keyRawValue ?? "N/A"}\" message=\"{safeMessage}\" stack=\"{ex?.StackTrace}\"");
		}

		public void Dispose()
		{
		}

		public static CP2020 CreateInstance(string   filename, int calOffset) => new CP2020(filename);
		public static CP2020 CreateInstance(string[] filename, int calOffset) => new CP2020(filename);
	}
}
