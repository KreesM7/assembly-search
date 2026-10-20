using AssemblySearch.Core.Query;
using Xunit;
namespace AssemblySearch.Core.Tests;

public class LiteralMatcherTests
{
    static bool Match(string q, object value, bool exact = false) => QueryCompiler.Compile(new(q, Mode: QueryMode.Literal, Exact: exact)).IsMatch("", value, true);
    [Fact]
    public void NumericStringsKeepType()
    {
        Assert.True(Match("\"123\"", "123"));
        Assert.False(Match("\"123\"", 123));
        Assert.False(Match("123", "123"));
    }
    [Fact]
    public void IntegersNeverRound()
    {
        Assert.True(Match("0xffffffffffffffff", ulong.MaxValue));
        Assert.False(Match("0xffffffffffffffff", (double)ulong.MaxValue));
        Assert.True(Match("-9223372036854775808", long.MinValue));
        Assert.True(Match("-0x8000000000000000", long.MinValue));
        Assert.False(Match("9007199254740993", 9007199254740992L));
    }
    [Theory][InlineData("18446744073709551616")][InlineData("-0x8000000000000001")][InlineData("0xgarbage")] public void OverflowInvalid(string q) => Assert.Throws<QueryValidationException>(() => QueryCompiler.Compile(new(q, Mode: QueryMode.Literal)));
    [Fact]
    public void TypedValues()
    {
        Assert.True(Match("1", true));
        Assert.False(Match("1", false));
        Assert.True(Match("65", 'A'));
        Assert.True(Match("1.5", 1.5f));
        Assert.False(Match("1.5", 1));
        Assert.True(Match("-0.0", 0d));
        Assert.True(Match("NaN", double.NaN));
        Assert.True(Match("Infinity", double.PositiveInfinity));
        Assert.False(Match("Infinity", double.NegativeInfinity));
    }
    [Fact]
    public void StringMatchingAndRegex()
    {
        Assert.True(Match("hello", "hello world"));
        Assert.False(Match("hello", "hello world", true));
        Assert.True(Match("/hello.*/", "hello world"));
    }
    [Fact] public void TextDoesNotMatchLiteralOnlyCandidate() => Assert.False(QueryCompiler.Compile(new("hello", Mode: QueryMode.Literal)).IsMatch("hello", null, false));
}
