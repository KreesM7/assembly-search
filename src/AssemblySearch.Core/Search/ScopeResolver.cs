using AssemblySearch.Core.Query;
namespace AssemblySearch.Core.Search;

public static class ScopeResolver
{
    public static string CanonicalPath(string path)
    {
        var full = Path.GetFullPath(path);
        var info = new FileInfo(full);
        return info.Exists && info.LinkTarget != null ? info.ResolveLinkTarget(true)?.FullName ?? full : full;
    }
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
