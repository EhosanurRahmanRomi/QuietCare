using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace QuietCare;

// This library only observes performance until the user explicitly requests Boost.
public sealed class MonitorController : IDisposable
{
    readonly Action open, exit;
    readonly Settings settings;
    readonly DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background);
    readonly SmartBoost booster = new SmartBoost();
    readonly CancellationTokenSource shutdown = new CancellationTokenSource();
    Telemetry telemetry;
    Widget widget;
    NativeCompactMonitor compact;
    NativeBoostNotice boostNotice;
    Forms.NotifyIcon tray;
    System.Drawing.Icon appIcon;
    bool sampling, disposed, dashboardVisible;
    string lastResult = "No RAM Boost has run in this session.";
    Window preferences;
    Action startupReview;
    public int SamplesCompleted { get; private set; }
    public Snapshot Latest { get; private set; }
    public string BoostStatus {get;private set;}="Ready. Boost checks idle background apps and reports the measured result after three seconds.";
    public event Action<string,bool> BoostStatusChanged;
    public event Action<Snapshot> SnapshotUpdated;
    public int BoostReadyInSeconds=>booster.ReadyInSeconds;
    public void ShowStartupNotice(string text,Action review)
    {
        if(tray==null)return;
        startupReview=review;
        tray.ShowBalloonTip(8000,"QuietCare — startup change",text,Forms.ToolTipIcon.Warning);
    }
    public double SamplingCpuMilliseconds { get; private set; }
    public double SamplingWallMilliseconds { get; private set; }

    public MonitorController(Action openApp, Action exitApp, string dataDirectory = null)
    {
        open = openApp; exit = exitApp;
        State.DirectoryPath = dataDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuietCareNext", "Monitor");
        settings = Settings.Load();
        try{var saved=State.Read(State.FilePath("last-boost.xml")).Root;lastResult=(string)saved.Element("report")??lastResult;BoostStatus=(string)saved.Element("summary")??BoostStatus;}catch(Exception){}
        timer.Interval = TimeSpan.FromMilliseconds(settings.RefreshMs);
        timer.Tick += async (_, _) => await Sample();
    }

    public void Start()
    {
        if (tray != null || disposed) return;
        if(Environment.GetCommandLineArgs().Contains("--monitor"))settings.ShowWidget=true;
        try{appIcon=System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath);}catch(Exception){appIcon=null;}
        tray = new Forms.NotifyIcon { Text = "QuietCare", Icon = appIcon??System.Drawing.SystemIcons.Application, Visible = true };
        tray.BalloonTipClicked+=(_,_)=>{var review=startupReview;startupReview=null;review?.Invoke();};
        tray.DoubleClick += (_, _) => open();
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open QuietCare", null, (_, _) => open());
        menu.Items.Add("Show / hide monitor", null, (_, _) => ToggleWidget());
        menu.Items.Add("Monitor settings", null, (_, _) => Reveal("Settings"));
        menu.Items.Add("Exit", null, (_, _) => Exit());
        tray.ContextMenuStrip = menu;
        if (settings.ShowWidget) EnsureWidget();
        RefreshMonitor();
    }

    void EnsureWidget()
    {
        if(widget != null && widget.IsExpanded) { widget.Show(); return; }
        compact ??= new NativeCompactMonitor(this,settings);
        compact.Show(); if(Latest != null) compact.Update(Latest);
    }

    public void OpenDetails()
    {
        compact?.Hide();
        widget ??= new Widget(this,settings);
        widget.Show();if(!widget.IsExpanded)widget.ToggleExpanded();
        RefreshMonitor();
    }
    public void DetailsCollapsed(){widget?.Hide();EnsureWidget();RefreshMonitor();}
    public void ShowBoostNotice(string message)
    {
        startupReview=null;
        if(tray!=null)tray.ShowBalloonTip(8000,"QuietCare RAM Boost",message,Forms.ToolTipIcon.Info);
    }
    public static void RenderPreview(string path)
    {
        using var bitmap=NativeCompactMonitor.Draw(new Snapshot{Cpu=24,Gpu=7,Ram=65},.48);
        bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);
    }
    public static void RenderBoostPreview(string path)=>NativeBoostNotice.RenderPreview(path);

    public void ToggleWidget()
    {
        settings.ShowWidget = !(compact?.Visible==true || widget?.IsVisible==true);
        if (settings.ShowWidget) EnsureWidget(); else { widget?.Hide();compact?.Hide(); }
        settings.Save(); RefreshMonitor();
    }

    public void ShowMonitor()
    {
        Start();settings.ShowWidget=true;if(widget?.IsExpanded==true)widget.ToggleExpanded();EnsureWidget();settings.Save();RefreshMonitor();
    }

    public void ShowReadingsInfo()=>MessageBox.Show("CPU: processor busy time over the sampling interval. Task Manager may use frequency-adjusted processor utility, so its percentage can differ.\n\nGPU: the busiest Windows GPU engine across adapters. It may belong to either GPU on a multi-GPU PC.\n\nRAM: Windows' approximate physical-memory usage. Available memory includes reusable standby pages.\n\nRefresh speed, whole-number rounding and driver support affect comparisons. A dash means the counter is unavailable.","About meter readings",MessageBoxButton.OK,MessageBoxImage.Information);

    public void SetRefresh(int ms)
    {
        settings.RefreshMs = Math.Max(100, Math.Min(10000, ms));
        timer.Interval = TimeSpan.FromMilliseconds(settings.RefreshMs);
        settings.Save();
    }

    public void SetDashboardVisible(bool visible){dashboardVisible=visible;RefreshMonitor();}

    public void RefreshMonitor()
    {
        if (dashboardVisible || compact?.Visible==true || widget?.IsVisible==true) { timer.Start(); _ = Sample(); }
        else timer.Stop();
    }

    async Task Sample()
    {
        if (sampling || disposed || !(dashboardVisible || compact?.Visible==true || widget?.IsVisible==true)) return;
        sampling = true;
        bool detailed = widget?.IsExpanded==true && widget.IsVisible;
        try
        {
            var value = await Task.Run(() => {
                using var process=System.Diagnostics.Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;var watch=System.Diagnostics.Stopwatch.StartNew();
                telemetry ??= new Telemetry();var snapshot=telemetry.Sample(detailed);
                SamplingCpuMilliseconds+=(process.TotalProcessorTime-cpu).TotalMilliseconds;SamplingWallMilliseconds+=watch.Elapsed.TotalMilliseconds;return snapshot;
            });
            if (!disposed) { Latest = value; SamplesCompleted++; compact?.Update(value); if(widget?.IsVisible==true)widget.Update(value);SnapshotUpdated?.Invoke(value); }
        }
        catch (Exception ex) { State.Log("Monitor: " + ex.Message); }
        finally { sampling = false; if (disposed) { telemetry?.Dispose(); telemetry = null; } }
    }

    public async void BoostRam()
    {
        try{await BoostRamAsync();}catch(OperationCanceledException){}catch(Exception ex){State.Log("Boost: "+ex.Message);}
    }
    public async Task<BoostResult> BoostRamAsync(CancellationToken ct=default,IProgress<string> progress=null)
    {
        if(disposed)throw new ObjectDisposedException(nameof(MonitorController));
        if(booster.Busy)throw new InvalidOperationException("RAM Boost is already running.");
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(ct,shutdown.Token);
        compact?.SetBusy(true);
        void Feedback(string text,bool busy)
        {
            BoostStatus=text;BoostStatusChanged?.Invoke(text,busy);
            if(widget?.IsVisible==true)widget.BoostFeedback(text,busy);
            if(compact?.Visible==true)
            {
                if(boostNotice==null||boostNotice.IsDisposed)boostNotice=new NativeBoostNotice(()=>Reveal("Boost results"));
                boostNotice.UpdateResult(text,busy,compact.Bounds);
            }
        }
        Feedback("Checking idle background processes…",true);
        try
        {
            var result = await booster.Run(settings.ProtectedApps,linked.Token,settings.BoostBackgroundBrowsers,new Progress<string>(text=>{if(!disposed&&booster.Busy){Feedback(text,true);progress?.Report(text);}}),expandedIdleApps:true);
            lastResult=result.Report;
            try{State.Save(State.FilePath("last-boost.xml"),new System.Xml.Linq.XDocument(new System.Xml.Linq.XElement("boost",new System.Xml.Linq.XAttribute("utc",DateTimeOffset.UtcNow.ToString("O")),new System.Xml.Linq.XElement("summary",result.Summary),new System.Xml.Linq.XElement("report",lastResult))));}catch(Exception ex){State.Log("Boost result could not be saved: "+ex.Message);}
            if (!disposed) {Feedback(result.Summary,false);await Sample();}
            return result;
        }
        catch (OperationCanceledException) {if(!disposed)Feedback("RAM Boost stopped. Completed trims are safe; pages remain available to their apps.",false);throw;}
        catch (Exception ex) { lastResult = ex.Message; if (!disposed) Feedback(ex.Message,false);throw; }
        finally { if(!disposed)compact?.SetBusy(false); }
    }

    public void Reveal(string page = null,bool activate=true)
    {
        if (page == "Boost results") { MessageBox.Show(lastResult, "RAM Boost results"); return; }
        if (page != "Settings") { open(); return; }
        if (preferences != null) { preferences.Activate(); return; }
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = "Monitor settings", FontSize = 24, Margin = new Thickness(0,0,0,20) });
        panel.Children.Add(new TextBlock { Text = "Refresh interval (100–10,000 milliseconds)" });
        var speed = new ComboBox { Margin = new Thickness(0,8,0,18), ItemsSource = new[] {100,250,500,1000,2000,5000,10000}, SelectedItem = settings.RefreshMs };
        speed.Foreground=System.Windows.Media.Brushes.Black;speed.Background=System.Windows.Media.Brushes.White;
        speed.SelectionChanged += (_, _) => { if (speed.SelectedItem is int ms) SetRefresh(ms); };
        panel.Children.Add(speed);
        panel.Children.Add(new TextBlock { Text = "Background opacity" });
        var opacity = new Slider { Minimum = .15, Maximum = 1, Value = settings.WidgetOpacity, Margin = new Thickness(0,8,0,18) };
        opacity.ValueChanged += (_, _) => { settings.WidgetOpacity = opacity.Value; settings.Save(); widget?.ApplyTransparency();compact?.ApplySettings(); };
        panel.Children.Add(opacity);
        var top = new CheckBox { Content = "Keep monitor above other windows", IsChecked = settings.Topmost, Margin = new Thickness(0,0,0,16) };
        top.Click += (_, _) => { settings.Topmost = top.IsChecked == true; if (widget != null) widget.Topmost = settings.Topmost;compact?.ApplySettings(); settings.Save(); };
        panel.Children.Add(top);
        var startup = new CheckBox { Content = "Start QuietCare monitor when I sign in", IsChecked = StartupEnabled(), Margin = new Thickness(0,0,0,16) };
        startup.Click += (_, _) => { try { SetStartup(startup.IsChecked == true); } catch (Exception ex) { startup.IsChecked = StartupEnabled(); MessageBox.Show(ex.Message, "Startup setting"); } };
        panel.Children.Add(startup);
        var browser = new CheckBox { Content = "Include browser renderers marked idle by Windows", IsChecked = settings.BoostBackgroundBrowsers, Margin = new Thickness(0,0,0,16) };
        browser.Click += (_, _) => { settings.BoostBackgroundBrowsers = browser.IsChecked == true; settings.Save(); };
        panel.Children.Add(browser);
        panel.Children.Add(new TextBlock { Text = "Protected process names (one per line)" });
        var excluded = new TextBox { Text = settings.BoostExclusions, AcceptsReturn = true, Height = 80, Margin = new Thickness(0,8,0,16) };
        excluded.Foreground=System.Windows.Media.Brushes.Black;excluded.Background=System.Windows.Media.Brushes.White;
        excluded.LostFocus += (_, _) => { settings.BoostExclusions = excluded.Text; settings.Save(); };
        panel.Children.Add(excluded);
        panel.Children.Add(new TextBlock { Text = "Boost runs only when clicked. Active apps, media and protected processes are skipped. Startup uses the installed application location.", TextWrapping = TextWrapping.Wrap });
        preferences = new Window { Title = "QuietCare — monitor settings", Content = new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}, Width = 520,MaxHeight=Math.Max(320,SystemParameters.WorkArea.Height-32), SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterScreen,ShowActivated=activate,
            Background=(System.Windows.Media.Brush)Application.Current.TryFindResource("ApplicationBackgroundBrush")??new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(14,24,40)),
            Foreground=(System.Windows.Media.Brush)Application.Current.TryFindResource("TextFillColorPrimaryBrush")??System.Windows.Media.Brushes.White};
        preferences.Closed += (_, _) => preferences = null;
        ((ScrollViewer)preferences.Content).Background=preferences.Background;
        preferences.Show();
    }
    public void RenderSettingsPreview(string path)
    {
        Reveal("Settings",false);preferences.UpdateLayout();var content=(FrameworkElement)preferences.Content;
        var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(content.ActualWidth),(int)Math.Ceiling(content.ActualHeight),96,96,System.Windows.Media.PixelFormats.Pbgra32);
        var background=new System.Windows.Media.DrawingVisual();using(var drawing=background.RenderOpen())drawing.DrawRectangle(preferences.Background,null,new Rect(0,0,content.ActualWidth,content.ActualHeight));bitmap.Render(background);
        bitmap.Render(content);var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));using(var stream=File.Create(path))encoder.Save(stream);preferences.Close();
    }

    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    static string StartupCommand => "\"" + Environment.ProcessPath + "\" --tray --monitor";
    static bool StartupEnabled()
    {
        using var key=Registry.CurrentUser.OpenSubKey(RunKey);
        using var approved=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run");
        var state=approved?.GetValue("QuietCareNext") as byte[];
        return (key?.GetValue("QuietCareNext") as string)==StartupCommand&&(state is null||state.Length==0||state[0]!=3);
    }
    static void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled)
        {
            key.SetValue("QuietCareNext",StartupCommand,RegistryValueKind.String);
            using var approved=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run");
            approved.SetValue("QuietCareNext",new byte[]{2,0,0,0,0,0,0,0,0,0,0,0},RegistryValueKind.Binary);
        }
        else if ((key.GetValue("QuietCareNext") as string) == StartupCommand) key.DeleteValue("QuietCareNext", false);
    }
    public void Exit() => exit();
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; shutdown.Cancel(); timer.Stop();
        preferences?.Close();
        if (widget != null) { widget.AllowClose = true; widget.Close(); }
        compact?.Shutdown();
        boostNotice?.Dispose();
        tray?.Dispose();
        appIcon?.Dispose();
        if (!sampling) { telemetry?.Dispose(); telemetry = null; }
    }
}
