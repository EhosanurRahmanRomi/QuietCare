# Validation and evidence

## Local v0.2.0 result

**42 core + 20 WPF UI + 4 isolated installer lifecycle checks passed** on the reference Windows PC. Both optimized x64 executables compiled with warnings treated as errors. This is scoped verification, not a guarantee of no bugs or complete security.

| Evidence | What it establishes |
| --- | --- |
| [Core results](evidence/v0.2.0/test-results.txt) | Path/handle boundaries, linked/locked/changed files, quarantine/recovery, XML rejection, startup fixtures, telemetry, Boost policy and dedicated-helper data preservation. |
| [WPF UI results](evidence/v0.2.0/ui-test-results.txt) | Real control layout, minimum width, transparent caption/widget properties, CPU/GPU/RAM order, direct Boost event wiring and numeric sorting/tooltips. |
| [Installer results](evidence/v0.2.0/installer-test-results.txt) | Isolated install integrity, registration/shortcuts, reinstall, unrelated-shortcut preservation and uninstall data retention. |
| [Single-instance result](evidence/v0.2.0/single-instance-results.txt) | Second launch exited and one matching process remained; the native main window was revealed. |
| [Benchmark JSON](evidence/v0.2.0/benchmark-report.json) | Short local process resource observations with explicit UI-construction and polling flags. |
| [Full validation report](VALIDATION-v0.2.0.txt) | Commands' results, binary hashes, methodology, native review and coverage limits. |
| [Showcase checkout verification](evidence/v0.2.0/source-checkout-verification.txt) | A fresh source copy rebuilt and passed the same 42 core, 20 UI and 4 isolated installer checks before publication. |

## Native UI and memory checks

The actual WPF Care window and transparent strip were inspected on the reference PC, and the transparent maximize/restore controls were clicked and verified. The saved screenshots show a 1000 × 670 main window and a 205 × 28 monitor. They are native captures rather than design mockups.

The direct Boost UI test invokes the floating WPF button with an injected scoped runtime. This proves the wiring and no-chooser behavior, not real user-app optimization. A separate live test filters all trimming to a dedicated memory helper, then checks every byte of its 96 MB allocation. It does not trim user applications. Read-only Core Audio enumeration also ran locally.

## CI scope

The repository workflow builds/packages on Windows and runs the layout/isolated-installer checks. Its status is separate from the dated local evidence above. A successful hosted build does not establish physical GPU/audio behavior or a clean installation on every supported Windows configuration.

## Remaining gaps

- Independent security audit and Authenticode signing.
- Broader Windows, GPU/driver, multiple-monitor, DPI, high-contrast and accessibility coverage.
- Long-running, battery and repeated-upgrade/uninstall testing.
- Compatibility observations across more browsers and project applications.
- A deterministic/reproducible binary build configuration.

The source and release notes keep these limits visible. Do not present compile, mocked-policy, layout, hosted CI, and native hardware evidence as interchangeable.
