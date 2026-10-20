using AssemblySearch.Core.Metadata;
using AssemblySearch.Core.Query;
using AssemblySearch.Core.Search;
using Xunit;
namespace AssemblySearch.Core.Tests;

public class CategoryTests
{
    public static string Fixture => Path.Combine(AppContext.BaseDirectory, "SearchFixture.dll");
    public static SearchCandidate[] Read(SearchKind kind, bool generated = false, string? path = null) => new MetadataReader().Read(path ?? Fixture, new("", kind, IncludeGenerated: generated), _ => { }, default).ToArray();
    [Theory]
    [InlineData(SearchKind.AssemblyDef, "SearchFixture")]
    [InlineData(SearchKind.ModuleDef, "SearchFixture.dll")]
    [InlineData(SearchKind.Namespace, "Sample")]
    [InlineData(SearchKind.TypeDef, "Widget")]
    [InlineData(SearchKind.FieldDef, "Counter")]
    [InlineData(SearchKind.MethodDef, "Read")]
    [InlineData(SearchKind.PropertyDef, "Title")]
    [InlineData(SearchKind.EventDef, "Changed")]
    [InlineData(SearchKind.ParamDef, "input")]
    [InlineData(SearchKind.AssemblyRef, "ReferenceFixture")]
    [InlineData(SearchKind.ModuleRef, "fixture-native")]
    [InlineData(SearchKind.GenericTypeDef, "Box")]
    [InlineData(SearchKind.NonGenericTypeDef, "Widget")]
    [InlineData(SearchKind.EnumTypeDef, "Choice")]
    [InlineData(SearchKind.InterfaceTypeDef, "IReadable")]
    [InlineData(SearchKind.ClassTypeDef, "Widget")]
    [InlineData(SearchKind.StructTypeDef, "Point")]
    [InlineData(SearchKind.DelegateTypeDef, "ChangedHandler")]
    [InlineData(SearchKind.Member, "Read")]
    [InlineData(SearchKind.Any, "Widget")]
    [InlineData(SearchKind.Literal, "body literal needle")]
    public void EachCategoryHasExpectedCandidate(SearchKind kind, string name)
    {
        var rows = Read(kind);
        if (kind == SearchKind.Member)
            Assert.All(rows, r => Assert.Contains(r.Kind, new[] { SearchKind.FieldDef, SearchKind.MethodDef, SearchKind.PropertyDef, SearchKind.EventDef }));
        else if (kind == SearchKind.Any)
            Assert.All(rows, r => Assert.InRange((int)r.Kind, 0, (int)SearchKind.Resource));
        else
            Assert.All(rows, r => Assert.Equal(kind, r.Kind));
        Assert.Contains(rows, r => r.DisplayName.Contains(name));
        Assert.DoesNotContain(rows, r => r.DisplayName == "NeverExists");
        Assert.All(rows, r => Assert.NotEqual(Guid.Empty, r.Assembly.Mvid));
    }
    [Fact]
    public void TypeCategoriesExcludeOtherKinds()
    {
        Assert.DoesNotContain(Read(SearchKind.ClassTypeDef), x => x.DisplayName.Contains("Choice") || x.DisplayName.Contains("ChangedHandler"));
        Assert.DoesNotContain(Read(SearchKind.GenericTypeDef), x => x.DisplayName.Contains("Widget"));
        Assert.DoesNotContain(Read(SearchKind.StructTypeDef), x => x.DisplayName.Contains("Choice"));
    }
    [Fact]
    public void GeneratedMembersAreOptIn()
    {
        Assert.DoesNotContain(Read(SearchKind.MethodDef), r => r.DisplayName.Contains("GeneratedNeedle"));
        Assert.Contains(Read(SearchKind.MethodDef, true), r => r.DisplayName.Contains("GeneratedNeedle"));
    }
    [Fact]
    public void OverloadsHaveDistinctTokens()
    {
        var rows = Read(SearchKind.MethodDef).Where(r => r.MatchText == "Read" && r.DeclaringType == "Sample.Widget").ToArray();
        Assert.Equal(2, rows.Length);
        Assert.Equal(2, rows.Select(r => r.Token).Distinct().Count());
    }
    [Fact]
    public void LiteralsIncludeCompactInstructionsAndDefaults()
    {
        var rows = Read(SearchKind.Literal);
        Assert.Contains(rows, r => Equals(r.LiteralValue, 42));
        Assert.Contains(rows, r => Equals(r.LiteralValue, 7));
        Assert.Contains(rows, r => Equals(r.LiteralValue, long.MinValue));
        Assert.Contains(rows, r => r.IlOffset != null && Equals(r.LiteralValue, 1.5));
    }
    [Fact]
    public void DoesNotExecuteStaticConstructor()
    {
        var marker = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".marker");
        Environment.SetEnvironmentVariable("ASSEMBLY_SEARCH_FIXTURE_MARKER", marker);
        try
        {
            Assert.NotEmpty(Read(SearchKind.Any));
            Assert.False(File.Exists(marker));
        }
        finally { Environment.SetEnvironmentVariable("ASSEMBLY_SEARCH_FIXTURE_MARKER", null); }
    }
    [Fact]
    public void RejectsBadInputs()
    {
        Assert.ThrowsAny<Exception>(() => Read(SearchKind.Any, path: Fixture + ".absent"));
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "not a PE file");
            Assert.ThrowsAny<Exception>(() => Read(SearchKind.Any, path: path));
        }
        finally { File.Delete(path); }
    }
}
