# Validation and evidence

## Local 0.3.0 results - 23 October 2026

**45 core + 27 WPF UI + 17 updater + 9 Care (including one live read-only scan) + 4 isolated installer checks passed.** Optimized x64 app/installer compile with warnings treated as errors. These are scoped checks, not a no-bug, security or performance guarantee.

| Evidence | Scope |
| --- | --- |
| [Core](evidence/v0.3.0/test-results.txt) | File/identity/recovery boundaries, telemetry, startup fixtures, protection policy, accepted-versus-observed outcomes, cooldown/caps and isolated live memory helper. |
| [WPF UI](evidence/v0.3.0/ui-test-results.txt) | 15 production routes, minimum width, transparency, direct floating-button event, scan steps/results, manual update host, settings and table sorting. Scan/update/Boost hosts here are fixtures. |
| [Updater](evidence/v0.3.0/update-test-results.txt) | Fake HTTP with real hash/file/cache/Internet-zone guards, channel/version parsing, tamper/redirect/size/cancel failures and launch callback locking. No installer is executed. |
| [Care](evidence/v0.3.0/care-live-results.txt) | Eight fixture/orchestration checks and a real six-category read-only Windows scan. Temp scan reached its bound and correctly reported Partial. |
| [Installer](evidence/v0.3.0/installer-test-results.txt) | Isolated install/hash/registration/shortcuts, reinstall, unrelated-shortcut preservation, uninstall with data retention. |
| [Boost eligibility audit](evidence/v0.3.0/boost-audit-results.txt) | Read-only current-PC selection/rechecks with aggregate reasons, no saved process names/paths and no user-app trim. |
| [Benchmark](evidence/v0.3.0/benchmark-results.json) | Four short local runs; cold monitor-only construction and hidden polling flags included. |

## What the live memory test proves

The test filters native trimming to a dedicated helper PID, observes its working-set decrease, then checks every byte of its 96 MB allocation. User applications are not trimmed by the test. Accepted API requests and lower resident memory do not establish a responsiveness improvement or lasting overall RAM reduction. The current-PC selection audit observed two eligible candidates at that moment; eligibility changes with activity and exclusions.

## Visual and runtime scope

The current Care, live-scan, Boost-results and Updates images are renders of production WPF controls made by the UI harness. The live-scan/Boost examples use explicitly labeled synthetic fixtures; they are not physical-PC scan/optimization evidence. Native automation of the 0.3.0 window could not complete because the computer-use helper reported `foreground window did not report a process id`, including after recovery. Interactive click/scroll/DPI acceptance therefore remains pending. Older 0.2.0 native monitor/window captures and maximize/restore evidence are historical, not current proof.

Core tests exercise live Windows telemetry/audio and native memory trimming; Care live checks read Windows state. WPF control tests and rendered review are separate from these runtime checks. Offline updater tests do not prove live GitHub retrieval; post-publication live download verification is reported separately with release verification evidence. It never executes the downloaded installer.

## CI and remaining gaps

CI builds/packages and runs WPF, offline updater/Care and isolated installer checks. It does not run the local hardware/audio core suite. A passing hosted build is not physical-device proof.

Remaining: independent security audit, Authenticode/update signatures, wider browser/application compatibility, interactive native UI/scrolling, Windows/GPU/DPI/multi-monitor/high-contrast/accessibility coverage, prolonged resource/battery tests, wider upgrade testing and deterministic builds. The app is not a complete Advanced SystemCare suite or an antivirus.

Historical [0.2.0 evidence](VALIDATION-v0.2.0.txt) remains dated and separate.
