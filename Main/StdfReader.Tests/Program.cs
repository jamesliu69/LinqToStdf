using StdfReader.Core;
using Stdf;
using Stdf.Records.V4;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

/// <summary>StdfReader.Core 的簡易回歸測試：以內建 sample 與合成 STDF 驗證 StdfDocumentLoader 的量測判定、繼承縮放、PIR/PRR 配對與各種不完整檔案的診斷；全部通過輸出 "PASS: N assertions" 並回傳 0。</summary>
internal static class Program
{
    /// <summary>測試進入點；任何未捕捉的例外視為失敗，輸出例外類型與訊息並回傳 1。</summary>
    [STAThread]
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
        TestRecordPresentation(loader, doc, check);
        string empty = Path.GetTempFileName();
        try { check(!loader.Load(empty).IsComplete, "空檔必須回報不完整"); }
        finally { File.Delete(empty); }
        TestSynthetic(loader, check);
        TestTrend(check);
        Console.WriteLine("PASS: " + assertions + " assertions");
        return 0;
    }

    private static void TestRecordPresentation(StdfDocumentLoader loader, StdfDocument sample, Action<bool, string> check)
    {
        var formType = Assembly.Load("StdfReader").GetType("StdfReader.MainForm", true);
        using (var form = (Form)Activator.CreateInstance(formType, new object[] { new string[0] }))
        {
            formType.GetField("document", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(form, sample);
            formType.GetMethod("DisplayDocument", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(form, new object[] { sample });
            var tree = (TreeView)formType.GetField("recordTree", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
            var fields = (DataGridView)formType.GetField("fieldGrid", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
            var nodes = Flatten(tree.Nodes).ToArray();
            var recordNodes = nodes.Where(n => n.Tag is StdfReader.Core.RecordEntry).ToArray();
            var ptrNodes = recordNodes.Where(n => ((StdfReader.Core.RecordEntry)n.Tag).Record is Ptr).ToArray();
            Action showSelected = () => formType.GetMethod("ShowSelectedRecord", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(form, null);
            check(ptrNodes.Length == 23, "記錄樹應保留並顯示全部 23 筆 PTR (tree=" + ptrNodes.Length + ", records=" + sample.Records.Count + ")");

            var mirNode = recordNodes.First(n => ((StdfReader.Core.RecordEntry)n.Tag).Record is Mir);
            tree.SelectedNode = mirNode;
            showSelected();
            check(FieldValue(fields, "REC_TYP") == "1" && FieldValue(fields, "REC_SUB") == "10", "MIR 應列出 REC_TYP/REC_SUB 標準代碼 (" + FieldValue(fields, "REC_TYP") + "/" + FieldValue(fields, "REC_SUB") + ")");
            check(FieldValue(fields, "LOT_ID") == ((Mir)((StdfReader.Core.RecordEntry)mirNode.Tag).Record).LotId, "MIR LOT_ID 應顯示解析值");
            check(FieldValue(fields, "PART_TYP") == "未提供／預設", "缺少 MIR 字串欄位不得冒出 0");

            var cpNode = ptrNodes.FirstOrDefault(n => (((Ptr)((StdfReader.Core.RecordEntry)n.Tag).Record).TestText ?? "").Contains("(Cp)"));
            if (cpNode != null)
            {
                tree.SelectedNode = cpNode;
                showSelected();
                double cpValue;
                check(double.TryParse(FieldValue(fields, "RESULT"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out cpValue) && Math.Abs(cpValue - 1e-9) < 1e-16, "Cp 結果需保留科學記號精度");
            }
            var invalidNode = ptrNodes.FirstOrDefault(n => ((((Ptr)((StdfReader.Core.RecordEntry)n.Tag).Record).TestFlags & 0x02) != 0));
            check(invalidNode != null, "sample 應含 invalid PTR 供 raw 值驗證");
            if (invalidNode != null)
            {
                tree.SelectedNode = invalidNode;
                showSelected();
                check(FieldValue(fields, "RESULT") == "0E+0", "invalid PTR 的原始 RESULT=0 不得被抹成未提供");
                check(FieldNote(fields, "RESULT").Contains("TEST_FLG=0x"), "invalid PTR 的說明需包含 TEST_FLG");
            }
        }

        string tsrPath = Path.ChangeExtension(Path.GetTempFileName(), ".stdf");
        try
        {
            var rawTsr = new List<byte> { 2, 0, 0, 10, 2, 4 };
            rawTsr.AddRange(RawTsrRecord(1, null, null));
            rawTsr.AddRange(RawTsrRecord(2, 4, 1.5f));
            File.WriteAllBytes(tsrPath, rawTsr.ToArray());
            var tsrDoc = loader.Load(tsrPath);
            using (var form = (Form)Activator.CreateInstance(formType, new object[] { new string[0] }))
            {
                formType.GetField("document", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(form, tsrDoc);
                formType.GetMethod("DisplayDocument", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(form, new object[] { tsrDoc });
                var tree = (TreeView)formType.GetField("recordTree", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                var fields = (DataGridView)formType.GetField("fieldGrid", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                var nodes = Flatten(tree.Nodes).Where(n => n.Tag is StdfReader.Core.RecordEntry && ((StdfReader.Core.RecordEntry)n.Tag).Record is Tsr).ToArray();
                check(nodes.Length == 2, "合成 TSR 紀錄應出現在樹中");
                if (nodes.Length == 2)
                {
                    tree.SelectedNode = nodes.First(n => ((Tsr)((StdfReader.Core.RecordEntry)n.Tag).Record).TestNumber == 1);
                    formType.GetMethod("ShowSelectedRecord", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(form, null);
                    check(FieldValue(fields, "OPT_FLAG") == "未提供／預設" && FieldNote(fields, "OPT_FLAG") == "原始紀錄未提供 OPT_FLAG", "TSR 尾端合法省略 OPT_FLAG 應標示未提供");
                    tree.SelectedNode = nodes.First(n => ((Tsr)((StdfReader.Core.RecordEntry)n.Tag).Record).TestNumber == 2);
                    formType.GetMethod("ShowSelectedRecord", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(form, null);
                    check(FieldValue(fields, "OPT_FLAG") == "4" && FieldNote(fields, "OPT_FLAG") == "原始位元組", "TSR OPT_FLAG 應顯示實際原始位元組");
                }
            }
        }
        finally { File.Delete(tsrPath); }

        var paired = LoadRecords(loader, new StdfRecord[] {
            new Far { CpuType = 2, StdfVersion = 4 }, new Mir { LotId = "TREE" },
            new Pir { HeadNumber = 1, SiteNumber = 1 }, new Pir { HeadNumber = 2, SiteNumber = 1 }, new Pir { HeadNumber = 1, SiteNumber = 2 },
            new Ptr { TestNumber = 11, HeadNumber = 1, SiteNumber = 1, Result = 1, TestFlags = 0 },
            new Ptr { TestNumber = 22, HeadNumber = 2, SiteNumber = 1, Result = 2, TestFlags = 0 },
            new Ptr { TestNumber = 33, HeadNumber = 1, SiteNumber = 2, Result = 3, TestFlags = 0 },
            new Prr { HeadNumber = 1, SiteNumber = 1, PartId = "A", Failed = false },
            new Prr { HeadNumber = 2, SiteNumber = 1, PartId = "B", Failed = false },
            new Prr { HeadNumber = 1, SiteNumber = 2, PartId = "C", Failed = false },
            new Mrr { FinishTime = new DateTime(2026, 1, 1) }
        });
        using (var form = (Form)Activator.CreateInstance(formType, new object[] { new string[0] }))
        {
            formType.GetField("document", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(form, paired);
            formType.GetMethod("DisplayDocument", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(form, new object[] { paired });
            var tree = (TreeView)formType.GetField("recordTree", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
            var filter = (ComboBox)formType.GetField("recordFilter", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
            filter.SelectedItem = "Ptr";
            var branches = tree.Nodes.Cast<TreeNode>().Where(n => n.Text.StartsWith("Part ", StringComparison.Ordinal)).ToArray();
            check(branches.Length == 3, "PTR 類型篩選需保留三個 Part 上下文");
            check(branches.All(n =>
            {
                if (n.Nodes.Count != 1) return false;
                var p = (Ptr)((StdfReader.Core.RecordEntry)n.Nodes[0].Tag).Record;
                return n.Text.Contains("Part A") ? p.HeadNumber == 1 && p.SiteNumber == 1 : n.Text.Contains("Part B") ? p.HeadNumber == 2 && p.SiteNumber == 1 : p.HeadNumber == 1 && p.SiteNumber == 2;
            }), "PTR 篩選下仍須依實際 Head/Site 掛回正確 Part");
        }
    }

    private static IEnumerable<TreeNode> Flatten(TreeNodeCollection nodes)
    {
        foreach (TreeNode node in nodes)
        {
            yield return node;
            foreach (var child in Flatten(node.Nodes)) yield return child;
        }
    }

    private static byte[] RawTsrRecord(uint testNumber, byte? optionalFlags, float? testTime)
    {
        var payload = new List<byte> { 1, 1, (byte)'P' };
        payload.AddRange(BitConverter.GetBytes(testNumber));
        payload.AddRange(BitConverter.GetBytes(1u));
        payload.AddRange(BitConverter.GetBytes(0u));
        payload.AddRange(BitConverter.GetBytes(0u));
        if (optionalFlags.HasValue)
        {
            payload.AddRange(new byte[] { 0, 0, 0, optionalFlags.Value });
            if (testTime.HasValue) payload.AddRange(BitConverter.GetBytes(testTime.Value));
        }
        var record = new List<byte> { (byte)payload.Count, (byte)(payload.Count >> 8), 10, 30 };
        record.AddRange(payload);
        return record.ToArray();
    }

    private static string FieldValue(DataGridView grid, string code)
    {
        foreach (DataGridViewRow row in grid.Rows) if (Convert.ToString(row.Cells[0].Value) == code) return Convert.ToString(row.Cells[2].Value);
        return null;
    }

    private static string FieldNote(DataGridView grid, string code)
    {
        foreach (DataGridViewRow row in grid.Rows) if (Convert.ToString(row.Cells[0].Value) == code) return Convert.ToString(row.Cells[3].Value);
        return null;
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
            var orphanPtr = LoadRecords(loader, new StdfRecord[] { new Far { CpuType = 2, StdfVersion = 4 }, new Mir(), new Ptr { TestNumber = 1, HeadNumber = 9, SiteNumber = 9, Result = 4 }, new Mrr { FinishTime = new DateTime(2026, 1, 1) } });
            check(!orphanPtr.IsComplete && orphanPtr.Diagnostics.Any(d => d.Contains("PTR")), "有結果的 orphan PTR 必須診斷且不完整");
            check(orphanPtr.Measurements.Count == 1 && orphanPtr.Measurements[0].PartIndex == null && orphanPtr.Measurements[0].Value == 4, "orphan PTR 應保留數值供檢查，不捏造 part");
            var invalidOrphanPtr = LoadRecords(loader, new StdfRecord[] { new Far { CpuType = 2, StdfVersion = 4 }, new Ptr { TestNumber = 1, TestFlags = 0x82, Result = null }, new Mrr { FinishTime = new DateTime(2026, 1, 1) } });
            check(!invalidOrphanPtr.IsComplete && invalidOrphanPtr.Measurements.Count == 1 && invalidOrphanPtr.Measurements[0].Status == "Invalid", "已執行但無結果的 orphan PTR 不可誤當純預設而靜默略過");
            var defaultPtr = LoadRecords(loader, new StdfRecord[] { new Far { CpuType = 2, StdfVersion = 4 }, new Mir(), new Ptr { TestNumber = 1, TestFlags = 0x12, Result = null, Units = "V", HighLimit = 10 }, new Pir { HeadNumber = 1, SiteNumber = 1 }, new Ptr { TestNumber = 1, HeadNumber = 1, SiteNumber = 1, Result = 8 }, new Prr { HeadNumber = 1, SiteNumber = 1 }, new Mrr { FinishTime = new DateTime(2026, 1, 1) } });
            check(defaultPtr.IsComplete && defaultPtr.Measurements.Count == 1 && defaultPtr.Measurements[0].HighLimit == 10 && defaultPtr.Measurements[0].Units == "V", "PIR 外首筆純預設 PTR 合法且只供繼承，不算量測");
            var pending = LoadRecords(loader, new StdfRecord[] { new Far { CpuType = 2, StdfVersion = 4 }, new Pir { HeadNumber = 9, SiteNumber = 9 }, new Mrr { FinishTime = new DateTime(2026, 1, 1) } });
            check(!pending.IsComplete && pending.Parts[0].Status == "Unknown" && pending.Diagnostics.Any(d => d.Contains("PIR")), "未配對 PIR 必須保留 unknown 並不完整");

            string big = Path.ChangeExtension(Path.GetTempFileName(), ".stdf");
            try
            {
                using (var writer = new StdfFileWriter(big, Endian.Big)) writer.WriteRecords(new StdfRecord[] { new Far { CpuType = 1, StdfVersion = 4 }, new Mir { LotId = "BE" }, new Mrr { FinishTime = new DateTime(2026, 1, 1) } });
                check(loader.Load(big).IsComplete, "big-endian FAR/MIR/MRR 應載入完整");
                using (var stream = new FileStream(big, FileMode.Open, FileAccess.Write)) stream.SetLength(stream.Length - 2);
                var partial = loader.Load(big);
                check(!partial.IsComplete, "截斷檔應回報不完整");
                var exceptionProperty = partial.GetType().GetProperty("LoadException", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var loadException = (Exception)exceptionProperty.GetValue(partial);
                check(loadException != null && !string.IsNullOrWhiteSpace(loadException.StackTrace), "截斷檔 partial 結果保留原始例外與 stack");
                string logDirectory = Path.Combine(Path.GetTempPath(), "StdfReaderPartialLog-" + Guid.NewGuid().ToString("N"));
                var logger = Assembly.Load("CSTDF").GetType("STDF.TraceLogger", true);
                var directoryProvider = logger.GetField("LogDirectoryProvider", BindingFlags.NonPublic | BindingFlags.Static);
                var oldProvider = directoryProvider.GetValue(null);
                try
                {
                    directoryProvider.SetValue(null, new Func<string>(() => logDirectory));
                    var formType = Assembly.Load("StdfReader").GetType("StdfReader.MainForm", true);
                    var form = (Form)Activator.CreateInstance(formType, new object[] { new string[0] });
                    using (form)
                    {
                        formType.GetMethod("BeginLoad", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(form, new object[] { big });
                        var deadline = DateTime.UtcNow.AddSeconds(10);
                        while (!Directory.Exists(logDirectory) && DateTime.UtcNow < deadline) { Application.DoEvents(); Thread.Sleep(20); }
                        while (Directory.Exists(logDirectory) && !Directory.GetFiles(logDirectory, "stdf-trace-*.log").Any() && DateTime.UtcNow < deadline) { Application.DoEvents(); Thread.Sleep(20); }
                        while (Directory.Exists(logDirectory) && Directory.GetFiles(logDirectory, "stdf-trace-*.log").Any() && !File.ReadAllText(Directory.GetFiles(logDirectory, "stdf-trace-*.log").Single()).Contains("Load.Partial") && DateTime.UtcNow < deadline) { Application.DoEvents(); Thread.Sleep(20); }
                        var dailyLog = Directory.GetFiles(logDirectory, "stdf-trace-*.log").Single();
                        string logText = File.ReadAllText(dailyLog);
                        check(logText.Contains("Load.Partial") && logText.Contains(loadException.GetType().FullName) && logText.Contains(loadException.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None)[0]), "partial load 日誌保留例外原因與 stack");
                        formType.GetMethod("BeginLoad", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(form, new object[] { big + ".missing" });
                        var stateLabel = (Label)formType.GetField("stateLabel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                        var openButton = (Button)formType.GetField("openButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                        deadline = DateTime.UtcNow.AddSeconds(10);
                        while (!stateLabel.Text.StartsWith("載入失敗：", StringComparison.Ordinal) && DateTime.UtcNow < deadline) { Application.DoEvents(); Thread.Sleep(20); }
                        check(stateLabel.Text.StartsWith("載入失敗：", StringComparison.Ordinal) && openButton.Enabled, "載入錯誤清除狀態並恢復開檔按鈕");
                    }
                }
                finally
                {
                    directoryProvider.SetValue(null, oldProvider);
                    if (Directory.Exists(logDirectory)) Directory.Delete(logDirectory, true);
                }
            }
            finally { File.Delete(big); }
        }
        finally { File.Delete(path); }
    }

    private static void TestTrend(Action<bool, string> check)
    {
        var formType = Assembly.Load("StdfReader").GetType("StdfReader.MainForm", true);
        using (var form = (Form)Activator.CreateInstance(formType, new object[] { new string[0] }))
        {
            var values = new List<MeasurementEntry> {
                new MeasurementEntry { Sequence = 1, Value = 4, HighLimit = 5, Status = "Pass" },
                new MeasurementEntry { Sequence = 2, Value = 8, LowLimit = 2, HighLimit = 10, Status = "Pass" },
                new MeasurementEntry { Sequence = 3, Value = 7, Status = "Pass" }
            };
            formType.GetMethod("DrawTrend", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(form, new object[] { values });
            var chart = (Chart)formType.GetField("trendChart", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
            var high = chart.Series["上限"].Points;
            check(high.Count == 2 && high[0].XValue == 1 && high[0].YValues[0] == 5 && high[1].XValue == 2 && high[1].YValues[0] == 10, "圖表上限應逐筆顯示 5、10，缺值不延伸");
            var low = chart.Series["下限"].Points;
            check(low.Count == 1 && low[0].XValue == 2 && low[0].YValues[0] == 2, "圖表下限僅顯示有下限的量測");
            check(chart.Series["有效 PTR"].Points.Count == 3, "不同界限不可遺失有效量測");
            var evidence = Environment.GetEnvironmentVariable("STDF_TREND_EVIDENCE");
            if (!string.IsNullOrEmpty(evidence))
            {
                form.Show();
                var tabs = (TabControl)formType.GetField("tabs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                tabs.SelectedIndex = 2;
                Application.DoEvents();
                chart.SaveImage(evidence, ChartImageFormat.Png);
                form.Close();
            }
        }
    }

    /// <summary>把記錄寫入暫存 STDF 檔（little-endian）後載入，回傳 StdfDocument 供驗證；結束時刪除暫存檔。</summary>
    private static StdfDocument LoadRecords(StdfDocumentLoader loader, StdfRecord[] records)
    {
        string path = Path.ChangeExtension(Path.GetTempFileName(), ".stdf");
        try { using (var writer = new StdfFileWriter(path, Endian.Little)) writer.WriteRecords(records); return loader.Load(path); }
        finally { File.Delete(path); }
    }
}
