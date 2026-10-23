# Research and design decisions

Reviewed 23 October 2026. This is a record of design/API references, not a claim of code reuse, affiliation or independent ranking. The supplied recording informed the circular scan, navy color scheme and tiny transparent monitor; the personal recording itself is not published.

| Reference | Application to QuietCare |
| --- | --- |
| [TrafficMonitor](https://github.com/zhongyang219/TrafficMonitor) | A small, customizable monitor should remain useful without opening the full app. |
| [TaskbarStats](https://github.com/ericbruggema/TaskbarStats) | Glanceable readings and adjustable update cadence. |
| [DevToys](https://github.com/DevToys-app/DevToys) | Focused tool categories, compact navigation and clear action names. |
| [Microsoft PC Manager](https://pcmanager.microsoft.com/en-us) | Visible core actions and a focused maintenance hierarchy. |
| [Microsoft title-bar guidance](https://learn.microsoft.com/en-us/windows/apps/develop/title-bar) | Extend the content canvas while preserving usable controls, drag regions and resizing. QuietCare uses WPF WindowChrome and transparent overlaid controls. |
| [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) | Hardware temperature/fan monitoring is a separate platform/driver capability; no values are fabricated here. |
| [Mem Reduct](https://github.com/henrypp/memreduct) and [WinMemoryCleaner](https://github.com/IgorMundstein/WinMemoryCleaner) | Related memory utilities. Their cache/purge options do not establish that aggressive clearing improves ongoing work. |
| [Advanced SystemCare manual](https://www.iobit.com/product-manuals/asc-help/) | Feature inventory used to distinguish native tools, Windows integrations and unsupported capabilities. |

## Windows and browser behavior

The implementation uses Microsoft's documented [EmptyWorkingSet](https://learn.microsoft.com/en-us/windows/win32/api/psapi/nf-psapi-emptyworkingset) behavior and [working-set model](https://learn.microsoft.com/en-us/windows/win32/memory/working-set). Smaller resident memory is not the same as freed application allocation or improved responsiveness. The [GPU engine aggregation explanation](https://devblogs.microsoft.com/directx/gpus-in-the-task-manager/) informs the usage counter.

[Chrome's performance guidance](https://support.google.com/chrome/answer/12929150?hl=en-GB) describes browser-managed Memory Saver and protection situations such as active media. QuietCare does not request tab discards/reloads or inspect tab contents. It protects active browser families and uses read-only [Core Audio session process state](https://learn.microsoft.com/en-us/windows/win32/api/audiopolicy/nn-audiopolicy-iaudiosessioncontrol2) for media exclusion.

## Review ratings have limits

The [Glary Utilities Pro Capterra review page](https://www.capterra.com/p/228413/Glary-Utilities-Pro/reviews/) showed 4.6/5 for ease of use from a page with 16 total reviews when reviewed. That small, product-specific sample is not a universal desktop UI ranking. The [TechRadar review](https://www.techradar.com/reviews/glary-utilities) is from 2020, and the [PC Manager review](https://www.thewindowsclub.com/microsoft-pc-manager-for-windows) provides qualitative observations. No objective “best UI” score is claimed for QuietCare.

No third-party app source or binaries are bundled. QuietCare's code, logo, and vector-drawn interface are original. A more detailed [HTML research record](UI-RESEARCH.html) is included for offline reading.
