using System.Collections.ObjectModel;
using System.Windows;
using Cleaner.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace Cleaner.App.ViewModels.Pages;

public sealed partial class CareHubViewModel : ObservableObject
{
    readonly AutomaticCleanupService cleanup=new();
    [ObservableProperty] bool _cleaning;
    [ObservableProperty] bool _windowsJobRunning;
    [ObservableProperty] string _cleanupStatus="Ready. Deletes old .tmp/.temp files and caches of closed browsers. Documents and projects are excluded.";
    [ObservableProperty] string _securityStatus="Open Windows Security to check your antivirus status.";
    [ObservableProperty] string _optimizationStatus="Windows chooses the correct operation for your system drive, including TRIM where appropriate.";
    [ObservableProperty] string _startupStatus="New startup entries are reported while QuietCare is running.";
    public ObservableCollection<string> Activity {get;}=[];
    public CareHubViewModel(){ }
    void Add(string line){Activity.Insert(0,DateTime.Now.ToString("HH:mm:ss")+"  "+line);while(Activity.Count>40)Activity.RemoveAt(Activity.Count-1);}
    [RelayCommand] public void BoostRam(){App.Monitor?.BoostRam();Add("RAM Boost requested. Results appear on the monitor and in Last Boost results.");}
    [RelayCommand] public void BoostResults()=>App.Monitor?.Reveal("Boost results");
    [RelayCommand] public async Task CleanSafeJunkAsync()
    {
        if(Cleaning)return;Cleaning=true;
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
        if(WindowsJobRunning)return;WindowsJobRunning=true;
        try
        {
            var result=await WindowsCareTools.RunAsync(job,new Progress<string>(line=>{if(job==WindowsCareJob.OptimizeSystemDrive)OptimizationStatus=line;else SecurityStatus=line;}));Add(result.Message);
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
