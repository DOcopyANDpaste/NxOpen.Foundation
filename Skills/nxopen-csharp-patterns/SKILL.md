---
name: nxopen-csharp-patterns
description: "Generates NXOpen automation code in C# (.NET) following the team's layered architecture. Use this skill WHENEVER writing, scaffolding, or reviewing NXOpen C# code — including Session/Part manipulation, feature creation, geometry automation, batch (ugbatch) programs, menu/ribbon entry points, and Block UI Styler dialogs. Trigger on any mention of NXOpen, NX Open, ugbatch, Block UI Styler, .dlx, NXException, Session.GetSession, or requests to build an NX tool, macro, dialog, or automation in C#. Apply this skill even when the user just pastes journal-recorded code and asks to 'clean it up' or 'make it production-ready' — that is exactly when these patterns matter most."
---

# NXOpen C# Coding Patterns

Generates production NXOpen automation in C# that is testable, reusable, and survives NX version upgrades. This skill enforces a layered architecture that keeps NXOpen types out of business logic.

## The one rule everything else serves

**NXOpen types (`Session`, `Part`, `NXObject`, `Feature`, `Body`, `Tag`, ...) never appear in business logic.** They live only in an adapter layer behind interfaces. This is what makes code unit-testable — you cannot instantiate NXOpen objects outside a running NX session, so any logic touching them directly is untestable.

If you find yourself writing domain logic (calculations, decisions, validation, orchestration rules) in a method that also calls `theSession.Parts.Work` or similar, stop and split it.

## Layers

```
Core/             — pure domain logic, ZERO NXOpen references (unit-testable)
NxAdapters/       — ALL NXOpen calls live here, behind interfaces
Shared.Contracts/ — interfaces + DTOs (the seam between Core and NX)
Automation/       — entry points: .dll (menu/ribbon) or .exe (ugbatch)
Ui.Dialogs/       — Block UI Styler output + presenters (only if building dialogs)
```

Code flows: entry point → resolves adapters → calls Core logic → Core returns plain DTOs → entry point/adapter applies results to NX.

A per-project `Contracts` layer is optional — some projects fold it directly into `Core` instead of keeping it as a separate project, when the seam it would enforce isn't load-bearing for that project's size. Either way, the shared, cross-project foundation this skill's own patterns are built from lives in `NxOpen.Foundation` (Contracts / Core / NxAdapters tiers), one directory up from any individual NX Open project — reuse generic plumbing (session access, undo scope, listing-window logging, material-library reading, the rule-engine shape) from there via a relative `ProjectReference` before writing a new copy in a project.

## Choose the variant

Read the reference file that matches the task. Read `common.md` in all cases.

| Task | Read |
|------|------|
| Any NXOpen C# task (baseline patterns) | `references/common.md` |
| Batch/menu tool, feature creation, geometry, no dialog | `references/without-block-ui.md` |
| A tool with a Block UI Styler dialog (`.dlx`) | `references/with-block-ui.md` |

For a dialog-based tool you read `common.md` + `with-block-ui.md`. For everything else you read `common.md` + `without-block-ui.md`.

## Non-negotiables (apply to all generated code)

1. **No journal-recorded code as-is.** Journaled output is a reference only. Refactor it into the layers above before it's production. Never emit raw `Menu:`/record-style code.
2. **Undo marks always.** Wrap every operation set in `Session.SetUndoMark` and roll back to the mark on failure. Use the disposable wrapper pattern in `common.md` — never leave marks unmanaged.
3. **Catch `NXException`, log the error code.** Never swallow. `ex.ErrorCode` matters more than `ex.Message` for support.
4. **No hardcoded install paths.** Reference NXOpen assemblies via an env var (`UGII_BASE_DIR`), never `C:\Program Files\Siemens\...`.
5. **Explicit Session/Part ownership.** Every generated component states who opens, owns, and closes the part. No ambiguous shared active-part assumptions.
6. **Plain DTOs cross the Core boundary** — never pass an NXOpen object into or out of Core.

## Output expectations

- Emit code split across the correct layers, not one monolithic file. If the user wants a single file for a quick tool, still separate the classes logically and say why the split matters.
- For anything over ~20 lines, write actual files in the project structure rather than inline snippets.
- When cleaning up journaled code, show the before→after mapping briefly so the user learns the pattern.
- If the NX version is unknown and it affects the API used, flag it — do not silently assume a version.
