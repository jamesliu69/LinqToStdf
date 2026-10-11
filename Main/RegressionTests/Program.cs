using System;
using System.Collections;
using System.IO;
using System.Linq;
using STDF;
using Stdf;
using Stdf.Records.V4;

namespace LinqToStdf.RegressionTests
{
	internal static class Program
	{
		private static int _passed;
		private static int _failed;
		private static readonly RecordConverterFactory Factory = new StdfFileWriter(new MemoryStream()).ConverterFactory;

		private static int Main()
		{
			foreach(Endian endian in new[] { Endian.Little, Endian.Big })
			{
				foreach(bool backwards in new[] { false, true })
				{
					foreach(int length in new[] { 0, 1, 7, 8, 9, 16, 17, 31, 32, 33, 65, 65535 })
					{
						int bitCount = length;
						Run($"Bit array {bitCount} bits / {endian} / backwards={backwards}", () => BitArrayRoundTrip(bitCount, endian, backwards));
					}
					Run($"Null bit array / {endian} / backwards={backwards}", () => NullBitArray(endian, backwards));
					Run($"Bit array overflow / {endian} / backwards={backwards}", () => BitArrayOverflow(endian, backwards));
					Run($"Nibble array fresh buffer / {endian} / backwards={backwards}", () => NibbleArrayRoundTrip(endian, backwards, false));
					Run($"Nibble array buffer growth / {endian} / backwards={backwards}", () => NibbleArrayRoundTrip(endian, backwards, true));
				}
				foreach(int length in new[] { 0, 2, 255 })
				{
					int byteCount = length;
					Run($"GDR byte array {byteCount} bytes / {endian}", () => GdrByteArrayRoundTrip(byteCount, endian));
				}
				Run($"GDR byte array overflow / {endian}", () => ExpectThrows<ArgumentOutOfRangeException>(() => Factory.Unconvert(new Gdr { GenericData = new object[] { new byte[256] } }, endian)));
				Run($"GDR nibble decode / {endian}", () => GdrNibbleDecode(endian));
				Run($"Nibble array backward byte order / {endian}", () => NibbleArrayBackwardOrder(endian));
				Run($"MPR pin states round trip / {endian}", () => MprRoundTrip(endian));
				Run($"FTR bitfield round trip / {endian}", () => FtrRoundTrip(endian));
			}

			Run("Converter construction preserves existing output", ConstructorPreservesOutput);
			Run("Converter construction does not create output", ConstructorDoesNotCreateOutput);
			Run("Analysis failure preserves output", AnalysisFailurePreservesOutput);
			Run("Analysis failure releases output", AnalysisFailureReleasesOutput);
			Run("Late failure preserves output and cleans temporary files", LateFailurePreservesOutput);
			Run("Failed new conversion leaves no output", FailedNewConversion);
			Run("Normal conversion creates readable output", () => SuccessfulConversion(false));
			Run("Normal conversion replaces existing output", () => SuccessfulConversion(true));
			Run("Failed conversion can be retried on the same instance", RetryAfterFailure);
			Run("Locked destination remains intact and can be retried", LockedDestination);
			Run("Relative output path works", RelativeOutputPath);
			Run("Long output filename works", LongOutputFilename);
			Run("MIR maps summary header fields", MirMapsSummaryHeaders);
			Run("FAIL part reports failing PTR and PRR", FailPartReportsFailingRecords);
			Run("Single-letter F result is accepted", SingleLetterFResultIsAccepted);
			Run("Single-letter P result is accepted", SingleLetterPResultIsAccepted);
			Run("Unsupported PASS/FAIL text fails the conversion", UnsupportedPassFailTextFailsConversion);
			Run("Ambiguous hardware bin fails the conversion", AmbiguousHardwareBinFailsConversion);
			Run("Hardware bin section maps PRR bins", HardwareBinSectionMapsPrrBins);

			Console.WriteLine($"RESULT: {_passed} passed, {_failed} failed");
			return _failed == 0 ? 0 : 1;
		}

		private static void Run(string name, Action test)
		{
			try
			{
				test();
				_passed++;
				Console.WriteLine("PASS " + name);
			}
			catch(Exception ex)
			{
				_failed++;
				Console.WriteLine($"FAIL {name}: {ex.GetType().Name}: {ex.Message}");
			}
		}

