# Architecture

**Analysis Date:** 2024-12-19

## Pattern Overview

**Overall:** LINQ-based Streaming Data Parser with Dynamic IL Compilation

**Key Characteristics:**
- LINQ-to-Objects provider enabling strongly-typed queries over binary STDF (Standard Test Data Format) files
- Dynamic IL code generation for high-performance record binary deserialization
- Streaming architecture with lazy evaluation for memory efficiency
- Attribute-based metadata-driven record specification and conversion
- Pluggable filtering, indexing, and serialization strategies

## Layers

**Library Layer (Stdf project):**
- Purpose: Core LINQ-to-STDF parsing engine providing the foundational library
- Location: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\`
- Contains: Binary I/O, record type definitions, IL-based converters, filtering, indexing, LINQ extensions
- Depends on: System (Core BCL), System.Core (LINQ, Reflection.Emit)
- Used by: P2020, WindowsFormsApp1

**Business Logic Layer (P2020 project):**
- Purpose: Domain-specific analysis layer for P2020 test data format parsing and STDF file generation
- Location: `D:\Project\RiderProjects\LinqToStdf\Main\P2020\`
- Contains: P2020 log file parser (CP2020), file parameter parser (CFileParam), STDF writer integration (CStdf), chip data models
- Depends on: Stdf library (records, file I/O, writer)
- Used by: WindowsFormsApp1

**Presentation Layer (WindowsFormsApp1 project):**
- Purpose: WinForms UI for invoking P2020-to-STDF conversion workflow
- Location: `D:\Project\RiderProjects\LinqToStdf\Main\WindowsFormsApp1\`
- Contains: Main form with button handlers for file conversion workflow
- Depends on: P2020 library
- Used by: End users

## Data Flow

**P2020 Log to STDF Conversion Pipeline:**

1. User clicks "GenerateSTDF" button → triggers `frmMain.GenerateStdfButton_Click()`
2. UI instantiates `CStdf` with source log folder and output path
3. `CStdf.DoWork()` orchestrates the analysis pipeline:
   - Loads all `*.log` files from source directory (excluding *Summary* files)
   - Instantiates `CP2020` with log file paths
   - `CP2020.AnalyzeFile()` parses each log using regex patterns to extract test data
     - Regex: `_dataLineRegex` extracts: passOrFail, site, pinName, forceValue, limits, measurements
     - Populates `CChipData` collection with parsed test measurements
   - Locates and parses `CFileParam` from summary file (*.txt or *Summary*.log)
   - `CFileParam.AnalyzeFile()` extracts test metadata/parameters
   - `StdfFileWriter` writes collected data as STDF V4 binary records
4. STDF file written to output path

**STDF File Reading Pipeline:**

1. User creates `StdfFile` instance with file path
2. Static initialization registers V4 record converters via `StdfV4Specification.RegisterRecords()`
3. User iterates STDF records or applies LINQ queries
4. Record iteration triggers:
   - `StreamManager.GetScope()` returns stream wrapper for resource management
   - `BinaryReader` reads record headers and payload
   - `RecordConverterFactory` looks up converter for record type (T, SubT)
   - Dynamic converter IL executes deserialization based on attribute metadata
   - Record object returned with properties populated
5. Filters applied (if registered) to stream (e.g., caching, format validation, summary reconstruction)
6. Results returned to caller

**State Management:**

- **File State:** `StdfFile` holds reference to `IStdfStreamManager` (scoped stream lifetime management)
- **Converter State:** `RecordConverterFactory` caches converter delegates for each record type (compiled IL)
- **Indexing State:** `IIndexingStrategy` controls caching behavior (simple list cache or non-caching)
- **Record State:** Each `StdfRecord` maintains `Offset` and `Synthesized` flags via bit-packed `ulong`
- **Parse Context:** Records reference parent `StdfFile` for cross-record navigation (e.g., `Wrr.GetPrrs()`)

## Key Abstractions

**StdfFile:**
- Purpose: Main entry point and query context for STDF parsing
- Examples: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\StdfFile.cs`
- Pattern: Implements `IRecordContext` to provide record stream and enable LINQ extensions
- Responsibilities: Stream lifecycle, record filter chain management, endian detection, indexing strategy selection

