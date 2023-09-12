// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
namespace Stdf.Records.V4
{
	public class Eps : StdfRecord
	{
		public override RecordType RecordType { get => new RecordType(20, 20); }
	}
}