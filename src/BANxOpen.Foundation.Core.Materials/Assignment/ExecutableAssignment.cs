using BANxOpen.Foundation.Contracts.Common;

namespace BANxOpen.Foundation.Core.Materials.Assignment;

public sealed record ExecutableAssignment(
    BodyId BodyId,
    MaterialId MaterialId,
    IReadOnlyList<SideEffectInstruction> SideEffects);
