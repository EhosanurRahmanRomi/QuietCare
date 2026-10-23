using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;

namespace QuietCare {
public enum CareState {Waiting,Running,Complete,Partial,Unavailable,Skipped}
public sealed class CareFinding {
    public string Item {get;set;}public string Detail {get;set;}
}
public sealed class CareCheckResult {
    public string Id,Name,Summary;public CareState State=CareState.Complete;public int Count;
    public ScanResult TempFiles;public readonly List<CareFinding> Findings=new List<CareFinding>();
}
public sealed class CareScanReport {
    public DateTime Started=DateTime.UtcNow;public TimeSpan Duration;public bool Stopped;
    public readonly List<CareCheckResult> Results=new List<CareCheckResult>();
}
public sealed class CareProgress {
    public string Id,Name,Detail,CurrentItem;public int Completed,Total;public CareState State;
}
public interface ICareChecks {
    CareCheckResult Check(string id,Action<string,string> progress,CancellationToken cancel);
}
public sealed class CareScanner {
    public static readonly string[] Ids={"temp","startup","memory","storage","apps","protection"};
    public static readonly string[] Names={"Temporary files","Startup entries","Memory usage","Disk space","Installed apps","Windows protection"};
    readonly ICareChecks checks;readonly object gate=new object();CancellationTokenSource current;int busy;
    public CareScanner(ICareChecks source=null){checks=source??new WindowsCareChecks();}
    public void SkipCurrent(){lock(gate){if(current!=null)current.Cancel();}}
    public CareScanReport Run(IEnumerable<string> selected,IProgress<CareProgress> progress,CancellationToken cancel){
        if(Interlocked.CompareExchange(ref busy,1,0)!=0)throw new InvalidOperationException("A scan is already running.");
        var report=new CareScanReport();var watch=Stopwatch.StartNew();
        try{
            var enabled=new HashSet<string>(selected??Ids,StringComparer.Ordinal);int total=Ids.Count(enabled.Contains),completed=0;
            if(total==0)throw new InvalidOperationException("Choose at least one scan category.");
            for(int i=0;i<Ids.Length;i++){
                string id=Ids[i],name=Names[i];
                if(!enabled.Contains(id)){if(progress!=null)progress.Report(new CareProgress{Id=id,Name=name,State=CareState.Skipped,Detail="Not selected",Completed=completed,Total=total});continue;}
                if(cancel.IsCancellationRequested){report.Stopped=true;break;}
                using(var step=CancellationTokenSource.CreateLinkedTokenSource(cancel)){
                    lock(gate)current=step;
                    Action<string,string> inform=(detail,item)=>{if(progress!=null)progress.Report(new CareProgress{Id=id,Name=name,State=CareState.Running,Detail=detail,CurrentItem=item,Completed=completed,Total=total});};
                    inform("Checking "+name.ToLowerInvariant()+"…","");CareCheckResult result;
                    try{result=checks.Check(id,inform,step.Token);step.Token.ThrowIfCancellationRequested();}
                    catch(OperationCanceledException){if(cancel.IsCancellationRequested){report.Stopped=true;break;}result=new CareCheckResult{State=CareState.Skipped,Summary="Skipped at your request."};}
                    catch(Exception ex){result=new CareCheckResult{State=CareState.Unavailable,Summary="This check could not finish ("+ex.GetType().Name+"). Review the related Windows tool."};}
                    finally{lock(gate)current=null;}
                    result.Id=id;result.Name=name;report.Results.Add(result);completed++;
                    if(progress!=null)progress.Report(new CareProgress{Id=id,Name=name,State=result.State,Detail=result.Summary,Completed=completed,Total=total});
                }
            }
            report.Duration=watch.Elapsed;return report;
        }finally{Interlocked.Exchange(ref busy,0);}
    }
}
public sealed class WindowsCareChecks : ICareChecks {
    readonly Cleaner cleaner;
    public WindowsCareChecks(Cleaner source=null){cleaner=source??new Cleaner();}
    public CareCheckResult Check(string id,Action<string,string> progress,CancellationToken cancel){
        cancel.ThrowIfCancellationRequested();var result=new CareCheckResult();
        switch(id){
            case "temp":
                var scan=cleaner.Scan(cancel,p=>progress(p.Examined+" entries examined · "+p.Candidates+" candidates · "+Format.Bytes(p.Bytes),p.Path));
                result.TempFiles=scan;result.Count=scan.Items.Count;result.State=scan.Limited?CareState.Partial:CareState.Complete;
                result.Summary=scan.Items.Count+" old-temp candidates · "+Format.Bytes(scan.Bytes)+(scan.Limited?" · scan limit reached":"")+". Review before quarantine.";
                foreach(var item in scan.Items.Take(200))result.Findings.Add(new CareFinding{Item=Path.GetFileName(item.Path),Detail=item.Size+" · "+item.Path});break;
            case "startup":
                var startup=Startup.List();cancel.ThrowIfCancellationRequested();result.Count=startup.Count(x=>x.Backup==null);
                result.Summary=result.Count+" registered/folder entries; "+startup.Count(x=>x.Backup!=null)+" QuietCare backups. Presence does not prove effective enabled state.";
                foreach(var item in startup){cancel.ThrowIfCancellationRequested();progress("Reviewing startup registration",item.Name);result.Findings.Add(new CareFinding{Item=item.Name,Detail=item.Source+" · "+item.Status});}break;
            case "memory":
                var memory=Native.Memory();var apps=Processes.List();cancel.ThrowIfCancellationRequested();result.Count=apps.Count;
                double used=100.0*(memory.Total-memory.Available)/memory.Total;result.Summary=used.ToString("F0")+"% physical RAM used · "+Format.Bytes(memory.Available)+" available. "+apps.Count+" accessible session processes; none changed.";
                foreach(var app in apps.Take(40)){cancel.ThrowIfCancellationRequested();progress("Reading process working sets; no trimming",app.Name);result.Findings.Add(new CareFinding{Item=app.Name+" ("+app.Id+")",Detail=app.Memory+" resident working set"});}break;
            case "storage":
                foreach(var drive in DriveInfo.GetDrives()){
                    cancel.ThrowIfCancellationRequested();if(drive.DriveType!=DriveType.Fixed)continue;
                    try{if(!drive.IsReady)continue;progress("Reading fixed-drive free space",drive.Name);double free=100.0*drive.AvailableFreeSpace/drive.TotalSize;result.Findings.Add(new CareFinding{Item=drive.Name,Detail=Format.Bytes(drive.AvailableFreeSpace)+" free of "+Format.Bytes(drive.TotalSize)+" ("+free.ToString("F0")+"% free)"});}
                    catch(IOException){result.State=CareState.Partial;}
                }
                result.Count=result.Findings.Count;result.Summary=result.Count+" fixed drives checked for available space. No disk repair or file changes performed.";break;
            case "apps":
                var programs=Programs.List();cancel.ThrowIfCancellationRequested();result.Count=programs.Count;result.Summary=programs.Count+" desktop programs inventoried. This is not an outdated-software or vulnerability scan.";
                foreach(var app in programs.Take(200)){cancel.ThrowIfCancellationRequested();progress("Reading installed desktop programs",app.Name);result.Findings.Add(new CareFinding{Item=app.Name,Detail=app.Version+" · "+app.Publisher});}break;
            case "protection":
                progress("Reading Defender status; no malware scan is started", "Windows Defender WMI status");
                var options=new EnumerationOptions{ReturnImmediately=true,Timeout=TimeSpan.FromSeconds(4)};
                using(var search=new ManagementObjectSearcher(@"root\Microsoft\Windows\Defender","SELECT AntivirusEnabled,RealTimeProtectionEnabled,AntivirusSignatureAge FROM MSFT_MpComputerStatus",options))using(var rows=search.Get())foreach(ManagementObject row in rows)using(row){
                    cancel.ThrowIfCancellationRequested();result.Findings.Add(new CareFinding{Item="Defender antivirus",Detail=Convert.ToBoolean(row["AntivirusEnabled"])?"Reported enabled":"Reported inactive; another provider may be in use"});
                    result.Findings.Add(new CareFinding{Item="Defender real-time protection",Detail=Convert.ToBoolean(row["RealTimeProtectionEnabled"])?"Reported enabled":"Reported inactive; review Windows Security"});
                    result.Findings.Add(new CareFinding{Item="Defender definitions",Detail="Signature age reported: "+Convert.ToString(row["AntivirusSignatureAge"])+" days"});
                }
                if(result.Findings.Count==0){result.State=CareState.Unavailable;result.Summary="Defender status is unavailable. Review Windows Security for your protection provider.";}
                else result.Summary="Read Defender protection status. This is not a malware scan or a complete security assessment.";result.Count=result.Findings.Count;break;
            default:throw new InvalidOperationException("Unknown scan category.");
        }
        return result;
    }
}
}
