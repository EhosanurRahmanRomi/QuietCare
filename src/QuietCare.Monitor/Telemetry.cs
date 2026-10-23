using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace QuietCare {
public sealed class Snapshot {
    public DateTime Time {get;set;}
    public double? Cpu {get;set;}
    public double? Gpu {get;set;}
    public double? DiskRead {get;set;}
    public double? DiskWrite {get;set;}
    public double Ram {get;set;}
    public ulong Total {get;set;}
    public ulong Available {get;set;}
    public double? Download {get;set;}
    public double? Upload {get;set;}
    public string GpuStatus {get;set;}
}
public sealed class Telemetry : IDisposable {
    IntPtr query, detailQuery, gpu, read, write; ulong lastIdle, lastTotal; bool primed,disposed; DateTime lastNetwork;
    Dictionary<string,long[]> traffic=new Dictionary<string,long[]>();
    NetworkInterface[] adapters=new NetworkInterface[0];DateTime nextAdapters,nextNetwork;double? lastDownload,lastUpload;
    IntPtr gpuBuffer;uint gpuCapacity;
    static string EngineKey(string name){
        if(String.IsNullOrEmpty(name))return null;int start=name.IndexOf("luid_",StringComparison.Ordinal),physical=name.IndexOf("_phys_",StringComparison.Ordinal),engine=name.IndexOf("_eng_",StringComparison.Ordinal);
        if(start<0||physical<=start||engine<=physical)return null;int p=physical+6;while(p<name.Length&&name[p]>='0'&&name[p]<='9')p++;if(p==physical+6||p!=engine)return null;int end=engine+5;while(end<name.Length&&name[end]>='0'&&name[end]<='9')end++;if(end==engine+5)return null;return name.Substring(start,end-start);
    }
    static void Accumulate(Dictionary<string,double> groups,string name,double value){if(double.IsNaN(value)||double.IsInfinity(value))return;string key=EngineKey(name);if(key==null)return;double prior;groups.TryGetValue(key,out prior);groups[key]=prior+Math.Max(0,value);}
    public Telemetry() {
        if(Native.PdhOpenQuery(null,IntPtr.Zero,out query)==0) {
            Native.PdhAddEnglishCounter(query,@"\GPU Engine(*)\Utilization Percentage",IntPtr.Zero,out gpu);
            Native.PdhCollectQueryData(query);
        }
    }
    public static double Clamp(double value) { return Math.Max(0,Math.Min(100,value)); }
    double? Scalar(IntPtr counter) {
        if(counter==IntPtr.Zero) return null; uint type; Native.CounterValue v;
        return Native.PdhGetFormattedCounterValue(counter,0x200,out type,out v)==0 && v.Status<=1 && !double.IsNaN(v.Value) && !double.IsInfinity(v.Value) ? (double?)Math.Max(0,v.Value) : null;
    }
    public static double? AggregateGpu(IEnumerable<KeyValuePair<string,double>> values) {
        var groups=new Dictionary<string,double>();
        foreach(var v in values)Accumulate(groups,v.Key,v.Value);
        return groups.Count==0?null:(double?)Clamp(groups.Values.Max());
    }
    double? Gpu() {
        if(gpu==IntPtr.Zero) return null; uint bytes=0,count=0;
        Native.PdhGetFormattedCounterArray(gpu,0x200,ref bytes,ref count,IntPtr.Zero);
        if(bytes==0 || bytes>16*1024*1024) return null;
        if(bytes>gpuCapacity){IntPtr next=Marshal.AllocHGlobal((int)bytes);if(gpuBuffer!=IntPtr.Zero)Marshal.FreeHGlobal(gpuBuffer);gpuBuffer=next;gpuCapacity=bytes;}
            if(Native.PdhGetFormattedCounterArray(gpu,0x200,ref bytes,ref count,gpuBuffer)!=0) return null;
            int size=Marshal.SizeOf(typeof(Native.CounterItem));if((ulong)count*(uint)size>bytes)return null;var groups=new Dictionary<string,double>();
            for(int i=0;i<count;i++) {
                var item=(Native.CounterItem)Marshal.PtrToStructure(IntPtr.Add(gpuBuffer,i*size),typeof(Native.CounterItem));
                if(item.Value.Status<=1&&item.Name!=IntPtr.Zero)Accumulate(groups,Marshal.PtrToStringUni(item.Name),item.Value.Value);
            }
            return groups.Count==0?null:(double?)Clamp(groups.Values.Max());
    }
    public Snapshot Sample(bool detailed=true) {
        if(disposed)throw new ObjectDisposedException("Telemetry");
        var m=Native.Memory(); var s=new Snapshot {Time=DateTime.Now,Ram=m.Load,Total=m.Total,Available=m.Available};
        Native.FileTime idle,kernel,user;
        if(Native.GetSystemTimes(out idle,out kernel,out user)) {
            ulong total=kernel.Value+user.Value;
            if(primed && total>lastTotal) s.Cpu=Clamp(100.0*(1.0-(double)(idle.Value-lastIdle)/(total-lastTotal)));
            primed=true;lastIdle=idle.Value;lastTotal=total;
        }
        if(query!=IntPtr.Zero && Native.PdhCollectQueryData(query)==0)s.Gpu=Gpu();
        s.GpuStatus=s.Gpu.HasValue?"Busiest GPU engine across adapters":"GPU counter unavailable on this driver";
        if(!detailed){lastNetwork=default(DateTime);traffic.Clear();lastDownload=null;lastUpload=null;return s;}
        if(detailQuery==IntPtr.Zero && Native.PdhOpenQuery(null,IntPtr.Zero,out detailQuery)==0){Native.PdhAddEnglishCounter(detailQuery,@"\PhysicalDisk(_Total)\Disk Read Bytes/sec",IntPtr.Zero,out read);Native.PdhAddEnglishCounter(detailQuery,@"\PhysicalDisk(_Total)\Disk Write Bytes/sec",IntPtr.Zero,out write);Native.PdhCollectQueryData(detailQuery);}
        else if(detailQuery!=IntPtr.Zero && Native.PdhCollectQueryData(detailQuery)==0){s.DiskRead=Scalar(read);s.DiskWrite=Scalar(write);}
        DateTime now=DateTime.UtcNow;if(now<nextNetwork){s.Download=lastDownload;s.Upload=lastUpload;return s;}
        nextNetwork=now.AddSeconds(1);
        if(now>=nextAdapters){adapters=NetworkInterface.GetAllNetworkInterfaces();nextAdapters=now.AddSeconds(30);}
        double seconds=lastNetwork==default(DateTime)?0:(now-lastNetwork).TotalSeconds;var next=new Dictionary<string,long[]>();double received=0,sent=0;bool hasBaseline=false;
        foreach(var adapter in adapters) {
            if(adapter.OperationalStatus!=OperationalStatus.Up || adapter.NetworkInterfaceType==NetworkInterfaceType.Loopback || adapter.NetworkInterfaceType==NetworkInterfaceType.Tunnel) continue;
            try {
                var t=adapter.GetIPStatistics(); next[adapter.Id]=new long[]{t.BytesReceived,t.BytesSent}; long[] prior;
                if(seconds>0 && traffic.TryGetValue(adapter.Id,out prior)) {received+=Math.Max(0,t.BytesReceived-prior[0])/seconds;sent+=Math.Max(0,t.BytesSent-prior[1])/seconds;hasBaseline=true;}
            } catch(NetworkInformationException) { }
        }
        if(hasBaseline){s.Download=received;s.Upload=sent;}traffic=next;lastNetwork=now;lastDownload=s.Download;lastUpload=s.Upload;return s;
    }
    public void Dispose(){disposed=true;if(query!=IntPtr.Zero){Native.PdhCloseQuery(query);query=IntPtr.Zero;}if(detailQuery!=IntPtr.Zero){Native.PdhCloseQuery(detailQuery);detailQuery=IntPtr.Zero;}if(gpuBuffer!=IntPtr.Zero){Marshal.FreeHGlobal(gpuBuffer);gpuBuffer=IntPtr.Zero;gpuCapacity=0;}}
}
public sealed class AppProcess {
    public int Id {get;set;} public string Name {get;set;} public string Memory {get;set;} public long Bytes {get;set;}
    public DateTime Started {get;set;} public string Path {get;set;} public string Eligibility {get;set;}
}
public static class Processes {
    public static List<AppProcess> List() {
        var list=new List<AppProcess>(); int session=Process.GetCurrentProcess().SessionId;
        foreach(var p in Process.GetProcesses()) using(p) {
            try {
                if(p.Id==0 || p.SessionId!=session) continue;
                list.Add(new AppProcess {Id=p.Id,Name=p.ProcessName,Bytes=p.WorkingSet64,Memory=Format.Bytes(p.WorkingSet64),Started=p.StartTime.ToUniversalTime(),Path=p.MainModule.FileName,Eligibility=p.Id==Process.GetCurrentProcess().Id?"QuietCare":"Manual selection only"});
            }catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}catch(NotSupportedException){}
        }
        return list.OrderByDescending(p=>p.Bytes).ToList();
    }
    public static void Trim(AppProcess item) {
        if(item==null) throw new InvalidOperationException("Select an app first.");
        using(var p=Process.GetProcessById(item.Id)) {
            if(p.SessionId!=Process.GetCurrentProcess().SessionId || p.StartTime.ToUniversalTime()!=item.Started) throw new InvalidOperationException("The process changed. Refresh the list.");
            using(var h=Native.OpenProcess(0x1000|0x100,false,item.Id)) {
                if(h.IsInvalid) throw new System.ComponentModel.Win32Exception();
                Native.FileTime c,e,k,u;
                if(!Native.GetProcessTimes(h,out c,out e,out k,out u) || DateTime.FromFileTimeUtc((long)c.Value)!=item.Started) throw new InvalidOperationException("Process identity changed.");
                var path=new System.Text.StringBuilder(32768);int size=path.Capacity;
                if(!Native.QueryFullProcessImageName(h,0,path,ref size)) throw new System.ComponentModel.Win32Exception();
                string win=Environment.GetFolderPath(Environment.SpecialFolder.Windows)+System.IO.Path.DirectorySeparatorChar;
                if(path.ToString().StartsWith(win,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Windows processes are excluded from RAM trimming.");
                if(!Native.EmptyWorkingSet(h)) throw new System.ComponentModel.Win32Exception();
            }
        }
    }
}
public static class Format {
    public static string Rate(double? value){return value.HasValue?Bytes(value.Value)+"/s":"—";}
    public static string Number(double? value,string format="F0"){return value.HasValue?value.Value.ToString(format,System.Globalization.CultureInfo.InvariantCulture):"";}
    public static string Bytes(double value) { string[] units={"B","KB","MB","GB","TB"};int i=0;while(value>=1024 && i<4){value/=1024;i++;}return value.ToString(i==0?"0":"0.0")+" "+units[i]; }
    public static string Percent(double? value) {return value.HasValue?value.Value.ToString("0")+"%":"—";}
}
public static class MonitorPolicy {
    public static bool Active(bool mainVisible,bool minimized,bool widgetVisible,bool suspended){return !suspended && ((mainVisible && !minimized)||widgetVisible);}
}
}
