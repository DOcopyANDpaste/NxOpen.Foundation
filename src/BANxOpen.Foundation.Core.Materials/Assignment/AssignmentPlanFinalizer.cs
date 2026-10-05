using BANxOpen.Foundation.Contracts.Common;

namespace BANxOpen.Foundation.Core.Materials.Assignment;

/// <summary>Turns an <see cref="AssignmentPlan"/> plus the user's confirm/decline answers into the single atomic
/// <see cref="ExecutablePlan"/> for one Apply click. Partial-apply semantics: blocked and declined bodies are skipped
/// and reported, the rest are still assigned as part of the same plan (still one undo mark for the adapter to
/// wrap).</summary>
public sealed class AssignmentPlanFinalizer : IAssignmentPlanFinalizer
{
    private readonly IReadOnlyList<IPostAssignmentEffectRule> _effectRules;

    public AssignmentPlanFinalizer(IEnumerable<IPostAssignmentEffectRule> effectRules) =>
        _effectRules = effectRules.OrderBy(r => r.Order).ToList();

    public ExecutablePlan Finalize(
        AssignmentPlan plan,
        MaterialAssignmentPlanningInput input,
        HashSet<BodyId> confirmedBodyIds)
    {
        var targets = AssignmentTargets.Select(plan, input, confirmedBodyIds);

        var assignments = new List<ExecutableAssignment>();
        foreach (var target in targets.ToAssign)
        {
            input.CurrentAssignments.TryGetValue(target.Body.Id, out var currentAssignment);
            var context = new MaterialAssignmentRuleContext(
                input.RequestedMaterial, target.Body, currentAssignment, input.TargetBodies)
            {
                RequestedVariant = input.RequestedVariant,
            };

            var effects = _effectRules.SelectMany(rule => rule.GenerateEffects(context)).ToList();
            assignments.Add(new ExecutableAssignment(target.Body.Id, plan.RequestedMaterialId, effects));
        }

        return new ExecutablePlan(plan.PlanId, assignments, targets.SkippedBlocked, targets.SkippedDeclinedConfirmation);
    }
}
