# Assembly Search — Linux search application specification

Date: 2026-10-20

Status: written specification awaiting user review. The conversational direction is approved; product implementation and verification have not started.

## Purpose and success criteria

Create a new Linux desktop application and companion CLI that reproduce the useful main assembly-search workflow of the supplied dnSpy build. The user wants the feature explained with binary evidence, implemented correctly, committed, and pushed to a new GitHub repository. The user specifically requested that no search functions be silently omitted.

Success means every requirement and every observed main-window search category below has an implementation, positive/negative tests, and a recorded verification outcome. A category listed in a menu but not implemented does not count as complete. Unknown behavior and deliberate differences must remain visible in the final report. Neither test coverage nor this specification establishes bug-free or whole-product equivalence.

## Scope and evidence boundary

Reference ZIP SHA-256: `025dc4e02540e9afa9ab0121c8b2a3fff5e9e515835823a74a9e5635bd4d4302`.

The evidence comes from static PE/CLI metadata and CIL anchors in dnSpy.dll and dnSpy.Contracts.DnSpy.dll. The investigation did not run the Windows UI. Evidence IDs, exact assembly hashes/MVIDs, selected decoded methods, fields and call edges are in `docs/evidence/`. All method tokens are build-local.

The target is the **main assembly-search window**. Search inside text/hex editors, the separate StringSearcher extension, debugger search, assembly editing, and a full C# decompiler are separate features. The application will display IL context and metadata. It will not advertise a C# decompilation feature.

Resource content search is included for ordinary text and standard .resources primitive/string elements. dnSpy's plug-in-specific renderers, BAML decompilation, image rendering and arbitrary serialized-object resource decoding are not promised. Unsupported content must be reported, not silently treated as searched. Missing PDBs may limit local-variable names; synthetic IL local identities remain available and must be labeled.

## Selected architecture

Use .NET 10 LTS with an Avalonia desktop UI and a shared C# core library. Read managed files with dnlib; never load an input assembly into the application's execution context or invoke its code. Use the existing compatible SDK when available, otherwise install the SDK into a workspace-local tools directory during implementation. Pin concrete SDK/package versions in the implementation plan and lockfiles.

