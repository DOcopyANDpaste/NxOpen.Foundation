using BANxOpen.Foundation.Contracts.Common;
using BANxOpen.Foundation.Core.Materials.Assignment;
using NXOpen;

namespace BANxOpen.Foundation.NxAdapters.Materials;

/// <summary>Carries out one kind of <see cref="SideEffectInstruction"/> against a resolved body, from inside
/// <see cref="PartMaterialService.ApplyPlan"/>'s per-body undo mark.
///
/// This is how a feature domain gets its own post-assignment effect executed without the engine learning what
/// the effect is: the domain's Core effect rule emits the instruction as plain data, and the domain's adapter
/// layer registers the matching executor beside the rule in its <see cref="INxMaterialRuleModule"/>;
/// <see cref="MaterialEngine.Create"/> refuses to start when one is missing. The two agree on the instruction type
/// and data keys through the rule's public constants, the same way <see cref="DisplayMaterialHelper"/> agrees with
/// <c>SyncCoatingDisplayMaterialEffectRule</c>.</summary>
public interface ISideEffectExecutor
{
    /// <summary>The <see cref="SideEffectInstruction.InstructionType"/> this executor handles.</summary>
    string InstructionType { get; }

    OperationResult Execute(SideEffectInstruction instruction, Body body);
}