**StdfRecord (abstract):**
- Purpose: Base class for all STDF record types
- Examples: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\Records\V4\*.cs` (Mir, Ptr, Wrr, Prr, etc.)
- Pattern: Abstract base with concrete subclasses for each STDF V4 record type
- Responsibilities: Record type identification, offset/synthesis tracking, context reference

**IRecordContext:**
- Purpose: Abstraction for objects providing access to owning `StdfFile`
- Examples: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\IRecordContext.cs`
- Pattern: Marker interface enabling LINQ extension methods on both `StdfFile` and individual records
- Responsibilities: Provides `.StdfFile` property for cross-record queries

**RecordConverterFactory:**
- Purpose: Manages dynamic IL compilation of record binary deserializers
- Examples: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\RecordConverterFactory.cs`
- Pattern: Factory with caching of compiled delegates; attribute-driven code generation
- Responsibilities: Analyze record class attributes, emit IL converters/unconverters, manage delegate lifecycle

**FieldLayoutAttribute (and subtypes):**
- Purpose: Metadata defining binary record field layouts and serialization behavior
- Examples: 
  - `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\Attributes\FieldLayoutAttribute.cs` (base)
  - `StringFieldLayoutAttribute` (variable-length strings)
  - `ArrayFieldLayoutAttribute` (typed arrays)
  - `FlaggedFieldLayoutAttribute` (conditional field presence)
  - `TimeFieldLayoutAttribute` (Unix timestamp conversion)
- Pattern: Class-level attributes describing field order, types, optional/required, missing-value semantics
- Responsibilities: Declarative specification of binary structure without hand-coded parsing

**IStdfStreamManager:**
- Purpose: Abstract stream lifecycle and file access policies
- Examples: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\StreamManager.cs`
- Pattern: Scope-based resource management; factory for stream wrappers
- Responsibilities: Provide streams with defined lifecycle (file, memory, compressed); handle disposal

**IIndexingStrategy:**
- Purpose: Control caching and query optimization strategies
- Examples: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\Indexing\IIndexingStrategy.cs`
- Pattern: Strategy pattern with `NonCachingStrategy` and `SimpleIndexingStrategy` implementations
- Responsibilities: Cache records on demand, transform LINQ expressions for optimization

**RecordFilter (delegate):**
- Purpose: Composable pipeline for stream transformation
- Examples: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\BuiltInFilters.cs`
- Pattern: Higher-order function pattern; chainable filters
- Responsibilities: Reconstruct missing summary records, enforce V4 spec ordering, handle errors

**BinaryReader/BinaryWriter:**
- Purpose: Endian-aware binary data I/O with STDF-specific types
- Examples: 
  - `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\BinaryReader.cs`
  - `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\BinaryWriter.cs`
- Pattern: Wrapper over `System.IO.Stream` with STDF data type knowledge
- Responsibilities: Handle endian conversion, parse/write time fields, variable-length strings, bit arrays

## Entry Points

**StdfFile Constructor:**
- Location: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\StdfFile.cs` lines 72-85
- Triggers: User code: `new StdfFile(filePath)`
- Responsibilities: 
  - Create stream manager for file access
  - Initialize V4 converter factory
  - Prepare for record parsing

**GetRecords() Method:**
- Location: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\StdfFile.cs` (partial, not shown in excerpt)
- Triggers: User code: `stdfFile.GetRecords()` or `stdfFile.GetRecords().OfExactType<T>()`
- Responsibilities:
  - Open stream via manager
  - Iterate records using binary reader
  - Convert each record via factory
  - Apply filter chain
  - Return IEnumerable of records

