# Common NXOpen C# Patterns

Baseline patterns for all NXOpen C# code. Read this in every case.

## Contents
1. Session access
2. The contracts seam (interfaces + DTOs)
3. Adapter layer
4. Undo mark wrapper
5. Error handling
6. Core logic shape
7. Wiring it together
8. User-facing text

---

## 1. Session access

Get the session once, at the adapter boundary — never scattered through logic.

```csharp
using NXOpen;

internal sealed class NxSessionProvider {
    public Session Session { get; } = Session.GetSession();
    public UI Ui { get; } = UI.GetUI();
}
```

Never call `Session.GetSession()` inside Core or inside a loop. It's a boundary concern.

## 2. The contracts seam

`Shared.Contracts` defines interfaces that hide NXOpen, plus DTOs that carry plain data. This is the entire point of the architecture.

```csharp
// Shared.Contracts/IPartService.cs
public interface IPartService {
    string WorkPartName { get; }
    IReadOnlyList<BodyInfo> GetSolidBodies();   // returns DTOs, not NXOpen.Body
    void CreateBlock(BlockSpec spec);
}

// Shared.Contracts/Dtos.cs — plain, no NXOpen references
public record BodyInfo(string Name, double Volume);
public record BlockSpec(double X, double Y, double Z, double Length, double Width, double Height);
public record OperationResult(bool Ok, string? ErrorCode, string? Message) {
    public static OperationResult Success() => new(true, null, null);
    public static OperationResult Fail(string code, string msg) => new(false, code, msg);
}
```

## 3. Adapter layer

`NxAdapters` implements the contracts. This is the ONLY place NXOpen types appear.

```csharp
// NxAdapters/PartService.cs
using NXOpen;
using NXOpen.Features;

internal sealed class PartService : IPartService {
    private readonly Session _session;
    public PartService(Session session) => _session = session;

    public string WorkPartName => _session.Parts.Work.Name;

    public IReadOnlyList<BodyInfo> GetSolidBodies() {
        var work = _session.Parts.Work;
        var list = new List<BodyInfo>();
        foreach (Body b in work.Bodies) {
            // map NXOpen.Body -> plain DTO; NX types stop here
            list.Add(new BodyInfo(b.Name, 0.0 /* measure via MeasureManager */));
        }
        return list;
    }

    public void CreateBlock(BlockSpec s) {
        var work = _session.Parts.Work;
        BlockFeatureBuilder builder = work.Features.CreateBlockFeatureBuilder(null);
        try {
            builder.SetOriginAndLengths(
                new Point3d(s.X, s.Y, s.Z),
                s.Length.ToString(), s.Width.ToString(), s.Height.ToString());
            builder.Commit();
        } finally {
            builder.Destroy();   // ALWAYS destroy builders
        }
    }
}
```

Rule: every NXOpen builder is destroyed in a `finally`. Leaked builders corrupt session state.

## 4. Undo mark wrapper

Standardize rollback with a disposable. Reuse `BANxOpen.Foundation.NxAdapters.UndoScope` from the shared foundation rather than writing a new copy per project.

```csharp
// BANxOpen.Foundation.NxAdapters/UndoScope.cs
using NXOpen;

public sealed class UndoScope : IDisposable {
    private readonly Session _session;
    private readonly Session.UndoMarkId _mark;
    private bool _committed;

    public UndoScope(Session session, string name) {
        _session = session;
        _mark = _session.SetUndoMark(Session.MarkVisibility.Visible, name);
    }

    public void Commit() => _committed = true;

    public void Dispose() {
        if (!_committed)
            _session.UndoToMark(_mark, null);   // roll back on any failure/early exit
    }
}
```

Usage — rollback is automatic unless `Commit()` is reached:

```csharp
using (var undo = new UndoScope(_session, "Create Block")) {
    _partService.CreateBlock(spec);
    undo.Commit();
}
```

## 5. Error handling

```csharp
try {
    // NXOpen operation
} catch (NXException ex) {
    // log the CODE, not just the message — support needs it
    _log.Error($"NX error {ex.ErrorCode}: {ex.Message}");
    return OperationResult.Fail(ex.ErrorCode.ToString(), ex.Message);
}
```

Never `catch { }` silently. Never catch `Exception` when you mean `NXException`.

## 6. Core logic shape

Core depends only on `Shared.Contracts`. No `using NXOpen;` — ever.

