# Testing Patterns

**Analysis Date:** 2024-12-19

## Test Framework

**Status:** No formal testing framework detected in codebase.

**Finding:**
- No test projects (*.Test.csproj, *.Tests.csproj) found in solution
- No test files (*Test.cs, *Tests.cs, *Spec.cs) present in directory tree
- No NUnit, xUnit, MSTest, or Moq references in .csproj files
- Solution contains only 3 projects:
  - `Stdf/Stdf.csproj` - Core STDF library
  - `P2020/P2020.csproj` - Test data processor
  - `WindowsFormsApp1/WindowsFormsApp1.csproj` - WinForms UI

**Compiler Configuration:**
- Project targets .NET Framework 4.5.2 (Stdf library)
- WarningLevel: 4 (strict)
- DocumentationFile: `bin\Stdf.xml` (generated for code analysis)
- NoWarn: 1591 (suppresses missing XML comment warning)
- CodeAnalysisRuleSet: AllRules.ruleset

## Manual Testing Approach

**Evidence:**
The P2020 project (`D:\Project\RiderProjects\LinqToStdf\Main\P2020\`) appears to serve as manual test/sample code rather than automated testing:

- `CP2020.cs` - Parses P2020 test equipment logs
- `CFileParam.cs` - Extracts test parameters from summary files
- `CChipData.cs` - Structures test result data
- `CStdf.cs` - Converts parsed data to STDF format
- Sample data files: `Data/2026-04-01-16-39-45.log` and `Data/2026-04-01-16-39-45_Summary.log`

**Manual Testing Pattern:**
```csharp
// From frmMain.cs - WindowsFormsApp1
private void GenerateStdfButton_Click(object sender, EventArgs e)
{
    string inputFolder = @"D:\Pti_Doc\AutoScan Log\2026-04-01-16-39-45";
    string outputPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
        "testSTDF.stdf");
    
    try
    {
        CStdf stdfConverter = new CStdf(inputFolder, outputPath);
        stdfConverter.DoWork();
    }
    catch(Exception ex)
    {
        LogEntryPointException("GenerateStdfButton_Click", ex, inputFolder, outputPath);
        throw;
    }
}
```

## Verification Mechanisms

**Error Handling as Verification:**
- Exceptions thrown immediately on validation failures
- `ArgumentNullException` for null parameters
- `ArgumentOutOfRangeException` for invalid ranges
- Custom exceptions (`InvalidRecordConversionException`, `StdfFormatException`) for domain errors
- `Debug.Assert()` for internal contracts

**Example from RecordConverterFactory.cs:**
```csharp
public ConverterGenerator(ILGenerator ilgen, Type type, HashSet<string> fields)
{
    _ILGen = ilgen ?? throw new ArgumentNullException("ilgen");
    _Type = type ?? throw new ArgumentNullException("type");
    
    if(fields != null)
    {
        _Fields = new HashSet<string>(fields);
    }
}
```

**Logging as Verification:**
- `Console.Error.WriteLine()` used for error logging
- `TraceLogger.WriteLine()` writes structured logs with timestamps
- Error logs include context: operation, stage, file paths, exception messages

**Example from TraceLogger.cs:**
```csharp
public static void WriteLine(string message)
{
    string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}";
    
    try
    {
        string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        string logFilePath = Path.Combine(logDirectory, $"stdf-trace-{DateTime.Now:yyyy-MM-dd}.log");
        
        lock(SyncRoot)
        {
            Directory.CreateDirectory(logDirectory);
            File.AppendAllText(logFilePath, line + Environment.NewLine);
        }
    }
    catch(Exception ex)
    {
        Console.Error.WriteLine(line);
        Console.Error.WriteLine($"[STDF-TRACE-ERR] op=TraceLogger.WriteLine target=\"daily-log-file\" message=\"{ex.Message}\" stack=\"{ex.StackTrace}\"");
        return;
    }
    
    Console.Error.WriteLine(line);
}
```

## Test Coverage - Gap Analysis

**Completely Untested Modules:**

**BinaryReader.cs / BinaryWriter.cs:**
- Files: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\BinaryReader.cs`, `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\BinaryWriter.cs`
- Functionality: Binary stream reading/writing with endian support
- Methods not covered: `ReadByte()`, `ReadByteArray()`, `ReadNibbleArray()`, `ReadBitArray()`, `ReadUInt16()`, `ReadDouble()`, `WriteUInt16()`, `WriteBitArray()`, etc.
- Risk: **HIGH** - Core I/O operations; bugs would corrupt STDF file parsing
- Suggestion: Needs unit tests for each Read/Write method with various endian configurations

