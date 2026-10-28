using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using Cleaner.Core.Services;

namespace Cleaner.App.Services;

internal static class DefenderControlEntry
{
    static readonly IReadOnlyDictionary<string,DefenderControlAction> Actions=new Dictionary<string,DefenderControlAction>(StringComparer.Ordinal)
    {
        ["keep-off"]=DefenderControlAction.KeepRealtimeOff,["restore-on"]=DefenderControlAction.RestoreRealtime,
        ["scan-low"]=DefenderControlAction.LowerScanCpu,["scan-restore"]=DefenderControlAction.RestoreScanCpu
    };
    public static bool IsAction(string value)=>Actions.ContainsKey(value);
    public static int Execute(string value)
    {
        if(!Actions.TryGetValue(value,out var action))return 64;
        using var identity=WindowsIdentity.GetCurrent();
        if(!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
        {MessageBox.Show("This explicit settings action requires Windows administrator approval.","QuietCare protection controls");return 5;}
        using var gate=new Mutex(false,@"Global\QuietCare.DefenderControls");
        bool acquired;
        try{acquired=gate.WaitOne(0);}catch(AbandonedMutexException){acquired=true;}
        if(!acquired){MessageBox.Show("Another QuietCare protection change is already running.","QuietCare protection controls");return 170;}
        try
        {
            string text=action switch
            {
                DefenderControlAction.KeepRealtimeOff=>"Keep real-time file scanning OFF using this PC's local Windows policy?\n\nFiles you open or download will not receive real-time antivirus scanning. This setting persists until restored, but Windows updates or managed policy can override it. Manual scans, updates and other security features remain available.\n\nQuietCare saves the previous local policy and provides Restore protection. It does not bypass Tamper Protection. Applies to this PC, not only this Windows account.",
                DefenderControlAction.RestoreRealtime=>"Restore the local real-time protection policy saved by QuietCare and request protection ON?\n\nWindows will be queried again before QuietCare reports success.",
                DefenderControlAction.LowerScanCpu=>"Apply lower scan CPU mode?\n\nUses a 20% average scan CPU target, low-priority scheduled scans and throttling during idle scans. This is guidance, not a hard CPU cap, and it does not limit real-time scans. Your current scan settings will be saved for restore.",
                _=>"Restore the scan CPU preferences saved before QuietCare's lower scan CPU mode?"
            };
            if(MessageBox.Show(text,"QuietCare — confirm protection change",MessageBoxButton.YesNo,action==DefenderControlAction.KeepRealtimeOff?MessageBoxImage.Warning:MessageBoxImage.Question,MessageBoxResult.No)!=MessageBoxResult.Yes)return 1223;
            var result=new DefenderControlService(new WindowsDefenderControlRuntime()).Apply(action);
            MessageBox.Show(result.Message,"QuietCare — Windows result",MessageBoxButton.OK,result.Verified?MessageBoxImage.Information:MessageBoxImage.Warning);
            return result.Verified?0:2;
        }
        catch(Exception ex)
        {MessageBox.Show("Windows did not complete the requested action. "+ex.Message+"\n\nAny saved backup is retained. Refresh Protection to see the actual state; Restore protection remains available.","QuietCare protection controls",MessageBoxButton.OK,MessageBoxImage.Warning);return 1;}
        finally{gate.ReleaseMutex();}
    }
    public static async Task<int> RequestAsync(string value)
    {
        if(!IsAction(value))throw new ArgumentOutOfRangeException(nameof(value));
        var executable=Environment.ProcessPath??throw new InvalidOperationException("The installed executable path is unavailable.");
        using var child=Process.Start(new ProcessStartInfo(executable,"--defender-action "+value){UseShellExecute=true,Verb="runas"})??throw new InvalidOperationException("Windows did not start the protection action.");
        await child.WaitForExitAsync();return child.ExitCode;
    }
}
