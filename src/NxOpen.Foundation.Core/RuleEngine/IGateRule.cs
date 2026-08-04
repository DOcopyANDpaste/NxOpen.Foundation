namespace NxOpen.Foundation.Core.RuleEngine;

/// <summary>A gate rule: decides whether an action is allowed, blocked, or needs user confirmation.
/// Implement this to add a new business rule without touching the planner or any other rule — that's
/// the whole point of the pipeline. Generalized from this repo's original IMaterialAssignmentRule.</summary>
public interface IGateRule<in TContext, out TOutcome>
{
    string RuleId { get; }

    /// <summary>Evaluation order, ascending. Leave gaps (100, 200, ...) between built-in rules so new
    /// rules can be inserted without renumbering existing ones.</summary>
    int Order { get; }

    TOutcome Evaluate(TContext context);
}
