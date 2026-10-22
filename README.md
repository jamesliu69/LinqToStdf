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

在 Windows 安裝 .NET SDK 與 .NET Framework 4.7.2 Targeting Pack 後，可從儲存庫根目錄執行：

```powershell
.\Main\RegressionTests\Run-Tests.ps1
```

測試程式不依賴額外的 NuGet 測試套件，會建置完整方案，執行回歸案例，並在任何案例失敗時回傳非零結束碼。案例涵蓋 bit／nibble 陣列的大小端序與反向寫入、GDR byte array 長度與 nibble 讀取、MPR／FTR 記錄讀寫，以及 P2020 轉檔成功、失敗保留原檔、檔案鎖與重試。

方案以 .NET Framework 4.7.2 建置；如需以其他目標版本建置，可明確指定：

```powershell
.\Main\RegressionTests\Run-Tests.ps1 -TargetFrameworkVersion v4.7.2
```

轉檔會先在輸出檔所在目錄寫入暫存檔，完成並關閉後才替換正式輸出；解析、寫入或替換失敗時會保留既有檔案並清理暫存檔。

## STDF 桌面讀取器

`Main/StdfReader` 為獨立的 Windows 桌面專案，重用 `Stdf` 解析器。建置 `Main/Project_Stdf.sln` 後，執行 `Main/StdfReader/bin/Release/StdfReader.exe`，使用「開啟檔案」或拖放載入單一未壓縮的 STDF V4 檔案，也可將檔案路徑作為啟動參數。

介面提供摘要、原始記錄，以及 PTR 量測表、趨勢圖和直方圖。不同測試、Head/Site 與單位分開顯示；無效量測不納入圖表。MPR/FTR 目前可查看原始欄位，尚未展開為數值圖表。格式錯誤或部分讀取會顯示診斷資訊；第一版未提供取消載入、壓縮檔與大型檔案效能保證。

讀取器業務測試：

```powershell
dotnet build Main/StdfReader.Tests/StdfReader.Tests.csproj -c Release
.\Main\StdfReader.Tests\bin\Release\StdfReader.Tests.exe
```
