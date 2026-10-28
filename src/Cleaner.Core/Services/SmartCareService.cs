namespace Cleaner.Core.Services;

public enum CareState { Waiting, Running, Completed, Skipped, Review, Failed, Cancelled }
public sealed record CareStep(string Key,string Title,CareState State,string Detail);
public sealed record CareAction(CareState State,string Detail);
public sealed record CareAssessment(DateTimeOffset CheckedUtc,double? MemoryUsedPercent,ulong? AvailableBytes,
    double? CpuPercent,bool? OnBattery,DefenderStatus Defender,DateTimeOffset? LastOptimizationUtc);
public sealed record CareDecision(bool Run,string Reason,bool NeedsReview=false);
public sealed record SmartCareReport(DateTimeOffset StartedUtc,DateTimeOffset FinishedUtc,bool Cancelled,IReadOnlyList<CareStep> Steps)
{
    public int AttentionCount=>Steps.Count(s=>s.State is CareState.Review or CareState.Failed);
    public string Summary=>$"{(Cancelled?"Stopped":"Finished")}: {Steps.Count(s=>s.State==CareState.Completed)} completed, {Steps.Count(s=>s.State==CareState.Skipped)} skipped, {AttentionCount} need attention.";
}

// Local, explainable rules. No network AI, invented health score, service disabling or app removal.
public static class SmartCarePolicy
{
    public static CareDecision Memory(CareAssessment pc)
    {
        if(pc.MemoryUsedPercent is not double used||!double.IsFinite(used)||used is <0 or >100||pc.AvailableBytes is null)
            return new(false,"Memory readings are unavailable. Use RAM Boost to retry the checks.",true);
        return used>=65||pc.AvailableBytes<2UL*1024*1024*1024
            ?new(true,$"RAM is {used:F1}% used. Check idle apps and trim eligible resident pages.")
            :new(false,$"RAM is {used:F1}% used with at least 2 GB available. No memory trim is needed.");
    }
    public static CareDecision Security(CareAssessment pc)
    {
        if(!pc.Defender.Available)return new(false,pc.Defender.Detail,true);
        if(pc.Defender.RealtimeOffByLocalPolicy)return new(false,"Your local policy requests real-time protection off. Smart Care respects that choice and skips its automatic Defender scan; manual scans remain available.");
        if(!pc.Defender.AntivirusEnabled)return new(false,"Defender is not the enabled antivirus. Review your active provider in Windows Security.",true);
        var last=pc.Defender.LastQuickScanUtc;
        if(last.HasValue&&last<=pc.CheckedUtc&&pc.CheckedUtc-last<TimeSpan.FromDays(1))
            return new(false,"Defender reports a completed quick scan in the last 24 hours.");
        return new(true,"No recent completed Defender quick scan was verified. Run the Microsoft scanner.");
    }
    public static CareDecision Drive(CareAssessment pc)
    {
        if(pc.OnBattery is null)return new(false,"Power status is unavailable. Drive optimization is deferred.",true);
        if(pc.OnBattery==true)return new(false,"Running on battery. Defer drive optimization until connected to power.");
        if(pc.CpuPercent is not double cpu||!double.IsFinite(cpu)||cpu is <0 or >100)
            return new(false,"CPU load is unavailable. Drive optimization is deferred.",true);
        if(cpu>=60)return new(false,$"CPU load is {cpu:F0}%. Defer drive optimization while the PC is busy.");
        var last=pc.LastOptimizationUtc;
        if(last.HasValue&&last<=pc.CheckedUtc&&pc.CheckedUtc-last<TimeSpan.FromDays(7))
            return new(false,"QuietCare recorded successful Windows drive optimization in the last seven days.");
        return new(true,"No recent successful QuietCare optimization is recorded. Windows will choose the operation for this drive.");
    }
}

public interface ISmartCareBackend
{
    Task<CareAssessment> AssessAsync(CancellationToken ct);
    Task<CareAction> CleanAsync(IProgress<string> progress,CancellationToken ct);
    Task<CareAction> BoostAsync(IProgress<string> progress,CancellationToken ct);
    Task<CareAction> ReviewStartupAsync(IProgress<string> progress,CancellationToken ct);
    Task<CareAction> RunWindowsAsync(WindowsCareJob job,IProgress<string> progress,CancellationToken ct);
}

