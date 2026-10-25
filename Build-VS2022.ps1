param(
	[ValidateSet('All', 'Debug', 'Release')]
	[string]$Configuration = 'All',
	[string]$Platform = 'Any CPU'
)

function Stop-Build([string]$message, [int]$exitCode = 1)
{
	[Console]::Error.WriteLine("ERROR: $message")
	exit $exitCode
}

$solutionPath = Join-Path $PSScriptRoot 'Main\Project_Stdf.sln'
if(-not (Test-Path -LiteralPath $solutionPath -PathType Leaf))
{
	Stop-Build "Solution file not found: $solutionPath"
}

$vswherePath = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)) 'Microsoft Visual Studio\Installer\vswhere.exe'
if(-not (Test-Path -LiteralPath $vswherePath -PathType Leaf))
{
	Stop-Build "vswhere.exe not found: $vswherePath. Install Visual Studio 2022 or Visual Studio 2026 (or their Build Tools)."
}

# vswhere 版本範圍 [17.0,19.0) 同時接受 VS2022 (17.x) 與 VS2026 (18.x)；-latest 會優先選擇已安裝的最新執行個體。
$installationPath = & $vswherePath -products '*' -version '[17.0,19.0)' -requires Microsoft.Component.MSBuild -latest -property installationPath
$vswhereExitCode = $LASTEXITCODE
if($vswhereExitCode -ne 0)
{
	Stop-Build 'vswhere failed while locating Visual Studio 2022 or 2026.' $vswhereExitCode
}
if([string]::IsNullOrWhiteSpace($installationPath))
{
	Stop-Build 'Visual Studio 2022 or 2026 with the MSBuild component was not found. Install VS2022/VS2026 or their Build Tools with MSBuild.'
}

$msbuildPath = Join-Path $installationPath 'MSBuild\Current\Bin\MSBuild.exe'
if(-not (Test-Path -LiteralPath $msbuildPath -PathType Leaf))
{
	Stop-Build "MSBuild.exe not found in the Visual Studio installation: $msbuildPath"
}

$configurations = if($Configuration -eq 'All') { @('Debug', 'Release') } else { @($Configuration) }
foreach($buildConfiguration in $configurations)
{
	Write-Host "Building $buildConfiguration ($Platform) with Visual Studio MSBuild at $installationPath..."
	$buildArguments = @(
		$solutionPath,
		'-target:Build',
		"-property:Configuration=$buildConfiguration",
		"-property:Platform=$Platform",
		'-verbosity:minimal',
		'-nologo'
	)
	& $msbuildPath @buildArguments
	$buildExitCode = $LASTEXITCODE
	if($buildExitCode -ne 0)
	{
		Stop-Build "MSBuild failed for $buildConfiguration ($Platform)." $buildExitCode
	}

	$outputPath = Join-Path $PSScriptRoot "Main\StdfConverter\bin\$buildConfiguration\StdfConverter.exe"
	if(-not (Test-Path -LiteralPath $outputPath -PathType Leaf))
	{
		Stop-Build "Build succeeded but the expected executable was not produced: $outputPath"
	}
	Write-Host "Output: $outputPath"
}

Write-Host 'Build completed successfully.'
