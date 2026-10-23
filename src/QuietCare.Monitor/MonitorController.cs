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
    Forms.NotifyIcon tray;
    bool sampling, disposed;
    string lastResult = "No RAM Boost has run in this session.";
    Window preferences;
    Action startupReview;
    public int SamplesCompleted { get; private set; }
    public Snapshot Latest { get; private set; }
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
        timer.Interval = TimeSpan.FromMilliseconds(settings.RefreshMs);
        timer.Tick += async (_, _) => await Sample();
    }

    public void Start()
    {
        if (tray != null || disposed) return;
        if(Environment.GetCommandLineArgs().Contains("--monitor"))settings.ShowWidget=true;
        tray = new Forms.NotifyIcon { Text = "QuietCare Next", Icon = System.Drawing.SystemIcons.Application, Visible = true };
        tray.BalloonTipClicked+=(_,_)=>{var review=startupReview;startupReview=null;review?.Invoke();};
        tray.DoubleClick += (_, _) => open();
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open QuietCare Next", null, (_, _) => open());
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

    public void ToggleWidget()
    {
        settings.ShowWidget = !(compact?.Visible==true || widget?.IsVisible==true);
        if (settings.ShowWidget) EnsureWidget(); else { widget?.Hide();compact?.Hide(); }
        settings.Save(); RefreshMonitor();
    }

    public void SetRefresh(int ms)
    {
        settings.RefreshMs = Math.Max(100, Math.Min(10000, ms));
        timer.Interval = TimeSpan.FromMilliseconds(settings.RefreshMs);
        settings.Save();
    }

    public void RefreshMonitor()
    {
        if (compact?.Visible==true || widget?.IsVisible==true) { timer.Start(); _ = Sample(); }
        else timer.Stop();
    }

    async Task Sample()
    {
        if (sampling || disposed || !(compact?.Visible==true || widget?.IsVisible==true)) return;
        sampling = true;
        bool detailed = widget?.IsExpanded==true && widget.IsVisible;
        try
        {
            var value = await Task.Run(() => {
                using var process=System.Diagnostics.Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;var watch=System.Diagnostics.Stopwatch.StartNew();
                telemetry ??= new Telemetry();var snapshot=telemetry.Sample(detailed);
                SamplingCpuMilliseconds+=(process.TotalProcessorTime-cpu).TotalMilliseconds;SamplingWallMilliseconds+=watch.Elapsed.TotalMilliseconds;return snapshot;
            });
            if (!disposed) { Latest = value; SamplesCompleted++; compact?.Update(value); if(widget?.IsVisible==true)widget.Update(value); }
        }
        catch (Exception ex) { State.Log("Monitor: " + ex.Message); }
        finally { sampling = false; if (disposed) { telemetry?.Dispose(); telemetry = null; } }
    }

    public async void BoostRam()
    {
        if (disposed || booster.Busy) return;
        compact?.SetBusy(true);
        widget?.BoostFeedback("Checking idle background processes…", true);
        try
        {
            var result = await booster.Run(settings.ProtectedApps, shutdown.Token, settings.BoostBackgroundBrowsers);
            lastResult = result.Summary + "\n\n" + string.Join("\n",result.Observations.Select(x=>x.App+": "+x.Before+" → "+x.After+" · "+x.Outcome))
                + "\n\nProtected or skipped:\n" + string.Join("\n",result.SkipReasons.Select(x=>x.Key+": "+x.Value))
                + "\n\nWorking-set trimming moves some resident pages out of RAM. It does not close tabs or free allocations, and pages can return when an app uses them. A reduction is not guaranteed.";
            if (!disposed) { widget?.BoostFeedback(result.Summary, false); if(compact?.Visible==true)ShowBoostNotice(result.Summary); await Sample(); }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { lastResult = ex.Message; if (!disposed) { widget?.BoostFeedback(ex.Message, false); if(compact?.Visible==true)ShowBoostNotice(ex.Message); } }
        finally { if(!disposed)compact?.SetBusy(false); }
    }

    public void Reveal(string page = null)
    {
        if (page == "Boost results") { MessageBox.Show(lastResult, "RAM Boost results"); return; }
        if (page != "Settings") { open(); return; }
        if (preferences != null) { preferences.Activate(); return; }
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = "Monitor settings", FontSize = 24, Margin = new Thickness(0,0,0,20) });
        panel.Children.Add(new TextBlock { Text = "Refresh interval (100–10,000 milliseconds)" });
        var speed = new ComboBox { Margin = new Thickness(0,8,0,18), ItemsSource = new[] {100,250,500,1000,2000,5000,10000}, SelectedItem = settings.RefreshMs };
        speed.SelectionChanged += (_, _) => { if (speed.SelectedItem is int ms) SetRefresh(ms); };
        panel.Children.Add(speed);
        panel.Children.Add(new TextBlock { Text = "Background opacity" });
        var opacity = new Slider { Minimum = .15, Maximum = 1, Value = settings.WidgetOpacity, Margin = new Thickness(0,8,0,18) };
        opacity.ValueChanged += (_, _) => { settings.WidgetOpacity = opacity.Value; settings.Save(); widget?.ApplyTransparency();compact?.ApplySettings(); };
        panel.Children.Add(opacity);
        var top = new CheckBox { Content = "Keep monitor above other windows", IsChecked = settings.Topmost, Margin = new Thickness(0,0,0,16) };
        top.Click += (_, _) => { settings.Topmost = top.IsChecked == true; if (widget != null) widget.Topmost = settings.Topmost;compact?.ApplySettings(); settings.Save(); };
        panel.Children.Add(top);
        var startup = new CheckBox { Content = "Start QuietCare Next monitor when I sign in", IsChecked = StartupEnabled(), Margin = new Thickness(0,0,0,16) };
        startup.Click += (_, _) => { try { SetStartup(startup.IsChecked == true); } catch (Exception ex) { startup.IsChecked = StartupEnabled(); MessageBox.Show(ex.Message, "Startup setting"); } };
        panel.Children.Add(startup);
        var browser = new CheckBox { Content = "Include browser renderers marked idle by Windows", IsChecked = settings.BoostBackgroundBrowsers, Margin = new Thickness(0,0,0,16) };
        browser.Click += (_, _) => { settings.BoostBackgroundBrowsers = browser.IsChecked == true; settings.Save(); };
        panel.Children.Add(browser);
        panel.Children.Add(new TextBlock { Text = "Protected process names (one per line)" });
        var excluded = new TextBox { Text = settings.BoostExclusions, AcceptsReturn = true, Height = 80, Margin = new Thickness(0,8,0,16) };
        excluded.LostFocus += (_, _) => { settings.BoostExclusions = excluded.Text; settings.Save(); };
        panel.Children.Add(excluded);
        panel.Children.Add(new TextBlock { Text = "Boost runs only when clicked. Active apps, media and protected processes are skipped. This preview has its own startup entry and settings.", TextWrapping = TextWrapping.Wrap });
        preferences = new Window { Title = "QuietCare Next — monitor settings", Content = panel, Width = 510, SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterScreen };
        preferences.Closed += (_, _) => preferences = null;
        preferences.Show();
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
        tray?.Dispose();
        if (!sampling) { telemetry?.Dispose(); telemetry = null; }
    }
}
