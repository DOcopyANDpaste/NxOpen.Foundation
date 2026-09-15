using BANxOpen.Foundation.Core.Materials.Assignment;
using BANxOpen.Foundation.Core.Materials.Rules.Features;

namespace BANxOpen.Foundation.Core.Materials.Rules.SheetMetal;

/// <summary>Sheet metal material libraries are reserved for sheet metal bodies, and sheet metal bodies take
/// materials from those libraries only.
///
/// Part of the baseline rather than the BANxOpen.SheetMetal domain on purpose: the libraries and body
/// classification belong to Foundation, and a tool that never registers the sheet metal domain must still refuse a
/// sheet metal material on a solid. Rules that depend on sheet metal features or preferences belong in that
/// domain's own modules.</summary>
public sealed class SheetMetalLibraryRuleModule : IMaterialRuleModule
{
    public const string Id = "SHEETMETAL.LIBRARY";

    /// <param name="sheetMetalLibraries">The configured list, normally from <see cref="SheetMetalLibraries.Load"/>.
    /// Omitted, the name-based default applies.</param>
    public SheetMetalLibraryRuleModule(SheetMetalLibraries? sheetMetalLibraries = null) =>
        ValidationRules = new IMaterialValidationRule[] { new BlockRestrictedBodyTypeRule(sheetMetalLibraries) };

    public string ModuleId => Id;

    public IReadOnlyList<IMaterialValidationRule> ValidationRules { get; }

    public IReadOnlyList<IFeatureMaterialConstraintProvider> FeatureConstraints => Array.Empty<IFeatureMaterialConstraintProvider>();

    public IReadOnlyList<IPostAssignmentEffectRule> SideEffectRules => Array.Empty<IPostAssignmentEffectRule>();
}