**RecordConverterFactory.cs:**
- Files: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\RecordConverterFactory.cs`
- Functionality: Dynamic IL generation for record converters
- Risk: **HIGH** - Complex reflection/emit code; no visibility into generated IL
- Suggestion: Integration tests that verify generated converters produce correct objects

**CompiledQuerySupport/ExpressionInspector.cs:**
- Files: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\CompiledQuerySupport\ExpressionInspector.cs`
- Functionality: LINQ expression tree analysis
- Risk: **MEDIUM** - Expression visitor pattern; subtle bugs in tree traversal possible
- Suggestion: Unit tests with various expression patterns (Where, Select, GroupBy)

**StdfFile.cs:**
- Files: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\StdfFile.cs`
- Functionality: Main STDF file parsing orchestration, filtering, indexing
- Risk: **HIGH** - Entry point; complex locking and state management
- TODO comments indicate known concerns: `//TODO: get this locking pattern right` (line 153), `//TODO: prevent this from changing` (line 168)
- Suggestion: Thread-safety tests, concurrent access scenarios

**Record Types (Atr.cs, Mir.cs, Prr.cs, etc.):**
- Files: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\Records\V4\*.cs`
- Functionality: STDF record definitions with attribute-based parsing
- Risk: **MEDIUM** - Schema compliance; incorrect field mapping would break parsing
- Suggestion: Roundtrip tests (parse → serialize → parse, verify equality)

**SeekAlgorithms.cs:**
- Files: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\SeekAlgorithms.cs`
- TODO comment: `//TODO: did I get this right?` (line 92)
- Risk: **MEDIUM** - Stream seeking logic; incorrect offsets cause data loss
- Suggestion: Tests with various file sizes and seek patterns

**ILGenHelpers.cs:**
- Files: `D:\Project\RiderProjects\LinqToStdf\Main\Stdf\ILGenHelpers.cs`
- Functionality: IL code generation utilities
- TODO comment: `//TODO:resource` (line 124)
- Risk: **MEDIUM** - Generates executable code; bugs hard to diagnose
- Suggestion: IL verification tests, generated method functional tests

**P2020 Project Logic:**
- Files: `D:\Project\RiderProjects\LinqToStdf\Main\P2020\CP2020.cs`, `D:\Project\RiderProjects\LinqToStdf\Main\P2020\CFileParam.cs`, `D:\Project\RiderProjects\LinqToStdf\Main\P2020\CChipData.cs`
- Functionality: Test equipment log parsing and STDF conversion
- Risk: **HIGH** - Application logic; used to generate production STDF files
- Suggestion: Unit tests for log parsing, file parameter extraction; integration tests with sample data files

## Known Testing Gaps

**Priority: HIGH - Must Add Before Production Release:**

1. **BinaryReader/Writer Roundtrip Tests**
   - Parse binary data, verify correct values read
   - Write values, verify bytes written correctly
   - Test both Little and Big Endian

2. **STDF Record Roundtrip Tests**
   - Create record with known data
   - Convert to binary and back to object
   - Verify data integrity after conversion
   - Test optional field handling

3. **P2020 Integration Tests**
   - Use sample data files (already present in codebase)
   - Run CStdf.DoWork() with real log files
   - Verify output STDF file structure
   - Validate record types and field values

