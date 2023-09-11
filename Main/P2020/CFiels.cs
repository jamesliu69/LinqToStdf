using System;

namespace WinFormsApp1
{
	public class CFiels
	{
		//MIR
		public DateTime SETUP_T;
		public DateTime START_T;
		public byte     STAT_NUM = 0;
		public char     MODE_COD = ' ';
		public char     RTST_COD = ' ';
		public char     PROT_COD = ' ';
		public ushort   BURN_TIM = 65535;
		public char     CMOD_COD = ' ';
		public string   LOT_ID   = "";
		public string   PART_TYP = "";
		public string   NODE_NAM = "";
		public string   TSTR_TYP = "";
		public string   JOB_NAM  = "";
		public string   JOB_REV  = "";
		public string   SBLOT_ID = "";
		public string   OPER_NAM = "";
		public string   EXEC_TYP = "";
		public string   EXEC_VER = "";
		public string   TEST_COD = "";

		//PIR
		public byte PIR_HEAD_NUM;
		public byte PIR_SITE_NUM;

		//PTR

		public uint   TEST_NUM     = 0;
		public byte   PTR_HEAD_NUM = 1;
		public byte   PTR_SITE_NUM = 1;
		public byte   TEST_FLG;
		public byte   PARM_FLG;
		public float? RESULT;
		public string TEST_TXT = "";
		public string ALARM_ID = "";
		public byte   OPT_FLAG;
		public sbyte? RES_SCAL;
		public sbyte? LLM_SCAL;
		public sbyte? HLM_SCAL;
		public float? LO_LIMIT;
		public float? HI_LIMIT;
		public string UNITS    = "";
		public string C_RESFMT = "";
		public string C_LLMFMT = "";
		public string C_HLMFMT = "";
		public float? LO_SPEC;
		public float? HI_SPEC;

		//PCR

		public byte HEAD_NUM = byte.MaxValue;
		public byte SITE_NUM = 1;
		public uint PART_CNT;
		public uint RTST_CNT = uint.MaxValue;
		public uint ABRT_CNT = uint.MaxValue;
		public uint GOOD_CNT = uint.MaxValue;
		public uint FUNC_CNT = uint.MaxValue;

		public int    Id       { get; set; }
		public int    CalSigma { get; set; }
		public int    Force    { get; set; }
		public float  Value    { get; set; }
		public string PF       { get; set; }

		public float LLim { get; set; }
		public float ULim { get; set; }

		public string SK        { get; set; } //使用SK 當作名稱 進行小數點判斷
		public string Comment   { get; set; }
		public string PinName   { get; set; }
		public string PinNumber { get; set; }
		public string Test      { get; set; }

		public string  Judge      { get; set; }
		public string  Site       { get; set; }
		public string  Type       { get; set; }
		public string  Key        { get; set; }
		public string  SortKey    { get; set; }
		public decimal DigitalNum { get; set; }

		public float OffsetValue { get; set; }

		public string PosFormat
		{
			get
			{
				switch(DigitalNum)
				{
					case 0:
						return "0";
					case 1:
						return "0.0";
					case 2:
						return "0.00";
					case 3:
						return "0.000";
				}
				return "0";
			}
		}
	}
}