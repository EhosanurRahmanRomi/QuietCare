using QuietCare;
using System.Diagnostics;
using System.Text.Json;

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
var result=await new SmartBoost(fake,()=>Task.CompletedTask).Run([],default);
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
var psi=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true};psi.ArgumentList.Add("--memory-child");
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

class Fake:IBoostRuntime,IBoostMeasurements,IBoostReadiness
{
 public List<BoostProcess> Items=[];public List<int> Trimmed=[];public bool KnownAudio=true;public int Active;
 public List<BoostProcess> Capture()=>Items;public int Foreground()=>Active;public AudioActivity Audio()=>new(){Verified=KnownAudio};public void Trim(AppProcess app)=>Trimmed.Add(app.Id);public ulong Available()=>2UL*1024*1024*1024;public long? WorkingSet(AppProcess app)=>Trimmed.Contains(app.Id)?1024:app.Bytes;public string Recheck(BoostProcess app,bool background)=>null;
}
class FixtureRuntime(int pid):IBoostRuntime,IBoostMeasurements
{
 readonly WindowsBoostRuntime native=new();public List<BoostProcess> Capture()=>native.Capture().Where(x=>x.App.Id==pid).ToList();public int Foreground()=>0;public AudioActivity Audio()=>new(){Verified=true};public ulong Available()=>native.Available();public long? WorkingSet(AppProcess app)=>native.WorkingSet(app);
 public void Trim(AppProcess app){if(app.Id!=pid)throw new Exception("Refused non-fixture trim");native.Trim(app);}
}
