using BANxOpen.Foundation.Core.Materials.Assignment.Choices;
using BANxOpen.Foundation.Core.Materials.Bodies;
using BANxOpen.Foundation.Contracts.Materials;
using BANxOpen.Foundation.Contracts.Bodies;

namespace BANxOpen.Foundation.Core.Materials.Assignment;

/// <summary>Everything a rule needs to evaluate one (body, requested material) pair.
/// <see cref="AllTargetBodiesInBatch"/> gives rules visibility into the whole Apply batch for
/// cross-body reasoning, even though every decision is still emitted per-body.</summary>
public sealed record MaterialAssignmentRuleContext(
    Material RequestedMaterial,
    BodyInfo TargetBody,
    BodyMaterialAssignment? CurrentAssignment,
    IReadOnlyList<BodyInfo> AllTargetBodiesInBatch)
{
    /// <summary>What the user picked for the questions the <see cref="IAssignmentChoiceProvider"/>s raised. Set
    /// only by <see cref="AssignmentPlanFinalizer"/>; always empty while planning.</summary>
    public AssignmentChoiceAnswers ChoiceAnswers { get; init; } = AssignmentChoiceAnswers.Empty;
}
