using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace QuietCare {
public static class Theme {
    public static SolidColorBrush Brush(string hex){var b=(SolidColorBrush)new BrushConverter().ConvertFromString(hex);b.Freeze();return b;}
    public static readonly Brush Bg=Brush("#080E23"),Panel=Brush("#14203B"),Line=Brush("#2A4168"),Text=Brush("#F2F6FF"),Muted=Brush("#ACBCE0"),Accent=Brush("#62DDFF"),Violet=Brush("#BA9AFF"),Mint=Brush("#64E5B7"),Amber=Brush("#FFD080");
    public static Brush Gradient(string a,string b,double angle=25){var g=new LinearGradientBrush((Color)ColorConverter.ConvertFromString(a),(Color)ColorConverter.ConvertFromString(b),angle);g.Freeze();return g;}
    public static readonly Brush Surface=Gradient("#111E3D","#0B1028",65),Primary=Gradient("#467CFF","#22CBE7",15),CardSurface=Gradient("#172544","#121A35",55);
    public static void Install(Application app) {
        string xaml=@"<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
<Style TargetType='Window'><Setter Property='FontFamily' Value='Segoe UI'/><Setter Property='FontSize' Value='13'/><Setter Property='Foreground' Value='#E8F1EF'/><Setter Property='Background' Value='#101719'/></Style>
<Style TargetType='Button'><Setter Property='Foreground' Value='#E8F1EF'/><Setter Property='Background' Value='#253437'/><Setter Property='Padding' Value='15,10'/><Setter Property='Margin' Value='0,0,8,8'/><Setter Property='Cursor' Value='Hand'/><Setter Property='BorderThickness' Value='0'/><Setter Property='HorizontalContentAlignment' Value='Center'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Button'><Border x:Name='b' Background='{TemplateBinding Background}' CornerRadius='7' Padding='{TemplateBinding Padding}'><ContentPresenter HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='b' Property='Opacity' Value='0.8'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='b' Property='BorderBrush' Value='#83E8BE'/><Setter TargetName='b' Property='BorderThickness' Value='1'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter TargetName='b' Property='Opacity' Value='0.4'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
<Style TargetType='CheckBox'><Setter Property='Foreground' Value='#E8F1EF'/><Setter Property='Margin' Value='0,7,0,12'/><Setter Property='VerticalContentAlignment' Value='Center'/><Setter Property='Cursor' Value='Hand'/></Style>
<Style TargetType='TextBox'><Setter Property='Background' Value='#111B1E'/><Setter Property='Foreground' Value='#E8F1EF'/><Setter Property='BorderBrush' Value='#37504E'/><Setter Property='Padding' Value='10,8'/><Setter Property='CaretBrush' Value='#83E8BE'/><Setter Property='Margin' Value='0,0,8,10'/></Style>
<Style TargetType='DataGrid'><Setter Property='Background' Value='#192326'/><Setter Property='Foreground' Value='#E8F1EF'/><Setter Property='RowBackground' Value='#192326'/><Setter Property='AlternatingRowBackground' Value='#1C292C'/><Setter Property='BorderThickness' Value='0'/><Setter Property='GridLinesVisibility' Value='None'/><Setter Property='AutoGenerateColumns' Value='False'/><Setter Property='CanUserAddRows' Value='False'/><Setter Property='CanUserDeleteRows' Value='False'/><Setter Property='IsReadOnly' Value='True'/><Setter Property='HeadersVisibility' Value='Column'/><Setter Property='RowHeight' Value='36'/><Setter Property='ColumnHeaderHeight' Value='36'/><Setter Property='EnableRowVirtualization' Value='True'/><Setter Property='SelectionMode' Value='Extended'/><Setter Property='SelectionUnit' Value='FullRow'/></Style>
<Style TargetType='DataGridColumnHeader'><Setter Property='Background' Value='#243437'/><Setter Property='Foreground' Value='#B7CBC7'/><Setter Property='Padding' Value='10,6'/><Setter Property='BorderThickness' Value='0'/></Style>
<Style TargetType='DataGridCell'><Setter Property='BorderThickness' Value='0'/><Setter Property='Padding' Value='9,5'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='DataGridCell'><Border Background='{TemplateBinding Background}' Padding='{TemplateBinding Padding}'><ContentPresenter VerticalAlignment='Center'/></Border></ControlTemplate></Setter.Value></Setter><Style.Triggers><Trigger Property='IsSelected' Value='True'><Setter Property='Background' Value='#365B52'/><Setter Property='Foreground' Value='#FFFFFF'/></Trigger></Style.Triggers></Style>
<Style TargetType='ScrollBar'><Setter Property='Margin' Value='0,30,0,2'/><Setter Property='Width' Value='9'/><Setter Property='Background' Value='Transparent'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ScrollBar'><Grid Background='{TemplateBinding Background}'><Track x:Name='PART_Track' Orientation='{TemplateBinding Orientation}' IsDirectionReversed='True'><Track.DecreaseRepeatButton><RepeatButton x:Name='PageBefore' Command='ScrollBar.PageUpCommand' Focusable='False'><RepeatButton.Template><ControlTemplate TargetType='RepeatButton'><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.DecreaseRepeatButton><Track.Thumb><Thumb x:Name='ScrollThumb' MinHeight='24'><Thumb.Template><ControlTemplate TargetType='Thumb'><Border Background='#536B8C' CornerRadius='4' Margin='1'/></ControlTemplate></Thumb.Template></Thumb></Track.Thumb><Track.IncreaseRepeatButton><RepeatButton x:Name='PageAfter' Command='ScrollBar.PageDownCommand' Focusable='False'><RepeatButton.Template><ControlTemplate TargetType='RepeatButton'><Border Background='Transparent'/></ControlTemplate></RepeatButton.Template></RepeatButton></Track.IncreaseRepeatButton></Track></Grid><ControlTemplate.Triggers><Trigger Property='Orientation' Value='Horizontal'><Setter Property='Margin' Value='0'/><Setter TargetName='ScrollThumb' Property='MinHeight' Value='0'/><Setter TargetName='PageBefore' Property='Command' Value='{x:Static ScrollBar.PageLeftCommand}'/><Setter TargetName='PageAfter' Property='Command' Value='{x:Static ScrollBar.PageRightCommand}'/><Setter TargetName='PART_Track' Property='IsDirectionReversed' Value='False'/><Setter Property='Height' Value='9'/><Setter Property='Width' Value='Auto'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
<Style TargetType='ProgressBar'><Setter Property='Background' Value='#263657'/><Setter Property='BorderThickness' Value='0'/><Setter Property='Foreground' Value='#62DDFF'/></Style>
<Style TargetType='ScrollViewer'><Setter Property='PanningMode' Value='VerticalOnly'/></Style>
<Style TargetType='ContextMenu'><Setter Property='Background' Value='#161C37'/><Setter Property='Foreground' Value='#ECF2FF'/><Setter Property='BorderBrush' Value='#3A466E'/></Style>
</ResourceDictionary>";
        foreach(var pair in new[]{new[]{"#101719","#090B23"},new[]{"#192326","#13162F"},new[]{"#E8F1EF","#F0F3FF"},new[]{"#253437","#252B4B"},new[]{"#83E8BE","#7EC8FF"},new[]{"#111B1E","#0C1029"},new[]{"#37504E","#343D64"},new[]{"#1C292C","#171C39"},new[]{"#243437","#202747"},new[]{"#B7CBC7","#B6C4EC"},new[]{"#365B52","#334E7D"}})xaml=xaml.Replace(pair[0],pair[1]);
        xaml=xaml.Replace("#090B23","#080E23").Replace("#13162F","#14203B").Replace("#252B4B","#263657").Replace("#7EC8FF","#62DDFF");
        app.Resources=(ResourceDictionary)XamlReader.Parse(xaml);
    }
    public static TextBlock Label(string text,double size=13,Brush color=null) {return new TextBlock{Text=text,FontSize=size,Foreground=color??Text,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,8)};}
    public static Button Button(string text,Action action,bool primary=false) {
        var b=new Button{Content=text};if(primary){b.Background=Primary;b.Foreground=Brushes.White;b.FontWeight=FontWeights.SemiBold;}
        b.Click+=(s,e)=>{try{action();}catch(Exception ex){MessageBox.Show(ex.Message,"QuietCare",MessageBoxButton.OK,MessageBoxImage.Information);}};return b;
    }
    public static Border Card(UIElement content) {return new Border{Background=CardSurface,CornerRadius=new CornerRadius(14),BorderBrush=Line,BorderThickness=new Thickness(1),Padding=new Thickness(20),Margin=new Thickness(0,0,12,14),Child=content};}
}
public sealed class Spark : FrameworkElement {
    readonly Queue<double> values=new Queue<double>();public Brush Stroke=Theme.Accent;
    public Spark(){Height=38;MinWidth=100;}
    public void Add(double value){values.Enqueue(value);while(values.Count>45)values.Dequeue();InvalidateVisual();}
    protected override void OnRender(DrawingContext dc) {
        base.OnRender(dc);if(values.Count<2)return;var pts=values.ToArray();var geometry=new StreamGeometry();
        using(var ctx=geometry.Open()) {for(int i=0;i<pts.Length;i++){var p=new Point(i*ActualWidth/(pts.Length-1),ActualHeight*(1-Telemetry.Clamp(pts[i])/100));if(i==0)ctx.BeginFigure(p,false,false);else ctx.LineTo(p,true,false);}}
        geometry.Freeze();dc.DrawGeometry(null,new Pen(Stroke,1.8),geometry);
    }
}
public sealed class Metric {
    public TextBlock Value=Theme.Label("—",32),Detail=Theme.Label("Waiting for first sample",11,Theme.Muted); public Spark Chart=new Spark(); public Border View;
    public Metric(string name,Brush color) {var stack=new StackPanel();stack.Children.Add(Theme.Label(name.ToUpperInvariant(),11,Theme.Muted));Value.Foreground=color;stack.Children.Add(Value);Chart.Stroke=color;stack.Children.Add(Chart);stack.Children.Add(Detail);View=Theme.Card(stack);}
}
public sealed class ScanDial : FrameworkElement {
    public string Caption="SCAN";
    public ScanDial(){Width=270;Height=270;}
    protected override void OnRender(DrawingContext dc) {
        var center=new Point(135,135);
        var halo=new RadialGradientBrush();halo.GradientStops.Add(new GradientStop(Color.FromArgb(0,58,153,255),0.65));halo.GradientStops.Add(new GradientStop(Color.FromArgb(70,58,153,255),0.82));halo.GradientStops.Add(new GradientStop(Color.FromArgb(0,58,153,255),1));dc.DrawEllipse(halo,null,center,134,134);
        dc.DrawEllipse(Theme.Brush("#101E41"),new Pen(Theme.Brush("#5588C6"),1.4),center,119,119);
        dc.DrawEllipse(null,new Pen(Theme.Brush("#2B4267"),19),center,105,105);
        for(int i=0;i<6;i++) {
            double a=(i*60-84)*Math.PI/180,b=(i*60-29)*Math.PI/180;
            var g=new StreamGeometry();using(var c=g.Open()){c.BeginFigure(new Point(135+105*Math.Cos(a),135+105*Math.Sin(a)),false,false);c.ArcTo(new Point(135+105*Math.Cos(b),135+105*Math.Sin(b)),new Size(105,105),0,false,SweepDirection.Clockwise,true,false);}g.Freeze();
            dc.DrawGeometry(null,new Pen(new LinearGradientBrush(Color.FromRgb(77,150,235),Color.FromRgb(137,213,255),90),16),g);
        }
        var fill=new RadialGradientBrush(Color.FromRgb(58,79,148),Color.FromRgb(19,33,77));fill.Freeze();dc.DrawEllipse(fill,new Pen(Theme.Brush("#8496DC"),1),center,87,87);
        var text=new FormattedText(Caption,System.Globalization.CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface(new FontFamily("Segoe UI"),FontStyles.Normal,FontWeights.Bold,FontStretches.Normal),Caption.Length>6?18:33,Brushes.White,VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(text,new Point(135-text.Width/2,135-text.Height/2));
    }
}
public sealed partial class MainWindow : Window {
    readonly Settings settings; Telemetry telemetry; readonly Cleaner cleaner=new Cleaner();
    readonly ContentControl content=new ContentControl();readonly TextBlock status=Theme.Label("Local only  ·  No ads  ·  Changes you can review",11,Theme.Muted);
    readonly Dictionary<string,Button> navigation=new Dictionary<string,Button>();readonly Queue<Snapshot> history=new Queue<Snapshot>();
    readonly DispatcherTimer timer=new DispatcherTimer(); CancellationTokenSource workCancel=new CancellationTokenSource();
    readonly System.Windows.Forms.NotifyIcon tray=new System.Windows.Forms.NotifyIcon();bool sampling,exiting; string current="Overview"; Widget widget;Snapshot latest;DateTime nextSecondaryUpdate;
    TextBlock careLive;DataGrid processGrid;ScanResult pendingScan;bool shellBuilt,started,suspended,operationRunning;readonly SmartBoost booster=new SmartBoost();
    public MainWindow(Settings prefs,bool startTray,IBoostRuntime boostRuntime=null,IUpdateService updateService=null,ICareChecks careChecks=null) {
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher));
        if(boostRuntime!=null)booster=new SmartBoost(boostRuntime);updates=updateService;careScanner=new CareScanner(careChecks);
        settings=prefs;Title="QuietCare — PC care without the noise";Width=1000;Height=670;MinWidth=860;MinHeight=610;WindowStartupLocation=WindowStartupLocation.CenterScreen;
        Background=Theme.Bg;Foreground=Theme.Text;FontFamily=new FontFamily("Segoe UI");FontSize=13;
        WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.CanResizeWithGrip;
        System.Windows.Shell.WindowChrome.SetWindowChrome(this,new System.Windows.Shell.WindowChrome{CaptionHeight=0,ResizeBorderThickness=new Thickness(6),GlassFrameThickness=new Thickness(0),CornerRadius=new CornerRadius(12),UseAeroCaptionButtons=false});
        tray.Icon=System.Drawing.Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);tray.Text="QuietCare — local PC monitor";tray.Visible=true;
        var menu=new System.Windows.Forms.ContextMenuStrip();menu.Items.Add("Open QuietCare",null,(s,e)=>Dispatcher.BeginInvoke(new Action(()=>Reveal())));menu.Items.Add("Show / hide monitor",null,(s,e)=>Dispatcher.BeginInvoke(new Action(()=>ToggleWidget())));menu.Items.Add("Exit QuietCare",null,(s,e)=>Dispatcher.BeginInvoke(new Action(()=>Exit())));tray.ContextMenuStrip=menu;
        tray.DoubleClick+=(s,e)=>Dispatcher.BeginInvoke(new Action(()=>Reveal()));tray.BalloonTipClicked+=(s,e)=>Dispatcher.BeginInvoke(new Action(()=>Reveal("Updates")));
        menu.Items.Insert(1,new System.Windows.Forms.ToolStripMenuItem("Check QuietCare updates",null,(s,e)=>Dispatcher.BeginInvoke(new Action(()=>{Reveal("Updates");Observe(CheckQuietCareUpdates(true));}))));
        timer.Interval=TimeSpan.FromMilliseconds(settings.RefreshMs);timer.Tick+=async(s,e)=>await Sample();
        Loaded+=(s,e)=>BeginMonitoring();
        StateChanged+=(s,e)=>UpdateTimerState();
        Microsoft.Win32.SystemEvents.PowerModeChanged+=PowerChanged;
        Closing+=(s,e)=>{
            if(!exiting && settings.CloseToTray){e.Cancel=true;HideMain();return;}
            exiting=true;workCancel.Cancel();StopCareScan();if(updateCancel!=null)updateCancel.Cancel();var updateDisposable=updates as IDisposable;if(updateDisposable!=null)updateDisposable.Dispose();timer.Stop();Microsoft.Win32.SystemEvents.PowerModeChanged-=PowerChanged;
            if(widget!=null){widget.AllowClose=true;widget.Close();}
            tray.Visible=false;tray.Dispose();try{settings.Save();}catch(Exception ex){State.Log("Settings save: "+ex.GetType().Name);}
        };
        Closed+=(s,e)=>{if(!sampling && telemetry!=null)telemetry.Dispose();Application.Current.Shutdown();};
        if(!startTray)BuildShell();
    }
    void BuildShell(){
        if(shellBuilt)return;
        var root=new Grid{Name="AppCanvas",Background=Theme.Surface};root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(174)});root.ColumnDefinitions.Add(new ColumnDefinition());root.RowDefinitions.Add(new RowDefinition());root.RowDefinitions.Add(new RowDefinition{Height=new GridLength(28)});Content=new Border{BorderBrush=Theme.Line,BorderThickness=new Thickness(1),Child=root};
        var side=new DockPanel{Background=Theme.Gradient("#142746","#0D142D",90)};root.Children.Add(side);
        var brand=new StackPanel{Margin=new Thickness(17,22,10,22)};var title=Theme.Label("◉  QuietCare",18,Theme.Accent);title.FontWeight=FontWeights.SemiBold;title.Margin=new Thickness(0,0,0,4);brand.Children.Add(title);var brandNote=Theme.Label("A QUIETER, CLEARER PC",9,Theme.Muted);brandNote.Margin=new Thickness(1,0,0,0);brand.Children.Add(brandNote);brand.MouseLeftButtonDown+=DragWindow;brand.ToolTip="Drag to move · double-click to maximize";DockPanel.SetDock(brand,Dock.Top);side.Children.Add(brand);
        var bottom=new StackPanel{Margin=new Thickness(12,5,12,12)};var monitor=Theme.Button("◫   Mini monitor",()=>{settings.ShowWidget=true;EnsureWidget();settings.Save();});monitor.FontSize=12;monitor.Background=Theme.Brush("#203452");bottom.Children.Add(monitor);var prefs=Theme.Button("⚙   Settings",()=>Navigate("Settings"));prefs.Background=Brushes.Transparent;prefs.HorizontalContentAlignment=HorizontalAlignment.Left;prefs.Padding=new Thickness(12,8,12,8);prefs.Margin=new Thickness(0,0,0,6);bottom.Children.Add(prefs);bottom.Children.Add(Theme.Label("Ad-free  •  Your control",10,Theme.Muted));DockPanel.SetDock(bottom,Dock.Bottom);side.Children.Add(bottom);
        var nav=new StackPanel{Margin=new Thickness(9,0,4,0)};side.Children.Add(nav);
        string[] pages={"Overview","Speed Up","Protect","Updates","Toolbox","Recovery"};string[] labels={"Care","Speed Up","Protect","Software Updater","Toolbox","Recovery"};
        string[] symbols={"◉","◴","⛨","⇧","⊞","↶"};
        Brush[] accents={Theme.Accent,Theme.Violet,Theme.Mint,Theme.Amber,Theme.Violet,Theme.Mint};
        for(int i=0;i<pages.Length;i++){string page=pages[i];var caption=new Grid();caption.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(32)});caption.ColumnDefinitions.Add(new ColumnDefinition());var glyph=Theme.Label(symbols[i],22,accents[i]);glyph.VerticalAlignment=VerticalAlignment.Center;glyph.Margin=new Thickness(0);caption.Children.Add(glyph);var text=Theme.Label(labels[i]=="Software Updater"?"App & updates":labels[i],12);text.VerticalAlignment=VerticalAlignment.Center;text.Margin=new Thickness(0);Grid.SetColumn(text,1);caption.Children.Add(text);var button=Theme.Button("",()=>Navigate(page));button.Content=caption;button.HorizontalContentAlignment=HorizontalAlignment.Stretch;button.Padding=new Thickness(11,11,5,11);button.Margin=new Thickness(0,0,5,6);button.ToolTip=labels[i];navigation[page]=button;nav.Children.Add(button);}
        content.Margin=new Thickness(25,21,13,0);Grid.SetColumn(content,1);root.Children.Add(content);
        status.Margin=new Thickness(20,4,20,0);status.TextTrimming=TextTrimming.CharacterEllipsis;status.TextWrapping=TextWrapping.NoWrap;Grid.SetColumnSpan(status,2);Grid.SetRow(status,1);root.Children.Add(status);
        var drag=new Border{Name="WindowDragRegion",Background=Brushes.Transparent,Height=16,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(0,0,174,0)};drag.MouseLeftButtonDown+=DragWindow;Grid.SetColumnSpan(drag,2);Panel.SetZIndex(drag,5);root.Children.Add(drag);
        var controls=new StackPanel{Name="CaptionControls",Orientation=Orientation.Horizontal,Background=Brushes.Transparent,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(0,7,9,0)};
        controls.Children.Add(CaptionButton("⚙","Settings",()=>Navigate("Settings")));controls.Children.Add(CaptionButton("—","Minimize",()=>WindowState=WindowState.Minimized));controls.Children.Add(CaptionButton("□","Maximize / restore",ToggleMaximize));controls.Children.Add(CaptionButton("✕","Close to notification area",()=>Close()));Grid.SetColumn(controls,1);Panel.SetZIndex(controls,10);root.Children.Add(controls);
        shellBuilt=true;Navigate(current);
    }
    Button CaptionButton(string glyph,string hint,Action action){var b=Theme.Button(glyph,action);b.Width=37;b.Height=30;b.Margin=new Thickness(2,0,0,0);b.Padding=new Thickness(0);b.Background=Brushes.Transparent;b.Foreground=Theme.Muted;b.FontSize=14;b.ToolTip=hint;System.Windows.Automation.AutomationProperties.SetName(b,hint);b.MouseEnter+=(s,e)=>b.Background=hint.StartsWith("Close",StringComparison.Ordinal)?Theme.Brush("#A72E50"):Theme.Brush("#263D60");b.MouseLeave+=(s,e)=>b.Background=Brushes.Transparent;return b;}
    void ToggleMaximize(){WindowState=WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;}
    void DragWindow(object sender,MouseButtonEventArgs e){if(e.ChangedButton!=MouseButton.Left)return;if(e.ClickCount==2)ToggleMaximize();else if(e.LeftButton==MouseButtonState.Pressed)DragMove();}
    internal bool ShellBuilt {get{return shellBuilt;}}
    internal bool SamplingScheduled {get{return timer.IsEnabled;}}
    internal void InspectPage(string page){BuildShell();Navigate(page);}
    internal FrameworkElement PageView {get{return content.Content as FrameworkElement;}}
    internal string CurrentPage {get{return current;}}
    public void StartInTray(){BeginMonitoring();}
    void BeginMonitoring(){if(started)return;started=true;if(settings.ShowWidget)EnsureWidget();UpdateTimerState();Observe(Sample());if(UpdatePolicy.Due(settings.CheckUpdatesWeekly,settings.LastUpdateAttemptUtc,DateTime.UtcNow))Observe(CheckQuietCareUpdates(false));}
    void PowerChanged(object sender,Microsoft.Win32.PowerModeChangedEventArgs e){Dispatcher.BeginInvoke(new Action(()=>{if(e.Mode==Microsoft.Win32.PowerModes.Suspend)suspended=true;else if(e.Mode==Microsoft.Win32.PowerModes.Resume)suspended=false;UpdateTimerState();}));}
    void UpdateTimerState(){if(started && !exiting && MonitorPolicy.Active(IsVisible,WindowState==WindowState.Minimized,widget!=null&&widget.IsVisible,suspended))timer.Start();else timer.Stop();}
    void HideMain(){Hide();StopCareScan();CancelWork();content.Content=null;Content=null;navigation.Clear();careLive=null;processGrid=null;careHeading=careDetail=careItem=careElapsed=null;careBar=null;careDial=null;careResults=null;careStop=careSkip=careRun=null;careRowStatus.Clear();updateStatus=updateVersion=null;updateCheck=updateDownload=updateInstall=updateStop=null;updateNotes=null;updateProgress=null;shellBuilt=false;UpdateTimerState();}
    public void Exit(){exiting=true;Close();}
    public void RefreshMonitor(){UpdateTimerState();Observe(Sample());}
    public async void BoostRam(){
        if(booster.Busy)return;if(careBusy){status.Text="Finish the read-only Care scan before applying RAM Boost.";return;}if(widget!=null)widget.BoostFeedback("Checking idle background processes…",true);status.Text="Checking idle background processes…";
        try{var result=await booster.Run(settings.ProtectedApps,CancellationToken.None,settings.BoostBackgroundBrowsers);lastBoost=result;if(exiting)return;status.Text=result.Summary;if(widget!=null)widget.BoostFeedback(result.Summary,false);if(IsVisible&&current=="Boost results")Navigate("Boost results");Observe(Sample());}
        catch(Exception ex){if(!exiting){status.Text=ex.Message;if(widget!=null)widget.BoostFeedback(ex.Message,false);}}
    }
    public void SetRefresh(int milliseconds){settings.RefreshMs=Math.Max(100,Math.Min(10000,milliseconds));timer.Interval=TimeSpan.FromMilliseconds(settings.RefreshMs);settings.Save();status.Text="Monitor refresh: "+settings.RefreshMs+" ms.";}
    public void Reveal(string page=null){BuildShell();Show();WindowState=WindowState.Normal;Activate();if(page!=null)Navigate(page);BeginMonitoring();UpdateTimerState();}
    public void ToggleWidget(){settings.ShowWidget=!settings.ShowWidget;if(settings.ShowWidget)EnsureWidget();else if(widget!=null)widget.Hide();settings.Save();UpdateTimerState();}
    void EnsureWidget(){if(widget==null)widget=new Widget(this,settings);widget.Topmost=settings.Topmost;widget.Show();if(latest!=null)widget.Update(latest);}
    async Task Sample() {
        if(sampling || exiting || !MonitorPolicy.Active(IsVisible,WindowState==WindowState.Minimized,widget!=null&&widget.IsVisible,suspended))return;ForegroundHistory.Observe();sampling=true;bool detailed=widget!=null&&widget.IsVisible&&widget.IsExpanded;
        try {var s=await Task.Run(()=>{if(telemetry==null)telemetry=new Telemetry();return telemetry.Sample(detailed);});if(exiting || suspended)return;latest=s;history.Enqueue(s);while(history.Count>900)history.Dequeue();
            bool updateSecondary=DateTime.UtcNow>=nextSecondaryUpdate;
            if(updateSecondary && IsVisible && WindowState!=WindowState.Minimized && careLive!=null && current=="Overview")careLive.Text="CPU "+Format.Percent(s.Cpu)+"     GPU "+Format.Percent(s.Gpu)+"     RAM "+Format.Percent(s.Ram);
            if(widget!=null && widget.IsVisible)widget.Update(s);
            if(updateSecondary){tray.Text="QuietCare • CPU "+Format.Percent(s.Cpu)+" • RAM "+Format.Percent(s.Ram);nextSecondaryUpdate=DateTime.UtcNow.AddSeconds(1);}
        }catch(Exception ex){State.Log("Monitor: "+ex.GetType().Name);status.Text="A sensor is unavailable. QuietCare will retry at the next interval.";}finally{sampling=false;if(exiting&&telemetry!=null)telemetry.Dispose();}
    }
    void CancelWork(){workCancel.Cancel();workCancel.Dispose();workCancel=new CancellationTokenSource();}
    void Navigate(string page) {
        if(page=="Overview"&&careBusy)page="Care scan";workCancel.Cancel();workCancel.Dispose();workCancel=new CancellationTokenSource();current=page;processGrid=null;
        string section=page=="Care scan"?"Overview":page=="Processes & RAM"||page=="Boost results"||page=="Startup manager"?"Speed Up":page=="Cleanup"?"Overview":page=="Installed apps"?"Updates":page=="File tools"||page=="System information"?"Toolbox":page;
        foreach(var pair in navigation){bool active=pair.Key==section;pair.Value.Background=active?Theme.Gradient("#244A77","#343061"):Brushes.Transparent;pair.Value.Foreground=active?Theme.Accent:Theme.Muted;}
        var stack=new StackPanel();var scroll=new ScrollViewer{Content=stack,Margin=new Thickness(0,0,14,0),VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};content.Content=scroll;
        if(page!="Overview"){var heading=Theme.Label(page,25);heading.Margin=new Thickness(0,0,166,8);stack.Children.Add(heading);}
        switch(page){case "Overview":Overview(stack);break;case "Care scan":CareScanPage(stack);break;case "Speed Up":SpeedPage(stack);break;case "Protect":ProtectPage(stack);break;case "Updates":UpdatesPage(stack);break;case "Boost results":BoostResultsPage(stack);break;case "Processes & RAM":ProcessPage(stack);break;case "Startup manager":StartupPage(stack);break;case "Cleanup":CleanupPage(stack);break;case "File tools":FilePage(stack);break;case "Installed apps":AppsPage(stack);break;case "Toolbox":ToolPage(stack);break;case "System information":InfoPage(stack);break;case "Recovery":RecoveryPage(stack);break;case "Settings":SettingsPage(stack);break;}
    }
    void Subtitle(StackPanel p,string text){var t=Theme.Label(text,13,Theme.Muted);t.Margin=new Thickness(0,0,20,20);p.Children.Add(t);}
    WrapPanel Actions(StackPanel parent){var w=new WrapPanel{Margin=new Thickness(0,4,0,8)};parent.Children.Add(w);return w;}
    DataGrid Table(StackPanel parent,params string[] columns) {
        var table=new DataGrid{Height=310,Margin=new Thickness(0,0,12,12)};
        for(int i=0;i<columns.Length;i+=2){string property=columns[i+1];var textStyle=new Style(typeof(TextBlock));textStyle.Setters.Add(new Setter(TextBlock.TextTrimmingProperty,TextTrimming.CharacterEllipsis));textStyle.Setters.Add(new Setter(FrameworkElement.ToolTipProperty,new Binding(property)));table.Columns.Add(new DataGridTextColumn{Header=columns[i],Binding=new Binding(property),SortMemberPath=property=="Memory"||property=="Size"?"Bytes":property,ElementStyle=textStyle,Width=new DataGridLength(i==0?2:1,DataGridLengthUnitType.Star),MinWidth=i==0?100:70});}
        parent.Children.Add(table);return table;
    }
    void ActionAsync(Panel panel,string text,Func<CancellationToken,Task> work,bool primary=false) {
        Button button=null;button=Theme.Button(text,async()=>{if(operationRunning){status.Text="Another action is finishing. Please wait.";return;}operationRunning=true;button.IsEnabled=false;var token=workCancel.Token;status.Text=text+"…";try{await work(token);}catch(OperationCanceledException){}catch(Exception ex){MessageBox.Show(ex.Message,"QuietCare",MessageBoxButton.OK,MessageBoxImage.Information);State.Log("Action: "+ex.GetType().Name);}finally{operationRunning=false;button.IsEnabled=true;}},primary);panel.Children.Add(button);
    }
    bool Confirm(string text){return MessageBox.Show(this,text,"QuietCare — review change",MessageBoxButton.OKCancel,MessageBoxImage.Question)==MessageBoxResult.OK;}
    void OpenTool(string id){Tools.Open(id);status.Text="Opened a Windows tool. Review and apply any changes there.";}
    void Overview(StackPanel p) {
        var heading=new StackPanel{Margin=new Thickness(0,0,0,17)};var welcome=Theme.Label("Your PC, refreshed.",26);welcome.Margin=new Thickness(0,0,166,6);welcome.MouseLeftButtonDown+=DragWindow;heading.Children.Add(welcome);
        careLive=Theme.Label("CPU —   GPU —   RAM —",11,Theme.Accent);careLive.Margin=new Thickness(0);heading.Children.Add(careLive);p.Children.Add(heading);
        var hero=new Grid();hero.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(292)});hero.ColumnDefinitions.Add(new ColumnDefinition());
        var dial=new ScanDial();var scan=Theme.Button("",()=>Observe(RunCareScan()));
        scan.Content=dial;scan.Background=Brushes.Transparent;scan.Padding=new Thickness(0);scan.Margin=new Thickness(0);scan.HorizontalAlignment=HorizontalAlignment.Center;scan.ToolTip="Start selected read-only checks with live steps";System.Windows.Automation.AutomationProperties.SetName(scan,"Start Care scan");hero.Children.Add(scan);
        var intro=new StackPanel{VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(6,0,8,0)};
        intro.Children.Add(Theme.Label("CHECK • UNDERSTAND • CHOOSE",10,Theme.Mint));var title=Theme.Label("See what needs\nattention.",25);title.FontWeight=FontWeights.SemiBold;intro.Children.Add(title);
        CareChoices(intro);intro.Children.Add(Theme.Label("Live steps and observations. Nothing changes until you review the results.",12,Theme.Muted));
        var buttons=Actions(intro);if(careReport!=null)buttons.Children.Add(Theme.Button("Last scan results",()=>Navigate("Care scan"),true));buttons.Children.Add(Theme.Button("Recovery",()=>Navigate("Recovery")));
        Grid.SetColumn(intro,1);hero.Children.Add(intro);var heroCard=Theme.Card(hero);heroCard.Padding=new Thickness(5,4,12,4);p.Children.Add(heroCard);
        var quick=new System.Windows.Controls.Primitives.UniformGrid{Columns=2};
        QuickTile(quick,"↗","Startup manager","Choose what launches with Windows",Theme.Violet,()=>Navigate("Startup manager"));
        QuickTile(quick,"▥","Smart RAM Boost","Trim idle background apps in one click",Theme.Mint,BoostRam);
        QuickTile(quick,"◇","Storage cleanup","Review old temp files and disk tools",Theme.Accent,()=>Navigate("Cleanup"));
        QuickTile(quick,"⛨","Protection","Review Windows security and privacy",Theme.Amber,()=>Navigate("Protect"));p.Children.Add(quick);
        var footer=Theme.Label("AD-FREE   ·   LOCAL ONLY   ·   CHANGES YOU CAN REVIEW",10,Theme.Muted);footer.HorizontalAlignment=HorizontalAlignment.Center;footer.Margin=new Thickness(0,2,0,8);p.Children.Add(footer);Observe(Sample());
    }
    void QuickTile(Panel host,string icon,string title,string subtitle,Brush accent,Action action){
        var row=new Grid();row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(44)});row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(22)});
        var glyph=Theme.Label(icon,25,accent);glyph.VerticalAlignment=VerticalAlignment.Center;glyph.Margin=new Thickness(0);row.Children.Add(glyph);
        var text=new StackPanel();var label=Theme.Label(title,15);label.FontWeight=FontWeights.SemiBold;text.Children.Add(label);var detail=Theme.Label(subtitle,11,Theme.Muted);detail.Margin=new Thickness(0);text.Children.Add(detail);Grid.SetColumn(text,1);row.Children.Add(text);var arrow=Theme.Label("›",22,accent);arrow.VerticalAlignment=VerticalAlignment.Center;arrow.Margin=new Thickness(0);Grid.SetColumn(arrow,2);row.Children.Add(arrow);
        var button=Theme.Button("",action);button.Content=row;button.HorizontalContentAlignment=HorizontalAlignment.Stretch;button.Background=Theme.CardSurface;button.Padding=new Thickness(15,12,12,12);button.Margin=new Thickness(0,0,12,12);host.Children.Add(button);
    }
    void AddModule(Panel panel,string glyph,string label,Action action){var b=Theme.Button(glyph+"  "+label,action);b.Background=Brushes.Transparent;b.HorizontalContentAlignment=HorizontalAlignment.Left;b.Padding=new Thickness(5,10,5,10);b.FontSize=11;b.Margin=new Thickness(0,1,5,1);panel.Children.Add(b);}
    void Feature(StackPanel p,string title,string description,string button,Action action){
        Brush accent=title.Contains("Startup")||title.Contains("RAM")?Theme.Violet:title.Contains("protection")||title.Contains("Firewall")?Theme.Mint:title.Contains("update")||title.Contains("Update")?Theme.Amber:Theme.Accent;
        var grid=new Grid();grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(7)});grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
        var stripe=new Border{Width=3,CornerRadius=new CornerRadius(2),Background=accent,Margin=new Thickness(0,1,4,3)};grid.Children.Add(stripe);
        var text=new StackPanel{Margin=new Thickness(10,0,0,0)};var label=Theme.Label(title,16);label.FontWeight=FontWeights.SemiBold;text.Children.Add(label);var detail=Theme.Label(description,12,Theme.Muted);detail.Margin=new Thickness(0,0,0,3);text.Children.Add(detail);Grid.SetColumn(text,1);grid.Children.Add(text);
        var b=Theme.Button(button,action,true);b.VerticalAlignment=VerticalAlignment.Center;b.Margin=new Thickness(18,0,0,0);Grid.SetColumn(b,2);grid.Children.Add(b);var card=Theme.Card(grid);card.Padding=new Thickness(15,14,16,12);p.Children.Add(card);
    }
    void SpeedPage(StackPanel p){Subtitle(p,"Choose the tools that help your PC run smoothly.");Feature(p,"Startup Optimizer","Review what launches with Windows. Disable selected user entries with a recovery backup.","Manage",()=>Navigate("Startup manager"));Feature(p,"Smart RAM Boost","One-click idle background memory trim. Measured results, with protection for foreground apps, audio and project tools.","Boost RAM",BoostRam);Feature(p,"Real-time Performance Monitor","A tiny transparent strip with CPU, GPU and RAM readings. Expand for more detail.","Show monitor",()=>{settings.ShowWidget=true;EnsureWidget();settings.Save();});Feature(p,"Power & Game Mode","Choose a power profile or open Windows Game Mode.","Configure",()=>Navigate("Settings"));Feature(p,"Disk Optimization","Let Windows optimize each SSD or hard drive using the right method.","Optimize",()=>OpenTool("drives"));var a=Actions(p);a.Children.Add(Theme.Button("Last Boost results",()=>Navigate("Boost results")));a.Children.Add(Theme.Button("Advanced process & RAM tools",()=>Navigate("Processes & RAM")));}
    void ProtectPage(StackPanel p){Subtitle(p,"Review protection and privacy using Windows’ own controls.");Feature(p,"Antivirus & ransomware protection","Open Windows Security to review your antivirus status and run a scan.","Review",()=>OpenTool("security"));Feature(p,"Firewall protection","Review Windows Firewall and its connection rules.","Review",()=>OpenTool("firewall"));Feature(p,"Privacy protection","Review permissions, diagnostic data and activity controls.","Configure",()=>OpenTool("privacy"));Feature(p,"System protection & recovery","Review restore points before making substantial changes.","Open",()=>OpenTool("restore"));Feature(p,"Backup","Review your backup configuration and recovery options.","Configure",()=>OpenTool("backup"));}
    void UpdatesPage(StackPanel p){UpdateCard(p);Subtitle(p,"Use trusted update sources and review the software installed on your PC.");Feature(p,"Software inventory & uninstall","Find unused desktop programs. Remove them using Windows Installed apps.","Manage apps",()=>Navigate("Installed apps"));Feature(p,"Windows & driver updates","Review security updates, optional drivers and hardware compatibility in Windows Update.","Check updates",()=>OpenTool("updates"));Feature(p,"Microsoft Store updates","Review updates for apps installed through Microsoft Store.","Open Store",()=>OpenTool("store"));}
    void Export() {
        var dialog=new Microsoft.Win32.SaveFileDialog{Filter="CSV files|*.csv",FileName="QuietCare-session.csv"};if(dialog.ShowDialog(this)!=true)return;
        var lines=new List<string>{"time,cpu_percent,ram_percent,gpu_busiest_engine_percent,available_bytes,total_bytes,download_bytes_sec,upload_bytes_sec,disk_read_bytes_sec,disk_write_bytes_sec"};
        var culture=System.Globalization.CultureInfo.InvariantCulture;
        foreach(var s in history)lines.Add(String.Join(",",new[]{s.Time.ToString("o"),s.Cpu.HasValue?s.Cpu.Value.ToString("F2",culture):"",s.Ram.ToString(culture),s.Gpu.HasValue?s.Gpu.Value.ToString("F2",culture):"",s.Available.ToString(),s.Total.ToString(),Format.Number(s.Download),Format.Number(s.Upload),s.DiskRead.HasValue?s.DiskRead.Value.ToString("F0",culture):"",s.DiskWrite.HasValue?s.DiskWrite.Value.ToString("F0",culture):""}));
        File.WriteAllLines(dialog.FileName,lines);status.Text="Exported "+history.Count+" local samples. No data was uploaded.";
    }
    void ProcessPage(StackPanel p) {
        Subtitle(p,"Review apps in your current Windows session. RAM Boost trims one selected app’s working set; it does not close it or guarantee a speed increase.");
        var actions=Actions(p);processGrid=Table(p,"App","Name","PID","Id","RAM","Memory","Started (UTC)","Started");var table=processGrid;table.SelectionMode=DataGridSelectionMode.Single;
        actions.Children.Add(Theme.Button("Smart RAM Boost",BoostRam,true));actions.Children.Add(Theme.Button("Last Boost results",()=>Navigate("Boost results")));
        Func<CancellationToken,Task> refresh=async token=>{var rows=await Task.Run(()=>Processes.List(),token);if(!token.IsCancellationRequested)table.ItemsSource=rows;};
        ActionAsync(actions,"Refresh apps",refresh,true);
        ActionAsync(actions,"Boost selected app",async token=>{
            var item=table.SelectedItem as AppProcess;if(item==null)throw new InvalidOperationException("Select an app in the table first.");
            if(!Confirm("Trim RAM pages for "+item.Name+" (PID "+item.Id+")?\n\nThe app stays open. Pages may be loaded again, which can cause pauses. Windows processes are excluded."))return;
            long before=item.Bytes;await Task.Run(()=>Processes.Trim(item),token);await refresh(token);status.Text="Requested RAM trim for "+item.Name+". The working set can grow again; system memory changes are not a speed benchmark.";
        });
        actions.Children.Add(Theme.Button("Task Manager",()=>OpenTool("task")));
        p.Children.Add(Theme.Label("Tip: closing unused apps usually has a more lasting effect than trimming their RAM. QuietCare never kills apps or clears the system standby cache automatically.",12,Theme.Muted));
        var task=refresh(workCancel.Token);Observe(task);
    }
    async void Observe(Task task){try{await task;}catch(OperationCanceledException){}catch(Exception ex){status.Text=ex.Message;}}
    void StartupPage(StackPanel p) {
        Subtitle(p,"Disable and restore current-user Run entries with a backup. Windows settings covers other startup types, services and scheduled tasks.");
        var search=new TextBox{Margin=new Thickness(0,0,12,8),ToolTip="Filter by startup name, source or command"};System.Windows.Automation.AutomationProperties.SetName(search,"Search startup entries");p.Children.Add(Theme.Label("Search startup entries",12,Theme.Muted));p.Children.Add(search);
        var actions=Actions(p);var table=Table(p,"Startup app","Name","Status","Status","Source","Source");var detail=new TextBox{IsReadOnly=true,TextWrapping=TextWrapping.Wrap,MinHeight=62,MaxHeight=100};p.Children.Add(detail);
        table.SelectionMode=DataGridSelectionMode.Single;table.SelectionChanged+=(s,e)=>{var item=table.SelectedItem as StartupItem;detail.Text=item==null?"Select an entry to inspect its full command.":item.Command;};
        var rows=new List<StartupItem>();Action filter=()=>{string term=search.Text.Trim();table.ItemsSource=rows.Where(x=>term.Length==0||((x.Name??"")+" "+(x.Source??"")+" "+(x.Command??"")).IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0).ToList();};search.TextChanged+=(s,e)=>filter();Action refresh=()=>{rows=Startup.List();filter();};actions.Children.Add(Theme.Button("Refresh",refresh,true));
        actions.Children.Add(Theme.Button("Disable selected",()=>{var item=table.SelectedItem as StartupItem;if(item==null)throw new InvalidOperationException("Select an entry.");if(Confirm("Disable automatic startup for "+item.Name+"?\nA backup will be saved. Security, backup, sync and hardware utilities may be important to you.")){Startup.Disable(item);refresh();}}));
        actions.Children.Add(Theme.Button("Restore selected",()=>{var item=table.SelectedItem as StartupItem;if(item==null)throw new InvalidOperationException("Select a disabled backup.");if(Confirm("Restore automatic startup for "+item.Name+"?")){Startup.Restore(item);refresh();}}));
        actions.Children.Add(Theme.Button("Windows startup apps",()=>OpenTool("startup")));refresh();
        p.Children.Add(Theme.Label("Entries marked Registered may already be disabled by Windows. QuietCare does not guess their effective state or change machine-wide startup entries.",12,Theme.Muted));
        var own=new CheckBox{Content="Start QuietCare with Windows (tray + your monitor preference)",IsChecked=Startup.OwnEnabled()};own.Click+=(s,e)=>{try{Startup.SetOwn(own.IsChecked==true);refresh();}catch(Exception ex){MessageBox.Show(ex.Message);}};p.Children.Add(own);
    }
    void CleanupPage(StackPanel p) {
        Subtitle(p,"Preview old temporary files before changing anything. Eligible extensions: TMP, TEMP, LOG, ETL, DMP and CACHE, older than 7 days. Your documents and Downloads are excluded.");
        var actions=Actions(p);var summary=Theme.Label("Scan your current-user Temp folder to begin.",13,Theme.Accent);p.Children.Add(summary);var table=Table(p,"Temporary file","Path","Size","Size","Modified","Modified");table.Height=280;
        if(pendingScan!=null){table.ItemsSource=pendingScan.Items;summary.Text=pendingScan.Items.Count+" candidates • "+Format.Bytes(pendingScan.Bytes)+(pendingScan.Limited?" • partial bounded scan":"");pendingScan=null;}
        var stop=Theme.Button("Stop scan",CancelWork);stop.IsEnabled=false;
        ActionAsync(actions,"Scan temp files",async token=>{summary.Text="Scanning…";stop.IsEnabled=true;var progress=new Progress<ScanFileProgress>(value=>{if(!token.IsCancellationRequested){summary.Text=value.Examined+" entries examined • "+value.Candidates+" candidates • "+Format.Bytes(value.Bytes);summary.ToolTip=value.Path;}});try{var scan=await Task.Run(()=>cleaner.Scan(token,value=>((IProgress<ScanFileProgress>)progress).Report(value)),token);if(token.IsCancellationRequested)return;table.ItemsSource=scan.Items;summary.Text=scan.Items.Count+" candidates • "+Format.Bytes(scan.Bytes)+" • "+scan.Skipped+" inaccessible / excluded"+(scan.Limited?" • bounded scan, more files may exist":"");}catch(OperationCanceledException){summary.Text="Scan stopped. Previously displayed candidates were not changed.";throw;}finally{stop.IsEnabled=false;}},true);actions.Children.Add(stop);
        ActionAsync(actions,"Quarantine selected",async token=>{
            var selected=table.SelectedItems.Cast<TempItem>().ToList();if(selected.Count==0)throw new InvalidOperationException("Select files first. Use Ctrl or Shift for multiple rows.");
            if(!Confirm("Move "+selected.Count+" selected old temp files to QuietCare Recovery?\n\nThis is reversible. Storage is reclaimed only after you permanently delete them in Recovery. Active, changed and linked files will be skipped."))return;
            var failures=new List<string>();int moved=0;
            await Task.Run(()=>{foreach(var item in selected){if(token.IsCancellationRequested)break;try{cleaner.Quarantine(item);moved++;}catch(Exception ex){if(failures.Count<3)failures.Add(ex.Message);}}});
            summary.Text=moved+" moved to Recovery; "+(selected.Count-moved)+" skipped.";table.ItemsSource=null;State.Log("Temp cleanup quarantined "+moved+" selected files.");if(failures.Count>0)MessageBox.Show(String.Join("\n",failures),"Some files were skipped");
        });
        actions.Children.Add(Theme.Button("Select all candidates",()=>table.SelectAll()));actions.Children.Add(Theme.Button("Cancel scan",CancelWork));
        var a=Actions(p);a.Children.Add(Theme.Button("Open Recovery",()=>Navigate("Recovery")));a.Children.Add(Theme.Button("Windows deep cleanup",()=>OpenTool("storage")));a.Children.Add(Theme.Button("Automatic cleanup schedule",()=>OpenTool("sense")));
        p.Children.Add(Theme.Label("Scans are bounded to 30,000 entries or 20 seconds. Individual files over 512 MB, reparse points and hard links are skipped. QuietCare does not clean registry entries, cookies or passwords.",12,Theme.Muted));
    }
    void RecoveryPage(StackPanel p) {
        Subtitle(p,"Restore quarantined temp files or permanently delete selected files to reclaim storage. Startup backups are restored in Startup manager.");
        var actions=Actions(p);var summary=Theme.Label("",13,Theme.Accent);p.Children.Add(summary);var table=Table(p,"Original path","Original","Size","Size","Quarantined","When");
        Action refresh=()=>{var rows=cleaner.ListRecovery();table.ItemsSource=rows;summary.Text=rows.Count+" files held • "+Format.Bytes(rows.Sum(x=>x.Bytes))+" retained on disk";};
        actions.Children.Add(Theme.Button("Refresh",refresh,true));
        ActionAsync(actions,"Restore selected",async token=>{var selected=table.SelectedItems.Cast<RecoveryItem>().ToList();if(selected.Count==0)throw new InvalidOperationException("Select recovery files.");if(!Confirm("Restore "+selected.Count+" files? Existing files will never be overwritten."))return;int done=0;var errors=new List<string>();await Task.Run(()=>{foreach(var r in selected){try{cleaner.Restore(r);done++;}catch(Exception ex){if(errors.Count<3)errors.Add(ex.Message);}}});refresh();status.Text=done+" files restored.";if(errors.Count>0)MessageBox.Show(String.Join("\n",errors),"Recovery");});
        ActionAsync(actions,"Permanently delete selected",async token=>{var selected=table.SelectedItems.Cast<RecoveryItem>().ToList();if(selected.Count==0)throw new InvalidOperationException("Select recovery files.");if(!Confirm("Permanently delete "+selected.Count+" quarantined files ("+Format.Bytes(selected.Sum(x=>x.Bytes))+")?\n\nThis cannot be undone. These files will not go to the Recycle Bin."))return;int done=0;long bytes=0;var errors=new List<string>();await Task.Run(()=>{foreach(var r in selected){try{cleaner.Purge(r);done++;bytes+=r.Bytes;}catch(Exception ex){if(errors.Count<3)errors.Add(ex.Message);}}});refresh();status.Text=done+" files permanently deleted • "+Format.Bytes(bytes)+" of file content removed (physical free space may differ).";State.Log("Permanently deleted "+done+" selected quarantined files.");if(errors.Count>0)MessageBox.Show(String.Join("\n",errors),"Recovery");});
        refresh();p.Children.Add(Theme.Label("Recovery is stored locally in your Windows user profile. The app never removes it automatically, and uninstall keeps recovery data.",12,Theme.Muted));
    }
    void FilePage(StackPanel p) {
        Subtitle(p,"Find large files or matching file content in a folder you choose. Results are read only: inspect files in Explorer before deciding what to remove.");
        var path=new TextBox{Text=Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)};p.Children.Add(path);var actions=Actions(p);var summary=Theme.Label("Duplicate matching uses SHA-256 with a 1 GB hashing budget.",12,Theme.Muted);p.Children.Add(summary);var table=Table(p,"File","Path","Size","Size","Duplicate group","Group");
        actions.Children.Add(Theme.Button("Choose folder",()=>{using(var d=new System.Windows.Forms.FolderBrowserDialog()){d.Description="Choose a folder to inspect";if(d.ShowDialog()==System.Windows.Forms.DialogResult.OK)path.Text=d.SelectedPath;}}));
        foreach(bool dup in new[]{false,true}){bool duplicates=dup;ActionAsync(actions,dup?"Find duplicates":"Find large files",async token=>{string root=Path.GetFullPath(path.Text);if(!Directory.Exists(root))throw new IOException("Choose an existing folder.");summary.Text="Scanning…";bool limited=false;var rows=await Task.Run(()=>FileFinder.Find(root,duplicates,token,out limited),token);if(token.IsCancellationRequested)return;table.ItemsSource=rows;summary.Text=rows.Count+" results"+(limited?" • scan budget reached; partial results":"")+" • linked / inaccessible files excluded";},!dup);}
        actions.Children.Add(Theme.Button("Show selected in Explorer",()=>{var f=table.SelectedItem as FoundFile;if(f==null)throw new InvalidOperationException("Select a file.");if(f.Path.Contains('"'))throw new IOException("Unsupported path.");Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe"),"/select,\""+f.Path+"\""){UseShellExecute=true});}));
        actions.Children.Add(Theme.Button("Cancel scan",CancelWork));
    }
    void AppsPage(StackPanel p) {
        Subtitle(p,"Review traditional desktop programs. Windows Installed apps also includes Store apps and handles each app’s own uninstall safely.");
        var search=new TextBox{Text="",ToolTip="Filter installed apps by name or publisher"};p.Children.Add(search);var actions=Actions(p);var table=Table(p,"Application","Name","Version","Version","Publisher","Publisher");var list=new List<InstalledApp>();
        Action filter=()=>table.ItemsSource=list.Where(a=>(a.Name+" "+a.Publisher).IndexOf(search.Text,StringComparison.OrdinalIgnoreCase)>=0).ToList();search.TextChanged+=(s,e)=>filter();
        Func<CancellationToken,Task> refresh=async token=>{var rows=await Task.Run(()=>Programs.List(),token);if(token.IsCancellationRequested)return;list=rows;filter();};ActionAsync(actions,"Refresh inventory",refresh,true);
        actions.Children.Add(Theme.Button("Uninstall apps in Windows",()=>OpenTool("apps")));actions.Children.Add(Theme.Button("Microsoft Store updates ↗",()=>OpenTool("store")));Observe(refresh(workCancel.Token));
        p.Children.Add(Theme.Label("To remove Advanced SystemCare, use Uninstall apps in Windows, search for Advanced SystemCare, and run its uninstaller. QuietCare never runs untrusted uninstall commands from the registry.",12,Theme.Muted));
    }
    void ToolPage(StackPanel p) {
        Subtitle(p,"A practical maintenance toolbox. Each card opens the named Windows tool; Windows handles permissions and applies the changes you choose there.");
        var shortcuts=Actions(p);shortcuts.Children.Add(Theme.Button("Large & duplicate files",()=>Navigate("File tools"),true));shortcuts.Children.Add(Theme.Button("System information",()=>Navigate("System information")));shortcuts.Children.Add(Theme.Button("Export monitor CSV",Export));
        var filter=new TextBox{ToolTip="Search tools",Text=""};p.Children.Add(filter);var host=new StackPanel();p.Children.Add(host);
        Action render=()=>{host.Children.Clear();foreach(var group in Tools.Catalog.Where(t=>(t.Title+" "+t.Description+" "+t.Category).IndexOf(filter.Text,StringComparison.OrdinalIgnoreCase)>=0).GroupBy(t=>t.Category)) {
            host.Children.Add(Theme.Label(group.Key,18));var cards=new WrapPanel();host.Children.Add(cards);
            foreach(var t in group){var tool=t;var body=new StackPanel();body.Children.Add(Theme.Label(t.Title,15));body.Children.Add(new TextBlock{Text=t.Description,Foreground=Theme.Muted,TextWrapping=TextWrapping.Wrap,Height=55,FontSize=12});body.Children.Add(Theme.Button("Open Windows tool  ↗",()=>OpenTool(tool.Id)));var card=Theme.Card(body);card.Width=250;card.Padding=new Thickness(16);cards.Children.Add(card);}
        }};filter.TextChanged+=(s,e)=>render();render();
    }
    void InfoPage(StackPanel p) {
        Subtitle(p,"Local hardware and Windows details. QuietCare reads Windows counters without installing a hardware driver.");var actions=Actions(p);var table=Table(p,"Property","Key","Value","Value");table.Height=390;
        Func<CancellationToken,Task> refresh=async token=>{var info=await Task.Run(()=>Tools.SystemInfo(),token);if(!token.IsCancellationRequested)table.ItemsSource=info.ToList();};ActionAsync(actions,"Refresh information",refresh,true);actions.Children.Add(Theme.Button("Full Windows inventory",()=>OpenTool("system")));Observe(refresh(workCancel.Token));
    }
    void SettingsPage(StackPanel p) {
        Subtitle(p,"Set your own balance between live detail and a quiet background footprint. All preferences are saved on this PC.");
        UpdateSettingsCard(p);var body=new StackPanel();
        var widgetToggle=new CheckBox{Content="Show floating performance monitor",IsChecked=settings.ShowWidget};widgetToggle.Click+=(s,e)=>{if(widgetToggle.IsChecked!=settings.ShowWidget)ToggleWidget();};body.Children.Add(widgetToggle);
        var top=new CheckBox{Content="Keep the floating monitor above other windows",IsChecked=settings.Topmost};top.Click+=(s,e)=>{settings.Topmost=top.IsChecked==true;if(widget!=null)widget.Topmost=settings.Topmost;settings.Save();};body.Children.Add(top);
        body.Children.Add(Theme.Label("Monitor background transparency",14));var opacity=new Slider{Minimum=0.15,Maximum=1,Value=settings.WidgetOpacity,Width=270,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,12),ToolTip="Left = more transparent; right = more opaque"};opacity.ValueChanged+=(s,e)=>{settings.WidgetOpacity=opacity.Value;if(widget!=null)widget.ApplyTransparency();};opacity.PreviewMouseLeftButtonUp+=(s,e)=>settings.Save();opacity.LostKeyboardFocus+=(s,e)=>settings.Save();body.Children.Add(opacity);
        var trayToggle=new CheckBox{Content="Close main window to the notification area",IsChecked=settings.CloseToTray};trayToggle.Click+=(s,e)=>{settings.CloseToTray=trayToggle.IsChecked==true;settings.Save();};body.Children.Add(trayToggle);
        var startup=new CheckBox{Content="Start QuietCare with Windows",IsChecked=Startup.OwnEnabled()};startup.Click+=(s,e)=>{try{Startup.SetOwn(startup.IsChecked==true);}catch(Exception ex){MessageBox.Show(ex.Message);}};body.Children.Add(startup);
        body.Children.Add(Theme.Label("Monitor refresh speed",16));var rateLabel=Theme.Label("Current interval: "+settings.RefreshMs+" ms",12,Theme.Accent);body.Children.Add(rateLabel);
        var rate=new Slider{Minimum=100,Maximum=10000,TickFrequency=100,IsSnapToTickEnabled=true,Value=settings.RefreshMs,Width=350,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,12)};
        rate.ValueChanged+=(s,e)=>{int ms=(int)rate.Value;settings.RefreshMs=ms;timer.Interval=TimeSpan.FromMilliseconds(ms);rateLabel.Text="Current interval: "+ms+" ms ("+(ms/1000.0).ToString("0.0")+" seconds)";};rate.PreviewMouseLeftButtonUp+=(s,e)=>settings.Save();rate.LostKeyboardFocus+=(s,e)=>settings.Save();body.Children.Add(rate);
        var refresh=Actions(body);foreach(int value in new[]{100,250,500,1000,2000,5000,10000}){int ms=value;refresh.Children.Add(Theme.Button(ms<1000?ms+" ms":(ms/1000)+" s",()=>{rate.IsSnapToTickEnabled=false;rate.Value=ms;rate.IsSnapToTickEnabled=true;SetRefresh(ms);rateLabel.Text="Current interval: "+ms+" ms";}));}
        body.Children.Add(Theme.Label("100 ms = 10 refreshes per second. Faster polling uses more CPU; 1–2 seconds is a calmer balance. QuietCare skips a tick while a previous sensor read is still running. Monitoring pauses when both windows are hidden.",12,Theme.Muted));p.Children.Add(Theme.Card(body));
        var boostSettings=new StackPanel();boostSettings.Children.Add(Theme.Label("Smart RAM Boost protection",18));boostSettings.Children.Add(Theme.Label("The floating Boost button checks idle background processes, then trims up to 12 qualifying working sets. No apps are closed, suspended or killed. Foreground processes, active audio/calls, Windows, developer tools, terminals and trading tools are excluded. Inactive browser renderers may qualify. The option below also allows idle renderers in your active browser when Windows gives them background priority. No tabs are discarded or reloaded.",12,Theme.Muted));var browserBoost=new CheckBox{Content="Include OS-background browser renderers",IsChecked=settings.BoostBackgroundBrowsers};browserBoost.Click+=(s,e)=>{settings.BoostBackgroundBrowsers=browserBoost.IsChecked==true;settings.Save();};boostSettings.Children.Add(browserBoost);boostSettings.Children.Add(Theme.Label("Background priority is an OS signal, not knowledge of tab contents. Add chrome.exe (or your browser name) below to protect every tab, including background web projects. Browser families playing audio are protected.",11,Theme.Muted));boostSettings.Children.Add(Theme.Label("Extra protected process names (one per line)",12));var exclusions=new TextBox{Text=settings.BoostExclusions,AcceptsReturn=true,Height=65,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};exclusions.LostKeyboardFocus+=(s,e)=>{settings.BoostExclusions=exclusions.Text;settings.Save();};boostSettings.Children.Add(exclusions);boostSettings.Children.Add(Theme.Label("Example: myeditor.exe. Pages can be loaded back when an app needs them; lower RAM usage is not a speed guarantee. Boost has a 30-second cooldown to avoid repeated paging.",11,Theme.Muted));p.Children.Add(Theme.Card(boostSettings));
        var power=new StackPanel();power.Children.Add(Theme.Label("Power profile",18));power.Children.Add(Theme.Label("Performance mode can use more battery and create more heat. Some PCs expose only Balanced; use Windows settings if a plan is unavailable.",12,Theme.Muted));var modes=Actions(power);
        foreach(string value in new[]{"Balanced","Performance","Battery saver"}){string mode=value;modes.Children.Add(Theme.Button(value,()=>{if(Confirm("Change the Windows power plan to "+mode+"? The original plan will be saved."))status.Text=Tools.SetPower(mode);}));}
        modes.Children.Add(Theme.Button("Restore original",()=>{Tools.RestorePower();status.Text="Original power plan restored.";}));modes.Children.Add(Theme.Button("Windows Power settings",()=>OpenTool("power")));p.Children.Add(Theme.Card(power));
        p.Children.Add(Theme.Label("Preview build • unsigned • no external security audit. No ads, telemetry uploads, network listener, bundled drivers, automatic app killing or automatic memory purges. This build does not replace antivirus software.",12,Theme.Muted));
        var actions=Actions(p);actions.Children.Add(Theme.Button("Exit QuietCare",Exit));
    }
}
public sealed class Widget : Window {
    readonly Settings settings;readonly TextBlock compact=Theme.Label("CPU —   GPU —   RAM —",10.5),details=Theme.Label("Waiting for readings",12,Theme.Muted);readonly StackPanel body=new StackPanel();readonly Border border=new Border();readonly TextBlock heading=Theme.Label("Performance Monitor",12,Theme.Accent);readonly Button expand;readonly MainWindow main;bool expanded;public bool AllowClose;
    readonly System.Windows.Documents.Run cpuValue=new System.Windows.Documents.Run("—"),gpuValue=new System.Windows.Documents.Run("—"),ramValue=new System.Windows.Documents.Run("—");Button boostButton;readonly System.Windows.Controls.Primitives.Popup boostPopup=new System.Windows.Controls.Primitives.Popup();readonly TextBlock boostNote=Theme.Label("",12);readonly DispatcherTimer toastTimer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(12)};
    public bool IsExpanded {get{return expanded;}}
    internal Button BoostControl {get{return boostButton;}}
    public Widget(MainWindow main,Settings prefs) {
        this.main=main;settings=prefs;Title="QuietCare Monitor";SizeToContent=SizeToContent.WidthAndHeight;ResizeMode=ResizeMode.NoResize;WindowStyle=WindowStyle.None;AllowsTransparency=true;ShowInTaskbar=false;Topmost=prefs.Topmost;Background=Brushes.Transparent;Foreground=Theme.Text;FontFamily=new FontFamily("Segoe UI");
        border.BorderBrush=Theme.Brush("#4056759C");border.BorderThickness=new Thickness(1);border.CornerRadius=new CornerRadius(6);border.Padding=new Thickness(5,2,4,2);Content=border;ApplyTransparency();
        var root=new DockPanel{LastChildFill=true};border.Child=root;var head=new DockPanel{Height=22,LastChildFill=true};DockPanel.SetDock(head,Dock.Top);root.Children.Add(head);
        expand=Tiny("›",ToggleExpanded);expand.ToolTip="Expand performance monitor";DockPanel.SetDock(expand,Dock.Right);head.Children.Add(expand);
        boostButton=Tiny("⚡",main.BoostRam);boostButton.Foreground=Theme.Mint;boostButton.ToolTip="Smart RAM Boost — trim idle background apps";DockPanel.SetDock(boostButton,Dock.Left);head.Children.Add(boostButton);
        SourceInitialized+=(s,e)=>{IntPtr handle=new System.Windows.Interop.WindowInteropHelper(this).Handle;long style=Native.GetWindowLongPtr(handle,-20).ToInt64();Native.SetWindowLongPtr(handle,-20,new IntPtr(style|0x08000000));};
        compact.Margin=new Thickness(4,0,0,0);compact.VerticalAlignment=VerticalAlignment.Center;compact.Inlines.Clear();
        compact.Inlines.Add(new System.Windows.Documents.Run("CPU "){Foreground=Theme.Accent});cpuValue.Foreground=Theme.Text;cpuValue.FontWeight=FontWeights.SemiBold;compact.Inlines.Add(cpuValue);
        compact.Inlines.Add(new System.Windows.Documents.Run("   GPU "){Foreground=Theme.Violet});gpuValue.Foreground=Theme.Text;gpuValue.FontWeight=FontWeights.SemiBold;compact.Inlines.Add(gpuValue);
        compact.Inlines.Add(new System.Windows.Documents.Run("   RAM "){Foreground=Theme.Mint});ramValue.Foreground=Theme.Text;ramValue.FontWeight=FontWeights.SemiBold;compact.Inlines.Add(ramValue);head.Children.Add(compact);
        compact.ToolTip="Drag to move • double-click to expand • right-click for settings";ToolTipService.SetInitialShowDelay(compact,1500);
        compact.MouseLeftButtonDown+=(s,e)=>{if(e.ClickCount==2)ToggleExpanded();else if(e.LeftButton==MouseButtonState.Pressed){DragMove();SavePosition();}};
        body.Visibility=Visibility.Collapsed;body.Margin=new Thickness(12,10,12,8);root.Children.Add(body);body.Children.Add(heading);details.LineHeight=27;body.Children.Add(details);
        var actions=new WrapPanel();actions.Children.Add(Theme.Button("Boost RAM",main.BoostRam,true));actions.Children.Add(Theme.Button("Open QuietCare",()=>main.Reveal()));actions.Children.Add(Theme.Button("Hide",()=>main.ToggleWidget()));body.Children.Add(actions);
        boostNote.Margin=new Thickness(0);boostPopup.PlacementTarget=border;boostPopup.Placement=System.Windows.Controls.Primitives.PlacementMode.Bottom;boostPopup.StaysOpen=true;boostPopup.AllowsTransparency=true;boostPopup.Child=new Border{Background=Theme.Brush("#EE14213B"),BorderBrush=Theme.Line,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(9),Padding=new Thickness(12),Width=330,Child=BoostToast()};toastTimer.Tick+=(s,e)=>{boostPopup.IsOpen=false;toastTimer.Stop();};
        var menu=new ContextMenu();var quickBoost=new MenuItem{Header="⚡  Smart RAM Boost"};quickBoost.Click+=(s,e)=>main.BoostRam();menu.Items.Add(quickBoost);var boostDetails=new MenuItem{Header="Last Boost results"};boostDetails.Click+=(s,e)=>main.Reveal("Boost results");menu.Items.Add(boostDetails);var open=new MenuItem{Header="Open QuietCare"};open.Click+=(s,e)=>main.Reveal();menu.Items.Add(open);var rates=new MenuItem{Header="Refresh speed"};foreach(int value in new[]{100,250,500,1000,2000,5000,10000}){int ms=value;var choice=new MenuItem{Header=ms<1000?ms+" ms":(ms/1000)+" seconds",IsCheckable=true};choice.Click+=(s,e)=>main.SetRefresh(ms);rates.Items.Add(choice);}rates.SubmenuOpened+=(s,e)=>{int index=0;int[] values={100,250,500,1000,2000,5000,10000};foreach(MenuItem choice in rates.Items)choice.IsChecked=values[index++]==settings.RefreshMs;};menu.Items.Add(rates);var prefsItem=new MenuItem{Header="Transparency & settings"};prefsItem.Click+=(s,e)=>main.Reveal("Settings");menu.Items.Add(prefsItem);var hide=new MenuItem{Header="Hide monitor"};hide.Click+=(s,e)=>main.ToggleWidget();menu.Items.Add(hide);var exit=new MenuItem{Header="Exit QuietCare"};exit.Click+=(s,e)=>main.Exit();menu.Items.Add(exit);ContextMenu=menu;
        Loaded+=(s,e)=>{Rect area=SystemParameters.WorkArea;Left=settings.WidgetX>=area.Left && settings.WidgetX<=area.Right-ActualWidth?settings.WidgetX:area.Right-ActualWidth-18;Top=settings.WidgetY>=area.Top && settings.WidgetY<=area.Bottom-ActualHeight?settings.WidgetY:area.Top+32;};
        SizeChanged+=(s,e)=>{if(IsLoaded && !Double.IsNaN(Left))Left=Math.Min(Left,SystemParameters.WorkArea.Right-ActualWidth);};
        Closing+=(s,e)=>{boostPopup.IsOpen=false;toastTimer.Stop();if(!AllowClose){e.Cancel=true;Hide();}SavePosition();};IsVisibleChanged+=(s,e)=>{if(!IsVisible){boostPopup.IsOpen=false;toastTimer.Stop();}};
    }
    Button Tiny(string text,Action action){var b=Theme.Button(text,action);b.Background=Brushes.Transparent;b.Foreground=Theme.Accent;b.Margin=new Thickness(0);b.Padding=new Thickness(4,0,4,0);b.FontSize=15;return b;}
    void ToggleExpanded(){expanded=!expanded;SizeToContent=expanded?SizeToContent.Manual:SizeToContent.WidthAndHeight;if(expanded){Width=430;Height=248;}else{Width=Double.NaN;Height=Double.NaN;}body.Visibility=expanded?Visibility.Visible:Visibility.Collapsed;expand.Content=expanded?"‹":"›";UpdateLayout();Left=Math.Min(Left,SystemParameters.WorkArea.Right-ActualWidth);Top=Math.Min(Top,SystemParameters.WorkArea.Bottom-ActualHeight);ApplyTransparency();main.RefreshMonitor();}
    public void ApplyTransparency(){var color=Color.FromArgb((byte)(255*Settings.Opacity(settings.WidgetOpacity)),8,12,35);var brush=new SolidColorBrush(color);brush.Freeze();border.Background=brush;}
    void SavePosition(){settings.WidgetX=Left;settings.WidgetY=Top;settings.Save();}
    StackPanel BoostToast(){var panel=new StackPanel();panel.Children.Add(boostNote);panel.Children.Add(Theme.Button("View Boost results",()=>{boostPopup.IsOpen=false;main.Reveal("Boost results");}));return panel;}
    public void BoostFeedback(string message,bool busy){boostButton.Content=busy?"…":"⚡";boostButton.IsEnabled=!busy;boostNote.Text=message;if(!busy&&IsVisible){boostPopup.IsOpen=true;toastTimer.Stop();toastTimer.Start();}}
    public void Update(Snapshot s){string cpu=Format.Percent(s.Cpu),gpu=Format.Percent(s.Gpu),ram=Format.Percent(s.Ram);if(cpuValue.Text!=cpu)cpuValue.Text=cpu;if(gpuValue.Text!=gpu)gpuValue.Text=gpu;if(ramValue.Text!=ram)ramValue.Text=ram;if(expanded)details.Text="CPU   "+cpu+"       GPU   "+gpu+"\nRAM   "+ram+"  ·  "+Format.Bytes(s.Available)+" available\nDisk   ↓ "+Format.Rate(s.DiskRead)+"   ↑ "+Format.Rate(s.DiskWrite)+"\nNetwork   ↓ "+Format.Rate(s.Download)+"   ↑ "+Format.Rate(s.Upload);}
}
}
