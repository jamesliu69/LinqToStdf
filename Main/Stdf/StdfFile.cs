// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using Stdf.CompiledQuerySupport;
using Stdf.Indexing;
using Stdf.Records;
using Stdf.Records.V4;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
#if SILVERLIGHT
using System.Windows.Controls;
#endif

namespace Stdf
{
	/// <summary>
	///     The delegate type for methods that add filtering capability to an StdfRecord stream.
	/// </summary>
	/// <param name="input"></param>
	/// <returns></returns>
	public delegate IEnumerable<StdfRecord> RecordFilter(IEnumerable<StdfRecord> input);

	/// <summary>
	///     The main instantiation point for using the library.
	/// </summary>
	/// <remarks>
	///     <para>
	///         This is THE starting point for the library.
	///         You construct an StdfFile over a path, and then
	///         you get access to all the records via a basic
	///         IEnumerable pattern, or via some of the Linq-like
	///         extensions to IRecordContext.
	///     </para>
	///     <para>
	///         By default, StdfFile will be preloaded with converters
	///         suitable for parsing STDF V4 files.
	///     </para>
	/// </remarks>
	public sealed partial class StdfFile : IRecordContext
	{
		internal static readonly RecordConverterFactory _V4ConverterFactory = new RecordConverterFactory();
		private readonly         object                 _ISLock             = new object();
		private readonly         IStdfStreamManager     _StreamManager;

		private bool              _FiltersLocked;
		private IIndexingStrategy _IndexingStrategy;

		//indicates we are in seek mode
		private bool _InSeekMode;

		private RecordFilter         _RecordFilter;
		private RewindableByteStream _Stream;

		private bool _ThrowOnFormatError = true;

		/// <summary>
		///     Static constructor that will prepare a converter factory
		///     suitable for parsing STDF V4 records.
		/// </summary>
		static StdfFile() => StdfV4Specification.RegisterRecords(_V4ConverterFactory);

		/// <summary>
		///     Constructs an StdfFile for the given path, suitable
		///     for parsing STDF V4 files.
		/// </summary>
		/// <param name="path">The path the the STDF file</param>
		public StdfFile(string path) : this(new DefaultFileStreamManager(path), false)
		{
		}

		/// <summary>
		///     Constructs an StdfFile for the given path, suitable
		///     for parsing STDF V4 files.
		/// </summary>
		/// <param name="path">The path the the STDF file</param>
		/// <param name="debug">
		///     True if you want the converter factory
		///     to emit to a dynamic assembly suitable for debugging IL generation.
		/// </param>
		public StdfFile(string path, bool debug) : this(new DefaultFileStreamManager(path), debug)
		{
		}

		/// <summary>
		///     Constructs an StdfFile for the given path, suitable
		///     for parsing STDF V4 files.
		/// </summary>
		/// <param name="streamManager">The STDF stream manager to use</param>
		public StdfFile(IStdfStreamManager streamManager) : this(streamManager, false)
		{
		}

		/// <summary>
		///     Constructs an StdfFile for the given path, suitable
		///     for parsing STDF V4 files.
		/// </summary>
		/// <param name="streamManager">The STDF stream manager to use</param>
		/// <param name="debug">
		///     True if you want the converter factory
		///     to emit to a dynamic assembly suitable for debugging IL generation.
		/// </param>
		public StdfFile(IStdfStreamManager streamManager, bool debug) : this(streamManager, debug, null)
		{
		}

		internal StdfFile(IStdfStreamManager streamManager, bool debug, RecordsAndFields recordsAndFields) : this(streamManager, PrivateImpl.None)
		{
			if(debug || (recordsAndFields != null))
			{
				ConverterFactory = new RecordConverterFactory(recordsAndFields) {
					Debug = debug,
				};
				StdfV4Specification.RegisterRecords(ConverterFactory);
			}
			else
			{
				ConverterFactory = new RecordConverterFactory(_V4ConverterFactory);
			}
		}

		internal StdfFile(IStdfStreamManager streamManager, RecordConverterFactory rcf) : this(streamManager, PrivateImpl.None) => ConverterFactory = rcf;

