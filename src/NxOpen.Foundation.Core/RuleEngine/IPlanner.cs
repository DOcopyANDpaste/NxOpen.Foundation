namespace NxOpen.Foundation.Core.RuleEngine;

/// <summary>Runs a set of <see cref="IGateRule{TContext,TOutcome}"/>s over an input and produces a plan.
/// Generalized from this repo's original IMaterialAssignmentPlanner.</summary>
public interface IPlanner<in TInput, out TPlan>
{
    TPlan Plan(TInput input);
}
