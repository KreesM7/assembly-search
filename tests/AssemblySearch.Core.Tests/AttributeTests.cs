using AssemblySearch.Core.Metadata;using AssemblySearch.Core.Query;using Xunit;
namespace AssemblySearch.Core.Tests;
public class AttributeTests {
 [Fact]public void AttributeValuesAreOptIn(){var reader=new MetadataReader();Assert.DoesNotContain(reader.Read(CategoryTests.Fixture,new("",SearchKind.Any),_=>{},default),x=>x.MatchText=="attribute needle");Assert.Contains(reader.Read(CategoryTests.Fixture,new("",SearchKind.Any,IncludeAttributes:true),_=>{},default),x=>x.MatchText=="attribute needle"&&x.Coordinate.Contains("attribute"));}
}
