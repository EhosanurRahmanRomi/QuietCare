using System.Diagnostics;

namespace Cleaner.Core.Services;

public enum FocusDurationUnit { Minutes,Hours,UntilStopped }
public static class FocusDuration
{
    public const int MaxMinutes=10080;
    public static int ToMinutes(int amount,FocusDurationUnit unit)
    {
        if(!Enum.IsDefined(unit))throw new ArgumentOutOfRangeException(nameof(unit));
        if(unit==FocusDurationUnit.UntilStopped)return 0;
        int maximum=unit==FocusDurationUnit.Hours?MaxMinutes/60:MaxMinutes;
        if(amount<1||amount>maximum)throw new ArgumentOutOfRangeException(nameof(amount),"Choose a positive duration up to seven days, or Until I end it.");
        return checked(amount*(unit==FocusDurationUnit.Hours?60:1));
    }
    public static DateTimeOffset ExpiresAt(DateTimeOffset now,int minutes)
    {
        Validate(minutes);return minutes==0?DateTimeOffset.MaxValue:now.AddMinutes(minutes);
    }
    public static void Validate(int minutes)
    {if(minutes is <0 or >MaxMinutes)throw new ArgumentOutOfRangeException(nameof(minutes),"Use 1 to 10,080 minutes, or zero for Until I end it.");}
    public static string Remaining(DateTimeOffset expires,DateTimeOffset now)
    {
        if(expires==DateTimeOffset.MaxValue)return "Until you end it";
        int minutes=Math.Max(0,(int)Math.Ceiling((expires-now).TotalMinutes));
        return minutes>=60?$"{minutes/60} hr{(minutes%60==0?"":" "+minutes%60+" min")} remaining":minutes+" min remaining";
    }
}
public sealed record FocusOptions(int Minutes=60,bool PrioritizeWork=true,bool LowerBackground=true,bool QuietNotifications=true,bool KeepAwake=true,bool GuardUpdates=true)
{
    public FocusDurationUnit DurationUnit {get;init;}=FocusDurationUnit.Minutes;
    public int DurationAmount {get;init;}
    public void Validate()=>FocusDuration.Validate(Minutes);
}
public sealed record FocusProcess(int Id,long StartTicks,string Image,string Name,ProcessPriorityClass Priority);
public sealed record FocusPriorityChange(FocusProcess Process,ProcessPriorityClass Applied);
public sealed class FocusJournal
{
    public Guid Session {get;set;}
    public DateTimeOffset Expires {get;set;}
    public int WorkerId {get;set;}
    public long WorkerStart {get;set;}
    public string? PreviousNotifications {get;set;}
    public bool NotificationOwned {get;set;}
    public List<FocusPriorityChange> Priorities {get;set;}=[];
    public List<FocusProcess> ExternallyChanged {get;set;}=[];
}
public interface IFocusRuntime
{
    FocusProcess? ReadProcess(int id);
    void SetPriority(FocusProcess process,ProcessPriorityClass priority);
    string ReadNotifications();
    void SetNotifications(string profile);
    void Save(FocusJournal journal);
}

// A small, testable ownership ledger. Every change is journaled before it is attempted.
// PID, creation time and executable must all agree before a process can be restored.
public sealed class FocusSession(IFocusRuntime runtime,FocusJournal journal)
{
    public const string Unrestricted="Microsoft.QuietHoursProfile.Unrestricted";
    public const string PriorityOnly="Microsoft.QuietHoursProfile.PriorityOnly";
    public const string AlarmsOnly="Microsoft.QuietHoursProfile.AlarmsOnly";
    public FocusJournal Journal=>journal;
    public static bool SameProcess(FocusProcess a,FocusProcess b)=>a.Id==b.Id&&a.StartTicks==b.StartTicks&&string.Equals(a.Image,b.Image,StringComparison.OrdinalIgnoreCase);
    public static bool KnownProfile(string value)=>value is Unrestricted or PriorityOnly or AlarmsOnly;
    public static bool SafePriority(ProcessPriorityClass value)=>value is ProcessPriorityClass.Normal or ProcessPriorityClass.AboveNormal or ProcessPriorityClass.BelowNormal;
    public void SetPriority(FocusProcess process,ProcessPriorityClass priority)
    {
        if(!SafePriority(priority)||!SafePriority(process.Priority))return;
        if(journal.ExternallyChanged.Any(x=>SameProcess(x,process)))return;
        var current=runtime.ReadProcess(process.Id);
        if(current is null||!SameProcess(current,process))return;
        var owned=journal.Priorities.FirstOrDefault(x=>SameProcess(x.Process,process));
        if(owned is not null)
        {
            if(current.Priority!=owned.Applied)
            {journal.ExternallyChanged.Add(current);journal.Priorities.Remove(owned);runtime.Save(journal);return;}
            if(owned.Applied==priority)return;
            RestorePriority(owned);current=runtime.ReadProcess(process.Id);
            if(current is null||!SameProcess(current,process)||current.Priority!=owned.Process.Priority)return;
        }
        if(current.Priority==priority)return;
        var change=new FocusPriorityChange(current,priority);
        journal.Priorities.Add(change);runtime.Save(journal);
        runtime.SetPriority(current,priority);
    }
    public void RestorePriority(FocusPriorityChange change)
    {
        var current=runtime.ReadProcess(change.Process.Id);
        if(current is not null&&SameProcess(current,change.Process)&&current.Priority!=change.Applied&&current.Priority!=change.Process.Priority)
            journal.ExternallyChanged.Add(current);
        if(current is not null&&SameProcess(current,change.Process)&&current.Priority==change.Applied)
        {
            runtime.SetPriority(current,change.Process.Priority);
            var after=runtime.ReadProcess(current.Id);
            if(after is not null&&SameProcess(after,current)&&after.Priority!=change.Process.Priority)throw new InvalidOperationException("Windows did not restore the process priority.");
        }
        journal.Priorities.Remove(change);runtime.Save(journal);
    }
    public string Quiet()
    {
        string previous=runtime.ReadNotifications();
        if(!KnownProfile(previous))return "Windows returned an unknown notification profile; it was preserved.";
        if(previous!=Unrestricted)return "Windows notifications were already quiet; the existing profile is preserved.";
        journal.PreviousNotifications=previous;journal.NotificationOwned=true;runtime.Save(journal);
        runtime.SetNotifications(PriorityOnly);
        return runtime.ReadNotifications()==PriorityOnly?"Windows Do not disturb requested and read back (priority notifications can still appear).":"Windows did not confirm Do not disturb. Use Windows notification settings.";
    }
    public IReadOnlyList<string> Restore()
    {
        var errors=new List<string>();
        foreach(var change in journal.Priorities.ToArray())
            try{RestorePriority(change);}catch(Exception ex){errors.Add("Priority restore: "+ex.Message);}
        if(journal.NotificationOwned)
        {
            try
            {
                string current=runtime.ReadNotifications();
                if(current==PriorityOnly&&journal.PreviousNotifications is Unrestricted)
                {
                    runtime.SetNotifications(journal.PreviousNotifications);
                    if(runtime.ReadNotifications()!=journal.PreviousNotifications)throw new InvalidOperationException("Windows did not confirm the previous notification profile.");
                }
                journal.NotificationOwned=false;runtime.Save(journal);
            }
            catch(Exception ex){errors.Add("Notification restore: "+ex.Message);}
        }
        return errors;
    }
}
