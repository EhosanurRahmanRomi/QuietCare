# QuietCare 0.6.0-preview.1

Rebuilds the main interface and introduces a proper Windows setup EXE with an embedded native MSI. The tiny transparent CPU → GPU → RAM monitor remains the primary everyday view.

- **Mini monitor** is available from every page. It restores the monitor directly; a second launch with `--monitor` also asks an existing copy to show it. Direct floating RAM Boost, transparency, position and configurable speed remain.
- New Overview with real CPU/GPU/RAM readings, central circular Start care control and live stage states. Dedicated Memory, Protection and Storage pages; organized two-column Toolbox; compact Startup table; redesigned About and Settings.
- Dashboard telemetry now continues while the main window is visible even if the floating view is hidden. It stops when neither is visible. Brief optional page fades respect reduced motion.
- Original Q logo throughout executable, tray, window, installer, Start menu and desktop shortcut.
- Setup installs into a permanent per-user location, registers Windows Installed apps and its native uninstaller, and offers sign-in startup, desktop shortcut and launch choices. It preserves local settings, Recovery, startup backups and unowned files on uninstall.
- Setup verifies its embedded MSI, invokes the fixed native engine, avoids arbitrary custom actions and does not close other apps or force a reboot. Updates remain manual through the official release page.

The care engines from the previous preview remain: guarded old-junk cleanup, measured resident-memory trimming, actual Microsoft Defender scans, Windows drive optimization, startup alerts and advanced file tools. The freezing Process Monitor remains removed. Smart Care uses local decision rules, not a remote AI model.

Validation includes 74 Core and 27 Boost checks, 21 production pages plus monitor rendering with zero binding errors, and isolated native installer checks. See [validation scope](VALIDATION.md) for current evidence and limits. CPU busy time can differ from Task Manager’s frequency-adjusted utility. RAM changes are workload-dependent; a 1 GiB gain is not promised. This is an unsigned preview, not a claim of bug-free or independently audited software.
