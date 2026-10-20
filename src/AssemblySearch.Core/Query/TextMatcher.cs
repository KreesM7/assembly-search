namespace AssemblySearch.Core.Query;

internal static class TextMatcher
{
    public static bool Match(string value, string term, StringComparison comparison, bool exact) => exact ? string.Equals(value, term, comparison) : value.IndexOf(term, comparison) >= 0;
}