**LINQ Extension Methods:**
- Location: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\Extensions.cs`
- Examples: `OfExactType<T>()`, `GetMir()`, `GetPrrs()`, `GetPcrs()`
- Responsibilities: Provide convenient strongly-typed queries over record stream

**CompiledQuery.Compile<T>():**
- Location: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\CompiledQuery.cs` lines 43, 53
- Triggers: User code: `CompiledQuery.Compile<ResultType>(stdfFile => query)`
- Responsibilities:
  - Analyze LINQ expression tree for required records/fields
  - Generate factory with minimal field parsing
  - Return function accepting file path → result

**P2020 Analysis Entry:**
- Location: `D:\Project\RiderProjects\LinqToStdf\Main\P2020\CStdf.cs` lines 29-37
- Triggers: UI: `new CStdf(inputFolder, outputPath)` → `.DoWork()`
- Responsibilities:
  - Orchestrate log file discovery and parsing
  - Coordinate CP2020 and CFileParam analysis
  - Write STDF records via StdfFileWriter

## Error Handling

**Strategy:** Dual-mode error handling with configurable strictness

**Patterns:**

1. **Strict Mode (default):** `ThrowOnFormatError = true`
   - Throws `StdfFormatException` on parsing errors
   - Throws `InvalidRecordConversionException` on conversion failures
   - Stream parsing halts

2. **Tolerant Mode:** `ThrowOnFormatError = false`
   - Injects error records into stream (`FormatErrorRecord`, `V4ContentErrorRecord`)
   - Attempts recovery and continuation
   - Error records processed through normal filter/indexing pipeline

3. **Validation Filters:**
   - `BuiltInFilters.ThrowOnV4ContentError` - strict enforcement of STDF V4 spec ordering
   - `BuiltInFilters.V4ContentSpec` - inject content error records instead of throwing
   - Location: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\BuiltInFilters.cs`

4. **Custom Exception Types:**
   - `StdfException` (base)
   - `StdfFormatException` (binary read/write failures)
   - `InvalidRecordConversionException` (attribute-to-record conversion failures)
   - `NonConsecutiveFieldIndexException` (attribute metadata validation)
   - Locations: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\Stdf*Exception.cs`

## Cross-Cutting Concerns

**Logging:** 
- P2020 layer uses text logging: `TraceLogger` writes to console and log files
- Location: `D:\Project\RiderProjects\LinqToStdf\Main\P2020\TraceLogger.cs`
- Stdf library has minimal logging (debug via exception messages)

**Validation:** 
- Binary structure: Validated by attribute metadata at converter factory registration time
- Record instances: Validated against STDF V4 spec ordering via optional filters
- File format: Detected via endian flag in FAR (File Access Record)
- Data ranges: Checked during binary read (e.g., offset must fit in 62 bits)

**Authentication:** Not applicable (file-based processing, no external APIs)

**Concurrency:** 
- Thread-safe: `StdfFile.IndexingStrategy` uses lock-based synchronization
- Not thread-safe: Individual `StdfFile` instances not designed for concurrent iteration
- Safe to share: `RecordConverterFactory` is thread-safe via delegate caching

**Performance Optimization:**
- **Compiled Queries:** `CompiledQuery.Compile()` analyzes expression trees to skip unneeded fields and records
  - Example: Query only `Ptr` records with `Result > 0.5` → converter parses only those fields from those records
  - Expression analysis: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\CompiledQuerySupport\ExpressionInspector.cs`
  - IL generation: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\RecordConverting\ConverterGenerator.cs`

- **Caching Strategies:** `IIndexingStrategy` enables full-file caching for repeated queries
  - `SimpleIndexingStrategy`: Reads entire file into memory on first query
  - `NonCachingStrategy`: Streaming mode (default for compiled queries)

- **IL Code Generation:** Converters compiled to native IL at first use, then cached by record type
  - Dynamic generation: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\RecordConverting\ConverterGenerator.cs`, `UnconverterGenerator.cs`
  - IL helpers: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\ILGenHelpers.cs`

---

*Architecture analysis: 2024-12-19*
