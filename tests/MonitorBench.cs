using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using QuietCare;

class MonitorBench {
    [STAThread]static int Main(string[] args){
        int rate=int.Parse(args[0]),seconds=int.Parse(args[1]);bool hidden=args[2]=="hidden";State.DirectoryPath=args[3];Directory.CreateDirectory(State.DirectoryPath);
        var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Theme.Install(app);var window=new MainWindow(new Settings{ShowWidget=!hidden,RefreshMs=rate,CloseToTray=false},true);app.MainWindow=window;window.StartInTray();
        var process=Process.GetCurrentProcess();var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(5)};double initial=0;var watch=new Stopwatch();bool warmed=false;
        timer.Tick+=(s,e)=>{
            if(!warmed){initial=process.TotalProcessorTime.TotalSeconds;watch.Start();warmed=true;timer.Interval=TimeSpan.FromSeconds(seconds);return;}
            timer.Stop();process.Refresh();double duration=watch.Elapsed.TotalSeconds,cpu=process.TotalProcessorTime.TotalSeconds-initial;var c=CultureInfo.InvariantCulture;
            Console.WriteLine("{\"mode\":\""+(hidden?"fully_hidden":"compact_monitor")+"\",\"refreshMs\":"+rate+",\"elapsedSeconds\":"+duration.ToString("F2",c)+",\"cpuSeconds\":"+cpu.ToString("F3",c)+",\"totalCpuPercent\":"+(100*cpu/duration/Environment.ProcessorCount).ToString("F3",c)+",\"workingSetMB\":"+(process.WorkingSet64/1048576.0).ToString("F1",c)+",\"privateMB\":"+(process.PrivateMemorySize64/1048576.0).ToString("F1",c)+",\"mainUiConstructed\":"+window.ShellBuilt.ToString().ToLowerInvariant()+",\"pollingEnabled\":"+window.SamplingScheduled.ToString().ToLowerInvariant()+"}");
            window.Exit();
        };timer.Start();app.Run();return 0;
    }
}
