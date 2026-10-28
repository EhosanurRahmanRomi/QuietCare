using System.Collections.ObjectModel;
using System.Windows;
using Cleaner.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuietCareSafety;
namespace Cleaner.App.ViewModels.Pages;

public sealed partial class RecoveryViewModel : ObservableObject
{
    public ObservableCollection<RecoveryItem> Files { get; } = new();
    [ObservableProperty] private RecoveryItem? _selected;
    [ObservableProperty] private string _status = "Quarantine preserves files on the same drive. Disk space is freed only after permanent deletion.";
    public RecoveryViewModel() => Refresh();
    [RelayCommand] public void Refresh()
    {
        try { Files.Clear(); foreach (var item in RecoveryStore.List()) Files.Add(item); }
        catch (Exception ex) { Status = "Could not read recovery records: " + ex.Message; }
    }
    [RelayCommand] public void Restore()
    {
        if (Selected is not { } item) return;
        try { RecoveryStore.Restore(item); Files.Remove(item); Status = "Restored: " + item.Original; }
        catch (Exception ex) { Status = "File preserved: " + ex.Message; }
    }
    [RelayCommand] public void Purge()
    {
        if (Selected is not { } item) return;
        if (MessageBox.Show("Permanently delete this quarantined file? It cannot be restored through QuietCare afterward.\n\n" + item.Original,
            "Permanent deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        try { RecoveryStore.Purge(item); Files.Remove(item); Status = "Permanently deleted " + item.Size + ". Windows may take time to update free-space readings."; }
        catch (Exception ex) { Status = "File preserved: " + ex.Message; }
    }
}
