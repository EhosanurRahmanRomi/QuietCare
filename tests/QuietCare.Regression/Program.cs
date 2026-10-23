using Cleaner.Core.Cleaners;
using Cleaner.Core.Models;
using Cleaner.Core.Services;
using Cleaner.Core.Utils;
using System.Text.Json;
using System.Diagnostics;

if (args[0] == "--wait-child") { Thread.Sleep(TimeSpan.FromMinutes(2)); return; }
var root = Path.GetFullPath(args[0]);
Directory.CreateDirectory(root);
if (Directory.EnumerateFileSystemEntries(root).Any()) throw new Exception("Fixture directory must be empty.");
RecoveryStore.Root = Path.Combine(root, "recovery");
var input = Path.Combine(root, "input"); Directory.CreateDirectory(input);
var checks = new List<object>();
void Check(bool passed, string name) { checks.Add(new { name, passed }); if (!passed) throw new Exception(name); Console.WriteLine("PASS " + name); }
string FileOf(string name, string value = "fixture", bool old = true) { var p = Path.Combine(input,name); File.WriteAllText(p,value); if(old) File.SetLastWriteTimeUtc(p,DateTime.UtcNow.AddDays(-20)); return p; }
bool Throws(Action action) { try { action(); return false; } catch { return true; } }
GlobalCleanupPolicy.MinimumAge = TimeSpan.FromDays(7);
GlobalCleanupPolicy.ExcludeSubstrings = ["protected"];
var target = new FixtureTarget(input);
var oldFile = FileOf("old.tmp"); var recent=FileOf("new.tmp", old:false); var excluded=FileOf("protected.tmp");
var scan=await target.ScanAsync();
Check(scan.Paths.SequenceEqual([oldFile]), "Age and exclusion policy bound preview");
Check(File.ReadAllText(oldFile)=="fixture", "Scan did not mutate content");
var changed=FileOf("changed.tmp"); scan=await target.ScanAsync(); File.AppendAllText(changed,"new");
var clean=await target.CleanAsync(scan,true);
Check(File.Exists(changed)&&clean.Errors.Any(x=>x.Contains("changed.tmp")), "Changed file rejected after preview");
Check(!File.Exists(oldFile)&&clean.FilesDeleted==1&&clean.QuarantinedBytes==7&&clean.FreedBytes==0, "Quarantine moves approved file without claiming freed space");
Check(File.Exists(recent)&&File.Exists(excluded), "Recent and excluded files preserved");
Check((await target.CleanAsync(scan,true)).FilesDeleted==0, "Scan authorization consumed once");
var records=RecoveryStore.List(); Check(records.Count==1, "Recovery metadata survives enumeration");
File.WriteAllText(oldFile,"conflict");
Check(Throws(()=>RecoveryStore.Restore(records[0]))&&File.ReadAllText(oldFile)=="conflict", "Restore refuses to overwrite existing file");
File.Delete(oldFile); RecoveryStore.Restore(records[0]); Check(File.ReadAllText(oldFile)=="fixture"&&RecoveryStore.List().Count==0,"Restore preserves content and removes completed record");
File.SetLastWriteTimeUtc(oldFile,DateTime.UtcNow.AddDays(-20));
var forged=new ScanResult{TargetId=target.Id,Paths=[oldFile],FileCount=1,SizeBytes=7};
Check((await target.CleanAsync(forged,true)).FilesDeleted==0&&File.Exists(oldFile), "Forged scan cannot authorize cleanup");
scan=await target.ScanAsync(); GlobalCleanupPolicy.ExcludeSubstrings=["old.tmp"];
clean=await target.CleanAsync(scan,true);Check(File.Exists(oldFile),"New exclusions rechecked before mutation");
GlobalCleanupPolicy.ExcludeSubstrings=["protected"];
scan=await target.ScanAsync();using(var cancelled=new CancellationTokenSource()){cancelled.Cancel();bool observed=false;try{await target.CleanAsync(scan,true,ct:cancelled.Token);}catch(OperationCanceledException){observed=true;}Check(observed&&File.Exists(oldFile),"Cancelled cleanup leaves input untouched");}
scan=await target.ScanAsync();var another=new FixtureTarget(input);Check((await another.CleanAsync(scan,true)).FilesDeleted==0,"A different target instance cannot consume an approval");
var outside=Path.Combine(root,"outside");Directory.CreateDirectory(outside);File.WriteAllText(Path.Combine(outside,"keep.tmp"),"outside sentinel");
var junction=Path.Combine(input,"linked");
var psi=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"cmd.exe")){UseShellExecute=false,CreateNoWindow=true};psi.ArgumentList.Add("/c");psi.ArgumentList.Add("mklink");psi.ArgumentList.Add("/J");psi.ArgumentList.Add(junction);psi.ArgumentList.Add(outside);
using(var child=Process.Start(psi)!){child.WaitForExit();Check(child.ExitCode==0,"Junction fixture created");}
Check(!SafeEnumerator.EnumerateFiles(input,"*",true).Any(x=>x.Contains("linked")),"Enumerator refuses junction traversal");
Check(!SafeEnumerator.EnumerateFiles(junction,"*",true).Any(),"Linked scan root rejected");
Directory.Delete(junction); // Remove only the verified fixture junction, not its target.
var hard=Path.Combine(input,"hard.tmp");psi.ArgumentList.Clear();foreach(var x in new[]{"/c","mklink","/H",hard,oldFile})psi.ArgumentList.Add(x);
using(var child=Process.Start(psi)!){child.WaitForExit();Check(child.ExitCode==0,"Hardlink fixture created");}
Check(!(await target.ScanAsync()).Paths.Contains(hard),"Multiply linked files excluded from approved preview");File.Delete(hard);
scan=await target.ScanAsync();clean=await target.CleanAsync(scan,true);records=RecoveryStore.List();Check(records.Count>0,"A second quarantine records recovery data");
var purge=records.First();var stored=purge.Stored;RecoveryStore.Purge(purge);Check(!File.Exists(stored),"Permanent deletion removes only selected quarantined file");
Check(File.ReadAllText(Path.Combine(outside,"keep.tmp"))=="outside sentinel","Outside sentinel remains intact");
var svc=new SystemMaintenanceService();var rejected=await svc.RunAsync("cmd.exe","/c exit 0",false,default);Check(rejected.ExitCode==-1,"Maintenance rejects arbitrary shell commands");
rejected=await svc.RunAsync("vssadmin.exe","delete shadows /all /quiet",true,default);Check(rejected.ExitCode==-1,"Restore-point destruction blocked before execution");
Check(!new FileSystemOperations().DeleteDirectory(outside,false)&&Directory.Exists(outside),"Generic recursive directory deletion unavailable");
Check((await new DuplicateFinder().FindAsync([],0)).Count==0,"Empty duplicate-search roots return no results");
var duplicates=Path.Combine(root,"duplicates");Directory.CreateDirectory(duplicates);
File.WriteAllText(Path.Combine(duplicates,"one.bin"),"duplicate fixture");File.WriteAllText(Path.Combine(duplicates,"two.bin"),"duplicate fixture");File.WriteAllText(Path.Combine(duplicates,"other.bin"),"different fixture");
var matches=await new DuplicateFinder().FindAsync([duplicates],0);
Check(matches.Count==1&&matches[0].Paths.Count==2,"Duplicate finder compares SHA-256 content");
var session=new DiskScanner().StartScan(duplicates);await session.Completion;
Check(session.Root.FileCount==3&&session.Root.Size==new DirectoryInfo(duplicates).GetFiles().Sum(f=>f.Length),"Disk analysis reports fixture sizes and counts");
var monitor=new ProcessMonitorService();Check(!monitor.Kill(Environment.ProcessId,Process.GetCurrentProcess().StartTime.ToUniversalTime()),"Process control refuses to terminate QuietCare itself");
Check(!monitor.KillElevated(Environment.ProcessId),"Elevated process termination disabled");
var childInfo=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true};childInfo.ArgumentList.Add("--wait-child");
using(var helper=Process.Start(childInfo)!){try{Thread.Sleep(300);var created=helper.StartTime.ToUniversalTime();Check(!monitor.Kill(helper.Id,created.AddSeconds(-1))&&!helper.HasExited,"Process identity mismatch leaves helper alive");Check(monitor.Kill(helper.Id,created)&&helper.WaitForExit(3000),"Confirmed helper termination uses its verified handle");}finally{if(!helper.HasExited)helper.Kill();}}
await CareChecks.Run(root,Check);
var report=new { utc=DateTimeOffset.UtcNow, scope="Isolated fixtures and read-only Windows-tool signature/resolution checks; no user cleanup, startup changes, antivirus scan or optimization executed", count=checks.Count,checks };
File.WriteAllText(Path.Combine(root,"regression.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"{checks.Count} checks passed.");

sealed class FixtureTarget(string root) : CleanupTargetBase(new FileSystemOperations())
{
 public override string Id=>"fixture";public override string Name=>"Fixture";public override string Description=>"Fixture";public override string IconGlyph=>"";public override CleanupCategory Category=>CleanupCategory.Developer;public override SafetyLevel SafetyLevel=>SafetyLevel.Caution;
 protected override IEnumerable<string> EnumerateCleanupRoots()=>[root];
}
