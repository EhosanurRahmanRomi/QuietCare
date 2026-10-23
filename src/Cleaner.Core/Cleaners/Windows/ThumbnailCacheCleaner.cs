using Cleaner.Core.Models;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Windows;

public sealed class ThumbnailCacheCleaner : CleanupTargetBase
{
    public ThumbnailCacheCleaner(IFileSystemOperations fs) : base(fs) { }

    public override string Id => "system.thumbnail-cache";
    public override string Name => "Thumbnail cache";
    public override string Description => "Old Explorer thumbnail cache files. Windows may rebuild previews after cleanup.";
    public override string IconGlyph => "";
    public override CleanupCategory Category => CleanupCategory.WindowsSystem;
    public override SafetyLevel SafetyLevel => SafetyLevel.Safe;

    protected override string FilePattern => "thumbcache_*.db";

    protected override IEnumerable<string> EnumerateCleanupRoots()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return Path.Combine(local, "Microsoft", "Windows", "Explorer");
    }
}
