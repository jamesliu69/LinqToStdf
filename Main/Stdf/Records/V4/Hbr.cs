// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
namespace Stdf.Records.V4
{
	public class Hbr : BinSummaryRecord
	{
		public override RecordType RecordType { get => new RecordType(1, 40); }

		public override BinType BinType { get => BinType.Hard; }
	}
}