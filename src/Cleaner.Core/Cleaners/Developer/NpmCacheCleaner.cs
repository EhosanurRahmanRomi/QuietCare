using Cleaner.Core.Models;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Developer;

public sealed class NpmCacheCleaner : CleanupTargetBase
{
    public NpmCacheCleaner(IFileSystemOperations fs) : base(fs) { }

    public override string Id => "dev.npm-cache";
    public override string Name => "npm cache";
    public override string Description => "Old npm download cache files. A later package install may download them again.";
    public override string IconGlyph => "";
    public override CleanupCategory Category => CleanupCategory.Developer;
    public override SafetyLevel SafetyLevel => SafetyLevel.Safe;

    protected override IEnumerable<string> EnumerateCleanupRoots()
    {
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return Path.Combine(roaming, "npm-cache");
        yield return Path.Combine(local, "npm-cache");

        // pnpm
        yield return Path.Combine(local, "pnpm", "store");

        // yarn classic / berry caches
        yield return Path.Combine(local, "Yarn", "Cache");
    }
}
