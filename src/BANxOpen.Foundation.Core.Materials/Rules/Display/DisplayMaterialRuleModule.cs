using BANxOpen.Foundation.Core.Materials.Assignment;
using BANxOpen.Foundation.Core.Materials.Assignment.Choices;
using BANxOpen.Foundation.Core.Materials.Rules.Features;

namespace BANxOpen.Foundation.Core.Materials.Rules.Display;

/// <summary>Display (coating/studio) material: validates the library's coating data against the body, then syncs
/// the display material and body color. The executor for <see cref="SyncCoatingDisplayMaterialEffectRule"/> is
/// <c>DisplayMaterialHelper</c> in BANxOpen.Foundation.NxAdapters, registered by <c>MaterialEngine</c>.</summary>
public sealed class DisplayMaterialRuleModule : IMaterialRuleModule
{
    public const string Id = "DISPLAY";

    public string ModuleId => Id;

    public IReadOnlyList<IMaterialValidationRule> ValidationRules { get; } = new IMaterialValidationRule[]
    {
        new ValidateCoatingDisplayMaterialRule(),
    };

    public IReadOnlyList<IFeatureMaterialConstraintProvider> FeatureConstraints => Array.Empty<IFeatureMaterialConstraintProvider>();

    public IReadOnlyList<IPostAssignmentEffectRule> SideEffectRules { get; } = new IPostAssignmentEffectRule[]
    {
        new SyncCoatingDisplayMaterialEffectRule(),
    };

    public IReadOnlyList<IAssignmentChoiceProvider> ChoiceProviders => Array.Empty<IAssignmentChoiceProvider>();
}
