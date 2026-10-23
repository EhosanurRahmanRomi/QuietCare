using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Win32;

namespace QuietCare {
public sealed class StartupItem {
    public string Name {get;set;} public string Command {get;set;} public string Source {get;set;} public string Status {get;set;}
    public bool Editable {get;set;} public string Backup {get;set;} public RegistryValueKind Kind {get;set;}
}
public static class Startup {
    const string Run=@"Software\Microsoft\Windows\CurrentVersion\Run";
    public static List<StartupItem> List() {
        var items=new List<StartupItem>();
        Read(Registry.CurrentUser,Run,"Current user • Run",true,items);
        Read(Registry.LocalMachine,Run,"All users • Run",false,items);
        if(Environment.Is64BitOperatingSystem)Read(Registry.LocalMachine,@"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run","All users • 32 bit",false,items);
        foreach(var folder in new[]{Environment.SpecialFolder.Startup,Environment.SpecialFolder.CommonStartup}) {
            string path=Environment.GetFolderPath(folder);if(!Directory.Exists(path))continue;
            foreach(string p in Directory.EnumerateFiles(path).Where(x=>!String.Equals(Path.GetFileName(x),"desktop.ini",StringComparison.OrdinalIgnoreCase)))items.Add(new StartupItem {Name=Path.GetFileNameWithoutExtension(p),Command=p,Source=folder==Environment.SpecialFolder.Startup?"User Startup folder":"Shared Startup folder",Status="Use Windows settings"});
        }
        string backups=State.FilePath("Startup");if(Directory.Exists(backups))foreach(string p in Directory.EnumerateFiles(backups,"*.xml"))try {
            var r=State.Read(p).Root;items.Add(new StartupItem {Name=(string)r.Attribute("name"),Command=r.Value,Source="QuietCare backup",Status="Disabled by QuietCare",Editable=true,Backup=p,Kind=(RegistryValueKind)(int)r.Attribute("kind")});
        }catch(Exception){}
        return items.OrderBy(i=>i.Name,StringComparer.OrdinalIgnoreCase).ToList();
    }
    static void Read(RegistryKey hive,string path,string source,bool editable,List<StartupItem> list) {
        using(var k=hive.OpenSubKey(path))if(k!=null)foreach(string name in k.GetValueNames())try {
            object value=k.GetValue(name,null,RegistryValueOptions.DoNotExpandEnvironmentNames);
            var kind=k.GetValueKind(name);if(!(value is string))continue;
            list.Add(new StartupItem {Name=name,Command=(string)value,Source=source,Status=editable?"Registered":"Managed by Windows",Editable=editable && name!="QuietCare" && (kind==RegistryValueKind.String || kind==RegistryValueKind.ExpandString),Kind=kind});
        }catch(System.Security.SecurityException){}
    }
    public static void Disable(StartupItem item) {
        if(item==null || !item.Editable || item.Source!="Current user • Run" || item.Backup!=null)throw new InvalidOperationException("Manage this entry in Windows Startup settings.");
        using(var k=Registry.CurrentUser.OpenSubKey(Run,true)) {
            if(k==null || !Equals(k.GetValue(item.Name,null,RegistryValueOptions.DoNotExpandEnvironmentNames),item.Command) || k.GetValueKind(item.Name)!=item.Kind)throw new InvalidOperationException("The startup entry changed. Refresh first.");
            string path=Path.Combine(State.FilePath("Startup"),Guid.NewGuid().ToString("N")+".xml");
            State.Save(path,new XDocument(new XElement("entry",new XAttribute("name",item.Name),new XAttribute("kind",(int)item.Kind),item.Command)));
            k.DeleteValue(item.Name,true);
        }
        State.Log("Disabled a selected current-user startup entry.");
    }
    public static void Restore(StartupItem item) {
        if(item==null || item.Backup==null || !SafePaths.Within(State.FilePath("Startup"),item.Backup))throw new InvalidOperationException("Select a QuietCare backup.");
        if(item.Kind!=RegistryValueKind.String && item.Kind!=RegistryValueKind.ExpandString)throw new InvalidOperationException("Unsupported startup entry type.");
        using(var k=Registry.CurrentUser.CreateSubKey(Run)) {
            if(k.GetValueNames().Contains(item.Name,StringComparer.OrdinalIgnoreCase))throw new InvalidOperationException("An entry with this name already exists; it will not be overwritten.");
            k.SetValue(item.Name,item.Command,item.Kind);
        }
        File.Delete(item.Backup);State.Log("Restored a selected startup entry.");
    }
    public static bool OwnEnabled() {using(var k=Registry.CurrentUser.OpenSubKey(Run))return k!=null && k.GetValue("QuietCare")!=null;}
    public static void SetOwn(bool enabled) {
        using(var k=Registry.CurrentUser.CreateSubKey(Run)) {
            if(enabled)k.SetValue("QuietCare","\""+Process.GetCurrentProcess().MainModule.FileName+"\" --tray",RegistryValueKind.String);
            else k.DeleteValue("QuietCare",false);
        }
    }
}
public sealed class InstalledApp {
    public string Name {get;set;} public string Version {get;set;} public string Publisher {get;set;} public string Date {get;set;}
}
public static class Programs {
    public static List<InstalledApp> List() {
        var apps=new List<InstalledApp>();const string sub=@"Software\Microsoft\Windows\CurrentVersion\Uninstall";
        foreach(var hive in new[]{RegistryHive.CurrentUser,RegistryHive.LocalMachine})foreach(var view in new[]{RegistryView.Registry64,RegistryView.Registry32})
        using(var root=RegistryKey.OpenBaseKey(hive,view))using(var key=root.OpenSubKey(sub))if(key!=null)foreach(string name in key.GetSubKeyNames()) {
            try {using(var k=key.OpenSubKey(name)) {
                if(k==null || (int?)k.GetValue("SystemComponent")==1)continue;string title=k.GetValue("DisplayName") as string;if(String.IsNullOrWhiteSpace(title))continue;
                apps.Add(new InstalledApp {Name=title,Version=k.GetValue("DisplayVersion") as string,Publisher=k.GetValue("Publisher") as string,Date=k.GetValue("InstallDate") as string});
            }}catch(System.Security.SecurityException){}
        }
        return apps.GroupBy(x=>x.Name+"|"+x.Version).Select(g=>g.First()).OrderBy(x=>x.Name).ToList();
    }
}
}
