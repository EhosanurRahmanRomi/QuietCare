using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("QuietCare Setup")]
[assembly: AssemblyVersion("0.3.0.0")]
namespace QuietCare {
internal static class SetupProgram {
#if INSTALLER_TEST
    static readonly string Target=Path.Combine(Environment.GetEnvironmentVariable("QUIETCARE_INSTALL_TEST"),"Install");
    const string UninstallKey=@"Software\QuietCare\InstallerFixture";
#else
    static readonly string Target=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","QuietCare");
    const string UninstallKey=@"Software\Microsoft\Windows\CurrentVersion\Uninstall\QuietCare";
#endif
    static string Executable {get{return Path.Combine(Target,"QuietCare.exe");}}
    static string StartFolder {get{
#if INSTALLER_TEST
        return Path.Combine(Environment.GetEnvironmentVariable("QUIETCARE_INSTALL_TEST"),"StartMenu");
#else
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"QuietCare");
#endif
    }}
    static string DesktopLink {get{
#if INSTALLER_TEST
        return Path.Combine(Environment.GetEnvironmentVariable("QUIETCARE_INSTALL_TEST"),"QuietCare.lnk");
#else
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"QuietCare.lnk");
#endif
    }}
    [STAThread] static void Main(string[] args) {
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        try {
            if(args.Length==2 && args[0]=="--finish-uninstall"){Finish(args[1]);return;}
            if(args.Contains("--uninstall")){Uninstall();return;}
            Application.Run(new SetupForm());
        }catch(Exception ex){MessageBox.Show(ex.Message,"QuietCare Setup",MessageBoxButtons.OK,MessageBoxIcon.Information);}
    }
    static bool Running(){return Process.GetProcessesByName("QuietCare").Any(p=>{using(p)try{return String.Equals(p.MainModule.FileName,Executable,StringComparison.OrdinalIgnoreCase);}catch{return true;}});}
    internal static void Install(bool desktop,bool launch) {
        if(Running())throw new IOException("Exit the installed QuietCare from its tray menu before updating it.");
        Directory.CreateDirectory(Target);
        using(var locks=SafePaths.LockParents(Path.Combine(Target,"probe"))) {
            string stage=Path.Combine(Target,Guid.NewGuid().ToString("N")+".stage");
            try {
                using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream("QuietCarePayload"))using(var output=new FileStream(stage,FileMode.CreateNew,FileAccess.Write,FileShare.None)){if(input==null)throw new IOException("Installer payload missing.");input.CopyTo(output);}
                using(var stream=File.OpenRead(stage))using(var sha=SHA256.Create()) {
                    string hash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");
                    if(hash!="PAYLOAD_HASH")throw new IOException("Installer payload integrity check failed.");
                }
                if(File.Exists(Executable))File.Replace(stage,Executable,null);else File.Move(stage,Executable);
                string setup=Path.Combine(Target,"QuietCare-Setup.exe");
                string self=Assembly.GetExecutingAssembly().Location;if(!String.Equals(self,setup,StringComparison.OrdinalIgnoreCase))File.Copy(self,setup,true);
                File.WriteAllText(Path.Combine(Target,"installed.txt"),"QuietCare 0.3.0\r\nPer-user installation. Recovery and settings are stored separately and retained on uninstall.");
            }finally{if(File.Exists(stage))File.Delete(stage);}
        }
        Directory.CreateDirectory(StartFolder);Link(Path.Combine(StartFolder,"QuietCare.lnk"),Executable,"");Link(Path.Combine(StartFolder,"Uninstall QuietCare.lnk"),Path.Combine(Target,"QuietCare-Setup.exe"),"--uninstall");if(desktop)Link(DesktopLink,Executable,"");
        using(var key=Registry.CurrentUser.CreateSubKey(UninstallKey)) {
            key.SetValue("DisplayName","QuietCare");key.SetValue("DisplayVersion","0.3.0");key.SetValue("Publisher","QuietCare (local preview)");key.SetValue("InstallLocation",Target);
            key.SetValue("UninstallString","\""+Path.Combine(Target,"QuietCare-Setup.exe")+"\" --uninstall");key.SetValue("DisplayIcon",Executable);key.SetValue("NoModify",1);key.SetValue("NoRepair",1);key.SetValue("EstimatedSize",(int)(new FileInfo(Executable).Length/1024+500));
        }
        if(launch)Process.Start(new ProcessStartInfo(Executable){UseShellExecute=true});
    }
    static void Link(string path,string target,string args) {
        using(var locks=SafePaths.LockParents(path)) {
            Type type=Type.GetTypeFromProgID("WScript.Shell");object shell=Activator.CreateInstance(type),shortcut=null;
            try {bool existed=File.Exists(path);shortcut=type.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});var t=shortcut.GetType();if(existed){string previous=Convert.ToString(t.InvokeMember("TargetPath",BindingFlags.GetProperty,null,shortcut,null));if(!String.Equals(previous,target,StringComparison.OrdinalIgnoreCase))return;}t.InvokeMember("TargetPath",BindingFlags.SetProperty,null,shortcut,new object[]{target});t.InvokeMember("Arguments",BindingFlags.SetProperty,null,shortcut,new object[]{args});t.InvokeMember("WorkingDirectory",BindingFlags.SetProperty,null,shortcut,new object[]{Target});t.InvokeMember("Save",BindingFlags.InvokeMethod,null,shortcut,null);}finally{if(shortcut!=null)Marshal.FinalReleaseComObject(shortcut);Marshal.FinalReleaseComObject(shell);}
        }
    }
    static void RemoveLink(string path) {
        if(!File.Exists(path))return;
        using(var locks=SafePaths.LockParents(path)) {
            Type type=Type.GetTypeFromProgID("WScript.Shell");object shell=Activator.CreateInstance(type),shortcut=null;
            try {shortcut=type.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});string target=Convert.ToString(shortcut.GetType().InvokeMember("TargetPath",BindingFlags.GetProperty,null,shortcut,null));if(String.Equals(target,Executable,StringComparison.OrdinalIgnoreCase)||String.Equals(target,Path.Combine(Target,"QuietCare-Setup.exe"),StringComparison.OrdinalIgnoreCase))File.Delete(path);}finally{if(shortcut!=null)Marshal.FinalReleaseComObject(shortcut);Marshal.FinalReleaseComObject(shell);}
        }
    }
    static void Uninstall() {
        if(!File.Exists(Path.Combine(Target,"installed.txt")))throw new IOException("QuietCare installation marker was not found.");
        if(MessageBox.Show("Uninstall QuietCare for this Windows user?\n\nSettings, recovery files, and startup backups will be kept. Restore any startup entries you want before uninstalling.","Uninstall QuietCare",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)!=DialogResult.OK)return;
        if(Running())throw new IOException("Exit QuietCare from its tray menu, then run Uninstall again.");
        // The helper receives only a parent PID. Its deletion targets are fixed to our per-user install folder.
        string helper=Path.Combine(Path.GetTempPath(),"QuietCare-uninstall-"+Guid.NewGuid().ToString("N")+".exe");
        using(var locks=SafePaths.LockParents(helper))File.Copy(Assembly.GetExecutingAssembly().Location,helper,false);
        Process.Start(new ProcessStartInfo(helper,"--finish-uninstall "+Process.GetCurrentProcess().Id){UseShellExecute=false,CreateNoWindow=true});
    }
    internal static void Finish(string parentId) {
        int pid;if(!Int32.TryParse(parentId,out pid))throw new IOException("Invalid uninstall operation.");
        try {using(var parent=Process.GetProcessById(pid)){if(!String.Equals(parent.MainModule.FileName,Path.Combine(Target,"QuietCare-Setup.exe"),StringComparison.OrdinalIgnoreCase) && !Path.GetFileName(parent.MainModule.FileName).StartsWith("QuietCare-Setup",StringComparison.OrdinalIgnoreCase))throw new IOException("Invalid uninstall parent.");if(!parent.WaitForExit(10000))throw new IOException("The installer is still running. Uninstall was cancelled before removing files.");}}catch(ArgumentException){}
        if(Running())throw new IOException("QuietCare is still running. Uninstall was cancelled.");
        using(var locks=SafePaths.LockParents(Path.Combine(Target,"probe"))) {
            if(!File.Exists(Path.Combine(Target,"installed.txt")))throw new IOException("Installation marker missing.");
            foreach(string file in new[]{"QuietCare.exe","QuietCare-Setup.exe","installed.txt"})File.Delete(Path.Combine(Target,file));
        }
        RemoveLink(DesktopLink);RemoveLink(Path.Combine(StartFolder,"QuietCare.lnk"));RemoveLink(Path.Combine(StartFolder,"Uninstall QuietCare.lnk"));
        if(Directory.Exists(StartFolder)&&!Directory.EnumerateFileSystemEntries(StartFolder).Any())Directory.Delete(StartFolder);
        if(Directory.Exists(Target)&&!Directory.EnumerateFileSystemEntries(Target).Any())Directory.Delete(Target);
        using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run",true))if(key!=null){string value=key.GetValue("QuietCare") as string;if(value!=null&&value.StartsWith("\""+Executable+"\"",StringComparison.OrdinalIgnoreCase))key.DeleteValue("QuietCare",false);}
        Registry.CurrentUser.DeleteSubKey(UninstallKey,false);
