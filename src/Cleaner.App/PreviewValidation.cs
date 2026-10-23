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
        using(var monitor=new QuietCare.MonitorController(()=>{},()=>{},Path.Combine(output,"state")))
        using(var process=System.Diagnostics.Process.GetCurrentProcess())
        {
            var coldCpu=process.TotalProcessorTime;var coldClock=System.Diagnostics.Stopwatch.StartNew();
            monitor.Start();await Task.Delay(20000);process.Refresh();
            measurements.Add(new{phase="First 20 seconds including startup initialization",seconds=coldClock.Elapsed.TotalSeconds,cpuPercentOfMachine=(process.TotalProcessorTime-coldCpu).TotalSeconds/coldClock.Elapsed.TotalSeconds/Environment.ProcessorCount*100,workingSetBytes=process.WorkingSet64,privateBytes=process.PrivateMemorySize64});
            foreach(var rate in new[]{1000,250})
            {
                monitor.SetRefresh(rate);await Task.Delay(1000);
                var start=process.TotalProcessorTime;int count=monitor.SamplesCompleted;var sampleCpu=monitor.SamplingCpuMilliseconds;var sampleWall=monitor.SamplingWallMilliseconds;var watch=System.Diagnostics.Stopwatch.StartNew();
                await Task.Delay(12000);process.Refresh();
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
        var pages = new Type[] {typeof(DashboardPage),typeof(SystemCleanerPage),typeof(DeveloperCleanerPage),typeof(DiskAnalyzerPage),typeof(DuplicatesPage),typeof(LargeFilesPage),typeof(LogFinderPage),typeof(FolderComparePage),typeof(OrphanUserDataPage),typeof(CleanupHistoryPage),typeof(AutostartPage),typeof(InstalledProgramsPage),typeof(ServicesPage),typeof(ProcessMonitorPage),typeof(RegistryCleanerPage),typeof(SystemMaintenancePage),typeof(SettingsPage),typeof(AboutPage)};
        var report = new List<object>();
        var window = services.GetRequiredService<MainWindow>();
        window.ShowActivated = false;
        window.Show();
        await Application.Current.Dispatcher.InvokeAsync(() => {}, DispatcherPriority.ApplicationIdle);
        var navigation = (NavigationView)window.FindName("RootNavigation");
        foreach (var type in pages)
        {
            var page = (FrameworkElement)services.GetRequiredService(type);
            bool navigated = navigation.Navigate(type);
            await Task.Delay(80);
            await Application.Current.Dispatcher.InvokeAsync(() => {},DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
            await Application.Current.Dispatcher.InvokeAsync(() => {},DispatcherPriority.Render);
            var shell=(FrameworkElement)window.Content;
            Render(shell,Path.Combine(output,type.Name+".png"),(int)Math.Ceiling(shell.ActualWidth),(int)Math.Ceiling(shell.ActualHeight));
            report.Add(new {page=type.Name,navigated,loaded=page.IsLoaded,visible=page.IsVisible,width=page.ActualWidth,height=page.ActualHeight});
            if(!page.IsLoaded || !page.IsVisible || page.ActualWidth<100)throw new InvalidOperationException("Page did not load: "+type.Name);
            services.GetRequiredService<ProcessMonitorViewModel>().Deactivate();
        }
        window.Hide();
        QuietCare.MonitorController.RenderPreview(Path.Combine(output,"NativeCompactMonitor.png"));
        QuietCare.State.DirectoryPath=Path.Combine(output,"monitor-fixture-state");
        using(var controller=new QuietCare.MonitorController(()=>{},()=>{},Path.Combine(output,"monitor-fixture-state")))
        {
            var monitor=new QuietCare.Widget(controller,new QuietCare.Settings());
            monitor.Update(new QuietCare.Snapshot{Cpu=24,Gpu=7,Ram=65,Total=16UL*1024*1024*1024,Available=6UL*1024*1024*1024});
            var content=(FrameworkElement)monitor.Content;
            content.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
            var size=content.DesiredSize;content.Arrange(new Rect(size));content.UpdateLayout();
            Render(content,Path.Combine(output,"CompactMonitor.png"),(int)Math.Ceiling(size.Width),(int)Math.Ceiling(size.Height));
            report.Add(new{page="CompactMonitor",width=size.Width,height=size.Height,fixture=true});
            monitor.AllowClose=true;monitor.Close();
        }
        File.WriteAllText(Path.Combine(output,"ui-validation.json"),JsonSerializer.Serialize(new{utc=DateTimeOffset.UtcNow,scope="Production control renders; monitor readings are fixtures; no destructive actions invoked",bindingErrors=bindingErrors.Messages.Distinct().ToArray(),pages=report},new JsonSerializerOptions{WriteIndented=true}));
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
