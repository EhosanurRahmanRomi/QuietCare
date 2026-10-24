# Validation of 0.6.0-preview.1

Validated locally on Windows 11 x64. Evidence levels are kept separate: fixtures, rendered dark/light layouts, live read-only checks and one explicitly requested live memory pass do not prove every native interaction or hardware configuration.

| Evidence | Result | Scope |
| --- | --- | --- |
| Release compile/publish | Successful with warnings treated as errors | SDK 10.0.402; self-contained compressed Windows x64 executable |
| Core regressions | **74 passed** | Guarded file cleanup/recovery, identity/age/exclusions, junctions/hard links, cancellation, receipt ownership, startup mappings/diffs, independent disk scans, Windows-tool signatures/resolution, automatic-care decisions, sequencing, changing conditions, failures and duplicate/cancelled requests |
| RAM fixtures/helper | **27 passed** | Conservative/expanded selection, media/foreground/project/security protection, CPU-idle constraints, new-app grace period, 24/32 request bounds, cooldown, refill/negative-delta reporting and allocated-data preservation in a dedicated 96 MiB helper |
| Production controls | **21 pages + monitor, zero binding errors** | Real WPF controls and the actual native monitor/result renderers; Smart Care progress and result displays use labeled fixtures. The header action was present on each page. Dark/light dashboards at 980 x 640, dark/light monitor settings and the no-animation override were also checked. No cleanup or Windows scan/optimization command was invoked by the UI harness. |
| Production assessment adapter | Valid live memory/CPU readings; power available; Defender read completed | Read-only adapter call using isolated application state; no care action started |
| Actual guarded Boost pass | **10 eligible processes trimmed, +294.1 MiB available RAM at three seconds** | Explicit live diagnostic invokes the production SmartBoost implementation with user exclusions and protection checks; no app termination, tab discard or file cleanup. This is separate from the fixture suite. |
| Live compact monitor/startup watch | Completed with main window unopened | Actual sampling and startup-baseline refresh; no Boost during resource sample |
| Dependency advisories | No known vulnerable packages returned | Fresh NuGet.org snapshot for App, Core and Monitor |
| Package integrity | Existing 41-archive signature report retained | No dependencies added/changed. Signatures establish provenance/integrity, not harmless behavior. |

The UI harness recorded 100 dispatcher heartbeats; the longest gap was 1.74 seconds during navigation/rendering. It also observed a five-second idle period. Short page fades respect Windows motion/high-contrast settings and the in-app appearance switch; there is no decorative monitor animation. This does not establish frame-perfect interaction. Smart Care stages in screenshots are demonstration outcomes, not a claim that Defender or drive optimization ran.

## Earlier live RAM result (0.5.0-preview.3)

A single guarded pass accepted 10 requests, with no declined requests. Available physical RAM rose by **308,338,688 bytes (294.1 MiB)** at the three-second follow-up; used RAM changed from **68.2% to 66.4%**. All 10 selected processes had a lower resident footprint at follow-up. Other running activity also affects the available-memory delta; this is an observed result, not a guaranteed isolated causal gain or long-term benchmark. It did **not** establish a 1 GiB gain.

The sum of per-process resident reductions was 891,125,760 bytes; shared pages can overlap, so this is not total RAM freed. No allocations were discarded, tabs closed or processes terminated. The sanitized summary is published; the private process-name report stays local. The explicit `--live-boost-check` diagnostic is opt-in and is never called by CI or the default fixture test path.

## Short local resource sample

The final 0.6 executable averaged **4.49% of total-machine CPU** over the first 20 seconds including initialization. Twelve-second warmed samples measured **0.24% at 1,000 ms** and **0.65% at 250 ms**, with 12 and 47 completed samples. The six-second hidden-monitor sample measured **0.00%**. Working sets were approximately **223â€“230 MiB** with startup monitoring enabled. These short samples depend on workload and are not sustained benchmarks.

## Native installer

**37 checks passed** across native fixture installation, installed-app registration, payload SHA-256 checks, both startup/desktop choices, native uninstall, a newer-version upgrade and downgrade rejection. Unowned sentinel files survived uninstall and upgrade. The test product, directories and startup registry were isolated. The build emits a native MSI and an EXE wrapper with the original icon; Python is only a build dependency. The corrected production EXE was also run on this PC: Windows Installer returned 0; installed payload SHA-256 matches the published executable; current-user startup points to the permanent installed path and is enabled; app icon, desktop and both Start menu shortcuts and native uninstaller registration were verified. The user interacted with the setup during this acceptance run. No reboot was forced or tested.

## Meter definitions

The meter reads Windows system busy time, approximate physical-memory load and the busiest GPU engine across adapters. A read-only Windows comparison showed 94â€“97% processor busy time versus 90â€“91% processor utility during a busy interval; that is a metric difference, not synchronized validation of every meter value. CPU/GPU integer rounding and sampling windows matter. Exact Task Manager parity, every GPU driver and greater-than-64-processor systems have not been established.

## Not yet established

- Full interactive acceptance of every button, keyboard path, drag/expand and UAC outcome.
- End-to-end Defender scanning/remediation or actual Windows drive optimization through this build. Resolution, signatures and command construction were checked; these operations were not launched during testing.
- Reboot acceptance and all real-user file-cleanup outcomes. Installer startup registration is verified; actual sign-in after restart is not. File cleanup and startup enable/disable regressions use isolated fixtures. An interactive care session was stopped before later operations during native review; it is not used as proof of cleanup, Defender scanning or optimization. The earlier explicitly requested live Boost pass is documented separately.
- Long-duration startup notifications, every persistence location, complete inherited-message localization, or broad hardware/OS/language coverage.
- Authenticode signing, independent penetration testing, bug-free behavior or a fixed RAM gain.

Machine-readable evidence is in [validation/](../validation/). Local startup inventories, commands and process-name reports are excluded. The mini monitor remains the primary view; installed startup and reboot acceptance are reported separately. [Exact automatic-care rules](SMART-CARE.md).
