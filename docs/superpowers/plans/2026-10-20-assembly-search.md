# Assembly Search Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver, launch, test, commit and push a Linux desktop application and CLI covering the approved dnSpy main assembly-search specification.

**Architecture:** A .NET core library separates matching, offline metadata enumeration and cancellable streaming search. A CLI and an Avalonia desktop application consume the same service. File identities and query generation IDs prevent stale navigation and late results.

**Tech Stack:** .NET SDK 10.0.402 / net10.0; Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent and Avalonia.Headless.XUnit 12.1.4; dnlib 4.5.0; xunit 2.9.3; xunit.runner.visualstudio 3.1.5; Microsoft.NET.Test.Sdk 18.11.0. Pin direct packages and commit NuGet lockfiles. Versions were read from official .NET release metadata and NuGet flat-container indexes on 2026-10-20.

**Spec:** [Approved design](../specs/2026-10-20-assembly-search-design.md).

## Global Constraints

- Target the main assembly-search window's C01–C24 categories; preserve every row in `docs/verification.md`.
- Use .NET 10 LTS with an Avalonia desktop UI and a shared C# core library.
- Read managed files with dnlib; never load an input assembly into the application's execution context or invoke its code.
- Cap query input at 4096 UTF-16 characters; default to case-insensitive AND substring matching.
- Regex per-match timeout: 100 ms; live query debounce: 250 ms; workers: min(processor count, 4); batches: at most 100 results.
- Default result limit: 10,000; allowed range: 1 through 100,000; bounded result channel capacity: 4 batches.
- Input-file size limit: 512 MiB; decoded-content limit per resource: 16 MiB.
- CLI executable: `assembly-search`; JSON schemaVersion: 1; exits: 0 complete, 2 invalid query/invocation, 3 incomplete, 130 cancelled.
- Publish a self-contained linux-x64 archive; keep input binaries, SDK downloads, credentials and build output out of Git.
- Push a new private repository under KreesM7, without force; verify local and remote final commit SHAs match.
- Run commands from the repository root. Add the workspace-local SDK directory to PATH in the execution session, without changing global profiles.

## Review Focus

- The file can change between search and navigation: compare SHA-256/MVID before showing context (Task 2 test).
- Path aliases can add the same assembly twice: resolve existing symlinks for selected files and retain a single canonical identity without recursively following directories (Task 4 test).
- A resource or attribute may name a dangerous serialized type: inspect raw metadata/data without constructing the type (Task 3 test).
- Names and strings can contain newlines, terminal escapes, invalid Unicode or nonfinite numeric values: emit valid JSON and safe terminal display without changing stored evidence (Tasks 1 and 5 tests).
- A consumer may stop reading while cancellation or a result cap is reached: bounded writers must terminate and release readers rather than deadlock (Task 4 test).

## Files and boundaries

- `AssemblySearch.sln`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `NuGet.config`: pinned build, nullable types, deterministic compilation, public NuGet source and package lock support.
- `src/AssemblySearch.Core/Query/`: immutable request/options, validation, text/regex/literal matchers.
- `src/AssemblySearch.Core/Metadata/`: file identity, candidates, dnlib traversal, resources, attributes, portable symbols and context navigation.
- `src/AssemblySearch.Core/Search/`: scopes, bounded streaming, terminal outcomes and diagnostics.
- `src/AssemblySearch.Cli/`: argument parsing, formatting, entry point and exit status mapping.
- `src/AssemblySearch.Desktop/`: Avalonia app/window, view model, request controller, settings and file dialog/export integration.
- `tests/AssemblySearch.Core.Tests/`, `tests/AssemblySearch.Cli.Tests/`, `tests/AssemblySearch.Desktop.Tests/`: component, process and headless interaction tests respectively.
- `tests/Fixtures/`: compiled reference assemblies, embedded text/.resources, portable PDBs and fixture manifest. Build fixture sources in CI; do not commit compiled assemblies.
- `scripts/publish-linux.sh`, `.github/workflows/linux.yml`, `README.md`, `THIRD_PARTY_NOTICES.md`, `docs/verification.md`: tested delivery instructions and evidence.

## Task 1: A tested query compiler

