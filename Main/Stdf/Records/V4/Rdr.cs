// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using Stdf.Attributes;

namespace Stdf.Records.V4
{
	[FieldLayout(FieldIndex = 0, FieldType = typeof(ushort)), ArrayFieldLayout(FieldIndex = 1, FieldType = typeof(ushort), MissingValue = ushort.MinValue, ArrayLengthFieldIndex = 0, RecordProperty = "RetestBins")]
	public class Rdr : StdfRecord
	{
		public override RecordType RecordType { get => new RecordType(1, 70); }

		public ushort[] RetestBins { get; set; }
	}
}