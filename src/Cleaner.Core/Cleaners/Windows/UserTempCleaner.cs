using Cleaner.Core.Models;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Windows;

public sealed class UserTempCleaner : CleanupTargetBase
{
    public UserTempCleaner(IFileSystemOperations fs) : base(fs) { }

    public override string Id => "system.user-temp";
    public override string Name => "User temporary files";
    public override string Description => "Old temporary files in your local Temp folder. Open, changed and linked files are skipped.";
    public override string IconGlyph => "";
    public override CleanupCategory Category => CleanupCategory.WindowsSystem;
    public override SafetyLevel SafetyLevel => SafetyLevel.Safe;

    // Sehr junge Dateien überspringen (Programme könnten sie gerade benutzen)
    protected override TimeSpan? MinimumAge => TimeSpan.FromHours(1);

    protected override IEnumerable<string> EnumerateCleanupRoots()
    {
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp");
    }
}