**Files:** create build/solution files; `src/AssemblySearch.Core/AssemblySearch.Core.csproj`; `Query/SearchQuery.cs`, `Query/QueryCompiler.cs`, `Query/TextMatcher.cs`, `Query/LiteralMatcher.cs`; `Search/SearchContracts.cs`; `tests/AssemblySearch.Core.Tests/AssemblySearch.Core.Tests.csproj`, `QueryCompilerTests.cs`, `LiteralMatcherTests.cs`.

**Interfaces:** define SearchKind with the exact 24 names from C01–C24; QueryMode {Text, Regex, Literal}; SearchScopeKind {All, Selected, Directory, Type}; SearchStatus {Complete, Incomplete, Cancelled, Invalid}. Define `SearchQuery(string Text, SearchKind Kind, QueryMode Mode, SearchScopeKind Scope, IReadOnlyList<string> SelectedPaths, string? SelectedType, bool CaseSensitive=false, bool Exact=false, bool AnyTerm=false, bool IncludeGenerated=false, bool IncludeFramework=true, bool IncludeAttributes=false, bool IncludeResourceContents=true, int Limit=10000)`. Define `SearchDiagnostic(string Code, string Message, string? Path=null)`; `QueryValidationException` carrying a diagnostic. Produce `QueryCompiler.Compile(SearchQuery query): CompiledQuery`; `CompiledQuery.IsMatch(string text, object? literal, bool isLiteral): bool`. Keep compiled query immutable and safe for concurrent use; regex timeouts propagate as classified search errors.

- [ ] Obtain SDK 10.0.402 from the official .NET release manifest into `/home/moh/Documents/Codex/2026-10-20/w/work/dotnet`; validate the archive's SHA-512 against the manifest before extraction. Create the solution and pinned project/package configuration; restore lockfiles. Do not upgrade global Node, Python or system packages.
- [ ] Add failing Q01–Q06 tests: empty/4097-character queries, limits 0/100001, AND/OR and exact/substring combinations with `Read`, `ReadFile`, repeated spaces and Unicode. Use evidence's invariant-culture comparisons; `/Read.*/` is regex, `//` is plain text. Test invalid regex and a deliberately pathological pattern reaching the 100 ms timeout.
- [ ] Add failing literal assertions: `"123"` matches a string and not numeric 123; `0xffffffffffffffff` matches UInt64.MaxValue and not a rounded double; -9223372036854775808 matches Int64.MinValue; 9007199254740993 does not match 9007199254740992; -0x8000000000000000 is valid and the next negative magnitude is invalid; 1.5 matches floating 1.5 and not 1; signed zeros compare equal; NaN matches NaN only under an explicitly documented Linux policy; infinity retains sign; integer 1 matches true and 65 matches char A. Numeric overflow is invalid. Unsupported decimal/date candidates carry explicit diagnostics from enumeration. Literal display uses strings so JSON never needs a nonfinite JSON number.
- [ ] Run `dotnet test tests/AssemblySearch.Core.Tests --filter 'FullyQualifiedName~QueryCompilerTests|FullyQualifiedName~LiteralMatcherTests'`; confirm failures assert missing/incorrect behavior, then implement the named interfaces and rerun until passing. Record output and update Q01–Q06 implementation/test states accurately.
- [ ] Run `dotnet build -c Release` and the focused tests; commit query models, matchers, configuration and tests with `feat: add validated text regex and literal queries`.

## Task 2: Offline metadata and category traversal

**Files:** create `Metadata/AssemblyIdentity.cs`, `SearchCandidate.cs`, `MetadataReader.cs`, `CategoryFilter.cs`, `ContextReader.cs`; `tests/Fixtures/SearchFixture/SearchFixture.csproj`, `FixtureTypes.cs`; `tests/Fixtures/ReferenceFixture/ReferenceFixture.csproj`, `ReferencedType.cs`; `tests/AssemblySearch.Core.Tests/CategoryTests.cs`, `ContextReaderTests.cs`.

