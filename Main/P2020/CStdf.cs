using Stdf;
using Stdf.Records.V4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace STDF
{
	public class CStdf
	{
		private const string LogTag = "[STDF-TRACE-ERR]";
		private readonly string         _logPath;
		private readonly string         _outputPath;
		private readonly StdfFileWriter _stdfWriter;
		private readonly string         _summaryLog;
		private          CFileParam     _fileParam;
		private          CP2020         _p2020;

		/// <summary>
		///     建構式
		/// </summary>
		/// <param name="LogPath"> Log 存在路徑位置</param>
		/// <param name="SummaryLog">Summary 存在路徑位置</param>
		/// <param name="Output">輸出路徑</param>
		public CStdf(string logPath, string outputPath)
		{
			Debug.Assert(logPath != null, nameof(logPath) + " != null");
			_logPath    = logPath;
			_summaryLog = logPath;
			Debug.Assert(outputPath != null, nameof(outputPath) + " != null");
			_outputPath = outputPath;
			_stdfWriter = new StdfFileWriter(_outputPath, true);
		}

		private void AnalyzeFile()
		{
			try
			{
				// 先解析 P2020 原始資料，再讀取對應的測試參數檔。
				string[] logFiles;
				try
				{
					logFiles = Directory.GetFiles(_logPath, "*.log");
				}
				catch(Exception ex)
				{
					LogException("LoadLogFiles", ex, _logPath, null, null, null, null, "P2020LogFiles");
					throw;
				}

				_p2020 = CP2020.CreateInstance(logFiles, 0);
				_p2020.AnalyzeFile();
				string summaryPath;
				try
				{
					summaryPath = Directory.GetFiles(_logPath, "*.txt")[0];
				}
				catch(Exception ex)
				{
					LogException("LoadSummaryFile", ex, _logPath, null, null, null, null, "SummaryTxt");
					throw;
				}

				_fileParam = new CFileParam(summaryPath);
				_fileParam.AnalyzeFile();
			}
			catch(Exception e)
			{
				LogException("AnalyzeFile", e, _logPath, null, null, null, null, "AnalyzeFlow");
				Console.WriteLine($@"處理中有錯誤發生: {e.Message}");
				throw;
			}
		}

		/// <summary>
		///     執行STDF 轉檔
		/// </summary>
		public void DoWork()
		{
			AnalyzeFile();
			Far far = new Far();
			far.CpuType     = 2;
			far.StdfVersion = 4;
			ExecuteWithLogging("WriteRecord", _logPath, null, null, null, null, "FAR", () => _stdfWriter.WriteRecord(far));
			Atr atr = new Atr();
			atr.ModifiedTime = DateTime.Now;
			atr.CommandLine  = "";
			ExecuteWithLogging("WriteRecord", _logPath, null, null, null, null, "ATR", () => _stdfWriter.WriteRecord(atr));

			#region MIR
			// MIR（Master Information Record）：記錄整批測試作業的主資訊，例如批號、開始時間與測試程式版本。

			Mir mir = new Mir();
			try
			{
				mir.SetupTime = DateTime.Parse(_fileParam.LotSTART);
				mir.StartTime = DateTime.Parse(_fileParam.LotSTART);
			}
			catch(Exception ex)
			{
				LogException("ParseDateTime", ex, _fileParam.FilePath, _fileParam.TestItemName, null, null, _fileParam.LotSTART, "MIR.SetupTime/StartTime");
				throw;
			}
			mir.StationNumber        = 0;
			mir.ModeCode             = "P";
			mir.RetestCode           = "N";
			mir.ProtectionCode       = "0";
			mir.BurnInTime           = 0;
			mir.CommandModeCode      = "0";
			mir.LotId                = _fileParam.LotNumber;
			mir.PartType             = "";
			mir.NodeName             = "";
			mir.TesterType           = "";
			mir.JobName              = "";
			mir.JobRevision          = "";
			mir.SublotId             = "";
			mir.OperatorName         = "";
			mir.ExecType             = "";
			mir.ExecVersion          = "";
			mir.TestCode             = "";
			mir.TestTemperature      = "";
			mir.UserText             = "";
			mir.AuxiliaryFile        = "";
			mir.PackageType          = "";
			mir.FamilyId             = "";
			mir.DateCode             = "";
			mir.FacilityId           = "";
			mir.FloorId              = "";
			mir.ProcessId            = "";
			mir.OperationFrequency   = "";
			mir.SpecificationName    = _fileParam.TestProgramName;
			mir.SpecificationVersion = "";
			mir.FlowId               = "";
			mir.SetupId              = "";
			mir.DesignRevision       = "";
			mir.EngineeringId        = "";
			mir.RomCode              = "";
			mir.SerialNumber         = "";
			mir.SupervisorName       = "";
			ExecuteWithLogging("WriteRecord", _fileParam.FilePath, _fileParam.TestItemName, null, null, null, "MIR", () => _stdfWriter.WriteRecord(mir));

			#endregion

			#region SDR
			// SDR（Site Description Record）：描述測試站台與設備配置資訊，定義 Head/Site 與硬體識別資料。

			Sdr sdr = new Sdr();
			sdr.HeadNumber = 1;
			sdr.SiteGroup  = 1;

			sdr.SiteNumbers = new byte[]
							  {
								  1
							  };
			sdr.HandlerType   = "";
			sdr.HandlerId     = "";
			sdr.CardType      = "";
			sdr.CardId        = "";
			sdr.LoadboardType = "";
			sdr.LoadboardId   = "";
			sdr.DibType       = "";
			sdr.DibId         = "";
			sdr.CableType     = "";
			sdr.CableId       = "";
			sdr.ContactorType = "";
			sdr.ContactorId   = "";
			sdr.LaserType     = "";
			sdr.LaserId       = "";
			sdr.ExtraType     = "";
			sdr.ExtraId       = "";
			ExecuteWithLogging("WriteRecord", _fileParam.FilePath, _fileParam.TestItemName, null, null, null, "SDR", () => _stdfWriter.WriteRecord(sdr));

			#endregion

			#region Prm
			// PMR（Pin Map Record）：定義量測通道與實體/邏輯腳位對應，供後續測試結果參照。

			Pmr pmr = new Pmr();
			pmr.PinIndex     = 1;
			pmr.ChannelType  = 0;
			pmr.ChannelName  = "";
			pmr.PhysicalName = "";
			pmr.LogicalName  = "";
			pmr.HeadNumber   = 1;
			pmr.SiteNumber   = 0;
			ExecuteWithLogging("WriteRecord", _fileParam.FilePath, _fileParam.TestItemName, null, null, null, "PMR", () => _stdfWriter.WriteRecord(pmr));

			#endregion

			#region PGR
			// PGR（Pin Group Record）：將多個腳位索引分組，方便以群組方式描述測試腳位集合。

			Pgr pgr = new Pgr();
			pgr.GroupIndex = 1;
			pgr.GroupName  = "G1_OPPN";

			pgr.PinIndexes = new ushort[]
							 {
								 1
							 };
			ExecuteWithLogging("WriteRecord", _fileParam.FilePath, _fileParam.TestItemName, null, null, null, "PGR", () => _stdfWriter.WriteRecord(pgr));

			#endregion

			#region PTR
			// PTR（Parametric Test Record）群組：以 PIR/PTR/PRR 串接每顆料件的進站、量測結果與出站資訊。

			Dictionary<string, uint> testNumberMap = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
			Dictionary<uint, TestSummary> testSummaries = new Dictionary<uint, TestSummary>();
			uint nextTestNumber = 1;

			for(int i = 0; i < _p2020.ChipDataList.Count; i++)
			{
				CChipData chip = _p2020.ChipDataList[i];
				Pir       pir  = new Pir();
				pir.HeadNumber = 1;
				byte siteNumber;
				try
				{
					siteNumber = Convert.ToByte(chip.Site);
				}
				catch(Exception ex)
				{
					LogException("ConvertSite", ex, chip.FileName, chip.Comment, chip.Site, chip.PinName, chip.Site, "PIR.SiteNumber");
					throw;
				}
				pir.SiteNumber = siteNumber;
				ExecuteWithLogging("WriteRecord", chip.FileName, chip.Comment, chip.Site, chip.PinName, null, "PIR", () => _stdfWriter.WriteRecord(pir));

				string testName = string.IsNullOrWhiteSpace(chip.Comment) ? "Unnamed_Test" : chip.Comment.Trim();
				if(!testNumberMap.TryGetValue(testName, out uint testNumber))
				{
					testNumber = nextTestNumber++;
					testNumberMap[testName] = testNumber;
				}

				string passOrFailText = chip.PassOrFail?.Trim() ?? string.Empty;
				bool   isPass         = passOrFailText.Equals("PASS", StringComparison.OrdinalIgnoreCase);

				Ptr ptr = new Ptr();
				ptr.TestNumber      = testNumber;
				ptr.HeadNumber      = 1;
				ptr.SiteNumber      = siteNumber;
				ptr.TestFlags       = isPass ? (byte)1 : (byte)0;
				ptr.ParametricFlags = 0;
				float? measurementValue;
				try
				{
					measurementValue = TryExtractFloat(chip.strMaxMeasureValue) ??
									   TryExtractFloat(chip.strMeasureValue) ??
									   TryExtractFloat(chip.strMinMeasureValue);
				}
				catch(Exception ex)
				{
					LogException("ValueConversion", ex, chip.FileName, chip.Comment, chip.Site, chip.PinName,
								 $"{chip.strMaxMeasureValue}|{chip.strMeasureValue}|{chip.strMinMeasureValue}", "PTR.Result");
					throw;
				}
				ptr.Result                   = measurementValue;
				ptr.TestText                 = chip.Comment;
				ptr.AlarmId                  = " ";
				ptr.OptionalFlags            = 0;
				ptr.ResultScalingExponent    = 6;
				ptr.LowLimitScalingExponent  = 6;
				ptr.HighLimitScalingExponent = 6;
				string lowLimitText  = chip.LowLimit;
				string highLimitText = chip.HighLimit;

				if(TryExtractFloat(lowLimitText) is float lowLimitValue)
				{
					ptr.LowLimit = lowLimitValue;
				}
				if(TryExtractFloat(highLimitText) is float highLimitValue)
				{
					ptr.HighLimit = highLimitValue;
				}
				string unitsText = string.Empty;

				if(!string.IsNullOrWhiteSpace(chip.LowLimit))
				{
					unitsText = Regex.Replace(chip.LowLimit, "[^a-zA-Z]", "");
				}

				if(string.IsNullOrWhiteSpace(unitsText))
				{
					unitsText = string.IsNullOrWhiteSpace(chip.HighLimit) ? string.Empty : Regex.Replace(chip.HighLimit, "[^a-zA-Z]", "");
				}
				ptr.Units = unitsText;
				ExecuteWithLogging("WriteRecord", chip.FileName, chip.Comment, chip.Site, chip.PinName, chip.strMeasureValue, "PTR", () => _stdfWriter.WriteRecord(ptr));

				if(!testSummaries.TryGetValue(testNumber, out TestSummary summary))
				{
					summary = new TestSummary
							  {
								  TestNumber = testNumber,
								  TestName   = testName
							  };
					testSummaries[testNumber] = summary;
				}
				summary.ExecutedCount++;
				if(!isPass)
				{
					summary.FailedCount++;
				}
				if(measurementValue.HasValue)
				{
					summary.HasMeasurement = true;
					float measured = measurementValue.Value;
					summary.TestMin = summary.TestMin.HasValue ? Math.Min(summary.TestMin.Value, measured) : measured;
					summary.TestMax = summary.TestMax.HasValue ? Math.Max(summary.TestMax.Value, measured) : measured;
					summary.TestSum += measured;
					summary.TestSumOfSquares += measured * measured;
				}

				Prr prr = new Prr();
				prr.HeadNumber = 1;
				prr.SiteNumber = siteNumber;
				ExecuteWithLogging("WriteRecord", chip.FileName, chip.Comment, chip.Site, chip.PinName, chip.PassOrFail, "PRR", () => _stdfWriter.WriteRecord(prr));
			}

			#endregion

			#region NO TSR
			// TSR（Test Synopsis Record）：提供特定測試項目的統計摘要（執行次數、失敗數、統計值等）。

			byte summarySiteNumber = _p2020.ChipDataList.Count > 0 ? Convert.ToByte(_p2020.ChipDataList[0].Site) : (byte)1;
			foreach(TestSummary summary in testSummaries.OrderBy(c => c.Key).Select(c => c.Value))
			{
				Tsr tsr = new Tsr();
				tsr.HeadNumber       = 1;
				tsr.SiteNumber       = summarySiteNumber;
				tsr.TestType         = "P";
				tsr.TestNumber       = summary.TestNumber;
				tsr.ExecutedCount    = summary.ExecutedCount;
				tsr.FailedCount      = summary.FailedCount;
				tsr.AlarmCount       = 0;
				tsr.TestName         = summary.TestName;
				tsr.SequencerName    = string.Empty;
				tsr.TestLabel        = string.Empty;
				tsr.TestTime         = null;
				tsr.TestMin          = summary.HasMeasurement ? summary.TestMin : null;
				tsr.TestMax          = summary.HasMeasurement ? summary.TestMax : null;
				tsr.TestSum          = summary.HasMeasurement ? summary.TestSum : null;
				tsr.TestSumOfSquares = summary.HasMeasurement ? summary.TestSumOfSquares : null;
				ExecuteWithLogging("WriteRecord", _fileParam.FilePath, summary.TestName, summarySiteNumber.ToString(), null, null, "TSR", () => _stdfWriter.WriteRecord(tsr));
			}

			#endregion

			#region NO HBR
			// HBR（Hardware Bin Record）：彙整硬體 Bin 分類結果與數量，用於硬體分 bin 統計。

			foreach(BinSummary bin in BuildBinSummaries(_fileParam.HardWareBin, _p2020.ChipDataList))
			{
				Hbr hbr = new Hbr();
				hbr.HeadNumber  = 1;
				hbr.SiteNumber  = summarySiteNumber;
				hbr.BinNumber   = bin.BinNumber;
				hbr.BinCount    = bin.BinCount;
				hbr.BinPassFail = bin.BinPassFail;
				hbr.BinName     = bin.BinName;
				ExecuteWithLogging("WriteRecord", _fileParam.FilePath, _fileParam.TestItemName, summarySiteNumber.ToString(), null, bin.BinName, "HBR", () => _stdfWriter.WriteRecord(hbr));
			}

			#endregion

			#region NO SBR
			// SBR（Software Bin Record）：彙整軟體 Bin 分類結果與數量，用於軟體分 bin 統計。

			foreach(BinSummary bin in BuildBinSummaries(_fileParam.SoftWareBin, _p2020.ChipDataList))
			{
				Sbr sbr = new Sbr();
				sbr.HeadNumber  = 1;
				sbr.SiteNumber  = summarySiteNumber;
				sbr.BinNumber   = bin.BinNumber;
				sbr.BinCount    = bin.BinCount;
				sbr.BinPassFail = bin.BinPassFail;
				sbr.BinName     = bin.BinName;
				ExecuteWithLogging("WriteRecord", _fileParam.FilePath, _fileParam.TestItemName, summarySiteNumber.ToString(), null, bin.BinName, "SBR", () => _stdfWriter.WriteRecord(sbr));
			}

			#endregion

			#region PCR
			// PCR（Part Count Record）：記錄測試數量統計（如測試顆數、良率相關計數）的站點摘要。

			Pcr pcr = new Pcr();
			pcr.HeadNumber = 1;
			pcr.SiteNumber = summarySiteNumber;
			ExecuteWithLogging("WriteRecord", _fileParam.FilePath, _fileParam.TestItemName, summarySiteNumber.ToString(), null, null, "PCR", () => _stdfWriter.WriteRecord(pcr));

			#endregion

			#region MRR
			// MRR（Master Results Record）：標記整批測試結束資訊，例如完工時間與結束說明。

			Mrr mrr = new Mrr();
			try
			{
				mrr.FinishTime = DateTime.Parse(_fileParam.LotEND);
			}
			catch(Exception ex)
			{
				LogException("ParseDateTime", ex, _fileParam.FilePath, _fileParam.TestItemName, null, null, _fileParam.LotEND, "MRR.FinishTime");
				throw;
			}
			mrr.DispositionCode = " ";
			mrr.UserDescription = " ";
			mrr.ExecDescription = " ";
			ExecuteWithLogging("WriteRecord", _fileParam.FilePath, _fileParam.TestItemName, null, null, null, "MRR", () => _stdfWriter.WriteRecord(mrr));

			#endregion

			_stdfWriter.Dispose();
		}

		private static float? TryExtractFloat(string rawText)
		{
			if(string.IsNullOrWhiteSpace(rawText))
			{
				return null;
			}

			Match match = Regex.Match(rawText, @"[-+]?\d*\.?\d+(?:[eE][-+]?\d+)?");
			if(!match.Success)
			{
				return null;
			}

			return float.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : (float?)null;
		}

		private static void ExecuteWithLogging(string operation, string filePath, string testItem, string site, string pin, string rawInputValue, string targetRecord, Action action)
		{
			try
			{
				action();
			}
			catch(Exception ex)
			{
				LogException(operation, ex, filePath, testItem, site, pin, rawInputValue, targetRecord);
				throw;
			}
		}

		private static void LogException(string operation, Exception ex, string filePath, string testItem, string site, string pin, string rawInputValue, string targetRecord)
		{
			string safeMessage = ex?.Message?.Replace(Environment.NewLine, " ");
			Console.Error.WriteLine(
				$"{LogTag} op={operation} filePath=\"{filePath ?? "N/A"}\" testItem=\"{testItem ?? "N/A"}\" site=\"{site ?? "N/A"}\" pin=\"{pin ?? "N/A"}\" rawInput=\"{rawInputValue ?? "N/A"}\" target=\"{targetRecord ?? "N/A"}\" message=\"{safeMessage}\" stack=\"{ex?.StackTrace}\"");
		}

		private static IEnumerable<BinSummary> BuildBinSummaries(Dictionary<string, IEnumerable<string>> binMap, IEnumerable<CChipData> chipDataList)
		{
			List<BinSummary> bins = new List<BinSummary>();

			if(binMap != null && binMap.Count > 0)
			{
				foreach(KeyValuePair<string, IEnumerable<string>> item in binMap)
				{
					string[] tokens = item.Value?.Where(token => !string.IsNullOrWhiteSpace(token)).ToArray() ?? Array.Empty<string>();
					if(!TryExtractUShort(item.Key, out ushort binNumber))
					{
						continue;
					}

					uint binCount = tokens
									.Select(token => uint.TryParse(token, out uint value) ? (uint?)value : null)
									.FirstOrDefault(value => value.HasValue) ?? 0;

					string binPassFail = tokens.Select(NormalizePassFailToken).FirstOrDefault(token => token != null) ?? InferPassFailFromBinNumber(binNumber);
					string binName = tokens.FirstOrDefault(token =>
														   !uint.TryParse(token, out _) &&
														   NormalizePassFailToken(token) == null) ?? item.Key;

					bins.Add(new BinSummary
							 {
								 BinNumber   = binNumber,
								 BinCount    = binCount,
								 BinPassFail = binPassFail,
								 BinName     = binName
							 });
				}
			}

			if(bins.Count == 0)
			{
				uint passCount = (uint)chipDataList.Count(chip => string.Equals(chip.PassOrFail?.Trim(), "PASS", StringComparison.OrdinalIgnoreCase));
				uint failCount = (uint)chipDataList.Count() - passCount;

				if(passCount > 0)
				{
					bins.Add(new BinSummary
							 {
								 BinNumber   = 1,
								 BinCount    = passCount,
								 BinPassFail = "P",
								 BinName     = "PASS"
							 });
				}

				if(failCount > 0)
				{
					bins.Add(new BinSummary
							 {
								 BinNumber   = 2,
								 BinCount    = failCount,
								 BinPassFail = "F",
								 BinName     = "FAIL"
							 });
				}
			}

			return bins;
		}

		private static bool TryExtractUShort(string rawText, out ushort value)
		{
			value = 0;
			if(string.IsNullOrWhiteSpace(rawText))
			{
				return false;
			}

			Match match = Regex.Match(rawText, @"\d+");
			return match.Success && ushort.TryParse(match.Value, out value);
		}

		private static string NormalizePassFailToken(string token)
		{
			if(string.IsNullOrWhiteSpace(token))
			{
				return null;
			}

			string normalized = token.Trim();
			if(normalized.Equals("P", StringComparison.OrdinalIgnoreCase) || normalized.Equals("PASS", StringComparison.OrdinalIgnoreCase))
			{
				return "P";
			}

			if(normalized.Equals("F", StringComparison.OrdinalIgnoreCase) || normalized.Equals("FAIL", StringComparison.OrdinalIgnoreCase))
			{
				return "F";
			}

			return null;
		}

		private static string InferPassFailFromBinNumber(ushort binNumber) => binNumber == 1 ? "P" : "F";

		private sealed class TestSummary
		{
			public uint   TestNumber       { get; set; }
			public string TestName         { get; set; }
			public uint   ExecutedCount    { get; set; }
			public uint   FailedCount      { get; set; }
			public bool   HasMeasurement   { get; set; }
			public float? TestMin          { get; set; }
			public float? TestMax          { get; set; }
			public float  TestSum          { get; set; }
			public float  TestSumOfSquares { get; set; }
		}

		private sealed class BinSummary
		{
			public ushort BinNumber   { get; set; }
			public uint   BinCount    { get; set; }
			public string BinPassFail { get; set; }
			public string BinName     { get; set; }
		}
	}
}
