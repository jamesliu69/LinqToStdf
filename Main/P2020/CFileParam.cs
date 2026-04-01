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
			try
			{
				logLines = File.ReadAllLines(FileName);
			}
			catch(Exception ex)
			{
				LogException("ReadSummaryFile", ex, FileName, null, null, null, null, "SummaryHeader");
				throw;
			}

			foreach(string headerLine in logLines)
			{
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
						LogException("ParseSiteCount", ex, FileName, TestItemName, null, null, headerLine, "SiteCount");
						throw;
					}
				}
				else if(headerParts[0].Contains("Pass  (By Sites)"))
				{
					IEnumerable<string> siteTokens   = headerParts[0].Trim().Split(' ').Where(c => c != "");
					IEnumerable<string> resultTokens = siteTokens.Skip(4);

					foreach(string siteValue in resultTokens)
					{
						ResultPass.Add(siteValue.Substring(0, resultTokens.ElementAt(0).IndexOf("(")));
					}
				}
				else if(headerParts[0].Contains("Fail  (By Sites)"))
				{
					IEnumerable<string> siteTokens   = headerParts[0].Trim().Split(' ').Where(c => c != "");
					IEnumerable<string> resultTokens = siteTokens.Skip(4);

					foreach(string siteValue in resultTokens)
					{
						ResultFail.Add(siteValue.Substring(0, resultTokens.ElementAt(0).IndexOf("(")));
					}
				}

				// 每筆資料重新清理，避免前一輪結果殘留。
				HardWareBin.Clear();

				if(headerParts[0].Contains("[HARDWARE BIN]"))
				{
					int hardwareBinStartIndex = logLines.ToList().IndexOf("[HARDWARE BIN]");

					var hardwareBinMarker = logLines.Select((item, index) => new
																			 {
																				 Item  = item,
																				 Index = index
																			 })
													.FirstOrDefault(x => x.Item.StartsWith("**************"));

					if(hardwareBinMarker != null && hardwareBinStartIndex >= 0)
					{
						List<string> hardwareBinLines = logLines.ToList().GetRange(hardwareBinStartIndex + 2, hardwareBinMarker.Index);

						foreach(string binLine in hardwareBinLines)
						{
							try
							{
								IEnumerable<string> splitValues = binLine.Split(' ').Where(c => c != "");

								if(!string.IsNullOrEmpty(splitValues.ElementAt(0)))
								{
									HardWareBin[splitValues.ElementAt(0)] = binLine.Trim().Split(' ').Where(c => c != "").Skip(3).ToList();
								}
							}
							catch(Exception ex)
							{
								LogException("ParseHardwareBin", ex, FileName, TestItemName, null, null, binLine, "HardWareBin");
								throw;
							}
						}
					}
				}

				if(headerParts[0].Contains("[SOFTWARE BIN]"))
				{
					int softwareBinStartIndex = logLines.ToList().IndexOf("[SOFTWARE BIN]");

					var softwareBinMarker = logLines.Select((item, index) => new
																			 {
																				 Item  = item,
																				 Index = index
																			 })
													.FirstOrDefault(x => x.Item.StartsWith("**************"));

					if(softwareBinMarker != null && softwareBinStartIndex >= 0)
					{
						List<string> softwareBinLines = logLines.ToList().GetRange(softwareBinStartIndex + 2, softwareBinMarker.Index);

						foreach(string binLine in softwareBinLines)
						{
							try
							{
								IEnumerable<string> splitValues = binLine.Split(' ').Where(c => c != "");

								if(!string.IsNullOrEmpty(splitValues.ElementAt(0)))
								{
									SoftWareBin[splitValues.ElementAt(0)] = binLine.Trim().Split(' ').Where(c => c != "").Skip(3).ToList();
								}
							}
							catch(Exception ex)
							{
								LogException("ParseSoftwareBin", ex, FileName, TestItemName, null, null, binLine, "SoftWareBin");
								throw;
							}
						}
					}
				}
			}
		}

		private static void LogException(string operation, Exception ex, string filePath, string testItem, string site, string pin, string rawInputValue, string targetRecord)
		{
			string safeMessage = ex?.Message?.Replace(Environment.NewLine, " ");
			Console.Error.WriteLine(
				$"{LogTag} op={operation} filePath=\"{filePath ?? "N/A"}\" testItem=\"{testItem ?? "N/A"}\" site=\"{site ?? "N/A"}\" pin=\"{pin ?? "N/A"}\" rawInput=\"{rawInputValue ?? "N/A"}\" target=\"{targetRecord ?? "N/A"}\" message=\"{safeMessage}\" stack=\"{ex?.StackTrace}\"");
		}
	}
}
