# QuietCare 0.8.1-preview.1

Adds variable Focus duration: Minutes, Hours and Until I end it, with remembered selection and automatic restoration on Stop or app exit. The mini monitor, its refresh speeds and startup behavior are retained.

- Minutes/hours convert to exact timed deadlines; untimed sessions show Until you end it and no fabricated countdown. Windows active-hours limits remain explicit for long sessions.
- Reworks Focus, Overview, Memory, Protection and Storage around compact controls and semantic accents. Keeps the tiny monitor intact.
- Stops idle progress animations, reduces inactive Focus polling and batches process/window inventory to reduce repeated work. [Research and scope](UI-PERFORMANCE-REVIEW.md).
- Expands protected process rules to include Windows Security UI and trading/project hosts; background lowering leaves those tools alone.
- Corrects stale post-session status labels: the page now reports that the keep-awake request was released and notification ownership ended. The native worker check verifies these ended-state labels.
- **Focus mode** follows the active app, uses reversible Above normal / Below normal CPU priorities, requests Windows Do not disturb, keeps the PC awake and optionally applies a bounded update guard. Its worker restores owned changes on stop, timeout or app exit; an orphaned policy ledger has an explicit recovery action. Open work is preserved. [Exact behavior and limits](FOCUS-MODE.md).
- QuietCare defers its care jobs and startup notification balloons during Focus, then offers a deferred startup notice.
- Protection offers **Keep real-time scanning off** through supported local Windows policy and **Restore protection** from a saved policy backup. It requires Windows administrator approval and a separate confirmation, respects managed/Tamper Protection restrictions, and verifies effective policy plus actual state. No service removal, tamper bypass or repeating enforcement process is included.
- **Lower scan CPU** saves the prior settings, requests a 20% average scan target, low scheduled-scan priority and throttled idle scans; **Restore scan settings** restores the saved values. This is not a hard CPU cap and does not limit real-time scans.
- Smart Care respects an explicit local off policy and skips its automatic Defender scan. Manual scans remain available.
- Invalid actions, failed backups, missing telemetry, rejected requests and external setting conflicts have regression coverage. The native provider/COM capability check is read-only; privileged policy writes and persistence across reboot/updates are not claimed as validated on the user's PC.

The app remains an unsigned preview. Windows can block or override a local off request. See [exact controls](DEFENDER-CONTROLS.md) and [validation](VALIDATION.md). Installation alone changes no Defender preferences.
