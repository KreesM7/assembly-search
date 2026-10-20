using AssemblySearch.Core.Query;
using System.Runtime.InteropServices;
namespace AssemblySearch.Core.Search;

public static class ScopeResolver
{
    public static string CanonicalPath(string path)
    {
        var full = Path.GetFullPath(path);
        // realpath resolves every component, including .. after a directory symlink.
        if (OperatingSystem.IsLinux())
        {
            var resolved = RealPath(path, IntPtr.Zero);
            if (resolved == IntPtr.Zero)
                return full; // Preserve missing paths for per-file diagnostics.
            try
            {
                return Marshal.PtrToStringUTF8(resolved) ?? full;
            }
            finally { Free(resolved); }
        }
        var info = new FileInfo(full);
        return info.Exists && info.LinkTarget != null ? info.ResolveLinkTarget(true)?.FullName ?? full : full;
    }
    [DllImport("libc", EntryPoint = "realpath", SetLastError = true)]
    static extern IntPtr RealPath([MarshalAs(UnmanagedType.LPUTF8Str)] string path, IntPtr buffer);
    [DllImport("libc", EntryPoint = "free")]
    static extern void Free(IntPtr buffer);
    public static IReadOnlyList<string> Resolve(SearchRequest request)
    {
        var q = request.Query;
        var files = request.LoadedPaths.Select(CanonicalPath).Distinct(StringComparer.Ordinal).ToArray();
        if (q.Scope == SearchScopeKind.All)
            return files;
        if (q.Scope == SearchScopeKind.Type)
        {
            if (string.IsNullOrWhiteSpace(q.SelectedType))
                throw new QueryValidationException("Select a qualified type for type scope.");
            return q.SelectedPaths is { Count: > 0 } ? SelectFiles() : files;
        }
        var selected = SelectFiles();
        if (q.Scope == SearchScopeKind.Selected)
            return selected;
        var dirs = selected.Select(Path.GetDirectoryName).ToHashSet(StringComparer.Ordinal);
        return files.Where(p => dirs.Contains(Path.GetDirectoryName(p))).ToArray();
        string[] SelectFiles()
        {
            var paths = q.SelectedPaths?.Select(CanonicalPath).Distinct(StringComparer.Ordinal).ToArray() ?? [];
            if (paths.Length == 0 || paths.Any(p => !files.Contains(p, StringComparer.Ordinal)))
                throw new QueryValidationException("Select at least one loaded assembly for this scope.");
            return paths;
        }
    }
}
