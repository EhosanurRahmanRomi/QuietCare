using QuietCare;
using System.Diagnostics;
using System.Text.Json;

if(args[0]=="--live-boost-check")
{
    var liveOutput=Path.GetFullPath(args[1]);Directory.CreateDirectory(liveOutput);
    State.DirectoryPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QuietCareNext","Monitor");
    var preferences=Settings.Load();State.DirectoryPath=Path.Combine(liveOutput,"state");
    using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(45));
    var measured=await new SmartBoost().Run(preferences.ProtectedApps,deadline.Token,preferences.BoostBackgroundBrowsers,expandedIdleApps:true);
    File.WriteAllText(Path.Combine(liveOutput,"private-boost-details.txt"),measured.Report);
    var receipt=new{utc=DateTimeOffset.UtcNow,scope="One live guarded Boost pass on eligible idle user-session apps; active work, media, project tools and exclusions protected; no app termination, tab discard or file cleanup",measured.Trimmed,measured.Skipped,measured.Failed,measured.Eligible,measured.MediaVerified,measured.TotalMemory,measured.AvailableBefore,measured.AvailableAfter,measured.AvailableDelta,measured.ObservedReductions,measured.ResidentReduction,measured.Summary,skipReasons=measured.SkipReasons};
    var json=JsonSerializer.Serialize(receipt,new JsonSerializerOptions{WriteIndented=true});File.WriteAllText(Path.Combine(liveOutput,"live-boost-summary.json"),json);Console.WriteLine(json);return;
}
if(args[0]=="--memory-child")
{
    byte[] data=new byte[96*1024*1024];long expected=0;
    for(int i=0;i<data.Length;i++){data[i]=(byte)(i%251);expected+=data[i];}
    Console.WriteLine("READY");Console.ReadLine();long actual=0;foreach(byte value in data)actual+=value;GC.KeepAlive(data);
    Console.WriteLine(actual==expected?"PRESERVED":"CORRUPTED");return;
}
var output=Path.GetFullPath(args[0]);Directory.CreateDirectory(output);State.DirectoryPath=Path.Combine(output,"state");
var tests=new List<object>();
void Check(bool value,string name){tests.Add(new{name,passed=value});if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
var gpu=Telemetry.AggregateGpu([new("pid_1_luid_0x0_0x123_phys_0_eng_0_engtype_3D",25),new("pid_2_luid_0x0_0x123_phys_0_eng_0_engtype_3D",30),new("pid_1_luid_0x0_0x123_phys_0_eng_1_engtype_Copy",20)]);
Check(gpu==55,"GPU sums matching engines and chooses the busiest engine");
Check(Telemetry.AggregateGpu([])==null,"Missing GPU readings remain unavailable");
BoostProcess App(int id,string name)=>new(){App=new(){Id=id,Name=name,Path=@"C:\FixtureApps\"+name+".exe",Bytes=96*1024*1024,Started=DateTime.UtcNow.AddHours(-1)},CpuMs=10};
var fake=new Fake();fake.Items=[App(12345,"background-fixture"),App(12346,"code")];
var result=await new SmartBoost(fake,()=>Task.CompletedTask,_=>Task.CompletedTask).Run([],default);
Check(result.Trimmed==1&&result.Skipped==1&&fake.Trimmed.SequenceEqual([12345]),"Only idle fixture qualifies; editor protected");
fake=new Fake{KnownAudio=false,Items=[App(12345,"background-fixture")]};result=await new SmartBoost(fake,()=>Task.CompletedTask).Run([],default);
Check(result.Trimmed==0&&!result.MediaVerified,"Unknown audio state prevents all trimming");
fake=new Fake{Active=12345,Items=[App(12345,"background-fixture")]};result=await new SmartBoost(fake,()=>Task.CompletedTask).Run([],default);
Check(result.Trimmed==0,"Foreground app is protected");
fake=new Fake{Items=[App(12345,"background-fixture")]};result=await new SmartBoost(fake,()=>Task.CompletedTask).Run(["background-fixture"],default);
Check(result.Trimmed==0,"User exclusion prevents trimming");
var browser=App(12345,"chrome");browser.CommandLine="chrome.exe --type=renderer";browser.BackgroundPriority=false;
fake=new Fake{Active=12345,Items=[browser]};result=await new SmartBoost(fake,()=>Task.CompletedTask).Run([],default,true);
Check(result.Trimmed==0,"Foreground browser renderer without idle priority protected");
fake=new Fake{Items=[App(12345,"background-fixture")]};
result=await new SmartBoost(fake,()=>Task.CompletedTask,_=>{fake.Refilled=true;return Task.CompletedTask;}).Run([],default);
Check(result.Trimmed==1&&result.Observations[0].ImmediateBytes==1024&&result.ObservedReductions==0,"Immediate working-set drop is rechecked after settling");
Check(result.ResidentReduction==0&&result.Summary.Contains("No overall available-RAM gain remained")&&result.Observations[0].Outcome.Contains("No lasting"),"Refilled pages are not reported as a lasting RAM reduction");
fake=new Fake{Items=[App(12345,"background-fixture")],AvailableAtEnd=2UL*1024*1024*1024+64UL*1024*1024};
result=await new SmartBoost(fake,()=>Task.CompletedTask,_=>Task.CompletedTask).Run([],default);
Check(result.AvailableDelta==64L*1024*1024&&result.Summary.Contains("+64"),"Small actual available-RAM changes are shown in bytes instead of whole percentages");
Check(result.Report.Contains("shared pages may overlap")&&result.Report.Contains("not total RAM freed"),"Process working-set sums are distinguished from total RAM reclaimed");
fake=new Fake{Items=[App(12345,"background-fixture")],AvailableAtEnd=2UL*1024*1024*1024-32UL*1024*1024};
result=await new SmartBoost(fake,()=>Task.CompletedTask,_=>Task.CompletedTask).Run([],default);
Check(result.AvailableDelta==-32L*1024*1024&&result.Summary.Contains("No overall available-RAM gain remained"),"An overall available-RAM decrease cannot be displayed as a gain");
fake=new Fake{Items=[App(12345,"background-fixture")]};using(var cancelled=new CancellationTokenSource())
{
    cancelled.Cancel();bool observed=false;try{await new SmartBoost(fake).Run([],cancelled.Token);}catch(OperationCanceledException){observed=true;}
    Check(observed&&fake.Trimmed.Count==0,"Cancelled Boost makes no trim requests");
}
var psi=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true};psi.ArgumentList.Add("--memory-child");
var visible=App(12345,"idle-visible-fixture");visible.VisibleWindow=true;
fake=new Fake{Items=[visible]};result=await new SmartBoost(fake,()=>Task.CompletedTask,_=>Task.CompletedTask).Run([],default);
Check(result.Trimmed==0,"Conservative policy continues to exclude visible background windows");
fake=new Fake{Items=[visible]};result=await new SmartBoost(fake,()=>Task.CompletedTask,_=>Task.CompletedTask).Run([],default,expandedIdleApps:true);
Check(result.Trimmed==1,"Expanded idle policy includes an inactive visible app without closing its window");
fake=new Fake{Active=12345,Items=[visible]};result=await new SmartBoost(fake,()=>Task.CompletedTask,_=>Task.CompletedTask).Run([],default,expandedIdleApps:true);
Check(result.Trimmed==0,"Expanded policy still protects the foreground app");
fake=new Fake{CpuDelta=15,Items=[visible]};result=await new SmartBoost(fake,()=>Task.CompletedTask,_=>Task.CompletedTask).Run([],default,expandedIdleApps:true);
Check(result.Trimmed==0,"Expanded policy applies a stricter observed CPU-idle requirement");
fake=new Fake{Playing=12345,Items=[visible]};result=await new SmartBoost(fake,()=>Task.CompletedTask,_=>Task.CompletedTask).Run([],default,expandedIdleApps:true);
Check(result.Trimmed==0,"Expanded policy protects media-playing background apps");
fake=new Fake{KnownAudio=false,Items=[visible]};result=await new SmartBoost(fake,()=>Task.CompletedTask,_=>Task.CompletedTask).Run([],default,expandedIdleApps:true);
Check(result.Trimmed==0,"Expanded policy cannot bypass unavailable media verification");
fake=new Fake{Items=[App(12345,"code"),App(12346,"msmpeng"),App(12347,"quietcare.next")]};result=await new SmartBoost(fake,()=>Task.CompletedTask,_=>Task.CompletedTask).Run([],default,expandedIdleApps:true);
Check(result.Trimmed==0,"Expanded policy preserves project tools, antivirus and QuietCare itself");
fake=new Fake{Items=Enumerable.Range(13000,35).Select(id=>App(id,"bounded-idle-fixture")).ToList()};
result=await new SmartBoost(fake,()=>Task.CompletedTask,_=>Task.CompletedTask).Run([],default,expandedIdleApps:true);
Check(result.Trimmed==32&&result.Details.Any(x=>x.Contains("bounded work limit")),"Expanded high-pressure run is bounded at 32 accepted trims");
fake=new Fake{InitialAvailable=6UL*1024*1024*1024,Items=Enumerable.Range(13000,30).Select(id=>App(id,"bounded-idle-fixture")).ToList()};
result=await new SmartBoost(fake,()=>Task.CompletedTask,_=>Task.CompletedTask).Run([],default,expandedIdleApps:true);
Check(result.Trimmed==24,"Expanded run with lower memory pressure is bounded at 24 accepted trims");
var fresh=App(12345,"new-fixture");fresh.App.Started=DateTime.UtcNow.AddSeconds(-20);
fake=new Fake{Items=[fresh]};result=await new SmartBoost(fake,()=>Task.CompletedTask,_=>Task.CompletedTask).Run([],default,expandedIdleApps:true);
Check(result.Trimmed==0,"Expanded policy leaves newly started apps time to initialize");
fake=new Fake{Items=[App(12345,"cooldown-fixture")]};var cooled=new SmartBoost(fake,()=>Task.CompletedTask,_=>Task.CompletedTask);await cooled.Run([],default);
bool cooling=false;try{await cooled.Run([],default);}catch(InvalidOperationException){cooling=true;}
Check(cooling&&cooled.ReadyInSeconds>0&&fake.Trimmed.Count==1,"RAM cooldown prevents repeated trimming and exposes the remaining wait");
long before=0,after=0;
using(var helper=Process.Start(psi)!)
{
    try
    {
        Check(helper.StandardOutput.ReadLine()=="READY","96 MB memory helper initialized");await Task.Delay(5200);helper.Refresh();before=helper.WorkingSet64;
        var runtime=new FixtureRuntime(helper.Id);result=await new SmartBoost(runtime).Run([],default);helper.Refresh();after=helper.WorkingSet64;
        Check(result.Trimmed==1&&result.ObservedReductions==1,"Live working-set reduction measured in dedicated helper");
        helper.StandardInput.WriteLine("verify");Check(helper.StandardOutput.ReadLine()=="PRESERVED"&&helper.WaitForExit(5000)&&helper.ExitCode==0,"All 96 MB of allocated helper data preserved");
    }
    finally{if(!helper.HasExited)helper.Kill();}
}
File.WriteAllText(Path.Combine(output,"boost-regression.json"),JsonSerializer.Serialize(new{utc=DateTimeOffset.UtcNow,count=tests.Count,scope="Synthetic policy checks and one dedicated memory helper; no user application trimmed",beforeBytes=before,afterBytes=after,tests},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"{tests.Count} Boost checks passed; helper working set {before} -> {after} bytes.");

class Fake:IBoostRuntime,IBoostMeasurements,IBoostReadiness,IBoostMemoryStatus
{
 public List<BoostProcess> Items=[];public List<int> Trimmed=[];public bool KnownAudio=true,Refilled;public int Active,Reads,Playing,Captures;public ulong? AvailableAtEnd;public ulong InitialAvailable=2UL*1024*1024*1024;public double CpuDelta;
 public List<BoostProcess> Capture(){Captures++;return Items.Select(x=>new BoostProcess{App=x.App,CpuMs=x.CpuMs+(Captures>1?CpuDelta:0),VisibleWindow=x.VisibleWindow,BackgroundPriority=x.BackgroundPriority,CommandLine=x.CommandLine}).ToList();}
 public int Foreground()=>Active;public AudioActivity Audio()=>new(){Verified=KnownAudio,ActiveProcesses={Playing}};public void Trim(AppProcess app)=>Trimmed.Add(app.Id);public ulong Available()=>Reads++==0?InitialAvailable:AvailableAtEnd??InitialAvailable;public long? WorkingSet(AppProcess app)=>Trimmed.Contains(app.Id)&&!Refilled?1024:app.Bytes;public string Recheck(BoostProcess app,bool background)=>null;public ulong TotalMemory()=>16UL*1024*1024*1024;
}
class FixtureRuntime(int pid):IBoostRuntime,IBoostMeasurements
{
 readonly WindowsBoostRuntime native=new();public List<BoostProcess> Capture()=>native.Capture().Where(x=>x.App.Id==pid).ToList();public int Foreground()=>0;public AudioActivity Audio()=>new(){Verified=true};public ulong Available()=>native.Available();public long? WorkingSet(AppProcess app)=>native.WorkingSet(app);
 public void Trim(AppProcess app){if(app.Id!=pid)throw new Exception("Refused non-fixture trim");native.Trim(app);}
}
