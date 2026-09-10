using BANxOpen.Foundation.Core.Materials.Assignment;
using BANxOpen.Foundation.Core.Materials.Assignment.Rules;
using BANxOpen.Foundation.Core.Materials.Bodies;
using BANxOpen.Foundation.Contracts.Common;
using BANxOpen.Foundation.Contracts.Materials;
using BANxOpen.Foundation.Core.RuleEngine;
using static BANxOpen.Foundation.Core.Materials.Tests.Assignment.TestFixtures;
using BANxOpen.Foundation.Contracts.Bodies;

namespace BANxOpen.Foundation.Core.Materials.Tests.Assignment.Rules;

public class BlockRestrictedBodyTypeRuleTests
{
    private readonly BlockRestrictedBodyTypeRule _rule = new();

    private static Material MakeSheetMetalLibraryMaterial(string name = "Sheet Steel") =>
        MakeMaterial(name) with { LibraryId = new MaterialLibraryId("Sheet Metal Materials") };

    private RuleDecision Evaluate(Material material, BodyKind kind) =>
        _rule.Evaluate(new MaterialAssignmentRuleContext(
            material,
            MakeBody("body1", kind),
            CurrentAssignment: null,
            AllTargetBodiesInBatch: Array.Empty<BodyInfo>())).Decision;

    [Fact]
    public void Evaluate_AllowsSheetMetalLibraryMaterialOnSheetMetalBody()
    {
        Assert.Equal(RuleDecision.Allow, Evaluate(MakeSheetMetalLibraryMaterial(), BodyKind.SheetMetal));
    }

    [Fact]
    public void Evaluate_BlocksNonSheetMetalLibraryMaterialOnSheetMetalBody()
    {
        Assert.Equal(RuleDecision.Block, Evaluate(MakeMaterial("Steel"), BodyKind.SheetMetal));
    }

    [Fact]
    public void Evaluate_BlocksSheetMetalLibraryMaterialOnSolidBody()
    {
        Assert.Equal(RuleDecision.Block, Evaluate(MakeSheetMetalLibraryMaterial(), BodyKind.Solid));
    }

    [Fact]
    public void Evaluate_AllowsNonSheetMetalLibraryMaterialOnSolidBody()
    {
        Assert.Equal(RuleDecision.Allow, Evaluate(MakeMaterial("Steel"), BodyKind.Solid));
    }

    [Fact]
    public void Evaluate_BlocksSheetMetalLibraryMaterialOnSurfaceBody()
    {
        // A zero-thickness surface body is not sheet metal, whatever its NX name suggests.
        Assert.Equal(RuleDecision.Block, Evaluate(MakeSheetMetalLibraryMaterial(), BodyKind.Sheet));
    }

    [Fact]
    public void Evaluate_AllowsNonSheetMetalLibraryMaterialOnSurfaceBody()
    {
        Assert.Equal(RuleDecision.Allow, Evaluate(MakeMaterial("Steel"), BodyKind.Sheet));
    }
}
