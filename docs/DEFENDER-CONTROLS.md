# Defender controls and CPU use

QuietCare 0.7.0 adds explicit controls on **Protection**. The tiny CPU / GPU / RAM monitor is unchanged. None of these protection preferences is applied at startup, by RAM Boost, by installation or by Smart Care.

## Keep real-time scanning off

On an unmanaged Windows Pro, Enterprise or Education PC, with Tamper Protection already off, **Keep real-time scanning off** requests administrator approval and an explicit confirmation. The elevated one-shot helper saves the previous local policy, changes only `Turn off real-time protection`, saves it through the official `IGroupPolicyObject` API, requests the Defender preference and reads the actual result again. Both the effective policy and real-time state must agree before the action reports verified success.

This is a persistent local real-time scanning policy, not removal of Defender or a guarantee that Windows can never turn protection on. Files opened or downloaded are not scanned in real time while it is off. Windows updates, management or Tamper Protection can override/block the policy. Other security features, manual scans and Defender updates remain available. There is no watchdog that keeps fighting Windows, service/driver removal, tamper bypass, broad exclusion or arbitrary privileged command executor.

**Restore protection** restores the policy value saved by QuietCare, requests real-time protection on and verifies Windows' current state. An absent value and an explicit zero are backed up distinctly. An externally owned off policy is not removed. A newer conflicting setting is not overwritten. Backups are kept under `HKLM\Software\QuietCare\DefenderControl` with normal machine-registry administrator write access; they are retained across app uninstall. Restore before uninstall if you want Windows defaults back. No whole-system restore point is created.

Smart Care skips its automatic Defender scan while an effective local off policy is present. It does not enable/disable protection automatically. Manual quick/full scans remain available when requested.

## Lower scan CPU

**Lower scan CPU** saves three current scan preferences and requests a 20% average CPU target, low-priority scheduled scans, and CPU throttling during idle scans. The actual values are read back. **Restore scan settings** restores the saved values and preserves external conflicting changes.

This target is guidance for scan work, not a hard process CPU ceiling. It does **not** cap real-time scans, and lowering priority can make scans take longer. It does not terminate background apps or disable antivirus protection. Unknown/managed states, unsupported values, cancelled UAC and unverified changes are reported rather than shown as success.

## Validation limits

Policy decisions, backup/restore, ownership/conflicts, unsupported/managed/tamper-protected states, invalid action rejection and missing telemetry are checked with injected fixtures. The native read-only capability probe verifies the Defender provider parameters and read-only local GPO access on the development PC. Rendering verifies the production page in both themes. The privileged policy write and restore have not been executed on the user's security configuration during acceptance, and persistence through every future Windows update is not claimed.

References: [Microsoft local policy and Tamper Protection](https://learn.microsoft.com/en-us/defender-endpoint/configure-real-time-protection-microsoft-defender-antivirus#disable-real-time-protection-in-group-policy), [scan CPU settings and their scope](https://learn.microsoft.com/en-us/defender-endpoint/troubleshoot-mdav-scan-issues#cpu-performance-and-scan-throttling-in-microsoft-defender-antivirus), [native GPO interface](https://learn.microsoft.com/en-us/windows/win32/api/gpedit/nn-gpedit-igrouppolicyobject).
