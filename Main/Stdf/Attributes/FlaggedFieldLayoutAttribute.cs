// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;

namespace Stdf.Attributes
{
    /// <summary>
    ///     Defines an STDF field whose "null" state is set by an external bitfield byte
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class FlaggedFieldLayoutAttribute : FieldLayoutAttribute
	{
		public FlaggedFieldLayoutAttribute()
		{
			IsOptional          = true;
			PersistMissingValue = true;
		}

        /// <summary>
        ///     This indicates the bitfield byte used to determine if we have a value
        ///     for this field
        /// </summary>
        public int FlagIndex { get; set; }
        /// <summary>
        ///     This indicates the mask that is used on the bitfield to determine if we have a value.
        /// </summary>
        public byte FlagMask { get; set; }
	}
}