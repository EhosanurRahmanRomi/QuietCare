# Changelog

## [0.2.0] — 2026-10-23

First public preview release of QuietCare.

### Added

- Direct Smart RAM Boost from the floating monitor, with active-app/browser/media/project protection, extra exclusions, capped work, cooldown and result feedback.
- A compact sidebar and integrated transparent window controls with maximize/restore.
- Public build/architecture/security/contribution documentation, native screenshots, source tests and scoped validation evidence.

### Improved

- Tiny content-sized CPU → GPU → RAM strip, adjustable 100–10,000 ms refresh and transparency.
- Lazy main UI, compact-only telemetry, demand-only disk/network collection, reused GPU buffers and change-only numeric redraws.
- Hidden/minimized-only/suspended sampling behavior, numeric table sorting and full-value tooltips.
- Settings/XML/handle boundaries, recovery tamper/stale-selection checks and cancellable duplicate hashing.

### Fixed

- Mixed WinForms/WPF async shutdown continuation context.
- Repeated launches now reveal the existing instance.
- Invalid GPU values and non-finite persisted settings are rejected/normalized.
- Installer preserves unrelated shortcut targets and waits for parent termination during uninstall.

### Validation and limits

42 core, 20 WPF UI and 4 isolated installer checks passed locally. Native layout and maximize/restore were reviewed; the dedicated 96 MB helper retained its data after trimming. The release remains unsigned with limited hardware coverage and no independent security audit. See [validation](docs/VALIDATION.md).

[0.2.0]: https://github.com/EhosanurRahmanRomi/QuietCare/releases/tag/v0.2.0
