param([string]$Output = (Join-Path $PSScriptRoot 'artifacts/portable'))
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'src/Cleaner.App/Cleaner.App.csproj'
& dotnet restore $project -r win-x64 -p:SelfContained=true -p:PublishSingleFile=true --locked-mode
if ($LASTEXITCODE -ne 0) { throw 'Locked restore failed.' }
& dotnet publish $project -c Release -r win-x64 --self-contained true --no-restore -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false -o $Output
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
Get-FileHash -LiteralPath (Join-Path $Output 'QuietCare.Next.exe') -Algorithm SHA256
