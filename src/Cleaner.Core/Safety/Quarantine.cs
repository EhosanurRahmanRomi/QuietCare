#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Win32.SafeHandles;

namespace QuietCareSafety {
public static class State {
    public static string DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QuietCareNext","Recovery");
    public static string FilePath(string name){return Path.Combine(DirectoryPath,name);}
    public static XDocument Read(string path) {
        using(var locks=SafePaths.LockParents(path))using(var handle=Native.CreateFile(path,0x80000080,1,IntPtr.Zero,3,0x00200000,IntPtr.Zero)) {
            SafePaths.Inspect(handle,path);
            using(var stream=new FileStream(handle,FileAccess.Read))using(var reader=XmlReader.Create(stream,new XmlReaderSettings {DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=8*1024*1024}))return XDocument.Load(reader);
        }
    }
    public static void Save(string path,XDocument doc) {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using(var locks=SafePaths.LockParents(Path.Combine(Path.GetDirectoryName(path),"lock"))) {
            string tmp=path+"."+Guid.NewGuid().ToString("N")+".new";
            try {doc.Save(tmp);if(File.Exists(path)) File.Replace(tmp,path,null); else File.Move(tmp,path);} finally {if(File.Exists(tmp))File.Delete(tmp);}
        }
    }
    public static void Log(string message) {
        try { string path=FilePath("activity.log");Directory.CreateDirectory(DirectoryPath);using(var locks=SafePaths.LockParents(path)) {
            if(File.Exists(path) && new FileInfo(path).Length>256000) File.Delete(path);
            File.AppendAllText(path,DateTime.Now.ToString("s")+" "+message+Environment.NewLine);
        }}catch(Exception){}
    }
}
internal sealed class DirectoryLocks : IDisposable {
    internal List<SafeFileHandle> Handles=new List<SafeFileHandle>();
    public void Dispose(){foreach(var h in Handles)h.Dispose();}
}
internal static class SafePaths {
    internal static bool Within(string root,string path) {
        root=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(root,StringComparison.OrdinalIgnoreCase);
    }
    internal static DirectoryLocks LockParents(string path) {
        string parent=Path.GetDirectoryName(Path.GetFullPath(path)),root=Path.GetPathRoot(parent);var locks=new DirectoryLocks();
        try {
            if(root.StartsWith(@"\\",StringComparison.Ordinal))throw new IOException("Network paths are excluded.");
            string current=root;
            foreach(string part in parent.Substring(root.Length).Split(new[]{Path.DirectorySeparatorChar},StringSplitOptions.RemoveEmptyEntries)) {
                current=Path.Combine(current,part);var h=Native.CreateFile(current,0x80,3,IntPtr.Zero,3,0x02000000|0x00200000,IntPtr.Zero);
                if(h.IsInvalid){h.Dispose();throw new Win32Exception();}
                locks.Handles.Add(h);Native.FileInfo info;
                if(!Native.GetFileInformationByHandle(h,out info) || (info.Attributes&0x400)!=0)throw new IOException("Linked folders are excluded.");
            }
            return locks;
        } catch {locks.Dispose();throw;}
    }
    internal static SafeFileHandle Open(string path,bool mutation) {
        var h=Native.CreateFile(path,0x80u|(mutation?0x10000u:0),mutation?1u:7u,IntPtr.Zero,3,0x00200000,IntPtr.Zero);
        if(h.IsInvalid){h.Dispose();throw new Win32Exception();}return h;
    }
    internal static Native.FileInfo Inspect(SafeFileHandle h,string expected) {
        Native.FileInfo info; if(!Native.GetFileInformationByHandle(h,out info))throw new Win32Exception();
        if((info.Attributes&(0x400|0x10))!=0 || info.Links!=1)throw new IOException("Links and directories are excluded.");
        var sb=new StringBuilder(32768);uint n=Native.GetFinalPathNameByHandle(h,sb,(uint)sb.Capacity,0);
        if(n==0 || n>=sb.Capacity)throw new Win32Exception();
        string final=sb.ToString();if(final.StartsWith(@"\\?\"))final=final.Substring(4);
        if(!String.Equals(Path.GetFullPath(final),Path.GetFullPath(expected),StringComparison.OrdinalIgnoreCase))throw new IOException("The resolved file path changed.");
        return info;
    }
}
public sealed class TempItem {
    public string Path {get;set;} public string Size {get;set;} public long Bytes {get;set;} public string Modified {get;set;}
    public string Identity {get;set;} public ulong Written {get;set;}
}
public sealed class ScanResult {
    public List<TempItem> Items=new List<TempItem>(); public int Skipped; public bool Limited; public long Bytes {get{return Items.Sum(x=>x.Bytes);}}
}
public sealed class ScanFileProgress {
    public int Examined,Candidates;public string Path;public long Bytes;
}
public sealed class RecoveryItem {
    public string Id {get;set;} public string Original {get;set;} public string Stored {get;set;} public string Size {get;set;} public string When {get;set;}
    public string Identity {get;set;} public long Bytes {get;set;}public ulong Written {get;set;}
}
public sealed class Cleaner {
    internal readonly string Root,Recovery;
    readonly Func<string,DateTime,bool> policy;
    public Cleaner():this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Temp"),State.FilePath("Recovery")){}
    internal Cleaner(string root,string recovery,Func<string,DateTime,bool> eligibility=null){Root=Path.GetFullPath(root);Recovery=Path.GetFullPath(recovery);policy=eligibility??Eligible;}
    static readonly HashSet<string> Extensions=new HashSet<string>(new[]{".tmp",".temp",".log",".etl",".dmp",".cache"},StringComparer.OrdinalIgnoreCase);
    internal static bool Eligible(string path,DateTime modified){return Extensions.Contains(Path.GetExtension(path)) && modified<DateTime.UtcNow.AddDays(-7);}
    public ScanResult Scan(CancellationToken cancel,Action<ScanFileProgress> progress=null) {
        var result=new ScanResult();if(!Directory.Exists(Root))return result;
        var watch=System.Diagnostics.Stopwatch.StartNew();var dirs=new Stack<string>();dirs.Push(Root);int visited=0;long nextReport=0,foundBytes=0;string current=Root;
        while(dirs.Count>0) {
            cancel.ThrowIfCancellationRequested();if(visited>30000 || watch.Elapsed.TotalSeconds>20){result.Limited=true;break;}
            string dir=dirs.Pop();
            try {using(var locks=SafePaths.LockParents(Path.Combine(dir,"probe"))) {
                foreach(string p in Directory.EnumerateFileSystemEntries(dir)) {
                    cancel.ThrowIfCancellationRequested();if(++visited>30000 || watch.Elapsed.TotalSeconds>20){result.Limited=true;break;}
                    current=p;if(progress!=null&&watch.ElapsedMilliseconds>=nextReport){progress(new ScanFileProgress{Examined=visited,Candidates=result.Items.Count,Bytes=foundBytes,Path=current});nextReport=watch.ElapsedMilliseconds+120;}
                    try {
                        var attr=File.GetAttributes(p);if((attr&FileAttributes.ReparsePoint)!=0){result.Skipped++;continue;}
                        if((attr&FileAttributes.Directory)!=0){dirs.Push(p);continue;}
                        if(!policy(p,File.GetLastWriteTimeUtc(p)))continue;
                        using(var h=SafePaths.Open(p,false)) {
                            var info=SafePaths.Inspect(h,p);if(!policy(p,DateTime.FromFileTimeUtc((long)info.Written.Value)))continue;if(info.Size>512UL*1024*1024){result.Skipped++;continue;}
                            result.Items.Add(new TempItem {Path=p,Bytes=(long)info.Size,Size=Format.Bytes(info.Size),Modified=DateTime.FromFileTimeUtc((long)info.Written.Value).ToLocalTime().ToString("g"),Identity=info.Identity,Written=info.Written.Value});
                            foundBytes+=(long)info.Size;
                        }
                    }catch(IOException){result.Skipped++;}catch(UnauthorizedAccessException){result.Skipped++;}catch(Win32Exception){result.Skipped++;}
                }
            }}catch(IOException){result.Skipped++;}catch(UnauthorizedAccessException){result.Skipped++;}catch(Win32Exception){result.Skipped++;}
        }
        if(progress!=null)progress(new ScanFileProgress{Examined=visited,Candidates=result.Items.Count,Bytes=foundBytes,Path=current});return result;
    }
    public RecoveryItem Quarantine(TempItem item) {
        if(!SafePaths.Within(Root,item.Path) || !policy(item.Path,DateTime.FromFileTimeUtc((long)item.Written))) throw new IOException("The file is outside the cleanup policy.");
        if(!String.Equals(Path.GetPathRoot(Root),Path.GetPathRoot(Recovery),StringComparison.OrdinalIgnoreCase)) throw new IOException("Recovery must be on the same drive as Temp.");
        Directory.CreateDirectory(Recovery);
        string id=Guid.NewGuid().ToString("N"), stored=Path.Combine(Recovery,id+".qcf");
        using(var sourceLocks=SafePaths.LockParents(item.Path))using(var destLocks=SafePaths.LockParents(stored))using(var h=SafePaths.Open(item.Path,true)) {
            var info=SafePaths.Inspect(h,item.Path);
            if(info.Identity!=item.Identity || info.Size!=(ulong)item.Bytes || info.Written.Value!=item.Written)throw new IOException("The file changed since the scan. Scan again.");
            var rec=new RecoveryItem {Id=id,Original=item.Path,Stored=stored,Bytes=item.Bytes,Size=item.Size,When=DateTime.Now.ToString("g"),Identity=info.Identity,Written=info.Written.Value};
            // Commit recovery metadata before the handle-based move. A crash leaves a recoverable record.
            State.Save(Path.Combine(Recovery,id+".xml"),new XDocument(new XElement("file",new XAttribute("id",id),new XAttribute("original",item.Path),new XAttribute("bytes",item.Bytes),new XAttribute("identity",info.Identity),new XAttribute("written",info.Written.Value),new XAttribute("when",rec.When))));
            try {Native.Rename(h,stored);}catch{File.Delete(Path.Combine(Recovery,id+".xml"));throw;}
            return rec;
        }
    }
    public List<RecoveryItem> ListRecovery() {
        var list=new List<RecoveryItem>();if(!Directory.Exists(Recovery))return list;
        using(var locks=SafePaths.LockParents(Path.Combine(Recovery,"probe"))) {
            foreach(string meta in Directory.EnumerateFiles(Recovery,"*.xml").Take(20000)) try {
                var r=State.Read(meta).Root;string id=(string)r.Attribute("id");Guid guid;
                if(!Guid.TryParseExact(id,"N",out guid) || Path.GetFileNameWithoutExtension(meta)!=id)continue;
                string stored=Path.Combine(Recovery,id+".qcf");if(!File.Exists(stored))continue;
                long bytes=(long)r.Attribute("bytes");
                if(bytes<0)continue;
                list.Add(new RecoveryItem {Id=id,Original=(string)r.Attribute("original"),Stored=stored,Identity=(string)r.Attribute("identity"),Bytes=bytes,Size=Format.Bytes(bytes),When=(string)r.Attribute("when"),Written=(ulong?)r.Attribute("written")??0});
            }catch(Exception){ }
        }
        return list;
    }
    void CheckRecord(RecoveryItem item) {
        Guid id;if(item==null || !Guid.TryParseExact(item.Id,"N",out id) || !String.Equals(item.Stored,Path.Combine(Recovery,item.Id+".qcf"),StringComparison.OrdinalIgnoreCase) || !SafePaths.Within(Root,item.Original))throw new IOException("Invalid recovery record.");
        var stored=State.Read(Path.Combine(Recovery,item.Id+".xml")).Root;
        if((string)stored.Attribute("id")!=item.Id || (string)stored.Attribute("original")!=item.Original || (string)stored.Attribute("identity")!=item.Identity || (long)stored.Attribute("bytes")!=item.Bytes || ((ulong?)stored.Attribute("written")??0)!=item.Written)throw new IOException("The recovery record changed. Refresh before continuing.");
    }
    void VerifyRecovery(RecoveryItem item,Native.FileInfo info){if(info.Identity!=item.Identity || info.Size!=(ulong)item.Bytes || (item.Written!=0 && info.Written.Value!=item.Written))throw new IOException("The recovery file changed. It will not be modified.");}
    void RemoveMetadata(RecoveryItem item){try{File.Delete(Path.Combine(Recovery,item.Id+".xml"));}catch(IOException){State.Log("A completed recovery operation left a stale metadata record.");}catch(UnauthorizedAccessException){State.Log("A completed recovery operation left a stale metadata record.");}}
    public void Restore(RecoveryItem item) {
        CheckRecord(item);
        // Do not recreate missing source trees: a user must inspect them first.
        using(var a=SafePaths.LockParents(item.Stored))using(var b=SafePaths.LockParents(item.Original))using(var h=SafePaths.Open(item.Stored,true)) {
            var info=SafePaths.Inspect(h,item.Stored);VerifyRecovery(item,info);
            Native.Rename(h,item.Original); // ReplaceIfExists=false: never overwrite an existing file.
        }
        RemoveMetadata(item);
    }
    public void Purge(RecoveryItem item) {
        CheckRecord(item);
        if(item.Written==0)throw new IOException("This older recovery record cannot be verified for deletion. Restore or inspect it first.");
        using(var a=SafePaths.LockParents(item.Stored))using(var h=SafePaths.Open(item.Stored,true)) {
            var info=SafePaths.Inspect(h,item.Stored);VerifyRecovery(item,info);
            IntPtr buf=Marshal.AllocHGlobal(4);try{Marshal.WriteInt32(buf,1);if(!Native.SetFileInformationByHandle(h,4,buf,4))throw new Win32Exception();}finally{Marshal.FreeHGlobal(buf);}
        }
        RemoveMetadata(item);
    }
}
}
