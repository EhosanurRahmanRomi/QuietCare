using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace Cleaner.App.ViewModels.Pages;

public sealed partial class ReleaseCheckViewModel : ObservableObject
{
    const string Api="https://api.github.com/repos/EhosanurRahmanRomi/QuietCare/releases?per_page=20";
    public const string Releases="https://github.com/EhosanurRahmanRomi/QuietCare/releases";
    [ObservableProperty] private string _status="Update checks run only when clicked. Downloads and installation are manual in this preview.";
    [ObservableProperty] private bool _busy;
    [RelayCommand] public async Task CheckAsync()
    {
        if (Busy) return; Busy=true;
        try
        {
            using var client=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(15),MaxResponseContentBufferSize=1024*1024};
            client.DefaultRequestHeaders.UserAgent.ParseAdd("QuietCare/"+AppVersion.Current);
            using var response=await client.GetAsync(Api);
            response.EnsureSuccessStatusCode();
            using var document=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var release=document.RootElement.EnumerateArray().FirstOrDefault(x=>!x.GetProperty("draft").GetBoolean());
            Status=release.ValueKind==JsonValueKind.Undefined?"No published releases found.":"Latest published release: "+release.GetProperty("tag_name").GetString()+". This build is "+AppVersion.Current+". Read the release notes before downloading; no files have been downloaded.";
        }
        catch(Exception ex){Status="Update check failed: "+ex.Message;}
        finally{Busy=false;}
    }
    [RelayCommand] public void OpenReleases()
    {
        try{Process.Start(new ProcessStartInfo(Releases){UseShellExecute=true});}
        catch(Exception ex){Status="Could not open releases: "+ex.Message;}
    }
}
