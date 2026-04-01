# Codebase Structure

**Analysis Date:** 2024-12-19

## Directory Layout

```
D:\Project\RiderProjects\LinqToStdf\
├── Main\                                    # Solution root
│   ├── Project_Stdf.sln                    # Visual Studio solution file
│   ├── Stdf\                               # Core LINQ-to-STDF library (29 C# files)
│   │   ├── Attributes\                     # Field layout metadata attributes
│   │   ├── Records\                        # STDF record type definitions
│   │   │   └── V4\                        # STDF V4 specification records (29 types)
│   │   ├── RecordConverting\              # Dynamic IL compilation for record parsing
│   │   ├── CompiledQuerySupport\          # Expression analysis for query optimization
│   │   ├── Indexing\                      # Caching and query optimization strategies
│   │   ├── [Core files]                   # BinaryReader, BinaryWriter, StdfFile, Extensions
│   │   ├── Properties\                    # AssemblyInfo, documentation XML
│   │   └── bin\, obj\                     # Build artifacts
│   ├── P2020\                              # P2020 test data analysis library (9 C# files)
│   │   ├── CP2020.cs                      # P2020 log file parser
│   │   ├── CChipData.cs                   # Parsed chip measurement data model
│   │   ├── CFileParam.cs                  # Summary file parameter parser
│   │   ├── CStdf.cs                       # P2020-to-STDF conversion orchestrator
│   │   ├── IAnalyze.cs                    # Analysis interface
│   │   ├── TraceLogger.cs                 # Error logging
│   │   ├── Program.cs                     # Possible alternate entry point
│   │   ├── Properties\                    # AssemblyInfo
│   │   └── bin\, obj\, [forms]            # Build artifacts, WinForms code
│   ├── WindowsFormsApp1\                  # WinForms UI application
│   │   ├── frmMain.cs                     # Main form with event handlers
│   │   ├── frmMain.Designer.cs            # Auto-generated form layout
│   │   ├── Program.cs                     # Application entry point (STAThread)
│   │   ├── Properties\                    # AssemblyInfo, settings, resources
│   │   ├── Data\                          # Sample test data files
│   │   └── bin\, obj\                     # Build artifacts
│   ├── packages\                          # NuGet packages (MSBuild.ILMerge.Task)
│   └── Backup\                            # Older solution file backup
├── .planning\
│   └── codebase\                          # (This directory - analysis output)
├── .git\                                  # Git repository
└── README.md
```

## Directory Purposes

**Stdf\ (Core Library - Class Library)**
- Purpose: Foundational LINQ provider for STDF file parsing
- Contains: Record types, binary I/O, IL code generation, LINQ extensions, filtering, indexing
- Key files: `StdfFile.cs`, `StdfRecord.cs`, `BinaryReader.cs`, `Extensions.cs`
- Output: `Stdf.dll` (targeting .NET 4.5.2)
- Assembly Name: `Stdf`

**P2020\ (Analysis Library - Class Library)**
- Purpose: Business logic for converting P2020 test logs to STDF format
- Contains: Log parsers, parameter extractors, STDF writer integration, data models
- Key files: `CP2020.cs`, `CStdf.cs`, `CFileParam.cs`, `IAnalyze.cs`
- Output: `CSTDF.dll` (targeting .NET 4.7.2)
- Assembly Name: `CSTDF`
- Dependencies: Stdf.dll

**WindowsFormsApp1\ (UI Application - WinExe)**
- Purpose: User interface for P2020-to-STDF conversion workflow
- Contains: WinForms main form, event handlers, sample data
- Key files: `frmMain.cs`, `Program.cs`
- Output: `WindowsFormsApp1.exe` (targeting .NET 4.7.2)
- Dependencies: CSTDF.dll → Stdf.dll

