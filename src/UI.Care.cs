using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace QuietCare {
public sealed partial class MainWindow {
    CareScanner careScanner;CancellationTokenSource careCancel;CareScanReport careReport;bool careBusy;
    readonly Dictionary<string,CareProgress> careSteps=new Dictionary<string,CareProgress>();
    readonly Dictionary<string,TextBlock> careRowStatus=new Dictionary<string,TextBlock>();
    TextBlock careHeading,careDetail,careItem,careElapsed;ProgressBar careBar;ScanDial careDial;StackPanel careResults;
    Button careStop,careSkip,careRun;DateTime careStarted;int careTotal,careCompleted;
    internal bool CareScanRunning {get{return careBusy;}}
    internal CareScanReport LastCareReport {get{return careReport;}}
    internal Task StartCareScan(){return RunCareScan();}
    internal void StopCareScan(){if(careCancel!=null)careCancel.Cancel();}
    IEnumerable<string> SelectedCare(){return (settings.ScanCategories??String.Join(",",CareScanner.Ids)).Split(',').Where(CareScanner.Ids.Contains).ToArray();}
    void CareChoices(Panel parent){
        var choices=new System.Windows.Controls.Primitives.UniformGrid{Columns=2,Margin=new Thickness(0,0,0,6)};
        var selected=new HashSet<string>(SelectedCare());
        for(int i=0;i<CareScanner.Ids.Length;i++){
            string id=CareScanner.Ids[i];var choice=new CheckBox{Content=CareScanner.Names[i],IsChecked=selected.Contains(id),FontSize=11,Margin=new Thickness(0,3,8,7)};
            choice.Click+=(s,e)=>{if(choice.IsChecked==true)selected.Add(id);else selected.Remove(id);settings.ScanCategories=String.Join(",",CareScanner.Ids.Where(selected.Contains));settings.Save();};choices.Children.Add(choice);
        }
        parent.Children.Add(choices);
    }
    async Task RunCareScan(){
        if(careBusy){Navigate("Care scan");return;}
        if(operationRunning||booster.Busy){status.Text="Finish the current action before starting a Care scan.";return;}
        var selected=SelectedCare().ToArray();if(selected.Length==0){status.Text="Choose at least one category beside SCAN.";return;}
        careCancel=new CancellationTokenSource();careBusy=true;careReport=null;careSteps.Clear();careTotal=selected.Length;careCompleted=0;careStarted=DateTime.UtcNow;
        Navigate("Care scan");var token=careCancel.Token;var clock=new DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};clock.Tick+=(s,e)=>{if(careElapsed!=null&&current=="Care scan")careElapsed.Text="Elapsed "+(DateTime.UtcNow-careStarted).ToString(@"m\:ss");};clock.Start();
        var progress=new Progress<CareProgress>(p=>{if(exiting||!careBusy)return;careSteps[p.Id]=p;careCompleted=p.Completed;RefreshCareProgress(p);});
        try{careReport=await Task.Run(()=>careScanner.Run(selected,progress,token));}
        catch(Exception ex){status.Text="Care scan could not finish: "+ex.Message;careReport=new CareScanReport{Stopped=true,Duration=DateTime.UtcNow-careStarted};}
        finally{clock.Stop();careBusy=false;careCancel.Dispose();careCancel=null;}
        if(exiting)return;
        foreach(var result in careReport.Results)careSteps[result.Id]=new CareProgress{Id=result.Id,Name=result.Name,State=result.State,Detail=result.Summary};
        status.Text=(careReport.Stopped?"Scan stopped. ":"Scan completed. ")+careReport.Results.Count+" categories processed. No changes were applied.";
        if(IsVisible&&current=="Care scan")Navigate("Care scan");
    }
    void RefreshCareProgress(CareProgress progress=null){
        if(current!="Care scan"||careHeading==null)return;
        foreach(var pair in careRowStatus){CareProgress step;if(!careSteps.TryGetValue(pair.Key,out step)){pair.Value.Text=careBusy?"Waiting":"Not checked";pair.Value.Foreground=Theme.Muted;continue;}pair.Value.Text=CareStateText(step.State);pair.Value.Foreground=step.State==CareState.Running?Theme.Accent:step.State==CareState.Complete?Theme.Mint:Theme.Amber;}
        if(progress!=null){careHeading.Text=progress.State==CareState.Running?progress.Name:"Checking the next category…";careDetail.Text=progress.Detail;string item=progress.CurrentItem??"";string profile=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);if(profile.Length>0&&item.StartsWith(profile,StringComparison.OrdinalIgnoreCase))item="%USERPROFILE%"+item.Substring(profile.Length);careItem.Text=item;careItem.ToolTip=progress.CurrentItem;}
        if(careBar!=null){careBar.Maximum=Math.Max(1,careTotal);careBar.Value=careCompleted;}
        if(careDial!=null){careDial.Caption=careCompleted+"/"+careTotal;careDial.InvalidateVisual();}
    }
    static string CareStateText(CareState state){switch(state){case CareState.Running:return "Checking…";case CareState.Complete:return "Checked";case CareState.Partial:return "Partial";case CareState.Unavailable:return "Unavailable";case CareState.Skipped:return "Skipped";default:return "Waiting";}}
    void CareScanPage(StackPanel p){
        if(!careBusy&&careReport==null)careTotal=SelectedCare().Count();
        Subtitle(p,"Live, read-only checks. Progress counts completed categories, not an estimated time remaining. Review each result before making changes.");
        careRowStatus.Clear();var columns=new Grid();columns.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(210)});columns.ColumnDefinitions.Add(new ColumnDefinition());
        var queue=new StackPanel();queue.Children.Add(Theme.Label("SCAN STEPS",11,Theme.Muted));
        for(int i=0;i<CareScanner.Ids.Length;i++){
            var row=new StackPanel{Margin=new Thickness(0,0,8,17)};row.Children.Add(Theme.Label((i+1).ToString("00")+"   "+CareScanner.Names[i],13));var label=Theme.Label("Waiting",11,Theme.Muted);label.Margin=new Thickness(30,-3,0,0);row.Children.Add(label);careRowStatus[CareScanner.Ids[i]]=label;queue.Children.Add(row);
        }
        columns.Children.Add(queue);var live=new StackPanel{Margin=new Thickness(12,0,8,0)};careDial=new ScanDial();live.Children.Add(new Viewbox{Width=220,Height=220,Child=careDial,HorizontalAlignment=HorizontalAlignment.Center});
        careHeading=Theme.Label(careBusy?"Starting scan…":careReport==null?"Ready to scan":careReport.Stopped?"Scan stopped":"Checks completed",20,Theme.Accent);live.Children.Add(careHeading);
        careDetail=Theme.Label(careBusy?"Reading your selected categories…":careReport==null?"Choose Run scan to begin.":careReport.Results.Count+" categories processed. No changes applied.",12,Theme.Muted);live.Children.Add(careDetail);
        careItem=Theme.Label("",11,Theme.Muted);careItem.TextWrapping=TextWrapping.NoWrap;careItem.TextTrimming=TextTrimming.CharacterEllipsis;live.Children.Add(careItem);
        careBar=new ProgressBar{Height=5,Minimum=0,Maximum=Math.Max(1,careTotal),Value=careCompleted,Foreground=Theme.Accent,Margin=new Thickness(0,5,0,8)};live.Children.Add(careBar);
        careElapsed=Theme.Label("Elapsed "+(careBusy?DateTime.UtcNow-careStarted:careReport==null?TimeSpan.Zero:careReport.Duration).ToString(@"m\:ss"),11,Theme.Muted);live.Children.Add(careElapsed);
        var controls=Actions(live);careStop=Theme.Button("Stop scan",()=>{StopCareScan();careDetail.Text="Stopping after the current Windows call returns…";});careStop.IsEnabled=careBusy;controls.Children.Add(careStop);
        careSkip=Theme.Button("Skip step",()=>{careScanner.SkipCurrent();careDetail.Text="Skipping after the current check responds…";});careSkip.IsEnabled=careBusy;controls.Children.Add(careSkip);
        careRun=Theme.Button("Run again",()=>Observe(RunCareScan()),true);careRun.IsEnabled=!careBusy;controls.Children.Add(careRun);Grid.SetColumn(live,1);columns.Children.Add(live);p.Children.Add(Theme.Card(columns));
        careResults=new StackPanel();p.Children.Add(careResults);RefreshCareProgress();
        if(!careBusy&&careReport!=null){
            careCompleted=careReport.Results.Count;RefreshCareProgress();careResults.Children.Add(Theme.Label("Your results",20));
            if(careReport.Stopped)careResults.Children.Add(Theme.Label("Only finished categories are listed. Remaining categories have not been checked.",12,Theme.Amber));
            foreach(var result in careReport.Results){var captured=result;var body=new StackPanel();body.Children.Add(Theme.Label(result.Name+"  ·  "+CareStateText(result.State),16,result.State==CareState.Complete?Theme.Mint:Theme.Amber));body.Children.Add(Theme.Label(result.Summary,12,Theme.Muted));
                if(result.Findings.Count>0){var table=new DataGrid{ItemsSource=result.Findings,Height=180,Margin=new Thickness(0,4,0,10)};table.Columns.Add(new DataGridTextColumn{Header="Item",Binding=new System.Windows.Data.Binding("Item"),Width=new DataGridLength(1,DataGridLengthUnitType.Star)});table.Columns.Add(new DataGridTextColumn{Header="Observed detail",Binding=new System.Windows.Data.Binding("Detail"),Width=new DataGridLength(2,DataGridLengthUnitType.Star)});body.Children.Add(new Expander{Header="View observations ("+result.Findings.Count+")",Content=table,Foreground=Theme.Text,Margin=new Thickness(0,0,0,8)});}
                body.Children.Add(Theme.Button(ReviewLabel(result.Id),()=>ReviewCareResult(captured)));careResults.Children.Add(Theme.Card(body));
            }
        }
        var end=Actions(p);end.Children.Add(Theme.Button("Back to Care",()=>Navigate("Overview")));end.Children.Add(Theme.Button("Choose scan categories",()=>{if(careBusy){status.Text="Stop the current scan before changing categories.";return;}Navigate("Overview");}));
    }
    static string ReviewLabel(string id){switch(id){case "temp":return "Review temp candidates";case "startup":return "Review startup entries";case "memory":return "Review processes & RAM";case "storage":return "Open Windows Storage ↗";case "apps":return "Review installed apps";default:return "Review Windows protection";}}
    void ReviewCareResult(CareCheckResult result){switch(result.Id){case "temp":pendingScan=result.TempFiles;Navigate("Cleanup");break;case "startup":Navigate("Startup manager");break;case "memory":Navigate("Processes & RAM");break;case "storage":OpenTool("storage");break;case "apps":Navigate("Installed apps");break;default:Navigate("Protect");break;}}
}
}
