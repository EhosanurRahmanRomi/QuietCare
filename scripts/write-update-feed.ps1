param(
    [Parameter(Mandatory=$true)][string]$Tag,
    [Parameter(Mandatory=$true)][string]$Installer,
    [Parameter(Mandatory=$true)][string]$NotesFile,
    [switch]$Preview
)
$ErrorActionPreference = 'Stop'
if ($Tag -notmatch '^v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z]+([.-][0-9A-Za-z]+)*)?$') { throw 'Use a plain vMajor.Minor.Patch version tag, optionally with a preview suffix.' }
if ($Tag.Contains('-') -and -not $Preview) { throw 'A prerelease tag requires -Preview.' }
$file = Get-Item -LiteralPath $Installer
if ($file.Name -cne 'QuietCare-Setup.exe' -or $file.Length -lt 2 -or $file.Length -gt 67108864) { throw 'Expected the final QuietCare-Setup.exe, at most 64 MiB.' }
$notes = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $NotesFile))
if ($notes.Length -gt 12000) { throw 'Release notes exceed the app limit.' }
$sha = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
$release = [ordered]@{
    tag_name = $Tag
    name = "QuietCare $Tag"
    html_url = "https://github.com/EhosanurRahmanRomi/QuietCare/releases/tag/$Tag"
    draft = $false
    prerelease = [bool]$Preview
    body = $notes
    assets = @([ordered]@{
        name = 'QuietCare-Setup.exe'
        browser_download_url = "https://github.com/EhosanurRahmanRomi/QuietCare/releases/download/$Tag/QuietCare-Setup.exe"
        size = $file.Length
        digest = "sha256:$sha"
    })
}
$directory = Join-Path (Split-Path -Parent $PSScriptRoot) 'updates'
New-Item -ItemType Directory -Force -Path $directory | Out-Null
$channel = if ($Preview) { 'preview' } else { 'stable' }
$path = Join-Path $directory ($channel + '.json')
$json = ConvertTo-Json -InputObject @($release) -Depth 5
[IO.File]::WriteAllText($path,$json + [Environment]::NewLine,[Text.UTF8Encoding]::new($false))
Write-Output "Generated $channel feed: $Tag; $($file.Length) bytes; SHA-256 $sha"
