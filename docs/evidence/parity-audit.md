# Main-window search audit

This supplement refines the initial findings using additional selected metadata/CIL inspections. It does not assert that a Linux implementation exists yet.

## New evidence and corrections

- `dnSpy.Search.SearchType` has 24 non-sentinel fields. Its assembly identity is the dnSpy.dll hash/MVID recorded in `findings.md`. Parent evidence: `ev_845e620516f8bf54fc98a72a025de397d81c98b1cbc2b0c96d83b8dd19415708`. The fields page and SearchControlVM constructor calls are in `parity-evidence.json`.
- The comparer inspection parent is `ev_980af51b51a7aeb683ff26a9f5ce6be194727bd5fe9cd04389d1a6c522299797`, bound to the recorded dnSpy.Contracts.DnSpy.dll identity. The chosen types had complete coverage with no reported issues; two guessed type names were unmatched and are explicitly retained in the selection record.
- `AndSearchComparer.IsMatch` (`0x060005ae`) branches on `matchWholeWords`: its true path calls String.Equals at IL offset 36, and its false path calls String.IndexOf at IL offset 53. `OrSearchComparer.IsMatch` (`0x060005b1`) has the corresponding branches. This is whole-candidate equality, not a word-boundary algorithm.
- Their boolean constructors (`0x060005ac`, `0x060005af`), and StringLiteralSearchComparer constructor (`0x060005a4`), contain `ldc.i4.2`/`ldc.i4.3` selections. These are [InvariantCulture and InvariantCultureIgnoreCase](https://learn.microsoft.com/en-us/dotnet/api/system.stringcomparison?view=net-10.0). Ordinal comparison would be a behavioral change.
- `SearchComparerFactory.Create` (`0x0600059d`) loads constant 32 at IL offset 27 before String.Split: the delimiter is an ordinary space. The exact overload's empty-segment policy still needs confirmation before equivalence is claimed.
- `TryCreateRegEx` (`0x0600059f`) trims input, checks length against two, compares the first and last characters with constant 47 at IL offsets 24 and 42, and passes the interior substring to Regex. Its exception region covers construction and leads to a null-regex result. Therefore `/pattern/` is supported, and invalid regex can fall through to a plain text/literal matcher in the factory.
- `CreateLiteral` (`0x0600059e`) checks first/last characters against constant 34 at IL offsets 98 and 116, identifying double-quoted strings. It attempts signed integer, unsigned integer and double parsing first, then quote handling, regex, and string matching.
- Integer parsing loads `-` and `0x`; the methods contain range checks against Int64 limits and use UInt64.TryParse. Exact unsigned bit-pattern matching still requires additional behavioral checks. Avoid conversion through double when implementing 64-bit integer comparisons.
- IntegerLiteralSearchComparer.IsMatch (`0x060005a7`) also calls Decimal comparison and DateTime construction/equality. This is a known compatibility edge; claiming complete numeric/resource parity without handling these paths would be incorrect.
- `StringLiteralSearchComparer.IsMatch` (`0x060005a5`) and the regex/numeric literal matchers read `IHasConstant.Constant.Value`. Literal search includes metadata constants, not only IL operands.
- The previously retained `FilterSearcher.ToString` (`0x06001212`) reads `SearchDecompiledData` and calls `IResourceNode.ToString`. This establishes a resource-rendering path; the option's name alone does not prove a full-text search over all decompiled C#.

## Completeness discipline

The specification's C01–C24 table maps every observed main-window search category to a required positive and negative test. It also identifies deliberate Linux behavior and unresolved compatibility details. These rows are requirements, not passed tests.

The initial report did not fully trace the text/hex-editor search services, the StringSearcher extension, native debugger integration, plug-in resource renderers, or all decompiler-specific hidden-member behavior. They must not be described as fully understood or reimplemented.

The source reference assemblies were not executed. Raw evidence is static decoding and includes body hashes and anchors; it is not original source. Runtime comparison, exact default options, complete resource rendering, local-symbol coverage, framework classification and numeric edge cases remain separately verifiable concerns.
