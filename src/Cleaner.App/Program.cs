namespace Cleaner.App;
internal static class Program
{
    [System.STAThread]
    private static int Main(string[] args)
    {
        if(args.Length==2&&args[0]=="--focus-worker-update-check")return Services.FocusWorkerCheck.Run(args[1],true);
        if(args.Length==2&&args[0]=="--focus-worker-check")return Services.FocusWorkerCheck.Run(args[1]);
        if(args.Length==2&&args[0]=="--focus-native-check")return Services.FocusNativeCheck.Run(args[1]);
        if(args.Length>0&&(args[0]=="--focus-worker"||args[0]=="--focus-update-guard"||args[0]=="--focus-recover-updates"))return Services.FocusControlEntry.Execute(args);
        if(args.Length==2&&args[0]=="--focus-probe")
        {
            try
            {
                string notifications;try{notifications=new Cleaner.Core.Services.WindowsFocusRuntime(System.IO.Path.GetTempPath()+"unused-focus-probe.json").ReadNotifications();}catch(Exception ex){notifications=ex.Message;}
                using var policies=new Cleaner.Core.Services.WindowsFocusPolicyRuntime(true);
                var values=Cleaner.Core.Services.FocusUpdateGuard.Settings.Keys.ToDictionary(x=>x,x=>policies.Read(x));
                System.IO.File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(new{scope="Read-only Focus capability probe; no preferences, priorities or policies changed",notifications,updateGuardSupported=policies.Supported,values},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));return 0;
            }catch{return 1;}
        }
        if(args.Length>0&&args[0]=="--defender-action")return args.Length==2?Services.DefenderControlEntry.Execute(args[1]):64;
        if(args.Length==2&&args[0]=="--defender-probe")
        {
            try{System.IO.File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(new Cleaner.Core.Services.WindowsDefenderControlRuntime().Probe(),new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));return 0;}
            catch{return 1;}
        }
        var identity=System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value;
        string name="Local\\QuietCareNext."+identity;
        if(args.Contains("--validate-ui")||args.Contains("--monitor-smoke"))name+=".Validation."+Environment.ProcessId;
        using var mutex = new System.Threading.Mutex(true, name, out var first);
        if (!first)
        {
            if(args.Contains("--monitor"))try{using var existingMonitor=System.Threading.EventWaitHandle.OpenExisting(name+".Monitor");existingMonitor.Set();}catch(System.Threading.WaitHandleCannotBeOpenedException){}
            if(!args.Contains("--tray"))try{using var existing=System.Threading.EventWaitHandle.OpenExisting(name+".Show");existing.Set();}catch(System.Threading.WaitHandleCannotBeOpenedException){}
            return 0;
        }
        using var show=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,name+".Show");
        using var showMonitor=new System.Threading.EventWaitHandle(false,System.Threading.EventResetMode.AutoReset,name+".Monitor");
        var app = new App();
        if (!args.Contains("--tray") && !args.Contains("--monitor-smoke")) app.InitializeComponent();
        var registration=System.Threading.ThreadPool.RegisterWaitForSingleObject(show,(_,_)=>app.Dispatcher.BeginInvoke(app.RequestReveal),null,-1,false);
        var monitorRegistration=System.Threading.ThreadPool.RegisterWaitForSingleObject(showMonitor,(_,_)=>app.Dispatcher.BeginInvoke(()=>App.Monitor?.ShowMonitor()),null,-1,false);
        try{return app.Run();}finally{registration.Unregister(null);monitorRegistration.Unregister(null);}
    }
}
