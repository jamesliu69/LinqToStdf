# Coding Conventions

**Analysis Date:** 2024-12-19

## Naming Patterns

**Files:**
- Class files use PascalCase matching the primary class name: `BinaryReader.cs`, `StdfFile.cs`, `StdfException.cs`
- Windows Forms classes use `frm` prefix for forms: `frmMain.cs`, `frmMain.Designer.cs`
- Generated/designer files use `.Designer.cs` suffix: `frmMain.Designer.cs`, `Resources.Designer.cs`
- Test files (if present) would use `.Test.cs` or `Tests.cs` suffix
- Abbreviation usage for record types: `Atr.cs`, `Mir.cs`, `Prr.cs` (STDF V4 record types)

**Classes and Interfaces:**
- PascalCase for all class names: `StdfFile`, `BinaryReader`, `RecordConverterFactory`
- Exception classes inherit appropriately and follow pattern: `StdfException`, `InvalidRecordConversionException`, `StdfFormatException`
- Interface names use I-prefix: `IRecordContext`, `IStdfStreamManager`, `IAnalyze`, `IHeadIndexable`
- Attribute classes use Attribute suffix: `FieldLayoutAttribute`, `StringFieldLayoutAttribute`, `ArrayFieldLayoutAttribute`
- Delegate types use full descriptor: `RecordFilter` (no "Delegate" suffix)

**Methods and Properties:**
- PascalCase for all public methods: `ReadByte()`, `ReadByteArray()`, `GenerateConverter()`, `AnalyzeFile()`
- camelCase for all parameters: `ilgen`, `type`, `fields`, `stream`, `streamEndian`
- PascalCase for properties: `RecordType`, `AtEndOfStream`, `IsOptional`, `MissingValue`, `FieldIndex`
- Auto-properties use concise syntax: `{ get; set; }`
- Properties with complex logic use expanded block syntax with comments

**Variables:**
- camelCase for local variables: `logFiles`, `headerLine`, `summaryBuilder`, `logLineList`, `convertedValue`
- PascalCase for private fields when used in parameter defaults: `_StreamManager`, `_ILGen`, `_Type`, `_Buffer`, `_OffsetData`
- Underscores prefix private fields: `_Stream`, `_OwnsStream`, `_Fields`, `_StreamEndian`, `_Synthesized`
- Constants use UPPER_SNAKE_CASE: `LogTag = "[STDF-TRACE-ERR]"` (actually const string), `split = "..."`
- Static readonly fields follow similar pattern: `_OffsetMask`, `_SynthesizedMask`, `_V4ConverterFactory`

**Constants:**
- Class-level const strings use camelCase: `const string LogTag = "[STDF-TRACE-ERR]"`
- const fields use UPPER_SNAKE_CASE for numeric/symbol values (when applicable)
- Magic numbers are extracted to const fields with descriptive names

**Namespaces:**
- PascalCase matching folder structure: `namespace Stdf`, `namespace Stdf.Records.V4`, `namespace Stdf.Attributes`
- Folder names match namespace hierarchy: `Stdf/Records/V4/Atr.cs` → `namespace Stdf.Records.V4`
- WinForms projects use project name as namespace: `namespace WindowsFormsApp1`, `namespace STDF`

## Code Style

**Formatting:**
- Indentation: Tab character (visible as 4-space in editor)
- Opening braces on same line (K&R style): `public class StdfFile : IRecordContext {`
- Multi-line method signatures place each parameter on new line with proper indentation
- Long attribute lists span multiple lines, each attribute prefixed on its own line

**Spacing:**
- Single space around binary operators: `value < 0`, `_OffsetData & _OffsetMask`
- No space before method call parentheses: `ReadByte()`, `AtEndOfStream`
- Space after keywords: `if(`, `for(`, `switch(`
- Single blank line between logical sections within methods

**Braces:**
- Required for all control structures, even single statements
- Opening brace on same line, closing on new line
- `if` blocks always use braces, never naked statements

