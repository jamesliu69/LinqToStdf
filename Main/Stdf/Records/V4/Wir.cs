// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using Stdf.Attributes;
using System;

namespace Stdf.Records.V4
{
	[FieldLayout(FieldIndex = 0, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "HeadNumber"), FieldLayout(FieldIndex = 1, FieldType = typeof(byte), MissingValue = byte.MaxValue, RecordProperty = "SiteGroup"), TimeFieldLayout(FieldIndex = 2, FieldType = typeof(DateTime), RecordProperty = "StartTime"), StringFieldLayout(FieldIndex = 3, IsOptional = true, RecordProperty = "WaferId")]
	public class Wir : StdfRecord, IHeadIndexable
	{
		public override RecordType RecordType { get => new RecordType(2, 10); }

		public byte?     SiteGroup { get; set; }
		public DateTime? StartTime { get; set; }
		public string    WaferId   { get; set; }

		public byte? HeadNumber { get; set; }
	}
}