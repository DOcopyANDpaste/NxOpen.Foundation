using BANxOpen.Foundation.Contracts.Bodies;
using BANxOpen.Foundation.Contracts.Common;

namespace BANxOpen.Foundation.Core.Materials.Assignment;

/// <summary>One body that is going ahead, paired with the evaluation that cleared it.</summary>
public sealed record AssignmentTarget(BodyAssignmentEvaluation Evaluation, BodyInfo Body);

/// <summary>Which bodies of a plan are going ahead, and why the rest are not.</summary>
public sealed record AssignmentTargetSet(
    IReadOnlyList<AssignmentTarget> ToAssign,
    IReadOnlyList<BodyId> SkippedBlocked,
    IReadOnlyList<BodyId> SkippedDeclinedConfirmation);

/// <summary>Works out which of a plan's bodies will actually be assigned, given the user's confirm/decline
/// answers. Shared by <see cref="AssignmentPlanFinalizer"/> and <see cref="Choices.AssignmentChoiceCollector"/>.</summary>
public static class AssignmentTargets
{
    public static AssignmentTargetSet Select(
        AssignmentPlan plan,
        MaterialAssignmentPlanningInput input,
        HashSet<BodyId> confirmedBodyIds)
    {
        var bodiesById = input.TargetBodies.ToDictionary(b => b.Id);
        var toAssign = new List<AssignmentTarget>();
        var skippedBlocked = new List<BodyId>();
        var skippedDeclined = new List<BodyId>();

        foreach (var evaluation in plan.BodyEvaluations)
        {
            if (evaluation.IsBlocked)
            {
                skippedBlocked.Add(evaluation.BodyId);
                continue;
            }

            if (evaluation.RequiresConfirmation && !confirmedBodyIds.Contains(evaluation.BodyId))
            {
                skippedDeclined.Add(evaluation.BodyId);
                continue;
            }

            // A plan evaluated against a different input than the one being finalized (e.g. the part was
            // rescanned in between and a body disappeared) would otherwise throw here mid-loop, losing
            // the assignments already accumulated. Skipping keeps partial-apply semantics intact.
            if (!bodiesById.TryGetValue(evaluation.BodyId, out var targetBody))
            {
                skippedBlocked.Add(evaluation.BodyId);
                continue;
            }

            toAssign.Add(new AssignmentTarget(evaluation, targetBody));
        }

        return new AssignmentTargetSet(toAssign, skippedBlocked, skippedDeclined);
    }
}
