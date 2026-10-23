# Feature coverage

QuietCare v0.3.0 is a focused Windows preview. This table describes actual behavior, including where the app hands off to Windows.

| Area | Implementation | Boundaries |
| --- | --- | --- |
| Live Care scan | Six real read-only categories, live current item, Skip/Stop and result review | Categories are observations, not all commercial-suite optimization engines. Partial/unavailable is explicit. |
| QuietCare updates | Manual check, channels, opt-in weekly startup check, verified installer and support links | GitHub/project feeds are trusted; SHA-256 is not a publisher signature; no silent installation. |
| CPU/GPU/RAM monitor | Native Windows counters and memory APIs; compact transparent window | GPU counters can be unavailable. No temperature/fan sensors. |
| Monitor controls | 100–10,000 ms polling, transparency, drag, topmost, expansion, startup | Source counters may update more slowly than the requested interval. |
| Smart RAM Boost | Direct capped working-set trimming, background-renderer option, measured results and skip reasons | No app killing, tab discard, system standby purge, allocation-freeing or speed guarantee. |
| Advanced RAM tools | Optional manual selected-process trim | Current-session non-Windows candidates; identity checks apply. |
| Startup | Search plus current-user Run disable/restore with literal command/type backups | HKLM/folder inventory is read-only; other types use Windows settings/consoles. |
| Cleanup | Preview, quarantine, restore, confirmed permanent removal | Only eligible old files in current-user LocalAppData/Temp. Quarantine alone does not reclaim storage. |
| Large/duplicate files | Read-only discovery and SHA-256 grouping | Bounded scan/hash work; no automatic duplicate removal. |
| Apps/system information | Desktop app registry inventory and local hardware/OS discovery | No Store-app inventory parity or vulnerability database claim. |
| Power plans | Select supported plans; retain original plan for restore | Available plans depend on Windows/device policy. |
| Security/privacy | Fixed Windows Security, Firewall and privacy entry points | No antivirus, spyware-removal or anti-tracking engine. |
| Updates | Windows Update, optional drivers, Store, Device Manager, uninstall entry points | No independent third-party driver/software updater. QuietCare updates are a separate implemented feature. |
| Storage/network/repair | 37 fixed Windows tool launchers | Named tools apply changes under Windows' permission model. |
| Recovery | Undo QuietCare's temp quarantine, user startup entries and power-plan choice | Not a full-system backup or recovery of unrelated apps' changes. |
| CSV export | Latest 900 monitor samples | Uncollected detail fields remain empty rather than fabricated zeroes. |

Not implemented: general registry cleaning/defragmentation, secure erasure, VPN, browser history/cookie/password cleanup, third-party driver updates, malware scanning, automatic app termination, scheduled QuietCare cleaning service, and hardware temperature/fan monitoring.

The [full HTML map](FEATURE-COVERAGE.html) contains the broader Advanced SystemCare feature comparison. The [user guide](USER-GUIDE.txt) documents scan limits, startup semantics, data locations and uninstall behavior.
