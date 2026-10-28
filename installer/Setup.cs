using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: AssemblyTitle("QuietCare Setup")]
[assembly: AssemblyProduct("QuietCare")]
[assembly: AssemblyCompany("QuietCare contributors")]
[assembly: AssemblyVersion("0.8.1.1")]
[assembly: AssemblyFileVersion("0.8.1.1")]

namespace QuietCare.Installer
{
    internal static class Program
    {
        [STAThread] static void Main()
        {
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupWindow());
        }
        internal static bool Running()
        {
            string name="Local\\QuietCareNext."+WindowsIdentity.GetCurrent().User.Value;
            try{using(Mutex.OpenExisting(name))return true;}
            catch(WaitHandleCannotBeOpenedException){return false;}
        }
        internal static string InstalledExe {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","QuietCareNext","QuietCare.Next.exe");}}
        internal static int Install(bool startup,bool desktop)
        {
            if(Running())throw new IOException("Exit QuietCare from its tray menu before installing this update. Your work will stay open.");
            string staging=Path.Combine(Path.GetTempPath(),"QuietCareSetup-"+Guid.NewGuid().ToString("N"));
            var security=new DirectorySecurity();security.SetAccessRuleProtection(true,false);
            security.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User,FileSystemRights.FullControl,InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Allow));
            security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid,null),FileSystemRights.FullControl,InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Allow));
            Directory.CreateDirectory(staging,security);string package=Path.Combine(staging,"QuietCare.msi");
            try
            {
                using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream("QuietCarePackage"))
                using(var output=new FileStream(package,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {if(input==null)throw new IOException("The installation package is missing.");input.CopyTo(output);output.Flush(true);}
                using(var stream=File.OpenRead(package))using(var sha=SHA256.Create())
                {if(BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","")!="PACKAGE_SHA256")throw new IOException("Installer integrity check failed. Download the installer again.");}
                string logDirectory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QuietCareNext","Setup");Directory.CreateDirectory(logDirectory);
                string log=Path.Combine(staging,"install.log");
                string savedLog=Path.Combine(logDirectory,"install-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".log");
                // Fixed native engine, local verified package and two boolean properties; no arbitrary command runner.
                var start=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"msiexec.exe"),
                    "/i \""+package+"\" /qn /norestart STARTUP="+(startup?"1":"0")+" DESKTOP="+(desktop?"1":"0")+" /L*v \""+log+"\"")
                    {UseShellExecute=false,CreateNoWindow=true};
                using(var process=Process.Start(start))
                {
                    process.WaitForExit();int code=process.ExitCode;
                    try{if(File.Exists(log))File.Copy(log,savedLog,true);}catch(IOException){}catch(UnauthorizedAccessException){}
                    if(code!=0&&code!=3010)throw new IOException("Windows Installer returned "+code+". Setup log: "+(File.Exists(savedLog)?savedLog:log));
                    return code;
                }
            }
            finally
            {
                // Only this invocation's exact package and empty staging directory are removed.
                try{File.Delete(package);File.Delete(Path.Combine(staging,"install.log"));Directory.Delete(staging,false);}catch(IOException){}catch(UnauthorizedAccessException){}
            }
        }
    }

    internal sealed class SetupWindow:Form
    {
        readonly Button install=new Button(),cancel=new Button();
        readonly CheckBox startup=new CheckBox(),desktop=new CheckBox(),launch=new CheckBox();
        readonly Label status=new Label();readonly ProgressBar progress=new ProgressBar();bool busy,done;
        public SetupWindow()
        {
            Text="QuietCare Setup";ClientSize=new Size(620,510);MinimumSize=Size;MaximumSize=Size;
            FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;
            BackColor=Color.White;ForeColor=Color.FromArgb(23,34,53);Font=new Font("Segoe UI",10);AutoScaleMode=AutoScaleMode.Dpi;
            Icon=Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            var logo=new PictureBox{Location=new Point(30,28),Size=new Size(54,54),SizeMode=PictureBoxSizeMode.Zoom,Image=Icon.ToBitmap()};Controls.Add(logo);
            Controls.Add(new Label{Text="Install QuietCare",Font=new Font("Segoe UI",23,FontStyle.Bold),Location=new Point(102,25),Size=new Size(470,43)});
            Controls.Add(new Label{Text="PC care, ready when you need it.",ForeColor=Color.FromArgb(99,114,137),Location=new Point(104,72),AutoSize=true});
            Controls.Add(new Label{Text="One-click care   •   Memory Boost   •   Windows protection\nStorage tools   •   Startup watch   •   Tiny performance monitor",Location=new Point(32,124),Size=new Size(556,62),ForeColor=Color.FromArgb(65,84,110)});
            startup.Text="Start the mini monitor when I sign in to Windows";startup.Checked=true;startup.SetBounds(32,207,550,28);Controls.Add(startup);
            desktop.Text="Create a desktop shortcut";desktop.Checked=true;desktop.SetBounds(32,245,550,28);Controls.Add(desktop);
            launch.Text="Open QuietCare after installation";launch.Checked=true;launch.SetBounds(32,283,550,28);Controls.Add(launch);
            status.Text="Installs for your Windows account. No separate .NET download.\nWindows Settings will list QuietCare with its icon and uninstaller.\nSettings, recovery files and startup backups are kept on uninstall.";
            status.SetBounds(32,335,556,72);status.ForeColor=Color.FromArgb(99,114,137);status.Font=new Font("Segoe UI",9);Controls.Add(status);
            progress.SetBounds(32,421,556,4);progress.Style=ProgressBarStyle.Marquee;progress.Visible=false;Controls.Add(progress);
            install.Text="Install QuietCare";install.SetBounds(400,451,188,37);install.FlatStyle=FlatStyle.Flat;install.BackColor=Color.FromArgb(23,105,210);install.ForeColor=Color.White;install.FlatAppearance.BorderSize=0;install.Click+=Install;Controls.Add(install);
            cancel.Text="Cancel";cancel.SetBounds(290,451,98,37);cancel.Click+=(_,__)=>Close();Controls.Add(cancel);CancelButton=cancel;
            FormClosing+=(_,e)=>{if(busy)e.Cancel=true;};
        }
        async void Install(object sender,EventArgs args)
        {
            if(done){Close();return;}
            busy=true;install.Enabled=false;cancel.Enabled=false;startup.Enabled=false;desktop.Enabled=false;launch.Enabled=false;progress.Visible=true;
            status.Text="Windows Installer is installing files, shortcuts and startup registration.\nPlease wait for its result. Your other apps will remain open.";
            bool atStartup=startup.Checked,onDesktop=desktop.Checked,open=launch.Checked;
            try
            {
                int result=await Task.Run(()=>Program.Install(atStartup,onDesktop));
                status.Text="QuietCare is installed. Open it from Start or your desktop.\n"+(atStartup?"The tiny monitor will start at your next Windows sign-in.\n":"Startup is off. You can enable it in monitor settings.\n")+(result==3010?"Windows reported that a restart is needed.":"No restart is needed to open it now.");
                done=true;install.Text="Finish";cancel.Visible=false;
                if(open)Process.Start(new ProcessStartInfo(Program.InstalledExe){UseShellExecute=true});
            }
            catch(Exception ex){status.Text=ex.Message;startup.Enabled=true;desktop.Enabled=true;launch.Enabled=true;cancel.Enabled=true;}
            finally{busy=false;install.Enabled=true;progress.Visible=false;}
        }
    }
}
