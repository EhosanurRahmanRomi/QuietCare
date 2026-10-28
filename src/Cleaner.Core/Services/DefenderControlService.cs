namespace Cleaner.Core.Services;

public enum DefenderControlAction { KeepRealtimeOff, RestoreRealtime, LowerScanCpu, RestoreScanCpu }
public sealed record DefenderControlState(bool Available, bool RealTimeEnabled, bool TamperProtected,
    bool PolicySupported, bool ManagedDevice, int? RealtimePolicy, int ScanCpuTarget,
    bool LowCpuPriority, bool UnthrottledIdleScans, bool OwnPolicy, bool OwnScanSettings, string Detail);
public sealed record DefenderControlResult(bool Verified, string Message);
public sealed record DefenderControlBackup(bool PolicyOwned, int? PreviousPolicy,
    bool ScanOwned, int PreviousCpu, bool PreviousLowPriority, bool PreviousUnthrottledIdle);

public interface IDefenderControlRuntime
{
    DefenderControlState Read();
    DefenderControlBackup LoadBackup();
    void SaveBackup(DefenderControlBackup backup);
    int? ReadLocalRealtimePolicy();
    void WriteLocalRealtimePolicy(int? value);
    void SetRealtimeDisabled(bool disabled);
    void SetScanSettings(int cpuTarget, bool lowPriority, bool unthrottledIdle);
    void WaitForPolicy();
}

// These actions run only after an explicit click and an elevated Windows prompt.
// No background enforcement, service/driver changes, exclusions or tamper bypass.
public sealed class DefenderControlService(IDefenderControlRuntime runtime)
{
    public DefenderControlResult Apply(DefenderControlAction action)
    {
        if (!Enum.IsDefined(action)) throw new ArgumentOutOfRangeException(nameof(action));
        var state=runtime.Read();
        if (!state.Available) return new(false,"Windows Defender status is unavailable. No settings were changed.");
        if (state.ManagedDevice) return new(false,"This PC is managed. Change its Defender policy through your administrator; no settings were changed.");
        var backup=runtime.LoadBackup();
        switch(action)
        {
            case DefenderControlAction.KeepRealtimeOff:
                if (state.TamperProtected) return new(false,"Tamper Protection is on. Windows protects this setting; QuietCare did not change it.");
                if (!state.PolicySupported) return new(false,"Local Defender policy requires Windows Pro, Enterprise or Education. No settings were changed.");
                var original=runtime.ReadLocalRealtimePolicy();
                if(original is not null and not 0 and not 1) return new(false,"The existing policy has an unsupported value. No settings were changed.");
                if (!backup.PolicyOwned)
                {
                    if(original==1) return new(!state.RealTimeEnabled,"A local policy already requests real-time protection off. QuietCare did not take ownership of that policy.");
                    runtime.SaveBackup(backup=backup with{PolicyOwned=true,PreviousPolicy=original});
                }
                // The native implementation persists this single setting through IGroupPolicyObject.
                runtime.WriteLocalRealtimePolicy(1);
                runtime.SetRealtimeDisabled(true);
                runtime.WaitForPolicy();
                var off=runtime.Read();
                return new(off.Available&&!off.RealTimeEnabled&&off.RealtimePolicy==1,!off.Available?"The local policy was saved, but Windows status is unavailable. Protection OFF has not been verified.":off.RealTimeEnabled
                    ?"The local off policy was saved, but real-time protection is still ON. Windows may have blocked or overridden it. Do not assume protection is off."
                    :off.RealtimePolicy!=1?"Real-time protection is currently OFF, but Windows has not confirmed the effective local off policy. Refresh status later; persistence is not verified."
                    :"Real-time protection is OFF. The local off policy remains until restored. Windows security updates or management can override it. Manual scans and Defender updates remain available.");
            case DefenderControlAction.RestoreRealtime:
                if(backup.PolicyOwned)
                {
                    var current=runtime.ReadLocalRealtimePolicy();
                    if(current!=1 && current!=backup.PreviousPolicy)
                        return new(false,"Another change replaced QuietCare's local policy. Review it in Local Group Policy; QuietCare will not overwrite it.");
                    runtime.WriteLocalRealtimePolicy(backup.PreviousPolicy);
                }
                else if(state.RealtimePolicy==1)
                    return new(false,"A policy owned outside QuietCare requests protection off. Review it in Local Group Policy.");
                runtime.SetRealtimeDisabled(false);
                runtime.WaitForPolicy();
                var on=runtime.Read();
                if(on.Available&&on.RealTimeEnabled && backup.PolicyOwned)runtime.SaveBackup(backup with{PolicyOwned=false,PreviousPolicy=null});
                return new(on.Available&&on.RealTimeEnabled,on.Available&&on.RealTimeEnabled?"Real-time protection is ON. QuietCare's previous local policy was restored.":"The restore request finished, but protection ON has not been verified. Review Windows Security and any managed policy.");
            case DefenderControlAction.LowerScanCpu:
                if(state.ScanCpuTarget is <0 or >100)return new(false,"Windows returned invalid scan preferences. No settings were changed.");
                if(!backup.ScanOwned)runtime.SaveBackup(backup=backup with{ScanOwned=true,PreviousCpu=state.ScanCpuTarget,PreviousLowPriority=state.LowCpuPriority,PreviousUnthrottledIdle=state.UnthrottledIdleScans});
                runtime.SetScanSettings(20,true,false);
                var low=runtime.Read();
                bool applied=low.Available&&low.ScanCpuTarget==20&&low.LowCpuPriority&&!low.UnthrottledIdleScans;
                return new(applied,applied?"Lower scan CPU mode is applied: 20% average scan target, low priority and idle-scan throttling. This is not a hard CPU cap and does not limit real-time scans.":"Windows did not retain all requested scan settings. Review the displayed values; no CPU limit is guaranteed.");
            case DefenderControlAction.RestoreScanCpu:
                if(!backup.ScanOwned)return new(true,"No scan settings saved by QuietCare need restoring.");
                if(state.ScanCpuTarget!=20&&state.ScanCpuTarget!=backup.PreviousCpu
                    ||state.LowCpuPriority!=true&&state.LowCpuPriority!=backup.PreviousLowPriority
                    ||state.UnthrottledIdleScans!=false&&state.UnthrottledIdleScans!=backup.PreviousUnthrottledIdle)
                    return new(false,"Scan preferences changed outside QuietCare. Their saved backup is retained; QuietCare will not overwrite the newer preferences.");
                runtime.SetScanSettings(backup.PreviousCpu,backup.PreviousLowPriority,backup.PreviousUnthrottledIdle);
                var restored=runtime.Read();
                bool verified=restored.Available&&restored.ScanCpuTarget==backup.PreviousCpu&&restored.LowCpuPriority==backup.PreviousLowPriority&&restored.UnthrottledIdleScans==backup.PreviousUnthrottledIdle;
                if(verified)runtime.SaveBackup(backup with{ScanOwned=false});
                return new(verified,verified?"Your previous scan CPU settings were restored.":"Windows did not retain the previous scan settings. The backup is still available.");
            default:throw new ArgumentOutOfRangeException(nameof(action));
        }
    }
}
