using System.Reflection;
using BANxOpen.Foundation.Contracts.Requirements;
using BANxOpen.Foundation.Core.Requirements;
using static BANxOpen.Foundation.Core.Tests.Requirements.RequirementNamingTests;

namespace BANxOpen.Foundation.Core.Tests;

public class FoundationTextTests
{
    private sealed class CustomText : FoundationText
    {
        public override string RequirementNameClaimed(string name, IEnumerable<string> identities) => $"custom:{name}";
    }

    [Fact]
    public void Default_words_a_name_conflict_with_the_name_and_every_claimant()
    {
        var diff = RequirementDiff.Compute(Array.Empty<RequirementSpec>(), new[] { Spec(key: "A-1"), Spec(key: "A_1") });

        var error = Assert.Single(diff.Errors);
        Assert.StartsWith("Requirement name '", error);
        Assert.Contains("D/A-1/A", error);
        Assert.Contains("D/A_1/A", error);
    }

    [Fact]
    public void An_overriding_describer_words_the_errors_it_is_given()
    {
        var diff = RequirementDiff.Compute(
            Array.Empty<RequirementSpec>(), new[] { Spec(key: "A-1"), Spec(key: "A_1") }, new CustomText());

        Assert.StartsWith("custom:", Assert.Single(diff.Errors));
    }

    [Fact]
    public void Codes_are_distinct()
    {
        var values = typeof(FoundationCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (string)f.GetValue(null)!)
            .ToList();

        Assert.Equal(values.Count, values.Distinct(StringComparer.Ordinal).Count());
    }
}
