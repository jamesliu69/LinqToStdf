using Stdf;
using Stdf.Records.V4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace STDF
{
	/// <summary>STDF 檔案轉換處理類別   負責讀取 P2020 測試日誌和摘要檔案，解析資料並產生標準 STDF V4 格式檔案；亦支援以 CSV 測試資料（DETAIL/RESULT）作為輸入（_csvInput=true 時改走 CSV 剖析）。</summary>
	public class CStdf
	{
		/// <summary>錯誤追蹤標籤，用於記錄日誌時標識異常來源</summary>
		private const string LogTag = "[STDF-TRACE-ERR]";
		/// <summary>整批彙總記錄（TSR/HBR/SBR 與 lot 級 PCR）使用的 HeadNumber（255）。</summary>
		private const byte   SummaryHeadNumber = byte.MaxValue;
		/// <summary>整批彙總記錄（TSR 與 lot 級 PCR）使用的 SiteNumber（0）。</summary>
		private const byte   SummarySiteNumber = 0;

		/// <summary>輸入日誌檔案的路徑；CSV 模式下為 CSV 檔案路徑（P2020 模式為記錄檔資料夾）。</summary>
		private readonly string _logPath;

		/// <summary>輸出 STDF 檔案的路徑</summary>
		private readonly string _outputPath;
		/// <summary>是否以 CSV 測試資料作為輸入；決定 AnalyzeFile 走 AnalyzeCsvFile 或 P2020 資料夾剖析。</summary>
		private readonly bool _csvInput;
		/// <summary>CSV 輸入是否出現 Retest 資料列；作為 MIR RetestCode 與 PCR RetestCount 的依據之一。</summary>
		private bool _hasCsvRetest;

		/// <summary>STDF 檔案寫入器，負責產生標準 STDF V4 格式輸出</summary>
		private StdfFileWriter _stdfWriter;

		/// <summary>摘要日誌檔案的路徑（目前與 _logPath 相同；此欄位目前未被讀取）</summary>
		private readonly string _summaryLog;

		/// <summary>檔案參數物件，用於解析和儲存摘要檔案內容；CSV 模式則由 CSV 檔本身的欄位填入批號與起訖時間</summary>
		private CFileParam _fileParam;

		/// <summary>P2020 日誌解析器，用於讀取和處理測試日誌檔案</summary>
		private CP2020 _p2020;

		/// <summary>建構 CStdf 類別的新實例，設定日誌與輸出路徑；寫入器於轉檔時建立。</summary>
		public CStdf(string logPath, string outputPath)
			: this(logPath, outputPath, false)
		{
		}

		public CStdf(string inputPath, string outputPath, bool csvInput)
		{
			Debug.Assert(inputPath != null, nameof(inputPath) + " != null");
			_logPath    = inputPath;
			_summaryLog = inputPath;
			Debug.Assert(outputPath != null, nameof(outputPath) + " != null");
			_outputPath = outputPath;
			_csvInput   = csvInput;
		}

		/// <summary>分析並處理輸入的測試日誌檔案   解析 P2020 格式的日誌檔案以及對應的摘要檔案，為後續產生 STDF 檔案做準備</summary>
		private void AnalyzeFile()
		{
			if(_csvInput)
			{
				AnalyzeCsvFile();
				return;
			}
			try
			{
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
					InvalidDataException ex = new InvalidDataException("找不到可供解析的測試 Log（*log，且檔名不含 Summary）。");
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
				if(logFiles.Concat(new[] { summaryPath }).Any(path => string.Equals(Path.GetFullPath(path), Path.GetFullPath(_outputPath), StringComparison.OrdinalIgnoreCase)))
				{
					throw new InvalidOperationException("P2020 來源 Log/Summary 與 STDF 輸出檔不可相同。");
				}
				_fileParam.AnalyzeFile();
			}
			catch(Exception e)
			{
				LogException("AnalyzeFile", e, _logPath, null, null, null, null, "AnalyzeFlow", "AnalyzeFile", _logPath, _outputPath);
				Console.WriteLine($@"處理中有錯誤發生: {e.Message}");
				throw;
			}
		}

		/// <summary>CSV 輸入模式的剖析：以 CP950 讀取 _logPath 的 CSV，驗證必要欄位與各列的判定/身分欄位，依身分鍵遞增分配 partId，將每列轉成 CChipData 加入 _p2020.ChipDataList，並把批號與起訖時間寫入 _fileParam。</summary>
		private void AnalyzeCsvFile()
		{
			string[] headers;
			List<string[]> rows = new List<string[]>();
			using(StreamReader reader = new StreamReader(_logPath, Encoding.GetEncoding(950), true))
			{
				string line;
				if((line = reader.ReadLine()) == null)
				{
					throw new InvalidDataException("CSV 檔案是空的。");
				}
				headers = ParseCsvLine(line);
				while((line = reader.ReadLine()) != null)
				{
					if(line.Length > 0)
					{
						rows.Add(ParseCsvLine(line));
					}
				}
			}

			Dictionary<string, int> columns = headers.Select((name, index) => new { name, index })
				.ToDictionary(item => item.name.Trim(), item => item.index, StringComparer.OrdinalIgnoreCase);
			string[] required = { "Date", "Time", "LotNo", "Serial", "PieceX", "PieceY", "DataStatus", "StepJudge", "Step", "Retest", "Dut", "Item", "Point", "No", "Direction", "PJudge", "Result", "Meas", "Unit", "Lower", "Upper", "Error" };
			foreach(string name in required)
			{
				if(!columns.ContainsKey(name))
				{
					throw new InvalidDataException($"CSV 缺少必要欄位：{name}");
				}
			}

			string Get(string[] row, string name)
			{
				if(row.Length != headers.Length)
				{
					throw new InvalidDataException($"CSV 欄位數不符：預期 {headers.Length} 欄，實際 {row.Length} 欄。");
				}
				return row[columns[name]].Trim();
			}

			_p2020 = new CP2020(Array.Empty<string>());
			_fileParam = new CFileParam(_logPath)
			{
				FilePath = _logPath,
				TestItemName = "CSV_Test",
			};
			Dictionary<string, int> sourcePartIds = new Dictionary<string, int>(StringComparer.Ordinal);
			int nextPartId = 1;
			_hasCsvRetest = false;
			int sourceLine = 1;
			foreach(string[] row in rows)
			{
				sourceLine++;
				string judge = Get(row, "PJudge");
				if(judge == "點位結果" || judge == "詳細結果" || Get(row, "DataStatus") == "STEP狀態")
				{
					continue;
				}
				if(Get(row, "DataStatus") != "Test done")
				{
					throw new InvalidDataException($"CSV 第 {sourceLine} 列 DataStatus 無法識別。");
				}
				if(judge != "PASS" && judge != "L_NG" && judge != "MS_ER")
				{
					throw new InvalidDataException($"CSV 第 {sourceLine} 列 PJudge 無法識別。");
				}
				string stepJudge = Get(row, "StepJudge");
				if(stepJudge != "LSL" && stepJudge != "svOther")
				{
					throw new InvalidDataException($"CSV 第 {sourceLine} 列 StepJudge 無法識別。");
				}
				string dut = Get(row, "Dut");
				string serial = Get(row, "Serial");
				if(!int.TryParse(dut, NumberStyles.Integer, CultureInfo.InvariantCulture, out int dutNumber) || string.IsNullOrWhiteSpace(serial))
				{
					throw new InvalidDataException($"CSV 第 {sourceLine} 列 DUT/Serial 不完整或格式不符。");
				}
				if(!short.TryParse(Get(row, "PieceX"), NumberStyles.Integer, CultureInfo.InvariantCulture, out short pieceX) || pieceX == short.MinValue || !short.TryParse(Get(row, "PieceY"), NumberStyles.Integer, CultureInfo.InvariantCulture, out short pieceY) || pieceY == short.MinValue)
				{
					throw new InvalidDataException($"CSV 第 {sourceLine} 列 PieceX/PieceY 超出 STDF 座標範圍或格式不符。");
				}
				string lot = Get(row, "LotNo");
				if(string.IsNullOrWhiteSpace(lot))
				{
					throw new InvalidDataException($"CSV 第 {sourceLine} 列缺少 LotNo。");
				}
				DateTime testTime;
				if(!DateTime.TryParseExact(Get(row, "Date") + " " + Get(row, "Time"), "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out testTime))
				{
					throw new InvalidDataException($"CSV 第 {sourceLine} 列 Date/Time 格式不符。");
				}
				if(_fileParam.LotNumber == null)
				{
					_fileParam.LotNumber = lot;
					_fileParam.LotSTART = testTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
					_fileParam.LotEND = _fileParam.LotSTART;
				}
				else if(!string.Equals(_fileParam.LotNumber, lot, StringComparison.Ordinal))
				{
					throw new InvalidDataException("單一 CSV 包含多個 LotNo。");
				}
				else
				{
					if(testTime < DateTime.Parse(_fileParam.LotSTART, CultureInfo.InvariantCulture))
					{
						_fileParam.LotSTART = testTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
					}
					if(testTime > DateTime.Parse(_fileParam.LotEND, CultureInfo.InvariantCulture))
					{
						_fileParam.LotEND = testTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
					}
				}

				string item = Get(row, "Item");
				string step = Get(row, "Step");
				string measurementType = Get(row, "Result");
				string measurementKind = measurementType.IndexOf("(Cp)", StringComparison.OrdinalIgnoreCase) >= 0 ? " (Cp)" : measurementType.IndexOf("(Rp)", StringComparison.OrdinalIgnoreCase) >= 0 ? " (Rp)" : string.Empty;
				string retest = Get(row, "Retest");
				if(string.IsNullOrWhiteSpace(item) || !int.TryParse(step, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) || !int.TryParse(retest, NumberStyles.Integer, CultureInfo.InvariantCulture, out int retestNumber))
				{
					throw new InvalidDataException($"CSV 第 {sourceLine} 列 Step/Retest/Item 無法識別。");
				}
				if(retestNumber < 0)
				{
					throw new InvalidDataException($"CSV 第 {sourceLine} 列 Retest 不可為負數。");
				}
				_hasCsvRetest |= retestNumber > 0;
				string sourcePartKey = $"{serial.Length}:{serial}|{pieceX}|{pieceY}|{dutNumber}|{retestNumber}";
				if(!sourcePartIds.TryGetValue(sourcePartKey, out int partId))
				{
					partId = nextPartId++;
					sourcePartIds.Add(sourcePartKey, partId);
				}
				string errorText = Get(row, "Error");
				CChipData chip = new CChipData
				{
					FileName = Path.GetFileNameWithoutExtension(_logPath),
					Id = partId,
					Site = "1",
					Comment = $"Step {step} {item} Point {Get(row, "Point")} No {Get(row, "No")} Direction {Get(row, "Direction")}{measurementKind}",
					SourcePartId = serial,
					SourcePartKey = sourcePartKey,
					SourcePieceX = pieceX,
					SourcePieceY = pieceY,
					SourceDut = dut,
					SourceRetest = retestNumber,
					PassOrFail = judge == "PASS" ? "PASS" : judge == "L_NG" ? "FAIL" : "MS_ER",
					ErrorText = errorText,
					strMeasureValue = judge == "MS_ER" ? null : Get(row, "Meas"),
					LowLimit = Get(row, "Lower"),
					HighLimit = Get(row, "Upper"),
					MeasureUnit = Get(row, "Unit"),
					PinName = Get(row, "Point"),
				};
				if(judge == "MS_ER" && string.IsNullOrWhiteSpace(errorText))
				{
					throw new InvalidDataException($"CSV 第 {sourceLine} 列 MS_ER 缺少 Error 說明。");
				}
				ParseNullableFloat(chip.LowLimit, sourceLine, "Lower");
				ParseNullableFloat(chip.HighLimit, sourceLine, "Upper");
				if(judge != "MS_ER" && ParseNullableFloat(chip.strMeasureValue, sourceLine, "Meas") == null)
				{
					throw new InvalidDataException($"CSV 第 {sourceLine} 列 PASS/L_NG 缺少有效 Meas。");
				}
				_p2020.ChipDataList.Add(chip);
			}
			if(_p2020.ChipDataList.Count == 0)
			{
				throw new InvalidDataException("CSV 沒有可轉換的量測資料。");
			}
		}

		/// <summary>以 invariant culture 把字串解析為 float?：空白回 null；解析失敗、NaN/Infinity 或超出 float 範圍即拋 InvalidDataException。</summary>
		private static float? ParseNullableFloat(string raw, int line, string field)
		{
			if(string.IsNullOrWhiteSpace(raw)) return null;
			if(!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > float.MaxValue)
			{
				throw new InvalidDataException($"CSV 第 {line} 列 {field} 超出 STDF PTR 浮點範圍或格式不符。");
			}
			return (float)value;
		}

		/// <summary>以 invariant culture 解析時間字串；CSV 模式視為 UTC+8 本地時間轉為 UTC，P2020 模式直接 ToUniversalTime()。</summary>
		private DateTime ParseStdfTime(string value)
		{
			DateTime parsed = DateTime.Parse(value, CultureInfo.InvariantCulture);
			return _csvInput
				? new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified), TimeSpan.FromHours(8)).UtcDateTime
				: parsed.ToUniversalTime();
		}

		/// <summary>切分單行 CSV：支援雙引號欄位與 "" 跳脫；引號未配對時拋 InvalidDataException。</summary>
		private static string[] ParseCsvLine(string line)
		{
			List<string> fields = new List<string>();
			StringBuilder field = new StringBuilder();
			bool quoted = false;
			for(int i = 0; i < line.Length; i++)
			{
				char c = line[i];
				if(c == '"')
				{
					if(quoted && i + 1 < line.Length && line[i + 1] == '"') { field.Append('"'); i++; }
					else { quoted = !quoted; }
				}
				else if(c == ',' && !quoted) { fields.Add(field.ToString()); field.Clear(); }
				else { field.Append(c); }
			}
			if(quoted) throw new InvalidDataException("CSV 引號未配對。");
			fields.Add(field.ToString());
			return fields.ToArray();
		}

		/// <summary>執行STDF 轉檔主要工作流程   協調整個轉換過程，包括日誌解析、STDF 記錄生成和檔案輸出</summary>
		public void DoWork()
		{
			string workflowStage       = "DoWork.AnalyzeFile";
			string temporaryOutputPath = null;

			try
			{
				if(_csvInput && string.Equals(Path.GetFullPath(_logPath), Path.GetFullPath(_outputPath), StringComparison.OrdinalIgnoreCase))
				{
					throw new InvalidOperationException("CSV 輸入檔與 STDF 輸出檔不可相同。");
				}
				AnalyzeFile();
				workflowStage = "DoWork.CreateTemporaryOutput";
				string finalOutputPath = Path.GetFullPath(_outputPath);
				temporaryOutputPath = Path.Combine(Path.GetDirectoryName(finalOutputPath), ".stdf-" + Guid.NewGuid().ToString("N") + ".tmp");
				_stdfWriter = new StdfFileWriter(temporaryOutputPath, true);
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
				atr.ModifiedTime = DateTime.UtcNow;
				atr.CommandLine  = "";
				ExecuteWithLogging("WriteRecord", _logPath, null, null, null, null, "ATR", () => _stdfWriter.WriteRecord(atr));

				#region MIR（Master Information Record）：記錄整批測試作業的主資訊，例如批號、開始時間與測試程式版本。

				workflowStage = "DoWork.WriteMIR";
				Mir mir = new Mir();

				try
				{
					mir.SetupTime = ParseStdfTime(_fileParam.LotSTART);
					mir.StartTime = ParseStdfTime(_fileParam.LotSTART);
				}
				catch(Exception ex)
				{
					LogException("ParseDateTime", ex, _fileParam.FilePath, _fileParam.TestItemName, null, null, _fileParam.LotSTART, "MIR.SetupTime/StartTime", workflowStage, _logPath, _outputPath);
					throw;
				}
				mir.StationNumber        = 0;
				mir.ModeCode             = "P";
				mir.RetestCode           = _csvInput && _hasCsvRetest ? null : "N";
				mir.ProtectionCode       = "0";
				mir.BurnInTime           = 0;
				mir.CommandModeCode      = "0";
				mir.LotId                = _fileParam.LotNumber;
				mir.PartType             = "";
				mir.NodeName             = "";
				mir.TesterType           = "";
				mir.JobName              = _fileParam.TestProgramName ?? string.Empty;
				mir.JobRevision          = "";
				mir.SublotId             = "";
				mir.OperatorName         = _fileParam.Operator ?? string.Empty;
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
				mir.SpecificationName    = string.Empty;
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
							ptr.TestFlags       = BuildPtrTestFlags(chip.PassOrFail);
							ptr.ParametricFlags = 0;
							float? measurementValue;

							try
							{
								measurementValue = _csvInput ? ParseNullableFloat(chip.strMeasureValue, 0, "Meas") : TryExtractFloat(chip.strMeasureValue) ?? TryExtractFloat(chip.strMaxMeasureValue) ?? TryExtractFloat(chip.strMinMeasureValue);
							}
							catch(Exception ex)
							{
								LogException("ValueConversion", ex, chip.FileName, chip.Comment, chip.Site, chip.PinName, $"part={part.PartIndex};site={part.SiteNumber};values={chip.strMaxMeasureValue}|{chip.strMeasureValue}|{chip.strMinMeasureValue}", "PTR.Result", workflowStage, _logPath, _outputPath);
								throw;
							}
							ptr.Result   = measurementValue;
							ptr.TestText = string.IsNullOrWhiteSpace(chip.ErrorText) ? chip.Comment : $"{chip.Comment}: {chip.ErrorText}";
							ptr.AlarmId  = " ";

							if((_csvInput ? ParseNullableFloat(chip.LowLimit, 0, "Lower") : TryExtractFloat(chip.LowLimit)) is float lowLimitValue)
							{
								ptr.LowLimit = lowLimitValue;
							}

							if((_csvInput ? ParseNullableFloat(chip.HighLimit, 0, "Upper") : TryExtractFloat(chip.HighLimit)) is float highLimitValue)
							{
								ptr.HighLimit = highLimitValue;
							}
							ptr.Units = string.IsNullOrWhiteSpace(chip.MeasureUnit) ? ExtractUnits(chip.LowLimit, chip.HighLimit) : chip.MeasureUnit;
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
						prr.PartId    = string.IsNullOrWhiteSpace(firstChip.SourcePartId) ? $"{firstChip.FileName}-{part.PartIndex.ToString(CultureInfo.InvariantCulture)}-S{part.SiteNumber.ToString(CultureInfo.InvariantCulture)}" : firstChip.SourcePartId;
						prr.XCoordinate = firstChip.SourcePieceX;
						prr.YCoordinate = firstChip.SourcePieceY;
						if(firstChip.SourceDut != null && firstChip.SourceRetest.HasValue)
						{
							prr.PartText = $"Dut {firstChip.SourceDut} Retest {firstChip.SourceRetest.Value.ToString(CultureInfo.InvariantCulture)}";
						}
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
				byte summarySiteNumber = SummarySiteNumber;

				foreach(TestSummary summary in testSummaries.OrderBy(c => c.Key).Select(c => c.Value))
				{
					Tsr tsr = new Tsr();
					tsr.HeadNumber       = SummaryHeadNumber;
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
					hbr.HeadNumber  = SummaryHeadNumber;
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
					sbr.HeadNumber  = SummaryHeadNumber;
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
					pcr.FunctionalCount = null;
					pcr.RetestCount     = _csvInput ? (uint)siteGroup.Count(part => part.Chips[0].SourceRetest.GetValueOrDefault() > 0) : 0;
					pcr.AbortCount      = 0;
					ExecuteWithLogging("WriteRecord", _fileParam.FilePath, _fileParam.TestItemName, siteGroup.Key.ToString(CultureInfo.InvariantCulture), null, null, "PCR", () => _stdfWriter.WriteRecord(pcr));
				}
				uint totalPartCount = (uint)partSiteSummaries.Count;
				uint totalGoodCount = (uint)partSiteSummaries.Count(part => part.IsPass);
				Pcr  lotPcr         = new Pcr();
				lotPcr.HeadNumber      = SummaryHeadNumber;
				lotPcr.SiteNumber      = SummarySiteNumber;
				lotPcr.PartCount       = totalPartCount;
				lotPcr.GoodCount       = totalGoodCount;
				lotPcr.FunctionalCount = null;
				lotPcr.RetestCount     = _csvInput ? (uint)partSiteSummaries.Count(part => part.Chips[0].SourceRetest.GetValueOrDefault() > 0) : 0;
				lotPcr.AbortCount      = 0;
				ExecuteWithLogging("WriteRecord", _fileParam.FilePath, _fileParam.TestItemName, "0", null, null, "PCR", () => _stdfWriter.WriteRecord(lotPcr));

				#endregion

				#region MRR（Master Results Record）：標記整批測試結束資訊，例如完工時間與結束說明。

				workflowStage = "DoWork.WriteMRR";
				Mrr mrr = new Mrr();

				try
				{
					mrr.FinishTime = ParseStdfTime(_fileParam.LotEND);
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
				_stdfWriter = null;

				// 同目錄暫存檔完成並關閉後才發布，失敗時保留原本的輸出。
				workflowStage = "DoWork.PublishOutput";

				if(File.Exists(finalOutputPath))
				{
					File.Replace(temporaryOutputPath, finalOutputPath, null);
				}
				else
				{
					File.Move(temporaryOutputPath, finalOutputPath);
				}
				temporaryOutputPath = null;
			}
			catch(Exception ex)
			{
				LogException("DoWork", ex, _fileParam?.FilePath ?? _logPath, _fileParam?.TestItemName, null, null, null, "Workflow", workflowStage, _logPath, _outputPath);
				throw;
			}
			finally
			{
				try
				{
					_stdfWriter?.Dispose();
				}
				catch(Exception ex)
				{
					LogException("DisposeWriter", ex, temporaryOutputPath, null, null, null, null, "TemporaryOutput", workflowStage, _logPath, _outputPath);
				}
				finally
				{
					_stdfWriter = null;
				}

				if(temporaryOutputPath != null)
				{
					try
					{
						File.Delete(temporaryOutputPath);
					}
					catch(Exception ex)
					{
						LogException("DeleteTemporaryOutput", ex, temporaryOutputPath, null, null, null, null, "TemporaryOutput", workflowStage, _logPath, _outputPath);
					}
				}
			}
		}

		/// <summary>以正規表示式從字串抽出第一個數字並轉為 float；抽不到或轉換失敗回傳 null。</summary>
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

		/// <summary>把 "P"/"PASS" 正規化為 "P"、"F"/"FAIL" 正規化為 "F"（大小寫不敏感）；其餘回 null。</summary>
		private static string NormalizePassFailResult(string passOrFailText)
		{
			string normalized = passOrFailText?.Trim();

			if(string.Equals(normalized, "P", StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "PASS", StringComparison.OrdinalIgnoreCase))
			{
				return "P";
			}
			if(string.Equals(normalized, "F", StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "FAIL", StringComparison.OrdinalIgnoreCase))
			{
				return "F";
			}
			return null;
		}

		/// <summary>判定正規化後的判定文字是否為 "P"。</summary>
		private static bool IsPassResult(string passOrFailText) => NormalizePassFailResult(passOrFailText) == "P";

		/// <summary>把判定文字映射為 PTR TEST_FLG："MS_ER"→0x82、無法辨識→0x10（invalid）、"P"→0x00、"F"→0x80。</summary>
		private static byte BuildPtrTestFlags(string passOrFailText)
		{
			const byte passFailInvalidMask = 0x10;
			const byte failMask            = 0x80;
			string     result               = NormalizePassFailResult(passOrFailText);

			if(string.Equals(passOrFailText?.Trim(), "MS_ER", StringComparison.OrdinalIgnoreCase))
			{
				return 0x82;
			}
			if(result == null)
			{
				return passFailInvalidMask;
			}
			return result == "P" ? (byte)0 : failMask;
		}

		/// <summary>從下限文字（空白時改用上限）剝除非字母字元取出單位字串；兩者皆無則回空字串。</summary>
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

		/// <summary>把 Id&gt;0 的晶片依「FileName|Id|站點」分組成 PartSiteSummary（IsPass 為該組全部 PASS），驗證判定值（CSV 模式另允許 MS_ER），最後依 PartIndex、SiteNumber 排序。</summary>
		private List<PartSiteSummary> BuildPartSiteSummaries(IEnumerable<CChipData> chipDataList)
		{
			List<PartSiteSummary> summaries     = new List<PartSiteSummary>();
			List<CChipData>       eligibleChips = (chipDataList ?? Enumerable.Empty<CChipData>()).Where(chip => chip != null && chip.Id > 0).ToList();

			foreach(CChipData chip in eligibleChips)
			{
				if(NormalizePassFailResult(chip.PassOrFail) == null && !(_csvInput && string.Equals(chip.PassOrFail, "MS_ER", StringComparison.OrdinalIgnoreCase)))
				{
					string result = string.IsNullOrWhiteSpace(chip.PassOrFail) ? "<empty>" : chip.PassOrFail.Trim();
					throw new InvalidDataException($"Unsupported P2020 PASS/FAIL result '{result}' in '{chip.FileName}' for part {chip.Id}.");
				}
			}

			foreach(IGrouping<string, CChipData> group in eligibleChips.GroupBy(chip => $"{chip.FileName}|{chip.Id}|{NormalizeSite(chip.Site)}"))
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

		/// <summary>把站點字串轉為 byte；解析失敗預設為 1。</summary>
		private static byte NormalizeSite(string siteRaw)
		{
			return byte.TryParse(siteRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte siteNumber) ? siteNumber : (byte)1;
		}

		/// <summary>在 bin 清單中找出 BinPassFail 符合預期 P/F 的唯一 bin 編號；符合者不是恰一個即拋 InvalidDataException。</summary>
		private static ushort ResolveBinNumberForOutcome(IEnumerable<BinSummary> bins, bool isPass)
		{
			string     expectedPassFail = isPass ? "P" : "F";
			ushort[]   matchingNumbers  = (bins ?? Enumerable.Empty<BinSummary>())
				.Where(bin => bin != null && string.Equals(bin.BinPassFail, expectedPassFail, StringComparison.OrdinalIgnoreCase))
				.Select(bin => bin.BinNumber)
				.Distinct()
				.ToArray();

			if(matchingNumbers.Length == 1)
			{
				return matchingNumbers[0];
			}
			throw new InvalidDataException($"Cannot resolve the {(isPass ? "PASS" : "FAIL")} PRR bin: expected exactly one matching bin number, found {matchingNumbers.Length}.");
		}

		/// <summary>執行指定動作並記錄例外情況   包裝動作執行，在發生異常時記錄詳細資訊後重新拋出例外</summary>
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

		/// <summary>記錄異常資訊至每日追蹤日誌，包含工作流程階段、輸入輸出路徑及完整例外鏈。</summary>
		private static void LogException(string operation, Exception ex, string filePath, string testItem, string site, string pin, string rawInputValue, string targetRecord, string stage = null, string inputFolder = null, string outputPath = null)
		{
			TraceLogger.Log("ERROR", operation, $"stage=\"{stage ?? "N/A"}\" inputFolder=\"{inputFolder ?? "N/A"}\" outputPath=\"{outputPath ?? "N/A"}\" filePath=\"{filePath ?? "N/A"}\" testItem=\"{testItem ?? "N/A"}\" site=\"{site ?? "N/A"}\" pin=\"{pin ?? "N/A"}\" rawInput=\"{rawInputValue ?? "N/A"}\" target=\"{targetRecord ?? "N/A"}\"", ex);
		}

		/// <summary>以 TraceLogger 輸出單行 bin 對照錯誤追蹤（op/filePath/binKey/tokens/reason）。</summary>
		private static void LogTraceError(string operation, string filePath, string binKey, IEnumerable<string> tokens, string reason)
		{
			string tokenText = tokens == null ? string.Empty : string.Join("|", tokens);
			TraceLogger.WriteLine($"{LogTag} op={operation} filePath=\"{filePath ?? "N/A"}\" binKey=\"{binKey ?? "N/A"}\" tokens=\"{tokenText}\" reason=\"{reason ?? "N/A"}\"");
		}

		/// <summary>把 bin 對照字典（鍵=bin 編號、值=數量/P-F/名稱 token）解析成 BinSummary：鍵無法轉編號或無有效數量 token 時記追蹤後跳過/補 0，P/F 取第一個符合 token 否則依 bin 編號推斷；完全沒有有效 bin 時改由晶片資料統計出 PASS(bin 1)/FAIL(bin 2)。</summary>
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
				uint passCount = (uint)chipDataList.Count(chip => IsPassResult(chip.PassOrFail));
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

		/// <summary>用 \d+ 從字串抽出第一組數字並轉為 ushort；抽不到或轉換失敗回 false。</summary>
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

		/// <summary>從腳位名稱的括號「( 數字 )」抽出腳位索引；無括號數字回 false。</summary>
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

		/// <summary>取腳位名稱中 '(' 或空白前的第一個 token 當邏輯名稱。</summary>
		private static string ExtractPinLogicalName(string pinName)
		{
			if(string.IsNullOrWhiteSpace(pinName))
			{
				return string.Empty;
			}
			Match match = Regex.Match(pinName, @"^(?<name>[^\(\s]+)");
			return match.Success ? match.Groups["name"].Value.Trim() : pinName.Trim();
		}

		/// <summary>把 bin token 的 "P"/"PASS" 正規化為 "P"、"F"/"FAIL" 正規化為 "F"（大小寫不敏感）；其餘回 null。</summary>
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

		/// <summary>用 \d+ 從 token 抽出數字並轉為 uint；抽不到或轉換失敗回 null。</summary>
		private static uint? TryParseCountToken(string token)
		{
			if(string.IsNullOrWhiteSpace(token))
			{
				return null;
			}
			Match match = Regex.Match(token, @"\d+");
			return match.Success && uint.TryParse(match.Value, out uint value) ? value : null;
		}

		/// <summary>bin 編號 1 推斷為 "P"，其餘為 "F"。</summary>
		private static string InferPassFailFromBinNumber(ushort binNumber) => binNumber == 1 ? "P" : "F";

		/// <summary>測試摘要資訊，用於彙整每個測試項目的統計數據</summary>
		private sealed class TestSummary
		{
			/// <summary>測試編號</summary>
			public uint TestNumber { get; set; }
			/// <summary>測試名稱</summary>
			public string TestName { get; set; }
			/// <summary>執行次數</summary>
			public uint ExecutedCount { get; set; }
			/// <summary>失敗次數</summary>
			public uint FailedCount { get; set; }
			/// <summary>是否有測量值</summary>
			public bool HasMeasurement { get; set; }
			/// <summary>最小測量值</summary>
			public float? TestMin { get; set; }
			/// <summary>最大測量值</summary>
			public float? TestMax { get; set; }
			/// <summary>測量值總和</summary>
			public float TestSum { get; set; }
			/// <summary>測量值平方和總和</summary>
			public float TestSumOfSquares { get; set; }
		}

		/// <summary>Bin 分類摘要，用於彙整硬體或軟體 Bin 的分類結果</summary>
		private sealed class BinSummary
		{
			/// <summary>Bin 編號</summary>
			public ushort BinNumber { get; set; }
			/// <summary>Bin 內的零件數量</summary>
			public uint BinCount { get; set; }
			/// <summary>Bin 的通過/失敗標記（P=Pass, F=Fail）</summary>
			public string BinPassFail { get; set; }
			/// <summary>Bin 名稱或描述</summary>
			public string BinName { get; set; }
		}

		/// <summary>腳位資訊，用於儲存腳位的實體名稱與邏輯名稱對應關係</summary>
		private sealed class PinInfo
		{
			/// <summary>腳位索引編號</summary>
			public ushort PinIndex { get; set; }
			/// <summary>腳位的實體名稱（來自原始資料）</summary>
			public string RawPinName { get; set; }
			/// <summary>腳位的邏輯名稱（經過標準化處理）</summary>
			public string LogicalName { get; set; }
		}

		/// <summary>零件站點摘要，用於彙整每個零件在特定站點的測試結果</summary>
		private sealed class PartSiteSummary
		{
			/// <summary>零件編號（Part Index）</summary>
			public int PartIndex { get; set; }
			/// <summary>站點編號（Site Number）</summary>
			public byte SiteNumber { get; set; }
			/// <summary>是否全部通過（該站點下所有晶片都是 PASS）</summary>
			public bool IsPass { get; set; }
			/// <summary>該零件在該站點的所有晶片資料清單</summary>
			public List<CChipData> Chips { get; set; } = new List<CChipData>();
		}
	}
}
