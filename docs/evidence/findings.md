# dnSpy search: evidence from the supplied ZIP

Inspected on 2026-10-20 using REA MCP. This report describes static observations from the supplied build, followed by a proposed independent Linux implementation. The Windows application was not executed.

## Artifact identity

| Artifact | SHA-256 | Module MVID |
| --- | --- | --- |
| dnSpy-net.zip | `025dc4e02540e9afa9ab0121c8b2a3fff5e9e515835823a74a9e5635bd4d4302` | n/a |
| bin/dnSpy.dll | `6a2d4c9b472200d55262b80a5f1bb6d08498488a7c38de14893e5a344bce212f` | `5ae4c3fc-8180-40f1-9604-113787d2e993` |
| bin/dnSpy.Contracts.DnSpy.dll | `1b883b87bb52dad1b1ef677ad7e80e288df2cdf913c791f7bd3072a36e2839e2` | `a631b8cc-516e-4564-9977-b2dc8098bf4f` |

Tokens below are coordinates in these exact assemblies; they are not portable across builds.

## How search works

The observed call relationships support this flow:

```text
SearchControlVM: text + options + search location
    -> SearchComparerFactory: text / regex / typed literal matcher
    -> DocumentSearcherOptions: matcher + member-kind filter + flags
    -> DocumentSearcher: background task, parallel traversal, cancellation
    -> FilterSearcher: assembly -> module -> types -> members -> method bodies
    -> queued SearchResult batches -> SearchControlVM result collection
```

### 1. The UI builds a query and chooses a scope

`dnSpy.Search.SearchControlVM.StartSearch` (`0x06001258`) calls `Clear`, `CancelSearch`, `String.IsNullOrEmpty`, `CreateSearchComparer`, and constructs `DocumentSearcherOptions` and `FlagsDocumentTreeNodeFilter`. It passes the selected member-kind flags, decompiled-data option, and compiler-generated-members option into search configuration, creates an `IDocumentSearcher`, and subscribes to result/completion events.

The same method calls four scope helpers: `GetAllFilesToSearch`, `GetSelectedFilesToSearch`, `GetAllFilesInSameDirToSearch`, and `GetSelectedTypeToSearch`. These establish distinct scope paths in the implementation. They do not establish which scope is selected by default.

`set_SearchText` (`0x06001251`) calls `DelayedAction.Start`; `CancelSearch` (`0x06001264`) calls `DelayedAction.Cancel` and `IDocumentSearcher.Cancel`. This supports delayed search restarts. The delay duration has not been established.

### 2. Matching is separate from traversal

`SearchControlVM.CreateSearchComparer` (`0x06001260`) reads `CaseSensitive`, `MatchWholeWords`, and `MatchAnySearchTerm`, and calls `SearchComparerFactory.Create` or `CreateLiteral`.

In **dnSpy.Contracts.DnSpy.dll**:

| Method | Token | Observed calls |
| --- | --- | --- |
| SearchComparerFactory.Create | `0x0600059d` | TryCreateRegEx, RegExSearchComparer constructor, String.Split, OrSearchComparer constructor, AndSearchComparer constructor |
| SearchComparerFactory.CreateLiteral | `0x0600059e` | TryParseInt64, TryParseUInt64, Double.TryParse, IntegerLiteralSearchComparer, DoubleLiteralSearchComparer, StringLiteralSearchComparer, RegExStringLiteralSearchComparer |
| SearchComparerFactory.TryCreateRegEx | `0x0600059f` | String.Trim, String character/length access, Substring, Regex constructor |

**Interpretation:** text searches support multiple terms and AND/OR composition, while literal mode distinguishes numeric and string values. Regex matching has a separate path. Exact query delimiters, word-boundary rules, numeric overflow behavior, and invalid-regex fallback are not established by these call edges alone.

### 3. Search runs away from the UI and can be cancelled

In **dnSpy.dll**, `DocumentSearcher.StartInternal` (`0x060011f9`) calls `TaskFactory.StartNew`, `Task.ContinueWith`, and `TaskScheduler.FromCurrentSynchronizationContext`.

`DocumentSearcher.SearchNewThread` (`0x060011fc`) constructs `ParallelOptions`, sets its cancellation token and maximum degree of parallelism, reads `Environment.ProcessorCount`, and calls `Parallel.ForEach`. It handles aggregate exceptions, sets `TooManyResults`, cancels work, and dispatches completion using `Dispatcher.BeginInvoke`.

`DocumentSearcher.Cancel` (`0x060011f6`) calls `CancellationTokenSource.Cancel`. `AddSearchResult` (`0x060011fe`) calls `ThrowIfCancellationRequested`.

