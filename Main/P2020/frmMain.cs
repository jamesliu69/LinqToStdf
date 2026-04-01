using Stdf;
using Stdf.Records.V4;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace STDF
{
	public partial class frmMain : Form
	{
		private CFileParam _fileParam;
		private CP2020     _p2020;

		public frmMain() => InitializeComponent();

		private void btnGenerateStdf_Click(object sender, EventArgs e)
		{
			// 將分析結果輸出成 STDF 檔案。
			string stdfPath = "C:\\STDFATDF\\jamesliu_test.stdf";
			File.Delete(stdfPath);
			StdfFileWriter stdfWriter = new StdfFileWriter(stdfPath, true);
			Far            far        = new Far();
			far.CpuType     = 2;
			far.StdfVersion = 4;
			stdfWriter.WriteRecord(far);
			Atr atr = new Atr();
			atr.ModifiedTime = DateTime.Now;
			atr.CommandLine  = "";
			stdfWriter.WriteRecord(atr);

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
			stdfWriter.WriteRecord(mir);

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
			stdfWriter.WriteRecord(sdr);

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
			stdfWriter.WriteRecord(pmr);

			#endregion

			#region PGR

			Pgr pgr = new Pgr();
			pgr.GroupIndex = 1;
			pgr.GroupName  = "G1_OPPN";

			pgr.PinIndexes = new ushort[]
							 {
								 1
							 };
			stdfWriter.WriteRecord(pgr);

			#endregion

			#region PTR

			for(int i = 0; i < _p2020.ChipDataList.Count; i++)
			{
				CChipData chip = _p2020.ChipDataList[i];
				Pir       pir  = new Pir();
				pir.HeadNumber = 1;
				pir.SiteNumber = Convert.ToByte(chip.Site);
				stdfWriter.WriteRecord(pir);
				Ptr ptr = new Ptr();
				ptr.TestNumber      = 1;
				ptr.HeadNumber      = 1;
				ptr.SiteNumber      = Convert.ToByte(chip.Site);
				ptr.TestFlags       = chip.PassOrFail == "PASS" ? (byte)1 : (byte)0;
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
				else
				{
					ptr.LowLimit = 0;
				}

				if(!string.IsNullOrWhiteSpace(highLimitText))
				{
					float? highLimitValue = float.Parse(Regex.Match(highLimitText, @"[-+]?\d+\.?\d*").Value);
					ptr.HighLimit = highLimitValue;
				}
				else
				{
					ptr.HighLimit = 0;
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
				stdfWriter.WriteRecord(ptr);
				Prr prr = new Prr();
				prr.HeadNumber = 1;
				prr.SiteNumber = Convert.ToByte(chip.Site);
				stdfWriter.WriteRecord(prr);
			}

			#endregion

			#region NO TSR

			Tsr tsr = new Tsr();
			tsr.HeadNumber       = 1;
			tsr.SiteNumber       = (byte?)_fileParam.SiteCount;
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
			stdfWriter.WriteRecord(tsr);

			#endregion

			#region NO HBR

			Hbr hbr = new Hbr();
			hbr.HeadNumber  = 1;
			hbr.SiteNumber  = (byte?)_fileParam.SiteCount;
			hbr.BinNumber   = 2;
			hbr.BinCount    = 2;
			hbr.BinPassFail = "P";
			hbr.BinName     = 2.ToString();
			stdfWriter.WriteRecord(hbr);

			#endregion

			#region NO SBR

			Sbr sbr = new Sbr();
			sbr.HeadNumber  = 1;
			sbr.SiteNumber  = (byte?)_fileParam.SiteCount;
			sbr.BinNumber   = 2;
			sbr.BinCount    = 2;
			sbr.BinPassFail = "P";
			sbr.BinName     = 2.ToString();
			stdfWriter.WriteRecord(sbr);

			#endregion

			#region PCR

			Pcr pcr = new Pcr();
			pcr.HeadNumber = 1;
			pcr.SiteNumber = (byte?)_fileParam.SiteCount;
			stdfWriter.WriteRecord(pcr);

			#endregion

			#region MRR

			Mrr mrr = new Mrr();
			mrr.FinishTime      = DateTime.Parse(_fileParam.LotEND);
			mrr.DispositionCode = " ";
			mrr.UserDescription = " ";
			mrr.ExecDescription = " ";
			stdfWriter.WriteRecord(mrr);

			#endregion

			stdfWriter.Dispose();
		}

		private void btnAnalyzeSource_Click(object sender, EventArgs e)
		{
			// 可在這裡切換不同 P2020 資料來源做轉檔測試。
			Stopwatch s = new Stopwatch();
			s.Start();
			_p2020 = CP2020.CreateInstance(Directory.GetFiles(@"C:\STDFATDF\2023-09-18-02-59-16\New", "*.txt"), 0);
			_p2020.AnalyzeFile();
			s.Stop();
			Console.WriteLine(s.ElapsedMilliseconds);

			// 讀取對應的測試參數檔。
			_fileParam = new CFileParam(@"C:\Users\USER1\Documents\Pti_Doc\Project\Tester STDF\P2020 8 Site\2023-09-06-14-06-02.txt");
			_fileParam.AnalyzeFile();

			//C:\Users\USER1\Documents\Pti_Doc\Project\Jerry\P2020\P2020_Data Log\Data Log
		}

		private void txtInputPath_KeyDown(object sender, KeyEventArgs e)
		{
			// 保留按鍵事件，以便未來加入路徑輸入驗證或快速執行。
		}
	}
}