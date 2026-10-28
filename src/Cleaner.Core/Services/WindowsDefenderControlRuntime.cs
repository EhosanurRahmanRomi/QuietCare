using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace Cleaner.Core.Services;

public sealed class WindowsDefenderControlRuntime : IDefenderControlRuntime
{
    const string Namespace=@"root\Microsoft\Windows\Defender";
    const string PolicyPath=@"Software\Policies\Microsoft\Windows Defender\Real-Time Protection";
    const string PolicyName="DisableRealtimeMonitoring";
    const string BackupPath=@"Software\QuietCare\DefenderControl";
    public static readonly DefenderControlBackup EmptyBackup=new(false,null,false,50,false,true);
    static bool Boolean(object? value)=>value is bool result?result:throw new InvalidOperationException("Windows returned an unknown protection boolean. No settings can be authorized from it.");
    static ManagementObject First(string scope,string query)
    {
        using var search=new ManagementObjectSearcher(scope,query,new System.Management.EnumerationOptions{Timeout=TimeSpan.FromSeconds(5),ReturnImmediately=true});
        using var rows=search.Get();
        foreach(ManagementObject item in rows)return item;
        throw new InvalidOperationException("Windows did not return the requested Defender information.");
    }
    public DefenderControlState Read()
    {
        try
        {
            using var status=First(Namespace,"SELECT RealTimeProtectionEnabled,IsTamperProtected FROM MSFT_MpComputerStatus");
            using var preferences=First(Namespace,"SELECT ScanAvgCPULoadFactor,EnableLowCpuPriority,DisableCpuThrottleOnIdleScans FROM MSFT_MpPreference");
            using var edition=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            string name=edition?.GetValue("EditionID") as string??"Unknown";
            bool supported=name.StartsWith("Professional",StringComparison.OrdinalIgnoreCase)||name.StartsWith("Enterprise",StringComparison.OrdinalIgnoreCase)||name.StartsWith("Education",StringComparison.OrdinalIgnoreCase);
            using var device=First(@"root\cimv2","SELECT PartOfDomain FROM Win32_ComputerSystem");
            bool managed=Boolean(device["PartOfDomain"]);
            using(var endpoint=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Advanced Threat Protection\Status"))managed|=endpoint?.GetValue("OnboardingState") is int onboarded&&onboarded==1;
            using(var enrollments=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Enrollments"))
                if(enrollments is not null)foreach(string child in enrollments.GetSubKeyNames())
                {using var enrollment=enrollments.OpenSubKey(child);managed|=string.Equals(enrollment?.GetValue("ProviderID") as string,"MS DM Server",StringComparison.OrdinalIgnoreCase);}
            var backup=LoadBackup();
            using var effective=Registry.LocalMachine.OpenSubKey(PolicyPath);
            object? policyValue=effective?.GetValue(PolicyName);
            if(policyValue is not null and not int)throw new InvalidOperationException("The effective local policy has an unsupported type.");
            int? policy=policyValue as int?;
            if(preferences["ScanAvgCPULoadFactor"] is null)throw new InvalidOperationException("Windows did not return its scan CPU target.");
            return new(true,Boolean(status["RealTimeProtectionEnabled"]),Boolean(status["IsTamperProtected"]),supported,managed,policy,
                Convert.ToInt32(preferences["ScanAvgCPULoadFactor"]),Boolean(preferences["EnableLowCpuPriority"]),Boolean(preferences["DisableCpuThrottleOnIdleScans"]),backup.PolicyOwned,backup.ScanOwned,name+" · "+(managed?"managed device":"local device"));
        }
        catch(Exception ex){return new(false,false,false,false,true,null,0,false,false,false,false,"Defender controls unavailable: "+ex.Message);}
    }
    public DefenderControlBackup LoadBackup()
    {
        using var key=Registry.LocalMachine.OpenSubKey(BackupPath);
        if(key?.GetValue("BackupV1") is not string json)return EmptyBackup;
        return JsonSerializer.Deserialize<DefenderControlBackup>(json)??throw new InvalidOperationException("The Defender settings backup could not be read. It was preserved.");
    }
    public void SaveBackup(DefenderControlBackup backup)
    {
        if(backup.PreviousPolicy is not null and not 0 and not 1||backup.PreviousCpu is <0 or >100)throw new InvalidOperationException("Invalid Defender backup values.");
        using var key=Registry.LocalMachine.CreateSubKey(BackupPath,true);
        key.SetValue("BackupV1",JsonSerializer.Serialize(backup),RegistryValueKind.String);key.Flush();
    }
    public int? ReadLocalRealtimePolicy()
    {
        using var local=new WindowsLocalMachinePolicy();return local.Read(PolicyPath,PolicyName);
    }
    public void WriteLocalRealtimePolicy(int? value)
    {
        if(value is not null and not 0 and not 1)throw new ArgumentOutOfRangeException(nameof(value));
        using var local=new WindowsLocalMachinePolicy();local.Set(PolicyPath,PolicyName,value);local.Commit();
    }
    public void SetRealtimeDisabled(bool disabled)=>SetPreferences(new Dictionary<string,object>{{"DisableRealtimeMonitoring",disabled}});
    public void SetScanSettings(int cpuTarget,bool lowPriority,bool unthrottledIdle)
    {
        if(cpuTarget is <0 or >100)throw new ArgumentOutOfRangeException(nameof(cpuTarget));
        SetPreferences(new Dictionary<string,object>{{"ScanAvgCPULoadFactor",(byte)cpuTarget},{"EnableLowCpuPriority",lowPriority},{"DisableCpuThrottleOnIdleScans",unthrottledIdle}});
    }
    static void SetPreferences(Dictionary<string,object> values)
    {
        using var preference=new ManagementClass(new ManagementScope(Namespace),new ManagementPath("MSFT_MpPreference"),null);
        using var input=preference.GetMethodParameters("Set");
        foreach(var entry in values)input[entry.Key]=entry.Value;
        using var output=preference.InvokeMethod("Set",input,new InvokeMethodOptions{Timeout=TimeSpan.FromSeconds(30)});
        uint code=Convert.ToUInt32(output?["ReturnValue"]??0);
        if(code!=0)throw new InvalidOperationException("Windows rejected the Defender preference request ("+code+"). The saved backup is retained.");
    }
    public void WaitForPolicy()=>Thread.Sleep(3000);
    public object Probe()
    {
        var state=Read();
        using var preference=new ManagementClass(new ManagementScope(Namespace),new ManagementPath("MSFT_MpPreference"),null);
        using var input=preference.GetMethodParameters("Set");
        var names=new[]{"DisableRealtimeMonitoring","ScanAvgCPULoadFactor","EnableLowCpuPriority","DisableCpuThrottleOnIdleScans"};
        var parameters=names.Select(name=>new{name,type=input.Properties[name].Type.ToString()}).ToArray();
        string policyRead;
        try{using var local=new WindowsLocalMachinePolicy(true);_ =local.Read(PolicyPath,PolicyName);policyRead="Read-only COM policy access succeeded";}
        catch(Exception ex){policyRead=ex.GetType().Name+": "+ex.Message;}
        return new{scope="Read-only Defender provider and local-policy capability probe; no Set, Save, backup or preference mutation",state,parameters,policyRead};
    }

}
