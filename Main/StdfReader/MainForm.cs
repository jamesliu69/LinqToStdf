using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Stdf;
using StdfReader.Core;
using STDF;

namespace StdfReader
{
    /// <summary>STDF 桌面讀取器主表單：以三頁籤（摘要/記錄/量測與圖表）呈現 StdfDocumentLoader 載入的 STDF 檔；支援按鈕開檔、拖放單一檔案與命令列參數自動載入。</summary>
    internal sealed class MainForm : Form
    {
        private readonly Button openButton = new Button { Name = "OpenButton", Text = "開啟 STDF…", AutoSize = true };
        private readonly Label stateLabel = new Label { Name = "LoadStatusLabel", AutoSize = true, Text = "請開啟 STDF 檔案，或拖放單一檔案至視窗。", Padding = new Padding(8) };
        private readonly TabControl tabs = new TabControl { Name = "MainTabs", Dock = DockStyle.Fill };
        private readonly Label alertLabel = new Label { Name = "DocumentAlertLabel", AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(8), BackColor = Color.LemonChiffon, Visible = false };
        private readonly Label[] summaryValues = new Label[7];
        private readonly DataGridView recordGrid = new DataGridView { Name = "RecordsGrid", Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoGenerateColumns = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        private readonly PropertyGrid propertyGrid = new PropertyGrid { Name = "RecordProperties", Dock = DockStyle.Fill, ToolbarVisible = true, HelpVisible = true };
        private readonly FlowLayoutPanel recordsToolbar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8), WrapContents = true };
        private readonly ComboBox recordFilter = new ComboBox { Name = "RecordTypeFilter", DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        private readonly ComboBox groupSelector = new ComboBox { Name = "TestSelector", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, DropDownWidth = 720, Margin = new Padding(3) };
        private readonly ComboBox partSelector = new ComboBox { Name = "PartSelector", DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new Padding(3) };
        private readonly DataGridView measurementGrid = new DataGridView { Name = "MeasurementsGrid", Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoGenerateColumns = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        private readonly Chart trendChart = new Chart { Name = "TrendChart", Dock = DockStyle.Fill };
        private readonly Chart histogramChart = new Chart { Name = "HistogramChart", Dock = DockStyle.Fill };
        private readonly ToolTip measurementToolTip = new ToolTip();
        private readonly Label histogramNote = new Label { Name = "HistogramSampleNote", Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(5) };
        /// <summary>目前載入的 STDF 檔彙整結果；null 表示未載入。</summary>
        private StdfDocument document;
        /// <summary>記錄頁目前顯示（經類型篩選）的記錄清單。</summary>
        private List<RecordEntry> currentRecords = new List<RecordEntry>();
        /// <summary>量測頁目前顯示（經群組與 Part 篩選）的量測清單。</summary>
        private List<MeasurementEntry> currentMeasurements = new List<MeasurementEntry>();
        /// <summary>是否正在背景載入檔案；載入中停用開檔與拖放。</summary>
        private bool isLoading;


        /// <summary>建立主表單、組版面並綁定事件；若有命令列參數，顯示後自動載入第一個。</summary>
        public MainForm(string[] args)
        {
            Text = "STDF Reader";
            Name = "StdfReaderMainForm";
            MinimumSize = new Size(980, 660);
            Size = new Size(1320, 860);
            Font = new Font("Segoe UI", 9F);
            AllowDrop = true;
            BuildLayout();
            openButton.Click += (s, e) => OpenFile();
            recordFilter.SelectedIndexChanged += (s, e) => ApplyRecordFilter();
            recordGrid.SelectionChanged += (s, e) => ShowSelectedRecord();
            groupSelector.SelectedIndexChanged += (s, e) => { UpdateTestSelectorToolTip(); RefreshMeasurements(); };
            partSelector.SelectedIndexChanged += (s, e) => RefreshMeasurements();
            DragEnter += OnDragEnterFile;
            DragDrop += OnDragDropFile;
            Shown += (s, e) => { if (args != null && args.Length > 0) BeginLoad(args[0]); };
        }

        /// <summary>組出主版面：頂部開檔列與狀態列、警告列，以及摘要/記錄（記錄表+屬性器分割）/量測與圖表三個頁籤。</summary>
        private void BuildLayout()
        {
            var header = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(8) };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.Controls.Add(openButton, 0, 0);
            header.Controls.Add(stateLabel, 1, 0);
            var root = new Panel { Dock = DockStyle.Fill };
            root.Controls.Add(tabs);
            root.Controls.Add(alertLabel);
            Controls.Add(root);
            Controls.Add(header);
            BuildSummaryPage();
            BuildRecordsPage();
            BuildMeasurementsPage();
            tabs.TabPages.Add(new TabPage("摘要") { Name = "SummaryTab" });
            tabs.TabPages[tabs.TabPages.Count - 1].Controls.Add(summaryValues[0].Parent);
            tabs.TabPages.Add(new TabPage("記錄") { Name = "RecordsTab" });
            var recordPage = tabs.TabPages[tabs.TabPages.Count - 1];
            var recordSplit = new SplitContainer { Name = "RecordSplitContainer", Size = new Size(1200, 700), Dock = DockStyle.Fill, SplitterDistance = 620, Orientation = Orientation.Vertical };
            recordSplit.Panel1.Controls.Add(recordGrid);
            recordSplit.Panel2.Controls.Add(propertyGrid);
            recordPage.Controls.Add(recordSplit);
            recordPage.Controls.Add(recordsToolbar);
            tabs.TabPages.Add(new TabPage("量測與圖表") { Name = "MeasurementsTab" });
            tabs.TabPages[tabs.TabPages.Count - 1].Controls.Add(BuildMeasurementsContainer());
        }

        /// <summary>建立摘要頁的 7 列標籤/值表（檔案、Lot ID、起訖時間、Part 統計、原始記錄統計、Diagnostics）。</summary>
        private void BuildSummaryPage()
        {
            var table = new TableLayoutPanel { Name = "SummaryTable", Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(18), AutoSizeMode = AutoSizeMode.GrowAndShrink };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            string[] labels = { "檔案", "Lot ID", "起始時間", "結束時間", "Part 統計", "原始記錄統計", "Diagnostics" };
            for (int i = 0; i < labels.Length; i++)
            {
                var title = new Label { Text = labels[i], AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 7, 18, 7) };
                var value = new Label { Name = "SummaryValue" + i, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 7, 0, 7), MaximumSize = new Size(1050, 0) };
                summaryValues[i] = value;
                table.Controls.Add(title, 0, i);
                table.Controls.Add(value, 1, i);
            }
        }

        /// <summary>建立記錄頁工具列（類型篩選下拉）與記錄表的三個資料行（Offset/類型/記錄摘要）。</summary>
        private void BuildRecordsPage()
        {
            recordsToolbar.Controls.Clear();
            recordsToolbar.Controls.Add(new Label { Text = "記錄類型：", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            recordsToolbar.Controls.Add(recordFilter);
            recordGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "OffsetColumn", HeaderText = "Offset", DataPropertyName = "Offset", FillWeight = 25 });
            recordGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TypeColumn", HeaderText = "類型", DataPropertyName = "Name", FillWeight = 35 });
            recordGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "RecordColumn", HeaderText = "記錄", DataPropertyName = "Summary", FillWeight = 40 });
            recordGrid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex != 2) return;
                var item = recordGrid.Rows[e.RowIndex].DataBoundItem as RecordRow;
                if (item != null) e.Value = item.Summary;
            };
        }

        /// <summary>設定量測表的資料行與趨勢/直方圖兩張圖表的標題與軸；趨勢圖 Y 軸使用科學記號格式。</summary>
        private void BuildMeasurementsPage()
        {
            string[] headers = { "序號", "Part", "Head", "Site", "數值", "下限", "上限", "狀態", "說明" };
            string[] props = { "Sequence", "Part", "Head", "Site", "ValueText", "LowText", "HighText", "Status", "Detail" };
            float[] weights = { 45, 45, 35, 35, 75, 75, 75, 55, 100 };
            for (int i = 0; i < headers.Length; i++) measurementGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Measurement" + props[i], HeaderText = headers[i], DataPropertyName = props[i], FillWeight = weights[i], SortMode = DataGridViewColumnSortMode.NotSortable });
            ConfigureChart(trendChart, "PTR 趨勢", "量測序號", "結果");
            ConfigureChart(histogramChart, "PTR 數值分布", "數值區間", "樣本數");
            trendChart.ChartAreas["Main"].AxisY.LabelStyle.Format = "0.###E+0";
        }

        /// <summary>組量測頁容器：上方測試群組/Part 選擇列，下方左右圖表（趨勢/直方圖）與量測表的上下分割。</summary>
        private Control BuildMeasurementsContainer()
        {
            var outer = new TableLayoutPanel { Name = "MeasurementsLayout", Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(6) };
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var selectors = new TableLayoutPanel { Name = "MeasurementSelectors", Dock = DockStyle.Fill, AutoSize = false, Height = 40, ColumnCount = 4, RowCount = 1, Padding = new Padding(3) };
            selectors.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); selectors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55)); selectors.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50)); selectors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            selectors.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            selectors.Controls.Add(new Label { Text = "測試群組：", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0); selectors.Controls.Add(groupSelector, 1, 0);
            selectors.Controls.Add(new Label { Text = "Part：", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 2, 0); selectors.Controls.Add(partSelector, 3, 0);
            var split = new SplitContainer { Name = "MeasurementSplitContainer", Size = new Size(1200, 650), Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 270, Panel1MinSize = 150, Panel2MinSize = 150 };
            var charts = new SplitContainer { Name = "ChartsSplitContainer", Size = new Size(1200, 430), Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 600, Panel1MinSize = 250, Panel2MinSize = 250 };
            charts.Panel1.Controls.Add(trendChart);
            charts.Panel2.Controls.Add(histogramChart);
            charts.Panel2.Controls.Add(histogramNote);
            split.Panel1.Controls.Add(charts);
            split.Panel2.Controls.Add(measurementGrid);
            outer.Controls.Add(selectors, 0, 0); outer.Controls.Add(split, 0, 1);
            return outer;
        }

        /// <summary>為圖表加入 ChartArea、標題與圖例。</summary>
        private static void ConfigureChart(Chart chart, string title, string xTitle, string yTitle)
        {
            chart.ChartAreas.Add(new ChartArea("Main") { AxisX = { Title = xTitle, MajorGrid = { LineColor = Color.Gainsboro } }, AxisY = { Title = yTitle, MajorGrid = { LineColor = Color.Gainsboro } } });
            chart.Titles.Add(title);
            chart.Legends.Add(new Legend("Legend"));
        }

        /// <summary>以 OpenFileDialog（STDF 副檔名過濾）選單一檔案後載入；載入中忽略。</summary>
        private void OpenFile()
        {
            if (isLoading) return;
            using (var dialog = new OpenFileDialog { Title = "選取 STDF V4 檔案", Filter = "STDF 檔案|*.std;*.stdf;*.stdf4|所有檔案|*.*", CheckFileExists = true, Multiselect = false })
                if (dialog.ShowDialog(this) == DialogResult.OK) BeginLoad(dialog.FileName);
        }

        /// <summary>載入 path 的進入點：呼叫 LoadFileAsync，失敗時清空畫面並顯示錯誤（表單已關閉則忽略）。</summary>
        private async void BeginLoad(string path)
        {
            TraceLogger.Log("INFO", "Load.Start", "application=StdfReader input=\"" + path + "\"");
            try { await LoadFileAsync(path); }
            catch (Exception ex)
            {
                TraceLogger.Log("ERROR", "Load.Failed", "application=StdfReader input=\"" + path + "\"", ex);
                if (IsDisposed || Disposing) return;
                document = null; ClearDocument();
                ShowLoadError("載入失敗：" + ex.Message);
                stateLabel.Text = "載入失敗：" + path;
            }
        }

        /// <summary>背景（Task.Run）以 StdfDocumentLoader 載入並顯示 STDF 檔；路徑空白或檔案不存在拋 FileNotFoundException，載入期間停用開檔按鈕。</summary>
        internal async Task LoadFileAsync(string path)
        {
            if (isLoading || IsDisposed) return;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) throw new FileNotFoundException("找不到 STDF 檔案。", path);
            isLoading = true; openButton.Enabled = false; stateLabel.Text = "正在讀取：" + path; alertLabel.Visible = false;
            try
            {
                var loaded = await Task.Run(() => (new StdfDocumentLoader()).Load(path));
                if (IsDisposed || Disposing) return;
                document = loaded;
                DisplayDocument(loaded);
                stateLabel.Text = (loaded.IsComplete ? "載入完成：" : "部分載入：") + loaded.FilePath;
                TraceLogger.Log(loaded.IsComplete ? "INFO" : "WARN", loaded.IsComplete ? "Load.Success" : "Load.Partial", "application=StdfReader input=\"" + path + "\" diagnostics=\"" + string.Join(" | ", loaded.Diagnostics) + "\"", loaded.LoadException);
            }
            finally
            {
                isLoading = false;
                if (!IsDisposed && !Disposing) openButton.Enabled = true;
            }
        }
        /// <summary>在頂部警告列以紅色顯示載入錯誤訊息。</summary>
        private void ShowLoadError(string message)
        {
            alertLabel.Text = message; alertLabel.BackColor = Color.MistyRose; alertLabel.ForeColor = Color.DarkRed; alertLabel.Visible = true;
        }

        /// <summary>把載入結果填入 UI：警告列（不完整=紅、有診斷=黃）、摘要七欄、記錄類型篩選、測試群組與 Part 選擇器，並切到摘要頁、刷新量測圖表。</summary>
        private void DisplayDocument(StdfDocument doc)
        {
            alertLabel.Visible = !doc.IsComplete || doc.Diagnostics.Count > 0;
            alertLabel.BackColor = doc.IsComplete ? Color.LemonChiffon : Color.MistyRose;
            alertLabel.ForeColor = doc.IsComplete ? Color.DarkGoldenrod : Color.DarkRed;
            alertLabel.Text = !doc.IsComplete ? "資料不完整／部分載入。請查看 Diagnostics；不可視為完整成功。" : (doc.Diagnostics.Count > 0 ? "載入時有診斷訊息，請查看摘要頁。" : "");
            summaryValues[0].Text = doc.FilePath ?? "未提供";
            summaryValues[1].Text = TextOrMissing(doc.LotId);
            summaryValues[2].Text = FormatDate(doc.StartTimeUtc);
            summaryValues[3].Text = FormatDate(doc.EndTimeUtc);
            var partCounts = doc.Parts.GroupBy(p => p.Status ?? "Unknown").OrderBy(g => g.Key).Select(g => g.Key + " " + g.Count());
            summaryValues[4].Text = doc.Parts.Count == 0 ? "未提供" : "Part 數 " + doc.Parts.Count + "（" + string.Join("、", partCounts) + "）";
            summaryValues[5].Text = "原始記錄 " + doc.RawRecordCount + " 筆" + (doc.RecordCounts.Count == 0 ? "" : "；" + string.Join("、", doc.RecordCounts.OrderBy(k => k.Key).Select(k => k.Key + " " + k.Value)));
            summaryValues[6].Text = doc.Diagnostics.Count == 0 ? "無" : string.Join(Environment.NewLine, doc.Diagnostics);
            currentRecords = doc.Records;
            var names = new[] { "全部類型" }.Concat(currentRecords.Select(r => r.Name ?? r.Record.GetType().Name).Distinct().OrderBy(x => x)).ToArray();
            recordFilter.Items.Clear(); recordFilter.Items.AddRange(names); recordFilter.SelectedIndex = 0;
            var groups = doc.Measurements.GroupBy(m => m.GroupKey).Select(g => new GroupChoice { Key = g.Key, Label = g.First().DisplayName }).OrderBy(g => g.Label).ToArray();
            groupSelector.Items.Clear(); groupSelector.Items.AddRange(groups); if (groups.Length > 0) groupSelector.SelectedIndex = 0;
            partSelector.Items.Clear(); partSelector.Items.Add("全部 Part");
            foreach (var part in doc.Parts) partSelector.Items.Add(new PartChoice { Index = part.Index, Label = "Part " + part.Index + (string.IsNullOrWhiteSpace(part.PartId) ? "" : " / " + part.PartId) });
            partSelector.SelectedIndex = 0;
            tabs.SelectedIndex = 0;
            RefreshMeasurements();
        }

        /// <summary>清空所有顯示：摘要值、記錄表/屬性器、選擇器、量測表與兩張圖表。</summary>
        private void ClearDocument()
        {
            foreach (var label in summaryValues) label.Text = "未提供";
            currentRecords.Clear(); recordGrid.DataSource = null; propertyGrid.SelectedObject = null;
            groupSelector.Items.Clear(); partSelector.Items.Clear(); measurementGrid.DataSource = null;
            trendChart.Series.Clear();
            ResetChartAxes(trendChart); histogramChart.Series.Clear(); histogramNote.Text = "";
        }

        /// <summary>依記錄類型篩選記錄表（"全部類型"=不篩），並選取第一列顯示於屬性器。</summary>
        private void ApplyRecordFilter()
        {
            if (document == null) return;
            string filter = recordFilter.SelectedItem as string;
            var filtered = string.IsNullOrEmpty(filter) || filter == "全部類型" ? currentRecords : currentRecords.Where(r => (r.Name ?? r.Record.GetType().Name) == filter).ToList();
            recordGrid.DataSource = filtered.Select(r => new RecordRow(r)).ToList();
            if (recordGrid.Rows.Count > 0) recordGrid.Rows[0].Selected = true;
            ShowSelectedRecord();
        }

        /// <summary>把目前選取的記錄以唯讀方式顯示於 PropertyGrid；UnknownRecord 改用 UnknownRecordView（含原始 hex）。</summary>
        private void ShowSelectedRecord()
        {
            var row = recordGrid.CurrentRow == null ? null : recordGrid.CurrentRow.DataBoundItem as RecordRow;
            if (row == null) { propertyGrid.SelectedObject = null; return; }
            propertyGrid.SelectedObject = row.Entry.Record is UnknownRecord ? (object)new UnknownRecordView(row.Entry.Record as UnknownRecord) : new ReadOnlyRecordView(row.Entry.Record);
        }

        /// <summary>把目前測試群組的完整標籤設為下拉的 ToolTip。</summary>
        private void UpdateTestSelectorToolTip()
        {
            var choice = groupSelector.SelectedItem as GroupChoice;
            measurementToolTip.SetToolTip(groupSelector, choice == null ? string.Empty : choice.Label);
        }
        /// <summary>依選取的測試群組與 Part 過濾量測，更新量測表並重繪趨勢與直方圖。</summary>
        private void RefreshMeasurements()
        {
            var choice = groupSelector.SelectedItem as GroupChoice;
            IEnumerable<MeasurementEntry> items = document == null || choice == null ? Enumerable.Empty<MeasurementEntry>() : document.Measurements.Where(m => m.GroupKey == choice.Key);
            var part = partSelector.SelectedItem as PartChoice;
            if (part != null) items = items.Where(m => m.PartIndex == part.Index);
            currentMeasurements = items.OrderBy(m => m.Sequence).ToList();
            measurementGrid.DataSource = currentMeasurements.Select(m => new MeasurementRow(m)).ToList();
            DrawTrend(currentMeasurements);
            DrawHistogram(currentMeasurements);
        }

        /// <summary>以點圖繪出有效量測及每筆量測自己的上下限，避免把不同測試條件的界限延伸到其他量測。</summary>
        private void DrawTrend(List<MeasurementEntry> values)
        {
            trendChart.Series.Clear();
            ResetChartAxes(trendChart);
            var series = new Series("有效 PTR") { ChartType = SeriesChartType.Point, MarkerStyle = MarkerStyle.Circle, MarkerSize = 7, Color = Color.RoyalBlue, ChartArea = "Main" };
            foreach (var m in values.Where(m => m.Value.HasValue && IsValid(m))) series.Points.AddXY(m.Sequence, m.Value.Value);
            trendChart.Series.Add(series);
            AddLimitPoints(trendChart, "下限", values, m => m.LowLimit, Color.DarkOrange);
            AddLimitPoints(trendChart, "上限", values, m => m.HighLimit, Color.Firebrick);
            ResetChartAxes(trendChart);
        }

        /// <summary>依量測序號繪製有界限的點；缺少界限的量測不補值或連線。</summary>
        private static void AddLimitPoints(Chart chart, string name, List<MeasurementEntry> values, Func<MeasurementEntry, double?> selectLimit, Color color)
        {
            var limits = new Series(name) { ChartType = SeriesChartType.Point, MarkerStyle = MarkerStyle.Cross, MarkerSize = 9, Color = color, ChartArea = "Main" };
            foreach (var m in values)
            {
                var limit = selectLimit(m);
                if (limit.HasValue) limits.Points.AddXY(m.Sequence, limit.Value);
            }
            if (limits.Points.Count > 0) chart.Series.Add(limits);
        }

        /// <summary>繪有效量測的直方圖：bin 數 min(20, max(3, ceil(√n)))，單一數值時畫成單欄；並在下方標註有效樣本數。</summary>
        private void DrawHistogram(List<MeasurementEntry> values)
        {
            histogramChart.Series.Clear();
            ResetChartAxes(histogramChart);
            var valid = values.Where(m => m.Value.HasValue && IsValid(m)).Select(m => m.Value.Value).ToArray();
            histogramNote.Text = "有效樣本數：" + valid.Length + (valid.Length < 2 ? "；樣本有限，直方圖參考性低。" : "");
            if (valid.Length == 0) { ResetChartAxes(histogramChart); return; }
            var series = new Series("有效 PTR") { ChartType = SeriesChartType.Column, Color = Color.SteelBlue, ChartArea = "Main" };
            if (valid.Length == 1 || valid.Min() == valid.Max()) series.Points.AddXY(valid[0].ToString("0.###E+0", CultureInfo.InvariantCulture), valid.Length);
            else
            {
                int binCount = Math.Min(20, Math.Max(3, (int)Math.Ceiling(Math.Sqrt(valid.Length))));
                double min = valid.Min(), max = valid.Max(), width = (max - min) / binCount;
                int[] bins = new int[binCount];
                foreach (double v in valid) bins[Math.Min(binCount - 1, (int)((v - min) / width))]++;
                for (int i = 0; i < bins.Length; i++) series.Points.AddXY((min + (i + 0.5) * width).ToString("0.###E+0", CultureInfo.InvariantCulture), bins[i]);
            }
            histogramChart.Series.Add(series);
            ResetChartAxes(histogramChart);
        }

        /// <summary>重設圖表所有軸的最小/最大/間隔並重算刻度。</summary>
        private static void ResetChartAxes(Chart chart)
        {
            foreach (ChartArea area in chart.ChartAreas)
            {
                ResetAxis(area.AxisX); ResetAxis(area.AxisY);
                ResetAxis(area.AxisX2); ResetAxis(area.AxisY2);
                area.RecalculateAxesScale();
            }
        }

        /// <summary>把單一軸的最小/最大/間隔還原為 NaN（自動）。</summary>
        private static void ResetAxis(Axis axis)
        {
            axis.Minimum = double.NaN;
            axis.Maximum = double.NaN;
            axis.Interval = double.NaN;
        }
        /// <summary>量測數值存在且非 NaN/Infinity 才視為有效（可繪圖）。</summary>
        private static bool IsValid(MeasurementEntry m) { return m.Value.HasValue && !double.IsNaN(m.Value.Value) && !double.IsInfinity(m.Value.Value); }
        /// <summary>時間同時以 UTC 與本地時區顯示；無值顯示「未提供」。</summary>
        private static string FormatDate(DateTime? utc) { return utc.HasValue ? utc.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture) + " / " + utc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss '本地'", CultureInfo.InvariantCulture) : "未提供"; }
        /// <summary>字串空白時顯示「未提供」。</summary>
        private static string TextOrMissing(string value) { return string.IsNullOrWhiteSpace(value) ? "未提供" : value; }
        /// <summary>數值以科學記號顯示；無值顯示 N/A。</summary>
        private static string Number(double? value) { return value.HasValue ? value.Value.ToString("0.###E+0", CultureInfo.InvariantCulture) : "N/A"; }

        /// <summary>拖放進入：非載入中且帶檔案資料時顯示複製游標。</summary>
        private void OnDragEnterFile(object sender, DragEventArgs e) { if (!isLoading && e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; }
        /// <summary>拖放放下：僅接受單一檔案，多檔或無檔顯示錯誤，單檔即開始載入。</summary>
        private void OnDragDropFile(object sender, DragEventArgs e)
        {
            if (isLoading) return;
            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths == null || paths.Length != 1) { ShowLoadError("請一次拖放單一 STDF 檔案。"); return; }
            BeginLoad(paths[0]);
        }

        /// <summary>測試群組下拉的選項（GroupKey + 顯示標籤）。</summary>
        private sealed class GroupChoice { public string Key; public string Label; public override string ToString() { return Label; } }
        /// <summary>Part 下拉的選項（Part 索引 + 顯示標籤）。</summary>
        private sealed class PartChoice { public int Index; public string Label; public override string ToString() { return Label; } }
        /// <summary>記錄表的資料列：包裝 RecordEntry 提供 Offset/名稱/記錄摘要（合成記錄加註）。</summary>
        private sealed class RecordRow
        {
            public RecordEntry Entry { get; private set; }
            public long Offset { get { return Entry.Offset; } }
            public string Name { get { return Entry.Name ?? Entry.Record.GetType().Name; } }
            public string Summary { get { return Entry.Record.RecordType + (Entry.Record.Synthesized ? "（合成）" : ""); } }
            public RecordRow(RecordEntry entry) { Entry = entry; }
        }
        /// <summary>量測表的資料列：包裝 MeasurementEntry，數值/界限以科學記號顯示，繼承欄位加註「（繼承）」並彙整明細。</summary>
        private sealed class MeasurementRow
        {
            private readonly MeasurementEntry m;
            public long Sequence { get { return m.Sequence; } }
            public string Part { get { return m.PartIndex.HasValue ? m.PartIndex.Value.ToString(CultureInfo.InvariantCulture) : "N/A"; } }
            public string Head { get { return m.HeadNumber.HasValue ? m.HeadNumber.Value.ToString(CultureInfo.InvariantCulture) : "N/A"; } }
            public string Site { get { return m.SiteNumber.HasValue ? m.SiteNumber.Value.ToString(CultureInfo.InvariantCulture) : "N/A"; } }
            public string ValueText { get { return Number(m.Value); } }
            public string LowText { get { return Number(m.LowLimit) + (m.LowLimit.HasValue && m.LowLimitInherited ? "（繼承）" : ""); } }
            public string HighText { get { return Number(m.HighLimit) + (m.HighLimit.HasValue && m.HighLimitInherited ? "（繼承）" : ""); } }
            public string Status { get { return m.Status ?? "Unknown"; } }
            public string Detail { get { var details = new List<string>(); if (!string.IsNullOrWhiteSpace(m.InvalidReason)) details.Add(m.InvalidReason); if (m.UnitsInherited) details.Add("單位繼承"); if (m.ResultScalingInherited) details.Add("結果縮放繼承"); if (m.LowLimitInherited) details.Add("下限繼承"); if (m.HighLimitInherited) details.Add("上限繼承"); return string.Join("；", details); } }
            public MeasurementRow(MeasurementEntry measurement) { m = measurement; }
        }
        /// <summary>把記錄物件以 ICustomTypeDescriptor 包成唯讀檢視（所有屬性套 ReadOnlyPropertyDescriptor）供 PropertyGrid 顯示。</summary>
        [TypeConverter(typeof(ExpandableObjectConverter))]
        private sealed class ReadOnlyRecordView : ICustomTypeDescriptor
        {
            private readonly object instance;
            public ReadOnlyRecordView(object value) { instance = value; }
            public AttributeCollection GetAttributes() { return TypeDescriptor.GetAttributes(instance); }
            public string GetClassName() { return TypeDescriptor.GetClassName(instance); }
            public string GetComponentName() { return TypeDescriptor.GetComponentName(instance); }
            public TypeConverter GetConverter() { return new ExpandableObjectConverter(); }
            public EventDescriptor GetDefaultEvent() { return TypeDescriptor.GetDefaultEvent(instance); }
            public PropertyDescriptor GetDefaultProperty() { return null; }
            public object GetEditor(Type editorBaseType) { return null; }
            public EventDescriptorCollection GetEvents() { return TypeDescriptor.GetEvents(instance); }
            public EventDescriptorCollection GetEvents(Attribute[] attributes) { return TypeDescriptor.GetEvents(instance, attributes); }
            public PropertyDescriptorCollection GetProperties() { return GetProperties(null); }
            public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
            {
                return new PropertyDescriptorCollection(TypeDescriptor.GetProperties(instance, attributes).Cast<PropertyDescriptor>().Select(p => new ReadOnlyPropertyDescriptor(p)).ToArray(), true);
            }
            public object GetPropertyOwner(PropertyDescriptor pd) { return instance; }
        }

        /// <summary>唯讀屬性描述器：GetValue 委派原描述器，SetValue 拋 NotSupportedException（「STDF 原始記錄為唯讀。」）。</summary>
        private sealed class ReadOnlyPropertyDescriptor : PropertyDescriptor
        {
            private readonly PropertyDescriptor source;
            public ReadOnlyPropertyDescriptor(PropertyDescriptor descriptor) : base(descriptor, new Attribute[] { ReadOnlyAttribute.Yes }) { source = descriptor; }
            public override Type ComponentType { get { return source.ComponentType; } }
            public override bool IsReadOnly { get { return true; } }
            public override Type PropertyType { get { return source.PropertyType; } }
            public override bool CanResetValue(object component) { return false; }
            public override object GetValue(object component) { return source.GetValue(component); }
            public override void ResetValue(object component) { }
            public override void SetValue(object component, object value) { throw new NotSupportedException("STDF 原始記錄為唯讀。"); }
            public override bool ShouldSerializeValue(object component) { return false; }
            public override PropertyDescriptorCollection GetChildProperties(object instance, Attribute[] filter)
            {
                return new PropertyDescriptorCollection(source.GetChildProperties(instance, filter).Cast<PropertyDescriptor>().Select(p => new ReadOnlyPropertyDescriptor(p)).ToArray(), true);
            }
        }
        /// <summary>UnknownRecord 的檢視模型：以 RecordType/Endian/RawHex（原始位元組 hex）呈現，並保留可展開的原始記錄。</summary>
        private sealed class UnknownRecordView
        {
            private readonly UnknownRecord record;
            public ReadOnlyRecordView 原始記錄 { get { return new ReadOnlyRecordView(record); } }
            public string RecordType { get { return record.RecordType.ToString(); } }
            public string Endian { get { return record.Endian.ToString(); } }
            public string RawHex { get { return BitConverter.ToString(record.Content ?? new byte[0]).Replace("-", " "); } }
            public UnknownRecordView(UnknownRecord value) { record = value; }
        }
    }
}