**Stdf\Attributes\**
- Purpose: Metadata attributes specifying binary record structure
- Key files:
  - `FieldLayoutAttribute.cs` - Base attribute for field specification
  - `ArrayFieldLayoutAttribute.cs` - Array elements with length tracking
  - `StringFieldLayoutAttribute.cs` - Variable-length strings
  - `FlaggedFieldLayoutAttribute.cs` - Conditional field presence via flag bits
  - `TimeFieldLayoutAttribute.cs` - Unix timestamp fields
  - `NibbleArrayFieldLayoutAttribute.cs` - 4-bit packed arrays
  - `DependencyProperty.cs` - Field dependency tracking

**Stdf\Records\**
- Purpose: STDF record type definitions
- Structure:
  - `CorruptDataRecord.cs`, `ErrorRecord.cs`, `FormatErrorRecord.cs` - Error handling
  - `StartOfStreamRecord.cs`, `EndOfStreamRecord.cs` - Stream markers
  - `SkippedRecord.cs` - For unrecognized record types
  - `V4\*.cs` - STDF V4 record types (e.g., Mir, Far, Ptr, Wrr, Prr, Hbr, Sbr, etc.)
- All inherit from `StdfRecord` base class

**Stdf\Records\V4\**
- Purpose: STDF Version 4 record type implementations
- Content: 29 record types including:
  - Test execution records: `Mir`, `Mrr` (test start/end)
  - Wafer records: `Wir`, `Wrr` (wafer start/end)
  - Part records: `Pir`, `Prr` (part start/end)
  - Parametric test records: `Ptr` (test results)
  - Bin summary records: `Hbr`, `Sbr` (histogram/summary bins)
  - Test summary records: `Tsr` (test info)
  - And more (Pcr, Mpr, Dtr, etc.)

**Stdf\RecordConverting\**
- Purpose: Dynamic IL code generation for binary record parsing
- Key files:
  - `ConverterGenerator.cs` - Generates IL converters from attributes
  - `UnconverterGenerator.cs` - Generates IL unconverters (object → binary)
  - `CodeNode.cs`, `ConverterNodes.cs`, `UnconverterNodes.cs` - AST for IL generation
  - `CodeNodeVisitor.cs` - Visitor pattern for AST traversal
  - `ConverterEmittingVisitor.cs` - Converts AST to IL code
  - `UnconverterEmittingVisitor.cs` - Converts AST to serialization IL
  - `PrettyPrintVisitor.cs` - Debug visualization of generated code
  - `ConverterLog.cs` - Logging for code generation

**Stdf\CompiledQuerySupport\**
- Purpose: Expression tree analysis for query optimization
- Key files:
  - `ExpressionInspector.cs` - Analyzes LINQ expression trees to extract used records/fields
  - `RecordsAndFields.cs` - Holds record type and field names required by a query

**Stdf\Indexing\**
- Purpose: Caching and query optimization strategy abstraction
- Key files:
  - `IIndexingStrategy.cs` - Strategy interface with implementations
  - `NonCachingStrategy.cs` - Streaming mode (records consumed once)
  - `SimpleIndexingStrategy.cs` - Full-file in-memory caching
  - `V4StructureIndexingStrategy.cs` - V4-specific indexing (if used)
  - `TypeHelper.cs` - Reflection utilities for indexing

**P2020\**
- Purpose: P2020-specific test data format support
- Contains:
  - `CP2020.cs` - Regex-based parser for P2020 test log format
  - `CChipData.cs` - Data model for parsed test measurements
  - `CFileParam.cs` - Summary file parser for test parameters
  - `CStdf.cs` - Orchestrator that writes parsed data as STDF records
  - `IAnalyze.cs` - Common analysis interface
  - `TraceLogger.cs` - Error logging to console/file
  - `frmMain.cs` - WinForms form (nested in WindowsFormsApp1, but listed in P2020.csproj)

