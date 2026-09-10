using BANxOpen.Foundation.Core.Materials.Bodies;
using BANxOpen.Foundation.Core.RuleEngine;
using BANxOpen.Foundation.Contracts.Bodies;

namespace BANxOpen.Foundation.Core.Materials.Assignment.Rules;

/// <summary>Sheet metal material libraries are exclusive to sheet metal bodies: a sheet-metal-library
/// material can only go on a <see cref="BodyKind.SheetMetal"/> body, and a sheet metal body can only take
/// materials from a sheet metal library. Any mismatch is blocked as "not available".
///
/// This used to pair sheet metal libraries with <see cref="BodyKind.Sheet"/>, which conflated sheet metal
/// with zero-thickness surface bodies. NX sheet metal parts are solids, so they were classified Solid and
/// every sheet metal material was blocked on them. Surface bodies are now treated like solids here: they
/// take ordinary library materials and not sheet metal ones.</summary>
public sealed class BlockRestrictedBodyTypeRule : IMaterialAssignmentRule
{
    public string RuleId => "BLOCK_BODY_TYPE_RESTRICTION";

    public int Order => 100;

    public RuleOutcome Evaluate(MaterialAssignmentRuleContext context)
    {
        // Previous category-based restriction — kept for reference, may be needed again later.
        // var restricted = context.TargetBody.Kind == BodyKind.Sheet
        //     && string.Equals(context.RequestedMaterial.Category.Key, "casting", StringComparison.OrdinalIgnoreCase);

        var isSheetMetalLibrary = IsSheetMetalLibrary(context.RequestedMaterial.LibraryId.Value);
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

    private static bool IsSheetMetalLibrary(string libraryName) =>
        libraryName.Replace(" ", "").IndexOf("sheetmetal", StringComparison.OrdinalIgnoreCase) >= 0;
}
