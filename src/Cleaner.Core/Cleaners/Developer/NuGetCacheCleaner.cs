using Cleaner.Core.Models;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Developer;

public sealed class NuGetHttpCacheCleaner : CleanupTargetBase
{
    public NuGetHttpCacheCleaner(IFileSystemOperations fs) : base(fs) { }

    public override string Id => "dev.nuget-http-cache";
    public override string Name => "NuGet HTTP cache";
    public override string Description => "Downloaded package metadata and HTTP caches. A later restore may download them again.";
    public override string IconGlyph => "";
    public override CleanupCategory Category => CleanupCategory.Developer;
    public override SafetyLevel SafetyLevel => SafetyLevel.Safe;

    protected override IEnumerable<string> EnumerateCleanupRoots()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return Path.Combine(local, "NuGet", "v3-cache");
        yield return Path.Combine(local, "NuGet", "plugins-cache");
    }
}

public sealed class NuGetGlobalPackagesCleaner : CleanupTargetBase
{
    public NuGetGlobalPackagesCleaner(IFileSystemOperations fs) : base(fs) { }

    public override string Id => "dev.nuget-global-packages";
    public override string Name => "NuGet package cache";
    public override string Description => "Packages last written over 180 days ago. Use only if the required packages can be downloaded again.";
    public override string IconGlyph => "";
    public override CleanupCategory Category => CleanupCategory.Developer;
    public override SafetyLevel SafetyLevel => SafetyLevel.Caution;

    protected override TimeSpan? MinimumAge => TimeSpan.FromDays(180);

    protected override IEnumerable<string> EnumerateCleanupRoots()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        yield return Path.Combine(profile, ".nuget", "packages");
    }
}
