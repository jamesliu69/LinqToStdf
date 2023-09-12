﻿// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;

namespace Stdf.Attributes
{
    /// <summary>
    ///     Indicates that the field is an array of the <see cref="FieldLayoutAttribute.FieldType" />.
    ///     The length is provided by another field whose index is indicated by <see cref="ArrayLengthFieldIndex" />
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class ArrayFieldLayoutAttribute : FieldLayoutAttribute
	{
		public ArrayFieldLayoutAttribute() => IsOptional = true;

        /// <summary>
        ///     Indicates the field that provides the length information for the array.
        /// </summary>
        public int ArrayLengthFieldIndex { get; set; }

        /// <summary>
        ///     Indicates that the array may be prematurely truncated by end of record.
        /// </summary>
        public bool AllowTruncation { get; set; }
	}
}