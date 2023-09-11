using System;
using System.Data;

namespace P2020
{
	public interface IAnalyze
	{
		string SavePath { get; set; }

		int AllPin { get; set; }

		int PassPin { get; set; }
		int FailPin { get; set; }

		void AnalyzeFile();
		void GroupBySite();

		/// <summary>
		/// 計算量測值
		/// </summary>
		void CalMeasure();

		/// <summary>
		/// 輸出檔案
		/// </summary>
		void OutputFile();

		/// <summary>
		/// 取得資料表 
		/// </summary>
		/// <returns></returns>
		DataTable GetTable();

		/// <summary>
		/// 自動顯示項目
		/// </summary>
		void AutoShowItem();

		/// <summary>選擇項目時觸發</summary>
		event EventHandler evtSelectItem;

		/// <summary>錯誤發生時觸發</summary>
		event EventHandler evtErrorArise;
	}
}