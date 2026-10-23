using System.Collections.ObjectModel;
using System.Windows;
using Cleaner.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cleaner.App.ViewModels.Pages;

public sealed partial class SystemMaintenanceViewModel : ObservableObject
{
    private readonly ISystemMaintenanceService _svc;

    public SystemMaintenanceViewModel(ISystemMaintenanceService svc)
    {
        _svc = svc;
        RefreshStatus();
    }

    public ObservableCollection<RestorePoint> RestorePoints { get; } = new();

    [ObservableProperty] private string _log = "Ready. Choose an action.\n";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _hibernationEnabled;
    [ObservableProperty] private long _hibernationFileSize;
    [ObservableProperty] private long _pageFileSize;
    [ObservableProperty] private int _restorePointCount;

    private void Append(string line)
    {
        var ts = DateTime.Now.ToString("HH:mm:ss");
        var indented = string.Join("\n", line.Split('\n').Select(l => "    " + l));
        Log = $"[{ts}] {line.Split('\n')[0]}\n" + (line.Contains('\n') ? indented + "\n" : "") + Log;
    }

    public void RefreshStatus()
    {
        HibernationEnabled = _svc.IsHibernationEnabled();
        HibernationFileSize = _svc.GetHibernationFileSize();
        PageFileSize = _svc.GetPageFileSize();
    }

    [RelayCommand]
    public async Task FlushDnsAsync()
    {
        IsBusy = true;
        Append("DNS-Cache wird geleert...");
        try
        {
            var (rc, output) = await _svc.RunAsync("ipconfig", "/flushdns", elevate: false, CancellationToken.None);
            Append(output);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task RunDismCleanupAsync()
    {
        if (MessageBox.Show("DISM-Component-Cleanup räumt alte Windows-Update-Komponenten weg.\n\n" +
                            "Dauer: typischerweise mehrere Minuten.\n" +
                            "Benötigt Admin-Rechte (UAC-Dialog wird erscheinen).\n\n" +
                            "Fortfahren?",
                "DISM-Cleanup", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        IsBusy = true;
        Append("DISM-Component-Cleanup startet (kann lange dauern)...");
        try
        {
            var (rc, output) = await _svc.RunAsync("dism.exe",
                "/Online /Cleanup-Image /StartComponentCleanup", elevate: true, CancellationToken.None);
            Append(output);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task RunDismResetBaseAsync()
    {
        if (MessageBox.Show("DISM /ResetBase deinstalliert ALLE alten Windows-Update-Versionen permanent.\n\n" +
                            "ACHTUNG: Update-Rollbacks sind danach nicht mehr möglich!\n" +
                            "Spart aber oft mehrere GB.\n\n" +
                            "Fortfahren?",
                "DISM ResetBase", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        IsBusy = true;
        Append("DISM /ResetBase startet (kann sehr lange dauern)...");
        try
        {
            var (rc, output) = await _svc.RunAsync("dism.exe",
                "/Online /Cleanup-Image /StartComponentCleanup /ResetBase", elevate: true, CancellationToken.None);
            Append(output);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task ToggleHibernationAsync()
    {
        var newState = !HibernationEnabled;
        var msg = newState
            ? "Hibernation einschalten?\n\nWird die hiberfil.sys neu anlegen (typisch ~40% RAM = mehrere GB)."
            : $"Hibernation ausschalten?\n\nDie hiberfil.sys ({Cleaner.Core.Utils.ByteFormatter.Format(HibernationFileSize)}) wird gelöscht.";
        if (MessageBox.Show(msg, "Hibernation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        IsBusy = true;
        Append($"powercfg /hibernate {(newState ? "on" : "off")}...");
        try
        {
            var (rc, output) = await _svc.RunAsync("powercfg.exe",
                "/hibernate " + (newState ? "on" : "off"), elevate: true, CancellationToken.None);
            Append(output);
        }
        finally
        {
            IsBusy = false;
            // Werte neu lesen
            await Task.Delay(500);
            RefreshStatus();
        }
    }

    [RelayCommand]
    public async Task LoadRestorePointsAsync()
    {
        IsBusy = true;
        RestorePoints.Clear();
        try
        {
            var pts = await _svc.GetRestorePointsAsync();
            foreach (var p in pts) RestorePoints.Add(p);
            RestorePointCount = pts.Count;
            Append($"{pts.Count} Wiederherstellungspunkte gefunden.");
        }
        catch (Exception ex)
        {
            Append("Fehler: " + ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task DeleteOldRestorePointsAsync()
    {
        Append("Unavailable in preview: this destructive action has not passed validation.");
        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task DeleteAllRestorePointsAsync()
    {
        Append("Unavailable in preview: this destructive action has not passed validation.");
        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task FlushPrintQueueAsync()
    {
        Append("Unavailable in preview: use Windows printer settings to manage the print queue.");
        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task EmptyRecycleBinAsync()
    {
        Append("Unavailable in preview: this destructive action has not passed validation.");
        await Task.CompletedTask;
    }

    [RelayCommand]
    public void OpenSpaceSettings()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:storagesense")
            { UseShellExecute = true });
        }
        catch { }
    }
}