#if !INSTALLER_TEST
        MessageBox.Show("QuietCare has been uninstalled. Recovery files and startup backups were kept in LocalAppData\\QuietCare.\n\nThe small temporary uninstall helper is retained in Temp until you remove it.","QuietCare",MessageBoxButtons.OK,MessageBoxIcon.Information);
#endif
    }
}
internal sealed class SetupForm : Form {
    readonly CheckBox desktop=new CheckBox(),launch=new CheckBox();readonly Button install=new Button();readonly Label status=new Label();
    public SetupForm() {
        Text="QuietCare Setup";ClientSize=new Size(540,420);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(9,11,35);ForeColor=Color.White;Font=new Font("Segoe UI",10);Icon=Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
        Controls.Add(new Label{Text="QuietCare",Font=new Font("Segoe UI",29,FontStyle.Bold),ForeColor=Color.FromArgb(126,200,255),Location=new Point(32,26),AutoSize=true});
        Controls.Add(new Label{Text="Your PC. Without the noise.",Location=new Point(35,86),AutoSize=true});
        Controls.Add(new Label{Text="Tiny transparent CPU / GPU / RAM monitor\nOne-click Smart RAM Boost and cleanup with recovery\nNo ads, accounts, telemetry, drivers, or background service",Location=new Point(35,131),Size=new Size(475,85),ForeColor=Color.FromArgb(175,188,219)});
        desktop.Text="Create a desktop shortcut";desktop.Location=new Point(35,229);desktop.Size=new Size(420,25);desktop.Checked=true;Controls.Add(desktop);
        launch.Text="Open QuietCare after installation";launch.Location=new Point(35,260);launch.Size=new Size(420,25);launch.Checked=true;Controls.Add(launch);
        status.Text="Installs only for your Windows user. No administrator access.\nPreview build 0.3.0 • Windows 10/11 x64 • .NET Framework 4.8";status.Location=new Point(35,303);status.Size=new Size(470,48);status.ForeColor=Color.FromArgb(155,170,205);status.Font=new Font("Segoe UI",9);Controls.Add(status);
        install.Text="Install QuietCare";install.Location=new Point(315,365);install.Size=new Size(190,36);install.BackColor=Color.FromArgb(126,200,255);install.ForeColor=Color.FromArgb(9,11,35);install.FlatStyle=FlatStyle.Flat;install.Click+=(s,e)=>{install.Enabled=false;try{SetupProgram.Install(desktop.Checked,launch.Checked);MessageBox.Show("QuietCare installed. Open it from Start or your desktop.\n\nUse its notification-area menu to exit completely.","QuietCare",MessageBoxButtons.OK,MessageBoxIcon.Information);Close();}catch(Exception ex){status.Text=ex.Message;install.Enabled=true;}};Controls.Add(install);
    }
}
}
