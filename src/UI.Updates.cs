using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace QuietCare {
public sealed partial class MainWindow {
    IUpdateService updates;CancellationTokenSource updateCancel;UpdateRelease updateRelease;VerifiedInstaller verifiedInstaller;
    bool updateBusy;string updateMessage="Check GitHub for a newer QuietCare release.";DateTime updateCooldown;
    TextBlock updateStatus,updateVersion;Button updateCheck,updateDownload,updateInstall,updateStop;TextBox updateNotes;ProgressBar updateProgress;
    internal bool UpdateRunning {get{return updateBusy;}}
    internal string UpdateMessage {get{return updateMessage;}}
    internal Task CheckUpdatesForTest(){return CheckQuietCareUpdates(true);}
    bool UpdateAvailable(){ReleaseVersion currentVersion;ReleaseVersion.TryParse(AppInfo.VersionText,out currentVersion);return updateRelease!=null&&updateRelease.Version.CompareTo(currentVersion)>0;}
    void UpdateCard(StackPanel p){
        var body=new StackPanel();body.Children.Add(Theme.Label("QuietCare updates",20,Theme.Accent));body.Children.Add(Theme.Label("Installed: "+AppInfo.VersionText+" Preview · Windows x64",12,Theme.Muted));
        updateStatus=Theme.Label(updateMessage,13,Theme.Text);body.Children.Add(updateStatus);updateVersion=Theme.Label("",12,Theme.Muted);body.Children.Add(updateVersion);
        var buttons=Actions(body);updateCheck=Theme.Button("Check for updates",()=>Observe(CheckQuietCareUpdates(true)),true);buttons.Children.Add(updateCheck);
        updateDownload=Theme.Button("Download verified installer",()=>Observe(DownloadQuietCareUpdate()));buttons.Children.Add(updateDownload);
        updateInstall=Theme.Button("Open installer",()=>{if(Confirm("Open the downloaded QuietCare installer and exit this app?\n\nThe SHA-256 will be checked again. This preview is unsigned. Review any Windows prompt; installation still requires your choice in Setup.")){GitHubUpdates.OpenInstaller(verifiedInstaller);Exit();}});buttons.Children.Add(updateInstall);
        updateStop=Theme.Button("Cancel",()=>{if(updateCancel!=null)updateCancel.Cancel();});buttons.Children.Add(updateStop);
        var links=Actions(body);links.Children.Add(Theme.Button("Release notes on GitHub ↗",()=>GitHubUpdates.OpenReleases(updateRelease)));links.Children.Add(Theme.Button("Report a problem ↗",()=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/"+AppInfo.Repository+"/issues/new/choose"){UseShellExecute=true})));
        updateProgress=new ProgressBar{Height=5,Maximum=100,Visibility=Visibility.Collapsed,Foreground=Theme.Accent,Margin=new Thickness(0,0,0,10)};body.Children.Add(updateProgress);
        updateNotes=new TextBox{IsReadOnly=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,MaxHeight=160,Visibility=Visibility.Collapsed};body.Children.Add(updateNotes);
        body.Children.Add(Theme.Label("Checks contact GitHub only. Downloads require an exact project URL and GitHub SHA-256 digest; the installer opens only after your confirmation. No silent installation.",11,Theme.Muted));p.Children.Add(Theme.Card(body));RefreshUpdateView();
    }
    void RefreshUpdateView(){
        if(exiting||current!="Updates"||updateStatus==null)return;
        updateStatus.Text=updateMessage;updateVersion.Text=updateRelease==null?"Last attempted check: "+(settings.LastUpdateAttemptUtc==DateTime.MinValue?"never":settings.LastUpdateAttemptUtc.ToLocalTime().ToString("g")):"Published: "+updateRelease.Version.Tag+(updateRelease.Preview?" · Preview":" · Stable");
        updateCheck.IsEnabled=!updateBusy;updateDownload.IsEnabled=!updateBusy&&UpdateAvailable()&&updateRelease.Installer!=null&&verifiedInstaller==null;updateInstall.IsEnabled=!updateBusy&&verifiedInstaller!=null;updateStop.IsEnabled=updateBusy;
        updateNotes.Text=updateRelease==null?"":updateRelease.Notes;updateNotes.Visibility=String.IsNullOrWhiteSpace(updateNotes.Text)?Visibility.Collapsed:Visibility.Visible;
    }
    async Task CheckQuietCareUpdates(bool userRequested){
        if(updateBusy)return;if(DateTime.UtcNow<updateCooldown){updateMessage="Please wait a minute between update checks.";RefreshUpdateView();return;}
        updateBusy=true;verifiedInstaller=null;updateRelease=null;updateCancel=new CancellationTokenSource();updateCooldown=DateTime.UtcNow.AddMinutes(1);settings.LastUpdateAttemptUtc=DateTime.UtcNow;updateMessage="Checking QuietCare releases on GitHub…";RefreshUpdateView();
        try{
            try{settings.Save();}catch(Exception){State.Log("Update-check preference could not be saved.");}
            if(updates==null)updates=new GitHubUpdates();var found=await updates.Check(settings.IncludePreviewUpdates,updateCancel.Token);updateCancel.Token.ThrowIfCancellationRequested();if(exiting)return;updateRelease=found;
            ReleaseVersion currentVersion;ReleaseVersion.TryParse(AppInfo.VersionText,out currentVersion);
            updateMessage=updateRelease==null?"No published release matches this update channel.":UpdateAvailable()?"QuietCare "+updateRelease.Version.Tag+" is available.":updateRelease.Version.CompareTo(currentVersion)<0?"This build is newer than the latest published release.":"You're up to date.";
            if(UpdateAvailable()&&updateRelease.Installer==null)updateMessage+=" Use the release page; no verifiable supported installer was found.";
            if(!userRequested&&UpdateAvailable()){tray.BalloonTipTitle="QuietCare update available";tray.BalloonTipText="Open App & updates to review "+updateRelease.Version.Tag+". Nothing was downloaded or installed.";tray.ShowBalloonTip(6000);}
        }catch(OperationCanceledException){updateMessage="Update check canceled or timed out. You can try again later.";}catch(Exception ex){updateMessage="Update check could not finish. "+FriendlyUpdateError(ex);}
        finally{updateBusy=false;updateCancel.Dispose();updateCancel=null;RefreshUpdateView();}
    }
    async Task DownloadQuietCareUpdate(){
        if(updateBusy||!UpdateAvailable()||updateRelease.Installer==null)return;
        updateBusy=true;updateCancel=new CancellationTokenSource();updateMessage="Downloading and verifying the installer…";RefreshUpdateView();if(updateProgress!=null){updateProgress.Visibility=Visibility.Visible;updateProgress.Value=0;}
        var progress=new Progress<int>(value=>{if(!exiting&&current=="Updates"&&updateProgress!=null)updateProgress.Value=value;});
        try{var downloaded=await updates.Download(updateRelease,progress,updateCancel.Token);updateCancel.Token.ThrowIfCancellationRequested();verifiedInstaller=downloaded;updateMessage="SHA-256 verified. Choose Open installer when you're ready; QuietCare will exit.";}
        catch(OperationCanceledException){updateMessage="Download canceled or timed out. No installer was opened.";}catch(Exception ex){updateMessage="Download was not opened. "+FriendlyUpdateError(ex);}
        finally{updateBusy=false;updateCancel.Dispose();updateCancel=null;if(updateProgress!=null)updateProgress.Visibility=Visibility.Collapsed;RefreshUpdateView();}
    }
    static string FriendlyUpdateError(Exception ex){return ex is System.IO.InvalidDataException||ex is System.Net.Http.HttpRequestException?ex.Message:"Check your connection or open the GitHub release page, then try again.";}
    void UpdateSettingsCard(StackPanel p){
        var body=new StackPanel();body.Children.Add(Theme.Label("App updates & support",18));body.Children.Add(Theme.Label("QuietCare "+AppInfo.VersionText+" Preview. Update checks send the app version in a User-Agent to GitHub; no hardware inventory, local files or credentials are sent.",12,Theme.Muted));
        var automatic=new CheckBox{Content="Check at startup, at most once a week",IsChecked=settings.CheckUpdatesWeekly};automatic.Click+=(s,e)=>{settings.CheckUpdatesWeekly=automatic.IsChecked==true;settings.Save();};body.Children.Add(automatic);
        var previews=new CheckBox{Content="Include preview releases",IsChecked=settings.IncludePreviewUpdates};previews.Click+=(s,e)=>{settings.IncludePreviewUpdates=previews.IsChecked==true;settings.Save();if(updateCancel!=null)updateCancel.Cancel();updateCooldown=DateTime.MinValue;updateRelease=null;verifiedInstaller=null;updateMessage="Update channel changed. Check again to refresh release information.";};body.Children.Add(previews);
        body.Children.Add(Theme.Label("The optional check is off by default. There is no recurring background update timer and no automatic download or installation. A failed check also counts toward the weekly startup interval.",11,Theme.Muted));body.Children.Add(Theme.Button("Open QuietCare updates",()=>Navigate("Updates"),true));p.Children.Add(Theme.Card(body));
    }
}
}
