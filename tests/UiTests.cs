using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using QuietCare;

internal static class UiTests {
    static int passed,failed;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Test(string name,Action action){try{action();passed++;Console.WriteLine("PASS "+name);}catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+": "+ex);}}
    static void Pump(int ms){var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(ms)};timer.Tick+=(s,e)=>{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
    static IEnumerable<DependencyObject> Descendants(DependencyObject root){if(root==null)yield break;yield return root;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in Descendants(VisualTreeHelper.GetChild(root,i)))yield return child;}
    sealed class BoostHost : IBoostRuntime {
        public int Trimmed;
        public List<BoostProcess> Capture(){return new List<BoostProcess>{new BoostProcess{App=new AppProcess{Id=999001,Name="UIBackgroundFixture",Path="C:\\Fixture\\UIBackgroundFixture.exe",Bytes=100*1024*1024,Started=new DateTime(2020,1,1,0,0,0,DateTimeKind.Utc)}}};}
        public int Foreground(){return 0;}public AudioActivity Audio(){return new AudioActivity{Verified=true};}public void Trim(AppProcess p){if(p.Id!=999001)throw new Exception("Unexpected process");Trimmed++;}public ulong Available(){return 2UL*1024*1024*1024;}
    }
    [STAThread]static int Main(string[] args){
        State.DirectoryPath=Path.Combine(args[0],"UiState");var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Theme.Install(app);
        var settings=new Settings{ShowWidget=false,CloseToTray=false};var boost=new BoostHost();var main=new MainWindow(settings,true,boost);app.MainWindow=main;
        Test("Cold monitor/tray launch does not construct the main UI",()=>{main.StartInTray();Check(!main.ShellBuilt,"Main UI eagerly created");Check(!main.IsVisible&&!main.SamplingScheduled,"Hidden app is polling");});
        var widget=new Widget(main,settings);
        Test("Floating Boost runs directly without opening any chooser",()=>{widget.BoostControl.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));for(int i=0;i<50&&boost.Trimmed==0;i++)Pump(50);Check(boost.Trimmed==1,"Button did not invoke direct Boost");Check(!main.ShellBuilt&&!main.IsVisible,"Boost opened the main chooser");});
        Test("Transparent widget stays compact at maximum values",()=>{widget.Update(new Snapshot{Cpu=100,Gpu=100,Ram=100,Available=1024,Total=4096});var root=(FrameworkElement)widget.Content;root.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));root.Arrange(new Rect(root.DesiredSize));root.UpdateLayout();Check(widget.AllowsTransparency,"Not a transparent window");Check(root.DesiredSize.Width<=240&&root.DesiredSize.Height<=32,"Widget too large: "+root.DesiredSize);var text=Descendants(root).OfType<TextBlock>().Select(t=>t.Text).First(t=>t.Contains("CPU")&&t.Contains("GPU")&&t.Contains("RAM"));Check(text.IndexOf("CPU")<text.IndexOf("GPU")&&text.IndexOf("GPU")<text.IndexOf("RAM"),"Wrong order");Check(((SolidColorBrush)((Border)root).Background).Color.A<255,"Background opaque");});
        Test("Unknown GPU values render a dash and no bogus zero",()=>{widget.Update(new Snapshot{Cpu=1,Gpu=null,Ram=50});var texts=Descendants((FrameworkElement)widget.Content).OfType<TextBlock>().Select(t=>t.Text);Check(texts.Any(t=>t.Contains("GPU —")),"Missing GPU was not shown as unavailable");});
        string[] pages={"Overview","Speed Up","Protect","Updates","Processes & RAM","Startup manager","Cleanup","File tools","Installed apps","Toolbox","System information","Recovery","Settings"};
        foreach(string page in pages){string name=page;Test("Layout and controls: "+name,()=>{main.InspectPage(name);var root=(FrameworkElement)main.Content;root.Measure(new Size(980,650));root.Arrange(new Rect(0,0,980,650));root.UpdateLayout();Pump(120);var view=(ScrollViewer)main.PageView;Check(view.Content!=null,"Empty page");Check(Descendants(view).OfType<Button>().Any(),"Page has no actions");Check(view.ExtentWidth<=view.ViewportWidth+2,"Horizontal content overflow: "+view.ExtentWidth+" / "+view.ViewportWidth);Check(main.CurrentPage==name,"Navigation mismatch");});}
        Test("Title strip removed; transparent caption controls preserve page space",()=>{main.InspectPage("Overview");var canvas=(Grid)((Border)main.Content).Child;Check(canvas.RowDefinitions.Count==2&&canvas.RowDefinitions[0].Height.IsStar,"Separate title row still reserves space");var controls=Descendants(canvas).OfType<StackPanel>().Single(x=>x.Name=="CaptionControls");Check(controls.Children.Count==4&&((SolidColorBrush)controls.Background).Color.A==0,"Caption overlay is opaque or incomplete");Check(controls.Children.OfType<Button>().All(x=>((SolidColorBrush)x.Background).Color.A==0),"Caption button background is opaque");Check(Descendants(canvas).OfType<Border>().Any(x=>x.Name=="WindowDragRegion"&&x.Height>=12),"Window has no draggable region");});
        Test("All pages fit the minimum supported window width",()=>{foreach(string page in pages){main.InspectPage(page);var root=(FrameworkElement)main.Content;root.Measure(new Size(858,608));root.Arrange(new Rect(0,0,858,608));root.UpdateLayout();var view=(ScrollViewer)main.PageView;Check(view.ViewportWidth>600,"Content area too narrow: "+page);Check(view.ExtentWidth<=view.ViewportWidth+2,"Horizontal overflow at minimum width: "+page);var controls=Descendants(root).OfType<StackPanel>().Single(x=>x.Name=="CaptionControls");var bounds=controls.TransformToAncestor(root).TransformBounds(new Rect(controls.RenderSize));Check(bounds.Right<=root.ActualWidth+1&&bounds.Top>=0,"Caption controls clipped");}});
        Test("Data tables sort memory by byte count and expose tooltips",()=>{main.InspectPage("Processes & RAM");var root=(FrameworkElement)main.Content;root.Measure(new Size(980,650));root.Arrange(new Rect(0,0,980,650));root.UpdateLayout();var grid=Descendants(main.PageView).OfType<DataGrid>().Single();Check(grid.Columns.Any(c=>c.Header.ToString()=="RAM"&&c.SortMemberPath=="Bytes"),"RAM sorting is lexical");Check(grid.Columns.OfType<DataGridTextColumn>().All(c=>c.ElementStyle.Setters.OfType<Setter>().Any(s=>s.Property==FrameworkElement.ToolTipProperty)),"Missing full-value tooltips");});
        widget.AllowClose=true;widget.Close();main.Exit();Console.WriteLine("UI RESULT "+passed+" passed / "+failed+" failed");return failed==0?0:1;
    }
}