**Interpretation:** bounded parallel work and cooperative cancellation keep the search operation separate from UI updates. Actual responsiveness, timing, and the result-limit value require further inspection or execution.

### 4. Traversal searches structured .NET data

`FilterSearcher` contains `SearchAssemblies`, `SearchModule`, `SearchModAsmReferences`, `SearchResources`, `SearchResourceTreeNodes`, `SearchMembers`, `CheckCustomAttributes`, and overloaded `Search`/`CheckMatch` methods. Their call edges lead into `ISearchComparer.IsMatch` through `FilterSearcher.IsMatch` (`0x06001207`).

`FilterSearcher.SearchBody` (`0x0600121f`) reads `MethodDef.Body`, `CilBody.Variables`, local names, and `CilBody.Instructions`. It checks the filter and cancellation token, invokes matching, constructs `SearchResult` and `BodyResult`, and invokes the match callback.

This is evidence of metadata and IL traversal, rather than only a text search over rendered C#. This does not prove all search modes inspect every category; filter and option branches control their inclusion. A decompiled-data option exists, but its complete behavior was not traced here.

### 5. Results arrive in batches with navigation context

`DocumentSearcher.AddSearchResultNoCheck` (`0x060011ff`) adds to a list and calls `EmptySearchResultsQueue`. `EmptySearchResultsQueue` (`0x06001200`) uses `Monitor.Enter/Exit`, copies the list with `ToArray`, clears it, checks cancellation state, and invokes a result event.

`SearchControlVM.FileSearcher_OnNewSearchResults` (`0x0600125f`) reads `SearchResultEventArgs.Results` and adds them to the UI result collection. Its completion handler (`0x0600125e`) removes the searching indicator and reads `TooManyResults`.

SearchBody assigns `Object`, `NameObject`, `LocationObject`, `Document`, and `ObjectInfo` on results. This supports results carrying both display and navigation context; the exact click-to-navigation path was not traced.

## Reproducible evidence

The companion `rea-evidence.json` is the complete exported REA evidence bundle for this investigation. `search-evidence.json` contains selected original view receipts for easier inspection. Look up the following parent evidence IDs and inspect `normalized_result` collections:

| Evidence | Parent ID |
| --- | --- |
| Archive inventory and integrity | `ev_cb06a3c7271c38a037db315a09bf6dd092b987ab67a6d16cc9e70d8489973b0a` |
| UI search control, settings persistence, result model | `ev_20c97eb04d93e727999d27a03cf5ff140883587d879ba92d1c1de7409bec48d2` |
| DocumentSearcher and FilterSearcher | `ev_41b8b7ce6b80dab40428bec25a9aafa4f610ad4b500680d2dc407dca3c518df8` |
| Matcher factory | `ev_da591f029e5d27e799893b7e40192d2908c0e4c0d3c9f4b5c33414081ff017ef` |

Archive inventory reported 500 regular-file occurrences, no integrity contradictions, and complete coverage within its inventory step. Selected managed inspections reported complete metadata coverage without issues. Completeness applies to the selected types, not the whole product.

The evidence contains build-bound method tokens, CIL body hashes, instruction anchors, and resolved call edges. It is decoded metadata/CIL evidence, not recovered original C# source or runtime tracing. Regex syntax, exact defaults, ranking, debounce interval, and full decompiled-data behavior remain unresolved.

## Proposed Linux project for review

Working name: **Assembly Search**. Build a native Linux desktop application with Avalonia and a shared .NET search library using dnlib to read managed DLL/EXE files without executing them. Include a CLI using the same engine, suitable for automation and JSON output.

The first version would open multiple assemblies; search type/member names and string/numeric IL literals; provide text and explicit regex modes, case sensitivity, whole-word matching, AND/OR terms, kind and assembly/type filters; stream results with cancellation and a visible result cap; and show assembly path, metadata token, declaring type, and IL offset/context for selected results. Query changes cancel prior work and prevent stale results from appearing.

Keep the matcher, metadata reader, background search service, CLI, and desktop UI separate. Tests should compile a small fixture assembly with known names and constants, exercise malformed inputs, query combinations, regex timeouts, cancellation and result limits, and run the CLI against real managed assemblies. Provide Linux launch/build instructions and CI. This is a search-focused application; full dnSpy debugging, assembly editing, and C# decompilation are beyond the proposed first version.

Alternatives: a local browser UI simplifies distribution but is less like a desktop application; a CLI-only project is smaller but loses the interactive search workflow. The native desktop plus shared CLI approach best fits the requested Linux application.

Publish the new repository under the authenticated account `KreesM7`, with private visibility by default, after implementation and validation. Include the human-readable findings and compact supporting evidence, not the supplied application binaries. No repository or product code has been created yet.
