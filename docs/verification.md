# Verification checklist

All rows are planned. This document records requirements; no product implementation or test run exists yet.

| Requirement | Expected behavior | State | Evidence of test result |
| --- | --- | --- | --- |
| C01 AssemblyDef | Match assembly name/full identity; reject an unrelated assembly. | planned | No implementation/test run yet. |
| C02 ModuleDef | Match module name; verify location is its module. | planned | No implementation/test run yet. |
| C03 Namespace | Match namespace; collapse duplicate namespace candidates within a module. | planned | No implementation/test run yet. |
| C04 TypeDef | Match named and nested types, with qualified names available. | planned | No implementation/test run yet. |
| C05 FieldDef | Match field names and return the owning type/token. | planned | No implementation/test run yet. |
| C06 MethodDef | Match method names, including overloads, with distinct tokens. | planned | No implementation/test run yet. |
| C07 PropertyDef | Match property names; do not fabricate accessor duplicates as properties. | planned | No implementation/test run yet. |
| C08 EventDef | Match event names and owner identity. | planned | No implementation/test run yet. |
| C09 ParamDef | Match named method parameters and preserve sequence/owner. | planned | No implementation/test run yet. |
| C10 Local | Match local identities; use PDB names when supported and explicitly diagnose missing names. | planned | No implementation/test run yet. |
| C11 ParamLocal | Union of parameter and local results; test both and exclude a field. | planned | No implementation/test run yet. |
| C12 AssemblyRef | Match declared referenced-assembly names without loading dependencies. | planned | No implementation/test run yet. |
| C13 ModuleRef | Match declared module references, including a P/Invoke fixture. | planned | No implementation/test run yet. |
| C14 Resource | Match resource names, .resources keys and supported decoded text/primitive values. | planned | No implementation/test run yet. |
| C15 GenericTypeDef | Match only generic type definitions. | planned | No implementation/test run yet. |
| C16 NonGenericTypeDef | Match only nongeneric type definitions. | planned | No implementation/test run yet. |
| C17 EnumTypeDef | Match enums and exclude ordinary classes. | planned | No implementation/test run yet. |
| C18 InterfaceTypeDef | Match interfaces and exclude classes. | planned | No implementation/test run yet. |
| C19 ClassTypeDef | Match class definitions; apply the explicit classification rules below. | planned | No implementation/test run yet. |
| C20 StructTypeDef | Match value types excluding enums. | planned | No implementation/test run yet. |
| C21 DelegateTypeDef | Match delegate definitions using metadata base-type identity. | planned | No implementation/test run yet. |
| C22 Member | Union of fields, methods, properties and events. | planned | No implementation/test run yet. |
| C23 Any | Union of C01–C14 candidate kinds, once per stable candidate identity. | planned | No implementation/test run yet. |
| C24 Literal | Match typed constants in metadata and IL operands, including strings and numeric values. | planned | No implementation/test run yet. |
| Q01 | See the corresponding specification requirement. | planned | No implementation/test run yet. |
| Q02 | See the corresponding specification requirement. | planned | No implementation/test run yet. |
| Q03 | See the corresponding specification requirement. | planned | No implementation/test run yet. |
| Q04 | See the corresponding specification requirement. | planned | No implementation/test run yet. |
| Q05 | See the corresponding specification requirement. | planned | No implementation/test run yet. |
| Q06 | See the corresponding specification requirement. | planned | No implementation/test run yet. |
| Q07 | See the corresponding specification requirement. | planned | No implementation/test run yet. |
| S01 | See the corresponding specification requirement. | planned | No implementation/test run yet. |
| S02 | See the corresponding specification requirement. | planned | No implementation/test run yet. |
| S03 | See the corresponding specification requirement. | planned | No implementation/test run yet. |
| S04 | See the corresponding specification requirement. | planned | No implementation/test run yet. |
| S05 | See the corresponding specification requirement. | planned | No implementation/test run yet. |
| S06 | See the corresponding specification requirement. | planned | No implementation/test run yet. |
| S07 | See the corresponding specification requirement. | planned | No implementation/test run yet. |
| S08 | See the corresponding specification requirement. | planned | No implementation/test run yet. |
| V01 | Fixture coverage including PDBs and resources | planned | No implementation/test run yet. |
| V02 | Concurrent cap, cancellation, stale-generation and disposal tests | planned | No implementation/test run yet. |
| V03 | Malformed/native inputs and explicit partial outcomes | planned | No implementation/test run yet. |
| V04 | CLI schema, exit codes, spaces in paths and cancellation | planned | No implementation/test run yet. |
| V05 | Known-query smoke tests on supplied dnSpy build | planned | No implementation/test run yet. |
| V06 | Actual Linux desktop launch and interaction | planned | No implementation/test run yet. |
| V07 | Linux CI and self-contained linux-x64 publish smoke test | planned | No implementation/test run yet. |
| V08 | Final requirement review and Git remote SHA verification | planned | No implementation/test run yet. |

## Known compatibility boundaries

- Exact category-union/classification branches, empty-term handling and framework/hidden-member policies still need reference checks.
- Decimal/DateTime matching and vendor-specific resource rendering are compatibility gaps unless implemented and verified.
- Regex timeout/error handling is an intentional Linux behavior difference.
- Full C# decompilation and other dnSpy search subsystems are outside the main assembly-search scope.
- Missing PDBs and unsupported resource contents must produce explicit diagnostics.
