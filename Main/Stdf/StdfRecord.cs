﻿// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;

namespace Stdf
{
	/// <summary>
	///     Abstract stdf record type
	/// </summary>
	public abstract class StdfRecord : IRecordContext
	{
		/// <summary>
		///     The mask used for the offset data. (we're reserving the 2 highest bits)
		/// </summary>
		private static readonly ulong _OffsetMask = 0x3fffffffffffffff;

		/// <summary>
		///     The mask used for the synthesized bit
		/// </summary>
		private static readonly ulong _SynthesizedMask = 0x8000000000000000;

		private ulong _OffsetData;

		/// <summary>
		///     The <see cref="RecordType" /> of the instance
		/// </summary>
		public abstract RecordType RecordType { get; }

		/// <summary>
		///     Indicates whether this record should be considered for persisting to a file.
		/// </summary>
		public virtual bool IsWritable { get => true; }

		/// <summary>
		///     The file/stream offset of this record's header
		/// </summary>
		public long Offset
		{
			get => (long)(_OffsetData & _OffsetMask);
			set
			{
				if(value < 0)
				{
					throw new ArgumentOutOfRangeException("value", "Offset must be >= 0");
				}

				if(value >= (long)_OffsetMask)
				{
					throw new ArgumentOutOfRangeException("value", "The offset is to large to be stored in an StdfRecord.");
				}
				_OffsetData = (~_OffsetMask & _OffsetData) | (_OffsetMask & (ulong)value);
			}
		}

		/// <summary>
		///     Indicates whether or not this record was synthesized
		/// </summary>
		public bool Synthesized { get => (_SynthesizedMask & _OffsetData) != 0; set => _OffsetData = value ? _OffsetData | _SynthesizedMask : _OffsetData & ~_SynthesizedMask; }

		/// <summary>
		///     Reference to the "owning" StdfFile.
		/// </summary>
		public StdfFile StdfFile { get; set; }
	}
}