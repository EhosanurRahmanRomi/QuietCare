using System.Diagnostics;
using Cleaner.Core.Services;

static class FocusChecks
{
    static readonly FocusProcess First=new(123,456,@"C:\Apps\work.exe","work",ProcessPriorityClass.Normal);
    static bool Throws(Action run){try{run();return false;}catch{return true;}}
    public static void Run(Action<bool,string> check)
    {
        var fake=new Runtime();var journal=new FocusJournal{Session=Guid.NewGuid()};var focus=new FocusSession(fake,journal);
        check(Throws(()=>new FocusOptions(0).Validate())&&Throws(()=>new FocusOptions(241).Validate()),"Focus duration rejects unbounded or invalid sessions");
        focus.SetPriority(First,ProcessPriorityClass.AboveNormal);
        check(fake.Process?.Priority==ProcessPriorityClass.AboveNormal&&fake.SavedBeforeSet,"Focus journals original identity and priority before changing it");
        focus.SetPriority(First,ProcessPriorityClass.AboveNormal);check(fake.Writes==1&&journal.Priorities.Count==1,"Focus avoids repeated priority writes");
        focus.SetPriority(First,ProcessPriorityClass.BelowNormal);check(journal.Priorities.Single().Process.Priority==ProcessPriorityClass.Normal&&fake.Process?.Priority==ProcessPriorityClass.BelowNormal,"Focus priority switches retain the original baseline");
        check(focus.Restore().Count==0&&fake.Process?.Priority==ProcessPriorityClass.Normal&&journal.Priorities.Count==0,"Focus end restores original priority and releases ownership");
        focus.SetPriority(First,ProcessPriorityClass.High);focus.SetPriority(First,ProcessPriorityClass.RealTime);check(fake.Process?.Priority==ProcessPriorityClass.Normal,"Focus never applies High or Realtime priority");
        focus.SetPriority(First,ProcessPriorityClass.AboveNormal);fake.Process=First with{StartTicks=457,Priority=ProcessPriorityClass.AboveNormal};focus.Restore();check(fake.Process.Priority==ProcessPriorityClass.AboveNormal,"Focus recovery refuses a reused PID");
        fake=new();journal=new();focus=new(fake,journal);focus.SetPriority(First,ProcessPriorityClass.AboveNormal);fake.Process=First with{Image=@"C:\Other\work.exe",Priority=ProcessPriorityClass.AboveNormal};focus.Restore();check(fake.Process.Priority==ProcessPriorityClass.AboveNormal,"Focus recovery verifies executable identity");
        fake=new();journal=new();focus=new(fake,journal);focus.SetPriority(First,ProcessPriorityClass.AboveNormal);fake.Process=First with{Priority=ProcessPriorityClass.BelowNormal};focus.Restore();check(fake.Process.Priority==ProcessPriorityClass.BelowNormal,"Focus preserves a priority changed by another tool");
        focus.SetPriority(First,ProcessPriorityClass.AboveNormal);check(fake.Process.Priority==ProcessPriorityClass.BelowNormal,"Focus does not reclaim a process changed externally during the session");
        fake=new(){FailSave=true};journal=new();focus=new(fake,journal);check(Throws(()=>focus.SetPriority(First,ProcessPriorityClass.AboveNormal))&&fake.Writes==0,"Focus refuses mutation if the journal cannot be saved");
        fake=new(){FailSet=true};journal=new();focus=new(fake,journal);check(Throws(()=>focus.SetPriority(First,ProcessPriorityClass.AboveNormal))&&journal.Priorities.Count==1,"Focus retains recovery data after a failed priority request");fake.FailSet=false;focus.Restore();
        fake=new();journal=new();focus=new(fake,journal);focus.Quiet();check(journal.NotificationOwned&&journal.PreviousNotifications==FocusSession.Unrestricted&&fake.Profile==FocusSession.PriorityOnly,"Focus records the previous Windows notification profile");
        focus.Restore();check(fake.Profile==FocusSession.Unrestricted&&!journal.NotificationOwned,"Focus restores its notification change");
        fake.Profile=FocusSession.AlarmsOnly;focus.Quiet();check(!journal.NotificationOwned&&fake.Profile==FocusSession.AlarmsOnly,"Existing Do not disturb and alarms-only profiles are preserved");
        fake.Profile="unknown-profile";focus.Quiet();check(!journal.NotificationOwned&&fake.Profile=="unknown-profile","Unknown notification profiles are never overwritten");
        fake.Profile=FocusSession.Unrestricted;focus.Quiet();fake.Profile=FocusSession.AlarmsOnly;focus.Restore();check(fake.Profile==FocusSession.AlarmsOnly&&!journal.NotificationOwned,"Focus does not undo notification changes made elsewhere");
        fake.Profile=FocusSession.Unrestricted;focus.Quiet();fake.FailNotificationRestore=true;check(focus.Restore().Count==1&&journal.NotificationOwned,"Failed notification restore retains recoverable ownership");
        check(!WindowsFocusRuntime.EligibleImage(@"C:\Windows\System32\svchost.exe","svchost")&&!WindowsFocusRuntime.EligibleImage(@"C:\Security\MsMpEng.exe","MsMpEng")&&!WindowsFocusRuntime.CanLower("dotnet"),"Focus excludes Windows/antivirus and protects project/build background priorities");
        check(!WindowsFocusRuntime.EligibleImage(@"C:\Program Files\WindowsApps\Microsoft.SecHealthUI\SecHealthUI.exe","SecHealthUI")&&!WindowsFocusRuntime.CanLower("terminal64")&&!WindowsFocusRuntime.CanLower("MarketLink"),"Focus leaves Windows Security and background trading/project hosts alone");
        var policies=new PolicyRuntime();var backup=new FocusPolicyBackup{Session=Guid.NewGuid()};var guard=new FocusUpdateGuard(policies);
        guard.Apply(backup,23,120);check(policies.SavedBeforeSet&&policies.Values["active-end"]==2&&policies.Values["automatic-updates"]==1,"Focus update guard journals all policies before writing and crosses midnight safely");
        guard.Restore(backup);check(policies.Values.Values.All(x=>x is null)&&backup.Values.Count==0,"Update guard restores previously absent policy values");
        policies=new();policies.Values["automatic-updates"]=0;backup=new(){Session=Guid.NewGuid()};guard=new(policies);guard.Apply(backup,9,60);guard.Restore(backup);check(policies.Values["automatic-updates"]==0,"Update guard restores explicit original DWORD values");
        policies=new(){Supported=false};backup=new(){Session=Guid.NewGuid()};guard=new(policies);check(Throws(()=>guard.Apply(backup,9,60))&&policies.Writes==0,"Update guard refuses unsupported or managed PCs before writes");
        policies=new(){FailSave=true};backup=new(){Session=Guid.NewGuid()};guard=new(policies);check(Throws(()=>guard.Apply(backup,9,60))&&policies.Writes==0,"Update guard refuses changes without a durable recovery ledger");
        policies=new();backup=new(){Session=Guid.NewGuid()};guard=new(policies);guard.Apply(backup,9,60);policies.Values["active-end"]=16;var warnings=guard.Restore(backup);check(warnings.Count==1&&policies.Values["active-end"]==16&&backup.Values.Count==0,"Update recovery preserves externally changed policy and releases ownership");
        var invalid=new FocusPolicyBackup{Session=Guid.NewGuid(),Values=[new("unexpected-key",null,1)]};check(Throws(()=>guard.Restore(invalid)),"Update recovery rejects an unknown policy key");
        invalid=new(){Session=Guid.NewGuid(),Values=[new("active-end",200,5)]};check(Throws(()=>guard.Restore(invalid)),"Update recovery rejects malformed saved values");
        policies=new(){IgnoreWrites=true};backup=new(){Session=Guid.NewGuid()};guard=new(policies);guard.Apply(backup,9,60);check(backup.Status.Contains("did not confirm"),"Update guard does not report unconfirmed policy changes as applied");
        policies=new();policies.Values["active-start"]=99;backup=new(){Session=Guid.NewGuid()};guard=new(policies);check(Throws(()=>guard.Apply(backup,9,60))&&policies.Writes==0,"Update guard preserves unsupported existing policy values");
    }
    public static async Task Native(string root,Action<bool,string> check)
    {
        // Only this dedicated child is reprioritized; no user application is used.
        string helper=Path.Combine(root,"FocusFixture.exe"),stop=Path.Combine(root,"focus-helper.stop");
        File.Copy(Path.Combine(AppContext.BaseDirectory,"QuietCare.Regression.exe"),helper);
        foreach(string file in Directory.GetFiles(AppContext.BaseDirectory))
            if(!file.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))File.Copy(file,Path.Combine(root,Path.GetFileName(file)),true);
        using var child=Process.Start(new ProcessStartInfo(helper){UseShellExecute=false,CreateNoWindow=true,ArgumentList={"--focus-helper",stop}})!;
        try
        {
            var runtime=new WindowsFocusRuntime(Path.Combine(root,"native-focus-journal.json"));FocusProcess? item=null;
            for(int i=0;i<20&&item is null;i++){await Task.Delay(100);item=runtime.ReadProcess(child.Id);}
            check(item is not null,"Native Focus adapter resolves the dedicated same-user child");
            var journal=new FocusJournal{Session=Guid.NewGuid()};var session=new FocusSession(runtime,journal);
            try
            {
                session.SetPriority(item!,ProcessPriorityClass.AboveNormal);
                check(runtime.ReadProcess(child.Id)?.Priority==ProcessPriorityClass.AboveNormal,"Native Focus priority change is read back from Windows");
            }
            finally{session.Restore();}
            check(runtime.ReadProcess(child.Id)?.Priority==item!.Priority,"Native Focus restores the dedicated child's exact original priority");
        }
        finally{File.WriteAllText(stop,"exit");await child.WaitForExitAsync();}
        check(child.ExitCode==0,"Dedicated Focus helper exits normally without forced termination");
    }
    sealed class Runtime : IFocusRuntime
    {
        public FocusProcess? Process=First;public string Profile=FocusSession.Unrestricted;public bool FailSave,FailSet,FailNotificationRestore,SavedBeforeSet;public int Writes;bool saved;
        public FocusProcess? ReadProcess(int id)=>Process?.Id==id?Process:null;
        public void SetPriority(FocusProcess process,ProcessPriorityClass priority){if(FailSet)throw new Exception("fixture denied");SavedBeforeSet=saved;Writes++;Process=process with{Priority=priority};}
        public string ReadNotifications()=>Profile;
        public void SetNotifications(string profile){if(FailNotificationRestore&&profile==FocusSession.Unrestricted)throw new Exception("fixture unavailable");Profile=profile;}
        public void Save(FocusJournal journal){if(FailSave)throw new Exception("fixture full disk");saved=true;}
    }
    sealed class PolicyRuntime : IFocusPolicyRuntime
    {
        public bool Supported {get;set;}=true;public bool FailSave,IgnoreWrites,SavedBeforeSet;public int Writes;bool saved;
        public Dictionary<string,int?> Values=FocusUpdateGuard.Settings.Keys.ToDictionary(x=>x,x=>(int?)null);
        public int? Read(string key)=>Values[key];
        public void Set(string key,int? value){SavedBeforeSet=saved;Writes++;if(!IgnoreWrites)Values[key]=value;}
        public void Commit(){ }
        public void Save(FocusPolicyBackup backup){if(FailSave)throw new Exception("fixture denied backup");saved=true;}
    }
}