```csharp
// Core/BlockPlanner.cs
public interface IBlockPlanner {
    OperationResult Plan(IReadOnlyList<BodyInfo> existing, BlockSpec requested);
}

public sealed class BlockPlanner : IBlockPlanner {
    public OperationResult Plan(IReadOnlyList<BodyInfo> existing, BlockSpec requested) {
        if (requested.Length <= 0 || requested.Width <= 0 || requested.Height <= 0)
            return OperationResult.Fail("VALIDATION", "Dimensions must be positive.");
        // pure decision logic, fully unit-testable with no NX session
        return OperationResult.Success();
    }
}
```

This class is unit-testable with plain in-memory `BodyInfo`/`BlockSpec` — no NX, no license.

## 7. Wiring it together

The entry point (menu handler or ugbatch Main) is the only place that knows both worlds.

```csharp
var session = Session.GetSession();
IPartService parts = new PartService(session);   // adapter
IBlockPlanner planner = new BlockPlanner();       // Core

var spec = new BlockSpec(0,0,0, 100,50,25);
var plan = planner.Plan(parts.GetSolidBodies(), spec);   // Core decides
if (!plan.Ok) { /* report */ return; }

using (var undo = new UndoScope(session, "Create Block")) {
    parts.CreateBlock(spec);                       // adapter acts
    undo.Commit();
}
```

## 8. User-facing text

Text the user reads lives with the knowledge that produces it, in one of three layers. Pick the layer by asking
"who would say this the same way in every tool?"

| Layer | Owns | File |
|---|---|---|
| **Domain** (library) | Anything a rule, validator, check or service decides and explains — shared by every UI | One describer per library repo, in its Core project: `FoundationText`, `MaterialsText`, `SheetMetalText`; codes in `FoundationCodes`, `MaterialsCodes`, `SheetMetalCodes` |
| **Shared NX dialog** | Wording every NX dialog shows the same way, with no domain meaning: message box titles, "could not start", "unexpected error handling …", "NX refused the change", NX error formatting | `BANxOpen.Foundation.NxAdapters/NxDialogText.cs`, used by `NxMessageBoxHelper` |
| **App** | Only what exists because of this dialog's design: menus, column titles, statuses, flow prompts, dialog-specific errors | The app's `UiText.cs` |

**The contract is the code, not the sentence.** A rule or service returns a stable code (`RuleOutcome.ReasonCode`,
`OperationResult.ErrorCode`) and the values behind the message (`RuleOutcome.Args`), and builds `Message` from its
library's describer. `Message` is canonical: every UI shows it as-is and never rewords it.

**Describer pattern** (as ASP.NET Identity's `IdentityErrorDescriber`): an ordinary class with one `public virtual`
member per message and a `Default` instance. Consumers take it as an optional constructor parameter; factories pass it
down. No global setter — tests and tools can never step on each other. An app *can* override a wording later by
subclassing and passing its describer to the factory; by default nobody does.

```csharp
// BANxOpen.Materials.Core/MaterialsText.cs
public class MaterialsText {
    public static MaterialsText Default { get; } = new();
    public virtual string Reassign(string current, string requested) =>
        $"Body already has '{current}'. Replace with '{requested}'?";
}

// BANxOpen.Materials.Core/MaterialsCodes.cs
public static class MaterialsCodes { public const string Reassign = "REASSIGN"; }

// A rule
public sealed class RequireConfirmationOnReassignmentRule : IMaterialValidationRule {
    private readonly MaterialsText _text;
    public RequireConfirmationOnReassignmentRule(MaterialsText? text = null) => _text = text ?? MaterialsText.Default;

    public RuleOutcome Evaluate(MaterialAssignmentRuleContext c) =>
        new(RuleId, RuleDecision.RequireConfirmation, MaterialsCodes.Reassign, _text.Reassign(current, requested))
        { Args = new Dictionary<string, string> { ["current"] = current, ["requested"] = requested } };
}

// A factory threads it through: MaterialEngine.Create(..., MaterialsText? text = null)
```

**What goes into a describer:** only text that reaches the user — a message box, or a status line/label shown in a
dialog. Log lines (Listing Window) stay inline where they are written. An exception whose message reaches the user is
worded by its library's describer at the throw site; raw NX or system exception text is shown only as a "Details"
line under a worded sentence (`NxDialogText.NxError(ex)` formats it).

**Never put stored data in a describer.** Text written into the part (attribute values, requirement descriptions) is
data: changing its wording would make every existing part look changed. Keep it where it is produced, marked as stored.

**Why static C# and not `.resx`:** the toolset is English-only, and typed methods keep placeholders compile-checked.
Moving to resources later only changes the bodies of the describer methods, not their callers.
