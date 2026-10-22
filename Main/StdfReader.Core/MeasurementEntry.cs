namespace StdfReader.Core
{
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
        public string GroupKey { get { return TestNumber + "|" + HeadNumber + "|" + SiteNumber + "|" + (Units ?? ""); } }
        public string DisplayName { get { return (TestText ?? "Test") + " (" + TestNumber + ") [H" + HeadNumber + "/S" + SiteNumber + "] " + Units; } }
    }
}
