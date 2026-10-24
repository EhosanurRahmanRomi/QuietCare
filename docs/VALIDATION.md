# Validation of 0.5.0-preview.2

Validated locally on Windows 11 x64. These are separate evidence levels; rendered pages and fixtures do not prove every native interaction or hardware configuration.

| Evidence | Result | Practical scope |
| --- | --- | --- |
| Release compilation/publish | Successful; no compiler warnings in the recorded build | .NET 10.0.402 SDK; self-contained compressed Windows x64 executable |
| Core regressions | **54 passed** | File identity, exclusions/age, cancellation, changed files, junctions/hard links, recovery conflicts, receipt ownership, one-click deletion, startup-state mapping/diffs, independent disk scans, and Windows-tool trust/resolution |
| RAM Boost | **16 passed** | Selection, refilled-memory reporting, available-RAM positive/negative deltas and cancellation policies plus a dedicated 96 MB helper; all helper bytes remained intact after working-set trimming. User apps were not trimmed. |
| Production controls | **17 pages loaded + monitor render; zero binding errors** | Real controls rendered by the application's validation harness; dispatcher heartbeat and Boost progress/result display fixtures included. Monitor image readings are fixtures. No cleaning, scan or optimization commands were clicked. |
| Live monitor + startup watch | Completed, main window unopened | Actual CPU/GPU/RAM sampling, startup baseline and a 30-second refresh; no Boost invoked |
| Packages | See signature report | All downloaded dependency, SDK-tool and runtime archives verified; signatures do not establish absence of malicious behavior |
| Advisory audit | No known vulnerable packages returned for App, Core or Monitor | Snapshot from the configured NuGet advisory source at validation time |
| Locked restores | Normal and single-file publish configurations passed | Separate lock files prevent the two build variants from replacing one another's dependency state |

The UI harness recorded 65 dispatcher heartbeats, including a five-second idle period after navigation. Its longest gap was 1.31 seconds during navigation/rendering; this is not a claim of frame-perfect interaction. The new floating-result image uses fixture values.

The Process Monitor page and polling backend are absent. Four old process-control checks were removed with the feature; the remaining core cases were rerun. No user application was terminated.

## Short local resource sample

The current correction's first 20 seconds, including startup initialization, averaged **4.56% of total-machine CPU**. After warm-up, 12-second samples measured **0.37% at 1,000 ms** and **0.59% at 250 ms**; a six-second hidden-monitor sample measured **0%**. Working set was about **223–229 MiB** with startup monitoring enabled. This was a short workload-dependent sample on an eight-logical-processor PC, not a sustained benchmark or a guarantee. No Boost ran during this sample.

## Not yet established

- Full interactive acceptance of every button, keyboard path, monitor drag/expand and UAC outcome.
- End-to-end Defender scanning, remediation, or actual drive optimization through this release. Tests verified the installed tools, signatures and allowed command construction; they did not run these system operations.
- Real startup enable/disable or real-user file cleanup on this PC. Mutation tests used dedicated fixtures. Detection-policy tests do not prove every Windows persistence location is covered.
- Long-duration notification tests, five-minute refresh behavior over days, reboot/startup acceptance, or broad hardware/OS/language coverage.
- Complete English localization of all inherited advanced-tool status messages.
- Authenticode signing, independent penetration testing, or a guarantee of bug-free behavior.

## Evidence

Machine-readable reports are in [validation/](../validation/): `regression.json`, `boost-regression.json`, `ui-validation.json`, `monitor-smoke.json`, `package-signatures.json`, and the three advisory reports. Local personal startup inventories and process screenshots are deliberately excluded from publication.

The previously installed QuietCare 0.3 application was not replaced or uninstalled during this work. Preview settings and startup registration use `QuietCareNext`.
