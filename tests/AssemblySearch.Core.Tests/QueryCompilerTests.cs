using AssemblySearch.Core.Query;
using Xunit;
namespace AssemblySearch.Core.Tests;

public class QueryCompilerTests
{
    [Theory]
    [InlineData("read", "ReadFile", false, false, true)]
    [InlineData("read", "ReadFile", true, false, false)]
    [InlineData("Read", "ReadFile", false, true, false)]
    [InlineData("Read", "Read", false, true, true)]
    [InlineData("Read File", "ReadFile", false, false, true)]
    [InlineData("Read File", "Read", false, false, false)]
    [InlineData("  Read  File ", "ReadFile", false, false, true)]
    [InlineData("", "Read", false, false, false)]
    [InlineData("  ", "Read", false, false, false)]
    [InlineData("é", "É", false, false, true)]
    public void MatchingRules(string query, string candidate, bool sensitive, bool exact, bool expected) => Assert.Equal(expected, QueryCompiler.Compile(new(query, CaseSensitive: sensitive, Exact: exact)).IsMatch(candidate));
    [Fact] public void AnyTermMatchesOnlyOneTerm() => Assert.True(QueryCompiler.Compile(new("Read Write", AnyTerm: true)).IsMatch("WriteFile"));
    [Fact] public void ExactAndRequiresEachEntireCandidate() => Assert.False(QueryCompiler.Compile(new("Read File", Exact: true)).IsMatch("Read File"));
    [Fact] public void SlashRegexWorks() => Assert.True(QueryCompiler.Compile(new("/Read.*/")).IsMatch("ReadFile"));
    [Fact] public void EmptySlashPatternIsText() => Assert.True(QueryCompiler.Compile(new("//")).IsMatch("path//file"));
    [Fact] public void InvalidRegexRejected() => Assert.Throws<QueryValidationException>(() => QueryCompiler.Compile(new("[", Mode: QueryMode.Regex)));
    [Fact]
    public void RegexTimeoutStopsMatching()
    {
        var c = QueryCompiler.Compile(new("(a+)+$", Mode: QueryMode.Regex));
        Assert.Throws<System.Text.RegularExpressions.RegexMatchTimeoutException>(() => c.IsMatch(new string('a', 100000) + "!"));
    }
    [Theory][InlineData(0)][InlineData(100001)] public void InvalidLimits(int limit) => Assert.Throws<QueryValidationException>(() => QueryCompiler.Compile(new("x", Limit: limit)));
    [Fact] public void OversizedQuery() => Assert.Throws<QueryValidationException>(() => QueryCompiler.Compile(new(new string('x', 4097))));
}