**Interfaces:** produce `AssemblyIdentity(string Path, string Sha256, Guid Mvid)`; `SearchCandidate(AssemblyIdentity Assembly, SearchKind Kind, string Coordinate, string DisplayName, string? DeclaringType, string? DeclaringMember, uint? Token, uint? IlOffset, string MatchText, object? LiteralValue, bool IsLiteral, bool IsGenerated, string Context)`; `IMetadataReader.Read(string path, SearchQuery query, Action<SearchDiagnostic> report, CancellationToken token): IEnumerable<SearchCandidate>`. `MetadataReader` implements the interface and owns/disposes modules within iterator lifetime. `CategoryFilter.Includes(SearchKind requested, SearchCandidate candidate): bool`. `ContextReader.Read(SearchCandidate candidate): string` verifies file identity before reading; mismatches raise a classified stale-input diagnostic.

- [ ] Create deterministic fixtures covering each named type/member category, overloads, nested types, module reference via a never-invoked P/Invoke declaration, referenced assembly, CompilerGeneratedAttribute and literal IL forms. Use Debug portable PDBs and disable fixture optimization. Add a static constructor that would create a marker if executed; metadata tests assert the marker stays absent.
- [ ] Write failing parameterized C01–C09/C12–C13/C15–C24 tests with expected owner/token and positive/negative assertions; scope Any unions to C01–C14 and Member to four member kinds. Ensure `ClassTypeDef` excludes delegate/interface/value-type/enum and GenericTypeDef uses own generic parameters. Read the existing reference evidence before choosing matcher/category compatibility claims.
- [ ] Write failing tests for missing/native/malformed files, size over 512 MiB using a sparse file, duplicate names with distinct coordinates, compiler-generated declaring ancestors, unsupported constants, iterator disposal, and file replacement before context navigation.
- [ ] Run `dotnet test tests/AssemblySearch.Core.Tests --filter 'FullyQualifiedName~CategoryTests|FullyQualifiedName~ContextReaderTests'`; observe meaningful failures, implement the dnlib reader/filter/context APIs, rerun until passing. Encode compact ldc.i4 operands as typed values and preserve IL offsets, rather than searching opcode text as literals.
- [ ] Verify fixture marker is absent, reader does not download assembly dependencies, and focused tests pass; update category/identity rows and commit `feat: search structured assembly metadata and IL`.

## Task 3: Local symbols, attributes and bounded resources

**Files:** create `Metadata/SymbolReader.cs`, `AttributeReader.cs`, `ResourceReader.cs`; modify MetadataReader; add fixture `FixtureResources.resx`, `Embedded.txt`, `LocalVariables.cs`; add `SymbolTests.cs`, `AttributeTests.cs`, `ResourceTests.cs` in Core.Tests.

**Interfaces:** produce `SymbolReader.ReadLocals(dnlib.DotNet.MethodDef method, AssemblyIdentity identity, Action<SearchDiagnostic> report): IReadOnlyDictionary<int,string>` with a documented deterministic policy for repeated local slots; `AttributeReader.Enumerate(dnlib.DotNet.IHasCustomAttribute owner, SearchCandidate parent, Action<SearchDiagnostic> report): IEnumerable<SearchCandidate>`; `ResourceReader.Enumerate(dnlib.DotNet.EmbeddedResource resource, AssemblyIdentity identity, SearchQuery query, Action<SearchDiagnostic> report, CancellationToken token): IEnumerable<SearchCandidate>`. Candidate categories remain local/parameter/resource/owner kinds; facet coordinates distinguish attribute values. Literal mode must not require nonliteral flags to discover supported numeric/string constants.

- [ ] Write failing C10/C11/C14 tests for PDB local names, missing symbols with labeled synthetic `V_0`, parameter/local union, text resource names/content, .resources keys and primitive/string values, plus attributes on assemblies/types/members/parameters. Include both inclusion and exclusion flags.
- [ ] Add adversarial tests: serialized user-type resource data is reported unsupported without constructors; oversized decoded resource stops at 16 MiB; truncated .resources and malformed UTF-8 return diagnostics; cancellation interrupts decoding; unsupported decimal/date resource literals are explicit and cannot produce a falsely complete search.
- [ ] Run the three focused test classes and confirm failures. Implement the readers using dnlib portable symbol data, strict/bounded text decoding and raw resource records. For .resources, use `ResourceReader.GetResourceData` to inspect type and raw bytes, decode only known primitive encodings, and never enumerate deserialized Value objects. Keep every skipped record's reason visible.
- [ ] Rerun category, symbol, attribute and resource suites; assert the marker file is absent and unsupported-resource status is preserved; document the resource-renderer limitation.
- [ ] Update C10/C11/C14 and relevant Q/S rows; commit `feat: search locals attributes and supported resources`.

