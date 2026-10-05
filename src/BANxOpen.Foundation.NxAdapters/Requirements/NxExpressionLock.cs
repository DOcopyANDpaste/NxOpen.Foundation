using NXOpen;

namespace BANxOpen.Foundation.NxAdapters.Requirements;

/// <summary>Expressions a tool owns are locked against hand edits (<see cref="Expression.IsNoEdit"/>, the Expressions
/// dialog's lock, as journaled), and unlocked only for the tool's own edit:
/// <code>using (NxExpressionLock.Unlocked(expression)) { expressions.EditExpressionWithUnits(...); }</code>
/// The previous state is restored on dispose — an expression someone deliberately left unlocked stays unlocked.</summary>
public sealed class NxExpressionLock : IDisposable
{
    private readonly Expression _expression;
    private readonly bool _wasLocked;

    private NxExpressionLock(Expression expression)
    {
        _expression = expression;
        _wasLocked = expression.IsNoEdit;
        if (_wasLocked)
            expression.IsNoEdit = false;
    }

    public static NxExpressionLock Unlocked(Expression expression) => new(expression);

    /// <summary>Locks <paramref name="expression"/> against hand edits.</summary>
    public static void Lock(Expression expression) => expression.IsNoEdit = true;

    public void Dispose()
    {
        if (_wasLocked)
            _expression.IsNoEdit = true;
    }
}
