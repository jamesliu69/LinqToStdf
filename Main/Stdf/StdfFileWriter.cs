// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using Stdf.Records;
using Stdf.Records.V4;
using System;
using System.Collections.Generic;
using System.IO;

namespace Stdf
{
    /// <summary>
    ///     StdfFileWriter provides a "what you expect" API for writing STDF files.
    ///     You provide a path and then call <see cref="WriteRecord" /> or <see cref="WriteRecords" />
    ///     to write to the file.
    ///     You can provide an endianness, or have the endianness inferred from the first record
    ///     (which must be either <see cref="StartOfStreamRecord" /> or <see cref="Far" />.
    /// </summary>
    /// <seealso cref="StdfOutputDirectory" />
    public sealed class StdfFileWriter : IDisposable
	{
		private readonly bool   _OwnsStream;
		private readonly Stream _Stream;
		private          Endian _Endian;

		public StdfFileWriter(string path, Endian endian, bool debug = false) : this(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read), endian, debug, true)
		{
		}

		public StdfFileWriter(string path, bool debug = false) : this(path, Endian.Unknown, debug)
		{
		}

		public StdfFileWriter(Stream stream, Endian endian, bool debug = false, bool ownsStream = false)
		{
			_Stream     = stream;
			_Endian     = endian;
			_OwnsStream = ownsStream;

			if(debug)
			{
				ConverterFactory = new RecordConverterFactory {
					Debug = debug,
                };
				StdfV4Specification.RegisterRecords(ConverterFactory);
			}
			else
			{
				ConverterFactory = new RecordConverterFactory(StdfFile._V4ConverterFactory);
			}
		}

		public StdfFileWriter(Stream stream, bool debug = false, bool ownsStream = false) : this(stream, Endian.Unknown, debug, ownsStream)
		{
		}

		public RecordConverterFactory ConverterFactory { get; }

#region IDisposable Members

		public void Dispose()
		{
			if(_OwnsStream)
			{
				_Stream?.Dispose();
			}
		}

#endregion

        /// <summary>
        ///     Writes a single record to the file, returning the number of bytes written
        /// </summary>
        /// <param name="record"></param>
        public int WriteRecord(StdfRecord record)
		{
			if(record == null)
			{
				throw new ArgumentNullException("record");
			}

			if(_Endian == Endian.Unknown)
			{
				//we must be able to infer the endianness based on the first record
				if(record.GetType() == typeof(StartOfStreamRecord))
				{
					StartOfStreamRecord sos = (StartOfStreamRecord)record;
					_Endian = sos.Endian;
					return 0;
				}

				if(record.GetType() == typeof(Far))
				{
					InferEndianFromFar((Far)record);
				}

				if(_Endian == Endian.Unknown)
				{
					throw new InvalidOperationException(Resources.CannotInferEndianness);
				}
			}

			if(record.IsWritable)
			{
				BinaryWriter  writer = new BinaryWriter(_Stream, _Endian, false);
				UnknownRecord ur     = ConverterFactory.Unconvert(record, _Endian);
				writer.WriteHeader(new RecordHeader((ushort)ur.Content.Length, ur.RecordType));
				_Stream.Write(ur.Content, 0, ur.Content.Length);
				return ur.Content.Length + 4;
			}
			return 0;
		}

		private void InferEndianFromFar(Far far)
		{
			if(far == null)
			{
				throw new ArgumentNullException("far");
			}

			switch(far.CpuType)
			{
				case 0:
				case 1:
					_Endian = Endian.Big;
					break;
				default:
					_Endian = Endian.Little;
					break;
			}
		}

        /// <summary>
        ///     Writes a stream of records to the file.
        /// </summary>
        /// <param name="records"></param>
        public int WriteRecords(IEnumerable<StdfRecord> records)
		{
			int bytesWritten = 0;

			if(records != null)
			{
				foreach(StdfRecord r in records)
				{
					if(r != null)
					{
						bytesWritten += WriteRecord(r);
					}
				}
			}
			return bytesWritten;
		}
	}
}