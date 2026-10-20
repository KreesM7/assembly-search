using AssemblySearch.Core.Query;
using AssemblySearch.Core.Search;
using Xunit;
namespace AssemblySearch.Core.Tests;

public class ReviewRegressionTests
{
    [Fact]
    public async Task ParentDirectoryAliasesScanAssemblyOnlyOnce()
    {
        var alias = Path.Combine(Path.GetTempPath(), "assembly-alias-" + Guid.NewGuid());
        Directory.CreateSymbolicLink(alias, Path.GetDirectoryName(CategoryTests.Fixture)!);
        try
        {
            var alternate = Path.Combine(alias, Path.GetFileName(CategoryTests.Fixture));
            var (rows, s) = await SearchServiceTests.Collect(new(new("Read", SearchKind.MethodDef), [CategoryTests.Fixture, alternate]));
            Assert.Equal(3, rows.Count);
            Assert.Equal(1, s.ScannedFiles);
        }
        finally { Directory.Delete(alias); }
    }
    [Fact]
    public void ParentTraversalUsesResolvedDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "assembly-traversal-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(root, "real", "sub"));
        File.WriteAllText(Path.Combine(root, "real", "marker.dll"), "marker");
        Directory.CreateSymbolicLink(Path.Combine(root, "alias"), Path.Combine(root, "real", "sub"));
        try
        {
            Assert.Equal(Path.Combine(root, "real", "marker.dll"), ScopeResolver.CanonicalPath(Path.Combine(root, "alias", "..", "marker.dll")));
        }
        finally { Directory.Delete(root, true); }
    }
    [Theory]
    [InlineData("1e999")]
    [InlineData("-1e999")]
    [InlineData("9.99e9999")]
    public void OverflowingFloatIsNotExplicitInfinity(string text) => Assert.Throws<QueryValidationException>(() => QueryCompiler.Compile(new(text, Mode: QueryMode.Literal)));
}
