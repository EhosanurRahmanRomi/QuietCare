using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using QuietCare;

internal static class InstallerTests {
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static void SetShortcut(string path,string target){
        object shell=null,link=null;
        try{shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));link=shell.GetType().InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});link.GetType().InvokeMember("TargetPath",BindingFlags.SetProperty,null,link,new object[]{target});link.GetType().InvokeMember("Save",BindingFlags.InvokeMethod,null,link,null);}
        finally{if(link!=null)Marshal.FinalReleaseComObject(link);if(shell!=null)Marshal.FinalReleaseComObject(shell);}
    }
    static string ShortcutTarget(string path){
        object shell=null,link=null;
        try{shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));link=shell.GetType().InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});return (string)link.GetType().InvokeMember("TargetPath",BindingFlags.GetProperty,null,link,null);}
        finally{if(link!=null)Marshal.FinalReleaseComObject(link);if(shell!=null)Marshal.FinalReleaseComObject(shell);}
    }
    [STAThread]static int Main() {
        string root=Environment.GetEnvironmentVariable("QUIETCARE_INSTALL_TEST");Directory.CreateDirectory(root);
        try {
            SetupProgram.Install(true,false);string app=Path.Combine(root,"Install","QuietCare.exe");
            Check(File.Exists(app),"Installed executable missing.");Check(File.Exists(Path.Combine(root,"QuietCare.lnk")),"Desktop shortcut missing.");Check(File.Exists(Path.Combine(root,"StartMenu","QuietCare.lnk")),"Start menu shortcut missing.");
            using(var input=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("QuietCarePayload"))using(var file=File.OpenRead(app))using(var sha=SHA256.Create())Check(sha.ComputeHash(input).SequenceEqual(sha.ComputeHash(file)),"Installed payload differs.");
            using(var k=Registry.CurrentUser.OpenSubKey(@"Software\QuietCare\InstallerFixture"))Check(k!=null&&(string)k.GetValue("DisplayName")=="QuietCare","Uninstall entry missing.");
            Console.WriteLine("PASS Isolated install: payload SHA-256, desktop and Start shortcuts, registration");
            string desktop=Path.Combine(root,"QuietCare.lnk");string unrelated=Path.Combine(root,"Unrelated.exe");
            SetShortcut(desktop,unrelated);SetupProgram.Install(true,false);Console.WriteLine("PASS Reinstall replaces only the product files");
            Check(ShortcutTarget(desktop)==unrelated,"Existing unrelated shortcut overwritten");Console.WriteLine("PASS Existing unrelated desktop shortcut is preserved");
            File.WriteAllText(Path.Combine(root,"Install","keep.me"),"unowned file");File.WriteAllText(Path.Combine(root,"recovery-must-remain.txt"),"recovery fixture");
            SetupProgram.Finish(Int32.MaxValue.ToString());
            Check(!File.Exists(app),"Executable remains.");Check(ShortcutTarget(Path.Combine(root,"QuietCare.lnk"))==unrelated,"Unrelated shortcut changed during uninstall.");Check(!Directory.Exists(Path.Combine(root,"StartMenu")),"Start menu group remains.");
            Check(File.ReadAllText(Path.Combine(root,"Install","keep.me"))=="unowned file","Unowned file removed.");Check(File.Exists(Path.Combine(root,"recovery-must-remain.txt")),"Recovery data removed.");
            using(var k=Registry.CurrentUser.OpenSubKey(@"Software\QuietCare\InstallerFixture"))Check(k==null,"Registration remains.");
            Console.WriteLine("PASS Isolated uninstall removes product files and registration, preserves other data");return 0;
        }catch(Exception ex){Console.WriteLine("FAIL "+ex);return 1;}finally{Registry.CurrentUser.DeleteSubKey(@"Software\QuietCare\InstallerFixture",false);}
    }
}
