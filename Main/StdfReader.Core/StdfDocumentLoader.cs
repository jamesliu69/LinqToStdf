using Stdf;
using Stdf.Records;
using Stdf.Records.V4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StdfReader.Core
{
    /// <summary>將 STDF V4 檔載入為 StdfDocument：逐記錄剖析，配對 PIR/PRR 成料件、PTR 成量測（套用同測試編號預設值與 10^exponent 縮放），並在結構不完整時記錄診斷。</summary>
    public sealed class StdfDocumentLoader
    {
        /// <summary>載入 path 指定的 STDF 檔：跳過串流頭尾記錄、驗證 FAR 版本為 V4、收集 MIR/MRR 批次資訊、配對料件與量測；任何結構問題（缺 MRR、PIR/PRR 不符、截斷等）都會加進 Diagnostics 並令 IsComplete=false，不拋出例外。</summary>
        public StdfDocument Load(string path)
        {
            var doc = new StdfDocument { FilePath = Path.GetFullPath(path), IsComplete = true };
            if (!File.Exists(path)) { doc.IsComplete = false; doc.Diagnostics.Add("檔案不存在：" + path); return doc; }
            if (new FileInfo(path).Length == 0) { doc.IsComplete = false; doc.Diagnostics.Add("STDF 檔案為空。"); return doc; }
            var active = new Dictionary<string, int>();
            var defaults = new Dictionary<uint, Ptr>();
            try
            {
                var file = new StdfFile(path);
                foreach (var record in file.GetRecordsEnumerable())
                {
                    if (record is StartOfStreamRecord) continue;
                    if (record is EndOfStreamRecord) continue;
                    if (record is FormatErrorRecord error)
                    {
                        doc.IsComplete = false; doc.Diagnostics.Add(error.Message ?? "STDF 格式錯誤"); continue;
                    }
                    if (record is Mir mir)
                    {
                        doc.LotId = mir.LotId;
                        if (mir.StartTime.HasValue) doc.StartTimeUtc = DateTime.SpecifyKind(mir.StartTime.Value, DateTimeKind.Utc);
                    }
                    if (record is Far far && far.StdfVersion != 4) { doc.IsComplete = false; doc.Diagnostics.Add("僅支援 STDF V4；FAR STDF_VER=" + far.StdfVersion); }
                    if (record is Mrr mrr && mrr.FinishTime.HasValue) doc.EndTimeUtc = DateTime.SpecifyKind(mrr.FinishTime.Value, DateTimeKind.Utc);
                    var name = record.GetType() == typeof(UnknownRecord) ? "Unknown " + record.RecordType : record.GetType().Name;
                    doc.Records.Add(new RecordEntry { Record = record, Offset = record.Offset, Name = name });
                    doc.RawRecordCount++;
                    doc.RecordCounts[name] = doc.RecordCounts.ContainsKey(name) ? doc.RecordCounts[name] + 1 : 1;
                    if (record is Pir pir)
                    {
                        var p = new PartEntry { Index = doc.Parts.Count, Head = pir.HeadNumber, Site = pir.SiteNumber, Status = "Unknown" };
                        var key = Key(pir.HeadNumber, pir.SiteNumber);
                        int previous;
                        if (active.TryGetValue(key, out previous))
                        {
                            doc.IsComplete = false;
                            doc.Diagnostics.Add("新 PIR 覆蓋尚未完成的 part " + previous + " (H" + pir.HeadNumber + "/S" + pir.SiteNumber + ").");
                        }
                        doc.Parts.Add(p); active[key] = p.Index;
                    }
                    else if (record is Prr prr)
                    {
                        int index; if (active.TryGetValue(Key(prr.HeadNumber, prr.SiteNumber), out index))
                        {
                            var p = doc.Parts[index]; p.PartId = prr.PartId; p.TestTimeMilliseconds = prr.TestTime;
                            p.Status = prr.Failed.HasValue ? (prr.Failed.Value ? "Fail" : "Pass") : "Unknown"; active.Remove(Key(prr.HeadNumber, prr.SiteNumber));
                        }
                        else { doc.IsComplete = false; doc.Diagnostics.Add("PRR 沒有相符的 PIR (H" + prr.HeadNumber + "/S" + prr.SiteNumber + ")."); }
                    }
                    else if (record is Ptr ptr) AddMeasurement(doc, ptr, active, defaults);
                }
            }
            catch (Exception ex) { doc.IsComplete = false; doc.LoadException = ex; doc.Diagnostics.Add(ex.Message); }
            if (doc.EndTimeUtc == null) { doc.IsComplete = false; doc.Diagnostics.Add("缺少 MRR 結束記錄。"); }
            foreach (var index in active.Values.Distinct()) { doc.Parts[index].Status = "Unknown"; doc.Diagnostics.Add("PIR 沒有相符的 PRR：part " + index); doc.IsComplete = false; }
            return doc;
        }

        /// <summary>把一筆 PTR 轉成 MeasurementEntry：以 defaults 內同測試編號的首筆 PTR 補繼承欄位（單位、縮放指數、上下限），套用縮放與 no-limit 旗標，判定 Pass/Fail/Unknown/Invalid 狀態。</summary>
        private static void AddMeasurement(StdfDocument doc, Ptr p, Dictionary<string, int> active, Dictionary<uint, Ptr> defaults)
        {
            int part; int? partIndex = active.TryGetValue(Key(p.HeadNumber, p.SiteNumber), out part) ? (int?)part : null;
            // STDF V4 允許首筆無結果 PTR 在 PIR 外提供預設值；它不是量測。
            if (!partIndex.HasValue)
            {
                if ((p.TestFlags & 0x10) != 0 && p.ParametricFlags == 0 && !defaults.ContainsKey(p.TestNumber)) { defaults[p.TestNumber] = p; return; }
                doc.IsComplete = false;
                doc.Diagnostics.Add("PTR 沒有相符的 PIR (H" + p.HeadNumber + "/S" + p.SiteNumber + ")，TestNumber=" + p.TestNumber + ".");
            }
            Ptr d; defaults.TryGetValue(p.TestNumber, out d);
            var resultScale = p.ResultScalingExponent ?? d?.ResultScalingExponent ?? (sbyte)0;
            var lowScale = p.LowLimitScalingExponent ?? d?.LowLimitScalingExponent ?? (sbyte)0;
            var highScale = p.HighLimitScalingExponent ?? d?.HighLimitScalingExponent ?? (sbyte)0;
            var units = p.Units ?? d?.Units;
            bool noLow = p.OptionalFlags.HasValue && (p.OptionalFlags.Value & 0x40) != 0;
            bool noHigh = p.OptionalFlags.HasValue && (p.OptionalFlags.Value & 0x80) != 0;
            var rawLow = p.LowLimit ?? (!noLow ? d?.LowLimit : null); var rawHigh = p.HighLimit ?? (!noHigh ? d?.HighLimit : null);
            var value = Scale(p.Result, resultScale); var low = Scale(rawLow, lowScale); var high = Scale(rawHigh, highScale);
            bool nonFiniteResult = p.Result.HasValue && !IsFinite((double)p.Result.Value * Math.Pow(10, resultScale));
            bool invalid = (p.TestFlags & 0x3f) != 0 || (p.ParametricFlags & 0x07) != 0 || nonFiniteResult;
            string status = invalid ? "Invalid" : (p.TestFlags & 0x40) != 0 ? "Unknown" : (p.TestFlags & 0x80) != 0 ? "Fail" : "Pass";
            var m = new MeasurementEntry { Sequence = doc.Measurements.Count + 1, TestNumber = p.TestNumber, HeadNumber = p.HeadNumber, SiteNumber = p.SiteNumber, PartIndex = partIndex, TestText = p.TestText ?? d?.TestText, Units = units, Value = invalid ? null : value, LowLimit = low, HighLimit = high, Status = status, InvalidReason = invalid ? (nonFiniteResult ? "Scaled value is not finite" : "PTR invalid flag") : ((p.TestFlags & 0x40) != 0 ? "PTR pass/fail unknown" : null), TestFlags = p.TestFlags, ParametricFlags = p.ParametricFlags, OptionalFlags = p.OptionalFlags, ResultScalingExponent = p.ResultScalingExponent ?? d?.ResultScalingExponent, LowLimitScalingExponent = p.LowLimitScalingExponent ?? d?.LowLimitScalingExponent, HighLimitScalingExponent = p.HighLimitScalingExponent ?? d?.HighLimitScalingExponent, UnitsInherited = p.Units == null && d?.Units != null, ResultScalingInherited = !p.ResultScalingExponent.HasValue && d?.ResultScalingExponent.HasValue == true, LowLimitInherited = !p.LowLimit.HasValue && !noLow && d?.LowLimit.HasValue == true, HighLimitInherited = !p.HighLimit.HasValue && !noHigh && d?.HighLimit.HasValue == true };
            if (value.HasValue && !IsFinite(value.Value)) { m.Value = null; m.Status = "Invalid"; m.InvalidReason = "Scaled value is not finite"; }
            if (low.HasValue && !IsFinite(low.Value)) m.LowLimit = null;
            if (high.HasValue && !IsFinite(high.Value)) m.HighLimit = null;
            if (!defaults.ContainsKey(p.TestNumber)) defaults[p.TestNumber] = p;
            doc.Measurements.Add(m);
        }

        /// <summary>把 float 乘上 10^exponent；結果非有限值（NaN/Infinity）時回傳 null。</summary>
        private static double? Scale(float? value, sbyte exponent)
        {
            if (!value.HasValue) return null;
            var scaled = (double)value.Value * Math.Pow(10, exponent);
            return IsFinite(scaled) ? (double?)scaled : null;
        }
        private static bool IsFinite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        /// <summary>PIR/PRR 配對用的 head:site 鍵；缺值時以 1:1 視之。</summary>
        private static string Key(byte? head, byte? site) { return (head ?? 1) + ":" + (site ?? 1); }
    }
}
