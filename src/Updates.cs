using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace QuietCare {
public static class AppInfo {
    public const string VersionText="0.3.0",Repository="EhosanurRahmanRomi/QuietCare";
    public const string ReleasesUrl="https://github.com/"+Repository+"/releases";
}

public sealed class ReleaseVersion : IComparable<ReleaseVersion> {
    public Version Number {get;private set;}
    public string Preview {get;private set;}
    public string Tag {get;private set;}
    public static bool TryParse(string tag,out ReleaseVersion value){
        value=null;if(tag==null||tag.Length>100)return false;
        var match=Regex.Match(tag,@"\Av?(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*))?(?:\+[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?\z",RegexOptions.CultureInvariant);
        int major,minor,patch;if(!match.Success||!Int32.TryParse(match.Groups[1].Value,out major)||!Int32.TryParse(match.Groups[2].Value,out minor)||!Int32.TryParse(match.Groups[3].Value,out patch))return false;
        string preview=match.Groups[4].Value;
        if(preview.Split('.').Any(x=>x.Length>1&&x.All(Char.IsDigit)&&x[0]=='0'))return false;
        value=new ReleaseVersion{Number=new Version(major,minor,patch),Preview=preview,Tag=tag};return true;
    }
    public int CompareTo(ReleaseVersion other){
        if(other==null)return 1;int number=Number.CompareTo(other.Number);if(number!=0)return number;
        if(Preview==other.Preview)return 0;if(Preview.Length==0)return 1;if(other.Preview.Length==0)return -1;
        string[] left=Preview.Split('.'),right=other.Preview.Split('.');
        for(int i=0;i<Math.Min(left.Length,right.Length);i++){
            bool ln=left[i].All(Char.IsDigit),rn=right[i].All(Char.IsDigit);int difference;
            if(ln&&rn){difference=left[i].Length.CompareTo(right[i].Length);if(difference==0)difference=String.CompareOrdinal(left[i],right[i]);}
            else if(ln!=rn)difference=ln?-1:1;else difference=String.CompareOrdinal(left[i],right[i]);
            if(difference!=0)return difference;
        }
        return left.Length.CompareTo(right.Length);
    }
}

public sealed class UpdateRelease {
    public ReleaseVersion Version;public string Title,Notes,PageUrl;public bool Preview;
    public UpdateAsset Installer;
}
public sealed class UpdateAsset {
    public string Url,Sha256;public long Size;
}
public sealed class VerifiedInstaller {
    public string Path {get;private set;}public UpdateAsset Asset {get;private set;}
    internal VerifiedInstaller(string path,UpdateAsset asset){Path=path;Asset=new UpdateAsset{Url=asset.Url,Sha256=asset.Sha256,Size=asset.Size};}
}

public static class UpdatePolicy {
    public const int FeedLimit=1024*1024;
    public const long InstallerLimit=64L*1024*1024;
    public static bool Due(bool enabled,DateTime lastAttempt,DateTime now){return enabled&&(lastAttempt==DateTime.MinValue||lastAttempt>now.AddDays(1)||now-lastAttempt>=TimeSpan.FromDays(7));}
    public static string Page(string tag){ReleaseVersion parsed;if(!ReleaseVersion.TryParse(tag,out parsed))throw new InvalidDataException("Invalid release version.");return AppInfo.ReleasesUrl+"/tag/"+Uri.EscapeDataString(tag);}
    public static bool ExactUrl(string value,string expected){Uri uri;return Uri.TryCreate(value,UriKind.Absolute,out uri)&&uri.Scheme=="https"&&uri.Port==443&&uri.UserInfo.Length==0&&uri.Query.Length==0&&uri.Fragment.Length==0&&String.Equals(uri.AbsoluteUri,expected,StringComparison.Ordinal);}
    public static bool DownloadHost(Uri uri){return uri!=null&&uri.IsAbsoluteUri&&uri.Scheme=="https"&&uri.Port==443&&uri.UserInfo.Length==0&&uri.Fragment.Length==0&&new[]{"release-assets.githubusercontent.com","objects.githubusercontent.com","github-releases.githubusercontent.com"}.Contains(uri.Host,StringComparer.OrdinalIgnoreCase);}
    public static void ValidateAsset(UpdateRelease release){
        if(release==null||release.Version==null||!ExactUrl(release.PageUrl,Page(release.Version.Tag)))throw new InvalidDataException("Release information could not be verified.");
        var asset=release.Installer;string expected="https://github.com/"+AppInfo.Repository+"/releases/download/"+Uri.EscapeDataString(release.Version.Tag)+"/QuietCare-Setup.exe";
        if(asset==null||asset.Size<2||asset.Size>InstallerLimit||!Regex.IsMatch(asset.Sha256??"",@"\A[0-9a-fA-F]{64}\z")||!ExactUrl(asset.Url,expected))throw new InvalidDataException("This release has no supported installer with a SHA-256 digest. Open its release page instead.");
    }
    static string Text(IDictionary<string,object> item,string key,int limit){object value;if(!item.TryGetValue(key,out value)||value==null)return "";var result=value as string;if(result==null)throw new InvalidDataException("Invalid release metadata.");return result.Length<=limit?result:result.Substring(0,limit);}
    static bool Flag(IDictionary<string,object> item,string key){object value;return item.TryGetValue(key,out value)&&value is bool&&(bool)value;}
    public static List<UpdateRelease> Parse(string json,bool includePreview){
        if(json==null||Encoding.UTF8.GetByteCount(json)>FeedLimit)throw new InvalidDataException("Release information exceeded the size limit.");
        object data;try{data=new JavaScriptSerializer{MaxJsonLength=FeedLimit,RecursionLimit=32}.DeserializeObject(json);}catch(ArgumentException){throw new InvalidDataException("GitHub returned invalid release information.");}
        var single=data as IDictionary<string,object>;var array=data as object[];
        if(array==null){if(single==null||!single.ContainsKey("tag_name"))throw new InvalidDataException("GitHub returned invalid release information.");array=new object[]{single};}
        var releases=new List<UpdateRelease>();
        foreach(var entry in array){
            var item=entry as IDictionary<string,object>;if(item==null||Flag(item,"draft"))continue;
            ReleaseVersion version;if(!ReleaseVersion.TryParse(Text(item,"tag_name",101),out version))continue;
            bool preview=Flag(item,"prerelease")||version.Preview.Length!=0;if(preview&&!includePreview)continue;
            string page=Text(item,"html_url",512);if(!ExactUrl(page,Page(version.Tag)))continue;
            var release=new UpdateRelease{Version=version,Preview=preview,Title=Text(item,"name",160),Notes=Text(item,"body",12000),PageUrl=page};
            object assets;var list=item.TryGetValue("assets",out assets)?assets as object[]:null;
            if(list!=null)foreach(var obj in list){
                var asset=obj as IDictionary<string,object>;if(asset==null||Text(asset,"name",256)!="QuietCare-Setup.exe")continue;
                long size;object raw;if(!asset.TryGetValue("size",out raw)||!Int64.TryParse(Convert.ToString(raw,CultureInfo.InvariantCulture),NumberStyles.None,CultureInfo.InvariantCulture,out size))continue;
                string digest=Text(asset,"digest",128);release.Installer=new UpdateAsset{Url=Text(asset,"browser_download_url",1024),Size=size,Sha256=digest.StartsWith("sha256:",StringComparison.Ordinal)?digest.Substring(7):""};
                try{ValidateAsset(release);}catch(InvalidDataException){release.Installer=null;}break;
            }
            releases.Add(release);
        }
        return releases.OrderByDescending(x=>x.Version).ToList();
    }
}

public interface IUpdateService {
    Task<UpdateRelease> Check(bool includePreview,CancellationToken cancel);
    Task<VerifiedInstaller> Download(UpdateRelease release,IProgress<int> progress,CancellationToken cancel);
}

public sealed class GitHubUpdates : IUpdateService,IDisposable {
    readonly HttpClient client;
    public GitHubUpdates(HttpMessageHandler handler=null){
        client=new HttpClient(handler??new HttpClientHandler{AllowAutoRedirect=false,UseCookies=false,AutomaticDecompression=DecompressionMethods.GZip|DecompressionMethods.Deflate,SslProtocols=SslProtocols.Tls12});
        client.Timeout=Timeout.InfiniteTimeSpan;client.DefaultRequestHeaders.UserAgent.ParseAdd("QuietCare/"+AppInfo.VersionText);client.DefaultRequestHeaders.Add("X-GitHub-Api-Version","2022-11-28");
    }
    static void Success(HttpResponseMessage response){
        if(response.StatusCode==HttpStatusCode.Forbidden||(int)response.StatusCode==429)throw new HttpRequestException("GitHub is limiting update requests. Try again later.");
        if(!response.IsSuccessStatusCode)throw new HttpRequestException("GitHub could not complete the update request (HTTP "+(int)response.StatusCode+").");
    }
    public async Task<UpdateRelease> Check(bool includePreview,CancellationToken cancel){
        string path="https://api.github.com/repos/"+AppInfo.Repository+(includePreview?"/releases?per_page=30":"/releases/latest");
        try{return await ReadRelease(path,!includePreview,includePreview,cancel).ConfigureAwait(false);}
        catch(HttpRequestException){cancel.ThrowIfCancellationRequested();}
        return await ReadRelease("https://raw.githubusercontent.com/"+AppInfo.Repository+"/main/updates/"+(includePreview?"preview":"stable")+".json",false,includePreview,cancel).ConfigureAwait(false);
    }
    async Task<UpdateRelease> ReadRelease(string path,bool stableNotFound,bool includePreview,CancellationToken cancel){
        using(var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancel)){
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            using(var request=new HttpRequestMessage(HttpMethod.Get,path)){
                request.Headers.Accept.ParseAdd("application/vnd.github+json");
                using(var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,deadline.Token).ConfigureAwait(false)){
                    if(stableNotFound&&response.StatusCode==HttpStatusCode.NotFound)return null;Success(response);
                    if(response.Content==null)throw new InvalidDataException("GitHub returned an empty response.");
                    var length=response.Content.Headers.ContentLength;if(length.HasValue&&length>UpdatePolicy.FeedLimit)throw new InvalidDataException("Release information exceeded the size limit.");
                    using(var input=await response.Content.ReadAsStreamAsync().ConfigureAwait(false))using(var memory=new MemoryStream()){
                        byte[] buffer=new byte[16384];int read;
                        while((read=await input.ReadAsync(buffer,0,buffer.Length,deadline.Token).ConfigureAwait(false))!=0){if(memory.Length+read>UpdatePolicy.FeedLimit)throw new InvalidDataException("Release information exceeded the size limit.");memory.Write(buffer,0,read);}
                        string json;try{json=new UTF8Encoding(false,true).GetString(memory.ToArray());}catch(DecoderFallbackException){throw new InvalidDataException("Release information is not valid UTF-8.");}
                        return UpdatePolicy.Parse(json,includePreview).FirstOrDefault();
                    }
                }
            }
        }
    }
    internal static string CachePath(UpdateAsset asset){if(asset==null||!Regex.IsMatch(asset.Sha256??"",@"\A[0-9a-fA-F]{64}\z")||asset.Size<2||asset.Size>UpdatePolicy.InstallerLimit)throw new InvalidDataException("Invalid installer identity.");return Path.Combine(State.FilePath("Updates"),asset.Sha256.ToLowerInvariant().Substring(0,24),"QuietCare-Setup.exe");}
    internal static FileStream ZoneStream(string path,bool write){var handle=Native.CreateFile(path+":Zone.Identifier",write?0x40000000u:0x80000000u,1,IntPtr.Zero,write?2u:3u,0,IntPtr.Zero);if(handle.IsInvalid){handle.Dispose();throw new System.ComponentModel.Win32Exception();}return new FileStream(handle,write?FileAccess.Write:FileAccess.Read);}
    static void MarkInternet(string path){using(var stream=ZoneStream(path,true)){byte[] text=Encoding.ASCII.GetBytes("[ZoneTransfer]\r\nZoneId=3\r\nHostUrl="+AppInfo.ReleasesUrl+"\r\n");stream.Write(text,0,text.Length);}}
    internal static string InternetMark(string path){using(var stream=ZoneStream(path,false))using(var reader=new StreamReader(stream,Encoding.ASCII))return reader.ReadToEnd();}
    public async Task<VerifiedInstaller> Download(UpdateRelease release,IProgress<int> progress,CancellationToken cancel){
        UpdatePolicy.ValidateAsset(release);var asset=new UpdateAsset{Url=release.Installer.Url,Sha256=release.Installer.Sha256,Size=release.Installer.Size};
        string target=CachePath(asset);Directory.CreateDirectory(Path.GetDirectoryName(target));
        using(var locks=SafePaths.LockParents(target)){
            if(File.Exists(target)){VerifyFile(target,asset,cancel);MarkInternet(target);return new VerifiedInstaller(target,asset);}
            string stage=Path.Combine(Path.GetDirectoryName(target),Guid.NewGuid().ToString("N").Substring(0,16)+".download");
            try{
                using(var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancel)){
                    deadline.CancelAfter(TimeSpan.FromMinutes(3));Uri next=new Uri(asset.Url);
                    for(int redirects=0;;redirects++){
                        using(var request=new HttpRequestMessage(HttpMethod.Get,next))using(var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,deadline.Token).ConfigureAwait(false)){
                            int code=(int)response.StatusCode;
                            if(code==301||code==302||code==303||code==307||code==308){
                                Uri location=response.Headers.Location;Uri destination=location==null?null:new Uri(next,location);
                                if(redirects>=4||!UpdatePolicy.DownloadHost(destination))throw new InvalidDataException("The installer redirected outside the approved GitHub download hosts.");next=destination;continue;
                            }
                            Success(response);if(response.Content==null)throw new InvalidDataException("Installer download was empty.");
                            long? declared=response.Content.Headers.ContentLength;if(declared.HasValue&&declared.Value!=asset.Size)throw new InvalidDataException("Installer download size did not match the release.");
                            using(var input=await response.Content.ReadAsStreamAsync().ConfigureAwait(false))using(var file=new FileStream(stage,FileMode.CreateNew,FileAccess.Write,FileShare.None))using(var hash=SHA256.Create()){
                                byte[] buffer=new byte[65536];long total=0;int read;
                                while((read=await input.ReadAsync(buffer,0,buffer.Length,deadline.Token).ConfigureAwait(false))!=0){total+=read;if(total>asset.Size||total>UpdatePolicy.InstallerLimit)throw new InvalidDataException("Installer download exceeded its expected size.");await file.WriteAsync(buffer,0,read,deadline.Token).ConfigureAwait(false);hash.TransformBlock(buffer,0,read,buffer,0);if(progress!=null)progress.Report((int)(total*100/asset.Size));}
                                hash.TransformFinalBlock(new byte[0],0,0);string digest=BitConverter.ToString(hash.Hash).Replace("-","");
                                if(total!=asset.Size||!String.Equals(digest,asset.Sha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Installer integrity verification failed. No installer was opened.");
                                file.Flush(true);
                            }
                            deadline.Token.ThrowIfCancellationRequested();MarkInternet(stage);File.Move(stage,target);VerifyFile(target,asset,deadline.Token);return new VerifiedInstaller(target,asset);
                        }
                    }
                }
            }finally{if(File.Exists(stage))try{File.Delete(stage);}catch(IOException){}catch(UnauthorizedAccessException){}}
        }
    }
    static FileStream VerifiedStream(string path,UpdateAsset asset){
        if(!String.Equals(Path.GetFullPath(path),Path.GetFullPath(CachePath(asset)),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Installer is outside the verified update cache.");
        var handle=Native.CreateFile(path,0x80000080,1,IntPtr.Zero,3,0x00200000,IntPtr.Zero);
        try{var info=SafePaths.Inspect(handle,path);long size=((long)info.SizeHigh<<32)|info.SizeLow;if(size!=asset.Size)throw new InvalidDataException("Cached installer size changed.");return new FileStream(handle,FileAccess.Read);}catch{handle.Dispose();throw;}
    }
    static void VerifyStream(FileStream stream,UpdateAsset asset,CancellationToken cancel){
        using(var hash=SHA256.Create()){byte[] buffer=new byte[65536];int read;while((read=stream.Read(buffer,0,buffer.Length))!=0){cancel.ThrowIfCancellationRequested();hash.TransformBlock(buffer,0,read,buffer,0);}hash.TransformFinalBlock(new byte[0],0,0);if(!String.Equals(BitConverter.ToString(hash.Hash).Replace("-",""),asset.Sha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Cached installer integrity verification failed. Download was not opened.");}
    }
    internal static void VerifyFile(string path,UpdateAsset asset,CancellationToken cancel){using(var file=VerifiedStream(path,asset))VerifyStream(file,asset,cancel);}
    internal static void UseVerifiedInstaller(VerifiedInstaller installer,Action<string> launch){
        if(installer==null)throw new InvalidOperationException("Download and verify an installer first.");
        using(var locks=SafePaths.LockParents(installer.Path))using(var stream=VerifiedStream(installer.Path,installer.Asset)){VerifyStream(stream,installer.Asset,CancellationToken.None);string zone=InternetMark(installer.Path);if(!zone.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).Contains("ZoneId=3"))throw new InvalidDataException("The downloaded installer's Internet zone mark is missing. Download it again.");launch(installer.Path);}
    }
    public static void OpenInstaller(VerifiedInstaller installer){UseVerifiedInstaller(installer,path=>Process.Start(new ProcessStartInfo(path){UseShellExecute=true}));}
    public static void OpenReleases(UpdateRelease release=null){string url=release==null?AppInfo.ReleasesUrl:UpdatePolicy.Page(release.Version.Tag);Process.Start(new ProcessStartInfo(url){UseShellExecute=true});}
    public void Dispose(){client.Dispose();}
}
}
