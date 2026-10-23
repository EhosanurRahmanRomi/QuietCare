using System.Runtime.CompilerServices;
using Cleaner.Core.Models;
using Cleaner.Core.Services;
using Cleaner.Core.Utils;
namespace Cleaner.Core.Cleaners;
public abstract class CleanupTargetBase : ICleanupTarget
{
    readonly ConditionalWeakTable<ScanResult, IReadOnlyList<QuietCareSafety.TempItem>> approvals = new();
    protected CleanupTargetBase(IFileSystemOperations fileSystem) { }
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string IconGlyph { get; }
    public abstract CleanupCategory Category { get; }
    public abstract SafetyLevel SafetyLevel { get; }
    public virtual bool RequiresAdmin => false;
    public virtual bool IsAvailable() => EnumerateCleanupRoots().Any(Directory.Exists);
    protected abstract IEnumerable<string> EnumerateCleanupRoots();
    protected virtual bool PreserveRootDirectories => true;
    protected virtual string FilePattern => "*";
    protected virtual TimeSpan? MinimumAge => null;
    protected virtual IEnumerable<string> ExcludePathSubstrings => [];
    protected virtual bool AdditionalPolicy(string path) => true;
    bool Eligible(string path, DateTime written, string[] roots)
    {
        var age = MinimumAge ?? TimeSpan.Zero;
        if (GlobalCleanupPolicy.MinimumAge is { } global && global > age) age = global;
        return roots.Any(r => QuietCareSafety.SafePaths.Within(r, path))
            && AdditionalPolicy(path)
            && !GlobalCleanupPolicy.IsExcluded(path)
            && !ExcludePathSubstrings.Any(x => path.Contains(x, StringComparison.OrdinalIgnoreCase))
            && System.IO.Enumeration.FileSystemName.MatchesSimpleExpression(FilePattern, Path.GetFileName(path), true)
            && written < DateTime.UtcNow - age;
    }
    protected ScanResult Seal(ScanResult raw, CancellationToken ct = default)
    {
        var roots = EnumerateCleanupRoots().Where(Directory.Exists).Select(Path.GetFullPath).ToArray();
        var stamps = new List<QuietCareSafety.TempItem>(); var errors = raw.Errors.ToList();
        foreach (var path in raw.Paths.Distinct(StringComparer.OrdinalIgnoreCase)) {
            ct.ThrowIfCancellationRequested();
            try {
                var item = RecoveryStore.Capture(path);
                if (Eligible(path, DateTime.FromFileTimeUtc((long)item.Written), roots)) stamps.Add(item);
            } catch (Exception ex) { errors.Add($"Skipped {path}: {ex.Message}"); }
        }
        var result = new ScanResult { TargetId = Id, SizeBytes = stamps.Sum(x => x.Bytes), FileCount = stamps.Count,
            Paths = Array.AsReadOnly(stamps.Select(x => x.Path).ToArray()), Errors = errors.AsReadOnly() };
        approvals.Add(result, stamps.AsReadOnly()); return result;
    }
    public virtual Task<ScanResult> ScanAsync(IProgress<ScanProgress>? progress = null, CancellationToken ct = default) => Task.Run(() => {
        var paths = new List<string>(); long bytes = 0;
        foreach (var root in EnumerateCleanupRoots())
            foreach (var path in SafeEnumerator.EnumerateFiles(root, FilePattern, true)) {
                ct.ThrowIfCancellationRequested();
                if(paths.Count>=200000)throw new IOException("Cleanup scan reached its 200,000-file limit. Choose a smaller target before cleaning.");
                paths.Add(path); bytes += SafeEnumerator.TryGetSize(path);
                if ((paths.Count & 63) == 0) progress?.Report(new ScanProgress(Id, path, bytes, paths.Count));
            }
        var result = Seal(new ScanResult { TargetId = Id, SizeBytes = bytes, FileCount = paths.Count, Paths = paths }, ct);
        progress?.Report(new ScanProgress(Id, string.Empty, result.SizeBytes, result.FileCount)); return result;
    }, ct);
    public virtual Task<CleanResult> CleanAsync(ScanResult scan, bool useRecycleBin, IProgress<CleanProgress>? progress = null, CancellationToken ct = default) => Task.Run(() => {
        if (scan.TargetId != Id || !approvals.TryGetValue(scan, out var items) || DateTimeOffset.UtcNow - scan.ScannedAt > TimeSpan.FromMinutes(10))
            return new CleanResult { TargetId = Id, FreedBytes = 0, FilesDeleted = 0, Errors = ["Scan approval is missing or expired. Scan again."] };
        var roots = EnumerateCleanupRoots().Where(Directory.Exists).Select(Path.GetFullPath).ToArray();
        var errors = new List<string>();var receipts = new List<string>(); long moved = 0; int count = 0;
        foreach (var item in items) {
            if (ct.IsCancellationRequested) break;
            try {
                if (!Eligible(item.Path, DateTime.FromFileTimeUtc((long)item.Written), roots)) throw new IOException("Cleanup policy changed.");
                var receipt=RecoveryStore.Quarantine(item);receipts.Add(receipt.Id); moved += item.Bytes; count++;
            } catch (Exception ex) { errors.Add($"{item.Path}: {ex.Message}"); }
            progress?.Report(new CleanProgress(Id, item.Path, 0, count, items.Count));
        }
        approvals.Remove(scan);
        return new CleanResult { TargetId = Id, FreedBytes = 0, QuarantinedBytes = moved, RecoveryIds=receipts.AsReadOnly(),FilesDeleted = count, Errors = errors.AsReadOnly() };
    }, ct);
}
