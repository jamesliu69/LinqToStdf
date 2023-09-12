// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Stdf.RecordConverting
{
	internal class InitializeRecordNode : CodeNode
	{
		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitInitializeRecord(this);
	}

	internal class EnsureCompatNode : CodeNode
	{
		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitEnsureCompat(this);
	}

	internal class InitReaderNode : CodeNode
	{
		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitInitReaderNode(this);
	}

	internal class TryFinallyNode : CodeNode
	{
		public TryFinallyNode(CodeNode tryNode, CodeNode finallyNode)
		{
			TryNode     = tryNode;
			FinallyNode = finallyNode;
		}

		public CodeNode TryNode     { get; private set; }
		public CodeNode FinallyNode { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitTryFinallyNode(this);
	}

	internal class DisposeReaderNode : CodeNode
	{
		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitDisposeReader(this);
	}

	internal class BlockNode : CodeNode
	{
		public BlockNode(params CodeNode[] nodes) : this((IEnumerable<CodeNode>)nodes)
		{
		}

		public BlockNode(IEnumerable<CodeNode> nodes) => Nodes = nodes.ToList();

		public List<CodeNode> Nodes { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitBlock(this);
	}

	internal class FieldAssignmentBlockNode : CodeNode
	{
		public FieldAssignmentBlockNode(BlockNode node) => Block = node;

		public BlockNode Block { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitFieldAssignmentBlock(this);
	}

	internal class ReturnRecordNode : CodeNode
	{
		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitReturnRecord(this);
	}

	internal class SkipRawBytesNode : CodeNode
	{
		public SkipRawBytesNode(int bytes) => Bytes = bytes;

		public int Bytes { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitSkipRawBytes(this);
	}

	internal class SkipTypeNode : CodeNode
	{
		public SkipTypeNode(Type type)
		{
			if(type.IsArray)
			{
				throw new InvalidOperationException("SkipTypeNode on an array type must be constructed with a length index.");
			}
			Type = type;
		}

		public SkipTypeNode(Type type, int lengthIndex, bool isNibble = false)
		{
			if(!type.IsArray)
			{
				throw new InvalidOperationException("SkipTypeNode on an non-array type can't be constructed with a length index.");
			}
			Type        = type;
			LengthIndex = lengthIndex;

			if(isNibble && (type != typeof(byte)))
			{
				throw new InvalidOperationException("Nibble arrays can only be read into byte arrays.");
			}
			IsNibble = isNibble;
		}

		public int? LengthIndex { get; private set; }
		public bool IsNibble    { get; private set; }
		public Type Type        { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitSkipType(this);
	}

	internal class ReadFixedStringNode : CodeNode
	{
		public ReadFixedStringNode(int length) => Length = length;

		public int Length { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitReadFixedString(this);
	}

	internal class ReadTypeNode : CodeNode
	{
		public ReadTypeNode(Type type)
		{
			if(type.IsArray)
			{
				throw new InvalidOperationException("ReadTypeNode on an array type must be constructed with a length index.");
			}
			Type = type;
		}

		public ReadTypeNode(Type type, int lengthIndex, bool isNibble = false)
		{
			if(!type.IsArray)
			{
				throw new InvalidOperationException("ReadTypeNode on an non-array type can't be constructed with a length index.");
			}
			Type        = type;
			LengthIndex = lengthIndex;

			if(isNibble && (type != typeof(byte[])))
			{
				throw new InvalidOperationException("Nibble arrays can only be read into byte arrays.");
			}
			IsNibble = isNibble;
		}

		public int? LengthIndex { get; private set; }
		public bool IsNibble    { get; private set; }
		public Type Type        { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitReadType(this);
	}

	internal class FieldAssignmentNode : CodeNode
	{
		public FieldAssignmentNode(Type type, int index, CodeNode readNode, BlockNode assignmentBlock)
		{
			Type            = type;
			FieldIndex      = index;
			ReadNode        = readNode;
			AssignmentBlock = assignmentBlock;
		}

		public Type      Type            { get; private set; }
		public int       FieldIndex      { get; private set; }
		public CodeNode  ReadNode        { get; private set; }
		public BlockNode AssignmentBlock { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitFieldAssignment(this);
	}

	internal class SkipAssignmentIfFlagSetNode : CodeNode
	{
		public SkipAssignmentIfFlagSetNode(int flagFieldIndex, byte flagMask)
		{
			FlagFieldIndex = flagFieldIndex;
			FlagMask       = flagMask;
		}

		public int  FlagFieldIndex { get; private set; }
		public byte FlagMask       { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitSkipAssignmentIfFlagSet(this);
	}

	internal class SkipAssignmentIfMissingValueNode : CodeNode
	{
		//TODO: find out if we need to be more explicit about type, or if we can infer it from the missing value.
		public SkipAssignmentIfMissingValueNode(object missingValue) => MissingValue = missingValue;

		public object MissingValue { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitSkipAssignmentIfMissingValue(this);
	}

	internal class AssignFieldToPropertyNode : CodeNode
	{
		public AssignFieldToPropertyNode(Type fieldType, PropertyInfo property)
		{
			FieldType = fieldType;
			Property  = property;
		}

		public Type         FieldType { get; private set; }
		public PropertyInfo Property  { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitAssignFieldToProperty(this);
	}

	internal class SkipArrayAssignmentIfLengthIsZeroNode : CodeNode
	{
		public SkipArrayAssignmentIfLengthIsZeroNode(int lengthIndex) => LengthIndex = lengthIndex;

		public int LengthIndex { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitSkipArrayAssignmentIfLengthIsZero(this);
	}

	internal class ThrowInvalidOperationNode : CodeNode
	{
		public ThrowInvalidOperationNode(string message) => Message = message;

		public string Message { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitThrowInvalidOperation(this);
	}
}