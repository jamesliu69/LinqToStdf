# 0001. 將 Stdf 目標框架由 .NET Framework 4.5.2 提升為 4.7.2

> 更名註記（2026-10-22）：本 ADR 中的歷史驗證資料仍使用當時的 WinForms 專案名稱 `WindowsFormsApp1`；該應用程式目前已更名為 `StdfConverter`。

- 狀態：Accepted
- 日期：2026-10-12
- 決策者：維護者（jamesliu69）
- 相關議題：[#1](https://github.com/jamesliu69/LinqToStdf/issues/1)

## 背景

`Main/Stdf/Stdf.csproj` 原本目標 .NET Framework 4.5.2，而方案內其餘專案（`P2020`、`WindowsFormsApp1`、`RegressionTests`）皆目標 4.7.2。

由於 `Stdf.csproj` 是方案中唯一需要 4.5.2 目標套件的專案，任何未安裝 **.NET Framework 4.5.2 Developer Pack** 的機器都無法建置整個方案：

```
error MSB3644: 找不到 .NETFramework,Version=v4.5.2 的參考組件
  [D:\Github\LinqToStdf\Main\Stdf\Stdf.csproj]
```

此失敗同時發生在 `dotnet build Main\Project_Stdf.sln` 與 Visual Studio 的建置，且發生於參考組件解析階段，因此方案內其他專案一併無法產出。

補充事實：

- 安裝 4.5.2 Developer Pack 需要系統管理員權限，且該目標套件僅存在於開發者機器，無法隨儲存庫散布。
- .NET Framework 4.5.2 已於 2016-01 終止支援。
- 方案的實際交付物 `WindowsFormsApp1.exe` 原本就需要 4.7.2 執行階段，因此沒有任何部署因這次調整而失去支援。

## 決策

1. `Stdf.csproj` 的 `TargetFrameworkVersion` 由 `v4.5.2` 改為 `v4.7.2`，使方案所有專案共用同一目標框架。
2. 一併移除 `Stdf.csproj` 中兩處指向不存在檔案 `AllRules.ruleset` 的 `<CodeAnalysisRuleSet>`。該檔案從未進版控；過去因為建置從未進行到程式碼分析階段而未顯現，改為 4.7.2 後才以 `warning MSB3884` 暴露。
3. `Main/RegressionTests/Run-Tests.ps1` 維持原樣：不加入目標框架自動偵測或覆寫。既有的 `-TargetFrameworkVersion` 參數保留，供臨時覆寫使用。
4. 同步更新 `README.md` 與 `.planning/codebase/{STACK,STRUCTURE,TESTING,CONVENTIONS}.md` 中所有 4.5.2 敘述。

## 後果

**破壞性變更**：`Stdf.dll` 的最低執行階段由 4.5.2 提升為 4.7.2。仍以 4.5.2／4.6.x 執行階段部署並引用此程式庫的使用者必須升級。

其他後果：

- 建置需求簡化為 .NET SDK ＋ .NET Framework 4.7.2 Targeting Pack，不再需要 4.5.2 Developer Pack，也不再需要 `-p:TargetFrameworkVersion` 覆寫。
- `dotnet build`、Visual Studio 建置與 `Run-Tests.ps1` 三條路徑的行為重新一致。
- 方案不再保留任何 4.5.2 專屬設定；後續若要支援更舊的執行階段，須重新評估並新增目標框架，而非沿用單一 TFM。

## 曾考慮的替代方案

1. **安裝 .NET Framework 4.5.2 Developer Pack**：可保留 4.5.2 目標，但需要系統管理員權限，且無法解決其他貢獻者機器上的同類問題；方案仍會綁定一個已終止支援的框架。
2. **保留 v4.5.2，僅以 `-p:TargetFrameworkVersion=v4.7.2` 覆寫建置**：能讓指令列建置與測試通過，但 Visual Studio 建置仍失敗，且每次建置都必須記得帶參數；`Run-Tests.ps1` 曾短暫加入自動偵測，因屬多餘複雜度而於同一次變更中移除。
3. **維持現狀**：不可行，方案無法建置。

## 驗證

| 檢查 | 結果 |
| --- | --- |
| `dotnet build Main\Project_Stdf.sln -c Release`（無覆寫）| 建置成功、0 錯誤、exit 0 |
| `dotnet msbuild Main\Project_Stdf.sln -t:Rebuild -p:Configuration=Release` | exit 0；警告僅剩 `CP2020.cs` 既有 4 個（CS8632 ×2、CS0067 ×2）|
| `.\Main\RegressionTests\Run-Tests.ps1` | `RESULT: 92 passed, 0 failed`、exit 0 |
| 產物 `TargetFrameworkAttribute`（反射實測）| `Stdf.dll`、`CSTDF.dll`、`LinqToStdf.RegressionTests.exe`、`WindowsFormsApp1.exe` 皆為 `.NETFramework,Version=v4.7.2` |
