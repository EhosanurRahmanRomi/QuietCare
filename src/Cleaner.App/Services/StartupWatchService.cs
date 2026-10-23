using Cleaner.Core.Services;
namespace Cleaner.App.Services;

public sealed class StartupWatchService : IDisposable
{
    readonly AppDataService store;
    readonly AutostartScanner scanner=new();
    readonly CancellationTokenSource stop=new();
    StartupBaseline baseline;
    readonly List<StartupChange> history;
    bool started;
    public event Action<IReadOnlyList<StartupChange>>? Changed;
    public string Status {get;private set;}="Startup monitoring is starting…";
    public IReadOnlyList<StartupChange> Recent=>history.AsReadOnly();
    public StartupWatchService(AppDataService store)
    {
        this.store=store;baseline=store.Load("startup-baseline.json",()=>new StartupBaseline());history=store.Load("startup-alerts.json",()=>new List<StartupChange>());
    }
    public void Start(){if(started)return;started=true;_ = Loop();}
    async Task Loop()
    {
        int cycle=0;
        while(!stop.IsCancellationRequested)
        {
            try
            {
                bool extended=cycle++%10==0;
                var entries=extended?await scanner.ScanAsync(stop.Token):await scanner.ScanFastAsync(stop.Token);
                var sources=Enum.GetValues<AutostartSource>().Where(x=>extended||x is not AutostartSource.ScheduledTask and not AutostartSource.Service).ToHashSet();
                var changes=StartupChangeDetector.Update(baseline,entries.Select(StartupSnapshot.From),sources);
                store.Save("startup-baseline.json",baseline);
                Status="Watching startup entries. Registry/folder checks every 30 seconds; tasks/services every 5 minutes.";
                if(changes.Count>0)
                {
                    history.InsertRange(0,changes);if(history.Count>200)history.RemoveRange(200,history.Count-200);
                    store.Save("startup-alerts.json",history);Changed?.Invoke(changes);
                }
            }
            catch(OperationCanceledException){break;}
            catch(Exception ex){Status="Startup monitoring could not refresh: "+ex.Message;App.LogException("Startup watch",ex);}
            try{await Task.Delay(TimeSpan.FromSeconds(30),stop.Token);}catch(OperationCanceledException){break;}
        }
    }
    public void Dispose()=>stop.Cancel();
}
