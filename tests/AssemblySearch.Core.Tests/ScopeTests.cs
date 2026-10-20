using AssemblySearch.Core.Query;using AssemblySearch.Core.Search;using Xunit;
namespace AssemblySearch.Core.Tests;
public class ScopeTests {
 [Theory][InlineData(SearchScopeKind.All,3)][InlineData(SearchScopeKind.Selected,1)][InlineData(SearchScopeKind.Directory,2)]public void ScopesUseLoadedSet(SearchScopeKind scope,int count){var files=new[]{"/one/a.dll","/one/b.dll","/two/c.dll"};Assert.Equal(count,ScopeResolver.Resolve(new(new("x",Scope:scope,SelectedPaths:[files[0]]),files)).Count);}
 [Fact]public void EmptySelectionIsInvalid()=>Assert.Throws<QueryValidationException>(()=>ScopeResolver.Resolve(new(new("x",Scope:SearchScopeKind.Selected),["/a.dll"])));
 [Fact]public void SymlinksDeduplicate(){var target=CategoryTests.Fixture;var link=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".dll");File.CreateSymbolicLink(link,target);try{Assert.Single(ScopeResolver.Resolve(new(new("x"),[link,target])));}finally{File.Delete(link);}}
 [Fact]public void TypeScopeRequiresType()=>Assert.Throws<QueryValidationException>(()=>ScopeResolver.Resolve(new(new("x",Scope:SearchScopeKind.Type),[CategoryTests.Fixture])));
}
