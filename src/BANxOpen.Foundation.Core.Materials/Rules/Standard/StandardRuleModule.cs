using BANxOpen.Foundation.Core.Materials.Assignment;
using BANxOpen.Foundation.Core.Materials.Assignment.Choices;
using BANxOpen.Foundation.Core.Materials.Rules.Features;

namespace BANxOpen.Foundation.Core.Materials.Rules.Standard;

/// <summary>Rules that apply to every material assignment regardless of material, body or feature.
///
/// <see cref="SyncPhysicalPropertiesEffectRule"/> is intentionally NOT registered: nothing executes
/// SYNC_PHYSICAL_PROPERTY instructions, and <see cref="MaterialRuleSet.EnsureExecutorsFor"/> would refuse it. The rule
/// and its tests are kept — add it to <see cref="SideEffectRules"/> together with an executor when physical property
/// sync is wanted.</summary>
public sealed class StandardRuleModule : IMaterialRuleModule
{
    public const string Id = "STANDARD";

    public string ModuleId => Id;

    public IReadOnlyList<IMaterialValidationRule> ValidationRules { get; } = new IMaterialValidationRule[]
    {
        new RequireConfirmationOnReassignmentRule(),
    };

    public IReadOnlyList<IFeatureMaterialConstraintProvider> FeatureConstraints => Array.Empty<IFeatureMaterialConstraintProvider>();

    public IReadOnlyList<IPostAssignmentEffectRule> SideEffectRules => Array.Empty<IPostAssignmentEffectRule>();

    public IReadOnlyList<IAssignmentChoiceProvider> ChoiceProviders => Array.Empty<IAssignmentChoiceProvider>();
}
