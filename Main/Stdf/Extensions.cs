// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using Stdf.Records.V4;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Stdf
{
	/// <summary>
	///     Provides convenient shortcuts to query the structure of STDF as extension methods.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		///     Returns only records of an exact type
		/// </summary>
		/// <typeparam name="TRecord"></typeparam>
		/// <param name="source"></param>
		/// <returns></returns>
		public static IEnumerable<TRecord> OfExactType<TRecord>(this IEnumerable<StdfRecord> source) where TRecord : StdfRecord
		{
			foreach(StdfRecord r in source)
			{
				if(r.GetType() == typeof(TRecord))
				{
					yield return (TRecord)r;
				}
			}
		}

		public static IQueryable<TRecord> OfExactType<TRecord>(this IQueryable<StdfRecord> source) where TRecord : StdfRecord => source.Provider.CreateQuery<TRecord>(Expression.Call(((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(TRecord)), source.Expression));

#region Extending Wrr

		/// <summary>
		///     Gets the <see cref="Prr">Prrs</see> for this wafer.
		/// </summary>
		public static IEnumerable<Prr> GetPrrs(this Wrr wrr) => from prr in wrr.StdfFile.GetRecords().OfExactType<Prr>() where prr.HeadNumber == wrr.HeadNumber select prr;

#endregion

		/// <summary>
		///     Combines two uint?'s with the sematics desirable
		///     for record summarizing.
		/// </summary>
		/// <param name="first">The first nullable uint</param>
		/// <param name="second">The second nullable uint</param>
		/// <returns>The addition of first and second where null is treated as 0.</returns>
		public static uint? Combine(this uint? first, uint? second)
		{
			switch(first)
			{
				case null when second == null:
					return null;
				case null:
					return second;
			}

			if(second == null)
			{
				return first;
			}
			return first + second;
		}

		/// <summary>
		///     Chains 2 <see cref="RecordFilter" />s together
		/// </summary>
		/// <param name="first">The first filter</param>
		/// <param name="other">The filter to chain to the first</param>
		public static RecordFilter Chain(this RecordFilter first, RecordFilter other) => input => other(first(input));

		/// <summary>
		///     Chains 2 <see cref="SeekAlgorithm" />s together
		/// </summary>
		/// <param name="first">The first algorithm</param>
		/// <param name="other">The algorithm to chain to the first</param>
		public static SeekAlgorithm Chain(this SeekAlgorithm first, SeekAlgorithm other) => (input, endian, callback) => other(first(input, endian, callback), endian, callback);

#region extending IRecordContext

		/// <summary>
		///     Gets the <see cref="Mir" /> for the record context.
		/// </summary>
		public static Mir GetMir(this IRecordContext record) => (from mir in record.StdfFile.GetRecords().OfExactType<Mir>() select mir).FirstOrDefault();

		/// <summary>
		///     Gets the <see cref="Mrr" /> for the record context.
		/// </summary>
		public static Mrr GetMrr(this IRecordContext record) => (from mrr in record.StdfFile.GetRecords().OfExactType<Mrr>() select mrr).FirstOrDefault();

		/// <summary>
		///     Gets the <see cref="Pcr">Pcrs</see> for the record context.
		/// </summary>
		public static IEnumerable<Pcr> GetPcrs(this IRecordContext record) => record.StdfFile.GetRecords().OfExactType<Pcr>();

		/// <summary>
		///     Gets the <see cref="Pcr">Pcrs</see> for the record context
		///     with the given head and site.
		/// </summary>
		public static IEnumerable<Pcr> GetPcrs(this IRecordContext record, byte headNumber, byte siteNumber) => from r in record.StdfFile.GetRecords().OfExactType<Pcr>() where (r.HeadNumber == headNumber) && (r.SiteNumber == siteNumber) select r;

		/// <summary>
		///     Gets the summary (head 255) <see cref="Pcr" /> for the record context.
		/// </summary>
		public static Pcr GetSummaryPcr(this IRecordContext record) => (from r in record.StdfFile.GetRecords().OfExactType<Pcr>() where r.HeadNumber == 255 select r).FirstOrDefault();

		/// <summary>
		///     Gets the <see cref="Hbr">Hbrs</see> for the record context.
		/// </summary>
		/// <param name="record">The record context</param>
		/// <returns>All the <see cref="Hbr">Hbrs</see>.</returns>
		public static IEnumerable<Hbr> GetHbrs(this IRecordContext record) => record.StdfFile.GetRecords().OfExactType<Hbr>();

		/// <summary>
		///     Gets the <see cref="Hbr">Hbrs</see> for the record context
		///     with the given head and site.
		/// </summary>
		public static IEnumerable<Hbr> GetHbrs(this IRecordContext record, byte headNumber, byte siteNumber) => GetBinRecords<Hbr>(record, headNumber, siteNumber);

		/// <summary>
		///     Gets the summary (head 255) <see cref="Hbr">Hbrs</see> for the record context.
		/// </summary>
		public static IEnumerable<Hbr> GetSummaryHbrs(this IRecordContext record) => from r in record.StdfFile.GetRecords().OfExactType<Hbr>() where r.HeadNumber == 255 select r;

		/// <summary>
		///     Gets the <see cref="Sbr">Sbrs</see> for the record context.
		/// </summary>
		public static IEnumerable<Sbr> GetSbrs(this IRecordContext record) => record.StdfFile.GetRecords().OfExactType<Sbr>();

		/// <summary>
		///     Gets the <see cref="Sbr">Sbrs</see> for the record context
		///     with the given head and site.
		/// </summary>
		public static IEnumerable<Sbr> GetSbrs(this IRecordContext record, byte headNumber, byte siteNumber) => GetBinRecords<Sbr>(record, headNumber, siteNumber);

		/// <summary>
		///     Gets the summary (head 255) <see cref="Sbr">Sbrs</see> for the record context.
		/// </summary>
		public static IEnumerable<Sbr> GetSummarySbrs(this IRecordContext record) => from r in record.StdfFile.GetRecords().OfExactType<Sbr>() where r.HeadNumber == 255 select r;

		/// <summary>
		///     Gets the <see cref="Tsr">Tsrs</see> for the record context.
		/// </summary>
		public static IEnumerable<Tsr> GetTsrs(this IRecordContext record) => record.StdfFile.GetRecords().OfExactType<Tsr>();

		/// <summary>
		///     Gets the <see cref="Tsr">Tsrs</see> for the record context
		///     with the given head and site.
		/// </summary>
		public static IEnumerable<Tsr> GetTsrs(this IRecordContext record, byte headNumber, byte siteNumber) => from r in record.StdfFile.GetRecords().OfExactType<Tsr>() where (r.HeadNumber == headNumber) && (r.SiteNumber == siteNumber) select r;

		/// <summary>
		///     Gets the summary (head 255) <see cref="Tsr">Tsrs</see> for the record context.
		/// </summary>
		public static IEnumerable<Tsr> GetSummaryTsrs(IRecordContext record) => from r in record.StdfFile.GetRecords().OfExactType<Tsr>() where r.HeadNumber == 255 select r;

#region Helpers

		private static IEnumerable<T> GetBinRecords<T>(IRecordContext record, byte head, byte site) where T : BinSummaryRecord => from r in record.StdfFile.GetRecords().OfExactType<T>() where (r.HeadNumber == head) && (r.SiteNumber == site) select r;

#endregion

#endregion

#region extending StdfRecord

		/// <summary>
		///     returns records that occur before the given record
		/// </summary>
		/// <param name="record">The "marker" record</param>
		/// <returns>All the records before the marker record</returns>
		public static IEnumerable<StdfRecord> Before(this StdfRecord record) => record.StdfFile.GetRecords().TakeWhile(r => r.Offset < record.Offset);

		/// <summary>
		///     Returns the records that occur after the given record
		/// </summary>
		/// <param name="record">The "marker" record</param>
		/// <returns>All the records after the marker record</returns>
		public static IEnumerable<StdfRecord> After(this StdfRecord record) => record.StdfFile.GetRecords().SkipWhile(r => r.Offset <= record.Offset);

#endregion

#region extending IHeadIndexable

		/// <summary>
		///     Gets the <see cref="Wir" /> for the current head
		/// </summary>
		public static Wir GetWir(this IHeadIndexable record) => (from wir in record.StdfFile.GetRecords().OfExactType<Wir>() where wir.HeadNumber == record.HeadNumber select wir).FirstOrDefault();

		/// <summary>
		///     Gets the <see cref="Wrr" /> for the current head
		/// </summary>
		public static Wrr GetWrr(this IHeadIndexable record) => (from wrr in record.StdfFile.GetRecords().OfExactType<Wrr>() where wrr.HeadNumber == record.HeadNumber select wrr).FirstOrDefault();

#endregion

#region extending IHeadSiteIndexable

		/// <summary>
		///     Gets the current Prr associated with the head/site
		/// </summary>
		public static Prr GetPrr(this IHeadSiteIndexable record) => (from prr in record.StdfFile.GetRecords().OfExactType<Prr>() where (prr.HeadNumber == record.HeadNumber) && (prr.SiteNumber == record.SiteNumber) select prr).FirstOrDefault();

		/// <summary>
		///     Gets the current Pir associated with the head/site
		/// </summary>
		public static Pir GetPir(this IHeadSiteIndexable record) => (from pir in record.StdfFile.GetRecords().OfExactType<Pir>() where (pir.HeadNumber == record.HeadNumber) && (pir.SiteNumber == record.SiteNumber) select pir).FirstOrDefault();

#endregion

#region extending PIR/PRR

		public static Prr GetMatchingPrr(this Pir pir) => pir.After().OfExactType<Prr>().Where(r => (r.HeadNumber == pir.HeadNumber) && (r.SiteNumber == pir.SiteNumber)).FirstOrDefault();

		public static Pir GetMatchingPir(this Prr prr) => prr.Before().OfExactType<Pir>().Where(r => (r.HeadNumber == prr.HeadNumber) && (r.SiteNumber == prr.SiteNumber)).LastOrDefault();

		/// <summary>
		///     Gets the records associated with this pir
		/// </summary>
		/// <param name="pir">The <see cref="Pir" /> representing the part</param>
		/// <returns>
		///     The records associated with the part (between the <see cref="Pir" />
		///     and <see cref="Prr" /> and sharing the same head/site information.
		/// </returns>
		public static IEnumerable<StdfRecord> GetChildRecords(this Pir pir) => pir.After().OfType<IHeadSiteIndexable>().Where(r => (r.HeadNumber == pir.HeadNumber) && (r.SiteNumber == pir.SiteNumber)).TakeWhile(r => r.GetType() != typeof(Prr)).Cast<StdfRecord>();

		/// <summary>
		///     Gets the records associated with this prr
		/// </summary>
		/// <param name="prr">The <see cref="Prr" /> representing the part</param>
		/// <returns>
		///     The records associated with the part (between the <see cref="Pir" />
		///     and <see cref="Prr" /> and sharing the same head/site information.
		/// </returns>
		public static IEnumerable<StdfRecord> GetChildRecords(this Prr prr) => prr.GetMatchingPir().GetChildRecords();

#endregion
	}
}