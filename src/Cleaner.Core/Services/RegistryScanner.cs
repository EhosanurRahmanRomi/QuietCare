using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace Cleaner.Core.Services;

public enum RegistryIssueCategory
{
    InvalidApplicationPath,
    DeadAutostartEntry,
    DeadUninstaller,
    ObsoleteMuiCache,
    InvalidOpenWith,
    MissingSharedDll,
    InvalidFileExtension,
}

public enum IssueSafety { Safe, Caution, Warning }

public sealed class RegistryIssue
{
    public required string Title { get; init; }
    public required string Detail { get; init; }
    public required string FullKeyPath { get; init; }
    public required string ValueName { get; init; }
    public required RegistryIssueCategory Category { get; init; }
    public required IssueSafety Safety { get; init; }

    public bool IsSelected { get; set; }
}

public sealed class RegistryCleanResult
{
    public int Deleted { get; init; }
    public int Failed { get; init; }
    public required string BackupFilePath { get; init; }
}

public interface IRegistryScanner
{
    Task<IReadOnlyList<RegistryIssue>> ScanAsync(IProgress<string>? progress = null, CancellationToken ct = default);
    Task<RegistryCleanResult> CleanAsync(IReadOnlyList<RegistryIssue> issues, CancellationToken ct = default);
}

public sealed class RegistryScanner : IRegistryScanner
{
    public async Task<IReadOnlyList<RegistryIssue>> ScanAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        // Alle 7 Kategorien parallel — jede schreibt in ihren eigenen Bucket, am Ende mergen.
        var categories = new (string Name, Action<List<RegistryIssue>> Scanner)[]
        {
            ("App Paths",            ScanAppPaths),
            ("Autostart",            ScanAutostartRefs),
            ("Uninstaller",          ScanUninstallEntries),
            ("MUI-Cache",            ScanMuiCache),
            ("OpenWith",             ScanOpenWithList),
            ("Shared DLLs",          ScanSharedDlls),
            ("Datei-Erweiterungen",  ScanFileExtensions),
        };

        var results = new System.Collections.Concurrent.ConcurrentBag<RegistryIssue>();
        var done = 0;
        var total = categories.Length;
        var gate = new object();

        await Parallel.ForEachAsync(
            categories,
            new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = total },
            async (cat, token) =>
            {
                await Task.Run(() =>
                {
                    var local = new List<RegistryIssue>();
                    try { cat.Scanner(local); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Scan {cat.Name}: {ex}"); }
                    foreach (var i in local) results.Add(i);

                    lock (gate)
                    {
                        done++;
                        progress?.Report($"{done}/{total} fertig: {cat.Name} → {local.Count}");
                    }
                }, token);
            });

