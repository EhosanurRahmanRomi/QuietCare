using System.Diagnostics;
using Cleaner.Core.Cleaners;
using Cleaner.Core.Cleaners.Browsers;
using Cleaner.Core.Models;
namespace Cleaner.Core.Services;

public sealed record AutomaticCleanupResult(long DeletedBytes,int DeletedFiles,int PreservedFiles,IReadOnlyList<string> Details);
public sealed class AutomaticCleanupService
{
    readonly SemaphoreSlim gate=new(1,1);
    public static IEnumerable<ICleanupTarget> DefaultPlan()
    {
        string temp=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Temp");
        yield return new SafeTemporaryFiles(temp,"*.tmp");yield return new SafeTemporaryFiles(temp,"*.temp");
        string local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return new ClosedBrowserCache("chrome",Path.Combine(local,"Google","Chrome","User Data"),false);
        yield return new ClosedBrowserCache("msedge",Path.Combine(local,"Microsoft","Edge","User Data"),false);
        yield return new ClosedBrowserCache("brave",Path.Combine(local,"BraveSoftware","Brave-Browser","User Data"),false);
        yield return new ClosedBrowserCache("firefox",Path.Combine(local,"Mozilla","Firefox","Profiles"),true);
    }
    static bool Running(string name)
    {
        try{var processes=Process.GetProcessesByName(name);bool exists=processes.Length>0;foreach(var process in processes)process.Dispose();return exists;}
        catch{return true;}
    }
    public async Task<AutomaticCleanupResult> RunAsync(IProgress<string>? progress=null,CancellationToken ct=default,IEnumerable<ICleanupTarget>? fixturePlan=null)
    {
        if(!await gate.WaitAsync(0,ct))throw new InvalidOperationException("Safe cleanup is already running.");
        try
        {
            long freed=0;int deleted=0,preserved=0;var details=new List<string>();
            foreach(var target in fixturePlan??DefaultPlan())
            {
                ct.ThrowIfCancellationRequested();if(!target.IsAvailable())continue;
                progress?.Report("Scanning "+target.Name+"…");
                var scan=await target.ScanAsync(new Progress<ScanProgress>(p=>progress?.Report($"Scanning {target.Name} · {p.FilesSoFar} candidates")),ct);
                progress?.Report($"Checking {scan.FileCount} old files in {target.Name}…");
                var result=await target.CleanAsync(scan,true,null,ct);preserved+=result.Errors.Count;
                var ids=result.RecoveryIds.ToHashSet(StringComparer.Ordinal);
                // Only receipts returned by this cleanup are eligible for permanent deletion.
                foreach(var item in RecoveryStore.List().Where(x=>ids.Contains(x.Id)))
                {
                    ct.ThrowIfCancellationRequested();
                    try{RecoveryStore.Purge(item);freed+=item.Bytes;deleted++;}
                    catch(Exception ex){preserved++;details.Add("Preserved in Recovery: "+ex.Message);}
                }
                details.Add($"{target.Name}: {result.FilesDeleted} files processed; {result.Errors.Count} preserved/skipped.");
                progress?.Report($"Removed {deleted} old junk files · {Utils.ByteFormatter.Format(freed)} deleted.");
            }
            details.Add("Running browsers, cookies, passwords, session data, documents, project folders and package caches were excluded from the automatic plan.");
            return new(freed,deleted,preserved,details.AsReadOnly());
        }
        finally{gate.Release();}
    }
    // This automatic plan has its own age floor even when advanced cleanup settings are relaxed.
    public sealed class ClosedBrowserCache(string processName,string profilesRoot,bool firefox,Func<bool>? isRunning=null) : CleanupTargetBase(new FileSystemOperations())
    {
        readonly Func<bool> running=isRunning??(()=>Running(processName));
        DateTime checkedAt=DateTime.MinValue;bool active=true;
        bool Closed(){if(DateTime.UtcNow-checkedAt>TimeSpan.FromMilliseconds(100)){active=running();checkedAt=DateTime.UtcNow;}return !active;}
        public override string Id=>"automatic.browser."+processName;
        public override string Name=>processName+" closed-browser cache";
        public override string Description=>"Old disposable caches only. Active browsers and profile/session storage are preserved.";
        public override string IconGlyph=>"";
        public override CleanupCategory Category=>CleanupCategory.Browsers;
        public override SafetyLevel SafetyLevel=>SafetyLevel.Caution;
        public override bool IsAvailable()=>Closed()&&base.IsAvailable();
        protected override TimeSpan? MinimumAge=>TimeSpan.FromDays(7);
        protected override bool AdditionalPolicy(string path)=>Closed();
        protected override IEnumerable<string> EnumerateCleanupRoots()
        {
            if(!Closed())yield break;
            foreach(var profile in Utils.SafeEnumerator.EnumerateDirectories(profilesRoot))
            {
                var name=Path.GetFileName(profile);
                if(!firefox&&!name.Equals("Default",StringComparison.OrdinalIgnoreCase)&&!name.StartsWith("Profile ",StringComparison.OrdinalIgnoreCase))continue;
                foreach(var cache in firefox?new[]{"cache2","startupCache"}:new[]{"Cache","Code Cache","GPUCache"})
                    if(Directory.Exists(Path.Combine(profile,cache)))yield return Path.Combine(profile,cache);
            }
        }
    }
    public sealed class SafeTemporaryFiles(string root,string pattern) : CleanupTargetBase(new FileSystemOperations())
    {
        public override string Id=>"automatic.temp."+pattern;
        public override string Name=>"Old temporary files ("+pattern+")";
        public override string Description=>"Temporary files older than seven days, excluding recovery and project folders.";
        public override string IconGlyph=>"";
        public override CleanupCategory Category=>CleanupCategory.WindowsSystem;
        public override SafetyLevel SafetyLevel=>SafetyLevel.Caution;
        protected override string FilePattern=>pattern;
        protected override TimeSpan? MinimumAge=>TimeSpan.FromDays(7);
        protected override IEnumerable<string> EnumerateCleanupRoots()=>[root];
        protected override IEnumerable<string> ExcludePathSubstrings=>["AutoRecover","AutoSave",@"\Recovery\",@"\.git\",@"\.vs\",@"\.vscode\",@"\Projects\",@"\source\",@"\repos\"];
        protected override bool AdditionalPolicy(string path){try{return new FileInfo(path).Length<=512L*1024*1024;}catch{return false;}}
    }
}
