using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace P2020
{
	public class CFunction
	{
		public static void CalcGroupId(ref string savePath)
		{
		}

		private static string[] CalLowCriteria(string[] splits, int MeanSub6S, int Criteria_L)
		{
			double low_diff = Convert.ToDouble(splits[Criteria_L]) - Convert.ToDouble(splits[MeanSub6S]);

			if(((splits[0].ToUpper().Contains("PWR") || splits[0].ToUpper().Contains("POWER") || splits[0].ToUpper().Contains("GND") || splits[0].ToUpper().Contains("GROUD")) && splits[0].ToUpper().Contains("SHORT")) || IsFloat(low_diff.ToString()))
			{
				//LowCriteria 大於0處理
				if((low_diff <= 0) && (low_diff >= -0.05))
				{
					splits[Criteria_L] = (Convert.ToDouble(splits[Criteria_L]) - 0.5).ToString("0.00");
				}

				//LowCriteria 小於0處理
				if((low_diff >= 0) && (low_diff <= 0.05))
				{
					splits[Criteria_L] = (Convert.ToDouble(splits[Criteria_L]) + 0.5).ToString("0.00");
				}
			}
			else
			{
				if((Convert.ToDouble(splits[MeanSub6S]) >= -50) && (Convert.ToDouble(splits[MeanSub6S]) <= 50))
				{
					splits[Criteria_L] = "-1200";
				}
				else if((Convert.ToDouble(splits[MeanSub6S]) <= -900) || (Convert.ToDouble(splits[MeanSub6S]) >= 900))
				{
					if(Convert.ToDouble(splits[MeanSub6S]) < -1200)
					{
						splits[Criteria_L] = "-1200";
					}

					if(Convert.ToDouble(splits[MeanSub6S]) > 1200)
					{
						splits[Criteria_L] = "1200";
					}
				}

				//LowCriteria 大於0處理
				if((low_diff <= 0) && (low_diff >= -10))
				{
					splits[Criteria_L] = (Convert.ToInt32(splits[Criteria_L]) - 50).ToString();

					if(Convert.ToInt32(splits[Criteria_L]) > 0)
					{
						splits[Criteria_L] = Math.Min(900, Convert.ToInt32(splits[Criteria_L])).ToString();
					}
					else
					{
						splits[Criteria_L] = Math.Max(-900, Convert.ToInt32(splits[Criteria_L])).ToString();
					}
				}

				//LowCriteria 小於0處理
				if((low_diff >= 0) && (low_diff <= 10))
				{
					splits[Criteria_L] = (Convert.ToInt32(splits[Criteria_L]) + 50).ToString();

					if(Convert.ToInt32(splits[Criteria_L]) > 0)
					{
						splits[Criteria_L] = Math.Min(900, Convert.ToInt32(splits[Criteria_L])).ToString();
					}
					else
					{
						splits[Criteria_L] = Math.Max(-900, Convert.ToInt32(splits[Criteria_L])).ToString();
					}
				}
			}
			return splits;
		}

		private static bool IsFloat(string str)
		{
			const string regextext = @"\d+\.\d+$";
			Regex        Regex     = new Regex(regextext, RegexOptions.None);
			return Regex.IsMatch(str);
		}

		private static string[] CalHighCriteria(string[] splits, int MeanPlus6S, int CriteriaH)
		{
			double HighDiff = Convert.ToDouble(splits[CriteriaH]) - Convert.ToDouble(splits[MeanPlus6S]);

			if(((splits[0].ToUpper().Contains("PWR") || splits[0].ToUpper().Contains("POWER") || splits[0].ToUpper().Contains("GND") || splits[0].ToUpper().Contains("GROUD")) && splits[0].ToUpper().Contains("SHORT")) || IsFloat(HighDiff.ToString()))
			{
				//LowCriteria 大於0處理
				if(HighDiff is <= 0 and >= -0.05)
				{
					splits[CriteriaH] = (Convert.ToDouble(splits[CriteriaH]) - 0.5).ToString("0.00");
				}

				//LowCriteria 小於0處理
				if(HighDiff is >= 0 and <= 0.05)
				{
					splits[CriteriaH] = (Convert.ToDouble(splits[CriteriaH]) + 0.5).ToString("0.00");
				}
			}
			else
			{
				switch(Convert.ToDouble(splits[MeanPlus6S]))
				{
					case >= -50 and <= 50:
						splits[CriteriaH] = "1200";
						break;
					case <= -900:
					case >= 900:
					{
						if(Convert.ToDouble(splits[MeanPlus6S]) < -1200)
						{
							splits[CriteriaH] = "-1200";
						}

						if(Convert.ToDouble(splits[MeanPlus6S]) > 1200)
						{
							splits[CriteriaH] = "1200";
						}
						break;
					}
				}

				//High Criteria 大於0處理
				if(HighDiff is >= 0 and <= 10)
				{
					splits[CriteriaH] = (Convert.ToInt32(splits[CriteriaH]) + 50).ToString();

					if(Convert.ToInt32(splits[CriteriaH]) > 0)
					{
						splits[CriteriaH] = Math.Min(1200, Convert.ToInt32(splits[CriteriaH])).ToString();
					}
					else
					{
						if(splits[CriteriaH].All(char.IsDigit))
						{
							splits[CriteriaH] = Math.Max(-1200, Convert.ToInt32(splits[CriteriaH])).ToString();
						}
						else
						{
							splits[CriteriaH] = "0";
						}
					}
				}

				//High Criteria 小於0處理
				else
				{
					if((HighDiff != 0) && (HighDiff < 0.1) && (HighDiff > 0.1))
					{
						splits[CriteriaH] = (Convert.ToInt32(splits[CriteriaH]) + 0.5).ToString(CultureInfo.InvariantCulture);
					}

					if(Convert.ToDouble(splits[CriteriaH]) > 0)
					{
						splits[CriteriaH] = Math.Min(900, Convert.ToInt32(splits[CriteriaH])).ToString();
					}
					else
					{
						if(splits[CriteriaH].All(char.IsDigit))
						{
							splits[CriteriaH] = Math.Max(-900, Convert.ToInt32(splits[CriteriaH])).ToString();
						}
						else
						{
							splits[CriteriaH] = "0";
						}
					}
				}
			}
			return splits;
		}
	}
}