using Microsoft.Extensions.Logging;
namespace Cleaner.Core.Services;
public sealed class FileSystemOperations(ILogger<FileSystemOperations>? logger = null) : IFileSystemOperations
{
    public bool DeleteFile(string path, bool useRecycleBin)
    {
        try { RecoveryStore.Quarantine(RecoveryStore.Capture(path)); return true; }
        catch (Exception ex) { logger?.LogWarning(ex, "File was preserved: {Path}", path); return false; }
    }
    public bool DeleteDirectory(string path, bool useRecycleBin)
    {
        // A folder-level preview is not enough to authorize deleting a changing tree.
        logger?.LogWarning("Directory removal is unavailable in preview: {Path}", path);
        return false;
    }
}
