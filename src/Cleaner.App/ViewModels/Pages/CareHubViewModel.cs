using System.Collections.ObjectModel;
using System.Windows;
using Cleaner.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cleaner.App.Services;
namespace Cleaner.App.ViewModels.Pages;

public sealed partial class CareHubViewModel : ObservableObject
{
    readonly AutomaticCleanupService cleanup=new();
    readonly SmartCareBackend smartBackend;
    readonly SmartCareService smartCare;
    readonly RunningTaskRegistry tasks;
    CancellationTokenSource? careStop;
    DateTime lastReading;
    [ObservableProperty] string _cpuReading="—";
    [ObservableProperty] string _gpuReading="—";
    [ObservableProperty] string _ramReading="—";
    [ObservableProperty] double _cpuValue;
    [ObservableProperty] double _gpuValue;
    [ObservableProperty] double _ramValue;
    [ObservableProperty] string _availableRam="Waiting for a live reading";
    [ObservableProperty] string _readingTime="Waiting for a live reading…";
    [ObservableProperty] string _protectionBadge="Not checked";
    [ObservableProperty] string _protectionDetail="Check your Windows antivirus status.";
    [ObservableProperty] string _defenderControlStatus="Refresh status to read Windows policy and scan CPU settings.";
    [ObservableProperty] string _scanCpuSettings="Reading scan preferences…";
    [ObservableProperty] string _realtimePolicyInfo="Local policy has not been checked.";
    [ObservableProperty] bool _canChangeDefender;
    [ObservableProperty] bool _canKeepRealtimeOff;
    [ObservableProperty] bool _hasScanCpuBackup;
    [ObservableProperty] string _diskAvailable="—";
    [ObservableProperty] string _diskDetail="System drive";
    [ObservableProperty] double _diskUsed;
    [ObservableProperty] string _stageTitle="Ready to care for your PC";
    [ObservableProperty] string _stageDetail="Checks memory, old junk, protection, startup and drive maintenance.";
    [ObservableProperty] string _careOutcome="Recommended care";
    [ObservableProperty] string _startupBadge="Watching for changes";
    public string CareButtonLabel=>SmartCareRunning?"Working":"Start care";
    partial void OnSmartCareRunningChanged(bool value)=>OnPropertyChanged(nameof(CareButtonLabel));
    [ObservableProperty,NotifyPropertyChangedFor(nameof(CareControlsReady))] bool _smartCareRunning;
    [ObservableProperty] string _smartCareStatus="One click chooses suitable actions from live PC checks. Windows may request administrator approval.";
    [ObservableProperty,NotifyPropertyChangedFor(nameof(CareControlsReady))] bool _cleaning;
    [ObservableProperty,NotifyPropertyChangedFor(nameof(CareControlsReady))] bool _windowsJobRunning;
    [ObservableProperty,NotifyPropertyChangedFor(nameof(CareControlsReady))] bool _boostRunning;
    [ObservableProperty,NotifyPropertyChangedFor(nameof(CareControlsReady))] bool _focusActive;
    public bool CareControlsReady=>!FocusActive&&!SmartCareRunning&&!Cleaning&&!WindowsJobRunning&&!BoostRunning;
    [ObservableProperty] string _boostStatus="Expanded idle-app check: up to 24 apps, or 32 under high memory pressure. Protects active work and measures again after three seconds.";
    [ObservableProperty] string _cleanupStatus="Ready. Deletes old .tmp/.temp files and caches of closed browsers. Documents and projects are excluded.";
    [ObservableProperty] string _securityStatus="Open Windows Security to check your antivirus status.";
    [ObservableProperty] string _optimizationStatus="Windows chooses the correct operation for your system drive, including TRIM where appropriate.";
    [ObservableProperty] string _startupStatus="New startup entries are reported while QuietCare is running.";
    public ObservableCollection<string> Activity {get;}=[];
    public ObservableCollection<CareStepRow> CareSteps {get;}=[];
    public CareHubViewModel(AppDataService store,RunningTaskRegistry tasks)
    {
        this.tasks=tasks;smartBackend=new(store,cleanup);smartCare=new(smartBackend);
        var last=store.Load("smart-care.json",()=>new CareHistory()).LastReport;
        foreach(var step in last?.Steps??SmartCareService.InitialSteps())CareSteps.Add(new(step));
        if(last is not null){SmartCareStatus="Last run "+last.FinishedUtc.ToLocalTime().ToString("g")+" · "+last.Summary;StageTitle=last.Cancelled?"Previous care was stopped":"Previous care result";StageDetail=last.Summary;CareOutcome="Last care "+last.FinishedUtc.ToLocalTime().ToString("g");}
    }
    public void ApplyCareStep(CareStep step)
    {
        void Apply(){if(step.State==CareState.Running){StageTitle=step.Title;StageDetail=step.Detail;}for(int i=0;i<CareSteps.Count;i++)if(CareSteps[i].Key==step.Key){CareSteps[i]=new(step);return;}}
        if(Application.Current.Dispatcher.CheckAccess())Apply();else Application.Current.Dispatcher.Invoke(Apply);
    }
    [RelayCommand] public async Task StartSmartCareAsync()
    {
        if(FocusActive){SmartCareStatus="Focus mode is active. End the focus session before starting maintenance.";return;}
        if(!CareControlsReady){SmartCareStatus="Another care action is running. Wait for its result before starting Smart Care.";return;}
        if(tasks.Count>0){SmartCareStatus="Wait for the current file/system task to finish before starting Smart Care.";return;}
        SmartCareRunning=true;careStop=new();
        foreach(var step in SmartCareService.InitialSteps())ApplyCareStep(step);
        var task=tasks.Start("Smart Care","Automatic care",careStop,true);
        SmartCareStatus="Checking this PC and choosing recommended actions…";
        try
        {
            var report=await smartCare.RunAsync(new CareProgress(step=>
            {
                ApplyCareStep(step);SmartCareStatus=step.Title+" · "+step.Detail;task.StatusText=SmartCareStatus;
                if(step.Key=="cleanup")CleanupStatus=step.Detail;
                if(step.Key=="security")SecurityStatus=step.Detail;
                if(step.Key=="drive")OptimizationStatus=step.Detail;
                if(step.Key=="startup")StartupStatus=step.Detail;
            }),careStop.Token);
            smartBackend.SaveReport(report);SmartCareStatus=report.Summary;CareOutcome=report.AttentionCount>0?"Review recommended":"Care completed";StageTitle=report.Cancelled?"Care stopped":report.AttentionCount>0?"Some items need your attention":"Your care session is complete";StageDetail=report.Summary;Add("Smart Care: "+report.Summary);
            foreach(var step in report.Steps)Add(step.Title+" · "+step.State+": "+step.Detail);
        }
        catch(OperationCanceledException){SmartCareStatus="Smart Care stopped before starting the next action.";}
        catch(Exception ex){SmartCareStatus="Smart Care stopped: "+ex.Message;Add(SmartCareStatus);}
        finally{tasks.Complete(task);careStop.Dispose();careStop=null;SmartCareRunning=false;}
    }
    public void OnSnapshot(QuietCare.Snapshot value)
    {
        if(DateTime.UtcNow-lastReading<TimeSpan.FromSeconds(1))return;lastReading=DateTime.UtcNow;
        CpuReading=value.Cpu.HasValue?value.Cpu.Value.ToString("F0")+"%":"—";GpuReading=value.Gpu.HasValue?value.Gpu.Value.ToString("F0")+"%":"—";
        RamReading=value.Total>0?value.Ram.ToString("F0")+"%":"—";CpuValue=value.Cpu??0;GpuValue=value.Gpu??0;RamValue=value.Ram;
        AvailableRam=value.Total>0?QuietCare.Format.Bytes(value.Available)+" available of "+QuietCare.Format.Bytes(value.Total):"Memory reading unavailable";
        ReadingTime="Last reading "+value.Time.ToString("HH:mm:ss");
    }
    [RelayCommand] public async Task RefreshReadingsAsync()
    {
        if(App.Monitor?.Latest is {} snapshot){lastReading=default;OnSnapshot(snapshot);return;}
        var reading=await Task.Run(async()=>{using var telemetry=new QuietCare.Telemetry();telemetry.Sample(false);await Task.Delay(250);return telemetry.Sample(false);});lastReading=default;OnSnapshot(reading);
    }
    [RelayCommand] public async Task RefreshStorageAsync()
    {
        try{var stats=await Task.Run(()=>{var root=System.IO.Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows))!;var drive=new System.IO.DriveInfo(root);return(root,drive.AvailableFreeSpace,drive.TotalSize);});DiskAvailable=Cleaner.Core.Utils.ByteFormatter.Format(stats.AvailableFreeSpace)+" free";DiskDetail=stats.root+"  ·  "+Cleaner.Core.Utils.ByteFormatter.Format(stats.TotalSize)+" total";DiskUsed=100.0*(1-(double)stats.AvailableFreeSpace/stats.TotalSize);}
        catch(Exception ex){DiskAvailable="Unavailable";DiskDetail=ex.Message;}
    }
    [RelayCommand] public void NavigateTool(string key)
    {
        Type? page=key switch{"focus"=>typeof(Cleaner.App.Views.Pages.FocusPage),"memory"=>typeof(Cleaner.App.Views.Pages.MemoryPage),"protection"=>typeof(Cleaner.App.Views.Pages.ProtectionPage),"storage"=>typeof(Cleaner.App.Views.Pages.StoragePage),"startup"=>typeof(Cleaner.App.Views.Pages.AutostartPage),"system"=>typeof(Cleaner.App.Views.Pages.SystemCleanerPage),"developer"=>typeof(Cleaner.App.Views.Pages.DeveloperCleanerPage),"disk"=>typeof(Cleaner.App.Views.Pages.DiskAnalyzerPage),"duplicates"=>typeof(Cleaner.App.Views.Pages.DuplicatesPage),"large"=>typeof(Cleaner.App.Views.Pages.LargeFilesPage),"logs"=>typeof(Cleaner.App.Views.Pages.LogFinderPage),"compare"=>typeof(Cleaner.App.Views.Pages.FolderComparePage),"orphan"=>typeof(Cleaner.App.Views.Pages.OrphanUserDataPage),"recovery"=>typeof(Cleaner.App.Views.Pages.CleanupHistoryPage),"services"=>typeof(Cleaner.App.Views.Pages.ServicesPage),"apps"=>typeof(Cleaner.App.Views.Pages.InstalledProgramsPage),"registry"=>typeof(Cleaner.App.Views.Pages.RegistryCleanerPage),"maintenance"=>typeof(Cleaner.App.Views.Pages.SystemMaintenancePage),_=>null};if(page is not null)App.Navigate(page);
    }
    [RelayCommand] public void StopSmartCare()
    {
        careStop?.Cancel();SmartCareStatus="Stop requested. A Windows scan or optimization already started will finish safely; remaining steps will not start.";
    }
    sealed class CareProgress(Action<CareStep> report):IProgress<CareStep>
    {
        public void Report(CareStep value)
        {
            if(Application.Current.Dispatcher.CheckAccess())report(value);else Application.Current.Dispatcher.Invoke(()=>report(value));
        }
    }
    void Add(string line){Activity.Insert(0,DateTime.Now.ToString("HH:mm:ss")+"  "+line);while(Activity.Count>40)Activity.RemoveAt(Activity.Count-1);}
    [RelayCommand] public void BoostRam(){if(!CareControlsReady)return;if(App.Monitor is null){BoostStatus="The monitor controller is unavailable. Restart QuietCare to retry.";return;}App.Monitor.BoostRam();}
    public void OnBoostStatus(string text,bool busy){bool completed=BoostRunning&&!busy;BoostStatus=text;BoostRunning=busy;if(completed)Add(text);}
    [RelayCommand] public void BoostResults()=>App.Monitor?.Reveal("Boost results");
    [RelayCommand] public async Task CleanSafeJunkAsync()
    {
        if(!CareControlsReady)return;Cleaning=true;
        try
        {
            var result=await cleanup.RunAsync(new Progress<string>(text=>CleanupStatus=text));
            CleanupStatus=$"Deleted {result.DeletedFiles} old junk files ({Cleaner.Core.Utils.ByteFormatter.Format(result.DeletedBytes)}); {result.PreservedFiles} preserved/skipped.";
            Add(CleanupStatus);foreach(var detail in result.Details)Add(detail);
        }
        catch(OperationCanceledException){CleanupStatus="Cleanup cancelled; any files still in Recovery are preserved.";}
        catch(Exception ex){CleanupStatus="Cleanup stopped: "+ex.Message;Add(CleanupStatus);}
        finally{Cleaning=false;}
    }
    [RelayCommand] public async Task RefreshProtectionAsync()
    {
        SecurityStatus="Reading Windows antivirus status…";
        var status=await WindowsCareTools.ReadDefenderStatusAsync();
        ProtectionBadge=!status.Available?"Status unavailable":!status.AntivirusEnabled?"Check antivirus provider":!status.RealTimeEnabled?"Protection needs review":"Protection on";
        ProtectionDetail=status.Available?"Microsoft Defender · real-time protection "+(status.RealTimeEnabled?"on":"off"):status.Detail;
        SecurityStatus=status.Available?$"Microsoft Defender: antivirus {(status.AntivirusEnabled?"enabled":"disabled")}; real-time protection {(status.RealTimeEnabled?"on":"off")}. Signatures: {status.SignatureUpdated}. Last quick scan: {status.LastQuickScan}.":status.Detail;
        var control=await Task.Run(()=>new WindowsDefenderControlRuntime().Read());
        CanChangeDefender=control.Available&&!control.ManagedDevice;
        CanKeepRealtimeOff=CanChangeDefender&&control.PolicySupported&&!control.TamperProtected;
        HasScanCpuBackup=control.OwnScanSettings;
        ScanCpuSettings=control.Available?$"Scan CPU target: {control.ScanCpuTarget}% average · Low priority: {(control.LowCpuPriority?"on":"off")} · Idle-scan throttling: {(control.UnthrottledIdleScans?"off":"on")}":control.Detail;
        RealtimePolicyInfo=control.Available?$"Real-time protection: {(control.RealTimeEnabled?"ON":"OFF")} · Local off policy: {(control.RealtimePolicy==1?"requested":"not requested")} · Tamper Protection: {(control.TamperProtected?"on":"off")} · {control.Detail}":control.Detail;
        if(control.Available&&!control.RealTimeEnabled)ProtectionBadge="Real-time scanning off";
        if(control.ManagedDevice&&control.Available)DefenderControlStatus="This device is managed. Protection changes must use your administrator's management policy.";
        else if(control.TamperProtected)DefenderControlStatus="Tamper Protection is on. The off-policy action is unavailable; QuietCare will not bypass it.";
        else DefenderControlStatus="Changes run only when you click, approve Windows and confirm the action. Restore is available for settings saved by QuietCare.";
    }
    [RelayCommand] public Task KeepRealtimeOffAsync()=>ChangeDefender("keep-off");
    [RelayCommand] public Task RestoreRealtimeAsync()=>ChangeDefender("restore-on");
    [RelayCommand] public Task LowerScanCpuAsync()=>ChangeDefender("scan-low");
    [RelayCommand] public Task RestoreScanCpuAsync()=>ChangeDefender("scan-restore");
    async Task ChangeDefender(string action)
    {
        if(!CareControlsReady||tasks.Count>0){DefenderControlStatus="Wait for the current care/file task to finish before changing protection preferences.";return;}
        WindowsJobRunning=true;DefenderControlStatus="Waiting for Windows administrator approval and your confirmation…";
        try
        {
            int code=await DefenderControlEntry.RequestAsync(action);
            await RefreshProtectionAsync();
            DefenderControlStatus=code==0?"Windows verified the requested change. Current settings are shown above.":code==1223?"The request was cancelled. No new action was started.":"Windows did not verify this request (code "+code+"). Review the current state and Windows result.";
            Add(DefenderControlStatus);
        }
        catch(System.ComponentModel.Win32Exception ex) when(ex.NativeErrorCode==1223){DefenderControlStatus="Windows administrator approval was cancelled. No settings were changed.";}
        catch(Exception ex){DefenderControlStatus="Protection change did not finish: "+ex.Message;}
        finally{WindowsJobRunning=false;}
    }
    [RelayCommand] public Task QuickVirusScanAsync()=>RunWindows(WindowsCareJob.DefenderQuickScan);
    [RelayCommand] public Task FullVirusScanAsync()=>RunWindows(WindowsCareJob.DefenderFullScan);
    [RelayCommand] public Task OptimizeDiskAsync()=>RunWindows(WindowsCareJob.OptimizeSystemDrive);
    async Task RunWindows(WindowsCareJob job)
    {
        if(!CareControlsReady)return;WindowsJobRunning=true;
        try
        {
            var result=await WindowsCareTools.RunAsync(job,new Progress<string>(line=>{if(job==WindowsCareJob.OptimizeSystemDrive)OptimizationStatus=line;else SecurityStatus=line;}));
            if(job==WindowsCareJob.OptimizeSystemDrive){OptimizationStatus=result.Message;if(result.ExitCode==0)smartBackend.RecordOptimization();}else SecurityStatus=result.Message;
            Add(result.Message);
        }
        catch(Exception ex)
        {
            var message="Windows operation did not complete: "+ex.Message;
            if(job==WindowsCareJob.OptimizeSystemDrive)OptimizationStatus=message;else SecurityStatus=message;Add(message);
        }
        finally{WindowsJobRunning=false;}
    }
    [RelayCommand] public void OpenSecurity()=>WindowsCareTools.OpenSecurity();
    [RelayCommand] public void OpenOptimization()=>WindowsCareTools.OpenOptimization();
    [RelayCommand] public void ManageStartup()=>App.Navigate(typeof(Cleaner.App.Views.Pages.AutostartPage));
    [RelayCommand] public void ManageApps()=>App.Navigate(typeof(Cleaner.App.Views.Pages.InstalledProgramsPage));
    [RelayCommand] public void ManageServices()=>App.Navigate(typeof(Cleaner.App.Views.Pages.ServicesPage));
    [RelayCommand] public void MonitorSettings()=>App.Monitor?.Reveal("Settings");
    public void OnStartupChanges(IReadOnlyList<StartupChange> changes){StartupBadge=changes.Count+" changes to review";StartupStatus=changes.Count+" startup changes detected. Review Startup apps.";foreach(var change in changes)Add(change.Kind+": "+change.Entry.Name);}
}

public sealed class CareStepRow(CareStep step)
{
    public string Key=>step.Key;
    public string Title=>step.Title;
    public string Detail=>step.Detail;
    public string Status=>step.State.ToString();
    public string Marker=>step.State switch{CareState.Completed=>"✓",CareState.Running=>"●",CareState.Review=>"!",CareState.Failed=>"!",CareState.Skipped=>"–",_=>"○"};
    public string Accent=>step.State switch{CareState.Completed=>"#67E1B2",CareState.Running=>"#7EC6FF",CareState.Review=>"#FFC680",CareState.Failed=>"#FF9292",_=>"#9AAFC6"};
}
