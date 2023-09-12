﻿// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;

namespace Stdf.Attributes
{
    /// <summary>
    ///     Indicates that the property identified by AssignTo is a "manual" projection of data from another field identified
    ///     by DependentOnIndex.
    ///     This is used in the compiled query support to allow the correct fields to be parsed in order to provide the data
    ///     for a given property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class DependencyProperty : FieldLayoutAttribute
	{
        /// <summary>
        ///     The index of the field whose data is projected into this property.
        /// </summary>
        public int DependentOnIndex { get; set; }
	}
}