		private static void BitArrayRoundTrip(int length, Endian endian, bool backwards)
		{
			BitArray value = new BitArray(length);
			byte[] packed = new byte[(length + 7) / 8];
			for(int i = 0; i < length; i++)
			{
				value[i] = i % 3 == 0 || i == length - 1;
				if(value[i]) packed[i / 8] |= (byte)(1 << (i % 8));
			}
			using(MemoryStream stream = new MemoryStream())
			{
				new Stdf.BinaryWriter(stream, endian, backwards).WriteBitArray(value);
				byte[] bytes = stream.ToArray();
				if(backwards) Array.Reverse(bytes);
				byte[] expected = new byte[2 + packed.Length];
				expected[endian == Endian.Little ? 0 : 1] = (byte)length;
				expected[endian == Endian.Little ? 1 : 0] = (byte)(length >> 8);
				packed.CopyTo(expected, 2);
				EqualBytes(expected, bytes);
				using(Stdf.BinaryReader reader = new Stdf.BinaryReader(new MemoryStream(bytes), endian, true))
				{
					BitArray actual = reader.ReadBitArray();
					if(length == 0) Check(actual == null, "Empty bit array should retain the reader's missing-value behavior.");
					else
					{
						Check(actual.Length == length, "Bit count changed.");
						for(int i = 0; i < length; i++) Check(actual[i] == value[i], "Bit changed at index " + i);
					}
					Check(reader.AtEndOfStream, "Unexpected trailing bytes.");
				}
			}
		}

		private static void NullBitArray(Endian endian, bool backwards)
		{
			using(MemoryStream stream = new MemoryStream())
			{
				new Stdf.BinaryWriter(stream, endian, backwards).WriteBitArray(null);
				EqualBytes(new byte[2], stream.ToArray());
			}
		}

		private static void BitArrayOverflow(Endian endian, bool backwards)
		{
			using(MemoryStream stream = new MemoryStream())
			{
				Stdf.BinaryWriter writer = new Stdf.BinaryWriter(stream, endian, backwards);
				ExpectThrows<ArgumentOutOfRangeException>(() => writer.WriteBitArray(new BitArray(65536)));
				Check(stream.Length == 0, "Invalid bit array partially wrote to the stream.");
			}
		}

		private static void NibbleArrayRoundTrip(Endian endian, bool backwards, bool grow)
		{
			byte[] values = grow ? Enumerable.Range(0, 19).Select(i => (byte)(i % 16)).ToArray() : new byte[] { 1, 2, 3, 4, 5 };
			byte[] original = (byte[])values.Clone();
			using(MemoryStream stream = new MemoryStream())
			{
				Stdf.BinaryWriter writer = new Stdf.BinaryWriter(stream, endian, backwards);
				if(grow)
				{
					writer.WriteUInt16(0);
					stream.SetLength(0);
					stream.Position = 0;
				}
				writer.WriteNibbleArray(values);
				byte[] bytes = stream.ToArray();
				if(backwards) Array.Reverse(bytes);
				Check(bytes.Length == (values.Length + 1) / 2, "Packed nibble length changed.");
				for(int i = 0; i < values.Length; i++) Check(((bytes[i / 2] >> ((i % 2) * 4)) & 15) == values[i], "Nibble changed at index " + i);
				EqualBytes(original, values);
				using(Stdf.BinaryReader reader = new Stdf.BinaryReader(new MemoryStream(bytes), endian, true))
				{
					EqualBytes(values, reader.ReadNibbleArray(values.Length));
					Check(reader.AtEndOfStream, "Unexpected trailing bytes.");
				}
			}
		}

		private static void GdrByteArrayRoundTrip(int length, Endian endian)
		{
			byte[] values = Enumerable.Range(0, length).Select(i => (byte)(170 + i)).ToArray();
			Gdr record = new Gdr { GenericData = new object[] { (uint)123456, values, "after-array" } };
			UnknownRecord raw = Factory.Unconvert(record, endian);
			Check(raw.Content[7] == 11 && raw.Content[8] == length, "GDR byte array length prefix is missing or incorrect.");
			Gdr actual = (Gdr)Factory.Convert(raw);
			Check((uint)actual.GenericData[0] == 123456, "Preceding GDR value changed.");
			EqualBytes(values, (byte[])actual.GenericData[1]);
			Check((string)actual.GenericData[2] == "after-array", "Following GDR value changed.");
		}

