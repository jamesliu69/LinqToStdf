param(
    [string]$ExePath = (Join-Path $PSScriptRoot '..\..\..\Main\StdfReader\bin\Debug\StdfReader.exe'),
    [string]$InputPath = (Join-Path $PSScriptRoot '..\..\..\Main\StdfConverter\bin\Release\testSTDF.stdf'),
    [string]$OutputDirectory = $PSScriptRoot
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class ReaderCapture {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
'@

function Pump-Until([scriptblock]$Predicate, [int]$TimeoutSeconds = 30) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        [System.Windows.Forms.Application]::DoEvents()
        if (& $Predicate) { return $true }
        Start-Sleep -Milliseconds 50
    }
    return $false
}

function Get-PrivateField($Object, [string]$Name) {
    $field = $Object.GetType().GetField($Name, [Reflection.BindingFlags]'Instance,NonPublic')
    if (!$field) { throw "找不到欄位：$Name" }
    return $field.GetValue($Object)
}

function Save-FormScreenshot($Form, [string]$Path) {
    [ReaderCapture]::SetForegroundWindow($Form.Handle) | Out-Null
    [System.Windows.Forms.Application]::DoEvents()
    Start-Sleep -Milliseconds 200
    $rect = New-Object ReaderCapture+RECT
    if (![ReaderCapture]::GetWindowRect($Form.Handle, [ref]$rect)) { throw '無法取得 Reader 視窗範圍。' }
    $bitmap = New-Object System.Drawing.Bitmap(($rect.Right - $rect.Left), ($rect.Bottom - $rect.Top))
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose(); $bitmap.Dispose()
}