public sealed class SmartCareService(ISmartCareBackend backend)
{
    readonly SemaphoreSlim gate=new(1,1);
    public static IReadOnlyList<CareStep> InitialSteps()=>[
        new("assess","Check this PC",CareState.Waiting,"Read memory, CPU, power and Windows protection status."),
        new("cleanup","Clean old junk",CareState.Waiting,"Old temporary files and closed-browser disposable cache only."),
        new("memory","Reclaim idle memory",CareState.Waiting,"Choose whether a memory trim is useful from live readings."),
        new("startup","Review startup",CareState.Waiting,"Check for changes; keep needed apps and services intact."),
        new("security","Check for threats",CareState.Waiting,"Run Defender quick scan when its last scan is due or unknown."),
        new("drive","Optimize the system drive",CareState.Waiting,"Use Windows when due, on power, with CPU load below 60%.")];

    public async Task<SmartCareReport> RunAsync(IProgress<CareStep>? progress=null,CancellationToken ct=default)
    {
        if(!await gate.WaitAsync(0,ct))throw new InvalidOperationException("Smart Care is already running.");
        var started=DateTimeOffset.UtcNow;var steps=InitialSteps().ToArray();CareAssessment? pc=null;bool cancelled=false;
        void Update(int index,CareState state,string detail){steps[index]=steps[index] with{State=state,Detail=detail};progress?.Report(steps[index]);}
        try
        {
            for(int i=0;i<steps.Length;i++)
            {
                if(ct.IsCancellationRequested){cancelled=true;break;}
                Update(i,CareState.Running,steps[i].Detail);
                try
                {
                    int stepIndex=i;
                    var output=new InlineProgress(line=>{if(steps[stepIndex].State==CareState.Running)Update(stepIndex,CareState.Running,line);});
                    if(i==0)
                    {
                        pc=await backend.AssessAsync(ct);
                        bool review=!pc.Defender.Available||!pc.Defender.AntivirusEnabled||!pc.Defender.RealTimeEnabled;
                        Update(i,review?CareState.Review:CareState.Completed,
                            $"RAM: {(pc.MemoryUsedPercent.HasValue?pc.MemoryUsedPercent.Value.ToString("F1")+"% used":"unavailable")}. CPU: {(pc.CpuPercent.HasValue?pc.CpuPercent.Value.ToString("F0")+"%":"unavailable")}. "
                            +(review?"Protection needs review in Windows Security. "+pc.Defender.Detail:"Defender antivirus and real-time protection are on."));
                        continue;
                    }
                    CareDecision? decision=null;
                    if(i is 2 or 4 or 5)
                    {
                        if(pc is null){Update(i,CareState.Review,"PC assessment failed; this action was not started.");continue;}
                        // Re-read immediately before memory/Windows actions: the PC may have changed during cleanup or a scan.
                        pc=await backend.AssessAsync(ct);
                        decision=i==2?SmartCarePolicy.Memory(pc):i==4?SmartCarePolicy.Security(pc):SmartCarePolicy.Drive(pc);
                        if(!decision.Run){Update(i,decision.NeedsReview?CareState.Review:CareState.Skipped,decision.Reason);continue;}
                        Update(i,CareState.Running,decision.Reason);
                    }
                    CareAction result=i switch
                    {
                        1=>await backend.CleanAsync(output,ct),
                        2=>await backend.BoostAsync(output,ct),
                        3=>await backend.ReviewStartupAsync(output,ct),
                        4=>await backend.RunWindowsAsync(WindowsCareJob.DefenderQuickScan,output,ct),
                        5=>await backend.RunWindowsAsync(WindowsCareJob.OptimizeSystemDrive,output,ct),
                        _=>throw new InvalidOperationException("Unknown care step.")
                    };
                    if(result.State is CareState.Waiting or CareState.Running or CareState.Cancelled)
                        throw new InvalidOperationException("The care action did not return a final outcome.");
                    Update(i,result.State,result.Detail);
                }
                catch(OperationCanceledException) when(ct.IsCancellationRequested)
                {Update(i,CareState.Cancelled,"Stopped. Remaining actions were not started; recovery files are preserved.");cancelled=true;break;}
                catch(Exception ex){Update(i,CareState.Failed,"Did not complete: "+ex.Message);}
            }
            cancelled|=ct.IsCancellationRequested;
            foreach(var step in steps.Select((value,index)=>(value,index)).Where(x=>x.value.State==CareState.Waiting))
                Update(step.index,CareState.Cancelled,"Not started because Stop was requested.");
            return new(started,DateTimeOffset.UtcNow,cancelled,steps);
        }
        finally{gate.Release();}
    }
    // Avoid queued SynchronizationContext progress arriving after a step is already complete.
    sealed class InlineProgress(Action<string> report):IProgress<string>{public void Report(string value)=>report(value);}
}
