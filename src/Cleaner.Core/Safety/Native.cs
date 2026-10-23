#nullable disable
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace QuietCareSafety {
internal static class Native {
    [StructLayout(LayoutKind.Sequential)] internal struct MemoryInfo {
        public uint Length, Load; public ulong Total, Available, PageTotal, PageAvailable, VirtualTotal, VirtualAvailable, Extended;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct FileTime { public uint Low, High; public ulong Value { get { return ((ulong)High << 32) | Low; } } }
    [StructLayout(LayoutKind.Sequential)] internal struct CounterValue { public uint Status; public double Value; }
    [StructLayout(LayoutKind.Sequential)] internal struct CounterItem { public IntPtr Name; public CounterValue Value; }
    [StructLayout(LayoutKind.Sequential)] internal struct FileInfo {
        public uint Attributes; public FileTime Created, Accessed, Written; public uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
        public ulong Size { get { return ((ulong)SizeHigh << 32) | SizeLow; } }
        public string Identity { get { return Volume + ":" + IndexHigh + ":" + IndexLow; } }
    }
    [DllImport("kernel32.dll")] internal static extern bool GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool GlobalMemoryStatusEx(ref MemoryInfo info);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern SafeProcessHandle OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool GetProcessTimes(SafeProcessHandle handle, out FileTime created, out FileTime exit, out FileTime kernel, out FileTime user);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] internal static extern bool QueryFullProcessImageName(SafeProcessHandle handle, uint flags, StringBuilder path, ref int size);
    [DllImport("psapi.dll", SetLastError=true)] internal static extern bool EmptyWorkingSet(SafeProcessHandle process);
    [DllImport("pdh.dll", CharSet=CharSet.Unicode)] internal static extern uint PdhOpenQuery(string source, IntPtr data, out IntPtr query);
    [DllImport("pdh.dll", CharSet=CharSet.Unicode)] internal static extern uint PdhAddEnglishCounter(IntPtr query, string path, IntPtr data, out IntPtr counter);
    [DllImport("pdh.dll")] internal static extern uint PdhCollectQueryData(IntPtr query);
    [DllImport("pdh.dll")] internal static extern uint PdhGetFormattedCounterValue(IntPtr counter, uint format, out uint type, out CounterValue value);
    [DllImport("pdh.dll", CharSet=CharSet.Unicode)] internal static extern uint PdhGetFormattedCounterArray(IntPtr counter, uint format, ref uint bytes, ref uint count, IntPtr buffer);
    [DllImport("pdh.dll")] internal static extern uint PdhCloseQuery(IntPtr query);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] internal static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool GetFileInformationByHandle(SafeFileHandle file, out FileInfo info);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] internal static extern uint GetFinalPathNameByHandle(SafeFileHandle file, StringBuilder name, uint length, uint flags);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool SetFileInformationByHandle(SafeFileHandle file, int kind, IntPtr info, uint size);
    [DllImport("powrprof.dll")] internal static extern uint PowerGetActiveScheme(IntPtr root, out IntPtr scheme);
    [DllImport("powrprof.dll")] internal static extern uint PowerSetActiveScheme(IntPtr root, ref Guid scheme);
    [DllImport("kernel32.dll")] internal static extern IntPtr LocalFree(IntPtr ptr);
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
    [DllImport("user32.dll")] internal static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] internal static extern IntPtr GetWindowLongPtr(IntPtr window,int index);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")] internal static extern IntPtr SetWindowLongPtr(IntPtr window,int index,IntPtr value);
    internal static int ForegroundPid(){uint pid;GetWindowThreadProcessId(GetForegroundWindow(),out pid);return (int)pid;}
    internal static MemoryInfo Memory() {
        MemoryInfo m = new MemoryInfo(); m.Length = (uint)Marshal.SizeOf(m);
        if (!GlobalMemoryStatusEx(ref m)) throw new Win32Exception(); return m;
    }
    internal static Guid PowerScheme() {
        IntPtr p; if(PowerGetActiveScheme(IntPtr.Zero,out p)!=0) throw new InvalidOperationException("Power plan is unavailable.");
        try { return (Guid)Marshal.PtrToStructure(p,typeof(Guid)); } finally { LocalFree(p); }
    }
    internal static void Rename(SafeFileHandle file, string destination) {
        byte[] name=Encoding.Unicode.GetBytes(destination); int offset=IntPtr.Size==8?20:12;
        IntPtr buf=Marshal.AllocHGlobal(offset+name.Length+2);
        try {
            for(int i=0;i<offset+name.Length+2;i++) Marshal.WriteByte(buf,i,0);
            Marshal.WriteInt32(buf,IntPtr.Size==8?16:8,name.Length);
            Marshal.Copy(name,0,IntPtr.Add(buf,offset),name.Length);
            if(!SetFileInformationByHandle(file,3,buf,(uint)(offset+name.Length+2))) throw new Win32Exception();
        } finally { Marshal.FreeHGlobal(buf); }
    }
}
}
