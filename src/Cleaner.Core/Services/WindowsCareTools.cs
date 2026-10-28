using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
namespace Cleaner.Core.Services;

public enum WindowsCareJob { DefenderQuickScan, DefenderFullScan, OptimizeSystemDrive }
public sealed record DefenderStatus(bool Available,bool AntivirusEnabled,bool RealTimeEnabled,string SignatureUpdated,string LastQuickScan,string Detail)
{
    public DateTimeOffset? LastQuickScanUtc {get;init;}
    public bool RealtimeOffByLocalPolicy {get;init;}
}
public sealed record WindowsJobResult(int ExitCode,string Message);

public static class WindowsCareTools
{
    public static Task<DefenderStatus> ReadDefenderStatusAsync()=>Task.Run(()=>
    {
        try
        {
            using var search=new ManagementObjectSearcher(@"root\Microsoft\Windows\Defender","SELECT AntivirusEnabled,RealTimeProtectionEnabled,AntivirusSignatureLastUpdated,QuickScanEndTime FROM MSFT_MpComputerStatus",new System.Management.EnumerationOptions{Timeout=TimeSpan.FromSeconds(3),ReturnImmediately=true});
            using var rows=search.Get();
            foreach(ManagementObject row in rows)using(row)
            {
                using var policy=Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection");
                return new DefenderStatus(true,Convert.ToBoolean(row["AntivirusEnabled"]),Convert.ToBoolean(row["RealTimeProtectionEnabled"]),Date(row["AntivirusSignatureLastUpdated"]),Date(row["QuickScanEndTime"]),"Microsoft Defender status, read from Windows."){LastQuickScanUtc=UtcDate(row["QuickScanEndTime"]),RealtimeOffByLocalPolicy=policy?.GetValue("DisableRealtimeMonitoring") is int off&&off==1};
            }
            return new(false,false,false,"Unknown","Unknown","Windows did not return Defender status. Open Windows Security to check your active antivirus.");
        }
        catch(Exception ex){return new(false,false,false,"Unknown","Unknown","Defender status unavailable: "+ex.Message);}
    });
    static string Date(object? value){try{return value is string s?ManagementDateTimeConverter.ToDateTime(s).ToString("g"):"Unknown";}catch{return "Unknown";}}
    static DateTimeOffset? UtcDate(object? value){try{return value is string s?new DateTimeOffset(ManagementDateTimeConverter.ToDateTime(s)).ToUniversalTime():null;}catch{return null;}}
    public static (string File,string Arguments) Resolve(WindowsCareJob job)
    {
        if(job==WindowsCareJob.OptimizeSystemDrive)
        {
            var drive=Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows))![0];
            if(drive is < 'A' or > 'Z')throw new InvalidOperationException("Invalid system drive.");
            return(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"defrag.exe"),$"{drive}: /O /U /V");
        }
        if(job is not WindowsCareJob.DefenderQuickScan and not WindowsCareJob.DefenderFullScan)throw new ArgumentOutOfRangeException(nameof(job));
        var platform=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"Microsoft","Windows Defender","Platform");
        if(Directory.Exists(platform))foreach(var directory in Utils.SafeEnumerator.EnumerateDirectories(platform).OrderByDescending(x=>Version.TryParse(Path.GetFileName(x).Split('-')[0],out var version)?version:new Version()))
        {
            var exe=Path.Combine(directory,"MpCmdRun.exe");if(File.Exists(exe)&&MicrosoftSignature.IsTrusted(exe))return(exe,"-Scan -ScanType "+(job==WindowsCareJob.DefenderQuickScan?"1":"2"));
        }
        var fallback=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"Windows Defender","MpCmdRun.exe");
        if(!File.Exists(fallback))throw new FileNotFoundException("Microsoft Defender command-line scanner is unavailable. Use your active antivirus from Windows Security.");
        return(fallback,"-Scan -ScanType "+(job==WindowsCareJob.DefenderQuickScan?"1":"2"));
    }
    public static async Task<WindowsJobResult> RunAsync(WindowsCareJob job,IProgress<string>? progress=null,CancellationToken ct=default)
    {
        ct.ThrowIfCancellationRequested();
        progress?.Report("Checking the Windows tool and its Microsoft signature…");
        var command=await Task.Run(()=>Resolve(job));
        using var lockedFile=new FileStream(command.File,FileMode.Open,FileAccess.Read,FileShare.Read);
        if(!MicrosoftSignature.IsTrusted(command.File))throw new InvalidOperationException("The Windows tool signature could not be verified. Nothing was started.");
        ct.ThrowIfCancellationRequested();
        progress?.Report("Requesting Windows permission. Approve the Windows UAC dialog to continue.");
        using var process=await Task.Run(()=>Process.Start(new ProcessStartInfo(command.File,command.Arguments){UseShellExecute=true,Verb="runas",WindowStyle=ProcessWindowStyle.Hidden}))??throw new InvalidOperationException("Windows did not start the requested tool.");
        var clock=Stopwatch.StartNew();
        while(!process.HasExited)
        {
            progress?.Report((ct.IsCancellationRequested?"Stop requested. Waiting for the running Windows job to finish safely; remaining actions will not start. ":"")+(job==WindowsCareJob.OptimizeSystemDrive?"Windows is optimizing the system drive with the operation appropriate to its media type":"Microsoft Defender is scanning")+$" · {clock.Elapsed:mm\\:ss}. Waiting for the actual Windows result.");
            await Task.Delay(1500);process.Refresh();
        }
        int code=process.ExitCode;
        string message=job==WindowsCareJob.OptimizeSystemDrive
            ?code==0?"Windows completed drive optimization.":$"Windows optimization returned code {code}. Open Optimize Drives for details."
            :code==0?"Defender completed the scan. Code 0 means no malware was found OR detected threats were successfully remediated. Review Protection history for exact findings.":$"Defender returned code {code}. This can indicate an unresolved threat or a scan error. Review Windows Security and Protection history.";
        progress?.Report(message);return new(code,message);
    }
    public static void OpenSecurity()=>Process.Start(new ProcessStartInfo("windowsdefender:"){UseShellExecute=true});
    public static void OpenOptimization()=>Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"dfrgui.exe")){UseShellExecute=true});
}

