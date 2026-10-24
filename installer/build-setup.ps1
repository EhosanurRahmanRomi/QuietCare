param(
    [Parameter(Mandatory=$true)][string]$Package,
    [Parameter(Mandatory=$true)][string]$Output
)
$ErrorActionPreference='Stop'
$packagePath=(Resolve-Path -LiteralPath $Package).Path
$outputPath=[IO.Path]::GetFullPath($Output)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($outputPath))|Out-Null
$sourcePath=Join-Path ([IO.Path]::GetDirectoryName($outputPath)) 'Setup.generated.cs'
$source=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Setup.cs')).Replace('PACKAGE_SHA256',(Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash)
[IO.File]::WriteAllText($sourcePath,$source)
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$icon=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../src/Cleaner.App/Assets/QuietCare.ico')).Path
$manifest=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../src/Cleaner.App/app.manifest')).Path
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /warn:4 /warnaserror+ '/reference:System.dll' '/reference:System.Core.dll' '/reference:System.Drawing.dll' '/reference:System.Windows.Forms.dll' "/win32manifest:$manifest" "/win32icon:$icon" "/resource:$packagePath,QuietCarePackage" "/out:$outputPath" $sourcePath
if($LASTEXITCODE -ne 0){throw 'Setup compilation failed.'}
Get-FileHash -LiteralPath $outputPath -Algorithm SHA256
