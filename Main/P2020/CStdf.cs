using Stdf;
using Stdf.Records.V4;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace STDF
{
	public class CStdf
	{
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
				_p2020 = CP2020.CreateInstance(Directory.GetFiles(_logPath, "*.log"), 0);
				_p2020.AnalyzeFile();
				_fileParam = new CFileParam(Directory.GetFiles(_logPath, "*.txt")[0]);
				_fileParam.AnalyzeFile();
			}
			catch(Exception e)
			{
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
			_stdfWriter.WriteRecord(far);
			Atr atr = new Atr();
			atr.ModifiedTime = DateTime.Now;
			atr.CommandLine  = "";
			_stdfWriter.WriteRecord(atr);

			#region MIR

			Mir mir = new Mir();
			mir.SetupTime            = DateTime.Parse(_fileParam.LotSTART);
			mir.StartTime            = DateTime.Parse(_fileParam.LotSTART);
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
			_stdfWriter.WriteRecord(mir);

			#endregion

			#region SDR

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
			_stdfWriter.WriteRecord(sdr);

			#endregion

			#region Prm

			Pmr pmr = new Pmr();
			pmr.PinIndex     = 1;
			pmr.ChannelType  = 0;
			pmr.ChannelName  = "";
			pmr.PhysicalName = "";
			pmr.LogicalName  = "";
			pmr.HeadNumber   = 1;
			pmr.SiteNumber   = 0;
			_stdfWriter.WriteRecord(pmr);

			#endregion

			#region PGR

			Pgr pgr = new Pgr();
			pgr.GroupIndex = 1;
			pgr.GroupName  = "G1_OPPN";

			pgr.PinIndexes = new ushort[]
							 {
								 1
							 };
			_stdfWriter.WriteRecord(pgr);

			#endregion

			#region PTR

			for(int i = 0; i < _p2020.ChipDataList.Count; i++)
			{
				CChipData chip = _p2020.ChipDataList[i];
				Pir       pir  = new Pir();
				pir.HeadNumber = 1;
				pir.SiteNumber = Convert.ToByte(chip.Site);
				_stdfWriter.WriteRecord(pir);
				Ptr ptr = new Ptr();
				ptr.TestNumber      = 1;
				ptr.HeadNumber      = 1;
				ptr.SiteNumber      = Convert.ToByte(chip.Site);
				string passOrFailText = chip.PassOrFail?.Trim() ?? string.Empty;
				bool   isPass         = passOrFailText.Equals("PASS", StringComparison.OrdinalIgnoreCase) ||
										passOrFailText.Equals("Pass", StringComparison.OrdinalIgnoreCase);
				ptr.TestFlags       = isPass ? (byte)1 : (byte)0;
				ptr.ParametricFlags = 0;
				float? measurementValue = float.Parse(Regex.Match(chip.strMaxMeasureValue, @"[-+]?\d+\.?\d*").Value);
				ptr.Result                   = measurementValue;
				ptr.TestText                 = chip.Comment;
				ptr.AlarmId                  = " ";
				ptr.OptionalFlags            = 0;
				ptr.ResultScalingExponent    = 6;
				ptr.LowLimitScalingExponent  = 6;
				ptr.HighLimitScalingExponent = 6;
				string lowLimitText  = chip.LowLimit;
				string highLimitText = chip.HighLimit;

				if(!string.IsNullOrWhiteSpace(lowLimitText))
				{
					float? lowLimitValue = float.Parse(Regex.Match(lowLimitText, @"[-+]?\d+\.?\d*").Value);
					ptr.LowLimit = lowLimitValue;
				}
				if(!string.IsNullOrWhiteSpace(highLimitText))
				{
					float? highLimitValue = float.Parse(Regex.Match(highLimitText, @"[-+]?\d+\.?\d*").Value);
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
				_stdfWriter.WriteRecord(ptr);
				Prr prr = new Prr();
				prr.HeadNumber = 1;
				prr.SiteNumber = Convert.ToByte(chip.Site);
				_stdfWriter.WriteRecord(prr);
			}

			#endregion

			#region NO TSR

			byte summarySiteNumber = _p2020.ChipDataList.Count > 0 ? Convert.ToByte(_p2020.ChipDataList[0].Site) : (byte)1;
			Tsr tsr = new Tsr();
			tsr.HeadNumber       = 1;
			tsr.SiteNumber       = summarySiteNumber;
			tsr.TestType         = "P";
			tsr.TestNumber       = 1000;
			tsr.ExecutedCount    = 0;
			tsr.FailedCount      = 0;
			tsr.AlarmCount       = 0;
			tsr.TestName         = "Sink out I";
			tsr.SequencerName    = "seqU751";
			tsr.TestLabel        = "";
			tsr.TestTime         = null;
			tsr.TestMin          = null;
			tsr.TestMax          = null;
			tsr.TestSum          = null;
			tsr.TestSumOfSquares = null;
			_stdfWriter.WriteRecord(tsr);

			#endregion

			#region NO HBR

			Hbr hbr = new Hbr();
			hbr.HeadNumber  = 1;
			hbr.SiteNumber  = summarySiteNumber;
			hbr.BinNumber   = 2;
			hbr.BinCount    = 2;
			hbr.BinPassFail = "P";
			hbr.BinName     = 2.ToString();
			_stdfWriter.WriteRecord(hbr);

			#endregion

			#region NO SBR

			Sbr sbr = new Sbr();
			sbr.HeadNumber  = 1;
			sbr.SiteNumber  = summarySiteNumber;
			sbr.BinNumber   = 2;
			sbr.BinCount    = 2;
			sbr.BinPassFail = "P";
			sbr.BinName     = 2.ToString();
			_stdfWriter.WriteRecord(sbr);

			#endregion

			#region PCR

			Pcr pcr = new Pcr();
			pcr.HeadNumber = 1;
			pcr.SiteNumber = summarySiteNumber;
			_stdfWriter.WriteRecord(pcr);

			#endregion

			#region MRR

			Mrr mrr = new Mrr();
			mrr.FinishTime      = DateTime.Parse(_fileParam.LotEND);
			mrr.DispositionCode = " ";
			mrr.UserDescription = " ";
			mrr.ExecDescription = " ";
			_stdfWriter.WriteRecord(mrr);

			#endregion

			_stdfWriter.Dispose();
		}
	}
}
