using Cleaner.Core.Services;
using System.Text.Json;

static class SmartCareChecks
{
    public static async Task Run(Action<bool,string> check)
    {
        var now=DateTimeOffset.UtcNow;
        var defender=new DefenderStatus(true,true,true,"Fixture","Fixture","Fixture"){LastQuickScanUtc=now.AddDays(-2)};
        var pc=new CareAssessment(now,72,3UL*1024*1024*1024,12,false,defender,now.AddDays(-8));
        check(SmartCarePolicy.Memory(pc).Run,"Automatic memory care chooses a trim when RAM use is at least 65 percent");
        check(!SmartCarePolicy.Memory(pc with{MemoryUsedPercent=50}).Run,"Healthy memory readings do not trigger unnecessary automatic trimming");
        check(SmartCarePolicy.Memory(pc with{MemoryUsedPercent=50,AvailableBytes=1024}).Run,"Low available RAM triggers a memory check independently of percentage");
        check(SmartCarePolicy.Memory(pc with{MemoryUsedPercent=double.NaN}).NeedsReview,"Invalid memory telemetry cannot authorize an automatic trim");
        check(SmartCarePolicy.Security(pc).Run&&!SmartCarePolicy.Security(pc with{Defender=defender with{LastQuickScanUtc=now.AddHours(-2)}}).Run,"Defender quick scan is selected only when due or last completion is unknown");
        check(!SmartCarePolicy.Security(pc with{Defender=defender with{AntivirusEnabled=false}}).Run,"Inactive Defender is not selected as the antivirus provider");
        check(SmartCarePolicy.Security(pc with{Defender=defender with{LastQuickScanUtc=now.AddDays(1)}}).Run,"A future scan timestamp cannot suppress a needed check");
        check(SmartCarePolicy.Drive(pc).Run&&!SmartCarePolicy.Drive(pc with{OnBattery=true}).Run,"Drive care is deferred on battery");
        check(!SmartCarePolicy.Drive(pc with{CpuPercent=60}).Run&&SmartCarePolicy.Drive(pc with{CpuPercent=null}).NeedsReview,"Busy or unknown CPU load does not authorize drive optimization");
        check(!SmartCarePolicy.Drive(pc with{LastOptimizationUtc=now.AddDays(-1)}).Run,"Recent successful optimization prevents repeated drive work");

        var fake=new FakeCare(pc);var changes=new List<CareStep>();
        var report=await new SmartCareService(fake).RunAsync(new CaptureProgress(changes));
        check(fake.Actions.SequenceEqual(["cleanup","memory","startup","security","drive"])&&report.Steps.All(s=>s.State==CareState.Completed),"One click runs the five real action interfaces sequentially with final outcomes");
        check(fake.Assessments==4&&changes.Any(x=>x.State==CareState.Running)&&changes.Count(x=>x.State==CareState.Completed)==6,"Automatic care rechecks live conditions before memory, security and drive actions and reports step transitions");
        fake.LastCleanupProgress!.Report("late queued progress");
        check(report.Steps.All(s=>s.State==CareState.Completed),"Late progress cannot turn a completed action back into a running step");
        check(JsonSerializer.Deserialize<SmartCareReport>(JsonSerializer.Serialize(report))!.Steps.Count==6,"Saved automatic care report retains all step results");

        fake=new FakeCare(pc){FailCleanup=true};report=await new SmartCareService(fake).RunAsync();
        check(report.AttentionCount==1&&report.Steps[1].State==CareState.Failed&&fake.Actions.Contains("security"),"An action failure stays visible and does not prevent independent care checks");
        fake=new FakeCare(pc){HighCpuAtDrive=true};report=await new SmartCareService(fake).RunAsync();
        check(!fake.Actions.Contains("drive")&&report.Steps[5].State==CareState.Skipped,"A later rise in CPU load prevents drive work using fresh readings");
        fake=new FakeCare(pc with{Defender=defender with{Available=false,AntivirusEnabled=false}});report=await new SmartCareService(fake).RunAsync();
        check(!fake.Actions.Contains("security")&&report.AttentionCount>=2,"Unavailable antivirus status cannot become a false all-clear or launch a scanner");

        using(var stop=new CancellationTokenSource())
        {
            fake=new FakeCare(pc){OnWindows=()=>stop.Cancel()};report=await new SmartCareService(fake).RunAsync(ct:stop.Token);
            check(report.Cancelled&&report.Steps[4].State==CareState.Completed&&report.Steps[5].State==CareState.Cancelled&&!fake.Actions.Contains("drive"),"Stop during a Windows job records its result and prevents the remaining drive action");
        }
        using(var stop=new CancellationTokenSource())
        {
            stop.Cancel();fake=new FakeCare(pc);bool cancelled=false;
            try{await new SmartCareService(fake).RunAsync(ct:stop.Token);}catch(OperationCanceledException){cancelled=true;}
            check(cancelled&&fake.Assessments==0&&fake.Actions.Count==0,"A pre-cancelled care request performs no assessment or action");
        }
        fake=new FakeCare(pc){HoldCleanup=new(TaskCreationOptions.RunContinuationsAsynchronously)};
        var service=new SmartCareService(fake);var first=service.RunAsync();
        bool rejected=false;try{await service.RunAsync();}catch(InvalidOperationException){rejected=true;}
        fake.HoldCleanup.SetResult();await first;
        check(rejected&&fake.Actions.Count(x=>x=="cleanup")==1,"Concurrent automatic care requests cannot duplicate cleanup or Windows jobs");
    }
    sealed class CaptureProgress(List<CareStep> steps):IProgress<CareStep>{public void Report(CareStep step)=>steps.Add(step);}
    sealed class FakeCare(CareAssessment pc):ISmartCareBackend
    {
        public List<string> Actions=[];public int Assessments;public bool FailCleanup,HighCpuAtDrive;
        public TaskCompletionSource? HoldCleanup;public Action? OnWindows;public IProgress<string>? LastCleanupProgress;
        public Task<CareAssessment> AssessAsync(CancellationToken ct){ct.ThrowIfCancellationRequested();Assessments++;return Task.FromResult(HighCpuAtDrive&&Assessments==4?pc with{CpuPercent=90}:pc);}
        public async Task<CareAction> CleanAsync(IProgress<string> progress,CancellationToken ct)
        {Actions.Add("cleanup");LastCleanupProgress=progress;if(HoldCleanup is not null)await HoldCleanup.Task;if(FailCleanup)throw new IOException("Fixture cleanup error");return new(CareState.Completed,"Fixture old junk removed");}
        public Task<CareAction> BoostAsync(IProgress<string> progress,CancellationToken ct){Actions.Add("memory");return Task.FromResult(new CareAction(CareState.Completed,"Fixture measured memory gain"));}
        public Task<CareAction> ReviewStartupAsync(IProgress<string> progress,CancellationToken ct){Actions.Add("startup");return Task.FromResult(new CareAction(CareState.Completed,"Fixture startup checked"));}
        public Task<CareAction> RunWindowsAsync(WindowsCareJob job,IProgress<string> progress,CancellationToken ct)
        {Actions.Add(job==WindowsCareJob.OptimizeSystemDrive?"drive":"security");OnWindows?.Invoke();return Task.FromResult(new CareAction(CareState.Completed,"Fixture Windows result 0"));}
    }
}