## Task 4: Cancellable search, scopes and exact limits

**Files:** create `Search/ScopeResolver.cs`, `SearchService.cs`, `SearchSession.cs`; extend SearchContracts; add `ScopeTests.cs`, `SearchServiceTests.cs`, `SearchLifecycleTests.cs`.

**Interfaces:** produce `SearchRequest(SearchQuery Query, IReadOnlyList<string> LoadedPaths)`; `SearchResult(string Id, SearchCandidate Candidate, string ValueKind, string ValueDisplay)`; `SearchBatch(IReadOnlyList<SearchResult> Results)`; `SearchSummary(SearchStatus Status, long ElapsedMilliseconds, int ScannedFiles, int SkippedFiles, int ResultCount, IReadOnlyList<SearchDiagnostic> Diagnostics)`; `SearchSession` exposes `IAsyncEnumerable<SearchBatch> ReadBatchesAsync(CancellationToken token)` and `Task<SearchSummary> Completion` and implements IAsyncDisposable for cancellation/disposal. `ISearchService.Start(SearchRequest request, CancellationToken token): SearchSession`; `SearchService` consumes IMetadataReader. `ScopeResolver.Resolve(SearchRequest request): IReadOnlyList<string>` validates scope selections; type filtering stays in reader traversal and includes nested descendants.

- [ ] Write failing S01/S02 tests for all/selected/same-directory/type scopes, empty selection, loaded-only directory behavior, file symlink aliases and Linux case-sensitive paths. Framework policy: identify known framework public-key tokens paired with known framework assembly identity namespaces; retain an application named System.Custom without such identity. Document this as a Linux policy, not verified dnSpy equivalence.
- [ ] Write failing lifecycle tests using a fake IMetadataReader: 4-worker maximum, batch <=100/channel capacity 4, concurrent cap exactly 1/101/10000, cancellation before start and during traversal, first-error/partial summaries, regex timeout, slow consumer, and a consumer that abandons the stream. Assert disposal and terminal completion within a test deadline after cancellation.
- [ ] Run focused tests to establish failures. Implement atomic result admission and cancellation-aware channel writes; reaching the cap emits no extra row and marks incomplete. DisposeAsync must cancel and await producers without requiring further consumer reads. Keep result IDs stable from assembly digest/MVID/category/coordinate.
- [ ] Rerun core suites; inspect that each supported candidate is matched exactly once per selected facet and that diagnostics do not turn failures into empty complete results. Record C/Q/S coverage in verification.md.
- [ ] Commit `feat: stream cancellable scoped searches with bounded results`.

## Task 5: Usable CLI and process tests

**Files:** create `src/AssemblySearch.Cli/AssemblySearch.Cli.csproj`, `Program.cs`, `CliOptions.cs`, `CliRunner.cs`, `OutputFormatter.cs`; create Cli.Tests project and `CliProcessTests.cs`.

**Interfaces:** `CliOptions.Parse(string[] args): CliOptions` produces a SearchRequest, Json flag and Help flag or a validation diagnostic. `CliRunner.RunAsync(string[] args, TextWriter stdout, TextWriter stderr, CancellationToken token): Task<int>`. Produce versioned JSON envelope `{schemaVersion:1, query, results, diagnostics, summary}` with explicit camelCase names and enum strings; format typed literals as ValueKind/ValueDisplay strings. Entry point wires Console.CancelKeyPress to cancellation without terminating before summary output.

- [ ] Add failing process tests for known fixture queries, all 24 `--kind` enum names, text/regex/literal flags, exact/any-term, `--selected` repeatable paths, `--type` qualified type selector, include-generated/include-attributes/no-framework/no-resource-contents, limit validation, help, unknown args and missing query. Help prints the semantics and scope rules.
- [ ] Test JSON-only stdout, control-character escaping, paths with spaces/leading dashes following `--`, hostile display names rendered safely in terminal output, nonfinite literals represented as strings, and 0/2/3/130 exit mapping. Use ProcessStartInfo.ArgumentList and no shell. On Linux send SIGINT to the owned test process for Ctrl+C verification.
- [ ] Run `dotnet test tests/AssemblySearch.Cli.Tests`; confirm failures, implement parsing/service wiring/formatting, build CLI, and rerun until passing. Sorting completed results uses stable coordinates; preserve streaming in the shared service.
- [ ] Compare a CLI result set with a direct SearchService result set for the same fixture/query; validate JSON with System.Text.Json and ensure stderr never corrupts stdout.
- [ ] Update V04 and CLI coverage; commit `feat: expose assembly search as a tested Linux CLI`.

