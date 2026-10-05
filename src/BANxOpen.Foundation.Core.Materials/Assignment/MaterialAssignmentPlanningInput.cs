using BANxOpen.Foundation.Contracts.Common;
using BANxOpen.Foundation.Contracts.Materials;
using BANxOpen.Foundation.Contracts.Bodies;

namespace BANxOpen.Foundation.Core.Materials.Assignment;

public sealed record MaterialAssignmentPlanningInput(
    Material RequestedMaterial,
    IReadOnlyList<BodyInfo> TargetBodies,
    IReadOnlyDictionary<BodyId, BodyMaterialAssignment> CurrentAssignments)
{
    /// <summary>Which variant of <see cref="RequestedMaterial"/> the user picked, as the domain that offers variants
    /// names it — for sheet metal, the standards file row the Sheet Metal Preferences are set to. Null when the
    /// material was picked on its own. Passed through to every rule unread.</summary>
    public string? RequestedVariant { get; init; }
}