**Line Length:**
- No strict limit observed, but lines typically 80-120 characters
- Longer lines in attributes, comments, and string literals acceptable
- Method signatures break across multiple lines when needed

**Linting:**
- Compiler warning level: 4 (strict)
- Code Analysis RuleSet: AllRules.ruleset (in Debug configuration)
- Documentation warning 1591 (missing XML comment) explicitly disabled: `<NoWarn>1591</NoWarn>`

## Import Organization

**Order:**
1. Project-specific using statements: `using Stdf`, `using Stdf.Records.V4`, `using STDF`
2. System namespaces: `using System`, `using System.Collections`, `using System.Linq`
3. System.* qualified namespaces: `using System.Collections.Generic`, `using System.Linq.Expressions`
4. Platform-specific (conditional): `#if SILVERLIGHT using System.Windows.Controls;`

**Example from StdfFile.cs:**
```csharp
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
```

**Path Aliases:**
- No namespace aliases (using X = Y) observed in codebase
- Direct namespace imports preferred

## Error Handling

**Exception Patterns:**
- Custom exception hierarchy: `StdfException` (base) → `InvalidRecordConversionException`, `StdfFormatException`, `NonConsecutiveFieldIndexException`
- Domain-specific exceptions inherit from appropriate base: `StdfException`, `InvalidOperationException`
- Constructors follow standard pattern with 3 overloads:
  1. No parameters: `public StdfException() {}`
  2. Message only: `public StdfException(string message) : base(message) {}`
  3. Message + inner exception: `public StdfException(string message, Exception inner) : base(message, inner) {}`

**Throwing Patterns:**
- Arguments validated via `ArgumentNullException`: `ilgen ?? throw new ArgumentNullException("ilgen")`
- Arguments validated via `ArgumentOutOfRangeException`: `if(value < 0) throw new ArgumentOutOfRangeException("value", Resources.NegativeFieldIndex)`
- Debug assertions for internal contracts: `Debug.Assert(stream != null, "The provided stream was null")`
- Custom exceptions for domain violations: `throw new InvalidRecordConversionException(string message)`

**Try-Catch Patterns:**
- Try blocks wrap risky operations (file I/O, parsing)
- Catch blocks log via `Console.Error.WriteLine()` or dedicated logger
- Exceptions often re-thrown after logging: `catch(Exception ex) { Log(...); throw; }`
- Some catch blocks suppress exceptions and continue: `catch(Exception ex) { return defaultValue; }`

**Example from CStdf.cs:**
```csharp
try
{
    logFiles = Directory.GetFiles(_logPath, "*.log")
                 .Where(path => Path.GetFileName(path).IndexOf("Summary", StringComparison.OrdinalIgnoreCase) < 0)
                 .ToArray();
}
catch(Exception ex)
{
    LogException("LoadLogFiles", ex, _logPath, null, null, null, null, "P2020LogFiles", "AnalyzeFile.LoadLogFiles", _logPath, _outputPath);
    throw;
}
```

## Null Handling and Validation

**Null Checks:**
- Explicit `null` comparison: `if(fieldValue == null)`, `if(field != null)`
- Null-coalescing operator: `field ?? defaultValue`
- Null checks in constructors with immediate throw: `parameter ?? throw new ArgumentNullException("paramName")`
- Property null checks with ternary: `string message = ex?.Message?.Replace(...)`

**Nullable Types:**
- Properties using nullable value types: `DateTime?`, `ushort?`, `uint?`
- Null represents "missing/invalid" data per STDF spec
- Some properties explicitly allow null: `public DateTime? ModifiedTime { get; set; }`
- Sentinel values (MissingValue) map to null on read: `IsOptional = true, MissingValue = ushort.MaxValue`

**Defensive Patterns:**
- Optional parameter checks: `if(string.IsNullOrWhiteSpace(summaryPath)) { throw new InvalidDataException(...) }`
- Collections checked for empty: `if(logFiles.Length == 0) { throw new InvalidDataException(...) }`
- Stream position validated: `if(AtEndOfStream) { if(i == 0) return null; ... }`

