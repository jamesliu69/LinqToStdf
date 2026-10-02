param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$TargetFrameworkVersion
)

$ErrorActionPreference = 'Stop'
$solutionPath = Join-Path $PSScriptRoot '..\Project_Stdf.sln'
$buildArguments = @(
    'msbuild',
    $solutionPath,
    '-t:Build',
    "-p:Configuration=$Configuration",
    '-verbosity:minimal',
    '-nologo'
)
if ($TargetFrameworkVersion) {
    $buildArguments += "-p:TargetFrameworkVersion=$TargetFrameworkVersion"
}

& dotnet @buildArguments
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$testExecutable = Join-Path $PSScriptRoot "bin\$Configuration\LinqToStdf.RegressionTests.exe"
& $testExecutable
exit $LASTEXITCODE
