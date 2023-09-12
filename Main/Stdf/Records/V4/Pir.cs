// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using Stdf.Attributes;

namespace Stdf.Records.V4
{
	[FieldLayout(FieldIndex = 0, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "HeadNumber"), FieldLayout(FieldIndex = 1, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "SiteNumber")]
	public class Pir : StdfRecord, IHeadSiteIndexable
	{
		public override RecordType RecordType { get => new RecordType(5, 10); }

		public byte? HeadNumber { get; set; }
		public byte? SiteNumber { get; set; }
	}
}