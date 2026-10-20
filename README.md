# Assembly Search

A planned Linux desktop application and CLI for searching .NET assemblies, informed by static analysis of the user-supplied dnSpy build.

**Current state:** design and evidence only. No application has been implemented or tested yet.

- [Written specification](docs/superpowers/specs/2026-10-20-assembly-search-design.md)
- [Initial dnSpy findings](docs/evidence/findings.md)
- [Follow-up parity audit and corrections](docs/evidence/parity-audit.md)
- [Implementation verification checklist](docs/verification.md)

The initial findings describe the proposal at the end of the first investigation. The written specification and parity audit supersede that proposal where they add categories or refine matching rules. This repository contains selected evidence receipts; the full exported REA bundles remain beside the project in `../dnspy-search-evidence/` in the original local workspace and are not required to read the selected evidence.

The inspected dnSpy binaries are not included. Evidence records retain their assembly hashes, MVIDs and build-local metadata tokens. Static observations do not establish runtime equivalence.