		private StdfFile(IStdfStreamManager streamManager, PrivateImpl _)
		{
			_StreamManager = streamManager;
			_RecordFilter  = BuiltInFilters.IdentityFilter;
		}

		/// <summary>
		///     Exposes the ConverterFactory in use for parsing.
		///     This allows record un/converters to be registered.
		/// </summary>
		public RecordConverterFactory ConverterFactory { get; }

		/// <summary>
		///     Exposes the Endianness of the file.  Will be "uknown" until
		///     after parsing has begun.
		/// </summary>
		public Endian Endian { get; private set; } = Endian.Unknown;

		public long? ExpectedLength { get; private set; }

		public IIndexingStrategy IndexingStrategy
		{
			get
			{
				//TODO: get this locking pattern right
				if(_IndexingStrategy == null)
				{
					lock(_ISLock)
					{
						if(_IndexingStrategy == null)
						{
							_IndexingStrategy = new SimpleIndexingStrategy();
						}
					}
				}
				return _IndexingStrategy;
			}
			set
			{
				//TODO: prevent this from changing
				lock(_ISLock)
				{
					EnsureFiltersUnlocked();
					_IndexingStrategy = value;
				}
			}
		}

		/// <summary>
		///     Indicates whether the library should throw in the case of
		///     a format error. (default=true)
		///     This can only be set before parsing has begun.
		/// </summary>
		/// <remarks>
		///     <para>
		///         This allows both tolerant and strict applications
		///         to be written using the same library.
		///     </para>
		///     <para>
		///         If set to false, a <see cref="Records.FormatErrorRecord" /> will be
		///         pushed through the stream instead of throwing an exception,
		///         and if possible, parsing will continue.
		///         However, at this time, most format exceptions are unrecoverable.
		///         In the future, the parser might be more intelligent in the way it
		///         handles issues.
		///     </para>
		/// </remarks>
		public bool ThrowOnFormatError
		{
			get => _ThrowOnFormatError;
			set
			{
				EnsureFiltersUnlocked();
				_ThrowOnFormatError = value;
			}
		}

#region IRecordContext Members

		/// <summary>
		///     Implementation of <see cref="IRecordContext.StdfFile" /> to enable
		///     the extension methods. (returns this)
		/// </summary>
		StdfFile IRecordContext.StdfFile { get => this; }

#endregion

		private void EnsureFiltersUnlocked()
		{
			if(_FiltersLocked)
			{
				throw new InvalidOperationException(Resources.ErrorFiltersLocked);
			}
		}

		/// <summary>
		///     Adds a record filter to the stream.
		///     Note, if caching is enabled, filters will only be run on the
		///     first time through the file.  If filtering needs to occur
		///     all the time, turn off caching, or apply the filter to the results
		///     of the call to <see cref="GetRecords" />.
		/// </summary>
		public void AddFilter(RecordFilter filter)
		{
			EnsureFiltersUnlocked();
			_RecordFilter = _RecordFilter.Chain(filter);
		}

		private IEnumerable<StdfRecord> SetStdfFile(IEnumerable<StdfRecord> records)
		{
			foreach(StdfRecord r in records)
			{
				r.StdfFile = this;
				yield return r;
			}
		}

		private RecordFilter GetBaseRecordFilter()
		{
			RecordFilter filter = SetStdfFile;
			return _ThrowOnFormatError ? filter.Chain(BuiltInFilters.ThrowOnFormatError) : filter;
		}

		private RecordFilter GetTopRecordFilter() => IndexingStrategy.CacheRecords;

		public IQueryable<StdfRecord> GetRecords() => new Queryable<StdfRecord>(GetRecordsEnumerable(), IndexingStrategy.TransformQuery);

