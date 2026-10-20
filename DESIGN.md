---
name: Assembly Search
register: product
colors:
  accent: '#176B87'
  ink: '#182433'
  muted: '#596879'
  surface: '#F3F5F7'
  paper: '#FFFFFF'
  border: '#D5DEE7'
typography:
  body:
    fontFamily: 'sans-serif'
    fontSize: '14px'
  code:
    fontFamily: 'monospace'
    fontSize: '13px'
spacing:
  compact: '6px'
  normal: '12px'
  section: '20px'
rounded:
  DEFAULT: '4px'
---
# Native assembly inspection workbench

The audience is a developer investigating .NET metadata. The approved product specification owns scope and matching behavior. Use familiar Fluent desktop controls, a loaded-assembly rail, dense search results, and a monospace context pane with an IL offset marker. Metadata identity is the signature element: a selected result exposes its assembly hash, MVID, token and IL coordinate alongside the relevant instructions.

The Avalonia Fluent theme owns system-theme colors, selection, focus, input geometry and scrolling. `App.axaml` is the single application theme adapter; accent and spacing are applied there, not separately in individual controls. The palette above describes the light theme reference; dark theme follows system Fluent resources. The native sans/monospace families resolve to installed platform fonts. This is one desktop workflow; there are no sibling screens or web routes.

Use sentence-case instructions and explicit recovery states. No animation is needed. Keep controls reachable at 1000×650 and use splitter/scrollable panes; at narrower supported widths, preserve horizontal scrolling rather than hide search controls. Selection must reveal full strings in context. Native ListBox, ComboBox, TextBox, CheckBox, Button and ScrollViewer own keyboard semantics and visible focus.
