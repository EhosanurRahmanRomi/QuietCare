using System;
using System.Linq;
using System.Windows.Controls;

namespace QuietCare {
public sealed partial class MainWindow {
    BoostResult lastBoost;
    internal BoostResult LastBoost {get{return lastBoost;}}
    void BoostResultsPage(StackPanel p){
        Subtitle(p,"Measured feedback from your most recent one-click Boost. Resident memory can return when an app needs its pages again.");
        var actions=Actions(p);actions.Children.Add(Theme.Button("Boost RAM",BoostRam,true));actions.Children.Add(Theme.Button("Protection settings",()=>Navigate("Settings")));actions.Children.Add(Theme.Button("Review running apps",()=>Navigate("Processes & RAM")));
        if(lastBoost==null){p.Children.Add(Theme.Label("No Boost has run in this session. Click Boost RAM here or use the floating lightning button.",15,Theme.Muted));return;}
        var result=lastBoost;var body=new StackPanel();body.Children.Add(Theme.Label(result.Trimmed==0?"No memory trim applied":result.ObservedReductions+" resident decreases observed",22,Theme.Mint));body.Children.Add(Theme.Label(result.Summary,14));
        body.Children.Add(Theme.Label(result.Eligible+" eligible  ·  "+result.Trimmed+" accepted  ·  "+result.Failed+" declined / unavailable  ·  "+result.Skipped+" protected / skipped",12,Theme.Muted));
        body.Children.Add(Theme.Label("Available physical RAM: "+Format.Bytes(result.AvailableBefore)+" → "+Format.Bytes(result.AvailableAfter),16,Theme.Accent));p.Children.Add(Theme.Card(body));
        if(result.Observations.Count>0){p.Children.Add(Theme.Label("Per-process measurements",18));var table=Table(p,"App","App","Before","Before","After","After","Observed result","Outcome");table.Height=230;table.ItemsSource=result.Observations;}
        if(result.SkipReasons.Count>0){p.Children.Add(Theme.Label("Why apps were protected or skipped",18));var reasons=new StackPanel();foreach(var reason in result.SkipReasons.OrderByDescending(x=>x.Value))reasons.Children.Add(Theme.Label(reason.Value+"  ·  "+reason.Key,13,Theme.Muted));p.Children.Add(Theme.Card(reasons));}
        if(result.Failed>0){var failures=new TextBox{IsReadOnly=true,Text=String.Join(Environment.NewLine,result.Details.Where(x=>x.Contains("declined / unavailable"))),TextWrapping=System.Windows.TextWrapping.Wrap,MaxHeight=120};p.Children.Add(failures);}
        p.Children.Add(Theme.Label("An accepted Windows request is different from an observed decrease. Per-process working sets can share pages, so they are not added together as ‘RAM freed’. Other activity changes the physical-RAM reading. Boost does not close apps, discard tabs, reduce committed allocations, or guarantee a speed increase.",12,Theme.Muted));
    }
}
}
