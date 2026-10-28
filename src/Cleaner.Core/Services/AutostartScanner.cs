using System.Diagnostics;
using System.Globalization;
using Microsoft.Win32;

namespace Cleaner.Core.Services;

public enum AutostartSource
{
    HklmRun,
    HkcuRun,
    HklmRunOnce,
    HkcuRunOnce,
    HklmRun32,
    HkcuRun32,
    StartupFolderUser,
    StartupFolderCommon,
    ScheduledTask,
    Service,
}

public sealed class AutostartEntry
{
    public required string Name { get; init; }
    public required string Command { get; init; }
    public required AutostartSource Source { get; init; }
    public bool IsEnabled { get; set; }
    public string? Publisher { get; init; }
    public string? Description { get; init; }

    /// <summary>Wo der Eintrag gespeichert ist (Registry-Pfad oder Datei-Pfad). Wird zum Löschen/Disable gebraucht.</summary>
    public required string Location { get; init; }
    public bool CanToggle=>Source is AutostartSource.HkcuRun or AutostartSource.HkcuRun32 or AutostartSource.StartupFolderUser;
    public string ManagementHint=>CanToggle?"Enable or disable this sign-in item. A backup is saved before each change.":"Manage this system-wide item in Windows Startup settings, Task Scheduler or Windows Services.";

    public string SourceLabel => Source switch
    {
        AutostartSource.HklmRun           => @"HKLM\...\Run",
        AutostartSource.HkcuRun           => @"HKCU\...\Run",
        AutostartSource.HklmRunOnce       => @"HKLM\...\RunOnce",
        AutostartSource.HkcuRunOnce       => @"HKCU\...\RunOnce",
        AutostartSource.HklmRun32         => @"HKLM\...\Wow6432\Run",
        AutostartSource.HkcuRun32         => @"HKCU\...\Wow6432\Run",
        AutostartSource.StartupFolderUser => "Startup-Ordner (User)",
        AutostartSource.StartupFolderCommon => "Startup-Ordner (Alle)",
        AutostartSource.ScheduledTask     => "Aufgabenplanung",
        AutostartSource.Service           => "Dienst (Automatisch)",
        _ => Source.ToString(),
    };

    // ===== Empfehlung/Info aus der eingebauten Wissensbasis (lazy, einmal berechnet) =====
    private AutostartAssessment? _assessment;
    public AutostartAssessment Assessment => _assessment ??= AutostartAdvisor.Analyze(this);

    public AutostartAdvice Advice => Assessment.Advice;
    public string AdviceInfo => Assessment.Info;

    public string AdviceLabel => Advice switch
    {
        AutostartAdvice.Keep => "Behalten",
        AutostartAdvice.Optional => "Optional",
        AutostartAdvice.Unnecessary => "Unnötig",
        AutostartAdvice.Caution => "Vorsicht",
        _ => "Unbekannt",
    };
}

public interface IAutostartScanner
{
    Task<IReadOnlyList<AutostartEntry>> ScanAsync(CancellationToken ct = default);
    bool ToggleEnabled(AutostartEntry entry, bool enabled);
    bool Delete(AutostartEntry entry);
}

