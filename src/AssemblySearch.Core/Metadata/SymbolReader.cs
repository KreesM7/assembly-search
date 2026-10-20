using dnlib.DotNet;using dnlib.DotNet.Pdb;using AssemblySearch.Core.Search;
namespace AssemblySearch.Core.Metadata;
public static class SymbolReader {
 public static IReadOnlyDictionary<int,string> ReadLocals(MethodDef method,AssemblyIdentity identity,Action<SearchDiagnostic> report){
  var result=new Dictionary<int,string>();
  void Visit(PdbScope? scope){if(scope==null)return;foreach(var v in scope.Variables)result.TryAdd(v.Index,v.Name);foreach(var child in scope.Scopes)Visit(child);}
  Visit(method.Body?.PdbMethod?.Scope);return result;
 }
}
