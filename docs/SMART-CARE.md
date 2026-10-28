# Smart Care automatic mode

Click **Run Smart Care** on Home or **Smart Care** in the window header from any page. The same recommended PC-care workflow is available across the app. Clicking the header while it runs shows its live steps instead of starting a second job.

This is an explainable local rules engine. It does not send PC data to an AI service, require an account or train a model. It runs when clicked, not continuously. The monitor and startup watch continue their usual sampling separately.

| Stage | Automatic decision and behavior |
| --- | --- |
| Check this PC | Read actual RAM, CPU, power and Defender status. Disabled or unavailable protection is marked for review. |
| Clean old junk | Scan, revalidate and permanently delete only eligible `.tmp` / `.temp` files and closed-browser disposable caches at least seven days old. Documents, recovery data, projects, active browsers and session/offline storage are excluded. |
| Reclaim idle memory | Run the expanded guarded trim when RAM use is at least 65% or available RAM is below 2 GiB. Missing/invalid readings prevent automatic trimming. A recent Boost cooldown is respected. |
| Review startup | Refresh accessible registry, folder, scheduled-task and automatic-service entries; record new/changed entries and keep the startup watch active. The first scan establishes a quiet baseline. Recommendations and individual toggles remain in Startup apps. |
| Check for threats | If an effective local off policy is present, skip the automatic scan to respect that choice. Otherwise, if Defender is available and enabled as antivirus, request a quick scan unless Windows reports a completed quick scan within 24 hours. Missing/future timestamps count as unknown. Other antivirus providers are reviewed through Windows Security. |
| Optimize the drive | Request the signed Windows optimizer with `/O` when QuietCare has no successful optimization recorded within seven days, AC power is available and CPU load is below 60%. Missing power/load readings defer the action. Windows selects the drive-appropriate operation. |

Memory, security and drive decisions use refreshed readings immediately before the action. Actions run sequentially. Windows antivirus and optimization can request UAC approval. Rejecting UAC or another action failure remains visible; independent checks can continue. A scanner exit of zero is not proof of a malware-free PC: Defender defines it as no detection or successful remediation. Review Protection history for exact findings.

**Stop** cancels cancellable work and prevents the remaining actions. A Windows scan or optimization already started is allowed to finish safely and its actual outcome is retained. Exiting the app is separate from Stop. The last report and successful drive-operation time are saved locally in `smart-care.json` under the application data directory.

## Behavior in other tools

The header action provides the automatic maintenance plan from every page. Advanced cleaner categories, duplicate files, large files, developer caches, orphan data and installed programs retain preview/review before removal. Smart Care does not guess which personal files, applications or services you need. It does not silently uninstall apps, disable startup entries/services or repair registry entries.

## Expanded RAM Boost

Both the floating lightning button and Home use the expanded idle profile with no selection dialog. It observes two seconds of activity, includes eligible inactive visible apps, requires at most 10 ms of process CPU growth across observations and skips apps started less than a minute ago. The minimum candidate working set is 32 MiB. Work is bounded at 24 accepted requests, or 32 when available RAM is below 20% of total, and an eight-second trim-work window.

Foreground app families, configured exclusions, project tools, antivirus/system processes and media activity remain protected. A foreground browser family's renderers qualify only if marked idle/below-normal by Windows and that priority is rechecked. Controllers, GPU processes and extensions are excluded. Unknown media status blocks trimming. Identity, foreground, CPU activity and media are checked again before a request; process handles enforce identity and session checks.

Results distinguish available RAM from per-process resident reductions. A three-second follow-up includes pages that refill. Accepted requests with no overall available-RAM gain are reported that way; there is no guaranteed reduction. Trimming does not release allocations or close tabs. Chrome's own Memory Saver can deactivate inactive tabs, which reload on return; QuietCare does not force this behavior or alter browser policies.

References: [Microsoft EmptyWorkingSet](https://learn.microsoft.com/en-us/windows/win32/api/psapi/nf-psapi-emptyworkingset), [Windows working sets](https://learn.microsoft.com/en-us/windows/win32/memory/working-set), [Chrome performance controls](https://support.google.com/chrome/answer/12929150?hl=en). Thresholds above are QuietCare's own conservative policy choices, not Microsoft or Google recommendations.