Official references: [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), [Avalonia Linux desktop guidance](https://docs.avaloniaui.net/docs/platform-specific-guides/linux), [dnlib API and examples](https://github.com/0xd4d/dnlib).

Components:

1. **Core query model and compiler:** immutable query, search category, scope, matching options, query validation, and reusable matcher. Depends on .NET, not Avalonia.
2. **Metadata reader:** owns/disposes dnlib modules, enumerates supported candidate kinds, preserves assembly/MVID/token/IL identity, and emits diagnostics for unreadable data.
3. **Search service:** bounded parallel file work, cooperative cancellation, bounded result channel, exact result cap, completion summary, and structured diagnostics. It invokes the metadata reader and matcher without referencing UI objects.
4. **CLI:** parses arguments into the shared query, prints text or JSON, handles Ctrl+C, and maps completion status to exit codes.
5. **Desktop:** assembly list, scope/category/options controls, live query, virtualized results, progress/cancel, and detail pane. Consumes the same service and enforces request-generation checks before applying UI events.
6. **Tests and fixtures:** deterministic generated .NET assemblies, embedded resources and PDBs; core, CLI, and desktop-controller tests plus Linux launch verification.

Alternatives considered: a local browser UI simplifies deployment but adds a local server and diverges from the requested desktop workflow; a CLI-only solution omits interactive discovery. Desktop plus CLI shares behavior and makes it testable.

## Observed category coverage and required tests

The following 24 non-sentinel members were observed in `dnSpy.Search.SearchType`, and corresponding UI resource getter calls were observed in SearchControlVM's constructor. These are categories, not 24 independent search algorithms. The Linux application must expose all 24 and exercise each against a fixture.

| ID | Observed category | Linux behavior and fixture assertion |
| --- | --- | --- |
| C01 | AssemblyDef | Match assembly name/full identity; reject an unrelated assembly. |
| C02 | ModuleDef | Match module name; verify location is its module. |
| C03 | Namespace | Match namespace; collapse duplicate namespace candidates within a module. |
| C04 | TypeDef | Match named and nested types, with qualified names available. |
| C05 | FieldDef | Match field names and return the owning type/token. |
| C06 | MethodDef | Match method names, including overloads, with distinct tokens. |
| C07 | PropertyDef | Match property names; do not fabricate accessor duplicates as properties. |
| C08 | EventDef | Match event names and owner identity. |
| C09 | ParamDef | Match named method parameters and preserve sequence/owner. |
| C10 | Local | Match local identities; use PDB names when supported and explicitly diagnose missing names. |
| C11 | ParamLocal | Union of parameter and local results; test both and exclude a field. |
| C12 | AssemblyRef | Match declared referenced-assembly names without loading dependencies. |
| C13 | ModuleRef | Match declared module references, including a P/Invoke fixture. |
| C14 | Resource | Match resource names, .resources keys and supported decoded text/primitive values. |
| C15 | GenericTypeDef | Match only generic type definitions. |
| C16 | NonGenericTypeDef | Match only nongeneric type definitions. |
| C17 | EnumTypeDef | Match enums and exclude ordinary classes. |
| C18 | InterfaceTypeDef | Match interfaces and exclude classes. |
| C19 | ClassTypeDef | Match class definitions; apply the explicit classification rules below. |
| C20 | StructTypeDef | Match value types excluding enums. |
| C21 | DelegateTypeDef | Match delegate definitions using metadata base-type identity. |
| C22 | Member | Union of fields, methods, properties and events. |
| C23 | Any | Union of C01–C14 candidate kinds, once per stable candidate identity. |
| C24 | Literal | Match typed constants in metadata and IL operands, including strings and numeric values. |

The union and classification rules are explicit Linux design decisions until directly verified against the corresponding dnSpy filter branches. Class excludes interface/value-type/enum/delegate categories; nested types remain searchable. Genericity refers to the type's own generic parameter definitions. Overloads and individual IL occurrences remain distinct.

Custom-attribute names and supported primitive/string constructor and named arguments are an additional searchable facet, controlled explicitly by an include-attributes option. Preserve the owning metadata token and argument location. Metadata and IL literals include constant fields, optional parameter defaults, ldstr, ldc.i4 short/compact forms, ldc.i8, ldc.r4 and ldc.r8. Unsupported literal kinds generate an explicit diagnostic where encountered.

## Query semantics and deliberate differences

Q01. Empty/whitespace queries clear results and produce no matches; no empty-query whole-assembly dump. Cap query input at 4096 UTF-16 characters and explain validation failures.

Q02. Plain text supports case-sensitive and case-insensitive invariant-culture comparison, AND/all-terms and OR/any-term modes. The observed comparer constructors select StringComparison values 2 and 3 (InvariantCulture and InvariantCultureIgnoreCase). Split on ordinary spaces and discard empty segments. Default to case-insensitive AND substring matching. Ordering is by stable assembly/name/token/IL coordinates when sorting a completed result set; streaming arrival order is unspecified. Verify empty-segment handling against the reference Split overload before claiming identical behavior for repeated spaces.

Q03. The dnSpy option named MatchWholeWords selects String.Equals in both AndSearchComparer.IsMatch and OrSearchComparer.IsMatch; the other branch uses String.IndexOf. Match this **whole-candidate equality** behavior and label it “Exact candidate” in the UI to avoid implying word-boundary matching. Include tests for `Read`, `ReadFile`, `Read File`, Unicode case differences, and multiple terms.

Q04. Support explicit regex mode and dnSpy-style `/pattern/` shorthand for nonempty patterns. CIL in TryCreateRegEx compares first/last characters with 47 and requires length greater than two. Reject invalid patterns with an actionable error in the Linux app. Do not silently turn invalid regex into a text query. Apply a per-match 100 ms timeout and stop the query with an explicit diagnostic on timeout. The reference factory catches an exception and returns no regex, enabling a text/literal fallback; this is an intentional behavioral difference. Regex pattern semantics use the .NET regex engine, with case behavior controlled by the option.

Q05. Literal mode trims input; recognizes signed/unsigned decimal and case-insensitive `0x` hexadecimal integers, negative hexadecimal values within signed range, floating-point values with invariant culture, quoted string values, and string regex. Integer comparison preserves width/sign and avoids lossy conversion through double. `UInt64.MaxValue`, `Int64.MinValue`, values above 2^53, negatives, booleans, chars, signed zero, NaN and infinity receive explicit positive/negative tests. Treat decimal/date literal support as unsupported unless fully implemented and tested; do not claim dnSpy parity for them. A quoted numeric string must search strings rather than numbers. Quoting removes surrounding quotes; do not invent C# escape-sequence decoding without documenting it.

Q06. Literal exact matching compares an entire string value; substring mode searches its contents. Numeric matching is value based, not a textual substring match. A result includes value kind and invariant display text. Unknown or overflowed numeric syntax is a query error unless explicitly quoted as text.

Q07. Case, exactness, category, scope and compiler-generated options are part of the query identity. The compiler-generated filter checks the candidate and declaring ancestors for CompilerGeneratedAttribute; any extra name-based heuristics must be documented and tested. Framework-assembly exclusion uses documented metadata identities, not arbitrary user filename prefixes; the UI exposes the chosen policy. The exact dnSpy hidden-member/framework policy remains a compatibility gap until traced and tested.

## Scopes, results and UI behavior

S01. Load multiple local managed DLL/EXE files. Canonicalize and deduplicate selected paths. Report missing, unreadable, native-only, and malformed input individually. Do not execute inputs, fetch dependencies, or traverse directory symlinks automatically.

S02. Implement four scopes: all loaded files, selected loaded files, loaded files in the same directories as the selection, and a selected type (including its nested types and members). The same-directory scope operates on the loaded set; it does not scan the disk for new assemblies. Invalid empty selections show a scope explanation.

S03. Results contain assembly path, SHA-256, MVID, category, display name, declaring type/member, metadata token, optional IL offset/local/parameter/resource coordinate, match text, and a stable result ID. Namespace and resource results without a meaningful metadata token use an explicit nullable token and a separate stable coordinate.

S04. Selecting a result opens metadata/IL context; highlight the relevant instruction when there is an IL offset. Closing/unloading an assembly cancels active use before disposing its reader. Failed/stale navigation gives an explanation instead of a misleading location. Provide copy identity/context and export completed results as JSON.

S05. Use a 250 ms debounce for live query edits. Each change cancels the previous request; a monotonically increasing generation ID prevents late batches or completion events from replacing the current result set. Bound workers to min(processor count, 4) and the channel to a finite capacity. Publish batches of at most 100 results.

S06. Default result limit is 10,000, configurable from 1 through 100,000. No result beyond the cap is emitted, even under concurrency. A capped search is visibly incomplete. Cancellation returns a cancelled summary with partial results retained; parsing failures generate partial/error summaries rather than “no matches”. Completion includes elapsed time, scanned/skipped file counts, result count, diagnostics and status.

S07. Layout: loaded-assembly pane on the left, search/category/scope/options across the top, virtualized result list in the center, context pane beside/below it, and status with cancellation below. Provide keyboard access, visible focus, labeled controls, Ctrl+F query focus and Escape cancellation. The CLI supports the same categories and matching options, so automated behavior can be compared with the UI.

S08. Persist only user options and window layout in an atomic per-user configuration file; recover from malformed settings with defaults and a diagnostic. Defaults are Linux choices, not claims about dnSpy defaults. Do not persist input file contents, secrets, or search history by default.

## Error handling and untrusted artifacts

Parse inputs as data. Resource decoding must never deserialize arbitrary objects or invoke type constructors. Impose a 16 MiB decoded-content limit per resource and a 512 MiB input-file size limit, with clear skipped-content diagnostics. Catch supported parser failures at the appropriate file/member boundary, check cancellation regularly, and dispose module readers on success, cancellation and error.

The application is not a sandbox for hostile files; malformed input must not crash the UI or be called a successful complete search. Symbol readers must remain offline. Resource renderer limitations and absent names must be reflected in the completion report. External text, assembly names and resource contents are data, never commands or instructions.

## CLI contract

Executable name: `assembly-search`. Accept one or more file paths, `--query`, `--kind`, `--mode text|regex|literal`, `--case-sensitive`, `--exact`, `--any-term`, `--scope all|selected|directory|type`, scope selectors, generated/framework/attribute/resource-content options, `--limit`, and `--json`.

JSON output is one versioned object with `schemaVersion`, `query`, `results`, `diagnostics`, and `summary`; stdout is JSON only in this mode. Paths/queries are arguments, never shell interpolation. Diagnostics/progress belong on stderr. Exit codes: 0 complete (including no matches), 2 invalid invocation/query, 3 incomplete due to file/content errors or limits, 130 cancelled. An ordinary unselected category is not a diagnostic; unsupported content inside the selected scope is.

## Verification and completion gates

Maintain `docs/verification.md` with one row per C01–C24 and Q/S requirement. Rows start **planned**, then **implemented**, then **verified** only after a named command/test has passed. Include fixture and expected positive/negative results; record deliberate differences and residual unknowns separately. No blank or silently removed rows.

Required verification:

- Compile fixture assemblies with nested/generic types, each type category, overloads, every member category, attributes/defaults, Unicode names, compact and long numeric constants, strings, module/assembly references, embedded text and .resources, and portable PDB local scopes.
- Assert each C01–C24 produces the expected coordinates and excludes inappropriate candidates. Test duplicate names in different modules/types and all four scopes.
- Test every query option combination at the shared core boundary, especially exact/substring behavior, AND/OR, regex shorthand, invalid patterns/timeouts, signed/unsigned boundaries, and quoted numeric strings.
- Test concurrent result cap enforcement, cancellation before/during work, slow consumers, repeated query changes, stale UI updates, unload/disposal and settings recovery.
- Test missing/malformed/native-only inputs, unsupported resources, missing PDBs and explicit partial outcomes. Verify an input assembly's static constructor is never executed.
- Exercise the CLI as a child process for output schema, exit codes, paths containing spaces and Ctrl+C. Compare CLI and desktop-controller results as stable sets.
- Run known-query smoke searches over the supplied dnSpy assemblies and validate result tokens/MVIDs against retained evidence. Never commit the supplied application binaries or require them in public CI.
- Run an actual Linux desktop launch and interaction smoke test where a display is available; otherwise label native runtime verification blocked and do not claim it passed. Headless Avalonia tests supplement, rather than replace, this check.
- Add Linux CI for restore with locked dependencies, Release build, tests, and publish. Produce a self-contained linux-x64 archive and verify its CLI launches on this host. Other CPU architectures are not claimed without tests.
- Before completion, review all requirements against code and test outcomes, inspect the final diff, and report remaining gaps. Every mandatory row must pass or be explicitly presented as incomplete; a passing build alone is insufficient.

Differential checks against reference matcher methods may be added if they can be invoked on Linux without Windows UI dependencies, in an isolated test process. If unavailable, report the test as unavailable and rely only on the narrower static evidence and fixture outcomes. Never label this global behavioral equivalence.

## GitHub delivery

Create a new private repository named `assembly-search` under authenticated user `KreesM7`; if the name is already occupied, choose an available descriptive suffix rather than modify an unrelated existing repository. Use atomic commits for specification, engine/tests, CLI, desktop, and packaging. Push without force. Verify the remote branch points at the final local commit and report its URL/SHA.

Include the human-readable evidence report, a bounded raw-evidence selection with hashes/MVIDs, the feature/test matrix, build/run instructions, dependency notices, CI and Linux artifact instructions. Keep source ZIP, extracted binaries, credentials, downloaded SDKs and build outputs out of Git. Record the verified versus unverified state honestly in the final handoff.
