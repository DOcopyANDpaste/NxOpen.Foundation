using BANxOpen.Foundation.Core.RuleEngine;

namespace BANxOpen.Foundation.Core.Materials.Assignment;

/// <summary>An effect rule: given an assignment that IS going to happen, produces side-effect
/// instructions (e.g. "sync this physical property") for the adapter layer to execute. Core never
/// performs the effect itself — it only describes what should happen, as plain data. Thin
/// specialization of the shared BANxOpen.Foundation.Core.RuleEngine.IEffectRule shape.</summary>
public interface IPostAssignmentEffectRule : IEffectRule<MaterialAssignmentRuleContext, SideEffectInstruction>
{
    /// <summary>Every <see cref="SideEffectInstruction.InstructionType"/> this rule can emit.
    /// <see cref="Rules.MaterialRuleSet.EnsureExecutorsFor"/> checks each has an executor, so a side effect cannot be
    /// registered without the code that carries it out.</summary>
    IReadOnlyCollection<string> InstructionTypes { get; }
}
