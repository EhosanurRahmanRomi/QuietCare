param([int]$Seconds = 20)
$ErrorActionPreference = 'Stop'
$project = $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$references = @('System.Net.Http.dll','System.Web.Extensions.dll','System.dll','System.Core.dll','System.Xml.dll','System.Xml.Linq.dll','System.Management.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xaml.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$references += @('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path (Join-Path $framework 'WPF') $_) }
$sources = Get-ChildItem -LiteralPath (Join-Path $project 'src') -Filter '*.cs' | ForEach-Object FullName
& (Join-Path $framework 'csc.exe') /nologo /main:MonitorBench /target:exe /platform:x64 /optimize+ /warn:4 /warnaserror+ "/out:$project\build\MonitorBench.exe" @references @sources (Join-Path $project 'tests\MonitorBench.cs')
if ($LASTEXITCODE -ne 0) { throw 'Benchmark compilation failed.' }
$rows = @()
foreach ($case in @(@{Rate=1000;Mode='compact'},@{Rate=250;Mode='compact'},@{Rate=100;Mode='compact'},@{Rate=250;Mode='hidden'})) {
    $fixture = Join-Path $project ('test-runs\bench-' + [Guid]::NewGuid().ToString('N'))
    $result = & (Join-Path $project 'build\MonitorBench.exe') $case.Rate $Seconds $case.Mode $fixture
    if ($LASTEXITCODE -ne 0) { throw 'Benchmark failed.' }
    Write-Output $result
    $rows += $result | ConvertFrom-Json
}
$rows | ConvertTo-Json | Set-Content -Encoding UTF8 (Join-Path $project 'build\benchmark-results.json')
