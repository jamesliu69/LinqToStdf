using System.Collections.Generic;

namespace P2020
{
	public class CFunctionDecimalPoint
	{
		public static List<CFunctionDecimalPoint> lst = new List<CFunctionDecimalPoint>();

		public CFunctionDecimalPoint(string itemName, bool showItem, int calDigitalNum, int calSigmaNum, float numericalRange, float numOffsetRange)
		{
			ItemName       = itemName;
			ShowItem       = showItem;
			CalDigitalNum  = calDigitalNum;
			CalSigmaNum    = calSigmaNum;
			NumericalRange = numericalRange;
			NumOffset      = numOffsetRange;
		}

		/// <summary>
		///     是否顯示名稱
		/// </summary>
		public bool ShowItem { get; set; }

		/// <summary>
		///     名稱
		/// </summary>
		public string ItemName { get; set; }
		/// <summary>
		///     小數點
		/// </summary>
		public decimal CalDigitalNum { get; set; }

		/// <summary>
		///     Sigma 倍率
		/// </summary>
		public int CalSigmaNum { get; set; }

		/// <summary>
		///     偏移值 Offset Value
		/// </summary>
		public float NumOffset { get; set; }
		/// <summary>
		///     級距
		/// </summary>
		public float NumericalRange { get; set; }

		public string PosFormat
		{
			get => CalDigitalNum switch
				   {
					   0 => "0",
					   1 => "0.0",
					   2 => "0.00",
					   3 => "0.000",
					   _ => "0",
				   };
		}

		public CFunctionDecimalPoint this[int index] { get => lst[index]; }
	}
}