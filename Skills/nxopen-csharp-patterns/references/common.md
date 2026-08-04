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

Standardize rollback with a disposable. Reuse `NxOpen.Foundation.NxAdapters.UndoScope` from the shared foundation rather than writing a new copy per project.

```csharp
// NxOpen.Foundation.NxAdapters/UndoScope.cs
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
