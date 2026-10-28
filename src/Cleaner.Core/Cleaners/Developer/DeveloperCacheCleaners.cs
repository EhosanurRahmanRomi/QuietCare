using Cleaner.Core.Models;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Developer;

/// <summary>pip Download-Cache (%LocalAppData%\pip\Cache, %APPDATA%\pip).</summary>
public sealed class PipCacheCleaner : CleanupTargetBase
{
    public PipCacheCleaner(IFileSystemOperations fs) : base(fs) { }

    public override string Id => "dev.pip-cache";
    public override string Name => "Python pip cache";
    public override string Description => "Old downloaded pip packages. A later install may need internet access.";
    public override string IconGlyph => "";
    public override CleanupCategory Category => CleanupCategory.Developer;
    public override SafetyLevel SafetyLevel => SafetyLevel.Safe;

    protected override IEnumerable<string> EnumerateCleanupRoots()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        yield return Path.Combine(local, "pip", "Cache");
        yield return Path.Combine(roaming, "pip", "Cache");
    }
}

/// <summary>Maven lokales Repository (~/.m2/repository).</summary>
public sealed class MavenCacheCleaner : CleanupTargetBase
{
    public MavenCacheCleaner(IFileSystemOperations fs) : base(fs) { }

    public override string Id => "dev.maven-repo";
    public override string Name => "Maven package cache";
    public override string Description => "Local Maven repository files. Review carefully: locally installed artifacts may not be downloadable.";
    public override string IconGlyph => "";
    public override CleanupCategory Category => CleanupCategory.Developer;
    public override SafetyLevel SafetyLevel => SafetyLevel.Recommended;

    protected override IEnumerable<string> EnumerateCleanupRoots()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        yield return Path.Combine(profile, ".m2", "repository");
    }
}

/// <summary>Gradle Caches (~/.gradle/caches).</summary>
public sealed class GradleCacheCleaner : CleanupTargetBase
{
    public GradleCacheCleaner(IFileSystemOperations fs) : base(fs) { }

    public override string Id => "dev.gradle-cache";
    public override string Name => "Gradle cache";
    public override string Description => "Old Gradle caches. Close build daemons first; later builds may download and rebuild data.";
    public override string IconGlyph => "";
    public override CleanupCategory Category => CleanupCategory.Developer;
    public override SafetyLevel SafetyLevel => SafetyLevel.Recommended;

    protected override IEnumerable<string> EnumerateCleanupRoots()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        yield return Path.Combine(profile, ".gradle", "caches");
    }
}

/// <summary>Go Build- und Modul-Cache (%LocalAppData%\go-build, GOPATH/pkg/mod).</summary>
public sealed class GoCacheCleaner : CleanupTargetBase
{
    public GoCacheCleaner(IFileSystemOperations fs) : base(fs) { }

    public override string Id => "dev.go-cache";
    public override string Name => "Go cache";
    public override string Description => "Old build and downloaded module cache files. Later builds may need network access.";
    public override string IconGlyph => "";
    public override CleanupCategory Category => CleanupCategory.Developer;
    public override SafetyLevel SafetyLevel => SafetyLevel.Recommended;

    protected override IEnumerable<string> EnumerateCleanupRoots()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        yield return Path.Combine(local, "go-build");

        // GOPATH (Standard: ~/go) — pkg/mod ist read-only, wird beim Löschen entsperrt.
        var gopath = Environment.GetEnvironmentVariable("GOPATH");
        var root = string.IsNullOrWhiteSpace(gopath) ? Path.Combine(profile, "go") : gopath;
        yield return Path.Combine(root, "pkg", "mod", "cache", "download");
    }
}

/// <summary>Rust/Cargo Registry-Cache (~/.cargo/registry/cache).</summary>
public sealed class CargoCacheCleaner : CleanupTargetBase
{
    public CargoCacheCleaner(IFileSystemOperations fs) : base(fs) { }

    public override string Id => "dev.cargo-cache";
    public override string Name => "Rust Cargo archives";
    public override string Description => "Downloaded crate archives only. Locally edited registry source is preserved.";
    public override string IconGlyph => "";
    public override CleanupCategory Category => CleanupCategory.Developer;
    public override SafetyLevel SafetyLevel => SafetyLevel.Recommended;

    protected override IEnumerable<string> EnumerateCleanupRoots()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        yield return Path.Combine(profile, ".cargo", "registry", "cache");

    }
}
