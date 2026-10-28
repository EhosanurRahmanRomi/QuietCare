using Cleaner.Core.Models;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Apps;

/// <summary>Microsoft Teams Cache (klassisch + neues Teams).</summary>
public sealed class TeamsCacheCleaner : CleanupTargetBase
{
    public TeamsCacheCleaner(IFileSystemOperations fs) : base(fs) { }

    public override string Id => "app.teams-cache";
    public override string Name => "Teams classic cache";
    public override string Description => "Old HTTP, code and GPU caches from classic Teams. New Teams profile data is preserved.";
    public override string IconGlyph => "";
    public override CleanupCategory Category => CleanupCategory.Other;
    public override SafetyLevel SafetyLevel => SafetyLevel.Recommended;

    protected override IEnumerable<string> EnumerateCleanupRoots()
    {
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Teams Classic
        var classic = Path.Combine(roaming, "Microsoft", "Teams");
        foreach (var sub in new[] { "Cache", "GPUCache", "Code Cache", "tmp" })
            yield return Path.Combine(classic, sub);

        // Neues Teams (MSIX-Paket)
        var packages = Path.Combine(local, "Packages");
        var newTeams = Path.Combine(packages, "MSTeams_8wekyb3d8bbwe", "LocalCache");
        // New Teams LocalCache can contain profile data; preserve the entire tree.
    }
}

/// <summary>Slack Cache.</summary>
public sealed class SlackCacheCleaner : CleanupTargetBase
{
    public SlackCacheCleaner(IFileSystemOperations fs) : base(fs) { }

    public override string Id => "app.slack-cache";
    public override string Name => "Slack cache";
    public override string Description => "Old HTTP, code, GPU caches and logs. Offline service-worker data is preserved.";
    public override string IconGlyph => "";
    public override CleanupCategory Category => CleanupCategory.Other;
    public override SafetyLevel SafetyLevel => SafetyLevel.Recommended;

    protected override IEnumerable<string> EnumerateCleanupRoots()
    {
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var slack = Path.Combine(roaming, "Slack");
        foreach (var sub in new[] { "Cache", "GPUCache", "Code Cache", "logs" })
            yield return Path.Combine(slack, sub);
    }
}

/// <summary>Discord Cache.</summary>
public sealed class DiscordCacheCleaner : CleanupTargetBase
{
    public DiscordCacheCleaner(IFileSystemOperations fs) : base(fs) { }

    public override string Id => "app.discord-cache";
    public override string Name => "Discord cache";
    public override string Description => "Old HTTP, code and GPU caches. Account settings are preserved.";
    public override string IconGlyph => "";
    public override CleanupCategory Category => CleanupCategory.Other;
    public override SafetyLevel SafetyLevel => SafetyLevel.Recommended;

    protected override IEnumerable<string> EnumerateCleanupRoots()
    {
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var discord = Path.Combine(roaming, "discord");
        foreach (var sub in new[] { "Cache", "GPUCache", "Code Cache" })
            yield return Path.Combine(discord, sub);
    }
}

/// <summary>Spotify Cache (Storage / Data).</summary>
public sealed class SpotifyCacheCleaner : CleanupTargetBase
{
    public SpotifyCacheCleaner(IFileSystemOperations fs) : base(fs) { }

    public override string Id => "app.spotify-cache";
    public override string Name => "Spotify browser cache";
    public override string Description => "Old browser cache only. Offline music, Storage and Data folders are preserved.";
    public override string IconGlyph => "";
    public override CleanupCategory Category => CleanupCategory.Other;
    public override SafetyLevel SafetyLevel => SafetyLevel.Recommended;

    protected override IEnumerable<string> EnumerateCleanupRoots()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var spotify = Path.Combine(local, "Spotify");


        yield return Path.Combine(spotify, "Browser", "Cache");
    }
}