**WindowsFormsApp1\**
- Purpose: WinForms UI entry point
- Contains:
  - `frmMain.cs` - Main form with button click handlers
  - `Program.cs` - Application startup (STAThread entry point)
  - `Data\` - Sample P2020 log files for testing

## Key File Locations

**Entry Points:**
- `D:\Project\RiderProjects\LinqToStdf\Main\WindowsFormsApp1\Program.cs` - UI application startup
- `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\StdfFile.cs` - LINQ query entry point
- `D:\Project\RiderProjects\LinqToStdf\Main\P2020\CStdf.cs` - File conversion orchestrator

**Configuration:**
- `D:\Project\RiderProjects\LinqToStdf\Main\Project_Stdf.sln` - Solution configuration
- `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\Stdf.csproj` - Stdf library project config
- `D:\Project\RiderProjects\LinqToStdf\Main\P2020\P2020.csproj` - P2020 library project config
- `D:\Project\RiderProjects\LinqToStdf\Main\WindowsFormsApp1\WindowsFormsApp1.csproj` - UI project config

**Core Logic:**
- `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\StdfFile.cs` - Main query class
- `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\StdfRecord.cs` - Base record class
- `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\BinaryReader.cs` - Binary deserialization
- `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\BinaryWriter.cs` - Binary serialization
- `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\RecordConverterFactory.cs` - Converter compilation
- `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\Extensions.cs` - LINQ extension methods
- `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\BuiltInFilters.cs` - Record stream filters
- `D:\Project\RiderProjects\LinqToStdf\Main\P2020\CP2020.cs` - P2020 log parser
- `D:\Project\RiderProjects\LinqToStdf\Main\P2020\CStdf.cs` - Conversion coordinator

**Testing:**
- `D:\Project\RiderProjects\LinqToStdf\Main\WindowsFormsApp1\Data\*` - Sample STDF/log files

## Naming Conventions

**Files:**
- Record types: PascalCase, single letter or short acronym (e.g., `Mir.cs`, `Ptr.cs`, `Wrr.cs`)
- Record containers: PrefixedPascalCase matching domain (e.g., `CP2020.cs`, `CChipData.cs`, `CFileParam.cs`)
- Attributes: DescriptivePascalCase with "Attribute" suffix (e.g., `FieldLayoutAttribute.cs`, `TimeFieldLayoutAttribute.cs`)
- Utilities: DescriptivePascalCase (e.g., `BinaryReader.cs`, `Extensions.cs`, `SeekAlgorithms.cs`)
- Visitors: DescriptivePascalCase with "Visitor" suffix (e.g., `CodeNodeVisitor.cs`, `ConverterEmittingVisitor.cs`)

**Directories:**
- Domain areas: PascalCase plural or category-based (e.g., `Attributes\`, `Records\`, `RecordConverting\`, `CompiledQuerySupport\`, `Indexing\`)
- Record versions: Abbreviated name matching spec (e.g., `V4\` for STDF V4 records)
- Nested logic: Named for responsibility (e.g., `RecordConverting\` contains converter code generation)

**Namespaces:**
- Root: `Stdf` (library root)
- Nested: `Stdf.[Feature]` where Feature matches directory (e.g., `Stdf.Records`, `Stdf.Records.V4`, `Stdf.RecordConverting`, `Stdf.Indexing`)
- Business: `STDF` (P2020 analysis - note different casing) or `CSTDF` (assembly name)

**Classes:**
- Records: STDF acronyms (Mir, Ptr, Wrr, Prr) or descriptive (StdfRecord, UnknownRecord)
- Context/Manager: `StdfFile`, `StreamManager`, `RecordConverterFactory`
- Attributes: `FieldLayoutAttribute`, `ArrayFieldLayoutAttribute`
- Visitors/Generators: `ConverterGenerator`, `CodeNodeVisitor`, `ConverterEmittingVisitor`
- Domain objects in P2020: `CP2020`, `CChipData`, `CFileParam`, `CStdf` (C prefix indicates "converter" or business class)

**Methods:**
- Query methods: `GetRecords()`, `GetMir()`, `GetPrrs()`, `OfExactType<T>()`
- Parsing methods: `AnalyzeFile()`, `ParseRecord()`, `ConvertRecord()`
- Filter methods: `AddFilter()`, `Chain()`
- Factory methods: `Create()`, `Register()`
- Visitors: `Visit()`, `VisitNode()`

## Where to Add New Code

**New STDF Record Type (V4 or future spec):**
1. Create file: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\Records\[V#]\[RecordName].cs`
2. Inherit from `StdfRecord` abstract class
3. Implement `RecordType` property with type/subtype bytes
4. Decorate properties with `[FieldLayout*]` attributes per STDF spec
5. Register in specification class: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\StdfV4Specification.cs` (see `RegisterRecords()` method)
6. Add LINQ shortcuts to `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\Extensions.cs` if commonly queried

**New Record Filter/Pipeline Stage:**
1. Implement as static property in `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\BuiltInFilters.cs`
2. Return `RecordFilter` delegate: `input => input.Where(...).Select(...)`
3. Add XML documentation with filter semantics
4. Test chaining with existing filters via `.Chain()` extension

**New P2020 Log Format Variant:**
1. Modify regex pattern in `D:\Project\RiderProjects\LinqToStdf\Main\P2020\CP2020.cs` (see `_dataLineRegex`)
2. Extend `CChipData` properties in `D:\Project\RiderProjects\LinqToStdf\Main\P2020\CChipData.cs` if new fields needed
3. Update `CStdf` STDF writing logic in `D:\Project\RiderProjects\LinqToStdf\Main\P2020\CStdf.cs` to map new fields to STDF records

**New LINQ Extension Method:**
1. Add public static method to `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\Extensions.cs`
2. Extend `IEnumerable<StdfRecord>` or specific record type
3. Use `IQueryable<T>` overload for provider-based optimization if needed
4. Add XML documentation with usage examples

**New Query Optimization Strategy:**
1. Create class implementing `IIndexingStrategy` in `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\Indexing\`
2. Implement `CacheRecords()` for caching policy
3. Implement `TransformQuery()` for expression optimization
4. Expose via `StdfFile.IndexingStrategy` property (assign before iteration)

**New UI Feature (WinForms):**
1. Edit form in Visual Studio designer or manually in `D:\Project\RiderProjects\LinqToStdf\Main\WindowsFormsApp1\frmMain.Designer.cs`
2. Add event handler in `D:\Project\RiderProjects\LinqToStdf\Main\WindowsFormsApp1\frmMain.cs`
3. Call P2020 library classes to execute business logic

## Special Directories

**Stdf\bin\Debug\, bin\Release\:**
- Purpose: Compiler output directory
- Generated: Yes (MSBuild output)
- Committed: No (in .gitignore)
- Contains: `Stdf.dll`, `Stdf.xml` (documentation)

**Stdf\obj\:**
- Purpose: Intermediate build artifacts
- Generated: Yes (MSBuild)
- Committed: No
- Contains: .obj files, temporary compilation state

**Stdf\Properties\:**
- Purpose: Project metadata
- Generated: Partially (AssemblyInfo auto-updated on build)
- Committed: Yes
- Contains: `AssemblyInfo.cs`

**P2020\bin\, P2020\obj\:**
- Purpose: P2020 library build output
- Generated: Yes
- Committed: No
- Contains: `CSTDF.dll` (assembly name differs from project name)

**WindowsFormsApp1\Data\:**
- Purpose: Sample P2020 test log files for manual testing
- Generated: No (static test data)
- Committed: Yes (checked in with repository)
- Contains: `2026-04-01-16-39-45.log`, `2026-04-01-16-39-45_Summary.log` (sample data)

**WindowsFormsApp1\2026-04-01-16-39-45\:**
- Purpose: Output directory for generated STDF files (from sample data)
- Generated: Yes (by application run)
- Committed: Partially (may be checked in with sample output)
- Contains: Generated `.stdf` files

**packages\MSBuild.ILMerge.Task.1.1.3\:**
- Purpose: NuGet dependency for IL merging (possibly for assembly consolidation)
- Generated: Yes (by NuGet restore)
- Committed: No (packages excluded)
- Contains: ILMerge task and tools

---

*Structure analysis: 2024-12-19*