**Priority: MEDIUM - Add for Robustness:**

4. **Concurrent Access Tests**
   - Multiple threads parsing same file
   - Verify IndexingStrategy locking works correctly
   - Test filter modification during iteration

5. **Expression Tree Tests**
   - CompiledQuery with various LINQ expressions
   - Verify ExpressionInspector correctly identifies records/fields
   - Performance benchmarks

6. **Error Recovery Tests**
   - Malformed STDF files with ThrowOnFormatError = false
   - Verify FormatErrorRecord generated correctly
   - Test continued parsing after errors

**Priority: LOW - Nice to Have:**

7. **Performance Tests**
   - Large file parsing (> 1GB STDF files)
   - Memory usage profiling
   - Seek algorithm performance

## Test Data

**Existing Sample Data:**
- `D:\Project\RiderProjects\LinqToStdf\Main\WindowsFormsApp1\Data\2026-04-01-16-39-45.log`
- `D:\Project\RiderProjects\LinqToStdf\Main\WindowsFormsApp1\Data\2026-04-01-16-39-45_Summary.log`

These files are hardcoded in `frmMain.cs` and represent actual P2020 test equipment output. Could be used as integration test fixtures.

## Recommended Testing Setup

**Framework Choice:**
- **Recommended:** xUnit with Xunit.Abstractions for output logging
  - Modern, supports async (future-proofing)
  - Good integration with Visual Studio
  - Strong isolation model
  - Can run tests in parallel
  
- **Alternative:** NUnit
  - More features (if needed)
  - Slightly more verbose

**Project Structure:**
```
LinqToStdf/
├── Main/
│   ├── Stdf/               # Core library
│   ├── Stdf.Tests/         # NEW: Unit tests
│   ├── P2020/              # Test data processor
│   ├── P2020.Tests/        # NEW: Integration tests
│   └── WindowsFormsApp1/   # UI
└── .planning/codebase/
```

**Test File Naming:**
- `BinaryReaderTests.cs` for unit tests
- `CStdfIntegrationTests.cs` for integration tests
- Test methods follow pattern: `MethodName_Scenario_ExpectedBehavior`
- Example: `ReadByte_AtEndOfStream_ThrowsEndOfStreamException()`

**Example Test Structure:**
```csharp
public class BinaryReaderTests : IDisposable
{
    private MemoryStream _stream;
    private BinaryReader _reader;
    
    public BinaryReaderTests()
    {
        _stream = new MemoryStream();
        _reader = new BinaryReader(_stream);
    }
    
    [Fact]
    public void ReadByte_ValidByte_ReturnsCorrectValue()
    {
        // Arrange
        _stream.WriteByte(0x42);
        _stream.Position = 0;
        
        // Act
        byte result = _reader.ReadByte();
        
        // Assert
        Assert.Equal(0x42, result);
    }
    
    [Fact]
    public void ReadByte_AtEndOfStream_ThrowsEndOfStreamException()
    {
        // Arrange
        _stream.Position = 0;
        
        // Act & Assert
        Assert.Throws<EndOfStreamException>(() => _reader.ReadByte());
    }
    
    public void Dispose()
    {
        _reader?.Dispose();
        _stream?.Dispose();
    }
}
```

## Current Testing Limitations

**Known Issues:**
- No CI/CD pipeline observed (no .yml files for GitHub Actions, Azure Pipelines, etc.)
- No code coverage tooling configured
- No automated regression tests
- No test data validation suite
- ThreadAbortException handling not tested (Silverlight-era code)

**Improvements Needed:**
1. Add dedicated test projects to solution
2. Configure CI/CD to run tests on every commit
3. Set coverage target (suggest 70%+ for core parsing logic)
4. Document test execution procedure
5. Create test data generators for various STDF scenarios

---

*Testing analysis: 2024-12-19*
