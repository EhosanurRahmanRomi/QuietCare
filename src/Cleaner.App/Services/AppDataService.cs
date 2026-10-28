using System.IO;
using System.Text.Json;

namespace Cleaner.App.Services;

/// <summary>
/// Zentraler Zugriff auf den App-Datenordner (%LocalAppData%\PowerClean) und
/// generisches Laden/Speichern von JSON-Dateien. Fundament für Einstellungen,
/// Profile, Schutzregeln und Cleanup-Verlauf.
/// </summary>
public sealed class AppDataService
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public AppDataService(string? root=null)
    {
        RootDir = root is not null?Path.GetFullPath(root):Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QuietCareNext");
        try { Directory.CreateDirectory(RootDir); } catch { /* best effort */ }
    }

    /// <summary>Basis-Ordner für alle persistierten Daten.</summary>
    public string RootDir { get; }

    public string PathFor(string fileName)
    {
        if(string.IsNullOrWhiteSpace(fileName)||Path.GetFileName(fileName)!=fileName)throw new ArgumentException("A local data file name is required.",nameof(fileName));
        return Path.Combine(RootDir,fileName);
    }

    /// <summary>Lädt eine JSON-Datei. Bei Fehler/fehlend wird der Fallback erzeugt.</summary>
    public T Load<T>(string fileName, Func<T> fallback)
    {
        try
        {
            var path = PathFor(fileName);
            if (!File.Exists(path)) return fallback();
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return fallback();
            var info=StartupJsonContext.Default.GetTypeInfo(typeof(T)) as System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>;
            return (info is null?JsonSerializer.Deserialize<T>(json,Json):JsonSerializer.Deserialize(json,info))??fallback();
        }
        catch (Exception ex)
        {
            Cleaner.App.App.LogException("AppData.Load:" + fileName, ex);
            return fallback();
        }
    }

    /// <summary>Speichert ein Objekt als JSON. Fehler werden geloggt, nie geworfen.</summary>
    public void Save<T>(string fileName, T value)
    {
        string? temporary=null;
        try
        {
            var info=StartupJsonContext.Default.GetTypeInfo(typeof(T)) as System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>;
            var json=info is null?JsonSerializer.Serialize(value,Json):JsonSerializer.Serialize(value,info);
            var destination=PathFor(fileName);temporary=destination+"."+Guid.NewGuid().ToString("N")+".tmp";
            using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {var bytes=System.Text.Encoding.UTF8.GetBytes(json);stream.Write(bytes);stream.Flush(true);}
            File.Move(temporary,destination,true);temporary=null;
        }
        catch (Exception ex)
        {
            Cleaner.App.App.LogException("AppData.Save:" + fileName, ex);
        }
        finally{if(temporary is not null)try{File.Delete(temporary);}catch{}}
    }
}