public sealed class AutostartScanner : IAutostartScanner
{
    public Task<IReadOnlyList<AutostartEntry>> ScanAsync(CancellationToken ct = default) => ScanInternalAsync(true,ct);
    public Task<IReadOnlyList<AutostartEntry>> ScanFastAsync(CancellationToken ct = default) => ScanInternalAsync(false,ct);
    private async Task<IReadOnlyList<AutostartEntry>> ScanInternalAsync(bool extended,CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            var list = new List<AutostartEntry>();
            list.AddRange(ScanRegistry(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run",
                AutostartSource.HklmRun));
            list.AddRange(ScanRegistry(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
                AutostartSource.HklmRunOnce));
            list.AddRange(ScanRegistry(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run",
                AutostartSource.HkcuRun));
            list.AddRange(ScanRegistry(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
                AutostartSource.HkcuRunOnce));

            if (Environment.Is64BitOperatingSystem)
            {
                list.AddRange(ScanRegistry(Registry.LocalMachine, @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run",
                    AutostartSource.HklmRun32));
                list.AddRange(ScanRegistry(Registry.CurrentUser, @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run",
                    AutostartSource.HkcuRun32));
            }

            list.AddRange(ScanStartupFolder(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                AutostartSource.StartupFolderUser));
            list.AddRange(ScanStartupFolder(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
                AutostartSource.StartupFolderCommon));

            try { if(extended) list.AddRange(ScanScheduledTasks(ct)); } catch { /* schtasks evtl. nicht da */ }
            try { if(extended) list.AddRange(ScanAutoServices()); } catch { /* fallback */ }

            return (IReadOnlyList<AutostartEntry>)list
                .OrderBy(e => e.Source)
                .ThenBy(e => e.Name)
                .ToList();
        }, ct);
    }

    private static IEnumerable<AutostartEntry> ScanRegistry(RegistryKey root, string subPath, AutostartSource source)
    {
        using var key = root.OpenSubKey(subPath, writable: false);
        if (key is null) yield break;

        // Disabled-Tracking: korrespondierender ApprovedKey
        var approvedPath = StartupApprovalPolicy.SubKey(source);
        using var approved = root.OpenSubKey(approvedPath, writable: false);

        foreach (var name in key.GetValueNames())
        {
            string command = key.GetValue(name)?.ToString() ?? "";
            bool enabled = true;
            if (approved?.GetValue(name) is byte[] data && data.Length > 0)
                enabled = StartupApprovalPolicy.IsEnabled(data); // 0x02 enabled, 0x03 disabled

            yield return new AutostartEntry
            {
                Name = name,
                Command = command,
                Source = source,
                IsEnabled = enabled,
                Location = $@"{root.Name}\{subPath}\{name}",
            };
        }
    }

    private static IEnumerable<AutostartEntry> ScanStartupFolder(string path, AutostartSource source)
    {
        if (!Directory.Exists(path)) yield break;
        using var approved=(source==AutostartSource.StartupFolderCommon?Registry.LocalMachine:Registry.CurrentUser).OpenSubKey(StartupApprovalPolicy.SubKey(source));
        foreach (var file in Utils.SafeEnumerator.EnumerateFiles(path))
        {
            yield return new AutostartEntry
            {
                Name = Path.GetFileNameWithoutExtension(file),
                Command = file,
                Source = source,
                IsEnabled = StartupApprovalPolicy.IsEnabled(approved?.GetValue(Path.GetFileName(file)) as byte[]),
                Location = file,
            };
        }
    }

    private static IEnumerable<AutostartEntry> ScanScheduledTasks(CancellationToken ct)
    {
        // schtasks /query /fo csv /v gibt alle Tasks aus, inkl. Trigger.
        // Wir filtern auf "logon"/"boot"-Trigger und Tasks die nicht von MS sind.
        var psi = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "schtasks.exe"), "/query /fo csv /v")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
        };

        using var p = Process.Start(psi);
        if (p is null) yield break;

        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(30));
        using var cancelRegistration=timeout.Token.Register(()=>{try{if(!p.HasExited)p.Kill();}catch{}});
        var output=p.StandardOutput.ReadToEndAsync(timeout.Token).GetAwaiter().GetResult();
        if(!p.WaitForExit(5000)||p.ExitCode!=0)yield break;
        using var reader=new StringReader(output);
        string? line;
        string[]? headers = null;
        var entries = new List<AutostartEntry>();
        while ((line = reader.ReadLine()) != null)
        {
            if (ct.IsCancellationRequested) break;
            var cols = ParseCsvLine(line);
            if (cols.Length < 5) continue;
            if (headers is null) { headers = cols; continue; }
            if (cols[0] == headers[0]) continue; // Header-Wiederholung

            int idxTask = Array.FindIndex(headers, h => h.Equals("TaskName", StringComparison.OrdinalIgnoreCase));
            int idxState = Array.FindIndex(headers, h => h.Equals("Status", StringComparison.OrdinalIgnoreCase));
            int idxRun = Array.FindIndex(headers, h => h.Equals("Task To Run", StringComparison.OrdinalIgnoreCase) ||
                                                       h.Equals("Auszuführende Aufgabe", StringComparison.OrdinalIgnoreCase));
            int idxAuthor = Array.FindIndex(headers, h => h.Equals("Author", StringComparison.OrdinalIgnoreCase));
            int idxTrigger = Array.FindIndex(headers, h => h.StartsWith("Schedule Type", StringComparison.OrdinalIgnoreCase) ||
                                                            h.StartsWith("Zeitplantyp", StringComparison.OrdinalIgnoreCase));

            if (idxTask < 0 || idxTask >= cols.Length) continue;
            var taskName = cols[idxTask];
            var run = idxRun >= 0 && idxRun < cols.Length ? cols[idxRun] : "";
            var state = idxState >= 0 && idxState < cols.Length ? cols[idxState] : "";
            var author = idxAuthor >= 0 && idxAuthor < cols.Length ? cols[idxAuthor] : "";
            var trigger = idxTrigger >= 0 && idxTrigger < cols.Length ? cols[idxTrigger] : "";

            // Nur Tasks mit Logon/Boot/Onstart-Triggern
            if (!trigger.Contains("Logon", StringComparison.OrdinalIgnoreCase) &&
                !trigger.Contains("Anmeldung", StringComparison.OrdinalIgnoreCase) &&
                !trigger.Contains("Boot", StringComparison.OrdinalIgnoreCase) &&
                !trigger.Contains("Start", StringComparison.OrdinalIgnoreCase))
                continue;

            // Microsoft-Tasks ausblenden — zu viele und User soll die meist nicht anfassen
            if (taskName.StartsWith(@"\Microsoft\", StringComparison.OrdinalIgnoreCase)) continue;

            entries.Add(new AutostartEntry
            {
                Name = taskName.TrimStart('\\'),
                Command = run,
                Source = AutostartSource.ScheduledTask,
                IsEnabled = !state.Contains("Deaktiviert", StringComparison.OrdinalIgnoreCase) &&
                            !state.Equals("Disabled", StringComparison.OrdinalIgnoreCase),
                Publisher = author,
                Location = taskName,
            });
        }
        p.WaitForExit(5000);
        foreach (var e in entries) yield return e;
    }

    private static IEnumerable<AutostartEntry> ScanAutoServices()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services", writable: false);
        if (key is null) yield break;

        foreach (var name in key.GetSubKeyNames())
        {
            using var sub = key.OpenSubKey(name, writable: false);
            if (sub is null) continue;

            int startType = sub.GetValue("Start") is int s ? s : -1;
            // 2 = Auto, 0 = Boot, 1 = System. Wir zeigen nur Auto-Start-Dienste.
            if (startType != 2) continue;

            var displayName = sub.GetValue("DisplayName")?.ToString() ?? name;
            var imagePath = sub.GetValue("ImagePath")?.ToString() ?? "";
            var description = sub.GetValue("Description")?.ToString();

            // Microsoft-eigene Standard-Dienste skippen (Heuristik)
            if (imagePath.Contains(@"\System32\", StringComparison.OrdinalIgnoreCase) ||
                imagePath.Contains(@"\SysWOW64\", StringComparison.OrdinalIgnoreCase))
                continue;

            yield return new AutostartEntry
            {
                Name = displayName,
                Command = imagePath,
                Source = AutostartSource.Service,
                IsEnabled = true,
                Description = description,
                Location = name,
            };
        }
    }

    public bool ToggleEnabled(AutostartEntry entry, bool enabled)
    {
        if(!entry.CanToggle)return false;
        try
        {
            var allUsers=entry.Source is AutostartSource.HklmRun or AutostartSource.HklmRunOnce or AutostartSource.HklmRun32 or AutostartSource.StartupFolderCommon;
            var root=allUsers?Registry.LocalMachine:Registry.CurrentUser;
            bool folder=entry.Source is AutostartSource.StartupFolderUser or AutostartSource.StartupFolderCommon;
            string name=folder?Path.GetFileName(entry.Location):entry.Name;
            if(!folder)
            {
                using var original=root.OpenSubKey(RegPathFor(entry.Source));
                if(!string.Equals(original?.GetValue(entry.Name) as string,entry.Command,StringComparison.Ordinal))return false;
            }
            else if(!File.Exists(entry.Location)||!Utils.SafeEnumerator.IsUnlinked(entry.Location))return false;
            var subKey=StartupApprovalPolicy.SubKey(entry.Source);
            using var approved=root.CreateSubKey(subKey,writable:true);
            var before=approved.GetValue(name) as byte[];
            var backupRoot=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QuietCareNext","StartupBackups");
            Directory.CreateDirectory(backupRoot);
            var backup=System.Text.Json.JsonSerializer.Serialize(new{Utc=DateTimeOffset.UtcNow,Hive=root.Name,SubKey=subKey,Name=name,Original=before,Command=entry.Command,EnabledBefore=entry.IsEnabled});
            File.WriteAllText(Path.Combine(backupRoot,Guid.NewGuid().ToString("N")+".json"),backup);
            approved.SetValue(name,StartupApprovalPolicy.Encode(enabled),RegistryValueKind.Binary);
            return true;
        }
        catch{return false;}
    }

    public bool Delete(AutostartEntry entry) => false; // Use reversible enable/disable, not deletion.

    private static string RegPathFor(AutostartSource s) => s switch
    {
        AutostartSource.HklmRun or AutostartSource.HkcuRun => @"Software\Microsoft\Windows\CurrentVersion\Run",
        AutostartSource.HklmRunOnce or AutostartSource.HkcuRunOnce => @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
        AutostartSource.HklmRun32 or AutostartSource.HkcuRun32 => @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run",
        _ => "",
    };

    private static bool DeleteRegValue(RegistryKey root, string path, string name)
    {
        using var key = root.OpenSubKey(path, writable: true);
        if (key is null) return false;
        key.DeleteValue(name, throwOnMissingValue: false);
        return true;
    }

    private static bool RunSchTasks(string args)
    {
        var psi = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "schtasks.exe"), args)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
        };
        using var p = Process.Start(psi);
        if (p is null) return false;
        p.WaitForExit(5000);
        return p.ExitCode == 0;
    }

    // Minimaler CSV-Parser — schtasks-Output ist quoted und enthält Kommas in Werten
    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var cur = new System.Text.StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(cur.ToString());
                cur.Clear();
            }
            else cur.Append(c);
        }
        fields.Add(cur.ToString());
        return fields.ToArray();
    }
}
