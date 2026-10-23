namespace Cleaner.Core.Utils;
public static class SafeEnumerator
{
    public static bool IsUnlinked(string path)
    {
        try {
            var full = Path.GetFullPath(path);
            if (full.StartsWith(@"\\", StringComparison.Ordinal)) return false;
            for (string? current = full; current != null; current = Path.GetDirectoryName(current))
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return false;
            return true;
        } catch { return false; }
    }
    static string[] Entries(string path, string pattern, bool directories)
    {
        try {
            if (!IsUnlinked(path)) return [];
            return (directories ? Directory.GetDirectories(path, pattern) : Directory.GetFiles(path, pattern)).Where(IsUnlinked).ToArray();
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or ArgumentException) { return []; }
    }
    public static IEnumerable<string> EnumerateFiles(string path, string pattern = "*", bool recursive = false)
    {
        var stack = new Stack<string>(); stack.Push(path);
        while (stack.TryPop(out var current)) {
            foreach (var file in Entries(current, pattern, false)) yield return file;
            if (recursive) foreach (var directory in Entries(current, "*", true)) stack.Push(directory);
        }
    }
    public static IEnumerable<string> EnumerateDirectories(string path) => Entries(path, "*", true);
    public static long TryGetSize(string file) { try { return new FileInfo(file).Length; } catch { return 0; } }
    public static DateTime TryGetLastWrite(string file) { try { return File.GetLastWriteTimeUtc(file); } catch { return DateTime.MinValue; } }
}
