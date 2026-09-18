using BANxOpen.Foundation.Contracts.Common;

namespace BANxOpen.Foundation.Core.Materials.Assignment;

/// <summary>The single atomic unit of work for one Apply click. The adapter layer wraps exactly one
/// call to <c>IPartMaterialService.ApplyPlan</c> per <see cref="ExecutablePlan"/> in one NX undo mark,
/// regardless of how many bodies were skipped as blocked/declined (partial-apply semantics).</summary>
public sealed record ExecutablePlan(
    string PlanId,
    IReadOnlyList<ExecutableAssignment> Assignments,
    IReadOnlyList<BodyId> SkippedBlocked,
    IReadOnlyList<BodyId> SkippedDeclinedConfirmation)
{
    /// <summary>Bodies a <see cref="Choices.IAssignmentChoiceProvider"/> raised a question about that never got
    /// an answer — a caller wiring mistake, not a user decision.</summary>
    public IReadOnlyList<BodyId> SkippedUnresolvedChoice { get; init; } = Array.Empty<BodyId>();
}
