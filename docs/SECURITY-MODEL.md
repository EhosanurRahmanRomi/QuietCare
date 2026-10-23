# Security and data model

QuietCare reduces its attack surface through a local single-process design. This document describes implemented boundaries, not an independent assurance or security guarantee.

## Network and execution

The application implements no listening socket, remote-control endpoint, account service, analytics upload, plugin loader, downloaded script runner or automatic updater. Windows tool launchers use a fixed allowlist and trusted Windows paths. App inventory does not execute registry-discovered uninstall strings. The application and installer request normal user privileges.

The local named mutex/reveal event coordinates instances; it is not a network listener. Source/release retrieval and CI are GitHub project infrastructure, separate from application runtime behavior.

## Files and state

Settings, activity logs, quarantine and startup/power backups are stored in the current user's LocalAppData/QuietCare directory. XML parsing prohibits DTD/external entities. State and cleanup mutations use parent-path locks and validated file handles, reject links/hard links and identity changes, and avoid restore destination overwrites. Recovery rechecks metadata and stored content before restore/purge.

The cleaner's eligible scope is the current user's Temp directory, allowed extensions older than seven days and no larger than 512 MB. Preview and quarantine precede user-confirmed permanent deletion. Quarantine is recovery, not storage reclamation; permanent deletion is irreversible. File finders are read-only.

Local data is not encrypted by QuietCare. Windows user permissions and disk encryption remain relevant. No claim is made to resist an already compromised same-user process or administrator. Local activity entries should be reviewed before sharing a bug report.

## Process and audio access

Smart Boost uses documented working-set trimming with process identity checks and an eligibility policy. It never requests app termination, suspension or browser tab discard. Core Audio is read for active process/session state; audio is not recorded. Unknown media activity prevents candidate changes. Working-set trimming can still add later page-fault latency.

## Installer

The installer embeds and hashes its app payload, installs in the current user's Programs directory, and creates its own shortcuts/uninstall registration. Uninstall verifies product paths/targets and preserves unrelated shortcut targets and settings/recovery/backups. Its temporary helper is retained after exit. It does not silently undo every previous user-approved startup or power-plan change; restore those before uninstall if desired.

The executables are currently unsigned. SHA-256 checks verify file integrity against the published manifest; they do not prove publisher identity or replace signing/auditing.

## Reporting and unsupported assumptions

See [SECURITY.md](../SECURITY.md) for private reporting. There has been no independent security audit, exhaustive fuzzing, all-app compatibility verification or universal bug-free guarantee. The app is not an antivirus, malware remover, general registry cleaner or security-feature replacement.
