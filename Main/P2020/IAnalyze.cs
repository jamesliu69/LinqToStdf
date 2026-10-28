using System;
using System.Data;

namespace STDF
{
	/// <summary>P2020 測試記錄分析器的介面：定義分析（AnalyzeFile/GroupBySite/CalMeasure）、輸出（OutputFile/GetTable）與項目選取事件；實作為 CP2020 與 CFileParam。</summary>
	public interface IAnalyze
	{
		string SavePath { get; set; }

		int AllPin { get; set; }

		int PassPin { get; set; }
		int FailPin { get; set; }

		void AnalyzeFile();
		void GroupBySite();

		/// <summary>計算量測值</summary>
		void CalMeasure();

		/// <summary>輸出檔案</summary>
		void OutputFile();

		/// <summary>取得資料表</summary>
		DataTable GetTable();

		/// <summary>自動顯示項目</summary>
		void AutoShowItem();

		/// <summary>選擇項目時觸發</summary>
		event EventHandler evtSelectItem;

		/// <summary>錯誤發生時觸發</summary>
		event EventHandler evtErrorArise;
	}
}