# Project Guide — Inventor 2027 Add-in

## Project identity
- Host application: Autodesk Inventor 2027
- Primary language: C#
- Add-in API: Inventor COM API
- Framework/runtime: verify against the installed Inventor 2027 API Help and the project's established configuration. The 2027 `What's New` topic recommends .NET 10; older SDK wizard notes refer to .NET 8. Don't blindly migrate an existing project.
- Primary source of truth: local 2027 SDK/API Help, with official Autodesk online references as corroboration.

## Engineering workflow
1. Inspect solution, target framework, references, manifest, and existing architecture.
2. Identify exact Inventor API topics involved.
3. Verify signatures and behavior from 2027 sources.
4. Design the smallest implementation and describe assumptions.
5. Implement; avoid unrelated refactors.
6. Build and run repository tests.
7. Test in Inventor 2027 where possible; distinguish build verification from host-runtime verification.
8. Provide a concise implementation report and source links.

## Reliability
- Respect Inventor document types and active-document state.
- Treat COM references and event subscriptions as lifecycle-sensitive.
- Avoid UI blocking for long operations; keep UI interactions on the appropriate thread.
- Use transactions/undo support where applicable and verified.
- Use defensive checks for missing documents, unsupported file types, unresolved references, and unavailable Flat Patterns.
- Do not assume a feature exists or is healthy merely because a cast succeeds; check relevant state.
- Keep external dependencies isolated and manifest settings consistent with the SDK.
- Do not put credentials, customer data, or proprietary model content in logs.

## API evidence
For each nontrivial API choice, link the API reference or SDK sample. For behavior not explicitly documented, label it as an assumption and test it in Inventor.
