using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;

[assembly: AssemblyTitle("QuietCare")]
[assembly: AssemblyDescription("Local, ad-free Windows monitoring and maintenance")]
[assembly: AssemblyCompany("QuietCare")]
[assembly: AssemblyProduct("QuietCare")]
[assembly: AssemblyVersion("0.3.0.0")]
[assembly: AssemblyFileVersion("0.3.0.0")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.8")]
namespace QuietCare {
internal static class Program {
    [STAThread] static void Main(string[] args) {
        AppContext.SetSwitch("Switch.System.IO.UseLegacyPathHandling",false);AppContext.SetSwitch("Switch.System.IO.BlockLongPaths",false);
        string identity=System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;bool created;
        using(var mutex=new Mutex(true,@"Local\QuietCare-"+identity,out created))using(var signal=new EventWaitHandle(false,EventResetMode.AutoReset,@"Local\QuietCare-show-"+identity)) {
            if(!created){signal.Set();return;}
            var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Theme.Install(app);
            app.DispatcherUnhandledException+=(s,e)=>{State.Log("UI error: "+e.Exception.GetType().Name);MessageBox.Show(e.Exception.Message,"QuietCare — action could not finish",MessageBoxButton.OK,MessageBoxImage.Information);e.Handled=true;};
            AppDomain.CurrentDomain.UnhandledException+=(s,e)=>State.Log("Unhandled application error.");
            bool tray=args.Contains("--tray");var window=new MainWindow(Settings.Load(),tray);app.MainWindow=window;
            var registration=ThreadPool.RegisterWaitForSingleObject(signal,(state,timedOut)=>app.Dispatcher.BeginInvoke(new Action(()=>window.Reveal())),null,Timeout.Infinite,false);
            try{if(tray){window.StartInTray();app.Run();}else app.Run(window);}finally{registration.Unregister(null);}
        }
    }
}
}
