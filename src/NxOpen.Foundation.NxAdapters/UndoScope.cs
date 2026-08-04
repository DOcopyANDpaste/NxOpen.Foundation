using NXOpen;

namespace NxOpen.Foundation.NxAdapters;

/// <summary>Standardized undo-mark wrapper, per Skills/common.md §4 — rollback is automatic on Dispose
/// unless <see cref="Commit"/> is reached, so any early return/exception between construction and Commit
/// rolls back cleanly.</summary>
public sealed class UndoScope : IDisposable
{
    private readonly Session _session;
    private readonly Session.UndoMarkId _mark;
    private bool _committed;

    public UndoScope(Session session, string name)
    {
        _session = session;
        _mark = _session.SetUndoMark(Session.MarkVisibility.Visible, name);
    }

    public void Commit() => _committed = true;

    public void Dispose()
    {
        if (!_committed)
            _session.UndoToMark(_mark, null);
    }
}
