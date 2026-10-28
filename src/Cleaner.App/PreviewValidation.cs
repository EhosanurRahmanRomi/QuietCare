using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Cleaner.App.ViewModels.Pages;
using Cleaner.App.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Cleaner.App;
internal static class PreviewValidation
{
    public static async Task MonitorSmoke(string output)
    {
        output=Path.GetFullPath(output);Directory.CreateDirectory(output);
        var measurements=new List<object>();
        using var startupWatch=new Services.StartupWatchService(new Services.AppDataService(Path.Combine(output,"watch-state")));
        startupWatch.Start();
        using(var monitor=new QuietCare.MonitorController(()=>{},()=>{},Path.Combine(output,"state-"+Guid.NewGuid().ToString("N"))))
        using(var process=System.Diagnostics.Process.GetCurrentProcess())
        {
            var coldCpu=process.TotalProcessorTime;var coldClock=System.Diagnostics.Stopwatch.StartNew();
            monitor.Start();await Task.Delay(20000);process.Refresh();
            if(monitor.SamplesCompleted==0||monitor.Latest is null)throw new InvalidOperationException("Visible monitor produced no samples.");
            measurements.Add(new{phase="First 20 seconds including startup initialization",seconds=coldClock.Elapsed.TotalSeconds,cpuPercentOfMachine=(process.TotalProcessorTime-coldCpu).TotalSeconds/coldClock.Elapsed.TotalSeconds/Environment.ProcessorCount*100,workingSetBytes=process.WorkingSet64,privateBytes=process.PrivateMemorySize64});
            foreach(var rate in new[]{1000,250})
            {
                monitor.SetRefresh(rate);await Task.Delay(1000);
                var start=process.TotalProcessorTime;int count=monitor.SamplesCompleted;var sampleCpu=monitor.SamplingCpuMilliseconds;var sampleWall=monitor.SamplingWallMilliseconds;var watch=System.Diagnostics.Stopwatch.StartNew();
                await Task.Delay(12000);process.Refresh();
                if(monitor.SamplesCompleted-count<4||monitor.Latest is null)throw new InvalidOperationException("Resource sample had too few real telemetry updates.");
                measurements.Add(new{refreshMs=rate,seconds=watch.Elapsed.TotalSeconds,samples=monitor.SamplesCompleted-count,cpuPercentOfMachine=(process.TotalProcessorTime-start).TotalSeconds/watch.Elapsed.TotalSeconds/Environment.ProcessorCount*100,samplingCpuMs=monitor.SamplingCpuMilliseconds-sampleCpu,samplingWallMs=monitor.SamplingWallMilliseconds-sampleWall,workingSetBytes=process.WorkingSet64,privateBytes=process.PrivateMemorySize64,latest=monitor.Latest,mainWindowCreated=Application.Current.MainWindow is MainWindow});
            }
            monitor.ToggleWidget();
            await Task.Delay(1000);var hiddenStart=process.TotalProcessorTime;var hiddenWatch=System.Diagnostics.Stopwatch.StartNew();await Task.Delay(6000);process.Refresh();
            measurements.Add(new{phase="Monitor hidden",seconds=hiddenWatch.Elapsed.TotalSeconds,cpuPercentOfMachine=(process.TotalProcessorTime-hiddenStart).TotalSeconds/hiddenWatch.Elapsed.TotalSeconds/Environment.ProcessorCount*100,workingSetBytes=process.WorkingSet64});
        }
        File.WriteAllText(Path.Combine(output,"monitor-smoke.json"),JsonSerializer.Serialize(new{utc=DateTimeOffset.UtcNow,scope="Live compact monitor plus startup watch, main UI unopened, no Boost invoked; short local sample, not a benchmark",startupWatchStatus=startupWatch.Status,measurements},new JsonSerializerOptions{WriteIndented=true}));
    }
    // Production controls, rendered without invoking any cleaning/system-change command.
    public static async Task Run(IServiceProvider services, string output)
    {
        output = Path.GetFullPath(output); Directory.CreateDirectory(output);
        var bindingErrors=new BindingErrors();
        System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Add(bindingErrors);
        System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level=System.Diagnostics.SourceLevels.Error;
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);
        Localization.L.Current.Language = "en";
        var pages = new Type[] {typeof(DashboardPage),typeof(FocusPage),typeof(MemoryPage),typeof(ProtectionPage),typeof(StoragePage),typeof(ToolboxPage),typeof(SystemCleanerPage),typeof(DeveloperCleanerPage),typeof(DiskAnalyzerPage),typeof(DuplicatesPage),typeof(LargeFilesPage),typeof(LogFinderPage),typeof(FolderComparePage),typeof(OrphanUserDataPage),typeof(CleanupHistoryPage),typeof(AutostartPage),typeof(InstalledProgramsPage),typeof(ServicesPage),typeof(RegistryCleanerPage),typeof(SystemMaintenancePage),typeof(SettingsPage),typeof(AboutPage)};
        var report = new List<object>();
        var window = services.GetRequiredService<MainWindow>();
        window.ShowActivated = false;
        window.Show();
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);PreviewTheme.Apply(true);
        await Application.Current.Dispatcher.InvokeAsync(() => {}, DispatcherPriority.ApplicationIdle);
        var navigation = (NavigationView)window.FindName("RootNavigation");
        int heartbeatTicks=0;double longestHeartbeatGap=0;
        var heartbeatClock=System.Diagnostics.Stopwatch.StartNew();double lastHeartbeat=0;
        var heartbeat=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(100)};
        heartbeat.Tick+=(_,_)=>{double now=heartbeatClock.Elapsed.TotalMilliseconds;longestHeartbeatGap=Math.Max(longestHeartbeatGap,now-lastHeartbeat);lastHeartbeat=now;heartbeatTicks++;};
        heartbeat.Start();
        await services.GetRequiredService<CareHubViewModel>().RefreshProtectionAsync();
        var displayHub=services.GetRequiredService<CareHubViewModel>();
        displayHub.OnSnapshot(new QuietCare.Snapshot{Cpu=24,Gpu=7,Ram=65,Total=16UL*1024*1024*1024,Available=6UL*1024*1024*1024,Time=DateTime.Now});
        displayHub.ReadingTime="Display fixture · CPU 24% / GPU 7% / RAM 65%";
        foreach (var type in pages)
        {
            var page = (FrameworkElement)services.GetRequiredService(type);
            window.NavigateTo(type);bool navigated=true;
            await Task.Delay(260);
            await Application.Current.Dispatcher.InvokeAsync(() => {},DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
            await Application.Current.Dispatcher.InvokeAsync(() => {},DispatcherPriority.Render);
            var shell=(FrameworkElement)window.Content;
            Render(shell,Path.Combine(output,type.Name+".png"),(int)Math.Ceiling(shell.ActualWidth),(int)Math.Ceiling(shell.ActualHeight));
            report.Add(new {page=type.Name,navigated,loaded=page.IsLoaded,visible=page.IsVisible,width=page.ActualWidth,height=page.ActualHeight});
            if(!page.IsLoaded || !page.IsVisible || page.ActualWidth<100)throw new InvalidOperationException("Page did not load: "+type.Name);
            if(window.FindName("SmartCareActions") is not FrameworkElement{IsVisible:true})throw new InvalidOperationException("Smart Care is not accessible from "+type.Name);
        }
        navigation.Navigate(typeof(DashboardPage));
        var liveAssessment=await new Services.SmartCareBackend(new Services.AppDataService(Path.Combine(output,"care-check-state")),new Cleaner.Core.Services.AutomaticCleanupService()).AssessAsync(default);
        if(liveAssessment.MemoryUsedPercent is not >=0 or >100||liveAssessment.AvailableBytes is null||liveAssessment.CpuPercent is not >=0 or >100)
            throw new InvalidOperationException("Live assessment did not return valid memory/CPU readings.");
        File.WriteAllText(Path.Combine(output,"assessment-adapter.json"),JsonSerializer.Serialize(new{utc=DateTimeOffset.UtcNow,scope="Production assessment adapter read actual memory, CPU, power and Defender status; no system actions started",memoryReadingValid=true,cpuReadingValid=true,powerReadingAvailable=liveAssessment.OnBattery.HasValue,defenderStatusReadCompleted=true}));
        var hub=services.GetRequiredService<CareHubViewModel>();
        hub.OnSnapshot(new QuietCare.Snapshot{Cpu=24,Gpu=7,Ram=65,Total=16UL*1024*1024*1024,Available=6UL*1024*1024*1024,Time=DateTime.Now});
        hub.ReadingTime="Display fixture · CPU 24% / GPU 7% / RAM 65%";
        hub.SmartCareRunning=true;
        hub.SmartCareStatus="Fixture: Checking idle apps and measuring actual memory changes…";
        var careFixture=Cleaner.Core.Services.SmartCareService.InitialSteps();
        for(int i=0;i<careFixture.Count;i++)hub.ApplyCareStep(careFixture[i] with{State=i<2?Cleaner.Core.Services.CareState.Completed:i==2?Cleaner.Core.Services.CareState.Running:Cleaner.Core.Services.CareState.Waiting});
        await Task.Delay(800);window.UpdateLayout();
        var smartShell=(FrameworkElement)window.Content;
        Render(smartShell,Path.Combine(output,"SmartCareProgressFixture.png"),(int)Math.Ceiling(smartShell.ActualWidth),(int)Math.Ceiling(smartShell.ActualHeight));
        hub.SmartCareRunning=false;
        hub.StageTitle="Fixture: care session complete";hub.StageDetail="4 completed, 1 skipped, 1 needs attention.";
        hub.SmartCareStatus="Fixture: Finished: 4 completed, 1 skipped, 1 needs attention. Open steps to review.";
        for(int i=0;i<careFixture.Count;i++)hub.ApplyCareStep(careFixture[i] with{State=i==2?Cleaner.Core.Services.CareState.Review:i==5?Cleaner.Core.Services.CareState.Skipped:Cleaner.Core.Services.CareState.Completed,Detail=i==2?"Fixture: Trims were accepted, but no overall available-RAM gain remained.":careFixture[i].Detail});
        await Task.Delay(150);window.UpdateLayout();
        Render(smartShell,Path.Combine(output,"SmartCareResultFixture.png"),(int)Math.Ceiling(smartShell.ActualWidth),(int)Math.Ceiling(smartShell.ActualHeight));
        hub.OnBoostStatus("Measuring again after 3 seconds so refilled memory is included…",true);
        await Task.Delay(150);window.UpdateLayout();
        var previewShell=(FrameworkElement)window.Content;
        Render(previewShell,Path.Combine(output,"BoostProgressFixture.png"),(int)Math.Ceiling(previewShell.ActualWidth),(int)Math.Ceiling(previewShell.ActualHeight));
        hub.OnBoostStatus("Fixture: available RAM +64 MB after 3 seconds. RAM used: 65.4% → 65.0%.",false);
        await Task.Delay(150);window.UpdateLayout();
        Render(previewShell,Path.Combine(output,"BoostResultFixture.png"),(int)Math.Ceiling(previewShell.ActualWidth),(int)Math.Ceiling(previewShell.ActualHeight));
        hub.Activity.Clear();
        double normalWidth=window.Width,normalHeight=window.Height;
        window.Width=980;window.Height=640;
        await Task.Delay(280);window.UpdateLayout();
        Render(previewShell,Path.Combine(output,"DashboardCompactDark.png"),(int)Math.Ceiling(previewShell.ActualWidth),(int)Math.Ceiling(previewShell.ActualHeight));
        ApplicationThemeManager.Apply(ApplicationTheme.Light);PreviewTheme.Apply(false);
        await Task.Delay(280);window.UpdateLayout();
        Render(previewShell,Path.Combine(output,"DashboardCompactLight.png"),(int)Math.Ceiling(previewShell.ActualWidth),(int)Math.Ceiling(previewShell.ActualHeight));
        foreach(var type in new[]{typeof(FocusPage),typeof(MemoryPage),typeof(ProtectionPage),typeof(StoragePage),typeof(ToolboxPage),typeof(AutostartPage)})
        {
            window.NavigateTo(type);await Task.Delay(280);window.UpdateLayout();Render(previewShell,Path.Combine(output,type.Name+"CompactLight.png"),(int)Math.Ceiling(previewShell.ActualWidth),(int)Math.Ceiling(previewShell.ActualHeight));
        }
        window.NavigateTo(typeof(DashboardPage));
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);PreviewTheme.Apply(true);
        window.Width=normalWidth;window.Height=normalHeight;
        bool motionSetting=CareMotion.Enabled;int transitions=CareMotion.TransitionsStarted;CareMotion.Enabled=false;CareMotion.Enter(previewShell);
        if(CareMotion.TransitionsStarted!=transitions)throw new InvalidOperationException("Reduced-motion override started an animation.");
        CareMotion.Enabled=motionSetting;
        if(CareMotion.Allowed&&CareMotion.TransitionsStarted<5)throw new InvalidOperationException("Expected page transitions did not run.");
        // Observe dispatching for longer than the removed process page's polling interval.
        await Task.Delay(5000);heartbeat.Stop();
        if(heartbeatTicks<20)throw new InvalidOperationException("UI dispatcher heartbeat did not remain active.");
        window.Hide();
        QuietCare.MonitorController.RenderPreview(Path.Combine(output,"NativeCompactMonitor.png"));
        QuietCare.MonitorController.RenderBoostPreview(Path.Combine(output,"FloatingBoostResultFixture.png"));
        QuietCare.State.DirectoryPath=Path.Combine(output,"monitor-fixture-state");
        using(var controller=new QuietCare.MonitorController(()=>{},()=>{},Path.Combine(output,"monitor-fixture-state")))
        {
            controller.RenderSettingsPreview(Path.Combine(output,"MonitorSettingsDark.png"));
            ApplicationThemeManager.Apply(ApplicationTheme.Light);PreviewTheme.Apply(false);
            controller.RenderSettingsPreview(Path.Combine(output,"MonitorSettingsLight.png"));
            ApplicationThemeManager.Apply(ApplicationTheme.Dark);PreviewTheme.Apply(true);
            var monitor=new QuietCare.Widget(controller,new QuietCare.Settings());
            monitor.Update(new QuietCare.Snapshot{Cpu=24,Gpu=7,Ram=65,Total=16UL*1024*1024*1024,Available=6UL*1024*1024*1024});
            var content=(FrameworkElement)monitor.Content;
            content.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
            var size=content.DesiredSize;content.Arrange(new Rect(size));content.UpdateLayout();
            Render(content,Path.Combine(output,"CompactMonitor.png"),(int)Math.Ceiling(size.Width),(int)Math.Ceiling(size.Height));
            report.Add(new{page="CompactMonitor",width=size.Width,height=size.Height,fixture=true});
            monitor.AllowClose=true;monitor.Close();
        }
        File.WriteAllText(Path.Combine(output,"ui-validation.json"),JsonSerializer.Serialize(new{utc=DateTimeOffset.UtcNow,scope="Production control renders, compact dark/light dashboard and monitor settings, reduced-motion override and dispatcher heartbeat; care/Boost displays use fixtures; no destructive actions invoked",compactWidth=980,compactHeight=640,reducedMotionOverridePassed=true,pageTransitionsStarted=CareMotion.TransitionsStarted,heartbeatTicks,longestHeartbeatGapMs=longestHeartbeatGap,bindingErrors=bindingErrors.Messages.Distinct().ToArray(),pages=report},new JsonSerializerOptions{WriteIndented=true}));
    }
    static void Render(FrameworkElement element,string file,int width,int height)
    {
        var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);bitmap.Render(element);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(file);encoder.Save(stream);
    }
    sealed class BindingErrors : System.Diagnostics.TraceListener
    {
        public List<string> Messages {get;}=new();
        public override void Write(string? value){if(value!=null)Messages.Add(value);}
        public override void WriteLine(string? value){if(value!=null)Messages.Add(value);}
    }
}
