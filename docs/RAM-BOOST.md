# Smart RAM Boost

## One click from the floating monitor

Click the lightning button. QuietCare checks background candidates, requests a working-set trim for qualifying processes, and shows a brief result popup beside the monitor. It does not open an application chooser. The advanced process page remains available for optional manual selection.

## Eligibility and protection

The current policy protects:

- The foreground application and observed application families; the non-activating monitor preserves focus.
- The entire active browser family, browser families playing audio, and processes with active audio/calls.
- Windows processes, visible unminimized applications, common editors/build tools, terminals, trading programs, Office apps, virtual machines and media tools.
- Additional process names entered in Settings.
- Inaccessible or identity-changed processes, newly started processes under five seconds, working sets under 48 MB, and candidates exceeding 20 ms CPU time between observations.

Media sessions are read twice without recording audio. If enumeration cannot be verified, no candidate apps are changed. Browser candidates must be renderer/content processes in an inactive browser. Controller, GPU, utility and extension processes do not qualify under the browser policy. The current policy is conservative, but it cannot know every application's purpose; add custom exclusions for other important tools.

The CPU observation includes a 750 ms delay plus the surrounding Windows calls. At most 12 successful trims are requested, with a five-second trim-loop budget and a 30-second cooldown. Observation and native calls can add time beyond that loop budget. There is no recurring automatic scan or purge.

## What Windows actually does

[`EmptyWorkingSet`](https://learn.microsoft.com/en-us/windows/win32/api/psapi/nf-psapi-emptyworkingset) asks Windows to remove resident pages from a process's working set. It does not deallocate its application data. Pages can return when accessed, potentially with page faults and latency. [Microsoft's working-set explanation](https://learn.microsoft.com/en-us/windows/win32/memory/working-set) provides the underlying memory model.

QuietCare does not terminate, suspend, restart, or lower the priority of applications; discard or reload tabs; clear the system standby list; or rewrite browser settings. Results show available physical RAM before and after instead of summing process working sets as if they were unique physical memory.

## Browser limits

Windows process APIs do not identify which individual tab contains a project, video or unfinished form. QuietCare therefore protects the whole active browser family. It does not promise to selectively reduce hidden YouTube tabs inside the browser you are using.

Browser-managed Memory Saver is the appropriate tab-level mechanism, but it can discard and later reload inactive tabs. See [Chrome's performance settings](https://support.google.com/chrome/answer/12929150?hl=en-GB). QuietCare requests no tab discard and uses no browser extension, remote-debugging port or tab-content inspection.

## Evidence and limits

Policy tests cover the exclusions, media uncertainty, browser family behavior, caps, cooldown and cancellation. A scoped UI host checks that the real floating button invokes Boost without opening the main window. A live test trims only a dedicated 96 MB helper process and verifies every byte afterward. No user app is trimmed by that test.

These checks do not establish compatibility with every application or guarantee a speed improvement. See [validation](VALIDATION.md).
