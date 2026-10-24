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
    [ObservableProperty,NotifyPropertyChangedFor(nameof(CareControlsReady))] bool _smartCareRunning;
    [ObservableProperty] string _smartCareStatus="One click chooses suitable actions from live PC checks. Windows may request administrator approval.";
    [ObservableProperty,NotifyPropertyChangedFor(nameof(CareControlsReady))] bool _cleaning;
    [ObservableProperty,NotifyPropertyChangedFor(nameof(CareControlsReady))] bool _windowsJobRunning;
    [ObservableProperty,NotifyPropertyChangedFor(nameof(CareControlsReady))] bool _boostRunning;
    public bool CareControlsReady=>!SmartCareRunning&&!Cleaning&&!WindowsJobRunning&&!BoostRunning;
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
        if(last is not null)SmartCareStatus="Last run "+last.FinishedUtc.ToLocalTime().ToString("g")+" · "+last.Summary;
    }
    public void ApplyCareStep(CareStep step)
    {
        void Apply(){for(int i=0;i<CareSteps.Count;i++)if(CareSteps[i].Key==step.Key){CareSteps[i]=new(step);return;}}
        if(Application.Current.Dispatcher.CheckAccess())Apply();else Application.Current.Dispatcher.Invoke(Apply);
    }
    [RelayCommand] public async Task StartSmartCareAsync()
    {
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
            smartBackend.SaveReport(report);SmartCareStatus=report.Summary;Add("Smart Care: "+report.Summary);
            foreach(var step in report.Steps)Add(step.Title+" · "+step.State+": "+step.Detail);
        }
        catch(OperationCanceledException){SmartCareStatus="Smart Care stopped before starting the next action.";}
        catch(Exception ex){SmartCareStatus="Smart Care stopped: "+ex.Message;Add(SmartCareStatus);}
        finally{tasks.Complete(task);careStop.Dispose();careStop=null;SmartCareRunning=false;}
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
        SecurityStatus=status.Available?$"Microsoft Defender: antivirus {(status.AntivirusEnabled?"enabled":"disabled")}; real-time protection {(status.RealTimeEnabled?"on":"off")}. Signatures: {status.SignatureUpdated}. Last quick scan: {status.LastQuickScan}.":status.Detail;
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
    public void OnStartupChanges(IReadOnlyList<StartupChange> changes){StartupStatus=changes.Count+" startup changes detected. Review Startup apps.";foreach(var change in changes)Add(change.Kind+": "+change.Entry.Name);}
}

public sealed class CareStepRow(CareStep step)
{
    public string Key=>step.Key;
    public string Title=>step.Title;
    public string Detail=>step.Detail;
    public string Status=>step.State.ToString();
    public string Accent=>step.State switch{CareState.Completed=>"#67E1B2",CareState.Running=>"#7EC6FF",CareState.Review=>"#FFC680",CareState.Failed=>"#FF9292",_=>"#9AAFC6"};
}