if ([Threading.Thread]::CurrentThread.ApartmentState -ne 'STA') { throw 'PowerShell 必須使用 STA。' }
$ExePath = (Resolve-Path -LiteralPath $ExePath).Path
$InputPath = (Resolve-Path -LiteralPath $InputPath).Path
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
[Environment]::CurrentDirectory = Split-Path -Parent $ExePath
$assembly = [Reflection.Assembly]::LoadFrom($ExePath)
$formType = $assembly.GetType('StdfReader.MainForm', $true)
$constructor = $formType.GetConstructor([Reflection.BindingFlags]'Instance,Public,NonPublic', $null, [Type[]]@([string[]]), $null)
if (!$constructor) { throw '找不到 MainForm(string[]) 建構子。' }
$constructorArguments = New-Object object[] 1
$constructorArguments[0] = [string[]]@($InputPath)
$form = $constructor.Invoke($constructorArguments)
try {
    $form.Show()
    if (!(Pump-Until { (Get-PrivateField $form 'stateLabel').Text -match '載入完成|部分載入|載入失敗' } 60)) { throw '等待 STDF 載入逾時。' }
    $status = (Get-PrivateField $form 'stateLabel').Text
    $status | Set-Content -LiteralPath (Join-Path $OutputDirectory 'reflect-status.txt') -Encoding UTF8
    Save-FormScreenshot $form (Join-Path $OutputDirectory 'reflect-summary.png')
    if ($status -match '載入失敗|部分載入') {
        $document = Get-PrivateField $form 'document'
        $report = [System.Collections.Generic.List[string]]::new()
        $report.Add("Status: $status")
        $report.Add("IsComplete: $($document.IsComplete); RawRecords: $($document.RawRecordCount); Diagnostics: $($document.Diagnostics -join ' | ')")
        Save-FormScreenshot $form (Join-Path $OutputDirectory 'reflect-partial-error.png')
        $tabs = Get-PrivateField $form 'tabs'
        $recordGrid = Get-PrivateField $form 'recordGrid'
        $selector = Get-PrivateField $form 'groupSelector'
        $measurements = Get-PrivateField $form 'measurementGrid'
        $trend = Get-PrivateField $form 'trendChart'
        $histogram = Get-PrivateField $form 'histogramChart'
        $tabs.SelectedIndex = 1
        [System.Windows.Forms.Application]::DoEvents()
        $report.Add("RecordRows: $($recordGrid.Rows.Count)")
        Save-FormScreenshot $form (Join-Path $OutputDirectory 'reflect-records.png')
        $tabs.SelectedIndex = 2
        [System.Windows.Forms.Application]::DoEvents()
        $report.Add("TestGroups: $($selector.Items.Count); MeasurementRows: $($measurements.Rows.Count); TrendSeries: $($trend.Series.Count); HistogramSeries: $($histogram.Series.Count)")
        Save-FormScreenshot $form (Join-Path $OutputDirectory 'reflect-measurements.png')
        $report | Set-Content -LiteralPath (Join-Path $OutputDirectory 'reflect-results.txt') -Encoding UTF8
        $report
        return
    }

    $tabs = Get-PrivateField $form 'tabs'
    $recordGrid = Get-PrivateField $form 'recordGrid'
    $propertyGrid = Get-PrivateField $form 'propertyGrid'
    $selector = Get-PrivateField $form 'groupSelector'
    $measurements = Get-PrivateField $form 'measurementGrid'
    $trend = Get-PrivateField $form 'trendChart'
    $histogram = Get-PrivateField $form 'histogramChart'
    $document = Get-PrivateField $form 'document'
    $report = [System.Collections.Generic.List[string]]::new()
    $report.Add("Status: $status")
    $report.Add("Pages: $($tabs.TabPages.Count): $((@($tabs.TabPages | ForEach-Object Text)) -join ', ')")
    $report.Add("RawRecords: $($document.RawRecordCount); Measurements: $($document.Measurements.Count); Groups: $($selector.Items.Count)")
    $report.Add("Groups: $((@($selector.Items | ForEach-Object ToString)) -join ' || ')")

    $tabs.SelectedIndex = 1
    [System.Windows.Forms.Application]::DoEvents()
    $report.Add("RecordRows: $($recordGrid.Rows.Count)")
    if ($recordGrid.Rows.Count) {
        $recordGrid.FirstDisplayedScrollingRowIndex = 0
        $recordGrid.CurrentCell = $recordGrid.Rows[0].Cells[0]
        $recordGrid.Rows[0].Selected = $true
        [System.Windows.Forms.Application]::DoEvents()
        $form.GetType().GetMethod('ShowSelectedRecord', [Reflection.BindingFlags]'Instance,NonPublic').Invoke($form, @()) | Out-Null
    }
    $selectedRecord = $propertyGrid.SelectedObject
    $firstRow = if ($recordGrid.Rows.Count) { $recordGrid.Rows[0].DataBoundItem } else { $null }
    $selectedInstance = if ($selectedRecord) { $selectedRecord.GetType().GetField('instance', [Reflection.BindingFlags]'Instance,NonPublic').GetValue($selectedRecord) } else { $null }
    $headers = @($recordGrid.Columns | ForEach-Object HeaderText)
    $farVisible = $recordGrid.ColumnHeadersVisible -and $recordGrid.Rows.Count -gt 0 -and $recordGrid.Rows[0].Displayed -and $firstRow.Name -eq 'Far' -and $firstRow.Offset -eq 0
    $report.Add("RecordsGrid: HeadersVisible=$($recordGrid.ColumnHeadersVisible); Headers=$($headers -join ','); FirstRowDisplayed=$($recordGrid.Rows[0].Displayed); FARRowVisible=$farVisible")
    if ($firstRow) { $report.Add("FirstRecordGridRow: offset=$($firstRow.Offset),name=$($firstRow.Name),record=$($firstRow.Entry.Record.GetType().FullName); PropertyGridRecord=$($selectedInstance.GetType().FullName)") }
    $descriptors = if ($selectedRecord) { [ComponentModel.TypeDescriptor]::GetProperties($selectedRecord) } else { @() }
    $editable = @($descriptors | Where-Object { !$_.IsReadOnly } | ForEach-Object Name)
    $propertyGrid.ExpandAllGridItems()
    [System.Windows.Forms.Application]::DoEvents()
    $gridItemCount = 0
    $pendingItems = New-Object System.Collections.Stack
    if ($propertyGrid.SelectedGridItem) { $pendingItems.Push($propertyGrid.SelectedGridItem) }
    while ($pendingItems.Count) {
        $gridItem = $pendingItems.Pop()
        $gridItemCount++
        foreach ($child in $gridItem.GridItems) { $pendingItems.Push($child) }
    }
    $setterRejected = $false
    if ($selectedRecord -and $descriptors.Count) {
        $firstDescriptor = $descriptors[0]
        try { $firstDescriptor.SetValue($selectedRecord, $firstDescriptor.GetValue($selectedRecord)) } catch [NotSupportedException] { $setterRejected = $true }
    }
    $converterExpandable = $selectedRecord -and $selectedRecord.GetType().GetCustomAttributes([ComponentModel.TypeConverterAttribute], $true).Count -gt 0
    $report.Add("PropertyObject: $(if($selectedRecord){$selectedRecord.GetType().FullName}else{'(none)'}); Expandable: $converterExpandable; GridItemsAfterExpandAll: $gridItemCount; EditableDescriptors: $($editable -join ', '); SetterRejected: $setterRejected")
    Save-FormScreenshot $form (Join-Path $OutputDirectory 'reflect-records.png')

    $arrayRow = $recordGrid.Rows | Where-Object { $_.Cells['TypeColumn'].Value -match 'Pgr' } | Select-Object -First 1
    if ($arrayRow) {
        $recordGrid.CurrentCell = $arrayRow.Cells[0]
        $recordGrid.ClearSelection()
        $arrayRow.Selected = $true
        [System.Windows.Forms.Application]::DoEvents()
        $showSelected = $form.GetType().GetMethod('ShowSelectedRecord', [Reflection.BindingFlags]'Instance,NonPublic')
        $showSelected.Invoke($form, @()) | Out-Null
        [System.Windows.Forms.Application]::DoEvents()
        $arrayWrapper = $propertyGrid.SelectedObject
        $arrayRecord = $arrayRow.DataBoundItem.Entry.Record
        $arrayDescriptors = [System.ComponentModel.TypeDescriptor]::GetProperties($arrayWrapper)
        $arrayDescriptor = $arrayDescriptors['PinIndexes']
        $arrayValue = if($arrayDescriptor){$arrayDescriptor.GetValue($arrayWrapper)}else{$null}
        $arrayLength = if($null -eq $arrayValue){'null'}else{$arrayValue.Length}
        $report.Add("ArrayRecord: $($arrayRecord.GetType().FullName); SelectedPropertyGridRecord=$($arrayWrapper.GetType().GetField('instance',[Reflection.BindingFlags]'Instance,NonPublic').GetValue($arrayWrapper).GetType().FullName); PinIndexesType=$($arrayDescriptor.PropertyType.FullName); PinIndexesLength=$arrayLength; ReadOnly=$($arrayDescriptor.IsReadOnly)")
        $propertyGrid.ExpandAllGridItems()
        [System.Windows.Forms.Application]::DoEvents()
        Save-FormScreenshot $form (Join-Path $OutputDirectory 'reflect-records-array.png')
    } else { $report.Add('ArrayRecord: Pgr not found') }

    $stdfAssembly = [AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq 'Stdf' } | Select-Object -First 1
    $recordTypeType = $stdfAssembly.GetType('Stdf.RecordType', $true)
    $recordTypeCtor = $recordTypeType.GetConstructor([Type[]]@([byte], [byte]))
    $recordTypeArgs = New-Object object[] 2
    $recordTypeArgs[0] = [byte]250; $recordTypeArgs[1] = [byte]42
    $rawRecordType = $recordTypeCtor.Invoke($recordTypeArgs)
    $unknownType = $stdfAssembly.GetType('Stdf.UnknownRecord', $true)
    $unknownCtor = $unknownType.GetConstructor([Type[]]@($recordTypeType, [byte[]], $stdfAssembly.GetType('Stdf.Endian', $true)))
    $unknownArgs = New-Object object[] 3
    $unknownArgs[0] = $rawRecordType; $unknownArgs[1] = [byte[]]@(0xDE, 0xAD, 0xBE, 0xEF); $unknownArgs[2] = [Enum]::Parse($stdfAssembly.GetType('Stdf.Endian'), 'Big')
    $unknownRecord = $unknownCtor.Invoke($unknownArgs)
    $unknownViewType = $form.GetType().GetNestedType('UnknownRecordView', [Reflection.BindingFlags]::NonPublic)
    $unknownViewCtor = $unknownViewType.GetConstructor([Reflection.BindingFlags]'Instance,Public,NonPublic', $null, [Type[]]@($unknownType), $null)
    $unknownViewArgs = New-Object object[] 1; $unknownViewArgs[0] = $unknownRecord
    $unknownView = $unknownViewCtor.Invoke($unknownViewArgs)
    $propertyGrid.SelectedObject = $unknownView
    $propertyGrid.ExpandAllGridItems()
    [System.Windows.Forms.Application]::DoEvents()
    $unknownProperties = [System.ComponentModel.TypeDescriptor]::GetProperties($unknownView)
    $rawHex = $unknownProperties['RawHex'].GetValue($unknownView)
    $rawTypeText = $unknownProperties['RecordType'].GetValue($unknownView)
    $report.Add("UnknownRecord: $rawTypeText; RawHex=$rawHex; RawRecordExpandable=$($unknownProperties['原始記錄'].PropertyType.GetCustomAttributes([ComponentModel.TypeConverterAttribute], $true).Count -gt 0); AllReadOnly=$(@($unknownProperties | Where-Object { !$_.IsReadOnly }).Count -eq 0)")
    Save-FormScreenshot $form (Join-Path $OutputDirectory 'reflect-records-unknown.png')

    $tabs.SelectedIndex = 2
    [System.Windows.Forms.Application]::DoEvents()
    $groupNames = @($selector.Items | ForEach-Object ToString)
    $groupNames | Set-Content -LiteralPath (Join-Path $OutputDirectory 'reflect-groups.txt') -Encoding UTF8
    $cpEntries = @($document.Measurements | Where-Object { $_.TestText -match 'Cp' })
    $report.Add("CpEntries: $($cpEntries.Count); Values: $((@($cpEntries | ForEach-Object { $_.Value.ToString('0.###E+0', [Globalization.CultureInfo]::InvariantCulture) + $_.Units })) -join ', ')")
    $cpIndex = -1
    for ($i = 0; $i -lt $selector.Items.Count; $i++) { if ($selector.Items[$i].ToString() -match '\(Cp\)') { $cpIndex = $i; break } }
    if ($cpIndex -ge 0) {
        $selector.SelectedIndex = $cpIndex
        [System.Windows.Forms.Application]::DoEvents()
        $cpMeasurement = (Get-PrivateField $form 'currentMeasurements')[0]
        $trend.Update(); $histogram.Update(); [System.Windows.Forms.Application]::DoEvents()
        $trend.SaveImage((Join-Path $OutputDirectory 'reflect-trend-cp.png'), [System.Windows.Forms.DataVisualization.Charting.ChartImageFormat]::Png)
        $histogram.SaveImage((Join-Path $OutputDirectory 'reflect-histogram-cp.png'), [System.Windows.Forms.DataVisualization.Charting.ChartImageFormat]::Png)
        $xAxis = $trend.ChartAreas['Main'].AxisX
        $yAxis = $trend.ChartAreas['Main'].AxisY
        $xMin = $xAxis.ActualMinimum; $xMax = $xAxis.ActualMaximum
        $yMin = $yAxis.ActualMinimum; $yMax = $yAxis.ActualMaximum
        $chartArea = $trend.ChartAreas['Main']
        $outer = $chartArea.Position
        $inner = $chartArea.InnerPlotPosition
        $plotLeft = ($outer.X + $outer.Width * $inner.X / 100) * $trend.Width / 100
        $plotRight = ($outer.X + $outer.Width * ($inner.X + $inner.Width) / 100) * $trend.Width / 100
        $plotTop = ($outer.Y + $outer.Height * $inner.Y / 100) * $trend.Height / 100
        $plotBottom = ($outer.Y + $outer.Height * ($inner.Y + $inner.Height) / 100) * $trend.Height / 100
        $cpPoints = $trend.Series[0].Points
        $cpPoint = if ($cpPoints.Count) { $cpPoints[0] } else { $null }
        $axisContainsPoint = $cpPoint -and $xMin -le $cpPoint.XValue -and $xMax -ge $cpPoint.XValue -and $yMin -le $cpPoint.YValues[0] -and $yMax -ge $cpPoint.YValues[0]
        $pixelX = $xAxis.ValueToPixelPosition($cpPoint.XValue)
        $pixelY = $yAxis.ValueToPixelPosition($cpPoint.YValues[0])
        $pointInsidePlot = $pixelX -ge $plotLeft -and $pixelX -le $plotRight -and $pixelY -ge $plotTop -and $pixelY -le $plotBottom
        $report.Add("CpGroup: $($selector.SelectedItem); TestNumber=$($cpMeasurement.TestNumber); Sequence=$($cpMeasurement.Sequence); Value=$($cpMeasurement.Value.ToString('0.###E+0', [Globalization.CultureInfo]::InvariantCulture))$($cpMeasurement.Units); Rows=$($measurements.Rows.Count); ValidTrendPoints=$($cpPoints.Count); PointXY=$($cpPoint.XValue),$($cpPoint.YValues[0]); AxisPixelRangeX=$plotLeft..$plotRight; AxisPixelRangeY=$plotTop..$plotBottom; DataPointPixel=$pixelX,$pixelY; AxisContainsPoint=$pointInsidePlot; HistogramSeries=$($histogram.Series.Count); HistogramPoints=$(if($histogram.Series.Count){$histogram.Series[0].Points.Count}else{0})")
        Save-FormScreenshot $form (Join-Path $OutputDirectory 'reflect-measurements-cp.png')
    } else { $report.Add('CpGroup: NOT FOUND') }

    $invalidIndex = -1
    $invalidSummary = ''
    $unknownValidIndex = -1
    $unknownValidSummary = ''
    for ($i = 0; $i -lt $selector.Items.Count; $i++) {
        $selector.SelectedIndex = $i
        [System.Windows.Forms.Application]::DoEvents()
        $current = Get-PrivateField $form 'currentMeasurements'
        $invalid = @($current | Where-Object { $_.TestNumber -eq 14 })
        if ($invalid.Count -and $invalidIndex -lt 0) { $invalidIndex = $i; $invalidSummary = ($invalid | ForEach-Object { "test=$($_.TestNumber),status=$($_.Status),value=$($_.Value),reason=$($_.InvalidReason)" }) -join '; ' }
        $unknownValid = @($current | Where-Object { $_.Status -eq 'Unknown' -and $null -ne $_.Value })
        if ($unknownValid.Count -and $unknownValidIndex -lt 0) { $unknownValidIndex = $i; $unknownValidSummary = ($unknownValid | ForEach-Object { "status=$($_.Status),value=$($_.Value),reason=$($_.InvalidReason)" }) -join '; ' }
    }
    if ($unknownValidIndex -ge 0) {
        $selector.SelectedIndex = $unknownValidIndex
        [System.Windows.Forms.Application]::DoEvents()
        $report.Add("UnknownWithValueGroup: $($selector.SelectedItem); Rows: $($measurements.Rows.Count); TrendPoints: $($trend.Series[0].Points.Count); Values: $unknownValidSummary")
        Save-FormScreenshot $form (Join-Path $OutputDirectory 'reflect-measurements-unknown-valid.png')
    } else { $report.Add('UnknownWithValueGroup: NOT FOUND') }
    if ($invalidIndex -ge 0) {
        $selector.SelectedIndex = $invalidIndex
        [System.Windows.Forms.Application]::DoEvents()
        $invalidMeasurement = (Get-PrivateField $form 'currentMeasurements' | Where-Object { $_.TestNumber -eq 14 } | Select-Object -First 1)
        $histogramPoints = if ($histogram.Series.Count) { $histogram.Series[0].Points.Count } else { 0 }
        $report.Add("InvalidGroup: $($selector.SelectedItem); TestNumber: $($invalidMeasurement.TestNumber); Status: $($invalidMeasurement.Status); ValueIsNull: $($null -eq $invalidMeasurement.Value); ValidTrendPoints: $($trend.Series[0].Points.Count); LimitSeries: $($trend.Series.Count - 1); HistogramSeries: $($histogram.Series.Count); HistogramPoints: $histogramPoints; Values: $invalidSummary")
        Save-FormScreenshot $form (Join-Path $OutputDirectory 'reflect-measurements-invalid.png')
    } else { $report.Add('InvalidGroup: NOT FOUND') }

    $coreAssembly = [Reflection.Assembly]::LoadFrom((Join-Path (Split-Path -Parent $ExePath) 'StdfReader.Core.dll'))
    $measurementType = $coreAssembly.GetType('StdfReader.Core.MeasurementEntry', $true)
    $syntheticType = [System.Collections.Generic.List``1].MakeGenericType(@($measurementType))
    $synthetic = [Activator]::CreateInstance($syntheticType)
    $syntheticEntry = [Activator]::CreateInstance($measurementType)
    $syntheticEntry.Sequence = 1
    $syntheticEntry.TestNumber = 9001
    $syntheticEntry.HeadNumber = [byte]1
    $syntheticEntry.SiteNumber = [byte]1
    $syntheticEntry.Units = 'F'
    $syntheticEntry.TestText = 'Synthetic unknown status'
    $syntheticEntry.Value = 1e-9
    $syntheticEntry.Status = 'Unknown'
    $syntheticEntry.TestFlags = 0x40
    $synthetic.Add($syntheticEntry)
    $drawParameters = [Type[]]@($syntheticType)
    foreach ($methodName in @('DrawTrend', 'DrawHistogram')) {
        $method = $form.GetType().GetMethod($methodName, [Reflection.BindingFlags]'Instance,NonPublic', $null, $drawParameters, $null)
        $methodArguments = New-Object object[] 1
        $methodArguments[0] = $synthetic
        $method.Invoke($form, $methodArguments) | Out-Null
    }
    [System.Windows.Forms.Application]::DoEvents()
    $report.Add("SyntheticUnknownValid: TestFlags=0x40; Status=$($syntheticEntry.Status); Value=$($syntheticEntry.Value); TrendPoints=$($trend.Series[0].Points.Count); HistogramSeries=$($histogram.Series.Count); HistogramPoints=$(if($histogram.Series.Count){$histogram.Series[0].Points.Count}else{0})")
    Save-FormScreenshot $form (Join-Path $OutputDirectory 'reflect-synthetic-unknown-valid.png')

    $partSelector = Get-PrivateField $form 'partSelector'
    if ($cpIndex -ge 0) { $selector.SelectedIndex = $cpIndex; [System.Windows.Forms.Application]::DoEvents() }
    $form.Size = New-Object Drawing.Size(1000,700)
    [System.Windows.Forms.Application]::DoEvents()
    $formClient = $form.RectangleToScreen($form.ClientRectangle)
    $partBounds = $partSelector.RectangleToScreen($partSelector.ClientRectangle)
    $partInside = $partBounds.Left -ge $formClient.Left -and $partBounds.Right -le $formClient.Right -and $partBounds.Top -ge $formClient.Top -and $partBounds.Bottom -le $formClient.Bottom
    $report.Add("Resize: FormClient=$($formClient.Width)x$($formClient.Height); PartSelector=$($partBounds.Left),$($partBounds.Top),$($partBounds.Width),$($partBounds.Height); InsideClient=$partInside")
    Save-FormScreenshot $form (Join-Path $OutputDirectory 'reflect-measurements-resize.png')
    $report | Set-Content -LiteralPath (Join-Path $OutputDirectory 'reflect-results.txt') -Encoding UTF8
    $report
}
finally {
    $form.Close()
    $form.Dispose()
}
