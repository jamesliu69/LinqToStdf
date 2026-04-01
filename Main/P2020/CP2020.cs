#region

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

#endregion

namespace STDF
{
	public class CP2020 : IAnalyze
	{
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

		public event EventHandler? evtSelectItem;

		public event EventHandler? evtErrorArise;

		public string SavePath { get; set; }

		public int AllPin { get; set; }

		public int PassPin { get; set; }

		public int FailPin { get; set; }

		public async Task<string[]> ReadAllLinesAsync(string path)
		{
			using(StreamReader reader = new StreamReader(path))
			{
				string text = await reader.ReadToEndAsync();
				return text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
			}
		}

		public void AnalyzeFile()
		{
			try
			{
				lstChip.Clear();
				lstChipData = new List<CChipData>();

				foreach(string file in FileName)
				{
					//ReadFileAsync
					string[] files = File.ReadAllLines(file); //以指定的編碼方式讀取檔案

					//string[] files = await ReadAllLinesAsync(file);
					files = files.Where(file => !string.IsNullOrEmpty(file)).ToArray();
					string[] boundedLines = files.SkipWhile(line => !line.Trim().StartsWith("==> Test Start")).Skip(1).TakeWhile(line => !line.Trim().StartsWith("==> Test End")).Where(line => /*!line.Contains("JUDGE_V:") &&*/ !line.Contains("P/F   Site              Pin_name        Force      L-Limit      H-Limit      Measure   Min Measure   Max Measure")).ToArray();
					int      idx          = 0;
					string   Title        = string.Empty;

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
						IEnumerable<string> Enumerables = word.Split(spilts, StringSplitOptions.None).Where(c => c != "");
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
				ChipData.FileName      = Path.GetFileNameWithoutExtension(name);
				ChipData.Comment       = Title.Trim();

				// 确保数组至少有9个元素
				if(StrArray.Length < 9)
				{
					throw new InvalidOperationException($"数据格式错误: 列数不足 (实际: {StrArray.Length}, 需要: 9)");
				}

				ChipData.PassOrFail    = StrArray[0].Trim();
				ChipData.Site          = StrArray[1].Trim();
				ChipData.PinName       = StrArray[2].Trim();
				ChipData.strForceValue = StrArray[3].Trim();
				
				// 直接读取 L-Limit 和 H-Limit，处理空白值
				string lowLimitRaw  = StrArray[4].Trim();
				string highLimitRaw = StrArray[5].Trim();
				
				ChipData.LowLimit  = string.IsNullOrWhiteSpace(lowLimitRaw) ? null : lowLimitRaw;
				ChipData.HighLimit = string.IsNullOrWhiteSpace(highLimitRaw) ? null : highLimitRaw;
				
				ChipData.strMeasureValue    = StrArray[6].Trim();
				ChipData.strMinMeasureValue = StrArray[7].Trim();
				ChipData.strMaxMeasureValue = StrArray[8].Trim();

				return ChipData;
			}
			catch(Exception e)
			{
				MessageBox.Show($"EnumerableConvert 错误:\n文件: {name}\n标题: {Title}\n数据: {string.Join(" | ", StrArray)}\n\n{e.Message}\n{e.StackTrace}");
			}
			return null;
		}

		public void Dispose()
		{
		}

		public static CP2020 CreateInstance(string   filename, int calOffset) => new CP2020(filename);
		public static CP2020 CreateInstance(string[] filename, int calOffset) => new CP2020(filename);
	}
}
