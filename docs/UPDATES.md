# QuietCare updates

Open **Updates > Check for updates**, or use the notification icon's check action. The app shows the installed version, selected channel, result, plain-text release notes and GitHub/support links. A failed or canceled check is not reported as up to date. Manual checks have a one-minute per-session cooldown.

**Settings > App updates & support** provides a preview-channel choice and an optional startup check at most once a week. The startup check is off by default; it has no recurring timer. Attempts, including failures, count toward the weekly interval. There are no automatic downloads or silent installations.

## Retrieval and installation

Checks request only this project's HTTPS GitHub Releases API. If the API is unavailable/rate limited, a fixed `raw.githubusercontent.com/EhosanurRahmanRomi/QuietCare/main/updates/` feed is used. Normal GitHub connections expose ordinary request/network metadata and the QuietCare version in the User-Agent; no local files, hardware inventory, credentials or telemetry are sent.

A newer supported release enables **Download verified installer**. Downloads must match the exact repository/tag/`QuietCare-Setup.exe` URL, expected size (maximum 64 MiB) and SHA-256 digest. Metadata is bounded to 1 MiB; notes are plain text. Redirects are bounded to four and only allowed HTTPS GitHub asset hosts. Cancellation/deadlines remove the app's own partial file.

The local cache rejects linked paths/hard links and validates file identity. The installer retains an Internet zone mark. **Open installer** requires confirmation, rechecks the hash and mark, and holds a read lock through process creation. The regular installer then requires your installation choice. Windows warnings are not suppressed.

GitHub metadata and the fallback feed are trusted distribution inputs controlled by the repository owner. Their SHA-256 values establish byte agreement, not an independent publisher signature. Compromise of the repository/distribution account remains a risk. The preview is unsigned; independent signatures/key rotation and Authenticode remain future work. Do not describe this as an independently audited updater.

## Maintainer release procedure

1. Change the app/assembly/manifest/installer versions and document the release.
2. Run local core, UI, updater, Care and installer checks; package final binaries.
3. Generate the channel feed with `scripts/write-update-feed.ps1` from the **final** installer. It computes size and SHA-256 and accepts only a version tag; no arbitrary download URL is accepted. Use `-Preview` for preview releases.
4. Stage and inspect source, docs, sanitized evidence and the generated feed; publish the reviewed commit and matching tag. Keep stable feed empty until a stable release exists.
5. Upload final binaries/source/checksums/evidence to the matching release. Never regenerate binaries after feed/checksum generation without regenerating all dependent metadata.
6. Verify remote tag/commit, CI, release asset digests and anonymous downloaded bytes. Run `test-updates.ps1 -Live` to verify retrieval/download without executing the installer.

Publish the feed and assets close together; a feed pointing to an unpublished release will fail the download safely. Keep prior versions available for manual recovery. Do not reuse a tag or silently replace published release binaries.
