# Focus source review

Reviewed on 2026-10-28. External source was treated as untrusted data. No third-party executable or script was run and no new runtime package was added.

| Project | Pinned commit | What was inspected | Decision |
| --- | --- | --- | --- |
| [Microsoft PowerToys](https://github.com/microsoft/PowerToys) | `9aae39b91ecaef63ce371a5e2470b7bf74b830a6` | Awake/Core/Manager.cs; MIT | Use a bounded Windows power-request handle, released automatically with the worker. No power-plan replacement. Independently implemented. |
| [Process Governor](https://github.com/lowleveldesign/process-governor) | `41f49007c24bdfbfd1f63b58f94e86ce3b45b9f7` | README process-priority, job, memory-limit and freeze behavior; MIT | Use reversible Above normal / Below normal priorities. No jobs, affinity masks, memory caps, privilege enabling or deep-freeze code copied. |
| [windows-focus-assist](https://github.com/bitdisaster/windows-focus-assist) | `042e154d20224517bac512462f776bdaa667d9e9` | lib/quiethours.idl; MIT | Adapt the two-method QuietHours COM ABI. Preserve prior profile; verify readback. Complete license is bundled. This undocumented bridge can stop working on future Windows releases. |
| [taskbar-focus](https://github.com/yoelrosenthal/taskbar-focus) | `82cbb62348dbfb7c3ce4c5b5d463d2c417cdaec4` | src/os/dnd/service.rs and profile.rs; MIT | Rejected CloudStore binary rewriting and notification-service restarts. No code copied. |

The optional QuietHours declaration is based on Rafael Rivera's MIT IDL, as distributed by windows-focus-assist. It is the only adapted source in this Focus addition. The remaining code is an independent implementation guided by the platform documentation.

## Windows contracts checked

- [FocusSessionManager.TryStartFocusSession](https://learn.microsoft.com/en-us/uwp/api/windows.ui.shell.focussessionmanager.trystartfocussession?view=winrt-28000): Limited Access Feature; not bypassed or used.
- [IAutomaticUpdates.Pause](https://learn.microsoft.com/en-us/windows/win32/api/wuapi/nf-wuapi-iautomaticupdates-pause): unsupported since Windows 10, can report success without doing anything; rejected.
- [Windows Update policies](https://learn.microsoft.com/en-us/windows/deployment/update/waas-wu-settings) and [restart/active-hours policies](https://learn.microsoft.com/en-us/windows/deployment/update/waas-restart): local automatic-update and active-hours requests only; existing updates and enforced deadlines can override the intended quiet period.
- [IGroupPolicyObject](https://learn.microsoft.com/en-us/windows/win32/api/gpedit/nn-gpedit-igrouppolicyobject): official local-GPO read, save and refresh flow. All policy keys are fixed in source.
- [PowerCreateRequest](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-powercreaterequest): no registry power-plan mutation.

This comparison does not claim an audit of every file in those repositories. See [Focus mode](FOCUS-MODE.md) for the exact shipped behavior and limits.
