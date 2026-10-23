using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;

namespace Cleaner.Core.Services;

public sealed class WindowsService
{
    public required string ServiceName { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public required ServiceControllerStatus Status { get; init; }
    public required ServiceStartMode StartType { get; init; }
    public string? ImagePath { get; init; }
    public int ProcessId { get; init; }
    public long RamBytes { get; init; }

    public string? Recommendation { get; init; }
    public bool IsMicrosoftService { get; init; }
}

public interface IServiceScanner
{
    Task<IReadOnlyList<WindowsService>> ScanAsync(CancellationToken ct = default);
}

public sealed class ServiceScanner : IServiceScanner
{
    public Task<IReadOnlyList<WindowsService>> ScanAsync(CancellationToken ct = default)
    {
        return Task.Run(IReadOnlyList<WindowsService> () =>
        {
            var list = new List<WindowsService>();
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services", writable: false);
            if (key is null) return list;

            // Process-RAM via Process-Lookup (PID kommt aus WMI)
            var processRam = SnapshotProcessRam();

            foreach (var name in key.GetSubKeyNames())
            {
                if (ct.IsCancellationRequested) break;

                using var sub = key.OpenSubKey(name, writable: false);
                if (sub is null) continue;
                int type = sub.GetValue("Type") is int t ? t : 0;
                // Nur "Service"-Typen (0x10 / 0x20), keine Driver
                if ((type & 0x10) == 0 && (type & 0x20) == 0) continue;

                int startType = sub.GetValue("Start") is int s ? s : -1;
                var imagePath = sub.GetValue("ImagePath")?.ToString();
                var displayName = sub.GetValue("DisplayName")?.ToString() ?? name;
                var description = sub.GetValue("Description")?.ToString();

                ServiceControllerStatus? status = null;
                int pid = 0;
                try
                {
                    using var sc = new ServiceController(name);
                    status = sc.Status;
                }
                catch { /* skip */ }

                bool isMs = imagePath is not null && (
                    imagePath.Contains(@"\System32\", StringComparison.OrdinalIgnoreCase) ||
                    imagePath.Contains(@"\SysWOW64\", StringComparison.OrdinalIgnoreCase));

                long ram = 0;
                if (processRam.TryGetValue(name, out var found)) { pid = found.pid; ram = found.workingSet; }

                list.Add(new WindowsService
                {
                    ServiceName = name,
                    DisplayName = displayName,
                    Description = description,
                    Status = status ?? ServiceControllerStatus.Stopped,
                    StartType = (ServiceStartMode)Math.Max(0, startType),
                    ImagePath = imagePath,
                    ProcessId = pid,
                    RamBytes = ram,
                    Recommendation = BuildRecommendation(name, displayName, status, (ServiceStartMode)Math.Max(0, startType)),
                    IsMicrosoftService = isMs,
                });
            }

            return list
                .OrderByDescending(s => s.RamBytes)
                .ThenBy(s => s.DisplayName)
                .ToList();
        }, ct);
    }

    private static Dictionary<string, (int pid, long workingSet)> SnapshotProcessRam()
    {
        var map = new Dictionary<string, (int, long)>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT Name, ProcessId FROM Win32_Service WHERE State = 'Running'");
            foreach (System.Management.ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString() ?? "";
                var pid = Convert.ToInt32(obj["ProcessId"]);
                if (pid <= 0 || string.IsNullOrEmpty(name)) continue;
                try
                {
                    using var p = Process.GetProcessById(pid);
                    map[name] = (pid, p.WorkingSet64);
                }
                catch { }
            }
        }
        catch { /* WMI evtl. nicht verfügbar */ }
        return map;
    }

    private static string? BuildRecommendation(string serviceName,string displayName, ServiceControllerStatus? status, ServiceStartMode start)=>null;
}
