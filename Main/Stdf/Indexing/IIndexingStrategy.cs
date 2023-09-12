﻿// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Stdf.Indexing
{
	public interface IIndexingStrategy
	{
		IEnumerable<StdfRecord> CacheRecords(IEnumerable<StdfRecord> records);
		Expression              TransformQuery(Expression            query);
	}

	public class NonCachingStrategy : IIndexingStrategy
	{
#region IIndexingStrategy Members

		public IEnumerable<StdfRecord> CacheRecords(IEnumerable<StdfRecord> records) => records;

		public Expression TransformQuery(Expression query) => query;

#endregion
	}

	public abstract class CachingIndexingStrategy : IIndexingStrategy
	{
		private bool _Cached;

		private         bool       _Caching;
		public abstract Expression TransformQuery(Expression query);

		IEnumerable<StdfRecord> IIndexingStrategy.CacheRecords(IEnumerable<StdfRecord> records)
		{
			if(_Caching)
			{
				throw new InvalidOperationException(Resources.CachingReEntrancy);
			}

			//cache the records
			if(!_Cached)
			{
				_Caching = true;
				IndexRecords(records);
				_Caching = false;
				_Cached  = true;
			}

			//provide the cached records
			return EnumerateIndexedRecords();
		}

		public abstract void                    IndexRecords(IEnumerable<StdfRecord> records);
		public abstract IEnumerable<StdfRecord> EnumerateIndexedRecords();
	}

	public class SimpleIndexingStrategy : CachingIndexingStrategy
	{
		private List<StdfRecord> _Records;

		public override Expression TransformQuery(Expression query) => query;

		public override void IndexRecords(IEnumerable<StdfRecord> records)
		{
			_Records = new List<StdfRecord>();

			foreach(StdfRecord r in records)
			{
				_Records.Add(r);
			}
		}

		public override IEnumerable<StdfRecord> EnumerateIndexedRecords()
		{
			foreach(StdfRecord r in _Records)
			{
				yield return r;
			}
		}
	}
}