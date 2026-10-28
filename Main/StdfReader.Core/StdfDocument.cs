using System;
using System.Collections.Generic;
namespace StdfReader.Core
{
    /// <summary>載入單一 STDF 檔後的彙整結果：原始記錄、料件（part）、量測（PTR）與批次資訊；由 StdfDocumentLoader 填入。</summary>
    public sealed class StdfDocument
    {
        public string FilePath { get; set; }
        /// <summary>檔案是否完整載入；任何診斷（格式錯誤、PIR/PRR 配對失敗、缺 MRR 等）都會設為 false。</summary>
        public bool IsComplete { get; set; }
        /// <summary>不完整或警告原因的清單，與 IsComplete=false 對應。</summary>
        public List<string> Diagnostics { get; } = new List<string>();
        public List<RecordEntry> Records { get; } = new List<RecordEntry>();
        public List<PartEntry> Parts { get; } = new List<PartEntry>();
        public List<MeasurementEntry> Measurements { get; } = new List<MeasurementEntry>();
        public string LotId { get; set; }
        /// <summary>由 MIR 取得的批次開始時間，以 UTC 解讀（SpecifyKind Utc）。</summary>
        public DateTime? StartTimeUtc { get; set; }
        /// <summary>由 MRR 取得的批次結束時間，以 UTC 解讀（SpecifyKind Utc）。</summary>
        public DateTime? EndTimeUtc { get; set; }
        /// <summary>已剖析的原始記錄總數（不含開始/結束串流記錄）。</summary>
        public int RawRecordCount { get; set; }
        /// <summary>記錄名稱 → 出現次數的統計，供 UI 顯示分佈。</summary>
        public Dictionary<string, int> RecordCounts { get; } = new Dictionary<string, int>();
    }
}
