# Build, run and package

## Requirements

- Windows 10 or Windows 11, x64.
- .NET Framework 4.8 or later, including `Framework64/v4.0.30319/csc.exe` and the WPF assemblies bundled with Windows.
- Windows PowerShell for the provided scripts; Git for cloning and repository checks.
- A normal interactive Windows session for full local UI, Core Audio and hardware validation.

No NuGet/npm dependency restore, downloaded compiler, separate SDK or Visual Studio installation is required. This is a script-driven Framework project; there is no `.sln`/SDK-style `.csproj` yet.

```powershell
git clone https://github.com/EhosanurRahmanRomi/QuietCare.git
cd QuietCare
.\build.ps1
.\build\QuietCare.exe
```

For tray/monitor startup:

```powershell
.\build\QuietCare.exe --tray
```

## Validation

```powershell
.\test.ps1
.\test-ui.ps1
.\test-updates.ps1
.\test-care.ps1
.\package.ps1
.\test-installer.ps1
```

`test.ps1` builds core tests and the isolated memory helper. The suite includes read-only live hardware/audio checks, filesystem junction/hard-link fixtures, and a trim of its own dedicated helper. It does not trim user apps or modify real user startup entries. Full local results depend on a working Windows desktop/audio environment.

`test-ui.ps1` compiles production WPF controls with a test entry point. It checks layout, minimum-width behavior, transparency, value order, sorting/tooltips and direct Boost wiring with a fake scoped runtime. Automated layout checks are separate from human native visual review.

`test-updates.ps1` uses fake HTTP with real hash/cache/native file guards. Add `-Live` to check real project metadata/download bytes without execution. `test-care.ps1` checks scan orchestration and fixture temp progress; `-Live` adds six real read-only Windows checks.

`test-installer.ps1` embeds the generated payload in a separate fixture-only installer executable. Its install paths and uninstall registry key are selected at compile time. It checks payload hashes, shortcuts/registration, reinstall, unrelated shortcut preservation and uninstall data retention. Run `package.ps1` first so `build/Setup.generated.cs` matches the payload.

The production application and installer are optimized x64 builds with compiler warnings treated as errors. The installer verifies its embedded application payload against its generated SHA-256 constant. A source rebuild can have a different binary hash because Framework compiler metadata is not configured for deterministic output.

## Outputs and local data

| Path | Contents |
| --- | --- |
| `build/` | Executables, generated installer source, local test logs and benchmark output. |
| `test-runs/` | Isolated test directories retained for inspection. |
| `%LOCALAPPDATA%/QuietCare` | Normal application settings, local activity log, recovery and backups. |
| `%LOCALAPPDATA%/Programs/QuietCare` | Per-user installation files. |

Generated binaries and user state are excluded from Git. Official preview binaries are distributed as GitHub release assets. Do not publish `test-runs`, settings, recovery contents, personal recordings or private logs.

## CI

The Windows workflow builds/packages the app, runs WPF, offline updater/Care and isolated installer checks, validates repository boundaries, and uploads unsigned build artifacts. It uses a read-only token and commit-pinned official actions. CI does not automatically publish releases or run the full hardware/audio-dependent core suite. Local native evidence is separately recorded in [VALIDATION.md](VALIDATION.md).

## Benchmarks

```powershell
.\benchmark.ps1 -Seconds 20
```

The benchmark starts one process at a time: compact 1,000/250/100 ms, then fully hidden. Do not interact with QuietCare during a run. Check `mainUiConstructed` before treating a sample as floating-only. The script retains raw JSON under `build/`. See [PERFORMANCE.md](PERFORMANCE.md).
