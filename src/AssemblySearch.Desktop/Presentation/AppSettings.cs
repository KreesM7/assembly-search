using AssemblySearch.Core.Query;
namespace AssemblySearch.Desktop.Presentation;

public sealed record AppSettings(SearchKind Kind = SearchKind.Any, QueryMode Mode = QueryMode.Text, SearchScopeKind Scope = SearchScopeKind.All, bool CaseSensitive = false, bool Exact = false, bool AnyTerm = false, bool IncludeGenerated = false, bool IncludeFramework = true, bool IncludeAttributes = false, bool IncludeResourceContents = true, int Limit = 10000, double Width = 1280, double Height = 800);
