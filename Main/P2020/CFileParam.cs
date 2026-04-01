using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace STDF
{
	public class CFileParam
	{
		private const string LogTag = "[STDF-TRACE-ERR]";
		public const string split = "********************************************************************";
		public       string ATEID;
		public       string Customer;
		public       string DeviceID;
		public       string FileName;

		public string FilePath;
		public string HandlerID;

		/// <summary>
		///     [HARDWARE BIN]
		/// </summary>
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

		/// <summary>
		///     [Result]
		/// </summary>
		public int SiteCount = 1;

		/// <summary>
		///     [SOFTWARE BIN]
		/// </summary>
		public Dictionary<string, IEnumerable<string>> SoftWareBin = new Dictionary<string, IEnumerable<string>>();
		public string TestCycle;

		/// <summary>
		///     [TEST ITEM]
		/// </summary>
		public string TestItemName = "O/S_Test";
		public string TestProgramName;

		public CFileParam(string filename) => FileName = filename;

		public void AnalyzeFile()
		{
			StringBuilder summaryBuilder = new StringBuilder();
			string[]      logLines;
			List<string>  logLineList;
			try
			{
				logLines = File.ReadAllLines(FileName);
			}
			catch(Exception ex)
			{
				LogException("ReadSummaryFile", ex, FileName, "SummaryHeader", null, null, null, null, null, -1);
				throw;
			}
			logLineList = logLines.ToList();

			for(int headerLineIndex = 0; headerLineIndex < logLines.Length; headerLineIndex++)
			{
				string   headerLine  = logLines[headerLineIndex];
				string[] headerParts = headerLine.Split(':');

				if(headerParts.Length < 2)
				{
					continue;
				}

				switch(headerParts[0].Trim())
				{
					case "File Path":
						FilePath = headerParts[1].Trim().Replace("----------", "");
						break;
					case "LoadBoard Name":
						LoadBoardName = headerParts[1].Trim().Replace("----------", "");
						break;
					case "Lot Number":
						LotNumber = headerParts[1].Trim().Replace("----------", "");
						break;
					case "Device ID":
						DeviceID = headerParts[1].Trim().Replace("----------", "");
						break;
					case "Operator":
						Operator = headerParts[1].Trim().Replace("----------", "");
						break;
					case "Customer":
						Customer = headerParts[1].Trim().Replace("----------", "");
						break;
					case "Test Program Name":
						TestProgramName = headerParts[1].Trim().Replace("----------", "");
						break;
					case "Sample Rate":
						SampleRate = headerParts[1].Trim().Replace("----------", "");
						break;
					case "Test Cycle":
						TestCycle = headerParts[1].Trim().Replace("----------", "");
						break;
					case "ATE ID":
						ATEID = headerParts[1].Trim().Replace("-", "");
						break;
					case "Handler ID":
						HandlerID = headerParts[1].Trim().Replace("----------", "");
						break;
					case "Lot START":
						LotSTART = headerLine.Remove(0, headerLine.IndexOf(":") + 1).Trim();
						break;
					case "Lot END":
						LotEND = headerLine.Remove(0, headerLine.IndexOf(":") + 1).Trim();
						break;
				}

				if(headerParts[0].Contains("Total (By Sites)"))
				{
					IEnumerable<string> siteTokens  = headerParts[0].Trim().Split(' ').Where(c => c != "");
					IEnumerable<string> countTokens = siteTokens.Skip(3);
					try
					{
						SiteCount = Convert.ToInt32(countTokens.ElementAt(0).Substring(0, countTokens.ElementAt(0).IndexOf("(")));
					}
					catch(Exception ex)
					{
						LogException("ParseSiteCount", ex, FileName, "Result", TestItemName, null, null, headerLine, headerParts[0], headerLineIndex + 1);
						throw;
					}
				}
				else if(headerParts[0].Contains("Pass  (By Sites)"))
				{
					IEnumerable<string> siteTokens   = headerParts[0].Trim().Split(' ').Where(c => c != "");
					IEnumerable<string> resultTokens = siteTokens.Skip(4);

					foreach(string siteValue in resultTokens)
					{
						try
						{
							ResultPass.Add(siteValue.Substring(0, resultTokens.ElementAt(0).IndexOf("(")));
						}
						catch(Exception ex)
						{
							LogException("ParsePassBySite", ex, FileName, "Result", TestItemName, null, null, headerLine, siteValue, headerLineIndex + 1);
							throw;
						}
					}
				}
				else if(headerParts[0].Contains("Fail  (By Sites)"))
				{
					IEnumerable<string> siteTokens   = headerParts[0].Trim().Split(' ').Where(c => c != "");
					IEnumerable<string> resultTokens = siteTokens.Skip(4);

					foreach(string siteValue in resultTokens)
					{
						try
						{
							ResultFail.Add(siteValue.Substring(0, resultTokens.ElementAt(0).IndexOf("(")));
						}
						catch(Exception ex)
						{
							LogException("ParseFailBySite", ex, FileName, "Result", TestItemName, null, null, headerLine, siteValue, headerLineIndex + 1);
							throw;
						}
					}
				}

				// 每筆資料重新清理，避免前一輪結果殘留。
				HardWareBin.Clear();

				if(headerParts[0].Contains("[HARDWARE BIN]"))
				{
					int hardwareBinStartIndex = logLineList.IndexOf("[HARDWARE BIN]");
					if(hardwareBinStartIndex < 0)
					{
						InvalidDataException ex = new InvalidDataException("找不到 [HARDWARE BIN] 區段起點。");
						LogException("ParseHardwareBin", ex, FileName, "HARDWARE BIN", TestItemName, null, null, "marker=[HARDWARE BIN]", null, headerLineIndex + 1);
						throw ex;
					}

					int hardwareBinMarkerIndex = logLineList.FindIndex(hardwareBinStartIndex + 1, line => line.StartsWith("**************"));
					if(hardwareBinMarkerIndex < 0)
					{
						InvalidDataException ex = new InvalidDataException($"[HARDWARE BIN] 缺少結尾 marker，startIndex={hardwareBinStartIndex}。");
						LogException("ParseHardwareBin", ex, FileName, "HARDWARE BIN", TestItemName, null, null, $"startIndex={hardwareBinStartIndex}; marker=**************", null, hardwareBinStartIndex + 1);
						throw ex;
					}

					int hardwareRangeStart = hardwareBinStartIndex + 2;
					int hardwareRangeCount = hardwareBinMarkerIndex - hardwareRangeStart;
					if(hardwareRangeStart < 0 || hardwareRangeStart > logLineList.Count || hardwareRangeCount < 0 || hardwareRangeStart + hardwareRangeCount > logLineList.Count)
					{
						InvalidDataException ex = new InvalidDataException($"[HARDWARE BIN] 範圍越界，start={hardwareRangeStart}, count={hardwareRangeCount}, total={logLineList.Count}。");
						LogException("ParseHardwareBin", ex, FileName, "HARDWARE BIN", TestItemName, null, null, $"startIndex={hardwareBinStartIndex}; markerIndex={hardwareBinMarkerIndex}; rangeStart={hardwareRangeStart}; rangeCount={hardwareRangeCount}", null, hardwareRangeStart + 1);
						throw ex;
					}

					if(hardwareRangeCount == 0)
					{
						InvalidDataException ex = new InvalidDataException($"[HARDWARE BIN] 區段為空，start={hardwareBinStartIndex}, marker={hardwareBinMarkerIndex}。");
						LogException("ParseHardwareBin", ex, FileName, "HARDWARE BIN", TestItemName, null, null, $"rangeStart={hardwareRangeStart}; rangeCount=0", null, hardwareRangeStart + 1);
						throw ex;
					}

					List<string> hardwareBinLines = logLineList.GetRange(hardwareRangeStart, hardwareRangeCount);

					foreach(string binLine in hardwareBinLines)
					{
						try
						{
							List<string> splitValues = binLine.Split(' ').Where(c => c != "").ToList();
							if(splitValues.Count == 0 || string.IsNullOrWhiteSpace(splitValues[0]))
							{
								InvalidDataException ex = new InvalidDataException($"Hardware bin 列格式錯誤，無法取得 key: '{binLine}'");
								LogException("ParseHardwareBin", ex, FileName, "HARDWARE BIN", TestItemName, null, null, binLine, splitValues.Count > 0 ? splitValues[0] : null, hardwareRangeStart + hardwareBinLines.IndexOf(binLine) + 1);
								throw ex;
							}

							HardWareBin[splitValues[0]] = splitValues.Skip(3).ToList();
						}
						catch(Exception ex)
						{
							LogException("ParseHardwareBin", ex, FileName, "HARDWARE BIN", TestItemName, null, null, binLine, null, hardwareRangeStart + hardwareBinLines.IndexOf(binLine) + 1);
							throw;
						}
					}
				}

				if(headerParts[0].Contains("[SOFTWARE BIN]"))
				{
					int softwareBinStartIndex = logLineList.IndexOf("[SOFTWARE BIN]");
					if(softwareBinStartIndex < 0)
					{
						InvalidDataException ex = new InvalidDataException("找不到 [SOFTWARE BIN] 區段起點。");
						LogException("ParseSoftwareBin", ex, FileName, "SOFTWARE BIN", TestItemName, null, null, "marker=[SOFTWARE BIN]", null, headerLineIndex + 1);
						throw ex;
					}

					int softwareBinMarkerIndex = logLineList.FindIndex(softwareBinStartIndex + 1, line => line.StartsWith("**************"));
					if(softwareBinMarkerIndex < 0)
					{
						InvalidDataException ex = new InvalidDataException($"[SOFTWARE BIN] 缺少結尾 marker，startIndex={softwareBinStartIndex}。");
						LogException("ParseSoftwareBin", ex, FileName, "SOFTWARE BIN", TestItemName, null, null, $"startIndex={softwareBinStartIndex}; marker=**************", null, softwareBinStartIndex + 1);
						throw ex;
					}

					int softwareRangeStart = softwareBinStartIndex + 2;
					int softwareRangeCount = softwareBinMarkerIndex - softwareRangeStart;
					if(softwareRangeStart < 0 || softwareRangeStart > logLineList.Count || softwareRangeCount < 0 || softwareRangeStart + softwareRangeCount > logLineList.Count)
					{
						InvalidDataException ex = new InvalidDataException($"[SOFTWARE BIN] 範圍越界，start={softwareRangeStart}, count={softwareRangeCount}, total={logLineList.Count}。");
						LogException("ParseSoftwareBin", ex, FileName, "SOFTWARE BIN", TestItemName, null, null, $"startIndex={softwareBinStartIndex}; markerIndex={softwareBinMarkerIndex}; rangeStart={softwareRangeStart}; rangeCount={softwareRangeCount}", null, softwareRangeStart + 1);
						throw ex;
					}

					if(softwareRangeCount == 0)
					{
						InvalidDataException ex = new InvalidDataException($"[SOFTWARE BIN] 區段為空，start={softwareBinStartIndex}, marker={softwareBinMarkerIndex}。");
						LogException("ParseSoftwareBin", ex, FileName, "SOFTWARE BIN", TestItemName, null, null, $"rangeStart={softwareRangeStart}; rangeCount=0", null, softwareRangeStart + 1);
						throw ex;
					}

					List<string> softwareBinLines = logLineList.GetRange(softwareRangeStart, softwareRangeCount);

					foreach(string binLine in softwareBinLines)
					{
						try
						{
							List<string> splitValues = binLine.Split(' ').Where(c => c != "").ToList();
							if(splitValues.Count == 0 || string.IsNullOrWhiteSpace(splitValues[0]))
							{
								InvalidDataException ex = new InvalidDataException($"Software bin 列格式錯誤，無法取得 key: '{binLine}'");
								LogException("ParseSoftwareBin", ex, FileName, "SOFTWARE BIN", TestItemName, null, null, binLine, splitValues.Count > 0 ? splitValues[0] : null, softwareRangeStart + softwareBinLines.IndexOf(binLine) + 1);
								throw ex;
							}

							SoftWareBin[splitValues[0]] = splitValues.Skip(3).ToList();
						}
						catch(Exception ex)
						{
							LogException("ParseSoftwareBin", ex, FileName, "SOFTWARE BIN", TestItemName, null, null, binLine, null, softwareRangeStart + softwareBinLines.IndexOf(binLine) + 1);
							throw;
						}
					}
				}
			}
		}

		private static void LogException(string operation, Exception ex, string filePath, string section, string testItem, string site, string pin, string rawInputValue, string keyRawValue, int lineNumber)
		{
			string safeMessage = ex?.Message?.Replace(Environment.NewLine, " ");
			TraceLogger.WriteLine(
				$"{LogTag} op={operation} filePath=\"{filePath ?? "N/A"}\" section=\"{section ?? "N/A"}\" testItem=\"{testItem ?? "N/A"}\" line=\"{lineNumber}\" site=\"{site ?? "N/A"}\" pin=\"{pin ?? "N/A"}\" rawInput=\"{rawInputValue ?? "N/A"}\" keyRaw=\"{keyRawValue ?? "N/A"}\" message=\"{safeMessage}\" stack=\"{ex?.StackTrace}\"");
		}
	}
}
