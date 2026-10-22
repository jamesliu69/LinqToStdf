# STDF Reader GUI QA

2026-10-22 以 Release `StdfReader.exe` 在互動式 Windows 桌面實際顯示 WinForms `MainForm`，載入 STDF 樣本並檢查三個頁面、量測分組、記錄屬性、圖表及縮放。操作透過 STA PowerShell 反射讀取表單控制項並擷取畫面；每次執行結束會關閉表單。UI Automation 未提供可用的標準頁籤/控制項名稱，因此不以 UIA 腳本作為主要證據。

## 結果

| 項目 | Release GUI 結果 |
| --- | --- |
| 三頁與載入 | 摘要、記錄、量測與圖表均存在並實際切換。樣本 `Main/StdfConverter/bin/Release/testSTDF.stdf` 為 4774 bytes；顯示 TEST-502、1 Part、59 筆原始記錄、23 筆量測、23 個群組，無 Diagnostics。 |
| 記錄表 | 59 列；Offset/類型/記錄標題可見，首列 FAR 可見。 |
| Record PropertyGrid | `ReadOnlyRecordView` 可展開；所有屬性唯讀，setter 拒絕寫入。PGR 的 `PinIndexes` 顯示為長度 1 的 `UInt16[]` 且唯讀。 |
| Unknown raw record | 以記憶體中的合成記錄驗證 `StdfRecord:250:42` 可展開，RawHex 顯示 `DE AD BE EF`，欄位唯讀。 |
| Cp 趨勢與直方圖 | 樣本的五筆 Cp 值皆約為 `1E-9F`；選取 Test 6 後值為 `1E-9F`、有效趨勢點 1、直方圖 1 series/1 point。截圖中資料標記在繪圖區可見。 |
| Invalid | Test 14 為 Invalid、Value 為 null；有效趨勢點 0，兩條限制線可見，直方圖無 series/點。 |
| Unknown 狀態且有值 | 原始樣本沒有此案例；以記憶體合成 TestFlags `0x40`、Status `Unknown`、Value `1E-9`，趨勢與直方圖各有 1 點。 |
| Resize | 將視窗調至 client 984×661 後，量測頁 Part 選單仍完整顯示於視窗內。 |
| 截斷輸入 | `truncated.stdf` 為樣本副本移除最後 2 bytes。UI 顯示「部分載入」、`IsComplete=False`，診斷為 offset 4761 有 11 bytes 損毀資料及缺少 MRR；0 筆 records/groups/measurements，沒有沿用先前載入資料。 |

趨勢點落在約 `1E-9` 的科學記號座標範圍；腳本將圖表繪圖區換算為像素時得到 `AxisContainsPoint=False`，差距約 0.15 px，屬邊界換算精度。Release 截圖可直接確認標記可見，因此以實際畫面作為可見性證據，不把該像素布林值當成失敗或成功判定。

## 證據檔

- `release-final/reflect-summary.png`、`reflect-records.png`、`reflect-records-array.png`、`reflect-records-unknown.png`：摘要、記錄列、陣列與 Unknown raw 欄位。
- `release-final/reflect-measurements-cp.png`、`reflect-histogram-cp.png`、`reflect-trend-cp.png`：Cp 值、趨勢標記與直方圖。
- `release-final/reflect-measurements-invalid.png`、`reflect-synthetic-unknown-valid.png`、`reflect-measurements-resize.png`：Invalid、合成 Unknown 有效值與縮放後版面。
- `release-final/reflect-results.txt`：Release 執行的載入、資料、唯讀、陣列、Unknown、Cp/Invalid 點數及尺寸結果。
- `truncated-run/reflect-partial-error.png`、`truncated-run/reflect-results.txt`：部分載入警告與錯誤診斷。
- `reflect-smoke.ps1`：建立並顯示真實 WinForms 表單，執行檢查及截圖；`smoke.ps1` 為一般互動啟動腳本。

Debug/Release solution build 與 solution tests 由主代理另行記錄；此 GUI QA 只證明上述離線桌面互動結果，不代表部署或硬體驗收。
