using Cleaner.Core.Services;

static class DefenderControlChecks
{
    public static void Run(Action<bool,string> check)
    {
        var f=new Fake();var service=new DefenderControlService(f);
        var result=service.Apply(DefenderControlAction.KeepRealtimeOff);
        check(result.Verified&&f.LocalPolicy==1&&!f.State.RealTimeEnabled&&f.Backup.PolicyOwned&&f.Backup.PreviousPolicy is null,"Explicit off persists the local policy, backs up the previous value and verifies actual Windows state");
        service.Apply(DefenderControlAction.KeepRealtimeOff);
        check(f.BackupWrites==1&&f.Backup.PreviousPolicy is null,"Repeated off does not overwrite the original policy backup");
        result=service.Apply(DefenderControlAction.RestoreRealtime);
        check(result.Verified&&f.LocalPolicy is null&&f.State.RealTimeEnabled&&!f.Backup.PolicyOwned,"Restore removes only the owned off policy and verifies protection on");
        f=new Fake{LocalPolicy=0};service=new(f);service.Apply(DefenderControlAction.KeepRealtimeOff);service.Apply(DefenderControlAction.RestoreRealtime);
        check(f.LocalPolicy==0,"Restore preserves a previous explicit zero local-policy value");
        f=new Fake();f.State=f.State with{TamperProtected=true};result=new DefenderControlService(f).Apply(DefenderControlAction.KeepRealtimeOff);
        check(!result.Verified&&f.Mutations==0,"Tamper Protection blocks off without bypasses or writes");
        f=new Fake();f.State=f.State with{Available=false};result=new DefenderControlService(f).Apply(DefenderControlAction.KeepRealtimeOff);
        check(!result.Verified&&f.Mutations==0,"Unavailable protection telemetry cannot authorize off");
        f=new Fake();f.State=f.State with{ManagedDevice=true};result=new DefenderControlService(f).Apply(DefenderControlAction.KeepRealtimeOff);
        check(!result.Verified&&f.Mutations==0,"Managed PCs do not receive local protection overrides");
        f=new Fake();f.State=f.State with{PolicySupported=false};result=new DefenderControlService(f).Apply(DefenderControlAction.KeepRealtimeOff);
        check(!result.Verified&&f.Mutations==0,"Unsupported Windows edition blocks persistent policy changes");
        f=new Fake{LocalPolicy=1};f.State=f.State with{RealTimeEnabled=false,RealtimePolicy=1};result=new DefenderControlService(f).Apply(DefenderControlAction.KeepRealtimeOff);
        check(result.Verified&&!f.Backup.PolicyOwned&&f.Mutations==0,"An existing external off policy is not claimed or changed");
        f=new Fake{IgnoreRealtime=true};result=new DefenderControlService(f).Apply(DefenderControlAction.KeepRealtimeOff);
        check(!result.Verified&&f.Backup.PolicyOwned&&result.Message.Contains("still ON"),"A rejected off request stays visibly unverified and retains its restore backup");
        f=new Fake{LoseStatusAfterWrite=true};result=new DefenderControlService(f).Apply(DefenderControlAction.KeepRealtimeOff);
        check(!result.Verified,"Losing status after a request cannot become a false protection-off success");
        f=new Fake();service=new(f);service.Apply(DefenderControlAction.KeepRealtimeOff);f.LocalPolicy=7;int before=f.Mutations;result=service.Apply(DefenderControlAction.RestoreRealtime);
        check(!result.Verified&&f.Mutations==before&&f.LocalPolicy==7,"Restore refuses to overwrite a conflicting newer local policy");
        f=new Fake{LocalPolicy=1};f.State=f.State with{RealtimePolicy=1};result=new DefenderControlService(f).Apply(DefenderControlAction.RestoreRealtime);
        check(!result.Verified&&f.Mutations==0,"Restore does not remove an off policy owned outside QuietCare");
        f=new Fake{FailBackup=true};bool failed=false;try{new DefenderControlService(f).Apply(DefenderControlAction.KeepRealtimeOff);}catch(IOException){failed=true;}
        check(failed&&f.LocalPolicy is null&&f.PreferenceWrites==0,"A failed backup prevents policy and preference changes");
        f=new Fake();service=new(f);result=service.Apply(DefenderControlAction.LowerScanCpu);
        check(result.Verified&&f.State.ScanCpuTarget==20&&f.State.LowCpuPriority&&!f.State.UnthrottledIdleScans&&f.Backup.ScanOwned&&f.State.RealTimeEnabled,"Lower scan CPU sets bounded scan preferences without disabling real-time protection");
        service.Apply(DefenderControlAction.LowerScanCpu);check(f.BackupWrites==1&&f.Backup.PreviousCpu==50,"Repeated CPU mode preserves the original scan preference backup");
        result=service.Apply(DefenderControlAction.RestoreScanCpu);
        check(result.Verified&&f.State.ScanCpuTarget==50&&!f.State.LowCpuPriority&&f.State.UnthrottledIdleScans&&!f.Backup.ScanOwned,"Previous scan preferences are restored exactly");
        f=new Fake();service=new(f);service.Apply(DefenderControlAction.LowerScanCpu);f.State=f.State with{ScanCpuTarget=35};before=f.Mutations;result=service.Apply(DefenderControlAction.RestoreScanCpu);
        check(!result.Verified&&f.Mutations==before&&f.Backup.ScanOwned,"CPU restore preserves externally changed settings and retains the backup");
        f=new Fake{IgnoreScan=true};result=new DefenderControlService(f).Apply(DefenderControlAction.LowerScanCpu);
        check(!result.Verified&&f.Backup.ScanOwned,"Ignored scan preferences cannot be reported as an applied CPU limit");
        result=new DefenderControlService(f).Apply(DefenderControlAction.RestoreScanCpu);
        check(result.Verified&&!f.Backup.ScanOwned,"An unapplied scan request can restore its unchanged original preferences");
        f=new Fake();failed=false;try{new DefenderControlService(f).Apply((DefenderControlAction)999);}catch(ArgumentOutOfRangeException){failed=true;}
        check(failed&&f.Mutations==0,"Unknown privileged actions are rejected before any mutation");
        f=new Fake();result=new DefenderControlService(f).Apply(DefenderControlAction.RestoreScanCpu);
        check(result.Verified&&f.Mutations==0,"Restoring absent scan backup is a harmless no-op");
    }
    sealed class Fake : IDefenderControlRuntime
    {
        public DefenderControlState State=new(true,true,false,true,false,null,50,false,true,false,false,"Fixture");
        public DefenderControlBackup Backup=new(false,null,false,50,false,true);
        public int? LocalPolicy;
        public int Mutations,BackupWrites,PreferenceWrites;
        public bool IgnoreRealtime,LoseStatusAfterWrite,FailBackup,IgnoreScan;
        public DefenderControlState Read()=>State with{Available=State.Available&&!(LoseStatusAfterWrite&&PreferenceWrites>0),OwnPolicy=Backup.PolicyOwned,OwnScanSettings=Backup.ScanOwned};
        public DefenderControlBackup LoadBackup()=>Backup;
        public void SaveBackup(DefenderControlBackup backup){if(FailBackup)throw new IOException("Fixture backup failure");Backup=backup;BackupWrites++;Mutations++;}
        public int? ReadLocalRealtimePolicy()=>LocalPolicy;
        public void WriteLocalRealtimePolicy(int? value){LocalPolicy=value;State=State with{RealtimePolicy=value};Mutations++;}
        public void SetRealtimeDisabled(bool disabled){PreferenceWrites++;Mutations++;if(!IgnoreRealtime)State=State with{RealTimeEnabled=!disabled};}
        public void SetScanSettings(int cpuTarget,bool lowPriority,bool unthrottledIdle){PreferenceWrites++;Mutations++;if(!IgnoreScan)State=State with{ScanCpuTarget=cpuTarget,LowCpuPriority=lowPriority,UnthrottledIdleScans=unthrottledIdle};}
        public void WaitForPolicy(){}
    }
}
