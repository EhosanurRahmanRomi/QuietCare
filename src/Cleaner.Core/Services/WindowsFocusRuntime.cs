using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace Cleaner.Core.Services;

public sealed class WindowsFocusRuntime(string journalPath) : IFocusRuntime
{
    static readonly int OwnSession=Process.GetCurrentProcess().SessionId;
    static readonly string OwnSid=WindowsIdentity.GetCurrent().User!.Value;
    static readonly string WindowsPath=Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Windows))+Path.DirectorySeparatorChar;
    static readonly HashSet<string> Protected=new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer","SystemSettings","Taskmgr","ApplicationFrameHost","SearchHost","StartMenuExperienceHost",
        "MsMpEng","MpDefenderCoreService","NisSrv","SecurityHealthService","SecurityHealthSystray","Sense",
        "MsSense","MsSenseS","smartscreen","dwm","audiodg","fontdrvhost","RuntimeBroker","ctfmon",
        "csrss","lsass","winlogon","wininit","services","svchost","sihost","ShellExperienceHost",
        "avp","avpui","avgui","avgsvc","avastui","avastsvc","bdagent","bdservicehost","ekrn","egui",
        "ProtonVPN","ProtonVPNService","mbamservice","mbamtray","mcshield","mfemms","nortonsecurity","sophoshealth","sophosui"
    };
    static readonly HashSet<string> BackgroundProtected=new(StringComparer.OrdinalIgnoreCase)
    {
        "devenv","MSBuild","dotnet","node","python","pythonw","git","Code","powershell","pwsh",
        "cmd","WindowsTerminal","conhost","wsl","wslhost","vmwp","vmware-vmx","VirtualBoxVM",
        "obs64","obs32","Teams","ms-teams","Zoom","WebexHost",
    };
    public static bool CanLower(string name)=>!BackgroundProtected.Contains(name);
    public static bool EligibleImage(string image,string name)
    {
        if(string.IsNullOrWhiteSpace(image)||!Path.IsPathFullyQualified(image)||Protected.Contains(name)||name.StartsWith("QuietCare",StringComparison.OrdinalIgnoreCase))return false;
        string full=Path.GetFullPath(image);
        return !full.StartsWith(WindowsPath,StringComparison.OrdinalIgnoreCase)&&!full.Contains(@"\Windows Defender\",StringComparison.OrdinalIgnoreCase)&&!full.Contains(@"\Windows Defender Advanced Threat Protection\",StringComparison.OrdinalIgnoreCase);
    }
    public FocusProcess? ReadProcess(int id)
    {
        try
        {
            using var process=Process.GetProcessById(id);
            if(process.SessionId!=OwnSession||process.HasExited)return null;
            using var handle=OpenProcess(0x1000,false,id);
            if(handle.IsInvalid||!OpenProcessToken(handle,8,out var token))return null;
            using(token)using(var identity=new WindowsIdentity(token.DangerousGetHandle()))
                if(identity.User?.Value!=OwnSid)return null;
            var path=new System.Text.StringBuilder(32768);int length=path.Capacity;
            if(!QueryFullProcessImageName(handle,0,path,ref length))return null;
            string image=path.ToString(),name=process.ProcessName;
            if(!EligibleImage(image,name))return null;
            return new(id,process.StartTime.ToUniversalTime().Ticks,image,name,process.PriorityClass);
        }
        catch{return null;}
    }
    public void SetPriority(FocusProcess process,ProcessPriorityClass priority)
    {
        if(!FocusSession.SafePriority(priority))throw new InvalidOperationException("Focus mode only uses Normal, Above normal and Below normal priorities.");
        // Retain a process handle across the identity check and mutation to prevent PID reuse.
        using var target=Process.GetProcessById(process.Id);
        _=target.Handle;
        var current=ReadProcess(process.Id);
        if(current is null||!FocusSession.SameProcess(current,process)||target.StartTime.ToUniversalTime().Ticks!=process.StartTicks)throw new InvalidOperationException("The selected process changed or is protected.");
        target.PriorityClass=priority;
    }
    public FocusProcess? Foreground()
    {
        _=GetWindowThreadProcessId(GetForegroundWindow(),out uint id);
        return id<=int.MaxValue?ReadProcess((int)id):null;
    }
    public List<FocusProcess> Snapshot(bool windowsOnly=false)
    {
        var list=new List<FocusProcess>();
        foreach(var process in Process.GetProcesses())using(process)
        {
            try
            {
                if(windowsOnly&&process.MainWindowHandle==IntPtr.Zero)continue;
                var item=ReadProcess(process.Id);if(item is not null)list.Add(item);
            }
            catch{ }
        }
        return list;
    }
    public string ReadNotifications(){using var bridge=new QuietHours();return bridge.Read();}
    public void SetNotifications(string profile)
    {
        if(!FocusSession.KnownProfile(profile))throw new ArgumentOutOfRangeException(nameof(profile));
        using var bridge=new QuietHours();
        for(int attempt=0;attempt<4;attempt++)
        {
            bridge.Write(profile);Thread.Sleep(600);
            if(bridge.Read()==profile)return;
        }
        throw new InvalidOperationException("Windows did not confirm the requested notification profile. The recovery ledger is retained.");
    }
    public void Save(FocusJournal journal)=>WriteJson(journalPath,journal);
    public static void WriteJson<T>(string path,T value)
    {
        string directory=Path.GetDirectoryName(path)!;Directory.CreateDirectory(directory);
        if((File.GetAttributes(directory)&FileAttributes.ReparsePoint)!=0||File.Exists(path)&&(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new IOException("A linked Focus state path was refused.");
        string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllText(temp,JsonSerializer.Serialize(value));File.Move(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public static IDisposable Awake()=>new AwakeRequest();
    sealed class AwakeRequest : IDisposable
    {
        readonly IntPtr handle;
        public AwakeRequest()
        {
            var context=new ReasonContext{Version=0,Flags=1,Reason="QuietCare timed Focus session"};
            handle=PowerCreateRequest(ref context);
            if(handle==IntPtr.Zero||handle==new IntPtr(-1))throw new Win32Exception(Marshal.GetLastWin32Error());
            if(!PowerSetRequest(handle,0)) // PowerRequestDisplayRequired; SystemRequired also keeps work awake.
            {CloseHandle(handle);throw new Win32Exception(Marshal.GetLastWin32Error());}
            if(!PowerSetRequest(handle,1)){PowerClearRequest(handle,0);CloseHandle(handle);throw new Win32Exception(Marshal.GetLastWin32Error());}
        }
        public void Dispose(){PowerClearRequest(handle,1);PowerClearRequest(handle,0);CloseHandle(handle);}
    }
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct ReasonContext{public uint Version;public uint Flags;[MarshalAs(UnmanagedType.LPWStr)]public string Reason;public ulong Padding1;public ulong Padding2;}
    [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr PowerCreateRequest(ref ReasonContext context);
    [DllImport("kernel32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]static extern bool PowerSetRequest(IntPtr handle,int type);
    [DllImport("kernel32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]static extern bool PowerClearRequest(IntPtr handle,int type);
    [DllImport("kernel32.dll")][return:MarshalAs(UnmanagedType.Bool)]static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll",SetLastError=true)]static extern SafeProcessHandle OpenProcess(uint access,[MarshalAs(UnmanagedType.Bool)]bool inherit,int id);
    [DllImport("advapi32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]static extern bool OpenProcessToken(SafeProcessHandle handle,uint access,out SafeAccessTokenHandle token);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]static extern bool QueryFullProcessImageName(SafeProcessHandle handle,uint flags,System.Text.StringBuilder value,ref int size);
    [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint id);

    // Optional compatibility bridge based on Rafael Rivera's MIT QuietHours IDL.
    // This Windows interface is undocumented: failures are reported, never replaced with
    // CloudStore binary edits or notification-service restarts.
    sealed class QuietHours : IDisposable
    {
        readonly IQuietHoursSettings settings;
        public QuietHours()=>settings=(IQuietHoursSettings)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("F53321FA-34F8-4B7F-B9A3-361877CB94CF"),true)!)!;
        public string Read(){settings.GetProfile(out string value);return value;}
        public void Write(string value)=>settings.SetProfile(value);
        public void Dispose()=>Marshal.FinalReleaseComObject(settings);
    }
    [ComImport,Guid("6BFF4732-81EC-4FFB-AE67-B6C1BC29631F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IQuietHoursSettings
    {
        void GetProfile([MarshalAs(UnmanagedType.LPWStr)]out string profile);
        void SetProfile([MarshalAs(UnmanagedType.LPWStr)]string profile);
    }
}
