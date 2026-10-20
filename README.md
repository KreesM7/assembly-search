# Assembly Search

Linux desktop and CLI search for .NET assembly metadata, IL literals, local symbols and supported resources. It implements all 24 main-window search categories identified in the supplied dnSpy build. It reads DLL/EXE files as data and never executes their code.

## Run the Linux package

Download the `assembly-search-linux-x64` artifact from a successful GitHub Actions Linux run, or build it with `scripts/publish-linux.sh`. Extract `assembly-search-linux-x64.tar.gz`, then run:

```bash
./desktop/assembly-search-desktop /path/to/MyApp.dll
./cli/assembly-search /path/to/MyApp.dll --query Read --kind MethodDef --json
```

The package includes the .NET runtime. It targets Linux x86_64. The desktop needs a working X11/Wayland desktop, fontconfig/fonts and the native libraries required by Avalonia; the CLI needs the standard native dependencies of .NET 10, including ICU and OpenSSL. The local verification host is Linux x86_64 with a graphical session. Other distributions and CPU architectures are not claimed as tested.

## Desktop workflow

Open multiple assemblies, choose category/matching mode and scope, and enter a query. Search edits restart after 250 ms; Enter/Search runs immediately. Clear cancels pending work. Ctrl+F focuses the query; Escape cancels while retaining partial results. Select a result to inspect its assembly path, SHA-256, MVID, metadata token and IL excerpt. Copy exports the selected identity/context; Export JSON writes results with diagnostics and completion status.

Use Ctrl/Shift in the assembly list to select files for selected-file or same-directory scope. Same-directory scope searches only files already loaded. Type scope takes the qualified type name; nested types use `/`. Removing an assembly unloads its current results without changing the file on disk.

## CLI examples

```bash
# Text terms default to AND and invariant-culture case-insensitive substring matching.
assembly-search MyApp.dll --query 'Read File' --kind MethodDef
assembly-search MyApp.dll --query 'Read Write' --kind Member --any-term

# /.../ shorthand or explicit regex mode; 100 ms per-match timeout.
assembly-search MyApp.dll --query '/Read.*/' --kind MethodDef --json

# Typed literals distinguish numbers from quoted strings.
assembly-search MyApp.dll --query 0xffffffffffffffff --kind Literal --json
assembly-search MyApp.dll --query '"123"' --kind Literal --exact

# Search only selected loaded files; include generated members or attributes explicitly.
assembly-search A.dll B.dll --query Token --scope selected --selected A.dll --include-generated
assembly-search A.dll --query Author --include-attributes
```

`--exact` means whole-candidate equality, matching the observed dnSpy comparator behavior; it is not a word-boundary matcher. `--kind` accepts AssemblyDef, ModuleDef, Namespace, TypeDef, FieldDef, MethodDef, PropertyDef, EventDef, ParamDef, Local, ParamLocal, AssemblyRef, ModuleRef, Resource, GenericTypeDef, NonGenericTypeDef, EnumTypeDef, InterfaceTypeDef, ClassTypeDef, StructTypeDef, DelegateTypeDef, Member, Any, Literal. `--help` lists all flags.

JSON schemaVersion 1 contains `query`, `results`, `diagnostics`, and `summary`; literal values are encoded using type/display strings, preserving nonfinite float output as valid JSON. Diagnostics go to stderr as well. Exit codes: 0 complete, 2 invalid arguments/query, 3 incomplete (including limits or coverage gaps), 130 cancelled. A no-match complete query exits 0. The default 10,000-result cap is configurable to 100,000; capped searches are visibly incomplete.

## Build and test

Install the .NET SDK pinned by global.json, then:

```bash
dotnet restore --locked-mode
dotnet build -c Release --no-restore
dotnet test -c Release --no-build
dotnet run --project src/AssemblySearch.Desktop -- /path/to/MyApp.dll
scripts/publish-linux.sh
scripts/package-smoke.sh
```

The fixtures are built from source; the supplied dnSpy binaries are not needed by CI. See [verification](docs/verification.md) for actual tests, checks and remaining gaps.

## Evidence and compatibility boundaries

Read the [binary findings](docs/evidence/findings.md), [follow-up matching audit](docs/evidence/parity-audit.md), [approved specification](docs/superpowers/specs/2026-10-20-assembly-search-design.md) and [implementation plan](docs/superpowers/plans/2026-10-20-assembly-search.md). The complete REA exports remain in the original workspace's sibling `dnspy-search-evidence/` directory; selected evidence is included here.

This is a search-focused application. It does not implement dnSpy's debugger, assembly editor, C# decompiler, hex/text-editor searches, StringSearcher extension or plug-in resource renderers. Resource contents support strict UTF-8 text and standard .resources primitive/string records; arbitrary serialized objects, BAML and other vendor formats are reported unsupported without deserialization. Decimal/DateTime literal matching is not claimed. Missing adjacent portable PDBs produce synthetic IL local names and an explicit coverage diagnostic.

Deliberate differences include invalid-regex errors rather than dnSpy's fallback, regex timeouts, documented Linux defaults, AND/OR union/classification policy, invariant numeric parsing, and a NaN-equals-NaN literal policy. Framework exclusion uses known public-key tokens paired with framework assembly names; it is not claimed identical to dnSpy's hidden-member policy. See verification.md for native OS interaction and compatibility limits.

Only options and window geometry are persisted, under `$XDG_CONFIG_HOME/assembly-search/settings.json` (or the platform's per-user configuration directory). Search history and input file contents are not stored.
