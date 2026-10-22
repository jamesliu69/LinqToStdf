# 通用 STDF 讀取與圖表工具設計（草稿，待批准）

> 狀態：設計草稿，尚未獲批准；此文件不授權開始實作。批准後再建立實作計畫。

## 目標與成功標準

新增 Windows 桌面 STDF 檢視專案，讓使用者能載入單一 STDF V4 檔案，確認批次摘要、瀏覽原始記錄，並檢視 PTR 測量值與基本圖表。缺失或無效資料必須可辨識且不可被當成零值；格式錯誤或截斷檔不可顯示為成功載入。

## 方案與平台

採用同一方案新增專案，重用現有解析器，沿用 WinForms 與 .NET Framework 4.7.2。另一選項是獨立儲存庫，可降低方案耦合，但需額外管理版本與解析器相依；本案選擇同方案，維持單一程式庫來源。專案放在 `Main/StdfReader/StdfReader.csproj`，參考 `Main/Stdf/Stdf.csproj`，並加入 `Main/Project_Stdf.sln`。圖表使用 `System.Windows.Forms.DataVisualization`，不新增套件。此方向符合 [ADR 0001](../../adr/0001-retarget-stdf-to-net472.md)。

## 使用流程與頁面

檔案可由開啟檔案對話框選取，或拖放單一未壓縮 STDF V4 檔案。解析在背景執行，載入期間停用重複載入；第一版不提供進度、取消或大型檔案效能保證。介面分三頁：**摘要**顯示 lot、時間與測試結果；**記錄**列出記錄分類並以 PropertyGrid 檢視原始欄位；**量測與圖表**顯示 PTR 數值及圖表。載入錯誤須顯示原因；若解析器提供部分結果，畫面須持續醒目標示「部分載入／資料不完整」。

## 資料流與顯示規則

資料流為 `File → StdfRecord（raw）→ summary／part／measurement projections → UI`。摘要時間同時標示 UTC 與本地時間。PRR attempt 數與 Pass／Fail／Unknown 按原始旗標判定；缺少資料顯示「未提供」，不得補成 0。PIR/PRR 依 head、site 與 attempt 關聯多個 part，不可假設所有 PRR 都屬於第一筆 PIR。TSR 只作來源摘要；量測表自算列數時須標明是工具計算值。

第一版只將 PTR 展開為數值表與圖表；MPR/FTR 僅在記錄頁檢視原始欄位，不推導多 pin 數值或判定。PTR 資料依 test、head/site 與 units 隔離或分組，不混合不同單位。顯示使用科學記號（例如 `1E-9F`）；無效數值顯示 `N/A`，不得進入統計或圖表。保留原始旗標與 diagnostics 供追查。若 test 高低限缺失，標示未提供；上限為 0 時保留原值，不推定為無上限。

圖表提供 PTR 序列趨勢與直方圖；橫軸採量測順序，不推造每筆量測時間。小樣本直方圖需標示樣本數並提示樣本有限。`BuiltInFilters.PopulatePtrFieldsWithDefaults` 可能修改記錄且以 test number 作為 key；讀取流程不得修改 raw record。量測投影依 STDF V4 規則繼承同一測試的預設單位、縮放與限制值，並標示繼承來源；原始檢視保留原值。結果有效性依原始旗標判定，禁止以預設值補出量測結果。PRR `TEST_T` 以毫秒耗時呈現，不當作日期。

STDF V4 未知記錄顯示 type/subtype 與 raw bytes。合成的 Start/End/Error 記錄不得計入原始記錄數；記錄分類與數量須能區別 parser 合成記錄。沿用 `StdfFile.GetRecordsEnumerable` 的錯誤行為（`ThrowOnFormatError` 預設為 true），不修改 parser 全域設定。截斷檔不得回報完整成功。

## 非目標

Wafer map、Cpk、MES 整合、多檔比較、gzip 解壓、STDF 改寫，以及 MPR/FTR 數值展開均不在第一版範圍。

## 驗收

- 保持既有轉檔測試 107 項通過；新增測試驗證多 part 配對、未知 PRR 旗標、缺失值與預設值、截斷檔、未知記錄，以及圖表排除無效值。
- 以既有 `Main/StdfConverter/bin/Release/testSTDF.stdf` 驗證 23 PTR、23 TSR、1 part、18 pass、3 fail、2 `MS_ER`、5 個 Cp 值為 `1E-9F`；時間範圍應對應本地時間 2026/10/08 17:32:09–17:32:14。
- Debug 與 Release 建置成功；GUI smoke test 實際開啟三頁並保存畫面截圖。若環境無法執行 GUI，須明確記錄未驗證，不得以建置成功替代。

## 設計核對

此草稿限定單檔、未壓縮 STDF V4 與 PTR 數值分析；MPR/FTR 僅原始檢視。背景讀取不提供進度或取消，且大檔無效能保證。raw record、投影資料及 parser 診斷各自保留，缺值、無效值、未知記錄與部分載入均有明確呈現方式。文件待使用者批准後才進入實作計畫。
