namespace Cleaner.Core.Models;

public sealed class CleanResult
{
    public required string TargetId { get; init; }
    public required long FreedBytes { get; init; }
    public long QuarantinedBytes { get; init; }
    public IReadOnlyList<string> RecoveryIds { get; init; } = Array.Empty<string>();
    public required int FilesDeleted { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public DateTimeOffset CleanedAt { get; init; } = DateTimeOffset.UtcNow;
}
