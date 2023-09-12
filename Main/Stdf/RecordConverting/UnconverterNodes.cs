// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Reflection;

namespace Stdf.RecordConverting
{
	internal class UnconverterShellNode : CodeNode
	{
		public UnconverterShellNode(BlockNode block) => Block = block;

		public BlockNode Block { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitUnconverterShell(this);
	}

	internal class CreateFieldLocalForWritingNode : CodeNode
	{
		public CreateFieldLocalForWritingNode(int fieldIndex, Type localType)
		{
			FieldIndex = fieldIndex;
			LocalType  = localType;
		}

		public int  FieldIndex { get; private set; }
		public Type LocalType  { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitCreateFieldLocalForWriting(this);
	}

	internal class WriteFieldNode : CodeNode
	{
		public WriteFieldNode(int fieldIndex, Type fieldType, CodeNode initialization = null, PropertyInfo sourceProperty = null, CodeNode writeOperation = null, CodeNode noValueWriteContingency = null, int? optionalFieldIndex = null, byte optionalFieldMask = 0)
		{
			FieldIndex              = fieldIndex;
			FieldType               = fieldType;
			Initialization          = initialization;
			Property                = sourceProperty;
			WriteOperation          = writeOperation;
			NoValueWriteContingency = noValueWriteContingency;
			OptionalFieldIndex      = optionalFieldIndex;
			OptionaFieldMask        = optionalFieldMask;
		}

		public int          FieldIndex              { get; private set; }
		public Type         FieldType               { get; private set; }
		public CodeNode     Initialization          { get; private set; }
		public PropertyInfo Property                { get; private set; }
		public CodeNode     WriteOperation          { get; private set; }
		public CodeNode     NoValueWriteContingency { get; private set; }
		public int?         OptionalFieldIndex      { get; private set; }
		public byte         OptionaFieldMask        { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitWriteField(this);
	}

	internal class WriteFixedStringNode : CodeNode
	{
		public WriteFixedStringNode(int stringLength, CodeNode valueSource)
		{
			StringLength = stringLength;
			ValueSource  = valueSource;
		}

		public int      StringLength { get; private set; }
		public CodeNode ValueSource  { get; set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitWriteFixedString(this);
	}

	internal class WriteTypeNode : CodeNode
	{
		public WriteTypeNode(Type type, CodeNode valueSource, bool isNibble = false)
		{
			Type        = type;
			ValueSource = valueSource;

			if(isNibble && (type != typeof(byte[])))
			{
				throw new InvalidOperationException("Nibble arrays can only be read into byte arrays.");
			}
			IsNibble = isNibble;
		}

		public Type     Type        { get; private set; }
		public CodeNode ValueSource { get; set; }
		public bool     IsNibble    { get; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitWriteType(this);
	}

	internal class LoadMissingValueNode : CodeNode
	{
		public LoadMissingValueNode(object missingValue, Type type)
		{
			MissingValue = missingValue;
			Type         = type;
		}

		public object MissingValue { get; private set; }
		public Type   Type         { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitLoadMissingValue(this);
	}

	internal class LoadFieldLocalNode : CodeNode
	{
		public LoadFieldLocalNode(int fieldIndex) => FieldIndex = fieldIndex;

		public int FieldIndex { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitLoadFieldLocal(this);
	}

	internal class LoadNullNode : CodeNode
	{
		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitLoadNull(this);
	}

	internal class ValidateSharedLengthLocalNode : CodeNode
	{
		public ValidateSharedLengthLocalNode(int arrayFieldIndex, int lengthFieldIndex)
		{
			ArrayFieldIndex  = arrayFieldIndex;
			LengthFieldIndex = lengthFieldIndex;
		}

		public int ArrayFieldIndex  { get; private set; }
		public int LengthFieldIndex { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitValidateSharedLengthLocal(this);
	}

	internal class SetLengthLocalNode : CodeNode
	{
		public SetLengthLocalNode(int arrayFieldIndex, int lengthFieldIndex)
		{
			ArrayFieldIndex  = arrayFieldIndex;
			LengthFieldIndex = lengthFieldIndex;
		}

		public int ArrayFieldIndex  { get; private set; }
		public int LengthFieldIndex { get; private set; }

		public override CodeNode Accept(CodeNodeVisitor visitor) => visitor.VisitSetLengthLocal(this);
	}
}