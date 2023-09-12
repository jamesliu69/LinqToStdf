using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace P2020
{
	public class CFileParam
	{
		public const string split = "********************************************************************";
		public       string ATEID;
		public       string Customer;
		public       string DeviceID;
		public       string FileName;

		public string FilePath;
		public string HandlerID;

		/// <summary>
		///     [HARDWARE BIN]
		/// </summary>
		public Dictionary<string, IEnumerable<string>> HardWareBin = new Dictionary<string, IEnumerable<string>>();
		public string       LoadBoardName;
		public string       LotEND;
		public string       LotNumber;
		public string       LotSTART;
		public string       Operator;
		public List<string> ResultFail = new List<string>();
		public List<string> ResultPass = new List<string>();
		public string[]     ResultTotal;
		public string       SampleRate;

		/// <summary>
		///     [Result]
		/// </summary>
		public int SiteCount = 1;

		/// <summary>
		///     [SOFTWARE BIN]
		/// </summary>
		public Dictionary<string, IEnumerable<string>> SoftWareBin = new Dictionary<string, IEnumerable<string>>();
		public string TestCycle;

		/// <summary>
		///     [TEST ITEM]
		/// </summary>
		public string TestItemName = "O/S_Test";
		public string TestProgramName;

		public CFileParam(string filename) => FileName = filename;

		public void AnnalyzeFile()
		{
			StringBuilder sb    = new StringBuilder();
			string[]      files = File.ReadAllLines(FileName);

			foreach(string title in files)
			{
				string[] str = title.Split(':');

				switch(str[0].Trim())
				{
					case "File Path":
						FilePath = str[1].Trim().Replace("----------", "");
						break;
					case "LoadBoard Name":
						LoadBoardName = str[1].Trim().Replace("----------", "");
						break;
					case "Lot Number":
						LotNumber = str[1].Trim().Replace("----------", "");
						break;
					case "Device ID":
						DeviceID = str[1].Trim().Replace("----------", "");
						break;
					case "Operator":
						Operator = str[1].Trim().Replace("----------", "");
						break;
					case "Customer":
						Customer = str[1].Trim().Replace("----------", "");
						break;
					case "Test Program Name":
						TestProgramName = str[1].Trim().Replace("----------", "");
						break;
					case "Sample Rate":
						SampleRate = str[1].Trim().Replace("----------", "");
						break;
					case "Test Cycle":
						TestCycle = str[1].Trim().Replace("----------", "");
						break;
					case "ATE ID":
						ATEID = str[1].Trim().Replace("-", "");
						break;
					case "Handler ID":
						HandlerID = str[1].Trim().Replace("----------", "");
						break;
					case "Lot START":
						LotSTART = str[1].Trim().Replace("----------", "");
						break;
					case "Lot END":
						LotEND = str[1].Trim().Replace("----------", "");
						break;
				}

				if(str[0].Contains("Total (By Sites)"))
				{
					IEnumerable<string> v1 = str[0].Trim().Split(' ').Where(c => c != "");
					IEnumerable<string> v2 = v1.Skip(3);
					SiteCount = Convert.ToInt32(v2.ElementAt(0).Substring(0, v2.ElementAt(0).IndexOf("(")));
				}
				else if(str[0].Contains("Pass  (By Sites)"))
				{
					IEnumerable<string> v1 = str[0].Trim().Split(' ').Where(c => c != "");
					IEnumerable<string> v2 = v1.Skip(4);

					foreach(string VARIABLE in v2)
					{
						ResultPass.Add(VARIABLE.Substring(0, v2.ElementAt(0).IndexOf("(")));
					}
				}
				else if(str[0].Contains("Fail  (By Sites)"))
				{
					IEnumerable<string> v1 = str[0].Trim().Split(' ').Where(c => c != "");
					IEnumerable<string> v2 = v1.Skip(4);

					foreach(string VARIABLE in v2)
					{
						ResultFail.Add(VARIABLE.Substring(0, v2.ElementAt(0).IndexOf("(")));
					}
				}
				HardWareBin.Clear();

				//[HARDWARE BIN]
				if(str[0].Contains("[HARDWARE BIN]"))
				{
					bool IsHardWareBinTitle = files.Contains("[HARDWARE BIN]");
					int  IdxString1         = files.ToList().IndexOf("[HARDWARE BIN]");

					var AssignItemAndIdx = files.Select((item, index) => new {
													Item  = item,
													Index = index,
												})
												.FirstOrDefault(x => x.Item.StartsWith("**************"));
					List<string> GetRangeString = files.ToList().GetRange(IdxString1 + 2, AssignItemAndIdx.Index);

					foreach(string s in GetRangeString)
					{
						IEnumerable<string> spilts = s.Split(' ').Where(c => c != "");

						if(!string.IsNullOrEmpty(spilts.ElementAt(0)))
						{
							HardWareBin[spilts.ElementAt(0)] = s.Trim().Split(' ').Where(c => c != "").Skip(3).ToList();
						}
					}
				}

				if(str[0].Contains("[SOFTWARE BIN]"))
				{
					bool IsSoftWareBinTitle = files.Contains("[SOFTWARE BIN]");
					int  IdxString1         = files.ToList().IndexOf("[SOFTWARE BIN]");

					var AssignItemAndIdx = files.Select((item, index) => new {
													Item  = item,
													Index = index,
												})
												.FirstOrDefault(x => x.Item.StartsWith("**************"));
					List<string> GetRangeString = files.ToList().GetRange(IdxString1 + 2, AssignItemAndIdx.Index);

					foreach(string s in GetRangeString)
					{
						IEnumerable<string> spilts = s.Split(' ').Where(c => c != "");

						if(!string.IsNullOrEmpty(spilts.ElementAt(0)))
						{
							SoftWareBin[spilts.ElementAt(0)] = s.Trim().Split(' ').Where(c => c != "").Skip(3).ToList();
						}
					}
				}
			}
		}
	}
}