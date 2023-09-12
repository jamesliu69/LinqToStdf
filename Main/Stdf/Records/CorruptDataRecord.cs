﻿// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
namespace Stdf.Records
{
    /// <summary>
    ///     Identifies a chunk of "corrupted" data.
    /// </summary>
    public class CorruptDataRecord : FormatErrorRecord
	{
		public override string Message { get => base.Message ?? string.Format(Resources.CorruptDataMessage, (CorruptData ?? new byte[0]).Length, Offset); }

		public byte[] CorruptData { get; set; }
	}
}