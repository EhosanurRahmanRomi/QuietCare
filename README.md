<p align="center"><img src="docs/images/banner.svg" alt="QuietCare — PC care without the noise" width="100%"></p>

<p align="center">
  <a href="https://github.com/EhosanurRahmanRomi/QuietCare/releases/tag/v0.3.0"><img alt="Preview release" src="https://img.shields.io/badge/preview-v0.3.0-62ddff?style=flat-square"></a>
  <img alt="Windows 10 and 11, x64" src="https://img.shields.io/badge/platform-Windows%2010%2F11%20x64-467cff?style=flat-square">
  <img alt=".NET Framework 4.8" src="https://img.shields.io/badge/runtime-.NET%20Framework%204.8-ba9aff?style=flat-square">
  <a href="LICENSE"><img alt="MIT license" src="https://img.shields.io/badge/license-MIT-64e5b7?style=flat-square"></a>
  <a href="https://github.com/EhosanurRahmanRomi/QuietCare/actions/workflows/windows.yml"><img alt="Windows build and checks" src="https://github.com/EhosanurRahmanRomi/QuietCare/actions/workflows/windows.yml/badge.svg"></a>
</p>

<p align="center"><strong>A tiny monitor when you need a glance. Clear maintenance tools when you need to act.</strong></p>
<p align="center"><a href="#download">Download</a> · <a href="docs/USER-GUIDE.txt">User guide</a> · <a href="docs/FEATURES.md">Features</a> · <a href="docs/ARCHITECTURE.md">Architecture</a> · <a href="docs/VALIDATION.md">Validation</a> · <a href="CONTRIBUTING.md">Contribute</a></p>

QuietCare is an original, ad-free Windows desktop utility built with C# and WPF. It combines a transparent CPU/GPU/RAM monitor, guarded one-click working-set trimming, startup management, and recoverable cleanup in a local-first interface. There are no accounts, advertisements, telemetry uploads, bundled drivers, or background services.

> **Preview status:** v0.3.0 is unsigned and has limited hardware coverage. It has not received an independent security audit. Some tools open Windows' own settings and consoles; QuietCare is not an antivirus or a complete Advanced SystemCare Pro replacement.

## A clearer desktop

![QuietCare 0.3 Care layout rendered from production WPF controls](docs/images/Care-home-render.png)

The main interface uses a continuous navy canvas, transparent window controls, compact navigation and focused actions. This 0.3.0 image is a production WPF control render from the test harness; its sensor/Boost state is a fixture.

![Live scan layout with labeled synthetic data](docs/images/Care-live-fixture.png)

The live queue, current-item display and Skip/Stop controls use labeled test data in this image. The real backend was tested separately against Windows. [Validation scope](docs/VALIDATION.md).

### The monitor stays small

Historical 0.2.0 native capture (the compact layout is retained):

![Native transparent CPU, GPU and RAM strip](docs/images/QuietCare-monitor.png)

The captured strip is **205 × 28 px**. Its width follows its contents. Readings stay in **CPU → GPU → RAM** order; drag to move, expand for disk/network details, or right-click for refresh and transparency settings.

## What it does

| Capability | Behavior |
| --- | --- |
| Transparent monitor | Adjustable 100–10,000 ms refresh, transparency, topmost mode, expandable details, and sign-in startup. |
| Live Care scan | Six read-only checks with current-step/item feedback, Skip/Stop, partial/unavailable states and expandable observations. |
| App updates | Manual GitHub checks, preview/stable choice, optional weekly startup check, verified installer and support links. |
| Direct Smart RAM Boost | The floating lightning button checks qualifying idle background processes and trims their working sets without opening a chooser. |
| Startup manager | Review startup entries; back up, disable, and restore current-user Run entries without overwriting restore collisions. |
| Recoverable temp cleanup | Preview eligible old user-temp files, quarantine selected files, restore them, or confirm permanent deletion. |
| File discovery | Read-only large-file and SHA-256 duplicate-content finders with bounded work and cancellation. |
| System toolbox | Hardware/app inventory, power-plan recovery, CSV export, and 37 fixed Windows maintenance entry points. |

See the [feature matrix](docs/FEATURES.md) for the distinction between implemented tools, Windows integrations, and unavailable features.

## Download

