using BANxOpen.Foundation.Contracts.Bodies;
using BANxOpen.Foundation.Core.Materials.Assignment;
using BANxOpen.Foundation.Core.RuleEngine;

namespace BANxOpen.Foundation.Core.Materials.Rules.SheetMetal;

/// <summary>Sheet metal material libraries are exclusive to sheet metal bodies: a sheet-metal-library
/// material can only go on a <see cref="BodyKind.SheetMetal"/> body, and a sheet metal body can only take
/// materials from a sheet metal library. Any mismatch is blocked as "not available".
///
/// This used to pair sheet metal libraries with <see cref="BodyKind.Sheet"/>, which conflated sheet metal
/// with zero-thickness surface bodies. NX sheet metal parts are solids, so they were classified Solid and
/// every sheet metal material was blocked on them. Surface bodies are now treated like solids here: they
/// take ordinary library materials and not sheet metal ones.
///
/// Which libraries count as sheet metal comes from <see cref="SheetMetalLibraries"/> — a configured list, or
/// the original name-based rule when none is configured.</summary>
public sealed class BlockRestrictedBodyTypeRule : IMaterialValidationRule
{
    private readonly SheetMetalLibraries _sheetMetalLibraries;

    public BlockRestrictedBodyTypeRule(SheetMetalLibraries? sheetMetalLibraries = null) =>
        _sheetMetalLibraries = sheetMetalLibraries ?? SheetMetalLibraries.NameHeuristic;

    public string RuleId => "BLOCK_BODY_TYPE_RESTRICTION";

    public int Order => MaterialRuleOrder.Validation.Eligibility;

    public RuleOutcome Evaluate(MaterialAssignmentRuleContext context)
    {
        // Previous category-based restriction — kept for reference, may be needed again later.
        // var restricted = context.TargetBody.Kind == BodyKind.Sheet
        //     && string.Equals(context.RequestedMaterial.Category.Key, "casting", StringComparison.OrdinalIgnoreCase);

        var isSheetMetalLibrary = _sheetMetalLibraries.IsSheetMetalLibrary(context.RequestedMaterial.LibraryId);
        var isSheetMetalBody = context.TargetBody.Kind == BodyKind.SheetMetal;
        var restricted = isSheetMetalLibrary != isSheetMetalBody;

        return restricted
            ? new RuleOutcome(
                RuleId,
                RuleDecision.Block,
                "BODY_TYPE_RESTRICTED",
                $"'{context.RequestedMaterial.Name}' is not available for this body.")
            : new RuleOutcome(RuleId, RuleDecision.Allow, null, null);
    }
}
