using StdfReader.Core;
using Stdf;
using Stdf.Records.V4;
using System;
using System.IO;
using System.Linq;

/// <summary>StdfReader.Core 的簡易回歸測試：以內建 sample 與合成 STDF 驗證 StdfDocumentLoader 的量測判定、繼承縮放、PIR/PRR 配對與各種不完整檔案的診斷；全部通過輸出 "PASS: N assertions" 並回傳 0。</summary>
internal static class Program
{
    /// <summary>測試進入點；任何未捕捉的例外視為失敗，輸出例外類型與訊息並回傳 1。</summary>
    private static int Main()
    {
        try { return Run(); }
        catch (Exception ex) { Console.Error.WriteLine(ex.GetType().FullName + ": " + ex.Message); return 1; }
    }

    /// <summary>執行所有測試：先驗證內建 sample（Fixtures\demo.stdf）的載入結果與各狀態數量，再以暫存合成檔驗證縮放、旗標判定與不完整結構的診斷。</summary>
    private static int Run()
    {
        int assertions = 0;
        Action<bool, string> check = (condition, message) => { assertions++; if (!condition) throw new Exception(message); };
        var loader = new StdfDocumentLoader();
        var sample = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "demo.stdf");
        var doc = loader.Load(sample);
        check(doc.IsComplete, "現有 sample 應完整載入：" + string.Join(";", doc.Diagnostics));
        check(doc.RawRecordCount > 0 && doc.Records.All(r => r.Record != null), "raw record 應保留且不含合成記錄");
        check(doc.Measurements.Count == 23, "sample 應有 23 個 PTR");
        check(doc.Parts.Count == 1, "sample 應有 1 個 part");
        check(doc.Measurements.Count(m => m.Status == "Pass") == 18, "sample 應有 18 個 pass");
        check(doc.Measurements.Count(m => m.Status == "Fail") == 3, "sample 應有 3 個 fail");
        check(doc.Measurements.Count(m => m.Status == "Invalid") == 2, "sample 應有 2 個 invalid");
        check(doc.Measurements.Count(m => m.Value.HasValue && Math.Abs(m.Value.Value - 1e-9) < 1e-16) == 5, "sample 應保留 5 個 Cp=1E-9");
        check(doc.StartTimeUtc.HasValue && doc.StartTimeUtc.Value.Hour == 9 && doc.StartTimeUtc.Value.Minute == 32, "MIR 開始時間應為 UTC 09:32");
        string empty = Path.GetTempFileName();
        try { check(!loader.Load(empty).IsComplete, "空檔必須回報不完整"); }
        finally { File.Delete(empty); }
        TestSynthetic(loader, check);
        Console.WriteLine("PASS: " + assertions + " assertions");
        return 0;
    }

    /// <summary>以多種旗標組合（TEST_FLG/PARM_FLG、no-limit、NaN/Infinity、重複 PIR、非 V4、缺 MRR、orphan PRR/PIR、big-endian、截斷檔）合成 STDF 驗證載入器的判定與診斷。</summary>
    private static void TestSynthetic(StdfDocumentLoader loader, Action<bool, string> check)
    {
        string path = Path.ChangeExtension(Path.GetTempFileName(), ".stdf");
        try
        {
            using (var writer = new StdfFileWriter(path, Endian.Little))
            {
                writer.WriteRecords(new StdfRecord[] {
                    new Far { CpuType = 2, StdfVersion = 4 }, new Mir { LotId = "SYN", StartTime = new DateTime(2026, 1, 1) },
                    new Pir { HeadNumber = 1, SiteNumber = 1 }, new Pir { HeadNumber = 2, SiteNumber = 1 },
                    new Ptr { TestNumber = 7, HeadNumber = 1, SiteNumber = 1, Result = 2, Units = "V", ResultScalingExponent = 1, LowLimit = 1, LowLimitScalingExponent = 1, HighLimit = 9, HighLimitScalingExponent = 1, TestFlags = 0x80 },
                    new Ptr { TestNumber = 7, HeadNumber = 2, SiteNumber = 1, Result = 1, TestFlags = 0x40 },
                    new Prr { HeadNumber = 1, SiteNumber = 1, PartId = "A", Failed = false },
                    new Pir { HeadNumber = 1, SiteNumber = 1 },
                    new Ptr { TestNumber = 7, HeadNumber = 1, SiteNumber = 1, Result = 3, TestFlags = 0x80 },
                    new Ptr { TestNumber = 20, HeadNumber = 1, SiteNumber = 1, Result = 4, TestFlags = 0x01 },
                    new Ptr { TestNumber = 21, HeadNumber = 1, SiteNumber = 1, Result = 4, TestFlags = 0x02 },
                    new Ptr { TestNumber = 22, HeadNumber = 1, SiteNumber = 1, Result = 4, TestFlags = 0x04 },
                    new Ptr { TestNumber = 23, HeadNumber = 1, SiteNumber = 1, Result = 4, TestFlags = 0x08 },
                    new Ptr { TestNumber = 24, HeadNumber = 1, SiteNumber = 1, Result = 4, TestFlags = 0x10 },
                    new Ptr { TestNumber = 25, HeadNumber = 1, SiteNumber = 1, Result = 4, TestFlags = 0x20 },
                    new Ptr { TestNumber = 26, HeadNumber = 1, SiteNumber = 1, Result = 4, ParametricFlags = 0x01 },
                    new Ptr { TestNumber = 27, HeadNumber = 1, SiteNumber = 1, Result = 4, ParametricFlags = 0x02 },
                    new Ptr { TestNumber = 28, HeadNumber = 1, SiteNumber = 1, Result = 4, ParametricFlags = 0x04 },
                    new Ptr { TestNumber = 90, HeadNumber = 1, SiteNumber = 1, Result = 5, Units = "A", LowLimit = 1, HighLimit = 8 },
                    new Ptr { TestNumber = 90, HeadNumber = 1, SiteNumber = 1, Result = 6, OptionalFlags = 0xC0 },
                    new Ptr { TestNumber = 91, HeadNumber = 1, SiteNumber = 1, Result = float.NaN },
                    new Ptr { TestNumber = 92, HeadNumber = 1, SiteNumber = 1, Result = float.PositiveInfinity },
                    new UnknownRecord(new RecordType(99, 88), new byte[] { 3, 4, 5 }, Endian.Little),
                    new Prr { HeadNumber = 2, SiteNumber = 1, PartId = "B", Failed = true },
                    new Prr { HeadNumber = 1, SiteNumber = 1, PartId = "A2", Failed = true },
                    new Mrr { FinishTime = new DateTime(2026, 1, 1, 0, 1, 0) }
                });
            }
            var result = loader.Load(path);
            check(result.IsComplete, "合成多 part 檔應完整：" + string.Join(";", result.Diagnostics));
            check(result.Parts.Count == 3 && result.Parts[0].PartId == "A" && result.Parts[2].PartId == "A2", "交錯 head/site 與 retest 應正確配對");
            check(result.Measurements[0].Status == "Fail" && result.Measurements[0].Value == 20, "PTR 0x80 是有效 Fail 且套用 10^1 scaling");
            check(result.Measurements[1].Status == "Unknown" && result.Measurements[1].Value == 10 && result.Measurements[1].ResultScalingInherited, "PTR 0x40 為 Unknown、數值有效且繼承 scaling");
            check(result.Measurements[2].Status == "Fail" && result.Measurements[2].Value == 30, "fail flag 仍保留有效量測並套用繼承縮放");
            check(result.Measurements.Skip(3).Take(9).All(m => m.Status == "Invalid" && m.Value == null), "TEST_FLG bits 0..5 與 PARM_FLG bits 0..2 均排除數值");
            check(result.Measurements[12].Units == "A" && result.Measurements[13].LowLimit == null && result.Measurements[13].HighLimit == null, "no-limit flags 不得繼承預設 limits");
            check(result.Records.Any(r => r.Record is UnknownRecord u && u.Content.Length == 3), "未知記錄 raw bytes 應保留");
            check(result.Measurements.Where(m => m.TestNumber == 91 || m.TestNumber == 92).All(m => m.Status == "Invalid" && m.Value == null && !string.IsNullOrEmpty(m.InvalidReason)), "NaN/Infinity 必須是 Invalid 並說明原因");
            var duplicateSite = LoadRecords(loader, new StdfRecord[] { new Far { CpuType = 2, StdfVersion = 4 }, new Pir { HeadNumber = 1, SiteNumber = 1 }, new Pir { HeadNumber = 1, SiteNumber = 1 }, new Prr { HeadNumber = 1, SiteNumber = 1 }, new Mrr { FinishTime = new DateTime(2026, 1, 1) } });
            check(!duplicateSite.IsComplete && duplicateSite.Parts[0].Status == "Unknown" && duplicateSite.Parts[1].PartId == null, "重複 PIR 不得覆蓋未結束 part 且必須標記不完整");
            var badVersion = LoadRecords(loader, new StdfRecord[] { new Far { CpuType = 2, StdfVersion = 3 }, new Mrr { FinishTime = new DateTime(2026, 1, 1) } });
            check(!badVersion.IsComplete && badVersion.Diagnostics.Any(d => d.Contains("STDF V4")), "FAR 非 V4 必須不完整");
            var noMrr = LoadRecords(loader, new StdfRecord[] { new Far { CpuType = 2, StdfVersion = 4 } });
            check(!noMrr.IsComplete && noMrr.Diagnostics.Any(d => d.Contains("MRR")), "缺少 MRR 必須不完整");
            var orphan = LoadRecords(loader, new StdfRecord[] { new Far { CpuType = 2, StdfVersion = 4 }, new Prr { HeadNumber = 9, SiteNumber = 9 }, new Mrr { FinishTime = new DateTime(2026, 1, 1) } });
            check(!orphan.IsComplete && orphan.Diagnostics.Any(d => d.Contains("PRR")), "orphan PRR 必須診斷且不完整");
            var pending = LoadRecords(loader, new StdfRecord[] { new Far { CpuType = 2, StdfVersion = 4 }, new Pir { HeadNumber = 9, SiteNumber = 9 }, new Mrr { FinishTime = new DateTime(2026, 1, 1) } });
            check(!pending.IsComplete && pending.Parts[0].Status == "Unknown" && pending.Diagnostics.Any(d => d.Contains("PIR")), "未配對 PIR 必須保留 unknown 並不完整");

            string big = Path.ChangeExtension(Path.GetTempFileName(), ".stdf");
            try
            {
                using (var writer = new StdfFileWriter(big, Endian.Big)) writer.WriteRecords(new StdfRecord[] { new Far { CpuType = 1, StdfVersion = 4 }, new Mir { LotId = "BE" }, new Mrr { FinishTime = new DateTime(2026, 1, 1) } });
                check(loader.Load(big).IsComplete, "big-endian FAR/MIR/MRR 應載入完整");
                using (var stream = new FileStream(big, FileMode.Open, FileAccess.Write)) stream.SetLength(stream.Length - 2);
                check(!loader.Load(big).IsComplete, "截斷檔應回報不完整");
            }
            finally { File.Delete(big); }
        }
        finally { File.Delete(path); }
    }

    /// <summary>把記錄寫入暫存 STDF 檔（little-endian）後載入，回傳 StdfDocument 供驗證；結束時刪除暫存檔。</summary>
    private static StdfDocument LoadRecords(StdfDocumentLoader loader, StdfRecord[] records)
    {
        string path = Path.ChangeExtension(Path.GetTempFileName(), ".stdf");
        try { using (var writer = new StdfFileWriter(path, Endian.Little)) writer.WriteRecords(records); return loader.Load(path); }
        finally { File.Delete(path); }
    }
}
