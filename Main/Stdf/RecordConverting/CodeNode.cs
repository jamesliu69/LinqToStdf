// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.

namespace Stdf.RecordConverting
{
	internal abstract class CodeNode
	{
		public abstract CodeNode Accept(CodeNodeVisitor visitor);
	}
}