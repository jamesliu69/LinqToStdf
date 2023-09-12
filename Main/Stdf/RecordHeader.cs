﻿// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
namespace Stdf
{
	/// <summary>
	///     Encapsulates an STDF record header
	/// </summary>
	public struct RecordHeader
	{
		/// <summary>
		///     Constructs a new record header
		/// </summary>
		public RecordHeader(ushort length, RecordType recordType)
		{
			Length     = length;
			RecordType = recordType;
		}

		/// <summary>
		///     The length of the record
		/// </summary>
		public ushort Length { get; }

		/// <summary>
		///     The <see cref="RecordType" /> of the record.
		/// </summary>
		public RecordType RecordType { get; }
	}
}