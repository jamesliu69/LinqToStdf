// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using Stdf.Attributes;
using System;

namespace Stdf.Records.V4
{
	[TimeFieldLayout(FieldIndex = 0, RecordProperty = "ModifiedTime"), StringFieldLayout(FieldIndex = 1, RecordProperty = "CommandLine")]
	public class Atr : StdfRecord
	{
		public override RecordType RecordType { get => new RecordType(0, 20); }

		public DateTime? ModifiedTime { get; set; }
		public string    CommandLine  { get; set; }
	}
}