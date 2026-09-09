using BANxOpen.Foundation.Core.Materials.Assignment;
using BANxOpen.Foundation.Core.Materials.Assignment.Rules;
using BANxOpen.Foundation.Core.Materials.Constraints;

namespace BANxOpen.Foundation.Core.Materials;

/// <summary>The default rule set every entry point starts from. It exists so that two composition roots
/// — the Material Assignment dialog and any feature tool that assigns material — cannot drift into
/// enforcing different rules for the same operation, which is the whole point of sharing the engine.
///
/// A caller that genuinely needs a different set can still build its own arrays; this is a starting
/// point, not a lock.</summary>
public static class StandardMaterialRules
{
    /// <summary>Gate rules in evaluation order: body-type restriction (100), feature constraints (150),
    /// reassignment confirmation (200), coating validation (300). Orders leave gaps so a new rule can slot
    /// in without renumbering.
    ///
    /// <paramref name="constraintProviders"/> is how a feature domain gets a say. Pass none — the default —
    /// and the feature-constraint rule is a no-op, which is exactly the behaviour before the seam existed.
    /// A composition root that wants domain restrictions enforced registers the domain's provider here;
    /// see BANxOpen.SheetMetal for an implementation.</summary>
    public static IReadOnlyList<IMaterialAssignmentRule> Gates(
        IEnumerable<IFeatureMaterialConstraintProvider>? constraintProviders = null) =>
        new IMaterialAssignmentRule[]
        {
            new BlockRestrictedBodyTypeRule(),
            new FeatureConstraintGateRule(
                constraintProviders ?? Array.Empty<IFeatureMaterialConstraintProvider>()),
            new RequireConfirmationOnReassignmentRule(),
            new ValidateCoatingDisplayMaterialRule(),
        };

    /// <summary>Post-assignment effect rules.
    ///
    /// <see cref="SyncPhysicalPropertiesEffectRule"/> is intentionally NOT registered: nothing executes
    /// SYNC_PHYSICAL_PROPERTY instructions, so wiring it would only generate work ApplyPlan discards. The
    /// rule and its tests are kept — add it here alongside a matching executor in PartMaterialService
    /// when physical property sync is wanted.</summary>
    public static IReadOnlyList<IPostAssignmentEffectRule> Effects() => new IPostAssignmentEffectRule[]
    {
        new SyncCoatingDisplayMaterialEffectRule(),
    };
}