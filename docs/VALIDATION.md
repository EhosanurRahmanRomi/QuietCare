# Validation of 0.5.0-preview.3

Validated locally on Windows 11 x64. Evidence levels are kept separate: fixtures, rendered dark/light layouts, live read-only checks and one explicitly requested live memory pass do not prove every native interaction or hardware configuration.

| Evidence | Result | Scope |
| --- | --- | --- |
| Release compile/publish | Successful with warnings treated as errors | SDK 10.0.402; self-contained compressed Windows x64 executable |
| Core regressions | **74 passed** | Guarded file cleanup/recovery, identity/age/exclusions, junctions/hard links, cancellation, receipt ownership, startup mappings/diffs, independent disk scans, Windows-tool signatures/resolution, automatic-care decisions, sequencing, changing conditions, failures and duplicate/cancelled requests |
| RAM fixtures/helper | **27 passed** | Conservative/expanded selection, media/foreground/project/security protection, CPU-idle constraints, new-app grace period, 24/32 request bounds, cooldown, refill/negative-delta reporting and allocated-data preservation in a dedicated 96 MiB helper |
| Production controls | **17 pages + monitor, zero binding errors** | Real WPF controls and the actual native monitor/result renderers; Smart Care progress and result displays use labeled fixtures. The header action was present on each page. Dark/light dashboards at 980 x 640, dark/light monitor settings and the no-animation override were also checked. No cleanup or Windows scan/optimization command was invoked by the UI harness. |
| Production assessment adapter | Valid live memory/CPU readings; power available; Defender read completed | Read-only adapter call using isolated application state; no care action started |
| Actual guarded Boost pass | **10 eligible processes trimmed, +294.1 MiB available RAM at three seconds** | Explicit live diagnostic invokes the production SmartBoost implementation with user exclusions and protection checks; no app termination, tab discard or file cleanup. This is separate from the fixture suite. |
| Live compact monitor/startup watch | Completed with main window unopened | Actual sampling and startup-baseline refresh; no Boost during resource sample |
| Dependency advisories | No known vulnerable packages returned | Fresh NuGet.org snapshot for App, Core and Monitor |
| Package integrity | Existing 41-archive signature report retained | No dependencies added/changed. Signatures establish provenance/integrity, not harmless behavior. |

The UI harness recorded 94 dispatcher heartbeats; the longest gap was 1.86 seconds during navigation/rendering. It also observed a five-second idle period. Short page fades respect Windows motion/high-contrast settings and the in-app appearance switch; there is no decorative monitor animation. This does not establish frame-perfect interaction. Smart Care stages in screenshots are demonstration outcomes, not a claim that Defender or drive optimization ran.

## Live RAM result

A single guarded pass accepted 10 requests, with no declined requests. Available physical RAM rose by **308,338,688 bytes (294.1 MiB)** at the three-second follow-up; used RAM changed from **68.2% to 66.4%**. All 10 selected processes had a lower resident footprint at follow-up. Other running activity also affects the available-memory delta; this is an observed result, not a guaranteed isolated causal gain or long-term benchmark. It did **not** establish a 1 GiB gain.

The sum of per-process resident reductions was 891,125,760 bytes; shared pages can overlap, so this is not total RAM freed. No allocations were discarded, tabs closed or processes terminated. The sanitized summary is published; the private process-name report stays local. The explicit `--live-boost-check` diagnostic is opt-in and is never called by CI or the default fixture test path.

## Short local resource sample

The first 20 seconds including startup initialization averaged **4.61% of total-machine CPU**. Twelve-second warmed samples measured **0.46% at 1,000 ms** and **0.91% at 250 ms**. The six-second hidden-monitor sample measured **0.00%**. Working sets were approximately **223–230 MiB** with startup monitoring enabled. Short samples depend on workload and are not sustained benchmarks.

## Not yet established

- Full interactive acceptance of every button, keyboard path, drag/expand and UAC outcome.
- End-to-end Defender scanning/remediation or actual Windows drive optimization through this build. Resolution, signatures and command construction were checked; these operations were not launched during testing.
- Real-user file cleanup, startup enable/disable or reboot acceptance on this PC. File mutations used isolated fixtures. The separate live Boost pass changed only eligible resident working sets.
- Long-duration startup notifications, every persistence location, complete inherited-message localization, or broad hardware/OS/language coverage.
- Authenticode signing, independent penetration testing, bug-free behavior or a fixed RAM gain.

Machine-readable evidence is in [validation/](../validation/). Local startup inventories, commands and process-name reports are excluded. The running user copy and older installed version were not replaced during this work. [Exact automatic-care rules](SMART-CARE.md).
