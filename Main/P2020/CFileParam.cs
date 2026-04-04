using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace STDF
{
	public class CFileParam
	{
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

		public CFileParam(string filename) => FileName = filename;

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

		private static void LogException(string operation, Exception ex, string filePath, string section, string testItem, string site, string pin, string rawInputValue, string keyRawValue, int lineNumber)
		{
			string safeMessage = ex?.Message?.Replace(Environment.NewLine, " ");
			TraceLogger.WriteLine($"{LogTag} op={operation} filePath=\"{filePath ?? "N/A"}\" section=\"{section ?? "N/A"}\" testItem=\"{testItem ?? "N/A"}\" line=\"{lineNumber}\" site=\"{site ?? "N/A"}\" pin=\"{pin ?? "N/A"}\" rawInput=\"{rawInputValue ?? "N/A"}\" keyRaw=\"{keyRawValue ?? "N/A"}\" message=\"{safeMessage}\" stack=\"{ex?.StackTrace}\"");
		}
	}
}