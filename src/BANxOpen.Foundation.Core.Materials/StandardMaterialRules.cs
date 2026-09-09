using BANxOpen.Foundation.Core.Materials.Assignment;
using BANxOpen.Foundation.Core.Materials.Assignment.Rules;

namespace BANxOpen.Foundation.Core.Materials;

/// <summary>The default rule set every entry point starts from. It exists so that two composition roots
/// — the Material Assignment dialog and any feature tool that assigns material — cannot drift into
/// enforcing different rules for the same operation, which is the whole point of sharing the engine.
///
/// A caller that genuinely needs a different set can still build its own arrays; this is a starting
/// point, not a lock.</summary>
public static class StandardMaterialRules
{
    /// <summary>Gate rules in evaluation order. Orders leave gaps of 100 so a new rule can slot in
    /// without renumbering: body-type restriction (100), reassignment confirmation (200), coating
    /// validation (300).</summary>
    public static IReadOnlyList<IMaterialAssignmentRule> Gates() => new IMaterialAssignmentRule[]
    {
        new BlockRestrictedBodyTypeRule(),
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