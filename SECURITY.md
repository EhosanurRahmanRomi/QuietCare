# Security and privacy

This preview is unsigned. Do not interpret source review, package-signature verification or passing fixtures as a guarantee of security or absence of bugs.

## Trust boundaries

- The app normally runs as the signed-in user. Only Defender/drive operations requested directly or by a clicked Smart Care run invoke Windows UAC for a fixed Microsoft-signed tool and allowlisted arguments. No general privileged command runner is exposed.
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
