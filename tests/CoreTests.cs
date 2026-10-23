using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;
using QuietCare;

internal static class CoreTests {
    static int passed,failed;static string fixture;static Cleaner cleaner;
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool CreateHardLink(string path,string existing,IntPtr security);
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Test(string name,Action action){try{action();passed++;Console.WriteLine("PASS "+name);}catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+": "+ex);}}
    static void Reject(Action action){bool rejected=false;try{action();}catch{rejected=true;}Check(rejected,"Expected rejection.");}
    static string Old(string name,string content){string path=Path.Combine(fixture,"Temp",name);File.WriteAllText(path,content);File.SetLastWriteTimeUtc(path,DateTime.UtcNow.AddDays(-10));return path;}
    static TempItem Item(string name){return cleaner.Scan(CancellationToken.None).Items.Single(x=>Path.GetFileName(x.Path)==name);}
    static int Main(string[] args) {
        fixture=Path.GetFullPath(args[0]);State.DirectoryPath=Path.Combine(fixture,"State");Directory.CreateDirectory(Path.Combine(fixture,"Temp"));cleaner=new Cleaner(Path.Combine(fixture,"Temp"),Path.Combine(fixture,"State","Recovery"));
        Test("Boundary rejects sibling prefix and traversal",()=>{Check(SafePaths.Within("C:\\Temp","C:\\Temp\\a.tmp"),"Child expected");Check(!SafePaths.Within("C:\\Temp","C:\\Temp-other\\a.tmp"),"Sibling escaped");Check(!SafePaths.Within("C:\\Temp","C:\\Temp\\..\\a.tmp"),"Traversal escaped");});
        Test("Only old allowed temp extensions",()=>{Old("old.tmp","alpha");Old("keep.docx","document");File.WriteAllText(Path.Combine(fixture,"Temp","new.tmp"),"new");Check(Item("old.tmp")!=null,"Old tmp not found");var rows=cleaner.Scan(CancellationToken.None).Items;Check(!rows.Any(x=>x.Path.EndsWith("keep.docx")||x.Path.EndsWith("new.tmp")),"Excluded file scanned");});
        Test("Linked directory never traversed",()=>{var scan=cleaner.Scan(CancellationToken.None);Check(Directory.Exists(Path.Combine(fixture,"Temp","junction")),"Fixture junction is missing");Check(!scan.Items.Any(x=>x.Path.Contains("outside.tmp")),"Traversed junction");Reject(()=>SafePaths.LockParents(Path.Combine(fixture,"Temp","junction","outside.tmp")));});
        Test("Hard-linked file excluded",()=>{string p=Old("hard.tmp","do not touch");Check(CreateHardLink(Path.Combine(fixture,"outside","hard-link.tmp"),p,IntPtr.Zero),"Hard-link fixture failed");Check(!cleaner.Scan(CancellationToken.None).Items.Any(x=>x.Path==p),"Hard link scanned");});
        Test("Changed file rejected",()=>{string p=Old("changed.tmp","before");var item=Item("changed.tmp");File.AppendAllText(p,"after");Reject(()=>cleaner.Quarantine(item));Check(File.Exists(p),"Changed file removed");});
        Test("Locked file rejected",()=>{string p=Old("locked.tmp","locked");var item=Item("locked.tmp");using(var f=new FileStream(p,FileMode.Open,FileAccess.ReadWrite,FileShare.None))Reject(()=>cleaner.Quarantine(item));Check(File.Exists(p),"Locked file removed");});
        Test("Handle-based quarantine and restore preserves bytes",()=>{string p=Old("restore.tmp","restore payload 123");var rec=cleaner.Quarantine(Item("restore.tmp"));Check(!File.Exists(p)&&File.Exists(rec.Stored),"Move failed");Check(cleaner.ListRecovery().Any(x=>x.Id==rec.Id),"Recovery record missing");cleaner.Restore(rec);Check(File.ReadAllText(p)=="restore payload 123","Content changed");});
        Test("Restore refuses overwrite",()=>{string p=Old("collision.tmp","original");var rec=cleaner.Quarantine(Item("collision.tmp"));File.WriteAllText(p,"new owner");Reject(()=>cleaner.Restore(rec));Check(File.ReadAllText(p)=="new owner","Overwrote new file");Check(File.Exists(rec.Stored),"Lost recovery");});
        Test("Permanent removal affects selected quarantine only",()=>{string p=Old("purge.tmp","purge");var rec=cleaner.Quarantine(Item("purge.tmp"));cleaner.Purge(rec);Check(!File.Exists(rec.Stored),"Quarantine file remains");Check(File.Exists(Path.Combine(fixture,"Temp","keep.docx")),"Other file affected");});
        Test("Forged outside cleanup path rejected",()=>{var item=Item("old.tmp");item.Path=Path.Combine(fixture,"outside","outside.tmp");Reject(()=>cleaner.Quarantine(item));Check(File.Exists(item.Path),"Outside file changed");});
        Test("Cancellation interrupts scans",()=>{var cancel=new CancellationTokenSource();cancel.Cancel();Reject(()=>cleaner.Scan(cancel.Token));});
        Test("Settings round trip and refresh clamp",()=>{new Settings{ShowWidget=false,Interval=5,WidgetX=42,WidgetY=53}.Save();var s=Settings.Load();Check(!s.ShowWidget&&s.Interval==5&&s.WidgetX==42,"Roundtrip failed");new Settings{Interval=999}.Save();Check(Settings.Load().Interval==2,"Interval was not clamped");});
        Test("Variable refresh persists and rejects out-of-range polling",()=>{new Settings{RefreshMs=250}.Save();Check(Settings.Load().RefreshMs==250,"250 ms was lost");new Settings{RefreshMs=1}.Save();Check(Settings.Load().RefreshMs==100,"Unsafe lower bound");new Settings{RefreshMs=99999}.Save();Check(Settings.Load().RefreshMs==10000,"Upper bound");});
        Test("XML external entities rejected",()=>{string path=Path.Combine(fixture,"evil.xml");File.WriteAllText(path,"<!DOCTYPE x [<!ENTITY test SYSTEM 'file:///C:/Windows/win.ini'>]><x>&test;</x>");Reject(()=>State.Read(path));});
        Test("GPU engine grouping sums same engine, not distinct engines",()=>{var values=new[]{new KeyValuePair<string,double>("pid_1_luid_0x0_0x123_phys_0_eng_0_engtype_3D",25),new KeyValuePair<string,double>("pid_2_luid_0x0_0x123_phys_0_eng_0_engtype_3D",30),new KeyValuePair<string,double>("pid_1_luid_0x0_0x123_phys_0_eng_1_engtype_Copy",20)};Check(Telemetry.AggregateGpu(values)==55,"GPU aggregation wrong");Check(Telemetry.AggregateGpu(new KeyValuePair<string,double>[0])==null,"Unavailable should be null");});
        Test("Live Windows telemetry produces plausible samples",()=>{using(var telemetry=new Telemetry()){telemetry.Sample();Thread.Sleep(1100);var s=telemetry.Sample();Check(s.Total>1024*1024*1024&&s.Available<=s.Total,"Memory implausible");Check(s.Cpu.HasValue&&s.Cpu>=0&&s.Cpu<=100,"CPU invalid");Check(!s.Gpu.HasValue || (s.Gpu>=0 && s.Gpu<=100),"GPU invalid");Console.WriteLine("  LIVE CPU="+Format.Percent(s.Cpu)+" RAM="+s.Ram+" GPU="+Format.Percent(s.Gpu)+" Disk="+s.DiskRead);}});
        Test("Process inventory includes this test process",()=>{Check(Processes.List().Any(x=>x.Id==Process.GetCurrentProcess().Id),"Self missing");});
        Test("Selected own-process RAM trim uses the documented API",()=>{var item=Processes.List().Single(x=>x.Id==Process.GetCurrentProcess().Id);Processes.Trim(item);Check(!Process.GetCurrentProcess().HasExited,"Process exited unexpectedly");item.Started=item.Started.AddSeconds(-1);Reject(()=>Processes.Trim(item));});
        Test("Unknown Windows tools and power modes rejected",()=>{Reject(()=>Tools.Open("untrusted.exe"));Reject(()=>Tools.SetPower("custom command"));Check(Tools.Catalog.Select(t=>t.Id).Distinct().Count()==Tools.Catalog.Length,"Duplicate IDs");foreach(var tool in Tools.Catalog)if(tool.SystemExe)Check(File.Exists(Tools.Resolve(tool)),"Missing tool: "+tool.Target);});
        Test("SHA-256 duplicate finder finds identical files",()=>{string folder=Path.Combine(fixture,"find");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"a.bin"),new string('a',4096));File.Copy(Path.Combine(folder,"a.bin"),Path.Combine(folder,"b.bin"));File.WriteAllText(Path.Combine(folder,"c.bin"),new string('b',4096));bool limited;var files=FileFinder.Find(folder,true,CancellationToken.None,out limited);Check(files.Count==2&&files.All(x=>x.Group=="Match 1"),"Duplicate matching failed: "+files.Count);});
        Test("Startup backup, disable, conflict rejection, restore",()=>{
            const string path=@"Software\Microsoft\Windows\CurrentVersion\Run";string name="QuietCare-TEST-"+Guid.NewGuid().ToString("N");
            using(var k=Registry.CurrentUser.CreateSubKey(path))k.SetValue(name,"%TEMP%\\QuietCare-test-never-executed.exe",RegistryValueKind.ExpandString);
            try {
                var item=Startup.List().Single(x=>x.Name==name&&x.Backup==null);Startup.Disable(item);
                using(var k=Registry.CurrentUser.OpenSubKey(path))Check(k.GetValue(name)==null,"Entry not disabled");
                var backup=Startup.List().Single(x=>x.Name==name&&x.Backup!=null);
                using(var k=Registry.CurrentUser.CreateSubKey(path))k.SetValue(name,"collision");Reject(()=>Startup.Restore(backup));
                using(var k=Registry.CurrentUser.CreateSubKey(path))k.DeleteValue(name);Startup.Restore(backup);
                using(var k=Registry.CurrentUser.OpenSubKey(path)){Check(k.GetValueKind(name)==RegistryValueKind.ExpandString,"Lost registry kind");Check((string)k.GetValue(name,null,RegistryValueOptions.DoNotExpandEnvironmentNames)==item.Command,"Lost literal command");}
            }finally{using(var k=Registry.CurrentUser.CreateSubKey(path))k.DeleteValue(name,false);}
        });
        AdvancedTests.Run(fixture,Test);
        Console.WriteLine("RESULT "+passed+" passed / "+failed+" failed");return failed==0?0:1;
    }
}
