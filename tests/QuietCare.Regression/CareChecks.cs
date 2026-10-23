using Cleaner.Core.Cleaners;
using Cleaner.Core.Services;
using System.Text.Json;

static class CareChecks
{
    public static async Task Run(string root,Action<bool,string> check)
    {
        string Folder(string name){var path=Path.Combine(root,"care",name);Directory.CreateDirectory(path);return path;}
        string Write(string folder,string name,bool old=true){var path=Path.Combine(folder,name);Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllText(path,"care fixture");if(old)File.SetLastWriteTimeUtc(path,DateTime.UtcNow.AddDays(-20));return path;}
        var savedAge=GlobalCleanupPolicy.MinimumAge;var savedExclusions=GlobalCleanupPolicy.ExcludeSubstrings;
        try
        {
            GlobalCleanupPolicy.MinimumAge=null;GlobalCleanupPolicy.ExcludeSubstrings=[];
            var heldRoot=Folder("held");Write(heldRoot,"held.tmp");var heldTarget=new AutomaticCleanupService.SafeTemporaryFiles(heldRoot,"*.tmp");
            var heldClean=await heldTarget.CleanAsync(await heldTarget.ScanAsync(),true);
            var heldId=heldClean.RecoveryIds.Single();
            var temp=Folder("temp");var old=Write(temp,"old.tmp");var recent=Write(temp,"recent.tmp",false);var text=Write(temp,"notes.txt");var project=Write(temp,@"Projects\active.tmp");var recovery=Write(temp,@"AutoRecover\unsaved.tmp");
            var result=await new AutomaticCleanupService().RunAsync(fixturePlan:[new AutomaticCleanupService.SafeTemporaryFiles(temp,"*.tmp")]);
            check(result.DeletedFiles==1&&result.DeletedBytes==12&&!File.Exists(old),"One-click cleanup actually deletes its eligible fixture and reports bytes");
            check(File.Exists(recent)&&File.Exists(text)&&File.Exists(project)&&File.Exists(recovery),"Automatic cleanup preserves recent, non-temp, project and recovery fixtures");
            check(RecoveryStore.List().Any(x=>x.Id==heldId),"Automatic cleanup never purges another operation's recovery receipt");
            var profiles=Folder("profiles");var stale=Write(profiles,@"Default\Cache\stale.cache");var fresh=Write(profiles,@"Default\Cache\fresh.cache",false);var session=Write(profiles,@"Default\Sessions\session.bin");var offline=Write(profiles,@"Default\Service Worker\CacheStorage\offline.bin");
            bool active=false;
            var cache=new AutomaticCleanupService.ClosedBrowserCache("fixture",profiles,false,()=>active);
            var scan=await cache.ScanAsync();
            check(scan.Paths.SequenceEqual([stale]),"Automatic browser plan enforces seven-day floor independently of advanced settings");
            check(!scan.Paths.Contains(session)&&!scan.Paths.Contains(offline)&&!scan.Paths.Contains(fresh),"Browser plan excludes sessions, offline storage and recent cache");
            active=true;await Task.Delay(150);
            var blocked=await cache.CleanAsync(scan,true);
            check(blocked.FilesDeleted==0&&File.Exists(stale),"Browser becoming active after scan blocks its cache cleanup");
            check(!cache.IsAvailable(),"Running browsers are unavailable to automatic cleanup");
            active=false;await Task.Delay(150);
            result=await new AutomaticCleanupService().RunAsync(fixturePlan:[cache]);
            check(result.DeletedFiles==1&&!File.Exists(stale)&&File.Exists(session)&&File.Exists(offline),"Closed-browser fixture cleanup removes only old disposable cache");
            using(var cts=new CancellationTokenSource()){cts.Cancel();bool cancelled=false;try{await new AutomaticCleanupService().RunAsync(ct:cts.Token,fixturePlan:[]);}catch(OperationCanceledException){cancelled=true;}check(cancelled,"Cancelled automatic cleanup never starts a scan");}
        }
        finally{GlobalCleanupPolicy.MinimumAge=savedAge;GlobalCleanupPolicy.ExcludeSubstrings=savedExclusions;}

        check(StartupApprovalPolicy.SubKey(AutostartSource.HklmRun32).EndsWith(@"StartupApproved\Run32"),"32-bit machine startup uses Run32 approval mapping");
        check(StartupApprovalPolicy.SubKey(AutostartSource.StartupFolderUser).EndsWith(@"StartupApproved\StartupFolder"),"Startup-folder toggles use Windows approval state");
        check(StartupApprovalPolicy.Encode(true).Length==12&&StartupApprovalPolicy.Encode(false).Length==12,"Startup approval encoding is exactly twelve bytes");
        check(StartupApprovalPolicy.IsEnabled(StartupApprovalPolicy.Encode(true))&&!StartupApprovalPolicy.IsEnabled(StartupApprovalPolicy.Encode(false)),"Startup approval enable/disable round trip");
        check(StartupApprovalPolicy.IsEnabled(null)&&StartupApprovalPolicy.IsEnabled([0x99]),"Unknown approval state is conservatively treated as enabled");
        var baseline=new StartupBaseline();var covered=new HashSet<AutostartSource>{AutostartSource.HkcuRun};
        var entry=new StartupSnapshot("key","Fixture","fixture.exe",true,AutostartSource.HkcuRun);
        check(StartupChangeDetector.Update(baseline,[entry],covered).Count==0,"First startup scan establishes a quiet baseline");
        check(StartupChangeDetector.Update(baseline,[entry],covered).Count==0,"Unchanged startup entries generate no alerts");
        var added=entry with{Key="new",Name="New fixture"};
        check(StartupChangeDetector.Update(baseline,[entry,added],covered).Single().Kind=="New startup item","A new startup item generates an alert");
        entry=entry with{Command="changed.exe"};
        check(StartupChangeDetector.Update(baseline,[entry,added],covered).Single().Kind=="Startup command changed","Changed startup commands generate an alert");
        entry=entry with{Enabled=false};StartupChangeDetector.Update(baseline,[entry],covered);
        entry=entry with{Enabled=true};
        check(StartupChangeDetector.Update(baseline,[entry],covered).Single().Kind=="Startup item enabled","Re-enabled startup entries generate an alert");
        baseline=JsonSerializer.Deserialize<StartupBaseline>(JsonSerializer.Serialize(baseline))!;
        check(StartupChangeDetector.Update(baseline,[entry with{Key="KEY"}],covered).Count==0,"Startup baseline remains case-insensitive after JSON reload");
        check(StartupChangeDetector.Update(baseline,[],covered).Count==0&&baseline.Entries.Count>=2,"An inaccessible or empty source does not erase the last-seen baseline");

        var unsigned=Write(Folder("unsigned"),"unsigned.exe");
        check(!MicrosoftSignature.IsTrusted(unsigned),"Unsigned executable fixtures fail Windows-tool trust verification");
        var optimize=WindowsCareTools.Resolve(WindowsCareJob.OptimizeSystemDrive);
        check(optimize.File==Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"defrag.exe")&&optimize.Arguments.EndsWith(" /O /U /V"),"Optimization uses the fixed Windows tool and media-appropriate operation");
        check(MicrosoftSignature.IsTrusted(optimize.File),"Installed Windows optimization tool has a trusted Microsoft signature");
        var quick=WindowsCareTools.Resolve(WindowsCareJob.DefenderQuickScan);var full=WindowsCareTools.Resolve(WindowsCareJob.DefenderFullScan);
        check(MicrosoftSignature.IsTrusted(quick.File)&&quick.Arguments=="-Scan -ScanType 1"&&full.Arguments=="-Scan -ScanType 2","Defender scan modes resolve to a trusted Microsoft scanner");
        bool invalid=false;try{WindowsCareTools.Resolve((WindowsCareJob)999);}catch(ArgumentOutOfRangeException){invalid=true;}
        check(invalid,"Unknown Windows job is rejected before launch");
        var first=new DiskScanner();var a=first.StartScan(Folder("temp"));var b=first.StartScan(Folder("unsigned"));await Task.WhenAll(a.Completion,b.Completion);
        check(a.Root.FileCount==4&&b.Root.FileCount==1,"Concurrent disk scans keep independent counts");
    }
}