**[Get the v0.3.0 Windows preview](https://github.com/EhosanurRahmanRomi/QuietCare/releases/tag/v0.3.0)**

| Asset | Use |
| --- | --- |
| [QuietCare-Setup.exe](https://github.com/EhosanurRahmanRomi/QuietCare/releases/download/v0.3.0/QuietCare-Setup.exe) | Per-user installer with Start menu shortcuts and an uninstaller. |
| [QuietCare.exe](https://github.com/EhosanurRahmanRomi/QuietCare/releases/download/v0.3.0/QuietCare.exe) | Portable application. Put it in its final location before enabling startup. |
| [QuietCare-source-v0.3.0.zip](https://github.com/EhosanurRahmanRomi/QuietCare/releases/download/v0.3.0/QuietCare-source-v0.3.0.zip) | The tagged source, documentation, tests, and images. |
| [SHA256SUMS.txt](https://github.com/EhosanurRahmanRomi/QuietCare/releases/download/v0.3.0/SHA256SUMS.txt) | File integrity checksums; these do not replace a code signature. |

Requires **Windows 10/11 x64** and **.NET Framework 4.8 or later**. Installation and normal use do not request administrator access. Windows may show an unsigned-application warning; review the source and provenance before deciding whether to run it. QuietCare does not automatically uninstall another product.

## RAM Boost, explained honestly

Boost uses Microsoft's documented `EmptyWorkingSet` API. It asks Windows to reduce resident pages in eligible idle background processes; it does not terminate, suspend, or restart applications or discard/reload browser tabs.

Foreground processes, active audio/calls, Windows processes and common project tools are protected. Settings also controls whether idle renderers with Windows background priority may qualify inside an active browser. Browser controllers and extensions remain excluded. Add browser process names to protect all background web projects. Unknown media state prevents trimming; work is capped with a 30-second cooldown.

Boost results distinguish Windows-accepted requests from measured resident decreases, show physical RAM before/after, and explain skip/failure reasons. Working-set trimming does not free committed allocations; pages may return with latency. Lower displayed RAM use is **not a guaranteed speed improvement**. [Policy and limits](docs/RAM-BOOST.md).

## App updates and support

Open **Updates > Check for updates**. Review notes, download the size/SHA-256-verified installer, then explicitly choose whether to open it. Preview/stable channels and an opt-in weekly startup check are in Settings. There is no silent installation. Network use is limited to GitHub/project release infrastructure when you check or download. [Update design and maintainer procedure](docs/UPDATES.md).

## Built to stay quiet

Compact mode collects CPU/GPU/RAM only; disk and network discovery starts with the expanded monitor. The main UI is built when opened and released on close to tray. Sampling pauses when both windows are hidden, when only a minimized main window remains, and during suspension.

v0.3.0 local measurements on one i5-8350U / Intel UHD 620 / 16 GB PC, with about 20 seconds per case after a 5-second warmup:

| Refresh | Total CPU | Working set |
| --- | ---: | ---: |
| 1,000 ms | 0.185% | 97.3 MB |
| 250 ms | 0.722% | 98.6 MB |
| 100 ms | 1.749% | 100.5 MB |

The main UI stayed unconstructed in these runs. Fully hidden polling was disabled. These are short local observations under other active workloads, not guaranteed bounds or long-duration/battery results. [Method, raw data, and limits](docs/PERFORMANCE.md).

## Build and test

No NuGet/npm restore or separate SDK is required. In **Windows PowerShell**, from the repository root:

```powershell
.\build.ps1
.\test.ps1
.\test-ui.ps1
.\test-updates.ps1
.\test-care.ps1
.\package.ps1
.\test-installer.ps1
```

The scripts use Windows' bundled .NET Framework 64-bit C# compiler. Binaries are written to `build/`; isolated fixtures stay in `test-runs/`, which is ignored by Git. See [BUILDING.md](docs/BUILDING.md) for prerequisites, expected output, CI coverage, and benchmark instructions.

The v0.3.0 checks include core/process protection, WPF controls, updater guards, Care orchestration and isolated installer lifecycle. A live helper test verifies its 96 MB allocation after trimming; the Care live test performs read-only Windows checks. Tests do not establish all-app compatibility or guaranteed performance gains. [Dated results and scope](docs/VALIDATION.md).

## For developers

```text
src/                 WPF shell, telemetry, maintenance services and native interop
installer/           Per-user installer and uninstaller
tests/               Core, WPF layout, installer and benchmark harnesses
docs/                Guides, design notes, screenshots and sanitized evidence
scripts/             Repository hygiene and release-feed generation
updates/             Fixed stable/preview fallback release metadata
.github/             Windows CI and contribution templates
```

Start with the [architecture](docs/ARCHITECTURE.md), [security model](docs/SECURITY-MODEL.md), [contribution guide](CONTRIBUTING.md), and [roadmap](docs/ROADMAP.md). Bug reports and focused pull requests are welcome. Please report vulnerabilities privately using the [security policy](SECURITY.md).

## Inspiration and license

The UI reference informed the circular scan and tiny transparent monitor. The whole-app review covers Advanced SystemCare, Microsoft PC Manager, Glary Utilities, Wise Care and BleachBit, plus 11 related GitHub projects including PowerClean, WinMemoryCleaner, Mem Reduct, TrafficMonitor and update frameworks. [Specific sources, licenses and implementation decisions](docs/RELATED-PROJECTS.md) distinguish actual source review from product documentation.

QuietCare's code, branding, and vector interface are original. No referenced application's code or binaries are bundled. It is not affiliated with IObit, Microsoft, or the referenced projects.

**[MIT licensed](LICENSE)** · Maintained by [@EhosanurRahmanRomi](https://github.com/EhosanurRahmanRomi)
