using Microsoft.VisualStudio.TestTools.UnitTesting;
using Stdf;
using Stdf.RecordConverting;
using Stdf.Records;
using Stdf.Records.V4;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace StdfFileTests
{
	internal static class HelperExtensions
	{
		public static TRecord GetSingleRecord<TRecord>(this StdfFile file) where TRecord : StdfRecord
		{
			StartOfStreamRecord sos              = null;
			Far                 far              = null;
			TRecord             recordOfInterest = null;
			EndOfStreamRecord   eos              = null;

			foreach(StdfRecord record in file.GetRecordsEnumerable())
			{
				if(sos == null)
				{
					sos = (StartOfStreamRecord)record;
				}
				else if(far == null)
				{
					far = (Far)record;

					if(typeof(TRecord) == typeof(Far))
					{
						recordOfInterest = (TRecord)(object)far;
					}
				}
				else if(recordOfInterest == null)
				{
					recordOfInterest = (TRecord)record;
				}
				else if(eos == null)
				{
					eos = (EndOfStreamRecord)record;
				}
				else
				{
					Assert.Fail("There were extra records");
				}
			}
			Assert.IsNotNull(sos,              "No start of stream");
			Assert.IsNotNull(recordOfInterest, "No record of interest");
			Assert.IsNotNull(eos,              "No end of stream");

			//TODO: assert things about sos/eos?
			return recordOfInterest;
		}

		public static DateTime TruncateToSeconds(this DateTime dateTime) => dateTime.AddTicks(-(dateTime.Ticks % TimeSpan.TicksPerSecond));
	}

	[TestClass]
	public class RoundTrip
	{
		public void Log(string msg) => Debug.WriteLine(msg);

		[TestInitialize]
		public void Init() => ConverterLog.MessageLogged += Log;

		[TestCleanup]
		public void Cleanup() => ConverterLog.MessageLogged -= Log;

		[TestMethod]
		public void TestFar()
		{
			Far far = new Far();
			TestRoundTripEquality(far);
			far.CpuType = 1;
			TestRoundTripEquality(far);
			far.CpuType = 2;
			TestRoundTripEquality(far, Endian.Little);
			far.CpuType     = 1;
			far.StdfVersion = 5;
			TestRoundTripEquality(far);
		}

		[TestMethod]
		public void TestAtr()
		{
			Atr atr = new Atr();
			TestRoundTripEquality(atr);
			atr.CommandLine = "This is a test";
			TestRoundTripEquality(atr);
			atr.ModifiedTime = DateTime.Now.TruncateToSeconds();
			atr.CommandLine  = null;
			TestRoundTripEquality(atr);
			atr.ModifiedTime = null;
			TestRoundTripEquality(atr);
		}

		[TestMethod]
		public void TestMir()
		{
			Mir mir = new Mir();
			TestRoundTripEquality(mir);
			mir.SupervisorName = "Mark";
			TestRoundTripEquality(mir);
		}

		[TestMethod]
		public void TestMrr()
		{
			Mrr mrr = new Mrr();
			TestRoundTripEquality(mrr);
			mrr.ExecDescription = "Super Cool";
			TestRoundTripEquality(mrr);
		}

		[TestMethod]
		public void TestPcr()
		{
			Pcr pcr = new Pcr {
				HeadNumber = 1,
				SiteNumber = 1,
			};
			TestRoundTripEquality(pcr);
			pcr.FunctionalCount = 1;
			TestRoundTripEquality(pcr);
		}

		[TestMethod]
		public void TestHbr()
		{
			Hbr hbr = new Hbr {
				HeadNumber = 1,
				SiteNumber = 1,
			};
			TestRoundTripEquality(hbr);
			hbr.BinName = "Fred";
			TestRoundTripEquality(hbr);
		}

		[TestMethod]
		public void TestSbr()
		{
			Sbr sbr = new Sbr {
				HeadNumber = 1,
				SiteNumber = 1,
			};
			TestRoundTripEquality(sbr);
			sbr.BinName = "Bob";
			TestRoundTripEquality(sbr);
		}

		[TestMethod]
		public void TestPmr()
		{
			Pmr pmr = new Pmr {
				HeadNumber = 1,
				SiteNumber = 1,
			};
			TestRoundTripEquality(pmr);
			pmr.SiteNumber = 0;
			TestRoundTripEquality(pmr);
		}

		[TestMethod]
		public void TestPgr()
		{
			Pgr pgr = new Pgr();
			TestRoundTripEquality(pgr);
			pgr.PinIndexes = new ushort[] { 0, 1 };
			TestRoundTripEquality(pgr);
		}

		[TestMethod]
		public void TestPlr()
		{
			Plr plr = new Plr {
				GroupIndexes = new ushort[] { 0 },
			};
			TestRoundTripEquality(plr);
		}

		[TestMethod]
		public void TestRdr()
		{
			Rdr rdr = new Rdr();
			TestRoundTripEquality(rdr);
			rdr.RetestBins = new ushort[] { 1, 2, 3, 4 };
			TestRoundTripEquality(rdr);
		}

		[TestMethod]
		public void TestSdr()
		{
			Sdr sdr = new Sdr {
				HeadNumber = 1,
			};
			TestRoundTripEquality(sdr);
			sdr.ExtraId = "Professor Snape";
			TestRoundTripEquality(sdr);
		}

		[TestMethod]
		public void TestWir()
		{
			Wir wir = new Wir {
				HeadNumber = 1,
			};
			TestRoundTripEquality(wir);
			wir.WaferId = "Wolverine";
			TestRoundTripEquality(wir);
		}

		[TestMethod]
		public void TestWrr()
		{
			Wrr wrr = new Wrr {
				HeadNumber = 1,
			};
			TestRoundTripEquality(wrr);
			wrr.ExecDescription = "It looks good";
			TestRoundTripEquality(wrr);
		}

		[TestMethod]
		public void TestWcr()
		{
			Wcr wcr = new Wcr();
			TestRoundTripEquality(wcr);
			wcr.PositiveY = "U";
			TestRoundTripEquality(wcr);
		}

		[TestMethod]
		public void TestPir()
		{
			Pir pir = new Pir();

			//we must skip head number since we persist the missing value 1
			TestRoundTripEquality(pir, skipProps: new[] { "HeadNumber" });
			pir.SiteNumber = 1;
			TestRoundTripEquality(pir, skipProps: new[] { "HeadNumber" });
			pir.HeadNumber = 1;
			TestRoundTripEquality(pir);
		}

		[TestMethod]
		public void TestPrr()
		{
			Prr prr = new Prr {
				HeadNumber = 1,
				SiteNumber = 1,
			};
			TestRoundTripEquality(prr);
			prr.PartFix = new byte[] { 1, 2, 3 };
			TestRoundTripEquality(prr);
		}

		[TestMethod]
		public void TestTsr()
		{
			Tsr tsr = new Tsr {
				HeadNumber = 1,
				SiteNumber = 1,
			};
			TestRoundTripEquality(tsr);
			tsr.TestSumOfSquares = 47.001f;
			TestRoundTripEquality(tsr);
		}

		[TestMethod]
		public void TestPtr()
		{
			Ptr ptr = new Ptr {
				HeadNumber = 1,
				SiteNumber = 1,
			};
			TestRoundTripEquality(ptr);
			ptr.TestFlags     = 0xff;
			ptr.OptionalFlags = 0xf7;
			ptr.HighSpecLimit = 40.01f;
			TestRoundTripEquality(ptr);
		}

		[TestMethod]
		public void TestMpr()
		{
			Mpr mpr = new Mpr {
				HeadNumber = 1,
				SiteNumber = 1,
			};
			TestRoundTripEquality(mpr);
			mpr.PinIndexes = new ushort[] { 0, 1 };
			mpr.PinStates  = new byte[] { 0, 1 };
			mpr.Results    = new[] { 0.0f, 1.1f };
			TestRoundTripEquality(mpr);
		}

		[TestMethod]
		public void TestFtr()
		{
			Ftr ftr = new Ftr {
				HeadNumber = 1,
				SiteNumber = 1,
			};
			TestRoundTripEquality(ftr);
		}

		[TestMethod]
		public void TestBps()
		{
			Bps bps = new Bps();
			TestRoundTripEquality(bps);
		}

		[TestMethod]
		public void TestEps()
		{
			Eps eps = new Eps();
			TestRoundTripEquality(eps);
		}

		[TestMethod]
		public void TestGdr()
		{
			Gdr gdr = new Gdr();
			TestRoundTripEquality(gdr);
		}

		[TestMethod]
		public void TestDtr()
		{
			Dtr dtr = new Dtr();
			TestRoundTripEquality(dtr);
		}

		public void TestRoundTripEquality<TRecord>(TRecord record, Endian endian = Endian.Big, IEnumerable<string> skipProps = null) where TRecord : StdfRecord => TestRecordEquality(record, RoundTripRecord(record, endian, true), skipProps);

		public void TestRecordEquality<TRecord>(TRecord one, TRecord two, IEnumerable<string> skipProps = null) where TRecord : StdfRecord
		{
			IEnumerable<Action> props = from prop in typeof(TRecord).GetProperties(BindingFlags.Public | BindingFlags.Instance)
										where (prop.Name != nameof(StdfRecord.StdfFile)) && (prop.Name != nameof(StdfRecord.Offset)) && !(skipProps?.Contains(prop.Name) ?? false)
										let del = (Func<TRecord, object>)(r => prop.GetGetMethod().Invoke(r, new object[0]))
										let test = (Action)(() =>
										{
											//TODO: test arrays
											if(prop.PropertyType.IsArray)
											{
												IList listOne = (IList)del(one);
												IList listTwo = (IList)del(two);
												Assert.AreEqual(listOne?.Count, listTwo?.Count, $"Arrays for {prop.Name} have different length");

												for(int i = 0; i < (listOne?.Count ?? 0); i++)
												{
													Assert.AreEqual(listOne[i], listTwo[i], $"{prop.Name} index {i} is different");
												}
											}
											else
											{
												Assert.AreEqual(del(one), del(two), $"{prop.Name} not equal");
											}
										})
										select test;

			foreach(Action t in props)
			{
				t();
			}
		}

		public TRecord RoundTripRecord<TRecord>(TRecord record, Endian endian, bool debug) where TRecord : StdfRecord
		{
			using(MemoryStream testStream = new MemoryStream())
			{
				using(StdfFileWriter writer = new StdfFileWriter(testStream, endian, true))
				{
					if(typeof(TRecord) != typeof(Far))
					{
						writer.WriteRecord(new Far {
							CpuType     = endian == Endian.Big ? (byte)1 : (byte)2,
							StdfVersion = 4,
						});
					}
					writer.WriteRecord(record);
				}
				testStream.Seek(0, SeekOrigin.Begin);

				using(TestStreamManager streamManager = new TestStreamManager(testStream))
				{
					StdfFile file = new StdfFile(streamManager, debug) {
						ThrowOnFormatError = true,
                    };
					return file.GetSingleRecord<TRecord>();
				}
			}
		}
	}

	internal class TestStreamManager : IStdfStreamManager, IStdfStreamScope
	{
		private readonly MemoryStream _TestStream;

		public TestStreamManager(MemoryStream testStream) => _TestStream = testStream;

		public string Name { get => "TestStream"; }

		public IStdfStreamScope GetScope()
		{
			_TestStream.Seek(0, SeekOrigin.Begin);
			return this;
		}

		Stream IStdfStreamScope.Stream { get => _TestStream; }

		void IDisposable.Dispose()
		{
			//nop
		}
	}
}