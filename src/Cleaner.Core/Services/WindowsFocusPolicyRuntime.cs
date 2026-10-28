using System.Management;
using System.Text.Json;
using Microsoft.Win32;
namespace Cleaner.Core.Services;

public sealed class WindowsFocusPolicyRuntime : IFocusPolicyRuntime,IDisposable
{
    const string StateKey=@"Software\QuietCare\FocusUpdateGuard";
    readonly WindowsLocalMachinePolicy policy;
    public WindowsFocusPolicyRuntime(bool readOnly=false)=>policy=new(readOnly);
    public bool Supported=>IsSupported();
    public static bool IsSupported()
    {
        try
        {
            using var edition=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            string name=edition?.GetValue("EditionID") as string??"";
            if(!new[]{"Professional","Enterprise","Education"}.Any(x=>name.StartsWith(x,StringComparison.OrdinalIgnoreCase)))return false;
            using var search=new ManagementObjectSearcher(@"root\cimv2","SELECT PartOfDomain FROM Win32_ComputerSystem");
            using var results=search.Get();bool returned=false;
            foreach(ManagementObject result in results)using(result){if(result["PartOfDomain"] is not bool domain||domain)return false;returned=true;}
            if(!returned)return false;
            using(var endpoint=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Advanced Threat Protection\Status"))if(endpoint?.GetValue("OnboardingState") is int state&&state==1)return false;
            using var enrollments=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Enrollments");
            if(enrollments is not null)foreach(string child in enrollments.GetSubKeyNames())
            {using var key=enrollments.OpenSubKey(child);if(string.Equals(key?.GetValue("ProviderID") as string,"MS DM Server",StringComparison.OrdinalIgnoreCase))return false;}
            return true;
        }
        catch{return false;}
    }
    public int? Read(string key){var setting=FocusUpdateGuard.Settings[key];return policy.Read(setting.Path,setting.Name);}
    public void Set(string key,int? value){var setting=FocusUpdateGuard.Settings[key];policy.Set(setting.Path,setting.Name,value);}
    public void Commit()=>policy.Commit();
    public void Save(FocusPolicyBackup backup)
    {
        using var key=Registry.LocalMachine.CreateSubKey(StateKey,true);
        key.SetValue("BackupV1",JsonSerializer.Serialize(backup),RegistryValueKind.String);key.Flush();
    }
    public static FocusPolicyBackup? Load()
    {
        using var key=Registry.LocalMachine.OpenSubKey(StateKey);
        return key?.GetValue("BackupV1") is string json?JsonSerializer.Deserialize<FocusPolicyBackup>(json):null;
    }
    public void Dispose()=>policy.Dispose();
}
