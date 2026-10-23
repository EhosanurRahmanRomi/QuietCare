using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Xml.Linq;

namespace QuietCare {
public sealed class Tool {
    public string Id,Category,Title,Description,Target,Arguments;public bool SystemExe;
    public Tool(string id,string category,string title,string description,string target,bool exe=false,string arguments=null){Id=id;Category=category;Title=title;Description=description;Target=target;SystemExe=exe;Arguments=arguments;}
}
public static class Tools {
    public static readonly Tool[] Catalog={
        new Tool("storage","Clean","Storage & temporary files","Review Windows cleanup recommendations and free disk space.","ms-settings:storagesense"),
        new Tool("sense","Clean","Automatic cleanup","Configure Windows Storage Sense for scheduled maintenance.","ms-settings:storagepolicies"),
        new Tool("cleanup","Clean","Disk Cleanup","Review system files and update leftovers using Windows.","cleanmgr.exe",true),
        new Tool("recycle","Clean","Recycle Bin","Review and restore recycled files. Empty only after review.","explorer.exe",true,"shell:RecycleBinFolder"),
        new Tool("drives","Speed","Optimize drives","Windows chooses SSD TRIM or hard-drive optimization.","dfrgui.exe",true),
        new Tool("startup","Speed","Windows startup apps","Manage all startup entries, including packaged apps.","ms-settings:startupapps"),
        new Tool("task","Speed","Task Manager","Review CPU, memory, GPU, startup impact and app activity.","Taskmgr.exe",true),
        new Tool("power","Speed","Power & battery","Choose performance, battery and sleep preferences.","ms-settings:powersleep"),
        new Tool("visual","Speed","Visual effects","Adjust animations and appearance for performance.","SystemPropertiesPerformance.exe",true),
        new Tool("gaming","Speed","Game Mode","Configure Windows Game Mode and game optimizations.","ms-settings:gaming-gamemode"),
        new Tool("graphics","Speed","Graphics preferences","Choose which GPU an app should use.","ms-settings:display-advancedgraphics"),
        new Tool("services","Speed","Services","Inspect service descriptions in the Windows console.","mmc.exe",true,"services.msc"),
        new Tool("tasks","Speed","Scheduled tasks","Inspect tasks that start programs in the background.","mmc.exe",true,"taskschd.msc"),
        new Tool("security","Protect","Windows Security","Review antivirus, ransomware and device protection.","windowsdefender:"),
        new Tool("firewall","Protect","Windows Firewall","Review firewall status without turning protection off.","control.exe",true,"/name Microsoft.WindowsFirewall"),
        new Tool("updates","Protect","Windows & driver updates","Install trusted Windows updates and optional drivers.","ms-settings:windowsupdate"),
        new Tool("backup","Protect","Backup & recovery","Review your Windows backup configuration.","ms-settings:backup"),
        new Tool("restore","Protect","System protection","Create or manage restore points using Windows.","SystemPropertiesProtection.exe",true),
        new Tool("trouble","Repair","Troubleshoot","Run the Windows troubleshooting tools.","ms-settings:troubleshoot"),
        new Tool("recovery","Repair","Windows recovery","Review repair and recovery options before making changes.","ms-settings:recovery"),
        new Tool("reliability","Repair","Reliability history","View app failures and Windows stability history.","perfmon.exe",true,"/rel"),
        new Tool("devices","Repair","Device Manager","Inspect hardware and device status.","mmc.exe",true,"devmgmt.msc"),
        new Tool("privacy","Privacy","Privacy controls","Review Windows privacy and permission settings.","ms-settings:privacy"),
        new Tool("diagnostics","Privacy","Diagnostics & feedback","Review optional diagnostic data preferences.","ms-settings:privacy-feedback"),
        new Tool("activity","Privacy","Activity history","Review activity history controls.","ms-settings:privacy-activityhistory"),
        new Tool("notifications","Privacy","Notifications","Turn off unwanted app notifications and distractions.","ms-settings:notifications"),
        new Tool("browser","Privacy","Browser data controls","Open Windows default-browser settings. Clear history inside your browser.","ms-settings:defaultapps"),
        new Tool("apps","Apps","Uninstall apps","Remove Advanced SystemCare or other apps in Windows Installed apps.","ms-settings:appsfeatures"),
        new Tool("store","Apps","Microsoft Store updates","Review trusted Store app updates.","ms-windows-store:downloads"),
        new Tool("optional","Apps","Optional features","Review installed Windows optional components.","ms-settings:optionalfeatures"),
        new Tool("network","Network","Network status","Review connection settings and adapter properties.","ms-settings:network-status"),
        new Tool("dns","Network","Adapter settings","Review DNS and adapter configuration in Windows.","control.exe",true,"/name Microsoft.NetworkAndSharingCenter"),
        new Tool("data","Network","Data usage","Inspect which apps use network traffic.","ms-settings:datausage"),
        new Tool("proxy","Network","Proxy settings","Review your proxy configuration.","ms-settings:network-proxy"),
        new Tool("system","Tools","System Information","Open the detailed Windows hardware and software inventory.","msinfo32.exe",true),
        new Tool("monitor","Tools","Resource Monitor","Investigate CPU, memory, disk and network activity.","resmon.exe",true),
        new Tool("disk","Tools","Disk Management","Review drives and partitions in Windows. Changes can affect data.","mmc.exe",true,"diskmgmt.msc")
    };
    public static void Open(string id) {
        Tool t=Catalog.FirstOrDefault(x=>x.Id==id);if(t==null)throw new InvalidOperationException("Unknown tool.");
        string target=Resolve(t);
        Process.Start(new ProcessStartInfo(target,t.Arguments??""){UseShellExecute=true,WorkingDirectory=Environment.SystemDirectory});
    }
    public static string Resolve(Tool t){return t.SystemExe?Path.Combine(t.Target=="explorer.exe"?Environment.GetFolderPath(Environment.SpecialFolder.Windows):Environment.SystemDirectory,t.Target):t.Target;}
    public static Dictionary<string,string> SystemInfo() {
        var result=new Dictionary<string,string>();result["Computer"]=Environment.MachineName;
        result["Architecture"]=Environment.Is64BitOperatingSystem?"64 bit":"32 bit";
        result["Logical processors"]=Environment.ProcessorCount.ToString();result["Installed memory"]=Format.Bytes(Native.Memory().Total);
        ReadWmi(result,"Win32_Processor",new[]{"Name"},"Processor");
        ReadWmi(result,"Win32_VideoController",new[]{"Name","DriverVersion"},"Graphics");
        ReadWmi(result,"Win32_OperatingSystem",new[]{"Caption","Version","LastBootUpTime"},"Windows");
        foreach(var d in DriveInfo.GetDrives())try{if(d.IsReady && d.DriveType==DriveType.Fixed)result["Drive "+d.Name]=Format.Bytes(d.AvailableFreeSpace)+" free of "+Format.Bytes(d.TotalSize);}catch(IOException){}
        result["Temperature sensors"]="Not available in this driver-free build. No kernel driver is installed.";
        result["QuietCare architecture"]="Native Windows • local only • standard user • no network listener";
        return result;
    }
    static void ReadWmi(Dictionary<string,string> data,string cls,string[] columns,string prefix) {
        try {var options=new EnumerationOptions {ReturnImmediately=true,Timeout=TimeSpan.FromSeconds(5)};
            using(var search=new ManagementObjectSearcher("root\\CIMV2","SELECT "+String.Join(",",columns)+" FROM "+cls,options))using(var objects=search.Get()) {
                int i=0;foreach(ManagementObject o in objects)using(o){i++;foreach(string c in columns)data[prefix+" "+i+" • "+c]=Convert.ToString(o[c]);}
            }
        }catch(ManagementException){data[prefix]="Unavailable";}catch(UnauthorizedAccessException){data[prefix]="Permission denied";}
    }
    public static string SetPower(string mode) {
        var known=new Dictionary<string,Guid>{{"Balanced",new Guid("381b4222-f694-41f0-9685-ff5bb260df2e")},{"Performance",new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c")},{"Battery saver",new Guid("a1841308-3541-4fab-bc81-f71556f20b4a")}};
        Guid id;if(!known.TryGetValue(mode,out id))throw new InvalidOperationException("Unknown power plan.");
        Guid previous=Native.PowerScheme();string backup=State.FilePath("power.xml");
        if(!File.Exists(backup))State.Save(backup,new XDocument(new XElement("power",new XAttribute("previous",previous))));
        uint status=Native.PowerSetActiveScheme(IntPtr.Zero,ref id);if(status!=0)throw new InvalidOperationException("This plan is unavailable on your PC. Use Windows Power & battery instead. ("+status+")");
        return "Power plan changed to "+mode+". The original plan is saved for Restore.";
    }
    public static void RestorePower() {
        string path=State.FilePath("power.xml");Guid id=Guid.Parse((string)State.Read(path).Root.Attribute("previous"));
        if(Native.PowerSetActiveScheme(IntPtr.Zero,ref id)!=0)throw new InvalidOperationException("Windows could not restore the previous power plan.");
        File.Delete(path);
    }
}
}