		/// <summary>
		///     Gets all the records in the file as a "stream" of StdfRecord object
		/// </summary>
		public IEnumerable<StdfRecord> GetRecordsEnumerable()
		{
			_FiltersLocked = true;

			foreach(StdfRecord record in GetTopRecordFilter()(_RecordFilter(GetBaseRecordFilter()(InternalGetAllRecords()))))
			{
				if(record.GetType() == typeof(StartOfStreamRecord))
				{
					StartOfStreamRecord sosRecord = (StartOfStreamRecord)record;
					sosRecord.FileName = _StreamManager.Name;

					if(Endian == Endian.Unknown)
					{
						Endian = sosRecord.Endian;
					}
					ExpectedLength = sosRecord.ExpectedLength;
				}
				yield return record;
			}
		}

		/// <summary>
		///     Consumes all the records in the StdfFile
		/// </summary>
		/// <remarks>
		///     This is useful if you have a set of filters set up, but no need for iterating
		///     through the records themselves at the end of the chain.
		/// </remarks>
		/// <returns>The count of records consumed at the end of the chain.</returns>
		public int Consume() => GetRecords().Count();

		private IEnumerable<StdfRecord> InternalGetAllRecords()
		{
			//set this in case the last time we ended in seek mode
			_InSeekMode = false;

			using(IStdfStreamScope streamScope = _StreamManager.GetScope())
			{
				try
				{
					_Stream = new RewindableByteStream(streamScope.Stream);

					//read the FAR to get endianness
					Endian endian = Endian.Little;
					byte[] far    = new byte[6];

					if(_Stream.Read(far, 6) < 6)
					{
						yield return new StartOfStreamRecord {
							Endian         = Endian.Unknown,
							ExpectedLength = _Stream.Length,
						};

						yield return new FormatErrorRecord {
							Message     = Resources.FarReadError,
							Recoverable = false,
						};
						yield return new EndOfStreamRecord();
						yield break;
					}
					endian = far[4] < 2 ? Endian.Big : Endian.Little;
					byte stdfVersion = far[5];
					byte length      = endian == Endian.Little ? far[0] : far[1];

					if(length != 2)
					{
						yield return new StartOfStreamRecord {
							Endian         = endian,
							ExpectedLength = _Stream.Length,
						};

						yield return new FormatErrorRecord {
							Message     = Resources.FarLengthError,
							Recoverable = false,
						};

						yield return new EndOfStreamRecord {
							Offset = 2,
						};
						yield break;
					}

					//validate record type
					if(far[2] != 0)
					{
						yield return new StartOfStreamRecord {
							Endian         = endian,
							ExpectedLength = _Stream.Length,
						};

						yield return new FormatErrorRecord {
							Offset      = 2,
							Message     = Resources.FarRecordTypeError,
							Recoverable = false,
						};

						yield return new EndOfStreamRecord {
							Offset = 6,
						};
						yield break;
					}

					//validate record type
					if(far[3] != 10)
					{
						yield return new StartOfStreamRecord {
							Endian         = endian,
							ExpectedLength = _Stream.Length,
						};

						yield return new FormatErrorRecord {
							Offset      = 3,
							Message     = Resources.FarRecordSubTypeError,
							Recoverable = false,
						};

						yield return new EndOfStreamRecord {
							Offset = 3,
						};
						yield break;
					}

					//OK we're satisfied, let's go
					yield return new StartOfStreamRecord {
						Endian         = endian,
						ExpectedLength = _Stream.Length,
					};

					yield return new Far {
						CpuType     = far[4],
						StdfVersion = far[5],
					};

					//flush the memory
					_Stream.Flush();

					//now we have the FAR out of the way, and we can blow through the rest.
					while(true)
					{
						if(_InSeekMode)
						{
							_Stream.RewindAll();
							int backup = -1;

							//create the callback algorithms use to indicate the found something
							void BackupCallback(int bytes) => backup = bytes;
							int corruptOffset = _Stream.Offset;

							//set up the seek algorithms and consume the sequence.
							IEnumerable<byte> algorithm = _SeekAlgorithm(_Stream.ReadAsByteSequence(), endian, BackupCallback);
							algorithm.Count();

							//when we get here, one of the algorithms has found the record stream,
							//or we went to the end of the stream
							bool recoverable = false;

							if(backup != -1)
							{
								//someone found where we need to be, backup the number of bytes they suggest
								_Stream.Rewind(backup);
								recoverable = true;
							}

							//spit out the corrupt data
							yield return new CorruptDataRecord {
								CorruptData = _Stream.DumpDataToCurrentOffset(),
								Offset      = corruptOffset,
								Recoverable = recoverable,
							};

							//the data's gone out the door, so flush it
							_Stream.Flush();

							if(!recoverable)
							{
								//we got to the end without finding anything
								//spit out a format error
								yield return new FormatErrorRecord {
									Message     = Resources.EOFInSeekMode,
									Recoverable = false,
									Offset      = _Stream.Offset,
								};

								yield return new EndOfStreamRecord {
									Offset = _Stream.Offset,
								};
								yield break;
							}
							_InSeekMode = false;
						}
						int position = _Stream.Offset;

						//read a record header
						RecordHeader? header = _Stream.ReadHeader(endian);

						//null means we hit EOS
						if(header == null)
						{
							if(!_Stream.PastEndOfStream)
							{
								//Something's wrong. We know the offset is rewound
								//to the begining of the header.  If there's still
								//data, we're corrupt
								yield return new CorruptDataRecord {
									Offset = position,

									//TODO: leverage the data in the stream.
									//we know we've hit the end, so we can just dump
									//the remaining memoized data
									CorruptData = _Stream.DumpRemainingData(),
									Recoverable = false,
								};

								yield return new FormatErrorRecord {
									Message     = Resources.EOFInHeader,
									Recoverable = false,
									Offset      = position,
								};
							}

							yield return new EndOfStreamRecord {
								Offset = _Stream.Offset,
							};
							yield break;
						}
						byte[] contents = new byte[header.Value.Length];
						int    read     = _Stream.Read(contents, contents.Length);

						if(read < contents.Length)
						{
							//rewind to the beginning of the record (read bytes + the header)
							_Stream.Rewind(_Stream.Offset - position);

							yield return new CorruptDataRecord {
								Offset      = position,
								CorruptData = _Stream.DumpRemainingData(),
								Recoverable = false,
							};

							yield return new FormatErrorRecord {
								Message     = Resources.EOFInRecordContent,
								Recoverable = false,
								Offset      = position,
							};
						}
						else
						{
							UnknownRecord ur = new UnknownRecord(header.Value.RecordType, contents, endian) {
								Offset = position,
							};
							StdfRecord r = ConverterFactory.Convert(ur);

							if(r.GetType() != typeof(UnknownRecord))
							{
								//it converted, so update our last known position
								//TODO: We should think about:
								//* how to indicate corruption within the record boundaries
								//* enabling filteres to set the last known offset (to allow valid unknown records to pass through)
								//  * This could possible be done by allowing filters access to Flush or the dump functionality.
								_Stream.Flush();
							}
							r.Offset = position;
							yield return r;
						}
					}
				}
				finally
				{
					//set stream to null so we're not holding onto it
					_Stream = null;
				}
			}
		}

