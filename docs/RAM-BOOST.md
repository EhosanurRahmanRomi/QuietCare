# Smart RAM Boost

Click the floating lightning button. Boost runs directly with a 12-second result popup; **View Boost results** is optional. Results are also available from Speed Up and Processes & RAM.

## Measured outcomes

Results separate eligible requests, Windows-accepted trims, observed resident decreases, declined/unavailable requests and protected/skipped processes. They show fresh per-process before/after values where accessible, physical available RAM before/after and aggregated skip reasons. Missing measurements are unavailable. Accepted requests with no observed decrease are labeled accordingly. Working sets can share pages, so decreases are not added together as unique physical RAM freed.

## Protection and eligibility

- Foreground process IDs are protected. The non-activating widget preserves external focus/history.
- Active application families are protected, except qualifying idle browser renderers with the background-browser option enabled.
- Browser families playing audio and processes with active audio/calls are protected. Unknown audio state prevents changes.
- Windows processes, visible unminimized non-browser apps, common project/build tools, terminals, trading programs, Office apps, VMs and media tools are protected.
- Extra process names in Settings protect the entire family. Add `chrome.exe`, for example, for background web projects.
- Inaccessible or identity-changed processes, processes younger than five seconds, working sets under 48 MB and candidates using more than 20 ms CPU between snapshots are skipped.

Process snapshots and read-only audio sessions are sampled twice. Browsers must be renderer/content processes; controllers, GPU/utility and extensions are excluded. **Include OS-background browser renderers**, on by default, allows renderers inside an active browser only with idle/below-normal Windows priority in both snapshots plus a fresh check. Disable it to protect the whole active browser family. Foreground/media/identity protection is rechecked before the native trim path; independent app activity can still change concurrently.

The observation includes a 750 ms delay plus Windows calls. At most 12 accepted requests occur within a five-second trim-loop budget; native calls/observation add time. A 30-second cooldown limits repeated paging. No recurring automatic purge runs.

## What Windows does

[EmptyWorkingSet](https://learn.microsoft.com/windows/win32/api/psapi/nf-psapi-emptyworkingset) asks Windows to remove resident pages, not deallocate app data. Pages can return with page faults and latency. See [Microsoft's working-set model](https://learn.microsoft.com/windows/win32/memory/working-set).

No external optimizer can reliably identify every unused allocation or tab containing an unsaved project. OS priority and recent CPU use are signals, not tab-content knowledge or a universal safety guarantee. No overall RAM change can mean no eligible processes, declined requests, returning pages or other workload increasing. Results distinguish the observable cases.

QuietCare never requests app termination, suspension, restart, priority lowering, tab discard/reload, global standby purges, RAM-filling tricks or browser-setting changes. Lower resident memory does not guarantee a faster PC; committed allocations remain intact.

[Chrome Memory Saver](https://support.google.com/chrome/answer/12929150?hl=en-GB) is a browser-managed tab-level alternative that may later reload discarded tabs. QuietCare has no extension, debugging port or tab-content access.

## Evidence

Policy fixtures cover protection, optional background browsers, fresh priority changes, failure feedback, measured versus accepted requests, caps, cooldown and cancellation. The real WPF button is invoked with a scoped host. A live Windows test trims only a dedicated helper and verifies every byte of its 96 MB allocation. A separate current-PC eligibility audit is read-only. These do not establish all-browser compatibility or a speed gain. See [validation](VALIDATION.md) and [source research](RELATED-PROJECTS.md).