## Task 6: Desktop state, request generations and settings

**Files:** create Desktop project; `Presentation/SearchController.cs`, `MainWindowViewModel.cs`, `SettingsStore.cs`, `AppSettings.cs`; create Desktop.Tests project, `SearchControllerTests.cs`, `SettingsStoreTests.cs`.

**Interfaces:** `SearchController(ISearchService service, Action<Action> postToUi, TimeProvider? timeProvider=null)` exposes `UpdateAsync(SearchRequest request): Task`, `CancelAsync(): Task`, `UnloadAsync(string path): Task`, `ObservableCollection<SearchResult> Results`, `SearchSummary? Summary`, `bool IsSearching`, `IReadOnlyList<SearchDiagnostic> Diagnostics`, INotifyPropertyChanged and IAsyncDisposable. Each update increments a generation immediately before debouncing 250 ms; cancellation ignores late results/completion from prior generations. `MainWindowViewModel` exposes the query's controls, `ObservableCollection<string> LoadedPaths`, controller Results, and `SearchResult? SelectedResult`; it forwards state changes to the controller. `SettingsStore.Load(string path, Action<SearchDiagnostic> report): AppSettings`; `SettingsStore.Save(string path, AppSettings settings): void`. Define `AppSettings(SearchKind Kind, QueryMode Mode, SearchScopeKind Scope, bool CaseSensitive, bool Exact, bool AnyTerm, bool IncludeGenerated, bool IncludeFramework, bool IncludeAttributes, bool IncludeResourceContents, int Limit, double Width, double Height)`; options default to SearchQuery defaults and width/height default to 1280/800. Persist options and window bounds only, never file bytes, queries or history.

- [ ] Write failing controller tests using fake sessions and a deterministic scheduler/TimeProvider to test 249/250 ms debounce, rapid updates, old batches/old completion after new query, Escape cancellation retaining partial results, unloading during a search and UI-thread posting. Use the same query defaults and field names defined in Task 1.
- [ ] Write failing settings tests for malformed JSON, interrupted atomic replacement, valid option round-trip, geometry clamping to available screen and absence of query/history/file contents in serialized output. Use the Linux XDG configuration directory when present, otherwise the platform's per-user config directory; do not edit shell profiles.
- [ ] Run the two test classes; observe failures, implement controller/view-model/settings APIs with explicit async disposal and no fire-and-forget reader ownership, then rerun until passing.
- [ ] Compare desktop-controller results with the CLI/core stable set; prove stale results cannot reappear after unload or navigation failure.
- [ ] Update S04/S05/S08 and V02 coverage; commit `feat: coordinate desktop queries and persistent options safely`.

## Task 7: Native Linux desktop workflow

**Files:** create Desktop `Program.cs`, `App.axaml`, `App.axaml.cs`, `MainWindow.axaml`, `MainWindow.axaml.cs`, `Presentation/ResultDetails.cs`; add Desktop.Tests `MainWindowTests.cs`, `NavigationTests.cs` using Avalonia.Headless.XUnit.

**Interfaces:** UI consumes MainWindowViewModel/SearchController from Task 6 and ContextReader from Task 2. `ResultDetails.Open(SearchCandidate candidate): string` returns verified context or a readable stale/error message. File opening uses Avalonia StorageProvider; JSON export reuses Task 5's envelope/serialization contract via a Core serialization module if needed, not a CLI executable dependency.

