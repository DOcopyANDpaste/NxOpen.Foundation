namespace NxOpen.Foundation.Core.RuleEngine;

/// <summary>A rule that generates side-effect instructions to run after the main action is applied
/// (e.g. syncing a derived property). Generalized from this repo's original IPostAssignmentEffectRule.</summary>
public interface IEffectRule<in TContext, out TEffect>
{
    string RuleId { get; }

    /// <summary>Evaluation order, ascending — see <see cref="IGateRule{TContext,TOutcome}.Order"/>.</summary>
    int Order { get; }

    IReadOnlyList<TEffect> GenerateEffects(TContext context);
}
