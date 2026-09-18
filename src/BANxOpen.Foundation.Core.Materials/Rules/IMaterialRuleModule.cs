using BANxOpen.Foundation.Core.Materials.Assignment;
using BANxOpen.Foundation.Core.Materials.Assignment.Choices;
using BANxOpen.Foundation.Core.Materials.Rules.Features;

namespace BANxOpen.Foundation.Core.Materials.Rules;

/// <summary>One category of material business rules — standard, display material, sheet metal, or a feature
/// domain such as beads — holding everything that category enforces when a material is assigned, in one place.
///
/// Where a new rule goes:
/// <list type="bullet">
/// <item>A check that allows, warns, asks, or refuses → an <see cref="IMaterialValidationRule"/> in
/// <see cref="ValidationRules"/>, ordered with <see cref="MaterialRuleOrder.Validation"/>.</item>
/// <item>A restriction that features already built on the body place on its material → an
/// <see cref="IFeatureMaterialConstraintProvider"/> in <see cref="FeatureConstraints"/>. Prefer this for feature
/// rules: the same predicate that refuses an assignment also filters the materials offered to the user.</item>
/// <item>Something to do once the assignment is going ahead → an <see cref="IPostAssignmentEffectRule"/> in
/// <see cref="SideEffectRules"/>, ordered with <see cref="MaterialRuleOrder.SideEffect"/>, plus an executor for its
/// instruction type on the NX side of the module. <see cref="MaterialRuleSet.EnsureExecutorsFor"/> refuses a side
/// effect that has no executor.</item>
/// <item>Something only the user can decide, that one of those side effects then acts on → an
/// <see cref="IAssignmentChoiceProvider"/> in <see cref="ChoiceProviders"/>. Pair it with the effect rule that
/// reads its answer, sharing the choice id between them.</item>
/// </list>
/// A new category is a new module registered with <see cref="MaterialRuleSet"/>; the planner, finalizer and
/// every other module stay untouched.
///
/// Lists a module does not use are empty, never null.</summary>
public interface IMaterialRuleModule
{
    /// <summary>Stable identifier, e.g. "DISPLAY" or "SHEETMETAL.BEAD". Must be unique within a rule set.</summary>
    string ModuleId { get; }

    IReadOnlyList<IMaterialValidationRule> ValidationRules { get; }

    IReadOnlyList<IFeatureMaterialConstraintProvider> FeatureConstraints { get; }

    IReadOnlyList<IPostAssignmentEffectRule> SideEffectRules { get; }

    /// <summary>Questions this module needs put to the user before its side effects can run.</summary>
    IReadOnlyList<IAssignmentChoiceProvider> ChoiceProviders { get; }
}
