using System.Windows;
using Cleaner.App.Helpers;
using Cleaner.App.ViewModels;
using Cleaner.App.ViewModels.Pages;
using Cleaner.App.Views;
using Cleaner.App.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wpf.Ui.Appearance;

namespace Cleaner.App;

public partial class App : Application
{
    private readonly IHost _host;
    public static QuietCare.MonitorController? Monitor { get; private set; }
    public static bool Exiting { get; private set; }
    public static Cleaner.App.Services.StartupWatchService? StartupWatch { get; private set; }
    public static void Navigate(Type page)
    {
        if(Current is not App app)return;
        app.RequestReveal();
        app.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle,new Action(()=>
        {if(app.MainWindow is MainWindow window)window.NavigateTo(page);}));
    }
    private Action? revealRequested;
    private bool revealPending;
    public void RequestReveal(){if(revealRequested is null)revealPending=true;else revealRequested();}

    // Log liegt direkt neben der EXE — leicht zu finden für User & Entwickler.
    private static readonly string LogPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuietCareNext", "app.log");

    public App()
    {
        LogInfo("=========================================================");
        LogInfo($"App startup, PID={Environment.ProcessId}, .NET={Environment.Version}");

        DispatcherUnhandledException += (_, e) =>
        {
            LogException("Dispatcher", e.Exception);
            try
            {
                MessageBox.Show(
                    e.Exception.ToString() + "\n\nLog: " + LogPath,
                    "Cleaner — unerwarteter Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch { /* Window might already be gone */ }
            e.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogException("AppDomain", ex);
                try
                {
                    MessageBox.Show(ex.ToString() + "\n\nLog: " + LogPath,
                        "Cleaner — fatal", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { /* shutdown is happening; best-effort */ }
            }
        };

        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogException("UnobservedTask", e.Exception);
            e.SetObserved();
        };

        _host = new HostBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddCleanerCore();

                // ViewModels
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<CareHubViewModel>();
                services.AddSingleton<Cleaner.App.Services.StartupWatchService>();
                services.AddSingleton<SystemCleanerViewModel>();
                services.AddSingleton<DeveloperCleanerViewModel>();
                services.AddSingleton<DiskAnalyzerViewModel>();
                services.AddSingleton<DuplicatesViewModel>();
                services.AddSingleton<LargeFilesViewModel>();
                services.AddSingleton<LogFinderViewModel>();
                services.AddSingleton<FolderCompareViewModel>();
                services.AddSingleton<OrphanUserDataViewModel>();
                services.AddSingleton<CleanupHistoryViewModel>();
                services.AddSingleton<AutostartViewModel>();
                services.AddSingleton<InstalledProgramsViewModel>();
                services.AddSingleton<ServicesViewModel>();
                services.AddSingleton<RegistryCleanerViewModel>();
                services.AddSingleton<SystemMaintenanceViewModel>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<AboutViewModel>();

                // Views
                services.AddSingleton<MainWindow>();
                services.AddSingleton<DashboardPage>();
                services.AddSingleton<SystemCleanerPage>();
                services.AddSingleton<DeveloperCleanerPage>();
                services.AddSingleton<DiskAnalyzerPage>();
                services.AddSingleton<DuplicatesPage>();
                services.AddSingleton<LargeFilesPage>();
                services.AddSingleton<LogFinderPage>();
                services.AddSingleton<FolderComparePage>();
                services.AddSingleton<OrphanUserDataPage>();
                services.AddSingleton<CleanupHistoryPage>();
                services.AddSingleton<AutostartPage>();
                services.AddSingleton<InstalledProgramsPage>();
                services.AddSingleton<ServicesPage>();
                services.AddSingleton<RegistryCleanerPage>();
                services.AddSingleton<SystemMaintenancePage>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<AboutPage>();
            })
            .Build();

        Services = _host.Services;
    }

    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        await _host.StartAsync();

        // Globale Schutzregeln aus den Einstellungen anwenden und auf Änderungen reagieren.
        var settings = _host.Services.GetRequiredService<AppSettings>();
        ApplyCleanupPolicy(settings);
        settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(AppSettings.ExclusionPatterns) or nameof(AppSettings.CleanMinAgeDays))
                ApplyCleanupPolicy(settings);
        };

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        if (Environment.GetCommandLineArgs().Contains("--monitor-smoke"))
        {
            var arguments = Environment.GetCommandLineArgs();
            try { await PreviewValidation.MonitorSmoke(arguments[Array.IndexOf(arguments,"--monitor-smoke")+1]); }
            catch(Exception ex) { LogException("Monitor smoke",ex); Exiting=true;Shutdown(1);return; }
            Exiting=true;Shutdown();return;
        }
        if (Environment.GetCommandLineArgs().Contains("--validate-ui"))
        {
            var arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, "--validate-ui");
            try { await PreviewValidation.Run(Services, arguments[index + 1]); }
            catch (Exception ex) { LogException("UI validation", ex); Exiting = true; Shutdown(1); return; }
            Exiting = true; Shutdown(); return;
        }
        MainWindow? mainWindow = null;
        void RevealMain()
        {
            if (mainWindow is null)
            {
                InitializeComponent();
                mainWindow = _host.Services.GetRequiredService<MainWindow>();
                MainWindow = mainWindow;
                mainWindow.Closing += (_, args) => { if (!Exiting) { args.Cancel = true; mainWindow.Hide(); } };
                _ = Services.GetRequiredService<CareHubViewModel>().RefreshProtectionAsync();
            }
            mainWindow.Show(); mainWindow.WindowState = WindowState.Normal; mainWindow.Activate();
        }
        Monitor = new QuietCare.MonitorController(RevealMain, () => { Exiting = true; Shutdown(); });
        revealRequested=RevealMain;
        if (!Environment.GetCommandLineArgs().Contains("--no-monitor")) Monitor.Start();
        StartupWatch=Services.GetRequiredService<Cleaner.App.Services.StartupWatchService>();
        StartupWatch.Changed+=changes=>Dispatcher.BeginInvoke(new Action(()=>
        {
            Services.GetRequiredService<CareHubViewModel>().OnStartupChanges(changes);
            Monitor?.ShowStartupNotice(changes.Count+" new or changed startup entries. Click to review.",()=>Navigate(typeof(AutostartPage)));
        }));
        var hub=Services.GetRequiredService<CareHubViewModel>();
        hub.BoostStatus=Monitor.BoostStatus;
        Monitor.BoostStatusChanged+=hub.OnBoostStatus;
        hub.StartupStatus="Startup watch active: registry/folders every 30 seconds; scheduled tasks/services every 5 minutes. First scan establishes a baseline.";
        if(StartupWatch.Recent.Count>0)hub.OnStartupChanges(StartupWatch.Recent.Take(10).ToArray());
        StartupWatch.Start();
        if (revealPending || !Environment.GetCommandLineArgs().Contains("--tray")) RevealMain();
    }

    private static void ApplyCleanupPolicy(AppSettings s)
    {
        Cleaner.Core.Cleaners.GlobalCleanupPolicy.ExcludeSubstrings = s.GetExclusionList();
        Cleaner.Core.Cleaners.GlobalCleanupPolicy.MinimumAge =
            s.CleanMinAgeDays > 0 ? TimeSpan.FromDays(s.CleanMinAgeDays) : null;
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Exiting = true;
        Monitor?.Dispose();
        StartupWatch?.Dispose();
        await _host.StopAsync(TimeSpan.FromSeconds(2));
        _host.Dispose();
        base.OnExit(e);
    }

    public static void LogException(string source, Exception ex)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(LogPath)!;
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(LogPath)!);
            System.IO.File.AppendAllText(LogPath,
                $"[{DateTimeOffset.UtcNow:O}] [EXCEPTION] [{source}]\n{ex}\n\n");
        }
        catch { /* logging must never throw */ }
    }

    public static void LogInfo(string message)
    {
        try
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(LogPath)!);
            System.IO.File.AppendAllText(LogPath,
                $"[{DateTimeOffset.UtcNow:O}] [INFO] {message}\n");
        }
        catch { }
    }
}
