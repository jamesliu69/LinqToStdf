#region

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

#endregion

namespace CSTDF
{
	public class CP2020 : IAnalyze
	{
		private          IAnalyze        _AnalyzeImplementation;
		private          int             CalOffset;
		private          DataTable?      dt;
		private readonly List<string>    FileName    = new List<string>();
		public           List<CChipData> lstChipData = new List<CChipData>();

		public CP2020(string filename)
		{
			FileName.Clear();
			FileName.Add(filename);
		}
		
		public CP2020(string[] filename)
		{
			FileName.Clear();
			FileName.AddRange(filename);
		}

		private List<CChipData> lstChip { get; } = new List<CChipData>();

		public int DictCount { get; set; }

		public DataTable GetTable() => null;

		public void AutoShowItem()
		{
		}

		public event EventHandler? evtSelectItem { add => _AnalyzeImplementation.evtSelectItem += value; remove => _AnalyzeImplementation.evtSelectItem -= value; }

		public event EventHandler? evtErrorArise;

		public string SavePath { get; set; }

		public int AllPin { get; set; }

		public int PassPin { get; set; }

		public int FailPin { get; set; }
		public async Task<string[]> ReadAllLinesAsync(string path)
		{
			using (var reader = new StreamReader(path))
			{
				var text = await reader.ReadToEndAsync();
				return text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
			}
		}

		public void AnalyzeFile()
		{
			try
			{
				lstChip.Clear();
				lstChipData = new List<CChipData>();
				foreach (string file in FileName)
				{
					//ReadFileAsync
					string[] files = File.ReadAllLines(file); //以指定的編碼方式讀取檔案
					//string[] files = await ReadAllLinesAsync(file);
					files = files.Where(file => !string.IsNullOrEmpty(file)).ToArray();
					string[] boundedLines = files.SkipWhile(line => !line.Trim().StartsWith("==> Test Start")).Skip(1).TakeWhile(line => !line.Trim().StartsWith("==> Test End")).Where(line => /*!line.Contains("JUDGE_V:") &&*/ !line.Contains("P/F   Site              Pin_name        Force      L-Limit      H-Limit      Measure   Min Measure   Max Measure")).ToArray();
					int      idx          = 0;
					
					string Title = string.Empty;

					foreach(string word in boundedLines)
					{
						if(word.Contains("<<<<<<---------------     Test Item :"))
						{
							Title = word.Replace("<<<<<<---------------     Test Item : OSitem_", "").Replace("--------------->>>>>>", "").Trim();
							continue;
						}

						if(word.Contains("JUDGE_V:"))
						{
							idx++;
							continue;
						}
						string[]            spilts      = { "  " };
						IEnumerable<string> Enumerables = word.Split(spilts, StringSplitOptions.RemoveEmptyEntries).Where(c => c != "");
						CChipData           convert     = EnumerableConvert(file, Title, Enumerables.ToArray());

						if(convert.PassOrFail.ToUpper() == "PASS")
						{
							convert.Id = idx;
						}
						lstChipData.Add(convert);
					}

					//listChip[file] = lstChipData;
				}
				DictCount = lstChip.Count;
			}
			catch(Exception e)
			{
				Console.WriteLine(e);
				MessageBox.Show(e.Message + "\r\n" + e.StackTrace);
			}
		}

		public void GroupBySite()
		{
			
		}

		public void CalMeasure()
		{
			//85/
		}

		public void OutputFile()
		{
		}

		private CChipData EnumerableConvert(string name, string Title, string[] StrArray)
		{
			try
			{
				CChipData ChipData = new CChipData();

				if(StrArray.Length < 9)
				{
					ChipData.FileName           = Path.GetFileNameWithoutExtension(name);
					ChipData.PassOrFail         = StrArray[0].Trim();
					ChipData.Site               = StrArray[1].Trim();
					ChipData.PinName            = StrArray[2].Trim();
					ChipData.strForceValue      = StrArray[3].Trim();

					if(StrArray[4].StartsWith("-"))
					{
						ChipData.LowLimit  = StrArray[4].Trim();
						ChipData.HighLimit = StrArray[4].Remove(0,1).Trim();
					}
					else
					{
						ChipData.LowLimit  = ("-"+StrArray[4]).Trim();
						ChipData.HighLimit = StrArray[4].Trim();	
					}
					ChipData.strMeasureValue    = StrArray[5].Trim();
					ChipData.strMinMeasureValue = StrArray[6].Trim();
					ChipData.strMaxMeasureValue = StrArray[7].Trim();
					ChipData.Comment            = Title.Trim();
					return ChipData;
				}
				else
				{
					ChipData.FileName           = Path.GetFileNameWithoutExtension(name);
					ChipData.PassOrFail         = StrArray[0].Trim();
					ChipData.Site               = StrArray[1].Trim();
					ChipData.PinName            = StrArray[2].Trim();
					ChipData.strForceValue      = StrArray[3].Trim();
					ChipData.LowLimit           = StrArray[4].Trim();
					ChipData.HighLimit          = StrArray[5].Trim();
					ChipData.strMeasureValue    = StrArray[6].Trim();
					ChipData.strMinMeasureValue = StrArray[7].Trim();
					ChipData.strMaxMeasureValue = StrArray[8].Trim();
					ChipData.Comment            = Title.Trim();
					return ChipData;
				}
			}
			catch(Exception e)
			{
				MessageBox.Show(e.Message + "\r\n" + e.StackTrace);
			}
			return null;
		}

		public void Dispose()
		{
		}

		public static CP2020 CreateInstance(string filename, int calOffset) => new CP2020(filename);
		public static CP2020 CreateInstance(string[] filename, int calOffset) => new CP2020(filename);
	}
}