# STDF Reader 實作與存檔點

## 範圍與假設
依核准設計與 ADR 0001，新增 .NET Framework 4.7.2 傳統 csproj 核心資料層及 console 業務測試；不改 parser、P2020、RegressionTests 或 solution。CONTEXT.md 不存在，使用核准設計與 ADR 詞彙。StdfFile.GetRecordsEnumerable() 的 raw stream 以 PIR/PTR/PRR 序列投影；保留合成錯誤/未知 raw record。

## 步驟與存檔點
1. 建立 Core/Tests csproj 與公開 DTO/API contract；先落地合成 STDF writer 測試骨架。
2. 實作載入、偏移與記錄診斷、PIR/PRR part 關聯、PTR default/scaling/flags 投影。
3. 執行 console 業務測試與既有 sample 驗證，記錄限制及可續作存檔點。
4. 新增 `Main/StdfReader` WinForms 專案，接上摘要、唯讀記錄、PTR 表格、趨勢與直方圖；以背景工作載入，並明示部分資料。
5. 將 Core、Reader、Tests 加入 `Main/Project_Stdf.sln`，保留原有方案平台設定，更新 README 使用方式。
6. 實際開啟三頁並截圖，檢查 Cp 科學記號、Invalid 不畫點、唯讀欄位、縮小視窗及截斷檔；修正後重跑受影響的畫面。

## 驗證
執行 dotnet build Main/StdfReader.Tests/StdfReader.Tests.csproj -c Debug/Release，並直接執行對應 bin\{Configuration}\StdfReader.Tests.exe（傳統 .NET Framework 4.7.2 console 專案不支援 dotnet run）。測試覆蓋有效性、預設繼承、縮放、retest、多 part/site、unknown、big-endian 與截斷。

## 2026-10-22 存檔點

- Core 與 Tests：Debug／Release 各 26 個業務斷言通過。審查攔下的重複 PIR 遺失配對與 NaN／Infinity 誤報 Pass 已修正並有測試。
- 整合方案：Debug／Release 建置成功，既有回歸測試各 107 passed、0 failed；紀錄位於 `docs/qa/stdf-reader/*-verification.log`。
- GUI：Release 已實際開啟三頁，完成 Cp／Invalid 圖表、唯讀陣列與未知記錄、縮小視窗及截斷檔提示檢查。啟動崩潰、切換測試後的座標範圍與版面遮蔽均已修正；結果與截圖見 `docs/qa/stdf-reader/README.md`。
- 最終整合建置：Debug／Release 各 0 警告、0 錯誤，紀錄見 `docs/qa/stdf-reader/final-*-build.log`。未新增 NuGet 套件，原解析器與轉檔程式未變更。
- 目前功能範圍仍為單一未壓縮 STDF V4；PTR 數值分析，MPR/FTR 原始欄位瀏覽。未 commit 或 push。
