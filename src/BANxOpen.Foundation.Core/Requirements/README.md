# Part requirements

A feature tool writes the rules its features rely on into the part as **native NX Requirements**. Each one is linked to the expression it checks by a **Requirement Check**. NX re-evaluates the checks on every change, so a rule keeps guarding the part when someone changes it with no BANxOpen tool loaded (for example, Sheet Metal Preferences edited in NX's own dialog). Every other tool that judges the part reads the same requirements, so NX and the tools can't disagree.

> The tool generates the requirement (from its SPEC data); the part holds it, and everyone validates against it.

## Pieces

| Where | What |
|---|---|
| `Contracts/Requirements/RequirementSpec.cs` | `RequirementSpec`: one requirement, identified by (Domain, Key, Aspect). It's either a `SetOfValuesRule` (String) or an `EqualsValueRule` (Number + tolerance), checked against `CheckedFormula`, and highlights `AssociatedObjectKeys` (feature tags). |
| `Core/Requirements/RequirementNaming.cs` | NX name `BA_REQ_<DOMAIN>_<KEY>_<ASPECT>`, check name `<name>_CHECK`, and the `#BA_REQ v1 …` bookkeeping line appended to the description. That line carries the exact identity and decides ownership on read. |
| `Core/Requirements/RequirementDiff.cs` | Pure create / replace / delete diff, so the store's bookkeeping is unit-tested. It refuses keys that collapse to the same NX name. |
| `Core/Requirements/IPartRequirementStore.cs` | `Read(domain)` and `Refresh(domain, specs)`. A domain only ever touches its own requirements. |
| `NxAdapters/Requirements/NxPartRequirementStore.cs` | NX implementation (`NXOpen.Validate`, NX 2412): `RequirementBuilder` → `Commit`, `Requirement.NewCheck(name, formula)`, `SetAssociatedObjects`, `SetUpdateControl(EveryChange)`. A changed requirement is replaced (deleted and re-created), not edited. |
| `NxAdapters/Requirements/NxExpressionLock.cs` | Locks tool-owned expressions (`IsNoEdit`) and unlocks them only for the tool's own edit. |

## Rules for callers

- **Call `Refresh` inside your Apply undo mark.** A failure part-way leaves the part half-written, and only your rollback restores it.
- **Never refresh from a failed read.** If the features or the requirements couldn't be read, fail instead: an empty list deletes every requirement the domain holds.
- **Key requirements by the thing that owns the driving values.** For example, sheet metal keys them by Standard + SPEC, the same identity as the SPEC's shared parameter expressions.

## Adding a feature domain

The sheet metal domain (`BANxOpen.SheetMetal`, `Requirements/`) is the worked example:

1. Map your feature data to `RequirementSpec`s. For sheet metal, `SheetMetalRequirementMapper` produces a GRADE set and a THICKNESS equality per SPEC.
2. Group your features into requirements, the way `BeadRequirementFactory` does.
3. Call `store.Refresh(yourDomain, specs)` from your tool's Apply.
4. Register your (domain, noun) with whatever reads requirements for the material engine. For sheet metal this is `SheetMetalRequirementReader` in `SheetMetalServices`.
