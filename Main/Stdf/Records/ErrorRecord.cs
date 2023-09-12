// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
namespace Stdf.Records
{
    /// <summary>
    ///     Root of a hierarchy of records that represent error conditions in an STDF.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Error records can be thought of as analogous to an exception mechanism,
    ///         with the exception that the stack isn't torn down.  This allows filters
    ///         to make decisions based on error conditions.
    ///     </para>
    ///     <para>
    ///         There are several built-in filters that can provide "throw" semantics for error
    ///         records for programs that do not wish to be tolerant of error conditions.
    ///     </para>
    /// </remarks>
    public abstract class ErrorRecord : StdfRecord
	{
		protected ErrorRecord() => Synthesized = true;

		public override RecordType RecordType { get => new RecordType(); }

		public virtual string Message { get; set; }

		public override bool IsWritable { get => false; }

		public virtual StdfException ToException() => new StdfException(Message) {
			ErrorRecord = this,
        };
	}
}