		private enum PrivateImpl
		{
			None = 0,
		}

		internal abstract class Queryable
		{
			public abstract IEnumerable Enumerable { get; }
			public abstract Expression  Expression { get; }

			internal static IQueryable Create(Type elementType, IEnumerable sequence, Func<Expression, Expression> transform) => (IQueryable)Activator.CreateInstance(typeof(Queryable<>).MakeGenericType(elementType), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new object[] { sequence, transform }, null);

			internal static IQueryable Create(Type elementType, Expression expression, Func<Expression, Expression> transform) => (IQueryable)Activator.CreateInstance(typeof(Queryable<>).MakeGenericType(elementType), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new object[] { expression, transform }, null);
		}

		internal class Queryable<T> : Queryable, IOrderedQueryable<T>, IQueryProvider
		{
			private readonly Expression                   _Expression;
			private readonly Func<Expression, Expression> _ExpressionTransform;
			private          EnumerableQuery<T>           _EnumerableQuery;

			private Queryable(Func<Expression, Expression> expressionTransform) => _ExpressionTransform = expressionTransform;

			public Queryable(IEnumerable<T> enumerable, Func<Expression, Expression> expressionTransform) : this(expressionTransform)
			{
				_EnumerableQuery = new EnumerableQuery<T>(enumerable);
				_Expression      = Expression.Constant(this);
			}

