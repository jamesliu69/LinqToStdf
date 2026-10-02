# Good News
Good news, everyone!  LinqToStdf is now using Git, and we'll happily take pull requests that line up with the principles of the project.  We owe you guys a list of those things, but we'll happily take a look at requests and let you know until we have them documented. ;)
# Project Description
A library for parsing/processing Standard Test Datalog Format (STDF) files, typically used in semiconductor testing.
# Features
* Parsing of the general STDF file structure
* Support for Linq style queries over STDF files
* Specific support for the STDF V4 spec, including "structured" extensions.  For example, get all the Parametric Test Records for a given Part (from PIR or PRR).
* Pluggable record registration.  Plug in parsers for your custom records, or describe them 
* Parsing of the general STDF file structure
* Support for Linq style queries over STDF files
* Specific support for the STDF V4 spec, including "structured" extensions.  For example, get all the Parametric Test Records for a given Part (from PIR or PRR).
* Pluggable record registration.  Plug in parsers for your custom records, or describe them via attributes and let the library create the parsers for you on the fly.
* Tolerance for corrupt/malformed files
  * Pluggable policy for errors.  For example, you can throw on any errors, or take other actions appropriate for your scenario like repair.
  * Pluggable corruption detection and recovery
* Generation of "missing" data (such as part/bin/test summaries)
* High performance, tunable for a broad range of scenarios
* STDF file generation, especially as a result of processing existing files.
* Pluggable filters, allowing a wide range of behavior such as data transform
  * Built-in filters for things like synthesizing summary records and enforcing STDF V4 record ordering.
* "Pre-compiled" queries, allowing you to leverage the richness of the API and the performance of a single-use parser.

## [We need corrupt STDFs]!

# General Overview
For a general overview, go see the [Basic Idea]

# Motivation
Discover the [Motivation] behind the library.

# Example Usage
See [Example Usage]

## 回歸測試

在 Windows 安裝 .NET SDK，以及 .NET Framework 4.5.2 和 4.7.2 Targeting Pack 後，可從儲存庫根目錄執行：

```powershell
.\Main\RegressionTests\Run-Tests.ps1
```

測試程式不依賴額外的 NuGet 測試套件，會建置完整方案，執行回歸案例，並在任何案例失敗時回傳非零結束碼。案例涵蓋 bit／nibble 陣列的大小端序與反向寫入、GDR byte array 長度與 nibble 讀取、MPR／FTR 記錄讀寫，以及 P2020 轉檔成功、失敗保留原檔、檔案鎖與重試。

若本機只有 .NET Framework 4.7.2 Targeting Pack，可僅對此次建置指定目標版本：

```powershell
.\Main\RegressionTests\Run-Tests.ps1 -TargetFrameworkVersion v4.7.2
```

轉檔會先在輸出檔所在目錄寫入暫存檔，完成並關閉後才替換正式輸出；解析、寫入或替換失敗時會保留既有檔案並清理暫存檔。
