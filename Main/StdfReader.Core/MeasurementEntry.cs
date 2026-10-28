namespace StdfReader.Core
{
    /// <summary>單筆 PTR 量測結果；由 StdfDocumentLoader 套用同測試編號的預設值與 10^exponent 縮放後產生。</summary>
    public sealed class MeasurementEntry
    {
        public long Sequence { get; set; }
        public uint TestNumber { get; set; }
        public byte? HeadNumber { get; set; }
        public byte? SiteNumber { get; set; }
        public int? PartIndex { get; set; }
        public string TestText { get; set; }
        public string Units { get; set; }
        public double? Value { get; set; }
        public double? LowLimit { get; set; }
        public double? HighLimit { get; set; }
        public string Status { get; set; }
        public string InvalidReason { get; set; }
        public byte TestFlags { get; set; }
        public byte ParametricFlags { get; set; }
        public byte? OptionalFlags { get; set; }
        public sbyte? ResultScalingExponent { get; set; }
        public sbyte? LowLimitScalingExponent { get; set; }
        public sbyte? HighLimitScalingExponent { get; set; }
        public bool UnitsInherited { get; set; }
        public bool ResultScalingInherited { get; set; }
        public bool LowLimitInherited { get; set; }
        public bool HighLimitInherited { get; set; }
        /// <summary>分組鍵："TestNumber|HeadNumber|SiteNumber|Units"，供相同測項/站點的量測歸為同一組。</summary>
        public string GroupKey { get { return TestNumber + "|" + HeadNumber + "|" + SiteNumber + "|" + (Units ?? ""); } }
        /// <summary>顯示名稱："測試文字 (編號) [Hhead/Ssite] 單位"。</summary>
        public string DisplayName { get { return (TestText ?? "Test") + " (" + TestNumber + ") [H" + HeadNumber + "/S" + SiteNumber + "] " + Units; } }
    }
}
