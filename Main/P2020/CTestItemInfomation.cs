namespace P2020
{
	public class CTestItemInfomation
	{
		public int    Id    { get; set; }
		public int    Force { get; set; }
		public float  Value { get; set; } = float.NaN;
		public string PF    { get; set; }

		public float LLim { get; set; } = float.NaN;
		public float ULim { get; set; } = float.NaN;

		public string LLimUnit  { get; set; }
		public string ULimUnit  { get; set; }
		public string forceUnit { get; set; }
		public string ItemName  { get; set; } //使用SK 當作名稱 進行小數點判斷
		public string Comment   { get; set; }
		public string PinName   { get; set; }
		public string PinNumber { get; set; }
		public string Test      { get; set; }

		//public string  Judge      { get; set; }
		public string Site { get; set; }

		//public string  Type       { get; set; }
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