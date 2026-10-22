using System;
using System.Collections.Generic;
namespace StdfReader.Core
{
    public sealed class StdfDocument
    {
        public string FilePath { get; set; }
        public bool IsComplete { get; set; }
        public List<string> Diagnostics { get; } = new List<string>();
        public List<RecordEntry> Records { get; } = new List<RecordEntry>();
        public List<PartEntry> Parts { get; } = new List<PartEntry>();
        public List<MeasurementEntry> Measurements { get; } = new List<MeasurementEntry>();
        public string LotId { get; set; }
        public DateTime? StartTimeUtc { get; set; }
        public DateTime? EndTimeUtc { get; set; }
        public int RawRecordCount { get; set; }
        public Dictionary<string, int> RecordCounts { get; } = new Dictionary<string, int>();
    }
}
