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
					// 讀取原始資料列，後續依 STDF 需要的欄位結構解析。
					string[] logLines = File.ReadAllLines(logFilePath);
					logLines = logLines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();

					// 只保留 Test Start / Test End 之間的資料。
					string[] dataLines = logLines
										 .SkipWhile(line => !line.Trim().StartsWith("==> Test Start"))
										 .Skip(1)
										 .TakeWhile(line => !line.Trim().StartsWith("==> Test End"))
										 .Where(line => !line.Contains("P/F   Site              Pin_name        Force      L-Limit      H-Limit      Measure   Min Measure   Max Measure"))
										 .ToArray();

					int    currentJudgeIndex = 0;
					string testItemTitle     = string.Empty;

					foreach(string dataLine in dataLines)
					{
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

						CChipData chipData = EnumerableConvert(logFilePath, testItemTitle, dataLine);

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
				Console.WriteLine(e);
				MessageBox.Show(e.Message + "\r\n" + e.StackTrace);
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

		private CChipData EnumerableConvert(string filePath, string testItemTitle, string dataLine)
		{
			try
			{
				Match match = _dataLineRegex.Match(dataLine);

				if(!match.Success)
				{
					throw new InvalidOperationException($"資料列格式不符合預期: {dataLine}");
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
				MessageBox.Show($"EnumerableConvert 錯誤:\n檔案: {filePath}\n標題: {testItemTitle}\n資料列: {dataLine}\n\n{e.Message}\n{e.StackTrace}");
			}

			return null;
		}

		public void Dispose()
		{
		}

		public static CP2020 CreateInstance(string   filename, int calOffset) => new CP2020(filename);
		public static CP2020 CreateInstance(string[] filename, int calOffset) => new CP2020(filename);
	}
}