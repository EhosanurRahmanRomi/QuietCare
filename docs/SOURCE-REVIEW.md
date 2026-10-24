# Seven-source review

These repositories were actually cloned locally. The six reference clones used shallow, partial Git fetches without checkout or execution. Relevant implementations and license files were inspected; this is not a claim that every line in every repository received a security audit. PowerClean supplied the working base, BCUninstaller supplied one attributed adaptation, and the other five supplied design/policy references.

| Source | Pinned commit | License | What QuietCare uses |
| --- | --- | --- | --- |
| [PowerClean](https://github.com/fgilde/PowerClean) | `82798038a8969465c8ef300c00f8d96a0dad4903` | MIT | Attributed application foundation; unsafe inherited actions removed or restricted. |
| [BCUninstaller](https://github.com/BCUninstaller/Bulk-Crap-Uninstaller) | `30da609384c98ba6e35c6530129541ea4ff3970a` | Apache-2.0 | Startup approval mapping/state encoding adapted with copyright and license; backups added independently. |
| [TrafficMonitor](https://github.com/zhongyang219/TrafficMonitor) | `930f17533d6098989ebad62f210aa97d75ef174b` | Anti-996 | Compact-monitor presentation reference. Own native layered renderer; no code copied. |
| [Mem Reduct](https://github.com/henrypp/memreduct) | `04402de8b6a8ff9d1c5517c675d87c346655323c` | GPL-3.0 | Memory-purge behavior reviewed. Global memory-list purges rejected; guarded per-process trimming retained. |
| [BleachBit](https://github.com/bleachbit/bleachbit) | `e071627a34973ba996c0c3b54a12c6dff98bab17` | GPL-3.0 | Cleanup actions/browser definitions reviewed. Own closed-browser disposable-cache plan; cookie/session/offline cleanup excluded. |
| [WinDirStat](https://github.com/windirstat/windirstat) | `243e0609cc7b7cfca5a895e63907fae8724d8ce6` | GPL-3.0 | Disk-tree work reviewed. Own iterative scan with session-local counters and bounded memory; no code copied. |
| [WinUtil](https://github.com/ChrisTitusTech/winutil) | `2f0bdc8fca52e1ebe31a8d98216428061d3f8956` | MIT | Maintenance/startup practices reviewed. No remote bootstrap scripts or blanket service disabling adopted. |

## Review-driven corrections

- Corrected startup `Run32` approval mapping and saved backups before toggles.
- Added a seven-day floor to automatic cleanup independent of advanced preferences. Closed-browser checks are repeated during the operation.
- Bound each permanent cleanup to the recovery receipts returned by that operation; unrelated recoverable files cannot be swept up by that cleanup.
- Used Windows catalog and embedded-signature verification for Microsoft tools. Unsigned fixture executables are rejected.
- Removed nested parallel disk traversal and shared scan counters; concurrent scans keep independent results.
- Removed inherited updater/browser dependencies, global kill shortcuts, destructive restore-point cleanup, broad recursive deletion and unsupported registry repair.

## Windows services used directly

Microsoft Defender is the installed scan engine. Windows Optimize Drives owns media-specific optimization. Windows Services and Installed Apps provide privileged service/uninstall workflows. QuietCare does not download external engines or execute a repository's install script.

Official references: [Defender command line](https://learn.microsoft.com/en-us/defender-endpoint/command-line-arguments-microsoft-defender-antivirus), [Windows defrag command](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/defrag), [catalog signature verification](https://learn.microsoft.com/en-us/windows/win32/api/wintrust/ns-wintrust-wintrust_catalog_info), [NuGet trust policy](https://learn.microsoft.com/en-us/nuget/reference/nuget-config-file), [Microsoft 2026 signing certificate](https://devblogs.microsoft.com/dotnet/microsoft-author-signing-certificate-update-2026/).

## Automatic care and browser-memory review

Smart Care adds original local decision rules and a sequential coordinator; no additional third-party engine or script is bundled. Microsoft working-set behavior and Chrome Memory Saver were checked against their official documentation. Chrome deactivates inactive tabs and reloads them when revisited; QuietCare preserves allocations/tabs and reports its narrower working-set effect honestly. See [the exact rules and references](SMART-CARE.md).

## Interface references

The revised care dashboard uses original XAML with a compact primary action, separate category surfaces, clear action/result states and optional 180 ms page fades. Official [Microsoft PC Manager](https://pcmanager.microsoft.com/en-us) and [PowerToys settings](https://learn.microsoft.com/en-us/windows/powertoys/general) pages/screenshots supplied layout references; [Fluent motion guidance](https://fluent2.microsoft.design/motion) informed short page fades, reduced-motion support and avoiding decorative background movement. Their artwork and interface code were not copied. Browser control was unavailable in this session; official web references and the application's own render harness were used, not interactive testing of those external applications. No UI-rating comparison is claimed.
