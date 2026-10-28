using System.IO;
using System.Text.Json;
using Cleaner.Core.Services;
namespace Cleaner.App.Services;
internal static class FocusNativeCheck
{
    public static int Run(string output)
    {
        Directory.CreateDirectory(output);
        string journalFile=Path.Combine(output,"native-notifications.json");
        if(File.Exists(journalFile))
        {
            var old=JsonSerializer.Deserialize<FocusJournal>(File.ReadAllText(journalFile));
            if(old is null)return 64;
            var errors=new FocusSession(new WindowsFocusRuntime(journalFile),old).Restore();
            File.WriteAllText(Path.Combine(output,"recovery-result.json"),JsonSerializer.Serialize(new{errors,remaining=old.NotificationOwned}));
            return errors.Count==0?0:2;
        }
        var runtime=new WindowsFocusRuntime(journalFile);
        var ledger=new FocusJournal{Session=Guid.NewGuid()};var session=new FocusSession(runtime,ledger);
        var checks=new List<Check>();
        string before=runtime.ReadNotifications();
        try
        {
            string result=session.Quiet();string during=runtime.ReadNotifications();
            bool confirmed=before!=FocusSession.Unrestricted?during==before:during==FocusSession.PriorityOnly;
            checks.Add(new("Windows notification profile request and readback",confirmed,result));
            using(WindowsFocusRuntime.Awake())checks.Add(new("Native display/system keep-awake handle acquired",true,"Request handles acquired successfully."));
        }
        catch(Exception ex){checks.Add(new("Native Focus request completed",false,ex.Message));}
        finally
        {
            var errors=session.Restore();
            checks.Add(new("Previous Windows notification profile restored",errors.Count==0&&runtime.ReadNotifications()==before,string.Join(" ",errors)));
        }
        File.WriteAllText(Path.Combine(output,"focus-native-check.json"),JsonSerializer.Serialize(new{scope="Explicit bounded native Do not disturb round trip and keep-awake handle. No update/Defender policy changes or user-app priority changes.",checks},new JsonSerializerOptions{WriteIndented=true}));
        return checks.Any(x=>!x.passed)?2:0;
    }
    sealed record Check(string name,bool passed,string detail);
}
