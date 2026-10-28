using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Cleaner.Core.Services;
namespace Cleaner.App.Services;
internal static class FocusWorkerCheck
{
    public static int Run(string output,bool updateGuard=false)
    {
        Directory.CreateDirectory(output);var checks=new List<object>();
        var runtime=new WindowsFocusRuntime(FocusControlEntry.JournalPath);string before=runtime.ReadNotifications();
        Dictionary<string,int?> Policies(){using var policy=new WindowsFocusPolicyRuntime(true);return FocusUpdateGuard.Settings.Keys.ToDictionary(x=>x,x=>policy.Read(x));}
        var beforePolicies=updateGuard?Policies():null;
        using var self=Process.GetCurrentProcess();Guid id=Guid.NewGuid();
        var info=new ProcessStartInfo(Environment.ProcessPath!,$"--focus-worker {id:N} {self.Id} {self.StartTime.ToUniversalTime().Ticks} 1 {(updateGuard?28:12)} 0"){UseShellExecute=false,CreateNoWindow=true};
        using var child=Process.Start(info)!;
        bool ready=false,restored=false,guardApplied=!updateGuard,guardRestored=!updateGuard;double cpu=0;
        try
        {
            for(int i=0;i<60;i++)
            {
                Thread.Sleep(200);var status=FocusControlEntry.Read<FocusStatus>(FocusControlEntry.StatusPath);
                if(status?.Session==id&&status.Active){ready=true;break;}
                if(child.HasExited)break;
            }
            checks.Add(new{name="Production Focus worker starts a bounded session",passed=ready});
            if(ready&&updateGuard)
            {
                for(int i=0;i<180;i++)
                {
                    var backup=WindowsFocusPolicyRuntime.Load();
                    if(backup?.Session==id&&backup.Status.StartsWith("Temporary update policies saved",StringComparison.Ordinal))
                    {guardApplied=Policies()["automatic-updates"]==1;break;}
                    var status=FocusControlEntry.Read<FocusStatus>(FocusControlEntry.StatusPath);
                    if(status?.Session==id&&(status.Updates.Contains("cancelled")||status.Updates.Contains("unavailable")))break;
                    Thread.Sleep(200);
                }
                checks.Add(new{name="Elevated production update guard saves temporary local policies",passed=guardApplied});
            }
            if(ready)
            {
                Thread.Sleep(1000);child.Refresh();var start=child.TotalProcessorTime;var clock=Stopwatch.StartNew();Thread.Sleep(6000);child.Refresh();
                cpu=(child.TotalProcessorTime-start).TotalSeconds/clock.Elapsed.TotalSeconds/Environment.ProcessorCount*100;
            }
        }
        finally
        {
            File.WriteAllText(Path.Combine(FocusControlEntry.DirectoryPath,id.ToString("N")+".stop"),"Explicit native check complete");
            if(!child.WaitForExit(15000))throw new InvalidOperationException("Focus worker did not stop within the expected interval; its bounded session and recovery ledger remain.");
            var journal=FocusControlEntry.Read<FocusJournal>(FocusControlEntry.JournalPath);
            restored=runtime.ReadNotifications()==before&&journal is not null&&!journal.NotificationOwned&&journal.Priorities.Count==0;
            checks.Add(new{name="Production worker restores notifications and exits after Stop",passed=restored&&child.ExitCode==0});
            var ended=FocusControlEntry.Read<FocusStatus>(FocusControlEntry.StatusPath);
            bool endedLabels=ended?.Session==id&&!ended.Active&&ended.Awake.Contains("released")&&ended.Notifications.Contains("No owned notification change remains");
            checks.Add(new{name="Ended session status reports released power and notification ownership",passed=endedLabels});
            restored&=endedLabels;
            if(updateGuard)
            {
                for(int i=0;i<50;i++)
                {
                    var backup=WindowsFocusPolicyRuntime.Load();
                    if(backup?.Session==id&&backup.Values.Count==0){var after=Policies();guardRestored=beforePolicies!.All(x=>after[x.Key]==x.Value);break;}
                    Thread.Sleep(200);
                }
                checks.Add(new{name="Elevated guard restores the exact original local policies after worker exit",passed=guardRestored});
            }
        }
        File.WriteAllText(Path.Combine(output,"focus-worker-check.json"),JsonSerializer.Serialize(new{scope=updateGuard?"Actual production worker and elevated temporary update guard, including policy restoration after Stop. No Defender settings or user-app priorities changed.":"Actual worker session with Do not disturb and keep awake; CPU priorities and update guard deselected. No Defender/update policies or user-app priorities changed.",workerCpuPercentOfMachine=cpu,sampleSeconds=6,checks},new JsonSerializerOptions{WriteIndented=true}));
        return ready&&restored&&guardApplied&&guardRestored&&child.ExitCode==0?0:2;
    }
}
