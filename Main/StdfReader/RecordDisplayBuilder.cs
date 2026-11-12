using Stdf;
using Stdf.Attributes;
using Stdf.Records.V4;
using StdfReader.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace StdfReader
{
    internal sealed class RecordFieldRow
    {
        public string Code { get; private set; }
        public string Property { get; private set; }
        public string Value { get; private set; }
        public string Note { get; private set; }

        public RecordFieldRow(string code, string property, string value, string note)
        { Code = code; Property = property; Value = value; Note = note; }
    }

    internal static class RecordDisplayBuilder
    {
        private static readonly Dictionary<string, string> Codes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "CpuType", "CPU_TYPE" }, { "StdfVersion", "STDF_VER" }, { "ModifiedTime", "MOD_TIM" }, { "CommandLine", "CMD_LINE" },
            { "SetupTime", "SETUP_T" }, { "StartTime", "START_T" }, { "StationNumber", "STAT_NUM" }, { "ModeCode", "MODE_COD" }, { "RetestCode", "RTST_COD" }, { "ProtectionCode", "PROT_COD" }, { "BurnInTime", "BURN_TIM" }, { "CommandModeCode", "CMOD_COD" }, { "LotId", "LOT_ID" }, { "PartType", "PART_TYP" }, { "NodeName", "NODE_NAM" }, { "TesterType", "TSTR_TYP" }, { "JobName", "JOB_NAM" }, { "JobRevision", "JOB_REV" }, { "SublotId", "SBLOT_ID" }, { "OperatorName", "OPER_NAM" }, { "ExecType", "EXEC_TYP" }, { "ExecVersion", "EXEC_VER" }, { "TestCode", "TEST_COD" }, { "TestTemperature", "TST_TEMP" }, { "UserText", "USER_TXT" }, { "AuxiliaryFile", "AUX_FILE" }, { "PackageType", "PKG_TYP" }, { "FamilyId", "FAMILY_ID" }, { "DateCode", "DATE_COD" }, { "FacilityId", "FACIL_ID" }, { "FloorId", "FLOOR_ID" }, { "ProcessId", "PROC_ID" }, { "OperationFrequency", "OPER_FRQ" }, { "SpecificationName", "SPEC_NAM" }, { "SpecificationVersion", "SPEC_VER" }, { "FlowId", "FLOW_ID" }, { "SetupId", "SETUP_ID" }, { "DesignRevision", "DSGN_REV" }, { "EngineeringId", "ENG_ID" }, { "RomCode", "ROM_COD" }, { "SerialNumber", "SERL_NUM" }, { "SupervisorName", "SUPR_NAM" },
            { "HeadNumber", "HEAD_NUM" }, { "SiteNumber", "SITE_NUM" }, { "SiteGroup", "SITE_GRP" }, { "SiteNumbers", "SITE_NUM" }, { "HandlerType", "HAND_TYP" }, { "HandlerId", "HAND_ID" }, { "CardType", "CARD_TYP" }, { "CardId", "CARD_ID" },
            { "Index", "PMR_INDX" }, { "ChannelType", "CHAN_TYP" }, { "ChannelName", "CHAN_NAM" }, { "PhysicalName", "PHY_NAM" }, { "LogicalName", "LOG_NAM" }, { "GroupIndex", "GRP_INDX" }, { "GroupName", "GRP_NAM" }, { "PinIndexes", "PMR_INDX" },
            { "TestNumber", "TEST_NUM" }, { "TestFlags", "TEST_FLG" }, { "ParametricFlags", "PARM_FLG" }, { "Result", "RESULT" }, { "TestText", "TEST_TXT" }, { "AlarmId", "ALARM_ID" }, { "OptionalFlags", "OPT_FLAG" }, { "ResultScalingExponent", "RES_SCAL" }, { "LowLimitScalingExponent", "LLM_SCAL" }, { "HighLimitScalingExponent", "HLM_SCAL" }, { "LowLimit", "LO_LIMIT" }, { "HighLimit", "HI_LIMIT" }, { "Units", "UNITS" }, { "ResultFormatString", "C_RESFMT" }, { "LowLimitFormatString", "C_LLMFMT" }, { "HighLimitFormatString", "C_HLMFMT" }, { "LowSpecLimit", "LO_SPEC" }, { "HighSpecLimit", "HI_SPEC" },
            { "PartFlag", "PART_FLG" }, { "TestCount", "NUM_TEST" }, { "HardBin", "HARD_BIN" }, { "SoftBin", "SOFT_BIN" }, { "XCoordinate", "X_COORD" }, { "YCoordinate", "Y_COORD" }, { "TestTime", "TEST_T" }, { "PartId", "PART_ID" }, { "PartText", "PART_TXT" },
            { "TestType", "TEST_TYP" }, { "ExecutedCount", "EXEC_CNT" }, { "FailedCount", "FAIL_CNT" }, { "AlarmCount", "ALRM_CNT" }, { "TestName", "TEST_NAM" }, { "SequencerName", "SEQ_NAME" }, { "TestLabel", "TEST_LBL" }, { "TestMin", "TEST_MIN" }, { "TestMax", "TEST_MAX" }, { "TestSum", "TST_SUMS" }, { "TestSumOfSquares", "TST_SQRS" },
            { "BinNumber", "HBIN_NUM" }, { "BinCount", "HBIN_CNT" }, { "BinName", "HBIN_NAM" }, { "BinPassFail", "HBIN_PF" }, { "PartCount", "PART_CNT" }, { "RetestCount", "RTST_CNT" }, { "AbortCount", "ABRT_CNT" }, { "GoodCount", "GOOD_CNT" }, { "FunctionalCount", "FUNC_CNT" }, { "FinishTime", "FINISH_T" }
        };

        internal static TreeNode[] BuildTree(IEnumerable<RecordEntry> records, string filter)
        {
            var roots = new List<TreeNode>();
            var active = new Dictionary<string, TreeNode>();
            foreach (var entry in records)
            {
                var record = entry.Record;
                bool include = string.IsNullOrEmpty(filter) || filter == "全部類型" || (entry.Name ?? record.GetType().Name) == filter;
                if (record is Pir pir)
                {
                    var branch = new TreeNode("Part H" + (pir.HeadNumber ?? 1) + "/S" + (pir.SiteNumber ?? 1));
                    roots.Add(branch);
                    if (include) branch.Nodes.Add(CreateNode(entry));
                    active[Key(pir.HeadNumber, pir.SiteNumber)] = branch;
                }
                else if (record is Ptr ptr)
                {
                    if (!include) continue;
                    TreeNode branch;
                    var node = CreateNode(entry);
                    if (active.TryGetValue(Key(ptr.HeadNumber, ptr.SiteNumber), out branch)) branch.Nodes.Add(node);
                    else roots.Add(node);
                }
                else if (record is Prr prr)
                {
                    TreeNode branch;
                    if (active.TryGetValue(Key(prr.HeadNumber, prr.SiteNumber), out branch))
                    {
                        if (include) branch.Nodes.Add(CreateNode(entry));
                        if (!string.IsNullOrWhiteSpace(prr.PartId)) branch.Text = "Part " + prr.PartId + " (H" + (prr.HeadNumber ?? 1) + "/S" + (prr.SiteNumber ?? 1) + ")";
                        active.Remove(Key(prr.HeadNumber, prr.SiteNumber));
                    }
                    else if (include) roots.Add(CreateNode(entry));
                }
                else if (include) roots.Add(CreateNode(entry));
            }
            return roots.Where(n => n.Nodes.Count > 0 || n.Tag is RecordEntry).ToArray();
        }

        internal static List<RecordFieldRow> BuildFields(RecordEntry entry, string filePath)
        {
            var record = entry.Record;
            var rows = new List<RecordFieldRow>();
            rows.Add(new RecordFieldRow("REC_TYP", "Record type", record.RecordType.Type.ToString(CultureInfo.InvariantCulture), ""));
            rows.Add(new RecordFieldRow("REC_SUB", "Record subtype", record.RecordType.Subtype.ToString(CultureInfo.InvariantCulture), ""));
            var layouts = record.GetType().GetCustomAttributes(typeof(FieldLayoutAttribute), true).Cast<FieldLayoutAttribute>().OrderBy(a => a.FieldIndex).ToArray();
            foreach (var layout in layouts)
            {
                var arrayLayout = layout as ArrayFieldLayoutAttribute;
                if (arrayLayout != null && !string.IsNullOrEmpty(arrayLayout.RecordProperty))
                {
                    var arrayProperty = record.GetType().GetProperty(arrayLayout.RecordProperty, BindingFlags.Public | BindingFlags.Instance);
                    var values = arrayProperty == null ? null : arrayProperty.GetValue(record, null) as Array;
                    string countCode = record is Sdr ? "SITE_CNT" : record is Pgr ? "INDX_CNT" : ToCode(arrayLayout.RecordProperty) + "_CNT";
                    rows.Add(new RecordFieldRow(countCode, countCode, values == null ? "未提供／預設" : values.Length.ToString(CultureInfo.InvariantCulture), "由欄位陣列長度推得"));
                }
                if (record is Tsr && layout.FieldIndex == 10)
                {
                    string flagsNote;
                    var flags = ReadRawTsrFlags(filePath, entry.Offset, record.StdfFile == null ? Endian.Unknown : record.StdfFile.Endian, out flagsNote);
                    rows.Add(new RecordFieldRow("OPT_FLAG", "OptionalFlags", flags.HasValue ? flags.Value.ToString(CultureInfo.InvariantCulture) : "未提供／預設", flagsNote));
                    continue;
                }
                if (string.IsNullOrEmpty(layout.RecordProperty)) continue;
                var property = record.GetType().GetProperty(layout.RecordProperty, BindingFlags.Public | BindingFlags.Instance);
                if (property == null) continue;
                object value = property.GetValue(record, null);
                string code; if (!Codes.TryGetValue(property.Name, out code)) code = ToCode(property.Name);
                if (record is Hbr) code = code.Replace("SBIN_", "HBIN_");
                if (record is Sbr) code = code.Replace("HBIN_", "SBIN_");
                if (record is Tsr && property.Name == "TestTime") code = "TEST_TIM";
                string note = "";
                var ptr = record as Ptr;
                if (ptr != null && property.Name == "Result" && ptr.Result.HasValue && ptr.ResultScalingExponent.HasValue && ptr.ResultScalingExponent.Value != 0)
                    note = "縮放後：" + FormatNumber(ptr.Result.Value * Math.Pow(10, ptr.ResultScalingExponent.Value));
                if (ptr != null && property.Name == "Result" && (ptr.TestFlags & 0x02) != 0)
                    note = "無效量測；TEST_FLG=0x" + ptr.TestFlags.ToString("X2", CultureInfo.InvariantCulture);
                if (record is Mir && property.Name == "SupervisorName") note = "Excel 欄名：SUITE_NAM";
                if (ptr != null && property.Name == "Result" && (!ptr.Result.HasValue || (ptr.TestFlags & 0x02) != 0))
                {
                    var raw = ReadRawPtrResult(filePath, entry.Offset, record.StdfFile == null ? Endian.Unknown : record.StdfFile.Endian);
                    if (raw.HasValue) value = raw.Value;
                    else note = (string.IsNullOrEmpty(note) ? "" : note + "；") + "原始 RESULT 未保留；來源讀取失敗";
                }
                rows.Add(new RecordFieldRow(code, property.Name, FormatValue(value), note));
                if (record is Mir && property.Name == "SupervisorName") rows.Add(new RecordFieldRow("SUITE_NAM", property.Name, FormatValue(value), "Excel 欄名；STDF 標準碼為 SUPR_NAM"));
            }
            if (record is UnknownRecord) rows.Add(new RecordFieldRow("RAW_HEX", "Raw content", FormatValue(((UnknownRecord)record).Content), "未知記錄原始位元組"));
            return rows;
        }

        internal static string FormatValue(object value)
        {
            if (value == null || (value is string && string.IsNullOrWhiteSpace((string)value))) return "未提供／預設";
            if (value is float) return ((float)value).ToString("0.########E+0", CultureInfo.InvariantCulture);
            if (value is double) return ((double)value).ToString("0.################E+0", CultureInfo.InvariantCulture);
            if (value is decimal) return ((decimal)value).ToString("G29", CultureInfo.InvariantCulture);
            if (value is DateTime) return ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            if (value is byte[]) return BitConverter.ToString((byte[])value).Replace("-", " ");
            if (value is Array) return string.Join(", ", ((Array)value).Cast<object>().Select(FormatValue));
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static string FormatNumber(double value) { return value.ToString("0.################E+0", CultureInfo.InvariantCulture); }
        private static float? ReadRawPtrResult(string filePath, long offset, Endian endian)
        {
            byte[] payload;
            if (!TryReadPayload(filePath, offset, endian, 15, 10, 12, out payload)) return null;
            var bytes = new byte[4]; Array.Copy(payload, 8, bytes, 0, 4);
            if ((endian == Endian.Big && BitConverter.IsLittleEndian) || (endian == Endian.Little && !BitConverter.IsLittleEndian)) Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }
        private static byte? ReadRawTsrFlags(string filePath, long offset, Endian endian, out string note)
        {
            note = "原始 OPT_FLAG 無法讀取或記錄標頭不符";
            byte[] payload;
            if (!TryReadPayload(filePath, offset, endian, 10, 30, 19, out payload)) return null;
            int position = 19;
            for (int i = 0; i < 3; i++)
            {
                if (position >= payload.Length) { note = "原始紀錄未提供 OPT_FLAG"; return null; }
                int length = payload[position++];
                if (position + length > payload.Length) { note = "原始紀錄不完整，無法定位 OPT_FLAG"; return null; }
                position += length;
            }
            if (position >= payload.Length) { note = "原始紀錄未提供 OPT_FLAG"; return null; }
            note = "原始位元組";
            return payload[position];
        }
        private static bool TryReadPayload(string filePath, long offset, Endian endian, byte recordType, byte recordSubtype, int minimumLength, out byte[] payload)
        {
            payload = null;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath) || endian == Endian.Unknown || offset < 0) return false;
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    stream.Position = offset;
                    var header = new byte[4]; if (!ReadExact(stream, header)) return false;
                    ushort length = endian == Endian.Little ? (ushort)(header[0] | header[1] << 8) : (ushort)(header[0] << 8 | header[1]);
                    if (header[2] != recordType || header[3] != recordSubtype || length < minimumLength || offset + 4L + length > stream.Length) return false;
                    payload = new byte[length]; return ReadExact(stream, payload);
                }
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (ArgumentOutOfRangeException) { return false; }
        }
        private static bool ReadExact(Stream stream, byte[] bytes)
        {
            int position = 0;
            while (position < bytes.Length)
            {
                int count = stream.Read(bytes, position, bytes.Length - position);
                if (count == 0) return false;
                position += count;
            }
            return true;
        }
        private static string ToCode(string name) { return System.Text.RegularExpressions.Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2").ToUpperInvariant(); }
        private static string Key(byte? head, byte? site) { return (head ?? 1) + ":" + (site ?? 1); }

        private static TreeNode CreateNode(RecordEntry entry)
        {
            var record = entry.Record;
            string text = entry.Name ?? record.GetType().Name;
            if (record is Mir) text += "  LOT_ID=" + Show(((Mir)record).LotId);
            else if (record is Ptr)
            {
                var p = (Ptr)record;
                text += "  " + Show(p.TestText ?? ("TEST_NUM " + p.TestNumber));
                if ((p.TestFlags & 0x3f) != 0 || (p.ParametricFlags & 0x07) != 0) text += "（無效量測）";
            }
            else if (record is Prr) text += "  PartId=" + Show(((Prr)record).PartId);
            else if (record is Tsr) text += "  " + Show(((Tsr)record).TestName ?? ("TEST_NUM " + ((Tsr)record).TestNumber));
            else text += "  " + record.RecordType;
            return new TreeNode(text) { Tag = entry, ToolTipText = text + "  Offset=" + entry.Offset };
        }

        private static string Show(string value) { return string.IsNullOrWhiteSpace(value) ? "未提供" : value; }
    }
}
