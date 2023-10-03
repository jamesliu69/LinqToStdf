// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;

namespace Stdf.Attributes
{
	/// <summary>
	///     Indicates that the field is a character array.  The result will be a
	///     <see cref="string" />. If a length is provided, it will be used,
	///     rather than the first byte of the field.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class StringFieldLayoutAttribute : FieldLayoutAttribute
	{
		public StringFieldLayoutAttribute()
		{
			base.FieldType = typeof(string);
			MissingValue   = string.Empty;
		}

		/// <summary>
		///     Indicates the length of the string.
		///     If not specified, the string will be a self-length string.
		/// </summary>
		public int Length { get; set; } = int.MinValue;

		/// <summary>
		///     Indicates the padding character to be used for a fixed-length string.
		/// </summary>
		public char PadCharacter { get; private set; } = ' ';

		/// <summary>
		///     Overriden to be locked to string. setting is an invalid operation.
		/// </summary>
		public override Type FieldType
		{
			get => base.FieldType;
			set
			{
				if(value != typeof(string))
				{
					throw new InvalidOperationException(Resources.StringFieldLayoutNonString);
				}
			}
		}
	}
}