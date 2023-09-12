// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;

namespace Stdf.Attributes
{
    /// <summary>
    ///     Indicates that the field is a timestamp.  The result will be a
    ///     <see cref="DateTime" />.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class TimeFieldLayoutAttribute : FieldLayoutAttribute
	{
        /// <summary>
        ///     The epoch used for STDF dates
        /// </summary>
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1);

		public TimeFieldLayoutAttribute()
		{
			base.FieldType = typeof(DateTime);
			MissingValue   = Epoch;
		}

        /// <summary>
        ///     Overriden to be locked to string. setting is an invalid operation.
        /// </summary>
        public override Type FieldType
		{
			get => base.FieldType;
			set
			{
				if(value != typeof(DateTime))
				{
					throw new InvalidOperationException(Resources.TimeFieldLayoutNonDateTime);
				}
			}
		}
	}
}