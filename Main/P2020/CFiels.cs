using System;

namespace WinFormsApp1
{
	public class CFiels
	{
		public uint   ABRT_CNT = uint.MaxValue;
		public string ALARM_ID = "";
		public ushort BURN_TIM = 65535;
		public string C_HLMFMT = "";
		public string C_LLMFMT = "";
		public string C_RESFMT = "";
		public char   CMOD_COD = ' ';
		public string EXEC_TYP = "";
		public string EXEC_VER = "";
		public uint   FUNC_CNT = uint.MaxValue;
		public uint   GOOD_CNT = uint.MaxValue;

		//PCR

		public byte   HEAD_NUM = byte.MaxValue;
		public float? HI_LIMIT;
		public float? HI_SPEC;
		public sbyte? HLM_SCAL;
		public string JOB_NAM = "";
		public string JOB_REV = "";
		public sbyte? LLM_SCAL;
		public float? LO_LIMIT;
		public float? LO_SPEC;
		public string LOT_ID   = "";
		public char   MODE_COD = ' ';
		public string NODE_NAM = "";
		public string OPER_NAM = "";
		public byte   OPT_FLAG;
		public byte   PARM_FLG;
		public uint   PART_CNT;
		public string PART_TYP = "";

		//PIR
		public byte   PIR_HEAD_NUM;
		public byte   PIR_SITE_NUM;
		public char   PROT_COD     = ' ';
		public byte   PTR_HEAD_NUM = 1;
		public byte   PTR_SITE_NUM = 1;
		public sbyte? RES_SCAL;
		public float? RESULT;
		public uint   RTST_CNT = uint.MaxValue;
		public char   RTST_COD = ' ';
		public string SBLOT_ID = "";

		//MIR
		public DateTime SETUP_T;
		public byte     SITE_NUM = 1;
		public DateTime START_T;
		public byte     STAT_NUM = 0;
		public string   TEST_COD = "";
		public byte     TEST_FLG;

		//PTR

		public uint   TEST_NUM = 0;
		public string TEST_TXT = "";
		public string TSTR_TYP = "";
		public string UNITS    = "";

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