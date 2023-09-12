// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using Stdf.Records;
using Stdf.Records.V4;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Stdf
{
    /// <summary>
    ///     Provides a number of built-in record filters.
    ///     These can be added via <see cref="StdfFile.AddFilter" />.
    /// </summary>
    public static class BuiltInFilters
	{
        /// <summary>
        ///     This filter does nothing. (and has no n-based overhead)
        /// </summary>
        /// <remarks>
        ///     This is useful if you need to return a <see cref="RecordFilter" />,
        ///     from a method, but it doesn't always need to do anything.
        /// </remarks>
        public static RecordFilter IdentityFilter { get => input => input; }

        /// <summary>
        ///     This filter implements caching for the StdfFile.
        ///     It is internal because it is controlled via an option on StdfFile.
        /// </summary>
        internal static RecordFilter CachingFilter { get => new CachingFilterImpl().Filter; }

        /// <summary>
        ///     Reconstructs any missing "head 255" summary hbrs from site-specific hbrs.
        /// </summary>
        public static RecordFilter MissingHbrSummaryFilter { get => new MissingBinSummaryFilterImpl<Hbr>().Filter; }

        /// <summary>
        ///     Reconstructs any missing "head 255" summary sbrs from site-specific sbrs.
        /// </summary>
        public static RecordFilter MissingSbrSummaryFilter { get => new MissingBinSummaryFilterImpl<Sbr>().Filter; }

        /// <summary>
        ///     Reconstructs a missing "head 255" summary pcrs from site-specific pcrs.
        /// </summary>
        public static RecordFilter MissingPcrSummaryFilter { get => new MissingPcrSummaryFilterImpl().Filter; }

        /// <summary>
        ///     Reconstructs any missing "head 255" summary tsrs from site-specific tsrs.
        /// </summary>
        public static RecordFilter MissingTsrSummaryFilter { get => new MissingTsrSummaryFilterImpl().Filter; }

        /// <summary>
        ///     Reconstructs any missing "head 255" bin summaries (hbr/sbr) from the site-specific records.
        /// </summary>
        public static RecordFilter MissingBinSummaryFilter { get => MissingHbrSummaryFilter.Chain(MissingSbrSummaryFilter); }

        /// <summary>
        ///     Reconstructs any missing "head 255" summaries (hbr/sbr/pcr/tsr) from the site-specific records.
        /// </summary>
        public static RecordFilter MissingSummaryFilter { get => MissingPcrSummaryFilter.Chain(MissingBinSummaryFilter.Chain(MissingTsrSummaryFilter)); }

        /// <summary>
        ///     If any format errors are encountered, this will throw
        /// </summary>
        internal static RecordFilter ThrowOnFormatError { get => ThrowOnFormatErrorFilter; }

        /// <summary>
        ///     Enforces the record ordering rules of the V4 spec.  It will push V4ContentErrorRecord's through the stream
        ///     for any violations.
        /// </summary>
        public static RecordFilter V4ContentSpec { get => V4ContentSpecFilter; }

        /// <summary>
        ///     Will throw if any V4ContentErrorRecords are encountered.
        /// </summary>
        public static RecordFilter ThrowOnV4ContentError { get => ThrowOnV4ContentErrorFilter; }

        /// <summary>
        ///     Will inject an mrr at the end of the stream if there wasn't one.
        /// </summary>
        /// <remarks>
        ///     This is useful for making sure that other synthesized records
        ///     that trigger off of MRR actually occur.
        /// </remarks>
        public static RecordFilter RepairMissingMrr { get => RepairMissingMrrImpl; }

        /// <summary>
        ///     Populates the "optional" PTR fields with the defaults provided by the first PTR record for the test.
        /// </summary>
        /// <remarks>
        ///     See the V4 STDF spec section on PTRs, "Notes on Specific Fields", "Default Data"
        /// </remarks>
        public static RecordFilter PopulatePtrFieldsWithDefaults { get => PopulatePtrFieldsWithDefaultsImpl; }

        /// <summary>
        ///     This filter will invoke the <see cref="StdfFile.RewindAndSeek">"rewind and seek"</see> functionality
        ///     if any unknown records are encountered.
        /// </summary>
        public static RecordFilter ExpectOnlyKnownRecords { get => ExpectOnlyKnownRecordsImpl; }

		private static IEnumerable<StdfRecord> ThrowOnFormatErrorFilter(IEnumerable<StdfRecord> input)
		{
			foreach(StdfRecord r in input)
			{
				if(r is FormatErrorRecord err)
				{
					throw err.ToException();
				}
				yield return r;
			}
		}

		private static IEnumerable<StdfRecord> ThrowOnV4ContentErrorFilter(IEnumerable<StdfRecord> input)
		{
			foreach(StdfRecord r in input)
			{
				if(r is V4ContentErrorRecord err)
				{
					throw err.ToException();
				}
				yield return r;
			}
		}

#region RepairMissingMrr implementation

		//TODO: decide whether this should react to a special ErrorRecord, or EndOfStream
		// This boils down to whether spec violations should be repaired before or after validation.
		// Up to this point, repairs have not been the result of violations, so this is the first case.
		private static IEnumerable<StdfRecord> RepairMissingMrrImpl(IEnumerable<StdfRecord> input)
		{
			Mrr mrr = null;

			foreach(StdfRecord r in input)
			{
				if(r.GetType() == typeof(Mrr))
				{
					mrr = (Mrr)r;
				}
				else if((r.GetType() == typeof(EndOfStreamRecord)) && (mrr == null))
				{
					yield return new Mrr {
						Synthesized = true,
						Offset      = r.Offset,
                    };
				}
				yield return r;
			}
		}

#endregion

#region ExpectNoUnknownRecords implementation

		private static IEnumerable<StdfRecord> ExpectOnlyKnownRecordsImpl(IEnumerable<StdfRecord> records)
		{
			foreach(StdfRecord r in records)
			{
				if(r.GetType() == typeof(UnknownRecord))
				{
					r.StdfFile.RewindAndSeek();
				}
				else
				{
					yield return r;
				}
			}
		}

#endregion

#region PopulatePtrFieldsWithDefaults implementation

		private static IEnumerable<StdfRecord> PopulatePtrFieldsWithDefaultsImpl(IEnumerable<StdfRecord> records)
		{
			Dictionary<uint, Ptr> firstPtrs = new Dictionary<uint, Ptr>();

			foreach(StdfRecord r in records)
			{
				if(r.GetType() == typeof(Ptr))
				{
					Ptr ptr = (Ptr)r;

					if(firstPtrs.TryGetValue(ptr.TestNumber, out Ptr first))
					{
						if(ptr.ResultScalingExponent == null)
						{
							ptr.ResultScalingExponent = first.ResultScalingExponent;
						}

						if(ptr.LowLimitScalingExponent == null)
						{
							ptr.LowLimitScalingExponent = first.LowLimitScalingExponent;
						}

						if(ptr.HighLimitScalingExponent == null)
						{
							ptr.HighLimitScalingExponent = first.HighLimitScalingExponent;
						}

						if(ptr.LowLimit == null)
						{
							ptr.LowLimit = first.LowLimit;
						}

						if(ptr.HighLimit == null)
						{
							ptr.HighLimit = first.HighLimit;
						}

						if(ptr.Units == null)
						{
							ptr.Units = first.Units;
						}

						if(ptr.ResultFormatString == null)
						{
							ptr.ResultFormatString = first.ResultFormatString;
						}

						if(ptr.LowLimitFormatString == null)
						{
							ptr.LowLimitFormatString = first.LowLimitFormatString;
						}

						if(ptr.HighLimitFormatString == null)
						{
							ptr.HighLimitFormatString = first.HighLimitFormatString;
						}

						if(ptr.LowSpecLimit == null)
						{
							ptr.LowSpecLimit = first.LowSpecLimit;
						}

						if(ptr.HighSpecLimit == null)
						{
							ptr.HighSpecLimit = first.HighSpecLimit;
						}
					}
					else
					{
						firstPtrs[ptr.TestNumber] = ptr;
					}
				}
				yield return r;
			}
		}

#endregion

#region CachingFilter implementation

        /// <summary>
        ///     Provides the implementation for the caching filter
        /// </summary>
        private class CachingFilterImpl
		{
			private bool _Caching;
            /// <summary>
            ///     The cached records
            /// </summary>
            private List<StdfRecord> _Records;

            /// <summary>
            ///     Caches the records provided by input and passes them through.
            ///     Subsequent calls return the contents of the cache
            /// </summary>
            public IEnumerable<StdfRecord> Filter(IEnumerable<StdfRecord> input)
			{
				if(_Caching)
				{
					throw new InvalidOperationException(Resources.CachingReEntrancy);
				}

				//cache the records
				if(_Records == null)
				{
					_Caching = true;
					_Records = new List<StdfRecord>();

					foreach(StdfRecord r in input)
					{
						_Records.Add(r);
					}
					_Caching = false;
				}

				//provide the cached records
				foreach(StdfRecord r in _Records)
				{
					yield return r;
				}
			}
		}

#endregion

		//generics are great, BTW

#region MissingBinSummaryFilter implementation

        /// <summary>
        ///     Provides the implementation for synthesizing summary records from
        ///     the site-specific records.
        /// </summary>
        /// <typeparam name="T">The kind of <see cref="BinSummaryRecord" /> to provide.</typeparam>
        private class MissingBinSummaryFilterImpl<T> where T : BinSummaryRecord, new()
		{
            /// <summary>
            ///     The list of bin records
            /// </summary>
            private readonly List<T> _Brs = new List<T>();
            /// <summary>
            ///     Indicates whether summary records are already in place
            /// </summary>
            private bool _FoundSummary;

            /// <summary>
            ///     Passes through the records provided by input,
            ///     taking note of the bin records.  If no summary records
            ///     are found, they are synthesized and passed through before the mrr.
            /// </summary>
            public IEnumerable<StdfRecord> Filter(IEnumerable<StdfRecord> input)
			{
				foreach(StdfRecord r in input)
				{
					if(r.GetType() == typeof(T))
					{
						T br = (T)r;
						_Brs.Add(br);

						if(br.HeadNumber == 255)
						{
							_FoundSummary = true;
						}
					}
					else if((r.GetType() == typeof(Mrr)) && !_FoundSummary)
					{
						foreach(StdfRecord gen in GenerateSummaries(r.Offset))
						{
							yield return gen;
						}
					}
					yield return r;
				}
			}

            /// <summary>
            ///     Generates the summary records
            /// </summary>
            private IEnumerable<StdfRecord> GenerateSummaries(long offset)
			{
				IEnumerable<T> q = from b in _Brs
								   group b by b.BinNumber into g
								   select new T {
									   Synthesized = true,
									   Offset      = offset,
									   HeadNumber  = 255,
									   SiteNumber  = 0,
									   BinNumber   = g.Key,
									   BinName     = g.First().BinName,
									   BinPassFail = g.First().BinPassFail,
									   BinCount    = (uint)g.Sum(b => b.BinCount),
								   };

				foreach(T b in q)
				{
					yield return b;
				}
			}
		}

#endregion

#region MissingPcrSummaryFilter implementation

		private class MissingPcrSummaryFilterImpl
		{
			private Pcr _Summary = new Pcr {
				Synthesized = true,
				HeadNumber  = 255,
				SiteNumber  = 0,
			};

			public IEnumerable<StdfRecord> Filter(IEnumerable<StdfRecord> input)
			{
				foreach(StdfRecord r in input)
				{
					if((r.GetType() == typeof(Pcr)) && (_Summary != null))
					{
						Pcr p = (Pcr)r;

						if(p.HeadNumber == 255)
						{
							_Summary = null;
						}
						else
						{
							_Summary.AbortCount      =  _Summary.AbortCount.Combine(p.AbortCount);
							_Summary.FunctionalCount =  _Summary.FunctionalCount.Combine(p.FunctionalCount);
							_Summary.GoodCount       =  _Summary.GoodCount.Combine(p.GoodCount);
							_Summary.RetestCount     =  _Summary.RetestCount.Combine(p.RetestCount);
							_Summary.PartCount       += p.PartCount;
						}
					}
					else if((r.GetType() == typeof(Mrr)) && (_Summary != null))
					{
						_Summary.Offset = r.Offset;
						yield return _Summary;
					}
					yield return r;
				}
			}
		}

#endregion

#region MissingTsrSummaryFilter implementation

		private class MissingTsrSummaryFilterImpl
		{
			private readonly List<Tsr> _Tsrs = new List<Tsr>();
			private          bool      _FoundSummary;

			public IEnumerable<StdfRecord> Filter(IEnumerable<StdfRecord> input)
			{
				foreach(StdfRecord r in input)
				{
					if(r.GetType() == typeof(Tsr))
					{
						Tsr tsr = (Tsr)r;
						_Tsrs.Add(tsr);

						if(tsr.HeadNumber == 255)
						{
							_FoundSummary = true;
						}
					}
					else if((r.GetType() == typeof(Mrr)) && !_FoundSummary)
					{
						foreach(StdfRecord gen in GenerateSummaries(r.Offset))
						{
							yield return gen;
						}
					}
					yield return r;
				}
			}

			private IEnumerable<StdfRecord> GenerateSummaries(long offset)
			{
				IEnumerable<Tsr> q = from t in _Tsrs
									 group t by t.TestNumber into g
									 select new Tsr {
										 Synthesized      = true,
										 Offset           = offset,
										 HeadNumber       = 255,
										 SiteNumber       = 0,
										 TestNumber       = g.Key,
										 TestName         = g.First().TestName,
										 TestLabel        = g.First().TestLabel,
										 AlarmCount       = (uint?)g.Sum(t => t.AlarmCount),
										 ExecutedCount    = (uint?)g.Sum(t => t.ExecutedCount),
										 FailedCount      = (uint?)g.Sum(t => t.FailedCount),
										 SequencerName    = g.First().SequencerName,
										 TestMax          = g.Max(t => t.TestMax),
										 TestMin          = g.Min(t => t.TestMin),
										 TestSum          = g.Sum(t => t.TestSum),
										 TestSumOfSquares = g.Sum(t => t.TestSumOfSquares),
										 TestTime         = g.Sum(t => t.TestTime),
										 TestType         = g.First().TestType,
									 };

				foreach(Tsr b in q)
				{
					yield return b;
				}
			}
		}

#endregion

#region V4ContentSpec implementation

        /// <summary>
        ///     node in the state machine representation
        /// </summary>
        private class RecordState
		{
			public string                 Message = Resources.V4ContentState_Unknown;
			public List<RecordState>      Routes;
			public Func<StdfRecord, bool> ShouldTransition;
		}

        /// <summary>
        ///     These records are not allowed after the initial sequence, or before the Mrr
        /// </summary>
        private static readonly HashSet<RuntimeTypeHandle> _InitialSequenceSet = new HashSet<RuntimeTypeHandle> {
			typeof(Far).TypeHandle,
			typeof(Atr).TypeHandle,
			typeof(Mir).TypeHandle,
			typeof(Rdr).TypeHandle,
			typeof(Sdr).TypeHandle,
			typeof(EndOfStreamRecord).TypeHandle,
        };

        /// <summary>
        ///     Uses a state machine to enforce the V4 content spec (initial sequence and mrr at the end)
        /// </summary>
        private static IEnumerable<StdfRecord> V4ContentSpecFilter(IEnumerable<StdfRecord> input)
		{
#region States

			// Build up the various states that describe the V4 content spec.

			RecordState eofState = new RecordState {
				Message          = Resources.V4ContentState_AtEOF,
				ShouldTransition = r => r.GetType() == typeof(EndOfStreamRecord),
				Routes           = new List<RecordState>(), //we'd better never get here.
			};

			RecordState mrrState = new RecordState {
				Message          = Resources.V4ContentState_AfterMrr,
				ShouldTransition = r => r.GetType() == typeof(Mrr),
				Routes = new List<RecordState> {
					eofState,
                }, //we only expect EOF from here
			};

			RecordState bodyState = new RecordState {
				Message = Resources.V4ContentState_StdfBody,

				//anything that's not in the initial sequence (or EOS)
				ShouldTransition = r => !_InitialSequenceSet.Contains(r.GetType().TypeHandle),
			};

			bodyState.Routes = new List<RecordState> {
				mrrState,
				bodyState,
            };

			RecordState sdrState = new RecordState {
				Message          = Resources.V4ContentState_AfterSdr,
				ShouldTransition = r => r.GetType() == typeof(Sdr),
			};

			sdrState.Routes = new List<RecordState> {
				sdrState,
				bodyState,
            };

			RecordState rdrState = new RecordState {
				Message          = Resources.V4ContentState_AfterRdr,
				ShouldTransition = r => r.GetType() == typeof(Rdr),
				Routes = new List<RecordState> {
					sdrState,
					bodyState,
                },
			};

			RecordState mirState = new RecordState {
				Message          = Resources.V4ContentState_AfterMir,
				ShouldTransition = r => r.GetType() == typeof(Mir),
				Routes = new List<RecordState> {
					rdrState,
					sdrState,
					bodyState,
                },
			};

			RecordState atrState = new RecordState {
				Message          = Resources.V4ContentState_AfterAtr,
				ShouldTransition = r => r.GetType() == typeof(Atr),
			};

			atrState.Routes = new List<RecordState> {
				atrState,
				mirState,
            };

			RecordState farState = new RecordState {
				Message          = Resources.V4ContentState_AfterFar,
				ShouldTransition = r => r.GetType() == typeof(Far),
				Routes = new List<RecordState> {
					atrState,
					mirState,
                },
			};

			RecordState sofState = new RecordState {
				Message          = Resources.V4ContentState_AtSOF,
				ShouldTransition = r => r.GetType() == typeof(StartOfStreamRecord),
				Routes = new List<RecordState> {
					farState,
                },
			};

#endregion

			//we'll start in a pre-far state
			RecordState currentState = new RecordState {
				Message = Resources.V4ContentState_BeforeSOF,
				Routes = new List<RecordState> {
					sofState,
                },
			};

			foreach(StdfRecord r in input)
			{
				bool transitioned = false;

				foreach(RecordState state in currentState.Routes)
				{
					if(state.ShouldTransition(r))
					{
						transitioned = true;
						currentState = state;
						break;
					}
				}

				//TODO: does IsWritable prevent informational and error records from violating the content spec (we want that)?
				if(!transitioned && r.IsWritable)
				{
					yield return new V4ContentErrorRecord {
						Offset  = r.Offset,
						Message = string.Format(Resources.InitialSequenceError, r.GetType().Name, currentState.Message),
                    };
				}
				yield return r;
			}
		}

#endregion
	}
}