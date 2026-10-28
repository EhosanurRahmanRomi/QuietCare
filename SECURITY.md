# Security and privacy

This preview is unsigned. Do not interpret source review, package-signature verification or passing fixtures as a guarantee of security or absence of bugs.

## Trust boundaries

- The app normally runs as the signed-in user. Defender/drive operations requested directly or by a clicked Smart Care run invoke Windows UAC for a fixed Microsoft-signed tool and allowlisted arguments. Explicit Defender preference controls use an elevated one-shot copy of the installed app, a four-action allowlist, fixed native policy/WMI APIs and a separate confirmation. No general privileged command runner is exposed.
- Tool paths are resolved from Windows system/Defender locations and verified through Windows embedded/catalog signature trust. Trust checks use cached certificate information; they do not provide a malware verdict or fresh online revocation assurance.
- Cleanup refuses reparse/junction traversal and multiply-linked approved files. Scan approvals bind a specific result and target instance to file identities, sizes and write times and expire after ten minutes. File changes are rechecked using handles before quarantine.
- Automatic cleanup permanently deletes only its own approved old-junk receipts. Advanced cleanup keeps recoverable items until the user explicitly restores or permanently deletes them. Recovery is not a backup of all PC files.
- RAM Boost does not terminate apps or purge global memory lists. It remeasures resident working sets after three seconds and reports overall available-RAM changes separately. Working-set reduction is temporary and workload-dependent. Last result text, including process names and skip categories, is saved locally under `Monitor/last-boost.xml`.
- Smart Care uses local decision rules and fresh telemetry. It does not send data to an AI service, silently run in the background, uninstall apps or disable services/startup entries. Last step outcomes and successful optimization time are stored locally in `smart-care.json`. Stop prevents remaining steps while allowing an already-running Windows scanner/optimizer to finish.
- Startup monitoring observes selected persistence locations. It is not an endpoint-protection or intrusion-detection engine. It never executes a new entry or automatically disables it.
- Registry repair, destructive restore-point operations, generic recursive removal and blanket service-disable tweaks are excluded.

## Data and network

Local settings, startup baselines/alerts/backups, log paths and recovery manifests can contain private application names and commands. They are stored as local files under `%LOCALAPPDATA%\QuietCareNext`, not encrypted and not uploaded. Avoid including them in a public issue without redaction.

The optional update check sends a normal HTTPS request to the fixed GitHub releases API when clicked. It does not download or execute updates. Windows Defender and Windows signature services have their own Windows-managed behavior and policies.

## Supply chain and reporting

NuGet.org is the only configured package feed; locked content hashes and explicit trusted signer certificates are committed. Official Microsoft author and NuGet repository certificates are pinned with untrusted roots disallowed. Runtime packages are Microsoft-signed. Dependency advisories are checked at release time, not continuously. Certificate rotation requires an intentional maintenance update.

Upstream licenses and pinned commits are documented. No cloned reference project was executed, no remote bootstrap script was run, and GPL/Anti-996 reference implementations were not copied into the shipped application.

Report suspected vulnerabilities through [GitHub private reporting](https://github.com/EhosanurRahmanRomi/QuietCare/security/advisories/new). If unavailable, open an issue requesting a private contact without posting exploit details, personal commands or files. No response-time or long-term-support contract is promised.

## Installer

Setup contains a local MSI and verifies its embedded SHA-256 before using the Windows engine. It creates a private staging directory for the current user and SYSTEM and removes only its own temporary file and empty directory. Startup and shortcuts are explicit setup choices, enabled initially. The package has no arbitrary custom-action script, service or driver. Uninstall removes registered resources and preserves user state. The EXE/MSI are unsigned: the embedded hash detects payload corruption, not a maliciously replaced whole installer. Check the release SHA-256 and source.

## Explicit Defender preferences

Protection can now change one persistent local real-time policy or three scan CPU preferences when the user clicks and confirms. These changes are not automatic. Tamper Protection and managed-device restrictions are honored; no service/driver/permission bypass or global exclusions are used. Previous values are backed up in administrator-writable machine registry and retained after uninstall. Restore before uninstall to undo changes. See [Defender controls](docs/DEFENDER-CONTROLS.md) for exact scope and the distinction between real-time policy and a nonbinding scan CPU target. Native write/restore acceptance on the user’s actual policy remains unexecuted; tests use fixtures and a read-only provider/COM probe.

## Focus sessions

Focus changes are opt-in, timed and journaled before mutation. CPU priority changes apply only to eligible same-user/session processes; recovery verifies PID, creation time and executable. No process termination/suspension, High/Realtime priority, GPU driver changes or forced RAM limits are used. The optional elevated update helper accepts a fixed command/setting allowlist, validates its owning executable/user/session, serializes local-GPO editing, preserves original values, and attempts restore on owner exit or expiry. Power loss or simultaneous helper termination can require explicit recovery on next launch. Focus does not disable Defender. The optional undocumented QuietHours COM bridge fails visibly and does not use CloudStore patches or notification-service restarts. [Exact behavior](docs/FOCUS-MODE.md).
