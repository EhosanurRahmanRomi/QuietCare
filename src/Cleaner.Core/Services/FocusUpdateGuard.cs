namespace Cleaner.Core.Services;

public sealed record FocusPolicyValue(string Key,int? Previous,int Applied);
public sealed class FocusPolicyBackup
{
    public Guid Session {get;set;}
    public int OwnerId {get;set;}
    public long OwnerStart {get;set;}
    public DateTimeOffset Expires {get;set;}
    public List<FocusPolicyValue> Values {get;set;}=[];
    public string Status {get;set;}="Not applied";
}
public interface IFocusPolicyRuntime
{
    bool Supported {get;}
    int? Read(string key);
    void Set(string key,int? value);
    void Commit();
    void Save(FocusPolicyBackup backup);
}
public sealed class FocusUpdateGuard(IFocusPolicyRuntime runtime)
{
    public static readonly IReadOnlyDictionary<string,(string Path,string Name)> Settings=new Dictionary<string,(string,string)>
    {
        ["automatic-updates"]=(@"Software\Policies\Microsoft\Windows\WindowsUpdate\AU","NoAutoUpdate"),
        ["active-hours"]=(@"Software\Policies\Microsoft\Windows\WindowsUpdate","SetActiveHours"),
        ["active-start"]=(@"Software\Policies\Microsoft\Windows\WindowsUpdate","ActiveHoursStart"),
        ["active-end"]=(@"Software\Policies\Microsoft\Windows\WindowsUpdate","ActiveHoursEnd")
    };
    public void Apply(FocusPolicyBackup backup,int startHour,int durationMinutes)
    {
        if(!runtime.Supported)throw new InvalidOperationException("Temporary update policies require an unmanaged Windows Pro, Enterprise or Education PC.");
        if(startHour is <0 or >23||durationMinutes is <1 or >240||backup.Values.Count!=0||backup.Session==Guid.Empty)throw new ArgumentException("Invalid Focus update session.");
        int end=(startHour+(int)Math.Ceiling(durationMinutes/60.0)+1)%24;
        var values=new Dictionary<string,int>{{"automatic-updates",1},{"active-hours",1},{"active-start",startHour},{"active-end",end}};
        foreach(var entry in values)
        {
            int? previous=runtime.Read(entry.Key);
            if(previous==entry.Value)continue;
            if(previous.HasValue&&(entry.Key is "automatic-updates" or "active-hours"?previous is not 0 and not 1:previous is <0 or >23))throw new InvalidOperationException("An unrecognized update policy value was preserved.");
            backup.Values.Add(new(entry.Key,previous,entry.Value));
        }
        // Whole rollback ledger is durable before the first local GPO edit.
        backup.Status="Applying temporary update policies";runtime.Save(backup);
        foreach(var value in backup.Values)runtime.Set(value.Key,value.Applied);
        runtime.Commit();
        backup.Status=backup.Values.All(x=>runtime.Read(x.Key)==x.Applied)?"Temporary update policies saved; in-progress updates and enforced deadlines can still run.":"Windows did not confirm all temporary update policies.";
        runtime.Save(backup);
    }
    public IReadOnlyList<string> Restore(FocusPolicyBackup backup)
    {
        if(backup.Session==Guid.Empty||backup.Values.Count>4||backup.Values.Select(x=>x.Key).Distinct().Count()!=backup.Values.Count)throw new InvalidOperationException("Invalid Focus recovery ledger.");
        foreach(var value in backup.Values)
        {
            bool flag=value.Key is "automatic-updates" or "active-hours";
            if(!Settings.ContainsKey(value.Key)||(flag?value.Applied!=1||value.Previous is not null and not 0 and not 1:value.Applied is <0 or >23||value.Previous is <0 or >23))throw new InvalidOperationException("Invalid policy value in the Focus recovery ledger.");
        }
        var warnings=new List<string>();var completed=new List<FocusPolicyValue>();
        foreach(var value in backup.Values.ToArray())
        {
            if(!Settings.ContainsKey(value.Key))throw new InvalidOperationException("Unknown policy in the Focus recovery ledger.");
            int? current=runtime.Read(value.Key);
            if(current==value.Applied){runtime.Set(value.Key,value.Previous);completed.Add(value);}
            else if(current==value.Previous)completed.Add(value);
            else
            {
                warnings.Add(value.Key+": changed elsewhere; its current value was preserved and QuietCare released ownership.");
                backup.Values.Remove(value);
            }
        }
        runtime.Commit();
        foreach(var value in completed)
            if(runtime.Read(value.Key)==value.Previous)backup.Values.Remove(value);else warnings.Add(value.Key+": Windows did not confirm restore; recovery data was retained.");
        backup.Status=warnings.Count==0?"Previous update policies restored.":string.Join(" ",warnings);
        runtime.Save(backup);return warnings;
    }
}
