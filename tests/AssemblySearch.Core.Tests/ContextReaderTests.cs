using AssemblySearch.Core.Metadata;using AssemblySearch.Core.Query;using Xunit;
namespace AssemblySearch.Core.Tests;
public class ContextReaderTests {
 [Fact]public void ContextHasMethodInstructions(){var r=CategoryTests.Read(SearchKind.MethodDef).First(x=>x.MatchText=="Read");Assert.Contains("IL_",ContextReader.Read(r));}
 [Fact]public void ReplacedFileCannotGiveStaleContext(){var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".dll");File.Copy(CategoryTests.Fixture,path);try{var row=CategoryTests.Read(SearchKind.MethodDef,path:path).First();File.WriteAllText(path,"changed");Assert.Throws<IOException>(()=>ContextReader.Read(row));}finally{File.Delete(path);}}
}