		private static void NibbleArrayBackwardOrder(Endian endian)
		{
			using(MemoryStream stream = new MemoryStream())
			{
				Stdf.BinaryWriter writer = new Stdf.BinaryWriter(stream, endian, true);
				writer.WriteUInt32(0);
				stream.SetLength(0);
				stream.Position = 0;
				writer.WriteNibbleArray(new byte[] { 1, 2, 3, 4 });
				EqualBytes(new byte[] { 0x43, 0x21 }, stream.ToArray());
			}
		}

		private static void GdrNibbleDecode(Endian endian)
		{
			byte[] bytes = endian == Endian.Little ? new byte[] { 2, 0, 13, 0xFA, 1, 0x12 } : new byte[] { 0, 2, 13, 0xFA, 1, 0x12 };
			Gdr actual = (Gdr)Factory.Convert(new UnknownRecord(new RecordType(50, 10), bytes, endian));
			Check(actual.GenericData[0] is byte && (byte)actual.GenericData[0] == 10, "GDR nibble value was lost or not masked.");
			Check((byte)actual.GenericData[1] == 0x12, "Following GDR value changed.");
		}

		private static void MprRoundTrip(Endian endian)
		{
			Mpr record = new Mpr { TestNumber = 1, PinStates = new byte[] { 1, 2, 3, 4, 5 }, PinIndexes = new ushort[] { 1, 2, 3, 4, 5 }, Results = new float[] { 1, 2, 3, 4, 5 } };
			Mpr actual = (Mpr)Factory.Convert(Factory.Unconvert(record, endian));
			EqualBytes(record.PinStates, actual.PinStates);
			Check(record.Results.SequenceEqual(actual.Results), "MPR results changed.");
		}

		private static void FtrRoundTrip(Endian endian)
		{
			Ftr record = new Ftr { TestNumber = 1, FailingPinBitfield = new BitArray(65, true) };
			Ftr actual = (Ftr)Factory.Convert(Factory.Unconvert(record, endian));
			Check(actual.FailingPinBitfield.Length == 65, "FTR bitfield length changed.");
			for(int i = 0; i < 65; i++) Check(actual.FailingPinBitfield[i], "FTR bitfield changed at index " + i);
		}

