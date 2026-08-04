# NXOpen C# — Without Block UI

Patterns for tools with no Styler dialog: batch (ugbatch) programs, menu/ribbon commands, and geometry/feature automation. Read `common.md` first.

## Contents
1. Entry point: internal library (menu/ribbon)
2. Entry point: batch (ugbatch .exe)
3. Feature/geometry automation
4. Parameter input without a dialog
5. Batch-safe logging

---

## 1. Entry point — internal library (menu/ribbon)

NX loads a DLL and calls a fixed-signature entry method. Keep it thin: resolve dependencies, delegate, report.

```csharp
// Automation/CreateBlockCommand.cs
using NXOpen;

public static class CreateBlockCommand {
    // Signature NX invokes from a MenuScript/ribbon action
    public static void Main(string[] args) {
        var session = Session.GetSession();
        var log = new NxListingLog(session);      // see section 5
        try {
            IPartService parts   = new PartService(session);
            IBlockPlanner planner = new BlockPlanner();

            var spec = ReadSpecFromArgsOrDefault(args);
            var plan = planner.Plan(parts.GetSolidBodies(), spec);
            if (!plan.Ok) { log.Error(plan.Message!); return; }

            using var undo = new UndoScope(session, "Create Block");
            parts.CreateBlock(spec);
            undo.Commit();
        } catch (NXException ex) {
            log.Error($"NX {ex.ErrorCode}: {ex.Message}");
        }
    }

    // NX asks the assembly whether it can be unloaded
    public static int GetUnloadOption(string dummy) =>
        (int)Session.LibraryUnloadOption.Immediately;
}
```

Rules:
- `Main` never contains business logic — it wires and delegates.
- Always implement `GetUnloadOption` so the DLL unloads predictably during development.

## 2. Entry point — batch (ugbatch .exe)

For headless runs. Same shape, but the process owns opening/closing the part explicitly.

```csharp
// Automation/BatchProgram.cs
using NXOpen;

public static class BatchProgram {
    public static int Main() {
        Session session = Session.GetSession();
        var log = new NxListingLog(session);
        BasePart? part = null;
        try {
            // batch owns the part lifetime — open and close deliberately
            PartLoadStatus status;
            part = session.Parts.OpenBaseDisplay("input.prt", out status);
            status.Dispose();

            IPartService parts   = new PartService(session);
            IBlockPlanner planner = new BlockPlanner();
            var spec = new BlockSpec(0,0,0, 100,50,25);

            var plan = planner.Plan(parts.GetSolidBodies(), spec);
            if (!plan.Ok) { log.Error(plan.Message!); return 1; }

            using (var undo = new UndoScope(session, "Batch Create Block")) {
                parts.CreateBlock(spec);
                undo.Commit();
            }

            ((Part)part).Save(BasePart.SaveComponents.True, BasePart.CloseAfterSave.False);
            return 0;
        } catch (NXException ex) {
            log.Error($"NX {ex.ErrorCode}: {ex.Message}");
            return 1;
        } finally {
            if (part != null)
                session.Parts.CloseAll(BasePart.CloseModified.CloseModified, null);
        }
    }
}
```

Rules:
- Return a nonzero exit code on failure — batch orchestration relies on it.
- Close parts in `finally`. A batch job that leaks open parts corrupts subsequent runs.
- Ownership is explicit: this process opened the part, so this process closes it.

## 3. Feature/geometry automation

All builder work lives in the adapter (see `common.md` §3). Core decides *what* to build; the adapter performs it. Keep the builder+`Destroy()` in `finally` pattern for every feature.

When creating multiple features, wrap the whole set in one `UndoScope` so a mid-sequence failure rolls back cleanly rather than leaving a half-built part.

```csharp
using (var undo = new UndoScope(session, "Build Bracket")) {
    parts.CreateBlock(baseSpec);
    parts.CreateHole(holeSpec);      // if this throws, the block is rolled back too
    undo.Commit();
}
```

## 4. Parameter input without a dialog

No Styler means parameters come from args, an input file, or config — not hardcoded. Parse them into a DTO at the boundary and hand the DTO to Core.

```csharp
private static BlockSpec ReadSpecFromArgsOrDefault(string[] args) {
    // parse args/config into a plain DTO; validation happens in Core
    if (args.Length >= 6 &&
        double.TryParse(args[0], out var x) && double.TryParse(args[1], out var y) &&
        double.TryParse(args[2], out var z) && double.TryParse(args[3], out var l) &&
        double.TryParse(args[4], out var w) && double.TryParse(args[5], out var h))
        return new BlockSpec(x, y, z, l, w, h);
    return new BlockSpec(0,0,0, 100,50,25);
}
```

Do not validate here — Core validates. This method only converts raw input to a DTO.

## 5. Batch-safe logging

`Console.WriteLine` is invisible in NX. Write to the Listing Window (interactive) or a file (batch). Reuse `NxOpen.Foundation.NxAdapters.NxListingLog` from the shared foundation rather than writing a new copy per project.

```csharp
// NxOpen.Foundation.NxAdapters/NxListingLog.cs
using NXOpen;

public sealed class NxListingLog {
    private readonly ListingWindow _lw;
    public NxListingLog(Session session) => _lw = session.ListingWindow;

    private void Write(string level, string msg) {
        if (!_lw.IsOpen) _lw.Open();
        _lw.WriteLine($"[{level}] {msg}");
    }
    public void Info(string m)  => Write("INFO", m);
    public void Warn(string m)  => Write("WARN", m);
    public void Error(string m) => Write("ERROR", m);
}
```

For batch, additionally route to a log file so failures are diagnosable after the process exits.
