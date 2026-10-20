# Verification checklist

Implemented on Linux x86_64. The Release build has no warnings/errors. Test commands and remaining limits are recorded below; partial means the named requirement is not fully verified.

| Requirement | Expected behavior | State | Evidence of test result |
| --- | --- | --- | --- |
| C01 AssemblyDef | Match assembly name/full identity; reject an unrelated assembly. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C02 ModuleDef | Match module name; verify location is its module. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C03 Namespace | Match namespace; collapse duplicate namespace candidates within a module. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C04 TypeDef | Match named and nested types, with qualified names available. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C05 FieldDef | Match field names and return the owning type/token. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C06 MethodDef | Match method names, including overloads, with distinct tokens. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C07 PropertyDef | Match property names; do not fabricate accessor duplicates as properties. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C08 EventDef | Match event names and owner identity. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C09 ParamDef | Match named method parameters and preserve sequence/owner. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C10 Local | Match local identities; use PDB names when supported and explicitly diagnose missing names. | verified | `dotnet test -c Release`: SymbolTests; all 24 CLI category invocations also pass. |
| C11 ParamLocal | Union of parameter and local results; test both and exclude a field. | verified | `dotnet test -c Release`: SymbolTests; all 24 CLI category invocations also pass. |
| C12 AssemblyRef | Match declared referenced-assembly names without loading dependencies. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C13 ModuleRef | Match declared module references, including a P/Invoke fixture. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C14 Resource | Match resource names, .resources keys and supported decoded text/primitive values. | verified | `dotnet test -c Release`: ResourceTests / ResourceBoundaryTests; all 24 CLI category invocations also pass. |
| C15 GenericTypeDef | Match only generic type definitions. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C16 NonGenericTypeDef | Match only nongeneric type definitions. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C17 EnumTypeDef | Match enums and exclude ordinary classes. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C18 InterfaceTypeDef | Match interfaces and exclude classes. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C19 ClassTypeDef | Match class definitions; apply the explicit classification rules below. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C20 StructTypeDef | Match value types excluding enums. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C21 DelegateTypeDef | Match delegate definitions using metadata base-type identity. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C22 Member | Union of fields, methods, properties and events. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C23 Any | Union of C01–C14 candidate kinds, once per stable candidate identity. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| C24 Literal | Match typed constants in metadata and IL operands, including strings and numeric values. | verified | `dotnet test -c Release`: CategoryTests; all 24 CLI category invocations also pass. |
| Q01 | See the corresponding specification requirement. | verified | `QueryCompilerTests`, `LiteralMatcherTests`; Q07 uses `CategoryTests.GeneratedMembersAreOptIn` and `BoundaryAuditTests.FrameworkPolicyExcludesCoreLibraryButKeepsUnsignedSystemName`. Reference-policy differences remain explicit. |
| Q02 | See the corresponding specification requirement. | verified | `QueryCompilerTests`, `LiteralMatcherTests`; Q07 uses `CategoryTests.GeneratedMembersAreOptIn` and `BoundaryAuditTests.FrameworkPolicyExcludesCoreLibraryButKeepsUnsignedSystemName`. Reference-policy differences remain explicit. |
| Q03 | See the corresponding specification requirement. | verified | `QueryCompilerTests`, `LiteralMatcherTests`; Q07 uses `CategoryTests.GeneratedMembersAreOptIn` and `BoundaryAuditTests.FrameworkPolicyExcludesCoreLibraryButKeepsUnsignedSystemName`. Reference-policy differences remain explicit. |
| Q04 | See the corresponding specification requirement. | verified | `QueryCompilerTests`, `LiteralMatcherTests`; Q07 uses `CategoryTests.GeneratedMembersAreOptIn` and `BoundaryAuditTests.FrameworkPolicyExcludesCoreLibraryButKeepsUnsignedSystemName`. Reference-policy differences remain explicit. |
| Q05 | See the corresponding specification requirement. | verified | `QueryCompilerTests`, `LiteralMatcherTests`; Q07 uses `CategoryTests.GeneratedMembersAreOptIn` and `BoundaryAuditTests.FrameworkPolicyExcludesCoreLibraryButKeepsUnsignedSystemName`. Reference-policy differences remain explicit. |
| Q06 | See the corresponding specification requirement. | verified | `QueryCompilerTests`, `LiteralMatcherTests`; Q07 uses `CategoryTests.GeneratedMembersAreOptIn` and `BoundaryAuditTests.FrameworkPolicyExcludesCoreLibraryButKeepsUnsignedSystemName`. Reference-policy differences remain explicit. |
| Q07 | See the corresponding specification requirement. | verified | `QueryCompilerTests`, `LiteralMatcherTests`; Q07 uses `CategoryTests.GeneratedMembersAreOptIn` and `BoundaryAuditTests.FrameworkPolicyExcludesCoreLibraryButKeepsUnsignedSystemName`. Reference-policy differences remain explicit. |
| S01 | See the corresponding specification requirement. | verified | CategoryTests.RejectsBadInputs; BoundaryAuditTests.OversizedSparseFileIsRejectedBeforeReading; ScopeTests.SymlinksDeduplicate; ReviewRegressionTests cover symlinked parents and symlink/.. resolution. |
| S02 | See the corresponding specification requirement. | verified | ScopeTests; SearchServiceTests.SelectedTypeExcludesOtherTypes. |
| S03 | See the corresponding specification requirement. | verified | CategoryTests; CLI JSON tests; original-build token smoke below. |
| S04 | See the corresponding specification requirement. | partial | ContextReaderTests; MainWindowTests.LoadSearchAndSelectShowsContext; OS clipboard/file picker/export dialogs not independently exercised. |
| S05 | See the corresponding specification requirement. | partial | SearchControllerTests.SupersededSearchNeverReturnsStaleRows; MainWindowTests regression; worker/batch bounds in SearchServiceTests. Exact 249/250 ms boundary is not independently measured. |
| S06 | See the corresponding specification requirement. | verified | SearchServiceTests.ResultLimitIsExactUnderConcurrency, PreCancellationIsVisible, AbandonedConsumerDisposes; BoundaryAuditTests.CancellationRetainsAlreadyPublishedBatches. |
| S07 | See the corresponding specification requirement. | partial | MainWindowTests keyboard/control/selection/rendering tests. Native OS input and full accessibility audit remain unverified. |
| S08 | See the corresponding specification requirement. | partial | SettingsStoreTests round-trip, malformed recovery and geometry; interrupted-write fault injection not exercised. |
| V01 | Fixture coverage including PDBs and resources | verified | Built SearchFixture/ReferenceFixture with portable PDB, resource/attribute/symbol and category tests; resource primitive bytes generated independently in tests. |
| V02 | Concurrent cap, cancellation, stale-generation and disposal tests | verified | SearchServiceTests, SearchControllerTests and BoundaryAuditTests; cancellation/caps/stale-generation/abandoned-consumer behavior. |
| V03 | Malformed/native inputs and explicit partial outcomes | verified | CategoryTests.RejectsBadInputs; SymbolTests.MissingSymbolsAreExplicit; ResourceBoundaryTests; SearchServiceTests.FailuresArePartial. |
| V04 | CLI schema, exit codes, spaces in paths and cancellation | verified | CLI process tests cover JSON, all 24 kinds, invalid arguments, spaces and exits 0/2/3; actual SIGINT against dnSpy search returned exit 130 and valid Cancelled JSON. |
| V05 | Known-query smoke tests on supplied dnSpy build | verified | Supplied dnSpy.dll, query StartSearch, MethodDef: seven matches; SearchControlVM.StartSearch token 0x06001258, SHA/MVID matched retained evidence. |
| V06 | Actual Linux desktop launch and interaction | partial | Native desktop launched with dnSpy.dll and remained running. Headless controls/search/selection/keyboard/rendering verified; native OS interactions unavailable through this session’s computer surface. |
| V07 | Linux CI and self-contained linux-x64 publish smoke test | verified | Release build, locked restore, self-contained linux-x64 package and package-smoke.sh pass locally. GitHub CI execution pending repository delivery. |
| V08 | Final requirement review and Git remote SHA verification | partial | Independent review complete; all three reported defects fixed with failing/passing regression tests. Remote commit/tree verification is recorded in the delivery receipt; connector-created commit SHAs differ from local author/date objects. |

