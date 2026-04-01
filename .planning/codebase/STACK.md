# Technology Stack

**Analysis Date:** 2024-12-19

## Languages

**Primary:**
- C# - Core implementation for STDF parsing, record handling, and binary serialization across all three projects
- Used in: `Stdf/`, `P2020/`, `WindowsFormsApp1/`

**Secondary:**
- XML - MSBuild project configuration and resource files (.csproj, .sln)
- Used in: `*.csproj`, `*.sln`, `Resources.resx` files

## Runtime

**Environment:**
- .NET Framework 4.5.2 - Stdf core library (`Stdf.csproj`)
- .NET Framework 4.7.2 - P2020 analysis library and WindowsFormsApp1 UI (`P2020.csproj`, `WindowsFormsApp1.csproj`)

**Package Manager:**
- NuGet (MSBuild integrated)
- Lockfile: Project files embedded in `.csproj` - no separate lock file

## Frameworks

**Core:**
- System - Base class library (System, System.Core, System.Data, System.Xml, System.Windows.Forms)
- No external framework dependencies beyond standard .NET library

**UI:**
- Windows Forms (`System.Windows.Forms`) - Used for desktop GUI in P2020 and WindowsFormsApp1 projects
- Visual inheritance-based UI components (Form-based applications)

**Build/Dev:**
- MSBuild 15.0 (Visual Studio 2017+) - Build system, compilation, and project orchestration
- ILMerge (via NuGet) - Assembly merging tool through `MSBuild.ILMerge.Task.1.1.3` package

## Key Dependencies

**Critical:**
- System.Linq - Expression trees and LINQ-to-Objects for querying STDF records
- System.Linq.Expressions - Compiled query support for optimized STDF file parsing
- System.Reflection - Dynamic IL generation for record converters at runtime
- System.IO - Binary stream handling for STDF file reading/writing

**Infrastructure:**
- System.Collections - Generic collections (List<T>, Dictionary<K,V>), BitArray for STDF bit fields
- System.Text.RegularExpressions - Pattern matching for P2020 log file parsing in `CP2020.cs`
- System.Diagnostics - Debug assertions and tracing
- System.Globalization - Locale-aware string and numeric handling

**Optional:**
- Silverlight support - Conditional compilation (#if SILVERLIGHT) for cross-platform GUI (present but not actively used in current projects)

## Configuration

**Environment:**
- No environment-based configuration detected
- Hardcoded paths and settings in application code (P2020 and WindowsFormsApp1)

**Build:**
- `Project_Stdf.sln` - Main Visual Studio solution file (Format Version 12.00, VS 2017+)
- Debug and Release configurations for AnyCPU platform
- x86 platform configuration support (mixed platform support)
- Output: `bin\Debug\` and `bin\Release\` directories

**Compilation:**
- Debug symbols enabled for Debug builds
- Full optimization for Release builds
- XML documentation generation (`DocumentationFile`: `bin\Stdf.xml`)
- Code analysis enabled (AllRules.ruleset)
- C# language version: `latest` for P2020 project (nullable reference types enabled)

## Platform Requirements

**Development:**
- Visual Studio 2017 or later
- .NET Framework 4.5.2 Developer Kit (for Stdf core library)
- .NET Framework 4.7.2 Developer Kit (for P2020 and UI projects)
- MSBuild tools for project compilation

**Production:**
- .NET Framework 4.5.2 runtime minimum (for Stdf.dll library usage)
- .NET Framework 4.7.2 runtime required (for P2020 and WindowsFormsApp1 executables)
- Windows operating system (desktop/server - Windows Forms application)

## Assembly Architecture

**Output Types:**
- `Stdf.dll` - Class library (OutputType: Library)
- `CSTDF.dll` - P2020 analysis library (OutputType: Library)
- `WindowsFormsApp1.exe` - Executable UI application (OutputType: WinExe)

**Bootstrapper:**
- .NET Framework 3.5 SP1 bootstrap support (legacy, present but not required for current targets)
- Custom bootstrapper configuration for deployment scenarios

---

*Stack analysis: 2024-12-19*
