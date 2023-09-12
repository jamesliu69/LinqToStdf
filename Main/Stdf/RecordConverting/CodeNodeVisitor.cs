// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Linq;

namespace Stdf.RecordConverting
{
	internal abstract class CodeNodeVisitor
	{
		public CodeNode Visit(CodeNode node) => node.Accept(this);

		public virtual CodeNode VisitInitializeRecord(InitializeRecordNode node) => node;

		public virtual CodeNode VisitEnsureCompat(EnsureCompatNode node) => node;

		public virtual CodeNode VisitInitReaderNode(InitReaderNode node) => node;

		public virtual CodeNode VisitTryFinallyNode(TryFinallyNode node)
		{
			CodeNode tryNode     = Visit(node.TryNode);
			CodeNode finallyNode = Visit(node.FinallyNode);

			if((tryNode == node.TryNode) && (finallyNode == node.FinallyNode))
			{
				return node;
			}
			return new TryFinallyNode(tryNode, finallyNode);
		}

		public virtual CodeNode VisitDisposeReader(DisposeReaderNode node) => node;

		public virtual CodeNode VisitBlock(BlockNode node) => new BlockNode(from n in node.Nodes select Visit(n));

		public virtual CodeNode VisitFieldAssignmentBlock(FieldAssignmentBlockNode node) => node;

		public virtual CodeNode VisitReturnRecord(ReturnRecordNode node) => node;

		public virtual CodeNode VisitSkipRawBytes(SkipRawBytesNode node) => node;

		public virtual CodeNode VisitSkipType(SkipTypeNode node) => node;

		public virtual CodeNode VisitReadFixedString(ReadFixedStringNode node) => node;

		public virtual CodeNode VisitReadType(ReadTypeNode node) => node;

		public virtual CodeNode VisitFieldAssignment(FieldAssignmentNode node)
		{
			CodeNode visitedReadNode          = Visit(node.ReadNode);
			CodeNode visitedConditionalsBlock = Visit(node.AssignmentBlock);

			if((visitedReadNode == node.ReadNode) && (visitedConditionalsBlock == node.AssignmentBlock))
			{
				return node;
			}
			return new FieldAssignmentNode(node.Type, node.FieldIndex, visitedReadNode, visitedConditionalsBlock as BlockNode ?? new BlockNode(visitedConditionalsBlock));
		}

		public virtual CodeNode VisitSkipAssignmentIfFlagSet(SkipAssignmentIfFlagSetNode node) => node;

		public virtual CodeNode VisitSkipAssignmentIfMissingValue(SkipAssignmentIfMissingValueNode node) => node;

		public virtual CodeNode VisitAssignFieldToProperty(AssignFieldToPropertyNode node) => node;

		public virtual CodeNode VisitSkipArrayAssignmentIfLengthIsZero(SkipArrayAssignmentIfLengthIsZeroNode node) => node;

		//unconverter node visiting
		public virtual CodeNode VisitUnconverterShell(UnconverterShellNode node)
		{
			CodeNode visitedBlock = Visit(node.Block);

			if(visitedBlock == node.Block)
			{
				return node;
			}
			return new UnconverterShellNode(visitedBlock as BlockNode ?? new BlockNode(visitedBlock));
		}

		public virtual CodeNode VisitCreateFieldLocalForWriting(CreateFieldLocalForWritingNode node) => node;

		public virtual CodeNode VisitWriteField(WriteFieldNode node) => throw

			//TODO: do this right;
			new NotSupportedException("WriteFieldNodes are too complicated to transform during visiting. :)");

		public virtual CodeNode VisitWriteFixedString(WriteFixedStringNode node)
		{
			CodeNode visited = Visit(node.ValueSource);

			if(visited == node.ValueSource)
			{
				return node;
			}
			return new WriteFixedStringNode(node.StringLength, visited);
		}

		public virtual CodeNode VisitWriteType(WriteTypeNode node)
		{
			CodeNode visited = Visit(node.ValueSource);

			if(visited == node.ValueSource)
			{
				return node;
			}
			return new WriteTypeNode(node.Type, visited);
		}

		public virtual CodeNode VisitLoadMissingValue(LoadMissingValueNode node) => node;

		public virtual CodeNode VisitLoadFieldLocal(LoadFieldLocalNode node) => node;

		public virtual CodeNode VisitLoadNull(LoadNullNode node) => node;

		public virtual CodeNode VisitThrowInvalidOperation(ThrowInvalidOperationNode node) => node;

		public virtual CodeNode VisitValidateSharedLengthLocal(ValidateSharedLengthLocalNode node) => node;

		public virtual CodeNode VisitSetLengthLocal(SetLengthLocalNode node) => node;
	}
}