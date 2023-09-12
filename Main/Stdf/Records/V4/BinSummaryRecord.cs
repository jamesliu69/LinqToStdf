// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using Stdf.Attributes;

namespace Stdf.Records.V4
{
	[FieldLayout(FieldIndex = 0, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "HeadNumber"), FieldLayout(FieldIndex = 1, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "SiteNumber"), FieldLayout(FieldIndex = 2, FieldType = typeof(ushort), RecordProperty = "BinNumber"), FieldLayout(FieldIndex = 3, FieldType = typeof(uint), RecordProperty = "BinCount"), StringFieldLayout(FieldIndex = 4, IsOptional = true, Length = 1, MissingValue = " ", RecordProperty = "BinPassFail"), StringFieldLayout(FieldIndex = 5, IsOptional = true, RecordProperty = "BinName")]
	public abstract class BinSummaryRecord : StdfRecord, IHeadSiteIndexable
	{
		public abstract BinType BinType { get; }
        /// <summary>
        ///     While ushort, valid bins must be 0 - 32,767
        /// </summary>
        public ushort BinNumber { get; set; }
		public uint BinCount { get;    set; }
        /// <summary>
        ///     Known values are P, F
        /// </summary>
        public string BinPassFail { get; set; }
		public string BinName    { get;  set; }
		public byte?  HeadNumber { get;  set; }
		public byte?  SiteNumber { get;  set; }
	}
}