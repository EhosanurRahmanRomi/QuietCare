using QuietCareSafety;
namespace Cleaner.Core.Services;

public static class RecoveryStore
{
    public static string Root { get => State.DirectoryPath; set => State.DirectoryPath = Path.GetFullPath(value); }
    internal static TempItem Capture(string path)
    {
        path = Path.GetFullPath(path);
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (SafePaths.Within(windows,path) && !SafePaths.Within(Path.Combine(windows,"Temp"),path) && !SafePaths.Within(Path.Combine(windows,"Logs"),path))
            throw new IOException("Windows system files are protected.");
        foreach (var folder in new[] { Environment.SpecialFolder.ProgramFiles,Environment.SpecialFolder.ProgramFilesX86,Environment.SpecialFolder.CommonApplicationData })
        {
            var protectedRoot=Environment.GetFolderPath(folder);
            if (!string.IsNullOrEmpty(protectedRoot) && SafePaths.Within(protectedRoot,path)) throw new IOException("Installed and shared application files are protected.");
        }
        if (SafePaths.Within(Root,path)) throw new IOException("Recovery files cannot be cleaned again.");
        using var parents = SafePaths.LockParents(path);
        using var handle = SafePaths.Open(path, false);
        var info = SafePaths.Inspect(handle, path);
        return new TempItem { Path = Path.GetFullPath(path), Bytes = checked((long)info.Size), Identity = info.Identity, Written = info.Written.Value, Size = Format.Bytes(info.Size) };
    }
    static QuietCareSafety.Cleaner CleanerFor(string original) => new(Path.GetDirectoryName(Path.GetFullPath(original))!, Root, (_, _) => true);
    internal static RecoveryItem Quarantine(TempItem item) => CleanerFor(item.Path).Quarantine(item);
    public static IReadOnlyList<RecoveryItem> List() => new QuietCareSafety.Cleaner(Root, Root).ListRecovery();
    public static void Restore(RecoveryItem item) => CleanerFor(item.Original).Restore(item);
    public static void Purge(RecoveryItem item) => CleanerFor(item.Original).Purge(item);
}
