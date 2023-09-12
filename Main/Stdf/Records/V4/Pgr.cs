// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using Stdf.Attributes;

namespace Stdf.Records.V4
{
	[FieldLayout(FieldIndex = 0, FieldType = typeof(ushort), RecordProperty = "GroupIndex"), StringFieldLayout(FieldIndex = 1, IsOptional = true, RecordProperty = "GroupName"), FieldLayout(FieldIndex = 2, FieldType = typeof(ushort), IsOptional = true), ArrayFieldLayout(FieldIndex = 3, FieldType = typeof(ushort), IsOptional = true, ArrayLengthFieldIndex = 2, RecordProperty = "PinIndexes")]
	public class Pgr : StdfRecord
	{
		public override RecordType RecordType { get => new RecordType(1, 62); }

		/// <summary>
		///     While ushort, valid PGR Indexes must be 32,768 - 65,535
		/// </summary>
		public ushort GroupIndex { get;   set; }
		public string   GroupName  { get; set; }
		public ushort[] PinIndexes { get; set; }
	}
}