        return results.ToList();
    }

    // ---- Scanner-Implementierungen ----

    private static void ScanAppPaths(List<RegistryIssue> issues)
    {
        ScanAppPathsHive(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\App Paths", issues);
        ScanAppPathsHive(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\App Paths", issues);
        if (Environment.Is64BitOperatingSystem)
            ScanAppPathsHive(Registry.LocalMachine, @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\App Paths", issues);
    }

    private static void ScanAppPathsHive(RegistryKey root, string path, List<RegistryIssue> issues)
    {
        using var key = root.OpenSubKey(path, writable: false);
        if (key is null) return;

        foreach (var sub in key.GetSubKeyNames())
        {
            using var s = key.OpenSubKey(sub, writable: false);
            if (s is null) continue;
            // Default-Wert ist der exe-Pfad
            var exePath = s.GetValue(null)?.ToString();
            if (string.IsNullOrWhiteSpace(exePath)) continue;
            exePath = exePath.Trim('"');
            if (!File.Exists(exePath))
            {
                issues.Add(new RegistryIssue
                {
                    Title = sub,
                    Detail = $"Verweist auf nicht existierende Datei: {exePath}",
                    FullKeyPath = $@"{root.Name}\{path}\{sub}",
                    ValueName = "", // ganzer Sub-Key
                    Category = RegistryIssueCategory.InvalidApplicationPath,
                    Safety = IssueSafety.Safe,
                });
            }
        }
    }

    private static readonly (RegistryKey Root, string Path)[] AutostartLocations =
    {
        (Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run"),
        (Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\RunOnce"),
        (Registry.CurrentUser,  @"Software\Microsoft\Windows\CurrentVersion\Run"),
        (Registry.CurrentUser,  @"Software\Microsoft\Windows\CurrentVersion\RunOnce"),
    };

    private static void ScanAutostartRefs(List<RegistryIssue> issues)
    {
        foreach (var (root, path) in AutostartLocations)
        {
            using var key = root.OpenSubKey(path, writable: false);
            if (key is null) continue;
            foreach (var name in key.GetValueNames())
            {
                var cmd = key.GetValue(name)?.ToString();
                if (string.IsNullOrWhiteSpace(cmd)) continue;

                var exe = ExtractExe(cmd);
                if (exe is null || File.Exists(exe)) continue;

                issues.Add(new RegistryIssue
                {
                    Title = name,
                    Detail = $"Autostart-Eintrag verweist auf gelöschte Datei: {exe}",
                    FullKeyPath = $@"{root.Name}\{path}",
                    ValueName = name,
                    Category = RegistryIssueCategory.DeadAutostartEntry,
                    Safety = IssueSafety.Safe,
                });
            }
        }
    }

    private static readonly string[] UninstallPaths =
    {
        @"Software\Microsoft\Windows\CurrentVersion\Uninstall",
        @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
    };

    private static void ScanUninstallEntries(List<RegistryIssue> issues)
    {
        foreach (var hive in new[] { Registry.LocalMachine, Registry.CurrentUser })
        {
            foreach (var p in UninstallPaths)
            {
                using var key = hive.OpenSubKey(p, writable: false);
                if (key is null) continue;
                foreach (var sub in key.GetSubKeyNames())
                {
                    using var s = key.OpenSubKey(sub, writable: false);
                    if (s is null) continue;
                    var name = s.GetValue("DisplayName")?.ToString();
                    var uninstallStr = s.GetValue("UninstallString")?.ToString();
                    if (string.IsNullOrWhiteSpace(uninstallStr)) continue;

                    var exe = ExtractExe(uninstallStr);
                    if (exe is null || File.Exists(exe)) continue;
                    // Microsoft-Updates skippen (haben oft msiexec verweise)
                    if (uninstallStr.Contains("msiexec", StringComparison.OrdinalIgnoreCase)) continue;

                    issues.Add(new RegistryIssue
                    {
                        Title = name ?? sub,
                        Detail = $"Deinstaller-Pfad existiert nicht: {exe}",
                        FullKeyPath = $@"{hive.Name}\{p}\{sub}",
                        ValueName = "",
                        Category = RegistryIssueCategory.DeadUninstaller,
                        Safety = IssueSafety.Caution,
                    });
                }
            }
        }
    }

    private static void ScanMuiCache(List<RegistryIssue> issues)
    {
        const string path = @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache";
        using var key = Registry.CurrentUser.OpenSubKey(path, writable: false);
        if (key is null) return;

        foreach (var name in key.GetValueNames())
        {
            // Werte sind oft "C:\path\foo.exe.FriendlyAppName" oder ähnlich.
            // Wir extrahieren den Pfad vorm letzten Punkt-Suffix.
            int lastDot = name.LastIndexOf('.');
            if (lastDot < 5) continue;
            var exePath = name[..lastDot];
            if (!exePath.Contains('\\')) continue;

            if (!File.Exists(exePath))
            {
                issues.Add(new RegistryIssue
                {
                    Title = Path.GetFileName(exePath),
                    Detail = $"MUI-Cache-Eintrag für gelöschte Datei: {exePath}",
                    FullKeyPath = $@"HKEY_CURRENT_USER\{path}",
                    ValueName = name,
                    Category = RegistryIssueCategory.ObsoleteMuiCache,
                    Safety = IssueSafety.Safe,
                });
            }
        }
    }

    private static void ScanOpenWithList(List<RegistryIssue> issues)
    {
        const string root = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts";
        using var key = Registry.CurrentUser.OpenSubKey(root, writable: false);
        if (key is null) return;

        foreach (var ext in key.GetSubKeyNames())
        {
            using var listKey = key.OpenSubKey(ext + @"\OpenWithList", writable: false);
            if (listKey is null) continue;
            foreach (var v in listKey.GetValueNames())
            {
                if (v.Equals("MRUList", StringComparison.OrdinalIgnoreCase)) continue;
                var exe = listKey.GetValue(v)?.ToString();
                if (string.IsNullOrWhiteSpace(exe)) continue;
                // OpenWithList enthält oft nur exe-Namen, nicht volle Pfade — nur scannen wenn ein Pfad drinsteht.
                if (!exe.Contains('\\')) continue;
                if (File.Exists(exe)) continue;

                issues.Add(new RegistryIssue
                {
                    Title = $".{ext} → {Path.GetFileName(exe)}",
                    Detail = $"OpenWith-Eintrag für gelöschte App: {exe}",
                    FullKeyPath = $@"HKEY_CURRENT_USER\{root}\{ext}\OpenWithList",
                    ValueName = v,
                    Category = RegistryIssueCategory.InvalidOpenWith,
                    Safety = IssueSafety.Safe,
                });
            }
        }
    }

    private static void ScanSharedDlls(List<RegistryIssue> issues)
    {
        const string path = @"Software\Microsoft\Windows\CurrentVersion\SharedDLLs";
        using var key = Registry.LocalMachine.OpenSubKey(path, writable: false);
        if (key is null) return;

        foreach (var name in key.GetValueNames())
        {
            if (File.Exists(name)) continue;
            issues.Add(new RegistryIssue
            {
                Title = Path.GetFileName(name),
                Detail = $"SharedDLL-Referenz auf gelöschte Datei: {name}",
                FullKeyPath = $@"HKEY_LOCAL_MACHINE\{path}",
                ValueName = name,
                Category = RegistryIssueCategory.MissingSharedDll,
                Safety = IssueSafety.Safe,
            });
        }
    }

    private static void ScanFileExtensions(List<RegistryIssue> issues)
    {
        // HKCR\.xxx → ProgID. ProgID-Key muss existieren.
        using var classes = Registry.ClassesRoot;
        foreach (var ext in classes.GetSubKeyNames())
        {
            if (!ext.StartsWith(".")) continue;
            using var k = classes.OpenSubKey(ext, writable: false);
            var progId = k?.GetValue(null)?.ToString();
            if (string.IsNullOrWhiteSpace(progId)) continue;
            using var pid = classes.OpenSubKey(progId, writable: false);
            if (pid is not null) continue; // OK

            issues.Add(new RegistryIssue
            {
                Title = ext,
                Detail = $"Datei-Erweiterung verweist auf nicht existierenden ProgID: {progId}",
                FullKeyPath = $@"HKEY_CLASSES_ROOT\{ext}",
                ValueName = "",
                Category = RegistryIssueCategory.InvalidFileExtension,
                Safety = IssueSafety.Caution,
            });
        }
    }

    // ---- Cleanup ----

    public Task<RegistryCleanResult> CleanAsync(IReadOnlyList<RegistryIssue> issues, CancellationToken ct = default)
        => Task.FromResult(new RegistryCleanResult { Deleted = 0, Failed = issues.Count, BackupFilePath = "Registry repairs are unavailable in this preview; scan is read-only." });

    private static string? ExtractExe(string command)
    {
        command = command.Trim();
        if (command.StartsWith("\""))
        {
            int close = command.IndexOf('"', 1);
            if (close > 0) return command[1..close];
        }
        int firstSpace = command.IndexOf(' ');
        return firstSpace > 0 ? command[..firstSpace] : command;
    }
}
