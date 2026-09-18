using BANxOpen.Foundation.Contracts.Common;
using BANxOpen.Foundation.Core.Materials.Assignment.Choices;

namespace BANxOpen.Foundation.Core.Materials.Assignment;

/// <summary>Turns an <see cref="AssignmentPlan"/> plus the user's confirm/decline answers and their answers to
/// any <see cref="AssignmentChoice"/> into the single atomic <see cref="ExecutablePlan"/> for one Apply click.
/// Partial-apply semantics: blocked, declined and unanswered bodies are skipped and reported, the rest are
/// still assigned as part of the same plan (still one undo mark for the adapter to wrap).</summary>
public sealed class AssignmentPlanFinalizer : IAssignmentPlanFinalizer
{
    private readonly IReadOnlyList<IPostAssignmentEffectRule> _effectRules;
    private readonly IReadOnlyList<IAssignmentChoiceProvider> _choiceProviders;

    public AssignmentPlanFinalizer(
        IEnumerable<IPostAssignmentEffectRule> effectRules,
        IEnumerable<IAssignmentChoiceProvider>? choiceProviders = null)
    {
        _effectRules = effectRules.OrderBy(r => r.Order).ToList();
        _choiceProviders = choiceProviders?.ToList() ?? new List<IAssignmentChoiceProvider>();
    }

    public ExecutablePlan Finalize(
        AssignmentPlan plan,
        MaterialAssignmentPlanningInput input,
        HashSet<BodyId> confirmedBodyIds) =>
        Finalize(plan, input, confirmedBodyIds, AssignmentChoiceAnswers.Empty);

    public ExecutablePlan Finalize(
        AssignmentPlan plan,
        MaterialAssignmentPlanningInput input,
        HashSet<BodyId> confirmedBodyIds,
        AssignmentChoiceAnswers choiceAnswers)
    {
        var targets = AssignmentTargets.Select(plan, input, confirmedBodyIds);

        var assignments = new List<ExecutableAssignment>();
        var skippedUnresolved = new List<BodyId>();

        foreach (var target in targets.ToAssign)
        {
            input.CurrentAssignments.TryGetValue(target.Body.Id, out var currentAssignment);
            var context = new MaterialAssignmentRuleContext(
                input.RequestedMaterial, target.Body, currentAssignment, input.TargetBodies)
            {
                ChoiceAnswers = choiceAnswers,
            };

            // Skipped whole rather than assigned half-done: its effect rules need an answer they don't have.
            if (_choiceProviders.Any(p => p.ChoiceFor(context) is { } choice
                                          && !choiceAnswers.TryGet(choice.ChoiceId, target.Body.Id, out _)))
            {
                skippedUnresolved.Add(target.Body.Id);
                continue;
            }

            var effects = _effectRules.SelectMany(rule => rule.GenerateEffects(context)).ToList();
            assignments.Add(new ExecutableAssignment(target.Body.Id, plan.RequestedMaterialId, effects));
        }

        return new ExecutablePlan(
            plan.PlanId, assignments, targets.SkippedBlocked, targets.SkippedDeclinedConfirmation)
        {
            SkippedUnresolvedChoice = skippedUnresolved,
        };
    }
}