## Known compatibility boundaries

- Exact category-union/classification branches, empty-term handling and framework/hidden-member policies still need reference checks.
- Decimal/DateTime matching and vendor-specific resource rendering are compatibility gaps unless implemented and verified.
- Regex timeout/error handling is an intentional Linux behavior difference.
- Full C# decompilation and other dnSpy search subsystems are outside the main assembly-search scope.
- Missing PDBs and unsupported resource contents must produce explicit diagnostics.

## Commands and runtime evidence

- `dotnet restore --locked-mode`: passed after pinning linux-x64 runtime targets.
- `dotnet build -c Release --no-restore`: passed, zero warnings/errors.
- `dotnet test -c Release --no-build`: core, CLI process and desktop/controller suites passed; final counts are recorded in the delivery report after review.
- `scripts/publish-linux.sh` and `scripts/package-smoke.sh`: passed; 78 MiB self-contained archive, extracted to a path with spaces for smoke verification.
- Strict frontend static audit: zero findings. This is not a native accessibility audit.
- Known query on the supplied dnSpy.dll: Complete, seven results, approximately 2.2 seconds; required StartSearch token verified.
- Desktop preview is a headless rendered screenshot, not a screenshot proving native OS interaction.

## Final review results

A fresh independent reviewer inspected the full implementation and independently passed the original 133 Release tests. The single fix pass added seven regression cases: canonical parent-directory aliases and symlink/.. semantics, three exponent overflows, and both directions of literal mode/category transitions. All **140 Release tests pass** (90 Core, 34 CLI, 16 Desktop); the Release build has zero warnings/errors. Explicit Infinity and NaN support remains covered. See [execution notes](execution-notes.md) for review rulings and limits.
