using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using Cleaner.App.Services;
using Cleaner.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cleaner.App.ViewModels.Pages;

public sealed partial class FocusViewModel : ObservableObject,IDisposable
{
    readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromSeconds(2)};
    readonly RunningTaskRegistry tasks;
    readonly CareHubViewModel hub;
    FocusProcess? lastWork;
    bool polling;
    public static bool Active {get;private set;}
    public static event Action<bool>? ActiveChanged;
    [ObservableProperty] bool _isActive;
    [ObservableProperty] bool _busy;
    [ObservableProperty] bool _needsRecovery;
    [ObservableProperty] int _minutes=60;
    [ObservableProperty] int _durationAmount=60;
    [ObservableProperty] int _durationUnitIndex;
    public IReadOnlyList<string> DurationUnits {get;}=new[]{"Minutes","Hours","Until I end it"};
    public bool HasTimedDuration=>DurationUnitIndex!=2;
    public int DurationMaximum=>DurationUnitIndex==1?168:FocusDuration.MaxMinutes;
    public string DurationHint=>HasTimedDuration?"Up to 7 days. End earlier whenever you want.":"No timer. Ends when you stop Focus or exit QuietCare.";
    partial void OnDurationUnitIndexChanged(int oldValue,int newValue)
    {
        int value=newValue;
        if(value is <0 or >2){DurationUnitIndex=0;return;}
        if(value!=2)
        {
            int converted=oldValue==0&&value==1?(int)Math.Ceiling(DurationAmount/60.0):oldValue==1&&value==0?Math.Min(FocusDuration.MaxMinutes,DurationAmount*60):DurationAmount;
            DurationAmount=Math.Clamp(converted,1,DurationMaximum);
        }
        OnPropertyChanged(nameof(HasTimedDuration));OnPropertyChanged(nameof(DurationMaximum));OnPropertyChanged(nameof(DurationHint));
    }
    [ObservableProperty] bool _prioritizeWork=true;
    [ObservableProperty] bool _lowerBackground=true;
    [ObservableProperty] bool _quietNotifications=true;
    [ObservableProperty] bool _keepAwake=true;
    [ObservableProperty] bool _guardUpdates=true;
    [ObservableProperty] string _headline="Make room for your work";
    [ObservableProperty] string _countdown="Ready when you are";
    [ObservableProperty] string _workApp="Automatic · follows the app you use";
    [ObservableProperty] string _priorityStatus="Work apps get Above normal CPU priority; eligible background apps get Below normal.";
    [ObservableProperty] string _notificationStatus="Windows Do not disturb, when supported. Priority alerts can still appear.";
    [ObservableProperty] string _awakeStatus="Keep your PC and display awake during the session.";
    [ObservableProperty] string _updateStatus="Temporary update guard · administrator approval required on supported Windows editions.";
    [ObservableProperty] string _detail="Switch to your work app after starting. Nothing is closed or suspended. Changes are restored when you stop, time runs out, or QuietCare exits.";
    public bool CanStart=>!IsActive&&!Busy&&!NeedsRecovery;
    public bool CanConfigure=>!IsActive&&!Busy;
    public string SessionLabel=>IsActive?"IN SESSION":"READY TO FOCUS";
    public string SessionAction=>IsActive?"End focus session":"Start focus session";
    public double SessionProgress=>IsActive&&Minutes>0?Math.Clamp(100*(1-RemainingMinutes/Math.Max(1,Minutes)),0,100):0;
    [ObservableProperty,NotifyPropertyChangedFor(nameof(SessionProgress))] double _remainingMinutes;
    public string PrioritySummary=>IsActive?"Work app prioritized":"Applies during your session";
    partial void OnIsActiveChanged(bool value)
    {
        Active=value;hub.FocusActive=value;ActiveChanged?.Invoke(value);
        OnPropertyChanged(nameof(CanStart));OnPropertyChanged(nameof(CanConfigure));
        OnPropertyChanged(nameof(SessionLabel));OnPropertyChanged(nameof(SessionAction));OnPropertyChanged(nameof(PrioritySummary));OnPropertyChanged(nameof(SessionProgress));
    }
    partial void OnBusyChanged(bool value){OnPropertyChanged(nameof(CanStart));OnPropertyChanged(nameof(CanConfigure));}
    partial void OnNeedsRecoveryChanged(bool value)=>OnPropertyChanged(nameof(CanStart));
    public FocusViewModel(RunningTaskRegistry tasks,CareHubViewModel hub)
    {
        this.tasks=tasks;this.hub=hub;
        try
        {
            var saved=FocusControlEntry.Read<FocusOptions>(Path.Combine(FocusControlEntry.DirectoryPath,"options.json"));
            if(saved is not null){saved.Validate();Minutes=saved.Minutes;
                DurationUnitIndex=saved.Minutes==0?2:0;DurationAmount=saved.Minutes==0?60:saved.Minutes;
                if(Enum.IsDefined(saved.DurationUnit)&&saved.DurationAmount>0&&FocusDuration.ToMinutes(saved.DurationAmount,saved.DurationUnit)==saved.Minutes)
                {DurationUnitIndex=(int)saved.DurationUnit;DurationAmount=saved.DurationAmount;}
                PrioritizeWork=saved.PrioritizeWork;LowerBackground=saved.LowerBackground;QuietNotifications=saved.QuietNotifications;KeepAwake=saved.KeepAwake;GuardUpdates=saved.GuardUpdates;}
        }
        catch{ } // A damaged optional preference file cannot authorize any system change.
        timer.Tick+=async(_,_)=>await PollAsync();
    }
    public void StartWatching(bool recover=true){timer.Start();if(recover)_=RecoverUserAsync();else _=PollAsync();}
    async Task RecoverUserAsync()
    {
        try
        {
            var journal=FocusControlEntry.Read<FocusJournal>(FocusControlEntry.JournalPath);
            if(journal is not null&&!FocusControlEntry.Alive(journal.WorkerId,journal.WorkerStart))
            {
                var errors=await Sta(()=>new FocusSession(new WindowsFocusRuntime(FocusControlEntry.JournalPath),journal).Restore());
                if(errors.Count>0){NeedsRecovery=true;Detail=string.Join(" ",errors);}
            }
        }
        catch(Exception ex){NeedsRecovery=true;Detail="Focus recovery needs attention: "+ex.Message;}
        await PollAsync();
    }
    static Task<T> Sta<T>(Func<T> action)
    {
        var completion=new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread=new Thread(()=>{try{completion.SetResult(action());}catch(Exception ex){completion.SetException(ex);}}){IsBackground=true};
        thread.SetApartmentState(ApartmentState.STA);thread.Start();return completion.Task;
    }
    public async Task PollAsync()
    {
        if(polling)return;polling=true;
        try
        {
            var status=await Task.Run(()=>FocusControlEntry.Read<FocusStatus>(FocusControlEntry.StatusPath));
            bool active=status?.Active==true&&FocusControlEntry.Alive(status.WorkerId,status.WorkerStart);
            timer.Interval=TimeSpan.FromSeconds(active?3:15);
            if(status is not null)
            {
                IsActive=active;Headline=active?"Your focus session is active":"Ready for your next session";
                Countdown=active?FocusDuration.Remaining(status.Expires,DateTimeOffset.UtcNow):"Session ended";
                if(active&&status.Expires==DateTimeOffset.MaxValue){Minutes=0;DurationUnitIndex=2;}
                RemainingMinutes=status.Expires==DateTimeOffset.MaxValue?0:Math.Max(0,(status.Expires-DateTimeOffset.UtcNow).TotalMinutes);
                WorkApp=status.WorkApp;PriorityStatus=$"{status.Prioritized} work processes prioritized · {status.Background} background processes at lower priority";
                NotificationStatus=status.Notifications;AwakeStatus=status.Awake;UpdateStatus=status.Updates;Detail=status.Detail;
                if(status.Active&&!active)Detail="The Focus helper exited. Use Restore changes to check its recovery ledger.";
            }
            var backup=await Task.Run(WindowsFocusPolicyRuntime.Load);
            if(backup is not null&&(status is null||backup.Session==status.Session))UpdateStatus=backup.Status;
            var journal=await Task.Run(()=>FocusControlEntry.Read<FocusJournal>(FocusControlEntry.JournalPath));
            if(!active&&status is not null)
            {
                AwakeStatus="Focus is inactive. No Focus keep-awake request is running.";
                NotificationStatus=journal?.NotificationOwned==true?"Notification restore needs attention. Use Restore changes.":"Focus is inactive. No owned notification change remains.";
            }
            NeedsRecovery=!active&&(journal is not null&&(journal.Priorities.Count>0||journal.NotificationOwned)||backup is not null&&backup.Values.Count>0&&!FocusControlEntry.Alive(backup.OwnerId,backup.OwnerStart));
            if(!active)lastWork=await Task.Run(()=>new WindowsFocusRuntime(FocusControlEntry.JournalPath).Foreground())??lastWork;
        }
        catch(Exception ex){Detail="Focus status unavailable: "+ex.Message;}
        finally{polling=false;}
    }
    [RelayCommand] public async Task StartAsync()
    {
        if(!CanStart)return;
        if(!hub.CareControlsReady||tasks.Count>0){Detail="Wait for the current scan, cleanup or system task to finish before starting Focus mode.";return;}
        Busy=true;hub.FocusActive=true;
        try
        {
            Minutes=FocusDuration.ToMinutes(DurationAmount,(FocusDurationUnit)DurationUnitIndex);
            var options=new FocusOptions(Minutes,PrioritizeWork,LowerBackground,QuietNotifications,KeepAwake,GuardUpdates){DurationUnit=(FocusDurationUnit)DurationUnitIndex,DurationAmount=DurationAmount};
            options.Validate();
            WindowsFocusRuntime.WriteJson(Path.Combine(FocusControlEntry.DirectoryPath,"options.json"),options);
            using var own=Process.GetCurrentProcess();Guid id=Guid.NewGuid();int flags=(PrioritizeWork?1:0)|(LowerBackground?2:0)|(QuietNotifications?4:0)|(KeepAwake?8:0)|(GuardUpdates?16:0);
            var info=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true};
            foreach(string argument in new[]{"--focus-worker",id.ToString("N"),own.Id.ToString(),own.StartTime.ToUniversalTime().Ticks.ToString(),Minutes.ToString(),flags.ToString(),(lastWork?.Id??0).ToString()})info.ArgumentList.Add(argument);
            using var worker=Process.Start(info)??throw new InvalidOperationException("Windows did not start the Focus helper.");
            for(int i=0;i<30;i++)
            {
                await Task.Delay(200);var status=FocusControlEntry.Read<FocusStatus>(FocusControlEntry.StatusPath);
                if(status?.Session==id&&status.Active){await PollAsync();break;}
                if(worker.HasExited)throw new InvalidOperationException("The Focus helper could not start ("+worker.ExitCode+"). Restore any previous session changes before retrying.");
            }
        }
        catch(Exception ex){Detail="Focus did not start: "+ex.Message;}
        finally{Busy=false;if(!IsActive)hub.FocusActive=false;}
    }
    [RelayCommand] public async Task StopAsync()
    {
        if(Busy)return;Busy=true;
        try
        {
            var status=FocusControlEntry.Read<FocusStatus>(FocusControlEntry.StatusPath);
            if(status is not null&&FocusControlEntry.Alive(status.WorkerId,status.WorkerStart))
            {
                Directory.CreateDirectory(FocusControlEntry.DirectoryPath);File.WriteAllText(Path.Combine(FocusControlEntry.DirectoryPath,status.Session.ToString("N")+".stop"),"Stop requested by QuietCare");
                for(int i=0;i<35&&FocusControlEntry.Alive(status.WorkerId,status.WorkerStart);i++)await Task.Delay(200);
            }
            await Task.Delay(2200);await PollAsync();
        }
        catch(Exception ex){Detail="Focus stop needs attention: "+ex.Message;}
        finally{Busy=false;}
    }
    [RelayCommand] public async Task RestoreAsync()
    {
        if(Busy||IsActive)return;Busy=true;
        try
        {
            await RecoverUserAsync();
            var backup=WindowsFocusPolicyRuntime.Load();
            if(backup?.Values.Count>0&&!FocusControlEntry.Alive(backup.OwnerId,backup.OwnerStart))
            {
                using var child=Process.Start(new ProcessStartInfo(Environment.ProcessPath!,"--focus-recover-updates"){UseShellExecute=true,Verb="runas"});
                if(child is not null)await child.WaitForExitAsync();
            }
            await PollAsync();
        }
        catch(Exception ex){Detail="Restore needs attention: "+ex.Message;}
        finally{Busy=false;}
    }
    [RelayCommand] static void OpenWindowsNotifications()=>Process.Start(new ProcessStartInfo("ms-settings:notifications"){UseShellExecute=true});
    public void Dispose(){timer.Stop();Active=false;}
}
