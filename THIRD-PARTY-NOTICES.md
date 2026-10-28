# Third-party notices

QuietCare Next is an attributed derivative of [PowerClean](https://github.com/fgilde/PowerClean), copyright 2026 fgilde, under MIT. The complete upstream copyright and license are preserved in [LICENSE](LICENSE). Base commit: `82798038a8969465c8ef300c00f8d96a0dad4903`.

`src/Cleaner.Core/Services/StartupApprovalPolicy.cs` adapts BCUninstaller `NewStartupDisable.cs`, copyright 2017 Marcin Szeniak, from commit `30da609384c98ba6e35c6530129541ea4ff3970a`, under Apache License 2.0. Changes include pure state encoding, enum mapping, corrected 32-bit startup location and read-only state discovery. Its complete license is in [licenses/BCUninstaller-Apache-2.0.txt](licenses/BCUninstaller-Apache-2.0.txt).

QuietCare changes include the independent monitor, guarded cleanup and recovery, one-click care hub, signed Windows-tool invocation, startup notifications, dependency policy, tests and documentation. Existing upstream screenshots and icon-generation sources are reference assets, not evidence of QuietCare behavior.

Mem Reduct, BleachBit and WinDirStat are GPL-3.0 references. Their source was reviewed; no code or executable from them was copied into this MIT-derived build. TrafficMonitor uses Anti-996; no TrafficMonitor source was copied. WinUtil is MIT, reviewed as a policy reference; its scripts are not bundled or executed. See [source comparison](docs/SOURCE-REVIEW.md).

Runtime dependencies include Microsoft .NET/WPF, Microsoft.Extensions and System.* libraries, CommunityToolkit.Mvvm, and WPF-UI. Their licenses and runtime notices remain applicable. Package identities, versions, content hashes and validation scope are recorded in lock files and validation reports. A valid package signature establishes integrity/provenance, not harmless behavior.

`src/Cleaner.Core/Services/WindowsFocusRuntime.cs` adapts the QuietHours COM interface declaration from Rafael Rivera's MIT IDL, distributed in bitdisaster/windows-focus-assist at commit `042e154d20224517bac512462f776bdaa667d9e9`. Complete license: [licenses/QuietHours-MIT.txt](licenses/QuietHours-MIT.txt). Focus source review: [docs/FOCUS-SOURCE-REVIEW.md](docs/FOCUS-SOURCE-REVIEW.md). PowerToys, Process Governor and taskbar-focus were reviewed as described there; their binaries, scripts and implementation code are not bundled.
