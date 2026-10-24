namespace Cleaner.App;
internal static class Program
{
    [System.STAThread]
    private static int Main(string[] args)
    {
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
