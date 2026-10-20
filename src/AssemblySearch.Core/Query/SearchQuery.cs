namespace AssemblySearch.Core.Query;

public enum SearchKind
{
    AssemblyDef, ModuleDef, Namespace, TypeDef, FieldDef, MethodDef, PropertyDef, EventDef, ParamDef, Local, ParamLocal, AssemblyRef, ModuleRef, Resource, GenericTypeDef, NonGenericTypeDef, EnumTypeDef, InterfaceTypeDef, ClassTypeDef, StructTypeDef, DelegateTypeDef, Member, Any, Literal
}
public enum QueryMode
{
    Text, Regex, Literal
}
public enum SearchScopeKind
{
    All, Selected, Directory, Type
}
public sealed record SearchQuery(string Text, SearchKind Kind = SearchKind.Any, QueryMode Mode = QueryMode.Text, SearchScopeKind Scope = SearchScopeKind.All, IReadOnlyList<string>? SelectedPaths = null, string? SelectedType = null, bool CaseSensitive = false, bool Exact = false, bool AnyTerm = false, bool IncludeGenerated = false, bool IncludeFramework = true, bool IncludeAttributes = false, bool IncludeResourceContents = true, int Limit = 10000);
public sealed class QueryValidationException(string message) : Exception(message);
