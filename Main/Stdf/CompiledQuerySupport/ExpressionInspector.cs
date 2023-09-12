// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Stdf.CompiledQuerySupport
{
    /// <summary>
    ///     This is the class that inspects precompile queries and determines
    ///     which records and fields should be parsed.
    /// </summary>
    internal static class ExpressionInspector
	{
        /// <summary>
        ///     Wraps the inspection of a lambda expression for use in CompiledQuery
        /// </summary>
        public static RecordsAndFields Inspect(LambdaExpression exp) => new InspectingVisitor().InspectExpression(exp);

        /// <summary>
        ///     This visitor processes a query (in the form of a LambdaExpression) and
        ///     ensure it won't leak concrete record types and tracks the records and
        ///     fields used in the query so that it can optimize the converters.
        /// </summary>
        private class InspectingVisitor : ExpressionVisitor
		{
            /// <summary>
            ///     This is the set of types we've checked to reduce duplication
            ///     and prevent following circular references
            /// </summary>
            private readonly HashSet<Type> _CheckedTypes = new HashSet<Type>();
            /// <summary>
            ///     The records and fields used in the query
            /// </summary>
            private RecordsAndFields _RecordsAndFields;

            /// <summary>
            ///     Inspects a query, ensuring it won't leak records and calculating the
            ///     records and fields it uses.
            /// </summary>
            public RecordsAndFields InspectExpression(LambdaExpression node)
			{
				_RecordsAndFields = new RecordsAndFields();

				//first see if the node leaks any records:
				EnsureTypeWontLeakRecords(node.ReturnType);

				//visit the tree
				Visit(node);
				return _RecordsAndFields;
			}

            /// <summary>
            ///     This gets called for each member access.
            /// </summary>
            protected override Expression VisitMember(MemberExpression node)
			{
				//Get the type that declares the member
				Type type = node.Member.DeclaringType;

				//if it is an StdfRecord, track the field
				if(typeof(StdfRecord).IsAssignableFrom(type))
				{
					_RecordsAndFields.AddField(type, node.Member.Name);
				}
				return base.VisitMember(node);
			}

            /// <summary>
            ///     Throws if the type, its interfaces, or any generic parameters leak stdf records
            /// </summary>
            private void EnsureTypeWontLeakRecords(Type type)
			{
				//TODO: think about whether we need to go up the base type chain to check for interfaces.
				//This depends on a) whether we care that much, and b) whether GetFields/etc. return aggregated data.

				//if it is a primitive, we don't care about it
				if(type.IsPrimitive)
				{
					return;
				}

				//see if we've checked this type.  Return if we have.
				if(!_CheckedTypes.Add(type))
				{
					return;
				}

				//see if it's a record
				if(typeof(StdfRecord).IsAssignableFrom(type))
				{
					throw new InvalidOperationException(string.Format("The compiled query can return {0} in its object graph or inheritance chain.  A compiled query can't return StdfRecords.  Just return the data you want in a new or anonymous type.", type));
				}

				//check any generic arguments
				if(type.IsGenericType)
				{
					foreach(Type genericType in type.GetGenericArguments())
					{
						EnsureTypeWontLeakRecords(genericType);
					}
				}

				//check any interfaces
				EnsureTypesWontLeakRecords(type.GetInterfaces());

				//check any element type (for arrays, pointers, etc.)
				if(type.HasElementType)
				{
					EnsureTypeWontLeakRecords(type.GetElementType());
				}

				//check public fields/properties/methods
				EnsureTypesWontLeakRecords(from f in type.GetFields() select f.FieldType);
				EnsureTypesWontLeakRecords(from p in type.GetProperties() select p.PropertyType);
				EnsureTypesWontLeakRecords(from m in type.GetMethods() select m.ReturnType);
			}

            /// <summary>
            ///     helper that will ensure collections don't leak
            /// </summary>
            private void EnsureTypesWontLeakRecords(IEnumerable<Type> types)
			{
				foreach(Type type in types)
				{
					EnsureTypeWontLeakRecords(type);
				}
			}
		}
	}
}