- [ ] Add failing headless interaction tests: all 24 category choices and all scopes; load/unload assembly; query/option change and results; empty states, diagnostics, selection opens correct token/IL context; stale file context explains failure; copy/export retain stable identity. Mark real OS clipboard/file picker checks separately from headless substitutes.
- [ ] Test keyboard focus, Ctrl+F and Escape, labeled controls, visible incomplete/cancelled states and a virtualized results list. Confirm a large fake result stream remains bounded and model updates use the UI dispatcher.
- [ ] Run `dotnet test tests/AssemblySearch.Desktop.Tests`, confirm failures, implement the approved three-pane layout and controls, and rerun. Use readable monospace for IL context, highlighted target instruction, system theme and Fluent controls; avoid custom-rendered fake controls.
- [ ] Launch with `dotnet run --project src/AssemblySearch.Desktop` in the real Linux display. Load the fixture and at least one supplied dnSpy assembly, perform a known query, select a result, change mode, cancel, and capture actual runtime verification. Headless success alone does not mark V06 verified. If available tools cannot inspect a native window, record that interaction limitation; still verify launch/process health and keep V06 incomplete.
- [ ] Update desktop verification rows with actual evidence and commit `feat: add the Linux desktop assembly search workflow`.

## Task 8: Packaging, final audit and GitHub delivery

**Files:** create `scripts/publish-linux.sh`, `.github/workflows/linux.yml`, `THIRD_PARTY_NOTICES.md`; update README, verification.md and evidence audit when runtime checks resolve unknowns.

**Interfaces:** publish script outputs `artifacts/assembly-search-linux-x64.tar.gz` containing `cli/assembly-search`, `desktop/assembly-search-desktop`, notices and launch instructions. CI builds tests/fixtures and all product projects, then runs tests and publish. Assets uploaded by CI contain no SDK, input dnSpy binaries or credentials.

- [ ] Add a packaging smoke check: extract archive to a path containing spaces, run its self-contained CLI `--help` and a fixture query without relying on installed dotnet, and parse JSON/exit status. Publish projects with `-r linux-x64 --self-contained true -c Release`; do not enable trimming for reflection-heavy parsing/UI packages.
- [ ] Implement the script and Linux CI with pinned SDK, locked restore, Release build, test, publish and artifact upload. Pin GitHub Actions to verified immutable SHAs at implementation time. Document tested native dependencies and keyboard/CLI examples, .NET resource/PDB limitations, attribution and exact compatibility differences.
- [ ] Run `dotnet restore --locked-mode`, `dotnet build -c Release --no-restore`, `dotnet test -c Release --no-build`, the publish smoke check and the actual desktop check. Run known-query searches on the supplied dnSpy assembly and verify SHA/MVID/token coordinates against evidence. Do not fetch recorded URLs or execute inspected assemblies.
- [ ] Review each of the 47 verification rows against source and passed commands. Confirm all 24 categories have positive/negative test assertions. Record incomplete requirements honestly; fix mandatory defects before declaring the app finished. Review the final diff, dependency lockfiles and repository status; perform a fresh independent review using the execution method selected by the user. Commit validated delivery changes.
- [ ] Discover authorized GitHub repository creation/push capabilities. Create private `KreesM7/assembly-search`, or an available descriptive suffix if occupied; never reuse an unrelated repo. Use an authenticated Git transport if available, otherwise the available GitHub/browser tools. Do not print/extract tokens. If authentication or creation is unavailable, report that exact blocker and leave verified local commits intact rather than claim a push. Push without force, compare remote/local final commit SHAs, inspect CI once, and fix failing checks before reporting success.

## Plan self-review and handoff

- C01–C24: Tasks 2–3, exercised by Task 5/7 and audited by Task 8.
- Q01–Q06: Task 1; Q07: Tasks 2/4/6.
- S01–S03: Tasks 2–4; S04: Tasks 2/6/7; S05–S06: Tasks 4/6; S07: Tasks 5/7; S08: Task 6.
- V01–V03: Tasks 2–4/6; V04: Task 5; V05–V08: Tasks 7–8.
- All five Review Focus conditions have owning tests. No existing code or lockfiles are assumed. All cross-task public names are defined in their producing Interfaces blocks.
- The current host is Linux x86_64 with DISPLAY and WAYLAND_DISPLAY set. It has Git, but `dotnet`, `gh` and Xvfb were not found on PATH. A display variable does not prove native app interaction is available.
- The user has approved the written specification but has not yet reviewed this plan or selected an execution method. No product implementation is authorized by the writing-plans workflow until that review is complete.
