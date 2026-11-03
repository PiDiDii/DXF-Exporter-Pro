# Agent Rules — Autodesk Inventor 2027

Read `PROJECT_GUIDE.md`, `references/API_SOURCES.md`, and the Inventor Add-in skill before implementation.

## Mandatory
1. Target Autodesk Inventor 2027 unless the task explicitly states another version.
2. Verify every Inventor API member, signature, enum value, and behavior using the Inventor 2027 API reference or installed 2027 SDK. Do not infer from memory or another release.
3. When documenting a claim about Inventor API, provide a direct reference link and identify the API topic/member. Distinguish official Autodesk documentation from third-party reference sites.
4. Do not fabricate APIs, SDK paths, template names, API signatures, or test results.
5. Inspect the existing repository and its project configuration before modifying framework, package, architecture, or deployment settings.
6. Prefer the smallest change that meets the task. Preserve existing working iLogic/code where requested; reuse or wrap it rather than rewriting without justification.
7. Build and run the available tests after code changes. If Inventor 2027 is unavailable in the environment, state clearly which validation was not possible.
8. Handle COM objects, events, document lifecycle, and Inventor UI thread requirements carefully. Follow SDK samples and verify behavior.
9. Do not assume API samples in VBA translate directly to C# without checking COM types and syntax.
10. Report changed files, build/test commands, actual results, and remaining risks.

## API citation format
In implementation notes, cite:
- Source title
- URL
- Object/member
- Version checked (Inventor 2027)
- Any caveat or missing validation