		private static void WithFixture(Action<string, string> test, bool summary = true, bool validEnd = true, string passOrFail = "PASS", string summaryExtras = null)
		{
			string root = Path.Combine(Path.GetTempPath(), "LinqToStdf-tests-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(root);
			File.WriteAllText(Path.Combine(root, "test.log"), "==> Test Start\r\n<<<<<<---------------     Test Item : OSitem_T1 --------------->>>>>>\r\n" + passOrFail + " 1 P1 0mA 0V 2V 1V 0.9V 1.1V\r\n==> Test End\r\n");
			if(summary) WriteSummary(root, validEnd, summaryExtras);
			try { test(root, Path.Combine(root, "output.stdf")); }
			finally
			{
				// All test files live in the unique directory created immediately above.
				// Leave failed fixtures available when an old implementation leaks a handle.
				try { Directory.Delete(root, true); }
				catch(IOException) { Console.WriteLine("Fixture retained: " + root); }
			}
		}

		private static void WriteSummary(string root, bool validEnd, string extras = null)
		{
			File.WriteAllText(Path.Combine(root, "test_Summary.log"), "Lot START: 2026-10-01 12:00:00\r\nLot END: " + (validEnd ? "2026-10-01 12:01:00" : "invalid-date") + "\r\nLot Number: LOT1\r\n" + (extras ?? string.Empty));
		}

		private static void ConstructorPreservesOutput()
		{
			WithFixture((root, output) =>
			{
				File.WriteAllText(output, "PREVIOUS_GOOD_OUTPUT");
				CStdf converter = new CStdf(root, output);
				Check(ReadOutputText(output) == "PREVIOUS_GOOD_OUTPUT", "Constructor truncated existing output.");
				ExclusiveOpen(output);
				GC.KeepAlive(converter);
			});
		}

		private static void ConstructorDoesNotCreateOutput()
		{
			WithFixture((root, output) =>
			{
				CStdf converter = new CStdf(root, output);
				Check(!File.Exists(output), "Constructor created output before conversion.");
				GC.KeepAlive(converter);
			});
		}

		private static void AnalysisFailurePreservesOutput()
		{
			WithFixture((root, output) =>
			{
				File.WriteAllText(output, "PREVIOUS_GOOD_OUTPUT");
				CStdf converter = new CStdf(root, output);
				ExpectThrows<InvalidDataException>(() => converter.DoWork());
				Check(ReadOutputText(output) == "PREVIOUS_GOOD_OUTPUT", "Analysis failure destroyed existing output.");
				NoTemporaryFiles(root);
				GC.KeepAlive(converter);
			}, false);
		}

		private static void AnalysisFailureReleasesOutput()
		{
			WithFixture((root, output) =>
			{
				File.WriteAllText(output, "PREVIOUS_GOOD_OUTPUT");
				CStdf converter = new CStdf(root, output);
				ExpectThrows<InvalidDataException>(() => converter.DoWork());
				ExclusiveOpen(output);
				GC.KeepAlive(converter);
			}, false);
		}

		private static void LateFailurePreservesOutput()
		{
			WithFixture((root, output) =>
			{
				File.WriteAllText(output, "PREVIOUS_GOOD_OUTPUT");
				CStdf converter = new CStdf(root, output);
				ExpectThrows<FormatException>(() => converter.DoWork());
				Check(ReadOutputText(output) == "PREVIOUS_GOOD_OUTPUT", "Failure after writing records destroyed existing output.");
				ExclusiveOpen(output);
				NoTemporaryFiles(root);
				GC.KeepAlive(converter);
			}, true, false);
		}

		private static void FailedNewConversion()
		{
			WithFixture((root, output) =>
			{
				CStdf converter = new CStdf(root, output);
				ExpectThrows<FormatException>(() => converter.DoWork());
				Check(!File.Exists(output), "Failed conversion left an incomplete output file.");
				NoTemporaryFiles(root);
				GC.KeepAlive(converter);
			}, true, false);
		}

		private static void SuccessfulConversion(bool existing)
		{
			WithFixture((root, output) =>
			{
				if(existing) File.WriteAllText(output, "PREVIOUS_GOOD_OUTPUT");
				new CStdf(root, output).DoWork();
				VerifyOutput(output);
				ExclusiveOpen(output);
				NoTemporaryFiles(root);
			});
		}

		private static void RetryAfterFailure()
		{
			WithFixture((root, output) =>
			{
				File.WriteAllText(output, "PREVIOUS_GOOD_OUTPUT");
				CStdf converter = new CStdf(root, output);
				ExpectThrows<FormatException>(() => converter.DoWork());
				WriteSummary(root, true);
				converter.DoWork();
				VerifyOutput(output);
				NoTemporaryFiles(root);
			}, true, false);
		}

		private static void LockedDestination()
		{
			WithFixture((root, output) =>
			{
				File.WriteAllText(output, "PREVIOUS_GOOD_OUTPUT");
				CStdf converter = new CStdf(root, output);
				using(FileStream locked = new FileStream(output, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					ExpectThrows<IOException>(() => converter.DoWork());
					Check(File.ReadAllText(output) == "PREVIOUS_GOOD_OUTPUT", "Locked destination was altered.");
					NoTemporaryFiles(root);
				}
				converter.DoWork();
				VerifyOutput(output);
				ExclusiveOpen(output);
			});
		}

		private static void RelativeOutputPath()
		{
			WithFixture((root, output) =>
			{
				string originalDirectory = Environment.CurrentDirectory;
				try
				{
					Environment.CurrentDirectory = root;
					new CStdf(root, "output.stdf").DoWork();
					VerifyOutput(output);
					NoTemporaryFiles(root);
				}
				finally { Environment.CurrentDirectory = originalDirectory; }
			});
		}

		private static void VerifyOutput(string output)
		{
			StdfRecord[] records = new StdfFile(output).GetRecordsEnumerable().ToArray();
			Check(records.Count(r => r is Far) == 1 && records.Count(r => r is Mrr) == 1, "Output lacks a single file start/end.");
			Check(!records.Any(r => r is Stdf.Records.ErrorRecord), "Output contains parse errors.");
			Ptr ptr = records.OfType<Ptr>().Single();
			Check(ptr.Result == 1 && ptr.LowLimit == 0 && ptr.HighLimit == 2 && ptr.Units == "V", "Measurement data changed.");
			Check(ptr.TestFlags == 0, "PASS data line should leave the PTR test flags clean.");
			Prr prr = records.OfType<Prr>().Single();
			Check(prr.Failed == false, "Part disposition changed.");
			Check(prr.HardBin == 1 && prr.SoftBin == 1, "Fallback bins should resolve to bin 1.");
			Mir mir = records.OfType<Mir>().Single();
			Check(mir.LotId == "LOT1", "MIR LOT_ID should carry the summary lot number.");
			Check(string.IsNullOrEmpty(mir.JobName) && string.IsNullOrEmpty(mir.OperatorName) && string.IsNullOrEmpty(mir.SpecificationName), "MIR fields without a matching summary header should stay empty.");
			Tsr tsr = records.OfType<Tsr>().Single();
			Check(tsr.HeadNumber == 255 && tsr.SiteNumber == 0, "TSR summary should use head 255 / site 0.");
			Check(tsr.ExecutedCount == 1 && tsr.FailedCount == 0, "TSR counts changed.");
			Hbr hbr = records.OfType<Hbr>().Single();
			Check(hbr.HeadNumber == 255 && hbr.SiteNumber == 0 && hbr.BinNumber == 1 && hbr.BinCount == 1 && hbr.BinPassFail == "P", "HBR fallback summary changed.");
			Sbr sbr = records.OfType<Sbr>().Single();
			Check(sbr.HeadNumber == 255 && sbr.SiteNumber == 0 && sbr.BinNumber == 1 && sbr.BinCount == 1 && sbr.BinPassFail == "P", "SBR fallback summary changed.");
			Pcr[] pcrs = records.OfType<Pcr>().ToArray();
			Check(pcrs.Length == 2, "Output should contain one site PCR and one lot PCR.");
			Pcr sitePcr = pcrs.Single(pcr => pcr.HeadNumber == 1);
			Check(sitePcr.SiteNumber == 1 && sitePcr.PartCount == 1 && sitePcr.GoodCount == 1, "Site PCR counts changed.");
			Pcr lotPcr = pcrs.Single(pcr => pcr.HeadNumber == 255);
			Check(lotPcr.SiteNumber == 0 && lotPcr.PartCount == 1 && lotPcr.GoodCount == 1, "Lot PCR counts changed.");
			Check(pcrs.All(pcr => pcr.FunctionalCount == null), "PCR FUNCTIONAL_COUNT should stay empty.");
		}

		private static void LongOutputFilename()
		{
			WithFixture((root, output) =>
			{
				string longOutput = Path.Combine(root, new string('x', 240 - root.Length - 6) + ".stdf");
				new CStdf(root, longOutput).DoWork();
				VerifyOutput(longOutput);
				NoTemporaryFiles(root);
			});
		}

		private static void MirMapsSummaryHeaders()
		{
			WithFixture((root, output) =>
			{
				new CStdf(root, output).DoWork();
				Mir mir = new StdfFile(output).GetRecordsEnumerable().OfType<Mir>().Single();
				Check(mir.JobName == "TP-2020A", "MIR JOB_NAME should carry the summary Test Program Name.");
				Check(mir.OperatorName == "OP-42", "MIR OPERATOR_NAME should carry the summary Operator.");
				Check(string.IsNullOrEmpty(mir.SpecificationName), "MIR SPECIFICATION_NAME should stay empty.");
				NoTemporaryFiles(root);
			}, true, true, "PASS", "Operator: OP-42\r\nTest Program Name: TP-2020A\r\n");
		}

		private static void FailPartReportsFailingRecords()
		{
			WithFixture((root, output) =>
			{
				new CStdf(root, output).DoWork();
				StdfRecord[] records = new StdfFile(output).GetRecordsEnumerable().ToArray();
				Check(records.OfType<Ptr>().Single().TestFlags == 0x80, "FAIL data line should set the PTR fail flag.");
				Prr prr = records.OfType<Prr>().Single();
				Check(prr.Failed == true, "FAIL part should be marked failed.");
				Check(prr.HardBin == 2 && prr.SoftBin == 2, "FAIL part should resolve to the fallback FAIL bin.");
				Hbr hbr = records.OfType<Hbr>().Single();
				Check(hbr.HeadNumber == 255 && hbr.BinNumber == 2 && hbr.BinPassFail == "F", "HBR should report the fallback FAIL bin summary.");
				Tsr tsr = records.OfType<Tsr>().Single();
				Check(tsr.ExecutedCount == 1 && tsr.FailedCount == 1, "TSR should count the failed execution.");
				Pcr lotPcr = records.OfType<Pcr>().Single(pcr => pcr.HeadNumber == 255);
				Check(lotPcr.PartCount == 1 && lotPcr.GoodCount == 0, "Lot PCR should report zero good parts.");
				NoTemporaryFiles(root);
			}, true, true, "FAIL");
		}

		private static void SingleLetterFResultIsAccepted()
		{
			WithFixture((root, output) =>
			{
				new CStdf(root, output).DoWork();
				StdfRecord[] records = new StdfFile(output).GetRecordsEnumerable().ToArray();
				Check(records.OfType<Ptr>().Single().TestFlags == 0x80, "Single-letter F should behave like FAIL.");
				Check(records.OfType<Prr>().Single().Failed == true, "Single-letter F should mark the part failed.");
				NoTemporaryFiles(root);
			}, true, true, "F");
		}

		private static void SingleLetterPResultIsAccepted()
		{
			WithFixture((root, output) =>
			{
				new CStdf(root, output).DoWork();
				StdfRecord[] records = new StdfFile(output).GetRecordsEnumerable().ToArray();
				Check(records.OfType<Ptr>().Single().TestFlags == 0, "Single-letter P should behave like PASS.");
				Check(records.OfType<Prr>().Single().Failed == false, "Single-letter P should not mark the part failed.");
				Pcr lotPcr = records.OfType<Pcr>().Single(pcr => pcr.HeadNumber == 255);
				Check(lotPcr.GoodCount == 1, "Single-letter P should count as a good part.");
				NoTemporaryFiles(root);
			}, true, true, "P");
		}

		private static void UnsupportedPassFailTextFailsConversion()
		{
			WithFixture((root, output) =>
			{
				File.WriteAllText(output, "PREVIOUS_GOOD_OUTPUT");
				CStdf converter = new CStdf(root, output);
				ExpectThrows<InvalidDataException>(() => converter.DoWork());
				Check(ReadOutputText(output) == "PREVIOUS_GOOD_OUTPUT", "Failed conversion destroyed existing output.");
				NoTemporaryFiles(root);
				GC.KeepAlive(converter);
			}, true, true, "OK");
		}

		private static void AmbiguousHardwareBinFailsConversion()
		{
			WithFixture((root, output) =>
			{
				File.WriteAllText(output, "PREVIOUS_GOOD_OUTPUT");
				CStdf converter = new CStdf(root, output);
				ExpectThrows<InvalidDataException>(() => converter.DoWork());
				Check(ReadOutputText(output) == "PREVIOUS_GOOD_OUTPUT", "Failed conversion destroyed existing output.");
				NoTemporaryFiles(root);
				GC.KeepAlive(converter);
			}, true, true, "PASS", "[HARDWARE BIN]\r\n" + new string('*', 64) + "\r\n1 (Pass) 1 PASS GoodBin\r\n3 (Pass) 1 PASS AlsoGood\r\n");
		}

		private static void HardwareBinSectionMapsPrrBins()
		{
			WithFixture((root, output) =>
			{
				new CStdf(root, output).DoWork();
				StdfRecord[] records = new StdfFile(output).GetRecordsEnumerable().ToArray();
				Prr prr = records.OfType<Prr>().Single();
				Check(prr.HardBin == 3, "PRR HARD_BIN should come from the summary hardware bin section.");
				Check(prr.SoftBin == 1, "PRR SOFT_BIN should fall back to bin 1 when no software bins are listed.");
				Hbr hbr = records.OfType<Hbr>().Single();
				Check(hbr.HeadNumber == 255 && hbr.BinNumber == 3 && hbr.BinCount == 1 && hbr.BinPassFail == "P", "HBR should mirror the summary hardware bin section.");
				NoTemporaryFiles(root);
			}, true, true, "PASS", "[HARDWARE BIN]\r\n" + new string('*', 64) + "\r\n3 (Pass) 1 PASS GoodBin\r\n");
		}

		private static void NoTemporaryFiles(string root) => Check(Directory.GetFiles(root, "*.tmp").Length == 0, "Temporary output was not cleaned up.");
		private static string ReadOutputText(string output)
		{
			using(StreamReader reader = new StreamReader(new FileStream(output, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))) return reader.ReadToEnd();
		}
		private static void ExclusiveOpen(string output) { using(FileStream stream = new FileStream(output, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { } }
		private static void EqualBytes(byte[] expected, byte[] actual) => Check(expected.SequenceEqual(actual), $"Expected {BitConverter.ToString(expected)}, got {BitConverter.ToString(actual)}.");
		private static void Check(bool condition, string message) { if(!condition) throw new InvalidOperationException(message); }

		private static void ExpectThrows<T>(Action action) where T : Exception
		{
			try { action(); }
			catch(T) { return; }
			throw new InvalidOperationException("Expected " + typeof(T).Name);
		}
	}
}
