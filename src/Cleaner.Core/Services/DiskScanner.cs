using Cleaner.Core.Models;
namespace Cleaner.Core.Services;

// One bounded worker per scan; counters belong to the session, never to the shared service.
// Iterative traversal avoids nested parallel jobs and deep-directory stack overflow.
public sealed class DiskScanner : IDiskScanner
{
    public ScanSession StartScan(string rootPath,IProgress<ScanProgress>? progress=null,CancellationToken ct=default)
    {
        rootPath=Path.GetFullPath(rootPath);
        var root=new FileSystemNode{Name=Path.GetFileName(rootPath.TrimEnd('\\')) is {Length:>0} name?name:rootPath,FullPath=rootPath,IsDirectory=true};
        var completion=Task.Run(async()=>
        {
            long bytes=0;int files=0,nodes=1;
            var pending=new Stack<FileSystemNode>();pending.Push(root);
            while(pending.TryPop(out var node))
            {
                ct.ThrowIfCancellationRequested();
                if(!Utils.SafeEnumerator.IsUnlinked(node.FullPath))continue;
                foreach(var file in Utils.SafeEnumerator.EnumerateFiles(node.FullPath))
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        if(++nodes>250000)throw new ScanLimitException();
                        var info=new FileInfo(file);
                        var child=new FileSystemNode{Name=info.Name,FullPath=file,IsDirectory=false,Size=info.Length,FileCount=1,LastWriteUtc=info.LastWriteTimeUtc,Parent=node};
                        lock(node.SyncRoot)node.Children.Add(child);
                        Bubble(node,child.Size);bytes+=child.Size;files++;
                        if((files&255)==0){progress?.Report(new("disk-scan",file,bytes,files));await Task.Delay(1,ct);}
                    }
                    catch(ScanLimitException){throw;}
                    catch(IOException){}catch(UnauthorizedAccessException){}
                }
                foreach(var directory in Utils.SafeEnumerator.EnumerateDirectories(node.FullPath))
                {
                    ct.ThrowIfCancellationRequested();
                    if(++nodes>250000)throw new ScanLimitException();
                    var child=new FileSystemNode{Name=Path.GetFileName(directory),FullPath=directory,IsDirectory=true,Parent=node};
                    lock(node.SyncRoot)node.Children.Add(child);pending.Push(child);
                }
            }
            progress?.Report(new("disk-scan",rootPath,bytes,files));
        },ct);
        return new(root,completion);
    }
    static void Bubble(FileSystemNode? node,long bytes)
    {
        while(node is not null){Interlocked.Add(ref node.Size,bytes);Interlocked.Increment(ref node.FileCount);node=node.Parent;}
    }
    sealed class ScanLimitException : IOException
    { public ScanLimitException():base("Scan reached the 250,000-item memory limit. The tree is partial; select a smaller folder for a complete analysis."){} }
}
