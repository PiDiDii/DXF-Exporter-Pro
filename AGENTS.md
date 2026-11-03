# Project Agent Instructions

Before implementing or reviewing project changes, read:

- `Skill/AGENTS.md`
- `Skill/PROJECT_GUIDE.md`
- `Skill/skills/SKILL.md`
- `Skill/references/API_SOURCES.md`
- `Skill/references/API_VERIFICATION.md`

For Inventor API work, also read the matching topic under `Skill/skills/references/` and consult `Skill/api/API_INDEX.md`. Follow these rules:

- Target Autodesk Inventor 2027 unless the task explicitly specifies another release.
- Verify exact API members, C# signatures, enums, context, and behavior against the installed 2027 API Help/interop or an Autodesk 2027 SDK sample. Never infer signatures from memory, another release, or VBA.
- Keep reference verification, compile verification, automated tests, and Inventor runtime testing distinct. Never claim a test that was not run.
- Inspect the existing project and preserve its framework, architecture, and working behavior unless the task requires a change.
- Handle COM lifetimes, events, document state, UI threading, and export side effects deliberately; report errors rather than silently continuing.
- Make focused changes, build and run available tests, and report changed files, commands/results, API evidence, and remaining limitations.
