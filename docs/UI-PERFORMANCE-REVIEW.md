# UI and resource review

Reviewed 2026-10-28 after a user-reported UI/CPU problem. No external executable, tuning script or memory-cleaning engine was bundled or run.

| Reference | Pinned commit | Reviewed | Decision |
| --- | --- | --- | --- |
| [Microsoft PowerToys](https://github.com/microsoft/PowerToys) | `9aae39b91ecaef63ce371a5e2470b7bf74b830a6` | Official General settings screenshot and settings project documentation; MIT | Use readable settings rows, separate actions and progressive disclosure. No PowerToys code copied for this UI. |
| [SophiApp](https://github.com/Sophia-Community/SophiApp) | `62e3bc297741ca3cc12cc1c8619b5022d7141630` | README and img/1.png; MIT | Keep persistent navigation, concise option labels and clear current-state controls. No SophiApp scripts or implementation copied. |
| [Windows Memory Cleaner](https://github.com/IgorMundstein/WinMemoryCleaner) | `a19f25719c9b9a58a0acea99cb8adb4737d2d318` | README, main/compact screenshots and src/View/Window/MainWindow.xaml; GPL-3.0 | Use compact grouping and a visible primary action as design references. No GPL source, engine or executable copied. Existing guarded Boost remains unchanged. |

The product UI is implemented independently in WPF. These are bounded source/visual reviews, not full security audits of the referenced repositories. Screenshots downloaded for private inspection are not redistributed.

## Changes

- Focus: option rows with readable labels and individual switches; timer/action panel beside them; session details/recovery in an expander. Labels remain readable during active sessions.
- Overview: compact CPU/GPU/RAM cards, maintenance steps and direct Focus/Memory/Protection/Storage actions. Removes the oversized circular care button.
- Memory, Protection and Storage: shorter panels, clearer live result areas, consistent actions. Advanced real-time protection changes remain explicit and separately confirmed.
- Shared theme: mint/blue/purple semantic accents, aligned padding and lighter borders; dark/light layouts. No continuous decorative animation added.
- Idle progress controls now bind animation state to actual work, rather than leaving IsIndeterminate true while hidden. The task-strip spinner was replaced with a static marker.
- Inactive Focus status checks back off to 15 seconds. Active checks remain responsive at three seconds. Focus inventory uses one window enumeration and one process capture per inventory pass; unchanged owned priorities are not reopened on every foreground tick.

## Evidence and limits

A pre-change eight-second installed-app sample measured about 1.66% of total-machine CPU with the main window open at the user's 500 ms refresh. A managed thread-time trace was collected to investigate; its percentages include waits and are not CPU-utilization percentages. Early first-page initialization samples are kept separate from settled view measurements. Settled new samples measured 0.63% on Focus at 500 ms, 0.49% monitor-only at 500 ms and 0.05% monitor-only at 1,000 ms. The early first-Focus initialization sample was 7.93% and is explicitly not a steady-use result. See [validation](VALIDATION.md) and the sanitized resource receipt for exact scope. Short samples under changing workload do not prove a fixed reduction or zero CPU usage.
