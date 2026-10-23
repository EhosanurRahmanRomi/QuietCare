using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace QuietCare {
public sealed class BoostProcess {
    public AppProcess App;public double CpuMs;public bool VisibleWindow;public string CommandLine;
}
public sealed class BoostResult {
    public int Trimmed,Skipped,Failed;public ulong AvailableBefore,AvailableAfter;public bool MediaVerified;public readonly List<string> Details=new List<string>();
    public string Summary {get{return !MediaVerified?"Media activity could not be verified. No apps were changed.":Trimmed==0?"No idle background apps qualified. Active apps and project tools were protected.":"Trimmed "+Trimmed+" idle background processes. Available RAM: "+Format.Bytes(AvailableBefore)+" → "+Format.Bytes(AvailableAfter)+".";}}
}
public static class BoostPolicy {
    public static readonly HashSet<string> Browsers=new HashSet<string>(new[]{"chrome","msedge","brave","vivaldi","opera","firefox"},StringComparer.OrdinalIgnoreCase);
    static readonly HashSet<string> Protected=new HashSet<string>(new[]{"quietcare","codex","code","devenv","rider64","rider","idea64","pycharm64","webstorm64","node","python","pythonw","python3","dotnet","java","javaw","jupyter","git","msbuild","cl","clang","gcc","g++","rustc","cargo","go","r","rstudio","terminal","terminal64","metaeditor64","powershell","pwsh","cmd","windowsterminal","conhost","openconsole","bash","wsl","docker","com.docker.backend","vmware-vmx","virtualboxvm","qemu-system-x86_64","excel","winword","powerpnt","notepad","notepad++","photoshop","illustrator","blender","obs64","obs32","vlc","spotify","teams","ms-teams","zoom","discord","slack","msedgewebview2","explorer","searchhost","startmenuexperiencehost","applicationframehost"},StringComparer.OrdinalIgnoreCase);
    internal static string Name(string value){string name=Path.GetFileName(value??"").Trim().ToLowerInvariant();return name.EndsWith(".exe",StringComparison.Ordinal)?name.Substring(0,name.Length-4):name;}
    public static string Reason(BoostProcess first,BoostProcess second,ISet<int> activeIds,ISet<string> activeNames,AudioActivity audio,ISet<string> extra,int ownId) {
        if(first==null||second==null||first.App==null||second.App==null)return "process unavailable";
        var p=second.App;string name=Name(p.Name);
        if(p.Id==ownId || Protected.Contains(name) || extra.Contains(name))return "protected app / project tool";
        if(first.App.Started!=p.Started || !String.Equals(first.App.Path,p.Path,StringComparison.OrdinalIgnoreCase))return "process identity changed";
        if(activeIds.Contains(p.Id)||activeNames.Contains(name))return "active application";
        if(String.IsNullOrWhiteSpace(p.Path))return "path unavailable";
        if(p.Path.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))return "Windows process";
        if((DateTime.UtcNow-p.Started).TotalSeconds<5)return "recently started";
        if(p.Bytes<48L*1024*1024)return "small working set";
        if(second.CpuMs<first.CpuMs || second.CpuMs-first.CpuMs>20)return "busy process";
        if(!audio.Verified)return "media activity could not be verified";
        if(audio.ActiveProcesses.Contains(p.Id))return "active audio / call";
        if(Browsers.Contains(name)) {
            if(activeNames.Contains(name))return "active browser family";
            string command=second.CommandLine??"";
            if(name=="firefox"){if(command.IndexOf("-contentproc",StringComparison.OrdinalIgnoreCase)<0)return "browser controller";}
            else if(command.IndexOf("--type=renderer",StringComparison.OrdinalIgnoreCase)<0 || command.IndexOf("--extension-process",StringComparison.OrdinalIgnoreCase)>=0)return "browser controller / GPU / extension";
        }else if(second.VisibleWindow || first.VisibleWindow)return "visible application";
        return null;
    }
}
public interface IBoostRuntime {
    List<BoostProcess> Capture();int Foreground();AudioActivity Audio();void Trim(AppProcess app);ulong Available();
}
public sealed class WindowsBoostRuntime : IBoostRuntime {
    public List<BoostProcess> Capture(){
        var result=new List<BoostProcess>();var commands=new Dictionary<int,string>();
        try{using(var search=new ManagementObjectSearcher("root\\CIMV2","SELECT ProcessId,CommandLine FROM Win32_Process WHERE Name='chrome.exe' OR Name='msedge.exe' OR Name='brave.exe' OR Name='vivaldi.exe' OR Name='opera.exe' OR Name='firefox.exe'",new EnumerationOptions{Timeout=TimeSpan.FromSeconds(3),ReturnImmediately=true}))using(var rows=search.Get())foreach(ManagementObject row in rows)using(row)commands[Convert.ToInt32(row["ProcessId"])]=row["CommandLine"] as string;}catch(ManagementException){}catch(UnauthorizedAccessException){}
        foreach(var app in Processes.List())try{using(var p=Process.GetProcessById(app.Id)){string command;commands.TryGetValue(app.Id,out command);IntPtr window=p.MainWindowHandle;result.Add(new BoostProcess{App=app,CpuMs=p.TotalProcessorTime.TotalMilliseconds,VisibleWindow=window!=IntPtr.Zero&&!Native.IsIconic(window),CommandLine=command});}}catch(InvalidOperationException){}catch(ArgumentException){}catch(System.ComponentModel.Win32Exception){}
        return result;
    }
    public int Foreground(){return ForegroundHistory.Observe();}public AudioActivity Audio(){return AudioActivity.Read();}public void Trim(AppProcess app){Processes.Trim(app);}public ulong Available(){return Native.Memory().Available;}
}
internal static class ForegroundHistory {
    static int lastExternal;static readonly int Own=Process.GetCurrentProcess().Id;
    internal static int Observe(){int pid=Native.ForegroundPid();if(pid!=0&&pid!=Own)Interlocked.Exchange(ref lastExternal,pid);return pid==Own?Volatile.Read(ref lastExternal):pid;}
}
public sealed class SmartBoost {
    readonly IBoostRuntime runtime;readonly Func<Task> observe;readonly Stopwatch cooldown=new Stopwatch();bool busy;
    public SmartBoost(IBoostRuntime host=null,Func<Task> observation=null){runtime=host??new WindowsBoostRuntime();observe=observation??(()=>Task.Delay(750));}
    public bool Busy {get{return busy;}}
    public async Task<BoostResult> Run(IEnumerable<string> exclusions,CancellationToken cancel){
        if(busy)throw new InvalidOperationException("RAM Boost is already running.");
        if(cooldown.IsRunning&&cooldown.Elapsed.TotalSeconds<30)throw new InvalidOperationException("RAM Boost is cooling down. Ready in "+Math.Ceiling(30-cooldown.Elapsed.TotalSeconds)+" seconds.");
        busy=true;
        try{
            int firstActive=runtime.Foreground();var first=await Task.Run(()=>runtime.Capture(),cancel);var audio=await Task.Run(()=>runtime.Audio(),cancel);await observe();cancel.ThrowIfCancellationRequested();
            int secondActive=runtime.Foreground();var second=await Task.Run(()=>runtime.Capture(),cancel);var currentAudio=await Task.Run(()=>runtime.Audio(),cancel);audio.Verified&=currentAudio.Verified;audio.ActiveProcesses.UnionWith(currentAudio.ActiveProcesses);
            var activeIds=new HashSet<int>{firstActive,secondActive};var activeNames=new HashSet<string>(first.Concat(second).Where(x=>activeIds.Contains(x.App.Id)||audio.ActiveProcesses.Contains(x.App.Id)).Select(x=>BoostPolicy.Name(x.App.Name)),StringComparer.OrdinalIgnoreCase);
            var extra=new HashSet<string>((exclusions??new string[0]).Select(BoostPolicy.Name),StringComparer.OrdinalIgnoreCase);var before=first.ToDictionary(x=>x.App.Id);var result=new BoostResult{AvailableBefore=runtime.Available(),MediaVerified=audio.Verified};int ownId=Process.GetCurrentProcess().Id;
            var watch=Stopwatch.StartNew();
            foreach(var candidate in second.OrderByDescending(x=>x.App.Bytes)){
                cancel.ThrowIfCancellationRequested();if(result.Trimmed>=12 || watch.Elapsed.TotalSeconds>5)break;
                BoostProcess previous;before.TryGetValue(candidate.App.Id,out previous);
                int current=runtime.Foreground();activeIds.Add(current);var active=second.FirstOrDefault(x=>x.App.Id==current);if(active!=null)activeNames.Add(BoostPolicy.Name(active.App.Name));
                string reason=BoostPolicy.Reason(previous,candidate,activeIds,activeNames,audio,extra,ownId);
                if(reason!=null){result.Skipped++;continue;}
                try{await Task.Run(()=>runtime.Trim(candidate.App),cancel);result.Trimmed++;result.Details.Add(candidate.App.Name+" • working set trim requested");}catch(OperationCanceledException){throw;}catch(Exception){result.Failed++;}
            }
            result.AvailableAfter=runtime.Available();cooldown.Restart();State.Log("Smart RAM Boost: "+result.Trimmed+" idle processes trimmed; "+result.Skipped+" protected/skipped; "+result.Failed+" unavailable.");return result;
        }finally{busy=false;}
    }
}
}
