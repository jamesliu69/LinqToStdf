// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using Stdf.Attributes;

namespace Stdf.Records.V4
{
	[StringFieldLayout(FieldIndex = 0, RecordProperty = "Text")]
	public class Dtr : StdfRecord
	{
		public override RecordType RecordType { get => new RecordType(50, 30); }

		public string Text { get; set; }
	}
}