public static class MicrosoftSignature
{
    public static bool IsTrusted(string path)
    {
        try
        {
            if(!Utils.SafeEnumerator.IsUnlinked(path))return false;
            using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
            var info=new TrustFile{Size=(uint)Marshal.SizeOf<TrustFile>(),Path=Path.GetFullPath(path),Handle=file.SafeFileHandle.DangerousGetHandle()};
            if(Verify(info,1))return true;
            return VerifyCatalog(file,path);
        }
        catch{return false;}
    }
    static bool Verify<T>(T info,uint choice) where T:struct
    {
        IntPtr filePointer=IntPtr.Zero;
        var action=new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
        var data=new TrustData();
        try
        {
            filePointer=Marshal.AllocHGlobal(Marshal.SizeOf<T>());Marshal.StructureToPtr(info,filePointer,false);
            data=new TrustData{Size=(uint)Marshal.SizeOf<TrustData>(),UI=2,Choice=choice,File=filePointer,Flags=0x1000,StateAction=1};
            if(WinVerifyTrust(IntPtr.Zero,ref action,ref data)!=0)return false;
            var provider=WTHelperProvDataFromStateData(data.State);if(provider==IntPtr.Zero)return false;
            var signer=WTHelperGetProvSignerFromChain(provider,0,false,0);if(signer==IntPtr.Zero)return false;
            var entry=WTHelperGetProvCertFromChain(signer,0);if(entry==IntPtr.Zero)return false;
            var context=Marshal.PtrToStructure<TrustCertificate>(entry).Context;if(context==IntPtr.Zero)return false;
            var encoded=Marshal.PtrToStructure<CertificateContext>(context);
            if(encoded.Length==0||encoded.Length>1024*1024||encoded.Encoded==IntPtr.Zero)return false;
            var bytes=new byte[encoded.Length];Marshal.Copy(encoded.Encoded,bytes,0,bytes.Length);
            using var certificate=X509CertificateLoader.LoadCertificate(bytes);
            return certificate.SubjectName.EnumerateRelativeDistinguishedNames().Any(r=>!r.HasMultipleElements&&r.GetSingleElementType().Value=="2.5.4.10"&&r.GetSingleElementValue()=="Microsoft Corporation");
        }
        catch{return false;}
        finally
        {
            if(data.State!=IntPtr.Zero){data.StateAction=2;WinVerifyTrust(IntPtr.Zero,ref action,ref data);}
            if(filePointer!=IntPtr.Zero){Marshal.DestroyStructure<T>(filePointer);Marshal.FreeHGlobal(filePointer);}
        }
    }
    static bool VerifyCatalog(FileStream file,string path)
    {
        IntPtr admin=IntPtr.Zero,catalog=IntPtr.Zero,hashPointer=IntPtr.Zero;
        try
        {
            if(!CryptCATAdminAcquireContext2(out admin,IntPtr.Zero,"SHA256",IntPtr.Zero,0))return false;
            uint length=0;var handle=file.SafeFileHandle.DangerousGetHandle();
            CryptCATAdminCalcHashFromFileHandle2(admin,handle,ref length,null,0);
            if(length==0||length>128)return false;
            var hash=new byte[length];if(!CryptCATAdminCalcHashFromFileHandle2(admin,handle,ref length,hash,0))return false;
            catalog=CryptCATAdminEnumCatalogFromHash(admin,hash,length,0,IntPtr.Zero);if(catalog==IntPtr.Zero)return false;
            var catalogInfo=new CatalogInfo{Size=(uint)Marshal.SizeOf<CatalogInfo>(),Path=""};
            if(!CryptCATCatalogInfoFromContext(catalog,ref catalogInfo,0))return false;
            hashPointer=Marshal.AllocHGlobal(hash.Length);Marshal.Copy(hash,0,hashPointer,hash.Length);
            return Verify(new TrustCatalog{Size=(uint)Marshal.SizeOf<TrustCatalog>(),Catalog=catalogInfo.Path,Tag=Convert.ToHexString(hash),Member=Path.GetFullPath(path),Handle=handle,Hash=hashPointer,HashLength=length,Admin=admin},2);
        }
        finally
        {
            if(hashPointer!=IntPtr.Zero)Marshal.FreeHGlobal(hashPointer);
            if(catalog!=IntPtr.Zero)CryptCATAdminReleaseCatalogContext(admin,catalog,0);
            if(admin!=IntPtr.Zero)CryptCATAdminReleaseContext(admin,0);
        }
    }
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct CatalogInfo{public uint Size;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)]public string Path;}
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct TrustCatalog{public uint Size,Version;[MarshalAs(UnmanagedType.LPWStr)]public string Catalog,Tag,Member;public IntPtr Handle,Hash;public uint HashLength;public IntPtr CatalogContext,Admin;}
    [DllImport("wintrust.dll",CharSet=CharSet.Unicode,ExactSpelling=true)][return:MarshalAs(UnmanagedType.Bool)]static extern bool CryptCATAdminAcquireContext2(out IntPtr admin,IntPtr subsystem,string algorithm,IntPtr policy,uint flags);
    [DllImport("wintrust.dll",ExactSpelling=true)][return:MarshalAs(UnmanagedType.Bool)]static extern bool CryptCATAdminCalcHashFromFileHandle2(IntPtr admin,IntPtr file,ref uint length,[Out]byte[]? hash,uint flags);
    [DllImport("wintrust.dll",ExactSpelling=true)]static extern IntPtr CryptCATAdminEnumCatalogFromHash(IntPtr admin,byte[] hash,uint length,uint flags,IntPtr previous);
    [DllImport("wintrust.dll",CharSet=CharSet.Unicode,ExactSpelling=true)][return:MarshalAs(UnmanagedType.Bool)]static extern bool CryptCATCatalogInfoFromContext(IntPtr catalog,ref CatalogInfo info,uint flags);
    [DllImport("wintrust.dll",ExactSpelling=true)][return:MarshalAs(UnmanagedType.Bool)]static extern bool CryptCATAdminReleaseCatalogContext(IntPtr admin,IntPtr catalog,uint flags);
    [DllImport("wintrust.dll",ExactSpelling=true)][return:MarshalAs(UnmanagedType.Bool)]static extern bool CryptCATAdminReleaseContext(IntPtr admin,uint flags);
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct TrustFile{public uint Size;[MarshalAs(UnmanagedType.LPWStr)]public string Path;public IntPtr Handle,Subject;}
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct TrustData{public uint Size;public IntPtr Policy,Sip;public uint UI,Revocation,Choice;public IntPtr File;public uint StateAction;public IntPtr State;[MarshalAs(UnmanagedType.LPWStr)]public string? Url;public uint Flags,Context;}
    [DllImport("wintrust.dll",ExactSpelling=true)]static extern int WinVerifyTrust(IntPtr window,ref Guid action,ref TrustData data);
    [StructLayout(LayoutKind.Sequential)]struct TrustCertificate{public uint Size;public IntPtr Context;}
    [StructLayout(LayoutKind.Sequential)]struct CertificateContext{public uint Encoding;public IntPtr Encoded;public uint Length;}
    [DllImport("wintrust.dll",ExactSpelling=true)]static extern IntPtr WTHelperProvDataFromStateData(IntPtr state);
    [DllImport("wintrust.dll",ExactSpelling=true)]static extern IntPtr WTHelperGetProvSignerFromChain(IntPtr provider,uint signer,[MarshalAs(UnmanagedType.Bool)]bool counter,uint counterIndex);
    [DllImport("wintrust.dll",ExactSpelling=true)]static extern IntPtr WTHelperGetProvCertFromChain(IntPtr signer,uint certificate);
}
