using AssemblySearch.Core.Query;
namespace AssemblySearch.Core.Metadata;
public static class CategoryFilter {
 public static bool Includes(SearchKind requested,SearchCandidate c)=>requested switch {
  SearchKind.Any=>c.Kind is >=SearchKind.AssemblyDef and <=SearchKind.Resource && c.Kind!=SearchKind.ParamLocal,
  SearchKind.Member=>c.Kind is SearchKind.FieldDef or SearchKind.MethodDef or SearchKind.PropertyDef or SearchKind.EventDef,
  SearchKind.ParamLocal=>c.Kind is SearchKind.ParamDef or SearchKind.Local,
  _=>requested==c.Kind};
}
