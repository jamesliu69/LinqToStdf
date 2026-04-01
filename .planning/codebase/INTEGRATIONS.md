# External Integrations

**Analysis Date:** 2024-12-19

## APIs & External Services

**None Detected** - The codebase has no explicit third-party API integrations or external service dependencies.

## Data Storage

**Databases:**
- None - No database integration detected
- All data handling is in-memory or file-based

**File Storage:**
- Local filesystem only
  - Input: STDF files (binary format) read from disk via `FileStream`
  - Output: STDF files written via `StdfFileWriter` to specified paths
  - Log files: P2020 log files (text format) read from disk in `CP2020.cs`
  - Summary logs: Text-based summary files parsed for metadata

**File Format Handling:**
- STDF V4 (Standard Test Data Format Version 4)
  - Binary format parser/writer in `Stdf/Records/V4/` directory
  - Endian-aware binary reading via `BinaryReader.cs` (supports both Little and Big Endian)
  - Binary writing via `BinaryWriter.cs`
  - Record types defined in `Stdf/Records/V4/`: Far.cs, Mir.cs, Wrr.cs, Prr.cs, Ptr.cs, Ftr.cs, etc.
  - V4 specification compliance in `StdfV4Specification.cs`

**Caching:**
- None - No explicit caching layer

## Data Format Specifications

**STDF V4 Record Types:**
The codebase implements comprehensive STDF V4 specification support with record types:
- `Far.cs` - File Attributes Record
- `Mir.cs` - Master Information Record
- `Mrr.cs` - Master Results Record
- `Pir.cs` - Part Information Record
- `Prr.cs` - Part Results Record
- `Ptr.cs` - Parametric Test Record
- `Ftr.cs` - Functional Test Record
- `Wir.cs` - Wafer Information Record
- `Wrr.cs` - Wafer Results Record
- `Atr.cs` - Audit Trail Record
- `Sbr.cs`, `Sdr.cs`, `Tsr.cs`, `Rdr.cs`, `Pgr.cs`, `Pmr.cs`, `Pcr.cs`, `Bps.cs`, `Eps.cs`, `Dtr.cs`, `Gdr.cs`, `Hbr.cs`, `Bdr.cs`, `Wcr.cs`, `Plr.cs`

**P2020 Log Format:**
- Text-based log files from semiconductor test equipment
- Parser in `CP2020.cs` uses regex pattern matching:
  - Pattern: `^\s*(?<passOrFail>\S+)\s+(?<site>\S+)\s+(?<pinName>.+?)\s+(?<forceValue>\S+)...`
  - Extracts: Pass/Fail status, site number, pin names, force values, limits, measured values, min/max measurements
- Output: STDF records generated from P2020 log data

## Authentication & Identity

**Auth Provider:**
- None - Not applicable for this library and tool suite
- No authentication or identity management required

## Monitoring & Observability

**Error Tracking:**
- None - No external error tracking service

**Logging:**
- Console/Debug output only via `Debug.Assert()` and `System.Diagnostics`
- Custom exception classes for error handling:
  - `StdfException.cs` - Base exception for STDF operations
  - `StdfFormatException.cs` - Format violation errors
  - `InvalidRecordConversionException.cs` - Record conversion failures
  - `NonConsecutiveFieldIndexException.cs` - Field layout violations
  - `FormatErrorRecord.cs`, `CorruptDataRecord.cs`, `ErrorRecord.cs` - Error record types

**Trace Logging:**
- `TraceLogger.cs` in P2020 project - Custom logging implementation for P2020 analysis
- Custom logging wrapper for STDF processing operations

## CI/CD & Deployment

**Hosting:**
- Local machine deployment (desktop application)
- No cloud hosting or remote deployment integration detected

**CI Pipeline:**
- None - No CI/CD configuration files detected (.yml, .xml, Azure Pipelines, etc.)

## Environment Configuration

**Required env vars:**
- None detected - Configuration is hardcoded in application code

**Secrets location:**
- No external secrets management
- No .env files or credential storage

**Application Configuration:**
- Runtime settings in `App.config` files (present but minimal):
  - `WindowsFormsApp1/App.config`
  - `P2020/App.config`
- Windows Forms application settings stored in `Properties/Settings.settings`

## Webhooks & Callbacks

**Incoming:**
- None

**Outgoing:**
- None - No webhook or callback integration

## Binary Data Handling

**Endianness Support:**
- `Endian.cs` - Enum defining endian-ness (Little, Big, Unknown)
- `BinaryReader.cs` - Endian-aware binary reading with byte order reversal
- `BinaryWriter.cs` - Endian-aware binary writing
- `StreamManager.cs` - Stream abstraction for file I/O management
- `RewindableByteStream.cs` - Stream buffering for record parsing with error recovery

**Data Type Mapping (STDF to CLR):**
- Primitive types: byte, ushort, uint, int, float, double, string, BitArray
- STDF-specific types via attributes:
  - `FieldLayoutAttribute.cs` - Basic field mapping
  - `ArrayFieldLayoutAttribute.cs` - Array field handling
  - `NibbleArrayFieldLayoutAttribute.cs` - Half-byte array support
  - `StringFieldLayoutAttribute.cs` - Variable-length string handling
  - `TimeFieldLayoutAttribute.cs` - STDF timestamp handling
  - `FlaggedFieldLayoutAttribute.cs` - Bit flag interpretation

**Record Parsing:**
- Dynamic IL generation at runtime via `RecordConverting/` directory:
  - `ConverterGenerator.cs` - Generates IL for record parsing
  - `UnconverterGenerator.cs` - Generates IL for record serialization
  - `CodeNode.cs`, `ConverterNodes.cs`, `UnconverterNodes.cs` - AST for IL generation
  - Expression trees for compilation and optimization

## Key Data Flows

**STDF File Reading:**
1. `StdfFile` constructor accepts file path
2. `StreamManager` opens FileStream with error handling
3. `BinaryReader` reads binary data with endian awareness
4. `RecordConverterFactory` uses registered converters or dynamically generates IL
5. Records parsed into strongly-typed objects inheriting from `StdfRecord`
6. Records yielded via `IEnumerable<StdfRecord>` for consumption

**STDF File Writing:**
- `StdfFileWriter` writes records back to binary format
- Supports both auto-detected endianness (from FAR record) and explicit endianness
- Handles record validation and STDF V4 specification compliance

**P2020 Log to STDF Conversion:**
1. Text log files read via `File.ReadAllLines()`
2. `CP2020` parser processes log lines with regex extraction
3. `CChipData` objects created from parsed test results
4. `CStdf` converter transforms P2020 data into STDF V4 records
5. `StdfFileWriter` outputs binary STDF file

---

*Integration audit: 2024-12-19*