			public Queryable(Expression expression, Func<Expression, Expression> expressionTransform) : this(expressionTransform) => _Expression = expression;

#region IEnumerable<T> Members

			public IEnumerator<T> GetEnumerator() => GetEnumeratorInternal();

#endregion

#region IEnumerable Members

			IEnumerator IEnumerable.GetEnumerator() => GetEnumeratorInternal();

#endregion

			private IEnumerator<T> GetEnumeratorInternal()
			{
				if(_EnumerableQuery == null)
				{
					_EnumerableQuery = new EnumerableQuery<T>(new QueryableRewriter().Visit(_ExpressionTransform(_Expression)));
				}
				return ((IEnumerable<T>)_EnumerableQuery).GetEnumerator();
			}

			private class QueryableRewriter : ExpressionVisitor
			{
				protected override Expression VisitConstant(ConstantExpression node)
				{
					if(!(node.Value is Queryable queryable))
					{
						return base.VisitConstant(node);
					}

					if(queryable.Enumerable != null)
					{
						//TODO: get public type?
						return base.VisitConstant(Expression.Constant(queryable.Enumerable));
					}
					return Visit(queryable.Expression);
				}
			}

#region IQueryable Members

			public Type ElementType { get => typeof(T); }

			public override Expression Expression { get => _Expression; }

			public override IEnumerable Enumerable { get => _EnumerableQuery; }

			public IQueryProvider Provider { get => this; }

#endregion

#region IQueryProvider Members

			public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new Queryable<TElement>(expression, _ExpressionTransform);

			public IQueryable CreateQuery(Expression expression)
			{
				Type type = TypeHelper.FindGenericType(typeof(IQueryable<>), expression.Type);

				if(type == null)
				{
					throw new ArgumentException("Could not find type", "expression");
				}
				return Create(type.GetGenericArguments()[0], expression, _ExpressionTransform);
			}

			public TResult Execute<TResult>(Expression expression)
			{
				EnumerableQuery<T> queryable = _EnumerableQuery;

				if(queryable == null)
				{
					queryable = new EnumerableQuery<T>(_Expression);
				}
				return ((IQueryProvider)queryable).Execute<TResult>(new QueryableRewriter().Visit(_ExpressionTransform(expression)));
			}

			public object Execute(Expression expression)
			{
				EnumerableQuery<T> queryable = _EnumerableQuery;

				if(queryable == null)
				{
					queryable = new EnumerableQuery<T>(_Expression);
				}
				return ((IQueryProvider)queryable).Execute(new QueryableRewriter().Visit(_ExpressionTransform(expression)));
			}

#endregion
		}

#region Rewind and Seek APIs

		private void EnterSeekMode() => _InSeekMode = true;

		/// <summary>
		///     Rewinds the underlying stream to the just after the last record conversion
		///     that yielded a known record and enters "seek" mode, where the stream will
		///     be assumed corrupt and searched for byte sequences that will identify the
		///     record boundarie. When the record stream is re-aquired, a CorruptDataRecord
		///     will be emitted containing the bytes between the last known good record and
		///     the newly-aquired record stream.
		///     Must be called within the context of live record reading.
		/// </summary>
		public void RewindAndSeek() => EnterSeekMode();

		private SeekAlgorithm _SeekAlgorithm = SeekAlgorithms.Identity;

		/// <summary>
		///     Adds a <see cref="SeekAlgorithm" /> to be used if <see cref="RewindAndSeek()" /> is called.
		/// </summary>
		/// <param name="algorithm"></param>
		public void AddSeekAlgorithm(SeekAlgorithm algorithm) => _SeekAlgorithm = _SeekAlgorithm.Chain(algorithm);

#endregion
	}
}