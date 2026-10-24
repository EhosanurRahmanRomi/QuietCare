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
    public AppProcess App;public double CpuMs;public bool VisibleWindow,BackgroundPriority;public string CommandLine;
}
public sealed class BoostObservation {
    public string App {get;set;}public long BeforeBytes,AfterBytes;public bool Measured,BeforeMeasured;
    public long ImmediateBytes;public bool ImmediateMeasured;
    public string Before {get{return BeforeMeasured?Format.Bytes(BeforeBytes):"Unavailable";}}public string After {get{return Measured?Format.Bytes(AfterBytes):"Unavailable";}}
    public string Outcome {get{return !Measured?"Trim accepted; settled measurement unavailable":AfterBytes<BeforeBytes?"Resident footprint lower at follow-up":"No lasting resident decrease at follow-up";}}
}
public sealed class BoostResult {
    public int Trimmed,Skipped,Failed,Eligible;public ulong AvailableBefore,AvailableAfter,TotalMemory;public bool MediaVerified;public readonly List<string> Details=new List<string>();
    public readonly Dictionary<string,int> SkipReasons=new Dictionary<string,int>();public readonly List<BoostObservation> Observations=new List<BoostObservation>();
    public int ObservedReductions {get{return Observations.Count(x=>x.Measured&&x.AfterBytes<x.BeforeBytes);}}
    public long AvailableDelta {get{return AvailableAfter>=AvailableBefore?(long)(AvailableAfter-AvailableBefore):-(long)(AvailableBefore-AvailableAfter);}}
    public long ResidentReduction {get{return Observations.Where(x=>x.Measured).Sum(x=>Math.Max(0,x.BeforeBytes-x.AfterBytes));}}
    public string AvailableChange {get{return (AvailableDelta>=0?"+":"−")+Format.Bytes(Math.Abs(AvailableDelta));}}
    public string UsageChange {get{return TotalMemory==0?"":" RAM used: "+(100.0*(1-(double)AvailableBefore/TotalMemory)).ToString("F1")+"% → "+(100.0*(1-(double)AvailableAfter/TotalMemory)).ToString("F1")+"%.";}}
    public string Summary {get{return !MediaVerified?"Media activity could not be verified. No apps were changed.":Trimmed==0?(Failed>0?"Windows declined "+Failed+" eligible trim requests.":"No idle background processes qualified. "+Skipped+" were protected or skipped."):"RAM checked after 3 seconds: available "+AvailableChange+"; "+ObservedReductions+"/"+Trimmed+" processes still have a lower resident footprint."+UsageChange+(AvailableDelta<=0?" No overall available-RAM gain remained.":" Other running apps also affect available RAM.");}}
    public string Report {get{return Summary+"\n\nAvailable RAM: "+Format.Bytes(AvailableBefore)+" → "+Format.Bytes(AvailableAfter)+"\nSum of per-process resident reductions: "+Format.Bytes(ResidentReduction)+" (shared pages may overlap; this is not total RAM freed).\n\n"+string.Join("\n",Observations.Select(x=>x.App+": "+x.Before+" → "+x.After+" · "+x.Outcome))+"\n\nProtected or skipped:\n"+string.Join("\n",SkipReasons.Select(x=>x.Key+": "+x.Value))+"\n\n"+string.Join("\n",Details)+"\n\nThis trims resident pages, not allocations. Pages can return when an app uses them. Active tabs and projects are preserved; no decrease is guaranteed.";}}
}
public static class BoostPolicy {
    public static readonly HashSet<string> Browsers=new HashSet<string>(new[]{"chrome","msedge","brave","vivaldi","opera","firefox"},StringComparer.OrdinalIgnoreCase);
    static readonly HashSet<string> Protected=new HashSet<string>(new[]{"quietcare","quietcare.next","msmpeng","mssense","nissrv","securityhealthservice","avp","avpui","avguard","avgsvc","avastsvc","mbamservice","wrsa","bdservicehost","ekrn","mcshield","codex","code","devenv","rider64","rider","idea64","pycharm64","webstorm64","node","python","pythonw","python3","dotnet","java","javaw","jupyter","git","msbuild","cl","clang","gcc","g++","rustc","cargo","go","r","rstudio","terminal","terminal64","metaeditor64","powershell","pwsh","cmd","windowsterminal","conhost","openconsole","bash","wsl","docker","com.docker.backend","vmware-vmx","virtualboxvm","qemu-system-x86_64","excel","winword","powerpnt","notepad","notepad++","photoshop","illustrator","blender","obs64","obs32","vlc","spotify","teams","ms-teams","zoom","discord","slack","msedgewebview2","explorer","searchhost","startmenuexperiencehost","applicationframehost"},StringComparer.OrdinalIgnoreCase);
    internal static string Name(string value){string name=Path.GetFileName(value??"").Trim().ToLowerInvariant();return name.EndsWith(".exe",StringComparison.Ordinal)?name.Substring(0,name.Length-4):name;}
    public static string Reason(BoostProcess first,BoostProcess second,ISet<int> activeIds,ISet<string> activeNames,AudioActivity audio,ISet<string> extra,int ownId,ISet<string> mediaNames=null,bool backgroundBrowsers=false,bool expandedIdleApps=false) {
        if(first==null||second==null||first.App==null||second.App==null)return "process unavailable";
        var p=second.App;string name=Name(p.Name);
        if(p.Id==ownId || Protected.Contains(name) || extra.Contains(name))return "protected app / project tool";
        if(first.App.Started!=p.Started || !String.Equals(first.App.Path,p.Path,StringComparison.OrdinalIgnoreCase))return "process identity changed";
        bool browser=Browsers.Contains(name);if(activeIds.Contains(p.Id))return "foreground process";
        if(activeNames.Contains(name)&&!(browser&&backgroundBrowsers&&first.BackgroundPriority&&second.BackgroundPriority))return "active application family";
        if(browser&&mediaNames!=null&&mediaNames.Contains(name))return "browser family playing audio / call";
        if(String.IsNullOrWhiteSpace(p.Path))return "path unavailable";
        if(p.Path.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))return "Windows process";
        if((DateTime.UtcNow-p.Started).TotalSeconds<(expandedIdleApps?60:5))return "recently started";
        if(p.Bytes<(expandedIdleApps?32L:48L)*1024*1024)return "small working set";
        if(second.CpuMs<first.CpuMs || second.CpuMs-first.CpuMs>(expandedIdleApps?10:20))return "busy process";
        if(!audio.Verified)return "media activity could not be verified";
        if(audio.ActiveProcesses.Contains(p.Id))return "active audio / call";
        if(Browsers.Contains(name)) {
            string command=second.CommandLine??"";
            if(name=="firefox"){if(command.IndexOf("-contentproc",StringComparison.OrdinalIgnoreCase)<0)return "browser controller";}
            else if(command.IndexOf("--type=renderer",StringComparison.OrdinalIgnoreCase)<0 || command.IndexOf("--extension-process",StringComparison.OrdinalIgnoreCase)>=0)return "browser controller / GPU / extension";
        }else if(!expandedIdleApps&&(second.VisibleWindow || first.VisibleWindow))return "visible application";
        return null;
    }
}
public interface IBoostRuntime {
    List<BoostProcess> Capture();int Foreground();AudioActivity Audio();void Trim(AppProcess app);ulong Available();
}
public interface IBoostMeasurements {long? WorkingSet(AppProcess app);}
public interface IBoostMemoryStatus {ulong TotalMemory();}
public interface IBoostReadiness {string Recheck(BoostProcess app,bool requireBackgroundPriority);}
public sealed class WindowsBoostRuntime : IBoostRuntime,IBoostMeasurements,IBoostReadiness,IBoostMemoryStatus {
    public ulong TotalMemory(){return Native.Memory().Total;}
    public List<BoostProcess> Capture(){
        var result=new List<BoostProcess>();var commands=new Dictionary<int,string>();
        try{using(var search=new ManagementObjectSearcher("root\\CIMV2","SELECT ProcessId,CommandLine FROM Win32_Process WHERE Name='chrome.exe' OR Name='msedge.exe' OR Name='brave.exe' OR Name='vivaldi.exe' OR Name='opera.exe' OR Name='firefox.exe'",new System.Management.EnumerationOptions{Timeout=TimeSpan.FromSeconds(3),ReturnImmediately=true}))using(var rows=search.Get())foreach(ManagementObject row in rows)using(row)commands[Convert.ToInt32(row["ProcessId"])]=row["CommandLine"] as string;}catch(ManagementException){}catch(UnauthorizedAccessException){}
        foreach(var app in Processes.List())try{using(var p=Process.GetProcessById(app.Id)){string command;commands.TryGetValue(app.Id,out command);IntPtr window=p.MainWindowHandle;bool low=false;try{low=p.PriorityClass==ProcessPriorityClass.Idle||p.PriorityClass==ProcessPriorityClass.BelowNormal;}catch(System.ComponentModel.Win32Exception){}result.Add(new BoostProcess{App=app,CpuMs=p.TotalProcessorTime.TotalMilliseconds,VisibleWindow=window!=IntPtr.Zero&&!Native.IsIconic(window),CommandLine=command,BackgroundPriority=low});}}catch(InvalidOperationException){}catch(ArgumentException){}catch(System.ComponentModel.Win32Exception){}
        return result;
    }
    public int Foreground(){return ForegroundHistory.Observe();}public AudioActivity Audio(){return AudioActivity.Read();}public void Trim(AppProcess app){Processes.Trim(app);}public ulong Available(){return Native.Memory().Available;}
    public long? WorkingSet(AppProcess app){try{using(var process=Process.GetProcessById(app.Id)){if(process.StartTime.ToUniversalTime()!=app.Started)return null;process.Refresh();return process.WorkingSet64;}}catch(ArgumentException){return null;}catch(InvalidOperationException){return null;}catch(System.ComponentModel.Win32Exception){return null;}}
    public string Recheck(BoostProcess candidate,bool requireBackgroundPriority){
        try{
            var app=candidate.App;string name=BoostPolicy.Name(app.Name);int foreground=ForegroundHistory.Observe();
            if(foreground==app.Id)return "became foreground";
            using(var process=Process.GetProcessById(app.Id)){
                if(process.StartTime.ToUniversalTime()!=app.Started)return "process identity changed";
                if(requireBackgroundPriority&&process.PriorityClass!=ProcessPriorityClass.Idle&&process.PriorityClass!=ProcessPriorityClass.BelowNormal)return "browser priority changed";
                if(process.TotalProcessorTime.TotalMilliseconds-candidate.CpuMs>25)return "became busy";
            }
            if(foreground>0)using(var active=Process.GetProcessById(foreground))if(String.Equals(active.ProcessName,name,StringComparison.OrdinalIgnoreCase)&&!requireBackgroundPriority)return "became active application family";
            var audio=Audio();if(!audio.Verified)return "media activity could not be verified";
            if(audio.ActiveProcesses.Contains(app.Id))return "active audio / call";
            if(BoostPolicy.Browsers.Contains(name)){
                foreach(int pid in audio.ActiveProcesses)using(var media=Process.GetProcessById(pid))if(String.Equals(media.ProcessName,name,StringComparison.OrdinalIgnoreCase))return "browser family playing audio / call";
            }
            return null;
        }catch(ArgumentException){return "process no longer accessible";}catch(InvalidOperationException){return "process no longer accessible";}catch(System.ComponentModel.Win32Exception){return "process no longer accessible";}
    }
}
internal static class ForegroundHistory {
    static int lastExternal;static readonly int Own=Process.GetCurrentProcess().Id;
    internal static int Observe(){int pid=Native.ForegroundPid();if(pid!=0&&pid!=Own)Interlocked.Exchange(ref lastExternal,pid);return pid==Own?Volatile.Read(ref lastExternal):pid;}
}
public sealed class SmartBoost {
    readonly IBoostRuntime runtime;readonly Func<Task> observe;readonly Func<CancellationToken,Task> settle;readonly Stopwatch cooldown=new Stopwatch();bool busy;
    public SmartBoost(IBoostRuntime host=null,Func<Task> observation=null,Func<CancellationToken,Task> settling=null){runtime=host??new WindowsBoostRuntime();observe=observation;settle=settling??(ct=>Task.Delay(3000,ct));}
    public bool Busy {get{return busy;}}
    public int ReadyInSeconds {get{return cooldown.IsRunning?Math.Max(0,(int)Math.Ceiling(30-cooldown.Elapsed.TotalSeconds)):0;}}
    public async Task<BoostResult> Run(IEnumerable<string> exclusions,CancellationToken cancel,bool backgroundBrowsers=false,IProgress<string> progress=null,bool expandedIdleApps=false){
        if(busy)throw new InvalidOperationException("RAM Boost is already running.");
        if(cooldown.IsRunning&&cooldown.Elapsed.TotalSeconds<30)throw new InvalidOperationException("RAM Boost is cooling down. Ready in "+Math.Ceiling(30-cooldown.Elapsed.TotalSeconds)+" seconds.");
        busy=true;
        try{
            progress?.Report(expandedIdleApps?"Checking two seconds of idle activity across background apps; protecting active work…":"Checking idle background apps and protecting active work…");
            int firstActive=runtime.Foreground();var first=await Task.Run(()=>runtime.Capture(),cancel);var audio=await Task.Run(()=>runtime.Audio(),cancel);await (observe?.Invoke()??Task.Delay(expandedIdleApps?2000:750,cancel));cancel.ThrowIfCancellationRequested();
            int secondActive=runtime.Foreground();var second=await Task.Run(()=>runtime.Capture(),cancel);var currentAudio=await Task.Run(()=>runtime.Audio(),cancel);audio.Verified&=currentAudio.Verified;audio.ActiveProcesses.UnionWith(currentAudio.ActiveProcesses);
            var activeIds=new HashSet<int>{firstActive,secondActive};var activeNames=new HashSet<string>(first.Concat(second).Where(x=>activeIds.Contains(x.App.Id)).Select(x=>BoostPolicy.Name(x.App.Name)),StringComparer.OrdinalIgnoreCase);var mediaNames=new HashSet<string>(first.Concat(second).Where(x=>audio.ActiveProcesses.Contains(x.App.Id)).Select(x=>BoostPolicy.Name(x.App.Name)),StringComparer.OrdinalIgnoreCase);
            var extra=new HashSet<string>((exclusions??new string[0]).Select(BoostPolicy.Name),StringComparer.OrdinalIgnoreCase);var before=first.ToDictionary(x=>x.App.Id);var result=new BoostResult{AvailableBefore=runtime.Available(),MediaVerified=audio.Verified,TotalMemory=(runtime as IBoostMemoryStatus)?.TotalMemory()??0};int ownId=Process.GetCurrentProcess().Id;
            var trimmedApps=new List<AppProcess>();
            int limit=expandedIdleApps?(result.TotalMemory>0&&(double)result.AvailableBefore/result.TotalMemory<.20?32:24):12;
            var watch=Stopwatch.StartNew();var measurements=runtime as IBoostMeasurements;var readiness=runtime as IBoostReadiness;
            foreach(var candidate in second.OrderByDescending(x=>x.App.Bytes)){
                cancel.ThrowIfCancellationRequested();if(result.Trimmed>=limit || watch.Elapsed.TotalSeconds>(expandedIdleApps?8:5)){result.Details.Add("Stopped at the bounded work limit ("+limit+" apps, "+(expandedIdleApps?8:5)+" seconds).");break;}
                BoostProcess previous;before.TryGetValue(candidate.App.Id,out previous);
                int current=runtime.Foreground();activeIds.Add(current);var active=second.FirstOrDefault(x=>x.App.Id==current);if(active!=null)activeNames.Add(BoostPolicy.Name(active.App.Name));
                string reason=BoostPolicy.Reason(previous,candidate,activeIds,activeNames,audio,extra,ownId,mediaNames,backgroundBrowsers,expandedIdleApps);
                bool activeBrowser=BoostPolicy.Browsers.Contains(BoostPolicy.Name(candidate.App.Name))&&activeNames.Contains(BoostPolicy.Name(candidate.App.Name));
                if(reason==null){if(activeBrowser&&readiness==null)reason="background priority could not be rechecked";else if(readiness!=null)reason=await Task.Run(()=>readiness.Recheck(candidate,activeBrowser),cancel);}
                if(reason!=null){result.Skipped++;if(!result.SkipReasons.ContainsKey(reason))result.SkipReasons[reason]=0;result.SkipReasons[reason]++;continue;}
                result.Eligible++;
                try{long? beforeBytes=measurements==null?null:measurements.WorkingSet(candidate.App);await Task.Run(()=>runtime.Trim(candidate.App),cancel);result.Trimmed++;trimmedApps.Add(candidate.App);long? afterBytes=measurements==null?null:measurements.WorkingSet(candidate.App);result.Observations.Add(new BoostObservation{App=candidate.App.Name,BeforeBytes=beforeBytes??0,ImmediateBytes=afterBytes??0,BeforeMeasured=beforeBytes.HasValue,ImmediateMeasured=afterBytes.HasValue});result.Details.Add(candidate.App.Name+" • Windows accepted working-set trim");progress?.Report("Windows accepted "+result.Trimmed+" trim requests. Checking the result…");}catch(OperationCanceledException){throw;}catch(Exception ex){result.Failed++;result.Details.Add(candidate.App.Name+" • trim declined / unavailable ("+ex.GetType().Name+")");}
            }
            if(result.Trimmed>0){progress?.Report("Measuring again after 3 seconds so refilled memory is included…");await settle(cancel);}
            for(int index=0;index<trimmedApps.Count;index++){
                var item=result.Observations[index];long? after=measurements==null?null:await Task.Run(()=>measurements.WorkingSet(trimmedApps[index]),cancel);
                item.AfterBytes=after??0;item.Measured=item.BeforeMeasured&&after.HasValue;
            }
            result.AvailableAfter=runtime.Available();cooldown.Restart();State.Log("Smart RAM Boost: "+result.Trimmed+" accepted; "+result.ObservedReductions+" decreases at follow-up; available change "+result.AvailableChange+"; "+result.Skipped+" protected/skipped; "+result.Failed+" unavailable.");progress?.Report(result.Summary);return result;
        }finally{busy=false;}
    }
}
}
