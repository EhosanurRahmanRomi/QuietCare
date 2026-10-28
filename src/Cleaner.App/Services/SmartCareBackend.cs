using System.IO;
using Cleaner.Core.Services;
namespace Cleaner.App.Services;

public sealed class CareHistory
{
    public DateTimeOffset? LastOptimizationUtc {get;set;}
    public SmartCareReport? LastReport {get;set;}
}

public sealed class SmartCareBackend(AppDataService store,AutomaticCleanupService cleanup):ISmartCareBackend
{
    public async Task<CareAssessment> AssessAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var statusTask=WindowsCareTools.ReadDefenderStatusAsync();
        var snapshot=await Task.Run(async ()=>
        {
            using var telemetry=new QuietCare.Telemetry();telemetry.Sample(false);
            await Task.Delay(250,ct);return telemetry.Sample(false);
        },ct);
        var defender=await statusTask;ct.ThrowIfCancellationRequested();
        var power=System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus;
        bool? battery=power==System.Windows.Forms.PowerLineStatus.Unknown?null:power==System.Windows.Forms.PowerLineStatus.Offline;
        return new(DateTimeOffset.UtcNow,snapshot.Total==0?null:snapshot.Ram,snapshot.Total==0?null:snapshot.Available,
            snapshot.Cpu,battery,defender,store.Load("smart-care.json",()=>new CareHistory()).LastOptimizationUtc);
    }
    public async Task<CareAction> CleanAsync(IProgress<string> progress,CancellationToken ct)
    {
        var result=await cleanup.RunAsync(progress,ct);
        return new(result.PreservedFiles>0?CareState.Review:CareState.Completed,
            $"Deleted {result.DeletedFiles} eligible old junk files ({Cleaner.Core.Utils.ByteFormatter.Format(result.DeletedBytes)}). {result.PreservedFiles} preserved/skipped. Active browser data, documents and projects were excluded.");
    }
    public async Task<CareAction> BoostAsync(IProgress<string> progress,CancellationToken ct)
    {
        if(App.Monitor is null)return new(CareState.Failed,"The monitor controller is unavailable. Restart QuietCare to retry.");
        if(App.Monitor.BoostReadyInSeconds>0)return new(CareState.Skipped,"RAM Boost ran recently. Cooldown: "+App.Monitor.BoostReadyInSeconds+" seconds. No repeat trim was made.");
        var result=await App.Monitor.BoostRamAsync(ct,progress);
        return new(!result.MediaVerified?CareState.Review:result.Trimmed==0?CareState.Skipped:
            result.AvailableDelta<=0?CareState.Review:CareState.Completed,result.Summary);
    }
    public async Task<CareAction> ReviewStartupAsync(IProgress<string> progress,CancellationToken ct)
    {
        if(App.StartupWatch is null)return new(CareState.Review,"Startup watch is unavailable. Open Startup apps to review entries.");
        progress.Report("Checking startup registry, folders, scheduled tasks and automatic services…");
        return new(CareState.Completed,await App.StartupWatch.RefreshNowAsync(ct));
    }
    public async Task<CareAction> RunWindowsAsync(WindowsCareJob job,IProgress<string> progress,CancellationToken ct)
    {
        var result=await WindowsCareTools.RunAsync(job,progress,ct);
        if(job==WindowsCareJob.OptimizeSystemDrive&&result.ExitCode==0)RecordOptimization();
        return new(result.ExitCode==0?CareState.Completed:CareState.Review,result.Message);
    }
    public void RecordOptimization()
    {
        var history=store.Load("smart-care.json",()=>new CareHistory());
        history.LastOptimizationUtc=DateTimeOffset.UtcNow;store.Save("smart-care.json",history);
    }
    public void SaveReport(SmartCareReport report)
    {
        var history=store.Load("smart-care.json",()=>new CareHistory());history.LastReport=report;store.Save("smart-care.json",history);
    }
}
