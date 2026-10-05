using BANxOpen.Foundation.Contracts.Requirements;
using BANxOpen.Foundation.Core.Requirements;
using static BANxOpen.Foundation.Core.Tests.Requirements.RequirementNamingTests;

namespace BANxOpen.Foundation.Core.Tests.Requirements;

public class RequirementDiffTests
{
    [Fact]
    public void Compute_CreatesMissing_DeletesUnwanted_LeavesEquivalentAlone()
    {
        var kept = Spec(key: "kept");
        var gone = Spec(key: "gone");
        var added = Spec(key: "added");

        var diff = RequirementDiff.Compute(new[] { kept, gone }, new[] { kept with { }, added });

        Assert.Equal(new[] { added }, diff.ToCreate);
        Assert.Empty(diff.ToReplace);
        Assert.Equal(new[] { gone }, diff.ToDelete);
        Assert.Empty(diff.Errors);
    }

    [Fact]
    public void Compute_ReplacesWhenAnythingNxWouldShowChanged()
    {
        var existing = Spec(rule: new EqualsValueRule(0.04, 0.0005));

        Assert.Single(RequirementDiff.Compute(new[] { existing }, new[] { existing with { Rule = new EqualsValueRule(0.05, 0.0005) } }).ToReplace);
        Assert.Single(RequirementDiff.Compute(new[] { existing }, new[] { existing with { Description = new[] { "other" } } }).ToReplace);
        Assert.Single(RequirementDiff.Compute(new[] { existing }, new[] { existing with { AssociatedObjectKeys = new[] { "101" } } }).ToReplace);
        Assert.Single(RequirementDiff.Compute(new[] { existing }, new[] { existing with { CheckedFormula = "other" } }).ToReplace);
    }

    [Fact]
    public void Compute_IgnoresAssociatedObjectOrderAndNumberRoundTripNoise()
    {
        var existing = Spec(rule: new EqualsValueRule(0.04, 0.0005), objects: new[] { "101", "102" });
        var wanted = existing with { Rule = new EqualsValueRule(0.04 + 1e-15, 0.0005), AssociatedObjectKeys = new[] { "102", "101" } };

        Assert.True(RequirementDiff.Compute(new[] { existing }, new[] { wanted }).IsEmpty);
    }

    [Fact]
    public void Compute_ValueOrderMatters_BecauseNxShowsItInThatOrder()
    {
        var existing = Spec(rule: new SetOfValuesRule(new[] { "a", "b" }));

        Assert.Single(RequirementDiff.Compute(new[] { existing }, new[] { existing with { Rule = new SetOfValuesRule(new[] { "b", "a" }) } }).ToReplace);
    }

    [Fact]
    public void Compute_RefusesKeysThatCollapseToOneName()
    {
        var diff = RequirementDiff.Compute(Array.Empty<RequirementSpec>(), new[] { Spec(key: "A-1"), Spec(key: "A_1") });

        Assert.NotEmpty(diff.Errors);
        Assert.True(diff.IsEmpty);
    }

    [Fact]
    public void Compute_DeletesExtraCopiesOfAWantedRequirement()
    {
        var first = Spec(key: "dup");
        var copy = Spec(key: "dup", description: new[] { "copied by hand" });

        var diff = RequirementDiff.Compute(new[] { first, copy }, new[] { first with { } });

        Assert.Same(copy, Assert.Single(diff.ToDelete));
        Assert.Empty(diff.ToReplace);
    }

    [Fact]
    public void Compute_DeletesEveryCopyOfAnUnwantedRequirement()
    {
        var first = Spec(key: "dup");
        var copy = Spec(key: "dup", description: new[] { "copied by hand" });

        var diff = RequirementDiff.Compute(new[] { first, copy }, Array.Empty<RequirementSpec>());

        Assert.Equal(2, diff.ToDelete.Count);
    }
}
