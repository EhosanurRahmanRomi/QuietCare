# Validation of 0.8.1-preview.1

Validated locally on Windows 11 x64 on 2026-10-28. Fixture coverage, production-control renders, native Windows calls and installed-app acceptance are distinguished below. Passing these checks does not establish bug-free behavior or every hardware configuration.

| Evidence | Result | Scope |
| --- | --- | --- |
| Release compile/publish | Successful; warnings treated as errors | .NET SDK 10.0.402; self-contained compressed Windows x64 executable; no new NuGet dependencies |
| Core regressions | **140 passed** | Guarded cleanup/recovery, startup mappings/diffs, Windows-tool trust, care decisions, explicit Defender settings, Focus journals, PID reuse, external changes, unsupported/managed devices, failure/rollback and a dedicated native process-priority child |
| RAM fixtures/helper | **27 passed** | Protected work/media/security, selection bounds, cooldown, refill/negative-delta reporting, and allocated-data preservation in a dedicated 96 MiB helper |
| Production controls | **22 pages + monitor; zero binding errors** | Real WPF controls, compact light layout including Focus, dark/light dashboard and monitor settings, reduced-motion override. Care/Boost results and the active Focus session use labeled display fixtures. No destructive actions invoked by the UI harness. |
| Untimed native worker | **4 passed** | Actual no-deadline session start, Stop, notification restoration and released power/notification state; no user-app priorities or update/Defender policies changed |
| Native Focus priorities | Passed | Dedicated child at Above normal, read back from Windows, then exact original priority restored; no user app used |
| Native notifications and awake | **3 passed** | Actual Do not disturb request/readback, display/system power request, and previous notification profile restored. A failed early restore check led to bounded setter retries; the corrected round trip passed. |
| Corrected worker end-state | **3 passed** | Production session start/stop, notification restore and released power/notification labels rechecked in 0.8.1; no user-app priority or update policy change in this patch check |
| Native production worker and update guard | **4 passed** | Feature acceptance in 0.7.0 (unchanged guard implementation): actual session, UAC-elevated temporary local-GPO writes, Stop, worker exit and exact original policy restoration. No Defender settings or user-app priorities changed in this check. |
| Defender provider and local-GPO probe | Passed, read-only | Actual WMI status, required Set-parameter types and COM policy access. Persistent protection-off and scan-CPU preference writes were not executed on the user PC. |
| Dependency advisories | No known vulnerable dependencies returned | Fresh NuGet.org advisory query; signatures/provenance are not a guarantee of harmless behavior |

The duration update additionally covers minute/hour conversion, saved unit/amount, untimed deadlines, expired timers and untimed policy restoration in fixtures. [Native untimed worker acceptance](../validation/focus-untimed-check.json) passed separately.

The UI harness recorded a successful no-animation override and kept its dispatcher active while rendering/navigation completed. Exact heartbeat/timing measurements are in its receipt. This is not frame-perfect interactive acceptance.

## Native installer

**55 local checks passed** with the earlier 0.7 full release payload in isolated native MSI fixtures; the unchanged packaging workflow reruns this suite against each new release payload in GitHub Windows CI: installation, SHA-256 payload checks, both startup/desktop choices, registration, uninstall preservation, newer-version upgrade and downgrade rejection. Exact results are in [installer-validation.json](../validation/installer-validation.json). Test startup entries/directories were isolated and unowned sentinel files survived. The real production package is distinct from these fixture products.

The source-tree installed acceptance report in `validation/installed-validation.json` identifies version 0.6.0 explicitly and is historical. Final local installation receipts identify their actual version separately. Reboot/sign-in acceptance has not been performed. Startup registration checks alone do not prove a reboot test.

## Short local resource samples

The 0.8 production-window harness includes real telemetry, the inactive Focus status watcher and an isolated startup watch. After initialization, eight-second samples measured:

| State | Refresh | Total-machine CPU | Private bytes | Working set |
| --- | --- | --- | --- | --- |
| Focus page visible, no active session | 500 ms | 0.63% | 164.5 MiB | 323.0 MiB |
| Full window hidden, mini monitor visible | 500 ms | 0.49% | 159.4 MiB | 317.4 MiB |
| Full window hidden, mini monitor visible | 1,000 ms | 0.05% | 157.3 MiB | 316.7 MiB |
| Overview visible, no care job | 500 ms | 0.73% | 163.1 MiB | 322.4 MiB |

A pre-change eight-second installed-app sample measured 1.66% with the main window open at 500 ms. Workload and initialization differ, so this is an observation rather than a controlled causal benchmark or a guaranteed percentage reduction. The first new Focus-page initialization sample was materially higher (7.93% over eight seconds); it was not represented as steady idle performance. Startup/first-page CPU spikes are not eliminated. The settled test waits for page initialization before measurement. [Resource receipt](../validation/resource-check.json) and [review](UI-PERFORMANCE-REVIEW.md).

The earlier 0.7 monitor-only harness kept the main window unopened and observed about 224-228 MiB working set. That is a different lifecycle from opening and then hiding the full UI. Working-set memory includes shared pages; the private-byte figures above are provided separately. A separate bounded worker check is recorded in its own receipt; UI-only measurements do not represent a continuously active Focus session plus every helper.

## RAM and meter limits

The last explicitly requested user-app Boost pass, from the earlier 0.5 build, observed **+294.1 MiB** available RAM after three seconds across ten eligible processes. It is historical, not a new 0.7 measurement or a 1 GiB guarantee. Current tests use isolated fixtures and a dedicated allocation helper. Working-set reductions can refill and shared pages overlap; they are not freed application allocations.

The meter reads Windows processor busy time, approximate physical-memory load and the busiest GPU engine across adapters. Sampling windows, rounding and driver support can differ from Task Manager. No exact Task Manager parity or universal GPU/greater-than-64-CPU coverage is claimed.

## Not established

- Every interactive button/keyboard/UAC outcome, broad hardware/language coverage or long-duration testing.
- End-to-end Defender scan/remediation, real drive optimization, or persistent protection-off/scan-CPU writes on this PC. Signature resolution, allowlists, fixture policies and the live read-only provider were tested.
- Restart behavior through every enforced deadline, updates already in progress, notification popups from every app, simultaneous helper termination/power loss, or full CPU/GPU/RAM reservation. Focus documents those limits and retains failed recovery data.
- Authenticode signing, independent security audit, bug-free behavior or fixed performance/RAM gains.

[Focus mode](FOCUS-MODE.md) · [Source review](FOCUS-SOURCE-REVIEW.md) · [Defender controls](DEFENDER-CONTROLS.md) · [Machine-readable evidence](../validation/)
