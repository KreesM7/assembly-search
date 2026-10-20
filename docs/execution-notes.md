# Execution and review notes

The user approved native desktop + CLI implementation, the specification, plan and GitHub publication. The project is search-focused; full dnSpy application parity is not claimed.

# SDD ledger — plan: docs/superpowers/plans/2026-10-20-assembly-search.md
Execution: Native, approved by user. Start commit: 479e0c1.
Ruling: Work in the newly created task-owned repository on feature/linux-search rather than a second checkout — there is no pre-existing user code or shared branch to protect and the output path is the deliverable — cost if wrong: branch can be moved to a linked checkout later.
Pre-flight: Tasks 1→2/4/5/6 consume SearchQuery and matcher; consistent.
Pre-flight: Tasks 2→3/4/7 consume candidates and identity; consistent.
Pre-flight: Tasks 4→5/6 consume request/session/summary; consistent.
Pre-flight: Tasks 6→7 consume controller/view model; observable members now explicit.
Ruling: Share JSON serialization in Core from Task 5 so Desktop has no CLI dependency — plan explicitly permits this — cost if wrong: relocate a serializer.
Baseline: documentation-only repository, clean; no existing product tests.
Task 1: complete (commits 479e0c1..6f34953, tests: dotnet test → Passed!  - Failed:     0, Passed:    27, Skipped:     0, Total:    27, Duration: 119 ms - AssemblySearch.Core.Tests.dll (net10.0))
Task 2: Ruling: overload assertion selects Sample.Widget — the fixture also contains IReadable.Read, so an assembly-wide count of two was wrong — cost if wrong: class-specific overload coverage only; separate category tests cover interfaces.
Task 2: complete (commits 6f34953..b5f645b, tests: dotnet test → Passed!  - Failed:     0, Passed:    56, Skipped:     0, Total:    56, Duration: 124 ms - AssemblySearch.Core.Tests.dll (net10.0))
Task 3: Ruling: generate primitive .resources data in the resource tests using ResourceWriter — MSBuild requires System.Resources.Extensions for non-string resx input, an unnecessary product dependency — cost if wrong: numeric resource tests use a generated resource rather than MSBuild resx.
Task 3: complete (commits b5f645b..d35f2b4, tests: dotnet test → Passed!  - Failed:     0, Passed:    67, Skipped:     0, Total:    67, Duration: 150 ms - AssemblySearch.Core.Tests.dll (net10.0))
Task 4: complete (commits d35f2b4..88151ff, tests: dotnet test → Passed!  - Failed:     0, Passed:    82, Skipped:     0, Total:    82, Duration: 179 ms - AssemblySearch.Core.Tests.dll (net10.0))
Task 5: complete (commits 88151ff..d0e9322, tests: dotnet test → Passed!  - Failed:     0, Passed:    34, Skipped:     0, Total:    34, Duration: 5 s - AssemblySearch.Cli.Tests.dll (net10.0))
Task 6: Ruling: use xunit.v3 3.2.2 in Desktop.Tests only — Avalonia.Headless.XUnit 12.1.4 depends on xunit.v3.extensibility.core 3.2.2 and conflicts with the planned xunit2 package — cost if wrong: test-harness migration; product interfaces unchanged.
Task 6: Ruling: finalize the task ledger with its controller/settings test filter — Task 7 UI scaffolding entered while the asynchronous full-suite gate was running and caused a new test-API compile failure; focused Task 6 tests had already passed — cost if wrong: completion ordering in the ledger; final suite remains mandatory.
Task 6: complete (commits d0e9322..efd8eb1, tests: dotnet test tests/AssemblySearch.Desktop.Tests --filter 'FullyQualifiedName~SearchControllerTests|FullyQualifiedName~SettingsStoreTests' → Passed!  - Failed:     0, Passed:     8, Skipped:     0, Total:     8, Duration: 1 s - AssemblySearch.Desktop.Tests.dll (net10.0))
Task 7: complete (commits efd8eb1..d2360f8, tests: dotnet test → Passed!  - Failed:     0, Passed:    34, Skipped:     0, Total:    34, Duration: 5 s - AssemblySearch.Cli.Tests.dll (net10.0))
Task 7: Ruling: native launch verified but native OS dialog/keyboard interaction cannot be inspected with this session’s browser-only computer surface — headless UI tests cover interaction and V06 remains partial — cost if wrong: native integration defects may remain.
Task 8: Ruling: pin linux-x64 runtime identifiers in the shared build props — publishing changed NuGet lock targets and the next locked restore failed NU1004 — cost if wrong: other architectures require explicit configuration and validation.
Task 8: Ruling: use authenticated GitHub connector delivery if Git transport remains unavailable — no SSH key is authorized on this host and no token will be extracted from the browser — cost if wrong: remote commit history may be a delivery snapshot rather than the full local incremental history.
Final independent review: fresh gpt-6-astra reviewer inspected 479e0c1..968f5e6 and independently passed all 133 pre-fix Release tests. Two Important defects and one Minor were reported.
Final review ruling: regrade floating exponent overflow from Minor to Important — Q06 requires rejecting numeric overflow, and accepting 1e999 as Infinity can return wrong matches — cost if wrong: stricter validation than the reference. Fixed in the same single review fix pass.
Final review fixes: parent-component symlink canonicalization (2 RED tests → 2 GREEN), numeric exponent overflow (3 RED → 3 GREEN), and synchronized literal mode/category controls (2 RED → 2 GREEN). No deferred review findings. Full Release suite now 140/140 passing (90 Core, 34 CLI, 16 Desktop), zero build warnings/errors; locked restore passes.
Final review rulings: decline native OS interaction claims because this session exposes only browser UI automation; keep V06 partial (cost: native integration remains unverified). Decline exact dnSpy runtime parity because reference evidence is static and documented gaps remain (cost: compatibility differences). Remote publication and hosted CI are delivery work, not product defects; finish them before final report (cost if wrong: deployment evidence incomplete).
Task 8: complete (commits d2360f8..92f208e, tests: dotnet test -c Release --no-build → Passed!  - Failed:     0, Passed:    34, Skipped:     0, Total:    34, Duration: 5 s - AssemblySearch.Cli.Tests.dll (net10.0))
Final delivery ruling: authenticated Git transport is unavailable, so mirror every local incremental commit via GitHub Git data APIs and compare every tree SHA. Remote commit hashes differ because the API supplies new author/date objects and adds an initialization parent; file trees are identical — cost if wrong: local and remote history cannot be fast-forwarded without reconciling commit identities. Preserve the mapping in the delivery receipt. No force push or browser credential extraction.
Final: fixed directory aliases — ParentDirectoryAliasesScanAssemblyOnlyOnce and ParentTraversalUsesResolvedDirectory RED→GREEN, suite 140/140.
Final: fixed floating overflow — OverflowingFloatIsNotExplicitInfinity (three values) RED→GREEN, suite 140/140.
Final: fixed literal control mismatch — SwitchingAwayFromLiteralRestoresDisplayedCategory and ExplicitLiteralModeMakesCategoryTruthful RED→GREEN, suite 140/140.
Publication: private KreesM7/assembly-search/main contains all source/evidence, remote 938ec162bf09134ab514a6b12f1f16b60f04a3f4 corresponds to local 92f208e5b841be88f287c6d2435c0e222184ee67 with identical tree 4730fe0b0b457599f732726d5eb3d6b9c2ec6f7c. GitHub Linux workflow running; final outcome is recorded in the external delivery receipt after this documentation commit.
