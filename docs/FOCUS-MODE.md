# Focus mode

Open **Focus mode** in the sidebar or app header, choose a session length, and click **Start Focus**. The default is 60 minutes; the maximum is four hours. Preferences are remembered. The mini monitor stays available. End the session from this page at any time.

| Option | Exact behavior |
| --- | --- |
| Prioritize my active app | Follows the foreground app and requests **Above normal** CPU scheduling priority for accessible same-user processes using the same executable. A new foreground app replaces the previous one. It does not use High/Realtime priority, CPU affinity or a hard resource reservation. |
| Lower background app priority | Requests **Below normal** priority for eligible other open app families. Windows, security processes, project/build tools, trading hosts, terminals, VMs and known call/recording apps are protected from background lowering. It does not terminate, suspend, freeze or cap app memory. |
| Do not disturb | If the Windows QuietHours interface is available and the user's profile is unrestricted, requests the priority-only quiet profile, checks readback and saves the previous profile. Existing quiet profiles are preserved. Priority/critical notifications and app-internal popups can still appear. An unavailable bridge is reported, with a link to Windows notification settings. |
| Keep PC awake | Holds Windows display/system power-request handles only during the session. Closing the worker releases them, including after a crash. No power-plan settings are changed. |
| Temporary update guard | With UAC approval on an unmanaged Pro, Enterprise or Education PC, temporarily requests `NoAutoUpdate=1` and active hours covering the session. It saves all original local policy values before editing. Updates already running, manual updates and enforced deadlines can still proceed; the feature cannot promise zero update CPU or prevent every restart. Defender settings are not part of this guard. |

While Focus is active, QuietCare defers its own Smart Care/maintenance actions and startup-change notification balloons. The lightweight startup watch still records changes; expensive scheduled-task/service inventory waits until Focus ends, and a deferred notice is offered after the session. The monitor and explicitly requested floating RAM Boost remain available.

## Restoration and ownership

A small worker follows the parent app, expiry and an explicit stop request. It restores priorities and notifications on stop, timeout or parent exit. Each process change is saved before mutation and bound to its PID, creation time and executable. Recovery ignores reused PIDs and preserves settings changed by another tool. Individual inaccessible/exited processes are skipped.

The optional elevated update helper runs only for the session, watches the worker and restores its own local-GPO backup when the worker exits or its deadline expires. It accepts only fixed commands and policy keys, validates the same executable/user/session owner, and installs no service, driver or scheduled task. Local-GPO edits are serialized. The previous value may be absent or an explicit DWORD; that distinction is preserved.

If Windows terminates both helpers during sign-out, a power loss or a crash, restoration might not finish. On next launch, user-level recovery is attempted; **Restore changes** requests UAC for an orphaned update-policy ledger. Policies changed elsewhere are preserved and QuietCare releases ownership of them. Failed restores retain recovery data and remain visible. End Focus and confirm recovery before uninstalling. Settings are kept across uninstall so recovery is not silently discarded.

User state: `%LOCALAPPDATA%\QuietCareNext\Focus`. Administrator-only update recovery: `HKLM\Software\QuietCare\FocusUpdateGuard`. Recovery data is not a system restore point. A policy value can be saved without Windows immediately stopping update activity; the UI therefore reports a policy request, rather than claiming that all updates have stopped.

## Limits

Windows, drivers, antivirus, audio, networking, other users and app-internal work can continue to consume CPU/GPU/RAM. Focus reduces contention and interruptions; it does not reserve the whole PC or force additional free RAM. GPU scheduling and RAM allocation are unchanged. The undocumented notification interface may change in a Windows update; QuietCare does not patch CloudStore or restart notification services to work around that.

[Source decisions](FOCUS-SOURCE-REVIEW.md) · [Validation](VALIDATION.md) · [Defender controls](DEFENDER-CONTROLS.md)
