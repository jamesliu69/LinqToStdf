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
		private const    string         LogTag = "[STDF-TRACE-ERR]";
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
					logFiles = Directory.GetFiles(_logPath, "*.log").Where(path => Path.GetFileName(path).IndexOf("Summary", StringComparison.OrdinalIgnoreCase) < 0).ToArray();
				}
				catch(Exception ex)
				{
					LogException("LoadLogFiles", ex, _logPath, null, null, null, null, "P2020LogFiles", "AnalyzeFile.LoadLogFiles", _logPath, _outputPath);
					throw;
				}

				if(logFiles.Length == 0)
				{
					InvalidDataException ex = new InvalidDataException("找不到可供解析的測試 Log（*.log，且檔名不含 Summary）。");
					LogException("LoadLogFiles", ex, _logPath, null, null, null, null, "P2020LogFiles", "AnalyzeFile.LoadLogFiles", _logPath, _outputPath);
					throw ex;
				}
				_p2020 = CP2020.CreateInstance(logFiles, 0);
				_p2020.AnalyzeFile();
				string summaryPath;

				try
				{
					summaryPath = Directory.GetFiles(_logPath, "*.txt").FirstOrDefault();

					if(string.IsNullOrWhiteSpace(summaryPath))
					{
						summaryPath = Directory.GetFiles(_logPath, "*.log").FirstOrDefault(path => Path.GetFileName(path).IndexOf("Summary", StringComparison.OrdinalIgnoreCase) >= 0);
					}

					if(string.IsNullOrWhiteSpace(summaryPath))
					{
						throw new InvalidDataException("找不到 Summary 檔案（支援 *.txt 或 *Summary*.log）。");
					}
				}
				catch(Exception ex)
				{
					LogException("LoadSummaryFile", ex, _logPath, null, null, null, null, "SummaryFile", "AnalyzeFile.LoadSummaryFile", _logPath, _outputPath);
					throw;
				}
				_fileParam = new CFileParam(summaryPath);
				_fileParam.AnalyzeFile();
			}
			catch(Exception e)
			{
				LogException("AnalyzeFile", e, _logPath, null, null, null, null, "AnalyzeFlow", "AnalyzeFile", _logPath, _outputPath);
				Console.WriteLine($@"處理中有錯誤發生: {e.Message}");
				throw;
			}
		}

		/// <summary>
		///     執行STDF 轉檔
		/// </summary>
		public void DoWork()
		{
			string workflowStage = "DoWork.AnalyzeFile";

			try
			{
				AnalyzeFile();
				workflowStage = "DoWork.PrepareSiteAndPinMap";
				List<CChipData> chipDataList = _p2020.ChipDataList ?? new List<CChipData>();
				List<byte>      siteNumbers  = chipDataList.Select(chip => byte.TryParse(chip.Site, out byte site) ? (byte?)site : null).Where(site => site.HasValue).Select(site => site.Value).Distinct().OrderBy(site => site).ToList();

				if(siteNumbers.Count == 0)
				{
					siteNumbers.Add(1);
				}
				Dictionary<ushort, PinInfo> pinMap = new Dictionary<ushort, PinInfo>();

				foreach(CChipData chip in chipDataList)
				{
					if(!TryExtractPinIndex(chip.PinName, out ushort pinIndex))
					{
						continue;
					}

					if(!pinMap.ContainsKey(pinIndex))
					{
						pinMap[pinIndex] = new PinInfo
						{
							PinIndex    = pinIndex,
							RawPinName  = chip.PinName?.Trim() ?? string.Empty,
							LogicalName = ExtractPinLogicalName(chip.PinName),
						};
					}
				}
				workflowStage = "DoWork.WriteFARATR";
				Far far = new Far();
				far.CpuType     = 2;
				far.StdfVersion = 4;
				ExecuteWithLogging("WriteRecord", _logPath, null, null, null, null, "FAR", () => _stdfWriter.WriteRecord(far));
				Atr atr = new Atr();
				atr.ModifiedTime = DateTime.Now;
				atr.CommandLine  = "";
				ExecuteWithLogging("WriteRecord", _logPath, null, null, null, null, "ATR", () => _stdfWriter.WriteRecord(atr));

				#region MIR（Master Information Record）：記錄整批測試作業的主資訊，例如批號、開始時間與測試程式版本。

				workflowStage = "DoWork.WriteMIR";
				Mir mir = new Mir();

				try
				{
					mir.SetupTime = DateTime.Parse(_fileParam.LotSTART);
					mir.StartTime = DateTime.Parse(_fileParam.LotSTART);
				}
				catch(Exception ex)
				{
					LogException("ParseDateTime", ex, _fileParam.FilePath, _fileParam.TestItemName, null, null, _fileParam.LotSTART, "MIR.SetupTime/StartTime", workflowStage, _logPath, _outputPath);
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

				#region SDR（Site Description Record）：描述測試站台與設備配置資訊，定義 Head/Site 與硬體識別資料。

				workflowStage = "DoWork.WriteSDR";
				Sdr sdr = new Sdr();
				sdr.HeadNumber    = 1;
				sdr.SiteGroup     = 1;
				sdr.SiteNumbers   = siteNumbers.ToArray();
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

				#region Prm（Pin Map Record）：定義量測通道與實體/邏輯腳位對應，供後續測試結果參照。

				workflowStage = "DoWork.WritePMR";

				if(pinMap.Count > 0)
				{
					foreach(PinInfo pin in pinMap.Values.OrderBy(p => p.PinIndex))
					{
						Pmr pmr = new Pmr();
						pmr.PinIndex     = pin.PinIndex;
						pmr.ChannelType  = 0;
						pmr.ChannelName  = pin.LogicalName;
						pmr.PhysicalName = pin.RawPinName;
						pmr.LogicalName  = pin.LogicalName;
						pmr.HeadNumber   = 1;
						pmr.SiteNumber   = siteNumbers[0];
						ExecuteWithLogging("WriteRecord", _fileParam.FilePath, _fileParam.TestItemName, null, pin.RawPinName, null, "PMR", () => _stdfWriter.WriteRecord(pmr));
					}
				}
				else
				{
					Pmr pmr = new Pmr();
					pmr.PinIndex     = 1;
					pmr.ChannelType  = 0;
					pmr.ChannelName  = "";
					pmr.PhysicalName = "";
					pmr.LogicalName  = "";
					pmr.HeadNumber   = 1;
					pmr.SiteNumber   = siteNumbers[0];
					ExecuteWithLogging("WriteRecord", _fileParam.FilePath, _fileParam.TestItemName, null, null, null, "PMR", () => _stdfWriter.WriteRecord(pmr));
				}

				#endregion

				#region PGR（Pin Group Record）：將多個腳位索引分組，方便以群組方式描述測試腳位集合。

				workflowStage = "DoWork.WritePGR";
				Pgr pgr = new Pgr();
				pgr.GroupIndex = 1;
				pgr.GroupName  = "G1_OPPN";

				pgr.PinIndexes = pinMap.Count > 0
					? pinMap.Keys.OrderBy(index => index).ToArray()
					: new ushort[]
					{
						1,
					};
				ExecuteWithLogging("WriteRecord", _fileParam.FilePath, _fileParam.TestItemName, null, null, null, "PGR", () => _stdfWriter.WriteRecord(pgr));

				#endregion

				#region PTR（Parametric Test Record）群組：以 PIR/PTR/PRR 串接每顆料件的進站、量測結果與出站資訊。

				workflowStage = "DoWork.WritePIR_PTR_PRR";
				Dictionary<string, uint>      testNumberMap     = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
				Dictionary<uint, TestSummary> testSummaries     = new Dictionary<uint, TestSummary>();
				uint                          nextTestNumber    = 1;
				List<PartSiteSummary>         partSiteSummaries = BuildPartSiteSummaries(chipDataList);

				if(partSiteSummaries.Count == 0)
				{
					InvalidDataException ex = new InvalidDataException("找不到可輸出的 Part/Site 資料。請確認來源 Log 具備 Test Start/Test End 與量測資料列。");
					LogException("BuildPartSiteSummaries", ex, _fileParam.FilePath, _fileParam.TestItemName, null, null, "partSiteSummaries=0", "PIR/PTR/PRR", workflowStage, _logPath, _outputPath);
					throw ex;
				}

				// 先把每個 Part 的 PASS/FAIL 彙成 bin 用的輸入，再產生 HBR/SBR 與 PRR。
				List<CChipData> partOutcomeList = partSiteSummaries.Select(part => new CChipData
				                                                   {
					                                                   PassOrFail = part.IsPass ? "PASS" : "FAIL",
				                                                   })
				                                                   .ToList();
				List<BinSummary> hardwareBins = BuildBinSummaries(_fileParam.HardWareBin, partOutcomeList).ToList();
				List<BinSummary> softwareBins = BuildBinSummaries(_fileParam.SoftWareBin, partOutcomeList).ToList();

				foreach(PartSiteSummary part in partSiteSummaries)
				{
					workflowStage = $"DoWork.ProcessPart[id={part.PartIndex},site={part.SiteNumber}]";
					CChipData firstChip = part.Chips[0];

					try
					{
						Pir pir = new Pir();
						pir.HeadNumber = 1;
						pir.SiteNumber = part.SiteNumber;
						ExecuteWithLogging("WriteRecord", firstChip.FileName, firstChip.Comment, firstChip.Site, firstChip.PinName, null, "PIR", () => _stdfWriter.WriteRecord(pir));

						foreach(CChipData chip in part.Chips)
						{
							string testName = string.IsNullOrWhiteSpace(chip.Comment) ? "Unnamed_Test" : chip.Comment.Trim();

							if(!testNumberMap.TryGetValue(testName, out uint testNumber))
							{
								testNumber              = nextTestNumber++;
								testNumberMap[testName] = testNumber;
							}
							bool isPass = IsPassResult(chip.PassOrFail);
							Ptr  ptr    = new Ptr();
							ptr.TestNumber      = testNumber;
							ptr.HeadNumber      = 1;
							ptr.SiteNumber      = part.SiteNumber;
							ptr.TestFlags       = BuildPtrTestFlags(isPass, chip.PassOrFail);
							ptr.ParametricFlags = 0;
							float? measurementValue;

							try
							{
								measurementValue = TryExtractFloat(chip.strMeasureValue) ?? TryExtractFloat(chip.strMaxMeasureValue) ?? TryExtractFloat(chip.strMinMeasureValue);
							}
							catch(Exception ex)
							{
								LogException("ValueConversion", ex, chip.FileName, chip.Comment, chip.Site, chip.PinName, $"part={part.PartIndex};site={part.SiteNumber};values={chip.strMaxMeasureValue}|{chip.strMeasureValue}|{chip.strMinMeasureValue}", "PTR.Result", workflowStage, _logPath, _outputPath);
								throw;
							}
							ptr.Result   = measurementValue;
							ptr.TestText = chip.Comment;
							ptr.AlarmId  = " ";

							if(TryExtractFloat(chip.LowLimit) is float lowLimitValue)
							{
								ptr.LowLimit = lowLimitValue;
							}

							if(TryExtractFloat(chip.HighLimit) is float highLimitValue)
							{
								ptr.HighLimit = highLimitValue;
							}
							ptr.Units = ExtractUnits(chip.LowLimit, chip.HighLimit);
							ExecuteWithLogging("WriteRecord", chip.FileName, chip.Comment, chip.Site, chip.PinName, chip.strMeasureValue, "PTR", () => _stdfWriter.WriteRecord(ptr));

							if(!testSummaries.TryGetValue(testNumber, out TestSummary summary))
							{
								summary = new TestSummary
								{
									TestNumber = testNumber,
									TestName   = testName,
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
								summary.TestMin          =  summary.TestMin.HasValue ? Math.Min(summary.TestMin.Value, measured) : measured;
								summary.TestMax          =  summary.TestMax.HasValue ? Math.Max(summary.TestMax.Value, measured) : measured;
								summary.TestSum          += measured;
								summary.TestSumOfSquares += measured * measured;
							}
						}
						Prr prr = new Prr();
						prr.HeadNumber = 1;
						prr.SiteNumber = part.SiteNumber;

						// PRR 要帶完整的 part disposition，包含測試數量與最終 bin。
						prr.TestCount = (ushort)Math.Min(part.Chips.Count, ushort.MaxValue);
						prr.HardBin   = ResolveBinNumberForOutcome(hardwareBins, part.IsPass);
						prr.SoftBin   = ResolveBinNumberForOutcome(softwareBins, part.IsPass);
						prr.Failed    = !part.IsPass;
						prr.PartId    = $"{firstChip.FileName}-{part.PartIndex.ToString(CultureInfo.InvariantCulture)}-S{part.SiteNumber.ToString(CultureInfo.InvariantCulture)}";
						ExecuteWithLogging("WriteRecord", firstChip.FileName, firstChip.Comment, firstChip.Site, firstChip.PinName, part.IsPass ? "PASS" : "FAIL", "PRR", () => _stdfWriter.WriteRecord(prr));
					}
					catch(Exception ex)
					{
						LogException("ProcessPart", ex, firstChip.FileName, firstChip.Comment, firstChip.Site, firstChip.PinName, $"part={part.PartIndex};site={part.SiteNumber};rows={part.Chips.Count}", "PIR/PTR/PRR", workflowStage, _logPath, _outputPath);
						throw;
					}
				}

				#endregion

				#region TSR （Test Synopsis Record）：提供特定測試項目的統計摘要（執行次數、失敗數、統計值等）。

				workflowStage = "DoWork.WriteTSR";
				byte summarySiteNumber = 0;

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

				#region HBR（Hardware Bin Record）：彙整硬體 Bin 分類結果與數量，用於硬體分 bin 統計。

				workflowStage = "DoWork.WriteHBR";

				foreach(BinSummary bin in hardwareBins)
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

				#region SBR（Software Bin Record）：彙整軟體 Bin 分類結果與數量，用於軟體分 bin 統計。

				workflowStage = "DoWork.WriteSBR";

				foreach(BinSummary bin in softwareBins)
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

				#region PCR （Part Count Record）：記錄測試數量統計（如測試顆數、良率相關計數）的站點摘要。

				workflowStage = "DoWork.WritePCR";

				foreach(IGrouping<byte, PartSiteSummary> siteGroup in partSiteSummaries.GroupBy(part => part.SiteNumber).OrderBy(group => group.Key))
				{
					uint sitePartCount = (uint)siteGroup.Count();
					uint siteGoodCount = (uint)siteGroup.Count(part => part.IsPass);

					// PCR 以 site 為單位記錄 part count / good count，避免整批資料被壓成單一站點。
					Pcr pcr = new Pcr();
					pcr.HeadNumber      = 1;
					pcr.SiteNumber      = siteGroup.Key;
					pcr.PartCount       = sitePartCount;
					pcr.GoodCount       = siteGoodCount;
					pcr.FunctionalCount = siteGoodCount;
					pcr.RetestCount     = 0;
					pcr.AbortCount      = 0;
					ExecuteWithLogging("WriteRecord", _fileParam.FilePath, _fileParam.TestItemName, siteGroup.Key.ToString(CultureInfo.InvariantCulture), null, null, "PCR", () => _stdfWriter.WriteRecord(pcr));
				}
				uint totalPartCount = (uint)partSiteSummaries.Count;
				uint totalGoodCount = (uint)partSiteSummaries.Count(part => part.IsPass);
				Pcr  lotPcr         = new Pcr();
				lotPcr.HeadNumber      = 1;
				lotPcr.SiteNumber      = 0;
				lotPcr.PartCount       = totalPartCount;
				lotPcr.GoodCount       = totalGoodCount;
				lotPcr.FunctionalCount = totalGoodCount;
				lotPcr.RetestCount     = 0;
				lotPcr.AbortCount      = 0;
				ExecuteWithLogging("WriteRecord", _fileParam.FilePath, _fileParam.TestItemName, "0", null, null, "PCR", () => _stdfWriter.WriteRecord(lotPcr));

				#endregion

				#region MRR（Master Results Record）：標記整批測試結束資訊，例如完工時間與結束說明。

				workflowStage = "DoWork.WriteMRR";
				Mrr mrr = new Mrr();

				try
				{
					mrr.FinishTime = DateTime.Parse(_fileParam.LotEND);
				}
				catch(Exception ex)
				{
					LogException("ParseDateTime", ex, _fileParam.FilePath, _fileParam.TestItemName, null, null, _fileParam.LotEND, "MRR.FinishTime", workflowStage, _logPath, _outputPath);
					throw;
				}
				mrr.DispositionCode = " ";
				mrr.UserDescription = " ";
				mrr.ExecDescription = " ";
				ExecuteWithLogging("WriteRecord", _fileParam.FilePath, _fileParam.TestItemName, null, null, null, "MRR", () => _stdfWriter.WriteRecord(mrr));

				#endregion

				workflowStage = "DoWork.DisposeWriter";
				_stdfWriter.Dispose();
			}
			catch(Exception ex)
			{
				LogException("DoWork", ex, _fileParam?.FilePath ?? _logPath, _fileParam?.TestItemName, null, null, null, "Workflow", workflowStage, _logPath, _outputPath);
				throw;
			}
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
			return float.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : null;
		}

		private static bool IsPassResult(string passOrFailText) => string.Equals(passOrFailText?.Trim(), "PASS", StringComparison.OrdinalIgnoreCase);

		private static byte BuildPtrTestFlags(bool isPass, string passOrFailText)
		{
			const byte passFailInvalidMask = 0x10;
			const byte failMask            = 0x80;

			if(!string.Equals(passOrFailText?.Trim(), "PASS", StringComparison.OrdinalIgnoreCase) && !string.Equals(passOrFailText?.Trim(), "FAIL", StringComparison.OrdinalIgnoreCase))
			{
				return passFailInvalidMask;
			}
			return isPass ? (byte)0 : failMask;
		}

		private static string ExtractUnits(string lowLimitText, string highLimitText)
		{
			string unitsText = string.Empty;

			if(!string.IsNullOrWhiteSpace(lowLimitText))
			{
				unitsText = Regex.Replace(lowLimitText, "[^a-zA-Z]", string.Empty);
			}

			if(string.IsNullOrWhiteSpace(unitsText) && !string.IsNullOrWhiteSpace(highLimitText))
			{
				unitsText = Regex.Replace(highLimitText, "[^a-zA-Z]", string.Empty);
			}
			return unitsText;
		}

		private static List<PartSiteSummary> BuildPartSiteSummaries(IEnumerable<CChipData> chipDataList)
		{
			List<PartSiteSummary> summaries = new List<PartSiteSummary>();

			foreach(IGrouping<string, CChipData> group in chipDataList.Where(chip => chip != null && chip.Id > 0).GroupBy(chip => $"{chip.FileName}|{chip.Id}|{NormalizeSite(chip.Site)}"))
			{
				List<CChipData> chips = group.ToList();

				if(chips.Count == 0)
				{
					continue;
				}
				CChipData firstChip = chips[0];

				summaries.Add(new PartSiteSummary
				{
					PartIndex  = firstChip.Id,
					SiteNumber = NormalizeSite(firstChip.Site),
					Chips      = chips,
					IsPass     = chips.All(chip => IsPassResult(chip.PassOrFail)),
				});
			}
			return summaries.OrderBy(summary => summary.PartIndex).ThenBy(summary => summary.SiteNumber).ToList();
		}

		private static byte NormalizeSite(string siteRaw)
		{
			return byte.TryParse(siteRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte siteNumber) ? siteNumber : (byte)1;
		}

		private static ushort ResolveBinNumberForOutcome(IEnumerable<BinSummary> bins, bool isPass)
		{
			string     expectedPassFail = isPass ? "P" : "F";
			BinSummary matched          = bins.FirstOrDefault(bin => string.Equals(bin.BinPassFail, expectedPassFail, StringComparison.OrdinalIgnoreCase));

			if(matched != null)
			{
				return matched.BinNumber;
			}
			ushort     defaultBinNumber = isPass ? (ushort)1 : (ushort)2;
			BinSummary firstBin         = bins.OrderBy(bin => bin.BinNumber).FirstOrDefault();
			return firstBin?.BinNumber ?? defaultBinNumber;
		}

		private void ExecuteWithLogging(string operation, string filePath, string testItem, string site, string pin, string rawInputValue, string targetRecord, Action action)
		{
			try
			{
				action();
			}
			catch(Exception ex)
			{
				LogException(operation, ex, filePath, testItem, site, pin, rawInputValue, targetRecord, "DoWork.WriteRecord", _logPath, _outputPath);
				throw;
			}
		}

		private static void LogException(string operation, Exception ex, string filePath, string testItem, string site, string pin, string rawInputValue, string targetRecord, string stage = null, string inputFolder = null, string outputPath = null)
		{
			string safeMessage = ex?.Message?.Replace(Environment.NewLine, " ");
			TraceLogger.WriteLine($"{LogTag} stage=\"{stage ?? "N/A"}\" op={operation} inputFolder=\"{inputFolder ?? "N/A"}\" outputPath=\"{outputPath ?? "N/A"}\" filePath=\"{filePath ?? "N/A"}\" testItem=\"{testItem ?? "N/A"}\" site=\"{site ?? "N/A"}\" pin=\"{pin ?? "N/A"}\" rawInput=\"{rawInputValue ?? "N/A"}\" target=\"{targetRecord ?? "N/A"}\" message=\"{safeMessage}\" stack=\"{ex?.StackTrace}\"");
		}

		private static void LogTraceError(string operation, string filePath, string binKey, IEnumerable<string> tokens, string reason)
		{
			string tokenText = tokens == null ? string.Empty : string.Join("|", tokens);
			TraceLogger.WriteLine($"{LogTag} op={operation} filePath=\"{filePath ?? "N/A"}\" binKey=\"{binKey ?? "N/A"}\" tokens=\"{tokenText}\" reason=\"{reason ?? "N/A"}\"");
		}

		private static IEnumerable<BinSummary> BuildBinSummaries(Dictionary<string, IEnumerable<string>> binMap, IEnumerable<CChipData> chipDataList)
		{
			List<BinSummary> bins = new List<BinSummary>();

			if(binMap != null && binMap.Count > 0)
			{
				foreach(KeyValuePair<string, IEnumerable<string>> item in binMap)
				{
					string[] tokens = item.Value?.Where(token => !string.IsNullOrWhiteSpace(token)).ToArray() ?? Array.Empty<string>();

					if(string.IsNullOrWhiteSpace(item.Key))
					{
						LogTraceError("BuildBinSummaries", null, item.Key, tokens, "Bin key is empty; skip this bin entry");
						continue;
					}

					if(!TryExtractUShort(item.Key, out ushort binNumber))
					{
						LogTraceError("BuildBinSummaries", null, item.Key, tokens, "Unable to parse bin number from key; skip this bin entry");
						continue;
					}
					uint? parsedBinCount = tokens.Select(TryParseCountToken).FirstOrDefault(value => value.HasValue);
					uint  binCount       = parsedBinCount ?? 0;

					if(!parsedBinCount.HasValue)
					{
						LogTraceError("BuildBinSummaries", null, item.Key, tokens, "No valid bin count token found; fallback to 0");
					}
					string binPassFail = tokens.Select(NormalizePassFailToken).FirstOrDefault(token => token != null)                                 ?? InferPassFailFromBinNumber(binNumber);
					string binName     = tokens.FirstOrDefault(token => !TryParseCountToken(token).HasValue && NormalizePassFailToken(token) == null) ?? item.Key;

					bins.Add(new BinSummary
					{
						BinNumber   = binNumber,
						BinCount    = binCount,
						BinPassFail = binPassFail,
						BinName     = binName,
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
						BinName     = "PASS",
					});
				}

				if(failCount > 0)
				{
					bins.Add(new BinSummary
					{
						BinNumber   = 2,
						BinCount    = failCount,
						BinPassFail = "F",
						BinName     = "FAIL",
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

		private static bool TryExtractPinIndex(string pinName, out ushort pinIndex)
		{
			pinIndex = 0;

			if(string.IsNullOrWhiteSpace(pinName))
			{
				return false;
			}
			Match parenMatch = Regex.Match(pinName, @"\(\s*(\d+)\s*\)");

			if(parenMatch.Success && ushort.TryParse(parenMatch.Groups[1].Value, out pinIndex))
			{
				return true;
			}
			return false;
		}

		private static string ExtractPinLogicalName(string pinName)
		{
			if(string.IsNullOrWhiteSpace(pinName))
			{
				return string.Empty;
			}
			Match match = Regex.Match(pinName, @"^(?<name>[^\(\s]+)");
			return match.Success ? match.Groups["name"].Value.Trim() : pinName.Trim();
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

		private static uint? TryParseCountToken(string token)
		{
			if(string.IsNullOrWhiteSpace(token))
			{
				return null;
			}
			Match match = Regex.Match(token, @"\d+");
			return match.Success && uint.TryParse(match.Value, out uint value) ? value : null;
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

		private sealed class PinInfo
		{
			public ushort PinIndex    { get; set; }
			public string RawPinName  { get; set; }
			public string LogicalName { get; set; }
		}

		private sealed class PartSiteSummary
		{
			public int             PartIndex  { get; set; }
			public byte            SiteNumber { get; set; }
			public bool            IsPass     { get; set; }
			public List<CChipData> Chips      { get; set; } = new List<CChipData>();
		}
	}
}