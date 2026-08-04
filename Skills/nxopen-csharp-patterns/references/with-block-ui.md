# NXOpen C# — With Block UI Styler

Patterns for tools with a Styler dialog (`.dlx`). Read `common.md` first.

## The constraint that shapes everything

Block UI Styler generates `MyDialog.cs` + `MyDialog.dlx` and is **meant to be regenerated** whenever the dialog layout changes. Any logic you put in the generated `.cs` is lost or merge-conflicted on the next regeneration. Therefore the generated file must stay thin and disposable; all logic lives in a hand-written presenter the Styler never touches.

## File layout

```
Ui.Dialogs/
    MyDialog.cs          — GENERATED, regenerable, thin (delegations only)
    MyDialog.dlx         — GENERATED layout (commit as source, never hand-edit)
    MyDialogPresenter.cs — HAND-WRITTEN, all logic
    BlockAccessor.cs     — HAND-WRITTEN, typed reads of dialog blocks
```

## Contents
1. Thin generated view
2. Hand-written presenter
3. BlockAccessor (typed, isolated block access)
4. Regeneration discipline
5. Modal vs interactive

---

## 1. Thin generated view

In the generated `MyDialog.cs`, each callback does ONE thing: delegate to the presenter. Mark the hand-edited lines with a banner so you can re-add them after regeneration.

```csharp
// GENERATED — MyDialog.cs
// >>> HAND-EDITED DELEGATIONS — re-add these after any Styler regeneration <<<
public int apply_cb() {
    return _presenter.OnApply();
}
public void update_cb(NXOpen.BlockStyler.UIBlock block) {
    _presenter.OnUpdate(block.Name);
}
public int ok_cb() {
    return _presenter.OnOk();
}
// >>> END HAND-EDITED <<<
```

Nothing else in this file is touched. No `if`, no geometry, no validation.

## 2. Hand-written presenter

The presenter owns all logic. It reads dialog values via `BlockAccessor`, hands plain DTOs to Core, and applies results via the adapter. It is the only new NX-touching layer, and it's testable by mocking its dependencies.

```csharp
// Ui.Dialogs/MyDialogPresenter.cs
using NXOpen;

public sealed class MyDialogPresenter {
    private readonly IPartService _parts;     // from Contracts (adapter)
    private readonly IBlockPlanner _planner;  // from Core
    private readonly BlockAccessor _blocks;
    private readonly Session _session;

    public MyDialogPresenter(Session session, BlockAccessor blocks,
                             IPartService parts, IBlockPlanner planner) {
        _session = session; _blocks = blocks; _parts = parts; _planner = planner;
    }

    public int OnApply() {
        BlockSpec spec = _blocks.ReadBlockSpec();          // UI -> DTO
        var plan = _planner.Plan(_parts.GetSolidBodies(), spec);  // Core decides
        if (!plan.Ok) { _blocks.ShowError(plan.Message!); return 1; }

        using var undo = new UndoScope(_session, "Dialog Create Block");
        _parts.CreateBlock(spec);                          // adapter acts
        undo.Commit();
        return 0;
    }

    public int OnOk() => OnApply();
    public void OnUpdate(string changedBlock) { /* live-update logic if interactive */ }
}
```

Core still never sees an NXOpen type. The presenter is the seam.

## 3. BlockAccessor — typed, isolated block access

All `FindBlock("stringID")` lookups and typed property reads live in ONE class, with string IDs as constants. When the Styler regenerates and renames/reorders block fields, only this file changes — the diff is contained and reviewable.

```csharp
// Ui.Dialogs/BlockAccessor.cs
using NXOpen.BlockStyler;

public sealed class BlockAccessor {
    // string IDs centralized — match the .dlx block IDs exactly
    private const string LengthId = "lengthDouble";
    private const string WidthId  = "widthDouble";
    private const string HeightId = "heightDouble";

    private readonly PropertyList _length, _width, _height;

    public BlockAccessor(BlockDialog dialog) {
        _length = dialog.GetBlock(LengthId).GetProperties();
        _width  = dialog.GetBlock(WidthId).GetProperties();
        _height = dialog.GetBlock(HeightId).GetProperties();
    }

    public BlockSpec ReadBlockSpec() => new BlockSpec(
        0, 0, 0,
        _length.GetDouble("Value"),
        _width.GetDouble("Value"),
        _height.GetDouble("Value"));

    public void ShowError(string msg) =>
        NXOpen.UI.GetUI().NXMessageBox.Show("Error",
            NXOpen.NXMessageBox.DialogType.Error, msg);
}
```

`BlockAccessor` itself is almost entirely project-specific (it's typed to one dialog's own blocks and DTOs) and is NOT something to pull from the shared foundation. The exception is the handful of fully generic NXMessageBox wrappers with no dialog-block dependency (a plain `Confirm`/`ShowResult`/`ShowError` trio) — those live once in `NxOpen.Foundation.NxAdapters.NxMessageBoxHelper` and `BlockAccessor` can forward to them instead of reimplementing the same three `NXMessageBox.Show` calls per project.

Note: the actual block reads (`GetDouble` etc.) require a live NX session, so `BlockAccessor` itself is integration-tested, not unit-tested. The presenter's decision logic is what you cover by mocking.

## 4. Regeneration discipline

1. **No logic in the generated `.cs`** — only the banner-marked delegations.
2. **All block access in `BlockAccessor`**, string IDs as constants.
3. **`.dlx` is the layout source of truth** — commit it, never hand-edit, round-trip through the Styler only.
4. **One dialog = one generated file set = one presenter.** Reuse lives in Core / a shared UI base, never in generated code.

## 5. Modal vs interactive

Pick the dialog mode before writing the presenter — the design differs:

- **Modal one-shot:** logic runs only in `OnApply`/`OnOk`. `OnUpdate` stays empty. Simpler; prefer this unless live feedback is required.
- **Interactive/live-update:** `update_cb` fires on every block change, so `OnUpdate` must hold and reconcile UI state carefully (guard against re-entrancy and partial input). Only take this on when the UX genuinely needs it.

State which mode a generated dialog uses at the top of its presenter, so maintainers know the contract.
