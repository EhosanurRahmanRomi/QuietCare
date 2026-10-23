namespace Cleaner.Core.Services;

public sealed record StartupSnapshot(string Key,string Name,string Command,bool Enabled,AutostartSource Source)
{
    public static StartupSnapshot From(AutostartEntry item)=>new(item.Source+"|"+item.Location+"|"+item.Name,item.Name,item.Command,item.IsEnabled,item.Source);
}
public sealed record StartupChange(DateTimeOffset Time,string Kind,StartupSnapshot Entry);
public sealed class StartupBaseline
{
    public HashSet<AutostartSource> InitializedSources { get; set; }=[];
    public Dictionary<string,StartupSnapshot> Entries { get; set; }=new(StringComparer.OrdinalIgnoreCase);
}
public static class StartupChangeDetector
{
    public static IReadOnlyList<StartupChange> Update(StartupBaseline baseline,IEnumerable<StartupSnapshot> current,IReadOnlySet<AutostartSource> covered)
    {
        baseline.Entries=new Dictionary<string,StartupSnapshot>(baseline.Entries,StringComparer.OrdinalIgnoreCase);
        var changes=new List<StartupChange>();var entries=current.GroupBy(x=>x.Key,StringComparer.OrdinalIgnoreCase).Select(x=>x.Last()).ToArray();
        foreach(var item in entries)
        {
            if(!covered.Contains(item.Source))continue;
            if(baseline.InitializedSources.Contains(item.Source))
            {
                if(!baseline.Entries.TryGetValue(item.Key,out var old))changes.Add(new(DateTimeOffset.UtcNow,"New startup item",item));
                else if(!string.Equals(old.Command,item.Command,StringComparison.Ordinal))changes.Add(new(DateTimeOffset.UtcNow,"Startup command changed",item));
                else if(!old.Enabled&&item.Enabled)changes.Add(new(DateTimeOffset.UtcNow,"Startup item enabled",item));
            }
            baseline.Entries[item.Key]=item;
        }
        // Retain last-seen records if a source is inaccessible; avoid a burst of false 'new' alerts.
        baseline.InitializedSources.UnionWith(covered);return changes;
    }
}