**Example from BinaryReader.cs:**
```csharp
public BitArray ReadBitArray()
{
    int length = ReadUInt16();
    
    if(length == 0)
    {
        return null;  // Null signals no bit array present
    }
    
    int realLength = (length + 7) / 8;
    
    BitArray bitArray = new BitArray(ReadByteArray(realLength))
    {
        Length = length
    };
    return bitArray;
}
```

## Comments and Documentation

**XML Documentation:**
- Public members documented with `/// <summary>` tags
- Parameters documented with `/// <param>` tags
- Return values documented with `/// <returns>` tags
- Remarks documented with `/// <remarks>` tags for complex behavior
- Exception patterns in attributes documented inline
- Multi-line remarks use nested `<para>` tags for clarity

**Inline Comments:**
- Single-line comments explain non-obvious logic: `// indicates we are in seek mode`
- Comments often precede or follow complex code blocks
- TODO comments mark areas needing future work (many examples throughout Stdf library)
- Comments on field attributes explain valid values: `// Known values are: A, C, D, E, M, P, Q, 0-9`

**Example from Mir.cs:**
```csharp
/// <summary>
///     Known values are: A, C, D, E, M, P, Q, 0-9
/// </summary>
public string ModeCode { get; set; }
```

## Async/Await Usage

**Not Observed:** The codebase does not use async/await patterns. All I/O operations (file reading, parsing) are synchronous.

**Reason:** Library targets .NET 4.5.2 (WindowsFormsApp1) and older frameworks, with focus on LINQ query composition and IL generation. Async operations not required by current design.

## Function Design

**Size:**
- Methods range from 2-20 lines for most utilities
- Some complex parsing methods extend to 50+ lines (e.g., `GenerateConverter`)
- Preference for clear, focused methods over nested complexity

**Parameters:**
- Few parameters (1-4) for most methods
- Longer parameter lists use attribute-style documentation
- Out parameters avoided; use return types instead
- Optional parameters used selectively: `public byte[] ReadByteArray(int length, bool throwOnEndOfStream = true)`

**Return Values:**
- Void used for operations with side effects (parsing records, writing files)
- Boolean returned for predicate checks: `ShouldParseField(string field)`
- Collections returned as arrays or IEnumerable: `IEnumerable<TRecord>`, `byte[]`
- Null used to indicate "no value" for reference types: `ReadBitArray()` returns null
- Tuple/KeyValuePair used for multiple returns: `List<KeyValuePair<FieldLayoutAttribute, PropertyInfo>>`

**Example from StdfFile.cs - Multi-line signature:**
```csharp
public StdfFile(IStdfStreamManager streamManager, bool debug, RecordsAndFields recordsAndFields) 
    : this(streamManager, PrivateImpl.None)
{
    if(debug || (recordsAndFields != null))
    {
        ConverterFactory = new RecordConverterFactory(recordsAndFields)
        {
            Debug = debug
        };
        StdfV4Specification.RegisterRecords(ConverterFactory);
    }
    else
    {
        ConverterFactory = new RecordConverterFactory(_V4ConverterFactory);
    }
}
```

## Module Design

**Exports:**
- Public classes form the public API
- Internal classes used for implementation details: `internal class ConverterGenerator`
- Internal static classes used for utilities: `internal static class ExpressionInspector`
- Sealed classes used when inheritance not intended: `public sealed partial class StdfFile`

**Partial Classes:**
- Used for separation of concerns: `StdfFile.cs` is partial (likely split for complexity management)
- Partial keyword used in definition: `public sealed partial class StdfFile`

**Access Modifiers:**
- Public for library entry points and primary types
- Internal for framework classes and helpers
- Private for local implementation details
- Protected rare (single inheritance not emphasized)

**Example from StdfFile.cs:**
```csharp
public sealed partial class StdfFile : IRecordContext
{
    internal static readonly RecordConverterFactory _V4ConverterFactory = new RecordConverterFactory();
    private readonly object _ISLock = new object();
    private readonly IStdfStreamManager _StreamManager;
    
    // ... public interface
}
```

---

*Convention analysis: 2024-12-19*
