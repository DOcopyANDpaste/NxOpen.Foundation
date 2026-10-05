using BANxOpen.Foundation.Core.Materials.Assignment;
using BANxOpen.Foundation.Core.Materials.Rules.Features;

namespace BANxOpen.Foundation.Core.Materials.Rules.Standard;

/// <summary>Rules that apply to every material assignment regardless of material, body or feature.</summary>
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
}
