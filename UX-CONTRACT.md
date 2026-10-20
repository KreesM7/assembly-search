# Assembly Search interaction owners

Source: approved specification in docs/superpowers/specs/2026-10-20-assembly-search-design.md.

| Capability | Canonical owner | Source of truth | Allowed variants | Verification |
| --- | --- | --- | --- | --- |
| Table Selection | Avalonia ListBox | approved specification S04/S07 | single result, multiple assemblies | MainWindowTests, NavigationTests |
| Select/Listbox | Avalonia ComboBox | DESIGN.md and Q/S requirements | category, mode, scope | MainWindowTests keyboard/popup |
| Form | MainWindowViewModel and native Avalonia inputs | approved query specification | query/options | QueryCompilerTests, SearchControllerTests |
| Scrollbar | Avalonia Fluent theme | DESIGN.md | result/context/file panes | headless screenshot, desktop launch |
| Search lifecycle | SearchController | S05/S06 | debounced edits, immediate clear/submit | SearchControllerTests |
| Feedback | persistent status and diagnostic pane | S06 | complete/incomplete/cancelled/invalid | MainWindowTests |

Input assemblies remain local and are parsed as data. No query history or file contents are persisted. There is no deletion, billing, account, network backend, or externally published UI action. File pickers and clipboard use OS integration; failures remain visible in the app. Removing a loaded file is reversible and needs no confirmation.

Query changes cancel prior work with a 250 ms debounce. Clear and explicit Enter are immediate. Superseded results cannot replace the latest generation. Search text is not put into a URL because this is a native desktop app. Errors retain query/options and explain recovery. Empty results distinguish no matches from missing files/unsupported content. Resource/PDB gaps remain persistent diagnostics rather than transient toasts.
