using Stdf;
using Stdf.Records.V4;
using System;
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
			mir.SetupTime            = DateTime.Parse("2001-06-05T09:18:06Z");
			mir.StartTime            = DateTime.Parse("2001-06-05T09:18:06Z");
			mir.StationNumber        = 0;
			mir.ModeCode             = "P";
			mir.RetestCode           = "N";
			mir.ProtectionCode       = "0";
			mir.BurnInTime           = 65535;
			mir.CommandModeCode      = "a";
			mir.LotId                = "GAL-LOT";
			mir.PartType             = "GOLD8BAR";
			mir.NodeName             = "galaxy-t";
			mir.TesterType           = "A530";
			mir.JobName              = "mobile-05";
			mir.JobRevision          = "16";
			mir.SublotId             = "03";
			mir.OperatorName         = "ews";
			mir.ExecType             = "IMAGE V6.3.y2k D8 052200";
			mir.ExecVersion          = " ";
			mir.TestCode             = "E38";
			mir.TestTemperature      = " ";
			mir.UserText             = " ";
			mir.AuxiliaryFile        = " ";
			mir.PackageType          = " ";
			mir.FamilyId             = " ";
			mir.DateCode             = " ";
			mir.FacilityId           = " ";
			mir.FloorId              = " ";
			mir.ProcessId            = " ";
			mir.OperationFrequency   = " ";
			mir.SpecificationName    = " ";
			mir.SpecificationVersion = " ";
			mir.FlowId               = " ";
			mir.SetupId              = " ";
			mir.DesignRevision       = " ";
			mir.EngineeringId        = " ";
			mir.RomCode              = " ";
			mir.SerialNumber         = " ";
			mir.SupervisorName       = " ";
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
			pmr.ChannelType  = 4;
			pmr.ChannelName  = "D9";
			pmr.PhysicalName = "4";
			pmr.LogicalName  = "U_D9";
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
				pir.SiteNumber = Convert.ToByte(0);
				writer.WriteRecord(pir);
				Ptr ptr = new Ptr();
				ptr.TestNumber      = 1;
				ptr.HeadNumber      = 1;
				ptr.SiteNumber      = Convert.ToByte(0);
				ptr.TestFlags       = chip.PassOrFail == "PASS" ? (byte)1 : (byte)0;
				ptr.ParametricFlags = 0;
				float? result = float.Parse(Regex.Match(chip.strMaxMeasureValue, @"\d+\.?\d*").Value);
				ptr.Result                   = result;
				ptr.TestText                 = chip.Comment + i;
				ptr.AlarmId                  = " ";
				ptr.OptionalFlags            = 0;
				ptr.ResultScalingExponent    = 6;
				ptr.LowLimitScalingExponent  = 6;
				ptr.HighLimitScalingExponent = 6;
				string LO_LIMIT = chip.LowLimit;
				string HI_LIMIT = chip.HighLimit;

				if(LO_LIMIT != null)
				{
					float? f = float.Parse(Regex.Match(LO_LIMIT, @"\d+\.?\d*").Value);
					ptr.LowLimit = f;
				}
				else
				{
					ptr.LowLimit = 0;
				}

				if(HI_LIMIT != null)
				{
					float? h = float.Parse(Regex.Match(HI_LIMIT, @"\d+\.?\d*").Value);
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
				prr.SiteNumber = 0;
				writer.WriteRecord(prr);
			}

			// PRR prr = new PRR();
			// prr.HEAD_NUM = 1;
			// prr.SITE_NUM = 8;
			// StdfRecords.Add(prr);

#endregion

#region PRR

			// PRR prr = new PRR();
			// prr.HEAD_NUM = 1;
			// prr.SITE_NUM = 8;
			// StdfRecords.Add(prr);

			// prr          = new PRR();
			// prr.HEAD_NUM = 1;
			// prr.SITE_NUM = 0;
			// prr.PART_FLG = 8;
			// prr.NUM_TEST = 1;
			// prr.HARD_BIN = 5;
			// prr.SOFT_BIN = 5;
			// prr.X_COORD  = 19;
			// prr.Y_COORD  = -3;
			// prr.TEST_T   = 0;
			// prr.PART_ID  = "1";
			// prr.PART_TXT = "";
			// prr.PART_FIX = null;
			// StdfRecords.Add(prr);

#endregion

#region NO TSR

			Tsr tsr = new Tsr();
			tsr.HeadNumber       = 1;
			tsr.SiteNumber       = 0;
			tsr.TestType         = "P";
			tsr.TestNumber       = 1000;
			tsr.ExecutedCount    = 0;
			tsr.FailedCount      = 0;
			tsr.AlarmCount       = 0;
			tsr.TestName         = "Sink out I";
			tsr.SequencerName    = "seqU751";
			tsr.TestLabel        = " ";
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
			hbr.SiteNumber  = 0;
			hbr.BinNumber   = 2;
			hbr.BinCount    = 2;
			hbr.BinPassFail = "P";
			hbr.BinName     = 2.ToString();
			writer.WriteRecord(hbr);

#endregion

#region NO SBR

			Sbr sbr = new Sbr();
			sbr.HeadNumber  = 1;
			sbr.SiteNumber  = 0;
			sbr.BinNumber   = 2;
			sbr.BinCount    = 2;
			sbr.BinPassFail = "P";
			sbr.BinName     = 2.ToString();
			writer.WriteRecord(sbr);

#endregion

#region PCR

			Pcr pcr = new Pcr();
			pcr.HeadNumber = 1;
			pcr.SiteNumber = 8;
			writer.WriteRecord(pcr);

#endregion

#region MRR

			Mrr mrr = new Mrr();
			mrr.FinishTime      = DateTime.Parse("2001-06-06T02:48:08Z");
			mrr.DispositionCode = " ";
			mrr.UserDescription = " ";
			mrr.ExecDescription = " ";
			writer.WriteRecord(mrr);

#endregion

			writer.Dispose();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			_P2020 = CP2020.CreateInstance(@"C:\Users\USER1\Documents\Pti_Doc\Project\Tester STDF\STDF 8Site\P2020_8site_datalog.txt", 0);
			_P2020.AnalyzeFile();
			_FileParam = new CFileParam(@"C:\Users\USER1\Documents\Pti_Doc\Project\Tester STDF\STDF 8Site\2023-09-06-14-06-02.txt");
			_FileParam.AnnalyzeFile();
		}
	}
}