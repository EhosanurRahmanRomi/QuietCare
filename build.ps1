$ErrorActionPreference = 'Stop'
$project = $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
$destination = Join-Path $project 'build'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
$references = @('System.dll','System.Core.dll','System.Xml.dll','System.Xml.Linq.dll','System.Management.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Xaml.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$references += @('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path (Join-Path $framework 'WPF') $_) }
$sources = Get-ChildItem -LiteralPath (Join-Path $project 'src') -Filter '*.cs' | ForEach-Object FullName
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /warn:4 /warnaserror+ "/win32manifest:$project\app.manifest" "/win32icon:$project\quietcare.ico" "/out:$destination\QuietCare.exe" @references @sources
if ($LASTEXITCODE -ne 0) { throw 'QuietCare compilation failed.' }
Write-Output (Join-Path $destination 'QuietCare.exe')
