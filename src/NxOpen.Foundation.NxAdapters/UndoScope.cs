using NXOpen;

namespace NxOpen.Foundation.NxAdapters;

/// <summary>Standardized undo-mark wrapper, per Skills/common.md §4 — rollback is automatic on Dispose
/// unless <see cref="Commit"/> is reached, so any early return/exception between construction and Commit
/// rolls back cleanly.
///
/// <see cref="Session.UndoToMark"/>'s second parameter is <c>markName</c>, not optional — verified against
/// the installed NX's <c>NXOpen.dll</c> rather than assumed. It must be the same name passed to
/// <see cref="Session.SetUndoMark"/>, so it's kept as a field rather than discarded after construction.</summary>
public sealed class UndoScope : IDisposable
{
    private readonly Session _session;
    private readonly Session.UndoMarkId _mark;
    private readonly string _name;
    private readonly Action<string>? _onRollbackFailed;
    private bool _committed;

    public UndoScope(Session session, string name, Action<string>? onRollbackFailed = null)
    {
        _session = session;
        _name = name;
        _onRollbackFailed = onRollbackFailed;
        _mark = _session.SetUndoMark(Session.MarkVisibility.Visible, name);
    }

    public void Commit() => _committed = true;

    public void Dispose()
    {
        if (_committed)
            return;

        try
        {
            _session.UndoToMark(_mark, _name);
        }
        catch (NXException ex)
        {
            _onRollbackFailed?.Invoke($"Undo to mark '{_name}' failed: NX {ex.ErrorCode}: {ex.Message}");
        }
    }
}
