using Cleaner.Core.Models;
using Cleaner.Core.Services;
using Cleaner.Core.Utils;
namespace Cleaner.Core.Cleaners.Developer;
public sealed class JetBrainsCleaner(IFileSystemOperations fs) : CleanupTargetBase(fs)
{
    public override string Id => "dev.jetbrains";
    public override string Name => "JetBrains caches and logs";
    public override string Description => "Old cache, log and index files. Close the IDE first. Local history, settings and older IDE installations are preserved.";
    public override string IconGlyph => "";
    public override CleanupCategory Category => CleanupCategory.Developer;
    public override SafetyLevel SafetyLevel => SafetyLevel.Caution;
    protected override IEnumerable<string> EnumerateCleanupRoots()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JetBrains");
        foreach (var ide in SafeEnumerator.EnumerateDirectories(root))
            foreach (var name in new[] { "caches", "log", "tmp", "index", "indexes" })
                if (Directory.Exists(Path.Combine(ide, name))) yield return Path.Combine(ide, name);
    }
}
