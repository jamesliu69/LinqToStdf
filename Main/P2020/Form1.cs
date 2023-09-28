using Stdf;
using Stdf.Records.V4;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace P2020
{
	public partial class Form1 : Form
	{
		private CFileParam _FileParam;
		private CP2020     _P2020;

		public Form1() => InitializeComponent();

		private void button1_Click(object sender, EventArgs e)
		{
			File.Delete("C:\\STDFATDF\\jamesliu_test.stdf");
			StdfFileWriter writer = new StdfFileWriter("C:\\STDFATDF\\jamesliu_test.stdf", true);
			Far            far    = new Far();
			far.CpuType     = 2;
			far.StdfVersion = 4;
			writer.WriteRecord(far);
			Atr atr = new Atr();
			atr.ModifiedTime = DateTime.Now;
			atr.CommandLine  = "";
			writer.WriteRecord(atr);

#region MIR

			Mir mir = new Mir();
			mir.SetupTime            = DateTime.Parse(_FileParam.LotSTART);
			mir.StartTime            = DateTime.Parse(_FileParam.LotSTART);
			mir.StationNumber        = 0;
			mir.ModeCode             = "P";
			mir.RetestCode           = "N";
			mir.ProtectionCode       = "0";
			mir.BurnInTime           = 0;
			mir.CommandModeCode      = "0";
			mir.LotId                = _FileParam.LotNumber;
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
			mir.SpecificationName    = _FileParam.TestProgramName;
			mir.SpecificationVersion = "";
			mir.FlowId               = "";
			mir.SetupId              = "";
			mir.DesignRevision       = "";
			mir.EngineeringId        = "";
			mir.RomCode              = "";
			mir.SerialNumber         = "";
			mir.SupervisorName       = "";
			writer.WriteRecord(mir);

#endregion

#region SDR

			Sdr sdr = new Sdr();
			sdr.HeadNumber    = 1;
			sdr.SiteGroup     = 1;
			sdr.SiteNumbers   = new byte[] { 1 };
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
			writer.WriteRecord(sdr);

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
			writer.WriteRecord(pmr);

#endregion

#region PGR

			Pgr pgr = new Pgr();
			pgr.GroupIndex = 1;
			pgr.GroupName  = "G1_OPPN";
			pgr.PinIndexes = new ushort[] { 1 };
			writer.WriteRecord(pgr);

#endregion

#region PTR

			for(int i = 1; i < _P2020.lstChipData.Count; i++)
			{
				CChipData chip = _P2020.lstChipData[i - 1];
				Pir       pir  = new Pir();
				pir.HeadNumber = 1;
				pir.SiteNumber = Convert.ToByte(chip.Site);
				writer.WriteRecord(pir);
				Ptr ptr = new Ptr();
				ptr.TestNumber      = 1;
				ptr.HeadNumber      = 1;
				ptr.SiteNumber      = Convert.ToByte(chip.Site);
				ptr.TestFlags       = chip.PassOrFail == "PASS" ? (byte)1 : (byte)0;
				ptr.ParametricFlags = 0;
				float? result = float.Parse(Regex.Match(chip.strMaxMeasureValue, @"[-+]?\d+\.?\d*").Value);

				if(result < 0)
				{
				}
				ptr.Result                   = result;
				ptr.TestText                 = chip.Comment;
				ptr.AlarmId                  = " ";
				ptr.OptionalFlags            = 0;
				ptr.ResultScalingExponent    = 6;
				ptr.LowLimitScalingExponent  = 6;
				ptr.HighLimitScalingExponent = 6;
				string LO_LIMIT = chip.LowLimit;
				string HI_LIMIT = chip.HighLimit;

				if(LO_LIMIT != null)
				{
					float? f = float.Parse(Regex.Match(LO_LIMIT, @"[-+]?\d+\.?\d*").Value);
					ptr.LowLimit = f;
				}
				else
				{
					ptr.LowLimit = 0;
				}

				if(HI_LIMIT != null)
				{
					float? h = float.Parse(Regex.Match(HI_LIMIT, @"[-+]?\d+\.?\d*").Value);
					ptr.HighLimit = h;
				}
				else
				{
					ptr.HighLimit = 0;
				}
				string UnitResult = "0";

				if(chip.LowLimit != null)
				{
					UnitResult = Regex.Replace(chip.LowLimit, "[^a-zA-Z]", "");
				}

				if(UnitResult == "0")
				{
					UnitResult = Regex.Replace(chip.HighLimit, "[^a-zA-Z]", "");
				}
				ptr.Units = UnitResult;
				writer.WriteRecord(ptr);
				Prr prr = new Prr();
				prr.HeadNumber = 1;
				prr.SiteNumber = Convert.ToByte(chip.Site);
				writer.WriteRecord(prr);
			}

#endregion

#region NO TSR

			Tsr tsr = new Tsr();
			tsr.HeadNumber       = 1;
			tsr.SiteNumber       = (byte?)_FileParam.SiteCount;
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
			writer.WriteRecord(tsr);

#endregion

#region NO HBR

			Hbr hbr = new Hbr();
			hbr.HeadNumber  = 1;
			hbr.SiteNumber  = (byte?)_FileParam.SiteCount;
			hbr.BinNumber   = 2;
			hbr.BinCount    = 2;
			hbr.BinPassFail = "P";
			hbr.BinName     = 2.ToString();
			writer.WriteRecord(hbr);

#endregion

#region NO SBR

			Sbr sbr = new Sbr();
			sbr.HeadNumber  = 1;
			sbr.SiteNumber  = (byte?)_FileParam.SiteCount;
			sbr.BinNumber   = 2;
			sbr.BinCount    = 2;
			sbr.BinPassFail = "P";
			sbr.BinName     = 2.ToString();
			writer.WriteRecord(sbr);

#endregion

#region PCR

			Pcr pcr = new Pcr();
			pcr.HeadNumber = 1;
			pcr.SiteNumber = (byte?)_FileParam.SiteCount;
			writer.WriteRecord(pcr);

#endregion

#region MRR

			Mrr mrr = new Mrr();
			mrr.FinishTime      = DateTime.Parse(_FileParam.LotEND);
			mrr.DispositionCode = " ";
			mrr.UserDescription = " ";
			mrr.ExecDescription = " ";
			writer.WriteRecord(mrr);

#endregion

			writer.Dispose();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			// _P2020 = CP2020.CreateInstance(@"C:\Users\USER1\Documents\Pti_Doc\Project\Tester STDF\P2020 8 Site\P2020_8site_datalog.txt", 0);
			// _P2020.AnalyzeFile();

			//_P2020 = CP2020.CreateInstance(Directory.GetFiles(@"C:\Users\USER1\Documents\Pti_Doc\Project\Jerry\P2020\P2020_Data Log\Data Log\","*.txt"), 0);
			//_P2020.AnalyzeFile();
			Stopwatch s = new Stopwatch();
			s.Start();
			_P2020 = CP2020.CreateInstance(Directory.GetFiles(@"C:\STDFATDF\2023-09-18-02-59-16\New", "*.txt"), 0);
			_P2020.AnalyzeFile();
			s.Stop();
			Console.WriteLine(s.ElapsedMilliseconds);
			//49s
			//32s
			_FileParam = new CFileParam(@"C:\Users\USER1\Documents\Pti_Doc\Project\Tester STDF\P2020 8 Site\2023-09-06-14-06-02.txt");
			_FileParam.AnnalyzeFile();

			//C:\Users\USER1\Documents\Pti_Doc\Project\Jerry\P2020\P2020_Data Log\Data Log
		}
	}
}