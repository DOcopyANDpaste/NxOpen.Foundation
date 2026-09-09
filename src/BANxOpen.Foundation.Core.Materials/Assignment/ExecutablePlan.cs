using BANxOpen.Foundation.Contracts.Common;

namespace BANxOpen.Foundation.Core.Materials.Assignment;

/// <summary>The single atomic unit of work for one Apply click. The adapter layer wraps exactly one
/// call to <c>IPartMaterialService.ApplyPlan</c> per <see cref="ExecutablePlan"/> in one NX undo mark,
/// regardless of how many bodies were skipped as blocked/declined (partial-apply semantics).</summary>
public sealed record ExecutablePlan(
    string PlanId,
    IReadOnlyList<ExecutableAssignment> Assignments,
    IReadOnlyList<BodyId> SkippedBlocked,
    IReadOnlyList<BodyId> SkippedDeclinedConfirmation);
