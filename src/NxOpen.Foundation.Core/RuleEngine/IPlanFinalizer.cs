namespace NxOpen.Foundation.Core.RuleEngine;

/// <summary>Turns a plan plus the set of user-confirmed items into an executable plan, applying
/// <see cref="IEffectRule{TContext,TEffect}"/>-generated side effects. Generalized from this repo's
/// original IAssignmentPlanFinalizer.</summary>
public interface IPlanFinalizer<in TPlan, in TInput, TConfirmKey, out TExecutablePlan>
{
    TExecutablePlan Finalize(TPlan plan, TInput input, HashSet<TConfirmKey> confirmedIds);
}
