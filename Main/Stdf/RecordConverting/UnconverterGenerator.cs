﻿// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using Stdf.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Stdf.RecordConverting
{
	internal class UnconverterGenerator
	{
		private readonly HashSet<int>                                           _FieldLocalsTouched = new HashSet<int>();
		private readonly ILGenerator                                            _ILGen;
		private readonly Type                                                   _Type;
		private          List<KeyValuePair<FieldLayoutAttribute, PropertyInfo>> _Fields;

		public UnconverterGenerator(ILGenerator ilgen, Type type)
		{
			_ILGen = ilgen ?? throw new ArgumentNullException("ilgen");
			_Type  = type  ?? throw new ArgumentNullException("type");
		}

		public void GenerateUnconverter()
		{
			_Fields = GetFieldLayoutsAndAssignments();

			UnconverterShellNode node = new UnconverterShellNode(new BlockNode(from pair in _Fields.AsEnumerable().Reverse()

																			   //don't generate code for dependency properties
																			   where !(pair.Key is DependencyProperty)

																			   //this call through reflection is icky, but marginally better than the hard-coded table
																			   //we're just binding to the generic GenerateAssignment method for the field's type
																			   select GenerateAssignment(pair)));

			new UnconverterEmittingVisitor {
				ConcreteType = _Type,
				ILGen        = _ILGen,
				EnableLog    = ConverterLog.IsLogging,
			}.Visit(node);
		}

		//TODO: refactor this so we're not duplicated with ConverterFactory
		private List<KeyValuePair<FieldLayoutAttribute, PropertyInfo>> GetFieldLayoutsAndAssignments()
		{
			//get the list
			IOrderedEnumerable<FieldLayoutAttribute> attributes = from a in _Type.GetCustomAttributes(typeof(FieldLayoutAttribute), true).Cast<FieldLayoutAttribute>() orderby a.FieldIndex select a;
			List<FieldLayoutAttribute>               list       = new List<FieldLayoutAttribute>(attributes);

			//make sure they are consecutive
			for(int i = 0; i < list.Count; i++)
			{
				if(list[i].FieldIndex != i)
				{
					throw new NonconsecutiveFieldIndexException(_Type);
				}
			}
			IEnumerable<KeyValuePair<FieldLayoutAttribute, PropertyInfo>> withPropInfo = from l in list select new KeyValuePair<FieldLayoutAttribute, PropertyInfo>(l, l.RecordProperty == null ? null : _Type.GetProperty(l.RecordProperty));
			return new List<KeyValuePair<FieldLayoutAttribute, PropertyInfo>>(withPropInfo);
		}

		private CodeNode GenerateAssignment(KeyValuePair<FieldLayoutAttribute, PropertyInfo> pair)
		{
			Type                       fieldType    = pair.Key.FieldType;
			StringFieldLayoutAttribute stringLayout = pair.Key as StringFieldLayoutAttribute;

			//if it is an array, defer to GenerateArrayAssignment
			if(pair.Key is ArrayFieldLayoutAttribute)
			{
				// TODO: Why do we need these fieldType checks at all?
				if(fieldType == typeof(string))
				{
					// TODO: Accept string arrays
					throw new InvalidOperationException(Resources.NoStringArrays);
				}

				if(fieldType == typeof(BitArray))
				{
					throw new InvalidOperationException(Resources.NoBitArrayArrays);
				}
				return GenerateArrayAssignment(pair);
			}
			List<CodeNode> initNodes       = new List<CodeNode>();
			bool           localWasPresent = true;

			if(_FieldLocalsTouched.Add(pair.Key.FieldIndex))
			{
				localWasPresent = false;

				//add a create local node
				initNodes.Add(new CreateFieldLocalForWritingNode(pair.Key.FieldIndex, pair.Key.FieldType));
				_FieldLocalsTouched.Add(pair.Key.FieldIndex);
			}

			//find out if there is an optional field flag that we need to manage
			FieldLayoutAttribute        optionalFieldLayout          = null;
			FlaggedFieldLayoutAttribute currentAsOptionalFieldLayout = pair.Key as FlaggedFieldLayoutAttribute;

			if(currentAsOptionalFieldLayout != null)
			{
				optionalFieldLayout = _Fields[currentAsOptionalFieldLayout.FlagIndex].Key;

				if(_FieldLocalsTouched.Add(currentAsOptionalFieldLayout.FlagIndex))
				{
					initNodes.Add(new CreateFieldLocalForWritingNode(currentAsOptionalFieldLayout.FlagIndex, optionalFieldLayout.FieldType));
				}
			}

			//this will hold a node that will write in the case we have no value to write
			CodeNode noValueWriteContingency = null;

			//this will hold the source of the write that will happen above
			CodeNode noValueWriteContingencySource = null;

			//Decide what to do if we don't have a value to write.
			//This will happen if we don't store the value in a property, it is "missing" from the property source, or something else
			//TODO: should these have a different precedence?
			if(pair.Key.MissingValue != null)
			{
				//if we have a missing value, set that as the write source
				noValueWriteContingencySource = new LoadMissingValueNode(pair.Key.MissingValue, fieldType);
			}
			else if(localWasPresent || (optionalFieldLayout != null))
			{
				//if the local was present when we started, that means it was initialized by another field. We can safely write it
				//Similarly, if this is marked as an optional field, we can still write whatever the value of the local is (cheat)
				noValueWriteContingencySource = new LoadFieldLocalNode(pair.Key.FieldIndex);
			}
			else if(fieldType.IsValueType)
			{
				//if T is a value type, we're up a creek with nothing to write.
				//this is obviously not a good place, so throw in the converter
				noValueWriteContingency = new ThrowInvalidOperationNode(string.Format(Resources.NonNullableField, pair.Key.FieldIndex, _Type));
			}
			else
			{
				//otherwise, we can try to write null (unless it is a fixed-length string, which should have a default instead)
				if((stringLayout != null) && (stringLayout.Length > 0))
				{
					//TODO: move this check into StdfStringLayout if we can, along with a check that the missing value length matches
					throw new NotSupportedException(Resources.FixedLengthStringMustHaveDefault);
				}
				noValueWriteContingencySource = new LoadNullNode();
			}

			//create the write node and the no-value contingency if we don't already have one
			CodeNode writeNode;

			if((stringLayout != null) && (stringLayout.Length > 0))
			{
				noValueWriteContingency = noValueWriteContingency ?? new WriteFixedStringNode(stringLayout.Length, noValueWriteContingencySource);
				writeNode               = new WriteFixedStringNode(stringLayout.Length, new LoadFieldLocalNode(pair.Key.FieldIndex));
			}
			else
			{
				noValueWriteContingency = noValueWriteContingency ?? new WriteTypeNode(fieldType, noValueWriteContingencySource);
				writeNode               = new WriteTypeNode(fieldType, new LoadFieldLocalNode(pair.Key.FieldIndex));
			}

			//return the crazy node
			//TODO: refactor this better, this sucks
			return new WriteFieldNode(pair.Key.FieldIndex, fieldType, new BlockNode(initNodes), pair.Value, writeNode, noValueWriteContingency, optionalFieldLayout == null ? null : (int?)optionalFieldLayout.FieldIndex, optionalFieldLayout == null ? (byte)0 : currentAsOptionalFieldLayout.FlagMask);
		}

		private CodeNode GenerateArrayAssignment(KeyValuePair<FieldLayoutAttribute, PropertyInfo> pair)
		{
			Type                      fieldType     = pair.Key.FieldType;
			ArrayFieldLayoutAttribute arrayLayout   = (ArrayFieldLayoutAttribute)pair.Key;
			bool                      isNibbleArray = arrayLayout is NibbleArrayFieldLayoutAttribute;
			List<CodeNode>            initNodes     = new List<CodeNode>();

			//there are no array optionals, we should always have to create the local here
			if(_FieldLocalsTouched.Add(arrayLayout.FieldIndex))
			{
				initNodes.Add(new CreateFieldLocalForWritingNode(arrayLayout.FieldIndex, fieldType.MakeArrayType()));
			}
			else
			{
				throw new InvalidOperationException("Array local was touched before we generated code for it.");
			}

			if(pair.Value == null)
			{
				throw new InvalidOperationException(Resources.ArraysMustBeAssignable);
			}
			CodeNode writeNode;

			if(_FieldLocalsTouched.Add(arrayLayout.ArrayLengthFieldIndex))
			{
				writeNode = new BlockNode(new CreateFieldLocalForWritingNode(arrayLayout.ArrayLengthFieldIndex, _Fields[arrayLayout.ArrayLengthFieldIndex].Key.FieldType), new SetLengthLocalNode(arrayLayout.FieldIndex, arrayLayout.ArrayLengthFieldIndex));
			}
			else
			{
				writeNode = new ValidateSharedLengthLocalNode(arrayLayout.FieldIndex, arrayLayout.ArrayLengthFieldIndex);
			}
			writeNode = new BlockNode(writeNode, new WriteTypeNode(fieldType.MakeArrayType(), new LoadFieldLocalNode(arrayLayout.FieldIndex), isNibbleArray));
			return new WriteFieldNode(arrayLayout.FieldIndex, fieldType.MakeArrayType(), new BlockNode(initNodes), pair.Value, writeNode);
		}
	}
}