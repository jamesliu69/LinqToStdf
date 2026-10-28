using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace STDF
{
	/// <summary>P2020 Summary log（測試摘要檔）解析器：讀取檔案後剖析標頭欄位（批號、起訖時間、測試程式名等）、各站數量統計、[HARDWARE BIN]/[SOFTWARE BIN] 區段與首個測試項目名稱，供 CStdf 轉換 STDF 時使用。</summary>
	public class CFileParam
	{
		/// <summary>LogException 寫入 TraceLogger 時的行首標記。</summary>
		private const string LogTag = "[STDF-TRACE-ERR]";
		public const  string split  = "********************************************************************";
		public        string ATEID;
		public        string Customer;
		public        string DeviceID;
		public        string FileName;

		public string FilePath;
		public string HandlerID;

		/// <summary>[HARDWARE BIN]</summary>
		public Dictionary<string, IEnumerable<string>> HardWareBin = new Dictionary<string, IEnumerable<string>>();
		public string       LoadBoardName;
		public string       LotEND;
		public string       LotNumber;
		public string       LotSTART;
		public string       Operator;
		public List<string> ResultFail = new List<string>();
		public List<string> ResultPass = new List<string>();
		public string[]     ResultTotal;
		public string       SampleRate;

		/// <summary>[Result]</summary>
		public int SiteCount = 1;

		/// <summary>[SOFTWARE BIN]</summary>
		public Dictionary<string, IEnumerable<string>> SoftWareBin = new Dictionary<string, IEnumerable<string>>();
		public string TestCycle;

		/// <summary>[TEST ITEM]</summary>
		public string TestItemName = "O/S_Test";
		public string TestProgramName;

		/// <summary>以 Summary 檔案路徑建立解析器；實際剖析由 AnalyzeFile 執行。</summary>
		public CFileParam(string filename) => FileName = filename;

		/// <summary>逐行讀取 Summary 檔：剖析「名稱: 值」標頭、Total/Pass/Fail (By Sites) 數量列（N(%) 只取 N，並由欄數推得站點數）、bin 區段與首個測試項目名稱；讀檔失敗時記錄後重新拋出。</summary>
		public void AnalyzeFile()
		{
			string[] logLines;

			try
			{
				logLines = File.ReadAllLines(FileName);
			}
			catch(Exception ex)
			{
				LogException("ReadSummaryFile", ex, FileName, "SummaryHeader", null, null, null, null, null, -1);
				throw;
			}
			ResultPass.Clear();
			ResultFail.Clear();
			HardWareBin.Clear();
			SoftWareBin.Clear();
			ResultTotal  = null;
			TestItemName = "O/S_Test";
			SiteCount    = 1;

			for(int lineIndex = 0; lineIndex < logLines.Length; lineIndex++)
			{
				string rawLine = logLines[lineIndex];
				string line    = rawLine?.Trim() ?? string.Empty;

				if(string.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				if(TryParseHeaderField(line, out string headerName, out string headerValue))
				{
					ApplyHeaderValue(headerName, headerValue);
					continue;
				}

				if(line.StartsWith("Total (By Sites)", StringComparison.OrdinalIgnoreCase))
				{
					// Summary 區的 count 文字格式是 N(%)，這裡只取 N 供 STDF 彙總使用。
					List<string> countTokens = ExtractCountTokens(line);
					ResultTotal = countTokens.ToArray();

					if(countTokens.Count > 1)
					{
						SiteCount = countTokens.Count - 1;
					}
					continue;
				}

				if(line.StartsWith("Pass  (By Sites)", StringComparison.OrdinalIgnoreCase))
				{
					List<string> countTokens = ExtractCountTokens(line);
					ResultPass.AddRange(countTokens.Skip(1));
					continue;
				}

				if(line.StartsWith("Fail  (By Sites)", StringComparison.OrdinalIgnoreCase))
				{
					List<string> countTokens = ExtractCountTokens(line);
					ResultFail.AddRange(countTokens.Skip(1));
				}
			}
			Dictionary<string, IEnumerable<string>> hardwareBins = ParseBinSection(logLines, "[HARDWARE BIN]");

			foreach(KeyValuePair<string, IEnumerable<string>> item in hardwareBins)
			{
				HardWareBin[item.Key] = item.Value;
			}
			Dictionary<string, IEnumerable<string>> softwareBins = ParseBinSection(logLines, "[SOFTWARE BIN]");

			foreach(KeyValuePair<string, IEnumerable<string>> item in softwareBins)
			{
				SoftWareBin[item.Key] = item.Value;
			}
			string parsedTestItemName = ParseFirstTestItemName(logLines);

			if(!string.IsNullOrWhiteSpace(parsedTestItemName))
			{
				TestItemName = parsedTestItemName;
			}
		}

		/// <summary>嘗試把整行拆成「標頭名稱: 標頭值」；冒號前無文字（&lt;=0）視為非標頭行。</summary>
		private static bool TryParseHeaderField(string line, out string headerName, out string headerValue)
		{
			headerName  = null;
			headerValue = null;
			int delimiterIndex = line.IndexOf(':');

			if(delimiterIndex <= 0)
			{
				return false;
			}
			headerName  = line.Substring(0, delimiterIndex).Trim();
			headerValue = line.Substring(delimiterIndex + 1).Trim();
			return true;
		}

		/// <summary>依標頭名稱把值（去除 '----------' 填充線後）寫入對應欄位；未列出的標頭名稱忽略。</summary>
		private void ApplyHeaderValue(string headerName, string headerValue)
		{
			string normalized = headerValue?.Replace("----------", string.Empty).Trim() ?? string.Empty;

			switch(headerName)
			{
				case "File Path":
					FilePath = normalized;
					break;
				case "LoadBoard Name":
					LoadBoardName = normalized;
					break;
				case "Lot Number":
					LotNumber = normalized;
					break;
				case "Device ID":
					DeviceID = normalized;
					break;
				case "Operator":
					Operator = normalized;
					break;
				case "Customer":
					Customer = normalized;
					break;
				case "Test Program Name":
					TestProgramName = normalized;
					break;
				case "Sample Rate":
					SampleRate = normalized;
					break;
				case "Test Cycle":
					TestCycle = normalized;
					break;
				case "ATE ID":
					ATEID = normalized.Replace("-", string.Empty);
					break;
				case "Handler ID":
					HandlerID = normalized;
					break;
				case "Lot START":
					LotSTART = normalized;
					break;
				case "Lot END":
					LotEND = normalized;
					break;
			}
		}

		/// <summary>從數量列抽出所有「N(%)」中的 N。</summary>
		private static List<string> ExtractCountTokens(string line)
		{
			MatchCollection matches = Regex.Matches(line ?? string.Empty, @"(?<count>\d+)\(\d+(?:\.\d+)?%\)");
			List<string>    counts  = new List<string>(matches.Count);

			foreach(Match match in matches)
			{
				if(match.Groups["count"].Success)
				{
					counts.Add(match.Groups["count"].Value);
				}
			}
			return counts;
		}

		/// <summary>剖析 [HARDWARE BIN]/[SOFTWARE BIN] 區段：跳過表頭列後，以首 token（含括號註記時併次 token）為鍵、其餘 token 為值，遇到分隔線或下一個 [區段] 即停止；解析例外時記錄後重新拋出。</summary>
		private Dictionary<string, IEnumerable<string>> ParseBinSection(string[] lines, string sectionName)
		{
			Dictionary<string, IEnumerable<string>> bins         = new Dictionary<string, IEnumerable<string>>();
			int                                     sectionIndex = Array.FindIndex(lines, line => string.Equals(line?.Trim(), sectionName, StringComparison.OrdinalIgnoreCase));

			if(sectionIndex < 0)
			{
				return bins;
			}

			for(int lineIndex = sectionIndex + 2; lineIndex < lines.Length; lineIndex++)
			{
				string rawLine = lines[lineIndex];
				string line    = rawLine?.Trim() ?? string.Empty;

				if(string.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				if(line.StartsWith("****************************************************************", StringComparison.Ordinal) || line.StartsWith("[", StringComparison.Ordinal))
				{
					break;
				}

				try
				{
					// Bin 區每列會同時帶 bin 名稱、數量與 pass/fail 標記，後續由 CStdf 轉成 HBR/SBR。
					List<string> tokens = Regex.Matches(rawLine, @"\S+").Cast<Match>().Select(match => match.Value).ToList();

					if(tokens.Count < 3)
					{
						continue;
					}

					if(tokens[0].Equals("Bin", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					string key = tokens[0];

					if(tokens.Count > 1 && tokens[1].StartsWith("(", StringComparison.Ordinal) && tokens[1].EndsWith(")", StringComparison.Ordinal))
					{
						key = $"{tokens[0]} {tokens[1]}";
					}
					bins[key] = tokens.Skip(2).ToList();
				}
				catch(Exception ex)
				{
					LogException("ParseBinSection", ex, FileName, sectionName, TestItemName, null, null, rawLine, null, lineIndex + 1);
					throw;
				}
			}
			return bins;
		}

		/// <summary>從 [TEST ITEM] 區段取第一個「名稱 N(%)」列的名稱，作為整份 Summary 的代表測試項目名稱；無則回傳 null。</summary>
		private static string ParseFirstTestItemName(string[] lines)
		{
			int testItemSectionIndex = Array.FindIndex(lines, line => string.Equals(line?.Trim(), "[TEST ITEM]", StringComparison.OrdinalIgnoreCase));

			if(testItemSectionIndex < 0)
			{
				return null;
			}

			for(int lineIndex = testItemSectionIndex + 2; lineIndex < lines.Length; lineIndex++)
			{
				string rawLine = lines[lineIndex];
				string line    = rawLine?.Trim() ?? string.Empty;

				if(string.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				if(line.StartsWith("****************************************************************", StringComparison.Ordinal) || line.StartsWith("[", StringComparison.Ordinal))
				{
					break;
				}
				Match nameMatch = Regex.Match(rawLine, @"^(?<name>.+?)\s+\d+\(\d+(?:\.\d+)?%\)");

				if(nameMatch.Success)
				{
					// 取第一個 Test Item 名稱做為整份 Summary 的代表名稱。
					return nameMatch.Groups["name"].Value.Trim();
				}
			}
			return null;
		}

		/// <summary>把例外詳情（操作、檔案、區段、行號、原始輸入等）以單行結構化格式交由 TraceLogger 寫入檔案日誌與 Console.Error。</summary>
		private static void LogException(string operation, Exception ex, string filePath, string section, string testItem, string site, string pin, string rawInputValue, string keyRawValue, int lineNumber)
		{
			string safeMessage = ex?.Message?.Replace(Environment.NewLine, " ");
			TraceLogger.WriteLine($"{LogTag} op={operation} filePath=\"{filePath ?? "N/A"}\" section=\"{section ?? "N/A"}\" testItem=\"{testItem ?? "N/A"}\" line=\"{lineNumber}\" site=\"{site ?? "N/A"}\" pin=\"{pin ?? "N/A"}\" rawInput=\"{rawInputValue ?? "N/A"}\" keyRaw=\"{keyRawValue ?? "N/A"}\" message=\"{safeMessage}\" stack=\"{ex?.StackTrace}\"");
		}
	}
}