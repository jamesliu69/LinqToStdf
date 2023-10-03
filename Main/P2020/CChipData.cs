namespace CSTDF
{
	public class CChipData
	{
		public string FileName   { get; set; }
		public int    Id         { get; set; }
		public double ForceValue { get; set; }

		public string strForceValue  { get; set; } //For P2020 量測值含單位
		public string ForceValueUnit { get; set; }
		public double MeasureValue   { get; set; }

		public string strMeasureValue    { get; set; } //For P2020 量測值含單位
		public string strMinMeasureValue { get; set; } //For P2020 量測值含單位
		public string strMaxMeasureValue { get; set; } //For P2020 量測值含單位
		public string MeasureUnit        { get; set; }
		public string PassOrFail         { get; set; }

		public string LowLimit   { get; set; }
		public string HighLimit  { get; set; }
		public string AveMeasure { get; set; }

		public string LowLimitUnit   { get; set; }
		public string HighLimitUnit  { get; set; }
		public string AveMeasureUnit { get; set; }
		public string Comment        { get; set; }
		public string PinName        { get; set; }
		public string PinNumber      { get; set; }
		public string TestNo         { get; set; }

		public string  TestType   { get; set; }
		public decimal DigitalNum { get; set; }

		public string Site { get; set; }

		public string Chain { get; set; }
	}
}