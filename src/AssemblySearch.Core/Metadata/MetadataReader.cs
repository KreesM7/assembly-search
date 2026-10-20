using System.Globalization;
using System.Security.Cryptography;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using AssemblySearch.Core.Query;
using AssemblySearch.Core.Search;
namespace AssemblySearch.Core.Metadata;
public sealed partial class MetadataReader:IMetadataReader {
 public IEnumerable<SearchCandidate> Read(string path,SearchQuery query,Action<SearchDiagnostic> report,CancellationToken token){
  token.ThrowIfCancellationRequested();
  path=Path.GetFullPath(path);
  if(new FileInfo(path).Length>512L*1024*1024)throw new IOException("Input exceeds the 512 MiB limit.");
  var bytes=File.ReadAllBytes(path);
  using var module=ModuleDefMD.Load(bytes,new ModuleCreationOptions {TryToLoadPdbFromDisk=false});
  module.Location=path;
  TryLoadSymbols(module,path,query,report);
  var identity=new AssemblyIdentity(path,Convert.ToHexStringLower(SHA256.HashData(bytes)),module.Mvid??Guid.Empty);
  if(!query.IncludeFramework&&IsFramework(module))yield break;
  foreach(var c in WithAttributes(module,identity,query,report,token)){
   token.ThrowIfCancellationRequested();
   var requested=query.Mode==QueryMode.Literal?SearchKind.Literal:query.Kind;
   if(!CategoryFilter.Includes(requested,c))continue;
   if(!query.IncludeGenerated&&c.IsGenerated)continue;
   if(query.Scope==SearchScopeKind.Type&&!(c.DeclaringType==query.SelectedType||c.DeclaringType?.StartsWith(query.SelectedType+"/",StringComparison.Ordinal)==true))continue;
   yield return c;
  }
 }
 static bool IsFramework(ModuleDef module){
  var a=module.Assembly;if(a==null)return false;
  string name=a.Name,publicKey=a.PublicKeyToken?.ToString()??"";
  return (name=="mscorlib"||name=="netstandard"||name=="System"||name.StartsWith("System.",StringComparison.Ordinal)||name.StartsWith("Microsoft.",StringComparison.Ordinal))&&new[]{"b77a5c561934e089","7cec85d7bea7798e","b03f5f7f11d50a3a","cc7b13ffcd2ddd51"}.Contains(publicKey);
 }
 static bool Generated(IHasCustomAttribute? owner)=>owner?.CustomAttributes.Any(a=>a.TypeFullName=="System.Runtime.CompilerServices.CompilerGeneratedAttribute")==true;
 static bool Generated(TypeDef? type)=>type!=null&&(Generated((IHasCustomAttribute)type)||Generated(type.DeclaringType));
 static string MethodContext(MethodDef method,uint? offset=null){
  var header=method.FullName+"\nToken: 0x"+method.MDToken.Raw.ToString("X8");
  if(!method.HasBody)return header+"\nNo IL body.";
  var instructions=method.Body.Instructions;
  int target=offset.HasValue?instructions.ToList().FindIndex(i=>i.Offset==offset.Value):0;
  int start=Math.Max(0,target-12),end=Math.Min(instructions.Count,start+80);
  return header+"\n"+string.Join('\n',instructions.Skip(start).Take(end-start).Select(i=>(offset==i.Offset?"> ":"  ")+i))+ (end<instructions.Count?"\n[Context excerpt; additional instructions omitted]":"");
 }
 IEnumerable<SearchCandidate> Enumerate(ModuleDefMD module,AssemblyIdentity id,SearchQuery q,Action<SearchDiagnostic> report,CancellationToken ct){
  SearchCandidate C(SearchKind kind,string name,uint? token,string? type=null,string? member=null,object? value=null,bool literal=false,bool generated=false,string? coordinate=null,uint? offset=null,string? context=null)=>new(id,kind,coordinate??$"{token:X8}:{kind}",name,type,member,token,offset,literal?Convert.ToString(value,CultureInfo.InvariantCulture)??"null":name,value,literal,generated,context??name);
  if(module.Assembly is {} asm)yield return C(SearchKind.AssemblyDef,asm.FullName,asm.MDToken.Raw);
  yield return C(SearchKind.ModuleDef,module.Name,module.MDToken.Raw);
  foreach(var a in module.GetAssemblyRefs())yield return C(SearchKind.AssemblyRef,a.FullName,a.MDToken.Raw);
  foreach(var a in module.GetModuleRefs())yield return C(SearchKind.ModuleRef,a.Name,a.MDToken.Raw);
  if(q.Kind is SearchKind.Any or SearchKind.Resource or SearchKind.Literal||q.Mode==QueryMode.Literal)
   foreach(var resource in module.Resources){ct.ThrowIfCancellationRequested();if(resource is EmbeddedResource embedded){foreach(var c in ResourceReader.Enumerate(embedded,id,q,report,ct))yield return c;}else report(new("resource-unsupported","Linked resources are not followed: "+resource.Name,id.Path));}
  var types=module.GetTypes().Where(t=>!t.IsGlobalModuleType).ToArray();
  foreach(var ns in types.Select(t=>t.Namespace.String).Distinct(StringComparer.Ordinal))yield return C(SearchKind.Namespace,ns,null,coordinate:"namespace:"+ns);
  foreach(var t in types){
   ct.ThrowIfCancellationRequested();string type=t.FullName;bool tg=Generated(t);
   var primary=C(SearchKind.TypeDef,type,t.MDToken.Raw,type,generated:tg,context:t.FullName+"\nBase: "+t.BaseType?.FullName);
   yield return primary;
   yield return primary with {Kind=t.HasGenericParameters?SearchKind.GenericTypeDef:SearchKind.NonGenericTypeDef};
   var subtype=t.IsEnum?SearchKind.EnumTypeDef:t.IsInterface?SearchKind.InterfaceTypeDef:t.BaseType?.FullName is "System.MulticastDelegate" or "System.Delegate"?SearchKind.DelegateTypeDef:t.IsValueType?SearchKind.StructTypeDef:SearchKind.ClassTypeDef;
   yield return primary with {Kind=subtype};
   foreach(var f in t.Fields){
    bool g=tg||Generated(f);var c=C(SearchKind.FieldDef,f.Name,f.MDToken.Raw,type,generated:g,context:f.FullName);yield return c;
    if(f.HasConstant)yield return c with {Kind=SearchKind.Literal,IsLiteral=true,LiteralValue=f.Constant.Value,DisplayName=$"{f.Name} = {f.Constant.Value}",Coordinate=c.Coordinate+":constant"};
   }
   foreach(var p in t.Properties)yield return C(SearchKind.PropertyDef,p.Name,p.MDToken.Raw,type,generated:tg||Generated(p),context:p.FullName);
   foreach(var e in t.Events)yield return C(SearchKind.EventDef,e.Name,e.MDToken.Raw,type,generated:tg||Generated(e),context:e.FullName);
   foreach(var m in t.Methods){
    ct.ThrowIfCancellationRequested();bool g=tg||Generated(m);var context=MethodContext(m);
    var mc=C(SearchKind.MethodDef,m.Name,m.MDToken.Raw,type,m.FullName,generated:g,context:context);yield return mc;
    foreach(var p in m.ParamDefs){
     var pc=C(SearchKind.ParamDef,p.Name,p.MDToken.Raw,type,m.FullName,generated:g,coordinate:$"{m.MDToken.Raw:X8}:param:{p.Sequence}",context:context);yield return pc;
     if(p.HasConstant)yield return pc with{Kind=SearchKind.Literal,IsLiteral=true,LiteralValue=p.Constant.Value,DisplayName=$"{p.Name} = {p.Constant.Value}",Coordinate=pc.Coordinate+":constant"};
    }
    if(!m.HasBody)continue;
    if(q.Kind is SearchKind.Local or SearchKind.ParamLocal or SearchKind.Any){
     var names=SymbolReader.ReadLocals(m,id,report);
     foreach(var local in m.Body.Variables){string name=names.TryGetValue(local.Index,out var debugName)?debugName:"V_"+local.Index;yield return C(SearchKind.Local,name,m.MDToken.Raw,type,m.FullName,generated:g,coordinate:$"{m.MDToken.Raw:X8}:local:{local.Index}",context:context+"\nLocal: "+name+(names.ContainsKey(local.Index)?"":" [synthetic IL identity]"));}
    }
    foreach(var i in m.Body.Instructions){
     ct.ThrowIfCancellationRequested();object? value=null;bool literal=true;
     if(i.IsLdcI4())value=i.GetLdcI4Value();else if(i.OpCode.Code is Code.Ldc_I8 or Code.Ldc_R4 or Code.Ldc_R8 or Code.Ldstr)value=i.Operand;else literal=false;
     if(literal)yield return C(SearchKind.Literal,$"{m.Name}: {value}",m.MDToken.Raw,type,m.FullName,value,true,g,$"{m.MDToken.Raw:X8}:IL_{i.Offset:X4}",i.Offset,MethodContext(m,i.Offset));
    }
   }
  }
 }
 IEnumerable<SearchCandidate> WithAttributes(ModuleDefMD module,AssemblyIdentity id,SearchQuery q,Action<SearchDiagnostic> report,CancellationToken token){
  foreach(var c in Enumerate(module,id,q,report,token)){
   yield return c;
   if(!q.IncludeAttributes||c.IsLiteral||c.Token==null||c.Kind is >SearchKind.Resource or SearchKind.Local or SearchKind.Resource or SearchKind.Namespace)continue;
   var owner=c.Kind==SearchKind.AssemblyDef?module.Assembly:module.ResolveToken(c.Token.Value) as IHasCustomAttribute;
   if(owner!=null)foreach(var a in AttributeReader.Enumerate(owner,c,report))yield return a;
  }
 }
 static void TryLoadSymbols(ModuleDefMD module,string path,SearchQuery query,Action<SearchDiagnostic> report){
  if(query.Kind is not (SearchKind.Local or SearchKind.ParamLocal or SearchKind.Any))return;
  var pdb=Path.ChangeExtension(path,".pdb");
  if(!File.Exists(pdb)){report(new("symbols-missing","No adjacent PDB; local names use synthetic IL identities.",path));return;}
  try{module.LoadPdb(pdb);}catch(Exception e)when(e is IOException or ArgumentException or BadImageFormatException){report(new("symbols-invalid",e.Message,path));}
 }
}
