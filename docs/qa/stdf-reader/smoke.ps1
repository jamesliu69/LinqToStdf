param(
    [string]$ExePath = (Join-Path $PSScriptRoot '..\..\..\Main\StdfReader\bin\Debug\StdfReader.exe'),
    [string]$InputPath = (Join-Path $PSScriptRoot '..\..\..\Main\StdfConverter\bin\Release\testSTDF.stdf'),
    [string]$OutputDirectory = $PSScriptRoot
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class WindowCapture {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
'@

function Save-WindowScreenshot([IntPtr]$Handle, [string]$Path) {
    [WindowCapture]::SetForegroundWindow($Handle) | Out-Null
    Start-Sleep -Milliseconds 150
    $rect = New-Object WindowCapture+RECT
    if (![WindowCapture]::GetWindowRect($Handle, [ref]$rect)) { throw '無法取得視窗位置。' }
    $bitmap = New-Object System.Drawing.Bitmap(($rect.Right - $rect.Left), ($rect.Bottom - $rect.Top))
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose(); $bitmap.Dispose()
}

function Click-WindowPoint([IntPtr]$Handle, [int]$X, [int]$Y) {
    $rect = New-Object WindowCapture+RECT
    if (![WindowCapture]::GetWindowRect($Handle, [ref]$rect)) { throw '無法取得視窗位置。' }
    [WindowCapture]::SetCursorPos(($rect.Left + $X), ($rect.Top + $Y)) | Out-Null
    [WindowCapture]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    [WindowCapture]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
}

if (!(Test-Path -LiteralPath $ExePath)) { throw "找不到 Reader 執行檔：$ExePath" }
if (!(Test-Path -LiteralPath $InputPath)) { throw "找不到輸入 STDF：$InputPath" }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$process = Start-Process -FilePath $ExePath -ArgumentList @('"' + (Resolve-Path -LiteralPath $InputPath).Path + '"') -PassThru
try {
    if (!$process.WaitForInputIdle(15000)) { throw 'Reader 在 15 秒內未進入互動狀態。' }
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $window = $null
    $deadline = (Get-Date).AddSeconds(15)
    while ((Get-Date) -lt $deadline -and !$window) {
        $process.Refresh()
        if ($process.MainWindowHandle -ne [IntPtr]::Zero) {
            $condition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NativeWindowHandleProperty, [int]$process.MainWindowHandle)
            $window = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $condition)
        }
        Start-Sleep -Milliseconds 250
    }
    if (!$window) { throw '找不到 Reader 主視窗。' }
    [WindowCapture]::SetForegroundWindow($process.MainWindowHandle) | Out-Null
    $status = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'LoadStatusLabel')))
    Start-Sleep -Seconds 3
    $statusText = if ($status) { $status.Current.Name } else { '(UI Automation 未提供狀態文字)' }
    $statusText | Set-Content -LiteralPath (Join-Path $OutputDirectory 'startup-status.txt') -Encoding UTF8
    Save-WindowScreenshot $process.MainWindowHandle (Join-Path $OutputDirectory 'startup.png')
    $loadFailed = $statusText -match '載入失敗'

    $elements = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    $lines = foreach ($element in $elements) {
        $name = $element.Current.Name -replace '[\r\n]+', ' '
        if ($name -or $element.Current.AutomationId) {
            '{0}`t{1}`t{2}' -f $element.Current.ControlType.ProgrammaticName, $element.Current.AutomationId, $name
        }
    }
    $lines | Set-Content -LiteralPath (Join-Path $OutputDirectory 'controls.txt') -Encoding UTF8

    if ($loadFailed) { Save-WindowScreenshot $process.MainWindowHandle (Join-Path $OutputDirectory 'Error.png') }
    $pagePoints = @(
        @{ Name = '摘要'; X = 38; Y = 90 },
        @{ Name = '記錄'; X = 78; Y = 90 },
        @{ Name = '量測與圖表'; X = 142; Y = 90 }
    )
    foreach ($page in $pagePoints) {
        Click-WindowPoint $process.MainWindowHandle $page.X $page.Y
        Start-Sleep -Milliseconds 700
        $safeName = $page.Name -replace '[^\p{L}\p{N}]', '_'
        Save-WindowScreenshot $process.MainWindowHandle (Join-Path $OutputDirectory ($safeName + '.png'))
        $pageElements = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
        $pageLines = foreach ($element in $pageElements) {
            $name = $element.Current.Name -replace '[\r\n]+', ' '
            if ($name -or $element.Current.AutomationId) { '{0}`t{1}`t{2}' -f $element.Current.ControlType.ProgrammaticName, $element.Current.AutomationId, $name }
        }
        $pageLines | Set-Content -LiteralPath (Join-Path $OutputDirectory ($safeName + '-controls.txt')) -Encoding UTF8
    }

    if (!$loadFailed) {
    Click-WindowPoint $process.MainWindowHandle 142 90
    Start-Sleep -Milliseconds 300
    $testSelector = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ComboBox)))
    if ($testSelector) {
        try { $testSelector.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand() } catch { }
        Start-Sleep -Milliseconds 250
        $items = $testSelector.FindAll([System.Windows.Automation.TreeScope]::Descendants,
            (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ListItem)))
        $itemNames = @($items | ForEach-Object { $_.Current.Name })
        $itemNames | Set-Content -LiteralPath (Join-Path $OutputDirectory 'test-groups.txt') -Encoding UTF8
        $cpItem = $items | Where-Object { $_.Current.Name -match 'Cp|1E-9' } | Select-Object -First 1
        if ($cpItem) {
            $cpItem.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
            Start-Sleep -Milliseconds 300
            Save-WindowScreenshot $process.MainWindowHandle (Join-Path $OutputDirectory 'Measurements-Cp.png')
        }
        $invalidItem = $items | Where-Object { $_.Current.Name -match 'invalid|MS_ER|Unknown' } | Select-Object -First 1
        if ($invalidItem) {
            $invalidItem.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
            Start-Sleep -Milliseconds 300
            Save-WindowScreenshot $process.MainWindowHandle (Join-Path $OutputDirectory 'Measurements-Invalid.png')
        }
    }

    [WindowCapture]::SetWindowPos($process.MainWindowHandle, [IntPtr]::Zero, 30, 30, 1000, 700, 0x0040) | Out-Null
    Start-Sleep -Milliseconds 500
    Save-WindowScreenshot $process.MainWindowHandle (Join-Path $OutputDirectory 'Measurements-Resize.png')
    }
    Write-Output "完成 GUI 操作；控制項清單：$(Join-Path $OutputDirectory 'controls.txt')"
}
finally {
    if ($process -and !$process.HasExited) { $process.CloseMainWindow(); if (!$process.WaitForExit(3000)) { $process.Kill() } }
}
