using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text.Json;
using Cleaner.Core.Services;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Cleaner.App.Services;

public sealed record FocusStatus(Guid Session,bool Active,DateTimeOffset Expires,int WorkerId,long WorkerStart,string WorkApp,int Prioritized,int Background,string Notifications,string Awake,string Updates,string Detail);
internal static class FocusControlEntry
{
    internal static string DirectoryPath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QuietCareNext","Focus");
    internal static string JournalPath=>Path.Combine(DirectoryPath,"session.json");
    internal static string StatusPath=>Path.Combine(DirectoryPath,"status.json");
    internal static T? Read<T>(string path)=>File.Exists(path)?JsonSerializer.Deserialize<T>(File.ReadAllText(path)):default;
    internal static bool Alive(int id,long start)
    {try{using var process=Process.GetProcessById(id);return !process.HasExited&&process.StartTime.ToUniversalTime().Ticks==start;}catch{return false;}}
    static bool ValidOwner(int id,long ticks)
    {
        try
        {
            using var process=Process.GetProcessById(id);using var own=Process.GetCurrentProcess();
            if(process.HasExited||process.StartTime.ToUniversalTime().Ticks!=ticks||process.SessionId!=own.SessionId||!string.Equals(process.MainModule?.FileName,Environment.ProcessPath,StringComparison.OrdinalIgnoreCase))return false;
            using var handle=OpenProcess(0x1000,false,id);
            if(handle.IsInvalid||!OpenProcessToken(handle,8,out var token))return false;
            using(token)using(var identity=new WindowsIdentity(token.DangerousGetHandle()))using(var self=WindowsIdentity.GetCurrent())return identity.User?.Value==self.User?.Value;
        }
        catch{return false;}
    }
    internal static int Execute(string[] args)
    {
        if(args.Length==1&&args[0]=="--focus-recover-updates")return GuardRecovery();
        if(args.Length!=7||!Guid.TryParseExact(args[1],"N",out var session)||!int.TryParse(args[2],out int parent)||!long.TryParse(args[3],out long parentStart)||!int.TryParse(args[4],out int minutes)||minutes is <1 or >240||!int.TryParse(args[5],out int flags)||flags is <0 or >63||!int.TryParse(args[6],out int seed)||!ValidOwner(parent,parentStart))return 64;
        try
        {
            if(args[0]=="--focus-worker")return Worker(session,parent,parentStart,minutes,flags,seed);
            if(args[0]=="--focus-update-guard")return Guard(session,parent,parentStart,minutes);
            return 64;
        }
        catch(Exception ex){App.LogException("Focus helper",ex);return 1;}
    }
    static int Worker(Guid id,int parent,long parentStart,int minutes,int flags,int seed)
    {
        using var gate=new Mutex(false,"Local\\QuietCare.Focus.Worker."+WindowsIdentity.GetCurrent().User!.Value);
        bool acquired;try{acquired=gate.WaitOne(0);}catch(AbandonedMutexException){acquired=true;}if(!acquired)return 170;
        IDisposable? awake=null;
        try
        {
            var options=new FocusOptions(minutes,(flags&1)!=0,(flags&2)!=0,(flags&4)!=0,(flags&8)!=0,(flags&16)!=0);options.Validate();
            var runtime=new WindowsFocusRuntime(JournalPath);
            var old=Read<FocusJournal>(JournalPath);
            if(old is not null)
            {
                if(Alive(old.WorkerId,old.WorkerStart))return 170;
                if(new FocusSession(runtime,old).Restore().Count!=0)return 2;
            }
            using var self=Process.GetCurrentProcess();
            var journal=new FocusJournal{Session=id,Expires=DateTimeOffset.UtcNow.AddMinutes(minutes),WorkerId=self.Id,WorkerStart=self.StartTime.ToUniversalTime().Ticks};runtime.Save(journal);
            var session=new FocusSession(runtime,journal);
            string notifications="Notifications unchanged",power="Sleep settings unchanged",updates=options.GuardUpdates?"Waiting for update-guard approval…":"Update guard not selected",detail="Focus session running. Switch to your work app; its priority follows the active window.";
            FocusProcess? work=runtime.ReadProcess(seed);string? workImage=work?.Image;
            List<FocusProcess> candidates=[];HashSet<string> openImages=new(StringComparer.OrdinalIgnoreCase);int iteration=0;
            void Status(bool active)=>WindowsFocusRuntime.WriteJson(StatusPath,new FocusStatus(id,active,journal.Expires,self.Id,journal.WorkerStart,work?.Name??"Switch to your work app",journal.Priorities.Count(x=>x.Applied==ProcessPriorityClass.AboveNormal),journal.Priorities.Count(x=>x.Applied==ProcessPriorityClass.BelowNormal),notifications,power,updates,detail));
            try
            {
                if(options.QuietNotifications)try{notifications=session.Quiet();}catch(Exception ex){notifications="Windows Do not disturb unavailable: "+ex.Message;}
                if(options.KeepAwake)try{awake=WindowsFocusRuntime.Awake();power="Keep-awake request active for this session.";}catch(Exception ex){power="Keep awake unavailable: "+ex.Message;}
                Status(true);
                if(options.GuardUpdates)
                {
                    try
                    {
                        if(!WindowsFocusPolicyRuntime.IsSupported())updates="Update guard unavailable on this Windows edition or managed PC.";
                        else
                        {
                            using var guard=Process.Start(new ProcessStartInfo(Environment.ProcessPath!,$"--focus-update-guard {id:N} {self.Id} {journal.WorkerStart} {minutes} 0 0"){UseShellExecute=true,Verb="runas"});
                            updates=guard is null?"Windows did not start the update guard.":"Starting temporary update guard…";
                        }
                    }
                    catch(System.ComponentModel.Win32Exception ex)when(ex.NativeErrorCode==1223){updates="Administrator approval cancelled; Windows update policies unchanged.";}
                    catch(Exception ex){updates="Update guard unavailable: "+ex.Message;}
                }
                while(DateTimeOffset.UtcNow<journal.Expires&&Alive(parent,parentStart)&&!File.Exists(Path.Combine(DirectoryPath,id.ToString("N")+".stop")))
                {
                    var foreground=options.PrioritizeWork||options.LowerBackground?runtime.Foreground():null;
                    bool workChanged=foreground is not null&&!string.Equals(foreground.Image,workImage,StringComparison.OrdinalIgnoreCase);
                    if(foreground is not null){work=foreground;workImage=work.Image;}
                    bool inventoryDue=(options.PrioritizeWork||options.LowerBackground)&&iteration++%5==0;
                    if(inventoryDue)
                    {
                        var inventory=runtime.Capture();candidates=inventory.Processes;
                        if(options.LowerBackground)openImages=inventory.WindowImages;
                    }
                    if(foreground is not null&&!candidates.Any(x=>x.Id==foreground.Id))candidates.Add(foreground);
                    foreach(var change in journal.Priorities.ToArray())
                    {
                        bool workNow=string.Equals(change.Process.Image,workImage,StringComparison.OrdinalIgnoreCase);
                        if(change.Applied==ProcessPriorityClass.AboveNormal&&!workNow||change.Applied==ProcessPriorityClass.BelowNormal&&(workNow||!openImages.Contains(change.Process.Image)))
                            try{session.RestorePriority(change);}catch{ }
                    }
                    foreach(var process in candidates)
                    {
                        try
                        {
                            // Recheck owned identities at inventory boundaries or an app switch;
                            // unchanged foreground ticks do not repeatedly reopen every process.
                            if(!inventoryDue&&!workChanged&&journal.Priorities.Any(x=>FocusSession.SameProcess(x.Process,process)))continue;
                            if(options.PrioritizeWork&&workImage is not null&&string.Equals(process.Image,workImage,StringComparison.OrdinalIgnoreCase))session.SetPriority(process,ProcessPriorityClass.AboveNormal);
                            else if(options.LowerBackground&&openImages.Contains(process.Image)&&!string.Equals(process.Image,workImage,StringComparison.OrdinalIgnoreCase)&&process.Priority==ProcessPriorityClass.Normal)session.SetPriority(process,ProcessPriorityClass.BelowNormal);
                        }
                        catch{ } // Exited, protected or inaccessible processes are left alone.
                    }
                    if(options.GuardUpdates)try{var guard=WindowsFocusPolicyRuntime.Load();if(guard?.Session==id)updates=guard.Status;}catch{ }
                    Status(true);Thread.Sleep(2000);
                }
            }
            finally
            {
                awake?.Dispose();awake=null;
                power=options.KeepAwake?"Keep-awake request released. Normal sleep settings apply.":"Sleep settings unchanged.";
                var errors=session.Restore();detail=errors.Count==0?"Focus ended. Owned process priorities and notification settings were restored.":string.Join(" ",errors);
                notifications=journal.NotificationOwned?"Notification restore needs attention. Use Restore changes.":"Focus ended. No owned notification change remains.";
                // The elevated guard notices this worker's exit and restores its own policy ledger.
                if(options.GuardUpdates)updates="Ending update guard; its helper restores the saved policy when this worker exits.";
                Status(false);
            }
            return journal.Priorities.Count==0&&!journal.NotificationOwned?0:2;
        }
        finally{awake?.Dispose();gate.ReleaseMutex();}
    }
    static bool Admin(){using var user=WindowsIdentity.GetCurrent();return new WindowsPrincipal(user).IsInRole(WindowsBuiltInRole.Administrator);}
    static int Guard(Guid session,int owner,long start,int minutes)
    {
        if(!Admin())return 5;
        using var gate=new Mutex(false,@"Global\QuietCare.Focus.UpdateGuard");bool acquired;try{acquired=gate.WaitOne(0);}catch(AbandonedMutexException){acquired=true;}if(!acquired)return 170;
        FocusPolicyBackup? backup=null;
        try
        {
            if(!WindowsFocusPolicyRuntime.IsSupported())return 50;
            var old=WindowsFocusPolicyRuntime.Load();
            if(old?.Values.Count>0)
            {
                if(Alive(old.OwnerId,old.OwnerStart))return 170;
                using var recovery=new WindowsFocusPolicyRuntime();if(new FocusUpdateGuard(recovery).Restore(old).Count>0)return 2;
            }
            backup=new(){Session=session,OwnerId=owner,OwnerStart=start,Expires=DateTimeOffset.UtcNow.AddMinutes(minutes)};
            try
            {
                using(var runtime=new WindowsFocusPolicyRuntime())new FocusUpdateGuard(runtime).Apply(backup,DateTime.Now.Hour,minutes);
                while(DateTimeOffset.UtcNow<backup.Expires&&Alive(owner,start))Thread.Sleep(2000);
            }
            finally
            {
                // Reload the local GPO so changes made by other tools during the session are visible.
                using var runtime=new WindowsFocusPolicyRuntime();new FocusUpdateGuard(runtime).Restore(backup);
            }
            return backup.Values.Count==0?0:2;
        }
        catch(Exception ex)
        {
            if(backup is not null){backup.Status="Update guard needs recovery: "+ex.Message;try{using var runtime=new WindowsFocusPolicyRuntime();runtime.Save(backup);}catch{ }}
            App.LogException("Focus update guard",ex);return 1;
        }
        finally{gate.ReleaseMutex();}
    }
    static int GuardRecovery()
    {
        if(!Admin())return 5;
        using var gate=new Mutex(false,@"Global\QuietCare.Focus.UpdateGuard");bool acquired;try{acquired=gate.WaitOne(0);}catch(AbandonedMutexException){acquired=true;}if(!acquired)return 170;
        try
        {
            var backup=WindowsFocusPolicyRuntime.Load();if(backup is null||backup.Values.Count==0)return 0;
            if(Alive(backup.OwnerId,backup.OwnerStart))return 170;
            using var runtime=new WindowsFocusPolicyRuntime();return new FocusUpdateGuard(runtime).Restore(backup).Count==0?0:2;
        }
        catch(Exception ex){App.LogException("Focus update recovery",ex);return 1;}
        finally{gate.ReleaseMutex();}
    }
    [DllImport("kernel32.dll",SetLastError=true)]static extern SafeProcessHandle OpenProcess(uint access,[MarshalAs(UnmanagedType.Bool)]bool inherit,int id);
    [DllImport("advapi32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]static extern bool OpenProcessToken(SafeProcessHandle handle,uint access,out SafeAccessTokenHandle token);
}
