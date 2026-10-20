using AssemblySearch.Core.Metadata;using AssemblySearch.Core.Query;using AssemblySearch.Core.Search;using Xunit;
namespace AssemblySearch.Core.Tests;
public class SymbolTests {
 [Fact]public void PdbLocalNamesAreSearchable(){Assert.Contains(CategoryTests.Read(SearchKind.Local),r=>r.DisplayName=="localNeedle");Assert.Contains(CategoryTests.Read(SearchKind.ParamLocal),r=>r.MatchText=="input");Assert.DoesNotContain(CategoryTests.Read(SearchKind.ParamLocal),r=>r.MatchText=="Counter");}
 [Fact]public void MissingSymbolsAreExplicit(){var p=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".dll");File.Copy(CategoryTests.Fixture,p);try{var diagnostics=new List<SearchDiagnostic>();var rows=new MetadataReader().Read(p,new("",SearchKind.Local),diagnostics.Add,default).ToArray();Assert.Contains(rows,r=>r.MatchText=="V_0");Assert.Contains(diagnostics,d=>d.Code=="symbols-missing");}finally{File.Delete(p);}}
}
